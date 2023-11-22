using Terraria.ModLoader;

namespace Zenith
{
	public class Zenith : Mod
	{
        public override void PostSetupContent()
        {
            ModContent.GetInstance<PrimitiveSystem>().RegisterRenderTarget("ScreenTarget");
        }
    }
}