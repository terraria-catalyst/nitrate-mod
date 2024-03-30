using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.UI;

namespace Terraria.Nitrate.UI;

/// <summary>
///		Handles Nitrate's reworked rendering of social links and version texts.
///		<br />
///		Supports selecting different mods (vanilla, tML, Nitrate).
/// </summary>
internal sealed class VersionBrandingRenderer
{
	/// <summary>
	///		
	/// </summary>
	/// <param name="Text"></param>
	/// <param name="Buttons"></param>
	public readonly record struct VersionBrandingRecord(Func<string> Text, Func<List<TitleLinkButton>> Buttons, Func<string> Name, Func<Asset<Texture2D>?> Icon);

	/// <summary>
	///		Vanilla branding and text.
	/// </summary>
	public static VersionBrandingRecord Vanilla = new(
		GetVanillaVersionText,
		() => Main.TitleLinks,
		() => "Terraria",
		() => UICommon.IconVanilla
	);

	/// <summary>
	///		tModLoader branding and text.
	/// </summary>
	public static VersionBrandingRecord Tml = new(
		GetTmlVersionText,
		() => Main.tModLoaderTitleLinks,
		() => "tModLoader",
		() => UICommon.IconTml
	);

	/// <summary>
	///		Nitrate branding and text.
	/// </summary>
	public static VersionBrandingRecord Nitrate = new(
		GetNitrateVersionText,
		() => Main.nitrateLinks,
		() => "Nitrate",
		() => UICommon.IconNitrate
	);

	private static Asset<Texture2D> panelGrayscale;
	private static Asset<Texture2D> categoryPanelBorder;
	private VersionBrandingRecord[] records;
	private int selected;


	public VersionBrandingRenderer(params VersionBrandingRecord[] records)
	{
		this.records = records;
	}

	public void Draw(SpriteBatch sb, Color menuColor, float upBump)
	{
		static void drawIcon(SpriteBatch sb, Texture2D texture, Vector2 location, int index, ref int selected, int buttonSize)
		{
			const int icon_off = 0;

			var hitbox = new Rectangle((int)location.X, (int)location.Y, buttonSize, buttonSize);
			var hovered = hitbox.Intersects(new Rectangle((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 1, 1));

			Utils.DrawSplicedPanel(sb, panelGrayscale.Value, hitbox.X, hitbox.Y, hitbox.Width, hitbox.Height, 10, 10, 10, 10, selected == index ? new Color(152, 175, 235) : Colors.InventoryDefaultColor);

			// if (selected == index)
			// 	Utils.DrawSplicedPanel(sb, categoryPanelHighlight.Value, hitbox.X + 2, hitbox.Y + 2, hitbox.Width - 4, hitbox.Height - 4, 10, 10, 10, 10, Color.White);
			
			if (hovered) {
				if (Main.mouseLeft)
					selected = index;
			
				Utils.DrawSplicedPanel(sb, categoryPanelBorder.Value, hitbox.X, hitbox.Y, hitbox.Width, hitbox.Height, 10, 10, 10, 10, Color.White);
			}

			sb.Draw(texture, new Vector2(location.X + icon_off, location.Y + icon_off), Color.White);
		}

		static bool tryInit()
		{
			if (Main.Assets is null)
				return false;

			panelGrayscale = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/PanelGrayscale");
			categoryPanelBorder = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanelBorder");
			return true;
		}

		if (!tryInit())
			return;

		const int button_size = 32;
		const int padding = 4;
		const int content_padding = 10;
		const int title_link_size = 30;

		// Draw record list.
		{

			int offY = (Main.screenHeight - ((button_size + padding) * records.Length));

			for (var i = records.Length - 1; i >= 0; i--) {
				var record = records[i];

				if (record.Icon() is not { } icon || !icon.IsLoaded)
					continue;

				drawIcon(sb, icon.Value, new Vector2(padding, offY), i, ref selected, button_size);

				offY += button_size + padding;
			}
		}

		// Draw current record.
		{
			if (GetCurrentRecord() is not { } record)
				return;

			var text = record.Text();
			var origin = FontAssets.MouseText.Value.MeasureString(text) * 0.5f;

			for (var i = 0; i < 5; i++) {
				var color = Color.Black;
				if (i == 4) {
					color = menuColor;
					color.R = (byte)((255 + color.R) / 2);
					color.G = (byte)((255 + color.R) / 2);
					color.B = (byte)((255 + color.R) / 2);
				}

				color.A = (byte)((float)(int)color.A * 0.3f);

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

					offX += button_size + content_padding;
				}

				// Draw text.
				sb.DrawString(FontAssets.MouseText.Value, text, new Vector2(origin.X + offX, Main.screenHeight - origin.Y + offY - 2f - upBump), color, 0f, origin, 1f, SpriteEffects.None, 0f);
			}

			// Draw link buttons.
			var links = record.Buttons();
			var anchor = new Vector2(button_size + content_padding + (title_link_size / 2), Main.screenHeight - origin.Y - 2f - upBump - title_link_size);
			foreach (var link in links) {
				link.Draw(sb, anchor);
				anchor.X += title_link_size;
			}
		}
	}

	/*public void Update()
	{
		// TODO
	}*/

	private VersionBrandingRecord? GetCurrentRecord()
	{
		selected = Math.Clamp(selected, 0, records.Length);
		if (records.Length <= 0)
			return null;

		return records[selected];
	}

	// Taken from tML's implementation of Main::DrawVersionNumber.
	internal static string GetTmlVersionText()
	{
		// TODO: Reimplement support for Patreon message? (tML has this)
		// Probably not: tML has a title link button now.

		string supportMessage = Language.GetTextValue("tModLoader.PatreonSupport");
		// string patreonShortURL = @"patreon.com/tModLoader";
		// bool showPatreon = SocialAPI.Mode != SocialMode.Steam;

		// Show number of mods - 1 such as to show number of enabled mods that are not tModLoader itself
		// string modsMessage = Language.GetTextValue("tModLoader.MenuModsEnabled", Math.Max(0, ModLoader.ModLoader.Mods.Length - 1));

		return ModLoader.ModLoader.versionedName /*+ (showPatreon ? Environment.NewLine + supportMessage : "")*/;
	}

	internal static string GetVanillaVersionText()
	{
		return "Terraria " + Main.versionNumber;
	}

	internal static string GetNitrateVersionText()
	{
		return "Nitrate vTODO";
	}
}
