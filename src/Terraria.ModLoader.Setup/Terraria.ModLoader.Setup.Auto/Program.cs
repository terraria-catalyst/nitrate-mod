using System;
using System.IO;

using Terraria.ModLoader.Setup.Common;

namespace Terraria.ModLoader.Setup.Auto;

internal static class Program
{
	public static void Main(string[] args)
	{
		var setup = new AutoSetup();
		Settings.InitializeSettings(setup);
		CommonSetup.IsAutomatic[setup] = true;
		CommonSetup.CreateSymlinks();
		
		CommonSetup.TerrariaSteamDirectory[setup] = Path.GetFullPath(args[0]);
		CommonSetup.TmlDeveloperSteamDirectory[setup] = Path.GetFullPath("steam_build");
		
		if (!Directory.Exists(CommonSetup.TmlDeveloperSteamDirectory[setup]))
		{
			Directory.CreateDirectory(CommonSetup.TmlDeveloperSteamDirectory[setup]);
		}
		
		Console.WriteLine("Automatic setup start");
		setup.DoAuto();
		Console.WriteLine("Automatic setup finished");
	}
}
