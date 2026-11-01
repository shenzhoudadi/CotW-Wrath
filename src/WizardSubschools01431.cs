using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class WizardSubschools01431
    {
        internal const string ModuleName = "Wizard schools: Undead and Prophecy";
        internal const string UndeadToken = "UndeadSchool";
        internal const string ProphecyToken = "ProphecySchool";
        internal const string ConsecrateAbilityId = "5bc013d5d0144502af6ae14141d6c35e";
        internal const string ConsecrateAreaId = "ba25ca0ee0e74199967db79cfae32318";
        internal const string ConsecrateBuffId = "ab7079a66bff427e82571f2457c04c14";
        const string ConsecrateComponentName = "CotWWrathUndeadBolster01431ConsecrateRemoval";
        internal static void Register(BlueprintsCache cache)
        {
            var f = new PsychicAssets(cache);
            var wizard = f.Native<BlueprintCharacterClass>(WizardSubschools01428.WizardId);
            var pool = f.Native<BlueprintFeatureSelection>(WizardSubschools01428.SchoolPoolId);
            var undead = Undead(f, wizard);
            var prophecy = Prophecy(f, wizard);
            var oppositions = new[] { f.Native<BlueprintFeature>("a9bb3dcb2e8d44a49ac36c393c114bd9"), f.Native<BlueprintFeature>("09595544116fe5349953f939aeba7611") };
            var previous = oppositions.Select(o => o.ComponentsArray).ToArray();
            try {
                var roots = new[] { undead, prophecy };
                for (int i = 0; i < roots.Length; i++) {
                    var root = cache.Load(roots[i].AssetGuid) as BlueprintFeature ?? roots[i];
                    if (oppositions[i].GetComponents<PrerequisiteNoFeature>().Any(c => c.Feature?.AssetGuid == root.AssetGuid)) continue;
                    var prerequisite = new PrerequisiteNoFeature { name = "Wizard01431Exclusive" + i };
                    Set(prerequisite, "m_Feature", Ref<BlueprintFeatureReference>(root));
                    oppositions[i].ComponentsArray = previous[i].Concat(new BlueprintComponent[] { prerequisite }).ToArray();
                }
                Migration01426.Commit(f, new[] { pool }, undead, prophecy);
            } catch { for (int i = 0; i < oppositions.Length; i++) oppositions[i].ComponentsArray = previous[i]; throw; }
            Optional(cache);
        }
        // Character Options Plus's exact public Consecrate identities. The
        // optional provider is validated before mutation; absence adds nothing.
        // The Bolster-local listener also handles entry, pre-existing areas and
        // late provider registration without a global event or Harmony patch.
        internal static void Optional(BlueprintsCache cache)
        {
            if (!(cache.Load(Id("UndeadBolsterBuff")) is BlueprintBuff bolster)) return;
            var raw = cache.Load(BlueprintGuid.Parse(ConsecrateBuffId));
            if (raw == null) return;
            var marker = raw as BlueprintBuff;
            var ability = cache.Load(BlueprintGuid.Parse(ConsecrateAbilityId)) as BlueprintAbility;
            var area = cache.Load(BlueprintGuid.Parse(ConsecrateAreaId)) as BlueprintAbilityAreaEffect;
            // BlueprintCore publishes empty stubs for disabled content and may
            // register the three assets sequentially. Await the late pass.
            if (marker != null && (marker.ComponentsArray == null || marker.ComponentsArray.Length == 0 || ability == null || area == null ||
                ability.ComponentsArray == null || ability.ComponentsArray.Length == 0 || area.ComponentsArray == null || area.ComponentsArray.Length == 0)) return;
            if (marker == null || ability == null || area == null || marker.name != "Consecrate.Buff" || area.name != "Consecrate.AoE" || ability.name != "Consecrate" ||
                ability.Type != AbilityType.Spell || ability.School != SpellSchool.Evocation ||
                !ability.GetComponents<AbilityEffectRunAction>().Any(c => c.Actions.Actions.OfType<ContextActionSpawnAreaEffect>().Any(a => a.AreaEffect?.AssetGuid == area.AssetGuid)) ||
                !marker.GetComponents<SavingThrowBonusAgainstDescriptor>().Any(c => c.Value == -3 && c.SpellDescriptor == SpellDescriptor.ChannelPositiveHarm) ||
                !area.GetComponents<AbilityAreaEffectBuff>().Any(c => c.Buff?.AssetGuid == marker.AssetGuid))
                throw new InvalidOperationException("COP Consecrate identity/layout changed; optional provider preserved.");
            var existing = marker.ComponentsArray.Where(c => c.name == ConsecrateComponentName).ToArray();
            if (existing.Length != 0) {
                if (existing.Length != 1 || !(existing[0] is AddFactContextActions fact) || fact.Activated.Actions.Length != 1 ||
                    !(fact.Activated.Actions[0] is ContextActionRemoveBuff removal) || removal.Buff?.AssetGuid != bolster.AssetGuid ||
                    removal.ToCaster || fact.NewRound.Actions.Length != 0 || fact.Deactivated.Actions.Length != 0 || fact.Dispose.Actions.Length != 0)
                    throw new InvalidOperationException("COP Consecrate integration component collision; provider preserved.");
                return;
            }
            var remove = new ContextActionRemoveBuff(); Set(remove, "m_Buff", Ref<BlueprintBuffReference>(bolster));
            var part = new AddFactContextActions { name = ConsecrateComponentName, Activated = Actions(remove), Deactivated = Actions(), NewRound = Actions(), Dispose = Actions() };
            var before = marker.ComponentsArray;
            try { marker.ComponentsArray = before.Concat(new BlueprintComponent[] { part }).ToArray(); }
            catch { marker.ComponentsArray = before; throw; }
        }
        static BlueprintProgression School(PsychicAssets f, BlueprintProgression source, string token, string title, string text, Dictionary<BlueprintGuid, BlueprintFeatureBase> replacements)
        {
            var p = f.Clone(source, token, title, text);
            p.LevelEntries = CopyData(source.LevelEntries);
            foreach (var entry in p.LevelEntries) {
                Set(entry, "m_Features", entry.Features.Where(x => !replacements.TryGetValue(x.AssetGuid, out var removed) || removed != null).Select(x => replacements.TryGetValue(x.AssetGuid, out var replacement) ? Ref<BlueprintFeatureBaseReference>(replacement) : Ref<BlueprintFeatureBaseReference>(x)).ToList());
                // The native getter caches a ReferenceListProxy tied to the
                // old list. Replacing the authored list must invalidate it.
                Set(entry, "m_FeaturesList", null);
            }
            p.LevelEntries = p.LevelEntries.Where(e => e.Features.Count != 0).ToArray();
            p.UIGroups = CopyData(source.UIGroups);
            foreach (var group in p.UIGroups) {
                Set(group, "m_Features", ((List<BlueprintFeatureBaseReference>)Get(group, "m_Features")).Where(r => !replacements.TryGetValue(r.Guid, out var removed) || removed != null).Select(r => replacements.TryGetValue(r.Guid, out var replacement) ? Ref<BlueprintFeatureBaseReference>(replacement) : r).ToList());
                Set(group, "m_FeaturesList", null);
            }
            Set(p, "m_UIDeterminatorsGroup", ((BlueprintFeatureBaseReference[])Get(source, "m_UIDeterminatorsGroup") ?? Array.Empty<BlueprintFeatureBaseReference>()).Where(r => !replacements.TryGetValue(r.Guid, out var removed) || removed != null).Select(r => replacements.TryGetValue(r.Guid, out var replacement) ? Ref<BlueprintFeatureBaseReference>(replacement) : CopyData(r)).ToArray());
            Set(p, "m_Classes", CopyData(Get(source, "m_Classes"))); Set(p, "m_Archetypes", CopyData(Get(source, "m_Archetypes")));
            p.IsClassFeature = true; return p;
        }
        static BlueprintAbilityResource Resource(PsychicAssets f, string token, string title, int baseAmount)
        {
            var r = f.Add(new BlueprintAbilityResource(), token, title, "Daily uses equal " + (baseAmount == 0 ? "your Intelligence modifier" : "3 + your Intelligence modifier") + " (minimum 0).");
            var amount = Get(r, "m_MaxAmount"); Set(amount, "BaseValue", baseAmount); Set(amount, "IncreasedByStat", true); Set(amount, "ResourceBonusStat", StatType.Intelligence);
            foreach (string field in new[] { "m_Class", "m_ClassDiv" }) Set(amount, field, Array.Empty<BlueprintCharacterClassReference>());
            foreach (string field in new[] { "m_Archetypes", "m_ArchetypesDiv" }) Set(amount, field, Array.Empty<BlueprintArchetypeReference>());
            Set(r, "m_MaxAmount", amount); Set(r, "m_Min", 0);
            r.LocalizedName = Text(token, "name", title); r.LocalizedDescription = Text(token, "description", "Daily uses equal " + (baseAmount == 0 ? "your Intelligence modifier" : "3 + your Intelligence modifier") + " (minimum 0)."); return r;
        }
        static AddAbilityResources Grant(BlueprintAbilityResource r) { var c = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true }; Set(c, "m_Resource", Ref<BlueprintAbilityResourceReference>(r)); return c; }
        static AbilityResourceLogic Cost(BlueprintAbilityResource r) { var c = new AbilityResourceLogic { Amount = 1 }; Set(c, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(r)); Set(c, "m_IsSpendResource", true); return c; }
        static BlueprintProgression Undead(PsychicAssets f, BlueprintCharacterClass wizard)
        {
            var source = f.Native<BlueprintProgression>("e9450978cc9feeb468fb8ee3a90607e3");
            var original = f.Native<BlueprintFeature>("927707dce06627d4f880c90b5575125f");
            var controlSource = f.Native<BlueprintAbility>(Id("ControlUndead").ToString());
            var oldTouch = BlueprintGuid.Parse("39af648796b7b9b4ab6321898ebb5fff"); var oldTurn = BlueprintGuid.Parse("71b8898b1d26d654b9a3eeac87e3e2f8");
            if (source.LevelEntries.Count(e => e.Level == 1 && e.Features.Count(x => x.AssetGuid == original.AssetGuid) == 1) != 1 ||
                original.GetComponents<AddSpecialSpellList>().Count() != 1 || original.GetComponents<AddFacts>().Count() != 1 ||
                original.GetComponent<AddFacts>().Facts.Count(x => x.AssetGuid == oldTouch) != 1 || original.GetComponent<AddFacts>().Facts.Count(x => x.AssetGuid == oldTurn) != 1 ||
                original.GetComponents<AddAbilityResources>().Count(x => x.Resource?.AssetGuid.ToString() == "d3c8231b4ab43d248944b6da83776522") != 1 ||
                original.GetComponents<AddAbilityResources>().Count(x => x.Resource?.AssetGuid.ToString() == "16394ff73175b5745957750040a2c99e") != 1)
                throw new InvalidOperationException("Necromancy base layout changed; original school preserved.");
            const string bolsterText = "As a spell-like standard action, touch an undead creature. For half your wizard level in rounds (minimum 1), it gains +1 profane to attack rolls and all saving throws, increasing by +1 every five wizard levels, 1 temporary hit point per Hit Die, and +2 turn resistance against the native turn/channel abilities. Consecrate immediately dispels Bolster. Daily uses: 3 + Intelligence modifier (minimum 0).";
            const string commandText = "As a supernatural standard action, control one nonsummoned undead within close range for 1 minute per wizard level, using the existing Control Undead adaptation and native domination AI. Will negates, DC 10 + half wizard level + Charisma modifier; spell resistance does not apply. Daily uses: 3 + Intelligence modifier (minimum 0), using the native necromancy channel pool. Control expires with its duration and restores the target's original allegiance.";
            var resource = Resource(f, "UndeadBolsterResource", "Bolster", 3);
            var rank = Inquisitions01426.Scaling(new[] { wizard }, AbilityRankType.Default, false); Set(rank, "m_Progression", ContextRankProgression.OnePlusDivStep); Set(rank, "m_StepLevel", 5);
            var stats = new[] { StatType.AdditionalAttackBonus, StatType.SaveFortitude, StatType.SaveReflex, StatType.SaveWill }.Select(s => (BlueprintComponent)new AddContextStatBonus { Stat = s, Descriptor = ModifierDescriptor.Profane, Value = new ContextValue { ValueType = ContextValueType.Rank } }).ToArray();
            var resistance = CopyData(f.Native<BlueprintFeature>("bfd0f754df75bd2449bb7d021dc5c7a1").GetComponent<SavingThrowBonusAgainstSpecificSpells>());
            if (resistance == null || resistance.Value != 2 || !resistance.Spells.Any()) throw new InvalidOperationException("Native channel resistance layout changed.");
            var bolsterBuff = f.Buff("UndeadBolsterBuff", "Bolster", bolsterText, stats.Concat(new BlueprintComponent[] { rank, resistance,
                new TemporaryHitPointsPerLevel { Descriptor = ModifierDescriptor.Profane, HitPointsPerLevel = 1, Value = 1, RemoveWhenHitPointsEnd = false }, new UndeadBolsterConsecrate01431() }).ToArray());
            Set(bolsterBuff, "m_Icon", Get(original, "m_Icon"));
            var duration = Inquisitions01426.Scaling(new[] { wizard }, AbilityRankType.Default, true); Set(duration, "m_UseMin", true); Set(duration, "m_Min", 1);
            var apply = Apply(bolsterBuff, 0); apply.DurationValue.BonusValue = new ContextValue { ValueType = ContextValueType.Rank };
            var target = new AbilityTargetHasFact(); Set(target, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("734a29b693e9ec346ba2951b27987e33")) });
            var bolster = f.Ability("UndeadBolster", "Bolster", bolsterText, AbilityType.SpellLike, UnitCommand.CommandType.Standard, AbilityRange.Touch, Run(apply), duration, Cost(resource), target);
            bolster.CanTargetEnemies = true; bolster.CanTargetFriends = true; bolster.CanTargetSelf = true; bolster.SpellResistance = false; Set(bolster, "m_Icon", Get(original, "m_Icon"));
            bolster.LocalizedDuration = Text("UndeadBolster", "duration", "1 round per two wizard levels (minimum 1)");
            var command = f.Clone(controlSource, "UndeadCommand", "Command Undead", commandText);
            command.Type = AbilityType.Supernatural; command.AvailableMetamagic = 0; command.SpellResistance = false;
            command.ComponentsArray = command.ComponentsArray.Where(c => !(c is SpellListComponent) && !(c is SpellComponent) && !(c is ContextRankConfig)).Concat(
                Inquisitions01426.Parameters(f, "UndeadCommand", new[] { wizard }, StatType.Charisma)).Concat(new BlueprintComponent[] { Cost(f.Native<BlueprintAbilityResource>("16394ff73175b5745957750040a2c99e")) }).ToArray();
            Set(resistance, "m_Spells", resistance.Spells.Concat(new[] { f.Native<BlueprintAbility>(oldTurn.ToString()), command }).GroupBy(a => a.AssetGuid).Select(g => Ref<BlueprintAbilityReference>(g.First())).ToArray());
            var first = f.Clone(original, "UndeadSchoolBase", "Undead", bolsterText + "\n" + commandText);
            foreach (var facts in first.GetComponents<AddFacts>()) Set(facts, "m_Facts", facts.Facts.Select(x => x.AssetGuid == oldTouch ? (BlueprintUnitFact)bolster : x.AssetGuid == oldTurn ? command : x).Select(Ref<BlueprintUnitFactReference>).ToArray());
            foreach (var stat in first.GetComponents<ReplaceAbilitiesStat>()) Set(stat, "m_Ability", stat.Ability.Select(a => a.AssetGuid == oldTouch ? bolster : a).Select(Ref<BlueprintAbilityReference>).ToArray());
            foreach (var casterLevel in first.GetComponents<ReplaceCasterLevelOfAbility>().Where(c => c.Spell?.AssetGuid == oldTouch)) Set(casterLevel, "m_Spell", Ref<BlueprintAbilityReference>(bolster));
            first.ComponentsArray = first.ComponentsArray.Where(c => !(c is AddAbilityResources grant && grant.Resource?.AssetGuid.ToString() == "d3c8231b4ab43d248944b6da83776522")).Concat(new BlueprintComponent[] { Grant(resource) }).ToArray();
            return School(f, source, UndeadToken, "Undead", "A necromancy school option. Bolster replaces Grave Touch; Command Undead uses the existing single-target Control Undead adaptation. Native specialist spells, two opposition choices and the complete level-8 Life Sight progression remain. Consecrate from Character Options Plus removes Bolster when present.", new Dictionary<BlueprintGuid, BlueprintFeatureBase> { [original.AssetGuid] = first });
        }
        static BlueprintProgression Prophecy(PsychicAssets f, BlueprintCharacterClass wizard)
        {
            var source = f.Native<BlueprintProgression>("d7d18ce5c24bd324d96173fdc3309646"); var original = f.Native<BlueprintFeature>("54d21b3221ea82a4d90d5a91b7872f3d");
            var greaterId = BlueprintGuid.Parse("ef01f802ebc01a94cbc25c89c365563b");
            var forewarnedId = BlueprintGuid.Parse("f8ee4f0f4cf741fa9b561ba4eef9ebef");
            var forewarnedCapstoneId = BlueprintGuid.Parse("a6839f2a709288e43b68d3281b3bbd8f");
            if (source.LevelEntries.Count(e => e.Level == 1 && e.Features.Count(x => x.AssetGuid == original.AssetGuid) == 1) != 1 || source.LevelEntries.Count(e => e.Level == 8 && e.Features.Count(x => x.AssetGuid == greaterId) == 1) != 1 || original.GetComponents<AddSpecialSpellList>().Count() != 1)
                throw new InvalidOperationException("Divination school layout changed; original school preserved.");
            if (original.GetComponents<AddFacts>().SelectMany(c => c.Facts).Count(fact => fact.AssetGuid == forewarnedId) != 1 || source.LevelEntries.SelectMany(e => e.Features).Count(fact => fact.AssetGuid == forewarnedCapstoneId) != 1)
                throw new InvalidOperationException("Divination Forewarned layout changed; original school preserved.");
            const string predictionText = "As a supernatural swift action, grant allies within 30 feet who can hear you +4 luck on their next attack roll, saving throw or skill check, lasting up to one minute. One roll consumes the bonus even when it fails. Daily uses: 3 + Intelligence modifier (minimum 0). This retains Call of the Wild's 30-foot area adaptation.";
            var resource = Resource(f, "ProphecyPredictionResource", "Inspiring Prediction", 3);
            var guidance = f.Native<BlueprintBuff>("ec931b882e806ce42906597e5585c13f"); var predictionBuff = f.Clone(guidance, "ProphecyPredictionBuff", "Inspiring Prediction", predictionText);
            foreach (var c in predictionBuff.ComponentsArray) {
                if (c is BuffAllSavesBonus saves) { saves.Value = 4; saves.Descriptor = ModifierDescriptor.Luck; }
                if (c is BuffAllSkillsBonus skills) { skills.Value = 4; skills.Descriptor = ModifierDescriptor.Luck; }
                if (c is AddStatBonus stat) { stat.Value = 4; stat.Descriptor = ModifierDescriptor.Luck; }
                ActionList actions = c is AddInitiatorAttackRollTrigger attack ? attack.Action : c is AddInitiatorSkillRollTrigger skill ? skill.Action : c is AddInitiatorPartySkillRollTrigger party ? party.Action : c is AddInitiatorSavingThrowTrigger save ? save.Action : null;
                if (actions != null) foreach (var action in actions.Actions.OfType<ContextActionRemoveBuff>()) { if (action.Buff?.AssetGuid != guidance.AssetGuid) throw new InvalidOperationException("Native Guidance cleanup changed."); Set(action, "m_Buff", Ref<BlueprintBuffReference>(predictionBuff)); }
            }
            // Unlike the individual-skill trigger, native party-skill checks
            // treat Unknown as an exact skill, not a wildcard. Preserve the
            // real handler and supply one private trigger per native skill.
            var partyTriggers = predictionBuff.GetComponents<AddInitiatorPartySkillRollTrigger>().ToArray();
            if (partyTriggers.Length != 1 || partyTriggers[0].Skill != StatType.Unknown) throw new InvalidOperationException("Native Guidance party-skill layout changed.");
            predictionBuff.ComponentsArray = predictionBuff.ComponentsArray.Where(c => !(c is AddInitiatorPartySkillRollTrigger)).Concat(
                StatTypeHelper.Skills.Select(skill => { var trigger = CopyData(partyTriggers[0]); trigger.Skill = skill; trigger.name = "ProphecyPredictionPartySkill" + skill; return (BlueprintComponent)trigger; })).ToArray();
            var around = new AbilityTargetsAround(); Set(around, "m_Radius", new Feet(30)); Set(around, "m_TargetType", TargetType.Ally); Set(around, "m_Condition", new ConditionsChecker { Conditions = new Condition[] { new AudibleAbilityTargetCondition() } });
            var prediction = f.Ability("ProphecyPrediction", "Inspiring Prediction", predictionText, AbilityType.Supernatural, UnitCommand.CommandType.Swift, AbilityRange.Personal, around, Run(Apply(predictionBuff, 10, fromSpell: false)), Cost(resource), new AudibleAbilityRestriction());
            Set(prediction, "m_Icon", Get(guidance, "m_Icon")); prediction.SpellResistance = false;
            var first = f.Clone(original, "ProphecySchoolBase", "Prophecy", predictionText + " Inspiring Prediction replaces Forewarned. Native Diviner's Fortune remains.");
            foreach (var facts in first.GetComponents<AddFacts>()) Set(facts, "m_Facts", facts.Facts.Where(fact => fact.AssetGuid != forewarnedId).Select(Ref<BlueprintUnitFactReference>).ToArray());
            first.ComponentsArray = first.ComponentsArray.Where(c => !(c is AddFacts facts && !facts.Facts.Any())).Concat(new BlueprintComponent[] { Facts(prediction), Grant(resource) }).ToArray();
            const string accordanceText = "At wizard level 8, declare your next spell guided by prophecy as a free action. The next non-item spell cast from a spellbook is empowered when Empower can affect it, and has a 20% chance to fail (15% at wizard level 12, 10% at 16). The spell slot and one daily use are spent on that cast whether it succeeds or fails. Canceling before casting spends no use. Daily uses: Intelligence modifier (minimum 0).";
            var accordanceResource = Resource(f, "ProphecyAccordanceResource", "In Accordance with the Prophecy", 0);
            var metamagic = new ProphecyAccordance01431 { Metamagic = Metamagic.Empower, Once = false, Abilities = new List<BlueprintAbilityReference>(), Resource = Ref<BlueprintAbilityResourceReference>(accordanceResource), Wizard = Ref<BlueprintCharacterClassReference>(wizard) };
            SetEnum(metamagic, "m_AllowedAbilities", "SpellOnly"); Set(metamagic, "m_IncludeClasses", Array.Empty<BlueprintCharacterClassReference>()); Set(metamagic, "m_ExcludeClasses", Array.Empty<BlueprintCharacterClassReference>());
            var accordanceBuff = f.Buff("ProphecyAccordanceBuff", "In Accordance with the Prophecy", accordanceText, metamagic); Set(accordanceBuff, "m_Icon", Get(guidance, "m_Icon"));
            var toggleCost = new ActivatableAbilityResourceLogic { SpendType = ActivatableAbilityResourceLogic.ResourceSpendType.Never }; Set(toggleCost, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(accordanceResource));
            var toggle = f.Add(new BlueprintActivatableAbility { ActivationType = AbilityActivationType.Immediately, DeactivateImmediately = true, DeactivateIfOwnerDisabled = true, DeactivateIfOwnerUnconscious = true, ComponentsArray = new BlueprintComponent[] { toggleCost } }, "ProphecyAccordance", "In Accordance with the Prophecy", accordanceText);
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(accordanceBuff)); Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free); Set(toggle, "m_Icon", Get(guidance, "m_Icon"));
            var greater = f.Feature("ProphecySchoolGreater", "In Accordance with the Prophecy", accordanceText, Facts(toggle), Grant(accordanceResource)); greater.IsClassFeature = true; Set(greater, "m_Icon", Get(guidance, "m_Icon"));
            return School(f, source, ProphecyToken, "Prophecy", "A divination school option. Inspiring Prediction replaces Forewarned, including its level-20 initiative capstone. Native Diviner's Fortune remains. In Accordance with the Prophecy replaces Foretell at level 8. The specialist spell list, both opposition-school choices and unrelated additional features remain.", new Dictionary<BlueprintGuid, BlueprintFeatureBase> { [original.AssetGuid] = first, [greaterId] = greater, [forewarnedCapstoneId] = null });
        }
    }
    [TypeId("6f8f64b226984752bf3c74db39ea583e")]
    public sealed class UndeadBolsterConsecrate01431 : UnitBuffComponentDelegate, IUnitBuffHandler
    {
        internal static bool Consecrated(UnitEntityData unit) => unit != null && unit.Buffs.Enumerable.Any(b => b.IsTurnedOn && !b.IsSuppressed && !b.IsDisposed && b.Blueprint.AssetGuid.ToString() == WizardSubschools01431.ConsecrateBuffId);
        void Check() { if (Consecrated(Owner)) Buff.Remove(); }
        protected override void OnTurnOn() => Check();
        protected override void OnApplyPostLoadFixes() => Check();
        public void HandleBuffDidAdded(Buff b) { if (b.Owner == Owner && b.Blueprint.AssetGuid.ToString() == WizardSubschools01431.ConsecrateBuffId) Check(); }
        public void HandleBuffDidRemoved(Buff b) { }
    }
    // Use native AutoMetamagic parameter handling behind the local eligibility
    // check. Its passive registry has no resource/book predicate, so do not
    // register there and let it bypass these checks. Once stays false so the
    // fact survives through the completed cast, including failed casts.
    [TypeId("53671bd8fbcb457d9609713528a54e4e")]
    public sealed class ProphecyAccordance01431 : AutoMetamagic, IInitiatorRulebookHandler<RuleCastSpell>, IInitiatorRulebookHandler<RuleCalculateAbilityParams>
    {
        public BlueprintAbilityResourceReference Resource;
        public BlueprintCharacterClassReference Wizard;
        static readonly ConditionalWeakTable<RuleCastSpell, HashSet<Buff>> Claimed = new ConditionalWeakTable<RuleCastSpell, HashSet<Buff>>();
        sealed class PaidMetamagic { internal Metamagic Mask; }
        // Runtime-only: these weak identity entries do not survive save reload.
        static readonly ConditionalWeakTable<AbilityExecutionContext, PaidMetamagic> PaidContexts = new ConditionalWeakTable<AbilityExecutionContext, PaidMetamagic>();
        // Called from the existing MechanicsContext.Recalculate postfix. Only
        // this module's completed, paid originating cast can enter this map.
        // Preserve its Empower bit without restoring another effect or DC.
        internal static void RestorePaidMetamagic(MechanicsContext context)
        {
            var source = context?.SourceAbilityContext;
            if (source == null || context.Params == null || !PaidContexts.TryGetValue(source, out var paid) || (context.Params.Metamagic & paid.Mask) == paid.Mask) return;
            var parameters = context.Params.Clone(); parameters.Metamagic |= paid.Mask; context.SetParams(parameters);
        }
        static void RememberPaidMetamagic(RuleCastSpell evt)
        {
            var context = evt.Context;
            if (context?.Params == null || !ReferenceEquals(context.Ability, evt.Spell) || !EmpowerEligible(evt.Spell) || !evt.ShouldSpendResource ||
                !(evt.Success || evt.IsSpellFailed || evt.IsArcaneSpellFailed) || (context.Params.Metamagic & Metamagic.Empower) == 0 || PaidContexts.TryGetValue(context, out _)) return;
            PaidContexts.Add(context, new PaidMetamagic { Mask = Metamagic.Empower });
        }
        protected override void OnTurnOn() { }
        protected override void OnTurnOff() => base.OnTurnOff();
        bool Ready => Fact.IsActive && Fact.IsTurnedOn && !Fact.IsDisposed && !(Fact is Buff b && b.IsSuppressed) && Resource?.Get() != null && Owner.Resources.GetResourceAmount(Resource.Get()) > 0;
        internal static bool RealSpell(AbilityData spell) => spell != null && spell.Blueprint.IsSpell && spell.Spellbook != null && spell.SourceItem == null && spell.StickyTouch == null;
        internal static bool EmpowerEligible(AbilityData spell) => RealSpell(spell) && (spell.Blueprint.AvailableMetamagic & Metamagic.Empower) != 0;
        internal int FailureChance() { int level = Owner.Progression.GetClassLevel(Wizard.Get()); return level >= 16 ? 10 : level >= 12 ? 15 : 20; }
        public new void OnEventAboutToTrigger(RuleCalculateAbilityParams evt) { if (Ready && EmpowerEligible(evt.AbilityData)) base.OnEventAboutToTrigger(evt); }
        public new void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
        public new void OnEventAboutToTrigger(RuleCastSpell evt)
        {
            if (!Ready || evt.Initiator != Owner || !RealSpell(evt.Spell) || evt.IsDuplicateSpellApplied || evt.Context?.IsDuplicateSpellApplied == true || !(Fact is Buff buff)) return;
            if (!Claimed.GetOrCreateValue(evt).Add(buff)) return;
            // CotW rolls the prophecy risk independently of other failure
            // effects; the native RuleCastSpell then owns actual failure.
            if ((int)RulebookEvent.Dice.D100 <= FailureChance()) evt.SpellFailureChance = 200;
        }
        public new void OnEventDidTrigger(RuleCastSpell evt)
        {
            if (!(Fact is Buff buff) || !Claimed.TryGetValue(evt, out var claims) || !claims.Remove(buff)) return;
            var owner = Owner; var resource = Resource.Get();
            if (owner.Resources.GetResourceAmount(resource) > 0) { owner.Resources.Spend(resource, 1); RememberPaidMetamagic(evt); }
            buff.Remove();
        }
    }
}
