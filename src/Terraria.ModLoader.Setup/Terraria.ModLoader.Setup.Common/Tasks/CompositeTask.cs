using System.Linq;

namespace Terraria.ModLoader.Setup.Common.Tasks;

public abstract class CompositeTask(CommonContext ctx, params SetupOperation[] tasks) : SetupOperation(ctx)
{
	public SetupOperation[] Tasks { get; set; } = tasks;
	
	private SetupOperation? failed;
	
	public override bool ConfigurationDialog()
	{
		return Tasks.All(task => task.ConfigurationDialog());
	}
	
	public override bool Failed()
	{
		return failed != null;
	}
	
	public override void FinishedDialog()
	{
		if (failed != null)
		{
			failed.FinishedDialog();
		}
		else
		{
			foreach (var task in Tasks)
			{
				task.FinishedDialog();
			}
		}
	}
	
	public override void Run()
	{
		foreach (var task in Tasks)
		{
			task.Run();
			if (!task.Failed())
			{
				continue;
			}
			
			failed = task;
			return;
		}
	}
}
