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

        var api = API.TcnPatch.InternalInstance;
        api.InsertExistingLocalizations();
        api.CanInsertOnRegister = true;
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

        var table = LocalizedText.mainTable
            .Where(p => VanillaLocalizationKeys.Contains(p.Key))
            .ToDictionary(
                p => p.Key,
                p => p.Value[(int) Plugin.ModConfig.AutoDumpLanguage.Value]
            );
        
        var additionalTable = LocalizedText.mainTable
            .Where(p => !VanillaLocalizationKeys.Contains(p.Key))
            .ToDictionary(
                p => API.TcnPatch.InternalInstance.ReplaceAsRegisteredCase(p.Key),
                p => p.Value[(int) Plugin.ModConfig.AutoDumpLanguage.Value]
            );
        
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
        public Dictionary<string, string> Translations { get; } = translations;
        public Dictionary<string, string> AdditionalTranslations { get; } = additionalTranslations;
    }
}