// Adapted from Holic75/KingmakerRebalance (MIT), Archetypes/DrillSergeant.cs.
using System;
using System.Linq;
using System.Collections.Generic;
using Kingmaker.EntitySystem;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class DrillSergeant01445
    {
        internal const string ModuleName="Fighter archetype: Drill Sergeant",RootToken="DrillSergeantArchetype01445",ClassId="48ac8db94d5de7645906c7d0ad3bcfbd",PoolId="d87e2f6a9278ac04caeb0f93eff95fcb";
        const string TacticianText="At 2nd level, a drill sergeant receives a teamwork feat as a bonus feat. He must meet the prerequisites for this feat. As a standard action, the drill sergeant can grant any teamwork feat he has to all allies within 30 feet who can see and hear him. Allies retain the use of this bonus feat for 3 rounds plus 1 round for every two levels the drill sergeant possesses. Allies do not need to meet the prerequisites of these bonus feats. The drill sergeant can use this ability once per day at 1st level, plus one additional time per day at 5th level and for every 5 levels thereafter.";
        internal static string T(string s)=>"DrillSergeant"+s+"01445";
        internal static BlueprintFeature[] Choices(BlueprintsCache c)=>(c.Load(BlueprintGuid.Parse(PoolId))as BlueprintFeatureSelection)?.AllFeatures.Where(x=>x!=null).Distinct().OrderBy(x=>x.AssetGuid.ToString()).ToArray()??throw new InvalidOperationException("Drill Sergeant teamwork pool missing.");
        internal static string[] Tokens(BlueprintsCache c)=>new[]{RootToken,T("Tactician"),T("GreaterTactician"),T("MasterTactician"),T("WeaponTrainer"),T("Resource"),T("Ability"),T("SwiftAbility"),T("TrainerBuff"),T("TrainerHost"),T("TrainerArea")}.Concat(Choices(c).SelectMany(x=>new[]{T(x.AssetGuid+"Toggle"),T(x.AssetGuid+"Marker"),T(x.AssetGuid+"Shared")})).ToArray();
        internal static void Register(BlueprintsCache cache)
        {
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(ClassId);if(Existing(cache,RootToken,cls)){Reconcile(cache);return;}var tokens=Tokens(cache);Vacant(cache,tokens);var choices=Choices(cache);if(choices.Length==0)throw new InvalidOperationException("Empty Drill Sergeant teamwork pool.");
            var donor=f.Native<BlueprintAbility>("3ff8ef7ba7b5be0429cf32cd4ddf637c");var icon=donor.Icon;
            const string text=TacticianText;
            BlueprintFeatureSelection Selection(string s,string name,string desc){var v=f.Add(new BlueprintFeatureSelection{Ranks=1,IsClassFeature=true,IgnorePrerequisites=false},T(s),name,desc);Set(v,"m_AllFeatures",choices.Select(Ref<BlueprintFeatureReference>).ToArray());Set(v,"m_Features",choices.Select(Ref<BlueprintFeatureReference>).ToArray());Set(v,"m_Icon",icon);return v;}
            var tactician=Selection("Tactician","Tactician",text);var greater=Selection("GreaterTactician","Greater Tactician","At 9th level, the drill sergeant receives an additional teamwork feat as a bonus feat. He must meet the prerequisites for this feat. Using the tactician ability is a swift action.");var master=Selection("MasterTactician","Master Tactician","At 17th level, the drill sergeant receives an additional teamwork feat as a bonus feat. He must meet the prerequisites for this feat. The drill sergeant can grant this feat to his allies using the tactician ability. Whenever the drill sergeant uses the tactician ability, he grants any two teamwork feats that he knows.");
            var resource=Resource(f,T("Resource"),cls,1,5);var toggles=choices.Select(feat=>CreateOption(f,feat,master,icon,text)).ToArray();
            BlueprintAbility Ability(string s,UnitCommand.CommandType command){var a=f.Clone(donor,T(s),"Tactician",text);a.ActionType=command;a.ComponentsArray=a.ComponentsArray.Where(c=>c is AbilityTargetsAround).Concat(new BlueprintComponent[]{Rank(cls,"Div2"),Cost(resource),Run(new DrillSergeantShare01445{Class=Ref<BlueprintCharacterClassReference>(cls)})}).ToArray();return a;}
            var standard=Ability("Ability",UnitCommand.CommandType.Standard);var swift=Ability("SwiftAbility",UnitCommand.CommandType.Swift);
            var show=new AbilityShowIfCasterHasFact{Not=true};Set(show,"m_UnitFact",Ref<BlueprintUnitFactReference>(greater));standard.ComponentsArray=standard.ComponentsArray.Concat(new BlueprintComponent[]{show}).ToArray();
            greater.ComponentsArray=new BlueprintComponent[]{Facts(swift)};
            tactician.ComponentsArray=new BlueprintComponent[]{Grant(resource),Facts(standard),new DrillSergeantLateOptions01445{InitialChoices=choices.Select(Ref<BlueprintFeatureReference>).ToArray()}}.Concat(choices.Select((feat,i)=>{var c=new AddFeatureIfHasFact();Set(c,"m_CheckedFact",Ref<BlueprintUnitFactReference>(feat));Set(c,"m_Feature",Ref<BlueprintUnitFactReference>(toggles[i]));return c;})).ToArray();
            const string trainerText="At 13th level, when a drill sergeant wields a weapon he has weapon training in, all allies within 30 feet who can see and hear the drill sergeant gain half his weapon training bonus when they wield any weapon from the same weapon group.\nThis bonus doesn’t stack with any weapon training bonus an ally already possesses.";
            var effect=f.Buff(T("TrainerBuff"),"Weapon Trainer",trainerText,new DrillSergeantWeaponTrainer01445());Set(effect,"m_Icon",f.Native<BlueprintAbility>("9d5d2d3ffdd73c648af3eb3e585b1113").Icon);
            var applied=new AbilityAreaEffectBuff{CheckConditionEveryRound=true,Condition=new ConditionsChecker{Conditions=new Condition[]{new ContextConditionIsAlly(),new ContextConditionIsCaster{Not=true}}}};Set(applied,"m_Buff",Ref<BlueprintBuffReference>(effect));var area=f.Add(new BlueprintAbilityAreaEffect{Size=new Feet(30),Shape=AreaEffectShape.Cylinder,AffectEnemies=false,ComponentsArray=new BlueprintComponent[]{applied}},T("TrainerArea"),"Weapon Trainer",trainerText);SetEnum(area,"m_TargetType","Any");var add=new AddAreaEffect();Set(add,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));var host=f.Buff(T("TrainerHost"),"Weapon Trainer",trainerText,add);Set(host,"m_Icon",effect.Icon);SetEnum(host,"m_Flags","HiddenInUi");var trainer=f.Feature(T("WeaponTrainer"),"Weapon Trainer",trainerText);
            // Native permanent fact application retains the caster's context for the aura.
            trainer.ComponentsArray=new BlueprintComponent[]{Facts(host)};Set(trainer,"m_Icon",effect.Icon);
            var bravery=f.Native<BlueprintFeature>("f6388946f9f472f4585591b80e9f2452");var training=f.Native<BlueprintFeature>("b8cecf4e5e464ad41b79d5b42b76b399");
            var root=f.Add(new BlueprintArchetype{LocalizedName=Text(RootToken,"name","Drill Sergeant"),LocalizedDescription=Text(RootToken,"description","Drill sergeants excel at training other combatants in fighting techniques."),RemoveFeatures=new[]{Entry(2,bravery),Entry(6,bravery),Entry(9,training),Entry(10,bravery),Entry(13,training),Entry(14,bravery),Entry(17,training),Entry(18,bravery)},AddFeatures=new[]{Entry(2,tactician),Entry(9,greater),Entry(13,trainer),Entry(17,master)}},RootToken,"Drill Sergeant","");Set(root,"m_ParentClass",cls);Publish(cache,f,tokens,cls,root,new BlueprintFeatureBase[]{tactician,greater,trainer,master});
        }
        static BlueprintActivatableAbility CreateOption(PsychicAssets f,BlueprintFeature feat,BlueprintFeature master,UnityEngine.Sprite icon,string text)
        {
                var grant=new AddFeatureIfHasFact{Not=true};Set(grant,"m_CheckedFact",Ref<Kingmaker.Blueprints.BlueprintUnitFactReference>(feat));Set(grant,"m_Feature",Ref<BlueprintUnitFactReference>(feat));var shared=f.Buff(T(feat.AssetGuid+"Shared"),feat.name,text,grant);Set(shared,"m_Icon",feat.Icon??icon);Set(shared,"m_DisplayName",Get(feat,"m_DisplayName"));
                var marker=f.Buff(T(feat.AssetGuid+"Marker"),feat.name,text,new DrillSergeantSelection01445{Feat=Ref<BlueprintFeatureReference>(feat),Shared=Ref<BlueprintBuffReference>(shared),Master=Ref<BlueprintFeatureReference>(master)});Set(marker,"m_Icon",feat.Icon??icon);Set(marker,"m_DisplayName",Get(feat,"m_DisplayName"));
                var toggle=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.Immediately,DoNotTurnOffOnRest=true,DeactivateIfCombatEnded=false,ComponentsArray=new BlueprintComponent[]{new DrillSergeantKnownFeat01445{Feat=Ref<BlueprintFeatureReference>(feat)}}},T(feat.AssetGuid+"Toggle"),feat.name,text);Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(marker));Set(toggle,"m_Icon",feat.Icon??icon);Set(toggle,"m_DisplayName",Get(feat,"m_DisplayName"));return toggle;

        }
        static void Reconcile(BlueprintsCache cache)
        {
            var selections=new[]{"Tactician","GreaterTactician","MasterTactician"}.Select(s=>(BlueprintFeatureSelection)cache.Load(Id(T(s)))).ToArray();
            var known=selections[0].AllFeatures.Select(f=>f.AssetGuid).ToHashSet();
            var fresh=Choices(cache).Where(f=>!known.Contains(f.AssetGuid)).ToArray();
            if(fresh.Length>0) {
                var f=new PsychicAssets(cache);var icon=selections[0].Icon;
                foreach(var feat in fresh)CreateOption(f,feat,selections[2],icon,TacticianText);
                var before=selections.Select(v=>new[]{Get(v,"m_AllFeatures"),Get(v,"m_Features")}).ToArray();
                Action undo=()=>{for(int i=0;i<selections.Length;i++){Set(selections[i],"m_AllFeatures",before[i][0]);Set(selections[i],"m_Features",before[i][1]);}};
                Extend(cache,RootToken,f,()=>{
                    foreach(var selection in selections){
                        foreach(string field in new[]{"m_AllFeatures","m_Features"}){
                            var old=(BlueprintFeatureReference[])Get(selection,field);
                            Set(selection,field,old.Concat(fresh.Where(bp=>!old.Any(r=>r.Guid==bp.AssetGuid)).Select(Ref<BlueprintFeatureReference>)).ToArray());
                        }
                        foreach(var feat in fresh)if(!selection.AllFeatures.Contains(feat))throw new InvalidOperationException("Drill Sergeant late choice not connected.");
                    }
                },undo);
            }
            DrillSergeantLateOptions01445.RefreshPublished(selections[0]);
        }

    }
    [TypeId("780f5f5e27a24c14a537a4011e5adc38")]
    public sealed class DrillSergeantKnownFeat01445:ActivatableAbilityRestriction
    {public BlueprintFeatureReference Feat;protected override bool IsAvailable()=>Owner.HasFact(Feat.Get());}
    [TypeId("9a9f9f0b45974785bf9e19cd20a63a46")]
    public sealed class DrillSergeantSelection01445:UnitBuffComponentDelegate
    {
        public BlueprintFeatureReference Feat,Master;public BlueprintBuffReference Shared;
        protected override void OnTurnOn(){int limit=Owner.HasFact(Master.Get())?2:1;var active=Owner.Buffs.Enumerable.Where(b=>b!=Buff&&b.IsTurnedOn&&b.Blueprint.GetComponent<DrillSergeantSelection01445>()!=null).ToArray();foreach(var b in active.Take(Math.Max(0,active.Length+1-limit))){foreach(var a in Owner.ActivatableAbilities.Enumerable.Where(a=>a.Blueprint.Buff==b.Blueprint).ToArray()){a.IsOn=false;a.Stop();}if(!b.IsDisposed)b.Remove();}}
    }
    [TypeId("1288719f7aad4677b561437631cff7f4")]
    public sealed class DrillSergeantShare01445:ContextAction
    {
        public BlueprintCharacterClassReference Class;
        public override string GetCaption()=>"Share selected teamwork feats";
        public override void RunAction(){var caster=Context.MaybeCaster;var target=Target?.Unit;if(caster==null||target==null||caster==target||!caster.IsAlly(target))return;int limit=caster.Facts.List.Any(x=>x.Blueprint.AssetGuid==PsychicAssets.Id(DrillSergeant01445.T("MasterTactician")))?2:1;var marked=caster.Buffs.Enumerable.Where(b=>b.IsTurnedOn&&!b.IsSuppressed).Select(b=>b.Blueprint.GetComponent<DrillSergeantSelection01445>()).Where(c=>c!=null&&caster.HasFact(c.Feat.Get())).Take(limit);foreach(var m in marked)target.AddBuff(m.Shared.Get(),Context,TimeSpan.FromSeconds((3+caster.Progression.GetClassLevel(Class.Get())/2)*6));}
    }
    [TypeId("c808f0fd7c8f41c69fb4c586a9581ecb")]
    public sealed class DrillSergeantWeaponTrainer01445:UnitBuffComponentDelegate,IInitiatorRulebookHandler<RuleCalculateWeaponStats>,IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,IInitiatorRulebookSubscriber
    {
        internal int Bonus(ItemEntityWeapon weapon){var caster=Context.MaybeCaster;var held=caster?.Body.PrimaryHand.MaybeWeapon;var part=caster?.Get<UnitPartWeaponTraining>();if(weapon==null||held==null||part==null||part.GetWeaponRank(held)<=0||(held.Blueprint.FighterGroup & weapon.Blueprint.FighterGroup & ~Kingmaker.Blueprints.Items.Weapons.WeaponFighterGroupFlags.None)==0)return 0;return Math.Max(0,part.GetWeaponRank(weapon)/2-(Owner.Get<UnitPartWeaponTraining>()?.GetWeaponRank(weapon)??0));}
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats e){int n=Bonus(e.Weapon);if(n>0)e.AddTemporaryModifier(Owner.Stats.AdditionalDamage.AddModifier(n,Fact,ModifierDescriptor.UntypedStackable));}public void OnEventDidTrigger(RuleCalculateWeaponStats e){}
        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget e){int n=Bonus(e.Weapon);if(n>0)e.AddModifier(n,Fact,ModifierDescriptor.UntypedStackable);}public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget e){}
    }
    public sealed class DrillSergeantLateOptionsData01445
    {
        [JsonProperty] public List<EntityFact> Applied=new List<EntityFact>();
        [JsonIgnore] public bool Updating;
    }
    [TypeId("a49300f176ad4fcd97a29ef287a360bd")]
    public sealed class DrillSergeantLateOptions01445:UnitFactComponentDelegate<DrillSergeantLateOptionsData01445>,IUnitGainFactHandler
    {
        public BlueprintFeatureReference[] InitialChoices;
        static readonly List<WeakReference<EntityFactComponent>> Live=new List<WeakReference<EntityFactComponent>>();
        internal static void RefreshPublished(BlueprintFeatureSelection selection)
        {
            foreach(var weak in Live.ToArray()){
                if(!weak.TryGetTarget(out var runtime)||runtime.Fact.IsDisposed||!runtime.Fact.IsActive){Live.Remove(weak);continue;}
                if(runtime.Fact.Blueprint!=selection||!runtime.Fact.IsTurnedOn)continue;
                using(runtime.RequestEventContext())((DrillSergeantLateOptions01445)runtime.SourceBlueprintComponent).Refresh();
            }
        }
        void Refresh()
        {
            if(Data.Updating)return;
            Data.Updating=true;
            try {
                foreach(var feat in ((BlueprintFeatureSelection)Fact.Blueprint).AllFeatures.Where(f=>!InitialChoices.Any(r=>r.Guid==f.AssetGuid)&&Owner.HasFact(f))){
                    var toggle=ResourcesLibrary.BlueprintsCache.Load(Id(DrillSergeant01445.T(feat.AssetGuid+"Toggle")))as BlueprintActivatableAbility;
                    if(toggle!=null&&!Owner.HasFact(toggle))Data.Applied.Add(Owner.AddFact(toggle));
                }
            }finally{Data.Updating=false;}
        }
        protected override void OnActivate(){if(!Live.Any(w=>w.TryGetTarget(out var r)&&r==Runtime))Live.Add(new WeakReference<EntityFactComponent>(Runtime));Refresh();}
        public void HandleUnitGainFact(EntityFact fact){if(fact.Owner==Owner)Refresh();}
        protected override void OnApplyPostLoadFixes(){if(Fact.IsActive&&!Fact.IsDisposed)OnActivate();}
        protected override void OnDeactivate(){foreach(var fact in Data.Applied.ToArray())if(fact!=null&&!fact.IsDisposed)Owner.RemoveFact(fact);Data.Applied.Clear();}
    }

}
