using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // COTW-0117 / S-SPIRIT-0117. This is the ONLY isolation bridge for the
    // Spirit Whisperer package. It owns the private identity namespace, the
    // private resource shapes, the donor rebinding rules and the atomic
    // publication transaction. No global Harmony patch and no Shaman edit.
    internal static class SpiritClassBridge0117
    {
        internal const string Salt = "CotW.Wrath.SpiritWhisperer0117.v1/";
        internal const string AssetPrefix = "CotWWrathSpiritWhisperer0117";

        internal static BlueprintGuid Id(string token)
        {
            using (var md5 = MD5.Create())
                return BlueprintGuid.Parse(string.Concat(md5.ComputeHash(
                    Encoding.UTF8.GetBytes(Salt + token)).Select(b => b.ToString("x2"))));
        }

        internal static bool Has(object obj, string field) => AccessTools.Field(obj.GetType(), field) != null;
        internal static object Get(object obj, string field)
        {
            var f = AccessTools.Field(obj.GetType(), field) ?? throw new MissingFieldException(obj.GetType().FullName, field);
            return f.GetValue(obj);
        }
        internal static void Set(object obj, string field, object value)
        {
            var f = AccessTools.Field(obj.GetType(), field);
            if (f != null) { f.SetValue(obj, value); return; }
            var p = AccessTools.Property(obj.GetType(), field);
            if (p != null && p.GetSetMethod(true) != null) { p.SetValue(obj, value, null); return; }
            throw new MissingFieldException(obj.GetType().FullName, field);
        }
        internal static T Ref<T>(SimpleBlueprint obj) where T : BlueprintReferenceBase, new() => PsychicAssets.Ref<T>(obj);
        internal static LocalizedString Text(string token, string suffix, string value) => PsychicAssets.Text(token, suffix, value);

        internal static void Log(string line)
        {
            try { ModEntry.TraceNatureTarget(line); } catch { /* isolated host has no UMM logger */ }
            Console.WriteLine(line);
        }

        // A private factory deliberately does not reuse PsychicAssets.Id: the
        // frozen contract requires its own salt and its own asset name prefix.
        internal sealed class Factory
        {
            internal readonly BlueprintsCache Cache;
            internal readonly Dictionary<string, BlueprintScriptableObject> Assets = new Dictionary<string, BlueprintScriptableObject>();
            internal readonly List<string> Diagnostics = new List<string>();
            internal Factory(BlueprintsCache cache) { Cache = cache; }

            internal T Native<T>(string guid) where T : SimpleBlueprint
                => Cache.Load(BlueprintGuid.Parse(guid)) as T;

            internal T Find<T>(string token) where T : BlueprintScriptableObject => (T)Assets[token];

            internal bool Has(string token) => Assets.ContainsKey(token);

            internal T Add<T>(T obj, string token, string name, string description) where T : BlueprintScriptableObject
            {
                obj.AssetGuid = Id(token);
                obj.name = AssetPrefix + token;
                if (obj is BlueprintUnitFact fact)
                {
                    Set(fact, "m_DisplayName", Text(token, "name", name));
                    Set(fact, "m_Description", Text(token, "description", description));
                }
                if (Assets.ContainsKey(token)) throw new InvalidOperationException("0117 duplicate token: " + token);
                Assets.Add(token, obj);
                return obj;
            }

            // MemberwiseClone + an identity-keyed component deep copy. Native
            // blueprint references stay shared read-only; component objects,
            // actions and conditions become private to this package.
            internal T Clone<T>(T original, string token, string name, string description) where T : BlueprintScriptableObject
            {
                var clone = (T)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(original, null);
                clone.ComponentsArray = (original.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                    .Select(c => (BlueprintComponent)PsychicAssets.CopyData(c)).ToArray();
                return Add(clone, token, name, description);
            }

            internal BlueprintFeature Feature(string token, string name, string description, params BlueprintComponent[] components)
                => Add(new BlueprintFeature { Ranks = 1, ComponentsArray = components }, token, name, description);

            internal BlueprintBuff Buff(string token, string name, string description, params BlueprintComponent[] components)
                => Add(new BlueprintBuff { Stacking = StackingType.Replace, ComponentsArray = components }, token, name, description);

            internal BlueprintAbility Ability(string token, string name, string description, AbilityType type,
                UnitCommand.CommandType action, AbilityRange range, params BlueprintComponent[] components)
                => Add(new BlueprintAbility {
                    Type = type, ActionType = action, Range = range,
                    CanTargetSelf = range == AbilityRange.Personal,
                    CanTargetFriends = range != AbilityRange.Personal,
                    CanTargetEnemies = range != AbilityRange.Personal,
                    EffectOnEnemy = AbilityEffectOnUnit.Harmful, EffectOnAlly = AbilityEffectOnUnit.Helpful,
                    ComponentsArray = components }, token, name, description);

            internal BlueprintProgression Progression(string token, string name, string description, LevelEntry[] entries, UIGroup[] groups)
                => Add(new BlueprintProgression { LevelEntries = entries, UIGroups = groups, Ranks = 1 }, token, name, description);

            internal void Enable() { foreach (var bp in Assets.Values) bp.OnEnable(); }
        }

        internal static LevelEntry Entry(int level, params BlueprintFeatureBase[] facts)
        {
            var e = new LevelEntry { Level = level };
            Set(e, "m_Features", facts.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            return e;
        }

        internal static UIGroup Group(params BlueprintFeatureBase[] facts)
        {
            var g = new UIGroup();
            Set(g, "m_Features", facts.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            return g;
        }

        // R1 = 3 + Cha, R2 = 1 + Cha, R3 = fixed 3, R4 = fixed 1,
        // R5 = one point per Wizard level. m_Min = 0 forbids negative pools.
        internal static BlueprintAbilityResource Resource(Factory f, string token, int baseValue, StatType? bonusStat, bool perWizardLevel)
        {
            var r = f.Add(new BlueprintAbilityResource(), token, "", "");
            var amount = Get(r, "m_MaxAmount");
            Set(amount, "BaseValue", baseValue);
            Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            if (bonusStat.HasValue)
            {
                Set(amount, "IncreasedByStat", true);
                Set(amount, "ResourceBonusStat", bonusStat.Value);
            }
            if (perWizardLevel)
            {
                Set(amount, "IncreasedByLevel", true);
                Set(amount, "LevelIncrease", 1);
            }
            Set(r, "m_MaxAmount", amount);
            if (Has(r, "m_Min")) Set(r, "m_Min", 0);
            return r;
        }

        internal static AddAbilityResources Grant(BlueprintAbilityResource resource)
        {
            var c = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true };
            Set(c, "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
            return c;
        }

        internal static AbilityResourceLogic Cost(BlueprintAbilityResource resource, int amount = 1)
        {
            var c = new AbilityResourceLogic { Amount = amount };
            Set(c, "m_IsSpendResource", true);
            Set(c, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
            return c;
        }

        // Rebind a private clone from Shaman/Wisdom donors onto Wizard,
        // Intelligence (primary) and Charisma (secondary, only on the named
        // branches). Returns a human readable audit list; unknown shapes are
        // rejected by the caller instead of being silently left native.
        internal static List<string> Rebind(object root, BlueprintCharacterClass wizard, StatType dcStat,
            Func<BlueprintReferenceBase, BlueprintAbilityResource> mapResource)
        {
            var audit = new List<string>();
            var seen = new HashSet<object>(PsychicAssets.IdentityComparer);
            // Start at the component array: a blueprint itself is a read-only
            // SimpleBlueprint and must never be walked into.
            var start = root is BlueprintScriptableObject bp
                ? (object)bp.ComponentsArray : root;
            Walk(start, seen, o =>
            {
                var type = o.GetType();
                var name = type.Name;
                if (name == "ContextRankConfig" && Has(o, "m_Class"))
                {
                    Set(o, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(wizard) });
                    audit.Add("rank->Wizard");
                }
                if (name == "ContextCalculateAbilityParamsBasedOnClass")
                {
                    if (Has(o, "m_CharacterClass")) Set(o, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(wizard));
                    if (Has(o, "StatType") && Equals(Get(o, "StatType"), StatType.Wisdom))
                        Set(o, "StatType", dcStat);
                    audit.Add("params->Wizard/Wisdom-to-" + dcStat);
                }
                // The class-less shape appears on hex leaves: it only names the
                // stat, so Shaman's Wisdom becomes Intelligence and nothing else
                // moves (Charisma stays Charisma).
                else if (name == "ContextCalculateAbilityParams" && Has(o, "StatType") &&
                         Equals(Get(o, "StatType"), StatType.Wisdom))
                {
                    Set(o, "StatType", dcStat);
                    audit.Add("params-stat " + dcStat);
                }
                // Rank configs that read a stat bonus: Wisdom is the shaman
                // casting stat, Charisma is a real secondary value and is kept.
                if (name == "ContextRankConfig" && Has(o, "m_Stat") &&
                    Equals(Get(o, "m_Stat"), StatType.Wisdom))
                {
                    Set(o, "m_Stat", dcStat);
                    audit.Add("rank-stat " + dcStat);
                }
                // Stat bonuses keyed on a bare stat name. Only non-racial ones
                // follow the caster: racial entries belong to the summoned pet
                // templates, whose own Wisdom is unrelated to the shaman stat.
                if (name == "AddStatBonus" && Has(o, "Stat") &&
                    Equals(Get(o, "Stat"), StatType.Wisdom) &&
                    !Equals(Get(o, "Descriptor"), ModifierDescriptor.Racial))
                {
                    Set(o, "Stat", dcStat);
                    audit.Add("statbonus-stat " + dcStat);
                }
                if (name == "AddFeatureOnClassLevel" && Has(o, "m_Class"))
                {
                    var field = AccessTools.Field(type, "m_Class");
                    object value = field.FieldType.IsArray
                        ? (object)new[] { Ref<BlueprintCharacterClassReference>(wizard) }
                        : Ref<BlueprintCharacterClassReference>(wizard);
                    field.SetValue(o, value);
                    audit.Add("classLevel->Wizard");
                }
                if ((name.Contains("ResourceLogic") && Has(o, "m_RequiredResource")) ||
                    (name == "AddAbilityResources" && Has(o, "m_Resource")))
                {
                    string field = name == "AddAbilityResources" ? "m_Resource" : "m_RequiredResource";
                    var donor = Get(o, field) as BlueprintReferenceBase;
                    if (donor != null)
                    {
                        var replacement = mapResource(donor);
                        if (replacement != null)
                        {
                            Set(o, field, Ref<BlueprintAbilityResourceReference>(replacement));
                            audit.Add("resource->" + replacement.name);
                        }
                    }
                }
            });
            return audit;
        }

        internal static void Walk(object value, HashSet<object> seen, Action<object> visit)
        {
            if (value == null || value is string || value is Type || value is Delegate ||
                value.GetType().IsValueType || value is SimpleBlueprint ||
                value is BlueprintReferenceBase || value is UnityEngine.Object) return;
            if (!seen.Add(value)) return;
            visit(value);
            if (value is System.Collections.IEnumerable items)
            {
                foreach (var item in items) Walk(item, seen, visit);
                return;
            }
            for (var t = value.GetType(); t != null && t != typeof(object) &&
                 t != typeof(BlueprintComponent) && t != typeof(Element); t = t.BaseType)
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!f.IsStatic) Walk(f.GetValue(value), seen, visit);
        }

        // Spirit magic lists must not leak into a Wizard spellbook. Only the
        // special-list component is removed; read-only native effects stay.
        internal static int StripSpellLists(BlueprintScriptableObject bp)
        {
            var kept = (bp.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Where(c => !(c != null && c.GetType().Name.Contains("AddSpecialSpellList"))).ToArray();
            int removed = (bp.ComponentsArray?.Length ?? 0) - kept.Length;
            bp.ComponentsArray = kept;
            return removed;
        }

        internal static void Icon(BlueprintUnitFact target, BlueprintUnitFact source)
        {
            if (source == null) return;
            try { SpellBatch01447.Icon(target, source); }
            catch { if (Has(target, "m_Icon") && Has(source, "m_Icon")) Set(target, "m_Icon", Get(source, "m_Icon")); }
        }

        // Reuse the donor's authored name, description and icon verbatim instead
        // of writing a fresh one. The localized strings are shared read-only, so
        // the clone reads exactly what the game (or the mod that added the
        // spirit) already ships, in the player's own locale.
        internal static void Present(BlueprintUnitFact target, BlueprintUnitFact source)
        {
            if (target == null || source == null) return;
            foreach (string field in new[] { "m_DisplayName", "m_Description", "m_DescriptionShort" })
            {
                // Read through the same accessor Set uses, so a property-backed
                // field is not silently skipped.
                object value = null;
                try { value = Get(source, field); } catch (MissingFieldException) { continue; }
                if (value == null) continue;
                try { Set(target, field, value); } catch (MissingFieldException) { }
            }
            Icon(target, source);
        }

        // Rebuild the private element cache inherited from a shallow clone, and
        // reset native ability caches so restrictions resolve our components.
        internal static void Prepare(BlueprintScriptableObject bp)
        {
            var elements = SpellBatch01447.Elements(bp.ComponentsArray, new HashSet<object>(PsychicAssets.IdentityComparer)).ToList();
            if (Has(bp, "m_AllElements")) Set(bp, "m_AllElements", elements);
            if (bp is Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility ability)
                SpellBatch01447.ResetAbilityCaches(ability);
            PsychicRepairComponents.UniqueNames(bp);
        }
    }
}
