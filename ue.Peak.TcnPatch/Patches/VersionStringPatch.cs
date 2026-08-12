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
            if (Refl.VersionString.Text == null)
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
            
                var textField = Refl.VersionString.Text;
                var text = __instance.GetReflectionFieldValue(textField);
                text.verticalAlignment = VerticalAlignmentOptions.Top;
            }

            Plugin.VersionStringInstance = __instance;
        }
    }
}