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
using Nitrate.Core.Utilities;

namespace Nitrate.Core.UI;

/// <summary>
///     Handles rendering UI to the main menu.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class MainMenuRenderer : ModSystem
{
    private static GameTime? gameTime;

    private UserInterface UserInterface { get; } = new();

    public override void Load()
    {
        base.Load();

        On_Main.DrawMenu += CaptureGameTime;
        On_Main.DrawSocialMediaButtons += DrawNitrateStuff;

        // UserInterface.SetState(new MainMenuPanels());
    }

    private static void CaptureGameTime(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
    {
        MainMenuRenderer.gameTime = gameTime;
        orig(self, gameTime);
    }

    private void DrawNitrateStuff(On_Main.orig_DrawSocialMediaButtons orig, Color menuColor, float upBump)
    {
        orig(menuColor, upBump);

        DrawLegacyMenuStuff();

        if (gameTime is null)
        {
            return;
        }

        UserInterface.Update(gameTime);
        UserInterface.Draw(Main.spriteBatch, gameTime);
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

        string nitrateWarning = "Menu.NitrateWarning".LocalizeNitrate();
        drawText(nitrateWarning, new FnaVector2(padding, padding + charHeight), Color.PaleVioletRed, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));

        Rectangle giveUsMoneyBox = new(padding, mainBox.Y + mainBox.Height + 6 + padding, 425, (int)(charHeight + (charHeight * small_text_scale)));
        ModContent.GetInstance<BoxRenderer>().DrawBox(Main.spriteBatch, giveUsMoneyBox);

        string giveUsMoney = "Menu.GiveUsMoney".LocalizeNitrate();
        const string condescending = ";)";
        string patreon = "Menu.Patreon".LocalizeNitrate(PATREON);
        drawText(giveUsMoney, new FnaVector2(padding, giveUsMoneyBox.Y), Color.White, 0f, FnaVector2.Zero, FnaVector2.One);
        drawText(condescending, new FnaVector2(padding + font.MeasureString(giveUsMoney).X + title_version_spacing, giveUsMoneyBox.Y + charHeight * ((1f - small_text_scale) / 2f)), Color.White, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));
        drawText(patreon, new FnaVector2(padding, giveUsMoneyBox.Y + charHeight), Color.White, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));

        string ignore = "Menu.Ignore".LocalizeNitrate();
        const string clickable = PATREON;
        float ignoreWidth = font.MeasureString(ignore).X * small_text_scale;
        float clickableWidth = font.MeasureString(clickable).X * small_text_scale;
        float clickableHeight = charHeight * small_text_scale;
        Rectangle clickableBox = new((int)(padding + ignoreWidth), (int)(giveUsMoneyBox.Y + charHeight), (int)clickableWidth, (int)clickableHeight);

        if (Main.mouseLeft && Main.mouseLeftRelease && clickableBox.Intersects(new Rectangle(Main.mouseX, Main.mouseY, 1, 1)))
        {
            Utils.OpenToURL($"https://{PATREON}");
        }

        Rectangle debugBox = new(padding, giveUsMoneyBox.Y + giveUsMoneyBox.Height + 6 + padding, 425, (int)(charHeight * 4 * small_text_scale));
        ModContent.GetInstance<BoxRenderer>().DrawBox(Main.spriteBatch, debugBox);

        drawText("Menu.SIMD".LocalizeNitrate(Vector.IsHardwareAccelerated), new FnaVector2(padding, debugBox.Y), Color.White, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));
        drawText("Menu.NETVersion".LocalizeNitrate(Environment.Version), new FnaVector2(padding, debugBox.Y + charHeight * small_text_scale), Color.White, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));
        drawText("Menu.OS".LocalizeNitrate(Environment.OSVersion.ToString().Replace("Microsoft ", "")), new FnaVector2(padding, debugBox.Y + charHeight * small_text_scale * 2), Color.White, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));
        drawText("Menu.Architecture".LocalizeNitrate(Environment.Is64BitOperatingSystem ? "x64" : "x86"), new FnaVector2(padding, debugBox.Y + charHeight * small_text_scale * 3), Color.White, 0f, FnaVector2.Zero, new FnaVector2(small_text_scale));

        return;

        void drawText(string text, FnaVector2 position, Color baseColor, float rotation, FnaVector2 origin, FnaVector2 baseScale)
        {
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, position, baseColor, rotation, origin, baseScale);
        }
    }
}