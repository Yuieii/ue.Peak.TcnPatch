// Copyright (c) 2025 Yuieii.

using System;
using System.Collections;
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
using Newtonsoft.Json.Linq;
using ue.Peak.TcnPatch.Adapters;
using ue.Peak.TcnPatch.Patches;
using UnityEngine;

namespace ue.Peak.TcnPatch;

[BepInPlugin(ModGuid, ModName, ModVersion)]
[BepInDependency("MoreAscents", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    public const string ModGuid = "ue.Peak.TcnPatch";
    public const string ModName = "ue.Peak.TcnPatch";
    public const string ModVersion = "1.2.1";
    
    internal static Plugin Instance { get; private set; }
    
    internal new static ManualLogSource Logger { get; private set; }
        
    private static FileSystemWatcher _watcher;

    public const string TcnTranslationFileName = "TcnTranslations.json";

    private static SemaphoreSlim _lock = new(1, 1);

    internal static TranslationFile EmptyTranslationFile { get; set; }

    internal static TranslationFile CurrentTranslationFile { get; private set; }

    internal static PluginConfig ModConfig { get; private set; }

    internal static HashSet<string> EphemeralTranslationKeys { get; } = new();

    private void Awake()
    {
        // Plugin startup logic
        Instance = this;
        ModConfig = new PluginConfig(Config);
        Logger = base.Logger;
        
        Logger.LogInfo($"正在載入模組 - {ModGuid}");
        
        if (Enum.GetValues(typeof(LanguageSetting.Language))
            .Cast<int>()
            .Any(values => values == (int) LocalizedText.Language.TraditionalChinese))
        {
            Logger.LogWarning("看起來繁體中文已經在設定清單裡面了！將會停用本模組。");
            return;
        }

        if (ModConfig.DownloadFromRemote.Value)
        {
            Task.Run(async () =>
            {
                var url = ModConfig.DownloadUrl.Value;
                Logger.LogInfo("正在從遠端下載翻譯資料... (可以在模組設定停用)");
                Logger.LogInfo($"網址：{url}");

                var client = new HttpClient();
                
                try
                {
                    var content = await client.GetStringAsync(url);

                    try
                    {
                        // The content we get should at least be a valid JSON object
                        _ = JObject.Parse(content);
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
        
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGuid);
        
        var api = API.TcnPatch.InternalInstance;
        api.RegisterLocalizationKey("PeakTcnPatch.Passport.Crabland", "CRABLAND");
        
        MoreAscentsSupport.RegisterLocalizations();
        
        Logger.LogInfo($"已載入模組 - {ModGuid}");
        Logger.LogInfo("  + 非官方繁體中文翻譯支援模組 -- by悠依");

        _ = LoadTranslationsAsync();
    }

    private static async Task LoadTranslationsAsync()
    {
        // Wait for the next frame, so all Awake() should have been called then.
        await Utils.WaitForNextFrameAsync();
        
        var dir = Path.Combine(Paths.ConfigPath, ModGuid);
        Directory.CreateDirectory(dir);
        
        _watcher = new FileSystemWatcher(dir, "*.json");

        _watcher.NotifyFilter = NotifyFilters.LastWrite;
        
        _watcher.Changed += (_, args) =>
        {
            if (args.Name == TcnTranslationFileName)
            {
                Logger.LogInfo("正在更新遊戲內繁體中文翻譯資料...");
                UpdateMainTable();
            }
        };
        
        UpdateMainTable();
        
        _watcher.EnableRaisingEvents = true;
    }

    private static void UpdateMainTable()
    {
        _lock.Wait();
        
        try
        {
            if (!TryReadFromJson(TcnTranslationFileName, out JObject obj, () => []))
                return;

            CurrentTranslationFile = TranslationFile.Deserialize(obj);
        }
        catch (TranslationParseException e)
        {
            Logger.LogError(e.UserMessage);
            Logger.LogError("翻譯資料分析失敗！");
            return;
        }
        catch (Exception e)
        {
            Logger.LogError("翻譯資料分析失敗！");
            Logger.LogError(e);
            return;
        }
        finally
        {
            _lock.Release();
        }

        var mainTable = LocalizedText.mainTable;
        var keys = mainTable.Keys.ToHashSet();

        if (CurrentTranslationFile.Authors.Count > 0)
        {
            Logger.LogInfo($"翻譯資料作者：{string.Join("、", CurrentTranslationFile.Authors)}");
        }
        else
        {
            Logger.LogInfo("翻譯資料作者：未知");
        }
        
        foreach (var removal in EphemeralTranslationKeys)
        {
            LocalizedText.mainTable.Remove(removal);
        }
        
        EphemeralTranslationKeys.Clear();
        
        foreach (var (key, value) in CurrentTranslationFile.Translations)
        {
            if (!mainTable.TryGetValue(key.ToUpperInvariant(), out var list))
            {
                Logger.LogWarning($"已忽略未知的翻譯key：「{key}」！");
                continue;
            }

            const int index = (int)LocalizedText.Language.TraditionalChinese;
            list[index] = value;
            keys.Remove(key);
        }
        
        foreach (var (key, value) in CurrentTranslationFile.AdditionalTranslations)
        {
            if (!mainTable.TryGetValue(key.ToUpperInvariant(), out var list))
            {
                // Found ephemeral key!
                EphemeralTranslationKeys.Add(key.ToUpperInvariant());
                
                var table = Enumerable.Repeat("", Enum.GetValues(typeof(LocalizedText.Language)).Length).ToList();
                table[(int) LocalizedText.Language.English] = value;
                LocalizedText.mainTable[key.ToUpperInvariant()] = table;
                
                continue;
            }

            const int index = (int)LocalizedText.Language.TraditionalChinese;
            list[index] = value;
            keys.Remove(key);
        }

        var vanillaKeys = LocalizedTextPatch.VanillaLocalizationKeys;
        
        foreach (var missing in keys)
        {
            if (vanillaKeys.Contains(missing))
            {
                Logger.LogWarning($"缺少「{missing}」翻譯key，請更新翻譯資料！");
            }
            else if (ModConfig.WarnMissingAdditionalKeys.Value)
            {
                Logger.LogWarning($"*附加翻譯* 缺少「{missing}」翻譯key！");
            }
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
}
