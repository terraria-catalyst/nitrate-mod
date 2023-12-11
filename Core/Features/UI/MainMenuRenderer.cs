using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Nitrate.Content.UI;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace Nitrate.Core.Features.UI;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class MainMenuRenderer : ModSystem
{
    private static GameTime? GameTime;

    public UserInterface UserInterface { get; } = new();

    public override void Load()
    {
        base.Load();

        On_Main.DrawMenu += CaptureGameTime;
        On_Main.DrawSocialMediaButtons += DrawNitrateStuff;

        UserInterface.SetState(new MainMenuPanels());
    }

    private static void CaptureGameTime(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
    {
        GameTime = gameTime;
        orig(self, gameTime);
    }

    private void DrawNitrateStuff(On_Main.orig_DrawSocialMediaButtons orig, Color menuColor, float upBump)
    {
        orig(menuColor, upBump);

        if (GameTime is null)
        {
            return;
        }

        UserInterface.Update(GameTime);
        UserInterface.Draw(Main.spriteBatch, GameTime);
    }
}