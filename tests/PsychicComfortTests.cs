using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicComfortTests
{
    static int count,applied,activations,deactivations;static bool passed;static EntityFactsManager facts;
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception("Comfort: "+why);count++;}
    static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicForceSword).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
    static EntityFactComponent Runtime(BlueprintComponent c,UnitFact f){var r=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);r.Setup(c);r.RestoreOwnerLink(f);return r;}
    static Buff Fact(BlueprintBuff bp,MechanicsContext ctx){var f=Empty<Buff>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"m_Context",ctx);Set(f,"<Rank>k__BackingField",1);Set(f,"<IsActive>k__BackingField",true);f.RestoreOwnerLink(facts);return f;}
    static void Patch(Harmony h,MethodBase m,string name)=>h.Patch(m,prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicComfortTests),name)));
    static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
    internal static int Run(BlueprintsCache cache)
    {
        var f=new Psychic070Tests.Caster(cache);facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(f.Unit,"<Buffs>k__BackingField",buffs);
        var h=new Harmony("CotW.Comfort.NativeBoundaries");
        try {
            Patch(h,AccessTools.PropertyGetter(typeof(MechanicsContext),"SavingThrow"),nameof(Saving));Patch(h,AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Passed));
            Patch(h,AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(Apply));
            Blueprints(cache,f);Suppression(cache,f,h);Charge(cache,f,h);Penalties(cache,f);
        }finally{h.UnpatchAll(h.Id);}
        PsychicExpansionTests.Inventory["comfortSpellAssertions"]=count;
        Console.WriteLine("PASS: "+count+" Invigorate/Howling Agony checks; native suppression registry/update/release and melee damage callback, seeded saves, scene activation boundaries substituted. Not gameplay.");return count;
    }
    static void Blueprints(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        foreach(string token in new[]{"Invigorate","InvigorateMass","HowlingAgony"}) {
            bool hostile=token=="HowlingAgony",mass=token=="InvigorateMass";var spell=Asset<BlueprintAbility>(cache,token);var run=spell.GetComponent<AbilityEffectRunAction>();
            Check(spell.Type==AbilityType.Spell && spell.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,"Standard spell");
            Check(spell.Range==(hostile?AbilityRange.Close:mass?AbilityRange.Personal:AbilityRange.Touch),"CotW targeting range");
            Check(spell.CanTargetSelf==!hostile && spell.CanTargetFriends==!hostile && spell.CanTargetEnemies==hostile && spell.CanTargetPoint==hostile,"Precise target flags");
            Check(spell.SpellResistance==hostile && run.SavingThrowType==(hostile?SavingThrowType.Fortitude:SavingThrowType.Unknown),"Only hostile spell saves/SR");
            Check(spell.GetComponent<SpellComponent>().School==(hostile?SpellSchool.Necromancy:SpellSchool.Illusion),"Original spell school");
            var around=spell.GetComponent<AbilityTargetsAround>();Check(hostile||mass?around!=null && ((Kingmaker.Utility.Feet)Get(around,"m_Radius")).Value==(hostile?30:15) && (TargetType)Get(around,"m_TargetType")== (hostile?TargetType.Enemy:TargetType.Ally):around==null,"Enemy30 / friendly15 / single touch");
            Check((spell.AvailableMetamagic&Metamagic.Extend)!=0 && ((spell.AvailableMetamagic&Metamagic.Reach)!=0)==!mass,"Extend and range metamagic");
            var effect=hostile?(ContextActionApplyBuff)((ContextActionConditionalSaved)run.Actions.Actions.Single()).Failed.Actions.Single():(ContextActionApplyBuff)run.Actions.Actions.Single();
            Check(effect.Buff.Stacking==StackingType.Replace && effect.IsFromSpell && !effect.Permanent && !effect.AsChild,"Dispelable replacement buff");
            Check(effect.Buff==Asset<BlueprintBuff>(cache,hostile?"HowlingAgonyBuff":"InvigorateBuff"),"Single/mass share one suppression identity");
            Check(spell.GetComponent<SpellDescriptorComponent>()==null,"No imported foreign Pain bit or unintended mind-affecting descriptor");
            foreach(var link in spell.GetComponents<SpellListComponent>())Check(link.SpellList.SpellsByLevel[link.SpellLevel].Spells.Count(s=>s==spell)==1,"Bidirectional class list has exactly one spell");
            var ctx=f.Cast(hostile?SpellSchool.Necromancy:SpellSchool.Illusion,0,hostile?2:mass?4:1).Context;Set(ctx,"AssociatedBlueprint",spell);Set(ctx,"<ClickedTarget>k__BackingField",new Kingmaker.Utility.TargetWrapper(f.Unit));
            foreach(bool save in new[]{false,true}) {passed=save;applied=0;using(ctx.GetDataScope(f.Unit))run.Actions.Run();Check(applied==(hostile&&save?0:1),"Native action graph: Fort negates / friendly spell always applies");}
            foreach(int level in new[]{1,2,5,10,20,30})foreach(bool extend in new[]{false,true}) {
                ctx.SetParams(new AbilityParams {CasterLevel=level,Metamagic=extend?Metamagic.Extend:0});Set(ctx,"m_Ranks",Enumerable.Repeat(spell.GetComponent<ContextRankConfig>().GetValue(ctx),32).ToArray());
                Check(effect.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds(level*(extend?2:1)*(hostile?6:600)),"Actual caster-level duration and Extend");Check(effect.DurationValue.Rate==(hostile?DurationRate.Rounds:DurationRate.TenMinutes),"Round versus ten-minute units");
            }
        }
    }
    static void Suppression(BlueprintsCache cache,Psychic070Tests.Caster f,Harmony h)
    {
        Patch(h,AccessTools.Method(typeof(EntityFact),"Activate"),nameof(Activate));Patch(h,AccessTools.Method(typeof(EntityFact),"Deactivate"),nameof(Deactivate));
        var ctx=f.Cast(SpellSchool.Illusion,0,1).Context;var bp=Asset<BlueprintBuff>(cache,"InvigorateBuff");var c=bp.GetComponent<SuppressBuffs>();var runtime=Runtime(c,Fact(bp,ctx));
        var tired=Fact(c.Buffs[0],ctx);var exhausted=Fact(c.Buffs[1],ctx);var unrelated=Fact(Asset<BlueprintBuff>(cache,"HowlingAgonyBuff"),ctx);
        facts.List.Add(tired);facts.List.Add(unrelated);Refresh();
        using(runtime.RequestEventContext())AccessTools.Method(typeof(SuppressBuffs),"OnActivate").Invoke(c,null);
        var part=f.Unit.Get<UnitPartBuffSuppress>();Check(part!=null && tired.IsSuppressed && !unrelated.IsSuppressed && deactivations==1,"Native suppression turns off fatigue only");
        Check(facts.List.Contains(tired),"Fatigue is preserved, never removed");
        facts.List.Add(exhausted);Refresh();part.ApplyChanges();Check(exhausted.IsSuppressed && deactivations==2,"Later exhaustion is suppressed by same registry");
        var late=Fact(c.Buffs[0],ctx);Check(part.IsSuppressed(late),"Native query suppresses newly acquired fatigue before activation");
        // An independent suppression source owns another reference. Removing
        // Invigorate must not reactivate fatigue while that source remains.
        part.PrepareForSuppression(c.Buffs[0]);part.ApplyChanges();
        using(runtime.RequestEventContext())AccessTools.Method(typeof(SuppressBuffs),"OnDeactivate").Invoke(c,null);
        Check(tired.IsSuppressed && !exhausted.IsSuppressed && activations==1,"Removing spell retains other suppression and releases exhaustion");
        part.PrepareForRelease(c.Buffs[0]);part.ApplyChanges();Check(!tired.IsSuppressed && activations==2 && f.Unit.Get<UnitPartBuffSuppress>()==null,"Last reference restores original condition and removes empty part");
        // Recasting/replacing the same buff never accumulates ownership.
        for(int i=0;i<3;i++) {using(runtime.RequestEventContext())AccessTools.Method(typeof(SuppressBuffs),"OnActivate").Invoke(c,null);Check(tired.IsSuppressed&&exhausted.IsSuppressed,"Replacement activation suppresses both");using(runtime.RequestEventContext())AccessTools.Method(typeof(SuppressBuffs),"OnDeactivate").Invoke(c,null);Check(!tired.IsSuppressed&&!exhausted.IsSuppressed&&f.Unit.Get<UnitPartBuffSuppress>()==null,"Replacement/deactivation returns to baseline");}
        Check(facts.List.Count==3 && !unrelated.IsSuppressed,"No condition removed or unrelated buff suppressed");
        facts.List.Clear();Refresh();
    }
    static void Charge(BlueprintsCache cache,Psychic070Tests.Caster f,Harmony h)
    {
        var charge=(BlueprintAbility)cache.Load(BlueprintGuid.Parse("c78506dd0e14f7c45a599990e4e65038"));
        var checker=charge.GetComponent<Kingmaker.UnitLogic.Abilities.Components.CasterCheckers.AbilityCasterHasNoFacts>();checker.OwnerBlueprint=charge;
        var ctx=f.Cast(SpellSchool.Illusion,0,1).Context;
        var exhausted=Fact((BlueprintBuff)cache.Load(BlueprintGuid.Parse("46d1b9cc3d0fd36469a471b047d773a2")),ctx);
        var vigor=Fact(Asset<BlueprintBuff>(cache,"InvigorateBuff"),ctx);Set(vigor,"<IsTurnedOn>k__BackingField",true);
        facts.List.Add(exhausted);facts.List.Add(vigor);Refresh();Set(exhausted,"<IsSuppressed>k__BackingField",true);
        Check(!checker.IsCasterRestrictionPassed(f.Unit),"Reproduced native charge rejection despite suppressed exhaustion");
        h.CreateClassProcessor(typeof(PsychicLocateWeakness).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicInvigorateChargePatch")).Patch();
        Check(checker.IsCasterRestrictionPassed(f.Unit),"Patched native charge accepts Invigorate-suppressed exhaustion");
        foreach(bool active in new[]{false,true})foreach(bool vigorSuppressed in new[]{false,true})foreach(bool exhaustedSuppressed in new[]{false,true}) {
            Set(vigor,"<IsTurnedOn>k__BackingField",active);Set(vigor,"<IsSuppressed>k__BackingField",vigorSuppressed);Set(exhausted,"<IsSuppressed>k__BackingField",exhaustedSuppressed);
            Check(checker.IsCasterRestrictionPassed(f.Unit)==(active&&!vigorSuppressed&&exhaustedSuppressed),"Active Invigorate and suppressed exhaustion required");
        }
        Set(vigor,"<IsTurnedOn>k__BackingField",true);Set(vigor,"<IsSuppressed>k__BackingField",false);Set(exhausted,"<IsSuppressed>k__BackingField",true);
        foreach(var other in checker.Facts.Where(x=>x!=exhausted.Blueprint)) {
            var blocked=Fact((BlueprintBuff)other,ctx);facts.List.Add(blocked);Refresh();
            Check(!checker.IsCasterRestrictionPassed(f.Unit),"Other native charge blocking facts stay effective");
            Check(((System.Collections.IList)Get(checker,"m_FactsPresentedCache")).Count==1,"Native restriction UI cache retains only real blocker");
            facts.List.Remove(blocked);Refresh();Check(checker.IsCasterRestrictionPassed(f.Unit),"Removing other blocker restores charge");
        }
        checker.OwnerBlueprint=Asset<BlueprintAbility>(cache,"Invigorate");Check(!checker.IsCasterRestrictionPassed(f.Unit),"Same checker on non-charge ability stays native");checker.OwnerBlueprint=charge;
        facts.List.Remove(vigor);Refresh();Check(!checker.IsCasterRestrictionPassed(f.Unit),"Exhaustion blocks immediately after Invigorate removal");
        facts.List.Clear();Refresh();Check(checker.IsCasterRestrictionPassed(f.Unit),"Healthy character remains allowed");
    }
    static void Penalties(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var bp=Asset<BlueprintBuff>(cache,"HowlingAgonyBuff");var fact=Fact(bp,f.Cast(SpellSchool.Necromancy,0,2).Context);
        var stats=bp.GetComponents<AddStatBonus>().ToArray();Check(stats.Select(c=>c.Stat).OrderBy(x=>x).SequenceEqual(new[]{StatType.AC,StatType.AdditionalAttackBonus,StatType.SaveReflex}.OrderBy(x=>x)),"Exactly AC/attacks/Reflex penalties");
        foreach(var c in stats){var s=new ModifiableValue(null,c.Stat){BaseValue=10};var other=s.AddModifier(3,Empty<Buff>(),ModifierDescriptor.UntypedStackable);var own=s.AddModifier(c.Value,fact,c.Descriptor);Check(s.ModifiedValue==11,"Native penalty combines with existing bonus");s.RemoveModifier(own);Check(s.ModifiedValue==13,"Removing only own penalty restores existing bonus");s.RemoveModifier(other);}
        Check(bp.GetComponent<AddCondition>().Condition==UnitCondition.SpellCastingIsDifficult && Get(bp,"m_Flags").ToString().Contains("Harmful"),"Native concentration condition and harmful flag");
        var damage=bp.GetComponent<WeaponAttackTypeDamageBonus>();var runtime=Runtime(damage,fact);
        foreach(var attack in new[]{AttackType.Melee,AttackType.Ranged,AttackType.Touch,AttackType.RangedTouch}) {
            var type=new BlueprintWeaponType();Set(type,"m_AttackType",attack);var weaponBp=new BlueprintItemWeapon();var reference=BlueprintReferenceBase.CreateTyped<BlueprintWeaponTypeReference>(type);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(reference,type,null);Set(weaponBp,"m_Type",reference);
            var weapon=Empty<ItemEntityWeapon>();Set(weapon,"m_Blueprint",weaponBp);var rule=Empty<RuleCalculateWeaponStats>();Set(rule,"Weapon",weapon);Set(rule,"DamageModifiers",new ModifiableBonus());
            using(runtime.RequestEventContext())using(fact.Context.GetDataScope(f.Unit))damage.OnEventAboutToTrigger(rule);
            Check(rule.DamageModifiers.TotalValue==(attack==AttackType.Melee||attack==AttackType.Touch?-2:0),"Native melee-only damage callback: "+attack);
        }
    }
    static bool Saving(ref RuleSavingThrow __result){__result=Empty<RuleSavingThrow>();return false;}
    static bool Passed(ref bool __result){__result=passed;return false;}
    static bool Apply(){applied++;return false;}
    static bool Activate(EntityFact __instance){activations++;Set(__instance,"<IsActive>k__BackingField",true);return false;}
    static bool Deactivate(EntityFact __instance){deactivations++;Set(__instance,"<IsActive>k__BackingField",false);return false;}
}
