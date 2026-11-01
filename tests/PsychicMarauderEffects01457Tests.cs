using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static readonly HashSet<UnitEntityData> Enemies057=new HashSet<UnitEntityData>();
    static int roll057=2,d3roll057=1,saveCount057,commandCount057;
    static RuleSavingThrow lastSave057;
    static string command057;
    static bool Enemy057(UnitEntityData __instance,UnitEntityData __0,ref bool __result){if(!Actors.Contains(__instance)||!Actors.Contains(__0))return true;__result=Enemies057.Contains(__instance)!=Enemies057.Contains(__0);return false;}
    static bool Dice057(DiceFormula __0,ref int __result){if(__0.Dice==DiceType.D20)__result=roll057;else if(__0.Dice==DiceType.D100)__result=1;else if(__0.Dice==DiceType.D3)__result=d3roll057;else return true;return false;}
    static void Save057(RuleSavingThrow __instance){lastSave057=__instance;saveCount057++;}
    static bool Command057(MethodBase __originalMethod,ref UnitCommand __result){command057=__originalMethod.Name;commandCount057++;__result=null;return false;}
    static Buff ForeignBuff057(UnitEntityData owner,BlueprintBuff bp,UnitEntityData caster){var b=new Buff(bp,new MechanicsContext(caster,owner.Descriptor,bp),null);owner.Facts.Add(b);return b;}
    static AreaEffectEntityData AuraArea057(string id,Buff host,BlueprintAbilityAreaEffect bp,params UnitEntityData[] actors)=>(AreaEffectEntityData)AccessTools.Method(typeof(TimeStutter01437Tests),"MakeArea").Invoke(null,new object[]{id,host.Context,bp,actors});
    static Buff CastAura057(BlueprintsCache c,UnitEntityData caster,string mode,int radius=5,bool charisma=false)
    {
        string pool=charisma?"Charisma":"Wisdom";var bp=A<BlueprintAbility>(c,"Marauder"+mode+radius+pool+"Ability01457");var a=new AbilityData(bp,caster.Descriptor);
        var logic=bp.GetComponent<AbilityResourceLogic>();int before=caster.Resources.GetResourceAmount(logic.RequiredResource);int cost=mode=="I"?1:mode=="II"?2:3;
        Check(logic.CalculateCost(a)==cost&&logic.IsAbilityRestrictionPassed(a),"actual native aura cost and availability "+mode+" "+pool);
        var parameters=a.CalculateParams();Check(parameters.DC==10+caster.Progression.Classes[0].Level/2+caster.Stats.Intelligence.Bonus,"native aura ability calculates class/Intelligence DC "+mode);
        AbilityCastRateUtils.SpendResources(a);var ctx=new AbilityExecutionContext(a,parameters,new TargetWrapper(caster),new RulebookEventContext(),caster);AbilityExecutionProcess.PrepareCast(ctx);bp.GetComponent<AbilityEffectRunAction>().Apply(ctx,new TargetWrapper(caster));
        Check(caster.Resources.GetResourceAmount(logic.RequiredResource)==before-cost,"actual native aura payment consumes only selected phrenic pool "+mode);
        var aura=caster.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("Marauder"+mode+radius+"Aura01457"));Console.WriteLine("057 aura timer "+mode+" permanent="+aura.IsPermanent+" end="+aura.EndTime+" game="+Kingmaker.Game.Instance.TimeController.GameTime+" charisma="+caster.Stats.Charisma.Bonus+" rank="+ctx[Kingmaker.Enums.AbilityRankType.Default]);Check(!aura.IsPermanent&&aura.EndTime-Kingmaker.Game.Instance.TimeController.GameTime==TimeSpan.FromSeconds(6*caster.Stats.Charisma.Bonus),"native aura duration uses Charisma modifier rounds "+mode);return aura;
    }
    static void MarauderEffects057(BlueprintsCache c,BlueprintArchetype root,Harmony h)
    {
        h.Patch(AccessTools.Method(typeof(UnitEntityData),"IsEnemy",new[]{typeof(UnitEntityData)}),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Enemy057)){priority=Priority.First});
        h.Patch(AccessTools.Method(typeof(RulebookEvent.Dice),"D",new[]{typeof(DiceFormula)}),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Dice057)){priority=Priority.First});
        h.Patch(AccessTools.Method(typeof(RuleSavingThrow),"OnTrigger"),postfix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Save057)));
        var controller=typeof(Kingmaker.Controllers.Units.UnitConfusionController);foreach(string name in new[]{"DoNothing","SelfHarm","AttackNearest"})h.Patch(AccessTools.Method(controller,name),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Command057)));
        var caster=EquipmentUnit(c,20,"83fa3b50138c566698801ba5967bbde0");caster.Progression.Classes[0].AddArchetype(root);caster.Stats.Charisma.BaseValue=18;caster.Stats.Intelligence.BaseValue=20;
        Grant(caster,N<BlueprintFeature>(c,(string)AccessTools.Field(T("PsychicClass"),"WisdomFeatureId").GetRawConstantValue()));Grant(caster,c,"MarauderAuraI01457");Grant(caster,c,"MarauderAuraII01457");Grant(caster,c,"MarauderAuraIII01457");
        // The level20 personal aura must remain usable alongside its own
        // complete immunity; the attack affects enemies rather than its caster.
        Grant(caster,c,"MarauderUnreal01457");
        var enemy=EquipmentUnit(c,12);var ally=EquipmentUnit(c,12);Enemies057.Add(enemy);enemy.Stats.Wisdom.BaseValue=10;enemy.Stats.SaveWill.BaseValue=0;Set(enemy,"PreventDirectControl",new CountableFlag());
        var confused=N<BlueprintBuff>(c,"886c7407dc629dc499b9f1465ff382df");
        var host=CastAura057(c,caster,"I");var areaBlueprint=A<BlueprintAbilityAreaEffect>(c,"MarauderI5Area01457");var area=AuraArea057("057-first",host,areaBlueprint,enemy,ally);var component=areaBlueprint.GetComponent<AbilityAreaEffectBuff>();
        component.HandleUnitEnter(host.Context,area,ally);Check(!ally.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==Id("MarauderIMember01457")),"actual native aura entry excludes ally");
        int before=saveCount057;component.HandleUnitEnter(host.Context,area,enemy);component.HandleUnitEnter(host.Context,area,enemy);
        var marker=enemy.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("MarauderIMember01457"));Check(saveCount057==before,"native entry/repeated entry grants one marker without immediate or movement save");
        roll057=20;marker.TickMechanics();Check(saveCount057==before+1&&lastSave057.IsPassed&&!enemy.Buffs.Enumerable.Any(b=>b.Blueprint==confused),"actual native owner-round callback succeeds save without confusion");
        roll057=2;marker.TickMechanics();Check(saveCount057==before+2&&!lastSave057.IsPassed&&enemy.Buffs.Enumerable.Any(b=>b.Blueprint==confused&&b.Context.MaybeCaster==caster),"actual native owner-round callback fails save and applies caster-owned confusion");
        var effect=enemy.Buffs.Enumerable.Single(b=>b.Blueprint==confused);Check(effect.EndTime-Kingmaker.Game.Instance.TimeController.GameTime==TimeSpan.FromSeconds(6),"actual native confusion duration is one round");
        component.HandleUnitExit(host.Context,area,enemy);Check(!enemy.Buffs.Enumerable.Contains(marker)&&!effect.IsDisposed,"native area exit removes its marker while failed one-round confusion remains");effect.Remove();
        var immune=Grant(enemy,c,"MarauderCracked01457");component.HandleUnitEnter(host.Context,area,enemy);Check(!enemy.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==Id("MarauderIMember01457"))&&!enemy.Buffs.Enumerable.Any(b=>b.Blueprint==confused),"actual descriptor immunity rejects the confusion aura marker");enemy.Facts.Remove(immune);
        host.Remove();
        foreach(string mode in new[]{"II","IIIStill","IIIHarm","IIIAttack"}){
            host=CastAura057(c,caster,mode);areaBlueprint=A<BlueprintAbilityAreaEffect>(c,"Marauder"+mode+"5Area01457");area=AuraArea057("057-"+mode,host,areaBlueprint,enemy);component=areaBlueprint.GetComponent<AbilityAreaEffectBuff>();component.HandleUnitEnter(host.Context,area,enemy);
            marker=enemy.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("Marauder"+mode+"Member01457"));roll057=2;marker.TickMechanics();
            var control=enemy.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("Marauder"+mode+"Control01457"));Check(control.Context.MaybeCaster==caster,"actual native failed save creates source-bound confusion control "+mode);
            var part=enemy.Ensure<UnitPartConfusion>();part.RoundStartTime=Kingmaker.Game.Instance.TimeController.GameTime-TimeSpan.FromSeconds(7);part.Cmd=null;commandCount057=0;command057=null;d3roll057=2;
            AccessTools.Method(controller,"TickConfusion").Invoke(null,new object[]{enemy});
            var state=mode=="IIIStill"?ConfusionState.DoNothing:mode=="IIIAttack"?ConfusionState.AttackNearest:ConfusionState.SelfHarm;
            Check(part.State==state&&commandCount057==1&&command057==(state==ConfusionState.DoNothing?"DoNothing":state==ConfusionState.SelfHarm?"SelfHarm":"AttackNearest"),"actual native confusion roll/event/command selection follows controlled state "+mode);
            Check(enemy.PreventDirectControl.Count==1,"native confusion control flag is retained exactly once "+mode);
            component.HandleUnitExit(host.Context,area,enemy);foreach(var b in enemy.Buffs.Enumerable.Where(b=>b.Blueprint==confused||b.Blueprint.name.Contains("Control01457")).ToArray())b.Remove();enemy.Remove<UnitPartConfusion>();Check(enemy.PreventDirectControl.Count==0,"native part disposal balances retained confusion control "+mode);host.Remove();
        }
        // Real stacked marker ownership: removing one caster's area must not
        // remove another caster's marker or already-created confusion.
        var other=EquipmentUnit(c,20,"83fa3b50138c566698801ba5967bbde0");other.Stats.Charisma.BaseValue=18;other.Stats.Intelligence.BaseValue=20;Grant(other,N<BlueprintFeature>(c,(string)AccessTools.Field(T("PsychicClass"),"WisdomFeatureId").GetRawConstantValue()));Grant(other,c,"MarauderAuraI01457");
        caster.Resources.Restore(N<BlueprintAbilityResource>(c,(string)AccessTools.Field(T("PsychicClass"),"WisdomPoolId").GetRawConstantValue()));
        host=CastAura057(c,caster,"I");var second=CastAura057(c,other,"I");areaBlueprint=A<BlueprintAbilityAreaEffect>(c,"MarauderI5Area01457");component=areaBlueprint.GetComponent<AbilityAreaEffectBuff>();area=AuraArea057("057-overlap-one",host,areaBlueprint,enemy);var otherArea=AuraArea057("057-overlap-two",second,areaBlueprint,enemy);
        component.HandleUnitEnter(host.Context,area,enemy);component.HandleUnitEnter(second.Context,otherArea,enemy);
        var overlapping=enemy.Buffs.Enumerable.Where(b=>b.Blueprint.AssetGuid==Id("MarauderIMember01457")).ToArray();Check(overlapping.Length==2&&overlapping.Select(b=>b.Context.MaybeCaster).Distinct().Count()==2,"actual native overlapping aura keeps one independent marker per caster");
        component.HandleUnitExit(host.Context,area,enemy);Check(enemy.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("MarauderIMember01457")).Context.MaybeCaster==other,"leaving first caster aura preserves second caster native marker");
        marker=enemy.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("MarauderIMember01457"));roll057=2;marker.TickMechanics();Check(enemy.Buffs.Enumerable.Any(b=>b.Blueprint==confused&&b.Context.MaybeCaster==other),"remaining caster's native owner-round save still produces its own confusion");
        var replaced=CastAura057(c,caster,"II");Check(host.IsDisposed&&!second.IsDisposed&&caster.Buffs.Enumerable.Count(b=>b.Blueprint.name.Contains("Aura01457"))==1,"switching own aura replaces only own previous native aura and preserves other caster");
        component.HandleUnitExit(second.Context,otherArea,enemy);second.Remove();replaced.Remove();foreach(var b in enemy.Buffs.Enumerable.Where(b=>b.Blueprint==confused).ToArray())b.Remove();
        Enemies057.Clear();
    }
}
