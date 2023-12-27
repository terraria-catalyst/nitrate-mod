using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nitrate.UI;
using ReLogic.Content;
using Terraria.ModLoader;

namespace Nitrate.API.UI;

/// <summary>
///     Provides access to an abstracted implementation of
///     <see cref="IBoxRenderer"/> to allow for user configuration/consistency.
/// </summary>
/// <remarks>
///     Inheritance from <see cref="ModSystem"/> is not an API guarantee but
///     rather an implementation detail. Implementation of
///     <see cref="IBoxRenderer"/> is not an API guarantee but rather an
///     implementation detail, you should use the static implementations of the
///     various methods instead.
/// </remarks>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public sealed class BoxRenderer : ModSystem, IBoxRenderer
{
    private IBoxRenderer? renderer;

    public override void Load()
    {
        base.Load();

        renderer = new VanillaBoxRenderer(Mod.Assets.Request<Texture2D>("Assets/UI/Box", AssetRequestMode.ImmediateLoad));
    }

    void IBoxRenderer.DrawBox(SpriteBatch spriteBatch, Rectangle box, Color? color, bool isPaddingPartOfRectangle)
    {
        renderer?.DrawBox(spriteBatch, box, color, isPaddingPartOfRectangle);
    }

    public static void DrawBox(SpriteBatch spriteBatch, Rectangle box, Color? color = null, bool isPaddingPartOfRectangle = false)
    {
        ((IBoxRenderer) ModContent.GetInstance<BoxRenderer>()).DrawBox(spriteBatch, box, color, isPaddingPartOfRectangle);
    }
}