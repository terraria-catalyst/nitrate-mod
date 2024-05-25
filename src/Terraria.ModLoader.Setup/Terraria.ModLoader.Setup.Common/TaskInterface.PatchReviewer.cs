using System.Collections.Generic;

using DiffPatch;

namespace Terraria.ModLoader.Setup.Common;

public interface IPatchReviewerInterface : ITaskInterface
{
	void ShowReviewWindow(IEnumerable<FilePatcher> reviewResults, string baseDir);
}

partial class TaskInterfaceExtensions
{
	public static void ShowReviewWindow(this ITaskInterface taskInterface, IEnumerable<FilePatcher> reviewResults, string baseDir)
	{
		if (taskInterface is not IPatchReviewerInterface patchReviewerInterface)
		{
			return;
		}
		
		patchReviewerInterface.ShowReviewWindow(reviewResults, baseDir);
	}
}
