using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using Terraria.ModLoader.Properties;
using Terraria.ModLoader.Setup.Utilities;

namespace Terraria.ModLoader.Setup;

internal static class Program
{
	public static readonly string LOGS_DIR = Path.Combine("setup", "logs");
	
	public static string TerrariaSteamDir => Settings.Default.TerrariaSteamDir;
	
	public static string TmlDevSteamDir => Settings.Default.TMLDevSteamDir;
	
	public static string TerrariaPath => Path.Combine(TerrariaSteamDir, "Terraria.exe");
	
	public static string TerrariaServerPath => Path.Combine(TerrariaSteamDir, "TerrariaServer.exe");
	
	/// <summary>
	/// The main entry point for the application.
	/// </summary>
	[STAThread]
	private static void Main()
	{
		CreateSymlinks();
		
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		
#if AUTO
			Settings.Default.TerrariaSteamDir = Path.GetFullPath(args[0]);
			Settings.Default.TMLDevSteamDir = Path.GetFullPath("steam_build");

			if (!Directory.Exists(TMLDevSteamDir))
				Directory.CreateDirectory(TMLDevSteamDir);
#else
		FindTerrariaDirectoryIfNecessary();
		CreateTmlSteamDirIfNecessary();
#endif
		UpdateTargetsFiles();
#if AUTO
			Console.WriteLine("Automatic setup start");
			new AutoSetup().DoAuto();
			Console.WriteLine("Automatic setup finished");
#else
		Application.Run(new MainForm());
#endif
	}
	
	public static void RunCmd(
		string dir,
		string cmd,
		string args,
		Action<string> output = null,
		Action<string> error = null,
		string input = null,
		CancellationToken cancel = default
	)
	{
		using var process = new Process();
		process.StartInfo = new ProcessStartInfo
		{
			FileName = cmd,
			Arguments = args,
			WorkingDirectory = dir,
			UseShellExecute = false,
			RedirectStandardInput = input != null,
			CreateNoWindow = true,
		};
		
		if (output != null)
		{
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
		}
		
		if (error != null)
		{
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
		}
		
		if (!process.Start())
		{
			throw new Exception($"Failed to start process: \"{cmd} {args}\"");
		}
		
		if (input != null)
		{
			var w = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false));
			w.Write(input);
			w.Close();
		}
		
		while (!process.HasExited)
		{
			if (cancel.IsCancellationRequested)
			{
				process.Kill();
				throw new OperationCanceledException(cancel);
			}
			
			process.WaitForExit(100);
			
			output?.Invoke(process.StandardOutput.ReadToEnd());
			error?.Invoke(process.StandardError.ReadToEnd());
		}
	}
	
	public static bool SelectAndSetTerrariaDirectoryDialog()
	{
		if (!TrySelectTerrariaDirectoryDialog(out var path))
		{
			return false;
		}
		
		SetTerrariaDirectory(path);
		return true;
	}
	
	public static bool TrySelectTerrariaDirectoryDialog(out string result)
	{
		result = null;
		
		while (true)
		{
			var dialog = new OpenFileDialog
			{
				InitialDirectory = Path.GetFullPath(Directory.Exists(TerrariaSteamDir) ? TerrariaSteamDir : "."),
				Filter = "Terraria|Terraria.exe",
				Title = "Select Terraria.exe",
			};
			
			if (dialog.ShowDialog() != DialogResult.OK)
			{
				return false;
			}
			
			string err = null;
			
			if (Path.GetFileName(dialog.FileName) != "Terraria.exe")
			{
				err = "File must be named Terraria.exe";
			}
			else if (!File.Exists(Path.Combine(Path.GetDirectoryName(dialog.FileName)!, "TerrariaServer.exe")))
			{
				err = "TerrariaServer.exe does not exist in the same directory";
			}
			
			if (err != null)
			{
				if (MessageBox.Show(err, "Invalid Selection", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
				{
					return false;
				}
			}
			else
			{
				result = Path.GetDirectoryName(dialog.FileName);
				
				return true;
			}
		}
	}
	
	private static void SetTerrariaDirectory(string path)
	{
		Settings.Default.TerrariaSteamDir = path;
		Settings.Default.TMLDevSteamDir = string.Empty;
		Settings.Default.Save();
		
		CreateTmlSteamDirIfNecessary();
		UpdateTargetsFiles();
	}
	
	public static void SelectTmlDirectoryDialog()
	{
		while (true)
		{
			var dialog = new OpenFileDialog
			{
				InitialDirectory = Path.GetFullPath(Directory.Exists(TerrariaSteamDir) ? TerrariaSteamDir : "."),
				ValidateNames = false,
				CheckFileExists = false,
				CheckPathExists = true,
				FileName = "Folder Selection.",
			};
			
			if (dialog.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			
			Settings.Default.TMLDevSteamDir = Path.GetDirectoryName(dialog.FileName);
			Settings.Default.Save();
			
			UpdateTargetsFiles();
			return;
		}
	}
	
	private static void FindTerrariaDirectoryIfNecessary()
	{
		if (!Directory.Exists(TerrariaSteamDir))
		{
			FindTerrariaDirectory();
		}
	}
	
	private static void FindTerrariaDirectory()
	{
		if (!SteamUtils.TryFindTerrariaDirectory(out var terrariaFolderPath))
		{
			const string message_text = "Unable to automatically find Terraria's installation path. Please select it manually.";
			
			Console.WriteLine(message_text);
			
			var messageResult = MessageBox.Show(message_text, "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
			
			if (messageResult != DialogResult.OK || !TrySelectTerrariaDirectoryDialog(out terrariaFolderPath))
			{
				Console.WriteLine("User chose to not retry. Exiting.");
				Environment.Exit(-1);
			}
		}
		
		SetTerrariaDirectory(terrariaFolderPath);
	}
	
	private static void CreateTmlSteamDirIfNecessary()
	{
		if (Directory.Exists(TmlDevSteamDir))
		{
			return;
		}
		
		Settings.Default.TMLDevSteamDir = Path.GetFullPath(Path.Combine(Settings.Default.TerrariaSteamDir, "..", "tModLoaderDev"));
		Settings.Default.Save();
		
		try
		{
			Directory.CreateDirectory(TmlDevSteamDir);
		}
		catch (Exception e)
		{
			Console.WriteLine($"{e.GetType().Name}: {e.Message}");
		}
	}
	
	internal static void UpdateTargetsFiles()
	{
		UpdateFileText("src/WorkspaceInfo.targets", GetWorkspaceInfoTargetsText());
		var tmlModTargetsContents = File.ReadAllText("patches/tModLoader/Terraria/release_extras/tMLMod.targets");
		
		var tmlVersion = Environment.GetEnvironmentVariable("TMLVERSION");
		if (!string.IsNullOrWhiteSpace(tmlVersion) && branch == "stable")
		{
			// Convert 2012.4.x to 2012_4
			Console.WriteLine($"TMLVERSION found: {tmlVersion}");
			var tmlVersionDefine = $"TML_{string.Join("_", tmlVersion.Split('.').Take(2))}";
			Console.WriteLine($"TMLVERSIONDefine: {tmlVersionDefine}");
			tmlModTargetsContents = tmlModTargetsContents.Replace("<!-- TML stable version define placeholder -->", $"<DefineConstants>$(DefineConstants);{tmlVersionDefine}</DefineConstants>");
			UpdateFileText("patches/tModLoader/Terraria/release_extras/tMLMod.targets", tmlModTargetsContents); // The patch file needs to be updated as well since it will be copied to src and the post-build will copy it to the steam folder as well.
		}
		
		UpdateFileText(Path.Combine(TmlDevSteamDir, "tMLMod.targets"), tmlModTargetsContents);
	}
	
	private static void UpdateFileText(string path, string text)
	{
		SetupOperation.CreateParentDirectory(path);
		
		if ((!File.Exists(path) || text != File.ReadAllText(path)) && path is not null)
		{
			File.WriteAllText(path, text);
		}
	}
	
	private static string branch = "";
	
	private static string GetWorkspaceInfoTargetsText()
	{
		var gitSha = "";
		RunCmd("", "git", "rev-parse HEAD", s => gitSha = s.Trim());
		
		branch = "";
		RunCmd("", "git", "rev-parse --abbrev-ref HEAD", s => branch = s.Trim());
		
		var githubHeadRef = Environment.GetEnvironmentVariable("GITHUB_HEAD_REF");
		if (!string.IsNullOrWhiteSpace(githubHeadRef))
		{
			Console.WriteLine($"GITHUB_HEAD_REF found: {githubHeadRef}");
			branch = githubHeadRef;
		}
		
		var headSha = Environment.GetEnvironmentVariable("HEAD_SHA");
		if (!string.IsNullOrWhiteSpace(headSha))
		{
			Console.WriteLine($"HEAD_SHA found: {headSha}");
			gitSha = headSha;
		}
		
		return
			$"""
			<?xml version="1.0" encoding="utf-8"?>
			<Project ToolsVersion="14.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
			  <!-- This file will always be overwritten, do not edit it manually. -->
			  <PropertyGroup>
				<BranchName>{branch}</BranchName>
				<CommitSHA>{gitSha}</CommitSHA>
				<TerrariaSteamPath>{TerrariaSteamDir}</TerrariaSteamPath>
			    <tModLoaderSteamPath>{TmlDevSteamDir}</tModLoaderSteamPath>
			  </PropertyGroup>
			</Project>
			""";
	}
	
	private static void CreateSymlinks()
	{
		string[] candidates = ["GoG", "Terraria", "TerrariaNetCore", "tModLoader",];
		var sourceDirectory = Path.Combine("src", "tModLoader", "patches");
		var targetDirectory = Path.Combine("patches");
		
		foreach (var candidate in candidates)
		{
			var source = Path.Combine(sourceDirectory, candidate);
			var target = Path.Combine(targetDirectory, candidate);
			
			if (!File.Exists(target) && !Directory.Exists(target))
			{
				CreateSymlink(source, target);
			}
		}
	}
	
	private static void CreateSymlink(string directoryToSymlink, string newPath)
	{
		if (!Directory.Exists(directoryToSymlink))
		{
			throw new DirectoryNotFoundException("Could not find directory to symlink to: " + directoryToSymlink);
		}
		
		if (Directory.Exists(newPath) || File.Exists(newPath))
		{
			throw new IOException("Attempted to create symlink at existing file/directory location: " + newPath);
		}
		
		ProcessStartInfo procInfo;
		if (OperatingSystem.IsWindows())
		{
			// Prefer junctions on Windows because they don't require administrator privileges.
			// mklink is a cmd command, so run cmd.exe...
			procInfo = new ProcessStartInfo("cmd.exe")
			{
				Arguments = $"/c mklink /j \"{newPath}\" \"{directoryToSymlink}\"",
				UseShellExecute = true,
			};
		}
		else
		{
			procInfo = new ProcessStartInfo("ln")
			{
				Arguments = $"-s \"{directoryToSymlink}\" \"{newPath}\"",
				UseShellExecute = true,
			};
		}
		
		var proc = Process.Start(procInfo);
		if (proc is null)
		{
			throw new IOException($"Failed to run command: {procInfo.FileName + " " + string.Join(' ', procInfo.Arguments)}");
		}
		
		proc.WaitForExit();
		if (proc.ExitCode != 0)
		{
			throw new IOException($"Failed to run command: {procInfo.FileName + " " + string.Join(' ', procInfo.Arguments)}");
		}
	}
}
