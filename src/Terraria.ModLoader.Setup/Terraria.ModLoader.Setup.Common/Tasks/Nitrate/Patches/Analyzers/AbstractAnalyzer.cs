using Microsoft.CodeAnalysis;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches.Analyzers;

/// <summary>
///		An object that analyzers a document in a compilation unit and provides
///		code fixes if necessary.
/// </summary>
internal abstract class AbstractAnalyzer
{
	public virtual Document? ProcessDocument(Document document)
	{
		var syntaxTree = document.GetSyntaxTreeAsync().Result!;
		var root = syntaxTree.GetRoot();
		var semanticModel = document.GetSemanticModelAsync().Result!;

		return ProcessDocumentWithContext(document, syntaxTree, root, semanticModel);
	}

	protected virtual Document? ProcessDocumentWithContext(Document document, SyntaxTree syntaxTree, SyntaxNode root, SemanticModel semanticModel)
	{
		return null;
	}
}
