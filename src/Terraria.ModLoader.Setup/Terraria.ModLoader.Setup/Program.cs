using System;
using System.IO;
using System.Windows.Forms;

using Terraria.ModLoader.Setup.Common;
using Terraria.ModLoader.Setup.Common.Utilities;

namespace Terraria.ModLoader.Setup;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		
		var ctx = new CommonContext(new MainSetup());
		{
			Settings.InitializeSettings(ctx.TaskInterface);
			CommonSetup.CreateSymlinks();
			FindTerrariaDirectoryIfNecessary(ctx);
			CommonSetup.CreateTmlSteamDirIfNecessary(ctx);
			CommonSetup.UpdateTargetsFiles(ctx);
		}
		
		Application.Run(((MainSetup)ctx.TaskInterface).Form = new MainForm(ctx));
	}
	
	public static void SelectTmlDirectoryDialog(CommonContext ctx)
	{
		while (true)
		{
			var dialog = new OpenFileDialog
			{
				InitialDirectory = Path.GetFullPath(Directory.Exists(ctx.TerrariaSteamDirectory) ? ctx.TerrariaSteamDirectory : "."),
				ValidateNames = false,
				CheckFileExists = false,
				CheckPathExists = true,
				FileName = "Folder Selection.",
			};
			
			if (dialog.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			
			ctx.TmlDeveloperSteamDirectory = Path.GetDirectoryName(dialog.FileName)!;
			ctx.Settings.Save();
			
			CommonSetup.UpdateTargetsFiles(ctx);
			return;
		}
	}
	
	private static void FindTerrariaDirectoryIfNecessary(CommonContext ctx)
	{
		if (!Directory.Exists(ctx.TerrariaSteamDirectory))
		{
			FindTerrariaDirectory(ctx);
		}
	}
	
	private static void FindTerrariaDirectory(CommonContext ctx)
	{
		if (!SteamUtils.TryFindTerrariaDirectory(out var terrariaFolderPath))
		{
			const string message_text = "Unable to automatically find Terraria's installation path. Please select it manually.";
			
			Console.WriteLine(message_text);
			
			var messageResult = MessageBox.Show(message_text, "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
			
			if (messageResult != DialogResult.OK || !CommonSetup.TrySelectTerrariaDirectoryDialog(ctx, out terrariaFolderPath))
			{
				Console.WriteLine("User chose to not retry. Exiting.");
				Environment.Exit(-1);
			}
		}
		
		CommonSetup.SetTerrariaDirectory(ctx, terrariaFolderPath);
	}
}
