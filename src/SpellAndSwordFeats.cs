using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class SpellAndSwordFeats
    {
        internal const string ModuleName = "Spell and swordplay feats";
        internal static readonly string[] Tokens = { "TopplingSpellFeat", "SpontaneousMetafocusFeat", "SwordplayStyleFeat", "SwordplayUpsetFeat", "FeyFoundlingFeat" };
        internal static readonly BlueprintGuid[] Ids = Tokens.Select(Id).ToArray();
        internal const string ExpertiseBuff = "e81cd772a7311554090e413ea28ceea1", DefensiveBuff = "6ffd93355fb3bcf4592a5d976b1d32a9";
        internal const string FeintAbility = "1bb6f0b196aa457ba80bdb312dc64952";
        internal static readonly WeaponCategory[] Blades = { WeaponCategory.BastardSword, WeaponCategory.Dagger, WeaponCategory.DoubleSword, WeaponCategory.DuelingSword, WeaponCategory.ElvenCurvedBlade,
            WeaponCategory.Estoc, WeaponCategory.Falcata, WeaponCategory.Falchion, WeaponCategory.Greatsword, WeaponCategory.Kama, WeaponCategory.Kukri, WeaponCategory.Longsword, WeaponCategory.Rapier,
            WeaponCategory.Sai, WeaponCategory.Scimitar, WeaponCategory.Shortsword, WeaponCategory.Starknife, WeaponCategory.Scythe, WeaponCategory.Sickle };
        internal static bool Has(UnitEntityData u, int i) => u != null && u.Facts.List.Any(f => f.Blueprint.AssetGuid == Ids[i] && T11TrainingOptions.Active(u, f));
        internal static bool Active(UnitEntityData u, string id) => u.Facts.List.Any(f => f.Blueprint.AssetGuid.ToString() == id && T11TrainingOptions.Active(u, f));
        internal static bool Blade(ItemEntityWeapon weapon) => weapon != null && weapon.Blueprint.IsMelee && Blades.Contains(weapon.Blueprint.Category);
        static PrerequisiteFeature Need(BlueprintFeature feature) { var p = new PrerequisiteFeature(); Set(p, "m_Feature", Ref<BlueprintFeatureReference>(feature)); return p; }
        internal static void Register(BlueprintsCache cache)
        {
            var f = new PsychicAssets(cache);
            var expertise = f.Native<BlueprintFeature>("4c44724ffa8844f4d9bedb5bb27d144a");
            var focus = f.Native<BlueprintParametrizedFeature>("1e1f627d26ad36f43bbd26cc2bf8ac7e");
            var feint = f.Native<BlueprintFeature>("c610310d31414edabcedf0c8a6fe32c4");
            var wizard = f.Native<BlueprintFeatureSelection>("8c3102c2ff3b69444b139a98521a4899");
            var spellParameters = f.Native<BlueprintParametrizedFeature>("e69a85f633ae8ca4398abeb6fa11b1fe").BlueprintParameterVariants;
            if (spellParameters == null) throw new InvalidOperationException("Native spell parameter catalogue is missing.");
            f.Native<BlueprintAbility>(FeintAbility); f.Native<BlueprintBuff>(ExpertiseBuff); f.Native<BlueprintBuff>(DefensiveBuff);
            f.Native<BlueprintUnitProperty>("8a63b06d20838954e97eb444f805ec89");
            var feyIcon = f.Native<Kingmaker.Blueprints.Facts.BlueprintUnitFact>("e8445256abbdc45488c2d90373f7dae8").Icon;
            const string topplingText = "A toppling spell uses a spell slot one level higher. When a spell with the force descriptor deals damage to a creature, attempt to trip that creature using caster level instead of base attack bonus and the spell's casting ability instead of Strength. A ray must hit, and actual damage must be dealt. Use the game's normal trip immunity and CMD rules. One check per damaged creature per casting; multiple missiles do not repeat the same check. No extra action or attack of opportunity is required.";
            var toppling = f.Feature(Tokens[0], "Metamagic (Toppling Spell)", topplingText, new AddMetamagicFeat { Metamagic = TopplingSpell.Metamagic }, new TopplingSpellDamage());
            const string metaText = "Requires Charisma 13, a metamagic feat and a known spell in a spontaneous spellbook. Choose one spell you can cast spontaneously. Applying metamagic to that spell no longer adds the spontaneous full-round casting delay. The spell's own longer casting time and other effects requiring a full-round action remain. Prepared books, arcanist preparation, item casts, spell level and slot costs are unchanged. You may select this feat again for a different known spontaneous spell.";
            var meta = f.Add(new BlueprintParametrizedFeature { ParameterType = FeatureParameterType.SpellSpecialization, BlueprintParameterVariants = spellParameters.ToArray(), HasNoSuchFeature = true, Ranks = 10, Groups = new[] { FeatureGroup.Feat, FeatureGroup.WizardFeat },
                ComponentsArray = new BlueprintComponent[] { new PrerequisiteStatValue { Stat = StatType.Charisma, Value = 13 }, new SpontaneousMetafocusPrerequisite() } }, Tokens[1], "Spontaneous Metafocus", metaText);
            const string styleText = "Requires base attack bonus +3, Combat Expertise and Weapon Focus in a supported blade category. While using this style with a supported blade and Fighting Defensively or Combat Expertise, gain a +1 shield bonus to AC (does not stack with other shield bonuses). On your first ordinary melee weapon attack each turn, ignore only the Combat Expertise attack penalty. Later attacks and attacks of opportunity retain it. The first attack is tracked even when the style is off; toggling it does not restore this benefit. Real-time uses a six-second turn window. Total defense is not an available action in this game.";
            var mode = f.Buff("SwordplayStyleMode", "Swordplay Style", styleText, new SwordplayStyleEffect());
            var toggle = f.Add(new BlueprintActivatableAbility { ActivationType = AbilityActivationType.Immediately, Group = ActivatableAbilityGroup.CombatStyle, DoNotTurnOffOnRest = true, DeactivateIfCombatEnded = false,
                ComponentsArray = new BlueprintComponent[] { new SwordplayWeaponRestriction() } }, "SwordplayStyleToggle", "Swordplay Style", styleText);
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(mode));
            var style = f.Feature(Tokens[2], "Swordplay Style", styleText, new PrerequisiteStatValue { Stat = StatType.BaseAttackBonus, Value = 3 }, Need(expertise),
                new SwordplayFocusPrerequisite(), Facts(toggle), new SwordplayFirstStrike(), new SpellFeatCleanup { Buffs = new[] { Ref<BlueprintBuffReference>(mode) } });
            const string upsetText = "Requires base attack bonus +5, Swordplay Style and the game's move-action Feint feat (the native counterpart of Improved Feint). While using Swordplay Style with a supported blade, an enemy missing you with a melee weapon attack permits an immediate feint against that attacker. It uses the current native Feint skill check, target restrictions and effects, including Final Feint and Slayer's Feint if owned. It does not spend another move action or cast the spell version of Feint. Fake preview rolls and ranged attacks do not trigger it.";
            var upset = f.Feature(Tokens[3], "Swordplay Upset", upsetText, new PrerequisiteStatValue { Stat = StatType.BaseAttackBonus, Value = 5 }, Need(style), Need(feint));
            const string feyText = "You can select this feat only at character level 1. Whenever you receive magical healing with dice, heal 2 additional points per healing die. The extra amount is added after Empower and before native healing resistance and the maximum-HP cap; flat healing and natural recovery gain no per-die bonus. Gain +2 on saving throws against death effects. Take 1 additional point of damage from a cold iron weapon hit, once per weapon-damage event, without multiplying this extra point on a critical hit. Cold iron weapon use is otherwise unchanged.";
            var fey = f.Feature(Tokens[4], "Fey Foundling", feyText, new FeyFoundlingFirstLevel(), new FeyFoundlingColdIron(),
                new SavingThrowBonusAgainstDescriptor { SpellDescriptor = SpellDescriptor.Death, ModifierDescriptor = ModifierDescriptor.Feat, Value = 2 });
            toppling.Groups = new[] { FeatureGroup.Feat, FeatureGroup.WizardFeat };
            style.Groups = upset.Groups = new[] { FeatureGroup.Feat, FeatureGroup.CombatFeat, FeatureGroup.StyleFeat };
            fey.Groups = new[] { FeatureGroup.Feat };
            Set(toppling, "m_Icon", PsychicOriginalIcons.Load("TopplingSpell")); Set(meta, "m_Icon", PsychicOriginalIcons.Load("SpontaneousMetafocus"));
            Set(style, "m_Icon", PsychicOriginalIcons.Load("SwordplayStyle")); Set(mode, "m_Icon", style.Icon); Set(toggle, "m_Icon", style.Icon);
            Set(upset, "m_Icon", PsychicOriginalIcons.Load("SwordplayUpset")); Set(fey, "m_Icon", feyIcon);
            SpellResourceFeats.Commit(cache, f, new BlueprintFeature[] { toppling, meta, style, upset, fey });
            CombatFeatPoolRepair.Reconcile(cache);
            T09CombatFeats.Append(new[] { wizard }, new BlueprintFeature[] { toppling, meta });
        }
        internal static void RepairStylePool(BlueprintsCache cache, BlueprintFeatureSelection selection)
        {
            if (selection?.AssetGuid == BlueprintGuid.Parse("8c3102c2ff3b69444b139a98521a4899"))
                T09CombatFeats.Append(new[] { selection }, Tokens.Take(2).Select(t => cache.Load(Id(t)) as BlueprintFeature).Where(x => x != null).ToArray());
            if (selection == null || (selection.Group != FeatureGroup.StyleFeat && selection.Group2 != FeatureGroup.StyleFeat)) return;
            var refs = (BlueprintFeatureReference[])Get(selection, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
            if (!refs.Any(x => x.Guid.ToString() == "0c17102f650d9044290922b0fad9132f") || !refs.Any(x => x.Guid.ToString() == "87ec6541cddfa394ab540dd13399d319")) return;
            T09CombatFeats.Append(new[] { selection }, Tokens.Skip(2).Take(2).Select(t => cache.Load(Id(t)) as BlueprintFeature).Where(x => x != null).ToArray());
        }
    }
}
