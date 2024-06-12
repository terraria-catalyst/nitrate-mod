using Microsoft.CodeAnalysis;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches.Analyzers;

/// <summary>
///		An object that analyzers a document in a compilation unit and provides
///		code fixes if necessary.
/// </summary>
internal abstract class AbstractAnalyzer
{
	public virtual Document? ProcessDocument(Compilation compilation, Document document)
	{
		var syntaxTree = document.GetSyntaxTreeAsync().Result!;
		var root = syntaxTree.GetRoot();
		var semanticModel = compilation.GetSemanticModel(syntaxTree);

		return ProcessDocumentWithContext(compilation, document, syntaxTree, root, semanticModel);
	}

	protected virtual Document? ProcessDocumentWithContext(Compilation compilation, Document document, SyntaxTree syntaxTree, SyntaxNode root, SemanticModel semanticModel)
	{
		return null;
	}
}
