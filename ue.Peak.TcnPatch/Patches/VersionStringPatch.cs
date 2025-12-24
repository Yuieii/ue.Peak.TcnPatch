// Copyright (c) 2025 Yuieii.

using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ue.Peak.TcnPatch.Patches
{
    // -- Harmony patch methods need special parameter names to do advanced stuffs like passing results or deciding
    //    whether to run original method after prefixes.
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch]
    public class VersionStringPatch
    {
        private static bool _versionTextMissingWarned;

        [HarmonyPatch(typeof(VersionString), "Start")]
        [HarmonyPostfix]
        private static void PatchVersionStringWarnOnStart(VersionString __instance)
        {
            // Just in case the field is missing in a future release of the game (unlikely but why not)
            if (ReflectionMembers.Fields.VersionStringText == null)
            {
                if (_versionTextMissingWarned) return;
            
                _versionTextMissingWarned = true;

                // Log a warning so we know what is happening when we don't see our credit text
                Plugin.Logger.LogWarning("VersionString: 找不到版本資訊的 m_text 欄位！");
                return;
            }

            // I am attempting to fix the alignment by adjusting the anchored position relatively
            var parentName = __instance.transform.GetParent().gameObject.name;
            var objectName = __instance.gameObject.name;
            if (objectName == "Version" && parentName == "MainPage")
            {
                var rect = __instance.GetComponent<RectTransform>();
                var anchored = rect.anchoredPosition;
                anchored.y -= 10;
                rect.anchoredPosition = anchored;
            
                var textField = ReflectionMembers.Fields.VersionStringText;
                var text = __instance.GetReflectionFieldValue(textField);
                text.verticalAlignment = VerticalAlignmentOptions.Top;
            }
        }
    
        [HarmonyPatch(typeof(VersionString), "Update")]
        [HarmonyPostfix]
        private static void PatchVersionString(VersionString __instance)
        {
            // We only want to show this when our language is Traditional Chinese
            if (LocalizedText.CURRENT_LANGUAGE != LocalizedText.Language.TraditionalChinese) return;
        
            // We only want to show this when this is not explicitly disabled
            if (!Plugin.ModConfig.ShowPatchCredit.Value) return;
        
            // Just in case the field is missing in a future release of the game (unlikely but why not)
            var textField = ReflectionMembers.Fields.VersionStringText;
            if (textField == null) return;
        
            var text = __instance.GetReflectionFieldValue(textField);

            // We only want to show this when we are in the main menu
            var translatorText = $"繁中翻譯by: {string.Join("、", Plugin.CurrentTranslationFile.Authors)}";
            var ueText = Plugin.ModConfig.ShowModVersionInPatchCredit.Value 
                ? $"繁中支援v{Plugin.ModVersion} by悠依"
                : "繁中支援by悠依";

            var showTranslator = Plugin.ModConfig.ShowTranslatorCredit.Value &&
                                 Plugin.CurrentTranslationFile.Authors.Count > 0;
        
            var parentName = __instance.transform.GetParent().gameObject.name;
            var objectName = __instance.gameObject.name;
        
            // PEAK <v1.31.a
            // !!: Users should have the latest version installed!
            if (objectName == "Version" && parentName == "Logo")
            {
                const float switchDuration = 10.0f;
                showTranslator &= Math.Floor(Time.realtimeSinceStartup / switchDuration) % 2 != 0;
            
                var shownText = showTranslator ? translatorText : ueText;
                text.text += $"  ({shownText})";
                return;
            }
        
            // PEAK >=v.1.31.a
            // -- The version text has now moved to the top left!
            // -- We have more space to write information about the translation data and this mod
            if (parentName != "MainPage")
            {
                text.text += $"<br><size=70%><alpha=#88>{Plugin.ModName} v{Plugin.ModVersion}<alpha=#FF></size>";
                return;
            }

            // Main menu only
            text.text += $"<br><size=70%>{ueText}</size>";

            if (showTranslator)
            {
                text.text += $"<br><size=70%>{translatorText}</size>";
            }
        }
    }
}