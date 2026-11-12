// Adapted from Holic75/KingmakerRebalance (MIT), ShowRerolls.cs and Archetypes/IroranPaladin.cs.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Components;

namespace CallOfTheWild.Wrath.Prototype
{
    // COTW-0109 Q06. Native dice wiring for the two Iroran auras, limited to real
    // active aura sources on the roller and actual RuleRollD20 events. Two entry
    // points only: the private parameterless Roll() reached from OnTrigger after
    // AddReroll aggregation (including Relentless/Pageantry OnTrigger additions),
    // and the public late Reroll(EntityFact,bool). The private parameterless
    // Reroll() is never patched, so per-die draws inside Roll() stay native.
    internal static class IroranRerolls0109
    {
        internal enum AuraKind { Excellence, Perfection }

        // Transient per-unit registry, rebuilt by the aura buff component
        // lifecycle after load; nothing here is serialized into a save.
        public sealed class AuraPart : UnitPart
        {
            private sealed class Entry
            {
                internal Buff Buff;
                internal UnitEntityData Emitter;
            }
            private readonly Dictionary<AuraKind, List<Entry>> m_Active = new Dictionary<AuraKind, List<Entry>>();

            internal void Add(AuraKind kind, Buff buff, UnitEntityData emitter)
            {
                if (!m_Active.TryGetValue(kind, out var list)) m_Active[kind] = list = new List<Entry>();
                if (list.Any(e => e.Buff == buff)) return;
                list.Add(new Entry { Buff = buff, Emitter = emitter });
            }
            internal void Remove(AuraKind kind, Buff buff)
            {
                if (m_Active.TryGetValue(kind, out var list)) list.RemoveAll(e => e.Buff == buff);
            }
            // self=true matches auras emitted by the roller; false matches auras
            // emitted by someone else. The emitter must be conscious and the
            // buff enabled (turned on and not suppressed) at roll time.
            internal bool Has(AuraKind kind, UnitEntityData roller, bool self)
            {
                if (!m_Active.TryGetValue(kind, out var list)) return false;
                foreach (var entry in list)
                {
                    if (entry.Buff == null || !entry.Buff.IsEnabled) continue;
                    var emitter = entry.Emitter;
                    if (emitter == null || !emitter.State.IsConscious) continue;
                    if ((emitter == roller) == self) return true;
                }
                return false;
            }
            internal bool HasAny(AuraKind kind, UnitEntityData roller) => Has(kind, roller, true) || Has(kind, roller, false);
        }

        [TypeId("9de4db35ae5e62269d169b8ec310cb77")]
        public sealed class AuraComponent : UnitBuffComponentDelegate
        {
            public AuraKind Kind;
            protected override void OnActivate() => base.Owner.Ensure<AuraPart>().Add(Kind, base.Buff, base.Buff.Context?.MaybeCaster);
            protected override void OnDeactivate() => base.Owner.Get<AuraPart>()?.Remove(Kind, base.Buff);
        }

        private static readonly FieldInfo ResultField = AccessTools.Field(typeof(RuleRollDice), "m_Result");
        private static readonly FieldInfo RerollAmountField = AccessTools.Field(typeof(RuleRollDice), "m_RerollAmount");
        private static readonly FieldInfo PreRolledField = AccessTools.Field(typeof(RuleRollDice), "m_PreRolledResult");
        private static readonly FieldInfo OverrideField = AccessTools.Field(typeof(RuleRollDice), "ResultOverride");
        private static readonly FieldInfo HistoryField = AccessTools.Field(typeof(RuleRollDice), "<RollHistory>k__BackingField");
        private static readonly FieldInfo SourcesField = AccessTools.Field(typeof(RuleRollDice), "<Rerolls>k__BackingField");

        // Native merge-effective value: reroll draws convert 1 to the die maximum
        // when ReplaceOneWithMax is set; the stored base die stays raw exactly as
        // in the native loop, and the Result getter still converts a final 1.
        private static int Effective(RuleRollDice roll, int raw) => raw == 1 && roll.ReplaceOneWithMax ? roll.DiceFormula.MaxValue(0) : raw;

        private static AuraPart Scope(RuleRollDice roll, out UnitEntityData roller)
        {
            roller = null;
            if (roll.GetType() != typeof(RuleRollD20) || roll.IsFake) return null;
            if (OverrideField.GetValue(roll) != null) return null;
            roller = roll.Initiator;
            var part = roller?.Get<AuraPart>();
            if (part == null || (!part.HasAny(AuraKind.Excellence, roller) && !part.HasAny(AuraKind.Perfection, roller))) return null;
            return part;
        }

        private static void Aggregate(RuleRollDice roll, AuraPart part, UnitEntityData roller)
        {
            bool selfExcellence = part.Has(AuraKind.Excellence, roller, true);
            bool allyExcellence = !selfExcellence && part.Has(AuraKind.Excellence, roller, false);
            bool perfection = part.HasAny(AuraKind.Perfection, roller);
            int baseDie = (int?)PreRolledField.GetValue(roll) ?? RulebookEvent.Dice.D(roll.DiceFormula);
            ResultField.SetValue(roll, baseDie);
            var history = new List<int> { baseDie };
            HistoryField.SetValue(roll, history);
            var sources = (List<RuleRollDice.RerollData>)SourcesField.GetValue(roll) ?? new List<RuleRollDice.RerollData>();
            if (selfExcellence)
            {
                // Self immunity filters take-worst sources and keeps positives even
                // when the native signed sum had cancelled them. Only admitted
                // sources remain recorded, and the aggregate sign stays consistent
                // for any later Reroll(EntityFact,bool) batch on this event.
                sources = sources.Where(s => s.Amount > 0).ToList();
                SourcesField.SetValue(roll, sources);
                RerollAmountField.SetValue(roll, sources.Sum(s => s.Amount));
            }
            int net = (int)RerollAmountField.GetValue(roll);
            var samples = new List<int> { baseDie };
            for (int i = Math.Abs(net); i > 0; i--)
            {
                int raw = RulebookEvent.Dice.D(roll.DiceFormula);
                history.Add(raw);
                int val = Effective(roll, raw);
                samples.Add(val);
                ResultField.SetValue(roll, net > 0 ? Math.Max((int)ResultField.GetValue(roll), val) : Math.Min((int)ResultField.GetValue(roll), val));
            }
            if (net < 0 && allyExcellence)
            {
                int raw = RulebookEvent.Dice.D(roll.DiceFormula);
                history.Add(raw);
                samples.Add(Effective(roll, raw));
                samples.Sort();
                ResultField.SetValue(roll, samples[1]);
            }
            else if (net > 0 && perfection)
            {
                int raw = RulebookEvent.Dice.D(roll.DiceFormula);
                history.Add(raw);
                ResultField.SetValue(roll, Math.Max((int)ResultField.GetValue(roll), Effective(roll, raw)));
            }
        }

        private static void Late(RuleRollDice roll, EntityFact source, bool takeBest, AuraPart part, UnitEntityData roller)
        {
            bool selfExcellence = part.Has(AuraKind.Excellence, roller, true);
            // Reject the negative late request as a whole: no AddReroll record and
            // no private Reroll draw, not merely a suppressed aggregate entry.
            if (selfExcellence && !takeBest) return;
            bool allyExcellence = !selfExcellence && part.Has(AuraKind.Excellence, roller, false);
            bool perfection = part.HasAny(AuraKind.Perfection, roller);
            roll.AddReroll(1, takeBest, source);
            int prior = (int)ResultField.GetValue(roll);
            var history = (List<int>)HistoryField.GetValue(roll);
            int raw = RulebookEvent.Dice.D(roll.DiceFormula);
            history.Add(raw);
            int val = Effective(roll, raw);
            int net = (int)RerollAmountField.GetValue(roll);
            int merged = net > 0 ? Math.Max(prior, val) : Math.Min(prior, val);
            // One real batch [prior valid result, this new die, aura extra die];
            // earlier history is never reprocessed, and the executed Min/Max
            // branch after the signed add decides the ally enhancement.
            if (net < 0 && allyExcellence)
            {
                int extraRaw = RulebookEvent.Dice.D(roll.DiceFormula);
                history.Add(extraRaw);
                var samples = new List<int> { prior, val, Effective(roll, extraRaw) };
                samples.Sort();
                merged = samples[1];
            }
            else if (net > 0 && perfection)
            {
                int extraRaw = RulebookEvent.Dice.D(roll.DiceFormula);
                history.Add(extraRaw);
                merged = Math.Max(merged, Effective(roll, extraRaw));
            }
            ResultField.SetValue(roll, merged);
        }

        [HarmonyPatch]
        internal static class RollPatch
        {
            private static MethodBase TargetMethod() => AccessTools.Method(typeof(RuleRollDice), "Roll", Type.EmptyTypes);
            private static bool Prefix(RuleRollDice __instance)
            {
                var part = Scope(__instance, out var roller);
                if (part == null) return true;
                Aggregate(__instance, part, roller);
                return false;
            }
        }

        [HarmonyPatch]
        internal static class LateRerollPatch
        {
            private static MethodBase TargetMethod() => AccessTools.Method(typeof(RuleRollDice), "Reroll", new[] { typeof(EntityFact), typeof(bool) });
            private static bool Prefix(RuleRollDice __instance, EntityFact rerollSource, bool takeBest)
            {
                var part = Scope(__instance, out var roller);
                if (part == null) return true;
                Late(__instance, rerollSource, takeBest, part, roller);
                return false;
            }
        }
    }
}