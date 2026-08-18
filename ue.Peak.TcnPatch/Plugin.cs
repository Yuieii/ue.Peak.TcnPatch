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
using ue.Peak.TcnPatch.Adapters;
using ue.Peak.TcnPatch.Core;
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
        public const string ModVersion = "2.1.1";

        internal static Plugin Instance { get; private set; }

        internal new static ManualLogSource Logger { get; private set; }

        private static FileSystemWatcher _watcher;

        public const string TcnTranslationFileName = "TcnTranslations.json";

        // This field is initialized at Start()
        private static Mutex<FileIO> _fileIO;

        internal static TranslationFile CurrentTranslationFile { get; private set; } = new();

        internal static PluginConfig ModConfig { get; private set; }
        
        [CanBeNull]
        internal static VersionString VersionStringInstance { get; set; }

        private static readonly VersionTextUpdater _versionStringUpdater = new();

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

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGuid);

            var api = API.TcnPatch.InternalInstance;
            api.RegisterLocalizationKey("PeakTcnPatch.Passport.Crabland", "CRABLAND");
            api.RegisterLocalizationKey("PeakTcnPatch.BoardingPass.CustomExpedition", "CUSTOM EXPEDITION");
            
            MoreAscentsSupport.RegisterLocalizations();

            Logger.LogInfo($"已載入模組 - {ModGuid}");
            Logger.LogInfo("  + 非官方繁體中文翻譯支援模組 -- by悠依");
        }

        private static FileSystemWatcher InitializeFileWatcher(string dir)
        {
            var watcher = new FileSystemWatcher(dir, "*.json");
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += (_, args) =>
            {
                if (args.Name == TcnTranslationFileName)
                {
                    Logger.LogInfo("正在更新遊戲內繁體中文翻譯資料...");
                    UpdateMainTable();
                }
            };

            watcher.EnableRaisingEvents = true;
            return watcher;
        }

        private void Start()
        {
            var dir = Path.Combine(Paths.ConfigPath, ModGuid);
            Directory.CreateDirectory(dir);

            _fileIO = new Mutex<FileIO>(new FileIO(Path.Combine(dir, TcnTranslationFileName)));
            
            if (!ModConfig.DownloadFromRemote.Value)
            {
                // Directly read from our locally stored translations
                UpdateMainTable();
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    var result = await DownloadTranslationsAsync();
                    if (result.IsSuccess) return;
                    
                    // A failing result means the main table is not updated (due to the file not being modified)
                    // Manually run it once
                    UpdateMainTable();
                });
            }

            _watcher = InitializeFileWatcher(dir);
        }

        private void Update()
        {
            if (VersionStringInstance)
            {
                _versionStringUpdater.Update(VersionStringInstance);
            }
            
            SetTraditionalChineseFont();
        }

        private bool _hasSetTraditionalChineseFont;
        
        private void SetTraditionalChineseFont()
        {
            if (!_hasSetTraditionalChineseFont && FontFallbackSwapper.instance)
            {
                _hasSetTraditionalChineseFont = true;
                
                if (LocalizedText.CURRENT_LANGUAGE == LocalizedText.Language.TraditionalChinese)
                {
                    FontFallbackSwapper.instance.SwitchToTraditional();
                }
            }
        }

        private void OnDestroy()
        {
            _watcher?.Dispose();
            _harmony?.UnpatchSelf();
            _fileIO.Dispose();
        }

        private static async Task SaveTranslationsAsync(string content)
        {
            using var guard = _fileIO.AcquireExclusive();
            await using var targetStream = guard.Value.Open(FileMode.Create, FileAccess.Write);
            await using var writer = new StreamWriter(targetStream);
            await writer.WriteAsync(content);
        }

        private static readonly HttpClient _httpClient = new(); 

        private async Task<Result<Unit, Exception>> DownloadTranslationsAsync()
        {
            var url = ModConfig.DownloadUrl.Value;

            for (var i = 0; i < 2; i++)
            {
                Logger.LogInfo("正在從遠端下載翻譯資料... (可以在模組設定停用)");
                Logger.LogInfo($"網址：{url}");

                var client = _httpClient;

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
                        return Result.Error(e);
                    }

                    await SaveTranslationsAsync(content);
                    Logger.LogInfo("翻譯資料下載完成！");
                    return Result.Success(Unit.Instance);
                }
                catch (HttpRequestException e)
                {
                    if (i > 0 || ModConfig.DownloadFailureHandling.Value == DownloadFailureHandling.UseLocal)
                    {
                        Logger.LogError("翻譯資料下載失敗！將使用本機資料。");
                        Logger.LogError(e);
                        return Result.Error<Exception>(e);    
                    }
                    
                    Logger.LogError("翻譯資料下載失敗！將嘗試使用預設的遠端資料。");
                    url = (string) ModConfig.DownloadUrl.DefaultValue;
                }
                catch (Exception e)
                {
                    Logger.LogError("翻譯資料下載失敗！將使用本機資料。");
                    Logger.LogError(e);
                    return Result.Error(e);    
                }
            }

            // Unreachable.
            // A successful result means we don't need to update manually since it is handled via the watcher
            return Result.Error(new Exception("unreachable"));
        }

        internal static Dictionary<string, string> TranslationsLookup { get; } = new();

        internal static Dictionary<string, string> AdditionalTranslationsLookup { get; } = new();

        // Registered from API, contains unlocalized texts
        internal static Dictionary<string, string> KeyToUnlocalizedLookup { get; } = new();

        internal static Option<string> GetVanilla(string id)
            => TranslationsLookup.GetOptional(id.ToUpperInvariant());

        internal static Option<string> GetRegistered(string id, LocalizedText.Language? language)
        {
            language ??= LocalizedText.CURRENT_LANGUAGE;

            var result = language == LocalizedText.Language.TraditionalChinese
                ? AdditionalTranslationsLookup.GetOptional(id)
                : Option<string>.None;

            return result.OrGet(() => KeyToUnlocalizedLookup.GetOptional(id));
        }

        private static void UpdateMainTable()
        {
            var flow = TryReadFromJson<JObject>(TcnTranslationFileName, () => [])
                .SelectMany(TranslationFile.TryDeserialize)
                .Select<ControlFlow<Unit, Unit>>(f =>
                {
                    CurrentTranslationFile = f;
                    return ControlFlow.Continue();
                })
                .SelectError<ControlFlow<Unit, Unit>>(ex =>
                {
                    if (ex is TranslationParseException e)
                    {
                        Logger.LogError(e.UserMessage);
                        Logger.LogError("翻譯資料分析失敗！");
                    }
                    else
                    {
                        Logger.LogError("翻譯資料分析失敗！");
                        Logger.LogError(ex);
                    }

                    return ControlFlow.Break();
                })
                .Branch();

            if (flow.IsBreak) return;

            // The statement would become too unnecessarily complicated if we apply this fix suggestion
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (CurrentTranslationFile.Authors.Count > 0)
            {
                Logger.LogInfo($"翻譯資料作者：{string.Join("、", CurrentTranslationFile.Authors)}");
            }
            else
            {
                Logger.LogInfo("翻譯資料作者：未知");
            }

            // Intentionally get main table before cleaning local lookups.
            // LocalizedText.mainTable is lazily loaded with LocalizedText.LoadMainTable().
            var mainTable = LocalizedText.mainTable;
            var keys = mainTable.Keys.ToHashSet();
            
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

        private static Result<T, Exception> TryReadFromJson<T>(string fileName, Func<T> defaultContent) where T : class
        {
            var preparePath = Result.Catch(() =>
            {
                using var guard = _fileIO.AcquireExclusive();
                
                if (!guard.Value.Exists)
                {
                    using var stream = guard.Value.Open(FileMode.CreateNew, FileAccess.Write);
                    using var writer = new StreamWriter(stream);

                    var def = JsonConvert.SerializeObject(defaultContent());
                    writer.Write(def);
                }

                return Unit.Instance;
            });
            
            return preparePath
                .IfError(e =>
                {
                    Logger.LogError($"無法初始化 JSON 設定：{fileName}");
                    Logger.LogError(e);
                })
                .SelectMany(_ => DeserializeFromPath());
            
            Result<T, Exception> DeserializeFromPath() 
                => Result.Catch(() =>
                {
                    using var guard = _fileIO.AcquireExclusive();
                    using var stream = guard.Value.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);
                    return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
                })
                .IfError(e =>
                {
                    Logger.LogError($"無法讀取 JSON 設定：{fileName}");
                    Logger.LogError(e);
                });
        }
    }
}