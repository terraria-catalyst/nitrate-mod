using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Nitrate.Core.Features.Threading;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Nitrate.Content.Optimizations.Tiles;

/// <summary>
/// TODO:
/// Make sure other effects such as dusts/tile cracks are rendered as well.
/// Ensure water squares can draw behind tiles.
/// Maybe make RenderTiles2 still run for the nonsolid layer and tile deco/animated tiles?
/// Fix layering.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class ChunkSystem : ModSystem
{
    // Good sizes include 20, 25, 40, 50, and 100 tiles, as these sizes all multiply evenly into every single default world size's width and height.
    // Smaller chunks are likely better performance-wise as not as many tiles need to be redrawn.
    private const int chunk_size = 20 * 16;

    // The number of layers of additional chunks that stay loaded off-screen around the player. Could help improve performance when moving around in one location.
    private const int chunk_offscreen_buffer = 1;

    private const int lighting_buffer_offscreen_range_tiles = 1;

    private readonly Dictionary<Point, RenderTarget2D> _loadedChunks = new();

    private readonly List<Point> _needsPopulating = new();

    private readonly Lazy<Effect> _lightMapRenderer;

    private RenderTarget2D _lightingBuffer;

    private Color[] _colorBuffer;

    private RenderTarget2D _chunkScreenTarget;

    private RenderTarget2D _screenSizeLightingBuffer;

    public ChunkSystem()
    {
        _lightMapRenderer = new Lazy<Effect>(() => Mod.Assets.Request<Effect>("Assets/Effects/LightMapRenderer", AssetRequestMode.ImmediateLoad).Value);
    }

    public override void OnModLoad()
    {
        base.OnModLoad();

        RegisterTileStateChangedEvents();

        IL_Main.RenderTiles += CancelVanillaRendering;
        // IL_Main.RenderTiles2 += CancelVanillaRendering;
        IL_Main.RenderWalls += CancelVanillaRendering;

        IL_Main.DoDraw_Tiles_Solid += ChunkDrawingPipeline;

        Main.RunOnMainThread(() =>
        {
            _lightingBuffer = new RenderTarget2D(
                Main.graphics.GraphicsDevice,
                (int)Math.Ceiling(Main.screenWidth / 16f) + (lighting_buffer_offscreen_range_tiles * 2),
                (int)Math.Ceiling(Main.screenHeight / 16f) + (lighting_buffer_offscreen_range_tiles * 2)
            );

            _colorBuffer = new Color[_lightingBuffer.Width * _lightingBuffer.Height];

            _chunkScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
            _screenSizeLightingBuffer = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            // By default Terraria has this set to DiscardContents. This means that switching RTs erases the contents of the backbuffer if done mid-draw.
            Main.graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            Main.graphics.ApplyChanges();
        });

        Main.OnResolutionChanged += _ =>
        {
            _lightingBuffer?.Dispose();
            _lightingBuffer = new RenderTarget2D(Main.graphics.GraphicsDevice, (Main.screenWidth / 16) + 2, (Main.screenHeight / 16) + 2);

            _chunkScreenTarget?.Dispose();
            _chunkScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            _screenSizeLightingBuffer?.Dispose();
            _screenSizeLightingBuffer = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            _colorBuffer = new Color[_lightingBuffer.Width * _lightingBuffer.Height];
        };
    }

    public override void OnWorldUnload()
    {
        base.OnWorldUnload();

        DisposeAllChunks();
    }

    public override void PostUpdateEverything()
    {
        base.PostUpdateEverything();

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

                if (!_loadedChunks.ContainsKey(chunkKey))
                {
                    LoadChunk(chunkKey);
                }
            }
        }

        List<Point> removeList = new();

        foreach (Point key in _loadedChunks.Keys)
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
            _loadedChunks.Remove(key);
        }

        foreach (Point key in _needsPopulating)
        {
            PopulateChunk(key);
        }

        _needsPopulating.Clear();
    }

    private void PopulateLightingBuffer()
    {
        FasterParallel.For(0, _colorBuffer.Length, (inclusive, exclusive, _) =>
        {
            for (int i = inclusive; i < exclusive; i++)
            {
                int x = i % _lightingBuffer.Width;
                int y = i / _lightingBuffer.Width;

                _colorBuffer[i] = Lighting.GetColor(
                    (int)(Main.screenPosition.X / 16) + x - lighting_buffer_offscreen_range_tiles,
                    (int)(Main.screenPosition.Y / 16) + y - lighting_buffer_offscreen_range_tiles
                );
            }
        });

        // SetDataPointerEXT skips some overhead.
        unsafe
        {
            fixed (Color* ptr = &_colorBuffer[0])
            {
                _lightingBuffer.SetDataPointerEXT(0, null, (IntPtr)ptr, _colorBuffer.Length);
            }
        }
    }

    private void DrawChunksToChunkTarget(GraphicsDevice device)
    {
        device.SetRenderTarget(_chunkScreenTarget);
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

        foreach (Point key in _loadedChunks.Keys)
        {
            RenderTarget2D chunk = _loadedChunks[key];

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

        device.SetRenderTargets(null);
    }

    private void TransferTileSpaceBufferToScreenSpaceBuffer(GraphicsDevice device)
    {
        device.SetRenderTarget(_screenSizeLightingBuffer);
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

        Vector2 offset = new(Main.screenPosition.X % 16, Main.screenPosition.Y % 16);

        // Account for tile padding around the screen.
        Main.spriteBatch.Draw(_lightingBuffer, new Vector2(-lighting_buffer_offscreen_range_tiles * 16) - offset, null, Color.White, 0, Vector2.Zero, 16, SpriteEffects.None, 0);
        Main.spriteBatch.End();

        device.SetRenderTargets(null);
    }

    private void RenderChunksWithLighting()
    {
        Main.spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            _lightMapRenderer.Value
        );

        _lightMapRenderer.Value.Parameters["lightMap"].SetValue(_screenSizeLightingBuffer);

        Main.spriteBatch.Draw(_chunkScreenTarget, Vector2.Zero, Color.White);

        Main.spriteBatch.End();
    }

    private void LoadChunk(Point chunkKey)
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

        _loadedChunks[chunkKey] = chunk;
        _needsPopulating.Add(chunkKey);
    }

    private void PopulateChunk(Point chunkKey)
    {
        RenderTarget2D chunk = _loadedChunks[chunkKey];

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

                if (!WorldGen.InWorld(tileX, tileY) || !Main.tile[tileX, tileY].active())
                {
                    continue;
                }

                // TODO: Might also need to account for RenderTiles2 behaviour (solidLayer = false).
                ModifiedTileDrawing.DrawSingleTile(chunkPositionWorld, Vector2.Zero, tileX, tileY);
            }
        }

        Main.tileBatch.End();
        Main.spriteBatch.End();

        device.SetRenderTargets(null);
    }

    private void UnloadChunk(Point chunkKey)
    {
        _loadedChunks[chunkKey].Dispose();
        _needsPopulating.Remove(chunkKey);
    }

    private void DisposeAllChunks()
    {
        Main.RunOnMainThread(() =>
        {
            foreach (RenderTarget2D chunk in _loadedChunks.Values)
            {
                chunk.Dispose();
            }
        });

        _loadedChunks.Clear();
        _needsPopulating.Clear();
    }

    private void RegisterTileStateChangedEvents()
    {
        On_WorldGen.PlaceTile += On_WorldGen_PlaceTile;
        On_WorldGen.KillTile += On_WorldGen_KillTile;
        On_WorldGen.TileFrame += On_WorldGen_TileFrame;
        On_WorldGen.PlaceWall += On_WorldGen_PlaceWall;
        On_WorldGen.KillWall += On_WorldGen_KillWall;
        On_Framing.WallFrame += On_Framing_WallFrame;
    }

    private bool On_WorldGen_PlaceTile(On_WorldGen.orig_PlaceTile orig, int i, int j, int type, bool mute, bool forced, int plr, int style)
    {
        bool result = orig(i, j, type, mute, forced, plr, style);

        // Maybe can check if(result)? Not sure if the method actually makes any world changes if false.
        TileStateChanged(i, j);

        return result;
    }

    private void On_WorldGen_KillTile(On_WorldGen.orig_KillTile orig, int i, int j, bool fail, bool effectOnly, bool noItem)
    {
        orig(i, j, fail, effectOnly, noItem);
        
        // Maybe can check if(fail)?
        TileStateChanged(i, j);
    }

    private void On_WorldGen_TileFrame(On_WorldGen.orig_TileFrame orig, int i, int j, bool resetFrame, bool noBreak)
    {
        orig(i, j, resetFrame, noBreak);

        TileStateChanged(i, j);
    }

    private void On_WorldGen_PlaceWall(On_WorldGen.orig_PlaceWall orig, int i, int j, int type, bool mute)
    {
        orig(i, j, type, mute);

        TileStateChanged(i, j);
    }

    private void On_WorldGen_KillWall(On_WorldGen.orig_KillWall orig, int i, int j, bool fail)
    {
        orig(i, j, fail);

        TileStateChanged(i, j);
    }

    private void On_Framing_WallFrame(On_Framing.orig_WallFrame orig, int i, int j, bool resetFrame)
    {
        orig(i, j, resetFrame);

        TileStateChanged(i, j);
    }

    private void TileStateChanged(int i, int j)
    {
        int chunkX = (int)Math.Floor(i / (chunk_size / 16.0));
        int chunkY = (int)Math.Floor(j / (chunk_size / 16.0));

        Point chunkKey = new(chunkX, chunkY);

        if (!_loadedChunks.ContainsKey(chunkKey))
        {
            return;
        }

        if (!_needsPopulating.Contains(chunkKey))
        {
            _needsPopulating.Add(chunkKey);
        }
    }

    private void CancelVanillaRendering(ILContext il)
    {
        ILCursor c = new(il);

        c.Emit(OpCodes.Ret);
    }

    private void ChunkDrawingPipeline(ILContext il)
    {
        ILCursor c = new(il);

        c.EmitDelegate(() =>
        {
            GraphicsDevice device = Main.graphics.GraphicsDevice;

            PopulateLightingBuffer();
            DrawChunksToChunkTarget(device);
            TransferTileSpaceBufferToScreenSpaceBuffer(device);
            RenderChunksWithLighting();
        });

        c.Emit(OpCodes.Ret);
    }
}