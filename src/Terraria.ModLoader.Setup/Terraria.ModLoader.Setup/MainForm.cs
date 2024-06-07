using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using Terraria.ModLoader.Setup.Common;
using Terraria.ModLoader.Setup.Common.Tasks;
using Terraria.ModLoader.Setup.Common.Tasks.Roslyn;

using static Terraria.ModLoader.Setup.Program;

namespace Terraria.ModLoader.Setup;

public partial class MainForm : Form
{
	public CancellationTokenSource CancelSource { get; private set; }
	
	public Label LabelStatus => labelStatus;
	
	public ProgressBar ProgressBar => progressBar;
	
	private readonly IDictionary<Button, Func<SetupOperation>> taskButtons = new Dictionary<Button, Func<SetupOperation>>();
	private readonly ITaskInterface taskInterface;
	private bool closeOnCancel;
	
	public MainForm(ITaskInterface taskInterface)
	{
		this.taskInterface = taskInterface;
		
		FormBorderStyle = FormBorderStyle.FixedSingle;
		MaximizeBox = false;
		
		InitializeComponent();
		
		labelWorkingDirectoryDisplay.Text = Directory.GetCurrentDirectory();
		
#region Task button initialization
		taskButtons[buttonDecompile] = () => new DecompileTask(taskInterface, "src/staging/decompiled");
		// Terraria
		taskButtons[buttonDiffTerraria] = () => new DiffTask(taskInterface, "src/staging/decompiled", "src/staging/Terraria", "patches/Terraria");
		taskButtons[buttonPatchTerraria] = () => new PatchTask(taskInterface, "src/staging/decompiled", "src/staging/Terraria", "patches/Terraria");
		// Terraria .NET Core
		taskButtons[buttonDiffTerrariaNetCore] = () => new DiffTask(taskInterface, "src/staging/Terraria", "src/staging/TerrariaNetCore", "patches/TerrariaNetCore");
		taskButtons[buttonPatchTerrariaNetCore] = () => new PatchTask(taskInterface, "src/staging/Terraria", "src/staging/TerrariaNetCore", "patches/TerrariaNetCore");
		// tModLoader
		taskButtons[buttonDiffModLoader] = () => new DiffTask(taskInterface, "src/staging/TerrariaNetCore", "src/staging/tModLoader", "patches/tModLoader");
		taskButtons[buttonPatchModLoader] = () => new PatchTask(taskInterface, "src/staging/TerrariaNetCore", "src/staging/tModLoader", "patches/tModLoader");
		// Nitrate
		taskButtons[buttonDiffNitrate] = () => new DiffTask(taskInterface, "src/staging/tModLoader", "src/staging/Nitrate", "patches/Nitrate");
		taskButtons[buttonPatchNitrate] = () => new PatchTask(taskInterface, "src/staging/tModLoader", "src/staging/Nitrate", "patches/Nitrate");
		
		taskButtons[buttonRegenerateSource] = () =>
		{
			return new RegenSourceTask(
				taskInterface,
				new[] { buttonPatchTerraria, buttonPatchTerrariaNetCore, buttonPatchModLoader, buttonPatchNitrate, }
					.Select(b => taskButtons[b]()).ToArray()
			);
		};
		
		taskButtons[buttonSetup] = () =>
		{
			return new SetupTask(
				taskInterface,
				new[] { buttonDecompile, buttonRegenerateSource, }
					.Select(b => taskButtons[b]()).ToArray()
			);
		};
#endregion
		
		SetPatchMode(taskInterface.Settings.Get<PatchSettings>().PatchMode);
		
		Closing += (_, args) =>
		{
			if (!buttonCancel.Enabled)
			{
				return;
			}
			
			CancelSource.Cancel();
			args.Cancel = true;
			closeOnCancel = true;
		};
	}
	
	private void buttonCancel_Click(object sender, EventArgs e)
	{
		CancelSource.Cancel();
	}
	
	private void menuItemTerraria_Click(object sender, EventArgs e)
	{
		CommonSetup.SelectAndSetTerrariaDirectoryDialog();
	}
	
	private void menuItemDecompileServer_Click(object sender, EventArgs e)
	{
		RunTask(new DecompileTask(taskInterface, "src/staging/decompiled_server", true));
	}
	
	private void menuItemFormatCode_Click(object sender, EventArgs e)
	{
		RunTask(new FormatTask(taskInterface));
	}
	
	private void menuItemHookGen_Click(object sender, EventArgs e)
	{
		RunTask(new HookGenTask(taskInterface));
	}
	
	private void simplifierToolStripMenuItem_Click(object sender, EventArgs e)
	{
		RunTask(new SimplifierTask(taskInterface));
	}
	
	private void buttonTask_Click(object sender, EventArgs e)
	{
		RunTask(taskButtons[(Button)sender]());
	}
	
	private void RunTask(SetupOperation task)
	{
		CancelSource = new CancellationTokenSource();
		taskInterface.Progress.ClearProgress();
		foreach (var b in taskButtons.Keys)
		{
			b.Enabled = false;
		}
		
		buttonCancel.Enabled = true;
		
		new Thread(() => RunTaskThread(task)).Start();
	}
	
	private void RunTaskThread(SetupOperation task)
	{
		var errorLogFile = Path.Combine(CommonSetup.LOGS_DIR, "error.log");
		try
		{
			SetupOperation.DeleteFile(errorLogFile);
			
			if (!task.ConfigurationDialog())
			{
				return;
			}
			
			if (!task.StartupWarning())
			{
				return;
			}
			
			try
			{
				task.Run();
				
				if (CancelSource.IsCancellationRequested)
				{
					throw new OperationCanceledException();
				}
			}
			catch (OperationCanceledException e)
			{
				Invoke(
					() =>
					{
						labelStatus.Text = "Cancelled";
						if (e.Message != new OperationCanceledException().Message)
						{
							labelStatus.Text += ": " + e.Message;
						}
					}
				);
				
				return;
			}
			
			if (task.Failed() || task.Warnings())
			{
				task.FinishedDialog();
			}
			
			Invoke(
				() =>
				{
					labelStatus.Text = task.Failed() ? "Failed" : "Done";
				}
			);
		}
		catch (Exception e)
		{
			var status = "";
			Invoke(
				() =>
				{
					status = labelStatus.Text;
					labelStatus.Text = "Error: " + e.Message.Trim();
				}
			);
			
			SetupOperation.CreateDirectory(CommonSetup.LOGS_DIR);
			File.WriteAllText(errorLogFile, status + "\r\n" + e);
		}
		finally
		{
			Invoke(
				() =>
				{
					foreach (var b in taskButtons.Keys)
					{
						b.Enabled = true;
					}
					
					buttonCancel.Enabled = false;
					progressBar.Value = 0;
					if (closeOnCancel)
					{
						Close();
					}
				}
			);
		}
	}
	
	private void SetPatchMode(int mode)
	{
		exactToolStripMenuItem.Checked = mode == 0;
		offsetToolStripMenuItem.Checked = mode == 1;
		fuzzyToolStripMenuItem.Checked = mode == 2;
		taskInterface.Settings.Get<PatchSettings>().PatchMode = mode;
		taskInterface.Settings.Save();
	}
	
	private void exactToolStripMenuItem_Click(object sender, EventArgs e)
	{
		SetPatchMode(0);
	}
	
	private void offsetToolStripMenuItem_Click(object sender, EventArgs e)
	{
		SetPatchMode(1);
	}
	
	private void fuzzyToolStripMenuItem_Click(object sender, EventArgs e)
	{
		SetPatchMode(2);
	}
	
	private void mainMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }
	
	private void toolTipButtons_Popup(object sender, PopupEventArgs e) { }
	
	private void menuItemTmlPath_Click(object sender, EventArgs e)
	{
		SelectTmlDirectoryDialog();
	}
}
