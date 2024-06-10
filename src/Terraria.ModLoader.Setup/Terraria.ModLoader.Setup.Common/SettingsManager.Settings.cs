using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Terraria.ModLoader.Setup.Common;

public sealed class PatchSettings
{
	public int PatchMode { get; set; }
}

public sealed class TerrariaPathSettings
{
	public string TerrariaSteamDirectory { get; set; } = string.Empty;

	public string TmlDeveloperSteamDirectory { get; set; } = string.Empty;
}

public static class Settings
{
	public static void InitializeSettings(ITaskInterface taskInterface)
	{
		Directory.CreateDirectory(CommonSetup.SETTINGS_DIR);

		// Set initial setting values.
		{
			taskInterface.Settings.Set(
				new PatchSettings
				{
					PatchMode = 0,
				}
			);

			taskInterface.Settings.Set(new TerrariaPathSettings());
		}

		// Load settings from file if it exists and save it and defaults.
		{
			taskInterface.Settings.Load(CommonSetup.SETTINGS_PATH);
			taskInterface.Settings.Save();
		}
	}

	public static void LoadSettings(string path, Dictionary<string, object> settings)
	{
		if (!File.Exists(path))
		{
			return;
		}

		var settingsJson = File.ReadAllText(path);
		var settingsDict = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(settingsJson);

		if (settingsDict is null)
		{
			return;
		}

		foreach (var (settingTypeName, settingObject) in settingsDict)
		{
			var settingType = Type.GetType(settingTypeName);
			if (settingType is null)
			{
				continue;
			}

			var setting = settingObject.ToObject(settingType);
			if (setting is null)
			{
				continue;
			}

			settings[settingTypeName] = setting;
		}
	}

	public static void SaveSettings(string path, Dictionary<string, object> settings)
	{
		var settingsDict = new Dictionary<string, JObject>();
		foreach (var (settingTypeName, setting) in settings)
		{
			settingsDict[settingTypeName] = JObject.FromObject(setting);
		}

		var settingsJson = JsonConvert.SerializeObject(settingsDict, Formatting.Indented);
		File.WriteAllText(path, settingsJson);
	}
}
