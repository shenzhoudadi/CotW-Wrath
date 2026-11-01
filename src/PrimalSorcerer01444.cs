// Adapted from Holic75/KingmakerRebalance (MIT), Archetypes/PrimalSorcerer.cs.
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01444;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PrimalSorcerer01444
    {
        internal const string ModuleName="Sorcerer archetype: Primal Sorcerer",RootToken="PrimalSorcererArchetype01444",SelectionToken="PrimalBloodlineSelection01444";
        internal const string ClassId="b3a505fb61437dc4097f43c3f8f9a4cf",ProgressionId="997665565ca80a649aedd72455c4df1f",SelectionId="24bef8d1bee12274686f6da6ccbc8914",FeatsId="d2a4b74ee7e43a648b51d0f36db2aa34",DiscipleId="72051275b1dbb2d42ba9118237794f7c",IconId="333efbf776ab61c4da53e9622751d95f";
        internal static readonly string[] Elements={"Air","Earth","Fire","Water"};
        internal static readonly string[] Progressions={"cd788df497c6f10439c7025e87864ee4","32393034410fb2f4d9c8beaa5c8c8ab7","17cc794d47408bc4986c55265475c06f","7c692e90592257a4e901d12ae6ec1e41"};
        internal static readonly string[] Arcanas={"0f0cb88a2ccc0814aa64c41fd251e84e","caebe2fa3b5a94d4bbc19ccca86d1d6f","a8baee8eb681d53438cc17bd1d125890","456e305ebfec3204683b72a45467d87c"};
        internal static readonly DamageEnergyType[] Energies={DamageEnergyType.Electricity,DamageEnergyType.Acid,DamageEnergyType.Fire,DamageEnergyType.Cold};
        internal static string Token(int i,string kind)=>"Primal"+Elements[i]+kind+"01444";
        internal static readonly string[] OwnTokens=new[]{RootToken,SelectionToken}.Concat(Enumerable.Range(0,4).SelectMany(i=>new[]{"Progression","Arcana","SummoningFeature","SummoningBuff"}.Select(k=>Token(i,k)))).ToArray();
        const string Description="Your powers are attuned to the concentrated core of the elemental plane.";
        static void OnlyClass(BlueprintProgression progression,BlueprintCharacterClass cls) {
            var type=HarmonyLib.AccessTools.Field(typeof(BlueprintProgression),"m_Classes").FieldType.GetElementType();var item=Activator.CreateInstance(type);Set(item,"m_Class",Ref<BlueprintCharacterClassReference>(cls));var array=Array.CreateInstance(type,1);array.SetValue(item,0);Set(progression,"m_Classes",array);
            var archetypes=HarmonyLib.AccessTools.Field(typeof(BlueprintProgression),"m_Archetypes").FieldType.GetElementType();Set(progression,"m_Archetypes",Array.CreateInstance(archetypes,0));
        }
        internal static void Register(BlueprintsCache cache)
        {
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(ClassId);
            if(Existing(cache,RootToken,cls))return;Vacant(cache,OwnTokens);
            var sourceSelection=f.Native<BlueprintFeatureSelection>(SelectionId);var featPool=f.Native<BlueprintFeatureSelection>(FeatsId);var disciple=f.Native<BlueprintCharacterClass>(DiscipleId);var eligibility=featPool.GetComponents<PrerequisiteFeaturesFromList>().Single();
            if(cls.Progression.AssetGuid!=BlueprintGuid.Parse(ProgressionId)||!cls.Progression.LevelEntries.Any(e=>e.Level==1&&e.Features.Any(x=>x?.AssetGuid==sourceSelection.AssetGuid)))throw new InvalidOperationException("Primal native Sorcerer bloodline entry changed.");
            var icon=f.Native<BlueprintAbility>(IconId);var choices=new BlueprintProgression[4];
            for(int i=0;i<4;i++) {
                var source=f.Native<BlueprintProgression>(Progressions[i]);var arcana=f.Clone(f.Native<BlueprintFeature>(Arcanas[i]),Token(i,"Arcana"),"Primal Bloodline Arcana","Whenever you cast a spell with "+Energies[i].ToString().ToLowerInvariant()+" damage, that spell deals +1 point of damage per die rolled.");
                var resistance=new PrimalSummonResistance01444{Type=Energies[i],Value=10};
                var damage=new ContextActionDealDamage{DamageType=new DamageTypeDescription{Type=DamageType.Energy,Energy=Energies[i]},Value=new ContextDiceValue{DiceType=DiceType.D6,DiceCountValue=1,BonusValue=0},IgnoreCritical=true,DisableSneakDamage=true};
                var trigger=new AddInitiatorAttackWithWeaponTrigger{OnlyHit=true,AllNaturalAndUnarmed=true,Action=Actions(damage)};
                var buff=f.Buff(Token(i,"SummoningBuff"),"Elementalist Summoning","At 9th level, whenever you summon a creature, it gains energy resistance 10 against the energy type that matches your elemental bloodline, and its natural attacks deal an additional 1d6 points of damage of the same energy type. ",resistance,trigger);buff.Stacking=StackingType.Replace;Set(buff,"m_Icon",Get(icon,"m_Icon"));
                var feature=f.Feature(Token(i,"SummoningFeature"),"Elementalist Summoning","At 9th level, whenever you summon a creature, it gains energy resistance 10 against the energy type that matches your elemental bloodline, and its natural attacks deal an additional 1d6 points of damage of the same energy type. ");Set(feature,"m_Icon",Get(icon,"m_Icon"));
                var onSpawn=new OnSpawnBuff{CheckDescriptor=true,SpellDescriptor=SpellDescriptor.Summoning,IsInfinity=true};Set(onSpawn,"m_IfHaveFact",Ref<BlueprintFeatureReference>(feature));Set(onSpawn,"m_buff",Ref<BlueprintBuffReference>(buff));feature.ComponentsArray=new BlueprintComponent[]{onSpawn};
                var oldArcana=source.LevelEntries.SelectMany(e=>e.Features).Where(x=>x.name.Contains("Arcana")).Distinct().ToArray();var oldBlast=source.LevelEntries.Where(e=>e.Level==9).SelectMany(e=>e.Features).Where(x=>x.name.Contains("ElementalBlastFeature")).ToArray();
                if(oldArcana.Length!=1||oldBlast.Length!=1)throw new InvalidOperationException("Primal elemental arcana/level9 blast anchor changed: "+source.name);
                BlueprintFeatureBase Replace(BlueprintFeatureBase bp)=>bp.AssetGuid==oldArcana[0].AssetGuid?arcana:bp.AssetGuid==oldBlast[0].AssetGuid?feature:bp;
                var progression=f.Clone(source,Token(i,"Progression"),"Primal Elemental Bloodline — "+Elements[i],"The power of the elements resides in you, and at times you can hardly control its fury. This influence comes from an elemental outsider in your family history or a time when you or your relatives were exposed to a powerful elemental force. "+Elements[i]+" infuses your being, and you can draw upon its power in times of need.\nBonus Feats of the Elemental Bloodline: Dodge, Empower Spell, Great Fortitude, Improved Initiative, Lightning Reflexes, Power Attack, Skill Focus (Knowledge: Arcana), Weapon Finesse.");OnlyClass(progression,cls);progression.LevelEntries=source.LevelEntries.Select(e=>Entry(e.Level,e.Features.Select(Replace).ToArray())).ToArray();
                BlueprintFeatureBaseReference ReplaceRef(BlueprintFeatureBaseReference r)=>r?.Guid==oldArcana[0].AssetGuid?Ref<BlueprintFeatureBaseReference>(arcana):r?.Guid==oldBlast[0].AssetGuid?Ref<BlueprintFeatureBaseReference>(feature):r;
                // Clone only authored references into fresh groups. Never copy
                // m_FeaturesList: that native proxy closes over its source list.
                progression.UIGroups=(source.UIGroups??Array.Empty<UIGroup>()).Select(group=>{
                    if(group==null)return null;
                    var copy=new UIGroup();
                    Set(copy,"m_Features",((System.Collections.Generic.List<BlueprintFeatureBaseReference>)Get(group,"m_Features")).Select(r=>ReplaceRef(CopyData(r))).ToList());
                    return copy;
                }).ToArray();
                Set(progression,"m_UIDeterminatorsGroup",((BlueprintFeatureBaseReference[])Get(source,"m_UIDeterminatorsGroup")??Array.Empty<BlueprintFeatureBaseReference>()).Select(r=>ReplaceRef(CopyData(r))).ToArray());choices[i]=progression;
            }
            var selection=f.Clone(sourceSelection,SelectionToken,"Primal Bloodline","Each sorcerer has a source of magic somewhere in her heritage that grants her spells, bonus feats, an additional class skill, and other special abilities. This source can represent a blood relation or an extreme event involving a creature somewhere in the family's past. For example, a sorcerer might have a dragon as a distant relative or her grandfather might have signed a terrible contract with a devil. Regardless of the source, this influence manifests in a number of ways as the sorcerer gains levels. A sorcerer must pick one bloodline upon taking her first level of sorcerer. Once made, this choice cannot be changed.\nFilled with magical power that screams for release, sorcery is not so much a calling as a blessing — or a curse. For some sorcerers, this arcane birthright manifests in subtle and carefully controlled ways, assisting in their manipulation of others or the pursuit of lofty goals. For others, it is wild and unpredictable, the primal and explosive lashing out of a power greater than themselves.\nAt 3rd level, and every two levels thereafter, a sorcerer learns an additional spell, derived from her bloodline. These spells are in addition to the number of spells given at new levels. These spells cannot be exchanged for different spells at higher levels.\nAt 7th level, and every six levels thereafter, a sorcerer receives one bonus feat, chosen from a list specific to each bloodline. The sorcerer must meet the prerequisites for these bonus feats.");Set(selection,"m_Features",choices.Select(Ref<BlueprintFeatureReference>).ToArray());Set(selection,"m_AllFeatures",choices.Select(Ref<BlueprintFeatureReference>).ToArray());Set(selection,"m_Icon",Get(icon,"m_Icon"));
            var noDisciple=new PrerequisiteNoClassLevel();Set(noDisciple,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(disciple));
            var root=f.Add(new BlueprintArchetype{LocalizedName=Text(RootToken,"name","Primal Sorcerer"),LocalizedDescription=Text(RootToken,"description",Description),AddFeatures=new[]{Entry(1,selection)},RemoveFeatures=new[]{Entry(1,sourceSelection)},ComponentsArray=new BlueprintComponent[]{noDisciple}},RootToken,"Primal Sorcerer",Description);Set(root,"m_ParentClass",cls);
            var noPrimal=new PrerequisiteNoArchetype{name="$CotWPrimalNoDragonDisciple01444",OwnerBlueprint=disciple};Set(noPrimal,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));Set(noPrimal,"m_Archetype",Ref<BlueprintArchetypeReference>(root));
            Func<Action> snapshot=()=>{var refs=Get(eligibility,"m_Features");var components=disciple.ComponentsArray;return ()=>{Set(eligibility,"m_Features",refs);disciple.ComponentsArray=components;};};
            Action connect=()=>{
                if(!ReferenceEquals(cache.Load(featPool.AssetGuid),featPool)||!ReferenceEquals(cache.Load(disciple.AssetGuid),disciple)||!featPool.ComponentsArray.Contains(eligibility))throw new InvalidOperationException("Primal native feat/prestige parent identity changed.");
                var refs=(BlueprintFeatureReference[])Get(eligibility,"m_Features")??Array.Empty<BlueprintFeatureReference>();foreach(var p in choices)if(refs.Count(r=>r?.Guid==p.AssetGuid)>1)throw new InvalidOperationException("Primal duplicate elemental feat prerequisite.");
                var same=disciple.ComponentsArray.Where(c=>c?.name==noPrimal.name).ToArray();if(same.Length>1||same.Length==1&&!ReferenceEquals(same[0],noPrimal))throw new InvalidOperationException("Primal prestige prerequisite collision.");
                Set(eligibility,"m_Features",refs.Concat(choices.Where(p=>!refs.Any(r=>r?.Guid==p.AssetGuid)).Select(Ref<BlueprintFeatureReference>)).ToArray());if(same.Length==0)disciple.ComponentsArray=disciple.ComponentsArray.Concat(new[]{noPrimal}).ToArray();
            };
            Publish(cache,f,OwnTokens,cls,root,new BlueprintFeatureBase[]{selection},snapshot,connect);
        }
    }
}
