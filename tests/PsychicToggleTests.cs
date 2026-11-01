using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicToggleTests
{
    private static int count, removed, delayed;
    private static bool turnBased;
    private static Kingmaker.Game game;
    private static UnitEntityData unit;
    private static EntityFactsManager manager;
    private static readonly HashSet<Buff> active=new HashSet<Buff>();
    private static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Set(object obj,string field,object value)=>AccessTools.Field(obj.GetType(),field).SetValue(obj,value);
    private static void Check(bool ok,string message) {if(!ok) throw new Exception(message);count++;}
    private static HarmonyMethod Method(string name)=>new HarmonyMethod(AccessTools.Method(typeof(PsychicToggleTests),name));
    internal static int Run(Assembly assembly,BlueprintsCache cache)
    {
        var all=(IDictionary)AccessTools.Field(cache.GetType(),"m_LoadedBlueprints").GetValue(cache);
        var modes=all.Keys.Cast<BlueprintGuid>().Select(cache.Load).OfType<BlueprintActivatableAbility>()
            .Where(a=>a.Buff?.GetComponent<PsychicAmplificationComponent>()!=null).ToArray();
        Check(modes.Length==11,"Exercise all eleven modes of the eight phrenic amplifications");
        var register=AccessTools.Method(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAmplifications"),"Register");
        modes[0].DeactivateImmediately=false;
        bool rejected=false;
        try {register.Invoke(null,new object[]{cache});}
        catch(TargetInvocationException e) when(e.InnerException is InvalidOperationException) {rejected=true;}
        finally {modes[0].DeactivateImmediately=true;}
        Check(rejected,"Registration rejects an existing mode with conflicting deactivation behavior");

        var h=new Harmony("CotWPsychic072.NativeToggleFixture");
        game=Empty<Kingmaker.Game>();Set(game,"TimeController",Empty<Kingmaker.Controllers.TimeController>());
        // Native IsOn setter, OnDidTurnOff, Stop and restriction predicates execute.
        // The clock/combat service and final Buff removal/fact lookup are boundaries;
        // this does not run the Unity buff collection, action bar or a game round.
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Game),"Instance"),prefix:Method(nameof(Game)));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"RealTime"),prefix:Method(nameof(Time)));
        h.Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController),"IsInTurnBasedCombat"),prefix:Method(nameof(Combat)));
        h.Patch(AccessTools.Method(typeof(Buff),"Remove",Type.EmptyTypes),prefix:Method(nameof(Remove)));
        h.Patch(AccessTools.Method(typeof(Buff),"RemoveAfterDelay",new[]{typeof(TimeSpan)}),prefix:Method(nameof(Delay)));
        h.Patch(AccessTools.Method(typeof(UnitHelper),"HasFact",new[]{typeof(UnitEntityData),typeof(BlueprintFact)}),prefix:Method(nameof(HasFact)));
        unit=Empty<UnitEntityData>(); manager=new EntityFactsManager(unit);Set(unit,"Facts",manager);
        var buffs=new BuffCollection();buffs.Initialize(manager);Set(unit,"<Buffs>k__BackingField",buffs);
        var predicate=AccessTools.Method(typeof(RestrictionHasFact),"IsAvailable");
        bool Available(BlueprintActivatableAbility a)=>a.ComponentsArray.OfType<RestrictionHasFact>().All(r=>(bool)predicate.Invoke(r,null));
        try {
            foreach(bool combat in new[]{false,true}) {
                turnBased=combat;
                foreach(var bp in modes) {
                    Check(bp.DeactivateImmediately && bp.ActivationType==AbilityActivationType.Immediately,"Mode uses immediate free activation/deactivation: "+bp.name);
                    active.Clear();manager.List.Clear();removed=delayed=0;
                    // A paid defensive effect is a separate fact and must survive mode changes.
                    var defense=modes.Select(m=>m.Buff.GetComponent<PsychicAmplificationComponent>()).First(c=>c.Defense!=null).Defense.Get();
                    var paid=MakeBuff(defense);active.Add(paid);
                    var instance=MakeMode(bp);
                    Check(modes.Where(a=>a!=bp).All(Available),"Sibling modes remain clickable while another mode is active");
                    foreach(var target in modes.Where(a=>a!=bp)) {
                        var next=MakeMode(target);Activate(next);
                        Check(next.IsOn && next.AppliedBuff!=null && !instance.IsOn && instance.AppliedBuff==null,"Click another mode immediately replaces the previous mode: "+target.name);
                        Check(active.Count(b=>b.Blueprint.GetComponent<PsychicAmplificationComponent>()!=null)==1 && active.Contains(paid),"Exactly one mode and the paid AC effect survive the switch");
                        instance=next;
                    }
                    removed=0;
                    instance.IsOn=false;
                    Check(!instance.IsOn && !instance.IsStarted && instance.AppliedBuff==null && removed==1 && delayed==0,"Native switch-off removes its buff synchronously without any round tick");
                    Check(modes.All(Available),"All other modes and tiers become available immediately after native switch-off");
                    Check(active.SetEquals(new[]{paid}),"Already paid defensive AC effect is not removed with the mode");
                    instance.IsOn=false;
                    Check(removed==1 && delayed==0,"Repeated off does not remove twice");

                    // Reproduce 0.7.0 with exactly its old flags, under both combat branches.
                    bp.DeactivateImmediately=false;bp.ActivationType=AbilityActivationType.WithUnitCommand;
                    try {
                        active.Clear();manager.List.Clear();removed=delayed=0;instance=MakeMode(bp);instance.IsOn=false;
                        Check(!instance.IsOn && instance.IsStarted && active.Contains(instance.AppliedBuff) && removed==0,"Old flags reproduce the lingering mode after switch-off");
                        
                    } finally {bp.DeactivateImmediately=true;bp.ActivationType=AbilityActivationType.Immediately;}
                }
            }
        } finally {h.UnpatchAll(h.Id);active.Clear();}
        register.Invoke(null,new object[]{cache});
        PsychicExpansionTests.Inventory["psychicToggleAssertions"]=count;
        Console.WriteLine("PASS: "+count+" immediate-toggle regression assertions (native managed path, game services/removal boundaries replaced).");
        return count;
    }
    private static Buff MakeBuff(BlueprintBuff bp) {var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);Set(b,"<Manager>k__BackingField",manager);manager.List.Add(b);Refresh();return b;}
    private static ActivatableAbility MakeMode(BlueprintActivatableAbility bp) {
        var a=Empty<ActivatableAbility>();Set(a,"<Blueprint>k__BackingField",bp);Set(a,"Components",new List<EntityFactComponent>());
        Set(a,"m_IsOn",true);Set(a,"<IsStarted>k__BackingField",true);a.TimeToNextRound=6;
        var buff=MakeBuff(bp.Buff);Set(a,"m_AppliedBuff",buff);active.Add(buff);manager.List.Add(a);return a;
    }
    private static void Refresh(){var cache=AccessTools.Field(typeof(EntityFactsManager),"m_Cache").GetValue(manager);AccessTools.Method(cache.GetType(),"Clear").Invoke(cache,null);}
    private static List<Buff> Snapshot()=>active.ToList();
    private static bool Buffs(ref List<Buff> __result) {__result=Snapshot();return false;}
    private static void Activate(ActivatableAbility mode) {
        var c=mode.Blueprint.Buff.GetComponent<PsychicAmplificationComponent>();
        var runtime=c.CreateRuntimeFactComponent();runtime.Setup(c);runtime.RestoreOwnerLink(mode.AppliedBuff);
        using(runtime.RequestEventContext()) AccessTools.Method(c.GetType(),"OnTurnOn").Invoke(c,null);
    }
    private static bool Game(ref Kingmaker.Game __result) {__result=game;return false;}
    private static bool Time(ref TimeSpan __result) {__result=TimeSpan.Zero;return false;}
    private static bool Combat(ref bool __result) {__result=turnBased;return false;}
    private static bool Remove(Buff __instance) {removed++;active.Remove(__instance);manager.List.Remove(__instance);Refresh();return false;}
    private static bool Delay(Buff __instance) {delayed++;return false;}
    private static bool Owner(ref UnitEntityData __result) {__result=unit;return false;}
    private static bool HasFact(BlueprintFact __1,ref bool __result) {__result=active.Any(b=>b.Blueprint==__1);return false;}
}