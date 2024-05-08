using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Simplification;

using Terraria.ModLoader.Setup.Common;

namespace Terraria.ModLoader.Setup;

internal sealed class SimplifierTask(ITaskInterface taskInterface) : RoslynTask(taskInterface)
{
	protected override string Status => "Simplifying";
	
	protected override int MaxDegreeOfParallelism => 2;
	
	protected override async Task<Document> Process(Document doc)
	{
		if (await doc.GetSyntaxRootAsync() is not { } root)
		{
			return doc;
		}
		
		root = root.WithAdditionalAnnotations(Simplifier.Annotation);
		return await Simplifier.ReduceAsync(doc.WithSyntaxRoot(root), cancellationToken: TaskInterface.CancellationToken);
	}
}
