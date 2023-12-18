/*using Microsoft.Win32.SafeHandles;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using System;
using System.Runtime.InteropServices;

namespace Nitrate.Content.Optimizations.FasterCursor;

internal sealed class SdlCursorHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private SdlCursorHandle(nint handle, bool ownsHandle) : base(ownsHandle)
    {
        SetHandle(handle);
    }

    public void SetSdlCursor()
    {
        if (!IsInvalid)
        {
            SDL.SDL_SetCursor(handle);
        }
    }

    protected override bool ReleaseHandle()
    {
        SDL.SDL_FreeCursor(handle);

        return true;
    }

    // https://github.com/FNA-NET/FNA/blob/79084855a0a1bbaef50fba6764dae70fc6cfc726/src/Input/MouseCursor.cs#L80
    public static SdlCursorHandle FromTexture2D(Texture2D texture, FnaVector2 origin)
    {
        if (texture.Format != SurfaceFormat.Color)
        {
            throw new Exception();
        }

        nint surface = IntPtr.Zero;
        nint handle;

        try
        {
            byte[] bytes = new byte[texture.Width * texture.Height * 4];
            texture.GetData(bytes);

            GCHandle gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            surface = SDL.SDL_CreateRGBSurfaceFrom(gcHandle.AddrOfPinnedObject(), texture.Width, texture.Height, 32, texture.Width * 4, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);
            gcHandle.Free();

            if (surface == IntPtr.Zero)
            {
                throw new Exception();
            }

            handle = SDL.SDL_CreateColorCursor(surface, (int)origin.X, (int)origin.Y);

            if (handle == IntPtr.Zero)
            {
                throw new Exception();
            }
        }
        finally
        {
            if (surface != IntPtr.Zero)
            {
                SDL.SDL_FreeSurface(surface);
            }
        }

        return new SdlCursorHandle(handle, true);
    }

    public static SdlCursorHandle FromPixels(byte[] pixels, int width, int height, FnaVector2 origin)
    {
        nint surface = IntPtr.Zero;
        nint handle;

        try
        {
            GCHandle gcHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            surface = SDL.SDL_CreateRGBSurfaceFrom(gcHandle.AddrOfPinnedObject(), width, height, 32, width * 4, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);
            gcHandle.Free();

            if (surface == IntPtr.Zero)
            {
                throw new Exception();
            }

            handle = SDL.SDL_CreateColorCursor(surface, (int)origin.X, (int)origin.Y);

            if (handle == IntPtr.Zero)
            {
                throw new Exception();
            }
        }
        finally
        {
            if (surface != IntPtr.Zero)
            {
                SDL.SDL_FreeSurface(surface);
            }
        }

        return new SdlCursorHandle(handle, true);
    }
}*/

