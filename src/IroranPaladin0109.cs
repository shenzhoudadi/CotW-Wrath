// Iroran Paladin adapted from Holic75/KingmakerRebalance (MIT),
// Archetypes/IroranPaladin.cs. Every native donor blueprint stays immutable;
// only the archetype graph under IroranPaladinArchetype0109 is published.
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class IroranPaladin0109
    {
        internal const string ModuleName = "Paladin archetype: Iroran Paladin";
        internal const string RootToken = "IroranPaladinArchetype0109";
        internal const string DeityToken = "IroranPaladinDeity0109";
        internal const string ProficienciesToken = "IroranPaladinProficiencies0109";
        internal const string UnarmedToken = "IroranUnarmedStrike0109";
        internal const string ConfidentDefenseToken = "IroranConfidentDefense0109";
        internal const string TrialToken = "IroranPersonalTrial0109";
        internal const string TrialAbilityToken = "IroranPersonalTrialAbility0109";
        internal const string TrialMarkToken = "IroranPersonalTrialMark0109";
        internal const string TrialResourceToken = "IroranPersonalTrialResource0109";
        internal const string TrialExtraToken = "IroranPersonalTrialExtraUse0109";
        internal const string KiToken = "IroranKiPool0109";
        internal const string KiIgnoreDrAbilityToken = "IroranKiIgnoreDrAbility0109";
        internal const string KiIgnoreDrBuffToken = "IroranKiIgnoreDrBuff0109";
        internal const string DivineBodyToken = "IroranDivineBody0109";
        internal const string DivineBodyAbilityToken = "IroranDivineBodyAbility0109";
        internal const string DivineBodyResourceToken = "IroranDivineBodyResource0109";
        internal const string DivineBodyExtraUseToken = "IroranDivineBodyAdditionalUse0109";
        internal const string ExcellenceToken = "IroranAuraOfExcellence0109";
        internal const string PerfectionToken = "IroranAuraOfPerfection0109";

        internal const string PaladinId = "bfa11238e7ae3544bbeb4d0b92e897ec";
        internal const string MonkId = "e8f21e5b58e0569468e420ebea456124";
        internal const string IroriId = "23a77a5985de08349820429ce1b5a234";
        internal const string DeitySelectionId = "59e7a76987fe3b547b9cce045f4db3e4";
        internal const string ScaledFistArchetypeId = "5868fc82eb11a4244926363983897279";
        internal const string KiResourceId = "7d002c1025fbfe2458f1509bf7a89ce1";
        internal const string ScaledFistKiFeatureId = "ae98ab7bda409ef4bb39149a212d6732";
        internal const string ScaledFistACBuffId = "64acb179cc6a4f19bb3513d094b28d02";
        internal const string ScaledFistExtraAttackId = "ca948bb4ce1a2014fbf4d8d44b553074";
        internal const string MonkUnarmedStrikeId = "c3fbeb2ffebaaa64aa38ce7a0bb18fb0";
        internal const string ImprovedUnarmedStrikeId = "7812ad3672a4b9a4fb894ea402095167";
        internal const string LightArmorProficiencyId = "6d3728d4e9c9898458fe5e9532951132";
        internal const string MartialWeaponProficiencyId = "203992ef5b35c864390b4e4a1e200629";
        internal const string SimpleWeaponProficiencyId = "e70ecf1ed95ca2f40b754f1adb22bbdd";
        internal const string KiStrikeMagicId = "1188005ee3160f84f8bed8b19a7d46cf";
        internal const string KiStrikeColdIronSilverId = "7b657938fde78b14cae10fc0d3dcb991";
        internal const string KiStrikeLawfulId = "34439e527a8f5fb4588024e71960dd42";
        internal const string KiStrikeAdamantineId = "ddc10a3463bd4d54dbcbe993655cf64e";
        internal const string PaladinProficienciesId = "b10ff88c03308b649b50c31611c2fefb";
        internal const string SmiteEvilFeatureId = "3a6db57fce75b0244a6a5819528ddf26";
        internal const string SmiteEvilAbilityId = "7bb9eb2042e67bf489ccd1374423cdec";
        internal const string SmiteAdditionalUseId = "0f5c99ffb9c084545bbbe960b825d137";
        internal const string AuraOfCourageFeatureId = "e45ab30f49215054e83b4ea12165409f";
        internal const string AuraCarrierId = "c96380f6dcac83c45acdb698ae70ffc4";
        internal const string AuraAreaId = "7ced0efa297bd5142ab749f6e33b112b";
        internal const string ChannelEnergyId = "cb6d55dda5ab906459d18a435994a760";
        internal const string DivineBondSelectionId = "ad7dc4eba7bf92f4aba23f716d7a9ba6";
        internal const string AuraOfJusticeId = "9f13fdd044ccb8a439f27417481cb00e";
        internal const string WeaponBondSwitchAbilityId = "7ff088ab58c69854b82ea95c2b0e35b4";
        internal const string WeaponBondDurationBuffId = "bf570774501886f47b395a4bfe75eeb2";
        internal const string WeaponBondAdditionalUseId = "5a64de5435667da4eae2e4c95ec87917";
        internal const string FlamingChoiceId = "7902941ef70a0dc44bcfc174d6193386";
        internal const string KeenChoiceId = "27d76f1afda08a64d897cc81201b5218";
        internal static readonly string[] BondPlusIds =
        {
            "c8db0772b7059ec4eabe55b7e0e79824", "d2f45a2034d4f7643ba1a450bc5c4c06",
            "6d73f49b602e29a43a6faa2ea1e4a425", "f17c3ba33bb44d44782cb3851d823011",
            "b936ee90c070edb46bd76025dc1c5936"
        };
        internal static readonly string[] BondDefaultEnchantments =
        {
            "d704f90f54f813043a525f304f6c0050", "9e9bab3020ec5f64499e007880b37e52",
            "d072b841ba0668846adeb007f623bd6c", "6a6a0901d799ceb49b33d4851ff72132",
            "746ee366e50611146821d61e391edf16"
        };
        internal static readonly string[] UnarmedTierWeaponIds =
        {
            "f60c5a820b69fb243a4cce5d1d07d06e", "9e87f72f8069310469d4431e925f035a",
            "ee31a19c8aa965f46a90ae02d59d4af7", "889e2927a67bd724ea1c203fe1b8b652",
            "84af5e0b4f5f251449aac2b530fab3d0", "7afc18ecb2cd96541b5cb8395f40d48c"
        };

        const string ArchetypeText = "Iroran paladins meditate on self-perfection and train relentlessly, knowing that their example can inspire others to excel. Irori offers no universal paladin code— each paladin in his service creates his own code as part of his spiritual journey, seeing the adherence to such a self-formulated creed as one of the many tests one must face to reach perfection.";
        const string IroriText = "Iroran paladin must worship Irori.";
        const string ProficienciesText = "An Iroran paladin is proficient with all simple and martial weapons and with light armor. He is not proficient with medium armor, heavy armor, or shields.";
        const string UnarmedText = "At 1st level, an Iroran paladin gains Improved Unarmed Strike as a bonus feat. In addition, he gains the unarmed strike monk ability, treating his monk level as his paladin level for calculating his unarmed strike damage.";
        const string ConfidentDefenseText = "At 1st level, when wearing light or no armor and not using a shield, an Iroran paladin adds his Charisma bonus (if any) as a dodge bonus to his Armor Class, up to a maximum equal to his Iroran paladin level plus any real Scaled Fist monk levels. If he already has an effective native Scaled Fist Charisma armor class bonus, this ability only tops that bonus up to the same limit. If he is caught flat-footed or otherwise denied his Dexterity bonus, he also loses this bonus. This ability replaces his proficiency with medium armor, heavy armor, and shields.";
        const string TrialText = "Once per day, an Iroran paladin can as a swift action declare one target within line of sight as his personal trial. The Iroran paladin gains a +1 insight bonus on attack rolls and damage rolls against that creature, to his AC against attacks made by the target, and on saving throws against the target's spells and special abilities. This bonus increases by 1 at 4th level and every 4 levels thereafter, to a maximum bonus of +6 at 20th level. The personal trial effect remains until the target of the trial is dead or the next time the paladin rests and regains daily uses of this ability. At 4th level and every 4 levels thereafter, the Iroran paladin can use personal trial one additional time per day.";
        const string TrialMarkText = "The target of an Iroran paladin's personal trial. The paladin gains an insight bonus on attack rolls and damage rolls against this creature, to his AC against attacks made by it, and on saving throws against its spells and special abilities.";
        const string TrialExtraText = "An Iroran paladin can use his personal trial ability one additional time per day.";
        const string KiText = "At 4th level, an Iroran paladin gains a ki pool with an effective monk level equal to his class level. The number of points in an Iroran paladin's ki pool is equal to 1/2 his class level + his Charisma modifier. By spending 1 point from his ki pool, he can ignore any damage reduction possessed by the target of his personal trial ability for 1 round.";
        const string KiIgnoreDrText = "By spending 1 point from his ki pool, an iroran paladin can ignore any damage reduction possessed by the target of his personal trial ability for 1 round.";
        const string BondText = "Upon reaching 5th level, an Iroran paladin must form a bond with a weapon, except he can only choose to enhance his unarmed strike. This ability otherwise functions as and replaces divine bond.";
        const string ExcellenceText = "At 3rd level, an Iroran paladin is immune to any effect that would force him to reroll a die against his will; rerolls that take the higher result are preserved. Whenever an ally within 13 feet would be forced to roll twice and take the lower result, she instead rolls three times and takes the second-lowest result. This ability functions only while the Iroran paladin is conscious.";
        const string PerfectionText = "At 11th level, whenever an Iroran paladin or an ally within 13 feet would reroll a die and take the best result, an additional die is rolled as part of the reroll and the highest of the results is used. This ability functions only while the Iroran paladin is conscious.";

        static string TierToken(int index) => "IroranUnarmedTier" + (index + 1) + "0109";

        internal static void Register(BlueprintsCache cache)
        {
            if (MagusBatch01454.Existing(cache, ModuleName)) return;
            var f = new PsychicAssets(cache);
            var paladin = f.Native<BlueprintCharacterClass>(PaladinId);
            var monk = f.Native<BlueprintCharacterClass>(MonkId);
            var irori = f.Native<BlueprintFeature>(IroriId);
            var deities = f.Native<BlueprintFeatureSelection>(DeitySelectionId);

            var deity = BuildIrori(f, irori);
            var proficiencies = BuildProficiencies(f);
            var unarmed = BuildUnarmed(f, paladin, monk);
            var defense = BuildConfidentDefense(f, paladin, monk);
            var trialMark = BuildTrialMark(f);
            var trial = BuildPersonalTrial(f, paladin, trialMark);
            var trialExtra = BuildTrialExtraUse(f, trial);
            var ki = BuildKi(f, paladin, monk, trialMark);
            var body = BuildDivineBody(f, paladin);
            var bodyExtra = BuildDivineBodyExtraUse(f, body);
            var excellence = BuildAura(f, IroranRerolls0109.AuraKind.Excellence, "Excellence", ExcellenceToken, "Aura of Excellence", ExcellenceText);
            var perfection = BuildAura(f, IroranRerolls0109.AuraKind.Perfection, "Perfection", PerfectionToken, "Aura of Perfection", PerfectionText);

            // One PrerequisiteNoFeature per other native deity plus the Irori
            // alignment mask; an existing deity is never replaced.
            var prerequisites = ((BlueprintFeatureReference[])Get(deities, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>())
                .Where(d => d != null && d.Guid != irori.AssetGuid).GroupBy(d => d.Guid)
                .Select(g => { var p = new PrerequisiteNoFeature { name = "IroranNoDeity" + g.Key }; Set(p, "m_Feature", CopyData(g.First())); return (BlueprintComponent)p; })
                .Concat(new BlueprintComponent[] { new PrerequisiteAlignment { Alignment = AlignmentMaskType.Lawful | AlignmentMaskType.TrueNeutral } })
                .ToArray();

            var root = f.Add(new BlueprintArchetype
            {
                LocalizedName = Text(RootToken, "name", "Iroran Paladin"),
                LocalizedDescription = Text(RootToken, "description", ArchetypeText),
                ReplaceClassSkills = true,
                ClassSkills = paladin.ClassSkills.Concat(new[] { StatType.SkillAthletics, StatType.SkillMobility }).Distinct().ToArray(),
                ComponentsArray = prerequisites,
                AddFeatures = new[]
                {
                    Entry(1, deity, proficiencies, f.Native<BlueprintFeature>(ImprovedUnarmedStrikeId), unarmed, defense, trial),
                    Entry(3, excellence),
                    Entry(4, ki, f.Native<BlueprintFeature>(KiStrikeMagicId), trialExtra),
                    Entry(5, body),
                    Entry(7, f.Native<BlueprintFeature>(KiStrikeColdIronSilverId)),
                    Entry(8, f.Native<BlueprintFeature>(BondPlusIds[0]), trialExtra),
                    Entry(9, bodyExtra),
                    Entry(10, f.Native<BlueprintFeature>(KiStrikeLawfulId)),
                    Entry(11, perfection, f.Native<BlueprintFeature>(BondPlusIds[1])),
                    Entry(12, trialExtra),
                    Entry(13, bodyExtra),
                    Entry(14, f.Native<BlueprintFeature>(BondPlusIds[2])),
                    Entry(16, f.Native<BlueprintFeature>(KiStrikeAdamantineId), trialExtra),
                    Entry(17, f.Native<BlueprintFeature>(BondPlusIds[3]), bodyExtra),
                    Entry(20, f.Native<BlueprintFeature>(BondPlusIds[4]), trialExtra)
                },
                RemoveFeatures = new[]
                {
                    Entry(1, f.Native<BlueprintFeature>(PaladinProficienciesId), f.Native<BlueprintFeature>(SmiteEvilFeatureId)),
                    Entry(3, f.Native<BlueprintFeature>(AuraOfCourageFeatureId)),
                    Entry(4, f.Native<BlueprintFeature>(ChannelEnergyId), f.Native<BlueprintFeature>(SmiteAdditionalUseId)),
                    Entry(5, f.Native<BlueprintFeatureSelection>(DivineBondSelectionId)),
                    Entry(7, f.Native<BlueprintFeature>(SmiteAdditionalUseId)),
                    Entry(10, f.Native<BlueprintFeature>(SmiteAdditionalUseId)),
                    Entry(11, f.Native<BlueprintFeature>(AuraOfJusticeId)),
                    Entry(13, f.Native<BlueprintFeature>(SmiteAdditionalUseId)),
                    Entry(16, f.Native<BlueprintFeature>(SmiteAdditionalUseId)),
                    Entry(19, f.Native<BlueprintFeature>(SmiteAdditionalUseId))
                }
            }, RootToken, "Iroran Paladin", ArchetypeText);
            Set(root, "m_ParentClass", paladin);
            foreach (var feature in f.Assets.Values.OfType<BlueprintFeature>()) feature.IsClassFeature = true;
            foreach (var buff in f.Assets.Values.OfType<BlueprintBuff>()) buff.IsClassFeature = true;

            var group = MagusBatch01454.Group(deity, proficiencies, unarmed, defense, trial, ki, body, excellence, perfection);
            var undoClass = MagusBatch01454.CaptureClass(paladin);
            Action undo = () => undoClass();
            void Connect()
            {
                MagusBatch01454.ConnectArchetype(paladin, root, group);
                MagusBatch01454.Determinators(paladin, deity, defense);
            }
            void Verify()
            {
                MagusBatch01454.VerifyArchetype(paladin, root, group);
                MagusBatch01454.VerifyDeterminators(paladin, deity, defense);
                VerifyTiers(f, unarmed);
            }
            MagusBatch01454.Publish(ModuleName, f, Connect, undo, Verify);
        }

        static void VerifyTiers(PsychicAssets f, BlueprintFeature unarmed)
        {
            for (int i = 0; i < UnarmedTierWeaponIds.Length; i++)
            {
                var tier = f.Find<BlueprintFeature>(TierToken(i));
                var weapon = tier.GetComponent<EmptyHandWeaponOverride>();
                var reference = (BlueprintItemWeaponReference)Get(weapon, "m_Weapon");
                if (reference == null || reference.Guid != BlueprintGuid.Parse(UnarmedTierWeaponIds[i]))
                    throw new InvalidOperationException("0109 unarmed tier weapon mismatch: " + tier.name);
            }
            var manager = unarmed.GetComponent<IroranUnarmed0109>();
            if (manager.Tiers.Length != UnarmedTierWeaponIds.Length) throw new InvalidOperationException("0109 unarmed tier count mismatch.");
        }

        static BlueprintFeature BuildIrori(PsychicAssets f, BlueprintFeature irori)
        {
            // AddFacts adopts an existing same-blueprint fact and removes it with
            // this source; native conditional granting never takes ownership of
            // a religion the character already chose.
            var grant = new AddFeatureIfHasFact { name = "IroranIroriGrant0109", Not = true };
            Set(grant, "m_CheckedFact", Ref<BlueprintUnitFactReference>(irori));
            Set(grant, "m_Feature", Ref<BlueprintUnitFactReference>(irori));
            var feature = f.Feature(DeityToken, "Irori", IroriText, grant);
            Set(feature, "m_Icon", irori.Icon);
            return feature;
        }

        static BlueprintFeature BuildProficiencies(PsychicAssets f)
        {
            var light = f.Native<BlueprintFeature>(LightArmorProficiencyId);
            var martial = f.Native<BlueprintFeature>(MartialWeaponProficiencyId);
            var simple = f.Native<BlueprintFeature>(SimpleWeaponProficiencyId);
            var feature = f.Feature(ProficienciesToken, "Iroran Proficiencies", ProficienciesText, Facts(light, martial, simple));
            Set(feature, "m_Icon", light.Icon);
            return feature;
        }

        static BlueprintFeature BuildUnarmed(PsychicAssets f, BlueprintCharacterClass paladin, BlueprintCharacterClass monk)
        {
            var tiers = new BlueprintFeature[UnarmedTierWeaponIds.Length];
            for (int i = 0; i < tiers.Length; i++)
            {
                var weapon = f.Native<BlueprintItemWeapon>(UnarmedTierWeaponIds[i]);
                var @override = new EmptyHandWeaponOverride { name = "IroranUnarmedOverride" + (i + 1) + "0109", IsPermanent = true, IsMonkUnarmedStrike = true };
                Set(@override, "m_Weapon", Ref<BlueprintItemWeaponReference>(weapon));
                tiers[i] = f.Feature(TierToken(i), "Iroran Unarmed Strike", UnarmedText, @override);
                tiers[i].HideInUI = true;
                tiers[i].HideInCharacterSheetAndLevelUp = true;
                Set(tiers[i], "m_Icon", f.Native<BlueprintFeature>(MonkUnarmedStrikeId).Icon);
            }
            var manager = new IroranUnarmed0109
            {
                name = "IroranUnarmedManager0109",
                Paladin = Ref<BlueprintCharacterClassReference>(paladin),
                Monk = Ref<BlueprintCharacterClassReference>(monk),
                Tiers = tiers.Select(t => Ref<BlueprintUnitFactReference>(t)).ToArray(),
                MonkUnarmedStrike = Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>(MonkUnarmedStrikeId))
            };
            var feature = f.Feature(UnarmedToken, "Iroran Unarmed Strike", UnarmedText, manager);
            Set(feature, "m_Icon", f.Native<BlueprintFeature>(MonkUnarmedStrikeId).Icon);
            return feature;
        }

        static BlueprintFeature BuildConfidentDefense(PsychicAssets f, BlueprintCharacterClass paladin, BlueprintCharacterClass monk)
        {
            var recalc = new RecalculateOnStatChange { name = "IroranDefenseCharisma0109", Stat = StatType.Charisma };
            var component = new IroranConfidentDefense0109
            {
                name = "IroranConfidentDefenseRules0109",
                Paladin = Ref<BlueprintCharacterClassReference>(paladin),
                Monk = Ref<BlueprintCharacterClassReference>(monk),
                ScaledFist = Ref<BlueprintArchetypeReference>(f.Native<BlueprintArchetype>(ScaledFistArchetypeId)),
                ScaledFistCharismaAC = Ref<BlueprintBuffReference>(f.Native<BlueprintBuff>(ScaledFistACBuffId))
            };
            var feature = f.Feature(ConfidentDefenseToken, "Confident Defense", ConfidentDefenseText, recalc, component);
            Set(feature, "m_Icon", f.Native<BlueprintFeature>(PaladinProficienciesId).Icon);
            return feature;
        }

        static BlueprintBuff BuildTrialMark(PsychicAssets f)
        {
            // Stack keeps two Iroran casters' independent marks on one target;
            // UniqueBuff keeps one mark per caster; no alignment, DR or
            // party-sharing side effects from the native SmiteEvil chain.
            return f.Add(new BlueprintBuff
            {
                Stacking = StackingType.Stack,
                ComponentsArray = new BlueprintComponent[]
                {
                    new UniqueBuff { name = "IroranTrialMarkUnique0109" },
                    new RemoveBuffIfCasterIsMissing { name = "IroranTrialMarkCaster0109", RemoveOnCasterDeath = true },
                    new AttackBonusAgainstTarget { name = "IroranTrialMarkAttack0109", CheckCaster = true, Descriptor = ModifierDescriptor.Insight, Value = new ContextValue { ValueType = ContextValueType.Rank } },
                    new ACBonusAgainstTarget { name = "IroranTrialMarkAC0109", CheckCaster = true, Descriptor = ModifierDescriptor.Insight, Value = new ContextValue { ValueType = ContextValueType.Rank } },
                    new DamageBonusAgainstTarget { name = "IroranTrialMarkDamage0109", CheckCaster = true, ApplyToSpellDamage = true, Value = new ContextValue { ValueType = ContextValueType.Rank } }
                }
            }, TrialMarkToken, "Personal Trial", TrialMarkText);
        }

        static BlueprintFeature BuildPersonalTrial(PsychicAssets f, BlueprintCharacterClass paladin, BlueprintBuff mark)
        {
            SetEnum(mark, "m_Flags", "RemoveOnRest");
            // Uses come from the 4/8/12/16/20 extra-use features, so the
            // resource itself has no class scaling: 1, growing to 6 at level 20.
            var resource = f.Add(new BlueprintAbilityResource(), TrialResourceToken, "Personal Trial", TrialText);
            var amount = Get(resource, "m_MaxAmount");
            Set(amount, "BaseValue", 1);
            Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(amount, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            Set(resource, "m_MaxAmount", amount);

            var apply = new ContextActionApplyBuff { name = "IroranTrialApply0109", Permanent = true, IsFromSpell = false };
            Set(apply, "m_Buff", Ref<BlueprintBuffReference>(mark));
            // The attack, AC and damage components live on the mark. Their rank
            // is read from the buff context, so the class-level config has to
            // be on the buff itself, not only on the casting ability.
            var markRank = Rank(paladin, "OnePlusDivStep");
            markRank.name = "IroranTrialMarkRank0109";
            Set(markRank, "m_StepLevel", 4);
            mark.ComponentsArray = mark.ComponentsArray.Concat(new BlueprintComponent[] { markRank }).ToArray();
            var rank = Rank(paladin, "OnePlusDivStep");
            rank.name = "IroranTrialRank0109";
            Set(rank, "m_StepLevel", 4);
            var cost = Cost(resource);
            cost.name = "IroranTrialCost0109";
            var ability = f.Clone(f.Native<BlueprintAbility>(SmiteEvilAbilityId), TrialAbilityToken, "Personal Trial", TrialText);
            ability.ComponentsArray = ability.ComponentsArray
                .Where(c => !(c is AbilityEffectRunAction) && !(c is AbilityResourceLogic) && !(c is ContextRankConfig) && !(c is ContextCalculateSharedValue))
                .Concat(new BlueprintComponent[] { cost, rank, Run(apply) })
                .ToArray();

            var feature = f.Feature(TrialToken, "Personal Trial", TrialText,
                Facts(ability), Grant(resource),
                new IroranPersonalTrial0109 { name = "IroranTrialSaves0109", Mark = Ref<BlueprintBuffReference>(mark), Paladin = Ref<BlueprintCharacterClassReference>(paladin) });
            Set(feature, "m_Icon", ability.Icon);
            return feature;
        }

        static BlueprintFeature BuildTrialExtraUse(PsychicAssets f, BlueprintFeature trial)
        {
            var resource = (BlueprintAbilityResource)f.Assets[TrialResourceToken];
            var extra = f.Clone(f.Native<BlueprintFeature>(SmiteAdditionalUseId), TrialExtraToken, "Personal Trial: Additional Use", TrialExtraText);
            var increase = extra.GetComponents<IncreaseResourceAmount>().ToArray();
            if (increase.Length != 1) throw new InvalidOperationException("0109 native smite additional use component changed.");
            Set(increase[0], "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
            Set(extra, "m_Icon", trial.Icon);
            return extra;
        }

        static BlueprintFeature BuildKi(PsychicAssets f, BlueprintCharacterClass paladin, BlueprintCharacterClass monk, BlueprintBuff trialMark)
        {
            // Ki Power: Ignore DR mirrors the Scaled Fist extra attack donor:
            // a free self ability that spends 1 point of the same native ki pool.
            var ignoreDrBuff = f.Add(new BlueprintBuff
            {
                Stacking = StackingType.Replace,
                ComponentsArray = new BlueprintComponent[] { new IroranTrialMark0109 { name = "IroranKiIgnoreDrMark0109", Mark = Ref<BlueprintBuffReference>(trialMark) } }
            }, KiIgnoreDrBuffToken, "Ki Power: Ignore DR", KiIgnoreDrText);
            var apply = new ContextActionApplyBuff { name = "IroranKiIgnoreDrApply0109", IsFromSpell = false, AsChild = true, DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = 1 } };
            Set(apply, "m_Buff", Ref<BlueprintBuffReference>(ignoreDrBuff));
            var ignoreDr = f.Clone(f.Native<BlueprintAbility>(ScaledFistExtraAttackId), KiIgnoreDrAbilityToken, "Ki Power: Ignore DR", KiIgnoreDrText);
            ignoreDr.ComponentsArray = ignoreDr.ComponentsArray
                .Where(c => !(c is AbilityEffectRunAction))
                .Concat(new BlueprintComponent[] { Run(apply) })
                .ToArray();

            var ki = f.Native<BlueprintAbilityResource>(KiResourceId);
            var feature = f.Feature(KiToken, "Ki Pool", KiText,
                Facts(f.Native<BlueprintAbility>(ScaledFistExtraAttackId), ignoreDr),
                new IroranKi0109
                {
                    name = "IroranKiPoolRules0109",
                    Paladin = Ref<BlueprintCharacterClassReference>(paladin),
                    Monk = Ref<BlueprintCharacterClassReference>(monk),
                    Ki = Ref<BlueprintAbilityResourceReference>(ki),
                    ScaledFistKi = Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>(ScaledFistKiFeatureId))
                });
            Set(feature, "m_Icon", f.Native<BlueprintAbility>(ScaledFistExtraAttackId).Icon);
            return feature;
        }

        static BlueprintFeature BuildDivineBody(PsychicAssets f, BlueprintCharacterClass paladin)
        {
            var resource = f.Add(new BlueprintAbilityResource(), DivineBodyResourceToken, "Divine Body", BondText);
            var amount = Get(resource, "m_MaxAmount");
            Set(amount, "BaseValue", 1);
            Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(amount, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            Set(resource, "m_MaxAmount", amount);

            var durationValue = new ContextDurationValue { Rate = DurationRate.Minutes, DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = new ContextValue { ValueType = ContextValueType.Rank } };
            var enchant = new IroranDivineBody0109
            {
                name = "IroranDivineBodyEnchant0109",
                Group = ActivatableAbilityGroup.DivineWeaponProperty,
                DurationValue = durationValue,
                DefaultEnchantments = BondDefaultEnchantments.Select(id => Ref<BlueprintItemEnchantmentReference>(f.Native<BlueprintItemEnchantment>(id))).ToArray()
            };
            var duration = new ContextActionApplyBuff { name = "IroranDivineBodyDuration0109", IsFromSpell = false, AsChild = true, DurationValue = durationValue };
            Set(duration, "m_Buff", Ref<BlueprintBuffReference>(f.Native<BlueprintBuff>(WeaponBondDurationBuffId)));
            var rank = Rank(paladin, "AsIs");
            rank.name = "IroranDivineBodyDurationRank0109";
            var cost = Cost(resource);
            cost.name = "IroranDivineBodyCost0109";
            var ability = f.Clone(f.Native<BlueprintAbility>(WeaponBondSwitchAbilityId), DivineBodyAbilityToken, "Divine Body", BondText);
            ability.NeedEquipWeapons = false;
            ability.CanTargetSelf = true;
            ability.CanTargetFriends = false;
            ability.CanTargetEnemies = false;
            ability.ComponentsArray = ability.ComponentsArray
                .Where(c => !(c is AbilityResourceLogic) && !(c is AbilityEffectRunAction) && !(c is ContextRankConfig))
                .Concat(new BlueprintComponent[] { cost, rank, Run(enchant, duration), new IroranDivineBodyCasterCheck0109 { name = "IroranDivineBodyCasterCheck0109" } })
                .ToArray();

            var feature = f.Feature(DivineBodyToken, "Divine Body", BondText,
                Facts(ability, f.Native<BlueprintUnitFact>(FlamingChoiceId), f.Native<BlueprintUnitFact>(KeenChoiceId)),
                Grant(resource),
                new IroranDivineBodyCleanup0109 { name = "IroranDivineBodyCleanup0109" });
            Set(feature, "m_Icon", ability.Icon);
            return feature;
        }

        static BlueprintFeature BuildDivineBodyExtraUse(PsychicAssets f, BlueprintFeature body)
        {
            var resource = (BlueprintAbilityResource)f.Assets[DivineBodyResourceToken];
            var extra = f.Clone(f.Native<BlueprintFeature>(WeaponBondAdditionalUseId), DivineBodyExtraUseToken, "Divine Body: Additional Use", BondText);
            var increase = extra.GetComponents<IncreaseResourceAmount>().ToArray();
            if (increase.Length != 1) throw new InvalidOperationException("0109 native bond additional use component changed.");
            Set(increase[0], "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
            Set(extra, "m_Icon", body.Icon);
            return extra;
        }

        static BlueprintFeature BuildAura(PsychicAssets f, IroranRerolls0109.AuraKind kind, string suffix, string token, string name, string text)
        {
            var ally = f.Buff("IroranAura" + suffix + "AllyBuff0109", name, text, new IroranRerolls0109.AuraComponent { name = "IroranAura" + suffix + "Rules0109", Kind = kind });
            var area = f.Clone(f.Native<BlueprintAbilityAreaEffect>(AuraAreaId), "IroranAura" + suffix + "Area0109", name, text);
            var applied = area.GetComponent<AbilityAreaEffectBuff>();
            if (applied == null) throw new InvalidOperationException("0109 native aura area component changed.");
            Set(applied, "m_Buff", Ref<BlueprintBuffReference>(ally));
            var carrier = f.Clone(f.Native<BlueprintBuff>(AuraCarrierId), "IroranAura" + suffix + "Carrier0109", name, text);
            var add = carrier.GetComponent<AddAreaEffect>();
            if (add == null) throw new InvalidOperationException("0109 native aura carrier component changed.");
            Set(add, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(area));
            var aura = new AuraFeatureComponent { name = "IroranAura" + suffix + "Feature0109" };
            Set(aura, "m_Buff", Ref<BlueprintBuffReference>(carrier));
            var feature = f.Feature(token, name, text, aura);
            Set(feature, "m_Icon", f.Native<BlueprintFeature>(AuraOfCourageFeatureId).Icon);
            return feature;
        }
    }
}
