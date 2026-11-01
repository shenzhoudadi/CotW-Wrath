using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
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
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;
internal static class PsychicConduitTests {
 static int n,removed,heal,saves;static bool passed;static readonly List<ContextActionApplyBuff> applications=new List<ContextActionApplyBuff>();static UnitEntityData source,recipient;static EntityFactsManager sf,rf;
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));static object Get(object o,string s)=>AccessTools.Field(o.GetType(),s).GetValue(o);static void Set(object o,string s,object v)=>AccessTools.Field(o.GetType(),s).SetValue(o,v);static void Check(bool b,string s){if(!b)throw new Exception("Conduit: "+s);n++;}
 static T Asset<T>(BlueprintsCache c,string token)where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicJudgmentLink).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
 static EntityFactsManager Setup(UnitEntityData u){var fs=new EntityFactsManager(u);Set(u,"Facts",fs);var bc=new BuffCollection();bc.Initialize(fs);Set(u,"<Buffs>k__BackingField",bc);return fs;}
 static void Refresh(EntityFactsManager fs)=>AccessTools.Method(Get(fs,"m_Cache").GetType(),"Clear").Invoke(Get(fs,"m_Cache"),null);
 static MechanicsContext Context(BlueprintScriptableObject bp,int cl=12,Metamagic mm=0){var c=new MechanicsContext(source,recipient.Descriptor,bp,null,new TargetWrapper(source));c.SetParams(new AbilityParams {CasterLevel=cl,DC=23,SpellLevel=5,Metamagic=mm});var ranks=new int[32];foreach(var r in bp.GetComponents<ContextRankConfig>())ranks[(int)Get(r,"m_Type")]=r.GetValue(c);Set(c,"m_Ranks",ranks);return c;}
 static Buff Fact(BlueprintBuff bp,MechanicsContext ctx,EntityFactsManager fs){var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);Set(b,"Components",new List<EntityFactComponent>());Set(b,"m_Context",ctx);Set(b,"<Rank>k__BackingField",1);Set(b,"<IsActive>k__BackingField",true);Set(b,"<IsTurnedOn>k__BackingField",true);b.RestoreOwnerLink(fs);return b;}
 static EntityFactComponent Runtime(BlueprintComponent c,Buff f){var rt=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);rt.Setup(c);rt.RestoreOwnerLink(f);return rt;}
 internal static int Run(BlueprintsCache c){source=new Psychic070Tests.Caster(c).Unit;recipient=new Psychic070Tests.Caster(c).Unit;sf=Setup(source);rf=Setup(recipient);var h=new Harmony("CotW01323.ConduitTests");void Patch(System.Reflection.MethodBase m,string name)=>h.Patch(m,prefix:new HarmonyMethod(typeof(PsychicConduitTests),name){priority=Priority.First});
 Patch(AccessTools.Method(typeof(Buff),"Remove"),nameof(Remove));Patch(AccessTools.Method(typeof(ContextActionHealTarget),"RunAction"),nameof(Heal));Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(Apply));Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Passed));PsychicRuleTestBoundary.Patch(h,typeof(RuleSavingThrow),AccessTools.Method(typeof(PsychicConduitTests),nameof(Save)));Patch(AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),nameof(Add));
 try{Judgments(c);Babble(c);Shapes(c);}finally{h.UnpatchAll(h.Id);}
 PsychicExpansionTests.Inventory["conduitSpellAssertions"]=n;Console.WriteLine("PASS: "+n+" conduit/shape checks: native rank/duration/area ownership, source filtering, healing and child action dispatch; scene/HP/model endpoints not executed.");return n;
 }
 static void Judgments(BlueprintsCache c){var root=Asset<BlueprintAbility>(c,"LendJudgment");var choices=root.GetComponent<AbilityVariants>().Variants;Check(choices.Length==15&&root.GetComponent<SpellListComponent>().SpellLevel==1,"15 native judgment choices, single Inquisitor1 root");var greater=Asset<BlueprintAbility>(c,"LendJudgmentGreater");Check(greater.GetComponent<SpellListComponent>().SpellLevel==5,"Greater Inquisitor5");
 var inq=new BlueprintCharacterClass {AssetGuid=BlueprintGuid.Parse("f1a70d9e1b0b41e49874e1fa9052a1ce"),name="Inquisitor"};Set(inq,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());c.AddCachedBlueprint(inq.AssetGuid,inq);var cl=new ClassData(inq);Set(source.Descriptor.Progression,"Classes",new List<ClassData>{cl});
 foreach(var a in choices){Check(a.Parent==root&&!a.CanTargetSelf&&!a.CanTargetEnemies&&a.CanTargetFriends&&!a.SpellResistance,"Friendly other target and owned variant");var apply=(ContextActionApplyBuff)a.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();var b=apply.Buff;var link=b.GetComponent<PsychicJudgmentLink>();var check=a.GetComponent<PsychicJudgmentAvailable>();
 Check(b.GetComponent<AddFactContextActions>()==null&&!b.IsClassFeature,"Native self watcher/activation not copied");
 foreach(int level in new[]{1,3,5,10,17,20}){cl.Level=level;foreach(var r in b.GetComponents<ContextRankConfig>()){var native=link.Source.Get().GetComponents<ContextRankConfig>().Single(x=>(AbilityRankType)Get(x,"m_Type")== (AbilityRankType)Get(r,"m_Type"));Check(r.GetValue(Context(b))==native.GetValue(Context(link.Source.Get())),"Shared native caster progression matches source");}foreach(bool ext in new[]{false,true})Check(apply.DurationValue.Calculate(Context(a,level,ext?Metamagic.Extend:0)).Seconds==TimeSpan.FromSeconds(level*6*(ext?2:1)),"Native duration independent of benefit rank");}
 sf.List.Clear();Refresh(sf);Check(!check.IsCasterRestrictionPassed(source),"Inactive source cannot be lent");var src=Fact(link.Source.Get(),Context(b),sf);sf.List.Add(src);Refresh(sf);Check(check.IsCasterRestrictionPassed(source),"Active source can be lent");Set(src,"<IsTurnedOn>k__BackingField",false);Check(!check.IsCasterRestrictionPassed(source),"Turned off source rejected");Set(src,"<IsTurnedOn>k__BackingField",true);
 var own=Fact(b,Context(b),rf);var rt=Runtime(link,own);using(rt.RequestEventContext()){removed=heal=0;link.OnNewRound();Check(removed==0,"Active source retained");Check(heal==(a.name.EndsWith("LendJudgment1")?7:0),"Healing native rank executes only healing judgment at level20");link.HandleBuffDidRemoved(Fact(link.Source.Get(),Context(b),rf));Check(removed==0,"Recipient native judgment removal cannot remove caster link");link.HandleBuffDidRemoved(src);Check(removed==1,"Matching caster source removal ends link immediately");sf.List.Clear();Refresh(sf);heal=0;link.OnNewRound();Check(removed==2&&heal==0,"Round fallback removes without stale healing");}
 }
 var host=Asset<BlueprintBuff>(c,"LendJudgmentGreaterBuff");var all=(PsychicLendAll)host.GetComponent<AddFactContextActions>().Activated.Actions.Single();
 foreach(int count in new[]{0,1,3}){sf.List.Clear();for(int i=0;i<count;i++)sf.List.Add(Fact(all.Sources[i].Get(),Context(host),sf));Refresh(sf);applications.Clear();using(Context(host).GetDataScope(recipient))all.RunAction();Check(applications.Count==count,"Greater snapshots active subset");for(int i=0;i<count;i++)Check(applications[i].Buff==all.Shared[i].Get()&&applications[i].AsChild&&applications[i].SameDuration&&applications[i].IsNotDispelable,"Shared children bound to parent duration/dispel");}
 sf.List.Clear();Refresh(sf);
 }
 static void Babble(BlueprintsCache c){var a=Asset<BlueprintAbility>(c,"Babble");var host=Asset<BlueprintBuff>(c,"BabbleBuff");var area=Asset<BlueprintAbilityAreaEffect>(c,"BabbleArea");var owned=area.GetComponent<AbilityAreaEffectBuff>();var save=(PsychicBabbleSave)owned.Condition.Conditions.Single();
 Check(a.GetComponents<SpellListComponent>().Select(x=>x.SpellLevel).OrderBy(x=>x).SequenceEqual(new[]{2,3})&&a.SpellResistance,"Bard2/Wizard3 SR");Check(host.GetComponent<AddCondition>().Condition==UnitCondition.Nauseated&&host.GetComponent<SpellDescriptorComponent>().Descriptor.HasAnyFlag(SpellDescriptor.Compulsion),"Nonpoison nauseated compulsion host");Check(area.Size.Value==30&&!owned.CheckConditionEveryRound&&owned.Buff.Stacking==StackingType.Stack,"30ft independent aura, entry save only");var trigger=owned.Buff.GetComponent<AddIncomingDamageTrigger>();Check(trigger.Actions.Actions.Single()is ContextActionRemoveSelf,"Damage breaks only self and grants no unrelated Bard immunity");var silence=save.Silence.Get();Check(silence.GetComponents<SuppressBuffs>().Any(s=>s.Buffs.Contains(host)&&s.Buffs.Contains(owned.Buff)),"Silence suppresses host and listeners");
 var enter=AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitEnter");var exit=AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitExit");var round=AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnRound");var ctx=Context(a);var one=Empty<AreaEffectEntityData>();Set(one,"<UniqueId>k__BackingField","one");var two=Empty<AreaEffectEntityData>();Set(two,"<UniqueId>k__BackingField","two");
 foreach(bool pass in new[]{false,true}){rf.List.Clear();Refresh(rf);saves=0;passed=pass;enter.Invoke(owned,new object[]{ctx,one,recipient});Check(saves==1&&rf.List.Count==(pass?0:1),"Initial listener Will branch");round.Invoke(owned,new object[]{ctx,one});Check(saves==1,"No repeat save each round");if(!pass){enter.Invoke(owned,new object[]{ctx,two,recipient});Check(rf.List.Count==2,"Two independent fascinations");exit.Invoke(owned,new object[]{ctx,one,recipient});Check(rf.List.Count==1&&((Buff)rf.List.Single()).SourceAreaEffectId=="two","Exit removes only matching source");((Buff)rf.List.Single()).Remove();round.Invoke(owned,new object[]{ctx,two});Check(rf.List.Count==0&&saves==2,"Broken fascination stays gone inside same aura");enter.Invoke(owned,new object[]{ctx,two,recipient});Check(saves==3&&rf.List.Count==1,"Reentry can fascinate again");}}
 rf.List.Clear();rf.List.Add(Fact(silence,ctx,rf));Refresh(rf);saves=0;enter.Invoke(owned,new object[]{ctx,one,recipient});Check(saves==0&&rf.List.Count==1,"Silenced listener not affected");using(ctx.GetDataScope(source))Check(!save.Check(),"Babbling host does not fascinate itself");rf.List.Clear();Refresh(rf);
 }
 static void Shapes(BlueprintsCache c){var root=Asset<BlueprintAbility>(c,"MagicalBeastShape");var children=root.GetComponent<AbilityVariants>().Variants;Check(children.Length==2&&root.GetComponent<SpellListComponent>().SpellLevel==7,"Two forms one Wizard7 root");foreach(var a in children){bool hydra=a.name.EndsWith("Hydra");var acts=a.GetComponent<AbilityEffectRunAction>().Actions.Actions;var apply=(ContextActionApplyBuff)acts[1];var b=apply.Buff;var p=b.GetComponent<Polymorph>();Check(acts[0]is ContextActionRemoveBuffsByDescriptor&&a.Parent==root,"Native shape replacement before application");Check(p.Size==Size.Huge&&p.StrengthBonus==8&&p.ConstitutionBonus==2&&p.DexterityBonus==-4&&p.NaturalArmor==7,"CotW Huge form traits");Check(p.AdditionalLimbs.Length==(hydra?3:1)&&p.MainHand!=null&&p.OffHand!=null,"Five bites or bite and two claws");Check(p.Facts.Any(x=>x.AssetGuid.ToString()=="bd09b025ee2a82f46afab922c4decca9")&&p.Facts.Any(x=>x.AssetGuid.ToString()=="20b57bab6bac9b04493491432bcb6868"),"Native return and pounce owned by polymorph");Check((string)Get(Get(p,"m_Prefab"),"AssetId")== (hydra?"b4b64be1ff38b2d44a48e58e9ac3a75d":"46b4f98ae7d8ddf40878914741a42129"),"Installed native creature prefab");Check(!(bool)Get(p,"m_KeepSlots"),"Equipment handled by native polymorph");foreach(int cl in new[]{1,10,20})foreach(bool ext in new[]{false,true})Check(apply.DurationValue.Calculate(Context(a,cl,ext?Metamagic.Extend:0)).Seconds==TimeSpan.FromMinutes(cl*(ext?2:1)),"CL and Extend form duration");if(hydra)Check(p.Facts.Any(x=>x.AssetGuid.ToString()=="37a5e51e9e3a23049a77ba70b4e7b2d2")&&p.Facts.Any(x=>x.AssetGuid.ToString()=="c1b26f97b974aec469613f968439e7bb"),"Native healing and trip immunity");else Check(b.GetComponent<Blindsense>().Blindsight&&b.GetComponent<Blindsense>().Range.Value==60,"Original CotW blindsight adaptation");}}
 static bool Remove(Buff __instance){removed++;rf.List.Remove(__instance);Refresh(rf);return false;}
 static bool Heal(ContextActionHealTarget __instance){heal+=__instance.Value.BonusValue.Calculate(MechanicsContext.Data.Current.Context);return false;}
 static bool Apply(ContextActionApplyBuff __instance){applications.Add(__instance);return false;}
 static bool Passed(ref bool __result){__result=passed;return false;}
 static bool Save(RuleSavingThrow rule,ref RuleSavingThrow __result){saves++;Check(rule.DifficultyClassBase==23,"Original spell DC retained");__result=rule;return false;}
 static bool Add(BlueprintBuff __1,MechanicsContext __2,ref Buff __result){__result=Fact(__1,__2,rf);rf.List.Add(__result);Refresh(rf);return false;}
}
