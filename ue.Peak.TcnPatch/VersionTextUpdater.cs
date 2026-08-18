// Copyright (c) 2026 Yuieii.

using System;
using JetBrains.Annotations;
using UnityEngine;

namespace ue.Peak.TcnPatch
{
    public class VersionTextUpdater
    {
        [CanBeNull]
        private string _originalText;

        public void Update(VersionString instance)
        {
            // We only want to show this when our language is Traditional Chinese
            if (LocalizedText.CURRENT_LANGUAGE != LocalizedText.Language.TraditionalChinese) return;
            
            // Just in case the field is missing in a future release of the game (unlikely but why not)
            var textField = Refl.VersionString.Text;
            if (textField == null) return;
            
            var text = instance.GetReflectionFieldValue(textField);
            
            if (_originalText == null)
            {
                _originalText = text.text;
            }
            
            text.text = _originalText;
            
            // We only want to show this when this is not explicitly disabled
            if (!Plugin.ModConfig.ShowPatchCredit.Value) return;

            // We only want to show this when we are in the main menu
            var translatorText = $"繁中翻譯by: {string.Join("、", Plugin.CurrentTranslationFile.Authors)}";
            var ueText = Plugin.ModConfig.ShowModVersionInPatchCredit.Value 
                ? $"繁中支援v{Plugin.ModVersion} by悠依"
                : "繁中支援by悠依";

            var showTranslator = Plugin.ModConfig.ShowTranslatorCredit.Value &&
                                 Plugin.CurrentTranslationFile.Authors.Count > 0;
        
            var parentName = instance.transform.GetParent().gameObject.name;
            var objectName = instance.gameObject.name;
            
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