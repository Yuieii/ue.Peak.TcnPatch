// Copyright (c) 2025 Yuieii.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ue.Peak.TcnPatch;

public class TranslationFile
{
    public const int CurrentFormatVersion = 0;

    public const string FormatVersionKey = "FormatVersion";
    public const string AuthorKey = "Authors";
    public const string TranslationEntriesKey = "Translations";

    public List<string> Authors { get; } = new();

    public Dictionary<string, string> Translations { get; } = new();

    public static TranslationFile Deserialize(JObject obj)
    {
        var schemefulKeys = new[]
        {
            FormatVersionKey, AuthorKey, TranslationEntriesKey
        };
        
        if (!schemefulKeys.All(obj.ContainsKey))
        {
            return InternalDeserializeFromLegacy(obj);
        }

        var result = new TranslationFile();
        var formatVersion = obj[FormatVersionKey]!.Value<int>();
        if (formatVersion > CurrentFormatVersion)
        {
            Plugin.Logger.LogWarning("正在讀取過新版本的翻譯資料！可能會無法正確讀取。");
        }

        var authorsToken = obj[AuthorKey];
        if (authorsToken is JArray authorsArr)
        {
            foreach (var authorToken in authorsArr)
            {
                result.Authors.Add(authorToken.Value<string>());
            }
        } 
        else if (authorsToken is JValue authorsValue)
        {
            result.Authors.Add(authorsValue.Value<string>());
        }
        else
        {
            Plugin.Logger.LogWarning($"無效的翻譯者資料！ ({AuthorKey})");
        }

        var entries = obj[TranslationEntriesKey];
        if (entries is not JObject entriesObj)
        {
            Plugin.Logger.LogWarning($"無效的翻譯資料！ ({TranslationEntriesKey})");
            return result;
        }

        foreach (var (key, value) in entriesObj)
        {
            result.Translations[key] = value!.Value<string>();
        }

        return result;
    }

    private static TranslationFile InternalDeserializeFromLegacy(JObject obj)
    {
        var result = new TranslationFile();
        
        foreach (var (key, value) in obj)
        {
            result.Translations[key] = value!.Value<string>();
        }

        return result;
    }
}