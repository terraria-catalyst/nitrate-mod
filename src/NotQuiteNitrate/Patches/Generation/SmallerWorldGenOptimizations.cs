using System;
using System.Reflection;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Utilities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace NotQuiteNitrate.Patches.Generation;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class SmallerWorldGenOptimizations : ModSystem
{
    public override void Load()
    {
        base.Load();

        On_WorldGen.TileRunner += BetterTileRunner;
    }

    private static void BetterTileRunner(
        On_WorldGen.orig_TileRunner orig,
        int i,
        int j,
        double strength,
        int steps,
        int type,
        bool addTile = false,
        double speedX = 0.0,
        double speedY = 0.0,
        bool noYChange = false,
        bool overRide = true,
        int ignoreTileType = -1
    )
    {
        // OPTIMIZATION: Accessing genRand (get_genRand) accounts for up to 2.5%
        // of runtime in my testing.  Cache the result here.
        var genRand = WorldGen.genRand;

        if (!GenVars.mudWall)
        {
            if (WorldGen.drunkWorldGen)
            {
                strength *= 1.0 + genRand.Next(-80, 81) * 0.01;
                steps = (int)(steps * (1.0 + genRand.Next(-80, 81) * 0.01));
            }
            else if (WorldGen.remixWorldGen)
            {
                strength *= 1.0 + genRand.Next(-50, 51) * 0.01;
            }
            else if (WorldGen.getGoodWorldGen && type != 57)
            {
                strength *= 1.0 + genRand.Next(-80, 81) * 0.015;
                steps += genRand.Next(3);
            }
        }

        var num = strength;
        double num2 = steps;
        var vector2D = default(Vector2D);
        vector2D.X = i;
        vector2D.Y = j;
        var vector2D2 = default(Vector2D);
        vector2D2.X = genRand.Next(-10, 11) * 0.1;
        vector2D2.Y = genRand.Next(-10, 11) * 0.1;
        if (speedX != 0.0 || speedY != 0.0)
        {
            vector2D2.X = speedX;
            vector2D2.Y = speedY;
        }

        var flag = type == 368;
        var flag2 = type == 367;
        var lava = WorldGen.getGoodWorldGen && genRand.NextBool(4);
        while (num > 0.0 && num2 > 0.0)
        {
            if (WorldGen.drunkWorldGen && genRand.NextBool(30))
            {
                vector2D.X += genRand.Next(-100, 101) * 0.05;
                vector2D.Y += genRand.Next(-100, 101) * 0.05;
            }

            if (vector2D.Y < 0.0 && num2 > 0.0 && type == 59)
            {
                num2 = 0.0;
            }

            num = strength * (num2 / steps);
            num2 -= 1.0;
            var num3 = (int)(vector2D.X - num * 0.5);
            var num4 = (int)(vector2D.X + num * 0.5);
            var num5 = (int)(vector2D.Y - num * 0.5);
            var num6 = (int)(vector2D.Y + num * 0.5);
            if (num3 < 1)
            {
                num3 = 1;
            }

            if (num4 > Main.maxTilesX - 1)
            {
                num4 = Main.maxTilesX - 1;
            }

            if (num5 < 1)
            {
                num5 = 1;
            }

            if (num6 > Main.maxTilesY - 1)
            {
                num6 = Main.maxTilesY - 1;
            }

            // OPTIMIZATION: Both loop variables are only mutated in their
            // increments so we can definitely just cache the tile.
            for (var k = num3; k < num4; k++)
            {
                if (k < WorldGen.beachDistance + 50 || k >= Main.maxTilesX - WorldGen.beachDistance - 50)
                {
                    lava = false;
                }

                for (var l = num5; l < num6; l++)
                {
                    var cachedTile = Main.tile[k, l];

                    if ((WorldGen.drunkWorldGen && l < Main.maxTilesY - 300 && type == 57) || (ignoreTileType >= 0 && cachedTile.active() && cachedTile.type == ignoreTileType) || !(Math.Abs(k - vector2D.X) + Math.Abs(l - vector2D.Y) < strength * 0.5 * (1.0 + genRand.Next(-10, 11) * 0.015)))
                    {
                        continue;
                    }

                    if (GenVars.mudWall && l > Main.worldSurface && Main.tile[k, l - 1].wall != 2 && l < Main.maxTilesY - 210 - genRand.Next(3) && Math.Abs(k - vector2D.X) + Math.Abs(l - vector2D.Y) < strength * 0.45 * (1.0 + genRand.Next(-10, 11) * 0.01))
                    {
                        if (l > GenVars.lavaLine - genRand.Next(0, 4) - 50)
                        {
                            if (Main.tile[k, l - 1].wall != 64 && Main.tile[k, l + 1].wall != 64 && Main.tile[k - 1, l].wall != 64 && Main.tile[k + 1, l].wall != 64)
                            {
                                WorldGen.PlaceWall(k, l, 15, mute: true);
                            }
                        }
                        else if (Main.tile[k, l - 1].wall != 15 && Main.tile[k, l + 1].wall != 15 && Main.tile[k - 1, l].wall != 15 && Main.tile[k + 1, l].wall != 15)
                        {
                            WorldGen.PlaceWall(k, l, 64, mute: true);
                        }
                    }

                    if (type < 0)
                    {
                        if (cachedTile.type == 53)
                        {
                            continue;
                        }

                        if (type == -2 && cachedTile.active() && (l < GenVars.waterLine || l > GenVars.lavaLine))
                        {
                            cachedTile.liquid = byte.MaxValue;
                            cachedTile.lava(lava);
                            if (WorldGen.remixWorldGen)
                            {
                                if (l > GenVars.lavaLine && (l < Main.rockLayer - 80.0 || l > Main.maxTilesY - 350) && !WorldGen.oceanDepths(k, l))
                                {
                                    cachedTile.lava(lava: true);
                                }
                            }
                            else if (l > GenVars.lavaLine)
                            {
                                cachedTile.lava(lava: true);
                            }
                        }

                        cachedTile.active(active: false);
                        continue;
                    }

                    if (flag && Math.Abs(k - vector2D.X) + Math.Abs(l - vector2D.Y) < strength * 0.3 * (1.0 + genRand.Next(-10, 11) * 0.01))
                    {
                        WorldGen.PlaceWall(k, l, 180, mute: true);
                    }

                    if (flag2 && Math.Abs(k - vector2D.X) + Math.Abs(l - vector2D.Y) < strength * 0.3 * (1.0 + genRand.Next(-10, 11) * 0.01))
                    {
                        WorldGen.PlaceWall(k, l, 178, mute: true);
                    }

                    if (overRide || !cachedTile.active())
                    {
                        var flag3 = (Main.tileStone[type] && cachedTile.type != 1) || !TileID.Sets.CanBeClearedDuringGeneration[cachedTile.type];
                        switch (cachedTile.type)
                        {
                            case 53:
                                if (type == 59 && GenVars.UndergroundDesertLocation.Contains(k, l))
                                {
                                    flag3 = true;
                                }

                                if (type == 40)
                                {
                                    flag3 = true;
                                }

                                if (l < Main.worldSurface && type != 59)
                                {
                                    flag3 = true;
                                }

                                break;

                            case 45:
                            case 147:
                            case 189:
                            case 190:
                            case 196:
                            case 460:
                                flag3 = true;
                                break;

                            case 396:
                            case 397:
                                flag3 = !TileID.Sets.Ore[type];
                                break;

                            case 1:
                                if (type == 59 && l < Main.worldSurface + genRand.Next(-50, 50))
                                {
                                    flag3 = true;
                                }

                                break;

                            case 367:
                            case 368:
                                if (type == 59)
                                {
                                    flag3 = true;
                                }

                                break;
                        }

                        if (!flag3)
                        {
                            cachedTile.type = (ushort)type;
                        }
                    }

                    if (addTile)
                    {
                        cachedTile.active(active: true);
                        cachedTile.liquid = 0;
                        cachedTile.lava(lava: false);
                    }

                    if (noYChange && l < Main.worldSurface && type != 59)
                    {
                        cachedTile.wall = 2;
                    }

                    if (type == 59 && l > GenVars.waterLine && cachedTile.liquid > 0)
                    {
                        cachedTile.lava(lava: false);
                        cachedTile.liquid = 0;
                    }
                }
            }

            vector2D += vector2D2;
            if ((!WorldGen.drunkWorldGen || !genRand.NextBool(3)) && num > 50.0)
            {
                vector2D += vector2D2;
                num2 -= 1.0;
                vector2D2.Y += genRand.Next(-10, 11) * 0.05;
                vector2D2.X += genRand.Next(-10, 11) * 0.05;
                if (num > 100.0)
                {
                    vector2D += vector2D2;
                    num2 -= 1.0;
                    vector2D2.Y += genRand.Next(-10, 11) * 0.05;
                    vector2D2.X += genRand.Next(-10, 11) * 0.05;
                    if (num > 150.0)
                    {
                        vector2D += vector2D2;
                        num2 -= 1.0;
                        vector2D2.Y += genRand.Next(-10, 11) * 0.05;
                        vector2D2.X += genRand.Next(-10, 11) * 0.05;
                        if (num > 200.0)
                        {
                            vector2D += vector2D2;
                            num2 -= 1.0;
                            vector2D2.Y += genRand.Next(-10, 11) * 0.05;
                            vector2D2.X += genRand.Next(-10, 11) * 0.05;
                            if (num > 250.0)
                            {
                                vector2D += vector2D2;
                                num2 -= 1.0;
                                vector2D2.Y += genRand.Next(-10, 11) * 0.05;
                                vector2D2.X += genRand.Next(-10, 11) * 0.05;
                                if (num > 300.0)
                                {
                                    vector2D += vector2D2;
                                    num2 -= 1.0;
                                    vector2D2.Y += genRand.Next(-10, 11) * 0.05;
                                    vector2D2.X += genRand.Next(-10, 11) * 0.05;
                                    if (num > 400.0)
                                    {
                                        vector2D += vector2D2;
                                        num2 -= 1.0;
                                        vector2D2.Y += genRand.Next(-10, 11) * 0.05;
                                        vector2D2.X += genRand.Next(-10, 11) * 0.05;
                                        if (num > 500.0)
                                        {
                                            vector2D += vector2D2;
                                            num2 -= 1.0;
                                            vector2D2.Y += genRand.Next(-10, 11) * 0.05;
                                            vector2D2.X += genRand.Next(-10, 11) * 0.05;
                                            if (num > 600.0)
                                            {
                                                vector2D += vector2D2;
                                                num2 -= 1.0;
                                                vector2D2.Y += genRand.Next(-10, 11) * 0.05;
                                                vector2D2.X += genRand.Next(-10, 11) * 0.05;
                                                if (num > 700.0)
                                                {
                                                    vector2D += vector2D2;
                                                    num2 -= 1.0;
                                                    vector2D2.Y += genRand.Next(-10, 11) * 0.05;
                                                    vector2D2.X += genRand.Next(-10, 11) * 0.05;
                                                    if (num > 800.0)
                                                    {
                                                        vector2D += vector2D2;
                                                        num2 -= 1.0;
                                                        vector2D2.Y += genRand.Next(-10, 11) * 0.05;
                                                        vector2D2.X += genRand.Next(-10, 11) * 0.05;
                                                        if (num > 900.0)
                                                        {
                                                            vector2D += vector2D2;
                                                            num2 -= 1.0;
                                                            vector2D2.Y += genRand.Next(-10, 11) * 0.05;
                                                            vector2D2.X += genRand.Next(-10, 11) * 0.05;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            vector2D2.X += genRand.Next(-10, 11) * 0.05;
            if (WorldGen.drunkWorldGen)
            {
                vector2D2.X += genRand.Next(-10, 11) * 0.25;
            }

            if (vector2D2.X > 1.0)
            {
                vector2D2.X = 1.0;
            }

            if (vector2D2.X < -1.0)
            {
                vector2D2.X = -1.0;
            }

            if (!noYChange)
            {
                vector2D2.Y += genRand.Next(-10, 11) * 0.05;
                if (vector2D2.Y > 1.0)
                {
                    vector2D2.Y = 1.0;
                }

                if (vector2D2.Y < -1.0)
                {
                    vector2D2.Y = -1.0;
                }
            }
            else if (type != 59 && num < 3.0)
            {
                if (vector2D2.Y > 1.0)
                {
                    vector2D2.Y = 1.0;
                }

                if (vector2D2.Y < -1.0)
                {
                    vector2D2.Y = -1.0;
                }
            }

            if (type == 59 && !noYChange)
            {
                if (vector2D2.Y > 0.5)
                {
                    vector2D2.Y = 0.5;
                }

                if (vector2D2.Y < -0.5)
                {
                    vector2D2.Y = -0.5;
                }

                if (vector2D.Y < Main.rockLayer + 100.0)
                {
                    vector2D2.Y = 1.0;
                }

                if (vector2D.Y > Main.maxTilesY - 300)
                {
                    vector2D2.Y = -1.0;
                }
            }
        }
    }

    private static void CacheGenRand(ILContext il)
    {
        var c = new ILCursor(il);

        il.Body.Variables.Add(new VariableDefinition(il.Import(typeof(UnifiedRandom))));
        var newVar = il.Body.Variables[^1];

        var getGenRand = typeof(WorldGen).GetMethod("get_genRand", BindingFlags.Public | BindingFlags.Static)!;

        c.EmitCall(getGenRand);
        c.EmitStloc(newVar);

        while (c.TryGotoNext(MoveType.Before, x => x.MatchCall(getGenRand)))
        {
            c.Remove();
            c.EmitLdloc(newVar);
        }
    }
}
