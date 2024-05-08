using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using Terraria.ModLoader.Setup.Properties;

using static Terraria.ModLoader.Setup.Program;

using Settings = Terraria.ModLoader.Setup.Properties.Settings;

namespace Terraria.ModLoader.Setup;

public partial class MainForm : Form, ITaskInterface
{
	private readonly IDictionary<Button, Func<SetupOperation>> taskButtons = new Dictionary<Button, Func<SetupOperation>>();
	private CancellationTokenSource cancelSource;
	private bool closeOnCancel;
	
	public MainForm()
	{
		FormBorderStyle = FormBorderStyle.FixedSingle;
		MaximizeBox = false;
		
		InitializeComponent();
		
		labelWorkingDirectoryDisplay.Text = Directory.GetCurrentDirectory();
		
#region Task button initialization
		taskButtons[buttonDecompile] = () => new DecompileTask(this, "src/staging/decompiled");
		// Terraria
		taskButtons[buttonDiffTerraria] = () => new DiffTask(this, "src/staging/decompiled", "src/staging/Terraria", "patches/Terraria");
		taskButtons[buttonPatchTerraria] = () => new PatchTask(this, "src/staging/decompiled", "src/staging/Terraria", "patches/Terraria");
		// Terraria .NET Core
		taskButtons[buttonDiffTerrariaNetCore] = () => new DiffTask(this, "src/staging/Terraria", "src/staging/TerrariaNetCore", "patches/TerrariaNetCore");
		taskButtons[buttonPatchTerrariaNetCore] = () => new PatchTask(this, "src/staging/Terraria", "src/staging/TerrariaNetCore", "patches/TerrariaNetCore");
		// tModLoader
		taskButtons[buttonDiffModLoader] = () => new DiffTask(this, "src/staging/TerrariaNetCore", "src/staging/tModLoader", "patches/tModLoader");
		taskButtons[buttonPatchModLoader] = () => new PatchTask(this, "src/staging/TerrariaNetCore", "src/staging/tModLoader", "patches/tModLoader");
		// Nitrate
		taskButtons[buttonDiffNitrate] = () => new DiffTask(this, "src/staging/tModLoader", "src/staging/Nitrate", "patches/Nitrate");
		taskButtons[buttonPatchNitrate] = () => new PatchTask(this, "src/staging/tModLoader", "src/staging/Nitrate", "patches/Nitrate");
		
		taskButtons[buttonRegenerateSource] = () =>
		{
			return new RegenSourceTask(
				this,
				new[] { buttonPatchTerraria, buttonPatchTerrariaNetCore, buttonPatchModLoader, buttonPatchNitrate, }
					.Select(b => taskButtons[b]()).ToArray()
			);
		};
		
		taskButtons[buttonSetup] = () =>
		{
			return new SetupTask(
				this,
				new[] { buttonDecompile, buttonRegenerateSource, }
					.Select(b => taskButtons[b]()).ToArray()
			);
		};
#endregion
		
		SetPatchMode(Settings.Default.PatchMode);
		formatDecompiledOutputToolStripMenuItem.Checked = Settings.Default.FormatAfterDecompiling;
		
		Closing += (_, args) =>
		{
			if (!buttonCancel.Enabled)
			{
				return;
			}
			
			cancelSource.Cancel();
			args.Cancel = true;
			closeOnCancel = true;
		};
	}
	
#region ITaskInterface implementation
	public CancellationToken CancellationToken => cancelSource.Token;
	
	public void SetMaxProgress(int value)
	{
		Invoke(
			() =>
			{
				progressBar.Maximum = value;
			}
		);
	}
	
	public void SetStatus(string value)
	{
		Invoke(
			() =>
			{
				labelStatus.Text = value;
			}
		);
	}
	
	public void SetProgress(int value)
	{
		Invoke(
			() =>
			{
				progressBar.Value = value;
			}
		);
	}
#endregion
	
	private void buttonCancel_Click(object sender, EventArgs e)
	{
		cancelSource.Cancel();
	}
	
	private void menuItemTerraria_Click(object sender, EventArgs e)
	{
		SelectAndSetTerrariaDirectoryDialog();
	}
	
	private void menuItemDecompileServer_Click(object sender, EventArgs e)
	{
		RunTask(new DecompileTask(this, "src/staging/decompiled_server", true));
	}
	
	private void menuItemFormatCode_Click(object sender, EventArgs e)
	{
		RunTask(new FormatTask(this));
	}
	
	private void menuItemHookGen_Click(object sender, EventArgs e)
	{
		RunTask(new HookGenTask(this));
	}
	
	private void simplifierToolStripMenuItem_Click(object sender, EventArgs e)
	{
		RunTask(new SimplifierTask(this));
	}
	
	private void buttonTask_Click(object sender, EventArgs e)
	{
		RunTask(taskButtons[(Button)sender]());
	}
	
	private void RunTask(SetupOperation task)
	{
		cancelSource = new CancellationTokenSource();
		foreach (var b in taskButtons.Keys)
		{
			b.Enabled = false;
		}
		
		buttonCancel.Enabled = true;
		
		new Thread(() => RunTaskThread(task)).Start();
	}
	
	private void RunTaskThread(SetupOperation task)
	{
		var errorLogFile = Path.Combine(LOGS_DIR, "error.log");
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
				
				if (cancelSource.IsCancellationRequested)
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
			
			SetupOperation.CreateDirectory(LOGS_DIR);
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
		Settings.Default.PatchMode = mode;
		Settings.Default.Save();
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
	
	private void formatDecompiledOutputToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Settings.Default.FormatAfterDecompiling ^= true;
		Settings.Default.Save();
		formatDecompiledOutputToolStripMenuItem.Checked = Settings.Default.FormatAfterDecompiling;
	}
	
	private void mainMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }
	
	private void toolTipButtons_Popup(object sender, PopupEventArgs e) { }
	
	private void menuItemTmlPath_Click(object sender, EventArgs e)
	{
		SelectTmlDirectoryDialog();
	}
}
