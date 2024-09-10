// This file is part of SaveLocally and is licensed under the MIT License.
// See LICENSE file for full text.
// Copyright © 2024 Michael Ripley

using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using CG.Cloud;
using CG.Profile;
using HarmonyLib;

namespace SaveLocally
{
    [BepInPlugin(GUID, MOD_NAME, MOD_VERSION)]
    public class SaveLocally : BaseUnityPlugin
    {
        internal const string GUID = "dev.zkxs.voidcrew.SaveLocally";
        internal const string MOD_NAME = "SaveLocally";
        internal const string MOD_VERSION = "1.0.0";

        internal static new ManualLogSource? Logger;

        private void Awake()
        {
            try
            {
                Logger = base.Logger; // this lets us access the logger from static contexts later: namely our patches
                Harmony harmony = new Harmony(GUID);
                harmony.PatchAll();
                Logger.LogInfo("successfully installed patches!"); // Nothing broke. Emit a log to indicate this.
            }
            catch (Exception e)
            {
                base.Logger.LogError($"Something has gone terribly wrong:\n{e}");
                throw e;
            }
        }

        [HarmonyPatch]
        private static class HarmonyPatches
        {
            // vanilla implmentation only saves UNSEEN.json
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CloudLocalProfile), "OnQuit")]
            private static bool CloudLocalProfileOnQuit(CloudLocalProfile __instance)
            {
                if (PlayerProfile.Instance != null)
                {
                    try
                    {
                        PlayerProfileLocalSave.SaveProfile(__instance);
                    }
                    catch (Exception e)
                    {
                        Logger!.LogError($"Error persisting profile:\n{e}");
                    }
                }
                return false; // skip original method
            }

            // vanilla implmentation throws NotImplementedException
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CloudShipLoadoutSync), nameof(CloudShipLoadoutSync.Loadouts), MethodType.Getter)]
            private static bool CloudShipLoadoutSyncLoadoutsGetter(ref IReadOnlyList<IShipLoadoutProfile> __result, CloudShipLoadoutSync __instance)
            {
                IShipLoadoutController shipLoadout = (IShipLoadoutController)AccessTools.DeclaredProperty(typeof(ChainOfResponsibility<IShipLoadoutController>), "source").GetValue(__instance);
                __result = shipLoadout.Loadouts;
                return false; // skip original method
            }

        }
    }
}
