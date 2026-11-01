using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using CallOfTheWild.Wrath.Prototype;

internal static class Psychic092Tests
{
    private static int count;
    private static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    private static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    private static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Check(bool b,string s){if(!b)throw new Exception(s);count++;}
    private static T Asset<T>(BlueprintsCache cache,string token) where T:SimpleBlueprint => (T)cache.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicAmplificationComponent).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
    private static EntityFactComponent Runtime(BlueprintComponent c,UnitFact fact) {var r=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);r.Setup(c);r.RestoreOwnerLink(fact);return r;}
    private static bool NoProfile(ref IDisposable __result){__result=null;return false;}
    internal static int Run(BlueprintsCache cache,Psychic070Tests.Caster f,Harmony h)
    {
        Console.WriteLine("Psychic 0.9.2: native slot conversions, pool feats and Countless Eyes");
        h.Patch(AccessTools.Method(typeof(Kingmaker.Utility.ProfileScope),"New",new[]{typeof(string),typeof(UnityEngine.Object)}),prefix:new HarmonyMethod(AccessTools.Method(typeof(Psychic092Tests),nameof(NoProfile))));
        var manager=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",manager);
        var abilities=new AbilityCollection();abilities.Initialize(manager);Set(f.Unit,"<Abilities>k__BackingField",abilities);
        var book=f.Book;var descriptor=f.Descriptor;
        Set(descriptor,"<Blueprint>k__BackingField",new BlueprintUnit {IsCheater=false});
        Set(descriptor,"m_Spellbooks",new Dictionary<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook,Spellbook>{{book.Blueprint,book}});
        Set(book,"m_KnownSpells",Enumerable.Range(0,10).Select(i=>new List<AbilityData>()).ToArray());Set(book,"m_SpecialSpells",Enumerable.Range(0,10).Select(i=>new List<AbilityData>()).ToArray());Set(book,"m_CustomSpells",Enumerable.Range(0,10).Select(i=>new List<AbilityData>()).ToArray());
        Set(book,"m_KnownSpellLevels",new Dictionary<BlueprintAbility,List<int>>());Set(book,"m_SpecialLists",new List<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList>());Set(book,"m_SpellConversionLists",new Dictionary<string,BlueprintAbility[]>());Set(book,"m_SpontaneousSlots",new int[10]);
        Set(book,"m_MemorizedSpells",Enumerable.Range(0,10).Select(i=>new List<SpellSlot>()).ToArray());
        Set(book,"m_MaxSpellLevel",(int?)10);
        Set(book,"m_HiddenCommonSlots",new Dictionary<int,Dictionary<int,SpellSlot>>());Set(book,"m_HiddenFavoriteSlots",new Dictionary<int,Dictionary<int,SpellSlot>>());
        var intelligence=new ModifiableValueAttributeStat(descriptor.Stats,StatType.Intelligence);Set(intelligence,"Type",StatType.Intelligence);Set(intelligence,"m_BaseValue",30);Set(intelligence,"<ModifiedValue>k__BackingField",30);Set(descriptor.Stats,"Intelligence",intelligence);((List<ModifiableValue>)Get(descriptor.Stats,"<AllStats>k__BackingField")).Add(intelligence);
        foreach(bool charisma in new[]{false,true}) {
            string poolName=charisma?"Charisma":"Wisdom";
            var feature=Asset<BlueprintFeature>(cache,"Psychofeedback"+poolName+"Conversions");
            var components=feature.GetComponents<SpontaneousSpellConversion>().ToArray();
            Check(components.Length==3,"Three separate native conversions for "+poolName);
            var fact=Empty<Feature>();Set(fact,"<Blueprint>k__BackingField",feature);Set(fact,"Components",new List<EntityFactComponent>());fact.RestoreOwnerLink(manager);
            var runtimes=components.Select(c=>Runtime(c,fact)).ToArray();
            for(int i=0;i<3;i++) using(runtimes[i].RequestEventContext())AccessTools.Method(typeof(SpontaneousSpellConversion),"OnTurnOn").Invoke(components[i],null);
            var pool=(BlueprintAbilityResource)cache.Load(BlueprintGuid.Parse(charisma?"0795daa1b7d259c3b5052c5c704cc031":"ddfc9f9a2cbd51b48cbd214dbd75c8fb"));
            var resource=new UnitAbilityResource(pool);var collection=new UnitAbilityResourceCollection(descriptor);Set(collection,"m_Resources",new Dictionary<BlueprintScriptableObject,UnitAbilityResource>{{pool,resource}});Set(descriptor,"Resources",collection);
            for(int circle=0;circle<=9;circle++) {
                var original=book.Blueprint.SpellList.SpellsByLevel[circle].Spells.First();book.AddKnown(circle,original,false);
                var known=book.GetKnownSpells(circle).First(a=>a.Blueprint==original);
                var conversions=known.GetConversions().Where(a=>a.Blueprint.name.StartsWith("CotWWrathPsychicPsychofeedback")).ToArray();
                var menuType=typeof(PsychicAmplificationComponent).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAmplificationMenu");
                var head=Empty<Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbility>();
                Set(head,"<Blueprint>k__BackingField",Asset<Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility>(cache,"PsychofeedbackMenu"));Set(head,"Components",new List<EntityFactComponent>());head.RestoreOwnerLink(manager);
                var menuSlots=(List<Kingmaker.UI.UnitSettings.MechanicActionBarSlot>)AccessTools.Method(menuType,"Children").Invoke(null,new object[]{head});
                var menuSpells=menuSlots.Cast<Kingmaker.UI.UnitSettings.MechanicActionBarSlotSpontaneusConvertedSpell>().Select(x=>x.Spell).ToArray();
                Check(menuSpells.Length==conversions.Length && menuSpells.All(x=>x.ConvertedFrom==known && x.Spellbook==book && x.SpellLevel==circle),"Phrenic menu retains real Psychofeedback slot conversions: "+poolName+"/"+circle);
                Check(conversions.Length==(circle>=2?3:0),"No sacrifice below circle 2; exactly three conversions otherwise: "+poolName+"/"+circle);
                foreach(var conversion in conversions) {
                    Check(conversion.ConvertedFrom==known && conversion.Spellbook==book && conversion.SpellLevel==circle,"Native conversion preserves the sacrificed spell and its slot");
                    var bp=conversion.Blueprint;var cost=bp.GetComponent<AbilityResourceLogic>();
                    Check(bp.Type==AbilityType.Supernatural && bp.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard && !bp.IsFullRoundAction,"Slot-sacrifice follows CotW supernatural standard-action semantics");
                    Check(cost.RequiredResource==pool && cost.CalculateCost(conversion)==2 && bp.GetComponent<PsychicAmplificationComponent>()==null,"Exactly the matching discipline resource is charged; no linked mode is attached");
                    Set(resource,"<Amount>k__BackingField",1);Check(!cost.IsAbilityRestrictionPassed(conversion),"One phrenic point blocks conversion");
                    Set(resource,"<Amount>k__BackingField",4);var slots=(int[])Get(book,"m_SpontaneousSlots");slots[circle]=2;
                    // Native SpendOneSpellCharge follows ConvertedFrom; the
                    // conversion blueprint is not an independently known spell.
                    Check(cost.IsAbilityRestrictionPassed(conversion) && book.CanSpend(conversion.ConvertedFrom),"Both phrenic points and the original native slot are available: "+poolName+"/"+circle);
                    // The outer SpendFromSpellbook queries Unity-backed mythic
                    // services. Exercise its native conversion/slot spender and
                    // resource dispatcher separately in this managed fixture.
                    AccessTools.Method(typeof(AbilityData),"SpendOneSpellCharge").Invoke(conversion,null);
                    AbilityCastRateUtils.SpendResources(conversion);
                    Check(slots[circle]==1 && resource.Amount==2,"Native conversion-slot spender and resource dispatcher consume one slot AND two points");
                    slots[circle]=0;Check(!book.CanSpend(conversion.ConvertedFrom),"No original native slot means no conversion");
                    var apply=bp.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single();
                    var bonus=apply.Buff.GetComponent<AddStatBonus>();
                    Check(bonus.Value==circle && bonus.Descriptor==ModifierDescriptor.Enhancement && new[]{StatType.Strength,StatType.Dexterity,StatType.Constitution}.Contains(bonus.Stat),"Converted level determines typed physical bonus");
                    var ctx=Empty<AbilityExecutionContext>();Set(ctx,"Ability",conversion);Set(ctx,"AssociatedBlueprint",bp);Set(ctx,"m_CasterRef",Get(conversion,"m_CasterRef"));Set(ctx,"<ClickedTarget>k__BackingField",new Kingmaker.Utility.TargetWrapper(f.Unit));ctx.SetParams(new AbilityParams {CasterLevel=30,SpellLevel=circle});
                    foreach(int classLevel in new[]{4,12,20}){f.Class.Level=classLevel;Check(bp.GetComponent<ContextRankConfig>().GetValue(ctx)==classLevel,"Duration uses psychic level, not boosted caster level");}
                    Set(ctx,"m_Ranks",new int[Enum.GetValues(typeof(AbilityRankType)).Cast<int>().Max()+1]);ctx[AbilityRankType.Default]=12;Check(apply.DurationValue.Calculate(ctx).Value==120 && apply.IsNotDispelable && !apply.IsFromSpell,"Native duration converts twelve minutes to 120 rounds");
                    Check(apply.Buff.FxOnRemove!=null && !ReferenceEquals(apply.Buff.Icon,null),"Conversion buff has valid removal metadata and attribute icon");
                }
                book.RemoveSpell(original);
            }
            for(int i=0;i<3;i++) using(runtimes[i].RequestEventContext())AccessTools.Method(typeof(SpontaneousSpellConversion),"OnTurnOff").Invoke(components[i],null);
            Check(((Dictionary<string,BlueprintAbility[]>)Get(book,"m_SpellConversionLists")).Count==0,"Native removal cleans only feature-owned conversion lists");
        }
        TestFeats(cache);TestEyes(cache,f,manager);
        PsychicExpansionTests.Inventory["psychic092Assertions"]=count;
        Console.WriteLine("PASS: "+count+" 0.9.2 assertions; native managed resource/slot/flag callbacks, not game UI/combat.");return count;
    }
    private static void TestFeats(BlueprintsCache cache)
    {
        var pool=Asset<BlueprintFeature>(cache,"ExpandedPhrenicPoolFeat");var extra=Asset<BlueprintFeatureSelection>(cache,"ExtraPhrenicAmplificationFeat");
        var basic=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("247a4068296e8be42890143f451b4b45"));
        Check(basic.AllFeatures.Count(f=>f==pool)==1 && basic.AllFeatures.Count(f=>f==extra)==1,"Both feats offered once in the general feat selection");
        Check(extra.Ranks==1 && extra.AllFeatures.Length==9 && extra.AllFeatures.Contains(Asset<BlueprintFeature>(cache,"PsychofeedbackFeature")),"Extra amplification includes the new option once");
        var increases=pool.GetComponents<IncreaseResourceAmount>().ToArray();var fact=Empty<Feature>();Set(fact,"<IsActive>k__BackingField",true);var runtimes=increases.Select(c=>Runtime(c,fact)).ToArray();
        foreach(int rank in new[]{1,2,10}) foreach(var id in new[]{"ddfc9f9a2cbd51b48cbd214dbd75c8fb","0795daa1b7d259c3b5052c5c704cc031"}) {
            Set(fact,"<Rank>k__BackingField",rank);int max=13;var resource=(BlueprintAbilityResource)cache.Load(BlueprintGuid.Parse(id));
            for(int i=0;i<increases.Length;i++)using(runtimes[i].RequestEventContext())increases[i].CalculateMaxResourceAmount(resource,ref max);
            Check(max==13+2*rank,"Expanded pool native ranks add two each without doubling across pools");
        }
    }
    private static void TestEyes(BlueprintsCache cache,Psychic070Tests.Caster f,EntityFactsManager manager)
    {
        var spell=Asset<BlueprintAbility>(cache,"CountlessEyes");var apply=spell.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single();var buff=apply.Buff;
        Check(spell.GetComponent<SpellListComponent>().SpellLevel==3 && spell.Range==AbilityRange.Touch && !spell.CanTargetEnemies,"Countless Eyes is a third-circle friendly touch spell");
        var ctx=Empty<MechanicsContext>();Set(ctx,"m_MainTarget",new Kingmaker.Utility.TargetWrapper(f.Unit));ctx.SetParams(new AbilityParams {CasterLevel=7});Set(ctx,"m_Ranks",new int[Enum.GetValues(typeof(AbilityRankType)).Cast<int>().Max()+1]);ctx[AbilityRankType.Default]=7;
        Check(apply.DurationValue.Calculate(ctx).Value==4200,"Seven-hour native duration");ctx.Params.Metamagic=Metamagic.Extend;Check(apply.DurationValue.Calculate(ctx).Value==8400,"Extend doubles actual duration");
        var flag=new Kingmaker.Utility.CountableFlag();Set(f.Descriptor.State.Features,"CannotBeFlanked",flag);
        var fact=Empty<Buff>();Set(fact,"<Blueprint>k__BackingField",buff);Set(fact,"Components",new List<EntityFactComponent>());fact.RestoreOwnerLink(manager);
        var c=buff.GetComponent<AddMechanicsFeature>();var runtime=Runtime(c,fact);
        AccessTools.Method(runtime.GetType(),"OnTurnOn").Invoke(runtime,null);Check(flag.Value,"Native buff callback prevents flanking");
        flag.Retain();AccessTools.Method(runtime.GetType(),"OnTurnOff").Invoke(runtime,null);Check(flag.Value,"Removal preserves another source of flank immunity");flag.Release();Check(!flag.Value,"Own immunity ends after removal");
    }
}
