using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class Migration01429Tests
{
    static int checks, statActivations, statRemovals, resourceSpends;
    static bool failAppend;
    static BlueprintGuid faultId;
    static readonly string[] Modules = { "EnhancementSchool01429", "FlameJet01429" };
    static readonly string[] RootTokens = { "EnhancementSchool", "FlameJetWildTalent", "FlameJetGreaterWildTalent" };
    static Type Module(string n) => typeof(FlameJetHover01429).Assembly.GetType("CallOfTheWild.Wrath.Prototype."+n,true);
    static object Get(object o,string n)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).GetValue(o);
    static void Set(object o,string n,object v)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).SetValue(o,v);
    static T Helper<T>(string n,params object[] a)=>(T)AccessTools.Method(typeof(Migration01426Tests),n).Invoke(null,a);
    static BlueprintGuid Id(string t)=>Helper<BlueprintGuid>("Id",t);
    static T Asset<T>(BlueprintsCache c,string t)where T:SimpleBlueprint=>(T)c.Load(Id(t));
    static void Check(bool b,string message){if(!b)throw new Exception("Migration01429: "+message);checks++;}
    static void Register(BlueprintsCache c,string n)=>AccessTools.Method(Module(n),"Register").Invoke(null,new object[] { c });
    static string Definition(object o)=>(string)AccessTools.Method(Module("Migration01426"),"Definition", new[] { typeof(object) }).Invoke(null,new[] { o });
    static bool Skip()=>false;
    static bool No(ref bool __result){__result=false;return false;}
    static bool NoController(ref LevelUpController __result){__result=null;return false;}
    static bool PoolIgnore(BlueprintFeatureSelection __instance,ref bool __result){__result=__instance.IgnorePrerequisites;return false;}
    static void AppendFault(BlueprintFeature[] feats){if(failAppend&&feats.Any(f=>f.AssetGuid==faultId))throw new InvalidOperationException("01429 append injection");}
    static bool Rejected(Action a){try{a();return false;}catch(Exception e){while(e is TargetInvocationException)e=e.InnerException;if(e is InvalidOperationException&&e.Message.Contains("01429 append injection"))return true;throw new Exception("Unexpected pre-injection failure",e);}}
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection p,string n)=>(BlueprintFeatureReference[])Get(p,n)??Array.Empty<BlueprintFeatureReference>();
    static T Hydrate<T>(BlueprintsCache cache,string id,params string[] extra)where T:BlueprintScriptableObject,new()
    {
        var row=PsychicExpansionTests.NativeRows.Where(r=>(string)r["blueprint"]["AssetId"]==id).ToArray();Check(row.Length==1,"unique current native donor "+id);
        var prior=cache.Load(BlueprintGuid.Parse(id))as BlueprintUnitFact;var b=PsychicExpansionTests.NativeCase<T>(id);var data=row[0]["blueprint"]["Data"];
        if(b is BlueprintUnitFact&&prior!=null)Set(b,"m_Icon",Get(prior,"m_Icon"));
        foreach(var n in new[] { "Groups","Ranks","IsClassFeature","Group","Group2","m_Features","m_AllFeatures","LevelEntries","m_Classes","m_Archetypes","m_UIDeterminatorsGroup","UIGroups","GiveFeaturesForPreviousLevels" }.Concat(extra).Distinct()){
            var field=AccessTools.Field(typeof(T),n);if(field!=null&&data[n]!=null)field.SetValue(b,PsychicExpansionTests.Read(data[n],field.FieldType));
        }
        b.OnEnable();cache.AddCachedBlueprint(b.AssetGuid,b);return b;
    }
    static UnitEntityData Unit(BlueprintsCache c,string name,BlueprintCharacterClass cls,int level)
    {
        var u=Helper<UnitEntityData>("Unit",c,name);u.Progression.Classes.Clear();u.Progression.Classes.Add(new ClassData(cls){Level=level});u.Stats.Intelligence.BaseValue=18;return u;
    }
    static Feature Fact(UnitEntityData u,BlueprintFeature b)=>Helper<Feature>("Fact",u,b);
    static void Remove(Feature f){f.Owner.Facts.List.Remove(f);AccessTools.Method(Get(f.Owner.Facts,"m_Cache").GetType(),"Clear").Invoke(Get(f.Owner.Facts,"m_Cache"),null);}
    internal static int Run(BlueprintsCache cache)
    {
        checks=statActivations=statRemovals=resourceSpends=0;
        var h=new Harmony("CotW01429.NativeMigrationContracts");
        h.Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),prefix:new HarmonyMethod(typeof(Migration01429Tests),nameof(Skip)));
        h.Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites),"Ignore"),prefix:new HarmonyMethod(typeof(Migration01429Tests),nameof(No)));
        h.Patch(AccessTools.Method(typeof(BlueprintFeatureSelection),"IsIgnorePrerequisites"),prefix:new HarmonyMethod(typeof(Migration01429Tests),nameof(PoolIgnore)));
        h.Patch(AccessTools.PropertyGetter(typeof(Game),"LevelUpController"),prefix:new HarmonyMethod(typeof(Migration01429Tests),nameof(NoController)));
        h.Patch(AccessTools.Method(Module("T09CombatFeats"),"Append"),postfix:new HarmonyMethod(typeof(Migration01429Tests),nameof(AppendFault)));
        Provider01419Tests.PatchLabels(h);
        try {
            // Hydrate the actual source progression and pool metadata, not made-up
            // school grants. Retain the earlier fixture's Unity asset-only icons.
            Hydrate<BlueprintProgression>(cache,"b6a604dab356ac34788abf4ad79449ec");
            Hydrate<BlueprintFeature>(cache,"c459c8200e666ef4c990873d3e501b91");
            Hydrate<BlueprintFeature>(cache,"93919f8ce64dc5a4cbf058a486a44a1b");
            var school=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("5f838049069f1ac4d804ce0862ab5110"));
            var wild=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("5c883ae0cd6d7d5448b7a420f51f8459"));
            var rageIds=(string[])AccessTools.Field(Module("RagePowers"),"SelectionIds").GetValue(null);
            var pools=rageIds.Select(id=>(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id))).Concat(new[] { school,wild }).ToArray();
            // NativeCase replaces earlier reference-only donors. Rebind the
            // fixture's existing pool links to these hydrated objects, keeping
            // every original GUID and order; running-game donors are not replaced.
            foreach(var pool in pools)foreach(var field in new[] { "m_Features","m_AllFeatures" })Set(pool,field,Refs(pool,field).Select(reference=>{
                var current=cache.Load(reference.Guid)as BlueprintFeature;if(current==null)return reference;
                var next=BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(current);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(next,current,null);return next;
            }).ToArray());
            var before=pools.Select(p=>new[] { Refs(p,"m_Features").Select(x=>x.Guid).ToArray(),Refs(p,"m_AllFeatures").Select(x=>x.Guid).ToArray() }).ToArray();
            var loaded=(IDictionary)Get(cache,"m_LoadedBlueprints");var oldIds=new HashSet<BlueprintGuid>(loaded.Keys.Cast<BlueprintGuid>());
            var hosts=((string[])AccessTools.Field(Module("RagePowers"),"HostIds").GetValue(null)).Select(id=>(BlueprintBuff)cache.Load(BlueprintGuid.Parse(id))).ToArray();
            var hostBefore=hosts.Select(x=>Definition(x.ComponentsArray)).ToArray();
            for(int i=0;i<Modules.Length;i++){
                faultId=Id(i==0?RootTokens[0]:RootTokens[1]);failAppend=true;
                try{Check(Rejected(()=>Register(cache,Modules[i])),Modules[i]+" rejects append injection");}finally{failAppend=false;}
                Check(!loaded.Keys.Cast<BlueprintGuid>().Except(oldIds).Any(),Modules[i]+" failed publication rolls back all newly published blueprints");
                for(int j=0;j<pools.Length;j++)for(int field=0;field<2;field++)Check(Refs(pools[j],field==0?"m_Features":"m_AllFeatures").Select(x=>x.Guid).SequenceEqual(before[j][field]),"failed registration retains actual pool references");
                for(int j=0;j<hosts.Length;j++)Check(Definition(hosts[j].ComponentsArray)==hostBefore[j],"failed registration retains rage/sharing action tree");
            }
            foreach(var m in Modules)Register(cache,m);
            var newAssets=loaded.Keys.Cast<BlueprintGuid>().Except(oldIds).Select(id=>(BlueprintScriptableObject)cache.Load(id)).ToArray();
            var once=pools.Select(p=>new[] { Refs(p,"m_Features").Select(x=>x.Guid).ToArray(),Refs(p,"m_AllFeatures").Select(x=>x.Guid).ToArray() }).ToArray();
            foreach(var m in Modules)Register(cache,m);
            foreach(var a in newAssets)Check(ReferenceEquals(cache.Load(a.AssetGuid),a),"repeat registration preserves new identity "+a.name);
            for(int i=0;i<pools.Length;i++)for(int field=0;field<2;field++){
                var refs=Refs(pools[i],field==0?"m_Features":"m_AllFeatures").Select(x=>x.Guid).ToArray();
                Check(refs.Take(before[i][field].Length).SequenceEqual(before[i][field]),"real pool retains original order/entries");Check(refs.SequenceEqual(once[i][field]),"real pool append is idempotent");
            }
            Enhancement(cache,h);FlameJet(cache,h);NativeEffectLifecycle(cache,h);Overlap(cache,h);OwnedHover(cache,h);
            PsychicExpansionTests.Inventory["migration01429Contracts"]=new JObject {
                ["passed"]=true,["assertions"]=checks,["additionalItemCount"]=2,["ordinaryFeatCount"]=0,["rootFeatureCount"]=3,["newItemsThisRelease"]=0,["retainedHistoricalScope"]="Enhancement and Flame Jet/Greater only; Atavism retired by explicit user request in 01432.",
                ["publicationAndAppendRollback"]=true,["realPoolsPreserved"]=true,["repeatIdentityPreserved"]=true,
                ["nativeStatActivations"]=statActivations,["nativeStatRemovals"]=statRemovals,["nativeResourceSpendCalls"]=resourceSpends,
                ["newBlueprints"]=JArray.FromObject(newAssets.Select(a=>new{id=a.AssetGuid.ToString(),name=a.name,type=a.GetType().FullName})),
                ["boundary"]="Current game prerequisites, selection CanSelect, ability resource amounts/spend, rank/stat modifier and visibility/action/range paths execute on constructed entities. Unity scene insertion, renderer/navigation and complete game persistence remain tester cases. Original native dependencies are hydrated from game JSON; no game launch." };
            Console.WriteLine("PASS: "+checks+" retained01429 native migration contracts.");return checks;
        }finally{h.UnpatchAll(h.Id);}
    }
    static void Enhancement(BlueprintsCache cache,Harmony h)
    {
        var wizard=(BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("ba34257984f4c41408ce1dc2004e342e"));var u=Unit(cache,"01429-wizard",wizard,8);
        var root=Asset<BlueprintProgression>(cache,RootTokens[0]);var source=(BlueprintProgression)cache.Load(BlueprintGuid.Parse("b6a604dab356ac34788abf4ad79449ec"));
        Check(root.CalcLevel(u.Descriptor)==8,"native wizard school level calculation");
        Check(root.LevelEntries.Single(e=>e.Level==20).Features.Select(f=>f.AssetGuid).SequenceEqual(source.LevelEntries.Single(e=>e.Level==20).Features.Select(f=>f.AssetGuid)),"native capstone retained exactly");
        Check(root.LevelEntries.Single(e=>e.Level==1).Features.Count(f=>f is BlueprintFeatureSelection)==2,"both native opposition choices retained");
        var resources=new[] { Asset<BlueprintAbilityResource>(cache,"EnhancementAugmentResource"),Asset<BlueprintAbilityResource>(cache,"EnhancementPerfectionResource") };
        Check(resources[0].GetMaxAmount(u.Descriptor)==7&&resources[1].GetMaxAmount(u.Descriptor)==8,"native daily resource amounts3+INT and wizard level");
        foreach(var r in resources){u.Resources.Add(r,true);int old=u.Resources.GetResourceAmount(r);u.Resources.Spend(r,1);resourceSpends++;Check(u.Resources.GetResourceAmount(r)==old-1,"native resource collection pays once");}
        foreach(int level in new[] {1,8,9,10,19,20}){
            u.Progression.Classes[0].Level=level;
            foreach(var name in new[] {"AugmentStrength","AugmentDexterity","AugmentConstitution","AugmentIntelligence","AugmentWisdom","AugmentCharisma","AugmentAC","PerfectionStrength"}){
                var buff=Asset<BlueprintBuff>(cache,"Enhancement"+name+"Buff");var bf=Psychic034Tests.Fact(u,buff,u,level);bf.Context.Recalculate();
                var stat=buff.GetComponent<AddContextStatBonus>();int expected=name=="AugmentAC"?Math.Min(5,1+level/5):name.StartsWith("Perfection")?Math.Min(10,level/2):level>=10?4:2;
                Check(stat.CalculateBaseValue(bf.Context)==expected,"native rank calculation "+name+" at"+level);
                var value=u.Stats.GetStat(stat.Stat);int before=value.ModifiedValue;
                var runtime=bf.Components.Single(c=>c.SourceBlueprintComponent==stat);
                using(runtime.RequestEventContext())AccessTools.Method(typeof(AddContextStatBonus),"OnTurnOn").Invoke(stat,null);statActivations++;
                Check(value.ModifiedValue==before+expected,"native stat modifier applies calculated value");
                using(runtime.RequestEventContext())AccessTools.Method(typeof(AddContextStatBonus),"OnTurnOff").Invoke(stat,null);statRemovals++;
                Check(value.ModifiedValue==before,"native stat modifier removes its own value");u.Facts.List.Remove(bf);
            }
        }
    }
    static void FlameJet(BlueprintsCache cache,Harmony h)
    {
        var cls=(BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("42a455d9ec1ad924d889272429eb8391"));var u=Unit(cache,"01429-kineticist",cls,10);
        var part=(UnitPartKineticist)AccessTools.Method(typeof(PurifyingFlames01428Tests),"Kineticist").Invoke(null,new object[] { u,cls,StatType.Constitution,0,0 });
        foreach(var method in new[] {AccessTools.Method(typeof(AbilityData),"Spend"),AccessTools.Method(typeof(Kingmaker.UnitLogic.Class.Kineticist.AbilityKineticist),"Spend"),AccessTools.Method(typeof(UnitPartKineticist),"AcceptBurn")})h.Patch(method,transpiler:new HarmonyMethod(typeof(PurifyingFlames01428Tests),"Boundary"));
        var basic=Asset<BlueprintFeature>(cache,RootTokens[1]);var greater=Asset<BlueprintFeature>(cache,RootTokens[2]);
        var fire=basic.GetComponent<PrerequisiteFeaturesFromList>();
        foreach(var element in fire.Features){var ef=Fact(u,element);
            foreach(int level in new[]{5,6,9,10}){u.Progression.Classes[0].Level=level;Check(basic.GetComponent<PrerequisiteClassLevel>().CheckRaw(null,u.Descriptor,null)==(level>=6),"native basic level gate "+level);Check(greater.GetComponent<PrerequisiteClassLevel>().CheckRaw(null,u.Descriptor,null)==(level>=10),"native greater level gate "+level);Check(fire.CheckRaw(null,u.Descriptor,null),"each actual fire-focus alternative is accepted");}Remove(ef);
        }
        Check(!fire.CheckRaw(null,u.Descriptor,null),"no fire focus cannot select either talent");u.Progression.Classes[0].Level=10;
        var standard=Asset<BlueprintAbility>(cache,"FlameJetStandard");var move=Asset<BlueprintAbility>(cache,"FlameJetMove");
        Check(!standard.GetComponent<FlameJetUpgradeRestriction>().IsCasterRestrictionPassed(u)&&!move.GetComponent<FlameJetUpgradeRestriction>().IsCasterRestrictionPassed(u),"no selected talent rejects both modes");
        var bf=Fact(u,basic);
        Check(standard.GetComponent<FlameJetUpgradeRestriction>().IsCasterRestrictionPassed(u)&&!move.GetComponent<FlameJetUpgradeRestriction>().IsCasterRestrictionPassed(u),"basic talent grants standard mode only");
        var sd=new AbilityData(standard,u.Descriptor);var md=new AbilityData(move,u.Descriptor);
        Check(sd.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard&&md.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move,"actual native AbilityData action types differ without a global patch");
        Check(standard.GetComponent<AbilityShowIfCasterHasFact>().IsAbilityVisible(sd)&&!move.GetComponent<AbilityShowIfCasterHasFact>().IsAbilityVisible(md),"native variant visibility selects standard mode");
        var gf=Fact(u,greater);
        Check(!standard.GetComponent<FlameJetUpgradeRestriction>().IsCasterRestrictionPassed(u)&&move.GetComponent<FlameJetUpgradeRestriction>().IsCasterRestrictionPassed(u),"upgrade rejects queued obsolete standard mode");
        Check(!standard.GetComponent<AbilityShowIfCasterHasFact>().IsAbilityVisible(sd)&&move.GetComponent<AbilityShowIfCasterHasFact>().IsAbilityVisible(md),"native upgraded visibility selects move mode");
        Check(Math.Abs(standard.GetRange(false,sd).Meters-9.144f)<0.001&&Math.Abs(move.GetRange(false,md).Meters-9.144f)<0.001,"both native range calculations are30ft");
        foreach(var a in new[] {standard,move}){
            var door=a.GetComponent<AbilityCustomDimensionDoor>();Check(door.Radius.Meters==0,"nearby companions excluded");
            var burn=a.GetComponent<Kingmaker.UnitLogic.Class.Kineticist.AbilityKineticist>();Check(burn.CalculateCost(new AbilityData(a,u.Descriptor))==0,"native burn cost is zero");
            new AbilityData(a,u.Descriptor).Spend();resourceSpends++;Check(part.AcceptedBurn==0&&part.AcceptedBurnThisRound==0,"actual native Spend consumes no burn even with exhausted capacity");
            Check(!a.ComponentsArray.Any(c=>c.GetType().Name=="LineOfSightIgnorance"),"line-of-sight bypass removed from private clone");
        }
        Check(greater.GetComponent<AddFacts>().Facts.Single().AssetGuid==Id("FlameJetHoverBuff"),"greater owns a private flight effect distinct from temporary flight spells");
        Remove(gf);Check(standard.GetComponent<FlameJetUpgradeRestriction>().IsCasterRestrictionPassed(u),"removing upgrade restores basic mode");Remove(bf);
        Check(!new FlameJetUpgradeRestriction().IsCasterRestrictionPassed(null),"mode caster restriction is null safe");
    }
    static void NativeEffectLifecycle(BlueprintsCache cache,Harmony h)
    {
        h.Patch(AccessTools.Method(typeof(UnitState),"UpdateStatusEffect"),prefix:new HarmonyMethod(typeof(Migration01429Tests),nameof(Skip)));
        var cls=(BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("ba34257984f4c41408ce1dc2004e342e"));var u=Unit(cache,"01429-native-effects",cls,10);
        u.Stats.Speed.BaseValue=30; // Real units start above native speed's5ft floor.
        foreach(var field in u.State.Features.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))if(field.FieldType==typeof(CountableFlag)&&field.GetValue(u.State.Features)==null)field.SetValue(u.State.Features,new CountableFlag());
        var ferocity=(BlueprintUnitFact)cache.Load(BlueprintGuid.Parse("955e356c813de1743a98ab3485d5bc69"));
        var holder=new BlueprintFeature { name="01429NativeFerocityFixture",ComponentsArray=ferocity.ComponentsArray };holder.OnEnable();var fact=Fact(u,holder);
        var component=holder.GetComponent<AddMechanicsFeature>();var runtime=fact.Components.Single(c=>c.SourceBlueprintComponent==component);
        bool before=u.State.Features.Ferocity;
        using(runtime.RequestEventContext())AccessTools.Method(runtime.GetType(),"OnTurnOn").Invoke(runtime,null);
        Check(u.State.Features.Ferocity,"native ferocity trait is active while its granted fact is on");
        using(runtime.RequestEventContext())AccessTools.Method(runtime.GetType(),"OnTurnOff").Invoke(runtime,null);
        Check((bool)u.State.Features.Ferocity==before,"native ferocity restores previous trait state");Remove(fact);
        var flight=Asset<BlueprintBuff>(cache,"OverlandFlightBuff");
        // AddFacts uses this exact native virtual constructor with no caster.
        // Exercise it to detect null-context assumptions in the reused effect.
        var hover=(Buff)flight.CreateFact(null,u.Descriptor,null);hover.RestoreOwnerLink(u.Facts);
        Set(hover,"IsActive",true);Set(hover,"IsTurnedOn",true);u.Facts.List.Add(hover);
        Check(hover.Context!=null,"native feature-granted buff has a valid context when created without a parent caster");
        int speed=u.Stats.Speed.ModifiedValue;bool flying=u.State.Features.Flying;bool immune=u.State.HasConditionImmunity(UnitCondition.DifficultTerrain);
        var selected=hover.Components.Where(r=>r.SourceBlueprintComponent is AddStatBonus||r.SourceBlueprintComponent is AddMechanicsFeature||r.SourceBlueprintComponent is AddConditionImmunity).ToArray();
        foreach(var r in selected)using(r.RequestEventContext()){var target=r.SourceBlueprintComponent is AddMechanicsFeature?(object)r:r.SourceBlueprintComponent;AccessTools.Method(target.GetType(),"OnTurnOn").Invoke(target,null);}
        Check(u.Stats.Speed.ModifiedValue==speed+10&&u.State.Features.Flying&&u.State.HasConditionImmunity(UnitCondition.DifficultTerrain),"native flight components provide speed, flying and terrain immunity");
        foreach(var r in selected.Reverse())using(r.RequestEventContext()){var target=r.SourceBlueprintComponent is AddMechanicsFeature?(object)r:r.SourceBlueprintComponent;AccessTools.Method(target.GetType(),"OnTurnOff").Invoke(target,null);}
        Check(u.Stats.Speed.ModifiedValue==speed&&(bool)u.State.Features.Flying==flying&&u.State.HasConditionImmunity(UnitCondition.DifficultTerrain)==immune,"native flight components remove only their own contributions: speed="+u.Stats.Speed.ModifiedValue+"/before="+speed+", fly="+(bool)u.State.Features.Flying+"/before="+flying+", terrain="+u.State.HasConditionImmunity(UnitCondition.DifficultTerrain)+"/before="+immune);u.Facts.List.Remove(hover);
    }
    static void Turn(Buff b,string method){foreach(var r in b.Components)using(r.RequestEventContext())AccessTools.Method(r.SourceBlueprintComponent.GetType(),method)?.Invoke(r.SourceBlueprintComponent,null);}
    static void Notify(Buff receiver,Buff changed,bool removed){foreach(var r in receiver.Components)if(r.SourceBlueprintComponent is Kingmaker.PubSubSystem.IUnitBuffHandler handler)using(r.RequestEventContext()){if(removed)handler.HandleBuffDidRemoved(changed);else handler.HandleBuffDidAdded(changed);}}
    static void Drop(Buff b){Set(b,"IsTurnedOn",false);Turn(b,"OnTurnOff");b.Owner.Facts.List.Remove(b);AccessTools.Method(Get(b.Owner.Facts,"m_Cache").GetType(),"Clear").Invoke(Get(b.Owner.Facts,"m_Cache"),null);}
    static UnitEntityData limbOwner;
    static bool LimbLimit(WeaponCategory slot,ref int __result){__result=slot==WeaponCategory.Bite?1:slot==WeaponCategory.Claw?2:-1;return false;}
    static bool LimbInsert(Kingmaker.Items.UnitBody __instance,int inputLimbIndex,bool isSecondary,ref int __result){
        var list=(List<Kingmaker.Items.Slots.WeaponSlot>)Get(__instance,"m_AdditionalLimbs");int index=inputLimbIndex;
        if(index<0){list.Add(new Kingmaker.Items.Slots.WeaponSlot(limbOwner.Descriptor));index=list.Count-1;}
        var w=(Kingmaker.Items.ItemEntityWeapon)AccessTools.Method(typeof(SpellAndSword01417Tests),"Weapon").Invoke(null,new object[]{limbOwner,WeaponCategory.Bite,"01429-owned-limb"});w.ForceSecondary=isSecondary;
        Set(list[index],"m_FakeItem",w);Set(list[index],"m_IsItemFake",true);Set(w,"HoldingSlot",list[index]);__result=index;return false;
    }
    static bool LimbRemove(Kingmaker.Items.UnitBody __instance,int limbIndex){var list=(List<Kingmaker.Items.Slots.WeaponSlot>)Get(__instance,"m_AdditionalLimbs");Set(list[limbIndex],"m_FakeItem",null);Set(list[limbIndex],"m_IsItemFake",false);return false;}
    static void Overlap(BlueprintsCache cache,Harmony h){
        var cls=(BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("f7d7eb166b3dd594fb330d085df41853"));var u=Unit(cache,"01429-overlap",cls,10);
        var wizard=(BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("ba34257984f4c41408ce1dc2004e342e"));var caster=Unit(cache,"01429-augment-other",wizard,20);var ac=Asset<BlueprintBuff>(cache,"EnhancementAugmentACBuff");
        int baseAc=u.Stats.AC.ModifiedValue;var original=u.Stats.AC.AddModifier(7,(EntityFact)null,ModifierDescriptor.NaturalArmor);
        var weakCaster=Unit(cache,"01429-augment-weaker",wizard,10);var weak=Psychic034Tests.Fact(u,ac,weakCaster,10);weak.Context.Recalculate();Turn(weak,"OnTurnOn");Check(u.Stats.AC.ModifiedValue==baseAc+10,"augmentation stacks with unrelated natural armor");
        var strong=Psychic034Tests.Fact(u,ac,caster,20);strong.Context.Recalculate();Turn(strong,"OnTurnOn");Notify(weak,strong,false);Check(u.Stats.AC.ModifiedValue==baseAc+12,"two casters choose strongest natural armor augmentation only");
        var repeat=Psychic034Tests.Fact(u,ac,caster,20);repeat.Context.Recalculate();Turn(repeat,"OnTurnOn");Notify(weak,repeat,false);Notify(strong,repeat,false);Check(u.Stats.AC.ModifiedValue==baseAc+12,"repeat same-caster augmentation cannot stack again");
        Drop(strong);Notify(weak,strong,true);Notify(repeat,strong,true);Check(u.Stats.AC.ModifiedValue==baseAc+12,"equal surviving augmentation preserves bonus after first expiry");
        Drop(repeat);Notify(weak,repeat,true);Check(u.Stats.AC.ModifiedValue==baseAc+10,"weaker live augmentation restores after stronger expiry");Drop(weak);Check(u.Stats.AC.ModifiedValue==baseAc+7,"last augmentation preserves original natural armor");u.Stats.AC.RemoveModifier(original);
        var hoverBp=Asset<BlueprintBuff>(cache,"FlameJetHoverBuff");var spellBp=Asset<BlueprintBuff>(cache,"OverlandFlightBuff");u.Stats.Speed.BaseValue=30;
        var hover=Psychic034Tests.Fact(u,hoverBp,u,10);var hoverRuntime=hover.Components.Single(r=>r.SourceBlueprintComponent is FlameJetHover01429);var hoverComponent=(FlameJetHover01429)hoverRuntime.SourceBlueprintComponent;
        using(hoverRuntime.RequestEventContext())AccessTools.Method(typeof(FlameJetHover01429),"Refresh").Invoke(hoverComponent,null);Check(u.Stats.Speed.ModifiedValue==40,"private hover supplies10ft speed");
        var spell=Psychic034Tests.Fact(u,spellBp,u,10);var speed=spellBp.GetComponent<AddStatBonus>();var sr=spell.Components.Single(r=>r.SourceBlueprintComponent==speed);using(sr.RequestEventContext())AccessTools.Method(typeof(AddStatBonus),"OnTurnOn").Invoke(speed,null);Notify(hover,spell,false);
        Check(u.Stats.Speed.ModifiedValue==40&&u.Facts.List.Contains(hover)&&u.Facts.List.Contains(spell),"spell flight coexists with hover without doubled speed or replaced ownership");
        using(sr.RequestEventContext())AccessTools.Method(typeof(AddStatBonus),"OnTurnOff").Invoke(speed,null);u.Facts.List.Remove(spell);Set(spell,"IsTurnedOn",false);Notify(hover,spell,true);Check(u.Stats.Speed.ModifiedValue==40,"permanent hover restores speed after temporary spell expires");
        using(hoverRuntime.RequestEventContext())AccessTools.Method(typeof(FlameJetHover01429),"OnTurnOff").Invoke(hoverComponent,null);u.Facts.List.Remove(hover);Check(u.Stats.Speed.ModifiedValue==30,"hover removal cleans only its own speed");
        PsychicExpansionTests.Inventory["overlap01429Contracts"]=new JObject{["passed"]=true,["strongestNaturalArmorRestores"]=true,["privateFlightOwnership"]=true,["flightSpeedNotDoubled"]=true,["boundary"]="Retained Enhancement natural-armor choice/restoration and Flame Jet private-flight ownership/speed paths execute on actual runtime contexts, with explicit lifecycle notifications. Atavism-only overlap cases were retired by user request; native donor retention is tested separately in 01432. Full event subscriptions/rendering/persistence are game cases."};
    }
    static int ownedInsertions,ownedRemovals;
    static bool OwnedInsert(EntityFactsManager __instance,UnitFact __0,ref UnitFact __result){__0.RestoreOwnerLink(__instance);Set(__0,"IsActive",true);Set(__0,"IsTurnedOn",true);__instance.List.Add(__0);AccessTools.Method(Get(__instance,"m_Cache").GetType(),"Clear").Invoke(Get(__instance,"m_Cache"),null);__result=__0;ownedInsertions++;return false;}
    static bool OwnedRemove(UnitEntityData __0,EntityFact __1){__0.Facts.List.Remove(__1);Set(__1,"IsTurnedOn",false);AccessTools.Method(Get(__0.Facts,"m_Cache").GetType(),"Clear").Invoke(Get(__0.Facts,"m_Cache"),null);ownedRemovals++;return false;}
    static UnitFact OwnedAddEndpoint(EntityFactsManager manager,UnitFact fact){UnitFact result=null;OwnedInsert(manager,fact,ref result);return result;}
    static IEnumerable<CodeInstruction> OwnedAddBoundary(IEnumerable<CodeInstruction> source){int count=0;foreach(var i in source){if(i.operand is MethodInfo m&&m.DeclaringType==typeof(EntityFactsManager)&&m.Name=="Add"){i.opcode=OpCodes.Call;i.operand=AccessTools.Method(typeof(Migration01429Tests),nameof(OwnedAddEndpoint));count++;}yield return i;}if(count!=1)throw new Exception("Native AddFacts insertion endpoint changed");}
    static void OwnedHover(BlueprintsCache cache,Harmony h){
        ownedInsertions=ownedRemovals=0;
        var cls=(BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("42a455d9ec1ad924d889272429eb8391"));var u=Unit(cache,"01429-hover-owner",cls,10);
        var add=AccessTools.Method(typeof(AddFacts),"UpdateFacts");
        h.Patch(add,transpiler:new HarmonyMethod(typeof(Migration01429Tests),nameof(OwnedAddBoundary)));
        var remove=AccessTools.Method(typeof(UnitHelper),"RemoveFact",new[]{typeof(UnitEntityData),typeof(EntityFact)});h.Patch(remove,prefix:new HarmonyMethod(typeof(Migration01429Tests),nameof(OwnedRemove)));
        try{
            var spell=Psychic034Tests.Fact(u,Asset<BlueprintBuff>(cache,"OverlandFlightBuff"),u,10);
            var bp=Asset<BlueprintFeature>(cache,RootTokens[2]);var host=Fact(u,bp);var grant=bp.GetComponent<AddFacts>();var runtime=host.Components.Single(r=>r.SourceBlueprintComponent==grant);
            using(runtime.RequestEventContext())AccessTools.Method(typeof(AddFacts),"OnActivate").Invoke(grant,null);
            Check(ownedInsertions==1&&u.Facts.List.Contains(spell),"native AddFacts grants a separate hover without claiming existing spell flight");
            var hover=u.Facts.List.OfType<Buff>().Single(b=>b.Blueprint.AssetGuid==Id("FlameJetHoverBuff"));Check(hover.Context!=null,"native granted hover constructor has non-null context");
            using(runtime.RequestEventContext())AccessTools.Method(typeof(AddFacts),"OnDeactivate").Invoke(grant,null);
            Check(ownedRemovals==1&&!u.Facts.List.Contains(hover)&&u.Facts.List.Contains(spell),"native AddFacts.Clear removes owned hover and preserves foreign spell instance");Remove(host);u.Facts.List.Remove(spell);
            PsychicExpansionTests.Inventory["hoverOwnership01429Contracts"]=new JObject{["passed"]=true,["nativeAddFactsOnActivateAndClear"]=true,["nativeBuffConstructor"]=true,["ownedInsertions"]=ownedInsertions,["ownedRemovals"]=ownedRemovals,["foreignSpellRetained"]=true,["boundary"]="Native AddFacts.UpdateFacts/Clear and BlueprintBuff.CreateFact execute. Final EntityFactsManager insertion and UnitHelper removal are fixture endpoints; source lists and native owned AppliedFacts bookkeeping are original."};
        }finally{h.Unpatch(add,HarmonyPatchType.All,h.Id);h.Unpatch(remove,HarmonyPatchType.All,h.Id);}
    }
}
