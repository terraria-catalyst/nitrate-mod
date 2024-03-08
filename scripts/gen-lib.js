const fs = require("fs");

const ns = process.argv[2];
const csprojText = `
<Project Sdk="Microsoft.NET.Sdk">

    <Import Project="../../catalyst/Library.Build.targets" />

    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
        <Nullable>enable</Nullable>
    </PropertyGroup>

</Project>
`.trim();

try {
  fs.mkdirSync(`../src/` + ns, { recursive: true });
} catch (e) {}

fs.writeFileSync(`../src/${ns}/${ns}.csproj`, csprojText);
