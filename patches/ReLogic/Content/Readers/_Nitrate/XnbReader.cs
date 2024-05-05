using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReLogic.Content.Readers;

partial class XnbReader
{
	// TODO: Change to private, convert Value field to a property (or remove)?
	[Obsolete("Nitrate: obsolete LoadOnMainThread<T> in favor of explicit method APIs (GetLoadOnMainThread and SetLoadOnMainThread)")]
	public static class LoadOnMainThread<T>
	{
		// ReSharper disable once StaticMemberInGenericType - Intentional.

		/*public static bool Value {
			get => GetLoadOnMainThread<T>();
			set => SetLoadOnMainThread<T>(value);
		}*/

		public static bool Value;
	}

	private static readonly Dictionary<Type, bool> loadOnMainThread = [];

	public static bool GetLoadOnMainThread<T>()
	{
		return GetLoadOnMainThread(typeof(T));
	}

	public static bool GetLoadOnMainThread(Type type)
	{
		return loadOnMainThread.GetValueOrDefault(type, false);
	}

	public static bool SetLoadOnMainThread<T>(bool value)
	{
		return SetLoadOnMainThread(typeof(T), value);
	}

	public static bool SetLoadOnMainThread(Type type, bool value)
	{
		return loadOnMainThread[type] = value;
	}


#pragma warning disable CS0618 // Type or member is obsolete
	private static bool SetValueForGenericType(Type type, bool value)
	{
		Type loadOnMainThreadType = typeof(LoadOnMainThread<>).MakeGenericType(type);
		FieldInfo valueField = loadOnMainThreadType.GetField("Value", BindingFlags.Public | BindingFlags.Static);
		if (valueField is null) {
			throw new InvalidOperationException($"Failed to set load-on-main-thread value of type {type.FullName}, Value field not found?");
		}

		valueField.SetValue(null, value);
		return value;

	}
#pragma warning restore CS0618 // Type or member is obsolete
}
