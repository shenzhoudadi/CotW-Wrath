using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics;
using CallOfTheWild.Wrath.Prototype;

internal static class Psychic070Tests
{
    private static int count;
    private static Type factory;
    private static int healAmount, defenseCalls;
    private static TimeSpan? defenseDuration;
    private static BlueprintBuff defenseBlueprint;
    private static Kingmaker.Game fixtureGame;
    private static readonly List<BlueprintScriptableObject> newAssets=new List<BlueprintScriptableObject>();
    private static void Check(bool ok,string message) { if(!ok) throw new Exception(message);count++; }
    private static object Get(object obj,string name)=>AccessTools.Field(obj.GetType(),name).GetValue(obj);
    private static void Set(object obj,string name,object value)=>AccessTools.Field(obj.GetType(),name).SetValue(obj,value);
    private static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    private static T Ref<T>(SimpleBlueprint bp) where T:BlueprintReferenceBase,new() {
        var r=BlueprintReferenceBase.CreateTyped<T>(bp);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,bp,null);return r;
    }
    private static T Asset<T>(BlueprintsCache cache,string token) where T:SimpleBlueprint => (T)cache.Load((BlueprintGuid)AccessTools.Method(factory,"Id").Invoke(null,new object[]{token}));
    static void ExportStrings01431() {
        var strings=(Dictionary<string,string>)AccessTools.Field(factory,"Strings").GetValue(null);
        PsychicExpansionTests.Inventory["strings01431"]=JObject.FromObject(strings.Where(p=>new[] { "Anger", "FerociousBeast", "UndeadSchool", "UndeadBolster", "UndeadCommand", "Prophecy" }.Any(t=>p.Key.StartsWith("cotw-wrath.psychic.full."+t,StringComparison.Ordinal))).ToDictionary(p=>p.Key,p=>p.Value));
    }
    public static int Run(Assembly assembly,BlueprintsCache cache,string evidence)
    {
        factory=assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets",true);
        Console.WriteLine("Psychic 0.7.0: amplification registration and native rule/resource fixtures");
        var registration=AccessTools.Method(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAmplifications"),"Register");
        var all=(IDictionary)Get(cache,"m_LoadedBlueprints");var before=new HashSet<BlueprintGuid>(all.Keys.Cast<BlueprintGuid>());
        var basic=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("247a4068296e8be42890143f451b4b45"));
        var oldBasic=(BlueprintFeatureReference[])Get(basic,"m_Features");
        var stray=new BlueprintFeatureReference();Set(stray,"deserializedGuid",(BlueprintGuid)AccessTools.Method(factory,"Id").Invoke(null,new object[]{"ExpandedPhrenicPoolFeat"}));
        Set(basic,"m_Features",oldBasic.Concat(new[]{stray}).ToArray());
        bool partialRejected=false;
        try {registration.Invoke(null,new object[]{cache});}catch(TargetInvocationException e)when(e.InnerException is InvalidOperationException){partialRejected=true;}
        Set(basic,"m_Features",oldBasic);
        Check(partialRejected && all.Count==before.Count,"Stray feat links reject registration before any assets are added");
        registration.Invoke(null,new object[]{cache});
        newAssets.AddRange(all.Keys.Cast<BlueprintGuid>().Where(g=>!before.Contains(g)).Select(g=>(BlueprintScriptableObject)cache.Load(g)));
        Check(newAssets.Count==122,"Amplification assets include four navigation heads and four inert navigation buffs");
        var nativeIds=new HashSet<string>(JObject.Parse(System.IO.File.ReadAllText(evidence))["allNativeIds"].Select(t=>(string)t));
        Check(newAssets.All(a=>!nativeIds.Contains(a.AssetGuid.ToString())) && newAssets.Select(a=>a.AssetGuid).Distinct().Count()==122,"New amplification identities do not collide with any installed game blueprint or each other");
        var choice=Asset<BlueprintFeatureSelection>(cache,"PhrenicAmplificationSelection");
        Check(choice.AllFeatures.Length==9 && choice.AllFeatures.All(f=>f.Ranks==1) && choice.Ranks==8,"Nine unique options support class and additional feat choices");
        var progression=(BlueprintProgression)cache.Load(BlueprintGuid.Parse("b14115fd5dd15c59a6075b10ef73f4ee"));
        Check(progression.LevelEntries.Where(e=>e.Features.Contains(choice)).Select(e=>e.Level).SequenceEqual(new[]{1,3,7,11,15,19,20}),"Choices at all six progression levels and mastery");
        var modes=newAssets.OfType<BlueprintActivatableAbility>().ToArray();
        foreach(var mode in modes) {
            Check(mode.Group==ActivatableAbilityGroup.None && !mode.ComponentsArray.OfType<RestrictionHasFact>().Any(),"Sibling modes remain available for component-driven switching");
            Check(mode.ComponentsArray.OfType<RestrictionHasFact>().All(r=>r.Not && r.Feature!=mode.Buff) && !mode.ComponentsArray.OfType<RestrictionHasFact>().Any(),"Every other mode buff, and no own buff, is restricted");
            Check(!ReferenceEquals(mode.Icon,null) && !ReferenceEquals(mode.Buff.Icon,null) && mode.Buff.FxOnRemove!=null && mode.Buff.FxOnStart!=null,"Mode icons and valid removal links initialized");
        }
        registration.Invoke(null,new object[]{cache});registration.Invoke(null,new object[]{cache});
        Check(newAssets.All(a=>ReferenceEquals(cache.Load(a.AssetGuid),a)) && progression.LevelEntries.Sum(e=>e.Features.Count(f=>f==choice))==8,"Repeat registration keeps identities and single choices");
        var component=Asset<BlueprintBuff>(cache,"OverpoweringMind1Mode").GetComponent<PsychicAmplificationComponent>();
        int previous=component.Cost;component.Cost=99;bool rejected=false;
        try {registration.Invoke(null,new object[]{cache});} catch(TargetInvocationException e) when(e.InnerException is InvalidOperationException) {rejected=true;}
        component.Cost=previous;
        Check(rejected && newAssets.All(a=>ReferenceEquals(cache.Load(a.AssetGuid),a)),"Component collision rejected without overwriting existing assets");
        TestRules(cache,evidence);
        count+=Psychic090Tests.NativeEligibility(evidence);
        TestLinks(assembly,cache,evidence);
        PsychicExpansionTests.Inventory["phrenicAssets"]=JArray.FromObject(newAssets.Select(a=>new {guid=a.AssetGuid.ToString(),name=a.name,type=a.GetType().Name}));
        PsychicExpansionTests.Inventory["psychic070Assertions"]=count;
        PsychicExpansionTests.Inventory["englishStrings"]=JObject.FromObject(AccessTools.Field(factory,"Strings").GetValue(null));
        Console.WriteLine("PASS: "+count+" optional integration/amplification assertions. Native managed types, no game UI/combat/save test.");
        return count;
    }
    private static void TestRules(BlueprintsCache cache,string evidence)
    {
        var f=new Caster(cache);
        var harmony=new Harmony("CotWPsychic070.NativeBoundaries");
        // Keep native resource arithmetic and component handlers; intercept only
        // the final healing rule/buff installation, which require a live game.
        fixtureGame=Empty<Kingmaker.Game>();Set(fixtureGame,"Rulebook",new Rulebook());
        harmony.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Game),"Instance"),prefix:new HarmonyMethod(AccessTools.Method(typeof(Psychic070Tests),nameof(GameInstance))));
        harmony.Patch(AccessTools.Method(typeof(RuleHealDamage),"OnTrigger"),prefix:new HarmonyMethod(AccessTools.Method(typeof(Psychic070Tests),nameof(CaptureHealing))));
        harmony.Patch(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel),"Exception",new[]{typeof(Exception),typeof(string),typeof(object[])}),prefix:new HarmonyMethod(AccessTools.Method(typeof(Psychic070Tests),nameof(FailLog))));
        harmony.Patch(AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),prefix:new HarmonyMethod(AccessTools.Method(typeof(Psychic070Tests),nameof(CaptureDefense))));
        // Native resource max calculation checks player faction for modifiers.
        harmony.Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor),"IsPlayerFaction"),prefix:new HarmonyMethod(AccessTools.Method(typeof(Psychic070Tests),nameof(Faction))));
        // Installed DLC state is a scene input; all native rules remain loaded.
        harmony.Patch(AccessTools.Method(typeof(Kingmaker.DLC.DlcConditionExtension),"IsDlcRestricted"),prefix:new HarmonyMethod(typeof(Psychic070Tests),nameof(NoDlcRestriction)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(ModifiableValueAttributeStat),"MinValueRaw"),prefix:new HarmonyMethod(AccessTools.Method(typeof(Psychic070Tests),nameof(MinimumAttribute))));
        try {
            if(Environment.GetEnvironmentVariable("COTW_DOOM_CASTING_01444_ONLY")=="1") {
                if(Environment.GetEnvironmentVariable("COTW_DOOM_PRELUDE_044")=="1") { count+=RegistrationWarmup01443Tests.Run(cache,evidence);count+=WarDrummerWarmup01443Tests.Run(cache);count+=Sunsinger01443Tests.Run(cache);count+=UrbanSkald01443Tests.Run(cache);count+=RogueSwashbuckler01443Tests.Run(cache);count+=HedgeWitch01443Tests.Run(cache);count+=Archetypes01444Tests.Run(cache); }
                count += AuraOfDoomCasting01444Tests.Run(cache); ExportStrings01431(); return;
            }
            if(Environment.GetEnvironmentVariable("COTW_STARTUP_RETRY_01444_ONLY")=="1") { count += StartupRetry01444Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_ORDER_01444_ONLY")=="1") { count += RegistrationOrder01444Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_REGWARM_01443_ONLY")=="1") { count += RegistrationWarmup01443Tests.Run(cache, evidence); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_REPAIR_01444_ONLY")=="1") { count += Archetypes01444Tests.Run(cache, "all"); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_STARTUP_ARCHETYPES_01445_ONLY")=="1") { count += StartupArchetypes01445Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_STARTUP_SPELL_BATCH_01447_ONLY")=="1") { count += StartupSpellBatch01447Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_STARTUP_ARCANE_01446_ONLY")=="1") { count += StartupArcane01446Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_ARCANE_01446_ONLY")=="1") { count += Arcane01446Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_ARCHETYPES_01445_ONLY")=="1") { count += Archetypes01445Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_ARCHETYPES_01444_ONLY")=="1") { count += Archetypes01444Tests.Run(cache, "all"); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_TERROR_WEAVER_01444_ONLY")=="1") { count += Archetypes01444Tests.Run(cache, "terror"); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_PRIMAL_SORCERER_01444_ONLY")=="1") { count += Archetypes01444Tests.Run(cache, "primal"); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_RAKE_01444_ONLY")=="1") { count += Archetypes01444Tests.Run(cache, "rake"); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_HEDGE_WITCH_01443_ONLY")=="1") { count += HedgeWitch01443Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_WAR_WARMUP_01443_ONLY")=="1") { count += WarDrummerWarmup01443Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_SUNSINGER_01443_ONLY")=="1") { count += Sunsinger01443Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_URBAN_SKALD_01443_ONLY")=="1") { count += UrbanSkald01443Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_ROGUE_SWASHBUCKLER_01443_ONLY")=="1") { count += RogueSwashbuckler01443Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_CALLER_01443_ONLY")=="1") { count += Migration01443CallerTests.Run(cache, evidence); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_MIGRATION_01443_ONLY")=="1") { count += RegistrationWarmup01443Tests.Run(cache, evidence); count += WarDrummerWarmup01443Tests.Run(cache); count += Sunsinger01443Tests.Run(cache); count += UrbanSkald01443Tests.Run(cache); count += RogueSwashbuckler01443Tests.Run(cache); count += Migration01443CallerTests.Run(cache, evidence); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_NATIVE_SPELLRESOURCE_ONLY")=="1") { count += SpellResourceFeatTests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_DISCOVERY_01438_ONLY")=="1") { count += DiscoveryAccess01438Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_TIME_STOP_01438_ONLY")=="1") { count += TimeStutter01437Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_CALLER_01438_ONLY")=="1") { count += Repair01438CallerTests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_IDR_01439_ONLY")=="1") { count += IncreasedDamageReduction01439Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_WAR_DRUMMER_01441_ONLY")=="1") { count += WarDrummer01441Tests.Run(cache); count += WarDrummerWarmup01443Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_TIME_STUTTER_01437_ONLY")=="1") { count += TimeStutter01437Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_INVOKER_01437_ONLY")=="1") { count += Invoker01437Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_CALLER_01437_ONLY")=="1") { count += Migration01437CallerTests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_MIGRATION_01437_ONLY")=="1") { count += TimeStutter01437Tests.Run(cache); count += Invoker01437Tests.Run(cache); count += Migration01437CallerTests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_REPAIR_01436_ONLY")=="1") { count += WizardDiscoveries01435Tests.Run(cache); count += BardMasterpieces01435Tests.Run(cache); count += Repair01436CallerTests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_CALLER_01436_ONLY")=="1") { count += Repair01436CallerTests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_WIZARD_01435_ONLY")=="1") { count += WizardDiscoveries01435Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_BARD_01435_ONLY")=="1") { count += BardMasterpieces01435Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_CALLER_01435_ONLY")=="1") { count += Repair01436CallerTests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_MIGRATION_01435_ONLY")=="1") { count += WizardDiscoveries01435Tests.Run(cache); count += BardMasterpieces01435Tests.Run(cache); count += Repair01436CallerTests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_SUPERSTITION_01434_ONLY")=="1") { count += SuperstitionRage01434Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_ANTIMAGIC_01434_ONLY")=="1") { count += AntimagicRage01434Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_CALLER_01434_ONLY")=="1") { count += Migration01434CallerTests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_MIGRATION_01434_ONLY")=="1") { count += SuperstitionRage01434Tests.Run(cache); count += AntimagicRage01434Tests.Run(cache); count += Migration01434CallerTests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_INVOCATION_01433_ONLY")=="1") { count += KineticInvocation01433Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_MIGRATION_01433_ONLY")=="1") { count += SpiritualGuardian01433Tests.Run(cache); count += KineticUtility01433Tests.Run(cache); count += KineticInvocation01433Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_GUARDIAN_01433_ONLY")=="1") { count += SpiritualGuardian01433Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_KINETIC_01433_ONLY")=="1") { count += KineticUtility01433Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_REGISTRATION_01432_ONLY")=="1") {
                count += RegistrationFeatKi01432Tests.Run(cache);
                count += Ki01427Tests.Run(cache);
                count += FerociousBeast01431Tests.Run(cache);
                count += AngerInquisition01431Tests.Run(cache);
                count += RegistrationRemoval01432Tests.Run(cache);
                ExportStrings01431();
                return;
            }
            if(Environment.GetEnvironmentVariable("COTW_FEATKI_01432_ONLY")=="1") { count += RegistrationFeatKi01432Tests.Run(cache); count += Ki01427Tests.Run(cache); return; }
            if(Environment.GetEnvironmentVariable("COTW_REMOVAL_01432_ONLY")=="1") { count += RegistrationRemoval01432Tests.Run(cache); return; }
            if(Environment.GetEnvironmentVariable("COTW_RAGE_01431_ONLY")=="1") { count+=FerociousBeast01431Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_WIZARD_01431_ONLY")=="1") { count+=WizardSubschools01431Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_ANGER_01431_ONLY")=="1") { count+=AngerInquisition01431Tests.Run(cache); ExportStrings01431(); return; }
            if(Environment.GetEnvironmentVariable("COTW_MIGRATION_01431_ONLY")=="1") {
                count += FerociousBeast01431Tests.Run(cache);
                count += AngerInquisition01431Tests.Run(cache);
                count += WizardSubschools01431Tests.Run(cache);
                ExportStrings01431();
                return;
            }
            if(Environment.GetEnvironmentVariable("COTW_MIGRATION_01429_ONLY")=="1") {
                count += Migration01429Tests.Run(cache);
                count += EnhancementLifecycle01430Tests.Run(cache);
                count += FlameJetLifecycle01430Tests.Run(cache);
                return;
            }
            if(Environment.GetEnvironmentVariable("COTW_PURIFY_01428_ONLY")=="1") { count += PurifyingFlames01428Tests.Run(cache); return; }
            if(Environment.GetEnvironmentVariable("COTW_WIZARD_01428_ONLY")=="1") { count += WizardSubschools01428Tests.Run(cache); return; }
            if(Environment.GetEnvironmentVariable("COTW_INQUISITIONS_01428_ONLY")=="1") { count += Inquisitions01428Tests.Run(cache); return; }
            if(Environment.GetEnvironmentVariable("COTW_HOWL_01428_ONLY")=="1") { AccessTools.Method(factory.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TerrifyingHowl"), "Register").Invoke(null, new object[] { cache }); count += Howl01428Tests.Run(cache); return; }
            if(Environment.GetEnvironmentVariable("COTW_FEEDBACK_01419_ONLY")=="1") {count+=Feedback01419Tests.Run(cache);count+=Provider01419Tests.Run(cache);return;}
            if(Environment.GetEnvironmentVariable("COTW_PAIN_01421_ONLY")=="1") {count+=Feedback01421Tests.Pain(cache,factory.Assembly);return;}
            if(Environment.GetEnvironmentVariable("COTW_FEINT_01421_ONLY")=="1") {count+=Feedback01421Tests.Feint(cache);return;}
            foreach(var mode in newAssets.OfType<BlueprintBuff>().Where(b=>b.GetComponent<PsychicAmplificationComponent>()!=null)) {
                var c=mode.GetComponent<PsychicAmplificationComponent>();
                if((int)c.Kind>=3) continue; // New linked SR/duration paths have their own fixtures below.
                f.Class.Level=c.MinimumLevel;
                var school=c.Kind==PsychicAmplificationKind.BiokineticHealing?SpellSchool.Transmutation:c.Kind==PsychicAmplificationKind.DefensivePrognostication?SpellSchool.Divination:SpellSchool.Enchantment;
                var descriptor=c.Kind==PsychicAmplificationKind.OverpoweringMind?SpellDescriptor.MindAffecting:SpellDescriptor.None;
                f.Amount=10;var rule=f.Cast(school,descriptor,3);var shared=rule.Context.Params;
                healAmount=defenseCalls=0;c.OnEventAboutToTrigger(rule);c.OnEventAboutToTrigger(rule);
                Check(f.Amount==10-c.Cost,"Eligible cast reserves cost exactly once: "+mode.name);
                Check(shared.DC==20 && rule.Context.Params.DC==(c.Kind==PsychicAmplificationKind.OverpoweringMind?20+c.Strength:20),"Paid DC changes only the current execution context");
                Kingmaker.Controllers.AbilityExecutionProcess.PrepareCast(rule.Context);
                Check(rule.Context.Params.DC==(c.Kind==PsychicAmplificationKind.OverpoweringMind?20+c.Strength:20),"Paid DC survives native PrepareCast and context recalculation for ordinary spells");
                rule.SetSuccess(true);c.OnEventDidTrigger(rule);c.OnEventDidTrigger(rule);
                Check(f.Amount==10-c.Cost,"Successful cast commits a single charge");
                Check(healAmount==(c.Kind==PsychicAmplificationKind.BiokineticHealing?6:0),"Healing routed once with twice the spell level: "+mode.name+" actual="+healAmount);
                Check(defenseCalls==(c.Kind==PsychicAmplificationKind.DefensivePrognostication?1:0),"Defense applied once, only by its mode");
                if(defenseCalls==1) Check(defenseBlueprint==c.Defense.Get() && defenseDuration==TimeSpan.FromSeconds(18),"Three-circle defense routes the correct insight buff for exactly three rounds");
                f.Amount=8;rule=f.Cast(school,descriptor,3);c.OnEventAboutToTrigger(rule);c.OnEventDidTrigger(rule);c.OnEventDidTrigger(rule);
                Check(f.Amount==8 && rule.Context.Params.DC==20,"Failed cast refunds once and removes only its DC change");
                f.Amount=c.Cost-1;rule=f.Cast(school,descriptor,3);c.OnEventAboutToTrigger(rule);rule.SetSuccess(true);c.OnEventDidTrigger(rule);
                Check(f.Amount==c.Cost-1 && rule.Context.Params.DC==20,"Insufficient points do not spend, boost or prevent the ordinary spell");
                foreach(int branch in Enumerable.Range(0,5)) {
                    f.Amount=8;rule=f.Cast(school,descriptor,branch==0?0:3);
                    if(branch==1) rule.ForceFail=true;
                    if(branch==2) rule.IsDuplicateSpellApplied=true;
                    if(branch==3) rule.Context.IsDuplicateSpellApplied=true;
                    if(branch==4) { var bp=rule.Spell.Blueprint;bp.ComponentsArray=new BlueprintComponent[]{new SpellComponent {School=SpellSchool.Evocation}};bp.OnEnable();Set(rule.Context,"<SpellDescriptor>k__BackingField",SpellDescriptor.None); }
                    c.OnEventAboutToTrigger(rule);rule.SetSuccess(true);c.OnEventDidTrigger(rule);
                    Check(f.Amount==8 && rule.Context.Params.DC==20,"Cantrip/forced failure/duplicate/nonmatching spell stays unmodified: "+branch);
                }
                if(c.MinimumLevel>1) {
                    f.Class.Level=c.MinimumLevel-1;f.Amount=8;rule=f.Cast(school,descriptor,3);c.OnEventAboutToTrigger(rule);
                    Check(f.Amount==8 && rule.Context.Params.DC==20,"High tier cannot apply before required Psychic level");
                }
            }
            var mind=Asset<BlueprintBuff>(cache,"OverpoweringMind1Mode").GetComponent<PsychicAmplificationComponent>();
            f.Class.Level=20;f.Amount=8;
            var first=f.Cast(SpellSchool.Enchantment,SpellDescriptor.MindAffecting,1);var second=f.Cast(SpellSchool.Enchantment,SpellDescriptor.MindAffecting,2);
            mind.OnEventAboutToTrigger(first);mind.OnEventAboutToTrigger(second);first.SetSuccess(true);mind.OnEventDidTrigger(first);mind.OnEventDidTrigger(second);
            Check(f.Amount==6 && first.Context.Params.DC==21 && second.Context.Params.DC==20,"Nested successful/failed casts keep their own reservations and DC");
            var other=Asset<BlueprintBuff>(cache,"OverpoweringMind2Mode").GetComponent<PsychicAmplificationComponent>();
            f.Amount=8;first=f.Cast(SpellSchool.Enchantment,SpellDescriptor.MindAffecting,1);mind.OnEventAboutToTrigger(first);other.OnEventAboutToTrigger(first);first.SetSuccess(true);other.OnEventDidTrigger(first);mind.OnEventDidTrigger(first);
            Check(f.Amount==6 && first.Context.Params.DC==21,"Corrupt simultaneous modes cannot double boost/charge one cast");
            f.Amount=8;first=f.Cast(SpellSchool.Enchantment,SpellDescriptor.MindAffecting,1);
            var touch=Empty<Ability>();Set(touch,"<StickyTouch>k__BackingField",second.Spell);Set(first.Spell,"m_Fact",touch);
            mind.OnEventAboutToTrigger(first);Check(f.Amount==8 && first.Context.Params.DC==20,"Stored touch delivery cannot charge or amplify the same spell again");
            // A marked external psychic book is not the Psychic class spellbook.
            var original=f.Book.Blueprint;var foreign=new BlueprintSpellbook {AssetGuid=BlueprintGuid.Parse("3344556677889900aabbccddeeff1133"),ComponentsArray=original.ComponentsArray};foreign.OnEnable();
            Set(f.Book,"Blueprint",foreign);f.Amount=8;first=f.Cast(SpellSchool.Enchantment,SpellDescriptor.MindAffecting,1);mind.OnEventAboutToTrigger(first);
            Check(f.Amount==8,"Medium/other marked spellbooks cannot borrow Psychic-only amplifications");Set(f.Book,"Blueprint",original);
            TestNewRules(cache,f,harmony);
            count+=Psychic090Tests.Run(cache,f,harmony);
            count+=Psychic092Tests.Run(cache,f,harmony);
            count+=Psychic093Tests.Run(cache,f);
            count+=PsychicFaithTests.Run(cache,f);
            count+=PsychicAfflictionTests.Run(cache,f);
            count+=PsychicMenuTests.Run(cache);
            count+=PsychicForceTests.Run(cache);
            count+=PsychicCrushTests.Run(cache);
            count+=PsychicPageantryTests.Run(cache);
            count+=PsychicInsightTests.Run(cache);
            count+=PsychicComfortTests.Run(cache);
            count+=PsychicUndeadTests.Run(cache);
            count+=PsychicWardingTests.Run(cache);
            count+=PsychicBloodTests.Run(cache);
            count+=PsychicMawCleanupTests.Run(cache);
            count+=PsychicFluidFormTests.Run(cache);
            count+=PsychicChannelVigorTests.Run(cache);
            count+=PsychicBladeTutorTests.Run(cache);
            count+=PsychicSecondWindTests.Run(cache);
            count+=PsychicGhostbaneTests.Run(cache);
            count+=PsychicBloodyClawsTests.Run(cache);
            count+=PsychicBattleSpellTests.Run(cache);
            count+=PsychicWeaponSpellTests.Run(cache);
            count+=PsychicTacticalTests.Run(cache);
            count+=PsychicAreaTests.Run(cache);
            count+=PsychicAspectTests.Run(cache);
            count+=PsychicWeatherTests.Run(cache);
            count+=PsychicCurseTests.Run(cache);
            count+=PsychicSupportTests.Run(cache);
            count+=PsychicTravelTests.Run(cache);
            count+=PsychicOrbTests.Run(cache);
            count+=PsychicCataclysmTests.Run(cache);count+=PsychicShapeTests.Run(cache);count+=PsychicConduitTests.Run(cache);count+=PsychicSpiritTests.Run(cache);count+=Psychic025Tests.Run(cache);count+=Psychic027Tests.Run(cache);count+=Psychic031Tests.Run(cache)+Psychic030Tests.Run(cache)+Psychic029Tests.Run(cache)+Psychic028Tests.Run(cache)+Psychic032Tests.Run(cache)+Psychic033Tests.Run(cache)+Psychic034Tests.Run(cache)+Psychic035Tests.Run(cache)+T09FeatTests.Run(cache)+DevastatingStrikeTests.Run(cache)+FeralGraceTests.Run(cache)+T11TrainingTests.Run(cache)+TrainingFeedbackTests.Run(cache)+SpellResourceFeatTests.Run(cache)+ChosenWeaponFeatTests.Run(cache)+Spirit0148Tests.Run(cache)+CombatFeatPoolTests.Run(cache)+WildShapeFeatTests.Run(cache)+TeamworkFeatTests.Run(cache)+Feedback01412Tests.Run(cache)+Ally01413Tests.Run(cache)+CooperationFeatTests.Run(cache);
            count+=CompanionSharing01414Tests.Run(cache)+Gambit01414Tests.Run(cache)+CooperationIcon01414Tests.Run(cache);
            count+=Feedback01415Tests.Run(cache);
            count+=Linnorm01416Tests.Run(cache);
            count+=SpellAndSword01417Tests.Run(cache);
            count+=Provider01419Tests.Run(cache);
            count+=Compatibility01420Tests.Run(cache);
            count+=Feedback01421Tests.Pain(cache,factory.Assembly)+Feedback01421Tests.Feint(cache);
            count+=Feedback01422CloneTests.Run(cache);
            count+=Feat01424Tests.Run(cache);
            count+=Talent01425Tests.Run(cache);
            count+=Prerequisites01426Tests.Run(cache);
            count+=Migration01426Tests.Run(cache, evidence);
            count+=Howl01427Tests.Run(cache);
            count+=Howl01428Tests.Run(cache);
            count+=Ki01427Tests.Run(cache);
            count+=Inquisitions01428Tests.Run(cache);
            count+=WizardSubschools01428Tests.Run(cache);
            count+=PurifyingFlames01428Tests.Run(cache);
            count+=Migration01429Tests.Run(cache);
            count+=EnhancementLifecycle01430Tests.Run(cache);
            count+=FlameJetLifecycle01430Tests.Run(cache);
            count+=FerociousBeast01431Tests.Run(cache);
            count+=AngerInquisition01431Tests.Run(cache);
            count+=WizardSubschools01431Tests.Run(cache);
            count+=RegistrationFeatKi01432Tests.Run(cache);
            count+=RegistrationRemoval01432Tests.Run(cache);
            count+=SpiritualGuardian01433Tests.Run(cache);
            count+=KineticUtility01433Tests.Run(cache);
            count+=KineticInvocation01433Tests.Run(cache);
            count+=SuperstitionRage01434Tests.Run(cache);
            count+=AntimagicRage01434Tests.Run(cache);
            count+=Migration01434CallerTests.Run(cache);
            count+=WizardDiscoveries01435Tests.Run(cache);
            count+=BardMasterpieces01435Tests.Run(cache);
            count+=Repair01436CallerTests.Run(cache);
            count+=DiscoveryAccess01438Tests.Run(cache);
            count+=TimeStutter01437Tests.Run(cache);
            count+=Invoker01437Tests.Run(cache);
            count+=Migration01437CallerTests.Run(cache);
            count+=Repair01438CallerTests.Run(cache);
            count+=IncreasedDamageReduction01439Tests.Run(cache);
            count+=WarDrummer01441Tests.Run(cache);
            count += RegistrationWarmup01443Tests.Run(cache, evidence);
            count += WarDrummerWarmup01443Tests.Run(cache);
            count += Sunsinger01443Tests.Run(cache);
            count += UrbanSkald01443Tests.Run(cache);
            count += RogueSwashbuckler01443Tests.Run(cache);
            count += HedgeWitch01443Tests.Run(cache);
            count += Archetypes01444Tests.Run(cache);
            count += Archetypes01445Tests.Run(cache);
            count += Arcane01446Tests.Run(cache);
            count += StartupArcane01446Tests.Run(cache);
            count += StartupSpellBatch01447Tests.Run(cache);
            count += StartupArchetypes01445Tests.Run(cache);
            count += AuraOfDoomCasting01444Tests.Run(cache);
            count += RegistrationOrder01444Tests.Run(cache);
            count += StartupRetry01444Tests.Run(cache);
            count += Migration01443CallerTests.Run(cache, evidence);
            ExportStrings01431();
            count+=Urgent01418Tests.Run(cache,evidence);
            count+=Feedback01419Tests.Run(cache);
            count+=PsychicGlobeTests.Run(cache);
            count+=PsychicCompulsionTests.Run(cache);
        } finally {harmony.UnpatchAll(harmony.Id);}
    }
    private static readonly Queue<int> dice=new Queue<int>();
    private static bool NoDlcRestriction(ref bool __result) {__result=false;return false;}
    private static bool ControlledDice(DiceFormula __0,ref int __result) {__result=__0.Rolls==0?0:dice.Dequeue();return false;}
    internal static IDisposable Scope(PsychicAmplificationComponent c) {
        var fact=Empty<Buff>();Set(fact,"<Blueprint>k__BackingField",c.OwnerBlueprint);
        var runtime=c.CreateRuntimeFactComponent();runtime.Setup(c);runtime.RestoreOwnerLink(fact);
        return runtime.RequestEventContext();
    }
    private static void TestNewRules(BlueprintsCache cache,Caster f,Harmony harmony)
    {
        var asm=typeof(PsychicAmplificationComponent).Assembly;
        foreach(var type in new[]{"PsychicRelentlessCastingPatch","PsychicOngoingDefensePatch"})
            harmony.CreateClassProcessor(asm.GetType("CallOfTheWild.Wrath.Prototype."+type)).Patch();
        harmony.Patch(AccessTools.Method(typeof(RulebookEvent.Dice),"D",new[]{typeof(DiceFormula)}),
            prefix:new HarmonyMethod(AccessTools.Method(typeof(Psychic070Tests),nameof(ControlledDice))));
        var ongoing=Asset<BlueprintBuff>(cache,"OngoingDefenseMode").GetComponent<PsychicAmplificationComponent>();
        var relentless=Asset<BlueprintBuff>(cache,"RelentlessCastingMode").GetComponent<PsychicAmplificationComponent>();
        f.Class.Level=20;
        var tokens=Enumerable.Range(1,5).Select(n=>"MentalBarrier"+n).Concat(Enumerable.Range(1,3).Select(n=>"ThoughtShield"+n)).Concat(new[]{"IntellectFortress"});
        RuleCastSpell Cast(BlueprintAbility bp,int circle=4) {
            var rule=f.Cast(bp.School,SpellDescriptor.None,circle);Set(rule.Spell,"Blueprint",bp);Set(rule.Context,"AssociatedBlueprint",bp);Set(rule.Context,"<ClickedTarget>k__BackingField",Empty<Kingmaker.Utility.TargetWrapper>());return rule;
        }
        foreach(var token in tokens) foreach(bool extend in new[]{false,true}) {
            var bp=Asset<BlueprintAbility>(cache,token);f.Amount=8;var cast=Cast(bp);
            cast.Context.Params.Metamagic=extend?Metamagic.Extend:(Metamagic)0;
            var duration=bp.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single().DurationValue;
            Check(duration.Calculate(cast.Context).Value==(extend?4:2),"Unpaid defensive spell keeps base/metamagic duration: "+token);
            using(Scope(ongoing)) {ongoing.OnEventAboutToTrigger(cast);ongoing.OnEventAboutToTrigger(cast);}
            Check(f.Amount==7 && duration.Calculate(cast.Context).Value==(extend?5:3),"Paid defense adds exactly one round after metamagic and charges once: "+token);
            var unrelated=new ContextDurationValue {Rate=DurationRate.Rounds,DiceType=DiceType.Zero,DiceCountValue=0,BonusValue=2};
            Check(unrelated.Calculate(cast.Context).Value==(extend?4:2),"Incidental duration in same context is not extended");
            cast.SetSuccess(true);ongoing.OnEventDidTrigger(cast);
            Check(duration.Calculate(cast.Context).Value==(extend?5:3),"Paid cast keeps extension after mode handler completes");
            var next=Cast(bp);Check(duration.Calculate(next.Context).Value==2,"Later unamplified cast does not inherit paid duration");
            f.Amount=8;var failed=Cast(bp);using(Scope(ongoing)) ongoing.OnEventAboutToTrigger(failed);
            ongoing.OnEventDidTrigger(failed);Check(f.Amount==8 && duration.Calculate(failed.Context).Value==2,"Failure refunds and cancels pending extension");
            f.Amount=0;var empty=Cast(bp);using(Scope(ongoing)) ongoing.OnEventAboutToTrigger(empty);
            Check(duration.Calculate(empty.Context).Value==2 && f.Amount==0,"No points leaves defensive spell usable at ordinary duration");
        }
        var thrust=Asset<BlueprintAbility>(cache,"MindThrust1");
        RuleRollD20 Roll(MechanicsContext ctx,int first,int second,bool resistance=true) {
            var events=new RulebookEventContext();RulebookEvent parent;
            if(resistance) {var sr=Empty<RuleSpellResistanceCheck>();Set(sr,"Context",ctx);Set(sr,"Initiator",f.Unit);parent=sr;}
            else parent=Empty<RuleAttackRoll>();
            var roll=new RuleRollD20(f.Unit);events.PushEvent(parent);events.PushEvent(roll);
            dice.Clear();dice.Enqueue(first);dice.Enqueue(second);roll.OnTrigger(events);return roll;
        }
        f.Amount=8;var linked=Cast(thrust,1);using(Scope(relentless)) {relentless.OnEventAboutToTrigger(linked);relentless.OnEventAboutToTrigger(linked);}
        linked.SetSuccess(true);relentless.OnEventDidTrigger(linked);Check(f.Amount==7,"Relentless costs one point per paid spell");
        foreach(var pair in new[]{new[]{3,17},new[]{18,4},new[]{1,20},new[]{12,12}}) {
            var roll=Roll(linked.Context,pair[0],pair[1]);
            Check(roll.Result==Math.Max(pair[0],pair[1]) && roll.RollHistory.Count==2,"Actual native d20 execution takes better of two SR rolls");
            Check(f.Amount==7,"Multiple SR targets on one cast do not charge again");
        }
        Check(Roll(linked.Context,3,17,false).RollHistory.Count==1,"An attack roll does not consume the SR reroll");
        var unlinked=Cast(thrust,1);Check(Roll(unlinked.Context,3,17).RollHistory.Count==1,"Another cast has no leaked SR reroll");
        f.Amount=8;var canceled=Cast(thrust,1);using(Scope(relentless)) relentless.OnEventAboutToTrigger(canceled);
        relentless.OnEventDidTrigger(canceled);Check(f.Amount==8 && Roll(canceled.Context,3,17).RollHistory.Count==1,"Failed cast cancels its SR reroll and refunds exactly once");
        foreach(var c in new[]{ongoing,relentless}) foreach(int branch in Enumerable.Range(0,5)) {
            f.Amount=8;var bp=c==ongoing?Asset<BlueprintAbility>(cache,"MentalBarrier1"):thrust;
            var cast=Cast(bp,branch==0?0:2);
            if(branch==1) cast.ForceFail=true;
            if(branch==2) cast.IsDuplicateSpellApplied=true;
            if(branch==3) cast.Context.IsDuplicateSpellApplied=true;
            if(branch==4) Set(cast.Spell,"Blueprint",new BlueprintAbility {Type=AbilityType.Spell});
            using(Scope(c)) c.OnEventAboutToTrigger(cast);
            Check(f.Amount==8,"New mode ignores cantrip, forced failure, duplicate and ineligible spell: "+c.Kind+"/"+branch);
        }
        PsychicExpansionTests.Inventory["newAmplificationRulesTested"]=true;
    }
    private static bool Faction(ref bool __result) {__result=true;return false;}
    private static bool MinimumAttribute(ref int __result) {__result=1;return false;}
    private static bool GameInstance(ref Kingmaker.Game __result) {__result=fixtureGame;return false;}
    private static bool CaptureHealing(RuleHealDamage __instance) {healAmount+=__instance.Bonus;return false;}
    private static bool FailLog(Exception __0) {throw new Exception("Native fixture boundary failed",__0);}
    private static bool CaptureDefense(BlueprintBuff __1,TimeSpan? __3,ref Buff __result) {defenseCalls++;defenseBlueprint=__1;defenseDuration=__3;__result=null;return false;}
    internal sealed class Caster
    {
        internal readonly UnitEntityData Unit=Empty<UnitEntityData>();
        internal readonly UnitDescriptor Descriptor=Empty<UnitDescriptor>();
        internal readonly Spellbook Book=Empty<Spellbook>();
        internal readonly ClassData Class;
        private readonly UnitAbilityResource resource;
        internal int Amount {get=>resource.Amount;set=>Set(resource,"<Amount>k__BackingField",value);}
        internal Caster(BlueprintsCache cache) {
            Set(Unit,"<Descriptor>k__BackingField",Descriptor);Set(Descriptor,"<Unit>k__BackingField",Unit);Set(Unit,"Parts",new EntityPartsManager(Unit));
            var proxy=FormatterServices.GetUninitializedObject(typeof(EntityService).GetNestedType("Proxy",BindingFlags.Public|BindingFlags.NonPublic));Set(proxy,"<Entity>k__BackingField",Unit);Set(Unit,"<Proxy>k__BackingField",proxy);Set(Unit,"<UniqueId>k__BackingField","phrenic-test");
            Set(Book,"Blueprint",cache.Load(BlueprintGuid.Parse("3ce26d41e96b5ca289f0c6ce9db28d3c")));Set(Book,"<Owner>k__BackingField",Descriptor);
            var progression=Empty<UnitProgressionData>();Set(Descriptor,"Progression",progression);Class=new ClassData(Book.Blueprint.CharacterClass);Set(progression,"Classes",new List<ClassData>{Class});
            var stats=Empty<CharacterStats>();Set(stats,"<Owner>k__BackingField",Descriptor);var wisdom=new ModifiableValueAttributeStat(stats,StatType.Wisdom);
            Set(wisdom,"Type",StatType.Wisdom);Set(wisdom,"m_BaseValue",30);Set(wisdom,"<ModifiedValue>k__BackingField",30);Set(stats,"Wisdom",wisdom);Set(stats,"<AllStats>k__BackingField",new List<ModifiableValue>{wisdom});Set(Descriptor,"Stats",stats);
            var state=Empty<UnitState>();var mechanics=Empty<UnitMechanicFeatures>();Set(mechanics,"DomainsByIntelligenceFeature",new Kingmaker.Utility.CountableFlag());Set(state,"Features",mechanics);Set(Descriptor,"State",state);
            var bp=(BlueprintAbilityResource)cache.Load(BlueprintGuid.Parse("ddfc9f9a2cbd51b48cbd214dbd75c8fb"));resource=new UnitAbilityResource(bp);
            var collection=new UnitAbilityResourceCollection(Descriptor);Set(collection,"m_Resources",new Dictionary<BlueprintScriptableObject,UnitAbilityResource>{{bp,resource}});Set(Descriptor,"Resources",collection);
        }
        internal RuleCastSpell Cast(SpellSchool school,SpellDescriptor flags,int circle) {
            var blueprint=new BlueprintAbility {Type=AbilityType.Spell,ComponentsArray=new BlueprintComponent[]{new SpellComponent{School=school}}};blueprint.OnEnable();
            var spell=Empty<AbilityData>();Set(spell,"Blueprint",blueprint);Set(spell,"m_CachedSpellbook",Book);object reference=default(EntityRef<UnitEntityData>);Set(reference,"m_Proxy",Get(Unit,"<Proxy>k__BackingField"));Set(spell,"m_CasterRef",reference);
            var context=Empty<AbilityExecutionContext>();context.SetParams(new AbilityParams {DC=20,SpellLevel=circle,CasterLevel=20});Set(context,"<SpellDescriptor>k__BackingField",flags);
            Set(context,"Ability",spell);Set(context,"AssociatedBlueprint",blueprint);Set(context,"m_CasterRef",reference);
            var rule=Empty<RuleCastSpell>();Set(rule,"Spell",spell);Set(rule,"Context",context);Set(rule,"Initiator",Unit);return rule;
        }
    }
    private static void TestLinks(Assembly assembly,BlueprintsCache cache,string evidence)
    {
        var bridge=assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicSpellIntegration");
        var available=AccessTools.Method(bridge,"Available");var register=AccessTools.Method(bridge,"Register");
        var entries=(Array)AccessTools.Field(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicSharedSpellCatalog"),"Entries").GetValue(null);
        var list=(BlueprintSpellList)cache.Load(BlueprintGuid.Parse("6a4b9d6651b155058004354a803da3bf"));
        Check(entries.Length==41,"41 audited spell identities across three original class lists");
        Check(!assembly.GetReferencedAssemblies().Any(a=>new[]{"PrestigePlus","CharacterOptionsPlus","ExpandedContent","TomeOfTheFirebird","TabletopTweaks-Core","TabletopTweaks-Base"}.Contains(a.Name)),"Optional spell providers are not assembly dependencies");
        var native=JObject.Parse(System.IO.File.ReadAllText(evidence))["linkedNative"];
        foreach(var row in entries.Cast<object>().Where(e=>(string)Get(e,"Provider")=="Wrath")) {
            var record=native.Single(r=>(string)r["blueprint"]["AssetId"]==(string)Get(row,"Guid"));var data=record["blueprint"]["Data"];
            Check((string)record["name"]==(string)Get(row,"Name") && (string)data["Type"]=="Spell" && (string)data["m_Parent"]==null,"Native linked spell name/type/root verified against game archive");
            var components=data["Components"];
            Check(components.Any(c=>((string)c["$type"]).EndsWith(", SpellComponent") && (string)c["School"]==Get(row,"School").ToString()),"Native linked spell school verified from game archive");
            Check(components.Any(c=>new[]{"AbilityEffectRunAction","AbilityVariants","AbilityEffectStickyTouch","AbilityShadowSpell","AdditionalAbilityEffectRunActionOnClickedTarget"}.Any(t=>((string)c["$type"]).EndsWith(", "+t))),"Native linked spell has a supported executable component");
        }
        var originals=new List<Tuple<BlueprintGuid,SimpleBlueprint>>();
        var foreignLists=new List<BlueprintSpellList>();
        int expected=0;
        foreach(var row in entries) {
            var id=BlueprintGuid.Parse((string)Get(row,"Guid"));var original=cache.Load(id);originals.Add(Tuple.Create(id,original));cache.RemoveCachedBlueprint(id);
            object[] args={cache,row,null};Check(!(bool)available.Invoke(null,args),"Missing optional spell is skipped");
            var spell=new BlueprintAbility {AssetGuid=id,name=(string)Get(row,"Name"),Type=AbilityType.Spell,ComponentsArray=Array.Empty<BlueprintComponent>()};cache.AddCachedBlueprint(id,spell);
            Check(!(bool)available.Invoke(null,args),"Disabled empty placeholder is rejected");
            var school=new SpellComponent {School=(SpellSchool)Get(row,"School")};
            spell.ComponentsArray=new BlueprintComponent[]{school,new AbilityEffectRunAction {Actions=new Kingmaker.ElementsSystem.ActionList {Actions=Array.Empty<Kingmaker.ElementsSystem.GameAction>()}}};spell.OnEnable();
            bool published=(bool)Get(row,"PublishedListRequired");
            Check((bool)available.Invoke(null,args)==!published,"Created but unpublished optional content stays disabled");
            var foreign=new BlueprintSpellList {AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),SpellsByLevel=new[]{new SpellLevelList(1)}};
            Set(foreign.SpellsByLevel[0],"m_Spells",new List<BlueprintAbilityReference>{Ref<BlueprintAbilityReference>(spell)});foreignLists.Add(foreign);
            var component=new SpellListComponent {SpellLevel=1};Set(component,"m_SpellList",Ref<BlueprintSpellListReference>(foreign));spell.ComponentsArray=spell.ComponentsArray.Concat(new BlueprintComponent[]{component}).ToArray();spell.OnEnable();
            Check((bool)available.Invoke(null,args),"Enabled provider spell with reciprocal list publication accepted");
            var originalSchool=school.School;school.School=SpellSchool.None;spell.OnEnable();
            Check(!(bool)available.Invoke(null,args),"Changed provider school is rejected");school.School=originalSchool;spell.OnEnable();
            if(published) {
                Set(foreign.SpellsByLevel[0],"m_Spells",new List<BlueprintAbilityReference>());Set(foreign.SpellsByLevel[0],"m_SpellsProxy",null);
                Check(!(bool)available.Invoke(null,args),"A stale SpellListComponent alone cannot reenable a disabled spell");
                Set(foreign.SpellsByLevel[0],"m_Spells",new List<BlueprintAbilityReference>{Ref<BlueprintAbilityReference>(spell)});Set(foreign.SpellsByLevel[0],"m_SpellsProxy",null);
            }
            spell.name+="Wrong";Check(!(bool)available.Invoke(null,args),"GUID collision with a different name is rejected");spell.name=(string)Get(row,"Name");
            if((int)Get(row,"Psychic")>=0) expected++;
        }
        int before=list.SpellsByLevel.Sum(l=>l.Spells.Count());register.Invoke(null,new object[]{cache});register.Invoke(null,new object[]{cache});
        Check(list.SpellsByLevel.Sum(l=>l.Spells.Count())==before+expected && expected==27,"Only 27 matching Psychic spells linked once; other-class-only spells excluded");
        Check(foreignLists.All(l=>l.SpellsByLevel[0].Spells.Count()==1),"Provider spell lists untouched");
        foreach(var row in entries) {
            var spell=(BlueprintAbility)cache.Load(BlueprintGuid.Parse((string)Get(row,"Guid")));
            Check(spell.GetComponents<SpellListComponent>().Count()==1,"Foreign spell blueprint not rewritten to add a Psychic list component");
        }
        // Restore the fixture, preserving subsequent baseline registration checks.
        foreach(var pair in originals) {cache.RemoveCachedBlueprint(pair.Item1);if(pair.Item2!=null) cache.AddCachedBlueprint(pair.Item1,pair.Item2);}
    }
}
