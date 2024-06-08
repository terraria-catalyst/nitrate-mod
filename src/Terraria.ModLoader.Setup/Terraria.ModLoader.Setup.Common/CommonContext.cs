using System.IO;

namespace Terraria.ModLoader.Setup.Common;

public sealed class CommonContext(ITaskInterface taskInterface)
{
	public ITaskInterface TaskInterface { get; } = taskInterface;
	
	public IProgressManager Progress => TaskInterface.Progress;
	
	public ISettingsManager Settings => TaskInterface.Settings;
	
	public bool IsAutomatic { get; init; }
	
	private string? terrariaSteamDirectory;
	
	public string TerrariaSteamDirectory
	{
		get => IsAutomatic ? terrariaSteamDirectory ??= TaskInterface.Settings.Get<TerrariaPathSettings>().TerrariaSteamDirectory : TaskInterface.Settings.Get<TerrariaPathSettings>().TerrariaSteamDirectory;
		
		set
		{
			if (IsAutomatic)
			{
				terrariaSteamDirectory = value;
			}
			else
			{
				TaskInterface.Settings.Get<TerrariaPathSettings>().TerrariaSteamDirectory = value;
			}
		}
	}
	
	private string? tmlDeveloperSteamDirectory;
	
	public string TmlDeveloperSteamDirectory
	{
		get => IsAutomatic ? tmlDeveloperSteamDirectory ??= TaskInterface.Settings.Get<TerrariaPathSettings>().TmlDeveloperSteamDirectory : TaskInterface.Settings.Get<TerrariaPathSettings>().TmlDeveloperSteamDirectory;
		
		set
		{
			if (IsAutomatic)
			{
				tmlDeveloperSteamDirectory = value;
			}
			else
			{
				TaskInterface.Settings.Get<TerrariaPathSettings>().TmlDeveloperSteamDirectory = value;
			}
		}
	}
	
	public string TerrariaPath => Path.Combine(TerrariaSteamDirectory, "Terraria.exe");
	
	public string TerrariaServerPath => Path.Combine(TerrariaSteamDirectory, "TerrariaServer.exe");
}
