using System.IO;

namespace Terraria.ModLoader.Setup.Common;

/// <summary>
///		Common (shared) context for a task interface lifetime.
/// </summary>
public sealed class CommonContext(ITaskInterface taskInterface)
{
	public ITaskInterface TaskInterface { get; } = taskInterface;

	public IProgressManager Progress => TaskInterface.Progress;

	public ISettingsManager Settings => TaskInterface.Settings;

	/// <summary>
	///		Whether this is an automated setup.
	/// </summary>
	public bool IsAutomatic { get; init; }

	/// <summary>
	///		The branch being used.
	/// </summary>
	public string Branch { get; set; } = "";

	private string? terrariaSteamDirectory;

	/// <summary>
	///		The path to Terraria's Steam directory.
	/// </summary>
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

	/// <summary>
	///		The path to the Terraria.ModLoader (tModLoader) development Steam
	///		directory.
	/// </summary>
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

	/// <summary>
	///		The path to Terraria's executable.
	/// </summary>
	public string TerrariaPath => Path.Combine(TerrariaSteamDirectory, "Terraria.exe");

	/// <summary>
	///		The path to Terraria's server executable.
	/// </summary>
	public string TerrariaServerPath => Path.Combine(TerrariaSteamDirectory, "TerrariaServer.exe");
}
