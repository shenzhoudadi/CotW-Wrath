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
        Sp046InjectSpell(c, "b924f40705afa57283f65679a2468775", "SpiritualWeapon");
        Sp046InjectSpell(c, "b406c2835f416afb0824cec610cba20a", "SpiritualAlly");
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

    static HashSet<BlueprintGuid> d4DlcBlueprints;
    static bool D4DlcForBattleSpell(BlueprintScriptableObject __0, ref bool __result)
    {
        if (__0 == null || !d4DlcBlueprints.Contains(__0.AssetGuid)) return true;
        __result = false;
        return false;
    }

    static int Speaker0046(BlueprintsCache parent, Harmony h)
    {
        int start = checks;
        h.CreateClassProcessor(typeof(SpeakerSpellbookSync0046).GetNestedType("EndHandoff", BindingFlags.NonPublic)).Patch();
        var timeModule = T("TimeMystery0046");
        var speakerModule = T("SpeakerForThePast0046");
        var binder = (GuidClassBinder)Kingmaker.Blueprints.JsonSystem.Json.Serializer.Binder;
        var allTypeIdAttrs = Main.GetTypes()
            .SelectMany(t => t.GetCustomAttributes(typeof(TypeIdAttribute), false)
                .Cast<TypeIdAttribute>()
                .Select(a => new { Type = t, Guid = a.GuidString }))
            .ToArray();
        var duplicateTypeIds = allTypeIdAttrs
            .GroupBy(x => x.Guid, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key + "=[" + string.Join(",", g.Select(x => x.Type.Name)) + "]")
            .ToArray();
        Check(duplicateTypeIds.Length == 0,
            "0046 D2-F01 assembly-wide TypeId scan has zero collisions: " + string.Join("; ", duplicateTypeIds));

        var type0046 = allTypeIdAttrs.Where(x => x.Type.Name.EndsWith("0046")).ToArray();
        Check(type0046.Length == 8, "0046 D2-F01 all 8 COTW-0046 custom runtime types carry [TypeId]");
        foreach (var entry in type0046)
            binder.AddToCache(entry.Type, entry.Guid);
        foreach (var entry in type0046)
        {
            binder.BindToName(entry.Type, out var boundAsm, out var boundType);
            Check(string.Equals(boundType, entry.Guid, StringComparison.OrdinalIgnoreCase) &&
                  binder.BindToType(boundAsm, boundType) == entry.Type,
                "0046 D2-F01 native GuidClassBinder round-trip failed for " + entry.Type.Name + " (" + entry.Guid + ")");
        }

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

            bool timeGate = (bool)Call(timeModule, "Register", c);
            Check(!timeGate && c.Load(Id(timeRootToken)) == null,
                "0046 D2-F02 TimeMystery Oracle expansion remains disabled per C01 2.7");
            bool speakerGate = (bool)Call(speakerModule, "Register", c);
            Check(!speakerGate && c.Load(Id(Sp046Root)) == null && c.Load(Id(Sp046Selection)) == null && c.Load(Id(Sp046SpellList)) == null,
                "0046 SpeakerForThePast Register returns false atomically without TTT-Base Sands of Time");
            ShamanClassArchetypeCount(c, out var archetypesAfter);
            Check(archetypesAfter == archetypesBefore && Sp046OracleAncestorsSnapshot(c) == oracleSnapBefore,
                "0046 rejection leaves ShamanClass.m_Archetypes and native Oracle Ancestors untouched");
            var spReject = (string)AccessTools.Field(speakerModule, "GateReport").GetValue(null);
            var spProvider = (string)AccessTools.Field(speakerModule, "ProviderReport").GetValue(null);
            Check(spReject.Contains("TabletopTweaks-Base") && spProvider.Contains(Sp046SandsId),
                "0046 rejection diagnostics name missing TTT-Base Sands of Time provider");

            Sp046InjectSandsProvider(c);
            Check((bool)Call(speakerModule, "Register", c), "0046 Speaker retry succeeds once Sands of Time arrives: " + AccessTools.Field(speakerModule, "GateReport").GetValue(null));
            Check((bool)Call(speakerModule, "Register", c),
                "0046 idempotent re-entry succeeds after late-provider retry");

            // ---- 2. Strict Oracle Isolation (C01 2.7) + 18 Frozen Bonus Spells (C01 2.8) + 16 Revelations ----
            var c2 = Fresh(parent);
            Sp046HydrateIsolatedHost(c2, speakerModule, includeSands: true);
            string SelectionSnapshot(BlueprintFeatureSelection fs)
                => fs == null ? "null" : string.Join(",", fs.AllFeatures.Select(x => x?.AssetGuid.ToString() ?? "")) + ";" +
                   string.Join(",", fs.Features.Select(x => x?.AssetGuid.ToString() ?? ""));

            var oracleMysterySelBefore = SelectionSnapshot(Native<BlueprintFeatureSelection>(c2, Sp046OracleMysterySelId));
            var secondMysteryBefore = SelectionSnapshot(Native<BlueprintFeatureSelection>(c2, Sp046SecondMysteryId));
            var oracleRevSelBefore = SelectionSnapshot(Native<BlueprintFeatureSelection>(c2, Sp046OracleRevSelId));
            var hermitSelBefore = SelectionSnapshot(c2.Load(BlueprintGuid.Parse("bc86ea452bef22a45989c6fa644bd2eb")) as BlueprintFeatureSelection);
            var spiritWalkBefore = Native<BlueprintFeature>(c2, "87bdea524911cfa4dacfe7243abe8b51");
            string spiritWalkSnapBefore = Sp046Signature(spiritWalkBefore) + "|" +
                string.Join(",", spiritWalkBefore.GetComponents<PrerequisiteFeaturesFromList>().SelectMany(p => p.Features).Select(x => x?.AssetGuid.ToString()));
            string oracleAncSnapBefore = Sp046OracleAncestorsSnapshot(c2);

            // Publish Speaker for the Past directly via C01 contract.
            Check((bool)Call(speakerModule, "Register", c2), "0046 SpeakerForThePast publishes cleanly");
            Check((bool)Call(speakerModule, "Register", c2), "0046 SpeakerForThePast re-entry is idempotent");
            Check(c2.Load(Id(timeRootToken)) == null,
                "0046 D2-F02 TimeOracleMysteryFeature0046 is not published into Oracle");
            Check(Sp046OracleAncestorsSnapshot(c2) == oracleAncSnapBefore &&
                  SelectionSnapshot(Native<BlueprintFeatureSelection>(c2, Sp046OracleMysterySelId)) == oracleMysterySelBefore &&
                  SelectionSnapshot(Native<BlueprintFeatureSelection>(c2, Sp046SecondMysteryId)) == secondMysteryBefore &&
                  SelectionSnapshot(Native<BlueprintFeatureSelection>(c2, Sp046OracleRevSelId)) == oracleRevSelBefore &&
                  SelectionSnapshot(c2.Load(BlueprintGuid.Parse("bc86ea452bef22a45989c6fa644bd2eb")) as BlueprintFeatureSelection) == hermitSelBefore,
                "0046 D2-F02 native Oracle mysteries, selections and Hermit selections remain untouched");
            string spiritWalkSnapAfter = Sp046Signature(spiritWalkBefore) + "|" +
                string.Join(",", spiritWalkBefore.GetComponents<PrerequisiteFeaturesFromList>().SelectMany(p => p.Features).Select(x => x?.AssetGuid.ToString()));
            Check(spiritWalkSnapAfter == spiritWalkSnapBefore &&
                  !spiritWalkBefore.GetComponents<PrerequisiteFeaturesFromList>().Any(p => p.Features.Any(m => m?.AssetGuid == BlueprintGuid.Parse(Sp046OracleMysteryId))),
                "0046 D2-F02 native OracleRevelationSpiritWalk prerequisites remain untouched");

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
            var frozenByCircle = new[] {
                new[] { "14c90900b690cac429b229efdf416127", "2c38da66e5a599347ac95b3294acbe00" }, // 1: Longstrider + True Strike
                new[] { "464a7193519429f48b4d190acb753cf0", "b924f40705afa57283f65679a2468775" }, // 2: Grace + Spiritual Weapon
                new[] { Sp046SandsId,                        "5ab0d42fb68c9e34abae4921822b9d63" }, // 3: Sands of Time + Heroism
                new[] { "503c3613f77e27d0a2650a09001b511f", "b406c2835f416afb0824cec610cba20a" }, // 4: Threefold Aspect + Spiritual Ally
                new[] { "41e8a952da7a5c247b3ec1c2dbb73018", "90810e5cf53bf854293cbd5ea1066252" }, // 5: Hold Monster + Righteous Might
                new[] { "b39dd2d96af348236b0117f57e5e445d", "e15e5e7045fda2244b98c8f010adfe31" }, // 6: Contingency + Heroism, Greater
                new[] { "4aa7942c3e62a164387a73184bca3fc1", "98310a099009bbd4dbdf66bcef58b4cd" }, // 7: Disintegrate + Invisibility, Mass
                new[] { "8b0761f3873aefb976c0f47fec3c600c", "0e67fa8f011662c43934d486acc50253" }, // 8: Temporal Stasis + Prediction of Failure
                new[] { "585f7776285d17b60b9e8e2fe42a69be", "43740dab07286fe4aa00a6ee104ce7c1" }, // 9: Time Stop + Heroic Invocation
            };
            for (int circle = 1; circle <= 9; circle++)
            {
                var actual = spellList.SpellsByLevel[circle].Spells.Select(s => s.AssetGuid).OrderBy(g => g.ToString()).ToArray();
                var expected = frozenByCircle[circle - 1].Select(BlueprintGuid.Parse).OrderBy(g => g.ToString()).ToArray();
                Check(actual.SequenceEqual(expected),
                    "0046 D2-F02 circle " + circle + " spells match frozen C01 2.8 GUIDs");
            }

            var spRevs = sel.AllFeatures.ToArray();
            Check(spRevs.Length == 16 && Sp046Names.All(n => spRevs.Any(r => Sp046DisplayName(r) == n)),
                "0046 RevelationPastSelection exposes all 16 Time + Ancestors revelations");
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

            // Check ModuleCatalog only exposes Speaker for the Past (not public Oracle Time Mystery).
            var catDefs = (Array)AccessTools.Field(T("ModuleCatalog"), "Definitions").GetValue(null);
            var catNames = catDefs.Cast<object>().Select(d => (string)Sp046Member(d, "Name")).ToArray();
            string timeModName = (string)AccessTools.Field(timeModule, "ModuleName").GetValue(null);
            string speakerModName = (string)AccessTools.Field(speakerModule, "ModuleName").GetValue(null);
            Check(!catNames.Contains(timeModName) && catNames.Contains(speakerModName),
                "0046 ModuleCatalog exposes Speaker for the Past and omits Oracle Time Mystery");

            // ---- 3. Runtime Rulebook Tests: D-0046-01 Rewind Time ----
            var rewindFeat = spRevs.First(r => Sp046DisplayName(r) == "Rewind Time");
            var rewindRes = rewindFeat.GetComponent<AddAbilityResources>().Resource;
            var rewindAllComps = Sp046CollectGraph(rewindFeat).OfType<BlueprintScriptableObject>().SelectMany(b => b.ComponentsArray).ToArray();
            var rewindRerolls = rewindAllComps.OfType<SpeakerFailedReroll0046>().ToArray();
            var rewindActiveBuff = (BlueprintBuff)rewindRerolls.Single(r => !r.SpendOnTrigger && r.ConsumeBuffOnTrigger).OwnerBlueprint;
            var rewindSaveToggle = rewindRerolls.Single(r => r.SpendOnTrigger && r.Rules.Length == 1 && r.Rules[0] == RuleType.SavingThrow);
            var rewindSaveBuff = (BlueprintBuff)rewindSaveToggle.OwnerBlueprint;
            var rewindInitToggle = rewindRerolls.Single(r => r.SpendOnTrigger && r.Rules.Length == 1 && r.Rules[0] == RuleType.Initiative);
            var rewindInitBuff = (BlueprintBuff)rewindInitToggle.OwnerBlueprint;
            var rewindSwiftAbility = Sp046CollectGraph(rewindFeat).OfType<BlueprintAbility>().Single(a => a.ActionType == UnitCommand.CommandType.Swift);

            var uRewind = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "rewind", Sp046ClassId, 14);
            uRewind.Progression.Classes[0].AddArchetype(root);
            uRewind.Stats.Wisdom.BaseValue = 18;
            uRewind.Stats.SaveWill.BaseValue = 5;
            Fact(uRewind, rewindFeat);
            uRewind.Resources.Restore(rewindRes, 10);

            // Active Swift cast: real AbilityResourceLogic.Spend deducts 1->0, then apply Active buff.
            uRewind.Resources.Spend(rewindRes, Math.Max(0, uRewind.Resources.GetResourceAmount(rewindRes) - 1));
            var rewindAbilityData = new AbilityData(rewindSwiftAbility, uRewind.Descriptor);
            var rewindSpendLogic = rewindSwiftAbility.GetComponent<AbilityResourceLogic>();
            Check(rewindSpendLogic != null && rewindSpendLogic.IsSpendResource && rewindSpendLogic.CalculateCost(rewindAbilityData) == 1,
                "0046 D-0046-01 Rewind Time swift ability carries real 1-use AbilityResourceLogic cost");
            rewindSpendLogic.Spend(rewindAbilityData);
            Check(uRewind.Resources.GetResourceAmount(rewindRes) == 0, "0046 D-0046-01 active cast AbilityResourceLogic.Spend deducts pool 1->0");
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

                // Initiative active & reaction branches (ModifyD20-aligned RuleInitiativeRoll).
                uRewind.Resources.Restore(rewindRes, 3);
                int beforeInitActive = uRewind.Resources.GetResourceAmount(rewindRes);
                var actInitBuffFact = Buff(uRewind, rewindActiveBuff);
                Sp046DiceRollsSeen = 0; Sp046Dice.Enqueue(3); Sp046Dice.Enqueue(17);
                var initActive = Rulebook.Trigger(new RuleInitiativeRoll(uRewind));
                Check(initActive.Result >= 17 && Sp046DiceRollsSeen == 2 &&
                      !uRewind.Buffs.HasFact(rewindActiveBuff) &&
                      uRewind.Resources.GetResourceAmount(rewindRes) == beforeInitActive,
                    "0046 D-0046-01 active Rewind buff rerolls RuleInitiativeRoll once, consumes buff and charges 0 on trigger");

                var initToggleFact = Buff(uRewind, rewindInitBuff);
                int beforeInitReaction = uRewind.Resources.GetResourceAmount(rewindRes);
                Sp046DiceRollsSeen = 0; Sp046Dice.Enqueue(4); Sp046Dice.Enqueue(16);
                var initReact = Rulebook.Trigger(new RuleInitiativeRoll(uRewind));
                Check(initReact.Result >= 16 && Sp046DiceRollsSeen == 2 &&
                      uRewind.Resources.GetResourceAmount(rewindRes) == beforeInitReaction - 1,
                    "0046 D-0046-01 Initiative reaction toggle rerolls RuleInitiativeRoll and spends 1 resource");

                Buff(uRewind, rewindActiveBuff);
                int beforeInitSim = uRewind.Resources.GetResourceAmount(rewindRes);
                Sp046DiceRollsSeen = 0; Sp046Dice.Enqueue(2); Sp046Dice.Enqueue(19);
                var initSim = Rulebook.Trigger(new RuleInitiativeRoll(uRewind));
                Check(initSim.Result >= 19 && Sp046DiceRollsSeen == 2 &&
                      !uRewind.Buffs.HasFact(rewindActiveBuff) &&
                      uRewind.Resources.GetResourceAmount(rewindRes) == beforeInitSim,
                    "0046 D-0046-01 simultaneous Active + Initiative toggle rerolls once with 0 trigger charge");
                initToggleFact.Remove();
            }
            finally
            {
                Sp046DiceOverrideActive = false;
                Sp046Dice.Clear();
            }

            // ---- 4. Runtime Spellbook Tests: D-0046-02 Bonus Spellbook Cleanup & D2-F04 Temp Known Interop ----
            var shamanCls = Native<BlueprintCharacterClass>(c2, Sp046ClassId);
            var shamanBookBp = Native<BlueprintSpellbook>(c2, Sp046ShamanSpellbookId);
            var shamanNativeList = Native<BlueprintSpellList>(c2, Sp046ShamanListId);
            var heroism = Native<BlueprintAbility>(c2, "5ab0d42fb68c9e34abae4921822b9d63"); // circle 3 Speaker spell
            var longstrider = Native<BlueprintAbility>(c2, "14c90900b690cac429b229efdf416127"); // circle 1 Speaker spell
            var spiritualAlly = Native<BlueprintAbility>(c2, "b406c2835f416afb0824cec610cba20a"); // circle 4 Speaker spell (C01 2.8)

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
            Check(book.IsKnown(longstrider) && book.IsKnown(heroism) && book.IsKnown(spiritualAlly),
                "0046 D-0046-02 at Shaman L10 Speaker circles 1..5 are known on primary spellbook");
            Check(book.SpecialLists.Count == 0, "0046 D-0046-02 zero spells leak into spirit magic SpecialLists");

            // De-level 10 -> 6 (drops 4th/5th circle coverage): Spiritual Ally removed, L1..3 + temp preserved!
            Set(book, "m_BaseLevelInternal", 6);
            using (syncRt.RequestEventContext()) ((SpeakerSpellbookSync0046)syncRt.SourceBlueprintComponent).HandleUnitGainLevel();
            Check(!book.IsKnown(spiritualAlly) && book.IsKnown(longstrider) && book.IsKnown(heroism),
                "0046 D-0046-02 de-level removes uncovered Speaker circle-4 exclusive spell (Spiritual Ally) while keeping L1..3");
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

            // D2-F04: UnitPartTemporarySpellsKnown multi-source interop (BOTH acquisition orders!)
            var foreignTempComp = new Kingmaker.UnitLogic.FactLogic.AddSpellKnownTemporary { name = "ForeignTempSpiritualAlly", Level = 4, OnlySpontaneous = false };
            Set(foreignTempComp, "m_CharacterClass", Sp046Ref<BlueprintCharacterClassReference>(shamanCls));
            Set(foreignTempComp, "m_Spell", Sp046Ref<BlueprintAbilityReference>(spiritualAlly));
            var foreignTempFeat = new BlueprintFeature {
                AssetGuid = BlueprintGuid.Parse("0046a1b2c3d4e5f600000000000000fd"),
                name = "Speaker0046TestForeignTemp",
                ComponentsArray = new BlueprintComponent[] { foreignTempComp }
            };

            // Order A: Foreign AddSpellKnownTemporary -> Speaker -> Remove Speaker -> Remove Foreign
            var uOrdA = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "tempOrdA", Sp046ClassId, 10);
            uOrdA.Progression.Classes[0].AddArchetype(root);
            uOrdA.Stats.Wisdom.BaseValue = 18;
            var bookA = new Spellbook(uOrdA.Descriptor, shamanBookBp);
            Set(bookA, "m_BaseLevelInternal", 10);
            bookA.UpdateAllSlotsSize(false);
            var sureMemSlots = AccessTools.Method(typeof(Spellbook), "SureMemorizedSpells");
            var slotsA4 = (List<SpellSlot>)sureMemSlots.Invoke(bookA, new object[] { 4 });
            if (!slotsA4.Any(s => s.Type == SpellSlotType.Common))
                slotsA4.Add(new SpellSlot(4, SpellSlotType.Common, 0, bookA));
            Set(uOrdA.Descriptor, "m_Spellbooks", new Dictionary<BlueprintSpellbook, Spellbook> { { shamanBookBp, bookA } });
            var ftFactA = Fact(uOrdA, foreignTempFeat);
            var knownA0 = bookA.GetKnownSpells(4).FirstOrDefault(s => s.Blueprint == spiritualAlly);
            Check(knownA0 != null && knownA0.IsTemporary, "0046 D2-F04 OrderA foreign temporary source teaches circle-4 Spiritual Ally as temporary");
            Check(bookA.Memorize(knownA0), "0046 D2-F04 OrderA prepares Spiritual Ally into L4 common slot before Speaker");
            var mystFactA = Fact(uOrdA, mysteriesFeat);
            Check(bookA.GetKnownSpells(4).Any(s => s.Blueprint == spiritualAlly && !s.IsTemporary),
                "0046 D2-F04 OrderA Speaker acquisition promotes shared circle-4 spell to non-temporary while active");
            uOrdA.Facts.Remove(mystFactA);
            var knownAfterSpRmA = bookA.GetKnownSpells(4).FirstOrDefault(s => s.Blueprint == spiritualAlly);
            int memCountA = bookA.GetMemorizedSpells(4).Count(s => s.SpellShell?.Blueprint == spiritualAlly);
            Check(knownAfterSpRmA != null && knownAfterSpRmA.IsTemporary && memCountA > 0,
                "0046 D2-F04 OrderA removing Speaker preserves foreign temporary Spiritual Ally, common-slot preparation, and restores IsTemporary=true (known=" + (knownAfterSpRmA != null) + " isTemp=" + (knownAfterSpRmA?.IsTemporary ?? false) + " mem=" + memCountA + ")");
            uOrdA.Facts.Remove(ftFactA);
            Check(!bookA.IsKnown(spiritualAlly),
                "0046 D2-F04 OrderA removing final foreign temporary source cleanly unlearns Spiritual Ally");

            // Order B: Speaker -> Foreign AddSpellKnownTemporary -> Remove Speaker -> Remove Foreign (plus reverse Foreign-first removal)
            var uOrdB = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "tempOrdB", Sp046ClassId, 10);
            uOrdB.Progression.Classes[0].AddArchetype(root);
            uOrdB.Stats.Wisdom.BaseValue = 18;
            var bookB = new Spellbook(uOrdB.Descriptor, shamanBookBp);
            Set(bookB, "m_BaseLevelInternal", 10);
            bookB.UpdateAllSlotsSize(false);
            var slotsB4 = (List<SpellSlot>)sureMemSlots.Invoke(bookB, new object[] { 4 });
            if (!slotsB4.Any(s => s.Type == SpellSlotType.Common))
                slotsB4.Add(new SpellSlot(4, SpellSlotType.Common, 0, bookB));
            Set(uOrdB.Descriptor, "m_Spellbooks", new Dictionary<BlueprintSpellbook, Spellbook> { { shamanBookBp, bookB } });
            var mystFactB = Fact(uOrdB, mysteriesFeat);
            var knownB0 = bookB.GetKnownSpells(4).FirstOrDefault(s => s.Blueprint == spiritualAlly);
            Check(knownB0 != null && bookB.Memorize(knownB0), "0046 D2-F04 OrderB prepares Speaker Spiritual Ally into L4 common slot before Foreign temp");
            var ftFactB = Fact(uOrdB, foreignTempFeat);
            uOrdB.Facts.Remove(mystFactB);
            var knownAfterSpRmB = bookB.GetKnownSpells(4).FirstOrDefault(s => s.Blueprint == spiritualAlly);
            int memCountB = bookB.GetMemorizedSpells(4).Count(s => s.SpellShell?.Blueprint == spiritualAlly);
            Check(knownAfterSpRmB != null && knownAfterSpRmB.IsTemporary && memCountB > 0,
                "0046 D2-F04 OrderB removing Speaker preserves later foreign AddSpellKnownTemporary Spiritual Ally & common-slot preparation (known=" + (knownAfterSpRmB != null) + " isTemp=" + (knownAfterSpRmB?.IsTemporary ?? false) + " mem=" + memCountB + ")");
            uOrdB.Facts.Remove(ftFactB);
            Check(!bookB.IsKnown(spiritualAlly),
                "0046 D2-F04 OrderB removing remaining foreign temporary source cleanly unlearns Spiritual Ally");

            var uOrdB2 = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "tempOrdB2", Sp046ClassId, 10);
            uOrdB2.Progression.Classes[0].AddArchetype(root);
            var bookB2 = new Spellbook(uOrdB2.Descriptor, shamanBookBp);
            Set(bookB2, "m_BaseLevelInternal", 10);
            Set(uOrdB2.Descriptor, "m_Spellbooks", new Dictionary<BlueprintSpellbook, Spellbook> { { shamanBookBp, bookB2 } });
            var mystFactB2 = Fact(uOrdB2, mysteriesFeat);
            var ftFactB2 = Fact(uOrdB2, foreignTempFeat);
            uOrdB2.Facts.Remove(ftFactB2);
            var knownAfterFtRmB2 = bookB2.GetKnownSpells(4).FirstOrDefault(s => s.Blueprint == spiritualAlly);
            Check(knownAfterFtRmB2 != null && !knownAfterFtRmB2.IsTemporary && bookB2.IsKnown(longstrider),
                "0046 D2-F04 OrderB2 removing foreign temporary source while Speaker stays active preserves non-temporary Spiritual Ally and other circles");

            // D3-F01: the same spell can be granted independently on a different circle.
            foreach (bool lowerLevel in new[] { false, true })
            foreach (bool temporary in new[] { false, true })
            {
                string scenario = $"0046 D3-F01 {(lowerLevel ? "level loss" : "feature removal")}, {(temporary ? "temporary" : "permanent")} overlap";
                int foreignCircle = lowerLevel ? 3 : 5;
                var unit = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, scenario, Sp046ClassId, 10);
                unit.Progression.Classes[0].AddArchetype(root);
                unit.Stats.Wisdom.BaseValue = 22;
                var crossBook = new Spellbook(unit.Descriptor, shamanBookBp);
                Set(crossBook, "m_BaseLevelInternal", 10);
                crossBook.UpdateAllSlotsSize(false);
                foreach (int circle in new[] { 4, 5, foreignCircle }.Distinct())
                {
                    var slots = (List<SpellSlot>)sureMemSlots.Invoke(crossBook, new object[] { circle });
                    if (!slots.Any(slot => slot.Type == SpellSlotType.Common))
                        slots.Add(new SpellSlot(circle, SpellSlotType.Common, 0, crossBook));
                }
                if (foreignCircle == 5)
                {
                    var slots = (List<SpellSlot>)sureMemSlots.Invoke(crossBook, new object[] { 5 });
                    if (slots.Count(slot => slot.Type == SpellSlotType.Common) < 2)
                        slots.Add(new SpellSlot(5, SpellSlotType.Common, slots.Count, crossBook));
                }
                Set(unit.Descriptor, "m_Spellbooks", new Dictionary<BlueprintSpellbook, Spellbook> { { shamanBookBp, crossBook } });

                var speakerFact = Fact(unit, mysteriesFeat);
                var ownSpell = crossBook.GetKnownSpells(4).SingleOrDefault(s => s.Blueprint == spiritualAlly);
                Check(ownSpell != null && crossBook.Memorize(ownSpell), scenario + " prepares Speaker circle 4");
                var metamagic = new MetamagicData { SpellLevelCost = 1 };
                metamagic.Add(Metamagic.Extend);
                var ownCustom = new AbilityData(spiritualAlly, crossBook, 4) { MetamagicData = metamagic };
                crossBook.AddCustomSpell(ownCustom);
                Check(crossBook.Memorize(ownCustom), scenario + " prepares the Speaker metamagic version in circle 5");

                BlueprintComponent grant = temporary
                    ? (BlueprintComponent)new AddSpellKnownTemporary { name = "D3Temporary", Level = foreignCircle, OnlySpontaneous = false }
                    : new AddKnownSpell { name = "D3Permanent", SpellLevel = foreignCircle };
                Set(grant, "m_CharacterClass", Sp046Ref<BlueprintCharacterClassReference>(shamanCls));
                Set(grant, "m_Spell", Sp046Ref<BlueprintAbilityReference>(spiritualAlly));
                var independentFact = Fact(unit, new BlueprintFeature {
                    AssetGuid = BlueprintGuid.Parse(temporary ? "0046a1b2c3d4e5f600000000000000fa" : "0046a1b2c3d4e5f600000000000000fb"),
                    name = temporary ? "Speaker0046D3Temporary" : "Speaker0046D3Permanent",
                    ComponentsArray = new[] { grant }
                });
                var otherSpell = crossBook.GetKnownSpells(foreignCircle).SingleOrDefault(s => s.Blueprint == spiritualAlly);
                Check(otherSpell != null && otherSpell.IsTemporary == temporary && crossBook.Memorize(otherSpell),
                    scenario + " prepares the independent version");

                if (lowerLevel)
                {
                    Set(crossBook, "m_BaseLevelInternal", 6);
                    var runtime = speakerFact.Components.Single(rc => rc.SourceBlueprintComponent is SpeakerSpellbookSync0046);
                    using (runtime.RequestEventContext())
                        ((SpeakerSpellbookSync0046)runtime.SourceBlueprintComponent).HandleUnitGainLevel();
                }
                else
                    unit.Facts.Remove(speakerFact);

                Check(!crossBook.IsKnownOnLevel(spiritualAlly, 4) &&
                      !crossBook.GetKnownSpells(4).Any(s => s.Blueprint == spiritualAlly),
                    scenario + " forgets only the Speaker circle");
                Check(!crossBook.GetMemorizedSpellSlots(spiritualAlly).Any(s => s.SpellShell.SpellLevelInSpellbook == 4) &&
                      !crossBook.GetCustomSpells(spiritualAlly).Any(s => s.SpellLevelInSpellbook == 4),
                    scenario + " clears the Speaker preparations and metamagic copy");
                Check(crossBook.IsKnownOnLevel(spiritualAlly, foreignCircle) &&
                      crossBook.GetMemorizedSpells(foreignCircle).Any(s => ReferenceEquals(s.SpellShell, otherSpell)),
                    scenario + " keeps the independent circle and its exact preparation");
                if (temporary)
                {
                    unit.Facts.Remove(independentFact);
                    Check(!crossBook.IsKnown(spiritualAlly) && crossBook.GetMemorizedSpellSlots(spiritualAlly).Count == 0,
                        scenario + " native ledger removes the final temporary grant and its preparation");
                }
            }

            // The spellbook preserves prepared spells in its hidden slots when slot count shrinks.
            var restoreUnit = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "D4-hidden-slot", Sp046ClassId, 10);
            restoreUnit.Progression.Classes[0].AddArchetype(root);
            restoreUnit.Stats.Wisdom.BaseValue = 22;
            var restoreBook = new Spellbook(restoreUnit.Descriptor, shamanBookBp);
            Set(restoreBook, "m_BaseLevelInternal", 10);
            Set(restoreUnit.Descriptor, "m_Spellbooks", new Dictionary<BlueprintSpellbook, Spellbook> { { shamanBookBp, restoreBook } });
            restoreBook.UpdateAllSlotsSize(false);
            var restoreFact = Fact(restoreUnit, mysteriesFeat);
            var originalKnown = restoreBook.GetKnownSpells(4).Single(s => s.Blueprint == spiritualAlly);
            Check(restoreBook.Memorize(originalKnown), "D4 setup: native preparation of Speaker's four-circle spell");
            var originalSlot = restoreBook.GetMemorizedSpells(4).Single(s => ReferenceEquals(s.SpellShell, originalKnown));
            var reduced = new AbilityData(spiritualAlly, restoreBook, 4)
            {
                MetamagicData = new MetamagicData { SpellLevelCost = -1 }
            };
            restoreBook.AddCustomSpell(reduced);
            Check(restoreBook.Memorize(reduced), "D4 setup: prepare a reduced-cost version in circle 3");
            // Lower Wisdom so the native Resize can hide slots while the higher circles are still visited.
            restoreUnit.Stats.Wisdom.BaseValue = 10;
            restoreBook.UpdateAllSlotsSize(true);
            Check(restoreBook.GetHiddenCommonSpells(4).Contains(originalSlot) &&
                  !restoreBook.GetMemorizedSpells(4).Any(s => s.SpellShell?.Blueprint == spiritualAlly),
                  "D4 setup: native UpdateAllSlotsSize moved the prepared spell into a hidden slot");

            Set(restoreBook, "m_BaseLevelInternal", 6);
            var restoreRuntime = restoreFact.Components.Single(rc => rc.SourceBlueprintComponent is SpeakerSpellbookSync0046);
            using (restoreRuntime.RequestEventContext())
                ((SpeakerSpellbookSync0046)restoreRuntime.SourceBlueprintComponent).HandleUnitGainLevel();
            Check(!restoreBook.IsKnownOnLevel(spiritualAlly, 4), "D4 level loss: Speaker spell leaves known circle 4");
            bool hiddenAfterLevelLoss = restoreBook.GetHiddenCommonSpells(4).Any(s => s.SpellShell?.Blueprint == spiritualAlly);
            restoreUnit.Facts.Remove(restoreFact);
            bool hiddenAfterRemoval = restoreBook.GetHiddenCommonSpells(4).Any(s => s.SpellShell?.Blueprint == spiritualAlly);

            Set(restoreBook, "m_BaseLevelInternal", 10);
            restoreUnit.Stats.Wisdom.BaseValue = 22;
            restoreBook.Rest();
            bool returnedWithoutSource = restoreBook.GetMemorizedSpells(4).Any(s => s.SpellShell?.Blueprint == spiritualAlly);
            Check(!restoreBook.IsKnownOnLevel(spiritualAlly, 4), "D4 setup: returned slot without any learn source");
            Check(!hiddenAfterLevelLoss && !hiddenAfterRemoval && !returnedWithoutSource, "D4-F01: hidden preparation is cleared and stays absent after slot restoration");
            Check(!restoreBook.GetMemorizedSpells(3).Any(s => ReferenceEquals(s.SpellShell, reduced)) &&
                  !restoreBook.GetHiddenCommonSpells(3).Any(s => ReferenceEquals(s.SpellShell, reduced)),
                  "D4-F01: a lower-slot metamagic version is also cleared");

            var newRestoreFact = Fact(restoreUnit, mysteriesFeat);
            var restoredKnown = restoreBook.GetKnownSpells(4).SingleOrDefault(s => s.Blueprint == spiritualAlly);
            Check(restoredKnown != null && !ReferenceEquals(restoredKnown, originalKnown), "D4 relearn: reacquiring Mysteries teaches a fresh level-four spell");
            restoreUnit.Facts.Remove(newRestoreFact);


            // If another source shares the base circle, cleanup must wait until the last source ends.
            var sharedUnit = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "D4-shared-metamagic", Sp046ClassId, 10);
            sharedUnit.Progression.Classes[0].AddArchetype(root);
            sharedUnit.Stats.Wisdom.BaseValue = 22;
            var sharedBook = new Spellbook(sharedUnit.Descriptor, shamanBookBp);
            Set(sharedBook, "m_BaseLevelInternal", 10);
            Set(sharedUnit.Descriptor, "m_Spellbooks", new Dictionary<BlueprintSpellbook, Spellbook> { { shamanBookBp, sharedBook } });
            sharedBook.UpdateAllSlotsSize(false);
            var sharedSpeaker = Fact(sharedUnit, mysteriesFeat);
            var sharedBase = sharedBook.GetKnownSpells(4).Single(s => s.Blueprint == spiritualAlly);
            var sharedMeta = new MetamagicData { SpellLevelCost = 1 };
            sharedMeta.Add(Metamagic.Extend);
            var sharedCustom = new AbilityData(spiritualAlly, sharedBook, 4) { MetamagicData = sharedMeta };
            sharedBook.AddCustomSpell(sharedCustom);
            Check(sharedBook.Memorize(sharedCustom), "D4 setup: prepare Speaker metamagic copy before sharing");
            var sharedTempSource = Fact(sharedUnit, foreignTempFeat);
            sharedUnit.Facts.Remove(sharedSpeaker);
            Check(sharedBase.IsTemporary && sharedBook.IsKnownOnLevel(spiritualAlly, 4) &&
                  sharedBook.GetMemorizedSpells(5).Any(s => ReferenceEquals(s.SpellShell, sharedCustom)),
                "D4: active same-circle temporary source preserves the shared base and preparation");
            sharedUnit.Facts.Remove(sharedTempSource);
            Check(!sharedBook.IsKnownOnLevel(spiritualAlly, 4), "D4: native last-source removal forgets the base spell");
            sharedBook.Rest();
            bool sharedPreparationRemains = sharedBook.GetMemorizedSpells(5).Any(slot => ReferenceEquals(slot.SpellShell, sharedCustom));
            bool sharedCustomRemains = sharedBook.GetCustomSpells(spiritualAlly).Any(ability => ReferenceEquals(ability, sharedCustom));
            bool canSpendSharedPreparation = sharedBook.CanSpend(sharedCustom);
            Check(!sharedPreparationRemains && !sharedCustomRemains && !canSpendSharedPreparation, "D4-F02: last temporary source clears the metamagic preparation and prevents spending");

            // The real Battle Spirit list and its AddSpecialSpellList feature come from the game's archived rows.
            // Restrict this boundary stub to the archive rows hydrated below. No spellbook logic is patched.
            var dlcType = typeof(Kingmaker.DLC.DlcConditionExtension);
            var dlcMethods = typeof(Arcane01446Tests);
            string[] battleAssetIds = {
                "90810e5cf53bf854293cbd5ea1066252", "c60969e7f264e6d4b84a1499fdcf9039", "4c3d08935262b6544ae97599b3a9556d",
                "2d4263d80f5136b4296d6eb43a221d7d", "2daf9c5112f16d54ab3cd6904c705c59", "6a234c6dcde7ae94e94e9c36fd1163a7",
                "e3d0dfe1c8527934294f241e0ae96a8d", "e788b02f8d21014488067bdd3ba7b325", "22b9044aa229815429d57d0a30e4b739"
            };
            d4DlcBlueprints = new HashSet<BlueprintGuid>(battleAssetIds.Select(BlueprintGuid.Parse));
            h.Patch(AccessTools.Method(dlcType, "IsDlcRestricted"),
                prefix: new HarmonyMethod(AccessTools.Method(dlcMethods, nameof(D4DlcForBattleSpell))));
            var battleRows = new Newtonsoft.Json.Linq.JArray(PsychicExpansionTests.NativeRows.Where(row =>
            {
                string id = (string)row["blueprint"]["AssetId"];
                return id == "e702a1335e1f66648afbe3e2ac182751" || id == "e788846aaf449404db11e51182174be8" ||
                    id == Sp046ShamanSpellbookId || d4DlcBlueprints.Contains(BlueprintGuid.Parse(id));
            }));
            PsychicExpansionTests.LoadNativeRows(c2, new Newtonsoft.Json.Linq.JObject { ["requiredNative"] = battleRows }, true);
            var battleGrant = Native<BlueprintFeature>(c2, "e702a1335e1f66648afbe3e2ac182751");
            var battleList = Native<BlueprintSpellList>(c2, "e788846aaf449404db11e51182174be8");
            var righteous = Native<BlueprintAbility>(c2, "90810e5cf53bf854293cbd5ea1066252");
            Check(battleList.GetSpells(5).Contains(righteous) &&
                  !shamanNativeList.SpellsByLevel[5].Spells.Contains(righteous) &&
                  battleGrant.GetComponent<AddSpecialSpellList>()?.SpellList == battleList,
                "D4 Battle Spirit fixture verifies the three real blueprint identities and a true overlap");

            var archivedShamanBook = battleRows.Single(row => (string)row["blueprint"]["AssetId"] == Sp046ShamanSpellbookId);
            shamanBookBp.AllSpellsKnown = (bool)archivedShamanBook["blueprint"]["Data"]["AllSpellsKnown"];
            Check(shamanBookBp.AllSpellsKnown, "D4 Battle Spirit fixture restores the archived Shaman AllSpellsKnown flag");
            foreach (bool spiritFirst in new[] { false, true })
            {
                var unit = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "D4-BattleSpirit-" + spiritFirst, Sp046ClassId, 10);
                unit.Progression.Classes[0].AddArchetype(root);
                unit.Stats.Wisdom.BaseValue = 22;
                var battleBook = new Spellbook(unit.Descriptor, shamanBookBp);
                Set(battleBook, "m_BaseLevelInternal", 10);
                Set(unit.Descriptor, "m_Spellbooks", new Dictionary<BlueprintSpellbook, Spellbook> { { shamanBookBp, battleBook } });
                if (spiritFirst) Fact(unit, battleGrant);
                var speaker = Fact(unit, mysteriesFeat);
                if (!spiritFirst) Fact(unit, battleGrant);
                battleBook.UpdateAllSlotsSize(false);
                var ordinary = battleBook.GetKnownSpells(5).FirstOrDefault(s => s.Blueprint == righteous);
                var spirit = battleBook.GetSpecialSpells(5).FirstOrDefault(s => s.Blueprint == righteous);
                Check(spirit != null && battleBook.SpecialLists.Contains(battleList),
                    "D4 Battle Spirit fixture: real AddSpecialSpellList added the five-circle spirit grant");
                Check(ordinary != null && !ReferenceEquals(ordinary, spirit), "D4 Battle Spirit: ordinary and spirit copies are distinct");
                var favorite = battleBook.GetMemorizedSpellSlots(5).First(s => s.Type == SpellSlotType.Favorite);
                var common = battleBook.GetMemorizedSpellSlots(5).First(s => s.Type == SpellSlotType.Common);
                Check(battleBook.Memorize(spirit, favorite) && battleBook.Memorize(ordinary, common),
                    "D4 Battle Spirit: native Memorize prepares the two distinct slot types");
                unit.Facts.Remove(speaker);
                bool commonKnown = battleBook.GetKnownSpells(5).Any(s => s.Blueprint == righteous);
                bool spiritKnown = battleBook.GetSpecialSpells(5).Any(s => s.Blueprint == righteous);
                bool spiritPrepared = ReferenceEquals(favorite.SpellShell, spirit);
                Check(!commonKnown && common.SpellShell == null && spiritKnown && spiritPrepared &&
                      battleBook.IsKnownOnLevel(righteous, 5) && battleBook.SpecialLists.Contains(battleList),
                    "D4-F03: only the ordinary Spirit overlap is removed, in either acquisition order");
            }

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
            int baseDispelNone = Rulebook.Trigger(new RuleDispelMagic(uSc, scTarget, null, RuleDispelMagic.CheckType.None, StatType.Wisdom, uSc)).Bonus;
            int baseEnemyDispelCl = Rulebook.Trigger(new RuleDispelMagic(scTarget, uSc, null, RuleDispelMagic.CheckType.CasterLevel, StatType.Wisdom, scTarget)).Bonus;
            int baseEnemyDispelDc = Rulebook.Trigger(new RuleDispelMagic(scTarget, uSc, null, RuleDispelMagic.CheckType.DC, StatType.Wisdom, scTarget)).Bonus;
            Sp046StopTrigger = false;

            Buff(uSc, scClBuff);
            int buffedCl = Rulebook.Trigger(new RuleCalculateAbilityParams(uSc, disintegrateData)).Result.CasterLevel;
            Sp046StopTrigger = true;
            var buffedSrEvt = Rulebook.Trigger(new RuleSpellResistanceCheck(baseCtx, scTarget));
            int buffedSrPen = (Get(buffedSrEvt, "m_AdditionalSpellPenetration") as ModifiableBonus)?.TotalValue ?? 0;
            int buffedDispelCl = Rulebook.Trigger(new RuleDispelMagic(uSc, scTarget, null, RuleDispelMagic.CheckType.CasterLevel, StatType.Wisdom, uSc)).Bonus;
            int buffedDispelDc = Rulebook.Trigger(new RuleDispelMagic(uSc, scTarget, null, RuleDispelMagic.CheckType.DC, StatType.Wisdom, uSc)).Bonus;
            int buffedDispelNone = Rulebook.Trigger(new RuleDispelMagic(uSc, scTarget, null, RuleDispelMagic.CheckType.None, StatType.Wisdom, uSc)).Bonus;
            int buffedEnemyDispelCl = Rulebook.Trigger(new RuleDispelMagic(scTarget, uSc, null, RuleDispelMagic.CheckType.CasterLevel, StatType.Wisdom, scTarget)).Bonus;
            int buffedEnemyDispelDc = Rulebook.Trigger(new RuleDispelMagic(scTarget, uSc, null, RuleDispelMagic.CheckType.DC, StatType.Wisdom, scTarget)).Bonus;
            Sp046StopTrigger = false;

            Check(buffedSrPen == baseSrPen + 2 && buffedDispelCl == baseDispel + 2 && buffedDispelDc == 2,
                "0046 D-0046-04 Sacred Council Caster Level Checks adds +2 to SR penetration and Dispel CL/DC checks");
            Check(buffedDispelNone == baseDispelNone && buffedDispelNone == 0 &&
                  buffedEnemyDispelCl == baseEnemyDispelCl && buffedEnemyDispelDc == baseEnemyDispelDc,
                "0046 D-0046-04 negative controls: CheckType.None (+0) and incoming enemy RuleDispelMagic against buffed unit are unchanged");
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

            // ---- 8. D2-F03 Spirit Walk + Temporal Celerity + Erase from Time (Multiclass Isolation) ----
            var fighterCls = Native<BlueprintCharacterClass>(c2, Sp046FighterClassId);

            // 8a. Spirit Walk
            var spwRev = spRevs.First(r => Sp046DisplayName(r) == "Spirit Walk");
            var spwPrereq = spwRev.GetComponents<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteArchetypeLevel>().SingleOrDefault();
            Check(spwPrereq != null && spwPrereq.Level == 11 && spwPrereq.Archetype == root && spwPrereq.CharacterClass == shamanCls,
                "0046 D2-F03 Spirit Walk requires Shaman Speaker archetype level 11");
            var spwRes = spwRev.GetComponents<AddAbilityResources>().Single().Resource;
            var spwToggleBp = Sp046CollectGraph(spwRev).OfType<BlueprintActivatableAbility>().Single();
            var spwResLogic = spwToggleBp.GetComponent<Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityResourceLogic>();
            var spwBuffBp = spwToggleBp.Buff;
            var spwInvis = spwBuffBp.GetComponent<Kingmaker.Designers.Mechanics.Buffs.BuffInvisibility>();
            var spwCantAct = spwBuffBp.GetComponents<Kingmaker.UnitLogic.FactLogic.AddCondition>()
                .Any(ac => ac.Condition == Kingmaker.UnitLogic.UnitCondition.CantAct);
            var spwIncFact = Native<BlueprintUnitFact>(c2, "c4a7f98d743bc784c9d4cf2105852c39");
            var spwGrantsInc = spwBuffBp.GetComponents<Kingmaker.UnitLogic.FactLogic.AddFacts>()
                .Any(af => af.Facts.Contains(spwIncFact));
            Check((UnitCommand.CommandType)Sp046Field(spwToggleBp, "m_ActivateWithUnitCommand") == UnitCommand.CommandType.Standard &&
                  spwResLogic != null && spwResLogic.RequiredResource == spwRes &&
                  spwResLogic.SpendType == Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityResourceLogic.ResourceSpendType.NewRound,
                "0046 D2-F03 Spirit Walk uses Standard action toggle with ResourceSpendType.NewRound");
            Check(spwInvis != null && spwInvis.NotDispellAfterOffensiveAction && spwCantAct && spwGrantsInc,
                "0046 D2-F03 Spirit Walk buff grants Incorporeal, BuffInvisibility(NotDispellAfterOffensiveAction), and CantAct");

            var uSpw11 = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "spw11", Sp046ClassId, 11);
            uSpw11.Progression.Classes[0].AddArchetype(root);
            uSpw11.Progression.Classes.Add(new ClassData(fighterCls) { Level = 5 });
            if (uSpw11.State == null) Set(uSpw11.Descriptor, "State", new UnitState(uSpw11.Descriptor));
            Set(uSpw11, "Stealth", new UnitStealth());
            Fact(uSpw11, spwRev);
            var uSpw20 = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "spw20", Sp046ClassId, 20);
            uSpw20.Progression.Classes[0].AddArchetype(root);
            Check(spwRes.GetMaxAmount(uSpw11) == 22 && spwRes.GetMaxAmount(uSpw20) == 40,
                "0046 D2-F03 Spirit Walk resource pool == 2 * ShamanLevel (22 at Fighter5+Shaman11, 40 at Shaman20)");

            var spwBefore = uSpw11.Resources.GetResourceAmount(spwRes);
            var spwAa = uSpw11.ActivatableAbilities.RawFacts.Single(a => a.Blueprint == spwToggleBp);
            var spwTickHarmony = new Harmony("Speaker0046.SpwTick");
            spwTickHarmony.Patch(AccessTools.Method(typeof(Kingmaker.Controllers.Units.UnitActivatableAbilitiesController), "GetTimeToNextRound"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(BardMasterpieces01435Tests), "NextRound")));
            spwTickHarmony.Patch(AccessTools.Method(typeof(Kingmaker.Controllers.Units.UnitActivatableAbilitiesController), "CanTickNewRound"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(BardMasterpieces01435Tests), "TickAllowed")));
            var spwCtrl = new Kingmaker.Controllers.Units.UnitActivatableAbilitiesController();
            var spwTickMethod = AccessTools.Method(typeof(Kingmaker.Controllers.Units.UnitActivatableAbilitiesController), "TickAbility");
            try
            {
                spwAa.IsOn = true;
                spwAa.TryStart();
                spwTickMethod.Invoke(spwCtrl, new object[] { uSpw11, spwAa, false }); // spends 1, stays active
                Check(spwAa.IsOn && spwAa.IsStarted && uSpw11.Buffs.HasFact(spwBuffBp) &&
                      uSpw11.State.HasCondition(Kingmaker.UnitLogic.UnitCondition.CantAct) &&
                      uSpw11.HasFact(spwIncFact) &&
                      spwBefore == 22 &&
                      uSpw11.Resources.GetResourceAmount(spwRes) == spwBefore - 1,
                    "0046 D2-F03 Spirit Walk activation applies buff, CantAct, and Incorporeal while spending per round (before=" + spwBefore + " after=" + uSpw11.Resources.GetResourceAmount(spwRes) + " on=" + spwAa.IsOn + " started=" + spwAa.IsStarted + " buff=" + uSpw11.Buffs.HasFact(spwBuffBp) + " cantAct=" + uSpw11.State.HasCondition(Kingmaker.UnitLogic.UnitCondition.CantAct) + " inc=" + uSpw11.HasFact(spwIncFact) + ")");

                uSpw11.Resources.Spend(spwRes, uSpw11.Resources.GetResourceAmount(spwRes));
                spwTickMethod.Invoke(spwCtrl, new object[] { uSpw11, spwAa, false });
                Check(!spwAa.IsOn && !spwAa.IsStarted && spwAa.AppliedBuff == null,
                    "0046 D2-F03 Spirit Walk controller exhaustion stops toggle (on=" + spwAa.IsOn + " started=" + spwAa.IsStarted + ")");
                uSpw11.Buffs.Tick();
                Check(!uSpw11.Buffs.HasFact(spwBuffBp) &&
                      !uSpw11.State.HasCondition(Kingmaker.UnitLogic.UnitCondition.CantAct) &&
                      !uSpw11.HasFact(spwIncFact),
                    "0046 D2-F03 Spirit Walk removes all three effects on resource exhaustion tick (buff=" + uSpw11.Buffs.HasFact(spwBuffBp) + " cantAct=" + uSpw11.State.HasCondition(Kingmaker.UnitLogic.UnitCondition.CantAct) + " inc=" + uSpw11.HasFact(spwIncFact) + ")");

                uSpw11.Resources.Restore(spwRes, 5);
                spwAa.IsOn = true;
                spwAa.TryStart();
                Check(uSpw11.Buffs.HasFact(spwBuffBp) && uSpw11.HasFact(spwIncFact),
                    "0046 D2-F03 Spirit Walk re-enables cleanly when resource restored");
                spwAa.TurnOffImmediately();
                uSpw11.Buffs.Tick();
                Check(!spwAa.IsOn && !spwAa.IsStarted && spwAa.AppliedBuff == null &&
                      !uSpw11.Buffs.HasFact(spwBuffBp) &&
                      !uSpw11.State.HasCondition(Kingmaker.UnitLogic.UnitCondition.CantAct) &&
                      !uSpw11.HasFact(spwIncFact),
                    "0046 D2-F03 Spirit Walk TurnOffImmediately removes all three effects cleanly (on=" + spwAa.IsOn + " started=" + spwAa.IsStarted + " buff=" + uSpw11.Buffs.HasFact(spwBuffBp) + " cantAct=" + uSpw11.State.HasCondition(Kingmaker.UnitLogic.UnitCondition.CantAct) + " inc=" + uSpw11.HasFact(spwIncFact) + ")");
            }
            finally { spwTickHarmony.UnpatchAll(spwTickHarmony.Id); }

            // 8b. Temporal Celerity (2 vs 3 dice & surprise round + multiclass isolation)
            var tcRev = spRevs.First(r => Sp046DisplayName(r) == "Temporal Celerity");
            var tcGraph = Sp046CollectGraph(tcRev);
            var tcModD20s = tcGraph.OfType<BlueprintScriptableObject>().SelectMany(o => o.ComponentsArray).OfType<Kingmaker.Designers.Mechanics.Facts.ModifyD20>()
                .Where(m => m.Rule == Kingmaker.Designers.Mechanics.Facts.RuleType.Initiative).ToList();
            var tcSurprise = tcGraph.OfType<BlueprintScriptableObject>().SelectMany(o => o.ComponentsArray).OfType<SpeakerSurpriseRound0046>().ToList();
            Check(tcModD20s.Any(m => m.RollsAmount == 1 && m.TakeBest) && tcModD20s.Any(m => m.RollsAmount == 2 && m.TakeBest) && tcSurprise.Count == 1,
                "0046 Temporal Celerity contains 1-reroll (2 dice), 2-reroll (3 dice) RuleType.Initiative ModifyD20 and SpeakerSurpriseRound0046");

            var uTc10 = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "tc10", Sp046ClassId, 10);
            uTc10.Progression.Classes[0].AddArchetype(root);
            uTc10.Progression.Classes.Add(new ClassData(fighterCls) { Level = 5 });
            Fact(uTc10, tcRev);
            var uTc11 = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "tc11", Sp046ClassId, 11);
            uTc11.Progression.Classes[0].AddArchetype(root);
            Fact(uTc11, tcRev);
            Sp046DiceOverrideActive = true;
            try
            {
                Sp046Dice.Clear(); Sp046DiceRollsSeen = 0;
                Sp046Dice.Enqueue(2); Sp046Dice.Enqueue(17);
                var tcRoll10 = Rulebook.Trigger(new RuleInitiativeRoll(uTc10));
                int tcSeen10 = Sp046DiceRollsSeen;

                Sp046Dice.Clear(); Sp046DiceRollsSeen = 0;
                Sp046Dice.Enqueue(2); Sp046Dice.Enqueue(3); Sp046Dice.Enqueue(19);
                var tcRoll11 = Rulebook.Trigger(new RuleInitiativeRoll(uTc11));
                int tcSeen11 = Sp046DiceRollsSeen;
                Check(tcSeen10 == 2 && tcRoll10.D20.Result == 17 && tcSeen11 == 3 && tcRoll11.D20.Result == 19,
                    "0046 Temporal Celerity rolls 2 dice at Fighter5+Shaman10 and 3 dice at Shaman11 (seen10=" + tcSeen10 + " seen11=" + tcSeen11 + ")");
            }
            finally
            {
                Sp046DiceOverrideActive = false;
                Sp046Dice.Clear();
            }

            // 8c. Erase from Time (1/day at L10, 2/day at L11, PsychicStasisPause without Paralyzed)
            var eftRev = spRevs.First(r => Sp046DisplayName(r) == "Erase from Time");
            var eftGraph = Sp046CollectGraph(eftRev);
            var eftRes = eftRev.GetComponents<AddAbilityResources>().Single().Resource;
            var eftAbs = eftGraph.OfType<BlueprintAbility>().ToList();
            var eftAb = eftAbs.Single(a => a.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic>() != null);
            var eftResLogic = eftAb.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic>();
            var eftBuff = eftGraph.OfType<BlueprintBuff>().Single();
            Check(eftAbs.Count == 2 && eftResLogic != null && eftResLogic.Amount == 1 &&
                  eftBuff.GetComponent<PsychicStasisPause>() != null &&
                  !eftBuff.GetComponents<Kingmaker.UnitLogic.FactLogic.AddCondition>().Any(cnd => cnd.Condition == Kingmaker.UnitLogic.UnitCondition.Paralyzed),
                "0046 Erase from Time uses held-charge touch pair with 1-charge AbilityResourceLogic and PsychicStasisPause without Paralyzed");
            Check(eftRes.GetMaxAmount(uTc10) == 1 && eftRes.GetMaxAmount(uTc11) == 2,
                "0046 Erase from Time charges == 1 at Fighter5+Shaman10 and == 2 at Shaman11 (max10=" + eftRes.GetMaxAmount(uTc10) + " max11=" + eftRes.GetMaxAmount(uTc11) + ")");
            var atRev = spRevs.First(r => Sp046DisplayName(r) == "Aging Touch");
            var atRes = atRev.GetComponents<AddAbilityResources>().Single().Resource;
            var uAt1 = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "at1", Sp046ClassId, 1);
            uAt1.Progression.Classes[0].AddArchetype(root);
            var uAt5 = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c2, "at5", Sp046ClassId, 5);
            uAt5.Progression.Classes[0].AddArchetype(root);
            Check(atRes.GetMaxAmount(uAt1) == 1 && atRes.GetMaxAmount(uAt5) == 2 && atRes.GetMaxAmount(uTc10) == 3,
                "0046 Aging Touch charges follow 1 + ShamanLevel/5 (1 at L1, 2 at L5, 3 at Fighter5+Shaman10)");
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
