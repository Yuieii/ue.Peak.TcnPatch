// Copyright (c) 2025 Yuieii.

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace ue.Peak.TcnPatch.Patches;

[HarmonyPatch]
public class LanguageSettingPatch
{
    [HarmonyPatch(typeof(LanguageSetting), nameof(LanguageSetting.GetCustomLocalizedChoices))]
    [HarmonyPostfix]
    private static void PatchLanguageChoices(LanguageSetting __instance, ref List<string> __result)
    {
        if (Plugin.HasOfficialTcn)
        {
            // Now (?) we have official Traditional Chinese.
            // The game should now interpret the language settings in its own way.
            return;
        }
        
        switch (Plugin.ModConfig.LanguagePatchMode.Value)
        {
            case LanguagePatchMode.ReplaceEnglish:
            case LanguagePatchMode.ReplaceSimplifiedChinese:
            {
                // We already replaced the language to Traditional Chinese in ValueToLanguage.
                // The localized choices will be automatically affected by that change.
                return;
            }
            
            case LanguagePatchMode.InsertAfterSimplifiedChinese:
            case LanguagePatchMode.Append:
            {
                // We already inserted our Traditional Chinese into ValueToLanguage.
                // We just need to add one more choice to the localized choices.
                var val = Enum.GetNames(typeof(LanguageSetting.Language)).Length;
                var choice = LocalizedText.GetText("CURRENT_LANGUAGE", __instance.ValueToLanguage(val));
                Plugin.Logger.LogInfo("正在修正設定中的語言清單...");
                __result.Add(choice);
                break;
            }
            
            case LanguagePatchMode.TraditionalChineseOnly:
            {
                // We already replaced the language to Traditional Chinese in ValueToLanguage.
                // We only have to limit the choices so we have Traditional Chinese only.
                __result = __result.Take(1).ToList();
                break;
            }
            
            default:
            {
                // Unknown case. Do nothing.
                return;
            }
        }
    }
    
    [HarmonyPatch(typeof(LanguageSetting), nameof(LanguageSetting.ValueToLanguage))]
    [HarmonyPostfix]
    private static void PatchValueToLanguage(int val, ref LocalizedText.Language __result)
    {
        if (Plugin.HasOfficialTcn)
        {
            // Now (?) we have official Traditional Chinese.
            // The game should now interpret the language settings in its own way.
            return;
        }
        
        switch (Plugin.ModConfig.LanguagePatchMode.Value)
        {
            case LanguagePatchMode.InsertAfterSimplifiedChinese:
            {
                // We put Traditional Chinese right after Simplified Chinese, which is how the enum is originally ordered.
                // !!: This is a breaking change for players using Japanese, Korean, Polish and Turkish.
                __result = (LocalizedText.Language)val;
                break;
            }
            
            case LanguagePatchMode.ReplaceEnglish:
            {
                if (val == (int)LanguageSetting.Language.English)
                {
                    __result = LocalizedText.Language.TraditionalChinese;
                }
                
                break;
            }
            
            case LanguagePatchMode.ReplaceSimplifiedChinese:
            {
                if (val == (int)LanguageSetting.Language.SimplifiedChinese)
                {
                    __result = LocalizedText.Language.TraditionalChinese;
                }
                
                break;
            }

            case LanguagePatchMode.Append:
            {
                var index = Enum.GetValues(typeof(LanguageSetting.Language)).Length;
                
                if (val == index)
                {
                    __result = LocalizedText.Language.TraditionalChinese;
                }

                break;
            }
            
            case LanguagePatchMode.TraditionalChineseOnly:
            {
                __result = LocalizedText.Language.TraditionalChinese;
                break;
            }

            default:
            {
                // Unknown case. Do nothing.
                return;
            }
        }
    }
}