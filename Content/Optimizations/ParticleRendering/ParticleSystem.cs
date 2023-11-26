using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using System.Threading.Tasks;
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

        // Prevent the original DrawDust method from running; we use an IL edit
        // instead of a detour to allow mods' detours to still run while
        // cancelling vanilla behavior.
        IL_Main.DrawDust += il =>
        {
            ILCursor c = new(il);
            c.Emit(OpCodes.Ret);
        };

        GraphicsDevice device = Main.graphics.GraphicsDevice;

        Main.RunOnMainThread(() =>
        {
            _vertexBuffer = new VertexBuffer(device, typeof(VertexPositionTexture), Particle.Length, BufferUsage.None);
            _indexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, ParticleIndices.Length, BufferUsage.None);

            _vertexBuffer.SetData(0, Particle, 0, Particle.Length, VertexPositionTexture.VertexDeclaration.VertexStride);
            _indexBuffer.SetData(0, ParticleIndices, 0, ParticleIndices.Length);

            _instanceBuffer = new DynamicVertexBuffer(device, InstanceData, _dusts.Length, BufferUsage.None);

            _instanceParticleRenderer = Mod.Assets.Request<Effect>("Assets/Effects/InstancedParticleRenderer", AssetRequestMode.ImmediateLoad).Value;

            _dustAtlas = TextureAssets.Dust.Value;
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

            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);

            _instanceParticleRenderer.Parameters["transformMatrix"].SetValue(projection);
            _instanceParticleRenderer.Parameters["dustTexture"].SetValue(_dustAtlas);

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
        Parallel.For(0, _dusts.Length, i =>
        {
            Dust dust = Main.dust[i];

            if (dust.active)
            {
                float halfWidth = dust.frame.Width / 2;
                float halfHeight = dust.frame.Height / 2;

                Vector2 initialOffset = new(-halfWidth, -halfHeight);

                Matrix rotation = Matrix.CreateRotationZ(dust.rotation);
                Matrix offset = Matrix.CreateTranslation(initialOffset.X / 2, initialOffset.Y / 2, 0);
                Matrix reset = Matrix.CreateTranslation(-initialOffset.X / 2, -initialOffset.Y / 2, 0);

                Matrix rotationMatrix = offset * rotation * reset;

                Matrix world =
                    Matrix.CreateScale(dust.scale * dust.frame.Width, dust.scale * dust.frame.Height, 1) *
                    rotationMatrix *
                    Matrix.CreateTranslation(
                        (int)(dust.position.X - Main.screenPosition.X + initialOffset.X),
                        (int)(dust.position.Y - Main.screenPosition.Y + initialOffset.Y),
                        0
                    );

                float uvX = (float)dust.frame.X / _dustAtlas.Width;
                float uvY = (float)dust.frame.Y / _dustAtlas.Height;
                float uvW = (float)(dust.frame.X + dust.frame.Width) / _dustAtlas.Width;
                float uvZ = (float)(dust.frame.Y + dust.frame.Height) / _dustAtlas.Height;

                Color color = Lighting.GetColor((int)(dust.position.X + 4.0) / 16, (int)(dust.position.Y + 4.0) / 16);

                Color dustColor = dust.GetAlpha(color);

                _dusts[i] = new DustInstance(world, new Vector4(uvX, uvY, uvW, uvZ), dustColor.ToVector4());
            }
            else
            {
                _dusts[i] = new DustInstance();
            }
        });

        _instanceBuffer?.SetData(_dusts, 0, _dusts.Length, SetDataOptions.None);
    }

    private void Benchmark()
    {
        // Dust benchmark (spawns all 6000 dusts and positions them in a spot next to the player).
        for (int i = 0; i < Main.maxDust; i++)
        {
            Dust dust = Main.dust[i];

            int type = Terraria.ID.DustID.FlameBurst;

            dust.fadeIn = 0f;
            dust.active = true;
            dust.type = type;
            dust.noGravity = true;
            dust.color = Color.White;
            dust.alpha = 0;
            dust.position = Main.LocalPlayer.position + new Vector2(100, 0);
            dust.velocity = Vector2.Zero;
            dust.frame.X = 10 * type;
            dust.frame.Y = 10;
            dust.shader = null;
            dust.customData = null;
            dust.noLightEmittence = false;
            int num4 = type;

            while (num4 >= 100)
            {
                num4 -= 100;
                dust.frame.X -= 1000;
                dust.frame.Y += 30;
            }

            dust.frame.Width = 8;
            dust.frame.Height = 8;
            dust.rotation = 0f;
            dust.scale = 1;
            dust.noLight = false;
            dust.firstFrame = true;
        }
    }
}