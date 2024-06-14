#nullable enable

using System;

using Terraria.Localization;

namespace Terraria.Nitrate.Localization;

/// <summary>
///		Defines well-known game cultures.
/// </summary>
public static class WellKnownLanguages
{
#region Culture object definitions
	/// <summary>
	///		A legacy (vanilla) game culture.
	/// </summary>
	private abstract class LegacyCulture(string languageCode, string languageName, string englishName, string? workshopName, int legacyId)
		: GameCulture(languageCode, languageName, englishName, workshopName, legacyId)
	{
		public sealed override int CardinalPluralRule(int count)
		{
			return CardinalPluralRule(count, count % 10, count % 100);
		}

		protected abstract int CardinalPluralRule(int count, int mod10, int mod100);

		protected static bool Contains(int i, int a, int b)
		{
			return i >= a && i <= b;
		}

		protected static int CommonOneMany(int count)
		{
			return count == 1 ? 0 : 1;
		}
	}

	private sealed class EnUsCulture() : LegacyCulture("en-US", "English (United States)", "English (United States)", "english", 1)
	{
		protected override int CardinalPluralRule(int count, int mod10, int mod100)
		{
			return CommonOneMany(count);
		}

		public override string FormatDateTime(DateTime dateTime)
		{
			return dateTime.ToShortDateString();
		}
	}

	private sealed class DeDeCulture() : LegacyCulture("de-DE", "Deutsch", "German", "german", 2)
	{
		protected override int CardinalPluralRule(int count, int mod10, int mod100)
		{
			return CommonOneMany(count);
		}
	}

	private sealed class ItItCulture() : LegacyCulture("it-IT", "Italiano", "Italian", "italian", 3)
	{
		protected override int CardinalPluralRule(int count, int mod10, int mod100)
		{
			return CommonOneMany(count);
		}
	}

	private sealed class FrFrCulture() : LegacyCulture("fr-FR", "Français", "French", "french", 4)
	{
		protected override int CardinalPluralRule(int count, int mod10, int mod100)
		{
			return count is 0 or 1 ? 0 : 1;
		}
	}

	private sealed class EsEsCulture() : LegacyCulture("es-ES", "Español", "Spanish", "spanish", 5)
	{
		protected override int CardinalPluralRule(int count, int mod10, int mod100)
		{
			return CommonOneMany(count);
		}
	}

	private sealed class RuRuCulture() : LegacyCulture("ru-RU", "Русский", "Russian", "russian", 6)
	{
		protected override int CardinalPluralRule(int count, int mod10, int mod100)
		{
			if (mod10 == 1 && mod100 != 11)
			{
				return 0;
			}

			if (Contains(mod10, 2, 4) && !Contains(mod100, 12, 14))
			{
				return 1;
			}

			return 2;
		}
	}

	private sealed class ZhHansCulture() : LegacyCulture("zh-Hans", "简体中文", "Chinese (Simplified)", "schinese", 7)
	{
		protected override int CardinalPluralRule(int count, int mod10, int mod100)
		{
			return 0;
		}
	}

	private sealed class PtBrCulture() : LegacyCulture("pt-BR", "Português (Brasil)", "Portuguese (Brazil)", "portuguese", 8)
	{
		protected override int CardinalPluralRule(int count, int mod10, int mod100)
		{
			return CommonOneMany(count);
		}
	}

	private sealed class PlPlCulture() : LegacyCulture("pl-PL", "Polski", "Polish", "polish", 9)
	{
		protected override int CardinalPluralRule(int count, int mod10, int mod100)
		{
			if (count == 1)
			{
				return 0;
			}

			if (Contains(mod10, 2, 4) && !Contains(mod100, 12, 14))
			{
				return 1;
			}

			return 2;
		}
	}
#endregion

	/// <summary>
	///		English (United States)
	/// </summary>
	public static readonly GameCulture EN_US = new EnUsCulture();

	/// <summary>
	///		German (Germany)
	/// </summary>
	public static readonly GameCulture DE_DE = new DeDeCulture();

	/// <summary>
	///		Italian (Italy)
	/// </summary>
	public static readonly GameCulture IT_IT = new ItItCulture();

	/// <summary>
	///		French (France)
	/// </summary>
	public static readonly GameCulture FR_FR = new FrFrCulture();

	/// <summary>
	///		Spanish (Spain)
	/// </summary>
	public static readonly GameCulture ES_ES = new EsEsCulture();

	/// <summary>
	///		Russian (Russia)
	/// </summary>
	public static readonly GameCulture RU_RU = new RuRuCulture();

	/// <summary>
	///		Chinese (Simplified)
	/// </summary>
	public static readonly GameCulture ZH_HANS = new ZhHansCulture();

	/// <summary>
	///		Portuguese (Brazil)
	/// </summary>
	public static readonly GameCulture PT_BR = new PtBrCulture();

	/// <summary>
	///		Polish (Poland)
	/// </summary>
	public static readonly GameCulture PL_PL = new PlPlCulture();

	static WellKnownLanguages()
	{
		Languages.CULTURES[EN_US.CultureInfo.Name] = EN_US;
		Languages.CULTURES[DE_DE.CultureInfo.Name] = DE_DE;
		Languages.CULTURES[IT_IT.CultureInfo.Name] = IT_IT;
		Languages.CULTURES[FR_FR.CultureInfo.Name] = FR_FR;
		Languages.CULTURES[ES_ES.CultureInfo.Name] = ES_ES;
		Languages.CULTURES[RU_RU.CultureInfo.Name] = RU_RU;
		Languages.CULTURES[ZH_HANS.CultureInfo.Name] = ZH_HANS;
		Languages.CULTURES[PT_BR.CultureInfo.Name] = PT_BR;
		Languages.CULTURES[PL_PL.CultureInfo.Name] = PL_PL;

#pragma warning disable CS0618 // Type or member is obsolete
		Languages.NAMED_CULTURES[GameCulture.CultureName.English] = EN_US;
		Languages.NAMED_CULTURES[GameCulture.CultureName.German] = DE_DE;
		Languages.NAMED_CULTURES[GameCulture.CultureName.Italian] = FR_FR;
		Languages.NAMED_CULTURES[GameCulture.CultureName.French] = ES_ES;
		Languages.NAMED_CULTURES[GameCulture.CultureName.Spanish] = RU_RU;
		Languages.NAMED_CULTURES[GameCulture.CultureName.Russian] = ZH_HANS;
		Languages.NAMED_CULTURES[GameCulture.CultureName.Chinese] = PT_BR;
		Languages.NAMED_CULTURES[GameCulture.CultureName.Polish] = PL_PL;
#pragma warning restore CS0618 // Type or member is obsolete
	}
}
