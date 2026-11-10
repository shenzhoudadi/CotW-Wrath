using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection.Emit;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Items;
using Kingmaker.Utility;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.ElementsSystem;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static readonly Assembly Main=typeof(MindbladeManifest01454).Assembly;
    static Type T(string name)=>Main.GetType("CallOfTheWild.Wrath.Prototype."+name,true);
    static object Call(Type type,string name,params object[] args)=>AccessTools.Method(type,name).Invoke(null,args);
    static object H(string name,params object[] args)=>Call(typeof(Arcane01446Tests),name,args);
    static void Set(object obj,string field,object value)=>(AccessTools.Field(obj.GetType(),field)??AccessTools.Field(obj.GetType(),"<"+field+">k__BackingField")).SetValue(obj,value);
    static BlueprintGuid Id(string token)=>(BlueprintGuid)Call(T("PsychicAssets"),"Id",token);
    static TAsset A<TAsset>(BlueprintsCache c,string token)where TAsset:SimpleBlueprint=>(TAsset)c.Load(Id(token));
    static TAsset N<TAsset>(BlueprintsCache c,string guid)where TAsset:SimpleBlueprint=>(TAsset)c.Load(BlueprintGuid.Parse(guid));
    static int checks;
    static readonly List<string> Coverage=new List<string>();
    static readonly List<UnitEntityData> Actors=new List<UnitEntityData>();
    static void Check(bool ok,string why){if(!ok)throw new Exception("Magus01454: "+why);checks++;Coverage.Add(why);}
    static string Root()=>TestPaths.EvidenceRoot;
    static SimpleBlueprint[] Loaded(BlueprintsCache c)=>(SimpleBlueprint[])H("Loaded",c);
    static BlueprintsCache active;
    static Dictionary<string,string> paths;
    static readonly Dictionary<BlueprintGuid,BlueprintScriptableObject> native=new Dictionary<BlueprintGuid,BlueprintScriptableObject>();
    static readonly HashSet<BlueprintGuid> loading=new HashSet<BlueprintGuid>();
    static ZipArchive archive;
    static readonly Kingmaker.Blueprints.Area.BlueprintArea EquipmentArea=new Kingmaker.Blueprints.Area.BlueprintArea{AssetGuid=BlueprintGuid.Parse("d065985cc2a04e82b8f55c7494455454"),name="054EquipmentSceneInput"};
    static bool Area(ref Kingmaker.Blueprints.Area.BlueprintArea __result){__result=EquipmentArea;return false;}
    static void NoEnchantmentFx(Kingmaker.Blueprints.Items.Ecnchantments.ItemEnchantment __instance)=>Set(__instance,"PreventFxSpawn",true);
    static void ItemIdentity(ItemEntity __result)
    {
        if(__result==null||__result.Proxy!=null)return;
        var proxy=FormatterServices.GetUninitializedObject(AccessTools.Inner(typeof(Kingmaker.EntitySystem.EntityService),"Proxy"));Set(proxy,"Entity",__result);Set(__result,"Proxy",proxy);
    }
    static string UiText(Kingmaker.Localization.LocalizedString value)
    {
        var key=value?.Key??"";var strings=(Dictionary<string,string>)AccessTools.Field(T("PsychicAssets"),"Strings").GetValue(null);return strings.TryGetValue(key,out var text)?text:Kingmaker.Localization.LocalizationManager.CurrentPack?.GetText(key,false)??key;
    }
    static string UiName(BlueprintScriptableObject bp)=>LocaleExport060?EnglishName060(bp):bp.name;
    static bool NativeUiName(Kingmaker.Blueprints.Facts.BlueprintUnitFact __instance,ref string __result){__result=UiName(__instance);return false;}
    static bool NativeUiDescription(Kingmaker.Blueprints.Facts.BlueprintUnitFact __instance,ref string __result){__result=UiDescription(__instance);return false;}
    static string UiDescription(BlueprintScriptableObject bp)
    {var field=AccessTools.Field(bp.GetType(),"m_Description");return field?.GetValue(bp) is Kingmaker.Localization.LocalizedString value?UiText(value):bp.name;}
    static IEnumerable<CodeInstruction> Labels(IEnumerable<CodeInstruction> instructions)
    {
        foreach(var i in instructions){
            if(i.operand is MethodInfo method){
                string replacement=null;
                if(method.DeclaringType==typeof(Kingmaker.Localization.LocalizedString)&&method.Name=="op_Implicit")replacement=nameof(UiText);
                else if(method.ReturnType==typeof(string)&&typeof(BlueprintScriptableObject).IsAssignableFrom(method.DeclaringType)){
                    if(method.Name=="get_Name")replacement=nameof(UiName);
                    if(method.Name=="get_Description")replacement=nameof(UiDescription);
                }
                if(replacement!=null){i.opcode=OpCodes.Call;i.operand=AccessTools.Method(typeof(MagusBatch01454Tests),replacement);}
            }
            yield return i;
        }
    }
    static bool Playing()=>true;
    static IEnumerable<CodeInstruction> ItemPlaying(IEnumerable<CodeInstruction> instructions)
    {
        int replaced=0;foreach(var i in instructions){if(i.operand is MethodInfo method&&method.DeclaringType==typeof(UnityEngine.Application)&&method.Name=="get_isPlaying"){i.opcode=OpCodes.Call;i.operand=AccessTools.Method(typeof(MagusBatch01454Tests),nameof(Playing));replaced++;}yield return i;}
        if(replaced!=1)throw new Exception("054 native item application-state endpoint must occur exactly once.");
    }
    static bool NativeLoad(BlueprintsCache __instance,BlueprintGuid __0,ref SimpleBlueprint __result)
    {
        if(__instance!=active||!paths.TryGetValue(__0.ToString(),out var path))return true;
        if(native.TryGetValue(__0,out var cached)){__result=cached;return false;}
        if(!loading.Add(__0))throw new Exception("Unexpected recursive native fixture load: "+path);
        try{
            JObject input;using(var reader=new StreamReader(archive.GetEntry(path).Open()))input=JObject.Parse(reader.ReadToEnd());var data=(JObject)input["Data"];
            var bp=(BlueprintScriptableObject)PsychicExpansionTests.Read(data,typeof(BlueprintScriptableObject));bp.AssetGuid=__0;bp.name=Path.GetFileNameWithoutExtension(path);
            // Native BlueprintReference JSON conversion turns serialized null
            // into an empty typed reference. Preserve that input convention;
            // e.g. a real buckler has no weapon but its getter dereferences the
            // reference object. The older selective reader returns CLR null.
            for(var owner=bp.GetType();owner!=null;owner=owner.BaseType)foreach(var field in owner.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
                if(!field.IsInitOnly&&!field.FieldType.IsAbstract&&typeof(BlueprintReferenceBase).IsAssignableFrom(field.FieldType)&&data[field.Name]?.Type==JTokenType.Null&&field.GetValue(bp)==null)field.SetValue(bp,Activator.CreateInstance(field.FieldType));
            bp.ComponentsArray=((JArray)data["Components"]??new JArray()).Select(c=>(BlueprintComponent)PsychicExpansionTests.Read(c,typeof(BlueprintComponent))).ToArray();
            native.Add(__0,bp);active.AddCachedBlueprint(__0,bp);
            foreach(var component in bp.ComponentsArray)component.OwnerBlueprint=bp;
            bp.OnEnable();__result=bp;return false;
        }catch(Exception e){throw new Exception("Actual native fixture load failed: "+path,e);}finally{loading.Remove(__0);}
    }
    static BlueprintsCache Fresh(BlueprintsCache parent)
    {
        active=null;var c=(BlueprintsCache)H("Fresh",parent);native.Clear();loading.Clear();active=c;return c;
    }
    static string Definition(BlueprintScriptableObject bp)=>(string)Call(T("MagusBatch01454"),"Definition",bp);
    static string[] Own(BlueprintsCache c)=>Loaded(c).Where(b=>b.name?.StartsWith("CotWWrathPsychic")==true&&b.name.Contains("01454")).Select(b=>b.AssetGuid.ToString()).OrderBy(s=>s).ToArray();
    static void Register(BlueprintsCache c,string module)=>Call(T(module),"Register",c);
    static void Structural(BlueprintsCache parent)
    {
        foreach(var name in new[]{"Mindblade01454","Skirnir01454"}){
            var c=Fresh(parent);var cls=N<BlueprintCharacterClass>(c,"45a4607686d96a1498891b3286121780");var originalBook=cls.Spellbook;var day=originalBook.SpellsPerDay.Levels.Select(l=>l.Count.ToArray()).ToArray();var oldArchetypes=cls.Archetypes.ToArray();
            var defaults=N<Kingmaker.Blueprints.Items.Weapons.BlueprintCategoryDefaults>(c,"567dc59213fd9664c8cb291643439714");foreach(var entry in defaults.Entries.Where(e=>e.DefaultWeapon!=null)){var warmed=entry.DefaultWeapon.Enchantments;if(entry.DefaultWeapon.SecondWeapon!=null)warmed=entry.DefaultWeapon.SecondWeapon.Enchantments;}
            Register(c,name);var token=(string)AccessTools.Field(T(name),"RootToken").GetRawConstantValue();var root=A<BlueprintArchetype>(c,token);Check(root!=null&&root.GetParentClass()==cls,"real native Magus archetype parent "+name);Check(cls.Archetypes.Count(a=>a==root)==1&&oldArchetypes.All(a=>cls.Archetypes.Contains(a)),"native original archetypes retained "+name);
            var own=Own(c);Check(own.Length>30,"complete staged module resources "+name);var shapes=own.Select(s=>Definition((BlueprintScriptableObject)c.Load(BlueprintGuid.Parse(s)))).ToArray();Register(c,name);Register(c,name);Check(own.SequenceEqual(Own(c))&&shapes.SequenceEqual(own.Select(s=>Definition((BlueprintScriptableObject)c.Load(BlueprintGuid.Parse(s))))),"reentry keeps exact own graph/identity "+name);
            Check(cls.Spellbook==originalBook&&day.SelectMany(x=>x).SequenceEqual(originalBook.SpellsPerDay.Levels.SelectMany(l=>l.Count)),"native base book and all spell slots unchanged "+name);
            Check(root.RemoveFeatures.All(e=>cls.Progression.LevelEntries.Where(x=>x.Level==e.Level).SelectMany(x=>x.Features).Any(x=>e.Features.Contains(x))),"removed native features exist at original levels "+name);
            if(name=="Mindblade01454"){
                var book=root.ReplaceSpellbook;Check(book.Spontaneous&&!book.IsArcane&&book.CastingAttribute==Kingmaker.EntitySystem.Stats.StatType.Intelligence&&book.GetComponent<PsychicSpellbookMarker>()?.MaximumSpellLevel==6,"native spontaneous Magus book with psychic marker and Int");
                var list=A<BlueprintSpellList>(c,"MindbladePsychicSpellList01454");int expectedRows=Main.GetType("CallOfTheWild.Wrath.Prototype.MindbladeModeGrant01455")==null?10:11;Check(list.SpellsByLevel.Length==expectedRows&&list.SpellsByLevel.Select((r,i)=>r.SpellLevel==i).All(b=>b),"private access list preserves exact versioned indexed rows: "+expectedRows);Check(list.SpellsByLevel.Skip(7).All(r=>!r.Spells.Any()),"no seventh+ access spells");Check(!list.SpellsByLevel.SelectMany(r=>r.Spells).Any(s=>originalBook.SpellList.SpellsByLevel.SelectMany(r=>r.Spells).Contains(s)),"access excludes normal Magus spells");
                Check(root.AddFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,3,4,5,6,7,8,9,11,12,13,14,15,18,19}),"original Mindblade replacement levels");
            }else{
                Check(!root.ReplaceSpellbook.Spontaneous&&root.ReplaceStartingEquipment&&root.StartingItems.OfType<Kingmaker.Blueprints.Items.Shields.BlueprintItemShield>().Any(),"prepared book and original starting shield");
                var actual=root.ReplaceSpellbook.SpellsPerDay.Levels;Check(actual.Length==day.Length&&actual.SelectMany((r,i)=>r.Count.Select((count,level)=>count==(level==0?day[i][level]:Math.Max(0,day[i][level]-1)))).All(x=>x),"every actual Magus level/circle has exactly one fewer slot, with cantrips and native row shape preserved");
                Check(root.AddFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,5,7,8,9,13,14,16,17,19}),"original Skirnir replacement levels");
            }
        }
    }
    static void InsertDiagnostic(Kingmaker.Items.Slots.ItemSlot __instance,ItemEntity __0,bool __result)
    {
        if(__result)return;Console.WriteLine("native insertion rejected: slot="+__instance.GetType().Name+" active="+__instance.Active+" disabled="+__instance.Disabled+" lock="+__instance.Lock.Count+" has="+__instance.HasItem+" supported="+__instance.IsItemSupported(__0)+" equippable="+__0.CanBeEquippedBy(__instance.Owner)+" blueprint="+__0.Blueprint.AssetGuid+" possible="+AccessTools.Method(typeof(Kingmaker.Items.Slots.ItemSlot),"IsPossibleInsertItems").Invoke(__instance,null));
    }
    static bool equipmentPatched;
    static void EquipmentPatches(Harmony h)
    {
        if(equipmentPatched)return;equipmentPatched=true;
        h.Patch(AccessTools.Method(typeof(Kingmaker.Items.Slots.ItemSlot),"CanInsertItem"),postfix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(InsertDiagnostic)));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Game),"CurrentlyLoadedArea"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Area)));
        h.Patch(AccessTools.Method(typeof(Kingmaker.Blueprints.Items.Ecnchantments.ItemEnchantment),"OnActivate"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(NoEnchantmentFx)));
        var factories=typeof(ItemsEntityFactory).GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Where(m=>m.Name=="CreateEntity"&&!m.IsGenericMethod&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType==typeof(Kingmaker.Blueprints.Items.BlueprintItem)).ToArray();Check(factories.Length==1,"exact native non-generic item factory endpoint");h.Patch(factories[0],postfix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(ItemIdentity)));
        foreach(var type in Main.GetTypes().Where(t=>(t.Name.EndsWith("01454")||t.Name.EndsWith("01455"))&&t.GetCustomAttributes(typeof(HarmonyPatch),false).Any())){Console.WriteLine("054 production patch: "+type.Name);h.CreateClassProcessor(type).Patch();}
    }
    static void Manifestation(BlueprintsCache parent,Harmony h)
    {
        Console.WriteLine("054 manifestation: fresh native cache");
        var c=Fresh(parent);Register(c,"Mindblade01454");
        EquipmentPatches(h);
        Console.WriteLine("054 manifestation: construct unit");
        UnitEntityData unit;active=null;try{unit=(UnitEntityData)H("Unit",c,"45a4607686d96a1498891b3286121780",7);}finally{active=c;}unit.Stats.Intelligence.BaseValue=20;
        Actors.Add(unit);
        Check(ReferenceEquals(unit.Progression.Classes[0].CharacterClass,N<BlueprintCharacterClass>(c,"45a4607686d96a1498891b3286121780")),"constructed actor retains actual canonical native Magus class");
        unit.Descriptor.m_Inventory=new ItemsCollection(unit);
        Set(unit.Descriptor,"Proficiencies",new UnitProficiency());Set(unit.Descriptor,"Body",new UnitBody(unit.Descriptor));Set(unit.Body,"m_HandsAreEnabled",true);SeedFist(c,unit);
        Console.WriteLine("054 manifestation: native feature/resource activation");
        var feat=(Feature)H("Fact",unit,A<BlueprintFeature>(c,"MindbladePsychicPool01454"));
        Console.WriteLine("054 manifestation: actual leaf and native caster restrictions");
        var leaf=unit.Abilities.Enumerable.Select(a=>a.Data).SelectMany(a=>a.Blueprint.GetComponent<AbilityVariants>()?.Variants.Select(v=>new AbilityData(v,unit.Descriptor))??Enumerable.Empty<AbilityData>()).First(a=>a.Blueprint.GetComponent<MindbladeManifestCost01454>()?.Weapon.Get().Category==Kingmaker.Enums.WeaponCategory.Dagger);
        var marker=leaf.Blueprint.GetComponent<MindbladeManifestCost01454>();Check(!marker.IsCasterRestrictionPassed(unit)&&!marker.IsAbilityVisible(leaf),"unproficient manufactured weapon is unavailable and hidden");unit.Descriptor.Proficiencies.Add(Kingmaker.Enums.WeaponCategory.Dagger);Check(marker.IsAbilityVisible(leaf)&&marker.IsCasterRestrictionPassed(unit),"actual proficient empty hands allow the correct single form");
        var pool=N<BlueprintAbilityResource>(c,"effc3e386331f864e9e06d19dc218b37");int before=unit.Resources.GetResourceAmount(pool);Check(before>0,"native Magus pool receives actual class and Intelligence capacity");
        var context=new AbilityExecutionContext(leaf,new AbilityParams{CasterLevel=7,SpellLevel=0,DC=15},new TargetWrapper(unit),new RulebookEventContext(),unit);
        // Supernatural manifestation has no spellbook/material payment. Call
        // the exact native resource entry used by AbilityData.Spend; its CLR
        // spellbook prelude contains Unity ECalls even in the unused branch.
        Console.WriteLine("054 manifestation: native payment");AbilityCastRateUtils.SpendResources(leaf);Console.WriteLine("054 manifestation: native effect");leaf.Blueprint.GetComponent<AbilityEffectRunAction>().Apply(context,new TargetWrapper(unit));
        Console.WriteLine("054 manifestation result: weapon="+unit.Body.PrimaryHand.MaybeWeapon?.Blueprint.AssetGuid+" before="+before+" after="+unit.Resources.GetResourceAmount(pool)+" hand-lock="+unit.Body.PrimaryHand.Lock.Count+" buffs="+unit.Buffs.Enumerable.Count());
        Check(unit.Body.PrimaryHand.MaybeWeapon?.Blueprint==marker.Weapon.Get()&&unit.Resources.GetResourceAmount(pool)==before-1,"native resource spend and native effect application create the owned dagger once");
        var buff=unit.Buffs.Enumerable.Single(b=>b.Blueprint.GetComponent<MindbladeManifest01454>()!=null);var runtime=buff.Components.Single(r=>r.SourceBlueprintComponent is MindbladeManifest01454);var data=runtime.GetData<MindbladeManifestData01454>();var weapon=unit.Body.PrimaryHand.MaybeWeapon;
        Check(data.Created&&data.Paid==1&&data.Reserved==1&&!data.Refunded&&ReferenceEquals(data.Weapons[0],weapon),"actual payment receipt binds the exact created entity and one reserved point");
        Check(weapon.Enchantments.Any(e=>e.Blueprint.AssetGuid==Id("MindbladeManifestedEnchantment01454")),"actual native factory receives the manifested visual tag after donor cache warming");
        Check(data.Enchantments.Count==1&&weapon.Enchantments.Any(e=>ReferenceEquals(e,data.Enchantments[0])),"one owned native enhancement instance");
        var owned=data.Enchantments[0];var foreign=weapon.AddEnchantment(owned.Blueprint,new MechanicsContext(unit,unit.Descriptor,owned.Blueprint));
        Console.WriteLine("foreign diagnostic: actual="+weapon.Enchantments.Count()+" ours="+data.Enchantments.Count+" foreign-present="+weapon.Enchantments.Any(e=>ReferenceEquals(e,foreign))+" old-present="+weapon.Enchantments.Any(e=>ReferenceEquals(e,owned))+" ready="+buff.IsActive+"/"+buff.IsTurnedOn+"/"+buff.IsSuppressed+"/"+data.Busy);
        Check(foreign!=null&&!ReferenceEquals(foreign,owned)&&weapon.Enchantments.Any(e=>ReferenceEquals(e,foreign))&&!weapon.Enchantments.Any(e=>ReferenceEquals(e,owned))&&data.Enchantments.Count==0,"foreign identical enchantment suppresses only our instance, preventing double application");
        weapon.RemoveEnchantment(foreign);Check(data.Enchantments.Count==1&&!ReferenceEquals(data.Enchantments[0],foreign)&&weapon.Enchantments.Any(e=>ReferenceEquals(e,data.Enchantments[0])),"actual native removal event restores our missing property without waiting for a round");
        var stable=data.Enchantments.ToArray();var harmless=new BlueprintBuff{AssetGuid=BlueprintGuid.Parse("935a276e492647f8b0fbd71defa65454"),name="054HarmlessBuffInput",ComponentsArray=Array.Empty<BlueprintComponent>()};harmless.OnEnable();var other=(Buff)H("Buff",unit,harmless,null);other.Remove();
        Check(stable.SequenceEqual(data.Enchantments),"unrelated native buff add/remove causes no enchantment churn");
        unit.Body.PrimaryHand.Lock.Retain();buff.Remove();Check(unit.Body.PrimaryHand.MaybeItem==null&&unit.Body.PrimaryHand.Lock.Count==1&&unit.Resources.GetResourceAmount(pool)==before,"native expiry removes only our weapon/lock and refunds once");buff.Remove();Check(unit.Resources.GetResourceAmount(pool)==before,"repeated expiry cannot mint pool points");unit.Body.PrimaryHand.Lock.Release();
        leaf.Blueprint.GetComponent<AbilityEffectRunAction>().Apply(context,new TargetWrapper(unit));Check(unit.Body.PrimaryHand.MaybeItem==null&&unit.Resources.GetResourceAmount(pool)==before,"unpaid effect replay has no reusable payment receipt and cannot mint items or points");
        var foreignWeapon=marker.Weapon.Get().CreateEntity<ItemEntityWeapon>();using(ContextData<ItemEntity.CanBeEquippedForce>.Request())Check(unit.Body.PrimaryHand.InsertItem(foreignWeapon),"native insertion creates the occupied-hand negative control");Check(!marker.IsCasterRestrictionPassed(unit),"occupied hand is rejected by the actual caster checker");AbilityCastRateUtils.SpendResources(leaf);leaf.Blueprint.GetComponent<AbilityEffectRunAction>().Apply(context,new TargetWrapper(unit));Check(ReferenceEquals(unit.Body.PrimaryHand.MaybeItem,foreignWeapon)&&unit.Resources.GetResourceAmount(pool)==before,"late failed creation refunds actual payment without deleting the foreign item");unit.Body.PrimaryHand.RemoveItem();foreignWeapon.Collection?.Remove(foreignWeapon);
        MindbladeMore(c,unit,pool);
    }
    internal static int Run(BlueprintsCache parent,Harmony h)
    {
        checks=0;Coverage.Clear();equipmentPatched=false;Actors.Clear();var oldSave=Kingmaker.Game.Instance.SaveManager;Set(Kingmaker.Game.Instance,"SaveManager",FormatterServices.GetUninitializedObject(typeof(Kingmaker.EntitySystem.Persistence.SaveManager)));var oldUI=Kingmaker.Game.Instance.UI;Set(Kingmaker.Game.Instance,"UI",FormatterServices.GetUninitializedObject(typeof(Kingmaker.UI.UIAccess)));var index=Path.Combine(Root(),"work/batch01454/native-archive-index.json");paths=JObject.Parse(File.ReadAllText(index)).Properties().ToDictionary(p=>p.Name,p=>(string)p.Value);archive=ZipFile.OpenRead(Path.Combine(Root(),"work/batch01454/native-clr-fixture.zip"));
        h.Patch(AccessTools.Method(typeof(BlueprintsCache),nameof(BlueprintsCache.Load)),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(NativeLoad)){priority=Priority.First});
        var binder=(GuidClassBinder)Kingmaker.Blueprints.JsonSystem.Json.Serializer.Binder;foreach(var type in Main.GetTypes().Where(t=>t.Name.EndsWith("01454")||t.Name.EndsWith("01455")||t.Name.EndsWith("01457")||t.Name.EndsWith("01458")||t.Name.EndsWith("01463")||t.Name.EndsWith("01464")))foreach(var attribute in type.GetCustomAttributes(typeof(TypeIdAttribute),false).Cast<TypeIdAttribute>())binder.AddToCache(type,attribute.GuidString);
        // Standalone CLR cannot resolve Unity's shared localization assets.
        // This existing presentation-only transpiler replaces localization/name
        // endpoints; registration, costs, prerequisites and rule branches remain.
        foreach(var type in Main.GetTypes().Where(t=>t.Name.EndsWith("01454")||t.DeclaringType?.Name.EndsWith("01454")==true||t.Name.EndsWith("01457")||t.DeclaringType?.Name.EndsWith("01457")==true||PresentationType058(t)))
            foreach(var method in type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly).Where(m=>m.GetMethodBody()!=null&&(!m.ContainsGenericParameters||!m.DeclaringType.Name.EndsWith("01457"))))
                h.Patch(method,transpiler:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Labels)));
        AmnesiacPresentation064(h);
        // Keep the native iterator and every selection branch intact. Only its
        // display-name/description endpoints lack Unity localization services.
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact),"Name"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(NativeUiName)));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact),"Description"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(NativeUiDescription)));
        try{
            if(Environment.GetEnvironmentVariable("COTW_01454_CASE")=="structural"||Environment.GetEnvironmentVariable("COTW_01454_CASE")=="all")Structural(parent);
            Console.WriteLine("054 structural checks complete: "+checks);
            var chosen=Environment.GetEnvironmentVariable("COTW_01454_CASE");
            if(chosen=="temporary064")Temporary064(parent,h);
            if(chosen=="amnesiac064")Amnesiac064(parent,h);
            if((chosen??"").StartsWith("feedback066",StringComparison.Ordinal))Feedback066(parent,h,chosen);
            if((chosen??"").EndsWith("063",StringComparison.Ordinal))Urban063(parent,h);
            if((chosen??"").EndsWith("062",StringComparison.Ordinal))Feedback058(parent,h);
            if(chosen=="graph059"||chosen=="display059"||chosen=="reach059"||(chosen??"").EndsWith("060",StringComparison.Ordinal)||(chosen??"").EndsWith("061",StringComparison.Ordinal))Feedback058(parent,h);
            if(chosen=="feedback058"||chosen=="disciplines058"||chosen=="discipline-effects058"||chosen=="amplifications058"||chosen=="registration058")Feedback058(parent,h);
            if(chosen=="skirnir056")Skirnir056(parent,h);
            if(chosen=="mutation057"||chosen=="marauder057")Psychic057(parent,h,chosen);
            if(chosen=="faults057")Faults057(parent,h);
            if(chosen=="feedback055")Feedback055(parent,h);
            if(chosen=="feedback055-store")FeedbackStore055(parent,h);
            if(chosen=="feedback055-shield")FeedbackShield055(parent,h);
            if(chosen=="feedback055-icons")FeedbackIcons055(parent,h);
            if(chosen=="all"||chosen=="mechanics"||chosen=="mindblade")Manifestation(parent,h);
            if(chosen=="all"||chosen=="skirnir")SkirnirMechanics(parent,h);
            if(chosen=="all"||chosen=="whip")WhipProvider(parent,h);
            if(chosen=="all"||chosen=="publication")Publication(parent,h);
            PsychicExpansionTests.Inventory["magusBatch01454Contracts"]=new JObject{["passed"]=true,["phase"]="standalone-native-case",["caseOnly"]=true,["assertions"]=checks,["nativeArchiveLazilyLoaded"]=native.Count,["case"]=chosen,["coverage"]=new JArray(Coverage),["ownedBlueprints"]=new JArray(Own(active).Select(id=>new JObject{["id"]=id,["name"]=active.Load(BlueprintGuid.Parse(id)).name,["type"]=active.Load(BlueprintGuid.Parse(id)).GetType().FullName})),["boundary"]="Native blueprint JSON is resolved by exact archive GUID in this isolated CLR cache. Actual resource entry, buff execution, item factory, slot insertion, enchantment events and cleanup execute. Actor/proxy/inventory/world/UI inputs and FX rendering are controlled; complete game cast/scene/save/turn validation is separate. All three complete feature contracts are required before delivery."};
            Console.WriteLine("PASS: "+checks+" 054 development checks");return checks;
        }finally{
            try{foreach(var u in Actors.AsEnumerable().Reverse()){foreach(var buff in u.Buffs.Enumerable.ToArray())buff.Remove();foreach(var fact in u.Facts.List.ToArray())if(fact.Manager!=null)u.Facts.Remove(fact);}}
            finally{Set(Kingmaker.Game.Instance,"UI",oldUI);Set(Kingmaker.Game.Instance,"SaveManager",oldSave);active=null;archive.Dispose();}
        }
    }
}
