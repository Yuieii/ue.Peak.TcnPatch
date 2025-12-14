// Copyright (c) 2025 Yuieii.

using System;
using BepInEx.Configuration;

namespace ue.Peak.TcnPatch
{
    public class PluginConfig(ConfigFile config)
    {
        public ConfigEntry<bool> DownloadFromRemote { get; } = config.Bind(
            "Update",
            "DownloadFromRemote",
            true,
            "是否每次啟動都要從遠端下載最新的翻譯資料？ (true: 是, false: 否)"
        );
    
        public ConfigEntry<string> DownloadUrl { get; } = config.Bind(
            "Update",
            "DownloadUrl",
            "https://raw.githubusercontent.com/Yuieii/ue.Peak.TcnPatch/refs/heads/master/TcnTranslations.json",
            "翻譯資料的 URL"
        );

        public ConfigEntry<LocalizedText.Language> AutoDumpLanguage { get; } = config.Bind(
            "Debug",
            "AutoDumpLanguage",
            LocalizedText.Language.SimplifiedChinese,
            "自動輸出官方參考翻譯時選擇的原始語言"
        );

        public ConfigEntry<bool> EnableAutoDumpLanguage { get; } = config.Bind(
            "Debug",
            "EnableAutoDumpLanguage",
            true,
            string.Join('\n', 
                "是否自動輸出遊戲內的翻譯表以供參考",
                " (有 ModConfig 的話即時更改可以重新輸出)"
            )
        );
    
        public ConfigEntry<bool> WarnUnknownTranslationKeys { get; } = config.Bind(
            "Debug",
            "WarnUnknownTranslationKeys",
            false,
            "有未知的非附加翻譯key時是否觸發警告"
        );
    
        public ConfigEntry<bool> WarnMissingAdditionalKeys { get; } = config.Bind(
            "Debug",
            "WarnMissingAdditionalKeys",
            false,
            "有缺失的附加翻譯key時是否觸發警告"
        );

        public ConfigEntry<LanguagePatchMode> LanguagePatchMode { get; } = config.Bind(
            "Patch",
            "LanguagePatchMode",
            TcnPatch.LanguagePatchMode.InsertAfterSimplifiedChinese,
            "設定如何修正設定中的語言清單"
        );

        public ConfigEntry<bool> ShowPatchCredit { get; } = config.Bind(
            "Patch",
            "ShowPatchCredit",
            true,
            "在主畫面版本文字下方顯示本模組作者？"
        );
    
        public ConfigEntry<bool> ShowTranslatorCredit { get; } = config.Bind(
            "Patch",
            "ShowTranslatorCredit",
            true,
            "在主畫面版本文字下方顯示翻譯資料的作者？"
        );

        public ConfigEntry<bool> ShowModVersionInPatchCredit { get; } = config.Bind(
            "Patch",
            "ShowModVersionInPatchCredit",
            true,
            $"在主畫面版本文字後面顯示這個模組的版本？ (v{Plugin.ModVersion})"
        );
    }

    public enum LanguagePatchMode
    {
        /// <summary>
        /// 放在簡體中文後面；位於簡體中文和日文之間 
        /// </summary>
        InsertAfterSimplifiedChinese,
    
        /// <summary>
        /// 取代英文的選項 
        /// </summary>
        ReplaceEnglish,
    
        /// <summary>
        /// 取代簡體中文的選項 
        /// </summary>
        ReplaceSimplifiedChinese,
    
        /// <summary>
        /// 放在語言清單的最下面
        /// </summary>
        Append,
    
        /// <summary>
        /// 只有繁體中文可以用
        /// </summary>
        TraditionalChineseOnly,
    }
}