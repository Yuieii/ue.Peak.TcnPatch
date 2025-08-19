// Copyright (c) 2025 Yuieii.

using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TMPro;

namespace ue.Peak.TcnPatch;

public static class ReflectionMembers
{
    public static class Fields
    {
        public static TypedFieldInfo<VersionString, TextMeshProUGUI> VersionStringText { get; } 
            = AccessTools.Field(typeof(VersionString), "m_text");
        public static TypedFieldInfo<LoadingScreenAnimation, string> LoadingScreenString { get; } 
            = AccessTools.Field(typeof(LoadingScreenAnimation), "loadingString");
        public static TypedFieldInfo<LoadingScreenAnimation, float> LoadingScreenDefaultStringLength { get; } 
            = AccessTools.Field(typeof(LoadingScreenAnimation), "defaultLoadingStringLength");
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
    }
}