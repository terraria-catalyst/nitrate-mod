using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Nitrate.Core.Features.UI;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class MainMenuRenderer : ModSystem
{
    public override void Load()
    {
        base.Load();

        On_Main.DrawSocialMediaButtons += DrawNitrateStuff;
    }

    private static void DrawNitrateStuff(On_Main.orig_DrawSocialMediaButtons orig, Color menuColor, float upBump)
    {
        orig(menuColor, upBump);

        ModContent.GetInstance<BoxRenderer>().DrawBox(Main.spriteBatch, new Rectangle(12, 12, 50, 50));
    }
}