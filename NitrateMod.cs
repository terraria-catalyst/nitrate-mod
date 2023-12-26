using JetBrains.Annotations;
using Nitrate.Config;
using System;
using System.Numerics;
using Terraria.ModLoader;

namespace Nitrate;

/// <summary>
///     The main <see cref="Mod"/> implementation of Nitrate.
/// </summary>
/// <remarks>
///     Logic is largely kept out of this class, and should instead be dealt
///     with appropriately in other tModLoader APIs such as
///     <see cref="ModSystem"/>.
/// </remarks>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public sealed class NitrateMod : Mod
{
    /// <summary>
    ///     The Patreon link for Team Catalyst, Nitrate's development team.
    /// </summary>
    public const string PATREON = "patreon.com/TeamCatalyst";

    public static IConfiguration Configuration => ModContent.GetInstance<NitrateMod>().configuration;

    private IConfiguration configuration = new ModConfigConfiguration();

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

    public override void Unload()
    {
        base.Unload();

        configuration = IConfiguration.NULL;
    }
}