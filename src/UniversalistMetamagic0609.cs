// Metamagic Mastery (COTW-0609) adapted from Call of the Wild (MIT).
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Tabletop Metamagic Mastery. The native universalist hands out fixed free
    // Extend and Reach uses at 8th level and free Empower, Maximize and Quicken
    // uses at 12th, 16th and 20th. This module replaces the whole progression
    // tail with one mutually exclusive toggle per metamagic the caster knows.
    //
    // Call of the Wild built the same feature by enumerating every
    // FeatureGroup.WizardFeat blueprint carrying AddMetamagicFeat and adding a
    // component to each of them. That only works while those blueprints are
    // already registered and it edits other mods' blueprints. The Metamagic
    // enum is a fixed [Flags] value in the game assembly, so every metamagic
    // any mod can add still maps onto one of its twelve values. Generating one
    // toggle per enum value therefore covers every present and future
    // metamagic feat without touching a foreign blueprint.
    internal static class UniversalistMetamagic0609
    {
        internal const string ModuleName = "Wizard school: Universalist Metamagic Mastery";
        internal const string RootToken = "UniversalistMetamagicMastery0609";
        // One parent entry in the ability bar; the per-metamagic toggles are its
        // sub-options. Same shape as the native hunter animal focus.
        internal const string MenuToken = RootToken + "Menu";
        const string ProgressionId = "0933849149cfc9244ac05d6a5b57fd80";
        const string ResourceId = "42fd5b455f986f94293b15b13f38d6a5";
        const string ExtendReachId = "541bb8d595532ec419343b7a93cdb449";
        const string EmpowerId = "26c7b2e163035254ead73d20fbce8c86";
        const string MaximizeId = "c9165b50a298ba346bd1b34f8d9cb9f9";
        const string QuickenId = "5e6fe442c2b4be24f969505cef85f61b";
        // MagicBolts is the only native group that no shipped blueprint and no
        // module of this project uses, so the toggles exclude each other and
        // nothing else. A 1000+ value would index past
        // UnitPartActivatableAbility.m_GroupsSizeIncreases, which is allocated
        // from EnumUtils.GetMaxValue<ActivatableAbilityGroup>().
        internal const ActivatableAbilityGroup Group = ActivatableAbilityGroup.MagicBolts;

        const string MasteryText = "At 8th level, you can apply any one metamagic feat that you know to a spell you are about to cast. This does not alter the level of the spell or the casting time. You can use this ability once per day at 8th level and one additional time per day for every two wizard levels you possess beyond 8th. Any time you use this ability to apply a metamagic feat that increases the spell level by more than 1, you must use an additional daily usage for each level above 1 that the feat adds to the spell. Even though this ability does not modify the spell's actual level, you cannot use this ability to cast a spell whose modified spell level would be above the level of the highest-level spell that you are capable of casting.\n"
            + "Metamagic Mastery lists the metamagic feats you know; turn one of them on to pick the feat your spells use, and only one can be active at a time. Extend, Reach, Selective, Bolstered, Piercing, Intensified and Completely Normal cost 1 daily use, Empower and Persistent cost 2, Maximize costs 3 and Quicken costs 4. Heighten Spell is not offered, because raising a spell's level is the one thing this ability may not do. Quicken Spell is also not held to the spell-level ceiling: this game fixes a spell's action type before the cast, where that limit cannot be checked, so quickening always costs its four uses.\n"
            + "This replaces the universalist's fixed Extend and Reach uses at 8th level and the free Empower, Maximize and Quicken uses at 12th, 16th and 20th level.";

        // Order is the order of the toggles in the ability list. Every value maps
        // to the native feat whose name, description and icon the toggle reuses.
        static readonly Metamagic[] Order =
        {
            Metamagic.Extend, Metamagic.Reach, Metamagic.Selective, Metamagic.Bolstered,
            Metamagic.Piercing, Metamagic.Intensified, Metamagic.CompletelyNormal,
            Metamagic.Empower, Metamagic.Persistent, Metamagic.Maximize, Metamagic.Quicken
        };

        static readonly string[] FeatIds =
        {
            "f180e72e4a9cbaa4da8be9bc958132ef", // Extend Spell
            "46fad72f54a33dc4692d3b62eca7bb78", // Reach Spell
            "85f3340093d144dd944fff9a9adfd2f2", // Selective Spell
            "fbf5d9ce931f47f3a0c818b3f8ef8414", // Bolstered Spell
            "c101ad6879a94204a780506f7a554865", // Piercing Spell
            "8ad7fd39abea4722b39eb5a67d606a41", // Intensified Spell
            "094b6278f7b570f42aeaa98379f07cf2", // Completely Normal Spell
            "a1de1e4f92195b442adb946f0e2b9d4e", // Empower Spell
            "cd26b9fa3f734461a0fcedc81cafaaac", // Persistent Spell
            "7f2b282626862e345935bbea5e66424b", // Maximize Spell
            "ef7ece7bb5bb66a41b256976b27f424e"  // Quicken Spell
        };

        internal static void Register(BlueprintsCache cache)
        {
            // Idempotent: the progression edit below is the only native mutation
            // and re-running it would rebuild the same table over a published one.
            if (cache.Load(Id(RootToken)) != null) return;
            var f = new PsychicAssets(cache);
            var progression = f.Native<BlueprintProgression>(ProgressionId);
            var resource = f.Native<BlueprintAbilityResource>(ResourceId);
            var extendReach = f.Native<BlueprintFeature>(ExtendReachId);

            // Anchors: the native tail must be the table this module replaces. A
            // moved school leaves the original ability intact instead of guessing.
            var tail = new Dictionary<int, string>
            {
                { 8, ExtendReachId }, { 12, EmpowerId }, { 16, MaximizeId }, { 20, QuickenId }
            };
            foreach (var pair in tail)
            {
                var entry = progression.LevelEntries.FirstOrDefault(e => e.Level == pair.Key);
                if (entry == null || entry.Features.Count(x => x.AssetGuid.ToString() == pair.Value) != 1)
                    throw new InvalidOperationException("Universalist progression layout changed; native Metamagic Mastery preserved.");
            }

            var toggles = new List<BlueprintActivatableAbility>();
            for (int i = 0; i < Order.Length; i++)
            {
                var magic = Order[i];
                var feat = f.Native<BlueprintFeature>(FeatIds[i]);
                // Base cost is one daily use, plus one per level above the first
                // that the feat adds. Completely Normal lowers the level, so its
                // -1 default cost still settles at the single base use.
                int cost = Math.Max(1, MetamagicHelper.DefaultCost(magic));
                string token = RootToken + magic;
                var spell = new MetamagicMasterySpell0609
                {
                    Metamagic = magic,
                    Resource = Ref<BlueprintAbilityResourceReference>(resource),
                    Cost = cost,
                    Increase = Math.Max(0, MetamagicHelper.DefaultCost(magic)),
                    // Quicken is the one value whose effect is the ability's action
                    // type, and that is read from UnitPartSpecialMetamagic before the
                    // cast. That registry has no resource or level predicate, so the
                    // toggle cannot both quicken and enforce the spell-level ceiling.
                    // It therefore charges its full cost for every spell instead.
                    IgnoreCeiling = magic == Metamagic.Quicken
                };
                var buff = f.Buff(token + "Buff", feat.name, "", spell);
                if (magic == Metamagic.Quicken)
                {
                    // Registered in the native special-metamagic list so the spell
                    // really becomes a swift action; the sibling component above owns
                    // the daily uses. Items stay exempt because AutoMetamagic skips
                    // anything with a source item.
                    buff.ComponentsArray = buff.ComponentsArray
                        .Concat(new BlueprintComponent[] { new QuickenActionType0609 { Metamagic = Metamagic.Quicken } }).ToArray();
                }
                var logic = new ActivatableAbilityResourceLogic { SpendType = ActivatableAbilityResourceLogic.ResourceSpendType.Never };
                Set(logic, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
                var toggle = f.Add(new BlueprintActivatableAbility
                {
                    ActivationType = AbilityActivationType.Immediately,
                    DeactivateImmediately = true,
                    DoNotTurnOffOnRest = true,
                    Group = Group,
                    ComponentsArray = new BlueprintComponent[]
                    {
                        logic,
                        new RestrictionHasEnoughResource0609 { Resource = Ref<BlueprintAbilityResourceReference>(resource), Amount = cost },
                        new RestrictionKnowsMetamagic0609 { Metamagic = magic }
                    }
                }, token + "Toggle", feat.name, "");
                Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(buff));
                Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);
                // The sub-option lives in the parent's menu, not in the bar. It
                // stays a granted fact because the native variant list resolves
                // its children from the owner's activatable abilities.
                toggle.HiddenInUI = true;
                // The sub-option reuses the native feat's own name, description and
                // icon, so it follows the game language and any text fix without a
                // second copy of the rules text in this project.
                foreach (var target in new BlueprintUnitFact[] { buff, toggle })
                {
                    Set(target, "m_DisplayName", Get(feat, "m_DisplayName"));
                    Set(target, "m_Description", Get(feat, "m_Description"));
                    Set(target, "m_Icon", feat.Icon);
                }
                toggles.Add(toggle);
            }

            // The bar entry the player actually sees. It owns no buff and cannot be
            // switched on itself (ActivationDisable), it only lists the toggles.
            var variants = new ActivatableAbilityVariants();
            Set(variants, "m_Variants", toggles.Select(Ref<BlueprintActivatableAbilityReference>).ToArray());
            var menu = f.Add(new BlueprintActivatableAbility
            {
                ActivationType = AbilityActivationType.Immediately,
                DeactivateImmediately = true,
                DoNotTurnOffOnRest = true,
                ComponentsArray = new BlueprintComponent[] { variants, new ActivationDisable() }
            }, MenuToken, "Metamagic Mastery", MasteryText);
            Set(menu, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);

            var granted = new List<BlueprintUnitFact> { menu };
            granted.AddRange(toggles);
            var feature = f.Feature(RootToken, "Metamagic Mastery", MasteryText,
                new BlueprintComponent[] { Grant(resource), Facts(granted.ToArray()) });
            Set(feature, "m_Icon", Get(extendReach, "m_Icon"));
            // The menu shows the same strings as the feature, so the translator
            // edits one entry and both follow.
            Set(menu, "m_DisplayName", Get(feature, "m_DisplayName"));
            Set(menu, "m_Description", Get(feature, "m_Description"));
            Set(menu, "m_Icon", Get(feature, "m_Icon"));

            var staged = f.Assets.Values.ToArray();
            foreach (var bp in staged) PsychicRepairComponents.UniqueNames(bp);
            var previous = progression.LevelEntries;
            var previousText = Get(progression, "m_Description");
            // Resolving the native school text bakes the current language into the
            // replacement, so it is only attempted when a string pack is present.
            // Without one the native summary is left alone rather than truncated.
            string school = null;
            try
            {
                if (Kingmaker.Localization.LocalizationManager.CurrentPack != null && previousText is Kingmaker.Localization.LocalizedString text)
                    school = text;
            }
            catch (Exception) { school = null; }
            try
            {
                f.Enable();
                foreach (var bp in staged) cache.AddCachedBlueprint(bp.AssetGuid, bp);
                progression.LevelEntries = previous.Where(e => e.Level < 8)
                    .Concat(new[] { Entry(8, feature) }).ToArray();
                // The school summary still described the fixed uses; keep its intro
                // and Hand of the Apprentice paragraph and swap the tail.
                if (school != null)
                    Set(progression, "m_Description", Text(RootToken, "school", ReplaceMastery(school, MasteryText)));
                if (progression.LevelEntries.Count(e => e.Level == 8) != 1 ||
                    progression.LevelEntries.First(e => e.Level == 8).Features.Count(x => x.AssetGuid == feature.AssetGuid) != 1 ||
                    progression.LevelEntries.Any(e => e.Level >= 8 && e.Level != 8))
                    throw new InvalidOperationException("0609 Metamagic Mastery table was not applied.");
                // The variant list belongs to the component, not to the ability:
                // reading it off the menu dereferenced a field that does not
                // exist there and aborted the whole registration.
                var listed = Get(variants, "m_Variants") as BlueprintActivatableAbilityReference[];
                if (listed == null || listed.Length != Order.Length ||
                    listed.Where((reference, index) => reference?.Get() != toggles[index]).Any() ||
                    menu.GetComponent<ActivationDisable>() == null ||
                    !GrantedFacts(feature).Contains(menu.AssetGuid))
                    throw new InvalidOperationException("0609 Metamagic Mastery menu was not applied.");
            }
            catch
            {
                progression.LevelEntries = previous;
                Set(progression, "m_Description", previousText);
                foreach (var bp in staged) if (ReferenceEquals(cache.Load(bp.AssetGuid), bp)) cache.RemoveCachedBlueprint(bp.AssetGuid);
                throw;
            }
        }

        // Keeps whatever precedes "Metamagic Mastery:" in the native school text,
        // so the encyclopedia tags and the player's language survive. An unknown
        // layout appends instead of dropping the native paragraph.
        static string ReplaceMastery(string native, string mastery)
        {
            const string marker = "Metamagic Mastery:";
            if (string.IsNullOrEmpty(native)) return "Metamagic Mastery: " + mastery;
            int at = native.IndexOf(marker, StringComparison.Ordinal);
            return at < 0 ? native + "\nMetamagic Mastery: " + mastery : native.Substring(0, at) + "Metamagic Mastery: " + mastery;
        }

        static LevelEntry Entry(int level, params BlueprintFeatureBase[] features)
        {
            var entry = new LevelEntry { Level = level };
            Set(entry, "m_Features", features.Select(Ref<BlueprintFeatureBaseReference>).ToList()); return entry;
        }

        static AddAbilityResources Grant(BlueprintAbilityResource resource)
        {
            var component = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true };
            Set(component, "m_Resource", Ref<BlueprintAbilityResourceReference>(resource)); return component;
        }
    }

    // Applies one metamagic without touching the spell's slot and settles the
    // daily uses when the cast actually happens. Call of the Wild subclassed
    // AutoMetamagic and overrode its handlers; in this game
    // AutoMetamagic.OnEventAboutToTrigger is not virtual, and its passive
    // registry carries no resource or level predicate, so the handlers are
    // implemented directly and the registry is left alone.
    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("7235d3d843da437d85ce374075f8c117")]
    public sealed class MetamagicMasterySpell0609 : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IInitiatorRulebookHandler<RuleCastSpell>
    {
        public Metamagic Metamagic;
        public BlueprintAbilityResourceReference Resource;
        // Daily uses this metamagic costs.
        public int Cost = 1;
        // Slot levels this metamagic would add, used only for the ceiling check.
        public int Increase;
        // Quicken cannot be level-capped on the action-type path, so it charges
        // its cost for every spell instead of refusing the expensive ones.
        public bool IgnoreCeiling;

        // RuleCalculateAbilityParams also runs for spellbook and action bar
        // previews, so the charge is remembered and settled by the cast. Every
        // cast recalculates first, which overwrites a stale preview charge.
        private int charged;

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            charged = 0;
            var data = evt.AbilityData;
            if (evt.Spell == null || data == null || data.Spellbook == null) return;
            if (evt.Spell.Type != AbilityType.Spell) return;
            if ((evt.Spell.AvailableMetamagic & Metamagic) == 0) return;
            // A spell that already carries this metamagic, from a rod or another
            // source, must not pay for a second copy of it.
            if (data.MetamagicData?.Has(Metamagic) == true) return;
            if (data.SourceItem != null) return;
            var resource = Resource?.Get();
            if (resource != null && Owner.Resources.GetResourceAmount(resource) < Cost) return;
            // The slot is unchanged, so the raised level only has to fit the
            // highest circle the caster can already reach.
            if (!IgnoreCeiling && data.Spellbook.MaxSpellLevel < evt.SpellLevel + Increase) return;
            charged = Cost;
            evt.AddMetamagic(Metamagic);
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }

        public void OnEventAboutToTrigger(RuleCastSpell evt) { }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            if (charged == 0) return;
            int cost = charged; charged = 0;
            var resource = Resource?.Get();
            if (resource != null && evt.Spell?.SourceItem == null) Owner.Resources.Spend(resource, cost);
        }
    }

    // Native AutoMetamagic with no work of its own: the sibling
    // MetamagicMasterySpell0609 already applied the metamagic and charged for it.
    // This one only keeps the fact in UnitPartSpecialMetamagic so
    // AbilityData.HasMetamagic reports Quicken and the cast becomes a swift
    // action. AutoMetamagic itself exempts anything cast from an item, so wands,
    // scrolls and potions are untouched.
    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("8804fe8e39af408a9cbd83a7301464e3")]
    public sealed class QuickenActionType0609 : AutoMetamagic { }

    // The toggle greys out and switches off once the daily pool cannot pay for it.
    [AllowedOn(typeof(BlueprintActivatableAbility), false)]
    [TypeId("90be5b1bcd0a45b5be01fd7751769c2e")]
    public sealed class RestrictionHasEnoughResource0609 : ActivatableAbilityRestriction
    {
        public BlueprintAbilityResourceReference Resource;
        public int Amount = 1;

        protected override bool IsAvailable()
        {
            var resource = Resource?.Get();
            return resource == null || Owner.Resources.GetResourceAmount(resource) >= Amount;
        }
    }

    // "Any metamagic feat that you know": the toggle only lights up when the
    // owner carries a fact whose AddMetamagicFeat maps to this value, which also
    // covers metamagic feats introduced by other mods.
    [AllowedOn(typeof(BlueprintActivatableAbility), false)]
    [TypeId("f39a832738114073a608ff25596fc881")]
    public sealed class RestrictionKnowsMetamagic0609 : ActivatableAbilityRestriction
    {
        public Metamagic Metamagic;

        internal static bool Knows(UnitDescriptor owner, Metamagic metamagic)
        {
            if (owner == null) return false;
            foreach (var fact in owner.Facts.List)
            {
                var source = fact?.Blueprint;
                if (source == null) continue;
                var add = source.GetComponent<AddMetamagicFeat>();
                if (add != null && add.Metamagic == metamagic) return true;
            }
            return false;
        }

        protected override bool IsAvailable() => Knows(Owner, Metamagic);
    }

    // The native variant list hands the menu every child the owner holds as a
    // fact, and all eleven toggles are granted up front. Only the metamagics this
    // caster actually knows belong in Metamagic Mastery, so the rest are dropped
    // here. Only this project's toggles carry the restriction, so another mod's
    // variant list is passed through untouched.
    [HarmonyPatch(typeof(ActivatableAbilityVariants), nameof(ActivatableAbilityVariants.GetConversions))]
    internal static class MetamagicMasteryVariants0609Patch
    {
        static void Postfix(UnitDescriptor owner, ref IEnumerable<ActivatableAbility> __result)
        {
            if (owner == null || __result == null) return;
            __result = Known(owner, __result);
        }

        static IEnumerable<ActivatableAbility> Known(UnitDescriptor owner, IEnumerable<ActivatableAbility> source)
        {
            foreach (var ability in source)
            {
                var restriction = ability?.Blueprint?.GetComponent<RestrictionKnowsMetamagic0609>();
                if (restriction != null && !RestrictionKnowsMetamagic0609.Knows(owner, restriction.Metamagic)) continue;
                yield return ability;
            }
        }
    }
}
