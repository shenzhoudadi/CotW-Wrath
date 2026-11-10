using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Newtonsoft.Json;

namespace CallOfTheWild.Wrath.Prototype
{
    // COTW-0060 Q02 / contract section 3. The ONLY environment-independent
    // Harmony hook in this package: a paired, narrow AddClassSkill.OnTurnOn /
    // OnTurnOff adaptation that suppresses just the extra class-skill
    // contribution of the real Oracle Mystery source while a Spirit Guide
    // archetype is present. The Mystery fact, its spells and qualifications are
    // kept; background/other-class/other-fact contributions and normal Oracles
    // always run the native method. Suppression state is a serialized per-unit
    // ledger keyed by source-fact identity + stable component name, recoverable
    // through the native lifecycle (never an unrecoverable static bool).
    internal static class SpiritGuideSkills0060
    {
        internal const string ClassSkillsToken = "SpiritGuideClassSkills0060";
        private static readonly BlueprintGuid SpiritGuideId = SpiritGuideBridge0060.Id(SpiritGuideBridge0060.RootToken);
        private static readonly BlueprintGuid OracleId = BlueprintGuid.Parse(SpiritGuideBridge0060.OracleClassId);

        // Read-only whitelist: the actual members of OracleMysterySelection
        // (5531b975dcdf0e24c98f1ff7e017e741), audited offline against the game's
        // own blueprint archive. Each carries its AddClassSkill components
        // directly; skills are distinct within a member so (guid,skill,name) is
        // a stable unique source key. See A-mystery-skill-whitelist.json.
        internal static readonly HashSet<BlueprintGuid> MysteryMembers = new HashSet<BlueprintGuid>
        {
            BlueprintGuid.Parse("9936554e9372dab4a874c1dd165fb6f8"), // Ancestors
            BlueprintGuid.Parse("6b8e676943cb91648b21b7ca72fbb833"), // Battle
            BlueprintGuid.Parse("067b175d3df0d1a408efd7eee2b36b9b"), // Bones
            BlueprintGuid.Parse("3b68909df737cd4458509d7f3a9c3706"), // Flame
            BlueprintGuid.Parse("a5458a1c316d1f24e8d9770f4fc179fa"), // Life
            BlueprintGuid.Parse("69579bfe28e15b942af0722021d8725c"), // Nature
            BlueprintGuid.Parse("7bb4bb3e7fd26f34e8ca035a27e03e85"), // Stone
            BlueprintGuid.Parse("e2f8bd7c45dfb954c8c42b168505c783"), // Waves
            BlueprintGuid.Parse("7c1fdd831af747b47bb2cce0051f309b"), // Wind
        };

        internal static bool IsSpiritGuide(UnitEntityData unit) => unit?.Progression?.Classes != null &&
            unit.Progression.Classes.Any(c => c.Level > 0 && c.CharacterClass != null &&
                c.CharacterClass.AssetGuid == OracleId && c.Archetypes.Any(a => a != null && a.AssetGuid == SpiritGuideId));

        internal static bool InScope(UnitEntityData unit, EntityFact fact) =>
            fact?.Blueprint != null && IsSpiritGuide(unit) && MysteryMembers.Contains(fact.Blueprint.AssetGuid);

        // Stable source key: source-fact blueprint identity + skill + the native
        // runtime component name (SourceBlueprintComponentName). The name is
        // restored by the native fact lifecycle on load, so the key survives
        // save/load and retrain without comparing memory references.
        internal static string Key(EntityFact fact, StatType skill, string componentName)
            => fact.Blueprint.AssetGuid + "|" + skill + "|" + (componentName ?? "");

        // The delegate base chain shadows the Fact property; a plain
        // GetProperty/AccessTools.Property lookup is ambiguous. Resolve the
        // most-derived declaration once, walking with DeclaredOnly.
        private static readonly System.Reflection.PropertyInfo FactProperty = ResolveFactProperty();
        private static System.Reflection.PropertyInfo ResolveFactProperty()
        {
            for (var t = typeof(AddClassSkill); t != null; t = t.BaseType)
            {
                var p = t.GetProperty("Fact", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
                if (p != null) return p;
            }
            return null;
        }

        private static EntityFact FactOf(AddClassSkill component)
            => FactProperty?.GetValue(component) as EntityFact;

        private static string NameOf(EntityFact fact, AddClassSkill component)
            => fact?.Components?.FirstOrDefault(c => ReferenceEquals(c.SourceBlueprintComponent, component))?.SourceBlueprintComponentName;

        private static ModifiableValueSkill SkillStat(UnitEntityData unit, StatType skill)
            => unit?.Stats?.GetStat<ModifiableValueSkill>(skill);

        // ---- Serialized, recoverable per-unit suppression ledger ----
        public sealed class Ledger : UnitPart
        {
            [JsonProperty]
            private readonly List<string> m_Suppressed = new List<string>();

            public bool IsSuppressed(string key) => m_Suppressed.Contains(key);
            public void MarkSuppressed(string key) { if (!m_Suppressed.Contains(key)) m_Suppressed.Add(key); }
            public void Clear(string key) => m_Suppressed.Remove(key);
            public IReadOnlyList<string> Keys => m_Suppressed;
        }

        // ---- Paired native hook (the single allowed Harmony patch) ----
        [HarmonyPatch(typeof(AddClassSkill), "OnTurnOn")]
        internal static class TurnOnPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(AddClassSkill __instance)
            {
                var fact = FactOf(__instance);
                var unit = fact?.Owner as UnitEntityData;
                if (unit == null || !InScope(unit, fact)) return true; // native: normal Oracle / background / other fact
                unit.Ensure<Ledger>().MarkSuppressed(Key(fact, __instance.Skill, NameOf(fact, __instance)));
                return false; // skip only this source's ClassSkillRetain; base.OnTurnOn is empty
            }
        }

        [HarmonyPatch(typeof(AddClassSkill), "OnTurnOff")]
        internal static class TurnOffPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(AddClassSkill __instance)
            {
                var fact = FactOf(__instance);
                var unit = fact?.Owner as UnitEntityData;
                if (unit == null) return true;
                var ledger = unit.Get<Ledger>();
                string key = Key(fact, __instance.Skill, NameOf(fact, __instance));
                if (ledger != null && ledger.IsSuppressed(key))
                {
                    ledger.Clear(key);
                    return false; // the paired Retain was skipped; do not Release (no underflow)
                }
                return true; // a native Retain ran earlier; release it exactly once
            }
        }

        // ---- Reconciliation riding our own Class Skills feature lifecycle ----
        // Handles "archetype added after Mystery already active" (offset this
        // source once) and "archetype removed while Mystery still lives"
        // (restore one Retain). Idempotent on re-entry / recalc / post-load.
        [TypeId("72f20e6402591cd323721cfcc67963a6")]
        public sealed class Adapter0060 : UnitFactComponentDelegate
        {
            protected override void OnTurnOn()
            {
                base.OnTurnOn();
                var unit = base.Owner;
                // This adapter only exists on the Spirit Guide Class Skills
                // feature, so its activation is itself the authority; do not
                // re-gate on progression order (archetype may register late).
                if (unit == null) return;
                var ledger = unit.Ensure<Ledger>();
                foreach (var fact in unit.Facts.List.Where(f => f.IsActive && f.IsTurnedOn && f.Blueprint != null && MysteryMembers.Contains(f.Blueprint.AssetGuid)).ToArray())
                    foreach (var rc in fact.Components.Where(c => c.SourceBlueprintComponent is AddClassSkill && c.IsTurnedOn).ToArray())
                    {
                        var skill = ((AddClassSkill)rc.SourceBlueprintComponent).Skill;
                        string key = Key(fact, skill, rc.SourceBlueprintComponentName);
                        if (ledger.IsSuppressed(key)) continue; // already offset; never double-release
                        SkillStat(unit, skill)?.ClassSkillRelease();
                        ledger.MarkSuppressed(key);
                    }
            }

            protected override void OnTurnOff()
            {
                base.OnTurnOff();
                var unit = base.Owner;
                if (unit == null) return;
                var ledger = unit.Get<Ledger>();
                if (ledger == null) return;
                // Restore exactly one Retain for each suppressed source that is
                // still alive; drop phantom keys whose source fact is gone.
                var living = new HashSet<string>();
                foreach (var fact in unit.Facts.List.Where(f => f.IsActive && f.IsTurnedOn && f.Blueprint != null && MysteryMembers.Contains(f.Blueprint.AssetGuid)).ToArray())
                    foreach (var rc in fact.Components.Where(c => c.SourceBlueprintComponent is AddClassSkill && c.IsTurnedOn).ToArray())
                    {
                        var skill = ((AddClassSkill)rc.SourceBlueprintComponent).Skill;
                        string key = Key(fact, skill, rc.SourceBlueprintComponentName);
                        if (!ledger.IsSuppressed(key)) continue;
                        SkillStat(unit, skill)?.ClassSkillRetain();
                        ledger.Clear(key);
                        living.Add(key);
                    }
                foreach (var key in ledger.Keys.ToArray())
                    if (!living.Contains(key)) ledger.Clear(key); // source disappeared: no phantom restore
            }
        }
    }
}