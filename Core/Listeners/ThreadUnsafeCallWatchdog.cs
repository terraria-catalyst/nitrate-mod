using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace Nitrate.Core.Listeners;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class ThreadUnsafeCallWatchdog : ModSystem
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private static class Delegator
    {
        public static Action MakeAction(Action action) => action;

        public static Action MakeAction<T1>(Action<T1> action, T1 t1) => () => action(t1);

        public static Action MakeAction<T1, T2>(Action<T1, T2> action, T1 t1, T2 t2) => () => action(t1, t2);

        public static Action MakeAction<T1, T2, T3>(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) => () => action(t1, t2, t3);

        public static Action MakeAction<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 t1, T2 t2, T3 t3, T4 t4) => () => action(t1, t2, t3, t4);

        public static Action MakeAction<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) => () => action(t1, t2, t3, t4, t5);

        public static Action MakeAction<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6) => () => action(t1, t2, t3, t4, t5, t6);
    }

    private static bool Enabled;
    private static readonly ConcurrentBag<Action> actions = new();

    public override void Load()
    {
        base.Load();

        IL_Lighting.AddLight_int_int_float_float_float += QueueEditor(typeof(Lighting).GetMethod(nameof(Lighting.AddLight), new[] { typeof(int), typeof(int), typeof(float), typeof(float), typeof(float) })!);
    }

    public static void Enable()
    {
        Enabled = true;
        actions.Clear();
    }

    public static void Disable()
    {
        Enabled = false;

        foreach (Action action in actions)
        {
            action();
        }

        actions.Clear();
    }

    private static void QueueAction(Action action)
    {
        actions.Add(action);
    }

    private static ILContext.Manipulator QueueEditor(MethodInfo method)
    {
        if (method.ReturnType != typeof(void))
        {
            throw new ArgumentException("ThreadUnsafeCallWatchdog does not currently support postponing the calling of methods with return values.");
        }

        return il =>
        {
            ILCursor c = new(il);
            ILLabel enabledLabel = c.DefineLabel();

            Type[] parameters = method.GetParameters().Select(x => x.ParameterType).ToArray();
            MethodInfo makeAction = typeof(Delegator).GetMethods().Single(x => x.Name == nameof(Delegator.MakeAction) && x.GetParameters().Length == parameters.Length + 1).MakeGenericMethod(parameters);
            ConstructorInfo actionCtor = typeof(Action).GetConstructor(new[] { typeof(object), typeof(IntPtr) })!;

            c.Emit(OpCodes.Ldsfld, typeof(ThreadUnsafeCallWatchdog).GetField(nameof(Enabled), BindingFlags.NonPublic | BindingFlags.Static)!);
            c.Emit(OpCodes.Brfalse, enabledLabel);

            if (!method.IsStatic)
            {
                c.Emit(OpCodes.Ldarg_0);

                if (method.DeclaringType!.IsValueType)
                {
                    c.Emit(OpCodes.Box, method.DeclaringType);
                }
            }
            else
            {
                c.Emit(OpCodes.Ldnull);
            }

            c.Emit(OpCodes.Ldftn, method);
            c.Emit(OpCodes.Newobj, actionCtor);

            for (int i = 0; i < parameters.Length; i++)
            {
                c.Emit(OpCodes.Ldarg, i);
            }

            c.Emit(OpCodes.Call, makeAction);
            c.EmitDelegate<Action<Action>>(QueueAction);

            c.Emit(OpCodes.Ret);

            c.MarkLabel(enabledLabel);
        };
    }
}