using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Each utility talent publishes independently. A missing source for one
    // talent does not roll back or change the other two modules.
    internal static class KineticUtility01433
    {
        internal const string WindsightModule = "Wild talent: Windsight";
        internal const string ColdSnapModule = "Wild talent: Cold Snap";
        internal const string SmokeStormModule = "Wild talent: Smoke Storm";
        internal const string WindsightToken = "WindsightWildTalent";
        internal const string ColdSnapToken = "ColdSnapWildTalent";
        internal const string SmokeStormToken = "SmokeStormWildTalent";
        internal const string AirPrerequisiteId = "b182f559f572da342b54bece4404e4e7";
        internal const string WaterPrerequisiteId = "1d42456e6113739499e1bda025e0ba03";
        internal const string ColdAdaptationId = "1ff5d6e76b7c2fa48be555b77d1ad8b2";
        internal const string ShroudOfWaterId = "29ec36fa2a5b8b94ebce170bd369083a";
        internal const string SickenedId = "4e42460798665fd4cb9173ffa7ada323";
        internal static readonly string[] Dependencies = { PurifyingFlames01428.PoolId, PurifyingFlames01428.ClassId,
            AirPrerequisiteId, WaterPrerequisiteId, PurifyingFlames01428.FoxfireId, ColdAdaptationId, ShroudOfWaterId,
            PurifyingFlames01428.HealerId, SickenedId, "734a29b693e9ec346ba2951b27987e33", "fd389783027d63343b4a5634bd81645f", "198fd8924dabcb5478d0f78bd453c586" };
        sealed class Publication {
            internal BlueprintFeature Root;
            internal BlueprintScriptableObject[] Assets;
            internal string[] Definitions;
        }
        static string OwnedDefinition(BlueprintScriptableObject bp) {
            string result = bp.GetType().FullName + "|" + bp.AssetGuid + "|" + bp.name + "|" + Migration01426.Definition(bp.ComponentsArray);
            if (bp is BlueprintFeature feature) result += "|" + feature.IsClassFeature + "|" + string.Join(",", feature.Groups) + "|" + feature.Ranks;
            if (bp is BlueprintAbility ability) result += "|" + ability.Type + "|" + ability.ActionType + "|" + ability.Range + "|" + ability.CanTargetPoint + "|" + ability.CanTargetSelf + "|" + ability.CanTargetFriends + "|" + ability.CanTargetEnemies + "|" + ability.SpellResistance + "|" + ability.AvailableMetamagic;
            if (bp is BlueprintAbilityAreaEffect area) result += "|" + area.Shape + "|" + area.Size.Value + "|" + area.SpellResistance + "|" + area.AffectEnemies + "|" + area.Fx?.AssetId;
            if (bp is BlueprintBuff buff) result += "|" + buff.Stacking + "|" + Get(buff, "m_Flags");
            if (bp is BlueprintActivatableAbility toggle) result += "|" + toggle.ActivationType + "|" + toggle.Group + "|" + toggle.Buff?.AssetGuid + "|" + Get(toggle, "m_ActivateWithUnitCommand");
            return result;
        }
        static readonly ConditionalWeakTable<BlueprintsCache, Dictionary<string, Publication>> Publications = new ConditionalWeakTable<BlueprintsCache, Dictionary<string, Publication>>();
        internal static void Register(BlueprintsCache cache) { RegisterWindsight(cache); RegisterColdSnap(cache); RegisterSmokeStorm(cache); }
        static bool Restore(BlueprintsCache cache, string token) {
            if (!Publications.TryGetValue(cache, out var publications) || !publications.TryGetValue(token, out var published)) return false;
            for (int i = 0; i < published.Assets.Length; i++) {
                var expected = published.Assets[i]; var actual = cache.Load(expected.AssetGuid) as BlueprintScriptableObject;
                if (!ReferenceEquals(actual, expected) || actual.ComponentsArray == null || actual.ComponentsArray.Any(c => c == null) ||
                    OwnedDefinition(actual) != published.Definitions[i])
                    throw new InvalidOperationException("Kinetic utility published identity or definition changed: " + expected.name);
            }
            var f = new PsychicAssets(cache);
            Migration01426.Commit(f, new[] { f.Native<BlueprintFeatureSelection>(PurifyingFlames01428.PoolId) }, published.Root);
            return true;
        }

        static BlueprintFeature Root(PsychicAssets f, string token, string title, string text, string element, params BlueprintComponent[] extra)
        {
            var source = f.Native<BlueprintFeature>(element).GetComponents<PrerequisiteFeaturesFromList>().ToArray();
            if (source.Length != 1 || source[0].Features.Length == 0 || source[0].Features.Any(x => x == null))
                throw new InvalidOperationException(title + " elemental prerequisite contract changed: components=" + source.Length +
                    "; unresolved=" + (source.Length == 1 ? source[0].Features.Count(x => x == null).ToString() : "n/a") + ".");
            var level = new PrerequisiteClassLevel { Level = 6 };
            Set(level, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(f.Native<BlueprintCharacterClass>(PurifyingFlames01428.ClassId)));
            var root = f.Feature(token, title, text, new BlueprintComponent[0]);
            var once = new PrerequisiteNoFeature(); Set(once, "m_Feature", Ref<BlueprintFeatureReference>(root));
            root.ComponentsArray = new BlueprintComponent[] { CopyData(source[0]), level, once }.Concat(extra).ToArray();
            root.Groups = new[] { FeatureGroup.KineticWildTalent }; root.IsClassFeature = true;
            Set(root, "m_Icon", Get(f.Native<BlueprintFeature>(element), "m_Icon"));
            return root;
        }
        static void Commit(PsychicAssets f, BlueprintFeature root) {
            Migration01426.Commit(f, new[] { f.Native<BlueprintFeatureSelection>(PurifyingFlames01428.PoolId) }, root);
            var assets = f.Assets.Values.Select(a => (BlueprintScriptableObject)f.Cache.Load(a.AssetGuid)).ToArray();
            Publications.GetOrCreateValue(f.Cache)[root.AssetGuid.ToString()] = new Publication {
                Root = (BlueprintFeature)f.Cache.Load(root.AssetGuid), Assets = assets, Definitions = assets.Select(OwnedDefinition).ToArray() };
        }
        internal static void RegisterWindsight(BlueprintsCache cache)
        {
            if (Restore(cache, Id(WindsightToken).ToString())) return;
            var f = new PsychicAssets(cache);
            const string text = "Element: air. Utility wild talent, level 3; requires kineticist level 6. Burn: 0. You see through mist and fog, including magical fog. This does not remove invisibility, blindness, blur, displacement, or non-fog concealment; other effects of a cloud still apply.";
            Commit(f, Root(f, WindsightToken, "Windsight", text, AirPrerequisiteId, new KineticWindsight01433()));
        }
        static AbilityKineticist Burn(PsychicAssets f, int amount)
        {
            var source = f.Native<BlueprintAbility>(PurifyingFlames01428.HealerId).GetComponents<AbilityKineticist>().ToArray();
            if (source.Length != 1) throw new InvalidOperationException("Kinetic utility native burn contract changed.");
            var result = CopyData(source[0]); result.Amount = result.WildTalentBurnCost = amount;
            result.InfusionBurnCost = result.BlastBurnCost = 0; return result;
        }
        static BlueprintComponent[] Parameters(PsychicAssets f, string token)
        {
            var result = Inquisitions01426.Parameters(f, token, new[] { f.Native<BlueprintCharacterClass>(PurifyingFlames01428.ClassId) }, StatType.Constitution);
            foreach (var c in result.OfType<ContextCalculateAbilityParams>()) c.UseKineticistMainStat = true;
            return result;
        }
        internal static void RegisterColdSnap(BlueprintsCache cache)
        {
            if (Restore(cache, Id(ColdSnapToken).ToString())) return;
            var f = new PsychicAssets(cache);
            const string text = "Element: water. Utility wild talent, level 3; requires kineticist level 6, cold adaptation and shroud of water. Accept 1 burn as a standard action to unlock Cold Snap until your burn is removed or you rest. You can then start or stop its 5-foot aura as a swift action without more burn. All other creatures in the aura take a -4 penalty to Dexterity. Cold immunity or cold resistance 5 or higher prevents this penalty. Other sources of Cold Snap do not stack. The aura stops if its owner dies or loses the talent or activation effect.";
            var penalty = f.Buff("ColdSnapPenaltyBuff", "Cold Snap", text, new KineticColdPenalty01433());
            penalty.Stacking = StackingType.Stack; SetEnum(penalty, "m_Flags", "HiddenInUi");
            var nonCaster = new KineticColdTarget01433();
            var area = PsychicWeatherSpells.Area(f, "ColdSnap", "Cold Snap", text,
                new Kingmaker.ResourceLinks.PrefabLink { AssetId = "" }, AreaEffectShape.Cylinder, 5, false,
                PsychicWeatherSpells.Owned(penalty, nonCaster));
            var auraSource = new AddAreaEffect(); Set(auraSource, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(area));
            var aura = f.Buff("ColdSnapAuraBuff", "Cold Snap", text, auraSource);
            var enabled = f.Buff("ColdSnapEnabledBuff", "Cold Snap: Activated", text,
                new KineticColdActivation01433 { Aura = Ref<BlueprintBuffReference>(aura) });
            SetEnum(enabled, "m_Flags", "RemoveOnRest");
            var requirement = new KineticColdToggleRestriction01433 { Activation = Ref<BlueprintBuffReference>(enabled) };
            var toggle = f.Add(new BlueprintActivatableAbility { ActivationType = AbilityActivationType.WithUnitCommand,
                Group = ActivatableAbilityGroup.None, DeactivateIfOwnerDisabled = true, DeactivateIfOwnerUnconscious = true, DeactivateIfCombatEnded = false,
                ComponentsArray = new BlueprintComponent[] { requirement } }, "ColdSnapToggle", "Cold Snap", text);
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(aura)); Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Swift);
            var apply = Apply(enabled, 0, DurationRate.Rounds); apply.Permanent = true; apply.IsNotDispelable = true;
            var ability = f.Ability("ColdSnapEnableAbility", "Activate Cold Snap", text, AbilityType.SpellLike,
                UnitCommand.CommandType.Standard, AbilityRange.Personal,
                new KineticColdEnableRestriction01433 { Activation = Ref<BlueprintBuffReference>(enabled) }, Run(apply), Burn(f, 1));
            ability.ComponentsArray = ability.ComponentsArray.Concat(Parameters(f, "ColdSnapActivation")).ToArray();
            ability.CanTargetSelf = true; ability.CanTargetFriends = ability.CanTargetEnemies = ability.CanTargetPoint = false;
            ability.NotOffensive = true; ability.SpellResistance = false; ability.AvailableMetamagic = 0;
            ability.LocalizedDuration = Text("ColdSnapEnableAbility", "duration", "Until burn is removed or you rest");
            var root = Root(f, ColdSnapToken, "Cold Snap", text, WaterPrerequisiteId,
                Migration01426.Require(f.Native<BlueprintFeature>(ColdAdaptationId)), Migration01426.Require(f.Native<BlueprintFeature>(ShroudOfWaterId)),
                Facts(ability, toggle), new KineticColdOwner01433 { Activation = Ref<BlueprintBuffReference>(enabled), Aura = Ref<BlueprintBuffReference>(aura) });
            foreach (var bp in new BlueprintUnitFact[] { penalty, aura, enabled, ability, toggle }) Set(bp, "m_Icon", Get(root, "m_Icon"));
            Commit(f, root);
        }
        internal static void RegisterSmokeStorm(BlueprintsCache cache)
        {
            if (Restore(cache, Id(SmokeStormToken).ToString())) return;
            var f = new PsychicAssets(cache);
            const string text = "Element: fire. Utility wild talent, level 3; requires kineticist level 6. Burn: 0. As a standard action, create a 20-foot-radius smoke cloud within medium range for 1 round per kineticist level. It obscures vision as obscuring mist. Each round, creatures beginning their turns inside attempt a Fortitude save or become sickened for 1d4+1 rounds. Undead, constructs, elementals, and creatures already sickened are unaffected by the sickness. The smoke affects allies and enemies. Only one Smoke Storm from each caster can remain; different casters maintain independent clouds.";
            var donor = cache.Load(Id("ObscuringMistArea")) as BlueprintAbilityAreaEffect
                ?? throw new InvalidOperationException("Smoke Storm requires registered Obscuring Mist.");
            if (donor.Shape != AreaEffectShape.Cylinder || donor.Size.Value != 20 || donor.GetComponents<AbilityAreaEffectBuff>().Count() != 1)
                throw new InvalidOperationException("Smoke Storm mist donor contract changed.");
            var fogDonor = donor.GetComponent<AbilityAreaEffectBuff>().Buff;
            if (fogDonor == null || !fogDonor.GetComponents<AddConcealment>().Any() || fogDonor.GetComponent<PsychicOutgoingFog>() == null)
                throw new InvalidOperationException("Smoke Storm fog donor is incomplete.");
            var fog = f.Clone(fogDonor, "SmokeStormFogBuff", "Smoke Storm", text);
            var area = f.Clone(donor, "SmokeStormArea", "Smoke Storm", text);
            Set(area.GetComponent<AbilityAreaEffectBuff>(), "m_Buff", Ref<BlueprintBuffReference>(fog));
            var sick = f.Native<BlueprintBuff>(SickenedId);
            var apply = Apply(sick, 1, DurationRate.Rounds); apply.DurationValue.DiceType = DiceType.D4; apply.DurationValue.DiceCountValue = 1;
            var save = new ContextActionSavingThrow { Type = SavingThrowType.Fortitude,
                Actions = Actions(Saved(Array.Empty<GameAction>(), new GameAction[] { apply })) };
            var eligible = new KineticSmokeEligible01433 {
                Excluded = new[] { "734a29b693e9ec346ba2951b27987e33", "fd389783027d63343b4a5634bd81645f", "198fd8924dabcb5478d0f78bd453c586" }
                    .Select(id => Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>(id))).ToArray() };
            var condition = new Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional {
                ConditionsChecker = new ConditionsChecker { Conditions = new Condition[] { eligible } }, IfTrue = Actions(save), IfFalse = Actions() };
            var root = Root(f, SmokeStormToken, "Smoke Storm", text, PurifyingFlames01428.FoxfireId);
            var unique = new UniqueAreaEffect(); Set(unique, "m_Feature", Ref<BlueprintUnitFactReference>(root));
            area.ComponentsArray = area.ComponentsArray.Concat(new BlueprintComponent[] { new AbilityAreaEffectRunAction {
                UnitEnter = Actions(), UnitExit = Actions(), UnitMove = Actions(), Round = Actions(condition) },
                new SpellDescriptorComponent { Descriptor = SpellDescriptor.Fire | SpellDescriptor.Sickened }, unique }).ToArray();
            area.AffectEnemies = true; area.SpellResistance = false; area.Size = new Feet(20); Set(area, "m_TickRoundAfterSpawn", false);
            var ability = PsychicWeatherSpells.AreaSpell(f, "SmokeStormAbility", "Smoke Storm", text, area,
                root.Icon, SpellSchool.Conjuration, AbilityRange.Medium, DurationRate.Rounds, SpellDescriptor.Fire | SpellDescriptor.Sickened);
            ability.Type = AbilityType.SpellLike; ability.AvailableMetamagic = 0;
            ability.ComponentsArray = ability.ComponentsArray.Where(c => !(c is ContextCalculateAbilityParams) && !(c is ContextRankConfig)).Concat(Parameters(f, "SmokeStorm"))
                .Concat(new BlueprintComponent[] { Burn(f, 0) }).ToArray();
            ability.LocalizedSavingThrow = Text("SmokeStormAbility", "savingThrow", "Fortitude negates sickness");
            root.ComponentsArray = root.ComponentsArray.Concat(new BlueprintComponent[] { Facts(ability) }).ToArray();
            Commit(f, root);
        }
    }

    [TypeId("f28be72b3fed45c2a0275401dd2bfaa1")]
    public sealed class KineticWindsight01433 : UnitFactComponentDelegate
    {
        static readonly BlueprintGuid FeatureId = Id(KineticUtility01433.WindsightToken);
        internal static bool Active(UnitEntityData unit) => unit?.Facts?.List.Any(f => f.IsTurnedOn && !f.IsDisposed &&
            f.Blueprint.AssetGuid == FeatureId) == true;
        sealed class FogScope : IDisposable
        {
            readonly List<UnitPartConcealment.ConcealmentEntry> list;
            readonly KeyValuePair<int, UnitPartConcealment.ConcealmentEntry>[] removed;
            bool disposed;
            internal FogScope(List<UnitPartConcealment.ConcealmentEntry> entries) {
                list = entries; removed = list.Select((v, i) => new KeyValuePair<int, UnitPartConcealment.ConcealmentEntry>(i, v))
                    .Where(p => p.Value.Descriptor == ConcealmentDescriptor.Fog).ToArray();
                foreach (var p in removed) list.Remove(p.Value);
            }
            public void Dispose() {
                if (disposed) return; disposed = true;
                foreach (var p in removed) if (!list.Any(x => ReferenceEquals(x, p.Value))) list.Insert(Math.Min(p.Key, list.Count), p.Value);
            }
        }
        internal static IDisposable Filter(UnitEntityData initiator, UnitEntityData target) {
            if (!Active(initiator)) return null;
            var part = target?.Get<UnitPartConcealment>();
            var entries = part == null ? null : (List<UnitPartConcealment.ConcealmentEntry>)AccessTools.Field(typeof(UnitPartConcealment), "m_Concealments").GetValue(part);
            return entries == null || !entries.Any(e => e.Descriptor == ConcealmentDescriptor.Fog) ? null : new FogScope(entries);
        }
    }
    [TypeId("8b4529a7870d4a4f9381f1e209872b32")]
    public sealed class KineticColdTarget01433 : ContextCondition
    {
        protected override string GetConditionCaption() => "Another creature inside Cold Snap";
        protected override bool CheckCondition() => Target.Unit != null && Target.Unit != Context.MaybeCaster;
    }
    [TypeId("8917223dc4df41b89cf37c9efea58321")]
    public sealed class KineticColdPenalty01433 : UnitBuffComponentDelegate, IUnitBuffHandler, IUnitGainFactHandler, IUnitLostFactHandler, ITickEachRound
    {
        static readonly BlueprintGuid AuraId = Id("ColdSnapAuraBuff");
        static readonly System.Reflection.FieldInfo Immunities = AccessTools.Field(typeof(UnitPartDamageReduction), "m_Immunities");
        internal static bool Protected(UnitEntityData u, EntityFact leaving = null) {
            if (u == null) return false;
            // BuffCollection raises DidRemoved before fact.Detach turns off
            // its DR component. Ignore precisely that departing source while
            // still respecting other live native immunity/resistance entries.
            bool Live(EntityFact f) => f != null && f != leaving && f.IsTurnedOn && !f.IsDisposed;
            // The native unconditional damage-immunity component bypasses the
            // DR part and is distinct from suppressible AddEnergyImmunity.
            if (u.Facts.List.Any(f => Live(f) && f.Blueprint.GetComponents<AddEnergyDamageImmunity>().Any(i => i.EnergyType == DamageEnergyType.Cold))) return true;
            var dr = u.Get<UnitPartDamageReduction>(); if (dr == null) return false;
            if (!u.State.HasCondition(UnitCondition.SuppressedEnergyImmunity) &&
                ((IEnumerable<UnitPartDamageReduction.Immunity>)Immunities.GetValue(dr)).Any(i => i.Type == DamageEnergyType.Cold && Live(i.SourceFact))) return true;
            if (u.State.HasCondition(UnitCondition.SuppressedEnergyResistance)) return false;
            return dr.AllSources.Any(c => Live(c.Fact) && c.Settings is AddDamageResistanceEnergy energy && energy.Type == DamageEnergyType.Cold && c.GetCurrentValue() >= 5);
        }
        internal void Refresh(bool turningOff = false, EntityFact leaving = null) {
            var effects = Owner.Buffs.Enumerable.Where(b => b.Blueprint.AssetGuid == Fact.Blueprint.AssetGuid).ToArray();
            foreach (var m in Owner.Stats.Dexterity.Modifiers.Where(m => m.Source is Buff b && b.Blueprint.AssetGuid == Fact.Blueprint.AssetGuid).ToArray()) Owner.Stats.Dexterity.RemoveModifier(m);
            if (Owner.State.IsDead || Protected(Owner, leaving)) return;
            var first = effects.Where(b => b.IsTurnedOn && !b.IsSuppressed && !b.IsDisposed && (!turningOff || b != Buff) &&
                b.Context?.MaybeCaster != null && b.Context.MaybeCaster != Owner && !b.Context.MaybeCaster.State.IsDead &&
                b.Context.MaybeCaster.Buffs.Enumerable.Any(a => a.IsTurnedOn && !a.IsSuppressed && !a.IsDisposed && a.Blueprint.AssetGuid == AuraId))
                .OrderBy(b => b.UniqueId, StringComparer.Ordinal).FirstOrDefault();
            if (first != null) Owner.Stats.Dexterity.AddModifier(-4, first, ModifierDescriptor.Penalty);
        }
        protected override void OnTurnOn() => Refresh();
        protected override void OnTurnOff() => Refresh(true);
        protected override void OnApplyPostLoadFixes() => Refresh();
        public void OnNewRound() => Refresh();
        public void HandleBuffDidAdded(Buff b) { if (b.Owner == Owner) Refresh(); }
        public void HandleBuffDidRemoved(Buff b) { if (b.Owner == Owner) Refresh(leaving:b); }
        public void HandleUnitGainFact(EntityFact f) => Refresh();
        public void HandleUnitLostFact(EntityFact f) => Refresh(leaving:f);
    }
    [TypeId("3fe05744dd5b4ac780d74a54e0481e44")]
    public sealed class KineticColdActivation01433 : UnitBuffComponentDelegate, IKineticistBurnValueHandler
    {
        public BlueprintBuffReference Aura;
        void StopAura() { foreach (var b in Owner.Buffs.Enumerable.Where(b => b.Blueprint == Aura.Get()).ToArray()) b.Remove(); }
        protected override void OnTurnOff() { if (Buff.IsSuppressed) StopAura(); }
        protected override void OnDeactivate() { if (!Owner.Facts.List.Contains(Fact)) StopAura(); }
        public void HandleKineticistBurnValueChanged(UnitPartKineticist part, int prior, AbilityData ability) { if (part.AcceptedBurn == 0) Buff.Remove(); }
    }
    [TypeId("682132f276324087bc219226a5c3cfa8")]
    public sealed class KineticColdOwner01433 : UnitFactComponentDelegate
    {
        public BlueprintBuffReference Activation, Aura;
        protected override void OnDeactivate() {
            if (Owner.Facts.List.Contains(Fact)) return;
            foreach (var b in Owner.Buffs.Enumerable.Where(b => b.Blueprint == Activation.Get() || b.Blueprint == Aura.Get()).ToArray()) b.Remove();
        }
    }
    [TypeId("c77bdbb2234d4b61b99c08b69255659a")]
    public sealed class KineticColdEnableRestriction01433 : BlueprintComponent, IAbilityCasterRestriction
    {
        public BlueprintBuffReference Activation;
        public bool IsCasterRestrictionPassed(UnitEntityData caster) => caster?.Get<UnitPartKineticist>() != null &&
            !caster.Buffs.Enumerable.Any(b => b.Blueprint == Activation.Get() && b.IsTurnedOn && !b.IsSuppressed);
        public string GetAbilityCasterRestrictionUIText() => "Cold Snap is already activated until your burn is removed.";
    }
    [TypeId("3c552e6289ab4def8ab8774d96f160ff")]
    public sealed class KineticColdToggleRestriction01433 : ActivatableAbilityRestriction
    {
        public BlueprintBuffReference Activation;
        protected override bool IsAvailable() => Owner.Buffs.Enumerable.Any(b => b.Blueprint == Activation.Get() && b.IsTurnedOn && !b.IsSuppressed);
    }
    [TypeId("6357c0baf6ca45b4bc595a911346d7c3")]
    public sealed class KineticSmokeEligible01433 : ContextCondition
    {
        public BlueprintUnitFactReference[] Excluded;
        protected override string GetConditionCaption() => "Living non-elemental creature not already sickened";
        protected override bool CheckCondition() => Target.Unit != null && !Target.Unit.State.IsDead &&
            !Target.Unit.State.HasCondition(UnitCondition.Sickened) && !Excluded.Any(r => Target.Unit.Descriptor.HasFact(r.Get()));
    }
}
