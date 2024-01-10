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
using Terraria.Graphics.Effects;
using Terraria.Graphics.Light;
using Terraria.ModLoader;

namespace Nitrate.Optimizations.Tiles;

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
    private const int edge_threshold = 3;

    private static readonly TileChunkCollection solid_tiles = new() { SolidLayer = true };
    private static readonly TileChunkCollection non_solid_tiles = new() { SolidLayer = false };
    private static readonly WallChunkCollection walls = new();
    private static readonly ChunkCollection[] chunk_collections = { solid_tiles, non_solid_tiles, walls };
    private static readonly Lazy<Effect> light_map_renderer = new(() => ModContent.Request<Effect>("Nitrate/Assets/Effects/LightMapRenderer", AssetRequestMode.ImmediateLoad).Value);
    private static RenderTarget2D? lightingBuffer;
    private static Color[] colorBuffer = Array.Empty<Color>();
    private static RenderTarget2D? screenSizeLightingBuffer;
    private static bool enabled;
    private static bool debugChunkBorders;
    private static bool debugLightMap;

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

        DynamicTileVisibilityListener.OnVisibilityChange += TileVisibilityChanged;

        // Disable RenderX methods in relation to tile rendering. These methods
        // are responsible for drawing the tile render target in vanilla.
        IL_Main.RenderTiles += CancelVanillaRendering;
        IL_Main.RenderTiles2 += CancelVanillaRendering;
        IL_Main.RenderWalls += CancelVanillaRendering;

        // Hijack the methods responsible for actually drawing to the vanilla
        // tile render target.
        IL_Main.DoDraw_WallsAndBlacks += NewDrawWalls;
        IL_Main.DoDraw_Tiles_NonSolid += NewDrawNonSolidTiles;
        IL_Main.DoDraw_Tiles_Solid += NewDrawSolidTiles;
        On_Main.DoDraw_WallsTilesNPCs += HookBeforeDrawingToPopulateLightingBufferAndHandleStuffThatShouldHappenWhenDrawingToScreen;

        Main.RunOnMainThread(() =>
        {
            // Clear any previous targets if vanilla tile rendering was previously enabled.
            Main.instance.ReleaseTargets();

            lightingBuffer = new RenderTarget2D(
                Main.graphics.GraphicsDevice,
                (int)Math.Ceiling(Main.screenWidth / 16f) + lighting_buffer_offscreen_range_tiles * 2,
                (int)Math.Ceiling(Main.screenHeight / 16f) + lighting_buffer_offscreen_range_tiles * 2
            );

            colorBuffer = new Color[lightingBuffer.Width * lightingBuffer.Height];

            foreach (ChunkCollection chunkCollection in chunk_collections)
            {
                chunkCollection.ScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
            }

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
                (int)Math.Ceiling(Main.screenWidth / 16f) + lighting_buffer_offscreen_range_tiles * 2,
                (int)Math.Ceiling(Main.screenHeight / 16f) + lighting_buffer_offscreen_range_tiles * 2
            );

            foreach (ChunkCollection chunkCollection in chunk_collections)
            {
                chunkCollection.ScreenTarget?.Dispose();
                chunkCollection.ScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
            }

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
        List<Chunk> loadedChunksClone = new();

        foreach (ChunkCollection chunkCollection in chunk_collections)
        {
            loadedChunksClone.AddRange(chunkCollection.Loaded.Values);
        }

        Main.RunOnMainThread(() =>
        {
            foreach (Chunk chunk in loadedChunksClone)
            {
                chunk.Dispose();
            }
        });

        foreach (ChunkCollection chunkCollection in chunk_collections)
        {
            chunkCollection.Loaded.Clear();
            chunkCollection.NeedsPopulating.Clear();
        }
    }

    public override void Unload()
    {
        base.Unload();

        foreach (ChunkCollection chunkCollection in chunk_collections)
        {
            chunkCollection.DisposeAllChunks();
        }

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

        const Keys chunk_border_key = Keys.F5;
        const Keys light_map_key = Keys.F6;

        if (Main.keyState.IsKeyDown(chunk_border_key) && !Main.oldKeyState.IsKeyDown(chunk_border_key))
        {
            debugChunkBorders = !debugChunkBorders;

            Main.NewText($"Chunk Borders ({chunk_border_key}): " + (debugChunkBorders ? "Shown" : "Hidden"), debugChunkBorders ? Color.Green : Color.Red);
        }

        if (Main.keyState.IsKeyDown(light_map_key) && !Main.oldKeyState.IsKeyDown(light_map_key))
        {
            debugLightMap = !debugLightMap;

            Main.NewText($"Light Map ({light_map_key}): " + (debugLightMap ? "Shown" : "Hidden"), debugLightMap ? Color.Green : Color.Red);
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

                foreach (ChunkCollection chunkCollection in chunk_collections)
                {
                    if (!chunkCollection.Loaded.ContainsKey(chunkKey))
                    {
                        chunkCollection.LoadChunk(chunkKey);
                    }
                }
            }
        }

        foreach (ChunkCollection chunkCollection in chunk_collections)
        {
            chunkCollection.RemoveOutOfBoundsAndPopulate(topX, bottomX, topY, bottomY);
        }
    }

    private static void PopulateLightingBuffer()
    {
        if (lightingBuffer is null)
        {
            return;
        }

        for (int i = 0; i < colorBuffer.Length; i++)
        {
            int x = i % lightingBuffer.Width;
            int y = i / lightingBuffer.Width;

            colorBuffer[i] = Lighting.GetColor(
                (int)(Main.screenPosition.X / 16) + x - lighting_buffer_offscreen_range_tiles,
                (int)(Main.screenPosition.Y / 16) + y - lighting_buffer_offscreen_range_tiles
            );
        }

        // SetDataPointerEXT skips some overhead.
        unsafe
        {
            fixed (Color* ptr = &colorBuffer[0])
                lightingBuffer.SetDataPointerEXT(0, null, (IntPtr)ptr, colorBuffer.Length);
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

        bool ended = Main.spriteBatch.TryEnd(out SpriteBatchUtil.SpriteBatchSnapshot snapshot);

        Main.spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
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

        if (ended)
        {
            Main.spriteBatch.BeginWithSnapshot(snapshot);
        }
    }

    private static void TileStateChanged(int i, int j)
    {
        int chunkX = (int)Math.Floor(i / (CHUNK_SIZE / 16.0));
        int chunkY = (int)Math.Floor(j / (CHUNK_SIZE / 16.0));

        List<Point> chunkKeys = new()
        {
            new Point(chunkX, chunkY),
        };

        if (i % CHUNK_SIZE < edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX - 1, chunkY));
        }
        else if (i % CHUNK_SIZE > CHUNK_SIZE - edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX + 1, chunkY));
        }

        if (j % CHUNK_SIZE < edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX, chunkY - 1));
        }
        else if (j % CHUNK_SIZE > CHUNK_SIZE - edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX, chunkY + 1));
        }

        foreach (TileChunkCollection chunkCollection in new[] { non_solid_tiles, solid_tiles })
        {
            foreach (Point chunkKey in chunkKeys)
            {
                if (!chunkCollection.Loaded.ContainsKey(chunkKey))
                {
                    return;
                }

                if (!chunkCollection.NeedsPopulating.Contains(chunkKey))
                {
                    chunkCollection.NeedsPopulating.Add(chunkKey);
                }
            }
        }
    }

    private static void WallStateChanged(int i, int j)
    {
        int chunkX = (int)Math.Floor(i / (CHUNK_SIZE / 16.0));
        int chunkY = (int)Math.Floor(j / (CHUNK_SIZE / 16.0));

        List<Point> chunkKeys = new()
        {
            new Point(chunkX, chunkY),
        };

        if (i % CHUNK_SIZE < edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX - 1, chunkY));
        }
        else if (i % CHUNK_SIZE > CHUNK_SIZE - edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX + 1, chunkY));
        }

        if (j % CHUNK_SIZE < edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX, chunkY - 1));
        }
        else if (j % CHUNK_SIZE > CHUNK_SIZE - edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX, chunkY + 1));
        }

        foreach (Point chunkKey in chunkKeys)
        {
            if (!walls.Loaded.ContainsKey(chunkKey))
            {
                return;
            }

            if (!walls.NeedsPopulating.Contains(chunkKey))
            {
                walls.NeedsPopulating.Add(chunkKey);
            }
        }
    }

    private static void TileVisibilityChanged(DynamicTileVisibilityListener.VisibilityType types)
    {
        foreach (ChunkCollection chunkCollection in chunk_collections)
        {
            foreach (Point chunkKey in chunkCollection.Loaded.Keys)
            {
                chunkCollection.NeedsPopulating.Add(chunkKey);
            }
        }
    }

    private static void CancelVanillaRendering(ILContext il)
    {
        ILCursor c = new(il);

        c.Emit(OpCodes.Ret);
    }

    private static void NewDrawWalls(ILContext il)
    {
        ILCursor c = new(il);

        c.EmitDelegate(() =>
        {
            walls.DoRenderWalls(Main.graphics.GraphicsDevice, screenSizeLightingBuffer, light_map_renderer, Main.spriteBatch.TryEnd(out SpriteBatchUtil.SpriteBatchSnapshot s) ? s : null);

            Main.instance.DrawTileCracks(2, Main.LocalPlayer.hitReplace);
            Main.instance.DrawTileCracks(2, Main.LocalPlayer.hitTile);

            Overlays.Scene.Draw(Main.spriteBatch, RenderLayers.Walls);
        });

        c.Emit(OpCodes.Ret);
    }

    private static void NewDrawNonSolidTiles(ILContext il)
    {
        ILCursor c = new(il);

        c.EmitDelegate(() =>
        {
            // FIX: Last parameter (intoRenderTargets) is TRUE because it is
            // required for special counts to actually clear.
            Main.instance.TilesRenderer.PreDrawTiles(false, false, true);

            non_solid_tiles.DoRenderTiles(Main.graphics.GraphicsDevice, screenSizeLightingBuffer, light_map_renderer, Main.spriteBatch.TryEnd(out SpriteBatchUtil.SpriteBatchSnapshot snapshot) ? snapshot : null);

            Main.instance.DrawTileEntities(false, false, false);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        });

        c.Emit(OpCodes.Ret);
    }

    private static void NewDrawSolidTiles(ILContext il)
    {
        ILCursor c = new(il);

        c.EmitDelegate(() =>
        {
            Main.instance.TilesRenderer.PreDrawTiles(true, false, false);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

            solid_tiles.DoRenderTiles(Main.graphics.GraphicsDevice, screenSizeLightingBuffer, light_map_renderer, Main.spriteBatch.TryEnd(out SpriteBatchUtil.SpriteBatchSnapshot snapshot) ? snapshot : null);

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

            if (debugChunkBorders || debugLightMap)
            {
                Main.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone
                );

                if (debugChunkBorders)
                {
                    const int line_width = 2;
                    const int offset = line_width / 2;

                    foreach (Point chunkKey in solid_tiles.Loaded.Keys)
                    {
                        int chunkX = chunkKey.X * CHUNK_SIZE - (int)Main.screenPosition.X;
                        int chunkY = chunkKey.Y * CHUNK_SIZE - (int)Main.screenPosition.Y;

                        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX - offset, chunkY - offset, CHUNK_SIZE + offset, line_width), Color.Yellow);
                        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX + CHUNK_SIZE - offset, chunkY - offset, line_width, CHUNK_SIZE + offset), Color.Yellow);
                        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX - offset, chunkY + CHUNK_SIZE - offset, CHUNK_SIZE + offset, line_width), Color.Yellow);
                        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX - offset, chunkY - offset, line_width, CHUNK_SIZE + offset), Color.Yellow);
                    }
                }

                if (debugLightMap)
                {
                    Main.spriteBatch.Draw(screenSizeLightingBuffer, Vector2.Zero, Color.White);
                }

                Main.spriteBatch.End();
            }
        });

        c.Emit(OpCodes.Ret);
    }

    private static void HookBeforeDrawingToPopulateLightingBufferAndHandleStuffThatShouldHappenWhenDrawingToScreen(On_Main.orig_DoDraw_WallsTilesNPCs orig, Main self)
    {
        bool old = Main.drawToScreen;
        Main.drawToScreen = true;
        Main.tileBatch.Begin();
        Main.instance.DrawWaters(true);
        Main.tileBatch.End();

        Main.drawToScreen = false;
        int oldRange = Main.offScreenRange;
        Main.offScreenRange = 0;
        Main.tileBatch.Begin();
        Main.instance.DrawBackground();
        Main.tileBatch.End();
        Main.drawToScreen = old;
        Main.offScreenRange = oldRange;

        PopulateLightingBuffer();
        TransferTileSpaceBufferToScreenSpaceBuffer(Main.graphics.GraphicsDevice);
        orig(self);
    }
}