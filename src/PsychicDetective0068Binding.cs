using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace CallOfTheWild.Wrath.Prototype
{
    // S-PSYCHIC-0068. One explicit binding replaces every hard-coded Psychic
    // identity in the shared amplification runtime. A component without the new
    // fields (an old blueprint or an old save) resolves byte-for-byte to the
    // original Psychic book, pool, class and mode scope.
    internal static class PsychicAmplificationBinding
    {
        internal static BlueprintGuid LegacyBookId => PsychicDetective0068Ids.LegacyBookId;

        internal static bool IsBound(PsychicAmplificationComponent c) => c != null && c.BoundBook != null;

        internal static BlueprintGuid BookId(PsychicAmplificationComponent c)
            => IsBound(c) ? c.BoundBook.Guid : LegacyBookId;

        internal static BlueprintGuid BookId(BlueprintBuff buff)
            => buff == null ? LegacyBookId : BookId(buff.GetComponent<PsychicAmplificationComponent>());

        internal static BlueprintGuid ScopeOf(EntityFact fact)
            => fact == null ? LegacyBookId : BookId(fact.Blueprint as BlueprintBuff);

        internal static bool InScope(BlueprintBuff buff, BlueprintGuid scope) => BookId(buff) == scope;

        internal static bool SameScope(EntityFact a, EntityFact b) => ScopeOf(a) == ScopeOf(b);

        internal static BlueprintAbilityResource Pool(PsychicAmplificationComponent c, UnitEntityData owner)
        {
            if (IsBound(c) && c.BoundPool != null) return c.BoundPool.Get();
            return PsychicDisciplineRuntime01458.Pool(owner);
        }

        internal static BlueprintAbilityResource Pool(BlueprintGuid bookId, UnitEntityData owner)
        {
            if (bookId == PsychicDetective0068Ids.BookId) return PsychicDetective0068Ids.DetectivePool(owner);
            return PsychicDisciplineRuntime01458.Pool(owner);
        }

        internal static int Level(PsychicAmplificationComponent c, UnitEntityData owner)
        {
            var cls = c?.CharacterClass?.Get();
            if (!IsBound(c) || cls == null) return PsychicDisciplineRuntime01458.Level(owner);
            return owner?.Progression?.GetClassLevel(cls) ?? 0;
        }

        internal static int ModeLimit(PsychicAmplificationComponent c, UnitEntityData owner)
            => IsBound(c) ? 1 : (PsychicAdvancedRuntime01458.HasDual(owner) ? 2 : 1);

        internal static bool Owns(PsychicAmplificationComponent c, AbilityData spell)
            => spell?.Spellbook?.Blueprint != null && spell.Spellbook.Blueprint.AssetGuid == BookId(c) &&
               PsychicCasting.IsPsychic(spell);

        internal static bool Owns(BlueprintGuid bookId, AbilityData spell)
            => spell?.Spellbook?.Blueprint != null && spell.Spellbook.Blueprint.AssetGuid == bookId &&
               PsychicCasting.IsPsychic(spell);

        // Shared effects must only ever be driven by a real, non-zero-circle
        // cast from the book that owns the mode. Items, previews, other books
        // and cantrips never pay from a bound pool.
        internal static bool Payable(PsychicAmplificationComponent c, AbilityData spell)
            => Owns(c, spell) && spell.SourceItem == null && spell.StickyTouch == null && spell.SpellLevel > 0;
    }

    // Q05. The original native descriptor component reads Reason.Context and
    // fact rank; this private component keeps that boundary but additionally
    // requires a real spell or spell-like ability source and reads the actual
    // HomebrewArchetypes Investigator level of the fact's owner.
    [TypeId("878620c7e3b466888f969af1289ca39d")]
    public sealed class PsychicDetectiveMeddler0068 : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleSavingThrow>, IInitiatorRulebookSubscriber
    {
        public BlueprintCharacterClassReference CharacterClass;
        public int MaximumBonus = 6;

        internal static int BonusFor(int level, int maximum)
        {
            if (level < 2) return 0;
            int value = level >= 17 ? 6 : level >= 14 ? 5 : level >= 11 ? 4 :
                        level >= 8 ? 3 : level >= 5 ? 2 : 1;
            return Math.Min(Math.Max(0, maximum), value);
        }

        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            if (!Fact.IsActive || !Fact.IsTurnedOn || evt.Reason?.Context == null) return;
            var context = evt.Reason.Context;
            if ((context.SpellDescriptor & SpellDescriptor.MindAffecting) == 0) return;
            var ability = context.SourceAbilityContext?.Ability;
            if (ability?.Blueprint == null) return;
            var type = ability.Blueprint.Type;
            if (type != AbilityType.Spell && type != AbilityType.SpellLike) return;
            var cls = CharacterClass?.Get();
            if (cls == null || Owner == null) return;
            int bonus = BonusFor(Owner.Progression.GetClassLevel(cls), MaximumBonus);
            if (bonus <= 0) return;
            // D-F04: only the save this event actually rolls is adjusted, the
            // same boundary as the existing Steadfast Personality handler.
            var stat = Owner.Stats.GetStat(RuleSavingThrow.GetStatFromSavingThrow(evt.Type));
            if (stat == null) return;
            evt.AddTemporaryModifier(stat.AddModifier(bonus, Runtime, ModifierDescriptor.UntypedStackable));
        }
        public void OnEventDidTrigger(RuleSavingThrow evt) { }
    }

    // Q07. Native CanSelect is the single boundary used by both the level-up UI
    // and SelectFeature.Check. Only the Investigator talent ancestry of a real
    // detective is restricted; an independent Alchemist or any other source
    // keeps its own native prerequisites and options.
    [HarmonyPatch(typeof(BlueprintFeatureSelection), nameof(BlueprintFeatureSelection.CanSelect))]
    internal static class PsychicDetectiveTalent0068Patch
    {
        private static readonly HashSet<BlueprintGuid> Forbidden = new HashSet<BlueprintGuid>(
            PsychicDetective0068Ids.ForbiddenDiscoveryIds.Select(BlueprintGuid.Parse));
        private static readonly BlueprintGuid TalentSelection = BlueprintGuid.Parse(PsychicDetective0068Ids.TalentSelectionId);
        private static readonly BlueprintGuid Discovery = BlueprintGuid.Parse(PsychicDetective0068Ids.AlchemistDiscoveryId);

        internal static bool HasDetective(UnitDescriptor unit)
        {
            var cls = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(BlueprintGuid.Parse(PsychicDetective0068Ids.ClassId));
            if (cls == null || unit?.Unit == null) return false;
            var data = unit.Progression?.GetClassData(cls);
            return data?.Archetypes?.Any(a => a != null && a.AssetGuid == PsychicDetective0068Ids.ArchetypeId) == true;
        }

        internal static bool FromInvestigatorTalents(FeatureSelectionState state)
        {
            for (var current = state; current != null; current = current.Parent)
            {
                if (current.Selection is BlueprintFeatureSelection selection &&
                    (selection.AssetGuid == TalentSelection || selection.AssetGuid == ExtraInvestigatorTalent.FeatId)) return true;
                var source = AccessTools.Field(typeof(FeatureSelectionState), "Source")?.GetValue(current);
                var property = source == null ? null : AccessTools.Property(source.GetType(), "Blueprint");
                var blueprint = property?.GetValue(source) as SimpleBlueprint;
                if (blueprint != null && (blueprint.AssetGuid == TalentSelection ||
                    blueprint.AssetGuid == ExtraInvestigatorTalent.FeatId)) return true;
            }
            return false;
        }

        private static bool Prefix(BlueprintFeatureSelection __instance, UnitDescriptor __0, LevelUpState __1,
            FeatureSelectionState __2, IFeatureSelectionItem __3, ref bool __result)
        {
            var feature = __3?.Feature;
            if (feature == null || !HasDetective(__0) || !FromInvestigatorTalents(__2)) return true;
            if (Forbidden.Contains(feature.AssetGuid)) { __result = false; return false; }
            // A nested selection with no legal remaining option must not be
            // offered as an empty UI choice. Keep it if another provider has
            // legitimately added a non-forbidden discovery.
            if (feature is BlueprintFeatureSelection nested && nested.AssetGuid == Discovery &&
                nested.AllFeatures.All(f => f == null || Forbidden.Contains(f.AssetGuid)))
            { __result = false; return false; }
            return true;
        }
    }
}
