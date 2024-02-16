if [ -z "$MOD_NAME" ]; then
    echo "do not run this directly"
    exit 1
fi

# set up
die() {
    popd > /dev/null
    exit 1
}
RELEASE_DIR="$(dirname "$0")"
pushd . > /dev/null || exit 1
cd "$RELEASE_DIR" || die

# extract version
VERSION="$(rg -or '$1' 'MOD_VERSION = "(.+)";' ../$MOD_NAME.cs)" || die

# build project to ensure the extracted version matches the binary
dotnet build "../$MOD_NAME.csproj" --configuration Release || die

# create release files
mkdir -p "./BepInEx/plugins" || die
cp "../../bin/Release/$MOD_NAME.dll" "./BepInEx/plugins/$MOD_NAME.dll" || die
sed "s/\$VERSION/$VERSION/" manifest-template.json > manifest.json || die

# create release zip
rm "$MOD_NAME-$VERSION.zip" 2> /dev/null
7z a -mx9 "$MOD_NAME-$VERSION.zip" BepInEx ../README.md ../CHANGELOG.md manifest.json icon.png || die

# clean up
popd > /dev/null
