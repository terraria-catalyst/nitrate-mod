using System;
using System.IO;
using System.Windows.Forms;

using Terraria.ModLoader.Setup.Common;
using Terraria.ModLoader.Setup.Common.Utilities;

namespace Terraria.ModLoader.Setup;

internal static class Program
{
	/// <summary>
	/// The main entry point for the application.
	/// </summary>
	[STAThread]
	private static void Main(string[] args)
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		var setup = new MainSetup();
		CommonSetup.TaskInterface = setup;
		Settings.InitializeSettings(setup);
		CommonSetup.CreateSymlinks();
		FindTerrariaDirectoryIfNecessary();
		CommonSetup.CreateTmlSteamDirIfNecessary();
		CommonSetup.UpdateTargetsFiles();
		Application.Run(setup.Form = new MainForm(setup));
	}
	
	public static void SelectTmlDirectoryDialog()
	{
		while (true)
		{
			var dialog = new OpenFileDialog
			{
				InitialDirectory = Path.GetFullPath(Directory.Exists(CommonSetup.TerrariaSteamDirectory) ? CommonSetup.TerrariaSteamDirectory : "."),
				ValidateNames = false,
				CheckFileExists = false,
				CheckPathExists = true,
				FileName = "Folder Selection.",
			};
			
			if (dialog.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			
			CommonSetup.TmlDeveloperSteamDirectory = Path.GetDirectoryName(dialog.FileName)!;
			CommonSetup.TaskInterface.SaveSettings();
			
			CommonSetup.UpdateTargetsFiles();
			return;
		}
	}
	
	private static void FindTerrariaDirectoryIfNecessary()
	{
		if (!Directory.Exists(CommonSetup.TerrariaSteamDirectory))
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
			
			if (messageResult != DialogResult.OK || !CommonSetup.TrySelectTerrariaDirectoryDialog(out terrariaFolderPath))
			{
				Console.WriteLine("User chose to not retry. Exiting.");
				Environment.Exit(-1);
			}
		}
		
		CommonSetup.SetTerrariaDirectory(terrariaFolderPath);
	}
}
