using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using CallOfTheWild.Wrath.Prototype;

internal static class Repair01447Tests
{
    static readonly Type H=typeof(Arcane01446Tests);
    static readonly Assembly Main=typeof(ArcaneCastMode01446).Assembly;
    static Type T(string n)=>Main.GetType("CallOfTheWild.Wrath.Prototype."+n,true);
    static object Call(Type t,string m,params object[] args)=>AccessTools.Method(t,m).Invoke(null,args);
    static object Call(string m,params object[] a)=>Call(H,m,a);
    static object Get(object o,string n)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).GetValue(o);
    static void Set(object o,string n,object v)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).SetValue(o,v);
    static BlueprintGuid Id(string s)=>(BlueprintGuid)Call(T("PsychicAssets"),"Id",s);
    static TAsset A<TAsset>(BlueprintsCache c,string token)where TAsset:SimpleBlueprint=>(TAsset)c.Load(Id(token));
    static TAsset N<TAsset>(BlueprintsCache c,string id)where TAsset:SimpleBlueprint=>(TAsset)c.Load(BlueprintGuid.Parse(id));
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Repair01447: "+why);checks++;}
    static string Root() {var p=new DirectoryInfo(Environment.CurrentDirectory);while(p!=null&&!File.Exists(Path.Combine(p.FullName,"HANDOFF.md")))p=p.Parent;return p?.FullName??throw new Exception("Project evidence root missing");}
    static IDisposable Begin(AbilityData a)=>(IDisposable)Call(T("AdmixtureDescriptors01447"),"Begin",a);
    static void Descriptor(BlueprintsCache parent,Harmony h) {
        foreach(string patch in new[]{"AdmixtureParams01447Patch","AdmixtureAbilityDescriptor01447Patch","AdmixtureBlueprintDescriptor01447Patch"})h.CreateClassProcessor(T(patch)).Patch();
        var c=(BlueprintsCache)Call("Fresh",parent);Call(T("Admixture01446"),"Register",c);
        var u=(UnitEntityData)Call("Unit",c,"ba34257984f4c41408ce1dc2004e342e",19);var cls=N<BlueprintCharacterClass>(c,"ba34257984f4c41408ce1dc2004e342e");var book=(Spellbook)Call("Book",u,cls);
        Call("Fact",u,A<BlueprintFeature>(c,"AdmixtureVersatileFeature01446"));
        var bp=N<BlueprintAbility>(c,"2d81362af43aeac4387a3d4fced489c3");
        var a=new AbilityData(bp,u.Descriptor);Set(a,"m_CachedSpellbook",book);
        var native=bp.SpellDescriptor;var pool=A<BlueprintAbilityResource>(c,"AdmixtureVersatileResource01446");
        var fire=new IncreaseSpellDescriptorCasterLevel{Descriptor=SpellDescriptor.Fire,BonusCasterLevel=3,SpellsOnly=true,ModifierDescriptor=ModifierDescriptor.UntypedStackable};
        var cold=new IncreaseSpellDescriptorCasterLevel{Descriptor=SpellDescriptor.Cold,BonusCasterLevel=7,SpellsOnly=true,ModifierDescriptor=ModifierDescriptor.UntypedStackable};
        var dc=new IncreaseSpellDescriptorDC{Descriptor=SpellDescriptor.Cold,BonusDC=2,SpellsOnly=true,ModifierDescriptor=ModifierDescriptor.UntypedStackable};
        fire.name="FireCL";cold.name="ColdCL";dc.name="ColdDC";
        var feature=new BlueprintFeature{AssetGuid=Id("DescriptorFixture01447"),name="DescriptorFixture01447",ComponentsArray=new BlueprintComponent[]{fire,cold,dc}};foreach(var part in feature.ComponentsArray)part.OwnerBlueprint=feature;
        var ff=(Feature)Call("Fact",u,feature);var before=a.CalculateParams();
        var buff=Call("Buff",u,A<BlueprintBuff>(c,"AdmixtureColdVersatileBuff01446"),null);
        var after=a.CalculateParams();        Check(after.CasterLevel==before.CasterLevel+4&&after.DC==before.DC+2,"real AbilityData.CalculateParams removes old Fire CL and applies new Cold CL/DC via native subscribers");
        Check(a.SpellDescriptor==SpellDescriptor.Cold&&bp.SpellDescriptor==native,"effective ability descriptor changes without shared-blueprint mutation");
        Check(u.Get<UnitPartChangeSpellElementalDamage>()==null,"native conversion part removed after actual calculation");
        var nativePart=u.Ensure<UnitPartChangeSpellElementalDamage>();nativePart.Entries.Add(new UnitPartChangeSpellElementalDamage.SpellDescriptorEntry(bp.AssetGuid.ToString(),SpellDescriptor.Acid));
        var prior=nativePart.Entries.ToArray();
        try {using(Begin(a)){Check(nativePart.ActualSpellDescriptor==SpellDescriptor.Cold,"conversion reaches native descriptor service");var other=new AbilityData(N<BlueprintAbility>(c,"c7104f7526c4c524f91474614054547e"),u.Descriptor);using(Begin(other))Check(nativePart.Entries.SequenceEqual(prior),"nested ineligible calculation suspends only our entry");Check(nativePart.ActualSpellDescriptor==SpellDescriptor.Cold,"nested calculation restores outer mode");throw new InvalidOperationException("exception cleanup fixture");}}
        catch(InvalidOperationException){}
        Check(nativePart.Entries.SequenceEqual(prior),"exception and nested calculations preserve external descriptor entries and order");u.Remove<UnitPartChangeSpellElementalDamage>();
        int count=u.Resources.GetResourceAmount(pool);for(int i=0;i<5;i++)a.CalculateParams();Check(u.Resources.GetResourceAmount(pool)==count,"parameter previews never spend resource");
        u.Resources.Spend(pool,count);Check(a.SpellDescriptor==native&&a.CalculateParams().CasterLevel==before.CasterLevel,"exhausted mode exposes original descriptors and CL");u.Resources.Restore(pool,99);
        var helper=typeof(BardMasterpieces01435Tests);var current=AccessTools.Field(helper,"current");var old=current.GetValue(null);
        // Exercise TTT's actual released AddContextCasterLevelBonus component,
        // the component used by Elemental Bond, without installing the mod.
        var ttt=Assembly.LoadFrom(Path.Combine(Root(),"work/repair01440/dependencies/TabletopTweaks-Core/TabletopTweaks-Core/TabletopTweaks-Core.dll"));
        var ct=ttt.GetType("TabletopTweaks.Core.NewComponents.AddContextCasterLevelBonus",true);var component=(BlueprintComponent)Activator.CreateInstance(ct);
        component.name="TTTElementalBond";
        Set(component,"Descriptor",(SpellDescriptorWrapper)SpellDescriptor.Cold);Set(component,"SpellsOnly",true);Set(component,"BonusCasterLevel",(ContextValue)5);Set(component,"ModifierDescriptor",ModifierDescriptor.UntypedStackable);
        var tf=new BlueprintFeature{AssetGuid=Id("ElementalBondFixture01447"),name="ElementalBondFixture01447",ComponentsArray=new[]{component}};
        var ttf=(Feature)Call("Fact",u,tf);var tparams=a.CalculateParams();Check(tparams.CasterLevel==after.CasterLevel+5,"real TTT Elemental Bond component sees converted descriptor during native parameter calculation");
        AccessTools.Method(buff.GetType(),"Remove").Invoke(buff,null);
        Check(a.SpellDescriptor==native,"switching off conversion restores effective spell descriptor");
        Set(component,"Descriptor",(SpellDescriptorWrapper)SpellDescriptor.Fire);
        var cone=N<BlueprintAbility>(c,"e7c530f8137630f4d9d7ee1aa7b1edc0");var ca=new AbilityData(cone,u.Descriptor);Set(ca,"m_CachedSpellbook",book);var coneNative=ca.CalculateParams();
        foreach(string name in new[]{"Acid","Electricity","Fire","Cold"}) {
            var bf=(Kingmaker.UnitLogic.Buffs.Buff)Call("Buff",u,A<BlueprintBuff>(c,"Admixture"+name+"VersatileBuff01446"),null);var descriptor=(SpellDescriptor)Enum.Parse(typeof(SpellDescriptor),name);
            Check(ca.SpellDescriptor==descriptor&&cone.SpellDescriptor==SpellDescriptor.Cold,"real Cone of Cold conversion to "+name+" preserves shared blueprint");
            if(name=="Fire") {var converted=ca.CalculateParams();Check(converted.CasterLevel==coneNative.CasterLevel+1,"actual Cold-to-Fire Cone gains native Fire3 and actual TTT Fire5, losing old Cold7");
                u.Resources.Spend(pool,u.Resources.GetResourceAmount(pool)-1);var evt=(RuleCastSpell)Call("Cast",u,u,book,cone,19);var mode=bf.Blueprint.GetComponent<ArcaneCastMode01446>();using((IDisposable)Call("Scope",bf,mode))mode.OnEventAboutToTrigger(evt);Check(u.Resources.GetResourceAmount(pool)==0&&evt.Context.SpellDescriptor==SpellDescriptor.Fire,"last paid use keeps converted execution descriptor after resource reaches zero");evt.SetSuccess(true);mode.OnEventDidTrigger(evt);u.Resources.Restore(pool,99);}
            bf.Remove();
        }
        Check((SpellDescriptor)Call(T("AdmixtureDescriptors01447"),"Replace",SpellDescriptor.Fire|SpellDescriptor.Cold|SpellDescriptor.MindAffecting,2)==(SpellDescriptor.Fire|SpellDescriptor.MindAffecting),"replace removes every original element but preserves non-element descriptors");

    }
    static void Bloodlines(BlueprintsCache parent) {
        var ecm=Assembly.LoadFrom(Path.Combine(Root(),"work/batch01447/dependencies/ECM/EbonsContentMod.dll"));
        var componentType=ecm.GetType("EbonsContentMod.Components.BloodHavoc",true);var registration=ecm.GetType("EbonsContentMod.Bloodlines.BloodlineMutations",true);
        var incomplete=(BlueprintsCache)Call("Fresh",parent);var primalRoot=incomplete.Load(Id("PrimalSorcererArchetype01444"));var nativeBefore=N<BlueprintProgression>(incomplete,"7bd143ead2d6c3a409aad6ee22effe34").LevelEntries;incomplete.RemoveCachedBlueprint(primalRoot.AssetGuid);bool waiting=false;try{Call(T("BloodlineMutations01446"),"Register",incomplete);}catch(TargetInvocationException e){waiting=e.InnerException is InvalidOperationException&&e.InnerException.Message.Contains("producer");}Check(waiting&&ReferenceEquals(nativeBefore,N<BlueprintProgression>(incomplete,"7bd143ead2d6c3a409aad6ee22effe34").LevelEntries)&&incomplete.Load(Id("BloodHavoc01446"))==null,"failed Primal producer blocks mutation publication without touching clone inputs");incomplete.AddCachedBlueprint(primalRoot.AssetGuid,primalRoot);Call(T("BloodlineMutations01446"),"Register",incomplete);Check(incomplete.Load(Id("BloodHavoc01446"))!=null,"restored producer permits safe mutation retry");
        var c=(BlueprintsCache)Call("Fresh",parent);var p=N<BlueprintProgression>(c,"7bd143ead2d6c3a409aad6ee22effe34");var power=N<BlueprintFeature>(c,"b80b73ef73535d54fbf76654c32f606b");
        var provider=new BlueprintFeature{AssetGuid=BlueprintGuid.Parse("5d61fcd8c3d545da9f97e1b2fda8218b"),name="BloodHavocBloodlineMutation",ComponentsArray=new[]{(BlueprintComponent)Activator.CreateInstance(componentType,true)}};provider.ComponentsArray[0].name="ECMHavoc";provider.ComponentsArray[0].OwnerBlueprint=provider;c.AddCachedBlueprint(provider.AssetGuid,provider);
        var guid=(string)Call(registration,"CreateDeterministicGuid","EbonsContentMod.BloodlineMutationSelection."+p.AssetGuid+".Level1");
        var wrapper=new BlueprintFeatureSelection{AssetGuid=BlueprintGuid.Parse(new Guid(guid).ToString("N")),name=p.name+"Level1BloodlineMutationSelection",ComponentsArray=Array.Empty<BlueprintComponent>()};
        foreach(string field in new[]{"m_Features","m_AllFeatures"})Set(wrapper,field,new[]{BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(power),BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(provider)});c.AddCachedBlueprint(wrapper.AssetGuid,wrapper);
        p.LevelEntries=p.LevelEntries.Select(e=>(Kingmaker.Blueprints.Classes.LevelEntry)Call(T("ArchetypeBatch01445"),"Entry",e.Level,e.Features.Select(x=>x==power?wrapper:x).ToArray())).ToArray();
        var pool=N<BlueprintFeatureSelection>(c,"3a60f0c0442acfb419b0c03b584e1394");foreach(string field in new[]{"m_Features","m_AllFeatures"})Set(pool,field,((BlueprintFeatureReference[])Get(pool,field)).Concat(new[]{BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(provider)}).ToArray());
        Call(T("BloodlineMutations01446"),"Register",c);var havoc=A<BlueprintFeature>(c,"BloodHavoc01446");
        var choice=p.LevelEntries.Single(e=>e.Level==1).Features.OfType<BlueprintFeatureSelection>().Single(s=>s.AllFeatures.Contains(havoc));
        Check(!p.LevelEntries.Single(e=>e.Level==1).Features.Contains(wrapper)&&choice.AllFeatures.Count(x=>x==havoc)==1&&!choice.AllFeatures.Contains(provider),"real ECM wrapper shape merges into one CotW mutation choice without duplicate Havoc");
        Check(pool.AllFeatures.Contains(havoc)&&!pool.AllFeatures.Contains(provider)&&ReferenceEquals(c.Load(provider.AssetGuid),provider)&&ReferenceEquals(c.Load(wrapper.AssetGuid),wrapper),"canonical feat entry replaces duplicate UI while retaining external assets for saves");
        var target=AccessTools.Method(componentType,"OnEventAboutToTrigger",new[]{typeof(Kingmaker.RuleSystem.Rules.Damage.RuleCalculateDamage)});var guard=(string)AccessTools.Field(T("BloodlineCompatibility01447"),"PatchOwner").GetRawConstantValue();
        for(int i=0;i<5;i++)Call(T("BloodlineMutations01446"),"Register",c);
        Check(Harmony.GetPatchInfo(target).Prefixes.Count(x=>x.owner==guard)==1,"actual ECM damage prefix is installed exactly once across successful reentry");
        foreach(string field in new[]{"m_Features","m_AllFeatures"})Set(pool,field,((BlueprintFeatureReference[])Get(pool,field)).Concat(new[]{BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(provider)}).ToArray());
        Call(T("BloodlineCompatibility01447"),"Reconcile",c);Check(!pool.AllFeatures.Contains(provider)&&pool.AllFeatures.Count(x=>x==havoc)==1,"late ECM public Havoc is reconciled without duplicate option or patch");
        var u=(UnitEntityData)Call("Unit",c,"b3a505fb61437dc4097f43c3f8f9a4cf",19);var victim=(UnitEntityData)Call("Unit",c,"b3a505fb61437dc4097f43c3f8f9a4cf",19);var book=(Spellbook)Call("Book",u,N<BlueprintCharacterClass>(c,"b3a505fb61437dc4097f43c3f8f9a4cf"));
        var ownFact=(Feature)Call("Fact",u,havoc);var otherFact=(Feature)Call("Fact",u,provider);var hc=havoc.GetComponent<BloodHavoc01446>();var fireball=N<BlueprintAbility>(c,"2d81362af43aeac4387a3d4fced489c3");int index=Array.FindIndex(hc.Spells,s=>s.Guid==fireball.AssetGuid);Call("Fact",u,(BlueprintFeature)hc.Grants[index].Get());
        var cast=(RuleCastSpell)Call("Cast",u,victim,book,fireball,19);var damage=new Kingmaker.RuleSystem.Rules.Damage.EnergyDamage(new DiceFormula(4,DiceType.D6),Kingmaker.Enums.Damage.DamageEnergyType.Fire);var rule=(Kingmaker.RuleSystem.Rules.Damage.RuleCalculateDamage)Call("Damage",cast,victim,new Kingmaker.RuleSystem.Rules.Damage.BaseDamage[]{damage});
        using((IDisposable)Call("Scope",ownFact,hc))hc.OnEventAboutToTrigger(rule);
        var external=otherFact.Components.Single();using(external.RequestEventContext())target.Invoke(external.SourceBlueprintComponent,new object[]{rule});
        Check(damage.Bonus==4,"actual ECM and CotW damage handlers award four dice once when both saved facts exist");

        u.Facts.Remove(ownFact);var secondDamage=new Kingmaker.RuleSystem.Rules.Damage.EnergyDamage(new DiceFormula(4,DiceType.D6),Kingmaker.Enums.Damage.DamageEnergyType.Fire);var secondRule=(Kingmaker.RuleSystem.Rules.Damage.RuleCalculateDamage)Call("Damage",cast,victim,new Kingmaker.RuleSystem.Rules.Damage.BaseDamage[]{secondDamage});using(external.RequestEventContext())target.Invoke(external.SourceBlueprintComponent,new object[]{secondRule});Check(secondDamage.Bonus==4,"ECM-only saved Havoc uses complete canonical bloodline spell coverage once");
        var exclusion=havoc.GetComponent<BloodHavocProviders01447>();Check(!(bool)AccessTools.Method(typeof(BloodHavocProviders01447),"CheckInternal").Invoke(exclusion,new object[]{null,u.Descriptor,null}),"dynamic prerequisite rejects own Havoc when a late ECM saved fact already exists");
        var clean=(BlueprintsCache)Call("Fresh",parent);if(clean.Load(Id("PrimalSorcererArchetype01444"))==null)Call(T("PrimalSorcerer01444"),"Register",clean);Call(T("BloodlineMutations01446"),"Register",clean);
        var rows=(string[][])Call(T("BloodlineCompatibility01447"),"Rows",clean);var seeker=(string[][])AccessTools.Field(T("BloodlineVariants01447"),"Rows").GetValue(null);Check(seeker.Length==21&&seeker.All(r=>r[3]==""),"all21 actual Seeker bloodlines retain only real replaceable powers and never recreate removed level3 powers");
        foreach(var row in seeker)foreach(var level in new[]{1,9})if(row[level==1?2:4]!="")Check(N<BlueprintProgression>(clean,row[0]).LevelEntries.Single(e=>e.Level==level).Features.OfType<BlueprintFeatureSelection>().Any(s=>s.AllFeatures.Contains(A<BlueprintFeature>(clean,level==1?"BloodHavoc01446":"BloodPiercing01446"))),"native Seeker mutation choice present: "+row[0]+"/"+level);
        foreach(string element in new[]{"Air","Earth","Fire","Water"}) {var primal=A<BlueprintProgression>(clean,"Primal"+element+"Progression01444");Check(primal.LevelEntries.Single(e=>e.Level==9).Features.OfType<BlueprintFeatureSelection>().Any(s=>s.AllFeatures.Any(x=>x.AssetGuid==Id("Primal"+element+"SummoningFeature01444"))&&s.AllFeatures.Contains(A<BlueprintFeature>(clean,"BloodPiercing01446"))),"Primal actual replacement summoning power remains one alternative: "+element);}
        new Harmony(guard).UnpatchAll(guard);
        PsychicExpansionTests.Inventory["bloodlineCompatibility01447Contracts"]=new JObject{["passed"]=true,["seekerBloodlines"]=21,["primalBloodlines"]=4,["actualECMComponent"]=true,["actualECMGuidFunction"]=true,["singleDamageBonus"]=true,["lateOptionReconciliation"]=true,["boundary"]="ECM v1.1.9 release DLL component and deterministic GUID function execute. Wrapper and feat-pool edits follow that DLL's checked Configure IL; the complete BlueprintCore configurator and full external startup are outside this fixture."};
    }
    internal static int Run(BlueprintsCache parent,Harmony h) {
        checks=0;Descriptor(parent,h);int sharedProvider=0;
        if(Main.GetType("CallOfTheWild.Wrath.Prototype.TomeClaws01449")==null)Bloodlines(parent);
        else {var provider=PsychicExpansionTests.Inventory["ecmMutations01449Contracts"] as JObject??throw new Exception("Required01449 actual provider evidence missing");Check(provider["passed"].Value<bool>(),"actual ECM-authoritative replacement contract passed");sharedProvider=provider["assertions"].Value<int>();}
        PsychicExpansionTests.Inventory["repair01447Contracts"]=new JObject{["passed"]=true,["assertions"]=checks+sharedProvider,["localDescriptorAssertions"]=checks,["sharedProviderAssertions"]=sharedProvider,["sharedProviderEvidence"]=sharedProvider==0?null:"ecmMutations01449Contracts",["earlyNativeParameters"]=true,["actualTTTComponent"]=true};
        return checks;
    }
}
