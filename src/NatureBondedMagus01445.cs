// Adapted from Holic75/KingmakerRebalance (MIT), Archetypes/NatureBondedMagus.cs.
using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Newtonsoft.Json;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class NatureBondedMagus01445
    {
        internal const string ModuleName="Magus archetype: Nature-Bonded Magus",RootToken="NatureBondedMagusArchetype01445",ClassId="45a4607686d96a1498891b3286121780";
        internal static string T(string s)=>"NatureBondedMagus"+s+"01445";
        internal static readonly string[] OwnTokens=new[]{RootToken,T("Familiar"),T("SpellList"),T("Symbiosis"),T("ImprovedSymbiosis"),T("WoodlandStride")}.Concat(Enumerable.Range(1,6).SelectMany(i=>new[]{T("NaturalMagic"+i),T("Learn"+i)})).ToArray();
        internal static readonly string[] Restricted={"2eacbdbf1c4f4134aa7fea99ab8763dc","a2e0691dcfda2374e84d8bbf480e06a0","4be0bb10e110a35419e406da767bd1e3","cb6916027e3c25e4185de068249254dc","7a73bf165e8eda6478b4419f857d1ab5","8896f327c59569c4eaf129bf35b96c1f","85c05a8120e3e9f4e8ae01625038809a","a3909a7293533fe49a2d7cfe051f17e4","42f96fc8d6c80784194262e51b0a1d25"};
        static ContextRankConfig Rank(BlueprintCharacterClass cls,string progression){var r=new ContextRankConfig();SetEnum(r,"m_BaseValueType","ClassLevel");SetEnum(r,"m_Progression",progression);Set(r,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(cls)});return r;}
        internal static void Register(BlueprintsCache cache)
        {
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(ClassId);if(Existing(cache,RootToken,cls))return;Vacant(cache,OwnTokens);
            var restrictions=Restricted.Select(f.Native<BlueprintFeature>).ToArray();var druid=f.Native<BlueprintSpellList>("bad8638d40639d04fa2f80a1cac67d6b");var magus=f.Native<BlueprintSpellList>("4d72e1e7bd6bc4f4caaea7aa43a14639");
            var familiarSource=f.Native<BlueprintFeatureSelection>(T11TrainingOptions.Familiar);if(familiarSource.AllFeatures.Length==0)throw new InvalidOperationException("Native familiar choices missing.");
            var familiar=f.Clone(familiarSource,T("Familiar"),"Familiar","A nature-bonded magus gains the familiar magus arcana.");familiar.ComponentsArray=Array.Empty<BlueprintComponent>();familiar.Groups=Array.Empty<FeatureGroup>();familiar.IgnorePrerequisites=false;Set(familiar,"m_Icon",f.Native<Kingmaker.Blueprints.Facts.BlueprintUnitFact>("1194129055923ae46a6b876ff6a30358").Icon);
            // Work on authored GUIDs, without eagerly loading the whole native
            // spell catalogue or sharing native list/reference proxy objects.
            var excluded=magus.SpellsByLevel.SelectMany(e=>(System.Collections.Generic.List<BlueprintAbilityReference>)Get(e,"m_Spells")).Where(r=>r!=null).Select(r=>r.Guid).ToHashSet();
            var list=f.Add(new BlueprintSpellList{SpellsByLevel=SpellListCompatibility.Build(druid.SpellsByLevel.Select(e=>{var copy=new SpellLevelList(e.SpellLevel);Set(copy,"m_Spells",((System.Collections.Generic.List<BlueprintAbilityReference>)Get(e,"m_Spells")).Where(r=>r!=null&&!excluded.Contains(r.Guid)).Select(CopyData).ToList());return copy;}).ToArray())},T("SpellList"),"Natural Magic","");
            var choices=new BlueprintFeatureSelection[6];const string natural="A nature - bonded magus adds one 1st - level spell from the druid spell list to his spellbook. Each time a nature - bonded magus gains the ability to cast a new level of spells, he can add one spell of that level from the druid spell list to his spellbook.";
            for(int i=1;i<=6;i++){
                var learn=f.Clone(f.Native<BlueprintParametrizedFeature>("bcd757ac2aeef3c49b77e5af4e510956"),T("Learn"+i),"Natural Magic (Level "+i+")",natural);learn.SpecificSpellLevel=true;learn.SpellLevel=i;learn.SpellLevelPenalty=0;Set(learn,"m_SpellList",Ref<BlueprintSpellListReference>(list));Set(learn,"m_SpellcasterClass",Ref<BlueprintCharacterClassReference>(cls));Set(learn,"m_CachedItems",null);
                var l=learn.GetComponent<LearnSpellParametrized>()??throw new InvalidOperationException("Native spell learning component missing.");l.SpecificSpellLevel=true;l.SpellLevel=i;l.SpellLevelPenalty=0;Set(l,"m_SpellList",Ref<BlueprintSpellListReference>(list));Set(l,"m_SpellcasterClass",Ref<BlueprintCharacterClassReference>(cls));Set(learn,"m_Icon",f.Native<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>("0fd00984a2c0e0a429cf1a911b4ec5ca").Icon);
                var choice=f.Add(new BlueprintFeatureSelection{Ranks=1,IsClassFeature=true},T("NaturalMagic"+i),"Natural Magic (Level "+i+")",natural);Set(choice,"m_AllFeatures",new[]{Ref<BlueprintFeatureReference>(learn)});Set(choice,"m_Features",new[]{Ref<BlueprintFeatureReference>(learn)});Set(choice,"m_Icon",learn.Icon);choices[i-1]=choice;
            }
            var symRank=Rank(cls,"AsIs");SetEnum(symRank,"m_BaseValueType","FeatureRank");
            var sym=f.Feature(T("Symbiosis"),"Symbiosis","A 4th level, a nature-bonded magus gains +1 bonus to its natural AC. This bonus increases by 1 at 7th level and every 4 levels thereafter.",symRank,new NatureBondedMagusSymbiosis01445{Value=new ContextValue{ValueType=ContextValueType.Rank}});sym.Ranks=5;sym.ReapplyOnLevelUp=true;Set(symRank,"m_Feature",Ref<BlueprintFeatureReference>(sym));Set(sym,"m_Icon",f.Native<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>("5b77d7cc65b8ab74688e74a37fc2f553").Icon);
            var improvedRank=ArchetypeBatch01445.Rank(cls,"Custom",14,4,18,6,20,8);
            var improved=f.Feature(T("ImprovedSymbiosis"),"Improved Symbiosis","At 11th level, a nature-bonded magus gains a +4 enhancement bonus to his Strength and Constitution. This bonus increases to +6 at level 15 and to +8 at level 19.",improvedRank,new AddContextStatBonus{Stat=StatType.Strength,Descriptor=ModifierDescriptor.Enhancement,Value=new ContextValue{ValueType=ContextValueType.Rank}},new AddContextStatBonus{Stat=StatType.Constitution,Descriptor=ModifierDescriptor.Enhancement,Value=new ContextValue{ValueType=ContextValueType.Rank}});improved.ReapplyOnLevelUp=true;Set(improved,"m_Icon",f.Native<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>("4c3d08935262b6544ae97599b3a9556d").Icon);
            var stride=f.Clone(f.Native<BlueprintFeature>("11f4072ea766a5840a46e6660894527d"),T("WoodlandStride"),"Woodland Stride","At 7th level, a nature-bonded magus can move through any sort of undergrowth (such as natural briars, overgrown areas, thorns, and similar terrain) at his normal speed and without taking damage or suffering any other impairment.");Set(stride,"m_Icon",sym.Icon);
            var removes=new[]{Entry(1,f.Native<BlueprintFeature>("3ce9bb90749c21249adc639031d5eed1")),Entry(4,f.Native<BlueprintFeature>("61fc0521e9992624e9c518060bf89c0f")),Entry(5,f.Native<BlueprintFeature>("36b609a6946733c42930c55ac540416b")),Entry(9,f.Native<BlueprintFeature>("70be888059f99a245a79d6d61b90edc5")),Entry(11,f.Native<BlueprintFeature>("0ef6ec1c2fdfc204fbd3bff9f1609490")),Entry(13,f.Native<BlueprintFeature>("1804187264121cd439d70a96234d4ddb")),Entry(17,f.Native<BlueprintFeature>("3cbe3e308342b3247ba2f4fbaf5e6307"))};
            var a=f.Add(new BlueprintArchetype{LocalizedName=Text(RootToken,"name","Nature-Bonded Magus"),LocalizedDescription=Text(RootToken,"description","A nature-bonded magus synergizes arcane magic and the divine magic traditions of druids into a deadly synthesis."),RemoveFeatures=removes,AddFeatures=new[]{Entry(1,familiar,choices[0]),Entry(4,sym,choices[1]),Entry(7,sym,stride,choices[2]),Entry(10,choices[3]),Entry(11,sym,improved),Entry(13,choices[4]),Entry(15,sym),Entry(16,choices[5]),Entry(19,sym)}},RootToken,"Nature-Bonded Magus","");Set(a,"m_ParentClass",cls);
            var no=restrictions.Select(x=>{var p=new PrerequisiteNoArchetype{name="$CotWNatureBonded01445",OwnerBlueprint=x};Set(p,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));Set(p,"m_Archetype",Ref<BlueprintArchetypeReference>(a));return p;}).ToArray();
            Func<Action> snap=()=>{var old=restrictions.Select(x=>x.ComponentsArray).ToArray();return ()=>{for(int i=0;i<old.Length;i++)restrictions[i].ComponentsArray=old[i];};};
            Action connect=()=>{for(int i=0;i<no.Length;i++){var target=restrictions[i];if(!ReferenceEquals(cache.Load(target.AssetGuid),target))throw new InvalidOperationException("Nature-Bonded arcana identity changed.");var present=target.ComponentsArray.Where(c=>c?.name==no[i].name).ToArray();if(present.Length==0)target.ComponentsArray=target.ComponentsArray.Concat(new[]{no[i]}).ToArray();else if(present.Length!=1||!ReferenceEquals(present[0],no[i]))throw new InvalidOperationException("Nature-Bonded arcana prerequisite collision.");}};
            Publish(cache,f,OwnTokens,cls,a,new BlueprintFeatureBase[]{familiar,choices[0],sym,stride,improved,choices[1],choices[2],choices[3],choices[4],choices[5]},snap,connect);
        }
    }
    public sealed class NatureBondedMagusSymbiosisData01445
    {
        [JsonIgnore] public bool Updating;
        [JsonIgnore] public Action<ModifiableValue,int> Changed;
    }
    [TypeId("6cc2c45635564dbda9d782b87849fe72")]
    public sealed class NatureBondedMagusSymbiosis01445:UnitFactComponentDelegate<NatureBondedMagusSymbiosisData01445>
    {
        public ContextValue Value;
        void Refresh()
        {
            if(Data.Updating)return;
            Data.Updating=true;
            try {
                var ac=Owner.Stats.AC;
                // Only supply the shortfall above existing natural armor.
                // Explicitly stacking improvements and enhancement bonuses keep
                // their native behavior. TTT's nonstacking descriptor needs the
                // full value, which the native aggregator then compares.
                int other=ac.Modifiers.Where(m=>m.Source!=Fact&&m.ModDescriptor==ModifierDescriptor.NaturalArmor&&m.StackMode!=ModifiableValue.StackMode.ForceStack&&m.ModValue>0).Sum(m=>m.ModValue);
                int bonus=ModifierDescriptorHelper.IsStackable(ModifierDescriptor.NaturalArmor)?Math.Max(0,Value.Calculate(Context)-other):Value.Calculate(Context);
                var own=ac.Modifiers.Where(m=>m.Source==Fact).ToArray();
                if(own.Length==(bonus>0?1:0)&&(bonus==0||own[0].ModValue==bonus))return;
                foreach(var m in own)ac.RemoveModifier(m);
                if(bonus>0)ac.AddModifier(bonus,Fact,ModifierDescriptor.NaturalArmor);
            }finally{Data.Updating=false;}
        }
        protected override void OnTurnOn()
        {
            if(Data.Changed==null){var runtime=Runtime;Data.Changed=(s,n)=>{using(runtime.RequestEventContext())Refresh();};Owner.Stats.AC.OnChanged+=Data.Changed;}
            Refresh();
        }
        protected override void OnTurnOff()
        {
            if(Data.Changed!=null)Owner.Stats.AC.OnChanged-=Data.Changed;
            Data.Changed=null;
            foreach(var m in Owner.Stats.AC.Modifiers.Where(m=>m.Source==Fact).ToArray())Owner.Stats.AC.RemoveModifier(m);
        }
        protected override void OnApplyPostLoadFixes(){OnTurnOff();if(Fact.IsActive&&Fact.IsTurnedOn&&!Fact.IsDisposed)OnTurnOn();}
    }

}
