using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;
using Way_of_the_shield;
using Way_of_the_shield.NewComponents;
using Way_of_the_shield.NewFeatsAndAbilities;
using static Way_of_the_shield.NewComponents.OffHandParry;
using TurnBased.Controllers;

internal static class FeatLauncher {
 static int Main(string[] args){if(args.Length!=3)return 2;AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{foreach(var d in new[]{args[0],Path.GetDirectoryName(args[1])}){var p=Path.Combine(d,new AssemblyName(e.Name).Name+".dll");if(File.Exists(p))return Assembly.LoadFrom(p);}return null;};try{typeof(FeatLauncher).Assembly.GetType("FeatRuntimeTests").GetMethod("Run").Invoke(null,new object[]{args});return 0;}catch(Exception e){while(e!=null){Console.Error.WriteLine(e);e=e.InnerException;}return 1;}}
}

// Actual module components, native rank calculation, enumeration, facts and JSON
// execute. Unity visuals/time, command dispatch and final combat animation are bounds.
public static partial class FeatRuntimeTests {
 static int checks,aoos,countCalls;static bool tb,flat=false;static TimeSpan now;
 static Game game;static BlueprintRoot root;static BlueprintsCache cache;static Harmony h;
 static UnitFact mythicFact;static RuleCalculateAttacksCount lastCount;
 static readonly Dictionary<RuleAttackRoll.ParryData,UnitEntityData> parries=new();
 static readonly Dictionary<WeaponSlot,ItemEntityShield> shields=new();
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static FieldInfo Field(object o,string n)=>AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")??throw new Exception("Fixture field: "+o.GetType()+"."+n);
 static object Get(object o,string n)=>Field(o,n).GetValue(o);static void Set(object o,string n,object v)=>Field(o,n).SetValue(o,v);
 static void Check(bool ok,string why){if(!ok)throw new Exception("Feat scenario: "+why);checks++;}
 static T Ref<T>(SimpleBlueprint bp)where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(bp);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,bp);return r;}
 static void Add(BlueprintScriptableObject bp,string id){bp.AssetGuid=BlueprintGuid.Parse(id);cache.AddCachedBlueprint(bp.AssetGuid,bp);}
 static void Refresh(UnitEntityData u){var c=Get(u.Facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
 static void Patch(MethodBase method,string prefix){if(method==null)throw new Exception("Missing endpoint "+prefix);h.Patch(method,prefix:new HarmonyMethod(typeof(FeatRuntimeTests),prefix){priority=Priority.First});}
 static bool Skip()=>false;static bool Yes(ref bool __result){__result=true;return false;}static bool Zero(ref float __result){__result=0;return false;}
 static bool GameBoundary(ref Game __result){__result=game;return false;}static bool TimeBoundary(ref TimeSpan __result){__result=now;return false;}static bool TB(ref bool __result){__result=tb;return false;}
 static bool Icon(ref UnityEngine.Sprite __result){__result=null;return false;}static bool Root(ref BlueprintRoot __result){__result=root;return false;}
 static bool Shield(HandSlot __instance,ref ItemEntityShield __result){if(!shields.TryGetValue(__instance,out __result))return true;return false;}
 static bool AOO(UnitEntityData __0,UnitEntityData __1){aoos++;return false;}
 static bool Parry(RuleAttackRoll __instance,UnitEntityData __0,ItemEntityWeapon __1,int __2){var p=Empty<RuleAttackRoll.ParryData>();parries[p]=__0;
  Set(__instance,"Parry",p);return false;}
 static bool ParryOwner(RuleAttackRoll.ParryData __instance,ref UnitEntityData __result){return !parries.TryGetValue(__instance,out __result);}
 static bool Triggered(RuleAttackRoll.ParryData __instance,ref bool __result){if(!parries.ContainsKey(__instance))return true;__result=true;return false;}
 static bool Removed(Buff __instance){var u=__instance.Owner;u.Facts.List.Remove(__instance);
  if(__instance.Blueprint.GetComponent<ShieldedDefenseAcBonus>()!=null)Call(__instance,typeof(ShieldedDefenseAcBonus),"OnTurnOff");
  Set(__instance,"IsActive",false);
  Set(__instance,"IsTurnedOn",false);Refresh(u);return false;}
 static bool nativeCount;
 static RuleCalculateAttacksCount Count(RuleCalculateAttacksCount rule){countCalls++;lastCount=rule;if(nativeCount){RulebookEventBus.OnEventAboutToTrigger(rule);rule.OnTrigger(null);RulebookEventBus.OnEventDidTrigger(rule);return rule;}rule.Result.SecondaryHand.AdditionalAttacks=1;rule.Result.SecondaryHand.PenalizedAttacks=2;if(mythicFact!=null)Call(mythicFact,typeof(AdditionalParriesComponent),"OnEventAboutToTrigger",rule);return rule;}
 static RuleCheckTargetFlatFooted Flat(RuleCheckTargetFlatFooted rule){Set(rule,"IsFlatFooted",flat);return rule;}
 static IEnumerable<CodeInstruction> Dispatch(IEnumerable<CodeInstruction> code){foreach(var i in code){if(i.operand is MethodInfo m&&m.DeclaringType==typeof(Rulebook)&&m.Name=="Trigger"&&m.IsGenericMethod){var t=m.GetGenericArguments()[0];if(t==typeof(RuleCalculateAttacksCount)||t==typeof(RuleCheckTargetFlatFooted)){i.opcode=OpCodes.Call;i.operand=AccessTools.Method(typeof(FeatRuntimeTests),t==typeof(RuleCalculateAttacksCount)?nameof(Count):nameof(Flat));}}yield return i;}}
 static Buff Added(BuffCollection collection,BlueprintBuff bp,MechanicsContext ctx,TimeSpan? duration){var result=Fact(collection.Owner,bp,ctx.MaybeCaster);if(bp.GetComponent<UpsettingRoundTracker>()!=null)Call(result,typeof(UpsettingRoundTracker),"OnActivate");if(bp.GetComponent<UpsettingPenaltyLifetime>()!=null)Call(result,typeof(UpsettingPenaltyLifetime),"OnActivate");return result;}
 static bool AddBuff(BuffCollection __instance,BlueprintBuff blueprint,MechanicsContext parentContext,TimeSpan? duration,ref Buff __result){__result=Added(__instance,blueprint,parentContext,duration);return false;}
 static bool AddFromCaster(BuffCollection __instance,BlueprintBuff __0,UnitEntityData __1,TimeSpan? __2,ref Buff __result){__result=Added(__instance,__0,new MechanicsContext(__1,__instance.Owner,__0,null,null),__2);return false;}
 public static void Run(string[] args){
  var dll=Path.Combine(args[0],"0Harmony.dll");
  Check(Assembly.LoadFrom(dll).GetName().Version==new Version(2,0,4,0),"actual game Harmony identity");using(var sha=SHA256.Create())using(var stream=File.OpenRead(dll))Check(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","")=="DE50CD610EA22854C8930EC5433276713AEE1BB0ECD785D6B15A0661C2FA9FD5","actual game Harmony bytes");
  h=new Harmony("CotW.WotS.FeatTests.SceneBoundaries");game=Empty<Game>();
  Set(game,"TimeController",Empty<Kingmaker.Controllers.TimeController>());
  Set(game,"CombatEngagementController",Empty<Kingmaker.Controllers.Combat.UnitCombatEngagementController>());
  Patch(AccessTools.PropertyGetter(typeof(Game),"Instance"),nameof(GameBoundary));Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(TimeBoundary));Patch(AccessTools.Method(typeof(CombatController),"IsInTurnBasedCombat"),nameof(TB));Patch(AccessTools.PropertyGetter(typeof(UnitState),"CanAct"),nameof(Yes));Patch(AccessTools.PropertyGetter(typeof(UnitEntityData),"Corpulence"),nameof(Zero));
  Patch(AccessTools.Method(typeof(ProfileScope),"New",new[]{typeof(string),typeof(UnityEngine.Object)}),nameof(Skip));Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),nameof(Skip));Patch(AccessTools.Method(typeof(Utilities),"LoadIcon"),nameof(Icon));Patch(AccessTools.Method(typeof(ResourcesLibrary),"GetRoot"),nameof(Root));Patch(AccessTools.Method(typeof(Buff),"Remove"),nameof(Removed));
  Patch(AccessTools.Method(typeof(BuffCollection),"AddBuff",new[]{typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),nameof(AddBuff));
  Patch(AccessTools.Method(typeof(BuffCollection),"AddBuff",new[]{typeof(BlueprintBuff),typeof(UnitEntityData),typeof(TimeSpan?),typeof(AbilityParams)}),nameof(AddFromCaster));
  Patch(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel),"Log",new[]{typeof(string),typeof(object[])}),nameof(Skip));Patch(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel),"Warning",new[]{typeof(string),typeof(object[])}),nameof(Skip));
  Patch(AccessTools.PropertyGetter(typeof(HandSlot),"MaybeShield"),nameof(Shield));Patch(AccessTools.Method(typeof(RuleAttackRoll),"TryParry"),nameof(Parry));Patch(AccessTools.PropertyGetter(typeof(RuleAttackRoll.ParryData),"Initiator"),nameof(ParryOwner));Patch(AccessTools.PropertyGetter(typeof(RuleAttackRoll.ParryData),"IsTriggered"),nameof(Triggered));Patch(AccessTools.Method(typeof(Kingmaker.Controllers.Combat.UnitCombatEngagementController),"ForceAttackOfOpportunity"),nameof(AOO));
  foreach(var method in new[]{AccessTools.Method(typeof(OffHandParryUnitPart),"TryActivate"),AccessTools.Method(typeof(OffHandParryUnitPart),"OnEventAboutToTrigger")})h.Patch(method,transpiler:new HarmonyMethod(typeof(FeatRuntimeTests),nameof(Dispatch)));
  try{Setup();ParryScenarios();Defense();Style();LightFeat();FeatPrerequisites();FeedbackProbe();File.WriteAllText(args[2],JsonConvert.SerializeObject(new{assertions=checks,passed=true,scope="Actual compiled components with native AC/stat filters, subscribed TWF/style/Bastion rule handlers, native damage-value calculation, Power Attack scaling, attack counts/enumeration, parry IsTriggered, rank calculations, CanSelect and JSON. Unity rendering/time/mount state/difficulty/faction, buff construction/removal, attack-roll outcomes and AoO animation are isolated boundaries; complete HP damage application and scene save reload are outside this fixture."},Formatting.Indented));Console.WriteLine("PASS: "+checks+" native feat/ability assertions");}finally{h.UnpatchAll(h.Id);}
 }
 static void Setup(){cache=new BlueprintsCache();AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,cache);Utilities.Comment=Owlcat.Runtime.Core.Logging.LogChannelFactory.GetOrCreate("WotS.FeatTests");root=new BlueprintRoot{SystemMechanics=new SystemMechanicsRoot()};var pool=new BlueprintFeatureSelection{m_AllFeatures=Array.Empty<BlueprintFeatureReference>(),m_Features=Array.Empty<BlueprintFeatureReference>()};Add(pool,"247a4068296e8be42890143f451b4b45");
  Set(root.SystemMechanics,"m_MythicFeatSelection",Ref<BlueprintFeatureSelectionReference>(pool));
  foreach(var id in new[]{"7c28228ce4eed1543a6b670fd2a88e72","cb8686e7357a68c42bdd9d4e65334633","121811173a614534e8720d7550aae253","dbec636d84482944f87435bd31522fcc","82fbdd5eb5ac73b498c572cc71bda48f","94fe0ca10f17bf143a7d2bd2ab2acdda","ac8aaf29054f5b74eb18f2af950e752d","0f8939ae6f220984e8fb568abbdfba95"})Add(new BlueprintFeature{ComponentsArray=new BlueprintComponent[]{new Kingmaker.UnitLogic.FactLogic.AddFacts{m_Facts=Array.Empty<BlueprintUnitFactReference>()}}},id);
  Add(new BlueprintFeature{Ranks=5},"6948b379c0562714d9f6d58ccbfa8faa");Add(new BlueprintActivatableAbility(),"3bb6b76ed5b38ab4f957c7f923c23b68");Add(new BlueprintBuff{ComponentsArray=new BlueprintComponent[]{new Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions{Activated=new(),Deactivated=new(),NewRound=new()}}},"6ffd93355fb3bcf4592a5d976b1d32a9");
  BoringBucklerTweaks.AddBucklerParryToBucklerProfficiencyBlueprint();ShieldedDefense.BlueprintCache_Init_Patch();AccessTools.Method(typeof(Mythic_BucklerParry),"CreateBlueprint").Invoke(null,null);UpsettingShieldStyle.BlueprintsCache_Init_Patch();
  h.CreateClassProcessor(typeof(OffHandParry.RuleCalculateAttacksCount_OnTrigger_patch)).Patch();
 }
 static UnitEntityData Unit(string id){var u=Empty<UnitEntityData>();var d=Empty<UnitDescriptor>();
  Set(u,"Descriptor",d);
  Set(d,"Unit",u);
  Set(u,"UniqueId",id);var proxy=FormatterServices.GetUninitializedObject(typeof(EntityService).GetNestedType("Proxy",BindingFlags.Public|BindingFlags.NonPublic));
  Set(proxy,"Entity",u);
  Set(u,"Proxy",proxy);
  Set(u,"Parts",new EntityPartsManager(u));
  Set(u,"Facts",new EntityFactsManager(u));var buffs=new BuffCollection();buffs.Initialize(u.Facts);
  Set(u,"Buffs",buffs);
  Set(d,"Proficiencies",new UnitProficiency());var progression=Empty<UnitProgressionData>();
  Set(progression,"Owner",d);
  Set(progression,"Classes",new List<ClassData>());var features=new FeatureCollection();features.Initialize(u.Facts);
  Set(progression,"Features",features);
  Set(d,"Progression",progression);
  var state=Empty<UnitState>();
  Set(state,"Owner",d);
  Set(state,"Features",new UnitMechanicFeatures());
  Set(state,"AdditionalFeatures",new UnitAdditionalFeatures());
  Set(state,"m_Size",(Size?)Size.Medium);
  Set(state,"m_Conditions",new sbyte[256]);
  Set(d,"State",state);var stats=Empty<CharacterStats>();
  Set(stats,"Owner",d);
  Set(stats,"AllStats",new List<ModifiableValue>());
  Set(d,"Stats",stats);foreach(var type in new[]{StatType.Dexterity,StatType.BaseAttackBonus}){ModifiableValue stat=type==StatType.Dexterity?(ModifiableValue)new ModifiableValueAttributeStat(stats,type):new ModifiableValue(stats,type);
  Set(stat,"m_BaseValue",10);
  Set(stat,"ModifiedValue",10);
  Set(stat,"PermanentValue",10);
  Set(stats,type.ToString(),stat);stats.AllStats.Add(stat);}
  var body=Empty<UnitBody>();
  Set(body,"Owner",d);
  Set(body,"m_HandsAreEnabled",true);
  Set(body,"m_AdditionalLimbs",new List<WeaponSlot>());var hands=Empty<HandsEquipmentSet>();
  Set(hands,"PrimaryHand",new HandSlot(d));
  Set(hands,"SecondaryHand",new HandSlot(d));
  Set(body,"m_HandsEquipmentSets",new[]{hands});
  Set(d,"Body",body);Part(u,new MechanicsFeatureExtension.MechanicsFeatureExtensionPart());Part(u,new OffHandParryUnitPart());return u;}
 static void Part(UnitEntityData u,EntityPart p){p.RestoreOwnerLink(u);((List<EntityPart>)Get(u.Parts,"m_Parts")).Add(p);AccessTools.Method(typeof(EntityPartsManager),"AddToCache").Invoke(u.Parts,new object[]{p});}
 static Feature Feature(UnitEntityData u,BlueprintFeature bp,int rank){var f=Empty<Feature>();
  Set(f,"Components",new List<EntityFactComponent>());f.RestoreOwnerLink(u.Facts);
  Set(f,"Param",new FeatureParam());
  Set(f,"Rank",rank);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(f,new object[]{bp});
  Set(f,"IsActive",true);
  Set(f,"IsTurnedOn",true);u.Facts.List.Add(f);Refresh(u);return f;}
 static Buff Fact(UnitEntityData u,BlueprintBuff bp,UnitEntityData caster){var b=Empty<Buff>();
  Set(b,"Components",new List<EntityFactComponent>());b.RestoreOwnerLink(u.Facts);
  Set(b,"m_Context",new MechanicsContext(caster,u.Descriptor,bp,null,new TargetWrapper(u)));
  Set(b,"Rank",1);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(b,new object[]{bp});
  Set(b,"IsActive",true);
  Set(b,"IsTurnedOn",true);foreach(var r in b.Components)Set(r,"IsTurnedOn",true);u.Facts.List.Add(b);Refresh(u);return b;}
 static void Call(UnitFact f,Type t,string m,object evt=null){var r=f.Components.Single(c=>c.SourceBlueprintComponent.GetType()==t);using(r.RequestEventContext())AccessTools.Method(t,m).Invoke(r.SourceBlueprintComponent,evt==null?null:new[]{evt});}
 static ItemEntityWeapon Weapon(WeaponCategory category){var type=new BlueprintWeaponType{Category=category};
  Set(type,"m_AttackType",AttackType.Melee);var bp=new BlueprintItemWeapon();
  Set(bp,"m_Type",Ref<BlueprintWeaponTypeReference>(type));var w=Empty<ItemEntityWeapon>();
  Set(w,"m_Blueprint",bp);return w;}
 static ItemEntityShield Equip(UnitEntityData u,ArmorProficiencyGroup group){var type=new BlueprintShieldType();
  Set(type,"m_ProficiencyGroup",group);var armor=new BlueprintItemArmor();
  Set(armor,"m_Type",Ref<BlueprintArmorTypeReference>(type));var bp=new BlueprintItemShield();
  Set(bp,"m_ArmorComponent",Ref<BlueprintItemArmorReference>(armor));var shield=Empty<ItemEntityShield>();
  Set(shield,"m_Blueprint",bp);var ac=Empty<ItemEntityArmor>();
  Set(ac,"m_Blueprint",armor);
  Set(shield,"ArmorComponent",ac);var w=Weapon(WeaponCategory.WeaponLightShield);
  Set(w,"Shield",shield);
  Set(shield,"WeaponComponent",w);w.HoldingSlot=u.Body.SecondaryHand;shields[u.Body.SecondaryHand]=shield;
  Set(u.Body.SecondaryHand,"m_FakeItem",shield);
  Set(u.Body.SecondaryHand,"m_IsItemFake",true);
  Set(u.Body.PrimaryHand,"m_FakeItem",Weapon(WeaponCategory.Longsword));
  Set(u.Body.PrimaryHand,"m_IsItemFake",true);u.Proficiencies.Add(group);return shield;}
 static RuleRollD20 DiceOutcome(int value){var die=Empty<RuleRollD20>();
  Set(die,"m_Result",value);return die;}
 static RuleAttackRoll Incoming(UnitEntityData source,UnitEntityData target){var r=Empty<RuleAttackRoll>();
  Set(r,"Initiator",source);
  Set(r,"Target",target);var ws=Empty<RuleCalculateWeaponStats>();
  Set(ws,"Weapon",Weapon(WeaponCategory.Longsword));
  Set(r,"WeaponStats",ws);
  Set(r,"Result",AttackResult.Miss);return r;}
 static void ParryScenarios(){Console.WriteLine("Parry runtime and save lifecycle");var u=Unit("shield-user");var enemy=Unit("enemy");Equip(u,ArmorProficiencyGroup.Buckler);var p=u.Get<OffHandParryUnitPart>();var mechanics=u.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>();var effect=(BlueprintBuff)cache.Load(BlueprintGuid.Parse("a8f2b254bc2e4f8b95e12aa444287c58"));var b=Fact(u,effect,u);tb=true;now=TimeSpan.FromSeconds(100);
  Call(b,typeof(OffHandParryComponent),"OnActivate");
  Check(p.activated&&p.n==3&&mechanics.ShieldDenied&&mechanics.ForceDualWieldingPenalties,"native attack enumerator supplies 0/5/10 and flags");
  Check(!OffHandParryUnitPart.flag&&!u.State.Features.ShieldBash&&u.Body.SecondaryHand.MaybeWeapon==null,"calculation flag and native shield-bash flag both off between attacks");var incoming=Incoming(enemy,u);p.OnEventAboutToTrigger(incoming);
  Check(p.activated&&incoming.Parry!=null,"ordinary incoming retains parry despite hidden MaybeWeapon");p.OnEventDidTrigger(incoming);
  Check(p.n==2&&p.State.Used==1,"actual attempted parry consumes one");p.OnEventDidTrigger(incoming);
  Check(p.n==2,"duplicate event cannot consume twice");
  var data=JsonConvert.DeserializeObject<OffHandParryData>(JsonConvert.SerializeObject(p.TurnState));
  Check(data.Used==1&&data.AttackPenalties.SequenceEqual(new[]{0,5,10}),"spent penalty roster survives JSON");
  Call(b,typeof(OffHandParryComponent),"OnTurnOff");
  Check(!p.activated&&!mechanics.ShieldDenied&&!mechanics.ForceDualWieldingPenalties,"native turn-off releases both flags");
  p.TurnState=data;int beforeRestore=countCalls;
  Call(b,typeof(OffHandParryComponent),"OnTurnOn");
  Check(p.activated&&p.n==2&&countCalls==beforeRestore,"turn-on restores JSON spent state without regenerating roster");
  Call(b,typeof(OffHandParryComponent),"OnApplyPostLoadFixes");p.Deactivate();
  Check(!mechanics.ShieldDenied&&!mechanics.ForceDualWieldingPenalties,"postload rebinding never double-retains");var repeated=new OffHandParryData();p.TryActivate(WeaponCategory.WeaponLightShield,false,repeated);
  Check(p.n==2&&repeated.Used==1,"replacement effect cannot refill same turn");p.HandleTurnStarted(enemy);
  Check(p.n==2,"other actor turn cannot refill");p.HandleTurnStarted(u);p.TryActivate(WeaponCategory.WeaponLightShield,false,repeated);
  Check(p.n==3&&repeated.Used==0,"owner next turn starts new budget");shields[u.Body.SecondaryHand]=null;
  Set(u.Body.SecondaryHand,"m_FakeItem",null);p.Refresh();
  Check(!p.activated&&!mechanics.ShieldDenied&&!mechanics.ForceDualWieldingPenalties,"changing equipment releases all flags");Equip(u,ArmorProficiencyGroup.Buckler);
  var baseTWF=(BlueprintFeature)cache.Load(BlueprintGuid.Parse("6948b379c0562714d9f6d58ccbfa8faa"));var rank=Feature(u,baseTWF,2);var mf=Mythic_BucklerParry.Mythic_BucklerParryFeature;mythicFact=Feature(u,mf,1);
  foreach(var r in new[]{2,3,4}){Set(rank,"Rank",r);p.HandleTurnStarted(u);p.TryActivate(WeaponCategory.WeaponLightShield,false,new OffHandParryData());
  Check(lastCount.Result.SecondaryHand.AdditionalAttacks==r&&p.n==r+2,"actual native feature rank fallback adds full-AB "+(r-1));}mythicFact=null;p.Deactivate();
 }
 static void Defense(){Console.WriteLine("Shielded Defense native property, ranks and consumption");var u=Unit("defender");var property=(Kingmaker.UnitLogic.Mechanics.Properties.BlueprintUnitProperty)cache.Load(BlueprintGuid.Parse("fa65cbcf6bba4f3992261173381a3874"));var bp=(BlueprintBuff)cache.Load(BlueprintGuid.Parse("29397505648a462a874872b132a00b75"));foreach(var group in new[]{ArmorProficiencyGroup.LightShield,ArmorProficiencyGroup.HeavyShield,ArmorProficiencyGroup.TowerShield})foreach(int bab in new[]{0,1,3,4,6,8,12,20}){Equip(u,group);
  Set(u.Stats.BaseAttackBonus,"m_BaseValue",bab);
  Set(u.Stats.BaseAttackBonus,"ModifiedValue",bab);var b=Fact(u,bp,u);
  Call(b,typeof(BuffSetRanksOnApply),"SetRanks");int expected=1+bab/(group==ArmorProficiencyGroup.LightShield?6:4);
  Check(b.Rank==2+bab/(group==ArmorProficiencyGroup.LightShield?3:2),"native property includes BaseValue2");
  Check(b.Context[AbilityRankType.StatBonus]==expected,"native Div2 AC growth "+group+"/"+bab);using(b.Context.GetDataScope(u)){new ContextActionRemoveBuffRanksCustom{m_Buff=Ref<BlueprintBuffReference>(bp),min=0,value=2,ToCaster=true,RemoveWhenZero=false}.RunAction();}Check(b.Rank==Math.Max(0,property.GetInt(u)-2)&&b.Context[AbilityRankType.StatBonus]==Math.Max(0,expected-1),"actual custom/native SetRank loses one AC per attack");b.Remove();}
  var mythic=Feature(u,Mythic_ShieldedDefense.Feature,1);
  foreach(var group in new[]{ArmorProficiencyGroup.LightShield,ArmorProficiencyGroup.HeavyShield,ArmorProficiencyGroup.TowerShield})foreach(int bab in new[]{0,2,3,4,6,12,20}){
   Equip(u,group);Set(u.Stats.BaseAttackBonus,"m_BaseValue",bab);Set(u.Stats.BaseAttackBonus,"ModifiedValue",bab);var host=Fact(u,bp,u);Call(host,typeof(BuffSetRanksOnApply),"SetRanks");int start=1+bab/(group==ArmorProficiencyGroup.LightShield?6:4);
   Check(host.Context[AbilityRankType.StatBonus]==start&&host.Rank==2*start+1,"mythic even/odd normalization preserves initial maximum");
   Check(host.Context[AbilityRankType.ProjectilesCount]==1,"native mythic rank spends one charge");
   for(int i=1;i<=4;i++){using(host.Context.GetDataScope(u))new ContextActionRemoveBuffRanksCustom{m_Buff=Ref<BlueprintBuffReference>(bp),min=0,value=host.Context[AbilityRankType.ProjectilesCount],ToCaster=true,RemoveWhenZero=false}.RunAction();Check(host.Context[AbilityRankType.StatBonus]==Math.Max(0,start-i/2),"mythic actual charge sequence keeps first hit and falls second "+group+"/"+bab+"/"+i);}host.Remove();
  }
 }
 static void Style(){Console.WriteLine("Upsetting native component null source, counters, expiry and AoO");var user=Unit("style-user");var a=Unit("style-a");var b=Unit("style-b");var main=Fact(user,UpsettingShieldStyle.MainBuff,user);
  Call(main,typeof(UpsettingShieldComponent),"OnEventDidTrigger",new RuleDealDamage(user,a,new DamageBundle()));
  Check(a.Buffs.RawFacts.Count==0,"null-weapon damage safely ignored");var ledger=Fact(user,UpsettingShieldStyle.checker,user);
  Call(ledger,typeof(UpsettingRoundTracker),"OnActivate");var rt=ledger.Components.Single(x=>x.SourceBlueprintComponent is UpsettingRoundTracker);using(rt.RequestEventContext()){var component=(UpsettingRoundTracker)rt.SourceBlueprintComponent;
  Check(component.TrySpend(a)&&!component.TrySpend(a)&&component.TrySpend(b),"actual component shared limit once per enemy");var saved=JsonConvert.DeserializeObject<UpsettingRoundData>(JsonConvert.SerializeObject(rt.GetData<UpsettingRoundData>()));
  Check(saved.TriggeredTargets.SetEquals(new[]{a.UniqueId,b.UniqueId}),"AoO target ledger saves both enemies");component.HandleTurnStarted(a);
  Check(ledger.IsActive,"other target turn retains ledger");component.HandleTurnStarted(user);
  Check(!ledger.IsActive,"caster next turn expires ledger");}
  ledger=Fact(user,UpsettingShieldStyle.checker,user);
  Call(ledger,typeof(UpsettingRoundTracker),"OnActivate");var penalty=Fact(a,UpsettingShieldStyle.StrikeBuff,user);var attack=Empty<RuleAttackWithWeapon>();
  Set(attack,"Initiator",a);
  Set(attack,"Target",user);
  Set(attack,"Weapon",Weapon(WeaponCategory.Longsword));var roll=Incoming(a,user);
  Set(roll,"AttackBonus",10);
  Set(roll,"D20",DiceOutcome(10));
  Set(roll,"TargetAC",25);
  Set(attack,"AttackRoll",roll);aoos=0;
  Call(penalty,typeof(AoOOnFarMiss),"OnEventDidTrigger",attack);
  Call(penalty,typeof(AoOOnFarMiss),"OnEventDidTrigger",attack);
  Check(aoos==1,"actual miss-by5 triggers once and duplicate cannot repeat");
  Set(roll,"Result",AttackResult.Hit);
  Set(roll,"D20",DiceOutcome(20));
  Set(roll,"TargetAC",40);
  Call(penalty,typeof(AoOOnFarMiss),"OnEventDidTrigger",attack);
  Check(aoos==1,"natural20 hit never triggers far-miss AoO");
  Call(penalty,typeof(UpsettingPenaltyLifetime),"OnActivate");
  Call(penalty,typeof(UpsettingPenaltyLifetime),"HandleTurnStarted",user);
  Check(!penalty.IsActive,"actual enemy penalty expires at caster own turn");
  var shield=Equip(user,ArmorProficiencyGroup.Buckler);var bundle=new DamageBundle{Weapon=shield.WeaponComponent};var damage=new RuleDealDamage(user,a,bundle);Set(damage,"Result",5);
  Call(main,typeof(UpsettingShieldComponent),"OnEventDidTrigger",damage);
  Call(main,typeof(UpsettingShieldComponent),"OnEventDidTrigger",damage);
  Check(a.Buffs.RawFacts.Count(x=>x.Blueprint==UpsettingShieldStyle.StyleBuff&&x.Context.MaybeCaster==user)==1,"actual repeated same-caster shield damage replaces penalty");
  Feature(user,UpsettingShieldStyle.Strike,1);
  Call(main,typeof(UpsettingShieldComponent),"OnEventDidTrigger",damage);
  Call(main,typeof(UpsettingShieldComponent),"OnEventDidTrigger",damage);
  Check(a.Buffs.RawFacts.Count(x=>x.Blueprint==UpsettingShieldStyle.StrikeBuff&&x.Context.MaybeCaster==user)==1&&!a.Buffs.HasFact(UpsettingShieldStyle.StyleBuff),"actual strike upgrade uses correct registered buff without stacking old style");
  main.Remove();main=Fact(user,UpsettingShieldStyle.MainBuff,user);
  var retained=user.Buffs.GetBuff(UpsettingShieldStyle.checker);var retainedRuntime=retained.Components.Single(x=>x.SourceBlueprintComponent is UpsettingRoundTracker);using(retainedRuntime.RequestEventContext())Check(!((UpsettingRoundTracker)retainedRuntime.SourceBlueprintComponent).TrySpend(a),"style reopen and repeat shield hits cannot restore spent enemy AoO");
 }
 static void LightFeat(){var u=Unit("light-candidate");var f=LightShieldParry.Feature;
  Check(f!=null&&f.AssetGuid.ToString()==LightShieldParry.Guid&&f.Groups.Contains(FeatureGroup.CombatFeat),"real single light parry feat identity and pool");var p=f.GetComponent<PrerequisiteProficiency>();bool Need()=> (bool)AccessTools.Method(typeof(PrerequisiteProficiency),"CheckInternal").Invoke(p,new object[]{null,u.Descriptor,null});
  Check(!Need(),"native unproficient candidate rejected");u.Proficiencies.Add(ArmorProficiencyGroup.Buckler);
  Check(!Need(),"buckler proficiency alone cannot qualify light feat");u.Proficiencies.Add(ArmorProficiencyGroup.LightShield);
  Check(Need(),"native light shield proficiency qualifies");
  Check(f.GetComponent<Kingmaker.UnitLogic.FactLogic.AddFacts>().m_Facts.Single().Guid.ToString()=="e547039b227a42d7a8d98fad72f8717c","real feat grants shared parry engine");}
 static bool False(ref bool __result){__result=false;return false;}
 static void FeatPrerequisites(){
  Console.WriteLine("Native CanSelect Shield Brace/Unhindering prerequisites");
  Add(new BlueprintCharacterClass(),"48ac8db94d5de7645906c7d0ad3bcfbd");Add(new BlueprintFeature(),"ac57069b6bf8c904086171683992a92a");Add(new BlueprintFeature(),"3c380607706f209499d951b29d3c44f3");
  ShieldBrace.CachePatch.Postfix();UnhinderingShield.BlueprintsCache_Patch();
  Patch(AccessTools.Method(typeof(BlueprintFeatureSelection),"IsIgnorePrerequisites"),nameof(False));Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites),"Ignore"),nameof(False));
  var pool=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("247a4068296e8be42890143f451b4b45"));var selected=new FeatureSelectionState(null,default,pool,0,1);var level=Empty<LevelUpState>();
  foreach(var id in new[]{"bacc4f29695d4aec868d31be1c04f29b","39470145204746cdb46cd95bcee4a92c"}){
   var bp=(BlueprintFeature)cache.Load(BlueprintGuid.Parse(id));var u=Unit("feat-prerequisite-"+id);var list=bp.GetComponent<PrerequisiteFeaturesFromList>();Check(list.Amount==1,"explicitly one shield-focus/armor-training prerequisite");
   bool Can()=>pool.CanSelect(u.Descriptor,level,selected,bp);Check(!Can(),"missing native proficiency denies selection");
   u.Proficiencies.Add(id.StartsWith("bacc")?ArmorProficiencyGroup.LightShield:ArmorProficiencyGroup.Buckler);Check(!Can(),"one native proficiency alone does not bypass Shield Focus/Armor Training");
   var focus=Feature(u,(BlueprintFeature)cache.Load(BlueprintGuid.Parse("ac57069b6bf8c904086171683992a92a")),1);
   Check(Can(),"one proficient shield and native Shield Focus qualifies full CanSelect");
   u.Facts.List.Remove(focus);Refresh(u);Feature(u,(BlueprintFeature)cache.Load(BlueprintGuid.Parse("3c380607706f209499d951b29d3c44f3")),1);Check(Can(),"native Armor Training independently substitutes for Shield Focus");
   if(id.StartsWith("bacc")){u.Proficiencies.Clear();u.Proficiencies.Add(ArmorProficiencyGroup.HeavyShield);Check(Can(),"native proficiency array accepts heavy shield alone");u.Proficiencies.Clear();u.Proficiencies.Add(ArmorProficiencyGroup.TowerShield);Check(Can(),"native proficiency array accepts tower shield alone");}
   Set(u.Stats.BaseAttackBonus,"m_BaseValue",0);Set(u.Stats.BaseAttackBonus,"ModifiedValue",0);Set(u.Stats.BaseAttackBonus,"PermanentValue",0);Check(!Can(),"native BAB/class Any group still requires one threshold");
  }
  var candidate=Unit("light-native-canselect");Check(!pool.CanSelect(candidate.Descriptor,level,selected,LightShieldParry.Feature),"full native CanSelect rejects unproficient light parry candidate");candidate.Proficiencies.Add(ArmorProficiencyGroup.LightShield);Check(pool.CanSelect(candidate.Descriptor,level,selected,LightShieldParry.Feature),"full native CanSelect accepts light shield proficiency for new feat");
 }
}
