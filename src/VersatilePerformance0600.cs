// COTW-0600 Versatile performance closer to tabletop rules, ported from
// Call of the Wild (Kingmaker) Features/VersatilePerformance.cs.
// Wrath has no Versatile Performance slot of its own: bards choose a Bard Talent
// at 2/6/10/14/18 and skalds a Skald Talent at 2/7/12/17. The reworked choice
// therefore becomes extra entries in those two existing pools, which is also
// what keeps every archetype that replaces the pool working unchanged - it
// simply loses these entries together with the pool.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Newtonsoft.Json;
using UnityEngine;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class VersatilePerformance0600
    {
        internal const string ModuleName = "Versatile performance";
        internal static string LastSummary = "Not registered";

        internal const string BardTalent = "94e2cd84bf3a8e04f8609fe502892f4f", SkaldTalent = "d2a8fde8985691045b90e1ec57e3cc57";
        internal const string FighterClass = "48ac8db94d5de7645906c7d0ad3bcfbd";
        internal const string SkillFocus = "c9629ef9eebb88b479b2fbc5e836656a", WeaponFocus = "1e1f627d26ad36f43bbd26cc2bf8ac7e";
        internal const string SelectionToken = "MartialPerformance0600";

        // The group both native talent pools use; new entries carry it so the
        // character sheet files them with the talent that granted them.
        static readonly FeatureGroup[] Talents = { FeatureGroup.RogueTalent };

        internal static readonly StatType[] Skills =
        {
            StatType.SkillMobility, StatType.SkillStealth, StatType.SkillPerception,
            StatType.SkillPersuasion, StatType.SkillUseMagicDevice
        };

        const string VersatileText = "At 2nd level a bard or skald selects one of the following skills: Mobility, Stealth, Perception, Persuasion or Use Magic Device. She gains a bonus equal to half her bard or skald level on this skill, uses her Charisma modifier in place of the ability modifier the skill would normally use, and treats it as a class skill. Choosing this consumes one Bard or Skald talent.";
        const string ExpandedText = "Select a skill for which you already have Versatile Performance. You gain an additional bonus equal to half your bard or skald level on that skill. Choosing this consumes one Bard or Skald talent.";
        const string MartialText = "Select a weapon you are proficient with. You gain Weapon Focus with that weapon and treat your bard or skald level as half your fighter level for the purpose of qualifying for combat feats. Choosing this consumes one Bard or Skald talent.";

        internal static string SkillName(StatType skill) =>
            skill.ToString().Substring("Skill".Length)
                .Replace("UseMagicDevice", "Use Magic Device");

        // The category enum also names things nobody wields: the blank slot,
        // rays, kinetic blasts and bombs. Weapon Focus cannot apply to them, so
        // they are matched by name and left out of the Martial Performance list.
        static readonly string[] NotAWieldedWeapon = { "None", "Ray", "KineticBlast", "Bomb" };

        internal static WeaponCategory[] WeaponCategories { get; } =
            Enum.GetValues(typeof(WeaponCategory)).Cast<WeaponCategory>()
                .Where(c => !NotAWieldedWeapon.Contains(c.ToString())).ToArray();

        // Weapon names come from the game's own localized category table, so a
        // player sees "Heavy Flail" / "重型连枷" rather than an enum name, and a
        // mod-added category gets its own localized text for free. The fallback
        // only runs when the table is unavailable (for example an unloaded
        // localization) and spells the enum name out.
        static readonly Dictionary<string, string> CategoryLabels = new Dictionary<string, string>
        {
            { "WeaponLightShield", "Light Shield" }, { "WeaponHeavyShield", "Heavy Shield" },
            { "SpikedLightShield", "Spiked Light Shield" }, { "SpikedHeavyShield", "Spiked Heavy Shield" },
            { "OtherNaturalWeapons", "Other Natural Weapons" }, { "UnarmedStrike", "Unarmed Strike" }
        };

        internal static string CategoryName(WeaponCategory category)
        {
            var localized = LocalizedCategoryName(category);
            if (!string.IsNullOrWhiteSpace(localized)) return localized;
            var source = category.ToString();
            string known;
            if (CategoryLabels.TryGetValue(source, out known)) return known;
            var text = new System.Text.StringBuilder(source.Length + 4);
            for (var i = 0; i < source.Length; i++)
            {
                if (i > 0 && char.IsUpper(source[i]) && !char.IsUpper(source[i - 1])) text.Append(' ');
                text.Append(source[i]);
            }
            return text.ToString();
        }

        static string LocalizedCategoryName(WeaponCategory category)
        {
            try
            {
                var table = Kingmaker.Blueprints.Root.LocalizedTexts.Instance?.WeaponCategories;
                return table?.GetText(category);
            }
            catch (Exception)
            {
                // A missing or partially loaded string table must not block
                // registration; the enum fallback still names the option.
                return null;
            }
        }

        static PrerequisiteClassLevel Level(PsychicAssets f, string classId, int level)
        {
            var p = new PrerequisiteClassLevel { Level = level, Group = Prerequisite.GroupType.Any };
            Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(f.Native<BlueprintCharacterClass>(classId)));
            return p;
        }

        static PrerequisiteFeature Need(BlueprintFeature feature)
        {
            var p = new PrerequisiteFeature();
            Set(p, "m_Feature", Ref<BlueprintFeatureReference>(feature));
            return p;
        }

        // Wrath ships one Skill Focus parametrized feature, not one leaf per
        // skill, so every entry shares that icon and is told apart by its name.
        static Sprite SkillIcon(PsychicAssets f)
        {
            var parametrized = f.Cache.Load(BlueprintGuid.Parse(SkillFocus)) as BlueprintParametrizedFeature;
            if (parametrized != null) return parametrized.Icon;
            var selection = f.Cache.Load(BlueprintGuid.Parse(SkillFocus)) as BlueprintFeatureSelection;
            return selection?.AllFeatures.FirstOrDefault()?.Icon;
        }

        internal static void Register(BlueprintsCache cache)
        {
            if (MagusBatch01454.Existing(cache, ModuleName)) { LastSummary = "Already registered"; return; }
            var f = new PsychicAssets(cache);
            var bardTalent = f.Native<BlueprintFeatureSelection>(BardTalent);
            var skaldTalent = f.Native<BlueprintFeatureSelection>(SkaldTalent);
            var bard = f.Native<BlueprintCharacterClass>(BardMasterpieces01435.BardClass);
            var skald = f.Native<BlueprintCharacterClass>(BardMasterpieces01435.SkaldClass);
            var fighter = f.Native<BlueprintCharacterClass>(FighterClass);
            var weaponFocus = f.Native<BlueprintParametrizedFeature>(WeaponFocus);
            var icon = SkillIcon(f);
            var martialIcon = weaponFocus?.Icon ?? icon;

            var leaves = new List<BlueprintFeature>();
            foreach (var skill in Skills)
            {
                var name = SkillName(skill);
                var granted = f.Feature("VersatilePerformance" + skill + "0600", "Versatile Performance: " + name, VersatileText,
                    new ReplaceStatBaseAttribute { TargetStat = skill, BaseAttributeReplacement = StatType.Charisma, ReplaceIfHigher = false },
                    new AddClassSkill { Skill = skill },
                    Ranks(bard, skald, skill));
                Set(granted, "m_Icon", icon);
                var expanded = f.Feature("ExpandedVersatility" + skill + "0600", "Expanded Versatility: " + name, ExpandedText,
                    Ranks(bard, skald, skill), Need(granted));
                Set(expanded, "m_Icon", icon);
                // Native entries of both pools are tagged as rogue talents, which
                // is the group the two talent selections share; the character
                // sheet then files these picks with the rest of the pool.
                granted.Groups = expanded.Groups = Talents;
                leaves.Add(granted);
                leaves.Add(expanded);
            }
            leaves.Add(Martial(f, bard, skald, fighter, martialIcon));

            var pools = new[] { bardTalent, skaldTalent };
            var before = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            try
            {
                MagusBatch01454.Publish(ModuleName, f,
                    () => T09CombatFeats.Append(pools, leaves.ToArray()),
                    () => { for (var i = 0; i < pools.Length; i++) { Set(pools[i], "m_Features", before[i][0]); Set(pools[i], "m_AllFeatures", before[i][1]); } },
                    () => Verify(cache, pools, leaves.ToArray()));
            }
            catch
            {
                for (var i = 0; i < pools.Length; i++) { Set(pools[i], "m_Features", before[i][0]); Set(pools[i], "m_AllFeatures", before[i][1]); }
                throw;
            }
            LastSummary = "Registered five Versatile Performance skills with Expanded Versatility and Martial Performance in the Bard and Skald talent pools.";
        }

        // Tabletop half level, rounded down: 2nd level grants one, 12th six. The
        // original CotW used 1 + level/2 here; this package follows the printed
        // rule instead, the same progression Expanded Versatility already uses.
        static VersatilePerformanceRanks0600 Ranks(BlueprintCharacterClass bard, BlueprintCharacterClass skald, StatType skill)
        {
            return new VersatilePerformanceRanks0600
            {
                Skill = skill,
                Bard = Ref<BlueprintCharacterClassReference>(bard),
                Skald = Ref<BlueprintCharacterClassReference>(skald)
            };
        }

        static BlueprintFeatureSelection Martial(PsychicAssets f, BlueprintCharacterClass bard, BlueprintCharacterClass skald, BlueprintCharacterClass fighter, Sprite icon)
        {
            var selection = f.Add(new BlueprintFeatureSelection
            {
                Ranks = 1,
                IsClassFeature = true,
                Groups = Talents,
                IgnorePrerequisites = false
            }, SelectionToken, "Martial Performance", MartialText);
            var entries = new List<BlueprintFeature>();
            var template = ParametrizedTemplate(f);
            foreach (var category in WeaponCategories)
            {
                // Wrath's additive parametrized-feature element type is private
                // and its serialized names no longer match Kingmaker's, so the
                // authoring shape is copied from a native Weapon Focus entry and
                // only the category field is replaced, found by type not by name.
                var choice = Activator.CreateInstance(template.GetType());
                foreach (var field in template.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    field.SetValue(choice, field.GetValue(template));
                foreach (var field in template.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    if (field.FieldType == typeof(WeaponCategory)) field.SetValue(choice, category);
                var array = Array.CreateInstance(template.GetType(), 1);
                array.SetValue(choice, 0);
                var grant = new AddParametrizedFeatures();
                Set(grant, "m_Features", array);
                var proficiency = new PrerequisiteProficiency
                {
                    WeaponProficiencies = new[] { category },
                    ArmorProficiencies = Array.Empty<ArmorProficiencyGroup>()
                };
                var entry = f.Feature("MartialPerformance" + category + "0600", "Martial Performance (" + CategoryName(category) + ")", MartialText,
                    grant, proficiency, FighterLevels(fighter, bard, 0.5), FighterLevels(fighter, skald, 0.5));
                Set(entry, "m_Icon", icon);
                entries.Add(entry);
            }
            Set(selection, "m_Features", Array.Empty<BlueprintFeatureReference>());
            Set(selection, "m_AllFeatures", entries.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(selection, "m_Icon", icon);
            // The fighter-level equivalence belongs to the picked feat, not to
            // this container, so a prerequisite check of the general feat pool
            // sees the character's own fact instead of the shared selection.
            selection.ComponentsArray = new BlueprintComponent[]
            {
                Level(f, BardMasterpieces01435.BardClass, 6), Level(f, BardMasterpieces01435.SkaldClass, 6)
            };
            return selection;
        }

        // Native Zen Archer Longbow Focus: one Weapon Focus entry, used only as
        // an authoring template and never modified.
        internal const string ParametrizedTemplateId = "2e659493076249f49821925b9b7dc389";

        static object ParametrizedTemplate(PsychicAssets f)
        {
            var prototype = f.Native<BlueprintFeature>(ParametrizedTemplateId);
            var component = prototype.GetComponent<AddParametrizedFeatures>()
                ?? throw new InvalidOperationException("0600 native Weapon Focus template lost its parametrized grant.");
            var array = (Array)Get(component, "m_Features");
            if (array == null || array.Length == 0 || array.GetValue(0) == null)
                throw new InvalidOperationException("0600 native Weapon Focus template is empty.");
            return array.GetValue(0);
        }

        // Native Sword Saint Fighter Training shape: half the listed class levels
        // count as fighter levels for the general feat pool's prerequisites.
        static ClassLevelsForPrerequisites FighterLevels(BlueprintCharacterClass fighter, BlueprintCharacterClass actual, double modifier)
        {
            var component = new ClassLevelsForPrerequisites { Modifier = modifier };
            Set(component, "m_FakeClass", Ref<BlueprintCharacterClassReference>(fighter));
            Set(component, "m_ActualClass", Ref<BlueprintCharacterClassReference>(actual));
            Set(component, "m_ForSelection", Ref<BlueprintFeatureSelectionReference>(
                (BlueprintFeatureSelection)ResourcesLibrary.BlueprintsCache.Load(BlueprintGuid.Parse(BlueprintRegistration.BasicFeatsId))));
            return component;
        }

        static void Verify(BlueprintsCache cache, BlueprintFeatureSelection[] pools, BlueprintFeature[] leaves)
        {
            var martial = cache.Load(Id(SelectionToken)) as BlueprintFeatureSelection;
            if (martial == null || !martial.AllFeatures.Any())
                throw new InvalidOperationException("0600 Martial Performance selection is missing.");
            foreach (var pool in pools)
                foreach (var leaf in leaves)
                    if ((Get(pool, "m_AllFeatures") as BlueprintFeatureReference[])?.Count(r => r != null && r.Guid == leaf.AssetGuid) != 1)
                        throw new InvalidOperationException("0600 pool " + pool.name + " lost entry " + leaf.name);
            // Every weapon entry carries its own parametrized Weapon Focus, the
            // proficiency gate and the fighter-level equivalence; the container
            // carries only the two class-level gates.
            var entries = martial.AllFeatures.ToArray();
            if (entries.Length != WeaponCategories.Length)
                throw new InvalidOperationException("0600 Martial Performance lists " + entries.Length + " weapons.");
            foreach (var entry in entries)
            {
                if (entry == null)
                    throw new InvalidOperationException("0600 Martial Performance has an empty weapon entry.");
                var grant = entry.GetComponent<AddParametrizedFeatures>();
                var array = grant == null ? null : (Array)Get(grant, "m_Features");
                if (array == null || array.Length != 1 || array.GetValue(0) == null)
                    throw new InvalidOperationException("0600 " + entry.name + " lost its Weapon Focus.");
                if (!entry.ComponentsArray.OfType<PrerequisiteProficiency>().Any())
                    throw new InvalidOperationException("0600 " + entry.name + " lost its proficiency gate.");
                if (!entry.ComponentsArray.OfType<ClassLevelsForPrerequisites>().Any())
                    throw new InvalidOperationException("0600 " + entry.name + " lost its fighter-level equivalence.");
            }
        }
    }

    // The ranks are granted as a skill modifier rather than by raising the
    // skill's base value. Wrath validates every skill in LevelUpState
    // .IsSkillPointsComplete against NextCharacterLevel, so ranks injected into
    // BaseValue would sit above the level cap and stop the player from
    // finishing any later level up. A modifier keeps the rank column legal,
    // stacks with itself for Expanded Versatility and is rebuilt from the fact,
    // so nothing has to be persisted: the modifier is removed by source and
    // added again on activation or level gain.
    [TypeId("3f0d5b0a1c8e4c37b6d1a2e4c9f70b60")]
    public sealed class VersatilePerformanceRanks0600 : UnitFactComponentDelegate, IOwnerGainLevelHandler
    {
        public StatType Skill;
        public BlueprintCharacterClassReference Bard, Skald;

        int Target()
        {
            var owner = Owner;
            if (owner == null) return 0;
            var level = 0;
            var bard = Bard?.Get();
            var skald = Skald?.Get();
            if (bard != null) level += owner.Progression.GetClassLevel(bard);
            if (skald != null) level += owner.Progression.GetClassLevel(skald);
            return level / 2;
        }

        protected override void OnTurnOn() => Refresh();

        protected override void OnTurnOff() => Refresh(0);

        public void HandleUnitGainLevel() => Refresh();

        void Refresh() => Refresh(Target());

        void Refresh(int value)
        {
            var stat = Owner?.Stats.GetStat<ModifiableValueSkill>(Skill);
            if (stat == null) return;
            foreach (var modifier in stat.Modifiers.Where(m => ReferenceEquals(m.Source, Fact)).ToArray())
                stat.RemoveModifier(modifier);
            if (value > 0) stat.AddModifier(value, Fact, ModifierDescriptor.UntypedStackable);
            stat.UpdateValue();
        }
    }
}
