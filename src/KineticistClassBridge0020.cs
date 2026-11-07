// S-KINETIC, contract K01-HAVOCKER-20261106-r1.
// Native blueprint identities/formulas stay owned by Owlcat. CotW source: MIT,
// Holic75/KingmakerRebalance; see LICENSE-CotW.txt.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Stages a caller-supplied, finite graph. It never discovers the blueprint
    // library, attaches an archetype, edits a native pool or patches HasFact.
    // F-0020 must use this one map for its element, infusion and ability graphs.
    internal sealed class KineticistClassBridge0020
    {
        internal const string Contract = "K01-HAVOCKER-20261106-r1";
        internal const string WitchId = "1b9873f1e7bfe5449bc84d03e9c8e3cc";
        internal const string KineticistId = "42a455d9ec1ad924d889272429eb8391";
        internal const string ElementPoolId = "1f3a15a3ae8a5524ab8b97f469bf4e3d";
        internal const string WrongElementPoolId = "bb24cc01319528849b09a3ae8eec0b31";
        internal const string BlastRankId = "93efbde2764b5504e98e6824cab3d27c";
        internal const string BlastProgressionId = "30a5b8cf728bd4a4d8d90fc4953e322e";
        internal const string BurnFeatureId = "57e3577a0eb53294e9d7cc649d5239a3";
        internal const string GatherFeatureId = "71f526b1d4b50b94582b0b9cbe12b0e0";
        internal const string BridgeToken = "HavockerKineticBridgeFeature0020";
        internal static string CloneToken(BlueprintGuid original) => "Havocker0020.Clone/" + original.ToString().ToLowerInvariant();

        readonly PsychicAssets assets;
        readonly Dictionary<BlueprintGuid, BlueprintScriptableObject> originals = new Dictionary<BlueprintGuid, BlueprintScriptableObject>();
        readonly Dictionary<BlueprintGuid, string> originalDefinitions = new Dictionary<BlueprintGuid, string>();
        readonly Dictionary<BlueprintGuid, BlueprintScriptableObject> copies = new Dictionary<BlueprintGuid, BlueprintScriptableObject>();
        readonly Dictionary<BlueprintGuid, string> definitions = new Dictionary<BlueprintGuid, string>();
        bool sealedGraph, published;
        internal KineticistClassBridge0020(BlueprintsCache cache) { assets = new PsychicAssets(cache ?? throw new ArgumentNullException(nameof(cache))); }
        internal IReadOnlyDictionary<BlueprintGuid, BlueprintScriptableObject> Copies => copies;
        internal BlueprintScriptableObject Resolve(BlueprintGuid guid) => assets.Assets.Values.FirstOrDefault(a => a.AssetGuid == guid) ?? assets.Cache.Load(guid) as BlueprintScriptableObject;
        internal bool IsStaged(BlueprintScriptableObject blueprint) => assets.Assets.Values.Any(a => ReferenceEquals(a, blueprint));
        internal T Copy<T>(string originalId) where T : BlueprintScriptableObject => (T)copies[BlueprintGuid.Parse(originalId)];
        internal T Own<T>(string token) where T : BlueprintScriptableObject => assets.Find<T>(token);
        internal T Add<T>(T blueprint, string token, string title, string description) where T : BlueprintScriptableObject {
            if (sealedGraph || assets.Cache.Load(Id(token)) != null || assets.Assets.ContainsKey(token)) throw new InvalidOperationException("Private kinetic identity unavailable: " + token);
            return assets.Add(blueprint, token, title, description);
        }

        // One donor can only have one private identity. The named-token override
        // is for the exact identities frozen in K01, never display-name lookup.
        internal T Stage<T>(string originalId, string token = null) where T : BlueprintScriptableObject
        {
            if (sealedGraph) throw new InvalidOperationException("The kinetic graph is already sealed.");
            var original = assets.Native<T>(originalId);
            if (original.AssetGuid == BlueprintGuid.Parse(WrongElementPoolId)) throw new InvalidOperationException("The wizard elemental spell feat pool is not a kinetic focus pool.");
            token = token ?? CloneToken(original.AssetGuid);
            if (copies.TryGetValue(original.AssetGuid, out var prior)) {
                if (!(prior is T) || prior.AssetGuid != Id(token)) throw new InvalidOperationException("A kinetic donor was assigned two private identities: " + originalId);
                return (T)prior;
            }
            if (assets.Cache.Load(Id(token)) != null || assets.Assets.Values.Any(x => x.AssetGuid == Id(token)))
                throw new InvalidOperationException("A private kinetic identity is occupied: " + token);
            string originalDefinition = Definition(original);
            // Reading native Name/Description warms LocalizedString caches on
            // the donor. Preserve its localized authored data instead of
            // materializing a display string while taking the graph snapshot.
            var copy = assets.Clone(original, token, original.name, "");
            // Clone the authored container fields as well as the components.
            // Runtime caches must be rebuilt, not inherited from a native asset.
            foreach (var field in AuthoredFields(original.GetType())) {
                if (field.Name == "Components") continue;
                field.SetValue(copy, CopyData(field.GetValue(original)));
            }
            originals.Add(original.AssetGuid, original); copies.Add(original.AssetGuid, copy);
            originalDefinitions.Add(original.AssetGuid, originalDefinition);
            return copy;
        }

        internal static IEnumerable<FieldInfo> AuthoredFields(Type type)
        {
            for (var current = type; current != null && current != typeof(SimpleBlueprint) && current != typeof(object); current = current.BaseType)
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!field.IsStatic && !field.IsNotSerialized && (field.IsPublic || field.IsDefined(typeof(UnityEngine.SerializeField), true) || field.IsDefined(typeof(Newtonsoft.Json.JsonPropertyAttribute), true)))
                        yield return field;
        }

        // Snapshot the native serialization contract recursively: public,
        // SerializeField and JsonProperty fields. Tooltip/LevelEntry/action
        // caches (m_ShouldProcess, m_FeaturesList, m_IsAOE, etc.) are private
        // nonserialized runtime state, not authored rules. Keep every authored
        // action, ordered reference, value and concrete type in the comparison.
        static object DefinitionValue(object value) => DefinitionValue(value, new HashSet<object>(IdentityComparer));
        static object DefinitionValue(object value, HashSet<object> path) {
            if (value == null) return null;
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || value is decimal || value is TimeSpan || value is Guid || value is DateTime || value is BlueprintGuid || value is SimpleBlueprint || value is BlueprintReferenceBase) return value;
            if (value is UnityEngine.Object && !(value is BlueprintComponent))
                return new object[] { "read-only Unity asset", type.FullName, RuntimeHelpers.GetHashCode(value) };
            if (!type.IsValueType && !path.Add(value)) return new object[] { "authored cycle", type.FullName };
            try {
                if (value is IEnumerable sequence) return new object[] { type.FullName, sequence.Cast<object>().Select(v => DefinitionValue(v, path)).ToArray() };
                return new object[] { type.FullName, AuthoredFields(type).Select(f => new object[] { f.DeclaringType.FullName, f.Name, DefinitionValue(f.GetValue(value), path) }).ToArray() };
            } finally { if (!type.IsValueType) path.Remove(value); }
        }
        static string Definition(BlueprintScriptableObject bp) => Migration01426.Definition(new object[] {
            bp.AssetGuid, bp.name, AuthoredFields(bp.GetType()).Select(f => new object[] { f.DeclaringType.FullName, f.Name, DefinitionValue(f.GetValue(bp)) }).ToArray(), DefinitionValue(bp.ComponentsArray)
        }, IdentityComparer);

        // This traversal is only over independently copied data, never through
        // a blueprint/reference into the native library. Value containers are
        // written back so references inside structs are handled too.
        object Rewrite(object value, IDictionary<BlueprintGuid, BlueprintScriptableObject> map, HashSet<object> seen)
        {
            if (value == null) return null;
            if (value is BlueprintReferenceBase reference) {
                if (map.TryGetValue(reference.Guid, out var target)) {
                    Set(reference, "deserializedGuid", target.AssetGuid); Set(reference, "guid", target.AssetGuid.ToString());
                    AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(reference, target, null);
                }
                return reference;
            }
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || value is decimal || value is TimeSpan || value is Guid || value is DateTime || value is BlueprintGuid || value is SimpleBlueprint || value is UnityEngine.Object || value is Type || value is Delegate) return value;
            if (!type.IsValueType && !seen.Add(value)) return value;
            if (value is Array array) { for (int i = 0; i < array.Length; i++) array.SetValue(Rewrite(array.GetValue(i), map, seen), i); return value; }
            if (value is IList list) { for (int i = 0; i < list.Count; i++) list[i] = Rewrite(list[i], map, seen); return value; }
            // Do not traverse component/element owner backreferences or caches.
            for (var current = type; current != null && current != typeof(object) && current != typeof(BlueprintComponent) && current != typeof(Element); current = current.BaseType)
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(f => !f.IsStatic))
                    field.SetValue(value, Rewrite(field.GetValue(value), map, seen));
            return value;
        }

        internal void Seal()
        {
            if (sealedGraph) { Verify(); return; }
            var witch = assets.Native<BlueprintCharacterClass>(WitchId);
            var map = new Dictionary<BlueprintGuid, BlueprintScriptableObject>(copies) { [BlueprintGuid.Parse(KineticistId)] = witch };
            foreach (var copy in assets.Assets.Values) {
                // Root entry exclusions refer to the real forbidden class,
                // whereas kinetic formulas/growth inside the graph use Witch.
                IDictionary<BlueprintGuid, BlueprintScriptableObject> ownMap = copy is BlueprintArchetype ? copies : map;
                foreach (var field in AuthoredFields(copy.GetType()))
                    field.SetValue(copy, Rewrite(field.GetValue(copy), ownMap, new HashSet<object>(IdentityComparer)));
                Rewrite(copy.ComponentsArray, ownMap, new HashSet<object>(IdentityComparer));
                foreach (var parameters in copy.GetComponents<ContextCalculateAbilityParamsBasedOnClass>()) {
                    if (parameters.CharacterClass != witch) continue;
                    parameters.UseKineticistMainStat = true; parameters.StatType = StatType.Intelligence;
                }
                Set(copy, "m_AllElements", SpellBatch01447.Elements(copy.ComponentsArray, new HashSet<object>(IdentityComparer)).ToList());
                if (copy is BlueprintAbility ability) SpellBatch01447.ResetAbilityCaches(ability);
                PsychicRepairComponents.UniqueNames(copy);
            }
            // Native OnEnable normalizes ability data and builds owned element
            // caches. Freeze that initialized private graph before publication,
            // rather than comparing uninitialized data with normalized data.
            assets.Enable();
            sealedGraph = true;
            foreach (var copy in assets.Assets.Values) definitions.Add(copy.AssetGuid, Definition(copy));
        }

        internal void Verify()
        {
            if (!sealedGraph) throw new InvalidOperationException("The kinetic graph has not been sealed.");
            foreach (var pair in originals)
                if (!ReferenceEquals(assets.Cache.Load(pair.Key), pair.Value) || Definition(pair.Value) != originalDefinitions[pair.Key])
                    throw new InvalidOperationException("Native kinetic donor identity or definition changed: " + pair.Key + "; " + Migration01426.Difference(originalDefinitions[pair.Key], Definition(pair.Value)));
            foreach (var copy in assets.Assets.Values) {
                if (Definition(copy) != definitions[copy.AssetGuid]) throw new InvalidOperationException("Private kinetic definition changed: " + copy.name + "; " + Migration01426.Difference(definitions[copy.AssetGuid], Definition(copy)));
                var current = assets.Cache.Load(copy.AssetGuid);
                if (published ? !ReferenceEquals(current, copy) : current != null) throw new InvalidOperationException("Private kinetic cache identity changed: " + copy.name);
            }
        }

        // Registration is deliberately separate from class/menu attachment.
        // K02 callers can load and verify this slice without exposing an
        // unfinished archetype to player selection. No global state is edited.
        internal void Publish() => PublishConnected(null, null);
        // The one publication transaction includes its parent links. A failed
        // attachment rolls back only our inserts, never another provider's data.
        internal void PublishConnected(Action connect, Action disconnect)
        {
            if (!sealedGraph) Seal();
            Verify(); if (published) { connect?.Invoke(); return; }
            var inserted = new List<BlueprintScriptableObject>();
            try {
                foreach (var copy in assets.Assets.Values) {
                    if (assets.Cache.Load(copy.AssetGuid) != null) throw new InvalidOperationException("Private kinetic identity changed while publishing.");
                    inserted.Add(copy); assets.Cache.AddCachedBlueprint(copy.AssetGuid, copy);
                }
                published = true;
                connect?.Invoke();
                // OnEnable rebuilds element/component ownership, not authored rules.
                Verify();
            } catch {
                disconnect?.Invoke();
                foreach (var copy in inserted) if (ReferenceEquals(assets.Cache.Load(copy.AssetGuid), copy)) assets.Cache.RemoveCachedBlueprint(copy.AssetGuid);
                published = false; throw;
            }
        }

        internal static int Specialization(int witchLevel) => witchLevel >= 20 ? 4 : witchLevel >= 16 ? 3 : witchLevel >= 12 ? 2 : witchLevel >= 10 ? 1 : 0;
        internal static int SpellburnCredit(int slotLevel) {
            if (slotLevel < 2 || slotLevel > 9) throw new ArgumentOutOfRangeException(nameof(slotLevel));
            return slotLevel / 2;
        }
        internal static bool CanUseBridge(UnitDescriptor owner) => Havocker0020.IsHavocker(owner) && owner.Progression.GetClassLevel(ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(BlueprintGuid.Parse(KineticistId))) == 0;

        // Finite core manifest. F-0020 will add its finite element/infusion
        // manifest to the same builder before Seal; no second bridge is needed.
        internal static KineticistClassBridge0020 StageCore(BlueprintsCache cache)
        {
            var graph = new KineticistClassBridge0020(cache);
            graph.Stage<BlueprintFeature>(BlastRankId);
            graph.Stage<BlueprintProgression>(BlastProgressionId, "HavockerKineticBlastProgression0020");
            var nativeBurn = cache.Load(BlueprintGuid.Parse(BurnFeatureId)) as BlueprintFeature ?? throw new InvalidOperationException("Native burn feature missing.");
            var settings = nativeBurn.GetComponents<AddKineticistPart>().Single();
            foreach (var row in new[] { new { Resource = settings.MaxBurn, Token = "HavockerBurnResource0020" }, new { Resource = settings.MaxBurnPerRound, Token = "HavockerBurnPerRoundResource0020" } }) {
                var resource = graph.Stage<BlueprintAbilityResource>(row.Resource.AssetGuid.ToString(), row.Token);
                // A fresh native amount struct has no stat/class increases.
                var amount = Activator.CreateInstance(Get(resource, "m_MaxAmount").GetType());
                Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>()); Set(amount, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>());
                Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>()); Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
                Set(amount, "BaseValue", 0); Set(resource, "m_MaxAmount", amount); Set(resource, "m_Min", 0);
            }
            graph.Stage<BlueprintFeature>(GatherFeatureId, "HavockerGatherPowerFeature0020");
            var gather = cache.Load(BlueprintGuid.Parse(GatherFeatureId)) as BlueprintFeature;
            foreach (var fact in gather.GetComponent<AddFacts>().Facts) {
                if (fact is BlueprintAbility ability) graph.Stage<BlueprintAbility>(ability.AssetGuid.ToString());
                else if (fact is BlueprintActivatableAbility toggle) graph.Stage<BlueprintActivatableAbility>(toggle.AssetGuid.ToString());
                else if (fact is BlueprintFeature feature) graph.Stage<BlueprintFeature>(feature.AssetGuid.ToString());
                else throw new InvalidOperationException("Unexpected native gather fact: " + fact?.AssetGuid + " / " + fact?.GetType().FullName);
            }
            foreach (var buff in new[] { settings.GatherPowerBuff1, settings.GatherPowerBuff2, settings.GatherPowerBuff3, settings.BladeActivatedBuff })
                graph.Stage<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(buff.AssetGuid.ToString());
            var bridge = graph.Stage<BlueprintFeature>(BurnFeatureId, BridgeToken);
            var safe = new HavockerOwnedKineticistPart0020();
            foreach (var field in AuthoredFields(typeof(AddKineticistPart))) field.SetValue(safe, CopyData(field.GetValue(settings)));
            safe.MainStat = StatType.Intelligence;
            // F-0020 adds the complete private blast list before sealing.
            Set(safe, "m_Blasts", Array.Empty<BlueprintAbilityReference>());
            bridge.ComponentsArray = bridge.ComponentsArray.Where(c => c is AddAbilityResources).Concat(new BlueprintComponent[] { safe }).ToArray();
            bridge.HideInUI = true; bridge.HideInCharacterSheetAndLevelUp = true;
            return graph;
        }
    }

    // Native AddKineticistPart.OnDeactivate removes by type without checking
    // ownership. This private subtype retains native Setup and protects a part
    // taken over by another provider, including across save suspension/reapply.
    [TypeId("6f52058f08d14c3cad57ad2c8714b8b6")]
    public sealed class HavockerOwnedKineticistPart0020 : AddKineticistPart
    {
        internal bool Owns(UnitPartKineticist part) => part != null && ReferenceEquals(Get(part, "m_Settings"), this);
        bool MaySetUp() {
            var part = Owner.Get<UnitPartKineticist>();
            if (!KineticistClassBridge0020.CanUseBridge(Owner) || part != null && !Owns(part)) {
                ModEntry.LogSpellDiagnostic("Havocker: incompatible kineticist ownership; private bridge disabled."); return false;
            }
            return true;
        }
        protected override void OnTurnOn() { if (MaySetUp()) base.OnTurnOn(); }
        protected override void OnPostLoad() { if (MaySetUp()) { Owner.Ensure<UnitPartKineticist>().Setup(this); Data.Initialized = true; } }
        protected override void OnDeactivate() {
            if (Owner?.Facts != null && Owner.Facts.List.Contains(Fact)) return; // temporary suspension
            if (Owns(Owner?.Get<UnitPartKineticist>())) base.OnDeactivate();
            else Data.Initialized = false;
        }
    }
}
