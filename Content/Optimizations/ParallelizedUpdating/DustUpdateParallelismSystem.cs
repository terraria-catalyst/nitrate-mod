using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Nitrate.Core.Features.Threading;

namespace Nitrate.Content.Optimizations.ParallelizedUpdating;

/// <summary>
///     Rewrites the dust update method to use parallelism since dust updating
///     (typically) isn't dependent on the states of other dust.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class DustUpdateParallelismSystem : ModSystem
{
    private static bool RunningInParallel;

    [ThreadStatic]
    [UsedImplicitly(ImplicitUseKindFlags.Access)]
    private static int DustFrom;

    [ThreadStatic]
    [UsedImplicitly(ImplicitUseKindFlags.Access)]
    private static int DustTo;

    [ThreadStatic]
    [UsedImplicitly(ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign)]
    private static int DustIndex;

    private static readonly FieldInfo dust_from_field = typeof(DustUpdateParallelismSystem).GetField(nameof(DustFrom), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly FieldInfo dust_to_field = typeof(DustUpdateParallelismSystem).GetField(nameof(DustTo), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly FieldInfo dust_index_field = typeof(DustUpdateParallelismSystem).GetField(nameof(DustIndex), BindingFlags.NonPublic | BindingFlags.Static)!;

    public override void OnModLoad()
    {
        base.OnModLoad();

        IL_Dust.UpdateDust += UpdateDustMakeThreadStaticParallel;
        On_Dust.UpdateDust += UpdateDustHandleThreadStaticParallel;
    }

    private static void UpdateDustMakeThreadStaticParallel(ILContext il)
    {
        // Rewrites Dust::UpdateDust to use our thread-static fields instead of
        // local variables and constant values.

        ILCursor c = new(il);

        // Navigate to the Main.maxDust constant used by the loop and use our
        // DustTo field instead.
        c.GotoNext(MoveType.After, x => x.MatchLdcI4(Main.maxDust));
        c.Emit(OpCodes.Pop);
        c.Emit(OpCodes.Ldsfld, dust_to_field);

        // Dynamically find the local index of the actual loop index variable.
        int loopVariableIndex = -1;
        c.GotoPrev(x => x.MatchLdloc(out loopVariableIndex));

        if (loopVariableIndex == -1)
        {
            throw new Exception("Could not find loop variable index");
        }

        // Find where the loop variable is initialized and use our DustFrom
        // field instead.
        c.Index = 0;
        c.GotoNext(x => x.MatchLdcI4(0), x => x.MatchStloc(loopVariableIndex));
        c.GotoPrev(MoveType.After, x => x.MatchLdcI4(0));
        c.Emit(OpCodes.Pop);
        c.Emit(OpCodes.Ldsfld, dust_from_field);

        // Replace references to the loop variable with our DustIndex field.

        // There exist some labels to these opcodes we normally want to remove,
        // so just change their opcodes and operands instead...

        c.Index = 0;

        while (c.TryGotoNext(MoveType.Before, x => x.MatchLdloc(loopVariableIndex)))
        {
            c.Next!.OpCode = OpCodes.Ldsfld;
            c.Next!.Operand = dust_index_field;
        }

        c.Index = 0;

        while (c.TryGotoNext(MoveType.Before, x => x.MatchStloc(loopVariableIndex)))
        {
            c.Next!.OpCode = OpCodes.Stsfld;
            c.Next!.Operand = dust_index_field;
        }
    }

    private static void UpdateDustHandleThreadStaticParallel(On_Dust.orig_UpdateDust orig)
    {
        if (RunningInParallel)
        {
            orig();

            return;
        }

        RunningInParallel = true;
        ThreadUnsafeCallWatchdog.Enable();

        FasterParallel.For(0, Main.maxDust, (inclusive, exclusive, _) =>
        {
            DustFrom = inclusive;
            DustTo = exclusive;

            orig();
        });

        RunningInParallel = false;
        ThreadUnsafeCallWatchdog.Disable();
    }
}