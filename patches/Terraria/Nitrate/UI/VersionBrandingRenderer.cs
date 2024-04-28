#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;

namespace Terraria.Nitrate.UI;

/// <summary>
///		Handles Nitrate's reworked rendering of social links and version texts.
///		<br />
///		Supports selecting different mods (vanilla, tML, Nitrate).
/// </summary>
internal sealed class VersionBrandingRenderer(params VersionBrandingRenderer.VersionBrandingRecord[] records)
{
	public class VersionBrandingRecord(Func<string> getText)
	{
		public string Text => getText();

		public List<TitleLinkButton> Buttons { get; } = [];

		public Asset<Texture2D>? Icon { get; set; }

		private readonly Func<string> getText = getText;
	}

	/// <summary>
	///		Vanilla branding and text.
	/// </summary>
	public static VersionBrandingRecord Vanilla = new(() => "Terraria " + Main.versionNumber);

	/// <summary>
	///		tModLoader branding and text.
	/// </summary>
	public static VersionBrandingRecord Tml = new(() => ModLoader.ModLoader.versionedName);

	/// <summary>
	///		Nitrate branding and text.
	/// </summary>
	public static VersionBrandingRecord Nitrate = new(() => "Nitrate vTODO");

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
		static void drawIcon(SpriteBatch sb, Texture2D texture, Vector2 location, int index, ref int selected, int buttonSize, ref bool anyHovered)
		{
			if (panelGrayscale is null || categoryPanelBorder is null)
				return;

			var hitbox = new Rectangle((int)location.X, (int)location.Y, buttonSize, buttonSize);
			bool hovered = hitbox.Intersects(new Rectangle((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 1, 1));
			anyHovered |= hovered;

			Utils.DrawSplicedPanel(sb, panelGrayscale.Value, hitbox.X, hitbox.Y, hitbox.Width, hitbox.Height, 10, 10, 10, 10, selected == index ? new Color(152, 175, 235) : Colors.InventoryDefaultColor);
			
			if (hovered) {
				// TODO: Update logic in draw loop.
				if (Main.mouseLeft)
					selected = index;
			
				Utils.DrawSplicedPanel(sb, categoryPanelBorder.Value, hitbox.X, hitbox.Y, hitbox.Width, hitbox.Height, 10, 10, 10, 10, Color.White);
			}

			sb.Draw(texture, new Vector2(location.X + link_icon_offset, location.Y + link_icon_offset), Color.White);
		}

		if (!EnsureAssetsAreInitialized())
			return;

		// Draw record list.
		{

			int offY = Main.screenHeight - (brand_button_size_with_padding * records.Length);

			bool anyHovered = false;
			for (int i = records.Length - 1; i >= 0; i--) {
				VersionBrandingRecord record = records[i];

				if (record.Icon is not { } icon || !icon.IsLoaded)
					continue;

				drawIcon(sb, icon.Value, new Vector2(brand_button_padding, offY), i, ref selected, brand_button_size, ref anyHovered);

				offY += brand_button_size_with_padding;
			}
		}

		// Draw current record.
		{
			if (!TryGetCurrentRecord(out var record))
				return;

			string text = record.Text;
			Vector2 origin = FontAssets.MouseText.Value.MeasureString(text) * 0.5f;

			// 4 offsets for immitating stroke, 1 for drawing the text in a readable color.
			for (int i = 0; i < 5; i++) {
				Color textColor = Color.Black;

				// Draw with the actual color for the last iteration.
				if (i == 4) {
					textColor = menuColor;
					textColor.R = (byte)((255 + textColor.R) / 2);
					textColor.G = (byte)((255 + textColor.R) / 2);
					textColor.B = (byte)((255 + textColor.R) / 2);
				}

				textColor.A = (byte)(textColor.A * 0.3f);

				int offX = 0;
				int offY = 0;
				{
					if (i == 0)
						offX = -2;

					if (i == 1)
						offX = 2;

					if (i == 2)
						offY = -2;

					if (i == 3)
						offY = 2;

					offX += brand_button_size + content_padding;
				}

				// Draw the text.
				sb.DrawString(FontAssets.MouseText.Value, text, new Vector2(origin.X + offX, Main.screenHeight - origin.Y + offY - 2f - upBump), textColor, 0f, origin, 1f, SpriteEffects.None, 0f);
			}

			// Draw link buttons.
			var anchor = new Vector2(brand_button_size + content_padding + (link_icon_size / 2), Main.screenHeight - origin.Y - 2f - upBump - link_icon_size);
			foreach (var link in record.Buttons) {
				link.Draw(sb, anchor);
				anchor.X += link_icon_size;
			}
		}
	}

	private bool TryGetCurrentRecord([NotNullWhen(returnValue: true)] out VersionBrandingRecord? record)
	{
		selected = Math.Clamp(selected, 0, records.Length);
		if (records.Length <= 0) {
			record = null;
			return false;
		}

		record = records[selected];
		return true;
	}

	private static bool EnsureAssetsAreInitialized()
	{
		if (panelGrayscale is not null && categoryPanelBorder is not null)
			return true;

		if (Main.Assets is null)
			return false;

		panelGrayscale = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/PanelGrayscale");
		categoryPanelBorder = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanelBorder");
		return true;
	}
}
