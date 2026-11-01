using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Items;
using Kingmaker.Items;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    sealed class Identity059:IEqualityComparer<object>{public new bool Equals(object a,object b)=>ReferenceEquals(a,b);public int GetHashCode(object a)=>RuntimeHelpers.GetHashCode(a);}
    static IEnumerable<object> Graph059(object value,HashSet<object> seen){
        if(value==null||value is string||value.GetType().IsValueType||value is UnityEngine.Object||value is SimpleBlueprint||value is BlueprintReferenceBase||!seen.Add(value))yield break;
        yield return value;if(value is IEnumerable rows){foreach(var row in rows)foreach(var child in Graph059(row,seen))yield return child;yield break;}
        for(var t=value.GetType();t!=null&&t!=typeof(object)&&t!=typeof(BlueprintComponent)&&t!=typeof(Element);t=t.BaseType)
            foreach(var field in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(f=>!f.IsStatic))foreach(var child in Graph059(field.GetValue(value),seen))yield return child;
    }
    static int EmptyNativeLinks059;
    static void NativeEmptyLinks059(string __0,object __1){
        if(__0!="Psychic remaining disciplines and amplifications")return;
        var assets=(IDictionary)AccessTools.Field(__1.GetType(),"Assets").GetValue(__1);
        foreach(BlueprintScriptableObject bp in assets.Values)foreach(var link in Graph059(bp.ComponentsArray,new HashSet<object>(new Identity059())).OfType<WeakResourceLink>()){
            link.AssetId=null;EmptyNativeLinks059++;
        }
    }
    static void Feedback059(BlueprintsCache parent,Harmony h)
    {
        EquipmentPatches(h);h.Patch(AccessTools.Method(T("PsychicOriginalIcons"),"Load"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Icon057)));
        foreach(string name in new[]{"PhysicalMutationCount01459Patch","PhysicalMutationPaidSlot01459Patch"}){var type=Main.GetType("CallOfTheWild.Wrath.Prototype."+name);if(type!=null)h.CreateClassProcessor(type).Patch();}
        var chosen=Environment.GetEnvironmentVariable("COTW_01454_CASE");
        var empty=new PrefabLink();empty.AssetId=null;bool nativeThrows=false;try{empty.GetHashCode();}catch(NullReferenceException){nativeThrows=true;}
        Check(nativeThrows,"059 actual native WeakResourceLink.GetHashCode reproduces reported null-AssetId exception");
        var c=PsychicCache057(parent);Register(c,"MutationMind01457");
        var beforePools=new[]{A<BlueprintFeatureSelection>(c,"DisciplineSelection"),A<BlueprintFeatureSelection>(c,"PhrenicAmplificationSelection"),A<BlueprintFeatureSelection>(c,"ExtraPhrenicAmplificationFeat")};
        var originals=beforePools.Select(p=>p.AllFeatures.ToArray()).ToArray();
        if(chosen=="graph059")h.Patch(AccessTools.Method(T("SpellBatch01447"),"Publish"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(NativeEmptyLinks059)));
        EmptyNativeLinks059=0;bool reported=false,success=false;Exception failure=null;
        Action<string,bool,Exception> report=(name,ok,error)=>{reported=true;success=ok;failure=error;};
        Call(T("BlueprintRegistration"),"RegisterModule","Psychic remaining disciplines and amplifications",new Action(()=>Register(c,"PsychicClassCompletion01458")),report);
        if(chosen=="graph059")Check(EmptyNativeLinks059>0,"059 actual staged producer contains native weak links; fixture uses real null links before Publish without replacing registration logic");
        if(failure!=null)Console.WriteLine("059 actual module failure: "+failure);
        Check(reported&&success&&failure==null,"059 actual RegisterModule publishes all three disciplines and six amplifications with native empty links");
        for(int i=0;i<beforePools.Length;i++)Check(originals[i].All(v=>beforePools[i].AllFeatures.Contains(v)),"059 original parent selection entries retained: "+i);
        foreach(string key in new[]{"Pain","Psychedelia","Abomination"})Check(beforePools[0].AllFeatures.Count(f=>f.AssetGuid==Id(key+"Discipline01458"))==1,"059 actual discipline parent contains exactly one "+key);
        string[] major={"SynapticShockFeature01458","SpaceRendingSpellFeature01458","DualAmplificationFeature01458","MimicMetamagic1Feature01458"};
        // Major choices use the existing11th-level amplification selection,
        // not an invented extra level-up pick or a second feat parent.
        foreach(string token in major){var feature=A<BlueprintFeature>(c,token);Check(feature!=null,"059 major choice published: "+token);foreach(var pool in beforePools.Skip(1)){
            Check(pool.AllFeatures.Count(f=>f.AssetGuid==feature.AssetGuid)==1,"059 normal/Extra pools retain one major choice: "+pool.name+"/"+token);
            foreach(int level in new[]{10,11,12,20}){var actor=EquipmentUnit(c,level,Psychic058);Check(Pick058(actor,pool,feature)==(level>=11),"059 actual level-up CanSelect respects Major threshold "+level+"/"+token);}
        }}
        var cls=N<BlueprintCharacterClass>(c,Psychic058);Check(cls.Progression.LevelEntries.Where(e=>e.Level==11).SelectMany(e=>e.Features).Contains(beforePools[1]),"059 actual Psychic progression grants amplification selection at level11");
        if(chosen=="graph059"){
            h.Unpatch(AccessTools.Method(T("SpellBatch01447"),"Publish"),AccessTools.Method(typeof(MagusBatch01454Tests),nameof(NativeEmptyLinks059)));
            Register(c,"PsychicClassCompletion01458");Register(c,"PsychicAmplifications");Check(true,"059 actual publication receipts repeat with native empty resource links");
        }
        if(chosen=="display059")Display059(c,h);
        if(chosen=="reach059")Reach059(c,h);
        PsychicExpansionTests.Inventory["psychicFeedback01459Contracts"]=new JObject{["passed"]=true,["case"]=chosen,["assertions"]=checks,["coverage"]=new JArray(Coverage),["emptyNativeLinksInRealProducer"]=EmptyNativeLinks059,["gameLaunchTested"]=false,["boundary"]="Actual native graph/register/selection/slot/toggle/resource/range methods; existing explicit scene, label, icon-rendering and unit-equipment inputs. No user game acceptance inferred."};
    }
    static void Display059(BlueprintsCache c,Harmony h){
        var owner=EquipmentUnit(c,12,Psychic058);Grant(owner,c,"PhysicalMutationFeature01457");var toggle=Toggle(owner,c,"PhysicalMutationToggle01458");var resource=A<BlueprintAbilityResource>(c,"PhysicalMutationResource01457");var slot=new MechanicActionBarSlotActivableAbility{ActivatableAbility=toggle};
        Set(slot,"m_UnitRef",new Kingmaker.EntitySystem.EntityRef<UnitEntityData>(owner));Check(slot.Unit==owner,"059 native actionbar slot is bound to its actual character");
        int amount=owner.Resources.GetResourceAmount(resource);Check(amount>1&&slot.GetResource()==amount&&toggle.ResourceCount==amount,"059 actual native actionbar GetResource and ResourceCount show daily mutation minutes before activation");
        Check(toggle.Blueprint.GetComponents<ActivatableAbilityResourceLogic>().Count()==0,"059 display does not add another automatic spender or zero-resource start gate");
        ToggleOn(toggle);var effect=owner.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("PhysicalMutationBuff01457"));var until=effect.EndTime;
        Check(slot.GetResource()==amount-1&&effect.TimeLeft==TimeSpan.FromMinutes(1),"059 real toggle spends one minute and native actionbar count updates exactly once");
        ToggleOff(toggle);Check(slot.GetResource()==amount-1&&effect.EndTime==until,"059 turning off preserves paid deadline and displayed resource");
        owner.Resources.Spend(resource,owner.Resources.GetResourceAmount(resource));Check(slot.GetResource()==0,"059 actual native actionbar explicitly displays exhausted zero");
        Check(!slot.IsDisabled(slot.GetResource()),"059 actual native slot remains usable at zero while remaining paid duration exists");
        ToggleOn(toggle);Check(toggle.IsOn&&effect.EndTime==until&&slot.GetResource()==0,"059 paid zero-resource reopen neither spends nor refreshes");
        ToggleOff(toggle);effect.Remove();Check(slot.IsDisabled(slot.GetResource()),"059 actual native slot rejects exhausted zero after paid effect ends");
        var other=EquipmentUnit(c,12,Psychic058);Grant(other,c,"PhysicalMutationFeature01457");var otherToggle=Toggle(other,c,"PhysicalMutationToggle01458");Check(otherToggle.ResourceCount==other.Resources.GetResourceAmount(resource),"059 native resource count uses each individual owner");
    }
    static void Reach059(BlueprintsCache c,Harmony h){
        var owner=EquipmentUnit(c,12,Psychic058);var other=EquipmentUnit(c,12,Psychic058);Grant(owner,c,"PhysicalMutationFeature01457");Grant(owner,c,"ElongatedFingersBodilyMutationFeature01457");
        var defaults=N<BlueprintCategoryDefaults>(c,"567dc59213fd9664c8cb291643439714");var all=defaults.Entries.Select(e=>e.DefaultWeapon).Where(w=>w!=null).Distinct().ToArray();
        var weapons=all.Where(w=>w.AttackType==AttackType.Melee||w.AttackType==AttackType.Ranged||w.AttackType==AttackType.Touch||w.AttackType==AttackType.RangedTouch).ToList();weapons.Add(owner.Body.EmptyHandWeapon.Blueprint);
        var before=weapons.ToDictionary(w=>w,w=>owner.Descriptor.GetWeaponRange(w).Value);
        var dagger=all.First(w=>w.Category==Kingmaker.Enums.WeaponCategory.Dagger).CreateEntity<Kingmaker.Items.ItemEntityWeapon>();
        using(Kingmaker.ElementsSystem.ContextData<Kingmaker.Items.ItemEntity.CanBeEquippedForce>.Request())Check(owner.Body.PrimaryHand.InsertItem(dagger),"059 real native dagger inserts into actual character hand");
        Check(dagger.Wielder==owner.Descriptor,"059 ordinary weapon positive control has native Wielder rather than unbound fixture fallback");int equippedBefore=dagger.AttackRange.Value;
        var unbound=owner.Body.EmptyHandWeapon;int unboundBefore=unbound.AttackRange.Value;Check(unbound.Wielder==null,"059 standalone fallback is explicitly unbound and uses native blueprint-range branch");
        var toggle=Toggle(owner,c,"PhysicalMutationToggle01458");ToggleOn(toggle);
        Check(dagger.AttackRange.Value==equippedBefore+5,"059 actual native equipped ItemEntityWeapon.AttackRange dispatch gains exactly5ft");
        Check(unbound.AttackRange.Value==unboundBefore,"059 unbound item range stays unchanged because it has no owner mutation");
        foreach(var w in weapons){int add=w.AttackType==AttackType.Melee||w.AttackType==AttackType.Touch?5:0;Check(owner.Descriptor.GetWeaponRange(w).Value==before[w]+add,"059 actual native weapon range changes only melee exactly5ft: "+w.name+"/"+w.AttackType);Check(other.Descriptor.GetWeaponRange(w).Value==before[w],"059 other owner range remains unchanged: "+w.name);}
        Check(weapons.Any(w=>w.AttackType==AttackType.Ranged)&&weapons.Any(w=>w.AttackType==AttackType.Melee),"059 native category defaults contain both ranged and melee positive/negative controls");
        var effect=owner.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("PhysicalMutationBuff01457"));ToggleOff(toggle);effect.Remove();Check(dagger.AttackRange.Value==equippedBefore,"059 actual equipped item range restores on mutation expiry");foreach(var w in weapons)Check(owner.Descriptor.GetWeaponRange(w).Value==before[w],"059 actual mutation expiry restores native reach: "+w.name);
    }
}
