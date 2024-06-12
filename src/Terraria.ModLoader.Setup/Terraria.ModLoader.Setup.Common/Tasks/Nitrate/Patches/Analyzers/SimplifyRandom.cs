using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches.Analyzers;

internal sealed class SimplifyRandomAnalyzer(string typeName) : AbstractAnalyzer
{
	protected override Document? ProcessDocumentWithContext(Compilation compilation, Document document, SyntaxTree syntaxTree, SyntaxNode root, SemanticModel semanticModel)
	{
		var randomType = compilation.GetTypeByMetadataName(typeName);
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
		var binaryOperations = binaryExpressions.Select(x => semanticModel.GetOperation(x)).ToArray();

		var generator = SyntaxGenerator.GetGenerator(document.Project);

		for (var i = 0; i < binaryExpressions.Length; i++)
		{
			var expression = binaryExpressions[i];
			var operation = binaryOperations[i];

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

			SyntaxNode newOperation;

			newOperation = isZeroLiteralCheck
				? generator.InvocationExpression(newMemberAccessExpression, oldInvocationExpression.ArgumentList.Arguments[0])
				: generator.InvocationExpression(newMemberAccessExpression, oldInvocationExpression.ArgumentList.Arguments[0], otherExpression);

			if (isNegated)
			{
				newOperation = generator.LogicalNotExpression(newOperation);
			}

			return document.WithSyntaxRoot(root.ReplaceNode(expression, newOperation));
		}

		return null;
	}
}
