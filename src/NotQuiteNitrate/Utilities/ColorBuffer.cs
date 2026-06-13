using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Light;

namespace NotQuiteNitrate.Utilities;

public static class ColorBuffer
{
    private static readonly (int x, int y)[] plus_offsets =
    [
        (+0, -1),
        (+0, +1),
        (-1, +0),
        (+1, +0),
    ];

    private static readonly (int x, int y)[] square_offsets =
    [
        (-1, -1),
        (+0, -1),
        (+1, -1),
        (-1, +0),
        (+0, +0),
        (+1, +0),
        (-1, +1),
        (+0, +1),
        (+1, +1),
    ];

    /// <summary>
    ///     Reads a "plus" section to a buffer.
    /// </summary>
    /// <param name="engine">The engine to get the color from.</param>
    /// <param name="x">The center X position.</param>
    /// <param name="y">The center Y position.</param>
    /// <param name="colors">
    ///     The buffer to write to.  Expected to be an ample size to write all
    ///     the colors to.
    /// </param>
    public static void GetPlus(
        ILightingEngine engine,
        int x,
        int y,
        Span<Vector3> colors
    )
    {
        GetBuffer(engine, x, y, colors, plus_offsets);
    }

    /// <summary>
    ///     Reads a 3x3 square section to a buffer.
    /// </summary>
    /// <param name="engine">The engine to get the color from.</param>
    /// <param name="x">The center X position.</param>
    /// <param name="y">The center Y position.</param>
    /// <param name="colors">
    ///     The buffer to write to.  Expected to be an ample size to write all
    ///     the colors to.
    /// </param>
    public static void GetSquare(
        ILightingEngine engine,
        int x,
        int y,
        Span<Vector3> colors
    )
    {
        GetBuffer(engine, x, y, colors, square_offsets);
    }

    private static void GetBuffer(
        ILightingEngine engine,
        int x,
        int y,
        Span<Vector3> colors,
        (int x, int y)[] offsets
    )
    {
        switch (engine)
        {
            case LegacyLighting legacy:
            {
                var realX = x - legacy._requestedRectLeft + Lighting.OffScreenTiles;
                var realY = y - legacy._requestedRectTop + Lighting.OffScreenTiles;

                // TODO: Squeeze out nanoseconds by not duplicating OffScreenTiles?
                var unscaledSize = legacy._camera.UnscaledSize;
                var unscaledX = unscaledSize.X / 16f + Lighting.OffScreenTiles * 2 + 10;
                var unscaledY = unscaledSize.Y / 16f + Lighting.OffScreenTiles * 2;

                // Simplify the condition by taking advantage of obvious logic
                // of a bounding box.
                //    (realX - 1 >= 0 && realY - 1 >= 0 && realX - 1 <= unscaledX && realY - 1 <= unscaledY)
                // && (realX + 1 >= 0 && realY + 1 >= 0 && realX + 1 <= unscaledX && realY + 1 <= unscaledY)
                if (realX - 1 >= 0 && realY - 1 >= 0 && realX + 1 <= unscaledX && realY + 1 <= unscaledY)
                {
                    for (var i = 0; i < offsets.Length; i++)
                    {
                        var offset = offsets[i];
                        var localX = realX + offset.x;
                        var localY = realY + offset.y;

                        var state = legacy._states[localX][localY];
                        colors[i] = new Vector3(state.R, state.G, state.B);
                    }
                }
                else
                {
                    for (var i = 0; i < offsets.Length; i++)
                    {
                        var offset = offsets[i];
                        var localX = realX + offset.x;
                        var localY = realY + offset.y;

                        if (localX < 0 || localY < 0 || localX > unscaledX || localY > unscaledY)
                        {
                            colors[i] = Vector3.Zero;
                        }
                        else
                        {
                            var state = legacy._states[localX][localY];
                            colors[i] = new Vector3(state.R, state.G, state.B);
                        }
                    }
                }

                break;
            }

            case LightingEngine modern:
            {
                if (modern._activeProcessedArea.Contains(x - 1, y - 1)
                 && modern._activeProcessedArea.Contains(x + 1, y + 1))
                {
                    for (var i = 0; i < offsets.Length; i++)
                    {
                        var offset = offsets[i];
                        var localX = x + offset.x;
                        var localY = y + offset.y;

                        colors[i] = modern._activeLightMap[
                            localX - modern._activeProcessedArea.X,
                            localY - modern._activeProcessedArea.Y
                        ];
                    }
                }
                else
                {
                    for (var i = 0; i < offsets.Length; i++)
                    {
                        var offset = offsets[i];
                        var localX = x + offset.x;
                        var localY = y + offset.y;

                        if (!modern._activeProcessedArea.Contains(localX, localY))
                        {
                            colors[i] = Vector3.Zero;
                        }
                        else
                        {
                            colors[i] = modern._activeLightMap[
                                localX - modern._activeProcessedArea.X,
                                localY - modern._activeProcessedArea.Y
                            ];
                        }
                    }
                }

                break;
            }
        }
    }
}
