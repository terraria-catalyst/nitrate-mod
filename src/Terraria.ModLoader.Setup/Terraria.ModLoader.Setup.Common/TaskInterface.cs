using System;
using System.Threading;

namespace Terraria.ModLoader.Setup.Common;

/// <summary>
///		The main interface object designed to handle task execution.
/// </summary>
public interface ITaskInterface
{
	/// <summary>
	///		The cancellation token used to cancel tasks.
	/// </summary>
	CancellationToken CancellationToken { get; }
	
	/// <summary>
	///		The progress manager instance.
	/// </summary>
	IProgressManager Progress { get; }
	
	/// <summary>
	///		The settings manager instance.
	/// </summary>
	ISettingsManager Settings { get; }
	
	/// <summary>
	///		Invokes the given <paramref name="action"/> on this task interface's
	///		main thread.
	/// </summary>
	/// <param name="action">The action to invoke.</param>
	/// <returns>The result of the action, if any.</returns>
	object? InvokeOnMainThread(Delegate action);
}

public static partial class TaskInterfaceExtensions;
