using Nitrate.Config;

namespace Nitrate.API.Config;

/// <summary>
///     Configuration for <see cref="NitrateMod"/>.
///     <br />
///     In an effort to stay mostly decoupled from tModLoader, the actual
///     implementation that deals with retrieving and saving configuration
///     values is abstracted away, only being accessed through properties.
/// </summary>
public interface IConfiguration
{
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
}