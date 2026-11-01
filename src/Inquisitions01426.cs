using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Properties;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Inquisitions01426
    {
        internal const string ModuleName = "Inquisitions: Conversion, Valor, Persistence";
        internal static readonly string[] PoolIds = { "48525e5da45c9c243a343fc6545dbdb9", "43281c3d7fe18cc4d91928395837cd1e" };
        internal static readonly string[] ClassIds = { "67819271767a9dd4fbfd4ae700befea0", "f1a70d9e1b0b41e49874e1fa9052a1ce" };
        internal static readonly string[] Tokens = { "ConversionInquisition", "ValorInquisition", "PersistenceInquisition" };
        internal static ContextValue Rank(AbilityRankType type = AbilityRankType.Default) => new ContextValue { ValueType = ContextValueType.Rank, ValueRank = type };
        internal static ContextRankConfig Scaling(BlueprintCharacterClass[] classes, AbilityRankType type, bool half) {
            var rank = new ContextRankConfig(); Set(rank, "m_Type", type); Set(rank, "m_BaseValueType", ContextRankBaseValueType.ClassLevel);
            Set(rank, "m_Class", classes.Select(Ref<BlueprintCharacterClassReference>).ToArray());
            Set(rank, "m_Progression", half ? ContextRankProgression.Div2 : ContextRankProgression.AsIs); Set(rank, "m_DisableRankBonus", true); return rank;
        }
        internal static BlueprintComponent[] Parameters(PsychicAssets f, string token, BlueprintCharacterClass[] classes, StatType stat, BlueprintArchetype archetype = null) {
            ContextValue Property(bool half) {
                var getter = new SummClassLevelGetter { Settings = new PropertySettings() };
                Set(getter, "m_Class", classes.Select(Ref<BlueprintCharacterClassReference>).ToArray());
                Set(getter, "Archetype", archetype == null ? new BlueprintArchetypeReference() : Ref<BlueprintArchetypeReference>(archetype));
                Set(getter, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
                SetEnum(getter.Settings, "m_Progression", half ? "Div2" : "AsIs");
                var property = f.Add(new BlueprintUnitProperty { ComponentsArray = new BlueprintComponent[] { getter } }, token + (half ? "HalfLevelProperty" : "LevelProperty"), "", "");
                var value = new ContextValue { ValueType = ContextValueType.CasterCustomProperty }; Set(value, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(property)); return value;
            }
            return new BlueprintComponent[] { Scaling(classes, AbilityRankType.Default, false), Scaling(classes, AbilityRankType.StatBonus, true),
                new ContextCalculateAbilityParams { StatType = stat, ReplaceCasterLevel = true, CasterLevel = Property(false), ReplaceSpellLevel = true, SpellLevel = Property(true) } };
        }
        static BlueprintAbilityResource Resource(PsychicAssets f, string token, string name, bool wisdom) {
            var r = f.Add(new BlueprintAbilityResource(), token, name, name); var amount = Get(r, "m_MaxAmount");
            Set(amount, "BaseValue", wisdom ? 3 : 1); Set(amount, "IncreasedByStat", wisdom); Set(amount, "ResourceBonusStat", StatType.Wisdom);
            foreach (string field in new[] { "m_Class", "m_ClassDiv" }) Set(amount, field, Array.Empty<BlueprintCharacterClassReference>());
            foreach (string field in new[] { "m_Archetypes", "m_ArchetypesDiv" }) Set(amount, field, Array.Empty<BlueprintArchetypeReference>());
            Set(r, "m_MaxAmount", amount); Set(r, "m_Min", 1); r.IsDomainAbility = true;
            r.LocalizedName = Text(token, "name", name); r.LocalizedDescription = Text(token, "description", name); return r;
        }
        static AddAbilityResources Grant(BlueprintAbilityResource r) { var c = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true }; Set(c, "m_Resource", Ref<BlueprintAbilityResourceReference>(r)); return c; }
        static AbilityResourceLogic Cost(BlueprintAbilityResource r) { var c = new AbilityResourceLogic { Amount = 1 }; Set(c, "m_IsSpendResource", true); Set(c, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(r)); return c; }
        static LevelEntry Entry(int level, params BlueprintFeatureBase[] features) {
            var entry = new LevelEntry { Level = level };
            Set(entry, "m_Features", features.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            foreach (var feature in features.OfType<BlueprintFeature>()) feature.IsClassFeature = true;
            return entry;
        }
        static void Icon(BlueprintUnitFact target, BlueprintUnitFact source) => Set(target, "m_Icon", Get(source, "m_Icon"));
        static BlueprintFeature AbilityFeature(PsychicAssets f, string token, BlueprintAbility ability, BlueprintAbilityResource resource) {
            var feature = f.Feature(token, token, "Grants the associated inquisition ability and its daily uses.", Facts(ability), Grant(resource)); feature.IsClassFeature = true;
            Set(feature, "m_DisplayName", Get(ability, "m_DisplayName")); Set(feature, "m_Description", Get(ability, "m_Description"));
            Icon(feature, ability); return feature;
        }
        static BlueprintProgression Progression(PsychicAssets f, BlueprintCharacterClass[] classes, string token, string name, string text, LevelEntry[] entries, params string[] deities) {
            var p = f.Add(new BlueprintProgression { Ranks = 1, IsClassFeature = true, Groups = new[] { FeatureGroup.Domain }, GiveFeaturesForPreviousLevels = true,
                LevelEntries = entries, ComponentsArray = deities.Select(id => (BlueprintComponent)Migration01426.Require(f.Native<BlueprintFeature>(id), true)).ToArray()
            }, token, name, text + " Uses cleric and inquisitor levels. An inquisition replaces a domain and grants no domain spells or spell slots.");
            Set(p, "m_Classes", classes.Select(c => new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(c) }).ToArray());
            var once = new PrerequisiteNoFeature(); Set(once, "m_Feature", Ref<BlueprintFeatureReference>(p)); p.ComponentsArray = p.ComponentsArray.Concat(new[] { once }).ToArray();
            Icon(p, entries[0].Features[0]); return p;
        }
        internal static void Register(BlueprintsCache cache) {
            var f = new PsychicAssets(cache); var classes = ClassIds.Select(f.Native<BlueprintCharacterClass>).ToArray();
            var pools = PoolIds.Select(f.Native<BlueprintFeatureSelection>).ToArray();
            var roots = new[] { Conversion(f, classes), Valor(f, classes), Persistence(f, classes) };
            Migration01426.Commit(f, pools, roots);
        }
        static BlueprintProgression Conversion(PsychicAssets f, BlueprintCharacterClass[] classes) {
            var charm = f.Feature("ConversionCharmOfWisdom", "Charm of Wisdom", "Use Wisdom instead of Charisma for Persuasion checks, including bluff, diplomacy and intimidation.",
                new ReplaceStatBaseAttribute { TargetStat = StatType.SkillPersuasion, BaseAttributeReplacement = StatType.Wisdom, ReplaceIfHigher = false });
            var dominate = f.Native<BlueprintAbility>("d7cbd2004ce66a042aeab2e95a3c5c61");
            var effect = dominate.GetComponents<AbilityEffectRunAction>().FirstOrDefault() ?? throw new InvalidOperationException("Native Dominate Person action is missing.");
            var nativeApply = effect.Actions.Actions.OfType<ContextActionConditionalSaved>().SelectMany(a => a.Failed.Actions).OfType<ContextActionApplyBuff>().FirstOrDefault()
                ?? effect.Actions.Actions.OfType<ContextActionApplyBuff>().FirstOrDefault();
            if (nativeApply?.Buff == null) throw new InvalidOperationException("Native Dominate Person buff action is missing.");
            var resource = Resource(f, "ConversionSwayingWordUses", "Swaying Word", false);
            var ability = f.Clone(dominate, "ConversionSwayingWord", "Swaying Word", "At level 8, once per day as a standard action, dominate one humanoid within line of sight for 1 minute (Will negates). Caster and target must be outside silence. DC 10 + half your cleric/inquisitor level + Wisdom modifier.");
            ability.Type = AbilityType.SpellLike; ability.AvailableMetamagic = 0; ability.ActionType = UnitCommand.CommandType.Standard;
            ability.ComponentsArray = ability.ComponentsArray.Where(c => c is AbilitySpawnFx || c is SpellDescriptorComponent || c.GetType().Namespace == "Kingmaker.UnitLogic.Abilities.Components.TargetCheckers")
                .Concat(Parameters(f, "ConversionSwayingWord", classes, StatType.Wisdom)).Concat(new BlueprintComponent[] { new AudibleAbilityRestriction(), Cost(resource), Save(SavingThrowType.Will, Saved(Array.Empty<Kingmaker.ElementsSystem.GameAction>(), new Kingmaker.ElementsSystem.GameAction[] { Apply(nativeApply.Buff, 1, DurationRate.Minutes) })) }).ToArray();
            ability.LocalizedDuration = Text(ability.name, "duration", "1 minute"); ability.LocalizedSavingThrow = Text(ability.name, "save", "Will negates");
            var word = AbilityFeature(f, "ConversionSwayingWordFeature", ability, resource); Icon(charm, dominate);
            return Progression(f, classes, Tokens[0], "Conversion Inquisition", "Any deity. Level 1: Charm of Wisdom. Level 8: Swaying Word, once per day.", new[] { Entry(1, charm), Entry(8, word) });
        }
        static BlueprintProgression Valor(PsychicAssets f, BlueprintCharacterClass[] classes) {
            var source = f.Native<BlueprintAbility>("55a037e514c0ee14a8e3ed14b47061de");
            if (!source.GetComponents<AbilityEffectRunAction>().Any()) throw new InvalidOperationException("Native Remove Fear action is missing.");
            var resource = Resource(f, "ValorTouchOfResolveUses", "Touch of Resolve", true);
            var touch = f.Clone(source, "ValorTouchOfResolve", "Touch of Resolve", "As a standard action, use remove fear on one touched creature. You can use this ability 3 + your Wisdom modifier times per day (minimum 1).");
            touch.Type = AbilityType.SpellLike; touch.AvailableMetamagic = 0; touch.Range = AbilityRange.Touch; touch.ActionType = UnitCommand.CommandType.Standard;
            touch.CanTargetSelf = touch.CanTargetFriends = true; touch.CanTargetEnemies = touch.CanTargetPoint = false;
            touch.ComponentsArray = touch.ComponentsArray.Where(c => c is AbilityEffectRunAction || c is AbilitySpawnFx)
                .Concat(Parameters(f, "ValorTouchOfResolve", classes, StatType.Wisdom)).Concat(new BlueprintComponent[] { Cost(resource) }).ToArray();
            var feature = AbilityFeature(f, "ValorTouchOfResolveFeature", touch, resource);
            var immunity = f.Native<BlueprintBuff>("993a5300cc84fde4bb4df441bf92d701");
            if (immunity.ComponentsArray.Length == 0) throw new InvalidOperationException("Native fear immunity components missing.");
            var fearless = f.Feature("ValorFearless", "Fearless", "At level 8, you become immune to fear.", CopyData(immunity.ComponentsArray)); Icon(fearless, immunity);
            return Progression(f, classes, Tokens[1], "Valor Inquisition", "Cayden Cailean, Erastil, Iomedae or Sarenrae. Level 1: Touch of Resolve. Level 8: immunity to fear.", new[] { Entry(1, feature), Entry(8, fearless) },
                "300e212868bca984687c92bcb66d381b", "afc775188deb7a44aa4cbde03512c671", "88d5da04361b16746bf5b65795e0c38c", "c1c4f7f64842e7e48849e5e67be11a1b");
        }
        static BlueprintProgression Persistence(PsychicAssets f, BlueprintCharacterClass[] classes) {
            var combat = f.Native<BlueprintFeature>("0f8939ae6f220984e8fb568abbdfba95");
            var bonus = f.Feature("PersistenceCombatReflexes", "Combat Reflexes", "Gain Combat Reflexes as a bonus feat.", Facts(combat)); Icon(bonus, combat);
            var speed = f.Native<BlueprintBuff>("035ed56eb973f0e469a288ff5991c9ff"); var speedResource = Resource(f, "PersistenceRelentlessFootingUses", "Relentless Footing", true);
            var speedAction = Apply(speed, 1, fromSpell: false); speedAction.IsNotDispelable = true;
            var footing = f.Ability("PersistenceRelentlessFooting", "Relentless Footing", "As a swift action, gain a +10-foot enhancement bonus to land speed for 1 round. Uses per day: 3 + Wisdom modifier (minimum 1).", AbilityType.Extraordinary, UnitCommand.CommandType.Swift, AbilityRange.Personal, Run(speedAction), Cost(speedResource));
            Icon(footing, speed); var footFeature = AbilityFeature(f, "PersistenceRelentlessFootingFeature", footing, speedResource);
            var healingResource = Resource(f, "PersistenceInnerStrengthUses", "Inner Strength", false);
            var healingIcon = f.Native<BlueprintAbility>("8d6073201e5395d458b8251386d72df1");
            var conditions = new[] { SpellDescriptor.Blindness, SpellDescriptor.Confusion, SpellDescriptor.Frightened, SpellDescriptor.Nauseated, SpellDescriptor.Shaken, SpellDescriptor.Sickened, SpellDescriptor.Staggered };
            var variants = conditions.Select(condition => {
                var a = f.Ability("PersistenceInnerStrength" + condition, "Inner Strength: " + condition,
                    "At level 6, once per day as a swift action, heal yourself for 1d6 per two cleric/inquisitor levels and remove the chosen condition from yourself.", AbilityType.Supernatural, UnitCommand.CommandType.Swift, AbilityRange.Personal,
                    Run(new ContextActionHealTarget { Value = new ContextDiceValue { DiceType = DiceType.D6, DiceCountValue = Rank(), BonusValue = 0 } }, new ContextActionRemoveBuffsByDescriptor { SpellDescriptor = condition }), Cost(healingResource), Scaling(classes, AbilityRankType.Default, true));
                Icon(a, healingIcon); return a;
            }).ToArray();
            var variantComponent = new AbilityVariants(); Set(variantComponent, "m_Variants", variants.Select(Ref<BlueprintAbilityReference>).ToArray());
            var wrapper = f.Ability("PersistenceInnerStrength", "Inner Strength", "At level 6, heal yourself and remove one chosen condition. All seven choices share one use per day; healing is 1d6 per two cleric/inquisitor levels.", AbilityType.Supernatural, UnitCommand.CommandType.Swift, AbilityRange.Personal, variantComponent);
            Icon(wrapper, healingIcon); foreach (var a in variants) Set(a, "m_Parent", Ref<BlueprintAbilityReference>(wrapper));
            var inner = AbilityFeature(f, "PersistenceInnerStrengthFeature", wrapper, healingResource);
            return Progression(f, classes, Tokens[2], "Persistence Inquisition", "Asmodeus, Iomedae or Urgathoa. Level 1: Combat Reflexes and Relentless Footing. Level 6: Inner Strength.", new[] { Entry(1, bonus, footFeature), Entry(6, inner) },
                "a3a5ccc9c670e6f4ca4a686d23b89900", "88d5da04361b16746bf5b65795e0c38c", "812f6c07148088e41a9ac94b56ac2fc8");
        }
    }
}
