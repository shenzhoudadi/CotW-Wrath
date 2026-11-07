// S-KINETIC / F-0020 runtime, K01-HAVOCKER-20261106-r1.
// CotW design: Holic75/KingmakerRebalance (MIT); LICENSE-CotW.txt.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class HavockerSpellburn0020
    {
        internal const string FeatureToken = "SpellburnHavockerFeature0020";
        // Native Gather Power ability. Its blueprint stores sprite 5f31e79573fa7ab43a1b4de6bb210182.
        // The kineticist Burn feature's authored m_Icon is null, so it cannot fill this buff.
        internal const string IconDonorId = "6dcbffb8012ba2a4cb4ac374a33e2d9a";
        internal const string BookId = "dd04f9239f655ea438976742728e4909";
        internal static TimeSpan Now => Game.Instance.TimeController.GameTime;
        internal static UnitDescriptor DeclaredCaster(AbilityData ability) => ability == null ? null : ((EntityRef<UnitEntityData>)Get(ability, "m_CasterRef")).Entity?.Descriptor;
        internal static bool OwnBlast(BlueprintAbility bp) {
            var cost = bp?.GetComponent<HavockerBlastCost0020>();
            return cost != null && bp.AssetGuid == Id(KineticistClassBridge0020.CloneToken(cost.Original));
        }
        internal static bool OwnActivation(BlueprintAbility bp) { var c = bp?.GetComponent<HavockerSpellburnActivation0020>(); return c != null && c.SlotLevel >= 2 && c.SlotLevel <= 9 && bp.AssetGuid == Id("SpellBurn" + c.SlotLevel + "Ability0020"); }

        // Runtime delegates require their actual fact-component event context;
        // never return a shared blueprint delegate with an unbound Owner/Data.
        internal static T With<T>(UnitDescriptor owner, Func<HavockerSpellburnController0020, T> action, T missing = default(T)) {
            if (owner == null) return missing;
            var id = Id(KineticistClassBridge0020.BridgeToken);
            foreach (var fact in owner.Facts.List.Where(f => f.Blueprint.AssetGuid == id && f.IsActive && f.IsTurnedOn && !f.IsDisposed).ToArray())
                foreach (var runtime in fact.Components.Where(c => c.SourceBlueprintComponent is HavockerSpellburnController0020).ToArray())
                    using (runtime.RequestEventContext()) return action((HavockerSpellburnController0020)runtime.SourceBlueprintComponent);
            return missing;
        }

        internal sealed class CastRecord {
            internal UnitDescriptor Owner;
            internal AbilityExecutionContext Context;
            internal HavockerSpellburnState0020 State;
            internal long ChargeId, Sequence;
            internal int Discount;
            internal bool Paid, Busy, NativeEvent;
        }
        sealed class ContextLink { internal AbilityExecutionContext Context; }
        static readonly ConditionalWeakTable<AbilityData, ContextLink> LastCasts = new ConditionalWeakTable<AbilityData, ContextLink>();
        static readonly ConditionalWeakTable<AbilityExecutionContext, CastRecord> Payments = new ConditionalWeakTable<AbilityExecutionContext, CastRecord>();
        internal static CastRecord CommittedReceipt(AbilityExecutionContext context) => context != null
            && Payments.TryGetValue(context, out var paid) && paid.Paid && paid.NativeEvent ? paid : null;
        internal static void LinkPaidContext(AbilityExecutionContext context, CastRecord paid) {
            if (context == null || paid == null || !paid.Paid) throw new InvalidOperationException("Havocker blade has no committed native activation.");
            if (Payments.TryGetValue(context, out var previous)) {
                if (!ReferenceEquals(previous, paid)) throw new InvalidOperationException("Havocker child context already has another payment identity.");
            } else Payments.Add(context, paid);
        }
        [ThreadStatic] internal static CastRecord CurrentPayment;
        internal static void Observe(RuleCastSpell rule) {
            if (!rule.Success || rule.Context == null || rule.IsCutscene || rule.IsDuplicateSpellApplied || !rule.ShouldSpendResource) return;
            LastCasts.Remove(rule.Spell); LastCasts.Add(rule.Spell, new ContextLink { Context = rule.Context });
        }
        internal static AbilityExecutionContext ContextFor(AbilityData ability) {
            var current = ContextData<MechanicsContext.Data>.Current?.Context?.SourceAbilityContext;
            if (ReferenceEquals(current?.Ability, ability)) return current;
            return ability != null && LastCasts.TryGetValue(ability, out var link) && ReferenceEquals(link.Context.Ability, ability) ? link.Context : null;
        }
        internal static CastRecord Receipt(AbilityData ability) {
            var context = ContextFor(ability);
            return context == null ? null : Payments.GetValue(context, c => new CastRecord { Context = c, Owner = DeclaredCaster(ability) });
        }

        internal static Spellbook BoundBook(UnitDescriptor owner) {
            if (owner == null) return null;
            var cls = owner.Progression.Classes.FirstOrDefault(c => c.CharacterClass.AssetGuid == BlueprintGuid.Parse(KineticistClassBridge0020.WitchId) && c.Level > 0);
            if (cls?.Spellbook?.AssetGuid != BlueprintGuid.Parse(BookId) || !ReferenceEquals(cls.Spellbook, ResourcesLibrary.TryGetBlueprint<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook>(BookId)) || cls.Spellbook.Spontaneous || !cls.Spellbook.MemorizeSpells) return null;
            return owner.Spellbooks.FirstOrDefault(b => ReferenceEquals(b.Owner, owner) && ReferenceEquals(b.Blueprint, cls.Spellbook));
        }

        internal static bool SourceSlot(AbilityData ability, int level, out Spellbook book, out SpellSlot slot) {
            book = null; slot = null;
            if (ability == null || level < 2 || level > 9) return false;
            // AbilityData.Equals, Spellbook and SourceItem can recurse through
            // ConvertedFrom. Prove the chain finite by reference identity first.
            var chain = new HashSet<object>(IdentityComparer); var leaf = ability;
            while (leaf != null && chain.Add(leaf)) { if (leaf.ConvertedFrom == null) break; leaf = leaf.ConvertedFrom; }
            if (leaf == null || leaf.ConvertedFrom != null || ReferenceEquals(leaf, ability)) return false;
            var owner = DeclaredCaster(ability);
            if (!KineticistClassBridge0020.CanUseBridge(owner) || owner.Progression.GetClassLevel(ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(KineticistClassBridge0020.WitchId)) < 6) return false;
            book = BoundBook(owner);
            if (book == null || !ReferenceEquals(leaf.Caster, owner) || !ReferenceEquals(leaf.Spellbook, book) || !ReferenceEquals(ability.Spellbook, book)) return false;
            for (var node = ability; node != null; node = node.ConvertedFrom)
                if (!ReferenceEquals(DeclaredCaster(node), owner) || node.SourceItem != null) return false;
            var matches = book.GetMemorizedSpellSlots(level).Where(s => s != null && ReferenceEquals(s.SpellShell, leaf)).ToArray();
            // Native Memorize deliberately shares one AbilityData between
            // duplicate prepared copies. Follow native Spend's reverse available
            // slot order, but only inside the reference-identical source set;
            // never fall back to another same-level/value-equal spell.
            var selected = matches.LastOrDefault(s => s.Available && !s.IsDouble);
            if (selected == null || !matches.Contains(selected) || !selected.Available || selected.SpellLevel != level || selected.IsDouble || !book.CanSpend(leaf)) return false;
            slot = selected; return true;
        }

        internal static void Stage(KineticistClassBridge0020 graph) {
            var buffs = new BlueprintBuff[10];
            // Native ReferenceArrayProxy's IEnumerable path calls .Get() on
            // every reference, including 0/1. Empty GUID references represent
            // absent entries; literal null references abort native conversion
            // registration before the prepared-slot menu can offer Spellburn.
            var abilities = Enumerable.Range(0, 10).Select(_ => new BlueprintAbilityReference()).ToArray();
            var shown = new List<BlueprintUnitFact>();
            for (int level = 2; level <= 9; level++) {
                string text = "Sacrifice one prepared witch spell of this level. For 1 round, reduce the infusion burn of your next qualifying kinetic blast by " + KineticistClassBridge0020.SpellburnCredit(level) + ". This does not reduce blast, wild talent or metakinesis burn. Only one Spellburn charge may be pending.";
                string buffToken = "SpellBurn" + level + "Buff0020", abilityToken = "SpellBurn" + level + "Ability0020";
                buffs[level] = graph.Add(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace, ComponentsArray = Array.Empty<BlueprintComponent>() }, buffToken, "Spellburn " + level, text);
                var ability = graph.Add(new BlueprintAbility { Type = AbilityType.Supernatural, Range = AbilityRange.Personal, ActionType = UnitCommand.CommandType.Free,
                    CanTargetSelf = true, ComponentsArray = new BlueprintComponent[] { new HavockerSpellburnActivation0020 { SlotLevel = level } } }, abilityToken, "Spellburn " + level, text);
                abilities[level] = Ref<BlueprintAbilityReference>(ability);
                shown.Add(buffs[level]);
                shown.Add(ability);
            }
            var conversion = new HavockerPreparedConversion0020();
            Set(conversion, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(KineticistClassBridge0020.WitchId)));
            Set(conversion, "m_SpellsByLevel", abilities);
            var feature = graph.Add(new BlueprintFeature { IsClassFeature = true, Ranks = 1, ComponentsArray = new BlueprintComponent[] { conversion } }, FeatureToken, "Spellburn", "At witch level 6, sacrifice prepared witch spells of levels 2 through 9 to fuel kinetic blast infusions. A charge lasts 1 round and is spent on resource commitment, even on a miss.");
            shown.Add(feature);
            ApplyIcons(shown.ToArray());
            var bridge = graph.Copy<BlueprintFeature>(KineticistClassBridge0020.BurnFeatureId);
            var settings = bridge.GetComponent<AddKineticistPart>();
            bridge.ComponentsArray = bridge.ComponentsArray.Concat(new BlueprintComponent[] { new HavockerSpellburnController0020 {
                ChargeBuffs = buffs.Select(b => b == null ? null : Ref<BlueprintBuffReference>(b)).ToArray(),
                GatherBuffs = new[] { settings.GatherPowerBuff1, settings.GatherPowerBuff2, settings.GatherPowerBuff3 }.Select(b => Ref<BlueprintBuffReference>(graph.Copy<BlueprintBuff>(b.AssetGuid.ToString()))).ToArray()
            } }).ToArray();
        }
        // The offline host drops bundle Sprites. Copy the donor reference anyway;
        // a null here means that host has not materialized the authored sprite.
        internal static void ApplyIcons(BlueprintUnitFact[] facts) {
            var donor = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(IconDonorId) ?? throw new InvalidOperationException("Spellburn icon donor missing: " + IconDonorId);
            var icon = Get(donor, "m_Icon");
            foreach (var fact in facts) if (fact != null) Set(fact, "m_Icon", icon);
        }

        // Only the explicitly staged private blast is changed. The complete
        // element/variant/weapon graph remains F-0020 wiring, not a library scan.
        internal static void PrepareBlast(KineticistClassBridge0020 graph, BlueprintAbility ability) {
            var source = ability.GetComponents<AbilityKineticist>().Single();
            if (source is HavockerBlastCost0020) return;
            var cost = new HavockerBlastCost0020 { Original = graph.Copies.Single(p => ReferenceEquals(p.Value, ability)).Key, BlastBurnCost = source.BlastBurnCost, InfusionBurnCost = source.InfusionBurnCost,
                WildTalentBurnCost = source.WildTalentBurnCost, AllowOnlyBurnCost = source.AllowOnlyBurnCost,
                CachedDamageInfo = CopyData(source.CachedDamageInfo), CachedDamageSource = CopyData(source.CachedDamageSource) };
            ability.ComponentsArray = ability.ComponentsArray.Select(c => ReferenceEquals(c, source) ? cost : c).ToArray();
        }
        internal static void Install(Harmony harmony) {
            harmony.CreateClassProcessor(typeof(HavockerSpellburnPay0020Patch)).Patch();
            harmony.CreateClassProcessor(typeof(HavockerBurnCost0020Patch)).Patch();
        }
    }

    public enum HavockerChargeStatus0020 { Absent, Armed, Reserved, Consumed }
    public sealed class HavockerSpellburnState0020 {
        [JsonProperty] public long Sequence, ChargeId;
        [JsonProperty] public int SlotLevel, Credit;
        [JsonProperty] public long ExpiresTicks;
        [JsonIgnore] public TimeSpan Expires { get => TimeSpan.FromTicks(ExpiresTicks); set => ExpiresTicks = value.Ticks; }
        [JsonProperty] public HavockerChargeStatus0020 Status;
        [JsonProperty] public string SourceBook, SourceSpell;
        [JsonProperty] public int SourceSlot;
        [JsonProperty] public EntityFactRef<Buff> Display;
        [JsonProperty] public List<EntityFactRef<Buff>> PendingDisplays = new List<EntityFactRef<Buff>>();
        [JsonIgnore] public int NativeAcceptEvents;
        [JsonIgnore] public bool Clearing;
    }

    [TypeId("639e98762a1d6cb128a9c69c735ef1b8")]
    public sealed class HavockerSpellburnController0020 : UnitFactComponentDelegate<HavockerSpellburnState0020>, IKineticistAcceptBurnHandler, IUnitRestHandler, IUnitBuffHandler, ITickEachRound, IInitiatorRulebookHandler<RuleCastSpell>, IGlobalSubscriber
    {
        public BlueprintBuffReference[] ChargeBuffs;
        public BlueprintBuffReference[] GatherBuffs;
        internal bool OwnPart { get { var part = Owner.Get<UnitPartKineticist>(); return part != null && KineticistClassBridge0020.CanUseBridge(Owner) && Get(part, "m_Settings") is HavockerOwnedKineticistPart0020; } }
        internal bool Pending => (Data.Status == HavockerChargeStatus0020.Armed || Data.Status == HavockerChargeStatus0020.Reserved) && Data.Expires > HavockerSpellburn0020.Now;
        internal bool CanActivate(AbilityData ability, int level) => OwnPart && !Pending && Owner.HasFact(ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(Id(HavockerSpellburn0020.FeatureToken))) && HavockerSpellburn0020.SourceSlot(ability, level, out _, out _);

        internal bool Activate(AbilityData ability, int level) {
            var receipt = HavockerSpellburn0020.Receipt(ability);
            if (receipt == null || receipt.Paid || receipt.Busy || !ReferenceEquals(receipt.Owner, Owner.Descriptor)) return false;
            if (!CanActivate(ability, level) || !HavockerSpellburn0020.SourceSlot(ability, level, out var book, out var slot)) return false;
            if (Data.Status != HavockerChargeStatus0020.Absent) Clear(false);
            receipt.Busy = true;
            try {
                // Native Spellbook.Spend compares AbilityData by value and may
                // choose another identical prepared copy. Spend the validated
                // real slot with its native primitive instead of that fallback.
                var now = HavockerSpellburn0020.Now; slot.Spend();
                if (slot.Available) throw new InvalidOperationException("Spellburn native prepared-slot payment did not commit.");
                receipt.Paid = true;
                Data.Sequence++; Data.ChargeId = Data.Sequence; Data.SlotLevel = level; Data.Credit = KineticistClassBridge0020.SpellburnCredit(level);
                Data.Expires = now + TimeSpan.FromSeconds(6); Data.SourceBook = book.Blueprint.AssetGuid.ToString(); Data.SourceSpell = slot.SpellShell.Blueprint.AssetGuid.ToString(); Data.SourceSlot = slot.Index;
                Data.Status = HavockerChargeStatus0020.Armed;
                Data.Display = Owner.Buffs.AddBuff(ChargeBuffs[level].Get(), Owner, TimeSpan.FromSeconds(6), AbilityParams.Empty);
                return true;
            } finally { receipt.Busy = false; }
        }

        internal bool OwnReceipt(HavockerSpellburn0020.CastRecord receipt) => OwnPart && ReferenceEquals(receipt?.State, Data);
        internal int Discount(ref KineticistAbilityBurnCost cost) {
            if (!OwnPart) return 0;
            cost.Decrease(KineticistClassBridge0020.Specialization(Owner.Progression.GetClassLevel(ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(KineticistClassBridge0020.WitchId))), KineticistBurnType.Infusion);
            int remaining = Math.Max(0, cost.InfusionBase + cost.InfusionIncrease - cost.InfusionDecrease);
            var frame = HavockerSpellburn0020.CurrentPayment;
            bool reservedForThis = Data.Status == HavockerChargeStatus0020.Reserved && ReferenceEquals(frame?.State, Data) && frame.ChargeId == Data.ChargeId;
            int discount = remaining > 0 && (Data.Status == HavockerChargeStatus0020.Armed || reservedForThis) && Data.Expires > HavockerSpellburn0020.Now ? Math.Min(remaining, Data.Credit) : 0;
            cost.Decrease(discount, KineticistBurnType.Infusion); return discount;
        }

        internal void Spend(HavockerBlastCost0020 component, AbilityData ability) {
            if (!OwnPart) throw new InvalidOperationException("Havocker kinetic ownership is invalid.");
            var receipt = HavockerSpellburn0020.Receipt(ability);
            if (receipt == null) throw new InvalidOperationException("Havocker resource commitment has no native cast context.");
            if (receipt.Paid) return;
            if (receipt.Busy) throw new InvalidOperationException("Reentrant Havocker resource commitment.");
            var cost = component.CalculateBurnCost(ability);
            if (cost.Total != 0) throw new InvalidOperationException("Havocker cannot accept positive burn.");
            // Reconstruct the pre-Spellburn infusion remainder from the raw
            // native cost result by calculating once with this charge hidden.
            var before = Data.Status; Data.Status = HavockerChargeStatus0020.Absent;
            KineticistAbilityBurnCost without;
            try { without = component.CalculateBurnCost(ability); } finally { Data.Status = before; }
            int infusion = Math.Max(0, without.InfusionBase + without.InfusionIncrease - without.InfusionDecrease);
            int used = Pending ? Math.Min(infusion, Data.Credit) : 0;
            receipt.State = Data; receipt.ChargeId = Data.ChargeId; receipt.Discount = used; receipt.Sequence = ++Data.Sequence; receipt.Busy = true;
            var previous = HavockerSpellburn0020.CurrentPayment; HavockerSpellburn0020.CurrentPayment = receipt;
            if (used > 0) Data.Status = HavockerChargeStatus0020.Reserved;
            try {
                component.NativeSpend(ability); // original AbilityKineticist.Spend, including its zero-burn event
                if (!receipt.NativeEvent) throw new InvalidOperationException("Native kinetic spend returned without the required accept-burn receipt.");
                HavockerBlade0020.RecordActivation(Owner, ability, receipt);
            } finally {
                HavockerSpellburn0020.CurrentPayment = previous; receipt.Busy = false;
                if (Data.Status == HavockerChargeStatus0020.Reserved) Data.Status = HavockerChargeStatus0020.Armed;
            }
        }
        public void HandleKineticistAcceptBurn(UnitPartKineticist part, int burn, AbilityData ability) {
            var receipt = HavockerSpellburn0020.CurrentPayment;
            if (!OwnPart || part?.Owner != Owner.Descriptor || receipt == null || receipt.Paid || !ReferenceEquals(receipt.State, Data) || !ReferenceEquals(receipt.Owner, Owner.Descriptor) || !ReferenceEquals(receipt.Context.Ability, ability)) return;
            receipt.NativeEvent = true; receipt.Paid = true; Data.NativeAcceptEvents++;
            if (receipt.Discount <= 0 || receipt.ChargeId != Data.ChargeId) return;
            Data.Status = HavockerChargeStatus0020.Consumed; // revoke discount BEFORE trying Buff.Remove
            ClearDisplay();
        }
        void ClearDisplay() {
            var buff = Data.Display.Fact;
            if (buff == null || buff.IsDisposed) { Data.Display = default(EntityFactRef<Buff>); return; }
            try { buff.Remove(); Data.Display = default(EntityFactRef<Buff>); }
            catch (Exception error) {
                if (!Data.PendingDisplays.Contains(Data.Display)) Data.PendingDisplays.Add(Data.Display);
                Data.Display = default(EntityFactRef<Buff>);
                ModEntry.LogSpellDiagnostic("Havocker consumed charge display cleanup failed; retry pending: " + error);
            }
        }
        void RetryDisplays() {
            foreach (var reference in Data.PendingDisplays.ToArray()) {
                try { var buff = reference.Fact; if (buff != null && !buff.IsDisposed) buff.Remove(); Data.PendingDisplays.Remove(reference); }
                catch (Exception error) { ModEntry.LogSpellDiagnostic("Havocker pending display cleanup retry failed: " + error); }
            }
        }
        internal void Clear(bool gather) {
            if (Data.Clearing) return; Data.Clearing = true;
            try {
                Data.Status = HavockerChargeStatus0020.Consumed; ClearDisplay();
                if ((Data.Display.Fact == null || Data.Display.Fact.IsDisposed) && Data.PendingDisplays.Count == 0) Data.Status = HavockerChargeStatus0020.Absent;
                if (gather) {
                    HavockerBlade0020.Clear(Owner);
                    // These are our private IDs even if the native part was
                    // already removed by another component in this fact.
                    foreach (var reference in GatherBuffs ?? Array.Empty<BlueprintBuffReference>()) Owner.Buffs.GetBuff(reference.Get())?.Remove();
                    var part = Owner.Get<UnitPartKineticist>();
                    if (part != null && Get(part, "m_Settings") is HavockerOwnedKineticistPart0020 settings) {
                        part.ClearAcceptedBurn();
                    }
                }
            } finally { Data.Clearing = false; }
        }
        public void HandleUnitRest(UnitEntityData unit) { if (unit == Owner) Clear(true); }
        public void OnNewRound() {
            RetryDisplays();
            if (Data.Status == HavockerChargeStatus0020.Consumed) Clear(false);
            else if (Data.Status != HavockerChargeStatus0020.Absent && !Pending) Clear(false);
        }
        public void HandleBuffDidAdded(Buff buff) { }
        public void HandleBuffDidRemoved(Buff buff) {
            if (!Data.Clearing && Data.Status == HavockerChargeStatus0020.Armed && ReferenceEquals(Data.Display.Fact, buff)) Clear(false);
        }
        protected override void OnApplyPostLoadFixes() {
            // A completed activation receipt is runtime-only. Do not restore a
            // blade-ready marker without its payment identity after loading.
            HavockerBlade0020.Clear(Owner);
            if (Data.Status == HavockerChargeStatus0020.Absent) return;
            if (Data.Status != HavockerChargeStatus0020.Armed || Data.SlotLevel < 2 || Data.SlotLevel > 9 || Data.Credit != KineticistClassBridge0020.SpellburnCredit(Data.SlotLevel) || !OwnPart || Data.Expires <= HavockerSpellburn0020.Now || HavockerSpellburn0020.BoundBook(Owner) == null) { Clear(false); return; }
            if (Data.Display.Fact == null || Data.Display.Fact.IsDisposed)
                Data.Display = Owner.Buffs.AddBuff(ChargeBuffs[Data.SlotLevel].Get(), Owner, Data.Expires - HavockerSpellburn0020.Now, AbilityParams.Empty);
        }
        protected override void OnDeactivate() { if (Owner?.Facts != null && !Owner.Facts.List.Contains(Fact)) Clear(true); }
        public void OnEventAboutToTrigger(RuleCastSpell evt) { }
        public void OnEventDidTrigger(RuleCastSpell evt) { if (evt.Initiator == Owner && (HavockerSpellburn0020.OwnBlast(evt.Spell.Blueprint) || HavockerSpellburn0020.OwnActivation(evt.Spell.Blueprint))) HavockerSpellburn0020.Observe(evt); }
    }

    [TypeId("cfbf049de70d8bbef2a69ca1df9ae6c7")]
    public sealed class HavockerSpellburnActivation0020 : BlueprintComponent, IAbilityRestriction {
        public int SlotLevel;
        public bool IsAbilityRestrictionPassed(AbilityData ability) => !HavockerSpellburn0020.OwnActivation(ability?.Blueprint) || HavockerSpellburn0020.With(HavockerSpellburn0020.DeclaredCaster(ability), c => c.CanActivate(ability, SlotLevel));
        public string GetAbilityRestrictionUIText() => "Requires an available prepared witch spell and no pending Spellburn charge.";
    }
    [TypeId("669f27f6378f76fb90fbc48268fceb18")]
    public sealed class HavockerBlastCost0020 : AbilityKineticist {
        public BlueprintGuid Original;
        public override bool IsAbilityRestrictionPassed(AbilityData ability) => !HavockerSpellburn0020.OwnBlast(ability?.Blueprint) ? base.IsAbilityRestrictionPassed(ability) : HavockerSpellburn0020.With(ability?.Caster, c => c.OwnPart && CalculateBurnCost(ability).Total == 0);
        public override void Spend(AbilityData ability) {
            if (!HavockerSpellburn0020.OwnBlast(ability?.Blueprint)) { base.Spend(ability); return; }
            if (!HavockerSpellburn0020.With(ability?.Caster, c => { c.Spend(this, ability); return true; })) throw new InvalidOperationException("Havocker kinetic bridge missing.");
        }
        internal void NativeSpend(AbilityData ability) => base.Spend(ability);
    }
    [TypeId("a5fa388cb1628ef38c59b7d35ac36d33")]
    public sealed class HavockerPreparedConversion0020 : SpontaneousSpellConversion {
        string OwnListId => Fact.Blueprint.AssetGuid + "#" + name;
        protected override void OnTurnOn() {
            var book = HavockerSpellburn0020.BoundBook(Owner);
            if (book == null || !KineticistClassBridge0020.CanUseBridge(Owner)) return;
            var list = SpellsByLevel.ToArray();
            var sources = (Dictionary<string, BlueprintAbility[]>)Get(book, "m_SpellConversionLists");
            if (sources.TryGetValue(OwnListId, out var existing)) {
                if (!existing.SequenceEqual(list)) throw new InvalidOperationException("Havocker conversion source identity occupied; existing list preserved.");
                return;
            }
            book.AddSpellConversionList(OwnListId, list);
        }
        protected override void OnTurnOff() { foreach (var book in Owner.Spellbooks.Where(b => ReferenceEquals(b.Owner, Owner.Descriptor))) book.RemoveSpellConversionList(OwnListId); }
    }
    [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.Spend))]
    internal static class HavockerSpellburnPay0020Patch {
        static bool Prefix(AbilityData __instance) {
            var marker = __instance?.Blueprint.GetComponent<HavockerSpellburnActivation0020>();
            if (marker == null || !HavockerSpellburn0020.OwnActivation(__instance.Blueprint)) return true;
            HavockerSpellburn0020.With(HavockerSpellburn0020.DeclaredCaster(__instance), c => c.Activate(__instance, marker.SlotLevel)); return false;
        }
    }
    [HarmonyPatch(typeof(AbilityKineticist), nameof(AbilityKineticist.CalculateBurnCost), typeof(UnitDescriptor), typeof(BlueprintAbility))]
    internal static class HavockerBurnCost0020Patch {
        static void Postfix(UnitDescriptor __0, BlueprintAbility __1, ref KineticistAbilityBurnCost __result) {
            if (!HavockerSpellburn0020.OwnBlast(__1)) return;
            var cost = __result;
            HavockerSpellburn0020.With(__0, c => c.Discount(ref cost)); __result = cost;
        }
    }
}
