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
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class LinnormFeats
    {
        internal const string ModuleName = "Linnorm style and reaction feats";
        internal static readonly string[] Tokens = { "LinnormStyleFeat", "LinnormVengeanceFeat", "LinnormWrathFeat", "ScalesAndSkinFeat", "ViciousStompFeat" };
        internal static readonly BlueprintGuid[] Ids = Tokens.Select(Id).ToArray();
        internal static bool Has(UnitEntityData unit, int index) => unit != null && unit.Facts.List.Any(f => f.Blueprint.AssetGuid == Ids[index] && T11TrainingOptions.Active(unit, f));
        static PrerequisiteFeature Need(BlueprintFeature feature) { var p = new PrerequisiteFeature(); Set(p, "m_Feature", Ref<BlueprintFeatureReference>(feature)); return p; }
        static PrerequisiteStatValue Stat(StatType stat, int value) => new PrerequisiteStatValue { Stat = stat, Value = value };

        internal static void Register(BlueprintsCache cache)
        {
            var f = new PsychicAssets(cache);
            var unarmed = f.Native<BlueprintFeature>("7812ad3672a4b9a4fb894ea402095167");
            var reflexes = f.Native<BlueprintFeature>("0f8939ae6f220984e8fb568abbdfba95");
            const string styleText = "Requires Wisdom 13, Improved Unarmed Strike and base attack bonus +3. While using this combat style, take -2 AC against melee attacks (including melee touch attacks). After an enemy makes a melee weapon attack against you, your unarmed strikes against that enemy use your positive Wisdom modifier for damage if it exceeds the current Strength or Dexterity modifier. The opening lasts until the beginning of that enemy's next turn (six seconds in real time). Other damage statistics, weapons and targets are unchanged.";
            const string vengeanceText = "Requires Wisdom 13, Improved Unarmed Strike, Linnorm Style and base attack bonus +6. While using Linnorm Style, after an enemy hits you with a melee weapon attack, gain +2 on unarmed attack rolls against that enemy until the beginning of its next turn (six seconds in real time). Repeated hits do not stack this bonus.";
            const string wrathText = "Requires Wisdom 13, Improved Unarmed Strike, Linnorm Style, Linnorm Vengeance and base attack bonus +9. While using Linnorm Style, an enemy's melee weapon hit lets you retaliate with one unarmed attack of opportunity per round. This requires an unarmed threat hand and normal threat, reach, target legality and remaining native opportunity-attack allowance. The allowance refreshes at the start of your turn (after six seconds in real time). A failed eligibility check does not consume this feat's use. These retaliations cannot trigger another Linnorm retaliation.";
            const string scalesText = "Requires Constitution 13. While a transmutation spell or spell-like ability with an ongoing buff affects you, increase your natural armor by +1, or +2 if that effect's caster level is at least 10. Use the highest eligible active effect; multiple effects do not stack this feat's bonus. It stacks with existing natural armor and natural armor enhancement, applies to normal and flat-footed AC, and does not apply to touch AC. It ends immediately when the last qualifying effect ends or is suppressed. Instantaneous effects and supernatural or extraordinary abilities do not qualify.";
            const string stompText = "Requires Combat Reflexes and Improved Unarmed Strike. When an enemy falls prone adjacent to you, it provokes one unarmed attack of opportunity. This uses the game's successful knockdown event, an unarmed threat hand, adjacency (5 feet), normal target legality and the native opportunity-attack allowance. Remaining prone or standing up does not trigger this feat again. An ordinary stand-up opportunity attack retains its own native rules. Greater Trip and this feat share the native opportunity-attack limits.";
            var mode = f.Buff("LinnormStyleMode", "Linnorm Style", styleText, new LinnormStyleEffect());
            var toggle = f.Add(new BlueprintActivatableAbility { ActivationType = AbilityActivationType.Immediately, Group = ActivatableAbilityGroup.CombatStyle, DeactivateIfCombatEnded = false, DoNotTurnOffOnRest = true }, "LinnormStyleToggle", "Linnorm Style", styleText);
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(mode));
            var style = f.Feature(Tokens[0], "Linnorm Style", styleText, Stat(StatType.Wisdom, 13), Stat(StatType.BaseAttackBonus, 3), Need(unarmed), Facts(toggle), new SpellFeatCleanup { Buffs = new[] { Ref<BlueprintBuffReference>(mode) } });
            var vengeance = f.Feature(Tokens[1], "Linnorm Vengeance", vengeanceText, Stat(StatType.Wisdom, 13), Stat(StatType.BaseAttackBonus, 6), Need(unarmed), Need(style));
            var wrath = f.Feature(Tokens[2], "Linnorm Wrath", wrathText, Stat(StatType.Wisdom, 13), Stat(StatType.BaseAttackBonus, 9), Need(unarmed), Need(style), Need(vengeance), new LinnormWrathAllowance());
            var scales = f.Feature(Tokens[3], "Scales and Skin", scalesText, Stat(StatType.Constitution, 13), new ScalesAndSkinEffect());
            var stomp = f.Feature(Tokens[4], "Vicious Stomp", stompText, Need(unarmed), Need(reflexes), new ViciousStompReaction());
            var roots = new[] { style, vengeance, wrath, scales, stomp };
            for (int i = 0; i < roots.Length; i++) roots[i].Groups = i < 3 ? new[] { FeatureGroup.Feat, FeatureGroup.CombatFeat, FeatureGroup.StyleFeat } : i == 3 ? new[] { FeatureGroup.Feat } : new[] { FeatureGroup.Feat, FeatureGroup.CombatFeat };
            Set(style, "m_Icon", PsychicOriginalIcons.Load("LinnormStyle")); Set(mode, "m_Icon", style.Icon); Set(toggle, "m_Icon", style.Icon);
            Set(vengeance, "m_Icon", PsychicOriginalIcons.Load("LinnormVengeance")); Set(wrath, "m_Icon", PsychicOriginalIcons.Load("LinnormWrath"));
            Set(stomp, "m_Icon", PsychicOriginalIcons.Load("ViciousStomp"));
            Set(scales, "m_Icon", f.Native<Kingmaker.Blueprints.Facts.BlueprintUnitFact>("7bc8e27cba24f0e43ae64ed201ad5785").Icon);
            SpellResourceFeats.Commit(cache, f, roots);
            CombatFeatPoolRepair.Reconcile(cache);
            RepairStyles(cache);
        }

        internal static void RepairStyles(BlueprintsCache cache)
        {
            // Only complete general style menus, never restricted Monk bonus menus.
            var selections = new List<BlueprintFeatureSelection>();
            cache.ForEachLoaded((id, bp) => { if (bp is BlueprintFeatureSelection s && (s.Group == FeatureGroup.StyleFeat || s.Group2 == FeatureGroup.StyleFeat)) selections.Add(s); });
            foreach (var selection in selections) RepairStylePool(cache, selection);
        }
        internal static void RepairStylePool(BlueprintsCache cache, BlueprintFeatureSelection selection)
        {
            if (selection == null || (selection.Group != FeatureGroup.StyleFeat && selection.Group2 != FeatureGroup.StyleFeat)) return;
            var refs = (BlueprintFeatureReference[])Get(selection, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
            // A complete style menu contains both Crane and Dragon style; exclude single-style submenus.
            if (!refs.Any(x => x.Guid.ToString() == "0c17102f650d9044290922b0fad9132f") || !refs.Any(x => x.Guid.ToString() == "87ec6541cddfa394ab540dd13399d319")) return;
            T09CombatFeats.Append(new[] { selection }, Tokens.Take(3).Select(t => cache.Load(Id(t)) as BlueprintFeature).Where(x => x != null).ToArray());
        }
    }
}
