/* Copyright (c) 2025  Tomat et al.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

/* DynamicModCalls.cs - performant, safe, ioc-based Mod::Call api
 *
 * Project URL: https://github.com/steviegt6/terraria-mods
 *
 * Usage:
 *   in your `Mod` implementation:
 *     - override Mod::Call:
 *       public override object? Call(params object?[]? args)
 *           => return CallHandler.HandleCall(this, args);
 *
 * DynamicModCalls implements a `ModCall` type extending `ModType` and handles
 * its registration, accepting any loaded types as input.  `ModCall` provides
 * an enumeration of strings `CallCommands` serving as a list of aliases to a
 * command, and a delegate `Delegate` which is the method to invoke upon a call.
 * When calling `Mod::Call(x, ...)` with this API installed, the `CallHandler`
 * will identify the `ModCall` "x" based on provided `CallCommands` and invoke
 * the delegate `Delegate` with the additional parameters `...` (optional).
 * `CallHandler` creates a method at runtime to validate and cast incoming
 * parameters `...` at runtime with native IL performance (no reflection).
 *
 * Example `ModCall` implementation:
 *   internal sealed class MyCall : ModCall
 *   {
 *       public override IEnumerable<string> CallCommands { get; }
 *           = new[] { "myCommand" };
 *
 *       public override Delegate Delegate { get; } = Implementation;
 *
 *       private static int Implementation(object a, Item b, string[] c)
 *       {
 *           // ...implementation
 *           return 1; // some value
 *       }
 *   }
 *
 * This `ModCall` "MyCall" will be registered to the `CallHandler` and may be
 * invoked with the given alias "myCommand" like so:
 *   (int)mod.Call("myCommand", someObject, Main.item[0], new string[0]);
 *
 * The `CallHandler` will confirm the given arguments are of the correct types
 * before invoking the method `Implementation` assigned to the delegate
 * `Delegate` and will generate a method to quickly box and unbox any necessary
 * values.
 */

#if JETBRAINS_ANNOTATIONS
#define INCLUDE_ANNOTATIONS
#endif

#if INCLUDE_ANNOTATIONS
using JetBrains.Annotations;
#endif

#if INCLUDE_METADATA
using Tomat.Runtime.CompilerServices;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Terraria.ModLoader;
using TmlMod = Terraria.ModLoader.Mod;

namespace Tomat.TML.Library.DynamicModCalls;

#if INCLUDE_METADATA
file static class Meta
{
    public const string NAME = "DynamicModCalls";
    public const string VERSION = "1.0.0";
}
#endif

#if INCLUDE_ANNOTATIONS
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature, ImplicitUseTargetFlags.WithInheritors)]
#endif
#if INCLUDE_METADATA
[IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
internal abstract class ModCall : ModType
{
    /// <summary>
    ///     A collection of (interpreted-as-case-insensitive) call "commands".
    /// </summary>
#if INCLUDE_METADATA
    [IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
    public abstract IEnumerable<string> CallCommands { get; }

    /// <summary>
    ///     The delegate to invoke.
    /// </summary>
#if INCLUDE_METADATA
    [IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
    public abstract Delegate Delegate { get; }

#if INCLUDE_METADATA
    [IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
    protected sealed override void Register()
    {
        CallHandler.Register(Mod, this);
    }
}

#if INCLUDE_ANNOTATIONS
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
#endif
#if INCLUDE_METADATA
[IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
internal sealed class CallHandler : ModSystem
{
#if INCLUDE_METADATA
    [IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
    private sealed class CallInfoCache
    {
#if INCLUDE_METADATA
        [IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
        private readonly List<ModCall> callCache = [];

#if INCLUDE_METADATA
        [IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
        private readonly Dictionary<ModCall, Func<object?[], object?>> invokeCache = [];

#if INCLUDE_METADATA
        [IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
        public void AddCall(ModCall call)
        {
            callCache.Add(call);
        }

#if INCLUDE_METADATA
        [IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
        public ModCall? GetCallByCommand(string command)
        {
            return callCache.FirstOrDefault(
                x => x.CallCommands.Contains(
                    command,
                    StringComparer.OrdinalIgnoreCase
                )
            );
        }

#if INCLUDE_METADATA
        [IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
        public object? InvokeCall(ModCall call, object?[]? args)
        {
            return GetOrCreateInvoke(call, args)(args!);
        }

#if INCLUDE_METADATA
        [IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
        private Func<object?[], object?> GetOrCreateInvoke(ModCall call, object?[]? args)
        {
            var method = call.Delegate.Method;
            var parameters = method.GetParameters();

            if (args is null || args.Length != parameters.Length)
            {
                throw new ArgumentException(
                    $"ModCall::Invoke expected {parameters.Length} arguments, but got {args?.Length ?? 0}."
                );
            }

            if (invokeCache.TryGetValue(call, out var invoke))
            {
                return invoke;
            }

            var dynMethod = new DynamicMethod(
                "Invoke",
                typeof(object),
                [typeof(object[])],
                typeof(CallInfoCache).Module
            );

            var il = dynMethod.GetILGenerator();

            // Validate argument inputs.
            {
                for (var i = 0; i < parameters.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);
                    il.Emit(OpCodes.Isinst, parameters[i].ParameterType);
                    var label = il.DefineLabel();
                    il.Emit(OpCodes.Brtrue_S, label);
                    il.Emit(OpCodes.Ldstr, $"Argument {i} is not of type {parameters[i].ParameterType}.");
                    il.Emit(OpCodes.Newobj, typeof(ArgumentException).GetConstructor([typeof(string)])!);
                    il.Emit(OpCodes.Throw);
                    il.MarkLabel(label);
                }
            }

            // Invoke the delegate.
            {
                // Prepare arguments (push to stack).
                for (var i = 0; i < parameters.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);
                    il.Emit(OpCodes.Unbox_Any, parameters[i].ParameterType);
                }

                il.Emit(OpCodes.Call, call.Delegate.Method);

                // Handle return type cases.  If the delegate returns void, push
                // null manually since we need an object.  If it's a value type,
                // box it.
                if (call.Delegate.Method.ReturnType == typeof(void))
                {
                    il.Emit(OpCodes.Ldnull);
                }
                else if (call.Delegate.Method.ReturnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, call.Delegate.Method.ReturnType);
                }

                il.Emit(OpCodes.Ret);
            }

            return invokeCache[call] = (Func<object?[], object?>)dynMethod.CreateDelegate(typeof(Func<object?[], object?>));
        }
    }

#if INCLUDE_METADATA
    [IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
    private static readonly Dictionary<TmlMod, CallInfoCache> calls = [];

#if INCLUDE_METADATA
    [IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
    public static void Register(TmlMod mod, ModCall call)
    {
        if (!calls.TryGetValue(mod, out var cache))
        {
            calls[mod] = cache = new CallInfoCache();
        }

        cache.AddCall(call);
    }

#if INCLUDE_METADATA
    [IncludedFrom(Meta.NAME, Meta.VERSION)]
#endif
    public static object? HandleCall(TmlMod mod, object?[]? args)
    {
        if (!calls.TryGetValue(mod, out var cache))
        {
            return null;
        }

        if (args?.Length < 1 || args?[0] is not string command)
        {
            throw new ArgumentException("Mod::Call invocation expected a string call command name as the first argument.");
        }

        if (cache.GetCallByCommand(command) is not { } modCall)
        {
            throw new ArgumentException($"Mod::Call invocation could not find a call command named '{command}'.");
        }

        return cache.InvokeCall(modCall, args[1..]);
    }
}
