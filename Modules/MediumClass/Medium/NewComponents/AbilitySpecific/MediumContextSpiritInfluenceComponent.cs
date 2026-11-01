using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
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
    // Lives on the hidden marker buff the mythic Spirit Influence feature applies
    // to the Medium while a seance is active. Grants the mirrored spirit bonus
    // feature to every active companion and refreshes or removes it with the
    // channel. The Medium itself is excluded: it already has its own bonus.
    [AllowedOn(typeof(BlueprintBuff), false)]
    [AllowMultipleComponents]
    [TypeId("0738a60b-f94c-44c9-957e-1873180f8c40")]
    public class MediumContextSpiritInfluenceComponent : UnitFactComponentDelegate<MediumContextSpiritInfluenceComponent.InfluenceData>
    {
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(MediumContextSpiritInfluenceComponent));

        public override void OnTurnOn()
        {
            RefreshAllies();
        }

        public override void OnTurnOff()
        {
            foreach (var grant in Data.Grants.ToArray())
            {
                if (grant.Target == null || grant.Fact == null || grant.Fact.IsDisposed) continue;
                if (!TryTransferGrant(grant)) grant.Target.RemoveFact(grant.Fact);
            }
            Data.Grants.Clear();
            // Mirrors held by another Medium may have been using this Medium's
            // value; recompute them without it so the runner-up takes over.
            var allyBlueprint = BlueprintTool.Get<BlueprintFeature>(Guids.MediumSpiritInfluenceAlly);
            foreach (var target in Recipients())
            {
                var fact = target.Facts.Get(allyBlueprint);
                if (fact != null && fact.IsActive)
                    fact.CallComponents<MediumSpiritInfluenceAllyComponent>(c => c.RefreshModifiers(null, Fact));
            }
        }

        internal void RefreshAllies()
        {
            UnitPartMedium medium = Owner.Get<UnitPartMedium>();
            if (medium?.PrimarySpirit == null || !medium.Spirits.ContainsKey(medium.PrimarySpirit))
            {
                return;
            }
            var allyBlueprint = BlueprintTool.Get<BlueprintFeature>(Guids.MediumSpiritInfluenceAlly);
            foreach (var target in Recipients())
            {
                var fact = target.Facts.Get(allyBlueprint);
                if (fact == null || !fact.IsActive)
                {
                    if (target.HasFact(allyBlueprint)) continue;
                    fact = target.AddFact(allyBlueprint);
                    fact?.CallComponents<MediumSpiritInfluenceAllyComponent>(c => c.SetProvider(Owner));
                    TrackGrant(target, fact);
                }
                else fact.CallComponents<MediumSpiritInfluenceAllyComponent>(c => c.RefreshModifiers(Owner));
            }
            Data.Version = 1;
        }

        private IEnumerable<UnitEntityData> Recipients() =>
            Game.Instance.Player.ActiveCompanions.Where(unit => unit != null && unit != Owner).Distinct();

        private void TrackGrant(UnitEntityData target, EntityFact fact)
        {
            if (fact != null && !Data.Grants.Any(grant => grant.Target == target && grant.Fact == fact))
                Data.Grants.Add(new Grant { Target = target, Fact = fact });
        }

        private bool TryTransferGrant(Grant grant)
        {
            // Two Mediums can share the same ally fact. Hand it to a remaining
            // provider instead of erasing the ally's mirrored bonus. The target
            // never holds its own mirror: it cannot be its own provider.
            var marker = BlueprintTool.Get<BlueprintBuff>(Guids.MediumSpiritInfluenceBuff);
            foreach (var provider in Recipients().Where(unit => unit != grant.Target))
            {
                var medium = provider.Get<UnitPartMedium>();
                if (medium == null || medium.PrimarySpirit == null
                    || !medium.Spirits.ContainsKey(medium.PrimarySpirit)) continue;
                foreach (var buff in provider.Buffs.Enumerable.Where(buff => buff != Fact
                    && buff.IsActive && buff.Blueprint == marker).ToArray())
                {
                    bool transferred = false;
                    buff.CallComponents<MediumContextSpiritInfluenceComponent>(component =>
                    {
                        component.TrackGrant(grant.Target, grant.Fact);
                        transferred = true;
                    });
                    if (transferred)
                    {
                        grant.Fact.CallComponents<MediumSpiritInfluenceAllyComponent>(c => c.SetProvider(provider, Fact));
                        return true;
                    }
                }
            }
            return false;
        }

        public class InfluenceData
        {
            [JsonProperty] public int Version;
            [JsonProperty] public List<Grant> Grants = new List<Grant>();
        }
        public class Grant
        {
            [JsonProperty] public UnitEntityData Target;
            [JsonProperty] public EntityFact Fact;
        }
    }
}
