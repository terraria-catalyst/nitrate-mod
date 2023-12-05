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
using System.Collections.Concurrent;
using Terraria.DataStructures;
using Terraria.Graphics.Light;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

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

    private static bool UpdatingDust;

    private static List<(int, int, Vector3)> LightCache = new();

    public override void OnModLoad()
    {
        base.OnModLoad();

        IL_Dust.UpdateDust += il => UpdateDustBody = il.Body;
        _updateDustFillerHook = new ILHook(typeof(DustUpdateParallelismSystem).GetMethod(nameof(UpdateDustFiller), BindingFlags.NonPublic | BindingFlags.Static)!, UpdateDustFillerEdit);
        IL_Dust.UpdateDust += UpdateDustMakeThreadStaticParallel;

        IL_Lighting.AddLight_int_int_float_float_float += QueueAddLightCall;
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
        UpdatingDust = true;

        FasterParallel.For(0, Main.maxDust, (inclusive, exclusive, _) =>
        {
            UpdateDustFiller(inclusive, exclusive);
        });

        UpdatingDust = false;

        for (int i = 0; i < LightCache.Count; i++)
        {
            (int, int, Vector3) light = LightCache[i];

            Lighting._activeEngine.AddLight(light.Item1, light.Item2, light.Item3);
        }

        LightCache.Clear();
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

        c.Simdify();
    }

    // ReSharper disable UnusedParameter.Local
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UpdateDustFiller(int inclusive, int exclusive)
    {
    }
    // ReSharper restore UnusedParameter.Local

    private void QueueAddLightCall(ILContext il)
    {
        ILCursor c = new(il);

        // Emit a delegate that adds the lighting info to the queue and then returns, instead of the default behaviour.
        for (int i = 0; i < 5; i++)
        {
            c.Emit(OpCodes.Ldarg, i);
        }

        c.EmitDelegate<Action<int, int, float, float, float>>((x, y, r, g, b) =>
        {
            LightCache.Add((x, y, new Vector3(r, g, b)));
        });
        c.Emit(OpCodes.Ret);

        // Mark a label at the start of the default behaviour.
        ILLabel defaultMethodStart = c.DefineLabel();

        c.MarkLabel(defaultMethodStart);

        c.Index = 0;

        FieldInfo? updatingDust = typeof(DustUpdateParallelismSystem).GetField("UpdatingDust", BindingFlags.Static | BindingFlags.NonPublic);

        if (updatingDust is null)
        {
            throw new Exception($"Could not find field {nameof(UpdatingDust)}.");
        }

        // Branch to the default behaviour if dusts aren't being updated.
        c.Emit(OpCodes.Ldsfld, updatingDust);
        c.Emit(OpCodes.Brfalse, defaultMethodStart);
    }
}