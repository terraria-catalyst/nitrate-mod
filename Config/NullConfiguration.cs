namespace Nitrate.Config;

public sealed class NullConfiguration : IConfiguration
{
    public bool UsesExperimentalTileRenderer {
        get => false;
        set { }
    }

    public bool DisabledExperimentalTileRendererWarning {
        get => false;
        set { }
    }
}