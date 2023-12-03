using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;

namespace Nitrate.Content.Optimizations.ParticleRendering;

/// <summary>
///     Instance data for a dust particle. Contains a world transformation
///     matrix, subdivision data, and the color used to render the particle.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct DustInstance
{
    public readonly SimdMatrix World;
    public readonly Vector4 InstanceUv;
    public readonly Vector4 InstanceColor;

    public DustInstance(SimdMatrix world, Vector4 instanceUv, Vector4 instanceColor)
    {
        World = world;
        InstanceUv = instanceUv;
        InstanceColor = instanceColor;
    }
}