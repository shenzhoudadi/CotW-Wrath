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
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicCrushTests
{
    private static int count;
    private static bool willPassed,fortPassed;
    private static readonly List<RuleSavingThrow> saves=new List<RuleSavingThrow>();
    private static readonly List<ContextActionDealDamage> damage=new List<ContextActionDealDamage>();
    private static readonly List<ContextActionApplyBuff> buffs=new List<ContextActionApplyBuff>();
    private static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    private static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    private static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    private static T Asset<T>(BlueprintsCache c,string token) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicAmplificationComponent).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
    private static void Check(bool ok,string why){if(!ok)throw new Exception(why);count++;}
    private static bool Create(ContextActionSavingThrow __instance,int dc,bool persistentSpell,ref RuleSavingThrow __result){__result=Empty<RuleSavingThrow>();Set(__result,"Type",__instance.Type);Set(__result,"DifficultyClassBase",dc);__result.PersistentSpell=persistentSpell;return false;}
    private static bool Trigger(RuleSavingThrow rule,ref RuleSavingThrow __result){saves.Add(rule);__result=rule;return false;}
    private static bool Passed(RuleSavingThrow __instance,ref bool __result){__result=__instance.Type==SavingThrowType.Will?willPassed:fortPassed;return false;}
    private static bool Damage(ContextActionDealDamage __instance){damage.Add(__instance);return false;}
    private static bool Buff(ContextActionApplyBuff __instance){buffs.Add(__instance);return false;}
    private static int resolvedDamage;
    private static T RuleBoundary<T>(T rule) {
        if(rule is RuleCalculateDamage calc)calc.CalculatedDamage.Add(new DamageValue(calc.DamageBundle.First(),resolvedDamage,resolvedDamage,resolvedDamage,1));
        return rule;
    }
    private static bool NoTargetBlock(ref bool __result){__result=false;return false;}
    private static void NoHitVisual(RuleDealDamage rule){}
    private static int IdentityDamage(RuleDealDamage rule,int amount)=>amount;
    private static IEnumerable<CodeInstruction> IdentityDifficulty(IEnumerable<CodeInstruction> instructions) {
        foreach(var code in instructions) {
            if(code.operand as MethodInfo==AccessTools.Method(typeof(RuleDealDamage),"ApplyDifficultyModifiers")) {
                code.opcode=OpCodes.Call;code.operand=AccessTools.Method(typeof(PsychicCrushTests),nameof(IdentityDamage));
            }
            if(code.operand as MethodInfo==AccessTools.Method(typeof(Kingmaker.Visual.HitSystem.HitPlayer),"PlayDamageHit")) {
                code.opcode=OpCodes.Call;code.operand=AccessTools.Method(typeof(PsychicCrushTests),nameof(NoHitVisual));
            }
            if(code.operand is MethodInfo method && method.DeclaringType==typeof(Rulebook) && method.Name=="Trigger" && method.IsGenericMethod) {
                code.opcode=OpCodes.Call;code.operand=AccessTools.Method(typeof(PsychicCrushTests),nameof(RuleBoundary)).MakeGenericMethod(method.GetGenericArguments());
            }
            yield return code;
        }
    }
    private static int NativeCap(BlueprintsCache cache)
    {
        int before=count;var h=new Harmony("CotW.PsychicCrush.NativeDamageCap");
        void Patch(MethodBase m,string n)=>h.Patch(m,prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicCrushTests),n)) {priority=Priority.First});
        Patch(AccessTools.Method(typeof(Kingmaker.UnitLogic.Commands.Base.UnitCommand),"CommandTargetUntargetable"),nameof(NoTargetBlock));
        h.Patch(AccessTools.Method(typeof(RuleDealDamage),"OnTrigger"),transpiler:new HarmonyMethod(AccessTools.Method(typeof(PsychicCrushTests),nameof(IdentityDifficulty))));
        Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),nameof(Damage));
        try {
            var caster=new Psychic070Tests.Caster(cache);var target=new Psychic070Tests.Caster(cache);
            var hp=Empty<ModifiableValueHitPoints>();var temp=Empty<ModifiableValueTemporaryHitPoints>();
            Set(target.Descriptor.Stats,"HitPoints",hp);Set(target.Descriptor.Stats,"TemporaryHitPoints",temp);
            var ctx=Empty<MechanicsContext>();Set(ctx,"m_CasterRef",Get(caster.Cast(SpellSchool.Necromancy,SpellDescriptor.MindAffecting,5).Context,"m_CasterRef"));
            foreach(int health in new[]{100,51,50,49,1,0})foreach(int temporary in new[]{0,35})foreach(bool collapse in new[]{false,true}) {
                Set(hp,"<ModifiedValue>k__BackingField",100);Set(temp,"<ModifiedValue>k__BackingField",temporary);target.Descriptor.Damage=100-health;
                ctx.SetParams(new AbilityParams {CasterLevel=20,DC=25});damage.Clear();
                using(ctx.GetDataScope(target.Unit))new PsychicCrushDamage {DiceCount=11,Collapse=collapse}.RunAction();
                var native=damage.Single();resolvedDamage=collapse?native.Value.BonusValue.Calculate(ctx):1000;
                var rule=new RuleDealDamage(caster.Unit,target.Unit,new DirectDamage(new DiceFormula(0,DiceType.Zero),resolvedDamage)) {MinHPAfterDamage=native.MinHPAfterDamage,IsFake=true};
                rule.Reason=Empty<RuleReason>();Set(rule.Reason,"<Context>k__BackingField",ctx);
                rule.OnTrigger(null);
                Check(rule.Result==health+temporary+1 && health-Math.Max(0,rule.Result-temporary)==-1,"Actual native RuleDealDamage cap stops at -1 for wounded/full and temporary-HP targets");
                Check(target.Descriptor.Damage==100-health,"IsFake cap audit does not mutate the target; no real hit/death lifecycle claimed");
            }
        } finally{h.UnpatchAll(h.Id);}
        return count-before;
    }
    internal static int Run(BlueprintsCache cache)
    {
        var h=new Harmony("CotW.PsychicCrush.NativeBranches");
        void Patch(MethodBase m,string n)=>h.Patch(m,prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicCrushTests),n)) {priority=Priority.First});
        // Native saving-throw actions, conditional DCs, nested SavingThrowData,
        // health conditions and conditional branches execute. Dice/outcomes and
        // final damage/buff dispatch are explicit external boundaries.
        Patch(AccessTools.Method(typeof(ContextActionSavingThrow),"CreateSavingThrow"),nameof(Create));
        PsychicRuleTestBoundary.Patch(h,typeof(RuleSavingThrow),AccessTools.Method(typeof(PsychicCrushTests),nameof(Trigger)));
        Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Passed));
        Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),nameof(Damage));
        Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(Buff));
        try {
            var caster=new Psychic070Tests.Caster(cache);var target=new Psychic070Tests.Caster(cache);
            var hp=Empty<ModifiableValueHitPoints>();var temp=Empty<ModifiableValueTemporaryHitPoints>();
            Set(target.Descriptor.Stats,"HitPoints",hp);Set(target.Descriptor.Stats,"TemporaryHitPoints",temp);
            var ctx=Empty<MechanicsContext>();Set(ctx,"m_CasterRef",Get(caster.Cast(SpellSchool.Necromancy,SpellDescriptor.MindAffecting,5).Context,"m_CasterRef"));
            foreach(int tier in new[]{1,2,3,4,5}) {
                var bp=Asset<BlueprintAbility>(cache,"PsychicCrush"+tier);var run=bp.GetComponent<AbilityEffectRunAction>();var family=bp.GetComponent<PsychicUndercastFamily>();
                Check(bp.GetComponent<SpellListComponent>().SpellLevel==tier+4 && family.Circle==tier+4 && family.LowerSpells.Length==tier-1 && family.Family=="PsychicCrush","Tier/circle and free lower family links");
                Check(bp.SpellResistance && bp.Range==AbilityRange.Close && bp.GetComponent<SpellComponent>().School==SpellSchool.Necromancy && (SpellDescriptor)bp.GetComponent<SpellDescriptorComponent>().Descriptor==SpellDescriptor.MindAffecting,"Native school, range, SR and mind-affecting immunity routing");
                Check(run.SavingThrowType==SavingThrowType.Unknown && run.Actions.Actions.Length==1 && run.Actions.Actions[0] is ContextActionSavingThrow,"No duplicate outer Will save");
                foreach(var health in new[]{new[]{100,100},new[]{100,99},new[]{100,51},new[]{100,50},new[]{100,49},new[]{101,51},new[]{101,50},new[]{100,1},new[]{100,0},new[]{100,-1},new[]{100,-5}})
                foreach(int tempHP in new[]{0,35})foreach(int cl in new[]{10,20})foreach(bool will in new[]{false,true})foreach(bool fort in new[]{false,true}) {
                    int maximum=health[0],current=health[1];bool below=current*2<maximum;
                    Set(hp,"<ModifiedValue>k__BackingField",maximum);Set(temp,"<ModifiedValue>k__BackingField",tempHP);target.Descriptor.Damage=maximum-current;
                    ctx.SetParams(new AbilityParams {CasterLevel=cl,DC=25,SpellLevel=tier+4});
                    saves.Clear();damage.Clear();buffs.Clear();willPassed=will;fortPassed=fort;
                    using(ctx.GetDataScope(target.Unit))run.Actions.Run();
                    bool needsFort=!will && !(tier>=4&&below),collapse=!will&&(!needsFort||!fort);
                    int bonus=tier==1?(current*2>maximum?4:0):tier==2?(current==maximum?4:current*2>maximum?2:0):tier==3&&current==maximum?2:0;
                    Check(saves.Count==(needsFort?2:1) && saves[0].Type==SavingThrowType.Will && saves[0].DifficultyClassBase==25+(tier==5&&below?2:0),"Correct single Will save and V's strictly-below-half modifier");
                    Check(!needsFort || saves[1].Type==SavingThrowType.Fortitude && saves[1].DifficultyClassBase==25-bonus,"Native Fortitude gets exact HP-based bonus; half HP is not below half");
                    Check(buffs.Count==(collapse?0:1) && buffs.All(b=>b.Buff.AssetGuid.ToString()=="4e42460798665fd4cb9173ffa7ada323" && b.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds(6)),"Successful Will/Fortitude causes exactly one round sickened; collapse does not");
                    bool deals=current>-1 && (!will||tier>=3);
                    Check(damage.Count==(deals?1:0),"I/II successful Will causes no damage; targets below -1 are never healed or hurt further");
                    if(!deals)continue;
                    var d=damage.Single();int expectedDice=collapse||will&&tier==3?0:2*tier+1;
                    Check(d.Value.DiceCountValue.Calculate(ctx)==expectedDice && d.Value.BonusValue.Calculate(ctx)==(collapse?current+tempHP+1:cl) && d.Value.DiceType==(expectedDice==0?DiceType.Zero:DiceType.D6),"Correct III Will damage and V 11d6 on both successful saves");
                    Check(d.UseMinHPAfterDamage && d.MinHPAfterDamage==maximum-current-1 && d.IgnoreCritical && d.DisableSneakDamage && !d.HalfIfSaved && d.DamageType.Type==DamageType.Direct,"Injured-target native damage cap is offset by existing damage");
                }
                Set(hp,"<ModifiedValue>k__BackingField",100);Set(temp,"<ModifiedValue>k__BackingField",0);target.Descriptor.Damage=0;
                ctx.SetParams(new AbilityParams {CasterLevel=20,DC=25,Metamagic=Metamagic.Persistent});saves.Clear();damage.Clear();buffs.Clear();willPassed=true;
                using(ctx.GetDataScope(target.Unit))run.Actions.Run();
                Check(saves.Count==2 && !saves[0].PersistentSpell && saves[1].PersistentSpell,"Native Persistent rerolls the successful first save");
            }
        } finally{h.UnpatchAll(h.Id);}
        int capChecks=NativeCap(cache);
        PsychicExpansionTests.Inventory["crushNativeCapAssertions"]=capChecks;
        PsychicExpansionTests.Inventory["crushSpellAssertions"]=count;
        Console.WriteLine("PASS: "+count+" Psychic Crush managed branches; damage/buff and random roll boundaries substituted.");return count;
    }
}
