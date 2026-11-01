using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class PurifyingFlames01428Tests
{
    static int checks, rawD20 = 1, burnEvents, dispelCalls, removeCalls;
    static bool failAppend;
    static RulebookEventContext current;
    static readonly List<RuleDispelMagic> dispels = new List<RuleDispelMagic>();
    static readonly List<Buff> removed = new List<Buff>();
    static Type Module => typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PurifyingFlames01428", true);
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static T Helper<T>(string n, params object[] a) => (T)AccessTools.Method(typeof(Migration01426Tests), n).Invoke(null, a);
    static BlueprintGuid Id(string token) => Helper<BlueprintGuid>("Id", token);
    static T Native<T>(BlueprintsCache cache, string field) where T : SimpleBlueprint => (T)cache.Load(BlueprintGuid.Parse((string)AccessTools.Field(Module, field).GetRawConstantValue()));
    static T Asset<T>(BlueprintsCache cache, string token) where T : SimpleBlueprint => (T)cache.Load(Id(token));
    static T Ref<T>(SimpleBlueprint b) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(b); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, b); return r; }
    static void Check(bool value, string reason) { if (!value) throw new Exception("PurifyingFlames01428: " + reason); checks++; }
    static bool Skip() => false;
    static bool No(ref bool __result) { __result = false; return false; }
    static bool Yes(ref bool __result) { __result = true; return false; }
    static bool NoController(ref LevelUpController __result) { __result = null; return false; }
    static bool PoolIgnore(BlueprintFeatureSelection __instance, ref bool __result) { __result = __instance.IgnorePrerequisites; return false; }
    static bool Context(ref RulebookEventContext __result) { if (current == null) return true; __result = current; return false; }
    static bool Dice(DiceFormula __0, ref int __result) { __result = __0.Dice == DiceType.D20 ? rawD20 : 0; return false; }
    static bool Roll(ref RuleRollD20 __result) { __result = NativeRule(new RuleRollD20(current.LastEvent<RulebookEvent>().Initiator)); return false; }
    static bool Expose(Exception __1) { throw new Exception("PurifyingFlames01428 native action error", __1); }
    static string Definition(object value) => (string)AccessTools.Method(typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426"), "Definition").Invoke(null, new[] { value });
    static void AppendFault(BlueprintFeature[] feats) { if (failAppend && feats.Any(f => f.AssetGuid == Id("PurifyingFlamesWildTalent"))) throw new InvalidOperationException("01428 purify append injection"); }
    static bool Reject(Action action) { try { action(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; if (e is InvalidOperationException && e.Message.Contains("01428 purify append injection")) return true; throw; } }
    static bool Raise<T>(Action<T> action, bool unused) { return true; }
    static bool RaiseUnit<T>(UnitEntityData unit, Action<T> action) { if (typeof(T) == typeof(IKineticistAcceptBurnHandler)) burnEvents++; return true; }
    static void BooklessSpend(AbilityData data) { Check(data.Spellbook == null, "wild talent has no spellbook slot to spend"); }
    static string Label(LocalizedString value) => value?.Key ?? "";
    static T NativeRule<T>(T rule) where T : RulebookEvent {
        var old = current; current = new RulebookEventContext(); current.PushEvent(rule);
        try { if (rule is RuleDispelMagic dispel) { dispelCalls++; dispels.Add(dispel); } rule.OnTrigger(current); return rule; }
        finally { current = old; }
    }
    static T ContextRule<T>(MechanicsContext context, T rule) where T : RulebookEvent { rule.Reason = context; return NativeRule(rule); }
    static IEnumerable<CodeInstruction> Boundary(IEnumerable<CodeInstruction> input) {
        foreach (var c in input) {
            if (c.operand is MethodInfo book && book.DeclaringType == typeof(AbilityData) && book.Name == "SpendFromSpellbook") {
                // Native SpendFromSpellbook contains a Unity call in its
                // unreachable book branch. Assert its actual early-return case.
                c.opcode = OpCodes.Call; c.operand = AccessTools.Method(typeof(PurifyingFlames01428Tests), nameof(BooklessSpend));
            }
            else if (c.operand is MethodInfo label && label.DeclaringType == typeof(LocalizedString) && label.Name == "op_Implicit") {
                c.opcode = OpCodes.Call; c.operand = AccessTools.Method(typeof(PurifyingFlames01428Tests), nameof(Label));
            }
            else if (c.operand is MethodInfo m && m.IsGenericMethod) {
                string target = m.DeclaringType == typeof(Rulebook) && m.Name == "Trigger" ? nameof(NativeRule)
                    : m.DeclaringType == typeof(MechanicsContext) && m.Name == "TriggerRule" ? nameof(ContextRule)
                    : m.DeclaringType == typeof(EventBus) && m.Name == "RaiseEvent" && m.GetParameters().Length == 2
                        ? (m.GetParameters()[0].ParameterType == typeof(UnitEntityData) ? nameof(RaiseUnit) : nameof(Raise)) : null;
                if (target != null) { c.opcode = OpCodes.Call; c.operand = AccessTools.Method(typeof(PurifyingFlames01428Tests), target).MakeGenericMethod(m.GetGenericArguments()); }
            }
            yield return c;
        }
    }
    // Native dispel decides whether/what to remove. Only scene fact removal is
    // bounded; this endpoint must never be called for failed or non-poison cases.
    static bool Remove(Buff __instance) {
        removeCalls++; removed.Add(__instance); __instance.Owner.Facts.List.Remove(__instance);
        var cache = Get(__instance.Owner.Facts, "m_Cache"); AccessTools.Method(cache.GetType(), "Clear").Invoke(cache, null);
        Set(__instance, "IsTurnedOn", false); Set(__instance, "IsActive", false); return false;
    }
    static BlueprintAbilityResource Resource(string token, int amount) {
        var resource = new BlueprintAbilityResource { name = token, AssetGuid = Id(token) };
        var max = Get(resource, "m_MaxAmount"); Set(max, "BaseValue", amount);
        Set(max, "m_Class", Array.Empty<BlueprintCharacterClassReference>()); Set(max, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>());
        Set(resource, "m_MaxAmount", max); return resource;
    }
    static UnitPartKineticist Kineticist(UnitEntityData unit, BlueprintCharacterClass cls, StatType stat, int total, int perRound) {
        var settings = new AddKineticistPart { MainStat = stat };
        var max = Resource(unit.UniqueId + "MaxBurn", total); var round = Resource(unit.UniqueId + "MaxBurnRound", perRound);
        Set(settings, "m_Class", Ref<BlueprintCharacterClassReference>(cls)); Set(settings, "m_MaxBurn", Ref<BlueprintAbilityResourceReference>(max)); Set(settings, "m_MaxBurnPerRound", Ref<BlueprintAbilityResourceReference>(round));
        unit.Descriptor.Resources.Add(max, true); unit.Descriptor.Resources.Add(round, true);
        var part = unit.Ensure<UnitPartKineticist>(); part.Setup(settings); return part;
    }
    static UnitEntityData Unit(BlueprintsCache cache, string name, BlueprintCharacterClass cls, int level) {
        var u = Helper<UnitEntityData>("Unit", cache, name); u.Progression.Classes.Clear(); u.Progression.Classes.Add(new ClassData(cls) { Level = level });
        u.Stats.Constitution.BaseValue = 18; u.Stats.Wisdom.BaseValue = 14; u.Stats.Charisma.BaseValue = 12; return u;
    }
    static Buff Buff(UnitEntityData target, BlueprintBuff blueprint, UnitEntityData caster, int dc, int level = 5) {
        // The common fixture does not hydrate display metadata; native dispel
        // rejects nameless buffs. Use the actual native key, including Shared.
        var row = PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == blueprint.AssetGuid.ToString());
        var display = row["blueprint"]["Data"]["m_DisplayName"]; string key = (string)display["m_Key"];
        if (string.IsNullOrEmpty(key)) key = (string)display["Shared"]?["stringkey"];
        Check(!string.IsNullOrEmpty(key), "native poison/control buff has an actual nonempty display key");
        Set(blueprint, "m_DisplayName", new LocalizedString { Key = key });
        var flags = AccessTools.Field(typeof(BlueprintBuff), "m_Flags"); flags.SetValue(blueprint, Enum.Parse(flags.FieldType, (string)row["blueprint"]["Data"]["m_Flags"]));
        var buff = Psychic034Tests.Fact(target, blueprint, caster, level); buff.Context.SetParams(new AbilityParams { CasterLevel = level, DC = dc });
        buff.Context.Recalculate(); buff.Context.SetParams(new AbilityParams { CasterLevel = level, DC = dc }); return buff;
    }
    static AbilityExecutionContext Cast(BlueprintAbility ability, UnitEntityData caster, UnitEntityData target) {
        var ctx = new AbilityExecutionContext(new AbilityData(ability, caster.Descriptor), new AbilityParams(), new TargetWrapper(target), null, caster);
        AbilityExecutionProcess.PrepareCast(ctx);
        Check(ctx.SpellSchool == SpellSchool.Conjuration, "native cast retains the donor Conjuration school");
        Check(ctx.Params.CasterLevel == caster.Progression.GetClassLevel(NativeClass), "native PrepareCast uses kineticist caster level");
        ability.GetComponent<AbilityEffectRunAction>().Apply(ctx, new TargetWrapper(target)); return ctx;
    }
    static BlueprintCharacterClass NativeClass;
    internal static int Run(BlueprintsCache cache) {
        checks = burnEvents = dispelCalls = removeCalls = 0; current = null; failAppend = false; dispels.Clear(); removed.Clear();
        var h = new Harmony("CotW01428.PurifyingFlamesNative");
        void Patch(MethodBase method, string name) { if (method == null) throw new Exception("Missing PurifyingFlames endpoint " + name); h.Patch(method, prefix: new HarmonyMethod(typeof(PurifyingFlames01428Tests), name) { priority = Priority.First }); }
        void Transpile(MethodBase method) { h.Patch(method, transpiler: new HarmonyMethod(typeof(PurifyingFlames01428Tests), nameof(Boundary))); }
        Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip));
        Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), nameof(No));
        Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), nameof(PoolIgnore));
        Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), nameof(NoController));
        Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), nameof(No));
        Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsInCombat"), nameof(Yes));
        Patch(AccessTools.PropertyGetter(typeof(Rulebook), "CurrentContext"), nameof(Context));
        Patch(AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }), nameof(Dice));
        Patch(AccessTools.PropertyGetter(typeof(RulebookEvent.Dice), "D20"), nameof(Roll));
        Patch(AccessTools.Method(typeof(Buff), "Remove", Type.EmptyTypes), nameof(Remove));
        Patch(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx), "ExceptionWithReport", new[] { typeof(Owlcat.Runtime.Core.Logging.LogChannel), typeof(Exception), typeof(string), typeof(object[]) }), nameof(Expose));
        foreach (var method in new[] {
            AccessTools.Method(typeof(ContextCalculateAbilityParams), "Calculate", new[] { typeof(MechanicsContext), typeof(BlueprintScriptableObject), typeof(UnitEntityData), typeof(AbilityData) }),
            AccessTools.Method(typeof(ContextActionDispelMagic), "TryDispelBuff"), AccessTools.Method(typeof(RuleDispelMagic), "OnTrigger"),
            AccessTools.Method(typeof(AbilityData), "Spend"),
            AccessTools.PropertyGetter(typeof(BlueprintBuff), "HiddenInInspector"),
            AccessTools.PropertyGetter(typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact), "Name"),
            AccessTools.Method(typeof(AbilityKineticist), "CalculateBurnCost", new[] { typeof(UnitDescriptor), typeof(BlueprintAbility) }), AccessTools.Method(typeof(AbilityKineticist), "Spend"),
            AccessTools.Method(typeof(UnitPartKineticist), "AcceptBurn"), AccessTools.Method(typeof(UnitPartKineticist), "ClearAcceptedBurn") }) Transpile(method);
        h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary"));
        h.Patch(AccessTools.Method(typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append"), postfix: new HarmonyMethod(typeof(PurifyingFlames01428Tests), nameof(AppendFault)));
        try {
            var pool = Native<BlueprintFeatureSelection>(cache, "PoolId"); NativeClass = Native<BlueprintCharacterClass>(cache, "ClassId");
            var prior = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().Where(id => cache.Load(id).name.StartsWith("CotWWrathPsychicPurifyingFlames")).ToHashSet();
            foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Set(pool, field, ((BlueprintFeatureReference[])Get(pool, field)).Where(r => !prior.Contains(r.Guid)).ToArray());
            foreach (var id in prior) cache.RemoveCachedBlueprint(id);
            var poolBefore = new[] { Get(pool, "m_Features"), Get(pool, "m_AllFeatures") };
            var initial = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().ToHashSet();
            var donor = Native<BlueprintAbility>(cache, "PoisonId");
            var donors = new BlueprintScriptableObject[] { donor, Native<BlueprintFeature>(cache, "FoxfireId"), Native<BlueprintAbility>(cache, "HealerId") };
            var donorBefore = donors.Select(d => Definition(d.ComponentsArray)).ToArray();
            Action register = () => AccessTools.Method(Module, "Register").Invoke(null, new object[] { cache });
            failAppend = true; Check(Reject(register), "injected post-append fault rejects transaction"); failAppend = false;
            Check(((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().All(initial.Contains), "registration fault rolls back all new module blueprints");
            Check(ReferenceEquals(poolBefore[0], Get(pool, "m_Features")) && ReferenceEquals(poolBefore[1], Get(pool, "m_AllFeatures")), "registration fault restores exact original pool arrays");
            register();
            var ability = Asset<BlueprintAbility>(cache, "PurifyingFlamesAbility"); var feature = Asset<BlueprintFeature>(cache, "PurifyingFlamesWildTalent");
            Check(ability.Range == AbilityRange.Touch && ability.Range == donor.Range && ability.ActionType == UnitCommand.CommandType.Standard && !ability.IsFullRoundAction && ability.Type == AbilityType.SpellLike, "standard touch spell-like behavior follows native poison donor");
            Check(ability.CanTargetSelf == donor.CanTargetSelf && ability.CanTargetFriends == donor.CanTargetFriends && ability.CanTargetEnemies == donor.CanTargetEnemies, "native target allegiance flags retained");
            Check(ability.GetComponents<SpellListComponent>().Count() == 0 && ability.AvailableMetamagic == 0, "wild talent is not newly registered as a spell or metamagic choice");
            var action = ability.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single() as ContextActionDispelMagic;
            Check(action != null && action.Descriptor.HasAnyFlag(SpellDescriptor.Poison) && action.Schools.Length == 0, "actual native neutralize-poison action retained");
            foreach (string field in new[] { "m_Features", "m_AllFeatures" }) {
                var now = (BlueprintFeatureReference[])Get(pool, field); var originalRefs = (BlueprintFeatureReference[])poolBefore[field == "m_Features" ? 0 : 1];
                Check(now.Count(x => x.Guid == feature.AssetGuid) == 1, "wild talent is present once in " + field);
                Check(now.Take(originalRefs.Length).SequenceEqual(originalRefs), "all original pool references and order retained " + field);
            }
            register();
            Check(ReferenceEquals(ability, Asset<BlueprintAbility>(cache, "PurifyingFlamesAbility")) && ReferenceEquals(feature, Asset<BlueprintFeature>(cache, "PurifyingFlamesWildTalent")) && pool.AllFeatures.Count(x => x == feature) == 1, "repeat module registration preserves blueprint identity and one choice");
            var fires = feature.GetComponent<PrerequisiteFeaturesFromList>(); var caster = Unit(cache, "01428-purifier", NativeClass, 7);
            var state = (LevelUpState)FormatterServices.GetUninitializedObject(typeof(LevelUpState)); Set(state, "m_Unit", caster); Set(state, "Selections", new List<FeatureSelectionState>()); Set(state, "NextCharacterLevel", 8); state.NextClassLevel = 8; state.SelectedClass = NativeClass; var pending = state.AddSelection(null, default(FeatureSource), pool, 8);
            Check(!pool.CanSelect(caster.Descriptor, state, pending, feature), "level7 no-fire character cannot select talent");
            caster.Progression.Classes[0].Level = 8; Check(!pool.CanSelect(caster.Descriptor, state, pending, feature), "level8 non-fire character cannot select talent");
            foreach (var fire in fires.Features) {
                var qualified = Unit(cache, "01428-fire-" + fire.AssetGuid, NativeClass, 7); Helper<Feature>("Fact", qualified, fire);
                Check(!feature.MeetsPrerequisites(null, qualified.Descriptor, null, false, false), "every native fire option still requires level8");
                qualified.Progression.Classes[0].Level = 8;
                Check(feature.MeetsPrerequisites(null, qualified.Descriptor, null, false, false), "every actual native fire focus meets fire prerequisite at level8");
            }
            Helper<Feature>("Fact", caster, fires.Features[0]); Check(pool.CanSelect(caster.Descriptor, state, pending, feature), "native pool CanSelect admits qualified fire kineticist8");
            Helper<Feature>("Fact", caster, feature); Check(!pool.CanSelect(caster.Descriptor, state, pending, feature), "native pool rejects duplicate owned talent");
            var cost = ability.GetComponent<AbilityKineticist>(); var data = new AbilityData(ability, caster.Descriptor);
            Check(ReferenceEquals(data.ResourceLogic, cost) && ability.Restrictions.Contains(cost) && data.ActionType == UnitCommand.CommandType.Standard && !data.RequireMaterialComponent, "native AbilityData resolves the registered burn restriction/resource and standard action");
            Check(!cost.IsAbilityRestrictionPassed(data), "no kineticist part is rejected by native burn gate");
            var part = Kineticist(caster, NativeClass, StatType.Constitution, 3, 1);
            Check(cost.CalculateCost(data) == 1 && cost.IsAbilityRestrictionPassed(data), "native wild-talent burn cost and availability are one");
            var other = Unit(cache, "01428-poison-recipient", NativeClass, 8); var otherPart = Kineticist(other, NativeClass, StatType.Constitution, 3, 1);
            data.Spend(); Check(part.AcceptedBurn == 1 && part.AcceptedBurnThisRound == 1 && otherPart.AcceptedBurn == 0 && burnEvents == 1, "native spend accepts one burn on caster only and raises accept event");
            Check(!cost.IsAbilityRestrictionPassed(data), "native per-round burn exhaustion prevents repeat cast");
            part.ClearAcceptedBurnThisRound(); Check(cost.IsAbilityRestrictionPassed(data), "new-round native counter permits next burn"); data.Spend();
            part.ClearAcceptedBurnThisRound(); data.Spend(); part.ClearAcceptedBurnThisRound(); Check(part.AcceptedBurn == 3 && !cost.IsAbilityRestrictionPassed(data), "native total burn limit prevents extra cast");
            part.ClearAcceptedBurn(); Check(cost.IsAbilityRestrictionPassed(data), "native burn recovery reopens ability");
            foreach (var stat in new[] { StatType.Constitution, StatType.Wisdom, StatType.Charisma }) {
                var who = Unit(cache, "01428-mainstat-" + stat, NativeClass, 12); Kineticist(who, NativeClass, stat, 3, 1);
                var ctx = new AbilityExecutionContext(new AbilityData(ability, who.Descriptor), new AbilityParams(), new TargetWrapper(who), null, who); AbilityExecutionProcess.PrepareCast(ctx);
                Check(ctx.Params.CasterLevel == 12 && ctx.Params.SpellLevel == 6 && ctx.Params.DC == 16 + who.Stats.GetStat<ModifiableValueAttributeStat>(stat).Bonus, "native parameters use kineticist12 and actual configured main stat " + stat);
            }
            var poison = (BlueprintBuff)cache.Load(BlueprintGuid.Parse("ba1ae42c58e228c4da28328ea6b4ae34"));
            var shaken = (BlueprintBuff)cache.Load(BlueprintGuid.Parse("25ec6cb6ab1845c48a95f9c20b034220"));
            var prismaticPoison = (BlueprintBuff)cache.Load(BlueprintGuid.Parse("71fe5a730db173243bda4901bc074780"));
            var bad = Buff(other, poison, other, 20); var unrelated = Buff(other, shaken, other, 1); var harder = Buff(other, prismaticPoison, other, 30);
            rawD20 = 1; int before = dispelCalls; Cast(ability, caster, other);
            Check(dispelCalls == before + 2 && dispels.Skip(before).All(r => !r.Success && r.CasterLevel == 8 && r.CheckRoll.Result == 1) && removeCalls == 0, "actual native dispel rolls fail against both poison DCs; non-poison gets no roll");
            rawD20 = 20; before = dispelCalls; Cast(ability, caster, other);
            Check(dispelCalls == before + 2 && removed.Contains(bad) && !removed.Contains(harder) && !removed.Contains(unrelated), "native roll20+CL8 removes DC20 poison, retains DC30 poison and unrelated buff");
            Check(other.Buffs.Enumerable.Contains(harder) && other.Buffs.Enumerable.Contains(unrelated) && !other.Buffs.Enumerable.Contains(bad), "actual removal endpoint updates only successfully neutralized poison fact");
            caster.Progression.Classes[0].Level = 20; before = dispelCalls; Cast(ability, caster, other);
            Check(dispelCalls == before + 1 && removed.Contains(harder) && other.Buffs.Enumerable.Contains(unrelated), "kineticist20 caster-level increase beats remaining DC30 poison");
            before = dispelCalls; Cast(ability, caster, other); Check(dispelCalls == before && other.Buffs.Enumerable.Contains(unrelated), "poison-free target does not dispel unrelated conditions");
            Check(donorBefore.SequenceEqual(donors.Select(d => Definition(d.ComponentsArray))), "native poison, foxfire and kinetic healer donor components unchanged");
            var added = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().Where(id => !initial.Contains(id)).Select(cache.Load).Where(b => b.name.StartsWith("CotWWrathPsychicPurifyingFlames")).ToArray();
            PsychicExpansionTests.Inventory["purifyingFlames01428Native"] = new JObject {
                ["passed"] = true, ["assertions"] = checks, ["nativeDispelRules"] = dispelCalls, ["nativeRemovalEndpoints"] = removeCalls, ["nativeBurnSpendEvents"] = burnEvents,
                ["additionalItemCount"] = 1, ["newBlueprints"] = new JArray(added.Select(b => new JObject { ["id"] = b.AssetGuid.ToString(), ["name"] = b.name, ["type"] = b.GetType().FullName })),
                ["registrationRollback"] = true, ["originalPoolReferencesPreserved"] = true, ["repeatRegistrationIdentityPreserved"] = true, ["nativeDonorsUnchanged"] = true,
                ["nativeSelectionAndPrerequisites"] = true, ["nativeBurnCostGateSpend"] = true, ["nativePrepareCastParameters"] = true, ["nativeNeutralizePoisonAction"] = true,
                ["boundary"] = "Actual CanSelect/prerequisites, AbilityData.Spend dispatcher and AbilityKineticist cost/restriction/spend, UnitPartKineticist counters, PrepareCast, ContextActionDispelMagic and RuleDispelMagic.OnTrigger run. Unit/fact/resource setup, verified no-spellbook SpendFromSpellbook early return (unreachable Unity branch), scene EventBus, raw d20, difficulty presets and final Buff.Remove attachment are fixture boundaries; game cursor, touch animation and native UI feedback remain game regression cases." };
            Console.WriteLine("PASS: " + checks + " PurifyingFlames01428 native choice/burn/poison checks."); return checks;
        } finally { h.UnpatchAll(h.Id); current = null; failAppend = false; }
    }
}
