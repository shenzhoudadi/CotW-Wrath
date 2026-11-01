using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Utilities;
using MediumClass.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.NewComponents.AbilitySpecific
{
    [TypeId("923eef91-ff39-4733-87ba-e466f6848ee5")]
    class MediumSpiritFocusComponent : UnitFactComponentDelegate
    {
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(MediumSpiritFocusComponent));
        public override void OnTurnOn()
        {
            Owner.Ensure<UnitPartMedium>().AddSpiritFocus(Spirit);
            RefreshSpiritBonus();
        }

        public override void OnTurnOff()
        {
            Owner.Get<UnitPartMedium>()?.RemoveSpiritFocus(Spirit);
            RefreshSpiritBonus();
        }

        // Gaining or losing focus at level-up changes the live spirit bonus modifiers,
        // but the bonus buff only refreshes itself on its own activation.
        private void RefreshSpiritBonus()
        {
            foreach (var buff in Owner.Buffs.Enumerable.Where(b => b.IsActive && b.IsTurnedOn).ToArray())
            {
                if (buff.Blueprint == BlueprintTool.Get<BlueprintBuff>(Guids.MediumSpiritBonusBuff))
                    buff.CallComponents<MediumContextSpiritBonusComponent>(c => c.RefreshModifiers());
                else if (buff.Blueprint == BlueprintTool.Get<BlueprintBuff>(Guids.MediumSpiritInfluenceBuff))
                    buff.CallComponents<MediumContextSpiritInfluenceComponent>(c => c.RefreshAllies());
            }
        }

        public BlueprintCharacterClassReference Spirit;
    }
}
