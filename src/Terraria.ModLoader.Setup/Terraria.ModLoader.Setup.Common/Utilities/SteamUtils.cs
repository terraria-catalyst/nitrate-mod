using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Microsoft.Win32;

namespace Terraria.ModLoader.Setup.Common.Utilities;

public static partial class SteamUtils
{
	public const int TERRARIA_APP_ID = 105600;
	
	public static readonly string TERRARIA_MANIFEST_FILE = $"appmanifest_{TERRARIA_APP_ID}.acf";
	
	private static readonly Regex steamLibraryFoldersRegex = SteamLibraryFoldersRegex();
	private static readonly Regex steamManifestInstallDirRegex = SteamManifestInstallDirRegex();
	
	public static bool TryFindTerrariaDirectory(out string path)
	{
		if (TryGetSteamDirectory(out var steamDirectory) && TryGetTerrariaDirectoryFromSteam(steamDirectory, out path))
		{
			return true;
		}
		
		path = null;
		return false;
	}
	
	public static bool TryGetTerrariaDirectoryFromSteam(string steamDirectory, out string path)
	{
		var steamApps = Path.Combine(steamDirectory, "steamapps");
		var libraries = new List<string>
		{
			steamApps,
		};
		
		var libraryFoldersFile = Path.Combine(steamApps, "libraryfolders.vdf");
		
		if (File.Exists(libraryFoldersFile))
		{
			var contents = File.ReadAllText(libraryFoldersFile);
			
			var matches = steamLibraryFoldersRegex.Matches(contents);
			
			foreach (Match match in matches)
			{
				var directory = Path.Combine(match.Groups[2].Value.Replace(@"\\", @"\"), "steamapps");
				
				if (Directory.Exists(directory))
				{
					libraries.Add(directory);
				}
			}
		}
		
		foreach (var directory in libraries)
		{
			var manifestPath = Path.Combine(directory, TERRARIA_MANIFEST_FILE);
			
			if (!File.Exists(manifestPath))
			{
				continue;
			}
			
			var contents = File.ReadAllText(manifestPath);
			var match = steamManifestInstallDirRegex.Match(contents);
			
			if (!match.Success)
			{
				continue;
			}
			
			path = Path.Combine(directory, "common", match.Groups[1].Value);
			
			if (Directory.Exists(path))
			{
				return true;
			}
		}
		
		path = null;
		
		return false;
	}
	
	public static bool TryGetSteamDirectory(out string path)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			path = GetSteamDirectoryWindows();
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			path = "~/Library/Application Support/Steam";
		}
		else
		{
			// Some kind of linux?
			path = "~/.local/share/Steam";
		}
		
		return path != null && Directory.Exists(path);
	}
	
	// Isolated to avoid loading Win32 stuff outside Windows.
	private static string GetSteamDirectoryWindows()
	{
		var keyPath = Environment.Is64BitOperatingSystem ? @"SOFTWARE\Wow6432Node\Valve\Steam" : @"SOFTWARE\Valve\Steam";
		
		using var key = Registry.LocalMachine.CreateSubKey(keyPath);
		if (key is null)
		{
			throw new InvalidOperationException("Failed to get sub-key: " + keyPath);
		}
		
		return key.GetValue("InstallPath") as string;
	}
	
	[GeneratedRegex(
		"""
		"(\d+)"[^\S\r\n]+"(.+)"
		""",
		RegexOptions.Compiled
	)]
	private static partial Regex SteamLibraryFoldersRegex();
	
	[GeneratedRegex(
		"""
		"installdir"[^\S\r\n]+"([^\r\n]+)"
		""",
		RegexOptions.Compiled
	)]
	private static partial Regex SteamManifestInstallDirRegex();
}
