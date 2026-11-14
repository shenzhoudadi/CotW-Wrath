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
        internal static LocalizedString Text(string token, string suffix, string value) {
            string key = "cotw-wrath.psychic.full." + token + "." + suffix; Strings[key] = value;
            PutFallback(key, QlTexts01460.Resolve(key,value)); return new LocalizedString { Key = key };
        }
        // GetText(key,false) conflates an absent key with an intentionally empty
        // translation. The actual native dictionary distinguishes both cases.
        private static void PutFallback(string key, string value) {
            var pack = LocalizationManager.CurrentPack;
            if (pack == null) return;
            lock (Get(pack, "m_Lock"))
                if (!((IDictionary)Get(pack, "m_Strings")).Contains(key)) pack.PutString(key, value);
        }
        internal static string PeekText(BlueprintUnitFact fact, bool description = false) {
            if (fact == null) return null;
            var ls = Get(fact, description ? "m_Description" : "m_DisplayName") as LocalizedString;
            if (ls == null) return null;
            if (!string.IsNullOrEmpty(ls.Key) && Strings.TryGetValue(ls.Key, out var cached)) return cached;
            if (ls.Shared?.String != null && !string.IsNullOrEmpty(ls.Shared.String.Key) && Strings.TryGetValue(ls.Shared.String.Key, out cached)) return cached;
            return null;
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
        internal void Enable() { foreach (var bp in Assets.Values) bp.OnEnable(); }
        internal static void ApplyQLLocalization()=>QlTexts01460.Apply(Strings);
    }
}
