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
        private const string LifeLinkBuffId = "598200ff68114111ba44d2fa08750771";

        internal static readonly string[] PoolIds =
        {
            "247a4068296e8be42890143f451b4b45",
            "d332c1748445e8f4f9e92763123e31bd",
            "a21acdafc0169f5488a9bd3256e2e65b"
        };

        // One discovered shaman spirit. Nothing here is authored: the spirit
        // progression and its 1/8/20 features are read off the game's own
        // Shaman spirit selection at registration time, so a new spirit from
        // any other mod is picked up without a code change.
        private sealed class Row
        {
            internal string Name;                 // localized spirit name, used for display only
            internal string Token;                // stable progression GUID, used for private identities
            internal BlueprintProgression Progression;
            internal BlueprintFeature[] Tiers;    // L1, L8, L20 donor features (null when the donor has none)
            internal List<BlueprintFeature> Hexes = new List<BlueprintFeature>();
            // Per-tier resource shape copied from the donor itself.
            internal bool[] HasPool = new bool[3];
            internal int[] PoolBase = new int[3];
            internal StatType?[] PoolStat = new StatType?[3];
            internal bool[] PoolPerLevel = new bool[3];
            internal string[] TierNames = new string[3];
            // Powers the donor grants beyond its 1/8/20 axis (Lore's repeatable
            // Arcane Enlightenment picks at 10/12/14/16/18 and the second pick
            // at 8). Cloned by the same donors, published on the wizard axis.
            internal SortedDictionary<int, List<BlueprintFeatureBase>> Extra =
                new SortedDictionary<int, List<BlueprintFeatureBase>>();
        }

        // A shaman hex is a spirit's own when one of its Any-group
        // PrerequisiteFeature entries points at the spirit progression, its L1
        // feature or the spirit's wandering wrapper. Wrath authors hexes that
        // way (Battle Ward -> the Battle progression), so the ownership edge is
        // data, not a list we maintain.
        private const string ShamanSpiritSelectionId = "00c8c566d1825dd4a871250f35285982";
        private const string ShamanHexSelectionId = "4223fe18c75d4d14787af196a04e14e7";

        // The one authored rule. Life's own hexes gate on the oracle mysteries
        // that share them instead of on the Life spirit, so the ownership edge
        // is simply missing in the data. When a hex gates on a
        // PrerequisiteFeaturesFromList whose entries carry the same localized
        // name as a spirit, the hex belongs to that spirit. No GUID is stored.
        private static readonly string[] SpiritGroupSuffixes = { "Mystery", "Spirit", "MysteryProgression" };

        // Wrath keeps a spirit's own hexes out of the shared pool by gating each
        // hex on an Any-group prerequisite pointing back at the spirit. The one
        // exception is authored here rather than in data: Life's two hexes gate
        // on the oracle mysteries that also grant them (they were written for
        // the oracle side and the shaman edge was never added), so a hex whose
        // from-list prerequisites all resolve to features carrying the same
        // localized name as a spirit is attributed to that spirit. No GUID is
        // stored, so a renamed or reloaded mystery still matches.
        private static Row[] Discover(BlueprintsCache cache)
        {
            var selection = cache.Load(BlueprintGuid.Parse(ShamanSpiritSelectionId)) as BlueprintFeatureSelection;
            if (selection == null) return Array.Empty<Row>();

            var pool = cache.Load(BlueprintGuid.Parse(ShamanHexSelectionId)) as BlueprintFeatureSelection;
            if (pool == null) return Array.Empty<Row>();
            var hexes = ((BlueprintFeatureReference[])Get(pool, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>())
                .Select(r => r?.Get() as BlueprintFeature).Where(h => h != null).ToArray();

            var rows = new List<Row>();
            foreach (var reference in (BlueprintFeatureReference[])Get(selection, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>())
            {
                var progression = reference?.Get() as BlueprintProgression;
                if (progression == null) continue;
                if (!HasGroup(progression, "ShamanSpirit")) continue;
                var entries = progression.LevelEntries;
                var l1 = Features(entries, 1);
                var l8 = Features(entries, 8);
                var l20 = Features(entries, 20);
                if (l1.Count == 0 || l20.Count == 0) continue;

                // A "repeatable choice" power (Lore's Arcane Enlightenment) is a
                // feature selection of parametrized spell options, not a plain
                // spirit power. Cloning it as a tier would keep the donor's own
                // class binding, which is exactly why the pick never worked, so
                // it is routed through the rebinding path below instead. The
                // manifestation is the last plain feature of level 20 - Lore
                // lists its eighth enlightenment pick before it.
                var row = new Row
                {
                    Name = Name(progression),
                    Token = progression.AssetGuid.ToString().Replace("-", ""),
                    Progression = progression,
                    Tiers = new[]
                    {
                        l1.FirstOrDefault(f => Usable(f)),
                        l8.FirstOrDefault(f => Usable(f)),
                        l20.LastOrDefault(f => Usable(f))
                    }
                };
                if (row.Tiers[0] == null || row.Tiers[2] == null) continue;

                // Any-group prerequisite pointing at the spirit, its L1 feature or
                // the spirit's wandering wrapper (a progression that re-grants L1).
                var keys = new HashSet<BlueprintGuid> { progression.AssetGuid, row.Tiers[0].AssetGuid };
                foreach (var candidate in BlueprintSnapshot<BlueprintProgression>(cache))
                {
                    if (candidate == null || !References(candidate, progression.AssetGuid)) continue;
                    keys.Add(candidate.AssetGuid);
                    foreach (var entry in candidate.LevelEntries)
                        foreach (var fact in entry.Features)
                            if (fact is BlueprintFeature feature) keys.Add(feature.AssetGuid);
                }
                row.Hexes.AddRange(hexes.Where(h => Owned(h, keys, row.Name)));
                // Only the donor's repeatable spell choices travel as extras. The
                // shaman true spirit ability (16th) is explicitly NOT part of this
                // archetype's trade, and the manifestation already arrives as the
                // level-20 tier, so neither is picked up here.
                var taken = new HashSet<BlueprintGuid>(row.Tiers.Where(t => t != null).Select(t => t.AssetGuid));
                foreach (var entry in entries ?? Array.Empty<LevelEntry>())
                {
                    var facts = ((IEnumerable<BlueprintFeatureBase>)entry?.Features ?? Array.Empty<BlueprintFeatureBase>())
                        .OfType<BlueprintFeature>()
                        .Where(f => IsRebadged(f) && !taken.Contains(f.AssetGuid))
                        .ToList();
                    if (facts.Count == 0) continue;
                    if (!row.Extra.TryGetValue(entry.Level, out var bucket))
                        row.Extra[entry.Level] = bucket = new List<BlueprintFeatureBase>();
                    bucket.AddRange(facts);
                }
                rows.Add(row);
            }
            return rows.ToArray();
        }

        private static List<BlueprintFeature> Features(LevelEntry[] entries, int level)
            => (entries ?? Array.Empty<LevelEntry>()).Where(e => e != null && e.Level == level)
                .SelectMany(e => e.Features).OfType<BlueprintFeature>().ToList();

        // A "repeatable choice" power is a feature selection whose options are
        // parametrized spell picks (Lore's Arcane Enlightenment). It must travel
        // through the rebinding path: a plain memberwise clone would keep the
        // shaman class binding and the pick could never resolve.
        private static bool IsRebadged(BlueprintFeature feature)
        {
            if (!(feature is BlueprintFeatureSelection selection)) return false;
            var options = ((BlueprintFeatureReference[])Get(selection, "m_AllFeatures")
                ?? Array.Empty<BlueprintFeatureReference>())
                .Select(r => r?.Get()).OfType<BlueprintParametrizedFeature>().ToArray();
            return options.Length > 0 && options.Any(o => o.ParameterType == FeatureParameterType.LearnSpell);
        }

        // The native hidden "spirit magic" feature only carries the bonus-spell
        // list. A whisperer trade is exactly that: no spirit magic, so the
        // feature is skipped instead of being cloned onto the wizard axis.
        private static bool CarriesSpellList(BlueprintFeatureBase fact)
        {
            if (!(fact is BlueprintScriptableObject bp) || bp.ComponentsArray == null) return false;
            return bp.ComponentsArray.Any(c => c != null && c.GetType().Name.Contains("AddSpecialSpellList"));
        }

        // A tier slot must be a plain spirit power: not a repeatable spell choice
        // (those are rebound separately) and not the hidden bonus-spell carrier,
        // which the whisperer never receives.
        private static bool Usable(BlueprintFeature feature) =>
            !IsRebadged(feature) && !CarriesSpellList(feature);

        private static T[] BlueprintSnapshot<T>(BlueprintsCache cache) where T : BlueprintScriptableObject
        {
            var gate = Get(cache, "m_Lock");
            var loaded = Get(cache, "m_LoadedBlueprints") as System.Collections.IDictionary;
            if (gate == null || loaded == null) return Array.Empty<T>();
            var arguments = loaded.GetType().GetGenericArguments();
            if (arguments.Length < 2) return Array.Empty<T>();
            var blueprint = AccessTools.Field(arguments[1], "Blueprint");
            if (blueprint == null) return Array.Empty<T>();
            object[] entries = null;
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    lock (gate)
                    {
                        entries = new object[loaded.Count];
                        loaded.Values.CopyTo(entries, 0);
                    }
                    break;
                }
                catch (Exception error) when (attempt < 2 &&
                    (error is InvalidOperationException || error is ArgumentException || error is IndexOutOfRangeException))
                {
                }
            }
            return entries.Where(e => e != null).Select(e => blueprint.GetValue(e)).OfType<T>().ToArray();
        }

        private static bool References(BlueprintScriptableObject bp, BlueprintGuid guid)
        {
            bool found = false;
            Walk(bp.ComponentsArray, new HashSet<object>(PsychicAssets.IdentityComparer), o =>
            {
                if (found) return;
                foreach (var field in o.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (field.GetValue(o) is BlueprintReferenceBase reference && reference.Guid == guid) { found = true; return; }
                }
            });
            return found;
        }

        private static bool Owned(BlueprintFeature hex, HashSet<BlueprintGuid> keys, string spirit)
        {
            foreach (var component in hex.ComponentsArray)
            {
                if (component == null || !component.GetType().Name.StartsWith("Prerequisite")) continue;
                var single = AccessTools.Field(component.GetType(), "m_Feature")?.GetValue(component) as BlueprintReferenceBase;
                if (single != null && keys.Contains(single.Guid)) return true;
                var list = AccessTools.Field(component.GetType(), "m_Features")?.GetValue(component) as BlueprintReferenceBase[];
                if (list == null || list.Length == 0) continue;
                if (list.Any(r => r != null && keys.Contains(r.Guid))) return true;
                // The authored Life rule: every entry must be a same-named feature
                // of another class' group (oracle mystery), never a shaman one.
                if (list.All(r => SameName((r as BlueprintFeatureReference)?.Get(), spirit))) return true;
            }
            return false;
        }

        // Groups may be a field or a property depending on the blueprint type,
        // and its element type differs per class (FeatureGroup vs string), so
        // read it without assuming either and compare by name.
        private static bool HasGroup(SimpleBlueprint bp, string group)
        {
            if (bp == null) return false;
            object raw;
            try { raw = Get(bp, "Groups"); }
            catch (MissingFieldException)
            {
                var property = AccessTools.Property(bp.GetType(), "Groups");
                if (property == null) return false;
                raw = property.GetValue(bp, null);
            }
            if (!(raw is System.Collections.IEnumerable items)) return false;
            foreach (var item in items)
                if (item != null && item.ToString() == group) return true;
            return false;
        }

        private static bool SameName(SimpleBlueprint bp, string spirit)
        {
            if (bp == null) return false;
            if (!SpiritGroupSuffixes.Any(s => HasGroup(bp, s))) return false;
            return string.Equals(Name(bp), spirit, StringComparison.Ordinal);
        }

        // The localized spirit name; the asset name is a fallback for a locale
        // that has not resolved yet.
        private static string Name(SimpleBlueprint bp)
        {
            try
            {
                var text = (bp as BlueprintUnitFact)?.Name;
                if (!string.IsNullOrEmpty(text)) return text;
            }
            catch { /* localized string not resolved yet */ }
            return bp.name ?? "Spirit";
        }

        // A row's tier names and its private resource shape. The mapping is
        // inferred from the donor itself instead of a table: the L1 feature's
        // own resource is 3 + Charisma, the L8 feature consumes one per use
        // (fixed three) and the L20 manifestation is passive.
        private static void Classify(Row row)
        {
            for (int tier = 0; tier < 3; tier++)
            {
                var donor = row.Tiers[tier];
                row.TierNames[tier] = donor == null ? string.Empty : Name(donor);
                if (donor == null) continue;
                ReadPoolShape(donor, out row.HasPool[tier], out row.PoolBase[tier],
                    out row.PoolStat[tier], out row.PoolPerLevel[tier]);
            }
        }

        // Copy the donor's own ability-resource shape instead of guessing a
        // base value: native spirits differ per tier (L1 is always 3 + Charisma,
        // L8/L20 vary between fixed pools and none at all).
        private static void ReadPoolShape(BlueprintFeature feature, out bool has, out int baseValue,
            out StatType? stat, out bool perLevel)
        {
            has = false; baseValue = 0; stat = null; perLevel = false;
            if (feature == null) return;
            BlueprintAbilityResource found = null;
            Walk(feature.ComponentsArray, new HashSet<object>(PsychicAssets.IdentityComparer), o =>
            {
                if (found != null) return;
                var component = o as AddAbilityResources;
                if (component == null) return;
                var reference = Get(component, "m_Resource") as BlueprintAbilityResourceReference;
                found = reference?.Get();
            });
            if (found == null) return;
            var amount = Get(found, "m_MaxAmount");
            if (amount == null) return;
            has = true;
            baseValue = Convert.ToInt32(Get(amount, "BaseValue") ?? 0);
            perLevel = Equals(Get(amount, "IncreasedByLevel"), true);
            if (Equals(Get(amount, "IncreasedByStat"), true))
            {
                var value = Get(amount, "ResourceBonusStat");
                if (value is StatType type && type != StatType.Unknown)
                    // Wisdom is the shaman casting stat: the whisperer's pool
                    // scales on Intelligence instead. Charisma is kept as is.
                    stat = type == StatType.Wisdom ? StatType.Intelligence : type;
            }
        }
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
            yield return ShamanSpiritSelectionId; yield return ShamanHexSelectionId;
            // The Arcane Enlightenment rewrite reads the shaman class (for its
            // spell list) and the wizard list it excludes; both must exist before
            // anything is staged.
            yield return LoreArcane0119.ShamanId; yield return LoreArcane0119.WizardListId;
            foreach (var p in PoolIds) yield return p;
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
                        (BlueprintRegistration.StrictPublishedGraphChecks && ArchetypeBatch01445.Definition(state.Assets[i]) != state.Definitions[i]))
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
            // Discovery happens before the transaction: a spirit set that cannot
            // be read rejects the package instead of publishing a partial link.
            var rows = Discover(cache);
            if (rows.Length == 0) { Log("reject: 0117 no shaman spirit discovered"); return false; }
            foreach (var row in rows) Classify(row);
            var rootRef = (BlueprintArchetypeReference[])Get(wizard, "m_Archetypes");
            var uiGroups = wizard.Progression.UIGroups;
            var determinators = (BlueprintFeatureBaseReference[])Get(wizard.Progression, "m_UIDeterminatorsGroup");
            var pools = PoolIds.Select(g => cache.Load(BlueprintGuid.Parse(g)) as BlueprintFeatureSelection).ToArray();
            if (pools.Any(p => p == null)) { Log("reject: 0117 feat pool missing"); return false; }
            var poolSnapshots = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();

            BlueprintArchetype root = null;
            var nativeUndo = new List<Action>();
            try
            {
                // Must run before Build(): the private tier clones deep-copy the
                // donor's components, so they inherit the repaired variant menu.
                int repaired = RepairNativeManifestations(rows, nativeUndo);
                if (repaired > 0) Log("0117 repaired " + repaired + " native manifestation variant menu(s)");
                root = Build(f, wizard, cache, rows);
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
                    // Native blueprints are shared with plain Shamans: a rejected
                    // package must not leave any global edit behind.
                    RollbackNative(nativeUndo);
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
                    var extra = f.Has("ExtraChannel") ? f.Find<BlueprintFeature>("ExtraChannel") : null;

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
                        if (extra == null || !all.Any(r => r != null && r.Guid == nativeExtraGuid)) continue;
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
                // Covers a failure before inner undo() is even created, so the
                // native edits are always restored on a rejected package.
                RollbackNative(nativeUndo);
                Log("reject: 0117 " + error.GetType().Name + ": " + error.Message + "\n" + error.StackTrace);
                return false;
            }
        }

        // Each restore assigns back the exact array captured before the edit,
        // so replaying it (both undo paths) is harmless.
        private static void RollbackNative(List<Action> undo)
        {
            foreach (var restore in undo)
            {
                try { restore(); }
                catch (Exception error) { Log("0117 native rollback failed: " + error.Message); }
            }
            undo.Clear();
        }

        private static BlueprintArchetype Build(Factory f, BlueprintCharacterClass wizard, BlueprintsCache cache, Row[] rows)
        {
            var school = f.Native<BlueprintFeatureSelection>(SchoolId);
            var bond = f.Native<BlueprintFeatureSelection>(BondId);
            var wizardFeat = f.Native<BlueprintFeatureSelection>(WizardFeatId);

            // The exclusive Arcane Enlightenment rewrite: candidate spells are
            // the *shaman* list minus what the wizard list already offers, the
            // gates are Wisdom and Charisma (10 + circle each), and the pick is
            // written into the wizard spellbook. Built once per cache and shared
            // by every cloned option of this package.
            var shaman = f.Native<BlueprintCharacterClass>(LoreArcane0119.ShamanId);
            var shamanList = shaman?.Spellbook?.SpellList;
            var wizardList = f.Native<BlueprintSpellList>(LoreArcane0119.WizardListId);
            var arcane = shamanList == null || wizardList == null
                ? null
                : LoreArcane0119.Candidate(f.NextList, "ArcaneEnlightenmentList", shamanList, wizardList);

            // Spirit Link: exactly one spirit progression, never a second or
            // wandering spirit and no daily switching.
            var link = Selection(f, "SpiritLink", "Spirit Link",
                "At 1st level a spirit whisperer forms a bond with one spirit. He uses his wizard level as his shaman level for these abilities and his Intelligence modifier in place of Wisdom. He does not gain spirit magic spells or the shaman true spirit ability.",
                Array.Empty<BlueprintFeature>());

            var hexMaster = Selection(f, "SpiritHex", "Spirit Hex",
                "At 5th, 10th and 15th level a spirit whisperer selects one hex from his chosen spirit, or a wizard bonus feat. Wizard level and Intelligence replace shaman level and Wisdom.",
                Array.Empty<BlueprintFeature>());

            var progressions = new List<BlueprintProgression>();
            var hexSubs = new List<BlueprintFeature>();

            // Native spirits prepend the same shared "hexes granted" lead-in
            // through AddFeaturesFromSelectionToDescription. Life is the one
            // spirit whose native progression never carried that component,
            // so cache the shared introduction from whichever spirit does and
            // reuse it for every private list (including Life's).
            object sharedHexIntroduction = null;
            foreach (var row in rows)
            {
                var nativeList = row.Progression?.ComponentsArray?.FirstOrDefault(
                    c => c != null && c.GetType().Name == "AddFeaturesFromSelectionToDescription");
                if (nativeList == null) continue;
                sharedHexIntroduction = ReadIntroduction(nativeList);
                if (sharedHexIntroduction != null) break;
            }
            // A build without any donor component still gets the game's own
            // "Additional Hexes:" lead-in instead of an empty introduction.
            if (sharedHexIntroduction == null)
                sharedHexIntroduction = Text("SpiritWhisperer0117HexList", "introduction", "Additional Hexes:");

            foreach (var row in rows)
            {
                var features = new BlueprintFeature[3];
                for (int tier = 0; tier < 3; tier++)
                    features[tier] = Tier(f, cache, wizard, row, tier);

                // Off-axis donor powers (Lore's eight Arcane Enlightenment picks
                // and its 20th-level manifestation) are cloned onto the same
                // wizard axis instead of being dropped. Entries are merged per
                // level so a donor's second level-8 pick stays a second choice.
                var entries = new SortedDictionary<int, List<BlueprintFeatureBase>>();
                for (int tier = 0; tier < 3; tier++)
                    if (features[tier] != null)
                        entries[new[] { 1, 8, 20 }[tier]] = new List<BlueprintFeatureBase> { features[tier] };
                foreach (var pair in row.Extra)
                {
                    if (!entries.TryGetValue(pair.Key, out var bucket))
                        entries[pair.Key] = bucket = new List<BlueprintFeatureBase>();
                    int index = 0;
                    foreach (var donor in pair.Value)
                    {
                        var extra = ExtraFact(f, cache, wizard, row, donor, pair.Key, arcane, index);
                        if (extra != null) bucket.Add(extra);
                        index++;
                    }
                }

                var levelEntries = entries
                    .Where(p => p.Value.Count > 0)
                    .Select(p => Entry(p.Key, p.Value.ToArray())).ToArray();
                var prog = f.Progression(row.Token + "Progression", row.Name, string.Empty,
                    levelEntries,
                    new[] { Group(levelEntries.SelectMany(e => e.Features).ToArray()) });
                SetClasses(prog, wizard);
                // The spirit entry reuses the shaman spirit's own text and icon.
                Present(prog, row.Progression);
                progressions.Add(prog);

                // Only the chosen spirit's own hexes, never the shared shaman
                // pool: a whisperer takes hexes from his spirit alone.
                var hexFeatures = row.Hexes.Select((hex, i) =>
                    BuildHexFeature(f, cache, wizard, row, hex, i)).ToArray();
                var sub = Selection(f, row.Token + "HexSelection", row.Name + " Spirit Hex",
                    "Select one hex granted by the " + row.Name + " spirit.", hexFeatures,
                    Migration01426.Require(prog));
                Icon(sub, row.Progression);
                // Built after the sub-selection, which requires the progression
                // to exist first. Point the description at this spirit's own
                // hex list so no other spirit's hexes leak into the wording.
                DescribeHexes(prog, sub, sharedHexIntroduction);
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
            // Life is discovered by name, so a game without a Life spirit simply
            // skips the clone instead of rejecting the package.
            var life = rows.FirstOrDefault(r => string.Equals(r.Name, "Life", StringComparison.OrdinalIgnoreCase));
            if (life != null && f.Has(life.Token + "Tier0") && f.Has(life.Token + "Tier0Pool"))
            {
                var nativeExtra = f.Native<BlueprintFeature>(ExtraChannelId);
                var extra = (BlueprintFeature)f.Clone(nativeExtra, "ExtraChannel",
                    "Extra Channel (Spirit Whisperer Life Spirit)",
                    "You can channel the Life spirit's positive energy two additional times per day. Requires Channel Positive Energy from this archetype.");
                var increase = extra.ComponentsArray.FirstOrDefault(c => c != null && c.GetType().Name == "IncreaseResourceAmount");
                if (increase == null) throw new InvalidOperationException("native Extra Channel resource component changed.");
                var lifeChannel = f.Find<BlueprintFeature>(life.Token + "Tier0");
                var lifeResource = f.Find<BlueprintAbilityResource>(life.Token + "Tier0Pool");
                Set(increase, "m_Resource", Ref<BlueprintAbilityResourceReference>(lifeResource));
                Set(increase, "Value", 2);
                extra.ComponentsArray = extra.ComponentsArray.Where(c => !(c is Prerequisite))
                    .Concat(new BlueprintComponent[] { Migration01426.Require(lifeChannel) }).ToArray();
            }

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

        // Native shaman spirits append their hex list to the description through
        // AddFeaturesFromSelectionToDescription pointing at a feature selection.
        // Our private progression points it at this spirit's own sub-selection,
        // so only this spirit's hexes are listed. OnlyIfRequiresThisFeature must
        // be false: the cloned hexes keep only PrerequisiteNoFeature, so the
        // native "requires this feature" filter would hide every entry.
        // The introduction is the native shared lead-in, reused for every spirit
        // including Life (whose native progression never carried this component
        // at all - the game's own omission).
        private static void DescribeHexes(BlueprintProgression progression, BlueprintFeatureSelection selection,
            object sharedIntroduction)
        {
            if (progression == null || selection == null) return;
            var fields = new List<KeyValuePair<string, object>>
            {
                F("m_FeatureSelection", Ref<BlueprintFeatureSelectionReference>(selection)),
                F("OnlyIfRequiresThisFeature", false)
            };
            if (sharedIntroduction != null) fields.Add(F("Introduction", sharedIntroduction));
            var hexList = Component("AddFeaturesFromSelectionToDescription", fields.ToArray());
            if (hexList == null) return; // component type absent in this build
            hexList.name = "$AddFeaturesFromSelectionToDescription$" + selection.name;
            var typeName = hexList.GetType().Name;
            progression.ComponentsArray = (progression.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Where(c => c != null && c.GetType().Name != typeName)
                .Concat(new[] { hexList }).ToArray();
        }

        // The native component exposes the lead-in as a field; some builds
        // surface it as a property, so read whichever the runtime provides.
        private static object ReadIntroduction(object component)
        {
            var field = AccessTools.Field(component.GetType(), "Introduction");
            if (field != null) return field.GetValue(component);
            var property = AccessTools.Property(component.GetType(), "Introduction");
            return property != null ? property.GetValue(component, null) : null;
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

        // The stat substitution is exactly one direction: Intelligence replaces
        // Wisdom. Charisma is never replaced, so a resource or DC that names
        // Charisma keeps naming it.
        private const StatType DcStat = StatType.Intelligence;

        private static BlueprintFeature Tier(Factory f, BlueprintsCache cache, BlueprintCharacterClass wizard, Row row, int tier)
        {
            string token = row.Token + "Tier" + tier;
            string display = row.TierNames[tier];

            var donorFeature = row.Tiers[tier];
            // A spirit may have no plain power on one tier slot (Lore's level 8
            // and 20 both carry repeatable spell picks); those slots are filled
            // by the rebinding path instead.
            if (donorFeature == null) return null;
            var feature = (BlueprintFeature)f.Clone(donorFeature, token, display, string.Empty);
            StripSpellLists(feature);
            // Keep the spirit power's own wording and icon.
            Present(feature, donorFeature);
            feature.ComponentsArray = feature.ComponentsArray
                .Where(c => c != null && c.GetType().Name != "AddAbilityResources").ToArray();

            BlueprintAbilityResource resource = null;
            if (row.HasPool[tier])
            {
                resource = GetOrCreateResource(f, token + "Pool", row.PoolBase[tier], row.PoolStat[tier], row.PoolPerLevel[tier]);
                // A fixed pool that is spent per use carries the original 1d4
                // round lockout; a stat- or level-scaled pool does not.
                if (!row.PoolPerLevel[tier] && !row.PoolStat[tier].HasValue && HasConsumption(feature))
                    AddCooldown(f, feature, token, wizard);
            }
            Rebind(feature, wizard, DcStat, _ => resource);
            CloneDirectAbilities(f, wizard, feature, token, DcStat, resource);
            if (resource != null)
                feature.ComponentsArray = feature.ComponentsArray.Concat(new BlueprintComponent[] { Grant(resource) }).ToArray();
            return feature;
        }


        private static BlueprintAbilityResource GetOrCreateResource(Factory f, string token, int baseValue, StatType? stat, bool perLevel)
        {
            if (f.Assets.TryGetValue(token, out var existing)) return (BlueprintAbilityResource)existing;
            return Resource(f, token, baseValue, stat, perLevel);
        }

        // One donor fact that is not on the archetype's 1/8/20 axis. A repeatable
        // spell choice is rebuilt as a private selection over rebinding cloning of
        // the donor's own parametrized options, so the whisperer's version asks for
        // Wisdom and Charisma and writes into the wizard book. Anything else is a
        // plain spirit power and takes the ordinary clone path.
        private static BlueprintFeatureBase ExtraFact(Factory f, BlueprintsCache cache, BlueprintCharacterClass wizard,
            Row row, BlueprintFeatureBase donor, int level, BlueprintSpellList arcane, int index)
        {
            if (donor is BlueprintFeatureSelection selection && IsRebadged(selection))
                return ArcaneSelection(f, wizard, row, selection, level, index, arcane);

            if (!(donor is BlueprintFeature feature)) return null;
            string token = row.Token + "Extra" + level + "Feature" + f.Assets.Count;
            var clone = (BlueprintFeature)f.Clone(feature, token, Name(feature), string.Empty);
            StripSpellLists(clone);
            Present(clone, feature);
            clone.ComponentsArray = clone.ComponentsArray
                .Where(c => c != null && c.GetType().Name != "AddAbilityResources").ToArray();
            var resource = HasConsumption(clone) ? GetOrCreateResource(f, token + "R1", 3, StatType.Charisma, false) : null;
            Rebind(clone, wizard, DcStat, _ => resource);
            CloneDirectAbilities(f, wizard, clone, token, DcStat, resource);
            if (resource != null)
                clone.ComponentsArray = clone.ComponentsArray.Concat(new BlueprintComponent[] { Grant(resource) }).ToArray();
            return clone;
        }

        // The nine parametrized options are cloned once per spirit and shared by
        // every selection of that spirit: a player sees the same 1..9 circle menu
        // at every Arcane Enlightenment step, whichever level granted it.
        private static List<BlueprintFeature> ArcaneOptions(Factory f, BlueprintCharacterClass wizard, Row row,
            BlueprintFeatureSelection donor, BlueprintSpellList arcane)
        {
            string token = row.Token + "ArcaneOptions";
            if (f.Scratch.TryGetValue(token, out var cached))
                return (List<BlueprintFeature>)cached;

            var options = ((BlueprintFeatureReference[])Get(donor, "m_AllFeatures")
                ?? Array.Empty<BlueprintFeatureReference>())
                .Select(r => r?.Get()).OfType<BlueprintParametrizedFeature>().ToArray();
            var clones = new List<BlueprintFeature>();
            for (int i = 0; i < options.Length; i++)
            {
                var source = options[i];
                int circle = source.SpellLevel > 0 ? source.SpellLevel : i + 1;
                var clone = (BlueprintParametrizedFeature)f.Clone(source,
                    token + circle, source.Name, source.Description);
                // Wisdom gates the shaman-list pick the whisperer is allowed to
                // take; Charisma stays the secondary requirement of the original.
                LoreArcane0119.Rebind(clone, wizard, arcane, StatType.Wisdom, StatType.Charisma, circle);
                clones.Add(clone);
            }
            f.Scratch[token] = clones;
            return clones;
        }

        // One selection per granted step. Two picks on the same level need two
        // distinct blueprints: Wrath folds identical references in one LevelEntry
        // into a single choice, which is what made the 8th-level pick count as one.
        private static BlueprintFeatureSelection ArcaneSelection(Factory f, BlueprintCharacterClass wizard, Row row,
            BlueprintFeatureSelection donor, int level, int index, BlueprintSpellList arcane)
        {
            var options = ArcaneOptions(f, wizard, row, donor, arcane);
            string token = row.Token + "Arcane" + level + "x" + index;
            if (f.Assets.TryGetValue(token, out var cached)) return (BlueprintFeatureSelection)cached;
            var result = f.Add(new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true },
                token, donor.Name, donor.Description);
            Set(result, "m_Features", options.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(result, "m_AllFeatures", options.Select(Ref<BlueprintFeatureReference>).ToArray());
            Icon(result, donor);
            return result;
        }

        private static KeyValuePair<string, object> F(string key, object value) => new KeyValuePair<string, object>(key, value);

        private static ContextValue Rank(AbilityRankType type = AbilityRankType.Default)
            => new ContextValue { ValueType = ContextValueType.Rank, ValueRank = type };

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
        // A donor may be granted by more than one AddFacts component (Flame's
        // Touch of Flames is listed twice), so each donor is cloned once and
        // every reference points at that single private copy. Variant menus
        // inside a clone still name the native children, so they are remapped
        // onto the private copies below.
        private static void CloneDirectAbilities(Factory f, BlueprintCharacterClass wizard, BlueprintFeature feature,
            string token, StatType dcStat, BlueprintAbilityResource resource)
        {
            DeduplicateAddFacts(feature);
            int index = 0;
            var clones = new Dictionary<BlueprintGuid, BlueprintUnitFact>();
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
                        if (!clones.TryGetValue(donor.AssetGuid, out var clone))
                        {
                            clone = (BlueprintUnitFact)f.Clone(donor,
                                token + "Ability" + index, donor.name, string.Empty);
                            StripSpellLists(clone);
                            if (Has(clone, "m_Parent")) Set(clone, "m_Parent", null);
                            Rebind(clone, wizard, dcStat, _ => resource);
                            // The ability is what the character sheet actually lists,
                            // so it must carry the original wording and icon too.
                            Present(clone, (BlueprintUnitFact)donor);
                            clones.Add(donor.AssetGuid, clone);
                            index++;
                        }
                        rewritten.Add(Ref<BlueprintUnitFactReference>(clone));
                    }
                    else rewritten.Add(reference);
                }
                Set(component, "m_Facts", rewritten.ToArray());
            }
            foreach (var clone in clones.Values) RebindVariants(clone, clones);
        }

        // Native Flame L1 grants Touch of Flames through two AddFacts components
        // that share the same component name; the game folds those into one by
        // name, but Prepare()/UniqueNames() renames duplicate components, so both
        // would take effect and the action bar would list the ability twice.
        // Drop AddFacts components with an identical name and fact list before
        // UniqueNames() runs. Other component shapes are left untouched.
        private static void DeduplicateAddFacts(BlueprintFeature feature)
        {
            var seen = new HashSet<string>();
            var kept = new List<BlueprintComponent>();
            foreach (var component in feature.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                if (component != null && component.GetType().Name == "AddFacts" && Has(component, "m_Facts"))
                {
                    var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts")
                        ?? Array.Empty<BlueprintUnitFactReference>();
                    var key = (component.name ?? "") + "|" +
                              string.Join("|", refs.Select(r => r == null ? "" : r.Guid.ToString()));
                    if (!seen.Add(key)) continue;
                }
                kept.Add(component);
            }
            feature.ComponentsArray = kept.ToArray();
        }

        // Owlcat's own L20 manifestation blueprints list Bolster twice and omit
        // Reach in ActivatableAbilityVariants.m_Variants, even though AddFacts
        // grants all three metamagic toggles. Rebuild the menu from the granted
        // facts so a plain Shaman also sees Extend / Bolster / Reach. The edit is
        // idempotent and every change records a reversible undo action.
        private static int RepairNativeManifestations(Row[] rows, List<Action> undo)
        {
            int repaired = 0;
            foreach (var row in rows)
            {
                var l20 = row.Tiers[2];
                if (l20 == null) continue;
                var granted = new List<BlueprintUnitFact>();
                foreach (var component in l20.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                {
                    if (component == null || component.GetType().Name != "AddFacts" || !Has(component, "m_Facts")) continue;
                    var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts")
                        ?? Array.Empty<BlueprintUnitFactReference>();
                    foreach (var reference in refs)
                    {
                        var fact = reference?.Get() as BlueprintUnitFact;
                        if (fact != null && !granted.Any(g => ReferenceEquals(g, fact))) granted.Add(fact);
                    }
                }
                foreach (var parent in granted)
                {
                    var variants = parent.ComponentsArray?.FirstOrDefault(
                        c => c != null && c.GetType().Name == "ActivatableAbilityVariants");
                    if (variants == null) continue;
                    var field = AccessTools.Field(variants.GetType(), "m_Variants");
                    if (field == null || !field.FieldType.IsArray) continue;
                    var siblings = granted.Where(g => !ReferenceEquals(g, parent)).ToArray();
                    if (siblings.Length < 2) continue;
                    var element = field.FieldType.GetElementType();
                    var rebuilt = Array.CreateInstance(element, siblings.Length);
                    bool complete = true;
                    for (int i = 0; i < siblings.Length; i++)
                    {
                        var mapped = VariantReference(element, siblings[i]);
                        if (mapped == null) { complete = false; break; }
                        rebuilt.SetValue(mapped, i);
                    }
                    if (!complete) continue;
                    var current = field.GetValue(variants) as Array;
                    if (current != null && current.Length == rebuilt.Length)
                    {
                        bool same = true;
                        for (int i = 0; i < current.Length; i++)
                        {
                            var a = (current.GetValue(i) as BlueprintReferenceBase)?.Guid;
                            var b = (rebuilt.GetValue(i) as BlueprintReferenceBase)?.Guid;
                            if (a != b) { same = false; break; }
                        }
                        if (same) continue;
                    }
                    var captured = current;
                    field.SetValue(variants, rebuilt);
                    undo.Add(() => field.SetValue(variants, captured));
                    repaired++;
                }
            }
            return repaired;
        }

        // ActivatableAbilityVariants / AbilityVariants are deep-copied together
        // with their parent ability, but their m_Variants still name the native
        // children. The character owns only the private clones, so a menu that
        // keeps the native GUIDs shows no options (the L20 Flame/Frost/Stone/
        // Waves/Wind manifestation metamagic choices). Point the menu at the
        // private copies instead. Native entries with no private clone are
        // left untouched, so this is a no-op for any other feature shape.
        private static void RebindVariants(BlueprintUnitFact clone, Dictionary<BlueprintGuid, BlueprintUnitFact> clones)
        {
            foreach (var component in clone.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                var name = component?.GetType().Name;
                if (name != "ActivatableAbilityVariants" && name != "AbilityVariants") continue;
                var field = AccessTools.Field(component.GetType(), "m_Variants");
                if (field == null || !field.FieldType.IsArray) continue;
                var old = field.GetValue(component) as Array;
                if (old == null || old.Length == 0) continue;
                var element = field.FieldType.GetElementType();
                var rebuilt = Array.CreateInstance(element, old.Length);
                for (int i = 0; i < old.Length; i++)
                {
                    var reference = old.GetValue(i) as BlueprintReferenceBase;
                    if (reference != null && clones.TryGetValue(reference.Guid, out var mapped))
                        rebuilt.SetValue(VariantReference(element, mapped) ?? old.GetValue(i), i);
                    else rebuilt.SetValue(old.GetValue(i), i);
                }
                field.SetValue(component, rebuilt);
            }
        }

        private static object VariantReference(Type element, BlueprintUnitFact target)
        {
            if (element == typeof(BlueprintActivatableAbilityReference)) return Ref<BlueprintActivatableAbilityReference>(target);
            if (element == typeof(BlueprintAbilityReference)) return Ref<BlueprintAbilityReference>(target);
            if (element == typeof(BlueprintUnitFactReference)) return Ref<BlueprintUnitFactReference>(target);
            try
            {
                var create = typeof(BlueprintReferenceBase).GetMethods(
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == "CreateTyped" && m.IsGenericMethodDefinition &&
                                         m.GetParameters().Length == 1);
                if (create == null) return null;
                var reference = create.MakeGenericMethod(element).Invoke(null, new object[] { target });
                AccessTools.Property(typeof(BlueprintReferenceBase), "Cached")?.SetValue(reference, target, null);
                return reference;
            }
            catch { return null; }
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
        // survives. A per-level pool (R5) is used where the donor charges per
        // round rather than per use.
        private static BlueprintFeature BuildHexFeature(Factory f, BlueprintsCache cache, BlueprintCharacterClass wizard,
            Row row, BlueprintFeature hex, int index)
        {
            string token = row.Token + "Hex" + index;
            string name = Name(hex);
            var feature = (BlueprintFeature)f.Clone(hex, token, name, string.Empty);
            StripSpellLists(feature);
            // Keep the hex's own wording and icon.
            Present(feature, hex);
            // Drop the donor's resource grant and every oracle/revelation
            // prerequisite; keep PrerequisiteNoFeature so a hex cannot be taken twice.
            feature.ComponentsArray = feature.ComponentsArray
                .Where(c => c != null && c.GetType().Name != "AddAbilityResources" &&
                            (!(c is Prerequisite) || c is PrerequisiteNoFeature)).ToArray();

            BlueprintAbilityResource resource = null;
            if (HasPerRoundSpend(feature)) resource = GetOrCreateResource(f, token + "R5", 0, null, true);
            else if (HasConsumption(feature)) resource = GetOrCreateResource(f, token + "R1", 3, StatType.Charisma, false);

            Rebind(feature, wizard, DcStat, _ => resource);
            CloneDirectAbilities(f, wizard, feature, token, DcStat, resource);
            if (resource != null)
                feature.ComponentsArray = feature.ComponentsArray.Concat(new BlueprintComponent[] { Grant(resource) }).ToArray();
            if (name == "Life Link" && resource != null)
                AddLifeLinkRefund(f, feature, resource);
            if (name == "Mental Acuity") FixMentalAcuity(feature);
            if (name == "Entangling Curse") FixEntanglingMinimum(feature);
            if (name == "Friend to Animals") RetargetSummon(f, feature, wizard, token);
            return feature;
        }

        // A round-by-round toggle (Life Sight) charges on NewRound instead of on
        // activation, so it needs the per-level pool rather than a use count.
        private static bool HasPerRoundSpend(BlueprintScriptableObject bp)
        {
            bool found = false;
            Walk(bp.ComponentsArray, new HashSet<object>(PsychicAssets.IdentityComparer), o =>
            {
                var logic = o as ActivatableAbilityResourceLogic;
                if (logic != null && logic.SpendType == ActivatableAbilityResourceLogic.ResourceSpendType.NewRound) found = true;
            });
            return found;
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
                    var clone = (BlueprintFeature)f.Clone(donor, token + "Summon" + index, donor.name, string.Empty);
                    Present(clone, donor);
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

        private static void SetEnum(object obj, string field, string name)
        {
            var target = AccessTools.Field(obj.GetType(), field) ?? throw new MissingFieldException(obj.GetType().FullName, field);
            target.SetValue(obj, Enum.Parse(target.FieldType, name));
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
                    refund = (BlueprintBuff)f.Clone(donor, "LifeLinkRefundBuff", "Life Link", string.Empty);
                    Present(refund, donor);
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
}
