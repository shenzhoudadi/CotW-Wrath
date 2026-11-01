using Kingmaker.EntitySystem.Stats;
using Kingmaker.Controllers.Units;
using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class PsychicConduitSpells {
  internal static readonly string[] Tokens={"LendJudgment","LendJudgmentGreater","Babble"};
  internal static readonly string[] JudgmentIds={"a8e7e315b5a241b47ad526771eee19b7","b1b7eeb49d703b047a3a534f17863001","fc246eb29c8f95c449fd156759f421e9","4d2930114daad304da816b1412c85d1d","99345ce7c0f8001438fee23db660d039","fbc7fe386bceb354494b05736381c238","e73da542e92df1843a93e29701a2e1d8","00369aa0a76141a479382e360b1f3dd7","61b9776ff6270ac48b0daffec5032052","fd66fc50ec2a65347a8fa3352aa3c581","456a099d26ef1e84c9401666870de095","f6ac26437d007544299bd113d58a3858","4594286e4a565cd41ad9475f6c903fda","2e3f01df36b508b4e9186bab7a337dfa","481b03bc6cbc5af448b1f6cb70d88859"};
  internal static readonly string[] JudgmentNames={"Destruction","Healing","Justice","Piercing","Protection","Purity","Resistance","Resiliency (magic)","Resiliency (chaotic)","Resiliency (evil)","Resiliency (good)","Resiliency (lawful)","Smiting (magic)","Smiting (adamantine)","Smiting (alignment)"};
  static ContextRankConfig Scaling(){var r=new ContextRankConfig();SetEnum(r,"m_BaseValueType","CasterLevel");return r;}
  static ContextActionApplyBuff Timed(BlueprintBuff b){var a=Apply(b,0);a.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};return a;}
  static BlueprintAbility Spell(PsychicAssets f,string token,string title,string text,UnityEngine.Sprite icon,params BlueprintComponent[] parts){var a=f.Ability(token,title,text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Touch,parts);a.CanTargetSelf=false;a.CanTargetFriends=true;a.NotOffensive=true;a.CanTargetEnemies=false;a.SpellResistance=false;a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach;if(token.StartsWith("LendJudgment"))a.ComponentsArray=a.ComponentsArray.Concat(new[]{(BlueprintComponent)new AbilitySpawnFx {PrefabLink=new PrefabLink {AssetId="c4d861e816edd6f4eab73c55a18fdadd"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget}}).ToArray();Set(a,"m_Icon",icon);a.LocalizedDuration=Text(token,"duration","1 round per caster level");a.LocalizedSavingThrow=Text(token,"savingThrow","");return a;}
  internal static void Build(PsychicAssets f){Judgments(f);Babble(f);}
  static void Judgments(PsychicAssets f){
   const string text="Lend an ally one of your currently active judgments for 1 round per caster level. The benefit uses your inquisitor progression, including archetype levels used by the native judgment. Both you and the ally benefit. Ending or switching off that judgment ends the shared benefit; switching it on again requires a new casting. Healing occurs once per round. Shared bonuses do not stack with equivalent native judgments.";
   var icon=f.Native<BlueprintAbility>("db0f61cd985ca09498cafde9a4b27a16").Icon;var choices=new BlueprintAbility[JudgmentIds.Length];var sources=new BlueprintBuffReference[choices.Length];var shared=new BlueprintBuffReference[choices.Length];
   for(int i=0;i<choices.Length;i++){
    var src=f.Native<BlueprintBuff>(JudgmentIds[i]);sources[i]=Ref<BlueprintBuffReference>(src);
    var b=f.Clone(src,"LendJudgment"+i+"Buff","Lend Judgment: "+JudgmentNames[i],text);b.IsClassFeature=false;SetEnum(b,"m_Flags","0");b.Stacking=StackingType.Replace;b.FxOnRemove=new PrefabLink();
    var rounds=b.GetComponents<AddFactContextActions>().SelectMany(c=>c.NewRound.Actions).ToArray();
    b.ComponentsArray=b.ComponentsArray.Where(c=>!(c is AddFactContextActions)&&!(c is AddOutgoingPhysicalDamageProperty p&&p.MyAlignment)).Concat(new BlueprintComponent[]{new PsychicJudgmentLink {Source=sources[i],NewRound=Actions(rounds)}}).ToArray();
    if(i==14)b.ComponentsArray=b.ComponentsArray.Concat(new[]{(BlueprintComponent)new PsychicJudgmentAlignment()}).ToArray();
    shared[i]=Ref<BlueprintBuffReference>(b);
    choices[i]=Spell(f,"LendJudgment"+i,"Lend Judgment: "+JudgmentNames[i],text,src.Icon,new SpellComponent {School=SpellSchool.Divination},Scaling(),new PsychicJudgmentAvailable {Sources=new[]{sources[i]}},Run(Timed(b)));
   }
   var variants=new AbilityVariants();Set(variants,"m_Variants",choices.Select(Ref<BlueprintAbilityReference>).ToArray());var root=Spell(f,"LendJudgment","Lend Judgment",text,icon,new SpellComponent {School=SpellSchool.Divination},variants);foreach(var a in choices)Set(a,"m_Parent",Ref<BlueprintAbilityReference>(root));
   const string greater="Lend an ally all judgments active when you cast, for 1 round per caster level. Each benefit uses your progression and ends when its source judgment ends. Judgments activated later require a new casting. Dispelling this spell removes all benefits it granted.";
   var host=f.Buff("LendJudgmentGreaterBuff","Lend Judgment, Greater",greater,Scaling(),new AddFactContextActions {Activated=Actions(new PsychicLendAll {Sources=sources,Shared=shared}),Deactivated=Actions(),NewRound=Actions()});Set(host,"m_Icon",icon);
   Spell(f,"LendJudgmentGreater","Lend Judgment, Greater",greater,icon,new SpellComponent {School=SpellSchool.Divination},Scaling(),new PsychicJudgmentAvailable {Sources=sources},Run(Timed(host)));
  }
  static void Babble(PsychicAssets f){
   const string text="One creature within medium range must succeed at a Will save or babble uncontrollably and become nauseated for 1 round per caster level. Other creatures within 30 feet that can hear it must save or become fascinated until they leave or the babbling ends. Damage ends fascination. After saving or recovering, a creature is not affected again by that instance until it leaves and re-enters. Silence suppresses babbling and fascination.";
   var icon=f.Native<BlueprintBuff>("886c7407dc629dc499b9f1465ff382df").Icon;var desc=SpellDescriptor.MindAffecting|SpellDescriptor.Compulsion;
   var fascinated=f.Clone(f.Native<BlueprintBuff>("9c70d2ae017665b4b845e6c299cb7439"),"BabbleFascinatedBuff","Fascinated",text);fascinated.Stacking=StackingType.Stack;fascinated.IsClassFeature=false;SetEnum(fascinated,"m_Flags","0");
   foreach(var trigger in fascinated.GetComponents<AddIncomingDamageTrigger>())trigger.Actions=Actions(new ContextActionRemoveSelf());
   var owned=PsychicWeatherSpells.Owned(fascinated,new PsychicBabbleSave());owned.CheckConditionEveryRound=false;
   var area=PsychicWeatherSpells.Area(f,"Babble","Babble",text,f.Native<BlueprintAbilityAreaEffect>("a4fc1c0798359974e99e1d790935501d").Fx,AreaEffectShape.Cylinder,30,false,owned,new SpellDescriptorComponent {Descriptor=desc});
   var addArea=new AddAreaEffect();Set(addArea,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));
   var host=f.Buff("BabbleBuff","Babble",text,new AddCondition {Condition=UnitCondition.Nauseated},addArea,new SpellDescriptorComponent {Descriptor=desc});Set(host,"m_Icon",icon);
   var silence=f.Find<BlueprintBuff>("SilenceBuff");var suppress=new SuppressBuffs {name="BabbleSilenceSuppression",Schools=Array.Empty<SpellSchool>()};Set(suppress,"m_ExcludedBuffs",Array.Empty<BlueprintBuffReference>());Set(suppress,"m_Buffs",new[]{Ref<BlueprintBuffReference>(host),Ref<BlueprintBuffReference>(fascinated)});silence.ComponentsArray=silence.ComponentsArray.Concat(new[]{(BlueprintComponent)suppress}).ToArray();
   ((PsychicBabbleSave)owned.Condition.Conditions[0]).Silence=Ref<BlueprintBuffReference>(silence);
   var run=Run(new ContextActionConditionalSaved {Succeed=Actions(),Failed=Actions(Timed(host))});run.SavingThrowType=SavingThrowType.Will;
   var spell=Spell(f,"Babble","Babble",text,icon,new SpellComponent {School=SpellSchool.Enchantment},new SpellDescriptorComponent {Descriptor=desc},Scaling(),run,new AbilitySpawnFx {PrefabLink=new PrefabLink {AssetId="c388856d0e8855f429a83ccba67944ba"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget});spell.Range=AbilityRange.Medium;spell.CanTargetEnemies=true;spell.NotOffensive=false;spell.SpellResistance=true;spell.EffectOnAlly=spell.EffectOnEnemy=AbilityEffectOnUnit.Harmful;spell.AvailableMetamagic=Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach|Metamagic.Persistent|Metamagic.Piercing;spell.LocalizedSavingThrow=Text("Babble","savingThrow","Will negates");
  }
 }
 [TypeId("755c31e1ff1f4404a5d2899bb8f93923")]
 public sealed class PsychicJudgmentAvailable:BlueprintComponent,IAbilityCasterRestriction {
  public BlueprintBuffReference[] Sources;
  internal static bool Active(UnitEntityData caster,BlueprintBuff source)=>caster!=null&&caster.Buffs.Enumerable.Any(b=>b.Blueprint==source&&b.IsTurnedOn&&!b.IsSuppressed);
  public bool IsCasterRestrictionPassed(UnitEntityData caster)=>Sources.Any(s=>Active(caster,s.Get()));
  public string GetAbilityCasterRestrictionUIText()=>"Activate the judgment to be shared.";
 }
 [TypeId("76eaa0ffdb814a88968db0f195e387c4")]
 public sealed class PsychicJudgmentLink:UnitBuffComponentDelegate,ITickEachRound,IUnitBuffHandler,IGlobalSubscriber {
  public BlueprintBuffReference Source;public ActionList NewRound;
  public void OnNewRound(){if(!PsychicJudgmentAvailable.Active(Context.MaybeCaster,Source.Get())){Buff.Remove();return;}using(Context.GetDataScope(Owner))NewRound.Run();}
  public void HandleBuffDidAdded(Buff b){}
  public void HandleBuffDidRemoved(Buff b){if(b.Owner==Context.MaybeCaster&&b.Blueprint==Source.Get())Buff.Remove();}
 }
 [TypeId("6520b28c634b42c08c2645b41fa5cae1")]
 public sealed class PsychicLendAll:ContextAction {
  public BlueprintBuffReference[] Sources,Shared;
  public override string GetCaption()=>"Share currently active judgments as owned child buffs";
  public override void RunAction(){for(int i=0;i<Sources.Length;i++)if(PsychicJudgmentAvailable.Active(Context.MaybeCaster,Sources[i].Get())){var apply=Apply(Shared[i].Get(),0);apply.AsChild=true;apply.SameDuration=true;apply.IsNotDispelable=true;apply.RunAction();}}
 }
 [TypeId("caed6023f6934c2a8794b01a99e94468")]
 public sealed class PsychicJudgmentAlignment:UnitBuffComponentDelegate,IInitiatorRulebookHandler<RulePrepareDamage>,ISubscriber {
  public void OnEventAboutToTrigger(RulePrepareDamage e){if(e.DamageBundle.Weapon==null||Context.MaybeCaster==null)return;var a=Context.MaybeCaster.Descriptor.Alignment.ValueRaw;var d=e.DamageBundle.WeaponDamage;if(d==null)return;foreach(var pair in new[]{Tuple.Create(AlignmentComponent.Good,DamageAlignment.Good),Tuple.Create(AlignmentComponent.Evil,DamageAlignment.Evil),Tuple.Create(AlignmentComponent.Lawful,DamageAlignment.Lawful),Tuple.Create(AlignmentComponent.Chaotic,DamageAlignment.Chaotic)})if(a.HasComponent(pair.Item1))d.AddAlignment(pair.Item2);}
  public void OnEventDidTrigger(RulePrepareDamage e){}
 }
 [TypeId("efddf246178f453e8bb92f8dfd2e092d")]
 public sealed class PsychicBabbleSave:ContextCondition {
  public BlueprintBuffReference Silence;
  protected override string GetConditionCaption()=>"A hearing listener fails the babbling Will save";
  protected override bool CheckCondition(){var target=Target.Unit;var host=Context.MainTarget?.Unit;if(target==null||target==host||PsychicJudgmentAvailable.Active(target,Silence.Get()))return false;bool Pass()=>Context.TriggerRule(new RuleSavingThrow(target,SavingThrowType.Will,Context.Params.DC){PersistentSpell=Context.HasMetamagic(Metamagic.Persistent)}).IsPassed;if(!Pass())return true;return Context.HasMetamagic(Metamagic.Persistent)&&!Pass();}
 }
}
