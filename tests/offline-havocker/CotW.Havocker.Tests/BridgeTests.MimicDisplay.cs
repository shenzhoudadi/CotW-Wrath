using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using HarmonyLib;
using Xunit;

namespace CotW.Havocker.Tests {
    public sealed partial class BridgeTests {
        // Same native feat list the catalogue uses. Loading them here does not rebuild the old psychic fixture.
        static readonly string[] NativeMetamagicFeats = {
            "46fad72f54a33dc4692d3b62eca7bb78", "7f2b282626862e345935bbea5e66424b", "85f3340093d144dd944fff9a9adfd2f2", "a1de1e4f92195b442adb946f0e2b9d4e",
            "cd26b9fa3f734461a0fcedc81cafaaac", "ef7ece7bb5bb66a41b256976b27f424e", "f180e72e4a9cbaa4da8be9bc958132ef", "fbf5d9ce931f47f3a0c818b3f8ef8414"
        };

        [Fact]
        public void LateMimicProviderKeepsGuidTieThenRefreshesLocalizedDisplay() {
            game.RequireGame();
            var cache = Kingmaker.Blueprints.ResourcesLibrary.BlueprintsCache;
            var stock = NativeMetamagicFeats.Select(id => Native<BlueprintFeature>(id)).ToArray();
            Assert.All(stock, feature => Assert.Equal(1, (feature.ComponentsArray ?? Array.Empty<BlueprintComponent>()).OfType<AddMetamagicFeat>().Count()));
            EnsureCatalogue(cache, stock);
            var extend = stock.Single(v => v.AssetGuid.ToString() == "f180e72e4a9cbaa4da8be9bc958132ef");
            var flag = extend.GetComponent<AddMetamagicFeat>().Metamagic;
            var donors = new List<BlueprintFeature> { extend };
            var probe = new BlueprintFeature {
                AssetGuid = BlueprintGuid.Parse("a11c0e00000000000000000000000061"),
                name = "MimicDisplayProbe061",
                ComponentsArray = new BlueprintComponent[] { new AddMetamagicFeat { Metamagic = flag } },
                Ranks = 1
            };
            probe.OnEnable();
            donors.Add(probe);
            var pack = LocalizationManager.CurrentPack;
            Assert.NotNull(pack);
            var saved = (Name: BridgeGame.Field(extend, "m_DisplayName"), Description: BridgeGame.Field(extend, "m_Description"), Icon: BridgeGame.Field(extend, "m_Icon"));
            try {
                for (int i = 0; i < donors.Count; i++) Publish(pack, donors[i], i, "Shared detail");
                cache.AddCachedBlueprint(probe.AssetGuid, probe);
                Changed(cache, probe);
                var root = Native<BlueprintFeatureSelection>(Token("MimicMetamagic1Feature01458"));
                var leaf = Native<BlueprintFeature>(Token("MimicMetamagicOption" + unchecked((uint)(int)flag) + "Feature01458"));
                var buff = Native<BlueprintBuff>(Token("MimicMetamagicOption" + unchecked((uint)(int)flag) + "Buff01458"));
                Assert.Equal(1, root.AllFeatures.Count(v => v.GetComponent<AddFacts>().Facts.Contains(leaf)));
                var tied = donors.OrderBy(v => v.AssetGuid.ToString(), StringComparer.Ordinal).First();
                Assert.Same(BridgeGame.Field(tied, "m_DisplayName"), BridgeGame.Field(buff, "m_DisplayName"));
                pack.PutString(((LocalizedString)BridgeGame.Field(tied, "m_Description")).Key, "");
                var richer = donors.Single(v => !ReferenceEquals(v, tied));
                var sentinel = FormatterServices.GetUninitializedObject(IconField(richer).FieldType);
                BridgeGame.Set(richer, "m_Icon", sentinel);
                Changed(cache, richer);
                var toggle = Native<BlueprintActivatableAbility>(Token("MimicMetamagicOption" + unchecked((uint)(int)flag) + "01458Toggle"));
                var selector = root.AllFeatures.OfType<BlueprintFeatureSelection>().Single(v => v.GetComponent<AddFacts>().Facts.Contains(leaf));
                Assert.Equal(1, root.AllFeatures.Count(v => v.GetComponent<AddFacts>().Facts.Contains(leaf)));
                foreach (var fact in new BlueprintScriptableObject[] { buff, toggle, leaf, selector }) {
                    Assert.Same(BridgeGame.Field(richer, "m_DisplayName"), BridgeGame.Field(fact, "m_DisplayName"));
                    Assert.Same(BridgeGame.Field(richer, "m_Description"), BridgeGame.Field(fact, "m_Description"));
                    Assert.Same(sentinel, BridgeGame.Field(fact, "m_Icon"));
                }
            } finally {
                BridgeGame.Set(extend, "m_DisplayName", saved.Name);
                BridgeGame.Set(extend, "m_Description", saved.Description);
                BridgeGame.Set(extend, "m_Icon", saved.Icon);
            }
        }

        static void EnsureCatalogue(BlueprintsCache cache, BlueprintFeature[] stock) {
            try { Native<BlueprintFeatureSelection>(Token("MimicMetamagic1Feature01458")); return; }
            catch (Exception) { }
            var assembly = typeof(CallOfTheWild.Wrath.Prototype.ModEntry).Assembly;
            var assetsType = assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets", true);
            var assets = assetsType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(BlueprintsCache) }, null).Invoke(new object[] { cache });
            var cls = Native<BlueprintCharacterClass>("1b9873f1e7bfe5449bc84d03e9c8e3cc");
            var modes = Activator.CreateInstance(typeof(List<BlueprintActivatableAbility>));
            var catalogue = assembly.GetType("CallOfTheWild.Wrath.Prototype.MimicCatalogue01461", true);
            catalogue.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, new object[] { assets, cls, modes, stock });
            var created = (System.Collections.IDictionary)assetsType.GetField("Assets", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(assets);
            foreach (SimpleBlueprint bp in created.Values) cache.AddCachedBlueprint(bp.AssetGuid, bp);
            catalogue.GetMethod("Seal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, new object[] { cache, Array.Empty<BlueprintFeatureSelection>() });
            var harmony = new Harmony("CotW.Havocker.Tests.MimicDisplay");
            foreach (string name in new[] { "MimicCatalogueAdded01461Patch", "MimicCatalogueAll01461Patch", "MimicCatalogueFeatures01461Patch" })
                harmony.CreateClassProcessor(assembly.GetType("CallOfTheWild.Wrath.Prototype." + name, true)).Patch();
        }

        static void Publish(LocalizationPack pack, BlueprintFeature feature, int index, string description) {
            string nameKey = "cotw-wrath.test.mimic-donor-" + index + ".name";
            string descriptionKey = "cotw-wrath.test.mimic-donor-" + index + ".description";
            pack.PutString(nameKey, "Mimic donor " + index);
            pack.PutString(descriptionKey, description);
            BridgeGame.Set(feature, "m_DisplayName", new LocalizedString { Key = nameKey });
            BridgeGame.Set(feature, "m_Description", new LocalizedString { Key = descriptionKey });
            BridgeGame.Set(feature, "m_Icon", null);
        }

        static FieldInfo IconField(object obj) {
            for (var type = obj.GetType(); type != null; type = type.BaseType) {
                var field = type.GetField("m_Icon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            throw new MissingFieldException(obj.GetType().FullName, "m_Icon");
        }

        static void Changed(BlueprintsCache cache, BlueprintFeature feature) {
            var catalogue = typeof(CallOfTheWild.Wrath.Prototype.ModEntry).Assembly.GetType("CallOfTheWild.Wrath.Prototype.MimicCatalogue01461", true);
            catalogue.GetMethod("Changed", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, new object[] { cache, feature });
        }

        static string Token(string token) {
            var assets = typeof(CallOfTheWild.Wrath.Prototype.ModEntry).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets", true);
            return ((BlueprintGuid)assets.GetMethod("Id", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, new object[] { token })).ToString();
        }

        static T Native<T>(string id) where T : BlueprintScriptableObject => BridgeGame.Native<T>(id);
    }
}
