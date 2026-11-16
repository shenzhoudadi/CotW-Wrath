using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class DivineMetamagic
    {
        internal const string ModuleName = "Divine Metamagic";
        internal const string SelectionToken = "DivineMetamagicSelection";
        internal const string ParentToken = "DivineMetamagicParentAbility";
        private const string ClericChannelFeatureId = "a79013ff4bcd4864cb669622a29ddafb";

        private const string DescriptionText =
            "你可以在你的神术中灌注引导能量，以让其更为强大。\n" +
            "先决条件：引导能量类能力，释放神术的能力。\n" +
            "效果：选择一个你已拥有的超魔专长。此后你可以用一个迅捷动作从能力栏的「神圣超魔」菜单中选择该超魔，使你下一次施放的神术自动附加该超魔效果，且不改变法术环级或施法时间；施放该神术时消耗 1 + 该超魔专长正常增加环级数 的引导能量次数（0 环超魔最低消耗 1 次），之后效果结束，需要再次用迅捷动作准备。\n" +
            "特殊：此专长可多次选取，每次必须选择一个不同的超魔专长。";

        private const string ParentDescription =
            "以一个迅捷动作准备神圣超魔：从子选项中选择一个你已通过「神圣超魔」专长掌握的超魔，使你下一次施放的神术附加该超魔效果。每次施法后需要重新准备。";

        private static readonly string[] KnownChannelResourceIds =
        {
            "5e2bba3e07c37be42909a12945c27de7", // Cleric ChannelEnergyResource
            "0669e4beb1d0e694eb203d129dbde969", // OracleRevelationChannelResource
            "b0e0c7716ab27c64fb4b131c9845c596", // ChannelEnergyHospitalerResource
            "d4a66a2d91b7e5d469a73a4acef12de2", // SpiritWardenChannelResource
            "ac099428fc0d7934bbcff77291682202", // WitchDoctorChannelEnergyResource
            "027cb4deb3e668b46904dde64f5d06b1", // ShamanLifeSpiritChannelEnergyResource
            "f9af9354fb8a79649a6e512569387dc5", // ChannelEnergyEmpyrealResource
            "707f3cc2e103a8d40ab79ff49664b2bf", // HexChannelerChannelResource
        };

        private static readonly string[] KnownChannelFeatureIds =
        {
            "a79013ff4bcd4864cb669622a29ddafb", // ChannelEnergyFeature
            "3adb2c906e031ee41a01bfc1d5fb7eea", // ChannelNegativeFeature
            "4bf9a9afadca5304e89bf52f2ac2d236", // OracleRevelationChannelFeature
            "a9ab1bbc79ecb174d9a04699986ce8d5", // ChannelEnergyHospitalerFeature
            "cb6d55dda5ab906459d18a435994a760", // ChannelEnergyPaladinFeature
            "eb388d17f07e0b44d9f83ada0148cc69", // WitchDoctorChannelPositiveFeature
            "b8ec9dccc0e7ef74fb4072b0679c2aec", // ShamanLifeSpiritChannelEnergyFeature
            "7d49d7f590dc9a948b3bd1c8b7979854", // ChannelEnergyEmpyrealFeature
            "332c43d3f25fb9a429a42067142c41d5", // WarpriestShieldbearerChannelEnergyFeature
            "5aebdef7644a74e4d8c0d3cd8909de2b", // WarpriestShieldbearerChannelNegativeFeature
            "bd588bc544d2f8547a02bb82ad9f466a", // WarpriestChannelEnergyFeature
            "e02c8a7336a542f4baffa116b6506950", // WarpriestChannelNegativeFeature
            "b40316f05d4772e4894688e6743602bd", // HexChannelerChannelFeature
            "7c8d5e2ab326fdb4cabafc1c84a5c8e2", // HexChannelerChannelNegativeFeature
            "b423fbf947bc51344bac21752c47471c", // HexChannelerChannelPositiveFeature
        };

        private static readonly HashSet<BlueprintGuid> ChannelResourceGuids =
            new HashSet<BlueprintGuid>(KnownChannelResourceIds.Select(BlueprintGuid.Parse));
        private static readonly HashSet<BlueprintGuid> ChannelFeatureGuids =
            new HashSet<BlueprintGuid>(KnownChannelFeatureIds.Select(BlueprintGuid.Parse));

        private static readonly string[] NativeMetamagicIds =
        {
            "f180e72e4a9cbaa4da8be9bc958132ef", // Extend
            "46fad72f54a33dc4692d3b62eca7bb78", // Reach
            "85f3340093d144dd944fff9a9adfd2f2", // Selective
            "fbf5d9ce931f47f3a0c818b3f8ef8414", // Bolstered
            "c101ad6879a94204a780506f7a554865", // Piercing
            "8ad7fd39abea4722b39eb5a67d606a41", // Intensified
            "a1de1e4f92195b442adb946f0e2b9d4e", // Empower
            "cd26b9fa3f734461a0fcedc81cafaaac", // Persistent
            "7f2b282626862e345935bbea5e66424b", // Maximize
            "ef7ece7bb5bb66a41b256976b27f424e", // Quicken
        };

        private static BlueprintFeatureSelection Selection;
        private static BlueprintAbility ParentAbility;
        private static AbilityVariants ParentVariants;
        private static BlueprintsCache CachedCache;
        private static readonly Dictionary<Metamagic, BlueprintFeature> RegisteredOptions =
            new Dictionary<Metamagic, BlueprintFeature>();
        private static readonly HashSet<BlueprintGuid> PublishedVariants = new HashSet<BlueprintGuid>();
        private static int LastSnapshotCount = -1;
        private static volatile bool OptionsDirty;
        private static int RefreshBudget = 8;
        private static readonly object Gate = new object();

        internal static void Register(BlueprintsCache cache)
        {
            CachedCache = cache;
            var basicPool = cache.Load(BlueprintGuid.Parse(BlueprintRegistration.BasicFeatsId)) as BlueprintFeatureSelection
                ?? throw new InvalidOperationException("Divine Metamagic basic feat pool missing.");

            var f = new PsychicAssets(cache);
            var iconSource = cache.Load(BlueprintGuid.Parse(ClericChannelFeatureId)) as BlueprintUnitFact;

            // One ability-bar entry. Its conversion menu is filled with one
            // one-shot swift ability per metamagic the player has selected.
            var variants = new AbilityVariants { name = "DivineMetamagicVariants" };
            Set(variants, "m_Variants", Array.Empty<BlueprintAbilityReference>());
            var parent = f.Ability(ParentToken, "神圣超魔", ParentDescription,
                AbilityType.Supernatural, UnitCommand.CommandType.Free, AbilityRange.Personal, variants);
            parent.NotOffensive = true;
            if (iconSource?.Icon != null) Set(parent, "m_Icon", iconSource.Icon);
            ParentAbility = parent;
            ParentVariants = variants;

            var sel = f.Add(new BlueprintFeatureSelection
            {
                Ranks = 20,
                Groups = new[] { FeatureGroup.Feat },
                IsClassFeature = false,
                IgnorePrerequisites = false,
                ComponentsArray = new BlueprintComponent[]
                {
                    new PrerequisiteChannelEnergy { name = "PrerequisiteChannelEnergy" },
                    new PrerequisiteDivineSpellcaster { name = "PrerequisiteDivineSpellcaster" },
                    Facts(parent)
                }
            }, SelectionToken, "神圣超魔", DescriptionText);
            if (iconSource?.Icon != null) Set(sel, "m_Icon", iconSource.Icon);

            PsychicRepairComponents.UniqueNames(parent);
            PsychicRepairComponents.UniqueNames(sel);
            f.Enable();
            foreach (var bp in f.Assets.Values)
                if (cache.Load(bp.AssetGuid) == null) cache.AddCachedBlueprint(bp.AssetGuid, bp);

            Selection = cache.Load(sel.AssetGuid) as BlueprintFeatureSelection ?? sel;
            ParentAbility = cache.Load(parent.AssetGuid) as BlueprintAbility ?? parent;
            ParentVariants = ParentAbility.GetComponent<AbilityVariants>() ?? variants;

            T09CombatFeats.Append(new[] { basicPool }, new[] { Selection });
            RefreshOptions(cache);
        }

        // Cheap gate on the feat-selection read path. The old implementation
        // called SublimeChord.Snapshot() (a full copy + reflection walk of all
        // ~250k cache entries) on EVERY get_AllFeatures/get_Features, which is
        // the "选择专长时短暂无响应" hitch. Now a heavy refresh happens only
        // when a metamagic feat blueprint was actually registered, and at most
        // 8 times per session. OptionsDirty is cleared BEFORE the refresh so
        // a metamagic added during it simply schedules one more bounded pass.
        private static void MaybeRefreshOptions(BlueprintsCache cache)
        {
            if (cache == null || Selection == null || RefreshBudget <= 0 || !OptionsDirty) return;
            RefreshBudget--;
            OptionsDirty = false;
            // A provider may replace an existing blueprint (same GUID, so the
            // snapshot length is unchanged). Force a real scan on a dirty pass.
            LastSnapshotCount = -1;
            RefreshOptions(cache);
        }

        internal static void RefreshOptions(BlueprintsCache cache)
        {
            if (cache == null || Selection == null) return;
            var snapshot = SublimeChord.Snapshot(cache);
            if (snapshot.Length == LastSnapshotCount) return;

            lock (Gate)
            {
                var stock = NativeMetamagicIds
                    .Select(id => cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeature)
                    .Where(x => x != null);
                var allFeatures = stock.Concat(snapshot.OfType<BlueprintFeature>()).Distinct().ToArray();

                var discovered = new Dictionary<Metamagic, BlueprintFeature>();
                foreach (var feat in allFeatures.OrderBy(v => v.AssetGuid.ToString(), StringComparer.Ordinal))
                {
                    var comps = feat.GetComponents<AddMetamagicFeat>().ToArray();
                    if (comps.Length != 1) continue;
                    var flag = comps[0].Metamagic;
                    uint bit = unchecked((uint)(int)flag);
                    if (bit == 0 || (bit & (bit - 1)) != 0) continue;
                    if (flag == Metamagic.Heighten || flag == Metamagic.CompletelyNormal) continue;
                    int increase;
                    try { increase = PsychicCompletionAmplifications01458.MetaCost(flag); }
                    catch { continue; }
                    if (increase < 0 || increase > 9) continue;

                    if (!discovered.TryGetValue(flag, out var prev) || ScorePresentation(feat) > ScorePresentation(prev))
                        discovered[flag] = feat;
                }

                var f = new PsychicAssets(cache);
                var added = new List<BlueprintFeature>();
                foreach (var pair in discovered.OrderBy(p => unchecked((uint)(int)p.Key)))
                {
                    if (RegisteredOptions.ContainsKey(pair.Key)) continue;
                    var option = BuildOption(cache, f, pair.Key, pair.Value);
                    if (option != null)
                    {
                        RegisteredOptions[pair.Key] = option;
                        added.Add(option);
                    }
                }

                if (added.Count > 0)
                    T09CombatFeats.Append(new[] { Selection }, added.ToArray());
                LastSnapshotCount = snapshot.Length;
            }
        }

        private static int ScorePresentation(BlueprintFeature feat)
        {
            int score = 0;
            if (!string.IsNullOrWhiteSpace(feat.Name)) score += 4;
            if (!string.IsNullOrWhiteSpace(feat.Description)) score += 2;
            if (feat.Icon != null) score += 1;
            return score;
        }

        private static BlueprintFeature BuildOption(BlueprintsCache cache, PsychicAssets f,
            Metamagic magic, BlueprintFeature sourceFeat)
        {
            string key = "DivineMetamagicOption" + unchecked((uint)(int)magic);
            int increase = Math.Max(0, PsychicCompletionAmplifications01458.MetaCost(magic));
            int cost = Math.Max(1, 1 + increase);
            string featName = string.IsNullOrWhiteSpace(sourceFeat.Name) ? magic.ToString() : sourceFeat.Name;
            string optionTitle = "神圣超魔：" + featName;
            // Sub-options reuse the original metamagic feat's own description.
            string sourceDesc = string.IsNullOrWhiteSpace(sourceFeat.Description) ? featName : sourceFeat.Description;

            T Cached<T>(T created) where T : BlueprintScriptableObject
                => cache.Load(created.AssetGuid) as T ?? created;

            // --- pending buff: applied to the caster by the one-shot ability,
            //     consumed (and removed) by the next divine spell ---
            var buffComps = new List<BlueprintComponent>
            {
                new DivineMetamagicBuffComponent
                {
                    name = "DivineMetamagicBuffComponent",
                    Metamagic = magic,
                    Cost = cost
                }
            };
            if (magic == Metamagic.Quicken)
                buffComps.Add(new DivineMetamagicQuickenActionType { name = "DivineMetamagicQuicken", Metamagic = Metamagic.Quicken });

            var pendingBuff = Cached(f.Buff(key + "Buff", optionTitle, sourceDesc, buffComps.ToArray()));
            if (sourceFeat.Icon != null) Set(pendingBuff, "m_Icon", sourceFeat.Icon);
            SetEnum(pendingBuff, "m_Flags", "RemoveOnRest");

            // --- one-shot swift child ability (a conversion of the parent) ---
            var gate = new DivineMetamagicOptionGate
            {
                name = "DivineMetamagicOptionGate",
                Metamagic = magic,
                Cost = cost
            };
            var apply = Apply(pendingBuff, 0, toCaster: true, fromSpell: false);
            apply.Permanent = true;
            apply.IsNotDispelable = true;
            apply.AsChild = false;
            var child = Cached(f.Ability(key + "Ability", featName, sourceDesc,
                AbilityType.Supernatural, UnitCommand.CommandType.Swift, AbilityRange.Personal,
                gate, Run(apply)));
            child.NotOffensive = true;
            child.ActionBarAutoFillIgnored = true;
            if (sourceFeat.Icon != null) Set(child, "m_Icon", sourceFeat.Icon);
            Set(child, "m_Parent", Ref<BlueprintAbilityReference>(ParentAbility));

            // --- legacy persistent toggle: still published so saves made with
            //     the old version keep resolving their facts, but hidden and no
            //     longer the way the feature is used ---
            var toggle = Cached(f.Add(new BlueprintActivatableAbility
            {
                ActivationType = AbilityActivationType.WithUnitCommand,
                DeactivateImmediately = true,
                DoNotTurnOffOnRest = true,
                DeactivateIfCombatEnded = false,
                Group = ActivatableAbilityGroup.None,
                ComponentsArray = new BlueprintComponent[]
                {
                    new RestrictionEnoughChannelEnergy { name = "RestrictionEnoughChannelEnergy", Cost = cost }
                }
            }, key + "Toggle", optionTitle, sourceDesc));
            toggle.HiddenInUI = true;
            toggle.ActionBarAutoFillIgnored = true;
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(pendingBuff));
            Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Swift);
            if (sourceFeat.Icon != null) Set(toggle, "m_Icon", sourceFeat.Icon);

            // --- the selection option (tracks which metamagics were chosen) ---
            var noRepeat = new PrerequisiteNoFeature { Group = Prerequisite.GroupType.All };
            Set(noRepeat, "m_Feature", Ref<BlueprintFeatureReference>(null));
            var option = Cached(f.Feature(key, optionTitle, sourceDesc,
                new PrerequisiteKnowsMetamagic { name = "PrerequisiteKnowsMetamagic", Metamagic = magic },
                noRepeat,
                Facts(toggle)));
            option.Ranks = 1;
            option.Groups = Array.Empty<FeatureGroup>();
            if (sourceFeat.Icon != null) Set(option, "m_Icon", sourceFeat.Icon);
            Set(noRepeat, "m_Feature", Ref<BlueprintFeatureReference>(option));

            foreach (var bp in new BlueprintScriptableObject[] { pendingBuff, child, toggle, option })
                PsychicRepairComponents.UniqueNames(bp);

            f.Enable();
            foreach (var bp in new BlueprintScriptableObject[] { pendingBuff, child, toggle, option })
                if (cache.Load(bp.AssetGuid) == null) cache.AddCachedBlueprint(bp.AssetGuid, bp);

            PublishVariant(child);
            return option;
        }

        private static void PublishVariant(BlueprintAbility child)
        {
            if (child == null || ParentVariants == null || ParentAbility == null) return;
            lock (Gate)
            {
                if (!PublishedVariants.Add(child.AssetGuid)) return;
                var current = (BlueprintAbilityReference[])Get(ParentVariants, "m_Variants")
                    ?? Array.Empty<BlueprintAbilityReference>();
                Set(ParentVariants, "m_Variants",
                    current.Concat(new[] { Ref<BlueprintAbilityReference>(child) }).ToArray());
                Set(child, "m_Parent", Ref<BlueprintAbilityReference>(ParentAbility));
            }
        }

        // A child is visible / usable only for a metamagic the character has
        // actually chosen through the Divine Metamagic feat.
        internal static bool HasOption(UnitEntityData unit, Metamagic magic)
        {
            if (unit == null) return false;
            var guid = Id("DivineMetamagicOption" + unchecked((uint)(int)magic));
            foreach (var f in unit.Facts.List)
                if (f?.Blueprint?.AssetGuid == guid) return true;
            // Mirror the native AbilityShowIfCasterHasFact path: selection
            // entries live in Progression.Features too.
            var features = unit.Descriptor?.Progression?.Features;
            if (features != null)
                foreach (var f in features.Enumerable)
                    if (f?.Blueprint?.AssetGuid == guid) return true;
            return false;
        }

        internal static bool IsChannelAbility(BlueprintAbility ability)
        {
            if (ability == null) return false;
            string n = ability.name ?? "";
            if (n.IndexOf("Channel", StringComparison.OrdinalIgnoreCase) >= 0 &&
                (n.IndexOf("Energy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 n.IndexOf("Positive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 n.IndexOf("Negative", StringComparison.OrdinalIgnoreCase) >= 0))
                return true;
            return false;
        }

        internal static List<BlueprintAbilityResource> GetChannelResources(UnitDescriptor unit)
        {
            var result = new List<BlueprintAbilityResource>();
            if (unit?.Resources == null) return result;
            var seen = new HashSet<BlueprintGuid>();

            void AddRes(BlueprintAbilityResource res)
            {
                if (res != null && seen.Add(res.AssetGuid) && unit.Resources.ContainsResource(res))
                    result.Add(res);
            }

            foreach (var guid in ChannelResourceGuids)
            {
                var res = ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(guid);
                if (res != null) AddRes(res);
            }

            if (unit.Abilities != null)
            {
                foreach (var ab in unit.Abilities.Enumerable)
                {
                    var bp = ab?.Blueprint;
                    if (bp == null) continue;
                    var rl = bp.GetComponent<AbilityResourceLogic>();
                    var res = rl != null ? (Get(rl, "m_RequiredResource") as BlueprintAbilityResourceReference)?.Get() : null;
                    if (res == null) continue;
                    if (ChannelResourceGuids.Contains(res.AssetGuid) || IsChannelAbility(bp))
                        AddRes(res);
                }
            }
            return result;
        }

        internal static BlueprintAbilityResource FindSpendableChannelResource(UnitDescriptor unit, int amount)
        {
            foreach (var res in GetChannelResources(unit))
            {
                if (unit.Resources.GetResourceAmount(res) >= amount)
                    return res;
            }
            return null;
        }

        internal static bool HasChannelEnergyFeature(UnitDescriptor unit)
        {
            if (unit == null) return false;
            if (GetChannelResources(unit).Count > 0) return true;
            if (unit.Progression?.Features != null)
            {
                foreach (var f in unit.Progression.Features.Enumerable)
                {
                    var bp = f?.Blueprint;
                    if (bp == null) continue;
                    if (ChannelFeatureGuids.Contains(bp.AssetGuid)) return true;
                    string n = bp.name ?? "";
                    if (n.IndexOf("Channel", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        n.IndexOf("Feature", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        (n.IndexOf("Energy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("Positive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("Negative", StringComparison.OrdinalIgnoreCase) >= 0))
                        return true;
                }
            }
            return false;
        }

        internal static bool IsDivineSpell(AbilityData data)
        {
            var book = data?.Spellbook?.Blueprint;
            if (book == null || data.SourceItem != null) return false;
            if (book.IsArcane || book.IsMythic || book.IsAlchemist) return false;
            if (PsychicCasting.IsPsychic(book)) return false;
            if (book.AssetGuid == BlueprintGuid.Parse(MediumIntegration.BookId)) return false;
            return true;
        }

        // Precise "late provider" signal: another mod published a metamagic feat.
        // Only a blueprint that actually carries AddMetamagicFeat can change the
        // option set, so this is a single cheap component check per published
        // blueprint instead of a full-cache snapshot on every UI read.
        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.AddCachedBlueprint))]
        private static class MetamagicAddedPatch
        {
            private static void Postfix(SimpleBlueprint __1)
            {
                var feat = __1 as BlueprintFeature;
                var comps = feat?.ComponentsArray;
                if (comps == null) return;
                foreach (var c in comps)
                    if (c is AddMetamagicFeat) { OptionsDirty = true; return; }
            }
        }

        [HarmonyPatch(typeof(BlueprintFeatureSelection), "get_AllFeatures")]
        private static class SelectionAllRefreshPatch
        {
            private static void Prefix(BlueprintFeatureSelection __instance)
            {
                if (Selection != null && __instance == Selection && CachedCache != null)
                    MaybeRefreshOptions(CachedCache);
            }
        }

        [HarmonyPatch(typeof(BlueprintFeatureSelection), "get_Features")]
        private static class SelectionFeaturesRefreshPatch
        {
            private static void Prefix(BlueprintFeatureSelection __instance)
            {
                if (Selection != null && __instance == Selection && CachedCache != null)
                    MaybeRefreshOptions(CachedCache);
            }
        }
    }

    [TypeId("d1c00000000000000000000000000014")]
    public sealed class PrerequisiteChannelEnergy : Prerequisite
    {
        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
            => DivineMetamagic.HasChannelEnergyFeature(unit);

        protected override string GetUITextInternal(UnitDescriptor unit) => "引导能量类能力";
    }

    [TypeId("d1c00000000000000000000000000015")]
    public sealed class PrerequisiteDivineSpellcaster : Prerequisite
    {
        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
        {
            if (unit == null) return false;
            foreach (var cls in unit.Progression.Classes)
            {
                var book = cls.Spellbook;
                if (book != null && !book.IsArcane && !book.IsMythic && !book.IsAlchemist &&
                    !PsychicCasting.IsPsychic(book) && book.AssetGuid != BlueprintGuid.Parse(MediumIntegration.BookId))
                {
                    if (unit.DemandSpellbook(cls.CharacterClass).MaxSpellLevel >= 1)
                        return true;
                }
            }
            return false;
        }

        protected override string GetUITextInternal(UnitDescriptor unit) => "能够施放 1 环或更高环位神术";
    }

    [TypeId("d1c00000000000000000000000000019")]
    public sealed class PrerequisiteKnowsMetamagic : Prerequisite
    {
        public Metamagic Metamagic;

        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
            => RestrictionKnowsMetamagic0609.Knows(unit, Metamagic);

        protected override string GetUITextInternal(UnitDescriptor unit) => $"已拥有超魔专长：{Metamagic}";
    }

    [AllowedOn(typeof(BlueprintActivatableAbility), false)]
    [TypeId("d1c00000000000000000000000000018")]
    public sealed class RestrictionEnoughChannelEnergy : ActivatableAbilityRestriction
    {
        public int Cost = 1;

        protected override bool IsAvailable()
            => DivineMetamagic.FindSpendableChannelResource(Owner, Cost) != null;
    }

    // Visibility + availability gate for a one-shot child ability. The parent's
    // conversion menu filters children through AbilityData.IsVisible(), and the
    // child's own restrictions are checked when it is selected.
    [TypeId("d1c00000000000000000000000000004")]
    public sealed class DivineMetamagicOptionGate : BlueprintComponent,
        IAbilityRestriction, IAbilityVisibilityProvider
    {
        public Metamagic Metamagic;
        public int Cost = 1;

        public bool IsAbilityVisible(AbilityData ability)
            => DivineMetamagic.HasOption(ability?.Caster?.Unit, Metamagic);

        public bool IsAbilityRestrictionPassed(AbilityData ability)
        {
            var unit = ability?.Caster?.Unit;
            if (unit == null || !DivineMetamagic.HasOption(unit, Metamagic)) return false;
            return DivineMetamagic.FindSpendableChannelResource(ability.Caster, Cost) != null;
        }

        public string GetAbilityRestrictionUIText()
            => $"需要已选择该神圣超魔，并有至少 {Cost} 次引导能量。";
    }

    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("d1c00000000000000000000000000017")]
    public sealed class DivineMetamagicQuickenActionType : AutoMetamagic { }

    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("d1c00000000000000000000000000016")]
    public sealed class DivineMetamagicBuffComponent : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>,
        IInitiatorRulebookHandler<RuleCastSpell>
    {
        public Metamagic Metamagic;
        public int Cost = 1;
        private int pendingCost;

        // Only one prepared metamagic at a time: selecting another one replaces
        // the previous pending buff instead of stacking several on the same spell.
        protected override void OnTurnOn()
        {
            var self = Fact as Buff;
            var buffs = Owner?.Buffs?.Enumerable;
            if (buffs == null) return;
            foreach (var other in buffs.ToArray())
            {
                if (other == null || ReferenceEquals(other, self)) continue;
                if (other.Blueprint?.GetComponent<DivineMetamagicBuffComponent>() != null)
                    other.Remove();
            }
        }

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            pendingCost = 0;
            var data = evt.AbilityData;
            if (evt.Spell == null || data == null || !DivineMetamagic.IsDivineSpell(data)) return;
            if (evt.Spell.Type != AbilityType.Spell) return;
            if ((evt.Spell.AvailableMetamagic & Metamagic) == 0) return;
            if (data.MetamagicData?.Has(Metamagic) == true) return;
            if (DivineMetamagic.FindSpendableChannelResource(Owner, Cost) == null) return;

            pendingCost = Cost;
            evt.AddMetamagic(Metamagic);
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }

        public void OnEventAboutToTrigger(RuleCastSpell evt) { }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            if (pendingCost <= 0) return;
            int amount = pendingCost;
            pendingCost = 0;
            if (evt.Spell == null || !DivineMetamagic.IsDivineSpell(evt.Spell)) return;
            var res = DivineMetamagic.FindSpendableChannelResource(Owner, amount);
            if (res != null) Owner.Resources.Spend(res, amount);
            // One-shot: the prepared metamagic is consumed by this spell.
            var buff = Fact as Buff;
            if (buff != null && !buff.IsDisposed) buff.Remove();
        }
    }
}
