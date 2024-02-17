using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Nitrate.API.SIMD;
using System.Collections.Generic;
using System.Reflection;

namespace Nitrate.SIMD;

internal abstract class AbstractSimdifier : ISimdifier {
    private readonly Dictionary<string, MethodInfo> instanceRemap = new();

    public virtual void Simdify(ILCursor c) {
        // Replace method calls from the old type to the new type.
        foreach (var instr in c.Instrs) {
            if (instr.OpCode != OpCodes.Call) {
                continue;
            }

            if (instr.Operand is not MethodReference methodReference) {
                continue;
            }

            foreach (var remap in instanceRemap) {
                if (methodReference.FullName != remap.Key) {
                    continue;
                }

                instr.Operand = c.Body.Method.Module.ImportReference(remap.Value);
            }
        }
    }

    protected void ReplaceCall(string fromSignature, string toName) {
        instanceRemap.Add(fromSignature, GetType().GetMethod(toName, BindingFlags.Static | BindingFlags.NonPublic)!);
    }
}
