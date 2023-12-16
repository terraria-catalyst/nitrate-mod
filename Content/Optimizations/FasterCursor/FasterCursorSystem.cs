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
    private static FnaVector2 CursorOrigin;
    private static RenderTarget2D CursorTarget = null!;
    private static SdlCursorHandle? CursorHandle;
    private static bool InExistingCursorContext;
    private static bool EnteredFromDrawThickCursor;
    private static SpriteBatchUtil.SpriteBatchSnapshot Snapshot;

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
                cursor_width,
                cursor_height
            );
        });
    }

    private static void UpdateAndSetCursor(On_Main.orig_Draw_Inner orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);

        if (!Enabled)
        {
            Main.instance.IsMouseVisible = false;

            return;
        }

        Main.instance.IsMouseVisible = true;

        byte[] pixelParts = new byte[cursor_width * cursor_height * 4];
        CursorTarget.GetData(pixelParts);

        SdlCursorHandle handle = SdlCursorHandle.FromPixels(pixelParts, cursor_width, cursor_height, CursorOrigin);
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

        if (!InExistingCursorContext)
        {
            InExistingCursorContext = true;

            (int realX, int realY) = (Main.mouseX, Main.mouseY);
            (Main.mouseX, Main.mouseY) = (0, 0);

            using (_ = Main.spriteBatch.BeginDrawingToRenderTarget(Main.graphics.graphicsDevice, CursorTarget))
                orig(bonus, smart);

            (Main.mouseX, Main.mouseY) = (realX, realY);

            InExistingCursorContext = false;
            EnteredFromDrawThickCursor = false;
        }
        else if (EnteredFromDrawThickCursor)
        {
            orig(bonus, smart);
            
            Main.spriteBatch.End();
            Main.instance.GraphicsDevice.SetRenderTarget(null);

            Main.spriteBatch.Begin(
                Snapshot.SortMode,
                Snapshot.BlendState,
                Snapshot.SamplerState,
                Snapshot.DepthStencilState,
                Snapshot.RasterizerState,
                Snapshot.Effect,
                Snapshot.TransformMatrix
            );
        }
        else
        {
            orig(bonus, smart);
        }
    }

    private static Vector2 CaptureDrawnThickCursor(On_Main.orig_DrawThickCursor orig, bool smart)
    {
        if (!Enabled)
        {
            return orig(smart);
        }

        if (!InExistingCursorContext)
        {
            InExistingCursorContext = true;
            EnteredFromDrawThickCursor = true;

            (int realX, int realY) = (Main.mouseX, Main.mouseY);
            (Main.mouseX, Main.mouseY) = (0, 0);

            Main.spriteBatch.TryEnd(out Snapshot);

            Main.instance.GraphicsDevice.SetRenderTarget(CursorTarget);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(
                Snapshot.SortMode,
                Snapshot.BlendState,
                Snapshot.SamplerState,
                Snapshot.DepthStencilState,
                Snapshot.RasterizerState,
                Snapshot.Effect,
                Snapshot.TransformMatrix
            );

            CursorOrigin = orig(smart);

            (Main.mouseX, Main.mouseY) = (realX, realY);

            // InExistingCursorContext = false;
        }
        else
        {
            CursorOrigin = orig(smart);
        }

        return CursorOrigin;
    }

    private static void CaptureInterface36Cursor(On_Main.orig_DrawInterface_36_Cursor orig)
    {
        if (!Enabled)
        {
            orig();

            return;
        }

        if (!InExistingCursorContext)
        {
            InExistingCursorContext = true;

            (int realX, int realY) = (Main.mouseX, Main.mouseY);
            (Main.mouseX, Main.mouseY) = (0, 0);

            using (_ = Main.spriteBatch.BeginDrawingToRenderTarget(Main.graphics.graphicsDevice, CursorTarget))
                orig();

            (Main.mouseX, Main.mouseY) = (realX, realY);

            InExistingCursorContext = false;
        }
        else
        {
            orig();
        }
    }
}