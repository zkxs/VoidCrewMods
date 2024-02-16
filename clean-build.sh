#!/bin/bash
rm -rf bin obj
dotnet build all-mods.csproj --configuration Release
