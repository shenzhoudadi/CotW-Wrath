using HarmonyLib;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;

namespace CallOfTheWild.Wrath.Prototype
{
    // Retain the serialized component identity/base type, but keep this effect
    // out of the global metamagic-query registry. Ordinary ability availability
    // must not reenter our eligibility filter through that registry.
    [AllowedOn(typeof(BlueprintBuff),false)]
    [TypeId("ad3a5f8dbf0f436cbdde2c0e2bf1b1c8")]
    public sealed class PsychicDivideMindMetamagic : AutoMetamagic,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>
    {
        protected override void OnTurnOn() => Owner.Get<UnitPartSpecialMetamagic>()?.Remove(Fact,this);
        protected override void OnTurnOff() => Owner.Get<UnitPartSpecialMetamagic>()?.Remove(Fact,this);
        public new void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if(evt.Blueprint is BlueprintAbility ability && PsychicDivideMindPatch.Eligible(ability,evt.AbilityData))
                evt.AddMetamagic(Metamagic.Quicken);
        }
        public new void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
    }

    [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.ActionType),MethodType.Getter)]
    internal static class PsychicDivideMindActionPatch
    {
        private static readonly BlueprintGuid Mind=PsychicAssets.Id("DivideMindBuff");
        [HarmonyPostfix]
        private static void Postfix(AbilityData __instance,ref UnitCommand.CommandType __result)
        {
            if(__result!=UnitCommand.CommandType.Standard && __result!=UnitCommand.CommandType.Move)return;
            if(__instance.Caster?.Buffs==null || !__instance.Caster.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==Mind && b.IsTurnedOn && !b.IsSuppressed))return;
            if(PsychicDivideMindPatch.Eligible(__instance.Blueprint,__instance))__result=UnitCommand.CommandType.Swift;
        }
    }

    [HarmonyPatch(typeof(AutoMetamagic),nameof(AutoMetamagic.ShouldApplyTo))]
    internal static class PsychicDivideMindPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(AutoMetamagic c,BlueprintAbility ability,AbilityData data,ref bool __result)
        {
            if(!(c is PsychicDivideMindMetamagic))return true;
            __result=Eligible(ability,data);return false;
        }
        internal static bool Eligible(BlueprintAbility ability,AbilityData data)
        {
            // Never recurse via HasMetamagic/ActionType/RequireFullRoundAction:
            // those properties ask this native metamagic registry themselves.
            if(ability==null || data==null || data.SourceItem!=null || data.SpellLevel>5 || data.SpellLevel<0 || ability.IsFullRoundAction)return false;
            if(ability.ActionType!=UnitCommand.CommandType.Standard && ability.ActionType!=UnitCommand.CommandType.Move)return false;
            if(ability.Type!=AbilityType.SpellLike && !PsychicCasting.IsPsychic(data))return false;
            if(data.IsSpontaneous && !data.IsArcanist && data.MetamagicData!=null && data.MetamagicData.NotEmpty && !data.MetamagicData.Has(Metamagic.Quicken))return false;
            return true;
        }
    }

    [AllowedOn(typeof(BlueprintBuff),false)]
    [TypeId("faef7ea32db04e20b9e13105922aa6a9")]
    public sealed class PsychicLocateWeakness : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>,IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            var attack=evt.ParentRule?.AttackRoll;
            var weapon=attack?.WeaponStats?.Weapon;
            var damage=evt.ParentRule?.DamageBundle?.WeaponDamage;
            if(damage==null || weapon==null || !attack.IsCriticalConfirmed || (evt.CritImmunity && !evt.IgnoreCritImmunity))return;
            if((attack.ImmuneToCriticalHit && !attack.IgnoreCritImmunity) || attack.FortificationNegatesCriticalHit)return;
            if(weapon.Blueprint.Category==WeaponCategory.Ray || weapon.Blueprint.Category==WeaponCategory.Touch)return;
            damage.CalculationType.Set(DamageCalculationType.Maximized,Fact);
        }
        public void OnEventDidTrigger(RuleCalculateDamage evt) { }
    }
}
