#!/bin/bash
rm -rf bin obj *.zip
dotnet restore all-mods.csproj --configfile NuGet.Offline.Config
./build.sh
