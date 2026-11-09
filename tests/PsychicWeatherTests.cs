using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Components;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
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
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicWeatherTests
{
    static int count,knocked,saves;static bool passed;static float distance=1;static UnitEntityData unit;static EntityFactsManager facts;static readonly List<object> damage=new List<object>();
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception("Weather: "+why);count++;}
    static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicWeatherMiss).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
    static EntityFactComponent Runtime(BlueprintComponent c,UnitFact f){var rt=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);rt.Setup(c);rt.RestoreOwnerLink(f);return rt;}
    static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
    static Buff Fact(BlueprintBuff bp,MechanicsContext ctx){var f=Empty<Buff>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"m_Context",ctx);Set(f,"<Rank>k__BackingField",1);Set(f,"<IsActive>k__BackingField",true);Set(f,"<IsTurnedOn>k__BackingField",true);f.RestoreOwnerLink(facts);return f;}
    static MechanicsContext Context(Psychic070Tests.Caster a,BlueprintScriptableObject bp,int cl=10,Metamagic mm=0){var ctx=new MechanicsContext(a.Unit,a.Descriptor,bp,null,new TargetWrapper(a.Unit));ctx.SetParams(new AbilityParams {CasterLevel=cl,DC=23,SpellLevel=3,Metamagic=mm});var ranks=new int[32];foreach(var rank in bp.GetComponents<ContextRankConfig>())ranks[(int)(AbilityRankType)Get(rank,"m_Type")]=rank.GetValue(ctx);Set(ctx,"m_Ranks",ranks);return ctx;}
    static void Clear(){facts.List.Clear();Refresh();}
    static ItemEntityWeapon Weapon(AttackType attack){var type=new BlueprintWeaponType();Set(type,"m_AttackType",attack);var bp=new BlueprintItemWeapon();var r=BlueprintReferenceBase.CreateTyped<BlueprintWeaponTypeReference>(type);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,type,null);Set(bp,"m_Type",r);var weapon=Empty<ItemEntityWeapon>();Set(weapon,"m_Blueprint",bp);return weapon;}
    internal static int Run(BlueprintsCache cache){
        var oldTime=Kingmaker.Game.Instance.TimeController;Set(Kingmaker.Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());
        var a=new Psychic070Tests.Caster(cache);unit=a.Unit;Set(unit,"m_IsInGame",true);Set(unit,"<HoldingState>k__BackingField",Empty<SceneEntitiesState>());facts=new EntityFactsManager(unit);Set(unit,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(unit,"<Buffs>k__BackingField",buffs);
        var h=new Harmony("CotW.Weather.NativeBoundaries");void Patch(System.Reflection.MethodBase m,string n)=>h.Patch(m,prefix:new HarmonyMethod(typeof(PsychicWeatherTests),n){priority=Priority.First});
        Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));
        Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),nameof(Damage));Patch(AccessTools.Method(typeof(ContextActionKnockdownTarget),"RunAction"),nameof(Knock));
        Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Passed));Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"ImprovedEvasion"),nameof(False));
        PsychicRuleTestBoundary.Patch(h,typeof(RuleSavingThrow),AccessTools.Method(typeof(PsychicWeatherTests),nameof(Save)));
        Patch(AccessTools.Method(typeof(RuleSkillCheck),"AdjustDCByDifficulty"),nameof(Difficulty));
        PsychicRuleTestBoundary.Patch(h,typeof(RuleSkillCheck),AccessTools.Method(typeof(PsychicWeatherTests),nameof(Skill)));
        Patch(AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),nameof(Add));Patch(AccessTools.Method(typeof(Buff),"Remove"),nameof(Remove));
        try{Metadata(cache,a);Ownership(cache,a);NativeFogCloudEffects(cache);Attacks(cache,a);Sleet(cache,a);River(cache,a);Conceal(cache,a,h);}finally{h.UnpatchAll(h.Id);Set(Kingmaker.Game.Instance,"TimeController",oldTime);Clear();}
        PsychicExpansionTests.Inventory["weatherSpellAssertions"]=count;Console.WriteLine("PASS: "+count+" weather checks: native area ownership, concealment and senses, overlap-safe penalties, weapon/ray direction, seeded native Mobility, River first/later saves and immunities, class lists and durations. Scene endpoints and random inputs substituted.");return count;
    }
    static void Metadata(BlueprintsCache c,Psychic070Tests.Caster a){
        string[] names={"ObscuringMist","FogCloud","SolidFog","SleetStorm","RiverOfWind","FickleWinds"};int[][] levels={new[]{1,1,1,1},new[]{2,2,2,2},new[]{4,4},new[]{3,3,3},new[]{4,4,4},new[]{3,5,5,5}};
        for(int i=0;i<names.Length;i++){
            var bp=Asset<BlueprintAbility>(c,names[i]);var links=bp.GetComponents<SpellListComponent>().ToArray();Check(links.Select(x=>x.SpellLevel).OrderBy(x=>x).SequenceEqual(levels[i]),"Original class levels "+names[i]);
            foreach(var l in links)Check(l.SpellList.SpellsByLevel[l.SpellLevel].Spells.Count(s=>s==bp)==1&&l.SpellList!=a.Book.Blueprint.SpellList,"Unique reciprocal native lists");
            Check(bp.Parent==null&&!ReferenceEquals(bp.Icon,null)&&bp.Type==AbilityType.Spell,"Learnable root and icon");
            Check(i==0?!bp.CanTargetPoint&&bp.CanTargetSelf&&bp.Range==AbilityRange.Personal&&!bp.AvailableMetamagic.HasFlag(Metamagic.Reach):bp.CanTargetPoint,"Obscuring Mist is self-centered; other weather roots target points");Check(bp.SpellResistance==(i==4),"River only SR");
            var action=bp.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();var duration=(action as ContextActionApplyBuff)?.DurationValue??((ContextActionSpawnAreaEffect)action).DurationValue;
            foreach(int cl in new[]{1,3,5,10,20,40})foreach(bool ext in new[]{false,true})Check(duration.Calculate(Context(a,bp,cl,ext?Metamagic.Extend:0)).Seconds==TimeSpan.FromSeconds(cl*(i==1?600:i==3||i==4?6:60)*(ext?2:1)),"Native duration/Extend "+names[i]);
            if(i<5){var area=((ContextActionSpawnAreaEffect)action).AreaEffect;Check(area.Shape==(i==4?AreaEffectShape.Wall:AreaEffectShape.Cylinder)&&area.Size.Value==(i==4?60:20)&&area.SpellResistance==(i==4),"Area geometry and SR");Check(!string.IsNullOrEmpty(area.Fx.AssetId),"Persistent visual");Check((bool)Get(area,"m_TickRoundAfterSpawn")== (i==4),"Initial tick only River; other entry handles it");}
        }
        var fickle=Asset<BlueprintAbility>(c,"FickleWinds");Check(fickle.NotOffensive&&!fickle.CanTargetEnemies&&(TargetType)Get(fickle.GetComponent<AbilityTargetsAround>(),"m_TargetType")==TargetType.Ally,"Fickle allies only");
    }
    static void Ownership(BlueprintsCache c,Psychic070Tests.Caster a){
        var enter=AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitEnter");var exit=AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitExit");
        foreach(string name in new[]{"ObscuringMist","FogCloud","SolidFog","SleetStorm","CondensedEther","BarrowHaze"}){
            var bp=Asset<BlueprintAbilityAreaEffect>(c,name+"Area");var ctx=Context(a,Asset<BlueprintAbility>(c,name));
            foreach(var owned in bp.GetComponents<AbilityAreaEffectBuff>().Where(b=>!b.Condition.Conditions.OfType<Kingmaker.UnitLogic.Mechanics.Conditions.ContextCondition>().Any(x=>x is PsychicSleetMovementFailure))){
                Clear();var one=Area("one",ctx,bp,true);var two=Area("two",ctx,bp,true);
                Check(owned.CheckConditionEveryRound&&owned.Buff.Stacking==StackingType.Stack,"Independent source-owned coexisting facts");
                enter.Invoke(owned,new object[]{ctx,one,unit});enter.Invoke(owned,new object[]{ctx,one,unit});Check(facts.List.Count==1&&unit.Buffs.Enumerable.Single().SourceAreaEffectId=="one","Native repeated entry idempotent");
                enter.Invoke(owned,new object[]{ctx,two,unit});Check(facts.List.Count==2,"Overlap retains both owners");exit.Invoke(owned,new object[]{ctx,one,unit});Check(facts.List.Count==1&&unit.Buffs.Enumerable.Single().SourceAreaEffectId=="two","Exit removes only its source");exit.Invoke(owned,new object[]{ctx,two,unit});Check(facts.List.Count==0,"Last exit/end cleans status");
            }
        }Clear();
    }
    static void NativeFogCloudEffects(BlueprintsCache c){
        // Obscuring Mist and Fog Cloud have concealment as their only effect, so the
        // area owns one buff that carries it instead of a second concealment buff.
        foreach(string token in new[]{"ObscuringMist","FogCloud"}){
            var area=Asset<BlueprintAbilityAreaEffect>(c,token+"Area");
            var owners=area.GetComponents<AbilityAreaEffectBuff>().Where(x=>x.CheckConditionEveryRound).ToArray();
            Check(owners.Length==1&&owners[0].Buff==Asset<BlueprintBuff>(c,token+"Buff"),token+" owns exactly its own buff");
            var single=owners[0].Buff;
            Check(single.ComponentsArray.OfType<AddConcealment>().Count()==2&&single.ComponentsArray.OfType<PsychicOutgoingFog>().Count()==1&&single.ComponentsArray.All(x=>x is AddConcealment||x is PsychicOutgoingFog),token+" buff carries the whole concealment contract and nothing else");
            Check(Asset<BlueprintBuff>(c,token+"Fog")==null,token+" no longer publishes a separate concealment buff");
        }
        var fog=Asset<BlueprintBuff>(c,"NativeFogCloudConcealmentBuff");
        Check(fog.ComponentsArray.OfType<AddConcealment>().Count()==2&&fog.ComponentsArray.OfType<PsychicOutgoingFog>().Count()==1,"Native fog effects use the complete incoming and outgoing fog contract");
        foreach(string id in new[]{"aa2e0a0fe89693f4e9205fd52c5ba3e5","053c8fd966bbcb94bb8db6582d990650","b342e42d2ed58484c8dff9150d18f4e4","ba09d51375db5f34790184443416d84b","6cae3c64989f3684bb078efcfa9021a1","6fa0adacca8d00f4aaba1e8df77a318f","706df636208b2864aa80032b72e0aabd","6df1ac314d4e6e9418e34470b79f90d8","1a62a0463e7d4da0814167e8308f1ebd","b6017edf09fe0374fa9a9bf06135f84b"}){
            var area=(BlueprintAbilityAreaEffect)c.Load(BlueprintGuid.Parse(id));
            var owners=(area.ComponentsArray??Array.Empty<BlueprintComponent>()).OfType<AbilityAreaEffectBuff>().Where(x=>x.Buff==fog).ToArray();
            Check(owners.Length==1&&owners[0].CheckConditionEveryRound,"Native fog area has one source-owned concealment effect: "+id);
        }
        var mephit=(BlueprintAbility)c.Load(BlueprintGuid.Parse("34283d686f5f5a847b4d0d6470b52a65"));
        var spawn=(ContextActionSpawnAreaEffect)mephit.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();
        Check(spawn.AreaEffect.AssetGuid==BlueprintGuid.Parse("aa2e0a0fe89693f4e9205fd52c5ba3e5"),"Water mephit Stinking Cloud shares the patched native area");
        var shadow=(BlueprintBuff)c.Load(BlueprintGuid.Parse("8de8e078992d7a4479f3d76e21aa1195"));
        var shadowArea=Asset<BlueprintAbilityAreaEffect>(c,"ShadowyMistFogArea");
        Check(shadow.GetComponents<AddAreaEffect>().Count(x=>x.AreaEffect==shadowArea)==1,"Shadowy Mist Form emits one moving 20-foot fog aura");
        Check(shadowArea.Size.Value==20&&shadowArea.GetComponents<AbilityAreaEffectBuff>().Single().Buff==fog,"Shadowy Mist Form aura applies the shared fog contract");
    }
    static void Attacks(BlueprintsCache c,Psychic070Tests.Caster a){
        var other=new Psychic070Tests.Caster(c).Unit;
        Set(unit.Stats,"AdditionalAttackBonus",new ModifiableValue(unit.Stats,StatType.AdditionalAttackBonus));
        foreach(string token in new[]{"SolidFogBuff","SleetStormRangedBuff","FickleWindsBuff","CondensedEtherBuff"}){
            var bp=Asset<BlueprintBuff>(c,token);var comp=bp.GetComponent<PsychicWeatherMiss>();var rt=Runtime(comp,Fact(bp,Context(a,bp)));
            foreach(AttackType type in new[]{AttackType.Melee,AttackType.Touch,AttackType.Ranged,AttackType.RangedTouch})foreach(int direction in new[]{0,1,2})foreach(bool miss in new[]{false,true}){
                var e=new RuleAttackRoll(direction==0?unit:other,direction==1?unit:other,Weapon(type),0){AutoMiss=miss};using(rt.RequestEventContext())comp.OnEventAboutToTrigger(e);
                Check(e.AutoMiss==(miss||type==AttackType.Ranged&&(direction==1||direction==0&&token!="FickleWindsBuff")),"Incoming/outgoing weapon blocking preserves rays, melee, unrelated rules and prior misses");
            }
        }
        var fog=Asset<BlueprintBuff>(c,"SolidFogBuff");var penalty=fog.GetComponent<PsychicSolidFogPenalty>();var ctx=Context(a,fog);
        foreach(int copies in new[]{1,2,3})foreach(bool melee in new[]{false,true}){
            Clear();Set(unit.Stats,"AdditionalAttackBonus",new ModifiableValue(unit.Stats,StatType.AdditionalAttackBonus));for(int i=0;i<copies;i++)facts.List.Add(Fact(fog,ctx));Refresh();var weapon=Weapon(melee?AttackType.Melee:AttackType.Ranged);var stats=new RuleCalculateWeaponStats(unit,weapon,null);var ab=new RuleCalculateAttackBonusWithoutTarget(unit,stats,0);
            foreach(var b in unit.Buffs.Enumerable){var rt=Runtime(penalty,b);using(rt.RequestEventContext()){penalty.OnEventAboutToTrigger(ab);penalty.OnEventAboutToTrigger(stats);}}
            Check(ab.AllBonuses.Sum(x=>x.Value)==(melee?-2:0),"Overlap attack penalty applied once");Check(stats.DamageModifiers.TotalValue==(melee?-2:0),"Overlap damage penalty applied once");
            var first=unit.Buffs.Enumerable.First();Set(first,"<IsTurnedOn>k__BackingField",false);Set(unit.Stats,"AdditionalAttackBonus",new ModifiableValue(unit.Stats,StatType.AdditionalAttackBonus));stats=new RuleCalculateWeaponStats(unit,weapon,null);ab=new RuleCalculateAttackBonusWithoutTarget(unit,stats,0);
            foreach(var b in unit.Buffs.Enumerable.Where(b=>b.IsTurnedOn)){var rt=Runtime(penalty,b);using(rt.RequestEventContext()){penalty.OnEventAboutToTrigger(ab);penalty.OnEventAboutToTrigger(stats);}}
            Check(ab.AllBonuses.Sum(x=>x.Value)==(melee&&copies>1?-2:0),"Remaining active source takes over without stacking");
        }Clear();
    }
    static int skillRoll;
    static void Sleet(BlueprintsCache c,Psychic070Tests.Caster a){
        var bp=Asset<BlueprintAbilityAreaEffect>(c,"SleetStormArea");var owned=bp.GetComponents<AbilityAreaEffectBuff>().Single(b=>b.Buff.name.EndsWith("MovementBuff"));var ctx=Context(a,Asset<BlueprintAbility>(c,"SleetStorm"));var area=Area("sleet",ctx,bp,true);var check=(PsychicSleetMovementFailure)owned.Condition.Conditions.Single();
        var dex=new ModifiableValueAttributeStat(unit.Stats,StatType.Dexterity){BaseValue=10};Set(unit.Stats,"Dexterity",dex);unit.Stats.AllStats.Add(dex);var stat=new ModifiableValueSkill(unit.Stats,dex,StatType.SkillMobility);unit.Stats.AllStats.Add(stat);Set(unit.Stats,"SkillMobility",stat);
        foreach(int result in new[]{1,4,5,6,9,10,11,20,6,10}){
            knocked=0;skillRoll=result;AccessTools.Method(typeof(AbilityAreaEffectBuff),facts.List.Count==0?"OnUnitEnter":"OnRound").Invoke(owned,facts.List.Count==0?new object[]{ctx,area,unit}:new object[]{ctx,area});
            Check(facts.List.Count==(result<10?1:0),"DC10 native Mobility determines current movement "+result);Check(knocked==(result<=5?1:0),"Failure margin five causes prone only "+result);
        }
        Clear();AddFeature(check.AirSubtype.Get());knocked=0;skillRoll=1;AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitEnter").Invoke(owned,new object[]{ctx,area,unit});Check(unit.Buffs.Enumerable.Count()==0&&knocked==0,"Air bypasses Mobility");Clear();
    }
    static void River(BlueprintsCache c,Psychic070Tests.Caster a){
        var bp=Asset<BlueprintAbilityAreaEffect>(c,"RiverOfWindArea");var run=bp.GetComponent<AbilityAreaEffectRunAction>();var spell=Asset<BlueprintAbility>(c,"RiverOfWind");var pulse=(PsychicRiverPulse)run.Round.Actions.Single();var conditional=(Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional)pulse.First.Actions.Single();var immunity=(PsychicWindImmunity)conditional.ConditionsChecker.Conditions.Single();
        Check(!run.UnitEnter.HasActions&&!run.UnitMove.HasActions&&!run.UnitExit.HasActions,"No entry double damage or movement spam");
        foreach(int immune in new[]{-1,0,1,2,3,4,5})foreach(bool pass in new[]{false,true})foreach(bool initial in new[]{false,true})foreach(var mm in new[]{(Metamagic)0,Metamagic.Empower|Metamagic.Maximize,Metamagic.Persistent}){
            Clear();if(immune==0)AddFeature(immunity.AirSubtype.Get());if(immune>0)facts.List.Add(Fact(immunity.Freedom[immune-1].Get(),Context(a,spell)));Refresh();
            var ctx=Context(a,spell,10,mm);var area=Area("river",ctx,bp,initial);passed=pass;damage.Clear();knocked=saves=0;AccessTools.Method(typeof(AbilityAreaEffectRunAction),"OnRound").Invoke(run,new object[]{ctx,area});
            Check(damage.Count==(immune<0?1:0)&&knocked==(immune<0&&!pass?1:0),"Initial/ongoing Fort and Air/all native Freedom immunity");
            Check(saves==(immune<0?(pass&&mm.HasFlag(Metamagic.Persistent)?2:1):0),"Immune skips saves; Persistent rerolls success");
            if(immune>=0)continue;var d=damage.Single();var dice=(DiceFormula)Get(d,"Dices");Check(dice.Dice==DiceType.D6&&dice.Rolls==(initial?4:2),"Exactly 4d6 first then 2d6");Check((bool)Get(d,"HalfBecauseSavingThrow")==pass&&(bool)Get(d,"Empower")==mm.HasFlag(Metamagic.Empower)&&(bool)Get(d,"Maximize")==mm.HasFlag(Metamagic.Maximize),"Native half and metamagic damage info");
        }Clear();
    }
    static void AddFeature(Kingmaker.Blueprints.Facts.BlueprintUnitFact bp){var f=Empty<UnitFact>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());f.RestoreOwnerLink(facts);facts.List.Add(f);Refresh();}
    static bool replacementConcealment;
    static bool TttStyleReplacement(UnitEntityData target,ref Kingmaker.Enums.Concealment __result){if(!replacementConcealment)return true;var part=target.Get<UnitPartConcealment>();var list=part==null?null:(List<UnitPartConcealment.ConcealmentEntry>)Get(part,"m_Concealments");__result=list==null||list.Count==0?Kingmaker.Enums.Concealment.None:list.Max(x=>x.Concealment);return false;}
    static void Conceal(BlueprintsCache c,Psychic070Tests.Caster a,Harmony h){
        var patch=typeof(PsychicOutgoingFog).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicSynesthesiaConcealmentPatch");var replacement=new Harmony("TabletopTweaks-Base");replacement.Patch(AccessTools.Method(typeof(UnitPartConcealment),"Calculate"),prefix:new HarmonyMethod(typeof(PsychicWeatherTests),nameof(TttStyleReplacement)));var hp=new Harmony("CotW.Weather.ProductionConcealment");hp.CreateClassProcessor(patch).Patch();
        h.Patch(AccessTools.Method(typeof(UnitEntityData),"DistanceTo",new[]{typeof(UnitEntityData)}),prefix:new HarmonyMethod(typeof(PsychicWeatherTests),nameof(Distance)));
        h.Patch(typeof(UnitEntityData).GetProperty("View",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.DeclaredOnly).GetGetMethod(),prefix:new HarmonyMethod(typeof(PsychicWeatherTests),nameof(View)));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.View.UnitEntityView),"Corpulence"),prefix:new HarmonyMethod(typeof(PsychicWeatherTests),nameof(Corpulence)));
        var other=new Psychic070Tests.Caster(c).Unit;var manager=new EntityFactsManager(other);Set(other,"Facts",manager);var buffs=new BuffCollection();buffs.Initialize(manager);Set(other,"<Buffs>k__BackingField",buffs);foreach(var u in new[]{unit,other}){Set(u,"<View>k__BackingField",Empty<Kingmaker.View.UnitEntityView>());Set(u.State,"m_Conditions",new sbyte[128]);Set(u.State,"m_ConditionsImmunity",new sbyte[128]);Set(u.State,"m_ConditionsExceptions",new List<UnitConditionExceptions>[128]);}var targetPart=other.Ensure<UnitPartConcealment>();var senses=unit.Ensure<UnitPartConcealment>();
        // A native same-value entry must survive temporary outgoing concealment cleanup.
        var existing=new UnitPartConcealment.ConcealmentEntry {Concealment=Kingmaker.Enums.Concealment.Partial,Descriptor=ConcealmentDescriptor.Fog};targetPart.AddConcealment(existing);
        try{
            foreach(string name in new[]{"ObscuringMistBuff","FogCloudBuff","SolidFogFog","SleetStormFog"})foreach(float feet in new[]{1f,5f,5.01f,30f})foreach(int mode in new[]{0,1,2,3,4}){
                Clear();var b=Fact(Asset<BlueprintBuff>(c,name),Context(a,Asset<BlueprintAbility>(c,name.EndsWith("Buff")?name.Substring(0,name.Length-4):name.Substring(0,name.Length-3))));facts.List.Add(b);Refresh();distance=new Feet(feet).Meters;Set(other,"m_Position",new UnityEngine.Vector3(distance,0,0));Set(senses,"m_IgnoreAllCount",mode==1?1:0);Set(senses,"m_IgnorePartialCount",mode==2?1:0);Set(senses,"m_TreatTotalAsPartialCount",mode==3?1:0);if(mode==4)senses.AddBlindsight(new Feet(60),null);
                var raw=name=="SleetStormFog"||feet>5?Kingmaker.Enums.Concealment.Total:Kingmaker.Enums.Concealment.Partial;var expected=mode==1||mode==4?Kingmaker.Enums.Concealment.None:mode==2&&raw==Kingmaker.Enums.Concealment.Partial?Kingmaker.Enums.Concealment.None:mode==3?Kingmaker.Enums.Concealment.Partial:raw;
                var actual=UnitPartConcealment.Calculate(unit,other,true);Check(actual==expected,"Native distance/sensory fog calculation "+name+" "+feet+" "+mode+" actual="+actual+" distance="+unit.DistanceTo(other)+" corpus="+unit.View.Corpulence);
                var entries=(List<UnitPartConcealment.ConcealmentEntry>)Get(targetPart,"m_Concealments");Check(entries.Count==1&&ReferenceEquals(entries[0],existing),"Temporary native entries fully removed; same-value foreign entry preserved");
                if(mode==4)senses.RemoveBlindsight(new Feet(60),null);
            }
            Set(senses,"m_IgnoreAllCount",0);Set(senses,"m_IgnorePartialCount",0);Set(senses,"m_TreatTotalAsPartialCount",0);
            foreach(bool otherEngine in new[]{false,true})foreach(bool own in new[]{false,true})foreach(bool suppressed in new[]{false,true})foreach(bool inside in new[]{false,true}){
                Clear();manager.List.Clear();var mc=Get(manager,"m_Cache");AccessTools.Method(mc.GetType(),"Clear").Invoke(mc,null);
                replacementConcealment=otherEngine;var cloudCaster=own?a:new Psychic070Tests.Caster(c);var b=Fact(Asset<BlueprintBuff>(c,"BarrowHazeFog"),Context(cloudCaster,Asset<BlueprintAbility>(c,"BarrowHaze")));
                Set(b,"<IsTurnedOn>k__BackingField",!suppressed);if(inside)facts.List.Add(b);else{b.RestoreOwnerLink(manager);manager.List.Add(b);AccessTools.Method(mc.GetType(),"Clear").Invoke(mc,null);}Refresh();var hazePart=(inside?unit:other).Ensure<UnitPartConcealment>();var hazeEntries=(List<UnitPartConcealment.ConcealmentEntry>)Get(hazePart,"m_Concealments");if(hazeEntries==null){hazeEntries=new List<UnitPartConcealment.ConcealmentEntry>();Set(hazePart,"m_Concealments",hazeEntries);}var beforeHaze=hazeEntries.ToArray();if(!suppressed)foreach(var comp in b.Blueprint.GetComponents<AddConcealment>()){var rt=Runtime(comp,b);using(rt.RequestEventContext())AccessTools.Method(typeof(AddConcealment),"OnTurnOn").Invoke(comp,null);}distance=new Feet(30).Meters;Set(other,"m_Position",new UnityEngine.Vector3(distance,0,0));
                Check(UnitPartConcealment.Calculate(unit,other,true)==(!own&&!suppressed?Kingmaker.Enums.Concealment.Total:Kingmaker.Enums.Concealment.Partial),"Barrow own vision, foreign source, inactive cloud, either endpoint; foreign native partial preserved");
                Check(((List<UnitPartConcealment.ConcealmentEntry>)Get(targetPart,"m_Concealments")).Count==(inside||suppressed?1:3),"Haze native entries retained; temporary entries removed");hazeEntries.Clear();hazeEntries.AddRange(beforeHaze);
            }
            replacementConcealment=false;manager.List.Clear();AccessTools.Method(Get(manager,"m_Cache").GetType(),"Clear").Invoke(Get(manager,"m_Cache"),null);

            Clear();facts.List.Add(Fact(Asset<BlueprintBuff>(c,"SolidFogFog"),Context(a,Asset<BlueprintAbility>(c,"SolidFog"))));Refresh();
            // Force an exception in the native calculation after the production prefix ran.
            // A throwing prefix on Calculate runs after ours at Last priority even if DistanceTo was inlined.
            var errorPatch=new Harmony("CotW.Weather.ExceptionBoundary");errorPatch.Patch(AccessTools.Method(typeof(UnitPartConcealment),"Calculate"),prefix:new HarmonyMethod(typeof(PsychicWeatherTests),nameof(Fault)){priority=Priority.Last});
            bool threw=false;try{UnitPartConcealment.Calculate(unit,other,true);}catch(InvalidOperationException){threw=true;}finally{errorPatch.UnpatchAll(errorPatch.Id);}
            var retained=(List<UnitPartConcealment.ConcealmentEntry>)Get(targetPart,"m_Concealments");Check(threw&&retained.Count==1&&ReferenceEquals(retained[0],existing),"Finalizer preserves original exception and removes only temporary entries");
            Clear();Check(UnitPartConcealment.Calculate(unit,other,true)==Kingmaker.Enums.Concealment.Partial,"No outgoing residue after removing buff");
        }finally{replacementConcealment=false;replacement.UnpatchAll(replacement.Id);hp.UnpatchAll(hp.Id);}
    }
    static AreaEffectEntityData Area(string id,MechanicsContext ctx,BlueprintAbilityAreaEffect bp,bool initial){var e=Empty<AreaEffectEntityData>();Set(e,"<UniqueId>k__BackingField",id);Set(e,"m_Context",ctx);Set(e,"m_Blueprint",bp);Set(e,"m_CreationTime",Kingmaker.Game.Instance.TimeController.GameTime-TimeSpan.FromSeconds(initial?0:6));var ft=AccessTools.Field(typeof(AreaEffectEntityData),"m_UnitsInside");var list=(System.Collections.IList)Activator.CreateInstance(ft.FieldType);var info=Activator.CreateInstance(ft.FieldType.GetGenericArguments()[0],true);Set(info,"Reference",(UnitReference)unit);list.Add(info);ft.SetValue(e,list);return e;}
    static bool Time(ref TimeSpan __result){__result=TimeSpan.FromSeconds(100);return false;}
    static bool Damage(ContextActionDealDamage __instance){damage.Add(AccessTools.Method(typeof(ContextActionDealDamage),"GetDamageInfo").Invoke(__instance,null));return false;}
    static bool Knock(){knocked++;return false;}static bool Passed(ref bool __result){__result=passed;return false;}static bool False(ref bool __result){__result=false;return false;}
    static bool Save(RuleSavingThrow rule,ref RuleSavingThrow __result){saves++;__result=rule;Check(rule.DifficultyClassBase==23,"Original spell DC");return false;}
    static bool Difficulty(int __0,ref int __1,ref int __result){__1=0;__result=__0;return false;}
    static bool Skill(RuleSkillCheck rule,ref RuleSkillCheck __result){var q=(Queue<int>)AccessTools.Field(typeof(Psychic070Tests),"dice").GetValue(null);q.Enqueue(skillRoll);rule.OnTrigger(null);q.Clear();__result=rule;return false;}
    static bool Add(BlueprintBuff __1,MechanicsContext __2,ref Buff __result){__result=Fact(__1,__2);facts.List.Add(__result);Refresh();return false;}
    static bool Remove(Buff __instance){facts.List.Remove(__instance);Refresh();return false;}
    static void Fault(){throw new InvalidOperationException("Weather native boundary test");}
    static bool Distance(ref float __result){__result=distance;return false;}static bool Corpulence(ref float __result){__result=0;return false;}static bool View(ref Kingmaker.View.UnitEntityView __result){__result=Empty<Kingmaker.View.UnitEntityView>();return false;}
}
