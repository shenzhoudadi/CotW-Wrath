using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class T11TrainingOptions
    {
        internal const string ModuleName="T11 training and arcana";
        internal const string Fighter="48ac8db94d5de7645906c7d0ad3bcfbd";
        internal const string Magus="45a4607686d96a1498891b3286121780";
        internal const string Armor="3c380607706f209499d951b29d3c44f3";
        internal const string TrainingPool="b8cecf4e5e464ad41b79d5b42b76b399";
        internal const string MagusPool="e9dc4dfc73eaaf94aae27e0ed6cc9ada";
        internal const string ScionPool="d4b54d9db4932454ab2899f931c2042c";
        internal const string Familiar="363cab72f77c47745bf3a8807074d183";
        internal const string Demoralize="7d2233c3b7a0b984ba058a83b736e6ac";
        internal const string Display="5f3126d4120b2b244a95cb2ec23d69fb";
        internal static readonly string[] AdvancedWeaponPools={"3aa4cbdd4af5ba54888b0dc7f07f80c4","70a139f0a4c6c534eaa34feea0d08622","ee9ab0117ca06b84f9c66469f4428c61","0b55d725ded1ae549bb858fba1d84114","538bc73900c2425b93677cc02a3048b1"};
        internal static readonly string[] AdvancedArmorPools={"5bf17c2ae9ee4615b27c993629ed9bc3","c1e7a208b5a544f58071af88a61ab842","7743a11f841449b4935224ac1de82edd","59cb9654de7147399b4d6a384d32dd3a","67eb66ec98754e228a6b4633a10327fa","bae7b370c4e8427c939a30e7133501c7","80246fa3dfe14dea94ec6e6640900083"};
        internal static readonly string[] FeatTokens={"DazzlingIntimidationFeat","VersatileTrainingSelection","AdaptableTrainingSelection"};
        internal static readonly Dictionary<StatType,string[]> Skills=new Dictionary<StatType,string[]> {
            {StatType.SkillAthletics,new[]{"Axes","Natural","Spears","Hammers"}},
            {StatType.SkillLoreNature,new[]{"Axes","Spears"}},
            {StatType.SkillPerception,new[]{"Bows","Close","Crossbows","Double","Polearms","Thrown"}},
            {StatType.SkillStealth,new[]{"Close","BladesLight","Crossbows"}},
            {StatType.SkillMobility,new[]{"Double","Monk","Thrown"}},
            {StatType.SkillPersuasion,new[]{"Hammers","BladesHeavy","BladesLight","Polearms"}}
        };
        internal static string SkillName(StatType s)=>s.ToString().Substring("Skill".Length).Replace("LoreNature","Lore (Nature)");
        internal static Prerequisite Need(BlueprintFeature bp,bool not=false) {
            var p=not?(Prerequisite)new PrerequisiteNoFeature():new PrerequisiteFeature();
            Set(p,"m_Feature",Ref<BlueprintFeatureReference>(bp));return p;
        }
        internal static PrerequisiteClassLevel Level(BlueprintCharacterClass c,int level) {
            var p=new PrerequisiteClassLevel{Level=level};Set(p,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(c));return p;
        }
        static PrerequisiteFeaturesFromList Any(IEnumerable<BlueprintFeature> facts) {
            var p=new PrerequisiteFeaturesFromList{Amount=1};Set(p,"m_Features",facts.Select(Ref<BlueprintFeatureReference>).ToArray());return p;
        }
        static BlueprintFeatureSelection Selection(PsychicAssets f,string token,string name,string description,FeatureGroup group,int ranks=1) {
            return f.Add(new BlueprintFeatureSelection{Ranks=ranks,Groups=new[]{group},ComponentsArray=Array.Empty<BlueprintComponent>()},token,name,description);
        }
        internal static void Register(BlueprintsCache cache) {
            var f=new PsychicAssets(cache);
            var fighter=f.Native<BlueprintCharacterClass>(Fighter);
            var magus=f.Native<BlueprintCharacterClass>(Magus);
            var armor=f.Native<BlueprintFeature>(Armor);
            var training=f.Native<BlueprintFeatureSelection>(TrainingPool);
            var pools=new[]{f.Native<BlueprintFeatureSelection>(MagusPool),f.Native<BlueprintFeatureSelection>(ScionPool)};
            var nativeFamiliar=f.Native<BlueprintFeatureSelection>(Familiar);
            var display=f.Native<BlueprintAbility>(Display);f.Native<BlueprintAbility>(Demoralize);
            var groups=training.AllFeatures.Where(x=>x.GetComponent<WeaponGroupAttackBonus>()!=null).ToArray();
            if(groups.Length<13)throw new InvalidOperationException("T11 native weapon training groups incomplete.");
            const string rankDescription="Use your total base attack bonus in place of ranks in the selected skill, keeping its ability modifier and other bonuses or penalties. The skill becomes a class skill. Previously allocated ranks are refunded for redistribution in the level-up skill step; no more skill points can be invested in this skill while trained. Existing characters receive the refund at their next normal level-up or retraining. You need not wield a trained weapon or wear armor for this rank substitution.";
            var dazzling=f.Feature("DazzlingIntimidationFeat","Dazzling Intimidation","While wielding a weapon for which you have weapon training, add that training bonus to Intimidate checks. Demoralize takes a move action, and Dazzling Display takes a standard action instead of a full-round action. This does not grant Dazzling Display or speed up unrelated abilities. Requires fighter level 5 and weapon training.",Level(fighter,5),Any(groups),new DazzlingTrainingBonus());
            dazzling.Groups=new[]{FeatureGroup.WeaponTraining};Set(dazzling,"m_Icon",display.Icon);
            var versatile=Selection(f,"VersatileTrainingSelection","Versatile Training",rankDescription+" Choose a skill associated with a weapon group in which you have training. Each skill is available once; this option may be selected again for a different skill.",FeatureGroup.Feat,3);
            versatile.Groups=new[]{FeatureGroup.WeaponTraining};versatile.ComponentsArray=new BlueprintComponent[]{Level(fighter,5),Any(groups)};
            var adaptable=Selection(f,"AdaptableTrainingSelection","Adaptable Training",rankDescription+" Choose Athletics, Mobility or Persuasion. Each skill is available once; this option may be selected again for a different skill. Requires fighter level 3 and armor training.",FeatureGroup.Feat,3);
            adaptable.Groups=Array.Empty<FeatureGroup>();adaptable.ComponentsArray=new BlueprintComponent[]{Level(fighter,3),Need(armor)};
            var v=new Dictionary<StatType,BlueprintFeature>();var a=new Dictionary<StatType,BlueprintFeature>();
            foreach(var entry in Skills) {
                var eligible=groups.Where(bp=>entry.Value.Contains(bp.GetComponent<WeaponGroupAttackBonus>().WeaponGroup.ToString())).ToArray();
                if(eligible.Length==0)throw new InvalidOperationException("T11 no native weapon group for skill "+entry.Key);
                var feature=f.Feature("VersatileTraining"+entry.Key,"Versatile Training: "+SkillName(entry.Key),rankDescription,new AddClassSkill{Skill=entry.Key},new TrainingSkillRanks{Skill=entry.Key},Any(eligible));
                feature.ComponentsArray=feature.ComponentsArray.Concat(new BlueprintComponent[]{Need(feature,true)}).ToArray();v.Add(entry.Key,feature);
            }
            foreach(var skill in new[]{StatType.SkillAthletics,StatType.SkillMobility,StatType.SkillPersuasion}) {
                var feature=f.Feature("AdaptableTraining"+skill,"Adaptable Training: "+SkillName(skill),rankDescription,new AddClassSkill{Skill=skill},new TrainingSkillRanks{Skill=skill});
                feature.ComponentsArray=feature.ComponentsArray.Concat(new BlueprintComponent[]{Need(feature,true),Need(v[skill],true)}).ToArray();
                v[skill].ComponentsArray=v[skill].ComponentsArray.Concat(new BlueprintComponent[]{Need(feature,true)}).ToArray();a.Add(skill,feature);
            }
            Set(versatile,"m_AllFeatures",v.Values.Select(Ref<BlueprintFeatureReference>).ToArray());Set(versatile,"m_Features",v.Values.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(adaptable,"m_AllFeatures",a.Values.Select(Ref<BlueprintFeatureReference>).ToArray());Set(adaptable,"m_Features",a.Values.Select(Ref<BlueprintFeatureReference>).ToArray());
            var familiar=f.Clone(nativeFamiliar,"MagusFamiliarSelection","Familiar","Gain one existing familiar using the game's native familiar choices. This arcana is available once; it does not create a combat animal companion.");
            familiar.Groups=new[]{FeatureGroup.MagusArcana};familiar.ComponentsArray=new BlueprintComponent[]{Need(familiar,true)};familiar.Ranks=1;
            var mastery=Selection(f,"MagusManeuverMasterySelection","Maneuver Mastery","Choose one combat maneuver. For that maneuver, replace the base attack bonus from your magus levels with your full magus level, preserving contributions from other classes and existing higher base attack bonus replacements. Different maneuvers may be selected; the same maneuver does not stack.",FeatureGroup.MagusArcana,5);
            var maneuvers=new[]{new[]{CombatManeuver.BullRush},new[]{CombatManeuver.Disarm},new[]{CombatManeuver.Trip},new[]{CombatManeuver.SunderArmor},new[]{CombatManeuver.DirtyTrickBlind,CombatManeuver.DirtyTrickEntangle,CombatManeuver.DirtyTrickSickened}};
            var choices=new List<BlueprintFeature>();
            foreach(var types in maneuvers) {
                string name=types[0]==CombatManeuver.DirtyTrickBlind?"Dirty Trick":types[0].ToString();
                var choice=f.Feature("MagusManeuverMastery"+types[0],"Maneuver Mastery: "+name,"Replace the base attack bonus from magus levels with the full magus level for this maneuver, preserving other classes and higher BAB effects.",new MagusManeuverTraining{Maneuvers=types,Class=Ref<BlueprintCharacterClassReference>(magus)});
                choice.Groups=new[]{FeatureGroup.MagusArcana};choice.ComponentsArray=choice.ComponentsArray.Concat(new BlueprintComponent[]{Need(choice,true)}).ToArray();choices.Add(choice);
            }
            Set(mastery,"m_AllFeatures",choices.Select(Ref<BlueprintFeatureReference>).ToArray());Set(mastery,"m_Features",choices.Select(Ref<BlueprintFeatureReference>).ToArray());
            var featPools=new[]{BlueprintRegistration.BasicFeatsId}.Concat(BlueprintRegistration.NativeCombatFeatSelectionIds).Select(id=>f.Native<BlueprintFeatureSelection>(id)).ToArray();
            var advanced=AdvancedWeaponPools.Concat(AdvancedArmorPools).Select(id=>cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection).Where(x=>x!=null).ToArray();
            var allPools=featPools.Concat(pools).Concat(new[]{training}).Concat(advanced).ToArray();
            var before=allPools.Select(s=>new[]{Get(s,"m_Features"),Get(s,"m_AllFeatures")}).ToArray();var added=new List<BlueprintGuid>();
            foreach(var bp in f.Assets.Values) {
                PsychicRepairComponents.UniqueNames(bp);
                var old=cache.Load(bp.AssetGuid);
                if(old!=null && (old.GetType()!=bp.GetType() || old.name!=bp.name || ((BlueprintScriptableObject)old).ComponentsArray==null || bp.ComponentsArray.Any(c=>!((BlueprintScriptableObject)old).ComponentsArray.Any(x=>x!=null&&x.GetType()==c.GetType()&&x.name==c.name))))throw new InvalidOperationException("T11 identity collision: "+bp.AssetGuid);
            }
            f.Enable();
            try {
                foreach(var bp in f.Assets.Values)if(cache.Load(bp.AssetGuid)==null){cache.AddCachedBlueprint(bp.AssetGuid,bp);added.Add(bp.AssetGuid);}
                BlueprintFeature Actual(BlueprintFeature bp)=>(BlueprintFeature)cache.Load(bp.AssetGuid);
                foreach(var pool in featPools.Concat(new[]{training}))RemoveTrainingRoots(pool);
                T09CombatFeats.Append(advanced.Where(x=>AdvancedWeaponPools.Contains(x.AssetGuid.ToString())).ToArray(),new[]{Actual(dazzling),Actual(versatile)});
                T09CombatFeats.Append(advanced.Where(x=>AdvancedArmorPools.Contains(x.AssetGuid.ToString())).ToArray(),new[]{Actual(adaptable)});
                T09CombatFeats.Append(pools,new[]{Actual(familiar),Actual(mastery)});
            } catch {
                for(int i=0;i<allPools.Length;i++){Set(allPools[i],"m_Features",before[i][0]);Set(allPools[i],"m_AllFeatures",before[i][1]);}
                foreach(var id in added)cache.RemoveCachedBlueprint(id);throw;
            }
        }
        internal static void Optional(BlueprintsCache cache) {
            foreach(var id in new[]{BlueprintRegistration.BasicFeatsId,TrainingPool,BlueprintRegistration.BrawlerCombatFeatSelectionId}.Concat(BlueprintRegistration.NativeCombatFeatSelectionIds)) {
                var pool=cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection;if(pool!=null)RemoveTrainingRoots(pool);
            }
            var weapon=AdvancedWeaponPools.Select(id=>cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection).Where(x=>x!=null).ToArray();
            var armor=AdvancedArmorPools.Select(id=>cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection).Where(x=>x!=null).ToArray();
            var w=FeatTokens.Take(2).Select(t=>cache.Load(Id(t)) as BlueprintFeature).Where(x=>x!=null).ToArray();var a=cache.Load(Id(FeatTokens[2])) as BlueprintFeature;
            T09CombatFeats.Append(weapon,w);if(a!=null)T09CombatFeats.Append(armor,new[]{a});
        }
        static void RemoveTrainingRoots(BlueprintFeatureSelection pool) {
            var ids=new HashSet<BlueprintGuid>(FeatTokens.Select(Id));
            foreach(string field in new[]{"m_Features","m_AllFeatures"})Set(pool,field,((BlueprintFeatureReference[])Get(pool,field)??Array.Empty<BlueprintFeatureReference>()).Where(x=>x!=null&&!ids.Contains(x.Guid)).ToArray());
        }
        internal static bool Trained(ModifiableValueSkill skill)=>skill?.Owner?.Unit?.Facts.List.Any(f=>Active(skill.Owner.Unit,f)&&f.Blueprint.GetComponents<TrainingSkillRanks>().Any(c=>c.Skill==skill.Type))==true;
        internal static void Refund(UnitEntityData unit) {if(unit?.Stats!=null)foreach(var s in unit.Stats.AllStats.OfType<ModifiableValueSkill>().Where(Trained))s.BaseValue=0;}
        internal static bool Active(UnitEntityData owner,Kingmaker.EntitySystem.EntityFact fact)=>owner!=null&&fact!=null&&fact.IsActive&&fact.IsTurnedOn&&!fact.IsDisposed&&owner.Facts.List.Contains(fact);
        internal static int WeaponRank(UnitEntityData owner)=>owner?.Body==null?0:owner.Get<UnitPartWeaponTraining>()?.GetMaxWeaponRank()??0;
        internal static bool Dazzling(UnitEntityData owner)=>WeaponRank(owner)>0 && owner.Facts.List.Any(f=>Active(owner,f)&&f.Blueprint.GetComponent<DazzlingTrainingBonus>()!=null);
    }
    [TypeId("cfc192ad21c24638b77806c3b038c513")]
    public sealed class TrainingSkillRanks : UnitFactComponentDelegate {
        public StatType Skill;
        protected override void OnTurnOn(){var stat=Owner.Stats.GetStat<ModifiableValueSkill>(Skill);if(stat!=null){stat.BaseValue=0;Owner.Stats.BaseAttackBonus.AddDependentValue(stat);stat.UpdateValue();}}
        protected override void OnTurnOff(){var stat=Owner.Stats.GetStat<ModifiableValueSkill>(Skill);if(stat!=null){Owner.Stats.BaseAttackBonus.RemoveDependentValue(stat);stat.UpdateValue();}}
    }
    [HarmonyPatch(typeof(ModifiableValueSkill),"CalculateBaseValue",new[]{typeof(int)})]
    internal static class TrainingSkillRankPatch {
        static void Prefix(ModifiableValueSkill __instance,ref int baseValue) {
            var owner=__instance.Owner?.Unit;if(owner==null)return;
            if(owner.Facts.List.Any(f=>T11TrainingOptions.Active(owner,f)&&f.Blueprint.GetComponents<TrainingSkillRanks>().Any(c=>c.Skill==__instance.Type)))baseValue=Math.Max(0,owner.Stats.BaseAttackBonus.ModifiedValue);
        }
    }
    [TypeId("b5948e9509874216b3e544754a93373a")]
    public sealed class DazzlingTrainingBonus : UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleSkillCheck>,ISubscriber {
        public void OnEventAboutToTrigger(RuleSkillCheck evt){if(T11TrainingOptions.Active(Owner,Fact)&&evt.Initiator==Owner&&evt.StatType==StatType.CheckIntimidate){int rank=T11TrainingOptions.WeaponRank(Owner);if(rank>0)evt.Bonus.AddModifier(rank,Fact,ModifierDescriptor.UntypedStackable);}}
        public void OnEventDidTrigger(RuleSkillCheck evt){}
    }
    [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.ActionType),MethodType.Getter)]
    internal static class DazzlingTrainingActionPatch {
        static void Postfix(AbilityData __instance,ref UnitCommand.CommandType __result){if(__result==UnitCommand.CommandType.Standard&&__instance.Blueprint.AssetGuid.ToString()==T11TrainingOptions.Demoralize&&T11TrainingOptions.Dazzling(__instance.Caster?.Unit))__result=UnitCommand.CommandType.Move;}
    }
    [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.RequireFullRoundAction),MethodType.Getter)]
    internal static class DazzlingTrainingFullRoundPatch {
        static void Postfix(AbilityData __instance,ref bool __result){if(__result&&__instance.Blueprint.AssetGuid.ToString()==T11TrainingOptions.Display&&T11TrainingOptions.Dazzling(__instance.Caster?.Unit))__result=false;}
    }
    [TypeId("b4b7f7c7086e4e25a046d607c41c8b18")]
    public sealed class MagusManeuverTraining : UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCalculateCMB>,ISubscriber {
        public CombatManeuver[] Maneuvers=Array.Empty<CombatManeuver>();
        public BlueprintCharacterClassReference Class;
        public void OnEventAboutToTrigger(RuleCalculateCMB evt) {
            var character=Class?.Get();if(!T11TrainingOptions.Active(Owner,Fact)||evt.Initiator!=Owner||!Maneuvers.Contains(evt.Type)||character?.BaseAttackBonus==null)return;
            int level=Owner.Progression.GetClassLevel(character);if(level<=0)return;
            int delta=level-character.BaseAttackBonus.GetBonus(level);
            int actual=evt.ReplaceBAB??evt.Base.ReplaceBAB??Owner.Stats.BaseAttackBonus.ModifiedValue;
            delta=Math.Min(delta,Math.Max(0,Owner.Progression.CharacterLevel-actual));
            if(delta>0&&!evt.AllBonuses.Any(m=>m.Fact?.Blueprint.AssetGuid==Fact.Blueprint.AssetGuid))evt.AddModifier(delta,Fact,ModifierDescriptor.UntypedStackable);
        }
        public void OnEventDidTrigger(RuleCalculateCMB evt){}
    }
}
