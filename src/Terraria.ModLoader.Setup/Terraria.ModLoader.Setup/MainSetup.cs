using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

using DiffPatch;

using PatchReviewer;

using Terraria.ModLoader.Setup.Common;

namespace Terraria.ModLoader.Setup;

internal sealed class MainSetup : IDialogTaskInterface, IPatchReviewerInterface
{
	public CancellationToken CancellationToken => Form!.CancelSource.Token;
	
	public int MaxProgress
	{
		set { InvokeOnMainThread(() => { Form!.ProgressBar.Maximum = value; }); }
	}
	
	public int Progress
	{
		set { InvokeOnMainThread(() => { Form!.ProgressBar.Value = value; }); }
	}
	
	public MainForm? Form { get; set; }
	
	public void UpdateStatus(string value)
	{
		Form!.Invoke(
			() =>
			{
				Form.LabelStatus.Text = value;
			}
		);
	}
	
	public object InvokeOnMainThread(Delegate action)
	{
		return Form.Invoke(action);
	}
	
	SetupDialogResult IDialogTaskInterface.ShowDialog(string title, string message, SetupMessageBoxButtons buttons, SetupMessageBoxIcon icon)
	{
		return (SetupDialogResult)MessageBox.Show(message, title, (MessageBoxButtons)buttons, (MessageBoxIcon)icon);
	}
	
	SetupDialogResult IDialogTaskInterface.ShowDialog(ref OpenFileDialogParameters parameters)
	{
		var dialog = new OpenFileDialog
		{
			FileName = parameters.FileName,
			InitialDirectory = parameters.InitialDirectory,
			Filter = parameters.Filter,
			Title = parameters.Title,
		};
		
		var result = dialog.ShowDialog();
		parameters = new OpenFileDialogParameters(dialog.FileName, dialog.InitialDirectory, dialog.Filter, dialog.Title);
		return (SetupDialogResult)result;
	}
	
	void IPatchReviewerInterface.ShowReviewWindow(IEnumerable<FilePatcher> reviewResults, string baseDir)
	{
		var w = new ReviewWindow(reviewResults, commonBasePath: baseDir + '/')
		{
			AutoHeaders = true,
		};
		
		ElementHost.EnableModelessKeyboardInterop(w);
		w.ShowDialog();
	}
	
#region Settings
	private readonly Dictionary<string, object> knownSettings = new();
	private string? settingsPath;
	
	public T GetSettings<T>()
	{
		return (T)knownSettings[typeof(T).FullName!];
	}
	
	public void SetSettings<T>(T settings)
	{
		knownSettings[typeof(T).FullName!] = settings!;
	}
	
	public void LoadSettings(string path)
	{
		settingsPath = path;
		Settings.LoadSettings(path, knownSettings);
	}
	
	public void SaveSettings()
	{
		Settings.SaveSettings(settingsPath!, knownSettings);
	}
#endregion
}
