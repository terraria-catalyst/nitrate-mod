using System;
using System.IO;
using System.Linq;

using Terraria.ModLoader.Setup.Common.Tasks;
using Terraria.ModLoader.Setup.Common.Tasks.Nitrate;
using Terraria.ModLoader.Setup.Common.Tasks.Nitrate.Patches;

namespace Terraria.ModLoader.Setup.Common;

/// <summary>
///		Common (shared) context for a task interface lifetime.
/// </summary>
public sealed class CommonContext(ITaskInterface taskInterface)
{
	/// <summary>
	///		Context for <see cref="NitrateTask"/> patches.
	/// </summary>
	public sealed class NitratePatchContext
	{
		/// <summary>
		///		The path to use for diffing Nitrate against.
		/// </summary>
		/// <remarks>
		///		Required since patch steps use intermediary unchecked
		///		directories.
		/// </remarks>
		public string NitrateDiffingPath { get; }

		/// <summary>
		///		Patch operations before and including the initial formatting
		///		analysis (which takes a long time).
		/// </summary>
		public SetupOperation[] PreAnalysisOperations { get; }

		/// <summary>
		///		Patch operations performed after the initial formatting
		///		analysis, allowing development to be sped up significantly.
		/// </summary>
		public SetupOperation[] PostAnalysisOperations { get; }

		/// <summary>
		///		The final patch operation.
		/// </summary>
		public SetupOperation PatchOperation { get; }

		/// <summary>
		///		All operations in the order they should be executed.
		/// </summary>
		public SetupOperation[] AllOperations => PreAnalysisOperations.Concat(PostAnalysisOperations).Append(PatchOperation).ToArray();

		public NitratePatchContext(CommonContext ctx, string baseDir, string patchedDir, string patchDir)
		{
			PreAnalysisOperations = [ patch<OrganizePartialClasses>(), patch<MakeTypesPartial>(), patch<TreeshakePreprocessors>(), patch<FormatWithEditorConfig>(), ];
			PostAnalysisOperations = [];
			PatchOperation = new PatchTask(ctx, baseDir, patchedDir, patchDir);

			NitrateDiffingPath = baseDir;

			return;

			T patch<T>() where T : SetupOperation
			{
				return (T)Activator.CreateInstance(typeof(T), ctx, baseDir, baseDir = patchedDir + '_' + typeof(T).Name)!;
			}
		}
	}

	public ITaskInterface TaskInterface { get; } = taskInterface;

	public IProgressManager Progress => TaskInterface.Progress;

	public ISettingsManager Settings => TaskInterface.Settings;

	/// <summary>
	///		Whether this is an automated setup.
	/// </summary>
	public bool IsAutomatic { get; init; }

	/// <summary>
	///		The branch being used.
	/// </summary>
	public string Branch { get; set; } = "";

	private string? terrariaSteamDirectory;

	/// <summary>
	///		The path to Terraria's Steam directory.
	/// </summary>
	public string TerrariaSteamDirectory
	{
		get => IsAutomatic ? terrariaSteamDirectory ??= TaskInterface.Settings.Get<TerrariaPathSettings>().TerrariaSteamDirectory : TaskInterface.Settings.Get<TerrariaPathSettings>().TerrariaSteamDirectory;

		set
		{
			if (IsAutomatic)
			{
				terrariaSteamDirectory = value;
			}
			else
			{
				TaskInterface.Settings.Get<TerrariaPathSettings>().TerrariaSteamDirectory = value;
			}
		}
	}

	private string? tmlDeveloperSteamDirectory;

	/// <summary>
	///		The path to the Terraria.ModLoader (tModLoader) development Steam
	///		directory.
	/// </summary>
	public string TmlDeveloperSteamDirectory
	{
		get => IsAutomatic ? tmlDeveloperSteamDirectory ??= TaskInterface.Settings.Get<TerrariaPathSettings>().TmlDeveloperSteamDirectory : TaskInterface.Settings.Get<TerrariaPathSettings>().TmlDeveloperSteamDirectory;

		set
		{
			if (IsAutomatic)
			{
				tmlDeveloperSteamDirectory = value;
			}
			else
			{
				TaskInterface.Settings.Get<TerrariaPathSettings>().TmlDeveloperSteamDirectory = value;
			}
		}
	}

	/// <summary>
	///		The path to Terraria's executable.
	/// </summary>
	public string TerrariaPath => Path.Combine(TerrariaSteamDirectory, "Terraria.exe");

	/// <summary>
	///		The path to Terraria's server executable.
	/// </summary>
	public string TerrariaServerPath => Path.Combine(TerrariaSteamDirectory, "TerrariaServer.exe");

	public NitratePatchContext CreateNitratePatchContext(string baseDir, string patchedDir, string patchDir)
	{
		return new NitratePatchContext(this, baseDir, patchedDir, patchDir);
	}
}
