using Terraria.ModLoader;

namespace Zenith;

public class ZenithMod : Mod
{
    public override void PostSetupContent()
    {
        ModContent.GetInstance<PrimitiveSystem>().RegisterRenderTarget("ScreenTarget");
    }
}