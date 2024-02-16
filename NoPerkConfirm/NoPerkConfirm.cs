// This file is part of NoPerkConfirm and is licensed under the MIT License.
// See LICENSE file for full text.
// Copyright © 2024 Michael Ripley

using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Gameplay.Perks;
using HarmonyLib;
using UI.Token;

namespace NoPerkConfirm
{
    [BepInPlugin(GUID, MOD_NAME, MOD_VERSION)]
    public class NoPerkConfirm : BaseUnityPlugin
    {
        internal const string GUID = "dev.zkxs.voidcrew.noperkconfirm";
        internal const string MOD_NAME = "NoPerkConfirm";
        internal const string MOD_VERSION = "1.0.0";

        internal static new ManualLogSource? Logger;

        private static ConfigEntry<bool>? modEnabled;

        private void Awake()
        {
            try
            {
                Logger = base.Logger; // this lets us access the logger from static contexts later: namely our patches

                modEnabled = Config.Bind("General", "Enable", true, "Enable skipping the perk confirmation popup");

                Harmony harmony = new Harmony(GUID);

                // I have no idea why the game creates LoadBalancingClients for like 6 different MatchmakingHandlers, but whatever, we'll hook them just in case
                harmony.Patch(
                    AccessTools.DeclaredMethod(typeof(PerkBuffTreeVE), "TryPurchase"),
                    prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.PerkBuffTryPurchase))));

                Logger.LogInfo("successfully installed patches!"); // Nothing broke. Emit a log to indicate this.
            }
            catch (Exception e)
            {
                base.Logger.LogError($"Something has gone terribly wrong:\n{e}");
                throw e;
            }
        }

        // the methods within this class are not called normally: they're harmony patches
        private static class HarmonyPatches
        {
            // prefix for PerkBuffTreeVE.TryPurchase()
            internal static bool PerkBuffTryPurchase(PerkBuff buff, TokenTerminalEvents ___events)
            {
                if (!modEnabled!.Value)
                {
                    return true; // run original method
                }

                if (!buff.CanAfford() || !buff.RequirementsMet() || buff.Completed())
                {
                    return false; // skip original method
                }
                ___events.TryBuffUpgrade(buff);
                return false; // skip original method
            }
        }
    }
}
