const path = require("path");
const fs = require("fs");

const dirs = require("./mods.json");

const sourceDirs = dirs.map((dir) => path.join(__dirname, "src", dir));
const targetDirs = dirs.map((dir) => path.join(__dirname, "..", dir));

for (let i = 0; i < sourceDirs.length; i++) {
  const sourceDir = sourceDirs[i];
  const targetDir = targetDirs[i];

  if (!fs.existsSync(targetDir)) {
    fs.symlinkSync(sourceDir, targetDir, "dir");
  }
}
