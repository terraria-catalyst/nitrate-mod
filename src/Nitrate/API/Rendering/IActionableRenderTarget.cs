using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Nitrate.API.Rendering;

/// <summary>
///     An actionable render target used in the
///     <see cref="ActionableRenderTargetSystem"/>. May hold a list of actions
///     to be executed in the context of the render target.
/// </summary>
public interface IActionableRenderTarget : IDisposable {
    /// <summary>
    ///     The list of actions to be executed in the context of the render
    ///     target.
    /// </summary>
    List<Action> Actions { get; }

    /// <summary>
    ///     The render target.
    /// </summary>
    RenderTarget2D RenderTarget { get; }

    /// <summary>
    ///     Finishes the render target, disposing of any resources.
    /// </summary>
    void Finish();

    /// <summary>
    ///     Reinitializes the <see cref="IActionableRenderTarget"/> for a
    ///     screen resize.
    /// </summary>
    IActionableRenderTarget ReinitForResize();
}
