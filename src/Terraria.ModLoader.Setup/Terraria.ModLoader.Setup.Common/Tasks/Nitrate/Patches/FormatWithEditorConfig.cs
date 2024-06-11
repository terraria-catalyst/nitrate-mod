using System.Collections.Generic;
using System.IO;

namespace Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches;

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
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class FormatWithEditorConfig(CommonContext ctx, string sourceDirectory, string targetDirectory) : SetupOperation(ctx)
{
	public override void Run()
	{
		var items = new List<WorkItem>();

		foreach (var (file, relPath) in EnumerateFiles(sourceDirectory))
		{
			var destination = Path.Combine(targetDirectory, relPath);

			// Exclude non-csproj files and templates.
			if (!relPath.EndsWith(".csproj") || relPath.EndsWith("{{ModName}}.csproj"))
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
			if (!relPath.EndsWith(".csproj") || relPath.EndsWith("{{ModName}}.csproj"))
			{
				continue;
			}

			// Copy .editorconfig over to the target directory.
			File.Copy(editorconfigPath, Path.Combine(Path.GetDirectoryName(file)!, ".editorconfig"), true);

			// Run dotnet format in directory.
			items.Add(
				new WorkItem(
					"Formatting: " + relPath + " (with dotnet format and cleanupcode.exe)",
					() =>
					{
						var cwd = Path.GetDirectoryName(file)!;
						CommonSetup.RunCommand(cwd, "dotnet", "format analyzers --severity info -v diag --exclude-diagnostics SYSLIB1054 CA1822", cancel: Context.TaskInterface.CancellationToken);
						CommonSetup.RunCommand(cwd, "dotnet", "format style --severity info -v diag --exclude-diagnostics SYSLIB1054 CA1822", cancel: Context.TaskInterface.CancellationToken);
						CommonSetup.RunCommand(cwd, "dotnet", "format whitespace -v diag", cancel: Context.TaskInterface.CancellationToken);
						// CommonSetup.RunCommand(cwd, "dotnet", "cleanupcode", cancel: Context.TaskInterface.CancellationToken);
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
