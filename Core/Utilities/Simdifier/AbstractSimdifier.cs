using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Reflection;

namespace Nitrate.Core.Utilities.Simdifier;

internal abstract class AbstractSimdifier : ISimdifier
{
    private readonly Dictionary<string, MethodInfo> _instanceRemap = new();

    public virtual void Simdify(ILCursor c)
    {
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
        }
    }

    protected void ReplaceCall(string fromSignature, string toName)
    {
        _instanceRemap.Add(fromSignature, GetType().GetMethod(toName, BindingFlags.Static | BindingFlags.NonPublic)!);
    }
}