using System.Collections.Generic;
using System.IO;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches;

/// <summary>
///		Rewrites all type definitions in C# source files to be partial.
/// </summary>
/// <remarks>
///		Convenient for development and reduces patches.
/// </remarks>
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class MakeTypesPartial(CommonContext ctx, string sourceDirectory, string targetDirectory) : SetupOperation(ctx)
{
	private sealed class TypePartialRewriter : CSharpSyntaxRewriter
	{
		public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
		{
			node = MakePartial(node);
			return base.VisitClassDeclaration(node);
		}
		
		public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
		{
			node = MakePartial(node);
			return base.VisitInterfaceDeclaration(node);
		}
		
		public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
		{
			node = MakePartial(node);
			return base.VisitRecordDeclaration(node);
		}
		
		public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
		{
			node = MakePartial(node);
			return base.VisitStructDeclaration(node);
		}
		
		private static ClassDeclarationSyntax MakePartial(ClassDeclarationSyntax node)
		{
			return node.Modifiers.Any(SyntaxKind.PartialKeyword) ? node : node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space));
		}
		
		private static InterfaceDeclarationSyntax MakePartial(InterfaceDeclarationSyntax node)
		{
			return node.Modifiers.Any(SyntaxKind.PartialKeyword) ? node : node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space));
		}
		
		private static RecordDeclarationSyntax MakePartial(RecordDeclarationSyntax node)
		{
			return node.Modifiers.Any(SyntaxKind.PartialKeyword) ? node : node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space));
		}
		
		private static StructDeclarationSyntax MakePartial(StructDeclarationSyntax node)
		{
			return node.Modifiers.Any(SyntaxKind.PartialKeyword) ? node : node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space));
		}
	}
	
	public override void Run()
	{
		// For each given C# source file, we want to make every defined type
		// partial (saves us patches later).
		
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
					"Partializing: " + relPath,
					() =>
					{
						CreateParentDirectory(destination);
						
						if (File.Exists(destination))
						{
							File.SetAttributes(destination, FileAttributes.Normal);
						}
						
						File.WriteAllText(destination, Partialize(File.ReadAllText(file), Context.TaskInterface.CancellationToken));
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
	
	private static string Partialize(string source, CancellationToken cancellationToken)
	{
		var tree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
		var node = tree.GetRoot(cancellationToken);
		node = new TypePartialRewriter().Visit(node);
		
		return node.ToFullString();
	}
}
