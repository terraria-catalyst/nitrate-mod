using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches.Analyzers;

internal sealed class SimplifyLocalPlayer : AbstractAnalyzer
{
	protected override Document? ProcessDocumentWithContext(Document document, SyntaxTree syntaxTree, SyntaxNode root, SemanticModel semanticModel)
	{
		var editor = new SyntaxEditor(root, document.Project.Solution.Workspace.Services);

		var mainTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Terraria.Main");
		if (mainTypeSymbol is null)
		{
			return null;
		}

		var mainPlayerMember = mainTypeSymbol.GetMembers("player").FirstOrDefault();
		var mainMyPlayerMember = mainTypeSymbol.GetMembers("myPlayer").FirstOrDefault();
		if (mainPlayerMember is null || mainMyPlayerMember is null)
		{
			return null;
		}

		var nodesToReplace = root.DescendantNodes().OfType<ElementAccessExpressionSyntax>().Where(
			x =>
			{
				var expressionSymbol = ModelExtensions.GetSymbolInfo(semanticModel, x.Expression).Symbol;
				if (expressionSymbol is null)
				{
					return false;
				}

				if (!SymbolEqualityComparer.Default.Equals(expressionSymbol, mainPlayerMember))
				{
					return false;
				}

				var argumentExpression = x.ArgumentList.Arguments.FirstOrDefault()?.Expression;
				if (argumentExpression is null)
				{
					return false;
				}

				var argumentSymbol = ModelExtensions.GetSymbolInfo(semanticModel, argumentExpression).Symbol;
				return argumentSymbol is not null && SymbolEqualityComparer.Default.Equals(argumentSymbol, mainMyPlayerMember);
			}
		).ToList();

		foreach (var node in nodesToReplace)
		{
			var newExpression = SyntaxFactory.ParseExpression("Main.LocalPlayer").WithTriviaFrom(node);
			editor.ReplaceNode(node, newExpression);
		}

		return nodesToReplace.Count > 0 ? document.WithSyntaxRoot(editor.GetChangedRoot()) : null;
	}
}
