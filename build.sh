#!/bin/bash
# Note that this won't work unless the project has already been restored by clean-build.sh
dotnet build all-mods.csproj --no-restore --configuration Release
