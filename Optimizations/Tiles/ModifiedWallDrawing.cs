using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.Graphics;
using Terraria.ModLoader;

namespace Nitrate.Optimizations.Tiles;

internal class ModifiedWallDrawing
{
    public static void DrawSingleWall(int i, int j, FnaVector2 screenPosition)
    {
        WallDrawing wd = Main.instance.WallsRenderer;
        wd._tileArray = Main.tile;
        int[] wallBlend = Main.wallBlend;
        Rectangle value = new(0, 0, 32, 32);
        Tile tile = wd._tileArray[i, j];
        ushort wall = tile.wall;

        // WallDrawing.FullTile accesses the tiles at (i - 1) and (i + 1)
        if ((i - 1) < 0 || (i + 1) >= wd._tileArray.Width)
        {
            return;
        }

        if (wall <= 0 || wd.FullTile(i, j) || (wall == 318 && !wd._shouldShowInvisibleWalls) || (tile.invisibleWall() && !wd._shouldShowInvisibleWalls))
        {
            return;
        }

        Color color = Color.White;

        Main.instance.LoadWall(wall);

        value.X = tile.wallFrameX();
        value.Y = tile.wallFrameY() + Main.wallFrame[wall] * 180;

        ushort wall2 = tile.wall;

        if ((uint)(wall2 - 242) <= 1u)
        {
            int num11 = 20;
            int num12 = (Main.wallFrameCounter[wall] + i * 11 + j * 27) % (num11 * 8);
            value.Y = tile.wallFrameY() + 180 * (num12 / num11);
        }

        Color color2 = color;

        if (wall == 44 || wall == 346)
            color2 = new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);

        if ((uint)(wall - 341) <= 4u)
            color2 = Color.Lerp(color2, Color.White, 0.5f);

        Texture2D tileDrawTexture2 = wd.GetTileDrawTexture(tile, i, j);
        Main.spriteBatch.Draw(tileDrawTexture2, new Vector2(i * 16 - (int)screenPosition.X - 8, j * 16 - (int)screenPosition.Y - 8), value, color2, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        float gfxQuality = Main.gfxQuality;
        int num = (int)(120f * (1f - gfxQuality) + 40f * gfxQuality);
        int num2 = (int)((float)num * 0.4f);
        int num3 = (int)((float)num * 0.35f);
        int num4 = (int)((float)num * 0.3f);

        if (color.R > num2 || color.G > num3 || color.B > num4)
        {
            bool num13 = wd._tileArray[i - 1, j].wall > 0 && wallBlend[wd._tileArray[i - 1, j].wall] != wallBlend[tile.wall];
            bool flag = wd._tileArray[i + 1, j].wall > 0 && wallBlend[wd._tileArray[i + 1, j].wall] != wallBlend[tile.wall];
            bool flag2 = wd._tileArray[i, j - 1].wall > 0 && wallBlend[wd._tileArray[i, j - 1].wall] != wallBlend[tile.wall];
            bool flag3 = wd._tileArray[i, j + 1].wall > 0 && wallBlend[wd._tileArray[i, j + 1].wall] != wallBlend[tile.wall];

            if (num13)
                Main.spriteBatch.Draw(TextureAssets.WallOutline.Value, new Vector2(i * 16 - (int)screenPosition.X, j * 16 - (int)screenPosition.Y), new Rectangle(0, 0, 2, 16), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            if (flag)
                Main.spriteBatch.Draw(TextureAssets.WallOutline.Value, new Vector2(i * 16 - (int)screenPosition.X + 14, j * 16 - (int)screenPosition.Y), new Rectangle(14, 0, 2, 16), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            if (flag2)
                Main.spriteBatch.Draw(TextureAssets.WallOutline.Value, new Vector2(i * 16 - (int)screenPosition.X, j * 16 - (int)screenPosition.Y), new Rectangle(0, 0, 16, 2), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            if (flag3)
                Main.spriteBatch.Draw(TextureAssets.WallOutline.Value, new Vector2(i * 16 - (int)screenPosition.X, j * 16 - (int)screenPosition.Y + 14), new Rectangle(0, 14, 16, 2), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }
    }

    public static void DrawSingleWallMostlyUnmodified(int i, int j, FnaVector2 screenPosition)
    {
        WallDrawing wd = Main.instance.WallsRenderer;
        wd._tileArray = Main.tile;
        int[] wallBlend = Main.wallBlend;
        Rectangle value = new(0, 0, 32, 32);
        Tile tile = wd._tileArray[i, j];
        ushort wall = tile.wall;
        VertexColors vertices = default;

        // WallDrawing.FullTile accesses the tiles at (i - 1) and (i + 1)
        if ((i - 1) < 0 || (i + 1) >= wd._tileArray.Width)
        {
            return;
        }

        if (WallLoader.PreDraw(i, j, wall, Main.spriteBatch))
        {
            if (wall <= 0 || wd.FullTile(i, j) || (wall == 318 && !wd._shouldShowInvisibleWalls) || (tile.invisibleWall() && !wd._shouldShowInvisibleWalls))
            {
                return;
            }

            Color color = Lighting.GetColor(i, j);

            if (tile.fullbrightWall())
            {
                color = Color.White;
            }

            if (wall == 318)
            {
                color = Color.White;
            }

            if (color is { R: 0, G: 0, B: 0 } && i < Main.UnderworldLayer)
            {
                return;
            }

            Main.instance.LoadWall(wall);

            value.X = tile.wallFrameX();
            value.Y = tile.wallFrameY() + Main.wallFrame[wall] * 180;

            ushort wall2 = tile.wall;

            if ((uint)(wall2 - 242) <= 1u)
            {
                int num11 = 20;
                int num12 = (Main.wallFrameCounter[wall] + i * 11 + j * 27) % (num11 * 8);
                value.Y = tile.wallFrameY() + 180 * (num12 / num11);
            }

            if (Lighting.NotRetro && !Main.wallLight[wall] && tile.wall != 241 && (tile.wall < 88 || tile.wall > 93) && !WorldGen.SolidTile(tile))
            {
                Texture2D tex = wd.GetTileDrawTexture(tile, i, j);

                if (tile.wall == 346 || tile.wall == 44)
                {
                    vertices.TopRightColor = vertices.TopLeftColor = vertices.BottomRightColor = vertices.BottomLeftColor = Main.DiscoColor;
                }
                else
                {
                    Lighting.GetCornerColors(i, j, out vertices);

                    if ((tile.wall - 341) <= 4)
                    {
                        wd.LerpVertexColorsWithColor(ref vertices, Color.White, 0.5f);
                    }

                    if (tile.fullbrightWall())
                    {
                        vertices = WallDrawing._glowPaintColors;
                    }
                }
                
                Main.tileBatch.Draw(tex, new Vector2(i * 16 - screenPosition.X - 8, i * 16 - screenPosition.Y - 8), value, vertices, Vector2.Zero, 1f, SpriteEffects.None);
            }
            else
            {
                Color color2 = color;

                if (wall == 44 || wall == 346)
                    color2 = new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);

                if ((uint)(wall - 341) <= 4u)
                    color2 = Color.Lerp(color2, Color.White, 0.5f);

                Texture2D tileDrawTexture2 = wd.GetTileDrawTexture(tile, i, j);
                Main.spriteBatch.Draw(tileDrawTexture2, new Vector2(i * 16 - (int)screenPosition.X - 8, j * 16 - (int)screenPosition.Y - 8), value, color2, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }

            float gfxQuality = Main.gfxQuality;
            int num = (int)(120f * (1f - gfxQuality) + 40f * gfxQuality);
            int num2 = (int)((float)num * 0.4f);
            int num3 = (int)((float)num * 0.35f);
            int num4 = (int)((float)num * 0.3f);

            if (color.R > num2 || color.G > num3 || color.B > num4)
            {
                bool num13 = wd._tileArray[i - 1, j].wall > 0 && wallBlend[wd._tileArray[i - 1, j].wall] != wallBlend[tile.wall];
                bool flag = wd._tileArray[i + 1, j].wall > 0 && wallBlend[wd._tileArray[i + 1, j].wall] != wallBlend[tile.wall];
                bool flag2 = wd._tileArray[i, j - 1].wall > 0 && wallBlend[wd._tileArray[i, j - 1].wall] != wallBlend[tile.wall];
                bool flag3 = wd._tileArray[i, j + 1].wall > 0 && wallBlend[wd._tileArray[i, j + 1].wall] != wallBlend[tile.wall];

                if (num13)
                    Main.spriteBatch.Draw(TextureAssets.WallOutline.Value, new Vector2(i * 16 - (int)screenPosition.X, j * 16 - (int)screenPosition.Y), new Rectangle(0, 0, 2, 16), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

                if (flag)
                    Main.spriteBatch.Draw(TextureAssets.WallOutline.Value, new Vector2(i * 16 - (int)screenPosition.X + 14, j * 16 - (int)screenPosition.Y), new Rectangle(14, 0, 2, 16), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

                if (flag2)
                    Main.spriteBatch.Draw(TextureAssets.WallOutline.Value, new Vector2(i * 16 - (int)screenPosition.X, j * 16 - (int)screenPosition.Y), new Rectangle(0, 0, 16, 2), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

                if (flag3)
                    Main.spriteBatch.Draw(TextureAssets.WallOutline.Value, new Vector2(i * 16 - (int)screenPosition.X, j * 16 - (int)screenPosition.Y + 14), new Rectangle(0, 14, 16, 2), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }
        }

        WallLoader.PostDraw(i, j, wall, Main.spriteBatch);
    }
}