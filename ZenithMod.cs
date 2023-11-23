using Terraria.ModLoader;
using Zenith.Core.Features.PrimitiveRendering;

namespace Zenith;

public class ZenithMod : Mod
{
    public override void PostSetupContent()
    {
        ModContent.GetInstance<PrimitiveRenderingSystem>().RegisterRenderTarget("ScreenTarget");
    }
}