using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.FactLogic;
using CallOfTheWild.Wrath.Prototype;

internal static class HedgeWitch01443Tests
{
    static readonly Type Module = typeof(PsychicAmplificationComponent).Assembly.GetType("CallOfTheWild.Wrath.Prototype.HedgeWitch01443", true);
    static int checks, conversions, spends, faults, iconChecks, dispatches;
    static bool cacheFault, definitionFault;
    static string[] Tokens => (string[])AccessTools.Field(Module,"OwnTokens").GetValue(null);
    static string[] CureIds => (string[])AccessTools.Field(Module,"CureIds").GetValue(null);
    static object Get(object o,string n) => AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Set(object o,string n,object v) => AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static object Call(Type t,string m,params object[] args) => AccessTools.Method(t,m).Invoke(null,args);
    static T Empty<T>() where T:class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static BlueprintGuid Id(string t) => (BlueprintGuid)Call(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id",t);
    static T Native<T>(BlueprintsCache c,string id) where T:SimpleBlueprint => (T)c.Load(BlueprintGuid.Parse(id));
    static T Asset<T>(BlueprintsCache c,string t) where T:SimpleBlueprint => (T)c.Load(Id(t));
    static void Check(bool b,string reason) { if(!b)throw new Exception("HedgeWitch01443: "+reason);checks++; }
    static void Register(BlueprintsCache c) => Call(Module,"Register",c);
    static bool Reject(Action action) {try{action();return false;}catch(Exception e){while(e is TargetInvocationException)e=e.InnerException;if(e is InvalidOperationException)return true;throw;}}
    static BlueprintCharacterClass Witch(BlueprintsCache c) => Native<BlueprintCharacterClass>(c,"1b9873f1e7bfe5449bc84d03e9c8e3cc");
    static BlueprintsCache Fresh(BlueprintsCache parent)
    {
        var c=(BlueprintsCache)Call(typeof(TimeStutter01437Tests),"Fresh",parent);
        foreach(string token in Tokens)c.RemoveCachedBlueprint(Id(token));
        AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,c);
        Invoker01437Tests.PrepareNative(c);
        var w=Witch(c);var p=w.Progression;
        Set(w,"m_Archetypes",((BlueprintArchetypeReference[])Get(w,"m_Archetypes")??Array.Empty<BlueprintArchetypeReference>()).Where(x=>x.Guid!=Id(Tokens[0])).ToArray());
        p.UIGroups=(p.UIGroups??Array.Empty<UIGroup>()).Where(g=>!g.Features.Any(x=>x!=null&&Tokens.Skip(1).Select(Id).Contains(x.AssetGuid))).ToArray();
        return c;
    }
    static void CacheFault(BlueprintGuid guid) {if(cacheFault&&guid==Id(Tokens[2])){cacheFault=false;faults++;throw new InvalidOperationException("Injected after final native cache write");}}
    static void DefinitionFault() {if(definitionFault){definitionFault=false;faults++;throw new InvalidOperationException("Injected after parent connection");}}
    static bool Scope(string name) => name=="Witch archetype: Hedge Witch";
    static bool SkipInvestigator(ref bool __result) {__result=false;return false;}
    static void Dispatcher(BlueprintsCache cache,Harmony h)
    {
        var registration=Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration");var investigator=Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.ExtraInvestigatorTalent");var catalogue=Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.ModuleCatalog");
        var states=(Dictionary<string,string>)Call(catalogue,"InitialStates");var before=states.ToDictionary(x=>x.Key,x=>x.Value);int reports=0;
        Action<string,bool,Exception> report=(name,passed,error)=>{Check(name=="Witch archetype: Hedge Witch"&&passed&&error==null,"actual RegisterAll reports successful Hedge Witch action");states[name]="Registered";reports++;};
        h.Patch(AccessTools.Method(registration,"RegisterModule"),prefix:new HarmonyMethod(typeof(HedgeWitch01443Tests),nameof(Scope)));
        h.Patch(AccessTools.Method(investigator,"Register"),prefix:new HarmonyMethod(typeof(HedgeWitch01443Tests),nameof(SkipInvestigator)));
        try {Call(registration,"RegisterAll",cache,report);dispatches++;Check(reports==1&&states["Witch archetype: Hedge Witch"]=="Registered","real dispatcher reaches new catalogue module exactly once");Check(states.Where(x=>x.Key!="Witch archetype: Hedge Witch").All(x=>x.Value==before[x.Key]),"non-target catalogue states remain honest");}
        finally {h.Unpatch(AccessTools.Method(registration,"RegisterModule"),HarmonyPatchType.Prefix,h.Id);h.Unpatch(AccessTools.Method(investigator,"Register"),HarmonyPatchType.Prefix,h.Id);}
    }
    static EntityFactComponent Runtime(BlueprintComponent c,UnitFact fact) {var r=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);r.Setup(c);r.RestoreOwnerLink(fact);return r;}
    static Feature Fact(BlueprintFeature bp,EntityFactsManager manager) {var f=Empty<Feature>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"<Rank>k__BackingField",1);f.RestoreOwnerLink(manager);return f;}
    static void Rules(BlueprintsCache c)
    {
        var f=new Psychic070Tests.Caster(c);var w=Witch(c);var book=f.Book;var d=f.Descriptor;
        Set(book,"Blueprint",w.Spellbook);Check(!book.Blueprint.Spontaneous,"actual Witch prepared book");
        Set(d.Progression,"Classes",new List<ClassData>{new ClassData(w){Level=8}});
        Set(d,"m_Spellbooks",new Dictionary<BlueprintSpellbook,Spellbook>{{book.Blueprint,book}});
        Set(d,"<Blueprint>k__BackingField",new BlueprintUnit{IsCheater=false});
        Set(book,"m_MaxSpellLevel",(int?)10);Set(book,"m_MemorizedSpells",Enumerable.Range(0,10).Select(_=>new List<SpellSlot>()).ToArray());
        Set(book,"m_KnownSpells",Enumerable.Range(0,10).Select(_=>new List<AbilityData>()).ToArray());Set(book,"m_SpecialSpells",Enumerable.Range(0,10).Select(_=>new List<AbilityData>()).ToArray());Set(book,"m_CustomSpells",Enumerable.Range(0,10).Select(_=>new List<AbilityData>()).ToArray());
        Set(book,"m_KnownSpellLevels",new Dictionary<BlueprintAbility,List<int>>());Set(book,"m_SpecialLists",new List<BlueprintSpellList>());
        var lists=new Dictionary<string,BlueprintAbility[]>();Set(book,"m_SpellConversionLists",lists);
        Set(book,"m_HiddenCommonSlots",new Dictionary<int,Dictionary<int,SpellSlot>>());Set(book,"m_HiddenFavoriteSlots",new Dictionary<int,Dictionary<int,SpellSlot>>());
        var intelligence=new ModifiableValueAttributeStat(d.Stats,StatType.Intelligence);Set(intelligence,"m_BaseValue",30);Set(intelligence,"<ModifiedValue>k__BackingField",30);Set(d.Stats,"Intelligence",intelligence);((List<ModifiableValue>)Get(d.Stats,"<AllStats>k__BackingField")).Add(intelligence);
        var manager=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",manager);
        var abilities=new AbilityCollection();abilities.Initialize(manager);Set(f.Unit,"<Abilities>k__BackingField",abilities);
        var component=Asset<BlueprintFeature>(c,Tokens[1]).GetComponent<SpontaneousSpellConversion>();var fact=Fact(component.OwnerBlueprint as BlueprintFeature,manager);var runtime=Runtime(component,fact);
        using(runtime.RequestEventContext())AccessTools.Method(typeof(SpontaneousSpellConversion),"OnTurnOn").Invoke(component,null);
        Check(lists.Count==1,"native OnTurnOn attaches one owned list");
        Check(component.CharacterClass==w,"conversion only attaches to Witch book");
        for(int n=0;n<=9;n++) {
            var bp=new BlueprintAbility{AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),Type=AbilityType.Spell,name="HedgePreparedFixture"+n,ComponentsArray=Array.Empty<BlueprintComponent>()};bp.OnEnable();
            var source=new AbilityData(bp,d){OverrideSpellLevel=n};Set(source,"m_CachedSpellbook",book);Set(source,"m_SpellbookBlueprint",book.Blueprint);((Dictionary<BlueprintAbility,List<int>>)Get(book,"m_KnownSpellLevels"))[bp]=new List<int>{n};
            var slot=new SpellSlot(n,SpellSlotType.Common,0,book);Set(slot,"Spell",source);slot.Available=true;((List<SpellSlot>[])Get(book,"m_MemorizedSpells"))[n].Add(slot);
            var choices=source.GetConversions().ToArray();Check(choices.Length==(n==0?0:1),"actual native conversion menu circle "+n);
            if(n==0)continue;
            var choice=choices.Single();Check(choice.Blueprint.AssetGuid==BlueprintGuid.Parse(CureIds[n]),"exact original CotW cure at circle "+n);
            Check(choice.ConvertedFrom==source&&choice.Spellbook==book,"native conversion retains original book and source slot "+n);
            Check(!book.IsKnown(choice.Blueprint),"cure not added to known spells "+n);
            Check(book.CanSpend(source),"original prepared slot initially available "+n);
            AccessTools.Method(typeof(AbilityData),"SpendOneSpellCharge").Invoke(choice,null);conversions++;spends++;
            Check(!slot.Available&&!book.CanSpend(source),"actual native conversion consumes one prepared slot "+n);
        }
        var independent=new BlueprintAbility[10];independent[1]=Native<BlueprintAbility>(c,CureIds[1]);lists.Add("independent-source",independent);
        using(runtime.RequestEventContext())AccessTools.Method(typeof(SpontaneousSpellConversion),"OnTurnOff").Invoke(component,null);
        Check(lists.Count==1&&ReferenceEquals(lists["independent-source"],independent),"native removal preserves independent conversion source");
        var skill=new ModifiableValueSkill(d.Stats,d.Stats.Wisdom,StatType.SkillLoreReligion);Set(d.Stats,"SkillLoreReligion",skill);((List<ModifiableValue>)Get(d.Stats,"<AllStats>k__BackingField")).Add(skill);
        var empathy=Asset<BlueprintFeature>(c,Tokens[2]);var bonus=empathy.GetComponent<AddStatBonus>();var statFact=Fact(empathy,manager);var sr=Runtime(bonus,statFact);
        skill.BaseValue=1;int before=skill.ModifiedValue;
        using(sr.RequestEventContext())AccessTools.Method(typeof(AddStatBonus),"OnTurnOn").Invoke(bonus,null);
        Check(skill.ModifiedValue==before+3,"native AddStatBonus adds exactly3 Lore Religion; before="+before+" after="+skill.ModifiedValue+" modifiers="+string.Join(",",skill.Modifiers.Select(m=>m.ModValue)));
        using(sr.RequestEventContext())AccessTools.Method(typeof(AddStatBonus),"OnTurnOff").Invoke(bonus,null);
        Check(skill.ModifiedValue==before,"native removal restores skill");
    }
    static void Icons(BlueprintsCache parent)
    {
        var urban=Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.UrbanSkald01443");var c=(BlueprintsCache)Call(typeof(UrbanSkald01443Tests),"Fresh",parent);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,c);
        var ids=(string[])AccessTools.Field(urban,"AllocationIconIds").GetValue(null);var stats=new[]{"Strength","Dexterity","Constitution"};var old=ids.Select(id=>Get(Native<BlueprintActivatableAbility>(c,id),"m_Icon")).ToArray();
        try {
            // Sprite payloads are loaded by Unity in game. These distinct CLR
            // identities test the actual field-copy path; raw native JSON and
            // referenced-asset bundle checks bind them to real shipped icons.
            var sprites=ids.Select(_=>Empty<UnityEngine.Sprite>()).ToArray();
            for(int i=0;i<3;i++)Set(Native<BlueprintActivatableAbility>(c,ids[i]),"m_Icon",sprites[i]);
            Call(urban,"Register",c);
            for(int i=0;i<3;i++)for(int n=0;n<3;n++) {
                var a=Asset<BlueprintActivatableAbility>(c,"UrbanSkaldAllocation"+stats[i]+n+"Ability01442");
                Check(ReferenceEquals(a.Icon,sprites[i]),"allocation gets correct native attribute sprite "+stats[i]+n);iconChecks++;
            }
        } finally {for(int i=0;i<3;i++)Set(Native<BlueprintActivatableAbility>(c,ids[i]),"m_Icon",old[i]);}
        PsychicExpansionTests.Inventory["urbanAllocationIcons01443Contracts"]=JToken.FromObject(new Dictionary<string,object>{{"passed",true},{"toggleCount",iconChecks},{"nativeIconDonors",ids},{"newPrivateResources",0},{"mechanicsUnchanged",true},{"boundary","Actual production field-copy using distinct managed Sprite identities. Native raw blueprint m_Icon GUID/fileid and shipped referenced-asset records audited separately; rendered action bar remains a guide observation step."}});
    }
    internal static int Run(BlueprintsCache parent)
    {
        checks=conversions=spends=faults=iconChecks=dispatches=0;var oldCache=ResourcesLibrary.BlueprintsCache;var h=new Harmony("CotW.HedgeWitch01443.Faults");
        try {
            var c=Fresh(parent);var w=Witch(c);var p=w.Progression;var entries=p.LevelEntries;var originalBook=w.Spellbook;var oldArchetypes=(BlueprintArchetypeReference[])Get(w,"m_Archetypes");var oldGroups=p.UIGroups;
            Register(c);var assets=Tokens.Select(t=>Asset<BlueprintScriptableObject>(c,t)).ToArray();var a=(BlueprintArchetype)assets[0];
            Check(ReferenceEquals(entries,p.LevelEntries)&&ReferenceEquals(originalBook,w.Spellbook),"native progression and spellbook preserved");
            Check(a.GetParentClass()==w&&!a.RemoveSpellbook&&!a.ReplaceClassSkills&&!a.ChangeCasterType,"archetype keeps prepared casting and class skills");
            Check(a.RemoveFeatures.Select(e=>e.Level).SequenceEqual(new[]{4,8})&&a.RemoveFeatures.All(e=>e.Features.Single().AssetGuid.ToString()=="9846043cf51251a4897728ed6e24e76f"),"only4/8 hexes replaced");
            Check(a.AddFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,8})&&a.AddFeatures.SelectMany(e=>e.Features).SequenceEqual(assets.Skip(1)),"original feature milestones");
            Check(((BlueprintArchetypeReference[])Get(w,"m_Archetypes")).Take(oldArchetypes.Length).SequenceEqual(oldArchetypes)&&p.UIGroups.Take(oldGroups.Length).SequenceEqual(oldGroups),"all previous class/UI references remain ordered");
            Register(c);Check(Tokens.Select((t,i)=>ReferenceEquals(Asset<BlueprintScriptableObject>(c,t),assets[i])).All(x=>x),"repeat registration retains identities");
            Check(Tokens.Select(Id).Distinct().Count()==3&&!Tokens.Select(Id).Any(id=>PsychicExpansionTests.NativeRows.Any(row=>(string)row["blueprint"]["AssetId"]==id.ToString())),"three private GUIDs distinct from hydrated native graph");
            Dispatcher(c,h);
            Rules(c);
            var bonus=((BlueprintFeature)assets[2]).GetComponent<AddStatBonus>();bonus.Value=4;Check(Reject(()=>Register(c)),"authored mutation rejected");bonus.Value=3;
            for(int i=0;i<Tokens.Length;i++) {
                var bad=Fresh(parent);var foreign=new BlueprintFeature{AssetGuid=Id(Tokens[i]),name="ForeignHedgeFixture"};bad.AddCachedBlueprint(foreign.AssetGuid,foreign);
                Check(Reject(()=>Register(bad))&&ReferenceEquals(bad.Load(foreign.AssetGuid),foreign),"foreign private GUID preserved "+i);
            }
            h.Patch(AccessTools.Method(typeof(BlueprintsCache),"AddCachedBlueprint"),postfix:new HarmonyMethod(typeof(HedgeWitch01443Tests),nameof(CacheFault)));
            h.Patch(AccessTools.Method(Module,"Definition"),prefix:new HarmonyMethod(typeof(HedgeWitch01443Tests),nameof(DefinitionFault)));
            foreach(bool atCache in new[]{true,false}) {
                var bad=Fresh(parent);var witch=Witch(bad);var refs=Get(witch,"m_Archetypes");var groups=witch.Progression.UIGroups;
                cacheFault=atCache;definitionFault=!atCache;Check(Reject(()=>Register(bad)),"injected publication fault rejected");
                Check(Tokens.All(t=>bad.Load(Id(t))==null)&&ReferenceEquals(Get(witch,"m_Archetypes"),refs)&&ReferenceEquals(witch.Progression.UIGroups,groups),"publication rollback restores exact parents and removes owned partial writes");
                Register(bad);Check(Tokens.All(t=>bad.Load(Id(t))!=null),"registration retries after rollback");
            }
            Icons(parent);
            PsychicExpansionTests.Inventory["hedgeWitch01443Contracts"]=JToken.FromObject(new Dictionary<string,object>{{"passed",true},{"assertions",checks},{"additionalItemCount",1},{"privateResourceCount",3},{"privateIds",Tokens.Select(t=>Id(t).ToString()).ToArray()},{"actualRegisterAllDispatches",dispatches},{"newSpellRootCount",0},{"newHarmonyPatchCount",0},{"newSerializedComponentTypeCount",0},{"nativePreparedConversions",conversions},{"nativePreparedSlotSpends",spends},{"publicationRollbackFaults",faults},{"baseProgressionPreserved",true},{"onlyOwnConversionRemoved",true},{"nativeSkillBonusAddedAndRemoved",true},{"boundary","Real native SpontaneousSpellConversion OnTurnOn/Off, AbilityData.GetConversions/SpendOneSpellCharge, prepared SpellSlot availability and AddStatBonus activation/removal execute. Units/books and prepared-slot lists are managed fixture inputs; full command, healing resolution, rendered UI and full save scene are guide observation steps. No game process launched."}});
            Console.WriteLine("PASS: "+checks+" Hedge Witch and allocation icon contracts.");return checks;
        } finally {h.UnpatchAll(h.Id);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,oldCache);}
    }
}
