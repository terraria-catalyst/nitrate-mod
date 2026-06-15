using System;
using System.Collections.Generic;
using System.Diagnostics;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMod.Cil;
using Nitrate.API.Listeners;
using Nitrate.API.Threading;
using Nitrate.Core;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader;

namespace Nitrate.Optimizations;

internal sealed class ChunkSystem : ModSystem
{
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
                Main.NewText(Mods.Nitrate.StartupMessages.ExperimentalTileRendererWarning.GetText(), Color.PaleVioletRed);
            }
        }
    }

    // Good sizes include 20, 25, 40, 50, and 100 tiles, as these sizes all multiply evenly into every single default world size's width and height.
    // Smaller chunks are likely better performance-wise as not as many tiles need to be redrawn.
    internal static int ChunkSize => 20 * 16;

    // The number of layers of additional chunks that stay loaded off-screen around the player. Could help improve performance when moving around in one location.
    internal static int ChunkOffscreenBuffer => 1;

    private const int edge_threshold = 3;

    private static readonly TileChunkCollection solid_tiles = new() { SolidLayer = true };
    private static readonly TileChunkCollection non_solid_tiles = new() { SolidLayer = false };
    private static readonly WallChunkCollection walls = new();
    private static readonly ChunkCollection[] chunk_collections = { solid_tiles, non_solid_tiles, walls };
    private static RenderTarget2D? lightingBuffer;
    private static RenderTarget2D? overrideBuffer;
    private static Color[] colorBuffer = Array.Empty<Color>();
    private static Color[] overrideColorBuffer = Array.Empty<Color>();
    // private static RenderTarget2D? screenSizeLightingBuffer;
    // private static RenderTarget2D? screenSizeOverrideBuffer;
    private static bool Enabled => Configuration.UsesExperimentalTileRenderer;
    private static bool debugChunkBorders;
    private static bool debugLightMap;
    private static WrapperShaderData<Assets.Effects.LightMapRenderer.Parameters> lightMapShader = null!;

    private static bool AllowVanillaDrawing => !Enabled || OverrideAllowVanillaDrawing;

    internal static bool OverrideAllowVanillaDrawing { get; set; }

    public override void OnModLoad()
    {
        base.OnModLoad();

        Main.RunOnMainThread(
            () =>
            {
                lightMapShader = Assets.Effects.LightMapRenderer.CreateLightMapRendererPass();
            }
        );

        TileStateChangedListener.OnTileSingleStateChange += TileStateChanged;
        TileStateChangedListener.OnWallSingleStateChange += WallStateChanged;

        DynamicTileVisibilityListener.OnVisibilityChange += TileVisibilityChanged;

        // Disable RenderX methods in relation to tile rendering. These methods
        // are responsible for drawing the tile render target in vanilla.
        IL_Main.RenderTiles += RenderSolidTiles;
        IL_Main.RenderTiles2 += RenderNonSolid;
        IL_Main.RenderWalls += RenderWalls;

        IL_TileDrawing.Draw += AllowRunningDetoursOnThisMethodWithoutRunningIt;
        IL_WallDrawing.DrawWalls += AllowRunningDetoursOnThisMethodWithoutRunningIt;

        IL_Main.DoDraw += il =>
        {
            var c = new ILCursor(il);

            c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<Main>(nameof(Main.DoLightTiles)));
            c.EmitDelegate(
                () =>
                {
                    PopulateLightingBuffer();
                    // TransferTileSpaceBufferToScreenSpaceBuffer(Main.graphics.GraphicsDevice);
                }
            );
        };

        On_Main.DoDraw_Tiles_Solid += [StackTraceHidden](orig, self) =>
        {
            orig(self);
            DebugDrawLightmap();
        };

        Main.RunOnMainThread(
            () =>
            {
                lightingBuffer = new RenderTarget2D(
                    Main.graphics.GraphicsDevice,
                    (int)Math.Ceiling(Main.instance.tileTarget.Width / 16f),
                    (int)Math.Ceiling(Main.instance.tileTarget.Height / 16f)
                );

                overrideBuffer = new RenderTarget2D(
                    Main.graphics.GraphicsDevice,
                    (int)Math.Ceiling(Main.instance.tileTarget.Width / 16f),
                    (int)Math.Ceiling(Main.instance.tileTarget.Height / 16f)
                );

                colorBuffer = new Color[lightingBuffer.Width * lightingBuffer.Height];
                overrideColorBuffer = new Color[lightingBuffer.Width * lightingBuffer.Height];

                foreach (var chunkCollection in chunk_collections)
                {
                    chunkCollection.ScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.instance.tileTarget.Width, Main.instance.tileTarget.Height);
                }

                // screenSizeLightingBuffer = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.instance.tileTarget.Width, Main.instance.tileTarget.Width);
                // screenSizeOverrideBuffer = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.instance.tileTarget.Width, Main.instance.tileTarget.Width);

                // By default, Terraria has this set to DiscardContents. This means that switching RTs erases the contents of the backbuffer if done mid-draw.
                Main.graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
                Main.graphics.ApplyChanges();
            }
        );

        Main.OnRenderTargetsInitialized += ResizeBuffers;
    }

    private static void AllowRunningDetoursOnThisMethodWithoutRunningIt(ILContext il)
    {
        var c = new ILCursor(il);

        var actualMethodLabel = c.DefineLabel();
        c.EmitDelegate(() => AllowVanillaDrawing);
        c.EmitBrtrue(actualMethodLabel);
        c.EmitRet();
        c.MarkLabel(actualMethodLabel);
    }

    private static void ResizeBuffers(int width, int height)
    {
        lightingBuffer?.Dispose();

        lightingBuffer = new RenderTarget2D(
            Main.graphics.GraphicsDevice,
            (int)Math.Ceiling(Main.instance.tileTarget.Width / 16f),
            (int)Math.Ceiling(Main.instance.tileTarget.Height / 16f)
        );

        overrideBuffer?.Dispose();

        overrideBuffer = new RenderTarget2D(
            Main.graphics.GraphicsDevice,
            (int)Math.Ceiling(Main.instance.tileTarget.Width / 16f),
            (int)Math.Ceiling(Main.instance.tileTarget.Height / 16f)
        );

        foreach (var chunkCollection in chunk_collections)
        {
            chunkCollection.ScreenTarget?.Dispose();
            chunkCollection.ScreenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.instance.tileTarget.Width, Main.instance.tileTarget.Height);
        }

        // screenSizeLightingBuffer?.Dispose();
        // screenSizeLightingBuffer = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);

        // screenSizeOverrideBuffer?.Dispose();
        // screenSizeOverrideBuffer = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);

        colorBuffer = new Color[lightingBuffer.Width * lightingBuffer.Height];
        overrideColorBuffer = new Color[lightingBuffer.Width * lightingBuffer.Height];
    }

    public override void OnWorldUnload()
    {
        base.OnWorldUnload();

        // Clone these outside of the delegate to ensure the delegate captures
        // an uncleared collection. This probably prevents a memory leak.
        List<Chunk> loadedChunksClone = new();

        foreach (var chunkCollection in chunk_collections)
        {
            loadedChunksClone.AddRange(chunkCollection.Loaded.Values);
        }

        Main.RunOnMainThread(
            () =>
            {
                foreach (var chunk in loadedChunksClone)
                {
                    chunk.Dispose();
                }
            }
        );

        foreach (var chunkCollection in chunk_collections)
        {
            chunkCollection.Loaded.Clear();
            chunkCollection.NeedsPopulating.Clear();
        }
    }

    public override void Unload()
    {
        base.Unload();

        foreach (var chunkCollection in chunk_collections)
        {
            chunkCollection.DisposeAllChunks();
        }

        Main.OnRenderTargetsInitialized -= ResizeBuffers;

        Main.RunOnMainThread(
            () =>
            {
                lightingBuffer?.Dispose();
                lightingBuffer = null;

                overrideBuffer?.Dispose();
                overrideBuffer = null;

                // screenSizeLightingBuffer?.Dispose();
                // screenSizeLightingBuffer = null;

                // screenSizeOverrideBuffer?.Dispose();
                // screenSizeOverrideBuffer = null;
            }
        );
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

    private static void PopulateLightingBuffer()
    {
        if (lightingBuffer is null || overrideBuffer is null)
        {
            return;
        }

        var checkDynamicLighting = Main.LocalPlayer.dangerSense || Main.LocalPlayer.findTreasure || Main.LocalPlayer.biomeSight;
        FasterParallel.For(
            0,
            colorBuffer.Length,
            (inclusive, exclusive, _) =>
            {
                for (var i = inclusive; i < exclusive; i++)
                {
                    var x = i % lightingBuffer.Width;
                    var y = i / lightingBuffer.Width;

                    // FIXME?
                    var tileX = (int)Math.Floor(Main.sceneTilePos.X / 16f) + x;
                    var tileY = (int)Math.Floor(Main.sceneTilePos.Y / 16f) + y;

                    colorBuffer[i] = Lighting.GetColor(tileX, tileY);

                    var overrideColor = Color.Transparent;

                    if (checkDynamicLighting)
                    {
                        solid_tiles.TryGetDynamicLighting(tileX, tileY, colorBuffer[i], ref overrideColor);
                    }

                    overrideColorBuffer[i] = overrideColor;
                }
            }
        );

        // SetDataPointerEXT skips some overhead.
        unsafe
        {
            fixed (Color* ptr = &colorBuffer[0])
            {
                lightingBuffer.SetDataPointerEXT(0, null, (nint)ptr, colorBuffer.Length);
            }

            if (!checkDynamicLighting)
            {
                return;
            }

            fixed (Color* ptr = &overrideColorBuffer[0])
            {
                overrideBuffer.SetDataPointerEXT(0, null, (nint)ptr, overrideColorBuffer.Length);
            }
        }
    }

    private static void TileStateChanged(int i, int j)
    {
        var chunkX = (int)Math.Floor(i / (ChunkSize / 16.0));
        var chunkY = (int)Math.Floor(j / (ChunkSize / 16.0));

        List<Point> chunkKeys = new()
        {
            new Point(chunkX, chunkY),
        };

        if (i % ChunkSize < edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX - 1, chunkY));
        }
        else if (i % ChunkSize > ChunkSize - edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX + 1, chunkY));
        }

        if (j % ChunkSize < edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX, chunkY - 1));
        }
        else if (j % ChunkSize > ChunkSize - edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX, chunkY + 1));
        }

        foreach (var chunkCollection in new[] { non_solid_tiles, solid_tiles })
        {
            foreach (var chunkKey in chunkKeys)
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
        var chunkX = (int)Math.Floor(i / (ChunkSize / 16.0));
        var chunkY = (int)Math.Floor(j / (ChunkSize / 16.0));

        List<Point> chunkKeys = new()
        {
            new Point(chunkX, chunkY),
        };

        if (i % ChunkSize < edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX - 1, chunkY));
        }
        else if (i % ChunkSize > ChunkSize - edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX + 1, chunkY));
        }

        if (j % ChunkSize < edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX, chunkY - 1));
        }
        else if (j % ChunkSize > ChunkSize - edge_threshold)
        {
            chunkKeys.Add(new Point(chunkX, chunkY + 1));
        }

        foreach (var chunkKey in chunkKeys)
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
        foreach (var chunkCollection in chunk_collections)
        {
            foreach (var chunkKey in chunkCollection.Loaded.Keys)
            {
                chunkCollection.NeedsPopulating.Add(chunkKey);
            }
        }
    }

    private static void RenderWalls(ILContext il)
    {
        var c = new ILCursor(il);

        while (c.TryGotoNext(MoveType.Before, x => x.MatchCallOrCallvirt<Main>(nameof(Main.DrawWalls))))
        {
            c.Remove();
            c.EmitDelegate(
                (Main self) =>
                {
                    if (!Enabled)
                    {
                        self.DrawWalls();
                        return;
                    }

                    walls.DoRenderWalls(Main.graphics.GraphicsDevice, lightingBuffer, overrideBuffer, lightMapShader);
                }
            );
        }
    }

    private static void RenderNonSolid(ILContext il)
    {
        var c = new ILCursor(il);

        while (c.TryGotoNext(MoveType.Before, x => x.MatchCallOrCallvirt<Main>(nameof(Main.DrawTiles))))
        {
            c.Remove();
            c.EmitDelegate(
                (Main self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets, int waterStyleOverride) =>
                {
                    if (!Enabled)
                    {
                        self.DrawTiles(solidLayer, forRenderTargets, intoRenderTargets, waterStyleOverride);
                        return;
                    }

                    non_solid_tiles.DoRenderTiles(Main.graphics.GraphicsDevice, lightingBuffer, overrideBuffer, lightMapShader);
                }
            );
        }
    }

    private static void RenderSolidTiles(ILContext il)
    {
        var c = new ILCursor(il);

        while (c.TryGotoNext(MoveType.Before, x => x.MatchCallOrCallvirt<Main>(nameof(Main.DrawTiles))))
        {
            c.Remove();
            c.EmitDelegate(
                (Main self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets, int waterStyleOverride) =>
                {
                    if (!Enabled)
                    {
                        self.DrawTiles(solidLayer, forRenderTargets, intoRenderTargets, waterStyleOverride);
                        return;
                    }

                    solid_tiles.DoRenderTiles(Main.graphics.GraphicsDevice, lightingBuffer, overrideBuffer, lightMapShader);
                }
            );
        }
    }

    private static void DebugDrawLightmap()
    {
        if (!debugChunkBorders && !debugLightMap)
        {
            return;
        }

        var sb = Main.spriteBatch;
        using (sb.Scope())
        {
            sb.Begin(
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

                foreach (var chunkKey in solid_tiles.Loaded.Keys)
                {
                    var chunkX = chunkKey.X * ChunkSize - (int)Main.screenPosition.X;
                    var chunkY = chunkKey.Y * ChunkSize - (int)Main.screenPosition.Y;

                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX - offset, chunkY - offset, ChunkSize + offset, line_width), Color.Yellow);
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX + ChunkSize - offset, chunkY - offset, line_width, ChunkSize + offset), Color.Yellow);
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX - offset, chunkY + ChunkSize - offset, ChunkSize + offset, line_width), Color.Yellow);
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(chunkX - offset, chunkY - offset, line_width, ChunkSize + offset), Color.Yellow);
                }
            }

            /*
            if (debugLightMap)
            {
                Main.spriteBatch.Draw(screenSizeLightingBuffer, Vector2.Zero, Color.White);
            }
            */

            sb.End();
        }
    }
}
