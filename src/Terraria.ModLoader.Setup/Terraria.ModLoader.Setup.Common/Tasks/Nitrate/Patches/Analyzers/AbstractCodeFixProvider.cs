﻿using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches.Analyzers;

internal abstract class AbstractCodeFixProvider(string diagnosticId) : CodeFixProvider
{
	protected readonly record struct Parameters(in SyntaxNode Root, in Diagnostic Diagnostic)
	{
		public TextSpan DiagnosticSpan => Diagnostic.Location.SourceSpan;
	}

	public override ImmutableArray<string> FixableDiagnosticIds { get; } = [diagnosticId,];

	public override FixAllProvider GetFixAllProvider()
	{
		return WellKnownFixAllProviders.BatchFixer;
	}

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false)!;
		var diagnostic = context.Diagnostics.First();

		var parameters = new Parameters(root, diagnostic);

		await RegisterAsync(context, parameters);
	}

	protected abstract Task RegisterAsync(CodeFixContext context, Parameters parameters);
}
