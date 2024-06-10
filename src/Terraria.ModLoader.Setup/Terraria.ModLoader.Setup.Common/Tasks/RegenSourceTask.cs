namespace Terraria.ModLoader.Setup.Common.Tasks;

public sealed class RegenSourceTask(CommonContext ctx, params SetupOperation[] tasks) : CompositeTask(ctx, tasks)
{
	public override bool StartupWarning()
	{
		SetupDialogResult res;
		if (Context.Settings.Get<PatchSettings>().PatchMode != 2)
		{
			res = Context.TaskInterface.ShowDialogWithOkFallback(
				"Ready for Setup",
				"Any changes in src/staging/ will be lost.\r\n",
				SetupMessageBoxButtons.OkCancel,
				SetupMessageBoxIcon.Information
			);

			return res == SetupDialogResult.Ok;
		}

		res = Context.TaskInterface.ShowDialogWithOkFallback(
			"Strict Patch Mode",
			"Patch mode will be reset from fuzzy to offset.\r\n",
			SetupMessageBoxButtons.OkCancel,
			SetupMessageBoxIcon.Information
		);

		if (res != SetupDialogResult.Ok)
		{
			return false;
		}

		res = Context.TaskInterface.ShowDialogWithOkFallback(
			"Ready for Setup",
			"Any changes in src/staging/ will be lost.\r\n",
			SetupMessageBoxButtons.OkCancel,
			SetupMessageBoxIcon.Information
		);

		return res == SetupDialogResult.Ok;
	}

	public override void Run()
	{
		if (Context.Settings.Get<PatchSettings>().PatchMode == 2)
		{
			Context.Settings.Get<PatchSettings>().PatchMode = 1;
			Context.Settings.Save();
		}

		base.Run();
	}
}
