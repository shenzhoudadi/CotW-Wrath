using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicFaithTests
{
    private static int count,turnedOff;
    private static bool friendly;
    private static EntityFactsManager auraFacts;
    private static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    private static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    private static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Check(bool b,string s){if(!b)throw new Exception("Faith: "+s);count++;}
    private static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicFaithConversionRestore).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
    private static EntityFactComponent Runtime(BlueprintComponent c,UnitFact fact){var r=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);r.Setup(c);r.RestoreOwnerLink(fact);return r;}
    private static void Patch(Harmony h,MethodBase method,string name)=>h.Patch(method,prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicFaithTests),name)){priority=Priority.First});
    internal static int Run(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        Console.WriteLine("Faith: progression, native conversions/costs, successful-cast restore, alignment and area ownership");
        Progression(cache,f);Conversions(cache,f);Aura(cache,f);
        PsychicExpansionTests.Inventory["faithAssertions"]=count;
        Console.WriteLine("PASS: "+count+" Faith checks; native managed calls with final buff installation/removal and faction/scene boundaries; no game run.");return count;
    }
    private static void Progression(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var choice=Asset<BlueprintFeatureSelection>(cache,"DisciplineSelection");
        Check(choice.AllFeatures.Select(x=>x.name).OrderBy(x=>x).SequenceEqual(new[]{"CotWWrathPsychicFaithDiscipline","CotWWrathPsychicFerocityDiscipline","CotWWrathPsychicPageantryDiscipline"}),"Exactly three complete disciplines offered");
        var faith=Asset<BlueprintProgression>(cache,"FaithDiscipline");
        Check(faith.LevelEntries.Select(x=>x.Level).SequenceEqual(new[]{1,4,5,6,8,10,12,13,14,16,18}),"All nine bonus spell milestones and 1/5/13 powers");
        Check(faith.GetComponent<PrerequisiteNoFeature>().Feature.AssetGuid.ToString()=="92c0d2da0a836ce418a267093c09ca54","Atheism excluded");
        Check(faith.LevelEntries[0].Features.Any(x=>x.GetComponent<AddAbilityResources>()?.Resource.AssetGuid.ToString()=="ddfc9f9a2cbd51b48cbd214dbd75c8fb"),"Wisdom phrenic pool");
        for(int level=1;level<=9;level++) {
            var add=Asset<BlueprintFeature>(cache,"FaithBonusSpell"+level).GetComponent<AddKnownSpell>();
            Check(add.SpellLevel==level && add.CharacterClass==f.Class.CharacterClass && add.Spell.Type==AbilityType.Spell,"Bonus spell circle/class/type "+level);
        }
        var resilience=Asset<BlueprintFeature>(cache,"FaithResilience");var rank=resilience.GetComponent<ContextRankConfig>();
        Check(resilience.ReapplyOnLevelUp && resilience.GetComponents<AddContextStatBonus>().Count()==3 && resilience.GetComponents<AddContextStatBonus>().All(x=>x.Descriptor==ModifierDescriptor.Resistance),"All saves use resistance and refresh on level-up");
        foreach(var sample in new[]{new[]{5,2},new[]{9,2},new[]{10,3},new[]{14,3},new[]{15,4},new[]{19,4},new[]{20,5}}){f.Class.Level=sample[0];var ctx=f.Cast(SpellSchool.Abjuration,SpellDescriptor.None,1).Context;Check(rank.GetValue(ctx)==sample[1],"Save scaling "+sample[0]);}
        var resource=Asset<BlueprintAbilityResource>(cache,"FaithDivineEnergyUses");
        foreach(int value in new[]{6,9,10,11,12,18,30}) {
            Set(f.Descriptor.Stats.Wisdom,"m_BaseValue",value);Set(f.Descriptor.Stats.Wisdom,"<ModifiedValue>k__BackingField",value);
            Check(resource.GetMaxAmount(f.Descriptor)==Math.Max(0,(int)Math.Floor((value-10)/2.0)),"Native Wisdom daily-use cap "+value);
        }
        Set(f.Descriptor.Stats.Wisdom,"m_BaseValue",30);Set(f.Descriptor.Stats.Wisdom,"<ModifiedValue>k__BackingField",30);
        var rounds=Asset<BlueprintAbilityResource>(cache,"FaithPrayerRounds");
        foreach(int level in new[]{13,17,20}){f.Class.Level=level;Check(rounds.GetMaxAmount(f.Descriptor)==level,"Aura rounds follow Psychic class level");}
    }
    private static void Conversions(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        // Surgery's earlier fixture replaced all six attributes with zero-base
        // damage samples. Reset casting eligibility for this independent suite.
        Set(f.Descriptor.Stats.Intelligence,"m_BaseValue",30);Set(f.Descriptor.Stats.Intelligence,"<ModifiedValue>k__BackingField",30);
        var manager=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",manager);var abilities=new AbilityCollection();abilities.Initialize(manager);Set(f.Unit,"<Abilities>k__BackingField",abilities);
        Set(f.Descriptor,"m_Spellbooks",new Dictionary<BlueprintSpellbook,Spellbook>{{f.Book.Blueprint,f.Book}});
        // Seed unlocked circles for the grant callback; full ApplySpellbook and
        // Unity character generation are outside this managed fixture.
        var dayTable=f.Book.Blueprint.SpellsPerDay;var oldLevels=dayTable.Levels;
        dayTable.Levels=Enumerable.Range(0,21).Select(_=>new SpellsLevelEntry {Count=new int[10]}).ToArray();
        Set(f.Descriptor.State.Features,"EnableSpellLevel10",new CountableFlag());
        try { foreach(int circle in Enumerable.Range(1,9)) {
            var feature=Asset<BlueprintFeature>(cache,"FaithBonusSpell"+circle);var add=feature.GetComponent<AddKnownSpell>();
            var bonusFact=Empty<Feature>();Set(bonusFact,"<Blueprint>k__BackingField",feature);Set(bonusFact,"Components",new List<EntityFactComponent>());bonusFact.RestoreOwnerLink(manager);
            var runtime=Runtime(add,bonusFact);
            using(runtime.RequestEventContext()){AccessTools.Method(typeof(AddKnownSpell),"OnActivate").Invoke(add,null);add.HandleLevelUpComplete(f.Unit,false);}
            Check(f.Book.GetKnownSpells(circle).Count(x=>x.Blueprint==add.Spell)==1,"Native bonus grant and level-up replay yield one known spell "+circle);
        } } finally {dayTable.Levels=oldLevels;}
        var pool=(BlueprintAbilityResource)cache.Load(BlueprintGuid.Parse("ddfc9f9a2cbd51b48cbd214dbd75c8fb"));
        var uses=Asset<BlueprintAbilityResource>(cache,"FaithDivineEnergyUses");
        var phrenic=new UnitAbilityResource(pool);var daily=new UnitAbilityResource(uses);var collection=new UnitAbilityResourceCollection(f.Descriptor);
        Set(collection,"m_Resources",new Dictionary<BlueprintScriptableObject,UnitAbilityResource>{{pool,phrenic},{uses,daily}});Set(f.Descriptor,"Resources",collection);
        var alignment=Empty<UnitAlignment>();Set(f.Descriptor,"Alignment",alignment);
        foreach(bool heal in new[]{true,false}) {
            string token=heal?"FaithCure":"FaithInflict";
            var feature=Asset<BlueprintFeature>(cache,token+"Feature");var conversion=feature.GetComponent<SpontaneousSpellConversion>();var restore=feature.GetComponent<PsychicFaithConversionRestore>();
            Check(feature.GetComponent<AddAbilityResources>().Resource==uses && restore.Conversions.Length==9,"Shared daily pool and nine explicit refund identities");
            Check(feature.GetComponent<PrerequisiteFeature>().Feature.AssetGuid.ToString()==(heal?"8c769102f3996684fb6e09a2c4e7e5b9":"dab5255d809f77c4395afc2b713e9cd6"),"Native deity permission gate");
            foreach(Alignment a in Enum.GetValues(typeof(Alignment))) {
                if(a.ToString()=="None")continue;
                Set(alignment,"m_Value",(Alignment?)a);
                bool actual=(bool)AccessTools.Method(typeof(PrerequisiteAlignment),"CheckInternal").Invoke(feature.GetComponent<PrerequisiteAlignment>(),new object[]{null,f.Descriptor,null});
                Check(actual==!a.ToString().EndsWith(heal?"Evil":"Good"),"Alignment conversion permission "+a+"/"+heal);
            }
            var fact=Empty<Feature>();Set(fact,"<Blueprint>k__BackingField",feature);Set(fact,"Components",new List<EntityFactComponent>());fact.RestoreOwnerLink(manager);
            var cr=Runtime(conversion,fact);var rr=Runtime(restore,fact);
            using(cr.RequestEventContext())AccessTools.Method(typeof(SpontaneousSpellConversion),"OnTurnOn").Invoke(conversion,null);
            for(int circle=0;circle<=9;circle++) {
                var original=new BlueprintAbility {AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),name="FaithSacrificeFixture",Type=AbilityType.Spell};original.OnEnable();var known=f.Book.AddKnown(circle,original,false);
                var choices=known.GetConversions().Where(x=>x.Blueprint.name.StartsWith("CotWWrathPsychicFaith")).ToArray();
                Check(choices.Length==(circle==0?0:1),"Exactly one native conversion per paid circle "+circle);
                if(circle==0){f.Book.RemoveSpell(original);continue;}
                var spell=choices.Single();var cost=spell.Blueprint.GetComponent<AbilityResourceLogic>();
                Check(spell.ConvertedFrom==known && spell.Spellbook==f.Book && spell.SpellLevel==circle,"Conversion retains original book and slot");
                Check(!spell.Blueprint.GetComponents<SpellListComponent>().Any() && !f.Book.GetKnownSpells(circle).Any(x=>x.Blueprint==spell.Blueprint),"Converted cure/inflict is not learned or on a class list");
                Set(daily,"<Amount>k__BackingField",0);Check(!cost.IsAbilityRestrictionPassed(spell),"Zero uses blocks conversion");
                Set(daily,"<Amount>k__BackingField",4);Set(phrenic,"<Amount>k__BackingField",1);var slots=(int[])Get(f.Book,"m_SpontaneousSlots");slots[circle]=2;
                Check(cost.RequiredResource==uses && cost.CalculateCost(spell)==1 && cost.IsAbilityRestrictionPassed(spell) && f.Book.CanSpend(known),"Daily use and native spell slot both available "+token+circle+" cost="+cost.CalculateCost(spell)+" allowed="+cost.IsAbilityRestrictionPassed(spell)+" slot="+f.Book.CanSpend(known));
                AccessTools.Method(typeof(AbilityData),"SpendOneSpellCharge").Invoke(spell,null);AbilityCastRateUtils.SpendResources(spell);
                Check(slots[circle]==1 && daily.Amount==3 && phrenic.Amount==1,"One spell slot and one daily use, no refund before successful cast");
                RuleCastSpell Rule(AbilityData data,bool success){var rule=f.Cast(SpellSchool.Conjuration,SpellDescriptor.None,circle);Set(rule,"Spell",data);Set(rule.Context,"Ability",data);rule.SetSuccess(success);return rule;}
                using(rr.RequestEventContext()) {
                    var failed=Rule(spell,false);restore.OnEventDidTrigger(failed);Check(phrenic.Amount==1,"Failed cast gives no phrenic point");
                    var success=Rule(spell,true);restore.OnEventDidTrigger(success);restore.OnEventDidTrigger(success);Check(phrenic.Amount==2,"Successful conversion restores exactly once");
                    var maximum=pool.GetMaxAmount(f.Descriptor);Set(phrenic,"<Amount>k__BackingField",maximum);restore.OnEventDidTrigger(Rule(spell,true));Check(phrenic.Amount==maximum,"Refund cannot overfill pool");
                    Set(phrenic,"<Amount>k__BackingField",1);var duplicate=Rule(spell,true);duplicate.IsDuplicateSpellApplied=true;restore.OnEventDidTrigger(duplicate);Check(phrenic.Amount==1,"Duplicate cast gives no extra refund");
                    duplicate=Rule(spell,true);duplicate.Context.IsDuplicateSpellApplied=true;restore.OnEventDidTrigger(duplicate);Check(phrenic.Amount==1,"Duplicate context gives no extra refund");
                    restore.OnEventDidTrigger(Rule(known,true));Check(phrenic.Amount==1,"Ordinary spell gives no refund");
                    var withoutConversion=new AbilityData(spell.Blueprint,f.Descriptor);restore.OnEventDidTrigger(Rule(withoutConversion,true));Check(phrenic.Amount==1,"Directly added ability gives no refund");
                    if(circle<=4) {
                        var delivery=spell.Blueprint.GetComponent<AbilityEffectStickyTouch>().TouchDeliveryAbility;
                        Check(delivery!=null && delivery.GetComponent<AbilityResourceLogic>()==null,"Native held-touch delivery has no daily cost");
                        var touch=new AbilityData(delivery,f.Descriptor);restore.OnEventDidTrigger(Rule(touch,true));Check(phrenic.Amount==1,"Later touch delivery cannot refund again");
                    }
                    var book=f.Book.Blueprint;Set(f.Book,"Blueprint",new BlueprintSpellbook {AssetGuid=BlueprintGuid.Parse("12345678901234567890123456789012")});restore.OnEventDidTrigger(Rule(spell,true));Check(phrenic.Amount==1,"Other spellbooks cannot borrow Faith refund");Set(f.Book,"Blueprint",book);
                }
                slots[circle]=0;Check(!f.Book.CanSpend(known),"No spell slot blocks conversion");f.Book.RemoveSpell(original);
            }
            using(cr.RequestEventContext())AccessTools.Method(typeof(SpontaneousSpellConversion),"OnTurnOff").Invoke(conversion,null);
            Check(((Dictionary<string,BlueprintAbility[]>)Get(f.Book,"m_SpellConversionLists")).Count==0,"Removing feature removes its native conversion list");
        }
    }
    private static void Aura(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var area=Asset<BlueprintAbilityAreaEffect>(cache,"FaithPrayerArea");var parts=area.GetComponents<AbilityAreaEffectBuff>().ToArray();var toggle=Asset<BlueprintActivatableAbility>(cache,"FaithPrayerToggle");
        var penaltyStat=new ModifiableValue(f.Unit.Stats,StatType.AdditionalAttackBonus);
        var ordinary=parts[1].Buff.GetComponents<AddStatBonus>().First();var opposed=parts[2].Buff.GetComponents<AddStatBonus>().First();
        penaltyStat.AddModifier(ordinary.Value,ordinary.Descriptor);penaltyStat.AddModifier(opposed.Value,opposed.Descriptor);
        Check(penaltyStat.ModifiedValue==-2,"Native luck penalties use the strongest value");
        Check(area.Size==new Feet(30) && area.CanTargetAllies && area.CanTargetEnemies && !area.SpellResistance && parts.Length==3 && parts.All(x=>x.CheckConditionEveryRound),"30-foot native aura handles both sides with round rechecks");
        Check(toggle.DeactivateImmediately && toggle.DeactivateIfCombatEnded && toggle.DeactivateIfOwnerDisabled && toggle.DeactivateIfOwnerUnconscious && toggle.Buff.GetComponent<AddAreaEffect>().AreaEffect==area,"Immediate toggle and native host lifecycle");
        for(int i=0;i<3;i++) {
            int bonus=new[]{1,-1,-2}[i];var buff=parts[i].Buff;
            Check(buff.GetComponents<AddStatBonus>().Count()==2 && buff.GetComponents<AddStatBonus>().All(c=>c.Value==bonus) && buff.GetComponent<BuffAllSavesBonus>().Value==bonus && buff.GetComponent<BuffAllSkillsBonus>().Value==bonus,"Attack/weapon damage/all saves/all skills "+bonus);
        }
        var target=new Psychic070Tests.Caster(cache);Set(target.Descriptor,"Alignment",Empty<UnitAlignment>());Set(f.Descriptor,"Alignment",Empty<UnitAlignment>());
        var ctx=f.Cast(SpellSchool.Enchantment,SpellDescriptor.None,1).Context;
        var predicate=parts[2].Condition.Conditions.OfType<PsychicFaithOpposedAlignment>().Single();
        var alignments=Enum.GetValues(typeof(Alignment)).Cast<Alignment>().Where(a=>a.ToString()!="None").ToArray();
        foreach(var a in alignments)foreach(var b in alignments) {
            Set(f.Descriptor.Alignment,"m_Value",(Alignment?)a);Set(target.Descriptor.Alignment,"m_Value",(Alignment?)b);
            string aa=a.ToString(),bb=b.ToString();bool expected=(aa.EndsWith("Good")&&bb.EndsWith("Evil"))||(aa.EndsWith("Evil")&&bb.EndsWith("Good"))||(aa.StartsWith("Lawful")&&bb.StartsWith("Chaotic"))||(aa.StartsWith("Chaotic")&&bb.StartsWith("Lawful"));
            using(ctx.GetDataScope(target.Unit))Check(predicate.Check()==expected,"Opposing alignment matrix "+a+"/"+b);
        }
        var h=new Harmony("CotW.Faith.SceneBoundaries");
        try {
            Patch(h,AccessTools.Method(typeof(ContextConditionIsAlly),"CheckCondition"),nameof(IsFriendly));Patch(h,AccessTools.Method(typeof(ContextConditionIsEnemy),"CheckCondition"),nameof(IsEnemy));
            Patch(h,AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),nameof(AddAuraBuff));
            Patch(h,AccessTools.Method(typeof(Buff),"Remove"),nameof(RemoveAuraBuff));Patch(h,AccessTools.Method(typeof(ActivatableAbility),"TurnOffImmediately"),nameof(TurnOff));
            auraFacts=new EntityFactsManager(target.Unit);Set(target.Unit,"Facts",auraFacts);var buffs=new BuffCollection();buffs.Initialize(auraFacts);Set(target.Unit,"<Buffs>k__BackingField",buffs);
            var entity=Empty<AreaEffectEntityData>();Set(entity,"<UniqueId>k__BackingField","faith-area");
            foreach(int sample in new[]{0,1,2}) {
                friendly=sample==0;Set(f.Descriptor.Alignment,"m_Value",(Alignment?)Alignment.LawfulGood);Set(target.Descriptor.Alignment,"m_Value",(Alignment?)(sample==2?Alignment.ChaoticEvil:Alignment.LawfulGood));
                foreach(var c in parts)AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitEnter").Invoke(c,new object[]{ctx,entity,target.Unit});
                Check(buffs.RawFacts.Count==1 && buffs.RawFacts[0].Blueprint==parts[sample].Buff,"Exactly one ally/ordinary/opposed effect; dual opposition never -3; branch="+sample+" facts="+buffs.RawFacts.Count);
                foreach(var c in parts)AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitEnter").Invoke(c,new object[]{ctx,entity,target.Unit});Check(buffs.RawFacts.Count==1,"Reentry does not duplicate owned buff");
                var unrelated=MakeBuff(parts[sample].Buff);unrelated.SourceAreaEffectId="other-area";auraFacts.List.Add(unrelated);RefreshAuraFacts();
                foreach(var c in parts)AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitExit").Invoke(c,new object[]{ctx,entity,target.Unit});
                Check(buffs.RawFacts.Count==1 && buffs.RawFacts[0]==unrelated,"Native exit removes own effect and preserves another aura source");auraFacts.List.Remove(unrelated);RefreshAuraFacts();
            }
            var rounds=Asset<BlueprintAbilityResource>(cache,"FaithPrayerRounds");var resource=new UnitAbilityResource(rounds);((Dictionary<BlueprintScriptableObject,UnitAbilityResource>)Get(f.Descriptor.Resources,"m_Resources"))[rounds]=resource;
            var fact=Empty<ActivatableAbility>();Set(fact,"<Blueprint>k__BackingField",toggle);Set(fact,"Components",new List<EntityFactComponent>());fact.RestoreOwnerLink(f.Unit.Facts);
            var cost=toggle.GetComponent<ActivatableAbilityResourceLogic>();var runtime=Runtime(cost,fact);
            Set(resource,"<Amount>k__BackingField",2);Check(cost.IsAvailable(runtime),"Native toggle available with rounds");
            using(runtime.RequestEventContext()){((IActivatableAbilitySpendResourceLogic)cost).OnNewRound();Check(resource.Amount==1,"Native round tick spends one round");((IActivatableAbilitySpendResourceLogic)cost).OnNewRound();Check(resource.Amount==0,"Second tick consumes final round");((IActivatableAbilitySpendResourceLogic)cost).OnNewRound();Check(turnedOff==1 && resource.Amount==0,"Exhaustion shuts off without negative rounds");}
            Check(!cost.IsAvailable(runtime),"No rounds means no reactivation");
        } finally {h.UnpatchAll(h.Id);}
    }
    private static bool IsFriendly(ref bool __result){__result=friendly;return false;}
    private static bool IsEnemy(ref bool __result){__result=!friendly;return false;}
    private static Buff MakeBuff(BlueprintBuff bp){var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);Set(b,"Components",new List<EntityFactComponent>());b.RestoreOwnerLink(auraFacts);return b;}
    private static void RefreshAuraFacts(){var c=Get(auraFacts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
    private static bool AddAuraBuff(BlueprintBuff __1,ref Buff __result){__result=MakeBuff(__1);auraFacts.List.Add(__result);RefreshAuraFacts();return false;}
    private static bool RemoveAuraBuff(Buff __instance){auraFacts.List.Remove(__instance);RefreshAuraFacts();return false;}
    private static bool TurnOff(){turnedOff++;return false;}
}
