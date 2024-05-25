using System;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terraria.ModLoader.Setup.Common.Tasks.Roslyn.Formatting;

internal sealed class RemoveBracesFromSingleStatementRewriter : CSharpSyntaxRewriter
{
	private readonly SyntaxAnnotation processedAnnotation = new();
	
	public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
	{
		if (node.HasAnnotation(processedAnnotation))
		{
			return base.VisitIfStatement(node);
		}
		
		if (NoClausesNeedBraces(node))
		{
			node = RemoveClauseBraces(node); //TODO: trailing space
		}
		
		node = AnnotateTreeProcessed(node);
		return base.VisitIfStatement(node);
	}
	
	private IfStatementSyntax AnnotateTreeProcessed(IfStatementSyntax node)
	{
		if (node.Else?.Statement is IfStatementSyntax elseIfStmt)
		{
			node = node.WithElse(node.Else.WithStatement(AnnotateTreeProcessed(elseIfStmt)));
		}
		
		return node.WithAdditionalAnnotations(processedAnnotation);
	}
	
	private bool NoClausesNeedBraces(IfStatementSyntax ifStmt)
	{
		// lets not destroy the stack
		while (true)
		{
			if (!StatementIsSingleLine(ifStmt.Statement))
			{
				return false;
			}
			
			var elseStmt = ifStmt.Else?.Statement;
			if (elseStmt == null)
			{
				return true;
			}
			
			if (elseStmt is IfStatementSyntax elseifStmt)
			{
				ifStmt = elseifStmt;
			}
			else
			{
				return StatementIsSingleLine(elseStmt);
			}
		}
	}
	
	private static bool StatementIsSingleLine(SyntaxNode node)
	{
		return node switch
		{
			BlockSyntax block => block.Statements.Count == 1 && block.GetLeadingTrivia().All(SyntaxUtils.IsWhitespace) && block.GetTrailingTrivia().All(SyntaxUtils.IsWhitespace) && StatementIsSingleLine(block.Statements[0]),
			// removing braces around if statements can change semantics
			IfStatementSyntax => false,
			// single line statements cannot be labelled or contain declarations
			LabeledStatementSyntax or LocalDeclarationStatementSyntax => false,
			_ => node.SpansSingleLine(),
		};
	}
	
	private IfStatementSyntax RemoveClauseBraces(IfStatementSyntax node)
	{
		if (node.Statement is BlockSyntax block)
		{
			node = node
				.WithStatement(RemoveBraces(block))
				.WithCloseParenToken(EnsureEndsLine(node.CloseParenToken));
		}
		
		if (node.Else is not { } elseClause)
		{
			return node;
		}
		
		elseClause = elseClause.Statement switch
		{
			IfStatementSyntax elseif => elseClause.WithStatement(RemoveClauseBraces(elseif)),
			BlockSyntax elseBlock => elseClause.WithStatement(RemoveBraces(elseBlock)).WithElseKeyword(EnsureEndsLine(elseClause.ElseKeyword)),
			_ => elseClause
		};
		
		node = node.WithElse(elseClause);
		return node;
	}
	
	private SyntaxToken EnsureEndsLine(SyntaxToken token)
	{
		return token.WithTrailingTrivia(EnsureEndsLine(token.TrailingTrivia));
	}
	
	private StatementSyntax EnsureEndsLine(StatementSyntax node)
	{
		return node.WithTrailingTrivia(EnsureEndsLine(node.GetTrailingTrivia()));
	}
	
	private static SyntaxTriviaList EnsureEndsLine(SyntaxTriviaList trivia)
	{
		return trivia.LastOrDefault().IsKind(SyntaxKind.EndOfLineTrivia) ? trivia : trivia.Add(SyntaxFactory.EndOfLine(Environment.NewLine));
	}
	
	private StatementSyntax RemoveBraces(BlockSyntax block)
	{
		return EnsureEndsLine(block.Statements[0]);
	}
	
	public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax method)
	{
		if (method.Body != null && StatementIsSingleLine(method.Body) && method.Body.DescendantTrivia().All(SyntaxUtils.IsWhitespace) && method.Body.Statements[0] is ReturnStatementSyntax { Expression: not null, } returnStatement)
		{
			method = method
				.WithBody(null)
				.WithExpressionBody(SyntaxFactory.ArrowExpressionClause(returnStatement.Expression))
				.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
			
			//TODO: remove newlines between method and arrowexpression
		}
		
		return base.VisitMethodDeclaration(method);
	}
}
