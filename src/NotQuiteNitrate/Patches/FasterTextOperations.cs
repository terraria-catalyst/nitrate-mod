/*
using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace NotQuiteNitrate.Patches;

internal sealed class FasterTextOperations : ModSystem
{
    private static readonly Dictionary<DynamicSpriteFont, Dictionary<string, Vector2>> dsf_measure_string_cache = [];
    private static readonly Dictionary<SpriteFont, Dictionary<string, Vector2>> sf_measure_string_cache = [];

    public override void Load()
    {
        base.Load();

        MonoModHooks.Add(
            typeof(DynamicSpriteFont).GetMethod(nameof(DynamicSpriteFont.MeasureString), BindingFlags.Public | BindingFlags.Instance),
            DynamicSpriteFontMeasureString
        );

        MonoModHooks.Add(
            typeof(SpriteFont).GetMethod(nameof(SpriteFont.MeasureString), BindingFlags.Public | BindingFlags.Instance, [typeof(string)]),
            SpriteFontMeasureString
        );

        On_ChatManager.DrawColorCodedString_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool += (
            orig,
            spriteBatch,
            font,
            snippets,
            position,
            baseColor,
            rotation,
            origin,
            baseScale,
            out hoveredSnippet,
            maxWidth,
            ignoreColors
        ) =>
        {
            var mousePos = new Vector2(Main.mouseX, Main.mouseY);

            var fontHeight = font.MeasureString(" ").X;

            var startPos = position;
            var resultPos = startPos;

            var drawColor = baseColor;

            var hoveredSnippetIndex = -1;

            var num3 = 0f;

            for (var i = 0; i < snippets.Length; i++)
            {
                var textSnippet = snippets[i];
                textSnippet.Update();
                if (!ignoreColors)
                {
                    drawColor = textSnippet.GetVisibleColor();
                }

                var snippetScale = textSnippet.Scale;
                if (textSnippet.UniqueDraw(justCheckingString: false, out var size, spriteBatch, startPos, drawColor, baseScale.X * snippetScale))
                {
                    if (mousePos.Between(startPos, startPos + size))
                    {
                        hoveredSnippetIndex = i;
                    }

                    startPos.X += size.X;
                    resultPos.X = Math.Max(resultPos.X, startPos.X);
                    continue;
                }

                // var array = Regex.Split(textSnippet.Text, "(\n)");
                var array = textSnippet.Text.Split('\n');
                var flag = true;
                foreach (var obj in array)
                {
                    var array3 = obj.Split(' ');
                    if (obj == "\n")
                    {
                        startPos.Y += font.LineSpacing * num3 * baseScale.Y;
                        startPos.X = position.X;
                        resultPos.Y = Math.Max(resultPos.Y, startPos.Y);
                        num3 = 0f;
                        flag = false;
                        continue;
                    }

                    for (var k = 0; k < array3.Length; k++)
                    {
                        if (k != 0)
                        {
                            startPos.X += fontHeight * baseScale.X * snippetScale;
                        }

                        if (maxWidth > 0f)
                        {
                            var num4 = font.MeasureString(array3[k]).X * baseScale.X * snippetScale;
                            if (startPos.X - position.X + num4 > maxWidth)
                            {
                                startPos.X = position.X;
                                startPos.Y += font.LineSpacing * num3 * baseScale.Y;
                                resultPos.Y = Math.Max(resultPos.Y, startPos.Y);
                                num3 = 0f;
                            }
                        }

                        if (num3 < snippetScale)
                        {
                            num3 = snippetScale;
                        }

                        spriteBatch.DrawString(font, array3[k], startPos, drawColor, rotation, origin, baseScale * textSnippet.Scale * snippetScale, SpriteEffects.None, 0f);
                        var vector2 = font.MeasureString(array3[k]);
                        if (mousePos.Between(startPos, startPos + vector2))
                        {
                            hoveredSnippetIndex = i;
                        }

                        startPos.X += vector2.X * baseScale.X * snippetScale;
                        resultPos.X = Math.Max(resultPos.X, startPos.X);
                    }

                    if (array.Length > 1 && flag)
                    {
                        startPos.Y += font.LineSpacing * num3 * baseScale.Y;
                        startPos.X = position.X;
                        resultPos.Y = Math.Max(resultPos.Y, startPos.Y);
                        num3 = 0f;
                    }

                    flag = true;
                }
            }

            hoveredSnippet = hoveredSnippetIndex;
            return resultPos;
        };
    }

    private static Vector2 DynamicSpriteFontMeasureString(
        Func<DynamicSpriteFont, string, Vector2> orig,
        DynamicSpriteFont self,
        string text
    )
    {
        if (text.Length == 0)
        {
            return Vector2.Zero;
        }

        if (dsf_measure_string_cache.TryGetValue(self, out var cache) && cache.TryGetValue(text, out var result))
        {
            return result;
        }

        if (!dsf_measure_string_cache.TryGetValue(self, out cache))
        {
            dsf_measure_string_cache[self] = cache = new Dictionary<string, Vector2>();
        }

        return cache[text] = orig(self, text);
    }

    private static Vector2 SpriteFontMeasureString(
        Func<SpriteFont, string, Vector2> orig,
        SpriteFont self,
        string text
    )
    {
        if (text.Length == 0)
        {
            return Vector2.Zero;
        }

        if (sf_measure_string_cache.TryGetValue(self, out var cache) && cache.TryGetValue(text, out var result))
        {
            return result;
        }

        if (!sf_measure_string_cache.TryGetValue(self, out cache))
        {
            sf_measure_string_cache[self] = cache = new Dictionary<string, Vector2>();
        }

        return cache[text] = orig(self, text);
    }
}
*/


