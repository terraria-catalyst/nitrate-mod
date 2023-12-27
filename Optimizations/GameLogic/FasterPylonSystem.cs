using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Nitrate.Optimizations.GameLogic;

/// <summary>
///     Optimizes internal code for pylons.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class FasterPylonSystem : ModSystem
{
    public override void OnModLoad()
    {
        base.OnModLoad();

        IL_TeleportPylonsSystem.IsPlayerNearAPylon += SpeedUpIsPlayerNearAPylon;
    }

    private static void SpeedUpIsPlayerNearAPylon(ILContext il)
    {
        ILCursor cursor = new(il);
        cursor.Emit(OpCodes.Ldarg_0);

        cursor.EmitDelegate<Func<Player, bool>>(player =>
        {
            foreach (TeleportPylonInfo info in Main.PylonSystem.Pylons)
            {
                Point16 pos = info.PositionInTiles;
                Point16 lowerRightPylonPoint = new(pos.X + 2, pos.Y + 3);

                Point playerPos = player.position.ToTileCoordinates();

                TileReachCheckSettings.Pylons.GetRanges(player, out int x, out int y);
                int minRangeX = Utils.Clamp(pos.X - x, 0, Main.maxTilesX - 1);
                int maxRangeX = Utils.Clamp(lowerRightPylonPoint.X + x - 1, 0, Main.maxTilesX - 1);
                int minRangeY = Utils.Clamp(pos.Y - y - 1, 0, Main.maxTilesY - 1);
                int maxRangeY = Utils.Clamp(lowerRightPylonPoint.Y + y - 1, 0, Main.maxTilesY - 1);

                if (playerPos.X >= minRangeX && playerPos.X <= maxRangeX && playerPos.Y >= minRangeY && playerPos.Y <= maxRangeY)
                {
                    return true;
                }
            }

            return false;
        });

        cursor.Emit(OpCodes.Ret);
    }
}