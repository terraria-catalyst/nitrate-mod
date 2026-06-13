/*using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using JetBrains.Annotations;

using Terraria;
using Terraria.Graphics.Light;
using Terraria.Map;
using Terraria.ModLoader;

using NotQuiteNitrate.Utilities;
using NotQuiteNitrate.Utilities.Numerics;

namespace NotQuiteNitrate.Patches;

/// <summary>
///     Rewrites <see cref="WorldMap"/> to used a chunked backing store.
/// </summary>
[Autoload(Side = ModSide.Client)]
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
internal sealed class LightweightWorldMap : ModSystem
{
    /// <summary>
    ///     A partial reimplementation of <see cref="WorldMap"/> that
    /// </summary>
    private sealed class ChunkedWorldMap : WorldMap
    {
        private const int chunk_width  = 40;
        private const int chunk_height = 40;

        private readonly Dictionary<PackedPoint16, MapTile[]> mapTiles = [];

        public new MapTile this[int x, int y] => GetTile(x, y);

        public ChunkedWorldMap(int maxWidth, int maxHeight) : base(maxWidth, maxHeight)
        {
            _tiles = null;
        }

        private MapTile GetTile(int x, int y)
        {
            Debug.Assert(
                x is >= 0 and <= ushort.MaxValue
             && y is >= 0 and <= ushort.MaxValue
            );

            var chunkX = x / chunk_width;
            var chunkY = y / chunk_height;
            {
                Debug.Assert(chunkX is >= 0 and <= byte.MaxValue);
                Debug.Assert(chunkY is >= 0 and <= byte.MaxValue);
            }

            var chunk = new PackedPoint16((byte)chunkX, (byte)chunkY);
            {
                if (!mapTiles.TryGetValue(chunk, out var tiles))
                {
                    return default(MapTile);
                }

                var tileX = x % chunk_width;
                var tileY = y % chunk_height;
                {
                    Debug.Assert(tileX is >= 0 and < chunk_width);
                    Debug.Assert(tileY is >= 0 and < chunk_height);
                }

                return tiles[tileX + tileY * chunk_width];
            }
        }

        private ref MapTile GetOrInitTile(int x, int y)
        {
            Debug.Assert(
                x is >= 0 and <= ushort.MaxValue
             && y is >= 0 and <= ushort.MaxValue
            );

            var chunkX = x / chunk_width;
            var chunkY = y / chunk_height;
            {
                Debug.Assert(chunkX is >= 0 and <= byte.MaxValue);
                Debug.Assert(chunkY is >= 0 and <= byte.MaxValue);
            }

            var chunk = new PackedPoint16((byte)chunkX, (byte)chunkY);
            {
                if (!mapTiles.TryGetValue(chunk, out var tiles))
                {
                    tiles           = new MapTile[chunk_width * chunk_height];
                    mapTiles[chunk] = tiles;
                }

                var tileX = x % chunk_width;
                var tileY = y % chunk_height;
                {
                    Debug.Assert(tileX is >= 0 and < chunk_width);
                    Debug.Assert(tileY is >= 0 and < chunk_height);
                }

                return ref tiles[tileX + tileY * chunk_width];
            }
        }

        public new void ConsumeUpdate(int x, int y)
        {
            GetOrInitTile(x, y).IsChanged = false;
        }

        public new void Update(int x, int y, byte light)
        {
            GetOrInitTile(x, y) = MapHelper.CreateMapTile(x, y, light);
        }

        public new void SetTile(int x, int y, ref MapTile tile)
        {
            GetOrInitTile(x, y) = tile;
        }

        public new bool IsRevealed(int x, int y)
        {
            return GetTile(x, y).Light > 0;
        }

        public new bool UpdateLighting(int x, int y, byte light)
        {
            // Permit uninitialized tiles here.  If light == 0, we don't need to
            // initialize it.
            var other = GetTile(x, y);
            if (light == 0 && other.Light == 0)
            {
                return false;
            }

            var mapTile = MapHelper.CreateMapTile(x, y, Math.Max(other.Light, light));

            // This check is somewhat strange with the new system, but we can
            // allow it.
            if (mapTile.Equals(ref other))
            {
                return false;
            }

            SetTile(x, y, ref mapTile);
            return true;
        }

        public new bool UpdateType(int x, int y)
        {
            var other   = GetTile(x, y);
            var newTile = MapHelper.CreateMapTile(x, y, other.Light);
            if (newTile.Equals(ref other))
            {
                return false;
            }

            SetTile(x, y, ref newTile);
            return true;
        }

        public new void Clear()
        {
            mapTiles.Clear();
        }

        public new void ClearEdges()
        {
            // TODO(perf): clear entire chunks

            for (var x = 0; x < MaxWidth; x++)
            for (var y = 0; y < 40; y++)
            {
                GetOrInitTile(x, y).Clear();
            }

            for (var x = 0; x < MaxWidth; x++)
            for (var y = MaxHeight - 40; y < MaxHeight; y++)
            {
                GetOrInitTile(x, y).Clear();
            }

            for (var x = 0; x < 40; x++)
            for (var y = 40; y < MaxHeight - 40; y++)
            {
                GetOrInitTile(x, y).Clear();
            }

            for (var x = MaxWidth - 40; x < MaxWidth; x++)
            for (var y = 40; y < MaxHeight - 40; y++)
            {
                GetOrInitTile(x, y).Clear();
            }
        }
    }

    public override void Load()
    {
        base.Load();

        Main.Map = new ChunkedWorldMap(Main.maxTilesX, Main.maxTilesY);
        {
            On_WorldMap.ConsumeUpdate += (orig, self, x, y) =>
            {
                if (self is ChunkedWorldMap chunkedWorldMap)
                {
                    chunkedWorldMap.ConsumeUpdate(x, y);
                }
                else
                {
                    orig(self, x, y);
                }
            };

            On_WorldMap.Update += (orig, self, x, y, light) =>
            {
                if (self is ChunkedWorldMap chunkedWorldMap)
                {
                    chunkedWorldMap.Update(x, y, light);
                }
                else
                {
                    orig(self, x, y, light);
                }
            };

            On_WorldMap.SetTile += (On_WorldMap.orig_SetTile orig, WorldMap self, int x, int y, ref MapTile tile) =>
            {
                if (self is ChunkedWorldMap chunkedWorldMap)
                {
                    chunkedWorldMap.SetTile(x, y, ref tile);
                }
                else
                {
                    orig(self, x, y, ref tile);
                }
            };

            On_WorldMap.IsRevealed += (orig, self, x, y) =>
            {
                if (self is ChunkedWorldMap chunkedWorldMap)
                {
                    return chunkedWorldMap.IsRevealed(x, y);
                }

                return orig(self, x, y);
            };

            On_WorldMap.UpdateLighting += (orig, self, x, y, light) =>
            {
                if (self is ChunkedWorldMap chunkedWorldMap)
                {
                    return chunkedWorldMap.UpdateLighting(x, y, light);
                }

                return orig(self, x, y, light);
            };

            On_WorldMap.UpdateType += (orig, self, x, y) =>
            {
                if (self is ChunkedWorldMap chunkedWorldMap)
                {
                    return chunkedWorldMap.UpdateType(x, y);
                }

                return orig(self, x, y);
            };

            On_WorldMap.Clear += (orig, self) =>
            {
                if (self is ChunkedWorldMap chunkedWorldMap)
                {
                    chunkedWorldMap.Clear();
                }
                else
                {
                    orig(self);
                }
            };

            On_WorldMap.ClearEdges += (orig, self) =>
            {
                if (self is ChunkedWorldMap chunkedWorldMap)
                {
                    chunkedWorldMap.ClearEdges();
                }
                else
                {
                    orig(self);
                }
            };

            var type    = typeof(WorldMap);
            var getItem = type.GetMethod("get_Item", BindingFlags.Public | BindingFlags.Instance);
            MonoModHooks.Add(
                getItem,
                (Func<WorldMap, int, int, MapTile> orig, WorldMap self, int x, int y) =>
                {
                    if (self is ChunkedWorldMap chunkedWorldMap)
                    {
                        return chunkedWorldMap[x, y];
                    }

                    return orig(self, x, y);
                }
            );
        }

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var methodsToReJit = new[]
        {
            typeof(Main).GetMethod("DrawToMap",         flags)!,
            typeof(Main).GetMethod("DrawToMap_Section", flags)!,
            typeof(Main).GetMethod("DrawMap",           flags)!,
            typeof(WorldGen).GetMethod("UpdateMapTile", flags)!,
            typeof(MapHelper).GetMethod("CreateMapTile",   flags)!,
            typeof(MapHelper).GetMethod("InternalSaveMap", flags)!,
            typeof(LegacyLighting).GetMethod("TryUpdatingMapWithLight", flags)!,
        };
        foreach (var method in methodsToReJit)
        {
            ReJit.Force(method);
        }
    }

    public override void Unload()
    {
        base.Unload();

        Main.Map = new WorldMap(Main.maxTilesX, Main.maxTilesY);
    }
}*/


