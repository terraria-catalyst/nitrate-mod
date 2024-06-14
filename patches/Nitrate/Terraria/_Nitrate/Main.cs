using Terraria.Nitrate.UI.States;
using Terraria.Nitrate.VersionBranding;
using Terraria.Nitrate.VersionBranding.UI;

namespace Terraria;

partial class Main
{
	private static UILanguageSelectMenu languageSelectMenu_InitialLanguageSelect = new(true);
	private static UILanguageSelectMenu languageSelectMenu = new(false);

	private static readonly VersionBrandingRenderer version_branding_renderer = new(WellKnownVersionBrandings.VANILLA, WellKnownVersionBrandings.TML, WellKnownVersionBrandings.NITRATE);
}
