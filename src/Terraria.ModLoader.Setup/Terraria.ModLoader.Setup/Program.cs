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
		
		var setup = new MainSetup();
		{
			Settings.InitializeSettings(setup);
			CommonSetup.IsAutomatic[setup] = false;
			CommonSetup.CreateSymlinks();
			FindTerrariaDirectoryIfNecessary(setup);
			CommonSetup.CreateTmlSteamDirIfNecessary(setup);
			CommonSetup.UpdateTargetsFiles(setup);
		}
		
		Application.Run(setup.Form = new MainForm(setup));
	}
	
	public static void SelectTmlDirectoryDialog(ITaskInterface taskInterface)
	{
		while (true)
		{
			var dialog = new OpenFileDialog
			{
				InitialDirectory = Path.GetFullPath(Directory.Exists(CommonSetup.TerrariaSteamDirectory[taskInterface]) ? CommonSetup.TerrariaSteamDirectory[taskInterface] : "."),
				ValidateNames = false,
				CheckFileExists = false,
				CheckPathExists = true,
				FileName = "Folder Selection.",
			};
			
			if (dialog.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			
			CommonSetup.TmlDeveloperSteamDirectory[taskInterface] = Path.GetDirectoryName(dialog.FileName)!;
			taskInterface.Settings.Save();
			
			CommonSetup.UpdateTargetsFiles(taskInterface);
			return;
		}
	}
	
	private static void FindTerrariaDirectoryIfNecessary(ITaskInterface taskInterface)
	{
		if (!Directory.Exists(CommonSetup.TerrariaSteamDirectory[taskInterface]))
		{
			FindTerrariaDirectory(taskInterface);
		}
	}
	
	private static void FindTerrariaDirectory(ITaskInterface taskInterface)
	{
		if (!SteamUtils.TryFindTerrariaDirectory(out var terrariaFolderPath))
		{
			const string message_text = "Unable to automatically find Terraria's installation path. Please select it manually.";
			
			Console.WriteLine(message_text);
			
			var messageResult = MessageBox.Show(message_text, "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
			
			if (messageResult != DialogResult.OK || !CommonSetup.TrySelectTerrariaDirectoryDialog(taskInterface, out terrariaFolderPath))
			{
				Console.WriteLine("User chose to not retry. Exiting.");
				Environment.Exit(-1);
			}
		}
		
		CommonSetup.SetTerrariaDirectory(taskInterface, terrariaFolderPath);
	}
}
