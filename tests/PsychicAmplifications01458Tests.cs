using System;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static Spellbook Book058(BlueprintsCache c,UnitEntityData owner)
    {
        var book=(Spellbook)H("Book",owner,N<BlueprintCharacterClass>(c,Psychic058));Set(book,"m_BaseLevelInternal",owner.Progression.Classes[0].Level);return book;
    }
    static BlueprintAbilityResource Pool058(BlueprintsCache c,UnitEntityData owner)
    {
        Grant(owner,N<BlueprintFeature>(c,(string)AccessTools.Field(T("PsychicClass"),"CharismaFeatureId").GetRawConstantValue()));return N<BlueprintAbilityResource>(c,(string)AccessTools.Field(T("PsychicClass"),"CharismaPoolId").GetRawConstantValue());
    }
    static AbilityData Known058(BlueprintsCache c,Spellbook book,int circle,string token){var bp=A<BlueprintAbility>(c,token);book.AddKnown(circle,bp,false);return book.GetKnownSpells(circle).First(a=>a.Blueprint==bp);}
    static RuleCastSpell Linked058(AbilityData spell,UnitEntityData target)
    {
        var rule=new RuleCastSpell(spell,new TargetWrapper(target));return rule;
    }
    static void ModeEvents058(RuleCastSpell cast,bool success)
    {
        RulebookEventBus.OnEventAboutToTrigger(cast);cast.SetSuccess(success);RulebookEventBus.OnEventDidTrigger(cast);
    }
    static void Amplifications058(BlueprintsCache c,Harmony h)
    {
        foreach(string name in new[]{"NatureTargetScope01448Patch","NaturePaidTarget01448Patch","PsychicRelentlessCastingPatch","PsychicOngoingDefensePatch"})Feedback01450Tests.PatchOnce(h,T(name));
        DualModes058(c,h);Mimic058(c,h);Surge058(c,h);WillDead058(c,h);Space058(c,h);
        PsychicExpansionTests.Inventory["psychicAmplifications01458Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["coverage"]=new JArray(Coverage),["gameLaunchTested"]=false,["boundary"]="Actual native selection/ability parameters, native rule event dispatch, resources, conversions and spellbook spending. The linked mode probes explicitly dispatch native cast events; complete cast command and final scene delivery are separate contracts."};
    }
    static void DualModes058(BlueprintsCache c,Harmony h)
    {
        var owner=EquipmentUnit(c,20,Psychic058);var target=EquipmentUnit(c,20);var pool=Pool058(c,owner);var book=Book058(c,owner);var spell=Known058(c,book,1,"MindThrust1");Grant(owner,c,"OverpoweringMindFeature");Grant(owner,c,"RelentlessCastingFeature");Grant(owner,c,"DualAmplificationFeature01458");
        var relentless=Toggle(owner,c,"RelentlessCastingToggle");var overpower=Toggle(owner,c,"OverpoweringMind1Toggle");ToggleOn(relentless);ToggleOn(overpower);
        Check(relentless.IsOn&&overpower.IsOn,"native Dual activatables retain two distinct old amplification modes");int initial=owner.Resources.GetResourceAmount(pool);var cast=Linked058(spell,target);int dc=cast.Context.Params.DC;ModeEvents058(cast,true);
        Check(owner.Resources.GetResourceAmount(pool)==initial-3&&cast.Context.Params.DC==dc+1,"actual native cast event dispatch applies both old modes and pays each once");
        Check((bool)Call(T("PsychicAdvancedRuntime01458"),"HasDual",owner),"actual dual feature qualification is source owned");
        int after=owner.Resources.GetResourceAmount(pool);ModeEvents058(cast,true);Check(owner.Resources.GetResourceAmount(pool)==after&&cast.Context.Params.DC==dc+1,"duplicate cast notifications cannot pay or apply either Dual mode twice");
        owner.Resources.Restore(pool,99);initial=owner.Resources.GetResourceAmount(pool);var failed=Linked058(spell,target);dc=failed.Context.Params.DC;ModeEvents058(failed,false);Check(owner.Resources.GetResourceAmount(pool)==initial&&failed.Context.Params.DC==dc,"native failed cast notifications refund both reservations and restore per-cast DC");
        owner.Resources.Spend(pool,owner.Resources.GetResourceAmount(pool)-2);var scarce=Linked058(spell,target);ModeEvents058(scarce,true);Check(owner.Resources.GetResourceAmount(pool)==0&&scarce.Context.Params.DC==dc+1,"newest eligible mode gets scarce budget deterministically; unpaid second mode is omitted");
        owner.Resources.Restore(pool,99);var stronger=Toggle(owner,c,"OverpoweringMind2Toggle");ToggleOn(stronger);Check(stronger.IsOn&&!overpower.IsOn&&relentless.IsOn,"same-kind stronger mode replaces weaker variant while retaining other Dual mode");
        var ordinary=EquipmentUnit(c,20,Psychic058);Pool058(c,ordinary);Grant(ordinary,c,"OverpoweringMindFeature");Grant(ordinary,c,"RelentlessCastingFeature");var a=Toggle(ordinary,c,"OverpoweringMind1Toggle");var b=Toggle(ordinary,c,"RelentlessCastingToggle");ToggleOn(a);ToggleOn(b);Check(!a.IsOn&&b.IsOn,"without Dual the original one-mode switch behavior remains intact");
        var feat=owner.Facts.List.Single(v=>v.Blueprint.AssetGuid==Id("DualAmplificationFeature01458"));owner.Facts.Remove(feat);Check(owner.ActivatableAbilities.Enumerable.Count(v=>v.IsOn&&v.Blueprint.Buff?.GetComponent<PsychicAmplificationComponent>()!=null)==1,"native removal of Dual shrinks active mode ownership back to one");
    }
    static BlueprintFeature MimicLeaf058(BlueprintsCache c,Metamagic meta)=>A<BlueprintFeature>(c,"MimicMetamagicOption"+(uint)meta+"Feature01458");
    static ActivatableAbility MimicToggle058(BlueprintsCache c,UnitEntityData unit,Metamagic meta)=>Toggle(unit,c,"MimicMetamagicOption"+(uint)meta+"01458Toggle");
    static void Mimic058(BlueprintsCache c,Harmony h)
    {
        var root=A<BlueprintFeatureSelection>(c,"MimicMetamagic1Feature01458");var secondTier=A<BlueprintFeatureSelection>(c,"MimicMetamagic2Feature01458");var amp=A<BlueprintFeatureSelection>(c,"PhrenicAmplificationSelection");
        foreach(int level in new[]{10,11,20}){var u=EquipmentUnit(c,level,Psychic058);Check(Pick058(u,amp,root)==(level>=11),"actual native Mimic major prerequisite at Psychic level "+level);Check(!Pick058(u,amp,secondTier),"actual native second Mimic tier requires first tier: "+level);}
        var owner=EquipmentUnit(c,11,Psychic058);var target=EquipmentUnit(c,11);var pool=Pool058(c,owner);var book=Book058(c,owner);var spell=Known058(c,book,1,"MindThrust1");var two=Known058(c,book,2,"MindThrust2");Grant(owner,MimicLeaf058(c,Metamagic.Quicken));var toggle=MimicToggle058(c,owner,Metamagic.Quicken);ToggleOn(toggle);
        Check(book.MaxSpellLevel==5,"native eleventh-level Psychic book has fifth-level casting access");Check(spell.HasMetamagic(Metamagic.Quicken)&&spell.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift,"actual native AutoMetamagic registry grants Quicken action to eligible first-circle spell");
        Check(!two.HasMetamagic(Metamagic.Quicken),"native Mimic maximum level rule excludes second-circle Quicken before sixth-level access");
        int before=owner.Resources.GetResourceAmount(pool);var cast=Linked058(spell,target);NativeCast058(cast,h);Check(owner.Resources.GetResourceAmount(pool)==before-8&&cast.Context.HasMetamagic(Metamagic.Quicken)&&spell.SpellLevel==1&&book.GetSpellLevel(spell)==1,"native Mimic cast pays eight points and preserves original spell/slot level");
        cast.Context.Recalculate();Check(cast.Context.HasMetamagic(Metamagic.Quicken),"actual native context recalculation retains paid Mimic after resource depletion");
        ToggleOff(toggle);Check(!new AbilityData(spell.Blueprint,book,1).HasMetamagic(Metamagic.Quicken),"native mode-off removes preview Quicken without mutating known-spell metamagic data");
        Grant(owner,root);Check(Pick058(owner,amp,secondTier),"native second Mimic tier becomes selectable after first root is owned");var leaves=root.AllFeatures.OfType<BlueprintFeatureSelection>().ToArray();Check(leaves.Length>=9&&leaves.All(v=>v.AllFeatures.Length==leaves.Length-1),"native nested Mimic selection requires one distinct second option for each first choice");
        var firstForQuicken=leaves.Single(v=>v.GetComponent<Kingmaker.UnitLogic.FactLogic.AddFacts>().Facts.Contains(MimicLeaf058(c,Metamagic.Quicken)));Check(!Pick058(owner,root,firstForQuicken),"actual native first-choice prerequisite rejects previously acquired Mimic option");
        foreach(var first in leaves)Check(!first.AllFeatures.Contains(first.GetComponent<Kingmaker.UnitLogic.FactLogic.AddFacts>().Facts.Single()),"native second choice cannot duplicate its granted first choice: "+first.name);
        var fresh=EquipmentUnit(c,11,Psychic058);var firstChoice=leaves.First();var firstLeaf=(BlueprintFeature)firstChoice.GetComponent<AddFacts>().Facts.Single();Grant(fresh,firstChoice);var secondLeaf=firstChoice.AllFeatures.First(v=>v!=firstLeaf);Check(fresh.HasFact(firstLeaf)&&Pick058(fresh,firstChoice,secondLeaf),"actual native first nested choice grants its first metamagic and qualifies the distinct second choice");Grant(fresh,secondLeaf);Check(fresh.HasFact(firstLeaf)&&fresh.HasFact(secondLeaf)&&!Pick058(fresh,firstChoice,secondLeaf),"actual native second leaf grant completes two distinct options and blocks acquiring it twice");
        var toppling=MimicLeaf058(c,(Metamagic)(1<<28));if(toppling!=null)Check(toppling.GetComponent<TopplingSpellDamage>()!=null,"Mimic Toppling supplies actual CotW trip handler rather than an inert custom bit");
    }
    static void Surge058(BlueprintsCache c,Harmony h)
    {
        var owner=EquipmentUnit(c,20,Psychic058);var target=EquipmentUnit(c,20);var pool=Pool058(c,owner);var book=Book058(c,owner);var low=Known058(c,book,1,"MindThrust1");Known058(c,book,3,"MindThrust3");Grant(owner,c,"UndercastSurgeFeature01458");var mode=Toggle(owner,c,"UndercastSurge201458Toggle");ToggleOn(mode);
        var high=A<BlueprintAbility>(c,"MindThrust3");bool menu=Main.GetType("CallOfTheWild.Wrath.Prototype.UndercastSurgeConversions01458Patch")!=null;
        var offered=low.GetConversions().Where(a=>a.Blueprint==high&&ReferenceEquals(a.ConvertedFrom,low)).ToArray();Check(offered.Length==(menu?1:0),"actual native conversion list follows the versioned Surge UI policy without changing payment");
        var conversion=menu?offered.Single():((System.Collections.Generic.IEnumerable<AbilityData>)Call(T("PsychicAdvancedRuntime01458"),"SurgeConversions",low)).Single(a=>a.Blueprint==high);Check(conversion.SpellLevel==3&&book.GetSpellLevel(conversion)==1,"actual promoted known-family data retains higher effect and first-circle payment identity");
        Check(conversion.CalculateParams().SpellLevel==3,"actual native promoted ability parameters use higher effective circle");
        book.RestoreSpontaneousSlots(1,2);int lowBefore=book.GetSpontaneousSlots(1),highBefore=book.GetSpontaneousSlots(3);Check(book.Spend(conversion,false)&&book.GetSpontaneousSlots(1)==lowBefore-1&&book.GetSpontaneousSlots(3)==highBefore,"actual native Spellbook.Spend consumes lower circle and never a high slot");
        owner.Resources.Spend(pool,owner.Resources.GetResourceAmount(pool)-4);var cast=Linked058(conversion,target);NativeCast058(cast,h);Check(owner.Resources.GetResourceAmount(pool)==0,"native undercast cast-event reservation charges last four points exactly once");
        Check((bool)Call(T("PsychicAdvancedRuntime01458"),"SurgeValid",conversion,cast.Context),"actual paid cast validation accepts already spent last points from its exact context");
        var next=new AbilityData(high,book,1){ConvertedFrom=low,OverrideSpellLevel=3};Check(!(bool)Call(T("PsychicAdvancedRuntime01458"),"SurgeValid",next,null),"later unpaid conversion cannot borrow previous cast's last-point receipt");
        owner.Resources.Restore(pool,99);ToggleOff(mode);Check(!(bool)Call(T("PsychicAdvancedRuntime01458"),"SurgeValid",conversion,null),"native stale conversion is rejected after its matching surge mode is removed");
        ToggleOn(mode);conversion.ConvertedFrom=null;Check(!(bool)Call(T("PsychicAdvancedRuntime01458"),"SurgeValid",conversion,null),"malformed/reloaded reduced high-family spell without lower identity is rejected");
        var unknown=new AbilityData(A<BlueprintAbility>(c,"MindThrust4"),book,1){ConvertedFrom=low,OverrideSpellLevel=4};Check(!(bool)Call(T("PsychicAdvancedRuntime01458"),"SurgeValid",unknown,null),"native surge never fabricates a higher version that the owner does not know");
    }
    static void NativeCast058(RuleCastSpell cast,Harmony h)
    {
        var apply=AccessTools.Method(typeof(RuleApplySpell),nameof(RuleApplySpell.OnTrigger));var cutscene=AccessTools.PropertyGetter(typeof(RuleCastSpell),"IsCutscene");var endpoint=AccessTools.Method(typeof(AbilityData),nameof(AbilityData.Cast),new[]{typeof(AbilityExecutionContext)});
        var skip=AccessTools.Method(typeof(Feedback01448Tests),"Skip");var no=AccessTools.Method(typeof(Feedback01448Tests),"No");var capture=AccessTools.Method(typeof(Feedback01448Tests),"CastEndpoint");
        h.Patch(apply,prefix:new HarmonyMethod(skip));h.Patch(cutscene,prefix:new HarmonyMethod(no));h.Patch(endpoint,prefix:new HarmonyMethod(capture));cast.Initiator.Blueprint.IsCheater=false;AccessTools.Field(typeof(Feedback01448Tests),"castCalls").SetValue(null,0);
        try{RulebookEventBus.OnEventAboutToTrigger(cast);cast.OnTrigger(new RulebookEventContext());RulebookEventBus.OnEventDidTrigger(cast);Check(cast.Success&&(int)AccessTools.Field(typeof(Feedback01448Tests),"castCalls").GetValue(null)==1&&!cast.Context.IsForced,"actual native nonforced cast validates paid ability and reaches terminal scene endpoint: "+cast.Spell.Blueprint.name);}
        finally{h.Unpatch(apply,skip);h.Unpatch(cutscene,no);h.Unpatch(endpoint,capture);}
    }
    static void WillDead058(BlueprintsCache c,Harmony h)
    {
        var owner=EquipmentUnit(c,20,Psychic058);var enemy=EquipmentUnit(c,20);var pool=Pool058(c,owner);var book=Book058(c,owner);var native=N<BlueprintAbility>(c,"3c17035ec4717674cae2e841a190e757");book.AddKnown(9,native,false);var spell=book.GetKnownSpells(9).Single(a=>a.Blueprint==native);var undead=N<BlueprintFeature>(c,"734a29b693e9ec346ba2951b27987e33");Grant(enemy,undead);Set(enemy,"m_Position",new UnityEngine.Vector3(3,0,0));
        Grant(owner,c,"WillOfTheDeadFeature01458");var mode=Toggle(owner,c,"WillOfTheDead01458Toggle");var target=new TargetWrapper(enemy);Check(!spell.CanTarget(target),"actual native Dominate Monster rejects Undead without paid Will mode");ToggleOn(mode);
        int before=owner.Resources.GetResourceAmount(pool);Check(spell.CanTarget(target)&&owner.Resources.GetResourceAmount(pool)==before,"actual native Dominate Monster targeting permits Undead with affordable Will mode without preview payment");
        var unpaid=Linked058(spell,enemy);var part=enemy.Get<UnitPartSpellResistance>();Check(part.IsImmune(unpaid.Context,false),"unpaid native spell-immunity execution outside preview remains authoritative");
        for(int n=0;n<=2;n++){owner.Resources.Spend(pool,owner.Resources.GetResourceAmount(pool));owner.Resources.Restore(pool,n);Check(spell.CanTarget(target)==(n>=2),"actual native Will target affordability boundary: "+n);}
        var cast=Linked058(spell,enemy);NativeCast058(cast,h);Check(owner.Resources.GetResourceAmount(pool)==0&&!part.IsImmune(cast.Context,false),"paid Will execution bypasses only inherited Undead immunity after final two points");
        var intrinsic=(System.Collections.Generic.HashSet<BlueprintGuid>)AccessTools.Field(T("WillOfTheDead01458"),"Intrinsic").GetValue(null);var immunity=enemy.Facts.List.SelectMany(f=>f.Components).Where(v=>v.SourceBlueprintComponent is BuffDescriptorImmunity b&&(b.Descriptor.Value&SpellDescriptor.MindAffecting)!=0&&intrinsic.Contains(b.OwnerBlueprint.AssetGuid)).ToArray();Check(immunity.Length>0,"actual native Undead grants its inherent buff descriptor immunity runtime");
        foreach(var runtime in immunity)using(runtime.RequestEventContext()){var component=(BuffDescriptorImmunity)runtime.SourceBlueprintComponent;Check(!(bool)AccessTools.Method(typeof(BuffDescriptorImmunity),"IsImmune").Invoke(component,new object[]{cast.Context})&&(bool)AccessTools.Method(typeof(BuffDescriptorImmunity),"IsImmune").Invoke(component,new object[]{unpaid.Context}),"actual native Undead buff immunity is bypassed only for exact paid execution");}
        int extra=part.AddImmunity(SpellImmunityType.SpellDescriptor,0,null,SpellDescriptor.MindAffecting,null);Check(part.IsImmune(cast.Context,false),"independent additional native mind immunity survives Will bypass");part.Remove(extra);
        Check(!spell.CanTarget(target)&&part.IsImmune(Linked058(spell,enemy).Context,false),"later unpaid preview or context cannot borrow the previous Will receipt");owner.Resources.Restore(pool,99);
        var construct=Grant(enemy,N<BlueprintFeature>(c,"fd389783027d63343b4a5634bd81645f"));Check(!spell.CanTarget(target),"hybrid Undead plus Construct retains actual independent negative target requirement");enemy.Facts.Remove(construct);
        var wizard=N<BlueprintCharacterClass>(c,"ba34257984f4c41408ce1dc2004e342e");Set(spell,"m_CachedSpellbook",new Spellbook(owner.Descriptor,wizard.Spellbook));Check(!spell.CanTarget(target),"same native spell from non-Psychic book cannot acquire Will exception");Set(spell,"m_CachedSpellbook",book);
        var item=(Kingmaker.Items.ItemEntity)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Kingmaker.Items.ItemEntityUsable));var fact=new Ability(spell.Blueprint,owner.Descriptor);Set(fact,"<SourceItem>k__BackingField",item);Set(spell,"m_Fact",fact);Check(!spell.CanTarget(target),"actual item-source fact cannot use Will targeting exception");Set(spell,"m_Fact",null);
        ToggleOff(mode);Check(!spell.CanTarget(target)&&!part.IsImmune(cast.Context,false),"mode-off restores preview restriction while exact already paid execution keeps its receipt");
    }
}
