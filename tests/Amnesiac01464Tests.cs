using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Kingmaker.PubSubSystem;
using Kingmaker.Localization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    // Exact historical DLL controls must exercise their own original 064
    // expectations before reaching the intended negative assertion. Every
    // other DLL is tested against the current user-requested contract.
    static bool? AmnesiacLegacyControlCache066;
    static bool AmnesiacLegacyControl066()
    {
        if(!AmnesiacLegacyControlCache066.HasValue){
            string sha;
            using(var input=File.OpenRead(Main.Location))
            using(var algorithm=System.Security.Cryptography.SHA256.Create())
                sha=BitConverter.ToString(algorithm.ComputeHash(input)).Replace("-","");
            AmnesiacLegacyControlCache066=sha=="D7E3724D49E0799CC82A839899726DE8E64BEBBF9199ECB2DCB567AA06B4097A"||sha=="31E01B5A95280562457DCE10E7E2A0BC56425A5F0F62B442316F05BB30740B93";
            Console.WriteLine("066 Amnesiac historical-control contract="+AmnesiacLegacyControlCache066.Value+" SHA256="+sha);
        }
        return AmnesiacLegacyControlCache066.Value;
    }
    static void EnglishInputs064()
    {
        if(GameEnglish060==null)GameEnglish060=(JObject)JObject.Parse(File.ReadAllText(Path.Combine(Root(),"../..", "steam/steamapps/common/Pathfinder Second Adventure/Wrath_Data/StreamingAssets/Localization/enGB.json")))["strings"];
        if(PackageEnglish060==null)PackageEnglish060=(JObject)JObject.Parse(File.ReadAllText(Path.Combine(Root(),"outputs/CallOfTheWild.Wrath.Prototype/package/Localization/enGB.json")))["strings"];
    }
    static string EnglishName064(BlueprintScriptableObject bp){EnglishInputs064();return EnglishName060(bp);}
    static string EnglishDescription064(BlueprintScriptableObject bp)
    {
        EnglishInputs064();var text=AccessTools.Field(bp.GetType(),"m_Description")?.GetValue(bp) as LocalizedString;
        var authored=(Dictionary<string,string>)AccessTools.Field(T("PsychicAssets"),"Strings").GetValue(null);
        string key=text?.Key;
        if(key!=null&&authored.TryGetValue(key,out var own))return own;
        if(key!=null&&GameEnglish060[key]?.Type==JTokenType.String)return (string)GameEnglish060[key];
        if(key!=null&&PackageEnglish060[key]?.Type==JTokenType.String)return (string)PackageEnglish060[key];
        if(paths.TryGetValue(bp.AssetGuid.ToString(),out var path)){
            JObject raw;using(var r=new StreamReader(archive.GetEntry(path).Open()))raw=JObject.Parse(r.ReadToEnd());
            var value=raw["Data"]?["m_Description"];string direct=(string)value?["m_Key"],shared=(string)(value?["Shared"] as JObject)?["stringkey"];
            string original=string.IsNullOrEmpty(direct)?shared:direct;
            if(!string.IsNullOrEmpty(original)&&GameEnglish060[original]?.Type==JTokenType.String)return (string)GameEnglish060[original];
            if(string.IsNullOrEmpty(original))return "";
        }
        throw new InvalidOperationException("064 no real English description: "+bp.name+" / "+key);
    }
    static IEnumerable<CodeInstruction> AmnesiacLabels064(IEnumerable<CodeInstruction> instructions)
    {
        foreach(var i in instructions){
            if(i.operand is MethodInfo m&&typeof(BlueprintScriptableObject).IsAssignableFrom(m.DeclaringType)&&m.ReturnType==typeof(string)&&(m.Name=="get_Name"||m.Name=="get_Description")){
                i.opcode=OpCodes.Call;i.operand=AccessTools.Method(typeof(MagusBatch01454Tests),m.Name=="get_Name"?nameof(EnglishName064):nameof(EnglishDescription064));
            }
            yield return i;
        }
    }
    static void AmnesiacPresentation064(Harmony h)
    {
        var type=Main.GetType("CallOfTheWild.Wrath.Prototype.Amnesiac01464");if(type==null)return;
        foreach(var t in new[]{type}.Concat(type.GetNestedTypes(BindingFlags.Public|BindingFlags.NonPublic)))
            foreach(var m in t.GetMethods(BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(m=>m.GetMethodBody()!=null&&!m.ContainsGenericParameters))
                h.Patch(m,transpiler:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(AmnesiacLabels064)));
    }
    static int D100Input064,D10Input064,D100Calls064,D10Calls064;
    static readonly List<RuleRollDice> RecallDice064=new List<RuleRollDice>();
    static UnitEntityData LevelStartOwner064,LevelStartPreview064;
    static Spellbook LevelStartBook064,LevelStartPreviewBook064;
    static BlueprintAbility LevelStartBorrowed064,LevelStartPaid064;
    static int LevelStartCalls064;
    static LevelUpController LevelStartController064;
    sealed class FirstLevelUpListener064:ILevelUpInitiateUIHandler
    {
        public void HandleLevelUpStart(LevelUpConfig config)
        {if(config?.Unit==LevelStartOwner064)LevelStartController064=config.StartController();}
    }
    static bool ControllerConstructionEndpoint064(UnitEntityData __0,ref LevelUpController __result)
    {
        Check(ReferenceEquals(__0,LevelStartOwner064)&&!LevelStartBook064.IsKnown(LevelStartBorrowed064),"064 recalled knowledge must be cleared before native StartController reaches its construction endpoint");
        AccessTools.Method(typeof(UnitHelper),"CopySpellbook").Invoke(null,new object[]{__0,LevelStartPreview064});
        Check(!LevelStartPreviewBook064.IsKnown(LevelStartBorrowed064)&&LevelStartBook064.IsKnown(LevelStartPaid064)&&LevelStartPreviewBook064.IsKnown(LevelStartPaid064),"064 actual preview CopySpellbook after StartController entry excludes borrowed knowledge and preserves permanent knowledge");
        LevelStartCalls064++;__result=(LevelUpController)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(LevelUpController));return false;
    }
    static void RecallDiceInput064(RuleRollDice __instance)
    {
        if(__instance.DiceFormula.Dice==DiceType.D100){D100Calls064++;Set(__instance,"m_PreRolledResult",(int?)D100Input064);RecallDice064.Add(__instance);}
        else if(__instance.DiceFormula.Dice==DiceType.D10){D10Calls064++;Set(__instance,"m_PreRolledResult",(int?)D10Input064);RecallDice064.Add(__instance);}
    }
    static BlueprintsCache AmnesiacCache064(BlueprintsCache parent)
    {
        var c=PsychicCache057(parent);WarmMercies058(c);Register(c,"MutationMind01457");Register(c,"PsychicMarauder01457");Register(c,"PsychicClassCompletion01458");return c;
    }
    static UnitEntityData AmnesiacOwner064(BlueprintsCache c,int level,out Spellbook book)
    {
        var owner=EquipmentUnit(c,level,Psychic058);var cls=N<BlueprintCharacterClass>(c,Psychic058);
        owner.Progression.Classes[0].AddArchetype(A<BlueprintArchetype>(c,"AmnesiacArchetype01464"));
        foreach(var old in owner.Facts.List.Where(f=>f.Blueprint.AssetGuid==BlueprintGuid.Parse("4896622b699957208e128c3a5361609c")).ToArray())owner.Facts.Remove(old);
        Grant(owner,c,"AmnesiacSpellcasting01464");
        book=owner.Descriptor.DemandSpellbook(cls);
        Check(book.RawBaseLevel==0&&book.Blueprint==A<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook>(c,"AmnesiacSpellbook01464"),"064 native class-data replacement selects a new zero-level Amnesiac book at class level "+level);
        for(int i=0;i<level;i++)book.AddLevelFromClass(cls);
        Grant(owner,c,"AmnesiacRecollectionFeature01464");return owner;
    }
    static void Amnesiac064(BlueprintsCache parent,Harmony h)
    {
        EquipmentPatches(h);
        var early=Main.GetType("CallOfTheWild.Wrath.Prototype.AmnesiaLevelUp01464Patch");if(early!=null)h.CreateClassProcessor(early).Patch();
        foreach(var type in T("PsychicUndercasting").GetNestedTypes(BindingFlags.NonPublic|BindingFlags.Public)
            .Concat(T("TemporaryKnowledge01464").GetNestedTypes(BindingFlags.NonPublic|BindingFlags.Public))
            .Where(t=>t.GetCustomAttributes(typeof(HarmonyPatch),false).Any()))h.CreateClassProcessor(type).Patch();
        h.Patch(AccessTools.Method(typeof(RuleRollDice),"OnTrigger"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(RecallDiceInput064)));
        var c=AmnesiacCache064(parent);var cls=N<BlueprintCharacterClass>(c,Psychic058);
        var before=cls.Archetypes.ToArray();var daily=cls.Spellbook.SpellsPerDay;var known=cls.Spellbook.SpellsKnown;
        var originalRows=known.Levels.Select(e=>(int[])e.Count.Clone()).ToArray();var originalList=cls.Spellbook.SpellList;
        Register(c,"Amnesiac01464");var root=A<BlueprintArchetype>(c,"AmnesiacArchetype01464");
        Check(root!=null&&root.GetParentClass()==cls&&cls.Archetypes.Count(a=>a==root)==1&&before.All(a=>cls.Archetypes.Contains(a)),"064 actual complete Amnesiac root adds once and preserves all prior Psychic archetypes");
        var bookBP=root.ReplaceSpellbook;
        Check(bookBP.SpellsPerDay==daily&&bookBP.SpellList==originalList&&cls.Spellbook.SpellsKnown==known&&bookBP.SpellsKnown!=known,"064 replacement book changes only its private known table and preserves shared daily slots/list/table identity");
        Check((bool)AccessTools.Method(T("PsychicCasting"),"IsPsychic",new[]{typeof(Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook)}).Invoke(null,new object[]{bookBP}),"064 actual replacement book retains native thought/emotion Psychic marker");
        var oracle=JObject.Parse(File.ReadAllText(Path.Combine(Root(),"work/batch01464/amnesiac-table-oracle064.json")));
        var rows=(JArray)oracle["originalRetainedRows"];
        Check(bookBP.SpellsKnown.Levels.Length==21&&Enumerable.Range(0,21).All(i=>bookBP.SpellsKnown.Levels[i].Count.SequenceEqual(rows[i].Values<int>())),"064 every one of the original twenty-one Amnesiac known rows matches the independent frozen source oracle");
        var nativeProgression=new ProgressionData(cls.Progression);nativeProgression.AddArchetype(root);
        Check(!nativeProgression.GetLevelEntry(1).Features.Any(f=>f.AssetGuid==BlueprintGuid.Parse("4896622b699957208e128c3a5361609c"))&&nativeProgression.GetLevelEntry(1).Features.Contains(A<BlueprintFeature>(c,"AmnesiacRecollectionFeature01464")),"064 native progression replaces casting presentation and grants the real recollection feature at level one");
        var menus=Enumerable.Range(1,9).Select(i=>A<BlueprintAbility>(c,"AmnesiacRecallMenu"+i+"01464")).ToArray();
        var expected=originalList.SpellsByLevel.Where(e=>e.SpellLevel>=1&&e.SpellLevel<=9).SelectMany(e=>e.Spells.Where(s=>s.Type==AbilityType.Spell).Select(s=>e.SpellLevel+":"+s.AssetGuid)).Distinct().OrderBy(s=>s).ToArray();
        var actual=menus.SelectMany(m=>m.GetComponent<AbilityVariants>().Variants).Select(a=>a.GetComponent<AmnesiaRecallGate01464>()).Select(g=>g.Circle+":"+g.Spell.Guid).OrderBy(s=>s).ToArray();
        Check(actual.SequenceEqual(expected),"064 recall catalogue covers every exact real current Psychic root and circle with no invented placeholder");
        var assets=Loaded(c).OfType<BlueprintScriptableObject>().Where(a=>a.name?.StartsWith("CotWWrathPsychicAmnesiac",StringComparison.Ordinal)==true).ToArray();
        Check(assets.Length==25+2*expected.Length&&assets.Select(a=>a.AssetGuid).Distinct().Count()==assets.Length,"064 complete private graph contains twenty-five core assets and exactly one ability/source pair per real spell root");
        Register(c,"Amnesiac01464");Check(assets.All(a=>ReferenceEquals(c.Load(a.AssetGuid),a))&&cls.Archetypes.Count(a=>a==root)==1,"064 strict repeat retains every published Amnesiac identity and one parent link");
        AmnesiacSerialization064(c,assets);
        for(int level=1;level<=20;level++){
            var owner=AmnesiacOwner064(c,level,out var book);
            for(int circle=1;circle<=9;circle++){
                int capacity=(int)oracle["capacityByClassLevel"][level-1]["byCircle"][circle-1]+(!AmnesiacLegacyControl066()&&circle>=2&&level==circle*2?1:0);
                var resource=A<BlueprintAbilityResource>(c,"AmnesiacSlot"+circle+"01464");
                Check(resource.GetMaxAmount(owner.Descriptor)==capacity&&owner.Resources.GetResourceAmount(resource)==capacity,
                    "066 actual native maximum and initial balance equal the old independent oracle plus the requested first-unlock increment level="+level+" circle="+circle+" max="+resource.GetMaxAmount(owner.Descriptor)+" amount="+owner.Resources.GetResourceAmount(resource));
                bool eligible=capacity>0&&circle<=book.MaxSpellLevel;
                Check(menus[circle-1].GetComponent<AmnesiaRecallGate01464>().IsAbilityVisible(new AbilityData(menus[circle-1],owner.Descriptor))==eligible,
                    "064 actual native circle visibility follows real available book level and nonzero amnesia capacity level="+level+" circle="+circle);
            }
        }
        AmnesiacLevelGrowth064(c,oracle);
        foreach(var sample in new[]{new[]{1,0},new[]{10,0},new[]{11,0},new[]{35,0},new[]{36,0},new[]{95,0},new[]{96,0},new[]{100,0},new[]{1,10},new[]{35,1},new[]{90,6},new[]{100,10}})
            AmnesiacRoll064(c,menus[0],sample[0],sample[1]);
        Check(RecallDice064.All(r=>r.RollHistory!=null&&r.RollHistory.Count==1&&r.Result==r.RollHistory[0]),"064 every controlled native recollection die runs native OnTrigger/Roll and records its real roll history");
        AmnesiacLevelStart064(c,menus[0],h);
        Check(Enumerable.Range(0,originalRows.Length).All(i=>originalRows[i].SequenceEqual(known.Levels[i].Count)),"064 table, resource and recall tests leave all original shared known-table cells unchanged");
        var texts=new Dictionary<string,string>((Dictionary<string,string>)AccessTools.Field(T("PsychicAssets"),"Strings").GetValue(null));
        AmnesiacPublication064(parent,h,c,assets);
        PsychicExpansionTests.Inventory["amnesiac01464Contracts"]=new JObject{
            ["passed"]=true,["assertions"]=checks,["coverage"]=new JArray(Coverage),["capacityCells"]=180,["rollCases"]=12,
            ["d100Calls"]=D100Calls064,["d10Calls"]=D10Calls064,["ownedBlueprints"]=new JArray(assets.Select(a=>new JObject{["id"]=a.AssetGuid.ToString(),["name"]=a.name,["type"]=a.GetType().FullName})),
            ["ownedTexts"]=JObject.FromObject(texts.Where(p=>p.Key.Contains("Amnesiac")).ToDictionary(p=>p.Key,p=>p.Value)),
            ["gameLaunchTested"]=false,["boundary"]="Actual native replacement spellbook, all20x9 resource cells, buff source ownership, rule rolls and history, native action cost and slot payment. Native StartController wrapper executes before an explicitly replaced controller-construction endpoint, which observes cleanup and calls real CopySpellbook; complete native controller construction/UI is not executed. Dice inputs and minimal actor/game services are explicit fixtures; full game UI/save/scene transport is separate."};
    }
    static void AmnesiacLevelStart064(BlueprintsCache c,BlueprintAbility menu,Harmony h)
    {
        Console.WriteLine("064 stage: UI listener starts controller before source event callback");
        LevelStartOwner064=AmnesiacOwner064(c,5,out LevelStartBook064);LevelStartPreview064=AmnesiacOwner064(c,5,out LevelStartPreviewBook064);
        LevelStartBorrowed064=A<BlueprintAbility>(c,"MindThrust1");LevelStartPaid064=N<BlueprintAbility>(c,"c7104f7526c4c524f91474614054547e");LevelStartBook064.AddKnown(2,LevelStartPaid064,false);
        var leaf=menu.GetComponent<AbilityVariants>().Variants.Single(a=>a.GetComponent<AmnesiaRecallGate01464>().Spell.Guid==LevelStartBorrowed064.AssetGuid);var action=leaf.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<RecallAmnesiaSpell01464>().Single();
        var resource=action.Resource.Get();int initial=LevelStartOwner064.Resources.GetResourceAmount(resource);LevelStartOwner064.Resources.Spend(resource,1);
        var listener=new FirstLevelUpListener064();EventBus.Subscribe(listener);
        var source=LevelStartOwner064.Buffs.AddBuff(action.Known.Get(),LevelStartOwner064,null);
        Check(source.IsAttached&&LevelStartBook064.IsKnown(LevelStartBorrowed064)&&LevelStartBook064.GetKnownSpell(LevelStartBorrowed064).IsTemporary,"064 pre-controller source is native borrowed knowledge with one paid amnesia slot");
        var start=AccessTools.Method(typeof(LevelUpController),"Start",new[]{typeof(UnitEntityData),typeof(LevelUpState.CharBuildMode),typeof(bool),typeof(bool)});var endpoint=AccessTools.Method(typeof(MagusBatch01454Tests),nameof(ControllerConstructionEndpoint064));
        Check(start!=null,"064 exact native controller construction endpoint is present");h.Patch(start,prefix:new HarmonyMethod(endpoint));LevelStartCalls064=0;
        try{
            var config=LevelUpConfig.Create(LevelStartOwner064);LevelStartController064=null;
            EventBus.RaiseEvent<ILevelUpInitiateUIHandler>(v=>v.HandleLevelUpStart(config));
            Check(LevelStartController064!=null&&LevelStartCalls064==1&&!source.IsAttached&&LevelStartOwner064.Resources.GetResourceAmount(resource)==initial,"064 single native start-event dispatch runs the earlier UI listener and actual StartController cleanup before preview construction");
            EventBus.Unsubscribe(listener);
            EventBus.RaiseEvent<ILevelUpInitiateUIHandler>(v=>v.HandleLevelUpStart(config));
            Check(LevelStartOwner064.Resources.GetResourceAmount(resource)==initial&&LevelStartBook064.IsKnown(LevelStartPaid064),"064 later global start notification is idempotent and preserves permanent knowledge");
        }finally{EventBus.Unsubscribe(listener);h.Unpatch(start,endpoint);LevelStartOwner064=LevelStartPreview064=null;LevelStartBook064=LevelStartPreviewBook064=null;LevelStartController064=null;}
    }
    static void AmnesiacSerialization064(BlueprintsCache c,BlueprintScriptableObject[] assets)
    {
        foreach(var type in new[]{typeof(AmnesiaCapacity01464),typeof(AmnesiaRecallGate01464),typeof(AmnesiaSource01464),typeof(RecallAmnesiaSpell01464)}){
            object value=assets.SelectMany(a=>a.ComponentsArray.Cast<object>()).FirstOrDefault(o=>o.GetType()==type);
            BlueprintScriptableObject owner;
            if(value is BlueprintComponent component)owner=component.OwnerBlueprint;
            else{
                owner=assets.OfType<BlueprintAbility>().First(a=>a.GetComponent<AbilityEffectRunAction>()?.Actions.Actions.Any(v=>v.GetType()==type)==true);
                value=((BlueprintAbility)owner).GetComponent<AbilityEffectRunAction>().Actions.Actions.Single(v=>v.GetType()==type);
            }
            var elements=owner.ElementsArray;var saved=elements.ToArray();var old=Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead;
            try{
                Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=new BlueprintJsonWrapper(owner);
                var writer=new StringWriter();var json=new Newtonsoft.Json.JsonTextWriter(writer);var baseType=value is BlueprintComponent?typeof(BlueprintComponent):typeof(Kingmaker.ElementsSystem.GameAction);
                Kingmaker.Blueprints.JsonSystem.Json.Serializer.Serialize(json,value,baseType);json.Flush();
                var restored=Kingmaker.Blueprints.JsonSystem.Json.Serializer.Deserialize(new Newtonsoft.Json.JsonTextReader(new StringReader(writer.ToString())),baseType);
                Check(restored?.GetType()==type&&writer.ToString().Contains(type.GetCustomAttributes(typeof(TypeIdAttribute),false).Cast<TypeIdAttribute>().Single().GuidString),"064 actual native polymorphic serializer restores the frozen component TypeId "+type.Name);
            }finally{elements.Clear();elements.AddRange(saved);Set(owner,"m_AllElements",elements);Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=old;}
        }
        foreach(bool paid in new[]{true,false}){
            var data=new AmnesiaSourceData01464{RefundOnLevelUp=paid};var sw=new StringWriter();var json=new Newtonsoft.Json.JsonTextWriter(sw);
            Kingmaker.Blueprints.JsonSystem.Json.Serializer.Serialize(json,data);json.Flush();var copy=Kingmaker.Blueprints.JsonSystem.Json.Serializer.Deserialize<AmnesiaSourceData01464>(new Newtonsoft.Json.JsonTextReader(new StringReader(sw.ToString())));
            Check(copy.RefundOnLevelUp==paid,"064 actual native source-data serializer preserves paid/free refund ownership: "+paid);
        }
    }
    static bool AmnesiacOnly064(string name)=>name=="Psychic archetype: Amnesiac";
    static void AmnesiacPublication064(BlueprintsCache parent,Harmony h,BlueprintsCache ready,BlueprintScriptableObject[] assets)
    {
        h.Patch(AccessTools.Method(typeof(BlueprintsCache),nameof(BlueprintsCache.AddCachedBlueprint)),postfix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(PublicationWrite)));
        h.Patch(AccessTools.Method(T("MagusBatch01454"),"VerifyArchetype"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(PublicationVerify)));
        foreach(int fault in new[]{1,2}){
            var c=AmnesiacCache064(parent);var cls=N<BlueprintCharacterClass>(c,Psychic058);var roots=cls.Archetypes.ToArray();var groups=cls.Progression.UIGroups;
            publicationId=Id("AmnesiacArchetype01464");publicationFault=fault;
            Check(Refused(()=>Register(c,"Amnesiac01464"))&&publicationFault==0,"064 actual cache or parent publication exception is rejected: "+fault);
            Check(assets.All(a=>c.Load(a.AssetGuid)==null)&&roots.SequenceEqual(cls.Archetypes)&&ReferenceEquals(groups,cls.Progression.UIGroups),"064 failed publication rolls back the complete owned graph and exact parent UI references: "+fault);
            Register(c,"Amnesiac01464");Check(assets.All(a=>c.Load(a.AssetGuid)!=null),"064 actual retry after failed publication restores a complete graph: "+fault);
        }
        publicationFault=0;
        var collision=AmnesiacCache064(parent);var occupied=Cache063(collision,new BlueprintFeature{AssetGuid=Id("AmnesiacRecollectionFeature01464"),name="ForeignOccupiedAmnesia",ComponentsArray=Array.Empty<BlueprintComponent>()});
        Check(Refused(()=>Register(collision,"Amnesiac01464"))&&ReferenceEquals(collision.Load(occupied.AssetGuid),occupied)&&assets.Where(a=>a.AssetGuid!=occupied.AssetGuid).All(a=>collision.Load(a.AssetGuid)==null),"064 occupied private GUID rejects the complete transaction and preserves the foreign resource");
        var registrar=T("BlueprintRegistration");var ordinary=AccessTools.Method(registrar,"RegisterModule");var available=AccessTools.Method(registrar,"RegisterAvailableModule");var investigator=AccessTools.Method(T("ExtraInvestigatorTalent"),"Register");
        var skip=AccessTools.Method(typeof(MagusBatch01454Tests),nameof(UrbanSkipModule063));var only=AccessTools.Method(typeof(MagusBatch01454Tests),nameof(AmnesiacOnly064));var noInvestigator=AccessTools.Method(typeof(MagusBatch01454Tests),nameof(UrbanNoInvestigator063));
        h.Patch(ordinary,prefix:new HarmonyMethod(skip));h.Patch(available,prefix:new HarmonyMethod(only));h.Patch(investigator,prefix:new HarmonyMethod(noInvestigator));
        try{
            var c=AmnesiacCache064(parent);var cls=N<BlueprintCharacterClass>(c,Psychic058);var reports=new List<Tuple<string,bool,Exception>>();Action<string,bool,Exception> report=(name,ok,error)=>reports.Add(Tuple.Create(name,ok,error));
            Call(registrar,"RegisterAll",c,report);Check(reports.Count==1&&reports[0].Item2&&reports[0].Item3==null,"064 actual RegisterAll available caller executes Amnesiac and reports its real success");
            var oldRoots=(BlueprintArchetypeReference[])AccessTools.Field(cls.GetType(),"m_Archetypes").GetValue(cls);var foreign=Cache063(c,new BlueprintArchetype{AssetGuid=BlueprintGuid.NewGuid(),name="ForeignAmnesiacArchetype",ComponentsArray=Array.Empty<BlueprintComponent>()});
            Set(cls,"m_Archetypes",oldRoots.Concat(new[]{Ref063<BlueprintArchetypeReference>(foreign)}).ToArray());reports.Clear();Call(registrar,"RegisterOptional",c,report);
            Check(!reports.Any(r=>r.Item1=="Psychic archetype: Amnesiac"&&r.Item2)&&cls.Archetypes.Contains(foreign),"064 actual RegisterOptional rejects a late equivalent external Amnesiac while preserving both published roots");
            Set(cls,"m_Archetypes",oldRoots);reports.Clear();Call(registrar,"RegisterOptional",c,report);Check(reports.Count==1&&reports[0].Item2,"064 actual optional retry resumes after the external conflict is removed");
        }finally{h.Unpatch(ordinary,skip);h.Unpatch(available,only);h.Unpatch(investigator,noInvestigator);}
        ready=AmnesiacCache064(parent);Register(ready,"Amnesiac01464");
        var psychic=N<BlueprintCharacterClass>(ready,Psychic058);var list=psychic.Spellbook.SpellList;var row=list.SpellsByLevel[1];
        var donor=row.Spells.First(s=>s.Type==AbilityType.Spell);var replacement=(BlueprintAbility)AccessTools.Method(typeof(object),"MemberwiseClone").Invoke(donor,null);replacement.AssetGuid=donor.AssetGuid;
        ready.RemoveCachedBlueprint(donor.AssetGuid);ready.AddCachedBlueprint(donor.AssetGuid,replacement);native[donor.AssetGuid]=replacement;
        try{Check(Refused(()=>Register(ready,"Amnesiac01464")),"064 strict repeat rejects replacement of an accepted spell donor with the same GUID");}
        finally{ready.RemoveCachedBlueprint(donor.AssetGuid);ready.AddCachedBlueprint(donor.AssetGuid,donor);native[donor.AssetGuid]=donor;}
        var late=(BlueprintAbility)AccessTools.Method(typeof(object),"MemberwiseClone").Invoke(donor,null);late.AssetGuid=BlueprintGuid.NewGuid();late.name="ActualLateRecallDonor064";Cache063(ready,late);
        row.SpellsRefs.Add(Ref063<BlueprintAbilityReference>(late));
        var menu=A<BlueprintAbility>(ready,"AmnesiacRecallMenu101464");var variants=menu.GetComponent<AbilityVariants>();int count=variants.Variants.Length;
        publicationId=Id("AmnesiacRecall1"+late.AssetGuid+"01464");publicationFault=1;
        Check(Refused(()=>Register(ready,"Amnesiac01464"))&&variants.Variants.Length==count&&ready.Load(publicationId)==null,"064 late catalogue cache failure restores the exact old menu and removes partial private assets");
        Register(ready,"Amnesiac01464");Check(variants.Variants.Length==count+1&&variants.Variants.Any(a=>a.GetComponent<AmnesiaRecallGate01464>().Spell.Get()==late),"064 late real spell catalogue adds exactly one callable recall without replacing old variants");
        Register(ready,"Amnesiac01464");Check(variants.Variants.Length==count+1,"064 strict late-catalogue repeat is idempotent");
        // Keep this explicit late donor until the cache is discarded: removing
        // an accepted provider is correctly a closed dependency failure.
    }
    static void AmnesiacLevelGrowth064(BlueprintsCache c,JObject oracle)
    {
        Console.WriteLine("064 stage: native same-actor level progression");
        var owner=AmnesiacOwner064(c,1,out var book);var cls=N<BlueprintCharacterClass>(c,Psychic058);
        Set(owner.Progression,"Owner",owner.Descriptor);
        if(owner.State.Features==null)Set(owner.State,"Features",System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnitMechanicFeatures)));
        if(owner.State.Features.LegendaryHero==null)Set(owner.State.Features,"LegendaryHero",new Kingmaker.Utility.CountableFlag());
        var progression=Game.Instance.BlueprintRoot.Progression;var xp=AccessTools.Field(progression.GetType(),"m_XPTable").GetValue(progression);
        Set(progression,"m_XPTable",Ref063<BlueprintStatProgressionReference>(N<BlueprintStatProgression>(c,"87c24ce6bcf1a994296f3c582c1a632b")));
        Check(owner.Progression.ExperienceTable.Bonuses.Length>20,"064 real native XP progression service is hydrated for actual AddClassLevel");
        if(AccessTools.Field(typeof(UnitProgressionData),"m_ClassesOrder").GetValue(owner.Progression)==null)Set(owner.Progression,"m_ClassesOrder",new List<BlueprintCharacterClass>{cls});
        var first=A<BlueprintAbilityResource>(c,"AmnesiacSlot101464");owner.Resources.Spend(first,1);
        var capacity=owner.Facts.List.Single(f=>f.Blueprint==A<BlueprintFeature>(c,"AmnesiacRecollectionFeature01464"));
        var runtime=capacity.Components.Single(r=>r.SourceBlueprintComponent is AmnesiaCapacity01464);
        try{for(int level=2;level<=20;level++){
            int[] before=Enumerable.Range(1,9).Select(i=>owner.Resources.GetResourceAmount(A<BlueprintAbilityResource>(c,"AmnesiacSlot"+i+"01464"))).ToArray();
            int[] previous=runtime.GetData<AmnesiaCapacityData01464>().Previous.ToArray();
            owner.Progression.AddClassLevel(cls);book.AddLevelFromClass(cls);
            EventBus.RaiseEvent<IOwnerGainLevelHandler>(owner,h=>h.HandleUnitGainLevel());
            for(int circle=1;circle<=9;circle++){
                var resource=A<BlueprintAbilityResource>(c,"AmnesiacSlot"+circle+"01464");int max=(int)oracle["capacityByClassLevel"][level-1]["byCircle"][circle-1]+(!AmnesiacLegacyControl066()&&circle>=2&&level==circle*2?1:0);
                int expected=Math.Min(max,before[circle-1]+Math.Max(0,max-previous[circle-1]));
                Check(owner.Progression.GetClassLevel(cls)==level&&resource.GetMaxAmount(owner.Descriptor)==max&&owner.Resources.GetResourceAmount(resource)==expected,
                    "066 same native actor class gain follows the old independent oracle plus the requested first-unlock increment, clamps retired circles and preserves spent attempts level="+level+" circle="+circle);
            }
            var balances=Enumerable.Range(1,9).Select(i=>owner.Resources.GetResourceAmount(A<BlueprintAbilityResource>(c,"AmnesiacSlot"+i+"01464"))).ToArray();
            var saved=Newtonsoft.Json.JsonConvert.SerializeObject(runtime.GetData<AmnesiaCapacityData01464>());
            var loaded=Newtonsoft.Json.JsonConvert.DeserializeObject<AmnesiaCapacityData01464>(saved);
            runtime.GetData<AmnesiaCapacityData01464>().Previous=loaded.Previous;
            EventBus.RaiseEvent<IOwnerGainLevelHandler>(owner,h=>h.HandleUnitGainLevel());
            Check(balances.SequenceEqual(Enumerable.Range(1,9).Select(i=>owner.Resources.GetResourceAmount(A<BlueprintAbilityResource>(c,"AmnesiacSlot"+i+"01464")))),"064 serialized capacity state and duplicate native gain event cannot refill consumed amnesia slots level="+level);
            if(level==5){
                capacity.Reapply();capacity.TurnOff();capacity.TurnOn();
                Check(balances.SequenceEqual(Enumerable.Range(1,9).Select(i=>owner.Resources.GetResourceAmount(A<BlueprintAbilityResource>(c,"AmnesiacSlot"+i+"01464")))),"064 actual native fact Reapply and TurnOff/TurnOn preserve paid expenditure without a second initial fill");
            }
        }}finally{Set(progression,"m_XPTable",xp);}
    }
    static void AmnesiacRoll064(BlueprintsCache c,BlueprintAbility menu,int d100,int d10)
    {
        Console.WriteLine("064 stage: recollection "+d100+"+"+d10);
        var owner=AmnesiacOwner064(c,5,out var book);Set(owner.CombatState,"m_InCombat",d10>0);
        Check(owner.IsInCombat==(d10>0),"064 actual native actor combat state matches the recollection input "+d100+"+"+d10);
        var ordinary=A<BlueprintAbility>(c,"MindThrust1");var leaf=menu.GetComponent<AbilityVariants>().Variants.Single(a=>a.GetComponent<AmnesiaRecallGate01464>().Spell.Guid==ordinary.AssetGuid);
        var ability=new AbilityData(leaf,owner.Descriptor);var gate=leaf.GetComponent<AmnesiaRecallGate01464>();var resource=A<BlueprintAbilityResource>(c,"AmnesiacSlot101464");
        int poolBefore=owner.Resources.GetResourceAmount(Pool058(c,owner));book.RestoreSpontaneousSlots(1,4);int slots=book.GetSpontaneousSlots(1),before=owner.Resources.GetResourceAmount(resource);
        Check(gate.IsCasterRestrictionPassed(owner)&&leaf.GetComponent<AbilityResourceLogic>().IsAbilityRestrictionPassed(ability),"064 real native restriction/resource path accepts an unknown affordable first-circle recall");
        D100Input064=d100;D10Input064=d10;int rolls=D100Calls064,tens=D10Calls064;ApplySelf(owner,ability);
        int result=d100+d10;bool success=result>=36,free=result>=96;
        Check(D100Calls064==rolls+1&&D10Calls064==tens+(d10>0?1:0),"064 actual recall executes exactly one d100 and combat-only d10 for "+d100+"+"+d10);
        Check(owner.Resources.GetResourceAmount(resource)==before-(free?0:1),"064 native recall pays exactly one amnesia slot except 96+ for "+d100+"+"+d10);
        Check(book.IsKnown(ordinary)==success&&(!success||book.GetKnownSpell(ordinary).IsTemporary),"064 native temporary knowledge follows exact d100 threshold for "+d100+"+"+d10);
        Check(owner.State.HasCondition(UnitCondition.SpellcastingForbidden)==(result<=10),"064 actual native one-round casting prohibition follows 1-10 only for "+d100+"+"+d10);
        var cooldown=owner.Buffs.Enumerable.SingleOrDefault(b=>b.Blueprint==A<BlueprintBuff>(c,"AmnesiacRecollectionCooldown01464"));
        if(AmnesiacLegacyControl066()||d10==0)Check(cooldown!=null&&cooldown.TimeLeft==TimeSpan.FromHours(1)&&!gate.IsCasterRestrictionPassed(owner)&&cooldown.IsNotDispelable,"066 actual legacy or out-of-combat one-hour nondispellable cooldown rejects another recall across circles");
        else Check(cooldown==null,"066 actual in-combat recollection does not start the out-of-combat cooldown");
        Check(book.GetSpontaneousSlots(1)==slots&&owner.Resources.GetResourceAmount(Pool058(c,owner))==poolBefore,"064 recollection preserves real ordinary spell slots and phrenic points");
        if(success){
            var recalled=book.GetKnownSpell(ordinary);Check(book.Spend(recalled,false)&&book.GetSpontaneousSlots(1)==slots-1,"064 casting recalled knowledge actually spends the ordinary native spellbook slot");
            var source=owner.Buffs.Enumerable.Single(b=>b.Blueprint.GetComponent<AmnesiaSource01464>()!=null);
            Check(source.IsNotDispelable&&source.Blueprint.StayOnDeath&&source.Blueprint.RemoveOnRest,"064 actual recalled source is nondispellable, survives death and expires on native rest");
            var runtime=source.Components.Single(r=>r.SourceBlueprintComponent is AmnesiaSource01464);
            Check(runtime.GetData<AmnesiaSourceData01464>().RefundOnLevelUp==!free,"064 native source data distinguishes paid knowledge from 96+ free recall");
            var other=EquipmentUnit(c,5,Psychic058);EventBus.RaiseEvent<ILevelUpInitiateUIHandler>(h=>h.HandleLevelUpStart(LevelUpConfig.Create(other)));
            Check(source.IsAttached&&book.IsKnown(ordinary),"064 another actor's level-up cannot revoke this owner's recalled knowledge");
            int spent=owner.Resources.GetResourceAmount(resource);EventBus.RaiseEvent<ILevelUpInitiateUIHandler>(h=>h.HandleLevelUpStart(LevelUpConfig.Create(owner)));
            Check(!source.IsAttached&&!book.IsKnown(ordinary)&&owner.Resources.GetResourceAmount(resource)==spent+(free?0:1),"064 owner-specific native level-up clears borrowed knowledge and returns only an actually consumed successful slot");
        }
        foreach(var forbidden in owner.Buffs.Enumerable.Where(b=>b.Blueprint==A<BlueprintBuff>(c,"AmnesiacRecollectionForbidden01464")).ToArray()){
            Game.Instance.Player.GameTime=forbidden.EndTime;owner.Buffs.TickBuff(forbidden,forbidden.EndTime);
            Check(!owner.State.HasCondition(UnitCondition.SpellcastingForbidden),"064 actual native one-round casting prohibition expires at its deadline");
        }
        if(cooldown!=null){Game.Instance.Player.GameTime=cooldown.EndTime;owner.Buffs.TickBuff(cooldown,cooldown.EndTime);
            Check(!cooldown.IsAttached&&gate.IsCasterRestrictionPassed(owner),"066 actual native one-hour cooldown expiry permits an eligible unknown recall again");
        }else Check(!owner.Buffs.HasFact(A<BlueprintBuff>(c,"AmnesiacRecollectionCooldown01464")),"066 combat recollection leaves no out-of-combat cooldown");
    }
}
