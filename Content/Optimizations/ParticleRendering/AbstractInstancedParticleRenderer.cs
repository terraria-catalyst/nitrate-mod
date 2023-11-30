using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Nitrate.Core.Features.Rendering;

namespace Nitrate.Content.Optimizations.ParticleRendering;

internal abstract class AbstractInstancedParticleRenderer<TParticle> : ModSystem where TParticle : unmanaged
{
    protected VertexBuffer? VertexBuffer;
    protected IndexBuffer? IndexBuffer;
    protected DynamicVertexBuffer? InstanceBuffer;
    protected Texture2D? ParticleAtlas;
    protected readonly TParticle[] Particles;
    private readonly string targetName;

    protected abstract Lazy<Effect> InstanceParticleRenderer { get; }

    protected AbstractInstancedParticleRenderer(int particleCount, string targetName)
    {
        Particles = new TParticle[particleCount];
        this.targetName = targetName;
    }

    public override void Load()
    {
        base.Load();

        GraphicsDevice device = Main.graphics.GraphicsDevice;

        Main.RunOnMainThread(() =>
        {
            VertexBuffer = new VertexBuffer(device, typeof(VertexPositionTexture), ParticleRendererConstants.PARTICLE.Length, BufferUsage.None);
            IndexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, ParticleRendererConstants.PARTICLE_INDICES.Length, BufferUsage.None);

            VertexBuffer.SetData(0, ParticleRendererConstants.PARTICLE, 0, ParticleRendererConstants.PARTICLE.Length, VertexPositionTexture.VertexDeclaration.VertexStride);
            IndexBuffer.SetData(0, ParticleRendererConstants.PARTICLE_INDICES, 0, ParticleRendererConstants.PARTICLE_INDICES.Length);

            InstanceBuffer = new DynamicVertexBuffer(device, ParticleRendererConstants.INSTANCE_DATA, Particles.Length, BufferUsage.None);

            _ = InstanceParticleRenderer.Value;

            ParticleAtlas = MakeAtlas();
        });
    }

    public override void PostSetupContent()
    {
        base.PostSetupContent();

        ModContent.GetInstance<ActionableRenderTargetSystem>().RegisterRenderTarget(targetName);
    }

    public override void Unload()
    {
        base.Unload();

        Main.RunOnMainThread(() =>
        {
            VertexBuffer?.Dispose();
            IndexBuffer?.Dispose();

            InstanceBuffer?.Dispose();
        });
    }

    protected abstract Texture2D MakeAtlas();
}

internal static class ParticleRendererConstants
{
    internal static readonly VertexPositionTexture[] PARTICLE =
    {
        new(Vector3.Zero, Vector2.Zero),
        new(Vector3.UnitX, Vector2.UnitX),
        new(new Vector3(1, 1, 0), Vector2.One),
        new(Vector3.UnitY, Vector2.UnitY),
    };

    internal static readonly short[] PARTICLE_INDICES =
    {
        0, 1, 2, 2, 3, 0,
    };

    public static readonly VertexDeclaration INSTANCE_DATA = new(
        new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Normal, 0),
        new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.Normal, 1),
        new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.Normal, 2),
        new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.Normal, 3),
        new VertexElement(64, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
        new VertexElement(80, VertexElementFormat.Vector4, VertexElementUsage.Color, 1)
    );
}