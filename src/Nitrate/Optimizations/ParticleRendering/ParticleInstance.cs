using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace TeamCatalyst.Nitrate.Optimizations.ParticleRendering;

/// <summary>
///     Instance data for a dust particle. Contains a world transformation
///     matrix, subdivision data, and the color used to render the particle.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct ParticleInstance {
    public readonly SimdMatrix World;
    public readonly Vector4 InstanceUv;
    public readonly Vector4 InstanceColor;

    public ParticleInstance(SimdMatrix world, Vector4 instanceUv, Vector4 instanceColor) {
        World = world;
        InstanceUv = instanceUv;
        InstanceColor = instanceColor;
    }
}
