// Copyright (c) 2025 Yuieii.

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
}