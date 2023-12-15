using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using ReLogic.Graphics;
using System;
using System.Numerics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Nitrate.Core.UI;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class MainMenuRenderer : ModSystem
{
    private static GameTime? GameTime;

    public UserInterface UserInterface { get; } = new();

    public override void Load()
    {
        base.Load();

        On_Main.DrawMenu += CaptureGameTime;
        On_Main.DrawSocialMediaButtons += DrawNitrateStuff;

        // UserInterface.SetState(new MainMenuPanels());
    }

    private static void CaptureGameTime(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
    {
        GameTime = gameTime;
        orig(self, gameTime);
    }

    private void DrawNitrateStuff(On_Main.orig_DrawSocialMediaButtons orig, Color menuColor, float upBump)
    {
        orig(menuColor, upBump);

        DrawLegacyMenuStuff();

        if (GameTime is null)
        {
            return;
        }

        UserInterface.Update(GameTime);
        UserInterface.Draw(Main.spriteBatch, GameTime);
    }

    private static void DrawLegacyMenuStuff()
    {
        const int padding = 14;

        DynamicSpriteFont font = FontAssets.MouseText.Value;
        float charHeight = font.MeasureString("A").Y;

        const float small_text_scale = 0.75f;

        Rectangle mainBox = new(padding, padding, 425, (int)(charHeight + charHeight * small_text_scale * 3));
        ModContent.GetInstance<BoxRenderer>().DrawBox(Main.spriteBatch, mainBox);

        const float title_version_spacing = 8f;
        const string nitrate_title = "Nitrate";
        string nitrateVersion = $"v{ModContent.GetInstance<NitrateMod>().Version} :)";
        drawText(nitrate_title, new FnaVector2(padding, padding), Color.White, 0f, FnaVector2.Zero, FnaVector2.One);
        drawText(nitrateVersion, new FnaVector2(padding + font.MeasureString(nitrate_title).X + title_version_spacing, padding + charHeight * ((1f - small_text_scale) / 2f)), Color.White, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));

        const string nitrate_warning = "Please bear in mind that Nitrate is still under active development.\nFeatures may be broken, tweaked, or removed/reworked entirely.\nPlease report any issues and keep in mind that your game may break!";
        drawText(nitrate_warning, new FnaVector2(padding, padding + charHeight), Color.PaleVioletRed, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));

        Rectangle giveUsMoneyBox = new(padding, mainBox.Y + mainBox.Height + 6 + padding, 425, (int)(charHeight + (charHeight * small_text_scale)));
        ModContent.GetInstance<BoxRenderer>().DrawBox(Main.spriteBatch, giveUsMoneyBox);

        const string give_us_money = "Consider supporting us!";
        const string condescending = ";)";
        const string patreon = $"[c/FF424D:Patreon]: [c/7289DA:{NitrateMod.PATREON}] <-- (clickable!)";
        drawText(give_us_money, new FnaVector2(padding, giveUsMoneyBox.Y), Color.White, 0f, FnaVector2.Zero, FnaVector2.One);
        drawText(condescending, new FnaVector2(padding + font.MeasureString(give_us_money).X + title_version_spacing, giveUsMoneyBox.Y + charHeight * ((1f - small_text_scale) / 2f)), Color.White, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));
        drawText(patreon, new FnaVector2(padding, giveUsMoneyBox.Y + charHeight), Color.White, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));

        const string ignore = "Patreon:" ;
        const string clickable = NitrateMod.PATREON;
        float ignoreWidth = font.MeasureString(ignore).X * small_text_scale;
        float clickableWidth = font.MeasureString(clickable).X * small_text_scale;
        float clickableHeight = charHeight * small_text_scale;
        Rectangle clickableBox = new((int)(padding + ignoreWidth), (int)(giveUsMoneyBox.Y + charHeight), (int)clickableWidth, (int)clickableHeight);

        if (Main.mouseLeft && Main.mouseLeftRelease && clickableBox.Intersects(new Rectangle(Main.mouseX, Main.mouseY, 1, 1)))
        {
            Utils.OpenToURL($"https://{NitrateMod.PATREON}");
        }

        Rectangle debugBox = new(padding, giveUsMoneyBox.Y + giveUsMoneyBox.Height + 6 + padding, 425, (int)(charHeight * 4 * small_text_scale));
        ModContent.GetInstance<BoxRenderer>().DrawBox(Main.spriteBatch, debugBox);

        drawText($"Supports SIMD: {Vector.IsHardwareAccelerated}", new FnaVector2(padding, debugBox.Y), Color.White, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));
        drawText($".NET Version: {Environment.Version}", new FnaVector2(padding, debugBox.Y + charHeight * small_text_scale), Color.White, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));
        drawText($"OS: {Environment.OSVersion.ToString().Replace("Microsoft ", "")}", new FnaVector2(padding, debugBox.Y + charHeight * small_text_scale * 2), Color.White, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));
        drawText($"Architecture: {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}", new FnaVector2(padding, debugBox.Y + charHeight * small_text_scale * 3), Color.White, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));

        void drawText(string text, FnaVector2 position, Color baseColor, float rotation, FnaVector2 origin, FnaVector2 baseScale)
        {
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, position, baseColor, rotation, origin, baseScale);
        }
    }
}