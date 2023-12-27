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
using System.Linq;
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
    internal const int CHUNK_SIZE = 20 * 16;

    // The number of layers of additional chunks that stay loaded off-screen around the player. Could help improve performance when moving around in one location.
    private const int chunk_offscreen_buffer = 1;

    private const int lighting_buffer_offscreen_range_tiles = 1;

    private static readonly ChunkCollection tiles = new TileChunkCollection();
    private static readonly ChunkCollection walls = new WallChunkCollection();

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

            tiles.ScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
            walls.ScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
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

            tiles.ScreenTarget?.Dispose();
            tiles.ScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
            walls.ScreenTarget?.Dispose();
            walls.ScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);

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

        // Clone these outside of the delegate to ensure the delegate captures
        // an uncleared collection. This probably prevents a memory leak.
        List<Chunk> loadedTilesClone = tiles.Loaded.Values.ToList();
        List<Chunk> loadedWallsClone = walls.Loaded.Values.ToList();

        Main.RunOnMainThread(() =>
        {
            foreach (Chunk chunk in loadedTilesClone)
            {
                chunk.Dispose();
            }

            foreach (Chunk chunk in loadedWallsClone)
            {
                chunk.Dispose();
            }
        });

        tiles.Loaded.Clear();
        walls.Loaded.Clear();
        tiles.NeedsPopulating.Clear();
        walls.NeedsPopulating.Clear();
    }

    public override void Unload()
    {
        base.Unload();

        tiles.DisposeAllChunks();
        walls.DisposeAllChunks();

        Main.RunOnMainThread(() =>
        {
            lightingBuffer?.Dispose();
            lightingBuffer = null;

            screenSizeLightingBuffer?.Dispose();
            screenSizeLightingBuffer = null;
        });
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
        int topX = (int)Math.Floor((double)screenArea.X / CHUNK_SIZE) - chunk_offscreen_buffer;
        int topY = (int)Math.Floor((double)screenArea.Y / CHUNK_SIZE) - chunk_offscreen_buffer;

        // Chunk coordinates of the bottom-rightmost visible chunk.
        int bottomX = (int)Math.Floor((double)(screenArea.X + screenArea.Width) / CHUNK_SIZE) + chunk_offscreen_buffer;
        int bottomY = (int)Math.Floor((double)(screenArea.Y + screenArea.Height) / CHUNK_SIZE) + chunk_offscreen_buffer;

        // Make sure all chunks onscreen as well as the buffer are loaded.
        for (int x = topX; x <= bottomX; x++)
        {
            for (int y = topY; y <= bottomY; y++)
            {
                Point chunkKey = new(x, y);

                if (!tiles.Loaded.ContainsKey(chunkKey))
                {
                    tiles.LoadChunk(chunkKey);
                }

                if (!walls.Loaded.ContainsKey(chunkKey))
                {
                    walls.LoadChunk(chunkKey);
                }
            }
        }

        List<Point> removeTileList = new();
        List<Point> removeWallList = new();

        foreach (Point key in tiles.Loaded.Keys)
        {
            // If this chunk is outside the load range, unload it.
            if (key.X < topX || key.X > bottomX || key.Y < topY || key.Y > bottomY)
            {
                tiles.UnloadChunk(key);
                removeTileList.Add(key);
            }
        }

        foreach (Point key in removeTileList)
        {
            tiles.Loaded.Remove(key);
        }

        foreach (Point key in tiles.NeedsPopulating)
        {
            tiles.PopulateChunk(key);
        }

        tiles.NeedsPopulating.Clear();

        foreach (Point key in walls.Loaded.Keys)
        {
            // If this chunk is outside the load range, unload it.
            if (key.X < topX || key.X > bottomX || key.Y < topY || key.Y > bottomY)
            {
                walls.UnloadChunk(key);
                removeWallList.Add(key);
            }
        }

        foreach (Point key in removeWallList)
        {
            walls.Loaded.Remove(key);
        }

        foreach (Point key in walls.NeedsPopulating)
        {
            walls.PopulateChunk(key);
        }

        walls.NeedsPopulating.Clear();
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

    private static void TileStateChanged(int i, int j)
    {
        int chunkX = (int)Math.Floor(i / (CHUNK_SIZE / 16.0));
        int chunkY = (int)Math.Floor(j / (CHUNK_SIZE / 16.0));

        Point chunkKey = new(chunkX, chunkY);

        if (!tiles.Loaded.ContainsKey(chunkKey))
        {
            return;
        }

        if (!tiles.NeedsPopulating.Contains(chunkKey))
        {
            tiles.NeedsPopulating.Add(chunkKey);
        }
    }

    private static void WallStateChanged(int i, int j)
    {
        int chunkX = (int)Math.Floor(i / (CHUNK_SIZE / 16.0));
        int chunkY = (int)Math.Floor(j / (CHUNK_SIZE / 16.0));

        Point chunkKey = new(chunkX, chunkY);

        if (!walls.Loaded.ContainsKey(chunkKey))
        {
            return;
        }

        if (!walls.NeedsPopulating.Contains(chunkKey))
        {
            walls.NeedsPopulating.Add(chunkKey);
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

            tiles.DrawChunksToChunkTarget(device);
            // TransferTileSpaceBufferToScreenSpaceBuffer(device);
            tiles.RenderChunksWithLighting(screenSizeLightingBuffer, light_map_renderer);

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

                foreach (Point chunkKey in tiles.Loaded.Keys)
                {
                    int chunkX = (chunkKey.X * CHUNK_SIZE) - (int)Main.screenPosition.X;
                    int chunkY = (chunkKey.Y * CHUNK_SIZE) - (int)Main.screenPosition.Y;

                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX - offset, chunkY - offset, CHUNK_SIZE + offset, lineWidth), Color.Yellow);
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX + CHUNK_SIZE - offset, chunkY - offset, lineWidth, CHUNK_SIZE + offset), Color.Yellow);
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX - offset, chunkY + CHUNK_SIZE - offset, CHUNK_SIZE + offset, lineWidth), Color.Yellow);
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX - offset, chunkY - offset, lineWidth, CHUNK_SIZE + offset), Color.Yellow);
                }

                Main.spriteBatch.End();
            }

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

            walls.DrawChunksToChunkTarget(device);
            TransferTileSpaceBufferToScreenSpaceBuffer(device);
            walls.RenderChunksWithLighting(screenSizeLightingBuffer, light_map_renderer);

            Main.spriteBatch.Begin(s.SortMode, s.BlendState, s.SamplerState, s.DepthStencilState, s.RasterizerState, s.Effect, s.TransformMatrix);

            Main.instance.DrawTileCracks(2, Main.LocalPlayer.hitReplace);
            Main.instance.DrawTileCracks(2, Main.LocalPlayer.hitTile);

            Overlays.Scene.Draw(Main.spriteBatch, RenderLayers.Walls);
        });

        c.Emit(OpCodes.Ret);
    }
}