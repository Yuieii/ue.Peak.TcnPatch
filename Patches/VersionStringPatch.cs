// Copyright (c) 2025 Yuieii.

using System;
using HarmonyLib;
using UnityEngine;

namespace ue.Peak.TcnPatch.Patches;

[HarmonyPatch]
public class VersionStringPatch
{
    private static bool _versionTextMissingWarned;

    [HarmonyPatch(typeof(VersionString), "Start")]
    [HarmonyPostfix]
    private static void PatchVersionStringWarnOnStart()
    {
        // Just in case the field is missing in a future release of the game (unlikely but why not)
        if (ReflectionMembers.Fields.VersionStringText != null || _versionTextMissingWarned) return;
        _versionTextMissingWarned = true;
            
        // Log a warning so we know what is happening when we don't see our credit text
        Plugin.Logger.LogWarning("VersionString: 找不到版本資訊的 m_text 欄位！");
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
        
        // We only want to show this when we are in the main menu
        var parentName = __instance.transform.GetParent().gameObject.name;
        var objectName = __instance.gameObject.name;
        if (parentName != "Logo" || objectName != "Version") return;

        var text = __instance.GetReflectionFieldValue(textField);

        const float switchDuration = 10.0f;
        var showTranslator = Plugin.ModConfig.ShowTranslatorCredit.Value &&
                             Plugin.CurrentTranslationFile.Authors.Count > 0 &&
                             Math.Floor(Time.realtimeSinceStartup / switchDuration) % 2 != 0;
        var translatorText = $"繁中翻譯by: {string.Join("、", Plugin.CurrentTranslationFile.Authors)}";
        var ueText = $"繁中支援v{Plugin.ModVersion} by悠依";
        
        var shownText = showTranslator ? translatorText : ueText;
        text.text += $"  ({shownText})";
    }
}