#nullable enable

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework.Graphics;

using ReLogic.Content;

using Terraria.DataStructures;

namespace Terraria.Nitrate.VersionBranding;

/// <summary>
///		Defines well-known version brandings.
/// </summary>
public static class WellKnownVersionBrandings
{
	/// <summary>
	///		<see cref="VersionBrandingRecord"/> implementation for vanilla and
	///		tModLoader displays.
	/// </summary>
	// The `buttons` parameter is a function because tModLoaderTitleLinks gets
	// initialized after the VersionBrandingRenderer instance does.
	private sealed class LegacyVersionBrandingRecord(Func<string> getText, Func<List<TitleLinkButton>> buttons) : VersionBrandingRecord
	{
		public override string Text => getText();

		public override List<TitleLinkButton> Buttons => buttons();

		public override Asset<Texture2D>? Icon { get; set; }
	}

	private sealed class NitrateVersionBrandingRecord : VersionBrandingRecord
	{
		public override string Text => "Nitrate vTODO";

		public override List<TitleLinkButton> Buttons { get; } = [];

		public override Asset<Texture2D>? Icon { get; set; }
	}

#pragma warning disable CS0618 // Type or member is obsolete
	/// <summary>
	///		Vanilla branding and text.
	/// </summary>
	public static readonly VersionBrandingRecord VANILLA = new LegacyVersionBrandingRecord(() => "Terraria " + Main.versionNumber, () => Main.TitleLinks);

	/// <summary>
	///		tModLoader branding and text.
	/// </summary>
	public static readonly VersionBrandingRecord TML = new LegacyVersionBrandingRecord(() => ModLoader.ModLoader.versionedName, () => Main.tModLoaderTitleLinks);
#pragma warning restore CS0618 // Type or member is obsolete

	/// <summary>
	///		Nitrate branding and text.
	/// </summary>
	public static readonly VersionBrandingRecord NITRATE = new NitrateVersionBrandingRecord();
}
