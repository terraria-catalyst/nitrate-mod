using System;
using System.IO;

using Mono.Cecil;

using MonoMod;

using HookGenerator = Terraria.ModLoader.Setup.Common.Utilities.HookGenerator;

namespace Terraria.ModLoader.Setup.Common.Tasks;

public sealed class HookGenTask(CommonContext ctx) : SetupOperation(ctx)
{
	private const string dotnet_sdk_version = "8.0.1";
	private const string dotnet_target_version = "net8.0";
	private const string libs_path = "src/staging/tModLoader/Terraria/Libraries";
	private const string bin_libs_path = $"src/staging/tModLoader/Terraria/bin/Release/{dotnet_target_version}/Libraries";
	private const string tml_assembly_path = @$"src/staging/tModLoader/Terraria/bin/Release/{dotnet_target_version}/tModLoader.dll";
	private const string installed_net_refs = $@"\dotnet\packs\Microsoft.NETCore.App.Ref\{dotnet_sdk_version}\ref\{dotnet_target_version}";
	
	public override void Run()
	{
		var status = Context.Progress.CreateStatus(0, 2);
		
		if (!File.Exists(tml_assembly_path))
		{
			Context.TaskInterface.ShowDialogWithOkFallback("tML exe not found", $"\"{tml_assembly_path}\" does not exist.", SetupMessageBoxButtons.Ok, SetupMessageBoxIcon.Error);
			status.AddMessage("Canceled");
			return;
		}
		
		var outputPath = Path.Combine(libs_path, "Common", "TerrariaHooks.dll");
		if (File.Exists(outputPath))
		{
			File.Delete(outputPath);
		}
		
		status.AddMessage("Hooking: tModLoader.dll -> TerrariaHooks.dll");
		{
			if (!HookGen(Context, tml_assembly_path, outputPath))
			{
				status.AddMessage("Cancelled");
				return;
			}
			
			status.Current++;
		}
		
		File.Delete(Path.ChangeExtension(outputPath, "pdb"));
		Context.TaskInterface.ShowDialogWithOkFallback("Success", "Make sure you diff tModLoader after this", SetupMessageBoxButtons.Ok, SetupMessageBoxIcon.Information);
		status.Current++;
		status.AddMessage("Successfully generated TerrariaHooks.dll");
	}
	
	public static bool HookGen(CommonContext ctx, string inputPath, string outputPath)
	{
		var dotnetReferencesDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + installed_net_refs;
		
		// Ensure that refs are present, for God's sake!
		if (!Directory.Exists(dotnetReferencesDirectory) || Directory.GetFiles(dotnetReferencesDirectory, "*.dll").Length == 0)
		{
			// Replace with exceptions if this is ever called in CLI.
			ctx.TaskInterface.ShowDialogWithOkFallback(
				$".NET SDK {dotnet_sdk_version} not found",
				$"""Unable to find reference libraries for .NET SDK '{dotnet_sdk_version}' - "{dotnetReferencesDirectory}" does not exist.""",
				SetupMessageBoxButtons.Ok,
				SetupMessageBoxIcon.Error
			);
			
			return false;
		}
		
		using var mm = new MonoModder
		{
			InputPath = inputPath,
			OutputPath = outputPath,
			ReadingMode = ReadingMode.Deferred,
			
			DependencyDirs = { dotnetReferencesDirectory, },
			MissingDependencyThrow = false,
		};
		
		mm.DependencyDirs.AddRange(Directory.GetDirectories(bin_libs_path, "*", SearchOption.AllDirectories));
		
		mm.Read();
		
		var gen = new HookGenerator(mm, "TerrariaHooks")
		{
			HookPrivate = true,
		};
		
		foreach (var type in mm.Module.Types)
		{
			if (!type.FullName.StartsWith("Terraria") || type.FullName.StartsWith("Terraria.ModLoader"))
			{
				continue;
			}
			
			gen.GenerateFor(type, out var hookType, out var hookIlType);
			if (hookType == null || hookIlType == null || hookType.IsNested)
			{
				continue;
			}
			
			AdjustNamespaceStyle(hookType);
			AdjustNamespaceStyle(hookIlType);
			
			gen.OutputModule.Types.Add(hookType);
			gen.OutputModule.Types.Add(hookIlType);
		}
		
		gen.OutputModule.Write(outputPath);
		
		return true;
	}
	
	// convert
	//   On.Namespace.Type -> Namespace.On_Type
	//   IL.Namespace.Type -> Namespace.IL_Type
	private static void AdjustNamespaceStyle(TypeReference type)
	{
		if (string.IsNullOrEmpty(type.Namespace))
		{
			return;
		}
		
		type.Name = type.Namespace[..2] + '_' + type.Name;
		type.Namespace = type.Namespace[Math.Min(3, type.Namespace.Length)..];
	}
}
