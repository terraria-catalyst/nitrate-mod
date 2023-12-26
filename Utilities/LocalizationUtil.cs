using Terraria.Localization;

namespace Nitrate.Utilities;

internal static class LocalizationUtil
{
    public static string Localize(this string key, params object?[] args) => Language.GetTextValue(key, args);

    public static string LocalizeNitrate(this string key, params object?[] args) => Language.GetTextValue($"Mods.Nitrate.{key}", args);
}