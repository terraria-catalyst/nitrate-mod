using System.Collections.Generic;

using Terraria.Localization;

namespace Terraria.Nitrate.Localization;

#pragma warning disable CS0618 // Type or member is obsolete
internal static class Languages
{
	private static readonly Dictionary<string, GameCulture> cultures = new();
	public static readonly Dictionary<GameCulture.CultureName, GameCulture> NAMED_CULTURES = new();
	
	public  static GameCulture Default => EN_US;
	
	public static IEnumerable<GameCulture> GetCultures()
	{
		return cultures.Values;
	}
	
	public static GameCulture FromCodeOrDefault(string languageCode)
	{
		return cultures.TryGetValue(languageCode, out var culture) ? culture : Default;
	}
}
#pragma warning restore CS0618 // Type or member is obsolete
