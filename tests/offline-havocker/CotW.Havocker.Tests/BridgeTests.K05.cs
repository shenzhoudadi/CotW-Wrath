using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.Designers.Mechanics.Facts;
using Xunit;
using CallOfTheWild.Wrath.Prototype;

namespace CotW.Havocker.Tests
{
    public sealed partial class BridgeTests
    {
        sealed class CastObserver : IGlobalRulebookHandler<RuleCastSpell>, IGlobalRulebookSubscriber {
            internal readonly List<RuleCastSpell> Casts = new List<RuleCastSpell>();
            public void OnEventAboutToTrigger(RuleCastSpell evt) { }
            public void OnEventDidTrigger(RuleCastSpell evt) => Casts.Add(evt);
        }
        [Fact]
        public void RealPreparedMetamagicUsesOccupiedFourthSlotNotBaseSecondSlot() {
            game.RequireGame(); var x = SpellUnit(7);
            try {
                var second = Prepare(x.book, 2); var bp = second.SpellShell.Blueprint;
                var metamagic = new MetamagicData { SpellLevelCost = 2 }; metamagic.Add(Metamagic.Empower);
                var custom = new AbilityData(bp, x.book, 2) { MetamagicData = metamagic };
                x.book.AddCustomSpell(custom); x.book.Rest(); Assert.True(x.book.Memorize(custom)); x.book.Rest();
                var fourth = x.book.GetMemorizedSpellSlots(4).Single(s => s.SpellShell?.Blueprint == bp && s.Available);
                Assert.Equal(4, x.book.GetSpellLevel(fourth.SpellShell));
                var target = BridgeGame.Native<BlueprintAbility>("c054087f586eda15b7bfdbdbfd980811");
                var conversion = fourth.SpellShell.GetConversions().Single(a => a.Blueprint == target);
                Pay(conversion, Context(conversion));
                Assert.False(fourth.Available); Assert.True(second.Available); Assert.Equal(4, x.state.SlotLevel); Assert.Equal(2, x.state.Credit);
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Fact]
        public void BladeWeaponClosureActivationAndNativeAttackChildShareOnePayment() {
            game.RequireGame(); var x = SpellUnit(6, CleanUnit(6, true)); var observer = new CastObserver();
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(observer)) {
                try {
                    x.unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeatureSelection>("45c746aef0ef78c14914a054cbc68aa2"));
                    x.unit.Progression.Features.AddFeature(game.Copy<BlueprintFeature>("7f5f82c1108b961459c9884a0fa0f5c4"));
                    x.unit.Progression.Features.AddFeature(game.Copy<BlueprintFeature>("cfb2ef3aacffde141915a7d5464d3bb3"));
                    var toggle = game.Copy<BlueprintActivatableAbility>("77d9c04214a9bd84bbc1eefabcd98220");
                    var weaponBp = toggle.Buff.GetComponent<AddKineticistBlade>().Blade;
                    var blade = weaponBp.GetComponent<WeaponKineticBlade>(); Assert.NotNull(blade);
                    Assert.NotEqual("d386e82ad6ef52a4ab5251bc2dc6d93f", blade.ActivationAbility.AssetGuid.ToString());
                    Assert.Same(game.Copy<BlueprintAbility>("4fc5cf33da20b5444ad3a96c77af8d20"), blade.Blast);
                    var buff = x.unit.Buffs.AddBuff(toggle.Buff, x.unit, TimeSpan.FromMinutes(1), AbilityParams.Empty);
                    try {
                        var weapon = x.unit.Body.PrimaryHand.MaybeWeapon;
                        Assert.NotNull(weapon); Assert.Same(weaponBp, weapon.Blueprint);
                        var activation = blade.GetActivationAbility(x.unit.Descriptor); Assert.NotNull(activation);
                        var illegal = Rulebook.Trigger(new RuleCastSpell(activation, x.unit));
                        Assert.False(illegal.Success); Assert.Equal(0, x.state.NativeAcceptEvents); Assert.False(x.unit.Get<UnitPartKineticist>().IsBladeActivated);
                        var burn = Conversion(x.unit, Prepare(x.book, 2)); Pay(burn, Context(burn));
                        for (int n = 0; n < 4; n++) Assert.Equal(0, activation.Blueprint.GetComponent<AbilityKineticist>().CalculateBurnCost(activation).Total);
                        Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                        var cast = Rulebook.Trigger(new RuleCastSpell(activation, x.unit)); Assert.True(cast.Success);
                        Pay(activation, cast.Context); Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                        Assert.True(x.unit.Get<UnitPartKineticist>().IsBladeActivated);
                        // A later charge must survive delivery/replay of the
                        // previously paid blade attack's child execution.
                        var next = Conversion(x.unit, Prepare(x.book, 2)); var nextContext = Context(next);
                        using (nextContext.GetDataScope(x.unit)) {
                            var runtime = x.bridge.Components.Single(c => c.SourceBlueprintComponent is HavockerSpellburnController0020);
                            using (runtime.RequestEventContext()) game.Observations["K05-next-charge-validation"] = $"context={ReferenceEquals(nextContext, BridgeGame.Call(game.SpellApi, "ContextFor", next))};slot={BridgeGame.Call(game.SpellApi, "SourceSlot", next, 2, null, null)};can={BridgeGame.Call(runtime.SourceBlueprintComponent, "CanActivate", next, 2)};book={x.book.GetSpellLevel(next.ConvertedFrom)};caster={ReferenceEquals(next.Caster, x.unit.Descriptor)}";
                        }
                        Pay(next, nextContext); var charge = x.state.ChargeId;
                        game.Observations["K05-blade-after-new-charge"] = $"status={x.state.Status};id={charge};time={Kingmaker.Game.Instance.Player.GameTime};expiry={x.state.Expires};events={x.state.NativeAcceptEvents}";
                        Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                        var target = HostileTarget(x.unit);
                        var attack = new RuleAttackWithWeapon(x.unit, target, weapon, 0) { AutoHit = true, IsFirstAttack = true, SkipAnimation = true, SkipMainDamage = true };
                        Rulebook.Trigger(attack);
                        game.Observations["K05-blade-after-native-attack"] = $"status={x.state.Status};id={x.state.ChargeId};time={Kingmaker.Game.Instance.Player.GameTime};expiry={x.state.Expires};events={x.state.NativeAcceptEvents}";
                        var child = observer.Casts.Last(c => c.Spell.Blueprint == blade.Blast);
                        Assert.True(child.Success); Pay(child.Spell, child.Context); Pay(child.Spell, child.Context);
                        game.Observations["K05-blade-after-child-replay"] = $"status={x.state.Status};id={x.state.ChargeId};time={Kingmaker.Game.Instance.Player.GameTime};expiry={x.state.Expires};events={x.state.NativeAcceptEvents}";
                        Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status); Assert.Equal(charge, x.state.ChargeId);
                        Lifecycle(x.bridge, "OnApplyPostLoadFixes");
                        Assert.False(x.unit.Get<UnitPartKineticist>().IsBladeActivated); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                    } finally { buff?.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
            }
        }

        [Fact]
        public void ExistingHexFeatPrerequisitesRequireRealSourcesWithoutAnArchetypeBan() {
            game.RequireGame();
            var api = typeof(CallOfTheWild.Wrath.Prototype.ModEntry).Assembly.GetType("CallOfTheWild.Wrath.Prototype.SpellResourceFeats", true);
            BridgeGame.Call(api, "Register", ResourcesLibrary.BlueprintsCache); // actual existing production provider, not synthetic feats
            var hash = typeof(CallOfTheWild.Wrath.Prototype.ModEntry).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets").GetMethod("Id", BindingFlags.Static | BindingFlags.NonPublic);
            var amp = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>((BlueprintGuid)hash.Invoke(null, new object[] { "AmplifiedHexFeat" }));
            var unit = BridgeGame.Unit(12); var prerequisite = amp.GetComponents<Prerequisite>().Single();
            Assert.False(prerequisite.Check(null, unit.Descriptor, null));
            var shaman = BridgeGame.Native<BlueprintCharacterClass>("145f1d3d360a7ad48bd95d392c81b38e"); unit.Progression.Classes.Add(new ClassData(shaman) { Level = 2 });
            var hex = unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeature>("894ef7374b805dd43b97603371b7ef10"));
            try { Assert.True(prerequisite.Check(null, unit.Descriptor, null)); }
            finally { unit.Progression.Features.RemoveFact(hex); }
            Assert.False(prerequisite.Check(null, unit.Descriptor, null));
            var extra = BridgeGame.Native<BlueprintFeatureSelection>("b6054088b4ab4be286724127cbf48b35");
            var sourceChecks = extra.GetComponents<PrerequisiteFeature>().ToArray();
            Assert.NotEmpty(sourceChecks); Assert.All(sourceChecks, p => Assert.False(p.Check(null, unit.Descriptor, null)));
            Assert.DoesNotContain(extra.GetComponents<PrerequisiteNoArchetype>(), p => p.Archetype.AssetGuid.ToString() == "c5c71fdb65d94504b2214b1ee5e5d032");
            K06CheckActualTttEligibilityWhenLoaded();
        }

        [Fact]
        public void EveryPrivateInfusionAndKineticFeeRetainsNativeInputsAndUsesTheOneBridge() {
            game.RequireGame(); var x = SpellUnit(20);
            try {
                var copies = (IDictionary)game.Api.GetProperty("Copies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game.Bridge);
                var nativePool = BridgeGame.Native<BlueprintFeatureSelection>("58d6f8e9eea63f6418b107ce64f315ea");
                var ownPool = game.Copy<BlueprintFeatureSelection>(nativePool.AssetGuid.ToString());
                Assert.Equal(nativePool.AllFeatures.Length, ownPool.AllFeatures.Length);
                for (int i = 0; i < nativePool.AllFeatures.Length; i++) {
                    Assert.Same(copies[nativePool.AllFeatures[i].AssetGuid], ownPool.AllFeatures[i]);
                    Assert.Equal(nativePool.AllFeatures[i].GetComponents<Prerequisite>().Count(), ownPool.AllFeatures[i].GetComponents<Prerequisite>().Count());
                    foreach (var required in ownPool.AllFeatures[i].GetComponents<PrerequisiteClassLevel>())
                        Assert.NotEqual("42a455d9ec1ad924d889272429eb8391", required.CharacterClass.AssetGuid.ToString());
                }
                int fees = 0;
                foreach (DictionaryEntry entry in copies) {
                    var node = (BlueprintScriptableObject)entry.Value;
                    Assert.Empty(node.GetComponents<AbilityAcceptBurnOnCast>()); // no exposed voluntary burn-amplifier cast
                    var elements = ((IEnumerable)BridgeGame.Field(node, "m_AllElements")).Cast<object>();
                    Assert.DoesNotContain(elements, e => e.GetType().FullName == "Kingmaker.UnitLogic.Class.Kineticist.Actions.ContextActionAcceptBurn");
                    foreach (var parameters in node.GetComponents<ContextCalculateAbilityParamsBasedOnClass>()) {
                        Assert.Equal("1b9873f1e7bfe5449bc84d03e9c8e3cc", parameters.CharacterClass.AssetGuid.ToString());
                        Assert.True(parameters.UseKineticistMainStat);
                    }
                    if (!(entry.Value is BlueprintAbility ability)) continue;
                    var fee = ability.GetComponent<AbilityKineticist>(); if (fee == null) continue; fees++;
                    var native = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>((BlueprintGuid)entry.Key).GetComponent<AbilityKineticist>();
                    Assert.IsType<HavockerBlastCost0020>(fee);
                    Assert.Equal(native.BlastBurnCost, fee.BlastBurnCost); Assert.Equal(native.InfusionBurnCost, fee.InfusionBurnCost); Assert.Equal(native.WildTalentBurnCost, fee.WildTalentBurnCost);
                    var data = new AbilityData(ability, x.unit.Descriptor); fee.CalculateBurnCost(data); fee.IsAbilityRestrictionPassed(data);
                    Assert.Equal(0, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Absent, x.state.Status);
                }
                Assert.True(fees > 0);
                game.Observations["K05-cost-preview-count"] = fees.ToString();
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Fact]
        public void DuplicateNativePreparedShellsSpendOnlyOneAndValueEqualForgeriesStayRejected() {
            game.RequireGame(); var x = SpellUnit();
            try {
                var first = Prepare(x.book, 2); var second = Prepare(x.book, 2);
                var shared = first.SpellShell; var slots = x.book.GetMemorizedSpellSlots(2).Where(s => ReferenceEquals(s.SpellShell, shared) && s.Available).ToArray();
                Assert.Equal(2, slots.Length);
                var fake = new AbilityData(shared.Blueprint, x.book, 2);
                var invalid = new AbilityData(BridgeGame.Native<BlueprintAbility>(Spellburn2), x.unit.Descriptor) { ConvertedFrom = fake };
                Pay(invalid, Context(invalid)); Assert.All(slots, s => Assert.True(s.Available)); Assert.Equal(HavockerChargeStatus0020.Absent, x.state.Status);
                var activation = shared.GetConversions().Single(a => a.Blueprint.AssetGuid.ToString() == Spellburn2);
                Pay(activation, Context(activation));
                Assert.False(slots.Last().Available); Assert.True(slots.First().Available); Assert.Equal(slots.Last().Index, x.state.SourceSlot); Assert.Equal(1, x.state.Credit);
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Fact]
        public void ForeignPartTakeoverAndNativeFactRemovalClearOnlyOwnedChargeAndConversion() {
            game.RequireGame(); var x = SpellUnit();
            var other = new BlueprintAbility[10]; other[2] = BridgeGame.Native<BlueprintAbility>(Spellburn2);
            x.book.AddSpellConversionList("K05.Foreign.Conversion", other);
            var slot = Prepare(x.book, 2); var activation = Conversion(x.unit, slot); Pay(activation, Context(activation));
            var part = x.unit.Get<UnitPartKineticist>(); var foreign = BridgeGame.Native<BlueprintFeature>(Burn).GetComponent<AddKineticistPart>(); part.Setup(foreign);
            x.unit.Progression.Features.RemoveFact(BridgeGame.Native<BlueprintFeature>(SpellburnFeature)); x.unit.Progression.Features.RemoveFact(x.bridge);
            Assert.Same(part, x.unit.Get<UnitPartKineticist>()); Assert.Same(foreign, BridgeGame.Field(part, "m_Settings"));
            Assert.False(slot.Available); Assert.Equal(HavockerChargeStatus0020.Absent, x.state.Status); Assert.Null(x.state.Display.Fact);
            Assert.Contains(other[2], x.book.GetSpontaneousConversionSpells(2)); x.book.RemoveSpellConversionList("K05.Foreign.Conversion");
        }

        [Fact]
        public void ExtendedRangeVisibilityUsesPrivateSourcesAndDoesNotForgeNativeFacts() {
            game.RequireGame(); var x = SpellUnit();
            try {
                var own = game.Copy<BlueprintAbility>("7d4712812818f094297f7d7920d130b1"); var native = BridgeGame.Native<BlueprintAbility>("7d4712812818f094297f7d7920d130b1");
                var ownCheck = own.GetComponents<AbilityShowIfCasterHasFact>().Single(); var nativeCheck = native.GetComponents<AbilityShowIfCasterHasFact>().Single();
                var ability = new AbilityData(own, x.unit.Descriptor); Assert.False(ownCheck.IsAbilityVisible(ability));
                x.unit.Progression.Features.AddFeature(game.Copy<BlueprintFeature>("cb2d9e6355dd33940b2bef49e544b0bf"));
                Assert.True(ownCheck.IsAbilityVisible(ability)); Assert.False(nativeCheck.IsAbilityVisible(new AbilityData(native, x.unit.Descriptor)));
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }
    }
}
