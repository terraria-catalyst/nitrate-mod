using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader;

namespace Nitrate.Content.Optimizations.Tiles;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class SingleTileInstancingSystem : ModSystem
{
    public override void OnModLoad()
    {
        base.OnModLoad();

        IL_TileDrawing.DrawSingleTile += InstancedDrawSingleTile;
    }

    private void InstancedDrawSingleTile(ILContext il)
    {
    }
}
