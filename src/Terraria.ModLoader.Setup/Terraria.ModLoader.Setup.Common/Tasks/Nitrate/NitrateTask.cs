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
				"Run additional Nitrate patch steps (initializes untracked intermediary projects)? This includes formatting and other automated processes that would produce large patches otherwise.",
				SetupMessageBoxButtons.YesNo,
				SetupMessageBoxIcon.Question
			);

		var runAllSteps = res == SetupDialogResult.Ok;
		var runExtraAnalysisSteps = false;
		if (!runAllSteps)
		{
			res = Context.TaskInterface.ShowDialogWithOkFallback(
				"Additional Nitrate Setup",
				"Run and apply additional analyzers performed after code formatting? This is for development purposes.",
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
