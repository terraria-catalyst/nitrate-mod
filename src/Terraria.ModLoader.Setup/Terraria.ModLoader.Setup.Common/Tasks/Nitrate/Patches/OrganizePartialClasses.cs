using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches;

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
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class OrganizePartialClasses(CommonContext ctx, string sourceDirectory, string targetDirectory) : SetupOperation(ctx)
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
