using System;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Buffs.Components;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class PsychicStorageSpells {
  internal static readonly string[] Tokens={"Contingency","Spite","DelayedConsumption","EmblemOfGreed"};
  internal static void Build(PsychicAssets f){
   string[] names={"Contingency","Spite","Delayed Consumption"};
   string[] texts={"Store one self-targeted non-alchemist spell up to one-third this spell's caster level (maximum 6th). Release on yourself as a free action. Duration: 1 day per caster level.","Store one harmful touch spell of 4th level or lower with a standard action or faster casting time. The next successful melee hit or combat maneuver against you releases it on the attacker, automatically hitting but retaining saving throws and spell resistance. An invalid target consumes the stored spell. Duration: 1 hour per caster level.","Store one self-targeted alchemist extract of 4th level or lower. Release on yourself as a swift action. Duration: 1 day per caster level."};
   for(int k=0;k<3;k++){
    var token=Tokens[k];var text=texts[k]+StorageHelp;var icon=k==0?f.Native<BlueprintFeature>("c46512b796216b64899f26301241e4e6").Icon:k==1?f.Native<BlueprintFeature>("fb343ede45ca1a84496c91c190a847ff").Icon:f.Native<BlueprintAbility>("07b608fab304f894880898dc0764e6e5").Icon;
    var buff=f.Buff(token+"Buff",names[k],text,new PsychicStoredSpell{Kind=k});Set(buff,"m_Icon",icon);Buttons(f,token,names[k],text,buff,k);
    var apply=Apply(buff,0,k==1?DurationRate.Hours:DurationRate.Days);apply.DurationValue.BonusValue=new ContextValue{ValueType=ContextValueType.Rank};
    var a=PsychicLinkedWard.Spell(f,token,names[k],text,k==2?SpellSchool.Transmutation:k==1?SpellSchool.Abjuration:SpellSchool.Evocation,f.Native<BlueprintAbility>("e84fc922ccf952943b5240293669b171"),Run(apply));a.Range=AbilityRange.Personal;a.CanTargetSelf=true;a.CanTargetFriends=a.CanTargetEnemies=false;a.LocalizedDuration=Text(token,"duration",k==1?"1 hour per caster level or until discharged":"1 day per caster level or until discharged");Set(a,"m_Icon",icon);
   }
  }
  internal const string StorageHelp=" Click Store Next, then cast a qualifying companion spell normally from your spellbook. Its slot, materials and casting action are consumed now; its effects are stored instead of applied. Nonqualifying spells cast normally and leave storage armed. Only one storage can be armed per character. Store Next toggles arming off if clicked again. Stored spells retain their original caster, caster level, DC and metamagic across saves. Recasting or dismissing the ward discards its stored spell. Area, multi-target and summon spells cannot be stored. For harmful touch spells, select a legal target as usual; the casting is intercepted before the touch charge is granted. Companion spells with custom external-mod delivery may require separate support.";
  internal static void Buttons(PsychicAssets f,string token,string title,string text,BlueprintBuff buff,int kind){
   var arm=f.Ability(token+"StoreNext",title+": Store Next",text,AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Personal,new PsychicStorageReady{Host=Ref<BlueprintBuffReference>(buff),Mode=0},Run(new PsychicStorageAction{Host=Ref<BlueprintBuffReference>(buff),Arm=true}));
   var release=f.Ability(token+"Release",title+": Release",text,AbilityType.Supernatural,kind==2?UnitCommand.CommandType.Swift:UnitCommand.CommandType.Free,AbilityRange.Personal,new PsychicStorageReady{Host=Ref<BlueprintBuffReference>(buff),Mode=1},Run(new PsychicStorageAction{Host=Ref<BlueprintBuffReference>(buff)}));
   var end=new ContextActionRemoveBuff{ToCaster=true};Set(end,"m_Buff",Ref<BlueprintBuffReference>(buff));var dismiss=f.Ability(token+"Dismiss",title+": Dismiss",text,AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Personal,Run(end));
   foreach(var a in new[]{arm,release,dismiss}){Set(a,"m_Icon",buff.Icon);a.LocalizedDuration=Text(a.name,"duration","Instantaneous");a.LocalizedSavingThrow=Text(a.name,"savingThrow","");}
   foreach(var a in new[]{arm,release,dismiss}){a.NotOffensive=true;a.SpellResistance=false;a.AvailableMetamagic=(Metamagic)0;}
   buff.ComponentsArray=buff.ComponentsArray.Concat(new BlueprintComponent[]{kind==1||kind==3?Facts(arm,dismiss):Facts(arm,release,dismiss)}).ToArray();
  }
 }
 public sealed class PsychicStoredSpellData {
  [JsonProperty] public bool Armed;
  [JsonProperty] public AbilityData Spell;
  [JsonProperty] public AbilityParams Parameters;
 }
 [TypeId("ebf0e317f1a04c4dae7e0935e474a4bd")]
 public sealed class PsychicStoredSpell:UnitBuffComponentDelegate<PsychicStoredSpellData>,ITargetRulebookHandler<RuleAttackWithWeapon>,ITargetRulebookHandler<RuleCombatManeuver>,IInitiatorRulebookHandler<RuleAttackWithWeapon>,ISubscriber {
  public int Kind;
  internal static Buff Find(UnitEntityData owner,BlueprintBuff bp)=>owner?.Buffs.Enumerable.FirstOrDefault(b=>b.Blueprint==bp&&b.IsTurnedOn&&!b.IsSuppressed);
  internal static PsychicStoredSpellData State(Buff b)=>b.Components.Single(c=>c.SourceBlueprintComponent is PsychicStoredSpell).GetData<PsychicStoredSpellData>();
  internal static bool Eligible(int kind,int level,AbilityData a,TargetWrapper target){
   if(a?.Spellbook==null||a.Blueprint.Type!=AbilityType.Spell||a.Blueprint.HasVariants||a.Blueprint.CanTargetPoint||a.Blueprint.GetComponent<AbilityTargetsAround>()!=null||a.Blueprint.GetComponents<AbilityEffectRunAction>().Any(c=>c.Actions.Actions.Any(x=>x is ContextActionSpawnMonster))||PsychicStorageSpells.Tokens.Any(t=>a.Blueprint.name.StartsWith("CotWWrathPsychic"+t)))return false;
   int max=kind==0?Math.Min(6,level/3):kind==3?3:4;if(a.SpellLevel<1||a.SpellLevel>max||kind==3&&level<14)return false;
   if(kind==1||kind==3)return a.Blueprint.GetComponent<AbilityEffectStickyTouch>()!=null&&a.Blueprint.CanTargetEnemies&&!a.Blueprint.NotOffensive&&!a.Blueprint.IsFullRoundAction&&a.ActionType!=UnitCommand.CommandType.Move;
   return target?.Unit==a.Caster.Unit&&a.Blueprint.CanTargetSelf&&a.Spellbook.Blueprint.IsAlchemist==(kind==2);
  }
  internal void Arm(){if(Data.Spell!=null||Kind==3&&(Context?.Params.CasterLevel??0)<14)return;bool on=!Data.Armed;foreach(var b in Owner.Buffs.Enumerable.ToArray())if(b.GetComponent<PsychicStoredSpell>()!=null)State(b).Armed=false;Data.Armed=on;}
  internal void Capture(AbilityExecutionContext c){Data.Spell=new AbilityData(c.Ability,c.Ability.Blueprint);Data.Parameters=c.Params.Clone();Data.Armed=false;}
  internal void Release(UnitEntityData target){var stored=Data.Spell;var parameters=Data.Parameters;if(stored==null||parameters==null)return;Data.Spell=null;Data.Parameters=null;Data.Armed=false;if(Kind!=3)Buff.Remove();if(stored==null||parameters==null||target==null)return;Deliver(stored,parameters,target,Kind==1||Kind==3);}
  internal static void Deliver(AbilityData stored,AbilityParams parameters,UnitEntityData target,bool touch){
   var blueprint=touch?stored.Blueprint.GetComponent<AbilityEffectStickyTouch>()?.TouchDeliveryAbility:stored.Blueprint;if(blueprint==null)return;
   var spell=new AbilityData(blueprint,stored.Caster){MetamagicData=SnapshotMetamagic(stored,parameters),SaveSpellbookSlot=true,OverrideCasterLevel=parameters.CasterLevel,OverrideDC=parameters.DC,OverrideSpellLevel=parameters.SpellLevel};
   if(!spell.CanTarget(new TargetWrapper(target)))return;
   var cast=new RuleCastSpell(spell,new TargetWrapper(target)){IsCutscene=true};cast.Context.SetParams(parameters.Clone());cast.Context.ForceAlwaysHit=touch;cast.Context.IsForced=true;PsychicStoragePatch.MarkRelease(cast.Context);Launch(cast);
  }
  internal static MetamagicData SnapshotMetamagic(AbilityData stored,AbilityParams p){var m=stored.MetamagicData?.Clone()??new MetamagicData();m.Add(p.Metamagic);return m;}
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static void Launch(RuleCastSpell cast)=>Rulebook.Trigger(cast);
  protected override void OnDeactivate(){Data.Armed=false;Data.Spell=null;Data.Parameters=null;}
  public void OnEventAboutToTrigger(RuleAttackWithWeapon e){}
  public void OnEventDidTrigger(RuleAttackWithWeapon e){if(Data.Spell==null||e.AttackRoll?.IsHit!=true||!e.Weapon.Blueprint.IsMelee)return;if(Kind==1&&e.Target==Owner)Release(e.Initiator);else if(Kind==3&&e.Initiator==Owner&&e.Weapon==Owner.Body.PrimaryHand.MaybeWeapon&&e.Weapon.Blueprint.name.StartsWith("CotWWrathPsychicEmblemOfGreedItem"))Release(e.Target);}
  public void OnEventAboutToTrigger(RuleCombatManeuver e){}
  public void OnEventDidTrigger(RuleCombatManeuver e){if(Kind==1&&Data.Spell!=null&&e.Success)Release(e.Initiator);}
 }
 [TypeId("b71d6a71cbec42768ad3533a8902a2ac")]
 public sealed class PsychicStorageAction:ContextAction {
  public BlueprintBuffReference Host;public bool Arm;public override string GetCaption()=>Arm?"Arm companion spell storage":"Release stored companion spell";
  public override void RunAction(){var b=PsychicStoredSpell.Find(Context.MaybeCaster,Host.Get());if(b==null)return;var rt=b.Components.Single(c=>c.SourceBlueprintComponent is PsychicStoredSpell);using(rt.RequestEventContext()){var c=(PsychicStoredSpell)rt.SourceBlueprintComponent;if(Arm)c.Arm();else c.Release(Context.MaybeCaster);}}
 }
 [TypeId("42bf13eb80a64d8da4ead6b2a05bde93")]
 public sealed class PsychicStorageReady:BlueprintComponent,IAbilityCasterRestriction {
  public BlueprintBuffReference Host;public int Mode;
  public bool IsCasterRestrictionPassed(UnitEntityData caster){var b=PsychicStoredSpell.Find(caster,Host.Get());if(b==null)return false;var data=PsychicStoredSpell.State(b);return Mode==1?data.Spell!=null:data.Spell==null&&(b.GetComponent<PsychicStoredSpell>().Kind!=3||b.Context.Params.CasterLevel>=14);}
  public string GetAbilityCasterRestrictionUIText()=>Mode==1?"No companion spell is stored.":"Already storing a spell, or caster level is too low.";
 }
 internal static class PsychicStoragePatch {
  static readonly ConditionalWeakTable<AbilityExecutionContext,object> Captured=new ConditionalWeakTable<AbilityExecutionContext,object>();
  static readonly ConditionalWeakTable<AbilityExecutionContext,object> Released=new ConditionalWeakTable<AbilityExecutionContext,object>();
  internal static void MarkRelease(AbilityExecutionContext context){Released.GetValue(context,_=>new object());}
  [HarmonyPatch(typeof(AbilityExecutionProcess),"DoApplyEffect")]
  internal static class Effect {
   [HarmonyPriority(Priority.First)]
   static bool Prefix(AbilityExecutionContext __0,TargetWrapper __1,AbilityApplyEffect __2){return Capture(__0,__1,__2);}
  }
  // ApplyEffectImmediate and external delivery controllers call these native
  // effects directly; they do not pass through DoApplyEffect.
  [HarmonyPatch(typeof(AbilityEffectRunAction),nameof(AbilityEffectRunAction.Apply))]
  internal static class RunActionEffect {
   [HarmonyPriority(Priority.First)]
   static bool Prefix(AbilityEffectRunAction __instance,AbilityExecutionContext __0,TargetWrapper __1)=>Capture(__0,__1,__instance);
  }
  [HarmonyPatch(typeof(AbilityEffectStickyTouch),nameof(AbilityEffectStickyTouch.Apply))]
  internal static class StickyTouchEffect {
   [HarmonyPriority(Priority.First)]
   static bool Prefix(AbilityEffectStickyTouch __instance,AbilityExecutionContext __0,TargetWrapper __1)=>Capture(__0,__1,__instance);
  }
  [HarmonyPatch(typeof(AbilityData),"get_Spellbook")]
  internal static class ButtonSpellbook {
   static bool Prefix(AbilityData __instance,ref Spellbook __result){
    if(__instance.Blueprint.Type!=AbilityType.Supernatural||__instance.Blueprint.GetComponent<PsychicStorageReady>()==null)return true;
    __result=null;return false;
   }
  }
  internal static bool Capture(AbilityExecutionContext c,TargetWrapper target,AbilityApplyEffect effect){
   if(c==null)return true;if(Captured.TryGetValue(c,out _))return false;
   // IsForced also denotes ordinary casts by native cheater/testing units. It
   // cannot identify our discharged spells; tag only our own release contexts.
   if(Released.TryGetValue(c,out _)||c.Ability?.Spellbook==null||!(effect is AbilityEffectRunAction||effect is AbilityEffectStickyTouch))return true;
   foreach(var b in c.MaybeCaster.Buffs.Enumerable.ToArray()){
    var comp=b.GetComponent<PsychicStoredSpell>();if(comp==null||!b.IsTurnedOn||b.IsSuppressed)continue;var d=PsychicStoredSpell.State(b);if(!d.Armed||d.Spell!=null||!PsychicStoredSpell.Eligible(comp.Kind,b.Context.Params.CasterLevel,c.Ability,target))continue;
    var rt=b.Components.Single(x=>x.SourceBlueprintComponent is PsychicStoredSpell);using(rt.RequestEventContext())comp.Capture(c);Captured.Add(c,new object());return false;
   }return true;
  }
 }
}
