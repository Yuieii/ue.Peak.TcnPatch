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
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ue.Core;
using ue.Peak.TcnPatch.Adapters;
using ue.Peak.TcnPatch.Patches;

namespace ue.Peak.TcnPatch
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("MoreAscents", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.github.PEAKModding.PEAKLib.UI", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "ue.Peak.TcnPatch";
        public const string ModName = "ue.Peak.TcnPatch";
        public const string ModVersion = "1.5.4";
    
        internal static Plugin Instance { get; private set; }
    
        internal new static ManualLogSource Logger { get; private set; }
        
        private static FileSystemWatcher _watcher;

        public const string TcnTranslationFileName = "TcnTranslations.json";

        private static readonly SemaphoreSlim _lock = new(1, 1);

        internal static TranslationFile EmptyTranslationFile { get; set; }

        internal static TranslationFile CurrentTranslationFile { get; private set; }

        internal static PluginConfig ModConfig { get; private set; }

        internal static HashSet<string> EphemeralTranslationKeys { get; } = new();

        internal static bool HasOfficialTcn { get; private set; }
        
        [CanBeNull]
        private Harmony _harmony;

        private void Awake()
        {
            // Plugin startup logic
            Instance = this;
            ModConfig = new PluginConfig(Config);
        
            ModConfig.EnableAutoDumpLanguage.SettingChanged += (_, _) =>
            {
                if (!ModConfig.EnableAutoDumpLanguage.Value) return;
                LocalizedTextPatch.DumpLanguageEntries();
            };
        
            Logger = base.Logger;
        
            Logger.LogInfo($"正在載入模組 - {ModGuid}");
        
            if (Enum.GetValues(typeof(LanguageSetting.Language))
                .Cast<int>()
                .Any(values => values == (int) LocalizedText.Language.TraditionalChinese))
            {
                // We have official Traditional Chinese now!
                HasOfficialTcn = true;
            }

            if (ModConfig.DownloadFromRemote.Value)
            {
                _ = Task.Run(async () =>
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
                        await _lock.EnterScopeAsync(async () =>
                        {
                            await using var targetStream = File.Open(path, FileMode.Create, FileAccess.Write);
                            await using var writer = new StreamWriter(targetStream);
                            await writer.WriteAsync(content);
                        });

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

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGuid);
        
            var api = API.TcnPatch.InternalInstance;
            api.RegisterLocalizationKey("PeakTcnPatch.Passport.Crabland", "CRABLAND");
        
            MoreAscentsSupport.RegisterLocalizations();
        
            Logger.LogInfo($"已載入模組 - {ModGuid}");
            Logger.LogInfo("  + 非官方繁體中文翻譯支援模組 -- by悠依");
        }

        private void Start()
        {
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

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        internal static Dictionary<string, string> TranslationsLookup { get; } = new();
    
        internal static Dictionary<string, string> AdditionalTranslationsLookup { get; } = new();
    
        // Registered from API, contains unlocalized texts
        internal static Dictionary<string, string> KeyToUnlocalizedLookup { get; } = new();

        internal static bool TryGetVanilla(string id, out string result) 
            => TranslationsLookup.TryGetValue(id.ToUpperInvariant(), out result);

        internal static bool TryGetRegistered(string id, LocalizedText.Language? language, out string result)
        {
            language ??= LocalizedText.CURRENT_LANGUAGE;
        
            if (language == LocalizedText.Language.TraditionalChinese && AdditionalTranslationsLookup.TryGetValue(id, out result))
            {
                return true;
            }
        
            return KeyToUnlocalizedLookup.TryGetValue(id, out result);
        }
    
        private static void UpdateMainTable()
        {
            var flow = _lock.EnterScope(() =>
            {
                try
                {
                    if (!TryReadFromJson(TcnTranslationFileName, out JObject obj, () => []))
                        return ReturnFlow.Break;

                    CurrentTranslationFile = TranslationFile.Deserialize(obj);
                }
                catch (TranslationParseException e)
                {
                    Logger.LogError(e.UserMessage);
                    Logger.LogError("翻譯資料分析失敗！");
                    return ReturnFlow.Break;
                }
                catch (Exception e)
                {
                    Logger.LogError("翻譯資料分析失敗！");
                    Logger.LogError(e);
                    return ReturnFlow.Break;
                }

                return ReturnFlow.Continue;
            });

            if (flow == ReturnFlow.Break) 
                return;

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

            TranslationsLookup.Clear();
            AdditionalTranslationsLookup.Clear();
        
            foreach (var (key, value) in CurrentTranslationFile.Translations)
            {
                var upper = key.ToUpperInvariant();
            
                if (TranslationsLookup.ContainsKey(upper))
                {
                    Logger.LogInfo($"發現重複的翻譯key：「{key}」！已存在大寫的同名key！");
                    continue;
                }
            
                if (!mainTable.ContainsKey(upper))
                {
                    if (ModConfig.WarnUnknownTranslationKeys.Value)
                    {
                        Logger.LogWarning($"正在使用未知的翻譯key：「{upper}」！");
                    }
                }

                TranslationsLookup[upper] = value;
                keys.Remove(upper);
            }
        
            foreach (var (key, value) in CurrentTranslationFile.AdditionalTranslations)
            {
                AdditionalTranslationsLookup[key] = value;
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
        
            // Perform a force refresh on all localizable text
            LocalizedText.RefreshAllText();
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
}
