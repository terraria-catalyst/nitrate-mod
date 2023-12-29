using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Nitrate.Optimizations.UI;

/// <summary>
///     Replaces the default laser ruler rendering system with a much more optimized one.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class NewLaserRulerRenderSystem : ModSystem
{
    public override void OnModLoad()
    {
        base.OnModLoad();

        IL_Main.DrawInterface_3_LaserRuler += RenderLaserRuler;
    }

    // TODO: See if colors can be cleaned up. The red outlined section just looks a little... off... 
    /// <summary>
    ///     The vanilla laser ruler UI renderer renders every tile individually.
    ///     For a full-screen game, this means upwards of 14,000
    ///     <see cref="Main.Draw"/> calls.
    /// <br />
    ///     Instead of rendering every tile individually, this renders the ruler
    ///     as a sequence of large shapes.
    ///     <br />
    ///     In the same screen size, this reduces the Draw calls to ~200.
    /// </summary>
    private static void RenderLaserRuler(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.After, instr => instr.MatchBrfalse(out ILLabel _));
        cursor.GotoNext(MoveType.After, instr => instr.MatchBrfalse(out ILLabel _));
        cursor.Index++;

        cursor.EmitDelegate<Func<bool>>(() =>
        {
            if (!Configuration.UsesNewLaserRulerRendering)
            {
                return false;
            }

            float scaleX = (Main.screenWidth + 100F) / 16F;
            float scaleY = (Main.screenHeight + 100F) / 16F;

            float num = Main.LocalPlayer.velocity.Length();
            const float num2 = 6f;
            float num3 = MathHelper.Lerp(0.2f, 0.7f, MathHelper.Clamp(1f - num / num2, 0f, 1f));
            Color colorBackground = new Color(0.24f, 0.8f, 0.9f, 1.0f) * 0.125F * num3;
            Color colorLines = new Color(0.24f, 0.8f, 0.9f, 1.0f) * 0.25F * num3;
            Main.spriteBatch.Draw(TextureAssets.BlackTile.Value, new Vector2(0, 0), null, colorBackground, 0F, Vector2.Zero, new Vector2(scaleX, scaleY), SpriteEffects.None, 0F);

            int screenTileWidth = (Main.screenWidth + 100) / 16;
            int screenTileHeight = (Main.screenHeight + 100) / 16;

            Vector2 vec = Main.screenPosition;
            vec += new Vector2(-50f);
            vec = vec.ToTileCoordinates().ToVector2() * 16f;

            // Draw lines across X axis.
            for (int x = 0; x < screenTileWidth; x++)
            {
                Main.spriteBatch.Draw(TextureAssets.BlackTile.Value,
                    Main.ReverseGravitySupport(new Vector2(x, 0) * 16F - Main.screenPosition + vec - Vector2.One, 16F),
                    new Rectangle(0, 0, 2, 16),
                    colorLines,
                    0F,
                    Vector2.Zero,
                    new Vector2(1F, scaleY),
                    SpriteEffects.None,
                    0F);
            }

            // Draw lines across Y axis.
            for (int y = 0; y < screenTileHeight; y++)
            {
                Main.spriteBatch.Draw(TextureAssets.BlackTile.Value,
                    Main.ReverseGravitySupport(new Vector2(0, y) * 16F - Main.screenPosition + vec - Vector2.One, 16F),
                    new Rectangle(0, 0, 16, 2),
                    colorLines,
                    0F,
                    Vector2.Zero,
                    new Vector2(scaleX, 1F),
                    SpriteEffects.None,
                    0F);
            }

            Point point = Main.MouseWorld.ToTileCoordinates();
            point.X -= (int)vec.X / 16;
            point.Y -= (int)vec.Y / 16;
            Color mouseHoverColor = new Color(1f, 0.1f, 0.1f, 0.0F) * 0.25F * num3;

            float mouseScaleY = point.Y;
            float mouseScaleYReversed = screenTileHeight - point.Y;

            // Draw 1st Y red column
            Main.spriteBatch.Draw(TextureAssets.BlackTile.Value,
                Main.ReverseGravitySupport(new Vector2(point.X, 0F) * 16F - Main.screenPosition + vec - Vector2.One, 16F),
                new Rectangle(0, 0, 18, 16),
                mouseHoverColor,
                0F,
                Vector2.Zero,
                new Vector2(1F, mouseScaleY),
                SpriteEffects.None,
                0F
            );

            // Draw X red column
            Main.spriteBatch.Draw(TextureAssets.BlackTile.Value,
                Main.ReverseGravitySupport(new Vector2(0F, point.Y) * 16F - Main.screenPosition + vec - Vector2.One, 16F),
                new Rectangle(0, 0, 16, 18),
                mouseHoverColor,
                0F,
                Vector2.Zero,
                new Vector2(scaleX, 1F),
                SpriteEffects.None,
                0F
            );

            // Draw 2nd Y red column
            Main.spriteBatch.Draw(TextureAssets.BlackTile.Value,
                Main.ReverseGravitySupport(new Vector2(point.X, point.Y + 1.125F) * 16F - Main.screenPosition + vec - Vector2.One, 16F),
                new Rectangle(0, 0, 18, 16),
                mouseHoverColor,
                0F,
                Vector2.Zero,
                new Vector2(1F, mouseScaleYReversed),
                SpriteEffects.None,
                0F
            );

            return true;
        });

        ILLabel label = cursor.DefineLabel();
        cursor.Emit(OpCodes.Brfalse, label);
        cursor.Emit(OpCodes.Ret);
        cursor.MarkLabel(label);
    }
}