using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Mono.Cecil;
using Mono.Cecil.Cil;

using MonoMod;
using MonoMod.Utils;

namespace Terraria.ModLoader.Setup.Utilities;

public class HookGenerator
{
	private static readonly Dictionary<Type, string> reflectionTypeNameMap = new()
	{
		{ typeof(string), "string" },
		{ typeof(object), "object" },
		{ typeof(bool), "bool" },
		{ typeof(byte), "byte" },
		{ typeof(char), "char" },
		{ typeof(decimal), "decimal" },
		{ typeof(double), "double" },
		{ typeof(short), "short" },
		{ typeof(int), "int" },
		{ typeof(long), "long" },
		{ typeof(sbyte), "sbyte" },
		{ typeof(float), "float" },
		{ typeof(ushort), "ushort" },
		{ typeof(uint), "uint" },
		{ typeof(ulong), "ulong" },
		{ typeof(void), "void" },
	};
	
	private static readonly Dictionary<string, string> typeNameMap = new();
	
	static HookGenerator()
	{
		foreach (var pair in reflectionTypeNameMap)
		{
			if (pair.Key.FullName is null)
			{
				throw new InvalidOperationException();
			}
			
			typeNameMap[pair.Key.FullName] = pair.Value;
		}
	}
	
	public ModuleDefinition OutputModule { get; }
	
	private readonly MonoModder modder;
	
	private readonly string @namespace;
	private readonly string namespaceIl;
	private readonly bool hookOrig;
	
	public bool HookPrivate { get; set; }
	
	private readonly TypeReference multicastDelegateType;
	private readonly TypeReference asyncResultInterfaceType;
	private readonly TypeReference asyncCallbackType;
	private readonly TypeReference editorBrowsableStateType;
	
	private readonly MethodReference editorBrowsableAttributeCtor;
	
	private readonly MethodReference getMethodFromHandle;
	private readonly MethodReference addMethod;
	private readonly MethodReference removeMethod;
	private readonly MethodReference modifyMethod;
	private readonly MethodReference unmodifyMethod;
	
	private readonly TypeReference ilManipulatorType;
	
	public HookGenerator(MonoModder modder, string name)
	{
		this.modder = modder;
		
		OutputModule = ModuleDefinition.CreateModule(
			name,
			new ModuleParameters
			{
				Architecture = modder.Module.Architecture,
				AssemblyResolver = modder.Module.AssemblyResolver,
				Kind = ModuleKind.Dll,
				Runtime = modder.Module.Runtime,
			}
		);
		
		// Copy all assembly references from the input module.
		// Cecil + .NET Standard libraries + .NET 5.0 = weirdness.
		modder.MapDependencies();
		
		// Removed for tML, better to only add dependencies as needed via the resolver
		// OutputModule.AssemblyReferences.AddRange(modder.Module.AssemblyReferences);
		// modder.DependencyMap[OutputModule] = new List<ModuleDefinition>(modder.DependencyMap[modder.Module]);
		
		@namespace = Environment.GetEnvironmentVariable("MONOMOD_HOOKGEN_NAMESPACE");
		if (string.IsNullOrEmpty(@namespace))
		{
			@namespace = "On";
		}
		
		namespaceIl = Environment.GetEnvironmentVariable("MONOMOD_HOOKGEN_NAMESPACE_IL");
		if (string.IsNullOrEmpty(namespaceIl))
		{
			namespaceIl = "IL";
		}
		
		hookOrig = Environment.GetEnvironmentVariable("MONOMOD_HOOKGEN_ORIG") == "1";
		HookPrivate = Environment.GetEnvironmentVariable("MONOMOD_HOOKGEN_PRIVATE") == "1";
		
		modder.MapDependency(modder.Module, "MonoMod.RuntimeDetour");
		if (!modder.DependencyCache.TryGetValue("MonoMod.RuntimeDetour", out var moduleRuntimeDetour))
		{
			throw new FileNotFoundException("MonoMod.RuntimeDetour not found!");
		}
		
		modder.MapDependency(modder.Module, "MonoMod.Utils");
		if (!modder.DependencyCache.TryGetValue("MonoMod.Utils", out var moduleUtils))
		{
			throw new FileNotFoundException("MonoMod.Utils not found!");
		}
		
		multicastDelegateType = OutputModule.ImportReference(modder.FindType("System.MulticastDelegate"));
		asyncResultInterfaceType = OutputModule.ImportReference(modder.FindType("System.IAsyncResult"));
		asyncCallbackType = OutputModule.ImportReference(modder.FindType("System.AsyncCallback"));
		var methodBaseType = OutputModule.ImportReference(modder.FindType("System.Reflection.MethodBase"));
		var runtimeMethodHandleType = OutputModule.ImportReference(modder.FindType("System.RuntimeMethodHandle"));
		editorBrowsableStateType = OutputModule.ImportReference(modder.FindType("System.ComponentModel.EditorBrowsableState"));
		
		var hookEndpointManagerType = moduleRuntimeDetour.GetType("MonoMod.RuntimeDetour.HookGen.HookEndpointManager");
		
		ilManipulatorType = OutputModule.ImportReference(
			moduleUtils.GetType("MonoMod.Cil.ILContext/Manipulator")
		);
		
		OutputModule.ImportReference(modder.FindType("System.ObsoleteAttribute").Resolve().FindMethod("System.Void .ctor(System.String,System.Boolean)"));
		editorBrowsableAttributeCtor = OutputModule.ImportReference(modder.FindType("System.ComponentModel.EditorBrowsableAttribute").Resolve().FindMethod("System.Void .ctor(System.ComponentModel.EditorBrowsableState)"));
		
		getMethodFromHandle = OutputModule.ImportReference(
			new MethodReference("GetMethodFromHandle", methodBaseType, methodBaseType)
			{
				Parameters =
				{
					new ParameterDefinition(runtimeMethodHandleType),
				},
			}
		);
		
		addMethod = OutputModule.ImportReference(hookEndpointManagerType.FindMethod("Add"));
		removeMethod = OutputModule.ImportReference(hookEndpointManagerType.FindMethod("Remove"));
		modifyMethod = OutputModule.ImportReference(hookEndpointManagerType.FindMethod("Modify"));
		unmodifyMethod = OutputModule.ImportReference(hookEndpointManagerType.FindMethod("Unmodify"));
	}
	
	public void Generate()
	{
		foreach (var type in modder.Module.Types)
		{
			GenerateFor(type, out var hookType, out var hookIlType);
			if (hookType == null || hookIlType == null || hookType.IsNested)
			{
				continue;
			}
			
			OutputModule.Types.Add(hookType);
			OutputModule.Types.Add(hookIlType);
		}
	}
	
	public void GenerateFor(TypeDefinition type, out TypeDefinition hookType, out TypeDefinition hookIlType)
	{
		hookType = hookIlType = null;
		
		if (type.HasGenericParameters || type.IsRuntimeSpecialName || type.Name.StartsWith("<", StringComparison.Ordinal))
		{
			return;
		}
		
		if (!HookPrivate && type.IsNotPublic)
		{
			return;
		}
		
		modder.LogVerbose($"[HookGen] Generating for type {type.FullName}");
		
		hookType = new TypeDefinition(
			type.IsNested ? null : (@namespace + (string.IsNullOrEmpty(type.Namespace) ? "" : ("." + type.Namespace))),
			type.Name,
			(type.IsNested ? TypeAttributes.NestedPublic : TypeAttributes.Public) | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class,
			OutputModule.TypeSystem.Object
		);
		
		hookIlType = new TypeDefinition(
			type.IsNested ? null : (namespaceIl + (string.IsNullOrEmpty(type.Namespace) ? "" : ("." + type.Namespace))),
			type.Name,
			(type.IsNested ? TypeAttributes.NestedPublic : TypeAttributes.Public) | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class,
			OutputModule.TypeSystem.Object
		);
		
		var add = false;
		
		foreach (var method in type.Methods)
		{
			add |= GenerateFor(hookType, hookIlType, method);
		}
		
		foreach (var nested in type.NestedTypes)
		{
			GenerateFor(nested, out var hookNestedType, out var hookNestedIlType);
			if (hookNestedType == null || hookNestedIlType == null)
			{
				continue;
			}
			
			add = true;
			hookType.NestedTypes.Add(hookNestedType);
			hookIlType.NestedTypes.Add(hookNestedIlType);
		}
		
		if (!add)
		{
			hookType = hookIlType = null;
		}
	}
	
	public bool GenerateFor(TypeDefinition hookType, TypeDefinition hookIlType, MethodDefinition method)
	{
		if (method.HasGenericParameters || method.IsAbstract || (method.IsSpecialName && !method.IsConstructor))
		{
			return false;
		}
		
		if (!hookOrig && method.Name.StartsWith("orig_", StringComparison.Ordinal))
		{
			return false;
		}
		
		if (!HookPrivate && method.IsPrivate)
		{
			return false;
		}
		
		var name = GetFriendlyName(method);
		var suffix = method.Parameters.Count != 0;
		
		List<MethodDefinition> overloads = null;
		if (suffix)
		{
			overloads = method.DeclaringType.Methods.Where(other => !other.HasGenericParameters && GetFriendlyName(other) == name && other != method).ToList();
			if (overloads.Count == 0)
			{
				suffix = false;
			}
		}
		
		if (suffix)
		{
			var builder = new StringBuilder();
			for (var i = 0; i < method.Parameters.Count; i++)
			{
				var param = method.Parameters[i];
				if (!typeNameMap.TryGetValue(param.ParameterType.FullName, out var typeName))
				{
					typeName = GetFriendlyName(param.ParameterType, false);
				}
				
				if (overloads.Any(
						other =>
						{
							var otherParam = other.Parameters.ElementAtOrDefault(i);
							return
								otherParam != null && GetFriendlyName(otherParam.ParameterType, false) == typeName && otherParam.ParameterType.Namespace != param.ParameterType.Namespace;
						}
					))
				{
					typeName = GetFriendlyName(param.ParameterType, true);
				}
				
				builder.Append('_');
				builder.Append(typeName.Replace(".", "", StringComparison.Ordinal).Replace("`", "", StringComparison.Ordinal));
			}
			
			name += builder.ToString();
		}
		
		if (hookType.FindEvent(name) != null)
		{
			string nameTmp;
			for (
				var i = 1;
				hookType.FindEvent(nameTmp = name + "_" + i) != null;
				i++
			) { }
			
			name = nameTmp;
		}
		
		// TODO: Fix possible conflict when other members with the same names exist.
		
		var delOrig = GenerateDelegateFor(method);
		delOrig.Name = "orig_" + name;
		delOrig.CustomAttributes.Add(GenerateEditorBrowsable(EditorBrowsableState.Never));
		hookType.NestedTypes.Add(delOrig);
		
		var delHook = GenerateDelegateFor(method);
		delHook.Name = "hook_" + name;
		var delHookInvoke = delHook.FindMethod("Invoke");
		delHookInvoke.Parameters.Insert(0, new ParameterDefinition("orig", ParameterAttributes.None, delOrig));
		var delHookBeginInvoke = delHook.FindMethod("BeginInvoke");
		delHookBeginInvoke.Parameters.Insert(0, new ParameterDefinition("orig", ParameterAttributes.None, delOrig));
		delHook.CustomAttributes.Add(GenerateEditorBrowsable(EditorBrowsableState.Never));
		hookType.NestedTypes.Add(delHook);
		
		var methodRef = OutputModule.ImportReference(method);
		
#region Hook
		var addHook = new MethodDefinition(
			"add_" + name,
			MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
			OutputModule.TypeSystem.Void
		);
		
		addHook.Parameters.Add(new ParameterDefinition(null, ParameterAttributes.None, delHook));
		addHook.Body = new MethodBody(addHook);
		var il = addHook.Body.GetILProcessor();
		il.Emit(OpCodes.Ldtoken, methodRef);
		il.Emit(OpCodes.Call, getMethodFromHandle);
		il.Emit(OpCodes.Ldarg_0);
		var endpointMethod = new GenericInstanceMethod(addMethod);
		endpointMethod.GenericArguments.Add(delHook);
		il.Emit(OpCodes.Call, endpointMethod);
		il.Emit(OpCodes.Ret);
		hookType.Methods.Add(addHook);
		
		var removeHook = new MethodDefinition(
			"remove_" + name,
			MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
			OutputModule.TypeSystem.Void
		);
		
		removeHook.Parameters.Add(new ParameterDefinition(null, ParameterAttributes.None, delHook));
		removeHook.Body = new MethodBody(removeHook);
		il = removeHook.Body.GetILProcessor();
		il.Emit(OpCodes.Ldtoken, methodRef);
		il.Emit(OpCodes.Call, getMethodFromHandle);
		il.Emit(OpCodes.Ldarg_0);
		endpointMethod = new GenericInstanceMethod(removeMethod);
		endpointMethod.GenericArguments.Add(delHook);
		il.Emit(OpCodes.Call, endpointMethod);
		il.Emit(OpCodes.Ret);
		hookType.Methods.Add(removeHook);
		
		var evHook = new EventDefinition(name, EventAttributes.None, delHook)
		{
			AddMethod = addHook,
			RemoveMethod = removeHook,
		};
		
		hookType.Events.Add(evHook);
#endregion
		
#region Hook IL
		var addIl = new MethodDefinition(
			"add_" + name,
			MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
			OutputModule.TypeSystem.Void
		);
		
		addIl.Parameters.Add(new ParameterDefinition(null, ParameterAttributes.None, ilManipulatorType));
		addIl.Body = new MethodBody(addIl);
		il = addIl.Body.GetILProcessor();
		il.Emit(OpCodes.Ldtoken, methodRef);
		il.Emit(OpCodes.Call, getMethodFromHandle);
		il.Emit(OpCodes.Ldarg_0);
		endpointMethod = new GenericInstanceMethod(modifyMethod);
		endpointMethod.GenericArguments.Add(delHook);
		il.Emit(OpCodes.Call, endpointMethod);
		il.Emit(OpCodes.Ret);
		hookIlType.Methods.Add(addIl);
		
		var removeIl = new MethodDefinition(
			"remove_" + name,
			MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
			OutputModule.TypeSystem.Void
		);
		
		removeIl.Parameters.Add(new ParameterDefinition(null, ParameterAttributes.None, ilManipulatorType));
		removeIl.Body = new MethodBody(removeIl);
		il = removeIl.Body.GetILProcessor();
		il.Emit(OpCodes.Ldtoken, methodRef);
		il.Emit(OpCodes.Call, getMethodFromHandle);
		il.Emit(OpCodes.Ldarg_0);
		endpointMethod = new GenericInstanceMethod(unmodifyMethod);
		endpointMethod.GenericArguments.Add(delHook);
		il.Emit(OpCodes.Call, endpointMethod);
		il.Emit(OpCodes.Ret);
		hookIlType.Methods.Add(removeIl);
		
		var evIl = new EventDefinition(name, EventAttributes.None, ilManipulatorType)
		{
			AddMethod = addIl,
			RemoveMethod = removeIl,
		};
		
		hookIlType.Events.Add(evIl);
#endregion
		
		return true;
	}
	
	public TypeDefinition GenerateDelegateFor(MethodDefinition method)
	{
		var name = GetFriendlyName(method);
		var index = method.DeclaringType.Methods.Where(other => !other.HasGenericParameters && GetFriendlyName(other) == name).ToList().IndexOf(method);
		if (index != 0)
		{
			var suffix = index.ToString(CultureInfo.InvariantCulture);
			do
			{
				name = name + "_" + suffix;
			}
			while (method.DeclaringType.Methods.Any(other => !other.HasGenericParameters && GetFriendlyName(other) == (name + suffix)));
		}
		
		name = "d_" + name;
		
		var del = new TypeDefinition(
			null,
			null,
			TypeAttributes.NestedPublic | TypeAttributes.Sealed | TypeAttributes.Class,
			multicastDelegateType
		);
		
		var ctor = new MethodDefinition(
			".ctor",
			MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.ReuseSlot,
			OutputModule.TypeSystem.Void
		)
		{
			ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
			HasThis = true,
		};
		
		ctor.Parameters.Add(new ParameterDefinition(OutputModule.TypeSystem.Object));
		ctor.Parameters.Add(new ParameterDefinition(OutputModule.TypeSystem.IntPtr));
		ctor.Body = new MethodBody(ctor);
		del.Methods.Add(ctor);
		
		var invoke = new MethodDefinition(
			"Invoke",
			MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
			ImportVisible(method.ReturnType)
		)
		{
			ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
			HasThis = true,
		};
		
		if (!method.IsStatic)
		{
			var selfType = ImportVisible(method.DeclaringType);
			if (method.DeclaringType.IsValueType)
			{
				selfType = new ByReferenceType(selfType);
			}
			
			invoke.Parameters.Add(new ParameterDefinition("self", ParameterAttributes.None, selfType));
		}
		
		foreach (var param in method.Parameters)
		{
			invoke.Parameters.Add(
				new ParameterDefinition(
					param.Name,
					param.Attributes & ~ParameterAttributes.Optional & ~ParameterAttributes.HasDefault,
					ImportVisible(param.ParameterType)
				)
			);
		}
		
		invoke.Body = new MethodBody(invoke);
		del.Methods.Add(invoke);
		
		var invokeBegin = new MethodDefinition(
			"BeginInvoke",
			MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
			asyncResultInterfaceType
		)
		{
			ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
			HasThis = true,
		};
		
		foreach (var param in invoke.Parameters)
		{
			invokeBegin.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, param.ParameterType));
		}
		
		invokeBegin.Parameters.Add(new ParameterDefinition("callback", ParameterAttributes.None, asyncCallbackType));
		invokeBegin.Parameters.Add(new ParameterDefinition(null, ParameterAttributes.None, OutputModule.TypeSystem.Object));
		invokeBegin.Body = new MethodBody(invokeBegin);
		del.Methods.Add(invokeBegin);
		
		var invokeEnd = new MethodDefinition(
			"EndInvoke",
			MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
			OutputModule.TypeSystem.Object
		)
		{
			ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
			HasThis = true,
		};
		
		invokeEnd.Parameters.Add(new ParameterDefinition("result", ParameterAttributes.None, asyncResultInterfaceType));
		invokeEnd.Body = new MethodBody(invokeEnd);
		del.Methods.Add(invokeEnd);
		
		return del;
	}
	
	private static string GetFriendlyName(MethodReference method)
	{
		var name = method.Name;
		if (name.StartsWith(".", StringComparison.Ordinal))
		{
			name = name[1..];
		}
		
		name = name.Replace('.', '_');
		return name;
	}
	
	private string GetFriendlyName(TypeReference type, bool full)
	{
		if (type is TypeSpecification)
		{
			var builder = new StringBuilder();
			BuildFriendlyName(builder, type, full);
			return builder.ToString();
		}
		
		return full ? type.FullName : type.Name;
	}
	
	private static void BuildFriendlyName(StringBuilder builder, TypeReference type, bool full)
	{
		if (type is not TypeSpecification specification)
		{
			builder.Append((full ? type.FullName : type.Name).Replace("_", "", StringComparison.Ordinal));
			return;
		}
		
		if (specification.IsByReference)
		{
			builder.Append("ref");
		}
		else if (specification.IsPointer)
		{
			builder.Append("ptr");
		}
		
		BuildFriendlyName(builder, specification.ElementType, full);
		
		if (specification.IsArray)
		{
			builder.Append("Array");
		}
	}
	
	private static bool IsPublic(TypeDefinition typeDef)
	{
		return typeDef != null && (typeDef.IsNestedPublic || typeDef.IsPublic) && !typeDef.IsNotPublic;
	}
	
	private bool HasPublicArgs(GenericInstanceType typeGen)
	{
		foreach (var arg in typeGen.GenericArguments)
		{
			// Generic parameter references are local.
			if (arg.IsGenericParameter)
			{
				return false;
			}
			
			if (arg is GenericInstanceType argGen && !HasPublicArgs(argGen))
			{
				return false;
			}
			
			if (!IsPublic(arg.SafeResolve()))
			{
				return false;
			}
		}
		
		return true;
	}
	
	private TypeReference ImportVisible(TypeReference typeRef)
	{
		// Check if the declaring type is accessible.
		// If not, use its base type instead.
		// Note: This will break down with type specifications!
		var type = typeRef?.SafeResolve();
		goto Try;
		
	Retry:
		typeRef = type.BaseType;
		type = typeRef?.SafeResolve();
		
	Try:
		if (type == null) // Unresolvable - probably private anyway.
		{
			return OutputModule.TypeSystem.Object;
		}
		
		// Generic instance types are special. Try to match them exactly or baseify them.
		if (typeRef is GenericInstanceType typeGen && !HasPublicArgs(typeGen))
		{
			goto Retry;
		}
		
		// Check if the type and all of its parents are public.
		// Generic return / param types are too complicated at the moment and will be simplified.
		for (var parent = type; parent != null; parent = parent.DeclaringType)
		{
			if (IsPublic(parent) && (parent == type || !parent.HasGenericParameters))
			{
				continue;
			}
			// If it isn't public, ...
			
			if (type.IsEnum)
			{
				// ... try the enum's underlying type.
				typeRef = type.FindField("value__").FieldType;
				break;
			}
			
			// ... try the base type.
			goto Retry;
		}
		
		try
		{
			return OutputModule.ImportReference(typeRef);
		}
		catch
		{
			// Under rare circumstances, ImportReference can fail, f.e. Private<K> : Public<K, V>
			return OutputModule.TypeSystem.Object;
		}
	}
	
	private CustomAttribute GenerateEditorBrowsable(EditorBrowsableState state)
	{
		var attrib = new CustomAttribute(editorBrowsableAttributeCtor);
		attrib.ConstructorArguments.Add(new CustomAttributeArgument(editorBrowsableStateType, state));
		return attrib;
	}
}
