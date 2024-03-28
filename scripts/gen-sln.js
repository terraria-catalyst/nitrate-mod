const fs = require("fs");
const crypto = require("crypto");

const name = process.argv[2];
const guid0 = crypto.randomUUID();
const guid1 = crypto.randomUUID();
const slnText = `
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{{GUID_0}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{GUID_0}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{GUID_0}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{GUID_0}}.Release|Any CPU.Build.0 = Release|Any CPU
		{{GUID_1}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{GUID_1}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{GUID_1}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{GUID_1}}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
`
  .replace(/{GUID_0}/g, guid0)
  .replace(/{GUID_1}/g, guid1);

try {
  fs.mkdirSync(`../src/`, { recursive: true });
} catch (e) {}
fs.writeFileSync(`../src/${name}.sln`, slnText);
