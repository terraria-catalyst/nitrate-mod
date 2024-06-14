#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using ReLogic.Content;
using ReLogic.Graphics;

using Terraria.GameContent;
using Terraria.ID;

namespace Terraria.Nitrate.VersionBranding.UI;

/// <summary>
///		Handles rendering social links and version text in the main menu.
///		<br />
///		Supports cycling between different providers (vanilla, tML, Nitrate).
/// </summary>
internal sealed class VersionBrandingRenderer(params VersionBrandingRecord[] records)
{
	private const int brand_button_size = 32;
	private const int brand_button_padding = 4;
	private const int brand_button_size_with_padding = brand_button_size + brand_button_padding;
	private const int content_padding = 10;
	private const int link_icon_size = 30;
	private const int link_icon_offset = 0;

	private static Asset<Texture2D>? panelGrayscale;
	private static Asset<Texture2D>? categoryPanelBorder;

	private int selected;

	public void Draw(SpriteBatch sb, Color menuColor, float upBump)
	{
		if (!EnsureAssetsAreInitialized())
		{
			return;
		}

		// Draw record list.
		{
			var offY = Main.screenHeight - brand_button_size_with_padding * records.Length;

			var anyHovered = false;
			for (var i = records.Length - 1; i >= 0; i--)
			{
				var record = records[i];
				if (record.Icon is not { IsLoaded: true, } icon)
				{
					continue;
				}

				drawIcon(sb, icon.Value, new Vector2(brand_button_padding, offY), i, ref selected, brand_button_size, ref anyHovered);
				offY += brand_button_size_with_padding;
			}
		}

		// Draw current record.
		{
			if (!TryGetCurrentRecord(out var record))
			{
				return;
			}

			var text = record.Text;
			var origin = FontAssets.MouseText.Value.MeasureString(text) * 0.5f;

			// 4 offsets for imitating stroke, 1 for drawing the text in a
			// readable color.
			for (var i = 0; i < 5; i++)
			{
				var textColor = Color.Black;

				// Draw the text with the actual color for the last iteration.
				if (i == 4)
				{
					textColor = menuColor;
					textColor.R = (byte)((255 + textColor.R) / 2);
					textColor.G = (byte)((255 + textColor.G) / 2);
					textColor.B = (byte)((255 + textColor.B) / 2);
				}

				textColor.A = (byte)((255 + textColor.A) / 2);

				var offX = 0;
				var offY = 0;
				{
					switch (i)
					{
					case 0:
						offX = -2;
						break;

					case 1:
						offX = 2;
						break;

					case 2:
						offY = -2;
						break;

					case 3:
						offY = 2;
						break;
					}

					offX += brand_button_size + content_padding;
				}

				// Draw the text.
				sb.DrawString(FontAssets.MouseText.Value, text, new Vector2(origin.X + offX, Main.screenHeight - origin.Y + offY - 2f - upBump), textColor, 0f, origin, 1f, SpriteEffects.None, 0f);
			}

			// Draw link buttons.
			// ReSharper disable once PossibleLossOfFraction
			var anchor = new Vector2(brand_button_size + content_padding + link_icon_size / 2, Main.screenHeight - origin.Y - 2f - upBump - link_icon_size);
			foreach (var link in record.Buttons)
			{
				link.Draw(sb, anchor);
				anchor.X += link_icon_size;
			}
		}

		return;

		static void drawIcon(SpriteBatch sb, Texture2D texture, Vector2 location, int index, ref int selected, int buttonSize, ref bool anyHovered)
		{
			if (panelGrayscale is null || categoryPanelBorder is null)
			{
				return;
			}

			var hitbox = new Rectangle((int)location.X, (int)location.Y, buttonSize, buttonSize);
			var hovered = hitbox.Intersects(new Rectangle((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 1, 1));
			anyHovered |= hovered;

			Utils.DrawSplicedPanel(sb, panelGrayscale.Value, hitbox.X, hitbox.Y, hitbox.Width, hitbox.Height, 10, 10, 10, 10, selected == index ? new Color(152, 175, 235) : Colors.InventoryDefaultColor);

			if (hovered)
			{
				// TODO: Move update logic out of the draw loop.
				if (Main.mouseLeft)
				{
					selected = index;
				}

				Utils.DrawSplicedPanel(sb, categoryPanelBorder.Value, hitbox.X, hitbox.Y, hitbox.Width, hitbox.Height, 10, 10, 10, 10, Color.White);
			}

			sb.Draw(texture, new Vector2(location.X + link_icon_offset, location.Y + link_icon_offset), Color.White);
		}
	}

	private bool TryGetCurrentRecord([NotNullWhen(returnValue: true)] out VersionBrandingRecord? record)
	{
		if (records.Length <= 0)
		{
			record = null;
			return false;
		}

		selected = Math.Clamp(selected, 0, records.Length);
		record = records[selected];
		return true;
	}

	private static bool EnsureAssetsAreInitialized()
	{
		if (panelGrayscale is not null && categoryPanelBorder is not null)
		{
			return true;
		}

		if (Main.Assets is null)
		{
			return false;
		}

		panelGrayscale = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/PanelGrayscale");
		categoryPanelBorder = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanelBorder");
		return true;
	}
}
