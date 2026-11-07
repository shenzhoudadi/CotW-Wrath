using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
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
        const string K10Prefix = "C:/Users/xuenc/agent-tools/havocker-flash/havocker-hard6-20261106-210102";
        sealed class K10SceneInput : IDisposable {
            readonly Kingmaker.EntitySystem.SceneEntitiesState scene = new Kingmaker.EntitySystem.SceneEntitiesState("K10-parry-input");
            readonly List<UnitEntityData> units = new List<UnitEntityData>();
            readonly Dictionary<UnitEntityData, string> ids = new Dictionary<UnitEntityData, string>();
            readonly List<K08GeometryInput> views = new List<K08GeometryInput>();
            readonly List<Kingmaker.UnitLogic.Groups.UnitGroup> groups = new List<Kingmaker.UnitLogic.Groups.UnitGroup>();
            internal void Add(UnitEntityData unit) {
                ids.Add(unit, unit.GroupId); unit.GroupId = "K10/" + unit.UniqueId;
                scene.AddEntityData(unit); unit.IsInGame = true; units.Add(unit); groups.Add(unit.Group); views.Add(new K08GeometryInput(unit));
            }
            public void Dispose() {
                foreach (var unit in units) { unit.Commands.Raw[(int)UnitCommand.CommandType.Free] = null; unit.CombatState.LeaveCombat(); unit.Memory.Clear(); }
                foreach (var view in views) view.Dispose();
                foreach (var unit in units) { unit.IsInGame = false; scene.RemoveEntityData(unit); unit.GroupId = ids[unit]; }
                foreach (var group in groups) if (group.Count == 0) { Kingmaker.Game.Instance.UnitGroupsController.Groups.Remove(group); group.Dispose(); }
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void K10NativeParryWeaponQualificationAndRiposteQueue(bool kineticBlade, bool riposte) {
            game.RequireGame(); var x = SpellUnit(6); var casts = new CastObserver(); var attacks = new K08AttackObserver(); var effects = new EffectObserver();
            var random = GameRandom.Save(); GameRandom.Init(62007);
            var oldEngagement = Kingmaker.Game.Instance.CombatEngagementController;
            var engagement = new Kingmaker.Controllers.Combat.UnitCombatEngagementController();
            BridgeGame.Set(Kingmaker.Game.Instance, "<CombatEngagementController>k__BackingField", engagement);
            var queue = (IList)BridgeGame.Field(engagement, "m_ForceAttackOfOpportunity"); Assert.Empty(queue);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(casts)) using (EventBus.Subscribe(attacks)) using (EventBus.Subscribe(effects)) using (var scene = new K10SceneInput()) {
                try {
                    scene.Add(x.unit); GiveNativeGrowthRanks(x.unit, 6);
                    x.unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeatureSelection>("45c746aef0ef78c14914a054cbc68aa2"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthBlastFeature")); x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthKineticBladeFeature"));
                    x.unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeature>("47e9ac1bf9c376e44b64cb5844a5e6a6"));
                    if (riposte) x.unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeature>("9680cce5b61e36e4499971ca3f794607"));
                    var rapier = BridgeGame.Native<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>("1546a05eb151d424eb9132832d5511bb");
                    if (kineticBlade) x.unit.Buffs.AddBuff(PrivateNamed<BlueprintActivatableAbility>("KineticBladeEarthBlastAbility").Buff, x.unit, TimeSpan.FromMinutes(1), null);
                    else {
                        x.unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeature>("292d51f3d6a331644a8c29be0614f671"));
                        Assert.True(x.unit.Body.PrimaryHand.InsertItem(rapier.CreateEntity<Kingmaker.Items.ItemEntityWeapon>()));
                    }
                    var weapon = x.unit.Body.PrimaryHand.MaybeWeapon;
                    Assert.Equal(kineticBlade, weapon.Blueprint.GetComponent<WeaponKineticBlade>() != null);
                    Assert.False(Kingmaker.Enums.WeaponCategory.KineticBlast.HasSubCategory(Kingmaker.Enums.WeaponSubCategory.OneHandedSlashing));
                    var target = HostileTarget(x.unit); BridgeGame.Set(target, "m_Position", x.unit.Position + new UnityEngine.Vector3(.6f, 0, 0)); scene.Add(target);
                    x.unit.Memory.Add(target); target.Memory.Add(x.unit); K08VisibleSceneInput(x.unit, target, true); K08VisibleSceneInput(target, x.unit, true);
                    x.unit.CombatState.JoinCombat(true); x.unit.CombatState.Engage(target);
                    x.unit.Stats.BaseAttackBonus.BaseValue = 100; x.unit.Stats.AC.BaseValue = -100; target.Stats.BaseAttackBonus.BaseValue = 5;
                    Assert.True(x.unit.CombatState.IsEngage(target)); Assert.True(x.unit.IsReach(target, x.unit.Body.PrimaryHand));
                    var toggle = BridgeGame.Native<BlueprintActivatableAbility>("c0248f304998aa64da458e097c022d73");
                    var parry = x.unit.Buffs.AddBuff(toggle.Buff, x.unit, TimeSpan.FromMinutes(1), null);
                    // The toggle is a full-attack trigger, not the parry itself.
                    // Exercise its authored first-full-attack event to arm the
                    // real one-round parry child; kinetic category is rejected
                    // by the original DuelistWeapon qualification.
                    GameRandom.Init(62007); Rulebook.Trigger(new RuleAttackWithWeapon(x.unit, target, weapon, 0) { IsFullAttack = true, IsFirstAttack = true, SkipAnimation = true });
                    var armedBlueprint = BridgeGame.Native<BlueprintBuff>("4c535eb8ce7b4f040a1e62a497a7d3b9");
                    var armed = x.unit.Buffs.GetBuff(armedBlueprint); Assert.Equal(!kineticBlade, armed != null);
                    var component = armedBlueprint.GetComponent<Kingmaker.UnitLogic.FactLogic.DuelistParry>(); Assert.NotNull(component);
                    if (armed != null) {
                        var runtime = armed.Components.Single(c => c.SourceBlueprintComponent == component);
                        using (runtime.RequestEventContext()) Assert.NotNull(BridgeGame.Call(component, "GetSuitableWeapon"));
                    }
                    var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge)); long id = x.state.ChargeId;
                    GameRandom.Init(62007); var incoming = Rulebook.Trigger(new RuleAttackRoll(target, x.unit, rapier.CreateEntity<Kingmaker.Items.ItemEntityWeapon>(), 0));
                    if (kineticBlade) { Assert.Null(incoming.Parry); Assert.NotEqual(Kingmaker.RuleSystem.Rules.AttackResult.Parried, incoming.Result); Assert.Empty(queue); Assert.True(x.unit.Buffs.HasFact(toggle.Buff)); }
                    else {
                        Assert.NotNull(incoming.Parry); Assert.Equal(Kingmaker.RuleSystem.Rules.AttackResult.Parried, incoming.Result); Assert.False(x.unit.Buffs.HasFact(armedBlueprint));
                        Assert.Equal(riposte ? 1 : 0, queue.Count);
                        if (riposte) {
                            Assert.True(x.unit.CombatState.CanActInCombat); Assert.True(x.unit.CombatState.CanAttackOfOpportunity);
                            int opportunities = x.unit.CombatState.AttackOfOpportunityCount;
                            engagement.Tick(); Assert.Empty(queue);
                            var command = x.unit.Commands.GetCommand<UnitAttackOfOpportunity>(); Assert.NotNull(command); Assert.Same(target, command.Target);
                            Assert.Equal(opportunities - 1, x.unit.CombatState.AttackOfOpportunityCount);
                            GameRandom.Init(62007); Assert.Equal(UnitCommand.ResultType.Success, (UnitCommand.ResultType)BridgeGame.Call(command, "OnAction"));
                            Assert.Contains(attacks.Attacks, a => a.Initiator == x.unit && a.Target == target && a.IsAttackOfOpportunity);
                            Assert.Contains(effects.Damage, d => d.Target == target && d.Result > 0);
                        }
                    }
                    Assert.Equal(0, x.state.NativeAcceptEvents); Assert.Equal(id, x.state.ChargeId); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                    game.Observations["K10-parry-" + kineticBlade + "-" + riposte] = "native weapon qualification and real incoming roll; result=" + incoming.Result + "; rapier positive -> native ForceAoO queue/Tick/UnitCommands/AoO OnAction/damage; kinetic Slashing blade cannot native DuelistParry, no fake Grace/category change; no animation/global command tick claim; no Spellburn consumption for ordinary rapier";
                } finally { queue.Clear(); x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); BridgeGame.Set(Kingmaker.Game.Instance, "<CombatEngagementController>k__BackingField", oldEngagement); }
            }
        }
        [Fact]
        public void K10ChainArrowsUsesPrivateCarrierAndNativeTargetRulesBeforeProjectileBoundary() {
            game.RequireGame(); var x = SpellUnit(10);
            try {
                x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthBlastFeature"));
                var data = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>("EarthBlastBase")).Data.GetConversions().Single(a => a.Blueprint == PrivateNamed<BlueprintAbility>("EarthBlastAbility"));
                var target = HostileTarget(x.unit); var context = new AbilityExecutionContext(data, data.CalculateParams(), target);
                var ricochet = data.Blueprint.GetComponent<AbilityDeliverRicochet>(); Assert.NotNull(ricochet);
                var condition = (Kingmaker.ElementsSystem.ConditionsChecker)BridgeGame.Field(ricochet, "m_BeforeCondition");
                bool Enabled() { using (context.GetDataScope(x.unit)) return condition.Check(); }
                Assert.False(Enabled()); Assert.False(ricochet.Deliver(context, target).MoveNext());
                var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge)); long id = x.state.ChargeId;
                var donor = x.unit.Buffs.AddBuff(BridgeGame.Native<BlueprintBuff>("f583e43e4c904c1e854c479a280ab657"), x.unit, TimeSpan.FromMinutes(1), null);
                try { Assert.False(Enabled()); } finally { donor.Remove(); }
                var own = x.unit.Buffs.AddBuff(PrivateNamed<BlueprintBuff>("ChainArrowsInfusionBuff"), x.unit, TimeSpan.FromMinutes(1), null);
                try {
                    Assert.True(Enabled()); Assert.True((bool)BridgeGame.Call(ricochet, "CheckTarget", context, target)); Assert.False((bool)BridgeGame.Call(ricochet, "CheckTarget", context, x.unit));
                } finally { own.Remove(); }
                Assert.False(Enabled()); Assert.Equal(0, x.state.NativeAcceptEvents); Assert.Equal(id, x.state.ChargeId); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                game.Observations["K10-chain-boundary"] = "actual private BeforeCondition accepts own carrier, rejects native carrier; native enemy/self target checks and disabled Deliver early exit ran; no payment/charge side effects; positive bounce NOT run: ProjectileController present=" + (Kingmaker.Game.Instance.ProjectileController != null) + "; Launch needs CreateView/GameObject/BaseMechanics scene/prefab and IsHit loop; not effect PASS";
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }
        [Fact]
        public void K10AllPrivateExplodingParametersUseWitchIntAndIgnoreForeignLevels() {
            game.RequireGame(); var x = SpellUnit(10);
            try {
                x.unit.Stats.Intelligence.BaseValue = 20; x.unit.Stats.Charisma.BaseValue = 30; x.unit.Stats.Constitution.BaseValue = 40;
                x.unit.Progression.Classes.Add(new ClassData(BridgeGame.Native<BlueprintCharacterClass>("ba34257984f4c41408ce1dc2004e342e")) { Level = 10 }); // independent Wizard source, no kinetic class
                var ids = (string[])typeof(ModEntry).Assembly.GetType("CallOfTheWild.Wrath.Prototype.Havocker0020", true).GetField("ExplodingArrowsIds", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                Assert.Equal(17, ids.Length);
                foreach (string id in ids) {
                    var donor = BridgeGame.Native<BlueprintAbility>(id); Assert.Null(donor.GetComponent<ContextAbilityParamsCalculator>());
                    var bp = game.Copy<BlueprintAbility>(id); var calculator = Assert.Single(bp.GetComponents<ContextCalculateAbilityParamsBasedOnClass>());
                    Assert.True(calculator.UseKineticistMainStat); Assert.Equal(StatType.Intelligence, calculator.StatType); Assert.Equal("1b9873f1e7bfe5449bc84d03e9c8e3cc", calculator.CharacterClass.AssetGuid.ToString());
                    var result = new AbilityData(bp, x.unit.Descriptor).CalculateParams(); Assert.Equal(10, result.CasterLevel); Assert.Equal(5, result.SpellLevel); Assert.Equal(20, result.DC); Assert.Equal(15, result.Concentration);
                }
                BridgeGame.Call(game.Bridge, "Verify");
                game.Observations["K10-exploding-parameters"] = "all 17 finite private leaves use existing native BasedOnClass calculator, Witch10/Int20 -> CL10/SL5/DC20/concentration15 despite Wizard10/Cha30/Con40; no donor calculator added or extra element/ability granted";
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }
        [Fact]
        public void K10ExplodingPrivatePropertiesMatchNativeFormulaWithWitchIntSubstitution() {
            game.RequireGame(); var x = SpellUnit(10);
            try {
                x.unit.Stats.Intelligence.BaseValue = 20; x.unit.Stats.Constitution.BaseValue = 40; x.unit.Stats.Dexterity.BaseValue = 12; x.unit.Stats.Charisma.BaseValue = 30;
                x.unit.Progression.Classes.Add(new ClassData(BridgeGame.Native<BlueprintCharacterClass>("ba34257984f4c41408ce1dc2004e342e")) { Level = 10 });
                var native = CleanUnit(0); native.Progression.Classes.Clear();
                var originalClass = new ClassData(BridgeGame.Native<BlueprintCharacterClass>("42a455d9ec1ad924d889272429eb8391")) { Level = 10 };
                originalClass.Archetypes.Add(BridgeGame.Native<BlueprintArchetype>("e0684d0a6ef44cae9d095f5318618a85")); native.Progression.Classes.Add(originalClass);
                native.Stats.Constitution.BaseValue = 20; native.Stats.Dexterity.BaseValue = 12; native.Stats.Intelligence.BaseValue = 40;
                var ids = (string[])typeof(ModEntry).Assembly.GetType("CallOfTheWild.Wrath.Prototype.Havocker0020", true).GetField("ExplodingPropertyIds", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                var values = new Dictionary<string, int>();
                foreach (var id in ids) {
                    var donor = BridgeGame.Native<Kingmaker.UnitLogic.Mechanics.Properties.BlueprintUnitProperty>(id);
                    var own = game.Copy<Kingmaker.UnitLogic.Mechanics.Properties.BlueprintUnitProperty>(id);
                    Assert.Equal(donor.BaseValue, own.BaseValue); Assert.Equal(donor.OperationOnComponents, own.OperationOnComponents);
                    Assert.Equal(donor.GetInt(native), own.GetInt(x.unit)); values[id] = own.GetInt(x.unit);
                    if (id == "ccca4a8e5d014a7a90e344c2c15b4595") Assert.Empty(own.GetComponents<Kingmaker.UnitLogic.Mechanics.Properties.ClassLevelGetter>()); // native energy bonus is stat/2 only
                    else {
                        var level = Assert.Single(own.GetComponents<Kingmaker.UnitLogic.Mechanics.Properties.ClassLevelGetter>());
                        Assert.Equal("1b9873f1e7bfe5449bc84d03e9c8e3cc", level.Class.AssetGuid.ToString()); Assert.Equal("c5c71fdb65d94504b2214b1ee5e5d032", level.Archetype.AssetGuid.ToString());
                        Assert.Equal("42a455d9ec1ad924d889272429eb8391", donor.GetComponent<Kingmaker.UnitLogic.Mechanics.Properties.ClassLevelGetter>().Class.AssetGuid.ToString());
                    }
                }
                Assert.Equal(16, values["b98f2e23c10f4194b7568905bafba22c"]); // native Dex DC: 10+10/2+1, not Int DC20
                x.unit.Stats.Intelligence.BaseValue = 30;
                Assert.Equal(16, game.Copy<Kingmaker.UnitLogic.Mechanics.Properties.BlueprintUnitProperty>("b98f2e23c10f4194b7568905bafba22c").GetInt(x.unit));
                Assert.True(game.Copy<Kingmaker.UnitLogic.Mechanics.Properties.BlueprintUnitProperty>("7ccb3098ddff4d7096cbe4c6bfd1fddc").GetInt(x.unit) > values["7ccb3098ddff4d7096cbe4c6bfd1fddc"]);
                BridgeGame.Call(game.Bridge, "Verify");
                game.Observations["K10-exploding-property-formulas"] = "native KineticArcher10/Con20/Dex12 oracle equals private Havocker10+Wizard10/Int20/Con40/Dex12 for all 3 properties; native Dex DC16 retained, Con damage getter -> native kinetic-main-stat getter, operations/progression unchanged; donors untouched; no Archer/extra element granted to Havocker";
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }
        [Fact]
        public void K10ChargeUsesNativeActivationQueueWithoutClaimingMovementDelivery() {
            game.RequireGame(); var x = SpellUnit(6);
            try {
                x.unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeatureSelection>("45c746aef0ef78c14914a054cbc68aa2"));
                x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthBlastFeature")); x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthKineticBladeFeature"));
                var buff = x.unit.Buffs.AddBuff(PrivateNamed<BlueprintActivatableAbility>("KineticBladeEarthBlastAbility").Buff, x.unit, TimeSpan.FromMinutes(1), null);
                try {
                    var blade = x.unit.Body.PrimaryHand.MaybeWeapon.Blueprint.GetComponent<WeaponKineticBlade>(); Assert.NotNull(blade);
                    var bp = BridgeGame.Native<BlueprintAbility>("c78506dd0e14f7c45a599990e4e65038");
                    var data = new AbilityData(bp, x.unit.Descriptor); var command = new UnitUseAbility(data, HostileTarget(x.unit)); BridgeGame.Set(command, "<Executor>k__BackingField", x.unit);
                    UnitCommands.CustomHandlerData? handler = null;
                    new KineticistController().HandleUnitWantRunCommand(command, ref handler);
                    Assert.True(handler.HasValue, "charge components=" + string.Join(",", bp.ComponentsArray.Select(c => c.GetType().FullName)));
                    var activation = Assert.IsType<UnitUseAbility>(handler.Value.ExecuteBefore).Ability; Assert.Same(blade.ActivationAbility, activation.Blueprint);
                    Assert.False(Rulebook.Trigger(new RuleCastSpell(activation, x.unit)).Success); Assert.Equal(0, x.state.NativeAcceptEvents);
                    var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge)); long id = x.state.ChargeId; var expires = x.state.Expires;
                    Assert.Equal(0, activation.Blueprint.GetComponent<AbilityKineticist>().CalculateBurnCost(activation).Total);
                    Assert.Equal(id, x.state.ChargeId); Assert.Equal(expires, x.state.Expires); Assert.Equal(0, x.state.NativeAcceptEvents);
                    var paid = Rulebook.Trigger(new RuleCastSpell(activation, x.unit)); Assert.True(paid.Success); Pay(activation, paid.Context);
                    Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                    game.Observations["K10-charge-queue"] = "actual native Charge component -> KineticistController queue -> existing native activation/Spend, positive burn refusal, preview inert; full AbilityCustomCharge movement/UnitAttack delivery not executed";
                } finally { buff.Remove(); }
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void K10ExplodingArrowsNativeMainAndSecondaryDamageSaveBranches(bool strongSave) {
            game.RequireGame(); var x = SpellUnit(10); var effects = new EffectObserver(); var random = GameRandom.Save(); GameRandom.Init(62007);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(effects)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 10); x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthBlastFeature"));
                    var bp = PrivateNamed<BlueprintAbility>("ExplodingArrowsEarthBlastAbility");
                    var shown = bp.GetComponent<AbilityShowIfCasterHasFact>();
                    var parent = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>("EarthBlastBase")).Data;
                    var data = parent.GetConversions().Single(a => a.Blueprint == bp); // actual native menu variant construction
                    Assert.False(shown.IsAbilityVisible(data));
                    // Only the authored marker is supplied for an effect-branch
                    // fixture. No Archer class, ArcherBlast flag, extra element,
                    // qualification rewrite or projectile replacement is added.
                    var marker = x.unit.AddFact(shown.UnitFact);
                    try {
                        Assert.True(shown.IsAbilityVisible(data)); Assert.False((bool)x.unit.Get<UnitPartKineticist>().ArcherBlast);
                        K09Gather(x.unit); var charge = Conversion(x.unit, Prepare(x.book, 4), "c054087f586eda15b7bfdbdbfd980811"); Pay(charge, Context(charge));
                        var main = HostileTarget(x.unit); var secondary = HostileTarget(x.unit);
                        BridgeGame.Set(main, "m_Position", new UnityEngine.Vector3(1, 0, 0)); BridgeGame.Set(secondary, "m_Position", new UnityEngine.Vector3(2, 0, 0)); secondary.Stats.SaveReflex.BaseValue = strongSave ? 100 : -100;
                        var initial = data.CalculateParams();
                        var cast = Rulebook.Trigger(new RuleCastSpell(data, main)); Assert.True(cast.Success); int before = cast.Context.Params.DC; AbilityExecutionProcess.PrepareCast(cast.Context); int prepared = cast.Context.Params.DC; Pay(data, cast.Context);
                        System.IO.File.WriteAllText(K10Prefix + "-exploding-params-" + strongSave + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(new { initial = initial.DC, before, prepared, afterPay = cast.Context.Params.DC, fact = data.Fact?.OverrideParams?.DC, bp = data.Blueprint.name, same = ReferenceEquals(data.Blueprint, bp), contextBp = cast.Context.AssociatedBlueprint.name, calculator = bp.GetComponent<ContextAbilityParamsCalculator>()?.GetType().FullName, mainStat = x.unit.Get<UnitPartKineticist>().MainStatType.ToString(), intelligence = x.unit.Stats.Intelligence.Bonus }), new System.Text.UTF8Encoding(false));
                        Assert.Equal(20, initial.DC); Assert.Equal(20, prepared); Assert.Equal(20, cast.Context.Params.DC);
                        GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, main);
                        var primaryDamage = Assert.Single(effects.Damage, d => d.Target == main); Assert.True(primaryDamage.Result > 0); Assert.DoesNotContain(effects.Saves, s => s.Initiator == main);
                        Assert.True(primaryDamage.DamageBundle.IsAoE); Assert.False(primaryDamage.Half); Assert.False(primaryDamage.HalfBecauseSavingThrow);
                        GameRandom.Init(62007); AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, secondary);
                        var save = Assert.Single(effects.Saves, s => s.Initiator == secondary); Assert.Equal(strongSave, save.IsPassed);
                        Assert.Equal(Kingmaker.EntitySystem.Stats.SavingThrowType.Reflex, save.Type); Assert.Equal(16, save.DifficultyClass); // native CustomDC is Dex, not Con; see independent native oracle test
                        int secondaryDamage = 0;
                        if (strongSave) Assert.DoesNotContain(effects.Damage, d => d.Target == secondary); // authored Succeed is empty, not quarter damage
                        else { var damage = Assert.Single(effects.Damage, d => d.Target == secondary); Assert.True(damage.Result > 0); Assert.True(damage.DamageBundle.IsAoE); Assert.True(damage.Half); Assert.False(damage.HalfBecauseSavingThrow); secondaryDamage = damage.Result; }
                        Assert.Equal(1, x.state.NativeAcceptEvents);
                        game.Observations["K10-effect-ExplodingArrows-" + strongSave] = "forced exact marker only; native main-target direct damage and secondary Reflex branch, DC=" + save.DifficultyClass + "; main=" + primaryDamage.Result + "; secondary=" + secondaryDamage + "; native Succeed empty/Failed Half=true; one native Spend; ordinary Archer eligibility, weapon delivery, radius/LOS/projectile not claimed";
                    } finally { x.unit.RemoveFact(marker); }
                    Assert.False(shown.IsAbilityVisible(data));
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }

        [Fact]
        public void K10CaptureBoundedParryArrowsAndWeaponSourceMetadata() {
            game.RequireGame();
            var authored = game.Api.GetMethod("DefinitionValue", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(object) }, null);
            var audit = new List<object>();
            foreach (string id in new[] { "51fab25afcf01744a8dab7fc0c737519", "c0248f304998aa64da458e097c022d73", "9680cce5b61e36e4499971ca3f794607", "4a18040254d040f78c298f10649eab71", "b98f2e23c10f4194b7568905bafba22c", "7ccb3098ddff4d7096cbe4c6bfd1fddc", "ccca4a8e5d014a7a90e344c2c15b4595" }) {
                var bp = BridgeGame.Native<BlueprintScriptableObject>(id);
                audit.Add(new { id, bp.name, type = bp.GetType().FullName, baseValue = (bp as Kingmaker.UnitLogic.Mechanics.Properties.BlueprintUnitProperty)?.BaseValue, operation = (bp as Kingmaker.UnitLogic.Mechanics.Properties.BlueprintUnitProperty)?.OperationOnComponents.ToString(), definition = K07JsonLeaves(authored.Invoke(null, new object[] { bp.ComponentsArray })) });
            }
            foreach (string element in new[] { "Air", "Earth", "Metal" }) {
                var toggle = PrivateNamed<BlueprintActivatableAbility>("KineticBlade" + element + "BlastAbility");
                var weapon = toggle.Buff.GetComponent<AddKineticistBlade>().Blade;
                audit.Add(new { element, weapon = weapon.AssetGuid.ToString(), weapon.IsLight, weapon.IsTwoHanded, weapon.Category, damage = weapon.DamageType.Type.ToString(), form = weapon.DamageType.Physical.Form.ToString() });
            }
            foreach (string id in new[] { "51fab25afcf01744a8dab7fc0c737519", "c0248f304998aa64da458e097c022d73" }) {
                var buff = BridgeGame.Native<BlueprintActivatableAbility>(id).Buff;
                audit.Add(new { buff.name, id = buff.AssetGuid.ToString(), definition = K07JsonLeaves(authored.Invoke(null, new object[] { buff.ComponentsArray })) });
            }
            var metal = PrivateNamed<BlueprintAbility>("MetalBlastAbility");
            audit.Add(new { name = "private Metal/Arrows authored components", definition = K07JsonLeaves(authored.Invoke(null, new object[] { metal.ComponentsArray })) });
            System.IO.File.WriteAllText(K10Prefix + "-source-metadata.json", Newtonsoft.Json.JsonConvert.SerializeObject(audit, Newtonsoft.Json.Formatting.Indented), new System.Text.UTF8Encoding(false));
            Assert.Equal(13, audit.Count);
            game.Observations["K10-bounded-source"] = "parry native abilities/riposte/property + private weapon metadata only; not effect PASS, no Archer or composite grants";
        }
    }
}
