#!/bin/bash
MOD_NAME="$1"
if [ -z "$MOD_NAME" ]; then
    echo "usage: ./release-thunderstore.sh <mod_name>"
    exit 1
fi

# set up
die() {
    popd > /dev/null
    exit 1
}
PROJECT_DIR="$(dirname "$0")"
pushd . > /dev/null || exit 1
cd "$PROJECT_DIR" || die
MOD_DIR="$MOD_NAME"
BUILD_DIR="$MOD_NAME/release-thunderstore"

# extract version
VERSION="$(rg -or '$1' 'MOD_VERSION = "(.+)";' $MOD_DIR/$MOD_NAME.cs)" || die

# build project to ensure the extracted version matches the binary
dotnet build "$MOD_DIR/$MOD_NAME.csproj" --configuration Release || die

# create release files
mkdir -p "$BUILD_DIR/BepInEx/plugins" || die
cp "bin/Release/$MOD_NAME.dll" "$BUILD_DIR/BepInEx/plugins/$MOD_NAME.dll" || die
cat $BUILD_DIR/manifest-template.json \
    | sed "s/\$VERSION/$VERSION/" \
    | sed "s/\$MOD_NAME/$MOD_NAME/" \
    > $BUILD_DIR/manifest.json || die

# create release zip
rm "$MOD_NAME-$VERSION.zip" 2> /dev/null
7z a -mx9 "$MOD_NAME-$VERSION.zip" "$BUILD_DIR/BepInEx" "$MOD_DIR/README.md" "$MOD_DIR/CHANGELOG.md" "$BUILD_DIR/manifest.json" "$BUILD_DIR/icon.png" || die

# clean up
rm "$BUILD_DIR/manifest.json"
rm -r "$BUILD_DIR/BepInEx"
popd > /dev/null
