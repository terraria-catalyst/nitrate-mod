using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using ReLogic.Graphics;
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

        const float small_text_scale = 0.75f;

        Rectangle mainBox = new(padding, padding, 425, (int)(charHeight + charHeight * small_text_scale * 3));
        ModContent.GetInstance<BoxRenderer>().DrawBox(Main.spriteBatch, mainBox);

        const float title_version_spacing = 8f;
        const string nitrate_title = "Nitrate";
        string nitrateVersion = $"v{ModContent.GetInstance<NitrateMod>().Version} :)";
        drawText(nitrate_title, new Vector2(padding, padding), Color.White, 0f, Vector2.Zero, Vector2.One);
        drawText(nitrateVersion, new Vector2(padding + font.MeasureString(nitrate_title).X + title_version_spacing, padding + charHeight * ((1f - small_text_scale) / 2f)), Color.White, 0f, Vector2.Zero, new Vector2(small_text_scale));

        const string nitrate_warning = "Please bear in mind that Nitrate is still under active development.\nFeatures may be broken, tweaked, or removed/reworked entirely.\nPlease report any issues and keep in mind that your game may break!";
        drawText(nitrate_warning, new Vector2(padding, padding + charHeight), Color.PaleVioletRed, 0f, Vector2.Zero, new Vector2(small_text_scale));

        Rectangle giveUsMoneyBox = new(padding, mainBox.Y + mainBox.Height + 6 + padding, 208, (int)(charHeight + (charHeight * small_text_scale)));
        ModContent.GetInstance<BoxRenderer>().DrawBox(Main.spriteBatch, giveUsMoneyBox);

        const string give_us_money = "Consider supporting us!";
        const string condescending = ";)";
        drawText(give_us_money, new Vector2(padding, giveUsMoneyBox.Y), Color.White, 0f, Vector2.Zero, Vector2.One);
        drawText(condescending, new Vector2(padding + font.MeasureString(give_us_money).X + title_version_spacing, giveUsMoneyBox.Y + charHeight * ((1f - small_text_scale) / 2f)), Color.White, 0f, Vector2.Zero, new Vector2(small_text_scale));
        drawText("[c/FF424D:Patreon]: patreon.com/tomatophile", new Vector2(padding, giveUsMoneyBox.Y + charHeight), Color.White, 0f, Vector2.Zero, new Vector2(small_text_scale));

        return;

        void drawText(string text, Vector2 position, Color baseColor, float rotation, Vector2 origin, Vector2 baseScale)
        {
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, position, baseColor, rotation, origin, baseScale);
        }
    }
}