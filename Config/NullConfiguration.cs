using Nitrate.API.Config;

namespace Nitrate.Config;

internal sealed class NullConfiguration : IConfiguration
{
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
}