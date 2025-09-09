// Copyright (c) 2025 Yuieii.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SocialPlatforms;

namespace ue.Peak.TcnPatch.API;

public class TcnPatch : ITcnPatch
{
    public static ITcnPatch Instance => InternalInstance;
    
    internal static TcnPatch InternalInstance { get; } = new();
    
    public void RegisterLocalizationKey(string key, string unlocalized)
    {
        Plugin.RegisteredOrigTable[key] = unlocalized;
    }
}