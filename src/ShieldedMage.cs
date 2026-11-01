using System;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class ShieldedMage
    {
        internal const string ModuleName = "Shielded Mage";
        internal const string Id = "ae0f6962109c4f46a97958fe8b11e5ac";
        internal const string Name = "CotWWrathPrototypeShieldedMage";
        internal const string NameKey = "d6b7ef6f-aeca-45a2-bd3a-5f469ed08856";
        internal const string DescriptionKey = "12e5f12d-29bc-445c-9305-d76930dc91ca";
        internal const string FighterId = "48ac8db94d5de7645906c7d0ad3bcfbd";
        internal const string ShieldFocusId = "ac57069b6bf8c904086171683992a92a";
        internal const string ArmorTrainingId = "3c380607706f209499d951b29d3c44f3";
        internal const string SteelbloodTrainingId = "8d6acd9ac87cf0149b7d92eb6ed105d2";

        internal static void Register(BlueprintsCache cache)
        {
            var fighter = Require<BlueprintCharacterClass>(cache, FighterId, "FighterClass");
            var focus = Require<BlueprintFeature>(cache, ShieldFocusId, "ShieldFocus");
            var armor = Require<BlueprintFeature>(cache, ArmorTrainingId, "ArmorTraining");
            var steelblood = Require<BlueprintFeature>(cache, SteelbloodTrainingId, "SteelbloodArmorTraining");
            var fighterRequirement = new PrerequisiteClassLevel {
                name = "ShieldedMageFighter1", Level = 1, Group = Prerequisite.GroupType.Any
            };
            AccessTools.Field(typeof(PrerequisiteClassLevel), "m_CharacterClass").SetValue(fighterRequirement,
                BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(fighter));
            var training = new PrerequisiteFeaturesFromList { name = "ShieldedMageShieldOrArmorTraining", Amount = 1 };
            AccessTools.Field(typeof(PrerequisiteFeaturesFromList), "m_Features").SetValue(training,
                new[] { BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(focus),
                    BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(armor),
                    BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(steelblood) });
            BlueprintRegistration.RegisterFeature(cache, Id, Name, NameKey, DescriptionKey, ShieldFocusId,
                () => new BlueprintComponent[] {
                    new PrerequisiteStatValue { name = "ShieldedMageBAB3", Stat = StatType.BaseAttackBonus,
                        Value = 3, Group = Prerequisite.GroupType.Any },
                    fighterRequirement, training,
                    new ArcaneSpellFailureIncrease { name = "ShieldedMageShieldFailureReduction", Bonus = -15,
                        ToShield = true, Universal = false }
                }, BlueprintRegistration.NativeCombatFeatSelectionIds,
                new[] { FeatureGroup.Feat, FeatureGroup.CombatFeat });
        }

        private static T Require<T>(BlueprintsCache cache, string id, string name) where T : SimpleBlueprint
        {
            var bp = cache.Load(BlueprintGuid.Parse(id)) as T;
            if (bp == null || bp.name != name)
                throw new InvalidOperationException("Shielded Mage dependency differs from the verified blueprint: " + name + " / " + id);
            return bp;
        }
    }
}
