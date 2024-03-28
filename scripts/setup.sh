#!/bin/sh
echo "Step 1: Generating Symbolic Links..."
if ! node ../generate-symlinks.js; then
    echo "Error: Failed to generate symbolic links."
    exit 1
fi

echo "Step 2: Updating Submodules..."
if ! git submodule update --init --recursive; then
    echo "Error: Failed to update submodules."
    exit 1
fi

echo "Step 3: Building Catalyst Build Tools..."
if ! dotnet restore ../catalyst/src/TeamCatalyst.Catalyst.Build; then
    echo "Error: Failed to restore Catalyst Tools dependencies."
    exit 1
fi

if ! dotnet build ../catalyst/src/TeamCatalyst.Catalyst.Build -c Release; then
    echo "Error: Failed to build Catalyst Tools."
    exit 1
fi

echo "Setup complete!"