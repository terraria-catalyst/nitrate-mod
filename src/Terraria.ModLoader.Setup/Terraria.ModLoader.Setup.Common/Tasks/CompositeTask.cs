using System.Linq;

namespace Terraria.ModLoader.Setup.Common.Tasks;

public abstract class CompositeTask(ITaskInterface taskInterface, params SetupOperation[] tasks) : SetupOperation(taskInterface)
{
	private SetupOperation? failed;
	
	public override bool ConfigurationDialog()
	{
		return tasks.All(task => task.ConfigurationDialog());
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
			foreach (var task in tasks)
			{
				task.FinishedDialog();
			}
		}
	}
	
	public override void Run()
	{
		foreach (var task in tasks)
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
