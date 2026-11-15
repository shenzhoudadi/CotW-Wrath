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
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.Spiritualist0085Ids;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Phantom0085
    {
        private static BlueprintGuid Id(string token) => Spiritualist0085Ids.Id(token);
        private static Kingmaker.Localization.LocalizedString Text(string token, string suffix, string value) =>
            PsychicAssets.Text("Spiritualist0085" + token, suffix, value);

        internal static string NameOf(BlueprintUnitFact fact) => PsychicAssets.PeekText(fact) ?? fact?.name ?? "";
        internal static string DescOf(BlueprintUnitFact fact) => PsychicAssets.PeekText(fact, true) ?? "";

        // ---- Phantom class (data from Fighter/Animal donors, hidden) ----
        internal static BlueprintCharacterClass BuildPhantomClass(FactoryShim f, BlueprintCharacterClass fighter,
            BlueprintCharacterClass animal, BlueprintStatProgression savesHigh)
        {
            var cls = f.Add(new BlueprintCharacterClass
            {
                SkillPoints = 3,
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
            Set(cls, "m_FortitudeSave", Ref<BlueprintStatProgressionReference>(savesHigh));
            Set(cls, "m_ReflexSave", Ref<BlueprintStatProgressionReference>(savesHigh));
            Set(cls, "m_WillSave", Ref<BlueprintStatProgressionReference>(savesHigh));
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
            var attrGrowth = Native<BlueprintFeature>(AttributeGrowthId);
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
                Rank(phantomClass, "Custom", 3, 0, 6, 1, 9, 2, 12, 3, 20, 4));
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
                new Phantom0085SkillRanks { name = "Phantom0085SkillRanks" + row.Name, Skills = row.Skills },
                Rank(phantomClass, "OnePlusDivStep"));
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
                    Rank(phantomClass, "Custom", 3, 0, 6, 1, 9, 2, 12, 3, 20, 4));
                var strCha = BuildStrChaGrowth(f);
                add[0] = Entry(1, skillFeature, baseTrait, largeSlam);
                foreach (var level in new[] { 2, 4, 6, 8, 9, 12, 13, 15 }) add.Add(Entry(level, strCha));
                var dexCha = f.Find<BlueprintFeature>("PhantomStrChaGrowth");
                remove.Add(Entry(1, f.Find<BlueprintFeature>("PhantomSlamDamage")));
                foreach (var level in new[] { 2, 4, 6, 8, 9, 12, 13, 15 }) remove.Add(Entry(level, dexCha));
            }
            archetype.AddFeatures = add.ToArray();
            archetype.RemoveFeatures = remove.ToArray();
            return archetype;
        }

        private static BlueprintFeature BuildStrChaGrowth(FactoryShim f)
        {
            if (f.Has("PhantomStrChaGrowth")) return f.Find<BlueprintFeature>("PhantomStrChaGrowth");
            return f.Feature("PhantomStrChaGrowth", "Phantom Prowess", "Anger phantom receives +1 Strength and +1 Charisma.",
                new AddStatBonus { name = "Phantom0085StrChaStr", Stat = StatType.Strength, Value = 1 },
                new AddStatBonus { name = "Phantom0085StrChaCha", Stat = StatType.Charisma, Value = 1 });
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
                        new Phantom0085Benevolent { name = "Phantom0085Benevolent" });
                default:
                    return null;
            }
        }

        internal static void AttachArchetype(FactoryShim f, BlueprintUnit unit, BlueprintArchetype archetype)
        {
            var add = new AddClassLevels
            {
                name = "Phantom0085AddClassLevels",
                DoNotApplyAutomatically = true, Levels = 0,
                Skills = new[] { StatType.SkillAthletics, StatType.SkillMobility, StatType.SkillStealth, StatType.SkillPersuasion, StatType.SkillKnowledgeArcana },
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

        private static ContextActionRemoveBuff Remove(BlueprintBuff buff, string name)
        {
            var action = new ContextActionRemoveBuff { name = name };
            Set(action, "m_Buff", Ref<BlueprintBuffReference>(buff));
            return action;
        }

        private static ContextActionRemoveBuff RemoveFromCaster(BlueprintBuff buff, string name)
        {
            var action = new ContextActionRemoveBuff { name = name, ToCaster = true };
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
            var host = f.Buff(token + "Host", name, description, AreaLink(area, token));
            SetEnum(host, "m_Flags", "HiddenInUi");
            var toggle = Toggle(f, token, name, description, host, command);
            return f.Feature(token + "Feature", name, description, Facts(toggle));
        }

        private static AddAreaEffect AreaLink(BlueprintAbilityAreaEffect area, string token)
        {
            var link = new AddAreaEffect { name = "Phantom0085AreaLink" + token };
            Set(link, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(area));
            return link;
        }

        private static ContextRankConfig Rank(BlueprintCharacterClass cls, string progression, params int[] thresholds)
            => ArchetypeBatch01445.Rank(cls, progression, thresholds);

        private static ContextRankConfig StatBonusRank(BlueprintCharacterClass cls, StatType stat)
        {
            var r = new ContextRankConfig();
            SetEnum(r, "m_BaseValueType", "StatBonus");
            SetEnum(r, "m_Progression", "AsIs");
            Set(r, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(cls) });
            Set(r, "m_Stat", stat);
            return r;
        }

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

        private static AbilityTargetHasFact OpeningTargetFact(BlueprintBuff buff)
        {
            var c = new AbilityTargetHasFact { name = "Phantom0085OpeningAllyTarget" };
            Set(c, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(buff) });
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
                ComponentsArray = new BlueprintComponent[] { new AbilityAreaEffectRunAction { name = "Phantom0085FuryAura", UnitEnter = PsychicAssets.Actions(Apply(furyBuff, new ContextDurationValue(), "Phantom0085FuryApply")), UnitExit = PsychicAssets.Actions(Remove(furyBuff, "Phantom0085FuryRemove")) } }
            }, "AngerAuraOfFuryArea", "Aura of Fury", "");
            var auraOfFury = AuraFeature(f, "AngerAuraOfFury", "Aura of Fury",
                "When the spiritualist reaches 7th level, as a swift action, the phantom can emit a 20-foot-radius aura of fury. Creatures within the aura gain a +2 bonus on melee attack rolls but take a -2 penalty to AC. Ending the aura is a free action.",
                furyAura, UnitCommand.CommandType.Swift);

            var mienBuff = f.Buff("AngerFerociousMienEffect", "Ferocious Mien",
                "The phantom grows one size category larger and fiercer, as enlarge person and rage.");
            mienBuff.ComponentsArray = CopyComponents(f.Native<BlueprintBuff>(EnlargePersonBuffId))
                .Concat(CopyComponents(f.Native<BlueprintBuff>(RageBuffId))).ToArray();
            var mienResource = FixedResource(f, "AngerFerociousMienResource", 3);
            var mienAbility = f.Ability("AngerFerociousMienAbility", "Ferocious Mien",
                "Three times per day as a swift action, a phantom in ectoplasmic form can grow more ferocious and frightening. It becomes one size category larger, as enlarge person, and grows fiercer in combat, as rage. This effect lasts for 1 round per spiritualist class level.",
                AbilityType.Supernatural, UnitCommand.CommandType.Swift, AbilityRange.Personal,
                PsychicAssets.Run(Apply(mienBuff, new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, BonusValue = new ContextValue { ValueType = ContextValueType.Rank } }, "Phantom0085MienApply")),
                Rank(character, "OnePlusDivStep"),
                Cost(mienResource));
            mienAbility.CanTargetSelf = true;
            var ferociousMien = f.Feature("AngerFerociousMien", "Ferocious Mien", NameOf(mienAbility), Facts(mienAbility), Grant(mienResource));

            var wailResource = FixedResource(f, "AngerFuriousWailResource", 1);
            var wail = ConvertToSpellLike(f, f.Cache, f.Native<BlueprintAbility>(WailOfBansheeId), "AngerFuriousWail", phantomClass, phantomClass, StatType.Charisma, wailResource, supernatural: true);
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

        private static AddAbilityResources Grant(BlueprintAbilityResource resource)
        {
            var c = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true };
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
        internal static BlueprintAbility ConvertToSpellLike(FactoryShim f, BlueprintsCache cache, BlueprintAbility root,
            string token, BlueprintCharacterClass casterClass, BlueprintCharacterClass dcClass, StatType dcStat,
            BlueprintAbilityResource resource, bool supernatural = false, bool noScaling = false)
        {
            var ability = (BlueprintAbility)CopyData(root);
            ability.AssetGuid = Id(token);
            ability.name = Spiritualist0085Ids.AssetPrefix + token;
            ability.Type = supernatural ? AbilityType.Supernatural : AbilityType.SpellLike;
            Set(ability, "m_DisplayName", Text(token, "name", NameOf(root)));
            Set(ability, "m_Description", Text(token, "description", DescOf(root)));
            ability.Parent = null;
            var components = new List<BlueprintComponent>();
            foreach (var c in ability.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                if (c is SpellComponent || c is SpellListComponent) continue;
                if (c is ContextRankConfig rank)
                {
                    var clone = (ContextRankConfig)CopyData(rank);
                    Set(clone, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(casterClass) });
                    components.Add(clone);
                    continue;
                }
                if (c is ContextCalculateAbilityParamsBasedOnClass paramsByClass)
                {
                    var clone = (ContextCalculateAbilityParamsBasedOnClass)CopyData(paramsByClass);
                    Set(clone, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(dcClass));
                    Set(clone, "StatType", dcStat);
                    components.Add(clone);
                    continue;
                }
                components.Add(c);
            }
            if (!components.Any(c => c is ContextCalculateAbilityParamsBasedOnClass))
                components.Add(Params(dcClass, dcStat, "Phantom0085SlaParams" + token));
            components.Add(Cost(resource));
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
                ComponentsArray = new BlueprintComponent[] { new AbilityAreaEffectRunAction { name = "Phantom0085DespairAura", UnitEnter = PsychicAssets.Actions(Apply(despairBuff, new ContextDurationValue(), "Phantom0085DespairApply")), UnitExit = PsychicAssets.Actions(Remove(despairBuff, "Phantom0085DespairRemove")) } }
            }, "DespairAuraOfDespairArea", "Aura of Despair", "");
            var auraOfDespair = AuraFeature(f, "DespairAuraOfDespair", "Aura of Despair",
                "When the spiritualist reaches 7th level, as a swift action, the phantom can emit a 20-foot aura of despair. Enemies within the aura take a -2 penalty on all saving throws. This is a fear effect. Deactivating the aura is a free action.",
                despairAura, UnitCommand.CommandType.Swift);

            var shoutResource = FixedResource(f, "DespairDespairingShoutResource", 3);
            var shout = ConvertToSpellLike(f, f.Cache, f.Native<BlueprintAbility>(CrushingDespairSpellId), "DespairDespairingShout", phantomClass, phantomClass, StatType.Charisma, shoutResource, supernatural: true);
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
            var hatedMove = f.Ability("HatredHatedTargetAbility", "Hated Target",
                "The phantom can take a move action to designate one creature within its line of sight as a hated target. At 7th level, this becomes a swift action.",
                AbilityType.Supernatural, UnitCommand.CommandType.Move, AbilityRange.Medium,
                PsychicAssets.Run(Apply(hatedBuff, new ContextDurationValue(), "Phantom0085HatedApply")));
            hatedMove.CanTargetEnemies = true;
            var hatedSwift = (BlueprintAbility)CopyData(hatedMove);
            hatedSwift.AssetGuid = Id("HatredHatedTargetSwiftAbility");
            hatedSwift.name = Spiritualist0085Ids.AssetPrefix + "HatredHatedTargetSwiftAbility";
            hatedSwift.ActionType = UnitCommand.CommandType.Swift;
            Set(hatedSwift, "m_DisplayName", Text("HatredHatedTargetSwiftAbility", "name", "Hated Target (Swift)"));
            f.Register(hatedSwift, "HatredHatedTargetSwiftAbility");
            var hatedTarget = f.Feature("HatredHatedTarget", "Hated Target", NameOf(hatedMove), Facts(hatedMove),
                ClassLevelGrant(character, 7, f.Feature("HatredHatedTargetSwiftFeature", "Hated Target (Swift)", DescOf(hatedSwift), Facts(hatedSwift)), "Phantom0085HatedSwift"));

            var hatefulBuff = f.Buff("HatredHatefulAuraEffect", "Hateful Aura",
                "Enemies within the aura that deal damage to the spiritualist or the phantom take damage equal to the phantom's Charisma bonus. This is a mind-affecting pain effect.",
                new Phantom0085HatefulAura { name = "Phantom0085HatefulAura" },
                StatBonusRank(phantomClass, StatType.Charisma),
                new SpellDescriptorComponent { name = "Phantom0085HatefulDescriptor", Descriptor = Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.MindAffecting });
            var hatefulAura = f.Add(new BlueprintAbilityAreaEffect
            {
                Shape = AreaEffectShape.Cylinder, Size = 20.Feet(), AffectEnemies = true,
                ComponentsArray = new BlueprintComponent[] { new AbilityAreaEffectRunAction { name = "Phantom0085HatefulArea", UnitEnter = PsychicAssets.Actions(Apply(hatefulBuff, new ContextDurationValue(), "Phantom0085HatefulApply")), UnitExit = PsychicAssets.Actions(Remove(hatefulBuff, "Phantom0085HatefulRemove")) } }
            }, "HatredHatefulAuraArea", "Hateful Aura", "");
            var hatefulAuraFeature = AuraFeature(f, "HatredHatefulAura", "Hateful Aura",
                "When the spiritualist reaches 7th level, as a swift action, the phantom can emit a 20-foot-radius aura that inflicts pain upon the minds of those who dare damage it or its master.",
                hatefulAura, UnitCommand.CommandType.Swift);

            var sneakAttack = f.Feature("HatredSneakAttack", "Sneak Attack",
                "At 12th level, the phantom gains the sneak attack ability, dealing extra precision damage when flanking or catching its target flat-footed.",
                new AddContextStatBonus { name = "Phantom0085SneakAttack", Stat = StatType.SneakAttack, Descriptor = ModifierDescriptor.Feat, Value = new ContextValue { ValueType = ContextValueType.Rank } },
                Rank(phantomClass, "Custom", 13, 3, 20, 5));

            var sharedBuff = f.Buff("HatredSharedHatredEffect", "Shared Hatred",
                "When the phantom designates a hated enemy, its allies also gain a +2 bonus on attack rolls and a +4 bonus on damage rolls against that enemy.",
                new AttackBonusAgainstTarget { name = "Phantom0085SharedAttack", Value = new ContextValue { Value = 2 }, CheckCasterFriend = true },
                new DamageBonusAgainstTarget { name = "Phantom0085SharedDamage", Value = new ContextValue { Value = 4 }, CheckCasterFriend = true });
            var sharedHatred = f.Feature("HatredSharedHatred", "Shared Hatred", NameOf(sharedBuff));
            // The hated-target application carries the shared grant when present.
            var apply = hatedMove.GetComponent<AbilityEffectRunAction>();
            var sharedConditional = Cond("Phantom0085SharedConditional", new Condition[] { HasFact(sharedHatred, "Phantom0085SharedCheck") }, PsychicAssets.Actions(Apply(sharedBuff, new ContextDurationValue(), "Phantom0085SharedApply", asChild: true)), PsychicAssets.Actions());
            apply.Actions = PsychicAssets.Actions(apply.Actions.Actions.Concat(new GameAction[] { sharedConditional }).ToArray());
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
            SetEnum(immuneBuff, "m_Flags", "RemoveOnRest, HiddenInUi");
            var frightenedAura = (BlueprintBuff)CopyData(frightened);
            frightenedAura.AssetGuid = Id("FearIncreaseFearAuraBuff");
            frightenedAura.name = Spiritualist0085Ids.AssetPrefix + "FearIncreaseFearAuraBuff";
            f.Register(frightenedAura, "FearIncreaseFearAuraBuff");
            var shelterBuff = f.Buff("FearShelterAlliesEffect", "Shelter Allies",
                "Allies within the phantom's increase fear aura are immune to fear as long as they are within the aura.",
                new AddConditionImmunity { name = "Phantom0085ShelterShaken", Condition = UnitCondition.Shaken },
                new AddConditionImmunity { name = "Phantom0085ShelterFrightened", Condition = UnitCondition.Frightened },
                new BuffDescriptorImmunity { name = "Phantom0085ShelterDescriptor", Descriptor = Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Shaken | Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Frightened },
                new SpellImmunityToSpellDescriptor { name = "Phantom0085ShelterSpell", Descriptor = Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Shaken | Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.Frightened });
            var shelterAllies = f.Feature("FearShelterAllies", "Shelter Allies", NameOf(shelterBuff));

            var enemySave = new ContextActionSavingThrow
            {
                name = "Phantom0085IncreaseFearSave", Type = SavingThrowType.Will,
                Actions = PsychicAssets.Actions(new ContextActionConditionalSaved
                {
                    name = "Phantom0085IncreaseFearSaved",
                    Succeed = PsychicAssets.Actions(Apply(immuneBuff, new ContextDurationValue(), "Phantom0085IncreaseFearImmuneApply")),
                    Failed = PsychicAssets.Actions(Apply(frightenedAura, new ContextDurationValue(), "Phantom0085IncreaseFearFrightenedApply"))
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
                PsychicAssets.Actions(Cond("Phantom0085IncreaseFearShelterCheck",
                    new Condition[] { HasFact(shelterAllies, "Phantom0085IncreaseFearHasShelter") },
                    PsychicAssets.Actions(Apply(shelterBuff, new ContextDurationValue(), "Phantom0085IncreaseFearShelterApply")),
                    PsychicAssets.Actions())));
            var removeAction = Cond("Phantom0085IncreaseFearRemove",
                new Condition[] { new ContextConditionIsEnemy { name = "Phantom0085IncreaseFearRemoveIsEnemy" } },
                PsychicAssets.Actions(
                    RemoveFromCaster(immuneBuff, "Phantom0085IncreaseFearRemoveImmune"),
                    RemoveFromCaster(frightenedAura, "Phantom0085IncreaseFearRemoveFrightened2")),
                PsychicAssets.Actions(RemoveFromCaster(shelterBuff, "Phantom0085IncreaseFearRemoveShelter")));
            var increaseAura = f.Add(new BlueprintAbilityAreaEffect
            {
                Shape = AreaEffectShape.Cylinder, Size = 20.Feet(), AffectEnemies = true,
                ComponentsArray = new BlueprintComponent[]
                {
                    new AbilityAreaEffectRunAction { name = "Phantom0085IncreaseFearArea", UnitEnter = PsychicAssets.Actions(auraAction), UnitExit = PsychicAssets.Actions(removeAction), Round = PsychicAssets.Actions(auraAction) }
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
                ComponentsArray = new BlueprintComponent[] { new AbilityAreaEffectRunAction { name = "Phantom0085DeterminationArea", UnitEnter = PsychicAssets.Actions(Apply(determinationBuff, new ContextDurationValue(), "Phantom0085DeterminationApply")), UnitExit = PsychicAssets.Actions(Remove(determinationBuff, "Phantom0085DeterminationRemove")) } }
            }, "ZealDeterminationAuraArea", "Determination Aura", "");
            var determinationAuraFeature = AuraFeature(f, "ZealDeterminationAura", "Determination Aura",
                "When the spiritualist reaches 7th level, as a swift action, the phantom can emit a 20-foot radius aura that grants its zeal to nearby allies. Allies within the aura gain a +2 competence bonus on attack rolls and saving throws.",
                determinationAura, UnitCommand.CommandType.Swift);

            var hasteResource = FixedResource(f, "ZealHasteResource", 3);
            var haste = ConvertToSpellLike(f, f.Cache, f.Native<BlueprintAbility>(HasteSpellId), "ZealHaste", phantomClass, phantomClass, StatType.Charisma, hasteResource, noScaling: true);
            var zealHaste = f.Feature("ZealHasteFeature", "Haste",
                "Three times per day as a standard action, the phantom can use haste as a spell-like ability, using its Hit Dice as caster level.",
                Facts(haste), Grant(hasteResource));

            var resolveResource = FixedResource(f, "ZealZealsResolveResource", 3);
            var resolveAttackBuff = (BlueprintBuff)CopyData(f.Native<BlueprintBuff>(LuckDomainBuffId));
            resolveAttackBuff.AssetGuid = Id("ZealZealsResolveAttackBuff");
            resolveAttackBuff.name = Spiritualist0085Ids.AssetPrefix + "ZealZealsResolveAttackBuff";
            resolveAttackBuff.ComponentsArray = new BlueprintComponent[]
            {
                new Phantom0085Reroll { name = "Phantom0085RerollAttack", Rule = RuleType.AttackRoll, Resource = Ref<BlueprintAbilityResourceReference>(resolveResource) }
            };
            Set(resolveAttackBuff, "m_DisplayName", Text("ZealZealsResolveAttackBuff", "name", "Zeal's Resolve: Attack Roll"));
            f.Register(resolveAttackBuff, "ZealZealsResolveAttackBuff");
            var resolveSaveBuff = (BlueprintBuff)CopyData(resolveAttackBuff);
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

        private static BlueprintFeature[] BuildKindness(FactoryShim f, BlueprintCharacterClass character, BlueprintCharacterClass phantomClass)
        {
            // Opening Strike: standard attack; on hit, one threatening ally gains
            // a swift attack against the target. Exceptional Aid adds CHA damage.
            var targetBuff = f.Buff("KindnessOpeningStrikeTarget", "Opening Strike Target",
                "As a standard action, the phantom can make a melee attack against a foe. If this attack hits, the spiritualist designates one ally threatening that enemy. This ally can make a single attack at his full base attack bonus against that enemy as a swift action.");
            var allyAbility = f.Ability("KindnessOpeningStrikeAllyAbility", "Opening Strike", DescOf(targetBuff),
                AbilityType.Special, UnitCommand.CommandType.Swift, AbilityRange.Weapon,
                OpeningTargetFact(targetBuff));
            allyAbility.NeedEquipWeapons = true;
            var allyBuff = f.Buff("KindnessOpeningStrikeAlly", "Opening Strike", DescOf(targetBuff), Facts(allyAbility));
            var openingAbility = f.Ability("KindnessOpeningStrikeAbility", "Opening Strike", DescOf(targetBuff),
                AbilityType.Special, UnitCommand.CommandType.Standard, AbilityRange.Weapon,
                PsychicAssets.Run(new Phantom0085OpeningStrike { name = "Phantom0085OpeningStrike", TargetBuff = Ref<BlueprintBuffReference>(targetBuff), AllyBuff = Ref<BlueprintBuffReference>(allyBuff) }));
            openingAbility.NeedEquipWeapons = true;
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
            var laySelf = (BlueprintAbility)CopyData(f.Native<BlueprintAbility>(LayOnHandsSelfId));
            laySelf.AssetGuid = Id("KindnessEthericHealingSelf");
            laySelf.name = Spiritualist0085Ids.AssetPrefix + "KindnessEthericHealingSelf";
            laySelf.Range = AbilityRange.Touch;
            laySelf.ComponentsArray = laySelf.ComponentsArray
                .Where(c => !(c is AbilityCasterAlignment))
                .Select(c => c is ContextRankConfig r ? Rebind(r, phantomClass) : c is AbilityResourceLogic l ? Rebind(l, healingResource) : c)
                .Concat(new BlueprintComponent[] { new Phantom0085TargetSelfOrMaster { name = "Phantom0085HealSelfOrMaster" } }).ToArray();
            Set(laySelf, "m_DisplayName", Text("KindnessEthericHealingSelf", "name", "Etheric Healing: Self or Master"));
            f.Register(laySelf, "KindnessEthericHealingSelf");
            var layOthers = (BlueprintAbility)CopyData(f.Native<BlueprintAbility>(LayOnHandsOthersId));
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

            // Mercy selection: four mercies available to a 12th-level paladin.
            var mercySelection = f.Native<BlueprintFeatureSelection>(MercySelectionId);
            var phantomMercy = f.Add(new BlueprintFeatureSelection { Ranks = 1 }, "KindnessPhantomMercySelection", "Phantom Mercy",
                "The phantom selects mercies available to a 12th-level paladin and adds them to its lay on hands ability.");
            var mercyOptions = new List<BlueprintFeature>();
            foreach (var reference in (BlueprintFeatureReference[])Get(mercySelection, "m_AllFeatures"))
            {
                var mercy = f.Cache.Load(reference.Guid) as BlueprintFeature;
                if (mercy == null) continue;
                var prereq = mercy.GetComponent<PrerequisiteClassLevel>();
                if (prereq != null && prereq.Level > 12) continue;
                var clone = (BlueprintFeature)CopyData(mercy);
                clone.AssetGuid = Id("KindnessPhantomMercy" + mercy.name);
                clone.name = Spiritualist0085Ids.AssetPrefix + "KindnessPhantomMercy" + mercy.name;
                clone.ComponentsArray = (clone.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                    .Where(c => !(c is PrerequisiteClassLevel) && !(c is PrerequisiteArchetypeLevel)).ToArray();
                f.Register(clone, "KindnessPhantomMercy" + mercy.name);
                mercyOptions.Add(clone);
            }
            Set(phantomMercy, "m_Features", mercyOptions.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(phantomMercy, "m_AllFeatures", mercyOptions.Select(Ref<BlueprintFeatureReference>).ToArray());
            var expandedAid = f.Feature("KindnessExpandedAid", "Expanded Aid",
                "When the spiritualist reaches 12th level, the phantom can use the aid another action as a move action. Additionally, the phantom selects four mercies available to a 12th-level paladin that it adds to its lay on hands ability.",
                new Phantom0085AidAnotherActionType { name = "Phantom0085ExpandedAid", ActionType = UnitCommand.CommandType.Move },
                new Phantom0085GrantSelection { name = "Phantom0085MercyGrant1", Selection = Ref<BlueprintFeatureSelectionReference>(phantomMercy), Count = 4 });
            var exceptionalAid = f.Feature("KindnessExceptionalAid", "Exceptional Aid",
                "When the spiritualist reaches 17th level, the phantom can use the aid another action as a swift action. Additionally, when designating an ally for an opening strike, that ally gains a bonus equal to the phantom's Charisma modifier on damage rolls for the attack granted to him.",
                new Phantom0085AidAnotherActionType { name = "Phantom0085ExceptionalAid", ActionType = UnitCommand.CommandType.Swift });
            return new[] { openingStrike, ethericHealing, expandedAid, exceptionalAid };
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

        internal static BlueprintFeature GrantToPet(FactoryShim f, string token, BlueprintFeature feature, PetType petType)
        {
            var grant = new AddFeatureToPet { name = "Phantom0085Grant" + token }; Set(grant, "m_PetType", petType);
            Set(grant, "m_Feature", Ref<BlueprintFeatureReference>(feature));
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
            var fusedSave = f.Buff("FusedConsciousnessSave", "Fused Consciousness", "",
                new SavingThrowBonusAgainstDescriptor { name = "Phantom0085FusedSave", SpellDescriptor = Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.MindAffecting, Bonus = new ContextValue { ValueType = ContextValueType.Rank }, ModifierDescriptor = ModifierDescriptor.UntypedStackable },
                Rank(character, "Custom", 11, 4, 20, 8));
            SetEnum(fusedSave, "m_Flags", "HiddenInUi");
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
                Group = (ActivatableAbilityGroup)850085,
                WeightInGroup = 1
            }, token, name, description);
            Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(buff));
            return f.Feature(token + "Feature", name, description, Facts(toggle));
        }

        internal static BlueprintFeature BuildBondedManifestation(FactoryShim f, BlueprintCharacterClass character, BlueprintCharacterClass phantomClass,
            BlueprintFeatureSelection selection, out BlueprintFeature dualBond)
        {
            var shieldBuff = f.Buff("BondedManifestationEffect", "Bonded Manifestation",
                "The spiritualist gains an ectoplasmic shield: +4 shield bonus to AC (also against incorporeal touch attacks), increasing to +6 at 13th level. At 8th level she sprouts ectoplasmic tendrils; at 18th level she can attack all creatures in reach with them, rolling twice and taking the better result.",
                new AddContextStatBonus { name = "Phantom0085BondedShield", Stat = StatType.AC, Descriptor = ModifierDescriptor.Shield, Value = new ContextValue { ValueType = ContextValueType.Rank } },
                Rank(character, "Custom", 12, 4, 20, 6),
                new Phantom0085ShieldToIncorporealTouch { name = "Phantom0085BondedIncorporealTouch" },
                new Phantom0085BondedTendrils { name = "Phantom0085BondedTendrils" });
            var resource = f.Add(new BlueprintAbilityResource(), "BondedManifestationResource", "", "");
            var amount = Get(resource, "m_MaxAmount");
            Set(amount, "BaseValue", 3);
            Set(amount, "IncreasedByLevel", true); Set(amount, "LevelIncrease", 1);
            Set(amount, "m_ClassDiv", new[] { Ref<BlueprintCharacterClassReference>(character) });
            Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            Set(resource, "m_MaxAmount", amount);
            var toggle = f.Add(new BlueprintActivatableAbility
            {
                ActivationType = AbilityActivationType.WithUnitCommand,
                DeactivateIfOwnerDisabled = true,
                ComponentsArray = new BlueprintComponent[]
                {
                    BondedCost(resource),
                    new Phantom0085BondedRestriction { name = "Phantom0085BondedRestriction" }
                }
            }, "BondedManifestation", "Bonded Manifestation", DescOf(shieldBuff));
            Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Swift);
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(shieldBuff));
            var tendril8 = BuildTendril(f, character, phantomClass, "BondedTendrils8", "Tendrils Attack",
                "At 8th level, the spiritualist sprouts ectoplasmic tendrils. As a swift action, she can attack a creature within her melee reach with them, using the damage dice of her manifested phantom.",
                UnitCommand.CommandType.Swift, fullRound: false);
            var tendril18 = BuildTendril(f, character, phantomClass, "BondedTendrils18", "Tendrils Attack II",
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
                dualResource, Rank(character, "OnePlusDivStep"));
            dualBond.ReapplyOnLevelUp = true;
            return bonded;
        }

        private static BlueprintFeature BuildTendril(FactoryShim f, BlueprintCharacterClass character, BlueprintCharacterClass phantomClass,
            string token, string name, string description, UnitCommand.CommandType action, bool fullRound)
        {
            var diceBuff = f.Buff(token + "Dice", "", "",
                new Phantom0085TendrilDice { name = "Phantom0085TendrilDice" + token },
                Rank(character, "DelayedStartPlusDivStep"));
            SetEnum(diceBuff, "m_Flags", "HiddenInUi");
            var diceRank = diceBuff.GetComponent<ContextRankConfig>();
            Set(diceRank, "m_StepLevel", 3);
            Set(diceRank, "m_StartLevel", 4);
            BlueprintBuff rerollBuff = null;
            if (fullRound)
            {
                rerollBuff = f.Buff(token + "Reroll", "", "",
                    new ModifyD20 { name = "Phantom0085TendrilReroll", Rule = RuleType.AttackRoll, RollsAmount = 1, TakeBest = true });
                SetEnum(rerollBuff, "m_Flags", "HiddenInUi");
            }
            var slam = f.Native<BlueprintItemWeapon>(Spiritualist0085Ids.SlamWeaponId);
            var ability = f.Ability(token + "Ability", name, description, AbilityType.Supernatural, action, AbilityRange.Touch,
                PsychicAssets.Run(new Phantom0085TendrilAttack
                {
                    name = "Phantom0085Tendril" + token, FullRound = fullRound,
                    DiceBuff = Ref<BlueprintBuffReference>(diceBuff),
                    RerollBuff = rerollBuff != null ? Ref<BlueprintBuffReference>(rerollBuff) : null,
                    Slam = Ref<BlueprintItemWeaponReference>(slam)
                }));
            ability.CanTargetEnemies = true;
            if (fullRound) Set(ability, "m_IsFullRoundAction", true);
            return f.Feature(token, name, description, Facts(ability));
        }

        internal static BlueprintFeature BuildInference(FactoryShim f, BlueprintCharacterClass character, BlueprintCharacterClass phantomClass, bool greater)
        {
            var token = greater ? "GreaterSpiritualInference" : "SpiritualInference";
            var ownerBuff = f.Buff(token + "Owner", "", "",
                new AddStatBonus { name = "Phantom0085InferenceOwnerAC", Stat = StatType.AC, Value = greater ? 4 : 2, Descriptor = ModifierDescriptor.Shield },
                new AddStatBonus { name = "Phantom0085InferenceOwnerFort", Stat = StatType.SaveFortitude, Value = greater ? 4 : 2, Descriptor = ModifierDescriptor.Circumstance },
                new AddStatBonus { name = "Phantom0085InferenceOwnerRef", Stat = StatType.SaveReflex, Value = greater ? 4 : 2, Descriptor = ModifierDescriptor.Circumstance },
                new AddStatBonus { name = "Phantom0085InferenceOwnerWill", Stat = StatType.SaveWill, Value = greater ? 4 : 2, Descriptor = ModifierDescriptor.Circumstance });
            var allyBuff = f.Buff(token + "Ally", "", "",
                new AddStatBonus { name = "Phantom0085InferenceAllyAC", Stat = StatType.AC, Value = 2, Descriptor = ModifierDescriptor.Shield },
                new AddStatBonus { name = "Phantom0085InferenceAllyFort", Stat = StatType.SaveFortitude, Value = 2, Descriptor = ModifierDescriptor.Circumstance },
                new AddStatBonus { name = "Phantom0085InferenceAllyRef", Stat = StatType.SaveReflex, Value = 2, Descriptor = ModifierDescriptor.Circumstance },
                new AddStatBonus { name = "Phantom0085InferenceAllyWill", Stat = StatType.SaveWill, Value = 2, Descriptor = ModifierDescriptor.Circumstance });
            ownerBuff.Stacking = StackingType.Stack;
            allyBuff.Stacking = StackingType.Stack;
            var aura = f.Add(new BlueprintAbilityAreaEffect
            {
                Shape = AreaEffectShape.Cylinder, Size = 10.Feet(), AffectEnemies = false,
                ComponentsArray = new BlueprintComponent[]
                {
                    new AbilityAreaEffectRunAction
                    {
                        name = "Phantom0085InferenceAura" + token,
                        UnitEnter = PsychicAssets.Actions(new Phantom0085InferenceEnter { name = "Phantom0085InferenceEnter" + token, Greater = greater, OwnerBuff = Ref<BlueprintBuffReference>(ownerBuff), AllyBuff = Ref<BlueprintBuffReference>(allyBuff) }),
                        UnitExit = PsychicAssets.Actions(new Phantom0085InferenceExit { name = "Phantom0085InferenceExit" + token, OwnerBuff = Ref<BlueprintBuffReference>(ownerBuff), AllyBuff = Ref<BlueprintBuffReference>(allyBuff) }),
                        Round = PsychicAssets.Actions(new Phantom0085InferenceEnter { name = "Phantom0085InferenceRound" + token, Greater = greater, OwnerBuff = Ref<BlueprintBuffReference>(ownerBuff), AllyBuff = Ref<BlueprintBuffReference>(allyBuff) })
                    }
                }
            }, token + "Area", greater ? "Greater Spiritual Inference" : "Spiritual Inference", "");
            var petFeature = f.Feature(token + "Pet", greater ? "Greater Spiritual Inference" : "Spiritual Inference",
                greater
                    ? "At 12th level, whenever allies are within the phantom's reach, each ally gains a +2 shield bonus to AC and a +2 circumstance bonus on saving throws. For the spiritualist, these bonuses increase to +4. The phantom itself does not benefit."
                    : "At 4th level, whenever the spiritualist is within the reach of her manifested phantom, she gains a +2 shield bonus to AC and a +2 circumstance bonus on saving throws. She doesn't gain these bonuses when the phantom is grappled, helpless or unconscious.",
                AreaLink(aura, token));
            var grant = new AddFeatureToPet { name = "Phantom0085InferenceGrant" + token }; Set(grant, "m_PetType", PhantomPetType);
            Set(grant, "m_Feature", Ref<BlueprintFeatureReference>(petFeature));
            return f.Feature(token, greater ? "Greater Spiritual Inference" : "Spiritual Inference", DescOf(petFeature), grant);
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
                var capstone = f.Feature(row.Name + "PotentPhantom", "Potent Phantom: " + row.Name, row.Description,
                    new Phantom0085PotentGrant { name = "Phantom0085Potent" + row.Name, Emotion = Ref<BlueprintProgressionReference>(emotion) },
                    NoFeature(emotion, "Phantom0085PotentNotPrimary" + row.Name));
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