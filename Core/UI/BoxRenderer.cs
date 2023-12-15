using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace Nitrate.Core.UI;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class BoxRenderer : ModSystem, IBoxRenderer
{
    private IBoxRenderer? _renderer;

    public override void Load()
    {
        base.Load();

        _renderer = new VanillaBoxRenderer(Mod.Assets.Request<Texture2D>("Assets/UI/Box", AssetRequestMode.ImmediateLoad));
    }

    public void DrawBox(SpriteBatch spriteBatch, Rectangle box, Color? color = null, bool isPaddingPartOfRectangle = false)
    {
        _renderer?.DrawBox(spriteBatch, box, color, isPaddingPartOfRectangle);
    }
}