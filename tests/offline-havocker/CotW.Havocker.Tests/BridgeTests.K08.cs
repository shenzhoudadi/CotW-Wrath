using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
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
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
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

namespace CotW.Havocker.Tests
{
    public sealed partial class BridgeTests
    {
        const string K08Prefix = "C:/Users/xuenc/agent-tools/havocker-flash/havocker-hard6-20261106-210102";
        sealed class K08GeometryInput : IDisposable {
            readonly UnitEntityData unit; readonly Kingmaker.View.EntityViewBase previous;
            internal K08GeometryInput(UnitEntityData value) {
                unit = value; previous = unit.View;
                // Managed scene input only. No engine object/view is created or
                // attached, and no Corpulence/size/range/targeting method patched.
                var view = (Kingmaker.View.UnitEntityView)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Kingmaker.View.UnitEntityView));
                BridgeGame.Set(view, "m_Data", unit); BridgeGame.Set(view, "m_Corpulence", .5f); BridgeGame.Set(view, "m_RacialPetScaleCoeff", 1f);
                BridgeGame.Set(view, "m_StartGetUpTime", Kingmaker.Game.Instance.Player.GameTime - TimeSpan.FromSeconds(1));
                BridgeGame.Set(unit, "<View>k__BackingField", view);
                Assert.Equal(.5f, view.Corpulence); Assert.False(view.IsGetUp);
            }
            public void Dispose() => BridgeGame.Set(unit, "<View>k__BackingField", previous);
        }
        static void K08VisibleSceneInput(UnitEntityData caster, UnitEntityData target, bool visible) {
            caster.CanBeInVisionRange.Add(target);
            var entryType = typeof(UnitSightCache).GetNestedType("CacheEntry", System.Reflection.BindingFlags.NonPublic);
            var entry = Activator.CreateInstance(entryType, new object[] { target.Position, visible, visible });
            ((System.Collections.IDictionary)BridgeGame.Field(caster.SightCache, "m_Cache"))[target.UniqueId] = entry;
            Assert.Equal(visible, caster.HasLOS(target)); // real cached-visibility query, not a replacement
        }
        [Fact]
        public void K08TttCleaveBindingRejectsImpostorAndLeavesForeignWeaponQueueAlone() {
            game.RequireGame();
            var actual = BridgeGame.Native<BlueprintAbility>("6447d104a2222c14d9c9b8a36e4eb242");
            var api = typeof(ModEntry).Assembly.GetType("CallOfTheWild.Wrath.Prototype.HavockerBlade0020", true);
            Assert.True((bool)BridgeGame.Call(api, "TttCleave", actual));
            var impostor = new BlueprintAbility { AssetGuid = actual.AssetGuid, ComponentsArray = actual.ComponentsArray };
            Assert.False((bool)BridgeGame.Call(api, "TttCleave", impostor));
            var ordinary = CleanUnit(6, true); Assert.Null(ordinary.Get<UnitPartKineticist>());
            var command = new UnitUseAbility(new AbilityData(actual, ordinary.Descriptor), ordinary);
            BridgeGame.Set(command, "<Executor>k__BackingField", ordinary);
            UnitCommands.CustomHandlerData? handler = null;
            new KineticistController().HandleUnitWantRunCommand(command, ref handler);
            Assert.False(handler.HasValue);
            game.Observations["K08-cleave-binding"] = "actual cached TTT source accepted; same-GUID/component impostor rejected; ordinary non-Havocker weapon command untouched";
        }
        sealed class K08AttackObserver : IGlobalRulebookHandler<RuleAttackWithWeapon>, IGlobalRulebookSubscriber {
            internal readonly List<RuleAttackWithWeapon> Attacks = new List<RuleAttackWithWeapon>();
            public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
            public void OnEventDidTrigger(RuleAttackWithWeapon evt) => Attacks.Add(evt);
        }
        sealed class K08DispelObserver : IGlobalRulebookHandler<RuleDispelMagic>, IGlobalRulebookSubscriber {
            internal readonly List<RuleDispelMagic> Checks = new List<RuleDispelMagic>();
            public void OnEventAboutToTrigger(RuleDispelMagic evt) { }
            public void OnEventDidTrigger(RuleDispelMagic evt) => Checks.Add(evt);
        }
        sealed class K08ManeuverObserver : IGlobalRulebookHandler<RuleCombatManeuver>, IGlobalRulebookSubscriber {
            internal readonly List<RuleCombatManeuver> Checks = new List<RuleCombatManeuver>();
            public void OnEventAboutToTrigger(RuleCombatManeuver evt) { }
            public void OnEventDidTrigger(RuleCombatManeuver evt) => Checks.Add(evt);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void K08BowlingNativeDamageRunsIntelligenceTripAndRespectsActualCmd(bool strongDefense) {
            game.RequireGame(); var x = SpellUnit(10, CleanUnit(10, true)); var effects = new EffectObserver(); var maneuvers = new K08ManeuverObserver();
            var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(effects)) using (EventBus.Subscribe(maneuvers)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 10); x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthBlastFeature"));
                    x.unit.Stats.BaseAttackBonus.BaseValue = 5;
                    var bp = PrivateNamed<BlueprintBuff>("BowlingInfusionBuff"); var buff = x.unit.Buffs.AddBuff(bp, x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        var target = HostileTarget(x.unit); target.Stats.BaseAttackBonus.BaseValue = strongDefense ? 100 : -100;
                        using (new K08GeometryInput(target)) {
                            var data = new AbilityData(PrivateNamed<BlueprintAbility>("EarthBlastAbility"), x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>("EarthBlastBase")).Data };
                            var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge));
                            var cast = Rulebook.Trigger(new RuleCastSpell(data, target)); Assert.True(cast.Success);
                            AbilityExecutionProcess.PrepareCast(cast.Context); Pay(data, cast.Context);
                            GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                            Assert.Contains(effects.Damage, d => d.Target == target && d.Result > 0);
                            var check = Assert.Single(maneuvers.Checks, m => m.Target == target);
                            Assert.Equal(Kingmaker.RuleSystem.Rules.CombatManeuver.Trip, check.Type); Assert.Equal(StatType.Intelligence, check.ReplaceBaseStat);
                            Assert.Equal(!strongDefense, check.Success); Assert.Equal(!strongDefense, target.State.Prone.ShouldBeActive);
                            Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                            game.Observations["K08-effect-Bowling-" + strongDefense] = "real earth damage -> native Trip rule, Int main stat, CMB=" + check.InitiatorCMB + "; CMD=" + check.TargetCMD + "; success=" + check.Success + "; native Prone request; managed geometry input only, no maneuver/roll replacement or standup animation claim";
                        }
                    } finally { buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }

        [Theory]
        [InlineData("Earth", "c6816ad80a3df9c4ea7d3b012b06bacd")]
        [InlineData("Fire", "3d8d3d6678b901444a07984294a1bc24")]
        public void K08NormalSingleElementGrantsDoNotGiveCompositeAbilitiesOrBladeActivations(string element, string focusId) {
            game.RequireGame(); var x = SpellUnit(1, CleanUnit(1, true));
            try {
                x.unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeatureSelection>("45c746aef0ef78c14914a054cbc68aa2"));
                // Apply the exact formal first-level grant row. This exercises
                // AddFacts/conditional grant components; it is not chargen UI.
                var focus = game.Copy<BlueprintProgression>(focusId);
                var state = new LevelUpState(x.unit, LevelUpState.CharBuildMode.LevelUp, false) { SelectedClass = BridgeGame.Native<BlueprintCharacterClass>("1b9873f1e7bfe5449bc84d03e9c8e3cc"), NextClassLevel = 1 };
                LevelUpHelper.AddFeaturesFromProgression(state, x.unit.Descriptor, Assert.Single(focus.LevelEntries).Features, focus, 1);
                x.unit.Progression.Classes.Single(c => c.CharacterClass == state.SelectedClass).Level = 16;
                state.NextClassLevel = 16;
                foreach (var child in focus.LevelEntries[0].Features.OfType<BlueprintProgression>()) LevelUpHelper.UpdateProgression(state, x.unit.Descriptor, child);
                GiveNativeGrowthRanks(x.unit, 16);
                x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("KineticBladeInfusion"));
                Assert.NotNull(x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>(element + "BlastBase")));
                foreach (string composite in new[] { "BlueFlame", "Magma", "Metal" }) {
                    Assert.Null(x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>(composite + "BlastBase")));
                    var weapon = PrivateNamed<BlueprintActivatableAbility>("KineticBlade" + composite + "BlastAbility").Buff.GetComponent<AddKineticistBlade>().Blade;
                    Assert.Null(x.unit.Abilities.GetAbility(weapon.GetComponent<WeaponKineticBlade>().ActivationAbility));
                }
                game.Observations["K08-normal-element-" + element] = "actual formal first-level source row + native AddFacts and blade infusion grant; base present, BlueFlame/Magma/Metal base+blade activation absent; no extra element or composite entitlement; chargen/controller combinations not claimed";
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void K08UnravelingNativeDamageDispelsActualSpellBuffButKeepsNonspellSource(bool highSourceLevel) {
            game.RequireGame(); var x = SpellUnit(16, CleanUnit(16, true)); var effects = new EffectObserver(); var dispels = new K08DispelObserver();
            var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(effects)) using (EventBus.Subscribe(dispels)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 16); x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("FireBlastFeature"));
                    var bp = PrivateNamed<BlueprintBuff>("UnravelingInfusionBuff"); var buff = x.unit.Buffs.AddBuff(bp, x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        var target = HostileTarget(x.unit);
                        var armor = BridgeGame.Native<BlueprintBuff>("a92acdf18049d784eaa8f2004f5d2304");
                        var spell = new AbilityData(BridgeGame.Native<BlueprintAbility>("9e1ad5d6f87d19e4d8883d63a6e35568"), target.Descriptor);
                        // Explicit native source-CL boundary input, no roll or
                        // dispel threshold overridden. High CL is not a build.
                        var source = new AbilityExecutionContext(spell, new AbilityParams { CasterLevel = highSourceLevel ? 50 : 1, DC = 11 }, target);
                        AbilityExecutionProcess.ApplyEffectImmediate(source, target);
                        var applied = target.Buffs.GetBuff(armor); Assert.NotNull(applied); Assert.True(applied.IsFromSpell);
                        Assert.Equal(highSourceLevel ? 50 : 1, applied.Context.Params.CasterLevel);
                        var nonspell = target.Buffs.AddBuff(PrivateNamed<BlueprintBuff>("MagneticInfusionEffectBuff"), target, TimeSpan.FromMinutes(1), null);
                        Assert.False(nonspell.IsFromSpell);
                        var data = new AbilityData(PrivateNamed<BlueprintAbility>("FireBlastAbility"), x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>("FireBlastBase")).Data };
                        var cast = Rulebook.Trigger(new RuleCastSpell(data, target)); Assert.True(cast.Success);
                        AbilityExecutionProcess.PrepareCast(cast.Context); Pay(data, cast.Context);
                        GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                        Assert.Contains(effects.Damage, d => d.Target == target && d.Result > 0);
                        var check = Assert.Single(dispels.Checks, c => c.Buff == applied);
                        Assert.Equal(16, check.CasterLevel); Assert.Equal(highSourceLevel ? 61 : 12, check.DC); Assert.Equal(!highSourceLevel, check.Success);
                        Assert.Equal(highSourceLevel, target.Buffs.HasFact(armor)); Assert.True(target.Buffs.HasFact(nonspell.Blueprint));
                        Assert.Equal(1, x.state.NativeAcceptEvents);
                        game.Observations["K08-effect-Unraveling-" + highSourceLevel] = "actual MageArmor effect creates spell Buff; native fire damage -> native dispel CL16 vs DC12/61, success=" + check.Success + "; nonspell payload untouched; no dispel RNG/threshold replacement; high source CL50 is a boundary input";
                        nonspell.Remove(); target.Buffs.GetBuff(armor)?.Remove();
                    } finally { buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }

        [Theory]
        [InlineData(false, 2)]
        [InlineData(true, 3)]
        public void K08NativeCleaveDeliveryKeepsOnePaidReceiptForItsOwnContinuation(bool greater, int count) {
            game.RequireGame(); var x = SpellUnit(6, CleanUnit(6, true)); var casts = new CastObserver(); var attacks = new K08AttackObserver(); var effects = new EffectObserver();
            var random = GameRandom.Save(); var memory = x.unit.Memory; var targets = new List<UnitEntityData>(); var geometries = new List<IDisposable>();
            var scene = new Kingmaker.EntitySystem.SceneEntitiesState("K08-cleave-input");
            var sceneUnits = new List<UnitEntityData>(); var oldGroups = new Dictionary<UnitEntityData, string>();
            var groups = new List<Kingmaker.UnitLogic.Groups.UnitGroup>();
            void Place(UnitEntityData unit) {
                oldGroups.Add(unit, unit.GroupId); unit.GroupId = "K08/" + unit.UniqueId;
                scene.AddEntityData(unit); unit.IsInGame = true; sceneUnits.Add(unit); groups.Add(unit.Group);
            }
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(casts)) using (EventBus.Subscribe(attacks)) using (EventBus.Subscribe(effects)) {
                try {
                    Place(x.unit); memory = x.unit.Memory; geometries.Add(new K08GeometryInput(x.unit));
                    GiveNativeGrowthRanks(x.unit, 6);
                    x.unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeatureSelection>("45c746aef0ef78c14914a054cbc68aa2"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthBlastFeature"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthKineticBladeFeature"));
                    x.unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeature>("d809b6c4ff2aaff4fa70d712a70f7d7b"));
                    if (greater) x.unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeature>("cc9c862ef2e03af4f89be5088851ea35"));
                    x.unit.Stats.BaseAttackBonus.BaseValue = 100; // scenario input; native rolls/hit logic unchanged
                    var toggle = PrivateNamed<BlueprintActivatableAbility>("KineticBladeEarthBlastAbility");
                    var buff = x.unit.Buffs.AddBuff(toggle.Buff, x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        var weapon = x.unit.Body.PrimaryHand.MaybeWeapon; var blade = weapon.Blueprint.GetComponent<WeaponKineticBlade>();
                        for (int i = 0; i < 3; i++) {
                            var target = HostileTarget(x.unit); BridgeGame.Set(target, "m_Position", new UnityEngine.Vector3(.4f + i * .1f, 0, 0));
                            Place(target); Assert.True(target.IsInGame); targets.Add(target); geometries.Add(new K08GeometryInput(target));
                            x.unit.Memory.Add(target);
                            K08VisibleSceneInput(x.unit, target, true);
                        }
                        var data = x.unit.Abilities.GetAbility(BridgeGame.Native<BlueprintAbility>("6447d104a2222c14d9c9b8a36e4eb242")).Data;
                        var command = new UnitUseAbility(data, targets[0]);
                        // Native UnitCommand.Init is exactly this identity
                        // assignment. UnitUseAbility.Init additionally needs
                        // absent view geometry in GetApproachDistance. Supply
                        // scheduler identity only; range/motion init is not tested.
                        BridgeGame.Set(command, "<Executor>k__BackingField", x.unit);
                        UnitCommands.CustomHandlerData? queued = null;
                        new KineticistController().HandleUnitWantRunCommand(command, ref queued);
                        Assert.True(queued.HasValue); var activateCommand = Assert.IsType<UnitUseAbility>(queued.Value.ExecuteBefore);
                        Assert.Same(blade.ActivationAbility, activateCommand.Ability.Blueprint);
                        UnitCommands.CustomHandlerData? existing = new UnitCommands.CustomHandlerData { TryReturnToQueue = true };
                        new KineticistController().HandleUnitWantRunCommand(command, ref existing);
                        Assert.True(existing.Value.TryReturnToQueue); Assert.Null(existing.Value.ExecuteBefore);
                        var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge));
                        var paid = Rulebook.Trigger(new RuleCastSpell(activateCommand.Ability, x.unit)); Assert.True(paid.Success); Pay(activateCommand.Ability, paid.Context);
                        Assert.Equal(1, x.state.NativeAcceptEvents);
                        BridgeGame.Set(command, "<IsStarted>k__BackingField", true);
                        x.unit.Commands.Raw[(int)UnitCommand.CommandType.Standard] = command;
                        var cast = Rulebook.Trigger(new RuleCastSpell(data, targets[0])); Assert.True(cast.Success);
                        var component = Assert.Single(data.Blueprint.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.Base.AbilityCustomLogic>());
                        Assert.Equal("TabletopTweaks.Core.NewActions.AbilityCustomCleaveTTT", component.GetType().FullName);
                        game.Observations["K08-cleave-input-" + greater] = "memory=" + x.unit.Memory.Enemies.Count + "; originalMemory=" + memory.Enemies.Count + "; targets=" + string.Join(",", targets.Select(t => t.IsInGame + "/" + t.State.IsConscious + "/" + x.unit.Group.IsEnemy(t)));
                        Assert.Equal(3, x.unit.Memory.Enemies.Count);
                        var delivery = component.Deliver(cast.Context, targets[0]);
                        int delivered = 0;
                        while (true) {
                            GameRandom.Init(62007);
                            if (!delivery.MoveNext()) break;
                            delivered++;
                        }
                        Assert.Equal(count, delivered);
                        Assert.Equal(count, attacks.Attacks.Count);
                        Assert.False(attacks.Attacks[0].IsFirstAttack); // actual TTT0.7.15 flags, not rewritten
                        Assert.All(attacks.Attacks.Skip(1), a => { Assert.True(a.IsFirstAttack); Assert.False(a.IsFullAttack); });
                        var children = casts.Casts.Where(c => c.Spell.Blueprint == blade.Blast).ToArray();
                        Assert.Equal(count, children.Length); // red on K07: native second/third cleave attacks have no child
                        Assert.All(children, c => Assert.True(c.Success));
                        var next = Conversion(x.unit, Prepare(x.book, 2)); Pay(next, Context(next)); long nextId = x.state.ChargeId;
                        foreach (var child in children) {
                            Pay(child.Spell, child.Context); Pay(child.Spell, child.Context);
                            AbilityExecutionProcess.PrepareCast(child.Context); AbilityExecutionProcess.ApplyEffectImmediate(child.Context, child.SpellTarget.Unit);
                        }
                        Assert.Equal(count, effects.Damage.Count(d => targets.Contains(d.Target) && d.Result > 0));
                        Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(nextId, x.state.ChargeId); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                        // Same live command is insufficient: unrelated contexts,
                        // context-free followups and ended commands must not inherit.
                        var other = new AbilityExecutionContext(data, AbilityParams.Empty, targets[0]);
                        other.TriggerRule(new RuleAttackWithWeapon(x.unit, targets[0], weapon, 0) { AutoHit = true, SkipAnimation = true, SkipMainDamage = true });
                        Rulebook.Trigger(new RuleAttackWithWeapon(x.unit, targets[0], weapon, 0) { AutoHit = true, SkipAnimation = true, SkipMainDamage = true });
                        BridgeGame.Set(command, "<IsFinished>k__BackingField", true);
                        cast.Context.TriggerRule(new RuleAttackWithWeapon(x.unit, targets[0], weapon, 0) { AutoHit = true, SkipAnimation = true, SkipMainDamage = true });
                        Assert.Equal(count, casts.Casts.Count(c => c.Spell.Blueprint == blade.Blast)); Assert.Equal(1, x.state.NativeAcceptEvents);
                        game.Observations["K08-cleave-" + greater] = "actual TTT Core cleave component and native activation queue generated " + count + " attacks/paid child casts/damage; original TTT first-attack flags preserved; one Spend, later charge intact; wrong/absent context and ended command rejected; native scene groups, managed view geometry and LOS cache are fixture inputs; raycast/motion/animation scheduler not run";
                    } finally { x.unit.Commands.Raw[(int)UnitCommand.CommandType.Standard] = null; buff.Remove(); }
                } finally {
                    foreach (var geometry in geometries) geometry.Dispose();
                    foreach (var target in targets) memory.Remove(target);
                    foreach (var unit in sceneUnits) { unit.IsInGame = false; scene.RemoveEntityData(unit); unit.GroupId = oldGroups[unit]; }
                    foreach (var group in groups) if (group.Empty()) { Kingmaker.Game.Instance.UnitGroupsController.Groups.Remove(group); group.Dispose(); }
                    x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random);
                }
            }
        }

        [Theory]
        [InlineData("Earth", "ExtendedRange")]
        [InlineData("Earth", "Fragmentation")]
        [InlineData("Earth", "Spindle")]
        [InlineData("Fire", "Eruption")]
        [InlineData("Fire", "FanOfFlames")]
        [InlineData("Water", "Spray")]
        [InlineData("Air", "Cyclone")]
        public void K08MoreNativeFormInfusionsCauseActualDamageOnExplicitRecipient(string element, string form) {
            game.RequireGame(); var x = SpellUnit(16, CleanUnit(16, true)); var observer = new EffectObserver();
            var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(observer)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 16);
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>(element + "BlastFeature"));
                    var bp = PrivateNamed<BlueprintAbility>(form + element + "BlastAbility");
                    var visible = bp.GetComponent<AbilityShowIfCasterHasFact>(); if (visible != null) x.unit.AddFact(visible.UnitFact);
                    var root = PrivateNamed<BlueprintAbility>(element + "BlastBase");
                    var data = new AbilityData(bp, x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(root).Data };
                    var charge = Conversion(x.unit, Prepare(x.book, 4), "c054087f586eda15b7bfdbdbfd980811"); Pay(charge, Context(charge));
                    var target = HostileTarget(x.unit); BridgeGame.Set(target, "m_Position", new UnityEngine.Vector3(2, 0, 0));
                    target.Stats.SaveReflex.BaseValue = -100;
                    TargetWrapper click = data.TargetAnchor == AbilityTargetAnchor.Owner ? new TargetWrapper(x.unit) : data.TargetAnchor == AbilityTargetAnchor.Point ? new TargetWrapper(new UnityEngine.Vector3(2, 0, 0)) : new TargetWrapper(target);
                    var fee = bp.GetComponent<AbilityKineticist>();
                    if (form == "Spindle") {
                        Assert.Equal(2, fee.CalculateBurnCost(data).Total); // native B2; Spellburn cannot pay it
                        Assert.False(Rulebook.Trigger(new RuleCastSpell(data, click)).Success); Assert.Equal(0, x.state.NativeAcceptEvents);
                        x.unit.Progression.Features.AddFeature(game.Copy<BlueprintFeature>("71f526b1d4b50b94582b0b9cbe12b0e0"));
                        var settings = game.Copy<BlueprintFeature>(Burn).GetComponent<AddKineticistPart>();
                        x.unit.Get<UnitPartKineticist>().SetGatherPowerMode(KineticistGatherPowerMode.High);
                        x.unit.Buffs.AddBuff(settings.GatherPowerBuff3, x.unit, TimeSpan.FromSeconds(12), null);
                    }
                    var cost = fee.CalculateBurnCost(data); Assert.Equal(0, cost.Total);
                    var cast = Rulebook.Trigger(new RuleCastSpell(data, click)); Assert.True(cast.Success, data.CantTargetReason(click));
                    AbilityExecutionProcess.PrepareCast(cast.Context); Pay(data, cast.Context);
                    GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                    Assert.Contains(observer.Damage, d => d.Target == target && d.Result > 0);
                    var saving = bp.GetComponent<AbilityEffectRunAction>().SavingThrowType;
                    if (saving != SavingThrowType.Unknown) {
                        var save = Assert.Single(observer.Saves, s => s.Initiator == target);
                        Assert.Equal(saving, save.Type); Assert.Equal(23, save.DifficultyClass); Assert.False(save.IsPassed);
                    }
                    Assert.Equal(1, x.state.NativeAcceptEvents);
                    Assert.Equal(cost.InfusionBase + cost.InfusionIncrease > 3 ? HavockerChargeStatus0020.Consumed : HavockerChargeStatus0020.Armed, x.state.Status);
                    game.Observations["K08-effect-" + form] = "native cast/PrepareCast/Spend/ApplyEffectImmediate real recipient damage; native save=" + saving + "; one payment; targeting geometry, projectile, ricochet/secondary delivery not executed";
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void K08FoxfireNativeWillSaveAppliesActualFaerieFireForBothDurations(bool strongSave) {
            game.RequireGame(); var x = SpellUnit(10, CleanUnit(10, true)); var observer = new EffectObserver();
            var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(observer)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 10); x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("FireBlastFeature"));
                    var bp = PrivateNamed<BlueprintBuff>("FoxfireInfusionBuff"); var buff = x.unit.Buffs.AddBuff(bp, x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        var target = HostileTarget(x.unit); target.Stats.SaveWill.BaseValue = strongSave ? 100 : -100;
                        var data = new AbilityData(PrivateNamed<BlueprintAbility>("FireBlastAbility"), x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>("FireBlastBase")).Data };
                        var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge));
                        var cast = Rulebook.Trigger(new RuleCastSpell(data, target)); Assert.True(cast.Success);
                        AbilityExecutionProcess.PrepareCast(cast.Context); Pay(data, cast.Context);
                        GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                        Assert.Contains(observer.Damage, d => d.Target == target && d.Result > 0);
                        var save = Assert.Single(observer.Saves, s => s.Initiator == target); Assert.Equal(SavingThrowType.Will, save.Type); Assert.Equal(20, save.DifficultyClass); Assert.Equal(strongSave, save.IsPassed);
                        var payload = PrivateNamed<BlueprintBuff>("FaerieFireBuff"); var applied = target.Buffs.GetBuff(payload); Assert.NotNull(applied);
                        Assert.Equal(strongSave ? TimeSpan.FromSeconds(6) : TimeSpan.FromMinutes(1), applied.TimeLeft);
                        target.Buffs.TickBuff(applied, applied.EndTime); Assert.False(target.Buffs.HasFact(payload));
                        Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                        game.Observations["K08-effect-Foxfire-" + strongSave] = "native fire damage, Will DC20; real FaerieFire even on successful save (6s), failed=60s; native expiry; concealment interaction remains HOLD";
                    } finally { buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }
    }
}
