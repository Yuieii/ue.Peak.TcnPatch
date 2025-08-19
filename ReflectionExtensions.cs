// Copyright (c) 2025 Yuieii.

using System.Reflection;
using System.Runtime.CompilerServices;
using TMPro;

namespace ue.Peak.TcnPatch;

internal static class ReflectionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue GetReflectionFieldValue<TOwner, TValue>(this TOwner owner, ReflectionMembers.TypedFieldInfo<TOwner, TValue> fieldInfo) 
        => (TValue) fieldInfo.FieldInfo.GetValue(owner);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetReflectionFieldValue<TOwner, TValue>(this TOwner owner, ReflectionMembers.TypedFieldInfo<TOwner, TValue> fieldInfo, out TValue result)
    {
        if (fieldInfo == null)
        {
            result = default;
            return false;
        }

        result = (TValue) fieldInfo.FieldInfo.GetValue(owner);
        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetReflectionFieldValue<TOwner, TValue>(this TOwner owner, ReflectionMembers.TypedFieldInfo<TOwner, TValue> fieldInfo, TValue value) 
        => fieldInfo.FieldInfo.SetValue(owner, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetReflectionFieldValue<TOwner, TValue>(this TOwner owner, ReflectionMembers.TypedFieldInfo<TOwner, TValue> fieldInfo, TValue value)
    {
        if (fieldInfo == null) return false;

        fieldInfo.FieldInfo.SetValue(owner, value);
        return true;
    }
}