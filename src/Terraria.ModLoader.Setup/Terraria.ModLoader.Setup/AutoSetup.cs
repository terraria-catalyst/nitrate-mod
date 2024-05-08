using System;
using System.IO;
using System.Linq;
using System.Threading;

using Terraria.ModLoader.Setup.Common;

namespace Terraria.ModLoader.Setup;

internal sealed class AutoSetup : ITaskInterface
{
	private CancellationTokenSource cancelSource;
	
	public CancellationToken CancellationToken => cancelSource.Token;
	
	public object Invoke(Delegate action)
	{
		return action.DynamicInvoke();
	}
	
	private int max = 1;
	
	public void SetMaxProgress(int value)
	{
		max = value;
	}
	
	public void SetProgress(int progress)
	{
		Console.WriteLine("Value: {0:P2}.", (float)progress / max);
	}
	
	public void SetStatus(string status)
	{
		Console.WriteLine(status);
	}
	
	public void DoAuto()
	{
		Func<SetupOperation> buttonDecompile = () => new DecompileTask(this, "src/staging/decompiled");
		Func<SetupOperation> buttonPatchTerraria = () => new PatchTask(this, "src/staging/decompiled", "src/staging/Terraria", "patches/Terraria");
		Func<SetupOperation> buttonPatchTerrariaNetCore = () => new PatchTask(this, "src/staging/Terraria", "src/staging/TerrariaNetCore", "patches/TerrariaNetCore");
		Func<SetupOperation> buttonPatchModLoader = () => new PatchTask(this, "src/staging/TerrariaNetCore", "src/staging/tModLoader", "patches/tModLoader");
		Func<SetupOperation> buttonPatchNitrate = () => new PatchTask(this, "src/staging/tModLoader", "src/staging/Nitrate", "patches/Nitrate");
		
		Func<SetupOperation> buttonRegenSource = () =>
			new RegenSourceTask(
				this,
				new[] { buttonPatchTerraria, buttonPatchTerrariaNetCore, buttonPatchModLoader, buttonPatchNitrate, }
					.Select(b => b()).ToArray()
			);
		
		Func<SetupOperation> task = () =>
			new SetupTask(
				this,
				new[] { buttonDecompile, buttonRegenSource, }
					.Select(b => b()).ToArray()
			);
		
		if (Directory.Exists("src/staging/decompiled"))
		{
			Console.WriteLine("decompiled folder found, skipping decompile step");
			task = () => new SetupTask(this, buttonRegenSource());
		}
		
		cancelSource = new CancellationTokenSource();
		DoAuto2(task());
	}
	
	public void DoAuto2(SetupOperation task)
	{
		var errorLogFile = Path.Combine(Program.LOGS_DIR, "error.log");
		try
		{
			SetupOperation.DeleteFile(errorLogFile);
			
			if (!task.ConfigurationDialog())
			{
				return;
			}
			
			try
			{
				task.Run();
				
				if (cancelSource.IsCancellationRequested)
				{
					throw new OperationCanceledException();
				}
			}
			catch (OperationCanceledException)
			{
				return;
			}
			
			if (task.Failed() || task.Warnings())
			{
				task.FinishedDialog();
			}
		}
		catch (Exception e)
		{
			SetStatus(e.Message);
			Environment.Exit(1);
		}
	}
}