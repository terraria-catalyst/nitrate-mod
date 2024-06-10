using System;
using System.IO;

using Terraria.ModLoader.Setup.Common;

namespace Terraria.ModLoader.Setup.Auto;

internal static class Program
{
	public static void Main(string[] args)
	{
		var ctx = new CommonContext(new AutoSetup())
		{
			IsAutomatic = true,
		};

		Settings.InitializeSettings(ctx.TaskInterface);
		CommonSetup.CreateSymlinks();

		ctx.TerrariaSteamDirectory = Path.GetFullPath(args[0]);
		ctx.TmlDeveloperSteamDirectory = Path.GetFullPath("steam_build");

		if (!Directory.Exists(ctx.TmlDeveloperSteamDirectory))
		{
			Directory.CreateDirectory(ctx.TmlDeveloperSteamDirectory);
		}

		Console.WriteLine("Automatic setup start");
		AutoSetup.DoAuto(ctx);
		Console.WriteLine("Automatic setup finished");
	}
}
