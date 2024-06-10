#nullable enable

using System;
using System.Globalization;

using Terraria.Nitrate.Localization;

namespace Terraria.Localization;

partial class GameCulture
{
	/// <summary>
	///		The actual name of the language in itself.
	/// </summary>
	public string LanguageName { get; }
	
	/// <summary>
	///		The English name for this language.
	/// </summary>
	/// <remarks>
	///		Also guarantees that this name is renderable in the English
	///		language.
	/// </remarks>
	public string EnglishName { get; }
	
	/// <summary>
	///		The string ID for this language on the Steam Workshop, if applicable.
	/// </summary>
	public string? WorkshopName { get; }
	
	/// <summary>
	///		Initializes a new game culture.
	/// </summary>
	public GameCulture(string languageCode, string languageName, string englishName, string? workshopName)
		: this(languageCode, languageName, englishName, workshopName, -1) { }
	
	internal GameCulture(string languageCode, string languageName, string englishName, string? workshopName, int legacyId)
	{
		CultureInfo = new CultureInfo(languageCode);
		LanguageName = languageName;
		EnglishName = englishName;
		WorkshopName = workshopName;
		
#pragma warning disable CS0618 // Type or member is obsolete
		LegacyId = legacyId;
#pragma warning restore CS0618 // Type or member is obsolete
	}
	
	/// <summary>
	///		Defines behavior for matching a given quantity with a cardinal
	///		plural rule ID.
	/// </summary>
	public virtual int CardinalPluralRule(int count)
	{
		return 0;
	}
	
	/// <summary>
	///		Gets the preferred formatting for date-time strings.
	/// </summary>
	public virtual string FormatDateTime(DateTime dateTime)
	{
		return dateTime.ToString("d MMMM yyyy");
	}
	
	/// <summary>
	///		Retrieves a game culture instance from a string ID
	///		(<paramref name="name"/>).
	/// </summary>
	/// <remarks>
	///		If no language is found, returns the default language.
	/// </remarks>
	public static GameCulture FromName(string name)
	{
		return Languages.FromCodeOrDefault(name);
	}
}
