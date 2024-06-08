namespace Terraria.ModLoader.Setup.Common.Tasks;

public sealed class SetupTask(CommonContext ctx, params SetupOperation[] tasks) : CompositeTask(ctx, tasks)
{
	public override bool StartupWarning()
	{
		var res = Context.TaskInterface.ShowDialogWithOkFallback(
			"Ready for Setup",
			"Any changes in src/staging/ will be lost.\r\n",
			SetupMessageBoxButtons.OkCancel,
			SetupMessageBoxIcon.Information
		);
		
		return res == SetupDialogResult.Ok;
	}
}
