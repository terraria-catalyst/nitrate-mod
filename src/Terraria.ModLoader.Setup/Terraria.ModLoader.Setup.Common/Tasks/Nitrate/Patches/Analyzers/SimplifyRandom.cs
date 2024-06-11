using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class SimplifyRandomAnalyzer(string typeName) : AbstractDiagnosticAnalyzer(RULE)
{
	public const string ID = "SimplifyRandom";

	public static readonly DiagnosticDescriptor RULE = new(ID, "", "", "", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: "");

	protected override void InitializeWorker(AnalysisContext context)
	{
		context.RegisterCompilationStartAction(
			ctx =>
			{
				var randomTypeSymbol = ctx.Compilation.GetTypeByMetadataName(typeName);
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
					return;
				}

				ctx.RegisterOperationAction(
					opCtx =>
					{
						if (opCtx.Operation is not IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals, } operation)
						{
							return;
						}

						var leftMethodSymbol = (operation.LeftOperand as IInvocationOperation)?.TargetMethod;
						var rightMethodSymbol = (operation.RightOperand as IInvocationOperation)?.TargetMethod;

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
					},
					OperationKind.Binary
				);
			}
		);
	}
}

internal sealed class SimplifyRandomCodeFixProvider() : AbstractCodeFixProvider(SimplifyRandomAnalyzer.ID)
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
}
