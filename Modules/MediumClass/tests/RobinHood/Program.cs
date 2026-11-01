using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Utility;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;

internal static class Launcher
{
    public static int Main(string[] args) {
        if(args.Length!=6) return 2;
        AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{
            var name=new AssemblyName(e.Name).Name;
            if(name=="0Harmony") return Assembly.LoadFrom(args[2]);
            foreach(var dir in new[]{args[3],args[0],Path.GetDirectoryName(args[1])}) {
                var file=Path.Combine(dir,name+".dll");if(File.Exists(file)) return Assembly.LoadFrom(file);
            }return null;
        };
        try {Assembly.LoadFrom(args[2]);typeof(Launcher).Assembly.GetType("Checks").GetMethod("Run").Invoke(null,new object[]{args});return 0;}
        catch(Exception e) {while(e!=null){Console.Error.WriteLine(e.Message+"\n"+e.StackTrace);e=e.InnerException;}return 1;}
    }
}
internal static class Checks
{
    private static int count;
    private static Assembly mod;
    private static BlueprintsCache cache;
    private static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Set(object o,string f,object v)=>AccessTools.Field(o.GetType(),f).SetValue(o,v);
    private static object Get(object o,string f)=>AccessTools.Field(o.GetType(),f).GetValue(o);
    private static void Check(bool b,string label){if(!b)throw new Exception(label);count++;}
    private static Type Type(string name)=>mod.GetType("MediumClass."+name,true);
    private static object Call(Type t,string name,params object[] args)=>AccessTools.Method(t,name).Invoke(null,args);
    private static string Id(string name)=>(string)AccessTools.Field(Type("Utilities.Guids"),name).GetRawConstantValue();
    private static T BP<T>(string name) where T:SimpleBlueprint=>(T)cache.Load(BlueprintGuid.Parse(Id(name)));
    private static T Add<T>(T bp,string id,string name) where T:SimpleBlueprint {bp.AssetGuid=BlueprintGuid.Parse(id);bp.name=name;cache.AddCachedBlueprint(bp.AssetGuid,bp);return bp;}
    private static bool Skip()=>false;
    private static bool Text(string __0,ref LocalizedString __result){__result=new LocalizedString{Key=__0};return false;}
    private static Type spriteAssetType;
    private static object SpriteAsset(UnityEngine.Sprite sprite){if(ReferenceEquals(sprite,null))return null;var asset=FormatterServices.GetUninitializedObject(spriteAssetType);Set(asset,"Value",sprite);Set(asset,"AssetId","fixture-sprite");return asset;}
    private static bool IsSpriteAssetConversion(CodeInstruction i)=>i.operand is MethodInfo m && m.Name=="op_Implicit" && m.DeclaringType==spriteAssetType && m.GetParameters().Length==1 && m.GetParameters()[0].ParameterType==typeof(UnityEngine.Sprite);
    private static IEnumerable<CodeInstruction> SpriteBoundary(IEnumerable<CodeInstruction> code){foreach(var i in code){if(IsSpriteAssetConversion(i)){i.opcode=OpCodes.Call;i.operand=AccessTools.Method(typeof(Checks),nameof(SpriteAsset));yield return i;yield return new CodeInstruction(OpCodes.Castclass,spriteAssetType);}else yield return i;}}
    private static void InstallSpriteBoundary(Harmony h){
        // Latest Harmony cannot detour this shared generic constructor on desktop CLR.
        // Replace only its Sprite conversion call sites; keep the same rendering boundary.
        spriteAssetType=mod.GetType("BlueprintCore.Utils.Assets.Asset`1").MakeGenericType(typeof(UnityEngine.Sprite));int patched=0;
        foreach(var t in mod.GetTypes().Where(t=>t.Namespace!=null&&t.Namespace.StartsWith("MediumClass")))foreach(var method in t.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly).Where(m=>!m.ContainsGenericParameters&&m.GetMethodBody()!=null)){
            if(PatchProcessor.GetOriginalInstructions(method).Any(IsSpriteAssetConversion)){h.Patch(method,transpiler:Hook(nameof(SpriteBoundary)));patched++;}
        }
        if(patched==0)throw new Exception("No actual Sprite conversion boundary found");
    }
    private static HarmonyMethod Hook(string name)=>new HarmonyMethod(AccessTools.Method(typeof(Checks),name));
    public static void Run(string[] args)
    {
        RequiredHarmonyRuntime.Verify(args[2]);
        mod=Assembly.LoadFrom(args[1]);var h=new Harmony("Medium040.NativeFixture");
        h.Patch(AccessTools.Method(typeof(Kingmaker.Utility.ProfileScope),"New",new[]{typeof(string),typeof(UnityEngine.Object)}),prefix:Hook(nameof(Skip)));
        foreach(var m in new[]{"Error","Warning","Log","Exception"}) foreach(var method in typeof(Owlcat.Runtime.Core.Logging.LogChannel).GetMethods().Where(x=>x.Name==m && !x.IsGenericMethod)) h.Patch(method,prefix:Hook(nameof(Skip)));
        foreach(var method in mod.GetType("BlueprintCore.Utils.LogWrapper").GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static).Where(m=>m.DeclaringType.FullName=="BlueprintCore.Utils.LogWrapper" && m.ReturnType==typeof(void) && !m.IsGenericMethod)) h.Patch(method,prefix:Hook(nameof(Skip)));
        var loc=mod.GetType("BlueprintCore.Utils.LocalizationTool");
        InstallSpriteBoundary(h);
        foreach(var m in loc.GetMethods().Where(m=>m.Name=="GetString" && m.GetParameters().Length==1)) h.Patch(m,prefix:Hook(nameof(Text)));
        cache=new BlueprintsCache();AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,cache);
        foreach(var row in JArray.Parse(File.ReadAllText(args[4]))) {
            string name=(string)row["name"],id=(string)row["blueprint"]["AssetId"];
            BlueprintScriptableObject bp=name.StartsWith("Saves")||name=="BABMedium"?(BlueprintScriptableObject)new BlueprintStatProgression():name.EndsWith("Buff")?new BlueprintBuff():new BlueprintAbility {Type=AbilityType.Spell};
            if(bp is BlueprintUnitFact fact) Set(fact,"m_Icon",Empty<UnityEngine.Sprite>());
            if(bp is BlueprintBuff buff) {
                buff.FxOnStart=new Kingmaker.ResourceLinks.PrefabLink();buff.FxOnRemove=new Kingmaker.ResourceLinks.PrefabLink();
                buff.ComponentsArray=new BlueprintComponent[]{new Kingmaker.Designers.Mechanics.Buffs.BuffInvisibility {NotDispellAfterOffensiveAction=true}};
            }
            Add(bp,id,name);bp.OnEnable();
        }
        foreach(var name in new[]{"Medium","Adriel","Trickster"}) Add(new BlueprintCharacterClass(),Id(name),name);
        foreach(var name in new[]{"TricksterSurpriseStrike","TricksterSeanceBoon","MediumSpiritBonus"}) Add(new BlueprintFeature {Ranks=10},Id(name),name);
        foreach(var name in new[]{"MediumInfluenceResource","MediumInfluenceResourceTrickster"}) Add(new BlueprintAbilityResource(),Id(name),name);
        Add(new BlueprintBuff(),Id("MediumInfluenceDebuff"),"InfluenceDebuff");
        Call(Type("Medium.Spirits.RobinHood.RobinHood"),"ConfigureEnabled");
        Call(Type("Medium.Spirits.RobinHood.RobinHood"),"ChannelAbility");
        Call(Type("Medium.Spirits.Adriel.DeathCurse"),"ConfigureEnabled");
        Call(mod.GetType("BlueprintCore.Blueprints.Configurators.Root.RootConfigurator"),"ConfigureDelayedBlueprints");
        Registration();Rules();Lifecycle(h);
        h.UnpatchAll(h.Id);
        File.WriteAllText(args[5],new JObject{{"passed",count},{"scope","Publicized game managed API, real blueprint registration and rule containers; localization/rendering/game services, distance and native donor loading use explicit fixture boundaries. No Unity/Mono/UI/real save playtest."},{"assemblySha256",BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(args[1]))).Replace("-","")}}.ToString());
        Console.WriteLine("PASS: "+count+" Robin Hood / Death Curse native managed checks. Not gameplay.");
    }
    private static void Registration()
    {
        var lesser=BP<BlueprintFeature>("RobinHoodLesser");var prof=lesser.GetComponent<AddProficiencies>();
        Check(prof.WeaponProficiencies.SequenceEqual(new[]{WeaponCategory.Longbow,WeaponCategory.Shortbow}),"Bow proficiencies cover native long/short/composite categories only");
        Check(prof.ArmorProficiencies!=null,"Native proficiency component has initialized armor array");
        var ability=BP<BlueprintAbility>("RobinHoodUnseenAbility");
        Check(ability.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift && ability.Type==AbilityType.Supernatural && ability.Range==AbilityRange.Personal,"Supreme is swift personal supernatural");
        var cost=ability.GetComponent<AbilityResourceLogic>();Check(cost.Amount==1 && cost.IsSpendResource && cost.RequiredResource==BP<BlueprintAbilityResource>("MediumInfluenceResource"),"Supreme accepts exactly one influence through existing resource contract");
        var apply=ability.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single();
        Check(apply.ToCaster && !apply.Permanent && apply.IsNotDispelable && apply.DurationValue.Rate==DurationRate.Rounds && apply.DurationValue.BonusValue.ValueType==ContextValueType.Rank,"Finite class-ranked rounds, self only");
        var rank=ability.GetComponent<Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig>();
        Check(((BlueprintCharacterClassReference[])Get(rank,"m_Class")).Single().Get()==BP<BlueprintCharacterClass>("Medium"),"Supreme duration uses Medium level, not total character level");
        var hidden=BP<BlueprintBuff>("RobinHoodUnseenBuff");Check(hidden.GetComponent<Kingmaker.Designers.Mechanics.Buffs.BuffInvisibility>().NotDispellAfterOffensiveAction,"Invisibility survives attacks");
        Check(hidden.FxOnStart!=null && hidden.FxOnRemove!=null && !ReferenceEquals(hidden.Icon,null),"Supreme reuses native presentation with valid cleanup links");
        var progression=BP<BlueprintProgression>("RobinHoodProgression");
        foreach(var pair in new[]{(1,"RobinHoodLesser"),(11,"RobinHoodGreater"),(17,"RobinHoodSupreme")}) Check(progression.LevelEntries.Single(e=>e.Level==pair.Item1).Features.Single().AssetGuid==BP<BlueprintFeature>(pair.Item2).AssetGuid,"Correct spirit power threshold "+pair.Item1);
        Check(progression.LevelEntries.Sum(e=>e.Features.Count(f=>f==BP<BlueprintFeature>("TricksterSurpriseStrike")))==6,"Intermediate Surprise Strike grows to six ranks at 18");
        var death=BP<BlueprintFeature>("MediumAdrielDeathCurse");var modes=death.GetComponent<AddFacts>().Facts.OfType<BlueprintActivatableAbility>().ToArray();
        Check(modes.Length==5,"Death Curse grants five native mode buttons");
        foreach(var mode in modes) {
            Check(mode.ActivationType==AbilityActivationType.Immediately && mode.DeactivateImmediately && (Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType)Get(mode,"m_ActivateWithUnitCommand")==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free,"Death Curse immediate on/off "+mode.name);
            Check(mode.Buff.FxOnStart!=null && mode.Buff.FxOnRemove!=null,"Death Curse removal links initialized");
            Check(mode.ComponentsArray.Any(c=>c.GetType().Name=="DeathCurseModeRestriction"),"Energy cap and spirit restriction present");
        }
        foreach(var name in new[]{"MediumAdrielDeathCurseAbility","MediumAdrielDeathCurseAbilityNegative","MediumAdrielDeathCurseAbilityFire"}) Check(BP<BlueprintAbility>(name)!=null,"Old ability GUID/type retained for saved references");
    }
    private static void Rules()
    {
        var rules=Type("Medium.RobinHoodRules");
        foreach(Alignment a in Enum.GetValues(typeof(Alignment))) {
            bool expected=new[]{Alignment.NeutralGood,Alignment.ChaoticGood,Alignment.TrueNeutral,Alignment.ChaoticNeutral}.Contains(a);
            Check((bool)Call(rules,"AllowedAlignment",a)==expected,"Alignment gate "+a);
        }
        var isBow=AccessTools.Method(rules,"IsBow",new[]{typeof(WeaponCategory)});
        foreach(WeaponCategory w in Enum.GetValues(typeof(WeaponCategory))) Check((bool)isBow.Invoke(null,new object[]{w})==(w==WeaponCategory.Longbow||w==WeaponCategory.Shortbow),"Weapon category "+w);
        foreach(var row in new[]{(0f,4,0),(9.9f,4,0),(10f,4,1),(19.9f,4,1),(20f,4,2),(30f,4,3),(30.1f,4,7),(40f,4,8),(40f,-2,4),(float.NaN,4,0)})
            Check((int)Call(rules,"DistanceBonus",row.Item1,row.Item2)==row.Item3,"Distance and positive Wisdom boundary "+row);
        var stats=(StatType[])GetStatic(rules,"AttunementStats");
        Check(stats.Contains(StatType.CheckBluff)&&stats.Contains(StatType.SaveReflex)&&stats.Contains(StatType.SkillThievery)&&stats.Contains(StatType.SkillMobility)&&stats.Contains(StatType.SkillStealth),"Attunement covers requested checks");
        Check(!stats.Contains(StatType.Dexterity)&&!stats.Contains(StatType.AdditionalAttackBonus),"No Dexterity score or global attack mutation");
        var component=Activator.CreateInstance(Type("Medium.RobinHoodUnseenAttacker"));
        var attack=Empty<RuleAttackRoll>();var flat=Empty<RuleCheckTargetFlatFooted>();
        AccessTools.Method(component.GetType(),"OnEventAboutToTrigger",new[]{typeof(RuleAttackRoll)}).Invoke(component,new object[]{attack});
        AccessTools.Method(component.GetType(),"OnEventAboutToTrigger",new[]{typeof(RuleCheckTargetFlatFooted)}).Invoke(component,new object[]{flat});
        Check(attack.ForceFlatFooted&&flat.ForceFlatFooted,"Supreme forces native flat-footed attack and AC checks");
    }
    private static object GetStatic(Type t,string name)=>AccessTools.Field(t,name).GetValue(null);
    private static UnitEntityData unit;
    private static EntityFactsManager manager;
    private static Spellbook book;
    private static int maxCircle;
    private static float distanceFeet;
    private static bool Distance(ref float __result){__result=distanceFeet.Feet().Meters;return false;}
    private static bool Minimum(ref int __result){__result=1;return false;}
    private static bool NoPreparation(ref IReadOnlyList<Spellbook> __result){__result=Array.Empty<Spellbook>();return false;}
    private static bool MainBook(ref Spellbook __result){__result=book;return false;}
    private static bool MaxCircle(ref int __result){__result=maxCircle;return false;}
    private static bool False(ref bool __result){__result=false;return false;}
    private static T Raw<T>(BlueprintUnitFact bp) where T:UnitFact {
        var fact=Empty<T>();Set(fact,"<Blueprint>k__BackingField",bp);Set(fact,"<Manager>k__BackingField",manager);
        Set(fact,"<IsActive>k__BackingField",true);Set(fact,"<IsTurnedOn>k__BackingField",true);Set(fact,"Components",new List<EntityFactComponent>());
        manager.List.Add(fact);var c=Get(manager,"m_Cache");AccessTools.Method(c.GetType(),"Add").Invoke(c,new object[]{fact});return fact;
    }
    private static object Ensure(string type)=>typeof(UnitEntityData).GetMethods().Single(m=>m.Name=="Ensure" && m.IsGenericMethod && m.GetParameters().Length==0).MakeGenericMethod(Type(type)).Invoke(unit,null);
    private static IDisposable Scope(UnitFactComponentDelegate c,UnitFact fact) {
        var runtime=c.CreateRuntimeFactComponent();runtime.Setup(c);runtime.RestoreOwnerLink(fact);return runtime.RequestEventContext();
    }
    private static void Lifecycle(Harmony h)
    {
        unit=Empty<UnitEntityData>();var owner=Empty<UnitDescriptor>();Set(unit,"<Descriptor>k__BackingField",owner);Set(owner,"<Unit>k__BackingField",unit);
        Set(unit,"Parts",new EntityPartsManager(unit));manager=new EntityFactsManager(unit);Set(unit,"Facts",manager);
        var buffs=new BuffCollection();buffs.Initialize(manager);Set(unit,"<Buffs>k__BackingField",buffs);
        var proxy=FormatterServices.GetUninitializedObject(typeof(EntityService).GetNestedType("Proxy",BindingFlags.Public|BindingFlags.NonPublic));
        Set(proxy,"<Entity>k__BackingField",unit);Set(unit,"<Proxy>k__BackingField",proxy);Set(unit,"<UniqueId>k__BackingField","robin-fixture");
        var progression=Empty<UnitProgressionData>();var features=new FeatureCollection();features.Initialize(manager);Set(progression,"<Features>k__BackingField",features);Set(progression,"<Owner>k__BackingField",owner);Set(owner,"Progression",progression);
        Set(progression,"Classes",new List<ClassData>{new ClassData(BP<BlueprintCharacterClass>("Medium")){Level=17}});
        var lesser=Raw<Feature>(BP<BlueprintFeature>("RobinHoodLesser"));Set(lesser,"<Rank>k__BackingField",1);
        var rules=Type("Medium.RobinHoodRules");
        Check((bool)Call(rules,"HasLesser",owner),"Active owned lesser power enables Robin Hood bonuses");
        Set(lesser,"<Deactivating>k__BackingField",true);Check(!(bool)Call(rules,"HasLesser",owner),"Native deactivation-before-IsActive-clear does not regrant spells");Set(lesser,"<Deactivating>k__BackingField",false);
        var bp=Add(new BlueprintSpellbook {Spontaneous=true},Id("MediumSpellbook"),"MediumBook");
        book=Empty<Spellbook>();Set(book,"Blueprint",bp);Set(book,"<Owner>k__BackingField",owner);
        foreach(var field in new[]{"m_KnownSpells","m_SpecialSpells","m_CustomSpells"}) Set(book,field,Enumerable.Range(0,11).Select(n=>new List<AbilityData>()).ToArray());
        Set(book,"m_MemorizedSpells",Enumerable.Range(0,11).Select(n=>new List<SpellSlot>()).ToArray());
        Set(book,"m_KnownSpellLevels",new Dictionary<BlueprintAbility,List<int>>());Set(book,"m_SpecialLists",new List<BlueprintSpellList>());
        Set(owner,"m_Spellbooks",new Dictionary<BlueprintSpellbook,Spellbook>{{bp,book}});
        var spellRules=Type("Medium.MediumSpiritSpellbookRules");
        // Existing caster preparation and spell-table progression are covered by old
        // suites. Here only those boundaries are replaced; native known-spell lists
        // and production temporary ownership/revocation run unchanged.
        h.Patch(AccessTools.Method(spellRules,"EnsurePreparationBooks"),prefix:Hook(nameof(NoPreparation)));
        h.Patch(AccessTools.Method(spellRules,"MediumBook"),prefix:Hook(nameof(MainBook)));
        h.Patch(AccessTools.Method(spellRules,"NotifyActionBar"),prefix:Hook(nameof(Skip)));
        h.Patch(AccessTools.PropertyGetter(typeof(Spellbook),"MaxSpellLevel"),prefix:Hook(nameof(MaxCircle)));
        h.Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor),"IsPlayerFaction"),prefix:Hook(nameof(False)));
        var part=Ensure("Medium.NewUnitParts.UnitPartMediumPreparedSpells");var sync=AccessTools.Method(part.GetType(),"Sync",new[]{typeof(bool)});
        var sense=(BlueprintAbility)cache.Load(BlueprintGuid.Parse("82962a820ebc0e7408b8582fdc3f4c0c"));
        book.AddKnown(2,sense,false);
        for(maxCircle=0;maxCircle<=4;maxCircle++) {
            sync.Invoke(part,new object[]{false});sync.Invoke(part,new object[]{false});
            Check(book.GetAllKnownSpells().Count()==maxCircle+(maxCircle<2?1:0),"Only unlocked circles added, repeated sync has no duplicates: "+maxCircle);
            Check(!book.GetKnownSpell(sense).IsTemporary,"Permanent Sense Vitals is never converted to a temporary spell");
        }
        Check(book.GetAllKnownSpells().Where(s=>s.Blueprint!=sense).All(s=>s.IsTemporary),"All Robin-only spells tracked as temporary");
        var stored=book.GetAllKnownSpells().Where(s=>s.IsTemporary).ToArray();
        Set(lesser,"<Deactivating>k__BackingField",true);sync.Invoke(part,new object[]{false});
        Check(book.GetAllKnownSpells().Count()==1 && book.IsKnown(sense),"Losing lesser power removes every borrowed spell, preserves original spell");
        sync.Invoke(part,new object[]{false});Check(book.GetAllKnownSpells().Count()==1,"Cleanup is idempotent");
        Set(lesser,"<Deactivating>k__BackingField",false);maxCircle=4;sync.Invoke(part,new object[]{false});
        Check(book.GetAllKnownSpells().Count()==4,"Next seance grants the four unlocked spells again");
        var entryComponent=(UnitFactComponentDelegate)Activator.CreateInstance(Type("Medium.NewComponents.AbilitySpecific.MediumSpiritComponent"));
        Call(Type("Medium.Spirits.RobinHood.RobinHood"),"ConfigureEntry",entryComponent);
        using(Scope(entryComponent,lesser)) AccessTools.Method(entryComponent.GetType(),"Register").Invoke(entryComponent,null);
        var medium=Ensure("Medium.NewUnitParts.UnitPartMedium");
        var robinRef=BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(BP<BlueprintCharacterClass>("RobinHood"));
        Set(medium,"PrimarySpirit",robinRef);
        var entries=(IDictionary)Get(medium,"Spirits");var entry=entries[robinRef];Set(entry,"SpiritFocus",2);
        var bonusFact=Raw<Feature>(BP<BlueprintFeature>("MediumSpiritBonus"));Set(bonusFact,"<Rank>k__BackingField",3);
        Check((int)Call(rules,"Attunement",unit)==5,"Spirit bonus rank plus focus read from actual Medium state");
        var checkComponent=(UnitFactComponentDelegate)Activator.CreateInstance(Type("Medium.RobinHoodDexterityChecks"));
        foreach(var stat in new[]{StatType.Dexterity,StatType.Strength,StatType.SkillStealth,StatType.SaveReflex}) {
            var evt=Empty<RuleSkillCheck>();Set(evt,"<StatType>k__BackingField",stat);Set(evt,"<StatValue>k__BackingField",4);
            Set(evt,"Bonus",new ModifiableValue(null,StatType.Unknown));
            using(Scope(checkComponent,bonusFact)) AccessTools.Method(checkComponent.GetType(),"OnEventAboutToTrigger").Invoke(checkComponent,new object[]{evt});
            Check(evt.TotalBonus==(stat==StatType.Dexterity?9:4),"Native skill total includes check-only bonus exactly once: "+stat);
        }
        Set(medium,"PrimarySpirit",new BlueprintCharacterClassReference());
        Check((int)Call(rules,"Attunement",unit)==0,"Ended Robin Hood channel no longer gives Dexterity check bonus");
        var stats=Empty<CharacterStats>();Set(stats,"<Owner>k__BackingField",owner);
        var wisdom=new ModifiableValueAttributeStat(stats,StatType.Wisdom);Set(wisdom,"Type",StatType.Wisdom);Set(wisdom,"m_BaseValue",18);Set(wisdom,"<ModifiedValue>k__BackingField",18);Set(stats,"Wisdom",wisdom);Set(owner,"Stats",stats);
        h.Patch(AccessTools.Method(typeof(UnitEntityData),"DistanceTo",new[]{typeof(UnitEntityData)}),prefix:Hook(nameof(Distance)));
        h.Patch(AccessTools.PropertyGetter(typeof(ModifiableValueAttributeStat),"MinValueRaw"),prefix:Hook(nameof(Minimum)));
        var archer=(UnitFactComponentDelegate)Activator.CreateInstance(Type("Medium.RobinHoodDistantArcher"));
        foreach(var category in new[]{WeaponCategory.Longbow,WeaponCategory.Shortbow,WeaponCategory.LightCrossbow,WeaponCategory.Longsword}) foreach(float feet in new[]{9f,10f,30f,31f,40f}) {
            distanceFeet=feet;
            var type=new BlueprintWeaponType {Category=category};var weaponBP=new BlueprintItemWeapon();
            var reference=BlueprintReferenceBase.CreateTyped<BlueprintWeaponTypeReference>(type);Set(reference,"<Cached>k__BackingField",type);Set(weaponBP,"m_Type",reference);
            var weapon=Empty<ItemEntityWeapon>();Set(weapon,"m_Blueprint",weaponBP);
            object wielderRef=default(EntityRef<UnitEntityData>);Set(wielderRef,"m_Proxy",proxy);Set(weapon,"m_WielderRef",wielderRef);
            var withWeapon=Empty<RuleAttackWithWeapon>();Set(withWeapon,"Target",unit);
            var damage=Empty<RuleCalculateWeaponStats>();Set(damage,"Weapon",weapon);Set(damage,"AttackWithWeapon",withWeapon);Set(damage,"DamageModifiers",new ModifiableBonus());
            var inner=Empty<RuleCalculateAttackBonusWithoutTarget>();Set(inner,"WeaponStats",damage);
            var attackRule=Empty<RuleCalculateAttackBonus>();Set(attackRule,"m_InnerRule",inner);Set(attackRule,"Target",unit);
            using(Scope(archer,bonusFact)) {
                AccessTools.Method(archer.GetType(),"OnEventAboutToTrigger",new[]{typeof(RuleCalculateAttackBonus)}).Invoke(archer,new object[]{attackRule});
                AccessTools.Method(archer.GetType(),"OnEventAboutToTrigger",new[]{typeof(RuleCalculateWeaponStats)}).Invoke(archer,new object[]{damage});
            }
            int expected=(category==WeaponCategory.Longbow||category==WeaponCategory.Shortbow)?(int)(feet/10)+(feet>30?4:0):0;
            Check(attackRule.AllBonuses.Sum(m=>m.Value)==expected && damage.DamageModifiers.TotalValue==expected,"Attack and damage get identical scoped bow bonus: "+category+"/"+feet);
            object[] rangeArgs={weapon,new Feet(70)};Call(Type("Medium.RobinHoodBowRangePatch"),"Postfix",rangeArgs);
            Check(((Feet)rangeArgs[1]).Value==((category==WeaponCategory.Longbow||category==WeaponCategory.Shortbow)?140:70),"Bow range doubles only for active owner: "+category);
            Set(lesser,"<Deactivating>k__BackingField",true);rangeArgs=new object[]{weapon,new Feet(70)};Call(Type("Medium.RobinHoodBowRangePatch"),"Postfix",rangeArgs);
            Check(((Feet)rangeArgs[1]).Value==70,"Range bonus ends with lesser power");Set(lesser,"<Deactivating>k__BackingField",false);
        }
        // Real native flat-footed rule bypasses all ordinary target-state gates when forced.
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper),"IsActive"),prefix:Hook(nameof(False)));
        var flat=new RuleCheckTargetFlatFooted(unit,unit);flat.ForceFlatFooted=true;flat.OnTrigger(new RulebookEventContext());
        Check(flat.IsFlatFooted,"Native ForceFlatFooted branch resolves before target immunities and visibility");
        var deathRules=Type("Medium.MediumDeathCurseRules");
        var modes=BP<BlueprintFeature>("MediumAdrielDeathCurse").GetComponent<AddFacts>().Facts.OfType<BlueprintActivatableAbility>().ToArray();
        foreach(var first in modes) foreach(var second in modes.Where(m=>m!=first)) {
            buffs.Enumerable.Clear();var a=Empty<Buff>();Set(a,"<Blueprint>k__BackingField",first.Buff);Set(a,"<IsActive>k__BackingField",true);buffs.Enumerable.Add(a);
            Check((bool)Call(deathRules,"CanToggle",unit,second.Buff),"Second energy allowed");
            var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",second.Buff);Set(b,"<IsActive>k__BackingField",true);buffs.Enumerable.Add(b);
            Check((bool)Call(deathRules,"CanToggle",unit,first.Buff),"Current energy remains switchable off at cap");
            Check(modes.Where(m=>m!=first&&m!=second).All(m=>!(bool)Call(deathRules,"CanToggle",unit,m.Buff)),"Third energy blocked for every pair");
            buffs.Enumerable.Remove(a);Check(modes.All(m=>(bool)Call(deathRules,"CanToggle",unit,m.Buff)),"Removing one energy releases a slot immediately");
        }
    }
}
