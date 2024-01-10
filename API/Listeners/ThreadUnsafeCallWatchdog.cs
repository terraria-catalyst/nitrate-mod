using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;
using Terraria;
using Terraria.ModLoader;

namespace Nitrate.API.Listeners;

/// <summary>
///     A toggleable watchdog that may capture common thread-unsafe calls in
///     various newly-parallelized callsites.
/// </summary>
public static class ThreadUnsafeCallWatchdog
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    private sealed class ThreadUnsafeCallWatchdogImpl : ModSystem
    {
        public override void Load()
        {
            base.Load();

            On_Lighting.AddLight_int_int_float_float_float += AddLight_int_int_float_float_float;
        }

        private static void AddLight_int_int_float_float_float(On_Lighting.orig_AddLight_int_int_float_float_float orig, int i, int j, float r, float g, float b)
        {
            if (Enabled)
            {
                actions.Add(() => orig(i, j, r, g, b));

                return;
            }

            orig(i, j, r, g, b);
        }
    }

    /// <summary>
    ///     Whether the watchdog is currently enabled.
    /// </summary>
    public static bool Enabled { get; private set; }

    private static readonly ConcurrentBag<Action> actions = new();

    /// <summary>
    ///     Enables the watchdog.
    /// </summary>
    public static void Enable()
    {
        Enabled = true;
        actions.Clear();
    }

    /// <summary>
    ///     Disables the watchdog.
    /// </summary>
    public static void Disable()
    {
        Enabled = false;

        foreach (Action action in actions)
        {
            action();
        }

        actions.Clear();
    }
}