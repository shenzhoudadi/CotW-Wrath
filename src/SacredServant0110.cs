// Sacred Servant adapted from Holic75/KingmakerRebalance (MIT).
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using HarmonyLib;
using Newtonsoft.Json;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class SacredServant0110
    {
        internal const string ModuleName = "Paladin archetype: Sacred Servant";
        internal const string ReconcileModuleName = "Sacred Servant domain catalogue";
        internal const string RootToken = "SacredServantArchetype0110";
        internal const string PaladinId = "bfa11238e7ae3544bbeb4d0b92e897ec";
        internal const string ClericId = "67819271767a9dd4fbfd4ae700befea0";
        internal const string DomainsId = "48525e5da45c9c243a343fc6545dbdb9";
        internal const string SecondDomainsId = "43281c3d7fe18cc4d91928395837cd1e";
        internal const string SeparatistDomainsId = "42b781e4375d499383b2602d90661283";
        internal const string DivineHunterDomainsId = "72909a37a1ed5344f88ec9d1d31f5c5b";
        internal const string ImpossibleDomainId = "213a8480d22206b45acbfa0619ca5aaf";
        internal const string DeityId = "a7c8b73528d34c2479b4bd638503da1d";
        internal const string LayOnHandsResourceId = "9dedf41d995ff4446a181f143c3db98c";
        internal const ActivatableAbilityGroup BondGroup = ActivatableAbilityGroup.DivineWeaponProperty;

        static readonly string[] DomainSelectionIds =
        {
            DomainsId,
            SecondDomainsId,
            SeparatistDomainsId,
            DivineHunterDomainsId,
            ImpossibleDomainId
        };

        const string ArchetypeText = "Paladins as a general rule, venerate the gods of good and purity, but some take this a step further, dedicating themselves to a specific deity and furthering the cause of the faith. These sacred servants are rewarded for their devotion with additional spells and powerful allies.";
        const string DomainText = "Sacred Servant chooses one domain associated with her deity. At 4th level she also gains one domain spell slot for each level of paladin spells she can cast. Every day she must prepare the domain spell from her chosen domain in that spell slot.";
        const string BondText = "At 5th level, instead of forming a divine bond with her weapon or a mount, a sacred servant forms a bond with a celestial spirit. At 5th level, the spirit grants one bonus. For every three levels beyond 5th, the spirit grants one additional bonus. Each bonus can grant +1 caster level to paladin spells, +1 DC to channel positive energy when harming undead, +1d6 channel positive energy, or restore one use of lay on hands. The enhancements are fixed when the spirit is called. Lay on hands uses restored this way cannot be restored repeatedly by calling the spirit again that day. The sacred servant can use this ability once per day at 5th level and one additional time per day at 9th, 13th, and 17th level.";
        const string AllyText = "At 8th level, a sacred servant can call upon her deity for aid in the form of a powerful servant. She can summon a bralani azata once per day as a spell-like ability for 1 minute per sacred servant level. At 12th level, she summons a movanic deva instead. At 16th level, she summons a ghaele azata.";

        sealed class FieldSnapshot
        {
            internal object Owner;
            internal string Field;
            internal object Original;
            internal BlueprintGuid Expected;
        }

        sealed class AdaptationSnapshot
        {
            internal readonly List<FieldSnapshot> Fields = new List<FieldSnapshot>();
            internal readonly List<Tuple<BlueprintScriptableObject, BlueprintComponent[]>> ComponentArrays = new List<Tuple<BlueprintScriptableObject, BlueprintComponent[]>>();
            internal readonly List<Tuple<BlueprintProgression, BlueprintProgression.ClassWithLevel[], BlueprintProgression.ArchetypeWithLevel[]>> Progressions = new List<Tuple<BlueprintProgression, BlueprintProgression.ClassWithLevel[], BlueprintProgression.ArchetypeWithLevel[]>>();
            internal readonly List<Tuple<BlueprintProgression, BlueprintSpellList>> SpellLists = new List<Tuple<BlueprintProgression, BlueprintSpellList>>();
        }

        internal static void Register(BlueprintsCache cache)
        {
            if (MagusBatch01454.Existing(cache, ModuleName)) return;
            var f = new PsychicAssets(cache);
            var paladin = f.Native<BlueprintCharacterClass>(PaladinId);
            var cleric = f.Native<BlueprintCharacterClass>(ClericId);
            var deity = f.Native<BlueprintFeatureSelection>(DeityId);
            var source = f.Native<BlueprintFeatureSelection>(DomainsId);
            var progressions = DiscoverProgressions(f).ToArray();
            if (progressions.Length == 0)
                throw new InvalidOperationException("0110 Sacred Servant could not resolve any Cleric domain progressions.");

            // Use the native Cleric domain selection directly. This keeps the
            // catalogue open to late providers and lets native mythic prerequisites
            // recognise the Sacred Servant's domain fact without a private clone.
            var bond = BuildDivineBond(f, paladin);
            var ally = BuildCallCelestialAlly(f, paladin);
            var root = f.Add(new BlueprintArchetype
            {
                LocalizedName = Text(RootToken, "name", "Sacred Servant"),
                LocalizedDescription = Text(RootToken, "description", ArchetypeText),
                ComponentsArray = Array.Empty<BlueprintComponent>(),
                AddFeatures = new[]
                {
                    Entry(1, source), Entry(5, bond[0]), Entry(8, bond[1], ally),
                    Entry(11, bond[2]), Entry(14, bond[3]), Entry(17, bond[4]), Entry(20, bond[5])
                },
                RemoveFeatures = new[]
                {
                    Entry(4, f.Native<BlueprintFeature>("0f5c99ffb9c084545bbbe960b825d137")),
                    Entry(5, f.Native<BlueprintFeature>("ad7dc4eba7bf92f4aba23f716d7a9ba6")),
                    Entry(8, f.Native<BlueprintFeature>("a28693b24cc412c478b8b85877f2dad2")),
                    Entry(10, f.Native<BlueprintFeature>("0f5c99ffb9c084545bbbe960b825d137")),
                    Entry(16, f.Native<BlueprintFeature>("0f5c99ffb9c084545bbbe960b825d137"))
                }
            }, RootToken, "Sacred Servant", ArchetypeText);
            Set(root, "m_ParentClass", paladin);
            foreach (var feature in f.Assets.Values.OfType<BlueprintFeature>()) feature.IsClassFeature = true;

            var adaptation = AdaptProgressions(progressions, paladin, cleric, root);
            var undoClass = MagusBatch01454.CaptureClass(paladin);
            var group = MagusBatch01454.Group(bond.Concat(new[] { ally }).ToArray());
            Action undo = () => { Rollback(adaptation); undoClass(); };
            void Connect()
            {
                MagusBatch01454.ConnectArchetype(paladin, root, group);
                InsertDomainAfterDeity(paladin, deity, source);
            }
            void Verify()
            {
                MagusBatch01454.VerifyArchetype(paladin, root, group);
                VerifyDomainAfterDeity(paladin, deity, source);
                VerifyAdaptation(adaptation, paladin, root);
            }
            MagusBatch01454.Publish(ModuleName, f, Connect, undo, Verify);
        }

        // Re-run from RegisterOptional so domains published by late providers are
        // adapted without rebuilding the archetype or changing its publication shape.
        internal static bool Reconcile(BlueprintsCache cache)
        {
            if (!(cache.Load(Id(RootToken)) is BlueprintArchetype root)) return false;
            var f = new PsychicAssets(cache);
            var paladin = f.Native<BlueprintCharacterClass>(PaladinId);
            var cleric = f.Native<BlueprintCharacterClass>(ClericId);
            var deity = f.Native<BlueprintFeatureSelection>(DeityId);
            var source = f.Native<BlueprintFeatureSelection>(DomainsId);
            var progressions = DiscoverProgressions(f).ToArray();
            if (progressions.Length == 0) return false;
            var pending = progressions.Where(domain => !IsAdapted(domain, paladin, root)).ToArray();
            if (pending.Length == 0)
            {
                InsertDomainAfterDeity(paladin, deity, source);
                VerifyDomainAfterDeity(paladin, deity, source);
                return true;
            }
            var adaptation = AdaptProgressions(pending, paladin, cleric, root);
            try
            {
                InsertDomainAfterDeity(paladin, deity, source);
                VerifyDomainAfterDeity(paladin, deity, source);
                VerifyAdaptation(adaptation, paladin, root);
                return true;
            }
            catch
            {
                Rollback(adaptation);
                throw;
            }
        }

        static IEnumerable<BlueprintProgression> DiscoverProgressions(PsychicAssets f)
        {
            var seen = new HashSet<BlueprintGuid>();
            foreach (var id in DomainSelectionIds)
            {
                if (!(f.Cache.Load(BlueprintGuid.Parse(id)) is BlueprintFeatureSelection selection)) continue;
                foreach (var feature in selection.AllFeatures.ToArray())
                    if (feature is BlueprintProgression progression && seen.Add(progression.AssetGuid))
                        yield return progression;
            }
        }

        static bool IsAdapted(BlueprintProgression domain, BlueprintCharacterClass paladin, BlueprintArchetype root)
        {
            var classes = (BlueprintProgression.ClassWithLevel[])Get(domain, "m_Classes") ?? Array.Empty<BlueprintProgression.ClassWithLevel>();
            var archetypes = (BlueprintProgression.ArchetypeWithLevel[])Get(domain, "m_Archetypes") ?? Array.Empty<BlueprintProgression.ArchetypeWithLevel>();
            return classes.Any(c => c.m_Class?.Guid == paladin.AssetGuid && c.AdditionalLevel == 0) &&
                archetypes.Any(a => a.m_Archetype?.Guid == root.AssetGuid && a.AdditionalLevel == -3);
        }

        static void InsertDomainAfterDeity(BlueprintCharacterClass paladin, BlueprintFeatureSelection deity, BlueprintFeatureSelection domain)
        {
            var progression = paladin.Progression;
            var headers = (BlueprintFeatureBaseReference[])Get(progression, "m_UIDeterminatorsGroup") ?? Array.Empty<BlueprintFeatureBaseReference>();
            if (headers.Count(reference => reference?.Guid == deity.AssetGuid) != 1)
                throw new InvalidOperationException("0110 expected exactly one native Paladin deity determinator.");
            headers = headers.Where(reference => reference?.Guid != domain.AssetGuid).ToArray();
            int deityIndex = Array.FindIndex(headers, reference => reference?.Guid == deity.AssetGuid);
            Set(progression, "m_UIDeterminatorsGroup", headers.Take(deityIndex + 1)
                .Concat(new[] { Ref<BlueprintFeatureBaseReference>(domain) })
                .Concat(headers.Skip(deityIndex + 1)).ToArray());
        }

        static void VerifyDomainAfterDeity(BlueprintCharacterClass paladin, BlueprintFeatureSelection deity, BlueprintFeatureSelection domain)
        {
            var headers = (BlueprintFeatureBaseReference[])Get(paladin.Progression, "m_UIDeterminatorsGroup") ?? Array.Empty<BlueprintFeatureBaseReference>();
            int deityIndex = Array.FindIndex(headers, reference => reference?.Guid == deity.AssetGuid);
            int domainIndex = Array.FindIndex(headers, reference => reference?.Guid == domain.AssetGuid);
            if (deityIndex < 0 || domainIndex <= deityIndex || headers.Count(reference => reference?.Guid == domain.AssetGuid) != 1)
                throw new InvalidOperationException("0110 Sacred Servant domain determinator is not after the native Paladin deity selection.");
        }

        static BlueprintFeature[] BuildDivineBond(PsychicAssets f, BlueprintCharacterClass paladin)
        {
            var resource = f.Add(new BlueprintAbilityResource(), "SacredServantDivineBondResource0110", "Divine Bond", BondText);
            var amount = Get(resource, "m_MaxAmount");
            Set(amount, "BaseValue", 1); Set(amount, "IncreasedByLevelStartPlusDivStep", true);
            Set(amount, "StartingLevel", 9); Set(amount, "StartingIncrease", 1); Set(amount, "LevelStep", 4); Set(amount, "PerStepIncrease", 1);
            Set(amount, "m_ClassDiv", new[] { Ref<BlueprintCharacterClassReference>(paladin) });
            Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>()); Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>()); Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(resource, "m_MaxAmount", amount);

            var effect = f.Buff("SacredServantDivineBondEffect0110", "Divine Bond", BondText, new SacredServantBondEffect0110 { name = "SacredServantBondRules0110", Paladin = Ref<BlueprintCharacterClassReference>(paladin) });
            Set(effect, "m_Icon", f.Native<BlueprintAbility>("1bc83efec9f8c4b42a46162d72cbf494").Icon);
            var rank = Rank(paladin, "AsIs"); rank.name = "SacredServantBondDuration0110";
            var activate = new SacredServantActivateBond0110 { name = "SacredServantLockBond0110", Effect = Ref<BlueprintBuffReference>(effect), LayOnHands = Ref<BlueprintAbilityResourceReference>(f.Native<BlueprintAbilityResource>(LayOnHandsResourceId)) };
            var cost = Cost(resource); cost.name = "SacredServantBondCost0110";
            var ability = f.Clone(f.Native<BlueprintAbility>("7ff088ab58c69854b82ea95c2b0e35b4"), "SacredServantDivineBondAbility0110", "Divine Bond", BondText);
            ability.Type = AbilityType.Supernatural; ability.ActionType = UnitCommand.CommandType.Standard; ability.Range = AbilityRange.Personal;
            ability.CanTargetSelf = true; ability.CanTargetFriends = false; ability.CanTargetEnemies = false;
            ability.ComponentsArray = ability.ComponentsArray.Where(c => !(c is AbilityResourceLogic) && !(c is AbilityEffectRunAction) && !(c is ContextRankConfig)).Concat(new BlueprintComponent[] { cost, rank, Run(activate) }).ToArray();
            Set(ability, "m_Icon", effect.Icon);

            var options = new List<BlueprintActivatableAbility>();
            string[] labels = { "Caster Level Bonus", "Channel Energy DC Bonus", "Channel Energy Dice Bonus" };
            string[] icons = { "1bc83efec9f8c4b42a46162d72cbf494", "a9a52760290591844a96d0109e30e04d", "0d657aa811b310e4bbd8586e60156a2d" };
            for (int kind = 0; kind < 3; kind++)
            for (int value = 1; value <= 6; value++)
            {
                string token = "SacredServantBondChoice" + kind + "x" + value + "0110";
                var marker = f.Buff(token + "Buff", labels[kind], BondText, new SacredServantBondChoice0110 { name = token + "Marker", Kind = kind, Value = value });
                SetEnum(marker, "m_Flags", "HiddenInUi"); Set(marker, "m_Icon", f.Native<BlueprintAbility>(icons[kind]).Icon);
                var toggle = f.Add(new BlueprintActivatableAbility { ActivationType = AbilityActivationType.Immediately, DeactivateImmediately = true, Group = BondGroup, WeightInGroup = value, DoNotTurnOffOnRest = true }, token + "Toggle", "Divine Bond: " + labels[kind] + " +" + value, BondText);
                Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(marker)); Set(toggle, "m_Icon", marker.Icon); Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);
                options.Add(toggle);
            }
            foreach (var toggle in options)
            {
                toggle.ComponentsArray = options.Where(x => x != toggle && x.Buff.GetComponent<SacredServantBondChoice0110>()?.Kind == toggle.Buff.GetComponent<SacredServantBondChoice0110>()?.Kind).Select(x => { var r = new RestrictionHasFact { name = "SacredServantExclusive" + x.AssetGuid, Not = true }; Set(r, "m_Feature", Ref<BlueprintUnitFactReference>(x.Buff)); return (BlueprintComponent)r; }).Concat(new BlueprintComponent[] { CreateRestriction(effect, "SacredServantBondInactive" + toggle.AssetGuid) }).ToArray();
                toggle.HiddenInUI = true;
                toggle.ActionBarAutoFillIgnored = true;
            }
            var variants = new ActivatableAbilityVariants { name = "SacredServantDivineBondVariants0110" };
            Set(variants, "m_Variants", options.Select(Ref<BlueprintActivatableAbilityReference>).ToArray());
            var selector = f.Add(new BlueprintActivatableAbility
            {
                ActivationType = AbilityActivationType.Immediately,
                Group = ActivatableAbilityGroup.None,
                ComponentsArray = new BlueprintComponent[] { variants, new ActivationDisable { name = "SacredServantDivineBondMenuOnly0110" } }
            }, "SacredServantDivineBondMenu0110", "Divine Bond: Choose Bonuses", BondText);
            Set(selector, "m_Icon", effect.Icon);

            var tiers = new BlueprintFeature[6];
            for (int i = 0; i < tiers.Length; i++)
            {
                var components = new List<BlueprintComponent>();
                if (i == 0)
                {
                    var grant = Grant(resource); grant.name = "SacredServantGrantBondResource0110"; components.Add(grant);
                    components.Add(Facts(ability));
                    components.Add(Facts(new[] { selector }.Concat(options).Cast<BlueprintUnitFact>().ToArray()));
                    components.Add(new SacredServantLayLedgerComponent0110 { name = "SacredServantLayLedger0110" });
                }
                else components.Add(new IncreaseActivatableAbilityGroupSize { name = "SacredServantBondPoint" + (i + 1) + "0110", Group = BondGroup });
                tiers[i] = f.Feature("SacredServantDivineBondTier" + (i + 1) + "0110", i == 0 ? "Divine Bond" : "Divine Bond (+" + (i + 1) + ")", BondText, components.ToArray());
                Set(tiers[i], "m_Icon", effect.Icon);
            }
            return tiers;
        }

        static RestrictionHasFact CreateRestriction(BlueprintUnitFact fact, string name)
        {
            var restriction = new RestrictionHasFact { name = name, Not = true };
            Set(restriction, "m_Feature", Ref<BlueprintUnitFactReference>(fact));
            return restriction;
        }

        static BlueprintFeature BuildCallCelestialAlly(PsychicAssets f, BlueprintCharacterClass paladin)
        {
            var resource = f.Add(new BlueprintAbilityResource(), "SacredServantCelestialAllyResource0110", "Call Celestial Ally", AllyText);
            var amount = Get(resource, "m_MaxAmount"); Set(amount, "BaseValue", 1); Set(resource, "m_MaxAmount", amount);
            var donors = new[] { "0964bf88b582bed41b74e79596c4f6d9", "eb6df7ddfc0669d4fb3fc9af4bd34bca", "e96593e67d206ab49ad1b567327d1e75" }.Select(f.Native<BlueprintAbility>).ToArray();
            var actions = donors.Select(d =>
            {
                var run = d.GetComponent<AbilityEffectRunAction>() ?? throw new InvalidOperationException("0110 celestial ally donor lacks actions: " + d.AssetGuid);
                var copy = CopyData(run.Actions);
                var spawns = PsychicRemainingDisciplines01458.Graph(copy.Actions).OfType<ContextActionSpawnMonster>().ToArray();
                if (spawns.Length == 0) throw new InvalidOperationException("0110 celestial ally donor lacks summon action: " + d.AssetGuid);
                foreach (var spawn in spawns) { spawn.name = "SacredServantCelestialSpawn" + d.AssetGuid; spawn.DurationValue.Rate = DurationRate.Minutes; spawn.DurationValue.BonusValue = new ContextValue { ValueType = ContextValueType.Rank }; }
                return copy;
            }).ToArray();
            var choose = new SacredServantCelestialAlly0110 { name = "SacredServantCelestialChoice0110", Paladin = Ref<BlueprintCharacterClassReference>(paladin), Actions = actions };
            var rank = Rank(paladin, "AsIs"); rank.name = "SacredServantCelestialDuration0110";
            var cost = Cost(resource); cost.name = "SacredServantCelestialCost0110";
            var ability = f.Clone(donors[0], "SacredServantCallCelestialAllyAbility0110", "Call Celestial Ally", AllyText);
            ability.Type = AbilityType.SpellLike; ability.ActionType = UnitCommand.CommandType.Standard; Set(ability, "m_IsFullRoundAction", true);
            ability.ComponentsArray = ability.ComponentsArray.Where(c => !(c is AbilityResourceLogic) && !(c is AbilityEffectRunAction) && !(c is ContextRankConfig)).Concat(new BlueprintComponent[] { cost, rank, Run(choose) }).ToArray();
            var feature = f.Feature("SacredServantCallCelestialAllyFeature0110", "Call Celestial Ally", AllyText, Facts(ability), Grant(resource)); Set(feature, "m_Icon", ability.Icon); return feature;
        }

        static AdaptationSnapshot AdaptProgressions(IEnumerable<BlueprintProgression> progressions, BlueprintCharacterClass paladin, BlueprintCharacterClass cleric, BlueprintArchetype root)
        {
            var snapshot = new AdaptationSnapshot();
            try
            {
                foreach (var domain in progressions)
                {
                    var classes = (BlueprintProgression.ClassWithLevel[])Get(domain, "m_Classes") ?? Array.Empty<BlueprintProgression.ClassWithLevel>();
                    var archetypes = (BlueprintProgression.ArchetypeWithLevel[])Get(domain, "m_Archetypes") ?? Array.Empty<BlueprintProgression.ArchetypeWithLevel>();
                    snapshot.Progressions.Add(Tuple.Create(domain, classes, archetypes));
                    if (!classes.Any(c => c.m_Class?.Guid == paladin.AssetGuid))
                        Set(domain, "m_Classes", classes.Concat(new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(paladin), AdditionalLevel = 0 } }).ToArray());
                    if (!archetypes.Any(a => a.m_Archetype?.Guid == root.AssetGuid))
                        Set(domain, "m_Archetypes", archetypes.Concat(new[] { new BlueprintProgression.ArchetypeWithLevel { m_Archetype = Ref<BlueprintArchetypeReference>(root), AdditionalLevel = -3 } }).ToArray());

                    var facts = DomainFacts(domain).ToArray();
                    var components = DomainComponents(domain).ToArray();

                    // Native Divine Hunter semantics: -3 only controls which
                    // progression level entries are granted. Ability rank, daily
                    // uses, DC and duration still read the full Paladin level.
                    foreach (var rank in components.OfType<ContextRankConfig>())
                    {
                        AppendRef<BlueprintCharacterClassReference>(rank, "m_Class", paladin, snapshot);
                        AppendRef<BlueprintArchetypeReference>(rank, "m_AdditionalArchetypes", root, snapshot);
                    }
                    foreach (var onLevel in components.OfType<AddFeatureOnClassLevel>())
                    {
                        AppendRef<BlueprintCharacterClassReference>(onLevel, "m_AdditionalClasses", paladin, snapshot);
                        AppendRef<BlueprintArchetypeReference>(onLevel, "m_Archetypes", root, snapshot);
                    }
                    foreach (var resource in DomainResources(components).Distinct())
                    {
                        var amount = Get(resource, "m_MaxAmount");
                        if (amount == null) continue;
                        if (Get(amount, "IncreasedByLevel") is bool byLevel && byLevel)
                        {
                            AppendRef<BlueprintCharacterClassReference>(amount, "m_Class", paladin, snapshot);
                            AppendRef<BlueprintArchetypeReference>(amount, "m_Archetypes", root, snapshot);
                        }
                        if (Get(amount, "IncreasedByLevelStartPlusDivStep") is bool byStep && byStep)
                            AppendRef<BlueprintCharacterClassReference>(amount, "m_ClassDiv", paladin, snapshot);
                    }

                    var lists = DomainSpellLists(facts, cleric).ToArray();
                    if (lists.Length == 0) continue;
                    var original = domain.ComponentsArray ?? Array.Empty<BlueprintComponent>();
                    snapshot.ComponentArrays.Add(Tuple.Create((BlueprintScriptableObject)domain, original));
                    foreach (var list in lists) snapshot.SpellLists.Add(Tuple.Create(domain, list));
                    var attached = new HashSet<BlueprintGuid>(original.OfType<AddSpecialSpellList>()
                        .Where(c => c.ForArchetypeOnly && c.CharacterClass == paladin && c.Archetype == root && c.SpellList != null)
                        .Select(c => c.SpellList.AssetGuid));
                    var additions = lists.Where(list => !attached.Contains(list.AssetGuid)).Select(list =>
                    {
                        var copy = new AddSpecialSpellList
                        {
                            name = "SacredServantDomainSpellList0110_" + list.AssetGuid,
                            ForArchetypeOnly = true
                        };
                        Set(copy, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(paladin));
                        Set(copy, "m_SpellList", Ref<BlueprintSpellListReference>(list));
                        Set(copy, "m_Archetype", Ref<BlueprintArchetypeReference>(root));
                        return (BlueprintComponent)copy;
                    }).ToArray();
                    if (additions.Length > 0) domain.ComponentsArray = original.Concat(additions).ToArray();
                }
                return snapshot;
            }
            catch
            {
                Rollback(snapshot);
                throw;
            }
        }

        static void AppendRef<T>(object owner, string field, SimpleBlueprint value, AdaptationSnapshot snapshot) where T : BlueprintReferenceBase, new()
        {
            var array = (T[])Get(owner, field) ?? Array.Empty<T>();
            if (array.Any(reference => reference != null && reference.Guid == value.AssetGuid)) return;
            snapshot.Fields.Add(new FieldSnapshot { Owner = owner, Field = field, Original = array, Expected = value.AssetGuid });
            Set(owner, field, array.Concat(new[] { Ref<T>(value) }).ToArray());
        }

        static void VerifyAdaptation(AdaptationSnapshot snapshot, BlueprintCharacterClass paladin, BlueprintArchetype root)
        {
            foreach (var tuple in snapshot.Progressions)
            {
                var domain = tuple.Item1;
                var classes = (BlueprintProgression.ClassWithLevel[])Get(domain, "m_Classes") ?? Array.Empty<BlueprintProgression.ClassWithLevel>();
                var archetypes = (BlueprintProgression.ArchetypeWithLevel[])Get(domain, "m_Archetypes") ?? Array.Empty<BlueprintProgression.ArchetypeWithLevel>();
                if (!classes.Any(c => c.m_Class?.Guid == paladin.AssetGuid && c.AdditionalLevel == 0) ||
                    !archetypes.Any(a => a.m_Archetype?.Guid == root.AssetGuid && a.AdditionalLevel == -3))
                    throw new InvalidOperationException("0110 Sacred Servant domain progression attachment incomplete: " + domain.name);
            }
            foreach (var tuple in snapshot.SpellLists)
            {
                var domain = tuple.Item1;
                var list = tuple.Item2;
                if (!(domain.ComponentsArray ?? Array.Empty<BlueprintComponent>()).OfType<AddSpecialSpellList>()
                    .Any(c => c.ForArchetypeOnly && c.CharacterClass == paladin && c.Archetype == root && c.SpellList != null && c.SpellList.AssetGuid == list.AssetGuid))
                    throw new InvalidOperationException("0110 Sacred Servant domain spell list attachment incomplete: " + domain.name + " / " + list.name);
            }
            foreach (var field in snapshot.Fields)
            {
                var array = Get(field.Owner, field.Field) as IEnumerable<BlueprintReferenceBase>;
                if (array == null || !array.Any(reference => reference != null && reference.Guid == field.Expected))
                    throw new InvalidOperationException("0110 Sacred Servant domain adaptation incomplete: " + field.Field);
            }
        }

        static void Rollback(AdaptationSnapshot snapshot)
        {
            if (snapshot == null) return;
            for (int i = snapshot.Fields.Count - 1; i >= 0; i--)
            {
                var field = snapshot.Fields[i];
                Set(field.Owner, field.Field, field.Original);
            }
            for (int i = snapshot.ComponentArrays.Count - 1; i >= 0; i--)
                snapshot.ComponentArrays[i].Item1.ComponentsArray = snapshot.ComponentArrays[i].Item2;
            for (int i = snapshot.Progressions.Count - 1; i >= 0; i--)
            {
                var tuple = snapshot.Progressions[i];
                Set(tuple.Item1, "m_Classes", tuple.Item2);
                Set(tuple.Item1, "m_Archetypes", tuple.Item3);
            }
        }

        static IEnumerable<BlueprintAbilityResource> DomainResources(IEnumerable<BlueprintComponent> components)
        {
            foreach (var component in components)
            {
                BlueprintAbilityResource resource = null;
                if (component is AddAbilityResources grant)
                    resource = (Get(grant, "m_Resource") as BlueprintAbilityResourceReference)?.Get();
                else if (component is AbilityResourceLogic cost)
                    resource = (Get(cost, "m_RequiredResource") as BlueprintAbilityResourceReference)?.Get();
                else if (component is ActivatableAbilityResourceLogic toggle)
                    resource = (Get(toggle, "m_RequiredResource") as BlueprintAbilityResourceReference)?.Get();
                if (resource != null) yield return resource;
            }
        }

        static IEnumerable<BlueprintComponent> DomainComponents(BlueprintProgression domain)
        {
            var seen = new HashSet<BlueprintComponent>();
            foreach (var fact in DomainFacts(domain))
            foreach (var component in fact.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                if (component != null && seen.Add(component)) yield return component;
        }

        static IEnumerable<BlueprintSpellList> DomainSpellLists(IEnumerable<BlueprintScriptableObject> facts, BlueprintCharacterClass cleric)
        {
            var seen = new HashSet<BlueprintGuid>();
            foreach (var fact in facts)
            foreach (var component in fact.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                BlueprintSpellList list = null;
                if (component is AddSpecialSpellList special && special.CharacterClass == cleric) list = special.SpellList;
                else if (component is AddSpecialSpellListForArchetype specialArchetype && specialArchetype.CharacterClass == cleric) list = specialArchetype.SpellList;
                else if (component is LearnSpellList learned && learned.CharacterClass == cleric) list = learned.SpellList;
                if (list != null && seen.Add(list.AssetGuid)) yield return list;
            }
        }

        internal static bool SuppressClericDomainSpellbook(AddSpecialSpellList component)
        {
            if (component?.CharacterClass?.AssetGuid != BlueprintGuid.Parse(ClericId)) return false;
            var root = ResourcesLibrary.TryGetBlueprint<BlueprintArchetype>(Id(RootToken));
            var ownerProperty = AccessTools.Property(typeof(AddSpecialSpellList), "Owner");
            var owner = ownerProperty?.GetValue(component, null) as UnitEntityData;
            return root != null && owner != null && owner.Progression.IsArchetype(root);
        }

        static IEnumerable<BlueprintScriptableObject> DomainFacts(BlueprintProgression domain)
        {
            var seen = new HashSet<BlueprintGuid>();
            var queue = new Queue<BlueprintScriptableObject>();
            queue.Enqueue(domain);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == null || !seen.Add(current.AssetGuid)) continue;
                yield return current;
                if (current is BlueprintProgression progression)
                    foreach (var feature in progression.LevelEntries.SelectMany(e => e.Features).OfType<BlueprintScriptableObject>()) queue.Enqueue(feature);
                if (current is BlueprintFeatureSelection selection)
                    foreach (var feature in selection.AllFeatures.OfType<BlueprintScriptableObject>()) queue.Enqueue(feature);
                foreach (var component in current.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                {
                    if (component is AddFacts addFacts)
                        foreach (var fact in addFacts.Facts.OfType<BlueprintScriptableObject>()) queue.Enqueue(fact);
                    if (component is AddFeatureOnClassLevel onClassLevel && onClassLevel.Feature is BlueprintScriptableObject featureOnLevel)
                        queue.Enqueue(featureOnLevel);
                    // Follow only buffs directly applied by a domain ability. This
                    // reaches effect ranks such as Touch of Good without recursively
                    // walking unrelated shared blueprint graphs.
                    if (component is AbilityEffectRunAction run)
                        foreach (var apply in PsychicRemainingDisciplines01458.Graph(run.Actions).OfType<ContextActionApplyBuff>())
                        {
                            var buff = (Get(apply, "m_Buff") as BlueprintBuffReference)?.Get();
                            if (buff != null) queue.Enqueue(buff);
                        }
                }
                // The explicit feature graph plus directly applied buffs contains
                // domain rank, upgrade and resource contracts without the previous
                // multi-minute recursive scan of every referenced blueprint.
            }
        }
    }

    [HarmonyPatch(typeof(AddSpecialSpellList), "OnActivate")]
    internal static class SacredServantClericDomainSpellbookActivate0110Patch
    {
        static bool Prefix(AddSpecialSpellList __instance) => !SacredServant0110.SuppressClericDomainSpellbook(__instance);
    }

    [HarmonyPatch(typeof(AddSpecialSpellList), "OnDeactivate")]
    internal static class SacredServantClericDomainSpellbookDeactivate0110Patch
    {
        static bool Prefix(AddSpecialSpellList __instance) => !SacredServant0110.SuppressClericDomainSpellbook(__instance);
    }

    [TypeId("0110ca11ce571a11a000000000000001")]
    public sealed class SacredServantBondChoice0110 : UnitBuffComponentDelegate { public int Kind, Value; }

    public sealed class SacredServantBondState0110 { [JsonProperty] public int CasterLevel, ChannelDc, ChannelDice; }

    [TypeId("0110ca11ce571a11a000000000000002")]
    public sealed class SacredServantBondEffect0110 : UnitBuffComponentDelegate<SacredServantBondState0110>, IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IInitiatorRulebookSubscriber
    {
        public BlueprintCharacterClassReference Paladin;
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams e)
        {
            if (e?.Initiator != Owner || e.Spell == null) return;
            if (e.Spellbook?.Blueprint?.CharacterClass == Paladin.Get() && Data.CasterLevel > 0) e.AddBonusCasterLevel(Data.CasterLevel);
            if ((e.Spell.SpellDescriptor & SpellDescriptor.ChannelPositiveHarm) != 0)
            {
                if (Data.ChannelDc > 0) e.AddBonusDC(Data.ChannelDc);
                if (Data.ChannelDice > 0) e.AddBonusCasterLevel(Data.ChannelDice * 2);
            }
        }
        public void OnEventDidTrigger(RuleCalculateAbilityParams e) { }
    }

    public sealed class SacredServantLayLedger0110 { [JsonProperty] public int RestoredToday; }

    [TypeId("0110ca11ce571a11a000000000000003")]
    public sealed class SacredServantActivateBond0110 : ContextAction
    {
        public BlueprintBuffReference Effect; public BlueprintAbilityResourceReference LayOnHands;
        public override string GetCaption() => "Lock Sacred Servant Divine Bond choices";
        public override void RunAction()
        {
            var owner = Context.MaybeCaster; if (owner == null) return;
            int[] values = new int[3]; int spent = 0;
            foreach (var buff in owner.Buffs.Enumerable)
            {
                var choice = buff.Blueprint.GetComponent<SacredServantBondChoice0110>();
                if (choice == null || choice.Kind < 0 || choice.Kind >= values.Length) continue;
                values[choice.Kind] = Math.Max(values[choice.Kind], choice.Value); spent += choice.Value;
            }
            int capacity = owner.Ensure<Kingmaker.UnitLogic.Parts.UnitPartActivatableAbility>().GetGroupSize(SacredServant0110.BondGroup);
            int restore = Math.Max(0, capacity - spent);
            var existing = owner.Buffs.Enumerable.FirstOrDefault(b => b.Blueprint == Effect.Get()); existing?.Remove();
            var applied = owner.Descriptor.AddBuff(Effect.Get(), Context, TimeSpan.FromMinutes(Math.Max(1, owner.Progression.GetClassLevel(ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(BlueprintGuid.Parse(SacredServant0110.PaladinId))))));
            var runtime = applied?.Components.FirstOrDefault(c => c.SourceBlueprintComponent is SacredServantBondEffect0110);
            if (runtime != null) using (runtime.RequestEventContext()) { var data = runtime.GetData<SacredServantBondState0110>(); data.CasterLevel = values[0]; data.ChannelDc = values[1]; data.ChannelDice = values[2]; }
            var ledgerFact = owner.Facts.List.FirstOrDefault(f => f.Blueprint.AssetGuid == PsychicAssets.Id("SacredServantDivineBondTier10110"));
            var ledger = ledgerFact?.Components.FirstOrDefault(c => c.SourceBlueprintComponent is SacredServantLayLedgerComponent0110);
            if (ledger != null && restore > 0) using (ledger.RequestEventContext())
            {
                var data = ledger.GetData<SacredServantLayLedger0110>(); int allowed = Math.Max(0, capacity - data.RestoredToday); int actual = Math.Min(restore, allowed);
                if (actual > 0) { owner.Resources.Restore(LayOnHands.Get(), actual); data.RestoredToday += actual; }
            }
        }
    }

    [TypeId("0110ca11ce571a11a000000000000004")]
    public sealed class SacredServantLayLedgerComponent0110 : UnitFactComponentDelegate<SacredServantLayLedger0110>, IUnitRestHandler, IUnitSubscriber
    {
        public void HandleUnitRest(UnitEntityData unit) { if (unit == Owner) Data.RestoredToday = 0; }
    }

    [TypeId("0110ca11ce571a11a000000000000005")]
    public sealed class SacredServantCelestialAlly0110 : ContextAction
    {
        public BlueprintCharacterClassReference Paladin; public ActionList[] Actions;
        public override string GetCaption() => "Call the level-appropriate celestial ally";
        public override void RunAction()
        {
            int level = Context.MaybeCaster?.Progression.GetClassLevel(Paladin.Get()) ?? 0; int index = level >= 16 ? 2 : level >= 12 ? 1 : 0;
            using (Context.GetDataScope(Target)) Actions[index].Run();
        }
    }
}
