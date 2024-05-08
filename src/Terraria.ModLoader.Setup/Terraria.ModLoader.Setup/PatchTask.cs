using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

using DiffPatch;

using PatchReviewer;

using Terraria.ModLoader.Setup.Common;

using Settings = Terraria.ModLoader.Setup.Properties.Settings;

namespace Terraria.ModLoader.Setup;

internal sealed class PatchTask(ITaskInterface taskInterface, string baseDir, string patchedDir, string patchDir) : SetupOperation(taskInterface)
{
	private static readonly string[] nonSourceDirs = [ "bin", "obj", ".vs", ];
	
	public static IEnumerable<(string file, string relPath)> EnumerateSrcFiles(string dir)
	{
		return EnumerateFiles(dir).Where(f => !f.relPath.Split('/', '\\').Any(nonSourceDirs.Contains));
	}
	
	private readonly string baseDir = PreparePath(baseDir);
	private readonly string patchedDir = PreparePath(patchedDir);
	private readonly string patchDir = PreparePath(patchDir);
	private Patcher.Mode mode;
	private int warnings;
	private int failures;
	private int fuzzy;
	private StreamWriter logFile;
	
	private readonly ConcurrentBag<FilePatcher> results = [];
	
	public override bool StartupWarning()
	{
		var res = MessageBox.Show(
			"Any changes in /" + patchedDir + " that have not been converted to patches will be lost.",
			"Possible loss of data",
			MessageBoxButtons.OKCancel,
			MessageBoxIcon.Warning
		);
		
		return res == DialogResult.OK;
	}
	
	public override void Run()
	{
		Program.UpdateTargetsFiles(); //Update branch information
		
		mode = (Patcher.Mode) Settings.Default.PatchMode;
		
		var removedFileList = Path.Combine(patchDir, DiffTask.REMOVED_FILE_LIST);
		var noCopy = File.Exists(removedFileList) ? [..File.ReadAllLines(removedFileList),] : new HashSet<string>();
		
		var items = new List<WorkItem>();
		var newFiles = new HashSet<string>();
		
		foreach (var (file, relPath) in EnumerateFiles(patchDir))
		{
			if (relPath.EndsWith(".patch"))
			{
				items.Add(new WorkItem("Patching: " + relPath, () => newFiles.Add(PreparePath(Patch(file).PatchedPath))));
				noCopy.Add(relPath[..^6]);
			}
			else if (relPath != DiffTask.REMOVED_FILE_LIST)
			{
				var destination = Path.Combine(patchedDir, relPath);
				
				items.Add(new WorkItem("Copying: " + relPath, () => Copy(file, destination)));
				newFiles.Add(destination);
			}
		}
		
		foreach (var (file, relPath) in EnumerateSrcFiles(baseDir))
		{
			if (!noCopy.Contains(relPath))
			{
				var destination = Path.Combine(patchedDir, relPath);
				
				items.Add(new WorkItem("Copying: " + relPath, () => Copy(file, destination)));
				newFiles.Add(destination);
			}
		}
		
		try
		{
			CreateDirectory(Program.LOGS_DIR);
			// ReSharper disable once InconsistentlySynchronizedField
			logFile = new StreamWriter(Path.Combine(Program.LOGS_DIR, "patch.log"));
			
			TaskInterface.SetMaxProgress(items.Count);
			ExecuteParallel(items);
		}
		finally
		{
			// ReSharper disable once InconsistentlySynchronizedField
			logFile?.Close();
		}
		
		//Remove files and directories that weren't in patches and original src.
		
		TaskInterface.SetStatus("Deleting Old Src Files");
		
		foreach (var (file, _) in EnumerateSrcFiles(patchedDir))
		{
			if (!newFiles.Contains(file))
			{
				File.Delete(file);
			}
		}
		
		TaskInterface.SetStatus("Deleting Old Src's Empty Directories");
		
		DeleteEmptyDirs(patchedDir);
		
		TaskInterface.SetStatus("Old Src Removed");
		
		//Show patch reviewer if there were any fuzzy patches.
		
		if (fuzzy > 0 || mode == Patcher.Mode.FUZZY && failures > 0)
		{
			TaskInterface.Invoke(new Action(() => ShowReviewWindow(results)));
		}
	}
	
	private void ShowReviewWindow(IEnumerable<FilePatcher> reviewResults)
	{
		var w = new ReviewWindow(reviewResults, commonBasePath: baseDir + '/')
		{
			AutoHeaders = true,
		};
		
		ElementHost.EnableModelessKeyboardInterop(w);
		w.ShowDialog();
	}
	
	public override bool Failed()
	{
		return failures > 0;
	}
	
	public override bool Warnings()
	{
		return warnings > 0;
	}
	
	public override void FinishedDialog()
	{
		if (fuzzy > 0)
		{
			return;
		}
		
		MessageBox.Show(
			$"Patches applied with {failures} failures and {warnings} warnings.\nSee /logs/patch.log for details",
			"Patch Results",
			MessageBoxButtons.OK,
			Failed() ? MessageBoxIcon.Error : MessageBoxIcon.Warning
		);
	}
	
	private FilePatcher Patch(string patchPath)
	{
		var patcher = FilePatcher.FromPatchFile(patchPath);
		
		// WEIRD NITRATE PATCH: Forcefully redirect paths for actually reading
		// the patches since we do weird submodule stuff.
		if (!patcher.patchFile.basePath.Contains("src/staging/"))
		{
			patcher.patchFile.basePath = patcher.patchFile.basePath.Replace("src/", "src/staging/");
		}
		
		if (!patcher.patchFile.patchedPath.Contains("src/staging/"))
		{
			patcher.patchFile.patchedPath = patcher.patchFile.patchedPath.Replace("src/", "src/staging/");
		}
		
		patcher.Patch(mode);
		results.Add(patcher);
		CreateParentDirectory(patcher.PatchedPath);
		patcher.Save();
		
		int exact = 0, offset = 0;
		foreach (var result in patcher.results)
		{
			if (!result.success)
			{
				failures++;
				continue;
			}
			
			if (result.mode == Patcher.Mode.FUZZY || result.offsetWarning)
			{
				warnings++;
			}
			
			if (result.mode == Patcher.Mode.EXACT)
			{
				exact++;
			}
			else if (result.mode == Patcher.Mode.OFFSET)
			{
				offset++;
			}
			else if (result.mode == Patcher.Mode.FUZZY)
			{
				fuzzy++;
			}
		}
		
		var log = new StringBuilder();
		log.AppendLine($"{patcher.patchFile.basePath},\texact: {exact},\toffset: {offset},\tfuzzy: {fuzzy},\tfailed: {failures}");
		
		foreach (var res in patcher.results)
		{
			log.AppendLine(res.Summary());
		}
		
		Log(log.ToString());
		
		return patcher;
	}
	
	private void Log(string text)
	{
		lock (logFile)
		{
			logFile.Write(text);
		}
	}
}
