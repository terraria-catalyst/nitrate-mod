using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches.Analyzers;

internal sealed class SimplifyRandomAnalyzer(string typeName) : AbstractAnalyzer
{
	protected override Document? ProcessDocumentWithContext(Document document, SyntaxTree syntaxTree, SyntaxNode root, SemanticModel semanticModel)
	{
		var randomType = semanticModel.Compilation.GetTypeByMetadataName(typeName);
		var nextMethod = randomType?.GetMembers("Next").FirstOrDefault(
			x =>
			{
				if (x is not IMethodSymbol methodSymbol)
				{
					return false;
				}

				return methodSymbol is { Parameters: [{ Type.SpecialType: SpecialType.System_Int32, },], };
			}
		);

		if (nextMethod is null)
		{
			return null;
		}

		var binaryExpressions = root.DescendantNodes().OfType<BinaryExpressionSyntax>().ToArray();

		var generator = SyntaxGenerator.GetGenerator(document.Project);

		var replacements = new Dictionary<SyntaxNode, SyntaxNode>();

		foreach (var expression in binaryExpressions)
		{
			// Avoid accidentally defining infinite recursion in implementation
			// methods.
			if (expression.Ancestors().OfType<MethodDeclarationSyntax>().Any(x => x.Identifier.Text == "NextBool"))
			{
				continue;
			}

			var operation = semanticModel.GetOperation(expression);

			if (operation is not IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals, } binaryOperation)
			{
				continue;
			}

			var leftMethodSymbol = (binaryOperation.LeftOperand as IInvocationOperation)?.TargetMethod;
			var rightMethodSymbol = (binaryOperation.RightOperand as IInvocationOperation)?.TargetMethod;

			if (!SymbolEqualityComparer.Default.Equals(leftMethodSymbol, nextMethod) && !SymbolEqualityComparer.Default.Equals(rightMethodSymbol, nextMethod))
			{
				continue;
			}

			var isLeft = SymbolEqualityComparer.Default.Equals(leftMethodSymbol, nextMethod);
			var isNegated = binaryOperation.OperatorKind is BinaryOperatorKind.NotEquals;

			var oldInvocationExpression = (isLeft ? expression.Left : expression.Right) as InvocationExpressionSyntax;
			if (oldInvocationExpression?.Expression is not MemberAccessExpressionSyntax oldMemberAccessExpression)
			{
				continue;
			}

			var otherExpression = isLeft ? expression.Right : expression.Left;
			var isZeroLiteralCheck = otherExpression is LiteralExpressionSyntax { Token.ValueText: "0", };

			var newMemberAccessExpression = oldMemberAccessExpression.WithName(SyntaxFactory.IdentifierName("NextBool"));

			var newOperation = isZeroLiteralCheck
				? generator.InvocationExpression(newMemberAccessExpression, oldInvocationExpression.ArgumentList.Arguments[0])
				: generator.InvocationExpression(newMemberAccessExpression, oldInvocationExpression.ArgumentList.Arguments[0], otherExpression);

			if (isNegated)
			{
				newOperation = generator.LogicalNotExpression(newOperation);
			}

			replacements.Add(expression, newOperation);
		}

		root = root.ReplaceNodes(replacements.Keys, (x, _) => replacements[x]);
		return replacements.Count != 0 ? document.WithSyntaxRoot(root) : null;
	}
}
