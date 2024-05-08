using System;
using System.Threading;

namespace Terraria.ModLoader.Setup;

internal interface ITaskInterface
{
	void SetMaxProgress(int max);
	
	void SetStatus(string status);
	
	void SetProgress(int progress);
	
	CancellationToken CancellationToken { get; }
	
	object Invoke(Delegate action);
}
