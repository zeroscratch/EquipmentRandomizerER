#!/usr/bin/bash

# The build system for windows projects will be different because the project is built using an IDE.
# We just script the copying for building on Ubuntu
PUBLISH_DIR=./publish/ZeroScratchBingoRandomizer

# Relocate the README temporarily so the publish command can work
echo "Copying README.md to temp location"
cp ../../README.md ..

# Run the build command
echo "Building executable"
dotnet publish -c Release /p:DebugType=none -r win-x64 --self-contained true --output $PUBLISH_DIR && \
echo "Build finished"

# Cleanup temp file
echo "Cleaning up README.md from temp location"
rm ../README.md

# Copy resources
echo "Copying resources to $PUBLISH_DIR"
cp -r ./Resources $PUBLISH_DIR
