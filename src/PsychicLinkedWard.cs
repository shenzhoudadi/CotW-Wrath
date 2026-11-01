using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Controllers;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using Kingmaker.UnitLogic.Buffs.Components;
using Newtonsoft.Json;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.FactLogic;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class PsychicLinkedWard {
  internal static readonly string[] Tokens={"Sanctuary","BattlemindLink","RejuvenateEidolon"};
  internal static ContextRankConfig CL(){var c=new ContextRankConfig();SetEnum(c,"m_BaseValueType","CasterLevel");return c;}
  internal static BlueprintAbility Spell(PsychicAssets f,string token,string title,string desc,SpellSchool school,BlueprintAbility icon,params BlueprintComponent[] components){
   var a=f.Ability(token,title,desc,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Touch,new BlueprintComponent[]{new SpellComponent{School=school},CL(),new AbilitySpawnFx{PrefabLink=new PrefabLink{AssetId="8de64fbe047abc243a9b4715f643739f"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget}}.Concat(components).ToArray());
   a.NotOffensive=true;a.CanTargetSelf=true;a.CanTargetFriends=true;a.CanTargetEnemies=false;a.SpellResistance=false;a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach;Set(a,"m_Icon",icon.Icon);a.LocalizedDuration=Text(token,"duration","Instantaneous");a.LocalizedSavingThrow=Text(token,"savingThrow","");return a;
  }
  internal static void Build(PsychicAssets f){
   var icon=f.Native<BlueprintAbility>("e84fc922ccf952943b5240293669b171");
   const string ward="Opponents must succeed at a Will save to directly attack the warded creature. Each opponent's result is remembered for this casting, including after saving and loading. Failed attackers cannot directly attack it until this ward ends. Area effects are unaffected. Making an offensive action ends your ward. Lasts 1 round per caster level. The command target check rejects prohibited attacks; it does not impose an additional action cost.";
   var b=f.Buff("SanctuaryBuff","Sanctuary",ward,new PsychicSanctuary());Set(b,"m_Icon",icon.Icon);var apply=Apply(b,0);apply.DurationValue.BonusValue=new ContextValue{ValueType=ContextValueType.Rank};
   var a=Spell(f,"Sanctuary","Sanctuary",ward,SpellSchool.Abjuration,icon,Run(apply));a.LocalizedDuration=Text("Sanctuary","duration","1 round per caster level");a.LocalizedSavingThrow=Text("Sanctuary","savingThrow","Will negates (attacker)");
   const string link="Link your thoughts with one ally for 1 minute per caster level. Both gain +2 initiative. After one partner attempts a melee or ranged attack against an enemy, the other gains +2 on attacks of the same kind against that enemy for 1 round. After one partner targets an enemy with a spell, that enemy takes -2 on saves against the other partner's spells for 1 round. This follows Call of the Wild's coordinated-attack implementation. A creature can have only one link; replacing or ending either half ends the pair.";
   var buff=f.Buff("BattlemindLinkBuff","Battlemind Link",link,new AddStatBonus{Stat=StatType.Initiative,Value=2,Descriptor=ModifierDescriptor.UntypedStackable},new PsychicBattlemind());Set(buff,"m_Icon",icon.Icon);
   var action=new PsychicMakeLink{Buff=Ref<BlueprintBuffReference>(buff),Duration=new ContextDurationValue{Rate=DurationRate.Minutes,DiceType=Kingmaker.RuleSystem.DiceType.Zero,DiceCountValue=0,BonusValue=new ContextValue{ValueType=ContextValueType.Rank}}};
   a=Spell(f,"BattlemindLink","Battlemind Link",link,SpellSchool.Divination,icon,Run(action),new SpellDescriptorComponent{Descriptor=SpellDescriptor.MindAffecting});a.Range=AbilityRange.Close;a.CanTargetSelf=false;a.LocalizedDuration=Text("BattlemindLink","duration","1 minute per caster level");
   PsychicEidolonHealing.Build(f);
   foreach(var x in f.Assets.Where(x=>Tokens.Any(t=>x.Key.StartsWith(t))))PsychicRepairComponents.UniqueNames(x.Value);
  }
 }
 public sealed class PsychicSanctuaryData {[JsonProperty] public Dictionary<string,bool> Results=new Dictionary<string,bool>();}
 [TypeId("69192f19950e4926a84b0408e6d11b2e")]
 public sealed class PsychicSanctuary:UnitBuffComponentDelegate<PsychicSanctuaryData>,IUnitMakeOffensiveActionHandler,ITargetRulebookHandler<RuleAttackRoll>,IUnitSubscriber {
  public bool Allows(UnitEntityData attacker){if(attacker==null||!attacker.IsEnemy(Owner))return true;if(Data.Results.TryGetValue(attacker.UniqueId,out var pass))return pass;pass=Context.TriggerRule(new RuleSavingThrow(attacker,SavingThrowType.Will,Context.Params.DC)).IsPassed;Data.Results[attacker.UniqueId]=pass;return pass;}
  public void OnEventAboutToTrigger(RuleAttackRoll e){if(!e.IsFake&&!Allows(e.Initiator))e.AutoMiss=true;}
  public void OnEventDidTrigger(RuleAttackRoll e){}
  public void HandleUnitMakeOffensiveAction(UnitEntityData target)=>Buff.Remove();
  internal static bool Blocks(UnitEntityData attacker,UnitEntityData target){if(target?.Buffs==null)return false;foreach(var b in target.Buffs.Enumerable.Where(b=>b.IsTurnedOn&&!b.IsSuppressed))foreach(var rt in b.Components.Where(c=>c.SourceBlueprintComponent is PsychicSanctuary))using(rt.RequestEventContext()){if(!((PsychicSanctuary)rt.SourceBlueprintComponent).Allows(attacker))return true;}return false;}
 }
 public sealed class PsychicBattlemindData {
  [JsonProperty] public UnitEntityData Partner;
  [JsonProperty] public string Pair;
  [JsonProperty] public Dictionary<string,TimeSpan> Melee=new Dictionary<string,TimeSpan>(),Ranged=new Dictionary<string,TimeSpan>(),Spells=new Dictionary<string,TimeSpan>();
 }
 [TypeId("ae85f630b6b64865b2710057272849c6")]
 public sealed class PsychicBattlemind:UnitBuffComponentDelegate<PsychicBattlemindData>,IInitiatorRulebookHandler<RuleAttackRoll>,IGlobalRulebookHandler<RuleSavingThrow>,ISubscriber {
  internal static TimeSpan Now=>Kingmaker.Game.Instance.TimeController.GameTime;
  internal static Buff Find(UnitEntityData u)=>u?.Buffs?.Enumerable.FirstOrDefault(b=>b.IsTurnedOn&&!b.IsSuppressed&&b.Blueprint.GetComponent<PsychicBattlemind>()!=null);
  internal static PsychicBattlemindData State(Buff b)=>b.Components.Single(c=>c.SourceBlueprintComponent is PsychicBattlemind).GetData<PsychicBattlemindData>();
  internal Buff Partner(){var other=Find(Data.Partner);return other!=null&&Data.Pair!=null&&State(other).Pair==Data.Pair&&State(other).Partner==Owner?other:null;}
  internal bool Match(Dictionary<string,TimeSpan> values,UnitEntityData target)=>target!=null&&values.TryGetValue(target.UniqueId,out var until)&&Now<until;
  public void OnEventAboutToTrigger(RuleAttackRoll e){if(e.IsFake||e.Target==null||!e.Target.IsEnemy(Owner))return;var other=Partner();if(other==null)return;bool ranged=e.AttackType==AttackType.Ranged||e.AttackType==AttackType.RangedTouch;if(Match(ranged?State(other).Ranged:State(other).Melee,e.Target))e.AddTemporaryModifier(Owner.Stats.AdditionalAttackBonus.AddModifier(2,Fact,ModifierDescriptor.UntypedStackable));}
  public void OnEventDidTrigger(RuleAttackRoll e){if(e.IsFake||e.Target==null||!e.Target.IsEnemy(Owner)||Partner()==null)return;(e.AttackType==AttackType.Ranged||e.AttackType==AttackType.RangedTouch?Data.Ranged:Data.Melee)[e.Target.UniqueId]=Now+TimeSpan.FromSeconds(6);}
  public void OnEventAboutToTrigger(RuleSavingThrow e){if(e.Reason?.Context?.MaybeCaster!=Owner||e.Reason.Context.SourceAbility?.Type!=AbilityType.Spell)return;var other=Partner();if(other!=null&&Match(State(other).Spells,e.Initiator))e.AddModifier(-2,Fact,ModifierDescriptor.UntypedStackable);}
  public void OnEventDidTrigger(RuleSavingThrow e){}
  protected override void OnDeactivate(){var other=Partner();Data.Pair=null;Data.Partner=null;other?.Remove();}
  internal void MarkSpell(UnitEntityData target){if(target!=null&&target.IsEnemy(Owner)&&Partner()!=null)Data.Spells[target.UniqueId]=Now+TimeSpan.FromSeconds(6);}
 }
 [TypeId("f9cc986b5b8d48d586239e9a58aa7c93")]
 public sealed class PsychicMakeLink:ContextAction {
  public BlueprintBuffReference Buff;public ContextDurationValue Duration;
  public override string GetCaption()=>"Create a reciprocal battle link";
  public override void RunAction(){var caster=Context.MaybeCaster;var target=Target.Unit;if(caster==null||target==null||caster==target)return;
   PsychicBattlemind.Find(caster)?.Remove();PsychicBattlemind.Find(target)?.Remove();
   var time=Duration.Calculate(Context).Seconds;var a=caster.Buffs.AddBuff(Buff.Get(),Context,time);var b=target.Buffs.AddBuff(Buff.Get(),Context,time);
   if(a==null||b==null){a?.Remove();b?.Remove();return;}a.IsFromSpell=b.IsFromSpell=true;var x=PsychicBattlemind.State(a);var y=PsychicBattlemind.State(b);var pair=Guid.NewGuid().ToString("N");x.Partner=target;y.Partner=caster;x.Pair=y.Pair=pair;
  }
 }
 internal static class PsychicLinkedWardPatches {
  [HarmonyPatch(typeof(UnitCommand),nameof(UnitCommand.CommandTargetUntargetable))]
  internal static class TargetPatch {static void Postfix(EntityDataBase sourceEntity,UnitEntityData targetUnit,RulebookEvent evt,ref bool __result){if(!__result&&evt==null)__result=PsychicSanctuary.Blocks(sourceEntity as UnitEntityData,targetUnit);}}
  [HarmonyPatch(typeof(AbilityExecutionProcess),"DoApplyEffect")]
  internal static class SpellPatch {
   static bool Prefix(AbilityExecutionContext __0,TargetWrapper __1,AbilityApplyEffect __2){if(PsychicGlobeSpells.IsPreparation(__0,__2)||__0?.AbilityBlueprint?.Type!=AbilityType.Spell)return true;return __0.AbilityBlueprint.NotOffensive||__0.AbilityBlueprint.CanTargetPoint||__0.AbilityBlueprint.GetComponent<AbilityTargetsAround>()!=null||!PsychicSanctuary.Blocks(__0.MaybeCaster,__1?.Unit);}
   static void Mark(UnitEntityData caster,UnitEntityData target){var b=PsychicBattlemind.Find(caster);if(b==null)return;var rt=b.Components.Single(x=>x.SourceBlueprintComponent is PsychicBattlemind);using(rt.RequestEventContext())((PsychicBattlemind)rt.SourceBlueprintComponent).MarkSpell(target);}
   // Keep the original postfix semantics without a newer Harmony injection. A
   // callback in the original body cannot run when ANY prefix skips that body
   // (Sanctuary, Globe, spell storage or another mod), unlike an ordinary postfix.
   static void Completed(AbilityExecutionContext context,TargetWrapper target,AbilityApplyEffect effect){if(context?.AbilityBlueprint?.Type==AbilityType.Spell&&!PsychicGlobeSpells.IsPreparation(context,effect))Mark(context.MaybeCaster,target?.Unit);}
   static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions){
    var completed=AccessTools.Method(typeof(SpellPatch),nameof(Completed));
    int exits=0;
    foreach(var original in instructions){
     var code=new CodeInstruction(original);
     if(code.opcode==OpCodes.Ret){
      var load=new CodeInstruction(OpCodes.Ldarg_0);
      load.labels.AddRange(code.labels);code.labels.Clear();
      // A branch to a native return must execute the notification as well.
      // Preserve exception-region boundaries carried by the return instruction.
      load.blocks.AddRange(code.blocks);code.blocks.Clear();
      yield return load;yield return new CodeInstruction(OpCodes.Ldarg_1);
      yield return new CodeInstruction(OpCodes.Ldarg_2);yield return new CodeInstruction(OpCodes.Call,completed);
      exits++;
     }
     yield return code;
    }
    if(exits==0)throw new InvalidOperationException("Battlemind Link: native effect completion path was not found.");
   }
  }
 }
}
