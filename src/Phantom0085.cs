// COTW-0085 Phantom graph: private class, shared progression, six emotion
// archetypes and marked units, plus the 24 phantom emotion features and the
// master ability builders. Contract C-0085-20261115-r1. Native shared roots
// are read-only donors; every private asset carries the 0085 salt.
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.UnitLogic;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.ElementsSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.Spiritualist0085Ids;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Phantom0085
    {
        private static readonly int[] GrowthLevels = { 2, 4, 6, 8, 9, 12, 13, 15 };

        private static BlueprintGuid Id(string token) => Spiritualist0085Ids.Id(token);
        private static Kingmaker.Localization.LocalizedString Text(string token, string suffix, string value) =>
            PsychicAssets.Text("Spiritualist0085" + token, suffix, value);

        internal static string NameOf(BlueprintUnitFact fact) => PsychicAssets.PeekText(fact) ?? fact?.name ?? "";
        internal static string DescOf(BlueprintUnitFact fact) => PsychicAssets.PeekText(fact, true) ?? "";

        // ---- Phantom class (data from Fighter/Animal donors, hidden) ----
        internal static BlueprintCharacterClass BuildPhantomClass(FactoryShim f, BlueprintCharacterClass fighter,
            BlueprintCharacterClass animal, BlueprintCharacterClass druid, BlueprintStatProgression savesLow)
        {
            var cls = f.Add(new BlueprintCharacterClass
            {
                SkillPoints = druid.SkillPoints - 1,
                IsArcaneCaster = false, IsDivineCaster = false, PrestigeClass = false,
                HideInUI = true,
                ClassSkills = new[] { StatType.SkillPersuasion, StatType.SkillPerception, StatType.SkillLoreReligion, StatType.SkillStealth },
                RecommendedAttributes = Array.Empty<StatType>(), NotRecommendedAttributes = Array.Empty<StatType>(),
                PrimaryColor = fighter.PrimaryColor, SecondaryColor = fighter.SecondaryColor,
                StartingGold = fighter.StartingGold,
                MaleEquipmentEntities = animal.MaleEquipmentEntities,
                FemaleEquipmentEntities = animal.FemaleEquipmentEntities,
                ComponentsArray = new BlueprintComponent[] { new PrerequisiteIsPet { name = "Phantom0085OnlyPet" } }
            }, "PhantomClass", "Phantom",
                "A phantom was once a sentient creature whose emotional trauma ripped it from the flow of spirits. It found shelter within the consciousness of a spiritualist.");
            Set(cls, "HitDie", DiceType.D10);
            Set(cls, "m_BaseAttackBonus", Get(fighter, "m_BaseAttackBonus"));
            Set(cls, "m_FortitudeSave", Ref<BlueprintStatProgressionReference>(savesLow));
            Set(cls, "m_ReflexSave", Ref<BlueprintStatProgressionReference>(savesLow));
            Set(cls, "m_WillSave", Ref<BlueprintStatProgressionReference>(savesLow));
            Set(cls, "m_StartingItems", Get(animal, "m_StartingItems"));
            Set(cls, "m_EquipmentEntities", Get(animal, "m_EquipmentEntities"));
            Set(cls, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(cls, "m_Icon", fighter.Icon);
            return cls;
        }

        // ---- Shared phantom progression (levels follow the master) ----
        internal static BlueprintProgression BuildPhantomProgression(FactoryShim f, BlueprintCharacterClass phantomClass)
        {
            var native = f.Cache;
            T Native<T>(string guid) where T : SimpleBlueprint => native.Load(BlueprintGuid.Parse(guid)) as T;

            var outsider = Native<BlueprintFeature>(OutsiderTypeId);
            var ray = Native<BlueprintFeature>(RayCalculateId);
            var touch = Native<BlueprintFeature>(TouchCalculateId);
            var devotion = Native<BlueprintFeature>(DevotionId);
            var naturalArmor = Native<BlueprintFeature>(NaturalArmorGrowthId);
            var attrGrowth = BuildAttributeGrowth(f, "PhantomDexChaGrowth", StatType.Dexterity);
            var multiAttack = Native<BlueprintFeature>("a634f97d23662b347872ffcf580495b3");

            // DR 5/slashing at 1; DR magic 5/10/15 at 4/8/12; DR 15/- text at 15
            // (original component actually sets 5/slashing; contract keeps code).
            var drSlashing = f.Feature("PhantomDR5Slashing", "Damage Reduction I", "A phantom has DR 5/slashing.",
                new AddDamageResistancePhysical { name = "Phantom0085DR5Slashing", Value = new ContextValue { Value = 5 }, BypassedByForm = true, Form = PhysicalDamageForm.Slashing });
            var drMagic = f.Feature("PhantomDRMagic", "Damage Reduction II",
                "At 4th level, the phantom gains DR 5/magic. At 8th level it increases to 10/magic. At 12th level it increases to 15/magic.",
                new AddDamageResistancePhysical { name = "Phantom0085DRMagic", Value = new ContextValue { ValueType = ContextValueType.Rank }, BypassedByMagic = true },
                Rank(phantomClass, "Custom", 7, 5, 11, 10, 20, 15));
            // C-0085 revision: the original component actually set 5/slashing
            // while the prose says 15/-; the frozen contract implements 15/-.
            var dr15 = f.Feature("PhantomDR15", "Damage Reduction III", "At 15th level, the phantom's damage reduction becomes DR 15/-.",
                new AddDamageResistancePhysical { name = "Phantom0085DR15", Value = new ContextValue { Value = 15 } });

            // Slam dice: standard 1d6/1d8/1d10/2d6/2d8 at 1/4/7/10/13.
            var slamDice = f.Feature("PhantomSlamDamage", "Slam Damage",
                "Phantoms have two slam natural weapon attacks. Medium phantom slam damage is 1d6 at levels 1-3, 1d8 at 4-6, 1d10 at 7-9, 2d6 at 10-12 and 2d8 at 13+.",
                new Phantom0085SlamDice { name = "Phantom0085SlamDice", Large = false },
                SlamRank(phantomClass));
            var magicAttacks = f.Feature("PhantomMagicAttacks", "Magic Attacks",
                "Starting from 3rd level, the phantom treats its slam attacks as magic for the purpose of overcoming damage reduction.",
                new WeaponMagic { name = "Phantom0085MagicAttacks" });
            var extraSkill = BuildExtraClassSkill(f);
            var bonusFeat = Native<BlueprintFeatureSelection>(BasicFeatsId);

            var progression = f.Progression("PhantomProgression", "Phantom", "Phantom class progression.",
                new[]
                {
                    Entry(1, outsider, drSlashing, slamDice, ray, touch),
                    Entry(2, naturalArmor, attrGrowth, extraSkill, bonusFeat),
                    Entry(3, magicAttacks),
                    Entry(4, naturalArmor, attrGrowth, drMagic),
                    Entry(5, devotion),
                    Entry(6, naturalArmor, attrGrowth),
                    Entry(7, multiAttack),
                    Entry(8, naturalArmor, attrGrowth),
                    Entry(9, naturalArmor, attrGrowth),
                    Entry(10), Entry(11),
                    Entry(12, naturalArmor, attrGrowth),
                    Entry(13, naturalArmor, attrGrowth),
                    Entry(14),
                    Entry(15, naturalArmor, attrGrowth, dr15),
                    Entry(16), Entry(17), Entry(18), Entry(19), Entry(20)
                },
                new[] { Group(slamDice, magicAttacks, devotion), Group(drSlashing, drMagic, dr15), Group(naturalArmor), Group(attrGrowth) });
            Set(progression, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(phantomClass) } });
            return progression;
        }

        private static BlueprintFeatureSelection BuildExtraClassSkill(FactoryShim f)
        {
            var selection = f.Native<BlueprintFeatureSelection>(SkillFocusSelectionId);
            var options = new List<BlueprintFeature>();
            foreach (var reference in (BlueprintFeatureReference[])Get(selection, "m_AllFeatures"))
            {
                var focus = f.Cache.Load(reference.Guid) as BlueprintFeature;
                var bonus = focus?.GetComponent<AddContextStatBonus>();
                if (bonus == null) continue;
                var stat = bonus.Stat;
                var option = f.Feature("PhantomExtraSkill" + stat, "Extra Class Skill: " + stat,
                    "The phantom can choose 1 additional skill as its class skill.",
                    new AddClassSkill { name = "Phantom0085ExtraClassSkill" + stat, Skill = stat });
                options.Add(option);
            }
            var result = f.Add(new BlueprintFeatureSelection { Ranks = 1 }, "PhantomExtraClassSkill", "Extra Class Skill",
                "The phantom can choose 1 additional skill as its class skill.");
            Set(result, "m_Features", options.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(result, "m_AllFeatures", options.Select(Ref<BlueprintFeatureReference>).ToArray());
            return result;
        }

        // ---- Marked private unit per emotion ----
        internal static BlueprintUnit BuildUnit(FactoryShim f, BlueprintsCache cache, BlueprintCharacterClass phantomClass, EmotionRow row)
        {
            var spectre = f.Native<BlueprintUnit>(SpectreUnitId);
            var smilodon = f.Native<BlueprintUnit>(SmilodonUnitId);
            var ghostFx = f.Native<BlueprintBuff>(GhostFxId);
            var naturalArmor2 = f.Native<BlueprintUnitFact>(NaturalArmor2Id);
            var slam = f.Native<BlueprintItemWeapon>(SlamWeaponId);
            var dog = f.Native<BlueprintUnit>("918939943bf32ba4a95470ea696c2ba5");
            var faction = f.Native<BlueprintFaction>(NeutralsFactionId);

            if (spectre == null || smilodon == null || ghostFx == null || naturalArmor2 == null || slam == null || dog == null)
                throw new InvalidOperationException("0085 unit donor missing: spectre=" + (spectre != null) + " smilodon=" + (smilodon != null) + " ghostFx=" + (ghostFx != null) + " armor2=" + (naturalArmor2 != null) + " slam=" + (slam != null) + " dog=" + (dog != null));
            if (spectre.Body == null)
                throw new InvalidOperationException("0085 spectre body not hydrated");
            var unit = f.Add(new BlueprintUnit
            {
                Alignment = Kingmaker.Enums.Alignment.TrueNeutral,
                Strength = row.Str, Dexterity = row.Dex, Constitution = 13, Intelligence = 7, Wisdom = 10, Charisma = 13,
                Speed = 30.Feet(),
                Faction = faction,
                ComponentsArray = new BlueprintComponent[]
                {
                    new AllowDyingCondition { name = "Phantom0085AllowDying" },
                    new AddResurrectOnRest { name = "Phantom0085ResurrectOnRest" },
                    new Phantom0085Marker { name = "Phantom0085Marker", Emotion = row.Name }
                }
            }, row.Name + "PhantomUnit", row.Name + " Phantom", row.Description);
            Set(unit, "m_AddFacts", new[] { Ref<BlueprintUnitFactReference>(naturalArmor2), Ref<BlueprintUnitFactReference>(ghostFx) });
            Set(unit, "m_Brain", Get(dog, "m_Brain"));
            Set(unit, "m_Race", null);
            var body = CopyData(spectre.Body);
            Set(body, "m_EmptyHandWeapon", Ref<BlueprintItemWeaponReference>(slam));
            Set(body, "m_PrimaryHand", null);
            Set(body, "m_SecondaryHand", null);
            body.DisableHands = false;
            Set(body, "m_AdditionalLimbs", Array.Empty<BlueprintItemWeaponReference>());
            Set(body, "m_AdditionalSecondaryLimbs", Array.Empty<BlueprintItemWeaponReference>());
            unit.Body = body;
            // Visual donor: Smilodon metadata with ghost footsteps and black blood.
            // The offline host may not hydrate nested visual settings; fall back
            // to a fresh instance so registration never depends on scene assets.
            var visual = (Kingmaker.Blueprints.UnitVisualParams)(Get(smilodon, "Visual") ?? Get(spectre, "Visual"))
                ?? new Kingmaker.Blueprints.UnitVisualParams();
            visual.BloodType = Kingmaker.Visual.HitSystem.BloodType.BlackUndead;
            visual.FootstepSoundSizeType = Kingmaker.Visual.Sound.FootstepSoundSizeType.Ghost;
            var spectreVisual = Get(spectre, "Visual");
            if (spectreVisual != null)
                AccessTools.Property(visual.GetType(), "Barks").SetValue(visual, AccessTools.Property(spectreVisual.GetType(), "Barks").GetValue(spectreVisual));
            unit.Visual = visual;
            Set(unit, "m_Portrait", Get(spectre, "m_Portrait"));
            return unit;
        }

        internal static BlueprintArchetype BuildArchetype(FactoryShim f, BlueprintCharacterClass phantomClass, EmotionRow row)
        {
            var savesHigh = f.Native<BlueprintStatProgression>(SavesHighId);
            var archetype = f.Add(new BlueprintArchetype
            {
                AddFeatures = Array.Empty<LevelEntry>(), RemoveFeatures = Array.Empty<LevelEntry>()
            }, row.Name + "PhantomArchetype", row.Name, row.Description);
            Set(archetype, "m_ParentClass", phantomClass);
            if (row.Fort) Set(archetype, "m_FortitudeSave", Ref<BlueprintStatProgressionReference>(savesHigh));
            if (row.Ref) Set(archetype, "m_ReflexSave", Ref<BlueprintStatProgressionReference>(savesHigh));
            if (row.Will) Set(archetype, "m_WillSave", Ref<BlueprintStatProgressionReference>(savesHigh));

            // Emotion skills: class skills plus ranks equal to phantom level.
            var skillFeature = f.Feature(row.Name + "PhantomSkills", "", "",
                new AddClassSkill { name = "Phantom0085Skill" + row.Name + "A", Skill = row.Skills[0] },
                new AddClassSkill { name = "Phantom0085Skill" + row.Name + "B", Skill = row.Skills[1] },
                new Phantom0085SkillRanks { name = "Phantom0085SkillRanks" + row.Name, Skills = row.Skills });
            skillFeature.HideInUI = true;
            skillFeature.HideInCharacterSheetAndLevelUp = true;

            var add = new List<LevelEntry> { Entry(1, skillFeature) };
            var remove = new List<LevelEntry>();
            var baseTrait = BuildBaseTrait(f, phantomClass, row);
            if (baseTrait != null) add[0] = Entry(1, skillFeature, baseTrait);

            if (row.LargeSlam)
            {
                // Anger: larger slam dice and STR/CHA growth replace DEX/CHA.
                var largeSlam = f.Feature("AngerPhantomSlamDamage", "Anger Phantom Slam Damage",
                    "Anger phantom slam damage is 1d8 at levels 1-3, 2d6 at 4-6, 2d8 at 7-9, 3d6 at 10-12 and 3d8 at 13+.",
                    new Phantom0085SlamDice { name = "Phantom0085AngerSlamDice", Large = true },
                    SlamRank(phantomClass));
                var strCha = BuildAttributeGrowth(f, "PhantomStrChaGrowth", StatType.Strength);
                add[0] = Entry(1, skillFeature, baseTrait, largeSlam);
                foreach (var level in GrowthLevels) add.Add(Entry(level, strCha));
                var dexCha = f.Find<BlueprintFeature>("PhantomDexChaGrowth");
                remove.Add(Entry(1, f.Find<BlueprintFeature>("PhantomSlamDamage")));
                foreach (var level in GrowthLevels) remove.Add(Entry(level, dexCha));
            }
            archetype.AddFeatures = add.ToArray();
            archetype.RemoveFeatures = remove.ToArray();
            return archetype;
        }

        private static BlueprintFeature BuildAttributeGrowth(FactoryShim f, string token, StatType primary)
        {
            var growth = f.Feature(token, "Phantom Prowess", "The phantom receives +1 " + primary + " and +1 Charisma.",
                new AddStatBonus { name = token + "Primary", Stat = primary, Descriptor = ModifierDescriptor.None, Value = 1 },
                new AddStatBonus { name = token + "Charisma", Stat = StatType.Charisma, Descriptor = ModifierDescriptor.None, Value = 1 });
            growth.Ranks = GrowthLevels.Length;
            return growth;
        }

        private static BlueprintFeature BuildBaseTrait(FactoryShim f, BlueprintCharacterClass phantomClass, EmotionRow row)
        {
            switch (row.BaseTrait)
            {
                case "PowerAttack":
                    return f.Feature("AngerPhantomBase", "Strength Focus",
                        "The phantom gains a +2 bonus to Strength and a -2 penalty to Dexterity, and gains Power Attack as a bonus feat.",
                        Facts(f.Native<BlueprintFeature>(PowerAttackId)));
                case "PowerFromDespair":
                    return f.Feature("DespairPhantomBase", "Power from Despair",
                        "The phantom gains a +2 bonus on attack and damage rolls against creatures that are shaken, frightened, panicked, cowering, or subject to aura of despair or crushing despair.",
                        new Phantom0085PowerFromDespair { name = "Phantom0085PowerFromDespair" });
                case "WeaponFinesse":
                    return f.Feature("HatredPhantomBase", "Weapon Finesse", "The phantom gains Weapon Finesse as a bonus feat.",
                        Facts(f.Native<BlueprintFeature>(WeaponFinesseId)));
                case "Stealthy":
                    return f.Feature("FearPhantomBase", "Stealthy", "The phantom gains Stealthy as a bonus feat.",
                        Facts(f.Native<BlueprintFeature>(StealthyId)));
                case "Tracking":
                    return f.Feature("ZealPhantomBase", "Tracking",
                        "The phantom adds half its number of Hit Dice (minimum 1) to Lore (Nature) checks.",
                        new AddContextStatBonus { name = "Phantom0085Tracking", Stat = StatType.SkillLoreNature, Descriptor = ModifierDescriptor.UntypedStackable, Value = new ContextValue { ValueType = ContextValueType.Rank } },
                        Rank(phantomClass, "Div2"));
                case "Benevolent":
                    return f.Feature("KindnessPhantomBase", "Benevolent",
                        "When the phantom uses the aid another action, the bonus granted by a successful check increases by 2.",
                        new Phantom0085Benevolent
                        {
                            name = "Phantom0085Benevolent",
                            Fallback = Ref<BlueprintAbilityReference>(BuildAidAnother(f))
                        });
                default:
                    return null;
            }
        }

        internal static void AttachArchetype(BlueprintUnit unit, BlueprintArchetype archetype, StatType[] emotionSkills)
        {
            var add = new AddClassLevels
            {
                name = "Phantom0085AddClassLevels",
                DoNotApplyAutomatically = true, Levels = 0,
                Skills = new[] { StatType.SkillAthletics, StatType.SkillMobility, StatType.SkillStealth,
                    StatType.SkillPersuasion, StatType.SkillKnowledgeArcana }.Except(emotionSkills).ToArray(),
                Selections = Array.Empty<SelectionEntry>()
            };
            Set(add, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(archetype.GetParentClass()));
            Set(add, "m_Archetypes", new[] { Ref<BlueprintArchetypeReference>(archetype) });
            Set(add, "m_MemorizeSpells", Array.Empty<BlueprintAbilityReference>());
            Set(add, "m_SelectSpells", Array.Empty<BlueprintAbilityReference>());
            unit.ComponentsArray = unit.ComponentsArray.Concat(new BlueprintComponent[] { add }).ToArray();
        }

        // ---- 24 emotion features (1/7/12/17 per emotion) ----
        internal static BlueprintFeature[] BuildEmotionFeatures(FactoryShim f, BlueprintsCache cache,
            BlueprintCharacterClass character, BlueprintCharacterClass phantomClass, EmotionRow row)
        {
            var both = new[] { phantomClass, character };
            switch (row.Name)
            {
                case "Anger": return BuildAnger(f, character, phantomClass);
                case "Despair": return BuildDespair(f, character, phantomClass);
                case "Hatred": return BuildHatred(f, character, phantomClass);
                case "Fear": return BuildFear(f, character, phantomClass);
                case "Zeal": return BuildZeal(f, character, phantomClass);
                case "Kindness": return BuildKindness(f, character, phantomClass);
                default: throw new InvalidOperationException("0085 unknown emotion " + row.Name);
            }
        }


        private static ContextActionApplyBuff Apply(BlueprintBuff buff, ContextDurationValue duration, string name, bool asChild = false)
        {
            var action = new ContextActionApplyBuff { name = name, DurationValue = duration ?? new ContextDurationValue(), AsChild = asChild };
            Set(action, "m_Buff", Ref<BlueprintBuffReference>(buff));
            return action;
        }

        private static AbilityAreaEffectBuff AreaBuff(BlueprintBuff buff, string name, params Condition[] conditions)
        {
            buff.Stacking = StackingType.Stack;
            var area = new AbilityAreaEffectBuff
            {
                name = name,
                Condition = new ConditionsChecker { Conditions = conditions },
                CheckConditionEveryRound = conditions.Length > 0
            };
            Set(area, "m_Buff", Ref<BlueprintBuffReference>(buff));
            return area;
        }

        private static ContextActionRemoveBuff RemoveFromCaster(BlueprintBuff buff, string name)
        {
            var action = new ContextActionRemoveBuff { name = name, OnlyFromCaster = true };
            Set(action, "m_Buff", Ref<BlueprintBuffReference>(buff));
            return action;
        }

        private static ContextConditionCasterHasFact HasFact(BlueprintUnitFact fact, string name, bool not = false)
        {
            var condition = new ContextConditionCasterHasFact { name = name, Not = not };
            Set(condition, "m_Fact", Ref<BlueprintUnitFactReference>(fact));
            return condition;
        }

        private static ContextConditionHasBuffFromCaster HasBuff(BlueprintBuff buff, string name, bool not = false)
        {
            var condition = new ContextConditionHasBuffFromCaster { name = name, Not = not };
            Set(condition, "m_Buff", Ref<BlueprintBuffReference>(buff));
            return condition;
        }

        private static ContextCalculateAbilityParamsBasedOnClass Params(BlueprintCharacterClass cls, StatType stat, string name)
        {
            var component = new ContextCalculateAbilityParamsBasedOnClass { name = name, StatType = stat };
            Set(component, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(cls));
            return component;
        }

        private static PrerequisiteNoFeature NoFeature(BlueprintFeature feature, string name)
        {
            var component = new PrerequisiteNoFeature { name = name };
            Set(component, "m_Feature", Ref<BlueprintFeatureReference>(feature));
            return component;
        }

        private static Conditional Cond(string name, Condition[] conditions, ActionList ifTrue, ActionList ifFalse)
            => new Conditional { name = name, ConditionsChecker = new ConditionsChecker { Conditions = conditions }, IfTrue = ifTrue, IfFalse = ifFalse };

        private static BlueprintActivatableAbility Toggle(FactoryShim f, string token, string name, string description,
            BlueprintBuff host, UnitCommand.CommandType command)
        {
            var toggle = f.Add(new BlueprintActivatableAbility
            {
                ActivationType = AbilityActivationType.WithUnitCommand,
                DeactivateIfOwnerDisabled = true
            }, token, name, description);
            Set(toggle, "m_ActivateWithUnitCommand", command);
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(host));
            return toggle;
        }

        private static BlueprintFeature AuraFeature(FactoryShim f, string token, string name, string description,
            BlueprintAbilityAreaEffect area, UnitCommand.CommandType command)
        {
            var host = AuraHost(f, token, name, description, area);
            var toggle = Toggle(f, token, name, description, host, command);
            return f.Feature(token + "Feature", name, description, Facts(toggle));
        }

        private static BlueprintBuff AuraHost(FactoryShim f, string token, string name, string description, BlueprintAbilityAreaEffect area)
        {
            var link = new AddAreaEffect { name = "Phantom0085AreaLink" + token };
            Set(link, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(area));
            var host = f.Buff(token + "Host", name, description, link);
            SetEnum(host, "m_Flags", "HiddenInUi");
            return host;
        }

        private static ContextRankConfig Rank(BlueprintCharacterClass cls, string progression, params int[] thresholds)
            => ArchetypeBatch01445.Rank(cls, progression, thresholds);

        private static ContextRankConfig SlamRank(BlueprintCharacterClass cls)
            => Rank(cls, "Custom", 3, 0, 6, 1, 9, 2, 12, 3, 20, 4);

        private static ContextRankConfig RankMulti(BlueprintCharacterClass[] classes, string progression)
        {
            var r = new ContextRankConfig();
            SetEnum(r, "m_BaseValueType", "ClassLevel");
            SetEnum(r, "m_Progression", progression);
            Set(r, "m_Class", classes.Select(Ref<BlueprintCharacterClassReference>).ToArray());
            return r;
        }

        private static WeaponTypeCriticalEdgeIncrease CritEdge(BlueprintWeaponType type, string name)
        {
            var c = new WeaponTypeCriticalEdgeIncrease { name = name };
            Set(c, "m_WeaponType", Ref<BlueprintWeaponTypeReference>(type));
            return c;
        }

        private static WeaponTypeCriticalMultiplierIncrease CritMult(BlueprintWeaponType type, string name)
        {
            var c = new WeaponTypeCriticalMultiplierIncrease { name = name, AdditionalMultiplier = 1 };
            Set(c, "m_WeaponType", Ref<BlueprintWeaponTypeReference>(type));
            return c;
        }

        private static ActivatableAbilityResourceLogic BondedCost(BlueprintAbilityResource resource)
        {
            var c = new ActivatableAbilityResourceLogic { name = "Phantom0085BondedCost", SpendType = ActivatableAbilityResourceLogic.ResourceSpendType.NewRound };
            Set(c, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
            return c;
        }

        private static AddFeatureOnClassLevel ClassLevelGrant(BlueprintCharacterClass cls, int level, BlueprintFeature feature, string name)
        {
            var grant = new AddFeatureOnClassLevel { name = name, Level = level };
            Set(grant, "m_Class", Ref<BlueprintCharacterClassReference>(cls));
            Set(grant, "m_Feature", Ref<BlueprintFeatureReference>(feature));
            Set(grant, "m_AdditionalClasses", Array.Empty<BlueprintCharacterClassReference>());
            Set(grant, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            return grant;
        }

        private static LevelEntry Entry(int level, params BlueprintFeatureBase[] facts)
        {
            var e = new LevelEntry { Level = level };
            Set(e, "m_Features", facts.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            return e;
        }

        private static UIGroup Group(params BlueprintFeatureBase[] facts)
        {
            var g = new UIGroup();
            Set(g, "m_Features", facts.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            return g;
        }

        private static AddFacts Facts(params BlueprintUnitFact[] facts)
        {
            var result = new AddFacts { name = "Phantom0085GrantedFacts" };
            Set(result, "m_Facts", facts.Select(Ref<BlueprintUnitFactReference>).ToArray());
            return result;
        }

        private static BlueprintFeature[] BuildAnger(FactoryShim f, BlueprintCharacterClass character, BlueprintCharacterClass phantomClass)
        {
            var powerfulStrikes = f.Feature("AngerPowerfulStrikes", "Powerful Strikes",
                "A phantom with this focus deals more damage with its slam attacks, dealing slam damage as a creature one size category larger. The phantom also gains Power Attack as a bonus feat.");

            var furyBuff = f.Buff("AngerAuraOfFuryEffect", "Aura of Fury",
                "Creatures within the aura gain a +2 bonus on melee attack rolls but take a -2 penalty to AC.",
                new AddStatBonus { name = "Phantom0085FuryAC", Stat = StatType.AC, Value = -2, Descriptor = ModifierDescriptor.UntypedStackable },
                new AttackTypeAttackBonus { name = "Phantom0085FuryAttack", AttackBonus = 2, Type = WeaponRangeType.Melee, Descriptor = ModifierDescriptor.UntypedStackable });
            var furyAura = f.Add(new BlueprintAbilityAreaEffect
            {
                Shape = AreaEffectShape.Cylinder, Size = 20.Feet(), AffectEnemies = true,
                ComponentsArray = new BlueprintComponent[] { AreaBuff(furyBuff, "Phantom0085FuryAura") }
            }, "AngerAuraOfFuryArea", "Aura of Fury", "");
            var auraOfFury = AuraFeature(f, "AngerAuraOfFury", "Aura of Fury",
                "When the spiritualist reaches 7th level, as a swift action, the phantom can emit a 20-foot-radius aura of fury. Creatures within the aura gain a +2 bonus on melee attack rolls but take a -2 penalty to AC. Ending the aura is a free action.",
                furyAura, UnitCommand.CommandType.Swift);

            var mienBuff = f.Buff("AngerFerociousMienEffect", "Ferocious Mien",
                "The phantom grows one size category larger and fiercer, as enlarge person and rage.");
            mienBuff.ComponentsArray = CopyComponents(f.Native<BlueprintBuff>(EnlargePersonBuffId))
                .Concat(CopyComponents(f.Native<BlueprintBuff>(RageBuffId))).ToArray();
            var mienResource = FixedResource(f, "AngerFerociousMienResource", 3);
            var masterLevel = f.Add(new BlueprintUnitProperty
            {
                ComponentsArray = new BlueprintComponent[]
                {
                    new Phantom0085MasterClassLevel { name = "Phantom0085MienMasterLevel", CharacterClass = Ref<BlueprintCharacterClassReference>(character) }
                }
            }, "AngerFerociousMienMasterLevel", "", "");
            var mienRounds = new ContextValue { ValueType = ContextValueType.CasterCustomProperty };
            Set(mienRounds, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(masterLevel));
            var mienAbility = f.Ability("AngerFerociousMienAbility", "Ferocious Mien",
                "Three times per day as a swift action, a phantom in ectoplasmic form can grow more ferocious and frightening. It becomes one size category larger, as enlarge person, and grows fiercer in combat, as rage. This effect lasts for 1 round per spiritualist class level.",
                AbilityType.Supernatural, UnitCommand.CommandType.Swift, AbilityRange.Personal,
                PsychicAssets.Run(Apply(mienBuff, new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, BonusValue = mienRounds }, "Phantom0085MienApply")),
                Cost(mienResource));
            mienAbility.CanTargetSelf = true;
            var ferociousMien = f.Feature("AngerFerociousMien", "Ferocious Mien", NameOf(mienAbility), Facts(mienAbility), Grant(mienResource));

            var wailResource = FixedResource(f, "AngerFuriousWailResource", 1);
            var wail = ConvertToSpellLike(f, f.Native<BlueprintAbility>(WailOfBansheeId), "AngerFuriousWail", phantomClass, StatType.Charisma, wailResource, supernatural: true);
            var furiousWail = f.Feature("AngerFuriousWailFeature", "Furious Wail",
                "Once per day, the phantom can emit a furious wail, as wail of the banshee, using its Hit Dice as caster level and Charisma for the save DC.",
                Facts(wail), Grant(wailResource));
            return new[] { powerfulStrikes, auraOfFury, ferociousMien, furiousWail };
        }

        private static BlueprintComponent[] CopyComponents(BlueprintBuff donor)
            => (donor.ComponentsArray ?? Array.Empty<BlueprintComponent>()).Select(c => (BlueprintComponent)CopyData(c)).ToArray();

        private static BlueprintAbilityResource FixedResource(FactoryShim f, string token, int value)
        {
            var r = f.Add(new BlueprintAbilityResource(), token, "", "");
            var amount = Get(r, "m_MaxAmount");
            Set(amount, "BaseValue", value);
            Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            Set(r, "m_MaxAmount", amount);
            return r;
        }

        internal static AddAbilityResources Grant(BlueprintAbilityResource resource)
        {
            var c = new AddAbilityResources { RestoreAmount = true };
            Set(c, "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
            return c;
        }

        private static AbilityResourceLogic Cost(BlueprintAbilityResource resource, int amount = 1)
        {
            var c = new AbilityResourceLogic { Amount = amount };
            Set(c, "m_IsSpendResource", true);
            Set(c, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
            return c;
        }

        // Convert a native spell root into a private spell-like/supernatural
        // ability with its own resource, class-based caster level and DC stat.
        internal static BlueprintAbility ConvertToSpellLike(FactoryShim f, BlueprintAbility root,
            string token, BlueprintCharacterClass casterClass, StatType dcStat,
            BlueprintAbilityResource resource, bool supernatural = false)
        {
            if (root == null) throw new InvalidOperationException("Missing spell-like ability variant for " + token);
            var ability = CopyBlueprint(root);
            ability.AssetGuid = Id(token);
            ability.name = Spiritualist0085Ids.AssetPrefix + token;
            ability.Type = supernatural ? AbilityType.Supernatural : AbilityType.SpellLike;
            ability.MaterialComponent = new BlueprintAbility.MaterialComponentData();
            if (supernatural)
            {
                ability.SpellResistance = false;
                ability.AvailableMetamagic = 0;
            }
            Set(ability, "m_DisplayName", Text(token, "name", NameOf(root)));
            Set(ability, "m_Description", Text(token, "description", DescOf(root)));
            ability.Parent = null;
            var components = new List<BlueprintComponent>();
            foreach (var c in ability.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                if (c is SpellListComponent || c is AbilityResourceLogic || c is ContextAbilityParamsCalculator ||
                    (supernatural && c is SpellComponent)) continue;
                if (c is AbilityVariants variants)
                {
                    var children = variants.Variants.Select((variant, i) =>
                        ConvertToSpellLike(f, variant, token + "Option" + i, casterClass, dcStat, resource, supernatural)).ToArray();
                    var menu = new AbilityVariants { name = variants.name };
                    Set(menu, "m_Variants", children.Select(Ref<BlueprintAbilityReference>).ToArray());
                    foreach (var child in children) child.Parent = ability;
                    components.Add(menu);
                    continue;
                }
                if (c is ContextRankConfig rank)
                {
                    var clone = (ContextRankConfig)CopyData(rank);
                    Set(clone, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(casterClass) });
                    components.Add(clone);
                    continue;
                }
                components.Add(c);
            }
            components.Add(Params(casterClass, dcStat, "Phantom0085SlaParams" + token));
            if (!components.Any(c => c is AbilityVariants)) components.Add(Cost(resource));
            ability.ComponentsArray = components.ToArray();
            f.Register(ability, token);
            return ability;
        }

        private static BlueprintFeature[] BuildDespair(FactoryShim f, BlueprintCharacterClass character, BlueprintCharacterClass phantomClass)
        {
            var both = new[] { phantomClass, character };
            var miserableBuff = f.Buff("DespairMiserableStrikesEffect", "Miserable Strikes",
                "The creature takes a -2 penalty on attack and damage rolls for 1 round. This is a mind-affecting fear and emotion effect.",
                new AddStatBonus { name = "Phantom0085MiserableAttack", Stat = StatType.AdditionalAttackBonus, Value = -2, Descriptor = ModifierDescriptor.UntypedStackable },
                new AddStatBonus { name = "Phantom0085MiserableDamage", Stat = StatType.AdditionalDamage, Value = -2, Descriptor = ModifierDescriptor.UntypedStackable },
                new SpellDescriptorComponent { name = "Phantom0085MiserableDescriptor", Descriptor = Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.MindAffecting | Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Fear });
            var inescapable = f.Feature("DespairInescapableDespair", "Inescapable Despair",
                "When the spiritualist reaches 17th level, if the phantom hits with its slam attack, the creature hit doesn't get a save to resist the effects of miserable strike.");
            var applyBuff = Apply(miserableBuff, new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, BonusValue = 1 }, "Phantom0085MiserableApply");
            var save = new ContextActionSavingThrow { name = "Phantom0085MiserableSave", Type = SavingThrowType.Will, Actions = PsychicAssets.Actions(new ContextActionConditionalSaved { name = "Phantom0085MiserableSaved", Failed = PsychicAssets.Actions(applyBuff) }) };
            var conditional = Cond("Phantom0085MiserableConditional", new Condition[] { HasFact(inescapable, "Phantom0085MiserableNoSave") }, PsychicAssets.Actions(applyBuff), PsychicAssets.Actions(save));
            var miserableStrike = f.Feature("DespairMiserableStrike", "Miserable Strikes",
                "If the phantom hits a creature with a slam attack, that creature must succeed at a Will save (DC = 10 + 1/2 the phantom's Hit Dice + the phantom's Charisma modifier) or take a -2 penalty on attack and damage rolls for 1 round. This is a mind-affecting fear and emotion effect. Penalties from multiple hits don't stack.",
                new AddInitiatorAttackWithWeaponTrigger { name = "Phantom0085MiserableTrigger", OnlyHit = true, CheckWeaponCategory = true, Category = WeaponCategory.OtherNaturalWeapons, Action = PsychicAssets.Actions(conditional) },
                Params(phantomClass, StatType.Charisma, "Phantom0085MiserableParams"),
                new SpellDescriptorComponent { name = "Phantom0085MiserableStrikeDescriptor", Descriptor = Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.MindAffecting | Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Fear });

            var despairBuff = f.Buff("DespairAuraOfDespairEffect", "Aura of Despair",
                "Enemies within the aura take a -2 penalty on all saving throws. This is a fear effect.",
                new AddStatBonus { name = "Phantom0085DespairFort", Stat = StatType.SaveFortitude, Value = -2, Descriptor = ModifierDescriptor.UntypedStackable },
                new AddStatBonus { name = "Phantom0085DespairRef", Stat = StatType.SaveReflex, Value = -2, Descriptor = ModifierDescriptor.UntypedStackable },
                new AddStatBonus { name = "Phantom0085DespairWill", Stat = StatType.SaveWill, Value = -2, Descriptor = ModifierDescriptor.UntypedStackable });
            var despairAura = f.Add(new BlueprintAbilityAreaEffect
            {
                Shape = AreaEffectShape.Cylinder, Size = 20.Feet(), AffectEnemies = true,
                ComponentsArray = new BlueprintComponent[]
                {
                    AreaBuff(despairBuff, "Phantom0085DespairAura", new ContextConditionIsEnemy { name = "Phantom0085DespairEnemy" })
                }
            }, "DespairAuraOfDespairArea", "Aura of Despair", "");
            var auraOfDespair = AuraFeature(f, "DespairAuraOfDespair", "Aura of Despair",
                "When the spiritualist reaches 7th level, as a swift action, the phantom can emit a 20-foot aura of despair. Enemies within the aura take a -2 penalty on all saving throws. This is a fear effect. Deactivating the aura is a free action.",
                despairAura, UnitCommand.CommandType.Swift);

            var shoutResource = FixedResource(f, "DespairDespairingShoutResource", 3);
            var shout = ConvertToSpellLike(f, f.Native<BlueprintAbility>(CrushingDespairSpellId), "DespairDespairingShout", phantomClass, StatType.Charisma, shoutResource, supernatural: true);
            var despairingShout = f.Feature("DespairDespairingShoutFeature", "Despairing Shout",
                "Three times per day as a standard action, the phantom can emit a shout that acts as crushing despair. The phantom uses its Hit Dice as its caster level, and the DC equals 10 + 1/2 the phantom's Hit Dice + the phantom's Charisma modifier.",
                Facts(shout), Grant(shoutResource));
            return new[] { miserableStrike, auraOfDespair, despairingShout, inescapable };
        }

        private static BlueprintFeature[] BuildHatred(FactoryShim f, BlueprintCharacterClass character, BlueprintCharacterClass phantomClass)
        {
            var both = new[] { phantomClass, character };
            var hatedBuff = f.Buff("HatredHatedTargetEffect", "Hated Target",
                "The phantom gains a +2 bonus on attack rolls against its hated target, and a bonus on damage rolls equal to 1/2 its Hit Dice (minimum 1). Only one target at a time; the bonuses remain until the target is dead or out of line of sight for 1 minute.",
                new AttackBonusAgainstTarget { name = "Phantom0085HatedAttack", Value = new ContextValue { Value = 2 }, CheckCaster = true },
                new DamageBonusAgainstTarget { name = "Phantom0085HatedDamage", Value = new ContextValue { ValueType = ContextValueType.Rank }, CheckCaster = true },
                RankMulti(both, "Div2"),
                new UniqueBuff { name = "Phantom0085HatedUnique" });
            SetEnum(hatedBuff, "m_Flags", "RemoveOnRest");
            hatedBuff.Stacking = StackingType.Stack;
            var markTarget = Apply(hatedBuff, new ContextDurationValue(), "Phantom0085HatedApply");
            markTarget.Permanent = true;
            var hatedMove = f.Ability("HatredHatedTargetAbility", "Hated Target",
                "The phantom can take a move action to designate one creature within its line of sight as a hated target. At 7th level, this becomes a swift action.",
                AbilityType.Supernatural, UnitCommand.CommandType.Move, AbilityRange.Medium,
                PsychicAssets.Run(markTarget));
            hatedMove.CanTargetEnemies = true;
            hatedMove.CanTargetFriends = false;
            var hatedTarget = f.Feature("HatredHatedTarget", "Hated Target", NameOf(hatedMove), Facts(hatedMove));

            var hatefulBuff = f.Buff("HatredHatefulAuraEffect", "Hateful Aura",
                "Enemies within the aura that deal damage to the spiritualist or the phantom take damage equal to the phantom's Charisma bonus. This is a mind-affecting pain effect.",
                new Phantom0085HatefulAura { name = "Phantom0085HatefulAura" },
                new SpellDescriptorComponent { name = "Phantom0085HatefulDescriptor", Descriptor = Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.MindAffecting });
            var hatefulAura = f.Add(new BlueprintAbilityAreaEffect
            {
                Shape = AreaEffectShape.Cylinder, Size = 20.Feet(), AffectEnemies = true,
                ComponentsArray = new BlueprintComponent[]
                {
                    AreaBuff(hatefulBuff, "Phantom0085HatefulArea", new ContextConditionIsEnemy { name = "Phantom0085HatefulEnemy" })
                }
            }, "HatredHatefulAuraArea", "Hateful Aura", "");
            var hatefulAuraFeature = AuraFeature(f, "HatredHatefulAura", "Hateful Aura",
                "When the spiritualist reaches 7th level, as a swift action, the phantom can emit a 20-foot-radius aura that inflicts pain upon the minds of those who dare damage it or its master.",
                hatefulAura, UnitCommand.CommandType.Swift);

            var sneakAttack = f.Feature("HatredSneakAttack", "Sneak Attack",
                "The phantom deals sneak attack damage when it flanks or catches its hated target flat-footed.",
                new Phantom0085HatredSneakAttack { name = "Phantom0085SneakAttack", Hated = Ref<BlueprintBuffReference>(hatedBuff) },
                Rank(phantomClass, "Custom", 13, 3, 20, 5));

            var sharedBuff = f.Buff("HatredSharedHatredEffect", "Shared Hatred",
                "When the phantom designates a hated enemy, its allies also gain a +2 bonus on attack rolls and a +4 bonus on damage rolls against that enemy.",
                new AttackBonusAgainstTarget { name = "Phantom0085SharedAttack", Value = new ContextValue { Value = 2 }, CheckCasterFriend = true },
                new DamageBonusAgainstTarget { name = "Phantom0085SharedDamage", Value = new ContextValue { Value = 4 }, CheckCasterFriend = true });
            var sharedHatred = f.Feature("HatredSharedHatred", "Shared Hatred", NameOf(sharedBuff));
            sharedBuff.Stacking = StackingType.Stack;
            var grantShared = Apply(sharedBuff, new ContextDurationValue(), "Phantom0085SharedApply", asChild: true);
            grantShared.Permanent = true;
            var sharedConditional = Cond("Phantom0085SharedConditional", new Condition[] { HasFact(sharedHatred, "Phantom0085SharedCheck") },
                PsychicAssets.Actions(grantShared), PsychicAssets.Actions());
            hatedBuff.ComponentsArray = hatedBuff.ComponentsArray.Concat(new BlueprintComponent[]
            {
                new AddFactContextActions { name = "Phantom0085ShareHatedTarget", Activated = PsychicAssets.Actions(sharedConditional) }
            }).ToArray();
            return new[] { hatedTarget, hatefulAuraFeature, sneakAttack, sharedHatred };
        }

        private static BlueprintFeature[] BuildFear(FactoryShim f, BlueprintCharacterClass character, BlueprintCharacterClass phantomClass)
        {
            var shaken = f.Native<BlueprintBuff>(ShakenBuffId);
            var frightened = f.Native<BlueprintBuff>(FrightenedBuffId);
            var frightfulBuff = f.Buff("FearFrightfulAttackEffect", "Frightful Attack",
                "When the spiritualist reaches 12th level, if the phantom hits with a slam attack, it can frighten those it hits instead of causing them to be shaken (the phantom chooses when it makes the attack). This is a mind-affecting fear effect.");
            var frightfulToggle = f.Add(new BlueprintActivatableAbility
            {
                ActivationType = AbilityActivationType.Immediately,
                DeactivateImmediately = true
            }, "FearFrightfulAttack", "Frightful Attack", NameOf(frightfulBuff));
            Set(frightfulToggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);
            Set(frightfulToggle, "m_Buff", Ref<BlueprintBuffReference>(frightfulBuff));
            var frightfulAttack = f.Feature("FearFrightfulAttackFeature", "Frightful Attack", NameOf(frightfulBuff), Facts(frightfulToggle));

            var applyShaken = Apply(shaken, new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.D4, DiceCountValue = 1 }, "Phantom0085HorrifyingShaken");
            var applyFrightened = Apply(frightened, new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.D4, DiceCountValue = 1 }, "Phantom0085HorrifyingFrightened");
            var saveShaken = new ContextActionSavingThrow { name = "Phantom0085HorrifyingSaveShaken", Type = SavingThrowType.Will, Actions = PsychicAssets.Actions(new ContextActionConditionalSaved { name = "Phantom0085HorrifyingSavedShaken", Failed = PsychicAssets.Actions(applyShaken) }) };
            var saveFrightened = new ContextActionSavingThrow { name = "Phantom0085HorrifyingSaveFrightened", Type = SavingThrowType.Will, Actions = PsychicAssets.Actions(new ContextActionConditionalSaved { name = "Phantom0085HorrifyingSavedFrightened", Failed = PsychicAssets.Actions(applyFrightened) }) };
            var effect = Cond("Phantom0085HorrifyingChoice", new Condition[] { HasFact(frightfulBuff, "Phantom0085HorrifyingFrightful") }, PsychicAssets.Actions(saveFrightened), PsychicAssets.Actions(saveShaken));
            var horrifyingStrike = f.Feature("FearHorrifyingStrike", "Horrifying Strike",
                "If the phantom hits a creature with a slam attack, that creature must succeed at a Will save (DC = 10 + 1/2 the phantom's Hit Dice + the phantom's Charisma modifier) or be shaken for 1d4 rounds. This is a mind-affecting fear effect.",
                new AddInitiatorAttackWithWeaponTrigger { name = "Phantom0085HorrifyingTrigger", OnlyHit = true, CheckWeaponCategory = true, Category = WeaponCategory.OtherNaturalWeapons, Action = PsychicAssets.Actions(effect) },
                Params(phantomClass, StatType.Charisma, "Phantom0085HorrifyingParams"),
                new SpellDescriptorComponent { name = "Phantom0085HorrifyingDescriptor", Descriptor = Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.MindAffecting | Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Fear | Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Shaken });

            // Increase Fear aura: shaken enemies save or become frightened;
            // successful saves grant 24h immunity; allies gain fear shelter at 17.
            var immuneBuff = f.Buff("FearIncreaseFearImmune", "", "", new Phantom0085NoOp { name = "Phantom0085FearImmuneMarker" });
            SetEnum(immuneBuff, "m_Flags", "HiddenInUi");
            immuneBuff.Stacking = StackingType.Stack;
            var frightenedAura = CopyBlueprint(frightened);
            frightenedAura.AssetGuid = Id("FearIncreaseFearAuraBuff");
            frightenedAura.name = Spiritualist0085Ids.AssetPrefix + "FearIncreaseFearAuraBuff";
            frightenedAura.Stacking = StackingType.Stack;
            f.Register(frightenedAura, "FearIncreaseFearAuraBuff");
            var shelterBuff = f.Buff("FearShelterAlliesEffect", "Shelter Allies",
                "Allies within the phantom's increase fear aura are immune to fear as long as they are within the aura.",
                new AddConditionImmunity { name = "Phantom0085ShelterShaken", Condition = UnitCondition.Shaken },
                new AddConditionImmunity { name = "Phantom0085ShelterFrightened", Condition = UnitCondition.Frightened },
                new BuffDescriptorImmunity { name = "Phantom0085ShelterDescriptor", Descriptor = Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Shaken | Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Frightened },
                new SpellImmunityToSpellDescriptor { name = "Phantom0085ShelterSpell", Descriptor = Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Shaken | Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Frightened });
            var shelterAllies = f.Feature("FearShelterAllies", "Shelter Allies", NameOf(shelterBuff));

            var fearDifficulty = f.Add(new BlueprintUnitProperty
            {
                ComponentsArray = new BlueprintComponent[] { new Phantom0085FearDifficulty { name = "Phantom0085FearDC" } }
            }, "FearIncreaseFearDifficulty", "", "");
            var difficulty = new ContextValue { ValueType = ContextValueType.CasterCustomProperty };
            Set(difficulty, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(fearDifficulty));
            var fearImmunity = Apply(immuneBuff, new ContextDurationValue { Rate = DurationRate.Days, BonusValue = 1 },
                "Phantom0085IncreaseFearImmuneApply");
            var amplifyFear = Apply(frightenedAura, new ContextDurationValue(), "Phantom0085IncreaseFearFrightenedApply", asChild: true);
            amplifyFear.Permanent = true;
            var enemySave = new ContextActionSavingThrow
            {
                name = "Phantom0085IncreaseFearSave", Type = SavingThrowType.Will, HasCustomDC = true, CustomDC = difficulty,
                Actions = PsychicAssets.Actions(new ContextActionConditionalSaved
                {
                    name = "Phantom0085IncreaseFearSaved",
                    Succeed = PsychicAssets.Actions(fearImmunity),
                    Failed = PsychicAssets.Actions(amplifyFear)
                })
            };
            var enemyEffect = Cond("Phantom0085IncreaseFearEnemyCheck",
                new Condition[]
                {
                    HasBuff(immuneBuff, "Phantom0085IncreaseFearNotImmune", not: true),
                    HasBuff(frightenedAura, "Phantom0085IncreaseFearNotFrightened", not: true)
                },
                PsychicAssets.Actions(enemySave), PsychicAssets.Actions());
            var auraAction = Cond("Phantom0085IncreaseFearAuraAction",
                new Condition[] { new ContextConditionIsEnemy { name = "Phantom0085IncreaseFearIsEnemy" } },
                PsychicAssets.Actions(Cond("Phantom0085IncreaseFearShakenCheck",
                    new Condition[] { new ContextConditionHasBuffWithDescriptor { name = "Phantom0085IncreaseFearShaken", SpellDescriptor = Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Shaken } },
                    PsychicAssets.Actions(enemyEffect),
                    PsychicAssets.Actions(RemoveFromCaster(frightenedAura, "Phantom0085IncreaseFearRemoveFrightened")))),
                PsychicAssets.Actions());
            var removeAction = RemoveFromCaster(frightenedAura, "Phantom0085IncreaseFearRemoveFrightened2");
            var increaseAura = f.Add(new BlueprintAbilityAreaEffect
            {
                Shape = AreaEffectShape.Cylinder, Size = 20.Feet(), AffectEnemies = true,
                ComponentsArray = new BlueprintComponent[]
                {
                    new AbilityAreaEffectRunAction { name = "Phantom0085IncreaseFearArea", UnitEnter = PsychicAssets.Actions(auraAction), UnitExit = PsychicAssets.Actions(removeAction), Round = PsychicAssets.Actions(auraAction) },
                    AreaBuff(shelterBuff, "Phantom0085IncreaseFearShelterCheck",
                        new ContextConditionIsAlly { name = "Phantom0085IncreaseFearIsAlly" },
                        HasFact(shelterAllies, "Phantom0085IncreaseFearHasShelter"))
                }
            }, "FearIncreaseFearArea", "Increase Fear", "");
            var increaseFear = AuraFeature(f, "FearIncreaseFear", "Increase Fear",
                "When the spiritualist reaches 7th level, as a swift action, the phantom can emit a 20-foot radius aura that amplifies the fear conditions of enemies within it if they fail their Will saves (DC = 10 + 1/2 the phantom's Hit Dice + the phantom's Charisma modifier). A shaken enemy becomes frightened. A creature that succeeds at its save is immune to this effect from the same phantom for 24 hours. This is a mind-affecting fear effect.",
                increaseAura, UnitCommand.CommandType.Swift);
            return new[] { horrifyingStrike, increaseFear, frightfulAttack, shelterAllies };
        }

        private static BlueprintFeature[] BuildZeal(FactoryShim f, BlueprintCharacterClass character, BlueprintCharacterClass phantomClass)
        {
            var slamType = f.Native<BlueprintWeaponType>(SlamTypeId);
            var critEdge = f.Feature("ZealRuthlessCombatant", "Ruthless Combatant",
                "The phantom threatens a critical hit with its slam attacks on a roll of 19-20. When the spiritualist reaches 11th level, the phantom's critical modifier with slam attacks increases to x3. This doesn't stack with Improved Critical or similar effects.",
                CritEdge(slamType, "Phantom0085RuthlessEdge"));
            var critMult = f.Feature("ZealRuthlessCombatant2", "", "",
                CritMult(slamType, "Phantom0085RuthlessMult"));
            critMult.HideInUI = true;
            critMult.HideInCharacterSheetAndLevelUp = true;
            critEdge.ComponentsArray = critEdge.ComponentsArray.Concat(new BlueprintComponent[]
            {
                ClassLevelGrant(phantomClass, 9, critMult, "Phantom0085RuthlessMultGrant")
            }).ToArray();

            var determinationBuff = f.Buff("ZealDeterminationAuraEffect", "Determination Aura",
                "Allies within the aura gain a +2 competence bonus on attack rolls and saving throws.",
                new AddStatBonus { name = "Phantom0085DeterminationAttack", Stat = StatType.AdditionalAttackBonus, Value = 2, Descriptor = ModifierDescriptor.Competence },
                new AddStatBonus { name = "Phantom0085DeterminationFort", Stat = StatType.SaveFortitude, Value = 2, Descriptor = ModifierDescriptor.Competence },
                new AddStatBonus { name = "Phantom0085DeterminationRef", Stat = StatType.SaveReflex, Value = 2, Descriptor = ModifierDescriptor.Competence },
                new AddStatBonus { name = "Phantom0085DeterminationWill", Stat = StatType.SaveWill, Value = 2, Descriptor = ModifierDescriptor.Competence });
            var determinationAura = f.Add(new BlueprintAbilityAreaEffect
            {
                Shape = AreaEffectShape.Cylinder, Size = 20.Feet(), AffectEnemies = true,
                ComponentsArray = new BlueprintComponent[]
                {
                    AreaBuff(determinationBuff, "Phantom0085DeterminationArea", new ContextConditionIsAlly { name = "Phantom0085DeterminationAlly" })
                }
            }, "ZealDeterminationAuraArea", "Determination Aura", "");
            var determinationAuraFeature = AuraFeature(f, "ZealDeterminationAura", "Determination Aura",
                "When the spiritualist reaches 7th level, as a swift action, the phantom can emit a 20-foot radius aura that grants its zeal to nearby allies. Allies within the aura gain a +2 competence bonus on attack rolls and saving throws.",
                determinationAura, UnitCommand.CommandType.Swift);

            var hasteResource = FixedResource(f, "ZealHasteResource", 3);
            var haste = ConvertToSpellLike(f, f.Native<BlueprintAbility>(HasteSpellId), "ZealHaste", phantomClass, StatType.Charisma, hasteResource);
            var zealHaste = f.Feature("ZealHasteFeature", "Haste",
                "Three times per day as a standard action, the phantom can use haste as a spell-like ability, using its Hit Dice as caster level.",
                Facts(haste), Grant(hasteResource));

            var resolveResource = FixedResource(f, "ZealZealsResolveResource", 3);
            var resolveAttackBuff = CopyBlueprint(f.Native<BlueprintBuff>(LuckDomainBuffId));
            resolveAttackBuff.AssetGuid = Id("ZealZealsResolveAttackBuff");
            resolveAttackBuff.name = Spiritualist0085Ids.AssetPrefix + "ZealZealsResolveAttackBuff";
            resolveAttackBuff.ComponentsArray = new BlueprintComponent[]
            {
                new Phantom0085Reroll { name = "Phantom0085RerollAttack", Rule = RuleType.AttackRoll, Resource = Ref<BlueprintAbilityResourceReference>(resolveResource) }
            };
            Set(resolveAttackBuff, "m_DisplayName", Text("ZealZealsResolveAttackBuff", "name", "Zeal's Resolve: Attack Roll"));
            f.Register(resolveAttackBuff, "ZealZealsResolveAttackBuff");
            var resolveSaveBuff = CopyBlueprint(resolveAttackBuff);
            resolveSaveBuff.AssetGuid = Id("ZealZealsResolveSaveBuff");
            resolveSaveBuff.name = Spiritualist0085Ids.AssetPrefix + "ZealZealsResolveSaveBuff";
            resolveSaveBuff.ComponentsArray = new BlueprintComponent[]
            {
                new Phantom0085Reroll { name = "Phantom0085RerollSave", Rule = RuleType.SavingThrow, Resource = Ref<BlueprintAbilityResourceReference>(resolveResource) }
            };
            Set(resolveSaveBuff, "m_DisplayName", Text("ZealZealsResolveSaveBuff", "name", "Zeal's Resolve: Saving Throw"));
            f.Register(resolveSaveBuff, "ZealZealsResolveSaveBuff");
            var resolveAttackToggle = f.Add(new BlueprintActivatableAbility { ActivationType = AbilityActivationType.Immediately, DeactivateImmediately = true },
                "ZealZealsResolveAttack", "Zeal's Resolve: Attack Roll",
                "Three times per day as a free action, when the phantom misses with an attack roll, it can reroll the failed attack. It must take the new result, even if worse.");
            Set(resolveAttackToggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);
            Set(resolveAttackToggle, "m_Buff", Ref<BlueprintBuffReference>(resolveAttackBuff));
            var resolveSaveToggle = f.Add(new BlueprintActivatableAbility { ActivationType = AbilityActivationType.Immediately, DeactivateImmediately = true },
                "ZealZealsResolveSave", "Zeal's Resolve: Saving Throw",
                "Three times per day as a free action, when the phantom fails a saving throw, it can reroll the failed save. It must take the new result, even if worse.");
            Set(resolveSaveToggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);
            Set(resolveSaveToggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);
            Set(resolveSaveToggle, "m_Buff", Ref<BlueprintBuffReference>(resolveSaveBuff));
            var zealsResolve = f.Feature("ZealZealsResolve", "Zeal's Resolve", NameOf(resolveAttackToggle),
                Facts(resolveAttackToggle, resolveSaveToggle), Grant(resolveResource));
            return new[] { critEdge, determinationAuraFeature, zealHaste, zealsResolve };
        }

        private static BlueprintAbility BuildAidAnother(FactoryShim f)
        {
            const string description = "Help an adjacent ally in melee. The ally gains +4 on the chosen roll, once, for up to 1 round.";
            var attack = f.Buff("KindnessAidAttackEffect", "Aid Another: Attack", description,
                new Phantom0085AidAttackBonus { name = "Phantom0085AidAttackBonus" },
                new AddInitiatorAttackWithWeaponTrigger
                {
                    name = "Phantom0085AidAttackConsume", CheckWeaponRangeType = true, RangeType = WeaponRangeType.Melee,
                    OnlyHit = false, ActionsOnInitiator = true, Action = PsychicAssets.Actions(new ContextActionRemoveSelf())
                });
            var defense = f.Buff("KindnessAidDefenseEffect", "Aid Another: Defense", description,
                new ACBonusAgainstAttacks
                {
                    name = "Phantom0085AidDefenseBonus", AgainstMeleeOnly = true, ArmorClassBonus = 4,
                    Descriptor = ModifierDescriptor.UntypedStackable
                },
                new AddTargetAttackWithWeaponTrigger
                {
                    name = "Phantom0085AidDefenseConsume", OnlyMelee = true, OnlyHit = false, WaitForAttackResolve = true,
                    ActionsOnAttacker = PsychicAssets.Actions(), ActionOnSelf = PsychicAssets.Actions(new ContextActionRemoveSelf())
                });
            attack.Stacking = defense.Stacking = StackingType.Stack;

            BlueprintAbility Variant(string token, BlueprintBuff buff)
            {
                var remove = new ContextActionRemoveBuff { name = token + "RemoveOwnAid", OnlyFromCaster = true };
                Set(remove, "m_Buff", Ref<BlueprintBuffReference>(buff));
                var apply = Apply(buff, new ContextDurationValue { Rate = DurationRate.Rounds, BonusValue = new ContextValue { Value = 1 } }, token + "Apply");
                var ability = f.Ability(token, NameOf(buff), description, AbilityType.Special, UnitCommand.CommandType.Standard,
                    AbilityRange.Touch, PsychicAssets.Run(remove, apply));
                ability.CanTargetSelf = false;
                ability.CanTargetEnemies = false;
                return ability;
            }

            var choices = new[] { Variant("KindnessAidAttack", attack), Variant("KindnessAidDefense", defense) };
            var variants = new AbilityVariants { name = "Phantom0085AidVariants" };
            Set(variants, "m_Variants", choices.Select(Ref<BlueprintAbilityReference>).ToArray());
            var aid = f.Ability("KindnessAidAnother", "Aid Another", description, AbilityType.Special,
                UnitCommand.CommandType.Standard, AbilityRange.Personal, variants);
            foreach (var choice in choices)
                Set(choice, "m_Parent", Ref<BlueprintAbilityReference>(aid));
            return aid;
        }

        private static BlueprintFeature[] BuildKindness(FactoryShim f, BlueprintCharacterClass character, BlueprintCharacterClass phantomClass)
        {
            // Opening Strike: standard attack; on hit, one threatening ally gains
            // a swift attack against the target. Exceptional Aid adds CHA damage.
            var targetBuff = f.Buff("KindnessOpeningStrikeTarget", "Opening Strike Target",
                "As a standard action, the phantom can make a melee attack against a foe. If this attack hits, the spiritualist designates one ally threatening that enemy. This ally can make a single attack at his full base attack bonus against that enemy as a swift action.");
            var allyBuff = f.Buff("KindnessOpeningStrikeAlly", "Opening Strike", DescOf(targetBuff));
            targetBuff.Stacking = allyBuff.Stacking = StackingType.Stack;
            var allyAbility = f.Ability("KindnessOpeningStrikeAllyAbility", "Opening Strike", DescOf(targetBuff),
                AbilityType.Special, UnitCommand.CommandType.Swift, AbilityRange.Weapon,
                new Phantom0085OpeningStrikeTarget
                {
                    name = "Phantom0085OpeningAllyTarget", TargetBuff = Ref<BlueprintBuffReference>(targetBuff),
                    AllyBuff = Ref<BlueprintBuffReference>(allyBuff)
                },
                PsychicAssets.Run(new Phantom0085OpeningStrike
                {
                    name = "Phantom0085UseOpeningStrike", TargetBuff = Ref<BlueprintBuffReference>(targetBuff),
                    AllyBuff = Ref<BlueprintBuffReference>(allyBuff), UseOpening = true
                }));
            allyAbility.NeedEquipWeapons = true;
            allyAbility.CanTargetFriends = false;
            allyBuff.ComponentsArray = new BlueprintComponent[] { Facts(allyAbility) };
            var openingAbility = f.Ability("KindnessOpeningStrikeAbility", "Opening Strike", DescOf(targetBuff),
                AbilityType.Special, UnitCommand.CommandType.Standard, AbilityRange.Weapon,
                PsychicAssets.Run(new Phantom0085OpeningStrike { name = "Phantom0085OpeningStrike", TargetBuff = Ref<BlueprintBuffReference>(targetBuff), AllyBuff = Ref<BlueprintBuffReference>(allyBuff) }));
            openingAbility.NeedEquipWeapons = true;
            openingAbility.CanTargetFriends = false;
            var openingStrike = f.Feature("KindnessOpeningStrike", "Opening Strike", NameOf(openingAbility), Facts(openingAbility));

            // Etheric Healing: lay on hands (self/master and others), phantom HD.
            var healingResource = f.Add(new BlueprintAbilityResource(), "KindnessEthericHealingResource", "", "");
            var amount = Get(healingResource, "m_MaxAmount");
            Set(amount, "BaseValue", 0);
            Set(amount, "IncreasedByLevelStartPlusDivStep", true);
            Set(amount, "StartingLevel", 2); Set(amount, "StartingIncrease", 1); Set(amount, "LevelStep", 2); Set(amount, "PerStepIncrease", 1);
            Set(amount, "IncreasedByStat", true); Set(amount, "ResourceBonusStat", StatType.Charisma);
            Set(amount, "m_ClassDiv", new[] { Ref<BlueprintCharacterClassReference>(phantomClass) });
            Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            Set(healingResource, "m_MaxAmount", amount);
            var laySelf = CopyBlueprint(f.Native<BlueprintAbility>(LayOnHandsSelfId));
            laySelf.AssetGuid = Id("KindnessEthericHealingSelf");
            laySelf.name = Spiritualist0085Ids.AssetPrefix + "KindnessEthericHealingSelf";
            laySelf.Range = AbilityRange.Touch;
            laySelf.ComponentsArray = laySelf.ComponentsArray
                .Where(c => !(c is AbilityCasterAlignment))
                .Select(c => c is ContextRankConfig r ? Rebind(r, phantomClass) : c is AbilityResourceLogic l ? Rebind(l, healingResource) : c)
                .Concat(new BlueprintComponent[] { new Phantom0085TargetSelfOrMaster { name = "Phantom0085HealSelfOrMaster" } }).ToArray();
            Set(laySelf, "m_DisplayName", Text("KindnessEthericHealingSelf", "name", "Etheric Healing: Self or Master"));
            f.Register(laySelf, "KindnessEthericHealingSelf");
            var layOthers = CopyBlueprint(f.Native<BlueprintAbility>(LayOnHandsOthersId));
            layOthers.AssetGuid = Id("KindnessEthericHealing");
            layOthers.name = Spiritualist0085Ids.AssetPrefix + "KindnessEthericHealing";
            layOthers.ComponentsArray = layOthers.ComponentsArray
                .Where(c => !(c is AbilityCasterAlignment))
                .Select(c => c is ContextRankConfig r ? Rebind(r, phantomClass) : c is AbilityResourceLogic l ? Rebind(l, healingResource) : c)
                .ToArray();
            Set(layOthers, "m_DisplayName", Text("KindnessEthericHealing", "name", "Etheric Healing"));
            f.Register(layOthers, "KindnessEthericHealing");
            var ethericHealing = f.Feature("KindnessEthericHealingFeature", "Etheric Healing",
                "When the spiritualist reaches 7th level, the phantom gains the lay on hands ability with an effective paladin level equal to its Hit Dice. The phantom can use this ability on the spiritualist as a swift action.",
                Facts(layOthers, laySelf), Grant(healingResource));

            var expandedAid = f.Feature("KindnessExpandedAid", "Expanded Aid",
                "When the spiritualist reaches 12th level, the phantom can use the aid another action as a move action. Additionally, the spiritualist selects four mercies available to a 12th-level paladin that the phantom adds to its lay on hands ability.",
                new Phantom0085AidAnotherActionType { name = "Phantom0085ExpandedAid", ActionType = UnitCommand.CommandType.Move });
            var exceptionalAid = f.Feature("KindnessExceptionalAid", "Exceptional Aid",
                "When the spiritualist reaches 17th level, the phantom can use the aid another action as a swift action. Additionally, when designating an ally for an opening strike, that ally gains a bonus equal to the phantom's Charisma modifier on damage rolls for the attack granted to him.",
                new Phantom0085AidAnotherActionType { name = "Phantom0085ExceptionalAid", ActionType = UnitCommand.CommandType.Swift });
            return new[] { openingStrike, ethericHealing, expandedAid, exceptionalAid };
        }

        internal static BlueprintFeatureSelection BuildKindnessMercySelection(FactoryShim f)
        {
            var mercySelection = f.Native<BlueprintFeatureSelection>(MercySelectionId);
            var phantomMercy = f.Add(new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true }, "KindnessPhantomMercySelection", "Phantom Mercy",
                "The phantom selects mercies available to a 12th-level paladin and adds them to its lay on hands ability.");
            var choices = ((BlueprintFeatureReference[])Get(mercySelection, "m_AllFeatures"))
                .Select(r => f.Cache.Load(r.Guid) as BlueprintFeature)
                .Where(mercy => mercy != null && (mercy.GetComponent<PrerequisiteClassLevel>()?.Level ?? 0) <= 12)
                .ToDictionary(mercy => mercy.AssetGuid, mercy => GrantToPet(f, "KindnessPhantomMercy" + mercy.name, mercy));
            foreach (var pair in choices)
            {
                var nativeMercy = f.Cache.Load(pair.Key) as BlueprintFeature;
                var prerequisites = nativeMercy.GetComponents<PrerequisiteFeature>().Select(p =>
                {
                    var copy = (PrerequisiteFeature)CopyData(p);
                    var nativeReference = (BlueprintFeatureReference)Get(p, "m_Feature");
                    if (nativeReference != null && choices.TryGetValue(nativeReference.Guid, out var prerequisite))
                        Set(copy, "m_Feature", Ref<BlueprintFeatureReference>(prerequisite));
                    return copy;
                });
                pair.Value.IsClassFeature = true;
                pair.Value.ComponentsArray = pair.Value.ComponentsArray.Concat(prerequisites).ToArray();
            }
            var options = choices.Values.Select(Ref<BlueprintFeatureReference>).ToArray();
            Set(phantomMercy, "m_Features", options);
            Set(phantomMercy, "m_AllFeatures", options);
            return phantomMercy;
        }

        private static ContextRankConfig Rebind(ContextRankConfig source, BlueprintCharacterClass cls)
        {
            var clone = (ContextRankConfig)CopyData(source);
            Set(clone, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(cls) });
            return clone;
        }

        private static AbilityResourceLogic Rebind(AbilityResourceLogic source, BlueprintAbilityResource resource)
        {
            var clone = (AbilityResourceLogic)CopyData(source);
            Set(clone, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
            return clone;
        }

        internal static BlueprintFeature GrantToPet(FactoryShim f, string token, BlueprintFeature feature)
        {
            var grant = new Phantom0085BoundGrant { name = "Phantom0085Grant" + token, Feature = Ref<BlueprintFeatureReference>(feature) };
            return f.Feature(token, NameOf(feature), DescOf(feature), grant);
        }

        // ---- Master ability builders (runtime lives in PhantomRuntime0085) ----
        internal static void BuildConsciousnessGrants(FactoryShim f, BlueprintCharacterClass character, BlueprintCharacterClass phantomClass,
            BlueprintFeatureSelection selection, List<BlueprintProgression> emotions, BlueprintFeature shared, BlueprintFeature fused)
        {
            var skillFocus = f.Native<BlueprintFeatureSelection>(SkillFocusSelectionId);
            var sharedSave = f.Buff("SharedConsciousnessSave", "Shared Consciousness", "",
                new SavingThrowBonusAgainstDescriptor { name = "Phantom0085SharedSave", SpellDescriptor = Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.MindAffecting, Bonus = new ContextValue { Value = 4 }, ModifierDescriptor = ModifierDescriptor.UntypedStackable });
            SetEnum(sharedSave, "m_Flags", "HiddenInUi");
            sharedSave.IsClassFeature = true;
            var fusedSave = f.Buff("FusedConsciousnessSave", "Fused Consciousness", "",
                new SavingThrowBonusAgainstDescriptor { name = "Phantom0085FusedSave", SpellDescriptor = Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.MindAffecting, Bonus = new ContextValue { ValueType = ContextValueType.Rank }, ModifierDescriptor = ModifierDescriptor.UntypedStackable },
                Rank(character, "Custom", 11, 4, 20, 8));
            SetEnum(fusedSave, "m_Flags", "HiddenInUi");
            fusedSave.IsClassFeature = true;
            foreach (var row in Emotions)
            {
                var emotion = emotions.First(e => e.name == Spiritualist0085Ids.AssetPrefix + row.Name + "Emotion");
                foreach (var skill in row.Skills)
                {
                    var focus = ((BlueprintFeatureReference[])Get(skillFocus, "m_AllFeatures"))
                        .Select(r => f.Cache.Load(r.Guid) as BlueprintFeature)
                        .FirstOrDefault(x => x?.GetComponent<AddContextStatBonus>()?.Stat == skill);
                    if (focus == null) continue;
                    var sharedGrant = new Phantom0085ConsciousnessGrant { name = "Phantom0085SharedFocus" + row.Name + skill, Emotion = Ref<BlueprintProgressionReference>(emotion), Focus = Ref<BlueprintFeatureReference>(focus), Fused = Ref<BlueprintFeatureReference>(fused), SaveBuff = Ref<BlueprintBuffReference>(sharedSave), Permanent = false };
                    var fusedGrant = new Phantom0085ConsciousnessGrant { name = "Phantom0085FusedFocus" + row.Name + skill, Emotion = Ref<BlueprintProgressionReference>(emotion), Focus = Ref<BlueprintFeatureReference>(focus), Fused = Ref<BlueprintFeatureReference>(fused), SaveBuff = Ref<BlueprintBuffReference>(fusedSave), Permanent = true };
                    shared.ComponentsArray = shared.ComponentsArray.Concat(new BlueprintComponent[] { sharedGrant }).ToArray();
                    fused.ComponentsArray = fused.ComponentsArray.Concat(new BlueprintComponent[] { fusedGrant }).ToArray();
                }
            }
        }

        internal static BlueprintFeature BuildTetherToggle(FactoryShim f, BlueprintCharacterClass character, string token, string name, string description, bool spiritualBond)
        {
            var buff = f.Buff(token + "Effect", name, description,
                new Phantom0085DamageTransfer { name = "Phantom0085Transfer" + token, SpiritualBond = spiritualBond });
            var toggle = f.Add(new BlueprintActivatableAbility
            {
                ActivationType = AbilityActivationType.Immediately,
                DeactivateImmediately = true,
                Group = ActivatableAbilityGroup.None,
                WeightInGroup = 1
            }, token, name, description);
            Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(buff));
            toggle.ComponentsArray = new BlueprintComponent[]
            {
                new Phantom0085DamageLinkRestriction { name = "Phantom0085" + token + "RequiresManifestedPet" }
            };
            return f.Feature(token + "Feature", name, description, Facts(toggle));
        }

        internal static BlueprintFeature BuildBondedManifestation(FactoryShim f, BlueprintCharacterClass character, BlueprintCharacterClass phantomClass,
            BlueprintFeatureSelection selection, out BlueprintFeature dualBond)
        {
            var shieldBuff = f.Buff("BondedManifestationEffect", "Bonded Manifestation",
                "The spiritualist gains an ectoplasmic shield: +4 shield bonus to AC (also against incorporeal touch attacks), increasing to +6 at 13th level. At 8th level she sprouts ectoplasmic tendrils; at 18th level she can attack all creatures in reach with them, rolling twice and taking the better result.",
                new AddContextStatBonus { name = "Phantom0085BondedShield", Stat = StatType.AC, Descriptor = ModifierDescriptor.Shield, Value = new ContextValue { ValueType = ContextValueType.Rank } },
                Rank(character, "Custom", 12, 4, 20, 6),
                new Phantom0085ShieldToIncorporealTouch { name = "Phantom0085BondedIncorporealTouch" });
            var resource = f.Add(new BlueprintAbilityResource(), "BondedManifestationResource", "", "");
            var amount = Get(resource, "m_MaxAmount");
            Set(amount, "BaseValue", 3);
            Set(amount, "IncreasedByLevel", true); Set(amount, "LevelIncrease", 1);
            Set(amount, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(character) });
            Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            Set(resource, "m_MaxAmount", amount);
            var toggle = f.Add(new BlueprintActivatableAbility
            {
                ActivationType = AbilityActivationType.WithUnitCommand,
                DeactivateIfOwnerDisabled = true,
                DeactivateImmediately = true,
                ComponentsArray = new BlueprintComponent[]
                {
                    BondedCost(resource),
                    new Phantom0085BondedRestriction { name = "Phantom0085BondedRestriction" }
                }
            }, "BondedManifestation", "Bonded Manifestation", DescOf(shieldBuff));
            Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Swift);
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(shieldBuff));
            var tendril8 = BuildTendril(f, character, shieldBuff, "BondedTendrils8", "Tendrils Attack",
                "At 8th level, the spiritualist sprouts ectoplasmic tendrils. As a swift action, she can attack a creature within her melee reach with them, using the damage dice of her manifested phantom.",
                UnitCommand.CommandType.Swift, fullRound: false);
            var tendril18 = BuildTendril(f, character, shieldBuff, "BondedTendrils18", "Tendrils Attack II",
                "At 18th level, as a full-round action, the spiritualist can attack all creatures within her melee reach with her tendrils, rolling the attack roll twice and taking the better result.",
                UnitCommand.CommandType.Standard, fullRound: true);
            var bonded = f.Feature("BondedManifestationFeature", "Bonded Manifestation", DescOf(shieldBuff),
                Facts(toggle), Grant(resource),
                ClassLevelGrant(character, 8, tendril8, "Phantom0085Tendrils8Grant"),
                ClassLevelGrant(character, 18, tendril18, "Phantom0085Tendrils18Grant"));

            var dualResource = new IncreaseResourceAmountBySharedValue { name = "Phantom0085DualBond", Value = new ContextValue { ValueType = ContextValueType.Rank } };
            Set(dualResource, "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
            dualBond = f.Feature("DualBond", "Dual Bond",
                "At 17th level, the spiritualist can use her bonded manifestation ability a number of rounds per day equal to 3 + twice her spiritualist level.",
                dualResource, Rank(character, "AsIs"));
            dualBond.ReapplyOnLevelUp = true;
            return bonded;
        }

        private static BlueprintFeature BuildTendril(FactoryShim f, BlueprintCharacterClass character, BlueprintBuff shieldBuff,
            string token, string name, string description, UnitCommand.CommandType action, bool fullRound)
        {
            var diceBuff = f.Buff(token + "Dice", "", "",
                new Phantom0085TendrilDice { name = "Phantom0085TendrilDice" + token },
                SlamRank(character));
            SetEnum(diceBuff, "m_Flags", "HiddenInUi");
            BlueprintBuff rerollBuff = null;
            if (fullRound)
            {
                rerollBuff = f.Buff(token + "Reroll", "", "",
                    new ModifyD20 { name = "Phantom0085TendrilReroll", Rule = RuleType.AttackRoll, RollsAmount = 1, TakeBest = true });
                SetEnum(rerollBuff, "m_Flags", "HiddenInUi");
            }
            var slam = f.Native<BlueprintItemWeapon>(Spiritualist0085Ids.SlamWeaponId);
            var requiresShield = new AbilityCasterHasFacts { name = "Phantom0085" + token + "RequiresBonded" };
            Set(requiresShield, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(shieldBuff) });
            var ability = f.Ability(token + "Ability", name, description, AbilityType.Supernatural, action,
                fullRound ? AbilityRange.Personal : AbilityRange.Touch, requiresShield,
                PsychicAssets.Run(new Phantom0085TendrilAttack
                {
                    name = "Phantom0085Tendril" + token,
                    DiceBuff = Ref<BlueprintBuffReference>(diceBuff),
                    RerollBuff = rerollBuff != null ? Ref<BlueprintBuffReference>(rerollBuff) : null,
                    Slam = Ref<BlueprintItemWeaponReference>(slam)
                }));
            ability.CanTargetFriends = false;
            ability.CanTargetEnemies = !fullRound;
            if (fullRound)
            {
                var area = new AbilityTargetsAround { name = "Phantom0085TendrilsEnemies" };
                Set(area, "m_Radius", 7.Feet());
                Set(area, "m_TargetType", TargetType.Enemy);
                Set(area, "m_Condition", new ConditionsChecker { Conditions = Array.Empty<Condition>() });
                ability.ComponentsArray = ability.ComponentsArray.Concat(new BlueprintComponent[] { area }).ToArray();
                Set(ability, "m_IsFullRoundAction", true);
            }
            return f.Feature(token, name, description, Facts(ability));
        }

        internal static BlueprintFeature BuildInference(FactoryShim f, BlueprintCharacterClass character, BlueprintCharacterClass phantomClass, bool greater)
        {
            var token = greater ? "GreaterSpiritualInference" : "SpiritualInference";
            var ownerBuff = f.Buff(token + "Owner", "", "",
                new Phantom0085InferenceBonus { name = "Phantom0085InferenceOwnerBonus", Greater = greater, ForMaster = true, Bonus = greater ? 4 : 2 });
            var allyBuff = f.Buff(token + "Ally", "", "",
                new Phantom0085InferenceBonus { name = "Phantom0085InferenceAllyBonus", Greater = greater, Bonus = 2 });
            var effects = new List<BlueprintComponent>
            {
                AreaBuff(ownerBuff, "Phantom0085InferenceOwnerArea",
                    new Phantom0085InferenceCondition { name = "Phantom0085InferenceOwnerEligible", Greater = greater, ForMaster = true })
            };
            if (greater)
                effects.Add(AreaBuff(allyBuff, "Phantom0085InferenceAllyArea",
                    new Phantom0085InferenceCondition { name = "Phantom0085InferenceAllyEligible", Greater = true }));
            var aura = f.Add(new BlueprintAbilityAreaEffect
            {
                Shape = AreaEffectShape.Cylinder, Size = 10.Feet(), AffectEnemies = false,
                ComponentsArray = effects.ToArray()
            }, token + "Area", greater ? "Greater Spiritual Inference" : "Spiritual Inference", "");
            var title = greater ? "Greater Spiritual Inference" : "Spiritual Inference";
            var description = greater
                ? "At 12th level, whenever allies are within the phantom's reach, each ally gains a +2 shield bonus to AC and a +2 circumstance bonus on saving throws. For the spiritualist, these bonuses increase to +4. The phantom itself does not benefit."
                : "At 4th level, whenever the spiritualist is within the reach of her manifested phantom, she gains a +2 shield bonus to AC and a +2 circumstance bonus on saving throws. She doesn't gain these bonuses when the phantom is grappled, helpless or unconscious.";
            var host = AuraHost(f, token, title, description, aura);
            var spawn = new AuraFeatureComponent { name = "Phantom0085InferenceHost" + token };
            Set(spawn, "m_Buff", Ref<BlueprintBuffReference>(host));
            var petFeature = f.Feature(token + "Pet", title, description, spawn);
            if (greater) return GrantToPet(f, token, petFeature);
            var carrier = GrantToPet(f, token + "Grant", petFeature);
            var gate = ClassLevelGrant(character, 12, carrier, "Phantom0085InferenceBeforeGreater");
            gate.BeforeThisLevel = true;
            return f.Feature(token, "Spiritual Inference", DescOf(petFeature), gate);
        }

        internal static BlueprintFeature BuildRecall(FactoryShim f, BlueprintCharacterClass character)
        {
            var resource = f.Add(new BlueprintAbilityResource(), "PhantomRecallResource", "", "");
            var amount = Get(resource, "m_MaxAmount");
            Set(amount, "BaseValue", 0);
            Set(amount, "IncreasedByLevelStartPlusDivStep", true);
            Set(amount, "StartingLevel", 6); Set(amount, "StartingIncrease", 1); Set(amount, "LevelStep", 4); Set(amount, "PerStepIncrease", 1);
            Set(amount, "m_ClassDiv", new[] { Ref<BlueprintCharacterClassReference>(character) });
            Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            Set(resource, "m_MaxAmount", amount);
            var ability = f.Ability("PhantomRecallAbility", "Phantom Recall",
                "At 6th level, as a swift action, a spiritualist can call her manifested phantom to her side. This functions as dimension door, using the spiritualist's caster level. The phantom appears adjacent to the spiritualist (or as close as possible). Once per day at 6th level, plus one additional time per day for every four levels beyond 6th.",
                AbilityType.Supernatural, UnitCommand.CommandType.Swift, AbilityRange.Personal,
                PsychicAssets.Run(new Phantom0085Recall { name = "Phantom0085Recall" }));
            ability.CanTargetSelf = true;
            return f.Feature("PhantomRecall", "Phantom Recall", NameOf(ability), Facts(ability), Grant(resource));
        }

        internal static BlueprintFeature BuildPotentPhantom(FactoryShim f, BlueprintCharacterClass character, BlueprintCharacterClass phantomClass, List<BlueprintProgression> emotions)
        {
            var options = new List<BlueprintFeature>();
            foreach (var row in Emotions)
            {
                var emotion = emotions.First(e => e.name == Spiritualist0085Ids.AssetPrefix + row.Name + "Emotion");
                // The four capability wrappers of the secondary emotion, applied
                // to the same phantom; no second AddPet, same emotion forbidden.
                var token = row.Name + "PotentPhantom";
                var name = "Potent Phantom: " + row.Name;
                var grant = new Phantom0085PotentGrant { name = "Phantom0085Potent" + row.Name, Emotion = Ref<BlueprintProgressionReference>(emotion) };
                var prerequisite = NoFeature(emotion, "Phantom0085PotentNotPrimary" + row.Name);
                BlueprintFeature capstone;
                if (row.Name == "Kindness")
                {
                    var mercy = f.Find<BlueprintFeatureSelection>("KindnessPhantomMercySelection");
                    var withMercies = f.Progression(token, name, row.Description,
                        new[] { Entry(20, mercy, mercy, mercy, mercy) }, Array.Empty<UIGroup>());
                    Set(withMercies, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(character) } });
                    withMercies.ComponentsArray = new BlueprintComponent[] { grant, prerequisite };
                    withMercies.IsClassFeature = true;
                    capstone = withMercies;
                }
                else
                {
                    capstone = f.Feature(token, name, row.Description, grant, prerequisite);
                }
                options.Add(capstone);
            }
            var selection = f.Add(new BlueprintFeatureSelection { Ranks = 1 }, "PotentPhantom", "Potent Phantom",
                "At 20th level, the spiritualist's phantom gains a second emotional focus. This does not change the phantom's saving throws, but the phantom otherwise grants all the skills and powers of the focus.");
            Set(selection, "m_Features", options.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(selection, "m_AllFeatures", options.Select(Ref<BlueprintFeatureReference>).ToArray());
            return selection;
        }

        internal static BlueprintFeature BuildConfine(FactoryShim f, BlueprintCharacterClass character)
        {
            var ability = f.Ability("ConfinePhantom", "Confine Phantom", "Confine your phantom in your consciousness.",
                AbilityType.Supernatural, UnitCommand.CommandType.Standard, AbilityRange.Personal,
                PsychicAssets.Run(new Phantom0085Confine { name = "Phantom0085Confine" }));
            ability.CanTargetSelf = true;
            return f.Feature("ConfinePhantomFeature", "Confine Phantom", DescOf(ability), Facts(ability));
        }

        internal static BlueprintFeature BuildManifest(FactoryShim f, BlueprintCharacterClass character)
        {
            var ability = f.Ability("ManifestPhantom", "Manifest Phantom", "Fully manifest your phantom.",
                AbilityType.Supernatural, UnitCommand.CommandType.Standard, AbilityRange.Personal,
                PsychicAssets.Run(new Phantom0085Manifest { name = "Phantom0085Manifest" }));
            ability.CanTargetSelf = true;
            Set(ability, "m_IsFullRoundAction", true);
            return f.Feature("ManifestPhantomFeature", "Manifest Phantom", DescOf(ability), Facts(ability));
        }

        // Single private factory for the whole 0085 graph: own salt, own asset
        // prefix, atomic publication through Spiritualist0085.Register.
        internal sealed class FactoryShim
        {
            internal readonly BlueprintsCache Cache;
            internal readonly Dictionary<string, BlueprintScriptableObject> Assets = new Dictionary<string, BlueprintScriptableObject>();
            internal FactoryShim(BlueprintsCache cache) { Cache = cache; }
            internal T Native<T>(string guid) where T : SimpleBlueprint => Cache.Load(BlueprintGuid.Parse(guid)) as T;
            internal bool Has(string token) => Assets.ContainsKey(token);
            internal T Find<T>(string token) where T : BlueprintScriptableObject => (T)Assets[token];
            internal T Add<T>(T obj, string token, string name, string description) where T : BlueprintScriptableObject
            {
                obj.AssetGuid = Id(token);
                obj.name = Spiritualist0085Ids.AssetPrefix + token;
                if (obj is BlueprintUnitFact fact)
                {
                    Set(fact, "m_DisplayName", Text(token, "name", name));
                    Set(fact, "m_Description", Text(token, "description", description));
                }
                Register(obj, token);
                return obj;
            }
            internal void Register(BlueprintScriptableObject obj, string token)
            {
                if (Assets.ContainsKey(token)) throw new InvalidOperationException("0085 duplicate token: " + token);
                Assets.Add(token, obj);
            }
            internal void Enable() { foreach (var bp in Assets.Values) bp.OnEnable(); }
            internal BlueprintFeature Feature(string token, string name, string description, params BlueprintComponent[] components)
                => Add(new BlueprintFeature { Ranks = 1, ComponentsArray = components }, token, name, description);
            internal BlueprintBuff Buff(string token, string name, string description, params BlueprintComponent[] components)
                => Add(new BlueprintBuff { Stacking = StackingType.Replace, ComponentsArray = components }, token, name, description);
            internal BlueprintAbility Ability(string token, string name, string description, AbilityType type,
                UnitCommand.CommandType action, AbilityRange range, params BlueprintComponent[] components)
                => Add(new BlueprintAbility
                {
                    Type = type, ActionType = action, Range = range,
                    CanTargetSelf = range == AbilityRange.Personal,
                    CanTargetFriends = range != AbilityRange.Personal,
                    CanTargetEnemies = range != AbilityRange.Personal,
                    EffectOnEnemy = AbilityEffectOnUnit.Harmful, EffectOnAlly = AbilityEffectOnUnit.Helpful,
                    ComponentsArray = components
                }, token, name, description);
            internal BlueprintProgression Progression(string token, string name, string description, LevelEntry[] entries, UIGroup[] groups)
                => Add(new BlueprintProgression { LevelEntries = entries, UIGroups = groups, Ranks = 1 }, token, name, description);
        }
    }
}
