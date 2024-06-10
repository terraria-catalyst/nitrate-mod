using System.Collections.Generic;

using DiffPatch;

namespace Terraria.ModLoader.Setup.Common;

/// <summary>
///		Patch Reviewer extension to <see cref="ITaskInterface"/>.
/// </summary>
public interface IPatchReviewerInterface : ITaskInterface
{
	/// <summary>
	///		Shows a patch reviewer window for a collection of patch results.
	/// </summary>
	/// <param name="reviewResults">The results to review.</param>
	/// <param name="baseDir">The base directory of the patches.</param>
	void ShowReviewWindow(IEnumerable<FilePatcher> reviewResults, string baseDir);
}

partial class TaskInterfaceExtensions
{
	/// <summary>
	///		Invokes <see cref="IPatchReviewerInterface.ShowReviewWindow"/> if
	///		this <paramref name="taskInterface"/> implements
	///		<see cref="IPatchReviewerInterface"/>; otherwise, does nothing.
	/// </summary>
	/// <param name="taskInterface">The task interface.</param>
	/// <param name="reviewResults">The results to review.</param>
	/// <param name="baseDir">The base directory of the patches.</param>
	public static void ShowReviewWindow(this ITaskInterface taskInterface, IEnumerable<FilePatcher> reviewResults, string baseDir)
	{
		if (taskInterface is not IPatchReviewerInterface patchReviewerInterface)
		{
			return;
		}

		patchReviewerInterface.ShowReviewWindow(reviewResults, baseDir);
	}
}
