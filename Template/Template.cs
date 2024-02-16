// This file is part of Template and is licensed under the MIT License.
// See LICENSE file for full text.
// Copyright © 2024 Michael Ripley

using System;
using BepInEx;
using BepInEx.Logging;

namespace Template
{
    [BepInPlugin(GUID, MOD_NAME, MOD_VERSION)]
    public class Template : BaseUnityPlugin
    {
        internal const string GUID = "dev.zkxs.voidcrew.template";
        internal const string MOD_NAME = "Template";
        internal const string MOD_VERSION = "1.0.0";

        internal static new ManualLogSource? Logger;

        private void Awake()
        {
            try
            {
                Logger = base.Logger; // this lets us access the logger from static contexts later: namely our patches
                Logger.LogInfo("Hello, world!"); // Nothing broke. Emit a log to indicate this.
            }
            catch (Exception e)
            {
                base.Logger.LogError($"Something has gone terribly wrong:\n{e}");
                throw e;
            }
        }
    }
}
