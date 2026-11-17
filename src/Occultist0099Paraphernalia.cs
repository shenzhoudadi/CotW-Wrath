using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099Paraphernalia
    {
        private const string ElvenMagic = "55edf82380a1c8540af6c6037d34f322";
        private const string BasicFeatSelection = "247a4068296e8be42890143f451b4b45";
        private const string WizardFeatSelection = "8c3102c2ff3b69444b139a98521a4899";
        private const string HeightenSpell = "2f5d1e705c7967546b72ad8218ccf99c";
        internal const ActivatableAbilityGroup MetamagicMasterGroup = (ActivatableAbilityGroup)0x00990001;
        internal const ActivatableAbilityGroup SpellPowerGroup = (ActivatableAbilityGroup)0x00990002;
        // The audited native Wizard feat pool. Every glyph is an original game
        // identity; none is reused as a private Wrath blueprint id.
        private static readonly string[] MetamagicFeats =
        {
            "fbf5d9ce931f47f3a0c818b3f8ef8414", // Bolstered
            "094b6278f7b570f42aeaa98379f07cf2", // Completely Normal
            "a1de1e4f92195b442adb946f0e2b9d4e", // Empower
            "f180e72e4a9cbaa4da8be9bc958132ef", // Extend
            HeightenSpell,
            "7f2b282626862e345935bbea5e66424b", // Maximize
            "cd26b9fa3f734461a0fcedc81cafaaac", // Persistent
            "ef7ece7bb5bb66a41b256976b27f424e", // Quicken
            "46fad72f54a33dc4692d3b62eca7bb78", // Reach
            "85f3340093d144dd944fff9a9adfd2f2", // Selective
            "8ad7fd39abea4722b39eb5a67d606a41", // Intensified
            "c101ad6879a94204a780506f7a554865"  // Piercing
        };
        internal static readonly string[] Donors = new[] { ElvenMagic, BasicFeatSelection, WizardFeatSelection }
            .Concat(MetamagicFeats).Distinct().ToArray();
        private static readonly string[] MetaTokens =
        {
            "Bolstered", "CompletelyNormal", "Empower", "Extend", "Maximize",
            "Persistent", "Quicken", "Reach", "Selective", "Intensified", "Piercing"
        };
        internal static readonly string[] ActiveEffectTokens =
            new[] { 2, 3, 4 }.Select(cost => "SpellPower" + cost + "Buff")
                .Concat(MetaTokens.Select(meta => "MetamagicMaster" + meta + "Buff")).ToArray();
        internal static readonly string[] Tokens = new[] { "ScholarlyKnowledgeValue",
            Occultist0099Panoplies.ParaphernaliaToken + "ResonanceBuff", "SpellPowerFeature",
            "MetamagicMasterFeature", "MetamagicMasterPower", "MetamagicKnowledgeSelection",
            Occultist0099Panoplies.ParaphernaliaToken + "Source" }
            .Concat(new[] { 2, 3, 4 }.SelectMany(cost => new[]
            {
                "SpellPower" + cost + "Buff", "SpellPower" + cost + "Toggle"
            }))
            .Concat(MetaTokens.SelectMany(meta => new[]
            {
                "MetamagicMaster" + meta + "Buff", "MetamagicMaster" + meta + "Toggle"
            })).ToArray();

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache,
            Func<string, string, string, LocalizedString> text, BlueprintUnitProperty level, BlueprintUnitProperty dc)
        {
            var elven = Occultist0099Illusion.Load<BlueprintFeature>(cache, ElvenMagic);
            var icon = elven == null ? null : new BlueprintAbility { name = "Occultist0099ElvenMagicIconDonor",
                ComponentsArray = Array.Empty<BlueprintComponent>() };
            if (icon != null) Set(icon, "m_Icon", Get(elven, "m_Icon"));
            var basic = Occultist0099Illusion.Load<BlueprintFeatureSelection>(cache, BasicFeatSelection);
            var wizard = Occultist0099Illusion.Load<BlueprintFeatureSelection>(cache, WizardFeatSelection);
            var feats = MetamagicFeats.Select(id => Occultist0099Illusion.Load<BlueprintFeature>(cache, id)).ToArray();
            if (icon == null || basic == null || wizard == null || feats.Any(feat => feat == null) ||
                feats.Any(feat => feat.GetComponent<AddMetamagicFeat>() == null)) return null;

            const string scholarlyText = "You gain a +1 bonus on all Knowledge and Lore checks for every 4 points of mental focus invested in Divination, Evocation and Necromancy, up to half your occultist level.";
            var value = Occultist0099Panoplies.ResonanceValue("ScholarlyKnowledgeValue",
                Occultist0099Panoplies.Paraphernalia, 4, true);
            var rank = new ContextRankConfig { name = "ScholarlyKnowledgeRank" };
            SetEnum(rank, "m_BaseValueType", "CustomProperty");
            Set(rank, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(value));
            var scholarly = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                ComponentsArray = new[] { rank }.Concat(new[]
                {
                    StatType.SkillKnowledgeArcana, StatType.SkillKnowledgeWorld,
                    StatType.SkillLoreNature, StatType.SkillLoreReligion
                }.Select(stat => (BlueprintComponent)new AddContextStatBonus
                {
                    name = "ScholarlyKnowledge" + stat, Stat = stat,
                    Descriptor = ModifierDescriptor.UntypedStackable,
                    Value = new ContextValue { ValueType = ContextValueType.Rank }
                })).ToArray() },
                Occultist0099Panoplies.ParaphernaliaToken + "ResonanceBuff", "Scholarly Knowledge", scholarlyText, icon, text);

            const string powerText = "Arm a spell-power option. The next qualifying spell cast from your private occultist spellbook costs 2, 3 or 4 mental focus and gains +2, +3 or +4 caster level. The +3 and +4 options open at occultist level 12 and 18.";
            var power = Identify(new BlueprintFeature { Ranks = 1, IsClassFeature = true,
                ComponentsArray = Array.Empty<BlueprintComponent>() }, "SpellPowerFeature", "Spell Power",
                powerText, icon, text);
            var powerToggles = new List<BlueprintUnitFact>();
            var powerAssets = new List<BlueprintScriptableObject>();
            foreach (var tier in new[] { new { Cost = 2, Minimum = 1 }, new { Cost = 3, Minimum = 12 }, new { Cost = 4, Minimum = 18 } })
            {
                var buff = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                    ComponentsArray = new BlueprintComponent[]
                    {
                        new Occultist0099SpellPower { name = "OccultistSpellPower" + tier.Cost,
                            Cost = tier.Cost, Bonus = tier.Cost, MinimumLevel = tier.Minimum }
                    } }, "SpellPower" + tier.Cost + "Buff", "Spell Power (+" + tier.Cost + ")",
                    powerText, icon, text);
                var gate = new Occultist0099SpellPowerAvailable { name = "OccultistSpellPowerGate" + tier.Cost, MinimumLevel = tier.Minimum };
                var toggle = Identify(new BlueprintActivatableAbility
                {
                    ActivationType = AbilityActivationType.Immediately, DeactivateImmediately = true,
                    DoNotTurnOffOnRest = true, Group = SpellPowerGroup,
                    ComponentsArray = new BlueprintComponent[] { gate }
                }, "SpellPower" + tier.Cost + "Toggle", "Spell Power (+" + tier.Cost + ")",
                    "Spend " + tier.Cost + " points of mental focus when you cast a qualifying spell from your private occultist spellbook to increase its caster level by " + tier.Cost + ".", icon, text);
                Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(buff));
                powerToggles.Add(toggle);
                powerAssets.Add(buff);
                powerAssets.Add(toggle);
            }
            power.ComponentsArray = new BlueprintComponent[] { Facts(powerToggles.Distinct().ToArray()) };

            const string knowledgeText = "You receive one metamagic feat from the native Wizard feat pool. You cannot select this knowledge more than once.";
            var knowledge = Identify(new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true,
                Group = FeatureGroup.None, ComponentsArray = Array.Empty<BlueprintComponent>() },
                "MetamagicKnowledgeSelection", "Metamagic Knowledge", knowledgeText, icon, text);
            var allMeta = feats.Distinct().ToArray();
            Set(knowledge, "m_Features", allMeta.Select(feat => Ref<BlueprintFeatureReference>(feat)).ToArray());
            Set(knowledge, "m_AllFeatures", allMeta.Select(feat => Ref<BlueprintFeatureReference>(feat)).ToArray());
            var once = new PrerequisiteNoFeature { name = "OccultistMetamagicKnowledgeOnce" };
            Set(once, "m_Feature", Ref<BlueprintFeatureReference>(knowledge));
            knowledge.ComponentsArray = new BlueprintComponent[] { once };

            const string masterText = "When you cast a qualifying spell from your private occultist spellbook, you may apply a metamagic feat you know without changing its slot. It costs mental focus equal to that feat's normal increase in spell level.";
            var master = Identify(new BlueprintFeature { Ranks = 1, IsClassFeature = true,
                ComponentsArray = Array.Empty<BlueprintComponent>() }, "MetamagicMasterFeature",
                "Metamagic Master", masterText, icon, text);
            var masterToggles = new List<BlueprintUnitFact>();
            var masterAssets = new List<BlueprintScriptableObject>();
            foreach (var feat in feats)
            {
                var meta = feat.GetComponent<AddMetamagicFeat>().Metamagic;
                if (meta == Metamagic.Heighten) continue;
                int cost = Math.Max(0, MetamagicHelper.DefaultCost(meta));
                var name = "MetamagicMaster" + meta;
                var buff = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                    ComponentsArray = new BlueprintComponent[]
                    {
                        new Occultist0099MetamagicMaster { name = name + "Buff", Feat = Ref<BlueprintFeatureReference>(feat),
                            Meta = meta, Cost = cost }
                    } }, name + "Buff", "Metamagic Master: " + meta, masterText, icon, text);
                var known = new Occultist0099HasMetamagicFeat { name = name + "Known", Feat = Ref<BlueprintFeatureReference>(feat) };
                var toggle = Identify(new BlueprintActivatableAbility
                {
                    ActivationType = AbilityActivationType.Immediately, DeactivateImmediately = true,
                    DoNotTurnOffOnRest = true, Group = MetamagicMasterGroup,
                    ComponentsArray = new BlueprintComponent[] { known }
                }, name + "Toggle", "Metamagic Master: " + meta, masterText, icon, text);
                Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(buff));
                masterToggles.Add(toggle);
                masterAssets.Add(buff);
                masterAssets.Add(toggle);
            }
            master.ComponentsArray = new BlueprintComponent[] { Facts(masterToggles.Distinct().ToArray()) };

            var source = Occultist0099Panoplies.Carrier(Occultist0099Panoplies.Paraphernalia,
                Occultist0099Panoplies.ParaphernaliaToken, scholarlyText, icon, text, power);
            return new BlueprintScriptableObject[] { value, scholarly, knowledge, power, master, source }
                .Concat(powerAssets).Concat(masterAssets).ToArray();
        }

        internal static bool Eligible(AbilityData spell, RuleCastSpell rule)
        {
            if (spell?.Blueprint == null || (!spell.Blueprint.IsSpell && spell.ConvertedFrom?.Blueprint?.IsSpell != true) ||
                spell.SourceItem != null || spell.StickyTouch != null ||
                !Occultist0099NativeScope.IsPrivateBook(spell.Spellbook) || spell.IsChildSpell) return false;
            return rule == null || (!rule.IsDuplicateSpellApplied && rule.Context?.IsDuplicateSpellApplied != true);
        }

        private sealed class ReferenceAbility : IEqualityComparer<AbilityData>
        {
            internal static readonly ReferenceAbility Instance = new ReferenceAbility();
            public bool Equals(AbilityData a, AbilityData b) => ReferenceEquals(a, b);
            public int GetHashCode(AbilityData a) => RuntimeHelpers.GetHashCode(a);
        }

        private sealed class MetamagicMask
        {
            internal Metamagic Value;
        }

        private sealed class CommandPlan
        {
            internal UnitCommand.CommandType Type;
            internal Metamagic Value;
        }

        [ThreadStatic] private static HashSet<AbilityData> Checking;
        [ThreadStatic] private static HashSet<AbilityData> Queries;

        private static readonly ConditionalWeakTable<RuleCastSpell, object> ZeroCostExecutions =
            new ConditionalWeakTable<RuleCastSpell, object>();

        private const Metamagic CommandMetamagic = Metamagic.Quicken | Metamagic.Reach;

        private static readonly ConditionalWeakTable<UnitUseAbility, CommandPlan> CommandPlans =
            new ConditionalWeakTable<UnitUseAbility, CommandPlan>();
        private static readonly ConditionalWeakTable<RuleCastSpell, CommandPlan> BoundPlans =
            new ConditionalWeakTable<RuleCastSpell, CommandPlan>();
        private static readonly ConditionalWeakTable<AbilityExecutionContext, MetamagicMask> PaidContexts =
            new ConditionalWeakTable<AbilityExecutionContext, MetamagicMask>();

        internal static void RememberCommand(UnitUseAbility command, Metamagic preview)
        {
            if (!Eligible(command.Ability, null)) return;
            var mask = preview & CommandMetamagic;
            if (command.Type != UnitCommand.CommandType.Swift)
                mask &= ~Metamagic.Quicken;
            CommandPlans.Add(command, new CommandPlan { Type = command.Type, Value = mask });
        }

        internal static void BindCommand(RuleCastSpell rule, UnitUseAbility command)
        {
            if (CommandPlans.TryGetValue(command, out var plan))
                BoundPlans.GetValue(rule, _ => plan);
        }

        internal static void Restore(MechanicsContext context)
        {
            var root = context?.SourceAbilityContext ?? context as AbilityExecutionContext;
            if (root == null || context.Params == null || !PaidContexts.TryGetValue(root, out var mask) ||
                (context.Params.Metamagic & mask.Value) == mask.Value) return;
            var copy = context.Params.Clone();
            copy.Metamagic |= mask.Value;
            context.SetParams(copy);
        }

        internal static bool HasExistingMetamagic(AbilityData spell, Metamagic meta)
        {
            if (spell == null || meta == 0) return false;
            if (Checking == null) Checking = new HashSet<AbilityData>(ReferenceAbility.Instance);
            if (!Checking.Add(spell)) return false;
            try
            {
                if (spell.MetamagicData != null && spell.MetamagicData.Has(meta)) return true;
                if (((spell.CasterUnitPartSpecialMetamagic?.GetMetamagic(spell) ?? 0) & meta) == meta) return true;
                return spell.ConvertedFrom != null && HasExistingMetamagic(spell.ConvertedFrom, meta);
            }
            finally { Checking.Remove(spell); }
        }

        private static bool CanPayPlan(Occultist0099FocusFact0099 focus, UnitEntityData unit, int totalCost, string identity)
            => totalCost > 0
                ? focus.CanPayPanoply(unit, Occultist0099Panoplies.Paraphernalia, totalCost, identity)
                : focus.IsOwnedBy(unit) && focus.OwnsPanoply(Occultist0099Panoplies.Paraphernalia) &&
                  focus.IsLocked() && Occultist0099Focus.PanoplyResonance(
                      unit.Get<Occultist0099FocusPart>()?.State, Occultist0099Panoplies.Paraphernalia) > 0;

        internal static int CollectCastPlan(UnitEntityData unit, AbilityData spell, int actualLevel,
            out Occultist0099SpellPower spellPower, List<Occultist0099MetamagicMaster> metamagics,
            Metamagic nativeMetamagic = 0, bool allowQuicken = true)
        {
            spellPower = null;
            metamagics?.Clear();
            int totalCost = 0;
            if (unit == null) return 0;
            foreach (var buff in unit.Buffs.Enumerable.Where(b => b.IsActive && b.IsTurnedOn && !b.IsSuppressed))
            {
                var power = buff.Blueprint.GetComponent<Occultist0099SpellPower>();
                if (power != null && power.MinimumLevel <= actualLevel &&
                    (spellPower == null || power.Bonus > spellPower.Bonus))
                    spellPower = power;
                var meta = buff.Blueprint.GetComponent<Occultist0099MetamagicMaster>();
                if (meta != null && (nativeMetamagic & meta.Meta) == 0 && meta.Applies(unit, spell, allowQuicken) &&
                    (metamagics == null || !metamagics.Any(x => x.Meta == meta.Meta)))
                {
                    metamagics?.Add(meta);
                    totalCost += meta.Cost;
                }
            }
            if (spellPower != null) totalCost += spellPower.Cost;
            return totalCost;
        }

        internal static Metamagic PreviewMetamagic(AbilityData spell)
        {
            Metamagic paid = 0;
            var activeRoot = ContextData<MechanicsContext.Data>.Current?.Context?.SourceAbilityContext;
            if (activeRoot != null && ReferenceEquals(activeRoot.Ability, spell) &&
                PaidContexts.TryGetValue(activeRoot, out var recorded))
                paid = recorded.Value;
            if (spell == null || Checking?.Contains(spell) == true) return paid;
            if (Queries == null) Queries = new HashSet<AbilityData>(ReferenceAbility.Instance);
            if (!Queries.Add(spell)) return paid;
            try
            {
                if (!Eligible(spell, null)) return paid;
                var unit = spell.Caster?.Unit;
                var runtime = Occultist0099NativeScope.RuntimeOf(unit, Occultist0099Ids.FocusFactGuid,
                    typeof(Occultist0099FocusFact0099));
                if (runtime == null) return paid;
                return Occultist0099NativeScope.InRuntimeContext(runtime, component =>
                {
                    var focus = (Occultist0099FocusFact0099)component;
                    var metamagics = new List<Occultist0099MetamagicMaster>();
                    int totalCost = CollectCastPlan(unit, spell, focus.ActualLevel, out _, metamagics);
                    if (metamagics.Count == 0 || !CanPayPlan(focus, unit, totalCost, null)) return paid;
                    Metamagic mask = paid;
                    foreach (var meta in metamagics) mask |= meta.Meta;
                    return mask;
                }, paid);
            }
            finally { Queries.Remove(spell); }
        }

        // Called from the one native RuleCastSpell payment bridge. All three
        // gates are checked before any balance changes: private book, known and
        // applicable metamagic, and a single atomic Panoply payment plan.
        // Only native metamagic is present in Params at this point, including
        // one-shot effects that already removed their buffs.
        internal static bool TryCast(RuleCastSpell rule)
        {
            if (rule == null || rule.Context?.IsForced == true) return true;
            bool bound = BoundPlans.TryGetValue(rule, out var plan);
            bool canCastWithoutPlan = !bound || plan.Value == 0;
            var spell = rule.Spell;
            if (!Eligible(spell, rule) || rule.Context?.Params == null) return canCastWithoutPlan;
            if (PaidContexts.TryGetValue(rule.Context, out _)) return false;
            var unit = rule.Initiator;
            var runtime = Occultist0099NativeScope.RuntimeOf(unit, Occultist0099Ids.FocusFactGuid,
                typeof(Occultist0099FocusFact0099));
            if (runtime == null) return canCastWithoutPlan;

            string identity = Occultist0099NativeCast.IdentityOf(rule) + ":paraphernalia";
            Occultist0099SpellPower spellPower = null;
            var metamagics = new List<Occultist0099MetamagicMaster>();
            int totalCost = 0;
            Metamagic nativeMetamagic = rule.Context.Params.Metamagic;
            Metamagic mask = 0;
            bool allowQuicken = !bound || plan.Type == UnitCommand.CommandType.Swift;
            bool eligible = Occultist0099NativeScope.InRuntimeContext(runtime, component =>
            {
                var focus = (Occultist0099FocusFact0099)component;
                totalCost = CollectCastPlan(unit, spell, focus.ActualLevel, out spellPower, metamagics, nativeMetamagic, allowQuicken);
                foreach (var meta in metamagics) mask |= meta.Meta;
                // The command has already chosen its action type and any Reach delivery.
                if (bound && (mask & CommandMetamagic) != (plan.Value & ~nativeMetamagic))
                    return false;
                if (spellPower == null && metamagics.Count == 0) return true;
                return CanPayPlan(focus, unit, totalCost, identity) &&
                       (totalCost > 0 || !ZeroCostExecutions.TryGetValue(rule, out _));
            }, false);
            if (!eligible) return false;
            if (spellPower == null && metamagics.Count == 0) return true;

            bool paid = Occultist0099NativeScope.InRuntimeContext(runtime, component =>
            {
                var focus = (Occultist0099FocusFact0099)component;
                if (totalCost > 0)
                {
                    if (!focus.TryPayPanoply(unit, Occultist0099Panoplies.Paraphernalia, totalCost, identity).Applied)
                        return false;
                    focus.SyncResourceMirror();
                    return true;
                }
                if (ZeroCostExecutions.TryGetValue(rule, out _)) return false;
                ZeroCostExecutions.Add(rule, new object());
                return true;
            }, false);
            if (!paid) return false;
            if (metamagics.Count == 0) return true;
            PaidContexts.GetOrCreateValue(rule.Context).Value |= mask;
            Restore(rule.Context);
            return true;
        }
    }

    [TypeId(Occultist0099Ids.SpellPowerTypeId)]
    public sealed class Occultist0099SpellPower : UnitBuffComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>
    {
        public int Cost;
        public int Bonus;
        public int MinimumLevel;

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (evt == null || Buff == null || !Buff.IsActive || !Buff.IsTurnedOn || Buff.IsSuppressed ||
                Occultist0099AegisStoreSpell.IsStoring || !Occultist0099Paraphernalia.Eligible(evt.AbilityData, null)) return;
            var owner = Owner;
            var runtime = Occultist0099NativeScope.RuntimeOf(owner, Occultist0099Ids.FocusFactGuid,
                typeof(Occultist0099FocusFact0099));
            bool affordable = Occultist0099NativeScope.InRuntimeContext(runtime, component =>
            {
                var focus = (Occultist0099FocusFact0099)component;
                int totalCost = Occultist0099Paraphernalia.CollectCastPlan(owner, evt.AbilityData,
                    focus.ActualLevel, out var selected, new List<Occultist0099MetamagicMaster>());
                return ReferenceEquals(this, selected) &&
                    (totalCost == 0 || focus.CanPayPanoply(owner, Occultist0099Panoplies.Paraphernalia, totalCost, null));
            }, false);
            if (!affordable) return;
            evt.AddBonusCasterLevel(Bonus, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
    }

    [TypeId(Occultist0099Ids.SpellPowerAvailabilityTypeId)]
    public sealed class Occultist0099SpellPowerAvailable : ActivatableAbilityRestriction
    {
        public int MinimumLevel;

        protected override bool IsAvailable()
        {
            var state = Owner?.Get<Occultist0099FocusPart>()?.State;
            if (state == null || Occultist0099Focus.PanoplyResonance(state,
                    Occultist0099Panoplies.Paraphernalia) <= 0) return false;
            var runtime = Occultist0099NativeScope.RuntimeOf(Owner, Occultist0099Ids.FocusFactGuid,
                typeof(Occultist0099FocusFact0099));
            return Occultist0099NativeScope.InRuntimeContext(runtime,
                component =>
                {
                    var focus = (Occultist0099FocusFact0099)component;
                    return focus.OwnsPanoply(Occultist0099Panoplies.Paraphernalia) &&
                           focus.ActualLevel >= MinimumLevel;
                }, false);
        }
    }

    [TypeId(Occultist0099Ids.MetamagicMasterTypeId)]
    public sealed class Occultist0099MetamagicMaster : UnitBuffComponentDelegate
    {
        public BlueprintFeatureReference Feat;
        public Metamagic Meta;
        public int Cost;

        internal bool Applies(UnitEntityData unit, AbilityData spell, bool allowQuicken = true)
        {
            if (unit == null || spell?.Blueprint == null || !unit.HasFact(Feat?.Get())) return false;
            if (Meta == Metamagic.Quicken &&
                (!allowQuicken || unit.CombatState?.HasCooldownForCommand(UnitCommand.CommandType.Swift) == true))
                return false;
            var available = spell.Blueprint.AvailableMetamagic |
                            (spell.ConvertedFrom?.Blueprint?.AvailableMetamagic ?? 0);
            return (available & Meta) != 0 && !Occultist0099Paraphernalia.HasExistingMetamagic(spell, Meta);
        }
    }

    [TypeId(Occultist0099Ids.MetamagicMasterToggleTypeId)]
    public sealed class Occultist0099HasMetamagicFeat : ActivatableAbilityRestriction
    {
        public BlueprintFeatureReference Feat;

        protected override bool IsAvailable()
        {
            if (Owner == null || !Owner.HasFact(Feat?.Get())) return false;
            var state = Owner.Get<Occultist0099FocusPart>()?.State;
            if (state == null || Occultist0099Focus.PanoplyResonance(state,
                    Occultist0099Panoplies.Paraphernalia) <= 0) return false;
            var runtime = Occultist0099NativeScope.RuntimeOf(Owner, Occultist0099Ids.FocusFactGuid,
                typeof(Occultist0099FocusFact0099));
            return Occultist0099NativeScope.InRuntimeContext(runtime,
                component => ((Occultist0099FocusFact0099)component).OwnsPanoply(
                    Occultist0099Panoplies.Paraphernalia), false);
        }
    }

    [HarmonyPatch(typeof(UnitPartActivatableAbility), nameof(UnitPartActivatableAbility.GetGroupSize))]
    internal static class Occultist0099NativeMetamagicMasterGroup
    {
        private static bool Prefix(ActivatableAbilityGroup group, ref int __result)
        {
            // Native switching handles each private group. Its capacity array only has
            // stock enum indices, so our private groups report their fixed size here.
            if (group != Occultist0099Paraphernalia.MetamagicMasterGroup &&
                group != Occultist0099Paraphernalia.SpellPowerGroup) return true;
            __result = 1;
            return false;
        }
    }

    [HarmonyPatch(typeof(UnitUseAbility), MethodType.Constructor,
        new[] { typeof(UnitCommand.CommandType), typeof(AbilityData), typeof(TargetWrapper) })]
    internal static class Occultist0099NativeParaphernaliaCommand
    {
        private static void Prefix(AbilityData __1, out Metamagic __state)
            => __state = Occultist0099Paraphernalia.PreviewMetamagic(__1);

        private static void Postfix(UnitUseAbility __instance, Metamagic __state)
            => Occultist0099Paraphernalia.RememberCommand(__instance, __state);
    }
}
