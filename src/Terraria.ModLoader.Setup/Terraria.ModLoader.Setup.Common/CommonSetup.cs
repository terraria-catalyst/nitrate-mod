using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Terraria.ModLoader.Setup.Common.Tasks;

namespace Terraria.ModLoader.Setup.Common;

/// <summary>
///		Shared (common) setup operations across implementations.
/// </summary>
public static class CommonSetup
{
#region Constants
	/// <summary>
	///		The setup directory name.
	/// </summary>
	public const string SETUP_DIR = ".setup";
	
	/// <summary>
	///		The setup logs directory.
	/// </summary>
	public static readonly string LOGS_DIR = Path.Combine(SETUP_DIR, "logs");
	
	/// <summary>
	///		The setup settings directory.
	/// </summary>
	public static readonly string SETTINGS_DIR = Path.Combine(SETUP_DIR, "settings");
	
	/// <summary>
	///		The setup settings file path.
	/// </summary>
	public static readonly string SETTINGS_PATH = Path.Combine(SETTINGS_DIR, "settings.json");
#endregion
	
#region Create Symlinks
	/// <summary>
	///		Initializes symlinks for compatibility between the source
	///		Terraria.ModLoader (tModLoader) submodule patches and our
	///		(Nitrate's) modified setup tool.
	/// </summary>
	public static void CreateSymlinks()
	{
		var candidates = new[] { "GoG", "Terraria", "TerrariaNetCore", "tModLoader", };
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
	/// <summary>
	///		Updates the <c>WorkspaceInfo.targets</c> amd <c>tMLMod.targets</c>
	///		files with current environment information.
	/// </summary>
	public static void UpdateTargetsFiles(CommonContext ctx)
	{
		// WorkspaceInfo.targets
		{
			updateTextFile("src/staging/WorkspaceInfo.targets", GetWorkspaceInfoTargetsText(ctx));
		}
		
		// tMLMod.targets
		{
			var targetsContents = File.ReadAllText("patches/tModLoader/Terraria/release_extras/tMLMod.targets");
			var tmlVersion = Environment.GetEnvironmentVariable("TMLVERSION");
			
			if (!string.IsNullOrWhiteSpace(tmlVersion) && ctx.Branch == "stable")
			{
				// Convert 2012.4.x to 2012_4
				var tmlVersionDefine = $"TML_{string.Join("_", tmlVersion.Split('.').Take(2))}";
				
				Console.WriteLine($"TMLVERSION found: {tmlVersion}");
				Console.WriteLine($"Defining TMLVERSION constant as: {tmlVersionDefine}");
				
				targetsContents = targetsContents.Replace("<!-- TML stable version define placeholder -->", $"<DefineConstants>$(DefineConstants);{tmlVersionDefine}</DefineConstants>");
				
				// The patch file needs to be updated as well since it gets
				// copied to src and the post-build task will copy it to the
				// Steam folder as well.
				updateTextFile("patches/tModLoader/Terraria/release_extras/tMLMod.targets", targetsContents);
			}
			
			updateTextFile(Path.Combine(ctx.TmlDeveloperSteamDirectory, "tMLMod.targets"), targetsContents);
		}
		
		return;
		
		static void updateTextFile(string path, string text)
		{
			SetupOperation.CreateParentDirectory(path);
			
			if (!File.Exists(path) || text != File.ReadAllText(path))
			{
				File.WriteAllText(path, text);
			}
		}
	}
	
	private static string GetWorkspaceInfoTargetsText(CommonContext ctx)
	{
		var gitSha = "";
		{
			RunCommand("", "git", "rev-parse HEAD", s => gitSha = s.Trim());
		}
		
		ctx.Branch = "";
		{
			RunCommand("", "git", "rev-parse --abbrev-ref HEAD", s => ctx.Branch = s.Trim());
		}
		
		var githubHeadRef = Environment.GetEnvironmentVariable("GITHUB_HEAD_REF");
		{
			if (!string.IsNullOrWhiteSpace(githubHeadRef))
			{
				Console.WriteLine($"GITHUB_HEAD_REF found: {githubHeadRef}");
				ctx.Branch = githubHeadRef;
			}
		}
		
		var headSha = Environment.GetEnvironmentVariable("HEAD_SHA");
		{
			if (!string.IsNullOrWhiteSpace(headSha))
			{
				Console.WriteLine($"HEAD_SHA found: {headSha}");
				gitSha = headSha;
			}
		}
		
		return
			$"""
			<?xml version="1.0" encoding="utf-8"?>
			<Project ToolsVersion="14.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
			  <!-- This file will always be overwritten, do not edit it manually. -->
			  <PropertyGroup>
				<BranchName>{ctx.Branch}</BranchName>
				<CommitSHA>{gitSha}</CommitSHA>
				<TerrariaSteamPath>{ctx.TerrariaSteamDirectory}</TerrariaSteamPath>
			    <tModLoaderSteamPath>{ctx.TmlDeveloperSteamDirectory}</tModLoaderSteamPath>
			  </PropertyGroup>
			</Project>
			""";
	}
#endregion
	
#region Select and Set Terraria Directory Dialog
	/// <summary>
	///		Prompts the user to select the Terraria directory and sets it.
	/// </summary>
	public static bool SelectAndSetTerrariaDirectoryDialog(CommonContext ctx)
	{
		if (!TrySelectTerrariaDirectoryDialog(ctx, out var path))
		{
			return false;
		}
		
		SetTerrariaDirectory(ctx, path);
		return true;
	}
	
	/// <summary>
	///		Prompts the user to select the Terraria directory.
	/// </summary>
	public static bool TrySelectTerrariaDirectoryDialog(CommonContext ctx, [NotNullWhen(returnValue: true)] out string? result)
	{
		result = null;
		
		while (true)
		{
			var dialog = new OpenFileDialogParameters
			{
				InitialDirectory = Path.GetFullPath(Directory.Exists(ctx.TerrariaSteamDirectory) ? ctx.TerrariaSteamDirectory : "."),
				Filter = "Terraria|Terraria.exe",
				Title = "Select Terraria.exe",
			};
			
			if (ctx.TaskInterface.ShowDialogWithOkFallback(ref dialog) != SetupDialogResult.Ok)
			{
				return false;
			}
			
			string? err = null;
			{
				// TODO: Are these restrictions really necessary?
				if (Path.GetFileName(dialog.FileName) != "Terraria.exe")
				{
					err = "File must be named Terraria.exe";
				}
				else if (!File.Exists(Path.Combine(Path.GetDirectoryName(dialog.FileName)!, "TerrariaServer.exe")))
				{
					err = "TerrariaServer.exe does not exist in the same directory";
				}
			}
			
			if (err != null)
			{
				var retryPrompt = ctx.TaskInterface.ShowDialogWithOkFallback("Invalid Selection", err, SetupMessageBoxButtons.RetryCancel, SetupMessageBoxIcon.Error);
				if (retryPrompt == SetupDialogResult.Cancel)
				{
					return false;
				}
			}
			else
			{
				result = Path.GetDirectoryName(dialog.FileName)!;
				return true;
			}
		}
	}
	
	/// <summary>
	///		Sets the Terraria directory.
	/// </summary>
	public static void SetTerrariaDirectory(CommonContext ctx, string path)
	{
		ctx.TerrariaSteamDirectory = path;
		ctx.TmlDeveloperSteamDirectory = string.Empty;
		ctx.Settings.Save();
		
		CreateTmlSteamDirIfNecessary(ctx);
		UpdateTargetsFiles(ctx);
	}
	
	/// <summary>
	///		Creates the Terraria.ModLoader (tModLoader) Steam developer
	///		directory if necessary.
	/// </summary>
	public static void CreateTmlSteamDirIfNecessary(CommonContext ctx)
	{
		if (Directory.Exists(ctx.TmlDeveloperSteamDirectory))
		{
			return;
		}
		
		ctx.TmlDeveloperSteamDirectory = Path.GetFullPath(Path.Combine(ctx.TerrariaSteamDirectory, "..", "tModLoaderDev"));
		ctx.Settings.Save();
		
		try
		{
			Directory.CreateDirectory(ctx.TmlDeveloperSteamDirectory);
		}
		catch (Exception e)
		{
			Console.WriteLine($"{e.GetType().Name}: {e.Message}");
		}
	}
#endregion
	
#region Miscellaneous Utilities
	/// <summary>
	///		Executes the given command.
	/// </summary>
	/// <param name="dir">The working directory.</param>
	/// <param name="cmd">The command (executable) name.</param>
	/// <param name="args">The arguments.</param>
	/// <param name="output">Invoked when an output is received.</param>
	/// <param name="error">Invoked when an error is received.</param>
	/// <param name="input">Input to pass to the process.</param>
	/// <param name="cancel">The cancellation token.</param>
	public static void RunCommand(
		string dir,
		string cmd,
		string args,
		Action<string>? output = null,
		Action<string>? error = null,
		string? input = null,
		CancellationToken cancel = default
	)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = cmd,
			Arguments = args,
			WorkingDirectory = dir,
			UseShellExecute = false,
			RedirectStandardInput = input != null,
			CreateNoWindow = true,
		};
		
		using var process = new Process();
		process.StartInfo = startInfo;
		
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
