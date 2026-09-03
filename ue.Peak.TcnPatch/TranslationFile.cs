// Copyright (c) 2025 Yuieii.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using ue.Peak.TcnPatch.Core;

namespace ue.Peak.TcnPatch
{
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
        public const string AdditionalTranslationEntriesKey = "AdditionalTranslations";
        public const string IgnoredTranslationEntriesKey = "IgnoredTranslations";

        // The field is needed for serialization
        [UsedImplicitly]
        private int FormatVersion { get; } = CurrentFormatVersion;
        
        public List<string> Authors { get; } = new();

        public Dictionary<string, string> Translations { get; } = new();
    
        // Apart from `Translations`, additional translations contains those which may come from other mods.
        // Mods (or supporting adapter) can register new localization keys via the provided API.
        public Dictionary<string, string> AdditionalTranslations { get; } = new();

        public HashSet<string> IgnoredTranslations { get; } = new();

        public TranslationFile CreateCopy()
        {
            var cloned = new TranslationFile();

            foreach (var author in Authors)
            {
                cloned.Authors.Add(author);
            }
            
            foreach (var (key, value) in Translations)
            {
                cloned.Translations[key] = value;
            }

            foreach (var (key, value) in AdditionalTranslations)
            {
                cloned.AdditionalTranslations[key] = value;
            }

            foreach (var key in IgnoredTranslations)
            {
                cloned.IgnoredTranslations.Add(key);
            }

            return cloned;
        }

        public static Result<TranslationFile, Exception> TryDeserialize(JObject obj)
            => Result.Catch(() => Deserialize(obj));
        
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
            obj.GetOptional(AuthorKey).IfSome(authorsToken =>
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
            });

            {
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
                    if (value?.Type != JTokenType.String)
                    {
                        Plugin.Logger.LogWarning($"無效的單一翻譯資料：\"{key}\" (非字串)");    
                        continue;
                    }
                    
                    result.Translations[key] = value!.Value<string>();
                }
            }

            obj.GetOptional(AdditionalTranslationEntriesKey).IfSome(entries =>
            {
                // Additional translation entries.
                if (entries is not JObject entriesObj)
                {
                    throw new TranslationParseException(
                        $"Additional translation entries must be an object, found {entries.Type}",
                        $"無效的附加翻譯資料！ ({AdditionalTranslationEntriesKey})"
                    );
                }
                
                var additionalKeys = new List<string>();
                foreach (var (key, value) in entriesObj)
                {
                    if (value?.Type != JTokenType.String)
                    {
                        Plugin.Logger.LogWarning($"無效的單一附加翻譯資料：\"{key}\" (非字串)");    
                        continue;
                    }
                    
                    if (additionalKeys.Contains(key, StringComparer.InvariantCultureIgnoreCase))
                    {
                        Plugin.Logger.LogWarning($"翻譯資料出現已註冊過的附加翻譯key「{key}」！新的同名翻譯將會被忽略。");
                        continue;
                    }
                    
                    additionalKeys.Add(key);
                    result.AdditionalTranslations[key] = value!.Value<string>();
                }
            });

            obj.GetOptional(IgnoredTranslationEntriesKey).IfSome(entries =>
            {
                // Additional translation entries.
                if (entries is not JArray entriesArr)
                {
                    throw new TranslationParseException(
                        $"Ignored translation entries must be an array, found {entries.Type}",
                        $"無效的忽略翻譯資料！ ({IgnoredTranslationEntriesKey})"
                    );
                }

                foreach (var value in entriesArr)
                {
                    if (value.Type != JTokenType.String)
                    {
                        // Can't log here because the value can be anything
                        continue;
                    }
                    
                    result.IgnoredTranslations.Add(value.Value<string>());
                }
            });

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
}