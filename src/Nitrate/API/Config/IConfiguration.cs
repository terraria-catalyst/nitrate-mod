using TeamCatalyst.Nitrate.Config;

namespace TeamCatalyst.Nitrate.API.Config;

/// <summary>
///     <see cref="NitrateMod"/> configuration.
/// </summary>
/// <remarks>
///     In an effort to remain decoupled from tModLoader and its configuration
///     system, the configuration is defined as an interface.
/// </remarks>
public interface IConfiguration {
    /// <summary>
    ///     A no-op, &quot;null&quot; configuration.
    /// </summary>
    public static readonly IConfiguration NULL = new NullConfiguration();

    /// <summary>
    ///     Whether the experimental tile renderer is in use.
    /// </summary>
    bool UsesExperimentalTileRenderer { get; set; }

    /// <summary>
    ///     Whether the warning for the experimental tile renderer has been
    ///     disabled.
    /// </summary>
    bool DisabledExperimentalTileRendererWarning { get; set; }

    /// <summary>
    ///     If the new laser ruler rendering system should be used over the vanilla one.
    /// </summary>
    bool UsesNewLaserRulerRendering { get; set; }
    
    /// <summary>
    ///     If the async scene metrics feature should be enabled
    /// </summary>
    bool UsesAsyncSceneMetrics { get; set; }
    
    /// <summary>
    ///     If the faster pylon system should be active
    /// </summary>
    bool UsesFasterPylonSystem { get; set; }
}
