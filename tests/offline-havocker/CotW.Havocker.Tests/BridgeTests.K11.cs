using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic;
using CallOfTheWild.Wrath.Prototype;
using Xunit;

namespace CotW.Havocker.Tests {
    public sealed partial class BridgeTests {
        const string K11Prefix = "C:/Users/xuenc/agent-tools/havocker-flash/havocker-hard6-20261106-210102";

        // Native scene registration and positions are scenario inputs. No view,
        // projectile, targeting rule, hit result or Unity object is substituted.
        sealed class K11TargetScene : IDisposable {
            readonly SceneEntitiesState scene = new SceneEntitiesState("K11-chain-target-input");
            readonly List<UnitEntityData> units = new List<UnitEntityData>();
            readonly Dictionary<UnitEntityData, string> oldGroups = new Dictionary<UnitEntityData, string>();
            readonly List<Kingmaker.UnitLogic.Groups.UnitGroup> groups = new List<Kingmaker.UnitLogic.Groups.UnitGroup>();
            internal void Add(UnitEntityData unit, UnityEngine.Vector3 point) {
                oldGroups.Add(unit, unit.GroupId); unit.GroupId = "K11/" + unit.UniqueId;
                BridgeGame.Set(unit, "m_Position", point);
                scene.AddEntityData(unit); unit.IsInGame = true; units.Add(unit); groups.Add(unit.Group);
                Assert.Contains(unit, Kingmaker.Game.Instance.State.Units);
                Assert.Equal(point, unit.Position);
            }
            public void Dispose() {
                foreach (var unit in units) {
                    unit.IsInGame = false; scene.RemoveEntityData(unit); unit.GroupId = oldGroups[unit];
                    Assert.DoesNotContain(unit, Kingmaker.Game.Instance.State.Units);
                }
                foreach (var group in groups) if (group.Count == 0) { Kingmaker.Game.Instance.UnitGroupsController.Groups.Remove(group); group.Dispose(); }
            }
        }

        [Theory]
        [InlineData("Earth")]
        [InlineData("Fire")]
        public void K11ChainNativeSelectorUsesBounceOriginDistanceRadiusAndUsedTargets(string element) {
            game.RequireGame(); var x = SpellUnit(10);
            using (var scene = new K11TargetScene()) {
                try {
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>(element + "BlastFeature"));
                    var data = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>(element + "BlastBase")).Data.GetConversions().Single(a => a.Blueprint == PrivateNamed<BlueprintAbility>(element + "BlastAbility"));
                    var ricochet = data.Blueprint.GetComponent<AbilityDeliverRicochet>(); Assert.NotNull(ricochet);
                    Assert.False((bool)BridgeGame.Field(ricochet, "m_UsedTargetsAgain"));
                    float radius = ricochet.Radius.Meters; Assert.True(radius > 0);
                    var origin = new UnityEngine.Vector3(10000, 0, 10000);
                    scene.Add(x.unit, origin + new UnityEngine.Vector3(radius * 5, 0, 0));
                    var first = HostileTarget(x.unit); scene.Add(first, origin);
                    var far = HostileTarget(x.unit); scene.Add(far, origin + new UnityEngine.Vector3(radius * .75f, 0, 0));
                    var near = HostileTarget(x.unit); scene.Add(near, origin + new UnityEngine.Vector3(radius * .25f, 0, 0));
                    var outside = HostileTarget(x.unit); scene.Add(outside, origin + new UnityEngine.Vector3(radius * 1.1f, 0, 0));
                    var ally = CleanUnit(1, true); scene.Add(ally, origin + new UnityEngine.Vector3(radius * .1f, 0, 0));
                    Assert.False(x.unit.IsEnemy(ally));
                    var context = new AbilityExecutionContext(data, data.CalculateParams(), first);
                    Assert.False((bool)BridgeGame.Call(ricochet, "CheckTarget", context, ally));
                    Assert.True((bool)BridgeGame.Call(ricochet, "CheckTarget", context, outside));
                    var used = new HashSet<UnitEntityData> { first };
                    UnitEntityData Select(UnitEntityData anchor) => (UnitEntityData)BridgeGame.Call(ricochet, "SelectNextTargetByMinDistance", context, (Kingmaker.Utility.TargetWrapper)anchor, used);
                    // Deliberately add the farther unit before the nearest one:
                    // native iteration order must not replace minimum-distance.
                    Assert.Same(near, Select(first));
                    used.Add(near); Assert.Same(far, Select(first));
                    used.Add(far); Assert.Null(Select(first)); // outside excluded by native radius
                    // Re-anchor only the standalone selector, not a simulated
                    // successful projectile. Outside is now in this new radius.
                    Assert.Same(outside, Select(far));
                    used.Add(outside); Assert.Null(Select(far));
                    Assert.Equal(0, x.state.NativeAcceptEvents);
                    game.Observations["K11-chain-selector-" + element] = "actual native SelectNextTargetByMinDistance on scene-registered native units: nearest despite insertion order, used targets excluded, ally rejected, radius enforced, re-anchored distance checked; no projectile launched, bounce/hit/effect/payment NOT claimed";
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void K11ChainNativeEnabledDeliveryStopsBeforeProjectileForExhaustedOrDisabledTargets(bool disableBeforeMove) {
            game.RequireGame(); var x = SpellUnit(10);
            using (var scene = new K11TargetScene()) {
                try {
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthBlastFeature"));
                    var data = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>("EarthBlastBase")).Data.GetConversions().Single(a => a.Blueprint == PrivateNamed<BlueprintAbility>("EarthBlastAbility"));
                    var ricochet = data.Blueprint.GetComponent<AbilityDeliverRicochet>(); Assert.NotNull(ricochet);
                    var origin = new UnityEngine.Vector3(-10000, 0, -10000); float radius = ricochet.Radius.Meters;
                    scene.Add(x.unit, origin + new UnityEngine.Vector3(radius * 5, 0, 0));
                    var first = HostileTarget(x.unit); scene.Add(first, origin);
                    var context = new AbilityExecutionContext(data, data.CalculateParams(), first);
                    Assert.True(ricochet.TargetsCount.Calculate(context) > 0);
                    var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge));
                    long id = x.state.ChargeId; var expires = x.state.Expires;
                    var own = x.unit.Buffs.AddBuff(PrivateNamed<BlueprintBuff>("ChainArrowsInfusionBuff"), x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        if (disableBeforeMove) {
                            var candidate = HostileTarget(x.unit); scene.Add(candidate, origin + new UnityEngine.Vector3(radius * .5f, 0, 0));
                            Assert.Same(candidate, BridgeGame.Call(ricochet, "SelectNextTargetByMinDistance", context, (Kingmaker.Utility.TargetWrapper)first, new HashSet<UnitEntityData> { first }));
                        }
                        var iterator = ricochet.Deliver(context, first);
                        try {
                            if (disableBeforeMove) { own.Remove(); own = null; }
                            Assert.False(iterator.MoveNext());
                        } finally { iterator.Dispose(); }
                        Assert.Equal(0, x.state.NativeAcceptEvents); Assert.Equal(id, x.state.ChargeId); Assert.Equal(expires, x.state.Expires); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                        Assert.NotNull(Kingmaker.Game.Instance.ProjectileController);
                        game.Observations["K11-chain-delivery-stop-" + disableBeforeMove] = "real native Deliver with positive authored targets count: " + (disableBeforeMove ? "eligible candidate exists, exact own carrier removed after iterator creation and before first MoveNext" : "enabled own carrier but original target already used and no legal next recipient") + "; native early exit, armed charge/deadline/events unchanged; standalone delivery only, no cast cost/cancel refund/full bounce PASS";
                    } finally { own?.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
            }
        }

        [Theory]
        [InlineData(10, 10, 6)]
        [InlineData(11, 9, 6)]
        [InlineData(20, 0, 11)]
        public void K11ChainPrivateTargetCountKeepsNativeFormulaButUsesOnlyWitch(int witchLevel, int wizardLevel, int expected) {
            game.RequireGame(); var x = SpellUnit(witchLevel);
            try {
                const string id = "4a18040254d040f78c298f10649eab71";
                const string witchId = "1b9873f1e7bfe5449bc84d03e9c8e3cc";
                // Raw class/level data are fixture inputs, as in prior cases.
                // Recompute the native CharacterLevel cache after changing them.
                x.unit.Progression.Classes.RemoveAll(c => c.CharacterClass.AssetGuid.ToString() != witchId);
                if (wizardLevel > 0) x.unit.Progression.Classes.Add(new ClassData(BridgeGame.Native<BlueprintCharacterClass>("ba34257984f4c41408ce1dc2004e342e")) { Level = wizardLevel });
                BridgeGame.Set(x.unit.Progression, "m_CharacterLevel", null);
                Assert.Equal(witchLevel + wizardLevel, x.unit.Progression.CharacterLevel);
                var donor = BridgeGame.Native<BlueprintUnitProperty>(id); var own = game.Copy<BlueprintUnitProperty>(id);
                Assert.NotSame(donor, own); Assert.Equal(donor.BaseValue, own.BaseValue); Assert.Equal(donor.OperationOnComponents, own.OperationOnComponents);
                Assert.Equal(UnitProperty.Level, Assert.Single(donor.GetComponents<SimplePropertyGetter>()).Property);
                var level = Assert.Single(own.GetComponents<ClassLevelGetter>()); Assert.Empty(own.GetComponents<SimplePropertyGetter>());
                Assert.Equal(witchId, level.Class.AssetGuid.ToString()); Assert.Equal("c5c71fdb65d94504b2214b1ee5e5d032", level.Archetype.AssetGuid.ToString());
                Assert.Equal(1 + (witchLevel + wizardLevel) / 2, donor.GetInt(x.unit));
                Assert.Equal(expected, own.GetInt(x.unit));
                var oracle = CleanUnit(0); oracle.Progression.Classes.Clear();
                oracle.Progression.Classes.Add(new ClassData(BridgeGame.Native<BlueprintCharacterClass>(witchId)) { Level = witchLevel });
                BridgeGame.Set(oracle.Progression, "m_CharacterLevel", null);
                Assert.Equal(donor.GetInt(oracle), own.GetInt(x.unit)); // independent native formula, no replacement calculator
                var copies = (System.Collections.IDictionary)game.Api.GetProperty("Copies", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(game.Bridge);
                int checkedEdges = 0;
                foreach (BlueprintAbility ability in copies.Values.OfType<BlueprintAbility>()) {
                    var ricochet = ability.GetComponent<AbilityDeliverRicochet>(); if (ricochet == null) continue;
                    var reference = (BlueprintUnitPropertyReference)BridgeGame.Field(ricochet.TargetsCount, "m_CustomProperty");
                    Assert.Same(own, reference.Get());
                    var data = new AbilityData(ability, x.unit.Descriptor);
                    var context = new AbilityExecutionContext(data, data.CalculateParams(), x.unit);
                    Assert.Equal(expected, ricochet.TargetsCount.Calculate(context)); checkedEdges++;
                }
                Assert.True(checkedEdges > 0);
                BridgeGame.Call(game.Bridge, "Verify");
                game.Observations["K11-chain-count-" + witchLevel + "-" + wizardLevel] = "native donor remains 1+floor(characterLevel/2); bounded private property is original math with actual Witch/Havocker ClassLevelGetter: " + expected + "; native oracle equal; all " + checkedEdges + " private Ricochet property edges point to own clone; no projectile/Archer/extra-element grant";
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }
    }
}
