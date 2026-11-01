// Adapted from Holic75/KingmakerRebalance Psychic.createMutationMind (MIT).
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class MutationMind01457
    {
        internal const string ModuleName="Psychic archetype: Mutation Mind",RootToken="MutationMindArchetype01457";
        internal static readonly string[] Mutations={"Adhesive","BiteAttack","Claws","ElongatedLegs","ElongatedTorso","Rubbery","Slimy","EnlargedBody","MultipleEyes","Recuperation","Wings","ElongatedFingers"};
        internal static readonly string[] OwnTokens=new[]{RootToken,"PhysicalMutationResource01457","PhysicalMutationBuff01457","PhysicalMutationAbility01457","PhysicalMutationFeature01457","BodilyMutationsFeature01457","ImprovedBodilyMutationsFeature01457"}.Concat(Mutations.SelectMany(n=>new[]{n+"BodilyMutationFeature01457",n+"BodilyMutationBuff01457"})).Concat(new[]{"PhysicalMutationControllerBuff01458","PhysicalMutationToggle01458"}).ToArray();
        static string N(string key)=>key=="ElongatedFingers"?"Elongated Fingers":PsychicOriginalTexts01457.Name(key);
        static string D(string key)=>key=="ElongatedFingers"?"The mutation mind’s fingers grow long and supple, increasing her reach by 5 feet for all melee attacks while Physical Mutation is active.":PsychicOriginalTexts01457.Description(key);
        sealed class SelectionExtension { internal BlueprintsCache Cache;internal BlueprintScriptableObject[] Assets;internal string[] Definitions;internal BlueprintFeature[] Choices; }
        static readonly ConditionalWeakTable<BlueprintFeatureSelection,SelectionExtension> Extensions=new ConditionalWeakTable<BlueprintFeatureSelection,SelectionExtension>();
        // PhrenicAmplifications owns and strictly fingerprints its original
        // pool. Normalize only this module's exact declared extension while
        // verifying every private reference/definition and both list counts.
        // Unknown additions, duplicates and changed original rows still fail.
        internal static BlueprintScriptableObject AmplificationRegistrationShape(BlueprintScriptableObject bp)
        {
            if(!(bp is BlueprintFeatureSelection selection)||!Extensions.TryGetValue(selection,out var r))return bp;
            for(int i=0;i<r.Assets.Length;i++)if(!ReferenceEquals(r.Cache.Load(r.Assets[i].AssetGuid),r.Assets[i])||Definition(r.Assets[i])!=r.Definitions[i])throw new InvalidOperationException("Mutation Mind extension resource changed: "+r.Assets[i].name);
            var clone=(BlueprintFeatureSelection)AccessTools.Method(typeof(object),"MemberwiseClone").Invoke(selection,null);
            foreach(string field in new[]{"m_Features","m_AllFeatures"}){
                var refs=(BlueprintFeatureReference[])Get(selection,field)??Array.Empty<BlueprintFeatureReference>();
                foreach(var feat in r.Choices)if(refs.Count(v=>v?.Guid==feat.AssetGuid)!=1||!ReferenceEquals(refs.Single(v=>v?.Guid==feat.AssetGuid).Get(),feat))throw new InvalidOperationException("Mutation Mind amplification extension link changed.");
                var ids=r.Choices.Select(v=>v.AssetGuid).ToArray();Set(clone,field,refs.Where(v=>v==null||!ids.Contains(v.Guid)).ToArray());
            }return clone;
        }
        static void Icon(BlueprintUnitFact dst,BlueprintUnitFact src)=>Set(dst,"m_Icon",Get(src,"m_Icon"));
        static PrerequisiteFeature Need(BlueprintFeature feat){var x=new PrerequisiteFeature();Set(x,"m_Feature",Ref<BlueprintFeatureReference>(feat));return x;}
        static AddStatBonus Stat(StatType stat,int n,ModifierDescriptor kind)=>new AddStatBonus{Stat=stat,Value=n,Descriptor=kind};
        static BlueprintComponent Weapon<T>(PsychicAssets f,string id)where T:BlueprintComponent,new(){var c=new T();Set(c,"m_Weapon",Ref<BlueprintItemWeaponReference>(f.Native<BlueprintItemWeapon>(id)));return c;}
        internal static void Register(BlueprintsCache cache)
        {
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(PsychicClass.ClassId);
            if(Existing(cache,RootToken,cls))return;Vacant(cache,OwnTokens);
            var amplification=(BlueprintFeatureSelection)cache.Load(Id("PhrenicAmplificationSelection"));
            var extra=(BlueprintFeatureSelection)cache.Load(Id("ExtraPhrenicAmplificationFeat"));
            if(amplification==null||!cls.Progression.LevelEntries.Any(e=>e.Level==1&&e.Features.Contains(amplification)))throw new InvalidOperationException("Mutation Mind requires the complete existing Psychic amplification selection.");
            if(extra==null)throw new InvalidOperationException("Mutation Mind requires the existing Extra Phrenic Amplification feat.");
            var pools=new[]{amplification,extra};
            var icon=f.Native<BlueprintUnitFact>("489c8c4a53a111d4094d239054b26e32");var bull=f.Native<BlueprintBuff>("b175001b42b1a02479881b72fe132116");
            var fatigue=f.Native<BlueprintBuff>("e6f2fc5d73d88064583cb828801212f4");var exhausted=f.Native<BlueprintBuff>("46d1b9cc3d0fd36469a471b047d773a2");
            var resource=f.Add(new BlueprintAbilityResource(),"PhysicalMutationResource01457","","");var amount=Get(resource,"m_MaxAmount");
            Set(amount,"BaseValue",0);Set(amount,"IncreasedByLevel",true);Set(amount,"LevelIncrease",1);Set(amount,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(cls)});Set(amount,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());Set(amount,"m_ClassDiv",Array.Empty<BlueprintCharacterClassReference>());Set(amount,"m_ArchetypesDiv",Array.Empty<BlueprintArchetypeReference>());Set(resource,"m_MaxAmount",amount);
            var strength=new AddContextStatBonus{Stat=StatType.Strength,Descriptor=ModifierDescriptor.Alchemical,Value=new ContextValue{ValueType=ContextValueType.Rank}};
            var main=f.Buff("PhysicalMutationBuff01457",N("PhysicalMutationBuff"),D("PhysicalMutationBuff"),Stat(StatType.Intelligence,-2,ModifierDescriptor.UntypedStackable),strength,Rank(cls,"Custom",11,4,15,6,20,8));
            main.ComponentsArray=main.ComponentsArray.Concat(new[]{new PhysicalMutationDuration01458()}).ToArray();
            // Kingmaker CotW explicitly uses this Bull's Strength prefab. The
            // Wrath Bull's Strength BUFF has empty FX fields; copying that buff
            // silently loses the original activation visual. Buff owns/clears
            // the start handle on deactivation/disposal; CotW has no end burst.
            Icon(main,icon);main.FxOnStart=new Kingmaker.ResourceLinks.PrefabLink{AssetId="3b42d149d55dfe5469d9967f22d7e020"};main.FxOnRemove=new Kingmaker.ResourceLinks.PrefabLink();
            var noFacts=new AbilityCasterHasNoFacts();Set(noFacts,"m_Facts",new[]{Ref<BlueprintUnitFactReference>(fatigue),Ref<BlueprintUnitFactReference>(exhausted)});
            var apply=Apply(main,1,DurationRate.Minutes,fromSpell:false);apply.IsNotDispelable=true;apply.AsChild=false;
            var ability=f.Ability("PhysicalMutationAbility01457",N("PhysicalMutationBuff"),D("PhysicalMutationBuff"),AbilityType.Supernatural,UnitCommand.CommandType.Swift,AbilityRange.Personal,Run(new StartPhysicalMutation01458()),noFacts,new PhysicalMutationCaster01457());ability.Hidden=true;
            Icon(ability,icon);ability.LocalizedDuration=Text("PhysicalMutationAbility01457","duration","1 minute");
            const string maintenance=" Keep this toggle enabled to maintain Physical Mutation, spending one daily minute at activation and another whenever the paid minute expires. Turning it off stops automatic spending without removing the remaining paid duration. Turning it back on during that duration does not spend or reset the minute. Fatigue begins only when the physical mutation actually ends.";
            var controller=f.Buff("PhysicalMutationControllerBuff01458",ability.Name,D("PhysicalMutationBuff")+maintenance,new PhysicalMutationController01458());SetEnum(controller,"m_Flags","HiddenInUi");Icon(controller,icon);
            var toggle=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.WithUnitCommand,DeactivateImmediately=true,DeactivateIfOwnerDisabled=true,DeactivateIfOwnerUnconscious=true,ComponentsArray=new BlueprintComponent[]{new PhysicalMutationRestriction01458()}},"PhysicalMutationToggle01458",ability.Name,D("PhysicalMutationBuff")+maintenance);Set(toggle,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Swift);Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(controller));Icon(toggle,icon);
            var physical=f.Feature("PhysicalMutationFeature01457",ability.Name,D("PhysicalMutationBuff")+maintenance,Facts(toggle),Grant(resource));Icon(physical,icon);
            var basic=f.Feature("BodilyMutationsFeature01457",N("BodilyMutationsFeature"),D("BodilyMutationsFeature"));Icon(basic,f.Native<BlueprintUnitFact>("c60969e7f264e6d4b84a1499fdcf9039"));
            var improved=f.Feature("ImprovedBodilyMutationsFeature01457",N("ImprovedBodilyMutationsFeature"),D("ImprovedBodilyMutationsFeature"));Icon(improved,f.Native<BlueprintUnitFact>("93d9d74dac46b9b458d4d2ea7f4b1911"));
            var feats=new List<BlueprintFeature>();var on=new List<GameAction>();
            foreach(string key in Mutations){
                var body=new List<BlueprintComponent>();string iconId="c60969e7f264e6d4b84a1499fdcf9039";
                switch(key){
                    case "Adhesive":body.Add(new ManeuverBonus{Type=Kingmaker.RuleSystem.Rules.CombatManeuver.Disarm,Bonus=4});body.Add(new ManeuverBonus{Type=Kingmaker.RuleSystem.Rules.CombatManeuver.Grapple,Bonus=4});iconId="6b30813c3709fc44b92dc8fd8191f345";break;
                    case "BiteAttack":body.Add(Weapon<AddAdditionalLimb>(f,"35dfad6517f401145af54111be04d6cf"));iconId="25954b1652bebc2409f9cb9d5728bceb";break;
                    case "Claws":body.Add(Weapon<EmptyHandWeaponOverride>(f,"118fdd03e569a66459ab01a20af6811a"));iconId="e17698ef77dc3174cbd0a42422c22441";break;
                    case "ElongatedLegs":body.Add(Stat(StatType.Speed,10,ModifierDescriptor.UntypedStackable));iconId="486eaff58293f6441a5c2759c4872f98";break;
                    case "ElongatedFingers":break;
                    case "ElongatedTorso":body.Add(Stat(StatType.AC,2,ModifierDescriptor.NaturalArmor));break;
                    case "Rubbery":body.Add(new AddDamageResistancePhysical{Value=2});iconId="0a5ddfbcfb3989543ac7c936fc256889";break;
                    case "Slimy":body.Add(Stat(StatType.AdditionalCMD,4,ModifierDescriptor.UntypedStackable));break;
                    case "EnlargedBody":var size=new ChangeUnitSize{SizeDelta=2};SetEnum(size,"m_Type","Delta");body.Add(size);body.Add(Stat(StatType.Strength,4,ModifierDescriptor.Size));body.Add(Stat(StatType.Dexterity,-2,ModifierDescriptor.Size));iconId="3dccdf27a8209af478ac71cded18a271";break;
                    case "MultipleEyes":var flank=new AddMechanicsFeature();SetEnum(flank,"m_Feature","CannotBeFlanked");body.Add(flank);body.Add(Stat(StatType.SkillPerception,4,ModifierDescriptor.UntypedStackable));iconId="8d20e33f40680ee1f860b2a23ed13c63";break;
                    case "Recuperation":body.Add(new AddEffectFastHealing{Heal=5});iconId="aad496a7c9bee224cbd01a3cf8e42061";break;
                    case "Wings":body.AddRange(f.Native<BlueprintBuff>("3c958be25ab34dc448569331488bee27").ComponentsArray.Select(v=>(BlueprintComponent)CopyData(v)));iconId="78197196e096c6e4eaed5c62fa108b52";break;
                }
                var effect=f.Buff(key+"BodilyMutationBuff01457",N(key),D(key),body.ToArray());
                var prereq=new List<BlueprintComponent>{Need(basic)};if(Array.IndexOf(Mutations,key)>=7&&key!="ElongatedFingers")prereq.Add(Need(improved));
                var feat=f.Feature(key+"BodilyMutationFeature01457",N(key),D(key),prereq.ToArray());feat.IsClassFeature=true;
                if(key=="Slimy"||key=="ElongatedFingers"){var sprite=PsychicOriginalIcons.Load(key);Set(effect,"m_Icon",sprite);Set(feat,"m_Icon",sprite);}else{var donor=f.Native<BlueprintUnitFact>(iconId);Icon(effect,donor);Icon(feat,donor);}
                if(key=="Wings"){var native=f.Native<BlueprintBuff>("3c958be25ab34dc448569331488bee27");if(native.FxOnStart!=null)effect.FxOnStart=CopyData(native.FxOnStart);if(native.FxOnRemove!=null)effect.FxOnRemove=CopyData(native.FxOnRemove);}
                var has=new ContextConditionHasFact();Set(has,"m_Fact",Ref<BlueprintUnitFactReference>(feat));var child=Apply(effect,0,fromSpell:false);child.Permanent=true;child.AsChild=true;child.IsNotDispelable=true;
                on.Add(new Conditional{ConditionsChecker=new ConditionsChecker{Conditions=new Condition[]{has}},IfTrue=Actions(child),IfFalse=Actions()});feats.Add(feat);
            }
            var after=Apply(fatigue,1,fromSpell:false);after.IsNotDispelable=true;after.AsChild=false;
            main.ComponentsArray=main.ComponentsArray.Concat(new[]{new AddFactContextActions{Activated=Actions(on.ToArray()),Deactivated=Actions(after),NewRound=Actions(),Dispose=Actions()}}).ToArray();
            var root=f.Add(new BlueprintArchetype{LocalizedName=Text(RootToken,"name",N("MutationMindArchetype")),LocalizedDescription=Text(RootToken,"description",D("MutationMindArchetype")),AddFeatures=new[]{Entry(1,physical),Entry(3,basic),Entry(11,improved)},RemoveFeatures=new[]{Entry(1,amplification)},ComponentsArray=Array.Empty<BlueprintComponent>()},RootToken,N("MutationMindArchetype"),D("MutationMindArchetype"));Set(root,"m_ParentClass",cls);
            Func<Action> snapshot=()=>{var old=pools.Select(s=>new[]{Get(s,"m_Features"),Get(s,"m_AllFeatures")}).ToArray();return ()=>{for(int i=0;i<pools.Length;i++){Set(pools[i],"m_Features",old[i][0]);Set(pools[i],"m_AllFeatures",old[i][1]);}};};
            Action connect=()=>{foreach(var selection in pools)foreach(string field in new[]{"m_Features","m_AllFeatures"}){var old=(BlueprintFeatureReference[])Get(selection,field)??Array.Empty<BlueprintFeatureReference>();if(old.Any(r=>r==null))throw new InvalidOperationException("Existing Psychic amplification pool contains a null reference.");foreach(var feat in feats){var links=old.Where(r=>r.Guid==feat.AssetGuid).ToArray();if(links.Length>1||links.Any(r=>!ReferenceEquals(r.Get(),feat)))throw new InvalidOperationException("Mutation Mind amplification extension link changed.");}var added=feats.Where(feat=>!old.Any(r=>r.Guid==feat.AssetGuid)).Select(Ref<BlueprintFeatureReference>);Set(selection,field,old.Concat(added).ToArray());}};
            Publish(cache,f,OwnTokens,cls,root,new BlueprintFeatureBase[]{physical,basic,improved},snapshot,connect);
            var assets=OwnTokens.Select(t=>f.Assets[t]).ToArray();foreach(var selection in pools)Extensions.Add(selection,new SelectionExtension{Cache=cache,Assets=assets,Definitions=assets.Select(Definition).ToArray(),Choices=feats.ToArray()});
        }
    }
}
