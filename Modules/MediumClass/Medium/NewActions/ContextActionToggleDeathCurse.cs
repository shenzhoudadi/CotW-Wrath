using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MediumClass.Utils;
using Owlcat.Runtime.Core.Logging;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.NewActions
{
    /// <summary>
    /// Run action of each Death Curse energy variant. It replaces 0.3.3's "has buff ? remove : apply"
    /// conditional so the two-energy limit is decided where the toggle is really executed,
    /// not only where the button is drawn.
    /// </summary>
    [TypeId("8312c244-24dc-4983-aa1a-68f8677f61c5")]
    public class ContextActionToggleDeathCurse : ContextAction
    {
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(ContextActionToggleDeathCurse));

        public BlueprintBuffReference m_Buff;

        public override string GetCaption() => "Toggle a Death Curse energy (at most two active)";

        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            var blueprint = m_Buff?.Get();
            if (caster == null || blueprint == null)
            {
                LogChannel.Default.Error(this, "ContextActionToggleDeathCurse: caster or buff is missing", Array.Empty<object>());
                return;
            }
            var outcome = MediumDeathCurseRules.Toggle(caster, blueprint,
                switchOn: () => caster.AddBuff(blueprint, Context, null),
                switchOff: buff => caster.Buffs.RemoveFact(buff));
            if (outcome == MediumDeathCurseRules.ToggleOutcome.LimitReached) Warn(MediumDeathCurseRules.LimitMessage());
        }

        // A warning is UI only; it can never undo or break the decision above.
        private static void Warn(string text)
        {
            try
            {
                EventBus.RaiseEvent<IWarningNotificationUIHandler>(h => h.HandleWarning(text, true));
            }
            catch (Exception e)
            {
                try { Logger.LogException("Could not show the Death Curse limit warning", e); }
                catch { /* logging is optional too */ }
            }
        }
    }
}
