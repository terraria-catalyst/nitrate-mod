using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Content.Readers;
using ReLogic.Utilities;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Assets;

namespace NotQuiteNitrate.Patches;

/// <summary>
///     Takes preliminary action to disable thread checks in FNA3D methods.
///     <br />
///     Rewrites <c>.rawimg</c> asset loading to reduce generated garbage and
///     generally operate more efficiently.  Co-authored by LolXD87 who provided
///     the initial implementation and suggestion.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class FasterAssetLoading : ModSystem
{
    private sealed class FastRawimgReader(GraphicsDevice graphicsDevice) : IAssetReader
    {
        public async ValueTask<T> FromStream<T>(Stream stream, MainThreadCreationContext mainThreadCtx) where T : class
        {
            Debug.Assert(typeof(T) == typeof(Texture2D));

            var buf = (Span<byte>)stackalloc byte[12];
            {
                stream.ReadExactly(buf);
            }

            var width = BinaryPrimitives.ReadInt32LittleEndian(buf[4..]);
            var height = BinaryPrimitives.ReadInt32LittleEndian(buf[8..]);

            var byteCount = width * height * 4;

            // if (byteCount < /* 256 * */ 1024)
            // {
            //     var data = (Span<byte>)stackalloc byte[byteCount];
            //     {
            //         stream.ReadExactly(data);
            //     }
            //     
            //     await mainThreadCtx;
            //
            //     var tex = new Texture2D(graphicsDevice, width, height);
            //     {
            //         fixed (byte* pData = data)
            //         {
            //             tex.SetDataPointerEXT(0, null, (nint)pData, byteCount);
            //         }
            //     }
            //
            //     return tex;
            // }

            var data = ArrayPool<byte>.Shared.Rent(byteCount);
            {
                await stream.ReadExactlyAsync(data, 0, byteCount);
            }

            await mainThreadCtx;

            var tex = new Texture2D(graphicsDevice, width, height);
            {
                tex.SetData(0, null, data, 0, byteCount);
            }

            ArrayPool<byte>.Shared.Return(data);
            return (tex as T)!;
        }
    }

    private static IAssetReader? rawimgReader;

#pragma warning disable CA2255
    [ModuleInitializer]
    public static void ModuleInit()
    {
        if (Main.dedServ)
        {
            return;
        }

        // Disable thread checks.
        MonoModHooks.Add(
            typeof(ThreadCheck).GetMethod(nameof(ThreadCheck.CheckThread), BindingFlags.Public | BindingFlags.Static),
            () => { }
        );

        var readers = Main.instance.Services.Get<AssetReaderCollection>();
        {
            readers.TryGetReader(".rawimg", out rawimgReader);
            readers.RegisterReader(new FastRawimgReader(Main.instance.Services.Get<IGraphicsDeviceService>().GraphicsDevice), ".rawimg");
        }
    }
#pragma warning restore CA2255

    public override void Unload()
    {
        base.Unload();

        var readers = Main.instance.Services.Get<AssetReaderCollection>();
        {
            readers.RegisterReader(rawimgReader ?? new RawImgReader(Main.instance.Services.Get<IGraphicsDeviceService>().GraphicsDevice), ".rawimg");
        }
    }
}
