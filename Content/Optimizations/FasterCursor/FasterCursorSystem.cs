using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nitrate.Core.Utilities;
using Terraria;
using Terraria.ModLoader;

namespace Nitrate.Content.Optimizations.FasterCursor;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class FasterCursorSystem : ModSystem
{
    // Cursor texture is 14px, arbitrarily multiply by 5 to account for UI zoom
    // and oscillating mouse scaling.
    private const int cursor_width = 14 * 5;
    private const int cursor_height = 14 * 5;
    private static FnaVector2 CursorPosition;
    private static FnaVector2 CursorOrigin;
    private static RenderTarget2D CursorTarget = null!;
    private static SdlCursorHandle? CursorHandle;

    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    private static bool Enabled => CursorTarget is not null && !(Main.gameMenu && Main.alreadyGrabbingSunOrMoon) && ModContent.GetInstance<NitrateConfig>().FasterCursor;

    public override void OnModLoad()
    {
        base.OnModLoad();

        On_Main.DrawCursor += CaptureDrawnCursor;
        On_Main.DrawThickCursor += CaptureDrawnThickCursor;
        On_Main.DrawInterface_36_Cursor += CaptureInterface36Cursor;
        On_Main.Draw_Inner += UpdateAndSetCursor;

        Main.QueueMainThreadAction(() =>
        {
            CursorTarget = new RenderTarget2D(
                Main.graphics.GraphicsDevice,
                Main.screenWidth,
                Main.screenHeight
            );
        });

        Main.OnResolutionChanged += UpdateRenderTargets;
    }

    public override void OnModUnload()
    {
        base.OnModUnload();

        Main.OnResolutionChanged -= UpdateRenderTargets;
    }

    private static void UpdateRenderTargets(Vector2 _)
    {
        CursorTarget?.Dispose();

        CursorTarget = new RenderTarget2D(
            Main.graphics.GraphicsDevice,
            Main.screenWidth,
            Main.screenHeight
        );
    }

    private static void UpdateAndSetCursor(On_Main.orig_Draw_Inner orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);

        if (!Enabled)
        {
            return;
        }

        Color[] pixels = new Color[CursorTarget.Width * CursorTarget.Height];
        CursorTarget.GetData(pixels);

        FnaVector2 start = CursorPosition - CursorOrigin;
        FnaVector2 end = start + new FnaVector2(cursor_width, cursor_height);
        Color[] cursorPixels = new Color[cursor_width * cursor_height];

        // copy the rect start -> end from pixels to cursorPixels
        for (int y = (int)start.Y; y < end.Y; y++)
        {
            for (int x = (int)start.X; x < end.X; x++)
            {
                int cursorIndex = (y - (int)start.Y) * cursor_width + (x - (int)start.X);
                int index = y * CursorTarget.Width + x;

                cursorPixels[cursorIndex] = pixels[index];
            }
        }

        SdlCursorHandle handle = SdlCursorHandle.FromPixels(cursorPixels, cursor_width, cursor_height, CursorOrigin);
        handle.SetSdlCursor();

        CursorHandle?.Dispose();
        CursorHandle = handle;
    }

    private static void CaptureDrawnCursor(On_Main.orig_DrawCursor orig, FnaVector2 bonus, bool smart)
    {
        if (!Enabled)
        {
            orig(bonus, smart);

            return;
        }

        CursorPosition = Main.MouseScreen;

        using (_ = Main.spriteBatch.BeginDrawingToRenderTarget(Main.graphics.graphicsDevice, CursorTarget))
            orig(bonus, smart);
    }

    private static Vector2 CaptureDrawnThickCursor(On_Main.orig_DrawThickCursor orig, bool smart)
    {
        if (!Enabled)
        {
            return orig(smart);
        }

        using (_ = Main.spriteBatch.BeginDrawingToRenderTarget(Main.graphics.graphicsDevice, CursorTarget))
            CursorOrigin = orig(smart);

        CursorPosition = Main.MouseScreen;

        return CursorOrigin;
    }

    private static void CaptureInterface36Cursor(On_Main.orig_DrawInterface_36_Cursor orig)
    {
        if (!Enabled)
        {
            orig();

            return;
        }

        using (_ = Main.spriteBatch.BeginDrawingToRenderTarget(Main.graphics.graphicsDevice, CursorTarget))
            orig();

        CursorPosition = Main.MouseScreen;
    }
}