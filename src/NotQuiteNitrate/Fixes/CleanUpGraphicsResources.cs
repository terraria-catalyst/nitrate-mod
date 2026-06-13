using System;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace NotQuiteNitrate.Fixes;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class CleanUpGraphicsResources : ModSystem
{
    public override void Load()
    {
        base.Load();

        MonoModHooks.Add(
            typeof(GraphicsResource).GetMethod("Finalize", BindingFlags.NonPublic | BindingFlags.Instance)!,
            DisposeOnFinalize
        );
    }

    private static void DisposeOnFinalize(Action<GraphicsResource> orig, GraphicsResource self)
    {
        try
        {
            if (self is { IsDisposed: false, GraphicsDevice.IsDisposed: false })
            {
                //ModContent.GetInstance<ModImpl>().Logger.Info("HANDLED DISPOSAL FOR: " + self.GetType().Name);

                self.Dispose(false);
            }
        }
        finally
        {
            // Necessary because it invokes object::Finalize()!
            orig(self);
        }
    }

    // Good for sanity checking to make sure our detour works.  Also enable the
    // logging in the detour.
    /*public override void PostDrawTiles()
    {
        base.PostDrawTiles();

        new RenderTarget2D(Main.instance.GraphicsDevice, 10, 10);
    }*/
}
