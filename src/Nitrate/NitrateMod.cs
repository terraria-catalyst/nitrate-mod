using JetBrains.Annotations;
using Terraria.ModLoader;

// Include a dummy type under the Nitrate namespace to trick tModLoader. If
// there isn't at least one type under a namespace matching the mod's internal
// name, it fails a validation check by tModLoader...
namespace Nitrate {
    [UsedImplicitly]
    internal static class Unused { }
}

namespace TeamCatalyst.Nitrate {
    public sealed class NitrateMod : Mod {
        public override void Load() {
            base.Load();
            // TODO: Initiate restart sequence.
        }
    }
}
