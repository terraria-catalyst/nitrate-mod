using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace Nitrate.UI;

internal sealed class VanillaBoxRenderer : IBoxRenderer
{
    private const int padding = 6;
    private const int content = 4;
    private static readonly Rectangle center = new(padding, padding, content, content);
    private static readonly Rectangle top_left_corner = new(0, 0, padding, padding);
    private static readonly Rectangle top_right_corner = new(padding + content, 0, padding, padding);
    private static readonly Rectangle bottom_left_corner = new(0, padding + content, padding, padding);
    private static readonly Rectangle bottom_right_corner = new(padding + content, padding + content, padding, padding);
    private static readonly Rectangle top_edge = new(padding, 0, content, padding);
    private static readonly Rectangle bottom_edge = new(padding, padding + content, content, padding);
    private static readonly Rectangle left_edge = new(0, padding, padding, content);
    private static readonly Rectangle right_edge = new(padding + content, padding, padding, content);
    private static readonly Color default_color = new Color(63, 82, 151) * 0.7f;

    private readonly Asset<Texture2D> boxTexture;

    public VanillaBoxRenderer(Asset<Texture2D> boxTexture)
    {
        this.boxTexture = boxTexture;
    }

    public void DrawBox(SpriteBatch spriteBatch, Rectangle box, Color? color = null, bool isPaddingPartOfRectangle = false)
    {
        color ??= default_color;

        Rectangle inner = box;

        if (isPaddingPartOfRectangle)
        {
            inner.Inflate(-padding, -padding);
        }
        else
        {
            box.Inflate(padding, padding);
        }

        spriteBatch.Draw(boxTexture.Value, inner, center, color.Value);

        spriteBatch.Draw(boxTexture.Value, new Rectangle(box.X + padding, box.Y, box.Width - padding * 2, padding), top_edge, color.Value);
        spriteBatch.Draw(boxTexture.Value, new Rectangle(box.X + padding, box.Y + box.Height - padding, box.Width - padding * 2, padding), bottom_edge, color.Value);
        spriteBatch.Draw(boxTexture.Value, new Rectangle(box.X, box.Y + padding, padding, box.Height - padding * 2), left_edge, color.Value);
        spriteBatch.Draw(boxTexture.Value, new Rectangle(box.X + box.Width - padding, box.Y + padding, padding, box.Height - padding * 2), right_edge, color.Value);

        spriteBatch.Draw(boxTexture.Value, new Rectangle(box.X, box.Y, padding, padding), top_left_corner, color.Value);
        spriteBatch.Draw(boxTexture.Value, new Rectangle(box.X + box.Width - padding, box.Y, padding, padding), top_right_corner, color.Value);
        spriteBatch.Draw(boxTexture.Value, new Rectangle(box.X, box.Y + box.Height - padding, padding, padding), bottom_left_corner, color.Value);
        spriteBatch.Draw(boxTexture.Value, new Rectangle(box.X + box.Width - padding, box.Y + box.Height - padding, padding, padding), bottom_right_corner, color.Value);
    }
}