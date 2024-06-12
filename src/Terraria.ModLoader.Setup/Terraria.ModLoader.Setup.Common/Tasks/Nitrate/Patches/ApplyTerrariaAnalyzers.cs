using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches.Analyzers;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches;

/// <summary>
///		Applies advanced Terraria/Terraria.ModLoader-specific C# code analyzers.
/// </summary>
/// <remarks>
///		Allows for further control over formatting and applying source-code
///		optimizations.
/// </remarks>
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class ApplyTerrariaAnalyzers(CommonContext ctx, string sourceDirectory, string targetDirectory) : SetupOperation(ctx)
{
#region Syntax rewriters
	/// <summary>
	///		Simplifies expressions that access the local client player instance.
	/// </summary>
	/// <remarks>
	///		<c>Main::player[Main::myPlayer]</c> becomes <c>Main::LocalPlayer</c>.
	/// </remarks>
	private sealed class SimplifyLocalPlayerAccess : CSharpSyntaxRewriter
	{
		public override SyntaxNode? VisitElementAccessExpression(ElementAccessExpressionSyntax node)
		{
			var identifier = SyntaxFactory.IdentifierName("LocalPlayer");

			// Main.player[Main.myPlayer] -> Main.LocalPlayer
			if (MatchMainPlayerMainMyPlayer(node))
			{
				var expression = SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					SyntaxFactory.IdentifierName("Main"),
					identifier
				);

				return ApplyTrivia(expression, node);
			}

			// player[myPlayer] -> LocalPlayer
			if (MatchPlayerMyPlayer(node))
			{
				return ApplyTrivia(identifier, node);
			}

			return base.VisitElementAccessExpression(node);
		}

		private static bool MatchMainPlayerMainMyPlayer(ElementAccessExpressionSyntax node)
		{
			if (node is not { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "Main", }, Name.Identifier.Text: "player", }, ArgumentList.Arguments.Count: 1, })
			{
				return false;
			}

			if (node.ArgumentList.Arguments[0].Expression is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "Main", }, Name.Identifier.Text: "myPlayer", })
			{
				return false;
			}

			return true;
		}

		private static bool MatchPlayerMyPlayer(ElementAccessExpressionSyntax node)
		{
			if (node is not { Expression: IdentifierNameSyntax { Identifier.Text: "player", }, ArgumentList.Arguments.Count: 1, })
			{
				return false;
			}

			if (node.ArgumentList.Arguments[0].Expression is not IdentifierNameSyntax { Identifier.Text: "myPlayer", })
			{
				return false;
			}

			return true;
		}
	}

	/// <summary>
	///		Simplifies unified random boolean expressions.
	/// </summary>
	/// <remarks>
	///		<c>if (Main.rand.Next(5) == 0)</c> becomes <c>if (Main.rand.NextBool(5))</c>.
	/// </remarks>
	private sealed class SimplifyUnifiedRandom : CSharpSyntaxRewriter
	{
		// rand.Next(expr1) == 0 -> rand.NextBool(expr1)
		// rand.Next(expr1) == expr2 -> rand.NextBool(expr1, expr2)
		// rand.Next(expr1) != 0 -> !rand.NextBool(expr1)
		// rand.Next(expr1) != expr2 -> !rand.NextBool(expr1, expr2)
		// 0 == rand.Next(expr1) -> rand.NextBool(expr1)
		// expr2 == rand.Next(expr1) -> rand.NextBool(expr1, expr2)
		// 0 != rand.Next(expr1) -> !rand.NextBool(expr1)
		// expr2 != rand.Next(expr1) -> !rand.NextBool(expr1, expr2)

		public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
		{
			// Skip over expressions other than == and !=.
			if (!node.IsKind(SyntaxKind.EqualsExpression) && !node.IsKind(SyntaxKind.NotEqualsExpression))
			{
				return base.VisitBinaryExpression(node);
			}

			var left = node.Left;
			var right = node.Right;

			// Match pattern: rand.Next(expr1) == expr2
			if (IsRandNextInvocation(left))
			{
				var newInvocation = TransformRandNextInvocation((InvocationExpressionSyntax)left, right, IsZeroLiteral(right));
				return ApplyTrivia(node.IsKind(SyntaxKind.EqualsExpression) ? newInvocation : NegateExpression(newInvocation), node);
			}

			// Match pattern: expr2 == rand.Next(expr1)
			if (IsRandNextInvocation(right))
			{
				var newInvocation = TransformRandNextInvocation((InvocationExpressionSyntax)right, left, IsZeroLiteral(left));
				return ApplyTrivia(node.IsKind(SyntaxKind.EqualsExpression) ? newInvocation : NegateExpression(newInvocation), node);
			}

			// Impossible condition?
			return base.VisitBinaryExpression(node);
		}

		private static bool IsRandNextInvocation(ExpressionSyntax expression)
		{
			if (expression is not InvocationExpressionSyntax invocation)
			{
				return false;
			}

			if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
			{
				return false;
			}

			return memberAccess.Name.Identifier.Text == "Next";
		}

		private static bool IsZeroLiteral(ExpressionSyntax expression)
		{
			return expression.IsKind(SyntaxKind.NumericLiteralExpression) && expression is LiteralExpressionSyntax { Token.ValueText: "0", } ;
		}

		private static InvocationExpressionSyntax TransformRandNextInvocation(InvocationExpressionSyntax invocation, ExpressionSyntax comparison, bool isZeroLiteralComparison)
		{
			if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
			{
				throw new SyntaxErrorException("Invalid invocation expression.");
			}

			var randExpression = memberAccess.Expression;
			var argumentList = invocation.ArgumentList.Arguments;
			var newArguments = isZeroLiteralComparison
				// rand.Next(expr1) == 0 -> rand.NextBool(expr1)
				? SyntaxFactory.SingletonSeparatedList(argumentList[0])
				// rand.Next(expr1) == expr2 -> rand.NextBool(expr1, expr2)
				: SyntaxFactory.SeparatedList(new[] { argumentList[0], SyntaxFactory.Argument(comparison), });

			var newInvocation = SyntaxFactory.InvocationExpression(
				SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					randExpression,
					SyntaxFactory.IdentifierName("NextBool")
				),
				SyntaxFactory.ArgumentList(newArguments)
			);

			return newInvocation;
		}

		private static PrefixUnaryExpressionSyntax NegateExpression(ExpressionSyntax expression)
		{
			return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, expression);
		}
	}
#endregion

	public override void Run()
	{
		var items = new List<WorkItem>();

		foreach (var (file, relPath) in EnumerateFiles(sourceDirectory))
		{
			var destination = Path.Combine(targetDirectory, relPath);

			// If not a C# source file, just copy and move on.
			if (!relPath.EndsWith(".cs"))
			{
				copy(file, relPath, destination);
				continue;
			}

			items.Add(
				new WorkItem(
					"Rewriting (syntax): " + relPath,
					() =>
					{
						CreateParentDirectory(destination);

						if (File.Exists(destination))
						{
							File.SetAttributes(destination, FileAttributes.Normal);
						}

						File.WriteAllText(destination, Rewrite(File.ReadAllText(file), Context.TaskInterface.CancellationToken));
					}
				)
			);
		}

		ExecuteParallel(items);

		using var workspace = Context.CreateWorkspace();

		// Temporary for debugging.
		workspace.WorkspaceFailed += (__, e) =>
		{
			_ = e.Diagnostic.Message;
		};

		// TODO: In this context, most references should be auto-resolved.
		var status = Context.Progress.CreateStatus(0, 2);
		status.AddMessage("Opening ReLogic.csproj...");
		var relogicProject = workspace.OpenProjectAsync(Path.Combine(targetDirectory, "ReLogic", "ReLogic.csproj")).GetAwaiter().GetResult();
		status.AddMessage("Opened ReLogic.csproj!");
		status.Current++;
		status.AddMessage("Opening Terraria.csproj...");
		var terrariaProject = workspace.OpenProjectAsync(Path.Combine(targetDirectory, "Terraria", "Terraria.csproj")).GetAwaiter().GetResult();
		status.AddMessage("Opened Terraria.csproj!");
		status.Current++;

		var analyzers = new List<DiagnosticAnalyzer>
		{
			new SimplifyRandomAnalyzer("Terraria.Utilities.UnifiedRandom"),
		};

		var codeFixProviders = new List<CodeFixProvider>
		{
			new SimplifyRandomCodeFixProvider(),
		};

		AnalyzeAndFixProjectsAsync(analyzers, codeFixProviders, relogicProject, terrariaProject).GetAwaiter().GetResult();

		return;

		void copy(string file, string relPath, string destination)
		{
			items.Add(new WorkItem("Copying: " + relPath, () => Copy(file, destination)));
		}
	}

	private static string Rewrite(string source, CancellationToken cancellationToken)
	{
		var tree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
		var node = tree.GetRoot(cancellationToken);
		node = new SimplifyLocalPlayerAccess().Visit(node);
		// node = new SimplifyUnifiedRandom().Visit(node);

		return node.ToFullString();
	}

	private async Task AnalyzeAndFixProjectsAsync(IEnumerable<DiagnosticAnalyzer> analyzers, IEnumerable<CodeFixProvider> codeFixProviders, params Project[] projects)
	{
		analyzers = analyzers.ToArray();
		codeFixProviders = codeFixProviders.ToArray();

		foreach (var project in projects)
		{
			var modifiedDocuments = await AnalyzeAndFixProjectAsync(project, (DiagnosticAnalyzer[])analyzers, (CodeFixProvider[])codeFixProviders, Context.TaskInterface.CancellationToken);
			foreach (var document in modifiedDocuments)
			{
				await File.WriteAllTextAsync(document.FilePath!, (await document.GetTextAsync(Context.TaskInterface.CancellationToken)).ToString(), Context.TaskInterface.CancellationToken);
			}
		}
	}

	private async Task<List<Document>> AnalyzeAndFixProjectAsync(Project project, DiagnosticAnalyzer[] analyzers, CodeFixProvider[] codeFixProviders, CancellationToken cancellationToken)
	{
		var modifiedDocuments = new ConcurrentDictionary<string, Document>();

		var status = Context.Progress.CreateStatus(0, 2);
		status.AddMessage($"Getting compilation for project \"{project.Name}\"...");
		var compilation = await project.GetCompilationAsync(cancellationToken);
		status.AddMessage("Got compilation!");
		status.Current++;
		if (compilation is null)
		{
			throw new DataException("Failed to get compilation for project.");
		}

		var compilationWithAnalyzers = compilation.WithAnalyzers([..analyzers,], cancellationToken: cancellationToken);

		status.AddMessage($"Getting analyzer diagnostics for project \"{project.Name}\"...");
		var analyzerDiagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken);
		status.AddMessage("Got analyzer diagnostics!");
		status.Current++;

		var diagnosticsPerFile = analyzerDiagnostics.GroupBy(d => d.Location.SourceTree!.FilePath).ToDictionary(g => g.Key, g => g.ToList());
		var items = new List<WorkItem>();

		foreach (var (fileName, diagnostics) in diagnosticsPerFile)
		{
			items.Add(
				new WorkItem(
					$"Applying fixes to {fileName[Path.GetDirectoryName(project.FilePath!)!.Length..]}...",
					() =>
					{
						foreach (var diagnostic in diagnostics)
						{
							foreach (var codeFixProvider in codeFixProviders)
							{
								var document = project.GetDocument(diagnostic.Location.SourceTree)!;
								var fixedDocument = ApplyCodeFixAsync(document, diagnostic, codeFixProvider, cancellationToken).GetAwaiter().GetResult();

								project = fixedDocument.Project;
								modifiedDocuments[fixedDocument.FilePath!] = fixedDocument;
							}
						}
					}
				)
			);
		}

		ExecuteParallel(items, 1);

		/*
		foreach (var documentId in project.DocumentIds)
		{
			var document = project.GetDocument(documentId);
			var formattedDocument = await Formatter.FormatAsync(document);
			project = formattedDocument.Project;
		}
		 */

		return modifiedDocuments.Values.ToList();
	}

	private static async Task<Document> ApplyCodeFixAsync(Document document, Diagnostic diagnostic, CodeFixProvider codeFixProvider, CancellationToken cancellationToken)
	{
		var actions = new List<CodeAction>();
		var context = new CodeFixContext(document, diagnostic, (a, _) => actions.Add(a), cancellationToken);

		await codeFixProvider.RegisterCodeFixesAsync(context);

		if (actions.Count > 0)
		{
			var action = actions.First();
			var operations = await action.GetOperationsAsync(cancellationToken);

			foreach (var operation in operations)
			{
				operation.Apply(document.Project.Solution.Workspace, cancellationToken);
			}
		}

		return document.Project.GetDocument(document.Id)!;
	}

	private static SyntaxNode ApplyTrivia(SyntaxNode node, SyntaxNode original)
	{
		if (original.HasLeadingTrivia)
		{
			node = node.WithLeadingTrivia(original.GetLeadingTrivia());
		}

		if (original.HasTrailingTrivia)
		{
			node = node.WithTrailingTrivia(original.GetTrailingTrivia());
		}

		return node;
	}
}
