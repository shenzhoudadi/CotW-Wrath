using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicPageantryTests
{
    private static int count,removed,applied,bonusCL,bonusDC;
    private static EntityFactsManager facts;
    private static RuleStatCheck skillCheck;
    private static bool skillSuccess;
    private static UnitEntityData skillCaster;
    private static Type factory=>typeof(PsychicPageantryPower).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets");
    private static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    private static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    private static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Check(bool b,string s){if(!b)throw new Exception("Pageantry: "+s);count++;}
    private static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(factory,"Id").Invoke(null,new object[]{n}));
    private static EntityFactComponent Runtime(BlueprintComponent c,UnitFact fact){var r=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);r.Setup(c);r.RestoreOwnerLink(fact);return r;}
    private static void Patch(Harmony h,MethodBase m,string n)=>h.Patch(m,prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicPageantryTests),n)){priority=Priority.First});
    private static Buff MakeBuff(BlueprintBuff bp) {var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);Set(b,"Components",new List<EntityFactComponent>());Set(b,"<IsTurnedOn>k__BackingField",true);b.RestoreOwnerLink(facts);return b;}
    private static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
    internal static int Run(BlueprintsCache cache)
    {
        var f=new Psychic070Tests.Caster(cache);f.Class.Level=20;Set(f.Descriptor,"<Blueprint>k__BackingField",new BlueprintUnit());
        facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(f.Unit,"<Buffs>k__BackingField",buffs);
        var charisma=new ModifiableValueAttributeStat(f.Descriptor.Stats,StatType.Charisma);Set(charisma,"m_BaseValue",20);Set(charisma,"<ModifiedValue>k__BackingField",20);Set(f.Descriptor.Stats,"Charisma",charisma);((List<ModifiableValue>)Get(f.Descriptor.Stats,"<AllStats>k__BackingField")).Add(charisma);
        BlueprintAbilityResource pool;
        // Resolve the pool from the real level-one class feature, not a test copy.
        var progression=Asset<BlueprintProgression>(cache,"PageantryDiscipline");
        pool=progression.LevelEntries[0].Features.SelectMany(x=>x.GetComponents<AddAbilityResources>()).Single(x=>x.Resource.name.Contains("Phrenic")).Resource;
        var resource=new UnitAbilityResource(pool);((Dictionary<BlueprintScriptableObject,UnitAbilityResource>)Get(f.Descriptor.Resources,"m_Resources"))[pool]=resource;
        var h=new Harmony("CotW.Pageantry.NativeBoundaries");
        try {
            Patch(h,AccessTools.Method(typeof(UnitHelper),"RemoveFact",new[]{typeof(UnitEntityData),typeof(EntityFact)}),nameof(RemoveFact));
            Patch(h,AccessTools.Method(typeof(RuleCalculateAbilityParams),"AddBonusCasterLevel"),nameof(AddCL));
            Patch(h,AccessTools.Method(typeof(RuleCalculateAbilityParams),"AddBonusDC"),nameof(AddDC));
            Progression(cache,f,progression);Ritual(cache,f,resource,h);Power(cache,f,resource);Focus(cache,f,resource);Silence(cache,f,h);
        } finally {h.UnpatchAll(h.Id);}
        PsychicExpansionTests.Inventory["pageantryAssertions"]=count;
        Console.WriteLine("PASS: "+count+" Pageantry/Silence managed checks; final scene installation/removal and skill dice boundaries substituted, not gameplay.");return count;
    }
    private static void Progression(BlueprintsCache cache,Psychic070Tests.Caster f,BlueprintProgression p)
    {
        Check(p.LevelEntries.Select(x=>x.Level).SequenceEqual(new[]{1,4,5,6,8,10,12,13,14,16,18}),"1/5/13 abilities and nine bonus milestones");
        Check(p.LevelEntries[0].Features.Any(x=>x.name=="CotWWrathPsychicPageantryRitualFeature") && p.LevelEntries.Single(x=>x.Level==5).Features.Single().name=="CotWWrathPsychicPageantryPowerFeature" && p.LevelEntries.Single(x=>x.Level==13).Features.Single().name=="CotWWrathPsychicPageantryFocusFeature","Correct powers at correct class levels");
        var ids=new[]{"90e59f4a4ada87243b7b3535a06d0638",Asset<BlueprintAbility>(cache,"Silence").AssetGuid.ToString(),"f492622e473d34747806bdb39356eb89","4b8265132f9c8174f87ce7fa6d0fe47b","cb15cc8d7a5480648855a23b3ba3f93d","e15e5e7045fda2244b98c8f010adfe31","defbbeaef79eda64abc645036228a31b","e788b02f8d21014488067bdd3ba7b325","43740dab07286fe4aa00a6ee104ce7c1"};
        for(int i=1;i<=9;i++){var add=Asset<BlueprintFeature>(cache,"PageantryBonusSpell"+i).GetComponent<AddKnownSpell>();Check(add.SpellLevel==i && add.CharacterClass==f.Class.CharacterClass && add.Spell.AssetGuid.ToString()==ids[i-1],"Native bonus spell grant "+i);}
        var uses=Asset<BlueprintAbilityResource>(cache,"PageantryRitualUses");
        foreach(int value in new[]{6,9,10,11,12,18,30}){Set(f.Descriptor.Stats.Charisma,"m_BaseValue",value);Set(f.Descriptor.Stats.Charisma,"<ModifiedValue>k__BackingField",value);Check(uses.GetMaxAmount(f.Descriptor)==Math.Max(0,(int)Math.Floor((value-10)/2.0)),"Daily Charisma limit "+value+" actual="+uses.GetMaxAmount(f.Descriptor));}
    }
    private static void Ritual(BlueprintsCache cache,Psychic070Tests.Caster f,UnitAbilityResource pool,Harmony h)
    {
        var ability=Asset<BlueprintAbility>(cache,"PageantryRitualAbility");var choices=ability.GetComponent<AbilityVariants>().Variants;
        Check(choices.Length==11,"Eleven native Wrath skills");
        var dailyBp=Asset<BlueprintAbilityResource>(cache,"PageantryRitualUses");var daily=new UnitAbilityResource(dailyBp);((Dictionary<BlueprintScriptableObject,UnitAbilityResource>)Get(f.Descriptor.Resources,"m_Resources"))[dailyBp]=daily;
        Patch(h,AccessTools.Method(typeof(RuleStatCheck),"Create",new[]{typeof(UnitEntityData),typeof(StatType),typeof(int)}),nameof(CreateSkill));
        Patch(h,AccessTools.Method(typeof(Kingmaker.Designers.GameHelper),"TriggerStatCheck",new[]{typeof(RuleStatCheck),typeof(MechanicsContext),typeof(bool)}),nameof(TriggerSkill));
        Patch(h,AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(ApplyRitual));
        foreach(var spell in choices) {
            Check(spell.Parent==ability && !spell.CanTargetSelf && spell.CanTargetFriends && !spell.CanTargetEnemies,"Ally-only skill variant");
            var action=spell.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionSkillCheck>().Single();var effect=action.Success.Actions.OfType<ContextActionApplyBuff>().Single();var buff=effect.Buff;
            Check(action.CheckForCaster && action.UseCustomDC && action.CustomDC.Value==20 && effect.Permanent && effect.IsNotDispelable && buff.RemoveOnRest,"Caster DC20, permanent until used/rest");
            Check(buff.GetComponent<AddStatBonus>().Value==4 && buff.GetComponent<AddStatBonus>().Stat==action.Stat && buff.Stacking==StackingType.Replace,"Matching +4 and no self-stacking");
            Check(buff.GetComponents<AddInitiatorSkillRollTrigger>().Count()==(action.Stat==StatType.SkillPersuasion?4:1) && buff.GetComponents<AddInitiatorPartySkillRollTrigger>().Count()==(action.Stat==StatType.SkillPersuasion?4:1),"Persuasion covers bluff/intimidation/diplomacy");
            foreach(var trigger in buff.GetComponents<AddInitiatorSkillRollTrigger>()) foreach(var stat in new[]{trigger.Skill,StatType.Strength}) {
                var rule=Empty<RuleSkillCheck>();Set(rule,"<StatType>k__BackingField",stat);
                Check((bool)AccessTools.Method(typeof(AddInitiatorSkillRollTrigger),"CheckConditions").Invoke(trigger,new object[]{rule})==(stat==trigger.Skill),"Native skill trigger consumes only matching skill on success or failure");
                Check(!trigger.OnlySuccess && trigger.Action.Actions.Single() is ContextActionRemoveSelf,"Consume own buff after check, no foreign facts");
            }
            var cast=f.Cast(SpellSchool.Enchantment,SpellDescriptor.None,1);Set(cast.Spell,"Blueprint",spell);
            var cost=spell.GetComponent<AbilityResourceLogic>();Set(daily,"<Amount>k__BackingField",0);Check(!cost.IsAbilityRestrictionPassed(cast.Spell),"No daily use blocks");
            foreach(bool success in new[]{false,true}) {
                Set(daily,"<Amount>k__BackingField",3);Set(pool,"<Amount>k__BackingField",1);skillSuccess=success;applied=0;skillCaster=null;
                AbilityCastRateUtils.SpendResources(cast.Spell);
                using(cast.Context.GetDataScope(f.Unit))action.RunAction();
                Check(daily.Amount==2 && pool.Amount==(success?2:1) && applied==(success?1:0) && skillCaster==f.Unit,"Actual native skill action/cost routes success and failure");
                Check(skillCheck.StatType==action.Stat && skillCheck.DifficultyClass==20,"Native action asks chosen skill and DC20");
            }
            Set(pool,"<Amount>k__BackingField",((BlueprintAbilityResource)pool.Blueprint).GetMaxAmount(f.Descriptor));using(cast.Context.GetDataScope(f.Unit))action.RunAction();Check(pool.Amount==((BlueprintAbilityResource)pool.Blueprint).GetMaxAmount(f.Descriptor),"Ritual restore respects pool maximum");
        }
        h.Unpatch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),HarmonyPatchType.Prefix,h.Id);
    }
    private static void Power(BlueprintsCache cache,Psychic070Tests.Caster f,UnitAbilityResource pool)
    {
        var ability=Asset<BlueprintAbility>(cache,"PageantryPowerAbility");var apply=ability.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single();
        var buff=MakeBuff(apply.Buff);var c=apply.Buff.GetComponent<PsychicPageantryPower>();var runtime=Runtime(c,buff);
        var durationContext=Empty<MechanicsContext>();durationContext.SetParams(new AbilityParams());Set(durationContext,"m_MainTarget",new Kingmaker.Utility.TargetWrapper(f.Unit));
        Check(ability.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move && apply.DurationValue.Calculate(durationContext).Value==1 && apply.Buff.Stacking==StackingType.Replace,"Move preparation, one round fallback, replace stacking");
        var cost=ability.GetComponent<AbilityResourceLogic>();var prep=f.Cast(SpellSchool.Abjuration,0,1);Set(prep.Spell,"Blueprint",ability);Set(pool,"<Amount>k__BackingField",0);Check(!cost.IsAbilityRestrictionPassed(prep.Spell),"Empty pool blocks preparation");Set(pool,"<Amount>k__BackingField",2);AbilityCastRateUtils.SpendResources(prep.Spell);Check(pool.Amount==1,"Preparation spends exactly one point");
        using(runtime.RequestEventContext()) {
            for(int sample=0;sample<4;sample++) {
                var cast=f.Cast(SpellSchool.Enchantment,SpellDescriptor.MindAffecting,2);var original=f.Book.Blueprint;
                if(sample==1)Set(f.Book,"Blueprint",new BlueprintSpellbook());
                if(sample==2)Set(cast.Spell,"Blueprint",ability);
                if(sample==3)cast.IsDuplicateSpellApplied=true;
                var calculate=new RuleCalculateAbilityParams(f.Unit,cast.Spell);bonusCL=bonusDC=removed=0;c.OnEventAboutToTrigger(calculate);c.OnEventDidTrigger(cast);
                Check(bonusCL==(sample==0||sample==3?1:0) && bonusDC==bonusCL,"Only Psychic spell parameters boosted");Check(removed==(sample==0?1:0),"Only next ordinary Psychic cast consumes, including failure");Set(f.Book,"Blueprint",original);
            }
            removed=0;c.HandleUnitTurnEnded(new Psychic070Tests.Caster(cache).Unit);Check(removed==0,"Another unit's turn does not expire preparation");c.HandleUnitTurnEnded(f.Unit);Check(removed==1,"Owner turn end expires preparation");
            foreach(int dc in new[]{10,21}) {
                var concentration=new RuleCheckConcentration(f.Unit,f.Cast(SpellSchool.Enchantment,0,1).Spell);var roll=new RuleRollD20(f.Unit);roll.Override(20);Set(concentration,"<ResultRollRaw>k__BackingField",roll);Set(concentration,"<Concentration>k__BackingField",0);Set(concentration,"<DC>k__BackingField",dc);removed=0;c.OnEventDidTrigger(concentration);Check(removed==(dc==21?1:0),"Concentration failure before RuleCastSpell consumes preparation; success retains it for actual cast");
            }
        }
    }
    private static void Focus(BlueprintsCache cache,Psychic070Tests.Caster f,UnitAbilityResource pool)
    {
        var toggle=Asset<BlueprintActivatableAbility>(cache,"PageantryFocusToggle");var buff=MakeBuff(toggle.Buff);facts.List.Add(buff);Refresh();
        Check(toggle.DeactivateImmediately && toggle.ActivationType==AbilityActivationType.Immediately && toggle.Group==ActivatableAbilityGroup.None,"Independent immediate toggle coexists with amplification");
        var apply=AccessTools.Method(typeof(PsychicPageantryPower).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicPageantryFocus"),"Apply");
        foreach(int points in new[]{0,1,3})foreach(int branch in new[]{0,1,2,3}) {
            Set(pool,"<Amount>k__BackingField",points);Set(buff,"<IsTurnedOn>k__BackingField",branch!=2);Set(buff,"<IsSuppressed>k__BackingField",branch==3);
            var check=new RuleCheckConcentration(f.Unit,f.Cast(SpellSchool.Enchantment,0,1).Spell);var roll=new RuleRollD20(f.Unit,4);var ctx=new RulebookEventContext();ctx.PushEvent(branch==1?(RulebookEvent)Empty<RuleAttackRoll>():check);ctx.PushEvent(roll);
            apply.Invoke(null,new object[]{roll,ctx});apply.Invoke(null,new object[]{roll,ctx});roll.OnTrigger(ctx);bool active=points>0&&branch==0;
            Check(pool.Amount==points-(active?1:0) && roll.Result==(active?20:4),"Only active concentration pays once; zero/off/suppressed/unrelated roll stays ordinary");
            if(active){Set(check,"<ResultRollRaw>k__BackingField",roll);Set(check,"<Concentration>k__BackingField",0);Set(check,"<DC>k__BackingField",21);Check(!check.Success,"Treat as20 is not automatic success");}
        }
        facts.List.Remove(buff);Refresh();
    }
    private static void Silence(BlueprintsCache cache,Psychic070Tests.Caster f,Harmony h)
    {
        var spell=Asset<BlueprintAbility>(cache,"Silence");var spawn=spell.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionSpawnAreaEffect>().Single();var area=spawn.AreaEffect;var part=area.GetComponent<AbilityAreaEffectBuff>();
        Check(spell.IsFullRoundAction && spell.CanTargetPoint && !spell.SpellResistance && !spawn.OnUnit && area.CanTargetAllies && area.CanTargetEnemies && area.Size.Value==20,"Stationary 20ft full-round no-save area affects both sides");
        Check(spell.GetComponents<SpellListComponent>().Select(x=>x.SpellList.AssetGuid.ToString()).OrderBy(x=>x).SequenceEqual(new[]{"25a5013493bdcf74bb2424532214d0c8","8443ce803d2d31347897a3d85cc32f53","57c894665b7895c499b3dce058c284b3"}.OrderBy(x=>x)),"Bard/Cleric/Inquisitor2, discipline bonus only for Psychic");
        foreach(var link in spell.GetComponents<SpellListComponent>())Check(link.SpellLevel==2 && link.SpellList.SpellsByLevel[2].Spells.Count(x=>x==spell)==1,"Bidirectional shared list exactly once");
        foreach(int cl in new[]{1,5,20})foreach(bool extend in new[]{false,true}) {var cast=f.Cast(SpellSchool.Illusion,0,2);cast.Context.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=extend?Metamagic.Extend:0});Set(cast.Context,"AssociatedBlueprint",spell);Set(cast.Context,"<ClickedTarget>k__BackingField",new Kingmaker.Utility.TargetWrapper(f.Unit));Set(cast.Context,"m_Ranks",Enumerable.Repeat(spell.GetComponent<ContextRankConfig>().GetValue(cast.Context),32).ToArray());Check(spawn.DurationValue.Calculate(cast.Context).Value==cl*(extend?2:1),"Silence native CL duration/Extend");}
        Check(part.Buff.Stacking==StackingType.Stack && part.Buff.GetComponent<ForbidSpellCasting>()!=null && part.Buff.GetComponent<AddEnergyImmunity>()!=null && (SpellDescriptor)part.Buff.GetComponent<SpellImmunityToSpellDescriptor>().Descriptor==SpellDescriptor.Sonic && part.Buff.GetComponent<SuppressBuffs>().Buffs.Single().AssetGuid.ToString()=="cbfd2f5279f5946439fe82570fd61df2","Independent silence sources, sonic protection, Echolocation suppression");
        Patch(h,AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),nameof(AddAreaBuff));Patch(h,AccessTools.Method(typeof(Buff),"Remove"),nameof(RemoveAreaBuff));
        var ctx=f.Cast(SpellSchool.Illusion,0,2).Context;var a=Empty<AreaEffectEntityData>();Set(a,"<UniqueId>k__BackingField","silence-one");var b=Empty<AreaEffectEntityData>();Set(b,"<UniqueId>k__BackingField","silence-two");
        foreach(var source in new[]{a,b,a})AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitEnter").Invoke(part,new object[]{ctx,source,f.Unit});Check(f.Unit.Buffs.RawFacts.Count==2,"Native overlap owns two distinct buffs, no duplicate on reentry");
        var state=f.Descriptor.State;Set(state,"m_Conditions",new sbyte[100]);Set(state,"m_ConditionsImmunity",new sbyte[100]);var conditions=(sbyte[])Get(state,"m_Conditions");conditions[(int)UnitCondition.SpellcastingForbidden]=2;
        var casting=AccessTools.Method(typeof(PsychicPageantryPower).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicCasting"),"CheckCastingCondition");var psychic=f.Cast(SpellSchool.Enchantment,0,1).Spell;
        Check(!(bool)casting.Invoke(null,new object[]{state,UnitCondition.SpellcastingForbidden,psychic}),"Overlapping Silence still permits Psychic casting");conditions[(int)UnitCondition.SpellcastingForbidden]=3;Check((bool)casting.Invoke(null,new object[]{state,UnitCondition.SpellcastingForbidden,psychic}),"Silence plus unrelated casting restriction remains blocked");
        foreach(var source in new[]{a,b}) {AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitExit").Invoke(part,new object[]{ctx,source,f.Unit});Check(f.Unit.Buffs.RawFacts.Count==(source==a?1:0),"Native area exit removes only own fact");}
    }
    private static bool RemoveFact(){removed++;return false;}
    private static bool AddCL(int __0){bonusCL+=__0;return false;}
    private static bool AddDC(int __0){bonusDC+=__0;return false;}
    private static bool CreateSkill(UnitEntityData __0,StatType __1,int __2,ref RuleStatCheck __result){skillCaster=__0;var r=Empty<RuleSkillCheck>();Set(r,"Initiator",__0);Set(r,"<StatType>k__BackingField",__1);Set(r,"<DC>k__BackingField",__2);r.EnsureSuccess=skillSuccess;Set(r,"m_Success",skillSuccess);__result=skillCheck=r;return false;}
    private static bool TriggerSkill(RuleStatCheck __0,ref RuleStatCheck __result){__result=__0;return false;}
    private static bool ApplyRitual(){applied++;return false;}
    private static bool AddAreaBuff(BlueprintBuff __1,ref Buff __result){__result=MakeBuff(__1);facts.List.Add(__result);Refresh();return false;}
    private static bool RemoveAreaBuff(Buff __instance){facts.List.Remove(__instance);Refresh();return false;}
}
