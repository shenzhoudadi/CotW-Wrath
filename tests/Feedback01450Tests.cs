using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;
using Newtonsoft.Json.Linq;

internal static class Feedback01450Tests
{
    static readonly Assembly Main=typeof(NaturesCommand01447).Assembly;
    static Type T(string s)=>Main.GetType("CallOfTheWild.Wrath.Prototype."+s,true);
    static object Call(Type t,string m,params object[] a)=>AccessTools.Method(t,m).Invoke(null,a);
    static object H(string m,params object[] a)=>Call(typeof(Arcane01446Tests),m,a);
    static BlueprintGuid Id(string s)=>(BlueprintGuid)Call(T("PsychicAssets"),"Id",s);
    static TAsset A<TAsset>(BlueprintsCache c,string token)where TAsset:SimpleBlueprint=>(TAsset)c.Load(Id(token));
    static TAsset N<TAsset>(BlueprintsCache c,string id)where TAsset:SimpleBlueprint=>(TAsset)c.Load(BlueprintGuid.Parse(id));
    static void Set(object o,string s,object v)=>AccessTools.Field(o.GetType(),s).SetValue(o,v);
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Feedback01450: "+why);checks++;}
    static string Root(){var p=new DirectoryInfo(Environment.CurrentDirectory);while(p!=null&&!File.Exists(Path.Combine(p.FullName,"HANDOFF.md")))p=p.Parent;return p.FullName;}
    internal static void PatchOnce(Harmony harmony,Type type)
    {
        int Expected()=>type.Name=="NatureTargetScope01448Patch"?6:type.Name=="NaturePaidTarget01448Patch"?2:1;
        int Count()=>Harmony.GetAllPatchedMethods().Sum(m=>{var info=Harmony.GetPatchInfo(m);return info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers).Count(p=>p.owner==harmony.Id&&p.PatchMethod.DeclaringType==type);});
        if(Count()==0)harmony.CreateClassProcessor(type).Patch();
        if(Count()!=Expected())throw new Exception("Exact single-owner Nature fixture patch count changed: "+type.Name+"/"+Count());
    }
    internal static int Nature(BlueprintsCache parent,Harmony h)
    {
        int before=checks;var c=(BlueprintsCache)H("Fresh",parent);
        if(c.Load(Id("PhrenicAmplificationSelection"))==null)Call(T("PsychicAmplifications"),"Register",c);
        Call(T("MagaambyanTelepath01447"),"Register",c);
        foreach(string name in new[]{"NatureSpellImmunity01447Patch","NatureBuffImmunity01447Patch","NatureAmplificationSwitch01447Patch","NatureTargetScope01448Patch","NatureTargetFact01448Patch","NaturePaidTarget01448Patch"})PatchOnce(h,T(name));
        foreach(var type in typeof(BlueprintsCache).Assembly.GetTypes().Where(t=>t.Name=="ReplaceSourceBone"||t.Name=="ReplaceCastSource"))foreach(var method in type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(m=>m.Name=="OnTurnOn"||m.Name=="OnTurnOff"))h.Patch(method,prefix:new HarmonyMethod(typeof(Feedback01450Tests),nameof(Skip)));
        var cls=N<BlueprintCharacterClass>(c,(string)AccessTools.Field(T("PsychicClass"),"ClassId").GetRawConstantValue());
        var caster=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);var plant=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);
        H("Fact",plant,N<BlueprintFeature>(c,"706e61781d692a042b35941f14bc41c5"));Set(plant,"m_Position",new UnityEngine.Vector3(3,0,0));
        var book=(Spellbook)H("Book",caster,cls);var ability=new AbilityData(A<BlueprintAbility>(c,"MindThrust1"),caster.Descriptor);Set(ability,"m_CachedSpellbook",book);
        var pool=N<BlueprintAbilityResource>(c,(string)AccessTools.Field(T("PsychicClass"),"WisdomPoolId").GetRawConstantValue());caster.Stats.Wisdom.BaseValue=24;caster.Resources.Add(pool,true);caster.Resources.Restore(pool,99);
        H("Fact",caster,A<BlueprintFeature>(c,"NaturesCommandFeature01447"));var toggle=caster.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.AssetGuid==Id("NaturesCommandToggle01447"));toggle.IsOn=true;toggle.TryStart();
        Check(ability.CanTarget(new TargetWrapper(plant)),"native Mind Thrust targeting with active mode before external preview");
        Assembly.LoadFrom(Path.Combine(Root(),"work/repair01440/references/UnityModManager.dll"));
        var pp=Assembly.LoadFrom(Path.Combine(Root(),(Main.GetType("CallOfTheWild.Wrath.Prototype.MagusBatch01454")!=null?"work/psychic01458/":Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineProvider01453")!=null?"work/repair01453/":Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineMenu01452")!=null?"work/repair01452/":Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineSlots01451")!=null?"work/repair01451/":"work/repair01450/")+"runtime-adapter/PrestigePlus.dll"));
        var log=AccessTools.Field(pp.GetType("PrestigePlus.Main",true),"Logger");
        if(log!=null){log.SetValue(null,System.Runtime.Serialization.FormatterServices.GetUninitializedObject(log.FieldType));foreach(var m in log.FieldType.GetMethods().Where(m=>m.Name=="Error"))h.Patch(m,prefix:new HarmonyMethod(typeof(Feedback01450Tests),nameof(LogError)));}
        var preview=AccessTools.Method(pp.GetType("PrestigePlus.HarmonyFix.WeirdBonusFix8",true),"Postfix");
        h.Patch(AccessTools.Method(typeof(AbilityData),nameof(AbilityData.CanTarget),new[]{typeof(TargetWrapper)}),postfix:new HarmonyMethod(preview));
        int amount=caster.Resources.GetResourceAmount(pool);
        Check(ability.CanTarget(new TargetWrapper(plant)),"actual PrestigePlus immunity preview must permit Mind Thrust against plant");
        for(int i=0;i<100;i++)Check(ability.CanTarget(new TargetWrapper(plant)),"repeated actual PrestigePlus preview stays valid "+i);
        Check(caster.Resources.GetResourceAmount(pool)==amount,"repeated target preview does not pay");
        Check(AccessTools.Field(T("NatureTargeting01448"),"Current").GetValue(null)==null,"each preview exits the exact single-installed scope");
        var resistance=plant.Get<UnitPartSpellResistance>();
        var unpaid=new AbilityExecutionContext(ability,ability.CalculateParams(),new TargetWrapper(plant),null,caster);
        Console.WriteLine("01450 unpaid control: meta="+unpaid.Params.Metamagic+" descriptor="+unpaid.SpellDescriptor+" current="+AccessTools.Field(T("NatureTargeting01448"),"Current").GetValue(null)+" target="+unpaid.MainTarget?.Unit?.UniqueId+" caster="+unpaid.MaybeCaster?.UniqueId+" immuneEntries="+((System.Collections.ICollection)AccessTools.Field(resistance.GetType(),"m_SpellImmunities").GetValue(resistance)).Count+" plant="+plant.UniqueId+" sameTarget="+(unpaid.MainTarget==new TargetWrapper(caster)));
        Check(resistance.IsImmune(unpaid,false),"unpaid immunity query outside CanTarget scope stays immune");
        Check(AccessTools.Field(T("NatureTargeting01448"),"Current").GetValue(null)==null,"native preview restores exact targeting scope");
        int extra=resistance.AddImmunity(SpellImmunityType.SpellDescriptor,0,null,SpellDescriptor.MindAffecting,null);
        Check(!ability.CanTarget(new TargetWrapper(plant)),"independent additional mind immunity still rejects actual PrestigePlus preview");resistance.Remove(extra);
        foreach(int remaining in new[]{0,1,2}){
            caster.Resources.Spend(pool,caster.Resources.GetResourceAmount(pool));caster.Resources.Restore(pool,remaining);
            Check(ability.CanTarget(new TargetWrapper(plant))==(remaining>=2),"actual preview resource threshold "+remaining);
            Check(caster.Resources.GetResourceAmount(pool)==remaining,"threshold preview is free "+remaining);
        }
        var other=new AbilityData(ability.Blueprint,caster.Descriptor);Set(other,"m_CachedSpellbook",H("Book",caster,N<BlueprintCharacterClass>(c,"ba34257984f4c41408ce1dc2004e342e")));
        Check(!other.CanTarget(new TargetWrapper(plant)),"same spell from non-Psychic book retains immunity");
        H("Book",caster,cls);
        // Keep the ordinary Plant control separate. Native AddFacts shares an
        // inherited immunity fact between the two artificial creature types;
        // removing one type would also alter that fixture's immunity lifecycle.
        var hybrid=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);
        H("Fact",hybrid,N<BlueprintFeature>(c,"706e61781d692a042b35941f14bc41c5"));H("Fact",hybrid,N<BlueprintFeature>(c,"fd389783027d63343b4a5634bd81645f"));
        Check(!ability.CanTarget(new TargetWrapper(hybrid)),"plant plus Construct retains actual Mind Thrust negative target restriction");
        Check(ability.CanTarget(new TargetWrapper(plant)),"independent ordinary Plant control remains eligible");
        toggle.TurnOffImmediately();Check(!ability.CanTarget(new TargetWrapper(plant)),"inactive mode preserves plant immunity in actual preview");
        PsychicExpansionTests.Inventory["naturePreview01450Contracts"]=new JObject{["passed"]=true,["actualPrestigePlusPostfix"]=true,["actualMindThrustCanTarget"]=true,["noPreviewPayment"]=true,["unpaidExecutionRetainsImmunity"]=true,["independentImmunityRetained"]=true,["constructRetained"]=true,["resourceThresholds"]=new JArray(0,1,2),["wrongBookRetained"]=true,["scopeRestored"]=true,["assertions"]=checks-before,["boundary"]="Actual released PrestigePlus3.8.0 postfix and native CanTarget/IsImmune execute. Only same-member private access is adapted for standalone CLR. No UI rendering, scene targeting or full game startup is claimed."};
        return checks-before;
    }
    static bool Skip()=>false;
    static bool LogError(string __0,Exception __1){throw new Exception("Actual PrestigePlus preview failed: "+__0,__1);}
    internal static int Retirement(BlueprintsCache parent)
    {
        int before=checks;var c=(BlueprintsCache)H("Fresh",parent);Call(T("BloodlineMutations01446"),"Register",c);
        Check(c.Load(Id("BloodHavoc01446"))==null,"CotW Havoc remains removed; ECM owns the playable Havoc");
        foreach(string token in new[]{"BloodIntensity01446","BloodPiercing01446"})foreach(string suffix in new[]{"","Buff","Toggle","Resource"})Check(c.Load(Id(token+suffix))!=null,"explicitly restored asset is published: "+token+suffix);
        foreach(string name in new[]{"BloodIntensityRank01446Patch","ArcaneDamage01446Patch","BloodPiercingResistance01446Patch","BloodIntensityDamageInfo01446Patch"})Check(T(name)!=null,"restored actual mechanic patch is present: "+name);
        var cls=N<BlueprintCharacterClass>(c,"b3a505fb61437dc4097f43c3f8f9a4cf");var owner=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);var victim=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);var book=(Spellbook)H("Book",owner,cls);
        var cast=(Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell)H("Cast",owner,victim,book,N<BlueprintAbility>(c,"2d81362af43aeac4387a3d4fced489c3"),19);
        var damage=new Kingmaker.RuleSystem.Rules.Damage.EnergyDamage(new Kingmaker.RuleSystem.DiceFormula(4,Kingmaker.RuleSystem.DiceType.D6),Kingmaker.Enums.Damage.DamageEnergyType.Fire);
        var rule=(Kingmaker.RuleSystem.Rules.Damage.RuleCalculateDamage)H("Damage",cast,victim,new Kingmaker.RuleSystem.Rules.Damage.BaseDamage[]{damage});new BloodHavoc01446().OnEventAboutToTrigger(rule);Check(damage.Bonus==0,"legacy CotW Havoc component remains inert in the actual damage handler path");

        Check((int)AccessTools.Field(T("TopplingSpell"),"Metamagic").GetRawConstantValue()==1<<28,"Toppling bit28 unchanged");
        foreach(string name in new[]{"MutationStamp","Intensity","Piercing"})Check(((int)AccessTools.Field(T("ArcaneCast01446"),name).GetRawConstantValue()&(1<<28))==0,"restored execution field never uses Toppling bit28: "+name);
        PsychicExpansionTests.Inventory["bloodlineRetirement01449Contracts"]=new JObject{["passed"]=true,["retiredAssets"]=new JArray("BloodHavoc01446"),["retiredAssetCount"]=1,["legacyHavocInert"]=true,["topplingUnchanged"]=true,["supersededByUser01450"]=new JArray("Blood Intensity","Blood Piercing"),["reason"]="User explicitly restored Intensity/Piercing after confirming ECM placeholders. Their original positive native casting/lifecycle contracts and the new composition/mask contract are mandatory; CotW Havoc remains absent."};
        return checks-before;
    }
}
