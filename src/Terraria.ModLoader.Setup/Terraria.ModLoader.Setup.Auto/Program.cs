using System;
using System.IO;

using Terraria.ModLoader.Setup.Common;

namespace Terraria.ModLoader.Setup.Auto;

internal static class Program
{
	public static void Main(string[] args)
	{
		var setup = new AutoSetup();
		CommonSetup.IsAutomatic = true;
		CommonSetup.TaskInterface = setup;
		Settings.InitializeSettings(setup);
		CommonSetup.CreateSymlinks();
		
		CommonSetup.TerrariaSteamDirectory = Path.GetFullPath(args[0]);
		CommonSetup.TmlDeveloperSteamDirectory = Path.GetFullPath("steam_build");
		
		if (!Directory.Exists(CommonSetup.TmlDeveloperSteamDirectory))
		{
			Directory.CreateDirectory(CommonSetup.TmlDeveloperSteamDirectory);
		}
		
		Console.WriteLine("Automatic setup start");
		setup.DoAuto();
		Console.WriteLine("Automatic setup finished");
	}
}
