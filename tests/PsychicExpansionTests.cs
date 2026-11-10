using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.RuleSystem.Rules.Damage;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicExpansionTests
{
    private static int count;
    private static Type expansion, factory;
    private static ContextActionDealDamage capturedDamage;
    private static void Check(bool result,string message) { if(!result) throw new Exception(message); count++; }
    private static object Get(object obj,string field)=>AccessTools.Field(obj.GetType(),field).GetValue(obj);
    private static void Set(object obj,string field,object value)=>AccessTools.Field(obj.GetType(),field).SetValue(obj,value);
    private static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    private static BlueprintGuid Id(string token)=>(BlueprintGuid)AccessTools.Method(factory,"Id").Invoke(null,new object[] { token });
    private static T Asset<T>(BlueprintsCache cache,string token) where T:SimpleBlueprint=>(T)cache.Load(Id(token));
    private static void Register(BlueprintsCache cache)=>AccessTools.Method(expansion,"Register").Invoke(null,new object[] { cache });
    private static string Signature(object obj)=>(string)AccessTools.Method(expansion,"Signature").Invoke(null,new object[] { obj });
    private static readonly Type[] GameTypes=typeof(Spellbook).Assembly.GetTypes();
    internal static JArray NativeRows;
    private static readonly List<string> learningLogs=new List<string>();
    private static bool CaptureLearning(string message){learningLogs.Add(message);return false;}
    internal static T NativeCase<T>(string guid) where T:BlueprintScriptableObject,new()
    {
        var row=NativeRows.Single(r=>(string)r["blueprint"]["AssetId"]==guid);
        var obj=new T {name=(string)row["name"],AssetGuid=BlueprintGuid.Parse(guid)};
        obj.ComponentsArray=((JArray)row["blueprint"]["Data"]["Components"]??new JArray()).Select(c=>(BlueprintComponent)Read(c,typeof(BlueprintComponent))).ToArray();
        foreach(var property in ((JObject)row["blueprint"]["Data"]).Properties().Where(x=>new[]{"Components","m_Flags","m_Parent","School","EffectOnEnemy","EffectOnAlly","Type","SpellResistance"}.Contains(x.Name))) {
            var field=AccessTools.Field(typeof(T),property.Name=="Components"?"ComponentsArray":property.Name);
            if(field!=null)field.SetValue(obj,Read(property.Value,field.FieldType));
        }
        obj.OnEnable();return obj;
    }
    private static bool FixtureForceIcon(ref UnityEngine.Sprite __result){__result=Empty<UnityEngine.Sprite>();return false;}

    // Construct managed native blueprints/components from audited game JSON. This is
    // a fixture reader, not the game's Unity asset or save deserializer. Asset links
    // remain native GUID references; icons, prefabs and animations are not loaded.
    internal static object Read(JToken value,Type target)
    {
        if(value==null || value.Type==JTokenType.Null) return null;
        if(typeof(BlueprintReferenceBase).IsAssignableFrom(target)) {
            var reference=(BlueprintReferenceBase)Activator.CreateInstance(target);
            Set(reference,"deserializedGuid",BlueprintGuid.Parse(((string)value).Replace("!bp_",""))); return reference;
        }
        if(target.IsEnum) return Enum.Parse(target,(string)value);
        if(target==typeof(string) || target.IsPrimitive || target==typeof(decimal)) return value.ToObject(target);
        if(target==typeof(BlueprintGuid)) return BlueprintGuid.Parse((string)value);
        if(typeof(Kingmaker.ResourceLinks.WeakResourceLink).IsAssignableFrom(target)) {
            var link=(Kingmaker.ResourceLinks.WeakResourceLink)Activator.CreateInstance(target);
            link.AssetId=(string)value["guid"]??(string)value["AssetId"]??"";
            return link;
        }
        if(target==typeof(UnityEngine.Vector3) && value.Type==JTokenType.String) {
            var parts=((string)value).Split('|');
            if(parts.Length!=3) throw new Exception("Invalid native vector fixture: "+value);
            var values=parts.Select(x=>float.Parse(x,System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            return new UnityEngine.Vector3(values[0],values[1],values[2]);
        }
        if(typeof(UnityEngine.Object).IsAssignableFrom(target) || target==typeof(UnityEngine.AnimationCurve)) return null;
        if(target.IsArray) { var values=(JArray)value;var result=Array.CreateInstance(target.GetElementType(),values.Count); for(int i=0;i<values.Count;i++) result.SetValue(Read(values[i],target.GetElementType()),i); return result; }
        if(typeof(IList).IsAssignableFrom(target) && target.IsGenericType) { var list=(IList)Activator.CreateInstance(target); foreach(var child in (JArray)value) list.Add(Read(child,target.GetGenericArguments()[0])); return list; }
        if(value is JObject obj) {
            if(obj["$type"]!=null) {
                string name=((string)obj["$type"]).Split(',').Last().Trim();
                var matches=GameTypes.Where(t=>t.Name==name && target.IsAssignableFrom(t)).ToArray();
                // Current native Skald dependencies include two GameAction
                // classes named RemoveFact. Resolve that ambiguity using the
                // real serialized TypeId, never a guessed namespace/order.
                if(matches.Length>1) {
                    string typeId=((string)obj["$type"]).Split(',')[0].Trim();
                    matches=matches.Where(t=>CustomAttributeData.GetCustomAttributes(t).Any(a=>a.AttributeType.Name=="TypeIdAttribute" && a.ConstructorArguments.Any(v=>v.Value is string id && id.Replace("-","").Equals(typeId.Replace("-",""),StringComparison.OrdinalIgnoreCase)))).ToArray();
                }
                if(matches.Length!=1) throw new Exception("Unresolved audited native fixture type "+name); target=matches[0];
            }
            var result=target==typeof(SpellLevelList)?new SpellLevelList((int)obj["SpellLevel"]):target==typeof(Kingmaker.UnitLogic.Buffs.Polymorph.VisualTransitionSettings)?FormatterServices.GetUninitializedObject(target):Activator.CreateInstance(target,true);
            foreach(var pair in obj.Properties().Where(p=>p.Name!="$type")) {
                var field=AccessTools.Field(target,pair.Name); if(field!=null && !field.IsInitOnly) field.SetValue(result,Read(pair.Value,field.FieldType));
            }
            // Native blueprint reference initialization supplies an empty reference for JSON null.
            if(result is Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig && Get(result,"Archetype")==null)Set(result,"Archetype",new BlueprintArchetypeReference());
            return result;
        }
        return value.ToObject(target);
    }
    public static int Run(Assembly assembly,BlueprintsCache cache,string evidence)
    {
        expansion=assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicExpansion",true); factory=assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets",true);
        if(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicClassCompletion01458")!=null)
            typeof(PsychicExpansionTests).Assembly.GetType("MagusBatch01454Tests",true).GetMethod("Prepare058Presentation",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        new Harmony("CotW.PsychicEmbeddedIconFixture").Patch(AccessTools.Method(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicRestorativeSpells"),"ForceIcon"),prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicExpansionTests),nameof(FixtureForceIcon))));
        new Harmony("CotW.PsychicEmbeddedIconFixture").Patch(AccessTools.Method(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAfflictionSpells"),"SynapseIcon"),prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicExpansionTests),nameof(FixtureForceIcon))));
        new Harmony("CotW.PsychicEmbeddedIconFixture").Patch(AccessTools.Method(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicOriginalIcons"),"Load"),prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicExpansionTests),nameof(FixtureForceIcon))));
        new Harmony("CotW.PsychicEmbeddedIconFixture").Patch(AccessTools.Method(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicSilence"),"Icon"),prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicExpansionTests),nameof(FixtureForceIcon))));
        var data=JObject.Parse(File.ReadAllText(evidence));
        NativeRows=(JArray)data["requiredNative"];
        foreach(var row in data["requiredNative"]) {
            var bp=row["blueprint"]; var guid=BlueprintGuid.Parse((string)bp["AssetId"]);
            if(cache.Load(guid)!=null) {
                if (cache.Load(guid) is Kingmaker.Blueprints.Facts.BlueprintUnitFact knownFact) Set(knownFact,"m_Icon",Empty<UnityEngine.Sprite>());
                if(guid.ToString()=="3c17035ec4717674cae2e841a190e757") {var known=(BlueprintScriptableObject)cache.Load(guid);known.ComponentsArray=((JArray)bp["Data"]["Components"]).Select(c=>(BlueprintComponent)Read(c,typeof(BlueprintComponent))).ToArray();known.OnEnable();}
                continue;
            }
            string type=((string)bp["Data"]["$type"]).Split(',').Last().Trim();
            var obj=(BlueprintScriptableObject)Activator.CreateInstance(GameTypes.Single(t=>t.Name==type));
            obj.name=(string)row["name"];obj.AssetGuid=guid;
            if (obj is Kingmaker.Blueprints.Facts.BlueprintUnitFact) Set(obj,"m_Icon",Empty<UnityEngine.Sprite>());
            obj.ComponentsArray=((JArray)bp["Data"]["Components"]??new JArray()).Select(c=>(BlueprintComponent)Read(c,typeof(BlueprintComponent))).ToArray();
            foreach(var property in ((JObject)bp["Data"]).Properties().Where(x=>new[]{"m_AllFeatures","m_Features","Type","Range","ActionType","CanTargetSelf","CanTargetFriends","CanTargetEnemies","CanTargetPoint","SpellResistance","EffectOnEnemy","EffectOnAlly","AvailableMetamagic","SpellsByLevel","m_Type","m_Size","m_Enchantments","m_OverrideDamageDice","m_DamageDice","m_OverrideDamageType","m_DamageType","m_BaseDamage","m_CriticalRollEdge","m_CriticalModifier","m_VisualParameters","m_SecondWeapon","m_AttackType","m_IsLight","m_IsOneHanded","m_IsTwoHanded","m_IsUnarmed","m_IsNatural","m_OverrideAttackBonusStat","m_AttackBonusStatOverride","Prefab","m_Race","Category","DamageType","CriticalRollEdge","CriticalModifier","WeaponFxPrefab","MaterialComponent","Fx","Shape","Size"}.Contains(x.Name))) {
                var field=AccessTools.Field(obj.GetType(),property.Name);if(field!=null)field.SetValue(obj,Read(property.Value,field.FieldType));
            }
            if(obj is BlueprintBuff visualBuff) {
                visualBuff.FxOnStart=(Kingmaker.ResourceLinks.PrefabLink)Read(bp["Data"]["FxOnStart"],typeof(Kingmaker.ResourceLinks.PrefabLink));
                visualBuff.FxOnRemove=(Kingmaker.ResourceLinks.PrefabLink)Read(bp["Data"]["FxOnRemove"],typeof(Kingmaker.ResourceLinks.PrefabLink));
            }
            // Summon equipment must come from the audited native unit, including
            // the ghost's DisableHands flag. A default UnitBody hides that bug.
            if(obj is BlueprintUnit nativeUnit) {
                nativeUnit.Body=(BlueprintUnit.UnitBody)Read(bp["Data"]["Body"],typeof(BlueprintUnit.UnitBody));
            }
            obj.OnEnable();cache.AddCachedBlueprint(guid,obj);
        }
        // Full spell parameter catalogue is required by native Items/CanSelectAny.
        const string spellParametersId="e69a85f633ae8ca4398abeb6fa11b1fe";
        var spellParametersRow=NativeRows.Single(r=>(string)r["blueprint"]["AssetId"]==spellParametersId);
        ((BlueprintParametrizedFeature)cache.Load(BlueprintGuid.Parse(spellParametersId))).BlueprintParameterVariants=
            (AnyBlueprintReference[])Read(spellParametersRow["blueprint"]["Data"]["BlueprintParameterVariants"],typeof(AnyBlueprintReference[]));
        // Companion sharing appends to the actual native level-one entry;
        // the general fixture field whitelist deliberately omits progressions.
        // Hydrate this audited progression instead of inventing an empty entry.
        const string animalProgressionId="f54159e8cfda69f4dbcfdf6ffbbd3293";
        var animalRow=NativeRows.Single(r=>(string)r["blueprint"]["AssetId"]==animalProgressionId);
        ((BlueprintProgression)cache.Load(BlueprintGuid.Parse(animalProgressionId))).LevelEntries=(LevelEntry[])Read(animalRow["blueprint"]["Data"]["LevelEntries"],typeof(LevelEntry[]));
        count += Psychic026Tests.BeforeRegistration(assembly,cache);
        Compatibility01420Tests.Inject(cache);
        Feedback01423Tests.Capture(cache,assembly);
        var original=(BlueprintBuff)cache.Load(BlueprintGuid.Parse("287682389d2011b41b5a65195d9cbc84"));var originalSignature=Signature(original.ComponentsArray);
        var faithDonors=NativeRows.Where(r=>((string)r["name"]).StartsWith("Cure") || ((string)r["name"]).StartsWith("Inflict") || ((string)r["name"]).StartsWith("Prayer")).Select(r=>(BlueprintScriptableObject)cache.Load(BlueprintGuid.Parse((string)r["blueprint"]["AssetId"]))).Distinct().ToDictionary(bp=>bp,bp=>Signature(bp.ComponentsArray));
        var list=(BlueprintSpellList)cache.Load(BlueprintGuid.Parse("6a4b9d6651b155058004354a803da3bf"));var before=list.SpellsByLevel.Sum(l=>l.Spells.Count());
        var donor=cache.Load(BlueprintGuid.Parse("6fd0a849531617844b195f452661b2cd"));
        var warmed=new List<Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment>();
        Set(donor,"m_CachedEnchantments",warmed);
        var wizard=(BlueprintSpellList)cache.Load(BlueprintGuid.Parse("ba0401fdeb4062f40a7aa95b6f07fe89"));
        var wizardTwo=wizard.SpellsByLevel.Single(l=>l.SpellLevel==2);
        var originalWizardSpells=(List<BlueprintAbilityReference>)Get(wizardTwo,"m_Spells");
        var orphan=new BlueprintAbilityReference();Set(orphan,"deserializedGuid",Id("ForceSword"));
        Set(wizardTwo,"m_Spells",originalWizardSpells.Concat(new[]{orphan}).ToList());
        bool orphanRejected=false;
        try {Register(cache);} catch(TargetInvocationException ex) when(ex.InnerException is InvalidOperationException) {orphanRejected=true;}
        Check(orphanRejected && cache.Load(Id("ForceSword"))==null && list.SpellsByLevel.Sum(l=>l.Spells.Count())==before,"Unresolved shared-list GUID is rejected before publishing any extension");
        Set(wizardTwo,"m_Spells",originalWizardSpells);Set(wizardTwo,"m_SpellsProxy",null);Set(wizardTwo,"m_SpellsFiltered",null);
        Register(cache);
        var listContracts01423=Feedback01423Tests.VerifyRegistered(cache,evidence);count+=(int)listContracts01423["assertions"];
        Check(ReferenceEquals(Get(donor,"m_CachedEnchantments"),warmed),"Native donor enchantment cache remains unchanged");
        for(int i=1;i<=3;i++) Check(Get(Asset<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>(cache,"ForceSwordWeapon"+i),"m_CachedEnchantments")==null,"Temporary weapon clears inherited warm enchantment cache");
        var nativeIds=new HashSet<string>(data["allNativeIds"].Select(i=>(string)i));
        foreach(var guid in (string[])AccessTools.Field(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicClass"),"OwnIds").GetValue(null)) Check(!nativeIds.Contains(guid),"Foundation GUID has no collision in the complete native identity inventory");
        var all=(IDictionary)Get(cache,"m_LoadedBlueprints");
        var own=all.Keys.Cast<BlueprintGuid>().Select(cache.Load).Where(b=>b.name!=null && b.name.StartsWith("CotWWrathPsychic")).Cast<BlueprintScriptableObject>().ToArray();
        Check(own.Length>17,"Extension registered spells and discipline assets");
        foreach(var obj in own) Check(!nativeIds.Contains(obj.AssetGuid.ToString()),"Extension identity does not collide with native game: "+obj.name);
        var spells=own.OfType<BlueprintAbility>().Where(a=>a.Type==AbilityType.Spell).ToArray();
        Check(spells.Length==233 && list.SpellsByLevel.Sum(l=>l.Spells.Count())==before+52,"48 spells appended once; sixteen child variants, two touch deliveries and eighteen Faith conversions are not separate learnable spells");
        foreach(var spell in spells.Where(s=>s.Parent==null && !new[]{"Spite","DelayedConsumption","EmblemOfGreed","SpiritBoundBlade","SpiritualWeapon","SpiritualAlly","ArcaneEye","Sanctuary","BattlemindLink","RejuvenateEidolon","FieryShuriken","OrbOfTheVoid","HexVulnerability","FireSeeds","SenseSpiritMagic","SpiritCall","FontOfSpiritMagic","TemporalStasisTouch","LendJudgment","Babble","MagicalBeastShape","UndeadAnatomy","GiantForm","Suffocation","ControlConstruct","ScouringWinds","Screech","CurseMajor","FlamingSphere","AggressiveThundercloud","BallLightning","AirWalk","OverlandFlight","WindWalk","EarthTremor","CondensedEther","BarrowHaze","BowSpirit","AlliedCloak","SmiteAbomination","ObscuringMist","FogCloud","SolidFog","SleetStorm","RiverOfWind","FickleWinds","ThreefoldAspect","BlisteringInvective","WallOfNausea","WallOfBlindness","RighteousVigor","PathOfGlory","Flashfire","ForcefulStrike","AuraOfDoom","LitanyOfTruth","DazzlingBlade","VineStrike","FlamesOfTheFaithful","WrathfulWeapon","FlurryOfSnowballs","TouchOfBloodletting","FlameArrow","Rebuke","SheetLightning"}.Any(t=>s.name.StartsWith("CotWWrathPsychic"+t)) && s.name!="CotWWrathPsychicBloodyClaws" && !s.name.StartsWith("CotWWrathPsychicGhostbaneDirge") && !s.name.StartsWith("CotWWrathPsychicFaith") && s.name!="CotWWrathPsychicSynapseOverloadTouch" && s.name!="CotWWrathPsychicIrresistibleDanceTouch" && s.name!="CotWWrathPsychicSecondWind" && s.name!="CotWWrathPsychicBladeTutor" && s.name!="CotWWrathPsychicChannelVigor" && s.name!="CotWWrathPsychicSilence" && s.name!="CotWWrathPsychicHaltUndead" && s.name!="CotWWrathPsychicControlUndead")) {
            var entry=spell.GetComponent<SpellListComponent>();
            Check(entry.SpellList==list && list.SpellsByLevel[entry.SpellLevel].Spells.Count(s=>s==spell)==1,"Every new spell has a matching circle/list link");
        }
        var progression=(BlueprintProgression)cache.Load(BlueprintGuid.Parse("b14115fd5dd15c59a6075b10ef73f4ee"));
        var selection=Asset<BlueprintFeatureSelection>(cache,"DisciplineSelection");
        Check(progression.LevelEntries[0].Features.Contains(selection) && !progression.LevelEntries[0].Features.Any(f=>f.AssetGuid==BlueprintGuid.Parse("b9cc062eaf665e8b9c2ba35fa107d273")),"Full Ferocity replaces temporary pool choice");
        var fingerprints=own.Select(o=>Signature(o.ComponentsArray)).ToArray();Register(cache);Register(cache);
        Check(fingerprints.SequenceEqual(own.Select(o=>Signature(o.ComponentsArray))) && list.SpellsByLevel.Sum(l=>l.Spells.Count())==before+52,"Extension repeated registration is idempotent");
        Check(originalSignature==Signature(original.ComponentsArray),"Native Transformation and its nested property calculations remain unchanged");
        foreach(var donorState in faithDonors)Check(donorState.Value==Signature(donorState.Key.ComponentsArray),"Faith preserves native donor components: "+donorState.Key.name);
        count += Psychic026Tests.AfterRegistration(assembly,cache);
        TestSpells(cache);TestDiscipline(cache);
        count += PsychicPresentationTests.Run(assembly, cache);TestUndercast(assembly,cache);TestConflicts(cache,list,progression);
        var strings=(Dictionary<string,string>)GetStatic(factory,"Strings");
        foreach(var pair in strings) Check((!string.IsNullOrWhiteSpace(pair.Value) || (pair.Value=="" && (pair.Key.EndsWith(".savingThrow") || pair.Key.EndsWith(".suffix")))) && !System.Text.RegularExpressions.Regex.IsMatch(pair.Value,"[\\p{IsCJKUnifiedIdeographs}]"),"Extension text remains English; only no-save rows may be empty: "+pair.Key);
        Inventory=new JObject { ["englishStrings"]=JObject.FromObject(strings),["assets"]=JArray.FromObject(own.Select(a=>new {guid=a.AssetGuid.ToString(),name=a.name,type=a.GetType().Name})),["exclusiveSpellCount"]=spells.Count(s=>s.Parent==null && s.GetComponent<SpellListComponent>()!=null),["extensionAssertions"]=count };
        Inventory["psychic095Assertions"]=Psychic095Tests.Assertions;
        Inventory["clone026Assertions"]=Psychic026Tests.Count;
        Inventory["spellList01423Contracts"]=listContracts01423;
        Console.WriteLine("PASS: "+count+" Psychic expansion assertions; constructed native managed objects, no Unity or game save lifecycle.");return count;
    }
    internal static JObject Inventory;
    private static object GetStatic(Type type,string field)=>AccessTools.Field(type,field).GetValue(null);
    private static void TestSpells(BlueprintsCache cache)
    {
        var harmony=new Harmony("CotWPsychic.NativeDamageCapture");
        harmony.Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicExpansionTests),nameof(CaptureDamage))));
        try {
            foreach(var sample in new[] { new[] {1,3,3},new[] {1,20,5},new[] {3,20,10},new[] {6,20,20} }) {
                var spell=Asset<BlueprintAbility>(cache,"MindThrust"+sample[0]);
                var damage=(PsychicDirectDamage)spell.GetComponent<AbilityEffectRunAction>().Actions.Actions[0];
                var context=Empty<MechanicsContext>();Set(context,"m_Params",new AbilityParams { CasterLevel=sample[1] });
                capturedDamage=null;using(context.GetDataScope(null)) damage.RunAction();
                Check(capturedDamage!=null && capturedDamage.Value.DiceCountValue.Value==sample[2] && capturedDamage.HalfIfSaved && capturedDamage.DamageType.Type==DamageType.Direct,"Mind Thrust routes capped direct Will-half damage through the native action");
                context.SetParams(new AbilityParams { CasterLevel=20,Metamagic=Metamagic.Intensified });using(context.GetDataScope(null)) damage.RunAction();
                Check(capturedDamage.Value.DiceCountValue.Value==Math.Min(20,damage.Cap+5),"Intensified raises only the Mind Thrust cap by five");
            }
        } finally { harmony.UnpatchAll(harmony.Id); }
        var effects=Asset<BlueprintAbility>(cache,"MindThrust6").GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionConditionalSaved>().Single();
        Check(effects.Succeed.Actions.Length==1 && effects.Failed.Actions.Length==2,"Mind Thrust VI retains distinct successful/failed status branches");
        for(int i=1;i<=5;i++) {
            var buff=Asset<BlueprintBuff>(cache,"MentalBarrier"+i+"Buff");var ac=buff.GetComponent<AddStatBonus>();
            Check(ac.Value==Math.Min(8,2+2*i) && ac.Descriptor==ModifierDescriptor.Shield,"Mental Barrier uses nonstacking shield AC");
            if(i>=4) Check(buff.GetComponent<AddFortification>().Bonus==(i-3)*25,"Mental Barrier fortification 25/50 percent");
        }
        for(int i=1;i<=3;i++) {
            var save=Asset<BlueprintBuff>(cache,"ThoughtShield"+i+"Buff").GetComponent<SavingThrowBonusAgainstDescriptor>();
            Check(save.Value+save.Bonus.Calculate((MechanicsContext)null)==2+2*i && save.ModifierDescriptor==ModifierDescriptor.Circumstance && (SpellDescriptor)save.SpellDescriptor==SpellDescriptor.MindAffecting,"Thought Shield bonus is 4/6/8, not accidentally doubled");
        }
        var host=Asset<BlueprintBuff>(cache,"IntellectFortressHost");var add=host.GetComponent<Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect>();
        var area=add.AreaEffect;Check(area.Size==new Kingmaker.Utility.Feet(20) && area.AffectEnemies,"Intellect Fortress suppresses emotion/fear on both sides within 20 feet");
        Check(area.CanTargetAllies && area.CanTargetEnemies && Get(area,"m_TargetType").GetType().DeclaringType==typeof(BlueprintAbilityAreaEffect),
            "Aura uses its own nested target enum and targets both sides");
        bool wrongEnumRejected=false;
        try { AccessTools.Method(factory,"Set").Invoke(null,new object[]{area,"m_TargetType",TargetType.Any}); }
        catch(TargetInvocationException e) when(e.InnerException is ArgumentException) { wrongEnumRejected=true; }
        Check(wrongEnumRejected,"Cross-enum reflection assignment rejected even on permissive CLR");
        var suppress=Asset<BlueprintBuff>(cache,"IntellectFortressEffect").GetComponent<SuppressBuffs>();
        Check(((SpellDescriptor)suppress.Descriptor & (SpellDescriptor.Fear|SpellDescriptor.Emotion|SpellDescriptor.NegativeEmotion))==(SpellDescriptor.Fear|SpellDescriptor.Emotion|SpellDescriptor.NegativeEmotion),"Intellect Fortress suppression descriptors audited");
        var pulse=Asset<BlueprintAbility>(cache,"SynapticPulseGreater").GetComponent<AbilityEffectRunAction>();
        var branches=(ContextActionConditionalSaved)pulse.Actions.Actions.Single();
        Check(pulse.SavingThrowType==SavingThrowType.Will && pulse.IgnoreCaster && ((ContextActionApplyBuff)branches.Failed.Actions.Single()).DurationValue.DiceType==DiceType.D4 && branches.Succeed.Actions.Length==1,"Greater Synaptic Pulse uses Will branches, 1d4 stun and successful-save sickness, excludes caster");
    }
    private static bool CaptureDamage(ContextActionDealDamage __instance) { capturedDamage=__instance;return false; }
    private static void TestDiscipline(BlueprintsCache cache)
    {
        var discipline=Asset<BlueprintProgression>(cache,"FerocityDiscipline");
        Check(discipline.LevelEntries.Select(e=>e.Level).SequenceEqual(new[] {1,4,5,6,8,10,12,13,14,16,18}),"Ferocity has all nine spell milestones and level 1/5/13 powers");
        var character=(BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("83fa3b50138c566698801ba5967bbde0"));
        foreach(var bonus in discipline.LevelEntries.SelectMany(e=>e.Features).SelectMany(f=>f.ComponentsArray).OfType<AddKnownSpell>()) Check(bonus.CharacterClass==character && bonus.Spell.Type==AbilityType.Spell,"Discipline bonus spell targets the private Psychic book");
        var low=Asset<BlueprintFeature>(cache,"FerocitySensesLow");var high=Asset<BlueprintFeature>(cache,"FerocitySensesHigh");
        var gated=Asset<BlueprintFeature>(cache,"FerocitySenses").GetComponents<AddFeatureOnClassLevel>().ToArray();
        Check(gated.Length==2 && gated.Single(g=>g.BeforeThisLevel).Feature==low && gated.Single(g=>!g.BeforeThisLevel).Feature==high && gated.All(g=>g.Level==11 && g.Class==character),"Senses ability is replaced at level 11");
        Check(high.GetComponent<Blindsense>().Range==new Kingmaker.Utility.Feet(30) && !high.GetComponent<Blindsense>().Blindsight,"Level 11 permanent blindsense remains distinct from paid blindsight");
        foreach(var token in new[] {"FerocitySensesLowAbility","FerocitySensesHighAbility"}) {
            var ability=Asset<BlueprintAbility>(cache,token);var cost=ability.GetComponent<AbilityResourceLogic>();
            Check(cost.Amount==1 && cost.IsSpendResource && cost.RequiredResource.AssetGuid==BlueprintGuid.Parse("ddfc9f9a2cbd51b48cbd214dbd75c8fb") && ability.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,"Enhanced Senses costs one Wisdom phrenic point and a standard action");
        }
        var threshold=Asset<BlueprintFeature>(cache,"FerocityWounded").GetComponent<Kingmaker.Designers.Mechanics.Buffs.BuffOnHealthTickingTrigger>();Check(threshold.HealthPercent==0.25f,"Native health trigger uses strict below-quarter boundary");
        var toggle=Asset<BlueprintActivatableAbility>(cache,"FerocityPrimalFuryToggle");
        Check((Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType)Get(toggle,"m_ActivateWithUnitCommand")==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free && toggle.DeactivateIfOwnerDisabled && toggle.DeactivateIfOwnerUnconscious && toggle.GetComponents<RestrictionHasUnitCondition>().Count()==2,"Primal Fury free toggle ends on incapacity and blocks fatigue/exhaustion");
        Check(toggle.GetComponent<ActivatableAbilityResourceLogic>().SpendType==ActivatableAbilityResourceLogic.ResourceSpendType.NewRound,"Primal Fury spends native daily rounds");
        var furyBuff=toggle.Buff;var native=(BlueprintBuff)cache.Load(BlueprintGuid.Parse("287682389d2011b41b5a65195d9cbc84"));
        Check(Signature(furyBuff.ComponentsArray.Take(native.ComponentsArray.Length).ToArray())==Signature(native.ComponentsArray),"Primal Fury retains every native Transformation component/value");
        Check(!ReferenceEquals(furyBuff.ComponentsArray[0],native.ComponentsArray[0]),"Transformation components are copied before extension");
    }
    private static void TestUndercast(Assembly assembly,BlueprintsCache cache)
    {
        var unit=Empty<UnitEntityData>();var descriptor=Empty<UnitDescriptor>();Set(unit,"<Descriptor>k__BackingField",descriptor);Set(descriptor,"<Unit>k__BackingField",unit);Set(unit,"Parts",new EntityPartsManager(unit));
        var proxy=FormatterServices.GetUninitializedObject(typeof(EntityService).GetNestedType("Proxy",BindingFlags.Public|BindingFlags.NonPublic));Set(proxy,"<Entity>k__BackingField",unit);Set(unit,"<Proxy>k__BackingField",proxy);Set(unit,"<UniqueId>k__BackingField","psychic-fixture");
        var book=Empty<Spellbook>();Set(book,"Blueprint",cache.Load(BlueprintGuid.Parse("3ce26d41e96b5ca289f0c6ce9db28d3c")));Set(book,"<Owner>k__BackingField",descriptor);
        Set(book,"m_KnownSpells",Enumerable.Range(0,10).Select(i=>new List<AbilityData>()).ToArray());Set(book,"m_SpecialSpells",Enumerable.Range(0,10).Select(i=>new List<AbilityData>()).ToArray());Set(book,"m_CustomSpells",Enumerable.Range(0,10).Select(i=>new List<AbilityData>()).ToArray());
        Set(book,"m_KnownSpellLevels",new Dictionary<BlueprintAbility,List<int>>());Set(book,"m_SpecialLists",new List<BlueprintSpellList>());
        Set(book,"m_SpellConversionLists",new Dictionary<string,BlueprintAbility[]>());
        var factManager = new EntityFactsManager(unit); Set(unit,"Facts",factManager);
        var abilities = new AbilityCollection(); abilities.Initialize(factManager); Set(unit,"<Abilities>k__BackingField",abilities);
        Set(book,"m_MemorizedSpells",Enumerable.Range(0,10).Select(i=>new List<SpellSlot>()).ToArray());
        Set(book,"m_HiddenCommonSlots",new Dictionary<int,Dictionary<int,SpellSlot>>());Set(book,"m_HiddenFavoriteSlots",new Dictionary<int,Dictionary<int,SpellSlot>>());
        book.Blueprint.SpellsPerDay.Levels=Enumerable.Range(0,21).Select(i=>new SpellsLevelEntry {Count=Enumerable.Range(0,10).Select(c=>c<7?0:-1).ToArray()}).ToArray();
        book.Blueprint.SpellsKnown.Levels=Enumerable.Range(0,21).Select(i=>new SpellsLevelEntry {Count=new int[10]}).ToArray();
        Set(descriptor,"m_Spellbooks",new Dictionary<BlueprintSpellbook,Spellbook> {{book.Blueprint,book}});
        var progression=Empty<UnitProgressionData>();Set(progression,"m_MythicLevel",(int?)0);Set(progression,"m_CharacterLevel",(int?)20);Set(progression,"Classes",new List<ClassData>());Set(descriptor,"Progression",progression);
        var stats=Empty<CharacterStats>();Set(stats,"<Owner>k__BackingField",descriptor);var intelligence=new ModifiableValueAttributeStat(stats,StatType.Intelligence);var wisdom=new ModifiableValueAttributeStat(stats,StatType.Wisdom);
        Set(intelligence,"Type",StatType.Intelligence);Set(intelligence,"<ModifiedValue>k__BackingField",18);Set(wisdom,"Type",StatType.Wisdom);Set(wisdom,"<ModifiedValue>k__BackingField",14);
        Set(intelligence,"m_BaseValue",18);Set(wisdom,"m_BaseValue",14);
        Set(stats,"Intelligence",intelligence);Set(stats,"Wisdom",wisdom);Set(stats,"<AllStats>k__BackingField",new List<ModifiableValue> {intelligence,wisdom});Set(descriptor,"Stats",stats);
        var state=Empty<UnitState>();var mechanics=Empty<UnitMechanicFeatures>();Set(mechanics,"DomainsByIntelligenceFeature",new Kingmaker.Utility.CountableFlag());Set(state,"Features",mechanics);Set(descriptor,"State",state);
        var classData=new ClassData(book.Blueprint.CharacterClass);((List<ClassData>)Get(progression,"Classes")).Add(classData);
        var rounds=Asset<BlueprintAbilityResource>(cache,"FerocityPrimalFuryRounds");var phrenic=(BlueprintAbilityResource)cache.Load(BlueprintGuid.Parse("ddfc9f9a2cbd51b48cbd214dbd75c8fb"));
        var harmony=new Harmony("CotWPsychic.UndercastFunctional");var undercast=assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicUndercasting",true);
        harmony.Patch(AccessTools.Method(assembly.GetType("CallOfTheWild.Wrath.Prototype.ModEntry"),"LogSpellDiagnostic"),prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicExpansionTests),nameof(CaptureLearning))));
        harmony.Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor),"IsPlayerFaction"),prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicExpansionTests),nameof(FixtureFaction))));
        harmony.Patch(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel),"Exception",new[] {typeof(Exception),typeof(string),typeof(object[])}),prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicExpansionTests),nameof(ThrowFixtureLog))));
        foreach(var name in new[] {"LearnedPatch","RemovedPatch","LoadedPatch","ReplacementPatch"}) harmony.CreateClassProcessor(undercast.GetNestedType(name,BindingFlags.NonPublic)).Patch();
        // Execute the native page-reuse decision, bypassing only its rendering
        // update callback. This catches a stale page even when slot data is right.
        var phaseType=typeof(LevelUpState).Assembly.GetType("Kingmaker.UI.MVVM._VM.CharGen.Phases.Spells.CharGenSpellsPhaseVM",true);
        harmony.Patch(AccessTools.Method(phaseType,"UpdateSelection"),prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicExpansionTests),nameof(SkipPhaseRender))));
        try {
            foreach(var sample in new[]{new[]{"bd81a3931aa285a4f9844585b5d97e51","1"},new[]{"c7104f7526c4c524f91474614054547e","2"},new[]{"7658b74f626c56a49939d9c20580885e","3"},new[]{"d2aeac47450c76347aebbc02e4f463e0","4"}}) {
                var spell=NativeCase<BlueprintAbility>(sample[0]);int circle=int.Parse(sample[1]);
                cache.AddCachedBlueprint(spell.AssetGuid,spell);
                var learnState=Empty<LevelUpState>();Set(learnState,"SpellSelections",new List<SpellSelectionData>());
                learnState.DemandSpellSelection(book.Blueprint,book.Blueprint.SpellList).SetLevelSpells(circle,1);
                new SelectSpell(book.Blueprint,book.Blueprint.SpellList,circle,spell,0).Apply(learnState,descriptor);
                Check(book.IsKnownOnLevel(spell,circle),spell.name+" native SelectSpell retains the chosen circle");
                Check(Kingmaker.UI.Common.UIUtilityUnit.GetKnownSpellsForLevel(circle,book).Any(a=>a.Blueprint==spell),spell.name+" native UI data includes learned spell");
                book.PostLoad(descriptor);Check(book.IsKnown(spell),spell.name+" survives managed PostLoad (not real save serialization)");
                book.RemoveSpell(spell);book.AddKnown(circle,spell,false);
                Check(book.IsKnownOnLevel(spell,circle),spell.name+" direct native AddKnown");book.RemoveSpell(spell);
                Check(learningLogs.Any(s=>s.Contains("SelectSpell after circle="+circle) && s.Contains(spell.AssetGuid+"=circle:"+circle)),spell.name+" selection diagnostic sees retained known data");
                Check(learningLogs.Any(s=>s.Contains("RemoveSpell;") && s.Contains("spell="+spell.name+"/") && s.Contains("caller=")),spell.name+" removal diagnostic captures its caller");
            }
            foreach(var token in new[]{"PainStrike","PainStrikeMass","Synesthesia","SynesthesiaMass","SynapseOverload","BurstOfForce","TelekineticStorm","PsychicCrush1","PsychicCrush2","PsychicCrush3","PsychicCrush4","PsychicCrush5"}) {
                var spell=Asset<BlueprintAbility>(cache,token);int circle=spell.GetComponents<SpellListComponent>().Single(c=>c.SpellList==book.Blueprint.SpellList).SpellLevel;
                var state101=Empty<LevelUpState>();Set(state101,"SpellSelections",new List<SpellSelectionData>());
                state101.DemandSpellSelection(book.Blueprint,book.Blueprint.SpellList).SetLevelSpells(circle,1);
                var select=new SelectSpell(book.Blueprint,book.Blueprint.SpellList,circle,spell,0);select.Apply(state101,descriptor);
                Check(book.IsKnownOnLevel(spell,circle),token+" native SelectSpell retains the paid choice");
                Check(Kingmaker.UI.Common.UIUtilityUnit.GetKnownSpellsForLevel(circle,book).Count(a=>a.Blueprint==spell)==1,token+" appears once in native spellbook UI data");
                book.PostLoad(descriptor);Check(book.IsKnownOnLevel(spell,circle),token+" survives managed PostLoad (not a real save)");
                book.RemoveSpell(spell);
            }
            foreach (var token in new[] { "BurstOfAdrenaline", "BurstOfInsight" }) {
                var spell = Asset<BlueprintAbility>(cache, token);
                var burstState = Empty<LevelUpState>(); Set(burstState, "SpellSelections", new List<SpellSelectionData>());
                burstState.DemandSpellSelection(book.Blueprint, book.Blueprint.SpellList).SetLevelSpells(1, 1);
                new SelectSpell(book.Blueprint, book.Blueprint.SpellList, 1, spell, 0).Apply(burstState, descriptor);
                Check(book.IsKnown(spell), token + " is actually learned by native SelectSpell");
                var known = Kingmaker.UI.Common.UIUtilityUnit.GetKnownSpellsForLevel(1, book).Single(a => a.Blueprint == spell);
                Check(known.IsVariable && known.GetConversions().Count() == 3, token + " appears in native spellbook with three conversions");
                foreach (var variant in known.GetConversions()) Check(variant.Spellbook == book && variant.SpellLevel == 1, token + " variant retains spellbook and level");
                book.PostLoad(descriptor);
                Check(book.IsKnown(spell), token + " survives native PostLoad");
                book.RemoveSpell(spell);
                book.AddKnown(1, spell, false);
                Check(book.IsKnown(spell), token + " supports direct native AddKnown");
                book.RemoveSpell(spell);
            }
            foreach(int level in new[] {1,4,13,20}) { classData.Level=level;Check(rounds.GetMaxAmount(descriptor)==level,"Native Primal Fury resource uses Psychic level only");Check(phrenic.GetMaxAmount(descriptor)==level/2+2,"Native Wisdom phrenic maximum is half level plus permanent modifier"); }
            var one=Asset<BlueprintAbility>(cache,"MindThrust1");var two=Asset<BlueprintAbility>(cache,"MindThrust2");var three=Asset<BlueprintAbility>(cache,"MindThrust3");var six=Asset<BlueprintAbility>(cache,"MindThrust6");
            book.AddKnown(6,six,false);
            Check(book.GetAllKnownSpells().Count()==6 && book.GetAllKnownSpells().Count(a=>!a.IsTemporary)==1,"Learning VI grants five temporary lower spells without known-spell quota");
            Check(book.GetKnownSpell(one).SpellLevel==1 && book.GetKnownSpell(three).SpellLevel==3,"Undercast retains lower spell-slot circles");
            Check(typeof(AbilityData).GetProperty("IsTemporary").GetCustomAttributes(false).Any(a=>a.GetType().Name=="JsonPropertyAttribute"),"Undercast provenance uses the native serialized temporary flag");
            book.RemoveTemporarySpell(book.GetKnownSpell(one));book.PostLoad(descriptor);
            Check(book.IsKnown(one) && book.GetKnownSpell(one).IsTemporary,"Native managed PostLoad hook reconstructs missing undercast entries");
            AccessTools.Method(undercast,"Reconcile").Invoke(null,new object[] {book});Check(book.GetAllKnownSpells().Count()==6,"Reconcile does not duplicate temporary lower spells");
            book.AddKnown(2,two,false);Check(!book.GetKnownSpell(two).IsTemporary,"Normal learning promotes an undercast spell to permanent");
            book.AddKnown(3,three,false);book.RemoveSpell(six);
            Check(book.IsKnown(one) && book.IsKnown(two) && book.IsKnown(three) && !book.IsKnown(Asset<BlueprintAbility>(cache,"MindThrust4")),"Removing VI preserves independently learned II/III and their lower versions");
            book.RemoveSpell(three);book.RemoveSpell(two);Check(!book.GetAllKnownSpells().Any(),"Removing final family anchors removes only the generated lower versions");
            // Native SelectSpell.Apply with a real SpellSelectionData, without UI.
            book.AddKnown(1,one,false);book.AddKnown(3,three,false);
            var levelState=Empty<LevelUpState>();Set(levelState,"SpellSelections",new List<SpellSelectionData>());
            var selection=levelState.DemandSpellSelection(book.Blueprint,book.Blueprint.SpellList);
            selection.SetLevelSpells(6,1); selection.SetLevelSpells(1,1);
            var chosen=(BlueprintAbility)cache.Load(BlueprintGuid.Parse("4ac47ddb9fa1eaf43a1b6809980cfbd2"));
            selection.SpendSlot(1,chosen,0);
            var previousSelection=selection;
            var phase=FormatterServices.GetUninitializedObject(phaseType);Set(phase,"m_SelectionData",previousSelection);
            var matches=AccessTools.Method(phaseType,"TryUpdateSamePhase");
            // Reproduce 0.6.4: extending the same object is invisible to the UI.
            var stale=new SpellSelectionData(book.Blueprint,book.Blueprint.SpellList);stale.SetLevelSpells(6,1);
            var stalePhase=FormatterServices.GetUninitializedObject(phaseType);Set(stalePhase,"m_SelectionData",stale);
            stale.SetLevelSpells(1,1);
            Check((bool)matches.Invoke(stalePhase,new object[] {stale}),"Old in-place extension incorrectly reuses the native page and hides a newly added circle");
            selection.ExtraMaxLevel=3;selection.ExtraByStat=true;selection.ExtraSelected=new[] {chosen};
            var learn=new SelectSpell(book.Blueprint,book.Blueprint.SpellList,6,six,0);
            learn.Apply(levelState,descriptor);
            selection=levelState.DemandSpellSelection(book.Blueprint,book.Blueprint.SpellList);
            Check(!ReferenceEquals(selection,previousSelection) && previousSelection.LevelCount[1].SpellSelections.Length==1 && previousSelection.LevelCount[3]==null,"Original UI snapshot retains its slot counts after higher spell selection");
            Check(!(bool)matches.Invoke(phase,new object[] {selection}),"Native UI rejects the old page immediately so current-level low-circle pages can be rebuilt");
            Check(selection.ExtraMaxLevel==3 && selection.ExtraByStat && selection.ExtraSelected[0]==chosen && !ReferenceEquals(selection.ExtraSelected,previousSelection.ExtraSelected),"Replacement snapshot preserves and separately owns extra spell selections");
            Check(selection.LevelCount[6].SpellSelections[0]==six && !ReferenceEquals(selection.LevelCount[6].SpellSelections,previousSelection.LevelCount[6].SpellSelections),"Higher spell choice is retained in a separate slot array");
            Check(selection.LevelCount[1].SpellSelections.Length==2 && selection.LevelCount[3].SpellSelections.Length==1,"Every independently known lower circle receives one native replacement slot");
            Check(selection.LevelCount[1].SpellSelections[0]==chosen,"Appending a replacement preserves an earlier choice in the same circle");
            Check(book.GetKnownSpell(one).IsTemporary && book.GetKnownSpell(three).IsTemporary,"Replaced lower spells become serialized free undercasts instead of retaining paid known slots");
            Check(selection.LevelCount[2]==null,"Automatically granted II does not earn a replacement");
            learn.Apply(levelState,descriptor);
            Check(selection.LevelCount[1].SpellSelections.Length==2 && selection.LevelCount[3].SpellSelections.Length==1,"Reapplying an already known higher spell cannot grant extra slots");
            new SelectSpell(book.Blueprint,book.Blueprint.SpellList,1,chosen,1).Apply(levelState,descriptor);
            Check(!book.GetKnownSpell(chosen).IsTemporary && book.GetKnownSpell(six)!=null,"Replacement is learned through the ordinary spell selection action");
            book.RemoveSpell(six);book.RemoveSpell(chosen);
            Check(!book.IsKnown(one) && !book.IsKnown(three),"Removing higher anchor cleans replaced undercasts without leaving paid copies");
            // A rebuilt preview/commit starts from the original paid known list.
            book.AddKnown(1,one,false);book.AddKnown(3,three,false);
            var rebuilt=Empty<LevelUpState>();Set(rebuilt,"SpellSelections",new List<SpellSelectionData>());
            var replay=rebuilt.DemandSpellSelection(book.Blueprint,book.Blueprint.SpellList);
            for(int circle=0;circle<replay.LevelCount.Length;circle++) replay.SetLevelSpells(circle,0);
            replay.SetLevelSpells(6,1);
            var replayPhase=FormatterServices.GetUninitializedObject(phaseType);Set(replayPhase,"m_SelectionData",replay);
            new SelectSpell(book.Blueprint,book.Blueprint.SpellList,6,six,0).Apply(rebuilt,descriptor);
            replay=rebuilt.DemandSpellSelection(book.Blueprint,book.Blueprint.SpellList);
            Check(replay.LevelCount[1].SpellSelections.Length==1 && replay.LevelCount[3].SpellSelections.Length==1,"Fresh preview/commit replay grants exactly the original replacement entitlement");
            Check(!(bool)matches.Invoke(replayPhase,new object[] {replay}) && replay.LevelCount.All(c=>c!=null),"Native phase matching detects compensation even when no low-circle page existed before learning the higher spell");
            Check(replay.HasEmpty() && replay.CanSpendSlot(1,0) && replay.CanSpendSlot(3,0),"Replacement choices are required and selectable in the same level-up state");
            book.RemoveSpell(six);
            book.AddKnown(1,one,false);book.AddKnown(3,three,false);book.AddKnown(6,six,false);
            var legacy=Empty<LevelUpState>();Set(legacy,"SpellSelections",new List<SpellSelectionData>());Set(legacy,"<SelectedClass>k__BackingField",book.Blueprint.CharacterClass);
            var repair=AccessTools.Method(undercast.GetNestedType("ExistingReplacementsPatch",BindingFlags.NonPublic),"Postfix");
            repair.Invoke(null,new object[] {legacy,descriptor});
            var legacyChoices=legacy.DemandSpellSelection(book.Blueprint,book.Blueprint.SpellList);
            Check(legacyChoices.LevelCount[1].SpellSelections.Length==1 && legacyChoices.LevelCount[3].SpellSelections.Length==1,"Legacy paid lower versions receive replacement choices at next Psychic level-up");
            repair.Invoke(null,new object[] {legacy,descriptor});
            Check(legacyChoices.LevelCount[1].SpellSelections.Length==1 && legacyChoices.LevelCount[3].SpellSelections.Length==1,"Legacy repair is idempotent after serialized temporary conversion");
            book.PostLoad(descriptor);repair.Invoke(null,new object[] {legacy,descriptor});
            Check(legacyChoices.LevelCount[1].SpellSelections.Length==1,"PostLoad does not duplicate refunded known-spell choices");
            book.RemoveSpell(six);
            var ordinary=(BlueprintSpellbook)cache.Load(BlueprintGuid.Parse("b3db3766a4b605040b366265e2af0e50"));Set(book,"Blueprint",ordinary);book.AddKnown(6,six,false);
            Check(book.GetAllKnownSpells().Count()==1,"Other spellbooks do not gain automatic psychic undercasts");
            count+=Psychic095Tests.Run(cache);
        } finally { harmony.UnpatchAll(harmony.Id); }
    }
    private static bool ThrowFixtureLog(Exception __0) { throw new Exception("Native fixture failure",__0); }
    private static bool FixtureFaction(ref bool __result) { __result=false;return false; }
    private static bool SkipPhaseRender() => false;
    private static void TestConflicts(BlueprintsCache cache,BlueprintSpellList list,BlueprintProgression progression)
    {
        var victim=Asset<BlueprintBuff>(cache,"MentalBarrier1Buff");var component=victim.GetComponent<AddStatBonus>();int previous=component.Value;component.Value=99;
        var before=list.SpellsByLevel.Sum(l=>l.Spells.Count());bool rejected=false;
        try { Register(cache); } catch(TargetInvocationException ex) when(ex.InnerException is InvalidOperationException) {rejected=true;}
        Check(rejected && list.SpellsByLevel.Sum(l=>l.Spells.Count())==before && component.Value==99,"Changed component value is rejected without overwriting content");component.Value=previous;
    }
}
