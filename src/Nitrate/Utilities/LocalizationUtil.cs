using Terraria.Localization;

namespace TeamCatalyst.Nitrate.Utilities;

internal static class LocalizationUtil {
    public static string Localize(this string key, params object?[] args) {
        return Language.GetTextValue(key, args);
    }

    public static string LocalizeNitrate(this string key, params object?[] args) {
        return Language.GetTextValue($"Mods.Nitrate.{key}", args);
    }
}
