// Copyright (c) 2025 Yuieii.

using HarmonyLib;

namespace ue.Peak.TcnPatch.Patches
{
    [HarmonyPatch]
    public class GUIManagerPatch
    {
        [HarmonyPatch(typeof(GUIManager), "Awake")]
        [HarmonyPostfix]
        private static void PatchPrompt(GUIManager __instance)
        {
            var g = __instance.strugglePrompt;
            if (!g)
            {
                LogNotFound();
                return;
            }
            
            var c = g.transform.Find("PromptText");
            if (!c)
            {
                LogNotFound();
                return;
            }

            var text = c.GetComponent<LocalizedText>();
            if (!text)
            {
                LogNotFound();
                return;
            }
            
            // The "autoSet" is not set to true, therefore the text becomes unlocalized,
            // even a localized text for this prompt exists.
            text.autoSet = true;
            text.RefreshText();
            void LogNotFound()
            {
                // - Might be moved somewhere...
                Plugin.Logger.LogDebug("!!: Failed to find the text in the Struggle prompt UI");
            }
        }
    }
}