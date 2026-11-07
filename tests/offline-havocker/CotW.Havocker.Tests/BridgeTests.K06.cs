using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
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
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Controllers;
using WotR.Testing.Offline;
using CallOfTheWild.Wrath.Prototype;
using Xunit;

namespace CotW.Havocker.Tests
{
    public sealed partial class BridgeTests
    {
        // Real vanilla NPC input, rather than the story MC's unrelated Halo
        // facts. No blueprint/rule/buff mutation and no log suppression.
        const string K06Npc = "e9c2bef81c166654088e27dea05e4fbd";
        T PrivateNamed<T>(string name) where T : BlueprintScriptableObject {
            var map = (IDictionary)game.Api.GetProperty("Copies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game.Bridge);
            var key = map.Keys.Cast<BlueprintGuid>().Single(k => BridgeGame.Native<BlueprintScriptableObject>(k.ToString()).name == name);
            return (T)map[key];
        }
        static UnitEntityData CleanUnit(int level, bool player = false) {
            var unit = BridgeGame.Unit(level, 0, K06Npc);
            unit.Stats.HitPoints.BaseValue = 200;
            if (player) unit.SwitchFactions(BridgeGame.Native<BlueprintUnit>("5def1061f2e84909a1235ac8bb42578f").Faction);
            return unit;
        }
        sealed class MainCharacterFixture : IDisposable {
            readonly UnitEntityData original;
            internal MainCharacterFixture(UnitEntityData unit) {
                original = Kingmaker.Game.Instance.Player.MainCharacter.Value;
                Kingmaker.Game.Instance.Player.SetMainCharacter(unit);
            }
            public void Dispose() => Kingmaker.Game.Instance.Player.SetMainCharacter(original);
        }
        static UnitEntityData HostileTarget(UnitEntityData caster) {
            var target = CleanUnit(1);
            caster.AttackFactions.Add(target.Faction); target.AttackFactions.Add(caster.Faction);
            Assert.True(caster.Group.IsEnemy(target));
            return target;
        }
        void K06CheckActualTttEligibilityWhenLoaded() {
            if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "TabletopTweaks-Base")) return;
            var split = BridgeGame.Native<BlueprintFeature>("0b938825baf14a70818562a3cfe50493");
            var major = BridgeGame.Native<BlueprintFeature>("0943638214534753a225f524d0377b31");
            var source = split.GetComponents<PrerequisiteClassLevel>().Single();
            var unit = CleanUnit(18, true);
            Assert.True((bool)BridgeGame.Call(source, "CheckInternal", null, unit.Descriptor, null)); // provider's original Witch-level check
            Assert.False(source.Check(null, unit.Descriptor, null)); Assert.False(source.CheckRaw(null, unit.Descriptor, null));
            var granted = unit.Progression.Features.AddFeature(split);
            var majorSource = major.GetComponents<PrerequisiteFeature>().Single(p => p.Feature == split);
            Assert.False(majorSource.Check(null, unit.Descriptor, null));
            var shaman = BridgeGame.Native<BlueprintCharacterClass>("145f1d3d360a7ad48bd95d392c81b38e");
            unit.Progression.Classes.Add(new ClassData(shaman) { Level = 2 });
            var hex = unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeature>("894ef7374b805dd43b97603371b7ef10"));
            try {
                Assert.True(source.Check(null, unit.Descriptor, null), "real Shaman source must satisfy the added source guard"); Assert.True(source.CheckRaw(null, unit.Descriptor, null));
                Assert.True(majorSource.Check(null, unit.Descriptor, null), "real Split feat + hex source must preserve Major's original feature check");
                unit.Progression.Classes.First(c => c.CharacterClass.AssetGuid.ToString() == "1b9873f1e7bfe5449bc84d03e9c8e3cc").Level = 9;
                Assert.False(source.Check(null, unit.Descriptor, null)); // original level remains necessary
            } finally { unit.Progression.Features.RemoveFact(hex); unit.Progression.Features.RemoveFact(granted); }
            Assert.False(source.Check(null, unit.Descriptor, null));
            var ordinary = CleanUnit(18, true); ordinary.Progression.Classes.Single(c => c.CharacterClass.AssetGuid.ToString() == "1b9873f1e7bfe5449bc84d03e9c8e3cc").Archetypes.Clear();
            Assert.True(source.Check(null, ordinary.Descriptor, null)); // no new global class/hex ban
            Assert.DoesNotContain(split.GetComponents<PrerequisiteNoArchetype>(), p => p.Archetype.AssetGuid.ToString() == "c5c71fdb65d94504b2214b1ee5e5d032");
            game.Observations["K06-TTT-Q11"] = "real TTT Base2.6.18/Core0.7.15 blueprints: pure Havocker refused; native Shaman Slumber source allowed; original Witch level still required; ordinary Witch unchanged; shared prereqs untouched";
        }
        void GiveNativeGrowthRanks(UnitEntityData unit, int level) {
            // This fixture inserts ClassData rather than driving chargen. Grant
            // the exact authored level-entry facts as level-up would; native
            // feature ranks and damage formulas remain untouched.
            foreach (var row in game.Copy<BlueprintProgression>(Growth).LevelEntries.Where(r => r.Level <= level))
                foreach (var fact in row.Features) unit.Progression.Features.AddFeature((BlueprintFeature)fact);
            Assert.Equal((level + 1) / 2, unit.Progression.Features.GetRank(game.Copy<BlueprintFeature>(Rank)));
        }

        [Fact]
        public void K06TooltipCacheReadsKeepGraphValidButAuthoredLocalizationKeyChangesFail() {
            game.RequireGame();
            var bp = PrivateNamed<BlueprintAbility>("EarthBlastBladeDamage");
            string description = bp.Description; Assert.NotEmpty(description);
            BridgeGame.Call(game.Bridge, "Verify");
            var text = (Kingmaker.Localization.LocalizedString)BridgeGame.Field(bp, "m_Description");
            string key = text.Key;
            try { text.Key = key + "/K06-deliberate-drift"; Assert.Throws<InvalidOperationException>(() => BridgeGame.Call(game.Bridge, "Verify")); }
            finally { text.Key = key; }
            var action = ((IEnumerable)BridgeGame.Field(bp, "m_AllElements")).Cast<object>().OfType<ContextActionDealDamage>().First();
            bool old = action.IsAoE;
            try { action.IsAoE = !old; Assert.Throws<InvalidOperationException>(() => BridgeGame.Call(game.Bridge, "Verify")); }
            finally { action.IsAoE = old; }
            BridgeGame.Call(game.Bridge, "Verify");
        }

        [Fact]
        public void K06NativeNpcFixtureHasNoStoryHaloFacts() {
            game.RequireGame(); var unit = CleanUnit(6);
            Assert.DoesNotContain(unit.Facts.List, f => f.Blueprint.name.IndexOf("Halo", StringComparison.OrdinalIgnoreCase) >= 0);
            game.Observations["K06-fixture"] = unit.Blueprint.AssetGuid + "/" + unit.Blueprint.name + "; native NPC, no Halo facts removed or patched; view=" + (unit.View != null) + "; loadedArea=" + (Kingmaker.Game.Instance.CurrentlyLoadedArea != null) + "; full SaveManager/chargen-respec controller not executed";
        }

        [Theory]
        [InlineData("Earth")]
        [InlineData("Air")]
        [InlineData("Fire")]
        [InlineData("Cold")]
        public void K06OtherElementBladeNativeAttackDeliversOnlyPaidChild(string element) {
            game.RequireGame(); var x = SpellUnit(6, CleanUnit(6, true)); var observer = new CastObserver();
            using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(observer)) {
                try {
                    x.unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeatureSelection>("45c746aef0ef78c14914a054cbc68aa2"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>(element + "BlastFeature"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>(element + "KineticBladeFeature"));
                    var toggle = PrivateNamed<BlueprintActivatableAbility>("KineticBlade" + element + "BlastAbility");
                    var buff = x.unit.Buffs.AddBuff(toggle.Buff, x.unit, TimeSpan.FromMinutes(1), AbilityParams.Empty);
                    try {
                        var weapon = x.unit.Body.PrimaryHand.MaybeWeapon;
                        var blade = weapon.Blueprint.GetComponent<WeaponKineticBlade>();
                        Assert.Same(toggle.Buff.GetComponent<AddKineticistBlade>().Blade, weapon.Blueprint);
                        var unpaid = Rulebook.Trigger(new RuleCastSpell(blade.GetBlastAbility(x.unit.Descriptor), x.unit));
                        Assert.False(unpaid.Success); Assert.Equal(0, x.state.NativeAcceptEvents);
                        var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge));
                        var activation = blade.GetActivationAbility(x.unit.Descriptor);
                        var cast = Rulebook.Trigger(new RuleCastSpell(activation, x.unit)); Assert.True(cast.Success, "activation failed: " + element + "; charge=" + x.state.Status + "; canTarget=" + activation.CanTarget(x.unit)); Pay(activation, cast.Context);
                        Assert.Equal(1, x.state.NativeAcceptEvents);
                        var target = CleanUnit(1);
                        var attack = Rulebook.Trigger(new RuleAttackWithWeapon(x.unit, target, weapon, 0) { AutoHit = true, IsFirstAttack = true, SkipAnimation = true, SkipMainDamage = true });
                        var child = observer.Casts.Last(c => c.Spell.Blueprint == blade.Blast);
                        Assert.True(attack.AttackRoll.IsHit); Assert.True(child.Success, "child failed: " + element + "; force=" + child.ForceFail + "; canTarget=" + child.Spell.CanTarget(target));
                        Pay(child.Spell, child.Context); Pay(child.Spell, child.Context);
                        Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                        game.Observations["K06-blade-" + element] = "actual native weapon/attack/child cast; one zero-burn Spend event; threatHand=" + x.unit.GetThreatHand()?.Weapon?.Blueprint.name;
                    } finally { buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
            }
        }

        [Fact]
        public void K06NativeFullAttackBuilderSharesReceiptAndRejectsFinishedCommand() {
            game.RequireGame(); var x = SpellUnit(12, CleanUnit(12, true)); var observer = new CastObserver();
            using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(observer)) {
                try {
                    x.unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeatureSelection>("45c746aef0ef78c14914a054cbc68aa2"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthBlastFeature"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthKineticBladeFeature"));
                    x.unit.Stats.BaseAttackBonus.BaseValue = 11; // scenario input for iterative attacks
                    var toggle = PrivateNamed<BlueprintActivatableAbility>("KineticBladeEarthBlastAbility");
                    var buff = x.unit.Buffs.AddBuff(toggle.Buff, x.unit, TimeSpan.FromMinutes(1), AbilityParams.Empty);
                    try {
                        var weapon = x.unit.Body.PrimaryHand.MaybeWeapon; var blade = weapon.Blueprint.GetComponent<WeaponKineticBlade>();
                        var activation = blade.GetActivationAbility(x.unit.Descriptor);
                        var cast = Rulebook.Trigger(new RuleCastSpell(activation, x.unit)); Assert.True(cast.Success); Pay(activation, cast.Context);
                        var target = CleanUnit(1); target.Stats.HitPoints.BaseValue = 10000;
                        var command = new UnitAttack(target) { ForceFullAttack = true, OnlyForInspect = true };
                        command.Init(x.unit);
                        // Only the scene/animation scheduler is absent. The real
                        // native command creates the attacks and all their flags.
                        BridgeGame.Set(command, "<IsStarted>k__BackingField", true);
                        x.unit.Commands.Raw[(int)UnitCommand.CommandType.Standard] = command;
                        try {
                            var attacks = command.CreateFullAttack(); Assert.True(attacks.Count >= 3);
                            foreach (var attack in attacks) {
                                BridgeGame.Call(command, "TriggerAttackRule", attack);
                                Assert.True(command.LastAttackRule.IsFullAttack);
                            }
                            Assert.Equal(attacks.Count - 1, command.LastAttackRule.AttackNumber);
                            var children = observer.Casts.Where(c => c.Spell.Blueprint == blade.Blast).ToArray();
                            Assert.NotEmpty(children); Assert.All(children, c => Assert.True(c.Success));
                            foreach (var child in children) Pay(child.Spell, child.Context);
                            Assert.Equal(1, x.state.NativeAcceptEvents);
                            BridgeGame.Set(command, "<IsFinished>k__BackingField", true);
                            int before = observer.Casts.Count(c => c.Spell.Blueprint == blade.Blast);
                            Rulebook.Trigger(new RuleAttackWithWeapon(x.unit, target, weapon, 0) { AutoHit = true, IsFullAttack = true, IsFirstAttack = false, SkipAnimation = true, SkipMainDamage = true });
                            Assert.Equal(before, observer.Casts.Count(c => c.Spell.Blueprint == blade.Blast));
                            game.Observations["K06-full-attack"] = "native command builder attacks=" + attacks.Count + "; hit children=" + children.Length + "; native Spend events=" + x.state.NativeAcceptEvents + "; viewless scheduler input only";
                        } finally { x.unit.Commands.Raw[(int)UnitCommand.CommandType.Standard] = null; }
                    } finally { buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
            }
        }

        [Fact]
        public void K06InfusionEffectDefinitionsAreCapturedForNativePathTests() {
            game.RequireGame();
            var definition = typeof(ModEntry).Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426").GetMethod("Definition", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(object) }, null);
            var authored = game.Api.GetMethod("DefinitionValue", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(object) }, null);
            var names = new[] { "BurningInfusionBuff", "ChillingInfusionBuff", "EntanglingInfusionBuff", "BowlingInfusionBuff", "DazzlingInfusionBuff" };
            var audit = names.Concat(new[] { "BurningInfusionEffectBuff", "ChillingInfusionEffectBuff", "BurningInfusionDamageEffect", "DazzledBuff" }).ToDictionary(n => n, n => (string)definition.Invoke(null, new[] { authored.Invoke(null, new object[] { PrivateNamed<BlueprintBuff>(n).ComponentsArray }) }));
            System.IO.File.WriteAllText("C:/Users/xuenc/agent-tools/havocker-flash/havocker-hard6-20261106-210102-effect-definitions.json", Newtonsoft.Json.JsonConvert.SerializeObject(audit, Newtonsoft.Json.Formatting.Indented), new System.Text.UTF8Encoding(false));
            Assert.All(names, n => Assert.NotNull(PrivateNamed<BlueprintBuff>(n).GetComponent<AddKineticistInfusionDamageTrigger>()));
        }

        [Theory]
        [InlineData(6)]
        [InlineData(10)]
        public void K06OpportunityCommandPaysNativeActivationOnceAndReplayCannotUseNewCharge(int level) {
            game.RequireGame(); var x = SpellUnit(level, CleanUnit(level, true)); var observer = new CastObserver();
            var random = GameRandom.Save(); GameRandom.Init(62006);
            using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(observer)) {
                try {
                    x.unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeatureSelection>("45c746aef0ef78c14914a054cbc68aa2"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthBlastFeature"));
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("EarthKineticBladeFeature"));
                    x.unit.Stats.BaseAttackBonus.BaseValue = 100;
                    var toggle = PrivateNamed<BlueprintActivatableAbility>("KineticBladeEarthBlastAbility");
                    var buff = x.unit.Buffs.AddBuff(toggle.Buff, x.unit, TimeSpan.FromMinutes(1), AbilityParams.Empty);
                    try {
                        var target = CleanUnit(1); var weapon = x.unit.Body.PrimaryHand.MaybeWeapon;
                        var blade = weapon.Blueprint.GetComponent<WeaponKineticBlade>();
                        var command = new UnitAttackOfOpportunity(target); command.Init(x.unit);
                        Assert.Same(weapon, command.Hand.Weapon);
                        x.unit.Commands.Raw[(int)UnitCommand.CommandType.Free] = command;
                        if (level == 6) {
                            Assert.Equal(UnitCommand.ResultType.Fail, (UnitCommand.ResultType)BridgeGame.Call(command, "OnAction"));
                            Assert.Equal(0, x.state.NativeAcceptEvents); Assert.Empty(observer.Casts);
                            command = new UnitAttackOfOpportunity(target); command.Init(x.unit);
                            x.unit.Commands.Raw[(int)UnitCommand.CommandType.Free] = command;
                        }
                        var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge));
                        Assert.Equal(UnitCommand.ResultType.Success, (UnitCommand.ResultType)BridgeGame.Call(command, "OnAction"));
                        Assert.Equal(1, x.state.NativeAcceptEvents);
                        var child = Assert.Single(observer.Casts, c => c.Spell.Blueprint == blade.Blast);
                        Assert.True(child.Success); Pay(child.Spell, child.Context);
                        Assert.False(x.unit.Get<UnitPartKineticist>().IsBladeActivated);
                        if (level == 6) {
                            Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                            var next = Conversion(x.unit, Prepare(x.book, 2)); Pay(next, Context(next));
                        } else Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                        long id = x.state.ChargeId; int casts = observer.Casts.Count;
                        Assert.Equal(UnitCommand.ResultType.Success, (UnitCommand.ResultType)BridgeGame.Call(command, "OnAction"));
                        Assert.Equal(casts, observer.Casts.Count); Assert.Equal(1, x.state.NativeAcceptEvents);
                        Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status); Assert.Equal(id, x.state.ChargeId);
                        var nextCommand = new UnitAttackOfOpportunity(target); nextCommand.Init(x.unit);
                        x.unit.Commands.Raw[(int)UnitCommand.CommandType.Free] = nextCommand;
                        Assert.Equal(UnitCommand.ResultType.Success, (UnitCommand.ResultType)BridgeGame.Call(nextCommand, "OnAction"));
                        Assert.Equal(2, x.state.NativeAcceptEvents);
                        Assert.Equal(level == 6 ? HavockerChargeStatus0020.Consumed : HavockerChargeStatus0020.Armed, x.state.Status);
                        game.Observations["K06-AoO-" + level] = "native UnitAttackOfOpportunity.OnAction -> activation/Spend -> attack child; two distinct commands = two native events; replay = no new cast or payment; D0 retains charge; view/OnRun not executed";
                    } finally { x.unit.Commands.Raw[(int)UnitCommand.CommandType.Free] = null; buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }

        sealed class EffectObserver : IGlobalRulebookHandler<RuleDealDamage>, IGlobalRulebookHandler<RuleSavingThrow>, IGlobalRulebookSubscriber {
            internal readonly List<RuleDealDamage> Damage = new List<RuleDealDamage>();
            internal readonly List<RuleSavingThrow> Saves = new List<RuleSavingThrow>();
            public void OnEventAboutToTrigger(RuleDealDamage evt) { }
            public void OnEventDidTrigger(RuleDealDamage evt) => Damage.Add(evt);
            public void OnEventAboutToTrigger(RuleSavingThrow evt) { }
            public void OnEventDidTrigger(RuleSavingThrow evt) => Saves.Add(evt);
        }

        [Theory]
        [InlineData("Fire", "Burning", 6, false)]
        [InlineData("Fire", "Burning", 6, true)]
        [InlineData("Cold", "Chilling", 12, false)]
        [InlineData("Cold", "Chilling", 12, true)]
        public void K06NativeInfusionDamageAppliesRealBuffAndExpiresAtNativeDuration(string element, string infusion, int level, bool strongSave) {
            game.RequireGame(); var x = SpellUnit(level, CleanUnit(level, true)); var observer = new EffectObserver();
            var random = GameRandom.Save(); GameRandom.Init(62006);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(observer)) {
                try {
                    GiveNativeGrowthRanks(x.unit, level);
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>(element + "BlastFeature"));
                    var bp = PrivateNamed<BlueprintBuff>(infusion + "InfusionBuff");
                    var infusionBuff = x.unit.Buffs.AddBuff(bp, x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        Assert.Equal(10 + level / 2 + 5, infusionBuff.Context.Params.DC);
                        var action = Assert.Single(bp.GetComponent<AddKineticistInfusionDamageTrigger>().Actions.Actions.OfType<ContextActionApplyBuff>());
                        var activated = action.Buff.GetComponent<AddFactContextActions>().Activated;
                        Assert.Contains(activated.Actions, a => a is ContextActionRemoveSelf);
                        var savingAction = Assert.Single(activated.Actions.OfType<ContextActionSavingThrow>());
                        var branches = Assert.Single(savingAction.Actions.Actions.OfType<ContextActionConditionalSaved>());
                        Assert.Empty(branches.Succeed.Actions);
                        var payload = Assert.Single(branches.Failed.Actions.OfType<ContextActionApplyBuff>());
                        var blast = new AbilityData(PrivateNamed<BlueprintAbility>(element + "BlastAbility"), x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>(element + "BlastBase")).Data };
                        var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge));
                        Assert.Equal(0, blast.Blueprint.GetComponent<AbilityKineticist>().CalculateBurnCost(blast).Total);
                        var target = HostileTarget(x.unit);
                        if (element == "Fire") target.Stats.SaveReflex.BaseValue = strongSave ? 100 : -100;
                        else target.Stats.SaveFortitude.BaseValue = strongSave ? 100 : -100;
                        var cast = Rulebook.Trigger(new RuleCastSpell(blast, target)); Assert.True(cast.Success);
                        AbilityExecutionProcess.PrepareCast(cast.Context);
                        Pay(blast, cast.Context);
                        Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                        Assert.False(target.Buffs.HasFact(action.Buff));
                        // Native immediate application is the game's own effect
                        // entry, bypassing only projectile/scene delivery. Do not
                        // claim the absent projectile or animation ran.
                        AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                        Assert.Contains(observer.Damage, d => d.Target == target && d.Result > 0);
                        var save = Assert.Single(observer.Saves, s => s.Initiator == target);
                        Assert.Equal(element == "Fire" ? SavingThrowType.Reflex : SavingThrowType.Fortitude, save.Type);
                        Assert.Equal(10 + level / 2 + 5, save.DifficultyClass); Assert.Equal(strongSave, save.IsPassed);
                        // The authored carrier deliberately removes itself after
                        // its save; validate the actual downstream effect, not a
                        // nonexistent six-second carrier lifetime.
                        Assert.False(target.Buffs.HasFact(action.Buff));
                        var applied = target.Buffs.GetBuff(payload.Buff);
                        if (strongSave) Assert.Null(applied);
                        else {
                            Assert.NotNull(applied);
                            if (element == "Cold") {
                                Assert.True(target.State.HasCondition(UnitCondition.Staggered));
                                Assert.Equal(TimeSpan.FromSeconds(6), applied.TimeLeft);
                                target.Buffs.TickBuff(applied, applied.EndTime);
                                Assert.False(target.Buffs.HasFact(payload.Buff)); Assert.False(target.State.HasCondition(UnitCondition.Staggered));
                            } else {
                                Assert.True(applied.IsPermanent); int before = observer.Damage.Count;
                                applied.TickMechanics();
                                Assert.Contains(observer.Damage.Skip(before), d => d.Target == target && d.Result > 0 && d.Reason.Context?.AssociatedBlueprint == payload.Buff);
                                applied.Remove(); Assert.False(target.Buffs.HasFact(payload.Buff));
                            }
                        }
                        Assert.Equal(1, x.state.NativeAcceptEvents);
                        game.Observations["K06-effect-" + infusion + "-" + strongSave] = "native ApplyEffectImmediate -> actual damage -> carrier/save/self-remove -> payload; DC=" + infusionBuff.Context.Params.DC + "; passed=" + save.IsPassed + "; Cold=6s Staggered+expiry; Fire=permanent burning+native round damage, not fake 6s; no scene/projectile claim";
                    } finally { infusionBuff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void K06DazzlingInfusionUsesNativeWillSaveAndMinuteBuff(bool strongSave) {
            game.RequireGame(); var x = SpellUnit(6, CleanUnit(6, true)); var observer = new EffectObserver();
            var random = GameRandom.Save(); GameRandom.Init(62006);
            using (new MainCharacterFixture(x.unit)) using (EventBus.Subscribe(new KineticistController())) using (EventBus.Subscribe(observer)) {
                try {
                    GiveNativeGrowthRanks(x.unit, 6);
                    x.unit.Progression.Features.AddFeature(PrivateNamed<BlueprintFeature>("FireBlastFeature"));
                    var bp = PrivateNamed<BlueprintBuff>("DazzlingInfusionBuff");
                    var buff = x.unit.Buffs.AddBuff(bp, x.unit, TimeSpan.FromMinutes(1), null);
                    try {
                        Assert.Equal(18, buff.Context.Params.DC);
                        var savingAction = Assert.Single(bp.GetComponent<AddKineticistInfusionDamageTrigger>().Actions.Actions.OfType<ContextActionSavingThrow>());
                        var branches = Assert.Single(savingAction.Actions.Actions.OfType<ContextActionConditionalSaved>());
                        Assert.Empty(branches.Succeed.Actions);
                        var action = Assert.Single(branches.Failed.Actions.OfType<ContextActionApplyBuff>());
                        var target = HostileTarget(x.unit); target.Stats.SaveWill.BaseValue = strongSave ? 100 : -100;
                        var blast = new AbilityData(PrivateNamed<BlueprintAbility>("FireBlastAbility"), x.unit.Descriptor) { ConvertedFrom = x.unit.Abilities.GetAbility(PrivateNamed<BlueprintAbility>("FireBlastBase")).Data };
                        var charge = Conversion(x.unit, Prepare(x.book, 2)); Pay(charge, Context(charge));
                        var cast = Rulebook.Trigger(new RuleCastSpell(blast, target)); Assert.True(cast.Success);
                        AbilityExecutionProcess.PrepareCast(cast.Context); Pay(blast, cast.Context);
                        AbilityExecutionProcess.ApplyEffectImmediate(cast.Context, target);
                        Assert.Contains(observer.Damage, d => d.Target == target && d.Result > 0);
                        var save = Assert.Single(observer.Saves, s => s.Initiator == target);
                        Assert.Equal(SavingThrowType.Will, save.Type); Assert.Equal(18, save.DifficultyClass);
                        Assert.Equal(strongSave, save.IsPassed);
                        var applied = target.Buffs.GetBuff(action.Buff);
                        if (strongSave) { Assert.Null(applied); Assert.False(target.State.HasCondition(UnitCondition.Dazzled)); }
                        else { Assert.NotNull(applied); Assert.True(target.State.HasCondition(UnitCondition.Dazzled)); Assert.Equal(TimeSpan.FromMinutes(1), applied.TimeLeft); target.Buffs.TickBuff(applied, applied.EndTime); Assert.False(target.Buffs.HasFact(action.Buff)); Assert.False(target.State.HasCondition(UnitCondition.Dazzled)); }
                        Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                        game.Observations["K06-effect-Dazzling-" + strongSave] = "actual native Will save DC18, passed=" + save.IsPassed + "; failed branch real 60s Buff and expiry; one payment; seed62006, no die result override";
                    } finally { buff.Remove(); }
                } finally { x.unit.Progression.Features.RemoveFact(x.bridge); GameRandom.Restore(random); }
            }
        }
    }
}
