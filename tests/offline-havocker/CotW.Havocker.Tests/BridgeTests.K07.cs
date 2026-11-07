using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Controllers;
using WotR.Testing.Offline;
using CallOfTheWild.Wrath.Prototype;
using Xunit;

namespace CotW.Havocker.Tests
{
    public sealed partial class BridgeTests
    {
        const string K07Prefix = "C:/Users/xuenc/agent-tools/havocker-flash/havocker-hard6-20261106-210102";
        [Theory]
        [InlineData("Fire", "Flash", false)]
        [InlineData("Fire", "Flash", true)]
        [InlineData("Earth", "Entangling", false)]
        [InlineData("Earth", "Entangling", true)]
        [InlineData("Electric", "Synaptic", false)]
        [InlineData("Electric", "Synaptic", true)]
        public void K07InfusionNativeHitSavePayloadAndExpiry(string element, string infusion, bool strongSave) {
            game.RequireGame(); var x = SpellUnit(10, CleanUnit(10, true)); var observer = new EffectObserver();
            var random = GameRandom.Save(); GameRandom.Init(62006);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(observer)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 10);
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>(element + "BlastFeature"));
                    var bp = PrivateNamed<BlueprintBuff>(infusion + "InfusionBuff");
                    var buff = x.unit.Buffs.AddBuff(bp, x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        ContextActionSavingThrow saving; ContextActionApplyBuff payload;
                        var actions = bp.GetComponent<AddKineticistInfusionDamageTrigger>().Actions.Actions;
                        if (infusion == "Synaptic") {
                            var carrier = Assert.Single(actions.OfType<ContextActionApplyBuff>());
                            saving = Assert.Single(carrier.Buff.GetComponent<AddFactContextActions>().Activated.Actions.OfType<ContextActionSavingThrow>());
                            payload = Assert.Single(Assert.Single(saving.Actions.Actions.OfType<ContextActionConditionalSaved>()).Failed.Actions.OfType<ContextActionApplyBuff>());
                        } else {
                            saving = Assert.Single(actions.OfType<ContextActionSavingThrow>());
                            var failed = Assert.Single(saving.Actions.Actions.OfType<ContextActionConditionalSaved>()).Failed;
                            payload = infusion == "Entangling"
                                ? Assert.Single(Assert.Single(failed.Actions.OfType<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>()).IfFalse.Actions.OfType<ContextActionApplyBuff>())
                                : Assert.Single(failed.Actions.OfType<ContextActionApplyBuff>());
                        }
                        var condition = infusion == "Flash" ? UnitCondition.Blindness : infusion == "Entangling" ? UnitCondition.Entangled : UnitCondition.Staggered;
                        var target = HostileTarget(x.unit); target.Stats.SaveWill.BaseValue = target.Stats.SaveReflex.BaseValue = strongSave ? 100 : -100;
                        var blast = new AbilityData(PrivateNamed<BlueprintAbility>(element + "BlastAbility"), x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>(element + "BlastBase")).Data };
                        var charge = Conversion(x.unit, Prepare(x.book, 4), "c054087f586eda15b7bfdbdbfd980811"); Pay(charge, Context(charge));
                        Assert.Equal(0, blast.Blueprint.GetComponent<AbilityKineticist>().CalculateBurnCost(blast).Total);
                        var cast = Rulebook.Trigger(new RuleCastSpell(blast, target)); Assert.True(cast.Success);
                        AbilityExecutionProcess.PrepareCast(cast.Context); Pay(blast, cast.Context);
                        GameRandom.Init(62007); // effect input isolated from unrelated spell-slot setup RNG
                        AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                        Assert.Contains(observer.Damage, d => d.Target == target && d.Result > 0);
                        var save = Assert.Single(observer.Saves, s => s.Initiator == target);
                        Assert.Equal(saving.Type, save.Type); Assert.Equal(20, save.DifficultyClass); Assert.Equal(strongSave, save.IsPassed);
                        var applied = target.Buffs.GetBuff(payload.Buff);
                        if (strongSave) { Assert.Null(applied); Assert.False(target.State.HasCondition(condition)); }
                        else {
                            Assert.NotNull(applied); Assert.True(target.State.HasCondition(condition));
                            Assert.Equal(infusion == "Entangling" ? TimeSpan.FromMinutes(1) : TimeSpan.FromSeconds(6), applied.TimeLeft);
                            if (infusion == "Entangling") {
                                // The second native hit takes the authored existing-buff
                                // branch. It must not merely reapply the first condition.
                                GameRandom.Init(62007);
                                AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                                Assert.Single(observer.Saves, s => s.Initiator == target); // native same-frame dedup
                                Assert.False(target.Buffs.HasFact(PrivateNamed<BlueprintBuff>("EntanglingInfusionSecondEffectBuff")));
                                var time = Kingmaker.Game.Instance.Player.GameTime;
                                try {
                                    Kingmaker.Game.Instance.Player.GameTime += TimeSpan.FromMilliseconds(1);
                                    GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                                } finally { Kingmaker.Game.Instance.Player.GameTime = time; }
                                Assert.Equal(2, observer.Saves.Count(s => s.Initiator == target));
                                Assert.NotNull(target.Buffs.GetBuff(PrivateNamed<BlueprintBuff>("EntanglingInfusionSecondEffectBuff")));
                                applied = target.Buffs.GetBuff(payload.Buff); // native reapply replaces the first instance
                            }
                            target.Buffs.TickBuff(applied, applied.EndTime);
                            Assert.False(target.Buffs.HasFact(payload.Buff)); Assert.False(target.State.HasCondition(condition));
                            if (infusion == "Entangling") Assert.False(target.Buffs.HasFact(PrivateNamed<BlueprintBuff>("EntanglingInfusionSecondEffectBuff")));
                        }
                        Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                        game.Observations["K07-effect-" + infusion + "-" + strongSave] = "real native damage/save DC20, passed=" + save.IsPassed + "; authored payload+condition+expiry; Entangling second-hit branch checked; scene/projectile not executed";
                    } finally { buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }

        [Theory]
        [InlineData("BlueFlame")]
        [InlineData("Magma")]
        [InlineData("Metal")]
        public void K07CompositeBladeRejectsUnpaidBodyCostAndSharesGatherPaidChild(string element) {
            game.RequireGame(); var x = SpellUnit(10, CleanUnit(10, true)); var observer = new CastObserver(); var effects = new EffectObserver();
            var random = GameRandom.Save(); GameRandom.Init(62006);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(observer)) using (EventBus.Subscribe(effects)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 10);
                    var root = PrivateNamed<BlueprintAbility>(element + "BlastBase");
                    var visible = root.GetComponent<AbilityShowIfCasterHasFact>(); Assert.NotNull(visible);
                    var data = new AbilityData(root, x.unit.Descriptor);
                    Assert.False(visible.IsAbilityVisible(data));
                    // Explicit SC-16 fee fixture, not normal Havocker progression
                    // or a grant of an extra element/composite to player characters.
                    var source = x.unit.AddFact(visible.UnitFact);
                    try { Assert.True(visible.IsAbilityVisible(data)); } finally { x.unit.RemoveFact(source); }
                    Assert.False(visible.IsAbilityVisible(data));
                    Assert.Null(x.unit.Abilities.GetAbility(root));
                    x.unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeatureSelection>("45c746aef0ef78c14914a054cbc68aa2"));
                    x.unit.AddFact(root);
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>(element + "KineticBladeFeature"));
                    var toggle = PrivateNamed<BlueprintActivatableAbility>("KineticBlade" + element + "BlastAbility");
                    var buff = x.unit.Buffs.AddBuff(toggle.Buff, x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        var weapon = x.unit.Body.PrimaryHand.MaybeWeapon; var blade = weapon.Blueprint.GetComponent<WeaponKineticBlade>();
                        var activation = blade.GetActivationAbility(x.unit.Descriptor); var fee = activation.Blueprint.GetComponent<AbilityKineticist>();
                        Assert.Equal(2, fee.BlastBurnCost);
                        var charge = Conversion(x.unit, Prepare(x.book, 4), "c054087f586eda15b7bfdbdbfd980811"); Pay(charge, Context(charge));
                        long id = x.state.ChargeId; Assert.Equal(2, fee.CalculateBurnCost(activation).Total);
                        var refused = Rulebook.Trigger(new RuleCastSpell(activation, x.unit)); Assert.False(refused.Success);
                        Assert.Equal(0, x.state.NativeAcceptEvents); Assert.Equal(id, x.state.ChargeId); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                        x.unit.Progression.Features.AddFeature(game.Copy<BlueprintFeature>("71f526b1d4b50b94582b0b9cbe12b0e0"));
                        var part = x.unit.Get<UnitPartKineticist>(); var settings = game.Copy<BlueprintFeature>(Burn).GetComponent<AddKineticistPart>();
                        part.SetGatherPowerMode(KineticistGatherPowerMode.High);
                        x.unit.Buffs.AddBuff(settings.GatherPowerBuff3, x.unit, TimeSpan.FromSeconds(12), null);
                        Assert.Equal(0, fee.CalculateBurnCost(activation).Total); Assert.Equal(3, part.GatherPowerRank);
                        var cast = Rulebook.Trigger(new RuleCastSpell(activation, x.unit)); Assert.True(cast.Success); Pay(activation, cast.Context);
                        Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(0, part.GatherPowerRank); Assert.Equal(0, part.AcceptedBurn);
                        Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status); // specialization covers I=1; Spellburn cannot pay B=2
                        var target = HostileTarget(x.unit);
                        Rulebook.Trigger(new RuleAttackWithWeapon(x.unit, target, weapon, 0) { AutoHit = true, IsFirstAttack = true, SkipAnimation = true, SkipMainDamage = true });
                        var child = Assert.Single(observer.Casts, c => c.Spell.Blueprint == blade.Blast);
                        Assert.True(child.Success); Pay(child.Spell, child.Context); Pay(child.Spell, child.Context);
                        AbilityExecutionProcess.PrepareCast(child.Context); AbilityExecutionProcess.ApplyEffectImmediate(child.Context, target);
                        Assert.Contains(effects.Damage, d => d.Target == target && d.Result > 0);
                        Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status); Assert.Equal(id, x.state.ChargeId);
                        game.Observations["K07-composite-" + element] = "explicit SC16 fixture: native menu visibility (not composite eligibility), base ability absent before explicit grant; B2 refused before Spend with Spellburn intact; native gather pays; native weapon attack/child/damage share one zero-burn event; no automatic composite entitlement";
                    } finally { buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }
        static object K07JsonLeaves(object value) {
            if (value == null) return null;
            if (value is BlueprintReferenceBase reference) return new { reference = reference.Guid.ToString() };
            if (value is SimpleBlueprint bp) return new { blueprint = bp.AssetGuid.ToString(), name = bp.name };
            if (value is Enum) return value.ToString();
            if (value is Array array) return array.Cast<object>().Select(K07JsonLeaves).ToArray();
            return value;
        }

        [Theory]
        [InlineData("Fire", "Detonation", false)]
        [InlineData("Fire", "Detonation", true)]
        [InlineData("Water", "Torrent", false)]
        [InlineData("Water", "Torrent", true)]
        public void K07AreaShapedInfusionNativeSaveAndDamageOnExplicitRecipients(string element, string form, bool strongSave) {
            game.RequireGame(); var x = SpellUnit(10, CleanUnit(10, true)); var observer = new EffectObserver();
            var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(observer)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 10);
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>(element + "BlastFeature"));
                    var bp = PrivateNamed<BlueprintAbility>(form + element + "BlastAbility");
                    x.unit.AddFact(bp.GetComponent<AbilityShowIfCasterHasFact>().UnitFact);
                    var data = new AbilityData(bp, x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>(element + "BlastBase")).Data };
                    var charge = Conversion(x.unit, Prepare(x.book, 4), "c054087f586eda15b7bfdbdbfd980811"); Pay(charge, Context(charge));
                    var target = HostileTarget(x.unit); target.Stats.SaveReflex.BaseValue = strongSave ? 100 : -100;
                    // Native TargetWrapper.Equals falls back to position even
                    // for distinct units. Viewless fixture placement only; no
                    // movement/controller or condition implementation replaced.
                    BridgeGame.Set(target, "m_Position", new UnityEngine.Vector3(3, 0, 0));
                    Assert.False(new Kingmaker.Utility.TargetWrapper(target) == new Kingmaker.Utility.TargetWrapper(x.unit));
                    var click = data.TargetAnchor == AbilityTargetAnchor.Owner ? new Kingmaker.Utility.TargetWrapper(x.unit) : new Kingmaker.Utility.TargetWrapper(target);
                    var cast = Rulebook.Trigger(new RuleCastSpell(data, click)); Assert.True(cast.Success, data.CantTargetReason(click));
                    AbilityExecutionProcess.PrepareCast(cast.Context); Pay(data, cast.Context);
                    GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                    if (form == "Detonation") {
                        var conditional = bp.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>().Single();
                        using (cast.Context.GetDataScope(target)) {
                            game.Observations["K07-detonation-diagnostic-" + strongSave] = "caster=" + cast.Context.MaybeCaster.UniqueId + "; recipient=" + target.UniqueId + "; condition=" + conditional.ConditionsChecker.Check() + "; damage=" + string.Join(",", observer.Damage.Select(d => d.Result)) + "; saves=" + string.Join(",", observer.Saves.Select(s => s.IsPassed)) + "; apply=" + bp.GetComponent<Kingmaker.UnitLogic.Abilities.Components.Base.AbilityApplyEffect>()?.GetType().FullName;
                        }
                    }
                    var damage = Assert.Single(observer.Damage, d => d.Target == target && d.Result > 0);
                    var save = Assert.Single(observer.Saves, s => s.Initiator == target);
                    Assert.Equal(SavingThrowType.Reflex, save.Type); Assert.Equal(20, save.DifficultyClass); Assert.Equal(strongSave, save.IsPassed);
                    Assert.True(damage.DamageBundle.IsAoE);
                    Assert.Equal(form == "Torrent", damage.Half);
                    Assert.Equal(strongSave, damage.HalfBecauseSavingThrow);
                    var second = HostileTarget(x.unit); second.Stats.SaveReflex.BaseValue = strongSave ? 100 : -100;
                    BridgeGame.Set(second, "m_Position", new UnityEngine.Vector3(-3, 0, 0));
                    GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, second);
                    Assert.Contains(observer.Damage, d => d.Target == second && d.Result == damage.Result);
                    Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                    game.Observations["K07-effect-" + form + "-" + strongSave] = "native save+AoE damage/half on two explicit recipients; same paid context once; actual radius/line selector, projectile and scene not executed";
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }

        [Theory]
        [InlineData("WallEarth")]
        [InlineData("DeadlyEarthEarth")]
        [InlineData("CloudSteam")]
        public void K07PersistentAreaNativeEnterDamageAndExpiryPredicate(string form) {
            game.RequireGame(); var x = SpellUnit(16, CleanUnit(16, true)); var observer = new EffectObserver();
            var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(observer)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 16);
                    string element = form == "CloudSteam" ? "Steam" : "Earth";
                    var root = PrivateNamed<BlueprintAbility>(element + "BlastBase"); x.unit.AddFact(root);
                    var bp = PrivateNamed<BlueprintAbility>(form + "BlastAbility");
                    x.unit.AddFact(bp.GetComponent<AbilityShowIfCasterHasFact>().UnitFact);
                    var data = new AbilityData(bp, x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(root).Data };
                    // Composite CloudSteam is an explicit frozen fee fixture,
                    // paid by native gather, not an additional element grant.
                    x.unit.Progression.Features.AddFeature(game.Copy<BlueprintFeature>("71f526b1d4b50b94582b0b9cbe12b0e0"));
                    var part = x.unit.Get<UnitPartKineticist>(); var settings = game.Copy<BlueprintFeature>(Burn).GetComponent<AddKineticistPart>();
                    part.SetGatherPowerMode(KineticistGatherPowerMode.High);
                    x.unit.Buffs.AddBuff(settings.GatherPowerBuff3, x.unit, TimeSpan.FromSeconds(12), null);
                    var charge = Conversion(x.unit, Prepare(x.book, 4), "c054087f586eda15b7bfdbdbfd980811"); Pay(charge, Context(charge));
                    var target = HostileTarget(x.unit);
                    var cast = Rulebook.Trigger(new RuleCastSpell(data, new Kingmaker.Utility.TargetWrapper(UnityEngine.Vector3.zero))); Assert.True(cast.Success);
                    AbilityExecutionProcess.PrepareCast(cast.Context); Pay(data, cast.Context);
                    var spawn = Assert.Single(bp.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionSpawnAreaEffect>());
                    var duration = spawn.DurationValue.Calculate(cast.Context).Seconds; Assert.True(duration > TimeSpan.Zero);
                    var areaContext = cast.Context.CloneFor(spawn.AreaEffect, x.unit.Descriptor);
                    // The native base ctor reads only these identity/settings
                    // fields from its mandatory view input. No Unity native
                    // object is allocated, no scene view attached, and every
                    // area/action/damage/rule implementation remains native.
                    var identity = (Kingmaker.View.MapObjects.AreaEffectView)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Kingmaker.View.MapObjects.AreaEffectView));
                    identity.UniqueId = Guid.NewGuid().ToString("N");
                    var area = new AreaEffectEntityData(identity, areaContext, spawn.AreaEffect, target, Kingmaker.Game.Instance.TimeController.GameTime, duration, false);
                    try {
                        Assert.False(area.IsEnded); Assert.Null(area.View);
                        spawn.AreaEffect.HandleUnitEnter(area.Context, area, target);
                        Assert.Contains(observer.Damage, d => d.Target == target && d.Result > 0);
                        var second = HostileTarget(x.unit);
                        spawn.AreaEffect.HandleUnitEnter(area.Context, area, second);
                        Assert.Contains(observer.Damage, d => d.Target == second && d.Result > 0);
                        var now = Kingmaker.Game.Instance.Player.GameTime;
                        try {
                            Kingmaker.Game.Instance.Player.GameTime = now + duration;
                            Assert.False(area.IsEnded); // native strict < comparison
                            Kingmaker.Game.Instance.Player.GameTime += TimeSpan.FromTicks(1);
                            Assert.True(area.IsEnded);
                        } finally { Kingmaker.Game.Instance.Player.GameTime = now; }
                        area.ForceEnd(); Assert.True(area.IsEnded);
                        Assert.Equal(1, x.state.NativeAcceptEvents);
                        game.Observations["K07-effect-" + form] = "native area ctor with view-identity-only input, no attached view; authored duration=" + duration + "; real enter callbacks damage two explicit recipients; native exact-deadline/one-tick-later expiry predicate and ForceEnd; no spatial selection/scene round scheduler/expiry-controller claim";
                    } finally { area.Dispose(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }
        [Fact]
        public void K07CaptureOnlyBoundedInfusionAndCompositeDefinitions() {
            game.RequireGame();
            var map = (IDictionary)game.Api.GetProperty("Copies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game.Bridge);
            var authored = game.Api.GetMethod("DefinitionValue", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(object) }, null);
            var audit = new List<object>();
            foreach (BlueprintGuid key in map.Keys) {
                var native = BridgeGame.Native<BlueprintScriptableObject>(key.ToString());
                bool wanted = native is BlueprintBuff && (native.name.Contains("Infusion") || native.name.Contains("Magnetic"))
                    || native is BlueprintAbilityAreaEffect
                    || native is BlueprintAbility && (native.name.StartsWith("WallEarth") || native.name.StartsWith("EruptionEarth") || native.name.StartsWith("DetonationFire") || native.name.StartsWith("TorrentWater") || native.name.StartsWith("BlueFlameBlast") || native.name.Contains("KineticBladeBlueFlame"));
                if (!wanted) continue;
                var bp = (BlueprintScriptableObject)map[key];
                audit.Add(new { nativeId = key.ToString(), name = native.name, privateId = bp.AssetGuid.ToString(),
                    definition = K07JsonLeaves(authored.Invoke(null, new object[] { bp.ComponentsArray })) });
            }
            System.IO.File.WriteAllText(K07Prefix + "-targeted-definitions.json", Newtonsoft.Json.JsonConvert.SerializeObject(audit, Newtonsoft.Json.Formatting.Indented), new System.Text.UTF8Encoding(false));
            Assert.NotEmpty(audit);
            game.Observations["K07-definition-capture"] = "bounded existing private infusion/area/composite nodes=" + audit.Count + "; source capture only, not effect PASS";
        }

        [Fact]
        public void K07PureFlameRealSpellResistanceRuleOnlyIgnoresItsAuthoredSpells() {
            game.RequireGame(); var x = SpellUnit(16, CleanUnit(16, true));
            var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) {
                try {
                    var target = HostileTarget(x.unit);
                    // Native part scenario input, no replacement of SR checks.
                    target.Ensure<Kingmaker.UnitLogic.Parts.UnitPartSpellResistance>().AddResistance(100, "K07-SR-input", null, null, null);
                    AbilityExecutionContext MakeContext(string name) {
                        var data = new AbilityData(PrivateNamed<BlueprintAbility>(name + "BlastAbility"), x.unit.Descriptor);
                        return new AbilityExecutionContext(data, new AbilityParams { CasterLevel = 16, DC = 23 }, target);
                    }
                    var blue = MakeContext("BlueFlame"); var fire = MakeContext("Fire");
                    var before = Rulebook.Trigger(new RuleSpellResistanceCheck(blue, target)); Assert.True(before.IsSpellResisted); Assert.False(before.IgnoreSpellResistance);
                    var buff = x.unit.Buffs.AddBuff(PrivateNamed<BlueprintBuff>("PureFlameInfusionBuff"), x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        var own = Rulebook.Trigger(new RuleSpellResistanceCheck(blue, target)); Assert.True(own.IgnoreSpellResistance); Assert.False(own.IsSpellResisted);
                        var other = Rulebook.Trigger(new RuleSpellResistanceCheck(fire, target)); Assert.False(other.IgnoreSpellResistance); Assert.True(other.IsSpellResisted);
                    } finally { buff.Remove(); }
                    Assert.True(Rulebook.Trigger(new RuleSpellResistanceCheck(blue, target)).IsSpellResisted);
                    game.Observations["K07-effect-PureFlame"] = "actual native SR100 RuleSpellResistanceCheck: BlueFlame ignored only with real infusion buff; Fire unaffected; removal restores resistance; explicit composite fee fixture; delivery/SR process integration not claimed";
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }

        [Fact]
        public void K07MagneticNativeDamagePayloadChangesOnlyMetalWeaponAcAndExpires() {
            game.RequireGame(); var x = SpellUnit(10, CleanUnit(10, true)); var observer = new EffectObserver();
            var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(observer)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 10);
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("ElectricBlastFeature"));
                    var bp = PrivateNamed<BlueprintBuff>("MagneticInfusionBuff");
                    var buff = x.unit.Buffs.AddBuff(bp, x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        var target = HostileTarget(x.unit); var attacker = CleanUnit(1, true);
                        attacker.AttackFactions.Add(target.Faction);
                        var metal = attacker.Body.PrimaryHand.MaybeWeapon; var unarmed = attacker.Body.EmptyHandWeapon;
                        Assert.True(metal.Blueprint.Category.HasSubCategory(WeaponSubCategory.Metal));
                        Assert.False(unarmed.Blueprint.Category.HasSubCategory(WeaponSubCategory.Metal));
                        int Ac(ItemEntityWeapon weapon) => Rulebook.Trigger(new RuleAttackWithWeapon(attacker, target, weapon, 0) { SkipAnimation = true, SkipMainDamage = true }).AttackRoll.TargetAC;
                        int metalBefore = Ac(metal), unarmedBefore = Ac(unarmed);
                        var blast = new AbilityData(PrivateNamed<BlueprintAbility>("ElectricBlastAbility"), x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>("ElectricBlastBase")).Data };
                        var charge = Conversion(x.unit, Prepare(x.book, 4), "c054087f586eda15b7bfdbdbfd980811"); Pay(charge, Context(charge));
                        var cast = Rulebook.Trigger(new RuleCastSpell(blast, target)); Assert.True(cast.Success);
                        AbilityExecutionProcess.PrepareCast(cast.Context); Pay(blast, cast.Context); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                        Assert.Contains(observer.Damage, d => d.Target == target && d.Result > 0);
                        var payload = Assert.Single(bp.GetComponent<AddKineticistInfusionDamageTrigger>().Actions.Actions.OfType<ContextActionApplyBuff>());
                        var applied = target.Buffs.GetBuff(payload.Buff); Assert.NotNull(applied); Assert.Equal(TimeSpan.FromSeconds(12), applied.TimeLeft);
                        Assert.Equal(metalBefore - 4, Ac(metal)); Assert.Equal(unarmedBefore, Ac(unarmed));
                        target.Buffs.TickBuff(applied, applied.EndTime);
                        Assert.False(target.Buffs.HasFact(payload.Buff)); Assert.Equal(metalBefore, Ac(metal));
                        Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                        game.Observations["K07-effect-Magnetic"] = "native electric damage -> actual 12s Magnetic payload; native metal attack TargetAC -4, nonmetal unchanged, expiry restores AC; electrical spell/save modifiers not claimed";
                    } finally { buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }

        [Fact]
        public void K07RealTttAccursedAndExtraHexRequireTheirOriginalActualSources() {
            game.RequireGame();
            Assert.Contains(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == "TabletopTweaks-Base");
            var accursed = BridgeGame.Native<BlueprintFeature>("d896081865a74b86adaf7b34d5c3014b");
            var witchExtra = BridgeGame.Native<BlueprintFeatureSelection>("49eca6f6c329418aaf8e9dee0b39e2ea");
            var shamanExtra = BridgeGame.Native<BlueprintFeatureSelection>("08d9f686b2944ba6b3f7763882c0ded4");
            var accursedSource = Assert.Single(accursed.GetComponents<PrerequisiteFeaturesFromList>());
            var witchSource = Assert.Single(witchExtra.GetComponents<PrerequisiteFeature>());
            var shamanSource = Assert.Single(shamanExtra.GetComponents<PrerequisiteFeature>());
            var unit = CleanUnit(18, true);
            Assert.False(accursedSource.Check(null, unit.Descriptor, null));
            Assert.False(witchSource.Check(null, unit.Descriptor, null)); Assert.False(shamanSource.Check(null, unit.Descriptor, null));
            var shaman = BridgeGame.Native<BlueprintCharacterClass>("145f1d3d360a7ad48bd95d392c81b38e");
            unit.Progression.Classes.Add(new ClassData(shaman) { Level = 2 });
            var selection = unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeatureSelection>("4223fe18c75d4d14787af196a04e14e7"));
            var hex = unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeature>("894ef7374b805dd43b97603371b7ef10"));
            try {
                Assert.True(accursedSource.Check(null, unit.Descriptor, null)); Assert.True(shamanSource.Check(null, unit.Descriptor, null));
                Assert.False(witchSource.Check(null, unit.Descriptor, null)); // a Shaman source does not become Witch's required source
                Assert.Contains(shamanExtra.AllFeatures, f => f.AssetGuid.ToString() == "894ef7374b805dd43b97603371b7ef10");
            } finally { unit.Progression.Features.RemoveFact(hex); unit.Progression.Features.RemoveFact(selection); }
            Assert.False(accursedSource.Check(null, unit.Descriptor, null)); Assert.False(shamanSource.Check(null, unit.Descriptor, null));
            var ordinary = CleanUnit(18, true);
            ordinary.Progression.Classes.Single(c => c.CharacterClass.AssetGuid.ToString() == "1b9873f1e7bfe5449bc84d03e9c8e3cc").Archetypes.Clear();
            var witchSelection = ordinary.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeatureSelection>("9846043cf51251a4897728ed6e24e76f"));
            try { Assert.True(witchSource.Check(null, ordinary.Descriptor, null)); Assert.True(accursedSource.Check(null, ordinary.Descriptor, null)); }
            finally { ordinary.Progression.Features.RemoveFact(witchSelection); }
            Assert.False(witchSource.Check(null, ordinary.Descriptor, null));
            foreach (var bp in new BlueprintFeature[] { accursed, witchExtra, shamanExtra })
                Assert.DoesNotContain(bp.GetComponents<PrerequisiteNoArchetype>(), p => p.Archetype.AssetGuid.ToString() == "c5c71fdb65d94504b2214b1ee5e5d032");
            game.Observations["K07-TTT-Accursed-Extra"] = "actual TTT blueprints; pure Havocker false, independent Shaman source accepted where originally valid, Witch-specific source not forged, removal rejects, ordinary Witch source works; not Accursed reroll combat acceptance";
        }
    }
}
