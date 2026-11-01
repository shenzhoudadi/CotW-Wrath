using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // A single archetype migration. Its nine facets are private subresources.
    internal static class Invoker01437
    {
        internal const string ModuleName = "Witch archetype: Invoker";
        internal const string RootToken = "InvokerWitchArchetype01437";
        internal const string WitchClassId = "1b9873f1e7bfe5449bc84d03e9c8e3cc";
        internal const string WitchProgressionId = "c5e9e2b086a9814409fb6a29a5acdd0f";
        internal const string HexPoolId = "9846043cf51251a4897728ed6e24e76f";
        internal const string PatronPoolId = "381cf4c890815d049a4420c6f31d063f";
        internal const string FeatureToken = "InvokePatronFeature01437", SecondToken = "SecondInvocationFeature01437";
        internal const string ResourceToken = "InvokePatronResource01437", AbilityToken = "InvokePatronAbility01437";
        internal static readonly string[] Facets = { "Bondage", "Bridge", "Crisis", "Curiosity", "Decisions", "Fortune", "Paradise", "Reckoning", "Revelation" };
        internal static readonly string[] OwnTokens = new[] { RootToken, FeatureToken, SecondToken, ResourceToken, AbilityToken }
            .Concat(Facets.SelectMany(n => new[] { n + "InvokerPreference01437", n + "InvokerEffect01437", n + "InvokerToggle01437" })).ToArray();
        internal static readonly BlueprintGuid WitchId = BlueprintGuid.Parse(WitchClassId), FeatureId = Id(FeatureToken);
        static readonly Dictionary<BlueprintGuid, int> ToggleChoices = Facets.Select((name, index) => new { Id = Id(name + "InvokerToggle01437"), Index = index }).ToDictionary(v => v.Id, v => v.Index);
        static readonly string[] Descriptions = {
            "+2 enhancement bonus to Strength, increasing to +4 at witch level 8 and +6 at level 16.",
            "Your damage treats physical damage reduction as 2 lower, increasing to 5 lower at witch level 8 and 10 lower at level 16. This does not lower energy resistance, spell resistance, hardness, or immunity.",
            "+2 enhancement bonus to Constitution, increasing to +4 at witch level 8 and +6 at level 16.",
            "The DCs of your witch patron spells increase by 1. At witch level 8 this also applies to your witch hexes. At level 16 both bonuses increase to +2. Other spellbooks, items and unrelated supernatural abilities are unaffected.",
            "+2 enhancement bonus to Dexterity, increasing to +4 at witch level 8 and +6 at level 16.",
            "+1 resistance bonus to saving throws, increasing to +3 at witch level 8 and +5 at level 16.",
            "+3 competence bonus on Mobility checks and concentration checks, increasing to +6 at witch level 8 and +9 at level 16.",
            "Your witch spell attacks and natural attacks gain an insight bonus of +1 damage per die. At witch level 8 they gain +4 on critical confirmation rolls. At level 16 their critical threat range doubles as with Improved Critical; this does not stack with another doubling. Area spells without an attack roll, damage over time and spells from other books do not qualify.",
            "+1 insight bonus on weapon attack rolls, increasing to +2 at witch level 8 and +3 at level 16. Spell touch attacks and rays are unaffected."
        };
        const string Description = "Invite facets of your patron into yourself. Select one facet using the free preference toggles, then use Invoke Patron as a swift action to gain the selected benefit for 1 minute. Each activation consumes 1 minute from a daily pool equal to your witch level, including reactivation before the previous minute ends. Preference changes apply to your next invocation. At witch level 10 you may select two different facets. Replaces the hexes at levels 1, 8, 10 and 16; the familiar, patron, spellbook and other hexes are retained.";
        sealed class Receipt
        {
            internal BlueprintScriptableObject[] Assets;
            internal string[] Definitions;
            internal BlueprintArchetype Root;
            internal UIGroup Group;
            internal BlueprintCharacterClass Witch;
            internal BlueprintProgression Progression;
        }
        static readonly ConditionalWeakTable<BlueprintsCache, Receipt> Published = new ConditionalWeakTable<BlueprintsCache, Receipt>();
        internal static int Level(UnitEntityData unit) => unit?.Progression?.Classes?.Where(c => c.CharacterClass.AssetGuid == WitchId).Select(c => c.Level).DefaultIfEmpty(0).Max() ?? 0;
        internal static int Tier(UnitEntityData unit) { int n = Level(unit); return n >= 16 ? 2 : n >= 8 ? 1 : 0; }
        internal static int Capacity(UnitEntityData unit) => Level(unit) >= 10 ? 2 : 1;
        internal static bool Has(UnitEntityData unit) => Level(unit) > 0 && unit?.Facts?.List.Any(f => f.IsActive && f.IsTurnedOn && !f.IsDisposed && f.Blueprint.AssetGuid == FeatureId) == true;
        internal static int[] Selected(UnitEntityData unit) => unit?.ActivatableAbilities?.Enumerable
            .Where(a => a.IsOn && !a.IsDisposed).Select(a => ToggleChoices.TryGetValue(a.Blueprint.AssetGuid, out int index) ? index : -1)
            .Where(i => i >= 0).Distinct().OrderBy(i => i).ToArray() ?? Array.Empty<int>();
        static LevelEntry Entry(int level, params BlueprintFeatureBase[] features) { var e = new LevelEntry { Level = level }; Set(e, "m_Features", features.Select(Ref<BlueprintFeatureBaseReference>).ToList()); return e; }
        static string Definition(BlueprintScriptableObject bp)
        {
            var values = new List<object> { bp.name, bp.GetType().FullName, bp.ComponentsArray };
            // LevelEntry.Features builds a lazy m_FeaturesList proxy during
            // normal UI/progression reads. Validate the authored raw list, not
            // that transient cache, while retaining level/order/reference checks.
            object[] Entries(LevelEntry[] rows) => rows?.Select(e => e == null ? null : new object[] { e.Level, Get(e, "m_Features") }).ToArray();
            if (bp is BlueprintArchetype a) values.AddRange(new object[] { a.LocalizedName.Key, a.LocalizedDescription.Key, Entries(a.AddFeatures), Entries(a.RemoveFeatures), Get(a, "m_ParentClass") });
            if (bp is BlueprintFeature f) values.AddRange(new object[] { f.Groups, f.Ranks, f.IsClassFeature });
            if (bp is BlueprintBuff b) values.AddRange(new object[] { b.Stacking, b.IsClassFeature });
            if (bp is BlueprintAbility s) values.AddRange(new object[] { s.Type, s.ActionType, s.Range, s.CanTargetSelf, s.CanTargetFriends, s.CanTargetEnemies });
            if (bp is BlueprintActivatableAbility t) values.AddRange(new object[] { t.Group, t.ActivationType, Get(t, "m_Buff"), Get(t, "m_ActivateWithUnitCommand") });
            if (bp is BlueprintAbilityResource r) values.Add(Get(r, "m_MaxAmount"));
            return Migration01426.Definition(values);
        }
        internal static void RegisterOptional(BlueprintsCache cache) => Register(cache);
        internal static void Register(BlueprintsCache cache)
        {
            var f = new PsychicAssets(cache);
            var witch = f.Native<BlueprintCharacterClass>(WitchClassId);
            var progression = f.Native<BlueprintProgression>(WitchProgressionId);
            var hex = f.Native<BlueprintFeatureSelection>(HexPoolId);
            var patrons = f.Native<BlueprintFeatureSelection>(PatronPoolId);
            if (witch.name != "WitchClass" || witch.Progression != progression || hex.name != "WitchHexSelection" || patrons.name != "WitchPatronSelection" ||
                new[] { 1, 8, 10, 16 }.Any(n => progression.LevelEntries?.Count(e => e.Level == n && e.Features.Any(x => x?.AssetGuid == hex.AssetGuid)) != 1))
                throw new InvalidOperationException("Invoker native witch progression/hex schema is missing or changed; no class mutation was made.");
            if (Published.TryGetValue(cache, out var receipt)) {
                if (!ReferenceEquals(witch, receipt.Witch) || !ReferenceEquals(progression, receipt.Progression)) throw new InvalidOperationException("Invoker native parent identity changed; foreign parents were preserved.");
                for (int i = 0; i < receipt.Assets.Length; i++)
                    if (!ReferenceEquals(cache.Load(receipt.Assets[i].AssetGuid), receipt.Assets[i]) || Definition(receipt.Assets[i]) != receipt.Definitions[i])
                        throw new InvalidOperationException("Invoker published private asset changed: " + receipt.Assets[i].name);
                Connect(witch, progression, receipt); return;
            }
            foreach (string token in OwnTokens) if (cache.Load(Id(token)) != null) throw new InvalidOperationException("Invoker private identity is already occupied: " + token);
            var resource = f.Add(new BlueprintAbilityResource(), ResourceToken, "Invoke Patron", "Daily minutes equal your witch level.");
            var amount = Get(resource, "m_MaxAmount"); Set(amount, "BaseValue", 0); Set(amount, "IncreasedByLevel", true); Set(amount, "LevelIncrease", 1);
            Set(amount, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(witch) }); Set(amount, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>()); Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            Set(resource, "m_MaxAmount", amount); Set(resource, "m_Min", 0); resource.LocalizedName = Text(ResourceToken, "name", "Invoke Patron"); resource.LocalizedDescription = Text(ResourceToken, "description", "Daily minutes equal your witch level.");
            var effects = new BlueprintBuff[9]; var toggles = new BlueprintActivatableAbility[9];
            var iconIds = new[] { "c7773d1b408fea24dbbb0f7bf3eb864e", "487af80cdfbaad74b8c2fd644c538233", "99cf556b967c2074ca284e127d815711", "ae4d3ad6a8fda1542acf2e9bbc13d113", "3553bda4d6dfe6344ad89b25f7be939a", "1bb08308c9f6a5e4697887cd438b7221", "c074a5d615200494b8f2a9c845799d93", "4ebaf39efb8ffb64baf92784808dc49c", "ef16771cb05d1344989519e87f25b3c5" };
            for (int i = 0; i < Facets.Length; i++) {
                string name = Facets[i]; var components = new List<BlueprintComponent>();
                if (i == 0 || i == 2 || i == 4 || i == 5 || i == 6) {
                    int[] values = i == 5 ? new[] { 1, 3, 5 } : i == 6 ? new[] { 3, 6, 9 } : new[] { 2, 4, 6 };
                    var rank = Inquisitions01426.Scaling(new[] { witch }, AbilityRankType.Default, false); Set(rank, "m_Progression", ContextRankProgression.Custom);
                    var itemType = HarmonyLib.AccessTools.Field(typeof(ContextRankConfig), "m_CustomProgression").FieldType.GetElementType(); var items = Array.CreateInstance(itemType, 3);
                    for (int j = 0; j < 3; j++) { var item = Activator.CreateInstance(itemType); Set(item, "BaseValue", j == 0 ? 7 : j == 1 ? 15 : 100); Set(item, "ProgressionValue", values[j]); items.SetValue(item, j); }
                    Set(rank, "m_CustomProgression", items); components.Add(rank);
                    var stats = i == 0 ? new[] { StatType.Strength } : i == 2 ? new[] { StatType.Constitution } : i == 4 ? new[] { StatType.Dexterity } : i == 5 ? new[] { StatType.SaveFortitude, StatType.SaveReflex, StatType.SaveWill } : new[] { StatType.SkillMobility };
                    foreach (var stat in stats) components.Add(new AddContextStatBonus { Stat = stat, Descriptor = i == 5 ? ModifierDescriptor.Resistance : i == 6 ? ModifierDescriptor.Competence : ModifierDescriptor.Enhancement, Multiplier = 1, Value = Inquisitions01426.Rank() });
                    if (i == 6) components.Add(new ConcentrationBonus { Value = Inquisitions01426.Rank() });
                } else components.Add(new InvokerFacetRules01437 { Facet = i, Hexes = Ref<BlueprintFeatureSelectionReference>(hex), Patrons = Ref<BlueprintFeatureSelectionReference>(patrons) });
                var effect = f.Buff(name + "InvokerEffect01437", "Invoke Patron: " + name, Descriptions[i], components.ToArray()); effect.Stacking = StackingType.Stack; effect.IsClassFeature = true; effects[i] = effect;
                var preference = f.Buff(name + "InvokerPreference01437", "Next Invocation: " + name, Descriptions[i]); preference.IsClassFeature = true;
                var toggle = f.Add(new BlueprintActivatableAbility { ActivationType = AbilityActivationType.Immediately, Group = ActivatableAbilityGroup.None,
                    DoNotTurnOffOnRest = true, DeactivateIfCombatEnded = false, ComponentsArray = new BlueprintComponent[] { new InvokerFacetRestriction01437 { Facet = i } } }, name + "InvokerToggle01437", "Invoke Patron: Select " + name, Descriptions[i] + " Select before using Invoke Patron. This choice does not alter an invocation already in effect.");
                Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(preference)); Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free); toggles[i] = toggle;
                var nativeIcon = cache.Load(BlueprintGuid.Parse(iconIds[i])) as BlueprintScriptableObject;
                if (nativeIcon != null) foreach (var bp in new BlueprintScriptableObject[] { effect, preference, toggle }) Set(bp, "m_Icon", Get(nativeIcon, "m_Icon"));
            }
            var invoke = f.Ability(AbilityToken, "Invoke Patron", Description, AbilityType.Supernatural, UnitCommand.CommandType.Swift, AbilityRange.Personal,
                new InvokerInvocationRestriction01437(), new InvokerApply01437 { Effects = effects.Select(Ref<BlueprintBuffReference>).ToArray() });
            var cost = new AbilityResourceLogic { Amount = 1 }; Set(cost, "m_IsSpendResource", true); Set(cost, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource)); invoke.ComponentsArray = invoke.ComponentsArray.Concat(new[] { cost }).ToArray();
            invoke.LocalizedDuration = Text(AbilityToken, "duration", "1 minute");
            var grant = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true }; Set(grant, "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
            var feature = f.Feature(FeatureToken, "Invoke Patron", Description, grant, Facts(toggles.Cast<Kingmaker.Blueprints.Facts.BlueprintUnitFact>().Concat(new[] { invoke }).ToArray()), new InvokerCleanup01437 { Effects = effects.Select(Ref<BlueprintBuffReference>).ToArray() }); feature.IsClassFeature = true; feature.Groups = Array.Empty<FeatureGroup>();
            var second = f.Feature(SecondToken, "Second Invocation", "At witch level 10 select two different facets for each Invoke Patron activation. Both facets share the same one-minute cost and duration."); second.IsClassFeature = true; second.Groups = Array.Empty<FeatureGroup>();
            var invocationIcon = cache.Load(BlueprintGuid.Parse("ef16771cb05d1344989519e87f25b3c5")) as BlueprintScriptableObject;
            if (invocationIcon != null) foreach (var bp in new BlueprintScriptableObject[] { invoke, feature, second }) Set(bp, "m_Icon", Get(invocationIcon, "m_Icon"));
            var root = f.Add(new BlueprintArchetype { LocalizedName = Text(RootToken, "name", "Invoker"), LocalizedDescription = Text(RootToken, "description", Description),
                AddFeatures = new[] { Entry(1, feature), Entry(10, second) }, RemoveFeatures = new[] { Entry(1, hex), Entry(8, hex), Entry(10, hex), Entry(16, hex) }, ComponentsArray = Array.Empty<BlueprintComponent>() }, RootToken, "Invoker", Description);
            Set(root, "m_ParentClass", witch);
            var group = new UIGroup(); Set(group, "m_Features", new BlueprintFeatureBase[] { feature, second }.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            var staged = OwnTokens.Select(t => f.Assets[t]).ToArray(); foreach (var bp in staged) PsychicRepairComponents.UniqueNames(bp);
            var next = new Receipt { Assets = staged, Root = root, Group = group, Witch = witch, Progression = progression };
            var oldArchetypes = Get(witch, "m_Archetypes"); var oldGroups = progression.UIGroups;
            try {
                f.Enable(); foreach (var bp in staged) cache.AddCachedBlueprint(bp.AssetGuid, bp);
                Connect(witch, progression, next); next.Definitions = staged.Select(Definition).ToArray(); Published.Add(cache, next);
            } catch {
                Set(witch, "m_Archetypes", oldArchetypes); progression.UIGroups = oldGroups;
                foreach (var bp in staged) if (ReferenceEquals(cache.Load(bp.AssetGuid), bp)) cache.RemoveCachedBlueprint(bp.AssetGuid);
                throw;
            }
        }
        static void Connect(BlueprintCharacterClass witch, BlueprintProgression progression, Receipt receipt)
        {
            var before = Get(witch, "m_Archetypes"); var groups = progression.UIGroups;
            try {
                var refs = (BlueprintArchetypeReference[])before ?? Array.Empty<BlueprintArchetypeReference>();
                var matching = refs.Where(r => r?.Guid == receipt.Root.AssetGuid).ToArray();
                if (matching.Length > 1 || matching.Length == 1 && !ReferenceEquals(matching[0].Get(), receipt.Root)) throw new InvalidOperationException("Invoker witch archetype reference collision.");
                if (matching.Length == 0) Set(witch, "m_Archetypes", refs.Concat(new[] { Ref<BlueprintArchetypeReference>(receipt.Root) }).ToArray());
                var own = (groups ?? Array.Empty<UIGroup>()).Where(g => g?.Features.Any(x => x?.AssetGuid == Id(FeatureToken) || x?.AssetGuid == Id(SecondToken)) == true).ToArray();
                if (own.Length == 0) progression.UIGroups = (groups ?? Array.Empty<UIGroup>()).Concat(new[] { receipt.Group }).ToArray();
                else if (own.Length != 1 || !ReferenceEquals(own[0], receipt.Group)) throw new InvalidOperationException("Invoker class UI group collision.");
            } catch { Set(witch, "m_Archetypes", before); progression.UIGroups = groups; throw; }
        }
    }

    [TypeId("d0a0143752754dab8e01100000000001")]
    public sealed class InvokerFacetRestriction01437 : ActivatableAbilityRestriction
    {
        public int Facet;
        protected override bool IsAvailable() { var selected = Invoker01437.Selected(Owner); return Invoker01437.Has(Owner) && (selected.Contains(Facet) || selected.Length < Invoker01437.Capacity(Owner)); }
    }
    [TypeId("d0a0143752754dab8e01100000000002")]
    public sealed class InvokerInvocationRestriction01437 : BlueprintComponent, IAbilityRestriction
    {
        public bool IsAbilityRestrictionPassed(AbilityData ability) { var unit = ability?.Caster?.Unit; int count = Invoker01437.Selected(unit).Length; return Invoker01437.Has(unit) && count > 0 && count <= Invoker01437.Capacity(unit); }
        public string GetAbilityRestrictionUIText() => "Select one patron facet, or up to two at witch level 10, before invoking your patron.";
    }
    [TypeId("d0a0143752754dab8e01100000000003")]
    public sealed class InvokerApply01437 : AbilityApplyEffect
    {
        public BlueprintBuffReference[] Effects;
        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var caster = context?.MaybeCaster; var selected = Invoker01437.Selected(caster);
            if (!Invoker01437.Has(caster) || selected.Length == 0 || selected.Length > Invoker01437.Capacity(caster)) return;
            var old = caster.Buffs.Enumerable.Where(b => Effects.Any(r => r.Guid == b.Blueprint.AssetGuid) && b.Context?.MaybeCaster == caster).ToArray();
            var added = new List<Buff>();
            try {
                foreach (int index in selected) { var buff = caster.Buffs.AddBuff(Effects[index].Get(), context, TimeSpan.FromMinutes(1)); if (buff == null) throw new InvalidOperationException("Invoker self facet could not be applied."); buff.IsNotDispelable = true; added.Add(buff); }
            } catch { foreach (var buff in added) buff.Remove(); throw; }
            foreach (var buff in old) buff.Remove();
        }
    }
    [TypeId("d0a0143752754dab8e01100000000004")]
    public sealed class InvokerCleanup01437 : UnitFactComponentDelegate
    {
        public BlueprintBuffReference[] Effects;
        protected override void OnTurnOff() { foreach (var buff in Owner.Buffs.Enumerable.Where(b => Effects.Any(r => r.Guid == b.Blueprint.AssetGuid) && b.Context?.MaybeCaster == Owner).ToArray()) buff.Remove(); }
    }
    [TypeId("d0a0143752754dab8e01100000000005")]
    public sealed class InvokerFacetRules01437 : UnitBuffComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>, IInitiatorRulebookHandler<RuleCalculateWeaponStats>,
        IInitiatorRulebookHandler<RuleAttackRoll>, IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IInitiatorRulebookSubscriber
    {
        public int Facet;
        public BlueprintFeatureSelectionReference Hexes, Patrons;
        static readonly ConditionalWeakTable<BaseDamage, object> DamageSeen = new ConditionalWeakTable<BaseDamage, object>();
        static readonly ConditionalWeakTable<BaseDamage, object> BridgeSeen = new ConditionalWeakTable<BaseDamage, object>();
        static readonly ConditionalWeakTable<RulebookEvent, object> Seen = new ConditionalWeakTable<RulebookEvent, object>();
        bool Active => Buff.IsActive && Buff.IsTurnedOn && !Buff.IsDisposed && !Buff.IsSuppressed && Invoker01437.Has(Owner) &&
            Owner.Buffs.Enumerable.FirstOrDefault(b => b.IsActive && b.IsTurnedOn && !b.IsDisposed && !b.IsSuppressed && b.Blueprint.AssetGuid == Buff.Blueprint.AssetGuid && b.Context?.MaybeCaster == Owner) == Buff;
        static bool Mark<T>(ConditionalWeakTable<T, object> table, T key) where T : class { if (table.TryGetValue(key, out _)) return false; table.Add(key, new object()); return true; }
        bool WitchSpell(MechanicsContext context) => context?.MaybeCaster == Owner && context.SourceAbility?.Type == AbilityType.Spell &&
            context.SourceAbilityContext?.Ability?.SourceItem == null && context.SourceAbilityContext?.Ability?.Spellbook?.Blueprint?.CharacterClass?.AssetGuid == Invoker01437.WitchId;
        bool Attack(Kingmaker.Items.ItemEntityWeapon weapon, MechanicsContext context) => weapon?.Blueprint?.IsNatural == true || WitchSpell(context);
        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            if (!Active || evt?.Initiator != Owner) return;
            if (Facet != 7 || evt.ParentRule?.AttackRoll == null || !Attack(evt.ParentRule.AttackRoll.Weapon, evt.Reason?.Context)) return;
            foreach (var damage in evt.DamageBundle) if (damage.Dice.ModifiedValue.Rolls > 0 && Mark(DamageSeen, damage))
                damage.AddModifier(new Modifier(damage.Dice.ModifiedValue.Rolls, Fact, ModifierDescriptor.Insight));
        }
        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
            if (!Active || Facet != 1 || evt?.Initiator != Owner) return;
            // The native body assigns target DR after constructing DamageValue.
            // DamageValue reads its source lazily. Cap this late penalty at the
            // remaining DR so an excess cannot spill into native hardness.
            int amount = new[] { 2, 5, 10 }[Invoker01437.Tier(Owner)];
            foreach (var damage in evt.DamageBundle.OfType<PhysicalDamage>()) {
                int effective = Math.Min(amount, Math.Max(0, (int)damage.ReductionBecauseResistance - (int)damage.ReductionPenalty));
                if (effective > 0 && Mark(BridgeSeen, damage)) damage.ReductionPenalty.Add(new Modifier(effective, Fact, ModifierDescriptor.UntypedStackable));
            }
        }
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if (Active && Facet == 7 && Invoker01437.Level(Owner) >= 16 && evt?.Initiator == Owner && Attack(evt.Weapon, evt.Reason?.Context)) evt.DoubleCriticalEdge = true;
        }
        public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            if (Active && Facet == 7 && Invoker01437.Level(Owner) >= 8 && evt?.Initiator == Owner && Attack(evt.Weapon, evt.Reason?.Context) && Mark(Seen, evt)) evt.CriticalConfirmationBonus += 4;
        }
        public void OnEventDidTrigger(RuleAttackRoll evt) { }
        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            if (Active && Facet == 8 && evt?.Initiator == Owner && evt.Weapon != null && evt.Weapon.Blueprint.Category != WeaponCategory.Touch && evt.Weapon.Blueprint.Category != WeaponCategory.Ray && Mark(Seen, evt))
                evt.AddModifier(1 + Invoker01437.Tier(Owner), Fact, ModifierDescriptor.Insight);
        }
        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }
        static bool SameSpell(BlueprintAbility actual, BlueprintAbility candidate) => actual != null && candidate != null && (actual.AssetGuid == candidate.AssetGuid ||
            actual.Parent?.AssetGuid == candidate.AssetGuid && candidate.GetComponent<AbilityVariants>()?.Variants.Any(a => a?.AssetGuid == actual.AssetGuid) == true);
        bool Patron(BlueprintAbility spell)
        {
            foreach (var p in Patrons?.Get()?.AllFeatures.OfType<BlueprintProgression>() ?? Enumerable.Empty<BlueprintProgression>()) {
                if (!Owner.HasFact(p)) continue;
                foreach (var feature in (p.LevelEntries ?? Array.Empty<LevelEntry>()).Where(e => e != null).SelectMany(e => e.Features).Where(x => x?.ComponentsArray != null))
                    foreach (var known in feature.GetComponents<AddKnownSpell>()) if (SameSpell(spell, known.Spell)) return true;
            }
            return false;
        }
        bool Hex(BlueprintAbility spell)
        {
            foreach (var feature in Hexes?.Get()?.AllFeatures.ToArray() ?? Array.Empty<BlueprintFeature>())
                if (feature?.ComponentsArray != null && Owner.HasFact(feature)) foreach (var facts in feature.GetComponents<AddFacts>())
                    if (Get(facts, "m_Facts") != null && facts.Facts.OfType<BlueprintAbility>().Any(a => SameSpell(spell, a))) return true;
            return false;
        }
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (!Active || Facet != 3 || evt?.Initiator != Owner || evt.Spell == null || evt.AbilityData?.SourceItem != null) return;
            int level = Invoker01437.Level(Owner); bool spell = evt.Spell.Type == AbilityType.Spell && evt.Spellbook?.Blueprint?.CharacterClass?.AssetGuid == Invoker01437.WitchId && Patron(evt.Spell);
            bool hex = level >= 8 && evt.Spell.Type == AbilityType.Supernatural && evt.Spellbook == null && Hex(evt.Spell);
            if ((spell || hex) && Mark(Seen, evt)) evt.AddBonusDC(level >= 16 ? 2 : 1, ModifierDescriptor.UntypedStackable);
        }
        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
    }
}
