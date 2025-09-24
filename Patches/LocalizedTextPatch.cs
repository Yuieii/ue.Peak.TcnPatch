// Copyright (c) 2025 Yuieii.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;

namespace ue.Peak.TcnPatch.Patches;

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
        if (Plugin.TryGetRegistered(id, language, out var result))
        {
            __runOriginal = false;
            __result = result;
            return;
        }
        
        if (language != LocalizedText.Language.TraditionalChinese) return;
        
        if (Plugin.TryGetVanilla(id, out result) && !string.IsNullOrEmpty(result))
        {
            __runOriginal = false;
            __result = result;
            return;
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
        if (_writtenMainTable) return;
        _writtenMainTable = true;
        
        var dir = Path.Combine(Paths.ConfigPath, Plugin.ModGuid);
        Directory.CreateDirectory(dir);
        
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
        
        foreach (var (key, value) in Plugin.RegisteredOrigTable)
        {
            additionalTable[key] = value;
        }
        
        var json = JsonConvert.SerializeObject(
            new AutoDumpRecord(table, additionalTable), 
            Formatting.Indented
        );
        
        File.WriteAllText(path, json);

        Plugin.EmptyTranslationFile = new TranslationFile();
        
        foreach (var key in LocalizedText.mainTable.Keys)
        {
            if (VanillaLocalizationKeys.Contains(key))
            {
                Plugin.EmptyTranslationFile.Translations[key] = "";
            }
            else
            {
                Plugin.EmptyTranslationFile.AdditionalTranslations[key] = "";
            }
        }
    }

    private class AutoDumpRecord(Dictionary<string, string> translations, Dictionary<string, string> additionalTranslations)
    {
        public int FormatVersion { get; } = TranslationFile.CurrentFormatVersion;
        public List<string> Authors { get; } = [];
        public Dictionary<string, string> Translations { get; } = translations;
        public Dictionary<string, string> AdditionalTranslations { get; } = additionalTranslations;
    }
}