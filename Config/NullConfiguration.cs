namespace Nitrate.Config;

public sealed class NullConfiguration : IConfiguration
{
    bool IConfiguration.UsesExperimentalTileRenderer {
        get => false;
        set { }
    }

    bool IConfiguration.DisabledExperimentalTileRendererWarning {
        get => false;
        set { }
    }
}