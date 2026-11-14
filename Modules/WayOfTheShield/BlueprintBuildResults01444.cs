using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Way_of_the_shield.NewComponents;
using Way_of_the_shield.NewFeatsAndAbilities;

namespace Way_of_the_shield
{
    internal enum BlueprintBuildStep01444
    {
        Proficiency, BucklerParry, BucklerGrip, ShieldBash, CouchedSpears,
        ShieldedDefense, ShieldBrace, UnhinderingShield, UpsettingStyle, MythicParry, SacredShield
    }

    // The upstream builders return void, and several return normally when a
    // donor is missing. Their return or their Created flag is not a commit.
    // Guard donors before builders which publish before checking them; check
    // the actual published graph and native attachment before recording success.
    internal static class BlueprintBuildResults01444
    {
        const string ShieldProficiency = "cb8686e7357a68c42bdd9d4e65334633";
        const string BucklerProficiency = "7c28228ce4eed1543a6b670fd2a88e72";
        const string ShieldBashAbility = "3bb6b76ed5b38ab4f957c7f923c23b68";
        const string ShieldBashFeature = "121811173a614534e8720d7550aae253";
        const string Defensive = "ca22afeb94442b64fb8536e7a9f7dc11";
        const string BucklerParryFeature = "e547039b227a42d7a8d98fad72f8717c";
        const string ParryAbility = "a86db1a253d0446b857938314af1ae51";
        const string BashWrapper = "f42adaab0f24462c87a7875c259ffccb";
        const string DefenseFeature = "76c121e7d9644d77948fe031b0eafe21";
        const string DefenseAbility = "7ca07f3bdbf145c2a975b11d9690c0b3";
        const string MythicDefense = "3d621329f6874035b4be27edc6b84f25";
        const string MythicParry = "0a1c1d2138964c97ba8f48d7843ea685";

        public static void Preflight(BlueprintBuildStep01444 step)
        {
            switch (step)
            {
                case BlueprintBuildStep01444.BucklerParry:
                    Require<BlueprintActivatableAbility>(ShieldBashAbility);
                    RequireSelections(UnhinderingShield.selections);
                    if (AddBucklerParry.GetValue()) RequireFeatures(BucklerProficiency, ShieldBashFeature);
                    break;
                case BlueprintBuildStep01444.BucklerGrip:
                    if (AllowTwoHanded_as_OneHandedWhenBuckler.GetValue())
                    {
                        var defensive = Require<BlueprintFeature>(Defensive);
                        if (defensive.GetComponent<AddFacts>()?.m_Facts == null)
                            throw new InvalidOperationException("Way of the Shield dependency missing: FightDefensively.AddFacts.m_Facts");
                        Require<BlueprintShieldType>("26fcc43f7d20374498d2e1643381d345");
                    }
                    break;
                case BlueprintBuildStep01444.ShieldBash:
                    if (AllowShieldBashToAllWhoProficient.GetValue())
                    {
                        Require<BlueprintActivatableAbility>(ShieldBashAbility);
                        RequireFeatures(ShieldBashFeature, ShieldProficiency, BucklerProficiency);
                        Require<BlueprintBuff>("5566971fdebf7fe468a497bbee0d3ed5");
                    }
                    break;
                case BlueprintBuildStep01444.ShieldedDefense:
                    RequireFeatures("dbec636d84482944f87435bd31522fcc");
                    if (AddShieldedDefense.GetValue())
                    {
                        Require<BlueprintActivatableAbility>(ShieldBashAbility);
                        RequireFeatures(ShieldProficiency, "82fbdd5eb5ac73b498c572cc71bda48f", "94fe0ca10f17bf143a7d2bd2ab2acdda");
                        RequireMythicSelection();
                    }
                    break;
                case BlueprintBuildStep01444.ShieldBrace:
                case BlueprintBuildStep01444.UnhinderingShield:
                    RequireFeatures("ac57069b6bf8c904086171683992a92a", "3c380607706f209499d951b29d3c44f3");
                    Require<BlueprintCharacterClass>("48ac8db94d5de7645906c7d0ad3bcfbd");
                    RequireSelections(step == BlueprintBuildStep01444.ShieldBrace ? ShieldBrace.CachePatch.selections : UnhinderingShield.selections);
                    // The upstream TTT Shield Mastery hook is optional. Its
                    // absence is not a missing native dependency.
                    break;
                case BlueprintBuildStep01444.UpsettingStyle:
                    RequireSelections(UpsettingShieldStyle.selections);
                    // Missing Shield Bash / Combat Reflexes returns before any
                    // writes. Let that path run, then reject its absent result.
                    break;
                case BlueprintBuildStep01444.MythicParry:
                    if (AddBucklerParry.GetValue())
                    {
                        RequireMythicSelection();
                        RequireFeatures("ac8aaf29054f5b74eb18f2af950e752d", BucklerProficiency, ShieldProficiency);
                    }
                    break;
                // Proficiency and mounted spear are already
                // safely resumable; Sacred Shield preflights its own donors.
            }
        }

        public static bool IsComplete(BlueprintBuildStep01444 step)
        {
            switch (step)
            {
                case BlueprintBuildStep01444.Proficiency:
                    var proficiency = Get<BlueprintFeature>(ShieldProficiency);
                    return proficiency != null && new[] { WeaponCategory.WeaponLightShield, WeaponCategory.WeaponHeavyShield, WeaponCategory.SpikedLightShield, WeaponCategory.SpikedHeavyShield }
                        .All(category => proficiency.GetComponents<AddProficiencies>().Any(c => (c.WeaponProficiencies ?? Array.Empty<WeaponCategory>()).Contains(category)));
                case BlueprintBuildStep01444.BucklerParry:
                    return Published("414f40680af64050a2a9dde3dede32ac", "dc65131392614eecba8b399e279c3915", "a8f2b254bc2e4f8b95e12aa444287c58", BucklerParryFeature, ParryAbility, LightShieldParry.Guid)
                        && Restricts(ShieldBashAbility, ParryAbility)
                        && Selected(UnhinderingShield.selections, LightShieldParry.Guid)
                        && (!AddBucklerParry.GetValue() || Grants(BucklerProficiency, BucklerParryFeature));
                case BlueprintBuildStep01444.BucklerGrip:
                    return Published("eb322fe21ec64a928ace4eaedb6d4339")
                        && (!AllowTwoHanded_as_OneHandedWhenBuckler.GetValue() || Grants(Defensive, "eb322fe21ec64a928ace4eaedb6d4339"));
                case BlueprintBuildStep01444.ShieldBash:
                    return Published(BashWrapper) && Grants(BashWrapper, ShieldBashAbility)
                        && (!AllowShieldBashToAllWhoProficient.GetValue() ||
                            (new[] { ShieldBashFeature, ShieldProficiency, BucklerProficiency }.All(id => Grants(id, BashWrapper))
                             && Get<BlueprintBuff>("5566971fdebf7fe468a497bbee0d3ed5")?.GetComponents<AddInitiatorShieldBashTrigger>().Any(c => c.name == "WayOfTheShield_ShieldBashLoseAC") == true));
                case BlueprintBuildStep01444.CouchedSpears:
                    return !AllowTwoHandedSpears_as_OneHandedWhenMounted.GetValue() ||
                        (Published("d02e3b77b5234c0ea7e181db362d45cd") && Grants(Defensive, "d02e3b77b5234c0ea7e181db362d45cd"));
                case BlueprintBuildStep01444.ShieldedDefense:
                    return Published(DefenseFeature, "fa65cbcf6bba4f3992261173381a3874", "29397505648a462a874872b132a00b75", "807ca5b2501444aab22a0f08a64691e1", DefenseAbility, MythicDefense)
                        && (!AddShieldedDefense.GetValue() || (Restricts(ShieldBashAbility, DefenseAbility)
                            && new[] { ShieldProficiency, "82fbdd5eb5ac73b498c572cc71bda48f", "94fe0ca10f17bf143a7d2bd2ab2acdda" }.All(id => Grants(id, DefenseFeature))
                            && InSelection(MythicSelection(), MythicDefense)));
                case BlueprintBuildStep01444.ShieldBrace:
                    return Published("bacc4f29695d4aec868d31be1c04f29b") && Selected(ShieldBrace.CachePatch.selections, "bacc4f29695d4aec868d31be1c04f29b");
                case BlueprintBuildStep01444.UnhinderingShield:
                    return Published("39470145204746cdb46cd95bcee4a92c") && Selected(UnhinderingShield.selections, "39470145204746cdb46cd95bcee4a92c");
                case BlueprintBuildStep01444.UpsettingStyle:
                    return Published("dfcbc91d3c2d4f8685acf158dcb58815", "fc452aa2ff72401f943a133706b35acc", "7da0bf0d789d4528b4c3b707eb98fbd2", "836dafe9db094691bce5dc0c6b0305ac", "c748ba6f94fb4dd68e5aebbe8562a80b", "725ff3821a1242a8bf9e15429319152d", "5a698c8b9ebe4ce49f21d965a8723786", "8256f265fae94c7a891a23e796d23956", "f66c775241f54f79a520f25ef3c0887e")
                        && new[] { "7da0bf0d789d4528b4c3b707eb98fbd2", "836dafe9db094691bce5dc0c6b0305ac", "c748ba6f94fb4dd68e5aebbe8562a80b" }.All(id => Selected(UpsettingShieldStyle.selections, id));
                case BlueprintBuildStep01444.MythicParry:
                    return Published(MythicParry) && (!AddBucklerParry.GetValue() || (InSelection(MythicSelection(), MythicParry)
                        && new[] { "ac8aaf29054f5b74eb18f2af950e752d", BucklerProficiency, ShieldProficiency }.All(id =>
                            Get<BlueprintFeature>(id)?.IsPrerequisiteFor?.Any(r => r?.deserializedGuid == BlueprintGuid.Parse(MythicParry)) == true)));
                case BlueprintBuildStep01444.SacredShield:
                    const string archetypeId = "56f19f65e28b4c3fb03e425fb047e08a";
                    var archetype = Get<BlueprintArchetype>(archetypeId);
                    return Published(archetypeId) && archetype?.AddFeatures?.Length > 0
                        && archetype.AddFeatures.All(e => e.m_Features != null && e.m_Features.All(r => r != null && Published(r.deserializedGuid.ToString())))
                        && Get<BlueprintCharacterClass>("bfa11238e7ae3544bbeb4d0b92e897ec")?.m_Archetypes?.Any(r => r?.deserializedGuid == archetype.AssetGuid) == true;
                default: throw new ArgumentOutOfRangeException(nameof(step));
            }
        }

        static T Get<T>(string id) where T : BlueprintScriptableObject => ResourcesLibrary.TryGetBlueprint<T>(id);
        static T Require<T>(string id) where T : BlueprintScriptableObject => Get<T>(id)
            ?? throw new InvalidOperationException("Way of the Shield dependency missing/wrong type: " + id + " (" + typeof(T).Name + ")");
        static void RequireFeatures(params string[] ids) { foreach (var id in ids) Require<BlueprintFeature>(id); }
        static void RequireSelections(IEnumerable<(string guid, string name)> selections)
        {
            foreach (var pair in selections) Require<BlueprintFeatureSelection>(pair.guid);
        }
        static BlueprintFeatureSelection MythicSelection()
        {
            var selection = ResourcesLibrary.GetRoot()?.SystemMechanics?.MythicFeatSelection;
            return selection != null && ReferenceEquals(ResourcesLibrary.BlueprintsCache.Load(selection.AssetGuid), selection) ? selection : null;
        }
        static void RequireMythicSelection()
        {
            if (MythicSelection() == null) throw new InvalidOperationException("Way of the Shield dependency missing: root MythicFeatSelection");
        }
        static bool Published(params string[] ids) => ids.All(id =>
        {
            var guid = BlueprintGuid.Parse(id);
            return Owned.TryGetValue(guid, out var asset) && asset != null && ReferenceEquals(ResourcesLibrary.BlueprintsCache.Load(guid), asset);
        });
        static bool Grants(string source, string fact) => Get<BlueprintFeature>(source)?.GetComponents<AddFacts>()
            .Any(c => (c.m_Facts ?? Array.Empty<BlueprintUnitFactReference>()).Any(r => r?.deserializedGuid == BlueprintGuid.Parse(fact))) == true;
        static bool Restricts(string source, string ability) => Get<BlueprintActivatableAbility>(source)?.GetComponents<RestrictionOtherActivatables>()
            .Any(c => (c.m_ActivatableAbilities ?? Array.Empty<BlueprintActivatableAbilityReference>()).Any(r => r?.deserializedGuid == BlueprintGuid.Parse(ability))) == true;
        static bool InSelection(BlueprintFeatureSelection selection, string id) => selection?.m_AllFeatures?.Any(r => r?.deserializedGuid == BlueprintGuid.Parse(id)) == true;
        static bool Selected(IEnumerable<(string guid, string name)> selections, string id) => selections.All(pair => InSelection(Get<BlueprintFeatureSelection>(pair.guid), id));
    }
}
