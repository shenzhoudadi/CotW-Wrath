using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Designers;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using UnityEngine;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype {
 internal static class TeamworkFeats {
  internal const string ModuleName="Teamwork feats";
  internal static readonly string[] Tokens={"CoordinatedShotFeat","DistractingChargeFeat","TargetOfOpportunityFeat","SwarmScatterFeat"};
  internal static readonly BlueprintGuid[] PoolIds=TeamworkCatalog.Pools.Select(BlueprintGuid.Parse).ToArray();
  internal static bool Has(UnitEntityData unit,string token)=>unit?.Facts.List.Any(f=>f.Blueprint.AssetGuid==Id(token)&&T11TrainingOptions.Active(unit,f))==true;
  internal static bool Partner(UnitEntityData owner,UnitEntityData ally,string token)=>ally!=null&&ally!=owner&&ally.IsAlly(owner)&&!ally.State.IsDead&&(owner.State.Features.SoloTactics||Has(ally,token));
  internal static UnitEntityData[] Nearby(UnitEntityData owner,float radius)=>GameHelper.GetTargetsAround(owner.Position,radius,false,false).Where(u=>u!=null&&u!=owner).ToArray();
  // Match CotW's seven-foot adjacency in this continuous-position engine.
  internal static int Adjacent(UnitEntityData owner)=>Nearby(owner,2.1336f).Count(u=>Partner(owner,u,Tokens[3])&&Vector3.Distance(owner.Position,u.Position)<=2.1336f);
  internal static bool Covers(UnitEntityData owner,UnitEntityData target,UnitEntityData ally){
   var ray=target.Position-owner.Position;float length=ray.sqrMagnitude;if(length<=0)return false;
   float along=Vector3.Dot(ally.Position-owner.Position,ray)/length;
   return along>0&&along<1&&Vector3.Distance(ally.Position,owner.Position+ray*along)<ally.Corpulence;
  }
  internal static void RepairPool(BlueprintsCache cache,BlueprintFeatureSelection selection){
   if(cache==null||selection==null)return;
   var refs=(BlueprintFeatureReference[])Get(selection,"m_AllFeatures")??Array.Empty<BlueprintFeatureReference>();
   bool known=PoolIds.Contains(selection.AssetGuid);
   bool clone=(selection.Group==FeatureGroup.TeamworkFeat||selection.Group2==FeatureGroup.TeamworkFeat)&&
    new[]{"422dab7309e1ad343935f33a4d6e9f11","6337b37f2a7c11b4ab0831d6780bce2a"}.All(id=>refs.Any(r=>r.Guid.ToString()==id));
   if(!known&&!clone)return;
   var feats=Tokens.Select(t=>cache.Load(Id(t)) as BlueprintFeature).Where(f=>f!=null&&f.HasGroup(FeatureGroup.TeamworkFeat)).ToArray();
   if(feats.Length!=0)T09CombatFeats.Append(new[]{selection},feats);
  }
  internal static void Register(BlueprintsCache cache){
   var f=new PsychicAssets(cache);var shot=f.Native<BlueprintFeature>("0da0c194d6e1d43419eb8d990b28e0ab");
   var pools=TeamworkCatalog.Pools.Select(id=>f.Native<BlueprintFeatureSelection>(id)).ToArray();
   const string cs="Ranged attacks gain +1 against an opponent threatened by an ally with Coordinated Shot who is not providing cover along the shot. The bonus is +2 if that opponent is flanked. The best eligible ally applies once. Solo Tactics waives the ally's feat requirement, not positioning. Requires Point-Blank Shot.";
   const string dc="When an ally with Distracting Charge hits an opponent with a charge attack, your next attack roll against that opponent gains +2. The opening expires at that ally's next turn (six seconds in real time); the next actual attack consumes it even on a miss. Solo Tactics waives the ally's feat requirement. Multiple openings do not stack on the same attack.";
   const string to="When an ally with Target of Opportunity hits an opponent with a ranged weapon attack, you may make one ranged attack against that opponent within 30 feet. Turn on the persistent Allow Reaction toggle to consent to automatic use of this game's swift-action pool (including outside your turn); the expenditure blocks another reaction or swift ability until your pool refreshes. Your primary ranged weapon must be ready, and you must be able to attack the target. Reaction attacks cannot trigger another Target of Opportunity reaction. Solo Tactics waives the ally's feat requirement. Requires Point-Blank Shot and base attack bonus +6.";
   const string ss="For each adjacent ally with Swarm Scatter, gain a +1 circumstance bonus to AC. Adjacency uses CotW's seven-foot radius in the game's continuous-position system. While at least one such ally is present, you are immune to rat-swarm attack damage and distraction, but not disease or unrelated attacks/spells. Solo Tactics waives the ally's feat requirement. The AC bonus follows movement and ends when no eligible ally remains.";
   var enabled=f.Buff("TargetOpportunityEnabled","Target of Opportunity: Allow Reaction",to,new TargetOpportunityReaction());
   var toggle=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.Immediately,Group=ActivatableAbilityGroup.None,DeactivateIfCombatEnded=false,DoNotTurnOffOnRest=true},"TargetOpportunityToggle","Target of Opportunity: Allow Reaction",to);
   Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(enabled));
   PrerequisiteFeature NeedShot(){var c=new PrerequisiteFeature();Set(c,"m_Feature",Ref<BlueprintFeatureReference>(shot));return c;}
   var roots=new[]{
    f.Feature(Tokens[0],"Coordinated Shot",cs,NeedShot(),new CoordinatedShotBonus()),
    f.Feature(Tokens[1],"Distracting Charge",dc,new DistractingChargeOpening()),
    f.Feature(Tokens[2],"Target of Opportunity",to,NeedShot(),new PrerequisiteStatValue{Stat=StatType.BaseAttackBonus,Value=6},Facts(toggle),new SpellFeatCleanup{Buffs=new[]{Ref<BlueprintBuffReference>(enabled)}}),
    f.Feature(Tokens[3],"Swarm Scatter",ss,new SwarmScatterDefense())
   };
   for(int i=0;i<roots.Length;i++)roots[i].Groups=i==3?new[]{FeatureGroup.Feat,FeatureGroup.TeamworkFeat}:new[]{FeatureGroup.Feat,FeatureGroup.CombatFeat,FeatureGroup.TeamworkFeat};
   foreach(var bp in f.Assets.Values)Set(bp,"m_Icon",shot.Icon);
   // Root pool transaction is shared with previous feat registrations.
   SpellResourceFeats.Commit(cache,f,roots);
   foreach(var pool in pools)RepairPool(cache,pool);
   CombatFeatPoolRepair.Reconcile(cache);
  }
 }
}
