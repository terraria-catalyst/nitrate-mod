using System;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace TeamCatalyst.Nitrate.Optimizations.GameLogic;

/// <summary>
///     Optimizes internal code for pylons.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class FasterPylonSystem : ModSystem {
    public override void OnModLoad() {
        base.OnModLoad();

        On_TeleportPylonsSystem.IsPlayerNearAPylon += On_IsPlayerNearAPylon;
    }

    private static bool On_IsPlayerNearAPylon(On_TeleportPylonsSystem.orig_IsPlayerNearAPylon original, Player player) {
        if (!Configuration.UsesFasterPylonSystem) {
            return original(player);
        }
        
        foreach (var info in Main.PylonSystem.Pylons) {
            var pos = info.PositionInTiles;
            Point16 lowerRightPylonPoint = new(pos.X + 2, pos.Y + 3);

            var playerPos = player.position.ToTileCoordinates();

            TileReachCheckSettings.Pylons.GetRanges(player, out var x, out var y);
            var minRangeX = Utils.Clamp(pos.X - x, 0, Main.maxTilesX - 1);
            var maxRangeX = Utils.Clamp(lowerRightPylonPoint.X + x - 1, 0, Main.maxTilesX - 1);
            var minRangeY = Utils.Clamp(pos.Y - y - 1, 0, Main.maxTilesY - 1);
            var maxRangeY = Utils.Clamp(lowerRightPylonPoint.Y + y - 1, 0, Main.maxTilesY - 1);

            if (playerPos.X >= minRangeX && playerPos.X <= maxRangeX && playerPos.Y >= minRangeY && playerPos.Y <= maxRangeY) {
                return true;
            }
        }

        return false;
    }
}
