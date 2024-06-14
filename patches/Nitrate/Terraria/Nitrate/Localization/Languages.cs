using System.Collections.Generic;

using Terraria.Localization;

namespace Terraria.Nitrate.Localization;

#pragma warning disable CS0618 // Type or member is obsolete
internal static class Languages
{
	public static readonly Dictionary<string, GameCulture> CULTURES = new();
	public static readonly Dictionary<GameCulture.CultureName, GameCulture> NAMED_CULTURES = new();

	public  static GameCulture Default => EN_US;

	public static IEnumerable<GameCulture> GetCultures()
	{
		return CULTURES.Values;
	}

	public static GameCulture FromCodeOrDefault(string languageCode)
	{
		return CULTURES.TryGetValue(languageCode, out var culture) ? culture : Default;
	}
}
#pragma warning restore CS0618 // Type or member is obsolete
