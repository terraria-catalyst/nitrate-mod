using System.Collections.Generic;
using System.IO;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
			if (MatchMainPlayerMainMyPlayer(node))
			{
				return SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					SyntaxFactory.IdentifierName("Main"),
					SyntaxFactory.IdentifierName("LocalPlayer")
				);
			}

			if (MatchPlayerMyPlayer(node))
			{
				return SyntaxFactory.IdentifierName("LocalPlayer");
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
					"Rewriting: " + relPath,
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

		return node.ToFullString();
	}
}
