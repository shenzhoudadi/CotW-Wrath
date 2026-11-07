using System;
using System.Collections;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.LevelUp;
using CallOfTheWild.Wrath.Prototype;
using Xunit;

namespace CotW.Havocker.Tests {
    public sealed partial class BridgeTests {
        const string K12Prefix = "C:/Users/xuenc/agent-tools/havocker-flash/havocker-hard6-20261106-210102";

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void K12NativeHeadlessPreviewOwnsItsFactsBookAndPartWithoutChangingOriginalCharge(bool consumed) {
            game.RequireGame(); var x = SpellUnit(); var player = Kingmaker.Game.Instance.Player; var start = player.GameTime;
            UnitEntityData preview = null;
            try {
                var slot = Prepare(x.book, 2); var charge = Conversion(x.unit, slot); Pay(charge, Context(charge));
                if (consumed) {
                    var blast = new AbilityData(game.Copy<BlueprintAbility>("7d4712812818f094297f7d7920d130b1"), x.unit.Descriptor);
                    Pay(blast, Context(blast)); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                }
                player.GameTime += TimeSpan.FromSeconds(2);
                long id = x.state.ChargeId; var deadline = x.state.Expires; var status = x.state.Status; int credit = x.state.Credit; int events = x.state.NativeAcceptEvents;
                var originalPart = x.unit.Get<UnitPartKineticist>(); var display = x.state.Display.Fact;
                var oldController = Kingmaker.Game.Instance.LevelUpController;
                // This is the shipped no-view branch of UnitHelper.Copy, not a
                // serializer replacement or fabricated UI/controller result.
                preview = x.unit.CreatePreview(createView: false);
                Assert.NotNull(preview); Assert.Null(preview.View); Assert.NotSame(x.unit, preview);
                Assert.Same(oldController, Kingmaker.Game.Instance.LevelUpController);
                Assert.Same(x.unit, preview.PreviewOf.Entity);
                var previewBook = Assert.Single(preview.Spellbooks, b => b.Blueprint == x.book.Blueprint);
                Assert.NotSame(x.book, previewBook); Assert.Same(preview.Descriptor, previewBook.Owner);
                var previewBridge = Assert.Single(preview.Progression.Features.Enumerable, f => f.Blueprint == x.bridge.Blueprint);
                Assert.NotSame(x.bridge, previewBridge);
                var runtime = Assert.Single(previewBridge.Components, c => c.SourceBlueprintComponent is HavockerSpellburnController0020);
                var previewState = runtime.GetData<HavockerSpellburnState0020>();
                Assert.NotSame(x.state, previewState); Assert.Equal(HavockerChargeStatus0020.Absent, previewState.Status);
                Assert.NotNull(preview.Get<UnitPartKineticist>()); Assert.NotSame(originalPart, preview.Get<UnitPartKineticist>());
                Assert.Contains(BridgeGame.Native<BlueprintAbility>(Spellburn2), previewBook.GetSpontaneousConversionSpells(2));
                // Preview.CopyFacts creates fresh native facts/runtime data. It
                // is NOT a full save-file restoration of the live charge.
                Assert.False(slot.Available); Assert.Equal(id, x.state.ChargeId); Assert.Equal(deadline, x.state.Expires); Assert.Equal(status, x.state.Status); Assert.Equal(credit, x.state.Credit); Assert.Equal(events, x.state.NativeAcceptEvents);
                Assert.Same(originalPart, x.unit.Get<UnitPartKineticist>()); Assert.Same(display, x.state.Display.Fact);
                preview.Dispose(); preview = null;
                Assert.False(slot.Available); Assert.Equal(id, x.state.ChargeId); Assert.Equal(deadline, x.state.Expires); Assert.Equal(status, x.state.Status); Assert.Equal(credit, x.state.Credit); Assert.Equal(events, x.state.NativeAcceptEvents);
                Assert.Same(originalPart, x.unit.Get<UnitPartKineticist>()); Assert.Same(display, x.state.Display.Fact);
                if (!consumed) { Assert.NotNull(display); Assert.False(display.IsDisposed); Assert.Equal(deadline, display.EndTime); }
                game.Observations["K12-native-preview-" + consumed] = "real UnitHelper.CreatePreview(false)/CopyInternal/CopyFacts/CopySpellbook/TurnOn -> own book/fact/kinetic part, fresh absent charge state -> real Dispose; original paid slot, charge ID/status/credit/deadline/display and native event count unchanged; no View/UI/SaveManager/full retrain commit acceptance";
            } finally { preview?.Dispose(); x.unit.Progression.Features.RemoveFact(x.bridge); player.GameTime = start; }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void K12NativeFactAndControllerReapplyKeepSpentSlotAndNeverRearmConsumedCharge(bool consumed) {
            game.RequireGame(); var x = SpellUnit(); var player = Kingmaker.Game.Instance.Player; var start = player.GameTime;
            var previous = Kingmaker.Game.Instance.LevelUpController; Assert.Null(previous);
            LevelUpController controller = null;
            try {
                var slot = Prepare(x.book, 2); var charge = Conversion(x.unit, slot); Pay(charge, Context(charge));
                if (consumed) {
                    var blast = new AbilityData(game.Copy<BlueprintAbility>("7d4712812818f094297f7d7920d130b1"), x.unit.Descriptor);
                    Pay(blast, Context(blast)); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                }
                player.GameTime += TimeSpan.FromSeconds(2);
                long id = x.state.ChargeId; var deadline = x.state.Expires; var status = x.state.Status;
                var part = x.unit.Get<UnitPartKineticist>(); int events = x.state.NativeAcceptEvents;
                var feature = x.unit.Facts.List.Single(f => f.Blueprint.AssetGuid.ToString() == SpellburnFeature);
                int lists = ((IDictionary)BridgeGame.Field(x.book, "m_SpellConversionLists")).Count;
                // Real temporary deactivation/reactivation; no direct call to
                // our component lifecycle hooks and no altered blueprint flags.
                x.bridge.Reapply(); feature.Reapply(); x.bridge.Reapply(); feature.Reapply();
                Assert.Same(part, x.unit.Get<UnitPartKineticist>()); Assert.Equal(status, x.state.Status);
                Assert.False(slot.Available); Assert.Equal(id, x.state.ChargeId); Assert.Equal(deadline, x.state.Expires); Assert.Equal(events, x.state.NativeAcceptEvents);
                Assert.Equal(lists, ((IDictionary)BridgeGame.Field(x.book, "m_SpellConversionLists")).Count);
                Assert.Single(x.book.GetSpontaneousConversionSpells(2), a => a == BridgeGame.Native<BlueprintAbility>(Spellburn2));
                // The native headless/instant mode has no preview View. The
                // empty action list intentionally tests only its shipped
                // ApplyLevelUpActions -> ReapplyFeaturesOnLevelUp subchain.
                // It is not an earned level or a complete retrain controller.
                controller = LevelUpController.Start(x.unit, LevelUpState.CharBuildMode.LevelUp, instantCommit: true, ignoreSettings: true);
                Assert.True(controller.AutoCommit); Assert.Same(x.unit, controller.Preview); Assert.Empty(controller.LevelUpActions);
                Assert.Empty(controller.ApplyLevelUpActions(x.unit));
                Assert.False(slot.Available); Assert.Equal(id, x.state.ChargeId); Assert.Equal(deadline, x.state.Expires); Assert.Equal(status, x.state.Status); Assert.Equal(events, x.state.NativeAcceptEvents);
                Assert.Same(part, x.unit.Get<UnitPartKineticist>());
                if (consumed) Assert.Null(x.state.Display.Fact);
                else { Assert.NotNull(x.state.Display.Fact); Assert.Equal(deadline, x.state.Display.Fact.EndTime); }
                controller.Stop(); controller = null; Assert.Null(Kingmaker.Game.Instance.LevelUpController);
                Assert.False(slot.Available); Assert.Equal(deadline, x.state.Expires); Assert.Equal(status, x.state.Status);
                game.Observations["K12-native-reapply-" + consumed] = "real Fact.Reapply twice (bridge + conversion source) and native instant LevelUpController.ApplyLevelUpActions(empty) -> real progression reapply -> Stop; original paid slot/ID/status/deadline/events stable, conversion list singular, no new charge; no level gained/full UI/retrain commit claim";
            } finally {
                controller?.Stop(); Kingmaker.Game.Instance.LevelUpController = previous;
                x.unit.Progression.Features.RemoveFact(x.bridge); player.GameTime = start;
            }
        }
    }
}
