using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Zenith.Core.Features.Threading;

namespace Zenith.Content.Optimizations.ParallelizedUpdating;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class DustUpdateParallelismSystem : ModSystem
{
    private static bool RunningInParallel;

    [ThreadStatic]
    private static int DustFrom;

    [ThreadStatic]
    private static int DustTo;

    [ThreadStatic]
    private static int DustIndex;

    private static FieldInfo DustFromField = typeof(DustUpdateParallelismSystem).GetField(nameof(DustFrom), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static FieldInfo DustToField = typeof(DustUpdateParallelismSystem).GetField(nameof(DustTo), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static FieldInfo DustIndexField = typeof(DustUpdateParallelismSystem).GetField(nameof(DustIndex), BindingFlags.NonPublic | BindingFlags.Static)!;

    public override void OnModLoad()
    {
        base.OnModLoad();

        IL_Dust.UpdateDust += MakeThreadStaticParallel;
        On_Dust.UpdateDust += HandleThreadStaticParallel;
    }

    private static void MakeThreadStaticParallel(ILContext il)
    {
        ILCursor c = new(il);

        int loopVariableIndex = -1;
        c.GotoNext(MoveType.After, x => x.MatchLdcI4(Main.maxDust));
        c.Emit(OpCodes.Pop);
        c.Emit(OpCodes.Ldsfld, DustToField);

        c.GotoPrev(x => x.MatchLdloc(out loopVariableIndex));

        if (loopVariableIndex == -1)
        {
            throw new Exception("Could not find loop variable index");
        }

        c.Index = 0;
        c.GotoNext(x => x.MatchLdcI4(0), x => x.MatchStloc(loopVariableIndex));
        c.GotoPrev(MoveType.After, x => x.MatchLdcI4(0));
        c.Emit(OpCodes.Pop);
        c.Emit(OpCodes.Ldsfld, DustFromField);

        // There exist some labels to these opcodes we normally want to remove,
        // so just change their opcodes and operands instead...

        c.Index = 0;

        while (c.TryGotoNext(MoveType.Before, x => x.MatchLdloc(loopVariableIndex)))
        {
            // c.Remove();
            // c.Emit(OpCodes.Ldsfld, DustIndexField);
            c.Next!.OpCode = OpCodes.Ldsfld;
            c.Next!.Operand = DustIndexField;
        }

        c.Index = 0;

        while (c.TryGotoNext(MoveType.Before, x => x.MatchStloc(loopVariableIndex)))
        {
            // c.Remove();
            // c.Emit(OpCodes.Stsfld, DustIndexField);
            c.Next!.OpCode = OpCodes.Stsfld;
            c.Next!.Operand = DustIndexField;
        }
    }

    private static void HandleThreadStaticParallel(On_Dust.orig_UpdateDust orig)
    {
        if (RunningInParallel)
        {
            orig();

            return;
        }

        RunningInParallel = true;

        FasterParallel.For(0, Main.maxDust, (inclusive, exclusive, _) =>
        {
            DustFrom = inclusive;
            DustTo = exclusive;

            // for (DustIndex = DustFrom; DustIndex < DustTo; DustIndex++)
            // {
            DustIndex = DustFrom;
            orig();
            // }
        });
    }
}