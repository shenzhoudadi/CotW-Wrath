using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Settings;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Execute the native weapon-stat builder, native VitalStrikePart handlers, native
// bonus aggregation, native maximum-dice calculation and final damage calculator.
// Scene equipment/size, difficulty/faction and shadow policy are explicit inputs.
internal static class DevastatingStrikeTests
{
    static int checks;
    static UnitDescriptor wielder;
    static readonly Type Module = typeof(DevastatingStrikeDamageBonus).Assembly.GetType("CallOfTheWild.Wrath.Prototype.DevastatingStrikeFeat", true);
    static readonly string[] Abilities = { "efc60c91b8e64f244b95c66b270dbd7c", "c714cd636700ac24a91ca3df43326b00", "11f971b6453f74d4594c538e3c88d499" };
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o, string field) => (AccessTools.Field(o.GetType(), field) ?? AccessTools.Field(o.GetType(), "<" + field + ">k__BackingField")).GetValue(o);
    static void Set(object o, string field, object value) => (AccessTools.Field(o.GetType(), field) ?? AccessTools.Field(o.GetType(), "<" + field + ">k__BackingField") ?? throw new Exception("Missing native fixture field: " + field)).SetValue(o, value);
    static void Check(bool b, string why) { if (!b) throw new Exception("Devastating Strike: " + why); checks++; }
    static void Register(BlueprintsCache cache) => AccessTools.Method(Module, "Register").Invoke(null, new object[] { cache });
    static bool Need(PrerequisiteFeature prerequisite, UnitEntityData unit) => (bool)AccessTools.Method(typeof(PrerequisiteFeature), "CheckInternal").Invoke(prerequisite, new object[] { null, unit.Descriptor, null });
    static BlueprintGuid Id() => (BlueprintGuid)AccessTools.Method(typeof(DevastatingStrikeDamageBonus).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "Id").Invoke(null, new object[] { "DevastatingStrikeFeat" });
    static void Refresh(UnitEntityData unit) { var c = Get(unit.Facts, "m_Cache"); AccessTools.Method(c.GetType(), "Clear").Invoke(c, null); }
    static Feature Feature(UnitEntityData owner, BlueprintFeature bp)
    {
        var f = Empty<Feature>(); Set(f, "Components", new List<EntityFactComponent>()); f.RestoreOwnerLink(owner.Facts);
        Set(f, "Rank", 1); AccessTools.Method(typeof(EntityFact), "Setup").Invoke(f, new object[] { bp });
        Set(f, "IsActive", true); Set(f, "IsTurnedOn", true); owner.Facts.List.Add(f); Refresh(owner); return f;
    }
    static UnitEntityData Unit(BlueprintsCache cache, string id)
    {
        var u = Psychic034Tests.Unit(cache, id).Unit; var collection = new FeatureCollection(); collection.Initialize(u.Facts);
        Set(u.Progression, "Features", collection); return u;
    }
    static void Bonus(Feature fact, RuleCalculateWeaponStats rule)
    {
        var runtime = fact.Components.Single(c => c.SourceBlueprintComponent is DevastatingStrikeDamageBonus);
        using (runtime.RequestEventContext()) ((DevastatingStrikeDamageBonus)runtime.SourceBlueprintComponent).OnEventAboutToTrigger(rule);
    }
    static BlueprintFeatureSelection Selection(BlueprintsCache cache, string id)
    {
        var s = cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection;
        if (s != null) return s;
        s = new BlueprintFeatureSelection { AssetGuid = BlueprintGuid.Parse(id), name = "NativeCombatSelection", ComponentsArray = Array.Empty<BlueprintComponent>() };
        Set(s, "m_Features", Array.Empty<BlueprintFeatureReference>()); Set(s, "m_AllFeatures", Array.Empty<BlueprintFeatureReference>());
        cache.AddCachedBlueprint(s.AssetGuid, s); return s;
    }
    static bool Skip() => false;
    static bool False(ref bool __result) { __result = false; return false; }
    static bool Zero(ref int __result) { __result = 0; return false; }
    static bool Wielder(ref UnitDescriptor __result) { __result = wielder; return false; }
    static bool Difficulty(ref DifficultySettings __result) { __result = Empty<DifficultySettings>(); return false; }
    static bool One(ref float __result) { __result = 1f; return false; }
    static bool NoShadow(int __2, ref int __result) { __result = __2; return false; }

    sealed class NativeAttack
    {
        internal AbilityData Ability;
        internal RuleAttackWithWeapon Attack;
        internal AbilityCustomVitalStrike.VitalStrikePart Part;
        internal RuleCalculateWeaponStats Stats;
    }
    static NativeAttack Attack(UnitEntityData owner, UnitEntityData target, ItemEntityWeapon weapon, BlueprintAbility abilityBlueprint)
    {
        var abilityFact = Empty<Ability>();
        Set(abilityFact, "Blueprint", abilityBlueprint);
        var ability = Empty<AbilityData>(); Set(ability, "Blueprint", abilityBlueprint); Set(ability, "m_Fact", abilityFact);
        var attack = new RuleAttackWithWeapon(owner, target, weapon, 0) { IsFirstAttack = true };
        var reason = Empty<RuleReason>(); Set(reason, "Ability", ability); attack.Reason = reason;
        var part = owner.Ensure<AbilityCustomVitalStrike.VitalStrikePart>();
        part.Caster = owner; part.VitalStrikeMod = abilityBlueprint.GetComponent<AbilityCustomVitalStrike>().VitalStrikeMod;
        part.Fact = abilityFact; part.MythicFact = null; part.Rowdy = false;
        return new NativeAttack { Ability = ability, Attack = attack, Part = part, Stats = attack.WeaponStats };
    }
    static RuleCalculateDamage Damage(NativeAttack a, UnitEntityData owner, UnitEntityData target, bool mythic, int critical, bool immune, bool rowdy)
    {
        a.Part.Rowdy = rowdy;
        a.Stats.OnTrigger(null); a.Part.OnEventDidTrigger(a.Stats);
        var chunks = a.Stats.DamageDescription.Select(d => d.CreateDamage()).ToArray();
        chunks[0].CriticalModifier = critical;
        foreach (var d in chunks) d.CalculationType.Set(DamageCalculationType.Maximized, a.Part.Fact);
        var fire = new EnergyDamage(new DiceFormula(1, DiceType.D6), DamageEnergyType.Fire);
        var precision = new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(1, DiceType.D6)), 0, PhysicalDamageForm.Slashing) { Precision = true };
        fire.CalculationType.Set(DamageCalculationType.Maximized, a.Part.Fact); precision.CalculationType.Set(DamageCalculationType.Maximized, a.Part.Fact);
        var bundle = new DamageBundle(chunks) { Weapon = a.Attack.Weapon }; bundle.Add(fire); bundle.Add(precision);
        var roll = Empty<RuleAttackRoll>(); Set(roll, "WeaponStats", a.Stats); Set(a.Attack, "AttackRoll", roll);
        var deal = new RuleDealDamage(owner, target, bundle) { AttackRoll = roll };
        var rule = Empty<RuleCalculateDamage>(); Set(rule, "ParentRule", deal); Set(rule, "Initiator", owner); Set(rule, "Target", target);
        Set(rule, "UnitsCount", 1); Set(rule, "TacticalCombatFactor", 1f); rule.CritImmunity = immune;
        a.Part.MythicFact = mythic ? a.Part.Fact : null;
        a.Part.OnEventAboutToTrigger(rule);
        return rule;
    }
    static int Final(RuleCalculateDamage rule, BaseDamage damage) => ((DamageValue)AccessTools.Method(typeof(RuleCalculateDamage), "CalculateDamageValue").Invoke(rule, new object[] { damage })).FinalValue;

    internal static int Run(BlueprintsCache cache)
    {
        checks = 0; var h = new Harmony("CotW.DevastatingStrike.NativePipeline");
        void Patch(System.Reflection.MethodBase m, string name) => h.Patch(m, prefix: new HarmonyMethod(typeof(DevastatingStrikeTests), name));
        Patch(AccessTools.PropertyGetter(typeof(ItemEntityWeapon), "HoldInTwoHands"), nameof(False));
        Patch(AccessTools.PropertyGetter(typeof(ItemEntity), "EnchantmentValue"), nameof(Zero));
        Patch(AccessTools.PropertyGetter(typeof(ItemEntity), "Wielder"), nameof(Wielder));
        Patch(AccessTools.Method(typeof(RuleCalculateWeaponSizeBonus), "OnTrigger"), nameof(Skip));
        Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip));
        Patch(AccessTools.PropertyGetter(typeof(SettingsRoot), "Difficulty"), nameof(Difficulty));
        Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayerFaction"), nameof(False));
        Patch(AccessTools.PropertyGetter(typeof(RuleCalculateDamage), "TacticalCombatDRModifier"), nameof(One));
        Patch(AccessTools.Method(typeof(ShadowSpellLogicExtension), "TryApplyShadowDamageFactor", new[] { typeof(RulebookEvent), typeof(UnitEntityData), typeof(int) }), nameof(NoShadow));
        try
        {
            var registration = Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration", true);
            var selectionIds = new[] { (string)AccessTools.Field(registration, "BasicFeatsId").GetValue(null) }
                .Concat((string[])AccessTools.Field(registration, "NativeCombatFeatSelectionIds").GetValue(null)).ToArray();
            var selections = selectionIds.Select(id => Selection(cache, id)).ToArray();
            var before = selections.Select(s => new[] { ((BlueprintFeatureReference[])Get(s, "m_Features")).Select(f => f.Guid).ToArray(), ((BlueprintFeatureReference[])Get(s, "m_AllFeatures")).Select(f => f.Guid).ToArray() }).ToArray();
            const string vitalId = "14a1fc1356df9f146900e1e42142fc9d";
            if (cache.Load(BlueprintGuid.Parse(vitalId)) == null) cache.AddCachedBlueprint(BlueprintGuid.Parse(vitalId), PsychicExpansionTests.NativeCase<BlueprintFeature>(vitalId));
            Register(cache); var feat = (BlueprintFeature)cache.Load(Id()); Register(cache);
            Check(ReferenceEquals(cache.Load(Id()), feat), "idempotent blueprint identity");
            Check(feat.Ranks == 1 && feat.Groups.Contains(FeatureGroup.Feat) && feat.Groups.Contains(FeatureGroup.CombatFeat), "single-rank general/combat feat");
            Check(feat.ComponentsArray.Select(c => c.name).Distinct().Count() == feat.ComponentsArray.Length, "unique persistent component names");
            Check(feat.GetComponents<DevastatingStrikeDamageBonus>().Count() == 1, "one damage handler");
            for (int i = 0; i < selections.Length; i++) foreach (string field in new[] { "m_Features", "m_AllFeatures" }) {
                var all = (BlueprintFeatureReference[])Get(selections[i], field); int j = field == "m_Features" ? 0 : 1;
                Check(before[i][j].All(id => all.Any(f => f.Guid == id)), "existing selection entries preserved");
                Check(all.Count(f => f.Guid == feat.AssetGuid) == 1, "exactly one selection entry");
            }
            var missing = new BlueprintsCache(); bool rejected = false; try { Register(missing); } catch { rejected = true; }
            Check(rejected && missing.Load(Id()) == null, "missing dependencies fail closed");
            var collision = new BlueprintsCache(); foreach (var id in selectionIds) Selection(collision, id);
            collision.AddCachedBlueprint(BlueprintGuid.Parse(vitalId), new BlueprintFeature { AssetGuid = BlueprintGuid.Parse(vitalId) });
            var foreign = new BlueprintFeature { AssetGuid = Id(), name = "ForeignDevastatingStrike" }; collision.AddCachedBlueprint(foreign.AssetGuid, foreign);
            rejected = false; try { Register(collision); } catch { rejected = true; }
            Check(rejected && ReferenceEquals(collision.Load(Id()), foreign), "foreign GUID remains unchanged");
            Check(selectionIds.All(id => ((BlueprintFeatureReference[])Get(collision.Load(BlueprintGuid.Parse(id)), "m_AllFeatures")).Length == 0), "collision does not partially add selection entries");
            var optional = new BlueprintsCache();
            string brawlerId = (string)AccessTools.Field(registration, "BrawlerCombatFeatSelectionId").GetValue(null);
            var brawler = Selection(optional, brawlerId);
            AccessTools.Method(Module, "Optional").Invoke(null, new object[] { optional });
            Check(((BlueprintFeatureReference[])Get(brawler, "m_AllFeatures")).Length == 0, "optional provider does not publish an absent feat");
            optional.AddCachedBlueprint(foreign.AssetGuid, foreign);
            AccessTools.Method(Module, "Optional").Invoke(null, new object[] { optional });
            Check(((BlueprintFeatureReference[])Get(brawler, "m_AllFeatures")).Length == 0, "foreign feat collision excluded from optional pool");
            optional.RemoveCachedBlueprint(foreign.AssetGuid); optional.AddCachedBlueprint(feat.AssetGuid, feat);
            AccessTools.Method(Module, "Optional").Invoke(null, new object[] { optional });
            AccessTools.Method(Module, "Optional").Invoke(null, new object[] { optional });
            Check(((BlueprintFeatureReference[])Get(brawler, "m_AllFeatures")).Count(f => f.Guid == feat.AssetGuid) == 1
                && ((BlueprintFeatureReference[])Get(brawler, "m_Features")).Count(f => f.Guid == feat.AssetGuid) == 1, "verified-provider integration is idempotent in both arrays");
            var owner = Unit(cache, "0141-devastating-owner"); var target = Unit(cache, "0141-devastating-target");
            owner.Stats.BaseAttackBonus.BaseValue = 8;
            var stat = feat.GetComponent<PrerequisiteStatValue>(); Check(!stat.CheckUnit(owner.Descriptor), "BAB 8 rejected");
            owner.Stats.BaseAttackBonus.BaseValue = 9; Check(stat.CheckUnit(owner.Descriptor), "BAB 9 accepted");
            var need = feat.GetComponent<PrerequisiteFeature>(); Check(!Need(need, owner), "Vital Strike prerequisite required");
            var vitalFact = Feature(owner, need.Feature); Check(Need(need, owner), "native Vital Strike satisfies prerequisite");
            owner.Facts.List.Remove(vitalFact); Refresh(owner); Check(!Need(need, owner), "retraining prerequisite removes qualification");
            var owned = Feature(owner, feat); owner.Stats.AdditionalDamage.BaseValue = 10; owner.Stats.SneakAttack.BaseValue = 1; wielder = owner.Descriptor;
            var donor = Psychic034Tests.Asset<BlueprintItemWeapon>(cache, "SpiritualWeaponItem");
            var weaponBlueprint = (BlueprintItemWeapon)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(donor, null);
            Set(weaponBlueprint, "m_OverrideDamageType", true); Set(weaponBlueprint, "m_DamageType", new DamageTypeDescription { Type = DamageType.Physical, Physical = new DamageTypeDescription.PhysicalData { Form = PhysicalDamageForm.Slashing } });
            Set(weaponBlueprint, "m_OverrideDamageDice", true);
            var weapon = Empty<ItemEntityWeapon>(); Set(weapon, "m_Blueprint", weaponBlueprint);
            for (int tier = 0; tier < 3; tier++)
            {
                var native = PsychicExpansionTests.NativeCase<BlueprintAbility>(Abilities[tier]);
                Check(native.GetComponent<AbilityCustomVitalStrike>().VitalStrikeMod == tier + 2, "audited native tier " + tier);
                foreach (var dice in new[] { new DiceFormula(1, DiceType.D8), new DiceFormula(2, DiceType.D6) })
                foreach (bool mythic in new[] { false, true }) foreach (int critical in new[] { 1, 2, 4 }) foreach (bool immune in new[] { false, true }) foreach (bool rowdy in new[] { false, true })
                {
                    Set(weaponBlueprint, "m_DamageDice", dice);
                    var baseline = Attack(owner, target, weapon, native); var plain = Damage(baseline, owner, target, mythic, critical, immune, rowdy);
                    int ordinary = Final(plain, plain.DamageBundle.WeaponDamage);
                    var active = Attack(owner, target, weapon, native); Bonus(owned, active.Stats);
                    int bonus = 2 * (tier + 1); int mod = tier + 2;
                    Check(active.Stats.DamageModifiers.Modifiers.Single(m => m.Fact == owned).Value == bonus, "exact attributed tier bonus");
                    var applied = Damage(active, owner, target, mythic, critical, immune, rowdy); var physical = applied.DamageBundle.WeaponDamage;
                    Check(physical.Bonus == 10 + bonus, "native packet receives fixed bonus independent of weapon dice");
                    Check(physical.PostCritIncrements.OverallBonus == (mythic ? (10 + bonus) * (mod - 1) : 0), "native mythic increments preserved");
                    int expectedDelta = bonus * ((immune ? 1 : critical) + (mythic ? mod - 1 : 0));
                    Check(Final(applied, physical) - ordinary == expectedDelta, "actual native final critical/mythic delta");
                    Check(physical.Modifiers.Single(m => m.Fact == owned).Value == bonus, "native damage log source remains feat");
                    var extras = applied.DamageBundle.Skip(1).ToArray(); Check(extras.All(d => !d.Modifiers.Any(m => m.Fact == owned) && d.PostCritIncrements.OverallBonus == 0 && !d.PostCritIncrements.DiceModifiers.Any()), "elemental/precision/Rowdy packets unchanged");
                    Check(extras.Select(d => Final(applied, d)).SequenceEqual(plain.DamageBundle.Skip(1).Select(d => Final(plain, d))), "actual final additional damage unchanged");
                }
                Set(weaponBlueprint, "m_DamageDice", new DiceFormula(1, DiceType.D8));
                foreach (var damageType in new[] { DamageType.Force, DamageType.Energy }) {
                    Set(weaponBlueprint, "m_DamageType", new DamageTypeDescription { Type = damageType });
                    var a = Attack(owner, target, weapon, native); Bonus(owned, a.Stats);
                    Check(!a.Stats.DamageModifiers.Modifiers.Any(m => m.Fact == owned), "native nonphysical weapon dice path excluded");
                }
                Set(weaponBlueprint, "m_DamageType", new DamageTypeDescription { Type = DamageType.Physical, Physical = new DamageTypeDescription.PhysicalData { Form = PhysicalDamageForm.Slashing } });
                foreach (string boundary in new[] { "ordinary", "no-part", "wrong-caster", "wrong-fact", "wrong-tier", "wrong-ability", "missing-component", "not-first", "full-attack", "charge", "AoO", "extra-attack", "iterative", "multiple", "wrong-weapon", "wrong-owner", "inactive", "turned-off", "disposed" })
                {
                    var a = Attack(owner, target, weapon, native); var originalFact = a.Part.Fact;
                    switch (boundary) {
                        case "ordinary": a.Attack.Reason = null; break;
                        case "no-part": owner.Remove<AbilityCustomVitalStrike.VitalStrikePart>(); break;
                        case "wrong-caster": a.Part.Caster = target; break;
                        case "wrong-fact": a.Part.Fact = Empty<Ability>(); break;
                        case "wrong-tier": a.Part.VitalStrikeMod++; break;
                        case "wrong-ability": Set(a.Ability, "Blueprint", new BlueprintAbility { AssetGuid = BlueprintGuid.Parse("006f00af9345114699cb40a514cf4d7b"), ComponentsArray = new BlueprintComponent[] { new AbilityCustomVitalStrike { VitalStrikeMod = tier + 2 } } }); break;
                        case "missing-component": Set(a.Ability, "Blueprint", new BlueprintAbility { AssetGuid = native.AssetGuid, ComponentsArray = Array.Empty<BlueprintComponent>() }); break;
                        case "not-first": a.Attack.IsFirstAttack = false; break;
                        case "full-attack": a.Attack.IsFullAttack = true; break;
                        case "charge": a.Attack.IsCharge = true; break;
                        case "AoO": a.Attack.IsAttackOfOpportunity = true; break;
                        case "extra-attack": a.Attack.ExtraAttack = true; break;
                        case "iterative": Set(a.Attack, "AttackBonusPenalty", 5); break;
                        case "multiple": a.Attack.AttacksCount = 2; break;
                        case "wrong-weapon": Set(a.Attack, "Weapon", Empty<ItemEntityWeapon>()); break;
                        case "wrong-owner": Set(a.Stats, "Initiator", target); break;
                        case "inactive": Set(owned, "IsActive", false); break;
                        case "turned-off": Set(owned, "IsTurnedOn", false); break;
                        case "disposed": Set(owned, "IsDisposed", true); break;
                    }
                    Bonus(owned, a.Stats); Check(!a.Stats.DamageModifiers.Modifiers.Any(m => m.Fact == owned), "negative boundary " + boundary);
                    Set(owned, "IsActive", true); Set(owned, "IsTurnedOn", true); Set(owned, "IsDisposed", false); a.Part.Fact = originalFact;
                }
            }
            var standalone = new RuleCalculateWeaponStats(owner, weapon); Bonus(owned, standalone);
            Check(!standalone.DamageModifiers.Modifiers.Any(m => m.Fact == owned), "separate nonattack weapon statistics excluded");
            // 0.14.1 game feedback: +1 frost dagger (x2) includes another +1
            // source; +1 frost scythe (x4) does not. Neither screenshot is a crit.
            // Execute the native Mythic stage with those exact fixed modifiers;
            // weapon critical capability must not multiply a noncritical attack.
            int beforeFeedback = checks;
            owner.Stats.AdditionalDamage.BaseValue=0;
            var enhancement=Feature(owner,new BlueprintFeature {name="FeedbackEnhancement",ComponentsArray=Array.Empty<BlueprintComponent>()});
            var other=Feature(owner,new BlueprintFeature {name="FeedbackAdditionalPlusOne",ComponentsArray=Array.Empty<BlueprintComponent>()});
            var feedback=new JArray();
            var greater=PsychicExpansionTests.NativeCase<BlueprintAbility>(Abilities[2]);
            foreach(int weaponCritical in new[]{2,4}) foreach(bool plusOne in new[]{false,true}) foreach(bool withFeat in new[]{false,true}) {
                var feedbackType=(BlueprintWeaponType)AccessTools.Method(typeof(object),"MemberwiseClone").Invoke(donor.Type,null);
                Set(feedbackType,"m_CriticalModifier",(DamageCriticalModifierType)weaponCritical);
                var typeRef=BlueprintReferenceBase.CreateTyped<BlueprintWeaponTypeReference>(feedbackType);
                AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(typeRef,feedbackType,null);
                Set(weaponBlueprint,"m_Type",typeRef);
                Set(weaponBlueprint,"m_DamageDice",new DiceFormula(weaponCritical==2?1:2,DiceType.D4));
                var a=Attack(owner,target,weapon,greater);
                a.Stats.AddDamageModifier(1,enhancement,ModifierDescriptor.Enhancement);
                if(plusOne)a.Stats.AddDamageModifier(1,other);
                if(withFeat)Bonus(owned,a.Stats);
                var rule=Damage(a,owner,target,true,1,false,false);var physical=rule.DamageBundle.WeaponDamage;
                int fixedBonus=1+(plusOne?1:0)+(withFeat?6:0);
                Check((int)weaponBlueprint.CriticalModifier==weaponCritical,"feedback weapon critical capability x"+weaponCritical);
                Check(physical.Bonus==fixedBonus && physical.CriticalModifier==1,"feedback exact main fixed bonus without a confirmed critical");
                Check(physical.PostCritIncrements.OverallBonus==fixedBonus*3,"native Greater Mythic increment uses three times current fixed bonus");
                Check(physical.PostCritIncrements.BonusModifiers.Single().Fact==a.Part.Fact,"native log attributes Mythic increment to Greater Vital Strike");
                Check(physical.PostCritIncrements.DiceModifiers.Sum(m=>m.Value)==(weaponCritical==2?3:6),"native extra weapon dice: 1d4 to 4d4 and 2d4 to 8d4");
                Check(Final(rule,physical)==(weaponCritical==2?16:32)+fixedBonus*4,"native final maximized physical damage includes all four fixed bonus sets");
                feedback.Add(new JObject{["weaponCriticalCapacity"]=weaponCritical,["confirmedCritical"]=false,["otherPlusOne"]=plusOne,["withDevastatingStrike"]=withFeat,["mainFixedBonus"]=physical.Bonus,["nativeGreaterMythicBonus"]=physical.PostCritIncrements.OverallBonus});
            }
            Check(feedback.Any(x=>(int)x["weaponCriticalCapacity"]==2&&(bool)x["otherPlusOne"]&&(bool)x["withDevastatingStrike"]&&(int)x["nativeGreaterMythicBonus"]==24)
                && feedback.Any(x=>(int)x["weaponCriticalCapacity"]==4&&!(bool)x["otherPlusOne"]&&(bool)x["withDevastatingStrike"]&&(int)x["nativeGreaterMythicBonus"]==21),"both screenshot increments reproduced by the native formula");
            PsychicExpansionTests.Inventory["devastatingFeedback0142Assertions"]=checks-beforeFeedback;
            PsychicExpansionTests.Inventory["devastatingFeedback0142Cases"]=feedback;
            Check(feat.GetComponents<DevastatingStrikeDamageBonus>().Single().GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).Length == 0, "no persisted combat tracker or stale tier fields");
            PsychicExpansionTests.Inventory["devastatingStrikeAssets"] = new JArray(new JObject { ["token"] = "DevastatingStrikeFeat", ["guid"] = feat.AssetGuid.ToString(), ["name"] = feat.name, ["type"] = feat.GetType().Name });
            PsychicExpansionTests.Inventory["devastatingStrikeAssertions"] = checks;
        }
        finally { h.UnpatchAll(h.Id); wielder = null; }
        Console.WriteLine("PASS: " + checks + " Devastating Strike checks: native prerequisites, selection identity, weapon bonus, three native Vital Strike tiers, Mythic/critical final damage and additional-damage exclusions; scene equipment/difficulty/shadow are bounded inputs.");
        return checks;
    }
}
