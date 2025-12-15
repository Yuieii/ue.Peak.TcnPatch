// Copyright (c) 2025 Yuieii.

using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using TMPro;

namespace ue.Peak.TcnPatch
{
    internal static class ReflectionExtensions
    {
        extension<TOwner>(TOwner owner)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TValue GetReflectionFieldValue<TValue>(ReflectionMembers.TypedFieldInfo<TOwner, TValue> fieldInfo) 
                => (TValue) fieldInfo.FieldInfo.GetValue(owner);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetReflectionFieldValue<TValue>(ReflectionMembers.TypedFieldInfo<TOwner, TValue> fieldInfo, TValue value) 
                => fieldInfo.FieldInfo.SetValue(owner, value);
        }

        extension([CanBeNull] FieldInfo field)
        {
            [CanBeNull]
            public ReflectionMembers.TypedFieldInfo<TOwner, TValue> AsTyped<TOwner, TValue>() => field;
        }
    }
}