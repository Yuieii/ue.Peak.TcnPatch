// Copyright (c) 2026 Yuieii.

using System;
using System.Collections.Generic;
using System.Linq;
using ue.Peak.TcnPatch.Core;
using ue.Peak.TcnPatch.Patches;

namespace ue.Peak.TcnPatch
{
    public class TranslationStorage
    {
        private Dictionary<string, string> TranslationsLookup { get; } = new();

        private Dictionary<string, string> AdditionalTranslationsLookup { get; } = new();

        // Registered from API, contains unlocalized texts
        private Dictionary<string, string> KeyToUnlocalizedLookup { get; } = new();

        public void InvalidateTranslations()
        {
            TranslationsLookup.Clear();
            AdditionalTranslationsLookup.Clear();
        }
        
        public void RegisterVanillaLocalizationKey(string key, string translation)
        {
            var upper = key.ToUpperInvariant();

            if (TranslationsLookup.ContainsKey(upper))
            {
                Plugin.Logger.LogInfo($"發現重複的翻譯key：「{key}」！已存在大寫的同名key！");
                return;
            }

            var mainTable = LocalizedText.mainTable;
            if (!mainTable.ContainsKey(upper))
            {
                if (Plugin.ModConfig.WarnUnknownTranslationKeys.Value)
                {
                    Plugin.Logger.LogWarning($"正在使用未知的翻譯key：「{upper}」！");
                }
            }
            
            TranslationsLookup[upper] = translation;
        }
        
        public void RegisterAdditionalLocalizationKey(string key, string translation)
        {
            AdditionalTranslationsLookup[key] = translation;
        }
        
        internal void RegisterExternalLocalizationKey(string key, string unlocalized)
        {
            KeyToUnlocalizedLookup[key] = unlocalized;
        }

        public void VisitExternalLocalizationKeys(Action<string, string> handler)
        {
            foreach (var (key, value) in KeyToUnlocalizedLookup)
            {
                handler(key, value);
            }
        }

        public void ImportFrom(TranslationFile file)
        {
            // Intentionally get main table before cleaning local lookups.
            // LocalizedText.mainTable is lazily loaded with LocalizedText.LoadMainTable().
            var mainTable = LocalizedText.mainTable;
            var keys = mainTable.Keys.ToHashSet();
            
            InvalidateTranslations();
            
            foreach (var (key, value) in file.Translations)
            {
                RegisterVanillaLocalizationKey(key, value);
                keys.Remove(key.ToUpperInvariant());
            }

            foreach (var (key, value) in file.AdditionalTranslations)
            {
                RegisterAdditionalLocalizationKey(key, value);
                keys.Remove(key);
            }

            foreach (var key in file.IgnoredTranslations)
            {
                keys.Remove(key.ToUpperInvariant());
                keys.Remove(key);
            }

            var vanillaKeys = LocalizedTextPatch.VanillaLocalizationKeys;

            foreach (var missing in keys)
            {
                if (vanillaKeys.Contains(missing))
                {
                    Plugin.Logger.LogWarning($"缺少「{missing}」翻譯key，請更新翻譯資料！");
                }
                else if (Plugin.ModConfig.WarnMissingAdditionalKeys.Value)
                {
                    Plugin.Logger.LogWarning($"*附加翻譯* 缺少「{missing}」翻譯key！");
                }
            }

            // Perform a force refresh on all localizable text
            LocalizedText.RefreshAllText();
        }
        
        public Option<string> GetVanilla(string id)
        {
            if (Plugin.ModConfig.IgnoreAllTranslations.Value)
            {
                return Option<string>.None;
            }
            
            return TranslationsLookup.GetOptional(id.ToUpperInvariant());
        }

        public Option<string> GetRegistered(string id, LocalizedText.Language? language)
        {
            if (Plugin.ModConfig.IgnoreAllTranslations.Value)
            {
                return Option<string>.None;
            }
            
            language ??= LocalizedText.CURRENT_LANGUAGE;

            var result = language == LocalizedText.Language.TraditionalChinese
                ? AdditionalTranslationsLookup.GetOptional(id)
                : Option<string>.None;

            return result.OrGet(() => KeyToUnlocalizedLookup.GetOptional(id));
        }
    }
}