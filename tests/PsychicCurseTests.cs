using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Combat;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicCurseTests
{
    static int count,aoo,applications,lastDC,lastCL,removed;
    static bool saved;static EntityFactsManager manager;static UnitEntityData target;
    static readonly List<RuleSavingThrow> saves=new List<RuleSavingThrow>();
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Check(bool ok,string why){if(!ok)throw new Exception("0.13.17: "+why);count++;}
    static T Asset<T>(BlueprintsCache c,string token) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicApplyMajorCurse).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
    static MechanicsContext Context(UnitEntityData unit,BlueprintScriptableObject bp,int dc=24,int cl=12){var ctx=new MechanicsContext(unit,unit.Descriptor,bp,null,new TargetWrapper(unit));ctx.SetParams(new AbilityParams {DC=dc,CasterLevel=cl,SpellLevel=6,Concentration=18});return ctx;}
    static IEnumerable<GameAction> Flat(ActionList list){foreach(var a in list.Actions){yield return a;if(a is Conditional c){foreach(var x in Flat(c.IfTrue))yield return x;foreach(var x in Flat(c.IfFalse))yield return x;}if(a is ContextActionSavingThrow s)foreach(var x in Flat(s.Actions))yield return x;if(a is ContextActionConditionalSaved b){foreach(var x in Flat(b.Succeed))yield return x;foreach(var x in Flat(b.Failed))yield return x;}}}
    internal static int Run(BlueprintsCache cache)
    {
        var caster=new Psychic070Tests.Caster(cache);target=caster.Unit;
        var h=new Harmony("CotW01317.NativeRules");void Patch(MethodBase m,string n)=>h.Patch(m,prefix:new HarmonyMethod(typeof(PsychicCurseTests),n){priority=Priority.First});
        Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Passed));Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"ImprovedEvasion"),nameof(False));
        PsychicRuleTestBoundary.Patch(h,typeof(RuleSavingThrow),AccessTools.Method(typeof(PsychicCurseTests),nameof(Save)));
        Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(Apply));
        Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Game),"CombatEngagementController"),nameof(Controller));
        Patch(AccessTools.Method(typeof(UnitCombatEngagementController),"ProvokeAttackOfOpportunity",new[]{typeof(UnitEntityData)}),nameof(Provoke));
        try {
            Unification(cache);
            var screech=Asset<BlueprintAbility>(cache,"Screech");var around=screech.GetComponent<AbilityTargetsAround>();
            Check((TargetType)Get(around,"m_TargetType")==TargetType.Enemy&&((Feet)Get(around,"m_Radius")).Value==30,"Screech 30-foot enemy filter");
            TargetSelection(cache,caster,screech,around);
            Check(screech.Range==AbilityRange.Personal&&screech.SpellResistance&&screech.School==SpellSchool.Evocation,"Screech range/school/SR");
            Check((SpellDescriptor)screech.GetComponent<SpellDescriptorComponent>().Descriptor==SpellDescriptor.Sonic,"Sonic immunity path");
            Check(screech.GetComponents<SpellListComponent>().Single().SpellList.AssetGuid.ToString()=="e17df9977b879b64e8a8cbb4b3569f19"&&screech.GetComponents<SpellListComponent>().Single().SpellLevel==3,"Witch level 3");
            foreach(bool pass in new[]{false,true})foreach(int dc in new[]{12,24,40}){
                saved=pass;aoo=0;saves.Clear();var ctx=Context(target,screech,dc);using(ctx.GetDataScope(target))screech.GetComponent<AbilityEffectRunAction>().Actions.Run();
                Check(saves.Count==1&&saves[0].Type==SavingThrowType.Fortitude&&saves[0].DifficultyClassBase==dc,"Single Fortitude at spell DC");Check(aoo==(pass?0:1),"Failed save provokes through native controller action");
            }
            var curse=Asset<BlueprintAbility>(cache,"CurseMajor");var variants=curse.GetComponent<AbilityVariants>().Variants;
            Check(variants.Length==4&&curse.GetComponents<SpellListComponent>().Select(c=>c.SpellLevel).OrderBy(x=>x).SequenceEqual(new[]{5,6}),"One major curse root, four choices, Cleric5 Wizard6");
            foreach(var spell in variants){
                Check(spell.Parent==curse&&spell.Range==AbilityRange.Close&&spell.GetComponent<AbilityDeliverTouch>()==null&&spell.GetComponent<AbilityEffectStickyTouch>()==null,"Close ranged curse without touch charge");
                var action=Flat(spell.GetComponent<AbilityEffectRunAction>().Actions).OfType<PsychicApplyMajorCurse>().Single();
                Check(action.Apply.Permanent&&!action.Apply.AsChild&&action.Apply.IsFromSpell&&!action.Apply.IsNotDispelable,"Native curse buff permanent and removable through normal curse tools");
                Check(((SpellDescriptor)action.Apply.Buff.GetComponent<SpellDescriptorComponent>().Descriptor).HasFlag(SpellDescriptor.Curse),"Native curse descriptor kept");
                foreach(int dc in new[]{10,24,45})foreach(int cl in new[]{1,12,30})foreach(bool pass in new[]{false,true}){
                    saved=pass;applications=0;saves.Clear();var ctx=Context(target,spell,dc,cl);using(ctx.GetDataScope(target))spell.GetComponent<AbilityEffectRunAction>().Actions.Run();
                    Check(saves.Count==1&&saves[0].Type==SavingThrowType.Will&&saves[0].DifficultyClassBase==dc,"Initial curse save unchanged");
                    Check(applications==(pass?0:1),"Successful save applies no curse");
                    if(!pass)Check(lastDC==dc+5&&lastCL==cl,"Only removal DC +5; CL unchanged");
                    Check(ctx.Params.DC==dc&&ctx.Params.CasterLevel==cl,"Cast context remains unchanged");
                }
            }
            BlindnessLifecycle(cache,caster);
        }finally{h.UnpatchAll(h.Id);}
        PsychicExpansionTests.Inventory["curseAndUnificationAssertions"]=count;
        Console.WriteLine("PASS: "+count+" curse/unification checks; native actions and buff replacement executed, spell/save and scene service boundaries seeded.");return count;
    }
    static UnitEntityData[] nearby;
    static bool Nearby(ref IEnumerable<UnitEntityData> __result){__result=nearby;return false;}
    static bool Enemy(UnitEntityData __0,ref bool __result){__result=!ReferenceEquals(__0,target);return false;}
    static void TargetSelection(BlueprintsCache cache,Psychic070Tests.Caster caster,BlueprintAbility spell,AbilityTargetsAround around){
        var h=new Harmony("CotW01317.TargetSelection");
        h.Patch(AccessTools.Method(typeof(Kingmaker.Designers.GameHelper),"GetTargetsAround",new[]{typeof(UnityEngine.Vector3),typeof(Feet),typeof(bool),typeof(bool)}),prefix:new HarmonyMethod(typeof(PsychicCurseTests),nameof(Nearby)));
        foreach(string method in new[]{"IsEnemy","CanAttack"})h.Patch(AccessTools.Method(typeof(UnitEntityData),method,new[]{typeof(UnitEntityData)}),prefix:new HarmonyMethod(typeof(PsychicCurseTests),nameof(Enemy)));
        h.Patch(AccessTools.PropertyGetter(typeof(UnitEntityData),"IsPlayerFaction"),prefix:new HarmonyMethod(typeof(PsychicCurseTests),nameof(False)));
        try{
            var cast=caster.Cast(SpellSchool.Evocation,SpellDescriptor.Sonic,3);Set(cast.Spell,"Blueprint",spell);Set(cast.Context,"AssociatedBlueprint",spell);
            var enemy=new Psychic070Tests.Caster(cache).Unit;
            foreach(var units in new[]{Array.Empty<UnitEntityData>(),new[]{target},new[]{target,enemy}}){
                nearby=units;var actual=around.Select(cast.Context,new TargetWrapper(UnityEngine.Vector3.zero)).Select(t=>t.Unit).ToArray();
                Check(actual.SequenceEqual(units.Where(u=>!ReferenceEquals(u,target))),"Native area selection handles empty/friendly/enemy without null condition");
            }
        }finally{h.UnpatchAll(h.Id);}
    }
    static void Unification(BlueprintsCache c){
        var nativeBlind=(BlueprintBuff)c.Load(BlueprintGuid.Parse("187f88d96a0ef464280706b63635f2af"));var nativeNausea=(BlueprintBuff)c.Load(BlueprintGuid.Parse("956331dba5125ef48afe41875a00ca0e"));
        foreach(bool blind in new[]{false,true}){
            string token=blind?"WallOfBlindness":"WallOfNausea";var old=Asset<BlueprintBuff>(c,token+"Debuff");var area=Asset<BlueprintAbilityAreaEffect>(c,token+"Area");
            var apply=Flat(area.GetComponent<PsychicUnoccupiedWall>().UnitEnter).OfType<ContextActionApplyBuff>().Single();
            Check(apply.Buff==(blind?nativeBlind:old),"New wall applications use chosen exact status identity");Check(apply.Permanent==blind&&apply.NotLinkToAreaEffect&&!apply.AsChild,"Wall end does not clear independent affliction");
            Check(old!=nativeBlind&&old!=nativeNausea,"Old GUID still registered separately");
            Check(!old.GetComponents<SpellDescriptorComponent>().Any(d=>((SpellDescriptor)d.Descriptor).HasFlag(SpellDescriptor.Poison)),"No poison descriptor on wall status");
            Check(ReferenceEquals(old.Icon,(blind?nativeBlind:nativeNausea).Icon),"Generic status icon used");
        }
        var daze=Asset<BlueprintAbility>(c,"DazeMass");var applied=Flat(daze.GetComponent<AbilityEffectRunAction>().Actions).OfType<ContextActionApplyBuff>().First();
        Check(applied.Buff.AssetGuid.ToString()=="9934fedff1b14994ea90205d189c8759"&&Asset<BlueprintBuff>(c,"DazeMassBuff")!=applied.Buff,"Mass Daze native status, legacy GUID retained");
        Check(nativeBlind.Stacking==StackingType.Replace,"No global change to native blindness stacking");
    }
    static void Refresh(){var o=Get(manager,"m_Cache");AccessTools.Method(o.GetType(),"Clear").Invoke(o,null);}
    static Buff Fact(BlueprintBuff bp,MechanicsContext ctx,TimeSpan? duration){var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);Set(b,"m_Context",ctx);Set(b,"Components",new List<EntityFactComponent>());Set(b,"<PlannedDuration>k__BackingField",duration);Set(b,"m_EndTime",duration.HasValue?(TimeSpan?)(TimeSpan.FromSeconds(100)+duration.Value):null);b.RestoreOwnerLink(manager);return b;}
    static void BlindnessLifecycle(BlueprintsCache c,Psychic070Tests.Caster caster){
        manager=new EntityFactsManager(target);Set(target,"Facts",manager);var collection=new BuffCollection();collection.Initialize(manager);Set(target,"<Buffs>k__BackingField",collection);
        var method=AccessTools.Method(typeof(BuffCollection),"PrepareFactForAttach",new[]{typeof(Buff)});var h=new Harmony("CotW01317.NativeBuffPrepare");var type=typeof(PsychicApplyMajorCurse).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicWallBlindnessDurationPatch");
        h.Patch(method,prefix:new HarmonyMethod(type,"Prefix"),transpiler:new HarmonyMethod(typeof(PsychicCurseTests),nameof(CanApplyBoundary)));
        h.Patch(AccessTools.Method(typeof(EntityFactsManager),"Remove",new[]{typeof(EntityFact),typeof(bool)}),prefix:new HarmonyMethod(typeof(PsychicCurseTests),nameof(Remove)));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),prefix:new HarmonyMethod(typeof(PsychicCurseTests),nameof(Time)));
        h.Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController),"IsInTurnBasedCombat"),prefix:new HarmonyMethod(typeof(PsychicCurseTests),nameof(False)));
        var oldTime=Kingmaker.Game.Instance.TimeController;Set(Kingmaker.Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());
        try{
            var bp=(BlueprintBuff)c.Load(BlueprintGuid.Parse("187f88d96a0ef464280706b63635f2af"));var wall=Context(target,Asset<BlueprintAbility>(c,"WallOfBlindness"));var other=Context(target,Asset<BlueprintAbility>(c,"Screech"));
            foreach(bool wallSource in new[]{false,true})foreach(bool existingPermanent in new[]{false,true})foreach(bool incomingPermanent in new[]{false,true}){
                manager.List.Clear();var old=Fact(bp,wallSource?wall:other,existingPermanent?(TimeSpan?)null:TimeSpan.FromSeconds(6));manager.List.Add(old);Refresh();removed=0;
                var next=Fact(bp,incomingPermanent?wall:other,incomingPermanent?(TimeSpan?)null:TimeSpan.FromSeconds(6));
                var result=(Buff)method.Invoke(collection,new object[]{next});bool protectedWall=wallSource&&existingPermanent&&!incomingPermanent;
                Check(protectedWall?result==null:result==next,"Native replacement matrix with wall-only permanent protection");
                Check(removed==(protectedWall?0:1),"Permanent wall source survives temporary replacement; other native replacement unchanged");
                if(protectedWall)Check(old.IsPermanent&&old.Context==wall,"Retains permanent duration and original source context");
                if(incomingPermanent)Check(next.IsPermanent,"Temporary followed by permanent stays permanent");
            }
            manager.List.Clear();Refresh();var incoming=Fact(bp,wall,null);Check(method.Invoke(collection,new object[]{incoming})==incoming&&incoming.IsPermanent,"Removing previous blindness permits fresh wall application");
        }finally{h.UnpatchAll(h.Id);Set(Kingmaker.Game.Instance,"TimeController",oldTime);manager.List.Clear();Refresh();}
    }
    static IEnumerable<CodeInstruction> CanApplyBoundary(IEnumerable<CodeInstruction> instructions){foreach(var c in instructions){if(c.operand is MethodInfo m&&m.DeclaringType==typeof(Rulebook)&&m.Name=="Trigger"){c.opcode=OpCodes.Call;c.operand=AccessTools.Method(typeof(PsychicCurseTests),nameof(CanApply));}yield return c;}}
    static RuleCanApplyBuff CanApply(RuleCanApplyBuff rule){rule.CanApply=true;return rule;}
    static bool Remove(EntityFact __0){removed++;manager.List.Remove(__0);Refresh();return false;}
    static bool Time(ref TimeSpan __result){__result=TimeSpan.FromSeconds(100);return false;}
    static bool Save(RuleSavingThrow rule,ref RuleSavingThrow __result){saves.Add(rule);__result=rule;return false;}
    static bool Passed(ref bool __result){__result=saved;return false;}static bool False(ref bool __result){__result=false;return false;}
    static bool Apply(ContextActionApplyBuff __instance){applications++;var ctx=(MechanicsContext)AccessTools.Property(typeof(ContextAction),"Context").GetValue(__instance);lastDC=ctx.Params.DC;lastCL=ctx.Params.CasterLevel;var child=ctx.CloneFor(__instance.Buff,target.Descriptor,null);Check(child.Params.DC==lastDC&&child.Params.CasterLevel==lastCL,"Native buff context cloning preserves adjusted DC and CL");return false;}
    static bool Controller(ref UnitCombatEngagementController __result){__result=Empty<UnitCombatEngagementController>();return false;}
    static bool Provoke(UnitEntityData __0){Check(__0==target,"Native action provokes on target, not caster");aoo++;return false;}
}
