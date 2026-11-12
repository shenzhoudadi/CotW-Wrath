using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.SpiritGuideBridge0060;

namespace CallOfTheWild.Wrath.Prototype
{
    // COTW-0060 Spirit Guide, the complete Oracle archetype. The dangerous-path
    // ownership slice (C03 skill pairing, C06 level/stat isolation, C10 Wisdom
    // branches, C13 source-safe temporary knowledge) and the live Shaman-spirit
    // catalogue (F-0060-SPIRITS) live behind one atomic registration. The identity
    // gate validates every discovered tier, hex and bonded spell donor together
    // with the parametrized spell-learning feature and Extra Channel feat, and
    // RegisterOptional retries the late external providers. Registration is a
    // real 0060-owned transaction: private identities, native parent connection,
    // pool snapshots, rollback, never a partial publish.
    internal static class SpiritGuide0060
    {
        internal const string ModuleName = "Spirit Guide";

        private sealed class State
        {
            internal BlueprintGuid Root;
            internal BlueprintScriptableObject[] Assets;
            internal string[] Definitions;
            internal BlueprintCharacterClass Oracle;
        }

        private static readonly ConditionalWeakTable<BlueprintsCache, State> Published =
            new ConditionalWeakTable<BlueprintsCache, State>();

        internal static bool Register(BlueprintsCache cache)
        {
            if (Published.TryGetValue(cache, out var state))
            {
                if (!ReferenceEquals(cache.Load(state.Root), state.Assets[0]))
                    throw new InvalidOperationException("0060 published archetype identity changed.");
                for (int i = 0; i < state.Assets.Length; i++)
                    if (!ReferenceEquals(cache.Load(state.Assets[i].AssetGuid), state.Assets[i]) ||
                        (BlueprintRegistration.StrictPublishedGraphChecks && ArchetypeBatch01445.Definition(state.Assets[i]) != state.Definitions[i]))
                        throw new InvalidOperationException("0060 published private blueprint changed: " + state.Assets[i].name);
                var existing = (BlueprintArchetypeReference[])Get(state.Oracle, "m_Archetypes") ?? Array.Empty<BlueprintArchetypeReference>();
                if (existing.Count(r => r != null && r.Guid == state.Root) != 1)
                    throw new InvalidOperationException("0060 oracle archetype link changed.");
                return true;
            }

            var oracle = cache.Load(BlueprintGuid.Parse(OracleClassId)) as BlueprintCharacterClass;
            if (oracle == null || oracle.Progression == null) { Log("reject: 0060 oracle progression absent"); return false; }

            var missing = SpiritGuideSpirits0060.MissingProvider(cache);
            if (missing != null) { Log("reject: 0060 " + missing); return false; }

            var f = new Factory(cache);
            var rootRef = (BlueprintArchetypeReference[])Get(oracle, "m_Archetypes");
            var uiGroups = oracle.Progression.UIGroups;
            var poolSnapshot = SpiritGuideSpirits0060.SnapshotPools(cache);

            BlueprintArchetype root = null;
            try
            {
                root = Build(f, oracle, cache);
                var assets = new[] { (BlueprintScriptableObject)root }.Concat(
                    f.Assets.Values.Where(a => !ReferenceEquals(a, root))).ToArray();
                foreach (var a in assets)
                    if (cache.Load(a.AssetGuid) != null)
                        throw new InvalidOperationException("0060 private GUID occupied: " + a.name);

                var undo = new Action(() =>
                {
                    Set(oracle, "m_Archetypes", rootRef);
                    oracle.Progression.UIGroups = uiGroups;
                    SpiritGuideSpirits0060.RestorePools(cache, poolSnapshot);
                });

                try
                {
                    foreach (var a in assets) Prepare(a);
                    f.Enable();
                    foreach (var a in assets) cache.AddCachedBlueprint(a.AssetGuid, a);
                    foreach (var a in assets)
                        if (!ReferenceEquals(cache.Load(a.AssetGuid), a))
                            throw new InvalidOperationException("0060 private blueprint not published: " + a.name);

                    var refs = rootRef ?? Array.Empty<BlueprintArchetypeReference>();
                    if (!refs.Any(r => r != null && r.Guid == root.AssetGuid))
                        Set(oracle, "m_Archetypes", refs.Concat(new[] { Ref<BlueprintArchetypeReference>(root) }).ToArray());
                    var classSkills = f.Find<BlueprintFeature>(SpiritGuideSkills0060.ClassSkillsToken);
                    var spirits = f.Find<BlueprintFeatureSelection>("BondedSpirit0060");
                    oracle.Progression.UIGroups = (uiGroups ?? Array.Empty<UIGroup>())
                        .Concat(new[] { Group(classSkills, spirits) }).ToArray();
                    SpiritGuideSpirits0060.ConnectExtraChannel(cache, f);

                    Published.Add(cache, new State
                    {
                        Root = root.AssetGuid,
                        Assets = assets,
                        Definitions = assets.Select(ArchetypeBatch01445.Definition).ToArray(),
                        Oracle = oracle
                    });
                    foreach (var line in SpiritGuideSpirits0060.Diagnostics) Log("0060 diagnostic " + line);
                    Log("0060 published " + root.AssetGuid + " with " + assets.Length + " private blueprints");
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
                Log("reject: 0060 " + error.GetType().Name + ": " + error.Message + "\n" + error.StackTrace);
                return false;
            }
        }

        private static BlueprintArchetype Build(Factory f, BlueprintCharacterClass oracle, BlueprintsCache cache)
        {
            // C03: the Class Skills adaptation feature carries the only reconciliation
            // component. The paired AddClassSkill hook is installed by production.
            var classSkills = f.Feature(SpiritGuideSkills0060.ClassSkillsToken, "Spirit Guide Class Skills",
                "A spirit guide's mystery no longer grants its extra class skills; the oracle's base class skills and any background contribution remain.",
                new SpiritGuideSkills0060.Adapter0060());

            // F-0060-SPIRITS: discover all currently published Shaman spirits,
            // clone their owned hexes, spirit spells and level-1/8 powers, then
            // adapt them to the Oracle 3/4/7/15 axis. Life keeps a private Extra
            // Channel feat tied to its cloned channel resource.
            var spirits = SpiritGuideSpirits0060.Build(f, oracle, cache);

            // C04: the archetype replaces the revelations at 3/7/15 with the spirit
            // bond; the level-1/11/19 revelations and the level-1 mystery stay.
            // The original Kingmaker-only mystery-skills entry is NOT ported: that
            // blueprint does not exist in Wrath and the C03 adapter already owns
            // the skill suppression.
            var revelation = f.Native<BlueprintFeatureSelection>(RevelationSelectionId);
            if (revelation == null) throw new InvalidOperationException("0060 oracle revelation selection missing.");

            var root = f.Add(new BlueprintArchetype
            {
                AddFeatures = new[] { Entry(1, classSkills), Entry(3, spirits) },
                RemoveFeatures = new[] { Entry(3, revelation), Entry(7, revelation), Entry(15, revelation) },
                ComponentsArray = Array.Empty<BlueprintComponent>()
            }, RootToken, "Spirit Guide",
                "Spirit guides are oracles who bond with a single spirit, trading selected revelations for spirit abilities and hexes. Charisma drives the spirit pools and save DCs; the hex branches that read the shaman's Wisdom read the spirit guide's Wisdom.");
            root.LocalizedName = Text(RootToken, "name", "Spirit Guide");
            root.LocalizedDescription = Text(RootToken, "description",
                "Through her exploration of the universe's mysteries, a spirit guide opens connections to the spirit world and forms bonds with the entities that inhabit it. At 3rd level she bonds with one spirit and gains one of its hexes; at 4th level the spirit's spells are added to her oracle spells known; at 7th and 15th level she gains the spirit's base and greater powers. The revelations of 3rd, 7th and 15th level are replaced, and the mystery's bonus class skills are suppressed while the oracle's base skills and background remain.");
            Set(root, "m_ParentClass", oracle);
            foreach (var entry in root.AddFeatures.Concat(root.RemoveFeatures)) entry.Features.ToArray();
            return root;
        }

        // Q05: a native temporary-knowledge grant bound to the real Oracle class.
        // OnlySpontaneous matches the normal Oracle spontaneous spellbook.
        internal static AddSpellKnownTemporary TemporarySpell(BlueprintCharacterClass oracle, BlueprintAbility spell, int level)
        {
            var c = new AddSpellKnownTemporary { Level = level, OnlySpontaneous = true };
            Set(c, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(oracle));
            Set(c, "m_Spell", Ref<BlueprintAbilityReference>(spell));
            return c;
        }

        // The hex_secondary_stat branch: StatBonus Wisdom, AsIs, clamped to min.
        internal static ContextRankConfig WisdomRounds(int min = 1)
        {
            var rank = new ContextRankConfig();
            Set(rank, "m_BaseValueType", ContextRankBaseValueType.StatBonus);
            Set(rank, "m_Stat", StatType.Wisdom);
            Set(rank, "m_UseMin", true);
            Set(rank, "m_Min", min);
            PsychicAssets.SetEnum(rank, "m_Progression", "AsIs");
            return rank;
        }
    }

}
