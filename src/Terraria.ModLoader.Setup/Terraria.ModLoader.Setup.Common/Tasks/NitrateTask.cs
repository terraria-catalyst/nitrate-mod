using System.Collections.Generic;
using System.IO;
using System.Linq;

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
		public override void Run() { }
	}
	
	private sealed class FormatWithDotnetFormatAndEditorConfigTask(CommonContext ctx, string sourceDirectory, string targetDirectory) : SetupOperation(ctx)
	{
		public override void Run() { }
	}
	
	// "src/staging/tModLoader", "src/staging/Nitrate", "patches/Nitrate"
	public NitrateTask(CommonContext ctx, string baseDir, string patchedDir, string patchDir) : base(ctx, GetOperations(ctx, baseDir, patchedDir, patchDir)) { }
	
	public static SetupOperation[] GetOperations(CommonContext ctx, string baseDir, string patchedDir, string patchDir)
	{
		return [new OrganizeExistingPartialClassesTask(ctx, update(ref baseDir, patchedDir + nameof(OrganizeExistingPartialClassesTask)), baseDir), new PatchTask(ctx, baseDir, patchedDir, patchDir),];
		
		T update<T>(ref T value, T newValue)
		{
			var old = value;
			value = newValue;
			return old;
		}
	}
}
