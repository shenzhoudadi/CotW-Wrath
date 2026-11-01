// Explicit game API doubles. The native rules reproduced here are transcribed from the shipped Assembly-CSharp
// (decompiled): BlueprintProgression.CalcLevel, LevelUpHelper.UpdateProgression / AddFeaturesFromProgression,
// ApplyClassMechanics.ApplyProgressions, FeatureCollection.PrepareFactForAttach and Feature.SetSource / AddRank /
// RemoveRank. The test program anchors the two rules that decide this bug to the real IL. Not Unity gameplay.
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.UnitLogic;
using BlueprintCore.Utils;

namespace Kingmaker.Blueprints
{
    public class BlueprintScriptableObject { public string name; public string AssetGuid; public override string ToString() => name ?? AssetGuid; }
}
namespace Kingmaker.Blueprints.Classes
{
    public class BlueprintFeatureBase : BlueprintScriptableObject { }
    public class BlueprintFeature : BlueprintFeatureBase { public int Ranks = 1; }
    public class LevelEntry { public int Level; public List<BlueprintFeatureBase> Features = new(); }
    public class BlueprintCharacterClass : BlueprintScriptableObject { public BlueprintProgression Progression; }
    public class BlueprintProgression : BlueprintFeature
    {
        public List<BlueprintCharacterClass> Classes = new();
        public LevelEntry[] LevelEntries = new LevelEntry[0];
        public bool GiveFeaturesForPreviousLevels;
        // Native: if (m_Classes.Empty() && m_Archetypes.Empty()) return unit.Progression.CharacterLevel; ... sum of class levels.
        public int CalcLevel(UnitDescriptor unit)
        {
            if (Classes.Count == 0) return unit.Progression.CharacterLevel;
            int total = 0;
            foreach (var c in Classes) total += Math.Max(0, unit.Progression.GetClassLevel(c));
            return total;
        }
    }
}
namespace Kingmaker.Blueprints.Classes.Selection
{
    public interface IFeatureSelection { }
    public class BlueprintFeatureSelection : BlueprintFeature, IFeatureSelection { }
}
namespace Kingmaker.UnitLogic
{
    public readonly struct FeatureSource
    {
        public readonly BlueprintScriptableObject Blueprint;
        public FeatureSource(BlueprintScriptableObject blueprint) { Blueprint = blueprint; }
        public static implicit operator FeatureSource(BlueprintProgression p) => new(p);
        public static implicit operator FeatureSource(BlueprintCharacterClass c) => new(c);
    }
    public readonly struct SourceAndLevel
    {
        public readonly BlueprintScriptableObject Blueprint; public readonly int Level;
        public SourceAndLevel(FeatureSource source, int level) { Blueprint = source.Blueprint; Level = level; }
    }
    public class Feature
    {
        public BlueprintFeature Blueprint; public int Rank = 1; public int SourceLevel;
        private BlueprintScriptableObject m_Source; private List<SourceAndLevel> m_RankToSource;
        public BlueprintProgression SourceProgression => m_Source as BlueprintProgression;
        public List<SourceAndLevel> RankToSource => m_RankToSource;
        public void AddRank() { if (Rank >= Blueprint.Ranks) return; Rank++; }
        public void RemoveRank() { Rank--; }
        // Native Feature.SetSource (rank bookkeeping only exists for multi-rank features).
        public void SetSource(FeatureSource source, int level)
        {
            m_Source = source.Blueprint; SourceLevel = level;
            if (Blueprint.Ranks <= 1) return;
            m_RankToSource ??= new();
            while (m_RankToSource.Count < Rank) m_RankToSource.Add(new SourceAndLevel(source, level));
        }
        public void SetSourceSameAsLastRankToSourceValue()
        {
            var last = m_RankToSource != null && m_RankToSource.Count > 0 ? m_RankToSource[^1] : default;
            if (last.Blueprint != null) { m_Source = last.Blueprint; SourceLevel = last.Level; }
        }
        // Simulates a save written before rank sources were recorded.
        public void ForgetRankSources() { m_RankToSource = null; }
    }
    public class FeatureCollection { public readonly List<Feature> Enumerable = new(); }
    public class ProgressionData
    {
        public int Level; public BlueprintProgression Blueprint;
        public List<LevelEntry> ArchetypeAdds = new();
        // Native ProgressionData.LevelEntries: the blueprint's entries with the archetype's changes applied.
        public LevelEntry[] LevelEntries => Enumerable.Range(1, 20).Select(GetLevelEntry).Where(e => e.Features.Count > 0).ToArray();
        public LevelEntry GetLevelEntry(int level)
        {
            var entry = new LevelEntry { Level = level };
            foreach (var e in Blueprint.LevelEntries.Where(x => x.Level == level)) entry.Features.AddRange(e.Features);
            foreach (var e in ArchetypeAdds.Where(x => x.Level == level)) entry.Features.AddRange(e.Features);
            return entry;
        }
    }
    public class UnitProgressionData
    {
        public readonly FeatureCollection Features = new();
        public readonly Dictionary<BlueprintCharacterClass, int> ClassLevels = new();
        public readonly Dictionary<BlueprintProgression, ProgressionData> Progressions = new();
        public int CharacterLevel => ClassLevels.Values.Sum();
        public int GetClassLevel(BlueprintCharacterClass c) => ClassLevels.GetValueOrDefault(c);
        public ProgressionData GetProgression(BlueprintProgression p) => Progressions.GetValueOrDefault(p);
        public ProgressionData SureProgressionData(BlueprintProgression p)
        {
            if (!Progressions.TryGetValue(p, out var data)) Progressions[p] = data = new ProgressionData { Blueprint = p };
            return data;
        }
    }
    public class UnitDescriptor { public Kingmaker.EntitySystem.Entities.UnitEntityData Unit; public UnitProgressionData Progression = new(); }
}
namespace Kingmaker.EntitySystem.Entities
{
    public class UnitEntityData
    {
        public UnitDescriptor Descriptor;
        public UnitEntityData() { Descriptor = new() { Unit = this }; }
        // Native FeatureCollection.PrepareFactForAttach: an existing feature gains a rank (capped), otherwise it is attached.
        public Feature AddFact(BlueprintFeature blueprint)
        {
            var existing = Descriptor.Progression.Features.Enumerable.FirstOrDefault(f => f.Blueprint == blueprint);
            if (existing != null) { existing.AddRank(); return existing; }
            var feature = new Feature { Blueprint = blueprint };
            Descriptor.Progression.Features.Enumerable.Add(feature);
            return feature;
        }
        // Native PrepareFactForDetach: a ranked feature loses one rank, a single rank detaches the fact.
        public void RemoveFact(BlueprintFeature blueprint)
        {
            var existing = Descriptor.Progression.Features.Enumerable.FirstOrDefault(f => f.Blueprint == blueprint);
            if (existing == null) return;
            if (existing.Rank >= 2) existing.RemoveRank(); else Descriptor.Progression.Features.Enumerable.Remove(existing);
        }
    }
}
namespace BlueprintCore.Utils
{
    public static class BlueprintTool
    {
        public static readonly Dictionary<string, BlueprintScriptableObject> Assets = new();
        public static T Get<T>(string id) where T : BlueprintScriptableObject => Assets.GetValueOrDefault(id) as T;
        public static T GetOrCreate<T>(string id, Func<T> create) where T : BlueprintScriptableObject
        {
            if (Assets.TryGetValue(id, out var have)) return (T)have;
            var made = create(); made.AssetGuid = id; Assets[id] = made; return made;
        }
    }
}
namespace BlueprintCore.Utils.Types
{
    public class LevelEntryBuilder
    {
        public readonly List<(int Level, string[] Guids)> Entries = new();
        public static LevelEntryBuilder New() => new();
        public LevelEntryBuilder AddEntry(int level, params string[] guids) { Entries.Add((level, guids)); return this; }
    }
}
namespace BlueprintCore.Blueprints.CustomConfigurators.Classes
{
    // Records the calls that decide progression level handling; the other setters are irrelevant to it.
    public class ProgressionConfigurator
    {
        private readonly BlueprintProgression progression = new();
        public static List<BlueprintProgression> Configured = new();
        public static ProgressionConfigurator New(string name, string guid)
        {
            var configurator = new ProgressionConfigurator();
            configurator.progression.name = name; configurator.progression.AssetGuid = guid;
            return configurator;
        }
        public ProgressionConfigurator SetClasses(params string[] classes) { progression.Classes = classes.Select(id => BlueprintTool.Get<BlueprintCharacterClass>(id)).ToList(); return this; }
        public ProgressionConfigurator AddToClasses(params string[] classes) { progression.Classes.AddRange(classes.Select(id => BlueprintTool.Get<BlueprintCharacterClass>(id))); return this; }
        public ProgressionConfigurator SetLevelEntries(BlueprintCore.Utils.Types.LevelEntryBuilder builder)
        {
            progression.LevelEntries = builder.Entries.Select(e => new LevelEntry
            {
                Level = e.Level,
                Features = e.Guids.Select(g => (BlueprintFeatureBase)BlueprintTool.GetOrCreate(g, () => new BlueprintFeature { name = g })).ToList()
            }).ToArray();
            return this;
        }
        public ProgressionConfigurator SetAllowNonContextActions(bool v) => this;
        public ProgressionConfigurator SetHideInUI(bool v) => this;
        public ProgressionConfigurator SetHideInCharacterSheetAndLevelUp(bool v) => this;
        public ProgressionConfigurator SetHideNotAvailibleInUI(bool v) => this;
        public ProgressionConfigurator SetRanks(int v) => this;
        public ProgressionConfigurator SetReapplyOnLevelUp(bool v) => this;
        public ProgressionConfigurator SetIsClassFeature(bool v) => this;
        public ProgressionConfigurator SetForAllOtherClasses(bool v) => this;
        public BlueprintProgression Configure() { BlueprintTool.Assets[progression.AssetGuid] = progression; Configured.Add(progression); return progression; }
    }
}
namespace UnityModManagerNet { public class UnityModManager { public class ModEntry { public class ModLogger { public static readonly List<string> Lines = new(); public void Log(string text) => Lines.Add(text); public void LogException(string text, Exception e) => Lines.Add(text + ": " + e.Message); } } } }
namespace MediumClass.Medium.Spirits.Archmage { public static class Archmage { public const string ArchmageName = "Archmage"; } }
namespace MediumClass.Utils { public static class Logging { public static UnityModManagerNet.UnityModManager.ModEntry.ModLogger GetLogger(string name) => new(); } }
// The features whose ConfigureEnabled the production progression triggers: only their registration order matters here.
namespace MediumClass.Medium
{
    internal static class MediumPsychicBlueprints { internal static void ConfigureEnabled() { } }
    internal static class MediumProficiencies { public static void ConfigureEnabled() { } }
    internal static class SpiritPower { public static void ConfigureEnabled() { } }
    internal static class SpiritSurge { public static void ConfigureEnabled() { } }
    internal static class SpiritBonus { public static void ConfigureEnabled() { } }
    internal static class ChannelSpirit { public static void ConfigureEnabled() { } }
    internal static class Influence { public static void ConfigureEnabled() { } }
    internal static class SharedSeance { public static void ConfigureEnabled() { } }
    internal static class Propitation { public static void ConfigureEnabled() { } }
    internal static class TranceOfThree { public static void ConfigureEnabled() { } }
    internal static class SpiritMastery { public static void ConfigureEnabled() { } }
    internal static class MediumSpellcasterFeat { public static void ConfigureEnabled() { } }
    internal static class WeakerSpiritChannel { public static void ConfigureEnabled() { } }
    internal static class AstralBeacon { public static void ConfigureEnabled() { } }
}
