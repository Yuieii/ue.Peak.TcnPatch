using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using Zorro.Settings;

namespace ue.Peak.TcnPatch;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
        
    private static FileSystemWatcher _watcher;

    private const string TcnTranslationFileName = "TcnTranslations.json";

    private static bool _writtenMainTable;
    
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;

        Logger.LogInfo($"正在載入模組 - {MyPluginInfo.PLUGIN_GUID}");
        
        if (Enum.GetValues(typeof(LanguageSetting.Language))
            .Cast<int>()
            .Any(values => values == (int) LocalizedText.Language.TraditionalChinese))
        {
            Logger.LogWarning("看起來繁體中文已經在設定清單裡面了！將會停用本模組。");
            return;
        }
        
        
        Harmony.CreateAndPatchAll(typeof(Plugin));
        
        var dir = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_GUID);
        Directory.CreateDirectory(dir);
        
        _watcher = new FileSystemWatcher(dir, "*.json");

        _watcher.NotifyFilter = NotifyFilters.LastWrite;
        
        _watcher.Changed += (sender, args) =>
        {
            if (args.Name == TcnTranslationFileName)
            {
                Logger.LogInfo("正在更新遊戲內繁體中文翻譯資料...");
                UpdateMainTable();
            }
        };
        
        UpdateMainTable();
        
        _watcher.EnableRaisingEvents = true;
        
        Logger.LogInfo($"已載入模組 - {MyPluginInfo.PLUGIN_GUID}");
        Logger.LogInfo("  + 非官方繁體中文翻譯支援模組 -- by悠依");
    }

    private static void UpdateMainTable()
    {
        if (!TryReadFromJson(TcnTranslationFileName, out Dictionary<string, string> table, () => []))
            return;

        var mainTable = LocalizedText.mainTable;
        var keys = mainTable.Keys.ToHashSet();
        
        foreach (var (key, value) in table)
        {
            if (!mainTable.TryGetValue(key, out var list))
            {
                Logger.LogWarning($"已忽略未知的翻譯key：「{key}」！");
                continue;
            }

            const int index = (int)LocalizedText.Language.TraditionalChinese;
            list[index] = value;
            keys.Remove(key);
        }

        foreach (var missing in keys)
        {
            Logger.LogWarning($"缺少「{missing}」翻譯key，請更新翻譯資料！");
        }
    }
    
    private static bool TryReadFromJson<T>(string fileName, out T result, Func<T> defaultContent) where T : class
    {
        var dir = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_GUID);
        Directory.CreateDirectory(dir);
        
        var path = Path.Combine(dir, fileName);

        if (!File.Exists(path))
        {
            var def = JsonConvert.SerializeObject(defaultContent());
            File.WriteAllText(path, def);
        }
        
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            result = JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError($"無法讀取 JSON 設定：{fileName}");
            Logger.LogError(e);
            result = null;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(LanguageSetting), nameof(LanguageSetting.GetCustomLocalizedChoices))]
    [HarmonyPostfix]
    private static void PatchLanguageChoices(LanguageSetting __instance, ref List<string> __result)
    {
        // We already injected our Traditional Chinese into ValueToLanguage.
        // We just need to add one more choice to the localized choices.
        var val = Enum.GetNames(typeof(LanguageSetting.Language)).Length;
        var choice = LocalizedText.GetText("CURRENT_LANGUAGE", __instance.ValueToLanguage(val));

        if (__result.Contains(choice))
        {
        }
        
        Logger.LogInfo("正在修正設定中的語言清單...");
        __result.Add(choice);
    }
    
    [HarmonyPatch(typeof(LanguageSetting), nameof(LanguageSetting.ValueToLanguage))]
    [HarmonyPostfix]
    private static void PatchValueToLanguage(int val, ref LocalizedText.Language __result)
    {
        // We put Traditional Chinese right after Simplified Chinese, which is how the enum is originally ordered.
        // !!: This is a breaking change for players using Japanese, Korean, Polish and Turkish.
        __result = (LocalizedText.Language) val;
    }

    [HarmonyPatch(typeof(LocalizedText), nameof(LocalizedText.LoadMainTable))]
    [HarmonyPostfix]
    private static void PatchLoadMainTable()
    {
        if (_writtenMainTable) return;
        _writtenMainTable = true;
        
        var dir = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_GUID);
        Directory.CreateDirectory(dir);
        
        var path =  Path.Combine(dir, "_Auto" + TcnTranslationFileName);

        var table = LocalizedText.mainTable.ToDictionary(
            p => p.Key,
            p => p.Value[(int) LocalizedText.Language.SimplifiedChinese]
        );
        
        var json = JsonConvert.SerializeObject(table);
        
        File.WriteAllText(path, json);
    }

    [HarmonyPatch(typeof(LoadingScreenAnimation), "Start")]
    [HarmonyPostfix]
    private static void PatchLoadingAnimation(LoadingScreenAnimation __instance)
    {
        var text = LocalizedText.GetText("LOADING");
        var fieldLoadingString = typeof(LoadingScreenAnimation).GetField("loadingString", BindingFlags.NonPublic | BindingFlags.Instance);
        var fieldDefaultLoadingStringLength = typeof(LoadingScreenAnimation).GetField("defaultLoadingStringLength", BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (fieldLoadingString == null)
        {
            Logger.LogWarning("Patcher 找不到欄位: LoadingScreenAnimation.loadingString");
            return;
        }
        
        if (fieldDefaultLoadingStringLength == null)
        {
            Logger.LogWarning("Patcher 找不到欄位: LoadingScreenAnimation.defaultLoadingStringLength");
            return;
        }
        
        Logger.LogInfo("正在修正載入中動畫的文字 @ LoadingScreenAnimation.Start()...");
        
        var loadingString = $"{text}...{text}...{text}...{text}...";
        fieldLoadingString.SetValue(__instance, loadingString);
        fieldDefaultLoadingStringLength.SetValue(__instance, (float) loadingString.Length);
    }
}
