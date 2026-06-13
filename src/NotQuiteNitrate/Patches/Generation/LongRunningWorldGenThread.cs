/*using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace NotQuiteNitrate.Patches.Generation;

// I'm not confident this provides a notable boost, but it doesn't hurt to have.
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class LongRunningWorldGenThread : ModSystem
{
    public override void Load()
    {
        base.Load();

        On_WorldGen.CreateNewWorld += InitializeLongRunningTask;
    }

    private static Task InitializeLongRunningTask(On_WorldGen.orig_CreateNewWorld orig, GenerationProgress progress)
    {
        WorldGen.generatingWorld = true;
        Main.rand                = new UnifiedRandom(Main.ActiveWorldFileData.Seed);
        WorldGen.gen             = true;
        Main.menuMode            = 888;

        try
        {
            Main.MenuUI.SetState(new UIWorldLoad());
        }
        catch
        {
            // ignore
        }

        return Task.Factory.StartNew(
            ctx =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                WorldGen.worldGenCallback(ctx);
            },
            progress,
            TaskCreationOptions.LongRunning
        );
    }
}*/


