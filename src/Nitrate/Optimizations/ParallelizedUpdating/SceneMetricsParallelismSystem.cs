using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoMod.Cil;
using Terraria;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace TeamCatalyst.Nitrate.Optimizations.ParallelizedUpdating;

/// <summary>
///        Makes the lighting engine's SceneMetrics run async.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal class SceneMetricsParallelismSystem : ModSystem {
    private static volatile SilentSceneMetrics? loadedMetrics;
    public override void OnModLoad() {
        base.OnModLoad();

        On_LightingEngine.ProcessArea += On_ProcessArea;
    }

    private static void On_ProcessArea(On_LightingEngine.orig_ProcessArea original, LightingEngine self, Rectangle area) {
        if (!Configuration.UsesAsyncSceneMetrics) {
            original(self, area);
            return;
        }
        
        // Update the metrics
        if (self._state == LightingEngine.EngineState.ExportMetrics) {
            if (loadedMetrics != null) {
                self._timer.Start();
                TimeLogger.LightingTime(0, 0.0);
                loadedMetrics.FlushPendingTileUpdates();
                Main.renderCount = (Main.renderCount + 1) % 4;
                
                Main.SceneMetrics = loadedMetrics;
                SystemLoader.ResetNearbyTileEffects();
                SystemLoader.TileCountsAvailable(loadedMetrics._tileCounts);
                loadedMetrics = null;
                self.IncrementState();
                TimeLogger.LightingTime(2, self._timer.Elapsed.TotalMilliseconds);
                self._timer.Reset();
            }
            else {
                original(self, area);
            }
            return;
        }
        
        // Start processing new metrics
        if (self._state == LightingEngine.EngineState.Scan) {
            // Run profiling timer as normal
            self._timer.Start();
            TimeLogger.LightingTime(0, 0.0);
            
            Main.renderCount = (Main.renderCount + 1) % 4;
            
            // Start a lookup for the scene.
            Rectangle copy = new(area.X, area.Y, area.Width, area.Height);
            copy.Inflate(28, 28);
            Task task = new Task(() => {
                SilentSceneMetrics metrics = new SilentSceneMetrics();
                metrics.ScanAndExportToMain(new SceneMetricsScanSettings {
                    VisualScanArea = copy,
                    BiomeScanCenterPositionInWorld = Main.LocalPlayer.Center,
                    ScanOreFinderData = Main.LocalPlayer.accOreFinder
                });

                loadedMetrics = metrics;
            }); 
            task.Start();
            self.ProcessScan(area);
            self.IncrementState();
            
            TimeLogger.LightingTime(3, self._timer.Elapsed.TotalMilliseconds);
            self._timer.Reset();
            return;
        }
        
        original(self, area);
    }

    /// <summary>
    ///        Used to scan metrics of a scene and skip calling the <see cref="SystemLoader.ResetNearbyTileEffects"/>
    ///        and <see cref="SystemLoader.TileCountsAvailable"/> methods.
    /// </summary>
    private class SilentSceneMetrics : SceneMetrics {
        private readonly List<Tuple<int, int, int, bool>> nearbyEffectPendingUpdates = new();

        /// <summary>
        ///        Takes all pending tile updates and processes them.
        /// </summary>
        public void FlushPendingTileUpdates() {
            foreach (var tuple in nearbyEffectPendingUpdates) {
                TileLoader.NearbyEffects(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
            }
        }
        
        public new void ScanAndExportToMain(SceneMetricsScanSettings settings) {
            Reset();
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            if (settings.ScanOreFinderData)
                _oreFinderTileLocations.Clear();

            if (settings.BiomeScanCenterPositionInWorld.HasValue) {
                Point point = settings.BiomeScanCenterPositionInWorld.Value.ToTileCoordinates();
                Rectangle tileRectangle = new Rectangle(point.X - Main.buffScanAreaWidth / 2, point.Y - Main.buffScanAreaHeight / 2, Main.buffScanAreaWidth, Main.buffScanAreaHeight);
                tileRectangle = WorldUtils.ClampToWorld(tileRectangle);
                for (int i = tileRectangle.Left; i < tileRectangle.Right; i++) {
                    for (int j = tileRectangle.Top; j < tileRectangle.Bottom; j++) {
                        Tile tile = Main.tile[i, j];
                        if (tile == null)
                            continue;

                        if (!tile.active()) {
                            if (tile.liquid > 0)
                                _liquidCounts[tile.liquidType()]++;

                            continue;
                        }

                        if (!TileID.Sets.isDesertBiomeSand[tile.type] || !WorldGen.oceanDepths(i, j))
                            _tileCounts[tile.type]++;

                        if (tile.type == 215 && tile.frameY < 36)
                            HasCampfire = true;

                        if (tile.type == 49 && tile.frameX < 18)
                            num++;

                        if (tile.type == 372 && tile.frameX < 18)
                            num2++;

                        if (tile.type == 646 && tile.frameX < 18)
                            num3++;

                        if (tile.type == 405 && tile.frameX < 54)
                            HasCampfire = true;

                        if (tile.type == 506 && tile.frameX < 72)
                            HasCatBast = true;

                        if (tile.type == 42 && tile.frameY >= 324 && tile.frameY <= 358)
                            HasHeartLantern = true;

                        if (tile.type == 42 && tile.frameY >= 252 && tile.frameY <= 286)
                            HasStarInBottle = true;

                        if (tile.type == 91 && (tile.frameX >= 396 || tile.frameY >= 54)) {
                            int num4 = tile.frameX / 18 - 21;
                            for (int num5 = tile.frameY; num5 >= 54; num5 -= 54) {
                                num4 += 90;
                                num4 += 21;
                            }

                            int num6 = Item.BannerToItem(num4);
                            if (ItemID.Sets.BannerStrength.IndexInRange(num6) && ItemID.Sets.BannerStrength[num6].Enabled) {
                                NPCBannerBuff[num4] = true;
                                hasBanner = true;
                            }
                        }

                        if (settings.ScanOreFinderData && Main.tileOreFinderPriority[tile.type] != 0)
                            _oreFinderTileLocations.Add(new Point(i, j));

                        ModTile? modTile = TileLoader.GetTile(tile.type);
                        if (modTile != null) {
                            nearbyEffectPendingUpdates.Add(new(i, j, tile.type, false));
                        }
                    }
                }
            }

            if (settings.VisualScanArea.HasValue) {
                Rectangle rectangle = WorldUtils.ClampToWorld(settings.VisualScanArea.Value);
                for (int k = rectangle.Left; k < rectangle.Right; k++) {
                    for (int l = rectangle.Top; l < rectangle.Bottom; l++) {
                        Tile tile2 = Main.tile[k, l];
                        if (tile2 == null || !tile2.active())
                            continue;

                        if (TileID.Sets.Clock[tile2.type])
                            HasClock = true;

                        switch (tile2.type) {
                            case 139:
                                if (tile2.frameX >= 36)
                                    ActiveMusicBox = tile2.frameY / 36;
                                break;
                            case 207:
                                if (tile2.frameY >= 72) {
                                    switch (tile2.frameX / 36) {
                                        case 0:
                                            ActiveFountainColor = 0;
                                            break;
                                        case 1:
                                            ActiveFountainColor = 12;
                                            break;
                                        case 2:
                                            ActiveFountainColor = 3;
                                            break;
                                        case 3:
                                            ActiveFountainColor = 5;
                                            break;
                                        case 4:
                                            ActiveFountainColor = 2;
                                            break;
                                        case 5:
                                            ActiveFountainColor = 10;
                                            break;
                                        case 6:
                                            ActiveFountainColor = 4;
                                            break;
                                        case 7:
                                            ActiveFountainColor = 9;
                                            break;
                                        case 8:
                                            ActiveFountainColor = 8;
                                            break;
                                        case 9:
                                            ActiveFountainColor = 6;
                                            break;
                                        default:
                                            ActiveFountainColor = -1;
                                            break;
                                    }
                                }
                                break;
                            case 410:
                                if (tile2.frameY >= 56) {
                                    int activeMonolithType = tile2.frameX / 36;
                                    ActiveMonolithType = activeMonolithType;
                                }
                                break;
                            case 509:
                                if (tile2.frameY >= 56)
                                    ActiveMonolithType = 4;
                                break;
                            case 480:
                                if (tile2.frameY >= 54)
                                    BloodMoonMonolith = true;
                                break;
                            // Extra extra patch context.
                            case 657:
                                if (tile2.frameY >= 54)
                                    EchoMonolith = true;
                                break;
                            // Extra patch context.
                            case 658: {
                                int shimmerMonolithState = tile2.frameY / 54;
                                ShimmerMonolithState = shimmerMonolithState;
                                break;
                            }
                        }

                        // This does not use TileLoader.IsModMusicBox because it needs the *exact* frameY for the second dict lookup
                        if (MusicLoader.tileToMusic.ContainsKey(tile2.type) && MusicLoader.tileToMusic[tile2.type].ContainsKey(tile2.frameY) && tile2.frameX == 36)
                            ActiveMusicBox = MusicLoader.tileToMusic[tile2.type][tile2.frameY];

                        ModTile? modTile = TileLoader.GetTile(tile2.type);
                        if (modTile != null) {
                            nearbyEffectPendingUpdates.Add(new(k, l, tile2.type, true));
                        }
                    }
                }
            }

            WaterCandleCount = num;
            PeaceCandleCount = num2;
            ShadowCandleCount = num3;
            ExportTileCountsToMain();
            CanPlayCreditsRoll = ActiveMusicBox == 85;
            if (settings.ScanOreFinderData)
                UpdateOreFinderData();
        }
    }
}