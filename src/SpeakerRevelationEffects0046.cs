// COTW-0046 F-0046: the actual combat semantics of the 16 revelations.
// This file is a partial of SpeakerForThePast0046 (same module, same private
// asset identities). It deliberately adds no second isolation mechanism: all
// level/attribute/resource inputs still go through RevelationBridge0046.
//
// Every effect below is built from component types that are already used by
// this production project (see PsychicBattleSpells.cs, PsychicTemporalStasis.cs,
// PsychicWeaponSpells.cs, Inquisitions01428.cs, GraveWarden01445.cs). Native
// read-only leaf blueprints are referenced or cloned, never edited in place.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Properties;
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
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static partial class SpeakerForThePast0046
    {
        // Native read-only sources that require a mutable private copy. Each is
        // pre-validated in Register() before any blueprint is created, so none
        // of these can leave a dangling reference in a half-published archetype.
        internal const string TouchDeliveryId = "3c17035ec4717674cae2e841a190e757";
        internal const string TouchCastId = "ab395d2335d3f384e99dddee8562978f";
        internal const string TouchWeaponId = "bb337517547de1a4189518d404ec49d4";
        internal const string TransformationBuffId = "287682389d2011b41b5a65195d9cbc84";
        internal const string StasisBuffId = "8fff364a212d4b4894b5e222f3696cdd";
        internal const string DimensionDoorCasterOnlyId = "a9b8be9b87865744382f7c64e599aeb2";
        internal const string DimensionDoorMassId = "5bdc37e4acfa209408334326076a43bc";
        internal const string HasteId = "486eaff58293f6441a5c2759c4872f98";
        internal const string SlowId = "f492622e473d34747806bdb39356eb89";
        internal const string GhostTouchId = "47857e1a5a3ec1a46adf6491b1423b4f";
        internal const string KeenId = "102a9c8c9b7a75e4fb5844e79deaf4c0";

        // Non-fact native assets are pre-validated by GUID *and* expected CLR
        // type: Clone()/Native<T>() would otherwise throw on a mismatched donor
        // after the first blueprint was already staged. Loading happens later,
        // on demand, inside BuildEffects.
        internal static readonly (string Guid, Type Type)[] ExtraNativeIds = {
            (TouchDeliveryId, typeof(BlueprintAbility)),
            (TouchCastId, typeof(BlueprintAbility)),
            (TouchWeaponId, typeof(Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon)),
            (TransformationBuffId, typeof(BlueprintBuff)),
            (StasisBuffId, typeof(BlueprintBuff)),
            (DimensionDoorCasterOnlyId, typeof(BlueprintAbility)),
            (DimensionDoorMassId, typeof(BlueprintAbility)),
            (HasteId, typeof(BlueprintAbility)),
            (SlowId, typeof(BlueprintAbility)),
            (GhostTouchId, typeof(Kingmaker.Blueprints.Items.Ecnchantments.BlueprintWeaponEnchantment)),
            (KeenId, typeof(Kingmaker.Blueprints.Items.Ecnchantments.BlueprintWeaponEnchantment))
        };

        // Filled in by Register() so the B-stage check can prove that the real
        // native LearnSpellList type and its three wiring fields were resolved,
        // instead of silently swallowing a missing field.
        internal static string LearnSpellListReport = "not probed";

        static ContextValue R(AbilityRankType type = AbilityRankType.Default)
            => new ContextValue { ValueType = ContextValueType.Rank, ValueRank = type };

        static ContextRankConfig Ranking(RevelationBinding0046 bind,
            string progression = "AsIs", AbilityRankType type = AbilityRankType.Default,
            bool minOne = false, params int[] custom)
        {
            var r = RevelationBridge0046.Rank(bind, progression);
            Set(r, "m_Type", type);
            if (minOne) { Set(r, "m_UseMin", true); Set(r, "m_Min", 1); }
            if (progression == "Custom" && custom.Length >= 2)
            {
                var element = AccessTools.Field(r.GetType(), "m_CustomProgression").FieldType.GetElementType();
                var rows = Array.CreateInstance(element, custom.Length / 2);
                for (int i = 0; i + 1 < custom.Length; i += 2)
                {
                    var row = Activator.CreateInstance(element);
                    Set(row, "BaseValue", custom[i]);
                    Set(row, "ProgressionValue", custom[i + 1]);
                    rows.SetValue(row, i / 2);
                }
                Set(r, "m_CustomProgression", rows);
            }
            return r;
        }

        // Wisdom modifier as a rank value: max(1, WisMod) rounds for Blood of Heroes.
        static ContextRankConfig WisdomRounds(AbilityRankType type = AbilityRankType.DamageDice)
        {
            var r = new ContextRankConfig { name = "Speaker0046WisdomRounds" };
            SetEnum(r, "m_BaseValueType", "StatBonus");
            SetEnum(r, "m_Type", type.ToString());
            SetEnum(r, "m_Progression", "AsIs");
            Set(r, "m_Stat", StatType.Wisdom);
            Set(r, "m_UseMin", true); Set(r, "m_Min", 1);
            return r;
        }

        static Condition FactCond(BlueprintUnitFact fact)
        {
            var c = new ContextConditionHasFact { name = "Speaker0046Has" + fact.name };
            Set(c, "m_Fact", Ref<BlueprintUnitFactReference>(fact));
            return c;
        }

        static Conditional If(Condition condition, GameAction[] yes, GameAction[] no)
            => new Conditional {
                name = "Speaker0046If",
                ConditionsChecker = new ConditionsChecker { Conditions = new[] { condition } },
                IfTrue = Actions(yes), IfFalse = Actions(no)
            };

        static ContextActionDealDamage Divine(ContextValue dice, bool half)
            => new ContextActionDealDamage {
                name = "Speaker0046StormDivine",
                DamageType = new DamageTypeDescription { Type = DamageType.Energy, Energy = DamageEnergyType.Divine },
                Value = new ContextDiceValue { DiceType = DiceType.D8, DiceCountValue = dice, BonusValue = 0 },
                IsAoE = true, HalfIfSaved = half, Half = false, IgnoreCritical = true, DisableSneakDamage = true
            };

        static AbilityTargetsAround Burst(int feet, TargetType targets)
        {
            var b = new AbilityTargetsAround { name = "Speaker0046Burst" };
            Set(b, "m_Radius", new Feet(feet));
            Set(b, "m_TargetType", targets);
            Set(b, "m_Condition", new ConditionsChecker { Conditions = Array.Empty<Condition>() });
            return b;
        }

        static BlueprintBuff Bf(PsychicAssets f, string token, string name, string text,
            BlueprintUnitFact donor, params BlueprintComponent[] components)
        {
            var b = f.Buff(token, name, text, components);
            RevelationBridge0046.Icon(b, donor);
            return b;
        }

        static BlueprintComponent[] Core(RevelationBinding0046 bind, BlueprintAbilityResource resource)
        {
            var list = new List<BlueprintComponent>();
            if (resource != null) list.Add(ArchetypeBatch01445.Cost(resource));
            list.Add(RevelationBridge0046.Rank(bind));
            list.Add(RevelationBridge0046.Params(bind.PrimaryClass, bind.CastingStat));
            return list.ToArray();
        }

        static BlueprintAbility Ab(PsychicAssets f, RevelationBinding0046 bind,
            BlueprintAbilityResource resource, string token, string name, string text,
            UnitCommand.CommandType action, AbilityRange range, BlueprintUnitFact donor,
            params BlueprintComponent[] extra)
        {
            var a = f.Ability(token, name, text, AbilityType.Supernatural, action, range,
                Core(bind, resource).Concat(extra).ToArray());
            RevelationBridge0046.Icon(a, donor);
            return a;
        }

        static BlueprintActivatableAbility Toggle(PsychicAssets f, string token, string name, string text,
            BlueprintBuff buff, BlueprintUnitFact donor, BlueprintComponent[] components = null,
            UnitCommand.CommandType command = UnitCommand.CommandType.Free)
        {
            var t = f.Add(new BlueprintActivatableAbility {
                ActivationType = AbilityActivationType.WithUnitCommand,
                DeactivateIfCombatEnded = true,
                ComponentsArray = components ?? Array.Empty<BlueprintComponent>()
            }, token, name, text);
            Set(t, "m_ActivateWithUnitCommand", command);
            Set(t, "m_Buff", Ref<BlueprintBuffReference>(buff));
            Set(t, "m_Icon", Get(donor, "m_Icon"));
            return t;
        }

        static ActivatableAbilityResourceLogic RoundCost(BlueprintAbilityResource resource)
        {
            var c = new ActivatableAbilityResourceLogic {
                name = "Speaker0046RoundCost", SpendType = ActivatableAbilityResourceLogic.ResourceSpendType.NewRound
            };
            Set(c, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
            return c;
        }

        // The native held-charge touch pair. Casting pays exactly one resource;
        // the delivery ability holds the real effect and keeps the charge on a miss.
        static BlueprintAbility Touch(PsychicAssets f, string token, string name, string text,
            BlueprintUnitFact donor, BlueprintComponent[] delivery, params BlueprintComponent[] castExtra)
        {
            var touch = f.Clone(f.Native<BlueprintAbility>(TouchDeliveryId), token + "Touch", name, text);
            var deliver = new AbilityDeliverTouch { name = token + "TouchDelivery" };
            Set(deliver, "m_TouchWeapon", Ref<BlueprintItemWeaponReference>(f.Native<BlueprintItemWeapon>(TouchWeaponId)));
            touch.ComponentsArray = new BlueprintComponent[] { deliver }.Concat(delivery).ToArray();

            var cast = f.Clone(f.Native<BlueprintAbility>(TouchCastId), token, name, text);
            var sticky = new AbilityEffectStickyTouch { name = token + "HeldCharge" };
            Set(sticky, "m_TouchDeliveryAbility", Ref<BlueprintAbilityReference>(touch));
            cast.ComponentsArray = new BlueprintComponent[] { sticky }.Concat(castExtra).ToArray();

            foreach (var a in new[] { cast, touch })
            {
                Set(a, "m_Parent", new BlueprintAbilityReference());
                a.Type = AbilityType.Supernatural;
                a.Range = AbilityRange.Touch;
                a.CanTargetSelf = false; a.CanTargetFriends = false; a.CanTargetEnemies = true; a.CanTargetPoint = false;
                a.SpellResistance = false;
                a.EffectOnEnemy = a.EffectOnAlly = AbilityEffectOnUnit.Harmful;
                a.AvailableMetamagic = 0;
                a.MaterialComponent = new BlueprintAbility.MaterialComponentData();
                RevelationBridge0046.Icon(a, donor);
            }
            return cast;
        }

        static BlueprintAbility Variants(PsychicAssets f, RevelationBinding0046 bind,
            string token, string name, string text, UnitCommand.CommandType action, AbilityRange range,
            BlueprintUnitFact donor, params BlueprintAbility[] children)
        {
            var parent = f.Ability(token, name, text, AbilityType.Supernatural, action, range,
                new BlueprintComponent[] { RevelationBridge0046.Params(bind.PrimaryClass, bind.CastingStat), RevelationBridge0046.Rank(bind) });
            var v = new AbilityVariants { name = token + "Choices" };
            Set(v, "m_Variants", children.Select(Ref<BlueprintAbilityReference>).ToArray());
            parent.ComponentsArray = parent.ComponentsArray.Concat(new BlueprintComponent[] { v }).ToArray();
            foreach (var child in children) Set(child, "m_Parent", Ref<BlueprintAbilityReference>(parent));
            RevelationBridge0046.Icon(parent, donor);
            return parent;
        }

        internal static BlueprintFeature Hidden(PsychicAssets f, string token, string name, string text,
            BlueprintUnitFact donor, params BlueprintUnitFact[] facts)
        {
            var feature = f.Feature(token, name, text, Facts(facts));
            feature.IsClassFeature = true; feature.HideInUI = true; feature.HideInCharacterSheetAndLevelUp = true;
            RevelationBridge0046.Icon(feature, donor);
            return feature;
        }

        internal static ContextActionApplyBuff For(BlueprintBuff buff, int duration, DurationRate rate, ContextValue scaled = null)
        {
            var a = Apply(buff, scaled == null ? duration : 0, rate, true, false);
            a.AsChild = false;
            if (scaled != null) a.DurationValue.BonusValue = scaled;
            return a;
        }

        internal static BlueprintAbilityResource BuildResource(PsychicAssets f, RevelationBinding0046 bind,
            string token, ResCfg res)
        {
            var resource = f.Add(new BlueprintAbilityResource(), token, "", "");
            var amount = Get(resource, "m_MaxAmount");
            Set(amount, "BaseValue", res.Base);
            RevelationBridge0046.BindResource(amount, bind);
            if (res.ByStat)
            {
                Set(amount, "IncreasedByStat", true);
                Set(amount, "ResourceBonusStat", bind.CastingStat);
            }
            if (res.ByLevel)
            {
                Set(amount, "IncreasedByLevel", true);
                Set(amount, "LevelIncrease", res.LevelIncrease);
            }
            if (res.Step > 0)
            {
                Set(amount, "IncreasedByLevelStartPlusDivStep", true);
                Set(amount, "StartingLevel", res.Start);
                Set(amount, "StartingIncrease", 1);
                Set(amount, "LevelStep", res.Step);
                Set(amount, "PerStepIncrease", res.PerStep);
            }
            Set(resource, "m_MaxAmount", amount);
            return resource;
        }

        // ------------------------------------------------------------------
        // Shared effect builders usable by both Oracle Time Mystery (+Ancestral
        // Weapon) and Speaker for the Past's Sacred Council adapter patch.
        // ------------------------------------------------------------------
        internal static BlueprintComponent[] BuildTemporalCelerityEffects(PsychicAssets f, RevelationBinding0046 bind, BlueprintUnitFact donor, string prefix)
        {
            var low = InitiativeFeature(f, prefix + "Initiative2Feature0046", "Temporal Celerity (two dice)", 1);
            var high = InitiativeFeature(f, prefix + "Initiative3Feature0046", "Temporal Celerity (three dice)", 2);
            var surprise = f.Feature(prefix + "SurpriseRoundFeature0046", "Temporal Celerity (surprise round)",
                "From 7th level, you may act during a surprise round.",
                new SpeakerSurpriseRound0046 { name = prefix + "SurpriseRound" });
            surprise.IsClassFeature = true; surprise.HideInUI = true; surprise.HideInCharacterSheetAndLevelUp = true;
            RevelationBridge0046.Icon(surprise, donor);
            return new BlueprintComponent[] {
                RevelationBridge0046.At(bind, low, 11, true),
                RevelationBridge0046.At(bind, high, 11, false),
                RevelationBridge0046.At(bind, surprise, 7, false)
            };
        }

        static BlueprintFeature InitiativeFeature(PsychicAssets f, string token, string name, int extraRolls)
        {
            var reroll = new ModifyD20 {
                name = token + extraRolls,
                Rule = RuleType.Initiative, RollsAmount = extraRolls, TakeBest = true,
                ActionsToTrigger = Actions(), ActionsToTriggerBeforeRoll = Actions()
            };
            var feature = f.Feature(token, name, "Roll initiative more than once and take the best result.", reroll);
            feature.IsClassFeature = true; feature.HideInUI = true; feature.HideInCharacterSheetAndLevelUp = true;
            return feature;
        }

        internal static BlueprintComponent[] BuildEffects(PsychicAssets f, RevelationBinding0046 bind,
            Rev spec, BlueprintAbilityResource resource, Dictionary<string, BlueprintUnitFact> leaf)
        {
            var donor = leaf[IconDonorId];
            switch (spec.Token)
            {
                case string t when t.StartsWith("AgingTouch", StringComparison.Ordinal): return AgingTouch(f, bind, spec, resource, leaf, donor);
                case string t when t.StartsWith("RewindTime", StringComparison.Ordinal): return RewindTime(f, bind, spec, resource, donor);
                case string t when t.StartsWith("SacredCouncil", StringComparison.Ordinal): return SacredCouncil(f, bind, spec, resource, donor);
                case string t when t.StartsWith("SpeedOrSlowTime", StringComparison.Ordinal): return SpeedOrSlowTime(f, bind, spec, resource, donor);
                case string t when t.StartsWith("TimeFlicker", StringComparison.Ordinal): return TimeFlicker(f, bind, spec, resource, leaf, donor);
                case string t when t.StartsWith("TimeHop", StringComparison.Ordinal): return TimeHop(f, bind, spec, resource, donor);
                case string t when t.StartsWith("TimeSight", StringComparison.Ordinal): return TimeSight(f, bind, spec, resource, leaf, donor);
                case string t when t.StartsWith("EraseFromTime", StringComparison.Ordinal): return EraseFromTime(f, bind, spec, resource, leaf, donor);
                case string t when t.StartsWith("AncestralWeapon", StringComparison.Ordinal): return AncestralWeapon(f, bind, spec, resource, donor);
                default: throw new InvalidOperationException("COTW-0046: no shared effect builder for " + spec.Token);
            }
        }

        // 1. Aging Touch: constructs take Ld6 direct damage, undead are immune,
        //    living creatures take max(1, L/2) Strength damage via the native rule.
        static BlueprintComponent[] AgingTouch(PsychicAssets f, RevelationBinding0046 bind,
            Rev spec, BlueprintAbilityResource res, Dictionary<string, BlueprintUnitFact> leaf, BlueprintUnitFact donor)
        {
            var direct = new ContextActionDealDamage {
                name = "Speaker0046AgingConstructDamage",
                DamageType = new DamageTypeDescription { Type = DamageType.Direct },
                Value = new ContextDiceValue { DiceType = DiceType.D6, DiceCountValue = R(), BonusValue = 0 },
                IgnoreCritical = true, DisableSneakDamage = true
            };
            var wither = new SpeakerAgingTouch0046 { name = "Speaker0046AgingStrengthDamage" };
            var body = If(FactCond(leaf[ConstructTypeId]), new GameAction[] { direct },
                new GameAction[] { If(FactCond(leaf[UndeadTypeId]), Array.Empty<GameAction>(), new GameAction[] { wither }) });
            var delivery = new BlueprintComponent[] {
                Run(body), RevelationBridge0046.Rank(bind), RevelationBridge0046.Params(bind.PrimaryClass, bind.CastingStat)
            };
            var cast = Touch(f, spec.Token + "Ability", spec.Name, spec.Text, donor, delivery,
                ArchetypeBatch01445.Cost(res), RevelationBridge0046.Rank(bind),
                RevelationBridge0046.Params(bind.PrimaryClass, bind.CastingStat));
            return new BlueprintComponent[] { Facts(cast) };
        }

        // 2. Rewind Time: one swift-action 1-round buff (prepaid on cast, still
        //    eligible when remaining pool hits 0, removes itself after 1 failed reroll)
        //    plus six free reaction toggles (spend 1 only when triggering a reroll).
        static BlueprintComponent[] RewindTime(PsychicAssets f, RevelationBinding0046 bind,
            Rev spec, BlueprintAbilityResource res, BlueprintUnitFact donor)
        {
            var active = Bf(f, spec.Token + "Buff", spec.Name, spec.Text, donor,
                new SpeakerFailedReroll0046 {
                    name = "Speaker0046RewindActive", RerollOnlyIfFailed = true,
                    SpendOnTrigger = false, ConsumeBuffOnTrigger = true,
                    Resource = Ref<BlueprintAbilityResourceReference>(res)
                });
            var swift = Ab(f, bind, res, spec.Token + "Ability", spec.Name, spec.Text,
                UnitCommand.CommandType.Swift, AbilityRange.Personal, donor,
                Run(For(active, 1, DurationRate.Rounds)));
            swift.CanTargetSelf = true; swift.CanTargetFriends = false; swift.CanTargetEnemies = false;
            var parts = new List<BlueprintComponent> { Facts(swift) };
            foreach (var rule in SpeakerFailedReroll0046.AllRules)
            {
                string suffix = rule.ToString();
                var toggleBuff = Bf(f, spec.Token + suffix + "Buff", spec.Name + ": " + suffix, spec.Text, donor,
                    new SpeakerFailedReroll0046 {
                        name = "Speaker0046Rewind" + suffix, RerollOnlyIfFailed = true,
                        SpendOnTrigger = true, ConsumeBuffOnTrigger = false,
                        Resource = Ref<BlueprintAbilityResourceReference>(res), Rules = new[] { rule }
                    });
                var toggle = Toggle(f, spec.Token + suffix + "Toggle", spec.Name + ": " + suffix, spec.Text, toggleBuff, donor);
                parts.Add(Facts(toggle));
            }
            return parts.ToArray();
        }

        // 5. Sacred Council: five move-action variants sharing one stat-mod pool.
        internal static BlueprintAbility SacredCouncilVariants(PsychicAssets f, RevelationBinding0046 bind,
            string tokenPrefix, string displayName, string text, BlueprintAbilityResource res, BlueprintUnitFact donor)
        {
            var variants = new BlueprintAbility[5];
            var names = new[] { "Attack Rolls", "Saving Throws", "Skills", "Caster Level Checks", "Combat Maneuvers" };
            for (int i = 0; i < 5; i++)
            {
                var comps = new List<BlueprintComponent>();
                if (i == 0) comps.Add(new AddStatBonus { name = "CouncilAttack", Stat = StatType.AdditionalAttackBonus, Value = 2, Descriptor = ModifierDescriptor.UntypedStackable });
                if (i == 1) foreach (var s in new[] { StatType.SaveFortitude, StatType.SaveReflex, StatType.SaveWill })
                    comps.Add(new AddStatBonus { name = "CouncilSave" + s, Stat = s, Value = 2, Descriptor = ModifierDescriptor.UntypedStackable });
                if (i == 2) comps.Add(new BuffAllSkillsBonus { name = "CouncilSkills", Value = 2, Multiplier = 1, Descriptor = ModifierDescriptor.UntypedStackable });
                // Bind directly to real spell resistance penetration and dispel CL checks
                // instead of BonusCasterLevel, keeping general caster level unchanged.
                if (i == 3) comps.Add(new SpeakerCasterLevelChecks0046 { name = "CouncilCasterChecks", Bonus = 2 });
                if (i == 4) comps.Add(new AddStatBonus { name = "CouncilManeuver", Stat = StatType.AdditionalCMB, Value = 2, Descriptor = ModifierDescriptor.UntypedStackable });
                var buff = Bf(f, tokenPrefix + "Buff" + i, displayName + ": " + names[i], text, donor, comps.ToArray());
                variants[i] = Ab(f, bind, res, tokenPrefix + "Ability" + i, displayName + ": " + names[i], text,
                    UnitCommand.CommandType.Move, AbilityRange.Personal, donor, Run(For(buff, 1, DurationRate.Rounds)));
                variants[i].CanTargetSelf = true; variants[i].CanTargetFriends = false; variants[i].CanTargetEnemies = false;
            }
            var parent = Variants(f, bind, tokenPrefix + "Ability", displayName, text,
                UnitCommand.CommandType.Move, AbilityRange.Personal, donor, variants);
            parent.CanTargetSelf = true; parent.CanTargetFriends = false; parent.CanTargetEnemies = false;
            return parent;
        }

        static BlueprintComponent[] SacredCouncil(PsychicAssets f, RevelationBinding0046 bind,
            Rev spec, BlueprintAbilityResource res, BlueprintUnitFact donor)
            => new BlueprintComponent[] { Facts(SacredCouncilVariants(f, bind, spec.Token, spec.Name, spec.Text, res, donor)) };

        // 7. Speed or Slow Time: two supernatural variants of the native spells.
        static BlueprintComponent[] SpeedOrSlowTime(PsychicAssets f, RevelationBinding0046 bind,
            Rev spec, BlueprintAbilityResource res, BlueprintUnitFact donor)
        {
            var children = new BlueprintAbility[2];
            for (int i = 0; i < 2; i++)
            {
                var a = f.Clone(f.Native<BlueprintAbility>(i == 0 ? HasteId : SlowId),
                    spec.Token + "Ability" + i, spec.Name + (i == 0 ? ": Haste" : ": Slow"), spec.Text);
                a.ComponentsArray = a.ComponentsArray
                    .Where(c => c != null && !(c is SpellComponent) && !(c is SpellListComponent) && !(c is AbilityResourceLogic))
                    .Concat(Core(bind, res)).ToArray();
                a.Type = AbilityType.Supernatural;
                a.ActionType = UnitCommand.CommandType.Standard;
                a.AvailableMetamagic = 0;
                a.MaterialComponent = new BlueprintAbility.MaterialComponentData();
                RevelationBridge0046.Icon(a, donor);
                children[i] = a;
            }
            var parent = Variants(f, bind, spec.Token + "Ability", spec.Name, spec.Text,
                UnitCommand.CommandType.Standard, AbilityRange.Close, donor, children);
            return new BlueprintComponent[] { Facts(parent) };
        }

        // 12. Time Flicker: 1-minute Blur plus an L7 Displacement toggle that
        //     spends one round of the SAME pool per round (Standard action).
        static BlueprintComponent[] TimeFlicker(PsychicAssets f, RevelationBinding0046 bind,
            Rev spec, BlueprintAbilityResource res, Dictionary<string, BlueprintUnitFact> leaf, BlueprintUnitFact donor)
        {
            var blur = Ab(f, bind, res, spec.Token + "Ability", spec.Name, spec.Text,
                UnitCommand.CommandType.Standard, AbilityRange.Personal, donor,
                Run(For(leaf[BlurBuffId] as BlueprintBuff, 1, DurationRate.Minutes)));
            blur.CanTargetSelf = true; blur.CanTargetFriends = false; blur.CanTargetEnemies = false;
            var displacement = f.Clone(leaf[DisplacementBuffId] as BlueprintBuff,
                spec.Token + "DisplacementBuff", spec.Name + ": Displacement", spec.Text);
            displacement.Stacking = StackingType.Replace;
            RevelationBridge0046.Icon(displacement, donor);
            var toggle = Toggle(f, spec.Token + "DisplacementToggle", spec.Name + ": Displacement", spec.Text,
                displacement, donor, new BlueprintComponent[] { RoundCost(res) }, UnitCommand.CommandType.Standard);
            var ext = Hidden(f, spec.Token + "DisplacementFeature", spec.Name + ": Displacement", spec.Text, donor, toggle);
            return new BlueprintComponent[] {
                Facts(blur), RevelationBridge0046.At(bind, ext, 7, false)
            };
        }

        // 13. Time Hop: Caster Only costs 1 use, Mass costs 2; both are
        //     supernatural move actions cloned from the native dimension doors.
        static BlueprintComponent[] TimeHop(PsychicAssets f, RevelationBinding0046 bind,
            Rev spec, BlueprintAbilityResource res, BlueprintUnitFact donor)
        {
            var children = new BlueprintAbility[2];
            for (int i = 0; i < 2; i++)
            {
                var a = f.Clone(f.Native<BlueprintAbility>(i == 0 ? DimensionDoorCasterOnlyId : DimensionDoorMassId),
                    spec.Token + "Ability" + i, spec.Name + (i == 0 ? ": Caster Only" : ": Mass"), spec.Text);
                a.ComponentsArray = a.ComponentsArray
                    .Where(c => c != null && !(c is SpellComponent) && !(c is SpellListComponent) && !(c is AbilityResourceLogic))
                    .Concat(new BlueprintComponent[] {
                        ArchetypeBatch01445.Cost(res, i == 0 ? 1 : 2),
                        RevelationBridge0046.Rank(bind), RevelationBridge0046.Params(bind.PrimaryClass, bind.CastingStat)
                    }).ToArray();
                a.Type = AbilityType.Supernatural;
                a.ActionType = UnitCommand.CommandType.Move;
                a.AvailableMetamagic = 0;
                a.MaterialComponent = new BlueprintAbility.MaterialComponentData();
                RevelationBridge0046.Icon(a, donor);
                children[i] = a;
            }
            var parent = Variants(f, bind, spec.Token + "Ability", spec.Name, spec.Text,
                UnitCommand.CommandType.Move, AbilityRange.Personal, donor, children);
            parent.CanTargetSelf = true;
            return new BlueprintComponent[] { Facts(parent) };
        }

        // 14. Time Sight: True Seeing only below L18; both True Seeing and
        //     Foresight from L18. The two hidden features are mutually exclusive.
        static BlueprintComponent[] TimeSight(PsychicAssets f, RevelationBinding0046 bind,
            Rev spec, BlueprintAbilityResource res, Dictionary<string, BlueprintUnitFact> leaf, BlueprintUnitFact donor)
        {
            Func<bool, BlueprintAbility> make = high => {
                var actions = new List<GameAction> { For(leaf[TrueSeeingBuffId] as BlueprintBuff, 1, DurationRate.Minutes) };
                if (high) actions.Add(For(leaf[ForesightBuffId] as BlueprintBuff, 1, DurationRate.Minutes));
                var a = Ab(f, bind, res, spec.Token + (high ? "GreaterAbility" : "Ability"),
                    spec.Name, spec.Text, UnitCommand.CommandType.Standard, AbilityRange.Personal, donor,
                    Run(actions.ToArray()));
                a.CanTargetSelf = true; a.CanTargetFriends = false; a.CanTargetEnemies = false;
                return a;
            };
            var prefix = spec.Token.Replace("Feature0046", "").Replace("OracleRevelation0046", "");
            var low = Hidden(f, prefix + "TimeSightLowFeature0046", spec.Name, spec.Text, donor, make(false));
            var high = Hidden(f, prefix + "TimeSightGreaterFeature0046", spec.Name, spec.Text, donor, make(true));
            return new BlueprintComponent[] {
                RevelationBridge0046.At(bind, low, 18, true),
                RevelationBridge0046.At(bind, high, 18, false)
            };
        }

        // 15. Erase from Time: Fortitude negates; on failure the target enters a
        //     private stasis clone that pauses every other buff timer.
        static BlueprintComponent[] EraseFromTime(PsychicAssets f, RevelationBinding0046 bind,
            Rev spec, BlueprintAbilityResource res, Dictionary<string, BlueprintUnitFact> leaf, BlueprintUnitFact donor)
        {
            var native = f.Native<BlueprintBuff>(StasisBuffId);
            var stasis = f.Clone(native, spec.Token + "Buff", spec.Name, spec.Text);
            stasis.ComponentsArray = stasis.ComponentsArray
                .Where(c => !(c is AddCondition a && a.Condition == UnitCondition.Paralyzed))
                .Concat(new BlueprintComponent[] { new PsychicStasisPause() }).ToArray();
            stasis.Stacking = StackingType.Replace;
            RevelationBridge0046.Icon(stasis, donor);
            var duration = Ranking(bind, "Div2", AbilityRankType.DamageDice, true);
            var apply = Apply(stasis, 0, DurationRate.Rounds, false, false);
            apply.AsChild = false;
            apply.DurationValue.BonusValue = R(AbilityRankType.DamageDice);
            var delivery = new BlueprintComponent[] {
                Save(SavingThrowType.Fortitude, new ContextActionConditionalSaved {
                    Succeed = Actions(), Failed = Actions(apply)
                }),
                duration, RevelationBridge0046.Params(bind.PrimaryClass, bind.CastingStat)
            };
            var cast = Touch(f, spec.Token + "Ability", spec.Name, spec.Text, donor, delivery,
                ArchetypeBatch01445.Cost(res), RevelationBridge0046.Params(bind.PrimaryClass, bind.CastingStat));
            return new BlueprintComponent[] { Facts(cast) };
        }

        // 16. Ancestral Weapon: GhostTouch on the primary weapon, automatically
        //     removed on unequip or when the private buff ends.
        static BlueprintComponent[] AncestralWeapon(PsychicAssets f, RevelationBinding0046 bind,
            Rev spec, BlueprintAbilityResource res, BlueprintUnitFact donor)
        {
            var ghostTouch = f.Native<BlueprintWeaponEnchantment>(GhostTouchId);
            var buff = Bf(f, spec.Token + "Buff", spec.Name, spec.Text, donor,
                new PsychicFixedWeaponEnchantment {
                    name = "Speaker0046GhostTouchWeapon",
                    Anchor = Ref<BlueprintItemEnchantmentReference>(ghostTouch),
                    Effect = Ref<BlueprintItemEnchantmentReference>(ghostTouch)
                });
            var a = Ab(f, bind, res, spec.Token + "Ability", spec.Name, spec.Text,
                UnitCommand.CommandType.Standard, AbilityRange.Personal, donor,
                Run(For(buff, 1, DurationRate.Minutes)));
            a.CanTargetSelf = true; a.CanTargetFriends = false; a.CanTargetEnemies = false;
            return new BlueprintComponent[] { Facts(a) };
        }

        // Spirit Walk (C01 2.5 #10): Standard-action toggle spending 1 round/round from
        // a 2 * Shaman(Speaker) pool (22 at L11, 40 at L20), granting Incorporeal,
        // BuffInvisibility (NotDispellAfterOffensiveAction) and UnitCondition.CantAct.
        internal static void BuildSpiritWalkComponents(PsychicAssets f, RevelationBinding0046 bind,
            BlueprintFeature feat, string featureToken, string resToken, string displayName, string displayDesc,
            BlueprintUnitFact incorporealFact, List<BlueprintComponent> comps)
        {
            var res = BuildResource(f, bind, resToken, ResCfg.Level(2));
            var buff = Bf(f, featureToken + "Buff", displayName, displayDesc, feat,
                new BuffInvisibility { name = "Speaker0046SpiritWalkInvis", NotDispellAfterOffensiveAction = true },
                new AddCondition { name = "Speaker0046SpiritWalkCantAct", Condition = UnitCondition.CantAct },
                Facts(incorporealFact));
            buff.Stacking = StackingType.Replace;
            var toggle = Toggle(f, featureToken + "Toggle", displayName, displayDesc,
                buff, feat, new BlueprintComponent[] { RoundCost(res) }, UnitCommand.CommandType.Standard);
            toggle.DeactivateImmediately = true;
            comps.Add(ArchetypeBatch01445.Grant(res));
            comps.Add(Facts(toggle));
        }

    }

    // Aging Touch: Wrath has no GameAction for ability damage, so raise the
    // native RuleDealStatDamage through the ordinary context rule bus. This is
    // real Strength damage (healable / restorable), not a stat penalty buff.
    [TypeId("0046a1b2c3d4e5f60000000000000007")]
    public sealed class SpeakerAgingTouch0046 : ContextAction
    {
        public override string GetCaption() => "Wither the target's Strength";
        public override void RunAction()
        {
            var target = Target?.Unit;
            var caster = Context?.MaybeCaster;
            if (target == null || caster == null) return;
            int level = Math.Max(1, Context.Params?.CasterLevel ?? 1);
            int amount = Math.Max(1, level / 2);
            Context.TriggerRule(new RuleDealStatDamage(caster, target, StatType.Strength,
                new DiceFormula(0, DiceType.Zero), amount));
        }
    }

    // Temporal Celerity, Ruling #6: from L7 the owner is allowed to act in a
    // surprise round. The native turn-based handler fires when the surprise
    // round starts; clearing the initiative cooldown lets the owner take it.
    [TypeId("0046a1b2c3d4e5f60000000000000008")]
    public sealed class SpeakerSurpriseRound0046 : UnitFactComponentDelegate, ITurnBasedModeHandler
    {
        public void HandleSurpriseRoundStarted()
        {
            var cooldown = Owner?.CombatState?.Cooldown;
            if (cooldown == null) return;
            var field = AccessTools.Field(cooldown.GetType(), "Initiative");
            if (field != null) field.SetValue(cooldown, 0f);
        }
        public void HandleRoundStarted(int round) { }
        public void HandleTurnStarted(Kingmaker.EntitySystem.Entities.UnitEntityData unit) { }
        public void HandleUnitControlChanged(Kingmaker.EntitySystem.Entities.UnitEntityData unit) { }
        public void HandleUnitNotSurprised(Kingmaker.EntitySystem.Entities.UnitEntityData unit, RuleSkillCheck rule) { }
    }
}
