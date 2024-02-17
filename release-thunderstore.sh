#!/bin/bash
MOD_NAME="$1"
if [ -z "$MOD_NAME" ]; then
    echo "usage: ./release-thunderstore.sh <mod_name>"
    exit 1
fi

# set up
die() {
    >&2 echo "something has gone terribly wrong"
    exit 1
}
PROJECT_DIR="$(dirname "$0")"
pushd . > /dev/null || die
cd "$PROJECT_DIR" || die
MOD_DIR="$MOD_NAME"
BUILD_DIR="$MOD_NAME/release-thunderstore"

# extract version
VERSION="$(rg -or '$1' 'MOD_VERSION = "(.+)";' $MOD_DIR/$MOD_NAME.cs)" || die

# rebuild project to ensure the extracted version matches the binary
dotnet build "$MOD_DIR/$MOD_NAME.csproj" --no-restore --configuration Release > /dev/null || die

# create release files
mkdir -p "$BUILD_DIR/BepInEx/plugins" || die
cp "bin/Release/$MOD_NAME.dll" "$BUILD_DIR/BepInEx/plugins/$MOD_NAME.dll" || die
cat $BUILD_DIR/manifest-template.json \
    | sed "s/\$VERSION/$VERSION/" \
    | sed "s/\$MOD_NAME/$MOD_NAME/" \
    > $BUILD_DIR/manifest.json || die

# create release zip. Note that 7z has no base directory flag, so I'm forced to cd into the archive's base directory
rm "$MOD_NAME-$VERSION.zip" 2> /dev/null
pushd . > /dev/null
cd "$BUILD_DIR"
7z a -mx9 "../../$MOD_NAME-$VERSION.zip" "BepInEx" "../README.md" "../CHANGELOG.md" "manifest.json" "icon.png" > /dev/null || die
popd > /dev/null

# If I need to debug the zip file structure, do this:
#7z l "$MOD_NAME-$VERSION.zip"

# clean up
rm "$BUILD_DIR/manifest.json"
rm -r "$BUILD_DIR/BepInEx"
