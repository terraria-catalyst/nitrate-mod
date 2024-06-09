using System;
using System.Linq;

using Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate;

/// <summary>
///		Composite task which applies automated intermediary Nitrate patches
///		before then applying explicitly-defined user patches.
/// </summary>
public sealed class NitrateTask(CommonContext ctx, string baseDir, string patchedDir, string patchDir, out string pathToUseForDiffing)
	: CompositeTask(ctx, GetOperations(ctx, baseDir, patchedDir, patchDir, out pathToUseForDiffing))
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
		
		Tasks = res switch
		{
			SetupDialogResult.Yes => GetOperations(Context, baseDir, patchedDir, patchDir, out _),
			SetupDialogResult.No => [GetOperations(Context, baseDir, patchedDir, patchDir, out _).Last(),],
			_ => Tasks,
		};
		
		return base.ConfigurationDialog();
	}
	
	public static SetupOperation[] GetOperations(CommonContext ctx, string baseDir, string patchedDir, string patchDir, out string pathToUseForDiffing)
	{
		var operations = new SetupOperation[] { patch<OrganizePartialClasses>(), patch<MakeTypesPartial>(), patch<TreeshakePreprocessors>(), patch<FormatWithEditorConfig>(), new PatchTask(ctx, baseDir, patchedDir, patchDir), };
		pathToUseForDiffing = baseDir;
		return operations;
		
		T patch<T>() where T : SetupOperation
		{
			return (T)Activator.CreateInstance(typeof(T), ctx, baseDir, baseDir = patchedDir + '_' + typeof(T).Name)!;
		}
	}
}
