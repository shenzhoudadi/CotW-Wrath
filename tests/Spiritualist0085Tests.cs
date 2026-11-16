using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View;
using Newtonsoft.Json;

// COTW-0085 B-stage directed checks, contract C-0085-20261115-r1.
// The host enters with COTW_0085_CASE=spiritualist0085 and routes straight
// here after the shared Psychic fixture; no historical behaviour suite runs.
//
// Scope: production blueprint registration, emotion and resource graphs,
// and native companion methods with pre-created managed entity fixtures.
// Unity spawning, the full AddClassLevels controller, placement, save/load, rest UI
// and the respec screen remain outside this host.
internal static class Spiritualist0085Tests
{
    private static int assertions;
    private static readonly List<string> Failures = new List<string>();
    private static Assembly Main => AppDomain.CurrentDomain.GetAssemblies()
        .First(a => a.GetName().Name == "CallOfTheWild.Wrath.Prototype");
    private static Type T(string name) => Main.GetType("CallOfTheWild.Wrath.Prototype." + name, true);
    private static object Get(object o, string f) => AccessTools.Field(o.GetType(), f)?.GetValue(o)
        ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")?.GetValue(o);
    private static void Set(object o, string f, object value)
        => (AccessTools.Field(o.GetType(), f) ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")).SetValue(o, value);
    private static object Call(string type, string method, params object[] args)
        => AccessTools.Method(T(type), method).Invoke(null, args);
    private static BlueprintGuid Id(string token) => (BlueprintGuid)Call("Spiritualist0085Ids", "Id", token);
    private static object IdsField(string name) => AccessTools.Field(T("Spiritualist0085Ids"), name).GetValue(null);
    private static bool NoTacticalCombat(ref bool __result)
    {
        __result = false;
        return false;
    }

    private static void Check(bool ok, string message)
    {
        if (ok) { assertions++; Console.WriteLine("0085 CHECK: " + message); }
        else { Failures.Add(message); Console.WriteLine("0085 CHECK FAILED: " + message); }
    }

    private static T Load<T>(BlueprintsCache cache, BlueprintGuid guid) where T : SimpleBlueprint => cache.Load(guid) as T;
    private static Kingmaker.Blueprints.Classes.Spells.SpellListComponent StagedSpellList(BlueprintSpellList list)
    {
        var c = new Kingmaker.Blueprints.Classes.Spells.SpellListComponent { name = "Staged0085List", SpellLevel = 1 };
        AccessTools.Field(c.GetType(), "m_SpellList").SetValue(c, BlueprintReferenceBase.CreateTyped<BlueprintSpellListReference>(list));
        return c;
    }
    private static BlueprintComponent Component(BlueprintScriptableObject bp, string typeName)
        => bp?.ComponentsArray?.FirstOrDefault(c => c != null && c.GetType().Name == typeName);
    private static IEnumerable<BlueprintComponent> Components(BlueprintScriptableObject bp, string typeName)
        => bp?.ComponentsArray?.Where(c => c != null && c.GetType().Name == typeName) ?? Enumerable.Empty<BlueprintComponent>();
    private static int Rank(ContextRankConfig config, int level)
        => (int)AccessTools.Method(typeof(ContextRankConfig), "ApplyProgression").Invoke(config, new object[] { level });

    private static void CheckInferenceExit(AbilityAreaEffectBuff effect, Harmony harmony)
    {
        TFact Empty<TFact>() => (TFact)FormatterServices.GetUninitializedObject(typeof(TFact));
        var target = Empty<UnitEntityData>();
        var facts = new EntityFactsManager(target);
        Set(target, "Facts", facts);
        var buffs = new BuffCollection();
        buffs.Initialize(facts);
        Set(target, "Buffs", buffs);
        var oldArea = Empty<AreaEffectEntityData>();
        Set(oldArea, "UniqueId", "old-inference");
        foreach (var source in new[] { oldArea.UniqueId, "recreated-inference", "second-phantom" })
        {
            var buff = Empty<Buff>();
            Set(buff, "Blueprint", effect.Buff);
            Set(buff, "Components", new List<EntityFactComponent>());
            buff.SourceAreaEffectId = source;
            buff.RestoreOwnerLink(facts);
            facts.List.Add(buff);
        }
        var remove = AccessTools.Method(typeof(Buff), nameof(Buff.Remove));
        var removeFixture = AccessTools.Method(typeof(Spiritualist0085Tests), nameof(RemoveInferenceFixtureBuff));
        harmony.Patch(remove, prefix: new HarmonyMethod(removeFixture));
        try
        {
            effect.HandleUnitExit(null, oldArea, target);
            effect.HandleUnitExit(null, oldArea, target);
            Check(facts.List.OfType<Buff>().Select(buff => buff.SourceAreaEffectId)
                .SequenceEqual(new[] { "recreated-inference", "second-phantom" }),
                "C15: native exit removes only the retired area, preserving its replacement and a second phantom's aura");
        }
        finally { harmony.Unpatch(remove, removeFixture); }
    }

    private static bool RemoveInferenceFixtureBuff(Buff __instance)
    {
        var facts = __instance.Manager;
        facts.List.Remove(__instance);
        var cache = Get(facts, "m_Cache");
        AccessTools.Method(cache.GetType(), "Clear").Invoke(cache, null);
        return false;
    }

    private static UnitEntityData ManagedFixtureUnit(string id)
    {
        var unit = (UnitEntityData)FormatterServices.GetUninitializedObject(typeof(UnitEntityData));
        var descriptor = (UnitDescriptor)FormatterServices.GetUninitializedObject(typeof(UnitDescriptor));
        Set(unit, "UniqueId", id);
        Set(unit, "Descriptor", descriptor);
        Set(descriptor, "Unit", unit);
        Set(descriptor, "State", new UnitState(descriptor));
        Set(unit, "Parts", new EntityPartsManager(unit));
        var proxy = FormatterServices.GetUninitializedObject(AccessTools.Inner(typeof(EntityService), "Proxy"));
        Set(proxy, "Entity", unit);
        Set(unit, "Proxy", proxy);
        return unit;
    }

    private static void CheckRecoveryVisibility(Harmony harmony)
    {
        var master = ManagedFixtureUnit("0085-recovery-master");
        var pet = ManagedFixtureUnit("0085-recovery-pet");
        Set(master, "m_IsInGame", true);
        Set(master.Descriptor, "m_IsPlayerFactionCached", true);
        Set(master.Descriptor, "AttackFactions", new UnitAttackFactions(master.Descriptor));
        var petPart = pet.Ensure<UnitPartPet>();
        Set(petPart, "m_MasterRef", new EntityPartRef<UnitEntityData, UnitPartPetMaster>(master));
        Set(petPart, "m_ShouldBeHidden", true);

        var link = T("Phantom0085Link");
        Check(typeof(IUnitLifeStateChanged).IsAssignableFrom(link),
            "C06: the primary emotion link listens to native life-state recovery");
        var part = Activator.CreateInstance(T("UnitPartPhantom0085"));
        Set(part, "Confined", true);
        Call("Phantom0085Link", "RestoreAfterRecovery", master, pet, part);
        Check(!(bool)Get(part, "Confined") && (bool)Get(part, "PendingManifest") && petPart.ShouldBeHidden && !pet.IsInGame,
            "C06/C07: recovery without a local scene clears confinement and defers visibility");

        var local = (SceneEntitiesState)FormatterServices.GetUninitializedObject(typeof(SceneEntitiesState));
        Set(master, "HoldingState", local);
        Set(pet, "HoldingState", local);
        var sceneCheck = AccessTools.Method(link, "CanPlace");
        var viewRefresh = AccessTools.Method(typeof(UnitEntityData), "OnIsInGameChanged");
        var allowScene = AccessTools.Method(typeof(Spiritualist0085Tests), nameof(AllowRecoveryFixturePlacement));
        var skipView = AccessTools.Method(typeof(Spiritualist0085Tests), nameof(SkipRecoveryFixtureView));
        harmony.Patch(sceneCheck, prefix: new HarmonyMethod(allowScene));
        harmony.Patch(viewRefresh, prefix: new HarmonyMethod(skipView));
        try
        {
            Set(master.Descriptor, "m_IsPlayerFactionCached", false);
            Call("Phantom0085Link", "RestoreAfterRecovery", master, pet, part);
            Check((bool)Get(part, "PendingManifest") && petPart.ShouldBeHidden && !pet.IsInGame,
                "C06: the recovery bridge respects native visibility eligibility for the master");
            Set(master.Descriptor, "m_IsPlayerFactionCached", true);
            Call("Phantom0085Link", "RestoreAfterRecovery", master, pet, part);
            Call("Phantom0085Link", "RestoreAfterRecovery", master, pet, part);
            Check(!(bool)Get(part, "PendingManifest") && !petPart.ShouldBeHidden && pet.IsInGame && petPart.Master == master,
                "C06/C07: native SetHidden restores the same linked pet and recovery replay keeps it visible");
        }
        finally
        {
            harmony.Unpatch(viewRefresh, skipView);
            harmony.Unpatch(sceneCheck, allowScene);
        }
    }

    private static bool AllowRecoveryFixturePlacement(ref bool __result)
    {
        __result = true;
        return false;
    }

    private static bool SkipRecoveryFixtureView() => false;

    private static void CheckNativeMountAndRetirement(Harmony harmony, BlueprintUnit phantomBlueprint, BlueprintUnit dogBlueprint, PetType privateType)
    {
        var master = ManagedFixtureUnit("0085-mount-master");
        var otherMaster = ManagedFixtureUnit("0085-mount-other-master");
        UnitEntityData Pet(string id, BlueprintUnit blueprint, PetType type)
        {
            var unit = ManagedFixtureUnit(id);
            Set(unit.Descriptor, "Blueprint", blueprint);
            var facts = new EntityFactsManager(unit);
            Set(unit, "Facts", facts);
            Set(unit, "Buffs", facts.EnsureFactProcessor<BuffCollection>());
            var part = unit.Ensure<UnitPartPet>();
            Set(part, "m_MasterRef", new EntityPartRef<UnitEntityData, UnitPartPetMaster>(master));
            Set(part, "Type", type);
            return unit;
        }

        var phantom = Pet("0085-mount-phantom", phantomBlueprint, privateType);
        var dog = Pet("0085-mount-dog", dogBlueprint, PetType.AnimalCompanion);
        var collision = Pet("0085-mount-foreign", dogBlueprint, privateType);
        const string mountButton = "Phantom0085Patches+MountSuppression";
        PetType MountType(UnitEntityData unit) => (PetType)Call(mountButton, "NativeMountType", unit.Get<UnitPartPet>());
        var updateMount = AccessTools.Method(typeof(UnitPartPetMaster), "UpdateMountAbility");
        var rewritten = (IEnumerable<CodeInstruction>)Call(mountButton, "Transpiler", PatchProcessor.GetOriginalInstructions(updateMount));
        Check(rewritten.Count(i => i.Calls(AccessTools.Method(T(mountButton), "NativeMountType"))) == 2 &&
            MountType(phantom) == PetType.Clone && phantom.Get<UnitPartPet>().Type == privateType &&
            MountType(dog) == PetType.AnimalCompanion && MountType(collision) == privateType,
            "C17: native mount-button criteria exclude only the private marked phantom, preserving Dog and same-type foreign pets");

        var nativeMount = AccessTools.Method(typeof(AbilityTargetIsSuitableMount), nameof(AbilityTargetIsSuitableMount.CanMount));
        var prefix = AccessTools.Method(T("Phantom0085Patches+MountTargetExclusion"), "Prefix");
        harmony.Patch(nativeMount, prefix: new HarmonyMethod(prefix));
        try
        {
            Check(!AbilityTargetIsSuitableMount.CanMount(master, phantom) &&
                AbilityTargetIsSuitableMount.CanMount(master, dog) && AbilityTargetIsSuitableMount.CanMount(master, collision) &&
                !AbilityTargetIsSuitableMount.CanMount(otherMaster, dog),
                "C17: native rider predicate excludes Phantom and retains native ownership checks for Dog and foreign pets");
        }
        finally { harmony.Unpatch(nativeMount, prefix); }

        bool Retired(UnitEntityData unit) => (bool)Call("Phantom0085Patches+ExistingPetReference", "RetiredRecordedPet", unit);
        phantom.MarkForDestroy();
        Check(!Retired(phantom) && !Retired(dog), "C09: a still-bound pet can use native detach cleanup");
        phantom.Get<UnitPartPet>().Clear();
        Check(Retired(phantom) && !Retired(dog) && dog.Master == master,
            "C09: an already detached and destroy-marked private pet skips repeated cleanup, leaving Dog's binding untouched");
    }

    private static readonly List<(AddClassLevels Source, UnitEntityData Pet, int Levels)> FixtureLevelRequests =
        new List<(AddClassLevels, UnitEntityData, int)>();

    private static bool ObserveFixtureLevelUp(AddClassLevels __0, UnitDescriptor __1, int __2)
    {
        FixtureLevelRequests.Add((__0, __1.Unit, __2));
        return false;
    }

    private static void EnsureFixtureFactId(EntityFact __instance)
    {
        if (string.IsNullOrEmpty(__instance.UniqueId))
            Set(__instance, "UniqueId", Guid.NewGuid().ToString("N"));
    }

    private static void CheckNativePetBinding(BlueprintCharacterClass spiritualist, BlueprintCharacterClass unrelated, BlueprintFeature anger,
        BlueprintFeature dogFeature, BlueprintFaction neutral)
    {
        var scene = (SceneEntitiesState)FormatterServices.GetUninitializedObject(typeof(SceneEntitiesState));
        var masterBlueprint = new BlueprintUnit { name = "0085-managed-owner", ComponentsArray = Array.Empty<BlueprintComponent>() };
        UnitEntityData Actor(string id, BlueprintUnit blueprint, bool owner = false)
        {
            var unit = ManagedFixtureUnit(id);
            Set(unit.Descriptor, "Blueprint", blueprint);
            Set(unit.Descriptor, "OriginalBlueprint", blueprint);
            Set(unit.Descriptor, "m_Faction", neutral);
            Set(unit.Descriptor, "m_IsPlayerFactionCached", false);
            Set(unit.Descriptor, "AttackFactions", new UnitAttackFactions(unit.Descriptor));
            var alignment = new UnitAlignment(unit.Descriptor);
            Set(alignment, "m_Value", Alignment.TrueNeutral);
            Set(unit.Descriptor, "Alignment", alignment);
            Set(unit, "Passive", new ObservableCountFlag());
            var facts = new EntityFactsManager(unit);
            Set(unit, "Facts", facts);
            Set(unit, "Buffs", facts.EnsureFactProcessor<BuffCollection>());
            Set(unit, "ActivatableAbilities", facts.EnsureFactProcessor<ActivatableAbilityCollection>());
            var progression = new UnitProgressionData();
            Set(unit.Descriptor, "Progression", progression);
            Set(progression, "Owner", unit.Descriptor);
            var classes = new List<ClassData>();
            Set(progression, "Classes", classes);
            Set(progression, "Features", facts.EnsureFactProcessor<FeatureCollection>());
            if (owner)
            {
                Set(unit, "HoldingState", scene);
                classes.Add(new ClassData(spiritualist) { Level = 1 });
            }
            return unit;
        }

        var a = Actor("0085-binding-a", masterBlueprint, owner: true);
        var b = Actor("0085-binding-b", masterBlueprint, owner: true);
        var petA = Actor("0085-binding-phantom-a", anger.GetComponent<AddPet>().Pet);
        var petB = Actor("0085-binding-phantom-b", anger.GetComponent<AddPet>().Pet);
        var dog = Actor("0085-binding-dog", dogFeature.GetComponent<AddPet>().Pet);
        var phantomLevels = petA.Blueprint.GetComponent<AddClassLevels>();
        foreach (var pet in new[] { petA, petB })
        {
            var unitFact = new UnitFact(pet.Blueprint);
            unitFact.RestoreOwnerLink(pet.Facts);
            pet.Facts.List.Add(unitFact);
        }
        BlueprintComponentAndRuntime<AddPet> Record(UnitEntityData owner, BlueprintFeature blueprint, UnitEntityData pet)
        {
            var fact = new Feature(blueprint, owner.Descriptor);
            fact.RestoreOwnerLink(owner.Facts);
            owner.Facts.List.Add(fact);
            var grant = fact.GetComponentWithRuntime<AddPet>();
            grant.GetData<AddPetData>().SpawnedPetRef = pet;
            return grant;
        }
        var sourceA = Record(a, anger, petA);
        var sourceB = Record(b, anger, petB);
        var dogSource = Record(a, dogFeature, dog);
        var getPart = AccessTools.Method(typeof(EntityPartsManager), "Get").MakeGenericMethod(T("UnitPartPhantom0085"));
        UnitEntityData Bound(UnitEntityData owner, object part)
            => part == null ? null : (UnitEntityData)AccessTools.Method(part.GetType(), "BoundPet").Invoke(part, new object[] { owner });
        void Update(BlueprintComponentAndRuntime<AddPet> grant)
        {
            using (grant.Runtime.RequestEventContext()) ((IUpdatePet)grant.Component).TryUpdatePet();
        }

        // Seed the recorded references because spawning a Unity view is outside this host.
        // The native binding, re-entry, ownership checks and detach below are executed.
        var native = new Harmony("CotWSpiritualist0085.PetFixture");
        var gameField = AccessTools.Field(typeof(Game), "s_Instance");
        var previousGame = gameField.GetValue(null);
        var game = (Game)FormatterServices.GetUninitializedObject(typeof(Game));
        var stateField = AccessTools.Field(typeof(Game), "State");
        var state = FormatterServices.GetUninitializedObject(stateField.FieldType);
        var player = (Player)FormatterServices.GetUninitializedObject(typeof(Player));
        Set(player, "PartyCharacters", new List<UnitReference>());
        Set(player, "m_Party", new List<UnitEntityData>());
        Set(player, "m_CharacterListsValid", true);
        Set(state, "PlayerState", player);
        stateField.SetValue(game, state);
        Set(game, "TimeController", new Kingmaker.Controllers.TimeController());
        gameField.SetValue(null, game);
        var progressionRoot = game.BlueprintRoot.Progression;
        var oldXP = Get(progressionRoot, "m_XPTable");
        var nativeXP = ResourcesLibrary.TryGetBlueprint<BlueprintStatProgression>(BlueprintGuid.Parse("87c24ce6bcf1a994296f3c582c1a632b"));
        try
        {
            Set(progressionRoot, "m_XPTable", BlueprintReferenceBase.CreateTyped<BlueprintStatProgressionReference>(nativeXP));
            var omitScene = new HarmonyMethod(typeof(Spiritualist0085Tests), nameof(SkipRecoveryFixtureView));
            native.Patch(AccessTools.Method(typeof(UnitEntityData), "UpdateGroup"), prefix: omitScene);
            native.Patch(AccessTools.Method(typeof(UnitPartPetMaster), "UpdateMountAbility"), prefix: omitScene);
            native.Patch(AccessTools.Method(typeof(UnitEntityData), "OnIsInGameChanged"), prefix: omitScene);
            native.Patch(AccessTools.Method(typeof(UnitEntityData), nameof(UnitEntityData.SetMaster)),
                transpiler: new HarmonyMethod(typeof(Spiritualist0085Tests), nameof(BoundFixtureScale)));
            native.Patch(AccessTools.Method(typeof(EntityFact), nameof(EntityFact.Attach)),
                prefix: new HarmonyMethod(typeof(Spiritualist0085Tests), nameof(EnsureFixtureFactId)));
            native.CreateClassProcessor(T("Phantom0085Patches+ExistingPetReference")).Patch();
            native.CreateClassProcessor(T("Phantom0085Patches+PetLevelOverride")).Patch();
            native.Patch(AccessTools.Method(typeof(AddClassLevels), nameof(AddClassLevels.LevelUp),
                new[] { typeof(AddClassLevels), typeof(UnitDescriptor), typeof(int), typeof(UnitFact) }),
                prefix: new HarmonyMethod(typeof(Spiritualist0085Tests), nameof(ObserveFixtureLevelUp)));
            a.Facts.TurnOn();
            b.Facts.TurnOn();
            foreach (var grant in new[] { sourceA, dogSource, sourceB })
                grant.Runtime.Fact.Activate();
            var partA = getPart.Invoke(a.Parts, null);
            var partB = getPart.Invoke(b.Parts, null);
            Check(partA != null && partB != null && (string)Get(partA, "PetId") == petA.UniqueId &&
                (string)Get(partB, "PetId") == petB.UniqueId,
                "C02: the initial native fact activation tracks both phantoms before any area or user action");
            Check(FixtureLevelRequests.Count == 2 && FixtureLevelRequests.All(request =>
                    request.Source == phantomLevels && request.Levels == 1) &&
                FixtureLevelRequests.Select(request => request.Pet).SequenceEqual(new[] { petA, petB }),
                "C02: the native AddPet path requests exactly one initial level from each phantom's own donor");
            var petClass = new ClassData(phantomLevels.CharacterClass) { Level = 1 };
            petA.Progression.Classes.Add(petClass);
            Set(petA.Progression, "m_CharacterLevel", 1);
            Set(petA.Descriptor, "Stats", new CharacterStats(petA.Descriptor));
            Set(petB.Descriptor, "Stats", new CharacterStats(petB.Descriptor));
            var skillBlueprint = phantomLevels.Archetypes.Single().AddFeatures.Single(entry => entry.Level == 1)
                .Features.OfType<BlueprintFeature>().Single(feature => Component(feature, "Phantom0085SkillRanks") != null);
            var skillFact = new Feature(skillBlueprint, petA.Descriptor);
            skillFact.RestoreOwnerLink(petA.Facts);
            petA.Facts.List.Add(skillFact);
            petA.Facts.TurnOn();
            skillFact.Activate();
            petA.Stats.SkillAthletics.BaseValue = 1;
            Check(skillFact.IsTurnedOn && petA.Stats.SkillPersuasion.BaseValue == 1 && petA.Stats.SkillLoreNature.BaseValue == 1,
                "C03: Anger starts with one rank in each emotion skill through native fact activation");
            FixtureLevelRequests.Clear();
            Update(sourceA);
            Check(a.Pets.Count == 2 && b.Pets.Count == 1 && petA.Master == a && petB.Master == b && dog.Master == a &&
                Bound(a, partA) == petA && Bound(b, partB) == petB && FixtureLevelRequests.Count == 0,
                "C02/C17: native AddPet update and SetMaster retain separate Anger bindings and the same-owner Dog on replay");
            var ownerClass = a.Progression.Classes.Single();
            ownerClass.Level = 2;
            sourceA.Runtime.Fact.Reapply();
            Check(FixtureLevelRequests.SequenceEqual(new[] { (phantomLevels, petA, 1) }) &&
                sourceA.Runtime.Fact.IsTurnedOn && Bound(a, partA) == petA,
                "C03: native reapplication asks for one Phantom level on the same entity when its owner reaches Spiritualist 2");
            petB.Descriptor.OnGainClassLevel(phantomLevels.CharacterClass);
            Check(petA.Stats.SkillPersuasion.BaseValue == 1 && petA.Stats.SkillLoreNature.BaseValue == 1,
                "C03/C17: another phantom's native gain-level event cannot update Anger's skills");
            petA.Progression.AddClassLevel(phantomLevels.CharacterClass);
            petA.Descriptor.OnGainClassLevel(phantomLevels.CharacterClass);
            Check(petA.Progression.GetClassLevel(phantomLevels.CharacterClass) == 2 &&
                petA.Stats.SkillPersuasion.BaseValue == 2 && petA.Stats.SkillLoreNature.BaseValue == 2 &&
                petA.Stats.SkillAthletics.BaseValue == 1,
                "C03: native AddClassLevel advances to level 2, updates only Anger's emotion skills, and gain-level replay does not add ranks");
            FixtureLevelRequests.Clear();
            a.Progression.Classes.Add(new ClassData(unrelated) { Level = 4 });
            sourceA.Runtime.Fact.Reapply();
            Check(FixtureLevelRequests.Count == 0 && Bound(a, partA) == petA,
                "C03: four unrelated class levels leave the native Phantom request unchanged");
            var saveSettings = new JsonSerializerSettings { ContractResolver = new OptInContractResolver() };
            Set(partA, "Confined", true);
            petA.Get<UnitPartPet>().SetHidden(true);
            var savedPhantomState = JsonConvert.SerializeObject(partA, saveSettings);
            petA.RemoveMaster();
            JsonConvert.PopulateObject(savedPhantomState, partA, saveSettings);
            Set(a, "m_IsInGame", true);
            Update(sourceA);
            Check((bool)Get(partA, "Confined") && (string)Get(partA, "PetId") == petA.UniqueId &&
                petA.Get<UnitPartPet>().ShouldBeHidden && !petA.IsInGame && Bound(a, partA) == petA,
                "C08: the native recorded-pet reconnect preserves the saved confinement and the same entity");
            petA.IsInGame = true;
            EventBus.RaiseEvent<IAreaLoadingStagesHandler>(handler => handler.OnAreaLoadingComplete());
            EventBus.RaiseEvent<IAreaLoadingStagesHandler>(handler => handler.OnAreaLoadingComplete());
            Check(!petA.IsInGame && petA.Get<UnitPartPet>().ShouldBeHidden && a.Pets.Count == 2 && dog.Master == a &&
                petB.Master == b && !dog.Get<UnitPartPet>().ShouldBeHidden && !petB.Get<UnitPartPet>().ShouldBeHidden,
                "C08/C17: the native area completion restores a confined phantom even if its hidden flag already exists");
            petA.State.UpdateLifeState(UnitLifeState.Dead);
            petA.Get<UnitPartPet>().SetHidden(false);
            petA.IsInGame = true;
            EventBus.RaiseEvent<IAreaLoadingStagesHandler>(handler => handler.OnAreaLoadingComplete());
            Check(petA.State.IsDead && petA.IsInGame && !petA.Get<UnitPartPet>().ShouldBeHidden,
                "C08: the area visibility projection leaves a native dead phantom available for recovery");
            petA.State.UpdateLifeState(UnitLifeState.Conscious);
            dog.RemoveMaster();
            Update(dogSource);
            Check(dog.Master == a && dog.IsInGame && !dog.Get<UnitPartPet>().ShouldBeHidden,
                "C08/C17: native Dog reconnect still inherits its master's scene visibility");
            ownerClass.Level = 20;
            sourceA.Runtime.Fact.Reapply();
            Check(FixtureLevelRequests.SequenceEqual(new[] { (phantomLevels, petA, 18) }) && Bound(a, partA) == petA,
                "C03: native reapplication requests the remaining 18 levels at Spiritualist 20 on the same entity");
            FixtureLevelRequests.Clear();
            sourceA.GetData<AddPetData>().SpawnedPetRef = petB;
            Update(sourceA);
            Check(Bound(a, partA) == null && Bound(b, partB) == petB && a.Pets.Count == 2 && petB.Master == b,
                "C17: a cross-owner recorded phantom is rejected without touching either native pet list");
            sourceA.GetData<AddPetData>().SpawnedPetRef = petA;
            petA.RemoveMaster();
            petA.MarkForDestroy();
            Update(sourceA);
            Check(Bound(a, partA) == null && Bound(b, partB) == petB && a.Pets.Single().Entity == dog &&
                dog.Master == a && petA.Master == null && petA.DestroyMark,
                "C09/C17: native detach followed by a stale update cannot reattach the retired Phantom or remove Dog");
        }
        finally
        {
            petA.Facts.TurnOff();
            FixtureLevelRequests.Clear();
            foreach (var grant in new[] { sourceA, dogSource, sourceB })
                if (grant.Runtime.Fact.IsTurnedOn) grant.Runtime.Fact.TurnOff();
            native.UnpatchAll(native.Id);
            Set(progressionRoot, "m_XPTable", oldXP);
            gameField.SetValue(null, previousGame);
        }
    }

    private static IEnumerable<CodeInstruction> BoundFixtureScale(IEnumerable<CodeInstruction> source)
    {
        var instructions = source.ToList();
        var scale = AccessTools.Method(typeof(UnitEntityView), "UpdateScaleImmediately");
        var call = instructions.Single(i => i.Calls(scale));
        call.opcode = System.Reflection.Emit.OpCodes.Pop;
        call.operand = null;
        return instructions;
    }

    private static void FailPrivateInsert(SimpleBlueprint __1)
    {
        if (__1.AssetGuid == Id("PhantomClass")) throw new InvalidOperationException("0085 test: late insertion fault");
    }

    internal static int Run(BlueprintsCache cache, Harmony harmony)
    {
        const string prefix = "CotWWrathSpiritualist0085";
        const string druidId = "610d836f3a3a9ed42a4349b62f002e96";
        var druid = Load<BlueprintCharacterClass>(cache, BlueprintGuid.Parse(druidId));
        var druidRow = PsychicExpansionTests.NativeRows.Single(row => (string)row["blueprint"]["AssetId"] == druidId);
        druid.SkillPoints = (int)druidRow["blueprint"]["Data"]["SkillPoints"];
        var vampiricId = BlueprintGuid.Parse("8a28a811ca5d20d49a863e832c31cce1");
        var freedomId = BlueprintGuid.Parse("0087fc2d64b6095478bc7b8d7d512caf");
        foreach (var id in new[] { vampiricId, freedomId })
        {
            var ability = Load<BlueprintAbility>(cache, id);
            if (ability.GetComponent<AbilityEffectStickyTouch>() != null) continue;
            var row = PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == id.ToString());
            ability.ComponentsArray = row["blueprint"]["Data"]["Components"]
                .Select(c => (BlueprintComponent)PsychicExpansionTests.Read(c, typeof(BlueprintComponent))).ToArray();
            ability.OnEnable();
        }

        // ---- Stage the audited optional SLA roots (provider/CotW identities
        // from A-spiritualist-assets) so the atomic gate can be exercised offline.
        // Real provider behaviour remains a D / player-TEST boundary.
        var emotionsForStaging = ((Array)IdsField("Emotions")).Cast<object>().ToArray();
        foreach (var row in emotionsForStaging)
        {
            var slaGuids = (string[])Get(row, "SlaGuids");
            var slaProviders = (string[])Get(row, "SlaProviders");
            for (int i = 0; i < 4; i++)
            {
                BlueprintGuid slaGuid;
                var provider = slaProviders[i];
                if (provider == null) slaGuid = BlueprintGuid.Parse(slaGuids[i]);
                else if (provider.StartsWith("CotW:")) slaGuid = (BlueprintGuid)Call("PsychicAssets", "Id", provider.Substring(5));
                else
                {
                    var link = ((Array)AccessTools.Field(T("PsychicSharedSpellCatalog"), "Entries").GetValue(null))
                        .Cast<object>().FirstOrDefault(e => (string)Get(e, "Name") == provider);
                    if (link == null) continue;
                    slaGuid = BlueprintGuid.Parse((string)Get(link, "Guid"));
                }
                if (cache.Load(slaGuid) == null)
                {
                    var stub = new BlueprintAbility { AssetGuid = slaGuid, name = "Staged0085Sla" + slaGuid, Type = AbilityType.Spell };
                    if (provider != null && !provider.StartsWith("CotW:"))
                    {
                        // Match the audited shared-catalog identity so the
                        // production Available gate accepts the staged root.
                        var link2 = ((Array)AccessTools.Field(T("PsychicSharedSpellCatalog"), "Entries").GetValue(null))
                            .Cast<object>().First(e => (string)Get(e, "Name") == provider);
                        stub.name = (string)Get(link2, "Name");
                        var school = (Kingmaker.Blueprints.Classes.Spells.SpellSchool)Get(link2, "School");
                        // Published-list providers also need a reciprocal list
                        // publication; stage a private one-spell list for them.
                        var stagedList = new BlueprintSpellList { AssetGuid = BlueprintGuid.Parse("0085" + slaGuid.ToString().Substring(4)), name = "Staged0085ProviderList" + slaGuid };
                        var levelRow = new Kingmaker.Blueprints.Classes.Spells.SpellLevelList(1);
                        AccessTools.Field(levelRow.GetType(), "m_Spells").SetValue(levelRow, new System.Collections.Generic.List<BlueprintAbilityReference> { BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(stub) });
                        stagedList.SpellsByLevel = new[] { levelRow };
                        stagedList.OnEnable();
                        cache.AddCachedBlueprint(stagedList.AssetGuid, stagedList);
                        stub.ComponentsArray = new BlueprintComponent[]
                        {
                            new Kingmaker.Blueprints.Classes.Spells.SpellComponent { name = "Staged0085School", School = school },
                            new Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction { name = "Staged0085Run", Actions = new Kingmaker.ElementsSystem.ActionList { Actions = Array.Empty<Kingmaker.ElementsSystem.GameAction>() } },
                            StagedSpellList(stagedList)
                        };
                    }
                    stub.OnEnable();
                    cache.AddCachedBlueprint(slaGuid, stub);
                }
            }
        }

        // ---- Registration gate and idempotency (C01) ----
        var faultPoint = AccessTools.Method(typeof(BlueprintsCache), "AddCachedBlueprint");
        var fault = AccessTools.Method(typeof(Spiritualist0085Tests), nameof(FailPrivateInsert));
        harmony.Patch(faultPoint, postfix: new HarmonyMethod(fault));
        try
        {
            Check(!(bool)Call("Spiritualist0085", "Register", cache), "C01: injected late insertion failure rejects registration");
            Check(cache.Load(Id("Spiritualist0085Class")) == null && cache.Load(Id("PhantomClass")) == null,
                "C01: failure rolls back both inserted class blueprints");
        }
        finally
        {
            harmony.Unpatch(faultPoint, fault);
        }
        bool first = (bool)Call("Spiritualist0085", "Register", cache);
        Check(first, "registration publishes with all native donors present");
        if (!first)
        {
            Console.WriteLine("0085 NOT RUN: registration rejected: " + AccessTools.Field(T("Spiritualist0085"), "LastSummary").GetValue(null));
            throw new InvalidOperationException("0085 retry failed to publish the class.");
        }
        var cls = Load<BlueprintCharacterClass>(cache, Id("Spiritualist0085Class"));
        Check(cls != null, "private class published");
        bool second = (bool)Call("Spiritualist0085", "Register", cache);
        Check(second && ReferenceEquals(cache.Load(cls.AssetGuid), cls), "second registration is idempotent and preserves identity");

        // ---- Master class shape ----
        var inquisitor = Load<BlueprintCharacterClass>(cache, BlueprintGuid.Parse("f1a70d9e1b0b41e49874e1fa9052a1ce"));
        Check(!cls.IsArcaneCaster && !cls.IsDivineCaster && !cls.PrestigeClass && !cls.HideInUI, "playable non-arcane non-divine base class");
        Check(cls.SkillPoints == inquisitor.SkillPoints - 1, "skill points are Inquisitor minus one");
        Check((DiceType)Get(cls, "HitDie") == DiceType.D8, "d8 hit die");
        Check(ReferenceEquals(Get(cls, "m_BaseAttackBonus"), Get(inquisitor, "m_BaseAttackBonus")), "Inquisitor BAB progression copied");
        Check(cls.ClassSkills.Contains(StatType.SkillPersuasion) && cls.ClassSkills.Contains(StatType.SkillUseMagicDevice) && cls.ClassSkills.Length == 7, "seven class skills including Persuasion and UMD");
        Check(cls.RecommendedAttributes.Contains(StatType.Wisdom), "Wisdom recommended");

        // ---- Private spellbook (Wisdom spontaneous, Inquisitor tables) ----
        var book = cls.Spellbook;
        Check(book != null && book.CastingAttribute == StatType.Wisdom && book.Spontaneous && !book.IsArcane, "Wisdom spontaneous non-arcane spellbook");
        Check(book.CantripsType == CantripsType.Cantrips && !book.AllSpellsKnown && !book.CanCopyScrolls, "cantrips, limited known, no scroll copying");
        Check(book.CharacterClass == cls, "reciprocal class link");
        var inqBook = Load<BlueprintSpellbook>(cache, BlueprintGuid.Parse("57fab75111f377248810ece84193a5a5"));
        Check(ReferenceEquals(book.SpellsKnown, inqBook.SpellsKnown) && ReferenceEquals(book.SpellsPerDay, inqBook.SpellsPerDay), "Inquisitor known/per-day tables reused read-only");
        var marker = Component(book, "PsychicSpellbookMarker");
        Check(marker != null && (int)Get(marker, "MaximumSpellLevel") == 6, "psychic marker capped at circle 6");

        // ---- 137-row catalog and source circles ----
        var rows = ((Array)IdsField("SpellList")).Cast<object>().ToArray();
        Func<object, int> circle = r => (int)Get(r, "Circle");
        Func<object, string> label = r => (string)Get(r, "Label");
        Func<object, string> guid = r => (string)Get(r, "Guid");
        Check(rows.Length == 137, "catalog holds exactly 137 source rows");
        var expected = new[] { 6, 17, 24, 28, 22, 24, 16 };
        for (int c = 0; c <= 6; c++)
            Check(rows.Count(r => circle(r) == c) == expected[c], "circle " + c + " has " + expected[c] + " source rows");
        Check(rows.All(r => circle(r) >= 0 && circle(r) <= 6), "no source row above circle 6");
        var list = book.SpellList;
        Check(list.SpellsByLevel.Length >= 11 && list.SpellsByLevel.Take(11).Select((r, i) => r.SpellLevel == i).All(x => x), "canonical 0-10 spell list array");
        for (int c = 7; c <= 10; c++)
            Check(list.SpellsByLevel[c].SpellsRefs == null || !list.SpellsByLevel[c].SpellsRefs.Any(), "circle " + c + " stays empty");
        var ghoul = rows.Single(r => label(r) == "Ghoul Touch");
        var ghoulGuid = BlueprintGuid.Parse("a2b05555c704458aaadc34be52a63633");
        Check(circle(ghoul) == 2 && guid(ghoul) == ghoulGuid.ToString() &&
            list.SpellsByLevel[2].SpellsRefs.Any(r => r.Guid == ghoulGuid), "Ghoul Touch uses the native DLC5 cast root at circle 2");
        var ghoulCast = Load<BlueprintAbility>(cache, ghoulGuid);
        Check(ghoulCast.GetComponent<Kingmaker.DLC.DlcCondition>() != null &&
            Component(ghoulCast, "AbilityEffectStickyTouch") != null, "Ghoul Touch preserves the original DLC condition and sticky touch");
        var unavailable = ((string[])AccessTools.Field(T("Spiritualist0085"), "LastUnavailable").GetValue(null));
        Check(!unavailable.Any(u => u.Contains("Ghoul Touch")), "Ghoul Touch no longer reported missing with the native entry");
        var lesser = rows.Single(r => label(r) == "Animate Dead, Lesser");
        Check(circle(lesser) == 2 && guid(lesser) == "57fcf8016cf04da4a8b33d2add14de7e", "Lesser Animate Dead kept at source circle 2 with the native root");
        Check(list.SpellsByLevel[2].SpellsRefs.Any(r => r.Guid == BlueprintGuid.Parse("57fcf8016cf04da4a8b33d2add14de7e")), "Lesser Animate Dead wired into circle 2 of the private list");

        // ---- Six emotions, one AddPet each, distinct marked units (C01/C16) ----
        var emotions = ((Array)IdsField("Emotions")).Cast<object>().ToArray();
        Func<object, string> name = e => (string)Get(e, "Name");
        Check(emotions.Length == 6 && emotions.Select(name).SequenceEqual(new[] { "Anger", "Despair", "Hatred", "Fear", "Zeal", "Kindness" }), "exactly the six original emotions, Whimsy unwired");
        var selection = Load<BlueprintFeatureSelection>(cache, Id("EmotionalFocus"));
        Check(selection != null && ((BlueprintFeatureReference[])Get(selection, "m_AllFeatures")).Length == 6, "emotion selection offers six options");
        var units = new HashSet<BlueprintGuid>();
        var slaResources = new HashSet<BlueprintAbilityResource>();
        var slaThresholds = new Dictionary<BlueprintAbilityResource, int>();
        IEnumerable<BlueprintAbility> Casts(BlueprintAbility ability)
        {
            var variants = ability.GetComponent<AbilityVariants>();
            return variants == null ? new[] { ability } : variants.Variants.SelectMany(Casts);
        }
        var petType = (PetType)850085;
        Check((int)petType == 850085 && !Enum.GetValues(typeof(PetType)).Cast<PetType>().Contains(petType) && (int)petType != 604767, "PetType 850085 reserved, no native or Making Friends overlap");
        foreach (var row in emotions)
        {
            var n = name(row);
            var prog = Load<BlueprintProgression>(cache, Id(n + "Emotion"));
            Check(prog != null, n + " emotion progression published");
            var pets = prog.GetComponents<AddPet>().ToArray();
            Check(pets.Length == 1, n + " has exactly one native AddPet");
            var pet = pets[0];
            Check(pet.Type == petType, n + " AddPet uses reserved type 850085");
            Check((bool)Get(pet, "m_UseContextValueLevel") && (bool)Get(pet, "m_ForceAutoLevelup") && (bool)Get(pet, "m_DestroyPetOnDeactivate"), n + " AddPet context-level, force-auto, destroy-on-remove");
            var rank = prog.GetComponent<ContextRankConfig>();
            Check(new[] { 1, 2, 20 }.All(l => Rank(rank, l) == l) &&
                ((BlueprintCharacterClassReference[])Get(rank, "m_Class")).Single().Guid == cls.AssetGuid,
                n + " native rank formula follows only committed Spiritualist levels");
            Check(Get(pet, "m_LevelRank") == null && Get(pet, "m_UpgradeFeature") == null && pet.UpgradeLevel == 0, n + " AddPet has no rank/upgrade donor");
            var unit = pet.Pet;
            Check(unit != null && units.Add(unit.AssetGuid), n + " unit blueprint is distinct");
            var unitMarker = Component(unit, "Phantom0085Marker");
            Check(unitMarker != null && (string)Get(unitMarker, "Emotion") == n, n + " unit carries the matching private marker");
            Check(unit.Strength == (int)Get(row, "Str") && unit.Dexterity == (int)Get(row, "Dex") && unit.Constitution == 13 && unit.Intelligence == 7 && unit.Wisdom == 10 && unit.Charisma == 13, n + " unit ability scores match A");
            Check(Component(unit, "AllowDyingCondition") != null && Component(unit, "AddResurrectOnRest") != null, n + " unit keeps AllowDyingCondition and AddResurrectOnRest");
            var levels = unit.GetComponent<AddClassLevels>();
            Check(levels != null && levels.DoNotApplyAutomatically && levels.Levels == 0, n + " unit AddClassLevels is manual and starts at 0");
            var archetype = ((BlueprintArchetypeReference[])Get(levels, "m_Archetypes")).Single().Get();
            Check(archetype != null && archetype.name == prefix + n + "PhantomArchetype", n + " archetype attached");
            Check(archetype.GetParentClass().name == prefix + "PhantomClass", n + " archetype parent is the private phantom class");
            bool fort = archetype.FortitudeSave != null, refl = archetype.ReflexSave != null, will = archetype.WillSave != null;
            Check(fort == (bool)Get(row, "Fort") && refl == (bool)Get(row, "Ref") && will == (bool)Get(row, "Will"), n + " good saves match the frozen code values");
            var parent = archetype.GetParentClass();
            Check(new[] { archetype.FortitudeSave ?? parent.FortitudeSave, archetype.ReflexSave ?? parent.ReflexSave,
                archetype.WillSave ?? parent.WillSave }.Select(save => save.GetBonus(1)).SequenceEqual(
                    new[] { fort ? 2 : 0, refl ? 2 : 0, will ? 2 : 0 }), n + " native level-one saves include the low progression");
            var skill = archetype.AddFeatures.Single(e => e.Level == 1).Features.OfType<BlueprintFeature>()
                .Single(f => Component(f, "Phantom0085SkillRanks") != null);
            Check(skill.GetComponent<ContextRankConfig>() == null, n + " skill ranks have no redundant native rank formula");
            var emotionSkills = (StatType[])Get(row, "Skills");
            Check(levels.Skills.SequenceEqual(new[] { StatType.SkillAthletics, StatType.SkillMobility, StatType.SkillStealth,
                    StatType.SkillPersuasion, StatType.SkillKnowledgeArcana }.Except(emotionSkills)) &&
                Component(skill, "Phantom0085SkillRanks") is IOwnerGainLevelHandler,
                n + " keeps its two automatic emotion skills out of the native discretionary allocation");
            var slaLevels = new[] { 5, 7, 9, 16 };
            for (int i = 0; i < 4; i++)
            {
                var entry = prog.LevelEntries.Single(e => e.Level == slaLevels[i]);
                Check(entry.Features.Count >= 1, n + " SLA entry at level " + slaLevels[i]);
                var token = n + "Sla" + i;
                var feature = entry.Features.OfType<BlueprintFeature>().Single(f => f.AssetGuid == Id(token + "Feature"));
                var grant = feature.GetComponent<AddAbilityResources>();
                var ability = Load<BlueprintAbility>(cache, Id(token));
                Check(grant?.Resource != null && grant.RestoreAmount && !grant.RestoreOnLevelUp &&
                    slaResources.Add(grant.Resource), token + " has an independent daily pool without level-up restoration");
                slaThresholds.Add(grant.Resource, slaLevels[i]);
                var leaves = Casts(ability).ToArray();
                Check(leaves.Length > 0 && leaves.All(leaf =>
                {
                    var parameters = leaf.GetComponents<ContextAbilityParamsCalculator>().ToArray();
                    var cost = leaf.GetComponents<AbilityResourceLogic>().ToArray();
                    return leaf.Type == AbilityType.SpellLike && leaf.MaterialComponent.Item == null &&
                        parameters.Length == 1 && parameters[0] is ContextCalculateAbilityParamsBasedOnClass calc &&
                        calc.CharacterClass == cls && calc.StatType == StatType.Wisdom && cost.Length == 1 &&
                        cost[0].RequiredResource == grant.Resource && cost[0].Amount == 1;
                }), token + " executable leaves use only the owner's Spiritualist/Wisdom parameters and one use");
            }
            foreach (var lvl in new[] { 1, 7, 12, 17 })
            {
                var entry = prog.LevelEntries.Single(e => e.Level == lvl);
                Check(entry.Features.OfType<BlueprintFeature>().Any(f => Component(f, "Phantom0085BoundGrant") != null), n + " scoped pet feature grant at level " + lvl);
            }
        }

        var actor = (UnitEntityData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnitEntityData));
        var descriptor = (UnitDescriptor)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnitDescriptor));
        var progression = new UnitProgressionData();
        var ownerLevel = new ClassData(cls) { Level = 1 };
        Set(actor, "Descriptor", descriptor);
        Set(descriptor, "Unit", actor);
        Set(actor, "Parts", new Kingmaker.EntitySystem.EntityPartsManager(actor));
        Set(descriptor, "Progression", progression);
        Set(progression, "Classes", new List<ClassData> { ownerLevel, new ClassData(inquisitor) { Level = 4 } });
        var tactical = AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive");
        harmony.Patch(tactical, prefix: new HarmonyMethod(typeof(Spiritualist0085Tests), nameof(NoTacticalCombat)));
        try
        {
            foreach (var pair in slaThresholds)
                Check(Enumerable.Range(1, 20).All(level =>
                {
                    ownerLevel.Level = level;
                    Set(progression, "m_CharacterLevel", (int?)(level + 4));
                    return pair.Key.GetMaxAmount(descriptor) == (level < pair.Value ? 0 : 1 + (level - pair.Value) / 4);
                }), pair.Key.name + " native resource arithmetic ignores four levels in another class");
        }
        finally
        {
            harmony.Unpatch(tactical, HarmonyPatchType.Prefix, harmony.Id);
        }

        var vampiricCharge = Load<BlueprintAbility>(cache, Id("FearSla2"));
        var freedomCharge = Load<BlueprintAbility>(cache, Id("ZealSla3"));
        var vampiricSpell = Load<BlueprintAbility>(cache, vampiricId);
        BlueprintGuid Delivery(BlueprintAbility spell) =>
            ((BlueprintAbilityReference)Get(spell.GetComponent<AbilityEffectStickyTouch>(), "m_TouchDeliveryAbility")).Guid;
        Check(Delivery(vampiricCharge) == BlueprintGuid.Parse("6cbb040023868574b992677885390f92") &&
            Delivery(vampiricCharge) == Delivery(vampiricSpell) &&
            Delivery(freedomCharge) == BlueprintGuid.Parse("4c349361d720e844e846ad8c19959b1e") &&
            Delivery(freedomCharge) == Delivery(Load<BlueprintAbility>(cache, freedomId)),
            "held emotional powers keep their native touch delivery identities");
        const string heldPatch = "Phantom0085Patches+HeldTouchCleanup";
        Check((bool)Call(heldPatch, "IsEmotionalPower", vampiricCharge) &&
            (bool)Call(heldPatch, "IsEmotionalPower", freedomCharge) &&
            !(bool)Call(heldPatch, "IsEmotionalPower", vampiricSpell),
            "held native spell recovery stays separate from private resource-backed touch casts");
        var heldMethod = AccessTools.Method(typeof(UnitPartTouch), "HandleObjectLeaveCombat");
        var heldIl = ((IEnumerable<CodeInstruction>)Call(heldPatch, "Transpiler",
            PatchProcessor.GetOriginalInstructions(heldMethod))).ToArray();
        Check(heldIl.Count(i => i.Calls(AccessTools.Method(T(heldPatch), "RestoreSlot"))) == 1 &&
            heldIl.Any(i => i.operand is MethodInfo method && method.Name == "GetMemorizedSpellSlots"),
            "held touch cleanup changes only the final spontaneous-slot recovery");
        Check(new[] { "AngerFuriousWail", "DespairDespairingShout" }.All(token =>
        {
            var ability = Load<BlueprintAbility>(cache, Id(token));
            return ability.Type == AbilityType.Supernatural && !ability.SpellResistance && ability.AvailableMetamagic == 0;
        }), "the two native supernatural conversions ignore spell resistance and metamagic");

        // ---- Phantom class and shared progression ----
        var phantomClass = Load<BlueprintCharacterClass>(cache, Id("PhantomClass"));
        Check(phantomClass.SkillPoints == druid.SkillPoints - 1 && phantomClass.SkillPoints == 2,
            "C03: Phantom takes the source skill pool from native Druid minus one");
        var angersGrant = Load<BlueprintProgression>(cache, Id("AngerEmotion")).GetComponent<AddPet>();
        var dogGrant = Load<BlueprintFeature>(cache, BlueprintGuid.Parse("f894e003d31461f48a02f5caec4e3359")).GetComponent<AddPet>();
        var progressionDonor = new BlueprintPet
        {
            AssetGuid = BlueprintGuid.Parse("0085008500850085008500850085f5e1"),
            ComponentsArray = new BlueprintComponent[] { new AddClassLevels { name = "0085 test owner override" } }
        };
        cache.AddCachedBlueprint(progressionDonor.AssetGuid, progressionDonor);
        var donorReference = new BlueprintPet.Reference();
        AccessTools.Field(typeof(BlueprintPet.Reference), "deserializedGuid").SetValue(donorReference, progressionDonor.AssetGuid);
        var ownerOverride = new AddClassLevelsToPets();
        AccessTools.Field(typeof(AddClassLevelsToPets), "m_BlueprintPet").SetValue(ownerOverride, donorReference);
        const string overridePatch = "Phantom0085Patches+PetLevelOverride";
        Check(ownerOverride.LevelsProgression != null &&
            Call(overridePatch, "OwnerProgression", ownerOverride, angersGrant) == null &&
            ReferenceEquals(Call(overridePatch, "OwnerProgression", ownerOverride, dogGrant), ownerOverride.LevelsProgression),
            "C03: the native owner override remains available to Dog and does not replace the private Phantom class");
        var overrideOriginal = AccessTools.Method(typeof(AddPet), "TryLevelUpPet");
        var overrideInstructions = ((IEnumerable<CodeInstruction>)Call(overridePatch, "Transpiler",
            PatchProcessor.GetOriginalInstructions(overrideOriginal))).ToArray();
        var nativeLevel = AccessTools.Method(typeof(AddClassLevels), "LevelUp",
            new[] { typeof(AddClassLevels), typeof(Kingmaker.UnitLogic.UnitDescriptor), typeof(int), typeof(Kingmaker.UnitLogic.UnitFact) });
        Check(nativeLevel != null &&
            overrideInstructions.Count(i => i.Calls(AccessTools.Method(T(overridePatch), "OwnerProgression"))) == 1 &&
            overrideInstructions.Any(i => i.Calls(nativeLevel)),
            "C03: only the native owner override lookup is redirected, preserving native leveling");
        var orphan = (object)default(UnitReference);
        AccessTools.Field(typeof(UnitReference), "m_UniqueId").SetValue(orphan, "saved-phantom-not-yet-loaded");
        const string referencePatch = "Phantom0085Patches+ExistingPetReference";
        var updatePet = AccessTools.Method(typeof(AddPet), "TryUpdatePet");
        var updateInstructions = (IEnumerable<CodeInstruction>)Call(referencePatch, "Transpiler",
            PatchProcessor.GetOriginalInstructions(updatePet), updatePet);
        Check(updateInstructions.Count(i => i.Calls(AccessTools.Method(T(referencePatch), "TransferNativeVisibility"))) == 1,
            "C08: the native AddPet reconnect contains exactly one guarded visibility assignment");
        Check((bool)Call(referencePatch, "WaitingForRecordedPet", orphan, null) &&
            !(bool)Call(referencePatch, "WaitingForRecordedPet", default(UnitReference), null) &&
            AccessTools.Property(typeof(AddPet), "Data")?.PropertyType == typeof(AddPetData) &&
            AccessTools.Property(typeof(AddPet), "Owner")?.PropertyType == typeof(UnitEntityData),
            "C17: an unresolved native save reference waits while a fresh grant is eligible to spawn");
        var recallState = Activator.CreateInstance(T("UnitPartPhantom0085"));
        var zero = new UnityEngine.Vector3(0f, 0f, 0f);
        var landing = new UnityEngine.Vector3(2f, 0f, 0f);
        var upstairs = new UnityEngine.Vector3(2f, 2f, 0f);
        void Remember() => AccessTools.Method(recallState.GetType(), "RememberRecall").Invoke(recallState, new object[] { "owned", zero, landing });
        bool Recalled(string id, UnityEngine.Vector3 anchor, UnityEngine.Vector3 position)
            => (bool)AccessTools.Method(recallState.GetType(), "AtLastRecallLanding").Invoke(recallState, new object[] { id, anchor, position });
        Remember();
        Check(Recalled("owned", zero, landing) && !Recalled("other", zero, landing) &&
            !Recalled("owned", zero, upstairs) && !Recalled("owned", upstairs, landing),
            "C05: a prior recall only suppresses its bound pet at the same owner anchor and 3-D landing");
        AccessTools.Method(recallState.GetType(), "ClearRecall").Invoke(recallState, null);
        Check(!Recalled("owned", zero, landing), "C05: area unload clears the previous landing record");
        Check(phantomClass != null && phantomClass.HideInUI, "phantom class hidden from players");
        Check((DiceType)Get(phantomClass, "HitDie") == DiceType.D10, "phantom d10 hit die");
        Check(phantomClass.GetComponent<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteIsPet>() != null, "phantom class is pet-only");
        var phantomProg = phantomClass.Progression;
        Check(phantomProg.LevelEntries.Length == 20, "phantom progression spans 20 levels");
        Check(phantomProg.LevelEntries[0].Features.Any(f => f.name == prefix + "PhantomDR5Slashing"), "phantom level 1 grants DR 5/slashing");
        Check(phantomProg.LevelEntries[0].Features.Any(f => f.name == prefix + "PhantomSlamDamage"), "phantom level 1 grants slam dice");
        Check(phantomProg.LevelEntries[14].Features.Any(f => f.name == prefix + "PhantomDR15"), "phantom level 15 grants the DR III entry");
        var dexCha = Load<BlueprintFeature>(cache, Id("PhantomDexChaGrowth"));
        var strCha = Load<BlueprintFeature>(cache, Id("PhantomStrChaGrowth"));
        var anger = Load<BlueprintArchetype>(cache, Id("AngerPhantomArchetype"));
        var growthLevels = new[] { 2, 4, 6, 8, 9, 12, 13, 15 };
        int[] GrantLevels(IEnumerable<LevelEntry> entries, BlueprintFeature feature)
            => entries.Where(e => e.Features.Contains(feature)).Select(e => e.Level).ToArray();
        Check(GrantLevels(phantomProg.LevelEntries, dexCha).SequenceEqual(growthLevels) &&
            GrantLevels(anger.RemoveFeatures, dexCha).SequenceEqual(growthLevels) &&
            GrantLevels(anger.AddFeatures, strCha).SequenceEqual(growthLevels),
            "Anger replaces all eight shared attribute entries");
        bool Growth(BlueprintFeature feature, StatType primaryStat) => feature.Ranks == growthLevels.Length &&
            feature.GetComponents<AddStatBonus>().Where(b => b.Value == 1).Select(b => b.Stat).SequenceEqual(new[] { primaryStat, StatType.Charisma });
        Check(Growth(dexCha, StatType.Dexterity) && Growth(strCha, StatType.Strength), "phantom attribute bonuses and rank limits match the source");
        var mien = Load<BlueprintAbility>(cache, Id("AngerFerociousMienAbility"));
        var mienLevel = Load<Kingmaker.UnitLogic.Mechanics.Properties.BlueprintUnitProperty>(cache, Id("AngerFerociousMienMasterLevel"));
        var mienAction = mien.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Actions.Actions
            .OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff>().Single();
        Check(mien.GetComponent<ContextRankConfig>() == null &&
            ((BlueprintUnitPropertyReference)Get(mienAction.DurationValue.BonusValue, "m_CustomProperty")).Guid == mienLevel.AssetGuid &&
            ((BlueprintCharacterClassReference)Get(Component(mienLevel, "Phantom0085MasterClassLevel"), "CharacterClass")).Guid == cls.AssetGuid,
            "Ferocious Mien duration reads the owner's Spiritualist level without a divisor");
        var kindnessBase = Load<BlueprintFeature>(cache, Id("KindnessPhantomBase"));
        var aid = ((BlueprintAbilityReference)Get(Component(kindnessBase, "Phantom0085Benevolent"), "Fallback")).Get();
        var aidOptions = aid.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityVariants>().Variants.ToArray();
        Check(aidOptions.Length == 2 && aidOptions.All(v => v.Parent == aid && v.CanTargetFriends && !v.CanTargetEnemies),
            "Kindness fallback grants two native ally-targeted variants");
        var aidAttack = Load<BlueprintBuff>(cache, Id("KindnessAidAttackEffect"));
        var aidDefense = Load<BlueprintBuff>(cache, Id("KindnessAidDefenseEffect"));
        Check(aidAttack.GetComponent<AddInitiatorAttackWithWeaponTrigger>()?.CheckWeaponRangeType == true &&
            aidDefense.GetComponent<AddTargetAttackWithWeaponTrigger>()?.OnlyMelee == true &&
            aidDefense.GetComponent<ACBonusAgainstAttacks>()?.ArmorClassBonus == 4,
            "Kindness fallback defense is +4, both variants expire on their next applicable melee attack");
        var mercy = Load<BlueprintFeatureSelection>(cache, Id("KindnessPhantomMercySelection"));
        var nativeMercies = ((BlueprintFeatureReference[])Get(
            Load<BlueprintFeatureSelection>(cache, BlueprintGuid.Parse("02b187038a8dce545bb34bbfb346428d")), "m_AllFeatures"))
            .Select(r => r.Guid).ToHashSet();
        var options = ((BlueprintFeatureReference[])Get(mercy, "m_AllFeatures")).Select(r => r.Get()).ToArray();
        Check(options.Length >= 4 && options.All(option =>
            nativeMercies.Contains(((BlueprintFeatureReference)Get(Component(option, "Phantom0085BoundGrant"), "Feature")).Guid)),
            "Mercy options grant the original Paladin fact through the bound phantom");
        var kindnessFocus = Load<BlueprintProgression>(cache, Id("KindnessEmotion"));
        var kindnessPotent = Load<BlueprintProgression>(cache, Id("KindnessPotentPhantom"));
        Check(kindnessFocus.LevelEntries.Single(e => e.Level == 12).Features.Count(f => f == mercy) == 4 &&
            kindnessPotent.LevelEntries.Single(e => e.Level == 20).Features.Count(f => f == mercy) == 4,
            "Kindness primary and secondary advances grant four choices through owner level-up");
        var openingReply = Load<BlueprintAbility>(cache, Id("KindnessOpeningStrikeAllyAbility"));
        Check(Component(openingReply, "Phantom0085OpeningStrikeTarget") != null &&
            openingReply.ActionType == Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift &&
            (bool)Get(openingReply.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Actions.Actions.Single(), "UseOpening"),
            "Opening Strike swift follow-up has source-specific targeting and an executable action");

        // ---- Master progression milestones ----
        var masterProg = cls.Progression;
        Check(masterProg.LevelEntries.Length == 20, "master progression spans 20 levels");
        Func<int, string, bool> has = (lvl, token) => masterProg.LevelEntries.Single(e => e.Level == lvl).Features.Any(f => f.AssetGuid == Id(token));
        Check(has(1, "EmotionalFocus") && has(1, "Spellcasting") && has(1, "Knacks") && has(1, "EthericTetherFeature") && has(1, "SharedConsciousness") && has(1, "MastersAlignment"), "level 1 grants selection, casting, knacks, tether, shared consciousness, alignment");
        Check(has(3, "BondedManifestationFeature"), "level 3 Bonded Manifestation");
        Check(has(4, "SpiritualInference"), "level 4 Spiritual Inference");
        Check(has(6, "PhantomRecall"), "level 6 Phantom Recall");
        Check(has(10, "FusedConsciousness"), "level 10 Fused Consciousness");
        Check(has(12, "GreaterSpiritualInference"), "level 12 Greater Spiritual Inference");
        Check(has(14, "SpiritualBondFeature"), "level 14 Spiritual Bond");
        Check(has(17, "DualBond"), "level 17 Dual Bond");
        Check(has(20, "PotentPhantom"), "level 20 Potent Phantom");

        // ---- Resource shapes (C05/C12 maxima) ----
        var recall = Load<BlueprintAbilityResource>(cache, Id("PhantomRecallResource"));
        var ra = Get(recall, "m_MaxAmount");
        Check((int)Get(ra, "BaseValue") == 0 && (int)Get(ra, "StartingLevel") == 6 && (int)Get(ra, "LevelStep") == 4 && (int)Get(ra, "StartingIncrease") == 1 && (int)Get(ra, "PerStepIncrease") == 1, "recall resource is 1/2/3/4 at 6/10/14/18");
        var bonded = Load<BlueprintAbilityResource>(cache, Id("BondedManifestationResource"));
        var ba = Get(bonded, "m_MaxAmount");
        Check((int)Get(ba, "BaseValue") == 3 && (bool)Get(ba, "IncreasedByLevel") && (int)Get(ba, "LevelIncrease") == 1, "bonded manifestation budget is 3 + level");
        var dual = Load<BlueprintFeature>(cache, Id("DualBond"));
        Check(Component(dual, "IncreaseResourceAmountBySharedValue") != null, "Dual Bond adds a second level-scaled budget");
        Check(((BlueprintCharacterClassReference[])Get(ba, "m_Class")).Single().Guid == cls.AssetGuid &&
            ((BlueprintCharacterClassReference[])Get(ba, "m_ClassDiv")).Length == 0 &&
            new[] { 17, 20 }.Select(l => 3 + l + Rank(dual.GetComponent<ContextRankConfig>(), l)).SequenceEqual(new[] { 37, 43 }),
            "C12: native linear resource source and Dual Bond totals are correct");

        // ---- Consciousness grants (C11) ----
        var shared = Load<BlueprintFeature>(cache, Id("SharedConsciousness"));
        var fused = Load<BlueprintFeature>(cache, Id("FusedConsciousness"));
        var sharedGrants = Components(shared, "Phantom0085ConsciousnessGrant").ToArray();
        var fusedGrants = Components(fused, "Phantom0085ConsciousnessGrant").ToArray();
        Check(sharedGrants.Length == 12 && fusedGrants.Length == 12, "two skill-focus grants per emotion on shared and fused");
        Check(sharedGrants.All(g => !(bool)Get(g, "Permanent")) && fusedGrants.All(g => (bool)Get(g, "Permanent")), "shared is confined-gated, fused is permanent");
        Check(sharedGrants.All(g => Get(g, "SaveBuff") != null) && fusedGrants.All(g => Get(g, "SaveBuff") != null), "consciousness save buffs wired");
        var fusedSave = ((BlueprintBuffReference)Get(fusedGrants[0], "SaveBuff")).Get();
        Check(fusedSave.GetComponent<ContextRankConfig>() != null, "fused save bonus scales by class rank");
        Check(Component(fused, "SavingThrowBonusAgainstDescriptor") == null &&
            new[] { 10, 12 }.Select(l => Rank(fusedSave.GetComponent<ContextRankConfig>(), l)).SequenceEqual(new[] { 4, 8 }),
            "C11: one Fused source, +4 and +8");

        // ---- Potent Phantom (C16) ----
        var potent = Load<BlueprintFeatureSelection>(cache, Id("PotentPhantom"));
        Check(((BlueprintFeatureReference[])Get(potent, "m_AllFeatures")).Length == 6, "Potent Phantom offers six secondary emotions");
        foreach (var reference in (BlueprintFeatureReference[])Get(potent, "m_AllFeatures"))
        {
            var option = reference.Get();
            Check(option.GetComponent<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteNoFeature>() != null, "Potent option " + option.name + " forbids the same primary emotion");
        }

        // ---- Self-review fixes (2026-11-16) ----
        var recallAbility = Load<BlueprintAbility>(cache, Id("PhantomRecallAbility"));
        Check(recallAbility.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic>() == null, "recall spends inside the action only on a successful move, not via pre-cost");
        Check(recallAbility.ComponentsArray.Any(c => c.GetType().Name == "AbilityEffectRunAction"), "recall action present");
        var tether = Load<BlueprintFeature>(cache, Id("EthericTetherFeature"));
        var bond = Load<BlueprintFeature>(cache, Id("SpiritualBondFeature"));
        var tetherToggle = tether.GetComponent<AddFacts>();
        var bondToggle = bond.GetComponent<AddFacts>();
        var tetherActivatable = ((BlueprintUnitFactReference[])Get(tetherToggle, "m_Facts")).Select(r => r.Get()).OfType<BlueprintActivatableAbility>().First();
        var bondActivatable = ((BlueprintUnitFactReference[])Get(bondToggle, "m_Facts")).Select(r => r.Get()).OfType<BlueprintActivatableAbility>().First();
        var nativeGroups = new UnitPartActivatableAbility();
        Check(nativeGroups.GetGroupSize(tetherActivatable.Group) == 1 && nativeGroups.GetGroupSize(bondActivatable.Group) == 1 &&
            tetherActivatable.Group == ActivatableAbilityGroup.None && bondActivatable.Group == ActivatableAbilityGroup.None &&
            (BlueprintGuid)Call("Phantom0085DamageTransfer", "OtherToggle", false) == bondActivatable.AssetGuid &&
            (BlueprintGuid)Call("Phantom0085DamageTransfer", "OtherToggle", true) == tetherActivatable.AssetGuid,
            "native groups accept both toggles and mutual exclusion references only the paired 0085 ability");
        Check(Component(tetherActivatable, "Phantom0085DamageLinkRestriction") != null &&
            Component(bondActivatable, "Phantom0085DamageLinkRestriction") != null,
            "both mutual protection toggles use the same active bound phantom condition as damage delivery");
        Check(Load<BlueprintBuff>(cache, Id("SharedConsciousnessSave")).IsClassFeature &&
            Load<BlueprintBuff>(cache, Id("FusedConsciousnessSave")).IsClassFeature, "consciousness buffs opt out of native respec restoration");
        var inferenceOwner = Load<BlueprintBuff>(cache, Id("SpiritualInferenceOwner"));
        var inferenceAlly = Load<BlueprintBuff>(cache, Id("GreaterSpiritualInferenceAlly"));
        Check(inferenceOwner.Stacking == StackingType.Stack && inferenceAlly.Stacking == StackingType.Stack, "inference buffs stack per owning phantom");
        var bondedFeature = Load<BlueprintFeature>(cache, Id("BondedManifestationFeature"));
        var bondedGrants = bondedFeature.GetComponents<AddFeatureOnClassLevel>().ToArray();
        Check(bondedGrants.Any(g => g.Level == 8) && bondedGrants.Any(g => g.Level == 18), "tendril attacks granted at 8 and 18");
        Check(bondedGrants.All(g => ((BlueprintFeatureReference)Get(g, "m_Feature")).Get().GetComponent<AddFacts>() != null), "tendril grants carry abilities");
        var bondedToggle = Load<BlueprintActivatableAbility>(cache, Id("BondedManifestation"));
        Check(bondedToggle.DeactivateImmediately && bondedToggle.GetComponent<ActivatableAbilityResourceLogic>()?.SpendType ==
            ActivatableAbilityResourceLogic.ResourceSpendType.NewRound, "Bonded can stop immediately and uses the native round budget");
        var tendril8 = Load<BlueprintAbility>(cache, Id("BondedTendrils8Ability"));
        var tendril18 = Load<BlueprintAbility>(cache, Id("BondedTendrils18Ability"));
        var bondedId = Id("BondedManifestationEffect");
        Check(new[] { tendril8, tendril18 }.All(a => a.GetComponent<AbilityCasterHasFacts>()?.Facts.Any(f => f.AssetGuid == bondedId) == true),
            "both tendrils require the Bonded shield through the native caster restriction");
        Check(tendril18.Range == AbilityRange.Personal && tendril18.IsFullRoundAction &&
            tendril18.GetComponent<AbilityTargetsAround>()?.Targets.ToString() == "Enemy" &&
            tendril8.GetComponent<AbilityTargetsAround>() == null, "full-round tendrils use the native hostile area selector centered on the caster");
        var tendrilDice = Load<BlueprintBuff>(cache, Id("BondedTendrils18Dice")).GetComponent<ContextRankConfig>();
        var phantomDice = Load<BlueprintFeature>(cache, Id("PhantomSlamDamage")).GetComponent<ContextRankConfig>();
        Check(Enumerable.Range(1, 20).All(l => Rank(tendrilDice, l) == Rank(phantomDice, l)),
            "tendril dice and native phantom progression use matching tiers at each level");
        var phantomProg2 = phantomClass.Progression;
        Check(phantomProg2.LevelEntries[6].Features.Any(f => f.AssetGuid == BlueprintGuid.Parse("a634f97d23662b347872ffcf580495b3")), "phantom level 7 grants the native Multiattack donor");
        var dr15 = Load<BlueprintFeature>(cache, Id("PhantomDR15"));
        var dr15Comp = dr15.GetComponent<Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical>();
        Check(dr15Comp != null, "dr15 component present");
        var dr15Value = (Kingmaker.UnitLogic.Mechanics.ContextValue)Get(dr15Comp, "Value");
        Check(dr15Value.Value == 15 && !dr15Comp.BypassedByForm, "level-15 DR implements the contract 15/- (not the original 5/slashing)");

        // ---- C13/C14 exact transfer arithmetic (pure contract function) ----
        var transferType = T("Phantom0085DamageTransfer");
        var transferMethod = AccessTools.Method(transferType, "TransferAmount");
        Func<bool, int, int, int, int, int> transfer = (bond, result, petHp, masterHp, con) =>
            (int)transferMethod.Invoke(null, new object[] { bond, result, petHp, masterHp, con });
        Check(transfer(false, 16, 1, 10, 13) == 3, "C13: decisive packet transfers exactly 3 (phantom 1 -> -12, owner 10 -> 7)");
        Check(transfer(false, 16, 1, 2, 13) == 1, "C13: owner at 2 HP transfers at most 1");
        Check(transfer(false, 5, 10, 10, 13) == 0, "C13: non-lethal packet transfers zero");
        Check(transfer(true, 20, 1, 5, 13) == 13, "C14: 20 damage transfers exactly 13 (phantom 1 -> -12, owner 5 -> -2)");
        Check(transfer(true, 20, 0, 5, 13) == 0, "C14: phantom below 1 HP transfers zero");
        Check(transfer(true, 3, 10, 5, 13) == 0, "C14: non-lethal-to-owner packet transfers zero");
        var effective = AccessTools.Method(transferType, "EffectiveHpDamage");
        int HpDamage(int damage, int temporary) => (int)effective.Invoke(null, new object[] { damage, temporary });
        Check(transfer(false, 200, 185, 10, 13) == 3, "C13: 200-point packet can transfer exactly 3");
        Check(transfer(false, HpDamage(16, 16), 1, 10, 13) == 0 &&
            transfer(false, HpDamage(20, 4), 1, 10, 13) == 3, "C13: only effective normal HP damage is transferred");
        var ownerEntity = (UnitEntityData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnitEntityData));
        var petEntity = (UnitEntityData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnitEntityData));
        var otherEntity = (UnitEntityData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnitEntityData));
        bool Victim(bool bondMode, UnitEntityData unit)
            => (bool)Call("Phantom0085DamageTransfer", "ExpectedVictim", bondMode, unit, ownerEntity, petEntity);
        Check(Victim(false, petEntity) && !Victim(false, ownerEntity) && !Victim(false, otherEntity) &&
            Victim(true, ownerEntity) && !Victim(true, petEntity) && !Victim(true, otherEntity),
            "C13/C14/C17: only the actual owner or own phantom can be the expected damage target");
        var patch = T("Phantom0085Patches+ExactDamageTransfer");
        var nativeDamage = AccessTools.Method(typeof(RuleDealDamage), nameof(RuleDealDamage.OnTrigger));
        var originalIl = PatchProcessor.GetOriginalInstructions(nativeDamage);
        var rewrittenIl = ((IEnumerable<CodeInstruction>)AccessTools.Method(patch, "Transpiler")
            .Invoke(null, new object[] { originalIl })).ToList();
        var boundary = AccessTools.PropertySetter(typeof(UnitEntityData), "LastHandledDamage");
        int insert = rewrittenIl.FindIndex(i => i.Calls(AccessTools.Method(patch, "Apply")));
        int deliver = rewrittenIl.FindIndex(i => i.Calls(AccessTools.Method(patch, "Deliver")));
        int payment = rewrittenIl.FindIndex(i => i.Calls(AccessTools.Method(patch, "ApplyToHitPoints")));
        Check(insert > 0 && rewrittenIl[insert + 1].Calls(boundary) && payment > insert && deliver > payment,
            "C13/C14: native damage IL provides the HP, temporary HP and guarded delivery boundaries");
        var paymentRule = (RuleDealDamage)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(RuleDealDamage));
        var payments = AccessTools.Field(patch, "Payments").GetValue(null);
        var paymentAmount = Activator.CreateInstance(patch.GetNestedType("Payment", BindingFlags.NonPublic),
            BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { 3 }, null);
        AccessTools.Method(payments.GetType(), "Add").Invoke(payments, new object[] { paymentRule, paymentAmount });
        AccessTools.Method(patch, "Apply").Invoke(null, new object[] { paymentRule });
        Check(paymentRule.Result == 3 &&
            (int)AccessTools.Method(patch, "ApplyToHitPoints").Invoke(null, new object[] { null, paymentRule.Result, paymentRule }) == 3,
            "C13/C14: a marked payment passes its full integer damage to native ordinary HP");
        Set(petEntity, "Parts", new EntityPartsManager(petEntity));
        Set(otherEntity, "Parts", new EntityPartsManager(otherEntity));
        var protectedDescriptor = (UnitDescriptor)FormatterServices.GetUninitializedObject(typeof(UnitDescriptor));
        Set(petEntity, "Descriptor", protectedDescriptor);
        Set(protectedDescriptor, "State", new UnitState(protectedDescriptor));
        petEntity.State.Features.IsUntargetable.Retain();
        var incoming = new RuleDealDamage(otherEntity, ownerEntity, new DirectDamage(Kingmaker.RuleSystem.DiceFormula.Zero, 20));
        incoming.Reason = incoming;
        Check(!(bool)Call("Phantom0085Patches+ExactDamageTransfer", "CanReceivePayment", incoming, petEntity),
            "C14: native untargetable recipient is rejected before deducting redirected damage from the original hit");
        CheckNativeMountAndRetirement(harmony, angersGrant.Pet, dogGrant.Pet, petType);
        CheckNativePetBinding(cls, inquisitor, (BlueprintFeature)angersGrant.OwnerBlueprint, (BlueprintFeature)dogGrant.OwnerBlueprint,
            Load<BlueprintFaction>(cache, BlueprintGuid.Parse("d8de50cc80eb4dc409a983991e0b77ad")));
        var restPatch = T("Phantom0085Patches+RestResurrectionQueue");
        var restMethod = AccessTools.Method(typeof(Kingmaker.Controllers.Rest.RestController), "ResurrectDeadUnits");
        var nativeRest = AccessTools.Method(typeof(Kingmaker.Controllers.Rest.State.CampingState), "GetPartyUnitsForRest");
        var restIl = ((IEnumerable<CodeInstruction>)AccessTools.Method(restPatch, "Transpiler")
            .Invoke(null, new object[] { PatchProcessor.GetOriginalInstructions(restMethod) })).ToList();
        int restQueue = restIl.FindIndex(i => i.Calls(AccessTools.Method(restPatch, "GetResurrectionUnits")));
        Check(restQueue >= 0 && restIl.Count(i => i.Calls(nativeRest)) == 1 &&
            restQueue < restIl.FindIndex(i => i.Calls(nativeRest)), "only the native resurrection loop receives the augmented queue");
        var reroll = (RuleRollD20)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(RuleRollD20));
        reroll.Override(4);
        AccessTools.PropertySetter(typeof(RuleRollDice), nameof(RuleRollDice.RollHistory))
            .Invoke(reroll, new object[] { new List<int> { 4 } });
        Call("Phantom0085Reroll", "UseReplacement", reroll, 12);
        bool improved = reroll.Result == 12;
        Call("Phantom0085Reroll", "UseReplacement", reroll, 2);
        Check(improved && reroll.Result == 2 && reroll.RollHistory.SequenceEqual(new[] { 4, 12, 2 }),
            "Zeal uses the newly rolled result whether higher or lower and keeps the native history");
        foreach (var area in new[] { "AngerAuraOfFuryArea", "DespairAuraOfDespairArea", "HatredHatefulAuraArea", "ZealDeterminationAuraArea" })
        {
            var effect = Load<BlueprintAbilityAreaEffect>(cache, Id(area));
            var applyAreaBuff = effect?.GetComponent<AbilityAreaEffectBuff>();
            Check(applyAreaBuff?.Buff?.Stacking == StackingType.Stack && effect.GetComponent<AbilityAreaEffectRunAction>() == null,
                area + " uses the native source-owned area buff");
        }
        var hatedAction = Load<BlueprintAbility>(cache, Id("HatredHatedTargetAbility"))
            .GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single();
        var hated = hatedAction.Buff;
        Check(hatedAction.Permanent && Component(hated, "UniqueBuff") != null && Component(hated, "AddFactContextActions") != null &&
            Component(Load<BlueprintFeature>(cache, Id("HatredSneakAttack")), "Phantom0085HatredSneakAttack") != null,
            "Hatred mark persists and owns its shared and target-specific sneak attack effects");
        var fearArea = Load<BlueprintAbilityAreaEffect>(cache, Id("FearIncreaseFearArea"));
        var exitFear = fearArea.GetComponent<AbilityAreaEffectRunAction>().UnitExit.Actions.OfType<ContextActionRemoveBuff>().Single();
        Check(exitFear.OnlyFromCaster && exitFear.Buff.AssetGuid == Id("FearIncreaseFearAuraBuff") &&
            fearArea.GetComponent<AbilityAreaEffectBuff>()?.Buff.AssetGuid == Id("FearShelterAlliesEffect"),
            "Fear exit only removes its frightened effect and Sheltering uses the native area-owned buff");
        var bondedShield = Load<BlueprintBuff>(cache, Id("BondedManifestationEffect"));
        Check(bondedShield.ComponentsArray.Any(c => c.GetType().Name == "Phantom0085ShieldToIncorporealTouch"), "bonded shield transfers to incorporeal touch AC");
        Check(bondedShield.ComponentsArray.Any(c => c.GetType().Name == "Phantom0085TendrilDice") == false, "tendril dice live on the temporary attack buff, not the shield");
        Check(new[] { 3, 13 }.Select(l => Rank(bondedShield.GetComponent<ContextRankConfig>(), l)).SequenceEqual(new[] { 4, 6 }),
            "C12: Bonded's own shield rank is +4/+6");
        var inference = Load<BlueprintFeature>(cache, Id("SpiritualInference"));
        Check(inference.GetComponent<AddFeatureOnClassLevel>() is AddFeatureOnClassLevel gate && gate.BeforeThisLevel && gate.Level == 12 &&
            Component(inferenceAlly, "Phantom0085InferenceBonus") != null, "C15: basic aura retires at 12 and the ally bonus rechecks its source");
        var basicArea = Load<BlueprintAbilityAreaEffect>(cache, Id("SpiritualInferenceArea"));
        var greaterArea = Load<BlueprintAbilityAreaEffect>(cache, Id("GreaterSpiritualInferenceArea"));
        foreach (var token in new[] { "SpiritualInference", "GreaterSpiritualInference" })
        {
            var feature = Load<BlueprintFeature>(cache, Id(token + "Pet"));
            var host = feature.GetComponent<AuraFeatureComponent>()?.Buff;
            Check(host?.GetComponent<AddAreaEffect>()?.AreaEffect == Load<BlueprintAbilityAreaEffect>(cache, Id(token + "Area")) &&
                feature.GetComponent<AddAreaEffect>() == null,
                "C15: " + token + " spawns its area from a native carrier buff on the pet feature");
        }
        var greaterEffects = greaterArea.GetComponents<AbilityAreaEffectBuff>().ToArray();
        Check(basicArea.GetComponents<AbilityAreaEffectBuff>().Select(effect => effect.Buff.AssetGuid)
            .SequenceEqual(new[] { Id("SpiritualInferenceOwner") }) &&
            greaterEffects.Select(effect => effect.Buff.AssetGuid)
                .SequenceEqual(new[] { Id("GreaterSpiritualInferenceOwner"), Id("GreaterSpiritualInferenceAlly") }) &&
            new[] { basicArea, greaterArea }.All(area => area.GetComponent<AbilityAreaEffectRunAction>() == null) &&
            new[] { basicArea, greaterArea }.SelectMany(area => area.GetComponents<AbilityAreaEffectBuff>())
                .All(effect => effect.CheckConditionEveryRound && effect.Condition.Conditions.Single().GetType() == T("Phantom0085InferenceCondition")),
            "C15: both auras use native area ownership with per-round phantom eligibility");
        CheckInferenceExit(greaterEffects.Single(effect => effect.Buff == inferenceAlly), harmony);
        CheckRecoveryVisibility(harmony);
        var curse = Load<BlueprintAbility>(cache, Id("HatredSla2"));
        var children = curse.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityVariants>()?.Variants.ToArray();
        Check(children != null && children.Length > 0 && children.All(child => child.Parent == curse &&
            child.Type == AbilityType.SpellLike &&
            child.GetComponent<ContextCalculateAbilityParamsBasedOnClass>()?.CharacterClass == cls &&
            child.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic>() != null),
            "C16: native Bestow Curse variants are private, use the master class and spend the matching SLA resource");

        // ---- Shared roots untouched ----
        Check(ReferenceEquals(cache.Load(inqBook.AssetGuid), inqBook), "Inquisitor spellbook identity preserved");

        // ---- Boundaries not entered by this offline host ----
        Console.WriteLine("0085 NOT RUN: Unity AddPet spawning, the full AddClassLevels controller, live scene placement and navigation, rest UI, save/load, respec screen and mount button.");
        Console.WriteLine("0085 RESULT: " + assertions + " assertions passed, " + Failures.Count + " failed.");
        if (Failures.Count > 0)
            throw new Exception("0085 directed failures: " + string.Join(" | ", Failures));
        return assertions;
    }
}
