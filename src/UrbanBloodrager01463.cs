// Adapted from Holic75/KingmakerRebalance Bloodrager.createUrbanBloodrager (MIT).
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using Kingmaker.ResourceLinks;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class UrbanBloodrager01463
    {
        internal const string ModuleName="Bloodrager archetype: Urban Bloodrager",RootToken="UrbanBloodragerArchetype01463";
        internal const string ClassId="d77e67a814d686842802c9cfd8ef8499",RageId="6991ee8175d87c04790067515f6fb322",RageBuffId="5eac31e457999334b98f98b60fc73b2f";
        internal const string FeatureToken="UrbanBloodrageFeature01463",AreaToken="UrbanRestrainedMagicArea01463";
        internal static readonly string[] Stats={"Strength","Dexterity","Constitution"};
        internal static readonly BlueprintGuid[] BuffIds={Id("UrbanBloodrageStrengthBuff01463")};
        internal static readonly BlueprintGuid FeatureId=Id(FeatureToken),NativeRage=BlueprintGuid.Parse(RageId),NativeBuff=BlueprintGuid.Parse(RageBuffId);
        const string RageText="Use the persistent allocation toggles to distribute a total +4 morale bonus among Strength, Dexterity and Constitution in increments of 2. The total increases to +6 at bloodrager level 11 and +8 at 20. Then activate Controlled Bloodrage. It grants none of the ordinary bloodrage attack, damage, temporary hit point, Will or AC adjustments. It counts as bloodrage for bloodline powers and prerequisites. Native rage rounds, fatigue restrictions and Limitless Rage are retained. Charisma-, Dexterity- and Intelligence-based skills remain usable.";
        const string RestrainedText="At 3rd level, non-enemies within 60 feet gain a +2 bonus on saving throws against spells cast by this bloodrager. Other casters' spells and non-spell effects do not receive this bonus. This replaces blood sanctuary.";
        const string AdoptedText="At 7th level, and at 10th, 13th, 16th and 19th levels, learn one bard or magus spell of a level you can cast that is not already on the bloodrager spell list. The spell becomes a bloodrager spell known for this character. This replaces damage reduction.";
        static void Icon(BlueprintUnitFact a,BlueprintUnitFact b)=>SpellBatch01447.Icon(a,b);
        internal static bool Register(BlueprintsCache cache)
        {
            if(MagusBatch01454.Existing(cache,ModuleName))return true;
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(ClassId);
            // Defer to an already supplied equivalent archetype instead of
            // registering another root under a different deterministic GUID.
            if(cls.Archetypes.Any(a=>a!=null&&a.AssetGuid!=Id(RootToken)&&a.name.IndexOf("UrbanBloodrager",StringComparison.OrdinalIgnoreCase)>=0))return false;
            var source=f.Native<BlueprintFeature>(RageId);var controlled=f.Clone(source,FeatureToken,"Controlled Bloodrage",RageText);
            var abilitySource=f.Native<BlueprintActivatableAbility>("e3a0056eedac7754ca9a50603ba05177");
            var buffSource=f.Native<BlueprintBuff>(RageBuffId);var allocations=new List<BlueprintUnitFact>();var bonuses=new List<BlueprintUnitFact>();
            foreach(string stat in Stats){
                var points=new List<BlueprintFeature>();
                for(int portion=0;portion<4;portion++){
                    string label="Allocation: "+stat+" +2 ("+(portion+1)+")";
                    var point=f.Feature(AllocationToken(stat,portion,"Point"),label,"One allocated 2-point contribution.");point.HideInUI=point.HideInCharacterSheetAndLevelUp=true;points.Add(point);
                    var marker=f.Buff(AllocationMarkerToken(stat,portion),label,RageText,Facts(point));marker.Stacking=StackingType.Replace;SetEnum(marker,"m_Flags","HiddenInUi");
                    var option=f.Add(new BlueprintActivatableAbility{Group=ActivatableAbilityGroup.None,ActivationType=AbilityActivationType.Immediately,DeactivateImmediately=true,DeactivateIfCombatEnded=false,DeactivateIfOwnerDisabled=false,DeactivateIfOwnerUnconscious=false,DoNotTurnOffOnRest=true,IsOnByDefault=stat=="Strength"&&portion<2},AllocationAbilityToken(stat,portion),label,RageText);
                    Set(option,"m_Buff",Ref<BlueprintBuffReference>(marker));Icon(option,f.Native<BlueprintUnitFact>(UrbanSkald01443.AllocationIconIds[Array.IndexOf(Stats,stat)]));option.ComponentsArray=new BlueprintComponent[]{new UrbanBloodrageAllocationRestriction{Portion=portion}};allocations.Add(option);
                }
                var rank=new ContextRankConfig();Set(rank,"m_Type",AbilityRankType.StatBonus);SetEnum(rank,"m_BaseValueType","FeatureList");SetEnum(rank,"m_Progression","AsIs");Set(rank,"m_FeatureList",points.Select(Ref<BlueprintFeatureReference>).ToArray());
                var recalc=new RecalculateOnFactsChange();Set(recalc,"m_CheckedFacts",points.Select(Ref<BlueprintUnitFactReference>).ToArray());
                var bonus=f.Buff("UrbanBloodrage"+stat+"AllocatedBonus01463", "Controlled Bloodrage: "+stat,RageText,rank,new AddContextStatBonus{Stat=(StatType)Enum.Parse(typeof(StatType),stat),Descriptor=ModifierDescriptor.Morale,Multiplier=2,Value=new ContextValue{ValueType=ContextValueType.Rank,ValueRank=AbilityRankType.StatBonus}},recalc);SetEnum(bonus,"m_Flags","HiddenInUi");bonuses.Add(bonus);
            }
            var buff=f.Clone(buffSource,"UrbanBloodrageStrengthBuff01463","Controlled Bloodrage",RageText);
            buff.ComponentsArray=buff.ComponentsArray.Where(c=>!(c is TemporaryHitPointsPerLevel)&&!(c is AttackTypeAttackBonus)&&!(c is WeaponAttackTypeDamageBonus)&&!(c is WeaponGroupDamageBonus)&&!(c is SpellDescriptorComponent)&&!(c is AddContextStatBonus)).Concat(new BlueprintComponent[]{Facts(bonuses.ToArray())}).ToArray();
            var ability=f.Clone(abilitySource,"UrbanBloodrageStrengthToggle01463","Controlled Bloodrage",RageText);Set(ability,"m_Buff",Ref<BlueprintBuffReference>(buff));ability.IsOnByDefault=false;Icon(ability,f.Native<BlueprintUnitFact>(UrbanSkald01443.AllocationIconIds[0]));
            var facts=controlled.GetComponent<AddFacts>()??throw new InvalidOperationException("Urban bloodrage requires native rage ability grant.");
            Set(facts,"m_Facts",new BlueprintUnitFact[]{ability}.Concat(allocations).Select(Ref<BlueprintUnitFactReference>).ToArray());
            var proficiency=f.Clone(f.Native<BlueprintFeature>("7aa59f5998e5baf4182cac3ff7998974"),"UrbanBloodragerProficiencies01463","Urban Bloodrager Proficiencies","Proficient with simple and martial weapons and light and medium armor. Not proficient with shields. Native armored bloodrager spellcasting is retained.");
            var proficiencyFacts=proficiency.GetComponent<AddFacts>()??throw new InvalidOperationException("Urban bloodrager requires native proficiencies.");
            Set(proficiencyFacts,"m_Facts",proficiencyFacts.Facts.Where(x=>x.AssetGuid!=BlueprintGuid.Parse("cb8686e7357a68c42bdd9d4e65334633")).Select(Ref<BlueprintUnitFactReference>).ToArray());
            var restraint=BuildRestraint(f);
            var adopted=BuildAdopted(f,cls);
            var additions=new[]{ArchetypeBatch01445.Entry(1,controlled,proficiency),ArchetypeBatch01445.Entry(3,restraint)}.Concat(new[]{7,10,13,16,19}.Select(l=>ArchetypeBatch01445.Entry(l,adopted))).ToArray();
            var remove=new[]{ArchetypeBatch01445.Entry(1,source,f.Native<BlueprintFeature>("7aa59f5998e5baf4182cac3ff7998974")),ArchetypeBatch01445.Entry(3,f.Native<BlueprintFeature>("70d9d54bd349a63448032b5ac2e5c47d"))}.Concat(new[]{7,10,13,16,19}.Select(l=>ArchetypeBatch01445.Entry(l,f.Native<BlueprintFeature>("07eba4bb72c2e3845bb442dce85d3b58")))).ToArray();
            var root=SpellBatch01447.Archetype(f,cls,RootToken,"Urban Bloodrager","An urban bloodrager controls her bloodrage in polite society, distributes its power among her physical abilities and learns magic from other urban traditions.",additions,remove);
            root.ReplaceClassSkills=true;root.ClassSkills=new[]{StatType.SkillKnowledgeArcana,StatType.SkillKnowledgeWorld,StatType.SkillMobility,StatType.SkillPerception,StatType.SkillAthletics,StatType.SkillPersuasion};
            foreach(var feature in f.Assets.Values.OfType<BlueprintFeature>())feature.IsClassFeature=true;
            var group=MagusBatch01454.Group(new BlueprintFeatureBase[]{controlled,proficiency,restraint,adopted});var undo=MagusBatch01454.CaptureClass(cls);
            void Connect(){MagusBatch01454.ConnectArchetype(cls,root,group);MagusBatch01454.Determinators(cls,controlled,proficiency);}
            void Verify(){MagusBatch01454.VerifyArchetype(cls,root,group);MagusBatch01454.VerifyDeterminators(cls,controlled,proficiency);foreach(var e in root.AddFeatures.Concat(root.RemoveFeatures)){var refs=e.Features;}}
            MagusBatch01454.Publish(ModuleName,f,Connect,undo,Verify);return true;
        }
        static BlueprintFeature BuildRestraint(PsychicAssets f)
        {
            var icon=f.Native<BlueprintFeature>("70d9d54bd349a63448032b5ac2e5c47d");
            var effect=f.Buff("UrbanRestrainedMagicBuff01463","Restrained Magic",RestrainedText,new UrbanRestrainedSave01463());effect.Stacking=StackingType.Stack;Icon(effect,icon);
            var apply=new AbilityAreaEffectBuff{CheckConditionEveryRound=true,Condition=new Kingmaker.ElementsSystem.ConditionsChecker{Conditions=new Kingmaker.ElementsSystem.Condition[]{new ContextConditionIsEnemy{Not=true}}}};Set(apply,"m_Buff",Ref<BlueprintBuffReference>(effect));
            var area=f.Add(new BlueprintAbilityAreaEffect{Size=60.Feet(),Shape=AreaEffectShape.Cylinder,AffectEnemies=true,SpellResistance=false,Fx=new PrefabLink{AssetId=AuraRadiusVisual01458.RingAsset},ComponentsArray=new BlueprintComponent[]{apply}},AreaToken,"Restrained Magic",RestrainedText);SetEnum(area,"m_TargetType","Any");
            var emitter=new AddAreaEffect();Set(emitter,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));
            var host=f.Buff("UrbanRestrainedMagicHost01463","Restrained Magic",RestrainedText,emitter);Icon(host,icon);
            var feature=f.Feature("UrbanRestrainedMagicFeature01463","Restrained Magic",RestrainedText,Facts(host));Icon(feature,icon);return feature;
        }
        static BlueprintFeatureSelection BuildAdopted(PsychicAssets f,BlueprintCharacterClass cls)
        {
            var own=new HashSet<BlueprintGuid>(cls.Spellbook.SpellList.SpellsByLevel.SelectMany(e=>e.Spells).Select(a=>a.AssetGuid));
            var bard=f.Native<BlueprintSpellList>("25a5013493bdcf74bb2424532214d0c8");var magus=f.Native<BlueprintSpellList>("4d72e1e7bd6bc4f4caaea7aa43a14639");
            var list=SpellBatch01447.List(f,"UrbanAdoptedSpellList01463",level=>level<1||level>4?Array.Empty<BlueprintAbility>():bard.SpellsByLevel.Concat(magus.SpellsByLevel).Where(e=>e.SpellLevel==level).SelectMany(e=>e.Spells).Where(a=>!own.Contains(a.AssetGuid)).GroupBy(a=>a.AssetGuid).Select(g=>g.First()).ToArray());
            var reserved=new SpellLevelList(10);Set(reserved,"m_Spells",new List<BlueprintAbilityReference>());list.SpellsByLevel=list.SpellsByLevel.Concat(new[]{reserved}).ToArray();
            var selection=f.Add(new BlueprintFeatureSelection{Ranks=5,IsClassFeature=true},"UrbanAdoptedMagicFeature01463","Adopted Magic",AdoptedText);Icon(selection,f.Native<BlueprintFeature>("70d9d54bd349a63448032b5ac2e5c47d"));
            var children=new List<BlueprintFeature>();
            for(int level=1;level<=4;level++){
                var learn=new LearnSpellParametrized{SpecificSpellLevel=true,SpellLevel=level};Set(learn,"m_SpellcasterClass",Ref<BlueprintCharacterClassReference>(cls));Set(learn,"m_SpellList",Ref<BlueprintSpellListReference>(list));
                var requirement=new PrerequisiteClassSpellLevel{RequiredSpellLevel=level};Set(requirement,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));
                var choice=f.Clone(f.Native<BlueprintParametrizedFeature>("bcd757ac2aeef3c49b77e5af4e510956"),"UrbanAdoptedMagicLevel"+level+"01463","Adopted Magic (Level "+level+")",AdoptedText);
                choice.ParameterType=FeatureParameterType.LearnSpell;choice.SpecificSpellLevel=true;choice.SpellLevel=level;choice.SpellLevelPenalty=0;Set(choice,"m_SpellcasterClass",Ref<BlueprintCharacterClassReference>(cls));Set(choice,"m_SpellList",Ref<BlueprintSpellListReference>(list));choice.ComponentsArray=new BlueprintComponent[]{learn,requirement};Icon(choice,selection);children.Add(choice);
            }
            foreach(string field in new[]{"m_Features","m_AllFeatures"})Set(selection,field,children.Select(Ref<BlueprintFeatureReference>).ToArray());return selection;
        }
        internal static string AllocationToken(string stat,int portion,string kind)=>"UrbanBloodrageAllocation"+stat+portion+kind+"01463";
        internal static string AllocationAbilityToken(string stat,int portion)=>portion==0&&stat!="Strength"?"UrbanBloodrage"+stat+"Toggle01463":AllocationToken(stat,portion,"Ability");
        internal static string AllocationMarkerToken(string stat,int portion)=>portion==0&&stat!="Strength"?"UrbanBloodrage"+stat+"Buff01463":AllocationToken(stat,portion,"Marker");
        internal static readonly HashSet<BlueprintGuid> AllocationIds=new HashSet<BlueprintGuid>(Stats.SelectMany(s=>Enumerable.Range(0,4).Select(i=>Id(AllocationAbilityToken(s,i)))));
    }

    [TypeId("7a837f340eaa4abfa7169320848d5d71")]
    public sealed class UrbanBloodrageAllocationRestriction:Kingmaker.UnitLogic.ActivatableAbilities.Restrictions.ActivatableAbilityRestriction
    {
        public int Portion;
        protected override bool IsAvailable()
        {
            var unit=Owner;if(unit==null||unit.Descriptor==null)return false;
            var cls=ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(UrbanBloodrager01463.ClassId);if(cls==null)return false;
            int level=unit.Descriptor.Progression.GetClassLevel(cls),budget=level>=20?4:level>=11?3:level>=1?2:0;
            if(Portion<0||Portion>=budget)return false;
            var active=unit.ActivatableAbilities.RawFacts.Where(a=>UrbanBloodrager01463.AllocationIds.Contains(a.Blueprint.AssetGuid)&&a.IsOn).ToArray();
            var current=Fact as ActivatableAbility;
            if(current!=null&&current.IsOn)return active.OrderBy(a=>a.Blueprint.AssetGuid.ToString()).Take(budget).Contains(current);
            return active.Length<budget;
        }
    }

    internal static class UrbanBloodrageBridge01463
    {
        // BlueprintGuid.Equals(object) does not compare two BlueprintGuid values.
        // Mono's array Contains can use that boxed equality, unlike CLR's typed
        // comparison. Use the native strongly typed operator on both runtimes.
        internal static bool Own(BlueprintBuff buff)
        {
            if(buff==null)return false;
            foreach(var id in UrbanBloodrager01463.BuffIds)if(id==buff.AssetGuid)return true;
            return false;
        }
        internal static bool Active(UnitDescriptor owner)=>owner!=null&&owner.Progression.Features.Enumerable.Any(f=>f.Blueprint.AssetGuid==UrbanBloodrager01463.FeatureId)&&owner.Buffs.Enumerable.Any(b=>Own(b.Blueprint)&&b.IsActive&&b.IsTurnedOn&&b.IsAttached);
        internal static bool Feature(FeatureCollection features,BlueprintFact feature)=>FeralWildshapePrerequisites01461.Feature(features,feature)||(feature?.AssetGuid==UrbanBloodrager01463.NativeRage&&features.Enumerable.Any(f=>f.Blueprint.AssetGuid==UrbanBloodrager01463.FeatureId));
        internal static BlueprintBuff CompatibleCheckedBuff(BuffExtraEffects component,Buff buff)
        {
            if(!Own(buff?.Blueprint))return component.CheckedBuff;
            bool matches=component.CheckedBuff?.AssetGuid==UrbanBloodrager01463.NativeBuff||((BlueprintBuffReference[])Get(component,"m_CheckedBuffList"))?.Any(r=>r?.Guid==UrbanBloodrager01463.NativeBuff)==true;
            return matches?buff.Blueprint:component.CheckedBuff;
        }
        internal static IEnumerable<CodeInstruction> BuffEvents(IEnumerable<CodeInstruction> instructions)
        {
            int count=0;var getter=AccessTools.PropertyGetter(typeof(BuffExtraEffects),nameof(BuffExtraEffects.CheckedBuff));
            foreach(var instruction in instructions){
                if(instruction.operand as MethodInfo==getter){
                    if(instruction.blocks.Count!=0)throw new InvalidOperationException("Unexpected exception boundary at native bloodrage buff comparison.");
                    var incoming=new CodeInstruction(OpCodes.Ldarg_1);incoming.labels.AddRange(instruction.labels);yield return incoming;
                    yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(UrbanBloodrageBridge01463),nameof(CompatibleCheckedBuff)));count++;
                }else yield return instruction;
            }
            if(count!=1)throw new InvalidOperationException("Urban bloodrage requires exactly one native checked-buff comparison; got "+count);
        }
        internal static IEnumerable<CodeInstruction> Qualify(IEnumerable<CodeInstruction> instructions)
        {
            var old=AccessTools.Method(typeof(FeralWildshapePrerequisites01461),nameof(FeralWildshapePrerequisites01461.Feature));int count=0;
            foreach(var instruction in instructions){
                var method=instruction.operand as MethodInfo;
                bool native=method?.DeclaringType==typeof(Kingmaker.EntitySystem.EntityFactsProcessor<Feature>)&&method.Name=="HasFact"&&method.GetParameters().Length==1&&method.GetParameters()[0].ParameterType==typeof(BlueprintFact);
                // Patch installation and owner-scoped unpatch can temporarily
                // leave either the native call or the existing Feral adapter.
                // Both must preserve the same complete native/Feral semantics.
                if(method==old||native){instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(UrbanBloodrageBridge01463),nameof(Feature));count++;}yield return instruction;
            }
            if(count!=1)throw new InvalidOperationException("Urban bloodrage requires exactly one retained native/Feral feature ownership path; got "+count);
        }
        internal static bool NativeFact(UnitDescriptor owner,BlueprintFact fact)=>UnitHelper.HasFact(owner,fact)||(fact?.AssetGuid==UrbanBloodrager01463.NativeBuff&&Active(owner));
        internal static bool NativeUnitFact(Kingmaker.EntitySystem.Entities.UnitEntityData owner,BlueprintFact fact)=>UnitHelper.HasFact(owner,fact)||(fact?.AssetGuid==UrbanBloodrager01463.NativeBuff&&Active(owner?.Descriptor));
        internal static IEnumerable<CodeInstruction> LocalFactCalls(IEnumerable<CodeInstruction> instructions,MethodBase consumer)
        {
            int count=0;
            foreach(var instruction in instructions){
                var method=instruction.operand as MethodInfo;
                if(instruction.opcode==OpCodes.Call&&method?.DeclaringType==typeof(UnitHelper)&&method.Name==nameof(UnitHelper.HasFact)){
                    var args=method.GetParameters();
                    if(args.Length==2&&args[1].ParameterType==typeof(BlueprintFact)){
                        string replacement=args[0].ParameterType==typeof(UnitDescriptor)?nameof(NativeFact):args[0].ParameterType==typeof(Kingmaker.EntitySystem.Entities.UnitEntityData)?nameof(NativeUnitFact):null;
                        if(replacement!=null){instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(UrbanBloodrageBridge01463),replacement);count++;}
                    }
                }
                yield return instruction;
            }
            if(count==0)try{ModEntry.TraceNatureTarget("Urban bloodrage local compatibility skipped: no supported fact call in "+consumer+"; existing patches retained.");}catch(Exception){/* Failure to log must not interrupt unrelated startup. */}
        }
    }
    [HarmonyPatch(typeof(UnitHelper),nameof(UnitHelper.HasFact),new[]{typeof(UnitDescriptor),typeof(BlueprintFact)})]
    internal static class UrbanBloodrageFact01463Patch
    {
        static void Postfix(UnitDescriptor _this,BlueprintFact blueprint,ref bool __result)
        {
            if(__result||blueprint==null)return;
            if(blueprint.AssetGuid==UrbanBloodrager01463.NativeBuff)__result=UrbanBloodrageBridge01463.Active(_this);
            else if(blueprint.AssetGuid==UrbanBloodrager01463.NativeRage)__result=_this.Progression.Features.Enumerable.Any(f=>f.Blueprint.AssetGuid==UrbanBloodrager01463.FeatureId);
        }
    }
    [HarmonyPatch(typeof(PrerequisiteFeature),"CheckInternal")]
    internal static class UrbanBloodragePrerequisite01463Patch
    {
        [HarmonyPriority(Priority.Last)]static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)=>UrbanBloodrageBridge01463.Qualify(instructions);
    }
    [HarmonyPatch(typeof(BuffExtraEffects),nameof(BuffExtraEffects.HandleBuffDidAdded))]
    internal static class UrbanBloodrageAdded01463Patch
    {
        static readonly PropertyInfo ComponentOwner=AccessTools.Property(typeof(BuffExtraEffects),"Owner");
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)=>UrbanBloodrageBridge01463.BuffEvents(instructions);
        static void Postfix(BuffExtraEffects __instance,Buff buff)
        {
            try{
                if(!UrbanBloodrageBridge01463.Own(buff?.Blueprint)||__instance.CheckedBuff?.AssetGuid!=UrbanBloodrager01463.NativeBuff||__instance.ExtraEffectBuff?.AssetGuid!=BlueprintGuid.Parse("ecc22ca1eea1bf6488a0d7c6ee2527d8"))return;
                var owner=buff.Owner;if(owner==null||ComponentOwner?.GetValue(__instance) as Kingmaker.EntitySystem.Entities.UnitEntityData!=owner.Unit)return;
                bool hasFeature=owner.Progression.Features.Enumerable.Any(f=>f.Blueprint.AssetGuid==BlueprintGuid.Parse("c846fed2d31e1734ebad07a64f72df2f"));
                bool hasHost=owner.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==BlueprintGuid.Parse("ecc22ca1eea1bf6488a0d7c6ee2527d8")&&b.IsActive&&b.IsTurnedOn&&b.IsAttached);
                ModEntry.TraceNatureTarget("Urban bloodrage Primalist event: feature="+hasFeature+", controlledActive="+UrbanBloodrageBridge01463.Active(owner)+", hostActive="+hasHost);
            }catch(Exception){/* Diagnostic logging must never interrupt the native buff event. */}
        }
    }
    [HarmonyPatch(typeof(BuffExtraEffects),nameof(BuffExtraEffects.HandleBuffDidRemoved))]
    internal static class UrbanBloodrageRemoved01463Patch{static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)=>UrbanBloodrageBridge01463.BuffEvents(instructions);}
    [HarmonyPatch(typeof(BuffExtraEffects),"CheckHadPrerequiredBuff")]
    internal static class UrbanBloodrageBuffConsumerPatch{static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,MethodBase __originalMethod)=>UrbanBloodrageBridge01463.LocalFactCalls(instructions,__originalMethod);}
    [HarmonyPatch(typeof(ContextConditionHasFact),"CheckCondition")]
    internal static class UrbanBloodrageConditionConsumerPatch{static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,MethodBase __originalMethod)=>UrbanBloodrageBridge01463.LocalFactCalls(instructions,__originalMethod);}

    [TypeId("5fbc2ed534484284b7632bdc7ee01463")]
    public sealed class UrbanRestrainedSave01463:UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleSavingThrow>,IInitiatorRulebookSubscriber
    {
        static readonly ConditionalWeakTable<RuleSavingThrow,HashSet<Kingmaker.EntitySystem.Entities.UnitEntityData>> Applied=new ConditionalWeakTable<RuleSavingThrow,HashSet<Kingmaker.EntitySystem.Entities.UnitEntityData>>();
        public void OnEventAboutToTrigger(RuleSavingThrow e)
        {
            var source=Context?.MaybeCaster;var reason=e.Reason?.Context;var spell=reason?.SourceAbility;
            if(source==null||reason?.MaybeCaster!=source||spell?.Type!=AbilityType.Spell||source.IsEnemy(e.Initiator))return;
            var seen=Applied.GetOrCreateValue(e);if(seen.Add(source))e.AddModifier(2,Fact,ModifierDescriptor.UntypedStackable);
        }
        public void OnEventDidTrigger(RuleSavingThrow e){}
    }
}
