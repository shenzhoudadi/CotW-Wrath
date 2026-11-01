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
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Groups;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class Archetypes01444Tests
{
    static readonly Assembly Main=typeof(RakeStance01444).Assembly;
    static readonly Type Helper=typeof(BardMasterpieces01435Tests),Common=Main.GetType("CallOfTheWild.Wrath.Prototype.ArchetypeBatch01444",true);
    static readonly string[] Names={"TerrorWeaver01444","PrimalSorcerer01444","Rake01444"};
    static readonly List<UnitEntityData> Units=new List<UnitEntityData>();
    static int checks,serial,cacheFaults,connectFaults,grants,spends,arcanaRules,summons,stanceStarts,demoralizes;
    static BlueprintGuid faultId;static bool cacheFault,definitionFault;
    static UnitEntityData hostile;static int lastIntimidate;static RuleSkillCheck lastSkill;
    static bool Enemy(UnitEntityData __instance,UnitEntityData __0,ref bool __result){__result=(__instance==hostile)!=(__0==hostile);return false;}
    static bool SkillBoundary(RuleSkillCheck __0,ref RuleSkillCheck __result){lastSkill=__0;lastIntimidate=__0.Initiator.Stats.CheckIntimidate.ModifiedValue;demoralizes++;__result=__0;__0.OnDidTrigger();return false;}
    static RuleSkillCheck SkillEndpoint(RuleSkillCheck rule,MechanicsContext context,bool passive){RuleSkillCheck result=null;SkillBoundary(rule,ref result);return result;}
    static IEnumerable<CodeInstruction> SkillCallBoundary(IEnumerable<CodeInstruction> input){int count=0;foreach(var c in input){if(c.operand is MethodInfo m&&m.DeclaringType==typeof(GameHelper)&&m.Name=="TriggerSkillCheck"){c.opcode=OpCodes.Call;c.operand=AccessTools.Method(typeof(Archetypes01444Tests),nameof(SkillEndpoint));count++;}yield return c;}if(count!=1)throw new InvalidOperationException("01444 native Demoralize skill endpoint changed.");}
    static object Get(object o,string n)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).GetValue(o);
    static void Set(object o,string n,object v)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).SetValue(o,v);
    static object Call(Type t,string m,params object[] a)=>AccessTools.Method(t,m).Invoke(null,a);
    static object B(string m,params object[] a)=>Call(Helper,m,a);
    static Type Module(int n)=>Main.GetType("CallOfTheWild.Wrath.Prototype."+Names[n],true);
    static string C(Type t,string n)=>(string)AccessTools.Field(t,n).GetRawConstantValue();
    static string[] Tokens(int n)=>(string[])AccessTools.Field(Module(n),"OwnTokens").GetValue(null);
    static BlueprintGuid Id(string t)=>(BlueprintGuid)Call(Main.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id",t);
    static T Asset<T>(BlueprintsCache c,string t)where T:SimpleBlueprint=>(T)c.Load(Id(t));
    static T Native<T>(BlueprintsCache c,string id)where T:SimpleBlueprint=>(T)c.Load(BlueprintGuid.Parse(id));
    static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static T Ref<T>(SimpleBlueprint b)where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(b);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,b,null);return r;}
    static void Check(bool b,string why){if(!b)throw new Exception("Archetypes01444: "+why);checks++;}
    static bool Reject(Action action){try{action();return false;}catch(Exception e){while(e is TargetInvocationException)e=e.InnerException;if(e is InvalidOperationException)return true;throw;}}
    static void Register(BlueprintsCache c,int n)=>Call(Module(n),"Register",c);
    static Feature Fact(UnitEntityData u,BlueprintFeature bp){var f=(Feature)u.Facts.Add(new Feature(bp,u.Descriptor));Check(f.IsActive&&f.IsTurnedOn,"native feature activates "+bp.name);return f;}
    static bool Need(Prerequisite p,UnitEntityData u)=>(bool)AccessTools.Method(p.GetType(),"CheckInternal").Invoke(p,new object[]{null,u.Descriptor,null});
    static Buff Buff(UnitEntityData u,BlueprintBuff bp,UnitEntityData caster=null){var f=new Buff(bp,new MechanicsContext(caster??u,u.Descriptor,bp),null);u.Facts.Add(f);Check(f.IsActive&&f.IsTurnedOn,"native buff activates "+bp.name);return f;}
    static EntityFactComponent Runtime(UnitFact fact,BlueprintComponent c)=>fact.Components.Single(r=>ReferenceEquals(r.SourceBlueprintComponent,c));
    static bool Skip()=>false;
    static bool Scope(string name)=>Names.Select(n=>C(Main.GetType("CallOfTheWild.Wrath.Prototype."+n),"ModuleName")).Contains(name);
    static bool NoInvestigator(ref bool __result){__result=false;return false;}
    static void CacheFault(BlueprintGuid guid){if(cacheFault&&guid==faultId){cacheFault=false;cacheFaults++;throw new InvalidOperationException("01444 injected post cache write");}}
    static void DefinitionFault(){if(definitionFault){definitionFault=false;connectFaults++;throw new InvalidOperationException("01444 injected after connection");}}
    static UnitEntityData Unit(BlueprintsCache c,string cls,int level=19){var u=(UnitEntityData)B("Unit",c,"044-"+(++serial),cls,level);Units.Add(u);return u;}
    static void Hydrate(BlueprintsCache c)
    {
        var ids=new HashSet<string>(new[]{"b3a505fb61437dc4097f43c3f8f9a4cf","997665565ca80a649aedd72455c4df1f","24bef8d1bee12274686f6da6ccbc8914","d2a4b74ee7e43a648b51d0f36db2aa34","72051275b1dbb2d42ba9118237794f7c","299aa766dee3cbf4790da4efb8c72484","b57b2a75a5abcaf47a01cf84672b50e9","8bc64d869456b004b9db255cdd1ea734","feb70aab86cc17f4bb64432c83737ac2","5f3126d4120b2b244a95cb2ec23d69fb"});
        foreach(var row in PsychicExpansionTests.NativeRows) {
            string id=(string)row["blueprint"]["AssetId"],name=(string)row["name"];if(!ids.Contains(id)&&!name.StartsWith("BloodlineElemental")&&!name.StartsWith("BloodlineDraconic"))continue;
            var bp=c.Load(BlueprintGuid.Parse(id))as BlueprintScriptableObject;if(bp==null)continue;var data=(JObject)row["blueprint"]["Data"];
            bp.ComponentsArray=((JArray)data["Components"]).Select(x=>(BlueprintComponent)PsychicExpansionTests.Read(x,typeof(BlueprintComponent))).ToArray();
            foreach(var p in data.Properties()){if(p.Name=="Components"||p.Name.StartsWith("$"))continue;var f=AccessTools.Field(bp.GetType(),p.Name);if(f!=null&&!f.IsStatic&&!f.IsInitOnly)f.SetValue(bp,PsychicExpansionTests.Read(p.Value,f.FieldType));}
            Set(bp,"m_AllElements",new List<Element>());bp.OnEnable();
        }
        B("Hydrate",c);
        foreach(string id in new[]{"feb70aab86cc17f4bb64432c83737ac2","e788b02f8d21014488067bdd3ba7b325","333efbf776ab61c4da53e9622751d95f","bd81a3931aa285a4f9844585b5d97e51","fd4d9fd7f87575d47aafe2a64a6e2d8d"})Set(c.Load(BlueprintGuid.Parse(id)),"m_Icon",Empty<UnityEngine.Sprite>());
    }
    static BlueprintsCache Fresh(BlueprintsCache parent)
    {
        var c=(BlueprintsCache)Call(typeof(TimeStutter01437Tests),"Fresh",parent);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,c);
        foreach(int n in Enumerable.Range(0,3))foreach(string t in Tokens(n))c.RemoveCachedBlueprint(Id(t));Hydrate(c);return c;
    }
    static void Registration(BlueprintsCache parent,int n,Harmony h)
    {
        var c=Fresh(parent);var before=new Dictionary<string,object>();Register(c,n);var a=Asset<BlueprintArchetype>(c,Tokens(n)[0]);var cls=a.GetParentClass();var entries=cls.Progression.LevelEntries;var book=cls.Spellbook;
        var assets=Tokens(n).Select(t=>Asset<BlueprintScriptableObject>(c,t)).ToArray();Check(assets.All(x=>x!=null)&&assets.Select(x=>x.AssetGuid).Distinct().Count()==Tokens(n).Length,"complete unique private graph "+Names[n]);
        foreach(var bp in assets.Where(bp=>bp is BlueprintAbility||bp is BlueprintActivatableAbility))Check(ReferenceEquals(Get(bp,"m_Icon"),Get(c.Load(BlueprintGuid.Parse(n==0?"e788b02f8d21014488067bdd3ba7b325":"bd81a3931aa285a4f9844585b5d97e51")),"m_Icon")),"new action button copies its exact verified native icon "+bp.name);
        for(int level=1;level<=20;level++){a.GetAddEntry(level);a.GetRemoveEntry(level);}Register(c,n);
        Check(ReferenceEquals(entries,cls.Progression.LevelEntries)&&ReferenceEquals(book,cls.Spellbook),"base progression/spellbook retained "+Names[n]);
        Check(Tokens(n).Select((t,i)=>ReferenceEquals(c.Load(Id(t)),assets[i])).All(x=>x),"warm native entries keep identities "+Names[n]);
        Check(a.AddFeatures.Select(e=>e.Level).Distinct().Count()==a.AddFeatures.Length,"no duplicate add level entries "+Names[n]);
        string oldName=assets.Last().name;assets.Last().name="ForeignAuthorMutation";Check(Reject(()=>Register(c,n)),"private authored mutation rejected");assets.Last().name=oldName;
        foreach(string t in new[]{Tokens(n)[0],Tokens(n).Last()}){var bad=Fresh(parent);var foreign=new BlueprintFeature{AssetGuid=Id(t),name="Foreign044"};bad.AddCachedBlueprint(foreign.AssetGuid,foreign);Check(Reject(()=>Register(bad,n))&&ReferenceEquals(bad.Load(foreign.AssetGuid),foreign),"foreign GUID preserved "+t);}
        foreach(bool atCache in new[]{true,false}) {
            var bad=Fresh(parent);var moduleClass=Native<BlueprintCharacterClass>(bad,n==0?"83fa3b50138c566698801ba5967bbde0":n==1?"b3a505fb61437dc4097f43c3f8f9a4cf":"299aa766dee3cbf4790da4efb8c72484");var refs=Get(moduleClass,"m_Archetypes");var groups=moduleClass.Progression.UIGroups;
            var featPool=Native<BlueprintFeatureSelection>(bad,"d2a4b74ee7e43a648b51d0f36db2aa34");var prereq=featPool.GetComponent<PrerequisiteFeaturesFromList>();var oldFeats=Get(prereq,"m_Features");var dd=Native<BlueprintCharacterClass>(bad,"72051275b1dbb2d42ba9118237794f7c");var oldDD=dd.ComponentsArray;
            faultId=Id(Tokens(n).Last());cacheFault=atCache;definitionFault=!atCache;
            Check(Reject(()=>Register(bad,n)),"publication fault is rejected "+Names[n]);
            Check(Tokens(n).All(t=>bad.Load(Id(t))==null)&&ReferenceEquals(refs,Get(moduleClass,"m_Archetypes"))&&ReferenceEquals(groups,moduleClass.Progression.UIGroups)&&ReferenceEquals(oldFeats,Get(prereq,"m_Features"))&&ReferenceEquals(oldDD,dd.ComponentsArray),"complete parent/cache/extra rollback "+Names[n]);
            Register(bad,n);Check(Tokens(n).All(t=>bad.Load(Id(t))!=null),"retry after publication fault "+Names[n]);
        }
    }
    static void Terror(BlueprintsCache parent)
    {
        var c=Fresh(parent);Register(c,0);var root=Asset<BlueprintArchetype>(c,Tokens(0)[0]);Check(root.RemoveFeatures.Select(e=>e.Level).SequenceEqual(new[]{11,19}),"Terror replaces only11/19 amplifications");
        Check(root.AddFeatures.Select(e=>e.Level).SequenceEqual(new[]{2,9,11,19}),"Terror original four milestones");
        var known=root.AddFeatures.Take(2).SelectMany(e=>e.Features).Select(f=>f.GetComponent<AddKnownSpell>()).ToArray();Check(known.Select(k=>k.SpellLevel).SequenceEqual(new[]{1,4})&&known[0].Spell.AssetGuid.ToString()=="feb70aab86cc17f4bb64432c83737ac2"&&known[1].Spell.AssetGuid==Id("AuraOfDoom"),"known spells reuse native Command and delivered Doom");
        foreach(bool charisma in new[]{false,true}) {
            var u=Unit(c,"83fa3b50138c566698801ba5967bbde0");string marker=charisma?"e2e8d9e569805bbfa05af26df72b7e52":"4e8266b477c65aaf836869377711184a",resource=charisma?"0795daa1b7d259c3b5052c5c704cc031":"ddfc9f9a2cbd51b48cbd214dbd75c8fb";
            Fact(u,Native<BlueprintFeature>(c,marker));var pool=Native<BlueprintAbilityResource>(c,resource);
            foreach(string speed in new[]{"Move","Swift"}) {
                var fact=Fact(u,Asset<BlueprintFeature>(c,"TerrorAura"+speed+"01444"));var ability=u.Abilities.Enumerable.Single(x=>x.Blueprint.name.Contains("Terror"+speed));grants++;
                Check(ability.Blueprint.name.Contains(charisma?"Charisma":"Wisdom"),"native conditional grant chooses owned phrenic pool");
                var data=new AbilityData(ability);var logic=ability.Blueprint.GetComponent<AbilityResourceLogic>();u.Descriptor.Resources.Restore(pool,5);
                int amount=u.Descriptor.Resources.GetResourceAmount(pool);Check(logic.CalculateCost(data)==1&&logic.IsAbilityRestrictionPassed(data),"native one-point cost available");logic.Spend(data);spends++;Check(u.Descriptor.Resources.GetResourceAmount(pool)==amount-1,"native spend consumes exactlyone point");
                u.Descriptor.Resources.Spend(pool,u.Descriptor.Resources.GetResourceAmount(pool));Check(!logic.IsAbilityRestrictionPassed(data),"zero points blocks native ability");
                var conditional=ability.Blueprint.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>().Single();
                var target=Unit(c,"299aa766dee3cbf4790da4efb8c72484");var other=Unit(c,"83fa3b50138c566698801ba5967bbde0");var match=conditional.ConditionsChecker.Conditions.OfType<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasBuffFromCaster>().Single();
                var context=new MechanicsContext(u,target.Descriptor,ability.Blueprint);using(context.GetDataScope(target))Check(!match.Check(),"no own Doom effect is rejected");
                var foreign=Buff(target,Asset<BlueprintBuff>(c,"AuraOfDoomShakenBuff"),other);using(context.GetDataScope(target))Check(!match.Check(),"other caster Doom effect is rejected");
                var own=Buff(target,Asset<BlueprintBuff>(c,"AuraOfDoomShakenBuff"),u);using(context.GetDataScope(target))Check(match.Check(),"own Doom effect accepted by native caster condition");
                var apply=conditional.IfTrue.Actions.OfType<ContextActionApplyBuff>().Single();Check(apply.Buff.AssetGuid.ToString()=="f08a7239aa961f34c8301518e71d4cdf"&&apply.DurationValue.Calculate(context).Value==1&&!apply.IsFromSpell,"native fear application is exactlyone supernatural round");
                hostile=null;using(context.GetDataScope(target))conditional.RunAction();Check(!target.Descriptor.HasFact(apply.Buff),"native conditional rejects ally despite own Doom");
                hostile=target;using(context.GetDataScope(target))conditional.RunAction();var frightened=target.Buffs.Enumerable.Single(b=>b.Blueprint==apply.Buff);Check(frightened.TimeLeft==TimeSpan.FromSeconds(6)&&frightened.Context.MaybeCaster==u,"real native conditional applies one round to own affected enemy");frightened.Remove();Check(!target.Descriptor.HasFact(apply.Buff),"native fear removal leaves no own status");
                own.Remove();Check(target.Descriptor.HasFact(foreign.Blueprint),"own effect cleanup preserves other caster Doom");
                u.Facts.Remove(fact);Check(!u.Abilities.Enumerable.Any(x=>x.Blueprint==ability.Blueprint),"removing conditional feature removes only its ability");
            }
        }
    }
    static void Primal(BlueprintsCache parent)
    {
        var c=Fresh(parent);Register(c,1);var root=Asset<BlueprintArchetype>(c,Tokens(1)[0]);var selection=Asset<BlueprintFeatureSelection>(c,"PrimalBloodlineSelection01444");Check(selection.AllFeatures.Length==4&&root.RemoveFeatures.Single().Level==1,"four elemental choices replace only original bloodline");
        var dd=Native<BlueprintCharacterClass>(c,"72051275b1dbb2d42ba9118237794f7c");Check(root.GetComponent<PrerequisiteNoClassLevel>()!=null&&dd.GetComponents<PrerequisiteNoArchetype>().Count(p=>p.Archetype==root)==1,"mutual Dragon Disciple exclusion");
        var owner=Unit(c,"b3a505fb61437dc4097f43c3f8f9a4cf");var target=Unit(c,"299aa766dee3cbf4790da4efb8c72484");
        var exclusion=dd.GetComponents<PrerequisiteNoArchetype>().Single(p=>p.Archetype==root);Check(Need(exclusion,owner)&&Need(root.GetComponent<PrerequisiteNoClassLevel>(),owner),"native prestige prerequisites accept ordinary sorcerer");owner.Progression.Classes[0].Archetypes.Add(root);Check(!Need(exclusion,owner),"native prestige prerequisite rejects Primal archetype");owner.Progression.Classes[0].Archetypes.Remove(root);owner.Progression.Classes.Add(new ClassData(dd){Level=1});Check(!Need(root.GetComponent<PrerequisiteNoClassLevel>(),owner),"native reverse prerequisite rejects existing Dragon Disciple");owner.Progression.Classes.RemoveAt(owner.Progression.Classes.Count-1);
        for(int i=0;i<4;i++) {
            string element=new[]{"Air","Earth","Fire","Water"}[i];var progression=Asset<BlueprintProgression>(c,"Primal"+element+"Progression01444");var arcana=Asset<BlueprintFeature>(c,"Primal"+element+"Arcana01444");var summonFeature=Asset<BlueprintFeature>(c,"Primal"+element+"SummoningFeature01444");
            Check(progression.LevelEntries.Where(e=>e.Level==9).SelectMany(e=>e.Features).Any(f=>f==summonFeature)&&!progression.LevelEntries.SelectMany(e=>e.Features).Any(f=>f.name.Contains("ElementalBlastFeature")),"only original blast replaced "+element);
            var source=Native<BlueprintProgression>(c,((string[])AccessTools.Field(Module(1),"Progressions").GetValue(null))[i]);
            Check(source.LevelEntries.Length==progression.LevelEntries.Length&&source.LevelEntries.Zip(progression.LevelEntries,(a,b)=>a.Level==b.Level&&a.Features.Count()==b.Features.Count()).All(v=>v),"all original feature/spell milestone counts retained "+element);
            var fact=Fact(owner,arcana);var component=arcana.GetComponent<DraconicBloodlineArcana>();
            foreach(bool match in new[]{false,true}) {
                var spell=new BlueprintAbility{Type=AbilityType.Spell,ComponentsArray=new BlueprintComponent[]{new SpellDescriptorComponent{Descriptor=match?(SpellDescriptor)component.SpellDescriptor:SpellDescriptor.None}}};spell.OnEnable();var context=new MechanicsContext(owner,target.Descriptor,spell);
                var damage=new EnergyDamage(new DiceFormula(4,DiceType.D6),DamageEnergyType.Fire);var evt=new RuleCalculateDamage(new RuleDealDamage(owner,target,new DamageBundle(damage)){Reason=context}){Reason=context};using(Runtime(fact,component).RequestEventContext())component.OnEventAboutToTrigger(evt);arcanaRules++;
                Check(damage.Modifiers.Sum(m=>m.Value)==(match?4:0),"actual native matching-descriptor +1 per die "+element+match);
            }
            owner.Facts.Remove(fact);var summonFact=Fact(owner,summonFeature);var spawn=summonFeature.GetComponent<OnSpawnBuff>();
            foreach(bool match in new[]{false,true}) {
                var unit=Unit(c,"299aa766dee3cbf4790da4efb8c72484");var spell=new BlueprintAbility{Type=AbilityType.Spell,ComponentsArray=new BlueprintComponent[]{new SpellDescriptorComponent{Descriptor=match?SpellDescriptor.Summoning:SpellDescriptor.None}}};spell.OnEnable();var evt=Empty<RuleSummonUnit>();Set(evt,"Initiator",owner);Set(evt,"SummonedUnit",unit);evt.Reason=new MechanicsContext(owner,unit.Descriptor,spell);
                using(Runtime(summonFact,spawn).RequestEventContext())spawn.OnEventDidTrigger(evt);summons++;
                Check(unit.Descriptor.HasFact(spawn.buff)==match,"actual native OnSpawnBuff descriptor gate "+element+match);
                if(match){var resist=spawn.buff.GetComponent<AddDamageResistanceEnergy>();var trigger=spawn.buff.GetComponent<AddInitiatorAttackWithWeaponTrigger>();Check(resist.Type==((DamageEnergyType[])AccessTools.Field(Module(1),"Energies").GetValue(null))[i]&&resist.Value.Calculate(unit.Buffs.Enumerable.Single(b=>b.Blueprint==spawn.buff).Context)==10&&trigger.OnlyHit&&trigger.AllNaturalAndUnarmed,"resistance10 and natural/unarmed hit filter "+element);var action=trigger.Action.Actions.OfType<ContextActionDealDamage>().Single();Check(action.IgnoreCritical&&action.Value.DiceType==DiceType.D6&&action.Value.DiceCountValue.Calculate((MechanicsContext)null)==1,"native elemental extra1d6 ignores critical multiplier");}
            }
            owner.Facts.Remove(summonFact);
        }
    }
    static void Rake(BlueprintsCache parent)
    {
        var c=Fresh(parent);Register(c,2);var root=Asset<BlueprintArchetype>(c,Tokens(2)[0]);Check(root.RemoveFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,3,6,9,12,15,18}),"Rake original replacement levels");
        var u=Unit(c,"299aa766dee3cbf4790da4efb8c72484");var smile=Asset<BlueprintFeature>(c,"RakesSmile01444");int bluff=u.Stats.CheckBluff.ModifiedValue,diplomacy=u.Stats.CheckDiplomacy.ModifiedValue,intimidate=u.Stats.CheckIntimidate.ModifiedValue,original=u.Stats.SkillPersuasion.ModifiedValue;
        for(int rank=1;rank<=6;rank++){Fact(u,smile);Check(u.Stats.CheckBluff.ModifiedValue==bluff+rank&&u.Stats.CheckDiplomacy.ModifiedValue==diplomacy+rank&&u.Stats.CheckIntimidate.ModifiedValue==intimidate&&u.Stats.SkillPersuasion.ModifiedValue==original,"native smile morale rank "+rank);}var smileFact=u.Facts.Get<Feature>(smile);for(int rank=5;rank>=0;rank--){u.Facts.Remove(smileFact);Check(u.Stats.CheckBluff.ModifiedValue==bluff+rank&&u.Stats.CheckDiplomacy.ModifiedValue==diplomacy+rank&&u.Stats.CheckIntimidate.ModifiedValue==intimidate&&u.Stats.SkillPersuasion.ModifiedValue==original,"native smile rank removal "+rank);}
        for(int n=1;n<=5;n++)Fact(u,Asset<BlueprintFeature>(c,"BravadosBlade"+n+"Feature01444"));u.Stats.SneakAttack.BaseValue=9;var target=Unit(c,"299aa766dee3cbf4790da4efb8c72484");hostile=target;
        var unrelatedBuff=new BlueprintBuff{name="044-independent-buff",AssetGuid=Id("044IndependentBuff"),ComponentsArray=Array.Empty<BlueprintComponent>()};unrelatedBuff.OnEnable();var unrelatedBP=new BlueprintActivatableAbility{name="044-independent-toggle",AssetGuid=Id("044IndependentToggle"),ActivationType=AbilityActivationType.Immediately,Group=ActivatableAbilityGroup.None,ComponentsArray=Array.Empty<BlueprintComponent>()};Set(unrelatedBP,"m_Buff",Ref<BlueprintBuffReference>(unrelatedBuff));unrelatedBP.OnEnable();var add=new AddFacts();Set(add,"m_Facts",new[]{Ref<BlueprintUnitFactReference>(unrelatedBP)});var independentFeature=new BlueprintFeature{name="044-independent-feature",AssetGuid=Id("044IndependentFeature"),ComponentsArray=new BlueprintComponent[]{add}};independentFeature.OnEnable();Fact(u,independentFeature);var unrelated=u.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint==unrelatedBP);unrelated.IsOn=true;unrelated.TryStart();Check(unrelated.IsStarted,"unrelated native stance starts");
        var weapon=(Kingmaker.Items.ItemEntityWeapon)Call(typeof(TeamworkFeatTests),"Weapon",u,false,"044-blade");
        foreach(int n in new[]{1,5,3,2,4}) {
            var ability=u.ActivatableAbilities.Enumerable.Single(x=>x.Blueprint.AssetGuid==Id("BravadosBlade"+n+"Toggle01444"));ability.IsOn=true;ability.TryStart();stanceStarts++;
            Check(ability.IsStarted&&ability.AppliedBuff!=null&&u.ActivatableAbilities.Enumerable.Count(a=>a.IsOn&&a.Blueprint.name.Contains("BravadosBlade"))==1,"native private stance switch "+n);Check(unrelated.IsStarted&&unrelated.IsOn&&u.Descriptor.HasFact(unrelatedBuff),"switch preserves unrelated stance "+n);
            var reduction=Asset<BlueprintBuff>(c,"BravadosBlade"+n+"Reduction01444");Check(reduction.ComponentsArray.Length==0,"legacy reduction GUID retained as inert hidden node");
            checks+=ArchetypeRepair01444Tests.Rake(c,u,target,ability.AppliedBuff,weapon,n);
            // Leave this stance active so the next native activation must stop it.

        }
        foreach(var a in u.ActivatableAbilities.Enumerable.Where(a=>a.Blueprint.name.Contains("BravadosBlade")).ToArray()){a.IsOn=false;a.Stop();}Check(!u.Buffs.Enumerable.Any(b=>b.Blueprint.name.Contains("BravadosBlade"))&&u.Stats.SneakAttack.ModifiedValue==9,"all own stance and transient buffs cleanly stop");
        var typeId=typeof(RakeStance01444).GetCustomAttributes(typeof(TypeIdAttribute),false).Cast<TypeIdAttribute>().Single().Guid;Check(typeId==Guid.ParseExact("4eb457a72e154765897c2deed7561b85","N"),"real native TypeId constructor validates new stance identity");
        Check(typeof(BlueprintsCache).Assembly.GetTypes().SelectMany(t=>t.GetCustomAttributes(typeof(TypeIdAttribute),false).Cast<TypeIdAttribute>()).All(a=>a.Guid!=typeId)&&Main.GetTypes().SelectMany(t=>t.GetCustomAttributes(typeof(TypeIdAttribute),false).Cast<TypeIdAttribute>()).Count(a=>a.Guid==typeId)==1,"new stance TypeId has no actual native or production attribute collision");
    }
    static void Caller(BlueprintsCache parent,Harmony h)
    {
        var c=Fresh(parent);var registrar=Main.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration");var investigator=Main.GetType("CallOfTheWild.Wrath.Prototype.ExtraInvestigatorTalent");
        h.Patch(AccessTools.Method(registrar,"RegisterModule"),prefix:new HarmonyMethod(typeof(Archetypes01444Tests),nameof(Scope)));ScopedAvailable01463Tests.Bind(h,registrar,"ScopeArchetypes01444Tests");h.Patch(AccessTools.Method(investigator,"Register"),prefix:new HarmonyMethod(typeof(Archetypes01444Tests),nameof(NoInvestigator)));
        var reports=new List<string>();Action<string,bool,Exception> report=(name,passed,error)=>{Check(passed&&error==null,"real new archetype dispatcher: "+name+" "+error);reports.Add(name);};
        try{Call(registrar,"RegisterAll",c,report);var first=Names.SelectMany((n,i)=>Tokens(i)).Select(t=>c.Load(Id(t))).ToArray();Call(registrar,"RegisterAll",c,report);Check(reports.Count==6&&reports.Distinct().Count()==3,"actual dispatcher twice reaches exactlythree new modules");Check(Names.SelectMany((n,i)=>Tokens(i)).Select((t,i)=>ReferenceEquals(c.Load(Id(t)),first[i])).All(x=>x),"actual caller retry retains all49 identities");}
        finally{h.Unpatch(AccessTools.Method(registrar,"RegisterModule"),HarmonyPatchType.Prefix,h.Id);h.Unpatch(AccessTools.Method(investigator,"Register"),HarmonyPatchType.Prefix,h.Id);}
        PsychicExpansionTests.Inventory["archetypeCaller01444Contracts"]=new JObject{["passed"]=true,["dispatches"]=2,["moduleCount"]=3,["privateResourceCount"]=49,["allPriorGraphsPreserved"]=true};
    }
    internal static int Run(BlueprintsCache parent,string selection="all")
    {
        checks=serial=cacheFaults=connectFaults=grants=spends=arcanaRules=summons=stanceStarts=demoralizes=0;ArchetypeRepair01444Tests.Reset();Units.Clear();var h=new Harmony("CotW.Archetypes01444");var game=Game.Instance;var oldState=game.State;var oldTime=game.TimeController;var originalCache=ResourcesLibrary.BlueprintsCache;
        var helperUnits=(IList)AccessTools.Field(Helper,"units").GetValue(null);int helperCount=helperUnits.Count;var current=AccessTools.Field(Helper,"current");var oldContext=current.GetValue(null);current.SetValue(null,null);
        var state=Empty<PersistentState>();var player=Empty<Player>();player.GameTime=TimeSpan.FromHours(1);Set(player,"m_Party",new List<UnitEntityData>());Set(player,"m_CharacterListsValid",true);state.PlayerState=player;Set(game,"State",state);Set(game,"TimeController",Empty<TimeController>());
        var endpoint=AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)});var oldPatches=(Harmony.GetPatchInfo(endpoint)?.Prefixes.AsEnumerable()??Enumerable.Empty<Patch>()).Where(p=>p.PatchMethod==AccessTools.Method(typeof(Psychic070Tests),"CaptureDefense")).ToArray();
        var snapshotType=typeof(Repair01436CallerTests).GetNestedType("ParentSnapshot",BindingFlags.NonPublic);var snapshot=Activator.CreateInstance(snapshotType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{parent},null);
        void Scene(MethodBase method,string name)=>h.Patch(method,prefix:new HarmonyMethod(Helper,name));
        try {
            foreach(var p in oldPatches)h.Unpatch(endpoint,p.PatchMethod);
            Scene(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),"Skip");Scene(AccessTools.PropertyGetter(typeof(Game),"LevelUpController"),"Controller");Scene(AccessTools.Method(typeof(EntityFact),"Attach"),"Identity");Scene(AccessTools.Method(typeof(UnitState),"UpdateStatusEffect"),"Skip");Scene(AccessTools.Method(typeof(UnitEntityData),"UpdateGroup"),"Skip");Scene(AccessTools.PropertyGetter(typeof(TimeController),"GameTime"),"Time");Scene(AccessTools.PropertyGetter(typeof(TimeController),"RealTime"),"Time");Scene(AccessTools.PropertyGetter(typeof(Game),"CurrentMode"),"Mode");Scene(AccessTools.PropertyGetter(typeof(Rulebook),"CurrentContext"),"Context");Scene(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel),"Log",new[]{typeof(string),typeof(object[])}),"Skip");Provider01419Tests.PatchLabels(h); Scene(AccessTools.Method(typeof(Kingmaker.UnitLogic.Commands.UnitCommands),"Run",new[]{typeof(Kingmaker.UnitLogic.Commands.Base.UnitCommand)}),"Submit"); Scene(AccessTools.Method(typeof(UnitEntityData),"IsCurrentUnit"),"CurrentUnit");
            h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff),"OnTrigger"),transpiler:new HarmonyMethod(Helper,"DifficultyBoundary"));
            h.Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),transpiler:new HarmonyMethod(Helper,"RuleBoundary"));
            Scene(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx),"ExceptionWithReport",new[]{typeof(Owlcat.Runtime.Core.Logging.LogChannel),typeof(Exception),typeof(string),typeof(object[])}),"Expose");
            h.Patch(AccessTools.Method(typeof(RuleSkillCheck),"AdjustDCByDifficulty"),transpiler:new HarmonyMethod(typeof(WizardSubschools01431Tests),"SkillDifficultyBoundary"));
            // Rebuild the exact caller: a previous suite may have JIT-inlined
            // GameHelper.TriggerSkillCheck before this boundary was installed.
            // Native Demoralize checks, arithmetic and temporary modifiers stay.
            h.Patch(AccessTools.Method(typeof(Demoralize),"RunAction"),transpiler:new HarmonyMethod(typeof(Archetypes01444Tests),nameof(SkillCallBoundary)));
            h.Patch(AccessTools.Method(typeof(UnitEntityData),"IsEnemy",new[]{typeof(UnitEntityData)}),prefix:new HarmonyMethod(typeof(Archetypes01444Tests),nameof(Enemy)));
            h.Patch(AccessTools.Method(typeof(BlueprintsCache),"AddCachedBlueprint"),postfix:new HarmonyMethod(typeof(Archetypes01444Tests),nameof(CacheFault)));h.Patch(AccessTools.Method(Common,"Definition"),prefix:new HarmonyMethod(typeof(Archetypes01444Tests),nameof(DefinitionFault)));
            for(int n=0;n<3;n++)if(selection=="all"||selection==new[]{"terror","primal","rake"}[n]) {
                int begin=checks;Registration(parent,n,h);if(n==0)Terror(parent);else if(n==1){Primal(parent);checks+=ArchetypeRepair01444Tests.Primal(parent,h);}else Rake(parent);
                string key=new[]{"terrorWeaver01444Contracts","primalSorcerer01444Contracts","rake01444Contracts"}[n];
                PsychicExpansionTests.Inventory[key]=new JObject{["passed"]=true,["assertions"]=checks-begin,["additionalItemCount"]=1,["privateResourceCount"]=Tokens(n).Length,["newSpellRootCount"]=0,["newHarmonyPatchCount"]=0,["newSerializedComponentTypeCount"]=n==0?0:1,["newBlueprints"]=new JArray(Tokens(n).Select(t=>new JObject{["token"]=t,["id"]=Id(t).ToString()})),["baseProgressionPreserved"]=true,["cacheRollbackFaults"]=1,["connectionRollbackFaults"]=1,["conditionalGrants"]=n==0?grants:0,["resourceSpends"]=n==0?spends:0,["nativeArcanaRules"]=n==1?arcanaRules:0,["nativeSummonEvents"]=n==1?summons:0,["nativeStanceStarts"]=n==2?stanceStarts:0,["nativeDemoralizeActions"]=n==2?demoralizes:0,["nativeDeferredResolveCallbacks"]=0,["boundary"]="Real game blueprint/component methods execute on managed units with controlled world, hostility, UI, clocks and logging. Native prerequisites, resource grants/spends, fear application/removal, arcana and OnSpawnBuff callbacks execute. Rake executes event-local sneak dice changes on weapon and independent ray preparation, repeat deliveries and native Demoralize temporary modifiers with final skill success bounded to failure; no character-wide sneak stat or duration-based cleanup is used. Natural/unarmed elemental damage/DR descriptors are checked; complete HP/attack/scene pipelines, presentation and scene save/load remain guide observations. No game process launched."};
            }
            if(selection=="all"){checks+=ArchetypeRepair01444Tests.Localization();ArchetypeRepair01444Tests.Report();}
            Caller(parent,h);AccessTools.Method(snapshotType,"Verify").Invoke(snapshot,null);Check(true,"complete original parent graph remains exact");Console.WriteLine("PASS: "+checks+" Archetypes01444 "+selection+" contracts.");return checks;
        } finally {
            foreach(var u in Units.AsEnumerable().Reverse())foreach(var f in u.Facts.List.ToArray())if(f.Manager!=null)u.Facts.Remove(f);
            h.UnpatchAll(h.Id);foreach(var p in oldPatches)new Harmony(p.owner).Patch(endpoint,prefix:new HarmonyMethod(p.PatchMethod){priority=p.priority,before=p.before,after=p.after,debug=p.debug});Set(game,"State",oldState);Set(game,"TimeController",oldTime);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,originalCache);current.SetValue(null,oldContext);while(helperUnits.Count>helperCount)helperUnits.RemoveAt(helperUnits.Count-1);
        }
    }
}
