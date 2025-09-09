// Copyright (c) 2025 Yuieii.

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace ue.Peak.TcnPatch.Patches;

[HarmonyPatch]
public class LoadingScreenAnimationPatch
{
    private static bool _loadingScreenAnimationStartTranspiled;

    [HarmonyPatch(typeof(LoadingScreenAnimation), "Start")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> PatchLoadingAnimationSwitch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        if (Plugin.HasOfficialTcn)
        {
            // Now (?) we have official Traditional Chinese.
            // The game should now interpret the loading animation in its own way.
            _loadingScreenAnimationStartTranspiled = true;
            return instructions;
        }
        
        Plugin.Logger.LogInfo("正在修補 LoadingScreenAnimation.Start() 的 IL code...");
        var list = instructions.ToList();
        var index = -1;
        var branch = -1;
        
        for (var i = 0; i < list.Count; i++)
        {
            var c = list[i];
            if (!c.LoadsField(ReflectionMembers.Fields.CurrentLanguage)) continue;

            // ldc.i4.s has a `sbyte` operand
            var nextInstruction = list[i + 1];
            if (nextInstruction.opcode != OpCodes.Ldc_I4_S) continue;
            if (!nextInstruction.operand.Equals((sbyte)LocalizedText.Language.SimplifiedChinese)) continue;

            index = i;
            branch = i + 3;
            break;
        }

        if (index == -1 || branch == -1)
        {
            Plugin.Logger.LogWarning("沒有在 LoadingScreenAnimation.Start() 找到合適的對應 branch！");
            Plugin.Logger.LogWarning("無法以 IL 方式修補 LoadingScreenAnimation.Start()！將會使用 Postfix 方式修補文字。");
            return list;
        }
        
        // What we are trying to add:
        // + ldsfld       valuetype LocalizedText/Language LocalizedText::CURRENT_LANGUAGE
        // + ldc.i4.s     LocalizedText.Language.TraditionalChinese
        // + beq.s        **Runs branch of LocalizedText.Language.SimplifiedChinese**

        Plugin.Logger.LogInfo("正在插入新的 IL code...");
        var branchTarget = list[branch];
        var label = generator.DefineLabel();
        branchTarget.labels.Add(label);
        
        var insertions = new List<CodeInstruction>
        {
            CodeInstruction.LoadField(typeof(LocalizedText), nameof(LocalizedText.CURRENT_LANGUAGE)),
            new CodeInstruction(OpCodes.Ldc_I4_S, (int)LocalizedText.Language.TraditionalChinese),
            new CodeInstruction(OpCodes.Beq_S, label)
        };

        list.InsertRange(index, insertions);
        _loadingScreenAnimationStartTranspiled = true;
        return list;
    }

    [HarmonyPatch(typeof(LoadingScreenAnimation), "Start")]
    [HarmonyPostfix]
    private static void PatchLoadingAnimation(LoadingScreenAnimation __instance)
    {
        if (_loadingScreenAnimationStartTranspiled) return;
        
        var text = LocalizedText.GetText("LOADING");
        
        if (ReflectionMembers.Fields.LoadingScreenString == null)
        {
            Plugin.Logger.LogWarning("Patcher 找不到欄位: LoadingScreenAnimation.loadingString");
            return;
        }
        
        if (ReflectionMembers.Fields.LoadingScreenDefaultStringLength == null)
        {
            Plugin.Logger.LogWarning("Patcher 找不到欄位: LoadingScreenAnimation.defaultLoadingStringLength");
            return;
        }
        
        Plugin.Logger.LogInfo("正在修正載入中動畫的文字 @ LoadingScreenAnimation.Start()...");
        
        var loadingString = $"{text}...{text}...{text}...{text}...";
        __instance.SetReflectionFieldValue(ReflectionMembers.Fields.LoadingScreenString, loadingString);
        __instance.SetReflectionFieldValue(ReflectionMembers.Fields.LoadingScreenDefaultStringLength, loadingString.Length);
    }
}