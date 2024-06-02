using System;
using System.Threading;

namespace Terraria.ModLoader.Setup.Common;

public interface ITaskInterface
{
	/// <summary>
	///		The cancellation token used to cancel tasks.
	/// </summary>
	CancellationToken CancellationToken { get; }
	
	/// <summary>
	///		The maximum progress to display.
	/// </summary>
	int MaxProgress { set; }
	
	/// <summary>
	///		The current progress to display.
	/// </summary>
	int Progress { set; }
	
	/// <summary>
	///		Updates the current status.
	/// </summary>
	/// <param name="status">The status line.</param>
	void UpdateStatus(string status);
	
	/// <summary>
	///		Invokes the given <paramref name="action"/> on this task interface's
	///		main thread.
	/// </summary>
	/// <param name="action">The action to invoke.</param>
	/// <returns>The result of the action, if any.</returns>
	object? InvokeOnMainThread(Delegate action);
	
	/// <summary>
	///		Retrieves a settings object.
	/// </summary>
	/// <typeparam name="T">The settings type.</typeparam>
	/// <returns>
	///		The settings instance.
	/// </returns>
	T GetSettings<T>();
	
	/// <summary>
	///		Sets settings objects.
	/// </summary>
	/// <param name="settings">Settings object instance.</param>
	/// <typeparam name="T">Settings type.</typeparam>
	void SetSettings<T>(T settings);
	
	/// <summary>
	///		Loads the settings.
	/// </summary>
	/// <param name="path">The path to load settings from.</param>
	void LoadSettings(string path);
	
	/// <summary>
	///		Saves settings objects.
	/// </summary>
	void SaveSettings();
}

public static partial class TaskInterfaceExtensions;
