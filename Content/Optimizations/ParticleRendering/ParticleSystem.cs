using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using System;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Zenith.Core.Features.Rendering;
using Zenith.Core.Utilities;

namespace Zenith.Content.Optimizations.ParticleRendering;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class ParticleSystem : AbstractParticleRenderer<DustInstance>
{
    private const string dust_target = "DustTarget";

    protected override Lazy<Effect> InstanceParticleRenderer { get; }

    public ParticleSystem() : base(Main.maxDust, dust_target)
    {
        InstanceParticleRenderer = new Lazy<Effect>(() => Mod.Assets.Request<Effect>("Assets/Effects/InstancedParticleRenderer", AssetRequestMode.ImmediateLoad).Value);
    }

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
    }

    protected override Texture2D MakeAtlas() => TextureAssets.Dust.Value;

    public override void PreUpdateDusts()
    {
        base.PreUpdateDusts();

        Benchmark();

        ModContent.GetInstance<ActionableRenderTargetSystem>().QueueRenderAction(dust_target, () =>
        {
            GraphicsDevice device = Main.graphics.GraphicsDevice;

            device.RasterizerState = RasterizerState.CullNone;

            FnaMatrix projection = FnaMatrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);

            InstanceParticleRenderer.Value.Parameters["transformMatrix"].SetValue(projection);
            InstanceParticleRenderer.Value.Parameters["dustTexture"].SetValue(ParticleAtlas);

            SetInstanceData();

            // Something has gone seriously wrong.
            if (VertexBuffer is null || IndexBuffer is null)
            {
                return;
            }

            // Instanced render all particles.
            device.SetVertexBuffers(VertexBuffer, new VertexBufferBinding(InstanceBuffer, 0, 1));
            device.Indices = IndexBuffer;

            foreach (EffectPass pass in InstanceParticleRenderer.Value.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, VertexBuffer.VertexCount, 0, IndexBuffer.IndexCount / 3, Particles.Length);
            }
        });
    }

    private void SetInstanceData()
    {
        Parallel.For(0, Particles.Length, i =>
        {
            Dust dust = Main.dust[i];

            // Something has gone seriously wrong if the atlas is null.
            if (dust.active && ParticleAtlas is not null)
            {
                float halfWidth = (int)(dust.frame.Width / 2f);
                float halfHeight = (int)(dust.frame.Height / 2f);

                Vector2 initialOffset = new(-halfWidth, -halfHeight);

                SimdMatrix rotation = SimdMatrix.CreateRotationZ(dust.rotation);
                SimdMatrix offset = SimdMatrix.CreateTranslation(initialOffset.X / 2, initialOffset.Y / 2, 0);
                SimdMatrix reset = SimdMatrix.CreateTranslation(-initialOffset.X / 2, -initialOffset.Y / 2, 0);

                SimdMatrix rotationMatrix = offset * rotation * reset;

                SimdMatrix world =
                    SimdMatrix.CreateScale(dust.scale * dust.frame.Width, dust.scale * dust.frame.Height, 1) *
                    rotationMatrix *
                    SimdMatrix.CreateTranslation(
                        (int)(dust.position.X - Main.screenPosition.X + initialOffset.X),
                        (int)(dust.position.Y - Main.screenPosition.Y + initialOffset.Y),
                        0
                    );

                float uvX = (float)dust.frame.X / ParticleAtlas.Width;
                float uvY = (float)dust.frame.Y / ParticleAtlas.Height;
                float uvW = (float)(dust.frame.X + dust.frame.Width) / ParticleAtlas.Width;
                float uvZ = (float)(dust.frame.Y + dust.frame.Height) / ParticleAtlas.Height;

                Color color = Lighting.GetColor((int)(dust.position.X + 4.0) / 16, (int)(dust.position.Y + 4.0) / 16);

                Color dustColor = dust.GetAlpha(color);

                Particles[i] = new DustInstance(world.ToFna(), new Vector4(uvX, uvY, uvW, uvZ), dustColor.ToVector4());
            }
            else
            {
                Particles[i] = new DustInstance();
            }
        });

        InstanceBuffer?.SetData(Particles, 0, Particles.Length, SetDataOptions.None);
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