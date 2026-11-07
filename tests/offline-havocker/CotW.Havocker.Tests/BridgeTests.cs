// Local consumer of WotR.Testing.Offline (AGPL-3.0). No game files are included.
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Abilities;
using CallOfTheWild.Wrath.Prototype;
using WotR.Testing.Offline;
using Xunit;

namespace CotW.Havocker.Tests
{
    // The native-loader CotW DLL is referenced, but its entire historical
    // registration is deliberately not invoked. This adapter loads only the
    // S-KINETIC slice under test after the real native cache has initialized.
    [CollectionDefinition("havocker-bridge", DisableParallelization = true)]
    public sealed class Collection : ICollectionFixture<BridgeGame> { }

    public sealed class BridgeGame : WotrGameFixture
    {
        internal object Bridge;
        internal Type Api => typeof(CallOfTheWild.Wrath.Prototype.ModEntry).Assembly.GetType("CallOfTheWild.Wrath.Prototype.KineticistClassBridge0020", true);
        internal Type SpellApi => typeof(ModEntry).Assembly.GetType("CallOfTheWild.Wrath.Prototype.HavockerSpellburn0020", true);
        internal Type FormalApi => typeof(ModEntry).Assembly.GetType("CallOfTheWild.Wrath.Prototype.Havocker0020", true);
        internal static object Call(object receiver, string name, params object[] args) {
            var type = receiver as Type ?? receiver.GetType();
            try { return type.GetMethod(name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(receiver is Type ? null : receiver, args); }
            catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; }
        }
        internal T Stage<T>(object bridge, string id) where T : BlueprintScriptableObject {
            try { return (T)Api.GetMethod("Stage", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(typeof(T)).Invoke(bridge, new object[] { id, null }); }
            catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; }
        }
        internal T Copy<T>(string id) where T : BlueprintScriptableObject => (T)Api.GetMethod("Copy", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(typeof(T)).Invoke(Bridge, new object[] { id });
        protected override void VerifyModInitialized(GameSession session) {
            var snapshot = typeof(ModEntry).Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426").GetMethod("Definition", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(object) }, null);
            System.IO.File.WriteAllText("C:/Users/xuenc/agent-tools/havocker-flash/havocker-hard6-20261106-210102-native-gather2-definition.txt", (string)snapshot.Invoke(null, new object[] { Native<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>("3a2bfdc8bf74c5c4aafb97591f6e4282").ComponentsArray }), new System.Text.UTF8Encoding(false));
            Bridge = Call(FormalApi, "Register", ResourcesLibrary.BlueprintsCache);
            Observations["privateDonors"] = ((IDictionary)Api.GetProperty("Copies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Bridge)).Count.ToString();
            var map = (IDictionary)Api.GetProperty("Copies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Bridge);
            var audit = map.Keys.Cast<BlueprintGuid>().Select(key => new { donor = key.ToString(), nativeName = ((BlueprintScriptableObject)ResourcesLibrary.BlueprintsCache.Load(key)).name, own = ((BlueprintScriptableObject)map[key]).AssetGuid.ToString(), type = map[key].GetType().Name, components = ((BlueprintScriptableObject)map[key]).ComponentsArray.Select(c => c.GetType().FullName).ToArray(), elements = ((IEnumerable)Field(map[key], "m_AllElements")).Cast<object>().Select(e => e.GetType().FullName).Distinct().ToArray(), allowOnlyBurnCost = (map[key] as BlueprintAbility)?.GetComponent<AbilityKineticist>()?.AllowOnlyBurnCost }).ToArray();
            string auditPath = "C:/Users/xuenc/agent-tools/havocker-flash/havocker-hard6-20261106-210102-" + DateTime.Now.ToString("HHmmss") + "-graph-audit.json";
            System.IO.File.WriteAllText(auditPath, Newtonsoft.Json.JsonConvert.SerializeObject(audit, Newtonsoft.Json.Formatting.Indented), new System.Text.UTF8Encoding(false));
            Observations["K06-graph-audit"] = auditPath;
            Call(SpellApi, "Install", new HarmonyLib.Harmony("CotW.Havocker.Tests.RuntimeSlice"));
            Call(FormalApi, "Install", new HarmonyLib.Harmony("CotW.Havocker.Tests.FormalEntry"));
            Call(Bridge, "Verify");
            var providers = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == "TabletopTweaks-Base" || a.GetName().Name == "DarkCodex").Select(a => a.GetName().FullName).ToArray();
            Observations["K06-external-loaded"] = providers.Length == 0 ? "none; external eligibility not claimed" : string.Join(";", providers);
            if (providers.Any(p => p.StartsWith("TabletopTweaks-Base,"))) {
                var ids = new[] { "0b938825baf14a70818562a3cfe50493", "0943638214534753a225f524d0377b31", "d896081865a74b86adaf7b34d5c3014b", "49eca6f6c329418aaf8e9dee0b39e2ea" };
                var optional = ids.Select(id => { var bp = ResourcesLibrary.BlueprintsCache.Load(BlueprintGuid.Parse(id)) as BlueprintScriptableObject; return new { guid = id, present = bp != null, name = bp?.name, components = bp?.ComponentsArray.Select(c => c.GetType().FullName).ToArray() }; });
                string path = "C:/Users/xuenc/agent-tools/havocker-flash/havocker-hard6-20261106-210102-external-" + DateTime.Now.ToString("HHmmss") + ".json";
                System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(optional, Newtonsoft.Json.Formatting.Indented), new System.Text.UTF8Encoding(false));
                Observations["K06-external-blueprint-receipt"] = path;
            }
            Observations["scope"] = "F01/F02 rooted private graph + existing S-KINETIC. Fee tests now use the real Extended Range Earth variant (native I=1), without detached parents or fixture fee rewrites. Complete blade/subexecution proof remains HOLD.";
        }
        internal static object Field(object obj, string name) {
            for (var type = obj.GetType(); type != null; type = type.BaseType) { var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly); if (field != null) return field.GetValue(obj); }
            throw new MissingFieldException(obj.GetType().FullName, name);
        }
        internal static void Set(object obj, string name, object value) {
            for (var type = obj.GetType(); type != null; type = type.BaseType) { var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly); if (field != null) { field.SetValue(obj, value); return; } }
            throw new MissingFieldException(obj.GetType().FullName, name);
        }
        internal static T Native<T>(string id) where T : BlueprintScriptableObject => OfflineGame.Blueprint<T>(id);
        internal static UnitEntityData Unit(int witchLevel, int kineticistLevel = 0, string fixture = "5def1061f2e84909a1235ac8bb42578f") {
            var unit = OfflineGame.CreateUnit(fixture);
            // Fixture input only: class levels are native ClassData objects;
            // progression, resource and part calculations remain the real game.
            var data = new ClassData(Native<BlueprintCharacterClass>("1b9873f1e7bfe5449bc84d03e9c8e3cc")) { Level = witchLevel };
            data.Archetypes.Add(Native<BlueprintArchetype>("c5c71fdb65d94504b2214b1ee5e5d032"));
            unit.Progression.Classes.Add(data);
            if (kineticistLevel > 0) unit.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>("42a455d9ec1ad924d889272429eb8391")) { Level = kineticistLevel });
            return unit;
        }
    }

    [Collection("havocker-bridge")]
    public sealed partial class BridgeTests
    {
        readonly BridgeGame game;
        public BridgeTests(BridgeGame game) { this.game = game; }
        const string Growth = "30a5b8cf728bd4a4d8d90fc4953e322e", Rank = "93efbde2764b5504e98e6824cab3d27c", Burn = "57e3577a0eb53294e9d7cc649d5239a3";
        const string SpellburnFeature = "b269a4bd4b4ed424d7d50459b13e8fcd", Spellburn2 = "e401b2761a32a35224b73088db8da5b2";

        [Fact]
        public void FormalArchetypeIsAttachedOnceAndFrozenLevelTableIsExact() {
            game.RequireGame();
            var root = BridgeGame.Native<BlueprintArchetype>("c5c71fdb65d94504b2214b1ee5e5d032");
            var witch = root.GetParentClass();
            Assert.Equal("1b9873f1e7bfe5449bc84d03e9c8e3cc", witch.AssetGuid.ToString());
            Assert.Single(witch.Archetypes, a => a == root);
            Assert.Equal(new[] { 1,2,4,6,8,10,12,14,16,18,20 }, root.RemoveFeatures.Select(e => e.Level));
            Assert.Equal(3, root.RemoveFeatures.Single(e => e.Level == 1).Features.Count);
            Assert.All(root.RemoveFeatures, e => Assert.Contains(e.Features, f => f.AssetGuid.ToString() == "9846043cf51251a4897728ed6e24e76f"));
            var infusion = BridgeGame.Native<BlueprintFeatureSelection>("dd6a4b9e6484a10ed60648126fff2825");
            Assert.Equal(new[] { 4,8,12,16,20 }, root.AddFeatures.Where(e => e.Features.Contains(infusion)).Select(e => e.Level));
            var spec = BridgeGame.Native<BlueprintFeature>("1d713e7a46789c37f9ef3f6daee32111");
            Assert.Equal(new[] { 10,12,16,20 }, root.AddFeatures.Where(e => e.Features.Contains(spec)).Select(e => e.Level));
            Assert.Empty(spec.ComponentsArray); // no second native burn reducer
            Assert.False(root.RemoveSpellbook); Assert.False(root.ChangeCasterType);
            Assert.Same(game.Bridge, BridgeGame.Call(game.FormalApi, "Register", ResourcesLibrary.BlueprintsCache));
            Assert.Single(witch.Archetypes, a => a == root);
            BridgeGame.Call(game.Bridge, "Verify");
        }

        [Fact]
        public void FourPatronsOnlyGrantTheirFirstLevelAndKeepNativeParents() {
            game.RequireGame();
            var pool = BridgeGame.Native<BlueprintFeatureSelection>("45c746aef0ef78c14914a054cbc68aa2");
            var ids = new[] { "2bd0d44953a536f489082534c48f8e31", "c6816ad80a3df9c4ea7d3b012b06bacd", "3d8d3d6678b901444a07984294a1bc24", "7ab8947ce2e19c44a9edcf5fd1466686" };
            Assert.Equal(4, pool.AllFeatures.Length);
            foreach (var id in ids) {
                var own = game.Copy<BlueprintProgression>(id); var native = BridgeGame.Native<BlueprintProgression>(id);
                Assert.Contains(own, pool.AllFeatures); Assert.NotSame(native, own);
                Assert.Single(own.LevelEntries); Assert.Equal(1, own.LevelEntries[0].Level);
                Assert.Contains(native.LevelEntries, e => e.Level == 2); // native defenses untouched
                Assert.Equal("1b9873f1e7bfe5449bc84d03e9c8e3cc", own.Classes.Single().AssetGuid.ToString());
                Assert.Equal("42a455d9ec1ad924d889272429eb8391", native.Classes.Single().AssetGuid.ToString());
                Assert.DoesNotContain(own.LevelEntries.SelectMany(e => e.Features), f => native.LevelEntries.Any(e => e.Level == 2 && e.Features.Contains(f)));
            }
            var donor = BridgeGame.Native<BlueprintFeatureSelection>("58d6f8e9eea63f6418b107ce64f315ea");
            var infusion = game.Copy<BlueprintFeatureSelection>(donor.AssetGuid.ToString());
            Assert.Equal(donor.AllFeatures.Length, infusion.AllFeatures.Length);
            Assert.All(infusion.AllFeatures, f => Assert.DoesNotContain(f, donor.AllFeatures));
            var basic = game.Copy<BlueprintAbility>("b28c336c10eb51c4a8ded0258d5742e1");
            Assert.Same(game.Copy<BlueprintAbility>("e53f34fb268a7964caf1566afb82dadd"), basic.Parent);
            Assert.Equal(0, basic.GetComponent<AbilityKineticist>().InfusionBurnCost);
            var extended = game.Copy<BlueprintAbility>("7d4712812818f094297f7d7920d130b1");
            Assert.Equal(1, extended.GetComponent<AbilityKineticist>().InfusionBurnCost); // real native fee
            Assert.IsType<HavockerBlastCost0020>(extended.GetComponent<AbilityKineticist>());
            Assert.IsNotType<HavockerBlastCost0020>(BridgeGame.Native<BlueprintAbility>("7d4712812818f094297f7d7920d130b1").GetComponent<AbilityKineticist>());
        }

        [Fact]
        public void ClassEntryRejectsBothOrdersAndCommitCannotBypassIt() {
            game.RequireGame(); var hv = BridgeGame.Unit(1); var kin = BridgeGame.Unit(1, 1);
            var cls = BridgeGame.Native<BlueprintCharacterClass>("42a455d9ec1ad924d889272429eb8391");
            var root = BridgeGame.Native<BlueprintArchetype>("c5c71fdb65d94504b2214b1ee5e5d032");
            var guard = cls.GetComponents<PrerequisiteNoArchetype>().Single(p => p.Archetype == root);
            Assert.False((bool)BridgeGame.Call(guard, "CheckInternal", null, hv.Descriptor, null));
            Assert.False((bool)BridgeGame.Call(root.GetComponent<PrerequisiteNoClassLevel>(), "CheckInternal", null, kin.Descriptor, null));
            new SelectClass(cls).Apply(null, hv.Descriptor); // native action, scoped prefix prevents mutation
            Assert.Equal(0, hv.Progression.GetClassLevel(cls));
            kin.Progression.Classes.First(c => c.CharacterClass == root.GetParentClass()).Archetypes.Clear();
            new AddArchetype(root.GetParentClass(), root).Apply(null, kin.Descriptor);
            Assert.Empty(kin.Progression.Classes.First(c => c.CharacterClass == root.GetParentClass()).Archetypes);
            Assert.True((bool)BridgeGame.Call(guard, "CheckInternal", null, kin.Descriptor, null)); // ordinary Witch + kineticist allowed
            Assert.True((bool)BridgeGame.Call(root.GetComponent<PrerequisiteNoClassLevel>(), "CheckInternal", null, hv.Descriptor, null));
        }

        [Fact]
        public void ConnectedPublicationRollsBackOwnAssetsAfterAttachmentFailure() {
            game.RequireGame();
            var graph = Activator.CreateInstance(game.Api, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { ResourcesLibrary.BlueprintsCache }, null);
            var feature = new BlueprintFeature { ComponentsArray = Array.Empty<BlueprintComponent>() };
            game.Api.GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(typeof(BlueprintFeature)).Invoke(graph, new object[] { feature, "Havocker0020.TestOnly.AtomicFailure", "Atomic test", "Test-only publication fault fixture." });
            object parent = new object(), original = parent;
            Action connect = () => { parent = feature; throw new InvalidOperationException("injected attachment fault"); };
            Action undo = () => parent = original;
            Assert.Throws<InvalidOperationException>(() => BridgeGame.Call(graph, "PublishConnected", connect, undo));
            Assert.Same(original, parent); Assert.Null(ResourcesLibrary.BlueprintsCache.Load(feature.AssetGuid));
            BridgeGame.Call(game.Bridge, "Verify");
        }

        [Fact]
        public void NativeGatherStacksAfterSpecializationAndSpellburnThenClearsOnZeroSpend() {
            game.RequireGame(); var x = SpellUnit(10);
            var bp = game.Copy<BlueprintAbility>("7d4712812818f094297f7d7920d130b1"); var fee = bp.GetComponent<AbilityKineticist>();
            int originalI = fee.InfusionBurnCost, originalB = fee.BlastBurnCost;
            using (EventBus.Subscribe(new KineticistController())) { // real native controller, not a cost shim
                try {
                    x.unit.Progression.Features.AddFeature(game.Copy<BlueprintFeature>("71f526b1d4b50b94582b0b9cbe12b0e0"));
                    var part = x.unit.Get<UnitPartKineticist>(); var settings = game.Copy<BlueprintFeature>(Burn).GetComponent<AddKineticistPart>();
                    Assert.NotNull(part.GatherPowerAbility); Assert.Equal(1, part.GatherPowerBaseValue);
                    Assert.NotEmpty(settings.Blasts); Assert.All(settings.Blasts, a => Assert.IsType<HavockerBlastCost0020>(a.GetComponent<AbilityKineticist>()));
                    part.SetGatherPowerMode(KineticistGatherPowerMode.High);
                    x.unit.Buffs.AddBuff(settings.GatherPowerBuff3, x.unit, TimeSpan.FromSeconds(12), AbilityParams.Empty);
                    Assert.Equal(3, part.GatherPowerRank);
                    var activation = Conversion(x.unit, Prepare(x.book, 2)); Pay(activation, Context(activation));
                    fee.InfusionBurnCost = 3; fee.BlastBurnCost = 2; // frozen SC-22 B2/I3 input, no rule replacement
                    var data = new AbilityData(bp, x.unit.Descriptor);
                    var cost = fee.CalculateBurnCost(data);
                    Assert.Equal(0, cost.Total); Assert.Equal(3, cost.GatherPower);
                    Assert.Equal(3, part.GatherPowerRank); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                    Pay(data, Context(data));
                    Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
                    Assert.Equal(0, part.GatherPowerRank); Assert.Equal(0, part.AcceptedBurn);
                    Assert.False(x.unit.Buffs.HasFact(settings.GatherPowerBuff3));
                } finally { fee.InfusionBurnCost = originalI; fee.BlastBurnCost = originalB; x.unit.Progression.Features.RemoveFact(x.bridge); }
            }
            BridgeGame.Call(game.Bridge, "Verify");
        }

        [Fact]
        public void OrdinaryWitchCannotActivateOrAcquireThePrivateBridge() {
            game.RequireGame(); var unit = BridgeGame.Unit(6);
            unit.Progression.Classes.First(c => c.CharacterClass.AssetGuid.ToString() == "1b9873f1e7bfe5449bc84d03e9c8e3cc").Archetypes.Clear();
            var bridge = unit.Progression.Features.AddFeature(game.Copy<BlueprintFeature>(Burn));
            try { Assert.Null(unit.Get<UnitPartKineticist>()); Assert.False((bool)BridgeGame.Call(game.Api, "CanUseBridge", unit.Descriptor)); }
            finally { unit.Progression.Features.RemoveFact(bridge); }
        }

        [Fact]
        public void ForgedSameGuidPreparedBookIsNotTheBoundNativeWitchBook() {
            game.RequireGame(); var x = SpellUnit();
            var cls = x.unit.Progression.Classes.First(c => c.CharacterClass.AssetGuid.ToString() == "1b9873f1e7bfe5449bc84d03e9c8e3cc");
            var original = cls.Spellbook;
            try {
                var copy = (Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook)typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(original, null);
                cls.Spellbook = copy;
                Assert.Null(BridgeGame.Call(game.SpellApi, "BoundBook", x.unit.Descriptor));
            } finally { cls.Spellbook = original; x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Fact]
        public void NativePreparedSlotConversionMenuActuallyOffersAndPaysSpellburn() {
            game.RequireGame(); var x = SpellUnit();
            try {
                var target = BridgeGame.Native<BlueprintAbility>(Spellburn2);
                // Must already be offered by automatic fact activation. Do not
                // repair a missing list by invoking activation from the test.
                Assert.Contains(target, x.book.GetSpontaneousConversionSpells(2));
                var source = x.unit.Facts.List.Single(f => f.Blueprint.AssetGuid.ToString() == SpellburnFeature);
                var conversionRuntime = source.Components.Single(c => c.SourceBlueprintComponent is HavockerPreparedConversion0020);
                int count = ((IDictionary)BridgeGame.Field(x.book, "m_SpellConversionLists")).Count;
                using (conversionRuntime.RequestEventContext()) {
                    BridgeGame.Call(conversionRuntime.SourceBlueprintComponent, "OnTurnOn");
                    BridgeGame.Call(conversionRuntime.SourceBlueprintComponent, "OnTurnOn");
                }
                Assert.Equal(count, ((IDictionary)BridgeGame.Field(x.book, "m_SpellConversionLists")).Count);
                Assert.Empty(x.book.GetSpontaneousConversionSpells(0)); Assert.Empty(x.book.GetSpontaneousConversionSpells(1));
                var slot = Prepare(x.book, 2);
                var conversion = slot.SpellShell.GetConversions().Single(a => a.Blueprint == target);
                Assert.Same(slot.SpellShell, conversion.ConvertedFrom);
                Pay(conversion, Context(conversion)); Assert.False(slot.Available); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                x.unit.Progression.Features.RemoveFact(BridgeGame.Native<BlueprintFeature>(SpellburnFeature));
                Assert.Empty(x.book.GetSpontaneousConversionSpells(2));
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        (UnitEntityData unit, Spellbook book, Feature bridge, HavockerSpellburnState0020 state) SpellUnit(int level = 6, UnitEntityData fixture = null) {
            var unit = fixture ?? CleanUnit(level, true); unit.Stats.Intelligence.BaseValue = 20;
            var witch = BridgeGame.Native<BlueprintCharacterClass>("1b9873f1e7bfe5449bc84d03e9c8e3cc");
            unit.Progression.Classes.First(c => c.CharacterClass == witch).Spellbook = witch.Spellbook;
            var book = unit.Descriptor.DemandSpellbook(witch);
            while (book.CasterLevel < level) book.AddBaseLevel();
            var bridge = unit.Progression.Features.AddFeature(game.Copy<BlueprintFeature>(Burn));
            unit.Progression.Features.AddFeature(BridgeGame.Native<BlueprintFeature>(SpellburnFeature));
            var runtime = bridge.Components.Single(c => c.SourceBlueprintComponent is HavockerSpellburnController0020);
            return (unit, book, bridge, runtime.GetData<HavockerSpellburnState0020>());
        }
        static SpellSlot Prepare(Spellbook book, int level) {
            var blueprint = book.Blueprint.SpellList.SpellsByLevel.Single(l => l.SpellLevel == level).Spells.First();
            var known = book.AddKnown(level, blueprint);
            book.Rest(); // native slot allocation precedes memorization
            Assert.True(book.Memorize(known)); book.Rest();
            return book.GetMemorizedSpellSlots(level).First(s => s.SpellShell != null && s.Available);
        }
        static AbilityData Conversion(UnitEntityData unit, SpellSlot slot, string guid = Spellburn2) {
            var result = new AbilityData(BridgeGame.Native<BlueprintAbility>(guid), unit.Descriptor);
            result.ConvertedFrom = slot.SpellShell; return result;
        }
        static AbilityExecutionContext Context(AbilityData data) => new AbilityExecutionContext(data, AbilityParams.Empty, data.Caster.Unit);
        static void Pay(AbilityData data, AbilityExecutionContext context) { using (context.GetDataScope(data.Caster.Unit)) data.Spend(); }
        static void Lifecycle(Feature feature, string method) {
            var runtime = feature.Components.Single(c => c.SourceBlueprintComponent is HavockerSpellburnController0020);
            using (runtime.RequestEventContext()) runtime.SourceBlueprintComponent.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(runtime.SourceBlueprintComponent, null);
        }
        static Kingmaker.EntitySystem.EntityFact faultDisplay;
        static bool faultOnce;
        static void ThrowOnExactDisplayRemoval(Kingmaker.EntitySystem.EntityFact __instance) {
            if (faultOnce && ReferenceEquals(__instance, faultDisplay)) { faultOnce = false; throw new InvalidOperationException("SC-48 deliberate one-shot display removal failure"); }
        }

        [Fact]
        public void DisplayRemovalFailureNeverRestoresDiscountAndRetriesSeparately() {
            game.RequireGame(); var x = SpellUnit(); var harmony = new HarmonyLib.Harmony("CotW.Havocker.Tests.SC48");
            var remove = HarmonyLib.AccessTools.Method(typeof(Kingmaker.UnitLogic.Buffs.Buff), "Remove");
            try {
                var a = Conversion(x.unit, Prepare(x.book, 2)); Pay(a, Context(a));
                faultDisplay = x.state.Display.Fact; faultOnce = true;
                Assert.NotNull(remove);
                harmony.Patch(remove, prefix: new HarmonyLib.HarmonyMethod(typeof(BridgeTests), nameof(ThrowOnExactDisplayRemoval)));
                var data = new AbilityData(game.Copy<BlueprintAbility>("7d4712812818f094297f7d7920d130b1"), x.unit.Descriptor);
                Pay(data, Context(data));
                Assert.False(faultOnce); Assert.Equal(1, x.state.NativeAcceptEvents);
                Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status); Assert.Single(x.state.PendingDisplays);
                Assert.Equal(1, data.Blueprint.GetComponent<AbilityKineticist>().CalculateCost(data));
                Lifecycle(x.bridge, "OnNewRound"); Assert.Empty(x.state.PendingDisplays);
                Assert.True(faultDisplay.IsDisposed); Assert.Equal(HavockerChargeStatus0020.Absent, x.state.Status);
                game.Observations["SC-48 fault injection"] = "One removal exception on exact owned display, after real zero-burn Spend event; no renewed discount; subsequent lifecycle retry removed it.";
            } finally { harmony.UnpatchAll(harmony.Id); faultDisplay = null; faultOnce = false; x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Fact]
        public void NinthRingPaysOneRealSlotAndArmsFourCredit() {
            game.RequireGame(); var x = SpellUnit(17);
            try {
                var slot = Prepare(x.book, 9); var ability = Conversion(x.unit, slot, "ba84c5768f30ce02151117fd1837c7d0");
                Pay(ability, Context(ability)); Assert.False(slot.Available); Assert.Equal(4, x.state.Credit); Assert.Equal(9, x.state.SlotLevel);
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Fact]
        public void OldCastReceiptDoesNotConsumeLaterChargeAndAbilityDataCanBeReused() {
            game.RequireGame(); var x = SpellUnit();
            try {
                var a = Conversion(x.unit, Prepare(x.book, 2)); Pay(a, Context(a));
                var data = new AbilityData(game.Copy<BlueprintAbility>("7d4712812818f094297f7d7920d130b1"), x.unit.Descriptor);
                var old = Context(data); Pay(data, old);
                var b = Conversion(x.unit, Prepare(x.book, 3), "5a77da1936fef096fafab86ff24f65e2"); Pay(b, Context(b));
                long next = x.state.ChargeId; Pay(data, old);
                Assert.Equal(next, x.state.ChargeId); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                Pay(data, Context(data)); Assert.Equal(2, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status);
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Fact]
        public void ForeignCasterSlotAndConvertedFromCycleAreRejectedWithoutPayment() {
            game.RequireGame(); var x = SpellUnit(); var foreign = SpellUnit();
            try {
                var slot = Prepare(foreign.book, 2); var ability = Conversion(x.unit, slot);
                Assert.False(ability.Blueprint.GetComponent<HavockerSpellburnActivation0020>().IsAbilityRestrictionPassed(ability));
                Pay(ability, Context(ability)); Assert.True(slot.Available); Assert.Equal(HavockerChargeStatus0020.Absent, x.state.Status);
                BridgeGame.Set(ability, "m_ConvertedFrom", ability);
                Assert.False(ability.Blueprint.GetComponent<HavockerSpellburnActivation0020>().IsAbilityRestrictionPassed(ability));
                BridgeGame.Set(ability, "m_ConvertedFrom", null);
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); foreign.unit.Progression.Features.RemoveFact(foreign.bridge); }
        }

        [Fact]
        public void PostLoadKeepsAbsoluteDeadlineAndNeverRestoresConsumedCharge() {
            game.RequireGame(); var x = SpellUnit(); var player = Kingmaker.Game.Instance.Player; var start = player.GameTime;
            try {
                var a = Conversion(x.unit, Prepare(x.book, 2)); Pay(a, Context(a));
                var paidSlot = x.book.GetMemorizedSpellSlots(2).Single(s => !s.Available && s.SpellShell != null);
                player.GameTime += TimeSpan.FromSeconds(4);
                var deadline = x.state.Expires;
                var output = new System.IO.StringWriter();
                using (var writer = new Newtonsoft.Json.JsonTextWriter(output)) Kingmaker.Blueprints.JsonSystem.Json.Serializer.Serialize(writer, x.state, typeof(HavockerSpellburnState0020));
                game.Observations["Spellburn-state-json"] = output.ToString();
                var restored = (HavockerSpellburnState0020)Kingmaker.Blueprints.JsonSystem.Json.Serializer.Deserialize(new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(output.ToString())), typeof(HavockerSpellburnState0020));
                Assert.Equal(x.state.ChargeId, restored.ChargeId); Assert.Equal(x.state.Credit, restored.Credit);
                Assert.Equal(x.state.SourceSlot, restored.SourceSlot); Assert.Equal(deadline, restored.Expires);
                Assert.DoesNotContain("NativeAcceptEvents", output.ToString());
                foreach (var field in typeof(HavockerSpellburnState0020).GetFields().Where(f => f.IsDefined(typeof(Newtonsoft.Json.JsonPropertyAttribute), true))) field.SetValue(x.state, field.GetValue(restored));
                Lifecycle(x.bridge, "OnApplyPostLoadFixes"); Lifecycle(x.bridge, "OnApplyPostLoadFixes");
                Assert.Equal(deadline, x.state.Expires); Assert.False(paidSlot.Available); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                Assert.Equal(deadline, x.state.Display.Fact.EndTime);
                x.state.Status = HavockerChargeStatus0020.Consumed;
                Lifecycle(x.bridge, "OnApplyPostLoadFixes"); Assert.Equal(HavockerChargeStatus0020.Absent, x.state.Status); Assert.Null(x.state.Display.Fact);
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); player.GameTime = start; }
        }

        [Fact]
        public void NativeGameTimeDeadlineAndActualFactRemovalClearCharge() {
            game.RequireGame(); var x = SpellUnit(); var player = Kingmaker.Game.Instance.Player; var start = player.GameTime;
            try {
                var a = Conversion(x.unit, Prepare(x.book, 2)); Pay(a, Context(a));
                Assert.Equal(start + TimeSpan.FromSeconds(6), x.state.Display.Fact.EndTime);
                player.GameTime = start + TimeSpan.FromSeconds(5.999); Lifecycle(x.bridge, "OnNewRound");
                Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                player.GameTime = start + TimeSpan.FromSeconds(6); Lifecycle(x.bridge, "OnNewRound");
                Assert.Equal(HavockerChargeStatus0020.Absent, x.state.Status); Assert.Null(x.state.Display.Fact);
                var b = Conversion(x.unit, Prepare(x.book, 3), "5a77da1936fef096fafab86ff24f65e2"); Pay(b, Context(b));
                x.unit.Progression.Features.RemoveFact(x.bridge);
                Assert.Null(x.state.Display.Fact); Assert.Null(x.unit.Get<UnitPartKineticist>());
            } finally { if (x.unit.Facts.List.Contains(x.bridge)) x.unit.Progression.Features.RemoveFact(x.bridge); player.GameTime = start; }
        }

        [Fact]
        public void PreparedSlotThroughAbilitySpendThenRealZeroBurnSpendConsumesOnce() {
            game.RequireGame(); var x = SpellUnit();
            try {
                var slot = Prepare(x.book, 2); var conversion = Conversion(x.unit, slot); var activation = Context(conversion);
                Assert.True(conversion.Blueprint.GetComponent<HavockerSpellburnActivation0020>().IsAbilityRestrictionPassed(conversion));
                Pay(conversion, activation);
                Assert.False(slot.Available); Assert.Equal(1, x.state.Credit); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
                Assert.NotNull(x.state.Display.Fact); var expiry = x.state.Expires;
                var data = new AbilityData(game.Copy<BlueprintAbility>("7d4712812818f094297f7d7920d130b1"), x.unit.Descriptor);
                var cost = data.Blueprint.GetComponent<AbilityKineticist>();
                for (int i = 0; i < 20; i++) Assert.Equal(0, cost.CalculateCost(data));
                Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status); Assert.Equal(expiry, x.state.Expires);
                var cast = Context(data); Pay(data, cast);
                Assert.Equal(1, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Consumed, x.state.Status); Assert.Null(x.state.Display.Fact);
                Assert.Equal(0, x.unit.Get<UnitPartKineticist>().AcceptedBurn);
                Pay(data, cast); Assert.Equal(1, x.state.NativeAcceptEvents); // same native cast receipt is idempotent
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Fact]
        public void PendingChargeRejectsAnotherSlotWithoutChangingDeadline() {
            game.RequireGame(); var x = SpellUnit();
            try {
                var first = Prepare(x.book, 2); var second = Prepare(x.book, 3);
                var a = Conversion(x.unit, first); Pay(a, Context(a)); var expiry = x.state.Expires; long id = x.state.ChargeId;
                var b = Conversion(x.unit, second, "5a77da1936fef096fafab86ff24f65e2");
                Assert.False(b.Blueprint.GetComponent<HavockerSpellburnActivation0020>().IsAbilityRestrictionPassed(b));
                Pay(b, Context(b)); Assert.True(second.Available); Assert.Equal(id, x.state.ChargeId); Assert.Equal(expiry, x.state.Expires);
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Theory]
        [InlineData(2,0)] [InlineData(0,2)]
        public void SpellburnCannotPayBaseOrWildTalentBurn(int blast, int wild) {
            game.RequireGame(); var x = SpellUnit(); var bp = game.Copy<BlueprintAbility>("7d4712812818f094297f7d7920d130b1");
            var cost = bp.GetComponent<AbilityKineticist>(); int originalI = cost.InfusionBurnCost;
            try {
                var a = Conversion(x.unit, Prepare(x.book, 2)); Pay(a, Context(a));
                cost.InfusionBurnCost = 0; cost.BlastBurnCost = blast; cost.WildTalentBurnCost = wild;
                var data = new AbilityData(bp, x.unit.Descriptor);
                Assert.Equal(2, cost.CalculateCost(data)); Assert.False(cost.IsAbilityRestrictionPassed(data));
                Assert.Throws<InvalidOperationException>(() => Pay(data, Context(data)));
                Assert.Equal(0, x.state.NativeAcceptEvents); Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status);
            } finally { cost.InfusionBurnCost = originalI; cost.BlastBurnCost = 0; cost.WildTalentBurnCost = 0; x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Fact]
        public void SpecializationMakesZeroInfusionAndDoesNotConsumeCharge() {
            game.RequireGame(); var x = SpellUnit(10);
            try {
                var a = Conversion(x.unit, Prepare(x.book, 2)); Pay(a, Context(a)); var expiry = x.state.Expires;
                var data = new AbilityData(game.Copy<BlueprintAbility>("7d4712812818f094297f7d7920d130b1"), x.unit.Descriptor);
                Pay(data, Context(data)); Assert.Equal(1, x.state.NativeAcceptEvents);
                Assert.Equal(HavockerChargeStatus0020.Armed, x.state.Status); Assert.Equal(expiry, x.state.Expires);
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Fact]
        public void RestEventClearsOwnChargeAndKeepsAnotherConversionSource() {
            game.RequireGame(); var x = SpellUnit();
            try {
                var other = new BlueprintAbility[10]; other[2] = BridgeGame.Native<BlueprintAbility>(Spellburn2);
                x.book.AddSpellConversionList("Independent.Test.Source", other);
                var slot = Prepare(x.book, 2); var a = Conversion(x.unit, slot); Pay(a, Context(a));
                x.book.Rest(); EventBus.RaiseEvent((IUnitRestHandler h) => h.HandleUnitRest(x.unit));
                Assert.True(slot.Available); Assert.Equal(HavockerChargeStatus0020.Absent, x.state.Status); Assert.Null(x.state.Display.Fact);
                x.unit.Progression.Features.RemoveFact(BridgeGame.Native<BlueprintFeature>(SpellburnFeature));
                Assert.Contains(other[2], x.book.GetSpontaneousConversionSpells(slot.SpellShell));
                x.book.RemoveSpellConversionList("Independent.Test.Source");
            } finally { x.unit.Progression.Features.RemoveFact(x.bridge); }
        }

        [Fact]
        public void LoadImageOverloadsUseGraphicsStubOnPinnedHarmony() {
            game.RequireGame();
            Assert.Equal(new Version(2,0,4,0), typeof(HarmonyLib.Harmony).Assembly.GetName().Version);
            var type = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule", true);
            Assert.StartsWith(OfflineRuntime.RuntimeDirectory, type.Assembly.Location, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(OfflineRuntime.RuntimeDirectory, typeof(UnityEngine.Random).Assembly.Location, StringComparison.OrdinalIgnoreCase);
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public).Where(m => m.Name == "LoadImage").ToArray();
            Assert.Equal(new[] { 2, 3 }, methods.Select(m => m.GetParameters().Length).OrderBy(x => x));
            foreach (var method in methods) {
                var args = method.GetParameters().Length == 2 ? new object[] { null, Array.Empty<byte>() } : new object[] { null, Array.Empty<byte>(), false };
                Assert.Equal(false, method.Invoke(null, args));
            }
            game.Observations["LoadImage-on-game-Harmony"] = "Both 2/3-argument overloads returned false under 2.0.4.0; no native decoder or rules called.";
        }

        [Fact]
        public void NativeRandomStateBoundaryRestoresTheExistingSequence() {
            game.RequireGame(); var saved = GameRandom.Save();
            try {
                UnityEngine.Random.InitState(9); var state = UnityEngine.Random.state;
                int first = UnityEngine.Random.Range(0, 1000000);
                UnityEngine.Random.Range(0, 1000000);
                UnityEngine.Random.state = state;
                Assert.Equal(first, UnityEngine.Random.Range(0, 1000000));
            } finally { GameRandom.Restore(saved); }
        }

        [Fact]
        public void PrivateGrowthUsesWitchAndPrivateRankWithoutEditingNative() {
            game.RequireGame();
            var native = BridgeGame.Native<BlueprintProgression>(Growth); var own = game.Copy<BlueprintProgression>(Growth);
            Assert.NotSame(native, own); Assert.NotSame(native.LevelEntries, own.LevelEntries);
            Assert.Equal("42a455d9ec1ad924d889272429eb8391", native.Classes.Single().AssetGuid.ToString());
            Assert.Equal("1b9873f1e7bfe5449bc84d03e9c8e3cc", own.Classes.Single().AssetGuid.ToString());
            Assert.Equal(new[] { 1,3,5,7,9,11,13,15,17,19 }, own.LevelEntries.Select(x => x.Level));
            Assert.All(native.LevelEntries, row => Assert.Equal(Rank, row.Features.Single().AssetGuid.ToString()));
            Assert.All(own.LevelEntries, row => Assert.Same(game.Copy<BlueprintFeature>(Rank), row.Features.Single()));
            Assert.NotSame(native.LevelEntries[0].Features[0], own.LevelEntries[0].Features[0]);
            BridgeGame.Call(game.Bridge, "Publish"); // exact re-entry is idempotent
        }

        [Fact]
        public void SafeSettingsArePrivateIntAndZeroBurn() {
            game.RequireGame(); var own = game.Copy<BlueprintFeature>(Burn);
            var settings = own.GetComponent<AddKineticistPart>();
            Assert.IsType<CallOfTheWild.Wrath.Prototype.HavockerOwnedKineticistPart0020>(settings);
            Assert.Equal(StatType.Intelligence, settings.MainStat);
            Assert.Equal("1b9873f1e7bfe5449bc84d03e9c8e3cc", settings.Class.AssetGuid.ToString());
            var native = BridgeGame.Native<BlueprintFeature>(Burn).GetComponent<AddKineticistPart>();
            Assert.Equal(StatType.Constitution, native.MainStat); Assert.NotSame(native.MaxBurn, settings.MaxBurn);
            var unit = BridgeGame.Unit(1); unit.Stats.Intelligence.BaseValue = 18; unit.Stats.Constitution.BaseValue = 10;
            var feature = unit.Progression.Features.AddFeature(own);
            try {
                var part = unit.Get<UnitPartKineticist>();
                Assert.NotNull(part); Assert.Equal(StatType.Intelligence, part.MainStatType); Assert.Equal(4, part.MainStatBonus);
                Assert.Equal(1, part.ClassLevel); Assert.Equal(0, part.MaxBurn); Assert.Equal(0, part.MaxBurnPerRound);
                Assert.Equal(0, part.AcceptedBurn); Assert.Equal(0, part.AcceptedBurnThisRound);
            } finally { unit.Progression.Features.RemoveFact(feature); }
            Assert.Null(unit.Get<UnitPartKineticist>());
        }

        [Fact]
        public void ForeignSettingsTakeoverSurvivesOwnRemoval() {
            game.RequireGame(); var unit = BridgeGame.Unit(1);
            var feature = unit.Progression.Features.AddFeature(game.Copy<BlueprintFeature>(Burn));
            var part = unit.Get<UnitPartKineticist>(); Assert.NotNull(part);
            var foreign = BridgeGame.Native<BlueprintFeature>(Burn).GetComponent<AddKineticistPart>();
            part.Setup(foreign);
            unit.Progression.Features.RemoveFact(feature);
            Assert.Same(part, unit.Get<UnitPartKineticist>()); Assert.Equal(StatType.Constitution, part.MainStatType);
            Assert.Same(foreign, BridgeGame.Field(part, "m_Settings"));
        }

        [Fact]
        public void IllegalDualClassNeverCreatesOrOverwritesPart() {
            game.RequireGame(); var unit = BridgeGame.Unit(1, 1);
            var foreign = BridgeGame.Native<BlueprintFeature>(Burn).GetComponent<AddKineticistPart>();
            var part = unit.Ensure<UnitPartKineticist>(); part.Setup(foreign);
            var feature = unit.Progression.Features.AddFeature(game.Copy<BlueprintFeature>(Burn));
            Assert.Same(foreign, BridgeGame.Field(part, "m_Settings"));
            unit.Progression.Features.RemoveFact(feature);
            Assert.Same(part, unit.Get<UnitPartKineticist>());
        }

        [Fact]
        public void SealRejectsLateStagingAndDefinitionDrift() {
            game.RequireGame(); Assert.Throws<InvalidOperationException>(() => game.Stage<BlueprintFeature>(game.Bridge, Rank));
            var own = game.Copy<BlueprintFeature>(Rank); int ranks = own.Ranks;
            try { own.Ranks = ranks + 1; Assert.Throws<InvalidOperationException>(() => BridgeGame.Call(game.Bridge, "Verify")); }
            finally { own.Ranks = ranks; }
            BridgeGame.Call(game.Bridge, "Verify");
        }

        [Fact]
        public void AuthoredProgressionReferenceChangesStillFailAfterProxyReads() {
            game.RequireGame(); var row = game.Copy<BlueprintProgression>(Growth).LevelEntries[0];
            var raw = BridgeGame.Field(row, "m_Features");
            var changed = ((System.Collections.Generic.List<BlueprintFeatureBaseReference>)raw).ToList();
            changed.Clear(); // a genuine authored grant removal, not proxy warming
            try {
                BridgeGame.Set(row, "m_Features", changed);
                Assert.Throws<InvalidOperationException>(() => BridgeGame.Call(game.Bridge, "Verify"));
            } finally { BridgeGame.Set(row, "m_Features", raw); }
            BridgeGame.Call(game.Bridge, "Verify");
        }

        [Theory]
        [InlineData(9,0)] [InlineData(10,1)] [InlineData(12,2)] [InlineData(14,2)] [InlineData(16,3)] [InlineData(18,3)] [InlineData(20,4)]
        public void FrozenSpecializationSchedule(int level, int expected) { game.RequireGame(); Assert.Equal(expected, BridgeGame.Call(game.Api, "Specialization", level)); }
        [Theory]
        [InlineData(2,1)] [InlineData(3,1)] [InlineData(4,2)] [InlineData(5,2)] [InlineData(6,3)] [InlineData(7,3)] [InlineData(8,4)] [InlineData(9,4)]
        public void FrozenSpellburnCredit(int slot, int expected) { game.RequireGame(); Assert.Equal(expected, BridgeGame.Call(game.Api, "SpellburnCredit", slot)); }
    }
}
