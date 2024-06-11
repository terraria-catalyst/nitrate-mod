namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate;

/// <summary>
///		Composite task which applies automated intermediary Nitrate patches
///		before then applying explicitly-defined user patches.
/// </summary>
public sealed class NitrateTask(CommonContext ctx, CommonContext.NitratePatchContext nitratePatchContext)
	: CompositeTask(ctx, nitratePatchContext.AllOperations)
{
	public override bool ConfigurationDialog()
	{
		var res = Context.IsAutomatic
			? SetupDialogResult.Yes
			: Context.TaskInterface.ShowDialogWithOkFallback(
				"Additional Nitrate Setup",
				"Run ALL setup operations to prepare the Nitrate-specific development environment? This includes running various formatting steps on the vanilla/tModLoader source code, which takes a long time. These steps are reproduced client-side to avoid large patches.",
				SetupMessageBoxButtons.YesNo,
				SetupMessageBoxIcon.Question
			);

		var runAllSteps = res == SetupDialogResult.Ok;
		var runExtraAnalysisSteps = false;
		if (!runAllSteps)
		{
			res = Context.TaskInterface.ShowDialogWithOkFallback(
				"Additional Nitrate Setup",
				"Run only setup operations applied AFTER standard formatting applications? This includes advanced analyzers for intelligently formatting Terraria-specific code. This option exists mostly for development purposes when working on the setup tool. These steps are reproduced client-side to avoid large patches.",
				SetupMessageBoxButtons.YesNo,
				SetupMessageBoxIcon.Question
			);

			runExtraAnalysisSteps = res == SetupDialogResult.Ok;
		}

		if (runAllSteps)
		{
			Tasks = nitratePatchContext.AllOperations;
		}
		else if (runExtraAnalysisSteps)
		{
			Tasks = nitratePatchContext.PostAnalysisOperations;
		}
		else
		{
			Tasks = [nitratePatchContext.PatchOperation,];
		}

		return base.ConfigurationDialog();
	}
}
