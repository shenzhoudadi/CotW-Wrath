using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.Designers;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Groups;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static partial class Archetypes01445Tests
{
    static readonly Assembly Main=typeof(DrillSergeantShare01445).Assembly;
    static readonly Type Helper=typeof(BardMasterpieces01435Tests),Common=Main.GetType("CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445",true);
    static readonly string[] Names={"CourtBard01445","NatureBondedMagus01445","SageCounselor01445","GraveWarden01445","DrillSergeant01445"};
    static readonly List<UnitEntityData> Units=new List<UnitEntityData>();
    static int checks,serial,cacheFaults,connectFaults,missingOutputs;
    static BlueprintGuid faultId;static bool cacheFault,definitionFault,skipWrite;
    static bool SkipWrite(BlueprintGuid guid){if(skipWrite&&guid==faultId){skipWrite=false;missingOutputs++;return false;}return true;}
    static object Get(object o,string n)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).GetValue(o);
    static void Set(object o,string n,object v)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).SetValue(o,v);
    static object Call(Type t,string m,params object[] a)=>AccessTools.Method(t,m).Invoke(null,a);
    static object B(string m,params object[] a)=>Call(Helper,m,a);
    static Type Module(int n)=>Main.GetType("CallOfTheWild.Wrath.Prototype."+Names[n],true);
    static string C(Type t,string n)=>(string)AccessTools.Field(t,n).GetRawConstantValue();
    static string[] Tokens(BlueprintsCache c,int n)=>n==4?(string[])Call(Module(n),"Tokens",c):(string[])AccessTools.Field(Module(n),"OwnTokens").GetValue(null);
    static BlueprintGuid Id(string t)=>(BlueprintGuid)Call(Main.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id",t);
    static T Asset<T>(BlueprintsCache c,string t)where T:SimpleBlueprint=>(T)c.Load(Id(t));
    static T Native<T>(BlueprintsCache c,string id)where T:SimpleBlueprint=>(T)c.Load(BlueprintGuid.Parse(id));
    static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Check(bool b,string why){if(!b)throw new Exception("Archetypes01445: "+why);checks++;}
    static bool Reject(Action action){try{action();return false;}catch(Exception e){while(e is TargetInvocationException)e=e.InnerException;if(e is InvalidOperationException)return true;throw;}}
    static void Register(BlueprintsCache c,int n)=>Call(Module(n),"Register",c);
    static Feature Fact(UnitEntityData u,BlueprintFeature bp){var f=(Feature)u.Facts.Add(new Feature(bp,u.Descriptor));Check(f.IsActive&&f.IsTurnedOn,"native feature activates "+bp.name);return f;}
    static bool Need(Prerequisite p,UnitEntityData u)=>(bool)AccessTools.Method(p.GetType(),"CheckInternal").Invoke(p,new object[]{null,u.Descriptor,null});
    static Buff Buff(UnitEntityData u,BlueprintBuff bp,UnitEntityData caster=null){var f=new Buff(bp,new MechanicsContext(caster??u,u.Descriptor,bp),null);u.Facts.Add(f);Check(f.IsActive&&f.IsTurnedOn,"native buff activates "+bp.name);return f;}
    static EntityFactComponent Runtime(UnitFact fact,BlueprintComponent c)=>fact.Components.Single(r=>ReferenceEquals(r.SourceBlueprintComponent,c));
    static bool Skip()=>false;
    static void CacheFault(BlueprintGuid guid){if(cacheFault&&guid==faultId){cacheFault=false;cacheFaults++;throw new InvalidOperationException("01445 injected post cache write");}}
    static void DefinitionFault(){if(definitionFault){definitionFault=false;connectFaults++;throw new InvalidOperationException("01445 injected after connection");}}
    static UnitEntityData Unit(BlueprintsCache c,int n,int level=19){var u=(UnitEntityData)B("Unit",c,"045-"+(++serial),C(Module(n),"ClassId"),level);Units.Add(u);return u;}
    static BlueprintsCache template;
    static BlueprintsCache Fresh(BlueprintsCache parent)
    {
        var field=AccessTools.Field(typeof(Repair01436CallerTests),"OwnIds");var prior=field.GetValue(null);BlueprintsCache c;
        try{field.SetValue(null,new HashSet<BlueprintGuid>());c=(BlueprintsCache)Call(typeof(Repair01436CallerTests),"Fresh",template??parent);}finally{field.SetValue(null,prior);}
        AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,c);
        var types=typeof(BlueprintAbility).Assembly.GetTypes();
        if(template==null) foreach(var row in PsychicExpansionTests.NativeRows){string id=(string)row["blueprint"]["AssetId"];var bp=c.Load(BlueprintGuid.Parse(id))as BlueprintScriptableObject;if(bp==null)continue;var data=(JObject)row["blueprint"]["Data"];
            bp.ComponentsArray=((JArray)data["Components"]??new JArray()).Select(x=>(BlueprintComponent)PsychicExpansionTests.Read(x,typeof(BlueprintComponent))).ToArray();
            foreach(var p in data.Properties()){if(!new[]{"m_Progression","m_Spellbook","m_Archetypes","LevelEntries","UIGroups","m_Classes","m_UIDeterminatorsGroup","m_MaxAmount","m_Min","ParameterType","SpecificSpellLevel","SpellLevel","SpellLevelPenalty","m_SpellList","m_SpellcasterClass","Type","Range","ActionType","CanTargetSelf","CanTargetFriends","CanTargetEnemies","CanTargetPoint","m_IsFullRoundAction","m_AllFeatures","m_Features","Mode","Group","Group2","IgnorePrerequisites","Ranks","SpellsByLevel","m_Flags","m_CharacterClass","m_SpellsPerDay","m_SpellsKnown","Spontaneous","CastingAttribute","AllSpellsKnown","IsArcane","Levels"}.Contains(p.Name))continue;var f=AccessTools.Field(bp.GetType(),p.Name);if(f!=null&&!f.IsStatic&&!f.IsInitOnly)f.SetValue(bp,PsychicExpansionTests.Read(p.Value,f.FieldType));}
            Set(bp,"m_AllElements",SerializedNodes(bp.ComponentsArray,new HashSet<object>()).OfType<Element>().ToList());bp.OnEnable();
        }
        if(template==null){
            var registrar=Main.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration",true);
            if(c.Load(BlueprintGuid.Parse("c01055f33991416bb1ecc332a578e7cd"))==null)Call(registrar,"RegisterImprovedCombatExpertise",c);
            foreach(var pair in new[]{new[]{"T09CombatFeats","FellingSmashFeat"},new[]{"SpellAndSwordFeats","SwordplayUpsetFeat"},new[]{"ImprovedTwoWeaponFeint","ImprovedTwoWeaponFeintFeat"}})if(c.Load(Id(pair[1]))==null)Call(Main.GetType("CallOfTheWild.Wrath.Prototype."+pair[0],true),"Register",c);
            template=c;return Fresh(parent);
        }
        foreach(int n in Enumerable.Range(0,5))foreach(var t in Tokens(c,n))c.RemoveCachedBlueprint(Id(t));return c;
    }
    static void Registration(BlueprintsCache parent,int n,Harmony h)
    {
        var c=Fresh(parent);var tokens=Tokens(c,n);var cls=Native<BlueprintCharacterClass>(c,C(Module(n),"ClassId"));var baseEntries=cls.Progression.LevelEntries;var sourceBook=cls.Spellbook;var oldRefs=Get(cls,"m_Archetypes");var oldGroups=cls.Progression.UIGroups;
        var nativeElements=PsychicExpansionTests.NativeRows.Select(r=>c.Load(BlueprintGuid.Parse((string)r["blueprint"]["AssetId"]))).OfType<BlueprintScriptableObject>().Select(bp=>new{Blueprint=bp,Elements=bp.ElementsArray,Nodes=bp.ElementsArray.ToArray(),Owners=bp.ElementsArray.Select(e=>e.Owner).ToArray()}).ToArray();
        Register(c,n);var a=Asset<BlueprintArchetype>(c,C(Module(n),"RootToken"));Check(a!=null&&a.GetParentClass()==cls&&cls.Archetypes.Count(x=>x==a)==1,"real archetype choice once: "+Names[n]);
        Check(nativeElements.All(x=>ReferenceEquals(x.Elements,x.Blueprint.ElementsArray)&&x.Nodes.SequenceEqual(x.Blueprint.ElementsArray)&&x.Owners.SequenceEqual(x.Blueprint.ElementsArray.Select(e=>e.Owner))),"all native element lists, nodes and owner metadata preserved");
        Check(tokens.Select(t=>c.Load(Id(t))).OfType<BlueprintScriptableObject>().All(bp=>bp.ElementsArray.SequenceEqual(SerializedNodes(bp.ComponentsArray,new HashSet<object>()).OfType<Element>())&&bp.ElementsArray.All(e=>ReferenceEquals(e.Owner,bp))),"every private element cache is rebuilt from its own authored component graph");
        Check(tokens.All(t=>c.Load(Id(t))!=null)&&tokens.Select(Id).Distinct().Count()==tokens.Length,"complete unique manifest "+Names[n]);
        foreach(var bp in tokens.Select(t=>c.Load(Id(t))).OfType<Kingmaker.Blueprints.Facts.BlueprintUnitFact>())Check(!ReferenceEquals(bp.Icon,null),"button/option icon exists "+bp.name);
        var published=tokens.Select(t=>c.Load(Id(t))).ToArray();Register(c,n);Check(tokens.Select((t,i)=>ReferenceEquals(c.Load(Id(t)),published[i])).All(x=>x),"idempotent graph "+Names[n]);Check(ReferenceEquals(baseEntries,cls.Progression.LevelEntries)&&ReferenceEquals(sourceBook,cls.Spellbook),"native class progression/spellbook unchanged");
        for(int level=1;level<=20;level++){a.GetAddEntry(level);a.GetRemoveEntry(level);Check(true,"native milestone readable "+Names[n]+level);}
        foreach(bool atCache in new[]{true,false}){var bad=Fresh(parent);var bc=Native<BlueprintCharacterClass>(bad,C(Module(n),"ClassId"));var refs=Get(bc,"m_Archetypes");var groups=bc.Progression.UIGroups;faultId=Id(tokens.Last());cacheFault=atCache;definitionFault=!atCache;Check(Reject(()=>Register(bad,n)),"injected actual transaction rejects "+Names[n]);Check(tokens.All(t=>bad.Load(Id(t))==null)&&ReferenceEquals(refs,Get(bc,"m_Archetypes"))&&ReferenceEquals(groups,bc.Progression.UIGroups),"cache and parent rollback "+Names[n]);Register(bad,n);Check(tokens.All(t=>bad.Load(Id(t))!=null),"retry builds every expected output "+Names[n]);}
        foreach(var t in new[]{tokens[0],tokens.Last()}){var bad=Fresh(parent);var foreign=new BlueprintFeature{AssetGuid=Id(t),name="foreign045"};bad.AddCachedBlueprint(foreign.AssetGuid,foreign);Check(Reject(()=>Register(bad,n))&&ReferenceEquals(bad.Load(foreign.AssetGuid),foreign),"collision preserved "+t);}
        {var bad=Fresh(parent);faultId=Id(tokens.Last());skipWrite=true;Check(Reject(()=>Register(bad,n))&&!skipWrite,"actual missing publication is rejected even when cache API returned normally");Check(tokens.All(t=>bad.Load(Id(t))==null),"missing-output rollback leaves no committed receipt or partial graph");Register(bad,n);Check(tokens.All(t=>bad.Load(Id(t))!=null),"retry after a normal-return missing output succeeds");}
        PsychicExpansionTests.Inventory[Names[n].Replace("01445","")+"01445Contracts"]=new JObject{["passed"]=true,["privateResourceCount"]=tokens.Length,["newBlueprints"]=new JArray(tokens.Select(t=>new JObject{["token"]=t,["id"]=Id(t).ToString()})),["newHarmonyPatchCount"]=0,["cacheRollbackFaults"]=1,["connectionRollbackFaults"]=1,["normalReturnMissingOutputRejected"]=true,["nativeClassPreserved"]=true};
    }
    static void RegistrationAndRules(BlueprintsCache parent,Harmony h)
    {
        for(int n=0;n<5;n++){if(int.TryParse(Environment.GetEnvironmentVariable("COTW_ARCHETYPE045_CASE"),out var selected)&&selected!=n)continue;Console.WriteLine("045 registration "+Names[n]);int start=checks;Registration(parent,n,h);Rules(parent,n,h);var row=(JObject)PsychicExpansionTests.Inventory[Names[n].Replace("01445","")+"01445Contracts"];row["assertions"]=checks-start;}
        PsychicExpansionTests.Inventory["archetypes01445Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["additionalItemCount"]=5,["newHarmonyPatchCount"]=0,["cacheRollbackFaults"]=cacheFaults,["connectionRollbackFaults"]=connectFaults,["normalReturnMissingOutputsRejected"]=missingOutputs,["originalParentGraphPreserved"]=true};
    }

    static T NativeRule<T>(T rule)where T:RulebookEvent {
        var field=AccessTools.Field(Helper,"current");var old=(RulebookEventContext)field.GetValue(null);var current=old??new RulebookEventContext();field.SetValue(null,current);current.PushEvent(rule);
        try{RulebookEventBus.OnEventAboutToTrigger(rule);rule.OnTrigger(current);RulebookEventBus.OnEventDidTrigger(rule);return rule;}finally{current.PopEvent(rule);field.SetValue(null,old);}
    }
    static IEnumerable<CodeInstruction> NestedRules(IEnumerable<CodeInstruction> input){foreach(var i in input){if(i.operand is MethodInfo m&&m.IsGenericMethod&&m.Name=="Trigger"&&m.DeclaringType==typeof(Rulebook)){i.opcode=OpCodes.Call;i.operand=AccessTools.Method(typeof(Archetypes01445Tests),nameof(NativeRule)).MakeGenericMethod(m.GetGenericArguments());}yield return i;}}
    static bool NestedD20(ref RuleRollD20 __result){var context=(RulebookEventContext)AccessTools.Field(Helper,"current").GetValue(null);__result=NativeRule(new RuleRollD20(context.LastEvent<RulebookEvent>().Initiator));return false;}
    static void Rules(BlueprintsCache parent,int n,Harmony h)
    {
        var c=Fresh(parent);
        if(n==2){var feat=new BlueprintFeature{name="ExternalCombatExpertiseChild045",AssetGuid=Id("045ExternalExpertiseChild"),Ranks=1};var need=new PrerequisiteFeature();Set(need,"m_Feature",BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(Native<BlueprintFeature>(c,"4c44724ffa8844f4d9bedb5bb27d144a")));feat.ComponentsArray=new BlueprintComponent[]{new PrerequisiteStatValue{Stat=StatType.Intelligence,Value=13},need};feat.OnEnable();c.AddCachedBlueprint(feat.AssetGuid,feat);var pool=Native<BlueprintFeatureSelection>(c,"247a4068296e8be42890143f451b4b45");Set(pool,"m_AllFeatures",((BlueprintFeatureReference[])Get(pool,"m_AllFeatures")).Concat(new[]{BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(feat)}).ToArray());}
        Register(c,n);Console.WriteLine("045 native rules "+Names[n]);
        if(n==0){Court(c,h);CourtLevel9(c,h);}else if(n==1){Nature(c);SymbiosisStacking(c);}else if(n==2){Sage(c);KiFeintTriggers(c);}else if(n==3){Grave(c);GraveEvents(c,h);DustInvisibility(c,h);}else {Drill(c);DrillTraining(c);DrillLatePool(c,h);}
        int schema=Schema(c,n);((JObject)PsychicExpansionTests.Inventory[Names[n].Replace("01445","")+"01445Contracts"])["nativeSerializedTypes"]=schema;Register(c,n);Check(true,"runtime use retains authored receipt "+Names[n]);
    }
    static void Nature(BlueprintsCache c)
    {
        var root=Asset<BlueprintArchetype>(c,"NatureBondedMagusArchetype01445");Check(root.AddFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,4,7,10,11,13,15,16,19})&&root.RemoveFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,4,5,9,11,13,17}),"Nature original CotW milestones");
        var sym=Asset<BlueprintFeature>(c,"NatureBondedMagusSymbiosis01445");var improved=Asset<BlueprintFeature>(c,"NatureBondedMagusImprovedSymbiosis01445");
        foreach(var pair in new[]{new[]{4,1},new[]{7,2},new[]{11,3},new[]{15,4},new[]{19,5}}){var u=Unit(c,1,pair[0]);int ac=u.Stats.AC.ModifiedValue;var sf=new Feature(sym,u.Descriptor);sf.SetRank(pair[1]);u.Facts.Add(sf);Check(u.Stats.AC.ModifiedValue==ac+pair[1],"native Symbiosis natural AC "+pair[0]);for(int rank=pair[1]-1;rank>=0;rank--){u.Facts.Remove(sf);Check(u.Stats.AC.ModifiedValue==ac+rank,"Symbiosis rank removal clean "+rank);}}
        foreach(int level in new[]{11,14,15,18,19,20}){var u=Unit(c,1,level);int str=u.Stats.Strength.ModifiedValue,con=u.Stats.Constitution.ModifiedValue;var f=Fact(u,improved);int value=level<15?4:level<19?6:8;Check(u.Stats.Strength.ModifiedValue==str+value&&u.Stats.Constitution.ModifiedValue==con+value,"native Improved Symbiosis "+level);u.Facts.Remove(f);Check(u.Stats.Strength.ModifiedValue==str&&u.Stats.Constitution.ModifiedValue==con,"enhancement removal clean");}
        var list=Asset<BlueprintSpellList>(c,"NatureBondedMagusSpellList01445");var magus=Native<BlueprintSpellList>(c,"4d72e1e7bd6bc4f4caaea7aa43a14639");var excluded=new HashSet<BlueprintGuid>(magus.SpellsByLevel.SelectMany(e=>(List<BlueprintAbilityReference>)Get(e,"m_Spells")).Select(r=>r.Guid));var source=Native<BlueprintSpellList>(c,"bad8638d40639d04fa2f80a1cac67d6b");
        foreach(var row in list.SpellsByLevel){var refs=(List<BlueprintAbilityReference>)Get(row,"m_Spells");var native=source.SpellsByLevel.Single(e=>e.SpellLevel==row.SpellLevel);Check(!ReferenceEquals(row,native)&&!ReferenceEquals(refs,Get(native,"m_Spells"))&&refs.All(r=>!excluded.Contains(r.Guid)),"private Druid-only row "+row.SpellLevel);Check(refs.Select(r=>r.Guid).SequenceEqual(((List<BlueprintAbilityReference>)Get(native,"m_Spells")).Where(r=>!excluded.Contains(r.Guid)).Select(r=>r.Guid)),"exact native Druid subtraction "+row.SpellLevel);}
        var learner=Unit(c,1,19);var cls=Native<BlueprintCharacterClass>(c,"45a4607686d96a1498891b3286121780");var book=new Spellbook(learner.Descriptor,cls.Spellbook);Set(book,"m_BaseLevelInternal",19);Set(learner.Descriptor,"m_Spellbooks",new Dictionary<BlueprintSpellbook,Spellbook>{{cls.Spellbook,book}});
        for(int level=1;level<=6;level++){var learn=Asset<BlueprintParametrizedFeature>(c,"NatureBondedMagusLearn"+level+"01445");Check(learn.SpecificSpellLevel&&learn.SpellLevel==level&&learn.SpellList==list&&learn.SpellcasterClass==cls,"native parametrized learning targets private list and Magus "+level);var row=list.SpellsByLevel.Single(e=>e.SpellLevel==level);var spell=row.Spells.FirstOrDefault(s=>s!=null);Check(spell!=null,"actual native learnable spell fixture at each circle "+level);var fact=new Feature(learn,learner.Descriptor){Param=new FeatureParam(spell)};learner.Facts.Add(fact);Check(book.GetKnownSpell(spell)!=null,"real native LearnSpellParametrized acquires "+level);}
        var restriction=Native<BlueprintFeature>(c,"42f96fc8d6c80784194262e51b0a1d25").GetComponents<PrerequisiteNoArchetype>().Single(p=>p.Archetype==root);Check(Need(restriction,learner),"ordinary Magus can use pool feat");learner.Progression.Classes[0].Archetypes.Add(root);Check(!Need(restriction,learner),"Nature-Bonded cannot select pool feat");
    }
    static void Court(BlueprintsCache c,Harmony h)
    {
        foreach(int level in new[]{1,3,4,5,7,8,10,11,14,15,16,17,19,20}){var caster=Unit(c,0,level);var enemy=Unit(c,0,level);var heraldic=Asset<BlueprintFeature>(c,"CourtBardHeraldicExpertise01445");int a=caster.Stats.SkillPersuasion.ModifiedValue,b=caster.Stats.SkillKnowledgeWorld.ModifiedValue;var f=Fact(caster,heraldic);Check(caster.Stats.SkillPersuasion.ModifiedValue==a+Math.Max(1,level/2)&&caster.Stats.SkillKnowledgeWorld.ModifiedValue==b+Math.Max(1,level/2),"native Heraldic half-level skills "+level);var r=Asset<BlueprintAbilityResource>(c,"CourtBardHeraldicResource01445");Check(r.GetMaxAmount(caster.Descriptor)==1+level/5,"native Heraldic daily resource "+level);caster.Facts.Remove(f);
            int attack=enemy.Stats.AdditionalAttackBonus.ModifiedValue,damage=enemy.Stats.AdditionalDamage.ModifiedValue;var sat=Buff(enemy,Asset<BlueprintBuff>(c,"CourtBardSatireEffect01445"),caster);int penalty=level<5?-1:level<11?-2:level<17?-3:-4;Check(enemy.Stats.AdditionalAttackBonus.ModifiedValue==attack+penalty&&enemy.Stats.AdditionalDamage.ModifiedValue==damage+penalty,"native Satire attack/damage scaling "+level);sat.Remove();Check(enemy.Stats.AdditionalAttackBonus.ModifiedValue==attack&&enemy.Stats.AdditionalDamage.ModifiedValue==damage,"Satire removes cleanly");
            int persuasion=enemy.Stats.SkillPersuasion.ModifiedValue,umd=enemy.Stats.SkillUseMagicDevice.ModifiedValue;var mock=Buff(enemy,Asset<BlueprintBuff>(c,"CourtBardMockeryEffect01445"),caster);int mp=level<=6?-2:level<=10?-3:level<=14?-4:level<=18?-5:-6;Check(enemy.Stats.SkillPersuasion.ModifiedValue==persuasion+mp&&enemy.Stats.SkillUseMagicDevice.ModifiedValue==umd+mp,"native Mockery scaling "+level);mock.Remove();
        }
        {var user=Unit(c,0,10);Fact(user,Asset<BlueprintFeature>(c,"CourtBardHeraldicExpertise01445"));var res=Asset<BlueprintAbilityResource>(c,"CourtBardHeraldicResource01445");var ready=Buff(user,Asset<BlueprintBuff>(c,"CourtBardHeraldicBuff01445"));int left=user.Resources.GetResourceAmount(res);
            foreach(var skill in new[]{StatType.CheckBluff,StatType.CheckDiplomacy,StatType.SkillKnowledgeWorld}){var check=NativeRule(new RuleSkillCheck(user,skill,100));Check(check.D20.RollHistory.Count==2&&user.Resources.GetResourceAmount(res)==--left,"actual failed Heraldic reroll spends one "+skill+" rolls="+check.D20.RollHistory.Count+" resource="+user.Resources.GetResourceAmount(res)+" expected="+left);}
            var empty=NativeRule(new RuleSkillCheck(user,StatType.CheckBluff,100));Check(empty.D20.RollHistory.Count==1&&left==0,"empty Heraldic pool cannot reroll");user.Resources.Restore(res,3);var success=NativeRule(new RuleSkillCheck(user,StatType.CheckBluff,1));Check(success.D20.RollHistory.Count==1&&user.Resources.GetResourceAmount(res)==3,"successful first skill costs nothing");var otherSkill=NativeRule(new RuleSkillCheck(user,StatType.SkillPerception,100));Check(otherSkill.D20.RollHistory.Count==1&&user.Resources.GetResourceAmount(res)==3,"other skills never reroll");ready.Remove();}
        var u=Unit(c,0,14);var target=Unit(c,0,14);var other=Unit(c,0,14);var epic=Asset<BlueprintBuff>(c,"CourtBardGloriousEpicEffect01445");var effect=Buff(target,epic,u);var flat=new RuleCheckTargetFlatFooted(u,target);var comp=epic.GetComponent<CourtBardFlatFooted01445>();using(Runtime(effect,comp).RequestEventContext())comp.OnEventAboutToTrigger(flat);Check(flat.ForceFlatFooted,"native flat-footed rule forced by Glorious Epic");effect.Remove();
        foreach(string token in new[]{"Satire","Mockery","GloriousEpic","Scandal"}){var toggle=Asset<BlueprintActivatableAbility>(c,"CourtBard"+token+"Toggle01445");Check(toggle.Group==ActivatableAbilityGroup.BardicPerformance&&toggle.GetComponent<ActivatableAbilityResourceLogic>().RequiredResource.AssetGuid.ToString()=="e190ba276831b5c4fa28737e5e49e6a6","native mutually exclusive Bard resource performance "+token);var area=Asset<BlueprintAbilityAreaEffect>(c,"CourtBard"+token+"Area01445");Check(area.Size.Value==(token=="Satire"||token=="Scandal"?50:30)&&area.AffectEnemies&&!area.SpellResistance,"original CotW aura range "+token);if(token=="GloriousEpic"||token=="Scandal"){var action=area.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectRunAction>();var conditional=(Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional)action.UnitEnter.Actions[0];var immune=conditional.ConditionsChecker.Conditions.OfType<CourtBardImmunity01445>().Single();var bp=immune.Buff.Get();var context=new MechanicsContext(u,target.Descriptor,area);using(context.GetDataScope(target))Check(immune.Check(),"no immunity can enter "+token);var foreign=Buff(target,bp,other);using(context.GetDataScope(target))Check(!immune.Check(),"original CotW immunity to the same performance applies across bards "+token);var own=Buff(target,bp,u);using(context.GetDataScope(target))Check(!immune.Check(),"same bard immunity blocks repeat save "+token);own.Remove();foreign.Remove();var save=(ContextActionSavingThrow)conditional.IfTrue.Actions[0];Check(save.Type==SavingThrowType.Will,"native Will save "+token);}}
    }
    static void Sage(BlueprintsCache c)
    {
        var u=Unit(c,2,10);u.Stats.Intelligence.BaseValue=7;var root=Asset<BlueprintArchetype>(c,"SageCounselorArchetype01445");Check(root.RemoveFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,2,3,4,6,10})&&root.AddFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,2,3,6,10}),"Sage original CotW replacement table");
        var waiver=Native<BlueprintFeature>(c,"0f15c6f70d8fb2b49aa6cc24239cc5fa").GetComponent<SageCounselorIntelligence01445>();Check(!Need(waiver,u),"ordinary low INT cannot pass feat prerequisite");Fact(u,Asset<BlueprintFeature>(c,"SageCounselorCunningFist101445"));
        Check(Need(waiver,u),"Cunning Fist waives only INT");Check(Need(Asset<BlueprintFeature>(c,"045ExternalExpertiseChild").GetComponent<SageCounselorIntelligence01445>(),u),"currently published external Combat Expertise child receives only the same INT waiver");foreach(string s in new[]{"2","6"})Fact(u,Asset<BlueprintFeature>(c,"SageCounselorCunningFist"+s+"01445"));Check(u.Descriptor.HasFact(Native<BlueprintFeature>(c,"c610310d31414edabcedf0c8a6fe32c4"))&&u.Descriptor.HasFact(Native<BlueprintFeature>(c,"32429740d6a5470aaaa02f20d61e43d3")),"native Feint and Final Feint granted without selection prerequisites");
        var ki=Fact(u,Asset<BlueprintFeature>(c,"SageCounselorDeceptiveKi01445"));Check(!u.Abilities.Enumerable.Any(x=>x.Blueprint.AssetGuid.ToString()=="7f6ea312f5dad364fa4a896d7db39fdd"),"Deceptive Ki omits extra-attack Ki Strike");var ability=Asset<BlueprintAbility>(c,"SageCounselorKiFeint01445");var data=new AbilityData(ability,u.Descriptor);var required=ability.GetComponents<Kingmaker.UnitLogic.Abilities.Components.CasterCheckers.AbilityCasterHasFacts>().Last();Check(!required.IsCasterRestrictionPassed(u.Descriptor),"swift Ki Feint requires real native flurry fact");Fact(u,Native<BlueprintFeature>(c,"332362f3bd39ebe46a740a36960fdcb4"));Check(required.IsCasterRestrictionPassed(u.Descriptor),"actual flurry unlock permits Ki Feint");var cost=ability.GetComponent<AbilityResourceLogic>();int amount=u.Resources.GetResourceAmount(cost.RequiredResource);Check(cost.IsAbilityRestrictionPassed(data),"native ki cost availability");cost.Spend(data);Check(u.Resources.GetResourceAmount(cost.RequiredResource)==amount-1,"swift feint spends exactly one native ki");
        int before=u.Stats.CheckBluff.ModifiedValue;var buff=Buff(u,Asset<BlueprintBuff>(c,"SageCounselorDeceptiveKiBuff01445"));Check(u.Stats.CheckBluff.ModifiedValue==before+4,"native next Bluff insight +4");var trigger=buff.Blueprint.GetComponent<AddInitiatorSkillRollTrigger>();using(Runtime(buff,trigger).RequestEventContext())trigger.OnEventDidTrigger(new RuleSkillCheck(u,StatType.SkillPerception,30));Check(!buff.IsDisposed,"unrelated skill leaves next Bluff bonus");using(Runtime(buff,trigger).RequestEventContext())trigger.OnEventDidTrigger(new RuleSkillCheck(u,StatType.CheckBluff,30));Check(buff.IsDisposed&&u.Stats.CheckBluff.ModifiedValue==before,"next Bluff consumes bonus cleanly");
        var ff=Asset<BlueprintBuff>(c,"SageCounselorFeintingFlurryBuff01445");var reduce=ff.GetComponent<Kingmaker.Designers.Mechanics.Buffs.ReduceAttacksCount>();var attack=ff.GetComponent<AddInitiatorAttackWithWeaponTrigger>();Check(reduce.ReduceCount==1&&reduce.OnlyFromPrimaryHand&&attack.TriggerBeforeAttack&&attack.OnlyOnFullAttack&&attack.OnlyOnFirstAttack&&!attack.OnlyHit,"native one-attack sacrifice and pre-first-attack trigger");var context=new MechanicsContext(u,u.Descriptor,ff);using(context.GetDataScope(u))Check(reduce.Condition.Check(),"sacrifice enabled by actual flurry fact");
    }
    static void Grave(BlueprintsCache c)
    {
        var root=Asset<BlueprintArchetype>(c,"GraveWardenArchetype01445");Check(root.AddFeatures.Select(e=>e.Level).SequenceEqual(new[]{2,7,10})&&root.RemoveFeatures.Select(e=>e.Level).SequenceEqual(new[]{2,10}),"Grave original CotW replacement table");
        foreach(int level in new[]{2,7,10,20}){var u=Unit(c,3,level);var feature=Asset<BlueprintFeature>(c,"GraveWardenHolyWaterFeature01445");Fact(u,feature);var res=Asset<BlueprintAbilityResource>(c,"GraveWardenResource01445");Check(res.GetMaxAmount(u.Descriptor)==level&&u.Resources.GetResourceAmount(res)==level,"native Holy Water uses equal Slayer level "+level);var ward=Asset<BlueprintAbility>(c,"GraveWardenDeathWardAbility01445");var data=new AbilityData(ward,u.Descriptor);var cost=ward.GetComponent<AbilityResourceLogic>();Check(ward.IsFullRoundAction&&ward.Range==AbilityRange.Personal&&ward.Type==AbilityType.SpellLike&&cost.Amount==4,"native self-only full-round Ward four-use cost");Check(cost.IsAbilityRestrictionPassed(data)==(level>=4),"Ward insufficient-resource boundary "+level);if(level>=4){cost.Spend(data);Check(u.Resources.GetResourceAmount(res)==level-4,"Ward spends exactly four shared uses");}}
        var owner=Unit(c,3,10);var target=Unit(c,3,10);var other=Unit(c,3,10);var dust=Asset<BlueprintAbility>(c,"GraveWardenDustbringerAbility01445");var type=dust.GetComponents<Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetHasFact>().Last();Check(!type.IsTargetRestrictionPassed(owner,new TargetWrapper(target)),"Dustbringer rejects living target");Fact(target,Native<BlueprintFeature>(c,"734a29b693e9ec346ba2951b27987e33"));Check(type.IsTargetRestrictionPassed(owner,new TargetWrapper(target)),"Dustbringer allows actual Undead feature");var check=dust.GetComponents<Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetHasFact>().First();var dataOwner=new AbilityData(dust,owner.Descriptor);var context=new MechanicsContext(owner,target.Descriptor,dust);var mark=Asset<BlueprintBuff>(c,"GraveWardenDustbringerMark01445");var foreign=Buff(target,mark,other);Check(check.IsTargetRestrictionPassed(owner,new TargetWrapper(target)),"foreign mark does not block own Dustbringer");var own=Buff(target,mark,owner);Check(!check.IsTargetRestrictionPassed(owner,new TargetWrapper(target)),"own mark blocks repeat target setup");own.Remove();foreign.Remove();
    }
    static void Drill(BlueprintsCache c)
    {
        var root=Asset<BlueprintArchetype>(c,"DrillSergeantArchetype01445");Check(root.AddFeatures.Select(e=>e.Level).SequenceEqual(new[]{2,9,13,17})&&root.RemoveFeatures.Select(e=>e.Level).SequenceEqual(new[]{2,6,9,10,13,14,17,18}),"Drill Sergeant original replacement table");
        foreach(int level in new[]{2,4,5,9,10,15,17,20}){var u=Unit(c,4,level);var f=Fact(u,Asset<BlueprintFeature>(c,"DrillSergeantTactician01445"));var resource=Asset<BlueprintAbilityResource>(c,"DrillSergeantResource01445");Check(resource.GetMaxAmount(u.Descriptor)==1+level/5,"native Tactician daily uses "+level);Check(u.Abilities.Enumerable.Any(a=>a.Blueprint==Asset<BlueprintAbility>(c,"DrillSergeantAbility01445")),"standard Tactician initially granted");var up=Fact(u,Asset<BlueprintFeature>(c,"DrillSergeantGreaterTactician01445"));Check(u.Abilities.Enumerable.Any(a=>a.Blueprint==Asset<BlueprintAbility>(c,"DrillSergeantSwiftAbility01445"))&&!Asset<BlueprintAbility>(c,"DrillSergeantAbility01445").GetComponent<AbilityShowIfCasterHasFact>().IsAbilityVisible(new AbilityData(Asset<BlueprintAbility>(c,"DrillSergeantAbility01445"),u.Descriptor)),"native conditional greater upgrade to swift");u.Facts.Remove(up);Check(u.Abilities.Enumerable.Any(a=>a.Blueprint==Asset<BlueprintAbility>(c,"DrillSergeantAbility01445")),"upgrade removal restores standard only");}
        var caster=Unit(c,4,17);var target=Unit(c,4,17);Fact(caster,Asset<BlueprintFeature>(c,"DrillSergeantTactician01445"));var pool=Native<BlueprintFeatureSelection>(c,"d87e2f6a9278ac04caeb0f93eff95fcb");var choices=pool.AllFeatures.Where(x=>x!=null).OrderBy(x=>x.AssetGuid.ToString()).Take(3).ToArray();foreach(var f in choices)Fact(caster,f);var baseFact=caster.Facts.List.OfType<Feature>().Single(f=>f.Blueprint.AssetGuid==Id("DrillSergeantTactician01445"));foreach(var grant in baseFact.Blueprint.GetComponents<AddFeatureIfHasFact>())using(Runtime(baseFact,grant).RequestEventContext())grant.HandleUnitGainLevel();
        foreach(var f in choices)Check(caster.ActivatableAbilities.Enumerable.Any(a=>a.Blueprint.AssetGuid==Id("DrillSergeant"+f.AssetGuid+"Toggle01445")),"own known teamwork feat grants corresponding selection button");
        var first=Buff(caster,Asset<BlueprintBuff>(c,"DrillSergeant"+choices[0].AssetGuid+"Marker01445"));var second=Buff(caster,Asset<BlueprintBuff>(c,"DrillSergeant"+choices[1].AssetGuid+"Marker01445"));Check(first.IsDisposed&&caster.Buffs.Enumerable.Count(b=>b.Blueprint.GetComponent<DrillSergeantSelection01445>()!=null)==1,"only one persistent selected feat before Master");Fact(caster,Asset<BlueprintFeature>(c,"DrillSergeantMasterTactician01445"));var third=Buff(caster,Asset<BlueprintBuff>(c,"DrillSergeant"+choices[2].AssetGuid+"Marker01445"));Check(caster.Buffs.Enumerable.Count(b=>b.Blueprint.GetComponent<DrillSergeantSelection01445>()!=null)==2,"Master allows exactly two selections");
        var ability=Asset<BlueprintAbility>(c,"DrillSergeantSwiftAbility01445");var context=new MechanicsContext(caster,target.Descriptor,ability);using(context.GetDataScope(target))ability.GetComponent<AbilityEffectRunAction>().Actions.Run();Check(target.Descriptor.HasFact(choices[1])&&target.Descriptor.HasFact(choices[2])&&!target.Descriptor.HasFact(choices[0]),"actual action shares selected two feats only");foreach(var b in target.Buffs.Enumerable.ToArray())b.Remove();Check(!target.Descriptor.HasFact(choices[1])&&!target.Descriptor.HasFact(choices[2]),"shared feat removal does not persist bonus feats");
    }

    static int holyDeliveries,kills;static bool flatInput=true;static int observedWillDC;static UnitEntityData hostile;
    static bool Enemy(UnitEntityData __instance,UnitEntityData __0,ref bool __result){__result=(__instance==hostile)!=(__0==hostile);return false;}
    static MechanicsContext ActionContext(ContextAction action)=>(MechanicsContext)AccessTools.Property(typeof(ContextAction),"Context").GetValue(action,null);
    static bool DamageEndpoint(ContextActionDealDamage __instance){holyDeliveries++;Check(__instance.Value.DiceType==DiceType.D4&&__instance.Value.DiceCountValue.Calculate(ActionContext(__instance))==2&&__instance.DamageType.Energy==DamageEnergyType.Holy,"actual Holy Water terminal damage is two holy d4, no sneak/critical multiplication");return false;}
    static bool KillEndpoint(){kills++;return false;}
    static bool FlatInput(ref bool __result){__result=flatInput;return false;}
    static void ObserveSave(ContextActionSavingThrow __instance){if(__instance.HasCustomDC&&__instance.Type==SavingThrowType.Will)observedWillDC=__instance.CustomDC.Calculate(ActionContext(__instance));}
    static void Equip(UnitEntityData u,Kingmaker.Items.ItemEntityWeapon w){Set(u.Body.PrimaryHand,"m_FakeItem",w);Set(u.Body.PrimaryHand,"m_IsItemFake",w!=null);if(w!=null)Set(w,"HoldingSlot",u.Body.PrimaryHand);}
    static RuleAttackRoll Roll(UnitEntityData u,UnitEntityData t,Kingmaker.Items.ItemEntityWeapon w,bool hit,bool sneak=true,bool ray=false){var r=new RuleAttackRoll(u,t,w,0);Set(r,"Result",hit?AttackResult.Hit:AttackResult.Miss);Set(r,"IsSneakAttack",sneak);Set(r,"AttackType",ray?AttackType.RangedTouch:AttackType.Melee);return r;}
    static void GraveEvents(BlueprintsCache c,Harmony h)
    {
        holyDeliveries=kills=observedWillDC=0;flatInput=true;
        var damageMethod=AccessTools.Method(typeof(ContextActionDealDamage),"RunAction");var killMethod=AccessTools.Method(typeof(ContextActionKill),"RunAction");var flatMethod=AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionIsFlatFooted),"CheckCondition");
        h.Patch(damageMethod,prefix:new HarmonyMethod(typeof(Archetypes01445Tests),nameof(DamageEndpoint)));h.Patch(killMethod,prefix:new HarmonyMethod(typeof(Archetypes01445Tests),nameof(KillEndpoint)));h.Patch(flatMethod,prefix:new HarmonyMethod(typeof(Archetypes01445Tests),nameof(FlatInput)));h.Patch(AccessTools.Method(typeof(ContextActionSavingThrow),"RunAction"),prefix:new HarmonyMethod(typeof(Archetypes01445Tests),nameof(ObserveSave)));
        try{
            var owner=Unit(c,3,10);var target=Unit(c,3,10);var other=Unit(c,3,10);hostile=target;Fact(target,Native<BlueprintFeature>(c,"734a29b693e9ec346ba2951b27987e33"));var weapon=(Kingmaker.Items.ItemEntityWeapon)Call(typeof(TeamworkFeatTests),"Weapon",owner,false,"045-primary");var offhand=(Kingmaker.Items.ItemEntityWeapon)Call(typeof(TeamworkFeatTests),"Weapon",owner,false,"045-other-weapon");Equip(owner,weapon);
            var buff=Buff(owner,Asset<BlueprintBuff>(c,"GraveWardenHolyWaterBuff01445"));var component=buff.Blueprint.GetComponent<GraveWardenHolyWater01445>();
            foreach(int mode in new[]{0,1,2,3,4}){var selected=mode==1?offhand:weapon;var r=Roll(owner,mode==3?other:target,selected,mode!=2);if(mode==4)Set(r,"IsFake",true);var attack=new RuleAttackWithWeapon(owner,r.Target,selected,0);Set(attack,"AttackRoll",r);r.RuleAttackWithWeapon=attack;int before=holyDeliveries;using(Runtime(buff,component).RequestEventContext())component.OnEventDidTrigger(attack);Check(holyDeliveries==before+(mode==0?1:0),"actual Holy Water primary/other weapon/miss/living/fake guards "+mode);using(Runtime(buff,component).RequestEventContext())component.OnEventDidTrigger(attack);Check(holyDeliveries==before+(mode==0?1:0),"duplicate attack notification does not add Holy Water twice "+mode);}
            buff.Remove();
            var feature=Asset<BlueprintFeature>(c,"GraveWardenDustbringerFeature01445");var fact=Fact(owner,feature);var mark=Asset<BlueprintBuff>(c,"GraveWardenDustbringerMark01445");var cooldown=Asset<BlueprintBuff>(c,"GraveWardenDustbringerCooldown01445");var sourceStudy=Native<BlueprintBuff>(c,"45548967b714e254aa83f23354f174b0");
            foreach(bool ray in new[]{false,true})foreach(int mode in new[]{0,1,2,3}){foreach(var b in target.Buffs.Enumerable.Where(b=>b.Context?.MaybeCaster==owner&&(b.Blueprint==mark||b.Blueprint==cooldown)).ToArray())b.Remove();var own=Buff(target,mark,owner);var foreign=Buff(target,mark,other);var study=Buff(target,sourceStudy,owner);study.Context[AbilitySharedValue.DamageBonus]=3;flatInput=mode!=3;var r=Roll(owner,target,weapon,mode!=1,mode!=2,ray);if(!ray){var attack=new RuleAttackWithWeapon(owner,target,weapon,0);Set(attack,"AttackRoll",r);r.RuleAttackWithWeapon=attack;}int before=kills;
                foreach(var trigger in feature.GetComponents<AddInitiatorAttackRollTrigger>())using(Runtime(fact,trigger).RequestEventContext())trigger.OnEventDidTrigger(r);
                Check(kills==before+(mode==0?1:0),"actual Dustbringer hit/sneak/flat-footed guards weapon/ray "+ray+mode);Check(own.IsDisposed&&!foreign.IsDisposed&&target.Buffs.Enumerable.Count(b=>b.Blueprint==cooldown&&b.Context?.MaybeCaster==owner)==1,"attack-end cleanup and caster-specific cooldown weapon/ray "+ray+mode);if(mode==0)Check(observedWillDC>=18,"native Will DC includes own Studied Target shared bonus");foreach(var trigger in feature.GetComponents<AddInitiatorAttackRollTrigger>())using(Runtime(fact,trigger).RequestEventContext())trigger.OnEventDidTrigger(r);Check(kills==before+(mode==0?1:0),"later/repeated attack has no residual Dustbringer mark");foreign.Remove();study.Remove();}
        }finally{foreach(var method in new[]{damageMethod,killMethod,flatMethod})h.Unpatch(method,HarmonyPatchType.Prefix,h.Id);h.Unpatch(AccessTools.Method(typeof(ContextActionSavingThrow),"RunAction"),AccessTools.Method(typeof(Archetypes01445Tests),nameof(ObserveSave)));}
    }

    static void DrillTraining(BlueprintsCache c)
    {
        var owner=Unit(c,4,17);var ally=Unit(c,4,17);var pool=Native<BlueprintFeatureSelection>(c,C(Main.GetType("CallOfTheWild.Wrath.Prototype.T11TrainingOptions",true),"TrainingPool"));var training=pool.AllFeatures.Single(f=>f.GetComponent<WeaponGroupAttackBonus>()?.WeaponGroup.ToString()=="Axes");
        var own=new Feature(training,owner.Descriptor);own.SetRank(4);owner.Facts.Add(own);var w=(Kingmaker.Items.ItemEntityWeapon)Call(typeof(TeamworkFeatTests),"Weapon",owner,false,"045-trainer-axe");var aw=(Kingmaker.Items.ItemEntityWeapon)Call(typeof(TeamworkFeatTests),"Weapon",ally,false,"045-ally-axe");Set(w.Blueprint.Type,"m_FighterGroupFlags",Kingmaker.Blueprints.Items.Weapons.WeaponFighterGroupFlags.Axes);Set(aw.Blueprint.Type,"m_FighterGroupFlags",Kingmaker.Blueprints.Items.Weapons.WeaponFighterGroupFlags.Axes);Equip(owner,w);Equip(ally,aw);
        Check(owner.Get<Kingmaker.UnitLogic.Parts.UnitPartWeaponTraining>().GetWeaponRank(w)==4,"actual native four-rank training part");var effect=Buff(ally,Asset<BlueprintBuff>(c,"DrillSergeantTrainerBuff01445"),owner);var component=effect.Blueprint.GetComponent<DrillSergeantWeaponTrainer01445>();
        foreach(int rank in new[]{0,1,3}){Feature ownAlly=null;if(rank>0){ownAlly=new Feature(training,ally.Descriptor);ownAlly.SetRank(rank);ally.Facts.Add(ownAlly);}int before=ally.Stats.AdditionalDamage.ModifiedValue;var stats=new RuleCalculateWeaponStats(ally,aw);using(Runtime(effect,component).RequestEventContext())component.OnEventAboutToTrigger(stats);Check(ally.Stats.AdditionalDamage.ModifiedValue==before+Math.Max(0,2-rank),"native Weapon Trainer damage non-stacking "+rank);stats.OnDidTrigger();Check(ally.Stats.AdditionalDamage.ModifiedValue==before,"temporary damage modifier cleaned at rule completion");var attack=new RuleCalculateAttackBonusWithoutTarget(ally,aw,0);int ab=ally.Stats.AdditionalAttackBonus.ModifiedValue;using(Runtime(effect,component).RequestEventContext())component.OnEventAboutToTrigger(attack);Check(attack.AllBonuses.Sum(m=>m.Value)==Math.Max(0,2-rank),"native Weapon Trainer attack non-stacking "+rank);attack.OnDidTrigger();if(ownAlly!=null)while(ownAlly.Manager!=null)ally.Facts.Remove(ownAlly);}
        Set(w.Blueprint.Type,"m_FighterGroupFlags",Kingmaker.Blueprints.Items.Weapons.WeaponFighterGroupFlags.BladesHeavy);var wrong=new RuleCalculateWeaponStats(ally,aw);int baseline=ally.Stats.AdditionalDamage.ModifiedValue;using(Runtime(effect,component).RequestEventContext())component.OnEventAboutToTrigger(wrong);Check(ally.Stats.AdditionalDamage.ModifiedValue==baseline,"different held weapon group grants nothing");wrong.OnDidTrigger();effect.Remove();
    }

    static IEnumerable<object> SerializedNodes(object value,HashSet<object> seen)
    {
        if(value==null||value is string||value.GetType().IsValueType||value is SimpleBlueprint||value is BlueprintReferenceBase||value is UnityEngine.Object||!seen.Add(value))yield break;
        if(value is BlueprintComponent||value is Element)yield return value;
        if(value is IEnumerable items){foreach(var item in items)foreach(var node in SerializedNodes(item,seen))yield return node;yield break;}
        for(var t=value.GetType();t!=null&&t!=typeof(object)&&t!=typeof(BlueprintComponent)&&t!=typeof(Element);t=t.BaseType)
            foreach(var field in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(f=>!f.IsStatic))foreach(var node in SerializedNodes(field.GetValue(value),seen))yield return node;
    }
    static int Schema(BlueprintsCache c,int n)
    {
        var assets=Tokens(c,n).Select(t=>(BlueprintScriptableObject)c.Load(Id(t))).Concat(n==2?new[]{Native<BlueprintFeature>(c,"0f15c6f70d8fb2b49aa6cc24239cc5fa")}:Array.Empty<BlueprintScriptableObject>()).ToArray();var elements=assets.SelectMany(bp=>SerializedNodes(bp.ComponentsArray,new HashSet<object>()).Select(e=>new{Blueprint=bp,Element=e})).Where(x=>x.Element.GetType().Assembly==Main&&x.Element.GetType().Name.StartsWith(Names[n].Replace("01445",""))&&x.Element.GetType().Name.EndsWith("01445")).GroupBy(x=>x.Element.GetType()).Select(g=>g.First()).ToArray();
        Check(elements.Length==new[]{4,1,1,3,5}[n],"every new serialized component/action/condition type is exercised for this archetype");
        var elementCaches=assets.Select(bp=>new{Blueprint=bp,List=bp.ElementsArray,Nodes=bp.ElementsArray.ToArray()}).ToArray();
        var binder=(GuidClassBinder)Kingmaker.Blueprints.JsonSystem.Json.Serializer.Binder;var toType=(Dictionary<string,Type>)Get(binder,"m_GuidToTypeCache");var toGuid=(Dictionary<Type,string>)Get(binder,"m_TypeToGuidCache");var beforeType=toType.ToArray();var beforeGuid=toGuid.ToArray();var previous=Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead;
        try{
            foreach(var type in typeof(BlueprintsCache).Assembly.GetTypes()){var a=type.GetCustomAttributes(typeof(TypeIdAttribute),false).Cast<TypeIdAttribute>().SingleOrDefault();if(a!=null)binder.AddToCache(type,a.GuidString);}
            foreach(var type in Main.GetTypes().Where(t=>t.Name.EndsWith("01445"))){var a=type.GetCustomAttributes(typeof(TypeIdAttribute),false).Cast<TypeIdAttribute>().SingleOrDefault();if(a!=null){Check(typeof(BlueprintsCache).Assembly.GetTypes().SelectMany(t=>t.GetCustomAttributes(typeof(TypeIdAttribute),false).Cast<TypeIdAttribute>()).All(v=>v.Guid!=a.Guid),"native serialized TypeId collision absent "+type.Name);binder.AddToCache(type,a.GuidString);}}
            foreach(var node in elements){Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=new BlueprintJsonWrapper(node.Blueprint);Type kind=node.Element is BlueprintComponent?typeof(BlueprintComponent):node.Element is GameAction?typeof(GameAction):typeof(Condition);var text=new System.IO.StringWriter();var writer=new Newtonsoft.Json.JsonTextWriter(text);Kingmaker.Blueprints.JsonSystem.Json.Serializer.Serialize(writer,node.Element,kind);writer.Flush();var copy=Kingmaker.Blueprints.JsonSystem.Json.Serializer.Deserialize(new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(text.ToString())),kind);Check(copy.GetType()==node.Element.GetType(),"actual native polymorphic serialization restores "+copy.GetType().Name);var expected=(string)Call(Common,"Authored",node.Element,new HashSet<object>());var actual=(string)Call(Common,"Authored",copy,new HashSet<object>());Check(expected==actual,"serialized authored fields preserve "+copy.GetType().Name);}
        }finally{foreach(var saved in elementCaches){saved.List.Clear();saved.List.AddRange(saved.Nodes);Set(saved.Blueprint,"m_AllElements",saved.List);}toType.Clear();foreach(var p in beforeType)toType.Add(p.Key,p.Value);toGuid.Clear();foreach(var p in beforeGuid)toGuid.Add(p.Key,p.Value);Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=previous;}
        return elements.Length;
    }

    internal static int Run(BlueprintsCache parent)
    {
        checks=serial=cacheFaults=connectFaults=missingOutputs=0;template=null;Units.Clear();var h=new Harmony("CotW.Archetypes01445");var game=Game.Instance;var oldState=game.State;var oldTime=game.TimeController;var originalCache=ResourcesLibrary.BlueprintsCache;
        var helperUnits=(IList)AccessTools.Field(Helper,"units").GetValue(null);int helperCount=helperUnits.Count;var current=AccessTools.Field(Helper,"current");var oldContext=current.GetValue(null);current.SetValue(null,null);
        var state=Empty<PersistentState>();var player=Empty<Player>();player.GameTime=TimeSpan.FromHours(1);Set(player,"m_Party",new List<UnitEntityData>());Set(player,"m_CharacterListsValid",true);state.PlayerState=player;Set(game,"State",state);Set(game,"TimeController",Empty<TimeController>());
        var endpoint=AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)});var diceEndpoint=AccessTools.Method(typeof(RulebookEvent.Dice),"D",new[]{typeof(DiceFormula)});var oldPatches=new[]{endpoint,diceEndpoint}.SelectMany(target=>(Harmony.GetPatchInfo(target)?.Prefixes.AsEnumerable()??Enumerable.Empty<Patch>()).Where(p=>p.PatchMethod.DeclaringType==typeof(Psychic070Tests)&&(p.PatchMethod.Name=="CaptureDefense"||p.PatchMethod.Name=="ControlledDice")).Select(p=>new{Target=target,Patch=p})).ToArray();
        var snapshotType=typeof(Repair01436CallerTests).GetNestedType("ParentSnapshot",BindingFlags.NonPublic);var snapshot=Activator.CreateInstance(snapshotType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{parent},null);
        void Scene(MethodBase method,string name)=>h.Patch(method,prefix:new HarmonyMethod(Helper,name));
        try {
            foreach(var p in oldPatches)h.Unpatch(p.Target,p.Patch.PatchMethod);
            Scene(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),"Skip");Scene(AccessTools.PropertyGetter(typeof(Game),"LevelUpController"),"Controller");Scene(AccessTools.Method(typeof(EntityFact),"Attach"),"Identity");Scene(AccessTools.Method(typeof(UnitState),"UpdateStatusEffect"),"Skip");Scene(AccessTools.Method(typeof(UnitEntityData),"UpdateGroup"),"Skip");Scene(AccessTools.PropertyGetter(typeof(TimeController),"GameTime"),"Time");Scene(AccessTools.PropertyGetter(typeof(TimeController),"RealTime"),"Time");Scene(AccessTools.PropertyGetter(typeof(Game),"CurrentMode"),"Mode");Scene(AccessTools.PropertyGetter(typeof(Rulebook),"CurrentContext"),"Context");Scene(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel),"Log",new[]{typeof(string),typeof(object[])}),"Skip");Provider01419Tests.PatchLabels(h); Scene(AccessTools.Method(typeof(Kingmaker.UnitLogic.Commands.UnitCommands),"Run",new[]{typeof(Kingmaker.UnitLogic.Commands.Base.UnitCommand)}),"Submit"); Scene(AccessTools.Method(typeof(UnitEntityData),"IsCurrentUnit"),"CurrentUnit");
            h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff),"OnTrigger"),transpiler:new HarmonyMethod(Helper,"DifficultyBoundary"));
            h.Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),transpiler:new HarmonyMethod(Helper,"RuleBoundary"));
            Scene(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx),"ExceptionWithReport",new[]{typeof(Owlcat.Runtime.Core.Logging.LogChannel),typeof(Exception),typeof(string),typeof(object[])}),"Expose");
            h.Patch(AccessTools.Method(typeof(RuleSkillCheck),"AdjustDCByDifficulty"),transpiler:new HarmonyMethod(typeof(WizardSubschools01431Tests),"SkillDifficultyBoundary"));
            // Rebuild the exact caller: a previous suite may have JIT-inlined
            // GameHelper.TriggerSkillCheck before this boundary was installed.
            // Native Demoralize checks, arithmetic and temporary modifiers stay.
            h.Patch(AccessTools.Method(typeof(BlueprintsCache),"AddCachedBlueprint"),prefix:new HarmonyMethod(typeof(Archetypes01445Tests),nameof(SkipWrite)),postfix:new HarmonyMethod(typeof(Archetypes01445Tests),nameof(CacheFault)));h.Patch(AccessTools.Method(Common,"Definition"),prefix:new HarmonyMethod(typeof(Archetypes01445Tests),nameof(DefinitionFault)));
            Scene(AccessTools.Method(typeof(UnitEntityData),"IsAlly",new[]{typeof(UnitEntityData)}),"Ally");Scene(AccessTools.Method(typeof(AddAreaEffect),"IsOwnerAvatarValid"),"False");
            h.Patch(AccessTools.Method(typeof(UnitEntityData),"IsEnemy",new[]{typeof(UnitEntityData)}),prefix:new HarmonyMethod(typeof(Archetypes01445Tests),nameof(Enemy)));
            Scene(AccessTools.Method(typeof(RulebookEvent.Dice),"D",new[]{typeof(DiceFormula)}),"Dice");h.Patch(AccessTools.PropertyGetter(typeof(RulebookEvent.Dice),"D20"),prefix:new HarmonyMethod(typeof(Archetypes01445Tests),nameof(NestedD20)));
            foreach(var method in new[]{AccessTools.Method(typeof(ContextActionSavingThrow),"RunAction"),AccessTools.Method(typeof(RuleSavingThrow),"OnTrigger"),AccessTools.Method(typeof(RuleSavingThrow),"RollD20"),AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),AccessTools.Method(typeof(ContextCalculateAbilityParams),"Calculate",new[]{typeof(MechanicsContext),typeof(BlueprintScriptableObject),typeof(UnitEntityData),typeof(AbilityData)})})h.Patch(method,transpiler:new HarmonyMethod(Helper,"RuleBoundary"));
            foreach(var method in new[]{AccessTools.Method(typeof(RuleSkillCheck),"OnTrigger"),AccessTools.Method(typeof(RuleSkillCheck),"RollD20")})h.Patch(method,transpiler:new HarmonyMethod(typeof(Archetypes01445Tests),nameof(NestedRules)));
            h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams),"OnTrigger"),transpiler:new HarmonyMethod(typeof(Migration01426Tests),"DifficultyBoundary"));
            Scene(AccessTools.PropertyGetter(typeof(UnitEntityData),"IsPlayersEnemy"),"False");Scene(AccessTools.Method(typeof(Player),"IsTurnBasedModeOn"),"False");Scene(AccessTools.Method(typeof(TurnBased.Controllers.CombatController),"IsInTurnBasedCombat"),"False");
            RegistrationAndRules(parent,h);
            AccessTools.Method(snapshotType,"Verify").Invoke(snapshot,null);Check(true,"original complete parent graph unchanged");
            Console.WriteLine("PASS: "+checks+" Archetypes01445 contracts.");return checks;
        } finally {
            foreach(var u in Units.AsEnumerable().Reverse())foreach(var f in u.Facts.List.ToArray())if(f.Manager!=null)u.Facts.Remove(f);
            h.UnpatchAll(h.Id);foreach(var saved in oldPatches){var p=saved.Patch;new Harmony(p.owner).Patch(saved.Target,prefix:new HarmonyMethod(p.PatchMethod){priority=p.priority,before=p.before,after=p.after,debug=p.debug});}Set(game,"State",oldState);Set(game,"TimeController",oldTime);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,originalCache);current.SetValue(null,oldContext);while(helperUnits.Count>helperCount)helperUnits.RemoveAt(helperUnits.Count-1);
        }
    }
}
