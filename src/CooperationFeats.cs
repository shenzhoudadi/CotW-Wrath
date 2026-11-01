using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype {
 internal static class CooperationFeats {
  internal const string ModuleName="Bonded spells and gambit feats";
  internal static readonly string[] Tokens={"BondedMindFeat","ShareSpellsFeat","BrokenWingGambitFeat","WoundedPawGambitFeat","StunningAssaultFeat"};
  internal static bool Has(UnitEntityData u,string token)=>TeamworkFeats.Has(u,token);
  static readonly Dictionary<string,BlueprintGuid> RuntimeIds=new Dictionary<string,BlueprintGuid>();
  internal static BlueprintGuid RuntimeId(string token){if(!RuntimeIds.TryGetValue(token,out var id))RuntimeIds.Add(token,id=Id(token));return id;}
  internal static bool Permanent(UnitEntityData u,string token){
   if(u==null)return false;var id=RuntimeId(token);var facts=u.Facts.List;
   // We are already traversing the owner's current facts. Rechecking Contains
   // here and hashing the token for every candidate made pointer targeting costly.
   for(int i=0;i<facts.Count;i++)if(facts[i] is Feature f&&f.Blueprint.AssetGuid==id&&!f.IsTemporary&&f.IsActive&&f.IsTurnedOn&&!f.IsDisposed)return true;
   return false;
  }
  internal static bool Enabled(UnitEntityData u,string token){if(u==null)return false;var id=RuntimeId(token);return u.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==id&&b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed);}
  internal static bool SharingCapability(UnitEntityData u)=>u!=null&&(u.State.Features.SharePersonalSpellsWithPets||u.Facts.List.Any(f=>T11TrainingOptions.Active(u,f)&&(CooperationCatalog.Familiars.Contains(f.Blueprint.AssetGuid.ToString())||f.Blueprint.GetComponents<AddPet>().Any(p=>p.Type==PetType.AnimalCompanion||(int)p.Type==FeralGraceFeat.MakingFriendsEidolonPetType))));
  static BlueprintActivatableAbility Toggle(PsychicAssets f,string token,string name,string text,BlueprintBuff buff){
   var a=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.Immediately,Group=ActivatableAbilityGroup.None,DeactivateIfCombatEnded=false,DoNotTurnOffOnRest=true},token,name,text);Set(a,"m_Buff",Ref<BlueprintBuffReference>(buff));return a;
  }
  static PrerequisiteFeature Need(BlueprintFeature feat){var p=new PrerequisiteFeature();Set(p,"m_Feature",Ref<BlueprintFeatureReference>(feat));return p;}
  internal static void RepairPool(BlueprintsCache cache,BlueprintFeatureSelection selection){
   if(cache==null||selection==null)return;
   var refs=(BlueprintFeatureReference[])Get(selection,"m_AllFeatures")??Array.Empty<BlueprintFeatureReference>();
   bool known=TeamworkFeats.PoolIds.Contains(selection.AssetGuid);
   bool clone=(selection.Group==FeatureGroup.TeamworkFeat||selection.Group2==FeatureGroup.TeamworkFeat)&&new[]{"422dab7309e1ad343935f33a4d6e9f11","6337b37f2a7c11b4ab0831d6780bce2a"}.All(id=>refs.Any(r=>r.Guid.ToString()==id));
   if(known||clone)T09CombatFeats.Append(new[]{selection},Tokens.Take(4).Select(t=>cache.Load(Id(t))as BlueprintFeature).Where(x=>x!=null).ToArray());
  }
  internal static void Register(BlueprintsCache cache){
   var f=new PsychicAssets(cache);var power=f.Native<BlueprintFeature>(T09CombatFeats.PowerAttack);
   foreach(string id in CooperationCatalog.Familiars)f.Native<BlueprintFeature>(id);
   const string bond="You and an ally who also permanently knows Bonded Mind can understand each other's nonverbal messages while you can see each other. This supplies the bond required by Share Spells; it grants no combat or skill bonus. Temporary teamwork sharing and Solo Tactics do not establish this bond.";
   const string share="Requires permanently learned Bonded Mind, caster level 1 and the ability to share spells with an animal companion, supported eidolon, familiar or phantom. This feat is always active: your personal-range spells from a spellbook can be cast on an allied creature that permanently knows Bonded Mind, at touch range. No toggle is needed. Self-casting remains available. Spell slots, casting time, caster level and duration are unchanged. Item spells, alchemical extracts and forced-owner abilities retain their own targeting. Caster-directed buffs in the initial spell delivery go to the selected ally; ongoing buff contexts retain their original caster.";
   const string wing="Requires 5 ranks in Persuasion. Enable Offer Gambit before a melee hit to give that opponent +2 on attack and weapon damage rolls against you until the end of your next turn (six seconds in real time) or its next weapon attack against you, hit or miss. That attack offers each eligible ally with Broken Wing Gambit an attack of opportunity. Normal threat, reach and opportunity-attack limits apply. Reactions cannot offer or trigger another gambit. Solo Tactics waives partner qualification for existing offers, but does not grant Offer Gambit to an ally who lacks it.";
   const string paw="Requires Broken Wing Gambit and 5 ranks in Persuasion. When an opponent accepts an ally's Broken Wing Gambit, you may instead make one ranged weapon attack against it within 30 feet as an immediate action. Enable Prefer Ranged Reaction to consent to spending the game's swift-action pool, including outside your turn. You receive only one gambit response per opening: this ranged attack or Broken Wing Gambit's opportunity attack. Normal ranged weapon readiness and target legality apply.";
   const string stun="Requires Strength 13, Power Attack and base attack bonus +16. Activate before attacking as a free action. Until your next turn (six seconds in real time), take -5 on all melee attack rolls and combat maneuver checks. Each melee weapon hit forces a Fortitude save, DC 10 + your base attack bonus; failure stuns for 1 round. Ranged attacks, misses and preview rolls do not force saves. The penalty cannot be toggled off early. Native stun immunities apply.";
   var shareBuff=f.Buff("BondedShareEnabled","Share Spells: Bonded Ally",share);
   var shareToggle=Toggle(f,"BondedShareToggle","Share Spells: Bonded Ally",share,shareBuff);
   // Keep both old identities and the existing AddFacts component for saves.
   // The hidden compatibility facts have no role in passive eligibility.
   shareToggle.HiddenInUI=true;shareToggle.ActionBarAutoFillIgnored=true;SetEnum(shareBuff,"m_Flags","HiddenInUi");
   var wingBuff=f.Buff("BrokenWingEnabled","Broken Wing Gambit: Offer Gambit",wing);
   var wingToggle=Toggle(f,"BrokenWingToggle","Broken Wing Gambit: Offer Gambit",wing,wingBuff);
   var pawBuff=f.Buff("WoundedPawEnabled","Wounded Paw Gambit: Prefer Ranged Reaction",paw);
   var pawToggle=Toggle(f,"WoundedPawToggle","Wounded Paw Gambit: Prefer Ranged Reaction",paw,pawBuff);
   var stunBuff=f.Buff("StunningAssaultArmed","Stunning Assault",stun,new StunningAssaultEffect());SetEnum(stunBuff,"m_Flags","RemoveOnRest");
   var arm=Apply(stunBuff,1,fromSpell:false);arm.IsNotDispelable=true;
   var stunAbility=f.Ability("StunningAssaultAbility","Stunning Assault",stun,AbilityType.Extraordinary,UnitCommand.CommandType.Free,AbilityRange.Personal,Run(arm));stunAbility.NotOffensive=true;
   var bonded=f.Feature(Tokens[0],"Bonded Mind",bond);
   var shared=f.Feature(Tokens[1],"Share Spells",share,new PrerequisiteBondedSharing(),Facts(shareToggle),new SpellFeatCleanup{Buffs=new[]{Ref<BlueprintBuffReference>(shareBuff)}});
   var broken=f.Feature(Tokens[2],"Broken Wing Gambit",wing,new PrerequisiteStatValue{Stat=StatType.SkillPersuasion,Value=5},new BrokenWingGambit(),Facts(wingToggle),new SpellFeatCleanup{Buffs=new[]{Ref<BlueprintBuffReference>(wingBuff)}});
   var wounded=f.Feature(Tokens[3],"Wounded Paw Gambit",paw,Need(broken),new PrerequisiteStatValue{Stat=StatType.SkillPersuasion,Value=5},Facts(pawToggle),new SpellFeatCleanup{Buffs=new[]{Ref<BlueprintBuffReference>(pawBuff)}});
   var stunning=f.Feature(Tokens[4],"Stunning Assault",stun,Need(power),new PrerequisiteStatValue{Stat=StatType.Strength,Value=13},new PrerequisiteStatValue{Stat=StatType.BaseAttackBonus,Value=16},Facts(stunAbility),new SpellFeatCleanup{Buffs=new[]{Ref<BlueprintBuffReference>(stunBuff)}});
   var roots=new[]{bonded,shared,broken,wounded,stunning};
   for(int i=0;i<5;i++)roots[i].Groups=i<2?new[]{FeatureGroup.Feat,FeatureGroup.TeamworkFeat}:i<4?new[]{FeatureGroup.Feat,FeatureGroup.TeamworkFeat,FeatureGroup.CombatFeat}:new[]{FeatureGroup.Feat,FeatureGroup.CombatFeat};
   CooperationIcons.Apply(f);
   SpellResourceFeats.Commit(cache,f,roots);foreach(var id in TeamworkFeats.PoolIds)RepairPool(cache,cache.Load(id)as BlueprintFeatureSelection);CombatFeatPoolRepair.Reconcile(cache);
   CompanionSpellSharing.Register(cache);
  }
 }
 [TypeId("f05f1f7e80bb4c97a98cac842b22b5e8")]
 public sealed class PrerequisiteBondedSharing:Prerequisite {
  protected override bool CheckInternal(FeatureSelectionState selectionState,UnitDescriptor unit,LevelUpState state)=>unit!=null&&CooperationFeats.Permanent(unit.Unit,"BondedMindFeat")&&unit.Spellbooks.Any(b=>b.CasterLevel>=1)&&CooperationFeats.SharingCapability(unit.Unit);
  protected override string GetUITextInternal(UnitDescriptor unit)=>"Permanently learned Bonded Mind, caster level 1, and companion/eidolon/familiar/phantom spell sharing";
 }
}
