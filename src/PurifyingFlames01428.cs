using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PurifyingFlames01428
    {
        internal const string ModuleName = "Purifying Flames wild talent";
        internal const string Token = "PurifyingFlamesWildTalent";
        internal const string PoolId = "5c883ae0cd6d7d5448b7a420f51f8459";
        internal const string ClassId = "42a455d9ec1ad924d889272429eb8391";
        internal const string FoxfireId = "14c699ccd0564e04a9587b1845d16014";
        internal const string PoisonId = "e7240516af4241b42b2cd819929ea9da";
        internal const string HealerId = "eff667a3a43a77d45a193bb7c94b3a6c";

        internal static void Register(BlueprintsCache cache)
        {
            var f = new PsychicAssets(cache);
            var pool = f.Native<BlueprintFeatureSelection>(PoolId);
            var kineticist = f.Native<BlueprintCharacterClass>(ClassId);
            var foxfire = f.Native<BlueprintFeature>(FoxfireId);
            var spell = f.Native<BlueprintAbility>(PoisonId);
            var healer = f.Native<BlueprintAbility>(HealerId);
            var fire = foxfire.GetComponents<PrerequisiteFeaturesFromList>().ToArray();
            var burn = healer.GetComponents<AbilityKineticist>().ToArray();
            if (fire.Length != 1 || fire[0].Features.Length == 0 || burn.Length != 1 ||
                spell.GetComponents<AbilityEffectRunAction>().Count() != 1)
                throw new InvalidOperationException("Purifying Flames native fire prerequisite, burn or poison action contract changed.");

            const string text = "Element: fire. Utility wild talent, level 4; requires kineticist level 8. Burn: 1. As a standard action, touch a creature and attempt a caster level check (1d20 + your kineticist level) against each poison's DC. A successful check neutralizes that poison, as neutralize poison. The kineticist accepts the burn; the target does not pay it. This is a spell-like ability.";
            var ability = f.Clone(spell, "PurifyingFlamesAbility", "Purifying Flames", text);
            ability.Type = AbilityType.SpellLike;
            ability.ActionType = UnitCommand.CommandType.Standard;
            ability.AvailableMetamagic = 0;
            Set(ability, "m_IsFullRoundAction", false);
            Set(ability, "m_Parent", new BlueprintAbilityReference());
            var cost = CopyData(burn[0]);
            cost.Amount = 1;
            cost.BlastBurnCost = cost.InfusionBurnCost = 0;
            cost.WildTalentBurnCost = 1;
            // Only this private clone changes; native poison removal and visual
            // actions keep the current game's behavior and restrictions.
            ability.ComponentsArray = ability.ComponentsArray
                .Where(c => c is AbilityEffectRunAction || c is AbilitySpawnFx || c is SpellComponent || c is IAbilityTargetRestriction)
                .Concat(Inquisitions01426.Parameters(f, "PurifyingFlames", new[] { kineticist }, StatType.Constitution))
                .Concat(new BlueprintComponent[] { cost }).ToArray();
            // Native kineticist archetypes may use Wisdom/Charisma instead of
            // Constitution; preserve the caster's actual kineticist setting.
            ability.GetComponent<ContextCalculateAbilityParams>().UseKineticistMainStat = true;

            var level = new PrerequisiteClassLevel { Level = 8 };
            Set(level, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(kineticist));
            var feature = f.Feature(Token, "Purifying Flames", text, CopyData(fire[0]), level, Facts(ability));
            feature.Groups = new[] { FeatureGroup.KineticWildTalent };
            feature.IsClassFeature = true;
            Set(feature, "m_Icon", Get(spell, "m_Icon"));
            var once = new PrerequisiteNoFeature();
            Set(once, "m_Feature", Ref<BlueprintFeatureReference>(feature));
            feature.ComponentsArray = feature.ComponentsArray.Concat(new[] { once }).ToArray();
            Migration01426.Commit(f, new[] { pool }, feature);
        }
    }
}
