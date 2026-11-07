using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;
using WotR.Testing.Offline;
using Xunit;

namespace CotW.Havocker.Tests {
    public sealed partial class BridgeTests {
        const string K09Prefix = "C:/Users/xuenc/agent-tools/havocker-flash/havocker-hard6-20261106-210102";
        static int K09StoneskinReduction(UnitEntityData target) {
            // Native RuleCalculateDamage.ApplyDifficultyForDamageReduction
            // runs after Stoneskin's actual DR10. Preserve that raw threshold;
            // account explicitly for the host's real enemy-stat difficulty.
            var setting = (Kingmaker.Settings.StatsAdjustmentsType)Kingmaker.Settings.SettingsRoot.Difficulty.StatsAdjustments;
            if (!target.IsPlayersEnemy) return 10;
            return setting == Kingmaker.Settings.StatsAdjustmentsType.ExtraDecline ? 0
                : setting == Kingmaker.Settings.StatsAdjustmentsType.StrongDecline || setting == Kingmaker.Settings.StatsAdjustmentsType.Decline ? 5 : 10;
        }
        void K09Gather(UnitEntityData unit) {
            unit.Progression.Features.AddFeature(game.Copy<BlueprintFeature>("71f526b1d4b50b94582b0b9cbe12b0e0"));
            unit.Get<UnitPartKineticist>().SetGatherPowerMode(KineticistGatherPowerMode.High);
            unit.Buffs.AddBuff(game.Copy<BlueprintFeature>(Burn).GetComponent<AddKineticistPart>().GatherPowerBuff3, unit, TimeSpan.FromSeconds(12), null);
            Assert.Equal(3, unit.Get<UnitPartKineticist>().GatherPowerRank);
        }
        [Theory]
        [InlineData("ArmorPiercingInfusion")]
        [InlineData("ChainArrowsInfusion")]
        [InlineData("ClusterArrowsInfusion")]
        [InlineData("ExplodingArrowsInfusion")]
        public void K09ArcherEntriesRetainNativeArchetypeQualificationWithoutGrantingArcher(string name) {
            game.RequireGame(); var x = SpellUnit(20);
            try {
                var bp = PrivateNamed<BlueprintFeature>(name);
                var required = Assert.Single(bp.GetComponents<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteArchetypeLevel>());
                Assert.Equal("e0684d0a6ef44cae9d095f5318618a85", required.Archetype.AssetGuid.ToString());
                Assert.False(required.Check(null, x.unit.Descriptor, null));
                Assert.DoesNotContain(x.unit.Progression.Classes.SelectMany(c => c.Archetypes), a => a == required.Archetype);
                Assert.False(x.unit.HasFact(bp));
                Assert.False((bool)x.unit.Get<UnitPartKineticist>().ArcherBlast);
                var blast = new AbilityData(PrivateNamed<BlueprintAbility>("EarthBlastAbility"), x.unit.Descriptor);
                Assert.False(AbilityKineticist.IsArcherBlast(blast));
                game.Observations["K09-Archer-qualification-" + name] = "real retained native Kinetic Archer prerequisite=" + required.Archetype.AssetGuid + "; pure Havocker20 rejected; no Archer/composite/extra-element grant; presence is not effect PASS";
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void K09ArmorPiercingNativeDamageBypassesActualStoneskinOnlyWithBuff(bool piercing) {
            game.RequireGame(); var x = SpellUnit(16); var effects = new EffectObserver(); var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(effects)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 16); x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthBlastFeature"));
                    // Forced component-effect fixture only. The normal Archer
                    // prerequisite remains false (separate test), no feat grant.
                    var buff = piercing ? x.unit.Buffs.AddBuff(PrivateNamed<BlueprintBuff>("ArmorPiercingInfusionBuff"), x.unit, TimeSpan.FromMinutes(1), null) : null;
                    try {
                        var target = HostileTarget(x.unit); target.Buffs.AddBuff(BridgeGame.Native<BlueprintBuff>("37a956d0e7a84ab0bb66baf784767047"), target, TimeSpan.FromMinutes(1), new AbilityParams { CasterLevel = 10 });
                        var data = new AbilityData(PrivateNamed<BlueprintAbility>("EarthBlastAbility"), x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>("EarthBlastBase")).Data };
                        var cast = Rulebook.Trigger(new RuleCastSpell(data, target)); Assert.True(cast.Success); AbilityExecutionProcess.PrepareCast(cast.Context); Pay(data, cast.Context);
                        GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                        var damage = Assert.Single(effects.Damage, d => d.Target == target && d.Result > 0);
                        var physical = Assert.Single(damage.DamageBundle.OfType<Kingmaker.RuleSystem.Rules.Damage.PhysicalDamage>());
                        Assert.Equal(10, (int)physical.ReductionBecauseResistance);
                        Assert.Equal(piercing, damage.IgnoreDamageReduction); Assert.Equal(piercing ? 0 : K09StoneskinReduction(target), damage.DamageWithoutReduction - damage.Result);
                        Assert.Equal(1, x.state.NativeAcceptEvents);
                        game.Observations["K09-effect-ArmorPiercing-" + piercing] = "forced native buff-effect fixture, no Archer grant/qualification claim; real Earth damage against Stoneskin, IgnoreDR=" + damage.IgnoreDamageReduction + "; reduction=" + (damage.DamageWithoutReduction - damage.Result);
                    } finally { buff?.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }

        [Fact]
        public void K09ClusterArrowsNativeDamageAppliesTwoAcPenaltiesAndOutOfCombatRoundClearsThem() {
            game.RequireGame(); var x = SpellUnit(10); var effects = new EffectObserver(); var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(effects)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 10); var root = PrivateNamed<BlueprintAbility>("MetalBlastBase"); x.unit.AddFact(root);
                    var buff = x.unit.Buffs.AddBuff(PrivateNamed<BlueprintBuff>("ClusterArrowsInfusionMainBuff"), x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        K09Gather(x.unit); var charge = Conversion(x.unit, Prepare(x.book, 4), "c054087f586eda15b7bfdbdbfd980811"); Pay(charge, Context(charge));
                        var data = new AbilityData(PrivateNamed<BlueprintAbility>("MetalBlastAbility"), x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(root).Data };
                        var target = HostileTarget(x.unit); int ac = target.Stats.AC.ModifiedValue;
                        var cast = Rulebook.Trigger(new RuleCastSpell(data, target)); Assert.True(cast.Success); AbilityExecutionProcess.PrepareCast(cast.Context); Pay(data, cast.Context);
                        GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                        Assert.Contains(effects.Damage, d => d.Target == target && d.Result > 0);
                        var payload = PrivateNamed<BlueprintBuff>("ClusterArrowsInfusionBuff"); var applied = target.Buffs.GetBuff(payload); Assert.NotNull(applied);
                        Assert.Equal(ac - 2, target.Stats.AC.ModifiedValue);
                        Assert.False(Kingmaker.Game.Instance.Player.IsInCombat);
                        foreach (var p in target.Buffs.Enumerable.Where(b => b.Blueprint == payload).ToArray()) p.TickMechanics();
                        Assert.False(target.Buffs.HasFact(payload)); Assert.Equal(ac, target.Stats.AC.ModifiedValue);
                        Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                        game.Observations["K09-effect-ClusterArrows"] = "forced native buff + SC16 Metal fixture only; actual damage triggers repeated permanent payloads -> AC-2; native out-of-combat round clears and restores AC; no Archer/composite entitlement";
                    } finally { buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void K09PushingDamageRunsNativeBullRushAndQueuesOnlySuccessfulForceMove(bool strongDefense) {
            game.RequireGame(); var x = SpellUnit(6); var effects = new EffectObserver(); var maneuvers = new K08ManeuverObserver();
            var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(effects)) using (EventBus.Subscribe(maneuvers)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 6); x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthBlastFeature"));
                    var buff = x.unit.Buffs.AddBuff(PrivateNamed<BlueprintBuff>("PushingInfusionBuff"), x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        x.unit.Stats.BaseAttackBonus.BaseValue = 5;
                        var target = HostileTarget(x.unit); target.Stats.BaseAttackBonus.BaseValue = strongDefense ? 100 : -100;
                        BridgeGame.Set(target, "m_Position", x.unit.Position + new UnityEngine.Vector3(1, 0, 0));
                        var origin = target.Position;
                        var data = new AbilityData(PrivateNamed<BlueprintAbility>("EarthBlastAbility"), x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>("EarthBlastBase")).Data };
                        var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge));
                        var cast = Rulebook.Trigger(new RuleCastSpell(data, target)); Assert.True(cast.Success);
                        AbilityExecutionProcess.PrepareCast(cast.Context); Pay(data, cast.Context);
                        GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                        Assert.Contains(effects.Damage, d => d.Target == target && d.Result > 0);
                        var check = Assert.Single(maneuvers.Checks, m => m.Target == target);
                        Assert.Equal(CombatManeuver.BullRush, check.Type); Assert.Equal(StatType.Intelligence, check.ReplaceBaseStat); Assert.Equal(!strongDefense, check.Success);
                        var move = target.Get<Kingmaker.UnitLogic.Parts.UnitPartForceMove>();
                        if (strongDefense) { Assert.Null(move); Assert.False(target.State.HasCondition(UnitCondition.ForceMove)); }
                        else {
                            Assert.NotNull(move); var chunk = Assert.Single(move.Chunks);
                            Assert.Equal(new UnityEngine.Vector3(1, 0, 0), chunk.Dir); Assert.Equal(10f, chunk.Speed);
                            Assert.Equal((5 + check.SuccessMargin) * .3048f, chunk.MaxDistance.Value, 3);
                            Assert.True(target.State.HasCondition(UnitCondition.ForceMove));
                            move.StopMove(chunk); Assert.Null(target.Get<Kingmaker.UnitLogic.Parts.UnitPartForceMove>()); Assert.False(target.State.HasCondition(UnitCondition.ForceMove));
                        }
                        Assert.Equal(origin, target.Position); // movement scheduler deliberately not simulated
                        Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                        game.Observations["K09-effect-Pushing-" + strongDefense] = "real damage -> Int BullRush; success=" + check.Success + "; native ForceMove chunk direction/speed/distance and cleanup asserted; no navmesh/movement-controller claim";
                    } finally { buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void K09GrapplingNativeWallDamageSaveConditionsAndExpiry(bool strongSave) {
            game.RequireGame(); var x = SpellUnit(16); var effects = new EffectObserver(); var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(effects)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 16); var root = PrivateNamed<BlueprintAbility>("EarthBlastBase"); x.unit.AddFact(root);
                    var bp = PrivateNamed<BlueprintAbility>("WallEarthBlastAbility"); x.unit.AddFact(bp.GetComponent<AbilityShowIfCasterHasFact>().UnitFact);
                    var buff = x.unit.Buffs.AddBuff(PrivateNamed<BlueprintBuff>("GrapplingInfusionBuff"), x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        K09Gather(x.unit);
                        var charge = Conversion(x.unit, Prepare(x.book, 4), "c054087f586eda15b7bfdbdbfd980811"); Pay(charge, Context(charge));
                        var data = new AbilityData(bp, x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(root).Data };
                        var target = HostileTarget(x.unit); target.Stats.SaveReflex.BaseValue = strongSave ? 100 : -100;
                        BridgeGame.Set(target, "m_Position", new UnityEngine.Vector3(2, 0, 0));
                        var cast = Rulebook.Trigger(new RuleCastSpell(data, new TargetWrapper(UnityEngine.Vector3.zero))); Assert.True(cast.Success);
                        AbilityExecutionProcess.PrepareCast(cast.Context); Pay(data, cast.Context);
                        var spawn = Assert.Single(bp.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionSpawnAreaEffect>());
                        var duration = spawn.DurationValue.Calculate(cast.Context).Seconds;
                        var identity = (Kingmaker.View.MapObjects.AreaEffectView)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Kingmaker.View.MapObjects.AreaEffectView)); identity.UniqueId = Guid.NewGuid().ToString("N");
                        var area = new AreaEffectEntityData(identity, cast.Context.CloneFor(spawn.AreaEffect, x.unit.Descriptor), spawn.AreaEffect, target, Kingmaker.Game.Instance.TimeController.GameTime, duration, false);
                        try {
                            GameRandom.Init(62007); spawn.AreaEffect.HandleUnitEnter(area.Context, area, target);
                            Assert.Contains(effects.Damage, d => d.Target == target && d.Result > 0);
                            var save = Assert.Single(effects.Saves, s => s.Initiator == target);
                            Assert.Equal(Kingmaker.EntitySystem.Stats.SavingThrowType.Reflex, save.Type); Assert.Equal(strongSave, save.IsPassed); Assert.Equal(23, save.DifficultyClass);
                            var payload = PrivateNamed<BlueprintBuff>("GrapplingInfusionEffectBuff"); var applied = target.Buffs.GetBuff(payload);
                            if (strongSave) { Assert.Null(applied); Assert.False(target.State.HasCondition(UnitCondition.Entangled)); Assert.False(target.State.HasCondition(UnitCondition.CantMove)); }
                            else {
                                Assert.NotNull(applied); Assert.Equal(TimeSpan.FromMinutes(1), applied.TimeLeft);
                                Assert.True(target.State.HasCondition(UnitCondition.Entangled)); Assert.True(target.State.HasCondition(UnitCondition.CantMove));
                                target.Buffs.TickBuff(applied, applied.EndTime);
                                Assert.False(target.Buffs.HasFact(payload)); Assert.False(target.State.HasCondition(UnitCondition.Entangled)); Assert.False(target.State.HasCondition(UnitCondition.CantMove));
                            }
                            Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                            game.Observations["K09-effect-Grappling-" + strongSave] = "real native Wall area enter damage -> Reflex DC" + save.DifficultyClass + "; passed=" + save.IsPassed + "; authored Entangled/CantMove and minute expiry; spatial selection/area scheduler/break-free not claimed";
                        } finally { area.Dispose(); }
                    } finally { buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void K09RareMetalNativeConditionalDamageChangesRealMaterialMask(bool rare) {
            game.RequireGame(); var x = SpellUnit(10); var effects = new EffectObserver(); var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(effects)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 10); var root = PrivateNamed<BlueprintAbility>("MetalBlastBase"); x.unit.AddFact(root); // SC16 explicit composite fee fixture, not a legal extra-element grant
                    var buff = rare ? x.unit.Buffs.AddBuff(PrivateNamed<BlueprintBuff>("RareMetalInfusionBuff"), x.unit, TimeSpan.FromMinutes(1), null) : null;
                    try {
                        K09Gather(x.unit); var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge));
                        var data = new AbilityData(PrivateNamed<BlueprintAbility>("MetalBlastAbility"), x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(root).Data };
                        var target = HostileTarget(x.unit);
                        target.Buffs.AddBuff(BridgeGame.Native<BlueprintBuff>("37a956d0e7a84ab0bb66baf784767047"), target, TimeSpan.FromMinutes(1), new AbilityParams { CasterLevel = 10 });
                        var cast = Rulebook.Trigger(new RuleCastSpell(data, target)); Assert.True(cast.Success);
                        AbilityExecutionProcess.PrepareCast(cast.Context); Pay(data, cast.Context);
                        GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                        var damage = Assert.Single(effects.Damage, d => d.Target == target && d.Result > 0);
                        var physical = Assert.Single(damage.DamageBundle.OfType<Kingmaker.RuleSystem.Rules.Damage.PhysicalDamage>());
                        Assert.Equal(rare ? -1 : 0, (int)physical.MaterialsMask);
                        Assert.Equal(rare ? 0 : 10, (int)physical.ReductionBecauseResistance);
                        Assert.Equal(rare ? 0 : K09StoneskinReduction(target), damage.DamageWithoutReduction - damage.Result);
                        Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(rare ? HavockerChargeStatus0020.Consumed : HavockerChargeStatus0020.Armed, x.state.Status);
                        game.Observations["K09-effect-RareMetal-" + rare] = "explicit SC16 Metal fee fixture; native condition selects actual physical damage material mask=" + (int)physical.MaterialsMask + "; no composite entitlement or DR-target matrix claim";
                    } finally { buff?.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }

        [Fact]
        public void K09WhirlwindCleanupBeforeFirstAttackInvalidatesCommittedReceipt() {
            game.RequireGame(); var x = SpellUnit(10); var casts = new CastObserver();
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(casts)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 10);
                    x.unit.Progression.Features.AddFeature(BridgeGame.Native<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>("45c746aef0ef78c14914a054cbc68aa2"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthBlastFeature"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthKineticBladeFeature"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("BladeWhirlwindInfusion"));
                    var buff = x.unit.Buffs.AddBuff(PrivateNamed<BlueprintActivatableAbility>("KineticBladeEarthBlastAbility").Buff, x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        var blade = x.unit.Body.PrimaryHand.MaybeWeapon.Blueprint.GetComponent<WeaponKineticBlade>(); Assert.NotNull(blade);
                        var data = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>("BladeWhirlwindAbility")).Data;
                        var command = new UnitUseAbility(data, x.unit); BridgeGame.Set(command, "<Executor>k__BackingField", x.unit); BridgeGame.Set(command, "<IsStarted>k__BackingField", true);
                        x.unit.Commands.Raw[(int)UnitCommand.CommandType.Standard] = command;
                        K09Gather(x.unit); var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge));
                        var cast = Rulebook.Trigger(new RuleCastSpell(data, x.unit)); Assert.True(cast.Success); AbilityExecutionProcess.PrepareCast(cast.Context); Pay(data, cast.Context);
                        Assert.Equal(1, x.state.NativeAcceptEvents);
                        var api = typeof(ModEntry).Assembly.GetType("CallOfTheWild.Wrath.Prototype.HavockerBlade0020", true);
                        BridgeGame.Call(api, "Clear", x.unit.Descriptor);
                        using (new K08GeometryInput(x.unit)) AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, HostileTarget(x.unit));
                        Assert.DoesNotContain(casts.Casts, c => c.Spell.Blueprint == blade.Blast); Assert.Equal(1, x.state.NativeAcceptEvents);
                        game.Observations["K09-whirlwind-cleanup-before-first"] = "native committed receipt is bound at payment, cleanup before first authored melee action cannot adopt a new ownership lifetime";
                    } finally { buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
            }
        }
        [Fact]
        public void K09WhirlwindPaidParentSharesOneNativeReceiptAcrossAuthoredMeleeActions() {
            game.RequireGame(); var x = SpellUnit(10); var casts = new CastObserver(); var attacks = new K08AttackObserver(); var effects = new EffectObserver(); var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(casts)) using (EventBus.Subscribe(attacks)) using (EventBus.Subscribe(effects)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 10);
                    x.unit.Progression.Features.AddFeature(BridgeGame.Native<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>("45c746aef0ef78c14914a054cbc68aa2"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthBlastFeature"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthKineticBladeFeature"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("BladeWhirlwindInfusion")); x.unit.Stats.BaseAttackBonus.BaseValue = 100;
                    var buff = x.unit.Buffs.AddBuff(PrivateNamed<BlueprintActivatableAbility>("KineticBladeEarthBlastAbility").Buff, x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        var weapon = x.unit.Body.PrimaryHand.MaybeWeapon; var blade = weapon.Blueprint.GetComponent<WeaponKineticBlade>(); Assert.NotNull(blade);
                        var data = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>("BladeWhirlwindAbility")).Data;
                        var targets = new[] { HostileTarget(x.unit), HostileTarget(x.unit) }; BridgeGame.Set(targets[1], "m_Position", new UnityEngine.Vector3(1, 0, 0));
                        var command = new UnitUseAbility(data, x.unit); BridgeGame.Set(command, "<Executor>k__BackingField", x.unit); BridgeGame.Set(command, "<IsStarted>k__BackingField", true);
                        x.unit.Commands.Raw[(int)UnitCommand.CommandType.Standard] = command;
                        var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge)); long id = x.state.ChargeId;
                        var cost = data.Blueprint.GetComponent<AbilityKineticist>(); Assert.Equal(3, cost.BlastBurnCost); Assert.True(cost.CalculateBurnCost(data).Total > 0);
                        Assert.False(Rulebook.Trigger(new RuleCastSpell(data, x.unit)).Success); Assert.Equal(0, x.state.NativeAcceptEvents); Assert.Equal(id, x.state.ChargeId);
                        K09Gather(x.unit); Assert.Equal(0, cost.CalculateBurnCost(data).Total);
                        var cast = Rulebook.Trigger(new RuleCastSpell(data, x.unit)); Assert.True(cast.Success); AbilityExecutionProcess.PrepareCast(cast.Context);
                        // A real cast without a resource commit must not borrow
                        // another activation or obtain any kinetic child.
                        using (new K08GeometryInput(x.unit)) {
                            GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, targets[0]);
                            Assert.DoesNotContain(casts.Casts, c => c.Spell.Blueprint == blade.Blast); Assert.Equal(0, x.state.NativeAcceptEvents);
                            Pay(data, cast.Context); Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                            var next = Conversion(x.unit, Prepare(x.book, 2)); Pay(next, Context(next)); long nextId = x.state.ChargeId;
                            foreach (var target in targets) { GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target); }
                            var children = casts.Casts.Where(c => c.Spell.Blueprint == blade.Blast).ToArray(); Assert.Equal(2, children.Length); Assert.All(children, c => Assert.True(c.Success));
                            foreach (var child in children) { Pay(child.Spell, child.Context); Pay(child.Spell, child.Context); AbilityExecutionProcess.PrepareCast(child.Context); AbilityExecutionProcess.ApplyEffectImmediate(child.Context, child.SpellTarget.Unit); }
                            Assert.All(targets, t => Assert.Contains(effects.Damage, d => d.Target == t && d.Result > 0));
                            Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(nextId, x.state.ChargeId); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                            var other = new AbilityExecutionContext(data, AbilityParams.Empty, x.unit);
                            AbilityExecutionProcess.ApplyEffectImmediate(other, targets[0]);
                            var replacement = new UnitUseAbility(data, x.unit); BridgeGame.Set(replacement, "<Executor>k__BackingField", x.unit); BridgeGame.Set(replacement, "<IsStarted>k__BackingField", true);
                            x.unit.Commands.Raw[(int)UnitCommand.CommandType.Standard] = replacement; AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, targets[0]);
                            x.unit.Commands.Raw[(int)UnitCommand.CommandType.Standard] = command;
                            BridgeGame.Set(command, "<IsFinished>k__BackingField", true); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, targets[0]);
                            BridgeGame.Set(command, "<IsFinished>k__BackingField", false);
                            var api = typeof(ModEntry).Assembly.GetType("CallOfTheWild.Wrath.Prototype.HavockerBlade0020", true);
                            BridgeGame.Call(api, "Clear", x.unit.Descriptor); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, targets[0]);
                            Assert.Equal(2, casts.Casts.Count(c => c.Spell.Blueprint == blade.Blast)); Assert.Equal(1, x.state.NativeAcceptEvents);
                            game.Observations["K09-effect-BladeWhirlwind"] = "authored B3/I2, positive final refused; uncommitted context no child; native Spend once -> authored melee actions on two explicit recipients -> two paid child casts and real damage; later charge/replayed child/wrong context/ended command protected; radius/LOS selector and scene scheduler not claimed";
                        }
                    } finally { buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }
        [Fact]
        public void K09CaptureRemainingEightAuthoredDefinitionsWithoutGrantingArcher() {
            game.RequireGame();
            var map = (IDictionary)game.Api.GetProperty("Copies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game.Bridge);
            var authored = game.Api.GetMethod("DefinitionValue", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(object) }, null);
            var names = new[] { "BladeWhirlwind", "Grappling", "Pushing", "RareMetal", "ArmorPiercing", "ChainArrows", "ClusterArrows", "ExplodingArrows", "MetalBlast" };
            var audit = new List<object>();
            foreach (BlueprintGuid key in map.Keys) {
                var native = BridgeGame.Native<BlueprintScriptableObject>(key.ToString());
                if (!names.Any(n => native.name.Contains(n))) continue;
                var bp = (BlueprintScriptableObject)map[key];
                audit.Add(new { nativeId = key.ToString(), name = native.name, privateId = bp.AssetGuid.ToString(),
                    definition = K07JsonLeaves(authored.Invoke(null, new object[] { bp.ComponentsArray })) });
            }
            System.IO.File.WriteAllText(K09Prefix + "-remaining-definitions.json", Newtonsoft.Json.JsonConvert.SerializeObject(audit, Newtonsoft.Json.Formatting.Indented), new System.Text.UTF8Encoding(false));
            Assert.Contains(map.Keys.Cast<BlueprintGuid>().Select(k => BridgeGame.Native<BlueprintScriptableObject>(k.ToString()).name), n => n == "BladeWhirlwindAbility");
            game.Observations["K09-source-capture"] = "bounded remaining-eight/Metal nodes=" + audit.Count + "; authored evidence only, no effect PASS or Archer grants";
        }
    }
}
