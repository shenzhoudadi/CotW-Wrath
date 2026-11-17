using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099Necromancy
    {
        internal const string School = "Necromancy";
        internal const string CauseFear = "bd81a3931aa285a4f9844585b5d97e51";
        internal const string AnimateDeadLesser = "57fcf8016cf04da4a8b33d2add14de7e";
        internal const string Undead = "734a29b693e9ec346ba2951b27987e33";
        private const string Construct = "fd389783027d63343b4a5634bd81645f";
        private const string Elemental = "198fd8924dabcb5478d0f78bd453c586";
        private const string Frightened = "f08a7239aa961f34c8301518e71d4cdf";
        private const string Shaken = "25ec6cb6ab1845c48a95f9c20b034220";
        private const string Sickened = "4e42460798665fd4cb9173ffa7ada323";
        private const string FalseLife = "7a5b5bf845779a941a67251539545762";
        private const string FalseLifeBuff = "0fdb3cca6744fd94b9436459e6d9b947";
        private const string TouchWeapon = "bb337517547de1a4189518d404ec49d4";
        private const string FamiliarSelection = "363cab72f77c47745bf3a8807074d183";
        private const string Skeleton = "53e228ba7fe18104c93dc4b7294a1b30";
        private const string TeamworkShare = "a603a90d24a636c41910b3868f434447";
        private const string Aberration = "3bec99efd9a363242a6c8d9957b75e91";
        private const string Vermin = "09478937695300944a179530664e42ec";
        private const string Plant = "706e61781d692a042b35941f14bc41c5";
        private static readonly BlueprintGuid FleshRotDeliveryId = BlueprintGuid.Parse(Occultist0099Ids.Id("FleshRotDelivery"));

        internal static readonly string[] Donors = { CauseFear, Undead, Construct, Elemental, Frightened, Shaken, Sickened,
            FalseLife, FalseLifeBuff, TouchWeapon, FamiliarSelection, Skeleton, TeamworkShare, Aberration, Vermin, Plant,
            AnimateDeadLesser };
        internal static readonly string[] Tokens = { "NecromancyEffectiveLevel", "NecromancyDCLevel", "NecromanticFocusBuff",
            "MindFearAbility", "FleshRotAbility", "FleshRotDelivery", "FleshRotPower", "PainWaveAbility", "PainWavePower",
            "SpiritShroudBuff", "SpiritShroudAbility", "SpiritShroudPower", "NecromancyInvestFocusAbility",
            "NecromancyReplenishFocusAbility", "NecromancyImplementFocus", "SoulboundPuppetAbility", "SoulboundPuppetPower",
            "NecromanticServantPool", "NecromanticServantConsumed", "NecromanticServantDamageBuff",
            "NecromanticServantBloodyBuff", "NecromanticServantSingleAbility", "NecromanticServantAreaAbility",
            "NecromanticServantAbility", "NecromanticServantPower" };

        internal static IEnumerable<string> PuppetTokens(BlueprintsCache cache)
            => FamiliarFeatures(cache).SelectMany(feature => new[] { PuppetToken(feature), PuppetBuffToken(feature) });

        internal static IEnumerable<string> PuppetBuffTokens(BlueprintsCache cache)
            => FamiliarFeatures(cache).Select(PuppetBuffToken);

        private static string PuppetToken(BlueprintFeature feature) => "SoulboundPuppet-" + feature.AssetGuid;
        private static string PuppetBuffToken(BlueprintFeature feature) => "SoulboundPuppetBuff-" + feature.AssetGuid;

        // A native familiar bond feature grants one activatable ability whose buff
        // carries the native AddFamiliar critter. Anything else is not a familiar.
        private static bool IsNativeFamiliar(BlueprintsCache cache, BlueprintFeature feature)
        {
            var facts = feature.GetComponent<AddFacts>();
            if (facts == null) return false;
            var granted = (BlueprintUnitFactReference[])Get(facts, "m_Facts");
            return granted.Length == 1 &&
                Occultist0099Illusion.Load<BlueprintActivatableAbility>(cache, granted[0].Guid.ToString()) is BlueprintActivatableAbility toggle &&
                toggle.Buff?.GetComponent<AddFamiliar>() != null;
        }

        // The native familiar selection is the authority for which familiars exist.
        private static BlueprintFeature[] FamiliarFeatures(BlueprintsCache cache)
        {
            var selection = Occultist0099Illusion.Load<BlueprintFeatureSelection>(cache, FamiliarSelection);
            if (selection?.name != "Familiar") return Array.Empty<BlueprintFeature>();
            return ((BlueprintFeatureReference[])Get(selection, "m_AllFeatures"))
                .Select(reference => Occultist0099Illusion.Load<BlueprintFeature>(cache, reference.Guid.ToString()))
                .Where(feature => feature != null).ToArray();
        }

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache, Func<string, string, string, LocalizedString> text)
        {
            var fear = Occultist0099Illusion.Load<BlueprintAbility>(cache, CauseFear);
            var falseLife = Occultist0099Illusion.Load<BlueprintAbility>(cache, FalseLife);
            var frightened = Occultist0099Illusion.Load<BlueprintBuff>(cache, Frightened);
            var shaken = Occultist0099Illusion.Load<BlueprintBuff>(cache, Shaken);
            var sickened = Occultist0099Illusion.Load<BlueprintBuff>(cache, Sickened);
            var falseLifeBuff = Occultist0099Illusion.Load<BlueprintBuff>(cache, FalseLifeBuff);
            var touchWeapon = Occultist0099Illusion.Load<BlueprintItemWeapon>(cache, TouchWeapon);
            var undead = Occultist0099Illusion.Load<BlueprintFeature>(cache, Undead);
            var construct = Occultist0099Illusion.Load<BlueprintFeature>(cache, Construct);
            var elemental = Occultist0099Illusion.Load<BlueprintFeature>(cache, Elemental);
            var hitPoints = CopyData(falseLifeBuff?.GetComponent<TemporaryHitPointsFromAbilityValue>());
            var roll = CopyData(falseLifeBuff?.GetComponent<ContextCalculateSharedValue>());
            var familiars = FamiliarFeatures(cache);
            var skeleton = Occultist0099Illusion.Load<BlueprintUnit>(cache, Skeleton);
            var teamwork = Occultist0099Illusion.Load<BlueprintBuff>(cache, TeamworkShare);
            var aberration = Occultist0099Illusion.Load<BlueprintFeature>(cache, Aberration);
            var vermin = Occultist0099Illusion.Load<BlueprintFeature>(cache, Vermin);
            var plant = Occultist0099Illusion.Load<BlueprintFeature>(cache, Plant);
            var animate = Occultist0099Illusion.Load<BlueprintAbility>(cache, AnimateDeadLesser);
            var nativeSpawn = animate?.GetComponent<AbilityEffectRunAction>()?.Actions?.Actions?.OfType<ContextActionSpawnMonster>().SingleOrDefault();
            if (fear?.name != "CauseFear" || falseLife?.name != "FalseLife" || frightened?.GetComponent<AddCondition>() == null ||
                shaken?.GetComponent<AddCondition>() == null || sickened?.GetComponent<AddCondition>() == null ||
                touchWeapon == null || undead == null || construct == null || elemental == null || hitPoints == null || roll == null ||
                familiars.Length == 0 || skeleton == null || aberration == null || vermin == null || plant == null ||
                teamwork?.GetComponent<AddFactsFromCaster>() == null || nativeSpawn == null ||
                nativeSpawn.Blueprint?.AssetGuid != skeleton.AssetGuid ||
                familiars.Any(feature => !IsNativeFamiliar(cache, feature))) return null;

            var level = Occultist0099SchoolValue.Create("NecromancyEffectiveLevel", School, Occultist0099SchoolValueKind.EffectiveLevel);
            var dc = Occultist0099SchoolValue.Create("NecromancyDCLevel", School, Occultist0099SchoolValueKind.DCLevel);
            var focus = Buff("NecromanticFocusBuff", "Necromantic Focus",
                "Your occultist spells gain +1 DC against undead for every 4 points of focus locked in Necromancy.",
                new Occultist0099NecromanticFocus { name = "OccultistNecromanticFocus", Undead = Ref<BlueprintUnitFactReference>(undead) });
            SetEnum(focus, "m_Flags", "StayOnDeath");

            const string fearText = "Spend 1 point of Necromancy focus to frighten a living target within 30 feet for 1d4 rounds. If its Hit Dice exceed your occultist level, it is shaken instead. A Will save negates this mind-affecting fear effect.";
            var afraid = When(new ContextConditionHitDice { AddSharedValue = true, SharedValue = AbilitySharedValue.StatBonus },
                new GameAction[] { D4(frightened) }, new GameAction[] { D4(shaken) });
            var mindFear = Power("MindFearAbility", "Mind Fear", fearText, AbilityType.SpellLike, AbilityRange.Close, fear,
                Save(SavingThrowType.Will, Saved(Array.Empty<GameAction>(), new GameAction[] { afraid })),
                Exclude(undead, construct),
                Descriptor(SpellDescriptor.Fear | SpellDescriptor.Shaken | SpellDescriptor.Emotion | SpellDescriptor.NegativeEmotion | SpellDescriptor.MindAffecting),
                new ContextCalculateSharedValue { name = "MindFearHitDice", ValueType = AbilitySharedValue.StatBonus,
                    Value = new ContextDiceValue { DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = PropertyValue(level) } });
            Harmful(mindFear, true);

            const string rotText = "Spend 1 point of Necromancy focus to deal 1d8 unholy damage plus 1 per occultist level on a melee touch hit. Gain another d8 every 4 levels beyond 3rd, up to 5d8. Undead, constructs and elementals are immune.";
            var dice = LevelRank("FleshRotDamageDice", AbilityRankType.DamageDice, "StartPlusDivStep", 4);
            Set(dice, "m_StartLevel", 3);
            Set(dice, "m_UseMax", true);
            Set(dice, "m_Max", 5);
            var damage = new ContextActionDealDamage { DamageType = new DamageTypeDescription { Type = DamageType.Energy,
                Energy = DamageEnergyType.Unholy }, Value = new ContextDiceValue { DiceType = DiceType.D8,
                    DiceCountValue = Rank(AbilityRankType.DamageDice), BonusValue = PropertyValue(level) } };
            var touch = new AbilityDeliverTouch { name = "FleshRotTouch" };
            Set(touch, "m_TouchWeapon", Ref<BlueprintItemWeaponReference>(touchWeapon));
            var rotDelivery = Identify(new BlueprintAbility { Type = AbilityType.SpellLike, Range = AbilityRange.Touch,
                ActionType = UnitCommand.CommandType.Standard, ComponentsArray = new BlueprintComponent[] { touch, dice,
                    new SpellComponent { name = "OccultistNecromancy", School = SpellSchool.Necromancy },
                    Exclude(undead, construct, elemental),
                    Run(When(Forbidden(undead, construct, elemental), Array.Empty<GameAction>(), new GameAction[] { damage })) } },
                "FleshRotDelivery", "Flesh Rot", rotText, fear, text);
            Harmful(rotDelivery, true);
            var sticky = new AbilityEffectStickyTouch { name = "FleshRotHeldTouch" };
            Set(sticky, "m_TouchDeliveryAbility", Ref<BlueprintAbilityReference>(rotDelivery));
            var rot = Power("FleshRotAbility", "Flesh Rot", rotText, AbilityType.SpellLike, AbilityRange.Touch, fear,
                sticky, Exclude(undead, construct, elemental));
            Harmful(rot, true);

            const string painText = "Spend 1 point of Necromancy focus to sicken living creatures other than yourself within a 20-foot burst for 1 round per occultist level. A successful Will save reduces the duration to 1 round.";
            var area = new AbilityTargetsAround { name = "PainWaveTargets" };
            Set(area, "m_Radius", new Feet(20));
            SetEnum(area, "m_TargetType", "Any");
            Set(area, "m_Condition", LivingOther(undead, construct));
            var pain = Power("PainWaveAbility", "Pain Wave", painText, AbilityType.Supernatural, AbilityRange.Medium, fear,
                area, Descriptor(SpellDescriptor.MindAffecting | SpellDescriptor.Sickened),
                Save(SavingThrowType.Will, Saved(new GameAction[] { Apply(sickened, 1) },
                    new GameAction[] { Occultist0099Transmutation.Timed(sickened, PropertyValue(level), DurationRate.Rounds) })));
            Harmful(pain, false);
            pain.CanTargetPoint = pain.CanTargetFriends = true;

            const string shroudText = "Spend 1 point of Necromancy focus for 1d6 temporary hit points plus your occultist level. While any remain, gain +1 resistance on saves against death, fear, and negative level effects, plus another +1 at every 4 occultist levels. The shroud lasts 1 minute per occultist level.";
            roll.Value = new ContextDiceValue { DiceType = DiceType.D6, DiceCountValue = 1, BonusValue = PropertyValue(level) };
            hitPoints.Descriptor = ModifierDescriptor.UntypedStackable;
            hitPoints.RemoveWhenHitPointsEnd = true;
            var shroudBuff = Buff("SpiritShroudBuff", "Spirit Shroud", shroudText, roll, hitPoints,
                LevelRank("SpiritShroudSaveRank", AbilityRankType.StatBonus, "OnePlusDivStep", 4),
                new SavingThrowContextBonusAgainstDescriptor { name = "SpiritShroudResistance", ModifierDescriptor = ModifierDescriptor.Resistance,
                    SpellDescriptor = SpellDescriptor.Death | SpellDescriptor.Fear | SpellDescriptor.NegativeLevel,
                    Value = Rank(AbilityRankType.StatBonus) });
            var shroudApply = Occultist0099Transmutation.Timed(shroudBuff, PropertyValue(level), DurationRate.Minutes, true);
            shroudApply.IsNotDispelable = true;
            var shroud = Power("SpiritShroudAbility", "Spirit Shroud", shroudText, AbilityType.Supernatural,
                AbilityRange.Personal, falseLife, Run(shroudApply));
            shroud.NotOffensive = true;
            shroud.EffectOnAlly = AbilityEffectOnUnit.Helpful;

            const string puppetText = "As a full-round action, spend 1 point of Necromancy focus to animate a bone, doll or skull as a familiar. It uses your occultist level as your wizard level and remains animated for 10 minutes per occultist level. Only one soulbound puppet can be active at a time, and you cannot create one while you already have a familiar of your own.";
            var puppetBuffs = familiars.Select(familiar => Identify(new BlueprintBuff { IsClassFeature = true,
                Stacking = StackingType.Replace, ComponentsArray = new BlueprintComponent[] { Facts(familiar) } },
                PuppetBuffToken(familiar), "Soulbound Puppet: " + Label(familiar), puppetText, fear, text)).ToArray();
            var familiarRefs = familiars.Select(Ref<BlueprintUnitFactReference>).ToArray();
            var puppetRefs = puppetBuffs.Select(Ref<BlueprintBuffReference>).ToArray();
            var puppets = new List<BlueprintAbility>();
            for (int i = 0; i < familiars.Length; i++)
            {
                var grant = Occultist0099Transmutation.Timed(puppetBuffs[i], PropertyValue(level), DurationRate.TenMinutes, true);
                grant.IsNotDispelable = true;
                var release = puppetBuffs.Select(other => (GameAction)Remove(other)).Append(grant).ToArray();
                var puppet = Power(PuppetToken(familiars[i]), "Soulbound Puppet: " + Label(familiars[i]), puppetText,
                    AbilityType.Supernatural, AbilityRange.Personal, fear, Run(release),
                    new Occultist0099SoulboundPuppet { name = "OccultistSoulboundPuppet", Familiars = familiarRefs, Puppets = puppetRefs });
                puppet.GetComponent<Occultist0099FocusResourceLogic>().CheckCasterRestrictions = true;
                puppet.SetIsFullRoundAction(true);
                puppet.NotOffensive = true;
                puppet.EffectOnAlly = AbilityEffectOnUnit.Helpful;
                puppets.Add(puppet);
            }
            var puppetRoot = Occultist0099Illusion.Variants("SoulboundPuppetAbility", "Soulbound Puppet", puppetText, fear, text,
                SpellSchool.Necromancy, puppets.ToArray());
            puppetRoot.Range = AbilityRange.Personal;
            puppetRoot.SetIsFullRoundAction(true);
            puppetRoot.NotOffensive = true;
            puppetRoot.EffectOnAlly = AbilityEffectOnUnit.Helpful;

            const string servantText = "Spend 1 point of Necromancy focus to raise a dead creature as a skeleton servant for 10 minutes per occultist level. The servant has Hit Dice equal to your occultist level and adds half your occultist level to its damage rolls. At 9th level it gains the bloody template. At 13th level you instead animate every eligible corpse in a 10-foot burst. At 17th level your servants gain your teamwork feats. You can maintain a number of servants equal to half your occultist level.";
            var pool = Identify(new BlueprintSummonPool { ComponentsArray = Array.Empty<BlueprintComponent>() },
                "NecromanticServantPool", "", "", null);
            var consumed = Identify(new BlueprintFeature { Ranks = 1, HideInUI = true, HideInCharacterSheetAndLevelUp = true,
                ComponentsArray = Array.Empty<BlueprintComponent>() }, "NecromanticServantConsumed", "", "", null, text);
            var excluded = new[] { undead, construct, elemental, aberration, vermin, plant };
            var halfLevel = LevelRank("ServantHalfLevel", AbilityRankType.Default, "Div2", 0);
            var servantDamage = Buff("NecromanticServantDamageBuff", "Necromantic Servant", servantText, halfLevel,
                new AddContextStatBonus { name = "ServantDamage", Stat = StatType.AdditionalDamage,
                    Descriptor = ModifierDescriptor.UntypedStackable, Value = Rank(AbilityRankType.Default) });
            SetEnum(servantDamage, "m_Flags", "HiddenInUi");
            var bloody = Buff("NecromanticServantBloodyBuff", "Bloody Skeleton",
                "Bloody skeletons have fast healing equal to half their Hit Dice and a +4 bonus to Charisma.",
                LevelRank("ServantBloodyHealing", AbilityRankType.Default, "Div2", 0),
                new AddStatBonus { name = "ServantBloodyCharisma", Stat = StatType.Charisma, Value = 4, Descriptor = ModifierDescriptor.Feat },
                new AddEffectContextFastHealing { name = "ServantBloodyHealing", Bonus = Rank(AbilityRankType.Default) });
            var excludedRefs = excluded.Select(Ref<BlueprintUnitFactReference>).ToArray();
            var poolRef = Ref<BlueprintSummonPoolReference>(pool);
            var consumedRef = Ref<BlueprintFeatureReference>(consumed);
            var single = Power("NecromanticServantSingleAbility", "Necromantic Servant", servantText, AbilityType.SpellLike,
                AbilityRange.Close, fear, new AbilityCanTargetDead { name = "ServantTargetsDead" },
                new Occultist0099ServantTarget { name = "OccultistServantCorpse", Consumed = consumedRef, ExcludedTypes = excludedRefs },
                CapRank(), TemplateRank(), Run(Raise()), new Occultist0099PowerLevel { name = "ServantSingleWindow", Maximum = 12 });
            single.CanTargetSelf = false;
            single.CanTargetPoint = false;
            single.CanTargetEnemies = true;
            single.NotOffensive = true;
            var burst = new AbilityTargetsAround { name = "ServantBurst" };
            Set(burst, "m_Radius", new Feet(10));
            SetEnum(burst, "m_TargetType", "Any");
            Set(burst, "m_IncludeDead", true);
            Set(burst, "m_Condition", new ConditionsChecker { Operation = Kingmaker.ElementsSystem.Operation.And,
                Conditions = new Condition[] { new Occultist0099ServantCondition { name = "ServantBurstCorpse",
                    Consumed = consumedRef, ExcludedTypes = excludedRefs } } });
            var servantArea = Power("NecromanticServantAreaAbility", "Necromantic Servant (Area)", servantText, AbilityType.SpellLike,
                AbilityRange.Close, fear, new AbilityCanTargetDead { name = "ServantAreaTargetsDead" }, burst,
                CapRank(), TemplateRank(), Run(Raise()), new Occultist0099PowerLevel { name = "ServantAreaWindow", Minimum = 13 });
            servantArea.CanTargetPoint = servantArea.CanTargetFriends = servantArea.CanTargetEnemies = servantArea.CanTargetSelf = true;
            servantArea.NotOffensive = true;
            var servantRoot = Occultist0099Illusion.Variants("NecromanticServantAbility", "Necromantic Servant", servantText, fear,
                text, SpellSchool.Necromancy, single, servantArea);
            servantRoot.NotOffensive = true;

            var invest = Operation("NecromancyInvestFocusAbility", "Invest Mental Focus: Necromancy",
                "Invest 1 point of unallocated mental focus in Necromancy before locking your focus.",
                Occultist0099FocusAction.Invest, UnitCommand.CommandType.Free, fear, text, School);
            var replenish = Operation("NecromancyReplenishFocusAbility", "Replenish Mental Focus: Necromancy",
                "Spend 1 unallocated point of mental focus to replenish a spent Necromancy point.",
                Occultist0099FocusAction.Replenish, UnitCommand.CommandType.Free, fear, text, School);
            var implement = Identify(new BlueprintFeature { Ranks = 1, IsClassFeature = true, ComponentsArray = new BlueprintComponent[]
                { new Occultist0099ImplementSchool { name = "OccultistNecromancySchool", School = School }, Facts(mindFear, invest, replenish) } },
                "NecromancyImplementFocus", "Necromancy Implement", "Gain Mind Fear and Necromantic Focus.", fear, text);
            return new BlueprintScriptableObject[] { level, dc, focus, mindFear, rot, rotDelivery, pain, shroudBuff, shroud,
                invest, replenish, implement, puppetRoot, pool, consumed, servantDamage, bloody, single, servantArea, servantRoot }
                .Concat(puppetBuffs).Concat(puppets).ToArray();

            BlueprintAbility Power(string token, string label, string description, AbilityType type, AbilityRange range, BlueprintAbility icon,
                params BlueprintComponent[] components)
                => Identify(Occultist0099Illusion.NewSchoolPower(SpellSchool.Necromancy, type, range, 1, level, dc, components),
                    token, label, description, icon, text);

            BlueprintBuff Buff(string token, string label, string description, params BlueprintComponent[] components)
                => Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace, ComponentsArray = components },
                    token, label, description, falseLife, text);

            ContextRankConfig CapRank() => LevelRank("ServantMaximumUnits", AbilityRankType.DamageDiceAlternative, "Div2", 0);

            ContextRankConfig TemplateRank()
            {
                var rank = LevelRank("ServantTemplateRank", AbilityRankType.DamageDice, "DelayedStartPlusDivStep", 8);
                Set(rank, "m_StartLevel", 9);
                return rank;
            }

            Occultist0099AnimateServant Raise()
            {
                var spawn = CopyData(nativeSpawn);
                Set(spawn, "m_SummonPool", poolRef);
                spawn.LevelValue = PropertyValue(level);
                spawn.CountValue = new ContextDiceValue { DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = 1 };
                spawn.DurationValue = new ContextDurationValue { Rate = DurationRate.TenMinutes, DiceType = DiceType.Zero,
                    DiceCountValue = 0, BonusValue = PropertyValue(level) };
                // Keep the native summon's own AfterSpawn (tier marker buff and the
                // Cruoromancer / Reanimator branches) and append the servant effects.
                spawn.AfterSpawn = Actions(spawn.AfterSpawn.Actions.Concat(new GameAction[]
                {
                    Permanent(servantDamage),
                    When(AtLeast(AbilityRankType.DamageDice, 1), new GameAction[] { Permanent(bloody) }, Array.Empty<GameAction>()),
                    When(AtLeast(AbilityRankType.DamageDice, 2), new GameAction[] { Permanent(teamwork) }, Array.Empty<GameAction>())
                }).ToArray());
                return new Occultist0099AnimateServant { name = "OccultistRaiseServant", Pool = poolRef, Consumed = consumedRef,
                    ExcludedTypes = excludedRefs, MaximumUnits = Rank(AbilityRankType.DamageDiceAlternative), Spawn = spawn };
            }
        }

        private static string Label(BlueprintFeature familiar)
        {
            string name = familiar.name.Replace("FamiliarBondFeature", "");
            return System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        }

        private static ContextActionRemoveBuff Remove(BlueprintBuff buff)
        {
            var remove = new ContextActionRemoveBuff { name = "SoulboundPuppetRelease", ToCaster = true };
            Set(remove, "m_Buff", Ref<BlueprintBuffReference>(buff));
            return remove;
        }

        private static ContextActionApplyBuff Permanent(BlueprintBuff buff)
        {
            var apply = Apply(buff, 0);
            apply.Permanent = true;
            apply.IsNotDispelable = true;
            apply.IsFromSpell = false;
            return apply;
        }

        private static ContextConditionCompare AtLeast(AbilityRankType rank, int value)
        {
            var compare = new ContextConditionCompare { CheckValue = Rank(rank), TargetValue = value };
            SetEnum(compare, "m_Type", "GreaterOrEqual");
            return compare;
        }

        internal static bool HoldsFleshRot(UnitPartTouch part) => part?.Ability?.Blueprint.AssetGuid == FleshRotDeliveryId;

        private static ContextValue Rank(AbilityRankType type) => new ContextValue { ValueType = ContextValueType.Rank, ValueRank = type };

        private static ContextRankConfig LevelRank(string name, AbilityRankType type, string progression, int step)
        {
            var result = new ContextRankConfig { name = name };
            SetEnum(result, "m_BaseValueType", "CasterLevel");
            Set(result, "m_Type", type);
            SetEnum(result, "m_Progression", progression);
            Set(result, "m_StepLevel", step);
            return result;
        }

        private static ContextActionApplyBuff D4(BlueprintBuff buff)
        {
            var apply = Apply(buff, 0);
            apply.DurationValue.DiceType = DiceType.D4;
            apply.DurationValue.DiceCountValue = 1;
            return apply;
        }

        private static SpellDescriptorComponent Descriptor(SpellDescriptor descriptor)
            => new SpellDescriptorComponent { name = "OccultistNecromancyDescriptors", Descriptor = descriptor };

        private static AbilityTargetHasFact Exclude(params BlueprintFeature[] features)
        {
            var rule = new AbilityTargetHasFact { name = "OccultistNecromancyLivingTarget", Inverted = true };
            Set(rule, "m_CheckedFacts", features.Select(Ref<BlueprintUnitFactReference>).ToArray());
            return rule;
        }

        private static ConditionsChecker Forbidden(params BlueprintFeature[] features)
            => new ConditionsChecker { Operation = Kingmaker.ElementsSystem.Operation.Or,
                Conditions = features.Select(feature => CheckType(feature, false)).ToArray() };

        private static ConditionsChecker LivingOther(params BlueprintFeature[] features)
            => new ConditionsChecker { Operation = Kingmaker.ElementsSystem.Operation.And, Conditions = features.Select(feature => CheckType(feature, true))
                .Append(new ContextConditionTargetIsYourself { Not = true }).ToArray() };

        private static Condition CheckType(BlueprintFeature feature, bool inverted)
        {
            var check = new ContextConditionHasFact { Not = inverted };
            Set(check, "m_Fact", Ref<BlueprintUnitFactReference>(feature));
            return check;
        }

        private static Conditional When(Condition condition, GameAction[] yes, GameAction[] no)
            => When(new ConditionsChecker { Conditions = new[] { condition } }, yes, no);

        private static Conditional When(ConditionsChecker conditions, GameAction[] yes, GameAction[] no)
            => new Conditional { ConditionsChecker = conditions, IfTrue = Actions(yes), IfFalse = Actions(no) };

        private static void Harmful(BlueprintAbility ability, bool spellResistance)
        {
            ability.CanTargetSelf = false;
            ability.CanTargetEnemies = true;
            ability.EffectOnEnemy = ability.EffectOnAlly = AbilityEffectOnUnit.Harmful;
            ability.SpellResistance = spellResistance;
        }
    }

    [TypeId(Occultist0099Ids.NecromanticFocusTypeId)]
    public sealed class Occultist0099NecromanticFocus : UnitBuffComponentDelegate, IGlobalRulebookHandler<RuleSavingThrow>
    {
        public BlueprintUnitFactReference Undead;

        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            var source = evt.Reason?.Context?.SourceAbilityContext;
            if (source?.MaybeCaster != Owner || source.Ability?.SourceItem != null ||
                source.Ability?.Spellbook?.Blueprint.GetComponent<Occultist0099SpellbookMarker0099>() == null ||
                source.Ability.Blueprint.Type != AbilityType.Spell || Undead?.Get() is not BlueprintFeature undead ||
                !evt.Initiator.Descriptor.HasFact(undead)) return;
            int bonus = Occultist0099NativeScope.InRuntimeContext(
                Occultist0099NativeScope.RuntimeOf(Owner, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099)),
                component => ((Occultist0099FocusFact0099)component).WardingTalismanResonance(Occultist0099Necromancy.School), 0);
            if (bonus > 0) evt.AddBonusDC(bonus);
        }

        public void OnEventDidTrigger(RuleSavingThrow evt) { }
    }
}
