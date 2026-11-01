using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.PubSubSystem;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class EnhancementSchool01429
    {
        internal const string ModuleName = "Wizard school: Enhancement";
        internal const string Token = "EnhancementSchool";
        internal const string SourceId = "b6a604dab356ac34788abf4ad79449ec";
        internal const string BaseId = "c459c8200e666ef4c990873d3e501b91";
        internal const string PhysicalId = "93919f8ce64dc5a4cbf058a486a44a1b";
        internal const string OppositionId = "fc519612a3c604446888bb345bca5234";
        static readonly StatType[] Stats = { StatType.Strength, StatType.Dexterity, StatType.Constitution, StatType.Intelligence, StatType.Wisdom, StatType.Charisma, StatType.AC };
        internal static void Register(BlueprintsCache cache)
        {
            var f = new PsychicAssets(cache);
            var pool = f.Native<BlueprintFeatureSelection>(WizardSubschools01428.SchoolPoolId);
            var wizard = f.Native<BlueprintCharacterClass>(WizardSubschools01428.WizardId);
            var source = f.Native<BlueprintProgression>(SourceId);
            var original = f.Native<BlueprintFeature>(BaseId);
            var physical = f.Native<BlueprintFeature>(PhysicalId);
            if (source.LevelEntries.Count(e => e.Level == 1) != 1 || source.LevelEntries.Count(e => e.Level == 8) != 1 ||
                source.LevelEntries.First(e => e.Level == 1).Features.Count(x => x.AssetGuid == original.AssetGuid) != 1 ||
                original.GetComponents<AddSpecialSpellList>().Count() != 1 || original.GetComponents<AddFacts>().Count() != 1 ||
                !original.GetComponent<AddFacts>().Facts.Any(x => x.AssetGuid == physical.AssetGuid) ||
                original.GetComponent<AddFacts>().Facts.Count(x=>x.AssetGuid.ToString()=="e015d05ca18b5c24183cd062486fd46b")!=1 ||
                source.LevelEntries.First(e=>e.Level==8).Features.Count(x=>x.AssetGuid.ToString()=="aeb56418768235640a3ee858d5ee05e8")!=1)
                throw new InvalidOperationException("Transmutation school layout changed; original school preserved.");
            const string augmentText = "As a supernatural standard action, touch a creature and grant +2 enhancement to one ability score (+4 at wizard level 10), or +1 natural armor plus 1 per five wizard levels, maximum +5. The augmentation lasts half your wizard level in rounds (minimum 1). Daily uses: 3 + Intelligence modifier (minimum 0).";
            const string perfectionText = "At wizard level 8, use a supernatural swift action to grant yourself an enhancement bonus to one ability score equal to half your wizard level (maximum +10) for one round. Daily uses equal your wizard level. All six variants share this resource.";
            var augmentResource = Resource(f, wizard, "EnhancementAugmentResource", "Augment", false);
            var perfectionResource = Resource(f, wizard, "EnhancementPerfectionResource", "Perfection of Self", true);
            var augmentVariants = Stats.Select(stat => Ability(f, wizard, stat, false, augmentResource, augmentText)).ToArray();
            var perfectionVariants = Stats.Where(s => s != StatType.AC).Select(stat => Ability(f, wizard, stat, true, perfectionResource, perfectionText)).ToArray();
            var augment = Wrapper(f, "EnhancementAugment", "Augment", augmentText, augmentVariants);
            var perfection = Wrapper(f, "EnhancementPerfection", "Perfection of Self", perfectionText, perfectionVariants);
            var first = f.Clone(original, "EnhancementBaseFeature", "Enhancement", augmentText);
            first.HideInUI=false;
            var grants=first.GetComponent<AddFacts>();
            Set(grants,"m_Facts",grants.Facts.Where(x=>x.AssetGuid.ToString()!="e015d05ca18b5c24183cd062486fd46b").Concat(new BlueprintUnitFact[] {augment}).Select(Ref<BlueprintUnitFactReference>).ToArray());
            first.ComponentsArray = first.ComponentsArray.Concat(new BlueprintComponent[] { Grant(augmentResource) }).ToArray();
            var greater = f.Feature("EnhancementPerfectionFeature", "Perfection of Self", perfectionText, Facts(perfection), Grant(perfectionResource)); greater.IsClassFeature = true;
            Set(greater, "m_Icon", Get(original, "m_Icon"));
            const string text = "A transmutation school option. Augment replaces Telekinetic Fist; Perfection of Self replaces Change Shape at level 8. Native Physical Enhancement, the specialist spell list, both opposition-school choices and the level-20 Physical Enhancement capstone remain. Augment and Perfection of Self grant enhancement bonuses, so a larger enhancement bonus takes precedence rather than adding twice.";
            var root = f.Clone(source, Token, "Enhancement", text);
            root.LevelEntries = CopyData(source.LevelEntries).Select(e => e.Level == 1
                ? Entry(1, e.Features.Select(x => x.AssetGuid == original.AssetGuid ? first : x).ToArray())
                : e.Level == 8 ? Entry(8, e.Features.Select(x=>x.AssetGuid.ToString()=="aeb56418768235640a3ee858d5ee05e8"?greater:x).ToArray()) : e).ToArray();
            Set(root, "m_Classes", CopyData(Get(source, "m_Classes"))); Set(root, "m_Archetypes", CopyData(Get(source, "m_Archetypes")));
            root.UIGroups=CopyData(source.UIGroups);
            foreach(var ui in root.UIGroups)Set(ui,"m_Features",((System.Collections.Generic.List<BlueprintFeatureBaseReference>)Get(ui,"m_Features")).Select(x=>x.Guid==original.AssetGuid?Ref<BlueprintFeatureBaseReference>(first):x.Guid.ToString()=="aeb56418768235640a3ee858d5ee05e8"?Ref<BlueprintFeatureBaseReference>(greater):x).ToList());
            Set(root,"m_UIDeterminatorsGroup",((BlueprintFeatureBaseReference[])Get(source,"m_UIDeterminatorsGroup")).Select(x=>x.Guid==original.AssetGuid?Ref<BlueprintFeatureBaseReference>(first):CopyData(x)).ToArray());
            // The cloned progression already rejects Transmutation opposition.
            // Its reciprocal native prerequisite must also know the new root.
            var opposition = f.Native<BlueprintFeature>(OppositionId);
            var previous = opposition.ComponentsArray;
            try {
                if (!opposition.GetComponents<PrerequisiteNoFeature>().Any(p => p.Feature?.AssetGuid == root.AssetGuid)) {
                    var exclusive = new PrerequisiteNoFeature { name = "Enhancement01430_ExclusiveSchool" };
                    Set(exclusive, "m_Feature", Ref<BlueprintFeatureReference>(cache.Load(root.AssetGuid) as BlueprintFeature ?? root));
                    opposition.ComponentsArray = previous.Concat(new BlueprintComponent[] { exclusive }).ToArray();
                }
                Migration01426.Commit(f, new[] { pool }, root);
            }
            catch { opposition.ComponentsArray = previous; throw; }
        }
        static LevelEntry Entry(int level, params BlueprintFeatureBase[] features) { var e = new LevelEntry { Level = level }; Set(e, "m_Features", features.Select(Ref<BlueprintFeatureBaseReference>).ToList()); return e; }
        static BlueprintAbilityResource Resource(PsychicAssets f, BlueprintCharacterClass wizard, string token, string name, bool byLevel)
        {
            var r = f.Add(new BlueprintAbilityResource(), token, name, name); var a = Get(r, "m_MaxAmount");
            Set(a, "BaseValue", byLevel ? 0 : 3); Set(a, "IncreasedByStat", !byLevel); Set(a, "ResourceBonusStat", StatType.Intelligence);
            Set(a, "IncreasedByLevel", byLevel); Set(a, "LevelIncrease", byLevel ? 1 : 0);
            Set(a, "m_Class", byLevel ? new[] { Ref<BlueprintCharacterClassReference>(wizard) } : Array.Empty<BlueprintCharacterClassReference>());
            Set(a, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>());
            foreach (var field in new[] { "m_Archetypes", "m_ArchetypesDiv" }) Set(a, field, Array.Empty<BlueprintArchetypeReference>());
            Set(r, "m_MaxAmount", a); Set(r, "m_Min", 0); r.LocalizedName = Text(token, "name", name); r.LocalizedDescription = Text(token, "description", name); return r;
        }
        static AddAbilityResources Grant(BlueprintAbilityResource r) { var c = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true }; Set(c, "m_Resource", Ref<BlueprintAbilityResourceReference>(r)); return c; }
        static BlueprintAbility Wrapper(PsychicAssets f, string token, string name, string text, BlueprintAbility[] leaves)
        {
            var c = new AbilityVariants(); Set(c, "m_Variants", leaves.Select(Ref<BlueprintAbilityReference>).ToArray());
            var a = f.Ability(token, name, text, AbilityType.Supernatural, leaves[0].ActionType, leaves[0].Range, c);
            a.CanTargetSelf = true; a.CanTargetFriends = leaves[0].CanTargetFriends; a.SpellResistance = false; Set(a, "m_Icon", Get(leaves[0], "m_Icon"));
            foreach (var leaf in leaves) Set(leaf, "m_Parent", Ref<BlueprintAbilityReference>(a)); return a;
        }
        static BlueprintAbility Ability(PsychicAssets f, BlueprintCharacterClass wizard, StatType stat, bool perfection, BlueprintAbilityResource resource, string text)
        {
            string token = "Enhancement" + (perfection ? "Perfection" : "Augment") + stat;
            string title = (perfection ? "Perfection of Self: " : "Augment: ") + (stat == StatType.AC ? "Natural Armor" : stat.ToString());
            var rank = Inquisitions01426.Scaling(new[] { wizard }, AbilityRankType.Default, perfection);
            if (perfection) { Set(rank, "m_UseMax", true); Set(rank, "m_Max", 10); }
            else if (stat == StatType.AC) { Set(rank, "m_Progression", ContextRankProgression.OnePlusDivStep); Set(rank, "m_StepLevel", 5); Set(rank, "m_UseMax", true); Set(rank, "m_Max", 5); }
            else { Set(rank, "m_Progression", ContextRankProgression.OnePlusDivStep); Set(rank, "m_StepLevel", 10); Set(rank, "m_UseMax", true); Set(rank, "m_Max", 2); }
            var bonus = stat==StatType.AC ? new EnhancementNaturalArmor01429() : new AddContextStatBonus();
            bonus.Stat=stat;bonus.Descriptor=stat==StatType.AC?ModifierDescriptor.NaturalArmor:ModifierDescriptor.Enhancement;bonus.Value=Inquisitions01426.Rank();bonus.Multiplier=!perfection&&stat!=StatType.AC?2:1;
            var buff = f.Buff(token + "Buff", title, text, rank, bonus);
            // Different sources coexist. Enhancement uses native non-stacking;
            // natural armor uses our group election and restores weaker sources.
            buff.Stacking = Kingmaker.UnitLogic.Buffs.Blueprints.StackingType.Stack;
            var duration = Inquisitions01426.Scaling(new[] { wizard }, AbilityRankType.Default, true); Set(duration, "m_UseMin", true); Set(duration, "m_Min", 1);
            var apply = Apply(buff, perfection ? 1 : 0, fromSpell: false); apply.IsNotDispelable = true;
            if (!perfection) apply.DurationValue.BonusValue = Inquisitions01426.Rank();
            var cost = new AbilityResourceLogic { Amount = 1 }; Set(cost, "m_IsSpendResource", true); Set(cost, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
            var ability = f.Ability(token + "Ability", title, text, AbilityType.Supernatural, perfection ? UnitCommand.CommandType.Swift : UnitCommand.CommandType.Standard,
                perfection ? AbilityRange.Personal : AbilityRange.Touch, Run(apply), duration, cost);
            ability.ComponentsArray = ability.ComponentsArray.Concat(Inquisitions01426.Parameters(f, token, new[] { wizard }, StatType.Intelligence).Where(c => !(c is ContextRankConfig))).ToArray();
            ability.CanTargetSelf = true; ability.CanTargetFriends = !perfection; ability.CanTargetEnemies = false; ability.SpellResistance = false;
            Set(ability, "m_Icon", Get(f.Native<BlueprintFeature>(BaseId), "m_Icon")); Set(buff, "m_Icon", Get(ability, "m_Icon"));
            return ability;
        }
    }
    // NaturalArmor stacks natively. Elect the strongest Augment instance only,
    // preserving other natural armor sources and restoring weaker live instances.
    [TypeId("720cf23f70f44bf48891b9ee30a53ef1")]
    public sealed class EnhancementNaturalArmor01429:AddContextStatBonus,IUnitBuffHandler
    {
        void Refresh(bool turningOff = false){
            // A manager restart turns facts on one at a time without buff-added
            // events. Reconcile the whole group on every transition, including
            // the departing fact after it has left the manager's collection.
            foreach(var m in Owner.Stats.AC.Modifiers.Where(m=>m.Source is Buff b&&b.Blueprint.AssetGuid==Fact.Blueprint.AssetGuid).ToArray())Owner.Stats.AC.RemoveModifier(m);
            var strongest=Owner.Buffs.Enumerable.Where(b=>b.IsTurnedOn&&!b.IsSuppressed&&!b.IsDisposed&&b.Blueprint.AssetGuid==Fact.Blueprint.AssetGuid&&(!turningOff||!ReferenceEquals(b,Fact)))
                .OrderByDescending(b=>CalculateBaseValue(b.Context)).ThenBy(b=>b.UniqueId,StringComparer.Ordinal).FirstOrDefault();
            if(strongest!=null)Owner.Stats.AC.AddModifier(CalculateBaseValue(strongest.Context),strongest,ModifierDescriptor.NaturalArmor);
        }
        protected override void OnTurnOn()=>Refresh();
        protected override void OnTurnOff()=>Refresh(true);
        protected override void OnApplyPostLoadFixes()=>Refresh();
        public void HandleBuffDidAdded(Buff b){if(b.Owner==Owner&&b.Blueprint==Fact.Blueprint)Refresh();}
        public void HandleBuffDidRemoved(Buff b){if(b.Owner==Owner&&b.Blueprint==Fact.Blueprint)Refresh();}
    }
}
