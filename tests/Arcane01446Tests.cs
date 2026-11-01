using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
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
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static partial class Arcane01446Tests
{
    static readonly Assembly Main=typeof(ArcaneCastMode01446).Assembly;
    static readonly Type Helper=typeof(BardMasterpieces01435Tests);
    static Type T(string name)=>Main.GetType("CallOfTheWild.Wrath.Prototype."+name,true);
    static object Get(object o,string n)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).GetValue(o);
    static void Set(object o,string n,object v)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).SetValue(o,v);
    static object Call(Type t,string m,params object[] a)=>AccessTools.Method(t,m).Invoke(null,a);
    static BlueprintGuid Id(string t)=>(BlueprintGuid)Call(T("PsychicAssets"),"Id",t);
    static TAsset Asset<TAsset>(BlueprintsCache c,string t)where TAsset:SimpleBlueprint=>(TAsset)c.Load(Id(t));
    static TAsset Native<TAsset>(BlueprintsCache c,string id)where TAsset:SimpleBlueprint=>(TAsset)c.Load(BlueprintGuid.Parse(id));
    static TEmpty Empty<TEmpty>()where TEmpty:class=>(TEmpty)FormatterServices.GetUninitializedObject(typeof(TEmpty));
    static BlueprintsCache Fresh(BlueprintsCache parent){
        var c=(BlueprintsCache)Call(typeof(Archetypes01445Tests),"Fresh",parent);
        foreach(var fact in Loaded(c).OfType<Kingmaker.Blueprints.Facts.BlueprintUnitFact>())if(ReferenceEquals(fact.Icon,null))Set(fact,"m_Icon",Empty<UnityEngine.Sprite>());
        // Both full and focused hosts must expose the same real optional
        // producer before auditing its compatible bloodline mutation graph.
        if(c.Load(Id("PrimalSorcererArchetype01444"))==null)Call(T("PrimalSorcerer01444"),"Register",c);
        return c;
    }
    static SimpleBlueprint[] Loaded(BlueprintsCache c)=>(SimpleBlueprint[])Call(typeof(Repair01436CallerTests),"Loaded",c);
    static readonly string[] Modules={"BloodlineMutations01446","Admixture01446"};
    static void Register(BlueprintsCache c,int n)=>Call(T(Modules[n]),"Register",c);
    static int checks,serial,cacheFailures,missingFailures,connectFailures;
    static readonly List<UnitEntityData> Units=new List<UnitEntityData>();
    static void Check(bool ok,string why){if(!ok)throw new Exception("Arcane01446: "+why);checks++;}
    static bool Reject(Action a){try{a();return false;}catch(Exception e){while(e is TargetInvocationException)e=e.InnerException;if(e is InvalidOperationException)return true;throw;}}
    static BlueprintGuid faultId;static int fault;
    static bool CacheBefore(BlueprintGuid guid){if(fault==2&&guid==faultId){fault=0;missingFailures++;return false;}return true;}
    static void CacheAfter(BlueprintGuid guid){if(fault==1&&guid==faultId){fault=0;cacheFailures++;throw new InvalidOperationException("01446 injected post-write failure");}}
    static void DefinitionBefore(BlueprintScriptableObject bp){if(fault==3&&bp.AssetGuid==faultId){fault=0;connectFailures++;throw new InvalidOperationException("01446 injected post-connect failure");}}
    static UnitEntityData Unit(BlueprintsCache c,string cls="b3a505fb61437dc4097f43c3f8f9a4cf",int level=19){var u=(UnitEntityData)Call(Helper,"Unit",c,"046-"+(++serial),cls,level);Units.Add(u);return u;}
    static Feature Fact(UnitEntityData u,BlueprintFeature bp){var f=(Feature)u.Facts.Add(new Feature(bp,u.Descriptor));Check(f.IsActive&&f.IsTurnedOn,"native feature activates "+bp.name);return f;}
    static Buff Buff(UnitEntityData u,BlueprintBuff bp,UnitEntityData caster=null){var b=new Buff(bp,new MechanicsContext(caster??u,u.Descriptor,bp),null);u.Facts.Add(b);Check(b.IsActive&&b.IsTurnedOn,"native buff activates "+bp.name);return b;}
    static IDisposable Scope(UnitFact fact,BlueprintComponent c)=>fact.Components.Single(r=>ReferenceEquals(r.SourceBlueprintComponent,c)).RequestEventContext();
    static void Structural(BlueprintsCache parent,Harmony h) {
        for(int n=0;n<2;n++) {
            var c=Fresh(parent);var before=Loaded(c);Register(c,n);var assets=Loaded(c).Except(before).OfType<BlueprintScriptableObject>().ToArray();Check(assets.Length>20,"complete independent module publishes "+n);
            foreach(var bp in assets.OfType<Kingmaker.Blueprints.Facts.BlueprintUnitFact>())Check(!ReferenceEquals(bp.Icon,null),"every new ability/option has real icon "+bp.name);
            Register(c,n);Register(c,n);Check(assets.All(bp=>ReferenceEquals(c.Load(bp.AssetGuid),bp)),"reentry preserves all published identities "+n);
            if(n==0) {
                var rows=(string[][])AccessTools.Field(T("BloodlineAnchors01446"),"Rows").GetValue(null);
                foreach(var row in rows) {var p=Native<BlueprintProgression>(c,row[0]);for(int i=2;i<5;i++)if(row[i].Length!=0){var power=Native<BlueprintFeature>(c,row[i]);var choices=p.LevelEntries.Where(e=>e.Level==(row[1]=="Sorcerer"?(i==2?1:i==3?3:9):(i==2?1:i==3?4:8))).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Where(s=>s.AllFeatures.Any(x=>x==power||x is BlueprintProgression branch&&branch.LevelEntries.Any(e=>e.Features.Contains(power)))).ToArray();Check(choices.Length==1&&choices[0].AllFeatures.Count(f=>f==power||f is BlueprintProgression branch&&branch.LevelEntries.Any(e=>e.Features.Contains(power)))==1,"native power retained as one alternative "+row[0]+i);}}
                var familiar=Asset<BlueprintFeatureSelection>(c,"BloodlineFamiliar01446");Check(familiar.ComponentsArray.Length==0&&familiar.Groups.Length==0&&familiar.AllFeatures.Count()==13,"Familiar clones actual thirteen native familiars without rogue prerequisite");
            } else {
                var root=Asset<BlueprintProgression>(c,"AdmixtureSchool01446");var source=Native<BlueprintProgression>(c,"f8019b7724d72a241a97157bc37f1c3b");Check(root.LevelEntries.Select(e=>e.Level).SequenceEqual(new[]{1,8,20})&&root.LevelEntries.Last().Features.SequenceEqual(source.LevelEntries.Last().Features),"Evocation capstone retained and exact CotW 1/8 replacements");
                Check(root.LevelEntries[0].Features.Count(x=>x.AssetGuid.ToString()=="6c29030e9fea36949877c43a6f94ff31")==2,"two opposition choices retained");Check(!ReferenceEquals(root.LevelEntries,source.LevelEntries)&&!ReferenceEquals(Get(root,"m_UIDeterminatorsGroup"),Get(source,"m_UIDeterminatorsGroup")),"private progression references independently copied");
            }
            foreach(int failure in new[]{1,2,3}) {
                var bad=Fresh(parent);var nativeBefore=Loaded(bad).OfType<BlueprintProgression>().Select(p=>new{P=p,E=p.LevelEntries,G=p.UIGroups,D=Get(p,"m_UIDeterminatorsGroup")}).ToArray();var pools=Loaded(bad).OfType<BlueprintFeatureSelection>().Select(p=>new{P=p,F=Get(p,"m_Features"),A=Get(p,"m_AllFeatures")}).ToArray();
                faultId=assets.Last().AssetGuid;fault=failure;Check(Reject(()=>Register(bad,n))&&fault==0,"actual publication failure rejected "+n+"/"+failure);Check(assets.All(bp=>bad.Load(bp.AssetGuid)==null),"no partial blueprints committed "+n+failure);Check(nativeBefore.All(p=>ReferenceEquals(p.E,p.P.LevelEntries)&&ReferenceEquals(p.G,p.P.UIGroups)&&ReferenceEquals(p.D,Get(p.P,"m_UIDeterminatorsGroup")))&&pools.All(p=>ReferenceEquals(p.F,Get(p.P,"m_Features"))&&ReferenceEquals(p.A,Get(p.P,"m_AllFeatures"))),"all native lists rolled back "+n+failure);Register(bad,n);Check(assets.All(bp=>bad.Load(bp.AssetGuid)!=null),"retry after actual failure publishes every output "+n+failure);
            }
            var collisionCache=Fresh(parent);var foreign=new BlueprintFeature{AssetGuid=assets.Last().AssetGuid,name="External046"};collisionCache.AddCachedBlueprint(foreign.AssetGuid,foreign);Check(Reject(()=>Register(collisionCache,n))&&ReferenceEquals(collisionCache.Load(foreign.AssetGuid),foreign),"foreign GUID rejected and preserved "+n);
            foreach(var type in new[]{typeof(ArcaneCastMode01446),typeof(BloodHavoc01446),typeof(ElementalManipulation01446),typeof(ElementalAuraMode01446)}) {
                foreach(var component in assets.SelectMany(a=>a.ComponentsArray).Where(x=>x.GetType()==type).Take(1)) {
                    var owner=component.OwnerBlueprint;var elements=owner.ElementsArray;var nodes=elements.ToArray();var previous=Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead;
                    try {Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=new BlueprintJsonWrapper(owner);var sw=new StringWriter();var writer=new JsonTextWriter(sw);Kingmaker.Blueprints.JsonSystem.Json.Serializer.Serialize(writer,component,typeof(BlueprintComponent));writer.Flush();var copy=Kingmaker.Blueprints.JsonSystem.Json.Serializer.Deserialize(new JsonTextReader(new StringReader(sw.ToString())),typeof(BlueprintComponent));Check(copy.GetType()==type,"native polymorphic serializer restores "+type.Name);}
                    finally {elements.Clear();elements.AddRange(nodes);Set(owner,"m_AllElements",elements);Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=previous;}
                }
            }
            PsychicExpansionTests.Inventory[n==0?"bloodlineMutations01446":"admixture01446"]=new JObject{["passed"]=true,["privateResourceCount"]=assets.Length,["ids"]=new JArray(assets.Select(a=>a.AssetGuid.ToString())),["cacheRollback"]=true,["missingOutputRejected"]=true,["connectionRollback"]=true};
        }
    }
    static Spellbook Book(UnitEntityData u,BlueprintCharacterClass cls) {var b=new Spellbook(u.Descriptor,cls.Spellbook);Set(b,"m_BaseLevelInternal",19);Set(u.Descriptor,"m_Spellbooks",new Dictionary<BlueprintSpellbook,Spellbook>{{cls.Spellbook,b}});return b;}
    static RuleCastSpell Cast(UnitEntityData u,UnitEntityData target,Spellbook book,BlueprintAbility bp,int level=19) {
        var ability=new AbilityData(bp,u.Descriptor);Set(ability,"m_CachedSpellbook",book);
        var context=new AbilityExecutionContext(ability,new AbilityParams{CasterLevel=level,SpellLevel=3,DC=20},new TargetWrapper(target),new RulebookEventContext(),u);
        var rule=Empty<RuleCastSpell>();Set(rule,"Spell",ability);Set(rule,"Context",context);Set(rule,"SpellTarget",new TargetWrapper(target));Set(rule,"Initiator",u);return rule;
    }
    static RuleCalculateDamage Damage(RuleCastSpell cast,UnitEntityData target,params BaseDamage[] damage){var parent=new RuleDealDamage(cast.Initiator,target,new DamageBundle(damage)){Reason=cast.Context};return new RuleCalculateDamage(parent){Reason=cast.Context};}
    static bool StopDamage()=>false;
    static int foreignCapCalls;
    static void ForeignFiveCap(ref object __result){var old=(DiceFormula)Get(__result,"Dices");Set(__result,"Dices",new DiceFormula(15,old.Dice));foreignCapCalls++;}
    static void CastModes(BlueprintsCache parent,Harmony h) {
        var c=Fresh(parent);Register(c,0);Register(c,1);var u=Unit(c);var target=Unit(c);var cls=Native<BlueprintCharacterClass>(c,"b3a505fb61437dc4097f43c3f8f9a4cf");var book=Book(u,cls);
        var fireball=Native<BlueprintAbility>(c,"2d81362af43aeac4387a3d4fced489c3");Check(fireball!=null,"real Fireball fixture exists");u.Stats.Charisma.BaseValue=24;u.Stats.Strength.BaseValue=18;
        var fact1=Fact(u,Asset<BlueprintFeature>(c,"BloodIntensity01446"));var fact2=Fact(u,Asset<BlueprintFeature>(c,"BloodPiercing01446"));
        var intensity=Asset<BlueprintBuff>(c,"BloodIntensity01446Buff");var piercing=Asset<BlueprintBuff>(c,"BloodPiercing01446Buff");
        var b1=Buff(u,intensity);var b2=Buff(u,piercing);var m1=intensity.GetComponent<ArcaneCastMode01446>();var m2=piercing.GetComponent<ArcaneCastMode01446>();var p1=m1.Resource.Get();var p2=m2.Resource.Get();
        foreach(int level in new[]{1,2,3,4,6,7,8,9,11,12,15,16,18,19,20}) {u.Progression.GetClassData(cls).Level=level;Check(p1.GetMaxAmount(u.Descriptor)==(level<3?0:1+(level-3)/4),"native Blood Intensity resource threshold "+level);Check(p2.GetMaxAmount(u.Descriptor)==(level<4?0:1+(level-4)/5),"native Blood Piercing resource threshold "+level);}u.Progression.GetClassData(cls).Level=19;
        void About(RuleCastSpell cast){using(Scope(b1,m1))m1.OnEventAboutToTrigger(cast);using(Scope(b2,m2))m2.OnEventAboutToTrigger(cast);}
        void End(RuleCastSpell cast){m1.OnEventDidTrigger(cast);m2.OnEventDidTrigger(cast);}
        u.Descriptor.Resources.Restore(p1,99);u.Descriptor.Resources.Restore(p2,99);int initial1=u.Descriptor.Resources.GetResourceAmount(p1),initial2=u.Descriptor.Resources.GetResourceAmount(p2);
        var cast=Cast(u,target,book,fireball);var parameters=cast.Context.Params;About(cast);About(cast);Check(u.Descriptor.Resources.GetResourceAmount(p1)==initial1-1&&u.Descriptor.Resources.GetResourceAmount(p2)==initial2-1,"two active mutations charge once each despite duplicate cast notifications");Check(!ReferenceEquals(parameters,cast.Context.Params)&&parameters.Metamagic==0,"cached/shared original params untouched");
        var rank=fireball.GetComponents<ContextRankConfig>().Single(r=>(bool)Get(r,"m_AffectedByIntensifiedMetamagic"));Check(rank.GetValue(cast.Context)==17,"native capped Fireball rank gains max Strength/Charisma seven dice");
        cast.Context.Params.Metamagic|=Metamagic.Intensified;Check(rank.GetValue(cast.Context)==17,"native Intensified does not stack with Blood Intensity");u.Stats.Charisma.BaseValue=16;Check(rank.GetValue(cast.Context)==15,"higher native Intensified five-die bonus wins");u.Stats.Charisma.BaseValue=24;
        cast.Context.Params.Metamagic|=(Metamagic)(1<<12);var action=((IEnumerable)Call(typeof(Archetypes01445Tests),"SerializedNodes",fireball.ComponentsArray,new HashSet<object>())).Cast<object>().OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage>().First();
        var info=AccessTools.Method(action.GetType(),"GetDamageInfo");var foreign=AccessTools.Method(typeof(Arcane01446Tests),nameof(ForeignFiveCap));h.Patch(info,postfix:new HarmonyMethod(foreign){priority=Priority.First});
        try {using(cast.Context.GetDataScope(target)){var result=info.Invoke(action,null);Check(((DiceFormula)Get(result,"Dices")).Rolls==17&&foreignCapCalls==1,"native hidden damage-info struct and installed postfix retain the higher paid cap after a TTT source-shaped five-level override");}}finally{h.Unpatch(info,foreign);}
        foreach(int ray in Enumerable.Range(0,3)){var e=new EnergyDamage(new DiceFormula(4,DiceType.D6),DamageEnergyType.Fire);var physical=new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(1,DiceType.D6)),0,PhysicalDamageForm.Bludgeoning);var rule=Damage(cast,target,e,physical);rule.OnTrigger(new RulebookEventContext());Check(e.ReductionPenalty.TotalValue==7&&physical.ReductionPenalty.TotalValue==0,"actual installed damage prefix reduces only energy resistance, ray "+ray);}
        var sr=new RuleSpellResistanceCheck(cast.Context,target);sr.OnTrigger(new RulebookEventContext());Check(((ModifiableBonus)Get(sr,"m_AdditionalSpellPenetration")).TotalValue==7,"actual installed resistance prefix adds seven penetration");
        cast.SetSuccess(true);End(cast);var later=Cast(u,target,book,fireball);Check(rank.GetValue(later.Context)==10&&later.Context.Params.Metamagic==0,"following unmodified spell has native ten-die cap and no paid flags");
        int before1=u.Descriptor.Resources.GetResourceAmount(p1),before2=u.Descriptor.Resources.GetResourceAmount(p2);var failed=Cast(u,target,book,fireball);About(failed);End(failed);End(failed);Check(u.Descriptor.Resources.GetResourceAmount(p1)==before1&&u.Descriptor.Resources.GetResourceAmount(p2)==before2&&failed.Context.Params.Metamagic==0,"failed cast refunds both exactly once and restores execution flags");
        u.Descriptor.Resources.Spend(p1,99);u.Descriptor.Resources.Spend(p2,99);var empty=Cast(u,target,book,fireball);About(empty);Check(empty.Context.Params.Metamagic==0,"zero resources preserve ordinary usable spell");
        // Native context JSON must carry the execution-only flags after the mode is off.
        var saved=new MechanicsContext(u,u.Descriptor,fireball);saved.SetParams(cast.Context.Params.Clone());var saveSerializer=JsonSerializer.Create(new JsonSerializerSettings{ContractResolver=new Kingmaker.EntitySystem.Persistence.JsonUtility.OptInContractResolver(),Converters=Kingmaker.EntitySystem.Persistence.JsonUtility.DefaultJsonSettings.CommonConverters.ToList(),TypeNameHandling=TypeNameHandling.None});var sw=new StringWriter();var writer=new JsonTextWriter(sw);saveSerializer.Serialize(writer,saved,typeof(MechanicsContext));writer.Flush();var restored=saveSerializer.Deserialize<MechanicsContext>(new JsonTextReader(new StringReader(sw.ToString())));Check(restored.Params.Metamagic==cast.Context.Params.Metamagic,"native context serializer roundtrip preserves paid flags");
        b1.Remove();b2.Remove();var ruleAfter=Damage(cast,target,new EnergyDamage(new DiceFormula(1,DiceType.D6),DamageEnergyType.Fire));ruleAfter.OnTrigger(new RulebookEventContext());Check(ruleAfter.DamageBundle.First().ReductionPenalty.TotalValue==7,"paid projectile retains effect after switching off mode");
        if(Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineExternal01450")==null) {
        // Havoc follows the real bonus-spell grant fact, not merely knowing a spell.
        var havoc=Asset<BlueprintFeature>(c,"BloodHavoc01446");var hc=havoc.GetComponent<BloodHavoc01446>();var hf=Fact(u,havoc);int i=Array.FindIndex(hc.Spells,s=>s.Guid==fireball.AssetGuid);Check(i>=0,"real native bloodline bonus-spell grant maps Fireball");
        var normal=Damage(Cast(u,target,book,fireball),target,new EnergyDamage(new DiceFormula(4,DiceType.D6),DamageEnergyType.Fire));using(Scope(hf,hc))hc.OnEventAboutToTrigger(normal);Check(normal.DamageBundle.First().Bonus==0,"merely knowing spell does not gain Havoc");
        var grant=Fact(u,(BlueprintFeature)hc.Grants[i].Get());var withGrant=Damage(Cast(u,target,book,fireball),target,new EnergyDamage(new DiceFormula(4,DiceType.D6),DamageEnergyType.Fire),new EnergyDamage(new DiceFormula(2,DiceType.D6),DamageEnergyType.Cold){Precision=true});using(Scope(hf,hc))hc.OnEventAboutToTrigger(withGrant);Check(withGrant.DamageBundle.First().Bonus==4&&withGrant.DamageBundle.Last().Bonus==0,"Havoc grants per actual spell die, excludes precision riders");
        Check(((JObject)PsychicExpansionTests.Inventory["bloodlineMutations01446"])["passed"].Value<bool>(),"registration evidence retained");
        }
        PsychicExpansionTests.Inventory["bloodlineCasting01446"]=new JObject{["passed"]=true,["multiRayCount"]=3,["realResourceFormulas"]=true,["perCastSpendAndRefund"]=true,["noLaterCastLeak"]=true,["nativeRankCalculation"]=true,["actualInstalledPrefixes"]=true,["nativeContextSerialization"]=true};
    }
    static void Admixture(BlueprintsCache parent) {
        var c=Fresh(parent);Register(c,1);var u=Unit(c,"ba34257984f4c41408ce1dc2004e342e");var target=Unit(c);var cls=Native<BlueprintCharacterClass>(c,"ba34257984f4c41408ce1dc2004e342e");var book=Book(u,cls);u.Stats.Intelligence.BaseValue=24;
        var baseFeature=Asset<BlueprintFeature>(c,"AdmixtureVersatileFeature01446");var fact=Fact(u,baseFeature);Check(u.ActivatableAbilities.Enumerable.Count(a=>a.Blueprint.Buff?.GetComponent<ArcaneCastMode01446>()?.Kind==ArcaneMode01446.Versatile)==4,"one native toggle for each CotW Versatile energy");var pool=Asset<BlueprintAbilityResource>(c,"AdmixtureVersatileResource01446");Check(pool.GetMaxAmount(u.Descriptor)==10,"native 3+Intelligence daily uses");var rounds=Asset<BlueprintAbilityResource>(c,"AdmixtureAuraResource01446");Check(rounds.GetMaxAmount(u.Descriptor)==19,"native wizard-level aura rounds");
        var names=new[]{"Acid","Electricity","Fire","Cold"};var energies=new[]{DamageEnergyType.Acid,DamageEnergyType.Electricity,DamageEnergyType.Fire,DamageEnergyType.Cold};var descriptors=new[]{SpellDescriptor.Acid,SpellDescriptor.Electricity,SpellDescriptor.Fire,SpellDescriptor.Cold};var fireball=Native<BlueprintAbility>(c,"2d81362af43aeac4387a3d4fced489c3");Buff previous=null;
        foreach(int i in Enumerable.Range(0,4)) {
            var bp=Asset<BlueprintBuff>(c,"Admixture"+names[i]+"VersatileBuff01446");var mode=bp.GetComponent<ArcaneCastMode01446>();var b=Buff(u,bp);Check(previous==null||previous.IsDisposed,"switching energy removes previous mode immediately");previous=b;
            u.Descriptor.Resources.Restore(pool,99);int amount=u.Descriptor.Resources.GetResourceAmount(pool);var cast=Cast(u,target,book,fireball);var original=cast.Context.SpellDescriptor;using(Scope(b,mode))mode.OnEventAboutToTrigger(cast);Check(u.Descriptor.Resources.GetResourceAmount(pool)==amount-1,"Versatile consumes one actual native point "+names[i]);Check((cast.Context.SpellDescriptor&(SpellDescriptor.Acid|SpellDescriptor.Electricity|SpellDescriptor.Fire|SpellDescriptor.Cold))==descriptors[i],"execution descriptor changed only for this spell "+names[i]);
            var energy=new EnergyDamage(new DiceFormula(5,DiceType.D6),DamageEnergyType.Fire);var sonic=new EnergyDamage(new DiceFormula(2,DiceType.D6),DamageEnergyType.Sonic);var rule=Damage(cast,target,energy,sonic);new RulePrepareDamage(rule.ParentRule).OnTrigger(new RulebookEventContext());rule.OnTrigger(new RulebookEventContext());Check(energy.EnergyType==energies[i]&&sonic.EnergyType==DamageEnergyType.Sonic,"actual installed conversion changes elemental component only "+names[i]);cast.SetSuccess(true);mode.OnEventDidTrigger(cast);
            var next=Cast(u,target,book,fireball);Check(next.Context.SpellDescriptor==original&&next.Context.Params.Metamagic==0,"next spell descriptor and flags preserved "+names[i]);
            var failed=Cast(u,target,book,fireball);using(Scope(b,mode))mode.OnEventAboutToTrigger(failed);mode.OnEventDidTrigger(failed);Check(u.Descriptor.Resources.GetResourceAmount(pool)==amount-1&&failed.Context.SpellDescriptor==original&&failed.Context.Params.Metamagic==0,"failed converted cast refunds and restores descriptor "+names[i]);
            var aura=Asset<BlueprintBuff>(c,"Admixture"+names[i]+"AuraEffect01446");var ab=Buff(target,aura,u);var ac=aura.GetComponent<ElementalManipulation01446>();
            foreach(int sourceLevel in new[]{18,19,20}){var source=Cast(target,u,Book(target,Native<BlueprintCharacterClass>(c,"b3a505fb61437dc4097f43c3f8f9a4cf")),fireball,sourceLevel);var damage=new EnergyDamage(new DiceFormula(1,DiceType.D6),DamageEnergyType.Fire);var evt=Damage(source,u,damage);using(Scope(ab,ac))ac.OnEventAboutToTrigger(evt);Check(damage.EnergyType==(sourceLevel<=19?energies[i]:DamageEnergyType.Fire),"aura respects emitting wizard's level and current native spell context "+i+sourceLevel);}ab.Remove();
            var area=Asset<BlueprintAbilityAreaEffect>(c,"Admixture"+names[i]+"AuraArea01446");Check(area.Size.Value==30&&!area.AggroEnemies&&!area.SpellResistance,"non-hostile native thirty-foot energy aura "+names[i]);
        }
        previous.Remove();Check(!u.Buffs.Enumerable.Any(b=>b.Blueprint.GetComponent<ArcaneCastMode01446>()?.Kind==ArcaneMode01446.Versatile),"mode buffs removed cleanly");
        PsychicExpansionTests.Inventory["admixtureCasting01446"]=new JObject{["passed"]=true,["fourEnergies"]=true,["realResourceFormula"]=true,["perCastDescriptorAndEnergy"]=true,["failedRefund"]=true,["noLaterCastLeak"]=true,["auraLevelBoundaryCases"]=12,["originalCotWOutgoingAura"]=true};
    }
    static bool FxEqual(UnityEngine.Object a,UnityEngine.Object b)=>ReferenceEquals(a,b);
    static bool FxUnequal(UnityEngine.Object a,UnityEngine.Object b)=>!ReferenceEquals(a,b);
    static UnityEngine.GameObject NoFxObject(UnityEngine.Component c)=>throw new Exception("Empty FX link must not reach a renderer endpoint");
    static IEnumerable<CodeInstruction> FxBoundary(IEnumerable<CodeInstruction> input) {
        foreach(var c in input) {
            var m=c.operand as MethodInfo;
            if(m?.DeclaringType==typeof(UnityEngine.Object)&&(m.Name=="op_Equality"||m.Name=="op_Inequality")){c.opcode=OpCodes.Call;c.operand=AccessTools.Method(typeof(Arcane01446Tests),m.Name=="op_Equality"?nameof(FxEqual):nameof(FxUnequal));}
            if(m?.DeclaringType==typeof(UnityEngine.Component)&&m.Name=="get_gameObject"){c.opcode=OpCodes.Call;c.operand=AccessTools.Method(typeof(Arcane01446Tests),nameof(NoFxObject));}
            yield return c;
        }
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void SpawnNative(Kingmaker.View.MapObjects.AreaEffectView view) {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)=>FxBoundary(code);
        _=(Func<IEnumerable<CodeInstruction>,IEnumerable<CodeInstruction>>)Transpiler;
        throw new NotImplementedException("Native SpawnFxs reverse copy must replace this stub");
    }
    static void RepairPaths(BlueprintsCache parent,Harmony h) {
        var c=Fresh(parent);Register(c,0);Register(c,1);
        var rows=(string[][])AccessTools.Field(T("BloodlineAnchors01446"),"Rows").GetValue(null);
        var growth=(string[][])AccessTools.Field(T("BloodlineAnchors01446"),"Growth").GetValue(null);
        Check(growth.Length==119,"all exact native later growth anchors audited");
        foreach(var g in growth) {
            var p=Native<BlueprintProgression>(c,g[0]);var power=Native<BlueprintFeature>(c,g[1]);int level=int.Parse(g[2]);var upgrade=Native<BlueprintFeature>(c,g[3]);
            Check(!new ProgressionData(p).GetLevelEntry(level).Features.Contains(upgrade),"parent no longer grants forfeited growth "+p.name+level+upgrade.name);
            var selection=p.LevelEntries.SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Single(x=>x.AllFeatures.OfType<BlueprintProgression>().Any(b=>b.LevelEntries.Any(e=>e.Features.Contains(power))));
            var branch=selection.AllFeatures.OfType<BlueprintProgression>().Single(b=>b.LevelEntries.Any(e=>e.Features.Contains(power)));
            Check(new ProgressionData(branch).GetLevelEntry(level).Features.Contains(upgrade)&&branch.GiveFeaturesForPreviousLevels,"native original-power progression retains later grants "+p.name+level);
            Check(selection.AllFeatures.Where(x=>x!=branch).All(x=>!(x is BlueprintProgression b)||!b.LevelEntries.Any(e=>e.Features.Contains(upgrade))),"mutation branch has no original upgrade "+p.name+level);
            var cls=rows.Single(r=>r[0]==g[0])[1]=="Sorcerer"?"b3a505fb61437dc4097f43c3f8f9a4cf":"d77e67a814d686842802c9cfd8ef8499";
            var unit=Unit(c,cls,level);Check(branch.CalcLevel(unit.Descriptor)==level,"native branch calculates actual class level "+p.name+level);
        }
        var viewType=typeof(Kingmaker.View.MapObjects.AreaEffectView);var tactical=AccessTools.PropertyGetter(viewType,"TacticalCombat");h.Patch(tactical,prefix:new HarmonyMethod(Helper,"False"));
        var spawn=AccessTools.Method(viewType,"SpawnFxs");var load=AccessTools.Method(typeof(Kingmaker.ResourceLinks.WeakResourceLink<UnityEngine.GameObject>),"Load",new[]{typeof(bool),typeof(bool)});var fxBoundary=AccessTools.Method(typeof(Arcane01446Tests),nameof(FxBoundary));
        h.Patch(load,transpiler:new HarmonyMethod(fxBoundary));h.CreateReversePatcher(spawn,new HarmonyMethod(typeof(Arcane01446Tests),nameof(SpawnNative))).Patch();
        int fx=0,negative=0;
        try {foreach(string n in new[]{"Acid","Electricity","Fire","Cold"}) {
            var area=Asset<BlueprintAbilityAreaEffect>(c,"Admixture"+n+"AuraArea01446");var view=Empty<Kingmaker.View.MapObjects.AreaEffectView>();var areaRef=new BlueprintAbilityAreaEffectReference();Set(areaRef,"deserializedGuid",area.AssetGuid);Set(view,"m_Blueprint",areaRef);
            Check(area.Fx!=null&&area.Fx.AssetId=="","every aura supplies a valid empty FX link");
            var good=area.Fx;area.Fx=null;try{SpawnNative(view);throw new Exception("negative null FX did not fail");}catch(NullReferenceException){negative++;}finally{area.Fx=good;}
            SpawnNative(view);fx++;Check(Get(view,"m_SpawnedFx")==null,"real native SpawnFxs and empty WeakResourceLink.Load safely finish without particles "+n);
        }}finally{h.Unpatch(tactical,AccessTools.Method(Helper,"False"));h.Unpatch(load,fxBoundary);}
        var wizard=Unit(c,"ba34257984f4c41408ce1dc2004e342e");var target=Unit(c);var book=Book(wizard,Native<BlueprintCharacterClass>(c,"ba34257984f4c41408ce1dc2004e342e"));var fireball=Native<BlueprintAbility>(c,"2d81362af43aeac4387a3d4fced489c3");
        Fact(wizard,Asset<BlueprintFeature>(c,"AdmixtureVersatileFeature01446"));var buff=Asset<BlueprintBuff>(c,"AdmixtureFireVersatileBuff01446");var fact=Buff(wizard,buff);var mode=buff.GetComponent<ArcaneCastMode01446>();
        var aura=Asset<BlueprintBuff>(c,"AdmixtureColdAuraEffect01446");var auraFact=Buff(wizard,aura,wizard);
        var current=AccessTools.Field(Helper,"current");var old=current.GetValue(null);int order=0;
        try {foreach(int level in new[]{18,19,20}) {
            var cast=Cast(wizard,target,book,fireball,level);using(Scope(fact,mode))mode.OnEventAboutToTrigger(cast);
            var energy=new EnergyDamage(new DiceFormula(1,DiceType.D6),DamageEnergyType.Acid);var calc=Damage(cast,target,energy);var prepare=new RulePrepareDamage(calc.ParentRule);var events=new RulebookEventContext();current.SetValue(null,events);events.PushEvent(prepare);
            try{RulebookEventBus.OnEventAboutToTrigger(prepare);prepare.OnTrigger(events);RulebookEventBus.OnEventDidTrigger(prepare);}finally{events.PopEvent(prepare);}
            Check(energy.EnergyType==DamageEnergyType.Fire,"Versatile converts during actual native prepare before calculate");events.PushEvent(calc);
            try{RulebookEventBus.OnEventAboutToTrigger(calc);calc.OnTrigger(events);RulebookEventBus.OnEventDidTrigger(calc);}finally{events.PopEvent(calc);}
            Check(energy.EnergyType==(level<=19?DamageEnergyType.Cold:DamageEnergyType.Fire),"later actual aura subscriber overrides Versatile only inside level boundary "+level);order++;cast.SetSuccess(true);mode.OnEventDidTrigger(cast);
        }}finally{current.SetValue(null,old);auraFact.Remove();fact.Remove();}
        var expected="At 8th level, you can emit a 30-foot aura that transforms magical energy.";var text=(string)AccessTools.Field(T("ArcaneText01446"),"Manipulation").GetRawConstantValue();Check(text.StartsWith(expected)&&text.Contains("rounds per day equal to your wizard level")&&!text.Contains("prophecy"),"original CotW Admixture description selected from correct method");
        PsychicExpansionTests.Inventory["repair01446Contracts"]=new JObject{["passed"]=true,["nativeGrowthAnchors"]=growth.Length,["nativeFXViews"]=fx,["nullFXNegativeControls"]=negative,["nativeConversionOrderCases"]=order,["originalAuraDescription"]=true,["boundary"]="Native AreaEffectView.SpawnFxs IL executes via reverse copy and empty WeakResourceLink.Load executes; TacticalCombat query and Unity object operators/gameObject engine calls are bounded, leaving FX dereference and Load control flow intact. Native ProgressionData.GetLevelEntry and BlueprintProgression.CalcLevel inspect all119 exact continuation rows; complete level-up controller/save lifecycle is outside this fixture. Native prepare/calculate event bus and installed prefixes execute; terminal damage is stopped."};
    }
    static void Lifecycle(BlueprintsCache parent) {
        var c=Fresh(parent);Register(c,0);Register(c,1);var u=Unit(c);var target=Unit(c);var book=Book(u,Native<BlueprintCharacterClass>(c,"b3a505fb61437dc4097f43c3f8f9a4cf"));Fact(u,Asset<BlueprintFeature>(c,"BloodIntensity01446"));
        var toggle=u.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.AssetGuid==Id("BloodIntensity01446Toggle"));var pool=Asset<BlueprintAbilityResource>(c,"BloodIntensity01446Resource");int count=u.Resources.GetResourceAmount(pool);
        toggle.IsOn=true;toggle.TryStart();Check(toggle.IsStarted&&toggle.AppliedBuff!=null,"actual native toggle starts with its persistent mode buff");toggle.OnNewRound();Check(u.Resources.GetResourceAmount(pool)==count&&toggle.ResourceCount==count,"native toggle does not spend on activation/round and exposes actual daily count");
        var cast=Cast(u,target,book,Native<BlueprintAbility>(c,"2d81362af43aeac4387a3d4fced489c3"));var current=AccessTools.Field(Helper,"current");var previous=current.GetValue(null);var events=new RulebookEventContext();events.PushEvent(cast);current.SetValue(null,events);
        try {RulebookEventBus.OnEventAboutToTrigger(cast);cast.SetSuccess(true);RulebookEventBus.OnEventDidTrigger(cast);}finally{events.PopEvent(cast);current.SetValue(null,previous);}
        Check(u.Resources.GetResourceAmount(pool)==count-1&&toggle.ResourceCount==count-1,"actual native event bus delivers paid mode once and actionbar count refreshes");toggle.TurnOffImmediately();Check(!toggle.IsStarted&&!u.Buffs.Enumerable.Any(b=>b.Blueprint==Asset<BlueprintBuff>(c,"BloodIntensity01446Buff")),"native switch-off removes own mode cleanly");
        u.Stats.Charisma.BaseValue=24;u.Resources.Restore(pool,99);toggle.IsOn=true;toggle.TryStart();var mode=toggle.Blueprint.Buff.GetComponent<ArcaneCastMode01446>();var original=Cast(u,target,book,Native<BlueprintAbility>(c,"ab395d2335d3f384e99dddee8562978f"));count=u.Resources.GetResourceAmount(pool);
        using(Scope(toggle.AppliedBuff,mode))mode.OnEventAboutToTrigger(original);Check(u.Resources.GetResourceAmount(pool)==count-1,"real native Shocking Grasp wrapper charges Intensity");original.SetSuccess(true);mode.OnEventDidTrigger(original);
        var sticky=original.Spell.Blueprint.GetComponent<AbilityEffectStickyTouch>();var part=u.Ensure<Kingmaker.UnitLogic.Parts.UnitPartTouch>();part.Init(sticky.TouchDeliveryAbility,original.Spell,original.Context);Check(part.Ability.StickyTouch==original.Spell&&part.Ability.OverrideParams.Metamagic==original.Context.Params.Metamagic,"native UnitPartTouch.Init preserves paid execution params and original wrapper identity");
        var deliveryContext=new AbilityExecutionContext(part.Ability.Data,part.Ability.OverrideParams,new TargetWrapper(target),null,u);var delivery=Empty<RuleCastSpell>();Set(delivery,"Spell",part.Ability.Data);Set(delivery,"Context",deliveryContext);Set(delivery,"Initiator",u);Set(delivery,"SpellTarget",new TargetWrapper(target));using(Scope(toggle.AppliedBuff,mode))mode.OnEventAboutToTrigger(delivery);Check(u.Resources.GetResourceAmount(pool)==count-1,"native held-touch delivery does not charge a second time");toggle.TurnOffImmediately();
        var touchRank=sticky.TouchDeliveryAbility.GetComponents<ContextRankConfig>().Single(r=>(bool)Get(r,"m_AffectedByIntensifiedMetamagic"));Check(touchRank.GetValue(deliveryContext)==12,"paid held touch retains increased native cap after mode is switched off");
        var wizard=Unit(c,"ba34257984f4c41408ce1dc2004e342e");Book(wizard,Native<BlueprintCharacterClass>(c,"ba34257984f4c41408ce1dc2004e342e"));Fact(wizard,Asset<BlueprintFeature>(c,"AdmixtureAuraFeature01446"));var aura=wizard.ActivatableAbilities.Enumerable.First(a=>a.Blueprint.AssetGuid==Id("AdmixtureFireAuraToggle01446"));var rounds=Asset<BlueprintAbilityResource>(c,"AdmixtureAuraResource01446");count=wizard.Resources.GetResourceAmount(rounds);aura.IsOn=true;aura.TryStart();Check(aura.IsStarted&&aura.AppliedBuff!=null,"actual native aura carrier starts");aura.OnNewRound();Check(wizard.Resources.GetResourceAmount(rounds)==count-1,"native aura spends exactly one round");aura.TurnOffImmediately();Check(!aura.IsStarted&&!wizard.Buffs.Enumerable.Any(b=>b.Blueprint==Asset<BlueprintBuff>(c,"AdmixtureFireAuraHost01446")),"native aura stop removes carrier");
        PsychicExpansionTests.Inventory["arcaneLifecycle01446"]=new JObject{["passed"]=true,["nativeToggleStartStop"]=true,["nativeRuleEventBus"]=true,["nativeResourceCounter"]=true,["nativeHeldTouchTransfer"]=true,["nativeAuraRoundSpend"]=true,["sceneBoundary"]="Native AddAreaEffect avatar/view endpoint is false; carrier, resource, fact and event lifecycles execute. World area spawning and rendered actionbar are outside this host."};
    }
    internal static int Run(BlueprintsCache parent)
    {
        checks=serial=cacheFailures=missingFailures=connectFailures=0;Units.Clear();var h=new Harmony("CotW.Arcane01446");var game=Game.Instance;var oldState=game.State;var oldTime=game.TimeController;var originalCache=ResourcesLibrary.BlueprintsCache;
        var helperUnits=(IList)AccessTools.Field(Helper,"units").GetValue(null);int helperCount=helperUnits.Count;var current=AccessTools.Field(Helper,"current");var oldContext=current.GetValue(null);current.SetValue(null,null);
        var state=Empty<PersistentState>();var player=Empty<Player>();player.GameTime=TimeSpan.FromHours(1);Set(player,"m_Party",new List<UnitEntityData>());Set(player,"m_CharacterListsValid",true);state.PlayerState=player;Set(game,"State",state);Set(game,"TimeController",Empty<TimeController>());
        var endpoint=AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)});var diceEndpoint=AccessTools.Method(typeof(RulebookEvent.Dice),"D",new[]{typeof(DiceFormula)});var oldPatches=new[]{endpoint,diceEndpoint}.SelectMany(target=>(Harmony.GetPatchInfo(target)?.Prefixes.AsEnumerable()??Enumerable.Empty<Patch>()).Where(p=>p.PatchMethod.DeclaringType==typeof(Psychic070Tests)&&(p.PatchMethod.Name=="CaptureDefense"||p.PatchMethod.Name=="ControlledDice")).Select(p=>new{Target=target,Patch=p})).ToArray();
        var snapshotType=typeof(Repair01436CallerTests).GetNestedType("ParentSnapshot",BindingFlags.NonPublic);var snapshot=Activator.CreateInstance(snapshotType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{parent},null);
        void Scene(MethodBase method,string name)=>h.Patch(method,prefix:new HarmonyMethod(Helper,name));
        var binder=(GuidClassBinder)Kingmaker.Blueprints.JsonSystem.Json.Serializer.Binder;var toType=(Dictionary<string,Type>)Get(binder,"m_GuidToTypeCache");var toGuid=(Dictionary<Type,string>)Get(binder,"m_TypeToGuidCache");var beforeType=toType.ToArray();var beforeGuid=toGuid.ToArray();
        try {
            foreach(var type in typeof(BlueprintsCache).Assembly.GetTypes().Concat(Main.GetTypes().Where(t=>(t.Name.EndsWith("01446")||t.Name.EndsWith("01447")))))foreach(var attr in type.GetCustomAttributes(typeof(TypeIdAttribute),false).Cast<TypeIdAttribute>())binder.AddToCache(type,attr.GuidString);
            foreach(var p in oldPatches)h.Unpatch(p.Target,p.Patch.PatchMethod);
            Scene(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),"Skip");Scene(AccessTools.PropertyGetter(typeof(Game),"LevelUpController"),"Controller");Scene(AccessTools.Method(typeof(EntityFact),"Attach"),"Identity");Scene(AccessTools.Method(typeof(UnitState),"UpdateStatusEffect"),"Skip");Scene(AccessTools.Method(typeof(UnitEntityData),"UpdateGroup"),"Skip");Scene(AccessTools.PropertyGetter(typeof(TimeController),"GameTime"),"Time");Scene(AccessTools.PropertyGetter(typeof(TimeController),"RealTime"),"Time");Scene(AccessTools.PropertyGetter(typeof(Game),"CurrentMode"),"Mode");Scene(AccessTools.PropertyGetter(typeof(Rulebook),"CurrentContext"),"Context");Scene(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel),"Log",new[]{typeof(string),typeof(object[])}),"Skip");Provider01419Tests.PatchLabels(h); Scene(AccessTools.Method(typeof(Kingmaker.UnitLogic.Commands.UnitCommands),"Run",new[]{typeof(Kingmaker.UnitLogic.Commands.Base.UnitCommand)}),"Submit"); Scene(AccessTools.Method(typeof(UnitEntityData),"IsCurrentUnit"),"CurrentUnit");
            h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff),"OnTrigger"),transpiler:new HarmonyMethod(Helper,"DifficultyBoundary"));
            h.Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),transpiler:new HarmonyMethod(Helper,"RuleBoundary"));
            Scene(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx),"ExceptionWithReport",new[]{typeof(Owlcat.Runtime.Core.Logging.LogChannel),typeof(Exception),typeof(string),typeof(object[])}),"Expose");
            h.Patch(AccessTools.Method(typeof(RuleSkillCheck),"AdjustDCByDifficulty"),transpiler:new HarmonyMethod(typeof(WizardSubschools01431Tests),"SkillDifficultyBoundary"));
            // Rebuild the exact caller: a previous suite may have JIT-inlined
            // GameHelper.TriggerSkillCheck before this boundary was installed.
            // Native Demoralize checks, arithmetic and temporary modifiers stay.
            h.Patch(AccessTools.Method(typeof(BlueprintsCache),"AddCachedBlueprint"),prefix:new HarmonyMethod(typeof(Arcane01446Tests),nameof(CacheBefore)),postfix:new HarmonyMethod(typeof(Arcane01446Tests),nameof(CacheAfter)));
            h.Patch(AccessTools.Method(T("ArchetypeBatch01445"),"Definition"),prefix:new HarmonyMethod(typeof(Arcane01446Tests),nameof(DefinitionBefore)));
            Scene(AccessTools.Method(typeof(UnitEntityData),"IsAlly",new[]{typeof(UnitEntityData)}),"Ally");Scene(AccessTools.Method(typeof(AddAreaEffect),"IsOwnerAvatarValid"),"False");
            h.Patch(AccessTools.Method(typeof(UnitEntityData),"IsEnemy",new[]{typeof(UnitEntityData)}),prefix:new HarmonyMethod(typeof(Archetypes01445Tests),"Enemy"));
            Scene(AccessTools.Method(typeof(RulebookEvent.Dice),"D",new[]{typeof(DiceFormula)}),"Dice");h.Patch(AccessTools.PropertyGetter(typeof(RulebookEvent.Dice),"D20"),prefix:new HarmonyMethod(typeof(Archetypes01445Tests),"NestedD20"));
            foreach(var method in new[]{AccessTools.Method(typeof(ContextActionSavingThrow),"RunAction"),AccessTools.Method(typeof(RuleSavingThrow),"OnTrigger"),AccessTools.Method(typeof(RuleSavingThrow),"RollD20"),AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),AccessTools.Method(typeof(ContextCalculateAbilityParams),"Calculate",new[]{typeof(MechanicsContext),typeof(BlueprintScriptableObject),typeof(UnitEntityData),typeof(AbilityData)})})h.Patch(method,transpiler:new HarmonyMethod(Helper,"RuleBoundary"));
            foreach(var method in new[]{AccessTools.Method(typeof(RuleSkillCheck),"OnTrigger"),AccessTools.Method(typeof(RuleSkillCheck),"RollD20")})h.Patch(method,transpiler:new HarmonyMethod(typeof(Archetypes01445Tests),"NestedRules"));
            h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams),"OnTrigger"),transpiler:new HarmonyMethod(typeof(Migration01426Tests),"DifficultyBoundary"));
            Scene(AccessTools.PropertyGetter(typeof(UnitEntityData),"IsPlayersEnemy"),"False");Scene(AccessTools.Method(typeof(Player),"IsTurnBasedModeOn"),"False");Scene(AccessTools.Method(typeof(TurnBased.Controllers.CombatController),"IsInTurnBasedCombat"),"False");
            using(var image=Main.GetManifestResourceStream("CotW.IntensifiedSpell.png"))using(var digest=System.Security.Cryptography.SHA256.Create())Check(image!=null&&BitConverter.ToString(digest.ComputeHash(image)).Replace("-","")=="1D522811639A4177E3816F938AA2206E2508DAB83FD05E56945AC68E45300395","actual DLL embeds exact original CotW IntensifiedSpell image");
            using(var image=Main.GetManifestResourceStream("CotW.PiercingSpell.png"))using(var digest=System.Security.Cryptography.SHA256.Create())Check(image!=null&&BitConverter.ToString(digest.ComputeHash(image)).Replace("-","")=="D4B94B4BCEE1C720301602F198F268B43A2476712A0082018663619CE8AEBEAB","actual DLL embeds exact original CotW PiercingSpell image");
            if(Environment.GetEnvironmentVariable("COTW_01454_CASE")!=null)return MagusBatch01454Tests.Run(parent,h);
            if(Environment.GetEnvironmentVariable("COTW_01452_CASE")?.StartsWith("menu-external")==true)return Feedback01449Tests.External01450(parent,h);
            if(Environment.GetEnvironmentVariable("COTW_01452_CASE")?.StartsWith("menu")==true)return Feedback01449Tests.Menu01452(parent,h);
            if(Environment.GetEnvironmentVariable("COTW_01451_CASE")?.StartsWith("slots")==true)return Feedback01449Tests.Slots01451(parent,h);
            if(Environment.GetEnvironmentVariable("COTW_01450_CASE")=="nature")return Feedback01450Tests.Nature(parent,h);
            if(Environment.GetEnvironmentVariable("COTW_01450_CASE")=="paid")return PaidOnly01450(parent,h);
            if(Environment.GetEnvironmentVariable("COTW_01450_CASE")?.StartsWith("external")==true)return Feedback01449Tests.External01450(parent,h);
            if(Environment.GetEnvironmentVariable("COTW_01449_CASE")=="nature-toggle")return Feedback01449Tests.NatureToggle(parent,h);
            if(Environment.GetEnvironmentVariable("COTW_01449_CASE")=="bloodline")return Feedback01449Tests.Bloodline(parent,h,false);
            if(Environment.GetEnvironmentVariable("COTW_01449_CASE")=="bloodline-external")return Feedback01449Tests.Bloodline(parent,h,true);
            if(Environment.GetEnvironmentVariable("COTW_01449_CASE")=="bloodline-late")return Feedback01449Tests.Bloodline(parent,h,true);
            bool partial047=Environment.GetEnvironmentVariable("COTW_01447_BEHAVIOR_ONLY")=="1";
            if(!partial047)Structural(parent,h);
            bool restored=Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineExternal01450")!=null;
            bool retired=Main.GetType("CallOfTheWild.Wrath.Prototype.TomeClaws01449")!=null&&!restored;
            foreach(string patch in retired?new[]{"ArcanePrepare01446Patch"}:new[]{"BloodIntensityRank01446Patch","ArcaneDamage01446Patch","ArcanePrepare01446Patch","BloodPiercingResistance01446Patch","BloodIntensityDamageInfo01446Patch"})h.CreateClassProcessor(T(patch)).Patch();
            h.Patch(AccessTools.Method(typeof(RuleCalculateDamage),"OnTrigger"),prefix:new HarmonyMethod(typeof(Arcane01446Tests),nameof(StopDamage)){priority=Priority.Last});
            h.Patch(AccessTools.Method(typeof(RuleSpellResistanceCheck),"OnTrigger"),prefix:new HarmonyMethod(typeof(Arcane01446Tests),nameof(StopDamage)){priority=Priority.Last});
            if(!partial047){if(retired)checks+=Feedback01449Tests.Retirement(parent);else CastModes(parent,h);Admixture(parent);if(retired)checks+=Feedback01449Tests.VersatileLifecycle(parent);else Lifecycle(parent);RepairPaths(parent,h);}
            checks+=Feedback01448Tests.Run(parent,h);
            if(retired||restored)checks+=Feedback01449Tests.NatureToggle(parent,h);
            if(restored){checks+=Feedback01450Tests.Retirement(parent);checks+=Feedback01450Tests.Nature(parent,h);Composition01450(parent);}
            checks+=Repair01447Tests.Run(parent,h);checks+=SpellBatch01447Tests.Run(parent,h);

            AccessTools.Method(snapshotType,"Verify").Invoke(snapshot,null);Check(true,"original complete parent graph unchanged");
            if(!partial047)PsychicExpansionTests.Inventory["arcane01446Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["additionalItemCount"]=restored?4:retired?2:5,["newHarmonyTargets"]=retired?1:5,["retiredItemCount"]=restored?1:retired?3:0,["cacheRollbackFaults"]=cacheFailures,["connectionRollbackFaults"]=connectFailures,["normalReturnMissingOutputsRejected"]=missingFailures,["originalParentPreserved"]=true,["boundary"]="Actual latest-game blueprint graphs, native resources, feature/buff lifecycles, ContextRankConfig.GetValue and context JSON serializer execute. Native damage/SR event bodies stop after real installed production prefixes; full combat, Unity scene, full save/load and external ecosystem runtime coexistence are outside these managed checks. In01450 the user restores original Intensity/Piercing positive native casting/lifecycle plus composition; only own Havoc remains retired. Familiar/Admixture and all rollback/native lifecycle remain mandatory."};
            Console.WriteLine("PASS: "+checks+" Arcane01446 contracts.");return checks;
        } finally {
            foreach(var u in Units.AsEnumerable().Reverse())foreach(var f in u.Facts.List.ToArray())if(f.Manager!=null)u.Facts.Remove(f);
            toType.Clear();foreach(var p in beforeType)toType.Add(p.Key,p.Value);toGuid.Clear();foreach(var p in beforeGuid)toGuid.Add(p.Key,p.Value);
            h.UnpatchAll(h.Id);foreach(var saved in oldPatches){var p=saved.Patch;new Harmony(p.owner).Patch(saved.Target,prefix:new HarmonyMethod(p.PatchMethod){priority=p.priority,before=p.before,after=p.after,debug=p.debug});}Set(game,"State",oldState);Set(game,"TimeController",oldTime);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,originalCache);current.SetValue(null,oldContext);while(helperUnits.Count>helperCount)helperUnits.RemoveAt(helperUnits.Count-1);
        }
    }
}
