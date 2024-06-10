using System;
using System.Collections.Generic;
using System.Linq;

namespace Terraria.ModLoader.Setup.Common;

public readonly record struct ProgressStatusMessageHandle(int Id);

/// <summary>
///		A progress status created and tracked by a
///		<see cref="IProgressManager"/>.
/// </summary>
public sealed class ProgressStatus(int current, int max, IProgressManager manager)
{
	/// <summary>
	///		The current progress for this status.
	/// </summary>
	public int Current
	{
		get => current;

		set
		{
			current = value;
			manager.NotifyProgressChanged();
		}
	}

	/// <summary>
	///		The maximum progress for this status.
	/// </summary>
	public int Max
	{
		get => max;

		set
		{
			max = value;
			manager.NotifyProgressChanged();
		}
	}

	private bool completed;

	/// <summary>
	///		Whether this status is completed.
	/// </summary>
	public bool Completed
	{
		get => completed;

		set
		{
			completed = value;
			manager.NotifyProgressChanged();
		}
	}

	/// <summary>
	///		The status messages.
	/// </summary>
	public IEnumerable<string> Messages => messages.Values;

	private readonly Dictionary<ProgressStatusMessageHandle, string> messages = [];
	private int nextMessageId;

	/// <summary>
	///		Adds a tracked message to this status.
	/// </summary>
	/// <param name="message">The message.</param>
	/// <returns>A handle to the message.</returns>
	public ProgressStatusMessageHandle AddMessage(string message)
	{
		var handle = new ProgressStatusMessageHandle(nextMessageId++);
		messages.Add(handle, message);
		manager.NotifyProgressChanged();
		return handle;
	}

	/// <summary>
	///		Gets the message associated with the given handle.
	/// </summary>
	/// <param name="handle">The message handle.</param>
	/// <returns>The message.</returns>
	public string GetMessage(ProgressStatusMessageHandle handle)
	{
		manager.NotifyProgressChanged();
		return messages[handle];
	}

	/// <summary>
	///		Updates the message associated with the given handle.
	/// </summary>
	/// <param name="handle">The handle.</param>
	/// <param name="message">The message.</param>
	public void SetMessage(ProgressStatusMessageHandle handle, string message)
	{
		manager.NotifyProgressChanged();
		messages[handle] = message;
	}

	/// <summary>
	///		Removes the message associated with the given handle.
	/// </summary>
	/// <param name="handle">The handle.</param>
	public void RemoveMessage(ProgressStatusMessageHandle handle)
	{
		manager.NotifyProgressChanged();
		messages.Remove(handle);
	}
}

/// <summary>
///		Simple wrapper to congregate progress statuses.
/// </summary>
/// <param name="Progress"></param>
public readonly record struct ProgressCounter(in IEnumerable<ProgressStatus> Progress)
{
	public int Current => Progress.Sum(status => status.Current);

	public int Max => Progress.Sum(status => status.Max);
}

/// <summary>
///		Handles displaying and updating tracked progress statuses.
/// </summary>
public interface IProgressManager
{
	/// <summary>
	///		A collection of all statuses.
	/// </summary>
	/// <remarks>
	///		This is a collection of every status created by this manager
	///		regardless of completion state. Useful for displaying all statuses
	///		including those already completed.
	/// </remarks>
	IEnumerable<ProgressStatus> AllStatuses { get; }

	/// <summary>
	///		A collection of completed progress statuses.
	/// </summary>
	IEnumerable<ProgressStatus> CompletedStatuses { get; }

	/// <summary>
	///		A collection of pending progress statuses.
	/// </summary>
	IEnumerable<ProgressStatus> PendingStatuses { get; }

	ProgressCounter AllProgress { get; }

	ProgressCounter CompletedProgress { get; }

	ProgressCounter PendingProgress { get; }

	ProgressStatus CreateStatus(int current, int max);

	/// <summary>
	///		Invoked whenever any progress status is created or updated.
	/// </summary>
	event Action OnProgressChanged;

	void Complete(ProgressStatus status);

	void NotifyProgressChanged();

	/// <summary>
	///		Clears all saved progress.
	/// </summary>
	void ClearProgress();
}

/// <summary>
///		Default <see cref="IProgressManager"/> implementation.
/// </summary>
public sealed class ProgressManager : IProgressManager
{
	IEnumerable<ProgressStatus> IProgressManager.AllStatuses => allStatuses;

	IEnumerable<ProgressStatus> IProgressManager.CompletedStatuses => completedStatuses;

	IEnumerable<ProgressStatus> IProgressManager.PendingStatuses => pendingStatuses;

	ProgressCounter IProgressManager.AllProgress => new(allStatuses);

	ProgressCounter IProgressManager.CompletedProgress => new(completedStatuses);

	ProgressCounter IProgressManager.PendingProgress => new(pendingStatuses);

	// Track individually instead of using LINQ on a single collection.
	// This is to avoid the overhead of filtering the statuses every time.
	private readonly List<ProgressStatus> allStatuses = [];
	private readonly List<ProgressStatus> completedStatuses = [];
	private readonly List<ProgressStatus> pendingStatuses = [];

	public ProgressStatus CreateStatus(int current, int max)
	{
		var status = new ProgressStatus(current, max, this);
		allStatuses.Add(status);
		pendingStatuses.Add(status);
		NotifyProgressChanged();
		return status;
	}

	public event Action? OnProgressChanged;

	public void Complete(ProgressStatus status)
	{
		if (status.Completed)
		{
			return;
		}

		status.Completed = true;
		pendingStatuses.Remove(status);
		completedStatuses.Add(status);

		NotifyProgressChanged();
	}

	public void NotifyProgressChanged()
	{
		OnProgressChanged?.Invoke();
	}

	public void ClearProgress()
	{
		allStatuses.Clear();
		completedStatuses.Clear();
		pendingStatuses.Clear();
		NotifyProgressChanged();
	}
}
