using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using System.Runtime.Serialization;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;
using FxComponent = Kingmaker.UnitLogic.Abilities.Components.Base.AbilitySpawnFx;
using FxBase = Kingmaker.UnitLogic.Abilities.Components.Base.AbilitySpawnFx;

internal static class Howl01427Tests
{
    static int checks, saves, fears, fx, delivery, beforeEffects, afterEffects, castEvents;
    static bool saved, failAppend;
    static UnitEntityData[] nearby = Array.Empty<UnitEntityData>();
    static readonly HashSet<UnitEntityData> enemies = new HashSet<UnitEntityData>();
    static readonly List<Buff> immunities = new List<Buff>();
    static Type Module => typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.TerrifyingHowl", true);
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static T Helper<T>(string n, params object[] args) => (T)AccessTools.Method(typeof(Migration01426Tests), n).Invoke(null, args);
    static BlueprintGuid Id(string token) => Helper<BlueprintGuid>("Id", token);
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static void Check(bool ok, string why) { if (!ok) throw new Exception("Howl01427: " + why); checks++; }
    static bool No(ref bool __result) { __result = false; return false; }
    static int sharedGrants, sharedRemovals;
    static bool Grant(UnitEntityData __0, BlueprintUnitFact __1, ref EntityFact __result) {
        Check(__1.AssetGuid == Id("TerrifyingHowlRagePower"), "native song dispatches precisely the owned Howl feature");
        __result = Helper<Feature>("Fact", __0, (BlueprintFeature)__1); sharedGrants++; return false;
    }
    static bool RemoveShared(UnitEntityData __0, EntityFact __1) { __0.Facts.List.Remove(__1); AccessTools.Method(Get(__0.Facts, "m_Cache").GetType(), "Clear").Invoke(Get(__0.Facts, "m_Cache"), null); sharedRemovals++; return false; }
    static bool Nearby(UnityEngine.Vector3 __0, Feet __1, bool __2, bool __3, ref IEnumerable<UnitEntityData> __result) {
        Check(__1.Value == 30 && __2 && !__3, "native selector requests 30ft, line of sight and no dead units");
        __result = nearby; return false;
    }
    static bool Enemy(UnitEntityData __instance, UnitEntityData __0, ref bool __result) { __result = enemies.Contains(__0) != enemies.Contains(__instance); return false; }
    static bool Save(RuleSavingThrow rule, ref RuleSavingThrow __result) { saves++; Check(rule.Type == SavingThrowType.Will && rule.DifficultyClassBase == 18, "actual save dispatched with final DC"); __result = rule; return false; }
    static bool Passed(ref bool __result) { __result = saved; return false; }
    static bool Apply(ContextActionApplyBuff __instance) {
        var c = ContextData<MechanicsContext.Data>.Current.Context; var t = ContextData<MechanicsContext.Data>.Current.CurrentTarget.Unit;
        if (__instance.Buff.AssetGuid == Id("TerrifyingHowlImmunity")) {
            Check(__instance.DurationValue.Rate == DurationRate.Days && __instance.DurationValue.BonusValue.Calculate(c) == 1 && __instance.IsNotDispelable, "24h nondispellable immunity");
            immunities.Add(Psychic034Tests.Fact(t, __instance.Buff, c.MaybeCaster, 8));
        } else {
            Check(__instance.Buff.AssetGuid.ToString() == "f08a7239aa961f34c8301518e71d4cdf", "native frightened buff dispatched");
            Check(__instance.DurationValue.DiceType == DiceType.D4 && __instance.DurationValue.DiceCountValue.Calculate(c) == 1 && __instance.DurationValue.BonusValue.Calculate(c) == 1, "1d4+1 native duration"); fears++;
        }
        return false;
    }
    static bool Spawn(FxComponent __instance) {
        // Native execution and SpawnFxs select the actual visual; Unity rendering is the endpoint boundary.
        Check(__instance.PrefabLink.AssetId == "d80d51c0a08f35140b10dd1526e540c4" && __instance.Time == AbilitySpawnFxTime.OnApplyEffect, "native Dazzling Display FX dispatch");
        fx++; return false;
    }
    sealed class Observer : IApplyAbilityEffectHandler {
        public void OnAbilityEffectApplied(AbilityExecutionContext c) { castEvents++; }
        public void OnTryToApplyAbilityEffect(AbilityExecutionContext c, TargetWrapper t) { beforeEffects++; }
        public void OnAbilityEffectAppliedToTarget(AbilityExecutionContext c, TargetWrapper t) { afterEffects++; }
    }
    static bool Raise(Action<IApplyAbilityEffectHandler> action, bool unused) { action(new Observer()); return true; }
    static T Rule<T>(T rule) where T : RulebookEvent { if (rule is RuleApplySpell) delivery++; return rule; }
    static IEnumerable<CodeInstruction> ProcessBoundary(IEnumerable<CodeInstruction> code) {
        foreach (var i in code) {
            if (i.operand is MethodInfo m && m.IsGenericMethod && m.DeclaringType == typeof(EventBus) && m.GetGenericArguments().SequenceEqual(new[] { typeof(IApplyAbilityEffectHandler) })) {
                i.opcode = OpCodes.Call; i.operand = AccessTools.Method(typeof(Howl01427Tests), nameof(Raise));
            } else if (i.operand is MethodInfo r && r.IsGenericMethod && r.DeclaringType == typeof(Rulebook) && r.Name == "Trigger") {
                i.opcode = OpCodes.Call; i.operand = AccessTools.Method(typeof(Howl01427Tests), nameof(Rule)).MakeGenericMethod(r.GetGenericArguments());
            }
            yield return i;
        }
    }
    static readonly MethodInfo Process = AccessTools.Method(typeof(AbilityExecutionProcess), "ApplyEffect", new[] { typeof(AbilityExecutionContext), typeof(TargetWrapper), typeof(AbilityApplyEffect), typeof(AbilitySelectTarget), typeof(bool) });
    static AbilityExecutionContext Context(BlueprintAbility a, UnitEntityData caster) {
        var data = new AbilityData(a, caster.Descriptor);
        var ctx = new AbilityExecutionContext(data, new AbilityParams { CasterLevel = 8, DC = 18 }, new TargetWrapper(UnityEngine.Vector3.zero), null, caster);
        ctx.FxSpawners = a.GetComponents<FxBase>().ToList(); return ctx;
    }
    static void Cast(BlueprintAbility a, UnitEntityData caster) {
        var ctx = Context(a, caster);
        Process.Invoke(null, new object[] { ctx, new TargetWrapper(UnityEngine.Vector3.zero), a.GetComponent<AbilityEffectRunAction>(), a.GetComponent<AbilityTargetsAround>(), true });
    }
    static void AppendFault(BlueprintFeature[] feats) { if (failAppend && feats.Any(f => f.AssetGuid == Id("TerrifyingHowlRagePower"))) throw new InvalidOperationException("01427 optional append injection"); }

    internal static int Run(BlueprintsCache cache) {
        checks = saves = fears = fx = delivery = beforeEffects = afterEffects = castEvents = 0; enemies.Clear(); immunities.Clear();
        bool old = Environment.GetEnvironmentVariable("COTW_HOWL_01426_REPRO") == "1";
        var h = new Harmony("CotW01427.HowlNativeProcess");
        void Patch(MethodBase m, string n) => h.Patch(m, prefix: new HarmonyMethod(typeof(Howl01427Tests), n));
        Patch(AccessTools.Method(typeof(Kingmaker.Designers.GameHelper), "GetTargetsAround", new[] { typeof(UnityEngine.Vector3), typeof(Feet), typeof(bool), typeof(bool) }), nameof(Nearby));
        foreach (string n in new[] { "IsEnemy", "CanAttack" }) Patch(AccessTools.Method(typeof(UnitEntityData), n, new[] { typeof(UnitEntityData) }), nameof(Enemy));
        Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayerFaction"), nameof(No));
        h.Patch(Process, transpiler: new HarmonyMethod(typeof(Howl01427Tests), nameof(ProcessBoundary)));
        h.Patch(AccessTools.Method(typeof(AbilityExecutionProcess), "DoApplyEffect"), transpiler: new HarmonyMethod(typeof(Howl01427Tests), nameof(ProcessBoundary)));
        PsychicRuleTestBoundary.Patch(h, typeof(RuleSavingThrow), AccessTools.Method(typeof(Howl01427Tests), nameof(Save)));
        Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow), "IsPassed"), nameof(Passed));
        Patch(AccessTools.Method(typeof(ContextActionApplyBuff), "RunAction"), nameof(Apply));
        Patch(AccessTools.Method(typeof(FxComponent), "Spawn"), nameof(Spawn));
        try {
            var a = Asset<BlueprintAbility>(cache, "TerrifyingHowlAbility");
            var caster = Helper<UnitEntityData>("Unit", cache, "01427-howler"); var target = Helper<UnitEntityData>("Unit", cache, "01427-victim");
            enemies.Add(target); nearby = new[] { caster, target };
            ((sbyte[])Get(target.State, "m_Conditions"))[(int)UnitCondition.Shaken] = 1;
            if (old) {
                bool failed = false; try { Cast(a, caster); } catch (TargetInvocationException ex) { failed = ex.InnerException is NullReferenceException && ex.InnerException.StackTrace.Contains("AbilityTargetsAround.Select"); }
                Check(failed && saves == 0 && fears == 0 && fx == 0, "published026 native process fails in target selection before saves and FX");
                PsychicExpansionTests.Inventory["howl01427OldReproduction"] = new JObject { ["passed"] = true, ["nullConditionFailureBeforeEffectAndFx"] = true, ["assertions"] = checks };
                return checks;
            }
            Check(Get(a.GetComponent<AbilityTargetsAround>(), "m_Condition") is ConditionsChecker, "selector owns required condition object");
            saved = false; Cast(a, caster); Check(saves == 1 && fears == 1 && fx > 0 && afterEffects == 1 && castEvents == 1, "native process delivers fear and visible/log event path: " + saves + "/" + fears + "/" + fx + "/" + afterEffects + "/" + castEvents);
            int previousFx = fx; Cast(a, caster); Check(saves == 1 && fears == 1 && fx > previousFx, "immune target does not save again; activation still has visual feedback");
            var other = Helper<UnitEntityData>("Unit", cache, "01427-other-howler"); saved = true; Cast(a, other); Check(saves == 2 && fears == 1, "independent caster save success applies immunity without fear");
            Cast(a, other); Check(saves == 2 && immunities.Count == 2, "both caster immunity facts remain effective");
            // The native expiry controller removes the Buff; model that scene endpoint,
            // retaining the other caster's record and executing the native predicate again.
            target.Facts.List.Remove(immunities[0]); var factsCache = Get(target.Facts, "m_Cache"); AccessTools.Method(factsCache.GetType(), "Clear").Invoke(factsCache, null);
            saved = false; Cast(a, caster); Check(saves == 3 && fears == 2, "expired source immunity allows that caster again");
            var fresh = Helper<UnitEntityData>("Unit", cache, "01427-fresh"); enemies.Add(fresh); nearby = new[] { fresh }; int before = saves;
            Cast(a, caster); Check(saves == before, "unshaken enemy ignored through full process");
            ((sbyte[])Get(fresh.State, "m_Conditions"))[(int)UnitCondition.Shaken] = 1;
            enemies.Remove(fresh); Cast(a, caster); Check(saves == before, "shaken ally excluded by native area selector"); enemies.Add(fresh);
            var silence = Psychic034Tests.Fact(fresh, Asset<BlueprintBuff>(cache, "SilenceBuff"), fresh, 8); Cast(a, caster); Check(saves == before, "silenced enemy ignored through native process");
            Set(silence, "IsSuppressed", true); Cast(a, caster); Check(saves == before + 1, "suppressed silence permits a real save");
            nearby = Array.Empty<UnitEntityData>(); previousFx = fx; Cast(a, caster); Check(fx > previousFx, "empty/occluded/out-of-range scene candidate set still shows cast FX");
            Check(!a.DisableLog && a.ActionType == Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard && !a.IsFullRoundAction, "native log enabled and standard action retained");
            var display = (BlueprintAbility)cache.Load(BlueprintGuid.Parse("5f3126d4120b2b244a95cb2ec23d69fb")); Check(a.Animation == display.Animation, "native cast animation retained");
            Optional(cache, h);
            PsychicExpansionTests.Inventory["howl01427Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["nativeExecutionProcess"] = true, ["nativeAreaSelector"] = true, ["nativeActionApply"] = true, ["savingThrowDispatches"] = saves, ["fearDispatches"] = fears, ["visualDispatches"] = fx, ["nativePreEffectEvents"] = beforeEffects, ["nativePostEffectEvents"] = afterEffects, ["nativeCastEvents"] = castEvents, ["tttPrimalistLateAndRollback"] = true, ["boundary"] = "Actual AbilityExecutionProcess.ApplyEffect/DoApplyEffect, native selector and AbilityEffectRunAction.Apply execute. Scene nearby/LOS set, enemy relation, save outcomes, final Buff insertion, rendering Spawn endpoint and EventBus subscribers are controlled. Native event and FX dispatch are verified, not game rendering or complete startup." };
            Console.WriteLine("PASS: " + checks + " Howl native execution and provider contracts."); return checks;
        } finally { h.UnpatchAll(h.Id); }
    }
    static void Optional(BlueprintsCache cache, Harmony h) {
        var id = BlueprintGuid.Parse("6baf176bc22c4a41ba421bce3fcd9497"); var old = cache.Load(id); Check(old == null, "controlled TTT absent initially");
        var power = Asset<BlueprintFeature>(cache, "TerrifyingHowlRagePower");
        var native = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("28710502f46848d48b3f0d6132817c4e"));
        var copied = ((BlueprintFeatureReference[])Get(native, "m_AllFeatures")).Where(r => r.Guid != power.AssetGuid).ToArray();
        var nativeRow = PsychicExpansionTests.NativeRows.First(r => (string)r["blueprint"]["AssetId"] == native.AssetGuid.ToString())["blueprint"]["Data"];
        var provider = new BlueprintFeatureSelection { AssetGuid = id, name = "PrimalistRagePowerSelection", Group = (FeatureGroup)Enum.Parse(typeof(FeatureGroup), (string)nativeRow["Group"]), Groups = Array.Empty<FeatureGroup>(), IsClassFeature = true, ComponentsArray = Array.Empty<BlueprintComponent>() };
        Set(provider, "m_Features", copied); Set(provider, "m_AllFeatures", copied); cache.AddCachedBlueprint(id, provider);
        var method = AccessTools.Method(Module, "Optional");
        h.Patch(AccessTools.Method(typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append", new[] { typeof(IEnumerable<BlueprintFeatureSelection>), typeof(BlueprintFeature[]) }), postfix: new HarmonyMethod(typeof(Howl01427Tests), nameof(AppendFault)));
        try {
            failAppend = true; bool threw = false; try { method.Invoke(null, new object[] { cache }); } catch (TargetInvocationException ex) { threw = ex.InnerException is InvalidOperationException; } finally { failAppend = false; }
            Check(threw && ReferenceEquals(Get(provider, "m_Features"), copied) && ReferenceEquals(Get(provider, "m_AllFeatures"), copied), "optional append failure restores exact TTT arrays");
            method.Invoke(null, new object[] { cache }); method.Invoke(null, new object[] { cache });
            foreach (string field in new[] { "m_Features", "m_AllFeatures" }) {
                var now = (BlueprintFeatureReference[])Get(provider, field); Check(now.Take(copied.Length).SequenceEqual(copied) && now.Count(r => r.Guid == power.AssetGuid) == 1, "TTT snapshot pool receives one root preserving original entries");
            }
            h.Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), prefix: new HarmonyMethod(typeof(Howl01427Tests), nameof(No)));
            h.Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), prefix: new HarmonyMethod(typeof(Migration01426Tests), "PoolIgnore"));
            h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Game), "LevelUpController"), prefix: new HarmonyMethod(typeof(Migration01426Tests), "NullController"));
            var u = Helper<UnitEntityData>("Unit", cache, "01427-primalist-preview");
            var cls = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("d77e67a814d686842802c9cfd8ef8499"));
            var arch = (BlueprintArchetype)cache.Load(BlueprintGuid.Parse("4d588290ed0736e4f8cd3f4c3beacd69"));
            var cd = new ClassData(cls) { Level = 8 }; u.Progression.Classes.Clear(); u.Progression.Classes.Add(cd);
            var state = (LevelUpState)FormatterServices.GetUninitializedObject(typeof(LevelUpState)); Set(state, "m_Unit", u); Set(state, "Selections", new List<FeatureSelectionState>()); Set(state, "NextCharacterLevel", 8); state.NextClassLevel = 8; state.SelectedClass = cls;
            foreach (var pool in new[] { native, provider }) {
                var pending = state.AddSelection(null, default(FeatureSource), pool, 8);
                Check(!pool.CanSelect(u.Descriptor, state, pending, power), "ordinary Bloodrager cannot choose Howl in native/TTT pool");
                cd.Archetypes.Add(arch); Check(pool.CanSelect(u.Descriptor, state, pending, power), "Primalist8 can choose Howl in native/TTT pool");
                cd.Level = 7; state.NextClassLevel = 7; Check(!pool.CanSelect(u.Descriptor, state, pending, power), "Primalist7 cannot choose level8 power");
                cd.Level = 8; state.NextClassLevel = 8; cd.Archetypes.Clear();
            }
            var skald = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("2476514e31791394fa140f1a07941c96"));
            Check(((BlueprintFeatureReference[])Get(skald, "m_AllFeatures")).Any(r => r.Guid == power.AssetGuid), "native Skald pool includes Howl like Renewed Vigor");
            Skald(cache, h, skald, power);
        } finally { cache.RemoveCachedBlueprint(id); }
    }
    static void Skald(BlueprintsCache cache, Harmony h, BlueprintFeatureSelection pool, BlueprintFeature power) {
        var caster = Helper<UnitEntityData>("Unit", cache, "01427-skald"); var target = Helper<UnitEntityData>("Unit", cache, "01427-song-recipient");
        var cls = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("6afa347d804838b48bda16acb0573dc0"));
        var cd = new ClassData(cls) { Level = 7 }; caster.Progression.Classes.Clear(); caster.Progression.Classes.Add(cd); caster.Stats.Strength.BaseValue = 18;
        var state = (LevelUpState)FormatterServices.GetUninitializedObject(typeof(LevelUpState)); Set(state, "m_Unit", caster); Set(state, "Selections", new List<FeatureSelectionState>()); Set(state, "NextCharacterLevel", 7); state.NextClassLevel = 7; state.SelectedClass = cls;
        var pending = state.AddSelection(null, default(FeatureSource), pool, 7);
        Check(!pool.CanSelect(caster.Descriptor, state, pending, power), "Skald7 cannot select level8 Howl"); cd.Level = 8; state.NextClassLevel = 8;
        Check(pool.CanSelect(caster.Descriptor, state, pending, power), "Skald8 eligible; ordinary scheduled rage-power choice follows native progression");
        var ability = Asset<BlueprintAbility>(cache, "TerrifyingHowlAbility"); var gate = ability.GetComponent<Kingmaker.UnitLogic.Abilities.Components.CasterCheckers.AbilityCasterHasFacts>();
        Check(!gate.IsCasterRestrictionPassed(caster), "Skald outside inspired rage cannot use Howl");
        h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary"));
        h.Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), prefix: new HarmonyMethod(typeof(Migration01426Tests), "No"));
        h.Patch(AccessTools.Method(typeof(ContextCalculateAbilityParams), "Calculate", new[] { typeof(MechanicsContext), typeof(BlueprintScriptableObject), typeof(UnitEntityData), typeof(AbilityData) }), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "ParamsBoundary"));
        var param = ability.GetComponent<ContextCalculateAbilityParams>().Calculate(new AbilityData(ability, caster.Descriptor)); Check(param.CasterLevel == 8 && param.DC == 18, "native Skald level and own Strength scale Howl");
        Helper<Feature>("Fact", caster, power);
        h.Patch(typeof(UnitHelper).GetMethods().Single(m => m.Name == "AddFact" && !m.IsGenericMethod && m.GetParameters().Select(a => a.ParameterType).SequenceEqual(new[] { typeof(UnitEntityData), typeof(BlueprintUnitFact), typeof(MechanicsContext), typeof(FeatureParam) })), prefix: new HarmonyMethod(typeof(Howl01427Tests), nameof(Grant)));
        h.Patch(AccessTools.Method(typeof(UnitHelper), "RemoveFact", new[] { typeof(UnitEntityData), typeof(EntityFact) }), prefix: new HarmonyMethod(typeof(Howl01427Tests), nameof(RemoveShared)));
        foreach (string id in new[] { "75b3978757908d24aaaecaf2dc209b89", "ee51f69e69cb43c3bc7c02002e482d17" }) {
            var host = (BlueprintBuff)cache.Load(BlueprintGuid.Parse(id)); var share = host.GetComponent<AddFactsFromCaster>();
            Check(share.Facts.Count(x => x?.AssetGuid == power.AssetGuid) == 1 && share.Facts.Any(x => x?.AssetGuid.ToString() == "aad496a7c9bee224cbd01a3cf8e42061"), "normal/mythic song preserves Renewed Vigor and shares Howl once");
            var ownSong = Psychic034Tests.Fact(caster, host, caster, 8); Check(gate.IsCasterRestrictionPassed(caster), "normal/mythic inspired rage permits owner Howl");
            var victim = Helper<UnitEntityData>("Unit", cache, "01427-song-howled-enemy"); enemies.Add(victim); nearby = new[] { victim }; ((sbyte[])Get(victim.State, "m_Conditions"))[(int)UnitCondition.Shaken] = 1;
            int priorSaves = saves, priorFear = fears; saved = false; Cast(ability, caster);
            Check(saves == priorSaves + 1 && fears == priorFear + 1, "Skald normal/mythic song Howl executes native effect and fear path");
            int prior = sharedGrants; var runtime = ownSong.Components.Single(x => x.SourceBlueprintComponent is AddFactsFromCaster);
            using (runtime.RequestEventContext()) AccessTools.Method(typeof(AddFactsFromCaster), "OnActivate").Invoke(runtime.SourceBlueprintComponent, null);
            Check(sharedGrants == prior, "song never duplicates owner's permanent Howl");
            var song = Psychic034Tests.Fact(target, host, caster, 8); runtime = song.Components.Single(x => x.SourceBlueprintComponent is AddFactsFromCaster);
            using (runtime.RequestEventContext()) AccessTools.Method(typeof(AddFactsFromCaster), "OnActivate").Invoke(runtime.SourceBlueprintComponent, null);
            Check(sharedGrants == prior + 1 && target.HasFact(power) && gate.IsCasterRestrictionPassed(target), "native song grants recipient Howl and valid rage gate");
            int removed = sharedRemovals; using (runtime.RequestEventContext()) AccessTools.Method(typeof(AddFactsFromCaster), "OnDeactivate").Invoke(runtime.SourceBlueprintComponent, null);
            Check(sharedRemovals == removed + 1 && !target.HasFact(power) && caster.HasFact(power), "native song removal clears only temporary recipient fact");
            foreach (var b in new[] { song, ownSong }) { b.Owner.Facts.List.Remove(b); AccessTools.Method(Get(b.Owner.Facts, "m_Cache").GetType(), "Clear").Invoke(Get(b.Owner.Facts, "m_Cache"), null); }
            Check(!gate.IsCasterRestrictionPassed(target), "recipient cannot howl once inspired rage ends");
        }
    }
}
