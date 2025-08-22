// Copyright (c) 2025 Yuieii.

using HarmonyLib;

namespace ue.Peak.TcnPatch.Patches;

[HarmonyPatch]
public class PassportManagerPatch
{
    [HarmonyPatch(typeof(PassportManager), "Awake")]
    [HarmonyPostfix]
    private static void PatchCrabland(PassportManager __instance)
    {
        var transform = __instance.transform.Find("PassportUI/Canvas/Panel/Panel/BG/Text/Nationality/Text");
        if (transform == null)
        {
            // - Might be moved somewhere...
            Plugin.Logger.LogDebug("!!: Failed to find the Crabland text in passport UI");
            return;
        }
        
        var localized = transform.gameObject.AddComponent<LocalizedText>();
        localized.index = "PeakTcnPatch.Passport.Crabland";
        localized.RefreshText();
    }
}