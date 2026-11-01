using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using Newtonsoft.Json.Linq;

internal static class Launcher
{
    public static int Main(string[] args) {
        if(args.Length!=5)return 2;
        AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{
            var name=new AssemblyName(e.Name).Name;
            if(name=="0Harmony")return Assembly.LoadFrom(args[2]);
            foreach(var dir in new[]{args[0],args[3],Path.GetDirectoryName(args[1])}){
                var path=Path.Combine(dir,name+".dll");if(File.Exists(path))return Assembly.LoadFrom(path);
            }return null;
        };
        try {Assembly.LoadFrom(args[2]);typeof(Launcher).Assembly.GetType("Checks").GetMethod("Run").Invoke(null,new object[]{args});return 0;}
        catch(Exception e){while(e!=null){Console.Error.WriteLine(e.Message+"\n"+e.StackTrace);e=e.InnerException;}return 1;}
    }
}
internal static class Checks
{
    static int count,starts,ticks,interrupts,spends;
    static bool inRange=true,turnBased=true,combat=true,hold,failStart;
    static UnitAttack latest;
    static readonly Dictionary<BuffCollection,Buff> Prepared=new();
    static int consumed;
    static bool GetPrepared(BuffCollection __instance,ref Buff __result){Prepared.TryGetValue(__instance,out __result);return false;}
    static bool RemovePrepared(Buff __instance){foreach(var pair in Prepared.Where(p=>p.Value==__instance).ToArray())Prepared.Remove(pair.Key);consumed++;return false;}
    static void Prepare(UnitEntityData u){Prepared[u.Buffs]=Empty<Buff>();}
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<AbilityExecutionContext,UnitEntityData> Owners=new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<AbilityData,UnitDescriptor> AbilityOwners=new();
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Set(object o,string name,object value)=>AccessTools.Field(o.GetType(),name).SetValue(o,value);
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;}
    static HarmonyMethod Hook(string name)=>new(AccessTools.Method(typeof(Checks),name));
    static bool Caster(AbilityExecutionContext __instance,ref UnitEntityData __result){Owners.TryGetValue(__instance,out __result);return false;}
    static bool AbilityCaster(AbilityData __instance,ref UnitDescriptor __result){AbilityOwners.TryGetValue(__instance,out __result);return false;}
    static bool Combat(ref bool __result){__result=combat;return false;}
    static bool TurnBased(ref bool __result){__result=turnBased;return false;}
    static bool Init(UnitAttack __instance,UnitEntityData __0){Set(__instance,"<Executor>k__BackingField",__0);return false;}
    static bool Range(ref bool __result){__result=inRange;return false;}
    static bool Start(UnitCommand __instance){latest=(UnitAttack)__instance;starts++;if(failStart)throw new InvalidOperationException("animation boundary failure");return false;}
    static bool Tick(UnitCommand __instance){ticks++;if(!hold)Set(__instance,"<IsFinished>k__BackingField",true);return false;}
    static bool Interrupt(UnitCommand __instance){interrupts++;Set(__instance,"<IsFinished>k__BackingField",true);return false;}
    static void SwiftSetter(){spends++;}
    static UnitEntityData Unit(){
        var u=Empty<UnitEntityData>();var d=Empty<UnitDescriptor>();Set(u,"<Descriptor>k__BackingField",d);Set(d,"<Unit>k__BackingField",u);
        var proxy=FormatterServices.GetUninitializedObject(typeof(Kingmaker.EntitySystem.EntityService).GetNestedType("Proxy",BindingFlags.Public|BindingFlags.NonPublic));
        Set(proxy,"<Entity>k__BackingField",u);Set(u,"<Proxy>k__BackingField",proxy);Set(u,"<UniqueId>k__BackingField",Guid.NewGuid().ToString());
        var state=Empty<UnitCombatState>();Set(state,"Unit",u);Set(state,"Cooldown",new UnitCombatState.Cooldowns());
        Set(u,"<CombatState>k__BackingField",state);Set(u,"<Buffs>k__BackingField",new BuffCollection());return u;
    }
    static AbilityExecutionContext Context(UnitEntityData u){var c=Empty<AbilityExecutionContext>();Owners.Add(c,u);return c;}
    static AbilityData Ability(UnitEntityData u){var a=Empty<AbilityData>();AbilityOwners.Add(a,u.Descriptor);return a;}
    public static void Run(string[] args){
        RequiredHarmonyRuntime.Verify(args[2]);
        var mod=Assembly.LoadFrom(args[1]);var logic=(AbilityCustomLogic)Activator.CreateInstance(mod.GetType("MediumClass.Medium.NewComponents.AbilitySpecific.ChampionFleetChargeLogic",true));
        var restriction=(IAbilityRestriction)logic;var h=new Harmony("Medium041.FleetFixture");
        h.Patch(AccessTools.PropertyGetter(typeof(AbilityExecutionContext),"Caster"),prefix:Hook(nameof(Caster)));
        h.Patch(AccessTools.PropertyGetter(typeof(AbilityData),"Caster"),prefix:Hook(nameof(AbilityCaster)));
        h.Patch(AccessTools.PropertyGetter(typeof(UnitEntityData),"IsInCombat"),prefix:Hook(nameof(Combat)));
        h.Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController),"IsInTurnBasedCombat"),prefix:Hook(nameof(TurnBased)));
        h.Patch(AccessTools.Method(typeof(UnitAttack),"Init"),prefix:Hook(nameof(Init)));
        h.Patch(AccessTools.Method(typeof(UnitCommand),"IsUnitCloseEnough"),prefix:Hook(nameof(Range)));
        h.Patch(AccessTools.Method(typeof(UnitCommand),"Start"),prefix:Hook(nameof(Start)));
        h.Patch(AccessTools.Method(typeof(UnitCommand),"Tick"),prefix:Hook(nameof(Tick)));
        h.Patch(AccessTools.Method(typeof(UnitCommand),"Interrupt"),prefix:Hook(nameof(Interrupt)));
        h.Patch(AccessTools.PropertySetter(typeof(UnitCombatState.Cooldowns),"SwiftAction"),prefix:Hook(nameof(SwiftSetter)));
        var cache=new BlueprintsCache();AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,cache);
        var buff=new BlueprintBuff {AssetGuid=BlueprintGuid.Parse("7a15022d36a55af9370cb6b807eed758"),name="FleetPrepared"};cache.AddCachedBlueprint(buff.AssetGuid,buff);
        h.Patch(AccessTools.Method(typeof(BuffCollection),"GetBuff",new[]{typeof(BlueprintBuff)}),prefix:Hook(nameof(GetPrepared)));
        h.Patch(AccessTools.Method(typeof(Buff),"Remove"),prefix:Hook(nameof(RemovePrepared)));
        Check(logic.IsEngageUnit,"offensive weapon delivery engages enemy");
        Check(!(logic is IAbilityOnCastLogic),"attack has no second swift payment callback");
        Check(!restriction.IsAbilityRestrictionPassed(null),"null preview unavailable");
        var enemy=Unit();var target=new TargetWrapper(enemy);
        foreach(bool tb in new[]{true,false})foreach(float move in new[]{0f,3f}) {
            combat=true;turnBased=tb;var u=Unit();var cd=u.CombatState.Cooldown;
            Check(!restriction.IsAbilityRestrictionPassed(Ability(u)),"unprepared attack blocked even with free swift");
            // Native action spender is used by the separate swift command.
            u.SpendAction(UnitCommand.CommandType.Swift,false,0f);Prepare(u);
            Check(!u.HasSwiftAction(),"native swift command consumes swift in either combat mode");
            cd.StandardAction=6;cd.MoveAction=move;
            var ctx=Context(u);var data=Ability(u);int start=starts,spent=spends,used=consumed;
            for(int i=0;i<4;i++)Check(restriction.IsAbilityRestrictionPassed(data),"prepared attack allowed with swift already spent");
            using(var it=logic.Deliver(ctx,target)) {
                Check(consumed==used,"creating iterator does not consume preparation");
                Check(it.MoveNext(),"delivery starts native command");
                Check(starts==start+1&&consumed==used+1,"one attack consumes one preparation");
                Check(spends==spent&&!u.HasSwiftAction()&&cd.StandardAction==6&&cd.MoveAction==move,"delivery preserves native swift/standard/move costs");
                Check(latest.ForceFullAttack&&!latest.IsSingleAttack&&!latest.IsCharge&&latest.IsIgnoreCooldown,"native full attack supports Sudden Attack with no charge or second cost");
                Check(latest.Target==enemy,"target retained");
                Check(!restriction.IsAbilityRestrictionPassed(data),"preparation gone immediately");
                Check(!it.MoveNext(),"command finishes");
            }
            Prepare(u);using(var retry=logic.Deliver(ctx,target))Check(!retry.MoveNext(),"same execution cannot attack again after completion");
            logic.Cleanup(ctx);logic.Cleanup(ctx);Check(latest.IsFinished,"cleanup idempotent");
        }
        {
            var u=Unit();int start=starts,used=consumed;
            using(var it=logic.Deliver(Context(u),target))Check(!it.MoveNext(),"execution also rejects no preparation");
            Check(starts==start&&consumed==used,"rejected attack changes nothing");
        }
        foreach(bool missing in new[]{false,true}) {
            var u=Unit();Prepare(u);int used=consumed,start=starts;inRange=false;
            using(var it=logic.Deliver(Context(u),missing?null:target))Check(!it.MoveNext(),"invalid target rejected");
            Check(consumed==used&&starts==start&&restriction.IsAbilityRestrictionPassed(Ability(u)),"invalid target preserves preparation");inRange=true;
        }
        {
            var u=Unit();Prepare(u);var c=Context(u);int used=consumed;
            using(var it=logic.Deliver(c,target)){};
            Check(consumed==used&&restriction.IsAbilityRestrictionPassed(Ability(u)),"cancel before execution preserves preparation");
            Prepared.Remove(u.Buffs);
            using(var it=logic.Deliver(c,target))Check(!it.MoveNext(),"expiry or channel cleanup invalidates pending attack");
        }
        hold=true;
        {
            var a=Unit();var b=Unit();Prepare(a);Prepare(b);var ca=Context(a);var cb=Context(b);int stopped=interrupts;
            using(var ia=logic.Deliver(ca,target))using(var ib=logic.Deliver(cb,target)){
                Check(ia.MoveNext(),"first caster starts");var aa=latest;
                Check(ib.MoveNext(),"second caster starts");var ab=latest;
                // Even if an external effect refreshes swift, one context cannot reenter.
                Prepare(a);int spent=spends;
                using(var duplicate=logic.Deliver(ca,target))Check(!duplicate.MoveNext(),"same context cannot run twice concurrently");
                Check(spends==spent,"duplicate does not charge swift");
                logic.Cleanup(ca);Check(aa.IsFinished&&!ab.IsFinished,"canceling one caster leaves another intact");
                Check(interrupts==stopped+1,"cleanup interrupts outstanding child once");
                Check(!ia.MoveNext(),"canceled delivery exits");
            }
            Check(interrupts==stopped+2,"disposing second delivery interrupts its child");
        }
        hold=false;failStart=true;
        {
            var u=Unit();Prepare(u);var c=Context(u);int stopped=interrupts;bool threw=false;
            try{using(var it=logic.Deliver(c,target))it.MoveNext();}catch(InvalidOperationException){threw=true;}
            Check(threw&&interrupts==stopped+1,"exception during native start cleans child state");
            logic.Cleanup(c);Check(interrupts==stopped+1,"exception cleanup is idempotent");
        }
        Check(ticks>0,"native command frame boundary exercised");h.UnpatchAll(h.Id);
        File.WriteAllText(args[4],new JObject{{"passed",count},{"assemblySha256",BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(args[1]))).Replace("-","")},{"scope","Production two-stage attack DLL and native swift spender on seeded combat units; preparation lookup/removal, combat mode, range and native animation boundaries use explicit hooks. Standard action is seeded as spent by the attack command. No Unity/Mono gameplay."}}.ToString());
        Console.WriteLine("PASS: "+count+" Fleet Charge managed checks; not gameplay.");
    }
}
