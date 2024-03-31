using System;
using System.Collections.Generic;
using Terraria.Localization;

namespace Terraria.Nitrate.Localization;

#pragma warning disable CS0618 // Type or member is obsolete - CultureName is used for legacy support.
internal static class Languages
{
	private static readonly Dictionary<string, GameCulture> cultures = new();
	internal static readonly Dictionary<GameCulture.CultureName, GameCulture> namedCultures = new();

	public static readonly GameCulture en_US =		MakeCulture("en-US",	"English (United States)",	"English (United States)",	CommonOneMany,	legacyId: 1, workshopName: "english");
	public static readonly GameCulture de_DE =		MakeCulture("de-DE",	"Deutsch",					"German",					 CommonOneMany,	legacyId: 2, workshopName: "german");
	public static readonly GameCulture it_IT =		MakeCulture("it-IT",	"Italiano",					"Italian",					CommonOneMany,	legacyId: 3, workshopName: "italian");
	public static readonly GameCulture fr_FR =		MakeCulture("fr-FR",	"Français",					"French",					French,			legacyId: 4, workshopName: "french");
	public static readonly GameCulture es_ES =		MakeCulture("es-ES",	"Español",					"Spanish",					CommonOneMany,	legacyId: 5, workshopName: "spanish");
	public static readonly GameCulture ru_RU =		MakeCulture("ru-RU",	"Русский",					"Russian",					Russian,		legacyId: 6, workshopName: "russian");
	public static readonly GameCulture zh_Hans =	MakeCulture("zh-Hans",	"简体中文",					"Chinese (Simplified)",		CommonOther,	legacyId: 7, workshopName: "schinese");
	public static readonly GameCulture pt_BR =		MakeCulture("pt-BR",	"Português (Brasil)",		"Portuguese (Brazil)",		CommonOneMany,	legacyId: 8, workshopName: "portuguese");
	public static readonly GameCulture pl_PL =		MakeCulture("pl-PL",	"Polski",					"Polish",					Polish,			legacyId: 9, workshopName: "polish");

	public static GameCulture Default => en_US;

	public static IEnumerable<GameCulture> GetCultures() => cultures.Values;

	public static GameCulture FromCodeOrDefault(string languageCode)
	{
		return cultures.TryGetValue(languageCode, out var culture) ? culture : Default;
	}

	private static GameCulture MakeCulture(string languageCode, string languageName, string englishName, Func<int, int, int, int> cardinalPluralRule, int legacyId = 0, string workshopName = null)
	{
		var culture = new GameCulture(languageCode, languageName, englishName, legacyId, workshopName, AsCardinal(cardinalPluralRule));
		cultures[languageCode] = culture;

		if (legacyId > 0)
			namedCultures[(GameCulture.CultureName)legacyId] = culture;

		return culture;
	}

	#region Plural Rules
	private static Func<int, int> AsCardinal(Func<int, int, int, int> rule)
	{
		return count => rule(count, count % 10, count % 100);
	}

	private static bool Contains(int i, int a, int b) => i >= a && i <= b;

	private static int Russian(int count, int mod_i10, int mod_i100)
	{
		if (mod_i10 == 1 && mod_i100 != 11)
			return 0;

		if (Contains(mod_i10, 2, 4) && !Contains(mod_i100, 12, 14))
			return 1;

		return 2;
	}

	private static int CommonOneMany(int count, int mod_i10, int mod_i100)
	{
		return count == 1 ? 0 : 1;
	}

	private static int French(int count, int mod_i10, int mod_i100)
	{
		return count == 0 || count == 1 ? 0 : 1;
	}

	private static int Polish(int count, int mod_i10, int mod_i100)
	{
		if (count == 1)
			return 0;

		if (Contains(mod_i10, 2, 4) && !Contains(mod_i100, 12, 14))
			return 1;

		return 2;
	}

	private static int CommonOther(int count, int mod_i10, int mod_i100)
	{
		return 0;
	}
	#endregion
}
#pragma warning restore CS0618 // Type or member is obsolete
