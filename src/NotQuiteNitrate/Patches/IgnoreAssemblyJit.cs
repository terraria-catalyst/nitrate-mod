/*using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace NotQuiteNitrate.Patches;

// TODO(conf): Add configuration option once our own system is added.

/// <summary>
///     Disables JITing of entire assemblies (during mod loading).
///     Significantly speeds up mod loading but may increase micro-stutter
///     in-game.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
internal sealed class IgnoreAssemblyJit : ModSystem
{
#pragma warning disable CA2255
    [ModuleInitializer]
    public static void ModuleLoad()
    {
        MonoModHooks.Add(
            typeof(AssemblyManager).GetMethod(nameof(AssemblyManager.JITAssembliesAsync), BindingFlags.Public | BindingFlags.Static),
            SkipJit
        );
    }
#pragma warning restore CA2255

    private static Task SkipJit(IEnumerable<Assembly> assemblies, PreJITFilter filter, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}*/


