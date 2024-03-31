namespace TeamCatalyst.Nitrate.Providers;

/// <summary>
///     Provides the path to the game's installation directory.
/// </summary>
internal interface IGameInstallPathProvider {
    /// <summary>
    ///     Gets the path to the game's installation directory.
    /// </summary>
    string GetGameInstallPath();
}

/// <summary>
///     Default implementation of <see cref="IGameInstallPathProvider"/>.
/// </summary>
internal sealed class GameInstallPathProvider : IGameInstallPathProvider {
    string IGameInstallPathProvider.GetGameInstallPath() {
        throw new System.NotImplementedException();
    }
}
