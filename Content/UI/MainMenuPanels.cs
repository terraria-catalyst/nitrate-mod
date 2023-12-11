using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Nitrate.Content.UI;

internal sealed class MainMenuPanels : UIState
{
    public override void OnInitialize()
    {
        base.OnInitialize();

        UIPanel panel = new();
        panel.Width.Set(200, 0);
        panel.Height.Set(200, 0);
        panel.Top.Set(200, 0);
        panel.Left.Set(200, 0);

        Append(panel);
    }
}