using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Terraria.ModLoader.Setup.Common.Tasks;

namespace Terraria.ModLoader.Setup.Common;

public static class CommonSetup
{
	public static readonly string LOGS_DIR = Path.Combine("setup", "logs");
	
	public static readonly string SETTINGS_DIR = Path.Combine("setup", "settings");
	
	public static readonly string SETTINGS_PATH = Path.Combine(SETTINGS_DIR, "settings.json");
	
	public static ITaskInterface TaskInterface { get; set; } = null!;
	
	public static bool IsAutomatic { get; set; }
	
	private static string? terrariaSteamDirectory;
	
	public static string TerrariaSteamDirectory
	{
		get
		{
			if (IsAutomatic)
			{
				return terrariaSteamDirectory ??= TaskInterface.GetSettings<TerrariaPathSettings>().TerrariaSteamDirectory;
			}
			
			return TaskInterface.GetSettings<TerrariaPathSettings>().TerrariaSteamDirectory;
		}
		
		set
		{
			if (IsAutomatic)
			{
				terrariaSteamDirectory = value;
			}
			else
			{
				TaskInterface.GetSettings<TerrariaPathSettings>().TerrariaSteamDirectory = value;
			}
		}
	}
	
	private static string? tmlDeveloperSteamDirectory;
	
	public static string TmlDeveloperSteamDirectory
	{
		get
		{
			if (IsAutomatic)
			{
				return tmlDeveloperSteamDirectory ??= TaskInterface.GetSettings<TerrariaPathSettings>().TmlDeveloperSteamDirectory;
			}
			
			return TaskInterface.GetSettings<TerrariaPathSettings>().TmlDeveloperSteamDirectory;
		}
		
		set
		{
			if (IsAutomatic)
			{
				tmlDeveloperSteamDirectory = value;
			}
			else
			{
				TaskInterface.GetSettings<TerrariaPathSettings>().TmlDeveloperSteamDirectory = value;
			}
		}
	}
	
	public static string TerrariaPath => Path.Combine(TerrariaSteamDirectory, "Terraria.exe");
	
	public static string TerrariaServerPath => Path.Combine(TerrariaSteamDirectory, "TerrariaServer.exe");
	
#region Create Symlinks
	public static void CreateSymlinks()
	{
		string[] candidates = ["GoG", "Terraria", "TerrariaNetCore", "tModLoader",];
		var sourceDirectory = Path.Combine("src", "Terraria.ModLoader", "patches");
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
#endregion
	
#region Update Targets Files
	private static string branch = "";
	
	public static void UpdateTargetsFiles()
	{
		UpdateFileText("src/staging/WorkspaceInfo.targets", GetWorkspaceInfoTargetsText());
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
		
		UpdateFileText(Path.Combine(TmlDeveloperSteamDirectory, "tMLMod.targets"), tmlModTargetsContents);
	}
	
	private static void UpdateFileText(string path, string text)
	{
		SetupOperation.CreateParentDirectory(path);
		
		if ((!File.Exists(path) || text != File.ReadAllText(path)) && path is not null)
		{
			File.WriteAllText(path, text);
		}
	}
	
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
				<TerrariaSteamPath>{TerrariaSteamDirectory}</TerrariaSteamPath>
			    <tModLoaderSteamPath>{TmlDeveloperSteamDirectory}</tModLoaderSteamPath>
			  </PropertyGroup>
			</Project>
			""";
	}
#endregion
	
#region Select and Set Terraria Directory Dialog
	public static bool SelectAndSetTerrariaDirectoryDialog()
	{
		if (!TrySelectTerrariaDirectoryDialog(out var path))
		{
			return false;
		}
		
		SetTerrariaDirectory(path);
		return true;
	}
	
	public static bool TrySelectTerrariaDirectoryDialog(out string? result)
	{
		result = null;
		
		while (true)
		{
			var dialog = new OpenFileDialogParameters
			{
				InitialDirectory = Path.GetFullPath(Directory.Exists(TerrariaSteamDirectory) ? TerrariaSteamDirectory : "."),
				Filter = "Terraria|Terraria.exe",
				Title = "Select Terraria.exe",
			};
			
			if (TaskInterface.ShowDialogWithOkFallback(ref dialog) != SetupDialogResult.Ok)
			{
				return false;
			}
			
			string? err = null;
			
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
				if (TaskInterface.ShowDialogWithOkFallback("Invalid Selection", err, SetupMessageBoxButtons.RetryCancel, SetupMessageBoxIcon.Error) == SetupDialogResult.Cancel)
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
	
	public static void SetTerrariaDirectory(string path)
	{
		TerrariaSteamDirectory = path;
		TmlDeveloperSteamDirectory = string.Empty;
		TaskInterface.SaveSettings();
		
		CreateTmlSteamDirIfNecessary();
		UpdateTargetsFiles();
	}
	
	public static void CreateTmlSteamDirIfNecessary()
	{
		if (Directory.Exists(TmlDeveloperSteamDirectory))
		{
			return;
		}
		
		TmlDeveloperSteamDirectory = Path.GetFullPath(Path.Combine(TerrariaSteamDirectory, "..", "tModLoaderDev"));
		TaskInterface.SaveSettings();
		
		try
		{
			Directory.CreateDirectory(TmlDeveloperSteamDirectory);
		}
		catch (Exception e)
		{
			Console.WriteLine($"{e.GetType().Name}: {e.Message}");
		}
	}
#endregion
	
#region Utilities
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
#endregion
}
