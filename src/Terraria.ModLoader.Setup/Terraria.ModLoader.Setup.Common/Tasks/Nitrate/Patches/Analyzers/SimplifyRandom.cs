using System;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches.Analyzers;

internal sealed class SimplifyRandomAnalyzer(string typeName) : AbstractAnalyzer
{
	public override bool ProcessDocument(Compilation compilation, Document document)
	{
		var modified = false;
		var root = document.GetSyntaxRootAsync().Result;
		if (root is null)
		{
			throw new Exception();
		}

		// get all binary expressions
		var binaryExpressions = root.DescendantNodes().OfType<BinaryExpressionSyntax>();
		foreach (var binaryExpression in binaryExpressions)
		{
			if (binaryExpression is not BinaryExpressionSyntax expression)
			{
				continue;
			}

			var randomTypeSymbol = compilation.GetTypeByMetadataName(typeName);
			var nextMethodSymbol = randomTypeSymbol?.GetMembers("Next").FirstOrDefault(
				x =>
				{
					if (x is not IMethodSymbol methodSymbol)
					{
						return false;
					}

					return methodSymbol is { Parameters: [{ Type.SpecialType: SpecialType.System_Int32, },], };
				}
			);

			if (nextMethodSymbol is null)
			{
				continue;
			}

			// TODO: Is this really how I have to do it?
			if (expression.OperatorToken.Text != "==" && expression.OperatorToken.Text != "!=")
			{
				continue;
			}

			var leftMethodSymbol = (expression.Left as InvocationExpressionSyntax)?.Expression as MemberAccessExpressionSyntax;
			var rightMethodSymbol = (expression.Right as InvocationExpressionSyntax)?.Expression as MemberAccessExpressionSyntax;

			if (leftMethodSymbol is null && rightMethodSymbol is null)
			{
				continue;
			}

			if ((leftMethodSymbol is null || leftMethodSymbol.Name.Identifier.Text != "Next") && (rightMethodSymbol is null || rightMethodSymbol.Name.Identifier.Text != "Next"))
			{
				continue;
			}

			var isNegated = expression.OperatorToken.Text == "!=";

			/*
						if (!SymbolEqualityComparer.Default.Equals(leftMethodSymbol, nextMethodSymbol) && !SymbolEqualityComparer.Default.Equals(rightMethodSymbol, nextMethodSymbol))
						{
							return;
						}

						var isLeft = SymbolEqualityComparer.Default.Equals(leftMethodSymbol, nextMethodSymbol);
						var isNegated = operation.OperatorKind is BinaryOperatorKind.NotEquals;

						// var nextMethodOperand = isLeft ? operation.LeftOperand : operation.RightOperand;
						// var expressionOperand = isLeft ? operation.RightOperand : operation.LeftOperand;

						var properties = new Dictionary<string, string?>();
						{
							if (isLeft)
							{
								properties.Add("IsLeft", null);
							}

							if (isNegated)
							{
								properties.Add("IsNegated", null);
							}
						}

						opCtx.ReportDiagnostic(
							Diagnostic.Create(
								RULE,
								operation.Syntax.GetLocation(),
								ImmutableDictionary.CreateRange(properties)
							)
						);
			 */
		}

		return true;
	}
}

/*internal sealed class SimplifyRandomCodeFixProvider() : AbstractCodeFixProvider(SimplifyRandomAnalyzer.ID)
{
	protected override Task RegisterAsync(CodeFixContext context, Parameters parameters)
	{
		var spanStart = parameters.DiagnosticSpan.Start;

		var operation = parameters.Root.FindToken(spanStart).Parent!.FirstAncestorOrSelf<BinaryExpressionSyntax>();

		var isLeft = parameters.Diagnostic.Properties.ContainsKey("IsLeft");
		var isNegated = parameters.Diagnostic.Properties.ContainsKey("IsNegated");

		context.RegisterCodeFix(
			CodeAction.Create(
				"",
				x => SimplifyAsync(context.Document, operation!, isLeft, isNegated, x)
			),
			parameters.Diagnostic
		);

		return Task.CompletedTask;
	}

	private static async Task<Document> SimplifyAsync(Document document, BinaryExpressionSyntax operation, bool isLeft, bool isNegation, CancellationToken cancellationToken)
	{
		var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
		var generator = SyntaxGenerator.GetGenerator(document.Project);

		var oldInvocationExpression = (isLeft ? operation.Left : operation.Right) as InvocationExpressionSyntax;
		if (oldInvocationExpression?.Expression is not MemberAccessExpressionSyntax oldMemberAccessExpression)
		{
			return document;
		}

		var newMemberAccessExpression = oldMemberAccessExpression.WithName(SyntaxFactory.IdentifierName("NextBool"));

		var newOperation = generator.InvocationExpression(newMemberAccessExpression, oldInvocationExpression.ArgumentList.Arguments[0]);
		if (isNegation)
		{
			newOperation = generator.LogicalNotExpression(newOperation);
		}

		var newRoot = oldRoot!.ReplaceNode(operation, newOperation);
		return document.WithSyntaxRoot(newRoot);
	}
}*/
