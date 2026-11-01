// Adapted from Holic75/KingmakerRebalance, MIT; see LICENSE-CotW.txt.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Reuse native Time Stop visuals and conditions in an isolated graph. Its
    // source-area buff component supplies reliable native exit/end cleanup.
    internal static class TimeStutter01437
    {
        internal const string ModuleName = "Arcane discovery: Time Stutter";
        internal const string RootToken = "TimeStutterDiscovery";
        internal const string AbilityToken = "TimeStutterAbility";
        internal const string ResourceToken = "TimeStutterResource";
        internal const string HostToken = "TimeStutterHost";
        internal const string AreaToken = "TimeStutterArea";
        internal const string EffectToken = "TimeStutterEffect";
        internal static readonly string[] OwnTokens = { RootToken, AbilityToken, ResourceToken, HostToken, AreaToken, EffectToken };
        internal const string NativeAbility = "70e55e5b0ac84af78c084eb6cf47ae06";
        internal const string NativeBuff = "f4d026d3bf5a4e8183ded452e8d87260";
        internal const string NativeArea = "22f6aa316f394e7c88814f98227ed421";
        internal const string NativeEffect = "8fff364a212d4b4894b5e222f3696cdd";
        const string Description = "You briefly step out of time, using the game's Time Stop effect for 1 round. " +
            "All other creatures in the area become unable to act, move or attack and cannot be targeted or damaged while the effect lasts. " +
            "This spell-like ability uses a standard action and can be used once per day, plus one additional time at wizard levels 15 and 20. " +
            "Requires wizard level 10 or arcanist level 20. Arcanist levels count at half their value; class levels are not added together. " +
            "Time Stutter can only be selected within Arcane Discovery, and does not grant the Time Stop spell.";
        sealed class Publication
        {
            internal BlueprintFeature Root;
            internal BlueprintScriptableObject[] Assets;
            internal string[] Definitions;
        }
        static readonly ConditionalWeakTable<BlueprintsCache, Publication> Published = new ConditionalWeakTable<BlueprintsCache, Publication>();
        static string Link(object value) => (value as Kingmaker.ResourceLinks.WeakResourceLink)?.AssetId;
        static string Definition(BlueprintScriptableObject b)
        {
            object shape;
            if (b is BlueprintFeature f) shape = new object[] { f.Groups, f.Ranks, f.IsClassFeature };
            else if (b is BlueprintAbility a) shape = new object[] { a.Type, a.Range, a.ActionType, a.CanTargetSelf, a.CanTargetEnemies, a.CanTargetFriends, a.CanTargetPoint, a.AvailableMetamagic };
            else if (b is BlueprintAbilityResource) shape = new object[] { Get(b, "m_MaxAmount"), Get(b, "m_Min") };
            else if (b is BlueprintBuff buff) shape = new object[] { buff.Stacking, buff.Ranks, buff.IsClassFeature, buff.TickEachSecond, Get(buff, "m_Flags"), Link(Get(buff, "FxOnStart")), Link(Get(buff, "FxOnRemove")) };
            else if (b is BlueprintAbilityAreaEffect area) shape = new object[] { area.Shape, area.Size, area.AffectEnemies, area.AffectDead, area.IgnoreSleepingUnits, Get(area, "m_TargetType"), Get(area, "m_Tags"), Link(area.Fx) };
            else throw new InvalidOperationException("Unexpected Time Stutter publication type: " + b.GetType().FullName);
            return Migration01426.Definition(new object[] { b.name, b.GetType().FullName, b.ComponentsArray, shape });
        }
        internal static bool IsPublishedRoot(BlueprintsCache cache, BlueprintFeature root) => Published.TryGetValue(cache, out var state) && ReferenceEquals(root, state.Root) && ReferenceEquals(cache.Load(root.AssetGuid), root);
        static ContextActionApplyBuff NativeApply(BlueprintAbility source)
        {
            var run = source.GetComponents<AbilityEffectRunAction>().ToArray();
            if (source.Type != AbilityType.Spell || source.Range != AbilityRange.Personal || !source.CanTargetSelf || source.CanTargetEnemies || source.CanTargetFriends ||
                run.Length != 1 || run[0].Actions?.Actions?.Length != 1 || !(run[0].Actions.Actions[0] is ContextActionApplyBuff apply) || apply.Buff?.AssetGuid != BlueprintGuid.Parse(NativeBuff))
                throw new InvalidOperationException("Native Time Stop casting graph changed; discovery registration deferred.");
            return apply;
        }
        internal static void Register(BlueprintsCache cache)
        {
            var state = Published.GetValue(cache, _ => new Publication());
            lock (state) {
                if (state.Root != null) {
                    for (int i = 0; i < state.Assets.Length; i++)
                        if (!ReferenceEquals(cache.Load(state.Assets[i].AssetGuid), state.Assets[i]) || Definition(state.Assets[i]) != state.Definitions[i])
                            throw new InvalidOperationException("Time Stutter published graph changed: " + state.Assets[i].name);
                    TimeStopEngine01438.RegisterGraph(cache, (BlueprintAbilityAreaEffect)cache.Load(Id(AreaToken)), (BlueprintBuff)cache.Load(Id(EffectToken)));
                    WizardDiscoveries01435.ReconcileTimeStutter(cache, state.Root); return;
                }
                foreach (string token in OwnTokens)
                    if (cache.Load(Id(token)) != null) throw new InvalidOperationException("Time Stutter private identity already exists without this publication: " + token);
                var f = new PsychicAssets(cache);
                var wizard = f.Native<BlueprintCharacterClass>(WizardDiscoveries01435.WizardId);
                var arcanist = f.Native<BlueprintCharacterClass>(WizardDiscoveries01435.ArcanistId);
                var source = f.Native<BlueprintAbility>(NativeAbility); NativeApply(source);
                var buff = f.Native<BlueprintBuff>(NativeBuff);
                var area = f.Native<BlueprintAbilityAreaEffect>(NativeArea);
                var originalEffect = f.Native<BlueprintBuff>(NativeEffect);
                if (buff.GetComponents<AddAreaEffect>().Count(c => c.AreaEffect?.AssetGuid == area.AssetGuid) != 1)
                    throw new InvalidOperationException("Native Time Stop area link changed; discovery registration deferred.");
                var resource = f.Add(new BlueprintAbilityResource(), ResourceToken, "Time Stutter", "Daily uses: 1, increasing to 2 at wizard level 15 and 3 at wizard level 20.");
                var amount = Get(resource, "m_MaxAmount");
                Set(amount, "BaseValue", 1); Set(amount, "IncreasedByLevel", false); Set(amount, "IncreasedByStat", false);
                Set(amount, "IncreasedByLevelStartPlusDivStep", true); Set(amount, "StartingLevel", 15); Set(amount, "StartingIncrease", 1);
                Set(amount, "LevelStep", 5); Set(amount, "PerStepIncrease", 1); Set(amount, "MinClassLevelIncrease", 0); Set(amount, "OtherClassesModifier", 0.0f);
                Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>()); Set(amount, "m_ClassDiv", new[] { Ref<BlueprintCharacterClassReference>(wizard) });
                Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>()); Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
                Set(resource, "m_MaxAmount", amount); Set(resource, "m_Min", 0);
                resource.LocalizedName = Text(ResourceToken, "name", "Time Stutter"); resource.LocalizedDescription = Text(ResourceToken, "description", "Daily uses: 1, increasing to 2 at wizard level 15 and 3 at wizard level 20.");
                var graph = TimeStopEngine01438.BuildGraph(f, HostToken, AreaToken, EffectToken, "Time Stutter", Description);
                var ability = f.Clone(source, AbilityToken, "Time Stutter", Description);
                TimeStopEngine01438.PrepareAbilityClone(ability);
                ability.ComponentsArray = ability.ComponentsArray.Where(c => !(c is SpellListComponent)).ToArray();
                var copied = NativeApply(ability);
                Set(copied, "m_Buff", Ref<BlueprintBuffReference>(graph.Host));
                copied.DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = 1 };
                copied.UseDurationSeconds = false; copied.Permanent = false; copied.SameDuration = false;
                ability.Type = AbilityType.SpellLike; ability.AvailableMetamagic = 0;
                ability.LocalizedDuration = Text(AbilityToken, "duration", "1 round");
                var cost = new AbilityResourceLogic { Amount = 1 }; Set(cost, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource)); Set(cost, "m_IsSpendResource", true);
                ability.ComponentsArray = ability.ComponentsArray.Concat(new BlueprintComponent[] { cost }).ToArray();
                var wiz = new PrerequisiteClassLevel { Level = 10, Group = Prerequisite.GroupType.Any }; Set(wiz, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(wizard));
                var arc = new PrerequisiteClassLevel { Level = 20, Group = Prerequisite.GroupType.Any }; Set(arc, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(arcanist));
                var grant = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true }; Set(grant, "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
                var root = f.Feature(RootToken, "Time Stutter", Description, wiz, arc, Facts(ability), grant);
                root.Groups = Array.Empty<FeatureGroup>(); root.Ranks = 1; root.IsClassFeature = true; Set(root, "m_Icon", source.Icon);
                var staged = f.Assets.Values.ToArray();
                bool registeredGraph = false;
                try {
                    Migration01426.Commit(f, Array.Empty<BlueprintFeatureSelection>(), root);
                    var assets = staged.Select(bp => cache.Load(bp.AssetGuid) as BlueprintScriptableObject).ToArray();
                    if (!assets.Zip(staged, ReferenceEquals).All(same => same)) throw new InvalidOperationException("Time Stutter publication changed during registration.");
                    var definitions = assets.Select(Definition).ToArray();
                    registeredGraph = TimeStopEngine01438.RegisterGraph(cache, graph.Area, graph.Effect);
                    state.Assets = assets; state.Definitions = definitions;
                    state.Root = (BlueprintFeature)cache.Load(root.AssetGuid); // Complete receipt is published last.
                } catch {
                    if (registeredGraph) TimeStopEngine01438.UnregisterGraph(graph.Area, graph.Effect);
                    foreach (var bp in staged) if (ReferenceEquals(cache.Load(bp.AssetGuid), bp)) cache.RemoveCachedBlueprint(bp.AssetGuid);
                    state.Assets = null; state.Definitions = null; state.Root = null; throw;
                }
                WizardDiscoveries01435.ReconcileTimeStutter(cache, state.Root);
            }
        }
        internal static void RegisterOptional(BlueprintsCache cache)
        {
            // A foreign same-GUID root can satisfy the dispatcher's root gate.
            // Revalidate ownership instead of turning an earlier Failed into
            // Registered through an unexecuted optional no-op.
            if (cache.Load(Id(RootToken)) != null) Register(cache);
        }
    }
}
