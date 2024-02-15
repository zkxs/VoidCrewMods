// This file is part of RecentPlayers and is licensed under the MIT License.
// See LICENSE file for full text.
// Copyright © 2024 Michael Ripley

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
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

        private static int initialized = 0;

        private void Awake()
        {
            try
            {
                Logger = base.Logger; // this lets us access the logger from static contexts later: namely our patches.

                Harmony harmony = new Harmony(GUID);

                harmony.Patch(
                    AccessTools.DeclaredMethod(typeof(MatchmakingHandler), "Awake"),
                    postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.MatchmakingHandlerAwake))));

                harmony.Patch(
                    GetAsyncMethodBody(AccessTools.DeclaredMethod(typeof(PhotonService), nameof(PhotonService.Connect))),
                    postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.AddCallbacksOnce))));

                Logger.LogDebug("successfully installed patches!"); // Nothing broke. Emit a log to indicate this.
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
                    Logger!.LogDebug($"SetPlayedWith({player})");
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

        private static MethodInfo GetAsyncMethodBody(MethodInfo asyncMethod)
        {
            AsyncStateMachineAttribute asyncAttribute = asyncMethod.GetCustomAttribute<AsyncStateMachineAttribute>();
            if (asyncAttribute == null)
            {
                throw new ReflectionException($"Could not find AsyncStateMachine for {asyncMethod}");
            }
            Type asyncStateMachineType = asyncAttribute.StateMachineType;
            MethodInfo asyncMethodBody = AccessTools.DeclaredMethod(asyncStateMachineType, "MoveNext");
            if (asyncMethodBody == null)
            {
                throw new ReflectionException($"Could not find async method body for {asyncMethod}");
            }
            return asyncMethodBody;
        }

        // the methods within this class are not called normally: they're harmony patches
        private static class HarmonyPatches
        {
            // postifx for MatchmakingHandler.Awake()
            // as far as I know this isn't used
            internal static void MatchmakingHandlerAwake(LoadBalancingClient ___client)
            {
                ___client.AddCallbackTarget(new PhotonInRoomCallbacks());
                ___client.AddCallbackTarget(new PhotonMatchmakingCallbacks());
                Logger!.LogDebug("hooked MatchmakingHandlerAwake.Awake()");
            }

            // postfix for async PhotonService.Connect()
            internal static void AddCallbacksOnce()
            {
                if (PhotonNetwork.IsConnected)
                {
                    // atomically set `initialized` to 1 and return the previous value. This will only return `0` once.
                    if (Interlocked.CompareExchange(ref initialized, 1, 0) == 0)
                    {
                        // this code will only run once, even when called by multiple threads
                        PhotonNetwork.NetworkingClient.AddCallbackTarget(new PhotonInRoomCallbacks());
                        PhotonNetwork.NetworkingClient.AddCallbackTarget(new PhotonMatchmakingCallbacks());
                        Logger!.LogInfo($"successfully finished initializing!");
                    }
                }
            }
        }
    }
}
