using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.FactLogic;
using MediumClass.Medium.NewActions;
using MediumClass.Utilities;
using UnityEngine;

namespace MediumClass.Medium
{
    /// <summary>
    /// Per-legend variants for the abilities that lend a legend's powers without
    /// channeling it (Astral Beacon, Trance of Many).
    ///
    /// The legend list is read from the channel feature itself instead of being
    /// listed here, so a legend added later — including a new legendary spirit —
    /// joins both abilities with no edit to either of them. What makes that work
    /// is <see cref="ContextActionApplySpirit"/>: it is the action that actually
    /// channels a legend, so its <c>Spirit</c> is the authoritative declaration
    /// of which legend a channel ability belongs to.
    /// </summary>
    internal static class LegendVariants
    {
        /// <summary>Channel abilities are named "MediumChannelSpiritAbility&lt;Legend&gt;".</summary>
        internal const string ChannelAbilityPrefix = "MediumChannelSpiritAbility";

        internal static IEnumerable<(BlueprintCharacterClassReference Spirit, BlueprintAbility Channel)> All()
        {
            var facts = ChannelSpirit.Feature?.GetComponents<AddFacts>().FirstOrDefault()?.Facts;
            if (facts == null) yield break;
            foreach (var fact in facts)
            {
                if (!(fact is BlueprintAbility channel)) continue;
                var spirit = SpiritOf(channel);
                if (spirit != null) yield return (spirit, channel);
            }
        }

        private static BlueprintCharacterClassReference SpiritOf(BlueprintAbility ability)
        {
            if (ability == null) return null;
            foreach (var run in ability.GetComponents<AbilityEffectRunAction>())
            {
                var actions = run.Actions?.Actions;
                if (actions == null) continue;
                foreach (var action in actions)
                    if (action is ContextActionApplySpirit apply && apply.Spirit != null) return apply.Spirit;
            }
            return null;
        }

        /// <summary>
        /// Asset-name suffix and localization key stem for a legend. Derived
        /// from the channel ability name, falling back to the class guid when a
        /// legend does not follow the convention.
        /// </summary>
        internal static string Suffix(BlueprintCharacterClassReference spirit, BlueprintAbility channel)
        {
            var name = channel?.name;
            if (!string.IsNullOrEmpty(name) && name.Length > ChannelAbilityPrefix.Length
                && name.StartsWith(ChannelAbilityPrefix, StringComparison.Ordinal))
                return name.Substring(ChannelAbilityPrefix.Length);
            return spirit.ToString();
        }

        internal static string Id(string purpose, BlueprintCharacterClassReference spirit) =>
            VariantId.Of(purpose, spirit.ToString());

        /// <summary>
        /// The legend's own display name. Channel abilities keep their text under
        /// "&lt;Legend&gt;.Name", so a variant can reuse the translated legend name
        /// instead of adding another localization entry per legend. A legend
        /// without such a key falls back to the shared ability name.
        /// </summary>
        internal static string NameKey(BlueprintCharacterClassReference spirit, BlueprintAbility channel, string fallback)
        {
            var key = Suffix(spirit, channel) + ".Name";
            var pack = LocalizationManager.CurrentPack;
            return pack != null && !string.IsNullOrEmpty(pack.GetText(key, reportUnknown: false)) ? key : fallback;
        }

        /// <summary>Legend icon: the channel ability already carries the spirit's own icon.</summary>
        internal static Sprite Icon(BlueprintAbility channel) => channel?.Icon;
    }
}
