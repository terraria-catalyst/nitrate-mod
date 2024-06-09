using System.Collections.Generic;
using System.IO;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches;

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
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class TreeshakePreprocessors(CommonContext ctx, string sourceDirectory, string targetDirectory) : SetupOperation(ctx)
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
		
		var includeStack = new Stack<bool>();
		var blockIncludedStack = new Stack<bool>();
		includeStack.Push(true);
		blockIncludedStack.Push(false);
		
		foreach (var line in lines)
		{
			var trimmedLine = line.Trim();
			
			if (trimmedLine.StartsWith("#if"))
			{
				var condition = trimmedLine[3..].Trim();
				var include = EvaluateCondition(condition, symbols);
				includeStack.Push(include && !blockIncludedStack.Peek());
				blockIncludedStack.Push(includeStack.Peek());
			}
			else if (trimmedLine.StartsWith("#elif"))
			{
				var condition = trimmedLine[5..].Trim();
				if (includeStack.Count <= 1)
				{
					continue;
				}
				
				includeStack.Pop();
				var include = EvaluateCondition(condition, symbols);
				includeStack.Push(include && !blockIncludedStack.Peek());
				
				if (includeStack.Peek())
				{
					blockIncludedStack.Pop();
				}
				
				blockIncludedStack.Push(includeStack.Peek());
			}
			else if (trimmedLine.StartsWith("#else"))
			{
				if (includeStack.Count <= 1)
				{
					continue;
				}
				
				includeStack.Pop();
				includeStack.Push(!blockIncludedStack.Pop());
				blockIncludedStack.Push(includeStack.Peek());
			}
			else if (trimmedLine.StartsWith("#endif"))
			{
				if (includeStack.Count <= 1)
				{
					continue;
				}
				
				includeStack.Pop();
				blockIncludedStack.Pop();
			}
			else if (includeStack.Peek())
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
		if (expression.StartsWith('!'))
		{
			var subExpression = expression[1..];
			return !EvaluateExpression(subExpression, symbols);
		}
		
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
