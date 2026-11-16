using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Localization;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Newtonsoft.Json;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099AbjurationPowers
    {
        internal const string DispelRoot = "92681f181b507b34ea87018e8f7a528a";
        internal const string TargetedDispel = "143775c49ae6b7446b805d3b2e702298";
        internal const string EnergyProtection = "d2f116cfe05fcdd4a94e80143b67046f";
        private const string School = Occultist0099Ids.SliceSchool;

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache, Func<string, string, string, LocalizedString> text)
        {
            var dispelGuid = BlueprintGuid.Parse(TargetedDispel);
            var dispel = cache.Load(dispelGuid) as BlueprintAbility;
            var action = dispel?.GetComponent<AbilityEffectRunAction>();
            if (dispel?.AssetGuid != dispelGuid || dispel.name != "DispelMagicTarget" || dispel.Type != AbilityType.Spell ||
                dispel.GetComponent<SpellComponent>()?.School != SpellSchool.Abjuration ||
                PsychicSpellIntegration.ParentGuid(dispel) != BlueprintGuid.Parse(DispelRoot) ||
                action?.Actions?.Actions?.OfType<ContextActionDispelMagic>().Count() != 1) return null;
            var protectionGuid = BlueprintGuid.Parse(EnergyProtection);
            var protection = cache.Load(protectionGuid) as BlueprintAbility;
            if (protection?.AssetGuid != protectionGuid || protection.name != "ProtectionFromEnergy") return null;

            var level = Occultist0099SchoolValue.Create("AbjurationEffectiveLevel", School, Occultist0099SchoolValueKind.EffectiveLevel);
            var spellLevel = Occultist0099SchoolValue.Create("AbjurationDCLevel", School, Occultist0099SchoolValueKind.DCLevel);
            var absorption = new ContextRankConfig { name = "EnergyShieldCapacity" };
            SetEnum(absorption, "m_BaseValueType", "CustomProperty");
            Set(absorption, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(level));
            SetEnum(absorption, "m_Progression", "MultiplyByModifier");
            Set(absorption, "m_StepLevel", 5);
            var capacity = new ContextValue { ValueType = ContextValueType.Rank };
            const string shieldText = "As a swift action, spend 1 point of Abjuration focus to absorb acid, cold, electricity and fire damage. The shield absorbs a total of 5 damage per occultist level across those energy types and lasts for 1 minute or until exhausted.";
            var shieldBuff = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                ComponentsArray = new BlueprintComponent[] { absorption, new Occultist0099EnergyShield
                    { name = "OccultistEnergyShield", UsePool = true, Pool = capacity, Value = capacity } } },
                "EnergyShieldBuff", "Energy Shield", shieldText, protection, text);
            var shield = Power("EnergyShieldAbility", "Energy Shield", shieldText, protection, UnitCommand.CommandType.Swift, 1,
                AbilityRange.Personal, Run(Apply(shieldBuff, 1, DurationRate.Minutes)));

            const string globeText = "As a standard action, spend 3 points of Abjuration focus to move within a globe that negates incoming spells. It negates spells totaling up to your occultist level in spell levels and collapses when its capacity is exhausted.";
            var globeBuff = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                ComponentsArray = new BlueprintComponent[] { new Occultist0099GlobeOfNegation
                    { name = "OccultistGlobeOfNegation", Capacity = PropertyValue(level) } } },
                "GlobeOfNegationBuff", "Globe of Negation", globeText, protection, text);
            var applyGlobe = Apply(globeBuff, 0);
            applyGlobe.Permanent = true;
            var globe = Power("GlobeOfNegationAbility", "Globe of Negation", globeText, protection, UnitCommand.CommandType.Standard, 3,
                AbilityRange.Personal, Run(applyGlobe));

            const string dispelText = "As a standard action, spend 1 point of Abjuration focus to use targeted dispel magic on yourself or an adjacent creature.";
            var unravel = Power("UnravelingAbility", "Unraveling", dispelText, dispel, UnitCommand.CommandType.Standard, 1,
                AbilityRange.Touch, CopyData(action));
            return new BlueprintScriptableObject[] { level, spellLevel, shieldBuff, shield, globeBuff, globe, unravel };

            BlueprintAbility Power(string token, string title, string description, BlueprintAbility donor, UnitCommand.CommandType command,
                int cost, AbilityRange range, AbilityEffectRunAction effect)
            {
                bool touch = range == AbilityRange.Touch;
                var ability = new BlueprintAbility { Type = AbilityType.SpellLike, ActionType = command,
                    Range = range, CanTargetSelf = true, CanTargetFriends = touch, CanTargetEnemies = touch,
                    EffectOnEnemy = touch ? AbilityEffectOnUnit.Harmful : AbilityEffectOnUnit.None,
                    EffectOnAlly = touch ? AbilityEffectOnUnit.Harmful : AbilityEffectOnUnit.Helpful,
                    ComponentsArray = new BlueprintComponent[]
                    {
                        effect, new SpellComponent { name = "OccultistPowerSchool", School = SpellSchool.Abjuration },
                        new Occultist0099FocusResourceLogic { name = "OccultistPowerCost", School = School, Amount = cost },
                        new ContextCalculateAbilityParams { name = "OccultistPowerParams", StatType = StatType.Intelligence,
                            ReplaceCasterLevel = true, CasterLevel = PropertyValue(level),
                            ReplaceSpellLevel = true, SpellLevel = PropertyValue(spellLevel) }
                    } };
                return Identify(ability, token, title, description, donor, text);
            }
        }
    }

    [TypeId(Occultist0099Ids.EnergyShieldTypeId)]
    public sealed class Occultist0099EnergyShield : AddDamageResistanceEnergy
    {
        // One native resistance chunk shares the pool even inside a single mixed damage packet.
        protected override bool Bypassed(ComponentRuntime runtime, BaseDamage damage, ItemEntityWeapon weapon)
            => !(damage is EnergyDamage energy) ||
                (energy.EnergyType != DamageEnergyType.Acid && energy.EnergyType != DamageEnergyType.Cold &&
                 energy.EnergyType != DamageEnergyType.Electricity && energy.EnergyType != DamageEnergyType.Fire) ||
                runtime.Owner.State.HasCondition(UnitCondition.SuppressedEnergyResistance);
    }

    public sealed class Occultist0099GlobeData
    {
        [JsonProperty] public int Remaining;
        [JsonProperty] public TimeSpan Started;
        [JsonIgnore] internal ConditionalWeakTable<MechanicsContext, object> ProtectedCasts = new ConditionalWeakTable<MechanicsContext, object>();
    }

    [TypeId(Occultist0099Ids.GlobeOfNegationTypeId)]
    public sealed class Occultist0099GlobeOfNegation : UnitBuffComponentDelegate<Occultist0099GlobeData>
    {
        private sealed class DeliveryReceipt
        {
            internal TimeSpan ProtectionStarted;
            internal TimeSpan? CollapsedAt;
        }

        private static readonly BlueprintGuid Identity = BlueprintGuid.Parse(Occultist0099Ids.Id("GlobeOfNegationBuff"));
        private static readonly ConditionalWeakTable<MechanicsContext, ConditionalWeakTable<UnitEntityData, DeliveryReceipt>> BlockedDeliveries =
            new ConditionalWeakTable<MechanicsContext, ConditionalWeakTable<UnitEntityData, DeliveryReceipt>>();
        private static readonly object Blocked = new object();
        public ContextValue Capacity;

        protected override void OnActivate()
        {
            Data.Remaining = Capacity.Calculate(Context);
            Data.Started = Game.Instance.TimeController.GameTime;
        }

        internal static bool TryBlock(UnitEntityData target, MechanicsContext context, TimeSpan? areaCreated)
        {
            int level = context?.Params?.SpellLevel ?? 0;
            if (target == null || context?.SourceAbility?.Type != AbilityType.Spell || level <= 0) return false;
            var key = context.SourceAbilityContext ?? context;
            if (BlockedDeliveries.TryGetValue(key, out var targets) && targets.TryGetValue(target, out var blocked) &&
                (areaCreated == null || (areaCreated >= blocked.ProtectionStarted &&
                    blocked.CollapsedAt == Game.Instance.TimeController.GameTime))) return true;
            var active = target.Buffs?.Enumerable.FirstOrDefault(buff => buff.Blueprint.AssetGuid == Identity && buff.IsTurnedOn && !buff.IsSuppressed);
            if (active == null) return false;
            bool absorbed = false;
            active.CallComponents<Occultist0099GlobeOfNegation>(globe =>
                absorbed = globe.Absorb(key, level, areaCreated));
            return absorbed;
        }

        private bool Absorb(MechanicsContext key, int spellLevel, TimeSpan? areaCreated)
        {
            if (Data.Remaining <= 0 || areaCreated < Data.Started) return false;
            if (Data.ProtectedCasts.TryGetValue(key, out _)) return true;
            Data.ProtectedCasts.Add(key, Blocked);
            var receipt = BlockedDeliveries.GetOrCreateValue(key).GetValue(Owner, _ => new DeliveryReceipt());
            receipt.ProtectionStarted = Data.Started;
            Data.Remaining = Math.Max(0, Data.Remaining - spellLevel);
            if (Data.Remaining == 0)
            {
                // Finish the terminal delivery across both native paths. Later
                // area ticks can admit the bearer once the protection is gone.
                receipt.CollapsedAt = Game.Instance.TimeController.GameTime;
                Buff.Remove();
            }
            return true;
        }
    }
}
