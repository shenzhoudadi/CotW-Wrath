using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Inquisitions01428
    {
        internal const string ModuleName = "Inquisitions: Heresy, Imprisonment";
        internal static readonly string[] Tokens = { "HeresyInquisition", "ImprisonmentInquisition" };
        internal static readonly string[] PoolIds = { "48525e5da45c9c243a343fc6545dbdb9", "43281c3d7fe18cc4d91928395837cd1e" };

        // These are the eighteen deities explicitly permitted by original CotW.
        // Cayden Cailean and Irori are deliberately absent.
        internal static readonly string[] HeresyDeities = {
            "Abadar", "Asmodeus", "Calistria", "Desna", "Erastil", "Gorum", "Gozreh", "Iomedae", "Lamashtu",
            "Nethys", "Norgorber", "Pharasma", "Rovagug", "Sarenrae", "Shelyn", "Torag", "Urgathoa", "ZonKuthon"
        };

        internal static void Register(BlueprintsCache cache)
        {
            var assets = new PsychicAssets(cache);
            var classes = Inquisitions01426.ClassIds.Select(assets.Native<BlueprintCharacterClass>).ToArray();
            var pools = PoolIds.Select(assets.Native<BlueprintFeatureSelection>).ToArray();
            var deities = assets.Native<BlueprintFeatureSelection>("59e7a76987fe3b547b9cce045f4db3e4");
            var heresy = Heresy(assets, classes, deities);
            var imprisonment = Imprisonment(assets, classes, deities);
            Migration01426.Commit(assets, pools, heresy, imprisonment);
        }

        private static void Icon(BlueprintUnitFact target, BlueprintUnitFact source)
        {
            Set(target, "m_Icon", Get(source, "m_Icon"));
        }

        private static BlueprintAbilityResource Resource(PsychicAssets assets, string token, string name, int baseValue, bool wisdom, int minimum)
        {
            var resource = assets.Add(new BlueprintAbilityResource(), token, name, name);
            var amount = Get(resource, "m_MaxAmount");
            Set(amount, "BaseValue", baseValue);
            Set(amount, "IncreasedByStat", wisdom);
            Set(amount, "ResourceBonusStat", StatType.Wisdom);
            foreach (var field in new[] { "m_Class", "m_ClassDiv" }) Set(amount, field, Array.Empty<BlueprintCharacterClassReference>());
            foreach (var field in new[] { "m_Archetypes", "m_ArchetypesDiv" }) Set(amount, field, Array.Empty<BlueprintArchetypeReference>());
            Set(resource, "m_MaxAmount", amount);
            Set(resource, "m_Min", minimum);
            resource.IsDomainAbility = true;
            resource.LocalizedName = Text(token, "name", name);
            resource.LocalizedDescription = Text(token, "description", name);
            return resource;
        }

        private static AddAbilityResources Grant(BlueprintAbilityResource resource)
        {
            var grant = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true };
            Set(grant, "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
            return grant;
        }

        private static AbilityResourceLogic Cost(BlueprintAbilityResource resource)
        {
            var cost = new AbilityResourceLogic { Amount = 1 };
            Set(cost, "m_IsSpendResource", true);
            Set(cost, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
            return cost;
        }

        private static LevelEntry Entry(int level, params BlueprintFeatureBase[] features)
        {
            var entry = new LevelEntry { Level = level };
            Set(entry, "m_Features", features.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            foreach (var feature in features.OfType<BlueprintFeature>()) feature.IsClassFeature = true;
            return entry;
        }

        private static BlueprintProgression Progression(PsychicAssets assets, BlueprintCharacterClass[] classes, BlueprintFeatureSelection deities,
            string token, string name, string description, LevelEntry[] entries, params string[] allowedDeities)
        {
            var requirements = new List<BlueprintComponent>();
            foreach (var deityName in allowedDeities)
            {
                var matches = deities.AllFeatures.Where(f => f != null && f.name == deityName + "Feature")
                    .GroupBy(f => f.AssetGuid).Select(g => g.First()).ToArray();
                if (matches.Length != 1)
                    throw new InvalidOperationException("Inquisition deity dependency is missing or ambiguous: " + deityName);
                requirements.Add(Migration01426.Require(matches[0], true));
            }
            var progression = assets.Add(new BlueprintProgression {
                Ranks = 1, IsClassFeature = true, Groups = new[] { FeatureGroup.Domain }, GiveFeaturesForPreviousLevels = true,
                LevelEntries = entries, ComponentsArray = requirements.ToArray()
            }, token, name, description + " Uses cleric and inquisitor levels. Replaces a domain without granting domain spells or spell slots.");
            Set(progression, "m_Classes", classes.Select(c => new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(c) }).ToArray());
            var once = new PrerequisiteNoFeature();
            Set(once, "m_Feature", Ref<BlueprintFeatureReference>(progression));
            progression.ComponentsArray = progression.ComponentsArray.Concat(new[] { once }).ToArray();
            Icon(progression, entries[0].Features[0]);
            return progression;
        }

        private static BlueprintActivatableAbility Toggle(PsychicAssets assets, string token, string name, string description,
            BlueprintBuff buff, BlueprintAbilityResource resource)
        {
            var logic = new ActivatableAbilityResourceLogic { SpendType = ActivatableAbilityResourceLogic.ResourceSpendType.Never };
            Set(logic, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
            var toggle = assets.Add(new BlueprintActivatableAbility {
                ActivationType = AbilityActivationType.Immediately, Group = ActivatableAbilityGroup.None,
                DeactivateImmediately = true, DeactivateIfCombatEnded = false, DoNotTurnOffOnRest = true,
                ComponentsArray = new BlueprintComponent[] { logic }
            }, token, name, description);
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(buff));
            Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);
            Icon(toggle, buff);
            return toggle;
        }

        private static BlueprintProgression Heresy(PsychicAssets assets, BlueprintCharacterClass[] classes, BlueprintFeatureSelection deities)
        {
            var source = assets.Native<BlueprintAbility>("69851cc3b821c2d479ac1f2d86e8ffa5");
            var righteous = assets.Feature("HeresyRighteousInfiltration", "Righteous Infiltration",
                "Use your Wisdom modifier instead of your current Persuasion ability modifier for Bluff and Intimidate checks. Diplomacy checks are unchanged. This replacement applies even when Wisdom is lower; it does not stack with another replacement that already uses Wisdom.",
                new HeresyWisdomChecks());
            Icon(righteous, source);

            const string rerollText = "At level 4, while the corresponding option is enabled, a failed Bluff, Diplomacy or Stealth check is rolled again and uses the better result. The three options share daily uses equal to your Wisdom bonus (minimum 0). A successful initial check and a check of a different kind spend no use.";
            var rerollResource = Resource(assets, "HeresyBlessedInfiltrationUses", "Blessed Infiltration", 0, true, 0);
            var toggles = new List<BlueprintUnitFact>();
            foreach (var skill in new[] { StatType.CheckBluff, StatType.CheckDiplomacy, StatType.SkillStealth })
            {
                var label = skill == StatType.CheckBluff ? "Bluff" : skill == StatType.CheckDiplomacy ? "Diplomacy" : "Stealth";
                var name = "Blessed Infiltration: " + label;
                var buff = assets.Buff("HeresyBlessedInfiltration" + label + "Buff", name, rerollText,
                    new HeresyInfiltrationReroll { Skill = skill, Resource = Ref<BlueprintAbilityResourceReference>(rerollResource) });
                Icon(buff, source);
                toggles.Add(Toggle(assets, "HeresyBlessedInfiltration" + label, name, rerollText, buff, rerollResource));
            }
            var blessed = assets.Feature("HeresyBlessedInfiltration", "Blessed Infiltration", rerollText, Facts(toggles.ToArray()), Grant(rerollResource));
            Icon(blessed, source);

            const string wordText = "At level 8, once per day as a standard action, speak a word of anathema against an enemy within 60 feet. A failed Will save applies bestow curse for 1 minute: -4 to attacks, saves, ability checks and skill checks. DC 10 + half your cleric/inquisitor level + Wisdom modifier. Caster and target must be able to hear the word (outside silence).";
            var nativeRun = source.GetComponents<AbilityEffectRunAction>().FirstOrDefault()
                ?? throw new InvalidOperationException("Bestow Curse Deterioration actions are missing.");
            var nativeBuff = FindAppliedBuff(nativeRun.Actions);
            if (nativeBuff == null) throw new InvalidOperationException("Bestow Curse Deterioration buff is missing.");
            var wordResource = Resource(assets, "HeresyWordOfAnathemaUses", "Word of Anathema", 1, false, 1);
            var word = assets.Clone(source, "HeresyWordOfAnathemaAbility", "Word of Anathema", wordText);
            word.Type = AbilityType.SpellLike;
            word.ActionType = UnitCommand.CommandType.Standard;
            word.Range = AbilityRange.Custom;
            word.CustomRange = new Feet(60);
            word.AvailableMetamagic = 0;
            word.CanTargetSelf = word.CanTargetFriends = word.CanTargetPoint = false;
            word.CanTargetEnemies = true;
            word.SpellResistance = true;
            Set(word, "m_Parent", new BlueprintAbilityReference());
            word.ComponentsArray = word.ComponentsArray.Where(c => c is AbilitySpawnFx || c is SpellDescriptorComponent)
                .Concat(Inquisitions01426.Parameters(assets, "HeresyWordOfAnathema", classes, StatType.Wisdom))
                .Concat(new BlueprintComponent[] {
                    new AudibleAbilityRestriction(), Cost(wordResource),
                    Save(SavingThrowType.Will, Saved(Array.Empty<GameAction>(), new GameAction[] { Apply(nativeBuff, 1, DurationRate.Minutes) }))
                }).ToArray();
            word.LocalizedDuration = Text("HeresyWordOfAnathema", "duration", "1 minute");
            word.LocalizedSavingThrow = Text("HeresyWordOfAnathema", "save", "Will negates");
            var wordFeature = assets.Feature("HeresyWordOfAnathema", "Word of Anathema", wordText, Facts(word), Grant(wordResource));
            Icon(wordFeature, word);
            return Progression(assets, classes, deities, Tokens[0], "Heresy Inquisition",
                "Root out heresy through infiltration and divine argument. Level 1: Righteous Infiltration. Level 4: Blessed Infiltration. Level 8: Word of Anathema.",
                new[] { Entry(1, righteous), Entry(4, blessed), Entry(8, wordFeature) }, HeresyDeities);
        }

        private static BlueprintBuff FindAppliedBuff(ActionList actions)
        {
            foreach (var action in actions?.Actions ?? Array.Empty<GameAction>())
            {
                if (action is ContextActionApplyBuff apply && apply.Buff != null) return apply.Buff;
                if (action is ContextActionConditionalSaved saved)
                {
                    var buff = FindAppliedBuff(saved.Failed);
                    if (buff != null) return buff;
                }
            }
            return null;
        }

        private static BlueprintProgression Imprisonment(PsychicAssets assets, BlueprintCharacterClass[] classes, BlueprintFeatureSelection deities)
        {
            const string cagingText = "While enabled, a confirmed critical hit with a melee or ranged attack (including spells requiring an attack roll) attempts to entangle the target for 1d4 rounds (Fortitude negates). Uses per day: 3 + Wisdom modifier, minimum 0. Each qualifying attack spends one use, whether the target saves or is immune. DC 10 + half your cleric/inquisitor level + Wisdom modifier.";
            const string prisonText = "At level 8, while enabled, your next successful melee attack attempts to hold the target as hold monster for one round per cleric/inquisitor level (Will negates, with a new save each round). Once per day. A qualifying hit spends the use even if the target saves or is immune. DC 10 + half your cleric/inquisitor level + Wisdom modifier.";
            var entangle = assets.Native<BlueprintBuff>("f7f6330726121cf4b90a6086b05d2e38");
            var hold = assets.Native<BlueprintBuff>("2cfcce5b62d3e6d4082ec31b58468cc8");
            var caging = ImprisonmentPower(assets, classes, "ImprisonmentCagingStrike", "Caging Strike", cagingText, entangle, true);
            var prison = ImprisonmentPower(assets, classes, "ImprisonmentDivinePrison", "Divine Prison", prisonText, hold, false);
            return Progression(assets, classes, deities, Tokens[1], "Imprisonment Inquisition",
                "Abadar, Asmodeus or Torag. Capture foes with spectral chains. Level 1: Caging Strike. Level 8: Divine Prison.",
                new[] { Entry(1, caging), Entry(8, prison) }, "Abadar", "Asmodeus", "Torag");
        }

        private static BlueprintFeature ImprisonmentPower(PsychicAssets assets, BlueprintCharacterClass[] classes, string token,
            string name, string description, BlueprintBuff effect, bool critical)
        {
            var resource = Resource(assets, token + "Uses", name, critical ? 3 : 1, critical, critical ? 0 : 1);
            var apply = Apply(effect, 1, fromSpell: false);
            apply.IsNotDispelable = true;
            if (critical)
                apply.DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.D4, DiceCountValue = 1, BonusValue = 0 };
            else
                apply.DurationValue.BonusValue = Inquisitions01426.Rank();
            var components = Inquisitions01426.Parameters(assets, token + "Effect", classes, StatType.Wisdom).ToList();
            // Copy the native descriptor so native mind-affecting / paralysis
            // immunities still apply to Hold Monster. No new immunity bypass.
            components.AddRange(CopyData(effect.GetComponents<SpellDescriptorComponent>().ToArray()));
            components.Add(Save(critical ? SavingThrowType.Fortitude : SavingThrowType.Will,
                Saved(Array.Empty<GameAction>(), new GameAction[] { apply })));
            var ability = assets.Ability(token + "Effect", name, description, AbilityType.Supernatural, UnitCommand.CommandType.Free,
                AbilityRange.Unlimited, components.ToArray());
            ability.SpellResistance = false;
            ability.AvailableMetamagic = 0;
            Icon(ability, effect);
            var buff = assets.Buff(token + "Ready", name + ": Enabled", description,
                new InquisitionImprisonmentAttack { CriticalOnly = critical, Resource = Ref<BlueprintAbilityResourceReference>(resource), Effect = Ref<BlueprintAbilityReference>(ability) });
            Icon(buff, effect);
            var toggle = Toggle(assets, token + "Toggle", name, description, buff, resource);
            var feature = assets.Feature(token, name, description, Facts(toggle), Grant(resource));
            Icon(feature, effect);
            return feature;
        }
    }

    [TypeId("56dfb9d384e14275b7e03d7d7c886824")]
    public sealed class HeresyWisdomChecks : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleSkillCheck>, IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleSkillCheck evt)
        {
            if (!T11TrainingOptions.Active(Owner, Fact) || evt.Initiator != Owner ||
                (evt.StatType != StatType.CheckBluff && evt.StatType != StatType.CheckIntimidate)) return;
            // Bluff and Intimidate are native ModifiableValueDependant values,
            // not replaceable skills. Adjust only this check using the actual
            // parent skill attribute: Conversion already using Wisdom adds 0.
            int difference = Owner.Stats.Wisdom.Bonus - Owner.Stats.SkillPersuasion.BaseAttribute.Bonus;
            if (difference != 0 && !evt.Bonus.Modifiers.Any(m => m.Source == Fact))
                evt.Bonus.AddModifier(difference, Fact, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventDidTrigger(RuleSkillCheck evt) { }
    }

    [TypeId("0c10a0f743e044918ad52f76a737bd89")]
    public sealed class HeresyInfiltrationReroll : UnitBuffComponentDelegate, IInitiatorRulebookHandler<RuleRollD20>, IInitiatorRulebookSubscriber
    {
        public StatType Skill;
        public BlueprintAbilityResourceReference Resource;

        public void OnEventAboutToTrigger(RuleRollD20 evt)
        {
            var resource = Resource?.Get();
            if (!Buff.IsActive || !Buff.IsTurnedOn || Buff.IsSuppressed || evt.IsFake || evt.Result > 0 || evt.Initiator != Owner || resource == null ||
                Owner.Resources.GetResourceAmount(resource) <= 0 || evt.Rerolls?.Any(r => ReferenceEquals(r.Source, Fact)) == true) return;
            var check = Rulebook.CurrentContext.PreviousEvent as RuleSkillCheck;
            if (check == null || check.Initiator != Owner || check.StatType != Skill) return;
            var first = evt.PreRollDice();
            if (check.IsSuccessRoll(first) || check.IsSuccessRoll(first, check.SuccessBonus)) return;
            // Pay before scheduling the native reroll. Nested rules cannot use
            // the same last charge, and successful first rolls cost nothing.
            Owner.Resources.Spend(resource, 1);
            evt.AddReroll(1, true, Fact);
        }

        public void OnEventDidTrigger(RuleRollD20 evt) { }
    }

    [TypeId("52075f3e8e1340fcad9e020724929892")]
    public sealed class InquisitionImprisonmentAttack : UnitBuffComponentDelegate, IInitiatorRulebookHandler<RuleAttackRoll>, IInitiatorRulebookSubscriber
    {
        public bool CriticalOnly;
        public BlueprintAbilityResourceReference Resource;
        public BlueprintAbilityReference Effect;
        private static readonly ConditionalWeakTable<RuleAttackRoll, HashSet<EntityFact>> Resolved = new ConditionalWeakTable<RuleAttackRoll, HashSet<EntityFact>>();

        public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            if (!Buff.IsActive || !Buff.IsTurnedOn || Buff.IsSuppressed || evt.IsFake || evt.Initiator != Owner || !evt.IsHit ||
                evt.Target == null || evt.Target.IsDisposed || evt.Target.State.IsDead || evt.Weapon == null) return;
            if (CriticalOnly ? !evt.IsCriticalConfirmed || evt.FortificationNegatesCriticalHit : !evt.AttackType.IsMelee()) return;
            var resource = Resource?.Get();
            var effect = Effect?.Get();
            if (resource == null || effect == null || Owner.Resources.GetResourceAmount(resource) <= 0) return;
            var handled = Resolved.GetValue(evt, _ => new HashSet<EntityFact>());
            if (!handled.Add(Fact)) return;
            Owner.Resources.Spend(resource, 1);

            // Calculate through the native ability path for this owner now,
            // rather than reusing a toggle context from a previous level/day.
            // This invokes only the effect; it never queues another attack.
            var data = new AbilityData(effect, Owner.Descriptor);
            var context = data.CreateExecutionContext(evt.Target, data.CalculateParams());
            // Supernatural powers skip numeric spell resistance, but native
            // descriptor / named-spell immunity remains a separate rule.
            if (context.TriggerRule(new RuleSpellResistanceCheck(context, evt.Target)).IsSpellResisted) return;
            foreach (var action in effect.GetComponents<AbilityEffectRunAction>()) action.Apply(context, evt.Target);
        }
    }
}
