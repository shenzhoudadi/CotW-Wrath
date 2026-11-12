using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using HarmonyLib;

internal static class Program
{
    private static string managed;
    private static int checks;
    private static bool controlled = true;
    private static bool ControlBoundary(ref bool __result) { __result = controlled; return false; }
    private static void Check(bool ok, string why) { checks++; if (!ok) throw new InvalidOperationException(why); }
    private static MethodInfo Method(Type type, string name) => AccessTools.Method(type, name);
    private static Type Type(Assembly assembly, string name) => assembly.GetType(name, true);
    private static object Raw(Type type) => FormatterServices.GetUninitializedObject(type);
    private static void Set(object obj, string name, object value)
        => (AccessTools.Field(obj.GetType(), name) ?? AccessTools.Field(obj.GetType(), "<" + name + ">k__BackingField") ?? throw new Exception("No field " + name)).SetValue(obj, value);
    private static object NewUnit(Assembly game)
    {
        var unit = Raw(Type(game, "Kingmaker.EntitySystem.Entities.UnitEntityData"));
        var descriptor = Raw(Type(game, "Kingmaker.UnitLogic.UnitDescriptor"));
        Set(unit, "Descriptor", descriptor); Set(descriptor, "Unit", unit);
        Set(unit, "Parts", Activator.CreateInstance(Type(game, "Kingmaker.EntitySystem.EntityPartsManager"), new[] { unit }));
        Set(descriptor, "Proficiencies", Activator.CreateInstance(Type(game, "Kingmaker.UnitLogic.UnitProficiency")));
        return unit;
    }
    private static object NewWeapon(Assembly game, string category, bool ranged = false, bool twoHanded = false)
    {
        var weapon = Raw(Type(game, "Kingmaker.Items.ItemEntityWeapon"));
        var blueprint = Raw(Type(game, "Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon"));
        var weaponType = Raw(Type(game, "Kingmaker.Blueprints.Items.Weapons.BlueprintWeaponType"));
        Set(weaponType, "Category", Enum.Parse(Type(game, "Kingmaker.Enums.WeaponCategory"), category));
        Set(weaponType, "m_AttackType", Enum.Parse(Type(game, "Kingmaker.RuleSystem.AttackType"), ranged ? "Ranged" : "Melee"));
        Set(weaponType, "m_IsTwoHanded", twoHanded);
        var reference = Activator.CreateInstance(Type(game, "Kingmaker.Blueprints.BlueprintWeaponTypeReference"));
        AccessTools.Property(reference.GetType(), "Cached").SetValue(reference, weaponType, null);
        Set(blueprint, "m_Type", reference); Set(weapon, "m_Blueprint", blueprint);
        return weapon;
    }
    private static object NewBonusRule(Assembly game, object unit, object weapon)
    {
        var stats = Raw(Type(game, "Kingmaker.RuleSystem.Rules.RuleCalculateWeaponStats"));
        Set(stats, "Weapon", weapon); Set(stats, "Initiator", unit);
        var rule = Raw(Type(game, "Kingmaker.RuleSystem.Rules.RuleCalculateAttackBonusWithoutTarget"));
        Set(rule, "WeaponStats", stats); Set(rule, "Initiator", unit);
        return rule;
    }
    private static int Bonus(object rule) => (int)AccessTools.Property(rule.GetType(), "TotalBonusValue").GetValue(rule);
    private static void Main(string[] args)
    {
        managed = Path.GetFullPath(args[0]);
        var modulePath = Path.GetFullPath(args[1]);
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
        {
            string path = Path.Combine(managed, new AssemblyName(e.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        try { Execute(modulePath); }
        catch (Exception e)
        {
            int depth = 0;
            while (e != null && depth++ < 8) { Console.Error.WriteLine(e.ToString()); e = e.InnerException; }
            Environment.Exit(1);
        }
    }
    private static void Execute(string modulePath)
    {
        var harmony = typeof(Harmony).Assembly;
        Check(harmony.GetName().Version.ToString() == "2.3.6.0", "actual game Harmony version");
        string digest;
        using (var sha = SHA256.Create()) using (var stream = File.OpenRead(harmony.Location))
            digest = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        Check(digest == "DF043F05C6E43602FA8E8F38F52A3A2CF962AAB7A65A9D85573EA25F01A63E80", "actual game Harmony SHA");
        Check(Path.GetFullPath(harmony.Location).Equals(Path.Combine(managed, "UnityModManager", "0Harmony.dll"), StringComparison.OrdinalIgnoreCase), "no copied Harmony runtime");
        var game = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
        Check(!Enum.IsDefined(Type(game, "Kingmaker.RuleSystem.Rules.BonusType"), 160), "custom weapon penalty bonus ID has no native collision");
        Check(!Enum.IsDefined(Type(game, "Kingmaker.Enums.ModifierDescriptor"), 81), "custom armor penalty descriptor has no native collision");
        Check(!Enum.IsDefined(Type(game, "Kingmaker.Enums.ModifierDescriptor"), 82), "custom shield penalty descriptor has no native collision");
        var module = Assembly.LoadFrom(modulePath);
        var fightingType = Type(game,"Kingmaker.Designers.Mechanics.Facts.TwoWeaponFightingAttackPenalty");
        bool ambiguous=false;
        try { AccessTools.PropertyGetter(fightingType,"Fact"); } catch(AmbiguousMatchException) { ambiguous=true; }
        Check(ambiguous,"raw native delegate hierarchy reproduces old ambiguous Fact lookup");
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(Type(module,"Way_of_the_shield.ShieldRuntimePatches").TypeHandle);
        var exactFact=(MethodInfo)AccessTools.Field(Type(module,"Way_of_the_shield.ShieldRuntimePatches"),"FightingFact").GetValue(null);
        Check(exactFact.ReturnType==Type(game,"Kingmaker.UnitLogic.UnitFact") && exactFact.DeclaringType.GetGenericTypeDefinition().FullName=="Kingmaker.UnitLogic.UnitFactComponentDelegate`1","corrected raw game lookup resolves only typed Fact; patch initializer succeeds");
        var rawBuff=Raw(Type(game,"Kingmaker.UnitLogic.Buffs.Buff"));
        Method(Type(module,"Way_of_the_shield.NewComponents.BuffSetRanksOnApply"),"SetCounter").Invoke(null,new object[]{rawBuff,7});
        Check((int)AccessTools.DeclaredProperty(rawBuff.GetType(),"Rank").GetValue(rawBuff)==7,"charge setter executes against private raw game setter without publicized-member access");
        Console.WriteLine("Actual game module: " + modulePath);
        var declaredRuntimeTypes = (string[])AccessTools.Field(Type(module, "Way_of_the_shield.Main"), "RuntimePatchTypes").GetValue(null);
        foreach (string name in declaredRuntimeTypes) Check(module.GetType(name) != null, "root runtime whitelist type exists: " + name);
        var nativeLightness = Type(module, "Way_of_the_shield.Tweaks_and_Changes.TWFShieldTweak");
        Method(nativeLightness, "VerifyNativeLightness").Invoke(null, null);
        Check((bool)nativeLightness.GetProperty("NativeLightnessVerified").GetValue(null), "real native light-shield/heavy-shield IL audited");
        Method(nativeLightness, "VerifyNativeLightness").Invoke(null, null);
        Check((bool)nativeLightness.GetProperty("NativeLightnessVerified").GetValue(null), "native IL audit idempotent");

        var equip = Type(module, "Way_of_the_shield.ProficiencyRework.ProficiencyPatches+ItemEntity_EquipNonProfficient_Patch");
        var targets = (IEnumerable<MethodBase>)Method(equip, "GetMethods").Invoke(null, null);
        int targetCount = 0;
        foreach (var target in targets)
        {
            targetCount++;
            var original = PatchProcessor.GetOriginalInstructions(target, (ILGenerator)null);
            var changed = ((IEnumerable<CodeInstruction>)Method(equip, "Transpiler").Invoke(null, new object[] { original, target })).ToList();
            Check(changed.Count == original.Count, target.DeclaringType.Name + " preserves equip method structure");
            Check(changed.Count(c => c.operand is MethodInfo m && m.Name.StartsWith("Allow") && m.DeclaringType.Namespace.EndsWith("ProficiencyRework")) == 1, "only one native proficiency predicate replaced");
            Check(changed.Any(c => c.operand is MethodInfo m && m.Name == "CanBeEquippedInternal" && m.DeclaringType.Name == "ItemEntity"), "base equipment restrictions retained");
        }
        Check(targetCount == 3, "all three equipment proficiency types handled");

        var armor = Type(module, "Way_of_the_shield.ProficiencyRework.ProficiencyPatches+ItemEntityArmorPatch");
        var recalc = Method(Type(game, "Kingmaker.Items.ItemEntityArmor"), "RecalculateStats");
        var nativeArmor = PatchProcessor.GetOriginalInstructions(recalc, (ILGenerator)null);
        var armorChanged = ((IEnumerable<CodeInstruction>)Method(armor, "Transpiler").Invoke(null, new object[] { nativeArmor })).ToList();
        Check(armorChanged.Count == nativeArmor.Count + 3, "ACP adds only three instructions");
        int injection = armorChanged.FindIndex(c => c.operand is MethodInfo m && m.Name == "AddAttackBonusPenaltyFromArmor");
        Check(injection > 2 && armorChanged[injection - 2].opcode == OpCodes.Ldarg_0, "native armor owns the ACP modifier");
        Check(armorChanged[injection - 3].IsStloc(), "ACP uses the actual result local");
        Check(armorChanged.Any(c => c.operand is MethodInfo m && m.Name == "RecalculateMaxDexBonus"), "native armor max-Dex logic retained");

        var bash = Type(module, "Way_of_the_shield.Tweaks_and_Changes.ShieldBashAbilityTweak");
        var attached = AccessTools.Method(Type(game, "Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityCollection"), "OnFactDidAttach", new[] { Type(game, "Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbility") });
        var nativeAttach = PatchProcessor.GetOriginalInstructions(attached, (ILGenerator)null);
        var changedAttach = ((IEnumerable<CodeInstruction>)Method(bash, "ActivatableAbilityCollection_OnFactDidAttach_PatchToNotActivateOnAttach").Invoke(null, new object[] { nativeAttach })).ToList();
        Check(changedAttach.Count == nativeAttach.Count + 2, "default-on filtering preserves native attachment");
        Check(changedAttach.Count(c => c.operand is MethodInfo m && m.Name == "FilterDefaultActivation") == 1, "default-on filter inserted once");
        Check(!(bool)Method(bash, "FilterDefaultActivation").Invoke(null, new object[] { false, null }), "other off-by-default activatables stay off");

        var slots = Type(module, "Way_of_the_shield.ItemEntityWeapon__HoldInTwoHands__Patch");
        var contextField = AccessTools.Field(slots, "insertion");
        var contextType = contextField.FieldType;
        var one = Raw(Type(game, "Kingmaker.Items.Slots.HandSlot"));
        var two = Raw(Type(game, "Kingmaker.Items.Slots.HandSlot"));
        var inserted = Raw(Type(game, "Kingmaker.Items.ItemEntityWeapon"));
        var outer = new object[] { one, inserted, null };
        Method(slots, "BeginCanInsert").Invoke(null, outer);
        Check(ReferenceEquals(contextType.GetField("Slot").GetValue(contextField.GetValue(null)), one), "real insertion prefix records its slot");
        var inner = new object[] { two, null, null };
        Method(slots, "BeginCanInsert").Invoke(null, inner);
        Check(ReferenceEquals(contextType.GetField("Slot").GetValue(contextField.GetValue(null)), two), "nested insertion owns its own context");
        Method(slots, "EndCanInsert").Invoke(null, new[] { inner[2] });
        Check(ReferenceEquals(contextType.GetField("Slot").GetValue(contextField.GetValue(null)), one), "nested finalizer restores outer slot");
        Check(ReferenceEquals(contextType.GetField("Item").GetValue(contextField.GetValue(null)), inserted), "nested finalizer restores outer inserted item");
        Method(slots, "EndCanInsert").Invoke(null, new[] { outer[2] });
        Check(contextType.GetField("Slot").GetValue(contextField.GetValue(null)) == null, "finalizer clears insertion scope after failure or success");

        var ordinary = NewUnit(game);
        // The forced one-handed grip toggle was removed from Way of the Shield;
        // its retired IL/runtime assertions were deleted with that feature.
        var acRule = Raw(Type(game, "Kingmaker.RuleSystem.Rules.RuleCalculateAC"));
        Set(acRule, "Initiator", ordinary); Set(acRule, "Target", ordinary);
        var runtime = Type(module, "Way_of_the_shield.ShieldRuntimePatches");
        Method(runtime, "DenyEquippedShieldAC").Invoke(null, new[] { acRule });
        Check(Bonus(acRule) == 0, "ordinary real native unit AC receives no shield-denial penalty");
        var harmlessBonus = NewBonusRule(game, ordinary, null);
        Check((bool)Method(runtime, "ApplyReservedShieldHandPenalty").Invoke(null, new object[] { null, harmlessBonus }), "ordinary native unit retains native TWF calculation");
        Check(Bonus(harmlessBonus) == 0, "ordinary native unit gets no forced TWF penalty");

        var h = new Harmony("CotW.WayOfTheShield.EquipmentSmoke.Boundary");
        int nativeTargetCount = 0;
        try
        {
            foreach (string patch in new[]
            {
                "Way_of_the_shield.ItemEntityWeapon__HoldInTwoHands__Patch",
                "Way_of_the_shield.MechanicsFeatureExtension",
                "Way_of_the_shield.ProficiencyRework.ProficiencyPatches+FixTheArmorBug_ThisPatchDoesNotWantToLoad",
                "Way_of_the_shield.ProficiencyRework.ProficiencyPatches+ProficiencyFactsChanged",
                "Way_of_the_shield.ProficiencyRework.ProficiencyPatches+UIUtilityItemPatch",
                "Way_of_the_shield.ProficiencyRework.ProficiencyPatches+ItemEntity_EquipNonProfficient_Patch",
                "Way_of_the_shield.ProficiencyRework.ProficiencyPatches+ItemEntityArmorPatch",
                "Way_of_the_shield.ProficiencyRework.WeaponNoProficiency",
                "Way_of_the_shield.ShieldRuntimePatches"
            })
            {
                h.CreateClassProcessor(Type(module, patch)).Patch();
                Check(true, "actual Harmony 2.0.4 installs " + patch);
            }
            h.Patch(attached, transpiler: new HarmonyMethod(Method(bash, "ActivatableAbilityCollection_OnFactDidAttach_PatchToNotActivateOnAttach")));
            nativeTargetCount = Harmony.GetAllPatchedMethods().Count(m => Harmony.GetPatchInfo(m).Owners.Contains(h.Id));
            Check(nativeTargetCount == 22,
                "native runtime patch install resolves equipment targets including typed attachment overload");
            // Direct control depends on the Unity scene selection/faction services; this is the sole boundary.
            h.Patch(AccessTools.PropertyGetter(Type(game, "Kingmaker.EntitySystem.Entities.UnitEntityData"), "IsDirectlyControllable"), prefix: new HarmonyMethod(typeof(Program), nameof(ControlBoundary)));
            var weapon = NewWeapon(game, "Longspear");
            var noProf = Type(module, "Way_of_the_shield.ProficiencyRework.WeaponNoProficiency");
            var attack = NewBonusRule(game, ordinary, weapon);
            Method(noProf, "Prefix").Invoke(null, new[] { attack });
            Check(Bonus(attack) == -4, "actual native rule bonus receives non-proficient -4");
            Method(noProf, "Prefix").Invoke(null, new[] { attack });
            Check(Bonus(attack) == -4, "same native rule is not penalized twice");
            var descriptor = AccessTools.Property(ordinary.GetType(), "Descriptor").GetValue(ordinary);
            var proficiency = AccessTools.Field(descriptor.GetType(), "Proficiencies").GetValue(descriptor);
            var categoryType = Type(game, "Kingmaker.Enums.WeaponCategory");
            var longspear = Enum.Parse(categoryType, "Longspear");
            AccessTools.Method(proficiency.GetType(), "Add", new[] { categoryType }).Invoke(proficiency, new[] { longspear });
            attack = NewBonusRule(game, ordinary, weapon);
            Method(noProf, "Prefix").Invoke(null, new[] { attack });
            Check(Bonus(attack) == 0, "adding real native weapon proficiency clears future penalty");
            AccessTools.Method(proficiency.GetType(), "Remove", new[] { categoryType }).Invoke(proficiency, new[] { longspear });
            attack = NewBonusRule(game, ordinary, weapon);
            Method(noProf, "Prefix").Invoke(null, new[] { attack });
            Check(Bonus(attack) == -4, "removing real native proficiency restores future penalty");
            attack = NewBonusRule(game, ordinary, NewWeapon(game, "OtherNaturalWeapons"));
            Method(noProf, "Prefix").Invoke(null, new[] { attack });
            Check(Bonus(attack) == 0, "native natural weapon category is exempt");
            controlled = false; attack = NewBonusRule(game, ordinary, weapon);
            Method(noProf, "Prefix").Invoke(null, new[] { attack });
            Check(Bonus(attack) == 0, "uncontrolled ordinary native unit receives no nonproficiency rule rewrite");
        }
        finally { h.UnpatchAll(h.Id); controlled = true; }

        var penalty = Method(runtime, "CalculateForcedDualWieldingPenalty");
        Action<int, int, bool, bool, bool, bool> Expect = (expected, rank, primary, light, mythic, effortless) =>
            Check((int)penalty.Invoke(null, new object[] { rank, primary, light, mythic, effortless }) == expected,
                "TWF penalty " + expected + " rank=" + rank + " primary=" + primary + " light=" + light + " mythic=" + mythic + " effortless=" + effortless);
        Expect(-4, 1, true, true, false, false); Expect(-8, 1, false, true, false, false);
        Expect(-6, 1, true, false, false, false); Expect(-10, 1, false, false, false, false);
        Expect(-2, 2, true, true, false, false); Expect(-2, 2, false, true, false, false);
        Expect(-4, 2, true, false, false, false); Expect(-4, 2, false, false, false, false);
        Expect(0, 2, true, true, true, false); Expect(0, 2, false, true, true, false);
        Expect(-2, 2, true, false, true, false); Expect(-2, 2, false, false, true, false);
        Expect(-2, 2, true, false, false, true); Expect(0, 2, true, false, true, true);
        Check(!(bool)Method(runtime, "IsShieldDenied").Invoke(null, new object[] { null }), "no unit does not deny shield AC");
        Check((int)Method(runtime, "CalculateEquippedShieldBonus").Invoke(null, new object[] { null }) == 0, "no unit grants no shield AC");

        File.WriteAllText(Path.Combine(Path.GetDirectoryName(modulePath), "equipment-smoke-result.json"), "{\"checks\":" + checks + ",\"nativeRuntimeTargets\":" + nativeTargetCount + ",\"harmony\":\"2.3.6.0\",\"nativeIl\":true}");
        Console.WriteLine("PASS " + checks + " actual-game equipment IL and penalty checks");
    }
}
