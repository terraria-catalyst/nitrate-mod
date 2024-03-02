using System;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using TeamCatalyst.Nitrate.API.Rendering;
using TeamCatalyst.Nitrate.API.Threading;
using TeamCatalyst.Nitrate.Utilities;
using Terraria;
using Terraria.GameContent;

namespace TeamCatalyst.Nitrate.Optimizations.ParticleRendering.Rain;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal class InstancedRainRenderer : AbstractInstancedParticleRenderer<ParticleInstance> {
    private const string dust_target = "DustTarget";

    protected override Lazy<Effect> InstanceParticleRenderer { get; }

    public InstancedRainRenderer() : base(Main.maxRain, dust_target) {
        InstanceParticleRenderer = new Lazy<Effect>(() => Mod.Assets.Request<Effect>("Assets/Effects/InstancedParticleRenderer", AssetRequestMode.ImmediateLoad).Value);
    }

    protected override Texture2D MakeAtlas() {
        return TextureAssets.Rain.Value;
    }

    public override void PreUpdateDusts() {
        base.PreUpdateDusts();

        if (!Main.raining) {
            return;
        }

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
                    var rain = Main.rain[i];

                    // Something has gone seriously wrong if the atlas is null.
                    if (rain.active && ParticleAtlas is not null) {
                        Rectangle frame = new(rain.type * 4, 0, 2, 40);

                        float halfWidth = (int)(frame.Width / 2f);
                        float halfHeight = (int)(frame.Height / 2f);

                        FnaVector2 initialOffset = new(-halfWidth, -halfHeight);

                        var rotation = SimdMatrix.CreateRotationZ(rain.rotation);
                        var offset = SimdMatrix.CreateTranslation(initialOffset.X / 2, initialOffset.Y / 2, 0);
                        var reset = SimdMatrix.CreateTranslation(-initialOffset.X / 2, -initialOffset.Y / 2, 0);

                        var rotationMatrix = offset * rotation * reset;

                        var world =
                            SimdMatrix.CreateScale(rain.scale * frame.Width, rain.scale * frame.Height, 1)
                            * rotationMatrix
                            * SimdMatrix.CreateTranslation(
                                (int)(rain.position.X - Main.screenPosition.X + initialOffset.X),
                                (int)(rain.position.Y - Main.screenPosition.Y + initialOffset.Y),
                                0
                            );

                        var uvX = (float)frame.X / ParticleAtlas.Width;
                        var uvY = (float)frame.Y / ParticleAtlas.Height;
                        var uvW = (float)(frame.X + frame.Width) / ParticleAtlas.Width;
                        var uvZ = (float)(frame.Y + frame.Height) / ParticleAtlas.Height;

                        var color = Lighting.GetColor((int)(rain.position.X + 4) / 16, (int)(rain.position.Y + 4) / 16) * 0.85f;

                        if (Main.shimmerAlpha > 0f) {
                            color *= 1f - Main.shimmerAlpha;
                        }

                        Particles[i] = new ParticleInstance(world, new Vector4(uvX, uvY, uvW, uvZ), color.ToVector4());
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
