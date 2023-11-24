using JetBrains.Annotations;
using Terraria.ModLoader;
using Zenith.Core.Features.PrimitiveRendering;

namespace Zenith;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public sealed class ZenithMod : Mod
{
    public override void PostSetupContent()
    {
        ModContent.GetInstance<PrimitiveRenderingSystem>().RegisterRenderTarget("DustTarget");
    }
}