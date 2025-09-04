// Copyright (c) 2025 Yuieii.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace ue.Peak.TcnPatch.Adapters;

// We could make this a dedicated plugin so it simplifies everything here.
// Actually this support is ported from another plugin I made! -> ue.Peak.TcnPatch.MoreAscents

// The only advantage I can think of by doing everything here is users don't need to install
// a separate plugin to support one another plugin.
public static class MoreAscentsSupport
{
    private static readonly Lazy<Type> _gimmickHandlerType = new(() => Type.GetType("MoreAscents.AscentGimmickHandler"));
    
    public static bool IsMoreAscentsInstalled => _gimmickHandlerType.Value != null;
    
    private static Type GimmickHandlerType => _gimmickHandlerType.Value;

    private static bool _setupSupport;
    
    public static void RegisterLocalizations()
    {
        if (!IsMoreAscentsInstalled) return;
        
        var hasInitializedField = AccessTools.Field(GimmickHandlerType, "HasInitialized");
        if (hasInitializedField == null) return;
        if (!(bool)hasInitializedField.GetValue(null))
        {
            Plugin.Logger.LogWarning("MoreAscents 還沒初始化！？");
            return;
        }
        
        if (_setupSupport) return;
        _setupSupport = true;
        
        Plugin.Logger.LogInfo("偵測到 MoreAscents！正在為 MoreAscents 提供繁中支援...");
        
        const string noSupport = "無法為 MoreAscents 提供繁中支援... x_x";
        
        try
        {
            if (!TryFindTypeOrWarn("MoreAscents.AscentGimmick", out var gimmickType))
                return;

            if (!TryFindMethodOrWarn(gimmickType, "GetTitle", 
                    out var gimmickGetTitleMethod)) return;
            
            if (!TryFindMethodOrWarn(gimmickType, "GetDescription", 
                    out var gimmickGetDescriptionMethod)) return;
            
            if (!TryFindMethodOrWarn(gimmickType, "GetTitleReward",
                    out var gimmickGetTitleRewardMethod)) return;

            if (!TryFindFieldOrWarn(GimmickHandlerType, "gimmicks", out var field))
                return;
            
            var gimmicks = (IReadOnlyList<object>)field.GetValue(null);

            var instanceProp = AccessTools.PropertyGetter(typeof(AscentData), "Instance");
            var ascentData = (AscentData)instanceProp.Invoke(null, []);
            var ascents = ascentData.ascents;

            foreach (var gimmick in gimmicks)
            {
                var data = ascents.FirstOrDefault(d =>
                    d.title == (string)gimmickGetTitleMethod.Invoke(gimmick, []) &&
                    d.description == (string)gimmickGetDescriptionMethod.Invoke(gimmick, []));

                if (data == null) continue;

                var prefix = "MoreAscent." + gimmick.GetType().Name;
                data.title = prefix;
                data.titleReward = $"{prefix}.Reward";

                API.TcnPatch.Instance.RegisterLocalizationKey($"{prefix}",
                    (string)gimmickGetTitleMethod.Invoke(gimmick, []));
                
                // Although `AscentData` has a `description` field, it is unused at runtime (!)
                // PEAK currently uses `LocalizedText.GetDescriptionIndex(data.title)` as its translation key
                // for the ascent description
                API.TcnPatch.Instance.RegisterLocalizationKey(LocalizedText.GetDescriptionIndex(prefix),
                    (string)gimmickGetDescriptionMethod.Invoke(gimmick, []));
                
                API.TcnPatch.Instance.RegisterLocalizationKey($"{prefix}.Reward",
                    (string)gimmickGetTitleRewardMethod.Invoke(gimmick, []));
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(noSupport);
            Plugin.Logger.LogError(e);
        }

        bool TryFindTypeOrWarn(string typeName, out Type type)
        {
            type = AccessTools.TypeByName(typeName);
            if (type != null) return true;
        
            Plugin.Logger.LogWarning($"無法找到 {typeName} 類型！{noSupport}");    
            return false;
        }
        
        bool TryFindMethodOrWarn(Type type, string methodName, out MethodInfo method)
        {
            method = AccessTools.Method(type, methodName);
            if (method != null) return true;
        
            Plugin.Logger.LogWarning($"無法找到 {type.Name}.{methodName}() 方法！{noSupport}");    
            return false;
        }
        
        bool TryFindFieldOrWarn(Type type, string fieldName, out FieldInfo field)
        {
            field = AccessTools.Field(type, fieldName);
            if (field != null) return true;
        
            Plugin.Logger.LogWarning($"無法找到 {type.Name}.{fieldName} 欄位！{noSupport}");    
            return false;
        }
    }
}