using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype
{
    // Deterministic identities are tied to tokens, never localized/display names.
    internal sealed class PsychicAssets
    {
        internal readonly BlueprintsCache Cache;
        internal readonly Dictionary<string, BlueprintScriptableObject> Assets = new Dictionary<string, BlueprintScriptableObject>();
        private static readonly Dictionary<string, string> Strings = new Dictionary<string, string>();
        internal PsychicAssets(BlueprintsCache cache) { Cache = cache; }
        internal static BlueprintGuid Id(string token) {
            using (var md5 = MD5.Create()) return BlueprintGuid.Parse(string.Concat(md5.ComputeHash(Encoding.UTF8.GetBytes("CotW.Wrath.Psychic.v1/" + token)).Select(b => b.ToString("x2"))));
        }
        internal static void Set(object obj, string field, object value) {
            var target = AccessTools.Field(obj.GetType(), field) ?? throw new MissingFieldException(obj.GetType().FullName, field);
            // CLR reflection can coerce unrelated enum types; Unity Mono rejects
            // them. Require the declared enum type on every runtime.
            if (target.FieldType.IsEnum && (value == null || value.GetType() != target.FieldType))
                throw new ArgumentException("Wrong enum type for " + obj.GetType().FullName + "." + field);
            target.SetValue(obj, value);
        }
        internal static void SetEnum(object obj, string field, string name) {
            var target = AccessTools.Field(obj.GetType(), field) ?? throw new MissingFieldException(obj.GetType().FullName, field);
            if (!target.FieldType.IsEnum) throw new ArgumentException("Expected enum field: " + field);
            Set(obj, field, Enum.Parse(target.FieldType, name));
        }
        internal static object Get(object obj, string field) => AccessTools.Field(obj.GetType(), field).GetValue(obj);
        internal static T Ref<T>(SimpleBlueprint obj) where T : BlueprintReferenceBase, new() {
            var reference = BlueprintReferenceBase.CreateTyped<T>(obj);
            // Construction can refer to another staged asset before the transaction is committed.
            AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(reference, obj, null); return reference;
        }
        internal T Native<T>(string guid) where T : SimpleBlueprint => Cache.Load(BlueprintGuid.Parse(guid)) as T ?? throw new InvalidOperationException("Psychic native dependency missing: " + guid);
        internal T Find<T>(string token) where T : BlueprintScriptableObject => (T)Assets[token];
        internal static string Key(string token, string suffix) => "cotw-wrath.psychic.full." + token + "." + suffix;
        internal static LocalizedString Text(string token, string suffix, string value) {
            string key = Key(token, suffix); Strings[key] = value;
            PutFallback(key, QlTexts01460.Resolve(key,value)); return new LocalizedString { Key = key };
        }
        // Only for keys this project composes at runtime: unlike Text(), which
        // must never overwrite a translation, these are recomputed from the live
        // pack so they follow both the player's language and late providers.
        internal static void Override(string key, string value) {
            var pack = LocalizationManager.CurrentPack; if (pack == null) return;
            lock (Get(pack, "m_Lock")) pack.PutString(key, value);
        }
        // GetText(key,false) conflates an absent key with an intentionally empty
        // translation. The actual native dictionary distinguishes both cases.
        private static void PutFallback(string key, string value) {
            var pack = LocalizationManager.CurrentPack;
            if (pack == null) return;
            lock (Get(pack, "m_Lock"))
                if (!((IDictionary)Get(pack, "m_Strings")).Contains(key)) pack.PutString(key, value);
        }
        internal static void ApplyLocalization() {
            foreach (var pair in Strings) PutFallback(pair.Key, pair.Value);
        }
        internal T Add<T>(T obj, string token, string name, string description) where T : BlueprintScriptableObject {
            obj.AssetGuid = Id(token); obj.name = "CotWWrathPsychic" + token;
            if (obj is BlueprintUnitFact fact) {
                Set(fact, "m_DisplayName", Text(token, "name", name)); Set(fact, "m_Description", Text(token, "description", description));
            }
            PsychicPresentation.Initialize(obj, token);
            Assets.Add(token, obj); return obj;
        }
        internal T Clone<T>(T original, string token, string name, string description) where T : BlueprintScriptableObject {
            var cloned = (T)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(original, null);
            cloned.ComponentsArray = original.ComponentsArray.Select(c => (BlueprintComponent)DeepCopy(c, new Dictionary<object, object>(ObjectIdentity.Instance))).ToArray();
            return Add(cloned, token, name, description);
        }
        // A copy graph is keyed by object identity, never resource/value equality.
        // WeakResourceLink.Equals compares only AssetId, even across UnitViewLink
        // and PrefabLink; empty IDs otherwise substitute an incompatible clone.
        internal static IEqualityComparer<object> IdentityComparer => ObjectIdentity.Instance;
        private sealed class ObjectIdentity : IEqualityComparer<object> {
            internal static readonly ObjectIdentity Instance = new ObjectIdentity();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
        private static object DeepCopy(object value, Dictionary<object, object> seen) {
            if (value == null) return null; var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is decimal || value is DateTime || value is TimeSpan || value is Guid || value is string || value is SimpleBlueprint || value is UnityEngine.Object || value is Type || value is Delegate) return value;
            if (seen.TryGetValue(value, out var old)) return old;
            if (value is Array array) {
                var result = Array.CreateInstance(type.GetElementType(), array.Length); seen[value] = result;
                for (int i = 0; i < array.Length; i++) result.SetValue(DeepCopy(array.GetValue(i), seen), i); return result;
            }
            if (value is IList list && type.IsGenericType) {
                var result = (IList)Activator.CreateInstance(type); seen[value] = result;
                foreach (var element in list) result.Add(DeepCopy(element, seen)); return result;
            }
            var clone = AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(value, null); seen[value] = clone;
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!field.IsStatic) field.SetValue(clone, DeepCopy(field.GetValue(value), seen));
            return clone;
        }
        // For private runtime assets whose non-component data must not alias a
        // native blueprint. Blueprint/prefab assets remain read-only references.
        internal static T CopyData<T>(T value) => (T)DeepCopy(value, new Dictionary<object, object>(ObjectIdentity.Instance));
        internal BlueprintBuff Buff(string token, string name, string description, params BlueprintComponent[] components)
            => Add(new BlueprintBuff { Stacking = StackingType.Replace, ComponentsArray = components }, token, name, description);
        internal BlueprintFeature Feature(string token, string name, string description, params BlueprintComponent[] components)
            => Add(new BlueprintFeature { Ranks = 1, ComponentsArray = components }, token, name, description);
        internal BlueprintAbility Ability(string token, string name, string description, AbilityType type, UnitCommand.CommandType action,
            AbilityRange range, params BlueprintComponent[] components) => Add(new BlueprintAbility {
                Type = type, ActionType = action, Range = range, CanTargetSelf = range == AbilityRange.Personal,
                CanTargetFriends = range != AbilityRange.Personal, CanTargetEnemies = range != AbilityRange.Personal,
                EffectOnEnemy = AbilityEffectOnUnit.Harmful, EffectOnAlly = AbilityEffectOnUnit.Helpful,
                ComponentsArray = components
            }, token, name, description);
        internal static ActionList Actions(params GameAction[] actions) => new ActionList { Actions = actions };
        internal static AbilityEffectRunAction Run(params GameAction[] actions) => new AbilityEffectRunAction { name = "PsychicEffects", Actions = Actions(actions) };
        internal static AbilityEffectRunAction Save(Kingmaker.EntitySystem.Stats.SavingThrowType save, params GameAction[] actions)
            => new AbilityEffectRunAction { name = "PsychicSavedEffects", SavingThrowType = save, Actions = Actions(actions) };
        internal static ContextActionApplyBuff Apply(BlueprintBuff buff, int duration, DurationRate rate = DurationRate.Rounds,
            bool toCaster = false, bool fromSpell = true) {
            var action = new ContextActionApplyBuff { name = "PsychicApply" + buff.name, ToCaster = toCaster, IsFromSpell = fromSpell,
                DurationValue = new ContextDurationValue { Rate = rate, DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = duration } };
            Set(action, "m_Buff", Ref<BlueprintBuffReference>(buff)); return action;
        }
        internal static ContextActionConditionalSaved Saved(GameAction[] succeed, GameAction[] failed)
            => new ContextActionConditionalSaved { Succeed = Actions(succeed), Failed = Actions(failed) };
        internal static AddFacts Facts(params BlueprintUnitFact[] facts) {
            var component = new AddFacts { name = "PsychicGrantedFacts" }; Set(component, "m_Facts", facts.Select(Ref<BlueprintUnitFactReference>).ToArray()); return component;
        }
        internal void Enable() { foreach (var bp in Assets.Values) bp.OnEnable(); PublishHexBridges(); }
        internal static void ApplyQLLocalization()=>QlTexts01460.Apply(Strings);

        // Hex Vulnerability and Amplified Hex identify native hexes through frozen
        // GUID tables. Hexes this project authors carry deterministic token GUIDs,
        // so they are published from the built graph instead: a ShamanHex feature
        // grants abilities through AddFacts, and an inverted single-fact
        // AbilityTargetHasFact on one of those abilities is the 24-hour target
        // lock that Hex Vulnerability may bypass. Enable() is the last step of a
        // successful transaction, so a rolled-back module publishes nothing and a
        // future hex is picked up without another GUID list.
        internal void PublishHexBridges()
        {
            var abilities = Assets.Values.OfType<BlueprintAbility>()
                .GroupBy(a => a.AssetGuid).ToDictionary(g => g.Key, g => g.First());
            var buffs = Assets.Values.OfType<BlueprintBuff>()
                .GroupBy(b => b.AssetGuid).ToDictionary(g => g.Key, g => g.First());
            foreach (var feature in Assets.Values.OfType<BlueprintFeature>())
            {
                if (feature.Groups == null || !feature.Groups.Contains(FeatureGroup.ShamanHex)) continue;
                foreach (var facts in (feature.ComponentsArray ?? Array.Empty<BlueprintComponent>()).OfType<AddFacts>())
                {
                    var granted = Get(facts, "m_Facts") as BlueprintUnitFactReference[];
                    if (granted == null) continue;
                    foreach (var reference in granted)
                    {
                        if (reference == null || !abilities.TryGetValue(reference.Guid, out var ability)) continue;
                        SpellResourceFeats.PublishHex(ability);
                        foreach (var check in (ability.ComponentsArray ?? Array.Empty<BlueprintComponent>()).OfType<AbilityTargetHasFact>())
                        {
                            if (!check.Inverted) continue;
                            var locked = Get(check, "m_CheckedFacts") as BlueprintUnitFactReference[];
                            if (locked == null || locked.Length != 1 || locked[0] == null) continue;
                            if (buffs.TryGetValue(locked[0].Guid, out var cooldown))
                                PsychicHexVulnerability.Publish(ability, cooldown);
                        }
                    }
                }
            }
        }

        // The SpiritTools wandering/unsworn pools are AbilityApplyFact components
        // whose granted list lives inside the component (m_Facts), not in an array
        // field on the blueprint. Writing only the blueprint's own reference fields
        // silently matched nothing, which is why authored hexes never appeared in
        // Wandering Hex and why Lore's hexes were missing from the same pools.
        // This appends to every fact list a blueprint actually carries.
        internal static int AppendFacts(BlueprintScriptableObject bp, IEnumerable<SimpleBlueprint> facts)
        {
            if (bp == null || facts == null) return 0;
            var wanted = facts.Where(f => f != null).ToArray();
            if (wanted.Length == 0) return 0;
            int added = 0;
            foreach (var component in bp.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                if (component == null) continue;
                var field = AccessTools.Field(component.GetType(), "m_Facts");
                if (field == null || !field.FieldType.IsArray) continue;
                var element = field.FieldType.GetElementType();
                if (element == null || !typeof(BlueprintReferenceBase).IsAssignableFrom(element)) continue;
                var current = (field.GetValue(component) as Array) ?? Array.CreateInstance(element, 0);
                var list = current.Cast<BlueprintReferenceBase>().Where(r => r != null).ToList();
                foreach (var fact in wanted)
                {
                    if (list.Any(r => r.Guid == fact.AssetGuid)) continue;
                    list.Add(BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(fact));
                    added++;
                }
                var next = Array.CreateInstance(element, list.Count);
                for (int i = 0; i < list.Count; i++) next.SetValue(list[i], i);
                field.SetValue(component, next);
            }
            return added;
        }

        // Read back the fact list a blueprint grants, for verification.
        internal static HashSet<BlueprintGuid> GrantedFacts(BlueprintScriptableObject bp)
        {
            var result = new HashSet<BlueprintGuid>();
            foreach (var component in bp?.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                if (component == null) continue;
                var field = AccessTools.Field(component.GetType(), "m_Facts");
                if (field?.GetValue(component) is Array array)
                    foreach (var item in array)
                        if (item is BlueprintReferenceBase reference) result.Add(reference.Guid);
            }
            return result;
        }
    }
}
