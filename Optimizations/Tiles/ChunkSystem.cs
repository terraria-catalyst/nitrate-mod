using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Nitrate.API.Listeners;
using Nitrate.API.Threading;
using Nitrate.API.Tiles;
using Nitrate.Utilities;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
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

    private static readonly Dictionary<Point, TileChunk> loaded_tile_chunks = new();
    private static readonly Dictionary<Point, WallChunk> loaded_wall_chunks = new();

    private static readonly List<Point> tiles_needs_populating = new();
    private static readonly List<Point> walls_needs_populating = new();

    private static RenderTarget2D? tileChunkScreenTarget;
    private static RenderTarget2D? wallChunkScreenTarget;

    private static readonly Lazy<Effect> light_map_renderer = new(() => ModContent.Request<Effect>("Nitrate/Assets/Effects/LightMapRenderer", AssetRequestMode.ImmediateLoad).Value);
    private static RenderTarget2D? lightingBuffer;
    private static Color[] colorBuffer = Array.Empty<Color>();
    private static RenderTarget2D? screenSizeLightingBuffer;
    private static bool enabled;
    private static bool debug;

    public override void OnModLoad()
    {
        base.OnModLoad();

        enabled = Configuration.UsesExperimentalTileRenderer;

        if (!enabled)
        {
            return;
        }

        TileStateChangedListener.OnTileSingleStateChange += TileStateChanged;
        TileStateChangedListener.OnWallSingleStateChange += WallStateChanged;

        // Disable RenderX methods in relation to tile rendering. These methods
        // are responsible for drawing the tile render target in vanilla.
        IL_Main.RenderTiles += CancelVanillaRendering;
        IL_Main.RenderTiles2 += CancelVanillaRendering;
        IL_Main.RenderWalls += CancelVanillaRendering;

        // Hijack the methods responsible for actually drawing to the vanilla
        // tile render target.
        IL_Main.DoDraw_Tiles_Solid += NewDrawSolidTiles;
        IL_Main.DoDraw_Tiles_NonSolid += NewDrawNonSolidTiles;

        IL_Main.DoDraw_WallsAndBlacks += NewDrawWalls;

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

            tileChunkScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
            wallChunkScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
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

            tileChunkScreenTarget?.Dispose();
            tileChunkScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
            wallChunkScreenTarget?.Dispose();
            wallChunkScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);

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
            foreach (TileChunk chunk in loaded_tile_chunks.Values)
            {
                chunk.Dispose();
            }

            foreach (WallChunk chunk in loaded_wall_chunks.Values)
            {
                chunk.Dispose();
            }
        });

        loaded_tile_chunks.Clear();
        loaded_wall_chunks.Clear();
        tiles_needs_populating.Clear();
        walls_needs_populating.Clear();
    }

    public override void Unload()
    {
        base.Unload();

        DisposeAllTileChunks();
        DisposeAllWallChunks();
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

                if (!loaded_tile_chunks.ContainsKey(chunkKey))
                {
                    LoadTileChunk(chunkKey);
                }

                if (!loaded_wall_chunks.ContainsKey(chunkKey))
                {
                    LoadWallChunk(chunkKey);
                }
            }
        }

        List<Point> removeTileList = new();
        List<Point> removeWallList = new();

        foreach (Point key in loaded_tile_chunks.Keys)
        {
            // If this chunk is outside the load range, unload it.
            if (key.X < topX || key.X > bottomX || key.Y < topY || key.Y > bottomY)
            {
                UnloadTileChunk(key);

                removeTileList.Add(key);
            }
        }

        foreach (Point key in loaded_wall_chunks.Keys)
        {
            // If this chunk is outside the load range, unload it.
            if (key.X < topX || key.X > bottomX || key.Y < topY || key.Y > bottomY)
            {
                UnloadWallChunk(key);

                removeWallList.Add(key);
            }
        }

        foreach (Point key in removeTileList)
        {
            loaded_tile_chunks.Remove(key);
        }

        foreach (Point key in removeWallList)
        {
            loaded_wall_chunks.Remove(key);
        }

        foreach (Point key in tiles_needs_populating)
        {
            PopulateTileChunk(key);
        }

        foreach (Point key in walls_needs_populating)
        {
            PopulateWallChunk(key);
        }

        tiles_needs_populating.Clear();
        walls_needs_populating.Clear();
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

    private static void DrawTileChunksToTileChunkTarget(GraphicsDevice device)
    {
        if (tileChunkScreenTarget is null)
        {
            return;
        }

        RenderTargetBinding[] bindings = device.GetRenderTargets();

        foreach (RenderTargetBinding binding in bindings)
        {
            ((RenderTarget2D)binding.RenderTarget).RenderTargetUsage = RenderTargetUsage.PreserveContents;
        }

        device.SetRenderTarget(tileChunkScreenTarget);
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

        foreach (Point key in loaded_tile_chunks.Keys)
        {
            TileChunk chunk = loaded_tile_chunks[key];
            RenderTarget2D target = chunk.RenderTarget;

            Rectangle chunkArea = new(key.X * chunk_size, key.Y * chunk_size, target.Width, target.Height);

            if (!chunkArea.Intersects(screenArea))
            {
                continue;
            }

            // This should never happen, something catastrophic happened if it did.
            // The check here is because rendering disposed targets generally has strange behaviour and doesn't always throw exceptions.
            // Therefore this check needs to be made as it's more robust.
            if (target.IsDisposed)
            {
                throw new Exception("Attempted to render a disposed chunk.");
            }

            Main.spriteBatch.Draw(target, new Vector2(chunkArea.X, chunkArea.Y) - screenPosition, Color.White);

            foreach (Point tilePoint in chunk.AnimatedTiles)
            {
                Tile tile = Framing.GetTileSafely(tilePoint);

                if (!tile.HasTile)
                {
                    continue;
                }

                // Main.instance.TilesRenderer.DrawSingleTile(new TileDrawInfo(), true, 0, new Vector2(tilePoint.X * 16, tilePoint.Y * 16), Vector2.Zero, tilePoint.X, tilePoint.Y);

                // TODO: Check IsTileDrawLayerSolid, solidLayer, DrawTile_LiquidBehindTile
                if (!TextureAssets.Tile[tile.type].IsLoaded)
                {
                    Main.instance.LoadTiles(tile.type);
                }

                if (TileLoader.PreDraw(tilePoint.X, tilePoint.Y, tile.type, Main.spriteBatch))
                {
                    ModifiedTileDrawing.StillHandleSpecialsBecauseTerrariaWasPoorlyProgrammed(tile.type, true, tilePoint.X, tilePoint.Y, tile.frameX, tile.frameY, tile);
                    Main.instance.TilesRenderer.DrawSingleTile(new TileDrawInfo(), true, 0, new Vector2(tilePoint.X * 16, tilePoint.Y * 16), Vector2.Zero, tilePoint.X, tilePoint.Y);
                }

                TileLoader.PostDraw(tilePoint.X, tilePoint.Y, tile.type, Main.spriteBatch);
            }
        }

        Main.spriteBatch.End();

        device.SetRenderTargets(bindings);
    }

    private static void DrawWallChunksToWallChunkTarget(GraphicsDevice device)
    {
        if (wallChunkScreenTarget is null)
        {
            return;
        }

        RenderTargetBinding[] bindings = device.GetRenderTargets();

        foreach (RenderTargetBinding binding in bindings)
        {
            ((RenderTarget2D)binding.RenderTarget).RenderTargetUsage = RenderTargetUsage.PreserveContents;
        }

        device.SetRenderTarget(wallChunkScreenTarget);
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

        foreach (Point key in loaded_wall_chunks.Keys)
        {
            WallChunk chunk = loaded_wall_chunks[key];
            RenderTarget2D target = chunk.RenderTarget;

            Rectangle chunkArea = new(key.X * chunk_size, key.Y * chunk_size, target.Width, target.Height);

            if (!chunkArea.Intersects(screenArea))
            {
                continue;
            }

            // This should never happen, something catastrophic happened if it did.
            // The check here is because rendering disposed targets generally has strange behaviour and doesn't always throw exceptions.
            // Therefore this check needs to be made as it's more robust.
            if (target.IsDisposed)
            {
                throw new Exception("Attempted to render a disposed chunk.");
            }

            Main.spriteBatch.Draw(target, new Vector2(chunkArea.X, chunkArea.Y) - screenPosition, Color.White);

            foreach (Point wallPoint in chunk.AnimatedWalls)
            {
                ModifiedWallDrawing.DrawSingleWallMostlyUnmodified(wallPoint.X, wallPoint.Y, new Vector2(key.X * chunk_size, key.Y * chunk_size));
            }
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

    private static void RenderTileChunksWithLighting()
    {
        if (tileChunkScreenTarget is null || screenSizeLightingBuffer is null)
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

        Main.spriteBatch.Draw(tileChunkScreenTarget, Vector2.Zero, Color.White);

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

            foreach (Point chunkKey in loaded_tile_chunks.Keys)
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

    private static void RenderWallChunksWithLighting()
    {
        if (wallChunkScreenTarget is null || screenSizeLightingBuffer is null)
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

        Main.spriteBatch.Draw(wallChunkScreenTarget, Vector2.Zero, Color.White);

        Main.spriteBatch.End();
    }

    private static void LoadTileChunk(Point chunkKey)
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

        loaded_tile_chunks[chunkKey] = new TileChunk(chunk, new List<Point>());
        tiles_needs_populating.Add(chunkKey);
    }

    private static void PopulateTileChunk(Point chunkKey)
    {
        TileChunk chunk = loaded_tile_chunks[chunkKey];
        RenderTarget2D target = chunk.RenderTarget;

        GraphicsDevice device = Main.graphics.GraphicsDevice;

        device.SetRenderTarget(target);
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

                Tile tile = Framing.GetTileSafely(tileX, tileY);

                if (!tile.HasTile)
                {
                    continue;
                }

                if (AnimatedTileRegistry.IsTilePossiblyAnimated(tile.TileType))
                {
                    chunk.AnimatedTiles.Add(new Point(tileX, tileY));
                }
                else
                {
                    ModifiedTileDrawing.DrawSingleTile(chunkPositionWorld, Vector2.Zero, tileX, tileY);
                }
            }
        }

        Main.tileBatch.End();
        Main.spriteBatch.End();

        device.SetRenderTargets(null);
    }

    private static void UnloadTileChunk(Point chunkKey)
    {
        loaded_tile_chunks[chunkKey].Dispose();
        tiles_needs_populating.Remove(chunkKey);
    }

    private static void DisposeAllTileChunks()
    {
        Main.RunOnMainThread(() =>
        {
            foreach (TileChunk chunk in loaded_tile_chunks.Values)
            {
                chunk.Dispose();
            }

            lightingBuffer?.Dispose();
            lightingBuffer = null;

            tileChunkScreenTarget?.Dispose();
            tileChunkScreenTarget = null;

            screenSizeLightingBuffer?.Dispose();
            screenSizeLightingBuffer = null;
        });

        loaded_tile_chunks.Clear();
        tiles_needs_populating.Clear();
    }

    private static void LoadWallChunk(Point chunkKey)
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

        loaded_wall_chunks[chunkKey] = new WallChunk(chunk, new List<Point>());
        walls_needs_populating.Add(chunkKey);
    }

    private static void PopulateWallChunk(Point chunkKey)
    {
        WallChunk chunk = loaded_wall_chunks[chunkKey];
        RenderTarget2D target = chunk.RenderTarget;

        GraphicsDevice device = Main.graphics.GraphicsDevice;

        device.SetRenderTarget(target);
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

                Tile tile = Framing.GetTileSafely(tileX, tileY);

                if (!tile.HasTile)
                {
                    continue;
                }

                if (AnimatedTileRegistry.IsWallPossiblyAnimated(tile.WallType))
                {
                    chunk.AnimatedWalls.Add(new Point(tileX, tileY));
                }
                else
                {
                    ModifiedWallDrawing.DrawSingleWall(tileX, tileY, chunkPositionWorld);
                }
            }
        }

        Main.tileBatch.End();
        Main.spriteBatch.End();

        device.SetRenderTargets(null);
    }

    private static void UnloadWallChunk(Point chunkKey)
    {
        loaded_wall_chunks[chunkKey].Dispose();
        walls_needs_populating.Remove(chunkKey);
    }

    private static void DisposeAllWallChunks()
    {
        Main.RunOnMainThread(() =>
        {
            foreach (WallChunk chunk in loaded_wall_chunks.Values)
            {
                chunk.Dispose();
            }

            lightingBuffer?.Dispose();
            lightingBuffer = null;

            wallChunkScreenTarget?.Dispose();
            wallChunkScreenTarget = null;

            screenSizeLightingBuffer?.Dispose();
            screenSizeLightingBuffer = null;
        });

        loaded_wall_chunks.Clear();
        walls_needs_populating.Clear();
    }

    private static void TileStateChanged(int i, int j)
    {
        int chunkX = (int)Math.Floor(i / (chunk_size / 16.0));
        int chunkY = (int)Math.Floor(j / (chunk_size / 16.0));

        Point chunkKey = new(chunkX, chunkY);

        if (!loaded_tile_chunks.ContainsKey(chunkKey))
        {
            return;
        }

        if (!tiles_needs_populating.Contains(chunkKey))
        {
            tiles_needs_populating.Add(chunkKey);
        }
    }

    private static void WallStateChanged(int i, int j)
    {
        int chunkX = (int)Math.Floor(i / (chunk_size / 16.0));
        int chunkY = (int)Math.Floor(j / (chunk_size / 16.0));

        Point chunkKey = new(chunkX, chunkY);

        if (!loaded_wall_chunks.ContainsKey(chunkKey))
        {
            return;
        }

        if (!walls_needs_populating.Contains(chunkKey))
        {
            walls_needs_populating.Add(chunkKey);
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

            DrawTileChunksToTileChunkTarget(device);
            // TransferTileSpaceBufferToScreenSpaceBuffer(device);
            RenderTileChunksWithLighting();

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
            Main.instance.TilesRenderer.PreDrawTiles(false, false, true);

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

    private static void NewDrawWalls(ILContext il)
    {
        ILCursor c = new(il);

        c.EmitDelegate(() =>
        {
            PopulateLightingBuffer();

            GraphicsDevice device = Main.graphics.GraphicsDevice;

            Main.spriteBatch.TryEnd(out SpriteBatchUtil.SpriteBatchSnapshot s);

            DrawWallChunksToWallChunkTarget(device);
            TransferTileSpaceBufferToScreenSpaceBuffer(device);
            RenderWallChunksWithLighting();

            Main.spriteBatch.Begin(s.SortMode, s.BlendState, s.SamplerState, s.DepthStencilState, s.RasterizerState, s.Effect, s.TransformMatrix);

            Main.instance.DrawTileCracks(2, Main.LocalPlayer.hitReplace);
            Main.instance.DrawTileCracks(2, Main.LocalPlayer.hitTile);

            Overlays.Scene.Draw(Main.spriteBatch, RenderLayers.Walls);
        });

        c.Emit(OpCodes.Ret);
    }
}