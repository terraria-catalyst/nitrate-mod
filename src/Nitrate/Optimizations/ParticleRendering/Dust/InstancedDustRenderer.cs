using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Nitrate.API.Rendering;
using Nitrate.API.Threading;
using Nitrate.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;

namespace Nitrate.Optimizations.ParticleRendering.Dust;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class InstancedDustRenderer : AbstractInstancedParticleRenderer<ParticleInstance> {
    private const string dust_target = "DustTarget";

    protected override Lazy<Effect> InstanceParticleRenderer { get; }

    public InstancedDustRenderer() : base(Main.maxDust, dust_target) {
        InstanceParticleRenderer = new Lazy<Effect>(() => Mod.Assets.Request<Effect>("Assets/Effects/InstancedParticleRenderer", AssetRequestMode.ImmediateLoad).Value);
    }

    public override void Load() {
        base.Load();

        // Prevent the original DrawDust method from running; we use an IL edit
        // instead of a detour to allow mods' detours to still run while
        // cancelling vanilla behavior.
        IL_Main.DrawDust += il => {
            ILCursor c = new(il);
            c.Emit(OpCodes.Ret);
        };
    }

    protected override Texture2D MakeAtlas() {
        return TextureAssets.Dust.Value;
    }

    public override void PreUpdateDusts() {
        base.PreUpdateDusts();

        ActionableRenderTargetSystem.QueueRenderAction(
            dust_target,
            () => {
                var device = Main.graphics.GraphicsDevice;

                device.RasterizerState = RasterizerState.CullNone;

                var projection = SimdMatrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);

                InstanceParticleRenderer.Value.Parameters["transformMatrix"].SetValue(projection.ToFna());
                InstanceParticleRenderer.Value.Parameters["dustTexture"].SetValue(ParticleAtlas);

                SetInstanceData();

                // Something has gone seriously wrong.
                if (VertexBuffer is null || IndexBuffer is null) {
                    return;
                }

                // Instanced render all particles.
                device.SetVertexBuffers(VertexBuffer, new VertexBufferBinding(InstanceBuffer, 0, 1));
                device.Indices = IndexBuffer;

                foreach (var pass in InstanceParticleRenderer.Value.CurrentTechnique.Passes) {
                    pass.Apply();
                    device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, VertexBuffer.VertexCount, 0, IndexBuffer.IndexCount / 3, Particles.Length);
                }
            }
        );
    }

    private void SetInstanceData() {
        FasterParallel.For(
            0,
            Particles.Length,
            (inclusive, exclusive, _) => {
                for (var i = inclusive; i < exclusive; i++) {
                    var dust = Main.dust[i];

                    // Something has gone seriously wrong if the atlas is null.
                    if (dust.active && ParticleAtlas is not null) {
                        float halfWidth = (int)(dust.frame.Width / 2f);
                        float halfHeight = (int)(dust.frame.Height / 2f);

                        FnaVector2 initialOffset = new(-halfWidth, -halfHeight);

                        var rotation = SimdMatrix.CreateRotationZ(dust.rotation);
                        var offset = SimdMatrix.CreateTranslation(initialOffset.X, initialOffset.Y, 0);
                        var reset = SimdMatrix.CreateTranslation(-initialOffset.X, -initialOffset.Y, 0);

                        var rotationMatrix = offset * rotation * reset;

                        var world =
                            SimdMatrix.CreateScale(dust.scale * dust.frame.Width, dust.scale * dust.frame.Height, 1)
                            * rotationMatrix
                            * SimdMatrix.CreateTranslation(
                                (int)(dust.position.X - Main.screenPosition.X + initialOffset.X),
                                (int)(dust.position.Y - Main.screenPosition.Y + initialOffset.Y),
                                0
                            );

                        var uvX = (float)dust.frame.X / ParticleAtlas.Width;
                        var uvY = (float)dust.frame.Y / ParticleAtlas.Height;
                        var uvW = (float)(dust.frame.X + dust.frame.Width) / ParticleAtlas.Width;
                        var uvZ = (float)(dust.frame.Y + dust.frame.Height) / ParticleAtlas.Height;

                        var color = Lighting.GetColor((int)(dust.position.X + 4) / 16, (int)(dust.position.Y + 4) / 16);

                        var dustColor = dust.GetAlpha(color);

                        Particles[i] = new ParticleInstance(world, new Vector4(uvX, uvY, uvW, uvZ), dustColor.ToVector4());
                    }
                    else {
                        Particles[i] = new ParticleInstance();
                    }
                }
            }
        );

        InstanceBuffer?.SetData(Particles, 0, Particles.Length, SetDataOptions.None);
    }
}
