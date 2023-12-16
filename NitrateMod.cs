using JetBrains.Annotations;
using System;
using System.Numerics;
using Terraria.ModLoader;

namespace Nitrate;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public sealed class NitrateMod : Mod
{
    internal const string PATREON = "patreon.com/TeamCatalyst";

    public override void Load()
    {
        base.Load();

        Logger.Info("              .......              ");
        Logger.Info("             .........             ");
        Logger.Info("            ...........            ");
        Logger.Info("            ...........            ");
        Logger.Info("             .........             ");
        Logger.Info("              .......              ");
        Logger.Info("                ...                ");
        Logger.Info("            ....   ....            ");
        Logger.Info("           ...       ...           ");
        Logger.Info("          ..           ..          ");
        Logger.Info("          ..           ..          ");
        Logger.Info("          ..           ..          ");
        Logger.Info("           ..         ..           ");
        Logger.Info("    ............   ............    ");
        Logger.Info("  ..........   .....   ..........  ");
        Logger.Info(" ............         ............ ");
        Logger.Info(" ............         ............ ");
        Logger.Info("  ..........           ..........  ");
        Logger.Info("    ......               ......    ");
        Logger.Info("Thank you for using Nitrate!");
        Logger.Info("Nitrate is free and open-source software, available @ https://github.com/terraria-catalyst/Nitrate under the GNU Affero General Public License, version 3.");
        Logger.Info("tModLoader makes it difficult to distribute licenses, you may view a copy @ https://github.com/terraria-catalyst/Nitrate/blob/master/LICENSE.txt.");
        Logger.Info("Supports SIMD: " + Vector.IsHardwareAccelerated);
        Logger.Info(".NET Version: " + Environment.Version);
        Logger.Info("OS: " + Environment.OSVersion);
        Logger.Info("Architecture: " + (Environment.Is64BitOperatingSystem ? "x64" : "x86"));
    }
}