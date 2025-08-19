// Copyright (c) 2025 Yuieii.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using Zorro.Settings;

namespace ue.Peak.TcnPatch;

[BepInPlugin(ModGuid, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    private const string ModGuid = "ue.Peak.TcnPatch";
    private const string ModName = "ue.Peak.TcnPatch";
    private const string ModVersion = "1.0.4";
    
    internal static new ManualLogSource Logger;
        
    private static FileSystemWatcher _watcher;

    private const string TcnTranslationFileName = "TcnTranslations.json";

    private static SemaphoreSlim _lock = new(1, 1);
    private static bool _writtenMainTable;

    private static PluginConfig _config;
    
    private void Awake()
    {
        // Plugin startup logic
        _config = new PluginConfig(Config);
        Logger = base.Logger;

        Logger.LogInfo($"正在載入模組 - {ModGuid}");
        
        if (Enum.GetValues(typeof(LanguageSetting.Language))
            .Cast<int>()
            .Any(values => values == (int) LocalizedText.Language.TraditionalChinese))
        {
            Logger.LogWarning("看起來繁體中文已經在設定清單裡面了！將會停用本模組。");
            return;
        }

        if (_config.DownloadFromRemote.Value)
        {
            Task.Run(async () =>
            {
                var url = _config.DownloadUrl.Value;
                Logger.LogInfo("正在從遠端下載翻譯資料... (可以在模組設定停用)");
                Logger.LogInfo($"網址：{url}");

                var client = new HttpClient();
                
                try
                {
                    var content = await client.GetStringAsync(url);

                    try
                    {
                        _ = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning("無效的遠端翻譯資料！將使用本機資料。");
                        Logger.LogWarning(e);
                        return;
                    }

                    var dir = Path.Combine(Paths.ConfigPath, ModGuid);
                    Directory.CreateDirectory(dir);

                    var path = Path.Combine(dir, TcnTranslationFileName);
                    await _lock.WaitAsync();
                    
                    try
                    {
                        await using var targetStream = File.Open(path, FileMode.Truncate, FileAccess.Write);
                        await using var writer = new StreamWriter(targetStream);
                        await writer.WriteAsync(content);
                    }
                    finally
                    {
                        _lock.Release();
                    }

                    Logger.LogInfo("翻譯資料下載完成！");
                }
                catch (Exception e)
                {
                    Logger.LogError("翻譯資料下載失敗！將使用本機資料。");
                    Logger.LogError(e);
                }
                
                client.Dispose();
            });
        }
        
        Harmony.CreateAndPatchAll(typeof(Plugin));
        
        var dir = Path.Combine(Paths.ConfigPath, ModGuid);
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
        
        Logger.LogInfo($"已載入模組 - {ModGuid}");
        Logger.LogInfo("  + 非官方繁體中文翻譯支援模組 -- by悠依");
    }

    private static void UpdateMainTable()
    {
        Dictionary<string, string> table;
        _lock.Wait();
        try
        {
            if (!TryReadFromJson(TcnTranslationFileName, out table, () => []))
                return;
        }
        finally
        {
            _lock.Release();
        }

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
        var dir = Path.Combine(Paths.ConfigPath, ModGuid);
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
        switch (_config.LanguagePatchMode.Value)
        {
            case LanguagePatchMode.ReplaceSimplifiedChinese:
            {
                // We already replaced Simplified Chinese to Traditional Chinese in ValueToLanguage.
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
                Logger.LogInfo("正在修正設定中的語言清單...");
                __result.Add(choice);
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
        switch (_config.LanguagePatchMode.Value)
        {
            case LanguagePatchMode.InsertAfterSimplifiedChinese:
            {
                // We put Traditional Chinese right after Simplified Chinese, which is how the enum is originally ordered.
                // !!: This is a breaking change for players using Japanese, Korean, Polish and Turkish.
                __result = (LocalizedText.Language)val;
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

            default:
            {
                // Unknown case. Do nothing.
                return;
            }
        }
    }

    private static bool _versionTextMissingWarned;

    [HarmonyPatch(typeof(VersionString), "Start")]
    [HarmonyPostfix]
    private static void PatchVersionStringWarnOnStart()
    {
        // Just in case the field is missing in a future release of the game (unlikely but why not)
        if (ReflectionMembers.Fields.VersionStringText != null || _versionTextMissingWarned) return;
        _versionTextMissingWarned = true;
            
        // Log a warning so we know what is happening when we don't see our credit text
        Logger.LogWarning("VersionString: 找不到版本資訊的 m_text 欄位！");
    }
    
    [HarmonyPatch(typeof(VersionString), "Update")]
    [HarmonyPostfix]
    private static void PatchVersionString(VersionString __instance)
    {
        // Just in case the field is missing in a future release of the game (unlikely but why not)
        var textField = ReflectionMembers.Fields.VersionStringText;
        if (textField == null) return;
        
        // We only want to show this when our language is Traditional Chinese
        if (LocalizedText.CURRENT_LANGUAGE != LocalizedText.Language.TraditionalChinese) return;
        
        // We only want to show this when we are in the main menu
        var parentName = __instance.transform.GetParent().gameObject.name;
        var objectName = __instance.gameObject.name;
        if (parentName != "Logo" || objectName != "Version") return;

        var text = __instance.GetReflectionFieldValue(textField);
        
        // TODO: Add custom credit support via translation .json file?
        // TODO: Will need to add custom key support
        text.text += "  (繁中支援by悠依)";
    }

    [HarmonyPatch(typeof(LocalizedText), nameof(LocalizedText.LoadMainTable))]
    [HarmonyPostfix]
    private static void PatchLoadMainTable()
    {
        if (_writtenMainTable) return;
        _writtenMainTable = true;
        
        var dir = Path.Combine(Paths.ConfigPath, ModGuid);
        Directory.CreateDirectory(dir);
        
        var path =  Path.Combine(dir, "_Auto" + TcnTranslationFileName);

        var table = LocalizedText.mainTable.ToDictionary(
            p => p.Key,
            p => p.Value[(int) _config.AutoDumpLanguage.Value]
        );
        
        var json = JsonConvert.SerializeObject(table, Formatting.Indented);
        
        File.WriteAllText(path, json);
    }

    [HarmonyPatch(typeof(LoadingScreenAnimation), "Start")]
    [HarmonyPostfix]
    private static void PatchLoadingAnimation(LoadingScreenAnimation __instance)
    {
        var text = LocalizedText.GetText("LOADING");
        
        if (ReflectionMembers.Fields.LoadingScreenString == null)
        {
            Logger.LogWarning("Patcher 找不到欄位: LoadingScreenAnimation.loadingString");
            return;
        }
        
        if (ReflectionMembers.Fields.LoadingScreenDefaultStringLength == null)
        {
            Logger.LogWarning("Patcher 找不到欄位: LoadingScreenAnimation.defaultLoadingStringLength");
            return;
        }
        
        Logger.LogInfo("正在修正載入中動畫的文字 @ LoadingScreenAnimation.Start()...");
        
        var loadingString = $"{text}...{text}...{text}...{text}...";
        __instance.SetReflectionFieldValue(ReflectionMembers.Fields.LoadingScreenString, loadingString);
        __instance.SetReflectionFieldValue(ReflectionMembers.Fields.LoadingScreenDefaultStringLength, loadingString.Length);
    }
}
