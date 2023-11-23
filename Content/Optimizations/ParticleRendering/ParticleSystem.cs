using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Diagnostics;
using Terraria;
using Terraria.ModLoader;

namespace Zenith.Content.Optimizations.ParticleRendering;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class ParticleSystem : ModSystem
{
    private DynamicVertexBuffer _vertexBuffer;
    private DynamicIndexBuffer _indexBuffer;
    private DynamicVertexBuffer _vector4Buffer;
    private Effect _effect;
    private RenderTarget2D _particlePositionVelocityMap;
    private RenderTarget2D _particlePositionVelocityMapCopy;

    private static readonly VertexPositionTexture[] Particle =
    {
        new(new Vector3(0, 0, 0), Vector2.Zero),
        new(new Vector3(1920, 0, 0), Vector2.UnitX),
        new(new Vector3(1920, 1080, 0), Vector2.One),
        new(new Vector3(0, 1080, 0), Vector2.UnitY),
    };

    private static readonly short[] ParticleIndices =
    {
        0, 1, 2, 2, 3, 0
    };

    private static readonly VertexDeclaration Vector4Buffer = new(
        new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0)
    );

    public override void Load()
    {
        GraphicsDevice device = Main.graphics.GraphicsDevice;

        Main.RunOnMainThread(() =>
        {
            _vertexBuffer = new DynamicVertexBuffer(device, typeof(VertexPositionTexture), Particle.Length, BufferUsage.None);
            _indexBuffer = new DynamicIndexBuffer(device, IndexElementSize.SixteenBits, ParticleIndices.Length, BufferUsage.None);

            _vertexBuffer.SetData(0, Particle, 0, Particle.Length, VertexPositionTexture.VertexDeclaration.VertexStride, SetDataOptions.Discard);
            _indexBuffer.SetData(0, ParticleIndices, 0, ParticleIndices.Length, SetDataOptions.Discard);

            _effect = Mod.Assets.Request<Effect>("Assets/Effects/ParticleRenderer", AssetRequestMode.ImmediateLoad).Value;

            _particlePositionVelocityMap = new RenderTarget2D(device, 2048, 2048, false, SurfaceFormat.Vector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _particlePositionVelocityMapCopy = new RenderTarget2D(device, 2048, 2048, false, SurfaceFormat.Vector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            Vector4[] data = new Vector4[2048 * 2048];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new Vector4(0, 0, 1, 1);
            }

            _particlePositionVelocityMap.SetData(data, 0, data.Length);
            _particlePositionVelocityMapCopy.SetData(data, 0, data.Length);

            _vector4Buffer = new DynamicVertexBuffer(device, Vector4Buffer, 2048 * 2048, BufferUsage.None);
        });
    }

    public override void Unload()
    {
        Main.RunOnMainThread(() =>
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();

            _particlePositionVelocityMap?.Dispose();
            _particlePositionVelocityMapCopy?.Dispose();
        });
    }

    Stopwatch sw = new();

    public override void PreUpdateDusts()
    {
        sw.Start();

        GraphicsDevice device = Main.graphics.GraphicsDevice;

        device.RasterizerState = RasterizerState.CullNone;

        Matrix transform = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);

        _effect.Parameters["transformMatrix"].SetValue(transform);
        _effect.Parameters["positionVelocityMap"].SetValue(_particlePositionVelocityMap);

        device.SetVertexBuffer(_vertexBuffer);
        device.Indices = _indexBuffer;

        RenderTargetBinding[] bindings = device.GetRenderTargets();

        device.SetRenderTarget(_particlePositionVelocityMapCopy);

        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _vertexBuffer.VertexCount, 0, _indexBuffer.IndexCount / 3);
        }

        device.SetRenderTargets(bindings);

        (_particlePositionVelocityMap, _particlePositionVelocityMapCopy) = (_particlePositionVelocityMapCopy, _particlePositionVelocityMap);
    }
}