using System.ComponentModel;
using JetBrains.Annotations;
using TeamCatalyst.Nitrate.API.Config;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace TeamCatalyst.Nitrate.Config;

internal sealed class ModConfigConfiguration : IConfiguration {
    /// <summary>
    ///     The <see cref="ModConfig"/> implementation of Nitrate's
    ///     <see cref="IConfiguration"/>.
    /// </summary>
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    private sealed class NitrateConfig : ModConfig {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [ReloadRequired]
        [DefaultValue(true)]
        [UsedImplicitly(ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign)]
        public bool ExperimentalTileRenderer { get; set; }

        [DefaultValue(false)]
        [UsedImplicitly(ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign)]
        public bool ExperimentalTileRendererWarning { get; set; }

        [DefaultValue(true)]
        [UsedImplicitly(ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign)]
        public bool UsesNewLaserRulerRendering { get; set; }
        
        [DefaultValue(true)]
        [UsedImplicitly(ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign)]
        public bool UsesAsyncSceneMetrics { get; set; }

        /*[DefaultValue(true)]
        [UsedImplicitly(ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign)]
        public bool FasterCursor { get; set; }*/
    }

    private static NitrateConfig Config => ModContent.GetInstance<NitrateConfig>();

    bool IConfiguration.UsesExperimentalTileRenderer {
        get => Config.ExperimentalTileRenderer;
        set => Config.ExperimentalTileRenderer = value;
    }

    bool IConfiguration.DisabledExperimentalTileRendererWarning {
        get => Config.ExperimentalTileRendererWarning;
        set => Config.ExperimentalTileRendererWarning = value;
    }

    bool IConfiguration.UsesNewLaserRulerRendering {
        get => Config.UsesNewLaserRulerRendering;
        set => Config.UsesNewLaserRulerRendering = value;
    }

    bool IConfiguration.UsesAsyncSceneMetrics {
        get => Config.UsesAsyncSceneMetrics;
        set => Config.UsesAsyncSceneMetrics = value;
    }
}
