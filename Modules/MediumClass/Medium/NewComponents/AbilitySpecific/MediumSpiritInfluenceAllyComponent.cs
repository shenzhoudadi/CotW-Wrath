using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Utilities;
using MediumClass.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.NewComponents.AbilitySpecific
{
    // Allied recipient of the mythic Spirit Influence feature. Mirrors the
    // channeling Mediums' spirit bonus: same stats, same value, including
    // Spirit Focus. Overlapping spirits and overlapping Mediums both use the
    // highest value per stat; nothing stacks.
    [AllowedOn(typeof(BlueprintFeature), false)]
    [AllowMultipleComponents]
    [TypeId("cf15cdaf-d6bc-4521-9c2e-290a5050e5f8")]
    public class MediumSpiritInfluenceAllyComponent : UnitFactComponentDelegate<MediumSpiritInfluenceAllyComponent.AllyData>
    {
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(MediumSpiritInfluenceAllyComponent));

        public override void OnTurnOn() => RefreshModifiers();

        public override void OnTurnOff() => RemoveOwnModifiers();

        // Provider records the Medium currently holding the grant (it decides when
        // the fact is removed); the mirrored values come from every channeler.
        internal void SetProvider(UnitEntityData provider, EntityFact endingMarker = null)
        {
            Data.Provider = provider;
            RefreshModifiers(provider, endingMarker);
        }

        // Total re-evaluation: each stat gets the highest value any channeling
        // Medium offers, never the sum. `channeling` is the caller, known to be
        // channeling even if its marker is mid-activation; `endingMarker` is a
        // marker buff being turned off that must no longer count. `ownBonusEnding`
        // means the recipient's own spirit bonus buff is turning off right now.
        internal void RefreshModifiers(UnitEntityData channeling = null, EntityFact endingMarker = null, bool ownBonusEnding = false)
        {
            RemoveOwnModifiers();
            var best = new Dictionary<StatType, int>();
            foreach (var provider in Providers(channeling, endingMarker))
                CollectBonuses(provider, best);
            // A channeling recipient already gets its own bonus from its own buff,
            // so the mirror only tops it up to the best offer: max, never a sum.
            // A non-medium recipient offers nothing and keeps the full mirror.
            var own = new Dictionary<StatType, int>();
            var ownBuff = Owner.Buffs.GetBuff(BlueprintTool.Get<BlueprintBuff>(Guids.MediumSpiritBonusBuff));
            if (!ownBonusEnding && ownBuff != null && ownBuff.IsActive && ownBuff.IsTurnedOn)
                CollectBonuses(Owner, own);
            foreach (var bonus in best)
            {
                int ownOffer = own.TryGetValue(bonus.Key, out int offer) ? offer : 0;
                Owner.Stats.GetStat(bonus.Key)?.AddModifier(Math.Max(0, bonus.Value - ownOffer), Runtime, ModifierDescriptor.UntypedStackable);
            }
        }

        // The unit's own spirit bonus changed (seance started, ended, or was
        // refreshed): re-clamp the mirror it holds against the new own offer.
        internal static void RefreshOwnMirror(UnitEntityData unit, bool ownBonusEnding = false)
        {
            var fact = unit?.Facts.Get(BlueprintTool.Get<BlueprintFeature>(Guids.MediumSpiritInfluenceAlly));
            if (fact != null && fact.IsActive)
                fact.CallComponents<MediumSpiritInfluenceAllyComponent>(c => c.RefreshModifiers(ownBonusEnding: ownBonusEnding));
        }

        private IEnumerable<UnitEntityData> Providers(UnitEntityData channeling, EntityFact endingMarker)
        {
            var marker = BlueprintTool.Get<BlueprintBuff>(Guids.MediumSpiritInfluenceBuff);
            return new[] { channeling, Data.Provider }.Concat(Game.Instance.Player.ActiveCompanions)
                .Where(unit => unit != null && unit != Owner).Distinct()
                .Where(unit => unit == channeling || unit.Buffs.Enumerable.Any(buff => buff != endingMarker
                    && buff.IsActive && buff.Blueprint == marker));
        }

        private static void CollectBonuses(UnitEntityData provider, Dictionary<StatType, int> best)
        {
            var medium = provider.Get<UnitPartMedium>();
            if (medium == null || provider.Progression == null
                || medium.PrimarySpirit == null || !medium.Spirits.ContainsKey(medium.PrimarySpirit))
            {
                return;
            }
            foreach (var spirit in medium.ActiveSpiritClasses.Where(medium.Spirits.ContainsKey))
            {
                var entry = medium.Spirits[spirit].SpiritBonus;
                int amount = provider.Progression.Features.GetRank(entry.SpiritBonusFeature)
                    + MultiSpiritRules.EffectiveSpiritFocus(medium, spirit);
                foreach (var stat in entry.Stats ?? Array.Empty<StatType>())
                    if (!best.TryGetValue(stat, out int current) || amount > current) best[stat] = amount;
            }
        }

        private void RemoveOwnModifiers()
        {
            // Spirit stat definitions survive load/teardown even when the provider's
            // runtime spirit table does not; clear by runtime source like the Medium's
            // own bonus buff does.
            foreach (var component in BlueprintTool.Get<BlueprintFeature>(Guids.MediumChannelSpirit)
                .GetComponents<MediumSpiritComponent>())
                // Also clear pre-0.3.5 Marshal modifiers from this runtime source.
                foreach (var stat in (component.Stats ?? Array.Empty<StatType>()).Concat(MarshalSpiritRules.SurgeStats).Distinct())
                    Owner.Stats.GetStat(stat)?.RemoveModifiersFrom(Runtime);
        }

        public class AllyData
        {
            [JsonProperty] public UnitEntityData Provider;
        }
    }
}
