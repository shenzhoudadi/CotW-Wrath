using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;

internal static class PsychicPresentationTests
{
    private static int count;
    private static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static object Get(object obj, string field) => AccessTools.Field(obj.GetType(), field).GetValue(obj);
    private static void Set(object obj, string field, object value) => AccessTools.Field(obj.GetType(), field).SetValue(obj, value);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); count++; }
    internal static int Run(Assembly assembly, BlueprintsCache cache)
    {
        var all = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().Select(cache.Load).ToArray();
        var owned = all.OfType<BlueprintScriptableObject>().Where(b => b.name != null && (b.name.StartsWith("CotWWrathPsychic") ||
            b.AssetGuid.ToString() == "e66116541ac4546c9084a7bcfa47e5a6" || b.AssetGuid.ToString() == "0b1c8fa9fda652ccbf0a5b89f2cc6d25")).ToArray();
        var strings = (IDictionary)AccessTools.Field(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets",true),"Strings").GetValue(null);
        var tooltipType = typeof(BlueprintAbility).Assembly.GetType("Kingmaker.UI.MVVM._VM.Tooltip.Templates.TooltipTemplateAbility",true);
        var tooltip = FormatterServices.GetUninitializedObject(tooltipType);
        var addBlock = AccessTools.Method(tooltipType,"AddTitleDescriptionBlock");
        foreach (var fact in owned.OfType<Kingmaker.Blueprints.Facts.BlueprintUnitFact>())
            Check(!ReferenceEquals(Get(fact,"m_Icon"),null), "Icon propagated from native donor: " + fact.name);
        foreach (var ability in owned.OfType<BlueprintAbility>()) {
            Check(!string.IsNullOrEmpty(ability.LocalizedDuration?.Key), "Duration label provided: " + ability.name);
            Check(!string.IsNullOrEmpty(ability.LocalizedSavingThrow?.Key), "Save label provided: " + ability.name);
            string save = (string)strings[ability.LocalizedSavingThrow.Key];
            bool fortSave=(ability.name.Contains("OrbOfTheVoid")&&!ability.name.EndsWith("Move"))||ability.name.Contains("TemporalStasis")||ability.name.Contains("Suffocation")||ability.name.Contains("Screech") || ability.name.Contains("RiverOfWind") || ability.name.Contains("WallOfNausea") || ability.name.Contains("WallOfBlindness") || ability.name.Contains("ForcefulStrike") || ability.name.Contains("Rebuke") || ability.name.Contains("SheetLightning") || ability.name.Contains("HowlingAgony") || ability.name.Contains("PainStrike") || ability.name.Contains("SynapseOverload") || ability.name.Contains("BurstOfForce") || ability.name.Contains("TelekineticStorm");
            bool reflexSave=(!ability.name.EndsWith("Move")&&(ability.name.Contains("FlamingSphere")||ability.name.Contains("AggressiveThundercloud")||ability.name.Contains("BallLightning"))) || ability.name.Contains("EarthTremor") || ability.name.Contains("BlisteringInvective") || ability.name.Contains("FlurryOfSnowballs")||ability.name.Contains("Flashfire");
            bool hasSave = ability.name.Contains("Sanctuary") || ability.name.Contains("HexVulnerability") || ability.name.Contains("Babble") || ability.name.Contains("CurseMajor") ||  ability.name.Contains("AuraOfDoom") || ability.name.Contains("LitanyOfTruth") ||  ability.name.Contains("DazzlingBladeRelease") || reflexSave || ability.name.Contains("TouchOfBloodletting") ||  ability.name.Contains("GhostbaneDirge") || ability.name.Contains("IrresistibleDance") || fortSave || ability.name.Contains("DazeMass") || ability.name.Contains("HaltUndead") || ability.name.Contains("ControlUndead") || ability.name.Contains("Synesthesia") || ability.name.Contains("PsychicCrush") || ability.name.Contains("MindThrust") || ability.name.Contains("SynapticPulse") || ability.name.Contains("FaithCure") || ability.name.Contains("FaithInflict");
            Check(hasSave ? !string.IsNullOrEmpty(save) && save.StartsWith(fortSave?"Fortitude":reflexSave?"Reflex":"Will") : save == "", "Saving throw label is empty only for abilities without a save: " + ability.name);
            var bricks = (IList)Activator.CreateInstance(addBlock.GetParameters()[0].ParameterType);
            addBlock.Invoke(tooltip,new object[] {bricks,"Saving throw",save});
            Check(bricks.Count == (hasSave ? 3 : 0), "Native tooltip builder omits the entire empty saving throw row: " + ability.name);
            if ((ability.Type == AbilityType.Spell && !ability.name.Contains("BurstOf")) || ability.name.Contains("Senses"))
                Check(ability.GetComponent<AbilityEffectRunAction>()?.Actions.Actions.OfType<CallOfTheWild.Wrath.Prototype.PsychicOrbPlace>().Any(x=>!string.IsNullOrEmpty(x.Area.Get().Fx?.AssetId))==true || ability.GetComponent<AbilityEffectRunAction>()?.Actions.Actions.OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnAreaEffect>().Any(x=>!string.IsNullOrEmpty(x.AreaEffect.Fx?.AssetId))==true || (ability.GetComponent<AbilityDeliverProjectile>()?.Projectiles.Any()==true) || (ability.GetComponent<AbilityVariants>()?.Variants.Any()==true && ability.GetComponent<AbilityVariants>().Variants.All(v=>v.GetComponent<AbilitySpawnFx>()?.PrefabLink!=null||v.GetComponent<AbilityDeliverProjectile>()?.Projectiles.Any()==true)) || ability.GetComponent<AbilitySpawnFx>()?.PrefabLink != null || ability.GetComponent<AbilityEffectStickyTouch>()?.TouchDeliveryAbility.GetComponent<AbilitySpawnFx>()?.PrefabLink!=null, "Spell/senses cast or native touch delivery FX link: " + ability.name);
        }
        var h = new Harmony("CotWPsychic.NativeRemovalAndPulseFixture");
        // Unity objects/rendering are replaced. Native Buff.OnRemove and empty
        // PrefabLink.Load run. Pulse checks its native condition predicate; the
        // full spatial selector cannot JIT outside Unity and is also audited in IL.
        h.Patch(AccessTools.Method(typeof(UnityEngine.Object), "op_Implicit"), prefix: Method(nameof(ObjectExists)));
        h.Patch(AccessTools.PropertyGetter(typeof(Buff), "IsEnabled"), prefix: Method(nameof(Enabled)));
        h.Patch(AccessTools.Method(typeof(Buff), "OnRemove"), transpiler: Method(nameof(RemovalFixture)));
        try {
            foreach (var bp in owned.OfType<BlueprintBuff>()) {
                Check(bp.FxOnStart != null && bp.FxOnRemove != null, "Buff lifecycle links initialized: " + bp.name);
                var buff = MakeBuff(bp); buff.OnRemove();
                Check(Get(buff, "m_ParticleEffectOwner") == null, "Native removal reaches completion: " + bp.name);
            }
            bool oldFailed = false;
            try { MakeBuff(new BlueprintBuff()).OnRemove(); } catch (NullReferenceException) { oldFailed = true; }
            Check(oldFailed, "Old null removal link reproduces native OnRemove failure with a particle owner");
            foreach (var ability in owned.OfType<BlueprintAbility>().Where(a => a.name.Contains("SynapticPulse"))) {
                var area = ability.GetComponent<AbilityTargetsAround>();
                Check(ability.CanTargetFriends && ability.CanTargetEnemies && ability.GetComponent<AbilityEffectRunAction>().IgnoreCaster,
                    "Pulse permits both sides and excludes caster at effect execution");
                Check(!((Kingmaker.ElementsSystem.ConditionsChecker)Get(area, "m_Condition")).HasConditions,
                    "Pulse native condition predicate permits every spatial candidate");
                var condition = Get(area, "m_Condition"); Set(area, "m_Condition", null);
                bool failed = false;
                try { bool unused = ((Kingmaker.ElementsSystem.ConditionsChecker)Get(area, "m_Condition")).HasConditions; }
                catch (NullReferenceException) { failed = true; }
                finally { Set(area, "m_Condition", condition); }
                Check(failed, "Old null condition fails at the native HasConditions call audited in selector IL");
            }
        } finally { h.UnpatchAll(h.Id); }
        return count;
    }
    private static Buff MakeBuff(BlueprintBuff bp) {
        var buff = Empty<Buff>(); Set(buff, "<Blueprint>k__BackingField", bp);
        Set(buff, "Components", new List<EntityFactComponent>());
        Set(buff, "m_ParticleEffectOwner", Empty<Kingmaker.View.UnitEntityView>()); return buff;
    }
    private static HarmonyMethod Method(string name) => new HarmonyMethod(AccessTools.Method(typeof(PsychicPresentationTests), name));
    private static bool ObjectExists(UnityEngine.Object __0, ref bool __result) { __result = !ReferenceEquals(__0, null); return false; }
    private static bool Enabled(ref bool __result) { __result = true; return false; }
    private static IEnumerable<CodeInstruction> RemovalFixture(IEnumerable<CodeInstruction> instructions) {
        foreach (var code in instructions) {
            if (code.operand is MethodInfo method && method.DeclaringType == typeof(Kingmaker.Visual.Particles.FxHelper) && method.Name == "SpawnFxOnUnit")
                code.operand = AccessTools.Method(typeof(PsychicPresentationTests), nameof(NoFx));
            yield return code;
        }
    }
    private static Kingmaker.Visual.Particles.FxSpawnSystem.IFxHandle NoFx(UnityEngine.GameObject prefab, Kingmaker.View.UnitEntityView unit,
        bool? player, string bone, UnityEngine.Vector3 offset, Kingmaker.Visual.Particles.FxSpawnSystem.FxPriority priority) => null;
}
