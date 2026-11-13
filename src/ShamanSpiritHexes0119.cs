using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    /// <summary>
    /// COTW-0119. Publishes the eight shaman hexes that Wrath never shipped and no
    /// other Wrath mod supplies, as ordinary Shaman/Wisdom spirit hexes. They are
    /// registered in the native ShamanHexSelection (and the SpiritTools
    /// wandering/unsworn pools) with a prerequisite on the spirit that owns them,
    /// so Spirit Whisperer's Discover() and the data-driven Spirit Guide both pick
    /// them up automatically - no GUID catalogue in the consumer.
    ///
    /// Targeted hexes use Owlcat's native 24-hour lock (hidden cooldown buff,
    /// StackingType.Replace, AbilityTargetHasFact with Inverted=true, applied
    /// before the saving throw so success and failure both lock the target).
    /// Spirits that do not exist in the current game (e.g. Heavens without
    /// ExpandedContent) simply have their hexes skipped, so the pool always
    /// mirrors what the game actually offers.
    /// </summary>
    internal static class ShamanSpiritHexes0119
    {
        internal const string ModuleName = "Shaman Spirit Hexes";
        internal const string RootToken = "ShamanSpiritHexes0119";

        const string ShamanId = "145f1d3d360a7ad48bd95d392c81b38e";
        const string SpiritSelectionId = "00c8c566d1825dd4a871250f35285982";
        const string HexSelectionId = "4223fe18c75d4d14787af196a04e14e7";

        const string BlindnessBuffId = "187f88d96a0ef464280706b63635f2af";
        const string BlurBuffId = "dd3ad347240624d46a11a092b4dd4674";
        const string SlowAbilityId = "f492622e473d34747806bdb39356eb89";
        const string FluidMagicIconId = "e3f41966c2d662a4e9582a0497621c46";
        const string ForcedRepentanceId = "cc0aeb74b35cb7147bff6c53538bbc76";
        const string DimensionDoorId = "5bdc37e4acfa209408334326076a43bc";
        // CotW drew Crashing Waves from Tsunami's icon. Stormwalker keeps the
        // original CotW art, embedded from CallOfTheWild/Icons/AbilityIcons.
        const string TsunamiAbilityId = "d8144161e352ca846a73cf90e85bf9ac";
        const string StormwalkerIconAsset = "StormStep";
        // Shared native cast visual used by the project's other spells when a
        // donor has no AbilitySpawnFx of its own.
        const string CastFxAssetId = "cbfe312cb8e63e240a859efaad8e467c";

        // Native pools that hand a shaman a borrowed hex: vanilla
        // ShamanWanderingHexAbility/2 plus the six Unsworn Shaman minor-spirit
        // pools. Each is an AbilityApplyFact whose fact list is the pool.
        static readonly string[] ApplyFactPoolIds =
        {
            "bff5039c06a4a494b84bbca5c1cf93e5", "8e37166acde792f458c1c02a7392ab7c",
            "cf57e3971a4f8d340a200794ba586bf9", "4248be819e269724086d8f6b44fafd10",
            "49c4c45281805fc43aa66cd036825450", "6485fe813137acd41aa56df5a39e0195",
            "e7be24dd1cf591d4baecd9578705513d", "1c06611ab264115479d16c0ff3ab0446"
        };

        // ExpandedContent publishes a spirit as a progression in the spirit
        // selection and again as a "wandering" feature granted by the Wandering
        // Spirit abilities. Its own Spirit Hexes accept either form through
        // Prerequisite.GroupType.Any, so they stay selectable when the spirit is
        // only borrowed. Only the progression was wired here, which meant a hex
        // could be listed in a Wandering Hex pool and still be filtered out.
        // Discover the wandering forms from those pools rather than keeping a
        // second GUID catalogue; a missing provider simply adds no extra form.
        static readonly string[] WanderingSpiritPoolIds =
        {
            "709db7ec794271b43b9c416b520d765c", "88032ba214e97a442849ef0d15ce1485",
            "77e02067a4f59ba4793c79a34391a658"
        };

        static BlueprintFeature[] WanderingSpiritForms(BlueprintsCache cache, string spirit)
        {
            var result = new List<BlueprintFeature>();
            foreach (var id in WanderingSpiritPoolIds)
            {
                var pool = cache.Load(BlueprintGuid.Parse(id)) as BlueprintScriptableObject;
                if (pool == null) continue;
                foreach (var guid in GrantedFacts(pool))
                {
                    var feature = cache.Load(guid) as BlueprintFeature;
                    var name = feature?.name ?? "";
                    // "Wandering" keeps this to spirit forms; plain hexes and
                    // unrelated features with the same spirit word are excluded.
                    if (feature != null && name.IndexOf(spirit, StringComparison.OrdinalIgnoreCase) >= 0
                        && name.IndexOf("Wandering", StringComparison.OrdinalIgnoreCase) >= 0)
                        result.Add(feature);
                }
            }
            return result.Distinct().ToArray();
        }

        internal sealed class Spec
        {
            internal readonly string Name, Spirits, Description;
            internal Spec(string name, string spirits, string description) { Name = name; Spirits = spirits; Description = description; }
        }

        // Descriptions are the original CotW text verbatim, including its
        // typographic apostrophes and wording. Only the sentences whose effect
        // actually changed in this port were adjusted.
        internal static readonly Spec[] Specs =
        {
            new Spec("Curse of Suffering", "Battle,Life", "The shaman causes a creature within 30 feet to take more damage from bleed effects and causes its wounds to heal at a slower rate. When the cursed creature takes bleed damage, it takes 1 additional point of bleed damage (even if the bleed is ability damage). Furthermore, when the target is subject to an effect that would restore its hit points, that effect restores only half the normal amount of hit points. This curse lasts for a number of rounds equal to the shaman’s level. A creature affected by this hex cannot be affected by it again for 24 hours."),
            new Spec("Enveloping Void", "Heavens", "The shaman curses one creature with the dark void. As a standard action, the shaman can cause one enemy within close range to become blind. This effect lasts for a number of rounds equal to the shaman’s level. A successful Will saving throw negates this effect. Whether or not the save is successful, the creature cannot be the target of this hex again for 24 hours."),
            new Spec("Heaven's Leap", "Heavens", "The shaman is adept at creating tiny tears in the fabric of space, and temporarily stitching them together to reach other locations through a limited, one-way wormhole. As a standard action, the shaman can designate herself or a single ally that she can see who is within close range of her. She can move that creature to any point within close range of herself. Once targeted by this hex, the ally cannot be the target of this hex again for 24 hours."),
            new Spec("Stormwalker", "Nature", "The shaman can move through non-magical fog, rain, mist, snow, and other environmental effects without penalty. She is never slowed by such effects. At 10th level she is also ignoring any concealement that is provided by environmental effects, whether or not they are magical in nature."),
            new Spec("Loadstone", "Stone", "The shaman causes one creature within 30 feet to become heavy and lethargic. The creature is treated as if it suffered effect of slow spell. The effect lasts for a number of rounds equal to the shaman’s level. A successful Will saving throw negates this effect. Whether or not the save is successful, the creature cannot be the target of this hex again for 24 hours."),
            new Spec("Crashing Waves", "Waves", "The force of a waves shaman’s water spells can bring even the mightiest of foes to the ground. When the shaman casts a spell with the water descriptor, she does so at 1 caster level higher. If that spell deals damage, the target must succeed at a Fortitude saving throw or be knocked prone; a cold spell the shaman casts can knock the target prone in the same way. At 8th level, the shaman casts cold spells at 2 caster levels higher. At 16th level, her ability to knock creatures prone extends to any spell that deals damage."),
            new Spec("Mist's Shroud", "Waves", "The shaman touches a willing creature (including herself ) and enshrouds that creature in mist. This grants the creature concealment as the blur spell. The mist dissipates after it causes an attack to miss because of concealment or after 1 minute, whichever comes first. At 8th and 16th levels, the mist lasts for one additional attack. A creature affected by this hex cannot be affected by it again for 24 hours."),
            new Spec("Fluid Magic", "Waves", "The shaman’s magic is not constrained by the reservoirs of magic that hold others back. She is able to prepare her spirit magic spells in her regular spell slots.")
        };

        static readonly Dictionary<string, string[]> Owners = Specs.ToDictionary(
            s => s.Name, s => s.Spirits.Split(','));

        internal static bool Register(BlueprintsCache cache)
        {
            var existing = cache.Load(Id(RootToken));
            if (existing != null)
            {
                if (existing.name != "CotWWrathPsychic" + RootToken)
                    throw new InvalidOperationException("0119 hex marker GUID is occupied by an unexpected blueprint.");
                Reconcile(cache);
                return true;
            }

            var f = new PsychicAssets(cache);
            var shaman = f.Native<BlueprintCharacterClass>(ShamanId);
            var spiritPool = f.Native<BlueprintFeatureSelection>(SpiritSelectionId);
            var hexPool = f.Native<BlueprintFeatureSelection>(HexSelectionId);

            // Resolve owners by the spirits that are actually published. No
            // spirit GUID is stored: a mod-added spirit with a matching name is
            // picked up on the next load without any code change.
            var spirits = DiscoverSpirits(cache, spiritPool);
            var built = new List<BlueprintFeature>();
            foreach (var spec in Specs)
            {
                var owners = Owners[spec.Name].Select(n => FindSpirit(spirits, n)).Where(p => p != null).Distinct().ToArray();
                if (owners.Length == 0) continue; // spirit not installed in this game
                built.Add(Build(f, shaman, spec, owners));
            }
            if (built.Count == 0) return false;

            var marker = f.Add(new BlueprintFeature { Ranks = 1, HideInUI = true, HideInCharacterSheetAndLevelUp = true },
                RootToken, "", "");

            foreach (var bp in f.Assets.Values)
            {
                PsychicRepairComponents.UniqueNames(bp);
                Set(bp, "m_AllElements", SpellBatch01447.Elements(bp.ComponentsArray, new HashSet<object>(IdentityComparer)).ToList());
                if (bp is BlueprintAbility ability) SpellBatch01447.ResetAbilityCaches(ability);
            }
            var assets = f.Assets.Values.ToArray();
            if (assets.Any(bp => cache.Load(bp.AssetGuid) != null))
                throw new InvalidOperationException("0119 hex GUID set is partially occupied.");

            var selections = new[] { hexPool };
            var selectionOld = selections.Select(s => new[] { Get(s, "m_Features"), Get(s, "m_AllFeatures") }).ToArray();
            var external = ApplyFactPoolIds.Select(id => cache.Load(BlueprintGuid.Parse(id)) as BlueprintScriptableObject
                ?? throw new InvalidOperationException("0119 SpiritTools anchor missing: " + id)).ToArray();
            var externalOld = external.Select(Snapshot).ToArray();
            try
            {
                f.Enable();
                foreach (var bp in assets) cache.AddCachedBlueprint(bp.AssetGuid, bp);
                T09CombatFeats.Append(selections, built.ToArray());
                Connect(external, built.ToArray());
                Verify(cache, built.ToArray());
                return true;
            }
            catch
            {
                for (int i = 0; i < selections.Length; i++) { Set(selections[i], "m_Features", selectionOld[i][0]); Set(selections[i], "m_AllFeatures", selectionOld[i][1]); }
                for (int i = 0; i < external.Length; i++) Restore(external[i], externalOld[i]);
                foreach (var bp in assets) if (ReferenceEquals(cache.Load(bp.AssetGuid), bp)) cache.RemoveCachedBlueprint(bp.AssetGuid);
                throw;
            }
        }

        static BlueprintFeature Build(PsychicAssets f, BlueprintCharacterClass shaman, Spec spec, BlueprintProgression[] owners)
        {
            var token = "ShamanHex" + spec.Name.Replace("'", "").Replace(" ", "").Replace("-", "") + "0119";
            BlueprintFeature feature;
            switch (spec.Name)
            {
                case "Curse of Suffering": feature = CurseOfSuffering(f, shaman, token, spec.Description); break;
                case "Enveloping Void": feature = EnvelopingVoid(f, shaman, token, spec.Description); break;
                case "Heaven's Leap": feature = HeavensLeap(f, token, spec.Description); break;
                case "Stormwalker": feature = Stormwalker(f, shaman, token, spec.Description); break;
                case "Loadstone": feature = Loadstone(f, shaman, token, spec.Description); break;
                case "Crashing Waves": feature = CrashingWaves(f, shaman, token, spec.Description); break;
                case "Mist's Shroud": feature = MistsShroud(f, shaman, token, spec.Description); break;
                case "Fluid Magic": feature = FluidMagic(f, shaman, spec.Description); break;
                default: throw new NotSupportedException("0119 no builder for " + spec.Name);
            }
            feature.IsClassFeature = true;
            feature.Groups = new[] { FeatureGroup.ShamanHex };

            // One prerequisite per spirit form, OR'ed by the native prerequisite
            // group: the owning progression (normal hex list) plus the wandering
            // feature (Wandering Hex). Spirit Whisperer's Owned() and the
            // data-driven Spirit Guide read these references back for attribution.
            var forms = owners.Select(o => (BlueprintFeature)o).ToList();
            foreach (var spirit in Owners[spec.Name])
                forms.AddRange(WanderingSpiritForms(f.Cache, spirit));
            var prerequisites = new List<BlueprintComponent>();
            foreach (var form in forms.Distinct())
            {
                var need = new PrerequisiteFeature { Group = Prerequisite.GroupType.Any };
                Set(need, "m_Feature", Ref<BlueprintFeatureReference>(form));
                prerequisites.Add(need);
            }
            feature.ComponentsArray = feature.ComponentsArray.Concat(prerequisites).ToArray();
            return feature;
        }

        // ---------------- Owlcat's native 24-hour lock ----------------

        static BlueprintBuff Cooldown(PsychicAssets f, string token, string hex)
        {
            var buff = f.Buff(token + "Cooldown", hex + " Cooldown",
                "This creature cannot be targeted by " + hex + " again for 24 hours.");
            buff.Stacking = StackingType.Replace;
            SetEnum(buff, "m_Flags", "HiddenInUi, RemoveOnRest");
            return buff;
        }

        // Owlcat's native lock is one target check plus a single
        // AbilityEffectRunAction whose first action applies the cooldown, before
        // any saving throw. Multiple Run components are not all executed, so the
        // effect actions must live in the same Run as the cooldown application.
        static AbilityTargetHasFact CooldownCheck(BlueprintBuff cooldown)
        {
            var check = new AbilityTargetHasFact { Inverted = true, FromCaster = false };
            Set(check, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(cooldown) });
            return check;
        }

        static ContextActionApplyBuff CooldownApply(BlueprintBuff cooldown)
        {
            var apply = Apply(cooldown, 1, DurationRate.Days, fromSpell: false);
            apply.Permanent = false;
            apply.IsNotDispelable = true;
            return apply;
        }

        static ContextRankConfig Rank(BlueprintCharacterClass shaman, ContextRankProgression progression = ContextRankProgression.AsIs, int step = 0)
        {
            var rank = new ContextRankConfig();
            Set(rank, "m_BaseValueType", ContextRankBaseValueType.ClassLevel);
            Set(rank, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(shaman) });
            Set(rank, "m_Progression", progression);
            if (step > 0) Set(rank, "m_StepLevel", step);
            Set(rank, "m_UseMin", true); Set(rank, "m_Min", 1);
            return rank;
        }

        static ContextCalculateAbilityParamsBasedOnClass Params(BlueprintCharacterClass shaman)
        {
            var p = new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Wisdom };
            Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(shaman));
            return p;
        }

        // Original CotW hexes reused the donor ability's icon and its cast
        // visual (Forced Repentance for Curse of Suffering, Blindness for
        // Enveloping Void, Dimension Door for Heaven's Leap, Slow for Loadstone).
        // These staged abilities were published without either. One shared
        // helper guarantees exactly one AbilitySpawnFx per ability, with the
        // project's standard fallback when a donor has none.
        static void CastPresentation(BlueprintAbility ability, BlueprintUnitFact iconDonor, PrefabLink fx)
        {
            if (ability == null) return;
            if (iconDonor != null) Set(ability, "m_Icon", iconDonor.Icon);
            // Some native donors carry an AbilitySpawnFx with an empty AssetId;
            // that would render nothing, so fall back to the project's standard.
            var link = fx != null && !string.IsNullOrEmpty(fx.AssetId)
                ? CopyData(fx) : new PrefabLink { AssetId = CastFxAssetId };
            var spawn = new AbilitySpawnFx
            {
                PrefabLink = link,
                Time = AbilitySpawnFxTime.OnApplyEffect,
                Anchor = AbilitySpawnFxAnchor.SelectedTarget,
                PositionAnchor = AbilitySpawnFxAnchor.None,
                OrientationAnchor = AbilitySpawnFxAnchor.None
            };
            ability.ComponentsArray = (ability.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Where(c => c != null && !(c is AbilitySpawnFx))
                .Concat(new BlueprintComponent[] { spawn }).ToArray();
        }

        static BlueprintAbility OptionalAbility(PsychicAssets f, string guid) =>
            f.Cache.Load(BlueprintGuid.Parse(guid)) as BlueprintAbility;

        // The borrowed-hex pools list the hex *features*, not their abilities, and
        // an icon-less feature is drawn with the pool's own icon (Wandering Hex),
        // so every hex feature carries the icon of the donor its abilities use.
        static void FeatureIcon(BlueprintFeature feature, BlueprintUnitFact donor)
        {
            if (feature != null && donor != null && donor.Icon != null) Set(feature, "m_Icon", donor.Icon);
        }

        static BlueprintUnitFact OptionalIcon(PsychicAssets f, string guid) =>
            f.Cache.Load(BlueprintGuid.Parse(guid)) as BlueprintUnitFact;

        static ContextValue RankValue() => new ContextValue { ValueType = ContextValueType.Rank };

        static ContextActionApplyBuff RankRounds(BlueprintBuff buff)
        {
            var apply = Apply(buff, 0);
            apply.DurationValue.BonusValue = RankValue();
            return apply;
        }

        // ---------------- hex builders ----------------

        static BlueprintFeature CurseOfSuffering(PsychicAssets f, BlueprintCharacterClass shaman, string token, string desc)
        {
            // IncomingDamageAndHealingModifier is a signed percentage: -50 makes
            // the carrier receive half. A positive value is an *increase*, which is
            // why the hex healed the target for an extra half instead of halving it.
            var heal = Component("IncomingDamageAndHealingModifier", F("Type", "OnlyHeal"),
                F("ModifierPercents", new ContextValue { Value = -50 }));
            if (heal == null) throw new InvalidOperationException("0119 IncomingDamageAndHealingModifier missing.");
            var buff = f.Buff(token + "Buff", "Curse of Suffering", desc, heal, new BleedIncrease0117());
            var cooldown = Cooldown(f, token, "Curse of Suffering");
            var ability = f.Ability(token + "Ability", "Curse of Suffering", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                CooldownCheck(cooldown), Run(CooldownApply(cooldown), RankRounds(buff)), Params(shaman), Rank(shaman));
            var donor = OptionalAbility(f, ForcedRepentanceId);
            var iconDonor = donor != null ? (BlueprintUnitFact)donor : f.Native<BlueprintUnitFact>(FluidMagicIconId);
            CastPresentation(ability, iconDonor, donor?.GetComponent<AbilitySpawnFx>()?.PrefabLink);
            var feature = f.Feature(token, "Curse of Suffering", desc, Facts(ability));
            FeatureIcon(feature, iconDonor);
            return feature;
        }

        static BlueprintFeature EnvelopingVoid(PsychicAssets f, BlueprintCharacterClass shaman, string token, string desc)
        {
            var donor = f.Native<BlueprintBuff>(BlindnessBuffId);
            var buff = (BlueprintBuff)f.Clone(donor, token + "Buff", "Enveloping Void", desc);
            var cooldown = Cooldown(f, token, "Enveloping Void");
            var ability = f.Ability(token + "Ability", "Enveloping Void", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                CooldownCheck(cooldown),
                Run(CooldownApply(cooldown), new ContextActionSavingThrow
                {
                    Type = SavingThrowType.Will,
                    Actions = Actions(Saved(Array.Empty<GameAction>(), new GameAction[] { RankRounds(buff) }))
                }),
                Params(shaman), Rank(shaman));
            CastPresentation(ability, donor, donor.FxOnStart);
            var feature = f.Feature(token, "Enveloping Void", desc, Facts(ability));
            FeatureIcon(feature, donor);
            return feature;
        }

        static BlueprintFeature HeavensLeap(PsychicAssets f, string token, string desc)
        {
            var door = OptionalAbility(f, DimensionDoorId);
            var iconDonor = door != null ? (BlueprintUnitFact)door : f.Native<BlueprintUnitFact>(FluidMagicIconId);
            var doorFx = door?.GetComponent<AbilitySpawnFx>()?.PrefabLink;
            var mark = f.Buff(token + "Mark", "Heaven's Leap Target", desc, new HeavensLeapMark0060());
            var cooldown = Cooldown(f, token, "Heaven's Leap");
            // The Deliver component applies the 24-hour lock only after a real
            // move. Applying it here would make Deliver see its own lock and
            // refuse the teleport, so the selection stage only checks the lock.
            var choose = f.Ability(token + "Select", "Heaven's Leap: Select", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Free, AbilityRange.Close, CooldownCheck(cooldown), Run(Apply(mark, 1)));
            choose.CanTargetEnemies = false; choose.CanTargetFriends = true; choose.CanTargetSelf = true;
            var leap = new HeavensLeap0060 { Mark = mark, DayLock = cooldown };
            var move = f.Ability(token + "Ability", "Heaven's Leap", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close, leap);
            move.CanTargetPoint = true; move.CanTargetFriends = false; move.CanTargetEnemies = false; move.CanTargetSelf = false;
            CastPresentation(choose, iconDonor, doorFx);
            CastPresentation(move, iconDonor, doorFx);
            var feature = f.Feature(token, "Heaven's Leap", desc, Facts(choose, move));
            FeatureIcon(feature, iconDonor);
            return feature;
        }

        static BlueprintFeature Stormwalker(PsychicAssets f, BlueprintCharacterClass shaman, string token, string desc)
        {
            // Reuse the project's verified Windsight filter: the Harmony patch on
            // UnitPartConcealment.Calculate removes only Fog-descriptor entries
            // for this initiator. The old FogConcealment0117 guessed field names
            // and compared Descriptor.ToString() to "Fog", which never matched.
            var fog = f.Feature(token + "Fog", "Stormwalker Fog", desc, new KineticWindsight01433());
            var gate = new AddFeatureOnClassLevel { Level = 10 };
            Set(gate, "m_Class", Ref<BlueprintCharacterClassReference>(shaman));
            Set(gate, "m_Feature", Ref<BlueprintFeatureReference>(fog));
            Set(gate, "m_AdditionalClasses", Array.Empty<BlueprintCharacterClassReference>());
            Set(gate, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            var feature = f.Feature(token, "Stormwalker", desc, new ShamanStormwalker0119(), gate);
            Set(feature, "m_Icon", PsychicOriginalIcons.Load(StormwalkerIconAsset));
            return feature;
        }

        static BlueprintFeature Loadstone(PsychicAssets f, BlueprintCharacterClass shaman, string token, string desc)
        {
            var slow = f.Native<BlueprintAbility>(SlowAbilityId);
            var ability = (BlueprintAbility)f.Clone(slow, token + "Ability", "Loadstone", desc);
            ability.Type = AbilityType.Supernatural;
            ability.Range = AbilityRange.Close;
            ability.SpellResistance = false;
            ability.ActionType = UnitCommand.CommandType.Standard;
            ability.AvailableMetamagic = 0;
            var cooldown = Cooldown(f, token, "Loadstone");
            // Original CotW keeps the donor Slow's own AbilityEffectRunAction.
            // Deleting and rebuilding it here dropped the donor's saving-throw
            // metadata and effect payload, so no Will prompt and no slow landed.
            // Insert the 24-hour lock into the donor's existing action list
            // instead - the same pattern 0117's AddCooldown uses.
            var run = ability.GetComponent<AbilityEffectRunAction>();
            var components = ability.ComponentsArray
                .Where(c => c != null && !(c is SpellListComponent) && !(c is SpellComponent) && !(c is AbilityTargetsAround))
                .ToList();
            if (run != null)
            {
                var list = Get(run, "Actions");
                var old = (GameAction[])Get(list, "Actions") ?? Array.Empty<GameAction>();
                Set(list, "Actions", new GameAction[] { CooldownApply(cooldown) }.Concat(old).ToArray());
            }
            else
            {
                // Unusual donor shape: still publish the lock instead of failing
                // the whole hex module; the donor's effects are absent anyway.
                components.Add(Run(CooldownApply(cooldown)));
            }
            components.Add(CooldownCheck(cooldown));
            components.Add(Params(shaman));
            ability.ComponentsArray = components.ToArray();
            Set(ability, "m_Parent", new BlueprintAbilityReference());
            CastPresentation(ability, slow, slow.GetComponent<AbilitySpawnFx>()?.PrefabLink);
            var feature = f.Feature(token, "Loadstone", desc, Facts(ability));
            FeatureIcon(feature, slow);
            return feature;
        }

        static BlueprintFeature CrashingWaves(PsychicAssets f, BlueprintCharacterClass shaman, string token, string desc)
        {
            // Owlcat's knockdown is a short-lived buff carrying AddCondition(Prone):
            // the condition is registered against its source Buff and disappears
            // with it. Calling State.AddCondition directly has no source fact, so
            // the target stays prone forever. Original CotW used the native
            // ContextActionKnockdownTarget for exactly this reason.
            var prone = f.Buff(token + "ProneBuff", "Knocked Prone",
                "This creature has been knocked prone and gets up after one round.",
                new AddCondition { Condition = UnitCondition.Prone });
            var feature = f.Feature(token, "Crashing Waves", desc,
                new ShamanCrashingWaves0119
                {
                    Shaman = Ref<BlueprintCharacterClassReference>(shaman),
                    Prone = Ref<BlueprintBuffReference>(prone)
                });
            // CotW gave this hex Tsunami's icon; Water Blast is only a fallback.
            FeatureIcon(feature, OptionalIcon(f, TsunamiAbilityId) ?? f.Native<BlueprintUnitFact>(FluidMagicIconId));
            return feature;
        }

        static BlueprintFeature MistsShroud(PsychicAssets f, BlueprintCharacterClass shaman, string token, string desc)
        {
            var donor = f.Native<BlueprintBuff>(BlurBuffId);
            var buff = (BlueprintBuff)f.Clone(donor, token + "Buff", "Mist's Shroud", desc);
            buff.ComponentsArray = buff.ComponentsArray.Concat(new BlueprintComponent[] { new ShamanMistCharges0119() }).ToArray();
            var cooldown = Cooldown(f, token, "Mist's Shroud");
            var ability = f.Ability(token + "Ability", "Mist's Shroud", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Touch,
                CooldownCheck(cooldown), Run(CooldownApply(cooldown), Apply(buff, 1, DurationRate.Minutes)));
            ability.CanTargetEnemies = false;
            Set(ability, "m_Icon", donor.Icon);
            var feature = f.Feature(token, "Mist's Shroud", desc, Facts(ability));
            FeatureIcon(feature, donor);
            return feature;
        }

        // "Spirit magic spells are prepared in your regular spell slots." Wrath's
        // spirit magic is a special list that only feeds the spirit-magic pool, so
        // the fix is to copy those spells into the shaman's own spell list. That is
        // exactly what the original Kingmaker CotW did: one hidden feature per
        // spirit carrying a LearnSpellList for that spirit's spirit-magic list,
        // gated on the spirit being bound.
        static BlueprintFeature FluidMagic(PsychicAssets f, BlueprintCharacterClass shaman, string description)
        {
            var parts = new List<BlueprintComponent>();
            int built = 0;
            foreach (var spirit in DiscoverSpirits(f.Cache, f.Native<BlueprintFeatureSelection>(SpiritSelectionId)))
            {
                var list = SpiritMagicList(spirit);
                if (list == null) continue;
                var token = "ShamanFluidMagic" + spirit.AssetGuid.ToString().Replace("-", "") + "0119";
                var learn = new LearnSpellList();
                Set(learn, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(shaman));
                Set(learn, "m_SpellList", Ref<BlueprintSpellListReference>(list));
                var carrier = f.Feature(token, "", "", learn);
                carrier.HideInUI = true;
                carrier.HideInCharacterSheetAndLevelUp = true;
                // AddFeatureIfHasFact: the carrier is live only while that spirit
                // is the one actually bound to this shaman.
                var gate = Component("AddFeatureIfHasFact",
                    F("m_Feature", Ref<BlueprintUnitFactReference>(carrier)),
                    F("m_CheckedFact", Ref<BlueprintUnitFactReference>(spirit)));
                if (gate == null) continue;
                parts.Add(gate);
                built++;
            }
            var feature = f.Feature("ShamanHexFluidMagic0119", "Fluid Magic", description, parts.ToArray());
            Set(feature, "m_Icon", f.Native<BlueprintUnitFact>(FluidMagicIconId).Icon);
            if (built == 0) throw new InvalidOperationException("0119 Fluid Magic found no spirit magic list to fold into the shaman spell list.");
            return feature;
        }

        // A spirit's spirit-magic table is the list its own progression publishes
        // through AddSpellsToDescription; the same object the real shaman reads.
        static BlueprintSpellList SpiritMagicList(BlueprintProgression spirit)
        {
            var component = spirit.GetComponent<AddSpellsToDescription>();
            if (component == null) return null;
            var lists = Get(component, "m_SpellLists") as BlueprintSpellListReference[];
            return lists != null && lists.Length > 0 ? lists[0]?.Get() as BlueprintSpellList : null;
        }

        // ---------------- discovery / registration plumbing ----------------

        static BlueprintProgression[] DiscoverSpirits(BlueprintsCache cache, BlueprintFeatureSelection pool)
        {
            var result = new List<BlueprintProgression>();
            foreach (var reference in (BlueprintFeatureReference[])Get(pool, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>())
            {
                var progression = reference?.Get() as BlueprintProgression;
                if (progression != null && HasGroup(progression, "ShamanSpirit")) result.Add(progression);
            }
            return result.ToArray();
        }

        static BlueprintProgression FindSpirit(BlueprintProgression[] spirits, string name)
        {
            foreach (var p in spirits)
            {
                string localized = null;
                try { localized = (p as BlueprintUnitFact)?.Name; } catch { /* unresolved localization */ }
                if (string.Equals(localized, name, StringComparison.OrdinalIgnoreCase)) return p;
                if ((p.name ?? "").IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return p;
            }
            return null;
        }

        static bool HasGroup(SimpleBlueprint bp, string group)
        {
            object raw;
            try { raw = Get(bp, "Groups"); }
            catch (MissingFieldException)
            {
                var property = AccessTools.Property(bp.GetType(), "Groups");
                if (property == null) return false;
                raw = property.GetValue(bp, null);
            }
            if (!(raw is System.Collections.IEnumerable items)) return false;
            foreach (var item in items) if (item != null && item.ToString() == group) return true;
            return false;
        }

        static KeyValuePair<string, object> F(string key, object value) => new KeyValuePair<string, object>(key, value);

        static BlueprintComponent Component(string typeName, params KeyValuePair<string, object>[] fields)
        {
            var type = typeof(BlueprintComponent).Assembly.GetTypes().FirstOrDefault(t => t.Name == typeName && typeof(BlueprintComponent).IsAssignableFrom(t));
            if (type == null) return null;
            var component = (BlueprintComponent)Activator.CreateInstance(type);
            foreach (var pair in fields)
            {
                var field = AccessTools.Field(type, pair.Key);
                var property = AccessTools.Property(type, pair.Key);
                var target = field?.FieldType ?? property?.PropertyType;
                if (target == null) continue;
                var value = pair.Value;
                if (target.IsEnum && value is string s) value = Enum.Parse(target, s);
                if (field != null) field.SetValue(component, value);
                else if (property != null && property.GetSetMethod(true) != null) property.SetValue(component, value, null);
            }
            return component;
        }

        static Dictionary<FieldInfo, object> Snapshot(BlueprintScriptableObject bp) => Fields(bp).ToDictionary(x => x, x => x.GetValue(bp));
        static void Restore(BlueprintScriptableObject bp, Dictionary<FieldInfo, object> snapshot) { foreach (var pair in snapshot) pair.Key.SetValue(bp, pair.Value); }
        static FieldInfo[] Fields(BlueprintScriptableObject bp) => bp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.FieldType.IsArray && typeof(BlueprintReferenceBase).IsAssignableFrom(f.FieldType.GetElementType())).ToArray();

        // The SpiritTools wandering/unsworn pools are not selections: they are
        // AbilityApplyFact components that grant a fixed fact list. That list
        // lives inside the component, not in an array field on the blueprint, so
        // scanning only the blueprint's own reference fields (what this used to
        // do) matched nothing and the new hexes never appeared in Wandering Hex.
        static void Connect(BlueprintScriptableObject[] pools, BlueprintFeature[] hexes)
        {
            foreach (var bp in pools)
            {
                // Blueprint-level arrays first, for any pool shaped that way.
                foreach (var field in Fields(bp))
                    field.SetValue(bp, Appended(field.FieldType, field.GetValue(bp) as Array, hexes));

                // Then the real shape: every component that carries a fact list.
                foreach (var component in bp.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                {
                    if (component == null) continue;
                    var facts = AccessTools.Field(component.GetType(), "m_Facts");
                    if (facts == null || !facts.FieldType.IsArray) continue;
                    var element = facts.FieldType.GetElementType();
                    if (element == null || !typeof(BlueprintReferenceBase).IsAssignableFrom(element)) continue;
                    facts.SetValue(component, Appended(facts.FieldType, facts.GetValue(component) as Array, hexes));
                }
            }
        }

        static Array Appended(Type arrayType, Array current, BlueprintFeature[] hexes)
        {
            var element = arrayType.GetElementType();
            var list = (current ?? Array.CreateInstance(element, 0)).Cast<BlueprintReferenceBase>().ToList();
            foreach (var hex in hexes)
                if (!list.Any(r => r != null && r.Guid == hex.AssetGuid))
                    list.Add(Ref<BlueprintUnitFactReference>(hex));
            var next = Array.CreateInstance(element, list.Count);
            for (int i = 0; i < list.Count; i++) next.SetValue(list[i], i);
            return next;
        }

        static void Verify(BlueprintsCache cache, BlueprintFeature[] hexes)
        {
            var pool = cache.Load(BlueprintGuid.Parse(HexSelectionId)) as BlueprintFeatureSelection;
            if (pool == null || hexes.Any(h => pool.AllFeatures.Count(x => x.AssetGuid == h.AssetGuid) != 1))
                throw new InvalidOperationException("0119 shaman hex registration failed.");
            // The temporary-grant pools surface the hexes through their fact list;
            // a hex the game published but Wandering Hex cannot offer is a defect,
            // so the fact list is audited too instead of only the main selection.
            foreach (var id in ApplyFactPoolIds)
            {
                var anchor = cache.Load(BlueprintGuid.Parse(id)) as BlueprintScriptableObject;
                if (anchor == null) throw new InvalidOperationException("0119 SpiritTools anchor missing: " + id);
                var granted = new HashSet<BlueprintGuid>();
                foreach (var component in anchor.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                {
                    if (component == null) continue;
                    var field = AccessTools.Field(component.GetType(), "m_Facts");
                    if (field?.GetValue(component) is Array array)
                        foreach (var item in array)
                            if (item is BlueprintReferenceBase reference) granted.Add(reference.Guid);
                }
                if (hexes.Any(h => !granted.Contains(h.AssetGuid)))
                    throw new InvalidOperationException("0119 hex missing from a temporary-grant pool: " + id);
            }
        }

        static void Reconcile(BlueprintsCache cache)
        {
            var hexPool = cache.Load(BlueprintGuid.Parse(HexSelectionId)) as BlueprintFeatureSelection
                ?? throw new InvalidOperationException("0119 shaman hex selection missing.");
            var existing = Specs.Select(s => "ShamanHex" + s.Name.Replace("'", "").Replace(" ", "").Replace("-", "") + "0119")
                .Select(t => cache.Load(Id(t)) as BlueprintFeature).Where(h => h != null).ToArray();
            T09CombatFeats.Append(new[] { hexPool }, existing);
            Connect(ApplyFactPoolIds.Select(id => cache.Load(BlueprintGuid.Parse(id)) as BlueprintScriptableObject
                ?? throw new InvalidOperationException("0119 SpiritTools anchor missing: " + id)).ToArray(), existing);
            Verify(cache, existing);
        }
    }

    [TypeId("0119a1b2c3d4e5f60718293a4b5c0001")]
    public sealed class ShamanCrashingWaves0119 : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IInitiatorRulebookHandler<RuleDealDamage>, IInitiatorRulebookSubscriber
    {
        public BlueprintCharacterClassReference Shaman;
        public BlueprintBuffReference Prone;

        int Level()
        {
            var cls = Shaman?.Get();
            return cls == null || Owner == null ? 0 : Owner.Progression.GetClassLevel(cls);
        }

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            var spell = evt.Spell;
            if (spell == null) return;
            int level = Level();
            if (Water(spell)) evt.AddBonusCasterLevel(level >= 8 ? 2 : 1);
            else if (Cold(spell) && level >= 8) evt.AddBonusCasterLevel(2);
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }

        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (evt.Initiator != Owner || evt.Target == null) return;
            var spell = evt.SourceAbility;
            if (spell == null) return;
            int level = Level();
            // Below 16th the knockdown is limited to the hex's own water spells,
            // which here means cold, name-matched water spells and any spell
            // carrying CotW's Water marker.
            if (level < 16 && !Water(spell) && !Cold(spell)) return;
            int dc = 10 + level / 2 + Math.Max(0, Owner.Stats.Wisdom.Bonus);
            var save = Rulebook.Trigger(new RuleSavingThrow(evt.Target, SavingThrowType.Fortitude, dc));
            if (save.IsPassed) return;
            var prone = Prone?.Get();
            if (prone == null) return;
            var context = evt.Reason?.Context ?? Fact?.MaybeContext;
            if (context == null) return; // an empty parent context crashes downstream buff patches
            var applied = evt.Target.Descriptor.AddBuff(prone, context, TimeSpan.FromSeconds(6));
            if (applied != null) applied.IsNotDispelable = true;
        }

        public void OnEventDidTrigger(RuleDealDamage evt) { }

        // Kingmaker had no Water descriptor, so original CotW tagged water spells
        // with its own bit (AdditionalSpellDescriptors.ExtraSpellDescriptor.Water,
        // 0x0010000000000000). Wrath ships no such marker, so a spell counts as
        // water when the CotW bit (or a native descriptor literally named Water)
        // is set, or when its name looks like water. The cold descriptor is
        // handled next to it: cold spells never carried CotW's bit, but the hex
        // treats them as water too, both for the caster level and the knockdown.
        const long CotWWaterBit = 0x0010000000000000L;
        static readonly SpellDescriptor WaterMarker = ResolveWaterMarker();

        static SpellDescriptor ResolveWaterMarker()
        {
            foreach (var name in Enum.GetNames(typeof(SpellDescriptor)))
                if (string.Equals(name, "Water", StringComparison.OrdinalIgnoreCase))
                    return (SpellDescriptor)Enum.Parse(typeof(SpellDescriptor), name);
            var marker = (SpellDescriptor)CotWWaterBit;
            // If the game already uses that bit for something else, claiming it
            // would turn unrelated spells into water spells.
            return Enum.IsDefined(typeof(SpellDescriptor), marker) ? SpellDescriptor.None : marker;
        }

        static SpellDescriptor Descriptor(BlueprintAbility spell) =>
            spell.GetComponent<SpellDescriptorComponent>()?.Descriptor ?? SpellDescriptor.None;

        static bool Cold(BlueprintAbility spell) => (Descriptor(spell) & SpellDescriptor.Cold) != 0;

        static bool Water(BlueprintAbility spell) =>
            (Descriptor(spell) & WaterMarker) != 0 || WaterByName(spell);

        static bool WaterByName(BlueprintAbility spell)
        {
            var name = spell.name ?? "";
            return name.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Wave", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Hydraulic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Tsunami", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    // Original CotW ignored the weather movement penalty instead of granting a
    // flat speed bonus: a postfix on UnitEntityData.CalculateSpeedModifier
    // doubles the already-halved result back to normal while the weather is bad
    // and the unit carries this feature. Ported as-is, with no speed bonus.
    [TypeId("0119a1b2c3d4e5f60718293a4b5c0003")]
    public sealed class ShamanStormwalker0119 : UnitFactComponentDelegate
    {
        internal static bool Active(UnitEntityData unit) =>
            unit?.Facts?.List.Any(f => f.IsTurnedOn && !f.IsDisposed &&
                f.Blueprint.GetComponent<ShamanStormwalker0119>() != null) == true;
    }

    [HarmonyPatch(typeof(UnitEntityData), "CalculateSpeedModifier")]
    internal static class ShamanStormwalkerSpeed0119
    {
        static void Postfix(UnitEntityData __instance, ref float __result)
        {
            // Only the weather slowdown needs undoing; anything else that leaves
            // the unit at or below half speed is somebody else's business.
            if (__result > 0.51f) return;
            if (!ShamanStormwalker0119.Active(__instance)) return;
            if (!BadWeather()) return;
            __result *= 2f;
        }

        // ActualWeather and SlowdownBonusBeginsOn are InclemencyType values from
        // Owlcat.Runtime.Visual, which the main project does not reference, so
        // both are read reflectively and compared as integers.
        static bool BadWeather()
        {
            var weather = Kingmaker.Game.Instance?.Player?.Weather;
            var actual = weather == null ? null : AccessTools.Property(weather.GetType(), "ActualWeather")?.GetValue(weather);
            var settings = Kingmaker.Blueprints.Root.BlueprintRoot.Instance?.WeatherSettings;
            var begins = settings == null ? null
                : AccessTools.Property(settings.GetType(), "SlowdownBonusBeginsOn")?.GetValue(settings)
                  ?? AccessTools.Field(settings.GetType(), "SlowdownBonusBeginsOn")?.GetValue(settings);
            if (actual == null || begins == null) return false;
            return Convert.ToInt32(actual) >= Convert.ToInt32(begins);
        }
    }

    [Serializable]
    public sealed class ShamanChargeState0119
    {
        [Newtonsoft.Json.JsonProperty] public int Left;
        [Newtonsoft.Json.JsonProperty] public bool Ready;
    }

    [TypeId("0119a1b2c3d4e5f60718293a4b5c0002")]
    public sealed class ShamanMistCharges0119 : UnitFactComponentDelegate<ShamanChargeState0119>,
        ITargetRulebookHandler<RuleAttackWithWeapon>, ITargetRulebookSubscriber
    {
        const string ShamanGuid = "145f1d3d360a7ad48bd95d392c81b38e";

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (evt.Target != Owner || evt.AttackRoll == null || evt.AttackRoll.IsHit || evt.AttackRoll.IsFake) return;
            if (evt.AttackRoll.Result.ToString().IndexOf("Conceal", StringComparison.Ordinal) < 0) return;
            if (!Data.Ready)
            {
                var caster = Fact?.MaybeContext?.MaybeCaster;
                var cls = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(ShamanGuid);
                int level = caster == null || cls == null ? 0 : caster.Progression.GetClassLevel(cls);
                Data.Left = level >= 16 ? 3 : level >= 8 ? 2 : 1;
                Data.Ready = true;
            }
            if (Data.Left <= 0) return;
            Data.Left--;
            if (Data.Left == 0 && Fact is Kingmaker.UnitLogic.Buffs.Buff buff) buff.Remove();
        }
    }
}
