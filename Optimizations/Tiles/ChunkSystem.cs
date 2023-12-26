using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Nitrate.API.Listeners;
using Nitrate.API.Threading;
using Nitrate.Utilities;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader;

namespace Nitrate.Optimizations.Tiles;

/// <summary>
/// TODO:
/// Make sure other effects such as dusts/tile cracks are rendered as well.
/// Ensure water squares can draw behind tiles.
/// Maybe make RenderTiles2 still run for the non-solid layer and tile deco/animated tiles?
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class ChunkSystem : ModSystem
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    private sealed class WarningSystem : ModPlayer
    {
        public override void OnEnterWorld()
        {
            base.OnEnterWorld();

            if (Main.myPlayer != Player.whoAmI)
            {
                return;
            }

            if (Configuration is { UsesExperimentalTileRenderer: true, DisabledExperimentalTileRendererWarning: false })
            {
                Main.NewText("StartupMessages.ExperimentalTileRendererWarning".LocalizeNitrate(), Color.PaleVioletRed);
            }
        }
    }

    // Good sizes include 20, 25, 40, 50, and 100 tiles, as these sizes all multiply evenly into every single default world size's width and height.
    // Smaller chunks are likely better performance-wise as not as many tiles need to be redrawn.
    private const int chunk_size = 20 * 16;

    // The number of layers of additional chunks that stay loaded off-screen around the player. Could help improve performance when moving around in one location.
    private const int chunk_offscreen_buffer = 1;

    private const int lighting_buffer_offscreen_range_tiles = 1;

    private static readonly Dictionary<Point, RenderTarget2D> loaded_chunks = new();
    private static readonly List<Point> needs_populating = new();
    private static readonly Lazy<Effect> light_map_renderer = new(() => ModContent.Request<Effect>("Nitrate/Assets/Effects/LightMapRenderer", AssetRequestMode.ImmediateLoad).Value);
    private static RenderTarget2D? lightingBuffer;
    private static Color[] colorBuffer = Array.Empty<Color>();
    private static RenderTarget2D? chunkScreenTarget;
    private static RenderTarget2D? screenSizeLightingBuffer;
    private static bool enabled;
    private static bool debug;
    private static Dictionary<Point, TileDrawing.TileCounterType> specialPoints = new();

    public override void OnModLoad()
    {
        base.OnModLoad();

        enabled = Configuration.UsesExperimentalTileRenderer;

        if (!enabled)
        {
            return;
        }

        TileStateChangedListener.OnTileSingleStateChange += TileStateChanged;
        TileStateChangedListener.OnWallSingleStateChange += TileStateChanged;

        // Disable RenderX methods in relation to tile rendering. These methods
        // are responsible for drawing the tile render target in vanilla.
        IL_Main.RenderTiles += CancelVanillaRendering;
        IL_Main.RenderTiles2 += CancelVanillaRendering;
        IL_Main.RenderWalls += CancelVanillaRendering;

        // Hijack the methods responsible for actually drawing to the vanilla
        // tile render target.
        IL_Main.DoDraw_Tiles_Solid += NewDrawSolidTiles;
        IL_Main.DoDraw_Tiles_NonSolid += NewDrawNonSolidTiles;

        On_TileDrawing.AddSpecialPoint += AddSpecialPointToHashMap;

        Main.RunOnMainThread(() =>
        {
            // Clear any previous targets if vanilla tile rendering was previously enabled.
            Main.instance.ReleaseTargets();

            lightingBuffer = new RenderTarget2D(
                Main.graphics.GraphicsDevice,
                (int)Math.Ceiling(Main.screenWidth / 16f) + (lighting_buffer_offscreen_range_tiles * 2),
                (int)Math.Ceiling(Main.screenHeight / 16f) + (lighting_buffer_offscreen_range_tiles * 2)
            );

            colorBuffer = new Color[lightingBuffer.Width * lightingBuffer.Height];

            chunkScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
            screenSizeLightingBuffer = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            // By default, Terraria has this set to DiscardContents. This means that switching RTs erases the contents of the backbuffer if done mid-draw.
            Main.graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            Main.graphics.ApplyChanges();
        });

        Main.OnResolutionChanged += _ =>
        {
            lightingBuffer?.Dispose();

            lightingBuffer = new RenderTarget2D(
                Main.graphics.GraphicsDevice,
                (int)Math.Ceiling(Main.screenWidth / 16f) + (lighting_buffer_offscreen_range_tiles * 2),
                (int)Math.Ceiling(Main.screenHeight / 16f) + (lighting_buffer_offscreen_range_tiles * 2)
            );

            chunkScreenTarget?.Dispose();
            chunkScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            screenSizeLightingBuffer?.Dispose();
            screenSizeLightingBuffer = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            colorBuffer = new Color[lightingBuffer.Width * lightingBuffer.Height];
        };
    }

    public override void OnWorldUnload()
    {
        base.OnWorldUnload();

        if (!enabled)
        {
            return;
        }

        Main.RunOnMainThread(() =>
        {
            foreach (RenderTarget2D chunk in loaded_chunks.Values)
            {
                chunk.Dispose();
            }
        });

        loaded_chunks.Clear();
        needs_populating.Clear();
    }

    public override void Unload()
    {
        base.Unload();

        DisposeAllChunks();
    }

    public override void PostUpdateInput()
    {
        base.PostUpdateInput();

        Keys debugKey = Keys.F5;

        if (Main.keyState.IsKeyDown(debugKey) && !Main.oldKeyState.IsKeyDown(debugKey))
        {
            debug = !debug;

            Main.NewText($"Chunk Borders ({debugKey}): " + (debug ? "Shown" : "Hidden"), debug ? Color.Green : Color.Red);
        }
    }

    public override void PostUpdateEverything()
    {
        base.PostUpdateEverything();

        if (!enabled)
        {
            return;
        }

        Rectangle screenArea = new((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);

        // Chunk coordinates are incremented by 1 in each direction per chunk; 1 unit in chunk coordinates is equal to CHUNK_SIZE.
        // Chunk coordinates of the top-leftmost visible chunk.
        int topX = (int)Math.Floor((double)screenArea.X / chunk_size) - chunk_offscreen_buffer;
        int topY = (int)Math.Floor((double)screenArea.Y / chunk_size) - chunk_offscreen_buffer;

        // Chunk coordinates of the bottom-rightmost visible chunk.
        int bottomX = (int)Math.Floor((double)(screenArea.X + screenArea.Width) / chunk_size) + chunk_offscreen_buffer;
        int bottomY = (int)Math.Floor((double)(screenArea.Y + screenArea.Height) / chunk_size) + chunk_offscreen_buffer;

        // Make sure all chunks onscreen as well as the buffer are loaded.
        for (int x = topX; x <= bottomX; x++)
        {
            for (int y = topY; y <= bottomY; y++)
            {
                Point chunkKey = new(x, y);

                if (!loaded_chunks.ContainsKey(chunkKey))
                {
                    LoadChunk(chunkKey);
                }
            }
        }

        List<Point> removeList = new();

        foreach (Point key in loaded_chunks.Keys)
        {
            // If this chunk is outside the load range, unload it.
            if (key.X < topX || key.X > bottomX || key.Y < topY || key.Y > bottomY)
            {
                UnloadChunk(key);

                removeList.Add(key);
            }
        }

        foreach (Point key in removeList)
        {
            loaded_chunks.Remove(key);
        }

        foreach (Point key in needs_populating)
        {
            PopulateChunk(key);
        }

        needs_populating.Clear();

        // Trees are weird so let's leave them out of this.

        Dictionary<int, int> specialsCounts = new();

        foreach ((Point point, TileDrawing.TileCounterType type) in specialPoints)
        {
            if (type == TileDrawing.TileCounterType.Tree)
            {
                continue;
            }

            specialsCounts.TryGetValue((int)type, out int count);
            Main.instance.TilesRenderer._specialPositions[(int)type][count] = point;
            specialsCounts[(int)type] = ++count;
        }

        foreach ((int type, int count) in specialsCounts)
        {
            if (type == (int)TileDrawing.TileCounterType.Tree)
            {
                continue;
            }

            Main.instance.TilesRenderer._specialsCount[type] = count;
        }
    }

    private static void PopulateLightingBuffer()
    {
        if (lightingBuffer is null)
        {
            return;
        }

        FasterParallel.For(0, colorBuffer.Length, (inclusive, exclusive, _) =>
        {
            for (int i = inclusive; i < exclusive; i++)
            {
                int x = i % lightingBuffer.Width;
                int y = i / lightingBuffer.Width;

                colorBuffer[i] = Lighting.GetColor(
                    (int)(Main.screenPosition.X / 16) + x - lighting_buffer_offscreen_range_tiles,
                    (int)(Main.screenPosition.Y / 16) + y - lighting_buffer_offscreen_range_tiles
                );
            }
        });

        // SetDataPointerEXT skips some overhead.
        unsafe
        {
            fixed (Color* ptr = &colorBuffer[0])
            {
                lightingBuffer.SetDataPointerEXT(0, null, (IntPtr)ptr, colorBuffer.Length);
            }
        }
    }

    private static void DrawChunksToChunkTarget(GraphicsDevice device)
    {
        if (chunkScreenTarget is null)
        {
            return;
        }

        RenderTargetBinding[] bindings = device.GetRenderTargets();

        foreach (RenderTargetBinding binding in bindings)
        {
            ((RenderTarget2D)binding.RenderTarget).RenderTargetUsage = RenderTargetUsage.PreserveContents;
        }

        device.SetRenderTarget(chunkScreenTarget);
        device.Clear(Color.Transparent);

        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null,
            Main.GameViewMatrix.TransformationMatrix
        );

        FnaVector2 screenPosition = Main.screenPosition;

        Rectangle screenArea = new((int)screenPosition.X, (int)screenPosition.Y, Main.screenWidth, Main.screenHeight);

        foreach (Point key in loaded_chunks.Keys)
        {
            RenderTarget2D chunk = loaded_chunks[key];

            Rectangle chunkArea = new(key.X * chunk_size, key.Y * chunk_size, chunk.Width, chunk.Height);

            if (!chunkArea.Intersects(screenArea))
            {
                continue;
            }

            // This should never happen, something catastrophic happened if it did.
            // The check here is because rendering disposed targets generally has strange behaviour and doesn't always throw exceptions.
            // Therefore this check needs to be made as it's more robust.
            if (chunk.IsDisposed)
            {
                throw new Exception("Attempted to render a disposed chunk.");
            }

            Main.spriteBatch.Draw(chunk, new Vector2(chunkArea.X, chunkArea.Y) - screenPosition, Color.White);
        }

        Main.spriteBatch.End();

        device.SetRenderTargets(bindings);
    }

    private static void TransferTileSpaceBufferToScreenSpaceBuffer(GraphicsDevice device)
    {
        if (screenSizeLightingBuffer is null)
        {
            return;
        }

        RenderTargetBinding[] bindings = device.GetRenderTargets();

        foreach (RenderTargetBinding binding in bindings)
        {
            ((RenderTarget2D)binding.RenderTarget).RenderTargetUsage = RenderTargetUsage.PreserveContents;
        }

        device.SetRenderTarget(screenSizeLightingBuffer);
        device.Clear(Color.Transparent);

        Main.spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null,
            Main.GameViewMatrix.TransformationMatrix
        );

        FnaVector2 offset = new(Main.screenPosition.X % 16, Main.screenPosition.Y % 16);

        // Account for tile padding around the screen.
        Main.spriteBatch.Draw(lightingBuffer, new Vector2(-lighting_buffer_offscreen_range_tiles * 16) - offset, null, Color.White, 0, Vector2.Zero, 16, SpriteEffects.None, 0);
        Main.spriteBatch.End();

        device.SetRenderTargets(bindings);
    }

    private static void RenderChunksWithLighting()
    {
        if (chunkScreenTarget is null || screenSizeLightingBuffer is null)
        {
            return;
        }

        Main.spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            light_map_renderer.Value
        );

        light_map_renderer.Value.Parameters["lightMap"].SetValue(screenSizeLightingBuffer);

        Main.spriteBatch.Draw(chunkScreenTarget, Vector2.Zero, Color.White);

        Main.spriteBatch.End();

        if (debug)
        {
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone
            );

            int lineWidth = 2;
            int offset = lineWidth / 2;

            foreach (Point chunkKey in loaded_chunks.Keys)
            {
                int chunkX = (chunkKey.X * chunk_size) - (int)Main.screenPosition.X;
                int chunkY = (chunkKey.Y * chunk_size) - (int)Main.screenPosition.Y;

                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX - offset, chunkY - offset, chunk_size + offset, lineWidth), Color.Yellow);
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX + chunk_size - offset, chunkY - offset, lineWidth, chunk_size + offset), Color.Yellow);
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX - offset, chunkY + chunk_size - offset, chunk_size + offset, lineWidth), Color.Yellow);
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX - offset, chunkY - offset, lineWidth, chunk_size + offset), Color.Yellow);
            }

            Main.spriteBatch.End();
        }
    }

    private static void LoadChunk(Point chunkKey)
    {
        RenderTarget2D chunk = new(
            Main.graphics.GraphicsDevice,
            chunk_size,
            chunk_size,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents
        );

        loaded_chunks[chunkKey] = chunk;
        needs_populating.Add(chunkKey);
    }

    private static void PopulateChunk(Point chunkKey)
    {
        RenderTarget2D chunk = loaded_chunks[chunkKey];

        GraphicsDevice device = Main.graphics.GraphicsDevice;

        device.SetRenderTarget(chunk);
        device.Clear(Color.Transparent);

        Vector2 chunkPositionWorld = new(chunkKey.X * chunk_size, chunkKey.Y * chunk_size);

        int sizeTiles = chunk_size / 16;

        Point chunkPositionTile = new((int)chunkPositionWorld.X / 16, (int)chunkPositionWorld.Y / 16);

        Main.tileBatch.Begin();

        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );

        for (int i = 0; i < sizeTiles; i++)
        {
            for (int j = 0; j < sizeTiles; j++)
            {
                int tileX = chunkPositionTile.X + i;
                int tileY = chunkPositionTile.Y + j;

                if (!WorldGen.InWorld(tileX, tileY))
                {
                    continue;
                }

                ModifiedWallDrawing.DrawSingleWall(tileX, tileY, chunkPositionWorld);
            }
        }

        for (int i = 0; i < sizeTiles; i++)
        {
            for (int j = 0; j < sizeTiles; j++)
            {
                int tileX = chunkPositionTile.X + i;
                int tileY = chunkPositionTile.Y + j;

                Tile tile = Framing.GetTileSafely(tileX, tileY);

                if (!WorldGen.InWorld(tileX, tileY) || !Main.tile[tileX, tileY].active())
                {
                    continue;
                }

                // TODO: Might also need to account for RenderTiles2 behaviour (solidLayer = false).
                if (!HandledBySpecialPoint(tile, tileX, tileY))
                {
                    ModifiedTileDrawing.DrawSingleTile(chunkPositionWorld, Vector2.Zero, tileX, tileY);
                }

                UpdateSpecialPoints(tile, tileX, tileY);
            }
        }

        Main.tileBatch.End();
        Main.spriteBatch.End();

        device.SetRenderTargets(null);
    }

    private static void UnloadChunk(Point chunkKey)
    {
        loaded_chunks[chunkKey].Dispose();
        needs_populating.Remove(chunkKey);
    }

    private static void DisposeAllChunks()
    {
        Main.RunOnMainThread(() =>
        {
            foreach (RenderTarget2D chunk in loaded_chunks.Values)
            {
                chunk.Dispose();
            }

            lightingBuffer?.Dispose();
            lightingBuffer = null;

            chunkScreenTarget?.Dispose();
            chunkScreenTarget = null;

            screenSizeLightingBuffer?.Dispose();
            screenSizeLightingBuffer = null;
        });

        loaded_chunks.Clear();
        needs_populating.Clear();
    }

    private static void TileStateChanged(int i, int j)
    {
        int chunkX = (int)Math.Floor(i / (chunk_size / 16.0));
        int chunkY = (int)Math.Floor(j / (chunk_size / 16.0));

        Point chunkKey = new(chunkX, chunkY);

        if (!loaded_chunks.ContainsKey(chunkKey))
        {
            return;
        }

        if (!needs_populating.Contains(chunkKey))
        {
            needs_populating.Add(chunkKey);
        }
    }

    private static void CancelVanillaRendering(ILContext il)
    {
        ILCursor c = new(il);

        c.Emit(OpCodes.Ret);
    }

    private static void NewDrawSolidTiles(ILContext il)
    {
        ILCursor c = new(il);

        c.EmitDelegate(() =>
        {
            // Main.instance.TilesRenderer.PreDrawTiles(true, false, true);

            GraphicsDevice device = Main.graphics.GraphicsDevice;

            PopulateLightingBuffer();
            DrawChunksToChunkTarget(device);
            TransferTileSpaceBufferToScreenSpaceBuffer(device);
            RenderChunksWithLighting();

            Main.instance.DrawTileEntities(true, true, false);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

            try
            {
                Main.player[Main.myPlayer].hitReplace.DrawFreshAnimations(Main.spriteBatch);
                Main.player[Main.myPlayer].hitTile.DrawFreshAnimations(Main.spriteBatch);
            }
            catch (Exception e2)
            {
                TimeLogger.DrawException(e2);
            }

            Main.spriteBatch.End();
        });

        c.Emit(OpCodes.Ret);
    }

    private static void NewDrawNonSolidTiles(ILContext il)
    {
        ILCursor c = new(il);

        c.EmitDelegate(() =>
        {
            /*Don't bother with these operations because we handle special
            points ourselves in PostUpdateEverything and PopulateChunk.

            Main.instance.TilesRenderer.PreDrawTiles(false, false, true);

            Main.instance.TilesRenderer.Draw(false, false, true);*/

            Main.spriteBatch.End();

            try
            {
                Main.instance.DrawTileEntities(false, true, false);
            }
            catch (Exception e)
            {
                TimeLogger.DrawException(e);
            }

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        });

        c.Emit(OpCodes.Ret);
    }

    private static void UpdateSpecialPoints(Tile tile, int x, int y)
    {
        Point point = new(x, y);

        if (specialPoints.ContainsKey(point))
        {
            specialPoints.Remove(point);
        }

        // ReSharper disable once ConvertToConstant.Local
        bool flag = true;
        // TODO: Put PreDraw hook call here?

        switch (tile.type)
        {
            case 52:
            case 62:
            case 115:
            case 205:
            case 382:
            case 528:
            case 636:
            case 638:
                if (flag)
                {
                    Main.instance.TilesRenderer.CrawlToTopOfVineAndAddSpecialPoint(y, x);
                }

                break;

            case 549:
                if (flag)
                {
                    Main.instance.TilesRenderer.CrawlToBottomOfReverseVineAndAddSpecialPoint(y, x);
                }

                break;

            case 34:
                if (tile.frameX % 54 == 0 && tile.frameY % 54 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 454:
                if (tile.frameX % 72 == 0 && tile.frameY % 54 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 42:
            case 270:
            case 271:
            case 572:
            case 581:
            case 660:
                if (tile.frameX % 18 == 0 && tile.frameY % 36 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 91:
                if (tile.frameX % 18 == 0 && tile.frameY % 54 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 95:
            case 126:
            case 444:
                if (tile.frameX % 36 == 0 && tile.frameY % 36 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 465:
            case 591:
            case 592:
                if (tile.frameX % 36 == 0 && tile.frameY % 54 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 27:
                if (tile.frameX % 36 == 0 && tile.frameY == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 236:
            case 238:
                if (tile.frameX % 36 == 0 && tile.frameY == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 233:
                if (tile.frameY == 0 && tile.frameX % 54 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                if (tile.frameY == 34 && tile.frameX % 36 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 652:
                if (tile.frameX % 36 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 651:
                if (tile.frameX % 54 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 530:
                if (tile.frameX < 270)
                {
                    if (tile.frameX % 54 == 0 && tile.frameY == 0 && flag)
                    {
                        Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                    }
                }

                break;

            case 485:
            case 489:
            case 490:
                if (tile.frameY == 0 && tile.frameX % 36 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 521:
            case 522:
            case 523:
            case 524:
            case 525:
            case 526:
            case 527:
                if (tile.frameY == 0 && tile.frameX % 36 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 493:
                if (tile.frameY == 0 && tile.frameX % 18 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 519:
                if (tile.frameX / 18 <= 4 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 373:
            case 374:
            case 375:
            case 461:
                Main.instance.TilesRenderer.EmitLiquidDrops(y, x, tile, tile.type);

                break;

            case 491:
                if (flag && tile.frameX == 18 && tile.frameY == 18)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.VoidLens);
                }

                break;

            case 597:
                if (flag && tile.frameX % 54 == 0 && tile.frameY == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.TeleportationPylon);
                }

                break;

            case 617:
                if (flag && tile.frameX % 54 == 0 && tile.frameY % 72 == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MasterTrophy);
                }

                break;

            case 184:
                if (flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.AnyDirectionalGrass);
                }

                break;

            default:
                if (Main.instance.TilesRenderer.ShouldSwayInWind(x, y, tile))
                {
                    if (flag)
                    {
                        Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.WindyGrass);
                    }
                }

                break;
        }
    }

    private static void AddSpecialPointToHashMap(On_TileDrawing.orig_AddSpecialPoint orig, TileDrawing self, int x, int y, int type)
    {
        if (type == (int)TileDrawing.TileCounterType.Tree)
        {
            orig(self, x, y, type);

            return;
        }

        specialPoints[new Point(x, y)] = (TileDrawing.TileCounterType)type;
    }

    private static bool HandledBySpecialPoint(Tile tile, int x, int y)
    {
        switch (tile.type)
        {
            case 52:
            case 62:
            case 115:
            case 205:
            case 382:
            case 528:
            case 636:
            case 638:
                return true;

            case 549:
                return true;

            case 34:
                return true;

            case 454:
                return true;

            case 42:
            case 270:
            case 271:
            case 572:
            case 581:
            case 660:
                return true;

            case 91:
                return true;

            case 95:
            case 126:
            case 444:
                return true;

            case 465:
            case 591:
            case 592:
                return true;

            case 27:
                return true;

            case 236:
            case 238:
                return true;

            case 233:
                return true;

            case 652:
                return true;

            case 651:
                return true;

            case 530:
                return true;

            case 485:
            case 489:
            case 490:
                return true;

            case 521:
            case 522:
            case 523:
            case 524:
            case 525:
            case 526:
            case 527:
                return true;

            case 493:
                return true;

            case 519:
                return true;

            case 184:
                return true;

            default:
                return Main.instance.TilesRenderer.ShouldSwayInWind(x, y, tile);
        }
    }
}