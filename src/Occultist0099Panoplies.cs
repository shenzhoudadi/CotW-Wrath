using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Properties;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099Panoplies
    {
        internal const string Trappings = "Trappings of the Warrior";
        internal const string TrappingsToken = "TrappingsOfTheWarrior";
        internal const string Paraphernalia = "Mage's Paraphernalia";
        internal const string ParaphernaliaToken = "MagesParaphernalia";
        internal const string Performers = "Performer's Accoutrements";
        internal const string PerformersToken = "PerformersAccoutrements";
        internal const string Regalia = "Saint's Holy Regalia";
        internal const string RegaliaToken = "SaintsHolyRegalia";
        internal static readonly (string Name, string Token, string Value, string[] Effects)[] Definitions =
        {
            (Trappings, TrappingsToken, "TrappingsMartialSkillValue", new[] { "CounterstrikeBuff" }),
            (Paraphernalia, ParaphernaliaToken, "ScholarlyKnowledgeValue", Occultist0099Paraphernalia.ActiveEffectTokens),
            (Performers, PerformersToken, "TrickstersSkillValue", new[] { "TrickstersEdgeBuff" }),
            (Regalia, RegaliaToken, "FontOfHealingValue", new[] { "GuardianAuraBuff" })
        };
        private const string PrescientAttack = "fa12d155c229c134dbbbebf0d7b980f0";
        private const string PrescientBuff = "2544b9d16793e2642a645c8e3aece7d3";
        private const string DominateMonster = "3c17035ec4717674cae2e841a190e757";
        internal static readonly string[] Donors = new[] { PrescientAttack, PrescientBuff, DominateMonster }
            .Concat(Occultist0099Trappings.Donors).Concat(Occultist0099Paraphernalia.Donors)
            .Concat(Occultist0099Regalia.Donors).ToArray();
        internal static readonly string[] Tokens = new[]
        {
            "PanoplyActualLevel", "PanoplyActualDCLevel", "TrickstersSkillValue", PerformersToken + "ResonanceBuff",
            "TrickstersEdgeBuff", "TrickstersEdgeAbility", PerformersToken + "Source", "PuppetMasterAbility", "PuppetMasterPower"
        }.Concat(Occultist0099Trappings.Tokens).Concat(Occultist0099Paraphernalia.Tokens)
          .Concat(Occultist0099Regalia.Tokens).ToArray();

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache, Func<string, string, string, LocalizedString> text)
        {
            var level = Occultist0099SchoolValue.Create("PanoplyActualLevel", null, Occultist0099SchoolValueKind.ActualLevel);
            var dc = Occultist0099SchoolValue.Create("PanoplyActualDCLevel", null, Occultist0099SchoolValueKind.ActualDCLevel);
            var trappings = Occultist0099Trappings.Build(cache, text, level, dc);
            var paraphernalia = Occultist0099Paraphernalia.Build(cache, text, level, dc);
            var performers = BuildPerformers(cache, text, level, dc);
            var regalia = Occultist0099Regalia.Build(cache, text, level, dc);
            return trappings == null || paraphernalia == null || performers == null || regalia == null ? null :
                new BlueprintScriptableObject[] { level, dc }.Concat(trappings).Concat(paraphernalia)
                    .Concat(performers).Concat(regalia).ToArray();
        }

        private static BlueprintScriptableObject[] BuildPerformers(BlueprintsCache cache, Func<string, string, string, LocalizedString> text,
            BlueprintUnitProperty level, BlueprintUnitProperty dc)
        {
            var prescient = Occultist0099Illusion.Load<BlueprintAbility>(cache, PrescientAttack);
            var prescientBuff = Occultist0099Illusion.Load<BlueprintBuff>(cache, PrescientBuff);
            var dominate = Occultist0099Illusion.Load<BlueprintAbility>(cache, DominateMonster);
            var dominateAction = CopyData(dominate?.GetComponent<AbilityEffectRunAction>());
            if (prescient?.name != "PrescientAttackAbility" || prescientBuff?.name != "PrescientAttackBuff" ||
                prescientBuff.ComponentsArray.Length == 0 || dominate?.name != "DominateMonster" || dominateAction == null)
                return null;
            var domination = SpellBatch01447.Elements(new[] { dominateAction }, new HashSet<object>(IdentityComparer))
                .OfType<ContextActionApplyBuff>().ToArray();
            if (domination.Length != 1) return null;

            var skill = ResonanceValue("TrickstersSkillValue", Performers, 3, true);
            const string skillText = "Gain a +1 bonus to Stealth and Trickery checks per 3 points of mental focus locked in Enchantment and Illusion, up to half your occultist level.";
            var skillRank = new ContextRankConfig { name = "TrickstersSkillRank" };
            SetEnum(skillRank, "m_BaseValueType", "CustomProperty");
            Set(skillRank, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(skill));
            var skillBuff = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                ComponentsArray = new BlueprintComponent[] { skillRank, SkillBonus(StatType.SkillStealth), SkillBonus(StatType.SkillThievery) } },
                PerformersToken + "ResonanceBuff", "Trickster's Skill", skillText, prescient, text);
            SetEnum(skillBuff, "m_Flags", "StayOnDeath");

            const string edgeText = "Spend 2 points of mental focus as a swift action. Your targets are denied their Dexterity bonus to AC against your attacks for 1 round.";
            var edgeBuff = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                ComponentsArray = prescientBuff.ComponentsArray.Select(CopyData).ToArray() },
                "TrickstersEdgeBuff", "Trickster's Edge", edgeText, prescient, text);
            var nativeEdge = CopyData(prescient.GetComponent<AbilityEffectRunAction>());
            if (nativeEdge?.Actions.Actions.SingleOrDefault() is not ContextActionApplyBuff prescientApply) return null;
            Set(prescientApply, "m_Buff", Ref<BlueprintBuffReference>(edgeBuff));
            var edge = Power("TrickstersEdgeAbility", "Trickster's Edge", edgeText, prescient, 2,
                AbilityType.Supernatural, AbilityRange.Personal, SpellSchool.Illusion, nativeEdge);
            edge.ActionType = UnitCommand.CommandType.Swift;
            edge.NotOffensive = true;
            var source = Carrier(Performers, PerformersToken, skillText, prescient, text, edge);

            const string puppetText = "Spend 3 points of mental focus to dominate a creature for 1 round per 2 occultist levels. A Will save negates the effect.";
            var duration = new ContextRankConfig { name = "PuppetMasterDuration" };
            SetEnum(duration, "m_BaseValueType", "CasterLevel");
            SetEnum(duration, "m_Progression", "Div2");
            domination[0].DurationValue.Rate = DurationRate.Rounds;
            domination[0].DurationValue.BonusValue = Rank();
            var puppetComponents = dominate.ComponentsArray.Where(component => component is IAbilityTargetRestriction ||
                component is SpellDescriptorComponent || component is AbilitySpawnFx).Select(CopyData)
                .Concat(new BlueprintComponent[] { dominateAction, duration,
                    new Occultist0099PowerLevel { name = "PuppetMasterLevel", Minimum = 9 } }).ToArray();
            var puppet = Power("PuppetMasterAbility", "Puppet Master", puppetText, dominate, 3, AbilityType.SpellLike,
                dominate.Range, SpellSchool.Enchantment, puppetComponents);
            puppet.CanTargetEnemies = true;
            puppet.CanTargetSelf = false;
            puppet.SpellResistance = dominate.SpellResistance;
            puppet.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            puppet.GetComponent<Occultist0099FocusResourceLogic>().CheckCasterRestrictions = true;
            return new BlueprintScriptableObject[] { skill, skillBuff, edgeBuff, edge, source, puppet };

            BlueprintAbility Power(string token, string name, string description, BlueprintAbility icon, int cost,
                AbilityType type, AbilityRange range, SpellSchool school, params BlueprintComponent[] components)
                => CreatePower(Performers, token, name, description, icon, text, cost, type, range, school, level, dc, components);
        }

        internal static BlueprintUnitProperty ResonanceValue(string token, string panoply, int divisor, bool halfLevelCap,
            bool capAtLevel = false)
            => Identify(new BlueprintUnitProperty { ComponentsArray = new BlueprintComponent[]
            {
                new Occultist0099PanoplyValue { name = token, Panoply = panoply, Divisor = divisor,
                    HalfLevelCap = halfLevelCap, CapAtLevel = capAtLevel, Settings = new PropertySettings() }
            } }, token, "", "", null);

        internal static BlueprintFeature Carrier(string panoply, string token, string description, BlueprintAbility icon,
            Func<string, string, string, LocalizedString> text, params BlueprintUnitFact[] facts)
        {
            var grant = new Occultist0099PanoplyCarrier { name = token, Panoply = panoply };
            Set(grant, "m_Facts", facts.Select(Ref<BlueprintUnitFactReference>).ToArray());
            return Identify(new BlueprintFeature { Ranks = 1, IsClassFeature = true,
                ComponentsArray = new BlueprintComponent[] { grant } }, token + "Source", panoply, description, icon, text);
        }

        internal static BlueprintAbility CreatePower(string panoply, string token, string name, string description, BlueprintAbility icon,
            Func<string, string, string, LocalizedString> text, int cost, AbilityType type, AbilityRange range, SpellSchool school,
            BlueprintUnitProperty level, BlueprintUnitProperty dc, params BlueprintComponent[] components)
        {
            var visibility = new Occultist0099PanoplyVisibility { name = "OccultistPanoplyVisibility", Panoply = panoply };
            var ability = Occultist0099Illusion.NewSchoolPower(school, type, range, cost, level, dc, components.Append(visibility).ToArray());
            ability.GetComponent<Occultist0099FocusResourceLogic>().Panoply = panoply;
            return Identify(ability, token, name, description, icon, text);
        }

        private static ContextValue Rank() => new ContextValue { ValueType = ContextValueType.Rank };

        private static AddContextStatBonus SkillBonus(StatType stat)
            => new AddContextStatBonus { name = "TrickstersSkill" + stat, Stat = stat, Value = Rank(), Descriptor = ModifierDescriptor.UntypedStackable };
    }

    [TypeId(Occultist0099Ids.PanoplyCarrierTypeId)]
    public sealed class Occultist0099PanoplyCarrier : AddFacts
    {
        public string Panoply;

        protected override void OnActivate()
        {
            base.OnActivate();
            Sync(true);
        }

        protected override void OnDeactivate()
        {
            Sync(false);
            base.OnDeactivate();
        }

        private void Sync(bool present)
            => Occultist0099NativeScope.InFactContext(Owner, Occultist0099Ids.FocusFactGuid,
                typeof(Occultist0099FocusFact0099), runtime => ((Occultist0099FocusFact0099)runtime).SyncPanoply(Panoply, present));
    }

    [TypeId(Occultist0099Ids.PanoplyValueTypeId)]
    public sealed class Occultist0099PanoplyValue : PropertyValueGetter
    {
        public string Panoply;
        public int Divisor = 1;
        public bool HalfLevelCap;
        public bool CapAtLevel;

        protected override int GetBaseValue(UnitEntityData unit)
            => Occultist0099NativeScope.InRuntimeContext(
                Occultist0099NativeScope.RuntimeOf(unit, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099)),
                runtime => ((Occultist0099FocusFact0099)runtime).PanoplyBonus(Panoply, Divisor, HalfLevelCap, CapAtLevel), 0);
    }

    [TypeId(Occultist0099Ids.PanoplyVisibilityTypeId)]
    public sealed class Occultist0099PanoplyVisibility : BlueprintComponent, IAbilityVisibilityProvider
    {
        public string Panoply;

        public bool IsAbilityVisible(AbilityData ability)
        {
            var unit = ability?.Caster?.Unit;
            return Occultist0099NativeScope.InRuntimeContext(
                Occultist0099NativeScope.RuntimeOf(unit, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099)),
                runtime => ((Occultist0099FocusFact0099)runtime).OwnsPanoply(Panoply), false);
        }
    }
}
