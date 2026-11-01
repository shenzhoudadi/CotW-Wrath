using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;

// Native manager, fact, runtime-component, buff processor and EventBus paths.
// Initial units/game services are constructed; no replacement of the lifecycle
// under test, no manual component notifications and no post-setup state writes.
internal static class FlameJetLifecycle01430Tests
{
    static int checks, added, removed, resurrected, serial;
    static JObject resurrectBridge;
    static Assembly Host => AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetType("Migration01429Tests")!=null);
    static Type Suite => Host.GetType("Migration01429Tests");
    static Assembly Production => AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetType("CallOfTheWild.Wrath.Prototype.FlameJetHover01429")!=null);
    static object Call(string name,params object[] args)=>AccessTools.Method(Suite,name).Invoke(null,args);
    static void Set(object value,string name,object field)=>(AccessTools.Field(value.GetType(),name)??AccessTools.Field(value.GetType(),"<"+name+">k__BackingField")).SetValue(value,field);
    static object Get(object value,string name)=>(AccessTools.Field(value.GetType(),name)??AccessTools.Field(value.GetType(),"<"+name+">k__BackingField")).GetValue(value);
    static T Asset<T>(BlueprintsCache cache,string token)where T:SimpleBlueprint=>(T)AccessTools.Method(Suite,"Asset").MakeGenericMethod(typeof(T)).Invoke(null,new object[]{cache,token});
    static void Check(bool ok,string reason){if(!ok)throw new Exception("FlameJetLifecycle01430: "+reason);checks++;}
    static bool False(ref bool __result){__result=false;return false;}
    static bool Skip()=>false;
    static bool Time(ref TimeSpan __result){__result=TimeSpan.FromHours(1);return false;}
    static bool UniqueId(EntityFact __instance){if(string.IsNullOrEmpty(__instance.UniqueId))Set(__instance,"UniqueId","01430-fact-"+(++serial));return true;}
    static void FaultAppend(){throw new InvalidOperationException("01430 flight observer rollback injection");}
    static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");}
    static string IlText(List<CodeInstruction> code){
        return string.Join("\n",code.Select((i,n)=>n.ToString("D4")+" "+i.opcode+" "+(i.operand is Label label?"instruction:"+code.FindIndex(c=>c.labels.Contains(label)):i.operand is Label[] labels?string.Join(",",labels.Select(l=>code.FindIndex(c=>c.labels.Contains(l)))):i.operand is Type type?type.FullName:i.operand is MemberInfo member?member.DeclaringType.FullName+"::"+member:i.operand)));
    }
    static IEnumerable<CodeInstruction> SceneCalls(IEnumerable<CodeInstruction> instructions){
        // Standalone CLR cannot JIT Unity ECalls even in this absent-view branch.
        // Preserve native death/HP/condition, buff removal, resurrection event
        // and life-state paths; skip only the original `if (Unit.View)` block.
        var code=instructions.ToList();string before=IlText(code);int view=code.FindIndex(i=>i.operand is MethodInfo m&&m.DeclaringType==typeof(UnitEntityData)&&m.Name=="get_View");
        if(view<2||code[view+2].opcode!=OpCodes.Brfalse)throw new Exception("Native resurrect View boundary changed");
        var target=(Label)code[view+2].operand;int end=code.FindIndex(view+3,i=>i.labels.Contains(target));int start=view-2;
        if(end<=start||!code.Skip(end).Take(5).Any(i=>i.operand is MethodInfo m&&m.Name=="set_IsHiddenBecauseDead"))throw new Exception("Native resurrect scene-block end changed");
        var critical=new[]{"set_IsFinallyDead","RemoveConditionAll","set_ForceKill","set_MarkedForDeath","set_Damage","get_Damage","RemoveBuffsOnResurrect","RaiseEvent","ForceUnitConscious"};
        var counts=critical.ToDictionary(n=>n,n=>code.Count(i=>i.operand is MethodInfo m&&m.Name==n));
        if(counts.Any(p=>p.Value==0))throw new Exception("Native resurrect required lifecycle call missing");
        string removed=IlText(code.Skip(start).Take(end-start).ToList());int removedCount=end-start;
        if(removedCount!=142||Hash(Encoding.UTF8.GetBytes(removed))!="8EF538785EC7BEB3480DB110B2C18AA8A3B3CA1C2B797BFC07389B23C94A9E92")throw new Exception("Native resurrect exact 142-instruction View boundary changed");
        var entryLabels=code[start].labels.ToArray();code.RemoveRange(start,end-start);code[start].labels.AddRange(entryLabels);
        foreach(var pair in counts)if(code.Count(i=>i.operand is MethodInfo m&&m.Name==pair.Key)!=pair.Value)throw new Exception("Scene boundary altered critical native call "+pair.Key);
        string after=IlText(code);resurrectBridge["removedInstructionCount"]=removedCount;resurrectBridge["removedStartOffset"]="0x01cf";resurrectBridge["removedEndOffsetExclusive"]="0x039d";
        resurrectBridge["originalInstructionSha256"]=Hash(Encoding.UTF8.GetBytes(before));resurrectBridge["transformedInstructionSha256"]=Hash(Encoding.UTF8.GetBytes(after));resurrectBridge["removedInstructionSha256"]=Hash(Encoding.UTF8.GetBytes(removed));resurrectBridge["preservedCriticalCalls"]=JObject.FromObject(counts);
        string evidence=Environment.GetEnvironmentVariable("COTW_FLAME_01430_EVIDENCE_DIR");
        if(!string.IsNullOrEmpty(evidence)){Directory.CreateDirectory(evidence);File.WriteAllText(Path.Combine(evidence,"Resurrect-original.instructions.txt"),before);File.WriteAllText(Path.Combine(evidence,"Resurrect-transformed.instructions.txt"),after);File.WriteAllText(Path.Combine(evidence,"Resurrect-removed-view.instructions.txt"),removed);File.WriteAllText(Path.Combine(evidence,"Resurrect-bridge.json"),resurrectBridge.ToString());}
        return code;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ResurrectNative(UnitDescriptor owner,UnitEntityData initiator,float health,bool restoreHealth,bool fullRestore){
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)=>SceneCalls(instructions);
        Transpiler(null);throw new InvalidOperationException("Native resurrect reverse patch not installed");
    }
    sealed class Observer:IUnitBuffHandler,IUnitResurrectedHandler
    {
        public void HandleBuffDidAdded(Buff buff){added++;}
        public void HandleBuffDidRemoved(Buff buff){removed++;}
        public void HandleUnitResurrected(UnitEntityData unit){resurrected++;}
    }
    static UnitEntityData Unit(BlueprintsCache cache,string label)
    {
        var cls=(BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("ba34257984f4c41408ce1dc2004e342e"));
        var u=(UnitEntityData)Call("Unit",cache,"01430-flight-"+label,cls,10);
        var neutral=new BlueprintUnit {name="01430-neutral-"+label,AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),ComponentsArray=Array.Empty<BlueprintComponent>()};neutral.OnEnable();
        Set(u.Descriptor,"Blueprint",neutral);Set(u.Descriptor,"OriginalBlueprint",neutral);
        Set(u,"Buffs",u.Facts.EnsureFactProcessor<BuffCollection>());
        Set(u.Progression,"Features",u.Facts.EnsureFactProcessor<FeatureCollection>());
        foreach(var field in u.State.Features.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
            if(field.FieldType==typeof(CountableFlag)&&field.GetValue(u.State.Features)==null)field.SetValue(u.State.Features,new CountableFlag());
        foreach(string name in new[]{"m_ConditionsExceptions","m_StatusEffects"}){
            var field=AccessTools.Field(typeof(UnitState),name);
            if(field.GetValue(u.State)==null)field.SetValue(u.State,field.FieldType.IsArray?Array.CreateInstance(field.FieldType.GetElementType(),Enum.GetValues(typeof(UnitCondition)).Cast<UnitCondition>().Max(v=>(int)v)+1):Activator.CreateInstance(field.FieldType));
        }
        Set(u,"CombatState",FormatterServices.GetUninitializedObject(AccessTools.Property(typeof(UnitEntityData),"CombatState").PropertyType));
        u.Stats.Speed.BaseValue=30;u.Stats.HitPoints.BaseValue=100;
        u.Facts.TurnOn();return u;
    }
    static Buff Add(UnitEntityData u,BlueprintBuff bp)
    {
        var buff=new Buff(bp,new MechanicsContext(u,u.Descriptor,bp),null);
        var result=u.Facts.Add(buff);
        Check(ReferenceEquals(result,buff)&&buff.IsActive&&buff.IsTurnedOn&&buff.IsSubscribedOnEventBus,"native Add attaches, activates and subscribes "+bp.name);
        return buff;
    }
    static int Flying(UnitEntityData u)=>u.State.Features.Flying.Count;
    static bool Ground(UnitEntityData u)=>(bool)AccessTools.Method(Production.GetType("CallOfTheWild.Wrath.Prototype.PsychicFlightGroundPatch"),"Protected").Invoke(null,new object[]{u});
    static void State(UnitEntityData u,int speed,int flying,bool ground,string reason){
        Check(u.Stats.Speed.ModifiedValue==speed,reason+" speed expected "+speed+" actual "+u.Stats.Speed.ModifiedValue);
        Check(Flying(u)==flying,reason+" Flying counter expected "+flying+" actual "+Flying(u));
        Check(Ground(u)==ground,reason+" ground guard");
        Check(u.State.HasConditionImmunity(UnitCondition.DifficultTerrain)==(flying>0),reason+" native difficult-terrain immunity");
    }
    static void Restart(UnitEntityData u,int speed,int flying,bool ground,string label){
        int priorAdded=added,priorRemoved=removed;
        u.Facts.TurnOff();State(u,30,0,false,label+" off");
        u.Facts.TurnOn();State(u,speed,flying,ground,label+" on");
        Check(added==priorAdded&&removed==priorRemoved,label+" reenable emitted no synthetic added/removed events");
    }
    static void SpeedLifecycle(BlueprintsCache cache)
    {
        var hb=Asset<BlueprintBuff>(cache,"FlameJetHoverBuff");var sb=Asset<BlueprintBuff>(cache,"OverlandFlightBuff");
        foreach(bool reverse in new[]{false,true})foreach(bool removeHoverFirst in new[]{false,true}){
            var u=Unit(cache,"speed-"+reverse+"-"+removeHoverFirst);Buff hover,spell;
            if(reverse){spell=Add(u,sb);hover=Add(u,hb);}else{hover=Add(u,hb);spell=Add(u,sb);}
            State(u,40,2,true,"both native add order "+reverse);
            Restart(u,40,2,true,"native manager restart order "+reverse);
            spell.TurnOff();State(u,40,1,true,"Overland off restores hover");
            spell.TurnOn();State(u,40,2,true,"Overland on removes duplicate hover speed");
            hover.TurnOff();State(u,40,1,true,"hover off retains spell");
            hover.TurnOn();State(u,40,2,true,"hover on with active spell");
            // The engine's suppression transition deactivates/activates actual facts.
            spell.Deactivate();State(u,40,1,true,"Overland deactivate");
            spell.Activate();State(u,40,2,true,"Overland activate");
            hover.Deactivate();State(u,40,1,true,"hover deactivate");
            hover.Activate();State(u,40,2,true,"hover activate");
            Restart(u,40,2,true,"restart from restored active state");
            u.Facts.Remove(removeHoverFirst?hover:spell);State(u,40,1,true,"first source removed "+removeHoverFirst);
            Check((removeHoverFirst?hover:spell).IsDisposed,"first source native disposal");
            u.Facts.Remove(removeHoverFirst?spell:hover);State(u,30,0,false,"last source removed");
            Check(u.Facts.List.Count==0,"native remove leaves no flight facts");u.Facts.TurnOff();
        }
        var alone=Unit(cache,"spell-alone");var only=Add(alone,sb);State(alone,40,1,true,"Overland without hover");
        Restart(alone,40,1,true,"Overland alone restart");only.Deactivate();State(alone,30,0,false,"Overland alone deactivate");only.Activate();State(alone,40,1,true,"Overland alone reactivate");alone.Facts.Remove(only);State(alone,30,0,false,"Overland alone remove");alone.Facts.TurnOff();
    }
    static void DeathLifecycle(BlueprintsCache cache)
    {
        var hb=Asset<BlueprintBuff>(cache,"FlameJetHoverBuff");var sb=Asset<BlueprintBuff>(cache,"OverlandFlightBuff");
        // An independent source uses native Flying/immunity/stat components and
        // its own source identity. Its persistence models a permanent wings fact.
        var wing=new BlueprintBuff {name="01430ExternalPermanentFlight",AssetGuid=BlueprintGuid.Parse("cdb3c813d4494a209050c79e23cb767a"),Stacking=StackingType.Stack};
        var flying=new AddMechanicsFeature();Set(flying,"m_Feature",Enum.Parse(AccessTools.Field(typeof(AddMechanicsFeature),"m_Feature").FieldType,"Flying"));
        flying.name="ExternalFlying";
        wing.ComponentsArray=new BlueprintComponent[]{flying,new AddConditionImmunity {name="ExternalTerrain",Condition=UnitCondition.DifficultTerrain},new AddStatBonus {name="ExternalSpeed",Stat=StatType.Speed,Value=7,Descriptor=ModifierDescriptor.UntypedStackable}};
        Set(wing,"m_Flags",Enum.ToObject(Get(wing,"m_Flags").GetType(),8));wing.OnEnable();
        var removeOnResurrect=new BlueprintBuff {name="01430ResurrectionRemovalControl",AssetGuid=BlueprintGuid.Parse("a85b68e2e4cb45b0b6925c5a769a211a"),Stacking=StackingType.Stack,ComponentsArray=Array.Empty<BlueprintComponent>()};
        var flagType=Get(removeOnResurrect,"m_Flags").GetType();Set(removeOnResurrect,"m_Flags",Enum.ToObject(flagType,8|Convert.ToInt32(Enum.Parse(flagType,"RemoveOnResurrect"))));removeOnResurrect.OnEnable();
        foreach(bool external in new[]{false,true}){
            var u=Unit(cache,"death-"+external);var other=external?Add(u,wing):null;
            var greater=Asset<BlueprintFeature>(cache,"FlameJetGreaterWildTalent");var host=(Feature)greater.CreateFact(null,u.Descriptor,null);u.Facts.Add(host);
            Check(host.IsActive&&host.IsTurnedOn,"Greater activated through native manager");
            var hover=u.Buffs.Enumerable.Single(b=>b.Blueprint==hb);var grant=host.Components.Single(r=>r.SourceBlueprintComponent is AddFacts);
            Check(grant.GetData<AddFactsData>().AppliedFacts.Single()==hover,"native AddFacts owns actual granted hover");
            var spell=Add(u,sb);State(u,external?47:40,external?3:2,true,"before death");
            var resurrectionControl=Add(u,removeOnResurrect);
            // Construct the dead input through native UnitState, then execute the
            // production buff-death cleanup and resurrection method unmodified.
            u.State.UpdateLifeState(UnitLifeState.Dead);u.State.IsFinallyDead=true;u.State.ForceKill=true;
            Check(u.State.IsDead,"native state is dead before buff tick");
            u.Buffs.UpdateOnDeadOwner();
            Check(spell.IsDisposed&&!u.Facts.List.Contains(spell),"death tick actually removes ordinary Overland");
            Check(u.Facts.List.Contains(hover)&&!hover.IsDisposed,"death tick retains the same owned hover instance");
            Check(u.Facts.List.Contains(resurrectionControl)&&!resurrectionControl.IsDisposed,"StayOnDeath control reaches resurrection removal path");
            State(u,external?47:40,external?2:1,true,"after death tick");
            int before=resurrected;ResurrectNative(u.Descriptor,u,0,true,false);
            Check(resurrected==before+1,"native resurrection publishes real resurrected event");
            Check(u.State.IsConscious&&!u.State.IsDead&&!u.State.IsFinallyDead&&!u.State.ForceKill,"native resurrection clears death state");
            Check(resurrectionControl.IsDisposed&&!u.Facts.List.Contains(resurrectionControl),"native RemoveBuffsOnResurrect really removes its marked control");
            Check(u.Facts.List.Contains(hover)&&ReferenceEquals(grant.GetData<AddFactsData>().AppliedFacts.Single(),hover),"resurrection retains original AddFacts ownership");
            State(u,external?47:40,external?2:1,true,"after native resurrection");
            u.Buffs.UpdateOnDeadOwner();
            var restoredSpell=Add(u,sb);
            u.Facts.Remove(host);
            Check(hover.IsDisposed&&!u.Facts.List.Contains(hover),"removing Greater executes native AddFacts.Clear and disposes owned hover");
            Check(u.Facts.List.Contains(restoredSpell)&&restoredSpell.IsTurnedOn,"Greater removal preserves separately owned Overland Flight");
            State(u,external?47:40,external?2:1,true,"Greater removed with independent spell");
            u.Facts.Remove(restoredSpell);
            State(u,external?37:30,external?1:0,false,"Greater removed after resurrection");
            if(external){Check(u.Facts.List.Contains(other)&&other.IsTurnedOn,"foreign flight survives Greater removal");u.Facts.Remove(other);State(u,30,0,false,"foreign owner removes own flight");}
            Check(u.Facts.List.Count==0,"all final source bookkeeping is clean");u.Facts.TurnOff();
        }
        Check(hb.StayOnDeath&&!hb.RemoveOnResurrect,"hover persists through both death and resurrection flags");
        Check(Convert.ToInt32(Get(hb,"m_Flags"))==(Convert.ToInt32(Get(sb,"m_Flags"))|8),"StayOnDeath is added without changing other donor flags");
    }
    static void Registration(BlueprintsCache cache,Harmony h){
        var sb=Asset<BlueprintBuff>(cache,"OverlandFlightBuff");var hb=Asset<BlueprintBuff>(cache,"FlameJetHoverBuff");var original=sb.ComponentsArray;
        Func<BlueprintComponent,bool> isObserver=c=>c.GetType().Name=="FlameJetFlightLifecycle01430";
        Check(original.Count(isObserver)==1,"Overland contains one lifecycle observer");Check(!hb.ComponentsArray.Any(isObserver),"hover clone excludes observer");
        var nativeSpeed=sb.GetComponent<AddStatBonus>();Check(nativeSpeed!=null&&nativeSpeed.Value==10&&nativeSpeed.Stat==StatType.Speed&&nativeSpeed.Descriptor==ModifierDescriptor.UntypedStackable,"native Overland speed component remains unchanged");
        Check(Array.FindIndex(original,c=>ReferenceEquals(c,nativeSpeed))<Array.FindIndex(original,c=>isObserver(c)),"observer runs after native speed component");
        var register=AccessTools.Method(Production.GetType("CallOfTheWild.Wrath.Prototype.FlameJet01429"),"Register");
        register.Invoke(null,new object[]{cache});Check(ReferenceEquals(original,sb.ComponentsArray)&&sb.ComponentsArray.Count(isObserver)==1,"repeat registration does not replace or duplicate observer");
        var without=original.Where(c=>!isObserver(c)).ToArray();sb.ComponentsArray=without;
        var append=AccessTools.Method(Production.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"),"Append");
        h.Patch(append,postfix:new HarmonyMethod(typeof(FlameJetLifecycle01430Tests),nameof(FaultAppend)));
        try {
            bool threw=false;try{register.Invoke(null,new object[]{cache});}catch(TargetInvocationException ex){threw=ex.InnerException is InvalidOperationException&&ex.InnerException.Message.Contains("01430 flight observer rollback injection");}
            Check(threw,"observer registration exercises append failure");Check(ReferenceEquals(without,sb.ComponentsArray),"failed registration restores exact prior Overland component array");
        }finally{h.Unpatch(append,HarmonyPatchType.All,h.Id);sb.ComponentsArray=original;}
    }
    internal static int Run(BlueprintsCache cache)
    {
        checks=added=removed=resurrected=serial=0;var h=new Harmony("CotW01430.FlameJetNativeLifecycle");var observer=new Observer();
        var timeField=AccessTools.Field(typeof(Game),"TimeController");var game=Game.Instance;
        var originalTime=timeField.GetValue(game);var originalState=game.State;
        Set(game,"TimeController",FormatterServices.GetUninitializedObject(timeField.FieldType));
        // Earlier suites may already have inlined these short native getters.
        // Populate their real scene inputs instead of patching Player/Party.
        var player=(Player)FormatterServices.GetUninitializedObject(typeof(Player));
        Set(player,"m_Party",new List<UnitEntityData>());Set(player,"m_CharacterListsValid",true);player.GameTime=TimeSpan.FromHours(1);
        var scene=(PersistentState)FormatterServices.GetUninitializedObject(typeof(PersistentState));scene.PlayerState=player;Set(game,"State",scene);
        h.Patch(AccessTools.PropertyGetter(timeField.FieldType,"GameTime"),prefix:new HarmonyMethod(typeof(FlameJetLifecycle01430Tests),nameof(Time)));
        h.Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController),"IsInTurnBasedCombat"),prefix:new HarmonyMethod(typeof(FlameJetLifecycle01430Tests),nameof(False)));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper),"IsActive"),prefix:new HarmonyMethod(typeof(FlameJetLifecycle01430Tests),nameof(False)));
        h.Patch(AccessTools.Method(typeof(EntityFact),"Attach"),prefix:new HarmonyMethod(typeof(FlameJetLifecycle01430Tests),nameof(UniqueId)));
        h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff),"OnTrigger"),transpiler:new HarmonyMethod(AccessTools.Method(Host.GetType("Howl01428Tests"),"DifficultyBoundary")));
        h.Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),prefix:new HarmonyMethod(typeof(FlameJetLifecycle01430Tests),nameof(Skip)));
        h.Patch(AccessTools.Method(AccessTools.Property(typeof(UnitEntityData),"CombatState").PropertyType,"ReturnToStartingPositionIfNeeded"),prefix:new HarmonyMethod(typeof(FlameJetLifecycle01430Tests),nameof(Skip)));
        var nativeResurrect=AccessTools.Method(typeof(UnitDescriptor),"Resurrect",new[]{typeof(UnitEntityData),typeof(float),typeof(bool),typeof(bool)});
        string nativeHash=Hash(nativeResurrect.GetMethodBody().GetILAsByteArray());Console.WriteLine("01430 native Resurrect method IL SHA256="+nativeHash);
        Check(nativeHash=="FDAB8FBC493660EBF48DD9AAE95C13D0FD693321A511319427D9361581CCD4AB"&&nativeResurrect.GetMethodBody().GetILAsByteArray().Length==1199,"current game resurrect IL hash and 1199-byte signature match reviewed input");
        resurrectBridge=new JObject{["nativeResurrectEntryUnmodified"]=false,["execution"]="Harmony reverse patch of current game method; only absent-View scene branch removed",["nativeMethodIlSha256"]=nativeHash,["nativeMethodSize"]=nativeResurrect.GetMethodBody().GetILAsByteArray().Length,["nativeMethod"]=nativeResurrect.ToString()};
        h.CreateReversePatcher(nativeResurrect,new HarmonyMethod(typeof(FlameJetLifecycle01430Tests),nameof(ResurrectNative))).Patch();
        EventBus.Subscribe(observer);
        try {
            bool partial=Environment.GetEnvironmentVariable("COTW_FLAME_01430_DEATH_ONLY")=="1";
            if(!partial)SpeedLifecycle(cache);
            DeathLifecycle(cache);
            Registration(cache,h);
            Check(added>0&&removed>0&&resurrected==2,"actual native attach/remove/resurrect event counters");
            var inventory=(JObject)AccessTools.Field(Host.GetType("PsychicExpansionTests"),"Inventory").GetValue(null);
            inventory["flameJetLifecycle01430Contracts"]=new JObject{["passed"]=true,["isPartial"]=partial,["assertions"]=checks,["nativeAddedEvents"]=added,["nativeRemovedEvents"]=removed,["nativeResurrectedEvents"]=resurrected,["sourceOrders"]=partial?0:2,["removalOrders"]=partial?0:2,["nativeManagerRestart"]=!partial,["nativeFactActivation"]=!partial,["nativeAddFactsOwnership"]=true,["nativeDeathTickUnmodified"]=true,["nativeResurrectEntryUnmodified"]=false,["resurrectBridge"]=resurrectBridge,["boundary"]="Constructed game/unit services; controlled time, empty party, tactical/turn-based scene state, difficulty setting, unique-id allocation, feature prerequisite UI and return-position navigation. Native manager/fact/runtime/component/EventBus and BuffCollection attach/remove/death execute directly. Resurrect executes the current native IL through a reverse-patch bridge with only absent-View scene branch removed; death/HP/conditions/buff cleanup/resurrect event/ForceUnitConscious calls remain native. No complete game/save/UI test."};
            Console.WriteLine("PASS: "+checks+" Flame Jet native lifecycle checks; added="+added+", removed="+removed+", resurrected="+resurrected);return checks;
        } finally {EventBus.Unsubscribe(observer);h.UnpatchAll(h.Id);Set(game,"TimeController",originalTime);Set(game,"State",originalState);}
    }
}
