using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Nitrate.Core.UI;

/// <summary>
///     Renders boxes.
/// </summary>
[ApiReleaseCandidate("1.0.0")]
internal interface IBoxRenderer
{
    /// <summary>
    ///     Draws a box at the given coordinates with the given width and
    ///     height.
    /// </summary>
    /// <param name="spriteBatch">
    ///     The <see cref="SpriteBatch"/> to draw with.
    /// </param>
    /// <param name="box">The box to draw.</param>
    /// <param name="color">The color of the box.</param>
    /// <param name="isPaddingPartOfRectangle">
    ///     Whether the box padding (arbitrarily decided by any renderer) is
    ///     inclusive or exclusive to the given box width and height.
    ///     Alternatively: whether the box outline is within the given box
    ///     width and height, or outside of it.
    /// </param>
    void DrawBox(SpriteBatch spriteBatch, Rectangle box, Color? color = null, bool isPaddingPartOfRectangle = false);
}