namespace Terraria.ModLoader.Setup.Common.Tasks;

public sealed class RegenSourceTask(ITaskInterface taskInterface, params SetupOperation[] tasks) : CompositeTask(taskInterface, tasks)
{
	public override bool StartupWarning()
	{
		SetupDialogResult res;
		if (taskInterface.Settings.Get<PatchSettings>().PatchMode != 2)
		{
			res = taskInterface.ShowDialogWithOkFallback(
				"Ready for Setup",
				"Any changes in src/staging/ will be lost.\r\n",
				SetupMessageBoxButtons.OkCancel,
				SetupMessageBoxIcon.Information
			);
			
			return res == SetupDialogResult.Ok;
		}
		
		res = taskInterface.ShowDialogWithOkFallback(
			"Strict Patch Mode",
			"Patch mode will be reset from fuzzy to offset.\r\n",
			SetupMessageBoxButtons.OkCancel,
			SetupMessageBoxIcon.Information
		);
		
		if (res != SetupDialogResult.Ok)
		{
			return false;
		}
		
		res = taskInterface.ShowDialogWithOkFallback(
			"Ready for Setup",
			"Any changes in src/staging/ will be lost.\r\n",
			SetupMessageBoxButtons.OkCancel,
			SetupMessageBoxIcon.Information
		);
		
		return res == SetupDialogResult.Ok;
	}
	
	public override void Run()
	{
		if (taskInterface.Settings.Get<PatchSettings>().PatchMode == 2)
		{
			taskInterface.Settings.Get<PatchSettings>().PatchMode = 1;
			taskInterface.Settings.Save();
		}
		
		base.Run();
	}
}
