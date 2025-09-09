// Copyright (c) 2025 Yuieii.

using System;
using BepInEx.Configuration;

namespace ue.Peak.TcnPatch;

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
        "設定修正設定中的語言清單時，要放在簡體中文後面，或是取代簡體中文"
    );

    public ConfigEntry<bool> ShowPatchCredit { get; } = config.Bind(
        "Patch",
        "ShowPatchCredit",
        true,
        "在主畫面版本文字後面顯示本模組作者？"
    );
    
    public ConfigEntry<bool> ShowTranslatorCredit { get; } = config.Bind(
        "Patch",
        "ShowTranslatorCredit",
        true,
        "在主畫面版本文字後面交替顯示翻譯資料的作者？"
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
    InsertAfterSimplifiedChinese,
    ReplaceSimplifiedChinese,
    Append,
}