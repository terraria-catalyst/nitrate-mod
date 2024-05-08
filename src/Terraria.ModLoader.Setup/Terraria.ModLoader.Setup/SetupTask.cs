using System.Windows.Forms;

using Terraria.ModLoader.Setup.Common;

namespace Terraria.ModLoader.Setup;

internal sealed class SetupTask(ITaskInterface taskInterface, params SetupOperation[] tasks) : CompositeTask(taskInterface, tasks)
{
	public override bool StartupWarning()
	{
		var res = MessageBox.Show(
			"Any changes in src/staging/ will be lost.\r\n",
			"Ready for Setup",
			MessageBoxButtons.OKCancel,
			MessageBoxIcon.Information
		);
		
		return res == DialogResult.OK;
	}
}
