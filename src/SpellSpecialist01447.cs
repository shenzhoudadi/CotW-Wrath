// Original CotW Classes/Arcanist.cs createSpellSpecialist (MIT).
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class SpellSpecialist01447
    {
        internal const string ModuleName="Arcanist archetype: Spell Specialist",RootToken="SpellSpecialistArchetype01447",ClassId="52dbfd8505e22f84fad8d702611f60b7",PoolId="cac948cbbe79b55459459dd6a8fe44ce";
        internal const string Description="Where most arcanists are broad in their study of magic, a spell specialist has her power focused in a few spells. Spell specialists are able to warp and twist the magic of their signature spells in ways other casters cannot.";
        internal const string SignatureText="At 1st level and each time a spell specialist gains a new spell level, she chooses a signature spell.\nThe DC for signature spells increases by 1. The spell specialist gains a +2 bonus on concentration checks when casting signature spells; this bonus increases to +4 at 10th level.";
        internal const string BenderText="The spell specialist can apply selective metamagic feat to any of her signature spells without increasing spell level or casting time by spending 1 point from her arcane reservoir.";
        internal const string DismissText="A spell specialist can dismiss a signature spell as a swift action instead of a standard action by spending 1 point from her arcane reservoir.";
        internal static bool Signature(UnitEntityData unit,BlueprintAbility spell)=>unit!=null&&spell!=null&&unit.Facts.List.OfType<Feature>().Any(f=>f.IsActive&&f.IsTurnedOn&&f.Blueprint.GetComponent<SignatureSpell01447>()!=null&&(f.Param.Blueprint==spell||f.Param.Blueprint==spell.Parent));
        internal static string Token(int level)=>"SignatureSpell"+level+"01447";
        internal static void Register(BlueprintsCache cache) {
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(ClassId);if(Existing(cache,RootToken,cls))return;
            var exploits=f.Native<BlueprintFeatureSelection>("b8bf3d5023f2d8c428fdf6438cecaea7");foreach(int level in new[]{1,7,13,19})if(cls.Progression.LevelEntries.Where(e=>e.Level==level).SelectMany(e=>e.Features).Count(x=>x==exploits)!=1)throw new InvalidOperationException("01447 Arcanist exploit replacement changed.");
            var reservoir=f.Native<BlueprintAbilityResource>(PoolId);var icon=f.Native<BlueprintFeature>("85f3340093d144dd944fff9a9adfd2f2");
            var signatures=new BlueprintFeatureSelection[9];var roman=new[]{"I","II","III","IV","V","VI","VII","VIII","IX"};
            for(int i=0;i<9;i++) {
                int level=i+1;string token=Token(level);var spells=cls.Spellbook.SpellList.SpellsByLevel.Single(e=>e.SpellLevel==level).Spells.ToArray();if(spells.Length==0)throw new InvalidOperationException("01447 Arcanist spell list incomplete: "+level);
                var learn=new LearnSpellParametrized{SpecificSpellLevel=true,SpellLevel=level};Set(learn,"m_SpellcasterClass",Ref<BlueprintCharacterClassReference>(cls));Set(learn,"m_SpellList",Ref<BlueprintSpellListReference>(cls.Spellbook.SpellList));
                // Native Custom parameters expose all available class spells,
                // including already-known spells, as CotW AvailableSpell does.
                var parameter=f.Add(new BlueprintParametrizedFeature{ParameterType=FeatureParameterType.Custom,SpecificSpellLevel=true,SpellLevel=level,Ranks=1,HasNoSuchFeature=true,BlueprintParameterVariants=spells.Select(Ref<AnyBlueprintReference>).ToArray(),ComponentsArray=new BlueprintComponent[]{learn,new SignatureSpell01447()}},token+"Parameter","Signature Spell "+roman[i],SignatureText);Set(parameter,"m_SpellcasterClass",Ref<BlueprintCharacterClassReference>(cls));Set(parameter,"m_SpellList",Ref<BlueprintSpellListReference>(cls.Spellbook.SpellList));SpellBatch01447.Icon(parameter,icon);
                var selection=f.Add(new BlueprintFeatureSelection{Ranks=1,IsClassFeature=true},token,"Signature Spell "+roman[i],SignatureText);Set(selection,"m_Features",new[]{Ref<BlueprintFeatureReference>(parameter)});Set(selection,"m_AllFeatures",new[]{Ref<BlueprintFeatureReference>(parameter)});SpellBatch01447.Icon(selection,icon);signatures[i]=selection;
            }
            var buff=f.Buff("SpellBenderBuff01447","Spell Bender",BenderText,new SpellBender01447{Resource=Ref<BlueprintAbilityResourceReference>(reservoir)});
            var toggle=BloodlineMutations01446.Toggle(f,"SpellBenderToggle01447","Spell Bender",BenderText,buff,reservoir,icon);
            var bender=f.Feature("SpellBenderFeature01447","Spell Bender",BenderText,Facts(toggle));SpellBatch01447.Icon(bender,icon);
            var dismiss=f.Ability("DismissSignatureAbility01447","Dismiss Signature Spell",DismissText,AbilityType.Supernatural,UnitCommand.CommandType.Swift,AbilityRange.Medium,Run(new DismissSignature01447()),new DismissSignatureTarget01447(),Cost(reservoir));dismiss.CanTargetPoint=true;dismiss.CanTargetFriends=dismiss.CanTargetSelf=dismiss.CanTargetEnemies=false;dismiss.NotOffensive=true;SpellBatch01447.Icon(dismiss,f.Native<BlueprintAbility>("92681f181b507b34ea87018e8f7a528a"));
            var dismissFeature=f.Feature("DismissSignatureFeature01447","Dismiss Signature Spell",DismissText,Facts(dismiss));SpellBatch01447.Icon(dismissFeature,dismiss);
            var levels=new[]{1,4,6,8,10,12,14,16,18};var add=levels.Select((level,i)=>i==0?Entry(level,signatures[i],dismissFeature,bender):Entry(level,signatures[i])).ToArray();
            var root=SpellBatch01447.Archetype(f,cls,RootToken,"Spell Specialist",Description,add,new[]{1,7,13,19}.Select(level=>Entry(level,exploits)).ToArray());
            SpellBatch01447.PublishArchetype(cache,f,cls,root,signatures.Cast<BlueprintFeatureBase>().Concat(new[]{dismissFeature,bender}).ToArray());
        }
        internal static AreaEffectEntityData Dismissible(UnitEntityData caster,TargetWrapper target)=>Game.Instance.State.AreaEffects.FirstOrDefault(a=>a.Context.MaybeCaster==caster&&a.Context.SourceAbility!=null&&!(a.Context.AssociatedBlueprint is BlueprintBuff)&&((TimeSpan?)Get(a,"m_Duration")).HasValue&&a.View?.Shape!=null&&a.View.Shape.Contains(target.Point,0f)&&Signature(caster,a.Context.SourceAbility));
    }
    [TypeId("077d918ec6c94fd5b70b6856d6180147")]
    public sealed class SignatureSpell01447 : UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCalculateAbilityParams>,IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams e){if(e.Spellbook?.Blueprint.CharacterClass?.AssetGuid!=BlueprintGuid.Parse(SpellSpecialist01447.ClassId)||((Feature)Fact).Param.Blueprint!=e.Spell&&((Feature)Fact).Param.Blueprint!=e.Spell?.Parent)return;
            if(Owner.Facts.List.OfType<Feature>().FirstOrDefault(f=>f.IsActive&&f.IsTurnedOn&&f.Blueprint.GetComponent<SignatureSpell01447>()!=null&&(f.Param.Blueprint==e.Spell||f.Param.Blueprint==e.Spell?.Parent))!=Fact)return;
            e.AddBonusDC(1);e.AddBonusConcentration(Owner.Progression.GetClassLevel(e.Spellbook.Blueprint.CharacterClass)>=10?4:2);}
        public void OnEventDidTrigger(RuleCalculateAbilityParams e){}
    }
    [TypeId("16755e1023504e9caafec7cd12950147")]
    public sealed class SpellBender01447 : UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCastSpell>,IInitiatorRulebookSubscriber
    {
        public BlueprintAbilityResourceReference Resource;
        sealed class Charge {internal BlueprintAbilityResource Pool;internal AbilityParams Before;internal bool Ended;}
        static readonly ConditionalWeakTable<RuleCastSpell,Charge> Casts=new ConditionalWeakTable<RuleCastSpell,Charge>();
        public void OnEventAboutToTrigger(RuleCastSpell e) {
            if(e.ForceFail||e.IsDuplicateSpellApplied||e.Context==null||e.Context.IsDuplicateSpellApplied||e.Spell?.SourceItem!=null||e.Spell?.StickyTouch!=null||e.Spell?.Spellbook?.Blueprint.CharacterClass?.AssetGuid!=BlueprintGuid.Parse(SpellSpecialist01447.ClassId)||!SpellSpecialist01447.Signature(Owner,e.Spell.Blueprint)||e.Context.HasMetamagic(Metamagic.Selective)||Casts.TryGetValue(e,out _))return;
            var pool=Resource.Get();if(!Owner.Resources.HasEnoughResource(pool,1))return;var charge=new Charge{Pool=pool,Before=e.Context.Params};Casts.Add(e,charge);Owner.Resources.Spend(pool,1);var copy=e.Context.Params.Clone();copy.Metamagic|=Metamagic.Selective;e.Context.SetParams(copy);
        }
        public void OnEventDidTrigger(RuleCastSpell e){if(!Casts.TryGetValue(e,out var charge)||charge.Ended)return;charge.Ended=true;if(!e.Success){Owner.Resources.Restore(charge.Pool,1);e.Context.SetParams(charge.Before);}}
    }
    [TypeId("9431658dd32742f48a445ed7e8890147")]
    public sealed class DismissSignature01447 : ContextAction {public override string GetCaption()=>"Dismiss";public override void RunAction()=>SpellSpecialist01447.Dismissible(Context.MaybeCaster,Target)?.ForceEnd();}
    [TypeId("513b75177e2943f18a565a791cc20147")]
    public sealed class DismissSignatureTarget01447 : BlueprintComponent,IAbilityTargetRestriction {public bool IsTargetRestrictionPassed(UnitEntityData caster,TargetWrapper target)=>SpellSpecialist01447.Dismissible(caster,target)!=null;public string GetAbilityTargetRestrictionUIText(UnitEntityData caster,TargetWrapper target)=>"Dismiss Signature Spell";}
    [HarmonyPatch(typeof(BlueprintParametrizedFeature),nameof(BlueprintParametrizedFeature.ExtractSelectionItems))]
    internal static class SignatureSelection01447Patch
    {
        static bool Prefix(BlueprintParametrizedFeature __instance,UnitDescriptor __1,ref IEnumerable<IFeatureSelectionItem> __result) {
            if(__instance.GetComponent<SignatureSpell01447>()==null)return true;
            var cls=ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(BlueprintGuid.Parse(SpellSpecialist01447.ClassId));var list=__1?.GetSpellbook(cls)?.Blueprint.SpellList;
            __result=list==null?Enumerable.Empty<IFeatureSelectionItem>():list.SpellsByLevel.Where(e=>e.SpellLevel==__instance.SpellLevel).SelectMany(e=>e.Spells).Distinct().Select(s=>(IFeatureSelectionItem)new FeatureUIData(__instance,new FeatureParam(s))).ToArray();return false;
        }
    }
}
