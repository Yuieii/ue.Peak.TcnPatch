// Copyright (c) 2025 Yuieii.

using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace ue.Peak.TcnPatch.Patches
{
    // -- Harmony patch methods need special parameter names to do advanced stuffs like passing results or deciding
    //    whether to run original method after prefixes.
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch]
    public class PassportManagerPatch
    {
        [HarmonyPatch(typeof(PassportManager), "Awake")]
        [HarmonyPostfix]
        private static void PatchCrabland(PassportManager __instance)
        {
            __instance.transform
                .Find("PassportUI/Canvas/Panel/Panel/BG/Text/Nationality/Text")
                .ToOptionUnity()
                .IfNone(() =>
                {
                    // - Might be moved somewhere...
                    Plugin.Logger.LogDebug("!!: Failed to find the Crabland text in passport UI");
                })
                .IfSome(transform =>
                {
                    var localized = transform.gameObject.AddComponent<LocalizedText>();
                    localized.index = "PeakTcnPatch.Passport.Crabland";
                    localized.RefreshText();
                });
        }
    }
}