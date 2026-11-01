using Kingmaker.Designers.Mechanics.Facts;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.Controllers;
using Kingmaker.AI.Blueprints;
using Kingmaker.Controllers.Units;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.View;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using UnityEngine;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class PsychicSpiritWeapons {
  internal static readonly string[] Tokens={"SpiritualWeapon","SpiritualAlly","MagesSword","TwilightKnife","ArcaneEye"};
  internal static void Build(PsychicAssets f){
   var donor=f.Native<BlueprintUnit>("34df84245e875364b9a8832256bc349f");var ghost=f.Native<BlueprintUnit>("655ac57b330918c4aadc78a00fb2ccaf");var human=f.Native<BlueprintUnit>("e9c2bef81c166654088e27dea05e4fbd");var wisp=f.Native<BlueprintUnit>("24719a49b84c5cd43b894268d22d9c89");
   var blankBrain=f.Clone(f.Native<BlueprintBrain>("5abc8884c6f15204c8604cb01a2efbab"),"SpiritualWeaponBrain","","Only the spell controller gives attack orders.");Set(blankBrain,"m_Actions",Array.Empty<BlueprintAiActionReference>());
   for(int kind=0;kind<5;kind++){
    string token=Tokens[kind],title=new[]{"Spiritual Weapon","Spiritual Ally","Mage's Sword","Twilight Knife","Arcane Eye"}[kind];bool eye=kind==4;
    var icon=kind==0?PsychicOriginalIcons.Load("SpiritualWeapon"):kind==1?PsychicOriginalIcons.Load("SpiritualAlly"):kind==2?PsychicOriginalIcons.Load("MagesSword"):kind==3?PsychicOriginalIcons.Load("TwilightKnife"):f.Native<BlueprintAbility>("41cf93453b027b94886901dbfc680cb9").Icon;
    string desc=eye?"Create an invisible, directly controllable floating sensor for 1 minute per caster level. It moves at speed 30 and reveals the area around it along walkable paths. Use a party member for object and door interactions. Select the eye alone to move it. It cannot attack or cast spells. Dismissing the spell or replacing your eye removes the sensor and its vision. The sensor follows the game's walkable navigation, not unrestricted three-dimensional flight.":
     $"Create {title.ToLowerInvariant()} for 1 round per caster level. It attacks your designated enemy and can be redirected with a move action. Casting allows one attack; redirecting after an attack changes the next attack without granting an extra attack that round. Later rounds allow normal iterative attacks (Mage's Sword and Twilight Knife attack once). Attacks use your base attack bonus and highest mental ability modifier; Mage's Sword instead uses caster level plus that modifier +3. Force damage ignores physical damage reduction and incorporeal resistance. A failed spell resistance check ends this casting. You may dismiss it as a free action. Only one of each kind can be maintained by a caster. "+
     (kind==2?"Mage's Sword deals 4d6+3 force damage.":kind==3?"Twilight Knife deals 1d4 force damage plus 1d6 sneak attack per 4 caster levels when the normal sneak attack conditions apply. It automatically follows the target of your weapon attacks, at most once per round.":"Each hit deals 1d8 force damage +1 per 3 caster levels (maximum +5).")+
     (kind==1?" Spiritual Ally threatens adjacent enemies and can flank and make attacks of opportunity, using the normal per-round opportunity limit.":"")+(kind<2?" Choose Chaos (battleaxe), Evil (light flail), Good (warhammer), Law (longsword), or your deity's favored weapon. Critical threat and multiplier follow that weapon. These choices are not restricted by your alignment. If your deity has multiple favored weapons, its first supported native category is used.":kind==3?" The floating weapon is a dagger.":" The floating weapon is a longsword.")+" Retargeting moves it to the target's vicinity. The spell controller schedules attacks at six-second round boundaries; enemy movement does not consume additional caster actions.";
    var unit=f.Clone(eye?wisp:kind==1?ghost:donor,token+"Unit",title,desc);if(!eye){unit.Prefab=CopyData(human.Prefab);unit.Visual=CopyData(human.Visual);Set(unit,"m_Race",CopyData(Get(human,"m_Race")));unit.Gender=human.Gender;}unit.ComponentsArray=unit.ComponentsArray.Where(c=>c is AddClassLevels).ToArray();foreach(var levels in unit.ComponentsArray.OfType<AddClassLevels>()){levels.Levels=1;var choices=(Array)Get(levels,"Selections");if(choices!=null)Set(levels,"Selections",Array.CreateInstance(choices.GetType().GetElementType(),0));}unit.AlternativeBrains=Array.Empty<BlueprintBrainReference>();unit.Strength=unit.Dexterity=unit.Constitution=unit.Intelligence=unit.Wisdom=unit.Charisma=10;unit.Speed=new Feet(30);Set(unit,"m_Brain",Ref<BlueprintBrainReference>(blankBrain));unit.Alignment=Alignment.TrueNeutral;
    var feature=f.Feature(token+"CreatureFeature",title,desc,new Untargetable(),new AddCondition{Condition=UnitCondition.Unlootable},new CompleteDamageImmunity(),new AddCondition{Condition=UnitCondition.ImmuneToAttackOfOpportunity},new AddCondition{Condition=UnitCondition.DisableAttacksOfOpportunity},new AddProficiencies{WeaponProficiencies=SpiritWeaponForms.NativeItems.Keys.ToArray()});Set(feature,"m_Icon",icon);
    if(eye)feature.ComponentsArray=feature.ComponentsArray.Concat(new BlueprintComponent[]{new AddCondition{Condition=UnitCondition.Invisible},new AddCondition{Condition=UnitCondition.CanNotAttack},new PsychicEyeVision()}).ToArray();
    else feature.ComponentsArray=feature.ComponentsArray.Concat(new BlueprintComponent[]{new AddCondition{Condition=UnitCondition.CantMove}}).ToArray();
    if(kind==1)feature.ComponentsArray=feature.ComponentsArray.Where(c=>!(c is AddCondition condition&&condition.Condition==UnitCondition.DisableAttacksOfOpportunity)).Concat(new BlueprintComponent[]{Facts(f.Native<BlueprintFeature>("7812ad3672a4b9a4fb894ea402095167"))}).ToArray();
    feature.ComponentsArray=feature.ComponentsArray.Concat(PsychicTravelSpells.Ground()).ToArray();Set(unit,"m_AddFacts",new[]{Ref<BlueprintUnitFactReference>(feature)});
    // Clone Body separately; no native donor slot or weapon is edited.
    var body=(BlueprintUnit.UnitBody)HarmonyLib.AccessTools.Method(typeof(object),"MemberwiseClone").Invoke(unit.Body,null);unit.Body=body;body.DisableHands=eye;body.ActiveHandSet=0;Set(body,"m_AdditionalLimbs",Array.Empty<BlueprintItemWeaponReference>());Set(body,"m_AdditionalSecondaryLimbs",Array.Empty<BlueprintItemWeaponReference>());
    if(!eye){BlueprintItemWeapon weapon=null;foreach(var category in kind<2?SpiritWeaponForms.NativeItems.Keys.ToArray():new[]{kind==3?WeaponCategory.Dagger:WeaponCategory.Longsword}){var built=SpiritWeaponForms.Build(f,kind,category,title,desc);if(category==(kind==3?WeaponCategory.Dagger:WeaponCategory.Longsword))weapon=built;}Set(body,"m_PrimaryHand",Ref<BlueprintItemEquipmentHandReference>(weapon));}
    else Set(body,"m_PrimaryHand",new BlueprintItemEquipmentHandReference());
    foreach(string slot in new[]{"m_SecondaryHand","m_PrimaryHandAlternative1","m_SecondaryHandAlternative1","m_PrimaryHandAlternative2","m_SecondaryHandAlternative2","m_PrimaryHandAlternative3","m_SecondaryHandAlternative3"})Set(body,slot,new BlueprintItemEquipmentHandReference());
    var host=f.Buff(token+"Controller",title,desc,new PsychicSpiritController{Kind=kind});Set(host,"m_Icon",icon);
    var move=f.Ability(token+"Direct",title+": Direct",desc,AbilityType.Supernatural,UnitCommand.CommandType.Move,AbilityRange.Medium,new PsychicSpiritReady{Controller=Ref<BlueprintBuffReference>(host)},Run(new PsychicSpiritRedirect{Controller=Ref<BlueprintBuffReference>(host)}));move.CanTargetSelf=move.CanTargetFriends=false;move.CanTargetEnemies=true;Set(move,"m_Icon",icon);move.LocalizedDuration=Text(token+"Direct","duration","Remaining spell duration");move.LocalizedSavingThrow=Text(token+"Direct","savingThrow","");
    var end=new ContextActionRemoveBuff{ToCaster=true};Set(end,"m_Buff",Ref<BlueprintBuffReference>(host));var dismiss=f.Ability(token+"Dismiss",title+": Dismiss",desc,AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Personal,Run(end));Set(dismiss,"m_Icon",icon);dismiss.LocalizedDuration=Text(token+"Dismiss","duration","Instantaneous");dismiss.LocalizedSavingThrow=Text(token+"Dismiss","savingThrow","");
    host.ComponentsArray=host.ComponentsArray.Concat(new[]{(BlueprintComponent)(eye?Facts(dismiss):Facts(move,dismiss))}).ToArray();
    var apply=Apply(host,0,eye?DurationRate.Minutes:DurationRate.Rounds,true);apply.DurationValue.BonusValue=new ContextValue{ValueType=ContextValueType.Rank};
    var actor=f.Buff(token+"ActorBuff",title,desc,new PsychicSpiritAttackStats{Kind=kind});Set(actor,"m_Icon",icon);
    // Native blue Brilliant Energy visual only: no weapon-enchantment mechanics.
    // Buff owns its FX across view attach, reload and destruction.
    if(kind<=3)actor.FxOnStart=new PrefabLink{AssetId="fdc7f8f37d3f8da42be2a1d35a617001"};
    if(kind<=3)actor.ComponentsArray=actor.ComponentsArray.Concat(new BlueprintComponent[]{new SpiritWeaponBlueVisual(),new SpiritWeaponAnimatedVisual{Kind=kind}}).ToArray();
    var applyActor=Apply(actor,0);applyActor.Permanent=true;applyActor.IsNotDispelable=true;
    var spawn=new ContextActionSpawnMonster{DurationValue=new ContextDurationValue{Rate=eye?DurationRate.Minutes:DurationRate.Rounds,DiceType=DiceType.Zero,DiceCountValue=0,BonusValue=new ContextValue{ValueType=ContextValueType.Rank}},CountValue=new ContextDiceValue{DiceType=DiceType.Zero,DiceCountValue=0,BonusValue=1},LevelValue=1,IsDirectlyControllable=eye,AfterSpawn=Actions(applyActor,new PsychicSpiritBind{Controller=Ref<BlueprintBuffReference>(host),Kind=kind}),DoNotLinkToCaster=false};Set(spawn,"m_Blueprint",Ref<BlueprintUnitReference>(unit));Set(spawn,"m_SummonPool",new BlueprintSummonPoolReference());
    var spell=f.Ability(token,title,desc,AbilityType.Spell,UnitCommand.CommandType.Standard,eye?AbilityRange.Close:AbilityRange.Medium,new SpellComponent{School=eye?SpellSchool.Divination:SpellSchool.Evocation},PsychicLinkedWard.CL(),new AbilitySpawnFx{PrefabLink=new PrefabLink{AssetId="8de64fbe047abc243a9b4715f643739f"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget},Run(apply,spawn));Set(spell,"m_Icon",icon);spell.CanTargetFriends=spell.CanTargetSelf=false;spell.CanTargetEnemies=!eye;spell.CanTargetPoint=eye;spell.SpellResistance=!eye;spell.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach;spell.LocalizedDuration=Text(token,"duration",eye?"1 minute per caster level":"1 round per caster level");spell.LocalizedSavingThrow=Text(token,"savingThrow","");if(!eye)spell.ComponentsArray=spell.ComponentsArray.Concat(new[]{(BlueprintComponent)new SpellDescriptorComponent{Descriptor=SpellDescriptor.Force}}).ToArray();
    if(kind<2){
     var children=new List<BlueprintAbility>();string[] choices={"Chaos (Battleaxe)","Evil (Light Flail)","Good (Warhammer)","Law (Longsword)","Deity's Favored Weapon"};
     for(int choice=0;choice<5;choice++){
      var child=f.Clone(spell,token+"Form"+choice,title+": "+choices[choice],desc);Set(child,"m_Parent",Ref<BlueprintAbilityReference>(spell));child.LocalizedDuration=spell.LocalizedDuration;child.LocalizedSavingThrow=spell.LocalizedSavingThrow;
      var binding=child.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionSpawnMonster>().Single().AfterSpawn.Actions.OfType<PsychicSpiritBind>().Single();binding.Choice=choice;
      if(choice==4)child.ComponentsArray=child.ComponentsArray.Concat(new BlueprintComponent[]{new SpiritFavoredWeaponRestriction()}).ToArray();children.Add(child);
     }
     var forms=new AbilityVariants();Set(forms,"m_Variants",children.Select(Ref<BlueprintAbilityReference>).ToArray());
     spell.ComponentsArray=spell.ComponentsArray.Where(c=>!(c is AbilityEffectRunAction)).Concat(new BlueprintComponent[]{forms}).ToArray();
    }
   }
   foreach(var item in f.Assets.Where(x=>Tokens.Any(t=>x.Key.StartsWith(t))))PsychicRepairComponents.UniqueNames(item.Value);
  }
 }
 public sealed class PsychicSpiritState {
  [JsonProperty] public UnitEntityData Summon,Target;
  [JsonProperty] public TimeSpan NextAttack;
  [JsonProperty] public bool SingleNext;
  [JsonProperty] public int Bab,Mental,Level;
  [JsonProperty] public HashSet<string> ResistancePassed=new HashSet<string>();
 }
 [TypeId("92f164cf78ed40c78a19ff28ffb50eb9")]
 public sealed class PsychicSpiritController:UnitBuffComponentDelegate<PsychicSpiritState>,ITickEachRound,IInitiatorRulebookHandler<RuleAttackRoll>,ISubscriber {
  public int Kind;
  internal static Buff Find(UnitEntityData caster,BlueprintBuff blueprint)=>caster?.Buffs?.Enumerable.FirstOrDefault(b=>b.Blueprint==blueprint&&b.IsTurnedOn&&!b.IsSuppressed);
  internal static PsychicSpiritState State(Buff b)=>b.Components.Single(c=>c.SourceBlueprintComponent is PsychicSpiritController).GetData<PsychicSpiritState>();
  internal bool Ready=>Game.Instance.TimeController.GameTime>=Data.NextAttack;
  public void OnNewRound(){if(Data.Summon==null||Data.Summon.IsDisposed){Buff.Remove();return;}if(Kind!=4&&Kind!=3&&Ready)Attack(Data.SingleNext);}
  internal void Attack(bool single){var target=Data.Target;var u=Data.Summon;if(u==null||target==null||target.State.IsDead||!target.IsEnemy(Owner)||!Ready)return;
   if(!InRange(Owner,target,Data.Level))return;
   if(!Data.ResistancePassed.Contains(target.UniqueId)){if(Context.TriggerRule(new RuleSpellResistanceCheck(Context,target)).IsSpellResisted){Buff.Remove();return;}Data.ResistancePassed.Add(target.UniqueId);}
   Data.NextAttack=Game.Instance.TimeController.GameTime+TimeSpan.FromSeconds(6);Data.SingleNext=false;
   PlaceNear(u,Owner,target);
   int count=single||Kind>=2?1:Math.Max(1,1+(Data.Bab-1)/5);
   for(int i=0;i<count&&!target.State.IsDead;i++)Strike(u,target,i,count);
  }
  internal static bool InRange(UnitEntityData owner,UnitEntityData target,int level)=>(target.Position-owner.Position).sqrMagnitude<=Math.Pow((100+10*level)*0.3048,2);
  internal static void PlaceNear(UnitEntityData unit,UnitEntityData owner,UnitEntityData target){var direction=(owner.Position-target.Position).normalized;if(direction.sqrMagnitude<0.01f)direction=Vector3.forward;unit.Position=target.Position+direction*1.5f;}
  internal void Redirect(UnitEntityData target){Data.Target=target;Data.SingleNext=true;if(Ready&&Kind!=3)Attack(true);}
  internal void Strike(UnitEntityData u,UnitEntityData target,int index,int count){SpiritWeaponAnimatedVisual.Swing(u,target);Context.TriggerRule(new RuleAttackWithWeapon(u,target,u.Body.PrimaryHand.MaybeWeapon,5*index){Reason=new RuleReason(Context),SkipAnimation=false,AttackNumber=index,AttacksCount=count});}
  protected override void OnDeactivate(){var u=Data.Summon;Data.Summon=null;Data.Target=null;if(u!=null&&!u.IsDisposed)u.MarkForDestroy();}
  public void OnEventAboutToTrigger(RuleAttackRoll e){}
  public void OnEventDidTrigger(RuleAttackRoll e){if(Kind!=3||e.IsFake||e.Target==null||!e.Target.IsEnemy(Owner))return;Data.Target=e.Target;if(Ready)Attack(true);}
 }
 [TypeId("a7deac2c959948519ec428ee80b658bf")]
 public sealed class PsychicSpiritBind:ContextAction {
  public BlueprintBuffReference Controller;public int Kind;public int Choice=4;
  public override string GetCaption()=>"Bind summoned actor to its caster and spell lifetime";
  public override void RunAction(){var owner=Context.MaybeCaster;var unit=Target.Unit;var host=PsychicSpiritController.Find(owner,Controller.Get());if(host==null||unit==null){unit?.MarkForDestroy();return;}var data=PsychicSpiritController.State(host);data.Summon=unit;data.Target=Context.MainTarget.Unit;data.Level=Context.Params.CasterLevel;data.Bab=SpiritualGuardian01433.Applies(owner,Kind)?data.Level:owner.Stats.BaseAttackBonus.ModifiedValue;data.Mental=new[]{owner.Stats.Intelligence.Bonus,owner.Stats.Wisdom.Bonus,owner.Stats.Charisma.Bonus}.Max();
   if(Kind<2){var category=SpiritWeaponForms.Category(owner,Choice);if(!category.HasValue||!SpiritWeaponForms.Equip(unit,Kingmaker.Blueprints.ResourcesLibrary.TryGetBlueprint<BlueprintItemWeapon>(Id(SpiritWeaponForms.ItemToken(Kind,category.Value))))){unit.MarkForDestroy();host.Remove();return;}}
   unit.Descriptor.CustomName=Controller.Get().Name;unit.Stats.BaseAttackBonus.BaseValue=Kind==2?data.Level:data.Bab;unit.Stats.AdditionalAttackBonus.BaseValue=0;if(Kind==3)unit.Stats.SneakAttack.BaseValue=data.Level/4;
   if(Kind==1)unit.Buffs.Enumerable.FirstOrDefault(b=>b.Blueprint.AssetGuid==Id("SpiritualAllyActorBuff"))?.CallComponents<PsychicSpiritAttackStats>(c=>c.RefreshEngagement());
   if(Kind!=4&&Kind!=3){if(data.Target!=null)data.ResistancePassed.Add(data.Target.UniqueId);var rt=host.Components.Single(c=>c.SourceBlueprintComponent is PsychicSpiritController);using(rt.RequestEventContext())((PsychicSpiritController)rt.SourceBlueprintComponent).Attack(true);}
  }
 }
 [TypeId("ebba5b1ba6da45beaa493cb0dc46b610")]
 public sealed class PsychicSpiritRedirect:ContextAction {
  public BlueprintBuffReference Controller;public override string GetCaption()=>"Redirect the existing spiritual weapon";
  public override void RunAction(){var host=PsychicSpiritController.Find(Context.MaybeCaster,Controller.Get());if(host==null)return;var rt=host.Components.Single(c=>c.SourceBlueprintComponent is PsychicSpiritController);using(rt.RequestEventContext()){var comp=(PsychicSpiritController)rt.SourceBlueprintComponent;comp.Redirect(Target.Unit);}}
 }
 [TypeId("c393a3518bea45a894bf1ac7cf0604a4")]
 public sealed class PsychicSpiritReady:BlueprintComponent,IAbilityCasterRestriction {
  public BlueprintBuffReference Controller;
  public bool IsCasterRestrictionPassed(UnitEntityData caster){var host=PsychicSpiritController.Find(caster,Controller.Get());return host!=null&&PsychicSpiritController.State(host).Summon!=null;}
  public string GetAbilityCasterRestrictionUIText()=>"The summoned spirit is no longer present.";
 }
 [TypeId("a0ffcb3c04a94744b979578a0203f6bb")]
 public sealed class PsychicSpiritAttackStats:UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCalculateWeaponStats>,IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,IInitiatorRulebookHandler<RuleAttackRoll>,IEntityPositionChangedHandler,ITickEachRound,IGlobalSubscriber {
  public int Kind;
  // These actors attack without UnitAttack commands and have an empty AI.
  // Explicitly enter combat and seed native engagements; leave all range,
  // concealment, disengagement attacks and per-round AoO spending to the engine.
  internal void RefreshEngagement(){
   if(Kind!=1||BoundAlly(Owner)==null||Owner.View is null||!Owner.State.CanAct||Owner.Passive)return;
   var caster=Context?.MaybeCaster;if(caster?.IsInCombat!=true)return;
   var hand=Owner.GetThreatHand();var range=hand==null?(float?)null:Owner.GetThreatRange(hand);if(!range.HasValue)return;
   Owner.CombatState.JoinCombat(true);
   foreach(var enemy in Kingmaker.Designers.GameHelper.GetTargetsAround(Owner.Position,range.Value+10f,false,false))
    if(enemy!=null&&enemy!=Owner&&!(enemy.View is null)&&enemy.IsEnemy(Owner)&&enemy.State.IsConscious&&UnitEngagementExtension.IsEngage(Owner,enemy,true,range.Value))Owner.CombatState.Engage(enemy);
  }
  public void HandleEntityPositionChanged(EntityDataBase unit)=>RefreshEngagement();
  public void OnNewRound()=>RefreshEngagement();
  protected override void OnApplyPostLoadFixes()=>RefreshEngagement();
  protected override void OnViewDidAttach()=>RefreshEngagement();
  internal static Buff BoundAlly(UnitEntityData unit){
   if(unit?.Buffs==null||unit.Blueprint.AssetGuid!=Id("SpiritualAllyUnit"))return null;
   var actor=unit.Buffs.Enumerable.FirstOrDefault(b=>b.Blueprint.AssetGuid==Id("SpiritualAllyActorBuff")&&b.IsTurnedOn&&!b.IsSuppressed);
   var caster=actor?.Context?.MaybeCaster;
   var host=caster?.Buffs?.Enumerable.FirstOrDefault(b=>b.Blueprint.AssetGuid==Id("SpiritualAllyController")&&b.IsTurnedOn&&!b.IsSuppressed);
   return host!=null&&ReferenceEquals(PsychicSpiritController.State(host).Summon,unit)?host:null;
  }
  public void OnEventAboutToTrigger(RuleAttackRoll e){
   if(Kind!=1||e.IsFake||e.RuleAttackWithWeapon?.IsAttackOfOpportunity!=true)return;
   var host=BoundAlly(Owner);if(host==null){e.AutoMiss=true;return;}
   // Native opportunity attacks bypass the controller's scheduled Strike.
   // Keep their independent per-round budget, but share this casting's SR cache.
   var data=PsychicSpiritController.State(host);e.Reason=new RuleReason(host.Context);
   if(!data.ResistancePassed.Contains(e.Target.UniqueId)){
    if(host.Context.TriggerRule(new RuleSpellResistanceCheck(host.Context,e.Target)).IsSpellResisted){e.AutoMiss=true;host.Remove();return;}
    data.ResistancePassed.Add(e.Target.UniqueId);
   }
  }
  public void OnEventDidTrigger(RuleAttackRoll e){if(Kind==1&&!e.IsFake)RefreshEngagement();}
  public void OnEventAboutToTrigger(RuleCalculateWeaponStats e){
   if(Kind==4)return;
   // OverrideDamageBonus is an ability-score MULTIPLIER in Wrath, not damage.
   // Remove ability damage (including negative Strength) and add a real source.
   Set(e,"<DamageBonusStat>k__BackingField",null);
   e.AddDamageModifier((Kind==2?3:Kind==3?0:Math.Min(5,(Context?.Params?.CasterLevel??0)/3))+(SpiritualGuardian01433.Applies(Context?.MaybeCaster,Kind)?2:0),Fact,ModifierDescriptor.UntypedStackable);
  }
  public void OnEventDidTrigger(RuleCalculateWeaponStats e){}
  public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget e){
   if(Kind==4)return;
   var caster=Context?.MaybeCaster;
   var host=caster?.Buffs?.Enumerable.FirstOrDefault(b=>b.Blueprint.AssetGuid==Id(PsychicSpiritWeapons.Tokens[Kind]+"Controller")&&b.IsTurnedOn&&!b.IsSuppressed);
   if(host==null)return;
   var data=PsychicSpiritController.State(host);
   if(!ReferenceEquals(data.Summon,Owner))return;
   // Existing saves may still contain the former anonymous flat bonus.
   Owner.Stats.AdditionalAttackBonus.BaseValue=0;
   e.AddModifier(data.Mental,Fact,ModifierDescriptor.UntypedStackable);
   if(Kind==2)e.AddModifier(3,Fact,ModifierDescriptor.Enhancement);
  }
  public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget e){}
 }
 // In real time the engine only counts engaged units with queued commands.
 // This spell attacks through its controller, so count its live, native melee
 // engagements too. Native range/LOS/engagement and flanking immunity still apply.
 [HarmonyPatch(typeof(UnitCombatState),"get_IsFlanked")]
 internal static class PsychicSpiritualAllyFlankingPatch {
  static void Postfix(UnitCombatState __instance,ref bool __result){
   if(__result||__instance.Unit.State.Features.CannotBeFlanked||!__instance.Unit.IsInCombat)return;
   bool ally=false;int count=0;
   foreach(var unit in __instance.EngagedBy){
    bool spiritual=unit.State.CanAct&&!unit.Passive&&PsychicSpiritAttackStats.BoundAlly(unit)!=null;
    ally|=spiritual;
    if(spiritual||unit.Commands.AnyCommandTargets(__instance.Unit))count++;
   }
   if(ally&&count>1)__result=true;
  }
 }
 // Read the resolved rule snapshot, so a later stat change, expired summon or
 // reopened combat log cannot change the explanation of a previous attack.
 [HarmonyPatch(typeof(AttackLogMessage),nameof(AttackLogMessage.AppendAttackBonusBreakdown))]
 internal static class PsychicSpiritAttackLogPatch {
  internal static string Details(RuleCalculateAttackBonus rule){
   if(rule?.Weapon==null)return null;
   int kind=SpiritWeaponForms.Kind(rule.Weapon.Blueprint);
   if(kind<0||kind==4)return null;
   var bonuses=rule.AllBonuses.ToArray();
   int basis=bonuses.Where(b=>b.Stat==StatType.BaseAttackBonus).Sum(b=>b.Value)+rule.AttackBonusPenalty;
   int mental=bonuses.Where(b=>b.Fact?.Blueprint?.AssetGuid==Id(PsychicSpiritWeapons.Tokens[kind]+"ActorBuff")&&b.Descriptor==ModifierDescriptor.UntypedStackable).Sum(b=>b.Value);
   int enhancement=bonuses.Where(b=>b.Fact?.Blueprint?.AssetGuid==Id(PsychicSpiritWeapons.Tokens[kind]+"ActorBuff")&&b.Descriptor==ModifierDescriptor.Enhancement).Sum(b=>b.Value);
   int other=rule.Result-basis-mental-enhancement+rule.AttackBonusPenalty;
   string Signed(int value)=>value>=0?"+"+value:value.ToString();
   return "Spell attack calculation\n"+(kind==2?"Caster level: ":"Spell base attack bonus: ")+Signed(basis)+"\nHighest mental ability modifier: "+Signed(mental)+(kind==2?"\nMage's Sword enhancement: "+Signed(enhancement):"")+"\nIterative attack: "+Signed(-rule.AttackBonusPenalty)+(other!=0?"\nOther attack modifiers: "+Signed(other):"")+"\nTotal: "+Signed(rule.Result);
  }
  static void Postfix(StringBuilder sb,RuleCalculateAttackBonus rule){var details=Details(rule);if(details!=null)sb.AppendLine().AppendLine(details);}
 }
 public sealed class PsychicEyeViewState {[JsonIgnore] public Transform Revealer;}
 [TypeId("6cc2c5c2960d4d6faf94c37688f7e484")]
 public sealed class PsychicEyeVision:UnitFactComponentDelegate<PsychicEyeViewState> {
  void Add(){var view=Owner.View;if(view==null||Data.Revealer==view.transform)return;Remove();Data.Revealer=view.transform;FogOfWarController.AddRevealer(Data.Revealer);}
  void Remove(){if(Data.Revealer!=null)FogOfWarController.RemoveRevealer(Data.Revealer);Data.Revealer=null;}
  protected override void OnTurnOn()=>Add();protected override void OnTurnOff()=>Remove();protected override void OnViewDidAttach()=>Add();protected override void OnViewWillDetach()=>Remove();
 }
 // The native SpawnMonster action ignores IsDirectlyControllable. NPC summon
 // blueprints fail the native companion/faction gate, even with that flag set.
 [HarmonyPatch(typeof(UnitEntityData),"get_IsDirectlyControllable")]
 internal static class PsychicEyeControlPatch {
  internal static bool OwnSensor(UnitEntityData unit){
   if(unit?.Buffs==null||unit.Blueprint.AssetGuid!=Id("ArcaneEyeUnit"))return false;
   var actor=unit.Buffs.Enumerable.FirstOrDefault(b=>b.IsTurnedOn&&!b.IsSuppressed&&b.GetComponent<PsychicSpiritAttackStats>()?.Kind==4);
   if(actor==null)return false;
   var caster=actor.Context?.MaybeCaster;
   var host=caster?.Buffs?.Enumerable.FirstOrDefault(b=>b.Blueprint.AssetGuid==Id("ArcaneEyeController")&&b.IsTurnedOn&&!b.IsSuppressed);
   return caster?.IsPlayerFaction==true&&host!=null&&ReferenceEquals(PsychicSpiritController.State(host).Summon,unit);
  }
  static void Postfix(UnitEntityData __instance,ref bool __result){
   if(__result||!__instance.IsInGame||__instance.IsDisposed||__instance.State.IsDead||__instance.PreventDirectControl||__instance.State.IsPanicked||__instance.IsDetached)return;
   if(OwnSensor(__instance))__result=true;
  }
 }
 // Untargetable is required for combat, but native click/hover also rejects it.
 // Permit only the caster's currently bound, controllable eye through that UI
 // gate; spell targeting and other summons keep their original restrictions.
 [HarmonyPatch(typeof(ClickUnitHandler),nameof(ClickUnitHandler.GetPriority))]
 internal static class PsychicEyeClickPatch {
  static void Postfix(GameObject gameObject,ref float __result){
   if(__result>0||!gameObject)return;
   var unit=gameObject.GetComponent<UnitEntityView>()?.EntityData;
   if(unit!=null&&unit.IsVisibleForPlayer&&unit.IsDirectlyControllable&&PsychicEyeControlPatch.OwnSensor(unit))__result=2f;
  }
 }
 [HarmonyPatch(typeof(UnitEntityView),nameof(UnitEntityView.HandleHoverChange))]
 internal static class PsychicEyeHoverPatch {
  static void Postfix(UnitEntityView __instance,bool isHover){
   var unit=__instance.EntityData;
   if(unit!=null&&unit.IsVisibleForPlayer&&unit.IsDirectlyControllable&&PsychicEyeControlPatch.OwnSensor(unit))__instance.MouseHighlighted=isHover;
  }
 }
}
