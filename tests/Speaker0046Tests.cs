using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using CallOfTheWild.Wrath.Prototype;

// COTW-0046 B-fix targeted verification, contract C01-SPEAKER-0046-20261108-r1.
// Tests Time Mystery + Ancestors Mystery completion, Speaker automatic discovery
// & reuse, atomic TTT-Base gating/retry, and all D-review positive + negative
// real Rulebook/Spellbook behaviors in the isolated PsychicTests host.
internal static partial class Arcane01446Tests
{
    const string Sp046ClassId = "145f1d3d360a7ad48bd95d392c81b38e";
    const string Sp046OracleClassId = "20ce9bf8af32bee4c8557a045ab499b1";
    const string Sp046FighterClassId = "48ac8db94d5de7645906c7d0ad3bcfbd";
    const string Sp046ShamanSpellbookId = "44f16931dabdff643bfe2a48138e769f";
    const string Sp046ShamanListId = "c0c40e42f07ff104fa85492da464ac69";
    const string Sp046OracleMysteryId = "9936554e9372dab4a874c1dd165fb6f8";
    const string Sp046OracleMysterySelId = "5531b975dcdf0e24c98f1ff7e017e741";
    const string Sp046OracleRevSelId = "60008a10ad7ad6543b1f63016741a5d2";
    const string Sp046SecondMysteryId = "277b0164740b97945a3f8022bd572f48";
    const string Sp046OracleMissBuffId = "cef9569005de6234bb8b69e45f9509d8";
    const string Sp046SandsId = "da49f07e431b47abaad41ef28df487f7";
    const string Sp046TransformBuffId = "287682389d2011b41b5a65195d9cbc84";
    const string Sp046Root = "SpeakerForThePastArchetype0046";
    const string Sp046Mysteries = "MysteriesOfThePastFeature0046";
    const string Sp046Selection = "RevelationPastSelection0046";
    const string Sp046SpellList = "MysteriesOfThePastSpellList0046";

    static readonly Queue<int> Sp046Dice = new Queue<int>();
    static bool Sp046DiceOverrideActive;
    static int Sp046DiceRollsSeen;
    static bool Sp046StopTrigger;

    static readonly string[] Sp046Names = {
        "Aging Touch", "Rewind Time", "Blood of Heroes", "Phantom Touch", "Sacred Council",
        "Spirit of the Warrior", "Speed or Slow Time", "Spirit Shield", "Storm of Souls",
        "Spirit Walk", "Temporal Celerity", "Time Flicker", "Time Hop", "Time Sight",
        "Erase from Time", "Ancestral Weapon"
    };

    static string Sp046DisplayName(BlueprintUnitFact fact)
        => (string)Call(T("PsychicAssets"), "PeekText", fact, false);
    static object Sp046Field(object value, string field) => value == null ? null : AccessTools.Field(value.GetType(), field)?.GetValue(value);
    static object Sp046Member(object value, string name)
    {
        if (value == null) return null;
        var type = value.GetType();
        var f = AccessTools.Field(type, name);
        if (f != null) return f.GetValue(value);
        var p = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return p?.GetValue(value, null);
    }
    static BlueprintGuid Sp046RefGuid(object reference) => Sp046Member(reference, "Guid") is BlueprintGuid g ? g : default(BlueprintGuid);
    static void Sp046TrySet(object value, string field, object data)
    {
        var f = AccessTools.Field(value.GetType(), field);
        if (f != null) f.SetValue(value, data);
    }
    static void Sp046SetEnum(object value, string field, string enumName)
    {
        var f = AccessTools.Field(value.GetType(), field);
        if (f != null && f.FieldType.IsEnum) f.SetValue(value, Enum.Parse(f.FieldType, enumName));
    }
    static TRef Sp046Ref<TRef>(SimpleBlueprint bp) where TRef : BlueprintReferenceBase, new()
    {
        var r = BlueprintReferenceBase.CreateTyped<TRef>(bp);
        AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, bp, null);
        return r;
    }
    static ActionList Sp046Actions(params GameAction[] list) => new ActionList { Actions = list };
    static List<BlueprintComponent> Sp046Components(IEnumerable<SimpleBlueprint> assets)
    {
        var result = new List<BlueprintComponent>();
        foreach (var bp in assets.OfType<BlueprintScriptableObject>())
            result.AddRange((bp.ComponentsArray ?? Array.Empty<BlueprintComponent>()).Where(x => x != null));
        return result;
    }

    static bool Sp046DicePrefix(DiceFormula __0, ref int __result)
    {
        if (__0.Dice == DiceType.One) { __result = __0.Rolls; return false; }
        if (!Sp046DiceOverrideActive) { __result = __0.Dice == DiceType.D20 ? 10 : __0.Rolls; return false; }
        if (__0.Dice != DiceType.D20) { __result = __0.Rolls; return false; }
        Sp046DiceRollsSeen++;
        if (Sp046Dice.Count == 0)
            throw new InvalidOperationException("0046 unexpected extra D20 roll beyond expected queue");
        __result = Sp046Dice.Dequeue();
        return false;
    }

    static bool Sp046StopRulePrefix() => !Sp046StopTrigger;

    static string Sp046Signature(BlueprintScriptableObject bp)
    {
        if (bp == null) return "null";
        var parts = (bp.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            .Select(x => x == null ? "null" : x.GetType().FullName + ":" + x.name);
        return bp.name + "/" + bp.AssetGuid + "/" + string.Join(",", parts);
    }

    static string Sp046OracleAncestorsSnapshot(BlueprintsCache c)
    {
        return string.Join("|", new[] {
            Sp046OracleMysteryId,
            "1b9eebdfa2ad9d44eb60446d465b0cfd",
            "ab88d55181c294a40aea8745f1fd6419",
            "a8ae26103124a1c409279392b8f56238",
            "b63f198db73edbc4b84d863cb681f5bc",
            "0a790e530dc3c4942a3ff4138fb1d73f",
            Sp046OracleMissBuffId
        }.Select(id => Sp046Signature(c.Load(BlueprintGuid.Parse(id)) as BlueprintScriptableObject)));
    }

    static void Sp046InjectSpell(BlueprintsCache c, string guid, string name, SpellDescriptor descriptor = SpellDescriptor.None)
    {
        var id = BlueprintGuid.Parse(guid);
        if (c.Load(id) != null) return;
        var spell = new BlueprintAbility { AssetGuid = id, name = name, Type = AbilityType.Spell };
        var comps = new List<BlueprintComponent> {
            new AbilityEffectRunAction { name = "Speaker0046SyntheticEffect" + guid.Substring(0, 6) }
        };
        if (descriptor != SpellDescriptor.None)
            comps.Add(new SpellDescriptorComponent { name = "Speaker0046Desc" + guid.Substring(0, 6), Descriptor = descriptor });
        spell.ComponentsArray = comps.ToArray();
        c.AddCachedBlueprint(id, spell);
    }

    static void Sp046InjectSandsProvider(BlueprintsCache c)
    {
        var id = BlueprintGuid.Parse(Sp046SandsId);
        if (c.Load(id) != null) return;
        var spell = new BlueprintAbility { AssetGuid = id, name = "SandsOfTimeCast", Type = AbilityType.Spell };
        var list = new BlueprintSpellList { AssetGuid = BlueprintGuid.Parse("0046a1b2c3d4e5f600000000000000ff") };
        var row = new SpellLevelList(3);
        Sp046TrySet(row, "m_Spells", new List<BlueprintAbilityReference> { Sp046Ref<BlueprintAbilityReference>(spell) });
        list.SpellsByLevel = new[] { row };
        var listComponent = new SpellListComponent { name = "Speaker0046ProviderList" };
        Sp046TrySet(listComponent, "m_SpellList", Sp046Ref<BlueprintSpellListReference>(list));
        Sp046TrySet(listComponent, "SpellList", list);
        Sp046TrySet(listComponent, "SpellLevel", 3);
        Sp046TrySet(listComponent, "m_SpellLevel", 3);
        var school = new SpellComponent { name = "Speaker0046ProviderSchool", School = SpellSchool.Necromancy };
        spell.ComponentsArray = new BlueprintComponent[] { school, new AbilityEffectRunAction { name = "Speaker0046ProviderEffect" }, listComponent };
        c.AddCachedBlueprint(list.AssetGuid, list);
        c.AddCachedBlueprint(id, spell);
    }

    // Hydrates the isolated psychic host fixture with missing native spells, donors,
    // and the 7 vanilla Oracle Ancestors revelations matching blueprints.zip schemas.
    static void Sp046HydrateIsolatedHost(BlueprintsCache c, Type speakerModule, bool includeSands)
    {
        if (includeSands) Sp046InjectSandsProvider(c);
        Sp046InjectSpell(c, "585f7776285d17b60b9e8e2fe42a69be", "TimeStopSpell01438");
        Sp046InjectSpell(c, "503c3613f77e27d0a2650a09001b511f", "ThreefoldAspect");
        Sp046InjectSpell(c, "b39dd2d96af348236b0117f57e5e445d", "Contingency");
        Sp046InjectSpell(c, "8b0761f3873aefb976c0f47fec3c600c", "TemporalStasis");

        if (c.Load(BlueprintGuid.Parse(Sp046OracleMissBuffId)) == null)
            c.AddCachedBlueprint(BlueprintGuid.Parse(Sp046OracleMissBuffId), new BlueprintBuff {
                AssetGuid = BlueprintGuid.Parse(Sp046OracleMissBuffId),
                name = "OracleRevelationSpiritShieldMissBuff",
                ComponentsArray = Array.Empty<BlueprintComponent>()
            });

        var donors = (Array)AccessTools.Field(speakerModule, "ExtraNativeIds").GetValue(null);
        foreach (var donor in donors)
        {
            var guid = (string)Sp046Field(donor, "Item1");
            var expected = (Type)Sp046Field(donor, "Item2");
            var id = BlueprintGuid.Parse(guid);
            if (c.Load(id) != null) continue;
            var bp = (SimpleBlueprint)Activator.CreateInstance(expected);
            bp.AssetGuid = id;
            bp.name = "Speaker0046NativeDonor" + guid.Substring(0, 8);
            if (bp is BlueprintScriptableObject so) so.ComponentsArray = Array.Empty<BlueprintComponent>();
            c.AddCachedBlueprint(id, bp);
        }

        // Hydrate 7 native Oracle Ancestors revelations from blueprints.zip schemas
        // when the compact host JSON omits them.
        var oracle = Native<BlueprintCharacterClass>(c, Sp046OracleClassId);
        var ancestors = Native<BlueprintFeature>(c, Sp046OracleMysteryId);
        var revSel = Native<BlueprintFeatureSelection>(c, Sp046OracleRevSelId);
        var boneMystery = c.Load(BlueprintGuid.Parse("067b175d3df0d1a408efd7eee2b36b9b")) as BlueprintFeature ?? ancestors;
        var keen = Native<BlueprintWeaponEnchantment>(c, "102a9c8c9b7a75e4fb5844e79deaf4c0");
        var incorporeal = Native<BlueprintFeature>(c, "c4a7f98d743bc784c9d4cf2105852c39");
        var touchWeapon = Native<BlueprintItemWeapon>(c, "bb337517547de1a4189518d404ec49d4");
        var shaken = Native<BlueprintBuff>(c, "25ec6cb6ab1845c48a95f9c20b034220");

        PrerequisiteFeaturesFromList AncPrereq(string idPrefix, params BlueprintFeature[] features)
        {
            var p = new PrerequisiteFeaturesFromList { name = idPrefix + "Prereq", Amount = 1, Group = Prerequisite.GroupType.All };
            Sp046TrySet(p, "m_Features", features.Select(Sp046Ref<BlueprintFeatureReference>).ToArray());
            return p;
        }
        PrerequisiteClassLevel OracleMin(int lv)
        {
            var p = new PrerequisiteClassLevel { name = "OracleMin" + lv, Level = lv, Group = Prerequisite.GroupType.All };
            Sp046TrySet(p, "m_CharacterClass", Sp046Ref<BlueprintCharacterClassReference>(oracle));
            return p;
        }
        ContextRankConfig OracleRank(string prog = "AsIs", int start = 0, int step = 0, bool useMin = false, int min = 0)
        {
            var r = new ContextRankConfig { name = "NativeOracleRank" + prog };
            Sp046SetEnum(r, "m_Type", "Default");
            Sp046SetEnum(r, "m_BaseValueType", "SummClassLevelWithArchetype");
            Sp046SetEnum(r, "m_Progression", prog);
            Sp046TrySet(r, "m_Class", new[] { Sp046Ref<BlueprintCharacterClassReference>(oracle) });
            Sp046TrySet(r, "m_StartLevel", start);
            Sp046TrySet(r, "m_StepLevel", step);
            Sp046TrySet(r, "m_UseMin", useMin);
            Sp046TrySet(r, "m_Min", min);
            return r;
        }
        BlueprintAbilityResource OracleRes(string guid, int baseVal, bool byStat = false, bool byLevel = false, int start = 0, int step = 0)
        {
            var id = BlueprintGuid.Parse(guid);
            if (c.Load(id) is BlueprintAbilityResource ex) return ex;
            var res = new BlueprintAbilityResource { AssetGuid = id, name = "NativeRes" + guid.Substring(0, 6) };
            var amt = Sp046Field(res, "m_MaxAmount");
            Sp046TrySet(amt, "BaseValue", baseVal);
            Sp046TrySet(amt, "m_Class", new[] { Sp046Ref<BlueprintCharacterClassReference>(oracle) });
            Sp046TrySet(amt, "m_ClassDiv", new[] { Sp046Ref<BlueprintCharacterClassReference>(oracle) });
            if (byStat) { Sp046TrySet(amt, "IncreasedByStat", true); Sp046TrySet(amt, "ResourceBonusStat", StatType.Charisma); }
            if (byLevel) { Sp046TrySet(amt, "IncreasedByLevel", true); Sp046TrySet(amt, "LevelIncrease", 1); }
            if (step > 0)
            {
                Sp046TrySet(amt, "IncreasedByLevelStartPlusDivStep", true);
                Sp046TrySet(amt, "StartingLevel", start);
                Sp046TrySet(amt, "StartingIncrease", 1);
                Sp046TrySet(amt, "LevelStep", step);
                Sp046TrySet(amt, "PerStepIncrease", 1);
            }
            Sp046TrySet(res, "m_MaxAmount", amt);
            c.AddCachedBlueprint(id, res);
            return res;
        }
        BlueprintFeature AddRev(string featId, string name, BlueprintComponent[] comps)
        {
            var id = BlueprintGuid.Parse(featId);
            if (c.Load(id) is BlueprintFeature ex) return ex;
            var f = new BlueprintFeature {
                AssetGuid = id, name = name,
                Groups = new[] { FeatureGroup.OracleRevelation },
                HideInCharacterSheetAndLevelUp = true, IsClassFeature = true,
                ComponentsArray = comps
            };
            c.AddCachedBlueprint(id, f);
            var all = (BlueprintFeatureReference[])Sp046Field(revSel, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
            if (!all.Any(r => r != null && r.Guid == id))
                Sp046TrySet(revSel, "m_AllFeatures", all.Concat(new[] { Sp046Ref<BlueprintFeatureReference>(f) }).ToArray());
            return f;
        }
        BlueprintBuff AddBuff(string guid, string name, BlueprintComponent[] comps)
        {
            var id = BlueprintGuid.Parse(guid);
            if (c.Load(id) is BlueprintBuff ex) return ex;
            var b = new BlueprintBuff { AssetGuid = id, name = name, ComponentsArray = comps };
            c.AddCachedBlueprint(id, b);
            return b;
        }
        BlueprintAbility AddAb(string guid, string name, BlueprintComponent[] comps)
        {
            var id = BlueprintGuid.Parse(guid);
            if (c.Load(id) is BlueprintAbility ex) return ex;
            var a = new BlueprintAbility { AssetGuid = id, name = name, Type = AbilityType.Supernatural, ComponentsArray = comps };
            c.AddCachedBlueprint(id, a);
            return a;
        }
        AddAbilityResources GrantRes(BlueprintAbilityResource r)
        {
            var g = new AddAbilityResources { name = "Grant" + r.name, RestoreAmount = true };
            Sp046TrySet(g, "m_Resource", Sp046Ref<BlueprintAbilityResourceReference>(r));
            return g;
        }
        AddFacts AddFact(params BlueprintUnitFact[] fs)
        {
            var af = new AddFacts { name = "AddFacts" + fs[0].name };
            Sp046TrySet(af, "m_Facts", fs.Select(Sp046Ref<BlueprintUnitFactReference>).ToArray());
            return af;
        }
        AbilityResourceLogic CostRes(BlueprintAbilityResource r)
        {
            var arl = new AbilityResourceLogic { name = "Cost" + r.name, Amount = 1 };
            Sp046TrySet(arl, "m_IsSpendResource", true);
            Sp046TrySet(arl, "m_RequiredResource", Sp046Ref<BlueprintAbilityResourceReference>(r));
            return arl;
        }

        // 1. Blood of Heroes
        var bohRes = OracleRes("8eb43ae66258f5a408b1e780e0fbd1f1", 0, start: 5, step: 5);
        var bohFear = new SavingThrowContextBonusAgainstDescriptor {
            name = "NativeBohFear", ModifierDescriptor = ModifierDescriptor.Morale,
            SpellDescriptor = (SpellDescriptor)4456480,
            Value = new ContextValue { ValueType = ContextValueType.Rank }
        };
        var bohAtk = new AddContextStatBonus { name = "NativeBohAtk", Stat = StatType.AdditionalAttackBonus, Descriptor = ModifierDescriptor.Morale, Multiplier = 1, Value = new ContextValue { ValueType = ContextValueType.Rank } };
        var bohDmg = new AddContextStatBonus { name = "NativeBohDmg", Stat = StatType.AdditionalDamage, Descriptor = ModifierDescriptor.Morale, Multiplier = 1, Value = new ContextValue { ValueType = ContextValueType.Rank } };
        var bohBuff = AddBuff("ab88d55181c294a40aea8745f1fd6419", "OracleRevelationBloodOfHeroesBuff", new BlueprintComponent[] { OracleRank("OnePlusDivStep", 0, 7), bohAtk, bohDmg, bohFear });
        var bohChaRank = new ContextRankConfig { name = "NativeBohCha" };
        Sp046SetEnum(bohChaRank, "m_BaseValueType", "StatBonus");
        Sp046TrySet(bohChaRank, "m_Stat", StatType.Charisma);
        var bohApply = new ContextActionApplyBuff { DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, BonusValue = new ContextValue { ValueType = ContextValueType.Rank } } };
        Sp046TrySet(bohApply, "m_Buff", Sp046Ref<BlueprintBuffReference>(bohBuff));
        var bohAb = AddAb("cbaba430e1540ac4e917f55bd8800c63", "OracleRevelationBloodOfHeroesAbility", new BlueprintComponent[] {
            CostRes(bohRes), bohChaRank,
            new AbilityEffectRunAction { name = "NativeBohRun", Actions = Sp046Actions(bohApply) }
        });
        AddRev("1b9eebdfa2ad9d44eb60446d465b0cfd", "OracleRevelationBloodOfHeroes", new BlueprintComponent[] { AncPrereq("Boh", ancestors), GrantRes(bohRes), AddFact(bohAb) });

        // 2. Phantom Touch
        var ptRes = OracleRes("e579ad81e91912641a6693d1fd250910", 3, byStat: true);
        var ptDeliv = new AbilityDeliverTouch { name = "NativePtTouch" };
        Sp046TrySet(ptDeliv, "m_TouchWeapon", Sp046Ref<BlueprintItemWeaponReference>(touchWeapon));
        var ptApply = new ContextActionApplyBuff { DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, BonusValue = new ContextValue { ValueType = ContextValueType.Rank } } };
        Sp046TrySet(ptApply, "m_Buff", Sp046Ref<BlueprintBuffReference>(shaken));
        var ptAb = AddAb("7d72d34578a369a4f8d2515d04a96b71", "OracleRevelationPhantomTouchAbility", new BlueprintComponent[] {
            CostRes(ptRes), OracleRank("Div2", useMin: true, min: 1), ptDeliv,
            new AbilityEffectRunAction { name = "NativePtRun", Actions = Sp046Actions(ptApply) }
        });
        AddRev("60a1be62a0edf274580a1cdc5250293e", "OracleRevelationPhantomTouch", new BlueprintComponent[] { AncPrereq("Pt", ancestors), GrantRes(ptRes), AddFact(ptAb) });

        // 3. Sacred Council
        var scRes = OracleRes("16297f2f3448f1940aa378daaab8bb1f", 0, byStat: true);
        var scBuff = AddBuff("a89480407531d3f4f86c3cdb361ecb26", "OracleRevelationSacredCouncilBuff", new BlueprintComponent[] {
            new AddStatBonus { name = "NativeScCl", Stat = StatType.BonusCasterLevel, Value = 2, Descriptor = ModifierDescriptor.UntypedStackable }
        });
        var scApply = new ContextActionApplyBuff { DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, BonusValue = new ContextValue { Value = 1 } } };
        Sp046TrySet(scApply, "m_Buff", Sp046Ref<BlueprintBuffReference>(scBuff));
        var scAb = AddAb("4a670e5d8de89c24eaca733194b74395", "OracleRevelationSacredCouncilAbility", new BlueprintComponent[] {
            CostRes(scRes),
            new AbilityEffectRunAction { name = "NativeScRun", Actions = Sp046Actions(scApply) }
        });
        AddRev("a8ae26103124a1c409279392b8f56238", "OracleRevelationSacredCouncil", new BlueprintComponent[] { AncPrereq("Sc", ancestors), GrantRes(scRes), AddFact(scAb) });

        // 4. Spirit of the Warrior
        var swarRes = OracleRes("968934de9ae9f674199f6502e45c18e9", 0, start: 2, step: 2);
        var swarKeen1 = new BuffEnchantAnyWeapon { name = "NativeSwarKeen1" };
        Sp046SetEnum(swarKeen1, "Slot", "PrimaryHand");
        Sp046TrySet(swarKeen1, "m_EnchantmentBlueprint", Sp046Ref<BlueprintItemEnchantmentReference>(keen));
        var swarKeen2 = new BuffEnchantAnyWeapon { name = "NativeSwarKeen2" };
        Sp046SetEnum(swarKeen2, "Slot", "SecondaryHand");
        Sp046TrySet(swarKeen2, "m_EnchantmentBlueprint", Sp046Ref<BlueprintItemEnchantmentReference>(keen));
        var swarBuff = AddBuff("0a790e530dc3c4942a3ff4138fb1d73f", "OracleRevelationSpiritOfTheWarriorBuff", new BlueprintComponent[] {
            OracleRank("DelayedStartPlusDivStep", 1, 4),
            new AddContextStatBonus { name = "NativeSwarStr", Stat = StatType.Strength, Descriptor = ModifierDescriptor.Enhancement, Multiplier = 1, Value = 4 },
            new AddContextStatBonus { name = "NativeSwarDex", Stat = StatType.Dexterity, Descriptor = ModifierDescriptor.Enhancement, Multiplier = 1, Value = 4 },
            new AddContextStatBonus { name = "NativeSwarCon", Stat = StatType.Constitution, Descriptor = ModifierDescriptor.Enhancement, Multiplier = 1, Value = 4 },
            new AddContextStatBonus { name = "NativeSwarAc", Stat = StatType.AC, Descriptor = ModifierDescriptor.NaturalArmor, Multiplier = 1, Value = 4 },
            new AddContextStatBonus { name = "NativeSwarBab", Stat = StatType.BaseAttackBonus, Descriptor = ModifierDescriptor.None, Multiplier = 1, Value = new ContextValue { ValueType = ContextValueType.Rank } },
            swarKeen1, swarKeen2,
            new RecalculateOnStatChange { name = "NativeSwarRecalc", Stat = StatType.BaseAttackBonus }
        });
        var swarToggleId = BlueprintGuid.Parse("e52a097da8913cc4baa31415e1fd72de");
        if (c.Load(swarToggleId) == null)
        {
            var cost = new ActivatableAbilityResourceLogic { name = "NativeSwarCost", SpendType = ActivatableAbilityResourceLogic.ResourceSpendType.NewRound };
            Sp046TrySet(cost, "m_RequiredResource", Sp046Ref<BlueprintAbilityResourceReference>(swarRes));
            var swarToggle = new BlueprintActivatableAbility {
                AssetGuid = swarToggleId, name = "OracleRevelationSpiritOfTheWarriorAbility",
                ActivationType = AbilityActivationType.WithUnitCommand,
                ComponentsArray = new BlueprintComponent[] { cost }
            };
            Sp046TrySet(swarToggle, "m_Buff", Sp046Ref<BlueprintBuffReference>(swarBuff));
            c.AddCachedBlueprint(swarToggleId, swarToggle);
        }
        var swarToggleLoaded = c.Load(swarToggleId) as BlueprintActivatableAbility;
        AddRev("b63f198db73edbc4b84d863cb681f5bc", "OracleRevelationSpiritOfTheWarrior", new BlueprintComponent[] { OracleMin(11), AncPrereq("Swar", ancestors), GrantRes(swarRes), AddFact(swarToggleLoaded) });

        // 5. Spirit Shield (+4/+6/+8/+10/+12 + L13 miss)
        var sshRes = OracleRes("4e2643951e18e1d43868ba7451690657", 0, byLevel: true);
        var sshBuff = AddBuff("60376ff2340b73346a555979d7763088", "OracleRevelationSpiritShieldBuff", new BlueprintComponent[] {
            OracleRank("DelayedStartPlusDivStep", 7, 4),
            new ContextCalculateSharedValue { ValueType = AbilitySharedValue.Heal, Value = new ContextDiceValue { DiceCountValue = new ContextValue(), BonusValue = new ContextValue { ValueType = ContextValueType.Rank } }, Modifier = 2.0 },
            new ContextCalculateSharedValue { ValueType = AbilitySharedValue.Damage, Value = new ContextDiceValue { DiceType = DiceType.One, DiceCountValue = 4, BonusValue = new ContextValue { ValueType = ContextValueType.Shared, ValueShared = AbilitySharedValue.Heal } }, Modifier = 1.0 },
            new AddContextStatBonus { name = "NativeSshAc", Stat = StatType.AC, Descriptor = ModifierDescriptor.Armor, Multiplier = 1, Value = new ContextValue { ValueType = ContextValueType.Shared, ValueShared = AbilitySharedValue.Damage } }
        });
        var sshApply = new ContextActionApplyBuff { DurationValue = new ContextDurationValue { Rate = DurationRate.Hours, BonusValue = 1 } };
        Sp046TrySet(sshApply, "m_Buff", Sp046Ref<BlueprintBuffReference>(sshBuff));
        var sshMissApply = new ContextActionApplyBuff { DurationValue = new ContextDurationValue { Rate = DurationRate.Hours, BonusValue = 1 } };
        Sp046TrySet(sshMissApply, "m_Buff", Sp046Ref<BlueprintBuffReference>(Native<BlueprintBuff>(c, Sp046OracleMissBuffId)));
        var sshAb = AddAb("b363ff079a69b844e957b8295cc73eb0", "OracleRevelationSpiritShieldAbility", new BlueprintComponent[] {
            CostRes(sshRes), OracleRank(),
            new ContextCalculateSharedValue { ValueType = AbilitySharedValue.Damage, Value = new ContextDiceValue { DiceCountValue = new ContextValue(), BonusValue = new ContextValue { ValueType = ContextValueType.Rank } }, Modifier = 1.0 },
            new AbilityEffectRunAction { name = "NativeSshRun", Actions = Sp046Actions(
                sshApply,
                new Conditional {
                    name = "NativeSshL13",
                    ConditionsChecker = new ConditionsChecker { Conditions = new Condition[] { new ContextConditionSharedValueHigher { SharedValue = AbilitySharedValue.Damage, HigherOrEqual = 13 } } },
                    IfTrue = Sp046Actions(sshMissApply),
                    IfFalse = Sp046Actions()
                }) }
        });
        AddRev("899d6aa944ea9654fa829edcaedcf073", "OracleRevelationSpiritShield", new BlueprintComponent[] { AncPrereq("Ssh", ancestors), GrantRes(sshRes), AddFact(sshAb) });

        // 6. Spirit Walk (initially missing OracleAncestorsMysteryFeature in vanilla!)
        var swalkRes = OracleRes("f2f4cd3185be358469c73f3b7853dac2", 0, start: 11, step: 4);
        var swalkBuff = AddBuff("d866e35ea697df445be59e3d1c69d9ba", "OracleRevelationSpiritWalkBuff", new BlueprintComponent[] {
            new AddCondition { Condition = UnitCondition.Invisible },
            AddFact(incorporeal)
        });
        var swalkApply = new ContextActionApplyBuff { DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, BonusValue = new ContextValue { ValueType = ContextValueType.Rank } } };
        Sp046TrySet(swalkApply, "m_Buff", Sp046Ref<BlueprintBuffReference>(swalkBuff));
        var swalkAb = AddAb("016802e4b8133f741a80ca7e9860a24a", "OracleRevelationSpiritWalkAbility", new BlueprintComponent[] {
            CostRes(swalkRes), OracleRank(),
            new AbilityEffectRunAction { name = "NativeSwalkRun", Actions = Sp046Actions(swalkApply) }
        });
        AddRev("87bdea524911cfa4dacfe7243abe8b51", "OracleRevelationSpiritWalk", new BlueprintComponent[] { OracleMin(11), AncPrereq("Swalk", boneMystery), GrantRes(swalkRes), AddFact(swalkAb) });

        // 7. Storm of Souls (ClassLevel DamageDice, lacks ContextCalculateAbilityParamsBasedOnClass in vanilla!)
        var sosRes = OracleRes("4e5515062208cf148b4abe799ff1042d", 0, start: 7, step: 4);
        var sosDmgRank = new ContextRankConfig { name = "NativeSosDmg" };
        Sp046SetEnum(sosDmgRank, "m_Type", "DamageDice");
        Sp046SetEnum(sosDmgRank, "m_BaseValueType", "ClassLevel");
        Sp046SetEnum(sosDmgRank, "m_Progression", "Div2");
        Sp046TrySet(sosDmgRank, "m_Class", new[] { Sp046Ref<BlueprintCharacterClassReference>(oracle) });
        var sosDmg = new ContextActionDealDamage {
            name = "NativeSosDmg",
            DamageType = new DamageTypeDescription { Type = DamageType.Energy, Energy = DamageEnergyType.Divine },
            Value = new ContextDiceValue { DiceType = DiceType.D8, DiceCountValue = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.DamageDice } },
            IsAoE = true, HalfIfSaved = true
        };
        var sosAb = AddAb("5195439b7006c004e865ff741a57406c", "OracleRevelationStormOfSoulsAbility", new BlueprintComponent[] {
            CostRes(sosRes), OracleRank(), sosDmgRank,
            new AbilityTargetsAround { name = "NativeSosBurst" },
            new AbilityEffectRunAction { name = "NativeSosRun", SavingThrowType = SavingThrowType.Fortitude, Actions = Sp046Actions(sosDmg) }
        });
        AddRev("0edd5395810cf3441a093ca49efb858f", "OracleRevelationStormOfSouls", new BlueprintComponent[] { OracleMin(7), AncPrereq("Sos", ancestors), GrantRes(sosRes), AddFact(sosAb) });
    }

    static List<SimpleBlueprint> Sp046CollectGraph(SimpleBlueprint root)
    {
        var list = new List<SimpleBlueprint>();
        var visited = new HashSet<BlueprintGuid>();
        var seenObj = new HashSet<object>(PsychicAssetsIdentityComparer());
        void Walk(object node)
        {
            if (node == null) return;
            if (node is SimpleBlueprint sb)
            {
                if (!visited.Add(sb.AssetGuid)) return;
                list.Add(sb);
            }
            if (!seenObj.Add(node)) return;
            if (node is BlueprintReferenceBase rb)
            {
                var t = AccessTools.Property(typeof(BlueprintReferenceBase), "Cached")?.GetValue(rb, null) as SimpleBlueprint;
                if (t != null && !(t is BlueprintCharacterClass) && !(t is BlueprintArchetype) && !(t is BlueprintFeatureSelection))
                    Walk(t);
                return;
            }
            if (node is System.Collections.IEnumerable seq && !(node is string))
            {
                foreach (var item in seq) Walk(item);
                return;
            }
            var type = node.GetType();
            if (type.IsPrimitive || type.IsEnum || node is string || node is Type || node is Delegate || node is BlueprintGuid) return;
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    if (!f.IsNotSerialized && f.DeclaringType == t)
                        Walk(f.GetValue(node));
        }
        Walk(root);
        return list;
    }

    static IEqualityComparer<object> PsychicAssetsIdentityComparer()
        => (IEqualityComparer<object>)AccessTools.Property(T("PsychicAssets"), "IdentityComparer").GetValue(null);

    static int Speaker0046(BlueprintsCache parent, Harmony h)
    {
        int start = checks;
        var timeModule = T("TimeMystery0046");
        var speakerModule = T("SpeakerForThePast0046");
        var binder = (GuidClassBinder)Kingmaker.Blueprints.JsonSystem.Json.Serializer.Binder;
        foreach (var type in Main.GetTypes().Where(t => t.Name.EndsWith("0046")))
            foreach (var attr in type.GetCustomAttributes(typeof(TypeIdAttribute), false).Cast<TypeIdAttribute>())
                binder.AddToCache(type, attr.GuidString);

        Console.WriteLine("0046 [BOUNDARY]: Managed tests run in PsychicTests EngineLauncher host; missing compact-JSON Ancestors/Time donors (ThreefoldAspect/Contingency/TemporalStasis/SpiritShieldMissBuff/omitted Ancestors revelation sub-assets) and TTT-Base SandsOfTimeCast are hydrated in-fixture from blueprints.zip schemas.");

        // Run Speaker0046 directly on real Game.Instance.Rulebook / Rulebook.CurrentContext
        // without Arcane01446Tests's single-event NativeRule transpilers clobbering PreviousEvent.
        h.Unpatch(AccessTools.PropertyGetter(typeof(Rulebook), "CurrentContext"), AccessTools.Method(Helper, "Context"));
        foreach (var m in new[] { AccessTools.Method(typeof(RuleSavingThrow), "OnTrigger"), AccessTools.Method(typeof(RuleSavingThrow), "RollD20") })
            h.Unpatch(m, AccessTools.Method(Helper, "RuleBoundary"));
        foreach (var m in new[] { AccessTools.Method(typeof(RuleSkillCheck), "OnTrigger"), AccessTools.Method(typeof(RuleSkillCheck), "RollD20") })
            h.Unpatch(m, AccessTools.Method(typeof(Archetypes01445Tests), "NestedRules"));
        h.Unpatch(AccessTools.PropertyGetter(typeof(RulebookEvent.Dice), "D20"), AccessTools.Method(typeof(Archetypes01445Tests), "NestedD20"));
        h.Unpatch(AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }), AccessTools.Method(Helper, "Dice"));

        h.Patch(AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }),
            prefix: new HarmonyMethod(typeof(Arcane01446Tests), nameof(Sp046DicePrefix)) { priority = Priority.First });
        h.Patch(AccessTools.Method(typeof(RuleSpellResistanceCheck), "OnTrigger"),
            prefix: new HarmonyMethod(typeof(Arcane01446Tests), nameof(Sp046StopRulePrefix)) { priority = Priority.Last });
        h.Patch(AccessTools.Method(typeof(RuleDispelMagic), "OnTrigger"),
            prefix: new HarmonyMethod(typeof(Arcane01446Tests), nameof(Sp046StopRulePrefix)) { priority = Priority.Last });

        try
        {
            // ---- 1. Atomic TTT-Base gate rejection + late-provider retry ----
            var c = Fresh(parent);
            Sp046HydrateIsolatedHost(c, speakerModule, includeSands: false);
            var oracleSnapBefore = Sp046OracleAncestorsSnapshot(c);
            ShamanClassArchetypeCount(c, out var archetypesBefore);

            string timeRootToken = (string)AccessTools.Field(timeModule, "RootToken").GetValue(null);
            string timeSpellsToken = (string)AccessTools.Field(timeModule, "SpellsToken").GetValue(null);
            string timeFinalToken = (string)AccessTools.Field(timeModule, "FinalToken").GetValue(null);
            string timeFinalSlaToken = (string)AccessTools.Field(timeModule, "FinalStopAbilityToken").GetValue(null);
            string ancWeaponToken = (string)AccessTools.Field(timeModule, "AncestralWeaponToken").GetValue(null);

            bool timeGate = (bool)Call(timeModule, "Register", c);
            Check(!timeGate && c.Load(Id(timeRootToken)) == null,
                "0046 TimeMystery Register returns false atomically when external Sands of Time is absent");
            bool speakerGate = (bool)Call(speakerModule, "Register", c);
            Check(!speakerGate && c.Load(Id(Sp046Root)) == null && c.Load(Id(Sp046Selection)) == null && c.Load(Id(Sp046SpellList)) == null,
                "0046 SpeakerForThePast Register returns false atomically without TimeMystery / Sands of Time");
            ShamanClassArchetypeCount(c, out var archetypesAfter);
            Check(archetypesAfter == archetypesBefore && Sp046OracleAncestorsSnapshot(c) == oracleSnapBefore,
                "0046 rejection leaves ShamanClass.m_Archetypes and native Oracle Ancestors untouched");
            var spReject = (string)AccessTools.Field(speakerModule, "GateReport").GetValue(null);
            var spProvider = (string)AccessTools.Field(speakerModule, "ProviderReport").GetValue(null);
            Check(spReject.Contains("TabletopTweaks-Base") && spProvider.Contains(Sp046SandsId),
                "0046 rejection diagnostics name missing TTT-Base Sands of Time provider");

            Sp046InjectSandsProvider(c);
            Check((bool)Call(timeModule, "Register", c), "0046 TimeMystery retry succeeds once Sands of Time arrives");
            Check((bool)Call(speakerModule, "Register", c), "0046 Speaker retry succeeds after TimeMystery publishes: " + AccessTools.Field(speakerModule, "GateReport").GetValue(null));
            Check((bool)Call(timeModule, "Register", c) && (bool)Call(speakerModule, "Register", c),
                "0046 idempotent re-entry succeeds after late-provider retry");

            // ---- 2. Clean Time Mystery + Ancestors Completion + Auto-Discovery Audit ----
            var c2 = Fresh(parent);
            Sp046HydrateIsolatedHost(c2, speakerModule, includeSands: true);
            Check((bool)Call(timeModule, "Register", c2), "0046 TimeMystery publishes cleanly");

            var oracleAncSnapAfterTime = Sp046OracleAncestorsSnapshot(c2);
            var timeMystery = Asset<BlueprintFeature>(c2, timeRootToken);
            var timeSpells = Asset<BlueprintFeature>(c2, timeSpellsToken);
            var timeFinal = Asset<BlueprintFeature>(c2, timeFinalToken);
            var oracleMysterySel = Native<BlueprintFeatureSelection>(c2, Sp046OracleMysterySelId);
            var secondMystery = Native<BlueprintFeatureSelection>(c2, Sp046SecondMysteryId);
            var oracleRevSel = Native<BlueprintFeatureSelection>(c2, Sp046OracleRevSelId);
            var spiritWalk = Native<BlueprintFeature>(c2, "87bdea524911cfa4dacfe7243abe8b51");
            var ancestralOracle = Asset<BlueprintFeature>(c2, ancWeaponToken);

            Check(timeMystery.Groups.SequenceEqual(new[] { FeatureGroup.OracleMystery }) && !timeMystery.HideInCharacterSheetAndLevelUp && timeMystery.IsClassFeature,
                "0046 Time Mystery follows vanilla OracleMystery Groups & UI visibility");
            var timeSkills = timeMystery.GetComponents<AddClassSkill>().Select(s => s.Skill).ToArray();
            Check(new[] { StatType.SkillMobility, StatType.SkillPerception, StatType.SkillUseMagicDevice }.All(s => timeSkills.Contains(s)),
                "0046 Time Mystery adds Mobility, Perception and UMD class skills");
            Check(timeMystery.GetComponent<AddFeaturesFromSelectionToDescription>() is AddFeaturesFromSelectionToDescription afd &&
                  afd.OnlyIfRequiresThisFeature && afd.FeatureSelection?.AssetGuid == oracleRevSel.AssetGuid,
                "0046 Time Mystery carries AddFeaturesFromSelectionToDescription bound to OracleRevelationSelection");
            Check(timeSpells.GetComponents<AddKnownSpell>().Count() == 9 && timeSpells.GetComponent<AddSpellsToDescription>() != null,
                "0046 Time Mystery bonus spells grant circles 1..9 including Sands of Time and Time Stop plus AddSpellsToDescription");
            Check(oracleMysterySel.AllFeatures.Count(x => x?.AssetGuid == timeMystery.AssetGuid) == 1 &&
                  secondMystery.AllFeatures.Count(x => x?.AssetGuid == timeMystery.AssetGuid) == 1,
                "0046 Time Mystery registered once in OracleMysterySelection and SecondMystery");
            var timeStopRoot = Native<BlueprintAbility>(c2, "585f7776285d17b60b9e8e2fe42a69be");
            var timeFinalSla = Asset<BlueprintAbility>(c2, timeFinalSlaToken);
            Check(timeFinalSla != null && timeFinalSla.Type == AbilityType.SpellLike && timeStopRoot.Type == AbilityType.Spell,
                "0046 Time Final Revelation clones Time Stop into 1/day SLA without mutating shared TimeStop01438 spell");
            Check(timeFinal.GetComponents<AddStatBonus>().Count(b => b.Descriptor == ModifierDescriptor.Competence && b.Value == 2) == 3,
                "0046 Time Final Revelation grants +2 competence to Int/Wis/Cha");

            // Time Oracle's own Rewind Time & Time Flicker fixes + Ancestors completion.
            var timeRewindToggle = Asset<BlueprintActivatableAbility>(c2, "RewindTimeOracleRevelation0046AttackRollToggle");
            var timeRewindActiveBuff = Asset<BlueprintBuff>(c2, "RewindTimeOracleRevelation0046Buff");
            var timeFlickerToggle = Asset<BlueprintActivatableAbility>(c2, "TimeFlickerOracleRevelation0046DisplacementToggle");
            Check(timeRewindActiveBuff.GetComponent<SpeakerFailedReroll0046>() is SpeakerFailedReroll0046 tAct && !tAct.SpendOnTrigger && tAct.ConsumeBuffOnTrigger,
                "0046 Time Oracle Rewind Time active buff pre-pays on cast & consumes on trigger");
            Check(timeRewindToggle.GetComponent<ActivatableAbilityResourceLogic>() == null,
                "0046 Time Oracle Rewind Time reaction toggles do not drain resource passively");
            Check((UnitCommand.CommandType)Sp046Field(timeFlickerToggle, "m_ActivateWithUnitCommand") == UnitCommand.CommandType.Standard,
                "0046 Time Oracle Time Flicker Displacement toggle uses Standard action");
            Check(ancestralOracle != null && oracleRevSel.AllFeatures.Count(x => x?.AssetGuid == ancestralOracle.AssetGuid) == 1 &&
                  spiritWalk.GetComponent<PrerequisiteFeaturesFromList>().Features.Any(m => m?.AssetGuid == BlueprintGuid.Parse(Sp046OracleMysteryId)),
                "0046 Ancestors Mystery completed with Ancestral Weapon + Spirit Walk prerequisites");

            // Publish Speaker for the Past automatically from Time + Ancestors.
            Check((bool)Call(speakerModule, "Register", c2), "0046 SpeakerForThePast publishes via automatic Time+Ancestors discovery");
            Check((bool)Call(speakerModule, "Register", c2), "0046 SpeakerForThePast re-entry is idempotent");
            Check(Sp046OracleAncestorsSnapshot(c2) == oracleAncSnapAfterTime,
                "0046 SpeakerForThePast leaves native Oracle Ancestors blueprints completely untouched");

            var root = Asset<BlueprintArchetype>(c2, Sp046Root);
            var sel = Asset<BlueprintFeatureSelection>(c2, Sp046Selection);
            var spellList = Asset<BlueprintSpellList>(c2, Sp046SpellList);
            var mysteriesFeat = Asset<BlueprintFeature>(c2, Sp046Mysteries);
            var speakerSkills = root.ClassSkills ?? Array.Empty<StatType>();
            Check(root.ReplaceClassSkills &&
                  new[] { StatType.SkillKnowledgeWorld, StatType.SkillPerception, StatType.SkillUseMagicDevice }.All(s => speakerSkills.Contains(s)),
                "0046 Speaker replaces class skills with Knowledge (World), Perception, UMD");
            Check(root.RemoveFeatures.Any(e => e.Level == 1 && e.Features.Any(x => x.AssetGuid == BlueprintGuid.Parse("d22f319fefac4ca4b90f03ac5cb9c714"))) &&
                  root.RemoveFeatures.Any(e => e.Level == 4 && e.Features.Any(x => x.AssetGuid == BlueprintGuid.Parse("70e6075a72633ba49804ccb6203fb18c"))),
                "0046 Speaker progression removes L1 familiar and L4 wandering spirit");
            Check(spellList.SpellsByLevel.Skip(1).Take(9).All(r => r.Spells.Count == 2) &&
                  spellList.SpellsByLevel[3].Spells.Any(s => s.AssetGuid == BlueprintGuid.Parse(Sp046SandsId)),
                "0046 auto-discovered 18 spells (9 circles x 2) including Time's Sands of Time");

            var spRevs = sel.AllFeatures.ToArray();
            Check(spRevs.Length == 16 && Sp046Names.All(n => spRevs.Any(r => Sp046DisplayName(r) == n)),
                "0046 RevelationPastSelection auto-discovered & cloned all 16 Time + Ancestors revelations");
            var spGraph = new SimpleBlueprint[] { mysteriesFeat }.Concat(spRevs).SelectMany(Sp046CollectGraph).Distinct().ToArray();
            var spComps = Sp046Components(spGraph);
            var spParamCalcs = spComps.OfType<ContextCalculateAbilityParamsBasedOnClass>().ToArray();
            Check(spParamCalcs.All(p => Sp046RefGuid(Sp046Field(p, "m_CharacterClass")) == BlueprintGuid.Parse(Sp046ClassId) && p.StatType == StatType.Wisdom),
                "0046 every cloned Speaker revelation ability calculates DC/CL via ShamanClass + Wisdom, never Oracle/Charisma");
            var archPrereqs = spComps.OfType<PrerequisiteArchetypeLevel>().Select(x => x.OwnerBlueprint?.name + ":" + x.Level).ToArray();
            var clsPrereqs = spComps.OfType<PrerequisiteClassLevel>().Select(x => x.OwnerBlueprint?.name + ":" + x.Level).ToArray();
            Check(archPrereqs.Length == 8 && clsPrereqs.Length == 0 &&
                  spRevs.All(r => r.Groups.Length == 0 && !r.HideInCharacterSheetAndLevelUp),
                "0046 cloned Speaker revelations prereqs mismatch: arch=" + archPrereqs.Length + "[" + string.Join(",", archPrereqs) + "] cls=" + clsPrereqs.Length + "[" + string.Join(",", clsPrereqs) + "]");
            // Contract C01 & D-0046-04: replaces old B assertion (BonusCasterLevel -> SpeakerCasterLevelChecks0046).
            Check(!spComps.OfType<AddStatBonus>().Any(b => b.Stat == StatType.BonusCasterLevel) &&
                  spComps.OfType<SpeakerCasterLevelChecks0046>().Count() == 1,
                "0046 D-0046-04: BonusCasterLevel removed; Sacred Council uses SpeakerCasterLevelChecks0046 (+2 CL checks)");

            // Check ModuleCatalog entries for both modules.
            var catDefs = (Array)AccessTools.Field(T("ModuleCatalog"), "Definitions").GetValue(null);
            var catNames = catDefs.Cast<object>().Select(d => (string)Sp046Member(d, "Name")).ToArray();
            string timeModName = (string)AccessTools.Field(timeModule, "ModuleName").GetValue(null);
            string speakerModName = (string)AccessTools.Field(speakerModule, "ModuleName").GetValue(null);
            Check(catNames.Contains(timeModName) && catNames.Contains(speakerModName),
                "0046 ModuleCatalog contains diagnostic entries for Time Mystery and Speaker for the Past");

            // ---- 3. Runtime Rulebook Tests: D-0046-01 Rewind Time ----
            var rewindFeat = spRevs.First(r => Sp046DisplayName(r) == "Rewind Time");
            var rewindRes = rewindFeat.GetComponent<AddAbilityResources>().Resource;
            var rewindAllComps = Sp046CollectGraph(rewindFeat).OfType<BlueprintScriptableObject>().SelectMany(b => b.ComponentsArray).ToArray();
            var rewindRerolls = rewindAllComps.OfType<SpeakerFailedReroll0046>().ToArray();
            var rewindActiveBuff = (BlueprintBuff)rewindRerolls.Single(r => !r.SpendOnTrigger && r.ConsumeBuffOnTrigger).OwnerBlueprint;
            var rewindSaveToggle = rewindRerolls.Single(r => r.SpendOnTrigger && r.Rules.Length == 1 && r.Rules[0] == RuleType.SavingThrow);
            var rewindSaveBuff = (BlueprintBuff)rewindSaveToggle.OwnerBlueprint;

            var uRewind = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "rewind", Sp046ClassId, 14);
            uRewind.Progression.Classes[0].AddArchetype(root);
            uRewind.Stats.Wisdom.BaseValue = 18;
            uRewind.Stats.SaveWill.BaseValue = 5;
            Fact(uRewind, rewindFeat);
            uRewind.Resources.Restore(rewindRes, 10);

            // Active Swift cast: pre-pay 1 leaving 0 remaining, then apply Active buff.
            uRewind.Resources.Spend(rewindRes, uRewind.Resources.GetResourceAmount(rewindRes));
            Check(uRewind.Resources.GetResourceAmount(rewindRes) == 0, "0046 D-0046-01 active cast pre-paid pool down to 0 remaining");
            Buff(uRewind, rewindActiveBuff);

            Sp046Dice.Clear(); Sp046DiceRollsSeen = 0; Sp046DiceOverrideActive = true;
            try
            {
                // Native passing save -> NO reroll, NO consume.
                Sp046Dice.Enqueue(20);
                var passSave = Rulebook.Trigger(new RuleSavingThrow(uRewind, SavingThrowType.Will, 20));
                Check(passSave.IsPassed && Sp046DiceRollsSeen == 1 && uRewind.Buffs.HasFact(rewindActiveBuff),
                    "0046 D-0046-01 native passing save does not trigger reroll or consume active Rewind buff");

                // Wrong rule type (RuleType.Reason) -> NO reroll, NO consume.
                Sp046DiceRollsSeen = 0; Sp046Dice.Enqueue(2);
                Rulebook.Trigger(new RuleRollD20(uRewind));
                Check(Sp046DiceRollsSeen == 1 && uRewind.Buffs.HasFact(rewindActiveBuff),
                    "0046 D-0046-01 unsupported RuleType does not trigger reroll or consume active buff");

                // Eligible failed save at 0 resource remaining -> triggers reroll ONCE (2 -> 19) & consumes active buff.
                Sp046DiceRollsSeen = 0; Sp046Dice.Enqueue(2); Sp046Dice.Enqueue(19);
                var failThenPass = Rulebook.Trigger(new RuleSavingThrow(uRewind, SavingThrowType.Will, 22));
                Check(failThenPass.IsPassed && Sp046DiceRollsSeen == 2 && !uRewind.Buffs.HasFact(rewindActiveBuff),
                    "0046 D-0046-01 active Rewind buff triggers at 0 remaining, succeeds on reroll, and consumes itself (passed=" + failThenPass.IsPassed +
                    " rolls=" + Sp046DiceRollsSeen + " hasBuff=" + uRewind.Buffs.HasFact(rewindActiveBuff) + ")");

                // Reaction toggle: SavingThrow toggle spends 0 upfront, ignores SkillCheck,
                // ignores modifier-inclusive pass, spends 1 on matching fail.
                uRewind.Resources.Restore(rewindRes, 2);
                Buff(uRewind, rewindSaveBuff);
                Check(uRewind.Resources.GetResourceAmount(rewindRes) == 2,
                    "0046 D-0046-01 enabling SavingThrow reaction toggle spends 0 upfront");

                Sp046DiceRollsSeen = 0; Sp046Dice.Enqueue(1);
                Rulebook.Trigger(new RuleSkillCheck(uRewind, StatType.SkillPerception, 30));
                Check(Sp046DiceRollsSeen == 1 && uRewind.Resources.GetResourceAmount(rewindRes) == 2,
                    "0046 D-0046-01 SkillCheck failure ignores SavingThrow reaction toggle with 0 resource spent");

                Sp046DiceRollsSeen = 0; Sp046Dice.Enqueue(10); // 10 + 9 = 19 >= DC 18
                var modPass = Rulebook.Trigger(new RuleSavingThrow(uRewind, SavingThrowType.Will, 18));
                Check(modPass.IsPassed && Sp046DiceRollsSeen == 1 && uRewind.Resources.GetResourceAmount(rewindRes) == 2,
                    "0046 D-0046-01 modifier-inclusive passing Will save does not reroll or spend");

                Sp046DiceRollsSeen = 0; Sp046Dice.Enqueue(2); Sp046Dice.Enqueue(18);
                var toggleReroll = Rulebook.Trigger(new RuleSavingThrow(uRewind, SavingThrowType.Will, 25));
                Check(toggleReroll.IsPassed && Sp046DiceRollsSeen == 2 && uRewind.Resources.GetResourceAmount(rewindRes) == 1,
                    "0046 D-0046-01 eligible failed Will save rerolls once via toggle and deducts 1 resource (2->1)");

                // Simultaneous Active buff + Save reaction toggle -> SINGLE reroll (2 rolls), NO double deduct!
                Buff(uRewind, rewindActiveBuff);
                int beforeSimultaneous = uRewind.Resources.GetResourceAmount(rewindRes);
                Sp046DiceRollsSeen = 0; Sp046Dice.Enqueue(2); Sp046Dice.Enqueue(18);
                var sim = Rulebook.Trigger(new RuleSavingThrow(uRewind, SavingThrowType.Will, 25));
                Check(sim.IsPassed && Sp046DiceRollsSeen == 2 &&
                      !uRewind.Buffs.HasFact(rewindActiveBuff) &&
                      uRewind.Resources.GetResourceAmount(rewindRes) == beforeSimultaneous,
                    "0046 D-0046-01 simultaneous Active + Save reaction performs only 1 reroll and never double-deducts");
            }
            finally
            {
                Sp046DiceOverrideActive = false;
                Sp046Dice.Clear();
            }

            // ---- 4. Runtime Spellbook Tests: D-0046-02 Bonus Spellbook Cleanup ----
            var shamanCls = Native<BlueprintCharacterClass>(c2, Sp046ClassId);
            var shamanBookBp = Native<BlueprintSpellbook>(c2, Sp046ShamanSpellbookId);
            var shamanNativeList = Native<BlueprintSpellList>(c2, Sp046ShamanListId);
            var heroism = Native<BlueprintAbility>(c2, "5ab0d42fb68c9e34abae4921822b9d63"); // circle 3 Speaker spell
            var longstrider = Native<BlueprintAbility>(c2, "14c90900b690cac429b229efdf416127"); // circle 1 Speaker spell
            var crusadersEdge = Native<BlueprintAbility>(c2, "be5452c422a6ea744bf1037b0a443bb1"); // circle 4 Speaker spell

            var uBook = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "spellbook", Sp046ClassId, 10);
            uBook.Progression.Classes[0].AddArchetype(root);
            var book = new Spellbook(uBook.Descriptor, shamanBookBp);
            Set(book, "m_BaseLevelInternal", 10);
            Set(uBook.Descriptor, "m_Spellbooks", new Dictionary<BlueprintSpellbook, Spellbook> { { shamanBookBp, book } });

            var otherKnownComp = new AddKnownSpell { name = "OtherHeroism", SpellLevel = 3 };
            Set(otherKnownComp, "m_CharacterClass", Sp046Ref<BlueprintCharacterClassReference>(shamanCls));
            Set(otherKnownComp, "m_Spell", Sp046Ref<BlueprintAbilityReference>(heroism));
            var otherFeat = new BlueprintFeature {
                AssetGuid = BlueprintGuid.Parse("0046a1b2c3d4e5f600000000000000fe"),
                name = "Speaker0046TestOtherKnown",
                ComponentsArray = new BlueprintComponent[] { otherKnownComp }
            };
            Fact(uBook, otherFeat);

            var nativeL1Spell = shamanNativeList.SpellsByLevel[1].Spells.FirstOrDefault(s => s != null && !spellList.SpellsByLevel[1].Spells.Contains(s));
            if (nativeL1Spell != null) book.AddKnown(1, nativeL1Spell);
            var tempOverlapSpell = Native<BlueprintAbility>(c2, "41e8a952da7a5c247b3ec1c2dbb73018"); // Hold Monster circle 5
            book.AddKnownTemporary(5, tempOverlapSpell);

            var mystFact = Fact(uBook, mysteriesFeat);
            var syncRt = mystFact.Components.FirstOrDefault(rc => rc.SourceBlueprintComponent is SpeakerSpellbookSync0046);
            Check(syncRt != null, "0046 D-0046-02 SpeakerSpellbookSync0046 runtime component active");
            Check(book.IsKnown(longstrider) && book.IsKnown(heroism) && book.IsKnown(crusadersEdge),
                "0046 D-0046-02 at Shaman L10 Speaker circles 1..5 are known on primary spellbook");
            Check(book.SpecialLists.Count == 0, "0046 D-0046-02 zero spells leak into spirit magic SpecialLists");

            // De-level 10 -> 6 (drops 4th/5th circle coverage): Crusader's Edge removed, L1..3 + temp preserved!
            Set(book, "m_BaseLevelInternal", 6);
            using (syncRt.RequestEventContext()) ((SpeakerSpellbookSync0046)syncRt.SourceBlueprintComponent).HandleUnitGainLevel();
            Check(!book.IsKnown(crusadersEdge) && book.IsKnown(longstrider) && book.IsKnown(heroism),
                "0046 D-0046-02 de-level removes uncovered Speaker circle-4 exclusive spell while keeping L1..3");
            Check(book.IsKnown(tempOverlapSpell), "0046 D-0046-02 de-level preserves temporary known spell overlap");

            // Feature removal -> removes remaining Speaker exclusives, preserves overlapping active Heroism + native L1 + temp!
            uBook.Facts.Remove(mystFact);
            Check(!book.IsKnown(longstrider), "0046 D-0046-02 feature removal removes Speaker-exclusive Longstrider");
            Check(book.IsKnown(heroism), "0046 D-0046-02 feature removal preserves overlapping spell from active other AddKnownSpell source");
            if (nativeL1Spell != null) Check(book.IsKnown(nativeL1Spell), "0046 D-0046-02 feature removal preserves native Shaman list spell");

            // Idempotent repeat cleanup!
            var cleanupMethod = AccessTools.Method(typeof(SpeakerSpellbookSync0046), "Cleanup",
                new[] { typeof(UnitDescriptor), typeof(BlueprintSpellbook), typeof(BlueprintSpellList), typeof(BlueprintSpellList), typeof(BlueprintGuid) });
            cleanupMethod.Invoke(null, new object[] { uBook.Descriptor, shamanBookBp, spellList, shamanNativeList, mysteriesFeat.AssetGuid });
            cleanupMethod.Invoke(null, new object[] { uBook.Descriptor, shamanBookBp, spellList, shamanNativeList, mysteriesFeat.AssetGuid });
            Check(!book.IsKnown(longstrider) && book.IsKnown(heroism),
                "0046 D-0046-02 repeated cleanup stays idempotent and keeps valid overlaps intact");

            // ---- 5. Runtime Rulebook Tests: D-0046-03 Blood of Heroes ----
            var bohRev = spRevs.First(r => Sp046DisplayName(r) == "Blood of Heroes");
            var bohCloneBuff = Sp046CollectGraph(bohRev).OfType<BlueprintBuff>().Single();
            foreach (var pair in new[] { Tuple.Create(1, 1), Tuple.Create(7, 2), Tuple.Create(14, 3) })
            {
                int lv = pair.Item1, expected = pair.Item2;
                var uBoh = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "boh" + lv, Sp046ClassId, lv);
                uBoh.Progression.Classes[0].AddArchetype(root);
                uBoh.Stats.Wisdom.BaseValue = 10; uBoh.Stats.Dexterity.BaseValue = 10; uBoh.Stats.Constitution.BaseValue = 10;
                uBoh.Stats.SaveWill.BaseValue = 4; uBoh.Stats.SaveFortitude.BaseValue = 4; uBoh.Stats.SaveReflex.BaseValue = 4;
                var fearSource = new BlueprintAbility { AssetGuid = BlueprintGuid.NewGuid(), name = "TestFear" };
                fearSource.ComponentsArray = new BlueprintComponent[] { new SpellDescriptorComponent { Descriptor = SpellDescriptor.Fear } };
                fearSource.OnEnable();
                var fearCtx = new MechanicsContext(uBoh, uBoh.Descriptor, fearSource);

                Buff(uBoh, bohCloneBuff);
                Check(uBoh.Stats.AdditionalAttackBonus.ModifiedValue == expected && uBoh.Stats.AdditionalDamage.ModifiedValue == expected,
                    "0046 D-0046-03 Blood of Heroes attack & damage morale bonus == +" + expected + " at Shaman L" + lv);

                int baseWill = uBoh.Stats.SaveWill.ModifiedValue;
                int baseFort = uBoh.Stats.SaveFortitude.ModifiedValue;
                int baseRef = uBoh.Stats.SaveReflex.ModifiedValue;
                var fearWill = new RuleSavingThrow(uBoh, SavingThrowType.Will, 20);
                using (fearCtx.GetDataScope(uBoh)) Rulebook.Trigger(fearWill);
                Check(fearWill.StatValue == baseWill + expected,
                    "0046 D-0046-03 Fear Will save receives +" + expected + " morale bonus at Shaman L" + lv +
                    " (got=" + fearWill.StatValue + " baseWill=" + baseWill + " desc=" + fearCtx.SpellDescriptor + " reasonDesc=" + (fearWill.Reason.Context?.SpellDescriptor.ToString() ?? "NULL") + ")");

                var normalWill = Rulebook.Trigger(new RuleSavingThrow(uBoh, SavingThrowType.Will, 20));
                Check(normalWill.StatValue == baseWill, "0046 D-0046-03 ordinary Will save gets +0 (got=" + normalWill.StatValue + " base=" + baseWill + ")");

                var fearFort = new RuleSavingThrow(uBoh, SavingThrowType.Fortitude, 20);
                var fearRef = new RuleSavingThrow(uBoh, SavingThrowType.Reflex, 20);
                using (fearCtx.GetDataScope(uBoh))
                {
                    Rulebook.Trigger(fearFort);
                    Rulebook.Trigger(fearRef);
                }
                Check(fearFort.StatValue == baseFort && fearRef.StatValue == baseRef,
                    "0046 D-0046-03 Fear Fortitude & Reflex saves remain untouched (fort=" + fearFort.StatValue + "/" + baseFort + " ref=" + fearRef.StatValue + "/" + baseRef + " comps=" + string.Join(",", bohCloneBuff.ComponentsArray.Select(c => c.GetType().Name)) + ")");

                Buff(uBoh, bohCloneBuff);
                var fearWillRefresh = new RuleSavingThrow(uBoh, SavingThrowType.Will, 20);
                using (fearCtx.GetDataScope(uBoh)) Rulebook.Trigger(fearWillRefresh);
                Check(fearWillRefresh.StatValue == baseWill + expected && uBoh.Stats.AdditionalAttackBonus.ModifiedValue == expected,
                    "0046 D-0046-03 refreshing Blood of Heroes does not stack bonuses");
            }

            // ---- 6. Runtime Rulebook Tests: D-0046-04 Sacred Council ----
            var scRev = spRevs.First(r => Sp046DisplayName(r) == "Sacred Council");
            var scParent = Sp046CollectGraph(scRev).OfType<BlueprintAbility>().First(a => a.GetComponent<AbilityVariants>() != null);
            Check(scParent.GetComponent<AbilityVariants>().Variants.Length == 5,
                "0046 D-0046-04 Sacred Council ships 5 variants");
            var scClBuff = Sp046CollectGraph(scRev).OfType<BlueprintBuff>().Single(b => b.GetComponent<SpeakerCasterLevelChecks0046>() != null);

            var uSc = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "sc", Sp046ClassId, 12);
            uSc.Progression.Classes[0].AddArchetype(root);
            uSc.Stats.Wisdom.BaseValue = 18;
            var scTarget = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "sc-tgt", Sp046ClassId, 10);
            var scBook = new Spellbook(uSc.Descriptor, shamanBookBp);
            Set(scBook, "m_BaseLevelInternal", 12);
            Set(uSc.Descriptor, "m_Spellbooks", new Dictionary<BlueprintSpellbook, Spellbook> { { shamanBookBp, scBook } });
            var disintegrate = Native<BlueprintAbility>(c2, "4aa7942c3e62a164387a73184bca3fc1");
            var disintegrateData = new AbilityData(disintegrate, scBook, 7);

            var baseParams = Rulebook.Trigger(new RuleCalculateAbilityParams(uSc, disintegrateData));
            Check(baseParams.Result != null, "0046 D-0046-04 baseline RuleCalculateAbilityParams returns Result");
            int baseCl = baseParams.Result.CasterLevel;
            var baseCtx = new MechanicsContext(uSc, scTarget.Descriptor, disintegrate, null, scTarget);
            Sp046StopTrigger = true;
            var baseSrEvt = Rulebook.Trigger(new RuleSpellResistanceCheck(baseCtx, scTarget));
            int baseSrPen = (Get(baseSrEvt, "m_AdditionalSpellPenetration") as ModifiableBonus)?.TotalValue ?? 0;
            int baseDispel = Rulebook.Trigger(new RuleDispelMagic(uSc, scTarget, null, RuleDispelMagic.CheckType.CasterLevel, StatType.Wisdom, uSc)).Bonus;
            Sp046StopTrigger = false;

            Buff(uSc, scClBuff);
            int buffedCl = Rulebook.Trigger(new RuleCalculateAbilityParams(uSc, disintegrateData)).Result.CasterLevel;
            Sp046StopTrigger = true;
            var buffedSrEvt = Rulebook.Trigger(new RuleSpellResistanceCheck(baseCtx, scTarget));
            int buffedSrPen = (Get(buffedSrEvt, "m_AdditionalSpellPenetration") as ModifiableBonus)?.TotalValue ?? 0;
            int buffedDispelCl = Rulebook.Trigger(new RuleDispelMagic(uSc, scTarget, null, RuleDispelMagic.CheckType.CasterLevel, StatType.Wisdom, uSc)).Bonus;
            int buffedDispelDc = Rulebook.Trigger(new RuleDispelMagic(uSc, scTarget, null, RuleDispelMagic.CheckType.DC, StatType.Wisdom, uSc)).Bonus;
            Sp046StopTrigger = false;

            Check(buffedSrPen == baseSrPen + 2 && buffedDispelCl == baseDispel + 2 && buffedDispelDc == 2,
                "0046 D-0046-04 Sacred Council Caster Level Checks adds +2 to SR penetration and Dispel CL/DC checks");
            Check(buffedCl == baseCl && uSc.Stats.GetStat(StatType.BonusCasterLevel).ModifiedValue == 0,
                "0046 D-0046-04 general CasterLevel, BonusCasterLevel, damage dice & duration remain unchanged");

            // ---- 7. Spirit of the Warrior + Time Flicker Standard Action + Spirit Shield AC ----
            var warRev = spRevs.First(r => Sp046DisplayName(r) == "Spirit of the Warrior");
            var warToggle = Sp046CollectGraph(warRev).OfType<BlueprintActivatableAbility>().Single();
            var warBuff = Sp046CollectGraph(warRev).OfType<BlueprintBuff>().Single(b => b.GetComponent<SpeakerWarriorBab0046>() != null);
            Check(!warBuff.ComponentsArray.OfType<AddContextStatBonus>().Any(x => x.Stat == StatType.SaveFortitude) &&
                  !warBuff.ComponentsArray.OfType<AddStatBonus>().Any(x => x.Stat == StatType.SaveFortitude),
                "0046 Spirit of the Warrior removes any Fortitude competence bonus");

            var uWar = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "warrior", Sp046ClassId, 12);
            if (uWar.State == null) Set(uWar.Descriptor, "State", new UnitState(uWar.Descriptor));
            uWar.Progression.Classes[0].AddArchetype(root);
            uWar.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(c2, Sp046FighterClassId)) { Level = 4 });
            uWar.Stats.BaseAttackBonus.BaseValue = 9;
            Fact(uWar, warRev);
            var warRes = warRev.GetComponent<AddAbilityResources>().Resource;
            uWar.Resources.Restore(warRes, 12);
            var warAaFact = uWar.ActivatableAbilities.RawFacts.Single(a => a.Blueprint == warToggle);
            var arl = warToggle.GetComponent<ActivatableAbilityResourceLogic>();
            var reqRes = arl?.RequiredResource;
            Check(warAaFact.IsAvailable, "0046 Spirit of the Warrior toggle is initially available before TransformationBuff (res=" + uWar.Resources.GetResourceAmount(warRes) + "/" + warRes.GetMaxAmount(uWar) + " sameReq=" + (reqRes == warRes) + " hasEnough=" + (reqRes != null && uWar.Resources.HasEnoughResource(reqRes, 1)) + " byRes=" + warAaFact.IsAvailableByResources + " byRest=" + warAaFact.IsAvailableByRestrictions + " tmpDis=" + Sp046Member(warAaFact, "TemporarilyDisabled") + ")");
            var transformBuffBp = Native<BlueprintBuff>(c2, Sp046TransformBuffId);
            foreach (var fsc in transformBuffBp.ComponentsArray.OfType<ForbidSpellCasting>())
                if (Sp046Field(fsc, "m_IgnoreFeature") == null)
                    Set(fsc, "m_IgnoreFeature", new BlueprintFeatureReference());

            Buff(uWar, warBuff);
            Check(uWar.Stats.BaseAttackBonus.ModifiedValue == 12,
                "0046 Spirit of the Warrior grants Shaman-level BAB (12), ignoring unrelated multiclass dip levels");

            var tb = Buff(uWar, transformBuffBp);
            Check(!uWar.Buffs.HasFact(warBuff) && !warAaFact.IsOn,
                "0046 Spirit of the Warrior automatically turns off when TransformationBuff is applied");
            Check(!warAaFact.IsAvailable,
                "0046 Spirit of the Warrior activatable restriction forbids enabling while TransformationBuff is active");

            uWar.Buffs.RemoveFact(tb);
            Check(warAaFact.IsAvailable, "0046 Spirit of the Warrior becomes available again after TransformationBuff ends");

            var tfRev = spRevs.First(r => Sp046DisplayName(r) == "Time Flicker");
            var tfToggle = Sp046CollectGraph(tfRev).OfType<BlueprintActivatableAbility>().Single();
            Check((UnitCommand.CommandType)Sp046Field(tfToggle, "m_ActivateWithUnitCommand") == UnitCommand.CommandType.Standard,
                "0046 Speaker Time Flicker Displacement toggle uses Standard action");

            var sshRev = spRevs.First(r => Sp046DisplayName(r) == "Spirit Shield");
            var sshCloneBuff = Sp046CollectGraph(sshRev).OfType<BlueprintBuff>().Single(b => b.GetComponent<AddContextStatBonus>() != null);
            foreach (var tier in new[] { Tuple.Create(1, 4), Tuple.Create(7, 6), Tuple.Create(11, 8), Tuple.Create(15, 10), Tuple.Create(19, 12) })
            {
                var uAc = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "ssh" + tier.Item1, Sp046ClassId, tier.Item1);
                uAc.Progression.Classes[0].AddArchetype(root);
                uAc.Stats.AC.BaseValue = 10;
                int acBefore = uAc.Stats.AC.ModifiedValue;
                var sshFact = Buff(uAc, sshCloneBuff);
                int acAfter = uAc.Stats.AC.ModifiedValue;
                Check(acAfter == acBefore + tier.Item2 && acAfter == 10 + tier.Item2,
                    "0046 Spirit Shield Armor bonus == +" + tier.Item2 + " at Shaman L" + tier.Item1 +
                    " (before=" + acBefore + " after=" + acAfter +
                    " heal=" + sshFact.Context[Kingmaker.UnitLogic.Abilities.AbilitySharedValue.Heal] +
                    " dmg=" + sshFact.Context[Kingmaker.UnitLogic.Abilities.AbilitySharedValue.Damage] +
                    " d1x4=" + Rulebook.Trigger(new RuleRollDice(uAc, new DiceFormula(4, DiceType.One))).Result + ")");
            }
        }
        finally
        {
            Sp046DiceOverrideActive = false;
            Sp046StopTrigger = false;
            Sp046Dice.Clear();
            h.Unpatch(AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }),
                AccessTools.Method(typeof(Arcane01446Tests), nameof(Sp046DicePrefix)));
            h.Unpatch(AccessTools.Method(typeof(RuleSpellResistanceCheck), "OnTrigger"),
                AccessTools.Method(typeof(Arcane01446Tests), nameof(Sp046StopRulePrefix)));
            h.Unpatch(AccessTools.Method(typeof(RuleDispelMagic), "OnTrigger"),
                AccessTools.Method(typeof(Arcane01446Tests), nameof(Sp046StopRulePrefix)));
        }

        Console.WriteLine("PASS: " + (checks - start) + " Speaker for the Past 0046 assertions.");
        return checks - start;
    }

    static void ShamanClassArchetypeCount(BlueprintsCache c, out int count)
    {
        var cls = Native<BlueprintCharacterClass>(c, Sp046ClassId);
        var refs = (BlueprintArchetypeReference[])Sp046Field(cls, "m_Archetypes") ?? Array.Empty<BlueprintArchetypeReference>();
        count = refs.Count(r => r != null);
    }
}
