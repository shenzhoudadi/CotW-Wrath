// Original CotW Features/Subschools.cs, createAdmixture (MIT).
using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Admixture01446
    {
        internal const string ModuleName="Wizard school: Admixture",RootToken="AdmixtureSchool01446";
        internal const string SourceId="f8019b7724d72a241a97157bc37f1c3b",BaseId="c46512b796216b64899f26301241e4e6",OppositionId="c3724cfbe98875f4a9f6d1aabd4011a6";
        internal static readonly string[] Names={"Acid","Electricity","Fire","Cold"};
        internal static readonly string[] Icons={"52135eada006e9045a848cd659749608","d439691f37d17804890bd9c263ae1e80","13bdf8d542811ac4ca228a53aa108145","2ed9d8bf76412ba4a8afe38fa9925fca"};
        internal static string T(string n)=>"Admixture"+n+"01446";
        static BlueprintAbilityResource Resource(PsychicAssets f,string token,BlueprintCharacterClass wizard,bool rounds) {
            var r=f.Add(new BlueprintAbilityResource(),token,"","");var a=Get(r,"m_MaxAmount");Set(a,"BaseValue",rounds?0:3);
            Set(a,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(wizard)});Set(a,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());Set(a,"m_ClassDiv",Array.Empty<BlueprintCharacterClassReference>());Set(a,"m_ArchetypesDiv",Array.Empty<BlueprintArchetypeReference>());
            if(rounds){Set(a,"IncreasedByLevel",true);Set(a,"LevelIncrease",1);}else{Set(a,"IncreasedByStat",true);Set(a,"ResourceBonusStat",StatType.Intelligence);}Set(r,"m_MaxAmount",a);return r;
        }
        internal static void Register(BlueprintsCache cache) {
            if(ArcaneBatch01446.Existing(cache,ModuleName))return;
            var f=new PsychicAssets(cache);var wizard=f.Native<BlueprintCharacterClass>(WizardSubschools01428.WizardId);var pool=f.Native<BlueprintFeatureSelection>(WizardSubschools01428.SchoolPoolId);
            var source=f.Native<BlueprintProgression>(SourceId);var original=f.Native<BlueprintFeature>(BaseId);var opposition=f.Native<BlueprintFeature>(OppositionId);
            if(source.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>e.Features).Count(x=>x==original)!=1||original.GetComponents<AddSpecialSpellList>().Count()!=1||original.GetComponents<IntenseSpells>().Count()!=1||source.LevelEntries.Count(e=>e.Level==20)!=1)throw new InvalidOperationException("01446 Evocation native anchors changed.");
            var versatileResource=Resource(f,T("VersatileResource"),wizard,false);var auraResource=Resource(f,T("AuraResource"),wizard,true);
            var baseFeature=f.Clone(original,T("VersatileFeature"),"Focused School — Admixture","Evokers revel in the raw power of magic, and can use it to create and destroy with shocking ease.\nIntense Spells: Whenever you cast an evocation spell that deals hit point damage, add 1/2 your wizard level to the damage (minimum +1). This bonus only applies once to a spell, not once per missile or ray, and cannot be split between multiple missiles or rays. This damage is of the same type as the spell. At 20th level, whenever you cast an evocation spell, you can roll twice to penetrate a creature's spell resistance and take the better result.\nVersatile Evocation: "+ArcaneText01446.Versatile+"\nElemental Manipulation: "+ArcaneText01446.Manipulation);
            // Retain the real native school spell slots and Intense Spells, but
            // replace Force Missile. Never copy by component index.
            baseFeature.ComponentsArray=baseFeature.ComponentsArray.Where(c=>!(c is AddFacts)).Concat(new BlueprintComponent[]{Grant(versatileResource)}).ToArray();
            var auraFeature=f.Feature(T("AuraFeature"),"Elemental Manipulation",ArcaneText01446.Manipulation,Grant(auraResource));
            for(int i=0;i<4;i++) {
                var icon=f.Native<BlueprintUnitFact>(Icons[i]);
                var mode=new ArcaneCastMode01446{Kind=ArcaneMode01446.Versatile,EnergyIndex=i,Resource=Ref<BlueprintAbilityResourceReference>(versatileResource)};
                var buff=f.Buff(T(Names[i]+"VersatileBuff"),"Versatile Metamagic: "+Names[i],ArcaneText01446.Versatile,mode);
                var toggle=BloodlineMutations01446.Toggle(f,T(Names[i]+"VersatileToggle"),"Versatile Metamagic: "+Names[i],ArcaneText01446.Versatile,buff,versatileResource,icon);
                baseFeature.ComponentsArray=baseFeature.ComponentsArray.Concat(new BlueprintComponent[]{Facts(toggle)}).ToArray();
                var effect=f.Buff(T(Names[i]+"AuraEffect"),"Elemental Manipulation: "+Names[i],ArcaneText01446.Manipulation,new ElementalManipulation01446{EnergyIndex=i,Wizard=Ref<BlueprintCharacterClassReference>(wizard)});ArcaneBatch01446.Icon(effect,icon);
                var apply=new AbilityAreaEffectBuff{CheckConditionEveryRound=true,Condition=new ConditionsChecker{Conditions=Array.Empty<Condition>()}};Set(apply,"m_Buff",Ref<BlueprintBuffReference>(effect));
                var area=f.Add(new BlueprintAbilityAreaEffect{Fx=new Kingmaker.ResourceLinks.PrefabLink{AssetId=""},Size=new Kingmaker.Utility.Feet(30),Shape=AreaEffectShape.Cylinder,AffectEnemies=true,AggroEnemies=false,SpellResistance=false,ComponentsArray=new BlueprintComponent[]{apply}},T(Names[i]+"AuraArea"),"Elemental Manipulation: "+Names[i],ArcaneText01446.Manipulation);SetEnum(area,"m_TargetType","Any");
                var add=new AddAreaEffect();Set(add,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));var host=f.Buff(T(Names[i]+"AuraHost"),"Elemental Manipulation: "+Names[i],ArcaneText01446.Manipulation,add,new ElementalAuraMode01446());SetEnum(host,"m_Flags","HiddenInUi");
                var auraToggle=BloodlineMutations01446.Toggle(f,T(Names[i]+"AuraToggle"),"Elemental Manipulation: "+Names[i],ArcaneText01446.Manipulation,host,auraResource,icon,true);auraFeature.ComponentsArray=auraFeature.ComponentsArray.Concat(new BlueprintComponent[]{Facts(auraToggle)}).ToArray();
            }
            ArcaneBatch01446.Icon(baseFeature,f.Native<BlueprintUnitFact>(Icons[2]));ArcaneBatch01446.Icon(auraFeature,f.Native<BlueprintUnitFact>("cdb106d53c65bbc4086183d54c3b97c7"));
            var root=f.Clone(source,RootToken,"Focused School — Admixture","");Set(root,"m_Description",Get(baseFeature,"m_Description"));
            root.LevelEntries=source.LevelEntries.Select(e=>Entry(e.Level,e.Level==1?e.Features.Select(bp=>bp==original?baseFeature:bp).ToArray():e.Level==8?new BlueprintFeatureBase[]{auraFeature}:e.Features.ToArray())).ToArray();
            root.UIGroups=(source.UIGroups??Array.Empty<UIGroup>()).Select(g=>{if(g==null)return null;var copy=new UIGroup();Set(copy,"m_Features",g.Features.Select(x=>Ref<BlueprintFeatureBaseReference>(x)).ToList());return copy;}).ToArray();
            foreach(string field in new[]{"m_UIDeterminatorsGroup","m_Classes","m_Archetypes"})Set(root,field,CopyData(Get(source,field)));
            var no=new PrerequisiteNoFeature{name="$Admixture01446Opposition",OwnerBlueprint=opposition};Set(no,"m_Feature",Ref<BlueprintFeatureReference>(root));
            var components=opposition.ComponentsArray;var refs=Get(pool,"m_Features");var all=Get(pool,"m_AllFeatures");
            Action undo=()=>{opposition.ComponentsArray=components;Set(pool,"m_Features",refs);Set(pool,"m_AllFeatures",all);};
            Action connect=()=>{if(!ReferenceEquals(cache.Load(pool.AssetGuid),pool)||!ReferenceEquals(cache.Load(opposition.AssetGuid),opposition))throw new InvalidOperationException("01446 school parent replaced.");opposition.ComponentsArray=components.Concat(new BlueprintComponent[]{no}).ToArray();T09CombatFeats.Append(new[]{pool},new BlueprintFeature[]{root});};
            Action verify=()=>{if(!ReferenceEquals(cache.Load(pool.AssetGuid),pool)||!ReferenceEquals(cache.Load(opposition.AssetGuid),opposition))throw new InvalidOperationException("01446 published school parent identity changed.");foreach(string field in new[]{"m_Features","m_AllFeatures"})if(((BlueprintFeatureReference[])Get(pool,field)).Count(r=>r.Guid==root.AssetGuid)!=1)throw new InvalidOperationException("01446 school selection missing or duplicated.");if(opposition.GetComponents<PrerequisiteNoFeature>().Count(p=>p.Feature==root)!=1)throw new InvalidOperationException("01446 opposition link missing or duplicated.");};
            ArcaneBatch01446.Publish(ModuleName,f,connect,undo,verify);
        }
    }
}
