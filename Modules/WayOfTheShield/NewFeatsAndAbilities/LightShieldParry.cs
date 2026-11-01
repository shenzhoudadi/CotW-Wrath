using System;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.FactLogic;

namespace Way_of_the_shield.NewFeatsAndAbilities
{
    // HEAD has only this feat's text. This entrance deliberately shares the existing parry engine.
    public static class LightShieldParry
    {
        public const string Guid = "3a76e2e292d14c4184165fffdbc0c4e0";
        public static BlueprintFeature Feature { get; private set; }
        public static void CreateBlueprint(BlueprintFeature parryFeature)
        {
            if (Feature != null || parryFeature == null) return;
            Feature = new BlueprintFeature {
                m_DisplayName = new() { Key = "LightShieldParryFeature_DisplayName" },
                m_Description = new() { Key = "LightShieldParryFeature_Description" },
                m_Icon = LoadIcon("Buckler"), Ranks = 1,
                Groups = new[] { FeatureGroup.Feat, FeatureGroup.CombatFeat }
            };
            Feature.AddToCache(Guid, "LightShieldParryFeature");
            Feature.AddComponent(new PrerequisiteProficiency { ArmorProficiencies = new[] { ArmorProficiencyGroup.LightShield }, WeaponProficiencies = Array.Empty<WeaponCategory>() });
            Feature.AddComponent(new AddFacts { m_Facts = new[] { parryFeature.ToReference<BlueprintUnitFactReference>() } });
            Feature.AddComponent(new FeatureTagsComponent { FeatureTags = FeatureTag.Defense | FeatureTag.Melee });
            Feature.AddFeatureToSelections(UnhinderingShield.selections);
        }
    }
}
