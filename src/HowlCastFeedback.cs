using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype
{
    // Casting feedback only: no checks during UI previews and no persistent
    // state. The native action remains responsible for saves, buffs and immunity.
    [TypeId("6dc33f096d0a4d1fa31af07926d3ec69")]
    public sealed class HowlCastFeedback : BlueprintComponent, IAbilityOnCastLogic
    {
        public ContextConditionHasBuffFromCaster AvailableAgain;
        public HowlShakenTarget Shaken;
        public AudibleAbilityTargetCondition Audible;
        public LocalizedString Summary, NoTargets;

        public void OnCast(AbilityExecutionContext context)
        {
            try {
                if (context?.MaybeCaster == null) return;
                var selector = context.AbilityBlueprint.GetComponent<AbilityTargetsAround>();
                if (selector == null) return;
                // One additional bounded, native 30-foot/LOS query per cast.
                // Never pre-trigger a save or a spell-resistance/immune rule.
                var targets = selector.Select(context, new TargetWrapper(context.MaybeCaster)).ToArray();
                int eligible = 0, immunity = 0, unshaken = 0, silenced = 0;
                foreach (var target in targets) {
                    using (context.GetDataScope(target)) {
                        if (!AvailableAgain.Check()) immunity++;
                        else if (!Shaken.Check()) unshaken++;
                        else if (!Audible.Check()) silenced++;
                        else eligible++;
                    }
                }
                string message = targets.Length == 0
                    ? string.Format(NoTargets.ToString(), context.MaybeCaster.CharacterName)
                    : string.Format(Summary.ToString(), context.MaybeCaster.CharacterName, targets.Length, eligible,
                        context.Params?.DC ?? 0, immunity, unshaken, silenced);
                ModEntry.LogSpellDiagnostic(message);
                EventBus.RaiseEvent<ILogMessageUIHandler>(h => h.HandleLogMessage(message));
            } catch (Exception exception) {
                // Optional presentation cannot interrupt the actual effect.
                ModEntry.LogSpellDiagnostic("Terrifying Howl target feedback failed: " + exception);
            }
        }
    }
}
