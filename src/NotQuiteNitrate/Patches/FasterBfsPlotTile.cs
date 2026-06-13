using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;
using NotQuiteNitrate.Utilities.Numerics;
using Terraria;
using Terraria.ModLoader;

namespace NotQuiteNitrate.Patches;

// TODO(perf): Look into scrapping ThreadLocal<T>s in favor of manually managing
//             ThreadStatic values (saves additional get_Value overhead).
// TODO(perf): Look into unrolling neighbor loops and optimizing InWorld
//             (according to Mirs)?

/// <summary>
///     Reimplements the breadth-first search algorithm implemented in
///     PlotTileArea to be ~25x faster.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class FasterBfsPlotTile : ModSystem
{
    private static readonly ThreadLocal<Queue<PackedPoint32>> tl_queue = new(() => new Queue<PackedPoint32>(100));
    private static readonly ThreadLocal<HashSet<PackedPoint32>> tl_visited = new(() => new HashSet<PackedPoint32>(100));

    public override void Load()
    {
        base.Load();

        On_Utils.PlotTileArea += PlotTileArea;
    }

    private static bool PlotTileArea(
        On_Utils.orig_PlotTileArea orig,
        int xInt,
        int yInt,
        Utils.TileActionAttempt plot
    )
    {
        Debug.Assert(xInt <= short.MaxValue && yInt <= short.MaxValue);

        var x = (short)xInt;
        var y = (short)yInt;

        if (!WorldGen.InWorld(x, y))
        {
            return false;
        }

        var queue = tl_queue.Value!;
        {
            queue.Enqueue(new PackedPoint32(x, y));
        }

        var visited = tl_visited.Value!;
        {
            visited.Add(new PackedPoint32(x, y));
        }

        while (queue.TryDequeue(out var current))
        {
            if (!WorldGen.InWorld(current.X, current.Y, 1))
            {
                continue;
            }

            if (!plot(current.X, current.Y))
            {
                continue;
            }

            var neighbors = (Span<PackedPoint32>)
            [
                new PackedPoint32((short)(current.X - 1), current.Y),
                new PackedPoint32((short)(current.X + 1), current.Y),
                new PackedPoint32(current.X, (short)(current.Y - 1)),
                new PackedPoint32(current.X, (short)(current.Y + 1)),
            ];

            foreach (var neighbor in neighbors)
            {
                if (WorldGen.InWorld(neighbor.X, neighbor.Y, 1) && visited.Add(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        visited.Clear();
        return true;
    }
}
