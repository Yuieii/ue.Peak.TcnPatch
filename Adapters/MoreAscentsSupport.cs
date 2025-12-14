// Copyright (c) 2025 Yuieii.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace ue.Peak.TcnPatch.Adapters
{
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
                if (!FindType("MoreAscents.AscentGimmick")
                        .IfError(e => e())
                        .TryUnwrap(out var gimmickType)) return;

                if (!FindMethod(gimmickType, "GetTitle")
                        .IfError(e => e())
                        .TryUnwrap(out var gimmickGetTitleMethod)) return;
                
                if (!FindMethod(gimmickType, "GetDescription")
                        .IfError(e => e())
                        .TryUnwrap(out var gimmickGetDescriptionMethod)) return;
                
                if (!FindMethod(gimmickType, "GetTitleReward")
                        .IfError(e => e())
                        .TryUnwrap(out var gimmickGetTitleRewardMethod)) return;

                if (!FindField(GimmickHandlerType, "gimmicks")
                        .IfError(e => e())
                        .TryUnwrap(out var field)) return;
            
                var gimmicks = (IReadOnlyList<object>) field.GetValue(null);

                var instanceProp = AccessTools.PropertyGetter(typeof(AscentData), "Instance");
                var ascentData = (AscentData) instanceProp.Invoke(null, []);
                var ascents = ascentData.ascents;

                foreach (var gimmick in gimmicks)
                {
                    var data = ascents.FirstOrDefault(d =>
                        d.title == (string) gimmickGetTitleMethod.Invoke(gimmick, []) &&
                        d.description == (string) gimmickGetDescriptionMethod.Invoke(gimmick, []));

                    if (data == null) continue;

                    var prefix = "MoreAscent." + gimmick.GetType().Name;
                    data.title = prefix;
                    data.titleReward = $"{prefix}.Reward";

                    API.TcnPatch.Instance.RegisterLocalizationKey($"{prefix}",
                        (string)gimmickGetTitleMethod.Invoke(gimmick, []));
                
                    // Although `AscentData` has a `description` field, it is currently *unused* at runtime (!)
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

            Result<Type, Action> FindType(string typeName)
            {
                var type = AccessTools.TypeByName(typeName);
                if (type != null) return Result.Success(type);

                return Result.Error(() =>
                {
                    Plugin.Logger.LogWarning($"無法找到 {typeName} 類型！{noSupport}"); 
                });
            }
            
            Result<MethodInfo, Action> FindMethod(Type type, string methodName)
            {
                var method = AccessTools.Method(type, methodName);
                if (method != null) return Result.Success(method);

                return Result.Error(() =>
                {
                    Plugin.Logger.LogWarning($"無法找到 {type.Name}.{methodName}() 方法！{noSupport}");    
                });
            }

            Result<FieldInfo, Action> FindField(Type type, string fieldName)
            {
                var field = AccessTools.Field(type, fieldName);
                if (field != null) return Result.Success(field);

                return Result.Error(() =>
                {
                    Plugin.Logger.LogWarning($"無法找到 {type.Name}.{fieldName} 欄位！{noSupport}");
                });
            }
        }
    }
}