using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099Evocation
    {
        internal const string School = "Evocation";
        private const string BrilliantEnergyBuff = "50d0501ad05f15d498c3aa8d602af273";
        private const string FaerieFireBuff = "cc383a9eaae4d2b45a925d442b367b54";
        private const string MageLight = "95f206566c5261c42aa5b3e7e0d1e36c";
        private const string MageLightBuff = "571baa4cf65bbcb4996fe429ca77d1a5";
        private const string BlindnessBuff = "187f88d96a0ef464280706b63635f2af";
        private const string Blindness = "46fd02ad56c35224c9c91c88cd457791";
        private const string FireDomainRay = "4ecdf240d81533f47a5279f5075296b9";
        private const string GatherPower = "104a9f275539abf44b594e9e36f71694";
        // The native plasma blade enchantment is the visual donor the original
        // dazzling blade FX used. Dazzling Blade already clones it in this repo.
        private const string GlowDonor = "6cbb732b9d638724a960d784634dcdcf";
        private const string Snowball = "9f10909f0be1f5141bf1c102041f93d9";
        private const string Fireball = "2d81362af43aeac4387a3d4fced489c3";

        private static readonly string[] RayDonors =
        {
            "435222be97067a447b2b40d3c58a058e", "7ef096fdc8394e149a9e8dced7576fee",
            "96ca3143601d6b242802655336620d91", "cdb106d53c65bbc4086183d54c3b97c7"
        };
        private static readonly string[] WallAreas =
        {
            "2a9cebe780b6130428f3bf4b18270021", "608d84e25f42d6044ba9b96d9f60722a",
            "2175d68215aa61644ad1d877d4915ece", "ac8737ccddaf2f948adf796b5e74eee7"
        };
        private static readonly string[] WallIcons =
        {
            "1e418794638cf95409f6e33c8c3dbe1a", "e377feb2ecec95e478e0565da621ea55",
            "8ba05ef69b06ea04c9430427a95685f6", "77d255c06e4c6a745b807400793cf7b1"
        };
        private static readonly string[] Names = { "Acid", "Cold", "Electricity", "Fire" };
        private static readonly DamageEnergyType[] Energies =
            { DamageEnergyType.Acid, DamageEnergyType.Cold, DamageEnergyType.Electricity, DamageEnergyType.Fire };
        private static readonly SpellDescriptor[] Descriptors =
            { SpellDescriptor.Acid, SpellDescriptor.Cold, SpellDescriptor.Electricity, SpellDescriptor.Fire };

        internal static readonly string[] Donors = RayDonors.Concat(WallAreas).Concat(WallIcons).Concat(new[]
            { BrilliantEnergyBuff, FaerieFireBuff, MageLight, MageLightBuff, BlindnessBuff, Blindness,
              FireDomainRay, GatherPower, GlowDonor, Snowball, Fireball }).ToArray();
        internal static readonly string[] Tokens =
        {
            "EvocationEffectiveLevel", "EvocationDCLevel", "IntenseFocusValue", "IntenseFocusBuff",
            "EnergyRayAbility", "EnergyBlastAbility", "EnergyBlastPower", "RadianceGlowEnchantment",
            "RadianceBuff", "RadianceEffectBuff", "RadianceAbility", "RadiancePower",
            "LightMatrixBuff", "LightMatrixAbility", "LightMatrixReleaseAbility", "LightMatrixPower",
            "WallOfPowerAbility", "WallOfPowerPower", "EvocationInvestFocusAbility",
            "EvocationReplenishFocusAbility", "EvocationImplementFocus"
        };
        internal static IEnumerable<string> VariantTokens()
            => Enumerable.Range(0, 4).SelectMany(i => new[]
                { "EnergyRay-" + Names[i], "EnergyBlast-" + Names[i], "WallOfPower-" + Names[i], "WallOfPowerArea-" + Names[i] });

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache, Func<string, string, string, LocalizedString> text)
        {
            var rays = RayDonors.Select(id => Occultist0099Illusion.Load<BlueprintAbility>(cache, id)).ToArray();
            var walls = WallAreas.Select(id => Occultist0099Illusion.Load<BlueprintAbilityAreaEffect>(cache, id)).ToArray();
            var wallIcons = WallIcons.Select(id => Occultist0099Illusion.Load<BlueprintAbility>(cache, id)).ToArray();
            var brilliant = Occultist0099Illusion.Load<BlueprintBuff>(cache, BrilliantEnergyBuff);
            var faerie = Occultist0099Illusion.Load<BlueprintBuff>(cache, FaerieFireBuff);
            var mageLight = Occultist0099Illusion.Load<BlueprintAbility>(cache, MageLight);
            var mageLightBuff = Occultist0099Illusion.Load<BlueprintBuff>(cache, MageLightBuff);
            var blindnessBuff = Occultist0099Illusion.Load<BlueprintBuff>(cache, BlindnessBuff);
            var blindness = Occultist0099Illusion.Load<BlueprintAbility>(cache, Blindness);
            var fireRay = Occultist0099Illusion.Load<BlueprintAbility>(cache, FireDomainRay);
            var gather = Occultist0099Illusion.Load<BlueprintActivatableAbility>(cache, GatherPower);
            var glowDonor = Occultist0099Illusion.Load<BlueprintWeaponEnchantment>(cache, GlowDonor);
            var snowball = Occultist0099Illusion.Load<BlueprintAbility>(cache, Snowball);
            var fireball = Occultist0099Illusion.Load<BlueprintAbility>(cache, Fireball);
            var projectiles = rays.Select(ray => ray?.GetComponent<AbilityDeliverProjectile>()).ToArray();
            var blindnessRun = blindness?.GetComponent<AbilityEffectRunAction>();
            var blindnessApply = blindnessRun == null ? null : SpellBatch01447
                .Elements(new[] { blindnessRun }, new HashSet<object>(IdentityComparer))
                .OfType<ContextActionApplyBuff>().FirstOrDefault(action => action.Buff == blindnessBuff);
            if (rays.Any(ray => ray == null || ray.School != SpellSchool.Evocation) ||
                projectiles.Any(projectile => projectile == null) ||
                walls.Any(wall => wall == null || wall.Shape != AreaEffectShape.Wall ||
                    wall.GetComponent<AbilityAreaEffectRunAction>() == null) ||
                wallIcons.Any(icon => icon == null) || brilliant == null || faerie == null ||
                mageLight?.name != "MageLight" || mageLightBuff == null || blindnessBuff == null || blindnessApply == null ||
                fireRay?.GetComponent<AbilityDeliverProjectile>() == null || gather == null || glowDonor == null ||
                snowball == null || fireball == null) return null;

            var level = Occultist0099SchoolValue.Create("EvocationEffectiveLevel", School, Occultist0099SchoolValueKind.EffectiveLevel);
            var dc = Occultist0099SchoolValue.Create("EvocationDCLevel", School, Occultist0099SchoolValueKind.DCLevel);

            const string intenseText = "Your damaging Evocation spells and Evocation focus powers deal 1 additional point of damage of the same type per creature for every 2 points of mental focus locked in Evocation, up to 1 + 1 for every 2 occultist levels.";
            var intenseValue = Occultist0099SchoolValue.Create("IntenseFocusValue", School, Occultist0099SchoolValueKind.Resonance);
            var intense = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                ComponentsArray = new BlueprintComponent[] { new Occultist0099IntenseFocus { name = "OccultistIntenseFocus" } } },
                "IntenseFocusBuff", "Intense Focus", intenseText, null, text);
            Set(intense, "m_Icon", Get(gather, "m_Icon"));
            SetEnum(intense, "m_Flags", "StayOnDeath");

            const string rayText = "Spend 1 point of Evocation focus to fire a ray of acid, cold, electricity or fire as a ranged touch attack. It deals 1d6 damage, plus 1d6 for every 2 occultist levels beyond 1st, up to 10d6 at 19th level.";
            var rayDice = new ContextRankConfig { name = "EnergyRayDice" };
            SetEnum(rayDice, "m_BaseValueType", "CasterLevel");
            SetEnum(rayDice, "m_Progression", "StartPlusDivStep");
            Set(rayDice, "m_StartLevel", 1);
            Set(rayDice, "m_StepLevel", 2);
            Set(rayDice, "m_UseMax", true);
            Set(rayDice, "m_Max", 10);
            var rayLeaves = new List<BlueprintAbility>();
            for (int i = 0; i < 4; i++)
            {
                var projectile = CopyData(projectiles[i]);
                Set(projectile, "m_Projectiles", new[] { ((BlueprintProjectileReference[])Get(projectiles[i], "m_Projectiles"))[0] });
                projectile.UseMaxProjectilesCount = false;
                var leaf = Power("EnergyRay-" + Names[i], "Energy Ray: " + Names[i], rayText, AbilityType.SpellLike,
                    AbilityRange.Close, rays[i], 1, Run(Damage(Energies[i], DiceType.D6, Rank(AbilityRankType.Default), 0, false)),
                    projectile, Descriptor(Descriptors[i]), CopyData(rayDice));
                Harmful(leaf);
                rayLeaves.Add(leaf);
            }
            var rayRoot = Occultist0099Illusion.Variants("EnergyRayAbility", "Energy Ray", rayText, rays[3], text,
                SpellSchool.Evocation, rayLeaves.ToArray());

            const string blastText = "Spend 2 points of Evocation focus to unleash a 20-foot burst of acid, cold, electricity or fire within medium range. It deals 1d4 damage per occultist level; each creature may attempt a Reflex save to halve the damage.";
            var blastLeaves = new List<BlueprintAbility>();
            for (int i = 0; i < 4; i++)
            {
                var area = new AbilityTargetsAround { name = "EnergyBlastBurst" };
                Set(area, "m_Radius", new Feet(20));
                SetEnum(area, "m_TargetType", "Any");
                var leaf = Power("EnergyBlast-" + Names[i], "Energy Blast: " + Names[i], blastText, AbilityType.SpellLike,
                    AbilityRange.Medium, i == 3 ? fireball : snowball, 2,
                    Save(SavingThrowType.Reflex, Damage(Energies[i], DiceType.D4, Rank(AbilityRankType.Default), 0, true)),
                    area, Descriptor(Descriptors[i]));
                Harmful(leaf);
                leaf.CanTargetPoint = leaf.CanTargetFriends = leaf.CanTargetEnemies = leaf.CanTargetSelf = true;
                blastLeaves.Add(leaf);
            }
            var blastRoot = Occultist0099Illusion.Variants("EnergyBlastAbility", "Energy Blast", blastText, fireball, text,
                SpellSchool.Evocation, blastLeaves.ToArray());
            blastRoot.CanTargetPoint = true;

            const string radianceText = "Spend 1 point of Evocation focus to make a melee weapon in your main hand glow for 1 minute per occultist level. When that weapon scores a critical hit, you may end the glow to outline the foe for 1d4 rounds: it cannot benefit from concealment or invisibility, and attacks against it gain a +2 circumstance bonus. The glow follows your current main-hand melee weapon.";
            // A private enchantment identity sharing the native visual prefab, so a
            // Dazzling Blade expiring first cannot extinguish this one.
            // A fresh enchantment carrying only the native visual prefab. Cloning the
            // donor blueprint would mutate the native asset's identity.
            var glow = Identify(new BlueprintWeaponEnchantment { WeaponFxPrefab = CopyData(glowDonor.WeaponFxPrefab),
                ComponentsArray = Array.Empty<BlueprintComponent>() }, "RadianceGlowEnchantment", "Radiance", "", null, text);
            Set(glow, "m_EnchantmentCost", 0);
            Set(glow, "m_Prefix", new LocalizedString());
            Set(glow, "m_Suffix", new LocalizedString());
            var radianceEffect = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                ComponentsArray = faerie.ComponentsArray.Concat(new BlueprintComponent[]
                    { new AttackBonusAgainstTarget { name = "RadianceOutline", Value = 2, CheckCaster = true, CheckCasterFriend = true } }).ToArray() },
                "RadianceEffectBuff", "Radiance Outline", radianceText, null, text);
            Set(radianceEffect, "m_Icon", Get(faerie, "m_Icon"));
            var outline = Apply(radianceEffect, 0);
            outline.DurationValue.DiceType = DiceType.D4;
            outline.DurationValue.DiceCountValue = 1;
            var radianceBuff = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                ComponentsArray = new BlueprintComponent[]
                {
                    new Occultist0099RadianceBuff { name = "OccultistRadianceGlow",
                        Enchantment = Ref<BlueprintWeaponEnchantmentReference>(glow),
                        Outline = Ref<BlueprintBuffReference>(radianceEffect) }
                } }, "RadianceBuff", "Radiance", radianceText, null, text);
            Set(radianceBuff, "m_Icon", Get(brilliant, "m_Icon"));
            var radiance = Power("RadianceAbility", "Radiance", radianceText, AbilityType.SpellLike, AbilityRange.Touch,
                fireball, 1, Run(Occultist0099Transmutation.Timed(radianceBuff, PropertyValue(level), DurationRate.Minutes, true)));
            Set(radiance, "m_Icon", Get(brilliant, "m_Icon"));
            radiance.CanTargetSelf = true;
            radiance.CanTargetFriends = false;
            radiance.CanTargetEnemies = false;
            radiance.NotOffensive = true;
            radiance.EffectOnAlly = AbilityEffectOnUnit.Helpful;

            const string matrixText = "Spend 1 point of Evocation focus to create an orb of light that lasts 1 hour per occultist level. As a move action you may direct it to make a ranged touch attack against a foe within close range; on a hit the target must succeed at a Fortitude save or be blinded for 1d4 rounds. Hit or miss, the orb then ends. Only one orb can exist at a time.";
            var releaseProjectile = CopyData(fireRay.GetComponent<AbilityDeliverProjectile>());
            var blindApply = CopyData(blindnessApply);
            blindApply.Permanent = false;
            blindApply.DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.D4,
                DiceCountValue = 1, BonusValue = 0 };
            var matrixBuff = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                ComponentsArray = mageLightBuff.ComponentsArray.ToArray() },
                "LightMatrixBuff", "Light Matrix", matrixText, mageLight, text);
            var release = Identify(new BlueprintAbility { Type = AbilityType.SpellLike, Range = AbilityRange.Close,
                ActionType = UnitCommand.CommandType.Move, CanTargetEnemies = true, EffectOnEnemy = AbilityEffectOnUnit.Harmful,
                ComponentsArray = new BlueprintComponent[]
                {
                    new SpellComponent { name = "OccultistEvocationRelease", School = SpellSchool.Evocation },
                    Descriptor(SpellDescriptor.Blindness | SpellDescriptor.SightBased),
                    releaseProjectile,
                    Save(SavingThrowType.Fortitude, Saved(Array.Empty<GameAction>(), new GameAction[] { blindApply })),
                    new AbilityExecuteActionOnCast { name = "LightMatrixDischarge",
                        Conditions = new ConditionsChecker { Conditions = Array.Empty<Condition>() },
                        Actions = Actions(RemoveBuff(matrixBuff, true)) },
                    new ContextCalculateAbilityParams { name = "OccultistEvocationReleaseParams", StatType = StatType.Intelligence,
                        ReplaceCasterLevel = true, CasterLevel = PropertyValue(level), ReplaceSpellLevel = true, SpellLevel = PropertyValue(dc) }
                } }, "LightMatrixReleaseAbility", "Light Matrix: Release", matrixText, mageLight, text);
            release.SpellResistance = true;
            matrixBuff.ComponentsArray = matrixBuff.ComponentsArray.Concat(new BlueprintComponent[] { Facts(release) }).ToArray();
            var matrix = Power("LightMatrixAbility", "Light Matrix", matrixText, AbilityType.SpellLike, AbilityRange.Personal,
                mageLight, 1, Run(Occultist0099Transmutation.Timed(matrixBuff, PropertyValue(level), DurationRate.Hours, true)));
            matrix.NotOffensive = true;
            matrix.EffectOnAlly = AbilityEffectOnUnit.Helpful;

            const string wallText = "Spend 1 point of Evocation focus to create a wall of acid, cold, electricity or fire up to 5 feet per occultist level long, 10 feet high and 1 foot thick. It does not block passage or line of sight, but deals 2d6 + 1 per occultist level to anyone passing through, and lasts 1 round per occultist level.";
            var wallLeaves = new List<BlueprintAbility>();
            var wallAreaAssets = new List<BlueprintAbilityAreaEffect>();
            for (int i = 0; i < 4; i++)
            {
                var wallDamage = Damage(Energies[i], DiceType.D6, 2, PropertyValue(level), true);
                // Fresh area reusing the native wall geometry and visual, not a clone
                // of the native blueprint.
                var area = Identify(new BlueprintAbilityAreaEffect { Shape = walls[i].Shape, Size = walls[i].Size,
                    Fx = CopyData(walls[i].Fx), AffectEnemies = true, AggroEnemies = true,
                    ComponentsArray = Array.Empty<BlueprintComponent>() },
                    "WallOfPowerArea-" + Names[i], "Wall of Power: " + Names[i], wallText, wallIcons[i], text);
                area.ComponentsArray = new BlueprintComponent[]
                {
                    new AbilityAreaEffectRunAction { name = "WallOfPowerDamage",
                        Round = Actions(wallDamage), UnitEnter = Actions(CopyData(wallDamage)) },
                    new ContextRankConfig { name = "WallOfPowerLevel" }
                };
                var areaRank = area.GetComponent<ContextRankConfig>();
                SetEnum(areaRank, "m_BaseValueType", "CasterLevel");
                area.SpellResistance = true;
                wallAreaAssets.Add(area);
                var spawn = new ContextActionSpawnAreaEffect { name = "WallOfPowerSpawn", OnUnit = false,
                    DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero,
                        DiceCountValue = 0, BonusValue = PropertyValue(level) } };
                Set(spawn, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(area));
                var leaf = Power("WallOfPower-" + Names[i], "Wall of Power: " + Names[i], wallText, AbilityType.SpellLike,
                    AbilityRange.Medium, wallIcons[i], 1, Run(spawn), Descriptor(Descriptors[i]));
                leaf.CanTargetPoint = leaf.CanTargetFriends = leaf.CanTargetEnemies = leaf.CanTargetSelf = true;
                leaf.EffectOnAlly = leaf.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                wallLeaves.Add(leaf);
            }
            var wallRoot = Occultist0099Illusion.Variants("WallOfPowerAbility", "Wall of Power", wallText, wallIcons[3], text,
                SpellSchool.Evocation, wallLeaves.ToArray());
            wallRoot.CanTargetPoint = true;

            var invest = Operation("EvocationInvestFocusAbility", "Invest Mental Focus: Evocation",
                "Invest 1 point of unallocated mental focus in Evocation before locking your focus.",
                Occultist0099FocusAction.Invest, UnitCommand.CommandType.Free, rays[3], text, School);
            var replenish = Operation("EvocationReplenishFocusAbility", "Replenish Mental Focus: Evocation",
                "Spend 1 unallocated point of mental focus to replenish a spent Evocation point.",
                Occultist0099FocusAction.Replenish, UnitCommand.CommandType.Free, rays[3], text, School);
            var implement = Identify(new BlueprintFeature { Ranks = 1, IsClassFeature = true, ComponentsArray = new BlueprintComponent[]
                { new Occultist0099ImplementSchool { name = "OccultistEvocationSchool", School = School }, Facts(rayRoot, invest, replenish) } },
                "EvocationImplementFocus", "Evocation Implement", "Gain Energy Ray and Intense Focus.", rays[3], text);
            return new BlueprintScriptableObject[] { level, dc, intenseValue, intense, rayRoot, blastRoot, glow,
                radianceEffect, radianceBuff, radiance, matrixBuff, matrix, release, wallRoot, invest, replenish, implement }
                .Concat(rayLeaves).Concat(blastLeaves).Concat(wallLeaves).Concat(wallAreaAssets).ToArray();

            BlueprintAbility Power(string token, string label, string description, AbilityType type, AbilityRange range,
                BlueprintAbility icon, int cost, params BlueprintComponent[] components)
                => Identify(Occultist0099Illusion.NewSchoolPower(SpellSchool.Evocation, type, range, cost, level, dc, components),
                    token, label, description, icon, text);
        }

        private static ContextValue Rank(AbilityRankType type) => new ContextValue { ValueType = ContextValueType.Rank, ValueRank = type };

        private static ContextActionDealDamage Damage(DamageEnergyType energy, DiceType dice, ContextValue count,
            ContextValue bonus, bool area)
            => new ContextActionDealDamage { DamageType = new DamageTypeDescription { Type = DamageType.Energy, Energy = energy },
                Value = new ContextDiceValue { DiceType = dice, DiceCountValue = count, BonusValue = bonus },
                IsAoE = area, HalfIfSaved = area };

        private static SpellDescriptorComponent Descriptor(SpellDescriptor descriptor)
            => new SpellDescriptorComponent { name = "OccultistEvocationDescriptors", Descriptor = descriptor };

        private static ContextActionRemoveBuff RemoveBuff(BlueprintBuff buff, bool toCaster)
        {
            var remove = new ContextActionRemoveBuff { name = "OccultistEvocationRemove", ToCaster = toCaster };
            Set(remove, "m_Buff", Ref<BlueprintBuffReference>(buff));
            return remove;
        }

        private static void Harmful(BlueprintAbility ability)
        {
            ability.CanTargetSelf = false;
            ability.CanTargetEnemies = true;
            ability.EffectOnEnemy = ability.EffectOnAlly = AbilityEffectOnUnit.Harmful;
            ability.SpellResistance = true;
        }
    }
}