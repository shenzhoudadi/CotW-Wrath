using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static readonly List<RuleSavingThrow> Saves058=new List<RuleSavingThrow>();
    static void ObserveSave058(RuleSavingThrow __instance)=>Saves058.Add(__instance);
    static int WarpedCount058()=>Saves058.Count(v=>v.Reason?.Context?.AssociatedBlueprint?.AssetGuid==Id("WarpedBrainFeature01458"));
    static TEvent Rule058<TEvent>(TEvent e)where TEvent:RulebookEvent=>(TEvent)AccessTools.Method(typeof(BardMasterpieces01435Tests),"NativeRule").MakeGenericMethod(typeof(TEvent)).Invoke(null,new object[]{e});
    static AbilityExecutionContext Context058(UnitEntityData source,UnitEntityData target,BlueprintAbility spell,int spellLevel=1)
    {
        var book=(Spellbook)H("Book",source,N<BlueprintCharacterClass>(active,Psychic058));var a=new AbilityData(spell,book,spellLevel);var ctx=new AbilityExecutionContext(a,new AbilityParams{CasterLevel=source.Progression.Classes[0].Level,SpellLevel=spellLevel,DC=20},new TargetWrapper(target),new RulebookEventContext(),source);ctx.FxSpawners=spell.GetComponents<AbilitySpawnFx>().ToList();return ctx;
    }
    static void NativeApply058(AbilityExecutionContext context,UnitEntityData target)
    {
        var method=AccessTools.Method(typeof(AbilityExecutionProcess),"DoApplyEffect",new[]{typeof(AbilityExecutionContext),typeof(TargetWrapper),typeof(AbilityApplyEffect)});
        method.Invoke(null,new object[]{context,new TargetWrapper(target),context.AbilityBlueprint.GetComponent<AbilityApplyEffect>()});
    }
    static void DisciplineEffects058(BlueprintsCache c,Harmony h)
    {
        h.Patch(AccessTools.Method(typeof(UnitEntityData),"IsEnemy",new[]{typeof(UnitEntityData)}),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Enemy057)){priority=Priority.First});
        h.Patch(AccessTools.Method(typeof(RuleSavingThrow),"OnTrigger"),postfix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Save057)));
        Saves058.Clear();h.Patch(AccessTools.Method(typeof(RuleSavingThrow),"OnTrigger"),postfix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(ObserveSave058)));
        h.Patch(AccessTools.Method(typeof(AbilityExecutionProcess),"DoApplyEffect",new[]{typeof(AbilityExecutionContext),typeof(TargetWrapper),typeof(AbilityApplyEffect)}),transpiler:new HarmonyMethod(typeof(Archetypes01445Tests),"NestedRules"));
        h.Patch(AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnFx),"RunAction"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(SceneFx058)));
        h.Patch(AccessTools.Method(typeof(AbilitySpawnFx),"Spawn"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(SceneFx058)));
        foreach(string method in new[]{"ApplyDifficultyBeforeCalculated","ApplyDifficultyForDamageReduction"})h.Patch(AccessTools.Method(typeof(RuleCalculateDamage),method),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(SceneFx058)));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Settings.SettingsRoot),"Difficulty"),prefix:new HarmonyMethod(typeof(IncreasedDamageReduction01439Tests),"NeutralDifficulty"));
        h.Patch(AccessTools.Method(typeof(ShadowSpellLogicExtension),"TryApplyShadowDamageFactor",new[]{typeof(RulebookEvent),typeof(UnitEntityData),typeof(int)}),prefix:new HarmonyMethod(typeof(IncreasedDamageReduction01439Tests),"NoShadow"));
        foreach(var type in new[]{typeof(RuleDealDamage),typeof(RuleCalculateDamage)})h.Patch(AccessTools.Method(type,"OnTrigger"),transpiler:new HarmonyMethod(typeof(Archetypes01445Tests),"NestedRules"));
        h.Patch(AccessTools.Method(typeof(RuleDealDamage),"OnTrigger"),transpiler:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(DamageScene058)));
        Warped058(c,h);Console.WriteLine("058 effects: Warped Brain native publisher and immunity cases passed.");Hallucinations058(c,h);Console.WriteLine("058 effects: Hallucinogenic native entry and cooldown cases passed.");DarkHalfReturn058(c,h);Console.WriteLine("058 effects: Dark Half return native checks passed.");PainDamage058(c,h);Agonizing058(c,h);Synaptic058(c,h);DarkHalfCombat058(c,h);
        PsychicExpansionTests.Inventory["psychicDisciplineEffects01458Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["coverage"]=new JArray(Coverage),["gameLaunchTested"]=false,["boundary"]="Native spell application notification before immunity, actual native area entry/round/save/buff and owner-specific cooldowns, rule event bus/damage and paid supernatural action execution. RNG/faction/scene inputs bounded; rendered combat and save transport remain game observations."};
    }
    static bool SceneFx058()=>false;
    static IEnumerable<CodeInstruction> DamageScene058(IEnumerable<CodeInstruction> input)
    {
        foreach(var c in input){if(c.operand as System.Reflection.MethodInfo==AccessTools.Method(typeof(RuleDealDamage),"ApplyDifficultyModifiers")){c.opcode=System.Reflection.Emit.OpCodes.Call;c.operand=AccessTools.Method(typeof(Invoker01437Tests),"NeutralDamage");}if(c.operand as System.Reflection.MethodInfo==AccessTools.Method(typeof(Kingmaker.Visual.HitSystem.HitPlayer),"PlayDamageHit")){c.opcode=System.Reflection.Emit.OpCodes.Call;c.operand=AccessTools.Method(typeof(Invoker01437Tests),"NoHitVisual");}yield return c;}
    }
    static void Warped058(BlueprintsCache c,Harmony h)
    {
        var owner=EquipmentUnit(c,20,Psychic058);var enemy=EquipmentUnit(c,20,Psychic058);var ally=EquipmentUnit(c,20,Psychic058);Enemies057.Add(enemy);enemy.Stats.Wisdom.BaseValue=10;enemy.Stats.SaveWill.BaseValue=0;Grant(owner,c,"WarpedBrainFeature01458");Grant(owner,c,"MarauderUnreal01457");
        var daze=N<BlueprintAbility>(c,"55f14bc84d7c85446b07a1b5dd6b2b4c");var nausea=A<BlueprintBuff>(c,"PsychicNauseated01458");
        D20Input058=1;int before=WarpedCount058();var cast=Context058(enemy,owner,daze);NativeApply058(cast,owner);
        Console.WriteLine("058 Warped diagnostics saves="+(saveCount057-before)+" dc="+lastSave057?.DifficultyClass+" passed="+lastSave057?.IsPassed+" tags="+cast.SpellDescriptor+" enemy="+enemy.IsEnemy(owner)+" ownerBuffs="+string.Join(";",owner.Buffs.Enumerable.Select(v=>v.Blueprint.name))+" enemyBuffs="+string.Join(";",enemy.Buffs.Enumerable.Select(v=>v.Blueprint.name)));
        var save=Saves058.Last(v=>v.Reason?.Context?.AssociatedBlueprint?.AssetGuid==Id("WarpedBrainFeature01458"));Check(WarpedCount058()==before+1&&save.DifficultyClass==25&&!save.IsPassed,"actual native spell-apply pre-effect publisher triggers Warped Brain save with Psychic/Intelligence DC");
        var effect=enemy.Buffs.Enumerable.Single(v=>v.Blueprint==nausea);Check(effect.Context.MaybeCaster==owner&&effect.TimeLeft==TimeSpan.FromSeconds(6)&&enemy.State.HasCondition(UnitCondition.Nauseated),"native Warped Brain failure creates one source-owned nonpoison one-round nausea");
        Check(!owner.State.HasCondition(UnitCondition.Dazed),"original spell native mind immunity does not suppress pre-application Warped retaliation");
        NativeApply058(cast,owner);Check(WarpedCount058()==before+1,"same native cast/application notification cannot repeat Warped retaliation");effect.Remove();
        D20Input058=20;NativeApply058(Context058(enemy,owner,daze),owner);save=Saves058.Last(v=>v.Reason?.Context?.AssociatedBlueprint?.AssetGuid==Id("WarpedBrainFeature01458"));Check(save.IsPassed&&!enemy.Buffs.Enumerable.Any(v=>v.Blueprint==nausea),"native Warped Brain successful save prevents nausea");
        before=WarpedCount058();D20Input058=1;NativeApply058(Context058(ally,owner,daze),owner);NativeApply058(Context058(owner,owner,daze),owner);
        if(Main.GetType("CallOfTheWild.Wrath.Prototype.PsychicWilling01460")!=null)Check(WarpedCount058()==before+1,"060 actual native harmful allied spell retaliates once while self application remains exempt");
        else Check(WarpedCount058()==before,"actual native allied/self application notifications never retaliate");
        Check((nausea.GetComponent<SpellDescriptorComponent>().Descriptor.Value&SpellDescriptor.Poison)==0,"private psychic nausea does not acquire native poison immunity restrictions");
    }
    static void Hallucinations058(BlueprintsCache c,Harmony h)
    {
        var caster=EquipmentUnit(c,13,Psychic058);var second=EquipmentUnit(c,13,Psychic058);var enemy=EquipmentUnit(c,13);var ally=EquipmentUnit(c,13);Enemies057.Add(enemy);enemy.Stats.Wisdom.BaseValue=10;enemy.Stats.SaveWill.BaseValue=0;
        var fact=Grant(caster,c,"HallucinogenicAuraFeature01458");var other=Grant(second,c,"HallucinogenicAuraFeature01458");var bp=A<BlueprintAbilityAreaEffect>(c,"HallucinogenicAuraArea01458");var run=bp.GetComponent<AbilityAreaEffectRunAction>();
        var area=(AreaEffectEntityData)AccessTools.Method(typeof(TimeStutter01437Tests),"MakeArea").Invoke(null,new object[]{"058-hallucination",fact.MaybeContext,bp,new[]{enemy,ally,caster}});
        var cooldown=A<BlueprintBuff>(c,"HallucinogenicAuraCooldown01458");var confusion=A<BlueprintBuff>(c,"HallucinogenicAuraConfusion01458");
        int before=saveCount057;run.HandleUnitEnter(fact.MaybeContext,area,ally);run.HandleUnitEnter(fact.MaybeContext,area,caster);Check(saveCount057==before,"native Hallucinogenic Aura entry excludes allies and self before saves");
        D20Input058=20;run.HandleUnitEnter(fact.MaybeContext,area,enemy);var immune=enemy.Buffs.Enumerable.Single(v=>v.Blueprint==cooldown&&v.Context.MaybeCaster==caster);Check(lastSave057.IsPassed&&immune.TimeLeft==TimeSpan.FromDays(1)&&!enemy.Buffs.Enumerable.Any(v=>v.Blueprint==confusion),"native successful aura save creates source-specific24hour immunity");
        before=saveCount057;for(int i=0;i<8;i++)run.HandleUnitEnter(fact.MaybeContext,area,enemy);Check(saveCount057==before,"repeated native aura entries do not reroll inside source immunity");
        D20Input058=1;var area2=(AreaEffectEntityData)AccessTools.Method(typeof(TimeStutter01437Tests),"MakeArea").Invoke(null,new object[]{"058-hallucination-other",other.MaybeContext,bp,new[]{enemy}});run.HandleUnitEnter(other.MaybeContext,area2,enemy);
        var affected=enemy.Buffs.Enumerable.Single(v=>v.Blueprint==confusion&&v.Context.MaybeCaster==second);Check(!lastSave057.IsPassed&&affected.TimeLeft==TimeSpan.FromSeconds(12)&&enemy.State.HasCondition(UnitCondition.Confusion),"another caster ignores only first caster immunity and applies native d4 confusion");
        before=saveCount057;run.HandleUnitEnter(other.MaybeContext,area2,enemy);Check(saveCount057==before,"the source's existing confusion prevents repeated entry saves");
        var clock=Game.Instance.Player.GameTime;Game.Instance.Player.GameTime+=TimeSpan.FromSeconds(5);affected.Remove();var immunity2=enemy.Buffs.Enumerable.Single(v=>v.Blueprint==cooldown&&v.Context.MaybeCaster==second);Check(immunity2.EndTime==Game.Instance.Player.GameTime+TimeSpan.FromDays(1),"native confusion deactivation starts second caster immunity when confusion actually ends");
        Game.Instance.Player.GameTime=immune.EndTime-TimeSpan.FromTicks(1);enemy.Buffs.TickBuff(immune,Game.Instance.Player.GameTime);Check(immune.IsAttached,"native aura immunity remains until exact24hour boundary");Game.Instance.Player.GameTime=immune.EndTime;enemy.Buffs.TickBuff(immune,immune.EndTime);Check(!immune.IsAttached&&immunity2.IsAttached,"native24hour boundary expires only the first caster's cooldown");Game.Instance.Player.GameTime=clock;
        var guard=Grant(enemy,c,"MarauderCracked01457");before=saveCount057;run.HandleUnitEnter(fact.MaybeContext,area,enemy);Check(saveCount057==before+1&&!enemy.Buffs.Enumerable.Any(v=>v.Blueprint==confusion&&v.Context.MaybeCaster==caster),"actual native mind immunity prevents confusion rather than being bypassed by aura");enemy.Facts.Remove(guard);
    }
    static void DarkHalfReturn058(BlueprintsCache c,Harmony h)
    {
        var owner=EquipmentUnit(c,13,Psychic058);owner.Stats.Intelligence.BaseValue=10;Grant(owner,c,"DarkHalfFeature01458");Super058(owner,A<BlueprintAbility>(c,"DarkHalfAbility01458"));var main=owner.Buffs.Enumerable.Single(v=>v.Blueprint.AssetGuid==Id("DarkHalfBuff01458"));var resource=A<BlueprintAbilityResource>(c,"DarkHalfResource01458");var leave=A<BlueprintAbility>(c,"DarkHalfReturnAbility01458");
        D20Input058=1;Super058(owner,leave);var runtime=main.Components.Single(v=>v.SourceBlueprintComponent is DarkHalf01458);var data=runtime.GetData<DarkHalfState01458>();Check(main.IsAttached&&data.Returning&&data.AttemptAfter==Game.Instance.Player.GameTime+TimeSpan.FromSeconds(6),"actual native Intelligence return failure retains Dark Half and schedules next-round attempt");
        Check(!leave.ComponentsArray.OfType<IAbilityCasterRestriction>().All(v=>v.IsCasterRestrictionPassed(owner)),"native return restriction rejects another attempt within same round");var used=owner.Resources.GetResourceAmount(resource);Super058(owner,leave);Check(main.IsAttached&&owner.Resources.GetResourceAmount(resource)==used,"replayed return action cannot charge or reroll during native cooldown");
        Game.Instance.Player.GameTime+=TimeSpan.FromSeconds(6);D20Input058=20;using(runtime.RequestEventContext())((DarkHalf01458)runtime.SourceBlueprintComponent).OnNewRound();Check(!main.IsAttached&&owner.Resources.GetResourceAmount(resource)==used-1,"native maintained round pays once and actual Intelligence success ends returning form");
    }
    static RuleDealDamage Deal058(UnitEntityData caster,UnitEntityData target,AbilityExecutionContext context,int amount)
    {
        var rule=new RuleDealDamage(caster,target,new DirectDamage(new DiceFormula(0,DiceType.Zero),amount)){Reason=context,SourceAbility=context.AbilityBlueprint};return Rule058(rule);
    }
    static void PainDamage058(BlueprintsCache c,Harmony h)
    {
        foreach(int level in new[]{1,8}){
            var caster=EquipmentUnit(c,level,Psychic058);caster.Stats.Charisma.BaseValue=16;
            // Use the actual declared pool feature rather than manufacture a
            // resource row; its native grant/max/spend paths are unchanged.
            var poolFact=(string)AccessTools.Field(T("PsychicClass"),"CharismaFeatureId").GetRawConstantValue();if(!caster.HasFact(N<BlueprintFeature>(c,poolFact)))Grant(caster,N<BlueprintFeature>(c,poolFact));Grant(caster,c,"PainfulReminderFeature01458");
            var target=EquipmentUnit(c,level);target.Stats.HitPoints.BaseValue=1000;var spell=A<BlueprintAbility>(c,"MindThrust1");var original=Context058(caster,target,spell);var dealt=Deal058(caster,target,original,10);Check(dealt.Result==10&&target.Damage==10,"actual native direct spell damage executes and routes event bus at Psychic level "+level);
            var marker=target.Buffs.Enumerable.Single(v=>v.Blueprint.AssetGuid==Id("PainfulReminderMarker01458")&&v.Context.MaybeCaster==caster);Check(marker.TimeLeft==TimeSpan.FromSeconds(12),"native positive spell damage creates this caster's two-round reminder marker");
            var reminder=A<BlueprintAbility>(c,"PainfulReminderAbility01458");Check(reminder.GetComponent<Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetHasFact>().IsTargetRestrictionPassed(caster,new TargetWrapper(target)),"actual native reminder target checker accepts own recent spell damage");
            var pool=N<BlueprintAbilityResource>(c,(string)AccessTools.Field(T("PsychicClass"),"CharismaPoolId").GetRawConstantValue());caster.Resources.Spend(pool,1);int before=caster.Resources.GetResourceAmount(pool);Super058(caster,reminder,target);Check(target.Damage==10+(level==1?3:6)&&caster.Resources.GetResourceAmount(pool)==before+(level==1?0:1),"native reminder real damage threshold restores a phrenic point only at five or more: "+level);
        }
    }
    static void Agonizing058(BlueprintsCache c,Harmony h)
    {
        var owner=EquipmentUnit(c,13,Psychic058);var other=EquipmentUnit(c,13,Psychic058);Grant(owner,c,"AgonizingWoundFeature01458");Grant(other,c,"AgonizingWoundFeature01458");
        var resource=A<BlueprintAbilityResource>(c,"AgonizingWoundResource01458");var mark=A<BlueprintBuff>(c,"AgonizingWoundTarget01458");var choose=A<BlueprintAbility>(c,"AgonizingWoundTargetAbility01458");var spell=A<BlueprintAbility>(c,"MindThrust1");
        for(int mode=0;mode<4;mode++){
            var enemy=EquipmentUnit(c,13);enemy.Stats.HitPoints.BaseValue=1000;enemy.Stats.Wisdom.BaseValue=10;enemy.Stats.SaveWill.BaseValue=0;Enemies057.Add(enemy);
            var toggle=Toggle(owner,c,"AgonizingWound"+mode+"01458Toggle");ToggleOn(toggle);Super058(owner,choose,enemy);var selected=enemy.Buffs.Enumerable.Single(b=>b.Blueprint==mark&&b.Context.MaybeCaster==owner);Check(selected.TimeLeft==TimeSpan.FromSeconds(6),"native wound selection creates one-round source-owned marker: "+mode);
            int before=owner.Resources.GetResourceAmount(resource);int saves=saveCount057;D20Input058=20;var damage=Deal058(owner,enemy,Context058(owner,enemy,spell),1);
            Check(lastSave057.IsPassed&&saveCount057==saves+1&&selected.IsAttached&&owner.Resources.GetResourceAmount(resource)==before,"native wound successful save retains marked target and all daily uses: "+mode);
            RulebookEventBus.OnEventDidTrigger(damage);Check(saveCount057==saves+1&&owner.Resources.GetResourceAmount(resource)==before,"repeated native damage notification cannot reroll or spend on same wound event: "+mode);
            D20Input058=1;Deal058(owner,enemy,Context058(owner,enemy,spell),1);var comp=toggle.Blueprint.Buff.GetComponent<PsychicDamageLink01458>();var effect=enemy.Buffs.Enumerable.Single(b=>b.Blueprint==comp.Effect.Get()&&b.Context.MaybeCaster==owner);
            Check(!lastSave057.IsPassed&&!selected.IsAttached&&effect.TimeLeft==TimeSpan.FromSeconds(6)&&owner.Resources.GetResourceAmount(resource)==before-(mode<2?1:2),"native failed wound save applies actual selected condition and spends original cost once: "+mode);
            before=owner.Resources.GetResourceAmount(resource);saves=saveCount057;Deal058(owner,enemy,Context058(owner,enemy,spell),1);Check(saveCount057==saves&&owner.Resources.GetResourceAmount(resource)==before,"native positive damage without owned marker cannot trigger another wound: "+mode);effect.Remove();
            owner.Resources.Restore(resource,99);before=owner.Resources.GetResourceAmount(resource);Super058(owner,choose,enemy);saves=saveCount057;var empty=Rule058(new RuleDealDamage(owner,enemy,new DamageBundle()){Reason=Context058(owner,enemy,spell),SourceAbility=spell});Check(empty.Result==0&&saveCount057==saves&&owner.Resources.GetResourceAmount(resource)==before,"native empty damage bundle returns zero and cannot spend wound uses: "+mode);
            Super058(other,choose,enemy);ToggleOff(toggle);Check(!enemy.Buffs.Enumerable.Any(b=>b.Blueprint==mark&&b.Context.MaybeCaster==owner)&&enemy.Buffs.Enumerable.Any(b=>b.Blueprint==mark&&b.Context.MaybeCaster==other),"native mode-off cleans only its saved source-owned target references: "+mode);
        }
        var ally=EquipmentUnit(c,13);Super058(owner,choose,ally);Super058(owner,choose,owner);Check(!ally.Buffs.Enumerable.Any(b=>b.Blueprint==mark)&&!owner.Buffs.Enumerable.Any(b=>b.Blueprint==mark),"native wound action also rejects allied/self targets after UI restrictions");
    }
    static void Synaptic058(BlueprintsCache c,Harmony h)
    {
        if(Main.GetType("CallOfTheWild.Wrath.Prototype.SynapticDelay01460")!=null){Synaptic060(c,h);PsychicExpansionTests.Inventory["synapticTimingPolicy060"]=true;return;}
        var owner=EquipmentUnit(c,13,Psychic058);var enemy=EquipmentUnit(c,1);var another=EquipmentUnit(c,1);Enemies057.Add(enemy);Enemies057.Add(another);enemy.Stats.Wisdom.BaseValue=10;enemy.Stats.SaveWill.BaseValue=0;
        var pool=Pool058(c,owner);var book=Book058(c,owner);var spray=N<BlueprintAbility>(c,"91da41b9793a4624797921f221db653c");book.AddKnown(1,spray,false);var spell=book.GetKnownSpells(1).Single(v=>v.Blueprint==spray);Grant(owner,c,"SynapticShockFeature01458");var toggle=Toggle(owner,c,"SynapticShock01458Toggle");ToggleOn(toggle);
        var choose=A<BlueprintAbility>(c,"SynapticShockTargetAbility01458");var marker=A<BlueprintBuff>(c,"SynapticShockTarget01458");var confusion=A<BlueprintBuff>(c,"SynapticShockConfusion01458");Super058(owner,choose,enemy);int before=owner.Resources.GetResourceAmount(pool);
        D20Input058=20;var passed=Linked058(spell,enemy);ModeEvents058(passed,true);AbilityExecutionProcess.PrepareCast(passed.Context);passed.Context.FxSpawners=spray.GetComponents<AbilitySpawnFx>().ToList();NativeApply058(passed.Context,enemy);
        Check(lastSave057.IsPassed&&owner.Resources.GetResourceAmount(pool)==before&&!enemy.Buffs.Enumerable.Any(v=>v.Blueprint==confusion)&&enemy.Buffs.Enumerable.Any(v=>v.Blueprint==marker),"native saved area spell applies no effect and does not pay Synaptic or consume its target");
        D20Input058=1;var failed=Linked058(spell,enemy);ModeEvents058(failed,true);AbilityExecutionProcess.PrepareCast(failed.Context);failed.Context.FxSpawners=spray.GetComponents<AbilitySpawnFx>().ToList();NativeApply058(failed.Context,another);
        Check(owner.Resources.GetResourceAmount(pool)==before&&!another.Buffs.Enumerable.Any(v=>v.Blueprint==confusion),"native effect on another area victim cannot consume selected Synaptic reaction");NativeApply058(failed.Context,enemy);
        var result=enemy.Buffs.Enumerable.Single(v=>v.Blueprint==confusion&&v.Context.MaybeCaster==owner);Check(result.TimeLeft==TimeSpan.FromSeconds(6)&&owner.Resources.GetResourceAmount(pool)==before-1&&!enemy.Buffs.Enumerable.Any(v=>v.Blueprint==marker),"native applied Color Spray buff publisher pays once and creates one-round source-owned confusion");
        NativeApply058(failed.Context,enemy);Check(owner.Resources.GetResourceAmount(pool)==before-1&&enemy.Buffs.Enumerable.Count(v=>v.Blueprint==confusion)==1,"multiple native buffs and repeated notifications in one linked cast cannot multiply Synaptic payment/effect");
        Super058(owner,choose,another);ToggleOff(toggle);Check(!another.Buffs.Enumerable.Any(v=>v.Blueprint==marker),"native Synaptic mode-off removes only its selected marker");
    }
    static bool CombatInput058(ref bool __result){__result=true;return false;}
    static void DarkHalfCombat058(BlueprintsCache c,Harmony h)
    {
        var heal=AccessTools.Method(typeof(RuleHealDamage),nameof(RuleHealDamage.OnTrigger));var bounded=(Harmony.GetPatchInfo(heal)?.Prefixes.AsEnumerable()??Enumerable.Empty<Patch>()).Where(p=>p.PatchMethod.DeclaringType==typeof(Psychic070Tests)&&p.PatchMethod.Name=="CaptureHealing").ToArray();foreach(var p in bounded)h.Unpatch(heal,p.PatchMethod);
        var combat=AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.Combat.UnitCombatState),"IsInCombat");h.Patch(combat,prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(CombatInput058)));
        try{foreach(int level in new[]{1,5,13}){
            var owner=EquipmentUnit(c,level,Psychic058);var enemy=EquipmentUnit(c,level);enemy.Stats.HitPoints.BaseValue=1000;Enemies057.Add(enemy);var book=Book058(c,owner);var ability=Known058(c,book,1,"MindThrust1");int dc=ability.CalculateParams().DC;Grant(owner,c,"DarkHalfFeature01458");if(level>=5)Grant(owner,c,"MorphicFormFeature01458");
            Super058(owner,A<BlueprintAbility>(c,"DarkHalfAbility01458"));var active=owner.Buffs.Enumerable.Single(v=>v.Blueprint.AssetGuid==Id("DarkHalfBuff01458"));Check(ability.CalculateParams().DC==dc+1,"actual native ability-parameter event adds Dark Half DC only while active: "+level);
            var wizard=N<BlueprintCharacterClass>(c,"ba34257984f4c41408ce1dc2004e342e");var ordinary=new AbilityData(ability.Blueprint,new Spellbook(owner.Descriptor,wizard.Spellbook),1);int otherDC=ordinary.CalculateParams().DC;
            var result=Deal058(owner,enemy,Context058(owner,enemy,ability.Blueprint),10);var bleed=enemy.Buffs.Enumerable.Single(v=>v.Blueprint.AssetGuid==Id("DarkHalfBleedBuff01458"));Check(result.Result==10&&bleed.Context.MaybeCaster==owner,"actual positive spell damage applies source-owned native bleed: "+level);
            var runtime=bleed.Components.Single(v=>v.SourceBlueprintComponent is AddFactContextActions);int before=enemy.Damage;using(runtime.RequestEventContext())((Kingmaker.Controllers.Units.ITickEachRound)runtime.SourceBlueprintComponent).OnNewRound();Check(enemy.Damage==before+(level<5?1:level<13?2:3)&&bleed.IsAttached,"actual native bleed round action deals original flat progression in combat: "+level);
            Rule058(new RuleHealDamage(owner,enemy,1){Reason=bleed.Context});Check(!bleed.IsAttached,"actual native healing rule and AddHealTrigger terminate private Dark Half bleed: "+level);
            if(level>=5){var form=owner.Buffs.Enumerable.Single(v=>v.Blueprint.name.Contains("MorphicForm"));Check(form.Blueprint.AssetGuid==Id("MorphicForm1Buff01458"),"controlled legal native d3 input selects original cold-iron-bypassed DR form");var part=owner.Get<UnitPartDamageReduction>();Console.WriteLine("058 DR before native part TurnOn: "+part.IsTurnedOn+"/"+part.IsSubscribedOnEventBus);part.TurnOn();Check(part.IsSubscribedOnEventBus,"actual native DR part TurnOn subscribes its scene lifecycle before damage: "+level);var physical=new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(0,DiceType.Zero)),10,Kingmaker.Enums.Damage.PhysicalDamageForm.Slashing);var hit=Rule058(new RuleDealDamage(enemy,owner,physical){Reason=Context058(enemy,owner,ability.Blueprint)});Console.WriteLine("058 DR actual result="+hit.Result+" sources="+part.AllSources.Count());Check(hit.Result==5,"actual native physical damage calculation applies Morphic DR5: "+level);}
            active.Remove();Check(ability.CalculateParams().DC==dc&&ordinary.CalculateParams().DC==otherDC,"native form removal restores own spell DC and never changes other spellbook DC: "+level);
        }}finally{h.Unpatch(combat,AccessTools.Method(typeof(MagusBatch01454Tests),nameof(CombatInput058)));foreach(var p in bounded)new Harmony(p.owner).Patch(heal,prefix:new HarmonyMethod(p.PatchMethod){priority=p.priority,before=p.before,after=p.after});}
    }
}
