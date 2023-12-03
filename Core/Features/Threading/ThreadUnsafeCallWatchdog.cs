using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;
using Terraria;
using Terraria.ModLoader;

namespace Nitrate.Core.Features.Threading;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class ThreadUnsafeCallWatchdog : ModSystem
{
    private static bool enabled;
    private static readonly ConcurrentQueue<Action> unsafe_calls = new();

    public static void Enable()
    {
        enabled = true;
    }

    public static void Disable()
    {
        enabled = false;

        foreach (Action action in unsafe_calls)
        {
            action();
        }

        unsafe_calls.Clear();
    }

    public static void QueueUnsafeCall(Action action)
    {
        if (enabled)
        {
            unsafe_calls.Enqueue(action);
        }
        else
        {
            action();
        }
    }

    public override void Load()
    {
        base.Load();

        On_Lighting.AddLight_int_int_float_float_float += (orig, i, i1, f, f1, f2) => QueueUnsafeCall(() => orig(i, i1, f, f1, f2));
    }
}