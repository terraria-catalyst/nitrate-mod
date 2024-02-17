using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader;

namespace Nitrate.Optimizations.Tiles;

// TODO: Make sure this doesn't mess with mods.
/// <summary>
///     Speeds up rendering of special tiles such as grass, vines, etc.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class SpecialTileRenderSystem : ModSystem {
    public override void OnModLoad() {
        base.OnModLoad();

        IL_TileDrawing.DrawGrass += CullFarGrass;
        IL_TileDrawing.DrawVines += CullFarVines;
    }

    /// <summary>
    ///     Overrides the vanilla
    ///     <see cref="TileDrawing.DrawVines"/> method to cull vines far away
    ///     from the local player.
    ///     <br />
    ///     By doing this, we can prevent a large majority of the
    ///     <see cref="Main.Draw"/> calls it performs.
    ///     <br />
    ///     For now, culling is only performed on the X axis. This is done as
    ///     vines could (potentially) by drawn across the entire Y axis.
    ///     TODO Cull vines on Y axis as well.
    /// </summary>
    private static void CullFarVines(ILContext il) {
        ILCursor cursor = new(il);

        // Instead of overriding a specific part of the method like
        // CullFarGrass, this method was short enough to simply override the
        // entire thing.
        cursor.EmitDelegate(
            () => {
                var unscaledPosition = Main.Camera.UnscaledPosition;
                var zero = Vector2.Zero;
                var topLeftPos = Main.ViewPosition.ToTileCoordinates();
                topLeftPos.X -= 10;
                var bottomRightPos = (Main.ViewPosition + Main.ViewSize).ToTileCoordinates();
                bottomRightPos.X += 10;

                const int num = 6;
                var num2 = Main.instance.TilesRenderer._specialsCount[num];

                for (var i = 0; i < num2; i++) {
                    var point = Main.instance.TilesRenderer._specialPositions[num][i];
                    var x = point.X;
                    var y = point.Y;

                    if (x < topLeftPos.X || x > bottomRightPos.X) {
                        continue;
                    }

                    Main.instance.TilesRenderer.DrawVineStrip(unscaledPosition, zero, x, y);
                }
            }
        );

        cursor.Emit(OpCodes.Ret);
    }

    /// <summary>
    ///     Overrides the vanilla
    ///     <see cref="TileDrawing.DrawGrass"/> method to cull grass far away from the local player.
    ///     <br />
    ///     By doing this, we can prevent a large majority of the
    ///     <see cref="Main.Draw"/> calls it performs.
    /// </summary>
    private static void CullFarGrass(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNext(instr => instr.MatchLdelemAny<Point>());
        cursor.Index++;

        var continueLabel = il.DefineLabel();
        var finishLabel = il.DefineLabel();

        cursor.Emit(OpCodes.Dup);

        cursor.EmitDelegate<Func<Point, bool>>(
            point => {
                var topLeftPos = Main.ViewPosition.ToTileCoordinates();
                topLeftPos.X -= 2;
                topLeftPos.Y -= 2;
                var bottomRightPos = (Main.ViewPosition + Main.ViewSize).ToTileCoordinates();
                bottomRightPos.X += 2;
                bottomRightPos.Y += 2;

                return point.X >= topLeftPos.X && point.X <= bottomRightPos.X && point.Y >= topLeftPos.Y && point.Y <= bottomRightPos.Y;
            }
        );

        // This extra branch is necessary to reset the stack back down to what it should be.
        cursor.Emit(OpCodes.Brtrue, finishLabel);
        cursor.Emit(OpCodes.Pop);
        cursor.Emit(OpCodes.Br, continueLabel);

        var saveIndex = cursor.Index;

        // Move to the last callvirt instruction to mark the 'continue' part of the loop.
        while (cursor.TryGotoNext(instr => instr.MatchCallvirt(out var _))) ;
        cursor.Index++;
        cursor.MarkLabel(continueLabel);

        cursor.Index = saveIndex;
        cursor.MarkLabel(finishLabel);
    }
}
