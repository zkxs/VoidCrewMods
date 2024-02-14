// This file is part of RecentPlayers and is licensed under the MIT License.
// See LICENSE file for full text.
// Copyright © 2024 Michael Ripley

using System;
using BepInEx;
using BepInEx.Logging;
using Photon.Pun;
using Photon.Realtime;
using Steamworks;

namespace RecentPlayers
{
    [BepInPlugin(GUID, MOD_NAME, MOD_VERSION)]
    public class RecentPlayers : BaseUnityPlugin
    {
        internal const string GUID = "dev.zkxs.voidcrew.recentplayers";
        internal const string MOD_NAME = "RecentPlayers";
        internal const string MOD_VERSION = "1.0.0";

        internal static new ManualLogSource? Logger;

        private void Awake()
        {
            try
            {
                Logger = base.Logger; // this lets us access the logger from static contexts later: namely our patches.

                PhotonNetwork.AddCallbackTarget(new PhotonInRoomCallbacks());
                PhotonNetwork.AddCallbackTarget(new PhotonMatchmakingCallbacks());

                // Nothing broke. Emit a log to indicate this.
                Logger.LogInfo($"{MOD_NAME} successfully initialized!");
            }
            catch (Exception e)
            {
                base.Logger.LogError($"Something has gone terribly wrong:\n{e}");
                throw e;
            }
        }

        public static void SetPlayedWith(Player player)
        {
            try
            {
                if (!player.IsLocal)
                {
                    SteamFriends.SetPlayedWith(GetSteamID(player));
                }
            }
            catch (ArgumentNullException)
            {
                Logger!.LogInfo($"Player \"{player}\" did not have a steam ID set");
            }
            catch (FormatException e)
            {
                Logger!.LogError($"Player \"{player}\" had a malformed steam ID: {e}");
            }
            catch (OverflowException e)
            {
                Logger!.LogError($"Player \"{player}\" had a malformed steam ID: {e}");
            }
        }

        private static CSteamID GetSteamID(Player player)
        {
            return new CSteamID(ulong.Parse((string)player.CustomProperties["STEAMID"]));
        }
    }
}
