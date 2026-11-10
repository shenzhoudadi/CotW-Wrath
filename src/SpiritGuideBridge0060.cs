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
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // COTW-0060 / S-0060-OWNERSHIP. The single isolation bridge for the Spirit
    // Guide Oracle archetype. It owns the private 0060 identity namespace and
    // the Oracle/Charisma(primary)/Wisdom(secondary) rebind rules. It reuses the
    // public PsychicAssets copy/reflection primitives and the native temporary
    // knowledge ledger; it is not a copy of the 0117 Wizard/Wisdom bridge and
    // introduces no second SpiritsEngine and no global Shaman/Oracle edit.
    internal static class SpiritGuideBridge0060
    {
        internal const string Salt = "CotW.Wrath.SpiritGuide0060.v1/";
        internal const string AssetPrefix = "CotWWrathSpiritGuide0060";
        internal const string RootToken = "SpiritGuideArchetype0060";

        // Frozen host identities (C-frozen-contract section 2).
        internal const string OracleClassId = "20ce9bf8af32bee4c8557a045ab499b1";
        internal const string OracleProgressionId = "5dd9b4982c9ed0b4090d5a35c459b729";
        internal const string OracleSpellbookId = "6c03364712b415941a98f74522a81273";
        internal const string OracleSpellListId = "8443ce803d2d31347897a3d85cc32f53";
        internal const string WizardSpellListId = "ba0401fdeb4062f40a7aa95b6f07fe89";
        internal const string MysterySelectionId = "5531b975dcdf0e24c98f1ff7e017e741";
        internal const string RevelationSelectionId = "60008a10ad7ad6543b1f63016741a5d2";

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
        internal static void Set(object obj, string field, object value) => PsychicAssets.Set(obj, field, value);
        internal static T Ref<T>(SimpleBlueprint obj) where T : BlueprintReferenceBase, new() => PsychicAssets.Ref<T>(obj);
        internal static LocalizedString Text(string token, string suffix, string value) => PsychicAssets.Text(token, suffix, value);

        internal static void Log(string line)
        {
            try { ModEntry.LogSpellDiagnostic(line); } catch { /* isolated host has no UMM logger */ }
            Console.WriteLine(line);
        }

        // A private factory with the frozen 0060 salt and asset name prefix.
        // Native blueprint references stay shared read-only; component objects,
        // actions and conditions become private to this package via CopyData.
        internal sealed class Factory
        {
            internal readonly BlueprintsCache Cache;
            internal readonly Dictionary<string, BlueprintScriptableObject> Assets = new Dictionary<string, BlueprintScriptableObject>();
            internal Factory(BlueprintsCache cache) { Cache = cache; }

            internal T Native<T>(string guid) where T : SimpleBlueprint => Cache.Load(BlueprintGuid.Parse(guid)) as T;
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
                if (Assets.ContainsKey(token)) throw new InvalidOperationException("0060 duplicate token: " + token);
                Assets.Add(token, obj);
                return obj;
            }

            internal T Clone<T>(T original, string token, string name, string description) where T : BlueprintScriptableObject
            {
                var clone = (T)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(original, null);
                clone.ComponentsArray = (original.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                    .Select(c => (BlueprintComponent)PsychicAssets.CopyData(c)).ToArray();
                return Add(clone, token, name, description);
            }

            internal BlueprintFeature Feature(string token, string name, string description, params BlueprintComponent[] components)
                => Add(new BlueprintFeature { Ranks = 1, ComponentsArray = components }, token, name, description);
            internal BlueprintFeatureSelection Selection(string token, string name, string description, params BlueprintComponent[] components)
                => Add(new BlueprintFeatureSelection { Ranks = 1, ComponentsArray = components }, token, name, description);
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

        internal static LevelEntry Entry(int level, params BlueprintFeatureBase[] facts) => ArchetypeBatch01445.Entry(level, facts);
        internal static UIGroup Group(params BlueprintFeatureBase[] facts)
        {
            var g = new UIGroup();
            Set(g, "m_Features", facts.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            return g;
        }

        // Q03 dual-channel: spirit base/greater pools use Charisma as primary.
        // baseValue is the fixed floor (e.g. 3 for most spirits, 1 for Life
        // channel); bonusStat is Charisma for primary pools. m_Min = 0 forbids
        // negative pools. The effective level L is the real Oracle level, bound
        // by the caller through Rank/ContextRankConfig, never total character level.
        internal static BlueprintAbilityResource Resource(Factory f, string token, int baseValue, StatType? bonusStat)
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
            Set(r, "m_MaxAmount", amount);
            if (Has(r, "m_Min")) Set(r, "m_Min", 0);
            return r;
        }

        internal static Kingmaker.Designers.Mechanics.Facts.AddAbilityResources Grant(BlueprintAbilityResource resource)
            => ArchetypeBatch01445.Grant(resource);
        internal static AbilityResourceLogic Cost(BlueprintAbilityResource resource, int amount = 1)
            => ArchetypeBatch01445.Cost(resource, amount);

        // A rank scaled by the real Oracle class level (progression "ClassLevel"),
        // optionally clamped to a minimum of 1. Used for level-type durations and
        // the secondary Wisdom branches keep their own stat-read components.
        internal static ContextRankConfig OracleRank(BlueprintCharacterClass oracle, int min = 0)
        {
            var r = new ContextRankConfig();
            SetEnum(r, "m_BaseValueType", "ClassLevel");
            SetEnum(r, "m_Progression", "AsIs");
            Set(r, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(oracle) });
            if (min > 0 && Has(r, "m_Min")) Set(r, "m_Min", min);
            return r;
        }

        internal static void Icon(BlueprintUnitFact target, BlueprintUnitFact source)
        {
            if (source == null) return;
            try { SpellBatch01447.Icon(target, source); }
            catch { if (Has(target, "m_Icon") && Has(source, "m_Icon")) Set(target, "m_Icon", Get(source, "m_Icon")); }
        }

        // Rebuild the private element cache inherited from a shallow clone and
        // reset native ability caches so restrictions resolve our components.
        internal static void Prepare(BlueprintScriptableObject bp)
        {
            var elements = SpellBatch01447.Elements(bp.ComponentsArray, new HashSet<object>(PsychicAssets.IdentityComparer)).ToList();
            if (Has(bp, "m_AllElements")) Set(bp, "m_AllElements", elements);
            if (bp is BlueprintAbility ability) SpellBatch01447.ResetAbilityCaches(ability);
            PsychicRepairComponents.UniqueNames(bp);
        }
    }
}