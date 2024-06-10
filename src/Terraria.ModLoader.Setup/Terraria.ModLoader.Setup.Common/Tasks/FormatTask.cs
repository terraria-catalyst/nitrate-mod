using System;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;

using Terraria.ModLoader.Setup.Common.Tasks.Roslyn.Formatting;

namespace Terraria.ModLoader.Setup.Common.Tasks;

public sealed class FormatTask : SetupOperation
{
	private static readonly AdhocWorkspace workspace = new();

	static FormatTask()
	{
		var optionSet = workspace.CurrentSolution.Options;

		// Essentials
		optionSet = optionSet
			.WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, true);

		// K&R
		optionSet = optionSet
			.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInProperties, false)
			.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInAccessors, false)
			.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, false)
			.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false)
			.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, false)
			.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false)
			.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, false);

		// Fix switch indentation
		optionSet = optionSet
			.WithChangedOption(CSharpFormattingOptions.IndentSwitchCaseSection, true)
			.WithChangedOption(CSharpFormattingOptions.IndentSwitchCaseSectionWhenBlock, false);

		workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(optionSet));
	}

	public FormatTask(CommonContext ctx) : base(ctx) { }

	private static string projectPath; //persist across executions

	public override bool ConfigurationDialog()
	{
		return (bool)Context.TaskInterface.InvokeOnMainThread(
			new Func<bool>(
				() =>
				{
					var dialog = new OpenFileDialogParameters
					{
						FileName = projectPath,
						InitialDirectory = Path.GetDirectoryName(projectPath) ?? Path.GetFullPath("."),
						Filter = "C# Project|*.csproj",
						Title = "Select C# Project",
					};

					var result = Context.TaskInterface.ShowDialogWithOkFallback(ref dialog);
					projectPath = dialog.FileName;
					return result == SetupDialogResult.Ok && File.Exists(projectPath);
				}
			)
		);
	}

	public override void Run()
	{
		var dir = Path.GetDirectoryName(projectPath); //just format all files in the directory
		if (dir is null)
		{
			throw new InvalidOperationException("No parent directory: " + projectPath);
		}

		var workItems = Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
			.Select(path => new FileInfo(path))
			.OrderByDescending(f => f.Length)
			.Select(f => new WorkItem("Formatting: " + f.Name, () => FormatFile(f.FullName, false, Context.TaskInterface.CancellationToken)));

		ExecuteParallel(workItems.ToList());
	}

	public static void FormatFile(string path, bool aggressive, CancellationToken cancellationToken)
	{
		var source = File.ReadAllText(path);
		var formatted = Format(source, cancellationToken, aggressive);
		if (source != formatted)
		{
			File.WriteAllText(path, formatted);
		}
	}

	public static SyntaxNode Format(SyntaxNode node, bool aggressive, CancellationToken cancellationToken)
	{
		if (aggressive)
		{
			node = new NoNewlineBetweenFieldsRewriter().Visit(node);
			node = new RemoveBracesFromSingleStatementRewriter().Visit(node);
		}

		node = new AddVisualNewlinesRewriter().Visit(node);
		node = new FileScopedNamespaceRewriter().Visit(node);
		node = Formatter.Format(node!, workspace, cancellationToken: cancellationToken);
		node = new CollectionInitializerFormatter().Visit(node);
		return node;
	}

	public static string Format(string source, CancellationToken cancellationToken, bool aggressive)
	{
		var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(preprocessorSymbols: new[] { "SERVER", }));
		return Format(tree.GetRoot(), aggressive, cancellationToken).ToFullString();
	}
}
