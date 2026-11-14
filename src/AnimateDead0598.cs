// COTW-0598 Animate Dead functions more like tabletop rules.
//
// Ported from Call of the Wild (Kingmaker) NewMechanics/DeadTargetMechanics.cs
// (ContextActionAnimateDead, AbilityTargetCanBeAnimated,
// ContextConditionCanBeAnimated) and Features/NewSpells.cs
// createAnimateDeadLesser.
//
// What the tabletop spell does
// ----------------------------
//   "This spell turns the bones or bodies of dead creatures into undead
//    skeletons or zombies that obey your spoken commands. The undead you create
//    remain under your control. You can't create more HD of undead than twice
//    your caster level with a single casting, and no matter how many times you
//    use this spell you can control only 4 HD worth of undead creatures per
//    caster level."
//
// What Wrath ships
// ----------------
// Spells/Level4/AnimateDead.jbp and Spells/Level3/AnimateDeadLesser.jbp are the
// same point-target summon: one ContextActionSpawnMonster that spawns copies of
// Units/NPC/Commoners/Skeleton/SummonedSkeletonChampion (a fixed 2 HD fighter
// skeleton) for 1 round per caster level. Nothing about either reads a corpse,
// so the spells work in an empty room and the result has nothing to do with
// what was lying on the floor. The lesser spell differs only in its spell
// lists, its rank config and a smaller CountValue.
//
// What this module changes
// ------------------------
//   * The spawn action of both spells is replaced by
//     ContextActionAnimateDead0598, which raises exactly one skeleton from one
//     corpse.
//   * The corpse supplies the skeleton's Hit Dice, size and physical ability
//     scores (Dexterity +2, the skeleton template's own adjustment), and its
//     gear is copied onto the result - the original CotW behaviour, kept
//     because a reanimated champion still swinging his own sword is the point
//     of the spell.
//   * A single casting is limited to 2 HD per caster level, and total control
//     to 4 HD per caster level, both counted over the skeletons this caster has
//     animated. The pool is a dedicated BlueprintSummonPool shared by both
//     spells, so normal summon-monster spells are not charged against it.
//   * Duration becomes 10 minutes per caster level.
//   * The lesser spell keeps its tabletop restriction: only a Small or Medium
//     corpse can be raised.
//   * A corpse that has been animated carries a marker fact, so it cannot be
//     raised twice; the skeleton itself is flagged unlootable.
//
// Targeting
// ---------
// Wrath keeps the "can this ability be cast at a dead unit" answer in
// BlueprintAbility.CanCastToDeadTarget, which is nothing more than
// `GetComponent<ICanTargetDeadUnits>() != null` (verified from IL: ldarg.0,
// call GetComponent<ICanTargetDeadUnits>, ldnull, cgt.un). CotW had to Harmony
// patch that getter because Kingmaker had no such component; Wrath ships
// AbilityCanTargetDead, a bare marker, so the port adds the marker instead of
// patching anything.
//
// Both spells stay point-targeted like the natives, because a unit target on a
// dead enemy fails the native friends/enemies branch (UnitEntityData.CanAttack
// is false for a corpse). Clicking a corpse therefore arrives as a ground
// point, and the action resolves the corpse by proximity. A target restriction
// still runs whenever the cursor hovers an actual unit, so living creatures
// show the red "cannot target" state instead of silently eating the spell slot.
//
// Deliberate omissions
// --------------------
//   * No renaming. CotW renamed the result to "Animated <name> (<n> HD)";
//     Wrath's UnitDescriptor has no CustomName, so the module leaves the
//     blueprint name alone.
//   * No battle-log refusal messages. CotW's Common.AddBattleLogMessage does not
//     exist here; the refusal reason rides on the target restriction's tooltip
//     instead.
//   * The native AfterSpawn list is reused when it can be found, so Augment
//     Summoning, Commanding Infusion and the Reanimator archetype keep working
//     on the result. It is looked up by walking the action graph rather than by
//     assuming Actions[0] is a ContextActionSpawnMonster: that assumption
//     aborted the first build of this module with "Animate Dead lost its native
//     spawn action", and a third-party mod that reshapes the list must not be
//     able to disable the spell.
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items.Slots;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View;
using UnityEngine;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Raises one corpse as a skeleton and charges the result against the
    // caster's animate dead Hit Dice budget.
    [TypeId("b41d6f7c58e34a1e9c0d2a5f7b6e0813")]
    [AllowedOn(typeof(BlueprintAbility), false)]
    public sealed class ContextActionAnimateDead0598 : ContextAction
    {
        public BlueprintUnitReference Skeleton;
        public BlueprintSummonPoolReference Pool;
        public BlueprintUnitFactReference NoAnimateFact;
        public ContextDurationValue DurationValue;
        public ActionList AfterSpawn;
        // Twice the caster level per casting, four times per caster level in total.
        public int PerCastHdPerLevel = 2;
        public int TotalHdPerLevel = 4;
        // CotW's AbilityTargetCanBeAnimated.max_size. Animate Dead, Lesser caps
        // this at Medium.
        public Size MaxCorpseSize = Size.Colossal;
        // Metres. How far from the clicked point a corpse may lie.
        public float CorpseSearchRadius;

        public override string GetCaption() => "Animate the nearest corpse as a skeleton";

        public override void RunAction()
        {
            try
            {
                var caster = Context?.MaybeCaster;
                if (caster == null) return;
                var corpse = FindCorpse();
                if (corpse == null) return;
                var casterLevel = CasterLevel(caster);
                if (casterLevel <= 0) return;
                var hitDice = AnimateDead0598.HitDice(corpse);
                if (hitDice > casterLevel * Math.Max(1, PerCastHdPerLevel)) return;
                var budget = casterLevel * Math.Max(1, TotalHdPerLevel);
                if (AnimateDead0598.UsedHitDice(caster, Pool) + hitDice > budget) return;
                Spawn(caster, corpse, hitDice);
            }
            catch (Exception exception) { AnimateDead0598.Report(exception); }
        }

        // A corpse arrives as a plain point because the ability is point-targeted,
        // so the unit has to be recovered from the click position. The nearest
        // eligible corpse wins; without this a click a few pixels off the body
        // would waste the spell.
        private UnitEntityData FindCorpse()
        {
            var direct = Target.Unit;
            if (AnimateDead0598.IsAnimatable(direct, NoAnimateFact?.Get(), MaxCorpseSize)) return direct;
            var point = direct != null ? direct.Position : Target.Point;
            var radius = CorpseSearchRadius > 0f ? CorpseSearchRadius : 15.Feet().Meters;
            UnitEntityData best = null;
            var bestDistance = float.MaxValue;
            foreach (var unit in GameHelper.GetTargetsAround(point, radius, false, true))
            {
                if (!AnimateDead0598.IsAnimatable(unit, NoAnimateFact?.Get(), MaxCorpseSize)) continue;
                var distance = (unit.Position - point).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = unit;
            }
            return best;
        }

        private int CasterLevel(UnitEntityData caster)
        {
            var level = 0;
            try { level = Context.Params.CasterLevel; }
            catch (Exception) { level = 0; }
            if (level <= 0) level = caster.Descriptor.Progression.CharacterLevel;
            return level;
        }

        private void Spawn(UnitEntityData caster, UnitEntityData corpse, int hitDice)
        {
            var blueprint = Skeleton?.Get();
            if (blueprint == null) return;
            var position = AnimateDead0598.SpawnPosition(corpse, blueprint);
            var rounds = DurationValue != null ? DurationValue.Calculate(Context) : default(Rounds);
            var unit = Context.TriggerRule<RuleSummonUnit>(
                new RuleSummonUnit(caster, blueprint, position, rounds, 0) { Context = Context }).SummonedUnit;
            if (unit == null) return;

            AnimateDead0598.ApplyTemplate(unit, corpse, hitDice);
            AnimateDead0598.TransferEquipment(unit, corpse);
            unit.Descriptor.State.AddCondition(UnitCondition.Unlootable, null);
            if (Pool != null) GameHelper.RegisterUnitInSummonPool(Pool.Get(), unit);

            // The native list carries Augment Summoning, Commanding Infusion and
            // the Reanimator buff, all of which expect the summon's own context.
            if (AfterSpawn != null && AfterSpawn.Actions != null && AfterSpawn.Actions.Length > 0)
                using (Context.GetDataScope(unit)) AfterSpawn.Run();

            // A corpse that has already been animated can't be animated again.
            var marker = NoAnimateFact?.Get();
            if (marker != null) corpse.Descriptor.AddFact(marker, null, null);
        }
    }

    // Rejects living creatures while the cursor is over them. Point targets have
    // no unit and are always allowed: that is the normal "click the ground next
    // to the body" case, and the action decides there whether a corpse is near.
    [TypeId("0c7e5a94d2f84b6e8a1c3f5d9e0b7a24")]
    [AllowedOn(typeof(BlueprintAbility), false)]
    public sealed class AbilityTargetCanBeAnimated0598 : BlueprintComponent, IAbilityTargetRestriction
    {
        public BlueprintUnitFactReference NoAnimateFact;
        public Size MaxCorpseSize = Size.Colossal;

        public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target)
        {
            var unit = target.Unit;
            if (unit == null) return true;
            return AnimateDead0598.IsAnimatable(unit, NoAnimateFact?.Get(), MaxCorpseSize);
        }

        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target)
            => AnimateDead0598.ReasonText?.ToString() ?? string.Empty;
    }

    internal static class AnimateDead0598
    {
        internal const string ModuleName = "Animate Dead tabletop";
        internal const string Token = "AnimateDead0598";
        internal const string LesserToken = "AnimateDeadLesser0598";
        internal const string PoolToken = "AnimateDeadPool0598";
        internal const string NoAnimateToken = "AnimateDeadNoAnimate0598";
        internal const string SizeToken = "AnimateDeadSize0598";

        // Native Spells/Level4/AnimateDead.jbp and Spells/Level3/AnimateDeadLesser.jbp.
        internal const string AnimateDeadId = "4b76d32feb089ad4499c3a1ce8e1ac27";
        internal const string AnimateDeadLesserId = "57fcf8016cf04da4a8b33d2add14de7e";
        // Native Units/NPC/Commoners/Skeleton/SummonedSkeletonChampion.jbp, the
        // unit both spells already spawn. Its AddClassLevels supplies the floor
        // Hit Dice, so a 1 HD corpse still yields a usable skeleton.
        internal const string SkeletonId = "53e228ba7fe18104c93dc4b7294a1b30";

        // Creature types that leave no skeleton behind. Same GUIDs CotW used and
        // still present in Wrath under Traits/Races/Common and
        // CreatureAbilities/Subtypes.
        internal static readonly string[] BlockingTypeIds =
        {
            "734a29b693e9ec346ba2951b27987e33", // UndeadType
            "fd389783027d63343b4a5634bd81645f", // ConstructType
            "198fd8924dabcb5478d0f78bd453c586", // SubtypeElemental
            "706e61781d692a042b35941f14bc41c5", // PlantType
            "3bec99efd9a363242a6c8d9957b75e91", // AberrationType
            "09478937695300944a179530664e42ec"  // VerminType
        };

        internal const string DescriptionText =
            "Turns the corpse of a dead creature into a skeleton that obeys your spoken commands.\n" +
            "The skeleton takes the corpse's Hit Dice, size and physical ability scores, and keeps the equipment the corpse was carrying. You can't create an undead with more HD than twice your caster level, and no matter how many times you use this spell you can control only 4 HD worth of undead creatures per caster level. A corpse you have already animated can't be animated again, and neither can the remains of an undead, construct, elemental, plant, vermin or aberration.\n" +
            "The skeleton remains animated for 10 minutes per caster level.";

        internal const string LesserDescriptionText =
            "This spell functions as animate dead, except you can only create a single Small or Medium skeleton.\n" +
            "Animate Dead: " + DescriptionText;

        internal const string DurationText = "10 minutes per caster level";

        internal const string TargetReasonText = "The target must be an intact corpse that can be animated.";

        internal const string LesserTargetReasonText = "The target must be an intact corpse of a Small or Medium creature.";

        internal static string LastSummary = "Not registered";

        internal static LocalizedString ReasonText;

        private static BlueprintUnitFact noAnimateFact;
        private static readonly Dictionary<Size, BlueprintUnitFact> SizeFacts = new Dictionary<Size, BlueprintUnitFact>();
        private static readonly List<BlueprintUnitFact> blockingTypes = new List<BlueprintUnitFact>();

        // One spell's wiring, captured before publication so the transaction can
        // be rolled back without re-reading the blueprint.
        private sealed class Wiring
        {
            internal string Token;
            internal BlueprintAbility Ability;
            internal ContextActionAnimateDead0598 Action;
            internal AbilityTargetCanBeAnimated0598 Restriction;
            internal AbilityCanTargetDead DeadMarker;
            internal AbilityEffectRunAction Run;
            internal bool AddedRun;
            internal ActionList PriorActions;
            internal BlueprintComponent[] PriorComponents;
            internal object PriorDescription;
            internal LocalizedString PriorDuration;
            internal object PriorElements;
        }

        internal static void Register(BlueprintsCache cache)
        {
            if (MagusBatch01454.Existing(cache, ModuleName)) { LastSummary = "Already registered"; return; }
            var f = new PsychicAssets(cache);
            var skeleton = f.Native<BlueprintUnit>(SkeletonId);
            var greater = f.Native<BlueprintAbility>(AnimateDeadId);
            var lesser = f.Native<BlueprintAbility>(AnimateDeadLesserId);

            // An unlimited pool of its own: the 4 HD control budget must count
            // only animated corpses, not summon monster spells that share the
            // game's generic SummonMonsterPool. Both spells charge the same pool,
            // because tabletop animate dead has one control budget.
            var pool = f.Add(new BlueprintSummonPool { Limit = 0, DoNotRemoveDeadUnits = false },
                PoolToken, "", "");

            var marker = f.Feature(NoAnimateToken, "", "", Array.Empty<BlueprintComponent>());
            marker.HideInUI = true;
            marker.HideInCharacterSheetAndLevelUp = true;

            // One absolute-size fact per category, so a Large corpse yields a
            // Large skeleton. ChangeUnitSize's m_Type is an enum whose members
            // are not part of the public API surface this project targets; the
            // value is read from the game's own getter (IsTypeValue compares
            // against 1) instead of guessing a member name.
            var typeField = HarmonyLib.AccessTools.Field(typeof(ChangeUnitSize), "m_Type")
                ?? throw new MissingFieldException(typeof(ChangeUnitSize).FullName, "m_Type");
            var absolute = Enum.ToObject(typeField.FieldType, 1);
            foreach (Size size in Enum.GetValues(typeof(Size)))
            {
                var component = new ChangeUnitSize { name = "AnimateDeadSize" + size };
                Set(component, "m_Type", absolute);
                Set(component, "SizeDelta", 0);
                Set(component, "Size", size);
                SizeFacts[size] = f.Feature(SizeToken + size, "", "", new BlueprintComponent[] { component });
            }

            // Resolve everything the rules read before touching either ability,
            // so a missing creature-type blueprint fails the module cleanly
            // instead of leaving a half-wired spell behind.
            var types = new List<BlueprintUnitFact>();
            foreach (var id in BlockingTypeIds)
            {
                var type = cache.Load(BlueprintGuid.Parse(id)) as BlueprintUnitFact;
                if (type != null) types.Add(type);
            }
            var reason = Text(Token, "targetReason", TargetReasonText);
            var lesserReason = Text(LesserToken, "targetReason", LesserTargetReasonText);

            var greaterWiring = Wire(f, greater, Token, skeleton, pool, marker, Size.Colossal, reason);
            var lesserWiring = Wire(f, lesser, LesserToken, skeleton, pool, marker, Size.Medium, lesserReason);
            var all = new[] { greaterWiring, lesserWiring };

            MagusBatch01454.Publish(ModuleName, f,
                () =>
                {
                    noAnimateFact = marker;
                    blockingTypes.Clear();
                    blockingTypes.AddRange(types);
                    ReasonText = reason;
                    foreach (var wiring in all) Connect(wiring);
                },
                () =>
                {
                    foreach (var wiring in all) Disconnect(wiring);
                    noAnimateFact = null;
                    blockingTypes.Clear();
                    ReasonText = null;
                },
                () => Verify(all));
            LastSummary = "Registered: Animate Dead and Animate Dead, Lesser raise one corpse as a skeleton with the corpse's Hit Dice, size, physical scores and equipment; " +
                "2 HD per caster level per casting, 4 HD per caster level controlled, 10 minutes per caster level; the lesser spell accepts only a Small or Medium corpse.";
        }

        // Builds the action and the two targeting components for one spell and
        // captures its pre-publication state.
        private static Wiring Wire(PsychicAssets f, BlueprintAbility ability, string token,
            BlueprintUnit skeleton, BlueprintSummonPool pool, BlueprintFeature marker,
            Size maxSize, LocalizedString reason)
        {
            if (ability == null) throw new InvalidOperationException("Animate Dead blueprint missing: " + token);
            var wiring = new Wiring
            {
                Token = token,
                Ability = ability,
                PriorComponents = ability.ComponentsArray,
                PriorDescription = Get(ability, "m_Description"),
                PriorDuration = ability.LocalizedDuration,
                PriorElements = Get(ability, "m_AllElements")
            };

            // A load order that drops the native run action must not disable the
            // spell; the component is re-added in that case.
            wiring.Run = ability.GetComponent<AbilityEffectRunAction>();
            if (wiring.Run == null)
            {
                wiring.Run = new AbilityEffectRunAction { name = "AnimateDeadRun" + token };
                wiring.AddedRun = true;
            }
            wiring.PriorActions = wiring.Run.Actions;

            wiring.Action = new ContextActionAnimateDead0598
            {
                name = "AnimateDeadCorpse" + token,
                Skeleton = Ref<BlueprintUnitReference>(skeleton),
                Pool = Ref<BlueprintSummonPoolReference>(pool),
                NoAnimateFact = Ref<BlueprintUnitFactReference>(marker),
                DurationValue = Extendable(new ContextDurationValue
                {
                    Rate = DurationRate.TenMinutes,
                    DiceType = DiceType.Zero,
                    DiceCountValue = 0,
                    BonusValue = new ContextValue { ValueType = ContextValueType.Rank }
                }),
                // Reused verbatim when present: Augment Summoning, Commanding
                // Infusion and the Reanimator buff all target the fresh unit.
                AfterSpawn = NativeAfterSpawn(wiring.Run, token),
                MaxCorpseSize = maxSize,
                CorpseSearchRadius = 15.Feet().Meters
            };
            wiring.Restriction = new AbilityTargetCanBeAnimated0598
            {
                name = "AnimateDeadCorpseOnly" + token,
                NoAnimateFact = Ref<BlueprintUnitFactReference>(marker),
                MaxCorpseSize = maxSize
            };
            // Bare marker component: BlueprintAbility.CanCastToDeadTarget is
            // `GetComponent<ICanTargetDeadUnits>() != null`, which is what makes
            // the cursor accept a corpse at all.
            wiring.DeadMarker = new AbilityCanTargetDead { name = "AnimateDeadCorpseTarget" + token };
            return wiring;
        }

        // The native AfterSpawn list is found by walking the action graph. The
        // first build of this module assumed Actions[0] was the spawn action and
        // aborted registration with "Animate Dead lost its native spawn action",
        // which is exactly the kind of assumption a third-party spell edit can
        // invalidate. The list is optional: without it the spell still works,
        // only the summon-boosting synergies are lost, and the log says so.
        private static ActionList NativeAfterSpawn(AbilityEffectRunAction run, string token)
        {
            var graph = SpellBatch01447.Elements(run?.Actions, new HashSet<object>(IdentityComparer)).ToList();
            var spawn = graph.OfType<ContextActionSpawnMonster>().FirstOrDefault();
            // Always logged once: this line is the only evidence of what the
            // native action list looks like after every other mod has edited it,
            // and the first build of this module failed on exactly that.
            Report(token + " native actions = [" +
                string.Join(",", graph.Select(e => e.GetType().Name).Distinct().ToArray()) +
                "]; spawn action = " + (spawn != null) +
                "; after spawn list = " + (spawn?.AfterSpawn != null));
            if (spawn != null && spawn.AfterSpawn != null) return spawn.AfterSpawn;
            return new ActionList { Actions = Array.Empty<GameAction>() };
        }

        private static void Connect(Wiring wiring)
        {
            var ability = wiring.Ability;
            wiring.Run.Actions = Actions(wiring.Action);
            var components = (wiring.PriorComponents ?? Array.Empty<BlueprintComponent>())
                .Concat(new BlueprintComponent[] { wiring.Restriction, wiring.DeadMarker });
            if (wiring.AddedRun) components = components.Concat(new BlueprintComponent[] { wiring.Run });
            ability.ComponentsArray = components.ToArray();
            Set(ability, "m_Description", Text(wiring.Token, "description",
                wiring.Token == Token ? DescriptionText : LesserDescriptionText));
            ability.LocalizedDuration = Text(wiring.Token, "duration", DurationText);
            RefreshAbility(ability);
        }

        private static void Disconnect(Wiring wiring)
        {
            var ability = wiring.Ability;
            wiring.Run.Actions = wiring.PriorActions;
            ability.ComponentsArray = wiring.PriorComponents;
            Set(ability, "m_Description", wiring.PriorDescription);
            ability.LocalizedDuration = wiring.PriorDuration;
            if (wiring.PriorElements != null) Set(ability, "m_AllElements", wiring.PriorElements);
            SpellBatch01447.ResetAbilityCaches(ability);
        }

        // The abilities are native, so MagusBatch01454 does not own their element
        // cache; the newly inserted action and components have to be re-walked by
        // hand or the game never enables them.
        private static void RefreshAbility(BlueprintAbility ability)
        {
            Set(ability, "m_AllElements",
                SpellBatch01447.Elements(ability.ComponentsArray, new HashSet<object>(IdentityComparer)).ToList());
            SpellBatch01447.ResetAbilityCaches(ability);
        }

        private static void Verify(Wiring[] all)
        {
            if (SizeFacts.Count != Enum.GetValues(typeof(Size)).Length)
                throw new InvalidOperationException("0598 size facts incomplete.");
            foreach (var wiring in all)
            {
                var ability = wiring.Ability;
                if (ability == null) throw new InvalidOperationException("0598 " + wiring.Token + " vanished.");
                if (!ReferenceEquals(wiring.Run.Actions?.Actions?.FirstOrDefault(), wiring.Action))
                    throw new InvalidOperationException("0598 " + wiring.Token + " does not run the corpse action.");
                var components = ability.ComponentsArray ?? Array.Empty<BlueprintComponent>();
                if (!components.Any(c => ReferenceEquals(c, wiring.Restriction)))
                    throw new InvalidOperationException("0598 " + wiring.Token + " lost its target restriction.");
                if (!components.Any(c => ReferenceEquals(c, wiring.DeadMarker)))
                    throw new InvalidOperationException("0598 " + wiring.Token + " lost its dead-target marker.");
                if (ability.GetComponent<ICanTargetDeadUnits>() == null)
                    throw new InvalidOperationException("0598 " + wiring.Token + " cannot target dead units.");
                if (wiring.Action.Skeleton?.Get() == null)
                    throw new InvalidOperationException("0598 skeleton blueprint missing.");
                if (wiring.Action.Pool?.Get() == null)
                    throw new InvalidOperationException("0598 summon pool missing.");
                if (wiring.Action.NoAnimateFact?.Get() == null)
                    throw new InvalidOperationException("0598 corpse marker missing.");
            }
        }

        // Extend Spell only lengthens a duration the component marks extendable,
        // and that flag has no public setter.
        private static ContextDurationValue Extendable(ContextDurationValue duration)
        {
            Set(duration, "m_IsExtendable", true);
            return duration;
        }

        // ------------------------------------------------------------- rules

        internal static bool IsAnimatable(UnitEntityData unit, BlueprintUnitFact marker = null,
            Size maxSize = Size.Colossal)
        {
            if (unit == null) return false;
            var descriptor = unit.Descriptor;
            if (descriptor == null) return false;
            if (!descriptor.State.IsDead) return false;
            if (descriptor.State.HasCondition(UnitCondition.Petrified)) return false;
            // CotW's rule, kept: a party member's corpse is not raw material.
            if (unit.IsPlayerFaction) return false;
            if (descriptor.OriginalSize > maxSize) return false;
            if (marker == null) marker = noAnimateFact;
            if (marker != null && descriptor.HasFact(marker)) return false;
            foreach (var type in blockingTypes)
                if (type != null && descriptor.HasFact(type)) return false;
            return true;
        }

        internal static int HitDice(UnitEntityData corpse)
        {
            var level = corpse?.Descriptor?.Progression?.CharacterLevel ?? 0;
            if (level < 1) level = 1;
            return Math.Min(level, 20);
        }

        internal static int UsedHitDice(UnitEntityData caster, BlueprintSummonPoolReference reference)
        {
            if (caster == null || reference == null) return 0;
            var pool = Game.Instance?.SummonPools?.GetPool(reference.Get());
            if (pool == null) return 0;
            var used = 0;
            foreach (var unit in pool.Units)
            {
                if (unit == null) continue;
                var part = unit.Get<UnitPartSummonedMonster>();
                if (part == null || part.Summoner != caster) continue;
                used += HitDice(unit);
            }
            return used;
        }

        // A corpse is already on the navmesh, so the search position is the spawn
        // position unless the game's placement helper finds a free spot nearby.
        // The helper is best-effort: a stale or out-of-area point must not cost
        // the spell.
        internal static Vector3 SpawnPosition(UnitEntityData corpse, BlueprintUnit blueprint)
        {
            var origin = corpse.Position;
            try
            {
                var view = blueprint.Prefab?.Load(false);
                var radius = view != null && view.Corpulence > 0f ? view.Corpulence : 0.5f;
                FreePlaceSelector.PlaceSpawnPlaces(1, radius, origin);
                var relaxed = FreePlaceSelector.GetRelaxedPosition(0, true);
                if (relaxed != Vector3.zero && (relaxed - origin).sqrMagnitude < (30f.Feet().Meters) * (30f.Feet().Meters))
                    return relaxed;
            }
            catch (Exception exception) { Report(exception); }
            return origin;
        }

        internal static void ApplyTemplate(UnitEntityData unit, UnitEntityData corpse, int hitDice)
        {
            try
            {
                unit.Stats.Strength.BaseValue = corpse.Stats.Strength.BaseValue;
                unit.Stats.Dexterity.BaseValue = corpse.Stats.Dexterity.BaseValue + 2;
            }
            catch (Exception exception) { Report(exception); }

            // The shipped skeleton is a 2 HD fighter skeleton; a tougher corpse
            // raises it to the corpse's own Hit Dice.
            try
            {
                var levels = unit.Blueprint.GetComponent<AddClassLevels>();
                if (levels != null && hitDice > levels.Levels)
                    AddClassLevels.LevelUp(levels, unit.Descriptor, hitDice - levels.Levels, null);
            }
            catch (Exception exception) { Report(exception); }

            try
            {
                if (SizeFacts.TryGetValue(corpse.Descriptor.OriginalSize, out var size) &&
                    size != null && corpse.Descriptor.OriginalSize != unit.Descriptor.OriginalSize)
                    unit.Descriptor.AddFact(size, null, null);
            }
            catch (Exception exception) { Report(exception); }
        }

        // CotW's transfer_equipment: the corpse's gear is copied onto the fresh
        // skeleton slot by slot, so a reanimated champion keeps its own weapon
        // and shield. The corpse keeps the originals and stays lootable, which is
        // how the original mod behaved.
        internal static void TransferEquipment(UnitEntityData unit, UnitEntityData corpse)
        {
            try
            {
                var source = corpse.Body;
                var target = unit.Body;
                if (source == null || target == null || !source.HandsAreEnabled) return;
                var from = source.CurrentEquipmentSlots.ToList();
                var to = target.CurrentEquipmentSlots.ToList();
                for (var index = 0; index < from.Count && index < to.Count; index++)
                {
                    var item = from[index]?.MaybeItem;
                    if (item == null || item.Blueprint == null) continue;
                    // Never overwrite what the skeleton blueprint already equips.
                    if (to[index] == null || to[index].HasItem) continue;
                    target.TryInsertItem(item.Blueprint, to[index]);
                }
                target.CurrentHandEquipmentSetIndex = source.CurrentHandEquipmentSetIndex;
            }
            catch (Exception exception) { Report(exception); }
        }

        internal static void Report(Exception exception) => ReportOnce(exception?.ToString() ?? "unknown");

        internal static void Report(string message) => ReportOnce(message);

        private static readonly HashSet<string> Reported = new HashSet<string>();

        private static void ReportOnce(string message)
        {
            try
            {
                if (message == null || !Reported.Add(message)) return;
                Console.WriteLine("[CotW-AnimateDead] " + message);
            }
            catch (Exception) { }
        }
    }
}
