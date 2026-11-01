// Adapted from Holic75/KingmakerRebalance (MIT), Archetypes/CourtBard.cs.
using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
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
    internal static class CourtBard01445
    {
        internal const string ModuleName="Bard archetype: Court Bard",RootToken="CourtBardArchetype01445",ClassId="772c83a25e2268e448e841dcd548235f";
        internal static string T(string s)=>"CourtBard"+s+"01445";
        internal static readonly string[] OwnTokens=new[]{RootToken,T("HeraldicExpertise"),T("HeraldicResource"),T("HeraldicBuff"),T("HeraldicToggle")}.Concat(new[]{"Satire","Mockery","GloriousEpic","Scandal"}.SelectMany(s=>new[]{"Feature","Effect","Host","Area","Toggle"}.Select(k=>T(s+k)))).Concat(new[]{T("GloriousEpicImmunity"),T("ScandalImmunity")}).ToArray();
        static readonly SpellDescriptor Descriptors=SpellDescriptor.MindAffecting|SpellDescriptor.Sonic;
        static BlueprintComponent[] Tags()=>new BlueprintComponent[]{new SpellDescriptorComponent{Descriptor=Descriptors}};
        static BlueprintFeature Performance(PsychicAssets f,BlueprintCharacterClass cls,string token,string name,string text,string iconId,int radius,BlueprintBuff effect,bool save=false)
        {
            var icon=f.Native<Kingmaker.Blueprints.Facts.BlueprintUnitFact>(iconId).Icon;Set(effect,"m_Icon",icon);
            var calc=new ContextCalculateAbilityParamsBasedOnClass{StatType=StatType.Charisma};Set(calc,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));
            BlueprintComponent application;
            if(!save){var b=new AbilityAreaEffectBuff{CheckConditionEveryRound=true,Condition=new ConditionsChecker{Conditions=new Condition[]{new ContextConditionIsEnemy()}}};Set(b,"m_Buff",Ref<BlueprintBuffReference>(effect));application=b;}
            else{
                var immune=f.Buff(T(token+"Immunity"),name+": Cooldown",text);Set(immune,"m_Icon",icon);SetEnum(immune,"m_Flags","RemoveOnRest");
                var apply=Apply(effect,0,fromSpell:false);apply.Permanent=true;apply.AsChild=true;
                var immunity=Apply(immune,1,DurationRate.Days,fromSpell:false);immunity.IsNotDispelable=true;
                var has=new ContextConditionHasBuff();Set(has,"m_Buff",Ref<BlueprintBuffReference>(immune));
                var onEnter=new Conditional{ConditionsChecker=new ConditionsChecker{Conditions=new Condition[]{new ContextConditionIsEnemy(),new CourtBardImmunity01445{Buff=Ref<BlueprintBuffReference>(immune),FromCaster=false,Not=true}}},IfTrue=Actions(new ContextActionSavingThrow{Type=SavingThrowType.Will,Actions=Actions(Saved(new GameAction[]{immunity},new GameAction[]{apply}))}),IfFalse=Actions()};
                var remove=new ContextActionRemoveBuff{OnlyFromCaster=true};Set(remove,"m_Buff",Ref<BlueprintBuffReference>(effect));
                application=new AbilityAreaEffectRunAction{UnitEnter=Actions(onEnter),UnitExit=Actions(remove),Round=Actions(),UnitMove=Actions()};
            }
            var area=f.Add(new BlueprintAbilityAreaEffect{Size=new Feet(radius),Shape=AreaEffectShape.Cylinder,AffectEnemies=true,AggroEnemies=true,SpellResistance=false,ComponentsArray=new[]{application,calc}.Concat(Tags()).ToArray()},T(token+"Area"),name,text);SetEnum(area,"m_TargetType","Any");
            var add=new AddAreaEffect();Set(add,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));var host=f.Buff(T(token+"Host"),name,text,add);SetEnum(host,"m_Flags","HiddenInUi");Set(host,"m_Icon",icon);
            Visuals01448.Court(token,area,host,effect);
            var spend=new ActivatableAbilityResourceLogic{SpendType=ActivatableAbilityResourceLogic.ResourceSpendType.NewRound};Set(spend,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(f.Native<BlueprintAbilityResource>("e190ba276831b5c4fa28737e5e49e6a6")));
            var toggle=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.WithUnitCommand,Group=ActivatableAbilityGroup.BardicPerformance,DeactivateIfCombatEnded=true,DeactivateIfOwnerDisabled=true,ComponentsArray=new BlueprintComponent[]{spend}},T(token+"Toggle"),name,text);Set(toggle,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Standard);Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(host));Set(toggle,"m_Icon",icon);
            var feature=f.Feature(T(token+"Feature"),name,text,Facts(toggle));Set(feature,"m_Icon",icon);return feature;
        }
        internal static void Register(BlueprintsCache cache)
        {
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(ClassId);if(Existing(cache,RootToken,cls))return;Vacant(cache,OwnTokens);
            var courage=f.Native<BlueprintFeature>("acb4df34b25ca9043a6aba1a4c92bc69");var competence=f.Native<BlueprintFeature>("6d3fcfab6d935754c918eb0e004b5ef7");
            const string satireText="A court bard can use performance to undermine the confidence of enemies who hear it, causing them to take a –1 penalty on attack and damage rolls (minimum 1) and a –1 penalty on saves against fear and charm effects as long as the bard continues performing. This penalty increases by –1 at 5th level and every six levels thereafter. Satire is a language-dependent, mind-affecting ability that uses audible components.";
            var satire=f.Buff(T("SatireEffect"),"Satire",satireText,Rank(cls,"Custom",4,-1,10,-2,16,-3,20,-4),new AddContextStatBonus{Stat=StatType.AdditionalAttackBonus,Value=new ContextValue{ValueType=ContextValueType.Rank}},new AddContextStatBonus{Stat=StatType.AdditionalDamage,Value=new ContextValue{ValueType=ContextValueType.Rank}},new Kingmaker.Designers.Mechanics.Facts.SavingThrowBonusAgainstDescriptor{SpellDescriptor=SpellDescriptor.Fear|SpellDescriptor.Shaken|SpellDescriptor.Charm,Bonus=new ContextValue{ValueType=ContextValueType.Rank},ModifierDescriptor=ModifierDescriptor.UntypedStackable},new SpellDescriptorComponent{Descriptor=Descriptors});
            var satireFeature=Performance(f,cls,"Satire","Satire",satireText,"6b81d1245d2b45b49bb98a8e7d32c64d",50,satire);
            const string mockText="A court bard of 3rd level or higher can subtly ridicule and defame a specific individual. The bard selects one target who can hear his performance. That individual takes a –2 penalty on Charisma checks and Charisma-related skill checks as long as the bard continues performing. This penalty increases by –1 every four levels after 3rd. Mockery is a language-dependent, mind-affecting ability that relies on audible components.";
            // CotW applies Mockery as a 30-foot enemy aura, despite its tabletop single-target prose.
            var mock=f.Buff(T("MockeryEffect"),"Mockery",mockText,Rank(cls,"Custom",6,-2,10,-3,14,-4,18,-5,20,-6),new AddContextStatBonus{Stat=StatType.SkillPersuasion,Value=new ContextValue{ValueType=ContextValueType.Rank}},new AddContextStatBonus{Stat=StatType.SkillUseMagicDevice,Value=new ContextValue{ValueType=ContextValueType.Rank}},new CourtBardCharismaCheck01445(),new SpellDescriptorComponent{Descriptor=Descriptors});
            var mockFeature=Performance(f,cls,"Mockery","Mockery",mockText,"4b1f07a71a982824988d7f48cd49f3f8",30,mock);
            const string epicText="A court bard of 8th level or higher can weave captivating tales that engross those who hear them. Enemies within 30 feet become flat-footed unless they succeed at a Will save (DC 10 + 1/2 the bard’s level + the bard’s Cha modifier). A save renders them immune to this ability for 24 hours. Glorious epic is a language-dependent, mind-affecting ability that uses audible components.";
            var epic=f.Buff(T("GloriousEpicEffect"),"Glorious Epic",epicText,new CourtBardFlatFooted01445(),new SpellDescriptorComponent{Descriptor=Descriptors});var epicFeature=Performance(f,cls,"GloriousEpic","Glorious Epic",epicText,"ce7dad2b25acf85429b6c9550787b2d9",30,epic,true);
            const string scandalText="Court bard of 14th level or higher can combine salacious gossip and biting calumny to incite a riot. Each enemy within 50 feet is affected as if by a song of discord for as long as it can hear the performance. A successful Will save (DC 10 + 1/2 the bard’s level + the bard’s Cha modifier) negates the effect, and that creature is immune to this ability for 24 hours. Scandal is a language-dependent, mind-affecting ability that uses audible components.";
            var scandal=f.Clone(f.Native<BlueprintBuff>("2e1646c2449c88a4188e58043455a43a"),T("ScandalEffect"),"Scandal",scandalText);scandal.ComponentsArray=scandal.ComponentsArray.Where(c=>!(c is SpellDescriptorComponent)).Concat(Tags()).ToArray();var scandalFeature=Performance(f,cls,"Scandal","Scandal",scandalText,"2e1646c2449c88a4188e58043455a43a",50,scandal,true);
            const string heraldicText="A court bard gains a bonus equal to half his bard level on Persuasion and Knowledge (World) checks (minimum +1). Once per day, the court bard can also reroll a check against one of these skills, though he must take the result of the second roll even if it is worse. He can reroll one additional time per day at 5th level and every five levels thereafter.";
            var resource=Resource(f,T("HeraldicResource"),cls,1,5);var rank=Rank(cls,"Div2");Set(rank,"m_UseMin",true);Set(rank,"m_Min",1);
            var reroll=f.Buff(T("HeraldicBuff"),"Heraldic Expertise",heraldicText,new CourtBardReroll01445{Resource=Ref<BlueprintAbilityResourceReference>(resource)});var icon=f.Native<BlueprintAbility>("446f7bf201dc1934f96ac0a26e324803").Icon;Set(reroll,"m_Icon",icon);
            var toggle=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.Immediately,DeactivateImmediately=true,DoNotTurnOffOnRest=true},T("HeraldicToggle"),"Heraldic Expertise",heraldicText);Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(reroll));Set(toggle,"m_Icon",icon);
            var heraldic=f.Feature(T("HeraldicExpertise"),"Heraldic Expertise",heraldicText,rank,new AddContextStatBonus{Stat=StatType.SkillPersuasion,Value=new ContextValue{ValueType=ContextValueType.Rank}},new AddContextStatBonus{Stat=StatType.SkillKnowledgeWorld,Value=new ContextValue{ValueType=ContextValueType.Rank}},Grant(resource),Facts(toggle));Set(heraldic,"m_Icon",icon);
            var root=f.Add(new BlueprintArchetype{LocalizedName=Text(RootToken,"name","Court Bard"),LocalizedDescription=Text(RootToken,"description","Spending years studying all of the finer points of erudition and etiquette, the court bard takes up the role of resplendent proclaimer and artist-in-residence at the hand of nobility, royalty, and the well-moneyed elite who aspire to join their ranks."),RemoveFeatures=new[]{Entry(1,courage,f.Native<BlueprintFeature>("65cff8410a336654486c98fd3bacd8c5")),Entry(3,competence),Entry(5,courage),Entry(7,competence),Entry(8,f.Native<BlueprintFeature>("1d48ab2bded57a74dad8af3da07d313a")),Entry(10,f.Native<BlueprintFeature>("21fbafd5dc42d4d488c4d6caed46bc99")),Entry(11,courage,competence),Entry(14,f.Native<BlueprintFeature>("cfd8940869a304f4aa9077415f93febe")),Entry(15,competence),Entry(17,courage),Entry(19,competence)},AddFeatures=new[]{Entry(1,satireFeature,heraldic),Entry(3,mockFeature),Entry(8,epicFeature),Entry(14,scandalFeature)}},RootToken,"Court Bard","");Set(root,"m_ParentClass",cls);Publish(cache,f,OwnTokens,cls,root,new BlueprintFeatureBase[]{satireFeature,heraldic,mockFeature,epicFeature,scandalFeature});
        }
    }
    [TypeId("8bd20c5360ee40d9a98a5e376e9db6a1")]
    public sealed class CourtBardReroll01445:UnitBuffComponentDelegate,IInitiatorRulebookHandler<RuleRollD20>,IInitiatorRulebookSubscriber
    {
        public BlueprintAbilityResourceReference Resource;
        public void OnEventAboutToTrigger(RuleRollD20 e){var r=Resource?.Get();var check=Rulebook.CurrentContext?.PreviousEvent as RuleSkillCheck;if(!Buff.IsTurnedOn||Buff.IsSuppressed||e.IsFake||e.Result>0||e.Initiator!=Owner||r==null||Owner.Resources.GetResourceAmount(r)<=0||check==null||check.Initiator!=Owner||!new[]{StatType.SkillPersuasion,StatType.SkillKnowledgeWorld,StatType.CheckBluff,StatType.CheckDiplomacy,StatType.CheckIntimidate}.Contains(check.StatType)||e.Rerolls?.Any(x=>ReferenceEquals(x.Source,Fact))==true)return;var first=e.PreRollDice();if(check.IsSuccessRoll(first)||check.IsSuccessRoll(first,check.SuccessBonus))return;Owner.Resources.Spend(r,1);e.AddReroll(1,true,Fact);}
        public void OnEventDidTrigger(RuleRollD20 e){}
    }
    [TypeId("ac67b1e127e94a4f9963c27ec16f8366")]
    public sealed class CourtBardFlatFooted01445:UnitBuffComponentDelegate,ITargetRulebookHandler<RuleCheckTargetFlatFooted>,ITargetRulebookSubscriber
    {public void OnEventAboutToTrigger(RuleCheckTargetFlatFooted e){if(Buff.IsTurnedOn&&!Buff.IsSuppressed)e.ForceFlatFooted=true;}public void OnEventDidTrigger(RuleCheckTargetFlatFooted e){} }
    [TypeId("33c78b7cae564a779e095245c82e584d")]
    public sealed class CourtBardImmunity01445:ContextCondition
    {public BlueprintBuffReference Buff;public bool FromCaster=true;protected override string GetConditionCaption()=>"Performance immunity";protected override bool CheckCondition()=>Target?.Unit?.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==Buff.Guid&&(!FromCaster||b.Context?.MaybeCaster==Context.MaybeCaster))==true;}
    [TypeId("8da53bf49a454639978772cb1f93299c")]
    public sealed class CourtBardCharismaCheck01445:UnitBuffComponentDelegate,IInitiatorRulebookHandler<RuleSkillCheck>,IInitiatorRulebookSubscriber
    {public void OnEventAboutToTrigger(RuleSkillCheck e){if(e.StatType==StatType.Charisma)e.AddTemporaryModifier(e.Bonus.AddModifier(Context[AbilityRankType.Default],Fact,ModifierDescriptor.UntypedStackable));}public void OnEventDidTrigger(RuleSkillCheck e){} }

    // DarkCodex SetResourceDecreasing can enable list mode and then discard
    // the list when another provider already supplied a free-performance fact.
    // The native getter dereferences that null list on every actionbar refresh.
    // Read the surviving single fact and DC's published stable identity only
    // for Court Bard's shared performance pool. Do not rewrite shared blueprints.
    [HarmonyPatch(typeof(ActivatableAbilityResourceLogic),nameof(ActivatableAbilityResourceLogic.IsFreeBecauseOfFact),MethodType.Getter)]
    internal static class CourtBardFreePerformance01445Patch
    {
        static readonly System.Reflection.FieldInfo UseList=AccessTools.Field(typeof(ActivatableAbilityResourceLogic),"m_UseListForFreeBlueprints"),
            FreeList=AccessTools.Field(typeof(ActivatableAbilityResourceLogic),"m_FreeBlueprints");
        static readonly BlueprintGuid ResourceId=BlueprintGuid.Parse("e190ba276831b5c4fa28737e5e49e6a6"),
            LimitlessId=BlueprintGuid.Parse("b5c00e781d3a45aa9a2ae169c5210b47"),ArchetypeId=Id(CourtBard01445.RootToken);
        static bool Prefix(ActivatableAbilityResourceLogic __instance,ref bool __result)
        {
            if(__instance.RequiredResource?.AssetGuid!=ResourceId||!(bool)UseList.GetValue(__instance)||FreeList.GetValue(__instance)!=null)return true;
            var owner=__instance.Fact.Owner;
            if(!owner.Progression.Classes.Any(c=>c.Archetypes.Any(a=>a.AssetGuid==ArchetypeId)))return true;
            var single=__instance.FreeBlueprint;
            var limitless=ResourcesLibrary.BlueprintsCache.Load(LimitlessId)as BlueprintFeature;
            __result=single!=null&&Kingmaker.UnitLogic.UnitHelper.HasFact(owner.Unit,single)||limitless!=null&&limitless.name=="LimitlessBardicPerformance"&&Kingmaker.UnitLogic.UnitHelper.HasFact(owner.Unit,limitless);
            return false;
        }
    }
}
