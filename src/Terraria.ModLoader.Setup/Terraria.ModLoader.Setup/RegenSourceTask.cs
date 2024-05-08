using System.Windows.Forms;

using Terraria.ModLoader.Setup.Properties;

using Settings = Terraria.ModLoader.Setup.Properties.Settings;

namespace Terraria.ModLoader.Setup;

internal sealed class RegenSourceTask(ITaskInterface taskInterface, params SetupOperation[] tasks) : CompositeTask(taskInterface, tasks)
{
	public override bool StartupWarning()
	{
		DialogResult res;
		if (Settings.Default.PatchMode != 2)
		{
			res = MessageBox.Show(
				"Any changes in src/staging/ will be lost.\r\n",
				"Ready for Setup",
				MessageBoxButtons.OKCancel,
				MessageBoxIcon.Information
			);
			
			return res == DialogResult.OK;
		}
		
		res = MessageBox.Show(
			"Patch mode will be reset from fuzzy to offset.\r\n",
			"Strict Patch Mode",
			MessageBoxButtons.OKCancel,
			MessageBoxIcon.Information
		);
		
		if (res != DialogResult.OK)
		{
			return false;
		}
		
		res = MessageBox.Show(
			"Any changes in src/staging/ will be lost.\r\n",
			"Ready for Setup",
			MessageBoxButtons.OKCancel,
			MessageBoxIcon.Information
		);
		
		return res == DialogResult.OK;
	}
	
	public override void Run()
	{
		if (Settings.Default.PatchMode == 2)
		{
			Settings.Default.PatchMode = 1;
			Settings.Default.Save();
		}
		
		base.Run();
	}
}
