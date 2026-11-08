// Missing original CotW Psychic disciplines. Native assets are deep-cloned;
// no native class progression, mercy prerequisite or cognatogen is rewritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicRemainingDisciplines01458
    {
        internal static string N(string key)=>PsychicTexts01458.Name(key);
        internal static string D(string key)=>key=="SynapticShockTargetBuff"?"Use Synaptic Shock Target to mark one enemy, then enable Synaptic Shock before casting a mind-affecting area or multiple-target spell. The mark only selects a target. Spend 1 phrenic point to confuse that target for one round: a successful lasting spell delays confusion until its effect ends; a resisted or instantaneous spell starts confusion immediately. Immunity to mind-affecting effects still applies unless the linked spell can overcome it. The mark is consumed once used and is cleared when this mode is disabled.":PsychicTexts01458.Description(key)+(key=="UndercastSurge1Buff"?" Enable the desired tier and cast the lower version normally. It executes the matching higher version available within your known spell family, spends the lower slot, and costs two phrenic points per tier. No eligible higher version or insufficient points leaves the ordinary spell unamplified and spends no phrenic points.":"");
        internal static void Icon(BlueprintUnitFact dst,BlueprintUnitFact source)=>Set(dst,"m_Icon",Get(source,"m_Icon"));
        internal static ContextCalculateAbilityParamsBasedOnClass Params(BlueprintCharacterClass cls){var c=new ContextCalculateAbilityParamsBasedOnClass{StatType=StatType.Intelligence};Set(c,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));return c;}
        static ContextRankConfig BonusRank(BlueprintCharacterClass cls,int bonus){var rank=Rank(cls,"BonusValue");Set(rank,"m_StepLevel",bonus);return rank;}
        internal static BlueprintAbilityResource Daily(PsychicAssets f,string token,int value,StatType stat,BlueprintCharacterClass cls=null,int step=0)
        {
            var r=f.Add(new BlueprintAbilityResource(),token,"","");var a=Get(r,"m_MaxAmount");Set(a,"BaseValue",value);Set(a,"IncreasedByStat",stat!=StatType.Unknown);Set(a,"ResourceBonusStat",stat);
            Set(a,"m_Class",Array.Empty<BlueprintCharacterClassReference>());Set(a,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());Set(a,"m_ArchetypesDiv",Array.Empty<BlueprintArchetypeReference>());
            Set(a,"m_ClassDiv",cls==null?Array.Empty<BlueprintCharacterClassReference>():new[]{Ref<BlueprintCharacterClassReference>(cls)});
            if(step>0){Set(a,"IncreasedByLevelStartPlusDivStep",true);Set(a,"StartingLevel",step);Set(a,"StartingIncrease",1);Set(a,"LevelStep",step);Set(a,"PerStepIncrease",1);}
            Set(r,"m_MaxAmount",a);return r;
        }
        internal static IEnumerable<object> Graph(object value,HashSet<object> seen=null)
        {
            if(value==null||value is string||value.GetType().IsValueType||value is UnityEngine.Object||value is SimpleBlueprint)return Enumerable.Empty<object>();
            if(seen==null||!ReferenceEquals(seen.Comparer,IdentityComparer))seen=new HashSet<object>(seen??Enumerable.Empty<object>(),IdentityComparer);if(!seen.Add(value))return Enumerable.Empty<object>();
            var result=new List<object>{value};if(value is BlueprintReferenceBase)return result;
            if(value is IEnumerable sequence){foreach(var v in sequence)result.AddRange(Graph(v,seen));return result;}
            for(var t=value.GetType();t!=null&&t!=typeof(object)&&t!=typeof(BlueprintComponent)&&t!=typeof(Element);t=t.BaseType)
                foreach(var field in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(v=>!v.IsStatic))result.AddRange(Graph(field.GetValue(value),seen));
            return result;
        }
        static BlueprintProgression Discipline(PsychicAssets f,BlueprintCharacterClass cls,string key,BlueprintFeature first,BlueprintFeature fifth,BlueprintFeature thirteenth,params string[] spells)
        {
            if(spells.Length!=9)throw new InvalidOperationException("058 discipline requires nine original bonus spells.");
            var list=new List<LevelEntry>();int[] levels={1,4,6,8,10,12,14,16,18};
            for(int i=0;i<9;i++){
                bool external=spells[i]=="ff31ae1abe3c418db7842dcc76eca7ee";
                var spell=f.Assets.Values.OfType<BlueprintAbility>().FirstOrDefault(v=>v.AssetGuid==BlueprintGuid.Parse(spells[i]));
                if(spell==null)spell=external?f.Cache.Load(BlueprintGuid.Parse(spells[i])) as BlueprintAbility:f.Native<BlueprintAbility>(spells[i]);
                AddKnownSpell add=external?new OptionalDisciplineSpell01458():new AddKnownSpell();add.SpellLevel=i+1;Set(add,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));var reference=new BlueprintAbilityReference();Set(reference,"deserializedGuid",BlueprintGuid.Parse(spells[i]));Set(add,"m_Spell",reference);
                string title=spell?.Name??"Inflict Pain, Mass";var bonus=f.Feature(key+"BonusSpell"+(i+1)+"01458","Discipline Spell: "+title,"Learn "+title+" in addition to the normal Psychic spells known."+(external?" This spell is supplied by Expanded Content when its spell content is enabled.":""),add);Icon(bonus,spell??f.Native<BlueprintAbility>("137af566f68fd9b428e2e12da43c1482"));
                var pool=f.Native<BlueprintFeature>(key=="Psychedelia"?PsychicClass.WisdomFeatureId:PsychicClass.CharismaFeatureId);
                list.Add(i==0?Entry(1,bonus,first,pool):Entry(levels[i],bonus));
            }
            list.Add(Entry(5,fifth));list.Add(Entry(13,thirteenth));
            var p=f.Add(new BlueprintProgression{Ranks=1,IsClassFeature=true,LevelEntries=list.OrderBy(v=>v.Level).ToArray(),UIGroups=Array.Empty<UIGroup>()},key+"Discipline01458",N(key),D(key)+"\nPhrenic Pool Ability: "+(key=="Psychedelia"?"Wisdom":"Charisma"));
            Set(p,"m_Classes",new[]{new BlueprintProgression.ClassWithLevel{m_Class=Ref<BlueprintCharacterClassReference>(cls)}});Icon(p,first);return p;
        }
        internal static BlueprintProgression[] Build(PsychicAssets f,BlueprintCharacterClass cls)
        {
            var nausea=f.Clone(f.Native<BlueprintBuff>("956331dba5125ef48afe41875a00ca0e"),"PsychicNauseated01458","Nauseated","Nauseated by psychic influence.");
            foreach(var descriptor in nausea.GetComponents<SpellDescriptorComponent>())descriptor.Descriptor=SpellDescriptor.Nauseated;
            return new[]{Pain(f,cls),Psychedelia(f,cls),Abomination(f,cls)};
        }
        static BlueprintProgression Pain(PsychicAssets f,BlueprintCharacterClass cls)
        {
            var marker=f.Buff("PainfulReminderMarker01458",N("PainfulReminderBuff"),D("PainfulReminderBuff"));marker.Stacking=StackingType.Stack;Icon(marker,f.Native<BlueprintAbility>("55f14bc84d7c85446b07a1b5dd6b2b4c"));
            var uses=Daily(f,"PainfulReminderResource01458",3,StatType.Charisma);
            var target=new AbilityTargetHasFact{FromCaster=true};Set(target,"m_CheckedFacts",new[]{Ref<BlueprintUnitFactReference>(marker)});
            var damage=new ContextActionDealDamage{DamageType=new DamageTypeDescription{Type=DamageType.Direct},Value=new ContextDiceValue{DiceType=DiceType.D6,DiceCountValue=new ContextValue{ValueType=ContextValueType.Rank},BonusValue=0},IgnoreCritical=true,DisableSneakDamage=true,WriteResultToSharedValue=true,ResultSharedValue=AbilitySharedValue.Damage};
            var reminder=f.Ability("PainfulReminderAbility01458","Painful Reminder",D("PainfulReminderBuff"),AbilityType.Supernatural,UnitCommand.CommandType.Swift,AbilityRange.Unlimited,Cost(uses),Rank(cls,"Custom",7,1,14,2,20,3),target,Run(damage,new RestorePainfulReminder01458()));Icon(reminder,marker);reminder.CanTargetFriends=reminder.CanTargetSelf=false;
            var first=f.Feature("PainfulReminderFeature01458",reminder.Name,D("PainfulReminderBuff"),Facts(reminder),Grant(uses),new PsychicDamageLink01458{Kind=PsychicDamageKind01458.Reminder,Marker=Ref<BlueprintBuffReference>(marker)});Icon(first,marker);
            var live=LiveOn(f,cls);
            var woundResource=Daily(f,"AgonizingWoundResource01458",3,StatType.Charisma);
            var wound=f.Feature("AgonizingWoundFeature01458",N("AgonizingWoundFeature"),D("AgonizingWoundFeature"),Grant(woundResource));Icon(wound,f.Native<BlueprintAbility>("137af566f68fd9b428e2e12da43c1482"));
            var selector=f.Buff("AgonizingWoundTarget01458","Agonizing Wound Target",D("AgonizingWoundFeature"),new SpellDescriptorComponent{Descriptor=SpellDescriptor.MindAffecting});selector.Stacking=StackingType.Stack;Icon(selector,wound);
            wound.ComponentsArray=wound.ComponentsArray.Concat(new[]{new AgonizingTargets01458{Marker=Ref<BlueprintBuffReference>(selector)}}).ToArray();
            var select=f.Ability("AgonizingWoundTargetAbility01458",selector.Name,D("AgonizingWoundFeature"),AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Unlimited,Run(new SelectAgonizingTarget01458()),new SpellDescriptorComponent{Descriptor=SpellDescriptor.MindAffecting});Icon(select,wound);select.CanTargetFriends=select.CanTargetSelf=false;
            var modes=new List<BlueprintUnitFact>();string[] ids={"f08a7239aa961f34c8301518e71d4cdf","4e42460798665fd4cb9173ffa7ada323","9934fedff1b14994ea90205d189c8759","956331dba5125ef48afe41875a00ca0e"};
            var woundChoices=new List<BlueprintAbility>{select};
            for(int i=0;i<ids.Length;i++){
                var effect=i==3?f.Find<BlueprintBuff>("PsychicNauseated01458"):f.Native<BlueprintBuff>(ids[i]);var mode=f.Buff("AgonizingWound"+i+"Mode01458","Agonizing Wound: "+effect.Name,D("AgonizingWoundFeature"),new PsychicDamageLink01458{Kind=PsychicDamageKind01458.Agonizing,Marker=Ref<BlueprintBuffReference>(selector),Effect=Ref<BlueprintBuffReference>(effect),Resource=Ref<BlueprintAbilityResourceReference>(woundResource),Cost=i<2?1:2},new SpellDescriptorComponent{Descriptor=SpellDescriptor.MindAffecting});Icon(mode,effect);
                var toggle=PsychicCompletionAmplifications01458.Toggle(f,"AgonizingWound"+i+"01458",mode,false);toggle.DeactivateImmediately=false;toggle.HiddenInUI=true;toggle.ActionBarAutoFillIgnored=true;modes.Add(toggle);
            }
            var variants=new AbilityVariants();Set(variants,"m_Variants",woundChoices.Select(a=>Ref<BlueprintAbilityReference>(a)).ToArray());
            var menu=f.Ability("AgonizingWoundMenu01460","Agonizing Wound",wound.Description,AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Personal,variants);Icon(menu,wound);
            foreach(var a in woundChoices){Set(a,"m_Parent",Ref<BlueprintAbilityReference>(menu));a.ActionBarAutoFillIgnored=true;}
            modes.Add(menu);
            wound.ComponentsArray=wound.ComponentsArray.Concat(new[]{Facts(modes.ToArray())}).ToArray();
            return Discipline(f,cls,"Pain",first,live,wound,"fa3078b9976a5b24caf92e20ee9c0f54",Id("PainStrike").ToString(),"8a28a811ca5d20d49a863e832c31cce1",Id("PainStrikeMass").ToString(),Id("SynapseOverload").ToString(),"ff31ae1abe3c418db7842dcc76eca7ee","3e4d3b9a5bd03734d9b053b9067c2f38","08323922485f7e246acb3d2276515526",Id("SuffocationMass").ToString());
        }
        static BlueprintProgression LiveOn(PsychicAssets f,BlueprintCharacterClass cls)
        {
            var uses=Daily(f,"LiveOnResource01458",-1,StatType.Charisma,cls,2);
            var maximum=Get(uses,"m_MaxAmount");Set(maximum,"BaseValue",0);Set(maximum,"StartingLevel",3);Set(maximum,"StartingIncrease",0);Set(uses,"m_MaxAmount",maximum);
            var native=f.Native<BlueprintAbility>("8d6073201e5395d458b8251386d72df1");var ability=f.Clone(native,"LiveOnAbility01458",N("LiveOn"),D("LiveOn"));
            ability.ComponentsArray=ability.ComponentsArray.Where(c=>!(c is AbilityResourceLogic)&&!(c is ContextRankConfig)&&!(c is AbilityCasterAlignment)).Concat(new BlueprintComponent[]{Cost(uses),Rank(cls,"Custom",4,0,6,1,8,2,10,3,12,4,14,5,16,6,18,7,20,8),new LiveOnCasterLevel01458()}).ToArray();ability.CanTargetSelf=true;ability.CanTargetFriends=ability.CanTargetEnemies=false;ability.Range=AbilityRange.Personal;
            var original=f.Native<BlueprintFeatureSelection>("02b187038a8dce545bb34bbfb346428d");
            if(original.AllFeatures.Any(v=>v==null))throw new InvalidOperationException("058 Live On requires resolved native mercy selection entries; registration rolled back.");
            // Keep the selection wrapper private, but reference every native Paladin
            // mercy directly. Names, descriptions, icons, prerequisite chains and
            // any provider-added mercies therefore remain owned by the Paladin
            // catalogue instead of being copied into a parallel Psychic graph.
            var selection=f.Clone(original,"LiveOnMercySelection01458",original.Name,D("LiveOn"));
            Set(selection,"m_Features",original.AllFeatures.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(selection,"m_AllFeatures",original.AllFeatures.Select(Ref<BlueprintFeatureReference>).ToArray());
            var live=f.Add(new BlueprintProgression{Ranks=1,IsClassFeature=true,ComponentsArray=new BlueprintComponent[]{Facts(ability),Grant(uses)},LevelEntries=new[]{6,9,12,15,18}.Select(l=>Entry(l,selection)).ToArray(),UIGroups=Array.Empty<UIGroup>()},"LiveOnProgression01458",N("LiveOn"),D("LiveOn"));Set(live,"m_Classes",new[]{new BlueprintProgression.ClassWithLevel{m_Class=Ref<BlueprintCharacterClassReference>(cls)}});Icon(live,ability);return live;
        }
        static BlueprintProgression Psychedelia(PsychicAssets f,BlueprintCharacterClass cls)
        {
            var native=f.Native<BlueprintFeature>("e3f460ea61fcc504183c7d6818bbbf7a");var resource=f.Native<BlueprintAbilityResource>("3b163587f010382408142fc8a97852b6");
            PsychedeliaCognatogens01458.Initialize(f.Cache);
            // Grant the real native discovery, as original CotW does: its
            // prerequisites, Greater/Grand replacements, infusion and shared
            // resource remain one native graph. Only this discipline's owner
            // receives the stacked level and one-minute duration adaptation.
            var cognatogen=f.Feature("PsychedeliaCognatogenFeature01458",N("PsychedeliaCognatogen"),D("PsychedeliaCognatogen"),Facts(native),Grant(resource));Icon(cognatogen,native);
            var warped=f.Feature("WarpedBrainFeature01458",N("WarpedBrainFeature"),D("WarpedBrainFeature"),new WarpedBrain01458{Effect=Ref<BlueprintBuffReference>(f.Find<BlueprintBuff>("PsychicNauseated01458"))},new SpellDescriptorComponent{Descriptor=SpellDescriptor.MindAffecting});Icon(warped,f.Native<BlueprintAbility>("eabf94e4edc6e714cabd96aa69f8b207"));
            var cooldown=f.Buff("HallucinogenicAuraCooldown01458",N("HallucinogenicAuraCooldownBuff"),D("HallucinogenicAuraCooldownBuff"));cooldown.Stacking=StackingType.Stack;
            var confused=f.Clone(f.Native<BlueprintBuff>("886c7407dc629dc499b9f1465ff382df"),"HallucinogenicAuraConfusion01458","Hallucinogenic Aura",D("HallucinogenicAuraCooldownBuff"));confused.Stacking=StackingType.Stack;
            var after=Apply(cooldown,1,DurationRate.Days,fromSpell:false);after.AsChild=false;after.IsNotDispelable=true;confused.ComponentsArray=confused.ComponentsArray.Concat(new[]{new AddFactContextActions{Activated=Actions(),Deactivated=Actions(after),NewRound=Actions(),Dispose=Actions()}}).ToArray();
            var area=f.Add(new BlueprintAbilityAreaEffect{Size=20.Feet(),Shape=AreaEffectShape.Cylinder,AffectEnemies=true,SpellResistance=false,Fx=new PrefabLink{AssetId=AuraRadiusVisual01458.RingAsset},ComponentsArray=new BlueprintComponent[]{Params(cls),new SpellDescriptorComponent{Descriptor=SpellDescriptor.MindAffecting},new AbilityAreaEffectRunAction{UnitEnter=Actions(new HallucinogenicAura01458{Cooldown=Ref<BlueprintBuffReference>(cooldown),Confusion=Ref<BlueprintBuffReference>(confused)}),UnitExit=Actions(),UnitMove=Actions(),Round=Actions()}}},"HallucinogenicAuraArea01458","Hallucinogenic Aura",D("HallucinogenicAuraCooldownBuff"));SetEnum(area,"m_TargetType","Enemy");
            var emitter=new AddAreaEffect();Set(emitter,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));
            var host=f.Buff("HallucinogenicAuraHost01460","Hallucinogenic Aura",D("HallucinogenicAuraCooldownBuff"),emitter,Params(cls));Icon(host,confused);
            var aura=f.Feature("HallucinogenicAuraFeature01458","Hallucinogenic Aura",D("HallucinogenicAuraCooldownBuff"),Facts(host),Params(cls));Icon(aura,confused);
            return Discipline(f,cls,"Psychedelia",cognatogen,warped,aura,"40ec382849b60504d88946df46a10f2d","fd4d9fd7f87575d47aafe2a64a6e2d8d",Id("Synesthesia").ToString(),"cf6c901fb7acc904e85c63b342e9c949","12fb4a4c22549c74d949e2916a2f0b6a","15a04c40f84545949abeedef7279751a","1e2d1489781b10a45a3b70192bba9be3","740d943e42b60f64a8de74926ba6ddf7",Id("DivideMind").ToString());
        }
        static BlueprintProgression Abomination(PsychicAssets f,BlueprintCharacterClass cls)
        {
            var morph=f.Feature("MorphicFormFeature01458",N("MorphicForFeature"),D("MorphicForFeature"));Icon(morph,f.Native<BlueprintAbility>("9e1ad5d6f87d19e4d8883d63a6e35568"));
            var safeguard=f.Feature("PsychicSafeguardFeature01458",N("PsychicSafeGuardFeature"),D("PsychicSafeGuardFeature"),new AddSpellResistance{Value=new ContextValue{ValueType=ContextValueType.Rank}},BonusRank(cls,8));safeguard.ReapplyOnLevelUp=true;Icon(safeguard,morph);
            var stronger=f.Buff("PsychicSafeguardBuff01458",safeguard.Name,safeguard.Description,new AddSpellResistance{Value=new ContextValue{ValueType=ContextValueType.Rank}},BonusRank(cls,16));Icon(stronger,safeguard);
            var forms=new List<BlueprintBuff>();for(int i=0;i<3;i++){var dr=new AddDamageResistancePhysical{Value=5};if(i==0){dr.BypassedByForm=true;dr.Form=PhysicalDamageForm.Bludgeoning;}if(i==1){dr.BypassedByMaterial=true;dr.Material=PhysicalDamageMaterial.ColdIron;}if(i==2)dr.BypassedByMagic=true;var b=f.Buff("MorphicForm"+i+"Buff01458",morph.Name,morph.Description,dr);Icon(b,morph);forms.Add(b);}
            var bleed=f.Clone(f.Native<BlueprintBuff>("75039846c3d85d940aa96c249b97e562"),"DarkHalfBleedBuff01458",N("DarkHalfBleedBuff"),D("DarkHalfBleedBuff"));var tick=bleed.GetComponent<AddFactContextActions>()?.NewRound.Actions.OfType<ContextActionDealDamage>().FirstOrDefault()??throw new InvalidOperationException("058 native bleed round action missing.");tick.Value=new ContextDiceValue{DiceType=DiceType.Zero,DiceCountValue=0,BonusValue=new ContextValue{ValueType=ContextValueType.Rank}};tick.DisableSneakDamage=tick.IgnoreCritical=true;bleed.ComponentsArray=bleed.ComponentsArray.Concat(new[]{Rank(cls,"Custom",4,1,12,2,20,3)}).ToArray();
            var resource=Daily(f,"DarkHalfResource01458",3,StatType.Charisma,cls,2);
            var state=f.Buff("DarkHalfBuff01458",N("DarkHalfBuff"),D("DarkHalfBuff"),new DarkHalf01458{Resource=Ref<BlueprintAbilityResourceReference>(resource),Morphic=Ref<BlueprintFeatureReference>(morph),Safeguard=Ref<BlueprintFeatureReference>(safeguard),Forms=forms.Select(Ref<BlueprintBuffReference>).ToArray(),Protection=Ref<BlueprintBuffReference>(stronger)},new AddStatBonus{Stat=StatType.SaveWill,Value=2,Descriptor=ModifierDescriptor.Morale},new SpellImmunityToSpellDescriptor{Descriptor=SpellDescriptor.Fear|SpellDescriptor.Shaken},new BuffDescriptorImmunity{Descriptor=SpellDescriptor.Fear|SpellDescriptor.Shaken},new PsychicDamageLink01458{Kind=PsychicDamageKind01458.DarkHalf,Effect=Ref<BlueprintBuffReference>(bleed)});Icon(state,f.Native<BlueprintUnitFact>("1d48ab2bded57a74dad8af3da07d313a"));state.FxOnStart=new PrefabLink{AssetId="53c86872d2be80b48afc218af1b204d7"};
            foreach(var skill in new[]{StatType.SkillThievery,StatType.SkillUseMagicDevice,StatType.CheckDiplomacy,StatType.CheckBluff,StatType.SkillKnowledgeArcana,StatType.SkillKnowledgeWorld})state.ComponentsArray=state.ComponentsArray.Concat(new[]{new AddStatBonus{Stat=skill,Value=-20,Descriptor=ModifierDescriptor.UntypedStackable}}).ToArray();
            var enter=f.Ability("DarkHalfAbility01458",state.Name,state.Description,AbilityType.Supernatural,UnitCommand.CommandType.Swift,AbilityRange.Personal,Cost(resource),Run(Apply(state,0,fromSpell:false)),new DarkHalfCaster01458{Active=Ref<BlueprintBuffReference>(state)});var entry=enter.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single();entry.Permanent=true;entry.AsChild=false;entry.IsNotDispelable=true;Icon(enter,state);
            var leave=f.Ability("DarkHalfReturnAbility01458","Return to Normal",state.Description,AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Personal,Run(new DarkHalfReturn01458()),new DarkHalfCaster01458{Active=Ref<BlueprintBuffReference>(state),Returning=true});Icon(leave,f.Native<BlueprintAbility>("d316d3d94d20c674db2c24d7de96f6a7"));
            var first=f.Feature("DarkHalfFeature01458",state.Name,state.Description,Facts(enter,leave),Grant(resource));Icon(first,state);
            return Discipline(f,cls,"Abomination",first,morph,safeguard,"450af0402422b0b4980d9c2175869612","14ec7a4e52e90fa47a4c8d63c69fd5c1","8eead52509987034ea9025d60cc05985","cf6c901fb7acc904e85c63b342e9c949",Id("BurstOfForce").ToString(),Id("MentalBarrier5").ToString(),"2b044152b3620c841badb090e01ed9de",Id("OrbOfTheVoid").ToString(),Id("TelekineticStorm").ToString());
        }
    }
}
