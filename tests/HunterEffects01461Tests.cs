using System;using System.Linq;using System.Collections.Generic;using HarmonyLib;using Newtonsoft.Json;using Newtonsoft.Json.Linq;
using Kingmaker.Items;
using Kingmaker;using Kingmaker.Blueprints;using Kingmaker.Blueprints.Classes;using Kingmaker.Blueprints.JsonSystem;using Kingmaker.EntitySystem;using Kingmaker.EntitySystem.Entities;using Kingmaker.EntitySystem.Stats;using Kingmaker.Enums;using Kingmaker.RuleSystem;using Kingmaker.RuleSystem.Rules;using Kingmaker.UnitLogic;using Kingmaker.UnitLogic.Abilities;using Kingmaker.UnitLogic.Abilities.Blueprints;using Kingmaker.UnitLogic.Abilities.Components;using Kingmaker.UnitLogic.ActivatableAbilities;using Kingmaker.UnitLogic.Buffs;using Kingmaker.UnitLogic.Buffs.Blueprints;using Kingmaker.UnitLogic.Mechanics;using Kingmaker.UnitLogic.Mechanics.Components;using Kingmaker.UnitLogic.Parts;using Kingmaker.Utility;using CallOfTheWild.Wrath.Prototype;
internal static partial class MagusBatch01454Tests{
 const string HunterClass061="34ecd1b5e1b90b9498795791b0855239";
 static void HunterEffects061(BlueprintsCache c,Harmony h){
  var clock=AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"RealTime");var prior=(Harmony.GetPatchInfo(clock)?.Prefixes.AsEnumerable()??Enumerable.Empty<Patch>()).ToArray();var real=Game.Instance.Player.RealTime;var combat=Game.Instance.TurnBasedCombatController;
  foreach(var p in prior){Check(p.PatchMethod.DeclaringType==typeof(BardMasterpieces01435Tests)&&p.PatchMethod.Name=="Time","061 replaces only inherited constant presentation-clock endpoint so native aspect ordering has distinct real timestamps");h.Unpatch(clock,p.PatchMethod);}Set(Game.Instance.Player,"RealTime",TimeSpan.FromHours(1));
  if(combat==null)Set(Game.Instance,"TurnBasedCombatController",System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TurnBased.Controllers.CombatController)));
  try{FeralFocus061(c);FeralSummonParameters061(c);FeralTactics061(c);FeralShapes061(c,h);TotemLeases061(c,h);TotemPet061(c);TotemCharge061(c);}
  finally{Set(Game.Instance,"TurnBasedCombatController",combat);Set(Game.Instance.Player,"RealTime",real);foreach(var p in prior)new Harmony(p.owner).Patch(clock,prefix:new HarmonyMethod(p.PatchMethod){priority=p.priority,before=p.before,after=p.after});}
 }
 static void HunterStart061(ActivatableAbility ability){
  Set(Game.Instance.Player,"RealTime",Game.Instance.Player.RealTime+TimeSpan.FromMilliseconds(10));ability.IsOn=true;
  var tick=AccessTools.Method(typeof(Kingmaker.Controllers.Units.UnitActivatableAbilitiesController),"TickAbility");var controller=new Kingmaker.Controllers.Units.UnitActivatableAbilitiesController();
  foreach(var old in ability.Owner.ActivatableAbilities.Enumerable.Where(a=>a!=ability&&a.IsStarted&&!a.IsOn).ToArray())tick.Invoke(controller,new object[]{ability.Owner.Unit,old,false});
  ability.TryStart();tick.Invoke(controller,new object[]{ability.Owner.Unit,ability,false});Check(ability.IsOn&&ability.IsStarted&&ability.AppliedBuff?.IsTurnedOn==true,"061 actual native controller starts persistent Hunter aspect and stops displaced old aspect: "+ability.Blueprint.name);
 }
 static void FeralFocus061(BlueprintsCache c){
  foreach(int level in new[]{1,8,15,20}){
   var u=EquipmentUnit(c,level,HunterClass061);int con=u.Stats.Constitution.ModifiedValue;var fact=Grant(u,c,"FeralHunterAnimalFocusFeature01461");
   Check(u.ActivatableAbilities.Enumerable.Count()==8&&!u.Resources.ContainsResource(N<BlueprintAbilityResource>(c,"72c9cd6d5a1464447a882590715d2b23")),"061 native unlimited Feral focus grants eight aspects without timed focus resource or self/pet lease toggles: "+level);
   var bear=u.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.AssetGuid==Id("FeralHunterFocusa5dba776428e8c441a8528edc444f3a8Toggle01461"));HunterStart061(bear);
   Check(u.Stats.Constitution.ModifiedValue==con+(level>=15?6:level>=8?4:2),"061 real native aspect actions and host grant the correct Constitution bonus: "+level);
   for(int i=0;i<4;i++)bear.OnNewRound();Check(bear.IsOn&&u.Stats.Constitution.ModifiedValue>con,"061 real native focus stays active across round ticks without spending a resource: "+level);
   u.Facts.Remove(fact);Check(u.Stats.Constitution.ModifiedValue==con&&!u.ActivatableAbilities.Enumerable.Any(),"061 native Feral grant removal removes only its focus children and bonuses: "+level);
  }
 }
 static void FeralSummonParameters061(BlueprintsCache c){
  foreach(int level in new[]{1,3,9,17,20}){
   var u=EquipmentUnit(c,level,HunterClass061);u.Stats.Wisdom.BaseValue=18;var resource=A<BlueprintAbilityResource>(c,"FeralHunterSummonResource01461");Grant(u,c,"FeralHunterSummon1Feature01461");Check(u.Resources.GetResourceAmount(resource)==7,"061 actual native Feral daily summon pool is 3 plus Wisdom: "+level);
   foreach(var circle in new[]{1,Math.Min(9,(level+1)/2)}.Distinct()){
    var f=A<BlueprintFeature>(c,"FeralHunterSummon"+circle+"Feature01461");var root=(BlueprintAbility)f.GetComponent<Kingmaker.UnitLogic.FactLogic.AddFacts>().Facts.Single();
    var abilities=HunterAbilityTree061(root).ToArray();
    foreach(var bp in abilities){var a=new AbilityData(bp,u.Descriptor);var p=a.CalculateParams();Check(p.CasterLevel==level&&p.SpellLevel==circle&&p.DC==14+circle,"061 actual native AbilityData.CalculateParams uses Hunter CL, exact source circle and Wisdom: "+level+"/"+circle+"/"+bp.name);if(bp.GetComponent<AbilityVariants>()==null){var context=new AbilityExecutionContext(a,p,new TargetWrapper(u),new RulebookEventContext(),u);context.Recalculate();Check(context.Params.CasterLevel==level&&context.Params.SpellLevel==circle&&context.Params.DC==p.DC,"061 native execution context recalculation agrees with the native preview: "+level+"/"+circle+"/"+bp.name);}else Check(a.GetConversions().Any(),"061 native variational summon wrapper offers its real selectable children rather than casting the wrapper");Check(a.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard&&!a.RequireFullRoundAction&&bp.Type==AbilityType.SpellLike,"061 native Feral summon is a standard-action SLA");}
    var leaf=abilities.Last();var paid=new AbilityData(leaf,u.Descriptor);int before=u.Resources.GetResourceAmount(resource);AbilityCastRateUtils.SpendResources(paid);Check(u.Resources.GetResourceAmount(resource)==before-1,"061 real native summon resource logic pays exactly once on the selected leaf: "+level+"/"+circle);
   }
  }
 }
 static IEnumerable<BlueprintAbility> HunterAbilityTree061(BlueprintAbility root){yield return root;var variants=root.GetComponent<AbilityVariants>();if(variants!=null)foreach(var child in variants.Variants)foreach(var ability in HunterAbilityTree061(child))yield return ability;}
 static void FeralTactics061(BlueprintsCache c){
  var owner=EquipmentUnit(c,4,HunterClass061);Grant(owner,c,"FeralHunterPreciseNatureAlly01461");var outflank=N<BlueprintFeature>(c,"422dab7309e1ad343935f33a4d6e9f11");Grant(owner,outflank);var privateSpell=(BlueprintAbility)A<BlueprintFeature>(c,"FeralHunterSummon1Feature01461").GetComponent<Kingmaker.UnitLogic.FactLogic.AddFacts>().Facts.Single();var effect=A<BlueprintBuff>(c,"FeralHunterSummonTacticsBuff01461");
  foreach(bool original in new[]{false,true}){
   var summoned=EquipmentUnit(c,2);var bp=original?N<BlueprintAbility>(c,"c6147854641924442a3bb736080cfeb6"):privateSpell;var ability=new AbilityData(bp,owner.Descriptor);var context=new AbilityExecutionContext(ability,ability.CalculateParams(),new TargetWrapper(summoned),new RulebookEventContext(),owner);var evt=new RuleSummonUnit(owner,summoned,new Rounds(40)){Context=context};Set(evt,"SummonedUnit",summoned);
   Kingmaker.PubSubSystem.RulebookEventBus.OnEventDidTrigger(evt);Check(summoned.HasFact(outflank)&&summoned.Buffs.Enumerable.Count(b=>b.Blueprint==effect)==1,"061 actual native summon-completion publisher grants caster-owned teamwork for "+(original?"original SNA spell":"private Feral SLA"));
   Kingmaker.PubSubSystem.RulebookEventBus.OnEventDidTrigger(evt);Check(summoned.Buffs.Enumerable.Count(b=>b.Blueprint==effect)==1&&summoned.Progression.Features.Enumerable.Count(f=>f.Blueprint==outflank)==1,"061 repeated real native summon event cannot duplicate Feral teamwork");summoned.Buffs.Enumerable.Single(b=>b.Blueprint==effect).Remove();Check(!summoned.HasFact(outflank),"061 native AddFactsFromCaster removes its owned temporary teamwork on summon share removal");
  }
  foreach(string rejected in new[]{"wrong-source","wrong-caster","wrong-initiator","no-context"}){
   var recipient=EquipmentUnit(c,2);var other=EquipmentUnit(c,4,HunterClass061);var bp=rejected=="wrong-source"?A<BlueprintAbility>(c,"MindThrust1"):privateSpell;var a=new AbilityData(bp,rejected=="wrong-caster"?other.Descriptor:owner.Descriptor);var context=new AbilityExecutionContext(a,a.CalculateParams(),new TargetWrapper(recipient),new RulebookEventContext(),a.Caster.Unit);var evt=new RuleSummonUnit(rejected=="wrong-initiator"?other:owner,recipient,new Rounds(40)){Context=rejected=="no-context"?null:context};Set(evt,"SummonedUnit",recipient);Kingmaker.PubSubSystem.RulebookEventBus.OnEventDidTrigger(evt);Check(!recipient.HasFact(outflank)&&!recipient.Buffs.Enumerable.Any(b=>b.Blueprint==effect),"061 actual native summon publisher rejects unrelated scope: "+rejected);
  }
  var existing=EquipmentUnit(c,2);var preexisting=Grant(existing,outflank);var data=new AbilityData(privateSpell,owner.Descriptor);var ctx=new AbilityExecutionContext(data,data.CalculateParams(),new TargetWrapper(existing),new RulebookEventContext(),owner);var same=new RuleSummonUnit(owner,existing,new Rounds(40)){Context=ctx};Set(same,"SummonedUnit",existing);Kingmaker.PubSubSystem.RulebookEventBus.OnEventDidTrigger(same);existing.Buffs.Enumerable.Single(b=>b.Blueprint==effect).Remove();Check(preexisting.IsAttached&&existing.HasFact(outflank),"061 native teamwork share preserves a pre-existing independently owned identical feat");
 }
 static void PolyViewInput061(Kingmaker.UnitLogic.Buffs.Polymorph source,bool immediate){}
 static void PolyRestoreInput061(Kingmaker.UnitLogic.Buffs.Polymorph source){}
 static RuleCalculateCMD HunterCMD061(UnitEntityData attacker,UnitEntityData defender,CombatManeuver maneuver){
  var rule=new RuleCalculateCMD(attacker,defender,maneuver);var events=new RulebookEventContext();var current=AccessTools.Field(typeof(BardMasterpieces01435Tests),"current");var previous=current.GetValue(null);events.PushEvent(rule);current.SetValue(null,events);
  try{Kingmaker.PubSubSystem.RulebookEventBus.OnEventAboutToTrigger(rule);rule.OnTrigger(events);Kingmaker.PubSubSystem.RulebookEventBus.OnEventDidTrigger(rule);return rule;}finally{events.PopEvent(rule);current.SetValue(null,previous);}
 }
 static RuleCheckTargetFlatFooted HunterFlatFootedScene061(RuleCheckTargetFlatFooted rule){Set(rule,"IsFlatFooted",false);return rule;}
 static IEnumerable<CodeInstruction> HunterCMDScene061(IEnumerable<CodeInstruction> instructions){int count=0;foreach(var i in instructions){if(i.operand is System.Reflection.MethodInfo m&&m.DeclaringType==typeof(Rulebook)&&m.IsGenericMethod&&m.Name=="Trigger"&&m.GetGenericArguments()[0]==typeof(RuleCheckTargetFlatFooted)){i.opcode=System.Reflection.Emit.OpCodes.Call;i.operand=AccessTools.Method(typeof(MagusBatch01454Tests),nameof(HunterFlatFootedScene061));count++;}yield return i;}if(count!=1)throw new InvalidOperationException("061 CMD test requires exactly one declared scene flat-footed query; native defense components and CMD calculations remain intact.");}
 static IEnumerable<CodeInstruction> PolyViewBoundary061(IEnumerable<CodeInstruction> instructions){int count=0;foreach(var i in instructions){if(i.operand is System.Reflection.MethodInfo m&&m.DeclaringType==typeof(Kingmaker.UnitLogic.Buffs.Polymorph)&&(m.Name=="TryReplaceView"||m.Name=="RestoreView")){i.opcode=System.Reflection.Emit.OpCodes.Call;i.operand=AccessTools.Method(typeof(MagusBatch01454Tests),m.Name=="TryReplaceView"?nameof(PolyViewInput061):nameof(PolyRestoreInput061));count++;}yield return i;}if(count!=1)throw new InvalidOperationException("061 actual native polymorph lifecycle requires exactly one declared Unity view replacement endpoint.");}
 static void FeralShapes061(BlueprintsCache c,Harmony h){
  var activate=AccessTools.Method(typeof(Kingmaker.UnitLogic.Buffs.Polymorph),"OnActivate");var deactivate=AccessTools.Method(typeof(Kingmaker.UnitLogic.Buffs.Polymorph),"OnDeactivate");var boundary=AccessTools.Method(typeof(MagusBatch01454Tests),nameof(PolyViewBoundary061));h.Patch(activate,transpiler:new HarmonyMethod(boundary));h.Patch(deactivate,transpiler:new HarmonyMethod(boundary));var cmd=AccessTools.Method(typeof(RuleCalculateCMD),"OnTrigger");var cmdScene=AccessTools.Method(typeof(MagusBatch01454Tests),nameof(HunterCMDScene061));h.Patch(cmd,transpiler:new HarmonyMethod(cmdScene));
  try{
  var resource=N<BlueprintAbilityResource>(c,"ae6af4d58b70a754d868324d1a05eda4");var root=A<BlueprintArchetype>(c,"FeralHunterArchetype01461");var fourLegs=N<BlueprintFeature>(c,"13c87ac5985cc85498ef9d1ac8b78923");
  foreach(int level in new[]{4,6,8,10,12,14,16,18,20}){var u=EquipmentUnit(c,level,HunterClass061);Grant(u,c,"FeralHunterWildShapeWolfFeature01461");for(int at=6;at<=level;at+=2)Grant(u,N<BlueprintFeature>(c,"f78260b9a089ccc44b55f0fed08b1752"));Check(resource.GetMaxAmount(u.Descriptor)==1+(level-4)/2,"061 real native shared Wild Shape resource and eight ranked native increments: "+level);}
  foreach(string shape in new[]{"Wolf","Leopard","Bear","DireWolf","Smilodon","Mastodon"}){
   int level=shape=="Wolf"||shape=="Leopard"?4:shape=="Bear"||shape=="DireWolf"?6:8;var u=EquipmentUnit(c,level,HunterClass061);var attacker=EquipmentUnit(c,level);Grant(u,c,"FeralHunterWildShapeWolfFeature01461");var bp=A<BlueprintAbility>(c,"FeralHunterWildShape"+shape+"Ability01461");var source=A<BlueprintBuff>(c,"FeralHunterWildShape"+shape+"Buff01461");var poly=source.GetComponent<Kingmaker.UnitLogic.Buffs.Polymorph>();int str=u.Stats.Strength.ModifiedValue,dex=u.Stats.Dexterity.ModifiedValue;
   Check(!u.HasFact(fourLegs)&&!HunterCMD061(attacker,u,CombatManeuver.Trip).AllBonuses.Any(m=>m.Fact?.Blueprint==fourLegs),"061 native CMD has no four-legged Trip defense before transformation: "+shape);
   var context=Super058(u,bp);var buff=u.Buffs.Enumerable.Single(b=>b.Blueprint==source);
   try{
   Check(u.Body.IsPolymorphed&&buff.TimeLeft==TimeSpan.FromHours(level)&&u.Resources.GetResourceAmount(resource)==0,"061 actual native Wild Shape action pays shared use and applies Hunter-level duration: "+shape);
   Check(u.Stats.Strength.ModifiedValue==str+poly.StrengthBonus&&u.Stats.Dexterity.ModifiedValue==dex+poly.DexterityBonus&&u.Body.PrimaryHand.MaybeWeapon?.Blueprint==poly.MainHand,"061 actual native polymorph applies its authored attributes and real natural primary weapon: "+shape);
   Check(u.HasFact(fourLegs)&&u.Facts.List.Count(f=>f.Blueprint==fourLegs)==1,"061 native polymorph grants exactly one four-legged defense fact: "+shape);
   var trip=HunterCMD061(attacker,u,CombatManeuver.Trip);var disarm=HunterCMD061(attacker,u,CombatManeuver.Disarm);Check(trip.AllBonuses.Where(m=>m.Fact?.Blueprint==fourLegs).Sum(m=>m.Value)==4&&trip.Result==disarm.Result+4,"061 actual native CMD publisher and rule apply four-legged Trip defense plus four: "+shape);
   Check(Enum.GetValues(typeof(CombatManeuver)).Cast<CombatManeuver>().Where(m=>m!=CombatManeuver.Trip).All(m=>!HunterCMD061(attacker,u,m).AllBonuses.Any(b=>b.Fact?.Blueprint==fourLegs)),"061 native four-legged defense does not affect unrelated maneuvers: "+shape);
   Check(!attacker.HasFact(fourLegs)&&!HunterCMD061(u,attacker,CombatManeuver.Trip).AllBonuses.Any(m=>m.Fact?.Blueprint==fourLegs),"061 native four-legged defense remains scoped to the transformed defender: "+shape);
   buff.Remove();Check(!u.Body.IsPolymorphed&&u.Stats.Strength.ModifiedValue==str&&u.Stats.Dexterity.ModifiedValue==dex,"061 native polymorph removal restores body and attributes: "+shape);
   Check(!u.HasFact(fourLegs)&&!HunterCMD061(attacker,u,CombatManeuver.Trip).AllBonuses.Any(m=>m.Fact?.Blueprint==fourLegs),"061 native polymorph removal revokes four-legged fact and actual Trip defense: "+shape);
   }finally{if(buff.IsAttached)buff.Remove();}
  }
  }finally{h.Unpatch(activate,boundary);h.Unpatch(deactivate,boundary);h.Unpatch(cmd,cmdScene);}
 }
 static void TotemCharge061(BlueprintsCache c){
  var owner=EquipmentUnit(c,15,HunterClass061);var enemy=EquipmentUnit(c,15);var buff=Add(owner,A<BlueprintBuff>(c,"TotemPowerfulChargeEffect01461"));var charge=Add(owner,N<BlueprintBuff>(c,"f36da144a379d534cad8e21667079066"));
  var melee=N<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>(c,"daf4ab765feba8548b244e174e7af5be").CreateEntity<Kingmaker.Items.ItemEntityWeapon>();
  foreach(string mode in new[]{"valid","late","opportunity","preview","no-charge"}){
   if(mode=="no-charge")charge.Remove();var attack=new RuleAttackWithWeapon(owner,enemy,melee,0){IsFirstAttack=mode!="late",IsAttackOfOpportunity=mode=="opportunity",IsCharge=mode!="no-charge"};var stats=new RuleCalculateWeaponStats(owner,melee,mode=="preview"?null:attack,null);stats.OnTrigger(new RulebookEventContext());var initial=stats.WeaponDamageDice.ModifiedValue;int modifier=stats.DamageModifiers.TotalValue;
   Kingmaker.PubSubSystem.RulebookEventBus.OnEventDidTrigger(stats);int factor=mode=="valid"?2:1;var actual=stats.WeaponDamageDice.ModifiedValue;Check(actual.Rolls==initial.Rolls*factor&&actual.Dice==initial.Dice&&stats.DamageModifiers.TotalValue==modifier,"061 actual native completed weapon stats double only the first melee charge dice without flat bonuses: "+mode);
   var damage=stats.DamageDescription.First().CreateDamage();Check(damage.Dice.ModifiedValue.Rolls==initial.Rolls*factor,"061 actual native damage creation consumes the modified final weapon dice: "+mode);Kingmaker.PubSubSystem.RulebookEventBus.OnEventDidTrigger(stats);Check(stats.WeaponDamageDice.ModifiedValue.Rolls==initial.Rolls*factor,"061 duplicate native completion notification cannot double charge dice again: "+mode);
  }
  buff.Remove();
 }
 static void TotemLeases061(BlueprintsCache c,Harmony h){
  foreach(int level in new[]{1,8,15,20}){
   var u=EquipmentUnit(c,level,HunterClass061);Grant(u,c,"TotemSharedStrengthFeature01461");if(level>=8)Grant(u,c,"TotemSharedStrength8Feature01461");if(level>=20)Grant(u,c,"TotemSharedStrength20Feature01461");var resource=A<BlueprintAbilityResource>(c,"TotemSharedStrengthResource01461");var armor=Toggle(u,c,"TotemArmor01461Toggle");var ability=A<BlueprintAbility>(c,"TotemSharedStrengthAbility01461");int ac=u.Stats.AC.ModifiedValue,points=u.Resources.GetResourceAmount(resource);
   ToggleOn(armor);Check(u.Stats.AC.ModifiedValue==ac&&u.Resources.GetResourceAmount(resource)==points,"061 native selected aspect is persistent and free but requires a paid lease: "+level);
   Super058(u,ability);var lease=u.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("TotemSharedStrengthLease01461"));var effect=u.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("TotemArmorEffect01461"));Check(u.Resources.GetResourceAmount(resource)==points-1&&lease.TimeLeft==TimeSpan.FromMinutes(1)&&u.Stats.AC.ModifiedValue==ac+(level>=8?2:1),"061 native paid Shared Strength applies exact natural armor for one minute: "+level);
   Check(effect.EndTime==lease.EndTime,"061 child effect inherits the exact paid lease expiry: "+level);PostLoad(armor.AppliedBuff,typeof(TotemStrengthMode01461));Check(u.Buffs.Enumerable.Count(b=>b.Blueprint==effect.Blueprint)==1&&u.Resources.GetResourceAmount(resource)==points-1,"061 actual mode postload retains one owned effect without charging: "+level);
   ToggleOff(armor);Check(u.Stats.AC.ModifiedValue==ac&&lease.IsAttached,"061 native aspect off removes its bonus and preserves already paid lease: "+level);ToggleOn(armor);Check(u.Stats.AC.ModifiedValue==ac+(level>=8?2:1)&&u.Resources.GetResourceAmount(resource)==points-1,"061 selecting aspect again inside paid minute neither repays nor extends lease: "+level);
   lease.Remove();Check(armor.IsOn&&u.Stats.AC.ModifiedValue==ac&&!u.Buffs.Enumerable.Any(b=>b.Blueprint==effect.Blueprint),"061 native lease removal immediately clears child effects and keeps only selected mode: "+level);ToggleOff(armor);
   if(level>=8){ToggleOn(armor);var speed=Toggle(u,c,"TotemSpeed01461Toggle");ToggleOn(speed);Check(armor.IsOn&&speed.IsOn,"061 real native two-aspect selection keeps both modes at or above level8");ToggleOff(armor);ToggleOff(speed);}
   var bite=Toggle(u,c,"TotemBite01461Toggle");Check(!bite.IsAvailableByRestrictions,"061 native bite aspect rejects a Hunter lacking the matching companion fact");
  }
 }
 static void TotemPet061(BlueprintsCache c){
  foreach(int level in new[]{1,7,8,14,15,20}){
   var master=EquipmentUnit(c,level,HunterClass061);var pet=EquipmentUnit(c,2);var part=pet.Ensure<UnitPartPet>();var masters=master.Ensure<UnitPartPetMaster>();Set(part,"Type",PetType.AnimalCompanion);Set(part,"m_MasterRef",new EntityPartRef<UnitEntityData,UnitPartPetMaster>(master));masters.Pets.Add(new EntityPartRef<UnitEntityData,UnitPartPet>(pet));
   var stats=new[]{StatType.SaveFortitude,StatType.SaveReflex,StatType.SaveWill,StatType.SkillAthletics,StatType.SkillMobility,StatType.SkillStealth,StatType.SkillPerception};var before=stats.Select(s=>pet.Stats.GetStat(s).ModifiedValue).ToArray();var grant=Grant(master,c,"TotemSharedStrengthFeature01461");int bonus=level>=15?3:level>=8?2:1;
   Check(pet.HasFact(A<BlueprintFeature>(c,"TotemCompanionStrengthFeature01461")),"061 actual native AddFeatureToPet grants passive strength to existing animal companion: "+level);
   Check(stats.Select((s,i)=>pet.Stats.GetStat(s).ModifiedValue-before[i]==bonus*(i<3?1:2)).All(v=>v),"061 real native companion ranks and seven stat components use master's Hunter level rather than pet HD: "+level);
   if(level==7||level==14){master.Progression.Classes[0].Level=level+1;Kingmaker.PubSubSystem.EventBus.RaiseEvent<Kingmaker.PubSubSystem.IOwnerGainLevelHandler>(master,v=>v.HandleUnitGainLevel());int next=level==7?2:3;Check(stats.Select((s,i)=>pet.Stats.GetStat(s).ModifiedValue-before[i]==next*(i<3?1:2)).All(v=>v)&&pet.Progression.Classes[0].Level==2,"061 actual master gain-level publisher updates companion bonuses across threshold without requiring pet HD gain: "+level);}
   master.Facts.Remove(grant);Check(stats.Select((s,i)=>pet.Stats.GetStat(s).ModifiedValue==before[i]).All(v=>v),"061 native master grant removal removes the pet's private strength and all seven bonuses: "+level);
  }
 }
}
