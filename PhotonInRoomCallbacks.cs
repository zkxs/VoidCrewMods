// This file is part of RecentPlayers and is licensed under the MIT License.
// See LICENSE file for full text.
// Copyright © 2024 Michael Ripley

using ExitGames.Client.Photon;
using Photon.Realtime;

namespace RecentPlayers
{
    internal class PhotonInRoomCallbacks : IInRoomCallbacks
    {
        public void OnMasterClientSwitched(Player newMasterClient)
        {
        }

        /// <summary>
        /// Called when a remote player entered the room. This Player is already added to the playerlist.
        ///
        /// If your game starts with a certain number of players, this callback can be useful to check the Room.playerCount and find out if you can start.
        ///
        /// Implemented in SupportLogger, PhotonHandler, MonoBehaviourPunCallbacks, PlayerNumbering, and PunTeams.
        /// </summary>
        /// <param name="newPlayer"></param>
        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            RecentPlayers.SetPlayedWith(newPlayer);
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
        }
    }
}
