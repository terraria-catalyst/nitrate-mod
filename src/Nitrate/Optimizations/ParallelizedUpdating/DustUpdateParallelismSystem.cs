using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using TeamCatalyst.Nitrate.API.Listeners;
using TeamCatalyst.Nitrate.API.SIMD;
using TeamCatalyst.Nitrate.API.Threading;
using TeamCatalyst.Nitrate.Utilities;
using Terraria;
using Terraria.ModLoader;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace TeamCatalyst.Nitrate.Optimizations.ParallelizedUpdating;

/// <summary>
///     Rewrites the dust update method to use parallelism since dust updating
///     (typically) isn't dependent on the states of other dust.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class DustUpdateParallelismSystem : ModSystem {
    private static readonly MethodInfo update_dust_filler = Info.OfMethod("Nitrate", "TeamCatalyst.Nitrate.Optimizations.ParallelizedUpdating.DustUpdateParallelismSystem", "UpdateDustFiller");
    private static readonly MethodInfo inner_update_dust = Info.OfMethod("Nitrate", "TeamCatalyst.Nitrate.Optimizations.ParallelizedUpdating.DustUpdateParallelismSystem", "InnerUpdateDust");
    private static MethodBody? updateDustBody;

    private ILHook? updateDustFillerHook;

    public override void OnModLoad() {
        base.OnModLoad();

        IL_Dust.UpdateDust += il => updateDustBody = il.Body;
        updateDustFillerHook = new ILHook(update_dust_filler, UpdateDustFillerEdit);
        IL_Dust.UpdateDust += UpdateDustMakeThreadStaticParallel;
    }

    public override void Unload() {
        base.Unload();

        updateDustFillerHook?.Dispose();
        updateDustFillerHook = null;
    }

    private static void UpdateDustMakeThreadStaticParallel(ILContext il) {
        // Rewrites Dust::UpdateDust to use our thread-static fields instead of
        // local variables and constant values.

        ILCursor c = new(il);
        var skipLabel = c.DefineLabel();

        ILLabel? loopLabel = null;
        c.GotoNext(MoveType.Before, x => x.MatchBr(out loopLabel));
        c.Emit(OpCodes.Br, skipLabel);

        if (loopLabel is null) {
            throw new Exception("Could not find loop label");
        }

        c.GotoLabel(loopLabel);
        c.GotoNext(MoveType.After, x => x.MatchBlt(out _));
        c.Emit(OpCodes.Call, inner_update_dust);
        c.GotoPrev(MoveType.After, x => x.MatchBlt(out _));
        c.MarkLabel(skipLabel);
    }

    [UsedImplicitly(ImplicitUseKindFlags.Access)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InnerUpdateDust() {
        ThreadUnsafeCallWatchdog.Enable();

        FasterParallel.For(
            0,
            Main.maxDust,
            (inclusive, exclusive, _) => {
                UpdateDustFiller(inclusive, exclusive);
            }
        );

        ThreadUnsafeCallWatchdog.Disable();
    }

    private static void UpdateDustFillerEdit(ILContext il) {
        if (updateDustBody is null) {
            throw new Exception("Could not find Dust::UpdateDust method body");
        }

        ILCursor c = new(il);
        IntermediateLanguageUtil.CloneMethodBodyToCursor(updateDustBody, c);

        // Navigate to the Main.maxDust constant used by the loop and use our
        // exclusive parameter instead.
        c.GotoNext(MoveType.After, x => x.MatchLdcI4(Main.maxDust));
        c.Emit(OpCodes.Pop);
        c.Emit(OpCodes.Ldarg_1);

        // Dynamically find the local index of the actual loop index variable.
        var loopVariableIndex = -1;
        c.GotoPrev(x => x.MatchLdloc(out loopVariableIndex));

        if (loopVariableIndex == -1) {
            throw new Exception("Could not find loop variable index");
        }

        // Find where the loop variable is initialized and use our inclusive
        // parameter instead.
        c.Index = 0;
        c.GotoNext(x => x.MatchStloc(loopVariableIndex));
        c.GotoPrev(MoveType.After, x => x.MatchLdcI4(0));
        c.Emit(OpCodes.Pop);
        c.Emit(OpCodes.Ldarg_0);

        updateDustBody = null;

        Simdifier.Simdify(c);
    }

    // ReSharper disable UnusedParameter.Local
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UpdateDustFiller(int inclusive, int exclusive) { }
    // ReSharper restore UnusedParameter.Local
}
