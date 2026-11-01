using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items;
using static Way_of_the_shield.Main;
using static Way_of_the_shield.NewFeatsAndAbilities.SacredShieldFeatures.BastionOfGoodFeature;
using static Way_of_the_shield.NewFeatsAndAbilities.SacredShieldFeatures.HolyShield;
using static Way_of_the_shield.NewFeatsAndAbilities.SacredShieldFeatures.SacredShieldBond;

namespace Way_of_the_shield.NewArchetypes
{
    public class SacredShield
    {
        public static void CreateSacredShieldArchetype()
        {
            const string id = "56f19f65e28b4c3fb03e425fb047e08a";
            var paladin = Require<BlueprintCharacterClass>("bfa11238e7ae3544bbeb4d0b92e897ec");
            var progression = Require<BlueprintProgression>("fd325cbba872e5f40b618970678db002");
            var existing = ResourcesLibrary.TryGetBlueprint<BlueprintArchetype>(id);
            if (existing != null)
            {
                if (existing.name != modName + "_SacredShieldArchetype" || existing.m_ParentClass != paladin)
                    throw new InvalidOperationException("Sacred Shield archetype GUID is owned by another provider.");
                return;
            }
            string[] dependencies = {
                "d7963e1fcf260c148877afd3252dbc91", "d52566ae8cbe8dc4dae977ef51c27d91", "533e10c8b4c6a4940a3767d096f4f05d", "f4cef3ba1a15b0f4fa7fd66b602ff32b",
                "3a6db57fce75b0244a6a5819528ddf26", "cb6d55dda5ab906459d18a435994a760", "ad7dc4eba7bf92f4aba23f716d7a9ba6", "9f13fdd044ccb8a439f27417481cb00e", "eff3b63f744868845a2f511e9929f0de",
                "7bb9eb2042e67bf489ccd1374423cdec", "b4274c5bb0bf2ad4190eb7c44859048b", "9dedf41d995ff4446a181f143c3db98c", "3683d1af071c1744185ff93cba9db10b", "7ff088ab58c69854b82ea95c2b0e35b4",
                "1e69e9029c627914eb06608dad707b36", "62ec0b22425fb424c82fd52d7f4c02a5", "9b1538c732e06544bbd955fee570a2be", "4bc20fd0e137e1645a18f030b961ef3d", "ad0f81f6377180d4292a2316efb950f2", "49fe9e1969afd874181ed7613120c250", "583938eaafc820f49ad94eca1e5a98ca",
                "1d9b60d57afb45c4f9bb0a3c21bb3b98", "d45bfd838c541bb40bde7b0bf0e1b684", "51c51d841e9f16046a169729c13c4d4f", "a23bcee56c9fcf64d863dafedb369387", "15d7d6cbbf56bd744b37bbf9225ea83b",
                "d704f90f54f813043a525f304f6c0050", "9e9bab3020ec5f64499e007880b37e52", "d072b841ba0668846adeb007f623bd6c", "6a6a0901d799ceb49b33d4851ff72132", "746ee366e50611146821d61e391edf16"
            };
            foreach (string dependency in dependencies) Require<BlueprintScriptableObject>(dependency);
            CreateBastionOfGoodFeature();
            CreateHolyShieldAbilities();
            CreateSacredShieldBond();
            var archetype = new BlueprintArchetype
            {
                AssetGuid = BlueprintGuid.Parse(id), name = modName + "_SacredShieldArchetype",
                LocalizedName = new() { m_Key = "SacredShieldArchetype_DisplayName" },
                LocalizedDescription = new() { m_Key = "SacredShieldArchetype_Description" },
                LocalizedDescriptionShort = new() { m_Key = "Empty" },
                ReplaceStartingEquipment = true,
                m_StartingItems = new[] {
                    Require<BlueprintItem>("d7963e1fcf260c148877afd3252dbc91").ToReference<BlueprintItemReference>(),
                    Require<BlueprintItem>("d52566ae8cbe8dc4dae977ef51c27d91").ToReference<BlueprintItemReference>(),
                    Require<BlueprintItem>("533e10c8b4c6a4940a3767d096f4f05d").ToReference<BlueprintItemReference>(),
                    Require<BlueprintItem>("f4cef3ba1a15b0f4fa7fd66b602ff32b").ToReference<BlueprintItemReference>() },
                m_ParentClass = paladin,
                AddFeatures = new[] {
                    Entry(1, BOGAbility1), Entry(4, SHAbility4), Entry(5, SSFeature), Entry(8, BondPlus2),
                    Entry(9, AdditionalUsesFeature), Entry(11, BOGAbility11, SHAbility11, BondPlus3),
                    Entry(13, AdditionalUsesFeature), Entry(14, BondPlus4), Entry(17, AdditionalUsesFeature, BondPlus5),
                    Entry(20, BOGAbility20, SHAbility20, BondPlus6) },
                RemoveFeatures = new[] {
                    Entry(1, Require<BlueprintFeature>("3a6db57fce75b0244a6a5819528ddf26")),
                    Entry(4, Require<BlueprintFeature>("cb6d55dda5ab906459d18a435994a760")),
                    Entry(5, Require<BlueprintFeature>("ad7dc4eba7bf92f4aba23f716d7a9ba6")),
                    Entry(11, Require<BlueprintFeature>("9f13fdd044ccb8a439f27417481cb00e")),
                    Entry(20, Require<BlueprintFeature>("eff3b63f744868845a2f511e9929f0de")) }
            };
            archetype.AddToCache();
            AppendGroup(progression, "0f5c99ffb9c084545bbbe960b825d137", BOGAbility1, BOGAbility11, BOGAbility20);
            AppendGroup(progression, "cb6d55dda5ab906459d18a435994a760", SHAbility4, SHAbility11, SHAbility20);
            AppendGroup(progression, "1c7cdc1605554954f838d85bbdd22d90", SSFeature, BondPlus2, BondPlus3, BondPlus4, BondPlus5, BondPlus6);
            AppendGroup(progression, AdditionalUsesFeature.AssetGuid.ToString(), AdditionalUsesFeature);
            var archetypes = paladin.m_Archetypes ?? Array.Empty<BlueprintArchetypeReference>();
            if (!archetypes.Any(reference => reference.deserializedGuid == archetype.AssetGuid))
                paladin.m_Archetypes = archetypes.Append(archetype.ToReference<BlueprintArchetypeReference>()).ToArray();
        }
        private static T Require<T>(string id) where T : BlueprintScriptableObject =>
            ResourcesLibrary.TryGetBlueprint<T>(id) ?? throw new InvalidOperationException("Sacred Shield dependency missing/wrong type: " + id);
        private static LevelEntry Entry(int level, params BlueprintFeature[] features) => new()
        {
            Level = level, m_Features = features.Select(feature => feature.ToReference<BlueprintFeatureBaseReference>()).ToList()
        };
        private static void AppendGroup(BlueprintProgression progression, string marker, params BlueprintFeature[] features)
        {
            var groups = (progression.UIGroups ?? Array.Empty<UIGroup>()).ToList();
            var group = groups.FirstOrDefault(candidate => candidate.m_Features != null && candidate.m_Features.Any(reference => reference.deserializedGuid == BlueprintGuid.Parse(marker)));
            if (group == null) { group = new UIGroup { m_Features = new List<BlueprintFeatureBaseReference>() }; groups.Add(group); }
            foreach (BlueprintFeature feature in features)
                if (!group.m_Features.Any(reference => reference.deserializedGuid == feature.AssetGuid))
                    group.m_Features.Add(feature.ToReference<BlueprintFeatureBaseReference>());
            progression.UIGroups = groups.ToArray();
        }
    }
}