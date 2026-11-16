using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099Divination
    {
        internal const string School = "Divination";
        private const string FortuneAbility = "0d10ea99df0b06249823c760c46029b4";
        private const string FortuneBuff = "6d338078b1a8cdc41bf3a39f65247161";
        private const string GuidanceBuff = "ec931b882e806ce42906597e5585c13f";
        private const string SeeInvisibilityBuff = "0adecbf63b614e846bfe15c33f34507e";
        internal const string ArcaneEye = "7873f6a427a61105c544507323662f63";
        internal static readonly string[] DonorIds = { FortuneAbility, FortuneBuff, GuidanceBuff, SeeInvisibilityBuff, ArcaneEye };
        internal static readonly string[] Tokens = { "DivinationEffectiveLevel", "ThirdEyeValue", "ThirdEyeBuff", "ThirdEyeSeeInvisibility",
            "ThirdEyeBlindsense", "ThirdEyeBlindsight", "SuddenInsightBuff", "SuddenInsightAbility", "DivinersFortuneBuff",
            "DivinersFortuneAbility", "DivinersFortuneDelivery", "DangerSightBuff", "DangerSightAbility", "MindEyeAbility", "ProphecyBuff", "ProphecyAbility",
            "DivinationInvestFocusAbility", "DivinationReplenishFocusAbility", "DivinationImplementFocus",
            "DivinersFortunePower", "DangerSightPower", "MindEyePower", "ProphecyPower" };
        private static readonly BlueprintGuid ProphecyId = BlueprintGuid.Parse(Occultist0099Ids.Id("ProphecyBuff"));
        private static readonly BlueprintGuid FortuneDeliveryId = BlueprintGuid.Parse(Occultist0099Ids.Id("DivinersFortuneDelivery"));

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache, Func<string, string, string, LocalizedString> text)
        {
            var donor = Load<BlueprintAbility>(FortuneAbility, "DivinationSchoolBaseAbility");
            var fortuneSource = Load<BlueprintBuff>(FortuneBuff, "DivinationSchoolBaseBuff");
            var guidance = Load<BlueprintBuff>(GuidanceBuff, "GuidanceBuff");
            var detection = Load<BlueprintBuff>(SeeInvisibilityBuff, "SeeInvisibilitytBuff");
            var eye = Load<BlueprintAbility>(ArcaneEye, "CotWWrathPsychicArcaneEye");
            var touch = donor?.GetComponent<AbilityDeliverTouch>();
            var attack = guidance?.GetComponent<AddInitiatorAttackRollTrigger>();
            var skill = guidance?.GetComponent<AddInitiatorSkillRollTrigger>();
            var party = guidance?.GetComponent<AddInitiatorPartySkillRollTrigger>();
            var save = guidance?.GetComponent<AddInitiatorSavingThrowTrigger>();
            var vision = detection?.GetComponent<AddCondition>();
            var eyeRun = eye?.GetComponent<AbilityEffectRunAction>();
            if (touch == null || fortuneSource == null || attack == null || skill == null || party == null || save == null ||
                vision?.Condition != UnitCondition.SeeInvisibility || eyeRun?.Actions?.Actions == null ||
                !eyeRun.Actions.Actions.OfType<ContextActionSpawnMonster>().Any(spawn =>
                    spawn.IsDirectlyControllable && spawn.AfterSpawn.Actions.OfType<PsychicSpiritBind>().Any(bind => bind.Kind == 4))) return null;
            var bonuses = fortuneSource.ComponentsArray.Where(component => !(component is ContextRankConfig)).ToArray();
            var saves = bonuses.OfType<AddStatBonusAbilityValue>().Where(component =>
                component.Stat == StatType.SaveFortitude || component.Stat == StatType.SaveReflex || component.Stat == StatType.SaveWill)
                .Cast<BlueprintComponent>().ToArray();
            if (saves.Length != 3 || bonuses.Length != 6 || !bonuses.OfType<BuffAbilityRollsBonus>().Any() ||
                !bonuses.OfType<BuffAllSkillsBonusAbilityValue>().Any()) return null;

            var level = Occultist0099SchoolValue.Create("DivinationEffectiveLevel", School, Occultist0099SchoolValueKind.EffectiveLevel);
            var resonance = Occultist0099SchoolValue.Create("ThirdEyeValue", School, Occultist0099SchoolValueKind.Resonance);
            var perception = new ContextRankConfig { name = "ThirdEyeLockedRank" };
            SetEnum(perception, "m_BaseValueType", "CustomProperty");
            Set(perception, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(resonance));
            const string thirdEyeText = "Gain an insight bonus on Perception equal to half the mental focus locked in Divination, up to your occultist level. At levels 7, 13, and 19, locking 9, 12, or 15 points grants see invisibility, blindsense (60 feet), or blindsight (30 feet) and gaze immunity, respectively.";
            var thirdEye = Buff("ThirdEyeBuff", "Third Eye", thirdEyeText, perception, StatBonus(StatType.SkillPerception));
            SetEnum(thirdEye, "m_Flags", "StayOnDeath");
            var see = Sense("ThirdEyeSeeInvisibility", CopyData(vision));
            var blindsense = Sense("ThirdEyeBlindsense", new Blindsense { name = "ThirdEyeBlindsense", Range = new Feet(60) });
            var blindsight = Sense("ThirdEyeBlindsight", new Blindsense { name = "ThirdEyeBlindsight", Range = new Feet(30), Blindsight = true },
                new SpellImmunityToSpellDescriptor { name = "ThirdEyeGazeSpells", Descriptor = SpellDescriptor.GazeAttack },
                new BuffDescriptorImmunity { name = "ThirdEyeGazeBuffs", Descriptor = SpellDescriptor.GazeAttack });

            const string fortuneText = "Spend 1 point of Divination focus to give a creature you touch an insight bonus to attacks, skills, ability checks and saving throws for 1 round. The bonus equals half your occultist level, minimum 1.";
            var fortuneBuff = Buff("DivinersFortuneBuff", "Diviner's Fortune", fortuneText, WithRank(bonuses));
            var fortuneDelivery = Identify(new BlueprintAbility { Type = AbilityType.SpellLike, Range = AbilityRange.Touch,
                ActionType = UnitCommand.CommandType.Standard, CanTargetSelf = true, CanTargetFriends = true, CanTargetEnemies = true,
                EffectOnAlly = AbilityEffectOnUnit.Helpful, NotOffensive = true, ComponentsArray = new BlueprintComponent[]
                { CopyData(touch), Run(Apply(fortuneBuff, 1)), new SpellComponent { name = "DivinersFortuneSchool", School = SpellSchool.Divination } } },
                "DivinersFortuneDelivery", "Diviner's Fortune", fortuneText, donor, text);
            var heldTouch = new AbilityEffectStickyTouch { name = "DivinersFortuneHeldTouch" };
            Set(heldTouch, "m_TouchDeliveryAbility", Ref<BlueprintAbilityReference>(fortuneDelivery));
            var fortune = Power("DivinersFortuneAbility", "Diviner's Fortune", fortuneText, UnitCommand.CommandType.Standard,
                AbilityRange.Touch, donor, heldTouch);
            fortune.CanTargetEnemies = fortune.CanTargetFriends = true;

            const string insightText = "Spend 1 point of Divination focus as a swift action to gain an insight bonus equal to half your occultist level, minimum 1, on your next attack, ability check or skill check within 1 round.";
            var insightAttack = CopyData(attack);
            insightAttack.Action = Actions(new ContextActionRemoveSelf());
            insightAttack.OnlyHit = false;
            var insightSkill = CopyData(skill);
            insightSkill.Action = Actions(new ContextActionRemoveSelf());
            var insightParty = CopyData(party);
            insightParty.Action = Actions(new ContextActionRemoveSelf());
            var insightBuff = Buff("SuddenInsightBuff", "Sudden Insight", insightText,
                WithRank(bonuses.Except(saves)).Concat(new BlueprintComponent[] { insightAttack, insightSkill, insightParty }).ToArray());
            var insight = Power("SuddenInsightAbility", "Sudden Insight", insightText, UnitCommand.CommandType.Swift,
                AbilityRange.Personal, donor, Run(Apply(insightBuff, 1)));

            const string dangerText = "Spend 1 point of Divination focus as a swift action to gain an insight bonus equal to half your occultist level, minimum 1, to AC against the next attack or to your next saving throw within 1 minute.";
            var savingThrow = CopyData(save);
            savingThrow.Action = Actions(new ContextActionRemoveSelf());
            var dangerBuff = Buff("DangerSightBuff", "Danger Sight", dangerText, WithRank(saves).Concat(new BlueprintComponent[]
            {
                StatBonus(StatType.AC), savingThrow, new AddTargetAttackRollTrigger
                {
                    name = "DangerSightUsedByAttack", OnlyHit = false, Categories = Array.Empty<WeaponCategory>(),
                    ActionsOnAttacker = Actions(), ActionOnSelf = Actions(new ContextActionRemoveSelf())
                }
            }).ToArray());
            var danger = Power("DangerSightAbility", "Danger Sight", dangerText, UnitCommand.CommandType.Swift,
                AbilityRange.Personal, donor, Run(Apply(dangerBuff, 1, DurationRate.Minutes)));

            const string eyeText = "Spend 1 point of Divination focus to create an invisible, controllable magical sensor with a movement speed of 30 feet. The sensor lasts 1 minute per occultist level.";
            var mindEye = Power("MindEyeAbility", "Mind Eye", eyeText, UnitCommand.CommandType.Standard, eye.Range, eye,
                CopyData(eyeRun), PsychicLinkedWard.CL());
            mindEye.CanTargetSelf = false;
            mindEye.CanTargetPoint = true;

            const string prophecyText = "Guide your next eligible occultist spell with prophecy. Spend 1 point of Divination focus when you cast it and apply Empower without raising its spell level. It has a 20% chance to fail, reduced to 15% at level 12 and 10% at level 16.";
            var prophecyBuff = Buff("ProphecyBuff", "In Accordance with the Prophecy", prophecyText);
            var prophecy = Identify(new BlueprintActivatableAbility { ActivationType = AbilityActivationType.Immediately,
                DeactivateImmediately = true, DeactivateIfOwnerDisabled = true, DeactivateIfOwnerUnconscious = true,
                ComponentsArray = new BlueprintComponent[] { new Occultist0099ProphecyReady { name = "OccultistProphecyReady" } } },
                "ProphecyAbility", "In Accordance with the Prophecy", prophecyText, donor, text);
            Set(prophecy, "m_Buff", Ref<BlueprintBuffReference>(prophecyBuff));

            var invest = Operation("DivinationInvestFocusAbility", "Invest Mental Focus: Divination",
                "Invest 1 point of unallocated mental focus in Divination before locking your focus.",
                Occultist0099FocusAction.Invest, UnitCommand.CommandType.Free, donor, text, School);
            var replenish = Operation("DivinationReplenishFocusAbility", "Replenish Mental Focus: Divination",
                "Spend 1 unallocated point of mental focus to replenish a spent Divination point.",
                Occultist0099FocusAction.Replenish, UnitCommand.CommandType.Free, donor, text, School);
            var implement = Identify(new BlueprintFeature { Ranks = 1, IsClassFeature = true, ComponentsArray = new BlueprintComponent[]
                { new Occultist0099ImplementSchool { name = "OccultistDivinationSchool", School = School }, Facts(insight, invest, replenish) } },
                "DivinationImplementFocus", "Divination Implement", "Gain Sudden Insight and Third Eye.", donor, text);
            return new BlueprintScriptableObject[] { level, resonance, thirdEye, see, blindsense, blindsight, fortuneBuff, fortune, fortuneDelivery, insightBuff,
                insight, dangerBuff, danger, mindEye, prophecyBuff, prophecy, invest, replenish, implement };

            T Load<T>(string id, string name) where T : BlueprintScriptableObject
            {
                var guid = BlueprintGuid.Parse(id);
                var result = cache.Load(guid) as T;
                return result?.AssetGuid == guid && result.name == name ? result : null;
            }

            BlueprintBuff Buff(string token, string name, string description, params BlueprintComponent[] components)
                => Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace, ComponentsArray = components },
                    token, name, description, donor, text);

            BlueprintBuff Sense(string token, params BlueprintComponent[] components)
            {
                var buff = Buff(token, "Third Eye", thirdEyeText, components);
                SetEnum(buff, "m_Flags", "HiddenInUi, StayOnDeath");
                return buff;
            }

            BlueprintAbility Power(string token, string name, string description, UnitCommand.CommandType command,
                AbilityRange range, BlueprintAbility icon, params BlueprintComponent[] components)
                => Identify(new BlueprintAbility { Type = AbilityType.SpellLike, ActionType = command, Range = range,
                    CanTargetSelf = true, NotOffensive = true, EffectOnAlly = AbilityEffectOnUnit.Helpful,
                    ComponentsArray = components.Concat(new BlueprintComponent[]
                    {
                        new SpellComponent { name = "OccultistDivination", School = SpellSchool.Divination },
                        new Occultist0099FocusResourceLogic { name = "OccultistDivinationCost", School = School, Amount = 1 },
                        new ContextCalculateAbilityParams { name = "OccultistDivinationLevel", StatType = StatType.Intelligence,
                            ReplaceCasterLevel = true, CasterLevel = PropertyValue(level) }
                    }).ToArray() }, token, name, description, icon, text);
        }

        private static BlueprintComponent[] WithRank(IEnumerable<BlueprintComponent> components)
        {
            var rank = new ContextRankConfig { name = "OccultistDivinationHalfLevel" };
            SetEnum(rank, "m_BaseValueType", "CasterLevel");
            SetEnum(rank, "m_Progression", "Div2");
            Set(rank, "m_UseMin", true);
            Set(rank, "m_Min", 1);
            return components.Select(CopyData).Concat(new[] { rank }).ToArray();
        }

        private static AddContextStatBonus StatBonus(StatType stat)
            => new AddContextStatBonus { name = "Occultist" + stat, Stat = stat, Descriptor = ModifierDescriptor.Insight,
                Value = new ContextValue { ValueType = ContextValueType.Rank } };

        internal static bool HoldsFortune(UnitPartTouch part) => part?.Ability?.Blueprint.AssetGuid == FortuneDeliveryId;

        internal static void TryEmpower(RuleCastSpell rule)
        {
            var spell = rule.Spell;
            var context = rule.Context;
            if (!Occultist0099NativeScope.IsPrivateBook(spell?.Spellbook) || !ProphecyAccordance01431.EmpowerEligible(spell) ||
                spell.IsChildSpell || context?.Params == null || context.IsForced || rule.IsDuplicateSpellApplied ||
                context.IsDuplicateSpellApplied || (context.Params.Metamagic & Metamagic.Empower) != 0) return;
            var buff = rule.Initiator.Buffs.Enumerable.FirstOrDefault(candidate => candidate.Blueprint.AssetGuid == ProphecyId &&
                candidate.IsTurnedOn && !candidate.IsSuppressed);
            if (buff == null) return;
            string identity = Occultist0099NativeCast.IdentityOf(rule);
            int effectiveLevel = Occultist0099NativeScope.InRuntimeContext(
                Occultist0099NativeScope.RuntimeOf(rule.Initiator, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099)),
                component =>
                {
                    var focus = (Occultist0099FocusFact0099)component;
                    if (!focus.TryPaySchool(rule.Initiator, School, 1, identity).Applied) return 0;
                    focus.SyncResourceMirror();
                    return focus.MindBarrier(School).EffectiveLevel;
                }, 0);
            if (effectiveLevel <= 0) return;
            buff.Remove();
            ProphecyAccordance01431.ApplyFailure(rule, effectiveLevel);
            var parameters = context.Params.Clone();
            parameters.Metamagic |= Metamagic.Empower;
            context.SetParams(parameters);
            ProphecyAccordance01431.PreservePaidEmpower(context);
        }
    }

    [HarmonyPatch(typeof(UnitPartTouch), nameof(UnitPartTouch.HandleObjectLeaveCombat))]
    internal static class Occultist0099NativeFortuneCleanup
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator labels)
        {
            var code = instructions.ToList();
            var spellbooks = AccessTools.PropertyGetter(typeof(UnitEntityData), nameof(UnitEntityData.Spellbooks));
            int refund = code.FindIndex(instruction => instruction.Calls(spellbooks));
            if (refund < 0 || code.Count(instruction => instruction.Calls(spellbooks)) != 1)
                throw new InvalidOperationException("The native held touch refund entry changed.");

            // Native removal clears the originating spell from the fact. Its
            // delivery blueprint still identifies Fortune at the refund step.
            // The caster is already on the stack for this lookup.
            var lookup = code[refund];
            var normalRefund = labels.DefineLabel();
            var held = new CodeInstruction(OpCodes.Ldarg_0);
            held.labels.AddRange(lookup.labels);
            lookup.labels.Clear();
            lookup.labels.Add(normalRefund);
            code.InsertRange(refund, new[]
            {
                held, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Occultist0099Divination), nameof(Occultist0099Divination.HoldsFortune))),
                new CodeInstruction(OpCodes.Brfalse, normalRefund), new CodeInstruction(OpCodes.Pop), new CodeInstruction(OpCodes.Ret)
            });
            return code;
        }
    }

    [TypeId(Occultist0099Ids.ProphecyReadyTypeId)]
    public sealed class Occultist0099ProphecyReady : ActivatableAbilityRestriction
    {
        protected override bool IsAvailable() => Occultist0099NativeScope.InRuntimeContext(
            Occultist0099NativeScope.RuntimeOf(Owner, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099)),
            component => ((Occultist0099FocusFact0099)component).CanPaySchool(Owner, Occultist0099Divination.School, 1, null), false);
    }
}
