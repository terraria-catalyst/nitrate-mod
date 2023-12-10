using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Nitrate.Core.Features.UI;

public interface IBoxRenderer
{
    void DrawBox(SpriteBatch spriteBatch, Rectangle box, Color? color = null, bool isPaddingPartOfRectangle = false);
}