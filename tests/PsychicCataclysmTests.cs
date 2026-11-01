using System;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums.Damage;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Components;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicCataclysmTests
{
 static readonly List<RuleDealDamage> damageRules=new List<RuleDealDamage>();static int count,saves,kills,removed,knocks,skills;static bool passed,skillPass,air;static double now;static EntityFactsManager facts;static readonly List<ContextActionDealDamage> damage=new List<ContextActionDealDamage>();static readonly List<int> hp=new List<int>();static readonly List<ContextActionApplyBuff> applied=new List<ContextActionApplyBuff>();
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);static void Check(bool b,string m){if(!b)throw new Exception("Cataclysm: "+m);count++;}
 static T Asset<T>(BlueprintsCache c,string n)where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicSuffocation).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
 static MechanicsContext Context(UnitEntityData u,BlueprintScriptableObject bp,int cl=10,Metamagic meta=0){var c=new MechanicsContext(u,u.Descriptor,bp,null,new TargetWrapper(u));c.SetParams(new AbilityParams {CasterLevel=cl,DC=23,SpellLevel=7,Metamagic=meta});var ranks=new int[32];foreach(var rank in bp.GetComponents<ContextRankConfig>())ranks[(int)(AbilityRankType)Get(rank,"m_Type")]=rank.GetValue(c);Set(c,"m_Ranks",ranks);return c;}
 static void Clear(){facts.List.Clear();Refresh();}static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
 static Buff Fact(BlueprintBuff bp,MechanicsContext ctx){var f=Empty<Buff>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"m_Context",ctx);Set(f,"<Rank>k__BackingField",1);f.RestoreOwnerLink(facts);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(f,new object[]{bp});Set(f,"<IsActive>k__BackingField",true);Set(f,"<IsTurnedOn>k__BackingField",true);foreach(var rt in f.Components)Set(rt,"<IsTurnedOn>k__BackingField",true);facts.List.Add(f);Refresh();return f;}
 internal static int Run(BlueprintsCache c){
  var u=new Psychic070Tests.Caster(c).Unit;facts=new EntityFactsManager(u);Set(u,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(u,"<Buffs>k__BackingField",buffs);var strength=new ModifiableValueAttributeStat(u.Stats,StatType.Strength){BaseValue=10};Set(u.Stats,"Strength",strength);u.Stats.AllStats.Add(strength);var intelligence=new ModifiableValueAttributeStat(u.Stats,StatType.Intelligence){BaseValue=10};Set(u.Stats,"Intelligence",intelligence);u.Stats.AllStats.Add(intelligence);var arcana=new ModifiableValueSkill(u.Stats,intelligence,StatType.SkillKnowledgeArcana);Set(u.Stats,"SkillKnowledgeArcana",arcana);u.Stats.AllStats.Add(arcana);
  var old=Kingmaker.Game.Instance.TimeController;Set(Kingmaker.Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());var h=new Harmony("CotW01321.Cataclysm");void Patch(System.Reflection.MethodBase m,string n)=>h.Patch(m,prefix:new HarmonyMethod(typeof(PsychicCataclysmTests),n){priority=Priority.First});
  Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Pass));Patch(AccessTools.PropertyGetter(typeof(RuleSkillCheck),"Success"),nameof(SkillSuccess));Patch(AccessTools.Method(typeof(RuleSkillCheck),"AdjustDCByDifficulty"),nameof(Difficulty));Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),nameof(Damage));Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(Apply));Patch(AccessTools.Method(typeof(PsychicSuffocationHP),"RunAction"),nameof(HP));Patch(AccessTools.Method(typeof(ContextActionKill),"RunAction"),nameof(Kill));Patch(AccessTools.Method(typeof(Buff),"Remove"),nameof(Remove));Patch(AccessTools.Method(typeof(ContextActionKnockdownTarget),"RunAction"),nameof(Knock));Patch(AccessTools.Method(typeof(UnitHelper),"HasFact",new[]{typeof(UnitDescriptor),typeof(Kingmaker.Blueprints.Facts.BlueprintFact)}),nameof(HasFact));
  PsychicRuleTestBoundary.Patch(h,typeof(RuleSavingThrow),AccessTools.Method(typeof(PsychicCataclysmTests),nameof(Save)));PsychicRuleTestBoundary.Patch(h,typeof(RuleSkillCheck),AccessTools.Method(typeof(PsychicCataclysmTests),nameof(Skill)));
  try{Metadata(c,u);Suffocation(c,u);Control(c,u);Winds(c,u);Health(c,u,h);}finally{h.UnpatchAll(h.Id);Set(Kingmaker.Game.Instance,"TimeController",old);Clear();}
  PsychicExpansionTests.Inventory["cataclysmSpellAssertions"]=count;Console.WriteLine("PASS: "+count+" cataclysm checks; original lists, durations, native save actions, staged death, skill checks, pertarget round gates and persistent component data; scene/damage/HP endpoints seeded.");return count;
 }
 static void Metadata(BlueprintsCache c,UnitEntityData u){
  string[] tokens={"Suffocation","SuffocationMass","ControlConstruct","ScouringWinds"};int[] levels={5,9,7,7};for(int i=0;i<4;i++){var a=Asset<BlueprintAbility>(c,tokens[i]);var lists=a.GetComponents<SpellListComponent>().ToArray();Check(lists.Length==(i>=3?2:1)&&lists.All(x=>x.SpellLevel==levels[i]),"Original classlevels");foreach(var l in lists)Check(l.SpellList.SpellsByLevel[l.SpellLevel].Spells.Count(x=>x==a)==1,"Lists reciprocal unique");Check(a.SpellResistance==(i<2),"SR only original spells");}
  foreach(var token in tokens.Where(t=>t!="ScourgeOfHorsemen"))foreach(int cl in new[]{1,5,10,20,30})foreach(bool extend in new[]{false,true}){var a=Asset<BlueprintAbility>(c,token);var ctx=Context(u,a,cl,extend?Metamagic.Extend:0);ContextActionApplyBuff apply;if(token.StartsWith("Suffocation")){var gate=(Conditional)a.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();var save=(ContextActionSavingThrow)gate.IfTrue.Actions.Single();var conditional=(ContextActionConditionalSaved)save.Actions.Actions.Single();apply=(ContextActionApplyBuff)conditional.Failed.Actions.Single();Check(((ContextActionApplyBuff)conditional.Succeed.Actions.Single()).DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds(6),"Initial success staggered1round no Extend");}else apply=(ContextActionApplyBuff)a.GetComponent<AbilityEffectRunAction>().Actions.Actions[0];Check(apply.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds((token=="Suffocation"?3:cl)*6*(extend?2:1)),"CL/fixed duration and Extend");}
  var b=Asset<BlueprintBuff>(c,"ControlConstructBuff");Check(b.GetComponents<ChangeFaction>().Count()==1&&b.GetComponent<UniqueBuff>()!=null&&b.GetComponent<SpellDescriptorComponent>()==null,"Native faction cleanup/unique caster; no mental immunity/repeated Will");var wind=Asset<BlueprintAbilityAreaEffect>(c,"ScouringWindsArea");Check(wind.Size.Value==20&&!wind.SpellResistance&&wind.GetComponent<UniqueAreaEffect>().Feature==Asset<BlueprintBuff>(c,"ScouringWindsController"),"Unique sourceowned sandstorm20");Check(wind.GetComponents<AbilityAreaEffectBuff>().All(x=>x.Buff.Stacking==StackingType.Stack&&x.CheckConditionEveryRound),"Native area-owned conditions support overlaps");
 }
 static void Suffocation(BlueprintsCache c,UnitEntityData u){
  foreach(var token in new[]{"Suffocation","SuffocationMass"})foreach(bool result in new[]{false,true}){passed=result;applied.Clear();saves=0;var a=Asset<BlueprintAbility>(c,token);using(Context(u,a).GetDataScope(u))a.GetComponent<AbilityEffectRunAction>().Actions.Run();Check(saves==1&&applied.Count==1&&applied[0].Buff==(result?(BlueprintBuff)c.Load(BlueprintGuid.Parse("df3950af5a783bd4d91ab73eb8fa0fd3")):Asset<BlueprintBuff>(c,"SuffocationBuff")),"Initial native save branches");}
  for(int pattern=0;pattern<8;pattern++){Clear();now=100;kills=saves=0;hp.Clear();var b=Fact(Asset<BlueprintBuff>(c,"SuffocationBuff"),Context(u,Asset<BlueprintAbility>(c,"Suffocation")));var comp=b.Blueprint.GetComponent<PsychicSuffocation>();var rt=((List<EntityFactComponent>)Get(b,"Components")).Single(x=>x.SourceBlueprintComponent is PsychicSuffocation);using(rt.RequestEventContext())AccessTools.Method(typeof(PsychicSuffocation),"OnActivate").Invoke(comp,null);b.CallComponents<Kingmaker.Controllers.Units.ITickEachRound>(x=>x.OnNewRound());Check(saves==0,"No immediate first step");int failed=0;for(int round=1;round<=3;round++){now=100+6*round;passed=(pattern&(1<<(round-1)))!=0;if(!passed)failed++;b.CallComponents<Kingmaker.Controllers.Units.ITickEachRound>(x=>x.OnNewRound());b.CallComponents<Kingmaker.Controllers.Units.ITickEachRound>(x=>x.OnNewRound());var state=rt.GetData<PsychicSuffocationState>();Check(saves==round&&state.Stage==failed&&kills==(failed>=3?1:0),"Eachround successful save delays; duplicate tick cannot repeat");Check(hp.SequenceEqual(Enumerable.Range(0,Math.Min(failed,2)).Select(n=>-n)),"0 then-1 never reversed");var copy=Newtonsoft.Json.JsonConvert.DeserializeObject<PsychicSuffocationState>(Newtonsoft.Json.JsonConvert.SerializeObject(state));Check(copy.Stage==state.Stage&&copy.NextTick==state.NextTick&&copy.Initialized,"Saved stage and clock preserved");using(rt.RequestEventContext())AccessTools.Method(typeof(PsychicSuffocation),"OnActivate").Invoke(comp,null);Check(state.Stage==copy.Stage&&state.NextTick==copy.NextTick,"Reactivate not reset");}}
  Clear();
 }
 static void Control(BlueprintsCache c,UnitEntityData u){foreach(bool success in new[]{false,true}){Clear();skillPass=success;removed=skills=0;var b=Fact(Asset<BlueprintBuff>(c,"ControlConstructBuff"),Context(u,Asset<BlueprintAbility>(c,"ControlConstruct")));b.CallComponents<Kingmaker.Controllers.Units.ITickEachRound>(x=>x.OnNewRound());Check(skills==1&&removed==(success?0:1),"Caster Arcana pass retains control/failure removes");}Clear();}
 static void Winds(BlueprintsCache c,UnitEntityData u){
  var area=Asset<BlueprintAbilityAreaEffect>(c,"ScouringWindsArea");var pulse=area.GetComponent<AbilityAreaEffectRunAction>();var movement=(PsychicScouringMovement)area.GetComponents<AbilityAreaEffectBuff>().Single(x=>x.Buff.name.EndsWith("Stuck")).Condition.Conditions.Single();
  foreach(var size in new[]{Size.Tiny,Size.Small,Size.Medium,Size.Large})foreach(bool immune in new[]{false,true})foreach(bool success in new[]{false,true}){Clear();now=100;air=immune;skillPass=success;damage.Clear();knocks=skills=0;Set(u.State,"m_Size",(Size?)size);var host=Fact(Asset<BlueprintBuff>(c,"ScouringWindsController"),Context(u,Asset<BlueprintAbility>(c,"ScouringWinds")));var ctx=Context(u,area);using(ctx.GetDataScope(u)){pulse.UnitEnter.Run();pulse.Round.Run();pulse.UnitEnter.Run();Check(movement.Check()==(size<=Size.Medium&&!immune&&!success),"Strength10 movement only eligible size and no air immunity");}bool small=size<=Size.Small&&!immune&&!success;Check(damage.Count==(small?2:1)&&knocks==(small?1:0),"Piercing3d6 all; small failed Strength15 extra2d6/prone");Check(damage[0].Value.DiceCountValue.Calculate(ctx)==3&&damage[0].DamageType.Physical.Form==PhysicalDamageForm.Piercing,"Correct sand damage");now=106;using(ctx.GetDataScope(u))pulse.Round.Run();Check(damage.Count==(small?4:2),"Next round one new pulse");var rt=((List<EntityFactComponent>)Get(host,"Components")).Single(x=>x.SourceBlueprintComponent is PsychicScouringLedger);var data=rt.GetData<PsychicScouringState>();var copy=Newtonsoft.Json.JsonConvert.DeserializeObject<PsychicScouringState>(Newtonsoft.Json.JsonConvert.SerializeObject(data));Check(copy.NextAttack[u.UniqueId]==TimeSpan.FromSeconds(112),"Per-target ledger data saved");Clear();int previous=damage.Count;using(ctx.GetDataScope(u))pulse.UnitEnter.Run();Check(damage.Count==previous,"Removed host no orphan damage");}
  air=false;Clear();
 }
 static void Health(BlueprintsCache c,UnitEntityData u,Harmony h){
  h.Unpatch(AccessTools.Method(typeof(PsychicSuffocationHP),"RunAction"),AccessTools.Method(typeof(PsychicCataclysmTests),nameof(HP)));
  PsychicRuleTestBoundary.Patch(h,typeof(RuleDealDamage),AccessTools.Method(typeof(PsychicCataclysmTests),nameof(Deal)));
  var hpstat=Empty<ModifiableValueHitPoints>();Set(hpstat,"<ModifiedValue>k__BackingField",100);Set(u.Stats,"HitPoints",hpstat);var temp=new ModifiableValueTemporaryHitPoints(u.Stats);Set(u.Stats,"TemporaryHitPoints",temp);u.Stats.AllStats.Add(temp);
  foreach(int health in new[]{100,50,1,0,-1,-5})foreach(int target in new[]{0,-1}){u.Descriptor.Damage=100-health;damageRules.Clear();using(Context(u,Asset<BlueprintAbility>(c,"Suffocation")).GetDataScope(u))new PsychicSuffocationHP {Value=target}.RunAction();Check(damageRules.Count==(health>target?1:0),"Already below desiredHP never heals");if(damageRules.Count==1){var rule=damageRules.Single();var d=rule.DamageBundle.Single();Check(d is DirectDamage&&d.Bonus==health-target&&d.IgnoreReduction&&rule.MinHPAfterDamage==target&&rule.IgnoreGrowthByDifficulty,"Native direct damage exact delta, no reduction and floor prevents premature kill");}}
 }
 static bool Deal(RuleDealDamage rule,ref RuleDealDamage __result){damageRules.Add(rule);__result=rule;return false;}
 static bool Difficulty(int __0,ref int __1,ref int __result){__1=0;__result=__0;return false;}
 static bool Time(ref TimeSpan __result){__result=TimeSpan.FromSeconds(now);return false;}static bool Pass(ref bool __result){__result=passed;return false;}static bool SkillSuccess(ref bool __result){__result=skillPass;return false;}static bool Save(RuleSavingThrow rule,ref RuleSavingThrow __result){Check(rule.Type==SavingThrowType.Fortitude&&rule.DifficultyClassBase==23,"OriginalFort/DC");saves++;__result=rule;return false;}
 static bool Skill(RuleSkillCheck rule,ref RuleSkillCheck __result){Check(rule.StatType==StatType.Strength?rule.DC==10||rule.DC==15:rule.StatType==StatType.SkillKnowledgeArcana&&rule.DC==10+rule.Initiator.Progression.CharacterLevel,"Native skill/DC");skills++;__result=rule;return false;}
 static bool Damage(ContextActionDealDamage __instance){damage.Add(__instance);return false;}static bool Apply(ContextActionApplyBuff __instance){applied.Add(__instance);return false;}static bool HP(PsychicSuffocationHP __instance){hp.Add(__instance.Value);return false;}static bool Kill(){kills++;return false;}static bool Remove(){removed++;return false;}static bool Knock(){knocks++;return false;}static bool HasFact(Kingmaker.Blueprints.Facts.BlueprintFact __1,ref bool __result){__result=air&&__1.AssetGuid.ToString()=="dd3d0c7f4f57f304cbdbb68170b1b775";return false;}
}
