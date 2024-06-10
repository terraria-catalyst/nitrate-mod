using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Terraria.ModLoader.Setup.Common.Tasks.Roslyn;

public abstract class RoslynTask(CommonContext ctx) : SetupOperation(ctx)
{
	private string projectPath;

	protected abstract string Status { get; }

	protected virtual int MaxDegreeOfParallelism => 0;

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
		RunAsync().GetAwaiter().GetResult();
	}

	public async Task RunAsync()
	{
		using var workspace = CreateWorkspace();
		// todo proper error log
		workspace.WorkspaceFailed += (o, e) => Console.Error.WriteLine(e.Diagnostic.Message);

		Console.WriteLine($"Loading project '{projectPath}'");

		// Attach progress reporter, so we print projects as they are loaded.
		var project = await workspace.OpenProjectAsync(projectPath);
		var workItems = project.Documents.Select(
			doc => new WorkItem(
				Status + " " + doc.Name,
				async () =>
				{
					var newDoc = await Process(doc);
					if (newDoc.FilePath is null)
					{
						throw new InvalidOperationException("New document file path was null");
					}

					var before = await doc.GetTextAsync();
					var after = await newDoc.GetTextAsync();
					if (before != after)
					{
						await File.WriteAllTextAsync(newDoc.FilePath, after.ToString());
					}
				}
			)
		);

		ExecuteParallel(workItems.ToList(), maxDegree: MaxDegreeOfParallelism);
	}

	private static bool msBuildFound;

	private MSBuildWorkspace CreateWorkspace()
	{
		if (msBuildFound)
		{
			return MSBuildWorkspace.Create();
		}

		var status = Context.Progress.CreateStatus(0, 1);

		VisualStudioInstance? vsInst;
		status.AddMessage("Finding MSBuild");
		{
			vsInst = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(inst => inst.Version).First();
			MSBuildLocator.RegisterInstance(vsInst);
			status.Current++;
		}

		status.AddMessage($"Found MSBuild {vsInst.Version} at {vsInst.MSBuildPath}");
		{
			msBuildFound = true;
		}

		return MSBuildWorkspace.Create();
	}

	protected abstract Task<Document> Process(Document doc);
}
