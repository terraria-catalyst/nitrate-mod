using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace Nitrate.Core.Utilities.Simdifier;

internal abstract class AbstractSimdifier<TFrom, TTo> : ISimdifier
    where TFrom : struct
    where TTo : struct
{
    private readonly Dictionary<string, Type> _variableRemap = new();
    private readonly Dictionary<string, MethodInfo> _instanceRemap = new();
    private readonly Dictionary<string, MethodInfo> _newobjRemap = new();

    private readonly Dictionary<string, MethodInfo> _lightweightMethodWrapperRemap = new();

    protected AbstractSimdifier()
    {
        ReplaceVariable(typeof(TFrom), typeof(TTo));
    }

    public virtual void Simdify(ILCursor c)
    {
        // Modify variables to use the new, expected type.
        /*foreach (VariableDefinition variable in c.Body.Variables)
        {
            if (!_variableRemap.TryGetValue(variable.VariableType.FullName, out Type? to))
            {
                continue;
            }

            variable.VariableType = c.Body.Method.Module.ImportReference(to);
        }*/

        // Replace method calls from the old type to the new type.
        foreach (Instruction instr in c.Instrs)
        {
            if (instr.OpCode == OpCodes.Call)
            {
                if (instr.Operand is not MethodReference methodReference)
                {
                    continue;
                }

                foreach (KeyValuePair<string, MethodInfo> remap in _instanceRemap)
                {
                    if (methodReference.FullName != remap.Key)
                    {
                        continue;
                    }

                    instr.Operand = c.Body.Method.Module.ImportReference(remap.Value);
                }
            }
            else if (instr.OpCode == OpCodes.Newobj)
            {
                if (instr.Operand is not MethodReference methodReference)
                {
                    continue;
                }

                foreach (KeyValuePair<string, MethodInfo> remap in _newobjRemap)
                {
                    if (methodReference.FullName != remap.Key)
                    {
                        continue;
                    }

                    instr.Operand = c.Body.Method.Module.ImportReference(remap.Value);
                }
            }
        }

        // Transform types to and from any relevant instructions.

        c.Index = 0;
        FieldReference? fieldRef = null;

        /*while (c.TryGotoNext(MoveType.After, x => x.MatchLdfld(out fieldRef)))
        {
            if (fieldRef is null)
            {
                continue;
            }

            if (fieldRef.FieldType.FullName != typeof(TFrom).FullName)
            {
                continue;
            }

            c.Emit(OpCodes.Call, GetType().GetMethod("As", BindingFlags.Static | BindingFlags.NonPublic)!);
        }

        c.Index = 0;
        fieldRef = null;

        while (c.TryGotoNext(MoveType.After, x => x.MatchLdsfld(out fieldRef)))
        {
            if (fieldRef is null)
            {
                continue;
            }

            if (fieldRef.FieldType.FullName != typeof(TFrom).FullName)
            {
                continue;
            }

            c.Emit(OpCodes.Call, GetType().GetMethod("As", BindingFlags.Static | BindingFlags.NonPublic)!);
        }*/

        c.Index = 0;
        fieldRef = null;

        while (c.TryGotoNext(MoveType.Before, x => x.MatchStfld(out fieldRef)))
        {
            if (fieldRef is null)
            {
                continue;
            }

            if (fieldRef.FieldType.FullName != typeof(TFrom).FullName)
            {
                continue;
            }

            c.Emit(OpCodes.Call, GetType().GetMethod("Undo", BindingFlags.Static | BindingFlags.NonPublic)!);
            c.Index++;
        }

        c.Index = 0;
        fieldRef = null;

        while (c.TryGotoNext(MoveType.Before, x => x.MatchStsfld(out fieldRef)))
        {
            if (fieldRef is null)
            {
                continue;
            }

            if (fieldRef.FieldType.FullName != typeof(TFrom).FullName)
            {
                continue;
            }

            c.Emit(OpCodes.Call, GetType().GetMethod("Undo", BindingFlags.Static | BindingFlags.NonPublic)!);
            c.Index++;
        }

        // Do the same for calls.
        c.Index = 0;
        MethodReference? methRef = null;

        while (c.TryGotoNext(MoveType.After, x => x.MatchCall(out methRef)))
        {
            if (methRef is null)
            {
                continue;
            }

            if (methRef.ReturnType.FullName != typeof(TFrom).FullName)
            {
                continue;
            }

            if (methRef.Name is "As" or "Undo")
            {
                continue;
            }

            c.Emit(OpCodes.Call, GetType().GetMethod("As", BindingFlags.Static | BindingFlags.NonPublic)!);
        }

        // Wrap method calls that expect the old type.
        methRef = null;

        while (c.TryGotoNext(MoveType.Before, x => x.MatchCall(out methRef)))
        {
            if (methRef is null)
            {
                continue;
            }

            if (!methRef.HasParameters)
            {
                continue;
            }

            if (methRef.Parameters.All(x => x.ParameterType.FullName != typeof(TFrom).FullName))
            {
                continue;
            }

            if (_instanceRemap.ContainsValue((MethodInfo)methRef.ResolveReflection()) || _newobjRemap.ContainsValue((MethodInfo)methRef.ResolveReflection()))
            {
                continue;
            }

            c.Remove();

            if (!_lightweightMethodWrapperRemap.TryGetValue(methRef.FullName, out MethodInfo? wrapper))
            {
                wrapper = _lightweightMethodWrapperRemap[methRef.FullName] = GenerateLightweightMethodWrapper(methRef);
            }

            c.Emit(OpCodes.Call, wrapper);
        }
    }

    protected void ReplaceVariable(Type from, Type to)
    {
        _variableRemap.Add(from.FullName!, to);
    }

    protected void ReplaceCall(string fromSignature, string toName)
    {
        _instanceRemap.Add(fromSignature, GetType().GetMethod(toName, BindingFlags.Static | BindingFlags.NonPublic)!);
    }

    protected void ReplaceNewobj(string fromSignature, string toName)
    {
        _newobjRemap.Add(fromSignature, GetType().GetMethod(toName, BindingFlags.Static | BindingFlags.NonPublic)!);
    }

    private MethodInfo GenerateLightweightMethodWrapper(MethodReference methodReference)
    {
        List<Type> parameters = methodReference.Parameters.Select(parameter => parameter.ParameterType.FullName == typeof(TFrom).FullName ? typeof(TTo) : parameter.ParameterType.ResolveReflection()).ToList();
        DynamicMethod wrapper = new(methodReference.Name + "_LightweightWrapper", methodReference.ReturnType.ResolveReflection(), parameters.ToArray());
        // I hate the standard library...
        // wrapper.MethodImplementationFlags = MethodImplAttributes.AggressiveInlining;

        ILGenerator il = wrapper.GetILGenerator();

        for (int i = 0; i < parameters.Count; i++)
        {
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg, i);

            if (parameters[i] == typeof(TTo))
            {
                il.Emit(System.Reflection.Emit.OpCodes.Call, GetType().GetMethod("As", BindingFlags.Static | BindingFlags.NonPublic)!);
            }
        }

        il.Emit(System.Reflection.Emit.OpCodes.Call, (MethodInfo)methodReference.ResolveReflection());
        il.Emit(System.Reflection.Emit.OpCodes.Ret);

        return wrapper;
    }
}