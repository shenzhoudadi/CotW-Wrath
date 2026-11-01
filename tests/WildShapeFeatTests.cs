using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Ecnchantments;
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
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using CallOfTheWild.Wrath.Prototype;

// Native archive anchors, resource calculations, context ranks, maneuver results,
// item enchantment collections and final physical damage objects execute here.
// Unity views, polymorph animation, full save loading and buff delivery are bounds.
internal static class WildShapeFeatTests {
 static int n;
 static readonly Type Module=typeof(WeaponShiftTransfer).Assembly.GetType("CallOfTheWild.Wrath.Prototype.WildShapeFeats");
 static readonly Type Factory=typeof(WeaponShiftTransfer).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets");
 static object Get(object o,string f)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).GetValue(o);
 static void Set(object o,string f,object v)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).SetValue(o,v);
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool b,string m){if(!b)throw new Exception("Wild shape feats: "+m);n++;}
 static BlueprintGuid Id(string token)=>(BlueprintGuid)AccessTools.Method(Factory,"Id").Invoke(null,new object[]{token});
 static T Asset<T>(BlueprintsCache c,string token)where T:SimpleBlueprint=>(T)c.Load(Id(token));
 static void Register(BlueprintsCache c)=>AccessTools.Method(Module,"Register").Invoke(null,new object[]{c});
 static T Ref<T>(SimpleBlueprint bp)where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(bp);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,bp,null);return r;}
 static bool Skip()=>false;
 static bool False(ref bool __result){__result=false;return false;}
 static bool Zero(ref int __result){__result=0;return false;}
 static bool NoWielder(ref UnitDescriptor __result){__result=null;return false;}
 static void AttachId(EntityFact __instance){if(string.IsNullOrEmpty(__instance.UniqueId))Set(__instance,"UniqueId",Guid.NewGuid().ToString("N"));}
 static bool Time(ref TimeSpan __result){__result=TimeSpan.FromSeconds(100);return false;}
 static ContextActionApplyBuff applied;
 static int specialCalls;
 static bool SpecialBoundary(){specialCalls++;return false;}
 static bool Apply(ContextActionApplyBuff __instance){applied=__instance;return false;}
 static bool Remove(Buff __instance){__instance.Owner.Facts.List.Remove(__instance);Refresh(__instance.Owner);return false;}
 static void Refresh(UnitEntityData u){var x=Get(u.Facts,"m_Cache");AccessTools.Method(x.GetType(),"Clear").Invoke(x,null);}
 static UnitEntityData Unit(BlueprintsCache c,string id){
  var u=Psychic034Tests.Unit(c,id).Unit;var features=new FeatureCollection();features.Initialize(u.Facts);Set(u.Progression,"Features",features);
  Set(u.Progression,"m_CharacterLevel",(int?)8);Set(u.Progression,"m_MythicLevel",(int?)0);Set(u.Descriptor,"Proficiencies",new UnitProficiency());
  var bp=Psychic034Tests.Asset<BlueprintUnit>(c,"SpiritualWeaponUnit");Set(u.Descriptor,"Blueprint",bp);Set(u.Descriptor,"OriginalBlueprint",bp);Set(u.Descriptor,"Body",new UnitBody(u.Descriptor));
  foreach(var s in new[]{StatType.AdditionalCMB,StatType.AdditionalCMD,StatType.SkillLoreReligion}){var v=(s==StatType.SkillLoreReligion?(ModifiableValue)new ModifiableValueSkill(u.Stats,u.Stats.Wisdom,s):new ModifiableValue(u.Stats,s));Set(u.Stats,s.ToString(),v);u.Stats.AllStats.Add(v);}
  var ac=new ModifiableValueArmorClass(u.Stats);Set(u.Stats,"AC",ac);u.Stats.AllStats.Add(ac);ac.BaseValue=10;
  Set(u.Descriptor,"Alignment",new Kingmaker.UnitLogic.Alignments.UnitAlignment(u.Descriptor));Set(u.Alignment,"m_Value",Alignment.TrueNeutral);return u;
 }
 static Feature Fact(UnitEntityData u,BlueprintFeature bp){
  var f=Empty<Feature>();Set(f,"Components",new List<EntityFactComponent>());f.RestoreOwnerLink(u.Facts);f.Param=new FeatureParam();Set(f,"Rank",1);
  Set(f,"m_Context",new MechanicsContext(u,u,bp,null));AccessTools.Method(typeof(EntityFact),"Setup").Invoke(f,new object[]{bp});Set(f,"IsActive",true);Set(f,"IsTurnedOn",true);u.Facts.List.Add(f);Refresh(u);return f;
 }
 static EntityFactComponent Runtime(EntityFact f,Type t)=>f.Components.Single(c=>c.SourceBlueprintComponent.GetType()==t);
 static void Call(EntityFact f,Type t,string method,params object[] args){var r=Runtime(f,t);using(r.RequestEventContext())AccessTools.Method(t,method,args.Select(a=>a.GetType()).ToArray()).Invoke(r.SourceBlueprintComponent,args);}
 static void Shape(UnitEntityData u,BlueprintBuff bp){Set(u.Body,"IsPolymorphed",true);Psychic034Tests.Fact(u,bp,u,8);Set(u.Body,"m_PolymorphHandsEquipmentSet",new HandsEquipmentSet(u.Descriptor));Set(u.Body,"m_PolymorphAdditionalLimbs",new List<Kingmaker.Items.Slots.WeaponSlot>());}
 static ItemEntityWeapon Weapon(UnitEntityData u,WeaponCategory category,bool natural,PhysicalDamageForm form,string id){
  var type=new BlueprintWeaponType{Category=category};Set(type,"m_IsNatural",natural);Set(type,"m_AttackType",AttackType.Melee);Set(type,"m_BaseDamage",new DiceFormula(1,DiceType.D6));Set(type,"m_CriticalRollEdge",20);Set(type,"m_CriticalModifier",2);
  Set(type,"m_DamageType",new DamageTypeDescription{Type=DamageType.Physical,Physical=new DamageTypeDescription.PhysicalData{Form=form}});type.OnEnable();
  var bp=new BlueprintItemWeapon{name=id,AssetGuid=Id(id)};Set(bp,"m_Type",Ref<BlueprintWeaponTypeReference>(type));Set(bp,"m_Size",Size.Medium);Set(bp,"m_Enchantments",Array.Empty<BlueprintWeaponEnchantmentReference>());bp.OnEnable();
  var item=Empty<ItemEntityWeapon>();Set(item,"m_Blueprint",bp);Set(item,"UniqueId",id);Set(item,"m_CachedEnchantments",new List<ItemEnchantment>());
  var facts=new EntityFactsManager(item);Set(item,"Facts",facts);var collection=facts.EnsureFactProcessor<ItemEnchantmentCollection>();Set(item,"m_Enchantments",collection);return item;
 }
 static void Equip(Kingmaker.Items.Slots.HandSlot slot,ItemEntityWeapon w){Set(slot,"m_FakeItem",w);Set(slot,"m_IsItemFake",w!=null);if(w!=null)Set(w,"HoldingSlot",slot);}
 static bool Need(Prerequisite p,UnitEntityData u)=>(bool)AccessTools.Method(p.GetType(),"CheckInternal").Invoke(p,new object[]{null,u.Descriptor,null});
 static bool Allowed(UnitEntityData u,bool celestial)=>(bool)AccessTools.Method(Module,"Allowed").Invoke(null,new object[]{u,celestial});
 static bool InShape(UnitEntityData u)=>(bool)AccessTools.Method(Module,"InShape").Invoke(null,new object[]{u});
 static ItemEntityWeapon Source(UnitEntityData u)=>(ItemEntityWeapon)AccessTools.Method(typeof(WeaponShiftTransfer),"Source").Invoke(null,new object[]{u});
 static bool Transfers(BlueprintItemEnchantment e,bool improved,bool greater)=>(bool)AccessTools.Method(typeof(WeaponShiftTransfer),"Transfer").Invoke(null,new object[]{e,improved,greater});
 internal static int Run(BlueprintsCache c){
  n=0;var oldTime=Kingmaker.Game.Instance.TimeController;Set(Kingmaker.Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());var h=new Harmony("CotW01410.WildShapeNative");void Patch(System.Reflection.MethodBase m,string name)=>h.Patch(m,prefix:new HarmonyMethod(typeof(WildShapeFeatTests),name));
  Patch(AccessTools.Method(typeof(EntityFact),"Attach"),nameof(AttachId));Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));
  Patch(AccessTools.Method(typeof(ItemEnchantment),"RespawnFx"),nameof(Skip));
  Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),nameof(Skip));Patch(AccessTools.PropertyGetter(typeof(ItemEntity),"Wielder"),nameof(NoWielder));
  Patch(AccessTools.PropertyGetter(typeof(ItemEntityWeapon),"HoldInTwoHands"),nameof(False));Patch(AccessTools.Method(typeof(RuleCalculateWeaponSizeBonus),"OnTrigger"),nameof(Skip));
  Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(Apply));Patch(AccessTools.Method(typeof(Buff),"Remove"),nameof(Remove));
  try{
   var tokens=(string[])AccessTools.Field(Module,"Tokens").GetValue(null);var forms=(string[][])AccessTools.Field(typeof(WeaponShiftTransfer).Assembly.GetType("CallOfTheWild.Wrath.Prototype.WildShapeCatalog"),"Forms").GetValue(null);
   var originals=forms.Select(r=>((BlueprintBuff)c.Load(BlueprintGuid.Parse(r[1]))).ComponentsArray).ToArray();Register(c);var feats=tokens.Select(t=>Asset<BlueprintFeature>(c,t)).ToArray();Register(c);
   for(int i=0;i<5;i++){Check(ReferenceEquals(c.Load(Id(tokens[i])),feats[i]),"idempotent identity "+tokens[i]);Check(feats[i].Groups.SequenceEqual(new[]{FeatureGroup.Feat}),"general feat group "+tokens[i]);Check(feats[i].ComponentsArray.Select(x=>x.name).Distinct().Count()==feats[i].ComponentsArray.Length,"stable component names "+tokens[i]);}
   var reg=typeof(WeaponShiftTransfer).Assembly.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration");var basic=(BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse((string)AccessTools.Field(reg,"BasicFeatsId").GetValue(null)));
   foreach(var field in new[]{"m_Features","m_AllFeatures"})foreach(var feat in feats)Check(((BlueprintFeatureReference[])Get(basic,field)).Count(r=>r.Guid==feat.AssetGuid)==(feat==feats[4]?0:1),"once in basic feat arrays");
   foreach(var id in (string[])AccessTools.Field(reg,"NativeCombatFeatSelectionIds").GetValue(null))foreach(var field in new[]{"m_Features","m_AllFeatures"})Check(!((BlueprintFeatureReference[])Get(c.Load(BlueprintGuid.Parse(id)),field)).Any(r=>feats.Any(f=>f.AssetGuid==r.Guid)),"general feats absent from combat-only selection");
   Check(forms.Length==105&&forms.Count(r=>r[2]=="animal")==4&&forms.Count(r=>r[2]=="shifter")==84,"actual current game wild-shape catalog");
   for(int i=0;i<forms.Length;i++){
    var buff=(BlueprintBuff)c.Load(BlueprintGuid.Parse(forms[i][1]));var ability=(BlueprintAbility)c.Load(BlueprintGuid.Parse(forms[i][0]));var cost=ability.GetComponent<AbilityResourceLogic>();bool animal=forms[i][2]!="other";
    Check(buff.GetComponents<PlanarShapeBinding>().Count()==(animal?1:0),"only animal forms bind template");Check(originals[i].All(x=>buff.ComponentsArray.Contains(x)),"native polymorph components preserved");
    Check(cost.ResourceCostIncreasingFacts.Count(r=>r.Guid==Id("PlanarCelestialChoice"))==(animal?1:0)&&cost.ResourceCostIncreasingFacts.Count(r=>r.Guid==Id("PlanarFiendishChoice"))==(animal?1:0),"one extra cost each choice only animals");
   }
   bool rejected=false;var missing=new BlueprintsCache();try{Register(missing);}catch{rejected=true;}Check(rejected&&missing.Load(Id(tokens[0]))==null,"missing native anchors leave no assets");
   var u=Unit(c,"wild-owner");var wild=(BlueprintFeature)c.Load(BlueprintGuid.Parse("19bb148cb92db224abb431642d10efeb"));var druid=(BlueprintCharacterClass)c.Load(BlueprintGuid.Parse("610d836f3a3a9ed42a4349b62f002e96"));u.Progression.Classes.Add(new ClassData(druid){Level=8});
   Check(!Need(feats[0].GetComponent<PrerequisiteFeaturesFromList>(),u),"wild shape required");Fact(u,wild);u.Stats.SkillLoreReligion.BaseValue=4;Check(!Need(feats[0].GetComponent<PrerequisiteStatValue>(),u),"Lore Religion four rejected");u.Stats.SkillLoreReligion.BaseValue=5;Check(feats[0].GetComponents<Prerequisite>().All(p=>Need(p,u)),"wild shape and Lore five accepted");
   var druidLevel=u.Progression.Classes.Single(x=>x.CharacterClass==druid);druidLevel.Level=7;Check(!Need(feats[1].GetComponent<PrerequisiteClassLevel>(),u),"druid seven rejected");druidLevel.Level=8;Check(feats[1].GetComponents<Prerequisite>().Where(p=>p.Group!=Prerequisite.GroupType.Any).All(p=>Need(p,u))&&feats[1].GetComponents<Prerequisite>().Where(p=>p.Group==Prerequisite.GroupType.Any).Any(p=>Need(p,u)),"druid eight accepted");
   Check(!feats[3].GetComponents<PrerequisiteFeature>().All(p=>Need(p,u)),"Improved needs base");var shift=Fact(u,feats[2]);u.Stats.BaseAttackBonus.BaseValue=5;Check(!Need(feats[3].GetComponent<PrerequisiteStatValue>(),u),"Improved BAB five denied");u.Stats.BaseAttackBonus.BaseValue=6;Check(feats[3].GetComponents<Prerequisite>().All(p=>Need(p,u)),"Improved BAB six accepted");var improved=Fact(u,feats[3]);u.Stats.BaseAttackBonus.BaseValue=7;Check(!Need(feats[4].GetComponent<PrerequisiteStatValue>(),u),"Greater BAB seven denied");u.Stats.BaseAttackBonus.BaseValue=8;Check(feats[4].GetComponents<Prerequisite>().All(p=>Need(p,u)),"Greater BAB eight accepted");var greater=Fact(u,feats[4]);
   foreach(Alignment a in Enum.GetValues(typeof(Alignment))){Set(u.Alignment,"m_Value",a);Check(Allowed(u,true)==!a.HasComponent(AlignmentComponent.Evil),"celestial alignment "+a);Check(Allowed(u,false)==!a.HasComponent(AlignmentComponent.Good),"fiendish alignment "+a);}Set(u.Alignment,"m_Value",Alignment.TrueNeutral);var planar=Fact(u,feats[0]);
   var smitePools=new List<BlueprintAbilityResource>();foreach(var runtime in planar.Components.Where(x=>x.SourceBlueprintComponent is Kingmaker.Designers.Mechanics.Facts.AddAbilityResources)){
    var grant=(Kingmaker.Designers.Mechanics.Facts.AddAbilityResources)runtime.SourceBlueprintComponent;var resource=grant.Resource;var row=PsychicExpansionTests.NativeRows.Single(r=>(string)r["blueprint"]["AssetId"]==resource.AssetGuid.ToString());var field=AccessTools.Field(resource.GetType(),"m_MaxAmount");field.SetValue(resource,PsychicExpansionTests.Read(row["blueprint"]["Data"]["m_MaxAmount"],field.FieldType));
    using(runtime.RequestEventContext())AccessTools.Method(grant.GetType(),"OnActivate").Invoke(grant,null);Check(u.Resources.GetResourceAmount(resource)==1,"real root resource grant fills one daily smite");u.Resources.Spend(resource,1);smitePools.Add(resource);
   }
   var wolf=(BlueprintBuff)c.Load(BlueprintGuid.Parse("470fb1a22e7eb5849999f1101eacc5dc"));var wolfAbility=(BlueprintAbility)c.Load(BlueprintGuid.Parse("ac8811714a45a5948b27208538ce4f03"));var data=new AbilityData(wolfAbility,u.Descriptor);var logic=wolfAbility.GetComponent<AbilityResourceLogic>();int baseCost=logic.CalculateCost(data);
   var chosen=Psychic034Tests.Fact(u,Asset<BlueprintBuff>(c,"PlanarCelestialChoice"),u,8);Check(logic.CalculateCost(data)==baseCost+1,"native resource cost adds one use");
   var pool=new UnitAbilityResource(logic.RequiredResource);Set(pool,"Amount",3);((Dictionary<BlueprintScriptableObject,UnitAbilityResource>)Get(u.Resources,"m_Resources")).Add(logic.RequiredResource,pool);logic.Spend(data);Check(pool.Amount==1,"real native spend deducts two wild-shape uses");Check(!logic.IsAbilityRestrictionPassed(data),"native resource restriction blocks one remaining use for cost two");
   foreach(var row in forms.Where(r=>r[2]=="other")){var ability=(BlueprintAbility)c.Load(BlueprintGuid.Parse(row[0]));Check(ability.GetComponent<AbilityResourceLogic>().CalculateCost(new AbilityData(ability,u.Descriptor))==1,"non-animal still one native use");}
   var compat=typeof(WeaponShiftTransfer).Assembly.GetType("CallOfTheWild.Wrath.Prototype.WildShapeCompatibility");
   h.CreateClassProcessor(compat).Patch();
   Set(u.State.Features,"MasterShapeshifter",new Kingmaker.Utility.CountableFlag());u.State.Features.MasterShapeshifter.Retain();Set(pool,"Amount",0);
   Check(logic.CalculateCost(data)==0&&logic.IsAbilityRestrictionPassed(data),"Master Shapeshifter waives base and extra use even at zero");logic.Spend(data);Check(pool.Amount==0,"native free spend does not consume or refund");
   foreach(var row in forms){var a=(BlueprintAbility)c.Load(BlueprintGuid.Parse(row[0]));Check(a.GetComponent<AbilityResourceLogic>().CalculateCost(new AbilityData(a,u.Descriptor))==0,"all catalog forms free with Master Shapeshifter");}
   u.State.Features.MasterShapeshifter.Release();Set(pool,"Amount",3);Check(logic.CalculateCost(data)==2,"normal planar cost restored after removing Master");
   var shifter=(BlueprintCharacterClass)c.Load(BlueprintGuid.Parse("a406d6ebea5c46bba3160246be03e96f"));var su=Unit(c,"shifter-qualification");su.Progression.Classes.Add(new ClassData(shifter){Level=8});
   var sf=(string[])AccessTools.Field(typeof(WeaponShiftTransfer).Assembly.GetType("CallOfTheWild.Wrath.Prototype.WildShapeCatalog"),"ShifterFeatures").GetValue(null);
   foreach(var id in sf){var fact=Fact(su,(BlueprintFeature)c.Load(BlueprintGuid.Parse(id)));Check(Need(feats[0].GetComponent<PrerequisiteFeaturesFromList>(),su),"Shifter form feature qualifies without Druid Wolf");su.Facts.List.Remove(fact);Refresh(su);}
   Fact(su,(BlueprintFeature)c.Load(BlueprintGuid.Parse(sf[0])));Check(feats[1].GetComponents<Prerequisite>().Where(p=>p.Group==Prerequisite.GroupType.Any).Any(p=>Need(p,su)),"Shifter eight qualifies for Powerful Shape");
   su.Progression.Classes.Single(x=>x.CharacterClass==shifter).Level=7;Check(!feats[1].GetComponents<Prerequisite>().Where(p=>p.Group==Prerequisite.GroupType.Any).Any(p=>Need(p,su)),"Shifter seven not sufficient");
   foreach(var row in forms.Where(x=>x[2]=="shifter")){Shape(su,(BlueprintBuff)c.Load(BlueprintGuid.Parse(row[1])));Check(InShape(su),"eligible Shifter buff recognized");foreach(var b in su.Buffs.Enumerable.ToArray())b.Remove();}
   Check(!forms.Any(x=>x[0]=="a97ef112430b4104956b737ddaede57b"||x[0]=="14d16d7ff88d41c08fb4cb4be26383b6"),"excluded Devastating Form and Fiendish Aspect never enter catalog");
   Shape(u,wolf);var shape=u.Buffs.Enumerable.Single(b=>b.Blueprint==wolf);applied=null;Call(shape,typeof(PlanarShapeBinding),"OnActivate");Check(applied?.Buff==Asset<BlueprintBuff>(c,"PlanarCelestialEffect")&&applied.AsChild&&applied.Permanent&&applied.IsNotDispelable,"choice delivers child template only for lifetime of parent shape");
   Set(u.Alignment,"m_Value",Alignment.NeutralEvil);applied=null;Call(shape,typeof(PlanarShapeBinding),"OnActivate");Check(applied==null,"opposite alignment cannot gain template");Call(chosen,typeof(PlanarChoice),"HandleAlignmentChange",u,Alignment.TrueNeutral,Alignment.NeutralEvil);Check(!u.HasFact(chosen.Blueprint)&&logic.CalculateCost(data)==baseCost,"alignment change clears incompatible extra cost");Set(u.Alignment,"m_Value",Alignment.TrueNeutral);
   foreach(string name in new[]{"Celestial","Fiendish"})foreach(int hd in new[]{1,4,5,8,10,11,20,24}){
    Set(u.Progression,"m_CharacterLevel",(int?)hd);var template=Asset<BlueprintFeature>(c,"Planar"+name+"Template");var original=(BlueprintFeature)c.Load(BlueprintGuid.Parse(name=="Celestial"?"ab3559a3cb9dcef49b8b529590e216d6":"8d24793851c6b2e4688e785f6954de68"));
    Check(template.ComponentsArray.Length==original.ComponentsArray.Length-1&&!template.ComponentsArray.Intersect(original.ComponentsArray).Any()&&template.GetComponent<Kingmaker.Designers.Mechanics.Facts.AddAbilityResources>()==null,"deep cloned native template; daily pool belongs to learned feat");
    var ctx=new MechanicsContext(u,u,template,null);ctx.RecalculateRanks();foreach(var rank in template.GetComponents<ContextRankConfig>())Check(rank.GetValue(ctx)==((ContextRankProgression)Get(rank,"m_Progression")==ContextRankProgression.Custom?(hd<5?1:hd<11?2:3):hd+5),"own HD native template ranks "+name+hd);
   }
   Set(u.Progression,"m_CharacterLevel",(int?)8);var powerful=Fact(u,feats[1]);foreach(Size size in Enum.GetValues(typeof(Size))){Set(u.State,"m_Size",size);var cmb=new RuleCalculateBaseCMB(u);cmb.OnTrigger(null);int before=cmb.Result;var boosted=new RuleCalculateBaseCMB(u);Call(powerful,typeof(PowerfulShapeBonus),"OnEventAboutToTrigger",boosted);boosted.OnTrigger(null);var next=size.Shift(1);int expected=next.GetModifiers().CMDAndCMD-size.GetModifiers().CMDAndCMD+next.GetModifiers().AttackAndAC-size.GetModifiers().AttackAndAC;Check(boosted.Result==before+expected,"native CMB next size "+size);boosted.OnDidTrigger();Check(u.Stats.AdditionalCMB.Modifiers.Count()==0,"temporary CMB modifier cleared");var cmd=new RuleCalculateBaseCMD(u);cmd.OnTrigger(null);before=cmd.Result;var cmdBoost=new RuleCalculateBaseCMD(u);Call(powerful,typeof(PowerfulShapeBonus),"OnEventAboutToTrigger",cmdBoost);cmdBoost.OnTrigger(null);Check(cmdBoost.Result==before+expected&&u.State.Size==size,"CMD next size without body mutation "+size);}
   var gt=Unit(c,"size-special-target");Set(u.State,"m_Size",Size.Large);Set(gt.State,"m_Size",Size.Huge);
   var grabAction=new ContextActionCombatManeuverCustom{Type=CombatManeuver.Grapple,Success=new Kingmaker.ElementsSystem.ActionList{Actions=new Kingmaker.ElementsSystem.GameAction[]{new ContextActionGrapple()}}};
   h.Patch(AccessTools.Method(typeof(ContextActionCombatManeuverCustom),"RunAction"),prefix:new HarmonyMethod(typeof(WildShapeFeatTests),nameof(SpecialBoundary)){priority=0});
   var specialContext=new MechanicsContext(u,u,feats[1],null);specialCalls=0;using(specialContext.GetDataScope(gt))grabAction.RunAction();Check(specialCalls==1,"native Grab action accepts next larger target with Powerful Shape");
   Set(gt.State,"m_Size",Size.Gargantuan);specialCalls=0;using(specialContext.GetDataScope(gt))grabAction.RunAction();Check(specialCalls==0,"native Grab action rejects target above effective size");
   Set(gt.State,"m_Size",Size.Large);Fact(gt,feats[1]);Shape(gt,wolf);specialCalls=0;using(specialContext.GetDataScope(gt))grabAction.RunAction();Check(specialCalls==1,"two powerful forms compare both effective sizes");
   var trampleFact=Fact(u,(BlueprintFeature)c.Load(BlueprintGuid.Parse("0e6233b7c8a14f01b80a83731868454c")));
   h.Patch(AccessTools.Method(typeof(Kingmaker.Designers.Mechanics.Facts.ManeuverTrigger),"OnEventDidTrigger"),prefix:new HarmonyMethod(typeof(WildShapeFeatTests),nameof(SpecialBoundary)){priority=0});
   specialCalls=0;Call(trampleFact,typeof(Kingmaker.Designers.Mechanics.Facts.ManeuverTrigger),"OnEventDidTrigger",new RuleCombatManeuver(u,gt,CombatManeuver.Overrun));Check(specialCalls==0,"native trample excludes target equal to effective size");
   foreach(var b in gt.Buffs.Enumerable.ToArray())b.Remove();Set(gt.Body,"IsPolymorphed",false);specialCalls=0;Call(trampleFact,typeof(Kingmaker.Designers.Mechanics.Facts.ManeuverTrigger),"OnEventDidTrigger",new RuleCombatManeuver(u,gt,CombatManeuver.Overrun));Check(specialCalls==1,"native trample admits target equal to actual size, one below effective size");
   foreach(Size a in Enum.GetValues(typeof(Size)))foreach(Size t in Enum.GetValues(typeof(Size))){Set(u.State,"m_Size",a);Set(gt.State,"m_Size",t);Check((bool)AccessTools.Method(compat,"GrabAllowed").Invoke(null,new object[]{u,gt})==(t<=a.Shift(1)),"grab size matrix "+a+"/"+t);Check((bool)AccessTools.Method(compat,"TrampleAllowed").Invoke(null,new object[]{u,gt})==(t<a.Shift(1)),"trample size matrix "+a+"/"+t);Check(u.State.Size==a&&gt.State.Size==t,"special-size checks never mutate real body sizes");}
   Set(u.State,"m_Size",Size.Medium);
   var source=Weapon(u,WeaponCategory.Scythe,false,PhysicalDamageForm.Slashing,"wild-source");u.Proficiencies.Add(WeaponCategory.Scythe);Equip(u.Body.HandsEquipmentSets[u.Body.CurrentHandEquipmentSetIndex].PrimaryHand,source);var bite=Weapon(u,WeaponCategory.Bite,true,PhysicalDamageForm.Piercing,"wild-bite");var claw=Weapon(u,WeaponCategory.Claw,true,PhysicalDamageForm.Slashing,"wild-claw");Equip(u.Body.PrimaryHand,bite);Equip(u.Body.SecondaryHand,claw);
   Check(Source(u)==source,"reads melded original hand rather than form natural hand");u.Proficiencies.Remove(WeaponCategory.Scythe);Check(Source(u)==null,"unproficient source excluded even with equip-anything shield rule");u.Proficiencies.Add(WeaponCategory.Scythe);
   var material=(BlueprintItemEnchantment)c.Load(BlueprintGuid.Parse("e5990dc76d2a613409916071c898eee8"));var frost=(BlueprintItemEnchantment)c.Load(BlueprintGuid.Parse("421e54078b7719d40915ce0672511d0b"));var plus=(BlueprintItemEnchantment)c.Load(BlueprintGuid.Parse("d42fc23b92c640846ac137dc26e000d4"));
   Check(Transfers(material,false,false)&&!Transfers(frost,false,false)&&Transfers(frost,true,false)&&Transfers(plus,true,false)&&Transfers(plus,true,true),"base material only; merged Improved includes enhancement; legacy Greater also works");
   var context=new MechanicsContext(u,u,feats[2],null);source.AddEnchantment(material,context);source.AddEnchantment(frost,context);source.AddEnchantment(plus,context);
   Call(shift,typeof(WeaponShiftTransfer),"OnPolymorphActivated",u,wolf.GetComponent<Polymorph>());var rt=Runtime(shift,typeof(WeaponShiftTransfer));var state=rt.GetData<WeaponShiftData>();Check(state.Applied.Count==6&&bite.Enchantments.Count==3&&claw.Enchantments.Count==3,"actual native enchantment collections: applied="+state.Applied.Count+" bite="+bite.Enchantments.Count+" claw="+claw.Enchantments.Count+" source="+source.Enchantments.Count+" primary="+u.Body.PrimaryHand.MaybeWeapon?.Blueprint.name+" secondary="+u.Body.SecondaryHand.MaybeWeapon?.Blueprint.name);
   // Unknown GUID with a standard weapon property: native third-party/permanent
   // and temporary spell additions both travel from the actual item instance.
   var custom=new BlueprintWeaponEnchantment{name="ThirdPartyCold",AssetGuid=Id("Wild01411Custom")};custom.ComponentsArray=frost.ComponentsArray.Select(x=>(BlueprintComponent)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"CopyData").MakeGenericMethod(typeof(BlueprintComponent)).Invoke(null,new object[]{x})).ToArray();custom.OnEnable();c.AddCachedBlueprint(custom.AssetGuid,custom);
   var extra=source.AddEnchantment(custom,context);Check(bite.Enchantments.Any(e=>e.Blueprint==custom)&&claw.Enchantments.Any(e=>e.Blueprint==custom),"source enchantment add synchronizes immediately without attack");
   source.RemoveEnchantment(extra);Check(!bite.Enchantments.Any(e=>e.Blueprint==custom)&&state.Applied.Count==6,"source removal synchronizes immediately");
   var anotherContext=new MechanicsContext(u,u,feats[3],null);var first=source.AddEnchantment(custom,context);var second=source.AddEnchantment(custom,anotherContext);
   foreach(var weapon in new[]{bite,claw}){var copies=weapon.Enchantments.Where(e=>e.Blueprint==custom).ToArray();Check(copies.Length==2&&copies.Any(e=>ReferenceEquals(e.ParentContext,context))&&copies.Any(e=>ReferenceEquals(e.ParentContext,anotherContext)),"same blueprint with distinct permanent/spell contexts preserves both actual instances");}
   source.RemoveEnchantment(second);Check(bite.Enchantments.Count(e=>e.Blueprint==custom)==1&&ReferenceEquals(bite.Enchantments.Single(e=>e.Blueprint==custom).ParentContext,context),"removing later same-blueprint instance retains earlier source context");source.RemoveEnchantment(first);Check(state.Applied.Count==6&&!bite.Enchantments.Any(e=>e.Blueprint==custom),"last source instance removal clears its copied properties");
   var ids=state.Applied.ToArray();Call(shift,typeof(WeaponShiftTransfer),"OnPolymorphActivated",u,wolf.GetComponent<Polymorph>());Check(state.Applied.SequenceEqual(ids),"repeat refresh creates no copies");
   foreach(var weapon in new[]{bite,claw}){var rule=new RuleCalculateWeaponStats(u,weapon);rule.OnTrigger(null);Check(rule.EnhancementTotal==1,"native weapon-stat enchantment dispatch applies Enhancement1");}
   foreach(bool celestial in new[]{true,false})foreach(Alignment alignment in new[]{Alignment.LawfulGood,Alignment.NeutralEvil,Alignment.TrueNeutral})foreach(var weapon in new[]{bite,source}){
    var target=Unit(c,"critical-target");Set(target.Alignment,"m_Value",alignment);var effect=Psychic034Tests.Fact(u,Asset<BlueprintBuff>(c,"Planar"+(celestial?"Celestial":"Fiendish")+"Effect"),u,8);var roll=new RuleAttackRoll(u,target,weapon,0);int ab=roll.AttackBonus;Call(effect,typeof(PlanarCriticalConfirmation),"OnEventAboutToTrigger",roll);Check(roll.CriticalConfirmationBonus==(weapon==bite&&alignment.HasComponent(celestial?AlignmentComponent.Evil:AlignmentComponent.Good)?2:0)&&roll.AttackBonus==ab,"confirmation-only +2, correct target alignment and natural category");effect.Remove();
   }
   var stats=new RuleCalculateWeaponStats(u,bite);stats.OnTrigger(null);var oldType=stats.DamageDescription.First(d=>d.TypeDescription.Type==DamageType.Physical).TypeDescription;Call(shift,typeof(WeaponShiftTransfer),"OnEventDidTrigger",stats);var damage=stats.DamageDescription.First(d=>d.TypeDescription.Type==DamageType.Physical);Check(damage.TypeDescription.Physical.Form==PhysicalDamageForm.Slashing&&bite.Blueprint.DamageType.Physical.Form==PhysicalDamageForm.Piercing&&oldType.Physical.Form==PhysicalDamageForm.Piercing,"finished damage copy changes form but no shared blueprint or original rule data");Check(damage.Dice.Rolls==1&&(int)bite.Blueprint.CriticalModifier==2,"form dice and critical multiplier preserved");
   Check(stats.DamageDescription.Any(d=>d.TypeDescription.Type==DamageType.Energy&&d.TypeDescription.Energy==DamageEnergyType.Cold)&&damage.TypeDescription.Type==DamageType.Physical,"native Frost component produces separate energy damage on copied instance");
   var trip=new RuleCalculateCMB(u,Unit(c,"trip-target"),CombatManeuver.Trip);Call(shift,typeof(WeaponShiftTransfer),"OnEventAboutToTrigger",trip);Check(trip.AllBonuses.Any(m=>m.Value==2&&m.Fact==shift),"trip property source in native CMB modifier");
   Call(shift,typeof(WeaponShiftTransfer),"OnApplyPostLoadFixes");Check(state.Applied.Count==6&&!state.Applied.Intersect(ids).Any()&&bite.Enchantments.Count==3,"post-load drops tracked instances and rebinds exactly once");
   var foreign=bite.AddEnchantment(frost,context);Call(shift,typeof(WeaponShiftTransfer),"OnPolymorphDeactivated",u,wolf.GetComponent<Polymorph>());Check(state.Applied.Count==0&&bite.Enchantments.SequenceEqual(new[]{foreign})&&claw.Enchantments.Count==0&&source.Enchantments.Count==3,"exit removes only this feat's enchantments and retains foreign/source properties");
   Set(u.Body,"IsPolymorphed",false);Check(!InShape(u)&&Source(u)==null,"human form no transfer");Call(shift,typeof(WeaponShiftTransfer),"OnTurnOff");Check(state.Applied.Count==0,"feat removal cannot leave attached copies");
   foreach(var resource in smitePools){Check(u.Resources.GetResourceAmount(resource)==0,"shape enter/exit does not refill or discard spent daily pool");u.Resources.Restore(resource);Check(u.Resources.GetResourceAmount(resource)==1,"native rest restore works while human");}
   var outside=new RuleCalculateBaseCMB(u);Call(powerful,typeof(PowerfulShapeBonus),"OnEventAboutToTrigger",outside);Check(!u.Stats.AdditionalCMB.Modifiers.Any(),"outside druid wild shape no powerful size bonus");
   foreach(var field in typeof(WeaponShiftData).GetFields())Check(field.IsDefined(typeof(Newtonsoft.Json.JsonPropertyAttribute),false),"native item references/signature declared for persistence");
   PsychicExpansionTests.Inventory["wildShape01410Assertions"]=n;
   PsychicExpansionTests.Inventory["wildShape01410Assets"]=new Newtonsoft.Json.Linq.JArray(feats.Select(f=>new Newtonsoft.Json.Linq.JObject{["name"]=f.name,["guid"]=f.AssetGuid.ToString()}));
   Console.WriteLine("PASS "+n+" wild-shape feat native checks; 4 selectable plus legacy, 105 native form bindings; renderer/buff delivery/full save bounds");return n;
  }finally{h.UnpatchAll(h.Id);Set(Kingmaker.Game.Instance,"TimeController",oldTime);}
 }
}
