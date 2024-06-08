using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terraria.ModLoader.Setup.Common.Tasks;

public sealed class NitrateTask : CompositeTask
{
	private sealed class OrganizeExistingPartialClassesTask(CommonContext ctx, string sourceDirectory, string targetDirectory) : SetupOperation(ctx)
	{
		public override void Run()
		{
			// We want to match cases like:
			// - Main.TML.cs
			// - Tile.TML.VanillaRemapping.cs
			// - TileData.Default.cs
			// - WorldFileData.tML.cs (note casing)
			// And ignore cases like:
			// - Logging.ExceptionHandling.cs (in ModLoader directory)
			// Notable exceptions (also ignore):
			// - TileData.Default.cs - TileData new to TML.
			// - Recipe.Extensions.cs - Recipe rewritten by TML.
			
			var items = new List<WorkItem>();
			
			foreach (var (file, relPath) in EnumerateFiles(sourceDirectory))
			{
				var destination = Path.Combine(targetDirectory, relPath);
				
				if (relPath.Contains("ModLoader"))
				{
					copy(file, relPath, destination);
					continue;
				}
				
				var parts = relPath.Split('.');
				if (parts.Length < 3)
				{
					copy(file, relPath, destination);
					continue;
				}
				
				var name = Path.GetFileName(parts[0]);
				var type = parts[1];
				var ext = parts[^1];
				
				// For now, let's only do this for C# source files.
				if (ext != "cs")
				{
					copy(file, relPath, destination);
					continue;
				}
				
				// If the name is Name.Type.cs and Type isn't TML, we'll assume
				// we should leave this be for now (part of exceptions list).
				if (parts.Length == 3 && !type.Equals("tml", System.StringComparison.InvariantCultureIgnoreCase))
				{
					copy(file, relPath, destination);
					continue;
				}
				
				// Now that we've determined this file is a C# source file and
				// explicitly specifies it's a TML particle definition, we can
				// move it to a _TML subdirectory.
				// For clarity's sake, we should preserve any extra context:
				// e.g. Main.TML.Something.cs -> _TML/Main.Something.cs
				destination = Path.Combine(targetDirectory, Path.GetDirectoryName(relPath)!, "_" + type.ToUpperInvariant(), string.Join('.', new[] { name, }.Concat(parts.Where((_, i) => i > 1))));
				copy(file, relPath, destination);
			}
			
			ExecuteParallel(items);
			
			return;
			
			void copy(string file, string relPath, string destination)
			{
				items.Add(new WorkItem("Copying: " + relPath, () => Copy(file, destination)));
			}
		}
	}
	
	private sealed class MakeEveryTypePartialTask(CommonContext ctx, string sourceDirectory, string targetDirectory) : SetupOperation(ctx)
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
							
							File.WriteAllText(destination, Partialize(File.ReadAllText(file), ctx.TaskInterface.CancellationToken));
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
	
	private sealed class FormatWithDotnetFormatAndEditorConfigTask(CommonContext ctx, string sourceDirectory, string targetDirectory) : SetupOperation(ctx)
	{
		public override void Run() { }
	}
	
	// "src/staging/tModLoader", "src/staging/Nitrate", "patches/Nitrate"
	public NitrateTask(CommonContext ctx, string baseDir, string patchedDir, string patchDir) : base(ctx, GetOperations(ctx, baseDir, patchedDir, patchDir)) { }
	
	public static SetupOperation[] GetOperations(CommonContext ctx, string baseDir, string patchedDir, string patchDir)
	{
		return [new OrganizeExistingPartialClassesTask(ctx, update(ref baseDir, patchedDir + nameof(OrganizeExistingPartialClassesTask)), baseDir), new MakeEveryTypePartialTask(ctx, update(ref baseDir, patchedDir + nameof(MakeEveryTypePartialTask)), baseDir), new FormatWithDotnetFormatAndEditorConfigTask(ctx, update(ref baseDir, patchedDir + nameof(FormatWithDotnetFormatAndEditorConfigTask)), baseDir), new PatchTask(ctx, baseDir, patchedDir, patchDir),];
		
		T update<T>(ref T value, T newValue)
		{
			var old = value;
			value = newValue;
			return old;
		}
	}
}
