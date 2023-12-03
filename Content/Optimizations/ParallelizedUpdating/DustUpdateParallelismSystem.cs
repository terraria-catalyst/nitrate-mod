using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
using Nitrate.Core.Features.Threading;
using Nitrate.Core.Utilities;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace Nitrate.Content.Optimizations.ParallelizedUpdating;

/// <summary>
///     Rewrites the dust update method to use parallelism since dust updating
///     (typically) isn't dependent on the states of other dust.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class DustUpdateParallelismSystem : ModSystem
{
    private static readonly MethodInfo inner_update_dust_method = typeof(DustUpdateParallelismSystem).GetMethod(nameof(InnerUpdateDust), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static MethodBody? UpdateDustBody;

    private ILHook? _updateDustFillerHook;

    public override void OnModLoad()
    {
        base.OnModLoad();

        IL_Dust.UpdateDust += il => UpdateDustBody = il.Body;
        _updateDustFillerHook = new ILHook(typeof(DustUpdateParallelismSystem).GetMethod(nameof(UpdateDustFiller), BindingFlags.NonPublic | BindingFlags.Static)!, UpdateDustFillerEdit);
        IL_Dust.UpdateDust += UpdateDustMakeThreadStaticParallel;
    }

    public override void Unload()
    {
        base.Unload();

        _updateDustFillerHook?.Dispose();
        _updateDustFillerHook = null;
    }

    private static void UpdateDustMakeThreadStaticParallel(ILContext il)
    {
        // Rewrites Dust::UpdateDust to use our thread-static fields instead of
        // local variables and constant values.

        ILCursor c = new(il);
        ILLabel skipLabel = c.DefineLabel();

        ILLabel? loopLabel = null;
        c.GotoNext(MoveType.Before, x => x.MatchBr(out loopLabel));
        c.Emit(OpCodes.Br, skipLabel);

        if (loopLabel is null)
        {
            throw new Exception("Could not find loop label");
        }

        c.GotoLabel(loopLabel);
        c.GotoNext(MoveType.After, x => x.MatchBlt(out _));
        c.Emit(OpCodes.Call, inner_update_dust_method);
        c.GotoPrev(MoveType.After, x => x.MatchBlt(out _));
        c.MarkLabel(skipLabel);
    }

    [UsedImplicitly(ImplicitUseKindFlags.Access)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InnerUpdateDust()
    {
        FasterParallel.For(0, Main.maxDust, (inclusive, exclusive, _) =>
        {
            UpdateDustFiller(inclusive, exclusive);
        });
    }

    private static void UpdateDustFillerEdit(ILContext il)
    {
        if (UpdateDustBody is null)
        {
            throw new Exception("Could not find Dust::UpdateDust method body");
        }

        ILCursor c = new(il);
        IntermediateLanguageUtil.CloneMethodBodyToCursor(UpdateDustBody, c);

        // Navigate to the Main.maxDust constant used by the loop and use our
        // exclusive parameter instead.
        c.GotoNext(MoveType.After, x => x.MatchLdcI4(Main.maxDust));
        c.Emit(OpCodes.Pop);
        c.Emit(OpCodes.Ldarg_1);

        // Dynamically find the local index of the actual loop index variable.
        int loopVariableIndex = -1;
        c.GotoPrev(x => x.MatchLdloc(out loopVariableIndex));

        if (loopVariableIndex == -1)
        {
            throw new Exception("Could not find loop variable index");
        }

        // Find where the loop variable is initialized and use our inclusive
        // parameter instead.
        c.Index = 0;
        c.GotoNext(x => x.MatchStloc(loopVariableIndex));
        c.GotoPrev(MoveType.After, x => x.MatchLdcI4(0));
        c.Emit(OpCodes.Pop);
        c.Emit(OpCodes.Ldarg_0);

        UpdateDustBody = null;

        MonoModHooks.DumpIL(ModContent.GetInstance<NitrateMod>(), il);
    }

    // ReSharper disable UnusedParameter.Local
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UpdateDustFiller(int inclusive, int exclusive)
    {
    }
    // ReSharper restore UnusedParameter.Local
}