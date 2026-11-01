using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static partial class Archetypes01445Tests
{
    static bool Suitable(ref bool __result){__result=true;return false;}
    static void DustInvisibility(BlueprintsCache c,Harmony h)
    {
        int begin=checks;
        var owner=Unit(c,3,10);var target=Unit(c,3,10);
        Set(owner,"Stealth",new UnitStealth());
        var priorHostile=hostile;hostile=target;
        var dust=Asset<BlueprintAbility>(c,"GraveWardenDustbringerAbility01445");
        var invis=Native<BlueprintBuff>(c,"525f980cb29bc2240b93e953974cb325");
        var controller=new OffensiveActionsController();
        h.Patch(AccessTools.Method(typeof(OffensiveActionsController),"IsSuitableUnit"),prefix:new HarmonyMethod(typeof(Archetypes01445Tests),nameof(Suitable)));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.EntitySystem.EntityDataBase),"IsInGame"),prefix:new HarmonyMethod(typeof(Archetypes01445Tests),nameof(Suitable)));
        try {
            Check(dust.NotOffensive,"Dustbringer preparation has native nonoffensive flag");
            var b=Buff(owner,invis);Check(!b.Blueprint.GetComponent<BuffInvisibility>().NotDispellAfterOffensiveAction,"actual ordinary invisibility is breakable");
            var data=new AbilityData(dust,owner.Descriptor);
            var context=new AbilityExecutionContext(data,new AbilityParams(),new TargetWrapper(target),null,owner);
            controller.OnTryToApplyAbilityEffect(context,new TargetWrapper(target));
            Check(!b.IsDisposed,"native offensive controller leaves ordinary invisibility during preparation");
            using(context.GetDataScope(target))dust.GetComponent<AbilityEffectRunAction>().Actions.Run();
            Check(target.Buffs.Enumerable.Any(x=>x.Blueprint.AssetGuid==Id("GraveWardenDustbringerMark01445"))&&!b.IsDisposed,"actual preparation applies mark without breaking invisibility");
            dust.NotOffensive=false;
            try{controller.OnTryToApplyAbilityEffect(context,new TargetWrapper(target));Check(b.IsDisposed,"negative control: old flag raises native offensive event and breaks invisibility");}
            finally{dust.NotOffensive=true;}
            var next=Buff(owner,invis);
            Call(typeof(OffensiveActionsController),"RaiseEvents",owner,target);
            Check(next.IsDisposed,"subsequent actual offensive action still ends ordinary invisibility");
        }finally{hostile=priorHostile;h.Unpatch(AccessTools.Method(typeof(OffensiveActionsController),"IsSuitableUnit"),AccessTools.Method(typeof(Archetypes01445Tests),nameof(Suitable)));h.Unpatch(AccessTools.PropertyGetter(typeof(Kingmaker.EntitySystem.EntityDataBase),"IsInGame"),AccessTools.Method(typeof(Archetypes01445Tests),nameof(Suitable)));}
        PsychicExpansionTests.Inventory["dustInvisibility01445Contracts"]=new JObject{["passed"]=true,["assertions"]=checks-begin,["nativeOffensiveController"]=true,["oldFlagNegativeControl"]=true,["preparationPreservesInvisibility"]=true,["attackStillBreaksInvisibility"]=true};
    }
    static void SymbiosisStacking(BlueprintsCache c)
    {
        int begin=checks,cases=0;var sym=Asset<BlueprintFeature>(c,"NatureBondedMagusSymbiosis01445");
        var lizard=Native<BlueprintFeature>(c,"61aeb92c176193e48b0c9c50294ab290");
        foreach(bool before in new[]{true,false})foreach(int rank in new[]{1,2,3,4,5}) {
            var u=Unit(c,1,4);int ac=u.Stats.AC.ModifiedValue;
            Feature familiar=before?Fact(u,lizard):null;
            var sf=new Feature(sym,u.Descriptor);sf.SetRank(rank);u.Facts.Add(sf);
            if(!before)familiar=Fact(u,lizard);
            Check(u.Stats.AC.ModifiedValue==ac+rank,"Symbiosis/lizard take highest in either grant order "+before+rank);
            var extra=u.Stats.AC.AddModifier(8,ModifierDescriptor.NaturalArmor);
            Check(u.Stats.AC.ModifiedValue==ac+9,"stronger existing native bonuses suppress only Symbiosis, retain other sources");
            u.Stats.AC.RemoveModifier(extra);Check(u.Stats.AC.ModifiedValue==ac+rank,"removing stronger armor restores shortfall");
            var enhancement=u.Stats.AC.AddModifier(3,ModifierDescriptor.NaturalArmorEnhancement);
            Check(u.Stats.AC.ModifiedValue==ac+rank+3,"natural armor enhancement still stacks");
            u.Stats.AC.RemoveModifier(enhancement);
            var force=u.Stats.AC.AddModifier(new ModifiableValue.Modifier{ModValue=2,ModDescriptor=ModifierDescriptor.NaturalArmor,StackMode=ModifiableValue.StackMode.ForceStack});
            Check(u.Stats.AC.ModifiedValue==ac+rank+2,"explicitly stacking improvement still stacks");
            u.Stats.AC.RemoveModifier(force);u.Facts.Remove(familiar);Check(u.Stats.AC.ModifiedValue==ac+rank,"removing familiar restores full Symbiosis");
            for(int i=0;i<rank;i++)u.Facts.Remove(sf);
            Check(u.Stats.AC.ModifiedValue==ac,"removal leaves no armor residue or listener contribution");cases++;
        }
        // Native policy used when TTT disables natural-armor stacking.
        bool was=ModifierDescriptorHelper.DefaultStackingDescriptors.Remove(ModifierDescriptor.NaturalArmor);
        try{var u=Unit(c,1,7);int ac=u.Stats.AC.ModifiedValue;Fact(u,lizard);var sf=new Feature(sym,u.Descriptor);sf.SetRank(2);u.Facts.Add(sf);Check(u.Stats.AC.ModifiedValue==ac+2,"native nonstacking policy compares full Symbiosis, no double subtraction");u.Facts.Remove(sf);u.Facts.Remove(sf);Check(u.Stats.AC.ModifiedValue==ac+1,"nonstacking removal preserves familiar");}
        finally{if(was)ModifierDescriptorHelper.DefaultStackingDescriptors.Add(ModifierDescriptor.NaturalArmor);}
        PsychicExpansionTests.Inventory["symbiosisStacking01445Contracts"]=new JObject{["passed"]=true,["assertions"]=checks-begin,["rankAndOrderCases"]=cases,["nativeLizardFamiliar"]=true,["nonstackingPolicy"]=true,["otherSourcesUnchanged"]=true};
    }
    static void KiFeintTriggers(BlueprintsCache c)
    {
        int begin=checks;
        foreach(bool cloak in new[]{false,true})foreach(bool useKi in new[]{false,true}) {
            var u=Unit(c,2,10);var target=Unit(c,2,10);
            var feint=Fact(u,Native<BlueprintFeature>(c,"c610310d31414edabcedf0c8a6fe32c4"));
            var ki=Fact(u,Asset<BlueprintFeature>(c,"SageCounselorDeceptiveKi01445"));
            if(cloak)Fact(u,Native<BlueprintFeature>(c,"927905afa09f4ccd9325c5051eddaa40"));
            var ability=useKi?Asset<BlueprintAbility>(c,"SageCounselorKiFeint01445"):Native<BlueprintAbility>(c,"1bb6f0b196aa457ba80bdb312dc64952");
            var context=new AbilityExecutionContext(new AbilityData(ability,u.Descriptor),new AbilityParams(),new TargetWrapper(target),null,u);
            int before=u.Stats.CheckBluff.ModifiedValue;
            foreach(var fact in new[]{feint,ki})foreach(var t in fact.Blueprint.GetComponents<AddAbilityUseTrigger>())
                using(Runtime(fact,t).RequestEventContext())t.OnEventAboutToTrigger(new Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell(context.Ability,new TargetWrapper(target)));
            Check(u.Stats.CheckBluff.ModifiedValue==before+(cloak?4:2),"native Feint pre-trigger applies exactly +2 plus cloak +2: "+cloak+useKi);
            foreach(var fact in new[]{feint,ki})foreach(var t in fact.Blueprint.GetComponents<AddAbilityUseTrigger>())
                using(Runtime(fact,t).RequestEventContext())t.OnAbilityEffectApplied(context);
            Check(u.Stats.CheckBluff.ModifiedValue==before,"native Feint post-trigger removes both temporary bonuses "+cloak+useKi);
            var other=new AbilityExecutionContext(new AbilityData(Asset<BlueprintAbility>(c,"SageCounselorDeceptiveKiAbility01445"),u.Descriptor),new AbilityParams(),new TargetWrapper(u),null,u);
            foreach(var fact in new[]{feint,ki})foreach(var t in fact.Blueprint.GetComponents<AddAbilityUseTrigger>())
                using(Runtime(fact,t).RequestEventContext())t.OnEventAboutToTrigger(new Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell(other.Ability,new TargetWrapper(u)));
            Check(u.Stats.CheckBluff.ModifiedValue==before,"unrelated ability does not receive Feint bonuses");
        }
        PsychicExpansionTests.Inventory["kiFeintTriggers01445Contracts"]=new JObject{["passed"]=true,["assertions"]=checks-begin,["nativePreAndPostTriggers"]=true,["nativeAndKiWithAndWithoutCloak"]=true,["temporaryCleanup"]=true,["nativeFeatureUnmodified"]=true};
    }
    static void DrillLatePool(BlueprintsCache c,Harmony h)
    {
        int begin=checks;
        var pool=Native<BlueprintFeatureSelection>(c,"d87e2f6a9278ac04caeb0f93eff95fcb");
        var root=Asset<BlueprintArchetype>(c,"DrillSergeantArchetype01445");
        var tactician=Asset<BlueprintFeatureSelection>(c,"DrillSergeantTactician01445");
        var owner=Unit(c,4,17);Fact(owner,tactician);var target=Unit(c,4,17);
        var old=(BlueprintFeatureReference[])Get(pool,"m_AllFeatures");
        var late=new BlueprintFeature{AssetGuid=Id("Repair01445LateTeamwork"),name="Repair01445LateTeamwork"};
        Set(late,"m_Icon",tactician.Icon);late.OnEnable();c.AddCachedBlueprint(late.AssetGuid,late);Fact(owner,late);
        Set(pool,"m_AllFeatures",old.Concat(new[]{BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(late)}).ToArray());
        var original=tactician.AllFeatures.ToArray();
        faultId=Id("DrillSergeant"+late.AssetGuid+"Marker01445");cacheFault=true;
        Check(Reject(()=>Register(c,4)),"late cache-write failure rejected");
        Check(tactician.AllFeatures.SequenceEqual(original),"late failure rolls back existing choices");
        foreach(string suffix in new[]{"Toggle","Marker","Shared"})Check(c.Load(Id("DrillSergeant"+late.AssetGuid+suffix+"01445"))==null,"late failed publication leaves no partial resource "+suffix);
        // The shared normal-return-without-write gate also applies to expansion.
        skipWrite=true;Check(Reject(()=>Register(c,4)),"late missing output without exception rejected");
        Check(tactician.AllFeatures.SequenceEqual(original),"late missing output keeps original choices");
        Register(c,4);
        foreach(string name in new[]{"Tactician","GreaterTactician","MasterTactician"})Check(Asset<BlueprintFeatureSelection>(c,"DrillSergeant"+name+"01445").AllFeatures.Count(x=>x==late)==1,"late feat added once to "+name);
        Check(owner.ActivatableAbilities.Enumerable.Count(x=>x.Blueprint.AssetGuid==Id("DrillSergeant"+late.AssetGuid+"Toggle01445"))==1,"already active character receives late known feat button");
        Buff(owner,Asset<BlueprintBuff>(c,"DrillSergeant"+late.AssetGuid+"Marker01445"));
        var ability=Asset<BlueprintAbility>(c,"DrillSergeantAbility01445");var context=new MechanicsContext(owner,target.Descriptor,ability);
        using(context.GetDataScope(target))ability.GetComponent<AbilityEffectRunAction>().Actions.Run();
        Check(target.HasFact(late),"late feat shared by actual native action");
        var assets=Tokens(c,4).Select(t=>c.Load(Id(t))).ToArray();Register(c,4);Register(c,4);
        Check(ReferenceEquals(c.Load(root.AssetGuid),root)&&Tokens(c,4).Select(t=>c.Load(Id(t))).SequenceEqual(assets),"repeated reconciliation retains every GUID identity");
        Check(owner.ActivatableAbilities.Enumerable.Count(x=>x.Blueprint.AssetGuid==Id("DrillSergeant"+late.AssetGuid+"Toggle01445"))==1,"repeat callback does not duplicate live button");
        PsychicExpansionTests.Inventory["drillLatePool01445Contracts"]=new JObject{["passed"]=true,["assertions"]=checks-begin,["incrementalCacheRollback"]=true,["missingOutputRejected"]=true,["threeSelectionsUpdated"]=true,["liveKnownFeatButton"]=true,["actualShareAction"]=true,["identitiesPreserved"]=true};
        Set(pool,"m_AllFeatures",old);cacheFaults--;missingOutputs--;
    }
}
