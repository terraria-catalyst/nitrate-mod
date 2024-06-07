using System.Collections.Generic;

namespace Terraria.ModLoader.Setup.Common;

public interface ISettingsManager
{
	/// <summary>
	///		Retrieves settings.
	/// </summary>
	/// <typeparam name="T">The settings type.</typeparam>
	/// <returns>
	///		The settings instance.
	/// </returns>
	T Get<T>();
	
	/// <summary>
	///		Sets settings.
	/// </summary>
	/// <param name="settings">Settings object instance.</param>
	/// <typeparam name="T">Settings type.</typeparam>
	void Set<T>(T settings);
	
	/// <summary>
	///		Loads settings.
	/// </summary>
	/// <param name="path">The path to load settings from.</param>
	void Load(string path);
	
	/// <summary>
	///		Saves settings.
	/// </summary>
	void Save();
}

/// <summary>
///		Default implementation of <see cref="ISettingsManager"/>.
/// </summary>
public sealed class SettingsManager : ISettingsManager
{
	private readonly Dictionary<string, object> knownSettings = new();
	private string? settingsPath;
	
	public T Get<T>()
	{
		return (T)knownSettings[typeof(T).FullName!];
	}
	
	public void Set<T>(T settings)
	{
		knownSettings[typeof(T).FullName!] = settings!;
	}
	
	public void Load(string path)
	{
		settingsPath = path;
		Settings.LoadSettings(path, knownSettings);
	}
	
	public void Save()
	{
		Settings.SaveSettings(settingsPath!, knownSettings);
	}
}
