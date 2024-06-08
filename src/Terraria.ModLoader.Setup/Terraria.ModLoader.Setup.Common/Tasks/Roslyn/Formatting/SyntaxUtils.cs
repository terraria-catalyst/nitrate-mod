﻿using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Terraria.ModLoader.Setup.Common.Tasks.Roslyn.Formatting;

internal static class SyntaxUtils
{
	public static bool SpansSingleLine(this SyntaxNode node)
	{
		return !node.DescendantTrivia(node.Span).Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
	}
	
	public static bool IsWhitespace(this SyntaxTrivia trivia)
	{
		return trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.EndOfLineTrivia);
	}
	
	public static string GetIndentation(this SyntaxNode node)
	{
		return GetIndentation(node.SyntaxTree, node.SpanStart);
	}
	
	public static string GetIndentation(SyntaxTree tree, int position)
	{
		var start = tree.GetText().Lines.GetLineFromPosition(position).Start;
		var whitespace = tree.GetRoot().FindTrivia(start, true);
		return whitespace.ToFullString();
	}
	
	public static SyntaxToken WithLeadingWhitespace(this SyntaxToken node, string indent)
	{
		return WithLeadingWhitespace((SyntaxNodeOrToken)node, indent).AsToken();
	}
	
	public static SyntaxNodeOrToken WithLeadingWhitespace(this SyntaxNodeOrToken node, string indent)
	{
		var t = node.GetLeadingTrivia();
		var last = t.Last();
		if (!last.IsKind(SyntaxKind.WhitespaceTrivia))
		{
			t = t.Add(SyntaxFactory.Whitespace(indent));
		}
		else if (last.ToFullString() == indent)
		{
			return node;
		}
		else
		{
			t = t.Replace(last, SyntaxFactory.Whitespace(indent));
		}
		
		return node.WithLeadingTrivia(t);
	}
}