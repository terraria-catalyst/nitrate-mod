using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

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

        const int padding = 14;

        DynamicSpriteFont font = FontAssets.MouseText.Value;
        float charHeight = font.MeasureString("A").Y;

        const float title_version_spacing = 8f;
        const float small_text_scale = 0.75f;
        const string nitrate_title = "Nitrate";
        string nitrateVersion = $"v{ModContent.GetInstance<NitrateMod>().Version} :)";

        ModContent.GetInstance<BoxRenderer>().DrawBox(Main.spriteBatch, new Rectangle(padding, padding, 425, (int)(charHeight + charHeight * small_text_scale * 3)));

        ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, nitrate_title, new Vector2(padding, padding), Color.White, 0f, Vector2.Zero, Vector2.One);
        ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, nitrateVersion, new Vector2(padding + font.MeasureString(nitrate_title).X + title_version_spacing, padding + charHeight * ((1f - small_text_scale) / 2f)), Color.White, 0f, Vector2.Zero, new Vector2(small_text_scale));

        ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, "Please bear in mind that Nitrate is still under active development.\nFeatures may be broken, tweaked, or removed/reworked entirely.\nPlease report any issues and keep in mind that your game may break!",
            new Vector2(padding, padding + charHeight), Color.PaleVioletRed, 0f, Vector2.Zero, new Vector2(small_text_scale));
    }
}