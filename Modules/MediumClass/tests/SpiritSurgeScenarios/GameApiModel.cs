// Narrow, explicit doubles for stat modification and the target/caster relationship.
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
namespace Kingmaker.Blueprints.JsonSystem { public class TypeIdAttribute:Attribute {public TypeIdAttribute(string s){}} }
namespace Kingmaker.Blueprints {
    public class BlueprintScriptableObject { public object[] Components=Array.Empty<object>();public IEnumerable<T> GetComponents<T>()=>Components.OfType<T>(); }
    public class BlueprintCharacterClassReference {public BlueprintCharacterClass Value;public BlueprintCharacterClass Get()=>Value;}
    public class BlueprintFeatureReference {public BlueprintFeature Value;public BlueprintFeature Get()=>Value;}
}
namespace Kingmaker.Blueprints.Classes {public class BlueprintFeature:BlueprintScriptableObject {}public class BlueprintCharacterClass:BlueprintScriptableObject{}}
namespace Kingmaker.UnitLogic.Abilities.Blueprints {public class BlueprintAbility:BlueprintScriptableObject{}}
namespace BlueprintCore.Utils {
    public static class BlueprintTool {
        public static readonly Dictionary<string,object> Assets=new();
        public static T Get<T>(string id)=>(T)Assets[id];
        public static T GetRef<T>(string id)=>(T)Assets[id+".ref"];
    }
}
namespace Kingmaker.EntitySystem {public class EntityFactComponent {public IDisposable RequestEventContext()=>null;} }
namespace Kingmaker.EntitySystem.Stats {
    public enum StatType {AdditionalAttackBonus, AdditionalDamage, Initiative, SaveWill, SkillPersuasion, SaveFortitude, SaveReflex, SkillUseMagicDevice,
        Strength,Dexterity,Constitution,Intelligence,Wisdom,Charisma,SkillAthletics,SkillMobility,SkillStealth,SkillThievery,SkillPerception,SkillKnowledgeArcana,SkillKnowledgeWorld,SkillLoreNature,SkillLoreReligion}
    public class Stat {
        // Like the game's ModifiableValue: a repeated AddModifier from one source stacks until it is removed.
        public readonly List<(object Source,int Amount)> Modifiers=new();
        public int Total=>Modifiers.Sum(m=>m.Amount);
        public void AddModifier(int amount,object source,Kingmaker.Enums.ModifierDescriptor descriptor)=>Modifiers.Add((source,amount));
        public void RemoveModifiersFrom(object source)=>Modifiers.RemoveAll(m=>m.Source==source);
    }
    public class Stats {private readonly Dictionary<StatType,Stat> values=new();public Stat GetStat(StatType type){if(!values.TryGetValue(type,out var stat))values[type]=stat=new();return stat;} }
}
namespace Kingmaker.Enums {public enum ModifierDescriptor{UntypedStackable}}
namespace Kingmaker.PubSubSystem {public interface IInitiatorRulebookHandler<T> {void OnEventAboutToTrigger(T evt);void OnEventDidTrigger(T evt);}}
namespace Kingmaker.RuleSystem.Rules {public class RuleSkillCheck {public StatType StatType;public Kingmaker.EntitySystem.Stats.Stat Bonus=new();}}
namespace Kingmaker.UnitLogic {
    public class UnitDescriptor {public UnitEntityData Unit;public bool Prowler,Hardened;}
    public class UnitPart {public UnitEntityData Owner;}
    public class FeatureCollection {public readonly Dictionary<BlueprintFeature,int> Ranks=new();public int GetRank(BlueprintFeature feature)=>Ranks.TryGetValue(feature,out int r)?r:0;}
    public class Progression {public FeatureCollection Features=new();}
    public class Context {public UnitEntityData MaybeCaster;public Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility SourceAbility;}
    public class UnitFactComponentDelegate {
        public UnitEntityData Owner;public Context Context;public object Runtime=new();
        public virtual void OnTurnOn(){}public virtual void OnTurnOff(){}
    }
    // Native runtime data: null until first used, saved with the fact, lost when the fact is disposed.
    public class UnitFactComponentDelegate<TData>:UnitFactComponentDelegate where TData:class,new() {
        private TData saved;
        public TData Data=>saved??=new();
        public TData MaybeData=>saved;
        public void LoadData(TData data)=>saved=data;
    }
}
namespace Kingmaker.EntitySystem.Entities {
    public class UnitEntityData {
        readonly Dictionary<Type,UnitPart> parts=new();public Kingmaker.EntitySystem.Stats.Stats Stats=new();public Progression Progression=new();public int Level=20;
        public UnitDescriptor Descriptor;public string CharacterName="Unit";
        public UnitEntityData(){Descriptor=new(){Unit=this};}
        public T Get<T>() where T:UnitPart=>parts.TryGetValue(typeof(T),out var v)?(T)v:null;
        public T Ensure<T>()where T:UnitPart,new(){if(Get<T>()is T p)return p;var value=new T{Owner=this};parts[typeof(T)]=value;return value;}
    }
}
namespace Kingmaker.Designers.Mechanics.Facts {public interface IConcentrationBonusProvider{int GetStaticConcentrationBonus(Kingmaker.EntitySystem.EntityFactComponent runtime);}}
namespace MediumClass.Medium.NewUnitParts {
    public class UnitPartMedium:UnitPart {
        public readonly Dictionary<BlueprintCharacterClassReference,SpiritEntry> Spirits=new();
        public readonly List<BlueprintCharacterClassReference> ActiveSpiritClasses=new();
        public bool IsActiveSpirit(BlueprintCharacterClassReference r)=>ActiveSpiritClasses.Contains(r);
        public class SpiritEntry {public SpiritStatEntry SpiritBonus=new();public int SpiritFocus;}
        public class SpiritStatEntry {public StatType[] Stats;public bool Concentration;public BlueprintFeatureReference SpiritBonusFeature;}
    }
}
namespace MediumClass.Medium.NewComponents.AbilitySpecific {public class MediumSpiritComponent{public StatType[] Stats;}}
namespace MediumClass.Medium {
    public static class HardenedSoul{public static bool HasFeature(UnitDescriptor unit)=>unit?.Hardened==true;}
    // Legendary tiers are not modelled here, so a spirit's focus is its own entry.
    internal static class MultiSpiritRules{internal static int EffectiveSpiritFocus(NewUnitParts.UnitPartMedium medium,BlueprintCharacterClassReference spirit)=>medium.Spirits.TryGetValue(spirit,out var entry)?entry.SpiritFocus:0;}
}
namespace MediumClass.Prowler {
    public static class ProwlerSpiritRules {
        public static bool IsProwler(UnitDescriptor unit)=>unit?.Prowler==true;
        public static int SpiritClassLevel(UnitEntityData unit)=>unit?.Level??0;
    }
}
namespace UnityModManagerNet {public class UnityModManager {public class ModEntry {public class ModLogger {public void LogException(string text,Exception e){}}}}}
namespace MediumClass.Utils {
    public static class Logging {public static UnityModManagerNet.UnityModManager.ModEntry.ModLogger GetLogger(string s)=>new();}
    // The on-screen line is UI only; the checks read the stat modifiers and the report the line is written from.
    internal static class CombatLog {
        internal static readonly List<(UnitEntityData Unit,MediumClass.Medium.SpiritSurgeReport Report)> Reports=new();
        internal static bool Throw;
        internal static void SpiritSurge(UnitEntityData unit,MediumClass.Medium.SpiritSurgeReport report){if(Throw)throw new InvalidOperationException("log failed");Reports.Add((unit,report));}
        // Signature used by 0.3.3, so the same program can run against the old component as a negative control.
        internal static readonly List<(UnitEntityData Unit,List<(StatType Stat,int Total)> Totals)> LegacyReports=new();
        internal static void SpiritSurge(UnitEntityData unit,IEnumerable<(StatType Stat,int Total)> totals)=>LegacyReports.Add((unit,totals.ToList()));
    }
}
namespace MediumClass.Medium.Spirits.Archmage {public static class Archmage {public const string ArchmageName="Archmage";}}

namespace Kingmaker.EntitySystem.Stats {
    public static class StatTypeHelper {
        public static readonly StatType[] Saves = {StatType.SaveFortitude,StatType.SaveReflex,StatType.SaveWill};
        public static readonly StatType[] Skills = ((StatType[])System.Enum.GetValues(typeof(StatType)))
            .Where(s => s.ToString().StartsWith("Skill", System.StringComparison.Ordinal)).ToArray();
    }
}
