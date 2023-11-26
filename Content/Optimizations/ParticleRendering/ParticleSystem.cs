using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Zenith.Core.Features.Rendering;

namespace Zenith.Content.Optimizations.ParticleRendering;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class ParticleSystem : ModSystem
{
    private const string DustTarget = "DustTarget";

    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private DynamicVertexBuffer _instanceBuffer;
    private Texture2D _dustAtlas;
    private Effect _instanceParticleRenderer;
    private readonly DustInstance[] _dusts = new DustInstance[Main.maxDust];

    private static readonly VertexPositionTexture[] Particle =
    {
        new(new Vector3(0, 0, 0), Vector2.Zero),
        new(new Vector3(1, 0, 0), Vector2.UnitX),
        new(new Vector3(1, 1, 0), Vector2.One),
        new(new Vector3(0, 1, 0), Vector2.UnitY),
    };

    private static readonly short[] ParticleIndices =
    {
        0, 1, 2, 2, 3, 0
    };

    private static readonly VertexDeclaration InstanceData = new(
        new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Normal, 0),
        new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.Normal, 1),
        new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.Normal, 2),
        new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.Normal, 3),
        new VertexElement(64, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
        new VertexElement(80, VertexElementFormat.Vector4, VertexElementUsage.Color, 1)
    );

    public override void Load()
    {
        base.Load();

        GraphicsDevice device = Main.graphics.GraphicsDevice;

        Main.RunOnMainThread(() =>
        {
            _vertexBuffer = new VertexBuffer(device, typeof(VertexPositionTexture), Particle.Length, BufferUsage.None);
            _indexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, ParticleIndices.Length, BufferUsage.None);

            _vertexBuffer.SetData(0, Particle, 0, Particle.Length, VertexPositionTexture.VertexDeclaration.VertexStride);
            _indexBuffer.SetData(0, ParticleIndices, 0, ParticleIndices.Length);

            _instanceBuffer = new DynamicVertexBuffer(device, InstanceData, _dusts.Length, BufferUsage.None);

            _instanceParticleRenderer = Mod.Assets.Request<Effect>("Assets/Effects/InstancedParticleRenderer", AssetRequestMode.ImmediateLoad).Value;

            _dustAtlas = TextureAssets.Heart.Value;
        });
    }

    public override void PostSetupContent()
    {
        base.PostSetupContent();

        ModContent.GetInstance<ActionableRenderTargetSystem>().RegisterRenderTarget(DustTarget);
    }

    public override void Unload()
    {
        base.Unload();

        Main.RunOnMainThread(() =>
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();

            _instanceBuffer?.Dispose();
        });
    }

    public override void PreUpdateDusts()
    {
        base.PreUpdateDusts();

        ModContent.GetInstance<ActionableRenderTargetSystem>().QueueRenderAction(DustTarget, () =>
        {
            GraphicsDevice device = Main.graphics.GraphicsDevice;

            device.RasterizerState = RasterizerState.CullNone;

            Matrix world = Matrix.Identity;
            Matrix view = Matrix.Identity;
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);

            _instanceParticleRenderer.Parameters["transformMatrix"].SetValue(world * view * projection);
            _instanceParticleRenderer.Parameters["dustTexture"].SetValue(_dustAtlas);
            _instanceParticleRenderer.Parameters["textureSize"].SetValue(new Vector2(_dustAtlas.Width, _dustAtlas.Height));

            SetInstanceData();

            // Instanced render all particles.
            device.SetVertexBuffers(_vertexBuffer, new VertexBufferBinding(_instanceBuffer, 0, 1));
            device.Indices = _indexBuffer;

            foreach (EffectPass pass in _instanceParticleRenderer.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, _vertexBuffer.VertexCount, 0, _indexBuffer.IndexCount / 3, _dusts.Length);
            }
        });
    }

    private void SetInstanceData()
    {
        for (int i = 0; i < _dusts.Length; i++)
        {
            // TODO: Set dust data here from Main.dust
        }

        _instanceBuffer?.SetData(_dusts, 0, _dusts.Length, SetDataOptions.None);
    }
}