using System.Windows.Forms;

namespace Terraria.ModLoader.Setup;

internal sealed class SetupTask(ITaskInterface taskInterface, params SetupOperation[] tasks) : CompositeTask(taskInterface, tasks)
{
	public override bool StartupWarning()
	{
		var res = MessageBox.Show(
			"Any changes in /src will be lost.\r\n",
			"Ready for Setup",
			MessageBoxButtons.OKCancel,
			MessageBoxIcon.Information
		);
		
		return res == DialogResult.OK;
	}
}
