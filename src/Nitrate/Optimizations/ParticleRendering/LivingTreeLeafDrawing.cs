using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Utilities;

namespace Nitrate.Optimizations.ParticleRendering;

internal static class LivingTreeLeafDrawing {
    
    public static readonly UnifiedRandom random = new();
    
    public static void EmitLivingTreeLeaf(int x, int y, int leafGoreType)  {
        EmitLivingTreeLeaf_Below(x, y, leafGoreType);
        if (random.NextBool(2)) {
            EmitLivingTreeLeaf_Sideways(x, y, leafGoreType);
        }
    }
    
    private static void EmitLivingTreeLeaf_Below(int x, int y, int leafGoreType) {
        Tile testTile = Main.tile[x, y + 1];
        if (WorldGen.SolidTile(testTile) || testTile.liquid > 0) {
            return;
        }
        
        float windForVisuals = Main.WindForVisuals;
        if (windForVisuals < -0.20000000298023224 && (WorldGen.SolidTile(Main.tile[x - 1, y + 1]) || WorldGen.SolidTile(Main.tile[x - 2, y + 1])) || windForVisuals > 0.20000000298023224 && (WorldGen.SolidTile(Main.tile[x + 1, y + 1]) || WorldGen.SolidTile(Main.tile[x + 2, y + 1]))) {
            return;
        }
        
        Gore.NewGorePerfect(new FnaVector2(x * 16, y * 16 + 16), Vector2.Zero, leafGoreType).Frame.CurrentColumn = Main.tile[x, y].color();
    }

    private static void EmitLivingTreeLeaf_Sideways(int x, int y, int leafGoreType) {
        int num1 = 0;
        if (Main.WindForVisuals > 0.20000000298023224) {
            num1 = 1;
        } else if (Main.WindForVisuals < -0.20000000298023224) {
            num1 = -1;
        }
        
        Tile testTile = Main.tile[x + num1, y];
        if (WorldGen.SolidTile(testTile) || testTile.liquid > 0) {
            return;
        }
        
        int num2 = 0;
        if (num1 == -1) {
            num2 = -10;
        }
        
        Gore.NewGorePerfect(new Vector2(x * 16 + 8 + 4 * num1 + num2, y * 16 + 8), Vector2.Zero, leafGoreType).Frame.CurrentColumn = Main.tile[x, y].color();
    }
}