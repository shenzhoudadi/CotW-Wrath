using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.SpiritClassBridge0117;

namespace CallOfTheWild.Wrath.Prototype
{
    // COTW-0117 F-0117. Wizard archetype: one Spirit Link, ten 1/8/20 spirit
    // progressions, three Spirit Hex selections with the Wizard bonus-feat
    // fallback, familiar-only Arcane Bond and a private Extra Channel clone.
    // Registration is atomic: any missing required provider OR any contract
    // effect that cannot be built from native Wrath components rejects the
    // whole package without a single private blueprint or shared class edit.
    internal static class SpiritWhisperer0117
    {
        internal const string ModuleName = "0117 Spirit Whisperer";
        internal const string RootToken = "SpiritWhisperer0117Archetype";

        private const string WizardId = "ba34257984f4c41408ce1dc2004e342e";
        private const string SchoolId = "5f838049069f1ac4d804ce0862ab5110";
        private const string BondId = "03a1781486ba98043afddaabf6b7d8ff";
        private const string WizardFeatId = "8c3102c2ff3b69444b139a98521a4899";
        private const string FamiliarId = "363cab72f77c47745bf3a8807074d183";
        private const string FamiliarIconId = "1194129055923ae46a6b876ff6a30358";
        private const string ExtraChannelId = "cd9f19775bd9d3343a31a065e93f0c47";
        private const string FaerieFireBuffId = "cc383a9eaae4d2b45a925d442b367b54";
        private const string GlitterdustId = "ce7dad2b25acf85429b6c9550787b2d9";
        private const string LifeLinkBuffId = "598200ff68114111ba44d2fa08750771";
        private const string LoreIconId = "f0455c9295b53904f9e02fc571dd2ce1"; // owl's wisdom

        internal static readonly string[] PoolIds =
        {
            "247a4068296e8be42890143f451b4b45",
            "d332c1748445e8f4f9e92763123e31bd",
            "a21acdafc0169f5488a9bd3256e2e65b"
        };

        // A hex either clones a real Wrath donor feature (Donor != null) or is
        // built privately from native components (Donor == null). Resource is
        // the contract shape: 0 passive, 2 = R2 (1+Cha), 5 = R5 (1/Wizard level).
        private sealed class HexSpec
        {
            internal string Name;
            internal string Donor;
            internal int Resource;
            internal HexSpec(string name, string donor, int resource) { Name = name; Donor = donor; Resource = resource; }
        }

        private sealed class Row
        {
            internal string Name;
            internal string[] Tiers;          // L1, L8, L20 donor GUIDs; null = private build
            internal HexSpec[] Hexes;         // full contract hex list
            internal int[] Resource;          // 0 none, 1 R1, 2 R2, 3 R3, 4 R4
            internal string[] TierNames;
            internal bool ChannelDc;          // Life channel uses Charisma for DC
            internal string PrivateSpirit;    // "Heavens" | "Lore": no native Wrath spirit
            internal string L8Private;        // "healersTouch" | null
        }

        // Wrath donor identities were read offline from the game's own blueprint
        // archive; see B-native-donor-map.json. Heavens and Lore have no native
        // Wrath spirit, so their three tiers are built privately (Tiers = null)
        // and never clone the Shaman frost-spirit GUIDs.
        private static readonly Row[] Rows =
        {
            new Row { Name = "Battle",
                Tiers = new[] { "5e1161a3a7e5a83458de8a5f412c8d0c", "cc6b293550ac66c48a0c787b5e986a34", "1ddcc62d55d24285be8c687c94180578" },
                Hexes = new[] {
                    new HexSpec("Battle Master", "4db9f3d913a59f14f9cc4ffaa73de2f0", 0),
                    new HexSpec("Battle Ward", "6f60723288b405c48af821cf995e4389", 0),
                    new HexSpec("Curse of Suffering", null, 0),
                    new HexSpec("Hampering Hex", "36f07f59f983cd042ae0c8c91e4dcea6", 0) },
                Resource = new[] { 1, 1, 0 }, TierNames = new[] { "Battle Spirit", "Enemies' Bane", "Manifestation" } },
            new Row { Name = "Bones",
                Tiers = new[] { "f556c44606e5eaa47bac31aae9ebc96d", "3f8db74aafbcaff40ae3f2cf5192ca4f", "10bb6847ee6b4cbf86dded2d86b76ab6" },
                Hexes = new[] {
                    new HexSpec("Bone Lock", "e506667113c93f94a962a90adedcdb03", 0),
                    new HexSpec("Bone Ward", "96a99ea4866da4b4cbd51b81f1a61a72", 0),
                    new HexSpec("Fearful Gaze", "89603c7a9ba690d46b95148b46a34f7d", 0),
                    new HexSpec("Deathly Being", null, 0) },
                Resource = new[] { 1, 3, 4 }, TierNames = new[] { "Touch of the Grave", "Shard Soul", "Manifestation" } },
            new Row { Name = "Flame",
                Tiers = new[] { "ee5503b51c0c9684cb7300d28e3fc4e1", "c2a4ce87f8600db41a9a74d33af87c2c", "e508bc6f64d64fc197ace10d66084b6d" },
                Hexes = new[] {
                    new HexSpec("Fire Nimbus", "24074b42608fff54f8e1a5b7fa4a7d29", 0),
                    new HexSpec("Flame Curse", "dec5607ab6ea68d4aac0772049e80be7", 0),
                    new HexSpec("Ward of Flames", "c95e1ccf872844b44a47378f761689be", 0),
                    new HexSpec("Cinder Dance", "6e67eae3081853544b191943f5ed4534", 0) },
                Resource = new[] { 1, 3, 0 }, TierNames = new[] { "Touch of Flame", "Fiery Soul", "Manifestation" } },
            new Row { Name = "Heavens",
                Tiers = new string[] { null, null, null },
                Hexes = new[] {
                    new HexSpec("Enveloping Void", null, 0),
                    new HexSpec("Lure of Heavens", null, 0),
                    new HexSpec("Heaven's Leap", null, 0),
                    new HexSpec("Starburn", null, 2) },
                Resource = new[] { 1, 0, 0 }, TierNames = new[] { "Stardust", "Void Adaptation", "Manifestation" },
                PrivateSpirit = "Heavens" },
            new Row { Name = "Life",
                Tiers = new[] { "9ae3ba76663a19141a3996c1e3ce95e3", "8cae5b62ad604de41bf3e9f4489c6b8f", "83a2f66218554ea3ada1aceacb4aad54" },
                Hexes = new[] {
                    new HexSpec("Curse of Suffering", null, 0),
                    new HexSpec("Enhanced Cures", "973a22b02c793ca49b48652e3d70ae80", 0),
                    new HexSpec("Life Link", "ef97c9bcc1c54ea7993ef8b2489c908a", 5),
                    new HexSpec("Life Sight", null, 5) },
                Resource = new[] { 2, 0, 0 }, TierNames = new[] { "Channel Positive Energy", "Healer's Touch", "Manifestation" },
                ChannelDc = true, L8Private = "healersTouch" },
            new Row { Name = "Lore",
                Tiers = new string[] { null, null, null },
                Hexes = new[] {
                    new HexSpec("Benefit of Wisdom", null, 0),
                    new HexSpec("Brain Drain", null, 0),
                    new HexSpec("Confusion Curse", null, 0),
                    new HexSpec("Mental Acuity", "736d3d5d940736b4e87792e94eaff96b", 0) },
                Resource = new[] { 1, 0, 4 }, TierNames = new[] { "Monstrous Insight", "Perfect Knowledge", "Manifestation" },
                PrivateSpirit = "Lore" },
            new Row { Name = "Nature",
                Tiers = new[] { "0a7166e9c6ea5874e9fc4984f30f2d8d", "f54191d614633da4baac7a47a659746b", "f0bf97f286bb44a686108a97988de89c" },
                Hexes = new[] {
                    new HexSpec("Entangling Curse", "176ee8bedc899964a96ef1b7478dcce7", 0),
                    new HexSpec("Erosion Curse", null, 0),
                    new HexSpec("Friend to Animals", "0d92799cecf414249a23ad82f7e9901a", 0),
                    new HexSpec("Stormwalker", null, 0) },
                Resource = new[] { 1, 0, 4 }, TierNames = new[] { "Storm Burst", "Spirit of Nature", "Manifestation" } },
            new Row { Name = "Stone",
                Tiers = new[] { "5c3ccab7cb27f4a408531197eb2abd3f", "ef692472df1b10f4caec90b4984965ef", "936734437ee8493497760174a0543045" },
                Hexes = new[] {
                    new HexSpec("Stone Stability", "05b6b99b0f394284899eccd4728031b7", 0),
                    new HexSpec("Loadstone", null, 0),
                    new HexSpec("Metal Curse", "ea8e9fef24350684d82b38fec2cc3139", 0),
                    new HexSpec("Ward of Stone", null, 0) },
                Resource = new[] { 1, 3, 0 }, TierNames = new[] { "Touch of Acid", "Body of Earth", "Manifestation" } },
            new Row { Name = "Waves",
                Tiers = new[] { "62f408b7561adb34b899363c905fa13a", "b2387cc5ccce4f9428ffd069aa03d029", "600780f354f24984b2fe210e2b54ad57" },
                Hexes = new[] {
                    new HexSpec("Beckoning Chill", "905a9eccb9fa73e4da1d3e86bcb1e421", 0),
                    new HexSpec("Crashing Waves", null, 0),
                    new HexSpec("Mist's Shroud", null, 0) },
                Resource = new[] { 1, 3, 0 }, TierNames = new[] { "Ice Splinter", "Frigid Blast", "Manifestation" } },
            new Row { Name = "Wind",
                Tiers = new[] { "5d810adea03fb644582eb74de32c75ec", "59550028efc886a4e8ca2c80b680642f", "a8c0403a27b6471f85d2bc441db5a993" },
                Hexes = new[] {
                    new HexSpec("Air Barrier", "4b5b0f429730a984fb56fc3e34e5231e", 0),
                    new HexSpec("Sparkling Aura", null, 0),
                    new HexSpec("Vortex Spells", "d54e52f0e3d086f468ede43222e053e9", 0),
                    new HexSpec("Wind Ward", "2a7f7b35e1a0fb44ba7f01e28fa7a535", 0) },
                Resource = new[] { 1, 3, 0 }, TierNames = new[] { "Shocking Touch", "Spark Soul", "Manifestation" } },
        };

        private sealed class State
        {
            internal BlueprintGuid Root;
            internal string[] Definitions;
            internal BlueprintScriptableObject[] Assets;
            internal BlueprintCharacterClass Wizard;
        }

        private static readonly ConditionalWeakTable<BlueprintsCache, State> Published =
            new ConditionalWeakTable<BlueprintsCache, State>();

        private static IEnumerable<string> RequiredProviders()
        {
            yield return WizardId; yield return SchoolId; yield return BondId; yield return WizardFeatId;
            yield return FamiliarId; yield return ExtraChannelId;
            yield return FaerieFireBuffId; yield return GlitterdustId;
            foreach (var p in PoolIds) yield return p;
            foreach (var row in Rows)
            {
                foreach (var t in row.Tiers) if (!string.IsNullOrEmpty(t)) yield return t;
                foreach (var h in row.Hexes) if (!string.IsNullOrEmpty(h.Donor)) yield return h.Donor;
            }
        }

        // Every original effect now has a native Wrath component or a bounded
        // private handler, so the gap table is empty and the package publishes.
        // The rejection path stays: a future genuine gap must reject the whole
        // package before a single private blueprint is staged ("all ten spirits
        // or nothing").
        private static List<string> ContractGaps(BlueprintsCache cache)
        {
            return new List<string>();
        }

        private static readonly Dictionary<string, bool> TypePresence = new Dictionary<string, bool>();

        private static bool ComponentTypePresent(string typeName)
        {
            if (TypePresence.TryGetValue(typeName, out var cached)) return cached;
            bool found = FindType(typeName, typeof(BlueprintComponent)) != null;
            TypePresence[typeName] = found;
            return found;
        }

        private static Type FindType(string typeName, Type baseType)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException error) { types = error.Types; }
                foreach (var t in types)
                    if (t != null && t.Name == typeName && (baseType == null || baseType.IsAssignableFrom(t)))
                        return t;
            }
            return null;
        }

        internal static bool Register(BlueprintsCache cache)
        {
            if (Published.TryGetValue(cache, out var state))
            {
                if (!ReferenceEquals(cache.Load(state.Root), state.Assets[0]))
                    throw new InvalidOperationException("0117 published archetype identity changed.");
                for (int i = 0; i < state.Assets.Length; i++)
                    if (!ReferenceEquals(cache.Load(state.Assets[i].AssetGuid), state.Assets[i]) ||
                        ArchetypeBatch01445.Definition(state.Assets[i]) != state.Definitions[i])
                        throw new InvalidOperationException("0117 published private blueprint changed: " + state.Assets[i].name);
                var refs = (BlueprintArchetypeReference[])Get(state.Wizard, "m_Archetypes") ?? Array.Empty<BlueprintArchetypeReference>();
                if (refs.Count(r => r != null && r.Guid == state.Root) != 1)
                    throw new InvalidOperationException("0117 wizard archetype link changed.");
                return true;
            }

            var missing = RequiredProviders().FirstOrDefault(g => cache.Load(BlueprintGuid.Parse(g)) == null);
            if (missing != null) { Log("reject: 0117 missing provider " + missing); return false; }

            var wizard = cache.Load(BlueprintGuid.Parse(WizardId)) as BlueprintCharacterClass;
            if (wizard == null || wizard.Progression == null) { Log("reject: 0117 wizard progression absent"); return false; }

            // Whole-package rejection on a genuine Wrath effect gap. This runs
            // before any Factory is created, so no private blueprint, archetype
            // link, UI group or feat-pool edit can leak from a rejected package.
            var gaps = ContractGaps(cache);
            if (gaps.Count > 0) { Log("reject: 0117 contract gap: " + string.Join(" | ", gaps)); return false; }

            var f = new Factory(cache);
            var rootRef = (BlueprintArchetypeReference[])Get(wizard, "m_Archetypes");
            var uiGroups = wizard.Progression.UIGroups;
            var determinators = (BlueprintFeatureBaseReference[])Get(wizard.Progression, "m_UIDeterminatorsGroup");
            var pools = PoolIds.Select(g => cache.Load(BlueprintGuid.Parse(g)) as BlueprintFeatureSelection).ToArray();
            if (pools.Any(p => p == null)) { Log("reject: 0117 feat pool missing"); return false; }
            var poolSnapshots = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();

            BlueprintArchetype root = null;
            try
            {
                root = Build(f, wizard, cache);
                var assets = new[] { (BlueprintScriptableObject)root }.Concat(
                    f.Assets.Values.Where(a => !ReferenceEquals(a, root))).ToArray();
                foreach (var a in assets)
                    if (cache.Load(a.AssetGuid) != null)
                        throw new InvalidOperationException("private GUID occupied: " + a.name);

                var undo = new Action(() =>
                {
                    Set(wizard, "m_Archetypes", rootRef);
                    wizard.Progression.UIGroups = uiGroups;
                    Set(wizard.Progression, "m_UIDeterminatorsGroup", determinators);
                    for (int i = 0; i < pools.Length; i++)
                    {
                        Set(pools[i], "m_Features", poolSnapshots[i][0]);
                        Set(pools[i], "m_AllFeatures", poolSnapshots[i][1]);
                    }
                });

                try
                {
                    foreach (var a in assets) Prepare(a);
                    f.Enable();
                    foreach (var a in assets) cache.AddCachedBlueprint(a.AssetGuid, a);
                    foreach (var a in assets)
                        if (!ReferenceEquals(cache.Load(a.AssetGuid), a))
                            throw new InvalidOperationException("private blueprint not published: " + a.name);

                    var spiritLink = f.Find<BlueprintFeatureSelection>("SpiritLink");
                    var familiar = f.Find<BlueprintFeatureSelection>("FamiliarOnlyBond");
                    var hexSelection = f.Find<BlueprintFeatureSelection>("SpiritHex");
                    var extra = f.Find<BlueprintFeature>("ExtraChannel");

                    var refs = rootRef ?? Array.Empty<BlueprintArchetypeReference>();
                    if (!refs.Any(r => r != null && r.Guid == root.AssetGuid))
                        Set(wizard, "m_Archetypes", refs.Concat(new[] { Ref<BlueprintArchetypeReference>(root) }).ToArray());
                    Set(wizard.Progression, "m_UIDeterminatorsGroup", (determinators ?? Array.Empty<BlueprintFeatureBaseReference>())
                        .Concat(new[] { Ref<BlueprintFeatureBaseReference>(spiritLink) }).ToArray());
                    wizard.Progression.UIGroups = (uiGroups ?? Array.Empty<UIGroup>())
                        .Concat(new[] { Group(familiar, hexSelection, hexSelection, hexSelection) }).ToArray();

                    var nativeExtraGuid = BlueprintGuid.Parse(ExtraChannelId);
                    for (int i = 0; i < pools.Length; i++)
                    {
                        var all = (BlueprintFeatureReference[])Get(pools[i], "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
                        // The clone may only enter pools that already offer the
                        // native Extra Channel feat (same gate as Sunsinger).
                        if (!all.Any(r => r != null && r.Guid == nativeExtraGuid)) continue;
                        foreach (string field in new[] { "m_Features", "m_AllFeatures" })
                        {
                            var old = (BlueprintFeatureReference[])Get(pools[i], field) ?? Array.Empty<BlueprintFeatureReference>();
                            if (!old.Any(r => r != null && r.Guid == extra.AssetGuid))
                                Set(pools[i], field, old.Concat(new[] { Ref<BlueprintFeatureReference>(extra) }).ToArray());
                        }
                    }

                    Published.Add(cache, new State
                    {
                        Root = root.AssetGuid,
                        Assets = assets,
                        Definitions = assets.Select(ArchetypeBatch01445.Definition).ToArray(),
                        Wizard = wizard
                    });
                    foreach (var line in f.Diagnostics) Log("0117 diagnostic " + line);
                    Log("0117 published " + root.AssetGuid);
                    return true;
                }
                catch
                {
                    undo();
                    foreach (var a in assets)
                        if (ReferenceEquals(cache.Load(a.AssetGuid), a)) cache.RemoveCachedBlueprint(a.AssetGuid);
                    throw;
                }
            }
            catch (Exception error)
            {
                Log("reject: 0117 " + error.GetType().Name + ": " + error.Message + "\n" + error.StackTrace);
                return false;
            }
        }

        private static BlueprintArchetype Build(Factory f, BlueprintCharacterClass wizard, BlueprintsCache cache)
        {
            var school = f.Native<BlueprintFeatureSelection>(SchoolId);
            var bond = f.Native<BlueprintFeatureSelection>(BondId);
            var wizardFeat = f.Native<BlueprintFeatureSelection>(WizardFeatId);

            // Spirit Link: exactly one of ten progressions, never a second or
            // wandering spirit and no daily switching.
            var link = Selection(f, "SpiritLink", "Spirit Link",
                "At 1st level a spirit whisperer forms a bond with one spirit. He uses his wizard level as his shaman level for these abilities and his Intelligence modifier in place of Wisdom. He does not gain spirit magic spells or the shaman true spirit ability.",
                Array.Empty<BlueprintFeature>());

            var hexMaster = Selection(f, "SpiritHex", "Spirit Hex",
                "At 5th, 10th and 15th level a spirit whisperer selects one hex from his chosen spirit, or a wizard bonus feat. Wizard level and Intelligence replace shaman level and Wisdom.",
                Array.Empty<BlueprintFeature>());

            var progressions = new List<BlueprintProgression>();
            var hexSubs = new List<BlueprintFeature>();

            foreach (var row in Rows)
            {
                var features = new BlueprintFeature[3];
                for (int tier = 0; tier < 3; tier++)
                    features[tier] = Tier(f, cache, wizard, row, tier);

                var prog = f.Progression(row.Name + "Progression", row.Name + " Spirit",
                    "Spirit Whisperer " + row.Name + " spirit. Wizard level and Intelligence drive this progression.",
                    new[] { Entry(1, features[0]), Entry(8, features[1]), Entry(20, features[2]) },
                    new[] { Group(features[0], features[1], features[2]) });
                SetClasses(prog, wizard);
                Icon(prog, row.PrivateSpirit != null ? PrivateIcon(f, row) : f.Native<BlueprintUnitFact>(row.Tiers[2]));
                progressions.Add(prog);

                var hexFeatures = row.Hexes.Select((hex, i) =>
                    BuildHexFeature(f, cache, wizard, row, hex, i)).ToArray();
                var sub = Selection(f, row.Name + "HexSelection", row.Name + " Spirit Hex",
                    "Select one hex granted by the " + row.Name + " spirit.", hexFeatures,
                    Migration01426.Require(prog));
                hexSubs.Add(sub);
            }

            Set(link, "m_Features", progressions.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(link, "m_AllFeatures", progressions.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(hexMaster, "m_Features", hexSubs.Cast<BlueprintFeature>().Concat(new[] { wizardFeat })
                .Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(hexMaster, "m_AllFeatures", hexSubs.Cast<BlueprintFeature>().Concat(new[] { wizardFeat })
                .Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(hexMaster, "IgnorePrerequisites", false);

            // Familiar-only Arcane Bond. The clone keeps the donor's familiar
            // options but drops every prerequisite component of its own.
            var familiarDonor = f.Native<BlueprintFeatureSelection>(FamiliarId);
            var familiar = (BlueprintFeatureSelection)f.Clone(familiarDonor, "FamiliarOnlyBond", "Arcane Bond",
                "A spirit whisperer must choose the familiar arcane bond. Bonded items are not available.");
            familiar.ComponentsArray = Array.Empty<BlueprintComponent>();
            Set(familiar, "m_Features", ((BlueprintFeatureReference[])Get(familiarDonor, "m_Features")
                ?? Array.Empty<BlueprintFeatureReference>()).ToArray());
            Set(familiar, "m_AllFeatures", ((BlueprintFeatureReference[])Get(familiarDonor, "m_AllFeatures")
                ?? Array.Empty<BlueprintFeatureReference>()).ToArray());
            var iconDonor = f.Native<BlueprintUnitFact>(FamiliarIconId);
            Icon(familiar, iconDonor ?? familiarDonor);

            // Extra Channel points only at this package's Life channel resource.
            var nativeExtra = f.Native<BlueprintFeature>(ExtraChannelId);
            var extra = (BlueprintFeature)f.Clone(nativeExtra, "ExtraChannel",
                "Extra Channel (Spirit Whisperer Life Spirit)",
                "You can channel the Life spirit's positive energy two additional times per day. Requires Channel Positive Energy from this archetype.");
            var increase = extra.ComponentsArray.FirstOrDefault(c => c != null && c.GetType().Name == "IncreaseResourceAmount");
            if (increase == null) throw new InvalidOperationException("native Extra Channel resource component changed.");
            var lifeChannel = f.Find<BlueprintFeature>("LifeTier0");
            var lifeResource = f.Find<BlueprintAbilityResource>("LifeR2");
            Set(increase, "m_Resource", Ref<BlueprintAbilityResourceReference>(lifeResource));
            Set(increase, "Value", 2);
            extra.ComponentsArray = extra.ComponentsArray.Where(c => !(c is Prerequisite))
                .Concat(new BlueprintComponent[] { Migration01426.Require(lifeChannel) }).ToArray();

            var root = f.Add(new BlueprintArchetype
            {
                AddFeatures = new[] { Entry(1, link, familiar), Entry(5, hexMaster), Entry(10, hexMaster), Entry(15, hexMaster) },
                RemoveFeatures = new[] { Entry(1, school, bond), Entry(5, wizardFeat), Entry(10, wizardFeat),
                                         Entry(15, wizardFeat), Entry(20, wizardFeat) },
                ComponentsArray = Array.Empty<BlueprintComponent>()
            }, RootToken, "Spirit Whisperer",
                "Spirit whisperers are wizards who commune with a single spirit through their familiar. They trade arcane school specialization and wizard bonus feats for spirit abilities, spirit hexes and a manifestation.");
            root.LocalizedName = Text(RootToken, "name", "Spirit Whisperer");
            root.LocalizedDescription = Text(RootToken, "description",
                "Spirit whisperers are wizards who commune with a single spirit through their familiar. Intelligence replaces Wisdom for spirit abilities; daily uses still follow Charisma where the original ability does.");
            Set(root, "m_ParentClass", wizard);
            foreach (var entry in root.AddFeatures.Concat(root.RemoveFeatures)) entry.Features.ToArray();
            return root;
        }

        private static BlueprintUnitFact PrivateIcon(Factory f, Row row)
        {
            // Heavens uses the glitterdust icon (original createSpiritWhispererSpirit);
            // Lore uses a loadable divination icon. Both are read-only native facts.
            var guid = row.PrivateSpirit == "Heavens" ? GlitterdustId : LoreIconId;
            return f.Native<BlueprintUnitFact>(guid) ?? f.Native<BlueprintUnitFact>(GlitterdustId);
        }

        private static BlueprintFeatureSelection Selection(Factory f, string token, string name, string description,
            BlueprintFeature[] features, params BlueprintComponent[] extra)
        {
            var s = f.Add(new BlueprintFeatureSelection { Ranks = 1, IgnorePrerequisites = false,
                ComponentsArray = extra ?? Array.Empty<BlueprintComponent>() }, token, name, description);
            Set(s, "m_Features", features.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(s, "m_AllFeatures", features.Select(Ref<BlueprintFeatureReference>).ToArray());
            return s;
        }

        private static void SetClasses(BlueprintProgression progression, BlueprintCharacterClass wizard)
        {
            var field = AccessTools.Field(typeof(BlueprintProgression), "m_Classes");
            if (field == null) return;
            var element = field.FieldType.IsArray ? field.FieldType.GetElementType() : null;
            if (element == null) return;
            var row = Activator.CreateInstance(element);
            foreach (var name in new[] { "m_Class", "Class" })
                if (AccessTools.Field(element, name) != null)
                    AccessTools.Field(element, name).SetValue(row, Ref<BlueprintCharacterClassReference>(wizard));
            if (AccessTools.Field(element, "AdditionalLevel") != null)
                AccessTools.Field(element, "AdditionalLevel").SetValue(row, 0);
            var array = Array.CreateInstance(element, 1);
            array.SetValue(row, 0);
            field.SetValue(progression, array);
            var archetypesField = AccessTools.Field(typeof(BlueprintProgression), "m_Archetypes");
            if (archetypesField != null && archetypesField.FieldType.IsArray)
                archetypesField.SetValue(progression, Array.CreateInstance(archetypesField.FieldType.GetElementType(), 0));
        }

        private static BlueprintFeature Tier(Factory f, BlueprintsCache cache, BlueprintCharacterClass wizard, Row row, int tier)
        {
            string token = row.Name + "Tier" + tier;
            string display = row.TierNames[tier];

            // Heavens and Lore have no native Wrath spirit: build all three tiers
            // privately from the original whisperer methods, never a frost clone.
            if (row.PrivateSpirit != null)
                return PrivateTier(f, cache, wizard, row, tier, token, display);

            // Life level 8 is Healer's Touch: Lore (Religion) +3 untyped stacking
            // (Life.cs createGreaterSpiritAbility), not a competence bonus.
            if (tier == 1 && row.L8Private == "healersTouch")
                return SkillFeature(f, token, display,
                    "Lore (Religion) checks gain a +3 untyped bonus that stacks with other bonuses.",
                    3, new[] { "SkillLoreReligion" }, "UntypedStackable");

            var donorFeature = f.Native<BlueprintFeature>(row.Tiers[tier]);
            if (donorFeature == null) throw new InvalidOperationException("tier donor missing: " + row.Name + " L" + new[] { 1, 8, 20 }[tier]);
            var feature = (BlueprintFeature)f.Clone(donorFeature, token, display,
                "Spirit Whisperer " + row.Name + " " + display + ". Wizard level and Intelligence apply; Charisma affects only the specifically named daily uses.");
            StripSpellLists(feature);
            Icon(feature, donorFeature);
            feature.ComponentsArray = feature.ComponentsArray
                .Where(c => c != null && c.GetType().Name != "AddAbilityResources").ToArray();

            BlueprintAbilityResource resource = null;
            string resourceToken = null;
            switch (row.Resource[tier])
            {
                case 1: resourceToken = row.Name + "R1"; resource = GetOrCreateResource(f, resourceToken, 3, StatType.Charisma, false); break;
                case 2: resourceToken = row.Name + "R2"; resource = GetOrCreateResource(f, resourceToken, 1, StatType.Charisma, false); break;
                case 3: resourceToken = row.Name + "R3"; resource = GetOrCreateResource(f, resourceToken, 3, null, false); break;
                case 4: resourceToken = row.Name + "R4"; resource = GetOrCreateResource(f, resourceToken, 1, null, false); break;
            }
            var dcStat = row.ChannelDc && tier == 0 ? StatType.Charisma : StatType.Intelligence;
            Rebind(feature, wizard, dcStat, _ => resource);
            CloneDirectAbilities(f, wizard, feature, token, dcStat, resource);
            if (resource != null)
                feature.ComponentsArray = feature.ComponentsArray.Concat(new BlueprintComponent[] { Grant(resource) }).ToArray();

            if (row.Resource[tier] == 3)
                AddCooldown(f, feature, token, wizard);
            return feature;
        }

        // Private 1/8/20 for the two spirits Wrath lacks. Each builder follows the
        // original createSpiritWhispererSpirit method using native Wrath components.
        private static BlueprintFeature PrivateTier(Factory f, BlueprintsCache cache, BlueprintCharacterClass wizard,
            Row row, int tier, string token, string display)
        {
            BlueprintAbilityResource resource = null;
            switch (row.Resource[tier])
            {
                case 1: resource = GetOrCreateResource(f, row.Name + "R1", 3, StatType.Charisma, false); break;
                case 4: resource = GetOrCreateResource(f, row.Name + "R4", 1, null, false); break;
            }

            BlueprintFeature feature;
            if (row.PrivateSpirit == "Heavens")
            {
                if (tier == 0) feature = HeavensStardust(f, wizard, token, display, resource);
                else if (tier == 1) feature = HeavensVoid(f, token, display);
                else feature = HeavensManifestation(f, wizard, token, display);
            }
            else
            {
                if (tier == 0) feature = MonstrousInsight(f, wizard, token, display, resource);
                else if (tier == 1) feature = SkillFeature(f, token, display,
                    "Four knowledge and lore skills gain a +10 competence bonus.", 10,
                    new[] { "SkillKnowledgeArcana", "SkillKnowledgeWorld", "SkillLoreNature", "SkillLoreReligion" }, "Competence");
                else feature = LoreManifestation(f, cache, wizard, token, display, resource);
            }

            if (resource != null)
                feature.ComponentsArray = feature.ComponentsArray.Concat(new BlueprintComponent[] { Grant(resource) }).ToArray();
            return feature;
        }

        // Heavens.cs createSpiritAbility: Stardust. A private faerie-fire buff
        // imposes a stacking penalty to attack rolls and sight-based Perception
        // that grows with Wizard level; the ability is a close-range supernatural
        // standard action spending one R1 use.
        private static BlueprintFeature HeavensStardust(Factory f, BlueprintCharacterClass wizard,
            string token, string display, BlueprintAbilityResource resource)
        {
            const string desc = "Stardust materializes around one creature within 30 feet. It sheds light as a candle, cannot benefit from concealment or invisibility, and takes a penalty on attack rolls and sight-based Perception checks that increases by 1 at 4th level and every 4 levels thereafter. The effect lasts 1 round per 2 wizard levels. Sightless creatures are unaffected.";
            var buff = (BlueprintBuff)f.Clone(f.Native<BlueprintBuff>(FaerieFireBuffId), token + "Buff", "Stardust", desc);
            buff.ComponentsArray = buff.ComponentsArray.Concat(new[]
            {
                Component("AddContextStatBonus", F("Stat", "SkillPerception"), F("Descriptor", "UntypedStackable"), F("Value", Rank()), F("Multiplier", -1)),
                Component("AddContextStatBonus", F("Stat", "AdditionalAttackBonus"), F("Descriptor", "UntypedStackable"), F("Value", Rank()), F("Multiplier", -1)),
                Component("ContextRankConfig", F("m_BaseValueType", "ClassLevel"), F("m_Class", new[] { Ref<BlueprintCharacterClassReference>(wizard) }), F("m_Progression", "OnePlusDivStep"), F("m_StepLevel", 4)),
            }).Where(c => c != null).ToArray();

            var ability = f.Ability(token + "Ability", display, desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Run(RankRounds(buff)),
                Component("ContextRankConfig", F("m_BaseValueType", "ClassLevel"), F("m_Class", new[] { Ref<BlueprintCharacterClassReference>(wizard) }), F("m_Progression", "Div2"), F("m_UseMin", true), F("m_Min", 1)),
                Cost(resource));
            return f.Feature(token, display, desc, PsychicAssets.Facts(ability));
        }

        // Heavens.cs createGreaterSpiritAbility. Wrath has no suffocation buff or
        // condition, so the immunity that can be expressed is poison plus blindsense.
        private static BlueprintFeature HeavensVoid(Factory f, string token, string display)
        {
            const string desc = "Blindsense 30 feet and immunity to poison.";
            var poison = new BuffDescriptorImmunity { Descriptor = SpellDescriptor.Poison };
            var sense = new Blindsense { Range = new Feet(30), Blindsight = false };
            return f.Feature(token, display, desc, poison, sense);
        }

        // Heavens.cs createManifestation: saves bonus equal to the Intelligence
        // modifier, fear immunity, and automatic confirmation of her own crits.
        private static BlueprintFeature HeavensManifestation(Factory f, BlueprintCharacterClass wizard, string token, string display)
        {
            const string desc = "The shaman becomes the spirit of heaven. She gains a bonus on all saving throws equal to her Intelligence modifier, is immune to fear effects, and automatically confirms critical hits she threatens.";
            return f.Feature(token, display, desc, new[]
            {
                Component("AddConditionImmunity", F("Condition", "Shaken")),
                Component("AddConditionImmunity", F("Condition", "Frightened")),
                Component("CritAutoconfirmAgainstAlignment", F("EnemyAlignment", "None")),
                Component("AddContextStatBonus", F("Stat", "SaveFortitude"), F("Descriptor", "UntypedStackable"), F("Value", Rank())),
                Component("AddContextStatBonus", F("Stat", "SaveReflex"), F("Descriptor", "UntypedStackable"), F("Value", Rank())),
                Component("AddContextStatBonus", F("Stat", "SaveWill"), F("Descriptor", "UntypedStackable"), F("Value", Rank())),
                Component("ContextRankConfig", F("m_BaseValueType", "StatBonus"), F("m_Stat", "Intelligence")),
            }.Where(c => c != null).ToArray());
        }

        private static BlueprintAbilityResource GetOrCreateResource(Factory f, string token, int baseValue, StatType? stat, bool perLevel)
        {
            if (f.Assets.TryGetValue(token, out var existing)) return (BlueprintAbilityResource)existing;
            return Resource(f, token, baseValue, stat, perLevel);
        }

        private static KeyValuePair<string, object> F(string key, object value) => new KeyValuePair<string, object>(key, value);

        private static ContextValue Rank(AbilityRankType type = AbilityRankType.Default)
            => new ContextValue { ValueType = ContextValueType.Rank, ValueRank = type };

        private static BlueprintFeature SkillFeature(Factory f, string token, string name, string description,
            int value, string[] stats, string descriptor)
        {
            var components = stats.Select(s => Component("AddStatBonus",
                F("Stat", s), F("Descriptor", descriptor), F("Value", value))).Where(c => c != null).ToArray();
            if (components.Length == 0) throw new InvalidOperationException("AddStatBonus component type unavailable.");
            return f.Feature(token, name, description, components);
        }

        private static BlueprintComponent Component(string typeName, params KeyValuePair<string, object>[] fields)
        {
            var type = FindType(typeName, typeof(BlueprintComponent));
            if (type == null) return null;
            var component = (BlueprintComponent)Activator.CreateInstance(type);
            foreach (var pair in fields)
            {
                var field = AccessTools.Field(type, pair.Key);
                var property = AccessTools.Property(type, pair.Key);
                var target = field?.FieldType ?? property?.PropertyType;
                if (target == null) continue;
                var value = Coerce(target, pair.Value);
                if (field != null) field.SetValue(component, value);
                else if (property != null && property.GetSetMethod(true) != null) property.SetValue(component, value, null);
            }
            return component;
        }

        // Wrath uses ContextValue for several numeric component fields. An int
        // must be wrapped in a real native ContextValue, not assigned directly.
        private static object Coerce(Type target, object value)
        {
            if (target.IsEnum && value is string s) return Enum.Parse(target, s);
            if (value is int number && target != typeof(int) && target.Name == "ContextValue")
                return new ContextValue { Value = number };
            return value;
        }

        // One level of private ability cloning: the feature's AddFacts entries
        // that are abilities become private graphs with re-bound class/stat/
        // resource fields. Native read-only buffs remain shared references.
        private static void CloneDirectAbilities(Factory f, BlueprintCharacterClass wizard, BlueprintFeature feature,
            string token, StatType dcStat, BlueprintAbilityResource resource)
        {
            int index = 0;
            foreach (var component in feature.ComponentsArray)
            {
                if (component == null || component.GetType().Name != "AddFacts" || !Has(component, "m_Facts")) continue;
                var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
                var rewritten = new List<BlueprintUnitFactReference>();
                foreach (var reference in refs)
                {
                    var donor = reference?.Get() as BlueprintScriptableObject;
                    if (donor is BlueprintAbility || donor is BlueprintActivatableAbility)
                    {
                        var clone = (BlueprintUnitFact)f.Clone((BlueprintScriptableObject)donor,
                            token + "Ability" + index, donor.name + " (Spirit Whisperer)",
                            "Private Spirit Whisperer adaptation of " + donor.name + ".");
                        StripSpellLists(clone);
                        if (Has(clone, "m_Parent")) Set(clone, "m_Parent", null);
                        Rebind(clone, wizard, dcStat, _ => resource);
                        rewritten.Add(Ref<BlueprintUnitFactReference>(clone));
                    }
                    else rewritten.Add(reference);
                    index++;
                }
                Set(component, "m_Facts", rewritten.ToArray());
            }
        }

        // R3: fixed three uses plus a private 1d4-round cooldown. The cooldown
        // fact is checked by a native caster restriction and applied by a real
        // effect action on every successfully executed leaf ability.
        private static void AddCooldown(Factory f, BlueprintFeature feature, string token, BlueprintCharacterClass wizard)
        {
            var buff = f.Buff(token + "Cooldown", "Spirit Whisperer cooldown",
                "This spirit power cannot be used again while this cooldown lasts.");
            var restriction = new AbilityCasterHasNoFacts();
            Set(restriction, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(buff) });
            var apply = new ContextActionApplyBuff { ToCaster = true, IsFromSpell = false,
                DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.D4,
                    DiceCountValue = 1, BonusValue = 0 } };
            Set(apply, "m_Buff", Ref<BlueprintBuffReference>(buff));
            foreach (var clone in f.Assets.Values.OfType<BlueprintAbility>().Where(a => a.name.Contains(token)))
            {
                var actions = clone.ComponentsArray.OfType<AbilityEffectRunAction>().ToArray();
                if (actions.Length == 0) continue;
                clone.ComponentsArray = clone.ComponentsArray.Concat(new BlueprintComponent[] { restriction }).ToArray();
                foreach (var run in actions)
                {
                    var list = Get(run, "Actions");
                    var old = (GameAction[])Get(list, "Actions") ?? Array.Empty<GameAction>();
                    Set(list, "Actions", old.Concat(new GameAction[] { apply }).ToArray());
                }
            }
        }

        // D-F02 / D-F03: a hex clones a real Wrath donor, strips the oracle class
        // and revelation prerequisites, and rebinds every consumption leaf onto a
        // package-private resource granted on this feature. No donor resource GUID
        // survives. Life Link / Life Sight use R5 (one point per Wizard level).
        private static BlueprintFeature BuildHexFeature(Factory f, BlueprintsCache cache, BlueprintCharacterClass wizard,
            Row row, HexSpec hex, int index)
        {
            string token = row.Name + "Hex" + index;
            if (string.IsNullOrEmpty(hex.Donor))
                return PrivateHex(f, wizard, row, hex, token);

            var donor = f.Native<BlueprintFeature>(hex.Donor);
            if (donor == null) throw new InvalidOperationException("hex donor missing: " + hex.Donor);
            var feature = (BlueprintFeature)f.Clone(donor, token, hex.Name + " (Spirit Whisperer)",
                "Spirit Whisperer hex adaptation. Wizard level and Intelligence replace shaman level and Wisdom. Wrath effect donor: " + donor.name + ".");
            StripSpellLists(feature);
            Icon(feature, donor);
            // Drop the donor's resource grant and every oracle/revelation
            // prerequisite; keep PrerequisiteNoFeature so a hex cannot be taken twice.
            feature.ComponentsArray = feature.ComponentsArray
                .Where(c => c != null && c.GetType().Name != "AddAbilityResources" &&
                            (!(c is Prerequisite) || c is PrerequisiteNoFeature)).ToArray();

            BlueprintAbilityResource resource = null;
            if (hex.Resource == 5) resource = GetOrCreateResource(f, token + "R5", 0, null, true);
            else if (hex.Resource == 2) resource = GetOrCreateResource(f, token + "R2", 1, StatType.Charisma, false);
            else if (HasConsumption(feature)) resource = GetOrCreateResource(f, token + "R1", 3, StatType.Charisma, false);

            Rebind(feature, wizard, StatType.Intelligence, _ => resource);
            CloneDirectAbilities(f, wizard, feature, token, StatType.Intelligence, resource);
            if (resource != null)
                feature.ComponentsArray = feature.ComponentsArray.Concat(new BlueprintComponent[] { Grant(resource) }).ToArray();
            if (hex.Name == "Life Link" && resource != null)
                AddLifeLinkRefund(f, feature, resource);
            if (hex.Name == "Mental Acuity") FixMentalAcuity(feature);
            if (hex.Name == "Entangling Curse") FixEntanglingMinimum(feature);
            if (hex.Name == "Friend to Animals") RetargetSummon(f, feature, wizard, token);
            return feature;
        }

        private static bool HasConsumption(BlueprintScriptableObject bp)
        {
            bool found = false;
            Walk(bp.ComponentsArray, new HashSet<object>(PsychicAssets.IdentityComparer), o =>
            {
                var n = o.GetType().Name;
                if (n.Contains("ResourceLogic") && Has(o, "m_RequiredResource")) found = true;
            });
            return found;
        }

        // Donor-less hexes are built privately from native Wrath components per
        // the original method. The trailing throw guards a future row naming a
        // hex with no builder.
        private static BlueprintFeature PrivateHex(Factory f, BlueprintCharacterClass wizard, Row row, HexSpec hex, string token)
        {
            switch (hex.Name)
            {
                case "Life Sight": return LifeSight(f, wizard, token);
                case "Curse of Suffering": return CurseOfSuffering(f, wizard, token);
                case "Deathly Being": return DeathlyBeing(f, wizard, token);
                case "Stormwalker": return Stormwalker(f, wizard, token);
                case "Crashing Waves": return CrashingWaves(f, wizard, token);
                case "Sparkling Aura": return SparklingAura(f, wizard, token);
                case "Loadstone": return Loadstone(f, token);
                case "Ward of Stone": return WardOfStone(f, wizard, token);
                case "Enveloping Void": return EnvelopingVoid(f, wizard, token);
                case "Lure of Heavens": return LureOfHeavens(f, wizard, token);
                case "Heaven's Leap": return HeavensLeap(f, token);
                case "Starburn": return Starburn(f, wizard, token);
                case "Benefit of Wisdom": return BenefitOfWisdom(f, token);
                case "Brain Drain": return BrainDrain(f, wizard, token);
                case "Confusion Curse": return ConfusionCurse(f, wizard, token);
                case "Erosion Curse": return ErosionCurse(f, wizard, token);
                case "Mist's Shroud": return MistsShroud(f, token);
            }
            throw new NotSupportedException("0117 private hex '" + hex.Name +
                "' has no same-scope Wrath effect; ContractGaps rejects the package before this builder runs.");
        }

        private static BlueprintFeature CurseOfSuffering(Factory f, BlueprintCharacterClass wizard, string token)
        {
            const string desc = "The target takes 1 extra point of damage whenever it takes bleed damage, and healing restores only half. The curse lasts one round per wizard level.";
            var heal = Component("IncomingDamageAndHealingModifier", F("Type", "OnlyHeal"), F("ModifierPercents", new ContextValue { Value = 50 }));
            if (heal == null) throw new InvalidOperationException("IncomingDamageAndHealingModifier missing.");
            var buff = f.Buff(token + "Buff", "Curse of Suffering", desc, heal, new BleedIncrease0117());
            var ability = f.Ability(token + "Ability", "Curse of Suffering", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Run(RankRounds(buff)), WizardParams(wizard), Rank(wizard, "AsIs"));
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Curse of Suffering", desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature DeathlyBeing(Factory f, BlueprintCharacterClass wizard, string token)
        {
            const string desc = "Positive energy that would heal the spirit whisperer harms her, and negative energy heals her. She has a +4 bonus on saves against death effects and immunity to negative energy.";
            var immune = new AddEnergyImmunity { Type = DamageEnergyType.NegativeEnergy };
            var death = new SavingThrowBonusAgainstDescriptor { SpellDescriptor = SpellDescriptor.Death, ModifierDescriptor = ModifierDescriptor.UntypedStackable, Value = 4 };
            return f.Feature(token, "Deathly Being", desc, immune, death, new DeathlyHealing0117());
        }

        private static BlueprintFeature Stormwalker(Factory f, BlueprintCharacterClass wizard, string token)
        {
            const string desc = "While the weather is bad, the spirit whisperer's speed increases by 10 feet. At 10th wizard level she ignores fog concealment.";
            var gate = new AddFeatureOnClassLevel { Level = 10 };
            Set(gate, "m_Class", Ref<BlueprintCharacterClassReference>(wizard));
            var fog = f.Feature(token + "Fog", "Stormwalker Fog", desc, new FogConcealment0117());
            Set(gate, "m_Feature", Ref<BlueprintFeatureReference>(fog));
            return f.Feature(token, "Stormwalker", desc, new WeatherSpeed0117(), gate);
        }

        private static BlueprintFeature CrashingWaves(Factory f, BlueprintCharacterClass wizard, string token)
        {
            const string desc = "Water spells are cast at a higher caster level and can knock a damaged target prone unless it succeeds at a Fortitude save. From 8th wizard level cold spells are also cast higher. From 16th wizard level any damaging spell can knock the target prone on a failed Fortitude save.";
            return f.Feature(token, "Crashing Waves", desc, new CrashingWaves0117());
        }

        private static BlueprintFeature SparklingAura(Factory f, BlueprintCharacterClass wizard, string token)
        {
            const string desc = "A creature within close range glows for 1 round per 2 wizard levels. A metal weapon that hits it deals extra electricity damage to that creature equal to the spirit whisperer's Charisma modifier.";
            var buff = f.Buff(token + "Buff", "Sparkling Aura", desc, new SparklingAura0117());
            var ability = f.Ability(token + "Ability", "Sparkling Aura", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close, PsychicAssets.Run(RankRounds(buff)), Rank(wizard, "Div2"));
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Sparkling Aura", desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature Loadstone(Factory f, string token)
        {
            var slow = f.Native<BlueprintAbility>("f492622e473d34747806bdb39356eb89");
            if (slow == null) throw new InvalidOperationException("Slow spell missing for Loadstone.");
            var ability = (BlueprintAbility)f.Clone(slow, token + "Ability", "Loadstone", "A single creature is slowed, as the Slow spell.");
            ability.Type = AbilityType.Supernatural;
            ability.Range = AbilityRange.Close;
            ability.SpellResistance = false;
            ability.ActionType = UnitCommand.CommandType.Standard;
            StripSpellLists(ability);
            ability.ComponentsArray = ability.ComponentsArray.Where(c => c != null && c.GetType().Name != "SpellComponent" && c.GetType().Name != "AbilityTargetsAround").ToArray();
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Loadstone", "A single creature is slowed, as the Slow spell.", PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature WardOfStone(Factory f, BlueprintCharacterClass wizard, string token)
        {
            const string desc = "A touched ally gains DR 5/adamantine for 1 minute. The ward is discharged after one hit, after two hits at 8th wizard level, or after three hits at 16th wizard level.";
            var dr = new AddDamageResistancePhysical { Value = 5, BypassedByMaterial = true, Material = PhysicalDamageMaterial.Adamantite };
            var buff = f.Buff(token + "Buff", "Ward of Stone", desc, dr, new WardCharges0117());
            var ability = f.Ability(token + "Ability", "Ward of Stone", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Touch,
                PsychicAssets.Run(PsychicAssets.Apply(buff, 1, DurationRate.Minutes)));
            ability.CanTargetEnemies = false;
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Ward of Stone", desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature MonstrousInsight(Factory f, BlueprintCharacterClass wizard, string token, string display, BlueprintAbilityResource resource)
        {
            const string desc = "As a standard action, attempt the Knowledge or Lore check appropriate to the target's type, with an insight bonus equal to your wizard level. For 1 minute you also gain a +2 insight bonus on attacks against that creature and to AC against its attacks.";
            var attack = new Kingmaker.Designers.Mechanics.Buffs.AttackBonusAgainstTarget { Value = new ContextValue { Value = 2 }, Descriptor = ModifierDescriptor.Insight, CheckCaster = true };
            var ac = new Kingmaker.Designers.Mechanics.Buffs.ACBonusAgainstTarget { Value = new ContextValue { Value = 2 }, Descriptor = ModifierDescriptor.Insight, CheckCaster = true };
            var buff = f.Buff(token + "Buff", display, desc, attack, ac);
            var ability = f.Ability(token + "Ability", display, desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Touch,
                PsychicAssets.Run(new MonsterLore0117(), PsychicAssets.Apply(buff, 1, DurationRate.Minutes)),
                Cost(resource), WizardParams(wizard));
            return f.Feature(token, display, desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature LoreManifestation(Factory f, BlueprintsCache cache, BlueprintCharacterClass wizard, string token, string display, BlueprintAbilityResource resource)
        {
            const string desc = "Take 20 on Knowledge and Lore checks. Once per day, cast a wizard spell of 8th level or lower that has no material component. The spell uses wizard level and Intelligence, and cannot grant an ability score bonus.";
            var skills = new[] { StatType.SkillKnowledgeArcana, StatType.SkillKnowledgeWorld, StatType.SkillLoreNature, StatType.SkillLoreReligion };
            var take = new ModifyD20 { Rule = RuleType.SkillCheck, SpecificSkill = true, Skill = skills, Replace = true,
                RollResult = new ContextValue { Value = 20 },
                ActionsToTrigger = PsychicAssets.Actions(), ActionsToTriggerBeforeRoll = PsychicAssets.Actions() };
            var menu = WishMenu(f, cache, wizard, token, resource);
            return f.Feature(token, display, desc, take, PsychicAssets.Facts(menu), Grant(resource));
        }

        // Contract section 3 forbids Wish granting ability-score bonuses. Match the twelve
        // native enhancement spells by AssetGuid (stable); blueprint .name is not reliably
        // the asset filename in every load path. GUIDs verified against blueprints.zip.
        private static readonly HashSet<string> WishBlockedSpells = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "4c3d08935262b6544ae97599b3a9556d", // BullsStrength
            "a900628aea19aa74aad0ece0e65d091a", // BearsEndurance
            "de7a025d48ad5da4991e7d3c682cf69d", // CatsGrace
            "446f7bf201dc1934f96ac0a26e324803", // EaglesSplendor
            "ae4d3ad6a8fda1542acf2e9bbc13d113", // FoxsCunning
            "f0455c9295b53904f9e02fc571dd2ce1", // OwlsWisdom
            "6a234c6dcde7ae94e94e9c36fd1163a7", // BullsStrengthMass
            "f6bcea6db14f0814d99b54856e918b92", // BearsEnduranceMass
            "1f6c94d56f178b84ead4c02f1b1e1c48", // CatsGraceMass
            "2caa607eadda4ab44934c5c9875e01bc", // EaglesSplendorMass
            "2b24159ad9907a8499c2313ba9c0f615", // FoxsCunningMass
            "9f5ada581af3db4419b54db77f44e430", // OwlsWisdomMass
        };

        private static BlueprintAbility WishMenu(Factory f, BlueprintsCache cache, BlueprintCharacterClass wizard, string token, BlueprintAbilityResource resource)
        {
            var list = cache.Load(BlueprintGuid.Parse("ba0401fdeb4062f40a7aa95b6f07fe89")) as BlueprintSpellList;
            if (list == null) throw new InvalidOperationException("Wizard spell list missing for Lore wish.");
            var parent = f.Ability(token + "Wish", "Manifestation: Wish", "Once per day, cast one wizard spell of 8th level or lower with no material component.",
                AbilityType.SpellLike, UnitCommand.CommandType.Standard, AbilityRange.Personal, WizardParams(wizard));
            var children = new List<BlueprintAbilityReference>();
            var levels = list.SpellsByLevel;
            int max = Math.Min(8, levels.Length - 1);
            for (int level = 1; level <= max; level++)
            {
                foreach (var spell in levels[level].Spells)
                {
                    if (spell == null) continue;
                    var material = spell.MaterialComponent;
                    if (material != null && material.Item != null) continue;
                    if (WishBlockedSpells.Contains(spell.AssetGuid.ToString().Replace("-", ""))) continue;
                    var copy = (BlueprintAbility)f.Clone(spell, token + "Wish" + spell.AssetGuid.ToString().Replace("-", ""), spell.name, "Wish copy of " + spell.name);
                    copy.Type = AbilityType.SpellLike;
                    copy.ActionType = UnitCommand.CommandType.Standard;
                    StripSpellLists(copy);
                    copy.ComponentsArray = copy.ComponentsArray.Where(c => c != null && c.GetType().Name != "SpellListComponent" && c.GetType().Name != "AbilityResourceLogic")
                        .Concat(new BlueprintComponent[] { Cost(resource), WizardParams(wizard) }).ToArray();
                    Set(copy, "m_Parent", Ref<BlueprintAbilityReference>(parent));
                    children.Add(Ref<BlueprintAbilityReference>(copy));
                }
            }
            if (children.Count == 0) throw new InvalidOperationException("Lore wish found no wizard spells.");
            var variants = new AbilityVariants();
            Set(variants, "m_Variants", children.ToArray());
            parent.ComponentsArray = parent.ComponentsArray.Concat(new BlueprintComponent[] { variants }).ToArray();
            return parent;
        }

        private static BlueprintFeature EnvelopingVoid(Factory f, BlueprintCharacterClass wizard, string token)
        {
            const string desc = "One enemy within close range is blinded for one round per wizard level. A Will save negates.";
            var donor = f.Native<BlueprintBuff>("187f88d96a0ef464280706b63635f2af");
            if (donor == null) throw new InvalidOperationException("blindness buff missing.");
            var buff = (BlueprintBuff)f.Clone(donor, token + "Buff", "Enveloping Void", desc);
            var ability = f.Ability(token + "Ability", "Enveloping Void", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Save(SavingThrowType.Will, PsychicAssets.Saved(Array.Empty<GameAction>(), new GameAction[] { RankRounds(buff) })),
                WizardParams(wizard), Rank(wizard, "AsIs"));
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Enveloping Void", desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature LureOfHeavens(Factory f, BlueprintCharacterClass wizard, string token)
        {
            const string desc = "At 10th wizard level the spirit whisperer can fly.";
            var flight = Component("AddMechanicsFeature", F("m_Feature", "Flying"));
            if (flight == null) throw new InvalidOperationException("Flying mechanics feature missing.");
            var gate = Component("PrerequisiteClassLevel", F("m_CharacterClass", Ref<BlueprintCharacterClassReference>(wizard)), F("Level", 10));
            var parts = new List<BlueprintComponent> { flight };
            if (gate != null) parts.Add(gate);
            return f.Feature(token, "Lure of Heavens", desc, parts.ToArray());
        }

        private static BlueprintFeature HeavensLeap(Factory f, string token)
        {
            const string desc = "Choose one ally, or yourself, within close range. Then choose a point within close range; that creature moves there. The same creature cannot be chosen again for 24 hours.";
            var mark = f.Buff(token + "Mark", "Heaven's Leap Target", desc);
            var day = f.Buff(token + "Day", "Heaven's Leap (already targeted)", "This creature cannot be the target of Heaven's Leap again for 24 hours.");
            var choose = f.Ability(token + "Select", "Heaven's Leap: Select", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Free, AbilityRange.Close, PsychicAssets.Run(PsychicAssets.Apply(mark, 1)));
            choose.CanTargetEnemies = false;
            choose.CanTargetFriends = true;
            choose.CanTargetSelf = true;
            var gate = new AbilityTargetHasFact { Inverted = true };
            Set(gate, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(day) });
            choose.ComponentsArray = choose.ComponentsArray.Concat(new BlueprintComponent[] { gate }).ToArray();
            var leap = new HeavensLeap0117 { Mark = mark, DayLock = day };
            var move = f.Ability(token + "Ability", "Heaven's Leap", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close, leap);
            move.CanTargetPoint = true;
            move.CanTargetFriends = false;
            move.CanTargetEnemies = false;
            move.CanTargetSelf = false;
            return f.Feature(token, "Heaven's Leap", desc, PsychicAssets.Facts(choose, move), PsychicAssets.Facts(mark));
        }

        private static BlueprintFeature Starburn(Factory f, BlueprintCharacterClass wizard, string token)
        {
            const string desc = "One creature within close range takes 1d6 fire damage per 2 wizard levels and shines for 1 round. A Fortitude save halves the damage. Uses are 1 + Charisma, and each use imposes a 1d4-round cooldown.";
            var resource = GetOrCreateResource(f, token + "R2", 1, StatType.Charisma, false);
            var light = (BlueprintBuff)f.Clone(f.Native<BlueprintBuff>(FaerieFireBuffId), token + "Light", "Starburn", desc);
            var damage = new ContextActionDealDamage {
                DamageType = new DamageTypeDescription { Type = DamageType.Energy, Energy = DamageEnergyType.Fire },
                Value = new ContextDiceValue { DiceType = DiceType.D6, DiceCountValue = Rank(), BonusValue = 0 },
                HalfIfSaved = true, IgnoreCritical = true, DisableSneakDamage = true
            };
            var cooldown = f.Buff(token + "Cooldown", "Starburn cooldown", "This hex cannot be used again while this cooldown lasts.");
            var restriction = new AbilityCasterHasNoFacts();
            Set(restriction, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(cooldown) });
            var cooldownApply = new ContextActionApplyBuff { ToCaster = true, IsFromSpell = false,
                DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.D4, DiceCountValue = 1, BonusValue = 0 } };
            Set(cooldownApply, "m_Buff", Ref<BlueprintBuffReference>(cooldown));
            var ability = f.Ability(token + "Ability", "Starburn", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Save(SavingThrowType.Fortitude, damage, PsychicAssets.Saved(Array.Empty<GameAction>(), new GameAction[] { PsychicAssets.Apply(light, 1) })),
                PsychicAssets.Run(cooldownApply), restriction,
                Cost(resource), WizardParams(wizard), Rank(wizard, "Div2"));
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Starburn", desc, PsychicAssets.Facts(ability), Grant(resource));
        }

        private static BlueprintFeature BenefitOfWisdom(Factory f, string token)
        {
            const string desc = "Lore (Nature) and Lore (Religion) checks use Intelligence instead of Wisdom.";
            var nature = Component("ReplaceStatBaseAttribute", F("TargetStat", "SkillLoreNature"), F("BaseAttributeReplacement", "Intelligence"));
            var religion = Component("ReplaceStatBaseAttribute", F("TargetStat", "SkillLoreReligion"), F("BaseAttributeReplacement", "Intelligence"));
            if (nature == null || religion == null) throw new InvalidOperationException("ReplaceStatBaseAttribute missing.");
            return f.Feature(token, "Benefit of Wisdom", desc, nature, religion);
        }

        private static BlueprintFeature BrainDrain(Factory f, BlueprintCharacterClass wizard, string token)
        {
            const string desc = "One intelligent enemy within 30 feet takes 1d4 damage per 2 wizard levels. A Will save negates. This is a mind-affecting effect.";
            var damage = new ContextActionDealDamage {
                DamageType = new DamageTypeDescription { Type = DamageType.Direct },
                Value = new ContextDiceValue { DiceType = DiceType.D4, DiceCountValue = Rank(), BonusValue = 0 },
                IgnoreCritical = true, DisableSneakDamage = true
            };
            var ability = f.Ability(token + "Ability", "Brain Drain", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Save(SavingThrowType.Will, PsychicAssets.Saved(Array.Empty<GameAction>(), new GameAction[] { damage })),
                WizardParams(wizard), Rank(wizard, "Div2"));
            AddDayLock(f, ability, token, true);
            return f.Feature(token, "Brain Drain", desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature ConfusionCurse(Factory f, BlueprintCharacterClass wizard, string token)
        {
            const string desc = "One intelligent enemy within 30 feet is confused for a number of rounds equal to the Charisma modifier, minimum 1. A Will save negates.";
            var donor = f.Native<BlueprintBuff>("886c7407dc629dc499b9f1465ff382df");
            if (donor == null) throw new InvalidOperationException("confused buff missing.");
            var buff = (BlueprintBuff)f.Clone(donor, token + "Buff", "Confusion Curse", desc);
            var ability = f.Ability(token + "Ability", "Confusion Curse", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Save(SavingThrowType.Will, PsychicAssets.Saved(Array.Empty<GameAction>(), new GameAction[] { RankRounds(buff) })),
                WizardParams(wizard), CharismaRounds());
            AddDayLock(f, ability, token, true);
            return f.Feature(token, "Confusion Curse", desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature ErosionCurse(Factory f, BlueprintCharacterClass wizard, string token)
        {
            const string desc = "A construct within close range takes 1d6 damage per 2 wizard levels. A Reflex save halves the damage.";
            var construct = f.Native<BlueprintFeature>("fd389783027d63343b4a5634bd81645f");
            var damage = new ContextActionDealDamage {
                DamageType = new DamageTypeDescription { Type = DamageType.Direct },
                Value = new ContextDiceValue { DiceType = DiceType.D6, DiceCountValue = Rank(), BonusValue = 0 },
                HalfIfSaved = true, IgnoreCritical = true, DisableSneakDamage = true
            };
            var ability = f.Ability(token + "Ability", "Erosion Curse", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Save(SavingThrowType.Reflex, damage),
                WizardParams(wizard), Rank(wizard, "Div2"));
            if (construct != null)
            {
                var gate = new AbilityTargetHasFact { Inverted = false };
                Set(gate, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(construct) });
                ability.ComponentsArray = ability.ComponentsArray.Concat(new BlueprintComponent[] { gate }).ToArray();
            }
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Erosion Curse", desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature MistsShroud(Factory f, string token)
        {
            const string desc = "A willing creature gains blur concealment until an attack misses it because of that concealment, or for 1 minute. At 8th wizard level the mist lasts through one extra such miss, and at 16th through two.";
            var donor = f.Native<BlueprintBuff>("dd3ad347240624d46a11a092b4dd4674");
            if (donor == null) throw new InvalidOperationException("blur buff missing.");
            var buff = (BlueprintBuff)f.Clone(donor, token + "Buff", "Mist's Shroud", desc);
            buff.ComponentsArray = buff.ComponentsArray.Concat(new BlueprintComponent[] { new MistCharges0117() }).ToArray();
            var ability = f.Ability(token + "Ability", "Mist's Shroud", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Touch, PsychicAssets.Run(PsychicAssets.Apply(buff, 1, DurationRate.Minutes)));
            ability.CanTargetEnemies = false;
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Mist's Shroud", desc, PsychicAssets.Facts(ability));
        }

        private static ContextCalculateAbilityParamsBasedOnClass WizardParams(BlueprintCharacterClass wizard)
        {
            var p = new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Intelligence };
            Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(wizard));
            return p;
        }

        private static ContextRankConfig Rank(BlueprintCharacterClass wizard, string progression)
        {
            var rank = new ContextRankConfig();
            Set(rank, "m_BaseValueType", ContextRankBaseValueType.ClassLevel);
            Set(rank, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(wizard) });
            SetEnum(rank, "m_Progression", progression);
            if (progression == "Div2") { Set(rank, "m_UseMin", true); Set(rank, "m_Min", 1); }
            return rank;
        }

        private static void AddDayLock(Factory f, BlueprintAbility ability, string token, bool onlyOnSuccess)
        {
            var mark = f.Buff(token + "Day", "Already targeted",
                "This creature cannot be the target of this hex again for 24 hours.");
            var gate = new AbilityTargetHasFact { Inverted = true };
            Set(gate, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(mark) });
            var apply = PsychicAssets.Apply(mark, 1, DurationRate.Days, false, false);
            apply.IsNotDispelable = true;
            if (onlyOnSuccess)
            {
                var saved = ability.ComponentsArray.OfType<AbilityEffectRunAction>()
                    .SelectMany(run => run.Actions.Actions ?? Array.Empty<GameAction>())
                    .OfType<ContextActionConditionalSaved>().FirstOrDefault();
                if (saved != null && saved.Failed != null)
                    saved.Failed.Actions = (saved.Failed.Actions ?? Array.Empty<GameAction>()).Concat(new GameAction[] { apply }).ToArray();
            }
            else ability.ComponentsArray = ability.ComponentsArray.Concat(new BlueprintComponent[] { PsychicAssets.Run(apply) }).ToArray();
            ability.ComponentsArray = ability.ComponentsArray.Concat(new BlueprintComponent[] { gate }).ToArray();
        }

        private static void FixMentalAcuity(BlueprintFeature feature)
        {
            foreach (var rank in feature.GetComponents<ContextRankConfig>())
            {
                SetEnum(rank, "m_Progression", "OnePlusDivStep");
                Set(rank, "m_StepLevel", 5);
                Set(rank, "m_StartLevel", 0);
                var field = AccessTools.Field(rank.GetType(), "m_CustomProgression");
                if (field != null && field.FieldType.IsArray)
                    field.SetValue(rank, Array.CreateInstance(field.FieldType.GetElementType(), 0));
            }
        }

        private static void FixEntanglingMinimum(BlueprintFeature feature)
        {
            foreach (var ability in GrantedAbilities(feature))
                foreach (var rank in ability.GetComponents<ContextRankConfig>())
                {
                    if (Get(rank, "m_Stat").ToString() != "Charisma") continue;
                    Set(rank, "m_UseMin", true);
                    Set(rank, "m_Min", 1);
                }
        }

        private static void RetargetSummon(Factory f, BlueprintFeature feature, BlueprintCharacterClass wizard, string token)
        {
            foreach (var component in feature.ComponentsArray)
            {
                if (component == null || component.GetType().Name != "AddFacts" || !Has(component, "m_Facts")) continue;
                var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
                var rewritten = new List<BlueprintUnitFactReference>();
                int index = 0;
                foreach (var reference in refs)
                {
                    var donor = reference?.Get() as BlueprintFeature;
                    bool summon = donor != null && donor.ComponentsArray.Any(c => c != null && c.GetType().Name == "SpontaneousSpellConversion");
                    if (!summon) { rewritten.Add(reference); index++; continue; }
                    var clone = (BlueprintFeature)f.Clone(donor, token + "Summon" + index, donor.name + " (Spirit Whisperer)",
                        "Spirit Whisperer spontaneous summon conversion uses Wizard spell slots.");
                    foreach (var conversion in clone.ComponentsArray.Where(c => c != null && c.GetType().Name == "SpontaneousSpellConversion"))
                        Set(conversion, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(wizard));
                    rewritten.Add(Ref<BlueprintUnitFactReference>(clone));
                    index++;
                }
                Set(component, "m_Facts", rewritten.ToArray());
            }
        }

        private static IEnumerable<BlueprintAbility> GrantedAbilities(BlueprintFeature feature)
        {
            foreach (var component in feature.ComponentsArray)
            {
                if (component == null || component.GetType().Name != "AddFacts" || !Has(component, "m_Facts")) continue;
                var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
                foreach (var reference in refs)
                    if (reference?.Get() is BlueprintAbility ability) yield return ability;
            }
        }

        // D-F11: Apply(buff, 1) stored a constant round. The duration reads the
        // ability's default rank instead, so the existing class-level rank (or the
        // Charisma rank below) is the number of rounds.
        private static ContextActionApplyBuff RankRounds(BlueprintBuff buff)
        {
            var apply = PsychicAssets.Apply(buff, 0);
            apply.DurationValue.BonusValue = Rank();
            return apply;
        }

        private static ContextRankConfig CharismaRounds()
        {
            var rank = new ContextRankConfig();
            Set(rank, "m_BaseValueType", ContextRankBaseValueType.StatBonus);
            Set(rank, "m_Stat", StatType.Charisma);
            Set(rank, "m_UseMin", true);
            Set(rank, "m_Min", 1);
            SetEnum(rank, "m_Progression", "AsIs");
            return rank;
        }

        private static void SetEnum(object obj, string field, string name)
        {
            var target = AccessTools.Field(obj.GetType(), field) ?? throw new MissingFieldException(obj.GetType().FullName, field);
            target.SetValue(obj, Enum.Parse(target.FieldType, name));
        }

        // Life Sight: R5 pool, a free-action toggle that spends one point per new
        // round (ActivatableAbilityResourceLogic.NewRound), gated at Wizard 12.
        private static BlueprintFeature LifeSight(Factory f, BlueprintCharacterClass wizard, string token)
        {
            const string desc = "The spirit whisperer sees the life force of nearby creatures, gaining blindsight 30 feet and immunity to gaze attacks. This toggle spends one use per round while active.";
            var resource = GetOrCreateResource(f, token + "R5", 0, null, true);
            var gazeSpell = new Kingmaker.UnitLogic.FactLogic.SpellImmunityToSpellDescriptor { Descriptor = SpellDescriptor.GazeAttack };
            var gazeBuff = new BuffDescriptorImmunity { Descriptor = SpellDescriptor.GazeAttack };
            Set(gazeSpell, "m_CasterIgnoreImmunityFact", new BlueprintUnitFactReference());
            Set(gazeBuff, "m_IgnoreFeature", new BlueprintUnitFactReference());
            var buff = f.Buff(token + "Buff", "Life Sight", desc,
                new Blindsense { Range = new Feet(30), Blindsight = true }, gazeSpell, gazeBuff);
            var spend = new ActivatableAbilityResourceLogic { SpendType = ActivatableAbilityResourceLogic.ResourceSpendType.NewRound };
            Set(spend, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
            var toggle = f.Add(new BlueprintActivatableAbility
            {
                ActivationType = AbilityActivationType.WithUnitCommand,
                Group = ActivatableAbilityGroup.None,
                DeactivateIfCombatEnded = true,
                ComponentsArray = new BlueprintComponent[] { spend }
            }, token + "Toggle", "Life Sight", desc);
            Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(buff));
            Icon(toggle, buff);
            var gate = Component("PrerequisiteClassLevel", F("m_CharacterClass", Ref<BlueprintCharacterClassReference>(wizard)), F("Level", 12));
            var components = new List<BlueprintComponent> { PsychicAssets.Facts(toggle), Grant(resource) };
            if (gate != null) components.Add(gate);
            return f.Feature(token, "Life Sight", desc, components.ToArray());
        }

        // D-F03: ending a Life Link refunds exactly one R5 point to the caster.
        // The donor link buff is cloned privately and given a Deactivated action
        // that restores one use; the original donor buff and resource are untouched.
        private static void AddLifeLinkRefund(Factory f, BlueprintFeature feature, BlueprintAbilityResource resource)
        {
            BlueprintBuff refund = null;
            var applies = new List<ContextActionApplyBuff>();
            Walk(feature.ComponentsArray, new HashSet<object>(PsychicAssets.IdentityComparer), o =>
            {
                if (o is ContextActionApplyBuff apply) applies.Add(apply);
            });
            foreach (var apply in applies)
            {
                var buffRef = Get(apply, "m_Buff") as BlueprintReferenceBase;
                if (buffRef == null || buffRef.Guid.ToString() != LifeLinkBuffId) continue;
                if (refund == null)
                {
                    var donor = f.Native<BlueprintBuff>(LifeLinkBuffId);
                    if (donor == null) continue;
                    refund = (BlueprintBuff)f.Clone(donor, "LifeLinkRefundBuff", "Life Link",
                        "When this link ends, one use of the spirit whisperer's Life Link pool is restored.");
                    var restore = new ContextRestoreResource();
                    Set(restore, "Resource", Ref<BlueprintAbilityResourceReference>(resource));
                    Set(restore, "Value", new ContextValue { Value = 1 });
                    var onCaster = WrapOnContextCaster(restore);
                    var ctx = new AddFactContextActions { Activated = PsychicAssets.Actions(),
                        Deactivated = PsychicAssets.Actions(onCaster), NewRound = PsychicAssets.Actions() };
                    refund.ComponentsArray = refund.ComponentsArray.Concat(new BlueprintComponent[] { ctx }).ToArray();
                }
                Set(apply, "m_Buff", Ref<BlueprintBuffReference>(refund));
            }
        }

        private static GameAction WrapOnContextCaster(GameAction action)
        {
            var type = FindType("ContextActionOnContextCaster", typeof(GameAction));
            if (type == null) return action;
            var wrapper = (GameAction)Activator.CreateInstance(type);
            var field = AccessTools.Field(type, "Action") ?? AccessTools.Field(type, "m_Action");
            if (field != null) field.SetValue(wrapper, action);
            return wrapper;
        }
    }

    [TypeId("0117c0e1a1b24c5d8e9f001122334402")]
    internal sealed class HeavensLeap0117 : AbilityCustomDimensionDoor
    {
        internal BlueprintBuff Mark;
        internal BlueprintBuff DayLock;
        public override System.Collections.Generic.IEnumerator<Kingmaker.UnitLogic.Abilities.Components.Base.AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target)
        {
            var caster = context.MaybeCaster;
            if (caster != null && Mark != null)
            {
                var ally = Marked(caster) ?? caster;
                bool locked = DayLock != null && ally.Buffs.Enumerable.Any(b => b.Blueprint == DayLock);
                if (!locked)
                {
                    ally.Position = target.Point;
                    foreach (var buff in ally.Buffs.Enumerable.Where(b => b.Blueprint == Mark).ToArray()) buff.Remove();
                    if (DayLock != null)
                    {
                        var added = ally.Buffs.AddBuff(DayLock, context, TimeSpan.FromDays(1));
                        if (added != null) added.IsNotDispelable = true;
                    }
                }
            }
            yield break;
        }
        UnitEntityData Marked(UnitEntityData caster)
        {
            foreach (var unit in Candidates(caster))
            {
                if (unit?.Buffs == null) continue;
                foreach (var buff in unit.Buffs.Enumerable)
                {
                    if (buff.Blueprint != Mark) continue;
                    var from = buff.Context?.MaybeCaster;
                    if (from == null || from == caster) return unit;
                }
            }
            return null;
        }
        static IEnumerable<UnitEntityData> Candidates(UnitEntityData caster)
        {
            yield return caster;
            var player = Game.Instance?.Player;
            if (player == null) yield break;
            object party = null;
            foreach (var name in new[] { "Party", "PartyCharacters", "Characters", "Group" })
            {
                party = AccessTools.Property(player.GetType(), name)?.GetValue(player, null);
                if (party != null) break;
            }
            if (party is System.Collections.IEnumerable items)
                foreach (var item in items)
                {
                    if (item is UnitEntityData unit) yield return unit;
                    else if (item != null)
                    {
                        var value = AccessTools.Property(item.GetType(), "Value")?.GetValue(item, null) as UnitEntityData;
                        if (value != null) yield return value;
                    }
                }
        }
    }

    [TypeId("0117c0e1a1b24c5d8e9f001122334401")]
    internal sealed class BleedIncrease0117 : UnitFactComponentDelegate, ITargetRulebookHandler<RuleDealDamage>
    {
        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (evt.Target != Owner || evt.IsFake || evt.DamageBundle?.First == null) return;
            var fact = evt.Reason?.Fact;
            var spell = fact?.Blueprint?.GetComponent<SpellDescriptorComponent>();
            if (spell == null || (spell.Descriptor & SpellDescriptor.Bleed) == 0) return;
            evt.DamageBundle.First.AddModifier(new Kingmaker.RuleSystem.Rules.Modifier(1, ModifierDescriptor.UntypedStackable));
        }
        public void OnEventDidTrigger(RuleDealDamage evt) { }
    }

    [TypeId("0117c0e1a1b24c5d8e9f001122334403")]
    internal sealed class DeathlyHealing0117 : UnitFactComponentDelegate, ITargetRulebookHandler<RuleHealDamage>, ITargetRulebookHandler<RuleDealDamage>
    {
        public void OnEventAboutToTrigger(RuleHealDamage evt)
        {
            if (evt.Target != Owner || evt.IsFake || evt.Interrupted) return;
            if (!HarmsAsPositive(DescriptorOf(evt.SourceFact), SourceName(evt), evt.SourceFact != null || evt.Reason?.Context?.SourceAbility != null)) return;
            int amount = Math.Max(0, evt.ValueWithoutReduction);
            evt.Interrupted = true;
            if (amount <= 0) return;
            var damage = new DirectDamage(new DiceFormula(0, DiceType.Zero), amount) { IgnoreReduction = true };
            Rulebook.Trigger(new RuleDealDamage(evt.Initiator, Owner, new DamageBundle(new BaseDamage[] { damage })));
        }
        public void OnEventDidTrigger(RuleHealDamage evt) { }
        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (evt.Target != Owner || evt.IsFake || evt.DamageBundle?.First == null) return;
            if (evt.DamageBundle.First.Type != DamageType.Energy) return;
            var energy = evt.DamageBundle.First as EnergyDamage;
            if (energy == null || energy.EnergyType != DamageEnergyType.NegativeEnergy) return;
            int amount = Math.Max(0, evt.DamageWithoutReduction);
            evt.DamageBundle.First.AddModifier(new Kingmaker.RuleSystem.Rules.Modifier(-amount, ModifierDescriptor.UntypedStackable));
            if (amount > 0) Rulebook.Trigger(new RuleHealDamage(evt.Initiator, Owner, amount));
        }
        public void OnEventDidTrigger(RuleDealDamage evt) { }
        internal static bool HarmsAsPositive(SpellDescriptor descriptor, string sourceName, bool sourced)
        {
            if ((descriptor & SpellDescriptor.ChannelNegativeHeal) != 0) return false;
            if ((descriptor & (SpellDescriptor.ChannelPositiveHeal | SpellDescriptor.ChannelPositiveHarm)) != 0) return true;
            if (!sourced) return false;
            var name = sourceName ?? "";
            if (name.IndexOf("Negative", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (name.IndexOf("Inflict", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return true;
        }
        static string SourceName(RuleHealDamage evt) =>
            evt.SourceFact?.Blueprint?.name ?? evt.Reason?.Context?.SourceAbility?.name ?? "";
        static SpellDescriptor DescriptorOf(Kingmaker.EntitySystem.EntityFact fact)
        {
            var component = fact?.Blueprint?.GetComponent<SpellDescriptorComponent>();
            return component == null ? SpellDescriptor.None : component.Descriptor;
        }
    }

    [TypeId("0117c0e1a1b24c5d8e9f001122334404")]
    internal sealed class FogConcealment0117 : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleConcealmentCheck>
    {
        public void OnEventAboutToTrigger(RuleConcealmentCheck evt)
        {
            if (evt.Initiator != Owner || evt.Target == null) return;
            var part = evt.Target.Get<Kingmaker.UnitLogic.Parts.UnitPartConcealment>();
            if (part == null) return;
            var field = AccessTools.Field(part.GetType(), "m_Concealments");
            var entries = field?.GetValue(part) as System.Collections.IEnumerable;
            if (entries == null) return;
            bool any = false, onlyFog = true;
            foreach (var entry in entries)
            {
                any = true;
                var desc = AccessTools.Field(entry.GetType(), "Descriptor")?.GetValue(entry);
                if (desc == null || desc.ToString() != "Fog") onlyFog = false;
            }
            if (any && onlyFog) AccessTools.Property(evt.GetType(), "ConcealmentValue")?.SetValue(evt, 0);
        }
        public void OnEventDidTrigger(RuleConcealmentCheck evt) { }
    }

    [TypeId("0117c0e1a1b24c5d8e9f001122334405")]
    internal sealed class WeatherSpeed0117 : UnitFactComponentDelegate, Kingmaker.Controllers.Units.ITickEachRound, IGlobalSubscriber
    {
        sealed class State { internal Kingmaker.EntitySystem.Stats.ModifiableValue.Modifier Speed; }
        static readonly ConditionalWeakTable<UnitEntityData, State> Speeds = new ConditionalWeakTable<UnitEntityData, State>();
        public void OnNewRound() => Refresh();
        protected override void OnTurnOn() => Refresh();
        protected override void OnTurnOff() => Clear();
        void Refresh()
        {
            if (Owner == null) return;
            var state = Speeds.GetOrCreateValue(Owner);
            if (BadWeather())
            {
                if (state.Speed == null) state.Speed = Owner.Stats.Speed.AddModifier(10, ModifierDescriptor.UntypedStackable);
            }
            else if (state.Speed != null)
            {
                Owner.Stats.Speed.RemoveModifier(state.Speed);
                state.Speed = null;
            }
        }
        void Clear()
        {
            if (Owner != null && Speeds.TryGetValue(Owner, out var state) && state.Speed != null)
                Owner.Stats.Speed.RemoveModifier(state.Speed);
            if (Owner != null) Speeds.Remove(Owner);
        }
        static bool BadWeather()
        {
            var weather = Game.Instance?.Player?.Weather;
            var actual = weather == null ? null : AccessTools.Property(weather.GetType(), "ActualWeather")?.GetValue(weather);
            return actual != null && Convert.ToInt32(actual) >= 2;
        }
    }

    [TypeId("0117c0e1a1b24c5d8e9f001122334406")]
    internal sealed class CrashingWaves0117 : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IInitiatorRulebookHandler<RuleDealDamage>
    {
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            var spell = evt.Spell;
            if (spell == null) return;
            int level = WizardLevel();
            if (Water(spell)) evt.AddBonusCasterLevel(level >= 8 ? 2 : 1);
            else if (Cold(spell) && level >= 8) evt.AddBonusCasterLevel(2);
        }
        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (evt.Initiator != Owner || evt.Target == null) return;
            var spell = evt.SourceAbility;
            if (spell == null) return;
            int level = WizardLevel();
            if (level < 16 && !Water(spell)) return;
            int dc = 10 + level / 2 + Math.Max(0, Owner.Stats.Intelligence.Bonus);
            var save = Rulebook.Trigger(new RuleSavingThrow(evt.Target, SavingThrowType.Fortitude, dc));
            if (save.IsPassed) return;
            evt.Target.Descriptor.State.AddCondition(UnitCondition.Prone);
        }
        public void OnEventDidTrigger(RuleDealDamage evt) { }
        int WizardLevel()
        {
            var wizard = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>("ba34257984f4c41408ce1dc2004e342e");
            return wizard == null ? 0 : Owner.Progression.GetClassLevel(wizard);
        }
        static bool Cold(BlueprintAbility spell)
        {
            var desc = spell.GetComponent<SpellDescriptorComponent>()?.Descriptor ?? SpellDescriptor.None;
            return (desc & SpellDescriptor.Cold) != 0;
        }
        static bool Water(BlueprintAbility spell)
        {
            var name = spell.name ?? "";
            return name.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Wave", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Hydraulic", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Tsunami", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    [TypeId("0117c0e1a1b24c5d8e9f001122334407")]
    internal sealed class SparklingAura0117 : UnitFactComponentDelegate, ITargetRulebookHandler<RuleAttackWithWeapon>
    {
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        // Shock the attacker only after a real hit with a metal weapon; the native metal test is Category.HasSubCategory(Metal).
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (evt.Target != Owner || evt.AttackRoll == null || !evt.AttackRoll.IsHit || evt.AttackRoll.IsFake) return;
            var type = evt.Weapon?.Blueprint?.Type;
            if (type == null || !type.Category.HasSubCategory(WeaponSubCategory.Metal)) return;
            var caster = Fact?.MaybeContext?.MaybeCaster ?? Owner;
            int cha = caster.Stats.Charisma.Bonus;
            if (cha <= 0) return;
            var damage = new EnergyDamage(new DiceFormula(0, DiceType.Zero), cha, DamageEnergyType.Electricity);
            Rulebook.Trigger(new RuleDealDamage(caster, Owner, new DamageBundle(new BaseDamage[] { damage })));
        }
    }

    static class ChargeMemory0117
    {
        sealed class State { internal int Left; internal bool Ready; }
        static readonly ConditionalWeakTable<Kingmaker.UnitLogic.Buffs.Buff, State> Table =
            new ConditionalWeakTable<Kingmaker.UnitLogic.Buffs.Buff, State>();
        internal static bool Spend(Kingmaker.UnitLogic.Buffs.Buff buff, int charges)
        {
            var state = Table.GetOrCreateValue(buff);
            if (!state.Ready) { state.Left = charges; state.Ready = true; }
            return --state.Left <= 0;
        }
        internal static int For(EntityFact fact)
        {
            var caster = fact?.MaybeContext?.MaybeCaster;
            var wizard = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>("ba34257984f4c41408ce1dc2004e342e");
            int level = caster == null || wizard == null ? 0 : caster.Progression.GetClassLevel(wizard);
            if (level >= 16) return 3;
            if (level >= 8) return 2;
            return 1;
        }
    }

    [TypeId("0117c0e1a1b24c5d8e9f001122334408")]
    internal sealed class WardCharges0117 : UnitFactComponentDelegate, ITargetRulebookHandler<RuleAttackWithWeapon>
    {
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (evt.Target != Owner || evt.AttackRoll == null || !evt.AttackRoll.IsHit || evt.AttackRoll.IsFake) return;
            var buff = Fact as Kingmaker.UnitLogic.Buffs.Buff;
            if (buff != null && ChargeMemory0117.Spend(buff, ChargeMemory0117.For(Fact))) buff.Remove();
        }
    }

    [TypeId("0117c0e1a1b24c5d8e9f00112233440a")]
    internal sealed class MistCharges0117 : UnitFactComponentDelegate, ITargetRulebookHandler<RuleAttackWithWeapon>
    {
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (evt.Target != Owner || evt.AttackRoll == null || evt.AttackRoll.IsHit || evt.AttackRoll.IsFake) return;
            if (evt.AttackRoll.Result.ToString().IndexOf("Conceal", StringComparison.Ordinal) < 0) return;
            var buff = Fact as Kingmaker.UnitLogic.Buffs.Buff;
            if (buff != null && ChargeMemory0117.Spend(buff, ChargeMemory0117.For(Fact))) buff.Remove();
        }
    }

    [TypeId("0117c0e1a1b24c5d8e9f001122334409")]
    internal sealed class MonsterLore0117 : ContextAction
    {
        public override string GetCaption() => "Monstrous Insight";
        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            var target = Target?.Unit;
            if (caster == null || target == null) return;
            var skill = SkillFor(target);
            int level = 0;
            var wizard = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>("ba34257984f4c41408ce1dc2004e342e");
            if (wizard != null) level = caster.Progression.GetClassLevel(wizard);
            var check = new RuleSkillCheck(caster, skill, InsightDC(target.Progression.CharacterLevel));
            if (level > 0) check.AddTemporaryModifier(caster.Stats.GetStat(skill).AddModifier(level, ModifierDescriptor.Insight));
            var result = Context.TriggerRule(check);
            Reveal(caster, target, result.RollResult);
        }
        // Identification DC is the native baseline only; the wizard level is the insight bonus, not a DC reduction.
        internal static int InsightDC(int targetCharacterLevel) => 10 + targetCharacterLevel;
        static void Reveal(UnitEntityData caster, UnitEntityData target, int roll)
        {
            var manager = Game.Instance?.Player?.InspectUnitsManager;
            if (manager == null) return;
            var blueprint = target.BlueprintForInspection;
            if (blueprint == null) return;
            var info = manager.GetInfo(blueprint);
            if (info == null || info.KnownPartsCount >= 4) return;
            info.SetCheck(roll);
            EventBus.RaiseEvent<IKnowledgeHandler>(h => h.HandleKnowledgeUpdated(info));
        }
        static StatType SkillFor(UnitEntityData unit)
        {
            var name = unit.Blueprint?.GetType().GetProperty("Type")?.GetValue(unit.Blueprint)?.ToString()
                ?? unit.Blueprint?.name ?? "";
            if (name.IndexOf("Undead", StringComparison.OrdinalIgnoreCase) >= 0) return StatType.SkillLoreReligion;
            if (name.IndexOf("Animal", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Vermin", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Plant", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Fey", StringComparison.OrdinalIgnoreCase) >= 0)
                return StatType.SkillLoreNature;
            if (name.IndexOf("Humanoid", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Construct", StringComparison.OrdinalIgnoreCase) >= 0)
                return StatType.SkillKnowledgeWorld;
            return StatType.SkillKnowledgeArcana;
        }
    }
}
