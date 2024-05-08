using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Terraria.ModLoader.Setup.Formatting;

internal sealed class UnindentRewriter : CSharpSyntaxRewriter
{
	private bool nextTokenIsOnNewLine = true;
	
	public override SyntaxToken VisitToken(SyntaxToken token)
	{
		if (token.IsKind(SyntaxKind.None))
		{
			return token; // skip these? Annoying
		}
		
		if (nextTokenIsOnNewLine)
		{
			token = Unindent(token);
		}
		
		nextTokenIsOnNewLine = EndsLine(token.TrailingTrivia);
		return token;
	}
	
	private static SyntaxToken Unindent(SyntaxToken token)
	{
		var triviaList = token.LeadingTrivia;
		var nextTriviaIsOnNewLine = true;
		for (var i = 0; i < triviaList.Count; i++)
		{
			var t = triviaList[i];
			if (t.IsKind(SyntaxKind.EndOfLineTrivia))
			{
				nextTriviaIsOnNewLine = true;
				continue;
			}
			
			if (nextTriviaIsOnNewLine && t.Span.Length > 0 && t.IsKind(SyntaxKind.WhitespaceTrivia) && t.ToString()[0] == '\t')
			{
				triviaList = triviaList.Replace(t, SyntaxFactory.Whitespace(t.ToString()[1..]));
			}
			
			nextTriviaIsOnNewLine = false;
		}
		
		return token.WithLeadingTrivia(triviaList);
	}
	
	private static bool EndsLine(SyntaxTriviaList trailingTrivia)
	{
		return trailingTrivia.Count > 0 && trailingTrivia.Last().IsKind(SyntaxKind.EndOfLineTrivia);
	}
}
