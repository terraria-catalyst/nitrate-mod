using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace Nitrate.Content.UI;

internal sealed class MainMenuPanels : UIState
{
    private const int padding = 8;

    public override void OnInitialize()
    {
        base.OnInitialize();

        UIList list = new()
        {
            ListPadding = padding,
        };

        list.Width.Set(0f, 1f);
        list.Height.Set(0f, 1f);
        list.Top.Set(padding, 0f);
        list.Left.Set(padding, 0f);

        list.Add(CreateTitlePanel());
        list.Add(CreateDonationPanel());
        list.Add(CreateDebugPanel());

        Append(list);
    }

    private static UIPanel CreateTitlePanel()
    {
        UIPanel panel = new();
        panel.Width.Set(425f, 0f);
        panel.Height.Set(24f * 4f, 0f);

        UIGrid titleGrid = new()
        {
            Width = { Percent = 1f },
            Height = { Percent = 1f },
        };

        UIText titleText = new("Nitrate");

        UIList versionList = new()
        {
            new UIElement
            {
                Width = StyleDimension.FromPercent(0f),
                Height = StyleDimension.FromPixels(24f * 0.25f),
            },
            new UIText($"v{ModContent.GetInstance<NitrateMod>().Version} :)", 0.75f),
        };

        versionList.ListPadding = 0f;
        versionList.SetPadding(0f);
        //versionList.SetM

        versionList.Width.Set(100f, 0f);
        versionList.Height.Set(0f, 1f);

        titleGrid.Add(titleText);
        titleGrid.Add(versionList);

        panel.Append(titleGrid);

        return panel;
    }

    private static UIPanel CreateDonationPanel()
    {
        UIPanel panel = new();
        panel.Width.Set(100f, 0f);
        panel.Height.Set(100f, 0f);

        return panel;
    }

    private static UIPanel CreateDebugPanel()
    {
        UIPanel panel = new();
        panel.Width.Set(100f, 0f);
        panel.Height.Set(100f, 0f);

        return panel;
    }
}