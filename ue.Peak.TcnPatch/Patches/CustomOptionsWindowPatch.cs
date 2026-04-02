// Copyright (c) 2026 Yuieii.

using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace ue.Peak.TcnPatch.Patches
{
    // -- Harmony patch methods need special parameter names to do advanced stuffs like passing results or deciding
    //    whether to run original method after prefixes.
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch]
    public class CustomOptionsWindowPatch
    {
        [HarmonyPatch(typeof(CustomOptionsWindow), "Initialize")]
        [HarmonyPostfix]
        private static void PatchTitle(CustomOptionsWindow __instance)
        {
            __instance.transform
                .Find("CustomOptions/Panel/Title")
                .ToOptionUnity()
                .IfNone(() =>
                {
                    // - Might be moved somewhere...
                    Plugin.Logger.LogDebug("!!: Failed to find the title text in custom options UI");
                })
                .IfSome(transform =>
                {
                    var localized = transform.gameObject.GetComponent<LocalizedText>();
                    if (localized == null)
                    {
                        localized = transform.gameObject.AddComponent<LocalizedText>();
                        
                        // See below
                        localized.autoSet = false;
                    }
                    
                    // We currently use this to determine if it changes to a (really) localizable text
                    if (localized.autoSet) return;
                            
                    localized.index = "PeakTcnPatch.BoardingPass.CustomExpedition";
                    localized.autoSet = true;
                    localized.RefreshText();
                });
        }
    }
}