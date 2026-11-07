using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using CallOfTheWild.Wrath.Prototype;
using Xunit;

namespace CotW.Havocker.Tests {
    public sealed partial class BridgeTests {
        [Fact]
        public void SpellburnChargeBuffsAbilitiesAndFeatureReuseGatherPowerIcon() {
            game.RequireGame();
            var donorId = (string)game.SpellApi.GetField("IconDonorId", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            var donor = BridgeGame.Native<BlueprintAbility>(donorId);
            Assert.Equal(donorId, donor.AssetGuid.ToString());
            var iconField = IconField(donor);
            var authored = iconField.GetValue(donor);
            var bridge = game.Copy<BlueprintFeature>(Burn);
            var controller = bridge.ComponentsArray.OfType<HavockerSpellburnController0020>().Single();
            var feature = BridgeGame.Native<BlueprintFeature>(SpellburnFeature);
            var spells = (BlueprintAbilityReference[])BridgeGame.Field(feature.ComponentsArray.OfType<HavockerPreparedConversion0020>().Single(), "m_SpellsByLevel");
            Assert.Null(controller.ChargeBuffs[0]);
            Assert.Null(controller.ChargeBuffs[1]);
            var shown = new System.Collections.Generic.List<BlueprintUnitFact> { feature };
            for (int level = 2; level <= 9; level++) {
                var buff = controller.ChargeBuffs[level].Get();
                var ability = spells[level].Get();
                Assert.Equal(level, ability.GetComponent<HavockerSpellburnActivation0020>().SlotLevel);
                shown.Add(buff);
                shown.Add(ability);
            }
            Assert.All(shown, fact => Assert.Same(authored, iconField.GetValue(fact)));
            // This host leaves bundle sprites null. Prove the production copy follows the donor reference.
            var sentinel = FormatterServices.GetUninitializedObject(iconField.FieldType);
            iconField.SetValue(donor, sentinel);
            try {
                BridgeGame.Call(game.SpellApi, "ApplyIcons", new object[] { shown.ToArray() });
                Assert.All(shown, fact => Assert.Same(sentinel, iconField.GetValue(fact)));
            } finally { iconField.SetValue(donor, authored); }
        }

        static FieldInfo IconField(BlueprintAbility donor) {
            for (var type = donor.GetType(); type != null; type = type.BaseType) {
                var field = type.GetField("m_Icon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            throw new MissingFieldException(donor.GetType().FullName, "m_Icon");
        }
    }
}
