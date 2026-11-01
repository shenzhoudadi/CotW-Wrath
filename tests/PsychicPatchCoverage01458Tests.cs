using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;

internal static class PsychicPatchCoverage01458Tests
{
    internal sealed class Row{internal MethodBase Target;internal string Type,Method;internal HarmonyPatchType Kind;}
    internal static Row[] Rows(Assembly mod)
    {
        if(mod.GetType("CallOfTheWild.Wrath.Prototype.PhysicalMutationToggle01458")==null)return Array.Empty<Row>();
        var game=typeof(BlueprintsCache).Assembly;
        Row M(string patch,string type,string method,HarmonyPatchType kind)=>new Row{Target=patch=="WillOfTheDeadSpellImmunity01458Patch"?null:AccessTools.Method(game.GetType(type,true),method),Type=patch,Method=kind==HarmonyPatchType.Prefix?"Prefix":kind==HarmonyPatchType.Transpiler?"Transpiler":"Postfix",Kind=kind};
        var rows=new[]{
            M("AuraRadiusSpawn01458Patch","Kingmaker.View.MapObjects.AreaEffectView","SpawnFxs",HarmonyPatchType.Postfix),
            M("MutationTouchApproach01458Patch","Kingmaker.UnitLogic.Abilities.AbilityData","GetApproachDistance",HarmonyPatchType.Postfix),
            M("MutationTouchWeapon01458Patch","Kingmaker.UnitLogic.UnitDescriptor","GetWeaponRange",HarmonyPatchType.Postfix),
            M("PhysicalMutationRenew01458Patch","Kingmaker.UnitLogic.Buffs.BuffCollection","TickBuff",HarmonyPatchType.Prefix),
            M("PsychedeliaCognatogenLevel01458Patch","Kingmaker.RuleSystem.Rules.Abilities.RuleCalculateAbilityParams","OnTrigger",HarmonyPatchType.Prefix),
            M("PsychedeliaCognatogenDuration01458Patch","Kingmaker.UnitLogic.Mechanics.ContextDurationValue","Calculate",HarmonyPatchType.Postfix),
            M("MimicMetamagicEligibility01458Patch","Kingmaker.Designers.Mechanics.Facts.AutoMetamagic","ShouldApplyTo",HarmonyPatchType.Prefix),
            M("UndercastSurgeConversions01458Patch","Kingmaker.UnitLogic.Abilities.AbilityData","GetConversions",HarmonyPatchType.Postfix),
            M("UndercastSurgeAvailability01458Patch","Kingmaker.UnitLogic.Abilities.AbilityData","get_IsAvailableForCast",HarmonyPatchType.Postfix),
            M("UndercastSurgeStaleCast01458Patch","Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell","OnTrigger",HarmonyPatchType.Prefix),
            M("MimicMetamagicRecalculate01458Patch","Kingmaker.UnitLogic.Mechanics.MechanicsContext","Recalculate",HarmonyPatchType.Postfix),
            M("WillOfTheDeadSpellImmunity01458Patch","Kingmaker.UnitLogic.Parts.UnitPartSpellResistance","CanApply",HarmonyPatchType.Prefix),
            M("WillOfTheDeadBuffImmunity01458Patch","Kingmaker.UnitLogic.FactLogic.BuffDescriptorImmunity","IsImmune",HarmonyPatchType.Postfix),
            M("WillOfTheDeadTarget01458Patch","Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetHasFact","IsTargetRestrictionPassed",HarmonyPatchType.Postfix),
            M("WillOfTheDeadUnlessTarget01458Patch","Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetHasNoFactUnless","IsTargetRestrictionPassed",HarmonyPatchType.Postfix)
        };
        // CanApply has multiple overloads; bind the actual production signature.
        rows.Single(r=>r.Type=="WillOfTheDeadSpellImmunity01458Patch").Target=AccessTools.Method(game.GetType("Kingmaker.UnitLogic.Parts.UnitPartSpellResistance",true),"CanApply",new[]{game.GetType("Kingmaker.UnitLogic.Parts.UnitPartSpellResistance+SpellImmunity",true),game.GetType("Kingmaker.UnitLogic.Mechanics.MechanicsContext",true)});
        if(rows.Length!=15||rows.Any(r=>r.Target==null)||rows.Select(r=>r.Type).Distinct().Count()!=15)throw new Exception("058 exact production patch catalogue changed.");
        if(mod.GetType("CallOfTheWild.Wrath.Prototype.PhysicalMutationCount01459Patch")!=null){
            rows=rows.Concat(new[]{M("PhysicalMutationCount01459Patch","Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbility","get_ResourceCount",HarmonyPatchType.Postfix),M("PhysicalMutationPaidSlot01459Patch","Kingmaker.UI.UnitSettings.MechanicActionBarSlotActivableAbility","IsDisabled",HarmonyPatchType.Prefix)}).ToArray();
            if(rows.Length!=17||rows.Any(r=>r.Target==null)||rows.Select(r=>r.Type).Distinct().Count()!=17)throw new Exception("059 exact additional count/paid-slot target catalogue changed.");
        }
        if(mod.GetType("CallOfTheWild.Wrath.Prototype.PsychicSurgeCast01460Patch")!=null){
            var constructor=AccessTools.Constructor(game.GetType("Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell",true),new[]{game.GetType("Kingmaker.UnitLogic.Abilities.AbilityData",true),game.GetType("Kingmaker.Utility.TargetWrapper",true)});
            rows=rows.Concat(new[]{new Row{Target=constructor,Type="PsychicSurgeCast01460Patch",Method="Prefix",Kind=HarmonyPatchType.Prefix},M("AgonizingCount01460Patch","Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility","GetResource",HarmonyPatchType.Postfix)}).ToArray();
            if(rows.Length!=19||rows.Any(r=>r.Target==null)||rows.Select(r=>r.Type).Distinct().Count()!=19)throw new Exception("060 exact constructor and actionbar target catalogue changed.");
        }
        if(mod.GetType("CallOfTheWild.Wrath.Prototype.MimicCatalogue01461")!=null){
            if(mod.GetType("CallOfTheWild.Wrath.Prototype.UndercastSurgeConversions01458Patch")!=null)throw new Exception("061 obsolete Surge conversion UI patch must be absent.");
            rows=rows.Where(r=>r.Type!="UndercastSurgeConversions01458Patch").Concat(new[]{
                M("AgonizingOpen01461Patch","Kingmaker.UI.MVVM._VM.ActionBar.ActionBarSlotVM","OnMainClick",HarmonyPatchType.Prefix),
                M("AgonizingExpand01461Patch","Kingmaker.UI.MVVM._VM.ActionBar.ActionBarSlotVM","OnShowConvertRequest",HarmonyPatchType.Prefix),
                M("AgonizingMenuAvailable01461Patch","Kingmaker.UI.MVVM._VM.ActionBar.ActionBarSlotVM","UpdateResource",HarmonyPatchType.Postfix),
                M("MimicCatalogueAdded01461Patch","Kingmaker.Blueprints.JsonSystem.BlueprintsCache","AddCachedBlueprint",HarmonyPatchType.Postfix),
                M("MimicCatalogueAll01461Patch","Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection","get_AllFeatures",HarmonyPatchType.Postfix),
                M("MimicCatalogueFeatures01461Patch","Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection","get_Features",HarmonyPatchType.Postfix)
            }).ToArray();
            if(rows.Length!=24||rows.Any(r=>r.Target==null)||rows.Select(r=>r.Type).Distinct().Count()!=24)throw new Exception("061 exact menu/catalogue additions and single UI retirement changed.");
        }
        if(mod.GetType("CallOfTheWild.Wrath.Prototype.FeralFeaturePrerequisite01461Patch")!=null){
            rows=rows.Concat(new[]{M("FeralFeaturePrerequisite01461Patch","Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeature","CheckInternal",HarmonyPatchType.Transpiler),M("FeralListPrerequisite01461Patch","Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeaturesFromList","CheckInternal",HarmonyPatchType.Transpiler),M("FeralNoFeaturePrerequisite01461Patch","Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteNoFeature","CheckInternal",HarmonyPatchType.Transpiler)}).ToArray();
            if(rows.Length!=27||rows.Any(r=>r.Target==null)||rows.Select(r=>r.Type).Distinct().Count()!=27)throw new Exception("061 exact three scoped Feral qualification ownership substitutions changed.");
        }
        if(AccessTools.Method(mod.GetType("CallOfTheWild.Wrath.Prototype.MimicMetamagic01458"),"Preview")!=null){
            var mimic=rows.Single(r=>r.Type=="MimicMetamagicEligibility01458Patch");
            mimic.Target=AccessTools.Method(game.GetType("Kingmaker.UnitLogic.Abilities.AbilityData",true),"HasMetamagic",new[]{typeof(Kingmaker.UnitLogic.Abilities.Metamagic)});mimic.Method="Postfix";mimic.Kind=HarmonyPatchType.Postfix;
        }
        if(mod.GetType("CallOfTheWild.Wrath.Prototype.UrbanBloodrageFact01463Patch")!=null){
            rows=rows.Concat(new[]{
                new Row{Target=AccessTools.Method(game.GetType("Kingmaker.UnitLogic.UnitHelper",true),"HasFact",new[]{typeof(Kingmaker.UnitLogic.UnitDescriptor),typeof(Kingmaker.Blueprints.Facts.BlueprintFact)}),Type="UrbanBloodrageFact01463Patch",Method="Postfix",Kind=HarmonyPatchType.Postfix},
                M("UrbanBloodragePrerequisite01463Patch","Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeature","CheckInternal",HarmonyPatchType.Transpiler),
                M("UrbanBloodrageAdded01463Patch","Kingmaker.Designers.Mechanics.Facts.BuffExtraEffects","HandleBuffDidAdded",HarmonyPatchType.Transpiler),
                M("UrbanBloodrageRemoved01463Patch","Kingmaker.Designers.Mechanics.Facts.BuffExtraEffects","HandleBuffDidRemoved",HarmonyPatchType.Transpiler)
            }).ToArray();
            if(rows.Length!=31||rows.Any(r=>r.Target==null)||rows.Select(r=>r.Type).Distinct().Count()!=31)throw new Exception("063 exact four scoped Urban bloodrage hooks changed.");
        }
        if(mod.GetType("CallOfTheWild.Wrath.Prototype.TemporaryKnowledge01464")!=null){
            const string patch="TemporaryKnowledge01464+",part="Kingmaker.UnitLogic.Parts.UnitPartTemporarySpellsKnown";
            var remove=AccessTools.Method(game.GetType(part,true),"RemoveEntry",new[]{game.GetType("Kingmaker.EntitySystem.EntityFact",true),game.GetType("Kingmaker.UnitLogic.FactLogic.AddSpellKnownTemporary",true)});
            rows=rows.Concat(new[]{M(patch+"SourcesAddedPatch",part,"AddEntry",HarmonyPatchType.Postfix),new Row{Target=remove,Type=patch+"SourcesRemovedPatch",Method="Postfix",Kind=HarmonyPatchType.Postfix},M(patch+"SourcesLoadedPatch",part,"OnPostLoad",HarmonyPatchType.Postfix),M(patch+"SourcesLeveledPatch",part,"HandleUnitGainLevel",HarmonyPatchType.Postfix)}).ToArray();
            if(rows.Length!=35||rows.Any(r=>r.Target==null)||rows.Select(r=>r.Target+"/"+r.Type+"/"+r.Kind).Distinct().Count()!=35)throw new Exception("064 exact four native temporary-knowledge source targets changed.");
            rows=rows.Concat(new[]{new Row{Target=remove,Type="TemporaryKnowledge01464+PruneSourcesPatch",Method="Prefix",Kind=HarmonyPatchType.Prefix}}).ToArray();
            if(rows.Length!=36||rows.Any(r=>r.Target==null)||rows.Select(r=>r.Target+"/"+r.Type+"/"+r.Kind).Distinct().Count()!=36)throw new Exception("064 native source-removal identity guard changed.");
            rows=rows.Concat(new[]{M("AmnesiaLevelUp01464Patch","Kingmaker.UnitLogic.Class.LevelUp.LevelUpConfig","StartController",HarmonyPatchType.Prefix)}).ToArray();
            if(rows.Length!=37||rows.Any(r=>r.Target==null)||rows.Select(r=>r.Target+"/"+r.Type+"/"+r.Kind).Distinct().Count()!=37)throw new Exception("064 exact pre-preview amnesia cleanup target changed.");
        }
        if(mod.GetType("CallOfTheWild.Wrath.Prototype.UrbanBloodrageBuffConsumerPatch")!=null){
            rows=rows.Concat(new[]{M("UrbanBloodrageBuffConsumerPatch","Kingmaker.Designers.Mechanics.Facts.BuffExtraEffects","CheckHadPrerequiredBuff",HarmonyPatchType.Transpiler),M("UrbanBloodrageConditionConsumerPatch","Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasFact","CheckCondition",HarmonyPatchType.Transpiler),M("UrbanBloodrageAdded01463Patch","Kingmaker.Designers.Mechanics.Facts.BuffExtraEffects","HandleBuffDidAdded",HarmonyPatchType.Postfix)}).ToArray();
            if(rows.Length!=40||rows.Any(r=>r.Target==null)||rows.Select(r=>r.Target+"/"+r.Type+"/"+r.Kind).Distinct().Count()!=40)throw new Exception("Urban controlled rage exact two native consumers and isolated diagnostic hook changed.");
        }
        return rows;
    }
    internal static int Verify(Assembly mod,Harmony owner)
    {
        var rows=Rows(mod);foreach(var row in rows){var method=AccessTools.DeclaredMethod(mod.GetType("CallOfTheWild.Wrath.Prototype."+row.Type,true),row.Method);var info=Harmony.GetPatchInfo(row.Target);if(info==null)throw new Exception("058 native target was not patched: "+row.Target);var hooks=row.Kind==HarmonyPatchType.Prefix?info.Prefixes:row.Kind==HarmonyPatchType.Transpiler?info.Transpilers:info.Postfixes;
            if(hooks.Count(p=>p.owner==owner.Id&&p.PatchMethod==method)!=1)throw new Exception("058 production hook must be installed exactly once with its owner/kind/target: "+row.Type);
        }return rows.Length;
    }
}
