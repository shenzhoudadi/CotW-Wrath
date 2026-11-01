using System;using System.Linq;using HarmonyLib;using Newtonsoft.Json.Linq;using Kingmaker.Controllers;
using Kingmaker;using Kingmaker.Blueprints;using Kingmaker.Blueprints.Classes;using Kingmaker.Blueprints.JsonSystem;using Kingmaker.EntitySystem.Entities;using Kingmaker.EntitySystem.Stats;using Kingmaker.RuleSystem;using Kingmaker.RuleSystem.Rules;using Kingmaker.RuleSystem.Rules.Damage;using Kingmaker.UnitLogic;using Kingmaker.UnitLogic.Abilities;using Kingmaker.UnitLogic.Abilities.Blueprints;using Kingmaker.UnitLogic.Abilities.Components;using Kingmaker.UnitLogic.Abilities.Components.Base;using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;using Kingmaker.UnitLogic.Buffs;using Kingmaker.UnitLogic.Buffs.Blueprints;using Kingmaker.UnitLogic.Buffs.Components;using Kingmaker.UnitLogic.FactLogic;using Kingmaker.UnitLogic.Mechanics;using Kingmaker.Utility;using CallOfTheWild.Wrath.Prototype;
internal static partial class MagusBatch01454Tests{
 static bool CombatOutside060(UnitEntityData __instance,ref bool __result){if(!Actors.Contains(__instance))return true;__result=false;return false;}
 static void EffectInputs060(Harmony h){
  h.Patch(AccessTools.Method(typeof(UnitEntityData),"IsEnemy",new[]{typeof(UnitEntityData)}),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Enemy057)){priority=Priority.First});
  h.Patch(AccessTools.Method(typeof(RuleSavingThrow),"OnTrigger"),postfix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Save057)));
  Saves058.Clear();h.Patch(AccessTools.Method(typeof(RuleSavingThrow),"OnTrigger"),postfix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(ObserveSave058)));
  h.Patch(AccessTools.Method(typeof(AbilityExecutionProcess),"DoApplyEffect",new[]{typeof(AbilityExecutionContext),typeof(TargetWrapper),typeof(AbilityApplyEffect)}),transpiler:new HarmonyMethod(typeof(Archetypes01445Tests),"NestedRules"));
  h.Patch(AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnFx),"RunAction"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(SceneFx058)));
  h.Patch(AccessTools.Method(typeof(AbilitySpawnFx),"Spawn"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(SceneFx058)));
  foreach(string method in new[]{"ApplyDifficultyBeforeCalculated","ApplyDifficultyForDamageReduction"})h.Patch(AccessTools.Method(typeof(RuleCalculateDamage),method),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(SceneFx058)));
  h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Settings.SettingsRoot),"Difficulty"),prefix:new HarmonyMethod(typeof(IncreasedDamageReduction01439Tests),"NeutralDifficulty"));
  h.Patch(AccessTools.Method(typeof(ShadowSpellLogicExtension),"TryApplyShadowDamageFactor",new[]{typeof(RulebookEvent),typeof(UnitEntityData),typeof(int)}),prefix:new HarmonyMethod(typeof(IncreasedDamageReduction01439Tests),"NoShadow"));
  foreach(var type in new[]{typeof(RuleDealDamage),typeof(RuleCalculateDamage)})h.Patch(AccessTools.Method(type,"OnTrigger"),transpiler:new HarmonyMethod(typeof(Archetypes01445Tests),"NestedRules"));
  h.Patch(AccessTools.Method(typeof(RuleDealDamage),"OnTrigger"),transpiler:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(DamageScene058)));
 }
 static void Psychedelia060(BlueprintsCache c,Harmony h){
  h.Patch(AccessTools.PropertyGetter(typeof(UnitEntityData),"IsInCombat"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(CombatOutside060)){priority=Priority.First});
  var owner=EquipmentUnit(c,13,Psychic058);var ally=EquipmentUnit(c,13,Psychic058);owner.Stats.HitPoints.BaseValue=1000;ally.Stats.HitPoints.BaseValue=1000;Grant(owner,c,"WarpedBrainFeature01458");
  Check(!owner.IsInCombat&&!ally.IsInCombat,"060 Warped native subjects are outside combat");
  foreach(string token in new[]{"MindThrust1","SynapticPulse"})foreach(int roll in new[]{1,20}){
   D20Input058=roll;var bp=A<BlueprintAbility>(c,token);int before=WarpedCount058();var ctx=Context058(ally,owner,bp,token=="MindThrust1"?1:3);NativeApply058(ctx,owner);
   Check(WarpedCount058()==before+1,"060 real native spell publisher retaliates against allied harmful spell irrespective of save: "+token+" / "+roll);
   Check(ally.State.HasCondition(UnitCondition.Nauseated)==(roll==1),"060 actual Warped retaliation nausea follows the caster's native save: "+token+" / "+roll);
   NativeApply058(ctx,owner);Check(WarpedCount058()==before+1,"060 one allied cast cannot repeat Warped retaliation: "+token+" / "+roll);
   foreach(var b in owner.Buffs.Enumerable.Concat(ally.Buffs.Enumerable).Where(b=>b.Blueprint.AssetGuid==Id("PsychicNauseated01458")).ToArray())b.Remove();
  }
  int count=WarpedCount058();D20Input058=1;NativeApply058(Context058(ally,owner,N<BlueprintAbility>(c,"5ab0d42fb68c9e34abae4921822b9d63"),3),owner);NativeApply058(Context058(owner,owner,A<BlueprintAbility>(c,"MindThrust1")),owner);
  Check(WarpedCount058()==count,"060 friendly helpful Heroism and self-cast do not trigger Warped");
  var auraFeature=A<BlueprintFeature>(c,"HallucinogenicAuraFeature01458");Check(!auraFeature.GetComponents<AddAreaEffect>().Any(),"060 native Buff-only emitter is never attached to a feature");
  var fact=Grant(owner,auraFeature);var host=owner.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("HallucinogenicAuraHost01460"));var runtime=host.Components.Single(r=>r.SourceBlueprintComponent is AddAreaEffect);
  using(runtime.RequestEventContext())Check(AccessTools.Property(runtime.SourceBlueprintComponent.GetType(),"Buff").GetValue(runtime.SourceBlueprintComponent,null)==host,"060 actual native emitter has the real permanent host Buff rather than null");
  Check(host.IsPermanent&&host.Context.MaybeCaster==owner,"060 native feature grant creates a permanent source-owned aura host outside combat");
  host=AuraLifecycle060(owner,fact,host,h);
  var areaBp=A<BlueprintAbilityAreaEffect>(c,"HallucinogenicAuraArea01458");Check(areaBp.Size.Value==20&&areaBp.Fx.AssetId=="20caf000cd4c3434da00a74f4a49dccc","060 twenty-foot Hallucinogenic area retains the real ring asset");
  var enemy=EquipmentUnit(c,13);Enemies057.Add(enemy);enemy.Stats.Wisdom.BaseValue=10;enemy.Stats.SaveWill.BaseValue=0;var area=(AreaEffectEntityData)AccessTools.Method(typeof(TimeStutter01437Tests),"MakeArea").Invoke(null,new object[]{"060-hallucinogenic",host.Context,areaBp,new[]{enemy,ally,owner}});
  D20Input058=1;int prior=Saves058.Count;areaBp.GetComponent<AbilityAreaEffectRunAction>().HandleUnitEnter(host.Context,area,enemy);
  Check(Saves058.Count==prior+1&&enemy.State.HasCondition(UnitCondition.Confusion),"060 native area entry from real host context rolls Will and applies confusion outside combat");
  prior=Saves058.Count;areaBp.GetComponent<AbilityAreaEffectRunAction>().HandleUnitEnter(host.Context,area,ally);areaBp.GetComponent<AbilityAreaEffectRunAction>().HandleUnitEnter(host.Context,area,owner);Check(Saves058.Count==prior,"060 aura retains ally and self exclusion");
  var confused=enemy.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("HallucinogenicAuraConfusion01458"));confused.Remove();Check(enemy.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==Id("HallucinogenicAuraCooldown01458")&&b.Context.MaybeCaster==owner),"060 native aura confusion removal starts source-owned24h immunity");
 }
 static void Synaptic060(BlueprintsCache c,Harmony h){
  var owner=EquipmentUnit(c,13,Psychic058);var enemy=EquipmentUnit(c,13);Enemies057.Add(enemy);enemy.Stats.Wisdom.BaseValue=10;enemy.Stats.SaveWill.BaseValue=0;
  Set(enemy,"m_Position",new UnityEngine.Vector3(3,0,0));Check(new TargetWrapper(enemy)!=new TargetWrapper(owner),"060 actual native target equality distinguishes the positioned area victim from the caster");
  var pool=Pool058(c,owner);var book=Book058(c,owner);var pulse=Known058(c,book,3,"SynapticPulse");Grant(owner,c,"SynapticShockFeature01458");ToggleOn(Toggle(owner,c,"SynapticShock01458Toggle"));
  var choose=A<BlueprintAbility>(c,"SynapticShockTargetAbility01458");var confusion=A<BlueprintBuff>(c,"SynapticShockConfusion01458");var pending=A<BlueprintBuff>(c,"SynapticShockPending01460");
  Super058(owner,choose,enemy);owner.Resources.Spend(pool,owner.Resources.GetResourceAmount(pool)-1);D20Input058=1;var cast=Linked058(pulse,owner);NativeCast058(cast,h);
  Check(owner.Resources.GetResourceAmount(pool)==0,"060 real linked cast reserves the final Synaptic point before delivery");AbilityExecutionProcess.PrepareCast(cast.Context);cast.Context.FxSpawners=pulse.Blueprint.GetComponents<AbilitySpawnFx>().ToList();Console.WriteLine("060 Pulse definition "+cast.Context.AbilityBlueprint.name+" save="+pulse.Blueprint.GetComponent<AbilityEffectRunAction>().SavingThrowType+" actions="+pulse.Blueprint.GetComponent<AbilityEffectRunAction>().Actions.Actions.Length+" owner="+owner.UniqueId+" enemy="+enemy.UniqueId+" actualcaster="+cast.Context.MaybeCaster.UniqueId+" targetEqual="+(new TargetWrapper(enemy)==new TargetWrapper(owner))+" immunity="+(enemy.Get<Kingmaker.UnitLogic.Parts.UnitPartSpellResistance>()?.IsImmune(cast.Context,false).ToString()??"none"));NativeApply058(cast.Context,enemy);
  Console.WriteLine("060 Synaptic native delivery: saves="+string.Join(",",Saves058.Select(s=>s.ToString()))+" buffs="+string.Join(",",enemy.Buffs.Enumerable.Select(b=>b.Blueprint.name+"/"+b.TimeLeft+"/source="+ReferenceEquals(b.Context?.SourceAbilityContext,cast.Context)))+" stunned="+enemy.State.HasCondition(UnitCondition.Stunned)+" confused="+enemy.State.HasCondition(UnitCondition.Confusion));
  Check(enemy.State.HasCondition(UnitCondition.Stunned)&&!enemy.State.HasCondition(UnitCondition.Confusion)&&enemy.Buffs.Enumerable.Any(b=>b.Blueprint==pending),"060 native failed Synaptic Pulse postpones confusion while stun is active");
  var stun=enemy.Buffs.Enumerable.Single(b=>ReferenceEquals(b.Context?.SourceAbilityContext,cast.Context)&&b.Blueprint!=pending);var until=stun.EndTime;Game.Instance.Player.GameTime=until;enemy.Buffs.TickBuff(stun,until);
  Check(!stun.IsAttached&&!enemy.Buffs.Enumerable.Any(b=>b.Blueprint==pending)&&enemy.State.HasCondition(UnitCondition.Confusion),"060 actual native timed stun expiry triggers one deferred Confused condition");
  var confused=enemy.Buffs.Enumerable.Single(b=>b.Blueprint==confusion);Check(confused.EndTime==until+TimeSpan.FromSeconds(6)&&owner.Resources.GetResourceAmount(pool)==0,"060 deferred confusion lasts one new round without a second point charge");enemy.Buffs.TickBuff(confused,confused.EndTime);Check(!enemy.State.HasCondition(UnitCondition.Confusion),"060 actual native confusion expires after its own full round");
  owner.Resources.Restore(pool,99);Super058(owner,choose,enemy);int before=owner.Resources.GetResourceAmount(pool);D20Input058=20;var saved=Linked058(pulse,owner);NativeCast058(saved,h);AbilityExecutionProcess.PrepareCast(saved.Context);saved.Context.FxSpawners=pulse.Blueprint.GetComponents<AbilitySpawnFx>().ToList();NativeApply058(saved.Context,enemy);
  Check(owner.Resources.GetResourceAmount(pool)==before-1&&enemy.State.HasCondition(UnitCondition.Confusion)&&!enemy.State.HasCondition(UnitCondition.Stunned)&&!enemy.Buffs.Enumerable.Any(b=>b.Blueprint==pending),"060 native successful linked-spell save applies immediate confusion and charges once");
  NativeApply058(saved.Context,enemy);Check(owner.Resources.GetResourceAmount(pool)==before-1&&enemy.Buffs.Enumerable.Count(b=>b.Blueprint==confusion)==1,"060 repeated native target notifications cannot multiply Synaptic cost or confusion");
  SynapticEdges060(c,h,owner,enemy,pulse,choose,pending,confusion,pool);
 }
}
