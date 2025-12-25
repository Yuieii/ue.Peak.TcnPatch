// Copyright (c) 2025 Yuieii.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;

namespace ue.Peak.TcnPatch.Patches
{
    // -- Harmony patch methods need special parameter names to do advanced stuffs like passing results or deciding
    //    whether to run original method after prefixes.
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch]
    public class LocalizedTextPatch
    {
        private static bool _autoDumpedMainTable;

        private static readonly Lazy<int> _languageCountLazy = new(() => Enum.GetNames(typeof(LanguageSetting.Language)).Length); 
        
        public static HashSet<string> VanillaLocalizationKeys { get; } = [];

        [HarmonyPatch(typeof(LocalizedText), nameof(LocalizedText.GetText), typeof(string), typeof(bool))]
        [HarmonyPrefix]
        private static void PatchGetText(string id, ref string __result, ref bool __runOriginal) 
            => PatchGetText(id, LocalizedText.CURRENT_LANGUAGE, ref __result, ref __runOriginal);

        [HarmonyPatch(typeof(LocalizedText), nameof(LocalizedText.GetText), typeof(string), typeof(LocalizedText.Language))]
        [HarmonyPrefix]
        private static void PatchGetText(string id, LocalizedText.Language language, ref string __result, ref bool __runOriginal)
        {
            // Don't do anything if the user is not using Traditional Chinese. 
            if (language != LocalizedText.Language.TraditionalChinese)
            {
                __runOriginal = true;
                return;
            }
            
            // First we search for registered localizations and see if we have a non-empty localization.
            // (e.g. additional translations and those which are registered from the API)
            var (runOriginal, res) = Plugin.GetRegistered(id, language)
                .Where(result => !string.IsNullOrEmpty(result.Trim()))
                // If we don't get a valid registered localizations, try searching by uppercase keys first.
                .OrGet(() => Plugin.GetRegistered(id.ToUpperInvariant(), language))
                .Where(result => !string.IsNullOrEmpty(result.Trim()))
                // If we still don't get a valid registered localizations, we then search from locally-stored vanilla
                // localizations and see if we have a non-empty localization.
                .OrGet(() => Plugin.GetVanilla(id))
                .Where(result => !string.IsNullOrEmpty(result.Trim()))
                // If we have a valid result, we do early return. (don't run original)
                .Select(result => (false, result))
                // If we don't get a valid custom localizations for vanilla texts, we pass to the original method,
                // and then we get a fallback from PEAK itself.
                .OrElse((true, null));

            __runOriginal = runOriginal;
            __result = res;

            if (!__runOriginal) return;
            
            // Try to fix inconsistent localization entries here, thanks to those *smart* modders who register their
            // localization entries with a single element list. (thumbs up)
            // -- To those who are curious about why this concerns:
            // This is an error when the game is set to non-English. It spams errors and the text ends up completely empty.
            // 你們他媽看不懂這是 "Localized" Text 嗎？
            if (!LocalizedText.mainTable.TryGetValue(id.ToUpperInvariant(), out var list)) return;
            
            while (list.Count < _languageCountLazy.Value)
            {
                list.Add(string.Empty);
            }
        }
    
        [HarmonyPatch(typeof(LocalizedText), nameof(LocalizedText.LoadMainTable))]
        [HarmonyPriority(Priority.First)]
        [HarmonyPostfix]
        private static void PatchLoadMainTableFirstChance()
        {
            if (VanillaLocalizationKeys.Count > 0)
            {
                foreach (var key in LocalizedText.mainTable.Keys
                             .Where(key => !VanillaLocalizationKeys.Contains(key)))
                {
                    LocalizedText.mainTable.Remove(key);
                }

                return;
            }
        
            foreach (var key in LocalizedText.mainTable.Keys)
            {
                VanillaLocalizationKeys.Add(key);
            }
        }

        [HarmonyPatch(typeof(LocalizedText), nameof(LocalizedText.LoadMainTable))]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPostfix]
        private static void PatchLoadMainTable()
        {
            if (_autoDumpedMainTable) return;
            _autoDumpedMainTable = true;

            if (!Plugin.ModConfig.EnableAutoDumpLanguage.Value) return;
        
            DumpLanguageEntries();
        }

        internal static void DumpLanguageEntries()
        {
            Plugin.Logger.LogInfo($"正在自動輸出翻譯表至 _Auto{Plugin.TcnTranslationFileName}...");
        
            var dir = Path.Combine(Paths.ConfigPath, Plugin.ModGuid);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var path =  Path.Combine(dir, "_Auto" + Plugin.TcnTranslationFileName);
            var language = Plugin.ModConfig.AutoDumpLanguage.Value;
            var table = LocalizedText.mainTable
                .Where(p => VanillaLocalizationKeys.Contains(p.Key))
                .ToDictionary(
                    p => p.Key,
                    p => p.Value[(int) language]
                );
        
            var additionalTable = LocalizedText.mainTable
                .Where(p => !VanillaLocalizationKeys.Contains(p.Key))
                .ToDictionary(
                    p => p.Key,
                    p => p.Value[(int) language]
                );
        
            foreach (var (key, value) in Plugin.KeyToUnlocalizedLookup)
            {
                additionalTable[key] = value;
            }
        
            var json = JsonConvert.SerializeObject(
                new AutoDumpRecord(table, additionalTable), 
                Formatting.Indented
            );
        
            File.WriteAllText(path, json);
        }

        private class AutoDumpRecord(Dictionary<string, string> translations, Dictionary<string, string> additionalTranslations)
        {
            public int FormatVersion { get; } = TranslationFile.CurrentFormatVersion;
            public List<string> Authors { get; } = [];
            public Dictionary<string, string> Translations { get; } = translations;
            public Dictionary<string, string> AdditionalTranslations { get; } = additionalTranslations;
        }
    }
}