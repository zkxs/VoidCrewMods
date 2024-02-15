#!/bin/bash

# set up
die() {
    popd > /dev/null
    exit 1
}
RELEASE_DIR="$(dirname "$0")"
pushd . > /dev/null || exit 1
cd "$RELEASE_DIR" || die

# extract version
VERSION="$(rg -or '$1' 'MOD_VERSION = "(.+)";' ../RecentPlayers.cs)" || die

# build project to ensure the extracted version matches the binary
dotnet build "../RecentPlayers.csproj" --configuration Release || die

# create release files
mkdir -p "./BepInEx/plugins" || die
cp "../bin/Release/netstandard2.1/RecentPlayers.dll" "./BepInEx/plugins/RecentPlayers.dll" || die
sed "s/\$VERSION/$VERSION/" manifest-template.json > manifest.json || die

# create release zip
rm "RecentPlayers-$VERSION.zip" 2> /dev/null
7z a -mx9 "RecentPlayers-$VERSION.zip" BepInEx ../README.md ../CHANGELOG.md manifest.json icon.png || die

# clean up
popd > /dev/null
