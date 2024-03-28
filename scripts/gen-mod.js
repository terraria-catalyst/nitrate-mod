const fs = require("fs");

const ns = process.argv[2];
const internalName = process.argv[3];
const csprojText = `
<Project Sdk="Microsoft.NET.Sdk">

    <Import Project="../../catalyst/Mod.Build.targets" />

    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
        <Nullable>enable</Nullable>
        <RootNamespace>{NAMESPACE}</RootNamespace>

        <AssemblyPublicizerPaths>$(AssemblyPublicizerPaths);$(MSBuildThisFileDirectory){NAME_LOWER}.publicizer.js</AssemblyPublicizerPaths>
    </PropertyGroup>

</Project>
`
  .trim()
  .replace(/{NAMESPACE}/g, ns)
  .replace(/{NAME_LOWER}/g, internalName.toLowerCase());
const buildText = `
displayName = {NAME}
author = Me
version = 1.0.0
`
  .trim()
  .replace(/{NAME}/g, internalName);

const descriptionText = "TODO";
const workshopText = "TODO";
const publicizerText = `
import {createPublicizer} from "publicizer";

export const publicizer = createPublicizer("{NAME}");

publicizer.createAssembly("tModLoader").publicizeAll();
`
  .trim()
  .replace(/{NAME}/g, internalName);

const launchSettingsText = `
  {
    "profiles": {
      "Terraria (Client) (dotnet)": {
        "commandName": "Executable",
        "executablePath": "dotnet",
        "commandLineArgs": "$(tMLPath) -unsafe true",
        "workingDirectory": "$(tMLSteamPath)"
      },
      "Terraria (Server) (dotnet)": {
        "commandName": "Executable",
        "executablePath": "dotnet",
        "commandLineArgs": "$(tMLServerPath) -unsafe true",
        "workingDirectory": "$(tMLSteamPath)"
      },
      "Terraria (Client) (dotnet.exe)": {
        "commandName": "Executable",
        "executablePath": "dotnet.exe",
        "commandLineArgs": "$(tMLPath) -unsafe true",
        "workingDirectory": "$(tMLSteamPath)"
      },
      "Terraria (Server) (dotnet.exe)": {
        "commandName": "Executable",
        "executablePath": "dotnet.exe",
        "commandLineArgs": "$(tMLServerPath) -unsafe true",
        "workingDirectory": "$(tMLSteamPath)"
      }
    }
  }
`.trim();

try {
  fs.mkdirSync(`../src/` + internalName, { recursive: true });
  fs.mkdirSync(`../src/${internalName}/Properties/`, { recursive: true });
} catch (e) {}

fs.writeFileSync(`../src/${internalName}/${internalName}.csproj`, csprojText);
fs.writeFileSync(`../src/${internalName}/build.txt`, buildText);
fs.writeFileSync(`../src/${internalName}/description.txt`, descriptionText);
fs.writeFileSync(
  `../src/${internalName}/description_workshop.txt`,
  workshopText
);
fs.writeFileSync(
  `../src/${internalName}/${internalName.toLowerCase()}.publicizer.js`,
  publicizerText
);
fs.writeFileSync(
  `../src/${internalName}/Properties/launchSettings.json`,
  launchSettingsText
);
fs.copyFileSync(
  `assets/icon_workshop.png`,
  `../src/${internalName}/icon_workshop.png`
);
fs.copyFileSync(`assets/icon.png`, `../src/${internalName}/icon.png`);

// add to mods.json
const modsJson = require("../mods.json");
modsJson.push(internalName);
fs.writeFileSync("../mods.json", JSON.stringify(modsJson, null, 4));
