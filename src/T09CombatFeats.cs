using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker;
using Kingmaker.Controllers.Units;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Controllers.Combat;
using TurnBased.Controllers;
using Kingmaker.TurnBasedMode.Controllers;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.UI.Common;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class T09CombatFeats
    {
        internal const string ModuleName = "T09 combat feats";
        internal const string Dodge = "97e216dbb46ae3c4faef90cf6bbe6fd5";
        internal const string PowerAttack = "9972f33f977fc724c838e59641b2fca5";
        internal const string ImprovedTrip = "0f15c6f70d8fb2b49aa6cc24239cc5fa";
        internal static readonly string[] Tokens = { "ArtfulDodgeFeat", "OsyluthGuileFeat", "FellingSmashFeat" };
        internal static readonly string[] DefensiveBuffs = { "6ffd93355fb3bcf4592a5d976b1d32a9", "e81cd772a7311554090e413ea28ceea1" };
        const string GuileDescription="Choose one visible opponent as a free action. The selection and its visible marker persist until that opponent dies or you choose another opponent. While fighting defensively or using Combat Expertise, add your positive Charisma modifier as dodge AC against that opponent's visible melee attacks. Requires Dodge or Artful Dodge and 8 ranks in Persuasion.";
        const string SmashDescription="When enabled, a hit with a single standard melee attack at full base attack bonus while using Power Attack spends your available swift action to attempt a trip. Full attacks, charges, opportunity attacks, misses and bonus attacks do not trigger it. The normal trip maneuver retains its immunities and checks. Requires Intelligence 13, Strength 13, base attack bonus +6, Combat Expertise, Improved Trip and Power Attack. Ordinary attack commands and native Vital Strike qualify; other separate attack abilities do not.";
        internal static BlueprintFeature Require(BlueprintsCache cache, string id) => cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeature ?? throw new InvalidOperationException("T09 feat dependency missing: " + id);
        static PrerequisiteStatValue Stat(StatType stat,int value) => new PrerequisiteStatValue { Stat=stat,Value=value };
        static PrerequisiteFeature Need(BlueprintFeature feature) { var p=new PrerequisiteFeature();Set(p,"m_Feature",Ref<BlueprintFeatureReference>(feature));return p; }
        internal static void Register(BlueprintsCache cache)
        {
            var selections = new[]{BlueprintRegistration.BasicFeatsId}.Concat(BlueprintRegistration.NativeCombatFeatSelectionIds)
                .Select(id=>cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection ?? throw new InvalidOperationException("T09 selection missing: "+id)).ToArray();
            var dodge=Require(cache,Dodge);var power=Require(cache,PowerAttack);var trip=Require(cache,ImprovedTrip);var expertise=Require(cache,BlueprintRegistration.CombatExpertiseId);
            var f=new PsychicAssets(cache);
            var artful=f.Feature(Tokens[0],"Artful Dodge","When you are the only character threatening an opponent, gain +1 dodge AC against that opponent. Counts as Dodge for feat prerequisites. You may use Intelligence in place of Dexterity for minimum Dexterity prerequisites. Requires Intelligence 13.",
                Stat(StatType.Intelligence,13),new ReplaceStatForPrerequisites { OldStat=StatType.Dexterity,NewStat=StatType.Intelligence,Policy=ReplaceStatForPrerequisites.StatReplacementPolicy.NewStat },new ArtfulDodgeDefense());
            Set(artful,"m_Icon",PsychicOriginalIcons.Load("ArtfulDodge"));
            var choose=f.Ability("OsyluthGuileChoose","Osyluth Guile: Select Opponent",GuileDescription,AbilityType.Extraordinary,UnitCommand.CommandType.Free,AbilityRange.Long,Run(new OsyluthGuileChoose()));
            choose.CanTargetSelf=choose.CanTargetFriends=false;choose.CanTargetEnemies=true;Set(choose,"m_Icon",PsychicOriginalIcons.Load("OsyluthGuile"));choose.LocalizedDuration=Text("OsyluthGuileChoose","duration","Until the opponent dies or another opponent is selected");choose.LocalizedSavingThrow=Text("OsyluthGuileChoose","savingThrow","");
            var marker=f.Buff("OsyluthGuileTargetMarkerBuff","Osyluth Guile: Selected Opponent","This opponent has been selected for Osyluth Guile. The selecting character gains their Osyluth Guile defense only under that feat's normal defensive stance and visibility conditions. The marker ends when the opponent dies, another opponent is selected, or the selecting character loses the feat.");
            marker.Stacking=StackingType.Stack;Set(marker,"m_Icon",choose.Icon);SetEnum(marker,"m_Flags","0");
            var guile=f.Feature(Tokens[1],"Osyluth Guile",GuileDescription,Stat(StatType.SkillPersuasion,8),Need(dodge),Facts(choose),new OsyluthGuileDefense());Set(guile,"m_Icon",choose.Icon);
            var buff=f.Buff("FellingSmashModeBuff","Felling Smash",SmashDescription,new FellingSmashTrigger());Set(buff,"m_Icon",trip.Icon);
            var toggle=f.Add(new BlueprintActivatableAbility { ActivationType=AbilityActivationType.WithUnitCommand,DeactivateImmediately=true,IsOnByDefault=false,ComponentsArray=Array.Empty<BlueprintComponent>() },"FellingSmashToggle","Felling Smash",SmashDescription);
            Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(buff));Set(toggle,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free);Set(toggle,"m_Icon",trip.Icon);
            var smash=f.Feature(Tokens[2],"Felling Smash",SmashDescription,Stat(StatType.Intelligence,13),Stat(StatType.Strength,13),Stat(StatType.BaseAttackBonus,6),Need(expertise),Need(trip),Need(power),Facts(toggle));Set(smash,"m_Icon",trip.Icon);
            foreach(var feat in new[]{artful,guile,smash})feat.Groups=new[]{FeatureGroup.Feat,FeatureGroup.CombatFeat};
            foreach(var bp in f.Assets.Values)PsychicRepairComponents.UniqueNames(bp);
            var actual=new Dictionary<BlueprintGuid,BlueprintScriptableObject>();
            foreach(var bp in f.Assets.Values) {
                var old=cache.Load(bp.AssetGuid);
                if(old!=null&&(old.name!=bp.name||old.GetType()!=bp.GetType()))throw new InvalidOperationException("T09 GUID collision; foreign asset preserved: "+bp.AssetGuid);
                if(old is BlueprintScriptableObject owned && (owned.ComponentsArray==null||bp.ComponentsArray.Any(expected=>!owned.ComponentsArray.Any(component=>component!=null&&component.GetType()==expected.GetType()&&component.name==expected.name))))throw new InvalidOperationException("T09 component identity differs; preserving asset: "+bp.AssetGuid);
                actual[bp.AssetGuid]=(BlueprintScriptableObject)(old??bp);
            }
            // Repair staged references to existing owned objects on idempotent registration.
            foreach(var bp in f.Assets.Values)foreach(var c in bp.ComponentsArray.OfType<AddFacts>())
                Set(c,"m_Facts",c.Facts.Select(x=>Ref<BlueprintUnitFactReference>((BlueprintUnitFact)actual[x.AssetGuid])).ToArray());
            Set(toggle,"m_Buff",Ref<BlueprintBuffReference>((BlueprintBuff)actual[buff.AssetGuid]));
            f.Enable();var added=new List<BlueprintGuid>();
            var previous=selections.Select(s=>new[]{Get(s,"m_Features"),Get(s,"m_AllFeatures")}).ToArray();
            try {
                foreach(var bp in f.Assets.Values)if(cache.Load(bp.AssetGuid)==null){cache.AddCachedBlueprint(bp.AssetGuid,bp);added.Add(bp.AssetGuid);}
                Append(selections,new[]{artful,guile,smash}.Select(x=>(BlueprintFeature)actual[x.AssetGuid]).ToArray());
            }catch{for(int i=0;i<selections.Length;i++){Set(selections[i],"m_Features",previous[i][0]);Set(selections[i],"m_AllFeatures",previous[i][1]);}foreach(var id in added)cache.RemoveCachedBlueprint(id);throw;}
        }
        internal static void Append(IEnumerable<BlueprintFeatureSelection> selections,BlueprintFeature[] feats) {
            foreach(var selection in selections)foreach(string field in new[]{"m_Features","m_AllFeatures"}) {
                var old=(BlueprintFeatureReference[])Get(selection,field)??Array.Empty<BlueprintFeatureReference>();
                Set(selection,field,old.Concat(feats.Where(f=>!old.Any(x=>x?.Guid==f.AssetGuid)).Select(Ref<BlueprintFeatureReference>)).ToArray());
            }
        }
        internal static void Optional(BlueprintsCache cache) {
            var selection=cache.Load(BlueprintGuid.Parse(BlueprintRegistration.BrawlerCombatFeatSelectionId)) as BlueprintFeatureSelection;
            var feats=Tokens.Select(t=>cache.Load(Id(t)) as BlueprintFeature).ToArray();
            if(selection!=null&&feats.All(x=>x!=null))Append(new[]{selection},feats);
        }
        internal static bool HasArtful(UnitDescriptor unit,FeatureSelectionState selection) {
            var feat=ResourcesLibrary.BlueprintsCache.Load(Id(Tokens[0])) as BlueprintFeature;
            return feat!=null&&unit?.Progression?.Features.HasFact(feat)==true&&(selection==null||!selection.IsSelectedInChildren(feat));
        }
        internal static bool IsDefensive(UnitEntityData unit)=>unit.Buffs.Enumerable.Any(b=>b.IsTurnedOn&&!b.IsSuppressed&&DefensiveBuffs.Contains(b.Blueprint.AssetGuid.ToString()));
    }
    [TypeId("ef81bb4a261440e3975b05222f19569b")]
    public sealed class ArtfulDodgeDefense:UnitFactComponentDelegate,ITargetRulebookHandler<RuleCalculateAC>,ISubscriber {
        public void OnEventAboutToTrigger(RuleCalculateAC e) {
            if(Owner.CombatState.IsEngage(e.Initiator)&&e.Initiator.CombatState.EngagedBy.Count==1)e.AddModifier(1,Fact,ModifierDescriptor.Dodge);
        }
        public void OnEventDidTrigger(RuleCalculateAC e){}
    }
    [HarmonyPatch(typeof(PrerequisiteFeature),"CheckInternal")]
    internal static class ArtfulDodgeFeaturePrerequisitePatch {
        static void Postfix(PrerequisiteFeature __instance,UnitDescriptor unit,FeatureSelectionState selectionState,ref bool __result) {
            if(!__result&&__instance.Feature?.AssetGuid==BlueprintGuid.Parse(T09CombatFeats.Dodge)&&T09CombatFeats.HasArtful(unit,selectionState))__result=true;
        }
    }
    [HarmonyPatch(typeof(PrerequisiteFeaturesFromList),"CheckInternal")]
    internal static class ArtfulDodgeListPrerequisitePatch {
        static void Postfix(PrerequisiteFeaturesFromList __instance,UnitDescriptor unit,FeatureSelectionState selectionState,ref bool __result) {
            if(!T09CombatFeats.HasArtful(unit,selectionState)||!__instance.Features.Any(f=>f?.AssetGuid==BlueprintGuid.Parse(T09CombatFeats.Dodge)))return;
            int count=__instance.Features.Count(f=>f!=null&&(selectionState==null||!selectionState.IsSelectedInChildren(f))&&(unit.HasFact(f)||f.AssetGuid==BlueprintGuid.Parse(T09CombatFeats.Dodge)));
            bool restrict=(bool)Get(__instance,"m_RestrictIfNot");__result=(count>=__instance.Amount)!=restrict;
        }
    }
    public sealed class OsyluthGuileState {
        [JsonProperty] public UnitEntityData Target;
        // Keep the old serialized member so 0.14.0 saves retain their identity.
        // Expiry is no longer used to clear a living selected opponent.
        [JsonProperty] public TimeSpan Until;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public string MarkerId;
    }
    [TypeId("a314b3a2ca2d4e9ca329e8979074de38")]
    public sealed class OsyluthGuileDefense:UnitFactComponentDelegate<OsyluthGuileState>,ITargetRulebookHandler<RuleCalculateAC>,ITickEachRound,IUnitLifeStateChanged,IGlobalSubscriber,ISubscriber {
        static readonly BlueprintGuid MarkerGuid=Id("OsyluthGuileTargetMarkerBuff");
        internal void Choose(UnitEntityData target) {
            if(target==null||target.IsDisposed||target.State.IsDead)return;
            if(!ReferenceEquals(Data.Target,target))ClearTarget();
            Data.Target=target;Data.Until=TimeSpan.MaxValue;EnsureMarker();
        }
        internal void ClearTarget(){
            var target=Data.Target;Data.Target=null;Data.MarkerId=null;Data.Until=TimeSpan.Zero;
            if(target==null||target.IsDisposed)return;
            foreach(var b in target.Buffs.Enumerable.Where(b=>b.Blueprint.AssetGuid==MarkerGuid&&ReferenceEquals(b.Context?.MaybeCaster,Owner)).ToArray())b.Remove();
        }
        bool ValidateTarget(){if(Data.Target==null)return false;if(Data.Target.IsDisposed||Data.Target.State.IsDead){ClearTarget();return false;}return true;}
        internal void EnsureMarker(){
            if(!ValidateTarget())return;
            var target=Data.Target;
            var old=target.Buffs.Enumerable.FirstOrDefault(b=>b.Blueprint.AssetGuid==MarkerGuid&&ReferenceEquals(b.Context?.MaybeCaster,Owner));
            if(old!=null){Data.MarkerId=old.UniqueId;old.IsNotDispelable=true;return;}
            var bp=ResourcesLibrary.BlueprintsCache.Load(MarkerGuid) as BlueprintBuff;
            if(bp==null)return;
            var context=Context;
            if(!ReferenceEquals(context?.MaybeCaster,Owner))context=new MechanicsContext(Owner,Owner.Descriptor,Fact.Blueprint,null,new TargetWrapper(target));
            var added=target.AddBuff(bp,context,null);
            if(added!=null){added.IsNotDispelable=true;Data.MarkerId=added.UniqueId;}
        }
        public void OnNewRound(){EnsureMarker();}
        protected override void OnActivate(){EnsureMarker();}
        protected override void OnDeactivate(){ClearTarget();}
        public void HandleUnitLifeStateChanged(UnitEntityData unit,UnitLifeState oldState){if(ReferenceEquals(unit,Data.Target)&&unit.State.IsDead)ClearTarget();}
        public void OnEventAboutToTrigger(RuleCalculateAC e) {
            if(!ValidateTarget())return;
            var target=Data.Target;
            if(!ReferenceEquals(target,e.Initiator)||!e.AttackType.IsMelee()||!T09CombatFeats.IsDefensive(Owner)||Owner.State.HasCondition(UnitCondition.Blindness)||!Owner.HasLOS(target)||UnitPartConcealment.Calculate(Owner,target)==Concealment.Total)return;
            e.AddModifier(Math.Max(0,Owner.Stats.Charisma.Bonus),Fact,ModifierDescriptor.Dodge);
        }
        public void OnEventDidTrigger(RuleCalculateAC e){}
    }
    [TypeId("20dfe1b8d1bf48978d9fec8f4bb090ab")]
    public sealed class OsyluthGuileChoose:ContextAction {
        public override string GetCaption()=>"Choose one opponent for Osyluth Guile";
        public override void RunAction() {
            var owner=Context.MaybeCaster;var fact=owner?.Facts.List.FirstOrDefault(x=>x.Blueprint.AssetGuid==Id(T09CombatFeats.Tokens[1])&&x.IsTurnedOn);
            var runtime=fact?.Components.FirstOrDefault(x=>x.SourceBlueprintComponent is OsyluthGuileDefense);
            if(runtime!=null&&Target.Unit!=null)using(runtime.RequestEventContext())((OsyluthGuileDefense)runtime.SourceBlueprintComponent).Choose(Target.Unit);
        }
    }
    [TypeId("2813a443bdcc423697c8f50ed3278fae")]
    public sealed class FellingSmashTrigger:UnitBuffComponentDelegate,IInitiatorRulebookHandler<RuleAttackWithWeapon>,ISubscriber {
        public void OnEventAboutToTrigger(RuleAttackWithWeapon e){}
        internal static bool Eligible(RuleAttackWithWeapon e)=>e.Weapon!=null&&e.Weapon.Blueprint.IsMelee&&e.AttackRoll?.IsHit==true&&!e.IsCharge&&!e.IsFullAttack&&!e.IsAttackOfOpportunity&&!e.ExtraAttack&&!e.AttackRoll.IsFake&&e.AttackNumber==0&&e.AttackBonusPenalty==0;
        internal static bool IsAttackAction(UnitCommand command,RuleAttackWithWeapon e) {
            if(command?.Type!=UnitCommand.CommandType.Standard)return false;
            if(command is UnitAttack attack)return e.AttacksCount==1&&ReferenceEquals(attack.LastAttackRule,e);
            // Native Vital Strike is an attack action, but its delivery rule has
            // no attack-list count. Require the exact current ability and first
            // rule so Cleave, spell attacks and unrelated procs stay excluded.
            if(command is UnitUseAbility use)return use.Ability?.Blueprint.GetComponent<AbilityCustomVitalStrike>()!=null&&ReferenceEquals(e.Reason?.Ability,use.Ability)&&e.IsFirstAttack&&e.AttacksCount<=1;
            return false;
        }
        internal static void SpendSwift(UnitEntityData owner,BlueprintBuff source){
            owner.SpendAction(UnitCommand.CommandType.Swift,false,0f);
            // Native SpendAction skips units before combat has started. Keep the
            // previously delivered opening-attack payment, without touching UI
            // belonging to an inactive/other turn.
            if(!owner.IsInCombat){owner.CombatState.Cooldown.SwiftAction=6f;return;}
            if(!CombatController.IsInTurnBasedCombat())return;
            var turn=Game.Instance.TurnBasedCombatController?.CurrentTurn;
            if(turn==null||(!ReferenceEquals(turn.Rider,owner)&&!ReferenceEquals(turn.Mount,owner)))return;
            var swift=turn.GetActionsStates(owner)?.Swift;
            if(swift==null)return;
            swift.SetPrediction(CombatAction.UsageType.UseAbility,CombatAction.ActivityType.Ability,CombatAction.ActivityState.WillBeUsed,source);
            swift.UpdateCurrentStates(false,true);
            EventBus.RaiseEvent<IActionsPredictionHandler>(h=>h.PredictionChanged());
        }
        public void OnEventDidTrigger(RuleAttackWithWeapon e) {
            // A proc during another standard action is not itself an attack action.
            var command=Owner.Commands?.Standard;
            if(Buff.IsSuppressed||!Buff.IsTurnedOn||!Eligible(e)||!Owner.State.CanAct||!IsAttackAction(command,e)||Owner.Commands.Swift!=null||Owner.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Swift)||!Owner.Buffs.Enumerable.Any(b=>b.IsTurnedOn&&!b.IsSuppressed&&b.Blueprint.AssetGuid==BlueprintGuid.Parse("5898bcf75a0942449a5dc16adc97b279")))return;
            SpendSwift(Owner,Buff.Blueprint);
            Context.TriggerRule(new RuleCombatManeuver(Owner,e.Target,CombatManeuver.Trip){Reason=new Kingmaker.RuleSystem.RuleReason(Context)});
        }
    }
}
