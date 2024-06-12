using Microsoft.CodeAnalysis;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches.Analyzers;

/// <summary>
///		An object that analyzers a document in a compilation unit and provides
///		code fixes if necessary.
/// </summary>
internal abstract class AbstractAnalyzer
{
	public virtual bool ProcessDocument(Compilation compilation, Document document)
	{
		return false;
	}
}
