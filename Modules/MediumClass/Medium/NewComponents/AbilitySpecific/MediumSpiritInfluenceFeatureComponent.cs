using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MediumClass.Utilities;
using MediumClass.Utils;
using System;
using System.Linq;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.NewComponents.AbilitySpecific
{
    // The mythic Spirit Influence feature can be gained while a seance is
    // already active, after the channel buff's facts-change trigger could have
    // fired. Apply or remove the marker buff from the feature itself so both
    // orders converge on the same state.
    [AllowedOn(typeof(Kingmaker.Blueprints.Classes.BlueprintFeature), false)]
    [TypeId("9dfba2b2-c40a-4b75-85b4-1e0fb07c2f13")]
    public class MediumSpiritInfluenceFeatureComponent : UnitFactComponentDelegate
    {
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(MediumSpiritInfluenceFeatureComponent));

        public override void OnTurnOn()
        {
            var channel = Owner.Buffs.Enumerable.FirstOrDefault(b => b.IsActive && b.Blueprint ==
                BlueprintTool.Get<BlueprintBuff>(Guids.MediumChannelSpiritPrimarySpiritBuff));
            if (channel == null) return;
            var marker = BlueprintTool.Get<BlueprintBuff>(Guids.MediumSpiritInfluenceBuff);
            if (Owner.Buffs.GetBuff(marker) != null) return;
            Owner.Buffs.AddBuff(marker, Owner, null);
        }

        public override void OnTurnOff()
        {
            var marker = BlueprintTool.Get<BlueprintBuff>(Guids.MediumSpiritInfluenceBuff);
            var buff = Owner.Buffs.GetBuff(marker);
            if (buff != null) Owner.Buffs.RemoveFact(buff);
        }
    }
}
