using System;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;
internal static class PsychicSecondWindTests
{
    static int count;
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static void Check(bool b,string why){if(!b)throw new Exception("Second Wind: "+why);count++;}
    internal static int Run(BlueprintsCache cache)
    {
        var f=new Psychic070Tests.Caster(cache);var id=(BlueprintGuid)AccessTools.Method(typeof(PsychicSecondWindRequirement).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{"SecondWind"});
        var spell=(BlueprintAbility)cache.Load(id);var checker=spell.GetComponent<PsychicSecondWindRequirement>();var hp=Empty<ModifiableValueHitPoints>();Set(f.Unit.Stats,"HitPoints",hp);
        Check(spell.ActionType==UnitCommand.CommandType.Swift&&spell.Range==AbilityRange.Personal&&spell.CanTargetSelf&&!spell.CanTargetFriends&&!spell.CanTargetEnemies,"Swift personal healing");
        Check(spell.School==SpellSchool.Conjuration&&!spell.SpellResistance,"Conjuration without SR");
        Check(spell.AvailableMetamagic==(Metamagic.Empower|Metamagic.Maximize|Metamagic.Heighten),"CotW healing metamagic choices");
        var lists=spell.GetComponents<SpellListComponent>().ToArray();Check(lists.Length==4&&lists.All(l=>l.SpellLevel==3),"Cleric Paladin Inquisitor Ranger3");
        foreach(var l in lists)Check(l.SpellList.SpellsByLevel[3].Spells.Count(s=>s==spell)==1,"Unique learned root");
        Check(!lists.Any(l=>l.SpellList==f.Book.Blueprint.SpellList),"No invented Psychic list entry");
        Check(!checker.IsCasterRestrictionPassed(null),"Missing caster rejected");
        foreach(int maximum in new[]{1,3,4,99,100,101,1000})foreach(int current in new[]{-1,0,1,maximum/4-1,maximum/4,maximum/4+1,maximum}){
            Set(hp,"<ModifiedValue>k__BackingField",maximum);f.Descriptor.Damage=maximum-current;
            Check(checker.IsCasterRestrictionPassed(f.Unit)==(current<(decimal)maximum/4),"Strict quarter threshold including non-divisible maximum");
        }
        Set(hp,"<ModifiedValue>k__BackingField",0);Check(!checker.IsCasterRestrictionPassed(f.Unit),"Invalid maximum rejected");
        Set(hp,"<ModifiedValue>k__BackingField",100);
        var ctx=f.Cast(SpellSchool.Conjuration,SpellDescriptor.Cure|SpellDescriptor.RestoreHP,3).Context;Set(ctx,"AssociatedBlueprint",spell);Set(ctx,"<ClickedTarget>k__BackingField",new TargetWrapper(f.Unit));
        var heal=spell.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionHealTarget>().Single();
        var harmony=new Harmony("CotW.SecondWind.Managed");harmony.Patch(AccessTools.Method(typeof(RuleHealDamage),"Roll"),prefix:new HarmonyMethod(typeof(PsychicSecondWindTests),nameof(Roll)));
        harmony.Patch(AccessTools.Method(typeof(ShadowSpellLogicExtension),"SkipBecauseOfShadow"),prefix:new HarmonyMethod(typeof(PsychicSecondWindTests),nameof(NotShadow)));
        var healTarget=AccessTools.Method(typeof(RuleHealDamage),"OnTrigger");
        var capture=AccessTools.Method(typeof(Psychic070Tests),"CaptureHealing");var captureOwner=Harmony.GetPatchInfo(healTarget).Prefixes.Single(p=>p.PatchMethod==capture).owner;
        harmony.Unpatch(healTarget,capture);
        try{
            foreach(int cl in new[]{1,5,9,10,11,20,30})foreach(var mm in new[]{(Metamagic)0,Metamagic.Empower,Metamagic.Maximize,Metamagic.Empower|Metamagic.Maximize}){
                ctx.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=mm});Set(ctx,"m_Ranks",Enumerable.Repeat(spell.GetComponent<ContextRankConfig>().GetValue(ctx),32).ToArray());
                RuleHealDamage rule;using(ctx.GetDataScope(f.Unit))rule=heal.GetHealRule(f.Unit,f.Unit);
                Check(rule.HealFormula.ModifiedValue.Rolls==2&&rule.HealFormula.ModifiedValue.Dice==DiceType.D8&&rule.Bonus==Math.Min(cl,10),"Native heal rule uses 2d8 and capped CL");
                Check((bool)rule.Empower==mm.HasFlag(Metamagic.Empower),"Native Empower propagation");
                Check((DamageCalculationType)rule.CalculationType==(mm.HasFlag(Metamagic.Maximize)?DamageCalculationType.Maximized:DamageCalculationType.Normal),"Native Maximize propagation");
                // Seed only the dice boundary; execute native heal/cap/HP update.
                f.Descriptor.Damage=90;rule.OnTrigger(null);int expected=(int)((8+Math.Min(cl,10))*(mm.HasFlag(Metamagic.Empower)?1.5:1));
                Check(rule.Value==expected&&f.Unit.HPLeft==10+expected,"Native healing updates HP with Empower and CL cap");
                using(ctx.GetDataScope(f.Unit))rule=heal.GetHealRule(f.Unit,f.Unit);f.Descriptor.Damage=2;rule.OnTrigger(null);Check(rule.Value==2&&f.Unit.HPLeft==100,"Native over-healing cap");
            }
        }finally{harmony.UnpatchAll(harmony.Id);new Harmony(captureOwner).Patch(healTarget,prefix:new HarmonyMethod(capture));}
        PsychicExpansionTests.Inventory["secondWindAssertions"]=count;Console.WriteLine("PASS: "+count+" Second Wind checks: HP threshold boundaries, lists/targets, native rank/metamagic/heal rule and HP update; seeded dice, constructed entities, not gameplay.");return count;
    }
    static bool NotShadow(ref bool __result){__result=false;return false;}
    static bool Roll(ref int __result){__result=8;return false;}
}
