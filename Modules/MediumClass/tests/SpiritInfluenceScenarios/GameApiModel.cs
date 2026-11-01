// Explicit game API doubles for the linked production components. This suite is
// a mechanics/wiring test, not Unity gameplay. Native behaviour reproduced here:
// AddFact builds a fresh runtime delegate per fact (CreateRuntimeFactComponent),
// blueprint references are cached per target, and RemoveFact detaches one rank.
using System.Linq;

namespace Kingmaker.Blueprints
{
    public class ComponentNameAttribute(string name) : Attribute { public string Name => name; }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AllowedOnAttribute(Type type, bool flag) : Attribute { public Type Type => type; public bool Flag => flag; }
    public class AllowMultipleComponentsAttribute : Attribute { }
    public class BlueprintComponent { }
    public class BlueprintCharacterClassReference
    {
        public Kingmaker.Blueprints.Classes.BlueprintCharacterClass Blueprint;
        public Kingmaker.Blueprints.Classes.BlueprintCharacterClass Get() => Blueprint;
    }
    public class BlueprintFeatureReference
    {
        public Kingmaker.Blueprints.Classes.BlueprintFeature Blueprint;
        public Kingmaker.Blueprints.Classes.BlueprintFeature Get() => Blueprint;
    }
}
namespace Kingmaker.Blueprints.Facts
{
    public class BlueprintFact { public string Name; }
    public class BlueprintUnitFact : BlueprintFact { }
}
namespace Kingmaker.Blueprints.Classes
{
    public class BlueprintCharacterClass { public string Name; }
    public class BlueprintFeature : Kingmaker.Blueprints.Facts.BlueprintFact
    {
        public int Ranks = 1;
        public List<object> Components = new();
        public IEnumerable<T> GetComponents<T>() => Components.OfType<T>();
        // Native CreateRuntimeFactComponent builds one fresh delegate per fact.
        public List<Func<Kingmaker.EntitySystem.Entities.UnitEntityData, Kingmaker.EntitySystem.EntityFact,
            Kingmaker.UnitLogic.UnitFactComponentDelegate>> RuntimeFactories = new();
    }
}
namespace Kingmaker.Blueprints.JsonSystem
{
    public class TypeIdAttribute(string id) : Attribute { public string Id => id; }
}
namespace Kingmaker.UnitLogic.Buffs.Blueprints
{
    public class BlueprintBuff : Kingmaker.Blueprints.Facts.BlueprintFact
    {
        public List<object> Components = new();
        public IEnumerable<T> GetComponents<T>() => Components.OfType<T>();
    }
}
namespace Kingmaker.EntitySystem
{
    public class EntityFact
    {
        public Kingmaker.Blueprints.Facts.BlueprintFact Blueprint;
        public bool IsDisposed;
        public bool IsTurnedOn = true;
        public bool IsActive = true;
        public List<object> Components = new();
        public void CallComponents<T>(Action<T> callback)
        {
            foreach (var component in Components.OfType<T>()) callback(component);
        }
    }
    public class FactCollection
    {
        public readonly Dictionary<Kingmaker.Blueprints.Facts.BlueprintFact, EntityFact> Facts = new();
        public EntityFact Get(Kingmaker.Blueprints.Facts.BlueprintFact blueprint) => Facts.GetValueOrDefault(blueprint);
    }
}
namespace Kingmaker.EntitySystem.Entities
{
    public class UnitEntityData
    {
        public Kingmaker.UnitLogic.UnitDescriptor Descriptor;
        public Kingmaker.UnitLogic.Progression Progression = new();
        public Kingmaker.EntitySystem.Stats.Stats Stats = new();
        public Kingmaker.UnitLogic.Buffs.BuffCollection Buffs = new();
        public Kingmaker.EntitySystem.FactCollection Facts = new();
        private readonly Dictionary<Type, Kingmaker.UnitLogic.UnitPart> parts = new();
        public UnitEntityData() { Descriptor = new() { Unit = this }; }
        public T Get<T>() where T : Kingmaker.UnitLogic.UnitPart => (T)parts.GetValueOrDefault(typeof(T));
        public T Ensure<T>() where T : Kingmaker.UnitLogic.UnitPart, new()
        {
            if (Get<T>() is T part) return part;
            var created = new T { Owner = this };
            parts[typeof(T)] = created;
            return created;
        }
        public bool HasFact(Kingmaker.Blueprints.Classes.BlueprintFeature feature) => Facts.Get(feature)?.IsActive == true;
        public Kingmaker.EntitySystem.EntityFact AddFact(Kingmaker.Blueprints.Classes.BlueprintFeature feature)
        {
            if (Facts.Get(feature) is { } existing) return existing;
            var fact = new Kingmaker.UnitLogic.UnitFact { Blueprint = feature };
            foreach (var factory in feature.RuntimeFactories) fact.Components.Add(factory(this, fact));
            Facts.Facts.Add(feature, fact);
            return fact;
        }
        public void RemoveFact(Kingmaker.Blueprints.Classes.BlueprintFeature feature)
        {
            if (Facts.Get(feature) is { } fact)
            {
                // Native fact removal turns the fact off before detaching it.
                foreach (var component in fact.Components.OfType<Kingmaker.UnitLogic.UnitFactComponentDelegate>().ToArray())
                    component.OnTurnOff();
                fact.IsDisposed = true;
                fact.IsActive = false;
                Facts.Facts.Remove(feature);
            }
        }
        public void RemoveFact(Kingmaker.EntitySystem.EntityFact fact)
        {
            if (fact?.Blueprint is Kingmaker.Blueprints.Classes.BlueprintFeature feature && Facts.Get(feature) == fact)
                RemoveFact(feature);
        }
    }
}
namespace Kingmaker.UnitLogic
{
    public class UnitPart { public Kingmaker.EntitySystem.Entities.UnitEntityData Owner; }
    public class UnitDescriptor { public Kingmaker.EntitySystem.Entities.UnitEntityData Unit; }
    public class UnitFact : Kingmaker.EntitySystem.EntityFact { }
    public class UnitFactComponentDelegate
    {
        public Kingmaker.EntitySystem.Entities.UnitEntityData Owner;
        public UnitFact Fact;
        public readonly object Runtime = new();
        public virtual void OnTurnOn() { }
        public virtual void OnTurnOff() { }
        public virtual void OnActivate() { }
        public virtual void OnDeactivate() { }
    }
    public class UnitFactComponentDelegate<TData> : UnitFactComponentDelegate where TData : class, new()
    {
        private TData savedData;
        public TData Data => savedData ??= new();
    }
    public class FeatureCollection
    {
        public readonly Dictionary<Kingmaker.Blueprints.Classes.BlueprintFeature, int> Counts = new();
        public int GetRank(Kingmaker.Blueprints.Classes.BlueprintFeature feature) => Counts.GetValueOrDefault(feature);
        public int GetRank(Kingmaker.Blueprints.BlueprintFeatureReference feature) => GetRank(feature.Get());
        public void AddFact(Kingmaker.Blueprints.Classes.BlueprintFeature feature) =>
            Counts[feature] = Math.Min(feature.Ranks, GetRank(feature) + 1);
    }
    public class Progression { public FeatureCollection Features = new(); }
}
namespace Kingmaker.UnitLogic.Buffs
{
    public class Buff : Kingmaker.UnitLogic.UnitFact
    {
        public new Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff Blueprint;
        public new bool IsActive = true;
        public new List<object> Components = new();
        public Kingmaker.UnitLogic.Mechanics.MechanicsContext Context;
        public new void CallComponents<T>(Action<T> callback)
        {
            foreach (var component in Components.OfType<T>()) callback(component);
        }
    }
    public class BuffCollection
    {
        public List<Buff> Enumerable = new();
        public Buff GetBuff(Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff blueprint) =>
            Enumerable.FirstOrDefault(b => b.Blueprint == blueprint);
        public void RemoveFact(Buff buff) => Enumerable.Remove(buff);
        public Buff AddBuff(Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff blueprint,
            Kingmaker.EntitySystem.Entities.UnitEntityData caster, object context)
        {
            var buff = new Buff { Blueprint = blueprint };
            Enumerable.Add(buff);
            return buff;
        }
    }
}
namespace Kingmaker.UnitLogic.Mechanics
{
    public class MechanicsContext { public Kingmaker.EntitySystem.Entities.UnitEntityData MaybeCaster; }
}
namespace Kingmaker.EntitySystem.Stats
{
    public enum StatType {Strength,Dexterity,Constitution,Intelligence,Wisdom,Charisma,AdditionalAttackBonus, AdditionalDamage, SaveFortitude, SaveWill, SkillKnowledgeArcana, SkillKnowledgeWorld, SaveReflex, SkillPersuasion, SkillUseMagicDevice}
    public class Stats
    {
        public Dictionary<StatType, Stat> Values = new();
        public Stat GetStat(StatType type)
        {
            if (!Values.TryGetValue(type, out var stat)) Values[type] = stat = new();
            return stat;
        }
    }
    public class Stat
    {
        public List<(int Amount, object Source, Kingmaker.Enums.ModifierDescriptor Descriptor)> Modifiers = new();
        public void AddModifier(int amount, object source, Kingmaker.Enums.ModifierDescriptor descriptor) =>
            Modifiers.Add((amount, source, descriptor));
        public void RemoveModifiersFrom(object source) => Modifiers.RemoveAll(m => m.Source == source);
    }
}
namespace Kingmaker.Enums { public enum ModifierDescriptor { UntypedStackable, Penalty } }
namespace Kingmaker.Enums.Damage
{
    public enum DamageEnergyType { Fire, Cold, Electricity, Acid, NegativeEnergy }
}
namespace Kingmaker.RuleSystem
{
    public enum DiceType { D3, D4, D6, D8, D10, D12, D20 }
    public struct DiceFormula
    {
        public int RollsCount; public DiceType Dice;
        public DiceFormula(int count, DiceType dice) { RollsCount = count; Dice = dice; }
    }
}
namespace Kingmaker.RuleSystem.Rules.Damage
{
    public class RulePrepareDamage
    {
        public DamageBundle DamageBundle = new();
        public Kingmaker.EntitySystem.Entities.UnitEntityData Target;
        public List<EnergyDamage> Added = new();
        public void Add(EnergyDamage damage) => Added.Add(damage);
    }
    public class EnergyDamage
    {
        public Kingmaker.RuleSystem.DiceFormula Formula;
        public Kingmaker.Enums.Damage.DamageEnergyType Energy;
        public EnergyDamage(Kingmaker.RuleSystem.DiceFormula formula, Kingmaker.Enums.Damage.DamageEnergyType energy)
        { Formula = formula; Energy = energy; }
    }
    public class DamageBundle { public object Weapon; }
}
namespace Kingmaker.PubSubSystem
{
    public interface IRulebookHandler<in T>
    {
        void OnEventAboutToTrigger(T evt);
        void OnEventDidTrigger(T evt);
    }
    public interface IInitiatorRulebookHandler<in T> : IRulebookHandler<T> { }
    public interface ISubscriber { }
    public interface IInitiatorRulebookSubscriber : ISubscriber { }
}
namespace Kingmaker.UnitLogic.Abilities
{
    public class AbilityData { public Kingmaker.UnitLogic.UnitDescriptor Caster; }
}
namespace Kingmaker.UnitLogic.Abilities.Components.Base
{
    public interface IAbilityRestriction
    {
        bool IsAbilityRestrictionPassed(Kingmaker.UnitLogic.Abilities.AbilityData ability);
        string GetAbilityRestrictionUIText();
    }
}
namespace Kingmaker
{
    public class Game
    {
        public static Game Instance = new();
        public Player Player = new();
    }
    public class Player { public List<Kingmaker.EntitySystem.Entities.UnitEntityData> ActiveCompanions = new(); }
}
namespace Newtonsoft.Json { public class JsonPropertyAttribute : Attribute { } }
namespace Kingmaker.UnitLogic.FactLogic { }
namespace Kingmaker.QA { }
namespace Kingmaker.Utility { }
namespace Kingmaker.UnitLogic.FactLogic { public class AddContextStatBonus { public class ComponentData { } } }
namespace Kingmaker.Blueprints.Classes.Selection { }
namespace Kingmaker.Blueprints.Classes.Spells { }
namespace Kingmaker.Blueprints { }
namespace BlueprintCore.Utils
{
    public static class BlueprintTool
    {
        public static readonly Dictionary<string, object> Assets = new();
        // The game caches references per target; keep one instance per (type, id).
        private static readonly Dictionary<(Type, string), object> Refs = new();
        public static T Get<T>(string id) => (T)Assets[id];
        public static T GetRef<T>(string id)
        {
            var key = (typeof(T), id);
            if (Refs.TryGetValue(key, out var cached)) return (T)cached;
            object value = typeof(T).Name switch
            {
                nameof(Kingmaker.Blueprints.BlueprintCharacterClassReference) =>
                    new Kingmaker.Blueprints.BlueprintCharacterClassReference { Blueprint = Get<Kingmaker.Blueprints.Classes.BlueprintCharacterClass>(id) },
                nameof(Kingmaker.Blueprints.BlueprintFeatureReference) =>
                    new Kingmaker.Blueprints.BlueprintFeatureReference { Blueprint = Get<Kingmaker.Blueprints.Classes.BlueprintFeature>(id) },
                _ => throw new NotSupportedException(typeof(T).Name)
            };
            Refs[key] = value;
            return (T)value;
        }
    }
}
namespace MediumClass.Medium
{
    // The legendary tier-inheritance rule itself is exercised by MythicRegistration
    // against the real MythicSpirits; here a simple per-entry focus is enough.
    internal static class MultiSpiritRules
    {
        internal static int EffectiveSpiritFocus(NewUnitParts.UnitPartMedium medium,
            Kingmaker.Blueprints.BlueprintCharacterClassReference spirit) =>
            spirit?.Get() != null && medium.Spirits.TryGetValue(spirit, out var entry) ? entry.SpiritFocus : 0;
    }
}
namespace MediumClass.Medium.NewUnitParts
{
    public class UnitPartMedium : Kingmaker.UnitLogic.UnitPart
    {
        public readonly Dictionary<Kingmaker.Blueprints.BlueprintCharacterClassReference, SpiritEntry> Spirits = new();
        public Kingmaker.Blueprints.BlueprintCharacterClassReference PrimarySpirit;
        public List<Kingmaker.Blueprints.BlueprintCharacterClassReference> AdditionalSpirits = new();
        public IEnumerable<Kingmaker.Blueprints.BlueprintCharacterClassReference> ActiveSpiritClasses =>
            new[] { PrimarySpirit }.Concat(AdditionalSpirits ?? Enumerable.Empty<Kingmaker.Blueprints.BlueprintCharacterClassReference>())
                .Where(s => s?.Get() != null && Spirits.ContainsKey(s)).Distinct();
        public bool IsActiveSpirit(Kingmaker.Blueprints.BlueprintCharacterClassReference spirit) => ActiveSpiritClasses.Contains(spirit);
    }
    public class SpiritEntry
    {
        public SpiritStatEntry SpiritBonus = new();
        public int SpiritFocus;
    }
    public class SpiritStatEntry
    {
        public Kingmaker.EntitySystem.Stats.StatType[] Stats;
        public Kingmaker.Blueprints.BlueprintFeatureReference SpiritBonusFeature;
    }
}
namespace MediumClass.Medium.NewComponents.AbilitySpecific
{
    public class MediumSpiritComponent
    {
        public Kingmaker.EntitySystem.Stats.StatType[] Stats;
        public Kingmaker.EntitySystem.Stats.StatType[] Penalties;
    }
}

// ---- 0.3.4 Death Curse toggle action, restriction and migration ---------------------------------------------------
namespace Kingmaker.Blueprints
{
    public class BlueprintBuffReference
    {
        public Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff Blueprint;
        public Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff Get() => Blueprint;
    }
}
namespace Kingmaker.UnitLogic.Mechanics.Actions
{
    public abstract class ContextAction
    {
        public Kingmaker.UnitLogic.Mechanics.MechanicsContext Context;
        public abstract string GetCaption();
        public abstract void RunAction();
    }
}
namespace Kingmaker.UnitLogic
{
    public static class UnitHelper
    {
        // Extension used by the production action: adds a permanent buff owned by the unit.
        public static Kingmaker.UnitLogic.Buffs.Buff AddBuff(this Kingmaker.EntitySystem.Entities.UnitEntityData unit,
            Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff blueprint, Kingmaker.UnitLogic.Mechanics.MechanicsContext parentContext, TimeSpan? duration = null)
        {
            var buff = new Kingmaker.UnitLogic.Buffs.Buff { Blueprint = blueprint, Context = parentContext };
            unit.Buffs.Enumerable.Add(buff);
            return buff;
        }
    }
}
namespace Kingmaker.PubSubSystem
{
    public interface IGlobalSubscriber : ISubscriber { }
    public interface IWarningNotificationUIHandler : IGlobalSubscriber
    {
        void HandleWarning(string text, bool addToLog = true);
    }
    public static class EventBus
    {
        public sealed class Warnings : IWarningNotificationUIHandler
        {
            public readonly List<string> Shown = new();
            public bool Throw;
            public void HandleWarning(string text, bool addToLog = true) { if (Throw) throw new InvalidOperationException("UI failed"); Shown.Add(text); }
        }
        public static Warnings Handler = new();
        public static void RaiseEvent<T>(Action<T> action) where T : class, IGlobalSubscriber => action((T)(object)Handler);
    }
}
namespace Owlcat.Runtime.Core.Logging
{
    public class LogChannel
    {
        public static LogChannel Default = new();
        public List<string> Errors = new();
        public void Error(object source, string message, params object[] args) => Errors.Add(message);
    }
}
namespace MediumClass.Utils
{
    // Buff removal by the migration goes through BuffCollection.RemoveFact of the model.
}

namespace Kingmaker.EntitySystem.Stats {
    public static class StatTypeHelper {
        public static readonly StatType[] Saves = {StatType.SaveFortitude,StatType.SaveReflex,StatType.SaveWill};
        public static readonly StatType[] Skills = ((StatType[])System.Enum.GetValues(typeof(StatType)))
            .Where(s => s.ToString().StartsWith("Skill", System.StringComparison.Ordinal)).ToArray();
    }
}
