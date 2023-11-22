using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Silk.NET.Direct3D11;
using Silk.NET.Vulkan;
using System;
using System.Diagnostics;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace Zenith
{
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

        private static readonly VertexPositionTexture[] Particle = new[]
        {
            new VertexPositionTexture(new Vector3(0, 0, 0), Vector2.Zero),
            new VertexPositionTexture(new Vector3(1920, 0, 0), Vector2.UnitX),
            new VertexPositionTexture(new Vector3(1920, 1080, 0), Vector2.One),
            new VertexPositionTexture(new Vector3(0, 1080, 0), Vector2.UnitY),
        };

        private static readonly short[] ParticleIndices = new short[]
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
                vertexBuffer = new(device, typeof(VertexPositionTexture), Particle.Length, BufferUsage.None);
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

                vector4Buffer = new(device, Vector4Buffer, 2048 * 2048, BufferUsage.None);

                deviceHandle = (nint)typeof(GraphicsDevice).GetField("GLDevice", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Main.instance.GraphicsDevice);

                textureHandle = (nint)typeof(RenderTarget2D).GetField("texture", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(particlePositionVelocityMap);
                bufferHandle = (nint)typeof(DynamicVertexBuffer).GetField("buffer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(vector4Buffer);
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

            Terraria.Utils.Swap(ref particlePositionVelocityMap, ref particlePositionVelocityMapCopy);

            unsafe
            {
                ID3D11DeviceContext* context = GetD3D11DeviceContext();

                //context->CopyResource(GetD3D11ResourceFromBuffer(), GetD3D11ResourceFromRenderTarget());
            }


            // Every second, print the values of the first element in the buffer. The values should be increasing over time if it's working.
            // The rendertarget component is confirmed to work so if it's not working the issue is definitely with this copy step.
            /*if (sw.ElapsedMilliseconds > 1000)
            {
                sw.Restart();

                Vector4[] data = new Vector4[2048 * 2048];

                vector4Buffer.GetData(data, 0, data.Length);

                Main.NewText(data[0]);
            }*/
        }

        private static unsafe ID3D11Resource* GetD3D11ResourceFromRenderTarget() => (ID3D11Resource*)(void*)textureHandle;

        private static unsafe ID3D11Resource* GetD3D11ResourceFromBuffer() => (ID3D11Resource*)(void*)bufferHandle;

        private static unsafe ID3D11Device* GetD3D11Device()
        {
            FNA3DDevice* fna3ddevice = (FNA3DDevice*)(void*)deviceHandle;

            FNAD3D11Device* FNAD3D11device = (FNAD3D11Device*)fna3ddevice->driverData;

            ID3D11Device* device = FNAD3D11device->device;

            return device;
        }

        private static unsafe ID3D11DeviceContext* GetD3D11DeviceContext()
        {
            FNA3DDevice* fna3ddevice = (FNA3DDevice*)(void*)deviceHandle;

            FNAD3D11Device* FNAD3D11device = (FNAD3D11Device*)fna3ddevice->driverData;

            ID3D11DeviceContext* context = FNAD3D11device->context;

            return context;
        }
    }

    public unsafe struct FNAD3D11Device
    {
        public ID3D11Device* device;
        public ID3D11DeviceContext* context;
    }

    public struct FNA3DDevice
    {
        public nint DestroyDevice;
        public nint SwapBuffers;
        public nint Clear;
        public nint DrawIndexedPrimitives;
        public nint DrawInstancedPrimitives;
        public nint Drawprimitives;
        public nint SetViewport;
        public nint SetScissorRect;
        public nint GetBlendFactor;
        public nint SetBlendFactor;
        public nint GetMultiSampleMask;
        public nint SetMultiSampleMask;
        public nint GetReferenceStencil;
        public nint SetReferenceStencil;
        public nint SetBlendState;
        public nint SetDepthStencilState;
        public nint ApplyRasterizerState;
        public nint VerifySampler;
        public nint VerifyVertexSampler;
        public nint ApplyVertexBufferBindings;
        public nint SetRenderTargets;
        public nint ResolveTarget;
        public nint ResetBackbuffer;
        public nint ReadBackbuffer;
        public nint GetBackbufferSize;
        public nint GetBackbufferSurfaceFormat;
        public nint GetBackbufferDepthFormat;
        public nint GetBackbufferMultiSampleCount;
        public nint CreateTexture2D;
        public nint CreateTexture3D;
        public nint CreateTextureCube;
        public nint AddDisposeTexture;
        public nint SetTextureData2D;
        public nint SetTextureData3D;
        public nint SetTextureDataCube;
        public nint SetTextureDataYUV;
        public nint GetTextureData2D;
        public nint GetTextureData3D;
        public nint GetTextureDataCube;
        public nint GenColorRenderBuffer;
        public nint GenDepthStencilRenderbuffer;
        public nint AddDisposeRenderbuffer;
        public nint GenVertexBuffer;
        public nint AddDisposeVertexBuffer;
        public nint SetVertexBufferData;
        public nint GetVertexBufferData;
        public nint GenIndexBuffer;
        public nint AddDisposeIndexBuffer;
        public nint SetIndexBufferData;
        public nint GetIndexBufferData;
        public nint CreateEffect;
        public nint CloneEffect;
        public nint AddDisposeEffect;
        public nint SetEffectTechnique;
        public nint ApplyEffect;
        public nint BeginPassRestore;
        public nint EndPassRestore;
        public nint CreateQuery;
        public nint AddDisposeQuery;
        public nint QueryBegin;
        public nint QueryEnd;
        public nint QueryComplete;
        public nint QueryPixelCount;
        public nint SupportsDXT1;
        public nint SupportsS3TC;
        public nint SupportsBC7;
        public nint SupportsHardwareInstancing;
        public nint SupportsNoOverwrite;
        public nint SupportsSRGBRenderTargets;
        public nint GetMaxTextureSlots;
        public nint GetMaxMultiSampleCount;
        public nint SetStringMarker;
        public nint GetSysRenderer;
        public nint CreateSysTexture;
        public nint driverData;
    }
}
