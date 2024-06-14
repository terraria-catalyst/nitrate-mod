#nullable enable

using System.Collections.Generic;

using Microsoft.Xna.Framework.Graphics;

using ReLogic.Content;

using Terraria.DataStructures;

namespace Terraria.Nitrate.VersionBranding;

/// <summary>
///		Contains and controls the displaying of the versioning and branding of a
///		particular mod.
/// </summary>
public abstract class VersionBrandingRecord
{
	public abstract string Text { get; }

	public abstract List<TitleLinkButton> Buttons { get; }

	public abstract Asset<Texture2D>? Icon { get; set; }
}
