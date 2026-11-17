using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.AreaLogic.SummonPool;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype
{
    // Soulbound Puppet grants a native familiar bond feature for a limited time.
    // The character's own permanent familiar must still block the power, while a
    // puppet this ability already granted must not block replacing it.
    [TypeId(Occultist0099Ids.SoulboundPuppetTypeId)]
    public sealed class Occultist0099SoulboundPuppet : BlueprintComponent, IAbilityCasterRestriction, IAbilityVisibilityProvider
    {
        public BlueprintUnitFactReference[] Familiars;
        public BlueprintBuffReference[] Puppets;

        public bool IsAbilityVisible(AbilityData ability) => IsCasterRestrictionPassed(ability?.Caster?.Unit);

        public string GetAbilityCasterRestrictionUIText() => "You already have a familiar of your own.";

        public bool IsCasterRestrictionPassed(UnitEntityData caster)
        {
            if (caster == null || Familiars == null || Puppets == null || Familiars.Length != Puppets.Length) return false;
            var granted = new Dictionary<BlueprintGuid, int>();
            for (int i = 0; i < Puppets.Length; i++)
            {
                var buff = Puppets[i]?.Get();
                var familiar = Familiars[i]?.Get();
                if (buff == null || familiar == null) continue;
                if (!caster.Buffs.Enumerable.Any(b => b.Blueprint == buff && b.IsTurnedOn && !b.IsSuppressed)) continue;
                granted[familiar.AssetGuid] = granted.TryGetValue(familiar.AssetGuid, out int owned) ? owned + 1 : 1;
            }
            foreach (var reference in Familiars)
            {
                var familiar = reference?.Get();
                if (familiar == null) continue;
                granted.TryGetValue(familiar.AssetGuid, out int ours);
                if (caster.Facts.List.Count(fact => !fact.IsDisposed && fact.Blueprint == familiar) > ours) return false;
            }
            return true;
        }
    }

    [TypeId(Occultist0099Ids.ServantTargetTypeId)]
    public sealed class Occultist0099ServantTarget : BlueprintComponent, IAbilityTargetRestriction
    {
        public BlueprintFeatureReference Consumed;
        public BlueprintUnitFactReference[] ExcludedTypes;

        public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target)
            => Occultist0099AnimateServant.Animatable(caster, target?.Unit, Consumed?.Get(), ExcludedTypes);

        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target)
            => "This corpse cannot be raised as a servant.";
    }

    [TypeId(Occultist0099Ids.ServantConditionTypeId)]
    public sealed class Occultist0099ServantCondition : ContextCondition
    {
        public BlueprintFeatureReference Consumed;
        public BlueprintUnitFactReference[] ExcludedTypes;

        protected override string GetConditionCaption() => "The target is a corpse this occultist can raise";

        protected override bool CheckCondition()
            => Occultist0099AnimateServant.Animatable(Context?.MaybeCaster, Target?.Unit, Consumed?.Get(), ExcludedTypes);
    }

    // Validates and consumes a real corpse, then delegates the spawn itself to the
    // native summon action. AddClassLevels reads RuleSummonUnit.Level, so the
    // native skeleton already arrives with the requested Hit Dice.
    [TypeId(Occultist0099Ids.AnimateServantTypeId)]
    public sealed class Occultist0099AnimateServant : ContextAction
    {
        public BlueprintSummonPoolReference Pool;
        public BlueprintFeatureReference Consumed;
        public BlueprintUnitFactReference[] ExcludedTypes;
        public ContextValue MaximumUnits;
        public ContextActionSpawnMonster Spawn;

        public override string GetCaption() => "Raise a corpse as a necromantic servant";

        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            var corpse = Target?.Unit;
            var consumed = Consumed?.Get();
            var pool = Pool?.Get();
            if (Spawn == null || pool == null || !Animatable(caster, corpse, consumed, ExcludedTypes)) return;
            int limit = MaximumUnits?.Calculate(Context) ?? 0;
            int before = ServantsOf(caster, pool);
            if (limit <= 0 || before >= limit) return;
            Spawn.RunAction();
            // Only a corpse that actually produced a servant is retired.
            if (ServantsOf(caster, pool) > before) corpse.Descriptor.AddFact(consumed);
        }

        internal static bool Animatable(UnitEntityData caster, UnitEntityData corpse, BlueprintFeature consumed,
            BlueprintUnitFactReference[] excludedTypes)
        {
            if (caster == null || corpse == null || consumed == null || corpse == caster || corpse.IsPlayerFaction) return false;
            if (!corpse.State.IsDead || corpse.State.HasCondition(UnitCondition.Petrified)) return false;
            if (corpse.Descriptor.HasFact(consumed) || corpse.Descriptor.OriginalSize > Size.Colossal) return false;
            if (excludedTypes == null) return false;
            foreach (var reference in excludedTypes)
            {
                var type = reference?.Get();
                if (type != null && corpse.Descriptor.HasFact(type)) return false;
            }
            return true;
        }

        internal static int ServantsOf(UnitEntityData caster, BlueprintSummonPool pool)
        {
            var live = Kingmaker.Game.Instance?.SummonPools?.GetPool(pool);
            if (live == null) return 0;
            return live.Units.Count(unit => unit != null && !unit.IsDisposed &&
                unit.Get<UnitPartSummonedMonster>()?.Summoner == caster);
        }
    }
}