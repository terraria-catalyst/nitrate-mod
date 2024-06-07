using System.Collections.Generic;
using System.IO;
using System.Linq;

using DiffPatch;

namespace Terraria.ModLoader.Setup.Common.Tasks;

public sealed class DiffTask(ITaskInterface taskInterface, string baseDir, string srcDir, string patchDir) : SetupOperation(taskInterface)
{
	private static readonly string[] extensions = [ ".cs", ".csproj", ".ico", ".resx", ".png", "App.config", ".json", ".targets", ".txt", ".bat", ".sh", ];
	
	private static bool IsDiffable(string relPath)
	{
		return extensions.Any(relPath.EndsWith);
	}
	
	public const string REMOVED_FILE_LIST = "removed_files.list";
	
	public override void Run()
	{
		var status = taskInterface.Progress.CreateStatus(0, 2);
		var items = new List<WorkItem>();
		
		foreach (var (file, relPath) in PatchTask.EnumerateSrcFiles(srcDir))
		{
			if (!File.Exists(Path.Combine(baseDir, relPath)))
			{
				items.Add(new WorkItem("Copying: " + relPath, () => Copy(file, Path.Combine(patchDir, relPath))));
			}
			else if (IsDiffable(relPath))
			{
				items.Add(new WorkItem("Diffing: " + relPath, () => Diff(relPath)));
			}
		}
		
		ExecuteParallel(items);
		
		status.AddMessage("Deleting Unnecessary Patches");
		{
			foreach (var (file, relPath) in EnumerateFiles(patchDir))
			{
				var targetPath = relPath.EndsWith(".patch") ? relPath[..^6] : relPath;
				if (!File.Exists(Path.Combine(srcDir, targetPath)))
				{
					DeleteFile(file);
				}
			}
			
			DeleteEmptyDirs(patchDir);
			status.Current++;
		}
		
		status.AddMessage("Noting Removed Files");
		{
			var removedFiles = PatchTask.EnumerateSrcFiles(baseDir)
				.Where(f => !File.Exists(Path.Combine(srcDir, f.relPath)))
				.Select(f => f.relPath)
				.ToArray();
			
			var removedFileList = Path.Combine(patchDir, REMOVED_FILE_LIST);
			if (removedFiles.Length > 0)
			{
				File.WriteAllLines(removedFileList, removedFiles);
			}
			else
			{
				DeleteFile(removedFileList);
			}
			
			status.Current++;
		}
	}
	
	private void Diff(string relPath)
	{
		var patchFile = Differ.DiffFiles(
			new LineMatchedDiffer(),
			Path.Combine(baseDir, relPath).Replace('\\', '/'),
			Path.Combine(srcDir, relPath).Replace('\\', '/')
		);
		
		var patchPath = Path.Combine(patchDir, relPath + ".patch");
		if (!patchFile.IsEmpty)
		{
			CreateParentDirectory(patchPath);
			
			// NITRATE PATCH: Handle unique circumstances for our directories.
			var fileText = patchFile.ToString(true);
			var lineEnding = fileText.Contains("\r\n") ? "\r\n" : "\n";
			var textParts = fileText.Split(lineEnding);
			if (textParts.Length >= 2)
			{
				textParts[0] = textParts[0].Replace("--- src/staging/", "--- src/");
				textParts[1] = textParts[1].Replace("+++ src/staging/", "+++ src/");
				fileText = string.Join(lineEnding, textParts);
			}
			
			File.WriteAllText(patchPath, fileText);
		}
		else
		{
			DeleteFile(patchPath);
		}
	}
}
