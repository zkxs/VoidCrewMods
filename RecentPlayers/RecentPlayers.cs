// This file is part of RecentPlayers and is licensed under the MIT License.
// See LICENSE file for full text.
// Copyright © 2024 Michael Ripley

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Timers;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CG.Profile;
using ExitGames.Client.Photon;
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
        internal const string MOD_VERSION = "1.1.1";

        private const double TIMER_INTERVAL_MS = 1000 * 60 * 2; // two minutes

        internal static new ManualLogSource? Logger;

        private static int initialized = 0;

        // used to remember which LoadBalancingClients we've registered callbacks on already
        private static ConditionalWeakTable<LoadBalancingClient, object?> hookedLoadBalancingClients = new();

        // players who's STEAM_ID property is malformed
        private static ConditionalWeakTable<Player, object?> naughtyPlayers = new();

        private static System.Timers.Timer? timer = null;

        private static ConfigEntry<bool>? runTimer;
        private static ConfigEntry<bool>? debugLogs;

        private void Awake()
        {
            try
            {
                Logger = base.Logger; // this lets us access the logger from static contexts later: namely our patches.

                runTimer = Config.Bind("General", "Timer", true, "Periodically resend the played-with-user event to Steam. Without this, the event is only sent on lobby join.");
                debugLogs = Config.Bind("Debug", "Debug Logs", true, "Emit LogLevel.Debug logs. In the default BepInEx.cfg these go nowhere. If you have BepInEx debug logging enabled and this mod's logs are bothering you, set this config to false.");

                Harmony harmony = new Harmony(GUID);

                // I have no idea why the game creates LoadBalancingClients for like 6 different MatchmakingHandlers, but whatever, we'll hook them just in case
                harmony.Patch(
                    AccessTools.DeclaredMethod(typeof(MatchmakingHandler), "Awake"),
                    postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.MatchmakingHandlerAwake))));

                // I believe this is the main LoadBalancingClient that's actually being used by Photon.
                // For some reason if I register callbacks too early in the game initialization, it breaks Photon... so I only register callbacks
                // after the game connects to Photon master. That's what's being done by this patch.
                harmony.Patch(
                    GetAsyncMethodBody(AccessTools.DeclaredMethod(typeof(PhotonService), nameof(PhotonService.Connect))),
                    postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.AddCallbacksOnce))));

                // there's a bug in this code in vanilla that sets your SteamID to your Unity Cloud ID, which is only SOMETIMES your SteamID.
                // fixing the bug shouldn't cause any problems.
                harmony.Patch(
                    AccessTools.DeclaredMethod(typeof(PhotonService), "SetSteamPlayerProperties"),
                    postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.SetSteamPlayerProperties))));

                Logger.LogInfo("successfully installed patches!"); // Nothing broke. Emit a log to indicate this.
            }
            catch (Exception e)
            {
                base.Logger.LogError($"Something has gone terribly wrong:\n{e}");
                throw e;
            }
        }

        private static void LogDebug(Func<string> messageFunc)
        {
            if (debugLogs!.Value)
            {
                Logger!.LogDebug(messageFunc.Invoke());
            }
        }

        private static void SetTimer()
        {
            timer = new(TIMER_INTERVAL_MS);
            timer.Elapsed += OnTimerEvent;
            timer.Start();
        }

        private static void OnTimerEvent(object source, ElapsedEventArgs eventArgs)
        {
            if (runTimer!.Value)
            {
                SetLobbyPlayedWith();
            }
        }

        public static void SetLobbyPlayedWith()
        {
            var players = PhotonNetwork.CurrentRoom?.Players?.Values;
            if (players != null)
            {
                foreach (Player player in players)
                {
                    SetPlayedWith(player);
                }
            }
        }

        public static void SetPlayedWith(Player player)
        {
            if (!player.IsLocal)
            {
                // if we already have tried STEAM_ID for this player, don't ever try it again. This prevents logspam.
                if (!naughtyPlayers.TryGetValue(player, out _))
                {
                    try
                    {
                        SteamFriends.SetPlayedWith(GetSteamID(player));
                        LogDebug(() => $"SetPlayedWith({player})");
                        return;
                    }
                    catch (SteamIdException e1)
                    {
                        naughtyPlayers.Add(player, null);
                        LogDebug(() => $"Could not get SteamID for \"{player}\" from primary source, which is expected as the primary source is known to be bugged. Will try again with fallback source. {e1}");
                    }
                }

                // if the player has a malformed STEAM_ID, we will try getting ID from fallback location
                try
                {
                    SteamFriends.SetPlayedWith(GetSteamIDFallback(player));
                    LogDebug(() => $"SetPlayedWith({player})");
                }
                catch (SteamIdException e2)
                {
                    Logger!.LogWarning($"Could not get SteamID for \"{player}\". {e2}");
                }
            }
        }

        // get steam ID from: player.CustomProperties["STEAM_ID"]
        // where "STEAM_ID" is defined intCloneStarConstants.STEAM_ID
        // set in PhotonService.SetSteamPlayerProperties()
        // used in NameIconRankDisplayer.Internal_SetRemotePlayerSteamInfo()
        // ... except this isn't steam ID, it's AuthenticationService.Instance.PlayerId, aka Unity Cloud ID. This appears to be a developer oversight, and is probably breaking the player icon cache.
        private static CSteamID GetSteamID(Player player)
        {
            object steamidObject = player.CustomProperties?[CloneStarConstants.STEAM_ID] ?? throw new SteamIdException($"unset");
            try
            {
                CSteamID steamId = new(ulong.Parse((string)steamidObject));
                if (!steamId.BIndividualAccount())
                {
                    throw new SteamIdException($"\"{steamidObject}\" is not an individual account");
                }
                return steamId;
            }
            catch (InvalidCastException e)
            {
                throw new SteamIdException($"\"{steamidObject}\" is not a string", e);
            }
            catch (FormatException e)
            {
                throw new SteamIdException($"\"{steamidObject}\" could not be parsed", e);
            }
            catch (OverflowException e)
            {
                throw new SteamIdException($"\"{steamidObject}\" overflows", e);
            }
        }

        // get steam ID from: player.UserId. No idea why this is SteamID, but it consistently is.
        private static CSteamID GetSteamIDFallback(Player player)
        {
            object steamidObject = player?.UserId ?? throw new SteamIdException($"unset");
            try
            {
                return new CSteamID(ulong.Parse((string)steamidObject));
            }
            catch (InvalidCastException e)
            {
                throw new SteamIdException($"\"{steamidObject}\" is not a string", e);
            }
            catch (FormatException e)
            {
                throw new SteamIdException($"\"{steamidObject}\" could not be parsed", e);
            }
            catch (OverflowException e)
            {
                throw new SteamIdException($"\"{steamidObject}\" overflows", e);
            }
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

        private static void AddLoadBalancingClientCallbacks(LoadBalancingClient client)
        {
            bool existingMapping = true;
            hookedLoadBalancingClients.GetValue(client, client =>
            {
                existingMapping = false;
                try
                {
                    client.AddCallbackTarget(new PhotonInRoomCallbacks());
                    client.AddCallbackTarget(new PhotonMatchmakingCallbacks());
                }
                catch (Exception e)
                {
                    // handle all exceptions because I don't trust Photon's API at all
                    Logger!.LogError($"Some mystery error happened when adding photon callbacks: {e}");
                }
                return null; // in practice ConditionalWeakTable doesn't care if the value is null, so just squelch this warning
            });

            if (existingMapping)
            {
                LogDebug(() => "We encountered a LoadBalancingClient more than once. This is unexpected, but handled.");
            }
        }

        // the methods within this class are not called normally: they're harmony patches
        private static class HarmonyPatches
        {
            // postifx for MatchmakingHandler.Awake()
            // as far as I know this isn't used
            internal static void MatchmakingHandlerAwake(LoadBalancingClient ___client)
            {
                AddLoadBalancingClientCallbacks(___client);
                LogDebug(() => "Hooked MatchmakingHandler.Awake");
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
                        AddLoadBalancingClientCallbacks(PhotonNetwork.NetworkingClient);
                        SetTimer();
                        LogDebug(() => "Hooked PhotonService.Connect");
                    }
                }
            }

            // postfix for async PhotonService.SetSteamPlayerProperties()
            internal static void SetSteamPlayerProperties()
            {
                try
                {
                    object oldSteamId = PhotonNetwork.LocalPlayer.CustomProperties[CloneStarConstants.STEAM_ID];
                    PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { CloneStarConstants.STEAM_ID, PlayerProfile.GetSteamID() } });
                    LogDebug(() => $"Changed our STEAM_ID property from \"{oldSteamId}\" to \"{PlayerProfile.GetSteamID()}\". Note that our UNITY_CLOUD_ID is \"{PlayerProfile.GetUnityCloudID()}\" and our Photon UserId is \"{PhotonNetwork.LocalPlayer.UserId}\"");
                }
                catch (Exception e)
                {
                    // handle all exceptions because I don't trust Photon's API at all
                    Logger!.LogError($"Some mystery error happened when setting our SteamID: {e}");
                }
            }
        }
    }
}
