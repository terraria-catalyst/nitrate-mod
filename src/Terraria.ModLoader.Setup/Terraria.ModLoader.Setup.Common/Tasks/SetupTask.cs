namespace Terraria.ModLoader.Setup.Common.Tasks;

public sealed class SetupTask(ITaskInterface taskInterface, params SetupOperation[] tasks) : CompositeTask(taskInterface, tasks)
{
	public override bool StartupWarning()
	{
		var res = taskInterface.ShowDialogWithOkFallback(
			"Ready for Setup",
			"Any changes in src/staging/ will be lost.\r\n",
			SetupMessageBoxButtons.OkCancel,
			SetupMessageBoxIcon.Information
		);
		
		return res == SetupDialogResult.Ok;
	}
}
