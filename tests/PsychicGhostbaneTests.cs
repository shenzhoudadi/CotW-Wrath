using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;

internal static class PsychicGhostbaneTests
{
    static int count,applied;static bool passed;static EntityFactsManager facts;
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Check(bool b,string why){if(!b)throw new Exception("Ghostbane: "+why);count++;}
    static T Asset<T>(BlueprintsCache c,string token) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(CallOfTheWild.Wrath.Prototype.PsychicForceSword).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
    static Buff BuffFact(BlueprintBuff bp){var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);Set(b,"<IsActive>k__BackingField",true);Set(b,"Components",new List<EntityFactComponent>());b.RestoreOwnerLink(facts);return b;}
    static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
    internal static int Run(BlueprintsCache cache)
    {
        var f=new Psychic070Tests.Caster(cache);facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);
        var native=(BlueprintFeature)cache.Load(BlueprintGuid.Parse("c4a7f98d743bc784c9d4cf2105852c39"));
        var signature=string.Join("|",native.ComponentsArray.Select(c=>c.GetType().FullName));
        var ghost=Empty<UnitFact>();Set(ghost,"<Blueprint>k__BackingField",native);Set(ghost,"Components",new List<EntityFactComponent>());ghost.RestoreOwnerLink(facts);
        var h=new Harmony("CotW.Ghostbane.NativeDivisor");
        try {
            h.Patch(AccessTools.PropertyGetter(typeof(MechanicsContext),"SavingThrow"),prefix:new HarmonyMethod(typeof(PsychicGhostbaneTests),nameof(Saving)));
            h.Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),prefix:new HarmonyMethod(typeof(PsychicGhostbaneTests),nameof(Passed)));
            h.Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),prefix:new HarmonyMethod(typeof(PsychicGhostbaneTests),nameof(Apply)));
            foreach(bool mass in new[]{false,true}) {
                var spell=Asset<BlueprintAbility>(cache,mass?"GhostbaneDirgeMass":"GhostbaneDirge");var bp=Asset<BlueprintBuff>(cache,"GhostbaneDirgeBuff");
                var ctx=f.Cast(SpellSchool.Transmutation,0,2).Context;Set(ctx,"AssociatedBlueprint",spell);Set(ctx,"<ClickedTarget>k__BackingField",new TargetWrapper(f.Unit));
                Check(spell.School==SpellSchool.Transmutation&&spell.SpellResistance&&spell.Range==AbilityRange.Close,"Close transmutation with SR");
                Check(spell.CanTargetPoint==mass&&spell.CanTargetEnemies&&!spell.CanTargetFriends&&!spell.CanTargetSelf,"Enemy target flags");
                Check(spell.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,"Standard action");
                Check(spell.AvailableMetamagic==(Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach|Metamagic.Persistent|Metamagic.Piercing),"Original metamagic set");
                Check(ReferenceEquals(spell.Icon,bp.Icon)&&!ReferenceEquals(spell.Icon,null)&&bp.FxOnStart.AssetId=="3cf209e5299921349a1c159f35cfa369","CotW icon/FX");
                Check(bp.Stacking==StackingType.Replace&&Get(bp,"m_Flags").ToString().Contains("Harmful")&&bp.ComponentsArray.Length==0,"Native timed replaceable state without custom saved components");
                var links=spell.GetComponents<SpellListComponent>().ToArray();
                var expected=new Dictionary<string,int>{{"25a5013493bdcf74bb2424532214d0c8",mass?4:2},{"8443ce803d2d31347897a3d85cc32f53",mass?5:2},{"57c894665b7895c499b3dce058c284b3",mass?5:2},{"9f5be2f7ea64fe04eb40878347b147bc",mass?3:1}};
                Check(links.Length==4,"Four native class lists, not Psychic");
                foreach(var l in links)Check(expected[l.SpellList.AssetGuid.ToString()]==l.SpellLevel&&l.SpellList.SpellsByLevel[l.SpellLevel].Spells.Count(s=>s==spell)==1,"Correct reciprocal list/circle exactly once");
                var run=spell.GetComponent<AbilityEffectRunAction>();Check(run.SavingThrowType==Kingmaker.EntitySystem.Stats.SavingThrowType.Will,"Will negates");
                var apply=(ContextActionApplyBuff)((ContextActionConditionalSaved)run.Actions.Actions.Single()).Failed.Actions.Single();
                Check(apply.Buff==bp&&!apply.Permanent&&!apply.AsChild&&!apply.IsNotDispelable&&apply.IsFromSpell,"Independent dispellable timed buff");
                foreach(bool save in new[]{false,true}){passed=save;applied=0;using(ctx.GetDataScope(f.Unit))run.Actions.Run();Check(applied==(save?0:1),"Actual native saved branch");}
                foreach(int cl in new[]{1,5,10,20,30})foreach(bool extend in new[]{false,true}) {
                    ctx.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=extend?Metamagic.Extend:0});Set(ctx,"m_Ranks",Enumerable.Repeat(spell.GetComponent<ContextRankConfig>().GetValue(ctx),32).ToArray());
                    Check(apply.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds(cl*6*(extend?2:1)),"Actual native CL rounds/Extend");
                }
                foreach(bool isGhost in new[]{false,true}) {
                    facts.List.Clear();if(isGhost)facts.List.Add(ghost);Refresh();
                    if(mass){var around=spell.GetComponent<AbilityTargetsAround>();Check(((Feet)Get(around,"m_Radius")).Value==15&&(TargetType)Get(around,"m_TargetType")==TargetType.Enemy,"Fifteen-foot enemy burst");using(ctx.GetDataScope(f.Unit))Check(((ConditionsChecker)Get(around,"m_Condition")).Check()==isGhost,"Actual native burst incorporeal predicate");}
                    else Check(spell.GetComponent<AbilityTargetHasFact>().IsTargetRestrictionPassed(f.Unit,new TargetWrapper(f.Unit))==isGhost,"Actual native single target restriction");
                }
            }
            facts.List.Clear();facts.List.Add(ghost);Refresh();
            var component=native.GetComponent<AddIncorporealDamageDivisor>();Check(component!=null,"Native incorporeal damage component extracted");
            var runtime=(EntityFactComponent)AccessTools.Method(component.GetType(),"CreateRuntimeFactComponent").Invoke(component,null);runtime.Setup(component);runtime.RestoreOwnerLink(ghost);
            var prefix=AccessTools.Method(typeof(CallOfTheWild.Wrath.Prototype.PsychicForceSword).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicGhostbaneDirge+DamagePatch"),"Prefix");
            h.Patch(AccessTools.Method(typeof(AddIncorporealDamageDivisor),"OnEventAboutToTrigger"),prefix:new HarmonyMethod(prefix));
            var buff=BuffFact(Asset<BlueprintBuff>(cache,"GhostbaneDirgeBuff"));
            var critical=native.GetComponent<GhostCriticalAndPrecisionImmunity>();
            var criticalRuntime=(EntityFactComponent)AccessTools.Method(critical.GetType(),"CreateRuntimeFactComponent").Invoke(critical,null);criticalRuntime.Setup(critical);criticalRuntime.RestoreOwnerLink(ghost);
            // Reproduce 0.13.9: the first patched component alone is insufficient.
            facts.List.Add(buff);
            foreach(int enchant in new[]{0,1}) {
                var d=new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(1,DiceType.D6)),0,PhysicalDamageForm.Slashing){EnchantmentTotal=enchant};
                var old=Empty<RuleCalculateDamage>();Set(old,"ParentRule",new RuleDealDamage(f.Unit,f.Unit,new DamageBundle(d)));Set(old,"Target",f.Unit);
                using(runtime.RequestEventContext())component.OnEventAboutToTrigger(old);
                using(criticalRuntime.RequestEventContext())critical.OnEventAboutToTrigger(old);
                Check(d.Decline.Value==(enchant==0?DamageDeclineType.Total:DamageDeclineType.ByHalf),"Old path reproduced: second native component restores immunity/half despite active Dirge");
            }
            facts.List.Remove(buff);
            var patchType=typeof(CallOfTheWild.Wrath.Prototype.PsychicForceSword).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicGhostbaneDirge+CriticalPatch");
            h.CreateClassProcessor(patchType).Patch();
            // Native handler runs for absent/inactive/suppressed/disposed states;
            // repeated reapplication needs no saved registry or stale unit part.
            foreach(bool reverse in new[]{false,true})foreach(bool precision in new[]{false,true})foreach(int state in Enumerable.Range(0,6))foreach(int enchant in new[]{0,1,5})foreach(bool ghostTouch in new[]{false,true})foreach(int abilityKind in Enumerable.Range(0,5))foreach(bool weapon in new[]{false,true}) {
                facts.List.Remove(buff);Set(buff,"<IsActive>k__BackingField",state!=2);Set(buff,"<IsDisposed>k__BackingField",state==4);Set(buff,"<IsSuppressed>k__BackingField",state==3);
                if(state!=0&&state!=5)facts.List.Add(buff);bool active=state==1;
                var physical=new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(2,DiceType.D6)),0,PhysicalDamageForm.Slashing){EnchantmentTotal=enchant};
                var fire=new EnergyDamage(new DiceFormula(2,DiceType.D6),DamageEnergyType.Fire);var force=new DirectDamage(new DiceFormula(2,DiceType.D6),0);
                if(ghostTouch)physical.Reality=DamageRealityType.Ghost;physical.Precision=precision;physical.CriticalModifier=2;
                var foreign=new DamageDecline(DamageDeclineType.ByQuarter,ghost);physical.AddDecline(foreign);fire.SetReductionBecauseResistance(3,ghost);
                var bundle=new DamageBundle(physical,fire,force);if(weapon)bundle.Weapon=Empty<ItemEntityWeapon>();
                var parent=new RuleDealDamage(f.Unit,f.Unit,bundle);parent.Reason=Empty<RuleReason>();
                if(abilityKind>0){var a=Empty<AbilityData>();Set(a,"Blueprint",new BlueprintAbility {Type=new[]{AbilityType.Extraordinary,AbilityType.Spell,AbilityType.SpellLike,AbilityType.Supernatural}[abilityKind-1]});Set(parent.Reason,"<Ability>k__BackingField",a);}
                var rule=Empty<RuleCalculateDamage>();Set(rule,"ParentRule",parent);Set(rule,"Initiator",f.Unit);Set(rule,"Target",f.Unit);
                if(reverse){using(criticalRuntime.RequestEventContext())critical.OnEventAboutToTrigger(rule);}
                using(runtime.RequestEventContext())component.OnEventAboutToTrigger(rule);
                if(!reverse){using(criticalRuntime.RequestEventContext())critical.OnEventAboutToTrigger(rule);}
                bool magic=abilityKind>=2&&!weapon;
                var expected=ghostTouch?DamageDeclineType.None:active?(enchant==0&&!magic?DamageDeclineType.ByHalf:DamageDeclineType.None):(enchant==0?DamageDeclineType.Total:DamageDeclineType.ByHalf);
                var combined=expected;
                if(!(ghostTouch&&weapon)) { if(precision)combined=DamageDeclineType.Total;else if(!active)combined=enchant==0?DamageDeclineType.Total:DamageDeclineType.ByHalf; }
                var declines=physical.Declines.Where(d=>d.IncorporealDamageDivisor).ToArray();
                Check((expected==DamageDeclineType.None?declines.Length==0:declines.Length>=1)&&declines.All(d=>d.Type==expected),"Patched native physical divisor: state/enchantment/ghost touch/direct magical effects/weapon delivery");
                Check(physical.Declines.Contains(foreign)&&physical.Decline.Value==(combined>DamageDeclineType.ByQuarter?combined:DamageDeclineType.ByQuarter),"Native total decline retains unrelated defense: "+state+"/"+enchant+"/ghost="+ghostTouch+"/precision="+precision+"/weapon="+weapon+"/magic="+magic+"/actual="+physical.Decline.Value+"/expected="+expected);
                Check(fire.Declines.Count(d=>d.IncorporealDamageDivisor)==(active?0:1)&&fire.ReductionBecauseResistance.TotalValue==3,"Energy full only under Dirge; independent resistance retained");
                Check(physical.CriticalModifier==(ghostTouch&&weapon?(int?)2:null),"Native critical immunity preserved");
                Check(!force.Declines.Any(),"Already unaffected force/direct damage not altered");
            }
            h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Settings.SettingsRoot),"Difficulty"),prefix:new HarmonyMethod(typeof(PsychicGhostbaneTests),nameof(Difficulty)));
            h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.EntitySystem.Entities.UnitEntityData),"IsPlayerFaction"),prefix:new HarmonyMethod(typeof(PsychicGhostbaneTests),nameof(NotPlayer)));
            h.Patch(AccessTools.PropertyGetter(typeof(RuleCalculateDamage),"TacticalCombatDRModifier"),prefix:new HarmonyMethod(typeof(PsychicGhostbaneTests),nameof(One)));
            h.Patch(AccessTools.Method(typeof(RuleCalculateDamage),"Roll"),prefix:new HarmonyMethod(typeof(PsychicGhostbaneTests),nameof(Roll)));
            h.Patch(AccessTools.Method(typeof(ShadowSpellLogicExtension),"TryApplyShadowDamageFactor",new[]{typeof(RulebookEvent),typeof(Kingmaker.EntitySystem.Entities.UnitEntityData),typeof(int)}),prefix:new HarmonyMethod(typeof(PsychicGhostbaneTests),nameof(NoShadow)));
            facts.List.Remove(buff);Set(buff,"<IsActive>k__BackingField",true);Set(buff,"<IsDisposed>k__BackingField",false);Set(buff,"<IsSuppressed>k__BackingField",false);
            foreach(bool active in new[]{false,true})foreach(int kind in new[]{0,1,2})foreach(bool precision in new[]{false,true}) {
                facts.List.Remove(buff);if(active)facts.List.Add(buff);
                BaseDamage d=kind==2?(BaseDamage)new EnergyDamage(new DiceFormula(4,DiceType.D6),DamageEnergyType.Fire):new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(4,DiceType.D6)),0,PhysicalDamageForm.Slashing){EnchantmentTotal=kind};d.Precision=precision;
                var parent=new RuleDealDamage(f.Unit,f.Unit,new DamageBundle(d));var rule=Empty<RuleCalculateDamage>();Set(rule,"ParentRule",parent);Set(rule,"Target",f.Unit);Set(rule,"Initiator",f.Unit);Set(rule,"<UnitsCount>k__BackingField",1);Set(rule,"<TacticalCombatFactor>k__BackingField",1f);
                using(runtime.RequestEventContext())component.OnEventAboutToTrigger(rule);
                // Warm the cache before the second callback to catch stale results.
                if(active&&!precision){var before=d.Decline.Value;}
                using(criticalRuntime.RequestEventContext())critical.OnEventAboutToTrigger(rule);
                var result=(DamageValue)AccessTools.Method(typeof(RuleCalculateDamage),"CalculateDamageValue").Invoke(rule,new object[]{d});
                int expected=precision?0:active?(kind==0?10:20):(kind==0?0:10);
                Check(result.FinalValue==expected,"Actual native final damage20: "+active+"/"+kind+"/"+precision+" actual="+result.FinalValue+" expected="+expected);
            }
            facts.List.Remove(buff);Check(facts.List.Contains(ghost)&&signature==string.Join("|",native.ComponentsArray.Select(c=>c.GetType().FullName)),"Original feature and all other components remain intact");
        }finally{h.UnpatchAll(h.Id);}
        PsychicExpansionTests.Inventory["ghostbaneAssertions"]=count;
        Console.WriteLine("PASS: "+count+" Ghostbane checks: patched native incorporeal handler/decline aggregation, defense preservation, target predicates, seeded save branches, duration and original lists. Constructed facts and scene application boundary; not gameplay.");return count;
    }
    static bool Difficulty(ref Kingmaker.Settings.DifficultySettings __result){__result=Empty<Kingmaker.Settings.DifficultySettings>();return false;}
    static bool NotPlayer(ref bool __result){__result=false;return false;}
    static bool One(ref float __result){__result=1f;return false;}
    static bool Roll(ref int __result){__result=20;return false;}
    static bool NoShadow(int __2,ref int __result){__result=__2;return false;}
    static bool Saving(ref RuleSavingThrow __result){__result=Empty<RuleSavingThrow>();return false;}
    static bool Passed(ref bool __result){__result=passed;return false;}
    static bool Apply(){applied++;return false;}
}
