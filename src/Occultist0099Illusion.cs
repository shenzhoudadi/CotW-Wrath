using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099Illusion
    {
        internal const string School = "Illusion";
        private const string RayId = "9b4d07751dd104243a94b495c571c9dd";
        private const string BlindId = "187f88d96a0ef464280706b63635f2af";
        private const string DazzleId = "df6d1025da07524429afbae248845ecc";
        private const string FearId = "d2aeac47450c76347aebbc02e4f463e0";
        private const string TouchId = "bb337517547de1a4189518d404ec49d4";
        private const string UnseenId = "ecaa0def35b38f949bd1976a6c9539e0";
        private const string GreaterInvisibilityBuffId = "e6b35473a237a6045969253beb09777c";
        private const string CurseId = "b48674cef2bff5e478a007cf57d8345b";
        private const string DisplacementId = "903092f6488f9ce45a80943923576ab3";
        private static readonly BlueprintGuid TerrorDeliveryId = BlueprintGuid.Parse(Occultist0099Ids.Id("TerrorDelivery"));
        internal static readonly BlueprintGuid DistortionId = BlueprintGuid.Parse(Occultist0099Ids.Id("DistortionBuff"));
        internal static readonly string[] DonorIds = { RayId, BlindId, DazzleId, FearId, TouchId, UnseenId, GreaterInvisibilityBuffId, CurseId, DisplacementId };
        internal static readonly string[] Tokens = { "IllusionEffectiveLevel", "IllusionDCLevel", "DistortionBuff", "ColorBeamAbility",
            "TerrorAbility", "TerrorDelivery", "TerrorPower", "UnseenAbility", "UnseenSelfAbility", "UnseenAllyAbility", "UnseenPower",
            "BedevelingAuraEffect", "BedevelingAuraArea", "BedevelingAuraBuff", "BedevelingAuraAbility", "BedevelingAuraPower",
            "IllusionInvestFocusAbility", "IllusionReplenishFocusAbility", "IllusionImplementFocus" };

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache, Func<string, string, string, LocalizedString> text)
        {
            var ray = Load<BlueprintAbility>(cache, RayId);
            var blind = Load<BlueprintBuff>(cache, BlindId);
            var dazzle = Load<BlueprintBuff>(cache, DazzleId);
            var fear = Load<BlueprintAbility>(cache, FearId);
            var touchWeapon = Load<BlueprintItemWeapon>(cache, TouchId);
            var invisibility = Load<BlueprintAbility>(cache, UnseenId);
            var invisibilityBuff = Load<BlueprintBuff>(cache, GreaterInvisibilityBuffId);
            var curse = Load<BlueprintAbility>(cache, CurseId);
            var displacement = Load<BlueprintAbility>(cache, DisplacementId);
            var projectile = ray?.GetComponent<AbilityDeliverProjectile>();
            var rayEffect = ray?.GetComponent<AbilityEffectRunAction>();
            var split = rayEffect?.Actions?.Actions?.SingleOrDefault() as Conditional;
            if (ray?.name != "IllusionSchoolBaseAbility" || projectile == null || split == null ||
                !(split.ConditionsChecker.Conditions.SingleOrDefault() is ContextConditionHitDice) ||
                !(split.IfTrue.Actions.SingleOrDefault() is ContextActionApplyBuff onBlind) || RefGuid(onBlind, "m_Buff") != blind?.AssetGuid ||
                !(split.IfFalse.Actions.SingleOrDefault() is ContextActionApplyBuff onDazzle) || RefGuid(onDazzle, "m_Buff") != dazzle?.AssetGuid ||
                fear?.name != "Fear" || curse?.name != "RemoveCurse" || displacement?.name != "Displacement" || touchWeapon?.name != "TouchItem" ||
                invisibility?.name != "InvisibilityGreater" || invisibilityBuff?.GetComponent<BuffInvisibility>() == null) return null;

            var level = Occultist0099SchoolValue.Create("IllusionEffectiveLevel", School, Occultist0099SchoolValueKind.EffectiveLevel);
            var dc = Occultist0099SchoolValue.Create("IllusionDCLevel", School, Occultist0099SchoolValueKind.DCLevel);
            const string distortionText = "Attacks against you have a separate 1% miss chance per point of mental focus locked in Illusion, up to your occultist level. See invisibility and true seeing bypass this effect.";
            var distortion = Buff("DistortionBuff", "Distortion", distortionText, displacement);
            SetEnum(distortion, "m_Flags", "StayOnDeath");

            const string beamText = "Spend 1 point of Illusion focus to fire a beam at a creature within 30 feet. On a ranged touch hit, a creature whose Hit Dice do not exceed your occultist level is blinded for 1 round. A stronger creature is dazzled for 1 round. This is a mind-affecting illusion.";
            var branches = CopyData(rayEffect);
            var conditions = (Conditional)branches.Actions.Actions.Single();
            ((ContextConditionHitDice)conditions.ConditionsChecker.Conditions.Single()).HitDice = 0;
            Set(conditions.IfTrue.Actions.Single(), "m_Buff", Ref<BlueprintBuffReference>(blind));
            Set(conditions.IfFalse.Actions.Single(), "m_Buff", Ref<BlueprintBuffReference>(dazzle));
            var beam = Power("ColorBeamAbility", "Color Beam", beamText, AbilityType.SpellLike, AbilityRange.Custom, ray, 1, level, dc,
                branches, CopyData(projectile), Descriptor(SpellDescriptor.MindAffecting),
                new ContextCalculateSharedValue { name = "ColorBeamHitDice", ValueType = AbilitySharedValue.Damage,
                    Value = new ContextDiceValue { DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = PropertyValue(level) } });
            beam.CustomRange = new Feet(30);
            beam.CanTargetSelf = false;
            beam.CanTargetEnemies = beam.CanTargetFriends = beam.SpellResistance = true;
            beam.EffectOnEnemy = beam.EffectOnAlly = AbilityEffectOnUnit.Harmful;

            const string terrorText = "Spend 1 point of Illusion focus to assail an enemy with a nightmare on a melee touch. An enemy whose Hit Dice do not exceed your occultist level provokes attacks of opportunity from your allies. This is a mind-affecting fear effect.";
            var touch = new AbilityDeliverTouch { name = "TerrorTouch" };
            Set(touch, "m_TouchWeapon", Ref<BlueprintItemWeaponReference>(touchWeapon));
            var terrorDelivery = Identify(new BlueprintAbility { Type = AbilityType.Supernatural, Range = AbilityRange.Touch,
                ActionType = UnitCommand.CommandType.Standard, CanTargetEnemies = true, EffectOnEnemy = AbilityEffectOnUnit.Harmful,
                ComponentsArray = new BlueprintComponent[] { touch, SchoolComponent(), Descriptor(SpellDescriptor.MindAffecting | SpellDescriptor.Fear),
                    Run(new PhantasmTerror01447 { SharedLevel = PropertyValue(level), AlliesOnly = true }) } },
                "TerrorDelivery", "Terror", terrorText, fear, text);
            var sticky = new AbilityEffectStickyTouch { name = "TerrorHeldTouch" };
            Set(sticky, "m_TouchDeliveryAbility", Ref<BlueprintAbilityReference>(terrorDelivery));
            var terror = Power("TerrorAbility", "Terror", terrorText, AbilityType.Supernatural, AbilityRange.Touch, fear, 1, level, dc,
                sticky, Descriptor(SpellDescriptor.MindAffecting | SpellDescriptor.Fear));
            terror.CanTargetSelf = false;
            terror.CanTargetEnemies = true;
            terror.EffectOnEnemy = AbilityEffectOnUnit.Harmful;

            const string unseenText = "Spend 1 point of Illusion focus to grant yourself greater invisibility for 1 minute, or 2 points to affect a willing adjacent creature.";
            var unseenSelf = Power("UnseenSelfAbility", "Unseen", unseenText, AbilityType.SpellLike, AbilityRange.Personal,
                invisibility, 1, level, dc, Run(Apply(invisibilityBuff, 1, DurationRate.Minutes)));
            var unseenAlly = Power("UnseenAllyAbility", "Unseen (Ally)", unseenText, AbilityType.SpellLike, AbilityRange.Touch,
                invisibility, 2, level, dc, Run(Apply(invisibilityBuff, 1, DurationRate.Minutes)));
            unseenAlly.CanTargetSelf = false;
            unseenAlly.CanTargetFriends = true;
            var unseen = Variants("UnseenAbility", "Unseen", unseenText, invisibility, text, unseenSelf, unseenAlly);

            const string auraText = "Spend 1 point of Illusion focus to create a 30-foot aura for 1 round per 2 occultist levels. Enemies move at half speed, cannot make attacks of opportunity, and are considered flanked. This is a mind-affecting effect.";
            var effect = Buff("BedevelingAuraEffect", "Bedeveling Aura", auraText, curse, Phantasm01447.AuraEffects());
            effect.Stacking = StackingType.Stack;
            var area = Identify(Phantasm01447.AuraArea(effect), "BedevelingAuraArea", "Bedeveling Aura", auraText, curse, text);
            var add = new AddAreaEffect { name = "BedevelingAuraAttach" };
            Set(add, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(area));
            var host = Buff("BedevelingAuraBuff", "Bedeveling Aura", auraText, curse, add);
            var halfLevel = new ContextRankConfig { name = "BedevelingDuration" };
            SetEnum(halfLevel, "m_BaseValueType", "CustomProperty");
            Set(halfLevel, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(level));
            SetEnum(halfLevel, "m_Progression", "Div2");
            var apply = Apply(host, 0, toCaster: true, fromSpell: false);
            apply.DurationValue.BonusValue = new ContextValue { ValueType = ContextValueType.Rank };
            var aura = Power("BedevelingAuraAbility", "Bedeveling Aura", auraText, AbilityType.Supernatural, AbilityRange.Personal,
                curse, 1, level, dc, halfLevel, Run(apply));

            var invest = Operation("IllusionInvestFocusAbility", "Invest Mental Focus: Illusion", "Invest 1 point of unallocated mental focus in Illusion before locking your focus.",
                Occultist0099FocusAction.Invest, UnitCommand.CommandType.Free, ray, text, School);
            var replenish = Operation("IllusionReplenishFocusAbility", "Replenish Mental Focus: Illusion", "Spend 1 unallocated point of mental focus to replenish a spent Illusion point.",
                Occultist0099FocusAction.Replenish, UnitCommand.CommandType.Free, ray, text, School);
            var implement = Identify(new BlueprintFeature { Ranks = 1, IsClassFeature = true, ComponentsArray = new BlueprintComponent[]
                { new Occultist0099ImplementSchool { name = "OccultistIllusionSchool", School = School }, Facts(beam, invest, replenish) } },
                "IllusionImplementFocus", "Illusion Implement", "Gain Color Beam and Distortion.", ray, text);
            return new BlueprintScriptableObject[] { level, dc, distortion, beam, terror, terrorDelivery, unseen, unseenSelf, unseenAlly,
                effect, area, host, aura, invest, replenish, implement };

            BlueprintBuff Buff(string token, string label, string description, BlueprintAbility icon, params BlueprintComponent[] components)
                => Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace, ComponentsArray = components },
                    token, label, description, icon, text);

            BlueprintAbility Power(string token, string label, string description, AbilityType type, AbilityRange range,
                BlueprintAbility icon, int cost, BlueprintUnitProperty casterLevel, BlueprintUnitProperty spellLevel, params BlueprintComponent[] components)
                => Identify(NewPower(type, range, cost, casterLevel, spellLevel, components), token, label, description, icon, text);
        }

        internal static T Load<T>(BlueprintsCache cache, string id) where T : BlueprintScriptableObject
        {
            var guid = BlueprintGuid.Parse(id);
            var candidate = cache.Load(guid) as T;
            return candidate?.AssetGuid == guid ? candidate : null;
        }

        internal static BlueprintAbility NewPower(AbilityType type, AbilityRange range, int cost, BlueprintUnitProperty level,
            BlueprintUnitProperty dc, params BlueprintComponent[] components)
            => new BlueprintAbility { Type = type, Range = range, CanTargetSelf = true, ActionType = UnitCommand.CommandType.Standard,
                ComponentsArray = components.Concat(new BlueprintComponent[]
                {
                    SchoolComponent(), new Occultist0099FocusResourceLogic { name = "OccultistIllusionCost", School = School, Amount = cost },
                    new ContextCalculateAbilityParams { name = "OccultistIllusionParams", StatType = StatType.Intelligence,
                        ReplaceCasterLevel = true, CasterLevel = PropertyValue(level), ReplaceSpellLevel = true, SpellLevel = PropertyValue(dc) }
                }).ToArray() };

        internal static BlueprintAbility Variants(string token, string label, string description, BlueprintAbility icon,
            Func<string, string, string, LocalizedString> text, params BlueprintAbility[] leaves)
        {
            var variants = new AbilityVariants { name = token + "Variants" };
            Set(variants, "m_Variants", leaves.Select(Ref<BlueprintAbilityReference>).ToArray());
            var root = Identify(new BlueprintAbility { Type = AbilityType.SpellLike, Range = icon.Range,
                ActionType = UnitCommand.CommandType.Standard, ComponentsArray = new BlueprintComponent[] { variants, SchoolComponent() } },
                token, label, description, icon, text);
            foreach (var leaf in leaves) Set(leaf, "m_Parent", Ref<BlueprintAbilityReference>(root));
            return root;
        }

        private static BlueprintGuid RefGuid(object obj, string field) => (Get(obj, field) as BlueprintReferenceBase)?.Guid ?? BlueprintGuid.Empty;
        private static SpellComponent SchoolComponent() => new SpellComponent { name = "OccultistIllusion", School = SpellSchool.Illusion };
        internal static SpellDescriptorComponent Descriptor(SpellDescriptor descriptor)
            => new SpellDescriptorComponent { name = "OccultistIllusionDescriptors", Descriptor = descriptor };
        internal static bool HoldsTerror(UnitPartTouch part) => part?.Ability?.Blueprint.AssetGuid == TerrorDeliveryId;
    }

    [HarmonyPatch(typeof(RuleAttackRoll), "TryOvercomeTargetConcealmentAndMissChance")]
    internal static class Occultist0099NativeDistortion
    {
        internal static int Chance(RuleAttackRoll attack)
        {
            var target = attack.Target;
            var attacker = attack.Initiator;
            if (target == null || attacker == null || attacker == target ||
                (attack.AttackType != AttackType.Melee && attack.AttackType != AttackType.Ranged &&
                 attack.AttackType != AttackType.Touch && attack.AttackType != AttackType.RangedTouch)) return 0;
            if (!target.Buffs.Enumerable.Any(buff => buff.Blueprint.AssetGuid == Occultist0099Illusion.DistortionId &&
                buff.IsTurnedOn && !buff.IsSuppressed)) return 0;
            if (attacker.State.HasCondition(UnitCondition.SeeInvisibility) || attacker.State.HasCondition(UnitCondition.TrueSeeing)) return 0;
            return Occultist0099Focus.Resonance(target.Get<Occultist0099FocusPart>()?.State, Occultist0099Illusion.School);
        }

        private static bool Prefix(RuleAttackRoll __instance, ref bool __result, out PsychicEtherMissPatch.RollState __state)
            => PsychicEtherMissPatch.RollIndependent(__instance, Chance(__instance), ref __result, out __state);
        private static void Postfix(RuleAttackRoll __instance, PsychicEtherMissPatch.RollState __state)
            => PsychicEtherMissPatch.Report(__instance, __state);
    }
}
