using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Nitrate.Core.Utilities.Simdifier;

internal abstract class AbstractSimdifier<TFrom, TTo> : ISimdifier
    where TFrom : struct
    where TTo : struct
{
    private readonly Dictionary<string, Type> _variableRemap = new();
    private readonly Dictionary<string, MethodInfo> _instanceRemap = new();
    private readonly Dictionary<string, MethodInfo> _newobjRemap = new();

    private readonly Dictionary<string, MethodDefinition> _lightweightMethodWrapperRemap = new();

    protected AbstractSimdifier()
    {
        ReplaceVariable(typeof(TFrom), typeof(TTo));
    }

    public virtual void Simdify(ILCursor c)
    {
        foreach (VariableDefinition variable in c.Body.Variables)
        {
            if (!_variableRemap.TryGetValue(variable.VariableType.FullName, out Type? to))
            {
                continue;
            }

            variable.VariableType = c.Body.Method.Module.ImportReference(to);
        }

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

        foreach (Instruction instr in c.Instrs)
        {
            if (instr.OpCode != OpCodes.Call)
            {
                continue;
            }

            if (instr.Operand is not MethodReference methodReference)
            {
                continue;
            }

            if (!methodReference.HasParameters)
            {
                continue;
            }

            if (methodReference.Parameters.All(x => x.ParameterType.FullName != typeof(TFrom).FullName))
            {
                continue;
            }

            if (_lightweightMethodWrapperRemap.TryGetValue(methodReference.FullName, out MethodDefinition? wrapper))
            {
                instr.Operand = c.Body.Method.Module.ImportReference(wrapper);

                continue;
            }

            wrapper = _lightweightMethodWrapperRemap[methodReference.FullName] = GenerateLightweightMethodWrapper(methodReference, c.Module);
            instr.Operand = c.Body.Method.Module.ImportReference(wrapper);
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

    private MethodDefinition GenerateLightweightMethodWrapper(MethodReference methodReference, ModuleDefinition module)
    {
        MethodDefinition wrapper = new(methodReference.Name + "_LightweightWrapper", MethodAttributes.Private | MethodAttributes.Static, methodReference.ReturnType)
        {
            AggressiveInlining = true,
        };

        foreach (ParameterDefinition parameter in methodReference.Parameters)
        {
            wrapper.Parameters.Add(parameter.ParameterType.FullName == typeof(TFrom).FullName
                ? new ParameterDefinition(parameter.Name, parameter.Attributes, module.ImportReference(typeof(TTo)))
                : new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.ParameterType));
        }

        ILCursor c = new(new ILContext(wrapper));

        // push each argument onto the stack and call the original method but convert the arguments to the original type
        for (int i = 0; i < wrapper.Parameters.Count; i++)
        {
            c.Emit(OpCodes.Ldarg, i);

            if (wrapper.Parameters[i].ParameterType.FullName == typeof(TTo).FullName)
            {
                c.Emit(OpCodes.Call, GetType().GetMethod("As", BindingFlags.Static | BindingFlags.NonPublic)!);
            }
        }

        c.Emit(OpCodes.Call, methodReference);
        c.Emit(OpCodes.Ret);

        return wrapper;
    }
}