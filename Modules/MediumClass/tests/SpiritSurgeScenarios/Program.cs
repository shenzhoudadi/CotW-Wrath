using System.Reflection;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using MediumClass.Medium;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Utilities;
using MediumClass.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

int checks=0;
void Equal(int want,int got,string why){checks++;if(want!=got){Console.Error.WriteLine($"FAIL: {why}: expected {want}, got {got}");throw new Exception(why);}}
void Same(string want,string got,string why){checks++;if(want!=got){Console.Error.WriteLine($"FAIL: {why}: expected '{want}', got '{got}'");throw new Exception(why);}}
void Check(bool ok,string why){checks++;if(!ok){Console.Error.WriteLine($"FAIL: {why}");throw new Exception(why);}}
var marshal=new BlueprintCharacterClassReference{Value=new()};
var champion=new BlueprintCharacterClassReference{Value=new()};
var trickster=new BlueprintCharacterClassReference{Value=new()};
BlueprintTool.Assets[Guids.Marshal+".ref"]=marshal;
var bonus=new BlueprintFeature();var bonusRef=new BlueprintFeatureReference{Value=bonus};
foreach(var id in new[]{Guids.MarshalMarshalsOrdersAbility,Guids.MarshalLegendaryMarshalAbility,Guids.SpiritSurgeAbility})BlueprintTool.Assets[id]=new BlueprintAbility();
BlueprintTool.Assets[Guids.MediumChannelSpirit]=new BlueprintFeature{Components=new object[]{new MediumSpiritComponent{Stats=(StatType[])Enum.GetValues(typeof(StatType))}}};

// Deterministic die source (the component's only random input). Every draw is recorded; results come from the
// script first, then default to the maximum face. No random hooks exist in the production code: the framework
// lets a test replace this one readonly field.
var die=new ScriptedDie();
typeof(MediumSpiritSurgeComponent).GetField("rnd",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,die);

UnitEntityData Caster(bool prowler,bool multiple,bool hardened=false,int level=20,string name="Caster",int rank=6,int focus=0){
    var caster=new UnitEntityData{CharacterName=name,Level=level};caster.Descriptor.Prowler=prowler;caster.Descriptor.Hardened=hardened;
    caster.Progression.Features.Ranks[bonus]=rank;
    var part=caster.Ensure<UnitPartMedium>();
    part.Spirits[marshal]=new(){SpiritFocus=focus,SpiritBonus=new(){Stats=MarshalSpiritRules.PassiveBonusStats,Concentration=false,SpiritBonusFeature=bonusRef}};
    part.Spirits[champion]=new(){SpiritBonus=new(){Stats=new[]{StatType.AdditionalAttackBonus,StatType.AdditionalDamage},SpiritBonusFeature=bonusRef}};
    part.Spirits[trickster]=new(){SpiritBonus=new(){Stats=new[]{StatType.Initiative,StatType.SkillPersuasion},SpiritBonusFeature=bonusRef}};
    part.ActiveSpiritClasses.Add(marshal);
    if(multiple){part.ActiveSpiritClasses.Add(champion);part.ActiveSpiritClasses.Add(trickster);}
    return caster;
}
MediumSpiritSurgeComponent Apply(UnitEntityData caster,UnitEntityData target,string ability){
    var component=new MediumSpiritSurgeComponent{Owner=target,Context=new(){MaybeCaster=caster,SourceAbility=BlueprintTool.Get<BlueprintAbility>(ability)}};
    component.OnTurnOn();return component;
}
int Total(UnitEntityData unit,StatType stat)=>unit.Stats.GetStat(stat).Total;
int Concentration(MediumSpiritSurgeComponent c)=>c.GetStaticConcentrationBonus(new EntityFactComponent());
void AllStatsZero(UnitEntityData unit,string why){foreach(StatType stat in Enum.GetValues(typeof(StatType)))Equal(0,Total(unit,stat),why);}
// The runtime data the game would save with the buff. A component without the property cannot persist a roll.
object SavedData(MediumSpiritSurgeComponent c)=>c.GetType().GetProperty("MaybeData")?.GetValue(c);
var resolver=new JsonSerializerSettings{ContractResolver=new OptInResolver()};   // the game's OptInContractResolver policy

// ---- A. one draw per effect instance: 1d6/1d8/1d10, Prowler, Legendary, 2d8; every stat and concentration reuse it -----------
foreach(int level in new[]{1,10,20})foreach(bool prowler in new[]{false,true})foreach(bool multiple in new[]{false,true})foreach(bool hardened in new[]{false,true}){
    string label=$"level {level}, prowler {prowler}, multiple {multiple}, hardened {hardened}";
    var sides=hardened?new[]{8,8}:new[]{prowler?6:level>=20?10:level>=10?8:6};
    var faces=hardened?new[]{3,5}:new[]{sides[0]-1==0?1:sides[0]-2};
    die.Script(faces);
    var caster=Caster(prowler,multiple,hardened,level);
    var surge=Apply(caster,caster,Guids.SpiritSurgeAbility);
    Check(die.Sides.SequenceEqual(sides),$"exactly one die of the level's type ({(hardened?"2d8":"1d"+sides[0])}) is drawn for the whole effect, {label}; drew [{string.Join(",",die.Sides)}]");
    int roll=faces.Sum();
    foreach(StatType stat in new[]{StatType.AdditionalAttackBonus,StatType.SaveWill,StatType.SkillPersuasion,StatType.AdditionalDamage,StatType.Initiative}.Where(s=>multiple||s==StatType.AdditionalAttackBonus||s==StatType.SaveWill||s==StatType.SkillPersuasion)){
        int marshalPart=6;
        Equal(roll+marshalPart,Total(caster,stat),$"{stat} uses the shared roll, {label}");
    }
    int drawn=die.Sides.Count;
    for(int i=0;i<4;i++)Equal(roll+6,Concentration(surge),$"repeated concentration queries reuse the roll, {label}");
    Equal(drawn,die.Sides.Count,$"querying concentration never draws again, {label}");
    surge.OnTurnOff();AllStatsZero(caster,"turn-off removes every modifier");
    surge.OnTurnOn();surge.OnTurnOff();surge.OnTurnOn();
    Equal(drawn,die.Sides.Count,$"refresh cycles (OnTurnOff/OnTurnOn) never draw again, {label}");
    Equal(roll+6,Total(caster,StatType.AdditionalAttackBonus),$"the refreshed effect keeps the same value, {label}");
}
die.Script();

// ---- B. an extra OnTurnOn without OnTurnOff (reapply, area change, load) does not stack ---------------------------------------------
{
    var caster=Caster(false,true);die.Script(3);
    var surge=Apply(caster,caster,Guids.SpiritSurgeAbility);
    surge.OnTurnOn();surge.OnTurnOn();
    Equal(3+6,Total(caster,StatType.AdditionalAttackBonus),"a repeated OnTurnOn does not stack the bonus");
    Equal(3+6,Total(caster,StatType.AdditionalDamage),"a repeated OnTurnOn does not stack a non-Marshal stat");
    Equal(1,die.Sides.Count,"a repeated OnTurnOn does not draw again");
    Equal(1,CombatLog.Reports.Count(r=>r.Unit==caster),"and does not write a second log line");
}

// ---- C. each cast, and each character, has its own roll -------------------------------------------------------------------
{
    var a=Caster(false,false,name:"A");die.Script(2);
    var first=Apply(a,a,Guids.SpiritSurgeAbility);
    Equal(2+6,Total(a,StatType.AdditionalAttackBonus),"first cast");
    first.OnTurnOff();
    die.Script(5);
    var second=Apply(a,a,Guids.SpiritSurgeAbility);   // removal and recast: a new buff instance
    Equal(5+6,Total(a,StatType.AdditionalAttackBonus),"a new cast rolls again");
    Equal(5+6,Concentration(second),"and its concentration follows the new roll");
    die.Script(1,4);
    var b=Caster(false,false,name:"B");var c=Caster(false,false,name:"C");
    var forB=Apply(b,b,Guids.SpiritSurgeAbility);var forC=Apply(c,c,Guids.SpiritSurgeAbility);
    Equal(1+6,Total(b,StatType.AdditionalAttackBonus),"character B keeps its own roll");
    Equal(4+6,Total(c,StatType.AdditionalAttackBonus),"character C is not polluted by B");
    Equal(1+6,Concentration(forB),"B concentration");Equal(4+6,Concentration(forC),"C concentration");
    // Orders cast by A on two allies: two buff instances, two rolls, both with A's Marshal term.
    die.Script(2,6);
    var ally1=new UnitEntityData{CharacterName="Ally1"};var ally2=new UnitEntityData{CharacterName="Ally2"};
    var o1=Apply(a,ally1,Guids.MarshalMarshalsOrdersAbility);var o2=Apply(a,ally2,Guids.MarshalMarshalsOrdersAbility);
    Equal(2+6,Total(ally1,StatType.AdditionalAttackBonus),"first ally roll");
    Equal(6+6,Total(ally2,StatType.AdditionalAttackBonus),"second ally has an independent roll");
    o1.OnTurnOff();Equal(6+6,Total(ally2,StatType.AdditionalAttackBonus),"ending one effect leaves the other untouched");
    o2.OnTurnOff();second.OnTurnOff();
    die.Script();
}

// ---- D. save and load: the roll comes back, it is never rerolled -------------------------------------------------------------
{
    var caster=Caster(false,true,hardened:true,name:"Saver");die.Script(3,6);
    var surge=Apply(caster,caster,Guids.SpiritSurgeAbility);
    Equal(2,die.Sides.Count,"2d8 draws exactly two dice");
    var saved=SavedData(surge);
    Check(saved!=null,"the component keeps per-instance runtime data that the game can save");
    string json=JsonConvert.SerializeObject(saved,resolver);
    surge.OnTurnOff();                                    // the unit leaves the scene while saving
    var loadedData=JsonConvert.DeserializeObject(json,saved.GetType(),resolver);
    var reloaded=new MediumSpiritSurgeComponent{Owner=caster,Context=surge.Context};
    reloaded.GetType().GetMethod("LoadData").Invoke(reloaded,new[]{loadedData});
    die.Script(1,1);                                      // any draw after loading would show up here
    reloaded.OnTurnOn();
    Equal(0,die.Sides.Count,"loading a save draws no dice");
    Equal(9+6,Total(caster,StatType.AdditionalAttackBonus),"the loaded roll (3 + 6) is applied to the Marshal stat");
    Equal(9+6,Total(caster,StatType.AdditionalDamage),"and to every other stat");
    Equal(9+6,Concentration(reloaded),"and to concentration");
    Equal(1,CombatLog.Reports.Count(r=>r.Unit==caster),"the log line is not repeated after loading");
    Same("3,6",string.Join(",",CombatLog.Reports.Last(r=>r.Unit==caster).Report.Roll.Results),"the report shows the individual dice");
    // The runtime data holds the roll only, no numbers of any character on shared blueprint state.
    Check(!json.Contains("Caster")&&!json.Contains("Saver"),"the saved data holds only the roll");
    reloaded.OnTurnOff();

    // A 0.3.3 save: no runtime data at all. The first use rolls once, later uses reuse that roll.
    var legacy=Caster(false,false,name:"OldSave");die.Script(4);
    var old=new MediumSpiritSurgeComponent{Owner=legacy,Context=new(){MaybeCaster=legacy,SourceAbility=BlueprintTool.Get<BlueprintAbility>(Guids.SpiritSurgeAbility)}};
    Check(SavedData(old)==null,"a component loaded from an old save has no runtime data");
    old.OnTurnOn();old.OnTurnOff();old.OnTurnOn();Concentration(old);
    Equal(1,die.Sides.Count,"an old save rolls exactly once on first use");
    Equal(4+6,Total(legacy,StatType.AdditionalAttackBonus),"and applies that roll");
    old.OnTurnOff();die.Script();

    // Damaged data (values outside the dice, mismatched lengths) is replaced by one fresh roll, never trusted.
    foreach(var broken in new[]{new{Sides=new[]{6},Results=new[]{7}},new{Sides=new[]{8,8},Results=new[]{3}},new{Sides=new int[0],Results=new int[0]},new{Sides=(int[])null,Results=(int[])null}}){
        var victim=Caster(false,false,level:1,name:"Broken");die.Script(2);
        var component=new MediumSpiritSurgeComponent{Owner=victim,Context=new(){MaybeCaster=victim,SourceAbility=BlueprintTool.Get<BlueprintAbility>(Guids.SpiritSurgeAbility)}};
        var data=Activator.CreateInstance(saved.GetType());
        saved.GetType().GetField("Version").SetValue(data,1);saved.GetType().GetField("Sides").SetValue(data,broken.Sides);saved.GetType().GetField("Results").SetValue(data,broken.Results);
        component.GetType().GetMethod("LoadData").Invoke(component,new[]{data});
        component.OnTurnOn();
        Equal(1,die.Sides.Count,"damaged data is rerolled once");
        Equal(2+6,Total(victim,StatType.AdditionalAttackBonus),"and the fresh roll is used");
        component.OnTurnOff();die.Script();
    }
}

// ---- E. the same roll, different additions: terms come from the granting medium, are listed, and add up ----------------------
{
    var granter=Caster(false,true,name:"A",rank:2,focus:1);die.Script(4);
    var self=Apply(granter,granter,Guids.SpiritSurgeAbility);
    var report=CombatLog.Reports.Last(r=>r.Unit==granter).Report;
    Same("1d10",report.Roll.Notation,"dice type in the report");   // level 20 default
    Equal(4,report.Roll.Total,"the report carries the drawn die");
    var plain=report.Lines.Where(l=>!l.IsConcentration&&l.MarshalOwner==null).ToArray();
    var boosted=report.Lines.Single(l=>l.MarshalOwner!=null&&!l.IsConcentration&&!l.Stats.Any(MarshalSpiritRules.AttributeChecks.Contains));
    var conc=report.Lines.Single(l=>l.IsConcentration);
    Check(plain.Length==0,"stats without a Marshal term are listed with the roll only");
    Check(boosted.Stats.OrderBy(x=>x).SequenceEqual(MarshalSpiritRules.SurgeStats.Concat(new[]{StatType.AdditionalDamage,StatType.Initiative}).Distinct().OrderBy(x=>x)),"stats with the Marshal term are listed together");
    Same("A",boosted.MarshalOwner,"the Marshal term is named after the granting medium");
    Equal(2,boosted.MarshalSpiritBonus,"spirit bonus term");Equal(1,boosted.MarshalFocus,"spirit focus term");
    Equal(4+2+1,boosted.Total,"listed total = roll + spirit bonus + focus");
    foreach(var line in report.Lines.Where(l=>!l.IsConcentration))foreach(var stat in line.Stats.Where(s=>!MarshalSpiritRules.AttributeChecks.Contains(s)))Equal(line.Total,Total(granter,stat),$"the logged total of {stat} is the modifier that was applied");
    Equal(conc.Total,Concentration(self),"the logged concentration is the value the provider returns");
    Equal(1,CombatLog.Reports.Count(r=>r.Unit==granter),"one line per effect instance");
    self.OnTurnOff();

    // A receiver's own Marshal attunement never replaces the granter's numbers; each side keeps its own terms.
    var receiver=Caster(false,false,name:"B",rank:1,focus:3);die.Script(6);
    var orders=Apply(granter,receiver,Guids.MarshalMarshalsOrdersAbility);
    var forReceiver=CombatLog.Reports.Last(r=>r.Unit==receiver).Report;
    var withTerm=forReceiver.Lines.First(l=>l.MarshalOwner!=null);
    Same("A",withTerm.MarshalOwner,"the ally's log names the granting medium");
    Equal(2,withTerm.MarshalSpiritBonus,"the granter's spirit bonus, not the receiver's");Equal(1,withTerm.MarshalFocus,"the granter's focus, not the receiver's");
    Equal(6+3,Total(receiver,StatType.AdditionalAttackBonus),"receiver total = shared roll + granter terms");
    Equal(6,forReceiver.Roll.Total,"the receiver's report shows the receiver's roll");
    orders.OnTurnOff();

    // Legendary Marshal: fixed 1d6, no terms of any kind. Hardened Soul does not turn it into 2d8.
    var legendaryCaster=Caster(false,true,hardened:true,name:"L",rank:5,focus:2);die.Script(5);
    var legendary=Apply(legendaryCaster,legendaryCaster,Guids.MarshalLegendaryMarshalAbility);
    Check(die.Sides.SequenceEqual(new[]{6}),"Legendary Marshal draws one d6");
    var lr=CombatLog.Reports.Last(r=>r.Unit==legendaryCaster).Report;
    Same("1d6",lr.Roll.Notation,"Legendary Marshal dice type");
    Check(lr.Lines.All(l=>l.MarshalOwner==null),"Legendary Marshal adds no attunement or focus term");
    Equal(5,Total(legendaryCaster,StatType.AdditionalAttackBonus),"Legendary bonus is the die alone");
    Equal(5,Concentration(legendary),"Legendary concentration is the die alone");
    legendary.OnTurnOff();die.Script();

    // Prowler keeps its own d6 while Hardened Soul keeps two d8 (explicit capstone wins over Prowler).
    foreach(var (prowler,hardened,expected) in new[]{(true,false,new[]{6}),(false,true,new[]{8,8}),(true,true,new[]{8,8})}){
        var unit=Caster(prowler,false,hardened,name:"P");die.Script(1,1);
        var effect=Apply(unit,unit,Guids.SpiritSurgeAbility);
        Check(die.Sides.SequenceEqual(expected),$"dice for prowler={prowler}, hardened={hardened}");
        effect.OnTurnOff();die.Script();
    }
}

// ---- F. a failing log never changes the effect -------------------------------------------------------------------------
{
    CombatLog.Throw=true;
    var caster=Caster(false,false,name:"Quiet");die.Script(3);
    var surge=Apply(caster,caster,Guids.SpiritSurgeAbility);
    CombatLog.Throw=false;
    Equal(3+6,Total(caster,StatType.AdditionalAttackBonus),"a failing log line leaves the bonus in place");
    Equal(3+6,Concentration(surge),"and the concentration");
    surge.OnTurnOff();
}
// 0.9.1: union coverage and exactly one term for overlaps, including raw attributes.
foreach(var ability in new[]{Guids.SpiritSurgeAbility,Guids.MarshalMarshalsOrdersAbility}) {
    var caster=Caster(false,true,rank:2,focus:1); die.Script(4);
    var effect=Apply(caster,caster,ability);
    foreach(var stat in MarshalSpiritRules.SurgeStats.Concat(new[]{StatType.AdditionalDamage,StatType.Initiative}).Distinct()) {
        Equal(7,Total(caster,stat),"self union gives die plus marshal rank and focus to "+stat);
        Equal(1,caster.Stats.GetStat(stat).Modifiers.Count,"overlapping channels apply only once to "+stat);
    }
    foreach(var attr in MarshalSpiritRules.AttributeChecks) {
        var check=new Kingmaker.RuleSystem.Rules.RuleSkillCheck {StatType=attr};
        typeof(MediumSpiritSurgeComponent).GetMethod("OnEventAboutToTrigger").Invoke(effect,new object[]{check});typeof(MediumSpiritSurgeComponent).GetMethod("OnEventAboutToTrigger").Invoke(effect,new object[]{check});
        Equal(7,check.Bonus.Total,"attribute check gets full surge once: "+attr);
        Equal(0,Total(caster,attr),"actual attribute score remains unchanged: "+attr);
    }
    foreach(var skill in MarshalSpiritRules.SurgeStats) {
        var check=new Kingmaker.RuleSystem.Rules.RuleSkillCheck {StatType=skill};typeof(MediumSpiritSurgeComponent).GetMethod("OnEventAboutToTrigger").Invoke(effect,new object[]{check});
        Equal(0,check.Bonus.Total,"skill handler never duplicates already-applied stat modifier: "+skill);
    }
    Equal(1,die.Sides.Count,"union uses one saved die");effect.OnTurnOff();AllStatsZero(caster,"union cleanup");
    var ally=new UnitEntityData();die.Script(2);var order=Apply(caster,ally,Guids.MarshalMarshalsOrdersAbility);
    Equal(0,Total(ally,StatType.AdditionalDamage),"ally Orders does not borrow Champion's extra category");
    var allyCheck=new Kingmaker.RuleSystem.Rules.RuleSkillCheck {StatType=StatType.Wisdom};typeof(MediumSpiritSurgeComponent).GetMethod("OnEventAboutToTrigger").Invoke(order,new object[]{allyCheck});
    Equal(5,allyCheck.Bonus.Total,"ally attributes use granting Marshal terms");order.OnTurnOff();
}

Console.WriteLine($"PASS: {checks} real SpiritSurge component checks over explicit game API doubles (single draw per instance, save/load, report vs. applied modifiers); not Unity gameplay.");

sealed class ScriptedDie:Random {
    private readonly Queue<int> script=new();
    public readonly List<int> Sides=new();
    public void Script(params int[] results){script.Clear();Sides.Clear();foreach(var r in results)script.Enqueue(r);}
    // System.Random.Next(min, max) is exclusive of max: faces are max-1.
    public override int Next(int minValue,int maxValue){int faces=maxValue-1;Sides.Add(faces);return script.Count>0?script.Dequeue():faces;}
}
sealed class OptInResolver:DefaultContractResolver {
    protected override IList<JsonProperty> CreateProperties(Type type,MemberSerialization memberSerialization)=>base.CreateProperties(type,MemberSerialization.OptIn);
}
