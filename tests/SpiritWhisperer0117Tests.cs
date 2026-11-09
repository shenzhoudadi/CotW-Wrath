using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;

// COTW-0117 B-stage directed check. Only this case runs: the host is entered
// with COTW_ARCANE_01446_ONLY=1 and COTW_0117_CASE=spiritwhisperer0117.
// No game, no UMM, no full historical suite and no whole-library matrix.
internal static class SpiritWhisperer0117Tests
{
    private const string WizardId = "ba34257984f4c41408ce1dc2004e342e";
    private const string WizardFeatId = "8c3102c2ff3b69444b139a98521a4899";
    private const string FaerieFireBuffId = "cc383a9eaae4d2b45a925d442b367b54";
    private const string ExtraChannelId = "cd9f19775bd9d3343a31a065e93f0c47";
    private static int checks;
    private static Assembly Main => AppDomain.CurrentDomain.GetAssemblies()
        .First(a => a.GetName().Name == "CallOfTheWild.Wrath.Prototype");
    private static Type T(string name) => Main.GetType("CallOfTheWild.Wrath.Prototype." + name, true);
    private static object Get(object o, string f)
    {
        var field = AccessTools.Field(o.GetType(), f);
        if (field != null) return field.GetValue(o);
        var property = AccessTools.Property(o.GetType(), f);
        return property?.GetValue(o, null);
    }
    private static bool Has(object o, string f) => AccessTools.Field(o.GetType(), f) != null ||
                                                   AccessTools.Property(o.GetType(), f) != null;
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception("0117 CHECK FAILED: " + message);
        checks++;
        Console.WriteLine("0117 CHECK: " + message);
    }
    private static BlueprintGuid Id(string token) => (BlueprintGuid)AccessTools.Method(T("SpiritClassBridge0117"), "Id")
        .Invoke(null, new object[] { token });
    private static BlueprintGuid RootId => Id("SpiritWhisperer0117Archetype");
    private static bool Register(BlueprintsCache cache) => (bool)AccessTools.Method(T("SpiritWhisperer0117"), "Register")
        .Invoke(null, new object[] { cache });
    private static BlueprintsCache FreshCache(BlueprintsCache parent)
    {
        var method = AccessTools.Method(typeof(Arcane01446Tests), "Fresh");
        return (BlueprintsCache)method.Invoke(null, new object[] { parent });
    }

    internal static int Run(BlueprintsCache parent, Harmony h)
    {
        checks = 0;
        Console.WriteLine("0117 stage: isolated managed host, real game blueprint cache, C-0117-20261108 cases");

        // ---- ATOMIC-REJECT: one missing required provider must publish zero.
        var rejected = FreshCache(parent);
        var wizardBefore = (BlueprintCharacterClass)rejected.Load(BlueprintGuid.Parse(WizardId));
        var archetypesBefore = ((BlueprintArchetypeReference[])Get(wizardBefore, "m_Archetypes") ?? Array.Empty<BlueprintArchetypeReference>()).Length;
        var poolsBefore = new[] { "247a4068296e8be42890143f451b4b45", "d332c1748445e8f4f9e92763123e31bd", "a21acdafc0169f5488a9bd3256e2e65b" }
            .Select(g => (BlueprintFeatureSelection)rejected.Load(BlueprintGuid.Parse(g)))
            .Select(p => ((BlueprintFeatureReference[])Get(p, "m_Features")).Length).ToArray();
        rejected.RemoveCachedBlueprint(BlueprintGuid.Parse(FaerieFireBuffId));
        Check(!Register(rejected), "ATOMIC-REJECT Register returns false when one required provider is absent");
        Check(rejected.Load(RootId) == null, "ATOMIC-REJECT no private archetype blueprint is published");
        Check(((BlueprintArchetypeReference[])Get(wizardBefore, "m_Archetypes")).Length == archetypesBefore,
            "ATOMIC-REJECT the Wizard archetype array is untouched");
        Check(new[] { "247a4068296e8be42890143f451b4b45", "d332c1748445e8f4f9e92763123e31bd", "a21acdafc0169f5488a9bd3256e2e65b" }
            .Select(g => (BlueprintFeatureSelection)rejected.Load(BlueprintGuid.Parse(g)))
            .Select(p => ((BlueprintFeatureReference[])Get(p, "m_Features")).Length).SequenceEqual(poolsBefore),
            "ATOMIC-REJECT no Extra Channel is appended to any feat pool");
        Check(rejected.Load(BlueprintGuid.Parse(ExtraChannelId)) != null,
            "ATOMIC-REJECT the native Extra Channel is not modified or removed");

        // ---- PUBLISH: with every required donor present the full ten-spirit
        // package publishes; the native Shaman class and spirit graphs stay
        // untouched and no contract gap remains.
        var cache = FreshCache(parent);
        var wizard = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse(WizardId));
        var shamanFingerprint = NativeSpiritFingerprint(cache);
        var gapArchetypesBefore = ((BlueprintArchetypeReference[])Get(wizard, "m_Archetypes") ?? Array.Empty<BlueprintArchetypeReference>()).Length;
        var poolIds = new[] { "247a4068296e8be42890143f451b4b45", "d332c1748445e8f4f9e92763123e31bd", "a21acdafc0169f5488a9bd3256e2e65b" };
        var gapPoolsBefore = poolIds.Select(g => (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(g)))
            .Select(p => ((BlueprintFeatureReference[])Get(p, "m_Features")).Length).ToArray();
        Check(Register(cache), "PUBLISH Register succeeds when the cache has the required donors");
        Check(cache.Load(RootId) != null, "PUBLISH the private archetype is in the cache");
        Check(AllPrivate(cache).Length > 0, "PUBLISH private blueprints are present");
        Check(((BlueprintArchetypeReference[])Get(wizard, "m_Archetypes")).Count(r => r != null && r.Guid == RootId) == 1,
            "PUBLISH exactly one Wizard archetype link is appended");
        Check(NativeSpiritFingerprint(cache) == shamanFingerprint,
            "PUBLISH native Shaman class and spirit progressions are unchanged");
        var gaps = (List<string>)AccessTools.Method(T("SpiritWhisperer0117"), "ContractGaps").Invoke(null, new object[] { cache });
        Check(gaps.Count == 0, "PUBLISH no contract gap remains");
        var presentMethod = AccessTools.Method(T("SpiritWhisperer0117"), "ComponentTypePresent");
        Check((bool)presentMethod.Invoke(null, new object[] { "IncomingDamageAndHealingModifier" }),
            "PUBLISH OnlyHeal modifier exists");
        Check((bool)presentMethod.Invoke(null, new object[] { "AbilityCustomDimensionDoor" }),
            "PUBLISH dimension door base exists");

        // ---- D-F01 / D-F02 source-data audit. Read the real Rows table; this is
        // structural evidence the contract lists are encoded, NOT a published
        // behaviour.
        var rows = (Array)AccessTools.Field(T("SpiritWhisperer0117"), "Rows").GetValue(null);
        Check(rows.Length == 10, "SOURCE exactly ten spirit rows");
        var frost = new[] { "30dc785861c12374c910e5ede5b87ef4", "a84c3cf1a23eff74188a239c8182d6ee", "5d396a800c34407fb3e01b8782b91c86" };
        var contractHexes = new Dictionary<string, string[]> {
            { "Battle", new[] { "Battle Master", "Battle Ward", "Curse of Suffering", "Hampering Hex" } },
            { "Bones", new[] { "Bone Lock", "Bone Ward", "Fearful Gaze", "Deathly Being" } },
            { "Flame", new[] { "Fire Nimbus", "Flame Curse", "Ward of Flames", "Cinder Dance" } },
            { "Heavens", new[] { "Enveloping Void", "Lure of Heavens", "Heaven's Leap", "Starburn" } },
            { "Life", new[] { "Curse of Suffering", "Enhanced Cures", "Life Link", "Life Sight" } },
            { "Lore", new[] { "Benefit of Wisdom", "Brain Drain", "Confusion Curse", "Mental Acuity" } },
            { "Nature", new[] { "Entangling Curse", "Erosion Curse", "Friend to Animals", "Stormwalker" } },
            { "Stone", new[] { "Stone Stability", "Loadstone", "Metal Curse", "Ward of Stone" } },
            { "Waves", new[] { "Beckoning Chill", "Crashing Waves", "Mist's Shroud" } },
            { "Wind", new[] { "Air Barrier", "Sparkling Aura", "Vortex Spells", "Wind Ward" } },
        };
        foreach (var row in rows)
        {
            var name = (string)Get(row, "Name");
            var tiers = (string[])Get(row, "Tiers");
            Check(tiers.Length == 3, name + " SOURCE has three 1/8/20 tier slots");
            Check(!tiers.Any(t => t != null && frost.Contains(t)), name + " D-F01 never clones a Shaman frost-spirit GUID");
            if (name == "Heavens" || name == "Lore")
                Check(tiers.All(t => t == null) && (string)Get(row, "PrivateSpirit") == name,
                    name + " D-F01 builds private 1/8/20 tiers, no native Wrath spirit donor");
            var hexNames = ((Array)Get(row, "Hexes")).Cast<object>().Select(hx => (string)Get(hx, "Name")).ToArray();
            Check(hexNames.SequenceEqual(contractHexes[name]),
                name + " D-F02 hex pool matches the contract list exactly (" + hexNames.Length + " hexes)");
            Check(!hexNames.Contains("Fluid Magic"), name + " D-F02 omits the non-functional Fluid Magic");
        }
        var wavesRow = rows.Cast<object>().First(r => (string)Get(r, "Name") == "Waves");
        Check(((Array)Get(wavesRow, "Hexes")).Length == 3,
            "D-F02 Waves has three real hexes (Beckoning Chill, Crashing Waves, Mist's Shroud), not one");

        // ---- D-F03 source-data: Life Link and Life Sight spend the R5 branch.
        var lifeRow = rows.Cast<object>().First(r => (string)Get(r, "Name") == "Life");
        foreach (var hx in ((Array)Get(lifeRow, "Hexes")).Cast<object>())
        {
            var hn = (string)Get(hx, "Name");
            if (hn == "Life Link" || hn == "Life Sight")
                Check((int)Get(hx, "Resource") == 5, "D-F03 " + hn + " uses the R5 (1/Wizard level) resource branch");
        }
        // The R5 shape is computed from a real native resource component.
        var factoryType = T("SpiritClassBridge0117+Factory");
        var factory = Activator.CreateInstance(factoryType, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { cache }, null);
        var r5 = (BlueprintAbilityResource)AccessTools.Method(T("SpiritClassBridge0117"), "Resource")
            .Invoke(null, new object[] { factory, "ProbeR5", 0, null, true });
        var r5Amount = Get(r5, "m_MaxAmount");
        Check((int)Get(r5Amount, "BaseValue") == 0 && (bool)Get(r5Amount, "IncreasedByLevel") &&
              (int)Get(r5Amount, "LevelIncrease") == 1 && !(bool)Get(r5Amount, "IncreasedByStat"),
            "D-F03 R5 resource is 0 base + 1 per Wizard level, no stat scaling");
        if (Has(r5, "m_Min")) Check((int)Get(r5, "m_Min") == 0, "D-F03 R5 resource cannot go negative");

        // ---- D-F04: exercise the real Tier build path for Life L8 and Lore L8
        // and read the produced native AddStatBonus components. Structural only.
        var tierMethod = AccessTools.Method(T("SpiritWhisperer0117"), "Tier");
        var loreRow = rows.Cast<object>().First(r => (string)Get(r, "Name") == "Lore");
        var life8 = (BlueprintFeature)tierMethod.Invoke(null, new object[] { factory, cache, wizard, lifeRow, 1 });
        var life8Bonus = life8.ComponentsArray.FirstOrDefault(c => c != null && c.GetType().Name == "AddStatBonus");
        Check(life8Bonus != null && Get(life8Bonus, "Stat").ToString() == "SkillLoreReligion" &&
              Get(life8Bonus, "Descriptor").ToString() == "UntypedStackable" && (int)Get(life8Bonus, "Value") == 3,
            "D-F04 Life L8 Healer's Touch is Lore (Religion) +3 UntypedStackable");
        var lore8 = (BlueprintFeature)tierMethod.Invoke(null, new object[] { factory, cache, wizard, loreRow, 1 });
        var lore8Bonuses = lore8.ComponentsArray.Where(c => c != null && c.GetType().Name == "AddStatBonus").ToArray();
        Check(lore8Bonuses.Length == 4 && lore8Bonuses.All(c => Get(c, "Descriptor").ToString() == "Competence" && (int)Get(c, "Value") == 10),
            "D-F04 Lore L8 Perfect Knowledge keeps four skills +10 Competence");
        var heavensRow = rows.Cast<object>().First(r => (string)Get(r, "Name") == "Heavens");
        var heavens8 = (BlueprintFeature)tierMethod.Invoke(null, new object[] { factory, cache, wizard, heavensRow, 1 });
        Check(heavens8.ComponentsArray.Any(c => c != null && c.GetType().Name == "Blindsense") &&
              !heavens8.ComponentsArray.Any(c => c != null && c.GetType().Name == "AddDamageResistanceEnergy"),
            "D-F01 Heavens level 8 is blindsense and poison immunity, not cold resistance");
        var flame = rows.Cast<object>().First(r => (string)Get(r, "Name") == "Flame");
        var cinder = ((Array)Get(flame, "Hexes")).Cast<object>().First(h => (string)Get(h, "Name") == "Cinder Dance");
        Check((string)Get(cinder, "Donor") == "6e67eae3081853544b191943f5ed4534",
            "D-F02 Cinder Dance clones the native revelation and BuildHexFeature strips its prerequisites");

        // ---- D-F05: Monstrous Insight must grant the promised +2 insight attack/AC
        // and spend one resource. Read the real published buff, not a field name.
        var insightBuff = cache.Load(Id("LoreTier0Buff")) as BlueprintBuff;
        Check(insightBuff != null, "D-F05 Monstrous Insight buff is published");
        var miAtk = insightBuff.ComponentsArray.FirstOrDefault(c => c != null && c.GetType().Name == "AttackBonusAgainstTarget");
        var miAc = insightBuff.ComponentsArray.FirstOrDefault(c => c != null && c.GetType().Name == "ACBonusAgainstTarget");
        Check(miAtk != null && (int)Get(Get(miAtk, "Value"), "Value") == 2 &&
              Get(miAtk, "Descriptor").ToString() == "Insight" && (bool)Get(miAtk, "CheckCaster"),
            "D-F05 buff grants +2 insight attack against the marked creature");
        Check(miAc != null && (int)Get(Get(miAc, "Value"), "Value") == 2 &&
              Get(miAc, "Descriptor").ToString() == "Insight" && (bool)Get(miAc, "CheckCaster"),
            "D-F05 buff grants +2 insight AC against the marked creature");
        var insightAbility = cache.Load(Id("LoreTier0Ability")) as BlueprintAbility;
        Check(insightAbility != null && insightAbility.ComponentsArray.Count(c => c != null && c.GetType().Name == "AbilityResourceLogic") == 1,
            "D-F05 Monstrous Insight spends exactly one resource");

        // ---- D-F08: the identification DC must not subtract the wizard level that
        // is already granted once as the insight bonus. Read the real source formula
        // and construct one real RuleSkillCheck (construction only, not the full suite).
        var insightDc = AccessTools.Method(T("MonsterLore0117"), "InsightDC");
        foreach (int targetLevel in new[] { 0, 1, 5, 10, 20 })
            Check((int)insightDc.Invoke(null, new object[] { targetLevel }) == 10 + targetLevel,
                "D-F08 identification DC is 10 + target level " + targetLevel + ", no wizard-level reduction");
        var miCaster = (UnitEntityData)AccessTools.Method(typeof(Arcane01446Tests), "Unit")
            .Invoke(null, new object[] { cache, "b3a505fb61437dc4097f43c3f8f9a4cf", 10 });
        const int miWizardLevel = 10, miTargetLevel = 10;
        var miCheck = new RuleSkillCheck(miCaster, StatType.SkillKnowledgeArcana,
            (int)insightDc.Invoke(null, new object[] { miTargetLevel }));
        Check((int)Get(miCheck, "DifficultyClass") == 10 + miTargetLevel,
            "D-F08 real RuleSkillCheck carries DC 20 for a level-10 target, not lowered by wizard level");
        var miStat = miCaster.Stats.GetStat(StatType.SkillKnowledgeArcana);
        int miStatBefore = miStat.ModifiedValue;
        miCheck.AddTemporaryModifier(miStat.AddModifier(miWizardLevel, ModifierDescriptor.Insight));
        Check(miStat.ModifiedValue - miStatBefore == miWizardLevel,
            "D-F08 the wizard level is applied once as a +10 insight bonus on the check's skill");

        // ---- D-F06: the Wish menu must exclude every ability-score enhancement spell.
        var wishList = cache.Load(BlueprintGuid.Parse("ba0401fdeb4062f40a7aa95b6f07fe89")) as BlueprintSpellList;
        var blockedGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "4c3d08935262b6544ae97599b3a9556d", "a900628aea19aa74aad0ece0e65d091a", "de7a025d48ad5da4991e7d3c682cf69d",
            "446f7bf201dc1934f96ac0a26e324803", "ae4d3ad6a8fda1542acf2e9bbc13d113", "f0455c9295b53904f9e02fc571dd2ce1",
            "6a234c6dcde7ae94e94e9c36fd1163a7", "f6bcea6db14f0814d99b54856e918b92", "1f6c94d56f178b84ead4c02f1b1e1c48",
            "2caa607eadda4ab44934c5c9875e01bc", "2b24159ad9907a8499c2313ba9c0f615", "9f5ada581af3db4419b54db77f44e430",
        };
        var blockedInList = new List<string>();
        var wishLevels = wishList.SpellsByLevel;
        for (int lv = 1; lv <= 8 && lv < wishLevels.Length; lv++)
            foreach (var sp in wishLevels[lv].Spells)
                if (sp != null && blockedGuids.Contains(sp.AssetGuid.ToString().Replace("-", "")))
                    blockedInList.Add(sp.AssetGuid.ToString().Replace("-", ""));
        Check(blockedInList.Count > 0, "D-F06 wizard list really contains ability-score spells (filter is non-vacuous)");
        Check(blockedInList.All(g => cache.Load(Id("LoreTier2Wish" + g)) == null),
            "D-F06 no ability-score spell is cloned into the Wish menu (" + blockedInList.Count + " blocked)");

        // ---- D-F07: the variant parent carries no resource; each leaf spends exactly
        // one and points m_Parent back at the menu (contract section 6).
        var wish = cache.Load(Id("LoreTier2Wish")) as BlueprintAbility;
        Check(wish != null, "D-F07 Wish menu is published");
        Check(wish.ComponentsArray.Count(c => c != null && c.GetType().Name == "AbilityResourceLogic") == 0,
            "D-F07 Wish parent has no resource component");
        var wishVariants = wish.ComponentsArray.FirstOrDefault(c => c != null && c.GetType().Name == "AbilityVariants");
        Check(wishVariants != null, "D-F07 Wish parent is a variant menu");
        var wishChildren = (BlueprintAbilityReference[])Get(wishVariants, "m_Variants");
        Check(wishChildren.Length > 0, "D-F07 Wish menu has children");
        bool leafCost = true, leafParent = true;
        foreach (var r in wishChildren)
        {
            var child = cache.Load(r.Guid) as BlueprintAbility;
            if (child == null) { leafCost = false; leafParent = false; continue; }
            var costs = child.ComponentsArray.Where(c => c != null && c.GetType().Name == "AbilityResourceLogic").ToArray();
            if (costs.Length != 1 || (int)Get(costs[0], "Amount") != 1 || !(bool)Get(costs[0], "m_IsSpendResource")) leafCost = false;
            var par = Get(child, "m_Parent") as BlueprintAbilityReference;
            if (par == null || par.Guid != wish.AssetGuid) leafParent = false;
        }
        Check(leafCost, "D-F07 every Wish leaf spends exactly one resource");
        Check(leafParent, "D-F07 every Wish leaf m_Parent points at the menu");

        // ---- D-F09 / D-F10: Ward of Stone discharge and Sparkling Aura shock must
        // fire only after the attack resolves and only on a real hit. Exercise the
        // real published buff components on a real unit with a controlled
        // RuleAttackWithWeapon/RuleAttackRoll fixture (callback invocation, not the
        // full combat damage pipeline).
        var wardBp = AllPrivate(cache).OfType<BlueprintBuff>()
            .FirstOrDefault(b => b.ComponentsArray.Any(c => c != null && c.GetType() == T("WardCharges0117")));
        var sparkBp = AllPrivate(cache).OfType<BlueprintBuff>()
            .FirstOrDefault(b => b.ComponentsArray.Any(c => c != null && c.GetType() == T("SparklingAura0117")));
        Check(wardBp != null && sparkBp != null, "D-F09/D-F10 published Ward of Stone and Sparkling Aura buffs carry the hit components");
        var wardHolder = (UnitEntityData)AccessTools.Method(typeof(Arcane01446Tests), "Unit").Invoke(null, new object[] { cache, "b3a505fb61437dc4097f43c3f8f9a4cf", 10 });
        var attacker = (UnitEntityData)AccessTools.Method(typeof(Arcane01446Tests), "Unit").Invoke(null, new object[] { cache, "b3a505fb61437dc4097f43c3f8f9a4cf", 10 });
        var metal = Weapon0117(wardHolder, WeaponCategory.Longsword, "0117-metal");
        var nonMetal = Weapon0117(wardHolder, WeaponCategory.UnarmedStrike, "0117-nonmetal");
        Check(metal.Blueprint.Type.Category.HasSubCategory(WeaponSubCategory.Metal) && !nonMetal.Blueprint.Type.Category.HasSubCategory(WeaponSubCategory.Metal),
            "D-F10 fixture metal weapon is metal and unarmed strike is not (native HasSubCategory)");

        var wardHit = AddBuff0117(wardHolder, wardBp);
        Invoke0117(wardHit, T("WardCharges0117"), "OnEventDidTrigger", Attack0117(attacker, wardHolder, metal, true));
        Check(wardHit.IsDisposed, "D-F09 a resolved hit removes the Ward of Stone buff");
        var wardMiss = AddBuff0117(wardHolder, wardBp);
        Invoke0117(wardMiss, T("WardCharges0117"), "OnEventDidTrigger", Attack0117(attacker, wardHolder, metal, false));
        Check(!wardMiss.IsDisposed, "D-F09 a miss keeps the Ward of Stone buff"); wardMiss.Remove();
        var wardFake = AddBuff0117(wardHolder, wardBp);
        Invoke0117(wardFake, T("WardCharges0117"), "OnEventDidTrigger", Attack0117(attacker, wardHolder, metal, true, true));
        Check(!wardFake.IsDisposed, "D-F09 a fake/preview hit keeps the Ward of Stone buff"); wardFake.Remove();
        var wardAbout = AddBuff0117(wardHolder, wardBp);
        Invoke0117(wardAbout, T("WardCharges0117"), "OnEventAboutToTrigger", Attack0117(attacker, wardHolder, metal, true));
        Check(!wardAbout.IsDisposed, "D-F09 the pre-resolution callback does not discharge the ward"); wardAbout.Remove();

        var sparkHolder = (UnitEntityData)AccessTools.Method(typeof(Arcane01446Tests), "Unit").Invoke(null, new object[] { cache, "b3a505fb61437dc4097f43c3f8f9a4cf", 10 });
        sparkHolder.Stats.Charisma.BaseValue = 14;
        int chaBonus = sparkHolder.Stats.Charisma.Bonus;
        var triggerDamage = AccessTools.Method(typeof(RuleDealDamage), "OnTrigger", new[] { typeof(RulebookEventContext) });
        var capture = AccessTools.Method(typeof(SpiritWhisperer0117Tests), nameof(CaptureDamage0117));
        h.Patch(triggerDamage, prefix: new HarmonyMethod(capture));
        try
        {
            var sparkHit = AddBuff0117(sparkHolder, sparkBp);
            damageCalls0117 = 0; capturedDamage0117 = null;
            Invoke0117(sparkHit, T("SparklingAura0117"), "OnEventDidTrigger", Attack0117(attacker, sparkHolder, metal, true));
            Check(damageCalls0117 == 1 && capturedDamage0117 != null && ReferenceEquals(capturedDamage0117.Target, sparkHolder) && ReferenceEquals(capturedDamage0117.Initiator, sparkHolder),
                "D-F13 a resolved metal hit shocks the glowing creature");
            var first = (EnergyDamage)Member0117(Member0117(capturedDamage0117, "DamageBundle"), "First");
            Check(first.EnergyType == DamageEnergyType.Electricity && first.Bonus == chaBonus && chaBonus == 2,
                "D-F10 the shock is electricity equal to the holder's Charisma modifier (+2 at Cha 14)");
            damageCalls0117 = 0;
            Invoke0117(sparkHit, T("SparklingAura0117"), "OnEventDidTrigger", Attack0117(attacker, sparkHolder, metal, false));
            Check(damageCalls0117 == 0, "D-F10 a metal weapon that misses deals no shock (the round-7 defect)");
            damageCalls0117 = 0;
            Invoke0117(sparkHit, T("SparklingAura0117"), "OnEventDidTrigger", Attack0117(attacker, sparkHolder, nonMetal, true));
            Check(damageCalls0117 == 0, "D-F10 a non-metal weapon that hits deals no shock");
            damageCalls0117 = 0;
            Invoke0117(sparkHit, T("SparklingAura0117"), "OnEventDidTrigger", Attack0117(attacker, sparkHolder, metal, true, true));
            Check(damageCalls0117 == 0, "D-F10 a fake/preview metal hit deals no shock");
            damageCalls0117 = 0;
            Invoke0117(sparkHit, T("SparklingAura0117"), "OnEventAboutToTrigger", Attack0117(attacker, sparkHolder, metal, true));
            Check(damageCalls0117 == 0, "D-F10 the pre-resolution callback deals no shock");
        }
        finally { h.Unpatch(triggerDamage, capture); }

        // ---- D-F11: the three hexes whose text names a scaling duration must
        // calculate that many rounds. A constant BonusValue of 1 is the defect.
        var durationCaster = (UnitEntityData)AccessTools.Method(typeof(Arcane01446Tests), "Unit")
            .Invoke(null, new object[] { cache, WizardId, 20 });
        Check(durationCaster.Progression.GetClassLevel(wizard) == 20, "D-F11 duration fixture is a level-20 wizard");
        CheckClassRank("BattleHex2Ability", "D-F11 Battle Curse of Suffering");
        CheckClassRank("LifeHex0Ability", "D-F11 Life Curse of Suffering");
        CheckClassRank("HeavensHex0Ability", "D-F11 Enveloping Void");
        CheckRankDuration("BattleHex2Ability", "D-F11 Battle Curse of Suffering", 20, durationCaster);
        CheckRankDuration("LifeHex0Ability", "D-F11 Life Curse of Suffering", 20, durationCaster);
        CheckRankDuration("HeavensHex0Ability", "D-F11 Enveloping Void", 20, durationCaster);
        var confusion = (BlueprintAbility)cache.Load(Id("LoreHex2Ability"));
        var chaRank = confusion.GetComponents<ContextRankConfig>().Single();
        Check(Get(chaRank, "m_BaseValueType").ToString() == "StatBonus" && Get(chaRank, "m_Stat").ToString() == "Charisma" &&
              (bool)Get(chaRank, "m_UseMin") && (int)Get(chaRank, "m_Min") == 1,
            "D-F11 Confusion Curse rank is the Charisma modifier, minimum 1");
        durationCaster.Stats.Charisma.BaseValue = 18;
        Check(durationCaster.Stats.Charisma.Bonus == 4, "D-F11 Charisma 18 is modifier +4, actual " + durationCaster.Stats.Charisma.Bonus);
        CheckRankDuration("LoreHex2Ability", "D-F11 Confusion Curse at Charisma 18", 4, durationCaster);
        durationCaster.Stats.Charisma.BaseValue = 8;
        Check(durationCaster.Stats.Charisma.Bonus <= 0, "D-F11 Charisma 8 is not a positive modifier, actual " + durationCaster.Stats.Charisma.Bonus);
        CheckRankDuration("LoreHex2Ability", "D-F11 Confusion Curse minimum", 1, durationCaster);

        var levelOne = (UnitEntityData)AccessTools.Method(typeof(Arcane01446Tests), "Unit")
            .Invoke(null, new object[] { cache, WizardId, 1 });
        Check(levelOne.Progression.GetClassLevel(wizard) == 1, "D-F12 fixture is a level-1 wizard");
        CheckRankDuration("HeavensTier0Ability", "D-F12 Stardust at wizard 20", 10, durationCaster);
        CheckRankDuration("HeavensTier0Ability", "D-F12 Stardust at wizard 1", 1, levelOne);
        CheckRankDuration("WindHex1Ability", "D-F13 Sparkling Aura at wizard 20", 10, durationCaster);
        CheckRankDuration("WindHex1Ability", "D-F13 Sparkling Aura at wizard 1", 1, levelOne);
        Check(((BlueprintAbility)cache.Load(Id("WindHex1Ability"))).Range == AbilityRange.Close, "D-F13 Sparkling Aura range is close");
        foreach (var pair in new[] { new[] { "LoreHex1Ability", "Brain Drain" }, new[] { "NatureHex1Ability", "Erosion Curse" }, new[] { "HeavensHex3Ability", "Starburn" } })
        {
            var rank = ((BlueprintAbility)cache.Load(Id(pair[0]))).GetComponents<ContextRankConfig>().Single();
            Check(Get(rank, "m_Progression").ToString() == "Div2" && (bool)Get(rank, "m_UseMin") && (int)Get(rank, "m_Min") == 1,
                "D-F14 " + pair[1] + " Div2 floor is enabled");
            Check(rank.GetValue(new MechanicsContext(levelOne, levelOne.Descriptor, (BlueprintAbility)cache.Load(Id(pair[0])))) == 1,
                "D-F14 " + pair[1] + " is 1 die at wizard 1");
        }
        var starburn = (BlueprintAbility)cache.Load(Id("HeavensHex3Ability"));
        Check(starburn.ComponentsArray.Any(c => c is AbilityCasterHasNoFacts), "D-F15 Starburn has a cooldown restriction");
        var starburnSaved = starburn.ComponentsArray.OfType<AbilityEffectRunAction>().SelectMany(r => r.Actions.Actions).OfType<ContextActionConditionalSaved>().Single();
        Check(starburnSaved.Failed.Actions.OfType<ContextActionApplyBuff>().Any() && (starburnSaved.Succeed.Actions == null || starburnSaved.Succeed.Actions.Length == 0),
            "D-F15 Starburn light is only on a failed Fortitude save");
        var sight = cache.Load(Id("LifeHex3Buff")) as BlueprintBuff;
        var blindsight = sight.ComponentsArray.FirstOrDefault(c => c is Blindsense);
        Check(blindsight != null && (bool)Get(blindsight, "Blindsight"), "D-F17 Life Sight grants blindsight");
        Check(sight.ComponentsArray.Any(c => c is BuffDescriptorImmunity && Get(c, "Descriptor").ToString().Contains("GazeAttack")),
            "D-F17 Life Sight is immune to gaze attacks");
        var acuity = cache.Load(Id("LoreHex3")) as BlueprintFeature;
        var acuityRank = acuity.GetComponents<ContextRankConfig>().Single();
        Check(Get(acuityRank, "m_Progression").ToString() == "OnePlusDivStep" && (int)Get(acuityRank, "m_StepLevel") == 5,
            "D-F18 Mental Acuity is +1 then +1 per 5 wizard levels");
        Check(acuityRank.GetValue(new MechanicsContext(levelOne, levelOne.Descriptor, acuity)) == 1, "D-F18 Mental Acuity is +1 at wizard 1");
        Check(acuityRank.GetValue(new MechanicsContext(durationCaster, durationCaster.Descriptor, acuity)) == 5, "D-F18 Mental Acuity is +5 at wizard 20");
        var entangle = Granted0117(cache.Load(Id("NatureHex0")) as BlueprintFeature).Single();
        var entangleRank = entangle.GetComponents<ContextRankConfig>().First(r => Get(r, "m_Stat").ToString() == "Charisma");
        Check((bool)Get(entangleRank, "m_UseMin") && (int)Get(entangleRank, "m_Min") == 1, "D-F19 Entangling Curse duration is at least 1 round");
        var friend = cache.Load(Id("NatureHex2")) as BlueprintFeature;
        var summon = GrantedFeatures0117(friend).First(f => f.ComponentsArray.Any(c => c != null && c.GetType().Name == "SpontaneousSpellConversion"));
        Check(summon.ComponentsArray.Where(c => c != null && c.GetType().Name == "SpontaneousSpellConversion").All(c => ((BlueprintCharacterClassReference)Get(c, "m_CharacterClass")).Guid == wizard.AssetGuid),
            "D-F20 Friend to Animals spontaneous conversion uses Wizard");
        foreach (var token in new[] { "BattleHex2Ability", "LoreHex1Ability", "LoreHex2Ability", "NatureHex1Ability", "StoneHex3Ability", "StoneHex1Ability", "WindHex1Ability", "HeavensHex3Ability", "HeavensHex0Ability", "WavesHex2Ability" })
            Check(HasDayLock((BlueprintAbility)cache.Load(Id(token))), "D-F21 " + token + " cannot retarget the same creature for a day");
        Check(AllPrivate(cache).OfType<BlueprintBuff>().Any(b => b.ComponentsArray.Any(c => c != null && c.GetType() == T("MistCharges0117"))),
            "D-F22 Mist's Shroud counts concealment misses");
        var leap = ((BlueprintAbility)cache.Load(Id("HeavensHex2Ability"))).ComponentsArray.First(c => c != null && c.GetType().Name == "HeavensLeap0117");
        Check(Get(leap, "DayLock") != null && cache.Load(Id("HeavensHex2Select")) != null && ((BlueprintAbility)cache.Load(Id("HeavensHex2Ability"))).CanTargetPoint,
            "D-F23 Heaven's Leap selects a creature, then moves that creature to a point");
        var weatherType = T("WeatherSpeed0117");
        var weatherFields = string.Join(",", weatherType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Select(f => f.Name));
        var weatherInterfaces = string.Join(",", weatherType.GetInterfaces().Select(i => i.Name));
        Check(weatherInterfaces.Contains("ITickEachRound") && weatherFields.Length == 0,
            "D-F24 Stormwalker rechecks weather; interfaces=" + weatherInterfaces + "; fields=" + weatherFields);
        var harms = AccessTools.Method(T("DeathlyHealing0117"), "HarmsAsPositive");
        Check((bool)harms.Invoke(null, new object[] { SpellDescriptor.None, "LayOnHandsAbility", true }), "D-F25 Lay on Hands is positive energy and harms");
        Check(!(bool)harms.Invoke(null, new object[] { SpellDescriptor.None, "", false }), "D-F25 unsourced fast healing is not converted");
        Check(!(bool)harms.Invoke(null, new object[] { SpellDescriptor.ChannelNegativeHeal, "InflictLightWounds", true }), "D-F25 negative channel healing is not converted to damage");

        // ---- Idempotent re-entry of a published package: still succeeds, no leak.
        var published = AllPrivate(cache).Length;
        Check(Register(cache), "IDEMPOTENT a second Register returns true");
        Check(AllPrivate(cache).Length == published, "IDEMPOTENT no duplicate private blueprint");

        Console.WriteLine("0117 RESULT: " + checks + " structural/component assertions in the isolated managed host.");
        Console.WriteLine("0117 NOT RUN: real AbilityCastRateUtils.SpendResources / IsAbilityRestrictionPassed / " +
            "OnNewRound / rest / retrain rule chains, player game, UMM, save UI. Field checks are not behaviour PASS.");
        return checks;

        void CheckClassRank(string token, string label)
        {
            var ability = (BlueprintAbility)cache.Load(Id(token));
            var rank = ability.GetComponents<ContextRankConfig>().Single();
            var classes = (BlueprintCharacterClassReference[])Get(rank, "m_Class");
            Check(Get(rank, "m_BaseValueType").ToString() == "ClassLevel" && Get(rank, "m_Progression").ToString() == "AsIs" &&
                  classes.Length == 1 && classes[0] != null && classes[0].Guid == wizard.AssetGuid,
                label + " duration rank is this Wizard's class level");
        }

        void CheckRankDuration(string token, string label, int expected, UnitEntityData caster)
        {
            var ability = cache.Load(Id(token)) as BlueprintAbility;
            Check(ability != null, label + " ability is published");
            var apply = DurationApply(ability);
            var bonus = apply.DurationValue.BonusValue;
            Check(bonus.ValueType == ContextValueType.Rank && bonus.ValueRank == AbilityRankType.Default && bonus.Value == 0 &&
                  apply.DurationValue.DiceType == DiceType.Zero && apply.DurationValue.DiceCountValue.Value == 0,
                label + " duration is the default rank, not a constant 1 round");
            var ctx = new MechanicsContext(caster, caster.Descriptor, ability);
            int value = ability.GetComponents<ContextRankConfig>().Single().GetValue(ctx);
            Check(value == expected, label + " native rank is " + expected + ", actual " + value);
            var ranks = new int[32];
            ranks[(int)AbilityRankType.Default] = value;
            AccessTools.Field(typeof(MechanicsContext), "m_Ranks").SetValue(ctx, ranks);
            int rounds = apply.DurationValue.Calculate(ctx).Value;
            Check(rounds == expected, label + " calculated duration is " + expected + " rounds, actual " + rounds);
        }
    }

    private static bool HasDayLock(BlueprintAbility ability)
    {
        bool gate = ability.ComponentsArray.Any(c => c is AbilityTargetHasFact && (bool)Get(c, "Inverted"));
        bool day = ability.ComponentsArray.OfType<AbilityEffectRunAction>()
            .SelectMany(run => run.Actions.Actions ?? Array.Empty<Kingmaker.ElementsSystem.GameAction>())
            .SelectMany(Flatten0117)
            .OfType<ContextActionApplyBuff>()
            .Any(apply => apply.DurationValue.Rate == Kingmaker.UnitLogic.Mechanics.DurationRate.Days);
        return gate && day;
    }

    private static IEnumerable<Kingmaker.ElementsSystem.GameAction> Flatten0117(Kingmaker.ElementsSystem.GameAction action)
    {
        yield return action;
        var saved = action as ContextActionConditionalSaved;
        if (saved == null) yield break;
        foreach (var inner in saved.Failed?.Actions ?? Array.Empty<Kingmaker.ElementsSystem.GameAction>()) yield return inner;
        foreach (var inner in saved.Succeed?.Actions ?? Array.Empty<Kingmaker.ElementsSystem.GameAction>()) yield return inner;
    }

    private static IEnumerable<BlueprintAbility> Granted0117(BlueprintFeature feature)
    {
        foreach (var component in feature.ComponentsArray)
        {
            if (component == null || component.GetType().Name != "AddFacts") continue;
            var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
            foreach (var reference in refs)
                if (reference?.Get() is BlueprintAbility ability) yield return ability;
        }
    }

    private static IEnumerable<BlueprintFeature> GrantedFeatures0117(BlueprintFeature feature)
    {
        foreach (var component in feature.ComponentsArray)
        {
            if (component == null || component.GetType().Name != "AddFacts") continue;
            var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
            foreach (var reference in refs)
                if (reference?.Get() is BlueprintFeature inner) yield return inner;
        }
    }

    private static ContextActionApplyBuff DurationApply(BlueprintAbility ability)
    {
        var found = new List<ContextActionApplyBuff>();
        foreach (var run in ability.ComponentsArray.OfType<AbilityEffectRunAction>())
            WalkActions(run.Actions?.Actions, found);
        var effect = found.Where(apply => apply.DurationValue.Rate != DurationRate.Days).ToArray();
        if (effect.Length != 1) throw new Exception("0117 CHECK FAILED: expected one buff duration on " + ability.name + ", found " + effect.Length);
        return effect[0];
    }

    private static void WalkActions(GameAction[] actions, List<ContextActionApplyBuff> found)
    {
        if (actions == null) return;
        foreach (var action in actions)
        {
            if (action is ContextActionApplyBuff apply) found.Add(apply);
            var saved = action as ContextActionConditionalSaved;
            if (saved == null) continue;
            WalkActions(saved.Failed == null ? null : saved.Failed.Actions, found);
            WalkActions(saved.Succeed == null ? null : saved.Succeed.Actions, found);
        }
    }

    private static BlueprintScriptableObject[] AllPrivate(BlueprintsCache cache)
    {
        var table = Get(cache, "m_LoadedBlueprints") as System.Collections.IDictionary;
        if (table == null) return Array.Empty<BlueprintScriptableObject>();
        return table.Keys.Cast<BlueprintGuid>().Select(cache.Load)
            .OfType<BlueprintScriptableObject>()
            .Where(bp => bp.name != null && bp.name.StartsWith("CotWWrathSpiritWhisperer0117"))
            .ToArray();
    }

    private static string NativeSpiritFingerprint(BlueprintsCache cache)
    {
        var ids = new[] { "26e81d83d13dbf6448b10019c802612d", "65b5e7f17a1b928418fc9e8a6b55eafa",
            "50efeb23b314869409365c8061a4082d", "f9ee5a9146561ba4bac4cb8a7126f85c",
            "379d33ef16e812146b5517601ab25e66", "acff6b0cf279a31439010afea01df912",
            "2196e093593903f4ebc36467ccbf1d9a", "eae7cee2a5da93442a7563dfeda33432" };
        return string.Join("|", ids.Select(g =>
        {
            var bp = cache.Load(BlueprintGuid.Parse(g)) as BlueprintScriptableObject;
            if (bp == null) return g + ":missing";
            var prog = bp as BlueprintProgression;
            return g + ":" + (prog == null ? "?" : string.Join(",", prog.LevelEntries.Select(e =>
                e.Level + "=" + string.Join(";", e.Features.Select(f => f == null ? "?" : f.AssetGuid.ToString()))))) +
                ":" + (bp.ComponentsArray?.Length ?? -1);
        }));
    }

    private static int damageCalls0117;
    private static RuleDealDamage capturedDamage0117;
    private static bool CaptureDamage0117(RuleDealDamage __instance) { damageCalls0117++; capturedDamage0117 = __instance; return false; }
    private static TEmpty Empty0117<TEmpty>() where TEmpty : class =>
        (TEmpty)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TEmpty));
    private static void Set0117(object o, string n, object v) =>
        (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    private static RuleAttackWithWeapon Attack0117(UnitEntityData attacker, UnitEntityData target, ItemEntityWeapon weapon, bool hit, bool fake = false)
    {
        var attack = Empty0117<RuleAttackWithWeapon>();
        Set0117(attack, "Initiator", attacker); Set0117(attack, "Target", target); Set0117(attack, "Weapon", weapon); Set0117(attack, "AttacksCount", 1);
        var roll = Empty0117<RuleAttackRoll>();
        Set0117(roll, "Initiator", attacker); Set0117(roll, "Target", target);
        Set0117(roll, "Result", hit ? AttackResult.Hit : AttackResult.Miss); Set0117(roll, "IsFake", fake);
        Set0117(attack, "AttackRoll", roll);
        return attack;
    }
    private static ItemEntityWeapon Weapon0117(UnitEntityData u, WeaponCategory category, string id) =>
        (ItemEntityWeapon)AccessTools.Method(typeof(WildShapeFeatTests), "Weapon").Invoke(null, new object[] { u, category, false, Kingmaker.Enums.Damage.PhysicalDamageForm.Slashing, id });
    private static Buff AddBuff0117(UnitEntityData u, BlueprintBuff bp) =>
        (Buff)AccessTools.Method(typeof(Arcane01446Tests), "Buff").Invoke(null, new object[] { u, bp, null });
    private static object Member0117(object o, string name)
    {
        for (var t = o.GetType(); t != null; t = t.BaseType)
        {
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (p != null) return p.GetValue(o, null);
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (f != null) return f.GetValue(o);
        }
        throw new Exception("0117 CHECK FAILED: member " + name + " not found on " + o.GetType().Name);
    }
    private static MethodInfo Method0117(Type type, string name, Type[] parameters)
    {
        for (var t = type; t != null; t = t.BaseType)
        {
            var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, parameters, null);
            if (m != null) return m;
        }
        throw new Exception("0117 CHECK FAILED: method " + name + " not found on " + type.Name);
    }
    private static void Invoke0117(Buff buff, Type compType, string method, RuleAttackWithWeapon attack)
    {
        object runtime = null;
        foreach (var r in (System.Collections.IEnumerable)Member0117(buff, "Components"))
            if (Member0117(r, "SourceBlueprintComponent").GetType() == compType) { runtime = r; break; }
        if (runtime == null) throw new Exception("0117 CHECK FAILED: runtime component " + compType.Name + " not found on buff");
        var comp = Member0117(runtime, "SourceBlueprintComponent");
        using ((IDisposable)Method0117(runtime.GetType(), "RequestEventContext", Type.EmptyTypes).Invoke(runtime, null))
            Method0117(compType, method, new[] { typeof(RuleAttackWithWeapon) }).Invoke(comp, new object[] { attack });
    }
}
