using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader;

namespace Nitrate.Optimizations.Tiles;

/// <summary>
///     Speeds up rendering of special tiles such as grass, vines, etc.
///     TODO Make sure this doesn't mess with mods.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class SpecialTileRenderSystem : ModSystem
{
    public override void OnModLoad()
    {
        base.OnModLoad();

        IL_TileDrawing.DrawGrass += CullFarGrass;
        IL_TileDrawing.DrawVines += CullFarVines;
    }

    /// <summary>
    ///     Overrides the vanilla
    ///     <see cref="TileDrawing.DrawVines"/> method to cull vines far away from the local player.
    ///     <br />
    ///     By doing this, we can prevent a large majority of the
    ///     <see cref="Main.Draw"/> calls it performs.
    ///     <br />
    ///     For now, culling is only performed on the X axis. This is done as
    ///     vines could (potentially) by drawn across the entire Y axis.
    ///     TODO Cull vines on Y axis as well.
    /// </summary>
    private static void CullFarVines(ILContext il)
    {
        ILCursor cursor = new(il);

        // Instead of overriding a specific part of the method like CullFarGrass, this method was short enough to simply
        // override the entire thing.
        cursor.EmitDelegate(() =>
        {
            Vector2 unscaledPosition = Main.Camera.UnscaledPosition;
            Vector2 zero = Vector2.Zero;
            Point topLeftPos = Main.ViewPosition.ToTileCoordinates();
            topLeftPos.X -= 10;
            Point bottomRightPos = (Main.ViewPosition + Main.ViewSize).ToTileCoordinates();
            bottomRightPos.X += 10;
            
            int num = 6;
            int num2 = Main.instance.TilesRenderer._specialsCount[num];
            for (int i = 0; i < num2; i++) {
                Point point = Main.instance.TilesRenderer._specialPositions[num][i];
                int x = point.X;
                int y = point.Y;
                if (x < topLeftPos.X || x > bottomRightPos.X)
                    continue;

                Main.instance.TilesRenderer.DrawVineStrip(unscaledPosition, zero, x, y);
            }
        });

        cursor.Emit(OpCodes.Ret);
    }

    /// <summary>
    ///     Overrides the vanilla
    ///     <see cref="TileDrawing.DrawGrass"/> method to cull grass far away from the local player.
    ///     <br />
    ///     By doing this, we can prevent a large majority of the
    ///     <see cref="Main.Draw"/> calls it performs.
    /// </summary>
    private static void CullFarGrass(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(instr => instr.MatchLdelemAny<Point>());
        cursor.Index++;

        ILLabel continueLabel = il.DefineLabel();
        ILLabel finishLabel = il.DefineLabel();
        
        cursor.Emit(OpCodes.Dup);
        cursor.EmitDelegate<Func<Point, bool>>(point =>
        {
            Point topLeftPos = Main.ViewPosition.ToTileCoordinates();
            topLeftPos.X -= 2;
            topLeftPos.Y -= 2;
            Point bottomRightPos = (Main.ViewPosition + Main.ViewSize).ToTileCoordinates();
            bottomRightPos.X += 2;
            bottomRightPos.Y += 2;
            
            return point.X >= topLeftPos.X && point.X <= bottomRightPos.X && point.Y >= topLeftPos.Y && point.Y <= bottomRightPos.Y;
        });

        // This extra branch is necessary to reset the stack back down to what it should be.
        cursor.Emit(OpCodes.Brtrue, finishLabel);
        cursor.Emit(OpCodes.Pop);
        cursor.Emit(OpCodes.Br, continueLabel);
            
        int saveIndex = cursor.Index;

        // Move to the last callvirt instruction to mark the 'continue' part of the loop.
        while (cursor.TryGotoNext(instr => instr.MatchCallvirt(out MethodReference _))) ;
        cursor.Index++;
        cursor.MarkLabel(continueLabel);

        cursor.Index = saveIndex;
        cursor.MarkLabel(finishLabel);
    }
}