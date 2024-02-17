# Changelog

This project follows [semantic versioning](https://semver.org/).

# 1.1.1 - 2024-02-17

## Added

- Fix a vanilla bug where your STEAM_ID property is set incorrectly. This fixes the NameIconRankDisplayer. Note that I don't know if the NameIconRankDisplayer is actually used anywhere, but with this fix remote players will see your steam avatar in it.

## Fixed

- Additional fixes for yesterday's Void Crew update. It turns out that Hutlihut is setting the STEAM_ID property to some sort of Unity Cloud ID, so I have to get the actual SteamID from somewhere else.

# 1.1.0 - 2024-02-16

## Added

- Added some BepInEx configurations that can be used for debug tuning

## Fixed

- Fix for today's Void Crew update changing where SteamIDs are stored.

# 1.0.0 - 2024-02-15

## Added

- Initial release. It does what it says on the tin: populate Steam's recent players menu
