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
    
    private readonly Dictionary<string, Action> _registeredKeys = new();
    
    public HashSet<string> RegisteredAdditionalKeys => _registeredKeys.Keys.ToHashSet();

    internal string ReplaceAsRegisteredCase(string key)
    {
        var keys = RegisteredAdditionalKeys.ToList();
        var index = keys.FindIndex(k => k.ToUpperInvariant() == key);
        return index == -1 ? key : keys[index];
    }
    
    public void RegisterLocalizationKey(string key, string unlocalized)
    {
        _registeredKeys[key] = Insert;

        if (CanInsertOnRegister)
        {
            Insert();
        }
        
        void Insert()
        {
            var table = Enumerable.Repeat("", Enum.GetValues(typeof(LocalizedText.Language)).Length).ToList();
            table[(int) LocalizedText.Language.English] = unlocalized;
            LocalizedText.mainTable[key.ToUpperInvariant()] = table;
        }
    }
    
    internal bool CanInsertOnRegister { get; set; }
    
    internal void InsertExistingLocalizations()
    {
        foreach (var register in _registeredKeys.Values)
        {
            register();
        }
    }
}