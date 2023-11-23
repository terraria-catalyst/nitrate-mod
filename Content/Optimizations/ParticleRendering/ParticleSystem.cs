using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Diagnostics;
using Terraria;
using Terraria.ModLoader;

namespace Zenith.Content.Optimizations.ParticleRendering;

public class ParticleSystem : ModSystem
{
    private DynamicVertexBuffer vertexBuffer;

    private DynamicIndexBuffer indexBuffer;

    private DynamicVertexBuffer vector4Buffer;

    private Effect effect;

    private RenderTarget2D particlePositionVelocityMap;

    private RenderTarget2D particlePositionVelocityMapCopy;

    private static nint deviceHandle;

    private static nint textureHandle;

    private static nint bufferHandle;

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
            vertexBuffer = new DynamicVertexBuffer(device, typeof(VertexPositionTexture), Particle.Length, BufferUsage.None);
            indexBuffer = new DynamicIndexBuffer(device, IndexElementSize.SixteenBits, ParticleIndices.Length, BufferUsage.None);

            vertexBuffer.SetData(0, Particle, 0, Particle.Length, VertexPositionTexture.VertexDeclaration.VertexStride, SetDataOptions.Discard);
            indexBuffer.SetData(0, ParticleIndices, 0, ParticleIndices.Length, SetDataOptions.Discard);

            effect = Mod.Assets.Request<Effect>("Assets/Effects/ParticleRenderer", AssetRequestMode.ImmediateLoad).Value;

            particlePositionVelocityMap = new RenderTarget2D(device, 2048, 2048, false, SurfaceFormat.Vector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            particlePositionVelocityMapCopy = new RenderTarget2D(device, 2048, 2048, false, SurfaceFormat.Vector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            Vector4[] data = new Vector4[2048 * 2048];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new Vector4(0, 0, 1, 1);
            }

            particlePositionVelocityMap.SetData(data, 0, data.Length);
            particlePositionVelocityMapCopy.SetData(data, 0, data.Length);

            vector4Buffer = new DynamicVertexBuffer(device, Vector4Buffer, 2048 * 2048, BufferUsage.None);

            deviceHandle = Main.instance.GraphicsDevice.GLDevice;
            textureHandle = particlePositionVelocityMap.texture;
            bufferHandle = vector4Buffer.buffer;
        });
    }

    public override void Unload()
    {
        Main.RunOnMainThread(() =>
        {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();

            particlePositionVelocityMap?.Dispose();
            particlePositionVelocityMapCopy?.Dispose();
        });
    }

    Stopwatch sw = new();

    public override void PreUpdateDusts()
    {
        sw.Start();

        GraphicsDevice device = Main.graphics.GraphicsDevice;

        device.RasterizerState = RasterizerState.CullNone;

        Matrix transform = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);

        effect.Parameters["transformMatrix"].SetValue(transform);
        effect.Parameters["positionVelocityMap"].SetValue(particlePositionVelocityMap);

        device.SetVertexBuffer(vertexBuffer);
        device.Indices = indexBuffer;

        RenderTargetBinding[] bindings = device.GetRenderTargets();

        device.SetRenderTarget(particlePositionVelocityMapCopy);

        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexBuffer.VertexCount, 0, indexBuffer.IndexCount / 3);
        }

        device.SetRenderTargets(bindings);

        (particlePositionVelocityMap, particlePositionVelocityMapCopy) = (particlePositionVelocityMapCopy, particlePositionVelocityMap);
    }
}