using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.Mechanics.Buffs;
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
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.SpiritGuideBridge0060;

namespace CallOfTheWild.Wrath.Prototype
{
    // COTW-0060 / F-0060-SPIRITS. The Oracle Spirit Guide catalogue is derived
    // at registration time from the live Shaman Spirit and Hex selections. Each
    // discovered spirit contributes its level-1/8 powers, owned hexes and nine
    // spirit-magic spells; those donors are cloned and rebound to the Oracle
    // 3/4/7/15 progression with Charisma/Wisdom rules. Mod-added spirits therefore
    // participate automatically. The private Life Extra Channel remains authored
    // from the original CotW methods. Every mutable subgraph is private: no
    // native blueprint component, spell list, buff or resource is edited.
    internal static class SpiritGuideSpirits0060
    {
        internal const string ParametrizedFeatureId = "bcd757ac2aeef3c49b77e5af4e510956";
        internal const string ExtraChannelId = "cd9f19775bd9d3343a31a065e93f0c47";
        internal const string FaerieFireBuffId = "cc383a9eaae4d2b45a925d442b367b54";
        internal const string GlitterdustId = "ce7dad2b25acf85429b6c9550787b2d9";
        private const string BlindnessBuffId = "187f88d96a0ef464280706b63635f2af";
        private const string ConfusedBuffId = "886c7407dc629dc499b9f1465ff382df";
        private const string EntangleBuffId = "f7f6330726121cf4b90a6086b05d2e38";
        private const string BlurBuffId = "dd3ad347240624d46a11a092b4dd4674";
        private const string SlowAbilityId = "f492622e473d34747806bdb39356eb89";
        private const string ConstructFeatureId = "fd389783027d63343b4a5634bd81645f";
        private const string DetectMagicIconId = "ee0b69e90bac14446a4cf9a050f87f2e";
        private const string RestorationIconId = "f2115ac1148256b4ba20788f7e966830";
        private const string BlurIconId = "14ec7a4e52e90fa47a4c8d63c69fd5c1";
        private const string AnimalFactId = "a95311b3dc996964cbaa30ff9965aaf6";
        private const string FriendCarrierBuffId = "05cdce24bc6c1b547a566faad6853579";
        private const string FriendDonorId = "0d92799cecf414249a23ad82f7e9901a";
        private const string FluidMagicIconId = "e3f41966c2d662a4e9582a0497621c46";
        private const string ChargedBlastIconId = "40681ea748d98f54ba7f5dc704507f39";

        // The native feat pools that already offer the native Extra Channel feat;
        // the private clone may only enter these (same gate as 0117).
        internal static readonly string[] ExtraChannelPoolIds =
        {
            "247a4068296e8be42890143f451b4b45",
            "d332c1748445e8f4f9e92763123e31bd",
            "a21acdafc0169f5488a9bd3256e2e65b"
        };

        // A hex either clones a real Wrath donor feature (Donor != null) or is
        // built privately from native components (Donor == null). Resource is the
        // contract shape: 0 passive, 2 = R2 (1 + Wisdom), 5 = R5 (one per Oracle level).
        internal sealed class HexSpec
        {
            internal string Name;
            internal string Donor;
            internal int Resource;
            internal HexSpec(string name, string donor, int resource) { Name = name; Donor = donor; Resource = resource; }
        }

        internal sealed class Row
        {
            internal string Name;             // localized spirit name, display only
            internal string Token;            // stable native progression GUID, private identities
            internal BlueprintProgression Progression;
            internal string[] Tiers;          // L1 / L8 donor feature GUIDs
            internal HexSpec[] Hexes;         // discovered from the native hex pool
            internal int[] Resource;          // donor-derived: 0 none, 1 R1, 2 R2, 3 R3
            internal string[] TierNames;
            internal string[] Spells;         // nine bonded spells, circles 1..9
            internal bool ExtraChannel;       // Life publishes a private Extra Channel feat
            // Powers the donor grants beyond its L1/L8 axis. Lore carries the
            // repeatable Arcane Enlightenment chain here (a spell-pick selection
            // at 8/10/12/14/16/18/20 plus its manifestation at 20), and dropping
            // it is what made the guide's Lore strictly worse than the shaman's.
            internal SortedDictionary<int, List<BlueprintFeature>> Extra =
                new SortedDictionary<int, List<BlueprintFeature>>();
        }

        const string ShamanSpiritSelectionId = "00c8c566d1825dd4a871250f35285982";
        const string ShamanHexSelectionId = "4223fe18c75d4d14787af196a04e14e7";

        // COTW-0119: the spirit catalogue is no longer authored here. Every
        // ShamanSpirit progression published in the native selection - Owlcat's
        // own, this project's Lore spirit, and any spirit another mod registers
        // - is discovered at registration time. Tiers come from LevelEntry 1/8,
        // hexes from the native hex pool's prerequisite edges, and the nine
        // bonded spells from the progression's own AddSpellsToDescription list.
        // Only the Oracle-side rules below (3/4/7/15 axis, Charisma/Wisdom
        // rebinding, temporary known spells, Extra Channel) remain authored.
        internal static Row[] Discover(BlueprintsCache cache)
        {
            var pool = cache.Load(BlueprintGuid.Parse(ShamanSpiritSelectionId)) as BlueprintFeatureSelection;
            var hexPool = cache.Load(BlueprintGuid.Parse(ShamanHexSelectionId)) as BlueprintFeatureSelection;
            if (pool == null || hexPool == null) return Array.Empty<Row>();

            var rows = new List<Row>();
            foreach (var reference in (BlueprintFeatureReference[])Get(pool, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>())
            {
                var progression = reference?.Get() as BlueprintProgression;
                if (progression == null || !HasGroup(progression, "ShamanSpirit")) continue;
                var l1 = Plain(progression.LevelEntries, 1);
                if (l1 == null) continue;
                // The greater power is the plain feature the spirit grants at 8th.
                // A spirit whose 8th level is its repeatable choice instead (Lore's
                // Arcane Enlightenment) has no plain greater power at all: its
                // "greater ability" IS that chain, which is carried over below on
                // the shaman's own levels. Substituting the shaman's 16th-level
                // true ability here would hand the guide a power the archetype
                // explicitly trades away.
                var greater = Plain(progression.LevelEntries, 8);
                var spells = DiscoverSpells(progression);
                if (spells == null) continue;

                var keys = new HashSet<BlueprintGuid> { progression.AssetGuid, l1.AssetGuid };
                var hexes = DiscoverHexes(hexPool, keys);
                if (hexes.Length == 0) continue;

                string name = NameOf(progression);
                var row = new Row
                {
                    Name = name,
                    Token = progression.AssetGuid.ToString().Replace("-", ""),
                    Progression = progression,
                    Tiers = new[] { l1.AssetGuid.ToString().Replace("-", ""), greater == null ? null : greater.AssetGuid.ToString().Replace("-", "") },
                    TierNames = new[] { NameOf(l1), greater == null ? string.Empty : NameOf(greater) },
                    Hexes = hexes.Select(h => new HexSpec(NameOf(h), h.AssetGuid.ToString().Replace("-", ""), 0)).ToArray(),
                    Resource = new[] { InferResource(l1), greater == null ? 0 : InferResource(greater) },
                    Spells = spells,
                    ExtraChannel = string.Equals(name, "Life", StringComparison.OrdinalIgnoreCase)
                };
                // The guide trades away the shaman's true spirit ability and its
                // manifestation, so only the spirit's repeatable choices are
                // carried over - Lore's Arcane Enlightenment picks, which the
                // 3/4/7/15 axis has no slot for and whose absence made the
                // guide's Lore strictly worse than a real shaman's.
                var taken = new HashSet<BlueprintGuid> { l1.AssetGuid };
                if (greater != null) taken.Add(greater.AssetGuid);
                foreach (var entry in progression.LevelEntries ?? Array.Empty<LevelEntry>())
                {
                    if (entry?.Features == null) continue;
                    var facts = ((IEnumerable<BlueprintFeatureBase>)entry.Features)
                        .OfType<BlueprintFeature>()
                        .Where(f => !f.HideInUI && !taken.Contains(f.AssetGuid) && IsArcaneChoice(f))
                        .ToList();
                    if (facts.Count == 0) continue;
                    if (!row.Extra.TryGetValue(entry.Level, out var bucket))
                        row.Extra[entry.Level] = bucket = new List<BlueprintFeature>();
                    bucket.AddRange(facts);
                }
                rows.Add(row);
            }
            return rows.ToArray();
        }

        // A spirit's tier slot must be a real power, not a menu of parametrized
        // spell picks: those are rebound separately as repeatable choices.
        static BlueprintFeature Plain(LevelEntry[] entries, int level)
        {
            foreach (var entry in entries ?? Array.Empty<LevelEntry>())
            {
                if (entry == null || entry.Level != level || entry.Features == null) continue;
                foreach (var fact in entry.Features)
                    if (fact is BlueprintFeature feature && !feature.HideInUI && !IsArcaneChoice(feature)) return feature;
            }
            return null;
        }

        static bool IsArcaneChoice(BlueprintFeature feature)
        {
            if (!(feature is BlueprintFeatureSelection selection)) return false;
            return ((BlueprintFeatureReference[])Get(selection, "m_AllFeatures")
                ?? Array.Empty<BlueprintFeatureReference>())
                .Select(r => r?.Get()).OfType<BlueprintParametrizedFeature>()
                .Any(o => o.ParameterType == FeatureParameterType.LearnSpell);
        }

        // The native Spirit progression already carries the nine-circle table
        // through AddSpellsToDescription. This is the same object the real
        // Shaman uses, so any fix or replacement applied to it is inherited.
        static string[] DiscoverSpells(BlueprintProgression progression)
        {
            var component = progression.GetComponent<AddSpellsToDescription>();
            if (component == null) return null;
            var lists = Get(component, "m_SpellLists") as BlueprintSpellListReference[];
            if (lists == null || lists.Length == 0) return null;
            var list = lists[0]?.Get() as BlueprintSpellList;
            if (list?.SpellsByLevel == null) return null;
            var result = new string[9];
            for (int ring = 1; ring <= 9; ring++)
            {
                var entry = list.SpellsByLevel.FirstOrDefault(e => e != null && e.SpellLevel == ring);
                if (entry == null) return null;
                var spells = Get(entry, "m_Spells") as System.Collections.IList;
                if (spells == null || spells.Count == 0) return null;
                var reference = spells[0] as BlueprintReferenceBase;
                if (reference == null) return null;
                result[ring - 1] = reference.Guid.ToString().Replace("-", "");
            }
            return result;
        }

        // A hex belongs to a spirit when one of its Any-group prerequisites
        // points at the spirit progression or its level-1 power. Wrath authors
        // hexes that way, and so does COTW-0119, so ownership is data, not a
        // list maintained here.
        static BlueprintFeature[] DiscoverHexes(BlueprintFeatureSelection hexPool, HashSet<BlueprintGuid> keys)
        {
            var result = new List<BlueprintFeature>();
            foreach (var reference in (BlueprintFeatureReference[])Get(hexPool, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>())
            {
                var hex = reference?.Get() as BlueprintFeature;
                if (hex == null) continue;
                bool owned = false;
                foreach (var component in hex.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                {
                    if (component == null || !(component is Prerequisite)) continue;
                    var single = AccessTools.Field(component.GetType(), "m_Feature")?.GetValue(component) as BlueprintReferenceBase;
                    if (single != null && keys.Contains(single.Guid)) { owned = true; break; }
                    var many = AccessTools.Field(component.GetType(), "m_Features")?.GetValue(component) as BlueprintReferenceBase[];
                    if (many != null && many.Any(r => r != null && keys.Contains(r.Guid))) { owned = true; break; }
                }
                if (owned) result.Add(hex);
            }
            return result.ToArray();
        }

        // Donor-derived resource shape: a stat-scaled pool of 3+ becomes R1,
        // a 1+ pool becomes R2, a fixed pool becomes R3 (with its cooldown),
        // and a passive power has no pool. No spirit name is consulted.
        static int InferResource(BlueprintFeature donor)
        {
            if (donor?.ComponentsArray == null) return 0;
            foreach (var component in donor.ComponentsArray)
            {
                if (component == null) continue;
                var field = AccessTools.Field(component.GetType(), "m_Resource")
                    ?? AccessTools.Field(component.GetType(), "m_RequiredResource");
                if (field == null) continue;
                var resourceReference = field.GetValue(component) as BlueprintReferenceBase;
                var resource = resourceReference == null ? null : ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(resourceReference.Guid);
                if (resource == null) continue;
                var amount = Get(resource, "m_MaxAmount");
                if (amount == null) continue;
                int baseValue = Convert.ToInt32(Get(amount, "BaseValue"));
                bool byStat = Convert.ToBoolean(Get(amount, "IncreasedByStat"));
                if (byStat) return baseValue <= 1 ? 2 : 1;
                return 3;
            }
            return 0;
        }

        static string NameOf(SimpleBlueprint bp)
        {
            try { var text = (bp as BlueprintUnitFact)?.Name; if (!string.IsNullOrEmpty(text)) return text; }
            catch { /* localized string not resolved yet */ }
            return bp?.name ?? "Spirit";
        }

        static bool HasGroup(SimpleBlueprint bp, string group)
        {
            object raw;
            try { raw = Get(bp, "Groups"); }
            catch (MissingFieldException)
            {
                var property = AccessTools.Property(bp.GetType(), "Groups");
                if (property == null) return false;
                raw = property.GetValue(bp, null);
            }
            if (!(raw is System.Collections.IEnumerable items)) return false;
            foreach (var item in items) if (item != null && item.ToString() == group) return true;
            return false;
        }

        // Every identity this package reads must exist before anything is staged:
        // all 90 bonded spells (including the four external roots and this mod's
        // own NewSpells), every tier and hex donor, the Extra Channel feat, the
        // parametrized spell-learning feature and the private builders' native
        // effect donors and icons. One missing identity rejects the whole package.
        internal static IEnumerable<string> RequiredProviders(Row[] rows)
        {
            yield return ExtraChannelId; yield return ParametrizedFeatureId;
            yield return FaerieFireBuffId; yield return GlitterdustId;
            yield return BlindnessBuffId; yield return ConfusedBuffId; yield return EntangleBuffId;
            yield return BlurBuffId; yield return SlowAbilityId; yield return ConstructFeatureId;
            yield return DetectMagicIconId; yield return RestorationIconId;
            yield return AnimalFactId; yield return FriendCarrierBuffId; yield return FriendDonorId;
            yield return BlurIconId; yield return FluidMagicIconId; yield return ChargedBlastIconId;
            foreach (var pool in ExtraChannelPoolIds) yield return pool;
            foreach (var row in rows)
            {
                foreach (var tier in row.Tiers) if (!string.IsNullOrEmpty(tier)) yield return tier;
                foreach (var hex in row.Hexes) if (!string.IsNullOrEmpty(hex.Donor)) yield return hex.Donor;
            }
        }

        // Known external spell roots used by the shared Shaman spirit lists.
        // Only roots actually present in the discovered spell tables are gated;
        // spirits and providers that are not installed do not block registration.
        internal sealed class ExternalSpell
        {
            internal readonly string Guid, Name, Provider;
            internal readonly SpellSchool School;
            internal ExternalSpell(string guid, string name, string provider, SpellSchool school)
            { Guid = guid; Name = name; Provider = provider; School = school; }
        }

        internal static readonly ExternalSpell[] ExternalSpells =
        {
            new ExternalSpell("80189142f7c640f39195defdc9777b27", "WallOfFireAbility", "ExpandedContent", SpellSchool.Evocation),
            new ExternalSpell("56b8f0304a704a67b3c35cbe8c774854", "HypnoticPatternAbility", "ExpandedContent", SpellSchool.Illusion),
            new ExternalSpell("99bad8fca7534acdb86866940eef62da", "IncendiaryCloudAbility", "ExpandedContent", SpellSchool.Conjuration),
            new ExternalSpell("67b29b99f13e40a48a1a95fbd909a298", "Frostbite", "CharacterOptionsPlus", SpellSchool.Transmutation)
        };

        // Derived from the discovered spirits, never from a frozen table.
        internal static string[] InternalSpellGuids(Row[] rows) => rows.SelectMany(r => r.Spells).Distinct()
            .Where(g => !ExternalSpells.Any(e => e.Guid == g)).ToArray();

        // Returns the first precise blocker, or null when the whole package may be
        // staged. Every bonded spell is read from the live Shaman spirit lists,
        // so only its published root identity needs checking; the retired
        // provider-publication contract no longer applies to this data path.
        internal static string MissingProvider(BlueprintsCache cache)
        {
            if (cache.Load(PsychicAssets.Id(SpiritSpellListOverrides0119.RootToken)) == null)
                return "Spirit Spell List Overrides 0119 not registered";
            if (cache.Load(PsychicAssets.Id(ShamanSpiritHexes0119.RootToken)) == null)
                return "Shaman Spirit Hexes 0119 not registered";
            var rows = Discover(cache);
            if (rows.Length == 0) return "no ShamanSpirit progression discovered in the native spirit selection";
            foreach (var guid in RequiredProviders(rows))
                if (cache.Load(BlueprintGuid.Parse(guid)) == null) return "provider " + guid;
            // Every spell now comes from the live Shaman spirit spell lists,
            // which 0119 validates and rewrites before this module runs. The old
            // PsychicSpellIntegration provider contract (name/school/reciprocal
            // provider spell-list publication) belonged to the retired path where
            // external roots were consumed through that publisher; it must not
            // gate a spell that is already published in a shared native list.
            // External roots only need to be real spell roots; the project-owned
            // roots additionally keep the executable-effect check.
            var spells = rows.SelectMany(r => r.Spells).Distinct().ToArray();
            foreach (var guid in spells)
            {
                var spell = cache.Load(BlueprintGuid.Parse(guid)) as BlueprintAbility;
                bool external = ExternalSpells.Any(e => e.Guid == guid);
                if (spell == null || spell.Type != AbilityType.Spell || (!external && !ExecutableRoot(spell)))
                    return "bonded spell " + guid + " is not a published spell root";
            }
            if (cache.Load(BlueprintGuid.Parse(RevelationSelectionId)) == null)
                return "provider " + RevelationSelectionId;
            return null;
        }

        // Bounded structural check for the root shapes this package uses. It
        // never walks arbitrary component graphs and never edits shared policy;
        // it only requires the one effect path a published root executes to be
        // non-empty. Project-owned bonded roots still keep this check.
        private static bool ExecutableRoot(BlueprintAbility ability)
        {
            if (ability == null) return false;
            foreach (var run in ability.GetComponents<AbilityEffectRunAction>())
                if (run.Actions?.Actions != null && run.Actions.Actions.Any(a => a != null)) return true;
            foreach (var variants in ability.GetComponents<AbilityVariants>())
            {
                var array = Get(variants, "m_Variants") as BlueprintAbilityReference[];
                if (array != null && array.Any(r => r != null)) return true;
            }
            foreach (var touch in ability.GetComponents<AbilityEffectStickyTouch>())
                if (Get(touch, "m_TouchDeliveryAbility") != null) return true;
            foreach (var shadow in ability.GetComponents<AbilityShadowSpell>())
                if (Get(shadow, "SpellList") != null) return true;
            foreach (var extra in ability.GetComponents<AdditionalAbilityEffectRunActionOnClickedTarget>())
                if (extra.Action?.Actions != null && extra.Action.Actions.Any(a => a != null)) return true;
            return false;
        }

        internal static readonly List<string> Diagnostics = new List<string>();

        internal static BlueprintFeatureSelection Build(Factory f, BlueprintCharacterClass oracle, BlueprintsCache cache)
        {
            var progressions = new List<BlueprintProgression>();

            // The guide keeps the donor's own candidate list (the wizard table
            // minus the shaman one). Only the caster class moves to Oracle, which
            // is the "swap the class, keep the Intelligence/Charisma gates" rule.
            var shaman = f.Native<BlueprintCharacterClass>(LoreArcane0119.ShamanId);
            var wizardList = f.Native<BlueprintSpellList>(LoreArcane0119.WizardListId);
            var arcane = shaman?.Spellbook?.SpellList == null || wizardList == null
                ? null
                : LoreArcane0119.Candidate(f.NextList, "ArcaneEnlightenmentList", wizardList, shaman.Spellbook.SpellList);

            var rows = Discover(cache);
            if (rows.Length == 0) throw new InvalidOperationException("0060 no shaman spirit discovered in the native pool.");
            foreach (var row in rows)
            {
                var hexes = row.Hexes.Select((hex, index) => Hex(f, oracle, row, hex, index)).ToArray();
                var hexSelection = f.Selection(row.Token + "HexSelection0060", "Hex",
                    "At 3rd level, a spirit guide gains one hex of her choice from the list of hexes available from that spirit. She uses her oracle level as her shaman level, and the hexes' save DCs use Charisma while the branches that read the shaman's Wisdom use her Wisdom.");
                Set(hexSelection, "m_AllFeatures", hexes.Select(Ref<BlueprintFeatureReference>).ToArray());
                Set(hexSelection, "m_Features", hexes.Select(Ref<BlueprintFeatureReference>).ToArray());

                var spells = row.Spells.Select(g => f.Native<BlueprintAbility>(g)).ToArray();
                if (spells.Any(s => s == null)) throw new InvalidOperationException("0060 bonded spell missing: " + row.Name);
                var spellNames = string.Join(", ", spells.Select(s => s.name));
                var learnSpells = f.Feature(row.Token + "Spells0060", "Bonded Spirit Spells",
                    "The " + row.Name + " bonded spirit grants the spirit guide the following spells, added to her oracle spellbook as temporary known spells while the bond is active: " + spellNames + ".",
                    spells.Select((s, i) => (BlueprintComponent)SpiritGuide0060.TemporarySpell(oracle, s, i + 1)).ToArray());

                var basePower = Tier(f, oracle, row, 0);
                var greaterPower = Tier(f, oracle, row, 1);

                // The 3/4/7/15 axis is the archetype's own; any power the donor
                // grants on a level outside it (Lore's repeatable Arcane
                // Enlightenment picks and its manifestation) is published on the
                // shaman's own level so the guide keeps parity with a real shaman.
                var byLevel = new SortedDictionary<int, List<BlueprintFeatureBase>>
                {
                    { 3, new List<BlueprintFeatureBase> { hexSelection } },
                    { 4, new List<BlueprintFeatureBase> { learnSpells } },
                    { 7, new List<BlueprintFeatureBase> { basePower } }
                };
                if (greaterPower != null)
                    byLevel[15] = new List<BlueprintFeatureBase> { greaterPower };
                foreach (var pair in row.Extra)
                {
                    if (!byLevel.TryGetValue(pair.Key, out var bucket))
                        byLevel[pair.Key] = bucket = new List<BlueprintFeatureBase>();
                    int index = 0;
                    foreach (var donor in pair.Value)
                    {
                        var extra = ExtraFact(f, oracle, row, donor, pair.Key, index, arcane);
                        if (extra != null) bucket.Add(extra);
                        index++;
                    }
                }
                var levelEntries = byLevel.Where(p => p.Value.Count > 0)
                    .Select(p => Entry(p.Key, p.Value.ToArray())).ToArray();
                var progression = f.Progression(row.Token + "SpiritProgression0060", row.Name + " Spirit",
                    string.Empty, levelEntries,
                    new[] { Group(levelEntries.SelectMany(e => e.Features).ToArray()) });
                Set(progression, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(oracle) } });
                // The bond reuses the shaman spirit's own name, description and
                // icon: the guide's version is the same spirit, not a rewrite.
                Present(progression, row.Progression);
                progressions.Add(progression);
                Diagnostics.Add(row.Name + ": " + hexes.Length + " hexes, 9 spells, base=" + basePower.name +
                    ", greater=" + (greaterPower == null ? "(none)" : greaterPower.name) +
                    ", extra levels=" + string.Join("/", row.Extra.Keys));

                if (row.ExtraChannel) ExtraChannel(f, oracle, basePower);
            }

            var selection = f.Selection("BondedSpirit0060", "Bonded Spirit",
                "At 3rd level, a spirit guide can form a bond with a spirit, as the shaman's wandering spirit class feature. At 3rd level she gains one hex of her choice from that spirit's hexes. At 4th level she adds the bonded spirit's spells to her oracle spells known. At 7th level she gains the spirit ability of her bonded spirit, and at 15th level its greater spirit ability. The choice is permanent for this character.");
            Set(selection, "m_AllFeatures", progressions.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(selection, "m_Features", progressions.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(selection, "m_Icon", f.Native<BlueprintUnitFact>(MysterySelectionId).Icon);
            Diagnostics.Add("published " + progressions.Count + " spirits, "
                + rows.Sum(r => r.Hexes.Length) + " hex pool entries, " + rows.Length * 9 + " bonded spells");
            return selection;
        }

        // ================= tiers (base at 7, greater at 15) =================

        private static BlueprintFeature Tier(Factory f, BlueprintCharacterClass oracle, Row row, int tier)
        {
            string token = row.Token + "Tier" + tier + "Feature0060";
            string display = row.TierNames[tier];

            var donor = row.Tiers[tier] == null ? null : f.Native<BlueprintFeature>(row.Tiers[tier]);
            // A spirit may have no plain power on this tier slot; the slot is then
            // simply absent from the progression instead of failing the package.
            if (donor == null) return null;
            var feature = (BlueprintFeature)f.Clone(donor, token, display, string.Empty);
            StripSpellLists(feature);
            // The bonded spirit grants the same power the shaman gets, so the
            // clone keeps the donor's own name, description and icon.
            Present(feature, donor);
            feature.ComponentsArray = feature.ComponentsArray
                .Where(c => c != null && c.GetType().Name != "AddAbilityResources").ToArray();

            var resource = ResourceCode(f, row, tier);
            Rebind(feature, oracle, StatType.Charisma, _ => resource);
            // D-F01: close the whole mutable subgraph (Buff -> Area -> EffectBuff
            // and AbilityVariants), not just the first AddFacts hop.
            SpiritGuideGraph0060.Privatize(f, oracle, feature, token, resource, false);
            if (resource != null)
                feature.ComponentsArray = feature.ComponentsArray.Concat(new BlueprintComponent[] { Grant(resource) }).ToArray();
            if (row.Resource[tier] == 3) AddCooldown(f, feature, token);
            return feature;
        }

        private static BlueprintAbilityResource ResourceCode(Factory f, Row row, int tier)
        {
            switch (row.Resource[tier])
            {
                case 1: return Resource(f, row.Token + "Tier" + tier + "R10060", 3, StatType.Charisma);
                case 2: return Resource(f, row.Token + "Tier" + tier + "R20060", 1, StatType.Charisma);
                case 3: return Resource(f, row.Token + "Tier" + tier + "R30060", 3, null);
                default: return null;
            }
        }

        // ================= hex pool =================

        private static BlueprintFeature Hex(Factory f, BlueprintCharacterClass oracle, Row row, HexSpec hex, int index)
        {
            string token = row.Token + "Hex" + index + "Feature0060";
            var donor = f.Native<BlueprintFeature>(hex.Donor);
            if (donor == null) throw new InvalidOperationException("0060 hex donor missing: " + hex.Name);
            var feature = (BlueprintFeature)f.Clone(donor, token, hex.Name, string.Empty);
            StripSpellLists(feature);
            // A hex is the same hex the shaman takes: reuse its own wording.
            Present(feature, donor);
            // Drop the donor's resource grant and every shaman-level prerequisite;
            // keep PrerequisiteNoFeature so a hex cannot be taken twice.
            feature.ComponentsArray = feature.ComponentsArray
                .Where(c => c != null && c.GetType().Name != "AddAbilityResources" &&
                            (!(c is Prerequisite) || c is PrerequisiteNoFeature)).ToArray();

            var resource = hex.Resource == 5 ? Resource(f, token + "R5", 0, null, true)
                : hex.Resource == 2 ? Resource(f, token + "R2", 1, StatType.Wisdom)
                : HasConsumption(feature) ? Resource(f, token + "R1", 3, StatType.Charisma)
                : null;

            Rebind(feature, oracle, StatType.Charisma, _ => resource);
            SpiritGuideGraph0060.Privatize(f, oracle, feature, token, resource, true);
            if (resource != null)
                feature.ComponentsArray = feature.ComponentsArray.Concat(new BlueprintComponent[] { Grant(resource) }).ToArray();

            if (hex.Name == "Life Link" && resource != null) AddLifeLinkRefund(f, feature, resource);
            if (hex.Name == "Mental Acuity") FixMentalAcuity(feature);
            if (hex.Name == "Entangling Curse") FixStatMinimum(feature, "Wisdom", 1);
            if (hex.Name == "Friend to Animals") FixStatMinimum(feature, "Wisdom", 0);
            if (hex.Name == "Friend to Animals") RetargetSummon(f, oracle, feature, token);
            return feature;
        }

        // Hexes and tier powers are cloned exclusively from the live Shaman
        // spirit and hex pools. Spirit-specific private builders were removed so
        // new Owlcat or mod-added spirits require no consumer-side catalogue.

        // One donor fact the shaman grants outside the guide's 3/4/7/15 axis.
        // A repeatable spell choice keeps its menu but is rebound onto the oracle
        // class and its own candidate list; anything else is an ordinary spirit
        // power and takes the usual clone-and-rebind path.
        private static BlueprintFeatureBase ExtraFact(Factory f, BlueprintCharacterClass oracle, Row row,
            BlueprintFeature donor, int level, int index, BlueprintSpellList arcane)
        {
            if (donor is BlueprintFeatureSelection selection && IsArcaneChoice(selection))
                return ArcaneSelection(f, oracle, row, selection, level, index, arcane);

            string token = row.Token + "Extra" + level + "Feature0060";
            string display = NameOf(donor);
            var feature = (BlueprintFeature)f.Clone(donor, token, display, string.Empty);
            StripSpellLists(feature);
            Present(feature, donor);
            feature.ComponentsArray = feature.ComponentsArray
                .Where(c => c != null && c.GetType().Name != "AddAbilityResources").ToArray();
            var resource = HasConsumption(feature) ? Resource(f, token + "R10060", 3, StatType.Charisma) : null;
            Rebind(feature, oracle, StatType.Charisma, _ => resource);
            SpiritGuideGraph0060.Privatize(f, oracle, feature, token, resource, false);
            if (resource != null)
                feature.ComponentsArray = feature.ComponentsArray.Concat(new BlueprintComponent[] { Grant(resource) }).ToArray();
            return feature;
        }

        // A spirit's repeatable choice is a feature selection over parametrized
        // spell options. Detected by shape, so no spirit name or GUID is stored.
        private static bool IsArcaneChoice(BlueprintFeatureSelection selection)
        {
            var options = ((BlueprintFeatureReference[])Get(selection, "m_AllFeatures")
                ?? Array.Empty<BlueprintFeatureReference>())
                .Select(r => r?.Get()).OfType<BlueprintParametrizedFeature>().ToArray();
            return options.Any(o => o.ParameterType == FeatureParameterType.LearnSpell);
        }

        // The nine circle options are cloned once per spirit and shared by every
        // selection of that spirit, so each Arcane Enlightenment step shows the
        // same 1..9 menu instead of one fixed circle. The pick is written into
        // the oracle book as a temporary known spell, matching AddSpellKnownTemporary.
        private static BlueprintFeatureSelection ArcaneSelection(Factory f, BlueprintCharacterClass oracle, Row row,
            BlueprintFeatureSelection donor, int level, int index, BlueprintSpellList arcane)
        {
            var options = ArcaneOptions(f, oracle, row, donor, arcane);
            string token = row.Token + "Arcane0060x" + level + "x" + index;
            if (f.Has(token)) return f.Find<BlueprintFeatureSelection>(token);
            var result = f.Selection(token, donor.Name, donor.Description);
            Set(result, "m_Features", options.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(result, "m_AllFeatures", options.Select(Ref<BlueprintFeatureReference>).ToArray());
            Icon(result, donor);
            return result;
        }

        // The nine circle options are cloned once per spirit and shared by every
        // selection of that spirit, so each Arcane Enlightenment step shows the
        // same 1..9 menu instead of one fixed circle. Two picks on the same level
        // still need two distinct selection blueprints: Wrath folds identical
        // references inside one LevelEntry into a single choice.
        private static List<BlueprintFeature> ArcaneOptions(Factory f, BlueprintCharacterClass oracle, Row row,
            BlueprintFeatureSelection donor, BlueprintSpellList arcane)
        {
            string token = row.Token + "ArcaneOptions0060";
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
                LoreArcane0119.Rebind(clone, oracle, arcane, StatType.Intelligence, StatType.Charisma, circle);
                clones.Add(clone);
            }
            f.Scratch[token] = clones;
            return clones;
        }

        // A spirit's repeatable choice is a feature selection over parametrized
        // spell options. Detected by shape, so no spirit name or GUID is stored.

        // The private Extra Channel clone: +2 uses of this package's Life channel
        // pool only, requiring this package's channel feature.
        private static void ExtraChannel(Factory f, BlueprintCharacterClass oracle, BlueprintFeature lifeChannel)
        {
            // Optional enhancement only. The old fixed "LifeTier0R20060" token
            // disappeared when private resources began following each discovered
            // spirit GUID, so resolve the cloned channel resource from the Life
            // base power's own AddAbilityResources component instead. If the
            // discovered Life spirit has no channel pool, skip the extra feat
            // without disabling the rest of the Spirit Guide.
            var nativeExtra = f.Native<BlueprintFeature>(ExtraChannelId);
            if (nativeExtra == null || !nativeExtra.ComponentsArray.Any(c => c != null && c.GetType().Name == "IncreaseResourceAmount")) return;
            var resourceRef = lifeChannel.ComponentsArray
                .Where(c => c != null && Has(c, "m_Resource"))
                .Select(c => Get(c, "m_Resource") as BlueprintAbilityResourceReference)
                .FirstOrDefault(r => r != null && r.Guid != BlueprintGuid.Empty);
            if (resourceRef == null) return;
            var extra = (BlueprintFeature)f.Clone(nativeExtra, "LifeExtraChannel0060",
                "Extra Channel (Spirit Guide Life Spirit)",
                "You can channel the Life spirit's positive energy two additional times per day. Requires Channel Positive Energy from this archetype.");
            var increase = extra.ComponentsArray.FirstOrDefault(c => c != null && c.GetType().Name == "IncreaseResourceAmount");
            if (increase == null) return;
            Set(increase, "m_Resource", resourceRef);
            Set(increase, "Value", 2);
            extra.ComponentsArray = extra.ComponentsArray.Where(c => !(c is Prerequisite))
                .Concat(new BlueprintComponent[] { Migration01426.Require(lifeChannel) }).ToArray();
        }

        // Publish the private Extra Channel into every native feat pool that
        // already offers the native Extra Channel feat (0117 gate).
        internal static void ConnectExtraChannel(BlueprintsCache cache, Factory f)
        {
            if (!f.Has("LifeExtraChannel0060")) return;
            var extra = f.Find<BlueprintFeature>("LifeExtraChannel0060");
            var nativeExtraGuid = BlueprintGuid.Parse(ExtraChannelId);
            foreach (var guid in ExtraChannelPoolIds)
            {
                var pool = cache.Load(BlueprintGuid.Parse(guid)) as BlueprintFeatureSelection;
                if (pool == null) continue;
                var all = (BlueprintFeatureReference[])Get(pool, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
                if (!all.Any(r => r != null && r.Guid == nativeExtraGuid)) continue;
                foreach (string field in new[] { "m_Features", "m_AllFeatures" })
                {
                    var old = (BlueprintFeatureReference[])Get(pool, field) ?? Array.Empty<BlueprintFeatureReference>();
                    if (!old.Any(r => r != null && r.Guid == extra.AssetGuid))
                        Set(pool, field, old.Concat(new[] { Ref<BlueprintFeatureReference>(extra) }).ToArray());
                }
            }
        }

        internal static object[][] SnapshotPools(BlueprintsCache cache) =>
            ExtraChannelPoolIds.Select(g => cache.Load(BlueprintGuid.Parse(g)))
                .Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();

        internal static void RestorePools(BlueprintsCache cache, object[][] snapshot)
        {
            for (int i = 0; i < ExtraChannelPoolIds.Length; i++)
            {
                var pool = cache.Load(BlueprintGuid.Parse(ExtraChannelPoolIds[i]));
                Set(pool, "m_Features", snapshot[i][0]);
                Set(pool, "m_AllFeatures", snapshot[i][1]);
            }
        }

        // ================= clone rebinding =================
        // The private graph is closed by SpiritGuideGraph0060 (D-F01); these two
        // helpers hold the stat swap used both there and on the cloned feature.

        // The original spirit guide "switches Wisdom for Charisma and vice versa":
        // donor primary Wisdom becomes Charisma on every StatBonus rank in the
        // private graph, and for hexes donor secondary Charisma becomes Wisdom
        // (tiers keep Charisma on both channels per the frozen contract).
        internal static void SwapStats(BlueprintScriptableObject bp, bool hexStats)
        {
            foreach (var rank in (bp.ComponentsArray ?? Array.Empty<BlueprintComponent>()).OfType<ContextRankConfig>())
                SwapRankStat(rank, hexStats);
        }

        private static void SwapRankStat(ContextRankConfig rank, bool hexStats)
        {
            if ((ContextRankBaseValueType)Get(rank, "m_BaseValueType") != ContextRankBaseValueType.StatBonus) return;
            var name = Get(rank, "m_Stat")?.ToString();
            if (name == "Wisdom") Set(rank, "m_Stat", StatType.Charisma);
            else if (hexStats && name == "Charisma") Set(rank, "m_Stat", StatType.Wisdom);
        }

        // Rebind a private clone from Shaman/Wisdom donors onto the real Oracle
        // class and Charisma (primary). Returns a human-readable audit list.
        internal static List<string> Rebind(object root, BlueprintCharacterClass oracle, StatType dcStat,
            Func<BlueprintReferenceBase, BlueprintAbilityResource> mapResource)
        {
            var audit = new List<string>();
            var seen = new HashSet<object>(PsychicAssets.IdentityComparer);
            var start = root is BlueprintScriptableObject bp ? (object)bp.ComponentsArray : root;
            Walk(start, seen, o =>
            {
                var type = o.GetType();
                var name = type.Name;
                if (name == "ContextRankConfig" && Has(o, "m_Class"))
                {
                    Set(o, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(oracle) });
                    audit.Add("rank->Oracle");
                }
                if (name == "ContextCalculateAbilityParamsBasedOnClass")
                {
                    if (Has(o, "m_CharacterClass")) Set(o, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(oracle));
                    if (Has(o, "StatType")) Set(o, "StatType", dcStat);
                    audit.Add("params->Oracle/" + dcStat);
                }
                if (name == "AddFeatureOnClassLevel" && Has(o, "m_Class"))
                {
                    var field = AccessTools.Field(type, "m_Class");
                    object value = field.FieldType.IsArray
                        ? (object)new[] { Ref<BlueprintCharacterClassReference>(oracle) }
                        : Ref<BlueprintCharacterClassReference>(oracle);
                    field.SetValue(o, value);
                    audit.Add("classLevel->Oracle");
                }
                if (name == "SpontaneousSpellConversion" && Has(o, "m_CharacterClass"))
                {
                    Set(o, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(oracle));
                    audit.Add("conversion->Oracle");
                }
                if ((name.Contains("ResourceLogic") && Has(o, "m_RequiredResource")) ||
                    (name == "AddAbilityResources" && Has(o, "m_Resource")))
                {
                    string field = name == "AddAbilityResources" ? "m_Resource" : "m_RequiredResource";
                    var donor = Get(o, field) as BlueprintReferenceBase;
                    if (donor != null)
                    {
                        var replacement = mapResource(donor);
                        if (replacement != null)
                        {
                            Set(o, field, Ref<BlueprintAbilityResourceReference>(replacement));
                            audit.Add("resource->" + replacement.name);
                        }
                    }
                }
            });
            return audit;
        }

        internal static void Walk(object value, HashSet<object> seen, Action<object> visit)
        {
            if (value == null || value is string || value is Type || value is Delegate ||
                value.GetType().IsValueType || value is SimpleBlueprint ||
                value is BlueprintReferenceBase || value is UnityEngine.Object) return;
            if (!seen.Add(value)) return;
            visit(value);
            if (value is System.Collections.IEnumerable items)
            {
                foreach (var item in items) Walk(item, seen, visit);
                return;
            }
            for (var t = value.GetType(); t != null && t != typeof(object) &&
                 t != typeof(BlueprintComponent) && t != typeof(Element); t = t.BaseType)
                foreach (var fieldInfo in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!fieldInfo.IsStatic) Walk(fieldInfo.GetValue(value), seen, visit);
        }

        internal static int StripSpellLists(BlueprintScriptableObject bp)
        {
            var kept = (bp.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Where(c => !(c != null && c.GetType().Name.Contains("AddSpecialSpellList"))).ToArray();
            int removed = (bp.ComponentsArray?.Length ?? 0) - kept.Length;
            bp.ComponentsArray = kept;
            return removed;
        }

        private static bool HasConsumption(BlueprintScriptableObject bp)
        {
            bool found = false;
            Walk(bp.ComponentsArray, new HashSet<object>(PsychicAssets.IdentityComparer), o =>
            {
                var name = o.GetType().Name;
                if (name.Contains("ResourceLogic") && Has(o, "m_RequiredResource")) found = true;
            });
            return found;
        }

        private static BlueprintAbilityResource Resource(Factory f, string token, int baseValue, StatType? bonusStat, bool perLevel = false)
        {
            var r = f.Add(new BlueprintAbilityResource(), token, "", "");
            var amount = Get(r, "m_MaxAmount");
            Set(amount, "BaseValue", baseValue);
            Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            if (bonusStat.HasValue)
            {
                Set(amount, "IncreasedByStat", true);
                Set(amount, "ResourceBonusStat", bonusStat.Value);
            }
            if (perLevel)
            {
                Set(amount, "IncreasedByLevel", true);
                Set(amount, "LevelIncrease", 1);
            }
            Set(r, "m_MaxAmount", amount);
            if (Has(r, "m_Min")) Set(r, "m_Min", 0);
            return r;
        }

        // R3: fixed three uses plus a private 1d4-round cooldown. The cooldown
        // fact is checked by a native caster restriction and applied by a real
        // effect action on every successfully executed leaf ability.
        private static void AddCooldown(Factory f, BlueprintFeature feature, string token)
        {
            var buff = f.Buff(token + "Cooldown", "Spirit Guide cooldown",
                "This spirit power cannot be used again while this cooldown lasts.");
            var restriction = new AbilityCasterHasNoFacts();
            Set(restriction, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(buff) });
            var apply = new ContextActionApplyBuff { ToCaster = true, IsFromSpell = false,
                DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.D4, DiceCountValue = 1, BonusValue = 0 } };
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

        // A 24-hour per-target lock: a native inverted AbilityTargetHasFact gate
        // plus a non-dispellable 1-day mark, appended to the failure branch when
        // the original hex only locks after a failed save.
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
                PsychicAssets.SetEnum(rank, "m_Progression", "OnePlusDivStep");
                Set(rank, "m_StepLevel", 5);
                Set(rank, "m_StartLevel", 0);
                var field = AccessTools.Field(rank.GetType(), "m_CustomProgression");
                if (field != null && field.FieldType.IsArray)
                    field.SetValue(rank, Array.CreateInstance(field.FieldType.GetElementType(), 0));
            }
        }

        private static void FixStatMinimum(BlueprintFeature feature, string stat, int minimum)
        {
            foreach (var ability in GrantedAbilities(feature))
                foreach (var rank in ability.GetComponents<ContextRankConfig>())
                {
                    if (Get(rank, "m_Stat")?.ToString() != stat) continue;
                    Set(rank, "m_UseMin", true);
                    Set(rank, "m_Min", minimum);
                }
            foreach (var granted in GrantedFacts(feature))
                foreach (var rank in granted.GetComponents<ContextRankConfig>())
                {
                    if (Get(rank, "m_Stat")?.ToString() != stat) continue;
                    Set(rank, "m_UseMin", true);
                    Set(rank, "m_Min", minimum);
                }
            foreach (var rank in feature.GetComponents<ContextRankConfig>())
            {
                if (Get(rank, "m_Stat")?.ToString() != stat) continue;
                Set(rank, "m_UseMin", true);
                Set(rank, "m_Min", minimum);
            }
        }

        private static IEnumerable<BlueprintUnitFact> GrantedFacts(BlueprintFeature feature)
        {
            foreach (var component in feature.ComponentsArray)
            {
                if (component == null || component.GetType().Name != "AddFacts" || !Has(component, "m_Facts")) continue;
                var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
                foreach (var reference in refs)
                    if (reference?.Get() is BlueprintUnitFact fact) yield return fact;
            }
        }

        // Friend to Animals: the donor's spontaneous summon conversion must use
        // the Oracle class before the Oracle spellbook can drive it.
        private static void RetargetSummon(Factory f, BlueprintCharacterClass oracle, BlueprintFeature feature, string token)
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
                        Set(conversion, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(oracle));
                    rewritten.Add(Ref<BlueprintUnitFactReference>(clone));
                    index++;
                }
                Set(component, "m_Facts", rewritten.ToArray());
            }
        }

        // Ending a Life Link refunds exactly one R5 point to the caster. The
        // donor's life-link buff is matched by name: the Kingmaker-era fixed GUID
        // does not exist in the Wrath library, so identity alone would miss it.
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
                var buff = (Get(apply, "m_Buff") as BlueprintBuffReference)?.Get();
                if (buff == null) continue;
                var buffName = buff.name ?? "";
                if (buffName.IndexOf("LifeLink", StringComparison.OrdinalIgnoreCase) < 0 &&
                    buffName.IndexOf("Life Link", StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (refund == null)
                {
                    refund = (BlueprintBuff)f.Clone(buff, "LifeLinkRefundBuff0060", "Life Link",
                        "When this link ends, one use of the spirit guide's Life Link pool is restored.");
                    var restore = new ContextRestoreResource();
                    Set(restore, "Resource", Ref<BlueprintAbilityResourceReference>(resource));
                    Set(restore, "Value", new ContextValue { Value = 1 });
                    var onCaster = WrapOnContextCaster(restore);
                    var ctx = new AddFactContextActions { Activated = PsychicAssets.Actions(), Deactivated = PsychicAssets.Actions(onCaster), NewRound = PsychicAssets.Actions() };
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

        // Rank that reads the real Oracle class level with the given progression.
        private static ContextRankConfig OracleRank(BlueprintCharacterClass oracle, string progression = "AsIs", int minimum = 0)
        {
            var rank = new ContextRankConfig();
            PsychicAssets.SetEnum(rank, "m_BaseValueType", "ClassLevel");
            PsychicAssets.SetEnum(rank, "m_Progression", progression);
            Set(rank, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(oracle) });
            if (minimum > 0) { Set(rank, "m_UseMin", true); Set(rank, "m_Min", minimum); }
            if (progression == "Div2") { Set(rank, "m_UseMin", true); Set(rank, "m_Min", 1); }
            return rank;
        }

        private static ContextCalculateAbilityParamsBasedOnClass OracleParams(BlueprintCharacterClass oracle)
        {
            var p = new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Charisma };
            Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(oracle));
            return p;
        }

        private static ContextValue RankValue(AbilityRankType type = AbilityRankType.Default)
            => new ContextValue { ValueType = ContextValueType.Rank, ValueRank = type };

        private static ContextActionApplyBuff RankRounds(BlueprintBuff buff)
        {
            var apply = PsychicAssets.Apply(buff, 0);
            apply.DurationValue.BonusValue = RankValue();
            return apply;
        }

        private static KeyValuePair<string, object> F(string key, object value) => new KeyValuePair<string, object>(key, value);

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
                var value = pair.Value;
                if (target.IsEnum && value is string s) value = Enum.Parse(target, s);
                else if (value is int number && target != typeof(int) && target.Name == "ContextValue")
                    value = new ContextValue { Value = number };
                if (field != null) field.SetValue(component, value);
                else if (property != null && property.GetSetMethod(true) != null) property.SetValue(component, value, null);
            }
            return component;
        }
    }

    // Q10: a new Heaven's Leap selection clears only this caster's previous mark,
    // so another spirit guide's marked ally is never moved or cleared.
    [TypeId("0060b1a2c3d4e5f60718293a4b5c0d01")]
    internal sealed class HeavensLeapMark0060 : UnitFactComponentDelegate
    {
        protected override void OnActivate()
        {
            var caster = Fact?.MaybeContext?.MaybeCaster;
            if (caster == null) return;
            foreach (var unit in HeavensLeap0060.Candidates(caster))
            {
                if (unit?.Buffs == null || ReferenceEquals(unit, Owner)) continue;
                foreach (var buff in unit.Buffs.Enumerable.Where(b => b.Blueprint == Fact.Blueprint).ToArray())
                    if (ReferenceEquals(buff.Context?.MaybeCaster, caster)) buff.Remove();
            }
        }
    }

    [TypeId("0060b1a2c3d4e5f60718293a4b5c0d02")]
    internal sealed class HeavensLeap0060 : AbilityCustomDimensionDoor
    {
        internal BlueprintBuff Mark;
        internal BlueprintBuff DayLock;
        public override System.Collections.Generic.IEnumerator<Kingmaker.UnitLogic.Abilities.Components.Base.AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target)
        {
            var caster = context.MaybeCaster;
            if (caster != null && Mark != null)
            {
                // Q10: move only the ally this caster explicitly selected. No
                // selection means no move; never silently fall back to the caster.
                var ally = Marked(caster);
                if (ally == null) yield break;
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
                    if (ReferenceEquals(from, caster)) return unit;
                }
            }
            return null;
        }
        internal static IEnumerable<UnitEntityData> Candidates(UnitEntityData caster)
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
}
