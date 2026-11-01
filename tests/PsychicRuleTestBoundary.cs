using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;

// Latest Harmony cannot detour these shared constructed generics on desktop CLR.
// Redirect the exact call sites instead, preserving the same explicit test boundary.
// Nothing from this class is compiled into the shipped mod.
internal static class PsychicRuleTestBoundary
{
    static readonly Dictionary<Type,MethodInfo> handlers=new Dictionary<Type,MethodInfo>();
    static readonly Dictionary<Type,string> owners=new Dictionary<Type,string>();
    static readonly Dictionary<string,HashSet<MethodBase>> installed=new Dictionary<string,HashSet<MethodBase>>();
    internal static void Patch(Harmony h,Type rule,MethodInfo prefix)
    {
        handlers[rule]=prefix;owners[rule]=h.Id;
        if(!installed.ContainsKey(h.Id))installed[h.Id]=new HashSet<MethodBase>();
        var native=new[]{typeof(ContextActionSavingThrow),typeof(ContextActionSkillCheck),typeof(ContextDiceValue)};
        var types=native.Concat(typeof(CallOfTheWild.Wrath.Prototype.PsychicCrushDamage).Assembly.GetTypes());
        foreach(var type in types)
        foreach(var method in type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly))
        {
            if(method.ContainsGenericParameters||method.GetMethodBody()==null||installed[h.Id].Contains(method))continue;
            var instructions=PatchProcessor.GetOriginalInstructions(method);
            if(!instructions.Any(c=>IsRuleCall(c.operand as MethodInfo)))continue;
            h.Patch(method,transpiler:new HarmonyMethod(typeof(PsychicRuleTestBoundary),nameof(Redirect)));
            installed[h.Id].Add(method);
        }
    }
    static bool IsRuleCall(MethodInfo m)=>m!=null&&m.DeclaringType==typeof(MechanicsContext)&&m.Name=="TriggerRule"&&m.IsGenericMethod;
    static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions)
    {
        foreach(var c in instructions){if(IsRuleCall(c.operand as MethodInfo)){var m=(MethodInfo)c.operand;c.opcode=OpCodes.Call;c.operand=AccessTools.Method(typeof(PsychicRuleTestBoundary),nameof(Dispatch)).MakeGenericMethod(m.GetGenericArguments());}yield return c;}
    }
    static T Dispatch<T>(MechanicsContext context,T rule) where T:RulebookEvent
    {
        if(!handlers.TryGetValue(typeof(T),out var handler)||!Harmony.HasAnyPatches(owners[typeof(T)]))return context.TriggerRule(rule);
        var args=new object[]{rule,rule};handler.Invoke(null,args);return (T)args[1];
    }
}
