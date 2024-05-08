using System;
using System.Threading;

namespace Terraria.ModLoader.Setup;

internal interface ITaskInterface
{
	CancellationToken CancellationToken { get; }

	void SetMaxProgress(int max);
	
	void SetStatus(string status);
	
	void SetProgress(int progress);
	
	object Invoke(Delegate action);
}
