using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;

namespace MediumClass.Medium
{
    /// <summary>
    /// Bookkeeping for the buffs that lend a legend the medium is not
    /// channeling (Astral Beacon, Trance of Many).
    ///
    /// Only one such lend may be active at a time: picking another legend
    /// retires the previous buff, and ending the channel retires all of them.
    /// Without this the two abilities could lend two different legends, or the
    /// same legend twice, and the borrowed ranks would stack.
    /// </summary>
    internal static class SecondaryLend
    {
        private static readonly List<BlueprintBuff> Blueprints = new List<BlueprintBuff>();

        internal static void Track(BlueprintBuff buff)
        {
            if (buff != null && !Blueprints.Contains(buff)) Blueprints.Add(buff);
        }

        internal static void Retire(UnitEntityData owner, BlueprintBuff keep)
        {
            if (owner == null) return;
            foreach (var buff in Blueprints)
                if (buff != null && buff != keep && owner.HasFact(buff)) owner.Buffs.RemoveFact(buff);
        }

        internal static void RetireAll(UnitEntityData owner) => Retire(owner, null);
    }
}
