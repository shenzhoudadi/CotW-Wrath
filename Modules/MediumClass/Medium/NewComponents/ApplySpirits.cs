using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Prowler;
using MediumClass.Utilities;
using MediumClass.Utils;
using Newtonsoft.Json;
using Owlcat.QA.Validation;
using Owlcat.Runtime.Core.Utils;
using TabletopTweaks.Core.NewUnitParts;
using UnityEngine;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.NewComponents
{
	// Token: 0x02001BB6 RID: 7094
	[TypeId("72326d37-dfb6-42a3-bd9c-24ee2201539b")]
	public class ApplySpirits : UnitFactComponentDelegate, IUnitReapplyFeaturesOnLevelUpHandler
	{
		private static readonly ModLogger Logger = Logging.GetLogger(nameof(ApplySpirits));
		// Resolve the current runtime owner instead of caching character state on a blueprint component.
		private UnitPartMedium medium => base.Owner.Get<UnitPartMedium>();

		public override void OnActivate()
		{
            if (!HasPrimarySpirit())
            {
                Logger.Log("Cannot apply spirit: state is missing. Save reconstruction requires investigation.");
                return;
            }
            this.TryApplySpirit();
		}

        public void HandleUnitReapplyFeaturesOnLevelUp()
        {
            if (!HasPrimarySpirit()) return;
            RefreshActiveSpirits();
            base.Owner.Ensure<UnitPartMediumPreparedSpells>().Sync();
            MediumClass.Medium.MediumSpiritSpellbookRules.ClampRemainingSlots(base.Owner.Descriptor);
        }

		public override void OnDeactivate()
		{
            // Astral Beacon and Trance of Many last as long as this channel
            // does: retire their buffs first so each component revokes exactly
            // the ranks it lent, instead of leaving them to the sweep below.
            MediumClass.Medium.SecondaryLend.RetireAll(Owner);
            var powers = MediumClass.Medium.MediumSpiritPowerCleanup.Catalogue(Owner);
            // Native removal detaches the marker before child-fact/UI callbacks.
            // Retire the choice first so such callbacks cannot regrant its powers.
            medium?.ClearChannelSelection();
            base.Owner.Get<UnitPartMediumPreparedSpells>()?.Clear();
            this.Revert(powers);
			
		}

        private bool HasPrimarySpirit()
        {
            return medium != null && medium.PrimarySpirit != null
                && medium.Spirits.ContainsKey(medium.PrimarySpirit);
        }

        // Instance wrappers keep every existing call site unchanged; the real
        // bodies take the owner so the Astral Beacon / Trance of Many buff
        // components (which have their own fact context) grant the same powers.
        private void AddPower(BlueprintFeatureReference reference, int ranks = 1) => AddPower(Owner, reference, ranks);

        /// <summary>Grants up to <paramref name="ranks"/> ranks; returns how many were added.</summary>
        private static int AddPower(UnitEntityData owner, BlueprintFeatureReference reference, int ranks = 1)
        {
            // Move/swift/overwrite powers are optional for several spirits.
            var blueprint = reference?.Get();
            if (blueprint == null || owner == null) return 0;
            int current = owner.Progression.Features.GetRank(blueprint);
            int added = 0;
            for (int i = current; i < ranks; i++) { owner.AddFact(blueprint); added++; }
            return added;
        }

        private void RemovePower(BlueprintFeatureReference reference) => RemovePower(Owner, reference);

        private static void RemovePower(UnitEntityData owner, BlueprintFeatureReference reference)
        {
            var blueprint = reference?.Get();
            if (blueprint == null || owner == null) return;
            // FeatureCollection.RemoveFact removes a single rank. These are
            // spirit-specific powers, so end every granted rank on teardown.
            int ranks = owner.Progression.Features.GetRank(blueprint);
            for (int i = 0; i < ranks; i++) owner.RemoveFact(blueprint);
        }

        /// <summary>
        /// Removes at most <paramref name="ranks"/> ranks, never more than the
        /// unit currently holds. A temporary grant must not strip a rank that a
        /// fully channeled spirit (or any other source) also provided.
        /// </summary>
        private static void RemoveRanks(UnitEntityData owner, BlueprintFeatureReference reference, int ranks)
        {
            var blueprint = reference?.Get();
            if (blueprint == null || owner == null || ranks <= 0) return;
            int removable = Math.Min(ranks, owner.Progression.Features.GetRank(blueprint));
            for (int i = 0; i < removable; i++) owner.RemoveFact(blueprint);
        }

		private void ApplySpiritSpellbook(BlueprintCharacterClassReference spirit)
        {
			int SpiritPowerRank = base.Owner.Progression.Features.GetRank(BlueprintTool.Get<BlueprintFeature>(Guids.SpiritPower)) - medium.ForgonePowers;
			if ((spirit.Get() == BlueprintTool.Get<BlueprintCharacterClass>(Guids.Hierophant) || spirit.Get() == BlueprintTool.Get<BlueprintCharacterClass>(Guids.Archmage)) && SpiritPowerRank > 0)
			{
				base.Owner.Progression.Features.RemoveFact(medium.Spirits[spirit].SpiritLesserPower.Get());
				base.Owner.Ensure<UnitPartMediumPreparedSpells>().Sync();
			}
		}
        private void CheckWeakerSpiritAndApply(BlueprintCharacterClassReference spirit)
        {
            int rank = Owner.Progression.Features.GetRank(BlueprintTool.Get<BlueprintFeature>(Guids.SpiritPower)) - medium.ForgonePowers;
            var entry = medium.Spirits[spirit];
            bool caster = spirit.Get() == BlueprintTool.Get<BlueprintCharacterClass>(Guids.Archmage)
                || spirit.Get() == BlueprintTool.Get<BlueprintCharacterClass>(Guids.Hierophant);
            // The caster lesser feature is a book prohibition, not a power to grant.
            if (rank >= 1 && !caster) AddPower(entry.SpiritLesserPower);
            else if (!caster) RemovePower(entry.SpiritLesserPower);
            if (rank >= 2)
            {
                int ranks = (spirit.Get() == BlueprintTool.Get<BlueprintCharacterClass>(Guids.Trickster)
                    || spirit.Get() == BlueprintTool.Get<BlueprintCharacterClass>(Guids.RobinHood))
                    ? Math.Max(1, ProwlerSpiritRules.SpiritClassLevel(Owner) / 2) : 1;
                AddPower(entry.SpiritIntermediatePower, ranks);
            }
            else
            {
                RemovePower(entry.SpiritIntermediatePower);
                RemovePower(entry.SpiritIntermediatePowerMove);
                RemovePower(entry.SpiritIntermediatePowerSwift);
            }
            if (rank >= 3)
            {
                AddPower(entry.SpiritGreaterPower);
                AddPower(entry.SpiritIntermediatePowerMove);
            }
            else
            {
                RemovePower(entry.SpiritGreaterPower);
                RemovePower(entry.SpiritIntermediatePowerMove);
            }
            if (rank >= 4)
            {
                AddPower(entry.SpiritSupremePower);
                AddPower(entry.SpiritIntermediatePowerSwift);
            }
            else
            {
                RemovePower(entry.SpiritSupremePower);
                RemovePower(entry.SpiritIntermediatePowerSwift);
            }
        }

        private void TryApplySpirit() => RefreshActiveSpirits();

        public void RefreshActiveSpirits()
        {
            if (!HasPrimarySpirit()) return;
            foreach (var spirit in medium.ActiveSpiritClasses.ToArray())
            {
                // A fully channeled spirit replaces Astral Beacon's secondary variants.
                RemovePower(medium.Spirits[spirit].OverwriteIntermediatePower);
                RemovePower(medium.Spirits[spirit].OverwriteGreaterPower);
                CheckWeakerSpiritAndApply(spirit);
                ApplySpiritSpellbook(spirit);
            }
            // Astral Beacon is no longer an all-day flag on the 20th level
            // feature: it is a two-round buff naming one legend, and
            // MediumAstralBeaconComponent grants/revokes that legend's
            // intermediate+greater+supreme powers through GrantSecondary.
            RefreshSpiritModifiers();
        }

        private void RefreshSpiritModifiers()
        {
            // Updating a feature rank or recalculating a buff's context does not
            // replace modifiers created from a fixed integer in OnTurnOn. Refresh
            // only these two already-enabled buffs in their own runtime context;
            // never deactivate the channel or invoke the whole buff lifecycle.
            foreach (var buff in Owner.Buffs.Enumerable.Where(b => b.IsActive && b.IsTurnedOn).ToArray())
            {
                if (buff.Blueprint == BlueprintTool.Get<BlueprintBuff>(Guids.MediumSpiritBonusBuff))
                    buff.CallComponents<MediumContextSpiritBonusComponent>(component => component.RefreshModifiers());
                else if (buff.Blueprint == BlueprintTool.Get<BlueprintBuff>(Guids.MediumInfluenceDebuff))
                    buff.CallComponents<MediumInfluencePenaltyComponent>(component => component.RefreshModifiers());
                else if (buff.Blueprint == BlueprintTool.Get<BlueprintBuff>(Guids.MediumSpiritInfluenceBuff))
                    buff.CallComponents<MediumContextSpiritInfluenceComponent>(component => component.RefreshAllies());
            }
            MediumSpiritualDefenseComponent.RefreshFor(Owner);
            MediumAdvancedWeaponTrainingComponent.RefreshFor(Owner);
        }

        /// <summary>
        /// What a temporary lend actually added, per power slot. Stored in the
        /// buff component's runtime data so the revoke removes exactly these
        /// ranks — never a rank a fully channeled spirit provided as well.
        /// </summary>
        public class SecondaryGrant
        {
            [JsonProperty] public int Intermediate;
            [JsonProperty] public int Move;
            [JsonProperty] public int Swift;
            [JsonProperty] public int Greater;
            [JsonProperty] public int Supreme;
            /// <summary>Highest tier granted so far: 0 = none, 1 = intermediate, 2 = +greater, 3 = +supreme.</summary>
            [JsonProperty] public int Tiers;
        }

        /// <summary>
        /// Astral Beacon / Trance of Many lend a legend's powers without
        /// channeling it. The buff components call these statics, so the
        /// overwrite-power handling and the Trickster rank scaling stay shared
        /// with the fully channeled path instead of being re-implemented.
        /// <paramref name="tiers"/>: 1 = intermediate only, 2 = +greater,
        /// 3 = +supreme. Passing the buff's existing <paramref name="grant"/>
        /// tops it up to a higher tier without re-granting what it already
        /// holds, which is how Trance of Many upgrades at 14th and 18th level.
        /// </summary>
        internal static SecondaryGrant GrantSecondary(UnitEntityData owner,
            BlueprintCharacterClassReference spirit, int tiers = 3, SecondaryGrant grant = null)
        {
            grant = grant ?? new SecondaryGrant();
            var entry = Entry(owner, spirit);
            if (entry == null) return grant;
            int ranks = (spirit.Get() == BlueprintTool.Get<BlueprintCharacterClass>(Guids.Trickster)
                    || spirit.Get() == BlueprintTool.Get<BlueprintCharacterClass>(Guids.RobinHood))
                ? Math.Max(1, ProwlerSpiritRules.SpiritClassLevel(owner) / 2) : 1;
            if (grant.Tiers < 1)
            {
                if (entry.OverwriteIntermediatePower?.Get() != null)
                    RemovePower(owner, entry.SpiritIntermediatePower);
                grant.Tiers = 1;
            }
            // Trickster and Robin Hood scale their intermediate power with
            // level, so a top-up after leveling must raise these ranks too.
            grant.Intermediate += AddPower(owner, entry.OverwriteIntermediatePower?.Get() != null
                ? entry.OverwriteIntermediatePower : entry.SpiritIntermediatePower, ranks);
            if (tiers >= 2 && grant.Tiers < 2)
            {
                if (entry.OverwriteGreaterPower?.Get() != null)
                {
                    RemovePower(owner, entry.SpiritGreaterPower);
                    grant.Greater = AddPower(owner, entry.OverwriteGreaterPower);
                }
                else grant.Greater = AddPower(owner, entry.SpiritGreaterPower);
                grant.Move = AddPower(owner, entry.SpiritIntermediatePowerMove);
                grant.Tiers = 2;
            }
            if (tiers >= 3 && grant.Tiers < 3)
            {
                grant.Supreme = AddPower(owner, entry.SpiritSupremePower);
                grant.Swift = AddPower(owner, entry.SpiritIntermediatePowerSwift);
                grant.Tiers = 3;
            }
            return grant;
        }

        internal static void RevokeSecondary(UnitEntityData owner, BlueprintCharacterClassReference spirit,
            SecondaryGrant grant)
        {
            if (grant == null) return;
            var entry = Entry(owner, spirit);
            if (entry == null) return;
            // If the legend was channeled for real while the buff ran, the
            // channel owns these powers now; tearing them down here would strip
            // the primary spirit.
            if (owner.Get<UnitPartMedium>()?.IsActiveSpirit(spirit) == true) return;
            RemoveRanks(owner, entry.OverwriteIntermediatePower?.Get() != null
                ? entry.OverwriteIntermediatePower : entry.SpiritIntermediatePower, grant.Intermediate);
            RemoveRanks(owner, entry.SpiritIntermediatePowerMove, grant.Move);
            RemoveRanks(owner, entry.SpiritIntermediatePowerSwift, grant.Swift);
            RemoveRanks(owner, entry.OverwriteGreaterPower?.Get() != null
                ? entry.OverwriteGreaterPower : entry.SpiritGreaterPower, grant.Greater);
            RemoveRanks(owner, entry.SpiritSupremePower, grant.Supreme);
        }

        private static UnitPartMedium.SpiritEntry Entry(UnitEntityData owner, BlueprintCharacterClassReference spirit)
        {
            var state = owner?.Get<UnitPartMedium>();
            var blueprint = spirit?.Get();
            if (state == null || blueprint == null) return null;
            // Blueprints resolve to one shared instance per GUID, so reference
            // equality is the same test IsActiveSpirit uses.
            foreach (var pair in state.Spirits)
                if (pair.Key?.Get() == blueprint) return pair.Value;
            return null;
        }

		// Token: 0x0600BDCF RID: 48591 RVA: 0x00318090 File Offset: 0x00316290
		private void Revert(BlueprintFeature[] powers)
		{
			List<Buff> list = base.Owner.Buffs.Enumerable.ToTempList<Buff>();
			foreach (Buff buff in list)
			{
				// Preserve the existing name-based cleanup for both shipped display languages.
				if (buff.Blueprint.Name.Contains("Trickster's Edge") || buff.Blueprint.Name.Contains("诡术师绝技")){
					base.Owner.Buffs.RemoveFact(buff);
				}
				if (buff.Blueprint.Name.Contains("Seance Boon") || buff.Blueprint.Name.Contains("降灵奖励"))
				{
					base.Owner.Buffs.RemoveFact(buff);
				}
			}

            MediumClass.Medium.MediumSpiritPowerCleanup.RevokeAll(Owner, powers);
            AddPower(BlueprintTool.GetRef<BlueprintFeatureReference>(Guids.MediumSpellcasterFeatProhibitArchmage));
            AddPower(BlueprintTool.GetRef<BlueprintFeatureReference>(Guids.MediumSpellcasterFeatProhibitHierophant));
            MediumClass.Medium.MediumInfluenceRules.Reset(Owner.Descriptor);
			MediumClass.Medium.MediumSpiritSpellbookRules.ClampRemainingSlots(base.Owner.Descriptor);
			base.ClearData();
		}
	}
}
