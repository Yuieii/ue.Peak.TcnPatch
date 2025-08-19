// Copyright (c) 2025 Yuieii.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ue.Peak.TcnPatch;

public class TranslationParseException : Exception
{
    public TranslationParseException(string message, string userMessage) : base(message)
    {
        UserMessage = userMessage;
    }

    public TranslationParseException(string message) : base(message)
    {
        UserMessage = message;
    }

    public string UserMessage { get; }
}

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
            FormatVersionKey, TranslationEntriesKey
        };
        
        if (!schemefulKeys.All(obj.ContainsKey))
        {
            return InternalDeserializeFromLegacy(obj);
        }

        var result = new TranslationFile();
        
        // -- Format version
        var formatVersionToken = obj[FormatVersionKey]!;
        if (formatVersionToken.Type != JTokenType.Integer)
        {
            throw new TranslationParseException(
                $"Format version must be an integer value, found {formatVersionToken.Type}",
                "無效的格式版本！格式版本必須為一個整數！"
            );
        }
        
        var formatVersion = formatVersionToken.Value<int>();
        if (formatVersion > CurrentFormatVersion)
        {
            Plugin.Logger.LogWarning("正在讀取過新版本的翻譯資料！可能會無法正確讀取。");
        }

        // Author info
        if (obj.TryGetValue(AuthorKey, out var authorsToken))
        {
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
        }

        // Translation entries.
        var entries = obj[TranslationEntriesKey];
        if (entries is not JObject entriesObj)
        {
            throw new TranslationParseException(
                $"Translation entries must be an object, found {entries.Type}",
                $"無效的翻譯資料！ ({TranslationEntriesKey})"
            );
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