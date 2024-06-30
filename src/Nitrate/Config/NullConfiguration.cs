using TeamCatalyst.Nitrate.API.Config;

namespace TeamCatalyst.Nitrate.Config;

internal sealed class NullConfiguration : IConfiguration {
    bool IConfiguration.UsesExperimentalTileRenderer {
        get => false;
        set { }
    }

    bool IConfiguration.DisabledExperimentalTileRendererWarning {
        get => false;
        set { }
    }

    bool IConfiguration.UsesNewLaserRulerRendering {
        get => false;
        set { }
    }

    bool IConfiguration.UsesAsyncSceneMetrics {
        get => false;
        set { }
    }

    bool IConfiguration.UsesFasterPylonSystem {
        get => false;
        set { }
    }
}
