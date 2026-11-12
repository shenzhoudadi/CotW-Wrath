// COTW-0070 Cryptid Scholar runtime. Contract C-0070-20261112-r1.
// The scholar's own native knowledge check, exact live-fact classification,
// one traceable Advice batch per caster, 24h per-category receipts on the real
// HA inspiration ledger, and a one-shot precision Strike converted from the
// same batch. Native rule events, native resources and native buff lifecycles
// are the only execution paths; no HA studied-combat runtime is modified.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using UnityEngine;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    public sealed class CryptidCooldownReceipt0070
    {
        [JsonProperty] public string Key;
        [JsonProperty] public TimeSpan Expires;
    }
    public sealed class CryptidAdviceBatch0070
    {
        [JsonProperty] public int Id;
        [JsonProperty] public string Key;
        [JsonProperty] public int Bonus;
        [JsonProperty] public TimeSpan Expires;
    }
    public sealed class CryptidScholarLedgerData0070
    {
        [JsonProperty] public List<string> Credentials = new List<string>();
        [JsonProperty] public List<CryptidCooldownReceipt0070> Receipts = new List<CryptidCooldownReceipt0070>();
        [JsonProperty] public CryptidAdviceBatch0070 Batch;
        [JsonProperty] public int NextBatchId;
    }
    public sealed class CryptidAdviceToken0070
    {
        [JsonProperty] public int BatchId;
        [JsonProperty] public string Key;
    }
    public sealed class CryptidStrikeToken0070
    {
        [JsonProperty] public string Key;
        [JsonProperty] public int Dice;
        [JsonProperty] public TimeSpan Expires;
    }

    // Q03. Exact classification from the unit's live, audited facts. The broad
    // type anchor is one of the eleven verified type facts; giants, goblins and
    // reptilians enter through their verified subtype/race facts exactly like
    // the native favored-enemy donors. Ordinary humanoids without one of those
    // three entries never become a selectable group. InstantEnemyBuff and
    // PlantTypeFake are never credentials.
    internal static class CryptidClassification0070
    {
        internal static readonly string[] BroadTypes =
        {
            "3bec99efd9a363242a6c8d9957b75e91", // AberrationType
            "a95311b3dc996964cbaa30ff9965aaf6", // AnimalType
            "fd389783027d63343b4a5634bd81645f", // ConstructType
            "455ac88e22f55804ab87c2467deff1d6", // DragonType
            "018af8005220ac94a9a4f47b3e9c2b4e", // FeyType
            "625827490ea69d84d8e599a33929fdc6", // MagicalBeastType
            "57614b50e8d86b24395931fffc5e409b", // MonstrousHumanoidType
            "9054d3988d491d944ac144e27b6bc318", // OutsiderType
            "706e61781d692a042b35941f14bc41c5", // PlantType
            "734a29b693e9ec346ba2951b27987e33", // UndeadType
            "09478937695300944a179530664e42ec"  // VerminType
        };
        internal static readonly string[] HumanoidEntries =
        {
            "f9c388137f4faa74aac9065a68b56880", // GiantSubtype
            "9d168ca7100e9314385ce66852385451", // GoblinRace
            "8b8809fd7f9039840ba2374310d7c634"  // ReptilianSubtype
        };
        internal static readonly string[] SubtypeRaceFacts =
        {
            "f9c388137f4faa74aac9065a68b56880", // GiantSubtype
            "9a122a03b2548d94ba90546083d999c0", // LycanthropeType
            "8b8809fd7f9039840ba2374310d7c634", // ReptilianSubtype
            "b7f02ba92b363064fb873963bec275ee", // AasimarRace
            "c4faf439f0e70bd40b5e36ee80d06be7", // DwarfRace
            "25a5878d125338244896ebd3238226c8", // ElfRace
            "ef35a22c9a27da345a4528f0d5889157", // GnomeRace
            "9d168ca7100e9314385ce66852385451", // GoblinRace
            "b3646842ffbd01643ab4dac7479b20b0", // HalfElfRace
            "b0c3ef2729c498f47970bb50fa1acd30", // HalflingRace
            "1dc20e195581a804890ddc74218bfd8e", // HalfOrcRace
            "0a5d473ead98b0646b94495af250fdc4", // HumanRace
            "1b466705276e3124ab43f865e282c6e8", // DemonOfMagicFeature
            "2fe24d538bbcea34cbe9e8600d92bdd2", // DemonOfSlaughterFeature
            "561041cdb5887464883c55c75219a9dc", // DemonOfStrengthFeature
            "e2986f96fa1cd3b4f8d9dfd8a9907731"  // NotADemonFeature
        };

        internal static bool Has(UnitEntityData unit, string guid)
        {
            var fact = ResourcesLibrary.TryGetBlueprint<BlueprintUnitFact>(BlueprintGuid.Parse(guid));
            return fact != null && unit.Descriptor.HasFact(fact);
        }

        // Null is an explicit "no verified category"; there is no default key
        // that would match every unit.
        internal static string KeyOf(UnitEntityData unit)
        {
            if (unit?.Descriptor == null) return null;
            string anchor = BroadTypes.FirstOrDefault(id => Has(unit, id))
                ?? HumanoidEntries.FirstOrDefault(id => Has(unit, id));
            if (anchor == null) return null;
            var set = SubtypeRaceFacts.Where(id => Has(unit, id)).Distinct().OrderBy(id => id, StringComparer.Ordinal);
            return anchor + "|" + string.Join(",", set);
        }
    }

    internal static class CryptidScholarRuntime0070
    {
        internal const float ThirtyFeet = 9.144f;
        internal static readonly TimeSpan Day = TimeSpan.FromHours(24);
        internal static TimeSpan GameTime => Game.Instance.TimeController.GameTime;

        internal static BlueprintAbilityResource InspirationResource(BlueprintsCache cache)
            => cache.Load(BlueprintGuid.Parse(CryptidScholar0070Ids.InspirationResourceId)) as BlueprintAbilityResource;

        internal static Feature LedgerFactOf(UnitEntityData unit)
            => unit?.Facts?.List?.OfType<Feature>().FirstOrDefault(f =>
                f.Blueprint.AssetGuid == Id(CryptidScholar0070Ids.LoreToken) && f.IsActive && f.IsTurnedOn);

        internal static T WithLedger<T>(UnitEntityData unit, Func<CryptidScholarLedgerData0070, T> action)
        {
            var fact = LedgerFactOf(unit);
            if (fact == null) return default;
            foreach (var runtime in fact.Components.Where(r => r.SourceBlueprintComponent is CryptidScholarLedger0070))
                using (runtime.RequestEventContext())
                    return action(((CryptidScholarLedger0070)runtime.SourceBlueprintComponent).Ledger);
            return default;
        }

        internal static bool WithinCooldown(CryptidScholarLedgerData0070 data, string key)
            => data.Receipts.Any(r => r.Key == key && r.Expires > GameTime);

        internal static IEnumerable<UnitEntityData> PartyUnits()
        {
            var player = Game.Instance?.Player;
            if (player == null) yield break;
            foreach (var unit in player.PartyAndPets) if (unit != null) yield return unit;
        }

        internal static bool AudibleListener(UnitEntityData unit)
            => unit != null && !unit.State.IsDead && unit.State.IsConscious && AudibleAbilityRestriction.Audible(unit);

        internal static IEnumerable<UnitEntityData> AroundCaster(UnitEntityData caster)
        {
            var seen = new HashSet<UnitEntityData>();
            foreach (var unit in GameHelper.GetTargetsAround(caster.Position, ThirtyFeet, false, false))
                if (unit != null && seen.Add(unit)) yield return unit;
            if (seen.Add(caster)) yield return caster;
        }

        internal static CryptidAdviceToken0070 AdviceToken(Buff buff)
        {
            foreach (var runtime in buff.Components.Where(r => r.SourceBlueprintComponent is CryptidAdviceBuff0070))
                using (runtime.RequestEventContext())
                    return ((CryptidAdviceBuff0070)runtime.SourceBlueprintComponent).Token;
            return null;
        }

        internal static CryptidStrikeToken0070 StrikeToken(Buff buff)
        {
            foreach (var runtime in buff.Components.Where(r => r.SourceBlueprintComponent is CryptidStrikeBuff0070))
                using (runtime.RequestEventContext())
                    return ((CryptidStrikeBuff0070)runtime.SourceBlueprintComponent).Token;
            return null;
        }

        internal static bool LiveAdvice(Buff buff)
            => buff != null && !buff.IsDisposed && buff.IsActive && buff.IsTurnedOn && !buff.IsSuppressed;

        // Q08. Only this caster's own batches and strikes are retired; another
        // scholar's Advice and every independent HA fact stay untouched.
        internal static void RemoveOwnBatch(UnitEntityData caster, int batchId)
        {
            foreach (var unit in PartyUnits().Concat(new[] { caster }).Distinct().ToArray())
            {
                if (unit?.Buffs == null) continue;
                foreach (var buff in unit.Buffs.Enumerable.ToArray())
                {
                    if (buff.Blueprint.AssetGuid != Id(CryptidScholar0070Ids.AdviceBuffToken)) continue;
                    if (!ReferenceEquals(buff.Context?.MaybeCaster, caster)) continue;
                    var token = AdviceToken(buff);
                    if (token != null && token.BatchId == batchId && !buff.IsDisposed) buff.Remove();
                }
            }
        }

        internal static void RemoveOwnSource(UnitEntityData caster)
        {
            foreach (var unit in PartyUnits().Concat(new[] { caster }).Distinct().ToArray())
            {
                if (unit?.Buffs == null) continue;
                foreach (var buff in unit.Buffs.Enumerable.ToArray())
                {
                    var guid = buff.Blueprint.AssetGuid;
                    if (guid != Id(CryptidScholar0070Ids.AdviceBuffToken) && guid != Id(CryptidScholar0070Ids.StrikeBuffToken)) continue;
                    if (!ReferenceEquals(buff.Context?.MaybeCaster, caster)) continue;
                    if (!buff.IsDisposed) buff.Remove();
                }
            }
        }

        // One consumption per real attack roll, shared by every damage bundle
        // and duplicate callback of that attack. Attack rolls are transient;
        // nothing here is saved or re-granted on load.
        internal static readonly ConditionalWeakTable<RuleAttackRoll, object> ConsumedStrikes = new ConditionalWeakTable<RuleAttackRoll, object>();
        internal static readonly ConditionalWeakTable<MechanicsContext, object> ProcessedCasts = new ConditionalWeakTable<MechanicsContext, object>();
    }

    // Q03/Q04/Q08. Persistent per-scholar credentials, 24-hour receipts and the
    // single current Advice batch. Rides the Intuitive Lore feature, so losing
    // the variant, retraining or losing the source retires exactly this
    // scholar's authorisations.
    [TypeId("980ab71c90b944dc9f6dd78d1f79cb19")]
    public sealed class CryptidScholarLedger0070 : UnitFactComponentDelegate<CryptidScholarLedgerData0070>
    {
        public CryptidScholarLedgerData0070 Ledger => Data;

        protected override void OnTurnOff()
        {
            var owner = Owner;
            CryptidScholarRuntime0070.RemoveOwnSource(owner);
            Data.Credentials.Clear();
            Data.Receipts.Clear();
            Data.Batch = null;
        }

        protected override void OnApplyPostLoadFixes()
        {
            var now = CryptidScholarRuntime0070.GameTime;
            Data.Receipts.RemoveAll(r => r.Expires <= now);
            if (Data.Batch != null && Data.Batch.Expires <= now) Data.Batch = null;
        }
    }

    // Q05. Insight AC against the matching category's attacks and Insight saves
    // against that category's Extraordinary/Supernatural/SpellLike sources.
    // The token resolves its batch from the issuing scholar's live ledger, so a
    // retired, replaced or expired batch never keeps granting bonuses.
    [TypeId("fde554123fb240348f24f589ec2e7f8c")]
    public sealed class CryptidAdviceBuff0070 : UnitBuffComponentDelegate<CryptidAdviceToken0070>,
        ITargetRulebookHandler<RuleCalculateAC>, IInitiatorRulebookHandler<RuleSavingThrow>,
        ITargetRulebookSubscriber, IInitiatorRulebookSubscriber
    {
        public CryptidAdviceToken0070 Token => Data;

        CryptidAdviceBatch0070 ResolveBatch()
        {
            if (!CryptidScholarRuntime0070.LiveAdvice(Buff)) return null;
            if (Owner == null || Owner.State.IsDead || !Owner.State.IsConscious) return null;
            var caster = Context?.MaybeCaster;
            if (caster == null) return null;
            var token = Data;
            if (token.Key == null) return null;
            return CryptidScholarRuntime0070.WithLedger(caster, data =>
                data.Batch != null && data.Batch.Id == token.BatchId && data.Batch.Key == token.Key &&
                data.Batch.Expires > CryptidScholarRuntime0070.GameTime ? data.Batch : null);
        }

        public void OnEventAboutToTrigger(RuleCalculateAC evt)
        {
            var batch = ResolveBatch();
            if (batch == null || evt.Initiator == null || evt.Initiator == Owner) return;
            var key = CryptidClassification0070.KeyOf(evt.Initiator);
            if (key == null || key != batch.Key) return;
            evt.AddModifier(batch.Bonus, Fact, ModifierDescriptor.Insight);
        }
        public void OnEventDidTrigger(RuleCalculateAC evt) { }

        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            var batch = ResolveBatch();
            if (batch == null) return;
            var context = evt.Reason?.Context;
            if (context == null) return;
            var ability = context.SourceAbilityContext?.Ability;
            if (ability == null && context is AbilityExecutionContext execution) ability = execution.Ability;
            var type = ability?.Blueprint?.Type;
            if (type != AbilityType.Extraordinary && type != AbilityType.Supernatural && type != AbilityType.SpellLike) return;
            var caster = context.MaybeCaster;
            if (caster == null || caster == Owner) return;
            var key = CryptidClassification0070.KeyOf(caster);
            if (key == null || key != batch.Key) return;
            // The native replacement comparison still decides the rolled stat;
            // the bonus joins the same real candidate the native event uses.
            var stat = Owner.Stats.GetStat(RuleSavingThrow.GetStatFromSavingThrow(evt.Type));
            if (stat == null) return;
            var replace = Owner.Get<UnitPartSavingThrowsReplace>();
            if (replace != null && replace.SavingThrowStat == stat.Type)
            {
                var alternative = Owner.Stats.GetStat(replace.ReplaceToStat);
                if (alternative != null && alternative.ModifiedValue > stat.ModifiedValue) stat = alternative;
            }
            evt.AddTemporaryModifier(stat.AddModifier(batch.Bonus, Runtime, ModifierDescriptor.Insight));
        }
        public void OnEventDidTrigger(RuleSavingThrow evt) { }

        protected override void OnApplyPostLoadFixes()
        {
            if (ResolveBatch() == null && Buff != null && !Buff.IsDisposed) Buff.Remove();
        }
    }

    // Q06. One-shot precision conversion of the scholar's current Advice batch.
    // Only a real, qualifying weapon hit on the exact category consumes it;
    // concealment, precision immunity, DisablePrecisionDamage, misses, rays and
    // out-of-range ranged attacks never consume the opportunity.
    [TypeId("eda899552790413ca2325c0433456dcd")]
    public sealed class CryptidStrikeBuff0070 : UnitBuffComponentDelegate<CryptidStrikeToken0070>,
        IInitiatorRulebookHandler<RulePrepareDamage>, IInitiatorRulebookSubscriber
    {
        public CryptidStrikeToken0070 Token => Data;

        public void OnEventAboutToTrigger(RulePrepareDamage evt)
        {
            if (!CryptidScholarRuntime0070.LiveAdvice(Buff)) return;
            var token = Data;
            if (token.Key == null) return;
            if (token.Expires <= CryptidScholarRuntime0070.GameTime) { Buff.Remove(); return; }
            var caster = Context?.MaybeCaster;
            if (caster == null || CryptidScholarRuntime0070.LedgerFactOf(caster) == null) { Buff.Remove(); return; }
            var roll = evt.ParentRule?.AttackRoll;
            if (roll == null || !roll.IsHit || roll.IsFake || roll.Target != evt.Target) return;
            if (CryptidScholarRuntime0070.ConsumedStrikes.TryGetValue(roll, out _)) return;
            if (evt.ParentRule.DisablePrecisionDamage || roll.ImmuneToSneakAttack) return;
            if (roll.ConcealmentCheck != null && roll.ConcealmentCheck.Concealment != Concealment.None) return;
            if (evt.DamageBundle.Empty()) return;
            var weapon = evt.DamageBundle.Weapon;
            if (weapon == null) return;
            if (evt.Initiator != Owner || evt.Target == null || evt.Target.State.IsDead) return;
            var key = CryptidClassification0070.KeyOf(evt.Target);
            if (key == null || key != token.Key) return;
            if (weapon.Blueprint.IsRanged && Vector3.Distance(Owner.Position, evt.Target.Position) > CryptidScholarRuntime0070.ThirtyFeet) return;
            CryptidScholarRuntime0070.ConsumedStrikes.Add(roll, new object());
            // Native fortification still protects the target once the trigger
            // threshold passed; the opportunity is spent either way.
            if (roll.TargetUseFortification)
            {
                bool overcome = roll.FortificationRoll != null
                    ? roll.FortificationOvercomed
                    : (int)RulebookEvent.Dice.D(new DiceFormula(1, DiceType.D100)) > roll.FortificationChance;
                if (!overcome) { Buff.Remove(); return; }
            }
            var damage = evt.DamageBundle.First().CreateTypeDescription()
                .GetDamageDescriptor(new DiceFormula(token.Dice, DiceType.D6)).CreateDamage();
            damage.Precision = true;
            damage.CriticalModifier = 1;
            evt.Add(damage);
            Buff.Remove();
        }
        public void OnEventDidTrigger(RulePrepareDamage evt) { }

        protected override void OnApplyPostLoadFixes()
        {
            if (Data.Key == null || Data.Expires <= CryptidScholarRuntime0070.GameTime)
                if (Buff != null && !Buff.IsDisposed) Buff.Remove();
        }
    }

    // Q03/Q04. The whole Move chain: native personal knowledge check (or the
    // scholar's own persisted credential), exact classification, audible allies
    // within 30 feet, conditional 1-point debit from the real HA inspiration
    // pool and one traceable batch. Rejections leave Advice, resources and
    // receipts untouched; a failed application refunds only the point actually
    // debited by this cast.
    [TypeId("da6b0676f79445b8b2e90c5c4c4a1497")]
    public sealed class CryptidAdviceAction0070 : ContextAction
    {
        public BlueprintCharacterClassReference CharacterClass;
        public BlueprintAbilityResourceReference Resource;
        public BlueprintBuffReference AdviceBuff;

        public override string GetCaption() => "Share identified monster insights with nearby allies";

        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            var target = Target?.Unit;
            if (caster == null || target == null) return;
            if (CryptidScholarRuntime0070.ProcessedCasts.TryGetValue(Context, out _)) return;
            if (!target.IsEnemy(caster) || target.State.IsDead) return;
            if (!AudibleAbilityRestriction.Audible(caster)) return;
            var key = CryptidClassification0070.KeyOf(target);
            if (key == null) return;
            var cls = CharacterClass?.Get();
            var resource = Resource?.Get();
            var advice = AdviceBuff?.Get();
            if (cls == null || resource == null || advice == null) return;
            var ledgerFact = CryptidScholarRuntime0070.LedgerFactOf(caster);
            if (ledgerFact == null) return;

            bool paid = false;
            using (var scope = ledgerFact.Components.First(r => r.SourceBlueprintComponent is CryptidScholarLedger0070).RequestEventContext())
            {
                var data = ((CryptidScholarLedger0070)ledgerFact.Components.First(r => r.SourceBlueprintComponent is CryptidScholarLedger0070).SourceBlueprintComponent).Ledger;
                if (!data.Credentials.Contains(key))
                {
                    bool success = Game.Instance.Player.InspectUnitsManager.TryMakeKnowledgeCheck(target, caster);
                    if (!success) return;
                    data.Credentials.Add(key);
                }
                var recipients = CryptidScholarRuntime0070.AroundCaster(caster)
                    .Where(u => (u == caster || u.IsAlly(caster)) && CryptidScholarRuntime0070.AudibleListener(u) &&
                        Vector3.Distance(u.Position, caster.Position) <= CryptidScholarRuntime0070.ThirtyFeet)
                    .Distinct().ToArray();
                if (recipients.Length == 0) return;
                if (CryptidScholarRuntime0070.WithinCooldown(data, key))
                {
                    if (!caster.Resources.HasEnoughResource(resource, 1)) return;
                    caster.Resources.Spend(resource, 1);
                    paid = true;
                }
                try
                {
                    if (data.Batch != null) CryptidScholarRuntime0070.RemoveOwnBatch(caster, data.Batch.Id);
                    int bonus = Math.Min(5, caster.Progression.GetClassLevel(cls) / 4);
                    int rounds = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default }.Calculate(Context);
                    if (rounds < 1) rounds = 1;
                    var duration = TimeSpan.FromSeconds(rounds * 6);
                    var now = CryptidScholarRuntime0070.GameTime;
                    int id = ++data.NextBatchId;
                    data.Batch = new CryptidAdviceBatch0070 { Id = id, Key = key, Bonus = bonus, Expires = now + duration };
                    foreach (var unit in recipients)
                    {
                        // One Advice per recipient: a valid new batch replaces
                        // any earlier Advice, regardless of its source.
                        foreach (var old in unit.Buffs.Enumerable.Where(b => b.Blueprint.AssetGuid == advice.AssetGuid && !b.IsDisposed).ToArray())
                            old.Remove();
                        var buff = unit.AddBuff(advice, Context, duration);
                        if (buff == null) continue;
                        foreach (var runtime in buff.Components.Where(r => r.SourceBlueprintComponent is CryptidAdviceBuff0070))
                            using (runtime.RequestEventContext())
                            {
                                var token = ((CryptidAdviceBuff0070)runtime.SourceBlueprintComponent).Token;
                                token.BatchId = id;
                                token.Key = key;
                            }
                    }
                    var receipt = data.Receipts.FirstOrDefault(r => r.Key == key);
                    if (receipt == null) data.Receipts.Add(new CryptidCooldownReceipt0070 { Key = key, Expires = now + CryptidScholarRuntime0070.Day });
                    else receipt.Expires = now + CryptidScholarRuntime0070.Day;
                    CryptidScholarRuntime0070.ProcessedCasts.Add(Context, new object());
                }
                catch
                {
                    if (paid) caster.Resources.Restore(resource, 1);
                    throw;
                }
            }
        }
    }

    // Q06. Convert the scholar's own current Advice batch into one round of
    // Knowledgeable Strike for the same-source holders still in range and
    // audible. Other scholars' batches are never touched.
    [TypeId("850b39cae3d546d487ab6a21fb78369f")]
    public sealed class CryptidStrikeAction0070 : ContextAction
    {
        public BlueprintBuffReference AdviceBuff;
        public BlueprintBuffReference StrikeBuff;

        public override string GetCaption() => "Convert current opportune advice into a knowledgeable strike";

        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            if (caster == null) return;
            if (CryptidScholarRuntime0070.ProcessedCasts.TryGetValue(Context, out _)) return;
            if (!AudibleAbilityRestriction.Audible(caster)) return;
            var advice = AdviceBuff?.Get();
            var strike = StrikeBuff?.Get();
            if (advice == null || strike == null) return;
            var ledgerFact = CryptidScholarRuntime0070.LedgerFactOf(caster);
            if (ledgerFact == null) return;

            using (var scope = ledgerFact.Components.First(r => r.SourceBlueprintComponent is CryptidScholarLedger0070).RequestEventContext())
            {
                var data = ((CryptidScholarLedger0070)ledgerFact.Components.First(r => r.SourceBlueprintComponent is CryptidScholarLedger0070).SourceBlueprintComponent).Ledger;
                var batch = data.Batch;
                if (batch == null || batch.Expires <= CryptidScholarRuntime0070.GameTime) return;
                var own = caster.Buffs.Enumerable.FirstOrDefault(b =>
                    b.Blueprint.AssetGuid == advice.AssetGuid && CryptidScholarRuntime0070.LiveAdvice(b) &&
                    ReferenceEquals(b.Context?.MaybeCaster, caster) &&
                    CryptidScholarRuntime0070.AdviceToken(b)?.BatchId == batch.Id);
                if (own == null) return;
                var holders = CryptidScholarRuntime0070.PartyUnits().Concat(new[] { caster }).Distinct()
                    .Where(u => u?.Buffs != null)
                    .SelectMany(u => u.Buffs.Enumerable.Where(b =>
                        b.Blueprint.AssetGuid == advice.AssetGuid && CryptidScholarRuntime0070.LiveAdvice(b) &&
                        ReferenceEquals(b.Context?.MaybeCaster, caster) &&
                        CryptidScholarRuntime0070.AdviceToken(b)?.BatchId == batch.Id)
                        .Select(b => new { Unit = u, Buff = b }))
                    .ToArray();
                var eligible = holders.Where(h =>
                    CryptidScholarRuntime0070.AudibleListener(h.Unit) &&
                    Vector3.Distance(h.Unit.Position, caster.Position) <= CryptidScholarRuntime0070.ThirtyFeet)
                    .Select(h => h.Unit).Distinct().ToArray();
                if (eligible.Length == 0) return;
                foreach (var holder in holders) if (!holder.Buff.IsDisposed) holder.Buff.Remove();
                data.Batch = null;
                var duration = TimeSpan.FromSeconds(6);
                var expires = CryptidScholarRuntime0070.GameTime + duration;
                foreach (var unit in eligible)
                {
                    // One Strike per beneficiary: a new grant replaces the old.
                    foreach (var old in unit.Buffs.Enumerable.Where(b => b.Blueprint.AssetGuid == strike.AssetGuid && !b.IsDisposed).ToArray())
                        old.Remove();
                    var buff = unit.AddBuff(strike, Context, duration);
                    if (buff == null) continue;
                    foreach (var runtime in buff.Components.Where(r => r.SourceBlueprintComponent is CryptidStrikeBuff0070))
                        using (runtime.RequestEventContext())
                        {
                            var token = ((CryptidStrikeBuff0070)runtime.SourceBlueprintComponent).Token;
                            token.Key = batch.Key;
                            token.Dice = batch.Bonus;
                            token.Expires = expires;
                        }
                }
                CryptidScholarRuntime0070.ProcessedCasts.Add(Context, new object());
            }
        }
    }

    // The advice Move targets one hostile living unit; the native knowledge
    // check decides inspectability, skill, training and DC.
    [TypeId("44348593d43e406d88c95c573fde21a3")]
    public sealed class CryptidAdviceTarget0070 : BlueprintComponent, IAbilityTargetRestriction
    {
        public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target)
        {
            var unit = target?.Unit;
            return unit != null && caster != null && unit.IsEnemy(caster) && !unit.State.IsDead;
        }
        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target)
            => "Target must be a hostile living creature.";
    }

    // Q07. Only this variant's real Investigator talent ancestry is restricted;
    // plain HA Investigators, Psychic Detectives and independent feat sources
    // keep their native options. The nine original study talents plus both
    // Ranged Study entries are the frozen list; Slowing/Repositioning Strike
    // stay available because no dedicated Studied Strike execution dependency
    // was proven from the pinned provider sources.
    [HarmonyPatch(typeof(BlueprintFeatureSelection), nameof(BlueprintFeatureSelection.CanSelect))]
    internal static class CryptidScholarTalent0070Patch
    {
        private static readonly HashSet<BlueprintGuid> Forbidden = new HashSet<BlueprintGuid>(
            CryptidScholar0070Ids.ExcludedTalentIds.Select(BlueprintGuid.Parse));

        internal static bool HasCryptidScholar(UnitDescriptor unit)
        {
            var cls = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(BlueprintGuid.Parse(CryptidScholar0070Ids.ClassId));
            if (cls == null || unit?.Unit == null) return false;
            var data = unit.Progression?.GetClassData(cls);
            return data?.Archetypes?.Any(a => a != null && a.AssetGuid == CryptidScholar0070Ids.ArchetypeId) == true;
        }

        private static bool Prefix(BlueprintFeatureSelection __instance, UnitDescriptor __0, LevelUpState __1,
            FeatureSelectionState __2, IFeatureSelectionItem __3, ref bool __result)
        {
            var feature = __3?.Feature;
            if (feature == null || !HasCryptidScholar(__0) ||
                !PsychicDetectiveTalent0068Patch.FromInvestigatorTalents(__2)) return true;
            if (Forbidden.Contains(feature.AssetGuid)) { __result = false; return false; }
            return true;
        }
    }
}