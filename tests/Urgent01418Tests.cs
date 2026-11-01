using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using CallOfTheWild.Wrath.Prototype;

internal static class Urgent01418Tests
{
    static int n,pools,refs,prerequisiteChecks;
    static LevelUpController controller;
    static readonly Kingmaker.Blueprints.Root.LocalizedTexts texts=new Kingmaker.Blueprints.Root.LocalizedTexts { Stats=Empty<Kingmaker.Blueprints.Root.Strings.StatsStrings>() };
    static T Empty<T>() where T:class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Set(object o,string f,object v) => (AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).SetValue(o,v);
    static object Get(object o,string f) => AccessTools.Field(o.GetType(),f).GetValue(o);
    static void Check(bool ok,string message){if(!ok)throw new Exception("Urgent01418: "+message);n++;}
    static Type Module(string name) => typeof(SpontaneousMetafocusPrerequisite).Assembly.GetType("CallOfTheWild.Wrath.Prototype."+name,true);
    static bool LevelUp(ref LevelUpController __result){__result=controller;return false;}
    static bool Skip()=>false;
    static bool No(ref bool __result){__result=false;return false;}
    static bool PoolIgnore(BlueprintFeatureSelection __instance,ref bool __result){__result=__instance.IgnorePrerequisites;return false;}
    static bool Texts(ref Kingmaker.Blueprints.Root.LocalizedTexts __result){__result=texts;return false;}
    static bool Labels(BlueprintParametrizedFeature __instance,IEnumerable<BlueprintScriptableObject> __0,ref IEnumerable<FeatureUIData> __result){__result=__0.Where(b=>b!=null).Select(b=>new FeatureUIData(__instance,(FeatureParam)b,b.name??"","",null,b.name??""));return false;}
    static bool SpellLabels(BlueprintParametrizedFeature __instance,UnitDescriptor __0,ref IEnumerable<FeatureUIData> __result){__result=__instance.ExtractSpellsFromSpellbooks(__0).Select(b=>new FeatureUIData(__instance,(FeatureParam)b,b.name??"","",null,b.name??""));return false;}
    static bool StatName(Kingmaker.EntitySystem.Stats.StatType __0,ref string __result){__result=__0.ToString();return false;}
    static bool WeaponName(WeaponCategory __0,ref string __result){__result=__0.ToString();return false;}
    static Feature Fact(UnitEntityData u,BlueprintFeature bp)=>(Feature)AccessTools.Method(typeof(Linnorm01416Tests),"Fact").Invoke(null,new object[]{u,bp});
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection s,string field)=>(BlueprintFeatureReference[])Get(s,field)??Array.Empty<BlueprintFeatureReference>();
    static BlueprintFeatureReference Ref(BlueprintFeature feature){var r=BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(feature);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,feature,null);return r;}
    internal static int Run(BlueprintsCache cache,string evidence)
    {
        n=pools=refs=prerequisiteChecks=0;var h=new Harmony("CotW01418.OfflineContracts");
        Provider01419Tests.PatchLabels(h);
        h.Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),prefix:new HarmonyMethod(typeof(Urgent01418Tests),nameof(Skip)));
        // Scene build-mode settings: prerequisites remain enabled.
        h.Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites),"Ignore"),prefix:new HarmonyMethod(typeof(Urgent01418Tests),nameof(No)));
        // No Trickster prerequisite bypass in these controlled player cases.
        h.Patch(AccessTools.Method(typeof(BlueprintFeatureSelection),"IsIgnorePrerequisites"),prefix:new HarmonyMethod(typeof(Urgent01418Tests),nameof(PoolIgnore)));
        h.Patch(AccessTools.PropertyGetter(typeof(Game),"LevelUpController"),prefix:new HarmonyMethod(typeof(Urgent01418Tests),nameof(LevelUp)));
        // Preserve native catalogue dereferencing/Items/selection checks. Only
        // localized label/icon composition needs a Unity scene endpoint.
        h.Patch(AccessTools.Method(typeof(BlueprintParametrizedFeature),"ExtractItemsFromBlueprints"),prefix:new HarmonyMethod(typeof(Urgent01418Tests),nameof(Labels)));
        h.Patch(AccessTools.Method(typeof(BlueprintParametrizedFeature),"ExtractItemsFromSpellbooks"),prefix:new HarmonyMethod(typeof(Urgent01418Tests),nameof(SpellLabels)));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Blueprints.Root.LocalizedTexts),"Instance"),prefix:new HarmonyMethod(typeof(Urgent01418Tests),nameof(Texts)));
        h.Patch(AccessTools.Method(typeof(Kingmaker.Blueprints.Root.Strings.StatsStrings),"GetText",new[]{typeof(Kingmaker.EntitySystem.Stats.StatType)}),prefix:new HarmonyMethod(typeof(Urgent01418Tests),nameof(StatName)));
        h.Patch(AccessTools.Method(typeof(Kingmaker.Blueprints.Root.Strings.StatsStrings),"GetText",new[]{typeof(WeaponCategory)}),prefix:new HarmonyMethod(typeof(Urgent01418Tests),nameof(WeaponName)));
        h.CreateClassProcessor(Module("SpontaneousMetafocusSelectionPatch")).Patch();
        try {
            var meta=Psychic034Tests.Asset<BlueprintParametrizedFeature>(cache,"SpontaneousMetafocusFeat");
            var template=(BlueprintParametrizedFeature)cache.Load(BlueprintGuid.Parse("e69a85f633ae8ca4398abeb6fa11b1fe"));var original=template.BlueprintParameterVariants;
            Check(meta.BlueprintParameterVariants!=null,"full parameter catalogue initialized");Check(!ReferenceEquals(meta.BlueprintParameterVariants,original),"native array not shared");Check(meta.GetFullSelectionItems().Any(),"native full Items enumerate");
            var u=(UnitEntityData)AccessTools.Method(typeof(SpellAndSword01417Tests),"Unit").Invoke(null,new object[]{cache,"01418-preview"});Fact(u,Psychic034Tests.Asset<BlueprintFeature>(cache,"TopplingSpellFeat"));
            var book=(Spellbook)AccessTools.Method(typeof(SpellAndSword01417Tests),"Book").Invoke(null,new object[]{u,true});
            var known=new BlueprintAbility{name="01418LateProviderSpell",AssetGuid=BlueprintGuid.Parse("9a0cf70e868e41359bfc225ea6ca7418"),Type=AbilityType.Spell,ComponentsArray=Array.Empty<BlueprintComponent>()};known.OnEnable();cache.AddCachedBlueprint(known.AssetGuid,known);
            AccessTools.Method(typeof(SpellAndSword01417Tests),"Learn").Invoke(null,new object[]{u,book,known,1});var prereq=meta.GetComponent<SpontaneousMetafocusPrerequisite>();
            Check(prereq.CheckRaw(null,u.Descriptor,null),"late-provider spell qualifies");var choices=meta.ExtractSelectionItems(u.Descriptor,u.Descriptor).ToArray();Check(choices.Length==1&&choices[0].Param.Blueprint==known,"only known spontaneous spell listed");
            var state=Empty<LevelUpState>();Set(state,"m_Unit",u);Set(state,"Selections",new List<FeatureSelectionState>());
            var basic=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("247a4068296e8be42890143f451b4b45"));var selection=Empty<FeatureSelectionState>();Set(selection,"Selection",basic);
            Check(meta.CanSelect(u.Descriptor,state,selection,choices[0]),"native parameter CanSelect permits known spell");Check(!prereq.CanBeSelected(meta,u.Descriptor,(FeatureParam)new BlueprintAbility()),"unknown spell rejected");Check(!prereq.CanBeSelected(meta,null,null),"null parameter safe");
            Check(FeatureSelectionExtensions.CanSelectAny(meta,u.Descriptor,state,selection),"native nested availability");Check(basic.CanSelect(u.Descriptor,state,selection,meta),"ordinary feat pool selects parent");
            controller=Empty<LevelUpController>();controller.State=state;controller.Unit=u;Set(controller,"Preview",u);
            var ownRoots=basic.Items.Select(i=>i.Feature).Where(f=>f?.name?.StartsWith("CotWWrath")==true).Distinct().ToArray();
            Check(ownRoots.Length>=10,"all registered CotW ordinary-feat roots enumerated");
            PsychicExpansionTests.Inventory["urgent01418PrerequisiteRoots"]=ownRoots.Length;
            foreach(int level in new[]{1,2,8,20}) {
                Set(u.Progression,"m_CharacterLevel",(int?)level);Set(state,"NextCharacterLevel",level);
                foreach(var feat in ownRoots) {
                    var token=feat.name;
                    foreach(var p in feat.GetComponents<Prerequisite>()){p.CheckRaw(selection,u.Descriptor,state);Check(p!=null,token+" prerequisite evaluation completed");if(p.GetType().Assembly==typeof(SpontaneousMetafocusPrerequisite).Assembly)Check(p.GetUIText(u.Descriptor)!=null,token+" custom prerequisite text");prerequisiteChecks++;}
                    var view=Empty<FeatureSelectionViewState>();view.RefreshCanSelectState(selection,basic,feat);Check(view.CanSelect==basic.CanSelect(u.Descriptor,state,selection,feat),token+" native viewstate agreement");
                }
                Check(Psychic034Tests.Asset<BlueprintFeature>(cache,"FeyFoundlingFeat").GetComponent<FeyFoundlingFirstLevel>().CheckRaw(selection,u.Descriptor,state)==(level==1),"first-level eligibility unchanged");
            }
            var focus=Fact(u,(BlueprintFeature)cache.Load(BlueprintGuid.Parse("1e1f627d26ad36f43bbd26cc2bf8ac7e")));Set(focus,"Param",null);Check(!Psychic034Tests.Asset<BlueprintFeature>(cache,"SwordplayStyleFeat").GetComponent<SwordplayFocusPrerequisite>().CheckRaw(null,u.Descriptor,null),"unassigned weapon-focus preview safe");
            var variants=meta.BlueprintParameterVariants;prereq.CheckRaw(selection,u.Descriptor,state);meta.ExtractSelectionItems(u.Descriptor,u.Descriptor).ToArray();Check(ReferenceEquals(variants,meta.BlueprintParameterVariants),"unchanged spells do not rewrite catalogue");
            Set(Fact(u,meta),"Param",(FeatureParam)known);Check(!meta.CanSelect(u.Descriptor,state,selection,choices[0]),"same spell cannot repeat");Check(ReferenceEquals(template.BlueprintParameterVariants,original),"native catalogue preserved");
            foreach(var type in new[]{typeof(SpontaneousMetafocusPrerequisite),typeof(FeyFoundlingFirstLevel),typeof(SwordplayFocusPrerequisite)}){var p=(Prerequisite)Activator.CreateInstance(type);Check(!p.CheckRaw(null,null,null),"null preview "+type.Name);Check(p.GetUIText(null)!=null,"null preview text "+type.Name);}
            AllPools(cache,JObject.Parse(System.IO.File.ReadAllText(evidence)));
        } finally {h.UnpatchAll(h.Id);controller=null;}
        PsychicExpansionTests.Inventory["urgent01418Assertions"]=n;PsychicExpansionTests.Inventory["urgent01418SelectionPools"]=pools;PsychicExpansionTests.Inventory["urgent01418SelectionReferences"]=refs;PsychicExpansionTests.Inventory["urgent01418PrerequisiteChecks"]=prerequisiteChecks;
        Console.WriteLine("PASS: "+n+" urgent release contracts; "+pools+" native pools preserved; "+refs+" references resolved; "+prerequisiteChecks+" prerequisite/text checks; real Items/nested/upgrade preview.");return n;
    }
    static void AllPools(BlueprintsCache cache,JObject data)
    {
        var originals=new Dictionary<BlueprintGuid,SimpleBlueprint>();var list=new List<BlueprintFeatureSelection>();var stubs=new Dictionary<BlueprintGuid,BlueprintFeature>();
        var shells=new Dictionary<BlueprintGuid,BlueprintFeatureSelection>();
        BlueprintFeature Resolve(BlueprintGuid id){var f=cache.Load(id)as BlueprintFeature;if(f!=null)return f;if(!stubs.TryGetValue(id,out f))stubs[id]=f=new BlueprintFeature{name="NativeMetadata_"+id,AssetGuid=id,ComponentsArray=Array.Empty<BlueprintComponent>()};Check(data["nativeBlueprintTypes"][id.ToString()]!=null,"native identity "+id);return f;}
        try {
            // Publish all audited selection shells before resolving their links.
            // Ref caches the object, so a one-pass rebuild can retain an older
            // partially hydrated nested selection even after its cache entry
            // is replaced later in this loop.
            foreach(var row in data["allNativeSelectionPools"]){var bp=row["blueprint"];var id=BlueprintGuid.Parse((string)bp["AssetId"]);originals[id]=cache.Load(id);var p=new BlueprintFeatureSelection{name=(string)row["name"],AssetGuid=id,Group=(FeatureGroup)Enum.Parse(typeof(FeatureGroup),(string)bp["Data"]["Group"]),Group2=(FeatureGroup)Enum.Parse(typeof(FeatureGroup),(string)bp["Data"]["Group2"])};
                shells.Add(id,p);cache.AddCachedBlueprint(id,p);list.Add(p);}
            foreach(var row in data["allNativeSelectionPools"]){var bp=row["blueprint"];var p=shells[BlueprintGuid.Parse((string)bp["AssetId"])];
                foreach(string field in new[]{"m_Features","m_AllFeatures"})Set(p,field,((JArray)bp["Data"][field]??new JArray()).Select(t=>Ref(Resolve(BlueprintGuid.Parse(((string)t).Replace("!bp_",""))))).ToArray());}
            foreach(var p in list)foreach(string field in new[]{"m_Features","m_AllFeatures"})foreach(var reference in Refs(p,field))
                if(shells.TryGetValue(reference.Guid,out var nested))Check(ReferenceEquals(reference.Get(),nested)&&ReferenceEquals(cache.Load(reference.Guid),nested),"nested native selection uses current shell "+p.name+"/"+field+"/"+nested.name);
            var before=list.ToDictionary(p=>p,p=>new[]{Refs(p,"m_Features"),Refs(p,"m_AllFeatures")});
            foreach(string module in new[]{"LinnormFeats","SpellAndSwordFeats","ProviderResourceFeats","ImprovedTwoWeaponFeint","ExtraInvestigatorTalent","ReapingStalker","RogueFeatTalent","TerrifyingHowl","Inquisitions01426","MonkKiPowers01427","Inquisitions01428","PurifyingFlames01428","WizardSubschools01428","EnhancementSchool01429","FlameJet01429","SpiritualGuardian01433","KineticInvocation01433","KineticUtility01433","SuperstitionRage01434","AntimagicRage01434","WizardDiscoveries01435","BardMasterpieces01435"})AccessTools.Method(Module(module),"Register").Invoke(null,new object[]{cache});
            var once=list.ToDictionary(p=>p,p=>new[]{Refs(p,"m_Features").Select(v=>v.Guid).ToArray(),Refs(p,"m_AllFeatures").Select(v=>v.Guid).ToArray()});
            foreach(string module in new[]{"LinnormFeats","SpellAndSwordFeats","ProviderResourceFeats","ImprovedTwoWeaponFeint","ExtraInvestigatorTalent","ReapingStalker","RogueFeatTalent","TerrifyingHowl","Inquisitions01426","MonkKiPowers01427","Inquisitions01428","PurifyingFlames01428","WizardSubschools01428","EnhancementSchool01429","FlameJet01429","SpiritualGuardian01433","KineticInvocation01433","KineticUtility01433","SuperstitionRage01434","AntimagicRage01434","WizardDiscoveries01435","BardMasterpieces01435"})AccessTools.Method(Module(module),"Register").Invoke(null,new object[]{cache});
            foreach(var p in list){pools++;for(int i=0;i<2;i++){string field=i==0?"m_Features":"m_AllFeatures";var a=Refs(p,field);var old=before[p][i];Check(a.Take(old.Length).Select(v=>v.Guid).SequenceEqual(old.Select(v=>v.Guid)),"original entries/order "+p.name+"/"+field);Check(a.Select(v=>v.Guid).SequenceEqual(once[p][i]),"repeat registration "+p.name+"/"+field);foreach(var v in a){Check(v!=null&&v.Get()!=null,"resolved reference "+p.name);refs++;}Check(a.Skip(old.Length).Select(v=>v.Guid).Distinct().Count()==a.Length-old.Length,"no appended duplicates "+p.name);}}
            // Record semantic coverage independently of cache-dependent raw
            // assertion counts, including every original and appended identity.
            PsychicExpansionTests.Inventory["urgent01432PoolCoverage"] = new JArray(list.Select(p => new JObject {
                ["id"] = p.AssetGuid.ToString(), ["name"] = p.name,
                ["fields"] = new JArray(new[] { "m_Features", "m_AllFeatures" }.Select((field, i) => new JObject {
                    ["field"] = field,
                    ["original"] = new JArray(before[p][i].Select(r => r.Guid.ToString())),
                    ["registered"] = new JArray(Refs(p, field).Select(r => r.Guid.ToString()))
                }))
            }));
        }finally{foreach(var pair in originals){cache.RemoveCachedBlueprint(pair.Key);if(pair.Value!=null)cache.AddCachedBlueprint(pair.Key,pair.Value);}}
    }
}
