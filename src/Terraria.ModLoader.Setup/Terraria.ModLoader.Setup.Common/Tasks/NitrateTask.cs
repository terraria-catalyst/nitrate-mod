using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terraria.ModLoader.Setup.Common.Tasks;

/// <summary>
///		Composite task which applies automated intermediary Nitrate patches
///		before then applying explicitly-defined user patches.
/// </summary>
/// <seealso cref="OrganizePartialClasses"/>
/// <seealso cref="MakeTypesPartial"/>
/// <seealso cref="TreeshakePreprocessors"/>
/// <seealso cref="FormatWithEditorConfig"/>
public sealed class NitrateTask(CommonContext ctx, string baseDir, string patchedDir, string patchDir)
	: CompositeTask(ctx, GetOperations(ctx, baseDir, patchedDir, patchDir))
{
	/// <summary>
	///		Organizes existing partial class files to be within subdirectories.
	/// </summary>
	/// <remarks>
	///		Matches cases like:
	///		<ul>
	///			<li>Main.TML.cs</li>
	///			<li>TileData.Default.cs</li>
	///			<li>WorldFileData.tML.cs (note casing)</li>
	///		</ul>
	///		Ignores cases like:
	///		<ul>
	///			<li>Logging.ExceptionHandling.cs (in ModLoader directory)</li>
	///		</ul>
	///		Notable exceptions (also gets ignored):
	///		<ul>
	///			<li>TileData.Default.cs - TileData new to TML.</li>
	///			<li>Recipe.Extensions.cs - Recipe rewritten by TML.</li>
	///		</ul>
	///		Collected files are moved to a subdirectory within the same
	///		directory, e.g.
	///		<c>Main.TML.Something.cs -> _TML/Main.Something.cs</c> and
	///		<c>Main.TML.cs -> _TML/Main.cs</c>.
	/// </remarks>
	private sealed class OrganizePartialClasses(CommonContext ctx, string sourceDirectory, string targetDirectory) : SetupOperation(ctx)
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
	
	/// <summary>
	///		Rewrites all type definitions in C# source files to be partial.
	/// </summary>
	/// <remarks>
	///		Convenient for development and reduces patches.
	/// </remarks>
	private sealed class MakeTypesPartial(CommonContext ctx, string sourceDirectory, string targetDirectory) : SetupOperation(ctx)
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
	
	/// <summary>
	///		Naively evaluates preprocessor directives using known symbols to cut
	///		down on unnecessary code from older Terraria.ModLoader projects.
	/// </summary>
	/// <remarks>
	///		Defined symbols:
	///		<ul>
	///			<li>FNA</li>
	///			<li>NETCORE</li>
	///		</ul>
	///		Files are interpreted line-by-line to process preprocessor
	///		directives; complex expressions involving parentheses and logical
	///		operators are handled.
	/// </remarks>
	private sealed class TreeshakePreprocessors(CommonContext ctx, string sourceDirectory, string targetDirectory) : SetupOperation(ctx)
	{
		public override void Run()
		{
			var items = new List<WorkItem>();
			
			foreach (var (file, relPath) in EnumerateFiles(sourceDirectory))
			{
				var destination = Path.Combine(targetDirectory, relPath);
				
				if (!relPath.EndsWith(".cs"))
				{
					copy(file, relPath, destination);
					continue;
				}
				
				items.Add(
					new WorkItem(
						"Treeshaking preprocessors: " + relPath,
						() =>
						{
							CreateParentDirectory(destination);
							
							if (File.Exists(destination))
							{
								File.SetAttributes(destination, FileAttributes.Normal);
							}
							
							File.WriteAllLines(destination, Treeshake(File.ReadAllLines(file)));
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
		
		private static string[] Treeshake(string[] lines)
		{
			var symbols = new HashSet<string> { "FNA", "NETCORE", };
			var processedLines = new List<string>();
			
			var include = true;
			foreach (var line in lines)
			{
				var trimmedLine = line.Trim();
				
				if (trimmedLine.StartsWith("#if"))
				{
					var condition = trimmedLine[3..].Trim();
					include = EvaluateCondition(condition, symbols);
				}
				else if (trimmedLine.StartsWith("#elif"))
				{
					var condition = trimmedLine[5..].Trim();
					if (!include)
					{
						include = EvaluateCondition(condition, symbols);
					}
				}
				else if (trimmedLine.StartsWith("#else"))
				{
					include = !include;
				}
				else if (trimmedLine.StartsWith("#endif"))
				{
					include = true;
				}
				else if (include)
				{
					processedLines.Add(line);
				}
			}
			
			return processedLines.ToArray();
		}
		
		private static bool EvaluateCondition(string condition, HashSet<string> symbols)
		{
			condition = condition.Replace(" ", "");
			return EvaluateExpression(condition, symbols);
		}
		
		private static bool EvaluateExpression(string expression, HashSet<string> symbols)
		{
			if (expression.Length == 1)
			{
				return symbols.Contains(expression);
			}
			
			if (expression[0] == '(' && expression[^1] == ')')
			{
				return EvaluateExpression(expression.Substring(1, expression.Length - 2), symbols);
			}
			
			var andIndex = FindLogicalOperatorIndex(expression, "&&");
			if (andIndex != -1)
			{
				var leftExpression = expression[..andIndex];
				var rightExpression = expression[(andIndex + 2)..];
				return EvaluateExpression(leftExpression, symbols) && EvaluateExpression(rightExpression, symbols);
			}
			
			var orIndex = FindLogicalOperatorIndex(expression, "||");
			if (orIndex != -1)
			{
				var leftExpression = expression[..orIndex];
				var rightExpression = expression[(orIndex + 2)..];
				return EvaluateExpression(leftExpression, symbols) || EvaluateExpression(rightExpression, symbols);
			}
			
			return symbols.Contains(expression);
		}
		
		private static int FindLogicalOperatorIndex(string expression, string logicalOperator)
		{
			var parenthesesDepth = 0;
			
			for (var i = 0; i < expression.Length - logicalOperator.Length + 1; i++)
			{
				var curr = expression[i];
				
				switch (curr)
				{
				case '(':
					parenthesesDepth++;
					break;
				
				case ')':
					parenthesesDepth--;
					break;
				
				default:
					if (parenthesesDepth == 0 && expression.Substring(i, logicalOperator.Length) == logicalOperator)
					{
						return i;
					}
					
					break;
				}
			}
			
			return -1;
		}
	}
	
	/// <summary>
	///		Runs <c>dotnet format analyzers</c> and <c>dotnet format style</c>
	///		on all resolved <c>.csproj</c> files using the <c>.editorconfig</c>
	///		file resolved at <c>&lt;cwd&gt;/src/.editorconfig</c>.
	/// </summary>
	/// <remarks>
	///		Additionally, copies the <c>.editorconfig</c> file to the directory of each
	///		<c>.csproj</c> file.
	///		<br />
	///		Also patches existing <c>.csproj</c> files to be restorable (fixes
	///		incorrect paths).
	/// </remarks>
	private sealed class FormatWithEditorConfig(CommonContext ctx, string sourceDirectory, string targetDirectory) : SetupOperation(ctx)
	{
		public override void Run()
		{
			var items = new List<WorkItem>();
			
			foreach (var (file, relPath) in EnumerateFiles(sourceDirectory))
			{
				var destination = Path.Combine(targetDirectory, relPath);
				
				if (!relPath.EndsWith(".csproj"))
				{
					copy(file, relPath, destination);
					continue;
				}
				
				items.Add(
					new WorkItem(
						"Making restorable: " + relPath,
						() =>
						{
							CreateParentDirectory(destination);
							
							if (File.Exists(destination))
							{
								File.SetAttributes(destination, FileAttributes.Normal);
							}
							
							File.WriteAllText(destination, MakeRestorable(File.ReadAllText(file)));
						}
					)
				);
			}
			
			ExecuteParallel(items);
			
			items.Clear();
			
			var editorconfigPath = Path.Combine(Directory.GetCurrentDirectory(), "src", ".editorconfig");
			foreach (var (file, relPath) in EnumerateFiles(targetDirectory))
			{
				if (!relPath.EndsWith(".csproj"))
				{
					continue;
				}
				
				// Copy .editorconfig over to the target directory.
				File.Copy(editorconfigPath, Path.Combine(Path.GetDirectoryName(file)!, ".editorconfig"), true);
				
				// Run dotnet format in directory.
				items.Add(
					new WorkItem(
						"Formatting: " + relPath + " (with dotnet format)",
						() =>
						{
							var analyzers = new ProcessStartInfo("dotnet.exe")
							{
								Arguments = "format analyzers -v diag --binarylog analyzers.binlog",
								UseShellExecute = true,
								WorkingDirectory = Path.GetDirectoryName(file)!,
							};
							
							var style = new ProcessStartInfo("dotnet.exe")
							{
								Arguments = "format style -v diag --binarylog style.binlog",
								UseShellExecute = true,
								WorkingDirectory = Path.GetDirectoryName(file)!,
							};
							
							var analyzersProcess = Process.Start(analyzers);
							analyzersProcess?.WaitForExit();
							var styleProcess = Process.Start(style);
							styleProcess?.WaitForExit();
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
		
		private static string MakeRestorable(string source)
		{
			source = source.Replace("<ProjectReference Include=\"../../../FNA/FNA.Core.csproj\" />", "<ProjectReference Include=\"../../../Terraria.ModLoader/FNA/FNA.Core.csproj\" />");
			source = source.Replace("<Import Project=\"../../../tModBuildTasks/BuildTasks.targets\" />", "<Import Project=\"../../../Terraria.ModLoader/tModBuildTasks/BuildTasks.targets\" />");
			return source.Replace("<ProjectReference Include=\"../../../tModPorter/tModPorter/tModPorter.csproj\" />", "<ProjectReference Include=\"../../../Terraria.ModLoader/tModPorter/tModPorter/tModPorter.csproj\" />");
		}
	}
	
	public static SetupOperation[] GetOperations(CommonContext ctx, string baseDir, string patchedDir, string patchDir)
	{
		return [new OrganizePartialClasses(ctx, baseDir, baseDir = patchedDir + '_' + nameof(OrganizePartialClasses)), new MakeTypesPartial(ctx, baseDir, baseDir = patchedDir + '_' + nameof(MakeTypesPartial)), new TreeshakePreprocessors(ctx, baseDir, baseDir = patchedDir + '_' + nameof(TreeshakePreprocessors)), new FormatWithEditorConfig(ctx, baseDir, baseDir = patchedDir + '_' + nameof(FormatWithEditorConfig)), new PatchTask(ctx, baseDir, patchedDir, patchDir),];
	}
}
