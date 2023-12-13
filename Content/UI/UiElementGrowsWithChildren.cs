using Terraria.UI;

namespace Nitrate.Content.UI;

internal class UiElementGrowsWithChildren : UIElement
{
    public override void Recalculate()
    {
        RecalculateChildren();
    }
}