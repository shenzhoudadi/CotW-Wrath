using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class Feedback01448Tests
{
    static readonly Type Helper=typeof(Arcane01446Tests);
    static readonly Assembly Main=typeof(NaturesCommand01447).Assembly;
    static Type T(string n)=>Main.GetType("CallOfTheWild.Wrath.Prototype."+n,true);
    static object Call(Type t,string name,params object[] args)=>AccessTools.Method(t,name).Invoke(null,args);
    static object H(string name,params object[] args)=>Call(Helper,name,args);
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static BlueprintGuid Id(string token)=>(BlueprintGuid)Call(T("PsychicAssets"),"Id",token);
    static TAsset A<TAsset>(BlueprintsCache c,string token)where TAsset:SimpleBlueprint=>(TAsset)c.Load(Id(token));
    static TAsset N<TAsset>(BlueprintsCache c,string id)where TAsset:SimpleBlueprint=>(TAsset)c.Load(BlueprintGuid.Parse(id));
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception("Feedback01448: "+message);checks++;}
    static string Root(){var p=new DirectoryInfo(Environment.CurrentDirectory);while(p!=null&&!File.Exists(Path.Combine(p.FullName,"HANDOFF.md")))p=p.Parent;return p.FullName;}
    static object Invoke(object o,string name,params object[] args)=>o.GetType().GetMethods().Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(o,args);
    static void NativeProxyClone(BlueprintProgression progression)
    {
        var first=progression.LevelEntries.Single(e=>e.Level==1);var donor=progression.LevelEntries.First(e=>e.Level==3);
        var oldProxy=donor.Features; // execute the real native lazy getter
        var clone=(LevelEntry)AccessTools.Method(typeof(object),"MemberwiseClone").Invoke(donor,null);
        clone.Level=1;Set(clone,"m_Features",new System.Collections.Generic.List<BlueprintFeatureBaseReference>((System.Collections.Generic.List<BlueprintFeatureBaseReference>)AccessTools.Field(typeof(LevelEntry),"m_Features").GetValue(first)));
        progression.LevelEntries=progression.LevelEntries.Select(e=>e==first?clone:e).ToArray();
        Check(ReferenceEquals(oldProxy,clone.Features),"native shallow clone retains a foreign resolved proxy while owning independent authored level1 references");
    }
    static void FamiliarChoices(BlueprintsCache c,Harmony h)
    {
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Blueprints.Classes.Prerequisites.IgnorePrerequisites),"Ignore"),prefix:new HarmonyMethod(typeof(Feedback01448Tests),nameof(No)));
        var familiar=A<BlueprintFeatureSelection>(c,"BloodlineFamiliar01446");var rows=(string[][])Call(T("BloodlineCompatibility01447"),"Rows",c);int native=0,seeker=0,primal=0;
        var seekerIds=new System.Collections.Generic.HashSet<string>(((string[][])AccessTools.Field(T("BloodlineVariants01447"),"Rows").GetValue(null)).Select(r=>r[0]));var primalIds=new System.Collections.Generic.HashSet<string>(new[]{"Air","Earth","Fire","Water"}.Select(e=>Id("Primal"+e+"Progression01444").ToString()));
        foreach(var row in rows.Where(r=>r[2]!="")){
            var progression=N<BlueprintProgression>(c,row[0]);var choice=progression.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Single(s=>s.AllFeatures.Contains(familiar));
            var u=(UnitEntityData)H("Unit",c,row[1]=="Sorcerer"?"b3a505fb61437dc4097f43c3f8f9a4cf":"d77e67a814d686842802c9cfd8ef8499",1);
            var state=(Kingmaker.UnitLogic.Class.LevelUp.LevelUpState)Call(typeof(Feat01424Tests),"State",u,1);var pending=state.AddSelection(null,default(FeatureSource),choice,1);
            Check(choice.CanSelect(u.Descriptor,state,pending,familiar),"native level-up CanSelect exposes Familiar for "+progression.name);
            if(primalIds.Contains(row[0]))primal++;else if(seekerIds.Contains(row[0]))seeker++;else native++;
        }
        Check(seeker==21&&primal==4&&native==45,"all45 replaceable native, Seeker21 and Primal4 actual level1 paths expose a single Familiar option; Empyreal has no level1 power to exchange");
        PsychicExpansionTests.Inventory["bloodlineReference01448Contracts"]=new JObject{["passed"]=true,["nativeLevelUpCanSelect"]=true,["nativeBloodlines"]=native,["nativeRowsAudited"]=46,["seekerBloodlines"]=seeker,["primalBloodlines"]=primal,["nativeShallowCloneProxy"]=true,["actualProviderConfigurator"]=true,["boundary"]="Actual native shallow cloning/lazy proxies, released ECM BlueprintCore selection configurator and native level-up CanSelect execute. Runtime available parent pools and Unity Sprite-name lookup are controlled inputs. Log identifies an anchor mismatch but does not serialize the user's live cache; the exact external write which produced that user's cache is not established."};
    }
    static void ProviderWrapper(BlueprintsCache parent,Harmony h)
    {
        var c=(BlueprintsCache)H("Fresh",parent);
        var ecm=Assembly.LoadFrom(Path.Combine(Root(),"work/batch01447/dependencies/ECM/EbonsContentMod.dll"));
        if(Environment.GetEnvironmentVariable("COTW_01448_PROVIDER_FULL")=="1"||Environment.GetEnvironmentVariable("COTW_01448_CASE")=="providerfull") {
            Assembly.LoadFrom(Path.Combine(Root(),"work/repair01440/references/UnityModManager.dll"));
            Assembly.LoadFrom(Path.Combine(Root(),"work/repair01440/dependencies/TabletopTweaks-Core/TabletopTweaks-Core/TabletopTweaks-Core.dll"));
            var log=AccessTools.Field(ecm.GetType("EbonsContentMod.Main",true),"log");
            log.SetValue(null,System.Runtime.Serialization.FormatterServices.GetUninitializedObject(log.FieldType));
            foreach(var method in log.FieldType.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Where(m=>m.ReturnType==typeof(void)&&m.Name=="Log"))h.Patch(method,prefix:new HarmonyMethod(typeof(Feedback01448Tests),nameof(Skip)));
            var sprite=ecm.GetType("BlueprintCore.Utils.Assets.Asset`1",true).MakeGenericType(typeof(UnityEngine.Sprite));
            h.Patch(AccessTools.Constructor(sprite,new[]{typeof(UnityEngine.Sprite)}),prefix:new HarmonyMethod(typeof(Feedback01448Tests),nameof(SpriteEndpoint)));
            // The archive's m_Features is empty; the running game's available
            // pool is populated from these exact native m_AllFeatures entries.
            foreach(string selectionId in new[]{"24bef8d1bee12274686f6da6ccbc8914","62b33ac8ceb18dd47ad4c8f06849bc01"}) {
                var selection=N<BlueprintFeatureSelection>(c,selectionId);
                if(selection!=null)Set(selection,"m_Features",selection.AllFeatures.Select(BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>).ToArray());
            }
            // ECM explicitly skips entries whose native lazy proxy has not yet
            // been read. Other startup consumers read these in the game.
            foreach(var progression in ((SimpleBlueprint[])H("Loaded",c)).OfType<BlueprintProgression>())foreach(var le in progression.LevelEntries){var nativeFeatures=le.Features.Count;}
            Call(ecm.GetType("EbonsContentMod.Bloodlines.BloodlineMutations",true),"Configure");
            var actual=N<BlueprintProgression>(c,"7bd143ead2d6c3a409aad6ee22effe34");
            File.WriteAllText(Path.Combine(Root(),"work/repair01448/actual-ECM-configure.json"),new JArray(actual.LevelEntries.Select(e=>new JObject{["level"]=e.Level,["features"]=new JArray(e.Features.Select(f=>new JObject{["guid"]=f.AssetGuid.ToString(),["name"]=f.name,["options"]=f is BlueprintFeatureSelection s?new JArray(s.AllFeatures.Select(x=>x.AssetGuid.ToString())):new JArray()}))})).ToString());
            NativeProxyClone(actual);
            Call(T("BloodlineMutations01446"),"Register",c);FamiliarChoices(c,h);PsychicExpansionTests.Inventory["providerFull01448Contracts"]=new JObject{["passed"]=true,["actualFullECMConfigure"]=true,["nativeProxyClone"]=true,["familiarNativeSelections"]=70};return;
        }
        var p=N<BlueprintProgression>(c,"7bd143ead2d6c3a409aad6ee22effe34");var power=N<BlueprintFeature>(c,"b80b73ef73535d54fbf76654c32f606b");
        var provider=new BlueprintFeature{AssetGuid=BlueprintGuid.Parse("5d61fcd8c3d545da9f97e1b2fda8218b"),name="BloodHavocBloodlineMutation",ComponentsArray=new[]{(BlueprintComponent)Activator.CreateInstance(ecm.GetType("EbonsContentMod.Components.BloodHavoc",true),true)}};
        provider.ComponentsArray[0].name="ECMHavoc";provider.ComponentsArray[0].OwnerBlueprint=provider;c.AddCachedBlueprint(provider.AssetGuid,provider);
        var guid=(string)Call(ecm.GetType("EbonsContentMod.Bloodlines.BloodlineMutations",true),"CreateDeterministicGuid","EbonsContentMod.BloodlineMutationSelection."+p.AssetGuid+".Level1");
        var configurator=ecm.GetType("BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection.FeatureSelectionConfigurator",true);
        var config=configurator.GetMethods().Single(m=>m.Name=="New"&&m.GetParameters().Length==3).Invoke(null,new object[]{p.name+"Level1BloodlineMutationSelection",guid,Array.Empty<FeatureGroup>()});
        var bt=ecm.GetType("BlueprintCore.Utils.Blueprint`1",true).MakeGenericType(typeof(BlueprintFeatureReference));
        var implicitBlueprint=bt.GetMethods().Single(m=>m.Name=="op_Implicit"&&m.GetParameters()[0].ParameterType==typeof(SimpleBlueprint));
        var options=Array.CreateInstance(bt,2);options.SetValue(implicitBlueprint.Invoke(null,new object[]{power}),0);options.SetValue(implicitBlueprint.Invoke(null,new object[]{provider}),1);
        Invoke(config,"AddToAllFeatures",options);var wrapper=(BlueprintFeatureSelection)Invoke(config,"Configure",false);
        var entry=p.LevelEntries.Single(e=>e.Level==1);
        p.LevelEntries=p.LevelEntries.Select(e=>(LevelEntry)Call(T("ArchetypeBatch01445"),"Entry",e.Level,e.Features.Select(x=>x.AssetGuid==power.AssetGuid?wrapper:x).ToArray())).ToArray();
        var report=new JObject{["wrapperGuid"]=wrapper.AssetGuid.ToString(),["features"]=new JArray(wrapper.Features.Select(f=>f.AssetGuid.ToString())),["allFeatures"]=new JArray(wrapper.AllFeatures.Select(f=>f.AssetGuid.ToString())),["components"]=new JArray(wrapper.ComponentsArray.Select(x=>x.GetType().FullName)),["level1"]=new JArray(p.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>e.Features).Select(x=>x.AssetGuid.ToString()))};
        var path=Path.Combine(Root(),"work/repair01448");Directory.CreateDirectory(path);File.WriteAllText(Path.Combine(path,"actual-BC-wrapper.json"),report.ToString());
        Check(wrapper.AllFeatures.Any(f=>f.AssetGuid==power.AssetGuid),"actual released BlueprintCore wrapper resolves its original power");
        NativeProxyClone(p);
        Call(T("BloodlineMutations01446"),"Register",c);
        Check(A<BlueprintFeatureSelection>(c,"BloodlineFamiliar01446")!=null,"actual provider-created selection permits complete Familiar publication");
        FamiliarChoices(c,h);
    }
    static void Nature(BlueprintsCache parent,Harmony h)
    {
        var c=(BlueprintsCache)H("Fresh",parent);if(c.Load(Id("PhrenicAmplificationSelection"))==null)Call(T("PsychicAmplifications"),"Register",c);Call(T("MagaambyanTelepath01447"),"Register",c);
        foreach(string name in new[]{"NatureSpellImmunity01447Patch","NatureBuffImmunity01447Patch","NatureAmplificationSwitch01447Patch","NatureTargetScope01448Patch","NatureTargetFact01448Patch","NaturePaidTarget01448Patch"})if(Main.GetType("CallOfTheWild.Wrath.Prototype."+name)!=null)Feedback01450Tests.PatchOnce(h,T(name));
        foreach(var type in typeof(BlueprintsCache).Assembly.GetTypes().Where(t=>t.Name=="ReplaceSourceBone"||t.Name=="ReplaceCastSource"))foreach(var m in type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(m=>m.Name=="OnTurnOn"||m.Name=="OnTurnOff"))h.Patch(m,prefix:new HarmonyMethod(typeof(Feedback01448Tests),nameof(Skip)));
        var cls=N<BlueprintCharacterClass>(c,(string)AccessTools.Field(T("PsychicClass"),"ClassId").GetRawConstantValue());
        var caster=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);var plant=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);H("Fact",plant,N<BlueprintFeature>(c,"706e61781d692a042b35941f14bc41c5"));
        Set(plant,"m_Position",new UnityEngine.Vector3(3,0,0));
        var book=(Spellbook)H("Book",caster,cls);var spell=N<BlueprintAbility>(c,"3c17035ec4717674cae2e841a190e757");var a=new AbilityData(spell,caster.Descriptor);Set(a,"m_CachedSpellbook",book);
        var restriction=spell.GetComponents<AbilityTargetHasFact>().Single(r=>r.CheckedFacts.Any(f=>f.AssetGuid.ToString()=="706e61781d692a042b35941f14bc41c5"));
        Check(!restriction.IsTargetRestrictionPassed(caster,new TargetWrapper(plant)),"native plant target restriction rejects ordinary caster");
        var buff=H("Buff",caster,A<BlueprintBuff>(c,"NaturesCommandBuff01447"),null);var pool=N<BlueprintAbilityResource>(c,(string)AccessTools.Field(T("PsychicClass"),"WisdomPoolId").GetRawConstantValue());caster.Stats.Wisdom.BaseValue=24;caster.Resources.Add(pool,true);caster.Resources.Restore(pool,99);int before=caster.Resources.GetResourceAmount(pool);
        // This is the actual AbilityData caller, before a RuleCastSpell and payment exist.
        bool can=a.CanTarget(new TargetWrapper(plant));
        if(!can&&Main.GetType("CallOfTheWild.Wrath.Prototype.NatureTargeting01448")!=null){
            var nt=T("NatureTargeting01448");var old=AccessTools.Field(nt,"Current").GetValue(null);AccessTools.Field(nt,"Current").SetValue(null,a);
            try{File.WriteAllText(Path.Combine(Root(),"work/repair01448/nature-target-debug.json"),new JObject{["canPay"]=(bool)Call(nt,"CanPay",a,caster),["plantAllowed"]=(bool)Call(nt,"PlantAllowed",a,caster,plant),["descriptor"]=a.SpellDescriptor.ToString(),["sameOwner"]=ReferenceEquals(restriction.OwnerBlueprint,spell),["owner"]=restriction.OwnerBlueprint?.name,["restrictions"]=new JArray(spell.TargetRestrictions.Select(r=>new JObject{["type"]=r.GetType().FullName,["passed"]=r.IsTargetRestrictionPassed(caster,new TargetWrapper(plant))}))}.ToString());}finally{AccessTools.Field(nt,"Current").SetValue(null,old);}
        }
        Check(can,"actual native precast AbilityData.CanTarget accepts eligible plant with Nature's Command enabled");
        Check(caster.Resources.GetResourceAmount(pool)==before,"target preview never spends phrenic points");
        var human=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);
        Check(a.CanTarget(new TargetWrapper(human))&&a.CantTargetReason(new TargetWrapper(plant))==a.CantTargetReason(new TargetWrapper(human)),"native target-reason fallback matches another legal target");
        for(int points=0;points<=3;points++){
            caster.Resources.Spend(pool,caster.Resources.GetResourceAmount(pool));caster.Resources.Restore(pool,points);
            Check(a.CanTarget(new TargetWrapper(plant))==(points>=2),"native precast phrenic threshold "+points);
            Check(caster.Resources.GetResourceAmount(pool)==points,"threshold preview is free "+points);
        }
        var wizard=N<BlueprintCharacterClass>(c,"ba34257984f4c41408ce1dc2004e342e");var otherBook=new Spellbook(caster.Descriptor,wizard.Spellbook);Set(a,"m_CachedSpellbook",otherBook);
        Check(!a.CanTarget(new TargetWrapper(plant)),"identical shared spell from a non-Psychic book remains illegal");Set(a,"m_CachedSpellbook",book);
        var item=(Kingmaker.Items.ItemEntity)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Kingmaker.Items.ItemEntityUsable));var itemFact=new Ability(spell,caster.Descriptor);Set(itemFact,"<SourceItem>k__BackingField",item);Set(a,"m_Fact",itemFact);
        Check(ReferenceEquals(a.SourceItem,item)&&!a.CanTarget(new TargetWrapper(plant)),"real item-origin ability fact cannot use the Psychic target exception");Set(a,"m_Fact",null);
        var construct=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);
        H("Fact",construct,N<BlueprintFeature>(c,"fd389783027d63343b4a5634bd81645f"));
        Check(!a.CanTarget(new TargetWrapper(construct)),"same negative target list retains Construct");
        H("Fact",plant,N<BlueprintFeature>(c,"fd389783027d63343b4a5634bd81645f"));
        Check(!a.CanTarget(new TargetWrapper(plant)),"a target with both Plant and Construct retains its independent restriction");
        var constructFact=plant.Facts.List.Single(f=>f.Blueprint.AssetGuid.ToString()=="fd389783027d63343b4a5634bd81645f");plant.Facts.Remove(constructFact);
        Check(a.CanTarget(new TargetWrapper(plant)),"removing only the independent restriction restores legal plant targeting");
        var command=N<BlueprintAbility>(c,"feb70aab86cc17f4bb64432c83737ac2");
        if(command!=null&&command.GetComponent<AbilityVariants>()!=null) {
            var variant=command.GetComponent<AbilityVariants>().Variants.First();Set(variant,"m_Parent",BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(command));
            for(int level=10;level<=11;level++){var u=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),level);var b=(Spellbook)H("Book",u,cls);H("Buff",u,A<BlueprintBuff>(c,"NaturesCommandBuff01447"),null);u.Resources.Add(pool,true);u.Stats.Wisdom.BaseValue=24;u.Resources.Restore(pool,99);var ca=new AbilityData(variant,u.Descriptor);Set(ca,"m_CachedSpellbook",b);Check((bool)Call(T("NatureTargeting01448"),"PlantAllowed",ca,u,plant)==(level>=11),"precast Command exception eligibility at native Psychic class-level boundary "+level);}
        }
        caster.Resources.Spend(pool,caster.Resources.GetResourceAmount(pool)-2);
        var cast=(RuleCastSpell)H("Cast",caster,plant,book,spell,19);var mode=(Buff)buff;var component=mode.Blueprint.GetComponent<NaturesCommand01447>();
        using((IDisposable)H("Scope",mode,component))component.OnEventAboutToTrigger(cast);
        Check(caster.Resources.GetResourceAmount(pool)==0,"actual mode handler pays the final two points");
        h.Patch(AccessTools.Method(typeof(RuleApplySpell),nameof(RuleApplySpell.OnTrigger)),prefix:new HarmonyMethod(typeof(Feedback01448Tests),nameof(Skip)));
        h.Patch(AccessTools.PropertyGetter(typeof(RuleCastSpell),"IsCutscene"),prefix:new HarmonyMethod(typeof(Feedback01448Tests),nameof(No)));caster.Blueprint.IsCheater=false;
        var castEndpoint=AccessTools.Method(typeof(AbilityData),nameof(AbilityData.Cast),new[]{typeof(AbilityExecutionContext)});
        h.Patch(castEndpoint,prefix:new HarmonyMethod(typeof(Feedback01448Tests),nameof(CastEndpoint)));
        castCalls=0;try{cast.OnTrigger(new RulebookEventContext());Check(cast.Success&&castCalls==1&&!cast.Context.IsForced,"native nonforced RuleCastSpell.OnTrigger validates paid last-use plant then reaches the engine cast endpoint");}finally{h.Unpatch(castEndpoint,AccessTools.Method(typeof(Feedback01448Tests),nameof(CastEndpoint)));h.Unpatch(AccessTools.Method(typeof(RuleApplySpell),nameof(RuleApplySpell.OnTrigger)),AccessTools.Method(typeof(Feedback01448Tests),nameof(Skip)));h.Unpatch(AccessTools.PropertyGetter(typeof(RuleCastSpell),"IsCutscene"),AccessTools.Method(typeof(Feedback01448Tests),nameof(No)));}
        Check(!a.CanTarget(new TargetWrapper(plant)),"a later unpaid preview cannot reuse the completed cast's paid exception");
        var part=plant.Get<UnitPartSpellResistance>();Check(!part.IsImmune(cast.Context,false),"paid execution bypasses inherited plant mind immunity after final resource use");
        int extra=part.AddImmunity(SpellImmunityType.SpellDescriptor,0,null,SpellDescriptor.MindAffecting,null);Check(part.IsImmune(cast.Context,false),"additional independent mind-affecting immunity stays authoritative");part.Remove(extra);
        component.OnEventDidTrigger(cast);Check(caster.Resources.GetResourceAmount(pool)==0,"successful native cast is not refunded");
        caster.Resources.Restore(pool,2);mode.Remove();Check(!a.CanTarget(new TargetWrapper(plant)),"removing Nature's Command restores original native target restriction");
        var logical=(RuleCastSpell)H("Cast",caster,plant,book,spell,19);logical.Context.Params.Metamagic=(Metamagic)(1<<29);
        using(logical.Context.GetDataScope(new TargetWrapper(plant)))Check(!logical.Spell.CanTarget(new TargetWrapper(plant)),"Logical Spell without Nature's Command cannot masquerade as a paid plant exception");
        var paid=(Metamagic)(int)AccessTools.Field(T("MagaambyanTelepath01447"),"Paid").GetRawConstantValue();
        Check(((int)paid&((1<<29)|(1<<30)|(1<<28)|(1<<27)))==0,"Nature marker is disjoint from Logical, conversion, Toppling/Piercing and Intensity");
        var saved=new Kingmaker.UnitLogic.Mechanics.MechanicsContext(caster,caster.Descriptor,spell);saved.SetParams(cast.Context.Params.Clone());
        var saveSerializer=Newtonsoft.Json.JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings{ContractResolver=new Kingmaker.EntitySystem.Persistence.JsonUtility.OptInContractResolver(),Converters=Kingmaker.EntitySystem.Persistence.JsonUtility.DefaultJsonSettings.CommonConverters.ToList(),TypeNameHandling=Newtonsoft.Json.TypeNameHandling.None});
        var writer=new StringWriter();saveSerializer.Serialize(writer,saved,typeof(Kingmaker.UnitLogic.Mechanics.MechanicsContext));var restored=saveSerializer.Deserialize<Kingmaker.UnitLogic.Mechanics.MechanicsContext>(new Newtonsoft.Json.JsonTextReader(new StringReader(writer.ToString())));
        Check((restored.Params.Metamagic&paid)!=0&&(restored.Params.Metamagic&(Metamagic)(1<<29))==0,"native save context JSON retains signed paid marker without silently adding Logical");
        saved.Params.Metamagic|=(Metamagic)(1<<29);writer=new StringWriter();saveSerializer.Serialize(writer,saved,typeof(Kingmaker.UnitLogic.Mechanics.MechanicsContext));restored=saveSerializer.Deserialize<Kingmaker.UnitLogic.Mechanics.MechanicsContext>(new Newtonsoft.Json.JsonTextReader(new StringReader(writer.ToString())));Check((restored.Params.Metamagic&paid)!=0&&(restored.Params.Metamagic&(Metamagic)(1<<29))!=0,"native save context preserves actual Logical and Nature flags independently");
        PsychicExpansionTests.Inventory["natureTargeting01448Contracts"]=new JObject{["passed"]=true,["nativeCanTarget"]=true,["nativeTargetReason"]=true,["resourceThresholds"]=4,["separateBookAndItem"]=true,["constructRetained"]=true,["plantPlusConstructRetained"]=true,["languageClassLevels"]=new JArray(10,11),["nativeNonforcedCast"]=true,["lastTwoPoints"]=true,["independentMindImmunity"]=true,["logicalCannotForgePaid"]=true,["paidMarker"]=unchecked((int)paid),["nativeParameterJson"]=true,["boundary"]="Actual AbilityData.CanTarget/CantTargetReason, native fact restrictions, native nonforced RuleCastSpell.OnTrigger and native parameter serializer execute. Scene/cutscene state and terminal AbilityData.Cast are bounded; the production resource handler executes in its actual fact context before the native cast body. This is not a complete Unity combat/save observation."};
    }
    static int castCalls;
    static bool CastEndpoint(ref Kingmaker.Controllers.AbilityExecutionProcess __result){castCalls++;__result=null;return false;}
    static readonly System.Collections.Generic.List<string> visualCalls=new System.Collections.Generic.List<string>();
    static bool SpawnEndpoint(AbilitySpawnFx __instance,AbilityExecutionContext context,TargetWrapper selectedTarget){visualCalls.Add(__instance.PrefabLink.AssetId+"/"+__instance.Anchor);return false;}
    static UnityEngine.GameObject LoadEndpoint(WeakResourceLink<UnityEngine.GameObject> link,bool unused,bool async){visualCalls.Add(link.AssetId);return null;}
    static System.Collections.Generic.IEnumerable<CodeInstruction> AreaVisualBoundary(System.Collections.Generic.IEnumerable<CodeInstruction> input)
    {
        var bounded=(System.Collections.Generic.IEnumerable<CodeInstruction>)Call(Helper,"FxBoundary",input);int replacements=0;
        foreach(var instruction in bounded){
            var method=instruction.operand as MethodInfo;
            if(method?.DeclaringType==typeof(WeakResourceLink<UnityEngine.GameObject>)&&method.Name=="Load"){
                instruction.opcode=System.Reflection.Emit.OpCodes.Call;instruction.operand=AccessTools.Method(typeof(Feedback01448Tests),nameof(LoadEndpoint));replacements++;
            }
            yield return instruction;
        }
        if(replacements!=1)throw new Exception("Expected exactly one native area-view prefab Load endpoint, got "+replacements);
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void AreaVisualNative(Kingmaker.View.MapObjects.AreaEffectView view)
    {
        System.Collections.Generic.IEnumerable<CodeInstruction> Transpiler(System.Collections.Generic.IEnumerable<CodeInstruction> code)=>AreaVisualBoundary(code);
        _=(Func<System.Collections.Generic.IEnumerable<CodeInstruction>,System.Collections.Generic.IEnumerable<CodeInstruction>>)Transpiler;
        throw new NotImplementedException("Native area-view reverse copy must replace this stub");
    }
    static void Visuals(BlueprintsCache parent,Harmony h)
    {
        // The full historical suite already published Urban Skald. Use its
        // own isolated native cache, not an inherited private graph without
        // the registrar's per-cache identity receipt.
        var urban=(BlueprintsCache)Call(typeof(UrbanSkald01443Tests),"Fresh",parent);var previous=ResourcesLibrary.BlueprintsCache;
        AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,urban);
        try{
            Call(T("UrbanSkald01443"),"Register",urban);
            Check(A<BlueprintBuff>(urban,"UrbanSkaldMockeryBuff01443").FxOnStart?.AssetId==N<BlueprintBuff>(urban,"4b1f07a71a982824988d7f48cd49f3f8").FxOnStart?.AssetId&&!string.IsNullOrEmpty(A<BlueprintBuff>(urban,"UrbanSkaldMockeryBuff01443").FxOnStart?.AssetId),"Urban Infuriating Mockery restores original CotW/native Hideous Laughter target visual");
        }finally{AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,previous);}
        var c=(BlueprintsCache)H("Fresh",parent);if(c.Load(Id("PhrenicAmplificationSelection"))==null)Call(T("PsychicAmplifications"),"Register",c);
        foreach(string module in new[]{"CourtBard01445","Phantasm01447","TerrorWeaver01444"})Call(T(module),"Register",c);
        var cls=N<BlueprintCharacterClass>(c,"ba34257984f4c41408ce1dc2004e342e");var caster=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);var target=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);var book=(Spellbook)H("Book",caster,cls);
        var endpoint=AccessTools.Method(typeof(AbilitySpawnFx),nameof(AbilitySpawnFx.Spawn));var phase=AccessTools.Method(typeof(Kingmaker.Controllers.AbilityExecutionProcess),"SpawnFxs");
        h.Patch(endpoint,prefix:new HarmonyMethod(typeof(Feedback01448Tests),nameof(SpawnEndpoint)));
        try {
            var terror=A<BlueprintAbility>(c,"PhantasmTerrorTouch01447");var cast=(RuleCastSpell)H("Cast",caster,target,book,terror,19);cast.Context.FxSpawners=cast.Context.AbilityBlueprint.GetComponents<AbilitySpawnFx>().ToList();visualCalls.Clear();
            phase.Invoke(null,new object[]{cast.Context,AbilitySpawnFxTime.OnStart,new TargetWrapper(target)});Check(visualCalls.Count==0,"Terror impact is not emitted during pre-touch start");
            phase.Invoke(null,new object[]{cast.Context,AbilitySpawnFxTime.OnApplyEffect,new TargetWrapper(target)});Check(visualCalls.SequenceEqual(new[]{"49a8069c238b1a8429f2123654d4f45b/SelectedTarget"}),"native FX phase dispatcher emits original Terror impact on selected target exactly once");
            foreach(string action in new[]{"Move","Swift"})foreach(string pool in new[]{"Wisdom","Charisma"}) {
                var ability=A<BlueprintAbility>(c,"Terror"+action+pool+"Ability01444");var evt=(RuleCastSpell)H("Cast",caster,target,book,ability,19);evt.Context.FxSpawners=evt.Context.AbilityBlueprint.GetComponents<AbilitySpawnFx>().ToList();visualCalls.Clear();
                phase.Invoke(null,new object[]{evt.Context,AbilitySpawnFxTime.OnStart,new TargetWrapper(target)});phase.Invoke(null,new object[]{evt.Context,AbilitySpawnFxTime.OnApplyEffect,new TargetWrapper(target)});
                Check(visualCalls.Count==2&&visualCalls.Contains("6ed99085fe122c04daec2fc2d9c03334/SelectedTarget")&&visualCalls.Contains("d80d51c0a08f35140b10dd1526e540c4/Caster"),"native Terror Weaver FX dispatcher preserves donor start/impact phases "+action+pool);
            }
        }finally{h.Unpatch(endpoint,AccessTools.Method(typeof(Feedback01448Tests),nameof(SpawnEndpoint)));}
        var viewType=typeof(Kingmaker.View.MapObjects.AreaEffectView);var tactical=AccessTools.PropertyGetter(viewType,"TacticalCombat");h.Patch(tactical,prefix:new HarmonyMethod(typeof(SpellBatch01447Tests),"False"));
        h.CreateReversePatcher(AccessTools.Method(viewType,"SpawnFxs"),new HarmonyMethod(typeof(Feedback01448Tests),nameof(AreaVisualNative))).Patch();
        var rows=new JArray();
        try {
            foreach(string token in new[]{"CourtBardSatireArea01445","CourtBardMockeryArea01445","CourtBardGloriousEpicArea01445","CourtBardScandalArea01445","PhantasmAuraArea01447"}) {
                var area=A<BlueprintAbilityAreaEffect>(c,token);Check(area.Fx!=null&&!string.IsNullOrEmpty(area.Fx.AssetId),"visible aura has nonempty original/native prefab "+token);
                var view=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(viewType);var reference=BlueprintReferenceBase.CreateTyped<BlueprintAbilityAreaEffectReference>(area);Set(view,"m_Blueprint",reference);visualCalls.Clear();
                var savedFx=area.Fx;area.Fx=null;bool rejected=false;
                try{AreaVisualNative((Kingmaker.View.MapObjects.AreaEffectView)view);}catch(NullReferenceException){rejected=true;}finally{area.Fx=savedFx;}
                Check(rejected,"native area-view null-prefab negative control rejects "+token);visualCalls.Clear();
                AreaVisualNative((Kingmaker.View.MapObjects.AreaEffectView)view);Check(visualCalls.SequenceEqual(new[]{area.Fx.AssetId}),"native area-view SpawnFxs reaches resource loader with exact intended prefab "+token);
                rows.Add(new JObject{["token"]=token,["areaFX"]=area.Fx.AssetId,["nativeViewResourceDispatch"]=true});
            }
        }finally{h.Unpatch(tactical,AccessTools.Method(typeof(SpellBatch01447Tests),"False"));}
        foreach(string name in new[]{"Satire","Mockery","GloriousEpic","Scandal"}) {
            var host=A<BlueprintBuff>(c,"CourtBard"+name+"Host01445");var effect=A<BlueprintBuff>(c,"CourtBard"+name+"Effect01445");
            Check(!string.IsNullOrEmpty(host.FxOnStart?.AssetId)&&!string.IsNullOrEmpty(effect.FxOnStart?.AssetId),"Court caster and affected target each retain a native-lifecycle visual "+name);
        }
        PsychicExpansionTests.Inventory["visualFeedback01448Contracts"]=new JObject{["passed"]=true,["areaViews"]=rows,["nullFXNegativeControls"]=5,["terrorImpact"]=true,["terrorWeaverFourVariants"]=true,["urbanMockeryTarget"]=true,["boundary"]="Actual native ability FX phase dispatcher and reverse-copied native AreaEffectView.SpawnFxs execute. Its spawner list is populated from the actual published ability components exactly as the native coroutine does. Spawn is a controlled endpoint. The area-view reverse copy substitutes exactly one nonempty prefab Load call with a recording endpoint, preserving the native prefab dereference and branches; five null-prefab negative controls execute. Separate actual-bundle GameObject resolution proves the links. Rendered particles, audio, animation and visual cleanup across full game save/load require scene observation."};
    }
    static bool Skip()=>false;
    static bool No(ref bool __result){__result=false;return false;}
    static bool SpriteEndpoint(object __instance,object __0){Set(__instance,"Value",__0);return false;}
    internal static int Run(BlueprintsCache parent,Harmony h)
    {
        checks=0;var which=Environment.GetEnvironmentVariable("COTW_01448_CASE");if(which==null||which=="bloodline"||which=="providerfull"){if(Main.GetType("CallOfTheWild.Wrath.Prototype.TomeClaws01449")!=null)checks+=Feedback01449Tests.Bloodline(parent,h,true);else ProviderWrapper(parent,h);}if(which==null||which=="nature")Nature(parent,h);if(which==null||which=="visual")Visuals(parent,h);
        PsychicExpansionTests.Inventory["feedback01448Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["actualProviderConfigurator"]=which==null||which=="bloodline",["nativePrecastTarget"]=which==null||which=="nature",["nativeVisualDispatch"]=which==null||which=="visual"};return checks;
    }
}
