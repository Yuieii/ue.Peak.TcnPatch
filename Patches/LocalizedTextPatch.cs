// Copyright (c) 2025 Yuieii.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;

namespace ue.Peak.TcnPatch.Patches
{
    [HarmonyPatch]
    public class LocalizedTextPatch
    {
        private static bool _writtenMainTable;

        public static HashSet<string> VanillaLocalizationKeys { get; } = [];

        [HarmonyPatch(typeof(LocalizedText), nameof(LocalizedText.GetText), typeof(string), typeof(bool))]
        [HarmonyPrefix]
        private static void PatchGetText(string id, ref string __result, ref bool __runOriginal) 
            => PatchGetText(id, LocalizedText.CURRENT_LANGUAGE, ref __result, ref __runOriginal);

        [HarmonyPatch(typeof(LocalizedText), nameof(LocalizedText.GetText), typeof(string), typeof(LocalizedText.Language))]
        [HarmonyPrefix]
        private static void PatchGetText(string id, LocalizedText.Language language, ref string __result, ref bool __runOriginal)
        {
            var (runOriginal, res) = Plugin.GetRegistered(id, language)
                .Where(result => !string.IsNullOrEmpty(result))
                .OrGet(() =>
                {
                    if (language != LocalizedText.Language.TraditionalChinese)
                        return Option.None;

                    return Plugin.GetVanilla(id)
                        .Where(result => !string.IsNullOrEmpty(result));
                })
                .Select(result => (false, result))
                .OrElse((true, null));

            __runOriginal = runOriginal;
            __result = res;
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
            if (_writtenMainTable) return;
            _writtenMainTable = true;

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