// This file is part of RecentPlayers and is licensed under the MIT License.
// See LICENSE file for full text.
// Copyright © 2024 Michael Ripley

using System.Collections.Generic;
using Photon.Realtime;

namespace RecentPlayers
{
    internal class PhotonMatchmakingCallbacks : IMatchmakingCallbacks
    {
        public void OnCreatedRoom()
        {
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
        }

        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
        }

        /// <summary>
        /// Called when the LoadBalancingClient entered a room, no matter if this client created it or simply joined.
        ///
        /// When this is called, you can access the existing players in Room.Players, their custom properties and Room.CustomProperties.
        ///
        /// In this callback, you could create player objects. For example in Unity, instantiate a prefab for the player.
        ///
        /// If you want a match to be started "actively", enable the user to signal "ready" (using OpRaiseEvent or a Custom Property).
        ///
        /// Implemented in MatchMakingCallbacksContainer, SupportLogger, PhotonHandler, MonoBehaviourPunCallbacks, OnJoinedInstantiate, ConnectAndJoinRandom, PlayerNumbering, and PunTeams.
        /// </summary>
        public void OnJoinedRoom()
        {
            RecentPlayers.SetLobbyPlayedWith();
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
        }

        public void OnLeftRoom()
        {
        }
    }
}
