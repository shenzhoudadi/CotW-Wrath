// COTW-0046 S-REVELATION-0046: the minimal isolation bridge for Speaker for the Past.
// It exists only to keep this archetype's level/attribute/resource inputs separate
// from the native Oracle (Charisma / OracleClass) machinery. It is deliberately
// local: no global MysteryEngine rewrite, no general-purpose dice framework.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Parameterized binding for both Oracle Time Mystery (Oracle+Charisma)
    // and Shaman Speaker for the Past (Shaman+Speaker+Wisdom).
    internal sealed class RevelationBinding0046
    {
        internal BlueprintCharacterClass PrimaryClass;
        internal BlueprintArchetype PrimaryArchetype;
        internal BlueprintCharacterClass[] ExtraClasses = Array.Empty<BlueprintCharacterClass>();
        internal BlueprintArchetype[] ExtraArchetypes = Array.Empty<BlueprintArchetype>();
        internal StatType CastingStat = StatType.Wisdom;
    }

    // Single point of truth: every Speaker revelation derives its rank from the
    // Shaman class plus this archetype, and its casting stat from Wisdom.
    internal static class RevelationBridge0046
    {
        internal static RevelationBinding0046 ForSpeaker(BlueprintCharacterClass shaman, BlueprintArchetype speaker)
            => new RevelationBinding0046 { PrimaryClass = shaman, PrimaryArchetype = speaker, CastingStat = StatType.Wisdom };

        internal static RevelationBinding0046 ForOracle(BlueprintCharacterClass oracle, BlueprintCharacterClass arcanist = null, BlueprintArchetype magicDeceiver = null)
            => new RevelationBinding0046 {
                PrimaryClass = oracle,
                ExtraClasses = arcanist != null ? new[] { arcanist } : Array.Empty<BlueprintCharacterClass>(),
                ExtraArchetypes = magicDeceiver != null ? new[] { magicDeceiver } : Array.Empty<BlueprintArchetype>(),
                CastingStat = StatType.Charisma
            };

        internal static ContextRankConfig Rank(RevelationBinding0046 bind, string progression = "AsIs")
        {
            var r = new ContextRankConfig { name = "Speaker0046Rank" };
            SetEnum(r, "m_Type", "Default");
            SetEnum(r, "m_BaseValueType", "SummClassLevelWithArchetype");
            SetEnum(r, "m_Progression", progression);
            var classes = new[] { bind.PrimaryClass }.Concat(bind.ExtraClasses ?? Array.Empty<BlueprintCharacterClass>())
                .Where(c => c != null).Distinct().Select(Ref<BlueprintCharacterClassReference>).ToArray();
            Set(r, "m_Class", classes);
            Set(r, "Archetype", bind.PrimaryArchetype == null ? null : Ref<BlueprintArchetypeReference>(bind.PrimaryArchetype));
            var extraArchs = (bind.ExtraArchetypes ?? Array.Empty<BlueprintArchetype>())
                .Where(a => a != null).Select(Ref<BlueprintArchetypeReference>).ToArray();
            Set(r, "m_AdditionalArchetypes", extraArchs);
            Set(r, "m_FeatureList", Array.Empty<BlueprintFeatureReference>());
            return r;
        }

        internal static ContextRankConfig Rank(BlueprintCharacterClass cls, BlueprintArchetype arch, string progression = "AsIs")
            => Rank(new RevelationBinding0046 { PrimaryClass = cls, PrimaryArchetype = arch, CastingStat = StatType.Wisdom }, progression);

        internal static ContextCalculateAbilityParamsBasedOnClass Params(BlueprintCharacterClass cls, StatType stat = StatType.Wisdom)
        {
            var p = new ContextCalculateAbilityParamsBasedOnClass { name = "Speaker0046Params", StatType = stat };
            Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(cls));
            return p;
        }

        // The resource's level terms and divisor both count only the bound class/archetype;
        // an unrelated multiclass dip contributes zero.
        internal static void BindResource(object amount, RevelationBinding0046 bind)
        {
            var classes = new[] { bind.PrimaryClass }.Concat(bind.ExtraClasses ?? Array.Empty<BlueprintCharacterClass>())
                .Where(c => c != null).Distinct().Select(Ref<BlueprintCharacterClassReference>).ToArray();
            var archs = bind.PrimaryArchetype != null
                ? new[] { Ref<BlueprintArchetypeReference>(bind.PrimaryArchetype) }
                : (bind.ExtraArchetypes ?? Array.Empty<BlueprintArchetype>()).Where(a => a != null).Select(Ref<BlueprintArchetypeReference>).ToArray();
            Set(amount, "m_Class", classes);
            Set(amount, "m_ClassDiv", classes);
            Set(amount, "m_Archetypes", archs);
            Set(amount, "m_ArchetypesDiv", archs);
        }

        internal static void BindResource(object amount, BlueprintCharacterClass cls, BlueprintArchetype arch)
            => BindResource(amount, new RevelationBinding0046 { PrimaryClass = cls, PrimaryArchetype = arch });

        internal static BlueprintComponent Level(RevelationBinding0046 bind, int level)
        {
            if (bind.PrimaryArchetype != null)
                return Level(bind.PrimaryClass, bind.PrimaryArchetype, level);
            var p = new PrerequisiteClassLevel { name = "TimeMystery0046Level" + level, Level = level };
            Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(bind.PrimaryClass));
            return p;
        }

        internal static PrerequisiteArchetypeLevel Level(BlueprintCharacterClass cls, BlueprintArchetype arch, int level)
        {
            var p = new PrerequisiteArchetypeLevel { name = "Speaker0046Level" + level, Level = level };
            Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(cls));
            Set(p, "m_Archetype", Ref<BlueprintArchetypeReference>(arch));
            return p;
        }

        internal static AddFeatureOnClassLevel At(RevelationBinding0046 bind, BlueprintFeature feature, int level, bool before = false)
        {
            var a = new AddFeatureOnClassLevel { name = "Speaker0046Grant" + level + feature.name, Level = level, BeforeThisLevel = before };
            Set(a, "m_Class", Ref<BlueprintCharacterClassReference>(bind.PrimaryClass));
            Set(a, "m_Feature", Ref<BlueprintFeatureReference>(feature));
            Set(a, "m_AdditionalClasses", (bind.ExtraClasses ?? Array.Empty<BlueprintCharacterClass>())
                .Where(c => c != null).Select(Ref<BlueprintCharacterClassReference>).ToArray());
            var archs = bind.PrimaryArchetype != null
                ? new[] { Ref<BlueprintArchetypeReference>(bind.PrimaryArchetype) }
                : (bind.ExtraArchetypes ?? Array.Empty<BlueprintArchetype>()).Where(x => x != null).Select(Ref<BlueprintArchetypeReference>).ToArray();
            Set(a, "m_Archetypes", archs);
            return a;
        }

        internal static AddFeatureOnClassLevel At(BlueprintCharacterClass cls, BlueprintArchetype arch, BlueprintFeature feature, int level, bool before = false)
            => At(new RevelationBinding0046 { PrimaryClass = cls, PrimaryArchetype = arch }, feature, level, before);

        internal static void Icon(Kingmaker.Blueprints.Facts.BlueprintUnitFact target, Kingmaker.Blueprints.Facts.BlueprintUnitFact donor)
            => Set(target, "m_Icon", Get(donor, "m_Icon"));
    }

    // LearnSpellList only implements ILevelUpCompleteUIHandler. Code-driven level
    // ups and feature removal therefore need an explicit, bounded reconciliation
    // of the 18 private bonus spells against the Shaman primary spellbook.
    [TypeId("0046a1b2c3d4e5f60000000000000001")]
    public sealed class SpeakerSpellbookSync0046 : UnitFactComponentDelegate, IOwnerGainLevelHandler, ILevelUpCompleteUIHandler
    {
        public BlueprintSpellbookReference CharacterClassSpellbook;
        public BlueprintSpellListReference SpellList;
        public BlueprintSpellListReference NativeShamanSpellList;

        // Retained for diagnostic inspection; actual reconciliation invokes
        // native Spellbook.AddKnown / RemoveSpell directly.
        static readonly MethodInfo AddKnownMethod = AccessTools.Method(typeof(Spellbook), nameof(Spellbook.AddKnown), new[] { typeof(int), typeof(BlueprintAbility), typeof(bool) });
        static readonly MethodInfo RemoveSpellMethod = AccessTools.Method(typeof(Spellbook), nameof(Spellbook.RemoveSpell), new[] { typeof(BlueprintAbility) });

        // B-stage diagnostic: proves the real native methods were resolved and
        // records how many spells were learned / removed on the last pass.
        internal static string LastReport = "not probed";

        protected override void OnActivate() => Sync();
        protected override void OnTurnOn() => Sync();
        protected override void OnPostLoad() => Sync();
        protected override void OnApplyPostLoadFixes() => Sync();
        protected override void OnDeactivate() { base.OnDeactivate(); Cleanup(); }
        protected override void OnTurnOff() { base.OnTurnOff(); Cleanup(); }
        public void HandleUnitGainLevel() => Sync();
        public void HandleLevelUpComplete(UnitEntityData unit, bool isChargen) { if (unit?.Descriptor == Owner) Sync(); }

        Spellbook Book() => FindBook(Owner, CharacterClassSpellbook?.Get());

        static Spellbook FindBook(UnitDescriptor owner, BlueprintSpellbook wanted)
            => owner?.Spellbooks?.FirstOrDefault(b => b != null && (wanted == null || b.Blueprint == wanted));

        // Compute spells that remain legitimately known in the target Shaman
        // spellbook from native Shaman progression or other still-active
        // features (LearnSpellList / AddKnownSpell / LearnSpells / copied scrolls).
        static HashSet<BlueprintGuid> CollectKeptSpells(UnitDescriptor owner, Spellbook book, BlueprintSpellList nativeList,
            int maxSpellLevel, BlueprintGuid excludeFactId)
        {
            var keep = new HashSet<BlueprintGuid>();
            if (owner == null || book == null) return keep;

            void AddFromList(BlueprintSpellList spellList, int upToLevel)
            {
                if (spellList?.SpellsByLevel == null) return;
                foreach (var row in spellList.SpellsByLevel)
                {
                    if (row == null || row.SpellLevel < 1 || row.SpellLevel > upToLevel || row.Spells == null) continue;
                    foreach (var spell in row.Spells)
                        if (spell != null) keep.Add(spell.AssetGuid);
                }
            }

            AddFromList(nativeList, maxSpellLevel);
            var targetBookBp = book.Blueprint;

            foreach (var fact in owner.Facts.List)
            {
                if (fact == null || !fact.Active || fact.Blueprint?.AssetGuid == excludeFactId) continue;
                var comps = fact.Blueprint?.ComponentsArray;
                if (comps == null) continue;
                foreach (var c in comps)
                {
                    if (c is LearnSpellList lsl && lsl.CharacterClass != null)
                    {
                        var cd = owner.Progression.GetClassData(lsl.CharacterClass);
                        if (cd != null && cd.Spellbook == targetBookBp &&
                            (lsl.Archetype == null || cd.Archetypes.Contains(lsl.Archetype)))
                            AddFromList(lsl.SpellList, maxSpellLevel);
                    }
                    else if (c is AddKnownSpell aks && aks.CharacterClass != null && aks.Spell != null && aks.SpellLevel <= maxSpellLevel)
                    {
                        var cd = owner.Progression.GetClassData(aks.CharacterClass);
                        if (cd != null && cd.Spellbook == targetBookBp &&
                            (aks.Archetype == null || cd.Archetypes.Contains(aks.Archetype)) &&
                            (aks.ExcludeArchetype == null || !cd.Archetypes.Contains(aks.ExcludeArchetype)))
                            keep.Add(aks.Spell.AssetGuid);
                    }
                    else if (c is LearnSpells ls && ls.CharacterClass != null)
                    {
                        var cd = owner.Progression.GetClassData(ls.CharacterClass);
                        if (cd != null && cd.Spellbook == targetBookBp && targetBookBp.SpellList != null)
                        {
                            foreach (var spell in ls.Spells)
                            {
                                if (spell == null) continue;
                                foreach (var slc in spell.GetComponents<SpellListComponent>())
                                {
                                    if ((slc.SpellList == targetBookBp.SpellList || slc.SpellList == targetBookBp.SpellList.FilteredList) &&
                                        slc.SpellLevel <= maxSpellLevel)
                                        keep.Add(spell.AssetGuid);
                                }
                            }
                        }
                    }
                }
            }

            // Preserve copied scrolls and temporary known spells across all circles 1..9 even after de-level.
            for (int lvl = 1; lvl <= 9; lvl++)
            {
                var known = book.GetKnownSpells(lvl);
                if (known == null) continue;
                foreach (var ad in known)
                    if (ad?.Blueprint != null && (ad.CopiedFromScroll || ad.IsTemporary))
                        keep.Add(ad.Blueprint.AssetGuid);
            }

            return keep;
        }

        static readonly FieldInfo TempEntriesField = AccessTools.Field(typeof(UnitPartTemporarySpellsKnown), "m_Entries");

        static List<AbilityData> CollectForeignTempSpells(UnitDescriptor owner, Spellbook book)
        {
            var result = new List<AbilityData>();
            var part = owner?.Unit?.Get<UnitPartTemporarySpellsKnown>();
            var entries = part != null ? TempEntriesField?.GetValue(part) as List<UnitPartTemporarySpellsKnown.Entry> : null;
            if (entries == null) return result;
            foreach (var entry in entries)
            {
                if (entry == null || entry.Removed || entry.Spells == null) continue;
                foreach (var ab in entry.Spells)
                    if (ab != null && ReferenceEquals(ab.Spellbook, book) && ab.Blueprint != null)
                        result.Add(ab);
            }
            return result;
        }

        static void RelinquishSpeakerSpell(Spellbook book, int level, BlueprintAbility spell,
            List<AbilityData> foreignTemps, ref int removed)
        {
            var matchingTemps = foreignTemps
                .Where(ab => ab.Blueprint.AssetGuid == spell.AssetGuid)
                .ToArray();
            if (matchingTemps.Length == 0)
            {
                if (book.IsKnown(spell))
                {
                    book.RemoveSpell(spell);
                    removed++;
                }
                return;
            }

            bool sharedOnLevel = matchingTemps.Any(ab => ab.SpellLevel == level);
            if (sharedOnLevel)
            {
                // Another active temporary source shares this exact book/circle
                // AbilityData object. Keep the spell and common-slot preparations,
                // and mark it temporary so removing the last temporary source
                // cleanly triggers UnitPartTemporarySpellsKnown.RemoveEntry.
                foreach (var ab in book.GetKnownSpells(level).ToArray())
                    if (ab != null && ab.Blueprint?.AssetGuid == spell.AssetGuid)
                        ab.IsTemporary = true;
                foreach (var ab in matchingTemps)
                    if (ab.SpellLevel == level)
                        ab.IsTemporary = true;
                return;
            }

            // Foreign temporary grant sits on a different circle in the same book:
            // unlearn only Speaker's circle without wiping the foreign circle.
            foreach (var ab in book.GetKnownSpells(level).ToArray())
            {
                if (ab == null || ab.Blueprint?.AssetGuid != spell.AssetGuid) continue;
                ab.IsTemporary = true;
                book.RemoveTemporarySpell(ab);
                removed++;
            }
        }

        void Sync()
        {
            var book = Book();
            var list = SpellList?.Get();
            if (book == null || list?.SpellsByLevel == null) return;
            int max = book.MaxSpellLevel, learned = 0, removed = 0;
            var keep = CollectKeptSpells(Owner, book, NativeShamanSpellList?.Get(), Math.Max(0, max), Fact?.Blueprint?.AssetGuid ?? BlueprintGuid.Empty);
            var foreignTemps = CollectForeignTempSpells(Owner, book);
            foreach (var row in list.SpellsByLevel)
            {
                if (row == null || row.SpellLevel < 1 || row.SpellLevel > 9) continue;
                foreach (var spell in row.Spells)
                {
                    if (spell == null) continue;
                    if (row.SpellLevel <= max)
                    {
                        bool hadKnownOnLevel = book.IsKnownOnLevel(spell, row.SpellLevel);
                        if (!hadKnownOnLevel)
                            book.AddKnown(row.SpellLevel, spell);
                        bool hasForeignTempOnLevel = foreignTemps.Any(ft =>
                            ft.Blueprint.AssetGuid == spell.AssetGuid && ft.SpellLevel == row.SpellLevel);
                        // Promote to non-temporary only when Speaker newly adds the spell
                        // or shares the slot with an active UnitPartTemporarySpellsKnown entry;
                        // unbacked pre-existing temporary entries keep IsTemporary=true.
                        if (!hadKnownOnLevel || hasForeignTempOnLevel)
                        {
                            foreach (var ab in book.GetKnownSpells(row.SpellLevel))
                                if (ab != null && ab.Blueprint?.AssetGuid == spell.AssetGuid)
                                    ab.IsTemporary = false;
                        }
                        learned++;
                    }
                    else if (!keep.Contains(spell.AssetGuid))
                    {
                        RelinquishSpeakerSpell(book, row.SpellLevel, spell, foreignTemps, ref removed);
                    }
                }
            }
            LastReport = "addKnown=" + AddKnownMethod + "; removeSpell=" + RemoveSpellMethod +
                "; maxSpellLevel=" + max + "; learned=" + learned + "; removed=" + removed;
        }

        void Cleanup() => Cleanup(Owner, CharacterClassSpellbook?.Get(), SpellList?.Get(), NativeShamanSpellList?.Get(), Fact?.Blueprint?.AssetGuid ?? BlueprintGuid.Empty);

        // On removal, unlearn Speaker-exclusive spells unless kept by native progression,
        // another permanent source, or an active foreign UnitPartTemporarySpellsKnown entry.
        // Idempotent if called again.
        internal static void Cleanup(UnitDescriptor owner, BlueprintSpellbook wantedBook, BlueprintSpellList list,
            BlueprintSpellList nativeList, BlueprintGuid excludeFactId)
        {
            var book = FindBook(owner, wantedBook);
            if (book == null || list?.SpellsByLevel == null) return;
            var keep = CollectKeptSpells(owner, book, nativeList, Math.Max(0, book.MaxSpellLevel), excludeFactId);
            var foreignTemps = CollectForeignTempSpells(owner, book);
            int removed = 0;
            foreach (var row in list.SpellsByLevel)
            {
                if (row == null || row.Spells == null) continue;
                foreach (var spell in row.Spells)
                {
                    if (spell == null || keep.Contains(spell.AssetGuid)) continue;
                    RelinquishSpeakerSpell(book, row.SpellLevel, spell, foreignTemps, ref removed);
                }
            }
        }
    }

    // Rewind Time: one failed d20 gets one reroll, taking the best result.
    // The swift-action active buff prepays 1 resource on cast, still fires
    // when remaining pool reaches 0, and removes itself once an eligible failed
    // reroll occurs. The six reaction switches do not cost resource to toggle
    // on and spend 1 resource only when they actually trigger a failed reroll.
    // Same-event dedup prevents double-reroll / double-charge when active buff
    // and a matching reaction toggle are active simultaneously.
    [TypeId("0046a1b2c3d4e5f60000000000000002")]
    public sealed class SpeakerFailedReroll0046 : UnitBuffComponentDelegate, IInitiatorRulebookHandler<RuleRollD20>, IInitiatorRulebookSubscriber
    {
        // Ruling #6: six reaction switches plus the swift-action active buff.
        internal static readonly RuleType[] AllRules = {
            RuleType.AttackRoll, RuleType.Initiative, RuleType.Maneuver,
            RuleType.SavingThrow, RuleType.SkillCheck, RuleType.SpellResistance
        };
        static readonly ConditionalWeakTable<RuleRollD20, EntityFact> TriggeredRolls = new ConditionalWeakTable<RuleRollD20, EntityFact>();

        public BlueprintAbilityResourceReference Resource;
        public bool RerollOnlyIfFailed = true;
        // True on the 6 reaction switches (spend 1 on trigger); false on the
        // swift-action active buff (prepaid 1 at cast time).
        public bool SpendOnTrigger = true;
        // True on the 1-round swift-action active buff so the buff ends after
        // performing one failed reroll.
        public bool ConsumeBuffOnTrigger = false;
        // null/empty means "any supported d20 rule check" (the 1-round active buff).
        // The six reaction toggles set exactly one rule type each.
        public RuleType[] Rules;

        public void OnEventAboutToTrigger(RuleRollD20 evt)
        {
            var resource = Resource?.Get();
            if (!Buff.IsActive || !Buff.IsTurnedOn || Buff.IsSuppressed || evt.IsFake || evt.Initiator != Owner || resource == null) return;
            if (HasSpeakerReroll(evt)) return;
            if (SpendOnTrigger && Owner.Resources.GetResourceAmount(resource) <= 0) return;
            var previous = Rulebook.CurrentContext?.PreviousEvent;
            var ruleType = Classify(previous);
            if (!ruleType.HasValue) return;
            if (!AcceptsRule(ruleType.Value)) return;
            // Prepaid active Rewind buff must take precedence regardless of buff application/subscriber order.
            if (SpendOnTrigger && HasActivePrepaidReroll(ruleType.Value)) return;
            if (RerollOnlyIfFailed && !Failed(evt, previous, ruleType.Value)) return;

            TriggeredRolls.Add(evt, Fact);
            if (SpendOnTrigger)
                Owner.Resources.Spend(resource, 1);
            evt.AddReroll(1, true, Fact);
        }

        public void OnEventDidTrigger(RuleRollD20 evt)
        {
            if (ConsumeBuffOnTrigger && TriggeredRolls.TryGetValue(evt, out var source) && ReferenceEquals(source, Fact))
                Owner?.RemoveFact(Fact);
        }

        bool AcceptsRule(RuleType ruleType) => Rules == null || Rules.Length == 0 || Rules.Any(r => r == ruleType);

        bool HasActivePrepaidReroll(RuleType ruleType)
        {
            if (Owner?.Buffs == null) return false;
            foreach (var b in Owner.Buffs.Enumerable)
            {
                if (b == null || b == Buff || !b.IsActive || !b.IsTurnedOn || b.IsSuppressed) continue;
                foreach (var comp in b.Blueprint?.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                    if (comp is SpeakerFailedReroll0046 other && !other.SpendOnTrigger && other.AcceptsRule(ruleType))
                        return true;
            }
            return false;
        }

        static bool HasSpeakerReroll(RuleRollD20 evt)
        {
            if (TriggeredRolls.TryGetValue(evt, out _)) return true;
            if (evt.Rerolls == null) return false;
            foreach (var r in evt.Rerolls)
                if (ReferenceEquals(r.Source, null) == false && r.Source.Blueprint?.GetComponent<SpeakerFailedReroll0046>() != null)
                    return true;
            return false;
        }

        static RuleType? Classify(RulebookEvent previous)
        {
            if (previous is RuleAttackRoll) return RuleType.AttackRoll;
            if (previous is RuleInitiativeRoll) return RuleType.Initiative;
            if (previous is RuleCombatManeuver) return RuleType.Maneuver;
            if (previous is RuleSavingThrow) return RuleType.SavingThrow;
            if (previous is RuleSkillCheck) return RuleType.SkillCheck;
            if (previous is RuleSpellResistanceCheck) return RuleType.SpellResistance;
            return null;
        }

        static bool Failed(RuleRollD20 evt, RulebookEvent previous, RuleType ruleType)
        {
            int first = evt.PreRollDice();
            switch (ruleType)
            {
                case RuleType.AttackRoll:
                {
                    var atk = (RuleAttackRoll)previous;
                    if (atk.AutoHit) return false;
                    if (atk.AutoMiss) return true;
                    // Primary attack roll uses TargetAC; critical confirmation roll (once D20 is already populated) uses TargetCriticalAC.
                    if (atk.D20 == null)
                        return !atk.IsSuccessRoll(first);
                    if (atk.AutoCriticalConfirmation) return false;
                    return first + atk.AttackBonus + atk.CriticalConfirmationBonus < atk.TargetCriticalAC;
                }
                case RuleType.Initiative:
                    // Native RuleInitiativeRoll has no target DC or ISuccessable contract;
                    // aligned with ModifyD20.ShouldReroll when an initiative roll occurs.
                    return true;
                case RuleType.Maneuver:
                {
                    // RuleCombatManeuver.IsSuccessRoll returns Fail when InitiatorRoll is still null during Dice.D20.
                    var man = (RuleCombatManeuver)previous;
                    if (man.AutoFailure) return false;
                    if (first == 1 && !(man.Initiator?.State?.Features.AlwaysChance ?? false)) return true;
                    if (first == 20) return false;
                    if (!man.IsWeaponSnatcher)
                        return first + man.InitiatorCMB < man.TargetCMD;
                    return !man.IsSuccessRoll(first);
                }
                case RuleType.SavingThrow:
                {
                    // RuleSavingThrow.RollD20 runs before SetStatValueFromThrow populates StatValue.
                    var save = (RuleSavingThrow)previous;
                    if (save.AutoPass) return false;
                    if (first == 20) return false;
                    if (first <= 1) return true;
                    var stat = save.Initiator?.Stats?.GetStat(save.StatType);
                    var replace = save.Initiator?.Get<UnitPartSavingThrowsReplace>();
                    if (replace != null && replace.SavingThrowStat == save.StatType)
                    {
                        var replaced = save.Initiator?.Stats?.GetStat(replace.ReplaceToStat);
                        if (replaced != null && (stat == null || replaced.ModifiedValue > stat.ModifiedValue))
                            stat = replaced;
                    }
                    int statValue = save.StatValue;
                    if (stat != null)
                    {
                        using (stat.GetTemporaryModifiersScope(save.AllBonuses))
                            statValue = stat.ModifiedValue;
                    }
                    int successBonus = save.RequiresSuccessBonus ? save.SuccessBonus : 0;
                    return first + statValue + successBonus < save.DifficultyClass;
                }
                case RuleType.SkillCheck:
                {
                    var skill = (RuleSkillCheck)previous;
                    int successBonus = skill.RequiresSuccessBonus ? skill.SuccessBonus : 0;
                    return !skill.IsSuccessRoll(first, successBonus);
                }
                case RuleType.SpellResistance:
                {
                    var sr = (RuleSpellResistanceCheck)previous;
                    if (!sr.HasResistanceRoll || sr.IgnoreSpellResistance || sr.TargetIsImmune || sr.RuleSkillCheck != null) return false;
                    return first + sr.SpellPenetration < sr.SpellResistance;
                }
                default:
                    return false;
            }
        }
    }

    // Blood of Heroes: morale bonus +(1 + ShamanLevel / 7) applies ONLY to
    // Will saving throws against effects bearing SpellDescriptor.Fear, leaving
    // ordinary Will saves and Fear Fortitude/Reflex saves untouched.
    [TypeId("0046a1b2c3d4e5f60000000000000003")]
    public sealed class SpeakerFearWillBonus0046 : UnitBuffComponentDelegate, IInitiatorRulebookHandler<RuleSavingThrow>, IInitiatorRulebookSubscriber
    {
        public BlueprintCharacterClassReference ShamanClass;
        public BlueprintArchetypeReference Archetype;

        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            if (!Buff.IsActive || !Buff.IsTurnedOn || Buff.IsSuppressed || evt.Initiator != Owner) return;
            if (evt.Type != SavingThrowType.Will) return;
            var context = evt.Reason.Context;
            if (context == null || (context.SpellDescriptor & SpellDescriptor.Fear) == 0) return;

            int level = 0;
            var cls = ShamanClass?.Get();
            var arch = Archetype?.Get();
            if (cls != null)
            {
                var cd = Owner?.Progression?.GetClassData(cls);
                if (cd != null && (arch == null || cd.Archetypes.Contains(arch)))
                    level = cd.Level;
            }
            int bonus = Math.Min(3, 1 + Math.Max(0, level) / 7);
            evt.AddTemporaryModifier(evt.Initiator.Stats.SaveWill.AddModifier(bonus, Runtime, ModifierDescriptor.Morale));
        }

        public void OnEventDidTrigger(RuleSavingThrow evt) { }
    }

    // Sacred Council (Caster Level Checks variant): grants +2 untyped bonus
    // specifically to caster-level checks (Spell Resistance penetration and
    // Dispel Magic CL/DC checks) without altering general CasterLevel.
    [TypeId("0046a1b2c3d4e5f60000000000000004")]
    public sealed class SpeakerCasterLevelChecks0046 : UnitBuffComponentDelegate,
        IInitiatorRulebookHandler<RuleSpellResistanceCheck>,
        IInitiatorRulebookHandler<RuleDispelMagic>,
        IInitiatorRulebookSubscriber
    {
        public int Bonus = 2;

        public void OnEventAboutToTrigger(RuleSpellResistanceCheck evt)
        {
            if (!Buff.IsActive || !Buff.IsTurnedOn || Buff.IsSuppressed || evt.Initiator != Owner) return;
            evt.AddSpellPenetration(Bonus, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventDidTrigger(RuleSpellResistanceCheck evt) { }

        public void OnEventAboutToTrigger(RuleDispelMagic evt)
        {
            if (!Buff.IsActive || !Buff.IsTurnedOn || Buff.IsSuppressed || evt.Initiator != Owner) return;
            if (evt.Check == RuleDispelMagic.CheckType.CasterLevel || evt.Check == RuleDispelMagic.CheckType.DC)
                evt.Bonus += Bonus;
        }

        public void OnEventDidTrigger(RuleDispelMagic evt) { }
    }

    // Spirit of the Warrior: sets base attack bonus equal to Shaman level
    // (never CharacterLevel), cleanly recalculating without stat loops.
    [TypeId("0046a1b2c3d4e5f60000000000000005")]
    public sealed class SpeakerWarriorBab0046 : UnitBuffComponentDelegate, IOwnerGainLevelHandler
    {
        public BlueprintCharacterClassReference ShamanClass;
        public BlueprintArchetypeReference Archetype;

        protected override void OnTurnOn() => Apply();
        protected override void OnTurnOff() => Owner?.Stats?.BaseAttackBonus?.RemoveModifiersFrom(Runtime);

        public void HandleUnitGainLevel()
        {
            if (Buff.IsActive && Buff.IsTurnedOn && !Buff.IsSuppressed) Apply();
        }

        void Apply()
        {
            var stat = Owner?.Stats?.BaseAttackBonus;
            if (stat == null) return;
            stat.RemoveModifiersFrom(Runtime);
            var cls = ShamanClass?.Get();
            var arch = Archetype?.Get();
            var cd = cls == null ? null : Owner?.Progression?.GetClassData(cls);
            int shamanLevel = (cd != null && (arch == null || cd.Archetypes.Contains(arch))) ? cd.Level : 0;
            int delta = Math.Max(0, shamanLevel - stat.ModifiedValue);
            if (delta > 0)
                stat.AddModifier(delta, Runtime, ModifierDescriptor.UntypedStackable);
        }
    }

    // Mutual exclusion between Spirit of the Warrior and native TransformationBuff:
    // prevents coexistence whichever buff is applied second.
    [TypeId("0046a1b2c3d4e5f60000000000000006")]
    public sealed class SpeakerBuffMutex0046 : UnitBuffComponentDelegate, IUnitBuffHandler
    {
        public BlueprintBuffReference IncompatibleBuff;

        protected override void OnTurnOn()
        {
            var other = IncompatibleBuff?.Get();
            if (other != null && Owner?.Buffs?.HasFact(other) == true)
                TurnOffSelf();
        }

        public void HandleBuffDidAdded(Kingmaker.UnitLogic.Buffs.Buff buff)
        {
            var other = IncompatibleBuff?.Get();
            if (other == null || buff == null || buff.Owner != Owner || buff.Blueprint != other) return;
            TurnOffSelf();
        }

        public void HandleBuffDidRemoved(Kingmaker.UnitLogic.Buffs.Buff buff) { }

        void TurnOffSelf()
        {
            var selfBlueprint = Buff?.Blueprint;
            if (Owner?.ActivatableAbilities != null && selfBlueprint != null)
            {
                foreach (var aa in Owner.ActivatableAbilities.RawFacts)
                    if (aa != null && aa.IsOn && aa.Blueprint?.Buff == selfBlueprint)
                        aa.IsOn = false;
            }
            Buff?.Remove();
        }
    }
}
