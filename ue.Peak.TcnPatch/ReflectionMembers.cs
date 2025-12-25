// Copyright (c) 2025 Yuieii.

using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TMPro;

namespace ue.Peak.TcnPatch
{
    public static class Refl // Reflection members shorthand prefix
    {
        public static class LoadingScreenAnimation
        {
            public static TypedFieldInfo<global::LoadingScreenAnimation, string> LoadingString { get; } 
                = AccessTools.Field(typeof(global::LoadingScreenAnimation), "loadingString");
            public static TypedFieldInfo<global::LoadingScreenAnimation, float> DefaultLoadingStringLength { get; } 
                = AccessTools.Field(typeof(global::LoadingScreenAnimation), "defaultLoadingStringLength");
        }

        public static class LocalizedText
        {
            public static TypedFieldInfo<global::LocalizedText, global::LocalizedText.Language> CurrentLanguage { get; }
                = AccessTools.Field(typeof(global::LocalizedText), nameof(global::LocalizedText.CURRENT_LANGUAGE));
        }

        public static class VersionString
        {
            public static TypedFieldInfo<global::VersionString, TextMeshProUGUI> Text { get; } 
                = AccessTools.Field(typeof(global::VersionString), "m_text");
        }
    }

    public class TypedFieldInfo<TOwner, TValue>(FieldInfo fieldInfo)
    {
        public FieldInfo FieldInfo { get; } = fieldInfo;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator TypedFieldInfo<TOwner, TValue>(FieldInfo fieldInfo) 
            => fieldInfo == null ? null : new TypedFieldInfo<TOwner, TValue>(fieldInfo);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FieldInfo(TypedFieldInfo<TOwner, TValue> fieldInfo) 
            => fieldInfo?.FieldInfo;

        public TValue GetStaticValue() => (TValue) FieldInfo.GetValue(null);
    }
}