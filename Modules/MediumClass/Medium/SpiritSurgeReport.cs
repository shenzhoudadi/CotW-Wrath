using System.Collections.Generic;
using Kingmaker.EntitySystem.Stats;

namespace MediumClass.Medium
{
    /// <summary>
    /// What one Spirit Surge did, computed once and used both to apply the modifiers and to write the log,
    /// so the log can never disagree with the bonus that was actually granted.
    /// </summary>
    internal sealed class SpiritSurgeReport
    {
        internal SpiritSurgeReport(SurgeRoll roll) { Roll = roll; }

        internal SurgeRoll Roll { get; }
        internal List<Line> Lines { get; } = new List<Line>();

        /// <summary>One group of targets that received the same final bonus (stats, or concentration).</summary>
        internal sealed class Line
        {
            internal StatType[] Stats = new StatType[0];
            internal bool IsConcentration;
            /// <summary>Name of the medium whose Marshal spirit added a term; null when no Marshal term applies.</summary>
            internal string MarshalOwner;
            internal int MarshalSpiritBonus;
            internal int MarshalFocus;
            internal int Total;
        }
    }
}
