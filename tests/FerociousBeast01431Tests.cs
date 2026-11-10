using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Real manager/fact/component, pet-master events, Buff.TickMechanics, modifiers
// and resource Spend. Only absent scene services are bounded; no replacement of
// the production lifecycle or explicit calls to its component notification hooks.
internal static class FerociousBeast01431Tests
{
    static Type Module => typeof(FerociousBeastHost01431).Assembly.GetType("CallOfTheWild.Wrath.Prototype.FerociousBeast01431", true);
    static readonly List<UnitEntityData> units = new List<UnitEntityData>();
    static readonly List<RankEvents> rankObservers = new List<RankEvents>();
    static int checks, serial, petAdds, petRemoves, buffAdds, buffRemoves, resourceSpends, rankGains, rankLosses, spawnDeaths;
    static bool fault, optionalFault;
    static readonly List<BlueprintScriptableObject> protectedDonors = new List<BlueprintScriptableObject>();
    static string[] donorBefore;
    static object Get(object o, string name) => (AccessTools.Field(o.GetType(), name) ?? AccessTools.Field(o.GetType(), "<" + name + ">k__BackingField")).GetValue(o);
    static void Set(object o, string name, object value) => (AccessTools.Field(o.GetType(), name) ?? AccessTools.Field(o.GetType(), "<" + name + ">k__BackingField")).SetValue(o, value);
    static BlueprintGuid Id(string token) => (BlueprintGuid)AccessTools.Method(typeof(Migration01429Tests), "Id").Invoke(null, new object[] { token });
    static T Asset<T>(BlueprintsCache cache, string token) where T : SimpleBlueprint => (T)cache.Load(Id(token));
    static void Check(bool ok, string text) { if (!ok) throw new Exception("Ferocious01431: " + text); checks++; }
    static void Register(BlueprintsCache cache) => AccessTools.Method(Module, "Register").Invoke(null, new object[] { cache });
    static string Definition(object value) => (string)AccessTools.Method(typeof(FerociousBeastHost01431).Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426"), "Definition", new[] { typeof(object) }).Invoke(null, new[] { value });
    static bool Skip() => false;
    static bool No(ref bool __result) { __result = false; return false; }
    static bool Clock(ref TimeSpan __result) { __result = TimeSpan.FromHours(1); return false; }
    static bool NoController(ref LevelUpController __result) { __result = null; return false; }
    static bool PoolIgnore(BlueprintFeatureSelection __instance, ref bool __result) { __result = __instance.IgnorePrerequisites; return false; }
    static void AttachId(EntityFact __instance) { if (string.IsNullOrEmpty(__instance.UniqueId)) Set(__instance, "UniqueId", "01431-fact-" + (++serial)); }
    static void AppendFault(IEnumerable<BlueprintFeatureSelection> selections, BlueprintFeature[] feats) { if ((fault || optionalFault && selections.Any(p => p.AssetGuid == BlueprintGuid.Parse("6baf176bc22c4a41ba421bce3fcd9497"))) && feats.Any(f => f.AssetGuid == Id("FerociousBeastFeature"))) throw new InvalidOperationException("Ferocious01431 append fault"); }
    static void Spent() => resourceSpends++;
    sealed class Events : IPetHandler, IUnitBuffHandler, IUnitSpawnOnlyHandler
    {
        public void HandleAddPet(UnitEntityData master, UnitEntityData pet) { petAdds++; }
        public void HandleRemovePet(UnitEntityData master, UnitEntityData pet) { petRemoves++; }
        public void HandleBuffDidAdded(Buff buff) { buffAdds++; }
        public void HandleBuffDidRemoved(Buff buff) { buffRemoves++; }
        public void HandleUnitDeathForSpawnOnly(UnitEntityData unit) { if (units.Contains(unit)) spawnDeaths++; }
    }
    sealed class RankEvents : IFeatureRankHandler
    {
        readonly UnitEntityData unit;
        public RankEvents(UnitEntityData unit) { this.unit = unit; }
        public UnitEntityData GetSubscribingUnit() => unit;
        public void HandleUnitGainFeatureRank(Feature feature) { rankGains++; }
        public void HandleUnitLostFeatureRank(Feature feature) { rankLosses++; }
    }
    static UnitEntityData Unit(BlueprintsCache cache, string name, string classId = "f7d7eb166b3dd594fb330d085df41853", int level = 8)
    {
        var u = (UnitEntityData)AccessTools.Method(typeof(FlameJetLifecycle01430Tests), "Unit").Invoke(null, new object[] { cache, "ferocious-" + name });
        // Flight's unit fixture does not need an ability processor. Constant
        // sharing must prove that active leaves were not installed into a real
        // native collection, rather than treating a missing collection as empty.
        Set(u, "Abilities", u.Facts.EnsureFactProcessor<Kingmaker.UnitLogic.Abilities.AbilityCollection>());
        u.Progression.Classes.Clear(); u.Progression.Classes.Add(new ClassData((BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse(classId))) { Level = level });
        Set(u, "UniqueId", "01431-unit-" + (++serial));
        var proxy = FormatterServices.GetUninitializedObject(typeof(EntityService).GetNestedType("Proxy", BindingFlags.Public | BindingFlags.NonPublic));
        Set(proxy, "Entity", u); Set(u, "Proxy", proxy);
        if (u.Passive == null) Set(u, "Passive", new ObservableCountFlag());
        u.Stats.AC.BaseValue = 10; u.Stats.SaveWill.BaseValue = 0; u.Stats.Speed.BaseValue = 30;
        units.Add(u); var rankObserver = new RankEvents(u); rankObservers.Add(rankObserver); EventBus.Subscribe(rankObserver); return u;
    }
    static Feature Add(UnitEntityData u, BlueprintFeature bp)
    {
        var f = new Feature(bp, u.Descriptor); u.Facts.Add(f);
        Check(f.IsActive && f.IsTurnedOn && f.IsSubscribedOnEventBus, "native feature Add/Activate/TurnOn/subscription " + bp.name); return f;
    }
    static Feature Presence(UnitEntityData unit, BlueprintFeature source)
    {
        // Eligibility/caster-gate input retains the exact cached blueprint:
        // native HasFact compares blueprint object identity. Only this input's
        // unrelated activation payload is bounded, then restored immediately.
        var components = source.ComponentsArray; var before = Definition(components);
        Feature fact;
        try { source.ComponentsArray = Array.Empty<BlueprintComponent>(); fact = Add(unit, source); }
        finally { source.ComponentsArray = components; }
        Check(ReferenceEquals(fact.Blueprint, source) && unit.HasFact(source), "presence input uses exact native blueprint identity " + source.name);
        Check(ReferenceEquals(source.ComponentsArray, components) && Definition(source.ComponentsArray) == before, "presence fixture restores original authored input payload " + source.name);
        return fact;
    }
    static Buff Rage(UnitEntityData u, BlueprintBuff bp)
    {
        var b = new Buff(bp, new MechanicsContext(u, u.Descriptor, bp), null); u.Facts.Add(b);
        Check(b.IsActive && b.IsTurnedOn && b.IsSubscribedOnEventBus, "native rage Add/Activate/TurnOn/subscription " + bp.name); return b;
    }
    static UnitEntityData Pet(BlueprintsCache c, UnitEntityData owner, string label)
    {
        var p = Unit(c, label, "26b10d4340839004f960f9816f6109fe", 8);
        owner.Ensure<UnitPartPetMaster>().AddPet(p, PetType.AnimalCompanion);
        Check(p.Get<UnitPartPet>()?.Master == owner && owner.Pets.Any(r => r.Entity == p), "native AddPet assigns actual owner and emits event"); return p;
    }
    static BlueprintBuff Host(BlueprintsCache c, int mode) => (BlueprintBuff)c.Load(BlueprintGuid.Parse(((string[])AccessTools.Field(Module, "Hosts").GetValue(null))[mode]));
    static BlueprintAbilityResource Resource(BlueprintsCache c, int mode) => (BlueprintAbilityResource)c.Load(BlueprintGuid.Parse(((string[])AccessTools.Field(Module, "Resources").GetValue(null))[mode]));
    static int EffectCount(UnitEntityData pet, string prefix) => pet.Buffs.Enumerable.Count(b => b.Blueprint.name.StartsWith("CotWWrathPsychic" + prefix, StringComparison.Ordinal) && b.IsTurnedOn && !b.IsDisposed);
    static string GuardEvidence(BlueprintsCache c, UnitEntityData owner, UnitEntityData pet, Buff rage)
    {
        var driver = rage.GetComponent<FerociousBeastHost01431>(); var root = Asset<BlueprintFeature>(c, "FerociousBeastFeature");
        var runtime = rage.Components.SingleOrDefault(r => r.SourceBlueprintComponent == driver);
        bool active = false; object state = null;
        if (runtime != null) using (runtime.RequestEventContext()) {
            active = (bool)AccessTools.Property(typeof(FerociousBeastHost01431), "Active").GetValue(driver, null);
            state = AccessTools.Property(driver.GetType(), "Data").GetValue(driver, null);
        }
        return new JObject {
            ["active"] = active, ["rootIdentity"] = driver != null && ReferenceEquals(driver.BasePower.Get(), root),
            ["sourceHostIdentity"] = driver != null && ReferenceEquals(driver.SourceHost.Get(), rage.Blueprint),
            ["rootNativeHasFact"] = owner.HasFact(root), ["rootFeatures"] = new JArray(owner.Progression.Features.Enumerable.Select(f => f.Blueprint.name + ":" + ReferenceEquals(f.Blueprint, root) + ":" + f.IsTurnedOn + ":" + f.IsDisposed)),
            ["resource"] = driver == null ? -1 : owner.Resources.GetResourceAmount(driver.Resource.Get()), ["ownerDead"] = owner.State.IsDead,
            ["hostOn"] = rage.IsTurnedOn, ["hostSuppressed"] = rage.IsSuppressed, ["hostDisposed"] = rage.IsDisposed,
            ["petMasterIdentity"] = pet.Get<UnitPartPet>()?.Master == owner, ["petType"] = pet.Get<UnitPartPet>()?.Type.ToString(), ["petDead"] = pet.State.IsDead,
            ["petDisposed"] = pet.IsDisposed,
            ["trackedBasic"] = state == null ? "null" : ((FerociousBeastHost01431.State)state).Basic.Fact?.Blueprint.name,
            ["trackedPetIdentity"] = state != null && ((FerociousBeastHost01431.State)state).Pet.Entity == pet,
            ["petBuffs"] = new JArray(pet.Buffs.Enumerable.Select(b => b.Blueprint.name + ":" + b.IsTurnedOn + ":" + b.IsActive + ":" + b.IsDisposed))
        }.ToString(Newtonsoft.Json.Formatting.None);
    }
    static BlueprintFeatureReference[] PoolRefs(BlueprintFeatureSelection pool, string field) => (BlueprintFeatureReference[])Get(pool, field) ?? Array.Empty<BlueprintFeatureReference>();
    static Feature Provider(UnitEntityData unit)
    {
        var bp = new BlueprintFeature { name = "01431AnimalCompanionProvider", AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), ComponentsArray = new BlueprintComponent[] { new AddPet { Type = PetType.AnimalCompanion } } };
        bp.OnEnable();
        // The prerequisite reads the original AddPet feature and native class
        // data. Spawning its blueprint-specific Unity companion is outside this
        // harness; actual generic AddPet/RemovePet are exercised below.
        return (Feature)AccessTools.Method(typeof(Migration01429Tests), "Fact").Invoke(null, new object[] { unit, bp });
    }
    static void Selection(BlueprintsCache c)
    {
        var root = Asset<BlueprintFeature>(c, "FerociousBeastFeature"); var greater = Asset<BlueprintFeature>(c, "FerociousBeastGreaterFeature");
        var pool = (BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse("28710502f46848d48b3f0d6132817c4e"));
        var barb = Unit(c, "selection", level: 1); var state = (LevelUpState)FormatterServices.GetUninitializedObject(typeof(LevelUpState));
        Set(state, "m_Unit", barb); Set(state, "Selections", new List<FeatureSelectionState>()); state.SelectedClass = barb.Progression.Classes[0].CharacterClass; state.NextClassLevel = 1; Set(state, "NextCharacterLevel", 1);
        var selection = state.AddSelection(null, default(FeatureSource), pool, 1);
        Check(!pool.CanSelect(barb.Descriptor, state, selection, root), "native CanSelect rejects no companion provider"); Provider(barb);
        Check(pool.CanSelect(barb.Descriptor, state, selection, root), "native CanSelect accepts barbarian1 with animal companion provider");
        Check(!pool.CanSelect(barb.Descriptor, state, selection, greater), "greater requires earlier root and8");
        Add(barb, root); barb.Progression.Classes[0].Level = 7; state.NextClassLevel = 7;
        Check(!pool.CanSelect(barb.Descriptor, state, selection, greater), "greater rejects level7 even with base");
        barb.Progression.Classes[0].Level = 8; state.NextClassLevel = 8; Check(pool.CanSelect(barb.Descriptor, state, selection, greater), "greater accepts base plus8");
        var skald = Unit(c, "skald", "6afa347d804838b48bda16acb0573dc0", 20); Provider(skald);
        Check(!root.MeetsPrerequisites(null, skald.Descriptor, null, false, false), "pure Skald20 cannot select a rage-round spending root");
        var blood = Unit(c, "blood", "d77e67a814d686842802c9cfd8ef8499", 8); Provider(blood);
        Check(!root.MeetsPrerequisites(null, blood.Descriptor, null, false, false), "ordinary bloodrager cannot select primalist root");
        blood.Progression.Classes[0].Archetypes.Add((BlueprintArchetype)c.Load(BlueprintGuid.Parse("4d588290ed0736e4f8cd3f4c3beacd69")));
        Check(root.MeetsPrerequisites(null, blood.Descriptor, null, false, false), "native primalist archetype level qualifies");
        var skaldPool = (BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse("2476514e31791394fa140f1a07941c96"));
        Check(!PoolRefs(skaldPool, "m_AllFeatures").Any(f => f != null && (f.Guid == root.AssetGuid || f.Guid == greater.AssetGuid)), "Skald pool receives no Ferocious Beast roots");
        foreach (var host in new[] { "75b3978757908d24aaaecaf2dc209b89", "ee51f69e69cb43c3bc7c02002e482d17" })
            Check(!((BlueprintBuff)c.Load(BlueprintGuid.Parse(host))).GetComponents<FerociousBeastHost01431>().Any(), "ordinary/mythic Inspired Rage have no spending observer");
    }
    static void Lifecycle(BlueprintsCache c, int mode)
    {
        var cls = mode == 2 ? "d77e67a814d686842802c9cfd8ef8499" : "f7d7eb166b3dd594fb330d085df41853";
        var owner = Unit(c, "owner-" + mode, cls); var pet = Pet(c, owner, "pet-" + mode); var unrelated = Unit(c, "unrelated-" + mode);
        if (mode == 2) owner.Progression.Classes[0].Archetypes.Add((BlueprintArchetype)c.Load(BlueprintGuid.Parse("4d588290ed0736e4f8cd3f4c3beacd69")));
        var r = Resource(c, mode); owner.Resources.Add(r, true); int amount = owner.Resources.GetResourceAmount(r);
        var noPower = Rage(owner, Host(c, mode)); Check(EffectCount(pet, "FerociousBeastBaseRage") == 0, "no selected base root grants no pet rage mode" + mode); owner.Facts.Remove(noPower);
        var basic = Add(owner, Asset<BlueprintFeature>(c, "FerociousBeastFeature"));
        var swiftBlueprint = (BlueprintFeature)c.Load(BlueprintGuid.Parse("7df32d4e9bd2cdc48b0f69b03a57754a"));
        var swift = Add(owner, swiftBlueprint);
        var rage = Rage(owner, Host(c, mode)); Check(EffectCount(pet, "FerociousBeastBaseRage") == 1, "one exact-owned base benefit mode" + mode + "; " + GuardEvidence(c, owner, pet, rage));
        Check(pet.Stats.TemporaryHitPoints.ModifiedValue > 0, "real native temporary HP modifier based on petHD and source rank mode" + mode);
        Check(unrelated.Stats.TemporaryHitPoints.ModifiedValue == 0, "unrelated creature receives no effect");
        int ownedHp = pet.Stats.TemporaryHitPoints.ModifiedValue; unrelated.State.UpdateLifeState(UnitLifeState.Dead);
        Check(EffectCount(pet, "FerociousBeastBaseRage") == 1 && pet.Stats.TemporaryHitPoints.ModifiedValue == ownedHp, "unrelated native spawn-only death does not revoke another master's owned pet benefit");
        int hp = pet.Stats.TemporaryHitPoints.ModifiedValue; pet.Stats.TemporaryHitPoints.HandleDamage(3);
        Check(pet.Stats.TemporaryHitPoints.ModifiedValue == hp - 3, "native HandleDamage consumes shared temporaryHP");
        int beforeAdds = buffAdds, beforeRemoves = buffRemoves; rage.TurnOff(); Check(pet.Stats.TemporaryHitPoints.ModifiedValue == 0, "source direct TurnOff removes owned pet modifier");
        rage.TurnOn(); Check(pet.Stats.TemporaryHitPoints.ModifiedValue == hp - 3, "source direct TurnOn preserves consumed temporaryHP");
        Check(buffAdds == beforeAdds && buffRemoves == beforeRemoves, "base host suppression uses real fact transitions without synthetic add/remove");
        owner.Facts.TurnOff(); Check(pet.Stats.TemporaryHitPoints.ModifiedValue == 0, "owner manager off removes pet effect");
        owner.Facts.TurnOn(); Check(pet.Stats.TemporaryHitPoints.ModifiedValue == hp - 3, "owner manager on restores remaining sharedHP");
        rage.TickMechanics(); Check(owner.Resources.GetResourceAmount(r) == amount - 1, "actual native rage TickMechanics spends exactly one extra source round");
        var greater = Add(owner, Asset<BlueprintFeature>(c, "FerociousBeastGreaterFeature"));
        Check(EffectCount(pet, "FerociousBeastConstantPowers") == 1 && pet.Stats.Speed.ModifiedValue == 40, "greater acquired during rage grants constant Swift Foot effect");
        swift.TurnOff(); rage.TickMechanics(); Check(pet.Stats.Speed.ModifiedValue == 30, "real Swift Foot donor off is observed by next native rage round");
        swift.TurnOn(); rage.TickMechanics(); Check(pet.Stats.Speed.ModifiedValue == 40, "real Swift Foot donor on restores its benefit by next native rage round");
        var mergedSwift = owner.Facts.Add(new Feature(swiftBlueprint, owner.Descriptor));
        Check(ReferenceEquals(mergedSwift, swift) && swift.GetRank() == 2, "native ranked Swift Foot ownership merges into the original feature");
        rage.TickMechanics(); Check(pet.Stats.Speed.ModifiedValue == 50, "native next-round signature observes existing donor rank increase");
        int gainBefore = rankGains, lossBefore = rankLosses; swift.AddRank();
        Check(swift.GetRank() == 3 && rankGains == gainBefore + 1, "real Feature.AddRank reapplication and native rank event reach the donor's maximum3");
        rage.TickMechanics(); Check(pet.Stats.Speed.ModifiedValue == 60, "native next-round signature detects direct donor AddRank without fact attach");
        swift.RemoveRank(); Check(swift.GetRank() == 2 && rankLosses == lossBefore + 1, "real Feature.RemoveRank emits native rank loss while keeping the donor active");
        rage.TickMechanics(); Check(pet.Stats.Speed.ModifiedValue == 50, "native next-round signature detects direct donor rank loss without fact detach");
        owner.Facts.Remove(mergedSwift); Check(swift.GetRank() == 1 && swift.IsTurnedOn, "native ranked donor removal retains its original rank");
        rage.TickMechanics(); Check(pet.Stats.Speed.ModifiedValue == 40, "native next-round signature restores lower surviving donor rank");
        int once = owner.Resources.GetResourceAmount(r); rage.TickMechanics(); Check(owner.Resources.GetResourceAmount(r) == once - 1, "greater does not pay a second extra round");
        greater.TurnOff(); Check(pet.Stats.Speed.ModifiedValue == 30, "greater root off revokes constant child effects"); greater.TurnOn(); Check(pet.Stats.Speed.ModifiedValue == 40, "greater root on reapplies constant child effects");
        rage.TurnOff(); Check(pet.Stats.Speed.ModifiedValue == 30 && pet.Stats.TemporaryHitPoints.ModifiedValue == 0, "host off removes both basic and all nested constant modifiers");
        rage.TurnOn(); Check(pet.Stats.Speed.ModifiedValue == 40 && pet.Stats.TemporaryHitPoints.ModifiedValue == hp - 3, "host on restores own effects with consumedHP retained");
        owner.Get<UnitPartPetMaster>().RemovePet(pet); Check(pet.Stats.Speed.ModifiedValue == 30 && pet.Stats.TemporaryHitPoints.ModifiedValue == 0, "real RemovePet event revokes former pet effects");
        int removedCost = owner.Resources.GetResourceAmount(r); rage.TickMechanics(); Check(owner.Resources.GetResourceAmount(r) == removedCost, "no active animal companion pays no extra round");
        var replacement = Pet(c, owner, "replacement-" + mode); Check(replacement.Stats.Speed.ModifiedValue == 40 && replacement.Stats.TemporaryHitPoints.ModifiedValue > 0, "new native animal companion receives ongoing owner's effects");
        int deathBefore = spawnDeaths; replacement.State.UpdateLifeState(UnitLifeState.Dead);
        Check(replacement.State.IsDead && spawnDeaths == deathBefore + 1, "real native UpdateLifeState sets IsDead before emitting its actual spawn-only death event");
        Check(replacement.Stats.TemporaryHitPoints.ModifiedValue == 0 && replacement.Stats.Speed.ModifiedValue == 30, "native life-state transition and EventBus immediately revoke dead pet effects");
        int deadCost = owner.Resources.GetResourceAmount(r); rage.TickMechanics(); Check(owner.Resources.GetResourceAmount(r) == deadCost, "dead pet consumes no extra rage round");
        owner.Get<UnitPartPetMaster>().RemovePet(replacement); replacement = Pet(c, owner, "living-after-death-" + mode);
        var foreign = new BlueprintBuff { name = "01431UnrelatedTempHP", AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), Stacking = StackingType.Stack,
            ComponentsArray = new BlueprintComponent[] { new AddStatBonus { Stat = StatType.Speed, Value = 7, Descriptor = ModifierDescriptor.UntypedStackable } } }; foreign.OnEnable(); var other = Rage(replacement, foreign);
        owner.Facts.Remove(greater); Check(replacement.Stats.Speed.ModifiedValue == 37, "greater removal preserves unrelated pet modifier");
        owner.Facts.Remove(basic); Check(replacement.Stats.TemporaryHitPoints.ModifiedValue == 0 && replacement.Stats.Speed.ModifiedValue == 37, "base removal revokes only its exact-owned effects");
        replacement.Facts.Remove(other); owner.Facts.Remove(rage);
    }
    static void Divine(BlueprintsCache c)
    {
        // The isolated rage selector and the combined suite both start here
        // before Anger's own test suite. Use the real production dependency in
        // canonical Ferocious-then-Anger order, only if its root is missing.
        if (c.Load(Id("AngerInquisition")) == null)
            AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.AngerInquisition01431", true), "Register").Invoke(null, new object[] { c });
        var divine = Asset<BlueprintBuff>(c, "AngerDivineRageBuff"); var resource = Asset<BlueprintAbilityResource>(c, "AngerDivineRageUses");
        Check(divine != null && resource != null, "same-batch Divine Anger donor and private resource available");
        var driver = divine.GetComponent<FerociousBeastHost01431>(); Check(driver?.Mode == 3 && driver.SourceHost.Get() == divine && driver.Resource.Get() == resource, "Divine driver binds exact host and actual new resource");
        var owner = Unit(c, "divine-owner");
        owner.Progression.Classes.Add(new ClassData((BlueprintCharacterClass)c.Load(BlueprintGuid.Parse("67819271767a9dd4fbfd4ae700befea0"))) { Level = 8 });
        var pet = Pet(c, owner, "divine-pet"); owner.Resources.Add(resource, true);
        Check(owner.Resources.GetResourceAmount(resource) > 1, "real Barbarian8/Cleric8 actor has actual positive Divine resource capacity");
        var barb = Resource(c, 0); owner.Resources.Add(barb, true); int barbBefore = owner.Resources.GetResourceAmount(barb);
        Add(owner, Asset<BlueprintFeature>(c, "FerociousBeastFeature")); var rage = Rage(owner, divine); int before = owner.Resources.GetResourceAmount(resource);
        Check(pet.Stats.TemporaryHitPoints.ModifiedValue > 0, "Divine Rage extends its own actual basic rage to owned animal companion");
        rage.TickMechanics(); Check(owner.Resources.GetResourceAmount(resource) == before - 1 && owner.Resources.GetResourceAmount(barb) == barbBefore, "Divine extra round spends actual Divine resource and preserves barbarian pool");
        owner.Facts.Remove(rage);
        var limitlessOriginal = (BlueprintFeature)c.Load(BlueprintGuid.Parse("5cb58e6e406525342842a073fb70d068"));
        var limitless = Presence(owner, limitlessOriginal);
        var unlimited = Rage(owner, divine); int unlimitedBefore = owner.Resources.GetResourceAmount(resource); unlimited.TickMechanics();
        Check(owner.Resources.GetResourceAmount(resource) == unlimitedBefore && owner.Resources.GetResourceAmount(barb) == barbBefore, "Divine Ferocious follows its native Limitless free-resource marker without touching barbarian pool");
        owner.Facts.Remove(unlimited); owner.Facts.Remove(limitless);
        var skald = Unit(c, "inspired-owner", "6afa347d804838b48bda16acb0573dc0"); var sp = Pet(c, skald, "inspired-pet");
        Add(skald, Asset<BlueprintFeature>(c, "FerociousBeastFeature"));
        var inspired = Rage(skald, (BlueprintBuff)c.Load(BlueprintGuid.Parse("75b3978757908d24aaaecaf2dc209b89")));
        Check(EffectCount(sp, "FerociousBeastBaseRage") == 0 && sp.Stats.TemporaryHitPoints.ModifiedValue == 0, "even a forced root on pure Skald does not add Ferocious to Inspired Rage"); skald.Facts.Remove(inspired);
        skald.Progression.Classes.Add(new ClassData((BlueprintCharacterClass)c.Load(BlueprintGuid.Parse("f7d7eb166b3dd594fb330d085df41853"))) { Level = 8 });
        skald.Resources.Add(barb, true); var mixedRage = Rage(skald, Host(c, 0));
        Check(sp.Stats.TemporaryHitPoints.ModifiedValue > 0, "Skald/barbarian mixed-class own personal rage supports Ferocious"); skald.Facts.Remove(mixedRage);
    }
    static void MultipleAndLimited(BlueprintsCache c)
    {
        var owner = Unit(c, "overlap-owner"); var pet = Pet(c, owner, "overlap-pet");
        Add(owner, Asset<BlueprintFeature>(c, "FerociousBeastFeature")); var normal = Resource(c, 0); var focused = Resource(c, 1);
        owner.Resources.Add(normal, true); owner.Resources.Add(focused, true);
        var a = Rage(owner, Host(c, 0)); var b = Rage(owner, Host(c, 1));
        Check(EffectCount(pet, "FerociousBeastBaseRage") == 1, "simultaneous personal rage hosts elect one pet base source");
        int focusBefore = owner.Resources.GetResourceAmount(focused); b.TickMechanics();
        Check(owner.Resources.GetResourceAmount(focused) == focusBefore, "inactive higher-priority-number host pays no duplicate extra round");
        a.TurnOff(); Check(EffectCount(pet, "FerociousBeastBaseRage") == 1, "direct source off restores remaining alternate source without buff event");
        b.TickMechanics(); Check(owner.Resources.GetResourceAmount(focused) == focusBefore - 1, "remaining alternate source spends its own focused resource");
        a.TurnOn(); Check(EffectCount(pet, "FerociousBeastBaseRage") == 1, "restored original source suppresses duplicate alternate pet modifiers");
        owner.Resources.Spend(normal, owner.Resources.GetResourceAmount(normal)); a.TickMechanics();
        Check(EffectCount(pet, "FerociousBeastBaseRage") <= 1, "empty normal resource never leaves two active sources");
        owner.Facts.Remove(a); owner.Facts.Remove(b); Check(pet.Stats.TemporaryHitPoints.ModifiedValue == 0, "last source removal cleans every owned modifier");
        owner.Resources.Restore(normal); var original = (BlueprintFeature)c.Load(BlueprintGuid.Parse("5cb58e6e406525342842a073fb70d068"));
        var free = Presence(owner, original);
        var rage = Rage(owner, Host(c, 0)); int before = owner.Resources.GetResourceAmount(normal); rage.TickMechanics();
        Check(owner.Resources.GetResourceAmount(normal) == before, "Limitless marker preserves original native unlimited rage rounds");
        owner.Facts.Remove(free); owner.Resources.Spend(normal, owner.Resources.GetResourceAmount(normal)); rage.TickMechanics();
        Check(pet.Stats.TemporaryHitPoints.ModifiedValue == 0 && owner.Resources.GetResourceAmount(normal) == 0, "finite exhausted rage suspends animal benefit without negative resources");
        owner.Facts.Remove(rage);
    }
    static void Registration(BlueprintsCache c, Harmony harmony)
    {
        Register(c);
        var root = Asset<BlueprintFeature>(c, "FerociousBeastFeature"); var greater = Asset<BlueprintFeature>(c, "FerociousBeastGreaterFeature");
        Check(root != null && greater != null && root.AssetGuid != greater.AssetGuid, "first registration publishes two distinct canonical roots");
        var hosts = Enumerable.Range(0, 3).Select(i => Host(c, i)).ToArray(); var original = hosts.Select(b => b.ComponentsArray).ToArray();
        Register(c); Check(ReferenceEquals(root, Asset<BlueprintFeature>(c, "FerociousBeastFeature")) && ReferenceEquals(greater, Asset<BlueprintFeature>(c, "FerociousBeastGreaterFeature")), "repeat registration preserves both root identities");
        for (int i = 0; i < hosts.Length; i++) Check(ReferenceEquals(original[i], hosts[i].ComponentsArray) && hosts[i].GetComponents<FerociousBeastHost01431>().Count() == 1, "repeat registration does not duplicate native host observer" + i);
        var pools = ((string[])AccessTools.Field(Module, "PoolIds").GetValue(null)).Select(id => (BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse(id))).ToArray();
        var poolBefore = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
        fault = true;
        try { bool rejected = false; try { Register(c); } catch (TargetInvocationException ex) { rejected = ex.InnerException is InvalidOperationException && ex.InnerException.Message.Contains("Ferocious01431 append fault"); }
            Check(rejected, "native pool append fault rejected");
            for (int i = 0; i < pools.Length; i++) foreach (var index in new[] { 0, 1 }) Check(ReferenceEquals(poolBefore[i][index], Get(pools[i], index == 0 ? "m_Features" : "m_AllFeatures")), "append failure restores exact pool reference");
            for (int i = 0; i < hosts.Length; i++) Check(ReferenceEquals(original[i], hosts[i].ComponentsArray), "append failure preserves exact native host component array");
        } finally { fault = false; }
        foreach (var pool in pools) foreach (var feature in new[] { root, greater }) Check(PoolRefs(pool, "m_AllFeatures").Count(f => f?.Guid == feature.AssetGuid) == 1, "two distinct roots each occur once in permitted real pool " + pool.name);
        foreach (var host in hosts) Check(host.GetComponents<FerociousBeastHost01431>().Single().SourceHost.Get() == host, "exact native host identity guards copied unrelated rage buffs");
        for (int i = 0; i < protectedDonors.Count; i++) Check(Definition(protectedDonors[i].ComponentsArray) == donorBefore[i], "existing native/Howl authored component graph retained " + protectedDonors[i].name);
        OptionalRegistration(c);
    }
    static void OptionalRegistration(BlueprintsCache c)
    {
        var providerId = BlueprintGuid.Parse("6baf176bc22c4a41ba421bce3fcd9497"); var priorProvider = c.Load(providerId);
        var native = (BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse("28710502f46848d48b3f0d6132817c4e"));
        var nativeData = PsychicExpansionTests.NativeRows.Single(row => (string)row["blueprint"]["AssetId"] == native.AssetGuid.ToString())["blueprint"]["Data"];
        var nativeGroup = (FeatureGroup)PsychicExpansionTests.Read(nativeData["Group"],typeof(FeatureGroup));
        var nativeGroups = (FeatureGroup[])PsychicExpansionTests.Read(nativeData["Groups"],typeof(FeatureGroup[]));
        Check(nativeGroup == FeatureGroup.RagePower && nativeGroups.Length == 0, "actual current native pool is Group=RagePower with empty Groups");
        var roots = new[] { Asset<BlueprintFeature>(c, "FerociousBeastFeature"), Asset<BlueprintFeature>(c, "FerociousBeastGreaterFeature") };
        void Optional() => AccessTools.Method(Module, "Optional").Invoke(null, new object[] { c });
        string Diagnostic() => (string)AccessTools.Property(Module, "LastOptionalDiagnostic").GetValue(null);
        void Stage(SimpleBlueprint bp) { c.RemoveCachedBlueprint(providerId); if (bp != null) c.AddCachedBlueprint(providerId, bp); }
        BlueprintFeatureSelection Provider() {
            // The exact current TTT authoring path: clone native Group and Groups
            // separately, retain original feature references and prerequisites.
            var p = (BlueprintFeatureSelection)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(native, null);
            p.AssetGuid = providerId; p.name = "PrimalistRagePowerSelection"; p.Group = nativeGroup; p.Groups = nativeGroups;
            return p;
        }
        try {
            Stage(null); Optional(); Check(Diagnostic() == null, "absent optional TTT is a supported native-only registration");
            var groupOnly = Provider(); groupOnly.Group = FeatureGroup.RagePower; groupOnly.Groups = Array.Empty<FeatureGroup>();
            var originalRefs = PoolRefs(groupOnly, "m_AllFeatures").Select(r => r.Guid).ToArray(); Stage(groupOnly); Optional(); Optional(); Register(c);
            foreach (var root in roots) foreach (var field in new[] { "m_Features", "m_AllFeatures" }) Check(PoolRefs(groupOnly, field).Count(r => r?.Guid == root.AssetGuid) == 1, "latest TTT Group-only pool accepts each exact cached root once " + field);
            Check(originalRefs.All(id => PoolRefs(groupOnly, "m_AllFeatures").Any(r => r.Guid == id)) && Diagnostic() == null, "optional repair preserves original TTT entries and clears diagnostic after success");
            var groupsOnly = Provider(); groupsOnly.Group = FeatureGroup.None; groupsOnly.Groups = new[] { FeatureGroup.RagePower }; Stage(groupsOnly); Optional(); Check(Diagnostic() == null, "supported Groups-only provider layout remains accepted");
            foreach (int bad in Enumerable.Range(0, 4)) {
                var invalid = Provider(); if (bad == 0) invalid.name = "ForeignSelection"; if (bad == 1) invalid.IgnorePrerequisites = true; if (bad == 2) { invalid.Group = FeatureGroup.None; invalid.Groups = Array.Empty<FeatureGroup>(); }
                SimpleBlueprint provider = bad == 3 ? (SimpleBlueprint)new BlueprintFeature { name = "PrimalistRagePowerSelection", AssetGuid = providerId } : invalid;
                var before = provider is BlueprintFeatureSelection p ? new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") } : null;
                Stage(provider); Register(c);
                Check(Diagnostic()?.Contains("optional TTT Primalist integration skipped") == true, "invalid provider has explicit isolated diagnostic " + bad);
                Check(roots.All(root => ReferenceEquals(c.Load(root.AssetGuid), root)), "invalid optional provider cannot remove cache-discoverable Ferocious roots " + bad);
                if (before != null) Check(ReferenceEquals(before[0], Get(provider, "m_Features")) && ReferenceEquals(before[1], Get(provider, "m_AllFeatures")), "invalid external pool arrays are untouched " + bad);
            }
            var failed = Provider(); Stage(failed); var failedBefore = new[] { Get(failed, "m_Features"), Get(failed, "m_AllFeatures") };
            optionalFault = true; Optional(); optionalFault = false;
            Check(Diagnostic()?.Contains("Ferocious01431 append fault") == true, "external append failure remains explicit in diagnostic");
            Check(ReferenceEquals(failedBefore[0], Get(failed, "m_Features")) && ReferenceEquals(failedBefore[1], Get(failed, "m_AllFeatures")), "external append failure restores exact external arrays");
            Optional(); Check(Diagnostic() == null, "late repaired provider retries successfully without stale failure");
            Check(roots.All(root => ReferenceEquals(c.Load(root.AssetGuid), root)), "optional fault and retry retain ToyBox cache discovery identities");
        } finally { optionalFault = false; Stage(priorProvider); }
    }
    static void ExistingConstantDonors(BlueprintsCache c)
    {
        var assembly = typeof(FerociousBeastHost01431).Assembly;
        AccessTools.Method(assembly.GetType("CallOfTheWild.Wrath.Prototype.TerrifyingHowl", true), "Register").Invoke(null, new object[] { c });
        protectedDonors.Clear();
        foreach (var id in new[] { "25954b1652bebc2409f9cb9d5728bceb", "a67b51a8074ae47438280be0a87b01b6", "955e356c813de1743a98ab3485d5bc69" }) protectedDonors.Add((BlueprintScriptableObject)c.Load(BlueprintGuid.Parse(id)));
        foreach (var token in new[] { "TerrifyingHowlRagePower", "TerrifyingHowlAbility" }) {
            var donor = c.Load(Id(token)) as BlueprintScriptableObject; Check(donor != null, "actual previously delivered donor registered " + token); protectedDonors.Add(donor);
        }
        donorBefore = protectedDonors.Select(b => Definition(b.ComponentsArray)).ToArray();
    }
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static void ActualConstantDonors(BlueprintsCache c, Harmony h)
    {
        Console.WriteLine("Ferocious donor stage: entry");
        // The original native limb owner/limit/restoration component executes.
        // Only item allocation into an absent Unity body slot is bounded, as in
        // the retained native limb fixture.
        Check(h != null, "actual donor Harmony scope exists");
        foreach (var pair in new[] { new { Type = typeof(Kingmaker.Dungeon.Blueprints.BlueprintPersonageLimits), Method = "GetWeaponSlotCount", Prefix = "LimbLimit" }, new { Type = typeof(Kingmaker.Items.UnitBody), Method = "AddLimbToIndex", Prefix = "LimbInsert" }, new { Type = typeof(Kingmaker.Items.UnitBody), Method = "RemoveAdditionalLimbItem", Prefix = "LimbRemove" } }) {
            var endpoint = AccessTools.Method(pair.Type, pair.Method); var prefix = AccessTools.Method(typeof(Migration01429Tests), pair.Prefix);
            Check(endpoint != null && prefix != null, "actual native limb endpoint and existing bounded allocator exist " + pair.Method);
            Console.WriteLine("Ferocious donor stage: patch " + pair.Method);
            h.Patch(endpoint, prefix: new HarmonyMethod(prefix));
        }
        Console.WriteLine("Ferocious donor stage: canonical assets");
        var wrapper = Asset<BlueprintBuff>(c, "FerociousBeastConstantPowers0");
        var lesser = (BlueprintFeature)c.Load(BlueprintGuid.Parse("25954b1652bebc2409f9cb9d5728bceb"));
        var howl = Asset<BlueprintFeature>(c, "TerrifyingHowlRagePower");
        Check(wrapper != null && lesser != null && howl != null, "actual native Animal Fury, accepted Howl and shared wrapper identities exist");
        Console.WriteLine("Ferocious donor stage: root classification");
        bool Constant(BlueprintFeature f) => (bool)AccessTools.Method(Module, "IsConstantPower").Invoke(null, new object[] { f });
        Check(Constant(lesser), "actual native Animal Fury is a constant root");
        Check(!Constant(howl), "accepted Howl is an action root and is excluded entirely");
        foreach (var id in new[] { "aad496a7c9bee224cbd01a3cf8e42061", "efb97e482f53f064dab85a9eeaf01085" }) {
            var root = (BlueprintFeature)c.Load(BlueprintGuid.Parse(id));
            Check(root != null && root.GetComponents<AddFacts>().Any(a => a.Facts.Any(f => f is Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility || f is Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility)), "actual native action-root grants a resolved ability/activatable type " + id);
            Check(!Constant(root), "native healing and stance action roots are excluded " + id);
        }
        foreach (bool existingBite in new[] { false, true }) {
            Console.WriteLine("Ferocious donor stage: construct body " + existingBite);
            var owner = Unit(c, "native-animal-fury-owner-" + existingBite); var pet = Pet(c, owner, "native-animal-fury-pet-" + existingBite);
            Check(pet.Body != null && pet.Body.AdditionalLimbs != null && pet.Body.CurrentHandsEquipmentSet != null, "constructed native body, additional-limb collection and hands set exist for bite branch " + existingBite);
            AccessTools.Field(typeof(Migration01429Tests), "limbOwner").SetValue(null, pet);
            Console.WriteLine("Ferocious donor stage: existing weapon " + existingBite);
            if (existingBite) {
                var weapon = AccessTools.Method(typeof(SpellAndSword01417Tests), "Weapon").Invoke(null, new object[] { pet, WeaponCategory.Bite, "01431-existing-pet-bite" });
                AccessTools.Method(typeof(SpellAndSword01417Tests), "Equip").Invoke(null, new[] { pet, weapon });
            }
            Console.WriteLine("Ferocious donor stage: actual roots " + existingBite);
            Add(owner, lesser);
            // Presence of an active root is the source gate; the original
            // authored graph, rather than this marker, is classified above.
            Presence(owner, howl);
            Console.WriteLine("Ferocious donor stage: actual wrapper add " + existingBite);
            var buff = new Buff(wrapper, new MechanicsContext(owner, pet.Descriptor, wrapper), null); pet.Facts.Add(buff);
            Console.WriteLine("Ferocious donor stage: child effects " + existingBite);
            var children = pet.Buffs.Enumerable.Where(b => b.IsTurnedOn && !b.IsDisposed && b != buff).ToArray();
            Check(pet.Body.AdditionalLimbs.Count(s => s.HasItem && s.MaybeWeapon.Blueprint.Category == WeaponCategory.Bite) == 1, "native Animal Fury keeps its original secondary-limb rule with or without a primary bite " + existingBite);
            if (existingBite) Check(pet.Body.PrimaryHand.MaybeWeapon?.Blueprint.Category == WeaponCategory.Bite, "native shared Animal Fury preserves the existing primary weapon");
            Check(!pet.Abilities.Enumerable.Any(a => a.Blueprint.AssetGuid == Id("TerrifyingHowlAbility")), "actual accepted active Howl is never granted to pet " + existingBite);
            Check(!pet.Progression.Features.Enumerable.Any(f => f.Blueprint.GetComponents<ManeuverTrigger>().Any()) && !children.Any(b => b.Blueprint.GetComponents<AddFacts>().Any(a => a.Facts.Any(f => f != null && f.GetComponents<ManeuverTrigger>().Any()))), "action-only Greater trample ancillary payload is excluded even through private cloned facts " + existingBite);
            Check(!pet.Progression.Features.Enumerable.Any(f => f.Blueprint.GetComponents<AddFacts>().Any(a => a.Facts.Any(x => x is Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility || x is Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility))), "constant sharing grants no native ability or activatable feature leaf " + existingBite);
            Console.WriteLine("Ferocious donor stage: actual wrapper remove " + existingBite);
            pet.Facts.Remove(buff);
            Check(pet.Body.AdditionalLimbs.All(s => !s.HasItem), "actual wrapper removal cleans native owned bite limb " + existingBite);
            if (existingBite) Check(pet.Body.PrimaryHand.MaybeWeapon?.Blueprint.Category == WeaponCategory.Bite, "removing private Animal Fury leaves preexisting primary bite intact");
        }
        for (int i = 0; i < protectedDonors.Count; i++) Check(Definition(protectedDonors[i].ComponentsArray) == donorBefore[i], "sharing leaves accepted donor source graph unchanged " + protectedDonors[i].name);
    }
    static void NestedGrant(BlueprintsCache c)
    {
        var factory = Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets");
        var strings = (Dictionary<string, string>)AccessTools.Field(factory, "Strings").GetValue(null);
        var original = new Dictionary<string, string>(strings);
        Check(original.ContainsKey("cotw-wrath.psychic.full.FerociousBeastFeature.description") && original.ContainsKey("cotw-wrath.psychic.full.FerociousBeastGreaterFeature.description"), "nested test locale snapshot includes the actual production root descriptions");
        try { NestedGrantCore(c); }
        finally {
            foreach (var key in strings.Keys.Except(original.Keys).ToArray()) strings.Remove(key);
            foreach (var pair in original) strings[pair.Key] = pair.Value;
        }
        Check(ReferenceEquals(strings, AccessTools.Field(factory, "Strings").GetValue(null)) && strings.Count == original.Count && original.All(pair => strings.TryGetValue(pair.Key, out var value) && value == pair.Value), "outside-cache nested test restores the exact global factory strings and all production values");
        Check(!strings.Keys.Any(key => key.StartsWith("cotw-wrath.psychic.full.FerociousBeastPassive88", StringComparison.Ordinal) || key.StartsWith("cotw-wrath.psychic.full.FerociousBeastPassiveFact88", StringComparison.Ordinal)), "Mode88 staged test locales are excluded from the exported production package");
    }
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static void NestedGrantCore(BlueprintsCache c)
    {
        // Exercise an indirect ability grant inside an otherwise constant effect.
        // Staged assets do not enter the real cache or production asset inventory.
        var overrun = (Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility)c.Load(BlueprintGuid.Parse("1a3b471ecea51f7439a946b23577fd70"));
        // EntityFact identifies authored runtime components by their name.
        // Distinct names are required even for this outside-cache donor graph.
        var nested = new BlueprintFeature { name = "01431NestedConstantWithActivePayload", AssetGuid = BlueprintGuid.Parse("a1eab3f8062e4887820e54f3a6810431"), ComponentsArray = new BlueprintComponent[] { new AddFacts { name = "01431NestedGrants" }, new AddStatBonus { name = "01431NestedReflex", Stat = StatType.SaveReflex, Value = 3, Descriptor = ModifierDescriptor.UntypedStackable } } };
        Set(nested.GetComponent<AddFacts>(), "m_Facts", new[] { Ref<BlueprintUnitFactReference>(overrun) }); nested.OnEnable();
        var payload = new BlueprintBuff { name = "01431ConstantNestedPayloadBuff", AssetGuid = BlueprintGuid.Parse("8327177108054c6094a43f3f68a10431"), Stacking = StackingType.Stack, ComponentsArray = new BlueprintComponent[] { new AddFacts() } };
        Set(payload.GetComponent<AddFacts>(), "m_Facts", new[] { Ref<BlueprintUnitFactReference>(nested) }); payload.OnEnable();
        var gate = new BlueprintFeature { name = "01431ConstantPayloadGate", AssetGuid = BlueprintGuid.Parse("a85cdd27d8af4c4b9430a0c4d6510431"), ComponentsArray = Array.Empty<BlueprintComponent>() }; gate.OnEnable();
        var condition = new Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasFact(); Set(condition, "m_Fact", Ref<BlueprintUnitFactReference>(gate));
        var apply = new ContextActionApplyBuff { Permanent = true, AsChild = true, DurationValue = new Kingmaker.UnitLogic.Mechanics.ContextDurationValue { DiceCountValue = 0, BonusValue = 0 } }; Set(apply, "m_Buff", Ref<BlueprintBuffReference>(payload));
        var sourceHost = (BlueprintBuff)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(Host(c, 0), null);
        var assetsType = Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets");
        sourceHost.ComponentsArray = (BlueprintComponent[])AccessTools.Method(assetsType, "CopyData").MakeGenericMethod(typeof(BlueprintComponent[])).Invoke(null, new object[] { sourceHost.ComponentsArray });
        var actions = sourceHost.GetComponents<Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions>().First(a => (a.Activated?.Actions ?? Array.Empty<Kingmaker.ElementsSystem.GameAction>()).OfType<Conditional>().Any());
        actions.Activated = new Kingmaker.ElementsSystem.ActionList { Actions = actions.Activated.Actions.Concat(new Kingmaker.ElementsSystem.GameAction[] { new Conditional { ConditionsChecker = new Kingmaker.ElementsSystem.ConditionsChecker { Conditions = new Kingmaker.ElementsSystem.Condition[] { condition } }, IfTrue = new Kingmaker.ElementsSystem.ActionList { Actions = new Kingmaker.ElementsSystem.GameAction[] { apply } }, IfFalse = new Kingmaker.ElementsSystem.ActionList { Actions = Array.Empty<Kingmaker.ElementsSystem.GameAction>() } } }).ToArray() };
        var assets = assetsType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(BlueprintsCache) }, null).Invoke(new object[] { c });
        var pool = (BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse("28710502f46848d48b3f0d6132817c4e"));
        // An action root can have a passive ancillary BuffExtraEffects rider.
        // Reject the whole root, including a nested ability grant, before copying
        // that rider. The source graph is deliberately kept outside the cache.
        var ancillary = new BlueprintBuff { name = "01431ActiveAncillaryBuff", AssetGuid = BlueprintGuid.Parse("81ba623768a14b4b9b1a942432710431"), ComponentsArray = new BlueprintComponent[] { new AddStatBonus { Stat = StatType.SaveWill, Value = 11, Descriptor = ModifierDescriptor.UntypedStackable } } }; ancillary.OnEnable();
        var extra = new BuffExtraEffects(); Set(extra, "m_CheckedBuff", Ref<BlueprintBuffReference>(sourceHost)); Set(extra, "m_ExtraEffectBuff", Ref<BlueprintBuffReference>(ancillary));
        var activeGate = new BlueprintFeature { name = "01431NestedActionRootWithAncillary", AssetGuid = BlueprintGuid.Parse("13cab66b0c384a879b195ff6fac10431"), ComponentsArray = new BlueprintComponent[] { new AddFacts(), extra } }; Set(activeGate.GetComponent<AddFacts>(), "m_Facts", new[] { Ref<BlueprintUnitFactReference>(nested) }); activeGate.OnEnable();
        Check(!(bool)AccessTools.Method(Module, "IsConstantPower").Invoke(null, new object[] { activeGate }), "deep root classifier rejects indirect active grants");
        var unknown = new BlueprintFeature { name = "01431UnresolvedActionGrant", AssetGuid = BlueprintGuid.Parse("b870aa21b51c4d40a8d672ea8fa10431"), ComponentsArray = new BlueprintComponent[] { new AddFacts() } };
        var unresolved = new BlueprintUnitFactReference(); Set(unresolved, "deserializedGuid", BlueprintGuid.Parse("aec5f98c3bde4c8599b70ee097410431"));
        Set(unknown.GetComponent<AddFacts>(), "m_Facts", new[] { unresolved }); unknown.OnEnable();
        Check(!(bool)AccessTools.Method(Module, "IsConstantPower").Invoke(null, new object[] { unknown }), "a nonempty unresolved grant is rejected rather than presumed constant");
        var stagedPool = (BlueprintFeatureSelection)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(pool, null); Set(stagedPool, "m_AllFeatures", PoolRefs(pool, "m_AllFeatures").Concat(new[] { Ref<BlueprintFeatureReference>(activeGate) }).ToArray());
        var activeBefore = Definition(activeGate.ComponentsArray);
        var result = (List<Kingmaker.ElementsSystem.GameAction>)AccessTools.Method(Module, "PassiveEffects").Invoke(null, new object[] { assets, sourceHost, new[] { stagedPool }, 88 });
        var stagedAssets = ((IDictionary)Get(assets, "Assets")).Values.Cast<BlueprintScriptableObject>().ToArray();
        foreach (BlueprintScriptableObject staged in stagedAssets) staged.OnEnable();
        Check(!stagedAssets.Any(asset => asset.GetComponent<AddStatBonus>() is AddStatBonus bonus && bonus.Stat == StatType.SaveWill && bonus.Value == 11), "an active root's constant ancillary payload is never cloned for sharing");
        var nestedClone = stagedAssets.OfType<BlueprintFeature>().Single(f => f.GetComponent<AddStatBonus>()?.Stat == StatType.SaveReflex);
        Check(nestedClone.GetComponent<AddStatBonus>().Value == 3 && !nestedClone.GetComponents<AddFacts>().SelectMany(a => a.Facts).Any(), "staged private nested fact retains the native constant bonus and removes its active leaf");
        var nestedGate = result.OfType<Conditional>().Single(a => a.ConditionsChecker.Conditions.OfType<FerociousBeastCasterHasFact01431>().Any(conditionGate => ReferenceEquals(conditionGate.Fact, gate)));
        Check(nestedGate.IfTrue.Actions.OfType<ContextActionApplyBuff>().Single().Buff.GetComponent<AddFacts>().Facts.Single() == nestedClone, "rewritten gate applies the private payload containing the exact private nested fact");
        var shared = new BlueprintBuff { name = "01431StagedNestedConstantWrapper", AssetGuid = BlueprintGuid.Parse("37979a5749f64ab7b86f4c1f22510431"), ComponentsArray = new BlueprintComponent[] {
            new Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions { Activated = new Kingmaker.ElementsSystem.ActionList { Actions = result.ToArray() }, Deactivated = new Kingmaker.ElementsSystem.ActionList { Actions = Array.Empty<Kingmaker.ElementsSystem.GameAction>() }, NewRound = new Kingmaker.ElementsSystem.ActionList { Actions = Array.Empty<Kingmaker.ElementsSystem.GameAction>() }, Dispose = new Kingmaker.ElementsSystem.ActionList { Actions = Array.Empty<Kingmaker.ElementsSystem.GameAction>() } }
        } }; shared.OnEnable();
        var owner = Unit(c, "nested-source"); var pet = Pet(c, owner, "nested-target"); var gateFact = Add(owner, gate);
        Presence(owner, activeGate);
        Check(ReferenceEquals(pet.Stats.GetStat(StatType.SaveReflex), pet.Stats.SaveReflex) && ReferenceEquals(pet.Stats.GetStat(StatType.SaveWill), pet.Stats.SaveWill), "native stats lookup resolves actual saving throw fields");
        int reflex = pet.Stats.SaveReflex.ModifiedValue, will = pet.Stats.SaveWill.ModifiedValue;
        var buff = new Buff(shared, new MechanicsContext(owner, pet.Descriptor, shared), null); pet.Facts.Add(buff);
        Console.WriteLine("Ferocious nested gate: on=" + gateFact.IsTurnedOn + ", active=" + gateFact.IsActive + ", exact=" + ReferenceEquals(gateFact.Blueprint, nestedGate.ConditionsChecker.Conditions.OfType<FerociousBeastCasterHasFact01431>().Single().Fact) + ", caster=" + ReferenceEquals(buff.Context.MaybeCaster, owner));
        Console.WriteLine("Ferocious nested blueprint: " + nestedClone.name + ", ranks=" + nestedClone.Ranks + ", components=" + string.Join(",", nestedClone.ComponentsArray.Select(component => component.GetType().Name)));
        Console.WriteLine("Ferocious nested target facts: " + string.Join(";", pet.Facts.List.Select(fact => fact.Blueprint.name + ":" + fact.GetType().Name + ":on=" + fact.IsTurnedOn + ":active=" + fact.IsActive + ":rank=" + fact.GetRank())));
        var privateFact = pet.Progression.Features.Enumerable.Single(feature => feature.Blueprint == nestedClone);
        Check(privateFact.Components.Any(runtime => runtime.SourceBlueprintComponent is AddFacts && runtime.IsTurnedOn) && privateFact.Components.Any(runtime => runtime.SourceBlueprintComponent is AddStatBonus && runtime.IsTurnedOn), "native private nested feature constructs and activates both distinctly named gameplay components");
        Console.WriteLine("Ferocious nested reflex: before=" + reflex + ", after=" + pet.Stats.SaveReflex.ModifiedValue + ", nativeGetStat=" + pet.Stats.GetStat(StatType.SaveReflex).ModifiedValue + ", owner=" + owner.Stats.SaveReflex.ModifiedValue + ", modifiers=" + string.Join(";", (pet.Stats.SaveReflex.GetModifiers(ModifierDescriptor.UntypedStackable) ?? Enumerable.Empty<ModifiableValue.Modifier>()).Select(modifier => modifier.ModValue + ":" + modifier.Source?.Blueprint.name)));
        Console.WriteLine("Ferocious active-root ancillary will: before=" + will + ", after=" + pet.Stats.SaveWill.ModifiedValue + ", modifiers=" + string.Join(";", (pet.Stats.SaveWill.GetModifiers(ModifierDescriptor.UntypedStackable) ?? Enumerable.Empty<ModifiableValue.Modifier>()).Select(modifier => modifier.ModValue + ":" + modifier.Source?.Blueprint.name)));
        Check(pet.Stats.SaveReflex.ModifiedValue == reflex + 3, "nested private feature preserves its constant native stat component");
        Check(!pet.Abilities.Enumerable.Any(a => a.Blueprint.AssetGuid == overrun.AssetGuid), "nested AddFacts active Overrun is filtered from the pet");
        Check(nested.GetComponent<AddFacts>().Facts.Single() == overrun, "nested source AddFacts remains unchanged");
        Check(pet.Stats.SaveWill.ModifiedValue == will && !pet.Buffs.Enumerable.Any(effect => effect.Blueprint == ancillary), "active root's passive BuffExtraEffects ancillary benefit is not shared");
        Check(Definition(activeGate.ComponentsArray) == activeBefore, "active source root and its ancillary definition remain unchanged");
        pet.Facts.Remove(buff); Check(pet.Stats.SaveReflex.ModifiedValue == reflex, "nested private constant feature is removed by actual wrapper lifecycle");
    }
    static void DaemonDonor(BlueprintsCache c)
    {
        var root = (BlueprintFeature)c.Load(BlueprintGuid.Parse("45102fd7aab96f94d81ec80768549e12"));
        var basis = (BlueprintBuff)c.Load(BlueprintGuid.Parse("a8957a1c6f212244d969645bc2fa7c25"));
        Check(root != null && basis != null, "real Daemon Lesser root and complete native extra-effect donor are present");
        var donorIds = new[] { "0eaf9696c9eb46f49898b6a5938bd2fb", "7c3e509fae07eaa458eae0d6860b922f", "e63719ab6f539ab4bb6aaae135f30736" };
        var donors = donorIds.Select(id => (BlueprintBuff)c.Load(BlueprintGuid.Parse(id))).ToArray();
        Check(donors.All(b => b?.GetComponent<SavingThrowBonusAgainstDescriptor>() != null), "all three real Daemon Lesser save payloads are hydrated");
        var before = Definition(root.ComponentsArray) + Definition(basis.ComponentsArray) + string.Join("", donors.Select(b => Definition(b.ComponentsArray)));
        var extras = root.GetComponents<BuffExtraEffects>().ToArray();
        Check(extras.Length == 2 && extras.Any(e => e.CheckedBuff?.AssetGuid == Host(c, 0).AssetGuid) && extras.Any(e => e.CheckedBuff?.AssetGuid == BlueprintGuid.Parse("5eac31e457999334b98f98b60fc73b2f")), "native Daemon checks ordinary rage and actual bloodrage donors");
        Check(!extras.Any(e => e.CheckedBuff?.AssetGuid == Host(c, 1).AssetGuid || ((BlueprintBuffReference[])Get(e, "m_CheckedBuffList") ?? Array.Empty<BlueprintBuffReference>()).Any(r => r.Guid == Host(c, 1).AssetGuid)), "current native Daemon does not support focused-rage host");
        Check(!Definition(Host(c, 1).ComponentsArray).Contains("ref:" + basis.AssetGuid), "native focused-rage activation graph has no alternate Daemon base application");
        foreach (int mode in new[] { 0, 1, 2 }) foreach (int tier in new[] { 0, 1, 2 }) {
            var owner = Unit(c, "daemon-owner-" + mode + "-" + tier); var pet = Pet(c, owner, "daemon-pet-" + mode + "-" + tier);
            var wrapper = Asset<BlueprintBuff>(c, "FerociousBeastConstantPowers" + mode);
            Buff ApplyShared() { var b = new Buff(wrapper, new MechanicsContext(owner, pet.Descriptor, wrapper), null); pet.Facts.Add(b); return b; }
            var absent = ApplyShared(); Check(!pet.Buffs.Enumerable.Any(b => b.IsTurnedOn && b.Blueprint.GetComponents<SavingThrowBonusAgainstDescriptor>().Any()), "no Daemon root gives no conditional save rider mode" + mode); pet.Facts.Remove(absent);
            Presence(owner, root);
            foreach (var id in new[] { "d673c30720e8e7c4bb0903dc3c9ab649", "9a2f0ffe517d221459640a4ad85710d7" }.Take(tier)) Presence(owner, (BlueprintFeature)c.Load(BlueprintGuid.Parse(id)));
            var shared = ApplyShared(); var leaves = pet.Buffs.Enumerable.Where(b => b.IsTurnedOn && b.Blueprint.GetComponents<SavingThrowBonusAgainstDescriptor>().Any()).ToArray();
            if (leaves.Length != (mode == 1 ? 0 : 1)) {
                var gates = wrapper.GetComponents<Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions>().SelectMany(a => a.Activated.Actions).OfType<Conditional>().SelectMany(a => a.ConditionsChecker.Conditions).OfType<FerociousBeastCasterHasFact01431>().ToArray();
                Console.WriteLine("Daemon failure diagnostic: mode=" + mode + ", tier=" + tier + ", root=" + root.name + ":" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(root) + ", globalCache=" + ReferenceEquals(ResourcesLibrary.BlueprintsCache,c) + ", caster=" + ReferenceEquals(shared.Context.MaybeCaster,owner) + ", ownerHasRoot=" + owner.HasFact(root) + ", gates=" + string.Join(";",gates.Select(g=>g.Fact?.name+":"+(g.Fact==null?0:System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(g.Fact))+":match="+ReferenceEquals(g.Fact,root)+":has="+owner.HasFact(g.Fact))) + ", petBuffs=" + string.Join(";", pet.Buffs.Enumerable.Select(b=>b.Blueprint.name+":"+b.IsTurnedOn)));
            }
            Check(leaves.Length == (mode == 1 ? 0 : 1), "real Daemon rider reaches pet only for the native-supported ordinary/bloodrage modes " + mode + "/" + tier);
            if (mode != 1) {
                Check(leaves.Single().Blueprint.GetComponent<SavingThrowBonusAgainstDescriptor>().Value == tier + 2, "Daemon native tier gate evaluates owning caster powers for pet bonus " + mode + "/" + tier);
                Check(leaves.Single().Context.MaybeCaster == owner && leaves.Single().Owner == pet, "native caster gates retain owner while nested ToCaster payload is redirected to pet");
            }
            Check(!owner.Buffs.Enumerable.Any(b => b.IsTurnedOn && b.Blueprint.GetComponents<SavingThrowBonusAgainstDescriptor>().Any()), "sharing does not accidentally reapply ToCaster save rider to the master");
            pet.Facts.Remove(shared); Check(!pet.Buffs.Enumerable.Any(b => b.IsTurnedOn && b.Blueprint.GetComponents<SavingThrowBonusAgainstDescriptor>().Any()), "native child ownership removes all nested Daemon save payloads");
        }
        Check(before == Definition(root.ComponentsArray) + Definition(basis.ComponentsArray) + string.Join("", donors.Select(b => Definition(b.ComponentsArray))), "precise bloodrage support and target rewrite preserve original Daemon source graph");
        var liveOwner = Unit(c, "daemon-source-switch"); var livePet = Pet(c, liveOwner, "daemon-pet-switch"); var resource = Resource(c, 0); liveOwner.Resources.Add(resource, true);
        Add(liveOwner, Asset<BlueprintFeature>(c, "FerociousBeastFeature")); Add(liveOwner, Asset<BlueprintFeature>(c, "FerociousBeastGreaterFeature"));
        var liveDonor = Add(liveOwner, root); var liveRage = Rage(liveOwner, Host(c, 0));
        int PetLeaves() => livePet.Buffs.Enumerable.Count(b => b.IsTurnedOn && b.Blueprint.GetComponents<SavingThrowBonusAgainstDescriptor>().Any());
        Check(PetLeaves() == 1, "real native Daemon donor Add then host activation grants one pet save payload");
        liveDonor.TurnOff(); liveRage.TickMechanics(); Check(PetLeaves() == 0, "real native Daemon donor off removes pet benefit by next rage round without manual Refresh");
        liveDonor.TurnOn(); liveRage.TickMechanics(); Check(PetLeaves() == 1, "real native Daemon donor on restores pet benefit by next rage round without manual Refresh");
        liveOwner.Facts.Remove(liveRage); Check(PetLeaves() == 0, "real host removal revokes the restored Daemon private payload");
    }
    static T Ref<T>(SimpleBlueprint bp) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(bp); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, bp, null); return r; }
    internal static int Run(BlueprintsCache cache)
    {
        checks = serial = petAdds = petRemoves = buffAdds = buffRemoves = resourceSpends = rankGains = rankLosses = spawnDeaths = 0; units.Clear(); rankObservers.Clear();
        var h = new Harmony("CotW01431.FerociousNativeContracts"); var observer = new Events(); EventBus.Subscribe(observer);
        var originalTime = Game.Instance.TimeController; var originalState = Game.Instance.State;
        var player = (Player)FormatterServices.GetUninitializedObject(typeof(Player)); player.GameTime = TimeSpan.FromHours(1); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true);
        var state = (PersistentState)FormatterServices.GetUninitializedObject(typeof(PersistentState)); state.PlayerState = player; Set(Game.Instance, "State", state);
        Set(Game.Instance, "TimeController", FormatterServices.GetUninitializedObject(typeof(TimeController)));
        void Patch(MethodBase method, string prefix) => h.Patch(method, prefix: new HarmonyMethod(typeof(FerociousBeast01431Tests), prefix));
        Patch(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), nameof(Clock));
        Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "IsInTurnBasedCombat"), nameof(No));
        Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), nameof(No));
        Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip));
        Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), nameof(No));
        Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), nameof(PoolIgnore));
        Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), nameof(NoController));
        // Friendly constructed actors and a neutral difficulty preset are
        // scene inputs. Native class/stat/DC/rank/metamagic arithmetic and
        // ContextCalculateAbilityParams.Recalculate remain intact.
        Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), nameof(No));
        Patch(AccessTools.Method(typeof(ContextActionSpawnFx), "RunAction"), nameof(Skip));
        // Current native code contains only prefab loading/SpawnFxOnUnit in
        // these three endpoints. Keep the original host component and all
        // manager/turn/resource hooks; the absent scene particle service is
        // not a gameplay outcome in this standalone CLR harness.
        var particles = typeof(Kingmaker.Designers.Mechanics.Buffs.BuffParticleEffectPlay);
        Patch(AccessTools.Method(particles, "OnActivate"), nameof(Skip));
        Patch(AccessTools.Method(particles, "OnDeactivate"), nameof(Skip));
        Patch(AccessTools.Method(particles, "Kingmaker.Controllers.Units.ITickEachRound.OnNewRound"), nameof(Skip));
        Patch(AccessTools.Method(typeof(UnitPartPetMaster), "UpdateMountAbility"), nameof(Skip));
        Patch(AccessTools.Method(typeof(UnitPartPetMaster), "UpdatePetEquipments"), nameof(Skip));
        Patch(AccessTools.Method(AccessTools.Property(typeof(UnitEntityData), "CombatState").PropertyType, "ReturnToStartingPositionIfNeeded"), nameof(Skip));
        h.Patch(AccessTools.Method(typeof(EntityFact), "Attach"), prefix: new HarmonyMethod(typeof(FerociousBeast01431Tests), nameof(AttachId)));
        h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Howl01428Tests), "DifficultyBoundary"));
        h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary"));
        h.Patch(AccessTools.Method(typeof(FerociousBeastHost01431).Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append", new[] { typeof(IEnumerable<BlueprintFeatureSelection>), typeof(BlueprintFeature[]) }), postfix: new HarmonyMethod(typeof(FerociousBeast01431Tests), nameof(AppendFault)));
        h.Patch(AccessTools.Method(typeof(UnitAbilityResourceCollection), "Spend"), postfix: new HarmonyMethod(typeof(FerociousBeast01431Tests), nameof(Spent)));
        Provider01419Tests.PatchLabels(h);
        var buffHelper = AccessTools.Method(typeof(UnitHelper), "AddBuff", new[] { typeof(UnitDescriptor), typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?) });
        var outerCapture = AccessTools.Method(typeof(Psychic070Tests), "CaptureDefense");
        var outerPatch = Harmony.GetPatchInfo(buffHelper)?.Prefixes.SingleOrDefault(p => p.PatchMethod == outerCapture);
        if (outerPatch != null) h.Unpatch(buffHelper, outerCapture);
        try {
            Check(!(Harmony.GetPatchInfo(buffHelper)?.Prefixes.Any(p => p.PatchMethod == outerCapture) ?? false), "outer null-buff capture is absent while real native UnitHelper/manager lifecycle executes");
            // Earlier suites construct new class objects in this same cache.
            // Rebind the nonserialized archetype-parent cache to those objects,
            // retaining the actual game's full authored archetype GUID list.
            var blood = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("d77e67a814d686842802c9cfd8ef8499"));
            var primal = (BlueprintArchetype)cache.Load(BlueprintGuid.Parse("4d588290ed0736e4f8cd3f4c3beacd69"));
            var bloodRow = PsychicExpansionTests.NativeRows.Single(row => (string)row["blueprint"]["AssetId"] == blood.AssetGuid.ToString())["blueprint"]["Data"];
            Set(blood, "m_Archetypes", PsychicExpansionTests.Read(bloodRow["m_Archetypes"], typeof(BlueprintArchetypeReference[])));
            Set(primal, "m_ParentClass", blood);
            Check(blood.Archetypes.Contains(primal) && primal.GetParentClass() == blood, "current native Primalist GUID membership and runtime parent cache agree");
            foreach (var mode in Enumerable.Range(0, 3)) {
                var resource = Resource(cache, mode);
                var row = PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == resource.AssetGuid.ToString())["blueprint"]["Data"];
                foreach (var fieldName in new[] { "m_MaxAmount", "m_Min", "m_Max", "m_UseMin", "m_UseMax" }) {
                    var field = AccessTools.Field(typeof(BlueprintAbilityResource), fieldName);
                    if (field != null && row[fieldName] != null) field.SetValue(resource, PsychicExpansionTests.Read(row[fieldName], field.FieldType));
                }
                Check(Definition(Get(resource, "m_MaxAmount")) != "null", "native rage maximum-amount formula hydrated mode" + mode);
            }
            var swiftRoot = (BlueprintFeature)cache.Load(BlueprintGuid.Parse("7df32d4e9bd2cdc48b0f69b03a57754a"));
            var swiftRow = PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == swiftRoot.AssetGuid.ToString())["blueprint"]["Data"];
            swiftRoot.Ranks = (int)swiftRow["Ranks"]; Check(swiftRoot.Ranks == 3, "actual native Swift Foot maximum ranks retained for multi-source ownership");
            ExistingConstantDonors(cache); Registration(cache, h); Selection(cache); for (int mode = 0; mode < 3; mode++) Lifecycle(cache, mode); Divine(cache); MultipleAndLimited(cache); ActualConstantDonors(cache, h); NestedGrant(cache); DaemonDonor(cache);
            Check(petAdds >= 8 && petRemoves >= 3 && buffAdds > 0 && buffRemoves > 0 && resourceSpends >= 7, "real native pet/buff EventBus and resource Spend paths observed");
            var loaded = (IDictionary)Get(cache, "m_LoadedBlueprints");
            var productionAssets = loaded.Keys.Cast<BlueprintGuid>().Select(cache.Load).OfType<BlueprintScriptableObject>()
                .Where(b => b.name.StartsWith("CotWWrathPsychicFerociousBeast", StringComparison.Ordinal) && b.AssetGuid != Id("FerociousBeastDivineBaseRage")).ToArray();
            PsychicExpansionTests.Inventory["rage01431Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks,
                ["additionalItemCount"] = 2, ["itemIds"] = new JArray("COTW-0281", "COTW-0282"), ["newItemCount"] = 2, ["distinctRootCount"] = 2,
                ["newBlueprints"] = JArray.FromObject(productionAssets.Select(b => new { id = b.AssetGuid.ToString(), name = b.name, type = b.GetType().FullName })),
                ["nativePetAddedEvents"] = petAdds, ["nativePetRemovedEvents"] = petRemoves,
                ["nativeBuffAddedEvents"] = buffAdds, ["nativeBuffRemovedEvents"] = buffRemoves, ["nativeResourceSpendCalls"] = resourceSpends,
                ["nativeFeatureRankGainEvents"] = rankGains, ["nativeFeatureRankLossEvents"] = rankLosses,
                ["nativeSpawnOnlyDeathEvents"] = spawnDeaths, ["petDeathImmediateExactOwnershipCleanup"] = true,
                ["nativeManagerLifecycle"] = true, ["nativePetOwnershipLifecycle"] = true, ["consumedTemporaryHPKeptAcrossSuppression"] = true,
                ["nativeUnitHelperAddBuffUncaptured"] = true, ["outerCaptureRestoredAfterScope"] = true,
                ["nativeContextParameterArithmeticPreserved"] = true,
                ["actualNativeAnimalFuryConstant"] = true, ["optionalPrimalistGroupAndGroupsLayouts"] = true, ["optionalProviderFailuresIsolated"] = true, ["deepActivePayloadFiltered"] = true, ["activeRootAncillaryBenefitsExcluded"] = true, ["acceptedDonorGraphsPreserved"] = true,
                ["actualDaemonDonorTiers"] = true, ["primalistActualBloodrageExtraEffect"] = true, ["nativeFocusedDaemonUnsupported"] = true, ["unresolvedGrantRejected"] = true,
                ["directDonorTurnOffOnNextNativeRound"] = true, ["rankedDonorChangeNextNativeRound"] = true,
                ["personalRageModes"] = new JArray("barbarian", "focused rage", "primalist bloodrage", "Divine Anger private resource"),
                ["skaldRestriction"] = "Ferocious Beast requires additional personal rage rounds. Pure Skald and Inspired Rage do not qualify; this differs from the accepted Howl standard-action path.",
                ["boundary"] = "Constructed native units/game state. Original manager/fact/runtime activation, deactivation, turn on/off, pet AddPet/RemovePet, EventBus, Buff.TickMechanics, temporary-HP HandleDamage, source resource Spend, native feature rank merge/removal, native Animal Fury AddSecondaryAttacks ownership/restoration and Daemon nested caster/target child-buff ownership execute. Current TTT authoring is reproduced from the real native pool's Group/Groups and feature arrays; invalid/late providers and append faults use controlled inputs. Ferocious Base/Greater availability updates through native events. Generic donor direct turn off/on and rank changes synchronize by the next native rage round, without manual Refresh; immediate generic-donor notifications are not claimed. No replacement of tested production hooks. Scene FX, pet mount/equipment setup, limb item allocation/removal and difficulty-settings access are bounded; native PersonageLimits Bite1/Claw2, fixed game time and empty native party. Limitless and active/tier-root gate inputs use exact cached blueprint identity with unrelated activation payload temporarily bounded and immediately restored. Synthetic nested grant/BuffExtraEffects graphs are staged outside the cache. UI, complete serialization/reload, actual combat time and external DLL combinations remain game tester cases." };
            Console.WriteLine("PASS: " + checks + " Ferocious Beast01431 native contracts."); return checks;
        } finally {
            try { foreach (var u in units.AsEnumerable().Reverse()) foreach (var fact in u.Facts.List.ToArray()) if (fact.Manager != null) u.Facts.Remove(fact); }
            finally {
                foreach (var rankObserver in rankObservers) EventBus.Unsubscribe(rankObserver);
                EventBus.Unsubscribe(observer); h.UnpatchAll(h.Id); Set(Game.Instance, "TimeController", originalTime); Set(Game.Instance, "State", originalState);
                if (outerPatch != null) {
                    new Harmony(outerPatch.owner).Patch(buffHelper, prefix: new HarmonyMethod(outerCapture) { priority = outerPatch.priority, before = outerPatch.before, after = outerPatch.after });
                    var restored = Harmony.GetPatchInfo(buffHelper)?.Prefixes.SingleOrDefault(p => p.PatchMethod == outerCapture);
                    if (restored == null || restored.owner != outerPatch.owner || restored.priority != outerPatch.priority || !restored.before.SequenceEqual(outerPatch.before) || !restored.after.SequenceEqual(outerPatch.after)) throw new Exception("Ferocious01431 outer null-buff capture metadata not restored");
                }
            }
        }
    }
}
