using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Class.LevelUp;
using CallOfTheWild.Wrath.Prototype;

// Actual native parameter selection, pet/class identity, finesse replacement,
// completed weapon/attack calculations and final damage objects execute here.
// Native size recalculation, held-item view, Unity startup/cheat environment and
// full upgrade/save UI are bounds. IgnorePrerequisites is false, so CanSelect's
// real category, duplicate and proficiency checks are not bypassed.
internal static class FeralGraceTests
{
    static int n;
    static UnitDescriptor wielder;
    static readonly Type Module = typeof(FeralGraceDamage).Assembly.GetType("CallOfTheWild.Wrath.Prototype.FeralGraceFeat");
    static object Get(object o,string field)=>(AccessTools.Field(o.GetType(),field)??AccessTools.Field(o.GetType(),"<"+field+">k__BackingField")??throw new Exception("Feral fixture field: "+field)).GetValue(o);
    static void Set(object o,string field,object value)=>(AccessTools.Field(o.GetType(),field)??AccessTools.Field(o.GetType(),"<"+field+">k__BackingField")??throw new Exception("Feral fixture field: "+field)).SetValue(o,value);
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Check(bool value,string message){if(!value)throw new Exception("Feral Grace: "+message);n++;}
    static void Register(BlueprintsCache cache)=>AccessTools.Method(Module,"Register").Invoke(null,new object[]{cache});
    static string Const(string field)=>(string)AccessTools.Field(Module,field).GetValue(null);
    static BlueprintGuid Id()=> (BlueprintGuid)AccessTools.Method(typeof(FeralGraceDamage).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{Const("Token")});
    static T Ref<T>(SimpleBlueprint bp) where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(bp);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,bp,null);return r;}
    static bool Skip()=>false;
    static bool False(ref bool __result){__result=false;return false;}
    static bool Zero(ref int __result){__result=0;return false;}
    static bool Wielder(ref UnitDescriptor __result){__result=wielder;return false;}
    static void Refresh(UnitEntityData unit){var cache=Get(unit.Facts,"m_Cache");AccessTools.Method(cache.GetType(),"Clear").Invoke(cache,null);}
    static UnitEntityData Unit(BlueprintsCache cache,string name)
    {
        var u=Psychic034Tests.Unit(cache,name).Unit;
        var features=new FeatureCollection();features.Initialize(u.Facts);Set(u.Progression,"Features",features);
        Set(u.Descriptor,"Proficiencies",new UnitProficiency());
        Set(u.Progression,"m_CharacterLevel",(int?)8);Set(u.Progression,"m_MythicLevel",(int?)0);
        var blueprint=Psychic034Tests.Asset<BlueprintUnit>(cache,"SpiritualWeaponUnit");Set(u.Descriptor,"Blueprint",blueprint);Set(u.Descriptor,"OriginalBlueprint",blueprint);
        Set(u.Descriptor,"Body",new UnitBody(u.Descriptor));
        return u;
    }
    static Feature Fact(UnitEntityData owner,BlueprintFeature bp,FeatureParam param=null)
    {
        var f=Empty<Feature>();Set(f,"Components",new List<EntityFactComponent>());f.RestoreOwnerLink(owner.Facts);
        f.Param=param??new FeatureParam();Set(f,"Rank",1);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(f,new object[]{bp});Set(f,"IsActive",true);Set(f,"IsTurnedOn",true);
        owner.Facts.List.Add(f);Refresh(owner);return f;
    }
    static void Call(Feature fact,Type type,string method,object evt)
    {
        var runtimes=fact.Components.Where(c=>c.SourceBlueprintComponent.GetType()==type).ToArray();
        if(runtimes.Length==0)throw new Exception("Feral fixture runtime missing: "+type.Name);
        foreach(var runtime in runtimes)using(runtime.RequestEventContext())AccessTools.Method(type,method).Invoke(runtime.SourceBlueprintComponent,new[]{evt});
    }
    static ItemEntityWeapon Weapon(UnitEntityData unit,WeaponCategory category,bool natural=true,AttackType kind=AttackType.Melee)
    {
        var type=new BlueprintWeaponType{Category=category};Set(type,"m_IsNatural",natural);Set(type,"m_AttackType",kind);Set(type,"m_BaseDamage",new DiceFormula(1,DiceType.D6));
        Set(type,"m_DamageType",new DamageTypeDescription{Type=DamageType.Physical,Physical=new DamageTypeDescription.PhysicalData{Form=PhysicalDamageForm.Piercing}});
        var bp=new BlueprintItemWeapon();Set(bp,"m_Type",Ref<BlueprintWeaponTypeReference>(type));Set(bp,"m_Size",Size.Medium);
        var item=Empty<ItemEntityWeapon>();Set(item,"m_Blueprint",bp);Set(item,"HoldingSlot",unit.Body.PrimaryHand);
        Set(unit.Body.PrimaryHand,"m_FakeItem",item);Set(unit.Body.PrimaryHand,"m_IsItemFake",true);return item;
    }
    static RuleCalculateAttackBonusWithoutTarget Calculation(UnitEntityData unit,ItemEntityWeapon weapon,StatType? damageStat=StatType.Strength,bool secondary=false)
    {
        wielder=unit.Descriptor;
        var stats=new RuleCalculateWeaponStats(unit,weapon){IsSecondaryOverride=secondary};
        if(damageStat.HasValue)stats.OverrideDamageBonusStat(damageStat.Value);else Set(stats,"DamageBonusStat",(StatType?)null);
        stats.OnTrigger(null);Set(stats,"m_Triggered",true);
        var attack=new RuleCalculateAttackBonusWithoutTarget(unit,stats,0);
        return attack;
    }
    static int Bonus(Feature fact,RuleCalculateAttackBonusWithoutTarget rule)
    {
        int before=rule.WeaponStats.DamageDescription[0].CreateDamage().Bonus;
        Call(fact,typeof(FeralGraceDamage),"OnEventDidTrigger",rule);
        return rule.WeaponStats.DamageDescription[0].CreateDamage().Bonus-before;
    }
    static bool Need(Prerequisite p,UnitEntityData unit)=>(bool)AccessTools.Method(p.GetType(),"CheckInternal").Invoke(p,new object[]{null,unit.Descriptor,null});
    sealed class NativeJsonFields:DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type,MemberSerialization mode)
        {
            if(type!=typeof(FeatureParam))throw new Exception("Feral JSON boundary only accepts native FeatureParam");
            // This feat's parameter is a natural WeaponCategory. The actual
            // native Blueprint member is null, and its Unity asset contract is
            // outside this managed persistence boundary.
            return type.GetFields().Where(field=>field.Name=="WeaponCategory"&&field.IsDefined(typeof(JsonPropertyAttribute),false))
                .Select(field=>CreateProperty(field,MemberSerialization.Fields)).ToList();
        }
    }
    internal static int Run(BlueprintsCache cache)
    {
        n=0;var h=new Harmony("CotWT10.FeralGraceNative");
        void Patch(System.Reflection.MethodBase method,string prefix)=>h.Patch(method,prefix:new HarmonyMethod(typeof(FeralGraceTests),prefix));
        Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),nameof(Skip));
        Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites),"Ignore"),nameof(False));
        Patch(AccessTools.PropertyGetter(typeof(ItemEntityWeapon),"HoldInTwoHands"),nameof(False));Patch(AccessTools.PropertyGetter(typeof(ItemEntity),"EnchantmentValue"),nameof(Zero));
        Patch(AccessTools.PropertyGetter(typeof(ItemEntity),"Wielder"),nameof(Wielder));Patch(AccessTools.Method(typeof(RuleCalculateWeaponSizeBonus),"OnTrigger"),nameof(Skip));
        try {
            var finesse=cache.Load(BlueprintGuid.Parse(Const("WeaponFinesseId"))) as BlueprintFeature;
            if(finesse==null){finesse=PsychicExpansionTests.NativeCase<BlueprintFeature>(Const("WeaponFinesseId"));cache.AddCachedBlueprint(finesse.AssetGuid,finesse);}
            // Actual game pets use AnimalCompanionClass, not the monster AnimalClass.
            // Read this identity independently of the production prerequisite constant.
            const string actualAnimalClassId="26b10d4340839004f960f9816f6109fe";
            var animal=cache.Load(BlueprintGuid.Parse(actualAnimalClassId)) as BlueprintCharacterClass;
            if(animal==null){animal=PsychicExpansionTests.NativeCase<BlueprintCharacterClass>(actualAnimalClassId);cache.AddCachedBlueprint(animal.AssetGuid,animal);}
            var focus=cache.Load(BlueprintGuid.Parse(Const("WeaponFocusId"))) as BlueprintParametrizedFeature;
            if(focus==null){focus=PsychicExpansionTests.NativeCase<BlueprintParametrizedFeature>(Const("WeaponFocusId"));cache.AddCachedBlueprint(focus.AssetGuid,focus);}
            var native=PsychicExpansionTests.NativeRows.Single(r=>(string)r["blueprint"]["AssetId"]==Const("WeaponFocusId"));
            focus.ParameterType=(FeatureParameterType)Enum.Parse(typeof(FeatureParameterType),(string)native["blueprint"]["Data"]["ParameterType"]);
            focus.RequireProficiency=(bool)native["blueprint"]["Data"]["RequireProficiency"];
            var reg=typeof(FeralGraceDamage).Assembly.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration");
            var ids=new[]{(string)AccessTools.Field(reg,"BasicFeatsId").GetValue(null)}.Concat((string[])AccessTools.Field(reg,"NativeCombatFeatSelectionIds").GetValue(null)).ToArray();
            var selections=ids.Select(id=>(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id))).ToArray();
            var previous=selections.Select(s=>new[]{((BlueprintFeatureReference[])Get(s,"m_Features")).Select(r=>r.Guid).ToArray(),((BlueprintFeatureReference[])Get(s,"m_AllFeatures")).Select(r=>r.Guid).ToArray()}).ToArray();
            Register(cache);var feat=(BlueprintParametrizedFeature)cache.Load(Id());Register(cache);
            Check(ReferenceEquals(cache.Load(Id()),feat),"repeat registration preserves blueprint identity");
            Check(feat.ParameterType==FeatureParameterType.WeaponCategory&&feat.WeaponSubCategory==WeaponSubCategory.Natural&&feat.Ranks==1&&feat.RequireProficiency,"native natural-weapon parameter selection retains original proficiency rule");
            Check(feat.Groups.Contains(FeatureGroup.Feat)&&feat.Groups.Contains(FeatureGroup.CombatFeat),"ordinary combat-feat groups with pet prerequisites");
            Check(feat.ComponentsArray.Select(c=>c.name).Distinct().Count()==feat.ComponentsArray.Length,"stable component names for saves");
            for(int i=0;i<selections.Length;i++)for(int j=0;j<2;j++) {var all=(BlueprintFeatureReference[])Get(selections[i],j==0?"m_Features":"m_AllFeatures");Check(previous[i][j].All(id=>all.Any(x=>x.Guid==id)),"preexisting pool items preserved");Check(all.Count(x=>x.Guid==feat.AssetGuid)==1,"feat present exactly once in both native selection arrays");}
            var empty=new BlueprintsCache();bool reject=false;try{Register(empty);}catch{reject=true;}Check(reject&&empty.Load(Id())==null,"missing dependencies create no partial asset");
            var foreign=new BlueprintsCache();foreach(var bp in selections.Cast<SimpleBlueprint>().Concat(new SimpleBlueprint[]{finesse,animal,focus}))foreign.AddCachedBlueprint(bp.AssetGuid,bp);
            var collision=new BlueprintParametrizedFeature{AssetGuid=Id(),name="ForeignFeralGrace"};foreign.AddCachedBlueprint(collision.AssetGuid,collision);reject=false;try{Register(foreign);}catch{reject=true;}Check(reject&&ReferenceEquals(foreign.Load(Id()),collision),"foreign GUID collision is preserved and rejected");
            var owner=Unit(cache,"feral-owner");var master1=Unit(cache,"feral-master1");var master2=Unit(cache,"feral-master2");
            var pet=owner.Ensure<UnitPartPet>();Set(pet,"Type",PetType.AnimalCompanion);Set(pet,"m_MasterRef",new EntityPartRef<UnitEntityData,UnitPartPetMaster>(master1));
            var level=new ClassData(animal){Level=8};owner.Progression.Classes.Add(level);owner.Stats.BaseAttackBonus.BaseValue=6;owner.Stats.Strength.BaseValue=12;owner.Stats.Dexterity.BaseValue=18;
            Check(animal.AssetGuid.ToString()==actualAnimalClassId && Const("AnimalClassId")==actualAnimalClassId,"production recognizes actual AnimalCompanionClass independently audited from pet blueprints");
            var legacy=cache.Load(BlueprintGuid.Parse("4cd1757a0eea7694ba5c933729a53920")) as BlueprintCharacterClass;
            Check(legacy!=null && owner.Progression.GetClassLevel(legacy)==0,"ordinary companion has no monster AnimalClass levels: old prerequisite red-cross reproduction");
            Check(feat.GetComponent<PrerequisiteIsPet>().HideInUI && feat.GetComponent<FeralGraceCompanionPrerequisite>().GetUIText(owner.Descriptor).Contains("learned by the companion"),"clear pet-owned requirement replaces misleading native No pet text");
            Check(owner.IsPet&&Need(feat.GetComponent<PrerequisiteIsPet>(),owner)&&Need(feat.GetComponent<FeralGraceCompanionPrerequisite>(),owner),"actual animal pet and animal class qualify");
            var needFinesse=feat.GetComponent<PrerequisiteFeature>();Check(!Need(needFinesse,owner),"Weapon Finesse is required");var finesseFact=Fact(owner,finesse);Check(Need(needFinesse,owner),"real Weapon Finesse fact satisfies prerequisite");
            var bab=feat.GetComponent<PrerequisiteStatValue>();owner.Stats.BaseAttackBonus.BaseValue=2;Check(!bab.CheckUnit(owner.Descriptor),"BAB two denied");owner.Stats.BaseAttackBonus.BaseValue=3;Check(bab.CheckUnit(owner.Descriptor),"BAB three accepted");
            Check(feat.GetComponents<Prerequisite>().All(p=>Need(p,owner)),"all real prerequisites accept actual companion with BAB three and Finesse, no monster AnimalClass levels");
            var bite=Fact(owner,feat,WeaponCategory.Bite);var claw=Fact(owner,feat,WeaponCategory.Claw);
            var itemBite=new FeatureUIData(feat,WeaponCategory.Bite,"Bite","",null,"Bite");var itemClaw=new FeatureUIData(feat,WeaponCategory.Claw,"Claw","",null,"Claw");Set(feat,"m_CachedItems",new[]{itemBite,itemClaw});
            try {
                Check(!feat.CanSelect(owner.Descriptor,null,null,itemBite),"native parameter selection rejects already selected Bite");
                owner.Facts.List.Remove(claw);Refresh(owner);Check(feat.CanSelect(owner.Descriptor,null,null,itemClaw),"native selection permits a different natural category");owner.Facts.List.Add(claw);Refresh(owner);
                var plain=Unit(cache,"feral-untrained");Set(plain.Progression,"Features",new FeatureCollection());((FeatureCollection)Get(plain.Progression,"Features")).Initialize(plain.Facts);
                Check(plain.Descriptor.Proficiencies.Contains(WeaponCategory.Claw)&&feat.CanSelect(plain.Descriptor,null,null,itemClaw),"native default natural proficiency allows category without checking current limbs");
                plain.Descriptor.Proficiencies.Remove(WeaponCategory.Claw);Check(!plain.Descriptor.Proficiencies.Contains(WeaponCategory.Claw)&&!feat.CanSelect(plain.Descriptor,null,null,itemClaw),"actual native proficiency guard rejects artificially removed default category");
            } finally {Set(feat,"m_CachedItems",null);}
            var weapon=Weapon(owner,WeaponCategory.Bite);
            var nativeFinesse=finesse.GetComponents<AttackStatReplacement>().ToArray();Check(nativeFinesse.Any(c=>c.ReplacementStat==StatType.Dexterity&&c.SubCategory==WeaponSubCategory.Finessable)&&nativeFinesse.Any(c=>c.ReplacementStat==StatType.Dexterity&&c.SubCategory==WeaponSubCategory.Natural),"audited native Weapon Finesse contains Finessable and Natural replacement strategies; each actual runtime callback executes");
            foreach(int hd in new[]{1,3,4,7,8,12,16,20,24})foreach(bool secondary in new[]{false,true}) {
                Set(owner.Progression,"m_CharacterLevel",(int?)hd);var attack=Calculation(owner,weapon,StatType.Strength,secondary);
                Call(finesseFact,typeof(AttackStatReplacement),"OnEventAboutToTrigger",attack);attack.OnTrigger(null);
                Check(attack.AttackBonusStat==StatType.Dexterity,"native finesse selects Dexterity");Check(Bonus(bite,attack)==hd/4,"flat own-HD divided by four for primary and secondary: "+hd+"/"+secondary);
                Check(Bonus(bite,attack)==0,"same calculation cannot receive bonus twice");
                if(hd>=4)Check(attack.WeaponStats.DamageDescription[0].Modifiers.Any(m=>m.Fact==bite&&m.Value==hd/4),"native final damage modifier has Feral Grace as source");
            }
            Set(owner.Progression,"m_CharacterLevel",(int?)8);
            foreach(StatType? damageStat in new StatType?[]{StatType.Dexterity,StatType.Intelligence,null}) {var attack=Calculation(owner,weapon,damageStat);attack.AttackBonusStat=StatType.Dexterity;attack.OnTrigger(null);Check(Bonus(bite,attack)==0,"Dex/other/no damage stat excluded: "+damageStat);}
            foreach(StatType attackStat in new[]{StatType.Strength,StatType.Charisma,StatType.Intelligence}){var attack=Calculation(owner,weapon);attack.AttackBonusStat=attackStat;attack.OnTrigger(null);Check(Bonus(bite,attack)==0,"actual non-Dex attack stat excluded: "+attackStat);}
            owner.Stats.Strength.BaseValue=24;var strength=Calculation(owner,weapon);Call(finesseFact,typeof(AttackStatReplacement),"OnEventAboutToTrigger",strength);strength.OnTrigger(null);Check(strength.AttackBonusStat==StatType.Strength&&Bonus(bite,strength)==0,"native finesse chooses Strength when it is higher");owner.Stats.Strength.BaseValue=12;
            var different=Calculation(owner,Weapon(owner,WeaponCategory.Claw));different.AttackBonusStat=StatType.Dexterity;different.OnTrigger(null);Check(Bonus(bite,different)==0&&Bonus(claw,different)==2,"each selected category affects only its own natural attacks");
            owner.Facts.List.Remove(claw);Refresh(owner);different=Calculation(owner,Weapon(owner,WeaponCategory.Claw));different.AttackBonusStat=StatType.Dexterity;different.OnTrigger(null);Check(Bonus(claw,different)==0,"removed category cannot leave an active stale damage handler");var retained=Calculation(owner,weapon);retained.AttackBonusStat=StatType.Dexterity;retained.OnTrigger(null);Check(Bonus(bite,retained)==2,"removing Claw retains learned Bite bonus");owner.Facts.List.Add(claw);Refresh(owner);different=Calculation(owner,Weapon(owner,WeaponCategory.Claw));different.AttackBonusStat=StatType.Dexterity;different.OnTrigger(null);Check(Bonus(claw,different)==2,"relearning removed category restores only its own bonus");
            foreach(var excluded in new[]{Weapon(owner,WeaponCategory.Longsword,false),Weapon(owner,WeaponCategory.UnarmedStrike),Weapon(owner,WeaponCategory.Bite,true,AttackType.Ranged)}){var attack=Calculation(owner,excluded);attack.AttackBonusStat=StatType.Dexterity;attack.OnTrigger(null);Check(Bonus(bite,attack)==0,"manufactured/unarmed/ranged attack excluded");}
            Set(pet,"m_MasterRef",new EntityPartRef<UnitEntityData,UnitPartPetMaster>(master2));Check(pet.Master==master2,"actual pet master reference transferred");var transferred=Calculation(owner,weapon);transferred.AttackBonusStat=StatType.Dexterity;transferred.OnTrigger(null);Check(Bonus(bite,transferred)==2,"master transfer preserves pet-owned bonus and own HD");
            var replacement=Unit(cache,"feral-new-pet");replacement.Ensure<UnitPartPet>();replacement.Progression.Classes.Add(new ClassData(animal){Level=8});Check(!replacement.HasFact(feat),"replacement pet does not inherit former pet feat");Check(!master1.HasFact(feat)&&!master2.HasFact(feat),"master never receives pet fact");
            var preview=Unit(cache,"feral-upgrade-preview");preview.Progression.Classes.Add(new ClassData(animal){Level=8});Set(preview,"PreviewOf",new EntityRef<UnitEntityData>(owner));Check(preview.Get<UnitPartPet>()==null&&preview.IsPet&&Need(feat.GetComponent<PrerequisiteIsPet>(),preview)&&Need(feat.GetComponent<FeralGraceCompanionPrerequisite>(),preview),"actual native IsPet prerequisite and companion prerequisite both accept PreviewOf-only upgrade identity while using preview levels");
            preview.Stats.BaseAttackBonus.BaseValue=3;Fact(preview,finesse);
            Check(feat.GetComponents<Prerequisite>().All(p=>Need(p,preview)),"actual companion upgrade preview satisfies every native and custom prerequisite");
            preview.Progression.Classes.Clear();Check(!Need(feat.GetComponent<FeralGraceCompanionPrerequisite>(),preview),"preview does not borrow original class levels");
            owner.Progression.Classes.Remove(level);var legacyLevel=new ClassData(legacy){Level=8};owner.Progression.Classes.Add(legacyLevel);
            Check(Need(feat.GetComponent<FeralGraceCompanionPrerequisite>(),owner),"existing custom animal pets with original AnimalClass remain supported");
            var legacyAttack=Calculation(owner,weapon);legacyAttack.AttackBonusStat=StatType.Dexterity;legacyAttack.OnTrigger(null);Check(Bonus(bite,legacyAttack)==2,"existing pet feat identity retains its own HD damage with legacy AnimalClass");
            owner.Progression.Classes.Remove(legacyLevel);owner.Progression.Classes.Add(level);
            var savedParam=AccessTools.Property(typeof(Feature),"ParamConverter");Check(savedParam.IsDefined(typeof(JsonPropertyAttribute),false),"native Feature Param is a persisted field");
            var persisted=(FeatureParam)savedParam.GetValue(bite);Check(persisted.Blueprint==null&&persisted.WeaponCategory==WeaponCategory.Bite,"native saved Feral parameter is a weapon category without a Blueprint object graph");
            var settings=new JsonSerializerSettings{ContractResolver=new NativeJsonFields()};string json=JsonConvert.SerializeObject(persisted,settings);var restoredParam=JsonConvert.DeserializeObject<FeatureParam>(json,settings);
            Check(restoredParam.WeaponCategory==WeaponCategory.Bite,"native parameter fields round-trip through managed JSON");owner.Facts.List.Remove(bite);Refresh(owner);var restored=Fact(owner,feat,restoredParam);var reload=Calculation(owner,weapon);reload.AttackBonusStat=StatType.Dexterity;reload.OnTrigger(null);Check(Bonus(restored,reload)==2,"recreated persisted native feature retains damage category");
            Set(restored,"IsTurnedOn",false);reload=Calculation(owner,weapon);reload.AttackBonusStat=StatType.Dexterity;reload.OnTrigger(null);Check(Bonus(restored,reload)==0,"turned-off/retrained feature does not apply");Set(restored,"IsTurnedOn",true);
            owner.Facts.List.Remove(finesseFact);Refresh(owner);reload=Calculation(owner,weapon);reload.AttackBonusStat=StatType.Dexterity;reload.OnTrigger(null);Check(Bonus(restored,reload)==0,"loss of Weapon Finesse disables stale saved feature");owner.Facts.List.Add(finesseFact);Refresh(owner);
            Set(pet,"Type",PetType.MythicSkeletalChampion);Check(!Need(feat.GetComponent<FeralGraceCompanionPrerequisite>(),owner),"other native pet types excluded");reload=Calculation(owner,weapon);reload.AttackBonusStat=StatType.Dexterity;reload.OnTrigger(null);Check(Bonus(restored,reload)==0,"other pet with animal levels receives no bonus");Set(pet,"Type",PetType.AnimalCompanion);
            owner.Parts.Remove<UnitPartPet>();Check(!Need(feat.GetComponent<FeralGraceCompanionPrerequisite>(),owner),"ordinary non-pet animal is excluded");reload=Calculation(owner,weapon);reload.AttackBonusStat=StatType.Dexterity;reload.OnTrigger(null);Check(Bonus(restored,reload)==0,"non-pet with forced feat receives no damage bonus");
            var eidolon=Unit(cache,"feral-eidolon");var eidolonPet=eidolon.Ensure<UnitPartPet>();Set(eidolonPet,"Type",(PetType)604767);Check(!Need(feat.GetComponent<FeralGraceCompanionPrerequisite>(),eidolon),"unknown eidolon does not create hard dependency or bypass class requirement");
            var eidolonGuid="075743fe93d54a58bef4fae948513a6f";var original=cache.Load(BlueprintGuid.Parse(eidolonGuid));
            if(original==null) {
                var supported=new BlueprintCharacterClass{AssetGuid=BlueprintGuid.Parse(eidolonGuid),name="EidolonBaseClass",ComponentsArray=Array.Empty<BlueprintComponent>()};cache.AddCachedBlueprint(supported.AssetGuid,supported);eidolon.Progression.Classes.Add(new ClassData(supported){Level=8});
                try{Check(Need(feat.GetComponent<FeralGraceCompanionPrerequisite>(),eidolon),"exact optional Making Friends eidolon class accepted");Fact(eidolon,finesse);var eidolonGrace=Fact(eidolon,feat,WeaponCategory.Bite);var eidolonAttack=Calculation(eidolon,Weapon(eidolon,WeaponCategory.Bite));eidolonAttack.AttackBonusStat=StatType.Dexterity;eidolonAttack.OnTrigger(null);Check(Bonus(eidolonGrace,eidolonAttack)==2,"exact optional eidolon owns and receives its own HD bonus");supported.name="ForeignClass";Check(!Need(feat.GetComponent<FeralGraceCompanionPrerequisite>(),eidolon),"same GUID with wrong provider identity excluded");supported.name="EidolonBaseClass";Set(eidolonPet,"Type",PetType.Clone);Check(!Need(feat.GetComponent<FeralGraceCompanionPrerequisite>(),eidolon),"optional class alone cannot qualify another pet type");}
                finally{cache.RemoveCachedBlueprint(supported.AssetGuid);}
            }
            PsychicExpansionTests.Inventory["feralGrace0141Assertions"]=n;
            PsychicExpansionTests.Inventory["feralGrace0142Assertions"]=n;
            PsychicExpansionTests.Inventory["feralGrace0141Assets"]=new JArray(new JObject{["token"]=Const("Token"),["guid"]=feat.AssetGuid.ToString(),["type"]=feat.GetType().Name});
            Console.WriteLine("PASS: "+n+" Feral Grace native pet, parameter, own-HD and completed-stat damage checks.");return n;
        } finally {wielder=null;h.UnpatchAll(h.Id);}
    }
}
