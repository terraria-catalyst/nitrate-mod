using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Simplification;

namespace Terraria.ModLoader.Setup.Common.Tasks.Roslyn;

public sealed class SimplifierTask(CommonContext ctx) : RoslynTask(ctx)
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
		return await Simplifier.ReduceAsync(doc.WithSyntaxRoot(root), cancellationToken: Context.TaskInterface.CancellationToken);
	}
}
