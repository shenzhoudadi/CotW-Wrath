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
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
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
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.ResourceLinks;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class LoreSpirit0118
    {
        internal const string ModuleName = "Lore Spirit";
        internal const string RootToken = "LoreSpirit0118";
        const string ShamanId = "145f1d3d360a7ad48bd95d392c81b38e";
        const string WizardListId = "ba0401fdeb4062f40a7aa95b6f07fe89";
        const string SpiritSelectionId = "00c8c566d1825dd4a871250f35285982";
        const string SecondSpiritSelectionId = "2faa80662a56ab644aec2f875a68597f";
        const string HexSelectionId = "4223fe18c75d4d14787af196a04e14e7";
        const string WishProviderId = "f2c67a37-d726-4fd7-a242-50ed1e0ff184";
        const string ParametrizedTemplateId = "bcd757ac2aeef3c49b77e5af4e510956";
        static readonly string[] SpiritSpellIds = { "2c38da66e5a599347ac95b3294acbe00", "f0455c9295b53904f9e02fc571dd2ce1", "1a045f845778dc54db1c2be33a8c3c0a", "e9cc9378fd6841f48ad59384e79e9953", "4cf3d0fae3239ec478f51e86f49161cb", "9f5ada581af3db4419b54db77f44e430", "fafd77c6bfa85c04ba31fdc1c962c914", "0e67fa8f011662c43934d486acc50253" };
        static readonly string[] ApplyFactPoolIds = { "bff5039c06a4a494b84bbca5c1cf93e5", "8e37166acde792f458c1c02a7392ab7c", "cf57e3971a4f8d340a200794ba586bf9", "4248be819e269724086d8f6b44fafd10", "49c4c45281805fc43aa66cd036825450", "6485fe813137acd41aa56df5a39e0195", "e7be24dd1cf591d4baecd9578705513d", "1c06611ab264115479d16c0ff3ab0446" };
        static readonly string[] AbilityGrantIds = { "709db7ec794271b43b9c416b520d765c", "88032ba214e97a442849ef0d15ce1485", "77e02067a4f59ba4793c79a34391a658" };

        internal static bool Register(BlueprintsCache cache)
        {
            var existing = cache.Load(Id(RootToken));
            if (existing != null)
            {
                if (!(existing is BlueprintProgression progression) || progression.name != "CotWWrathPsychic" + RootToken)
                    throw new InvalidOperationException("0118 Lore Spirit root GUID is occupied by an unexpected blueprint.");
                Reconcile(cache, progression);
                return true;
            }

            var f = new PsychicAssets(cache);
            var shaman = f.Native<BlueprintCharacterClass>(ShamanId);
            var shamanList = shaman.Spellbook?.SpellList
                ?? throw new InvalidOperationException("0118 Shaman spellbook or spell list is unavailable.");
            if (shamanList.SpellsByLevel == null || shamanList.SpellsByLevel.Length < 10)
                throw new InvalidOperationException("0118 Shaman spell list has an unexpected structure.");
            var wizardList = f.Native<BlueprintSpellList>(WizardListId);
            var spiritPool = f.Native<BlueprintFeatureSelection>(SpiritSelectionId);
            var secondPool = f.Native<BlueprintFeatureSelection>(SecondSpiritSelectionId);
            var hexPool = f.Native<BlueprintFeatureSelection>(HexSelectionId);
            var parametrizedTemplate = f.Native<BlueprintParametrizedFeature>(ParametrizedTemplateId);
            var spellList = BuildSpiritList(f);
            // Owlcat keeps the bonus-spell grant on a separate hidden level-1
            // feature; the progression itself lists the spells through
            // AddSpellsToDescription. Keeping AddSpecialSpellList off the visible
            // Monstrous Insight feature prevents the spell table from being
            // rendered inside that ability's description.
            var spellListFeature = f.Feature("LoreSpiritSpellListFeature0118", "", "", Special(shaman, spellList));
            spellListFeature.HideInUI = true;

            var monstrous = BuildMonstrousInsight(f, shaman);

            var enlightenment = BuildEnlightenment(f, shaman, shamanList, wizardList, parametrizedTemplate);
            var perfect = f.Feature("LorePerfectKnowledge0118", "Perfect Knowledge", "You gain a +10 competence bonus on Knowledge (Arcana), Knowledge (World), Lore (Nature), and Lore (Religion) checks.",
                Bonus(StatType.SkillKnowledgeArcana, 10), Bonus(StatType.SkillKnowledgeWorld, 10), Bonus(StatType.SkillLoreNature, 10), Bonus(StatType.SkillLoreReligion, 10));
            perfect.IsClassFeature = true;

            var manifestation = BuildManifestation(f, shaman);
            // Native and ExpandedContent spirits never spell out their bonus spells
            // or hexes in the description text. Two description components append
            // those sections dynamically, with the same formatting and links as
            // every Owlcat spirit. The spirit's own text stays purely thematic.
            const string rootText = "A shaman who selects the lore spirit appears far wiser and knowing that her age would suggest. Though she can seem unassuming, her eyes give the impression she is peering deep into all she looks at, seeing the secrets of the essential merely by concentrating.";
            var hexDescription = new AddFeaturesFromSelectionToDescription {
                Introduction = Text("LoreSpirit0118", "hexIntro", "Additional Hexes:"),
                OnlyIfRequiresThisFeature = true
            };
            Set(hexDescription, "m_FeatureSelection", Ref<BlueprintFeatureSelectionReference>(hexPool));
            var spellDescription = new AddSpellsToDescription {
                Introduction = Text("LoreSpirit0118", "spellIntro", "Bonus Spells:")
            };
            Set(spellDescription, "m_SpellLists", new[] { Ref<BlueprintSpellListReference>(spellList) });
            if (AccessTools.Field(typeof(AddSpellsToDescription), "m_Spells") != null)
                Set(spellDescription, "m_Spells", Array.Empty<BlueprintAbilityReference>());
            // Reuse Owlcat's own localized section headers ("额外巫术：" / "额外法术：")
            // so a localized client renders Lore exactly like a native spirit.
            // Failure is non-fatal: the English fallback strings above remain.
            ReuseNativeIntroductions(cache, hexDescription, spellDescription);
            var root = f.Add(new BlueprintProgression { Ranks = 1, IsClassFeature = true,
                Groups = new[] { FeatureGroup.ShamanSpirit },
                LevelEntries = new[] { Entry(1, monstrous, spellListFeature), Entry(8, enlightenment[0], enlightenment[1]), Entry(10, enlightenment[2]), Entry(12, enlightenment[3]), Entry(14, enlightenment[4]), Entry(16, enlightenment[5], perfect), Entry(18, enlightenment[6]), Entry(20, enlightenment[7], manifestation) },
                UIGroups = Array.Empty<UIGroup>(),
                ComponentsArray = new BlueprintComponent[] { hexDescription, spellDescription } }, RootToken, "Lore Spirit", rootText);
            Set(root, "m_Icon", Get(f.Native<BlueprintAbility>(WishProviderId), "m_Icon"));
            Set(root, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(shaman) } });
            Set(root, "m_Archetypes", Array.Empty<BlueprintProgression.ArchetypeWithLevel>());
            Set(root, "m_UIDeterminatorsGroup", Array.Empty<BlueprintFeatureBaseReference>());
            Set(root, "m_AllowNonContextActions", false);

            var wisdomIcon = f.Native<BlueprintAbility>("f0455c9295b53904f9e02fc571dd2ce1");
            var wisdom = Hex(f, root, "LoreBenefitOfWisdom0118", "Benefit of Wisdom", "Use Wisdom instead of Intelligence for Knowledge (Arcana) and Knowledge (World) checks.",
                new ReplaceStatBaseAttribute { TargetStat = StatType.SkillKnowledgeArcana, BaseAttributeReplacement = StatType.Wisdom, ReplaceIfHigher = false },
                new ReplaceStatBaseAttribute { TargetStat = StatType.SkillKnowledgeWorld, BaseAttributeReplacement = StatType.Wisdom, ReplaceIfHigher = false });
            Set(wisdom, "m_Icon", wisdomIcon.Icon);
            var brain = BuildAttackHex(f, root, shaman, true);
            var confusion = BuildAttackHex(f, root, shaman, false);
            var acuity = Hex(f, root, "LoreMentalAcuity0118", "Mental Acuity", "Gain a +1 inherent Intelligence bonus at levels 1-4, increasing by +1 at levels 5, 10, 15, and 20.",
                Rank(shaman, ContextRankProgression.OnePlusDivStep, 5), new AddContextStatBonus { Stat = StatType.Intelligence, Descriptor = ModifierDescriptor.Inherent, Value = RankValue(), Multiplier = 1 });
            // AddContextStatBonus resolves its rank only when the fact is applied.
            // Native scaling features (BardicKnowledge, MagicalInventionsFeature)
            // set ReapplyOnLevelUp so the modifier is recomputed after each level;
            // without it the bonus stays at the rank from the level the hex was taken.
            acuity.ReapplyOnLevelUp = true;
            Set(acuity, "m_Icon", f.Native<BlueprintAbility>("ae4d3ad6a8fda1542acf2e9bbc13d113").Icon);
            var hexes = new[] { wisdom, brain, confusion, acuity };

            foreach (var bp in f.Assets.Values) { PsychicRepairComponents.UniqueNames(bp); Set(bp, "m_AllElements", SpellBatch01447.Elements(bp.ComponentsArray, new HashSet<object>(IdentityComparer)).ToList()); if (bp is BlueprintAbility ability) SpellBatch01447.ResetAbilityCaches(ability); }
            AuditWish(manifestation);
            var assets = f.Assets.Values.ToArray();
            if (assets.Any(bp => cache.Load(bp.AssetGuid) != null)) throw new InvalidOperationException("0118 Lore Spirit private GUID set is partially occupied.");
            var selections = new[] { spiritPool, secondPool, hexPool };
            var selectionOld = selections.Select(s => new[] { Get(s, "m_Features"), Get(s, "m_AllFeatures") }).ToArray();
            var external = ResolveExternal(cache);
            var externalOld = external.Select(SnapshotReferences).ToArray();
            try
            {
                f.Enable(); foreach (var bp in assets) cache.AddCachedBlueprint(bp.AssetGuid, bp);
                T09CombatFeats.Append(new[] { spiritPool, secondPool }, new[] { root });
                T09CombatFeats.Append(new[] { hexPool }, hexes);
                ConnectExternal(external, root, hexes);
                Verify(cache, root, hexes);
                return true;
            }
            catch
            {
                for (int i = 0; i < selections.Length; i++) { Set(selections[i], "m_Features", selectionOld[i][0]); Set(selections[i], "m_AllFeatures", selectionOld[i][1]); }
                for (int i = 0; i < external.Length; i++) RestoreReferences(external[i], externalOld[i]);
                foreach (var bp in assets) if (ReferenceEquals(cache.Load(bp.AssetGuid), bp)) cache.RemoveCachedBlueprint(bp.AssetGuid);
                throw;
            }
        }

        static BlueprintSpellList BuildSpiritList(PsychicAssets f)
        {
            var spells = SpiritSpellIds.Select(f.Native<BlueprintAbility>).Concat(new[] { f.Native<BlueprintAbility>(Id(TimeStop01438.RootToken).ToString()) }).ToArray();
            return SpellBatch01447.List(f, "LoreSpiritSpellList0118", level => level == 0 ? Array.Empty<BlueprintAbility>() : new[] { spells[level - 1] });
        }

        static void ReuseNativeIntroductions(BlueprintsCache cache, object hexDescription, object spellDescription)
        {
            try
            {
                var donor = cache.Load(BlueprintGuid.Parse("26e81d83d13dbf6448b10019c802612d")) as BlueprintProgression;
                if (donor == null) return;
                var nativeHex = donor.GetComponent<AddFeaturesFromSelectionToDescription>();
                var nativeSpells = donor.GetComponent<AddSpellsToDescription>();
                if (nativeHex != null) CopyIntroduction(nativeHex, hexDescription);
                if (nativeSpells != null) CopyIntroduction(nativeSpells, spellDescription);
            }
            catch { /* English fallback introduction stays in place. */ }
        }
        static void CopyIntroduction(object source, object target)
        {
            var type = source.GetType();
            var property = AccessTools.Property(type, "Introduction");
            if (property != null && property.CanRead && property.CanWrite)
            {
                var value = property.GetValue(source, null);
                if (value != null) property.SetValue(target, value, null);
                return;
            }
            var field = AccessTools.Field(type, "Introduction");
            if (field == null) return;
            var fieldValue = field.GetValue(source);
            if (fieldValue != null) field.SetValue(target, fieldValue);
        }

        static AddSpecialSpellList Special(BlueprintCharacterClass cls, BlueprintSpellList list)
        {
            var c = new AddSpecialSpellList(); Set(c, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(cls)); Set(c, "m_SpellList", Ref<BlueprintSpellListReference>(list)); return c;
        }

        static BlueprintFeature BuildMonstrousInsight(PsychicAssets f, BlueprintCharacterClass shaman)
        {
            const string text = "As a standard action, the shaman identifies a creature and gains insight into its strengths and weaknesses. The identification check gains an insight bonus equal to her shaman level. Whether or not the check succeeds, for 1 minute she gains a +2 insight bonus on attack rolls against that creature and a +2 insight bonus to AC against its attacks. She can use this ability 3 + her Charisma modifier times per day.";
            var icon = f.Native<BlueprintFeature>("ee0b69e90bac14446a4cf9a050f87f2e");
            var resource = f.Add(new BlueprintAbilityResource(), "LoreMonstrousInsightResource0118", "Monstrous Insight", text);
            var amount = Get(resource, "m_MaxAmount");
            Set(amount, "BaseValue", 3); Set(amount, "IncreasedByStat", true); Set(amount, "ResourceBonusStat", StatType.Charisma);
            Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>()); Set(amount, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>()); Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            Set(resource, "m_MaxAmount", amount); Set(resource, "m_Min", 0);

            var mark = f.Buff("LoreMonstrousInsightBuff0118", "Monstrous Insight", text,
                new AttackBonusAgainstTarget { CheckCaster = true, Value = new ContextValue { ValueType = ContextValueType.Simple, Value = 2 } },
                new ACBonusAgainstTarget { CheckCaster = true, Descriptor = ModifierDescriptor.Insight, Value = new ContextValue { ValueType = ContextValueType.Simple, Value = 2 } });
            mark.Stacking = StackingType.Replace; Set(mark, "m_Icon", icon.Icon);
            var apply = Apply(mark, 1, DurationRate.Minutes, fromSpell: false); apply.IsNotDispelable = true;
            var rank = Rank(shaman, ContextRankProgression.AsIs, 0);
            var ability = f.Ability("LoreMonstrousInsightAbility0118", "Monstrous Insight", text,
                AbilityType.Supernatural, UnitCommand.CommandType.Standard, AbilityRange.Close,
                rank, Run(new LoreMonstrousInsightCheck0118 { Shaman = Ref<BlueprintCharacterClassReference>(shaman) }, apply),
                Cost(resource), new AbilitySpawnFx { PrefabLink = new PrefabLink { AssetId = "749bb96fb50ee5b4685645472d718465" }, Time = AbilitySpawnFxTime.OnApplyEffect, Anchor = AbilitySpawnFxAnchor.SelectedTarget, PositionAnchor = AbilitySpawnFxAnchor.None, OrientationAnchor = AbilitySpawnFxAnchor.None });
            ability.CanTargetSelf = false; ability.CanTargetFriends = true; ability.CanTargetEnemies = true; ability.SpellResistance = false;
            Set(ability, "m_Icon", icon.Icon);
            var feature = f.Feature("LoreMonstrousInsight0118", "Monstrous Insight", text, Facts(ability), Grant(resource));
            feature.IsClassFeature = true; Set(feature, "m_Icon", icon.Icon); return feature;
        }

        static BlueprintFeatureSelection[] BuildEnlightenment(PsychicAssets f, BlueprintCharacterClass shaman, BlueprintSpellList shamanList, BlueprintSpellList wizardList, BlueprintParametrizedFeature template)
        {
            var filtered = SpellBatch01447.List(f, "LoreArcaneEnlightenmentList0118", level => wizardList.SpellsByLevel.Where(x => x.SpellLevel == level).SelectMany(x => x.Spells)
                .Where(s => !shamanList.SpellsByLevel.SelectMany(x => x.Spells).Any(k => k.AssetGuid == s.AssetGuid)).Distinct().ToArray());
            var options = new BlueprintParametrizedFeature[9];
            for (int level = 1; level <= options.Length; level++)
            {
                var learn = new LearnSpellParametrized { SpecificSpellLevel = true, SpellLevel = level, SpellLevelPenalty = 0 };
                Set(learn, "m_SpellcasterClass", Ref<BlueprintCharacterClassReference>(shaman)); Set(learn, "m_SpellList", Ref<BlueprintSpellListReference>(filtered));
                var cast = new PrerequisiteClassSpellLevel { RequiredSpellLevel = level }; Set(cast, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(shaman));
                var param = f.Clone(template, "LoreArcaneEnlightenmentParameter" + level + "0118", "Arcane Enlightenment (Level " + level + ")", "Select one wizard or sorcerer spell not already on the shaman spell list.");
                param.ParameterType = FeatureParameterType.LearnSpell; param.SpecificSpellLevel = true; param.SpellLevel = level; param.SpellLevelPenalty = 0; param.Ranks = 1;
                Set(param, "m_SpellcasterClass", Ref<BlueprintCharacterClassReference>(shaman)); Set(param, "m_SpellList", Ref<BlueprintSpellListReference>(filtered));
                param.ComponentsArray = new BlueprintComponent[] { learn, cast, new PrerequisiteStatValue { Stat = StatType.Intelligence, Value = 10 + level }, new PrerequisiteStatValue { Stat = StatType.Charisma, Value = 10 + level } };
                options[level - 1] = param;
            }
            var refs = options.Select(Ref<BlueprintFeatureReference>).ToArray();
            var result = new BlueprintFeatureSelection[8];
            for (int i = 0; i < result.Length; i++)
            {
                var selection = f.Add(new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true }, "LoreArcaneEnlightenmentSelection" + (i + 1) + "0118", "Arcane Enlightenment", "Select one eligible arcane spell from levels 1 through 9, subject to spellcasting and ability-score prerequisites.");
                Set(selection, "m_Features", refs); Set(selection, "m_AllFeatures", refs); result[i] = selection;
            }
            return result;
        }

        static readonly string[] WishSpellLevelIds = {
            "16096ebf-c90b-463a-82c4-e587dc61cc57", "a2701100-7f81-430e-bb43-fae4e7c6732d",
            "9fae2634-f85d-4fa0-8a0a-a1ba10589bc1", "9836cd37-1c27-4891-8266-9709455a9389",
            "e8ede857-fa83-4cbd-9576-706e0b522621", "45727b35-cfc8-4ee5-bb54-221b8b34b5ee",
            "665cca33-4e19-4891-89b8-7de298d8a564", "63d267b2-0ec5-4a08-9501-6922b158f264"
        };

        static BlueprintFeature BuildManifestation(PsychicAssets f, BlueprintCharacterClass shaman)
        {
            var provider = f.Native<BlueprintAbility>(WishProviderId);
            if (provider.name != "EbonsContentModWishAbility") throw new InvalidOperationException("0118 Wish provider has an unexpected name.");
            var providerMenu = provider.GetComponent<AbilityVariants>() ?? throw new InvalidOperationException("0118 Wish provider root lacks AbilityVariants.");
            if (provider.GetComponents<AbilityVariants>().Count() != 1) throw new InvalidOperationException("0118 Wish provider has an unexpected number of root menus.");

            var providerVariants = providerMenu.Variants.ToArray();
            var expectedIds = new HashSet<BlueprintGuid>(WishSpellLevelIds.Select(BlueprintGuid.Parse));
            var safeSources = providerVariants.Where(v => v != null && expectedIds.Contains(v.AssetGuid)).ToArray();
            if (safeSources.Length != WishSpellLevelIds.Length || safeSources.Select(v => v.AssetGuid).Distinct().Count() != WishSpellLevelIds.Length)
                throw new InvalidOperationException("0118 Wish provider does not expose the expected eight spell-level branches.");
            foreach (var id in expectedIds)
                if (safeSources.Count(v => v.AssetGuid == id) != 1)
                    throw new InvalidOperationException("0118 Wish provider spell-level branch is missing or duplicated: " + id);

            var resource = f.Add(new BlueprintAbilityResource(), "LoreWishResource0118", "", "");
            var amount = Get(resource, "m_MaxAmount"); Set(amount, "BaseValue", 1); Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>()); Set(amount, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>()); Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>()); Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>()); Set(resource, "m_MaxAmount", amount);

            var wish = f.Clone(provider, "LorePrivateWish0118", provider.Name, provider.Description);
            wish.Type = AbilityType.SpellLike;
            wish.MaterialComponent = new BlueprintAbility.MaterialComponentData();
            wish.ComponentsArray = wish.ComponentsArray.Where(c => !(c is SpellListComponent) && !(c is AbilityResourceLogic) && !(c is AbilityVariants)).ToArray();

            var children = new BlueprintAbility[WishSpellLevelIds.Length];
            for (int i = 0; i < WishSpellLevelIds.Length; i++)
            {
                var source = safeSources.Single(v => v.AssetGuid == BlueprintGuid.Parse(WishSpellLevelIds[i]));
                var child = f.Clone(source, "LorePrivateWishSpellLevel" + (i + 1) + "0118", source.Name, source.Description);
                child.Type = AbilityType.SpellLike;
                child.MaterialComponent = new BlueprintAbility.MaterialComponentData();
                child.ComponentsArray = child.ComponentsArray.Where(c => !(c is SpellListComponent) && !(c is AbilityResourceLogic) && !(c is AbilityVariants)).Concat(new BlueprintComponent[] { Cost(resource) }).ToArray();
                Set(child, "m_Parent", Ref<BlueprintAbilityReference>(wish));
                children[i] = child;
            }
            var privateMenu = new AbilityVariants();
            Set(privateMenu, "m_Variants", children.Select(Ref<BlueprintAbilityReference>).ToArray());
            wish.ComponentsArray = wish.ComponentsArray.Concat(new BlueprintComponent[] { privateMenu }).ToArray();

            if (wish.MaterialComponent.Item != null || wish.MaterialComponent.Count != 0 ||
                children.Any(c => c.MaterialComponent.Item != null || c.MaterialComponent.Count != 0) ||
                children.Any(c => c.GetComponent<AbilityResourceLogic>() == null))
                throw new InvalidOperationException("0118 private Wish graph failed its material or resource audit.");

            var manifestation = f.Feature("LoreManifestation0118", "Manifestation", "Take 20 on all Knowledge and Lore checks, even when untrained, and use a private Wish entry once per day.",
                new LorePerfectKnowledgeChecks0118(), Facts(wish), Grant(resource));
            manifestation.IsClassFeature = true; return manifestation;
        }
        static void AuditWish(BlueprintFeature manifestation)
        {
            var wish = manifestation.GetComponent<AddFacts>()?.Facts.OfType<BlueprintAbility>().SingleOrDefault() ?? throw new InvalidOperationException("0118 private Wish grant missing.");
            if (wish.AssetGuid == BlueprintGuid.Parse(WishProviderId)) throw new InvalidOperationException("0118 shared Wish root was granted directly.");
            if (wish.GetComponent<AbilityResourceLogic>() != null) throw new InvalidOperationException("0118 private Wish root must not spend the resource.");
            var menu = wish.GetComponent<AbilityVariants>();
            var children = menu == null ? Array.Empty<BlueprintAbility>() : menu.Variants.ToArray();
            if (children.Length != WishSpellLevelIds.Length) throw new InvalidOperationException("0118 private Wish menu has an unexpected number of branches.");
            var costs = children.Select(c => c.GetComponents<AbilityResourceLogic>().ToArray()).ToArray();
            if (costs.Any(c => c.Length != 1) || costs.SelectMany(c => c).Any(c => c.Amount != 1) ||
                costs.SelectMany(c => c).Select(c => c.RequiredResource?.AssetGuid).Distinct().Count() != 1)
                throw new InvalidOperationException("0118 private Wish branches do not share exactly one one-use resource cost.");
        }

        static BlueprintFeature BuildAttackHex(PsychicAssets f, BlueprintProgression root, BlueprintCharacterClass shaman, bool brain)
        {
            string key = brain ? "BrainDrain" : "ConfusionCurse"; string title = brain ? "Brain Drain" : "Confusion Curse";
            string text = brain ? "A creature within 30 feet makes a Will save. On failure it takes 1d4 divine damage per two shaman levels, minimum 1d4. Whether or not it succeeds, that target cannot be selected again for 24 hours." : "A creature within 30 feet makes a Will save. On failure it is confused for a number of rounds equal to your Charisma modifier, minimum 1. Whether or not it succeeds, that target cannot be selected again for 24 hours.";
            var cooldown = f.Buff("Lore" + key + "Cooldown0118", title + " Cooldown", "This creature cannot be targeted by " + title + " again for 24 hours.");
            cooldown.Stacking = StackingType.Replace; SetEnum(cooldown, "m_Flags", "HiddenInUi, RemoveOnRest");
            var targetCheck = new AbilityTargetHasFact { Inverted = true, FromCaster = false };
            Set(targetCheck, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(cooldown) });
            var applyCooldown = Apply(cooldown, 1, DurationRate.Days, fromSpell: false); applyCooldown.Permanent = false; applyCooldown.IsNotDispelable = true;
            GameAction effect;
            var components = new List<BlueprintComponent> { targetCheck, Inquisitions01426.Parameters(f, "Lore" + key + "0118", new[] { shaman }, StatType.Wisdom).First(c => c is ContextCalculateAbilityParams) };
            BlueprintUnitFact icon;
            if (brain)
            {
                var rank = Rank(shaman, ContextRankProgression.Div2, 0); Set(rank, "m_Type", AbilityRankType.DamageBonus); components.Add(rank);
                effect = new ContextActionDealDamage { DamageType = new DamageTypeDescription { Type = DamageType.Direct }, Value = new ContextDiceValue { DiceType = DiceType.D4, DiceCountValue = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.DamageBonus }, BonusValue = 0 }, IgnoreCritical = true, DisableSneakDamage = true };
                icon = f.Native<BlueprintAbility>("eabf94e4edc6e714cabd96aa69f8b207");
                components.Add(new AbilitySpawnFx { PrefabLink = new Kingmaker.ResourceLinks.PrefabLink { AssetId = "cbfe312cb8e63e240a859efaad8e467c" }, Time = AbilitySpawnFxTime.OnApplyEffect, Anchor = AbilitySpawnFxAnchor.SelectedTarget, PositionAnchor = AbilitySpawnFxAnchor.None, OrientationAnchor = AbilitySpawnFxAnchor.None });
            }
            else
            {
                var duration = new ContextRankConfig(); Set(duration, "m_BaseValueType", ContextRankBaseValueType.StatBonus); Set(duration, "m_Stat", StatType.Charisma); Set(duration, "m_Progression", ContextRankProgression.AsIs); Set(duration, "m_UseMin", true); Set(duration, "m_Min", 1); Set(duration, "m_Type", AbilityRankType.StatBonus); components.Add(duration);
                var confusedBuff = f.Native<BlueprintBuff>("886c7407dc629dc499b9f1465ff382df");
                var confused = Apply(confusedBuff, 1, DurationRate.Rounds, fromSpell: false); confused.DurationValue.BonusValue = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.StatBonus }; effect = confused;
                icon = confusedBuff;
                var donorFx = f.Native<BlueprintAbility>("eabf94e4edc6e714cabd96aa69f8b207").GetComponent<AbilitySpawnFx>();
                components.Add(donorFx != null ? CopyData(donorFx) : new AbilitySpawnFx { PrefabLink = new Kingmaker.ResourceLinks.PrefabLink { AssetId = "cbfe312cb8e63e240a859efaad8e467c" }, Time = AbilitySpawnFxTime.OnApplyEffect, Anchor = AbilitySpawnFxAnchor.SelectedTarget, PositionAnchor = AbilitySpawnFxAnchor.None, OrientationAnchor = AbilitySpawnFxAnchor.None });
            }
            components.Add(Run(applyCooldown, new ContextActionSavingThrow { Type = SavingThrowType.Will, Actions = Actions(Saved(Array.Empty<GameAction>(), new[] { effect })) }));
            components.Add(new SpellDescriptorComponent { Descriptor = SpellDescriptor.MindAffecting });
            var ability = f.Ability("Lore" + key + "Ability0118", title, text, AbilityType.Supernatural, UnitCommand.CommandType.Standard, AbilityRange.Close, components.ToArray()); ability.SpellResistance = false; ability.LocalizedSavingThrow = Text(key, "save", "Will negates"); Set(ability, "m_Icon", icon.Icon);
            var feature = Hex(f, root, "Lore" + key + "0118", title, text, Facts(ability)); Set(feature, "m_Icon", icon.Icon); Set(cooldown, "m_Icon", icon.Icon); return feature;
        }

        static BlueprintFeature Hex(PsychicAssets f, BlueprintProgression root, string token, string name, string text, params BlueprintComponent[] components)
        {
            var need = new PrerequisiteFeature(); Set(need, "m_Feature", Ref<BlueprintFeatureReference>(root));
            var feature = f.Feature(token, name, text, components.Concat(new BlueprintComponent[] { need }).ToArray()); feature.IsClassFeature = true; feature.Groups = new[] { FeatureGroup.ShamanHex }; return feature;
        }
        static AddStatBonus Bonus(StatType stat, int value) => new AddStatBonus { Stat = stat, Value = value, Descriptor = ModifierDescriptor.Competence };
        static ContextValue RankValue() => new ContextValue { ValueType = ContextValueType.Rank };
        static ContextRankConfig Rank(BlueprintCharacterClass cls, ContextRankProgression progression, int step)
        {
            var rank = new ContextRankConfig(); Set(rank, "m_BaseValueType", ContextRankBaseValueType.ClassLevel); Set(rank, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(cls) }); Set(rank, "m_Progression", progression); if (step > 0) Set(rank, "m_StepLevel", step); Set(rank, "m_UseMin", true); Set(rank, "m_Min", 1); return rank;
        }
        static AddAbilityResources Grant(BlueprintAbilityResource r) { var c = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true }; Set(c, "m_Resource", Ref<BlueprintAbilityResourceReference>(r)); return c; }
        static AbilityResourceLogic Cost(BlueprintAbilityResource r) { var c = new AbilityResourceLogic { Amount = 1 }; Set(c, "m_IsSpendResource", true); Set(c, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(r)); return c; }
        static LevelEntry Entry(int level, params BlueprintFeatureBase[] features) { var entry = new LevelEntry { Level = level }; Set(entry, "m_Features", features.Select(Ref<BlueprintFeatureBaseReference>).ToList()); return entry; }

        static BlueprintScriptableObject[] ResolveExternal(BlueprintsCache cache)
        {
            return ApplyFactPoolIds.Concat(AbilityGrantIds).Select(id => cache.Load(BlueprintGuid.Parse(id)) as BlueprintScriptableObject ?? throw new InvalidOperationException("0118 SpiritTools integration anchor missing: " + id)).ToArray();
        }
        static Dictionary<FieldInfo, object> SnapshotReferences(BlueprintScriptableObject bp) => ReferenceFields(bp).ToDictionary(f => f, f => f.GetValue(bp));
        static void RestoreReferences(BlueprintScriptableObject bp, Dictionary<FieldInfo, object> snapshot) { foreach (var pair in snapshot) pair.Key.SetValue(bp, pair.Value); }
        static FieldInfo[] ReferenceFields(BlueprintScriptableObject bp) => bp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(f => f.FieldType.IsArray && typeof(BlueprintReferenceBase).IsAssignableFrom(f.FieldType.GetElementType())).ToArray();
        static void ConnectExternal(BlueprintScriptableObject[] external, BlueprintProgression root, BlueprintFeature[] hexes)
        {
            foreach (var bp in external)
            {
                foreach (var field in ReferenceFields(bp))
                {
                    var array = (Array)field.GetValue(bp) ?? Array.CreateInstance(field.FieldType.GetElementType(), 0);
                    var targets = ApplyFactPoolIds.Contains(bp.AssetGuid.ToString()) ? hexes.Cast<SimpleBlueprint>() : new SimpleBlueprint[] { root };
                    var list = array.Cast<BlueprintReferenceBase>().ToList();
                    foreach (var target in targets) if (!list.Any(r => r != null && r.Guid == target.AssetGuid)) list.Add((BlueprintReferenceBase)typeof(BlueprintReferenceBase).GetMethod("CreateTyped", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(field.FieldType.GetElementType()).Invoke(null, new object[] { target }));
                    var next = Array.CreateInstance(field.FieldType.GetElementType(), list.Count); for (int i = 0; i < list.Count; i++) next.SetValue(list[i], i); field.SetValue(bp, next);
                }
            }
        }
        static void Reconcile(BlueprintsCache cache, BlueprintProgression root)
        {
            var hexes = new[] { "LoreBenefitOfWisdom0118", "LoreBrainDrain0118", "LoreConfusionCurse0118", "LoreMentalAcuity0118" }.Select(t => cache.Load(Id(t)) as BlueprintFeature ?? throw new InvalidOperationException("0118 existing Lore Spirit graph is incomplete.")).ToArray();
            var pools = new[] { SpiritSelectionId, SecondSpiritSelectionId }.Select(id => cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection ?? throw new InvalidOperationException("0118 spirit selection missing.")).ToArray();
            T09CombatFeats.Append(pools, new[] { root }); T09CombatFeats.Append(new[] { cache.Load(BlueprintGuid.Parse(HexSelectionId)) as BlueprintFeatureSelection }, hexes); ConnectExternal(ResolveExternal(cache), root, hexes); Verify(cache, root, hexes);
        }
        static void Verify(BlueprintsCache cache, BlueprintProgression root, BlueprintFeature[] hexes)
        {
            foreach (var id in new[] { SpiritSelectionId, SecondSpiritSelectionId }) { var pool = cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection; if (pool == null || pool.AllFeatures.Count(x => x.AssetGuid == root.AssetGuid) != 1) throw new InvalidOperationException("0118 spirit selection registration failed: " + id); }
            var hexPool = cache.Load(BlueprintGuid.Parse(HexSelectionId)) as BlueprintFeatureSelection; if (hexPool == null || hexes.Any(h => hexPool.AllFeatures.Count(x => x.AssetGuid == h.AssetGuid) != 1)) throw new InvalidOperationException("0118 shaman hex registration failed.");
        }
    }

    [TypeId("0118b3806df34c85a110000000000003")]
    public sealed class LoreMonstrousInsightCheck0118 : ContextAction
    {
        public BlueprintCharacterClassReference Shaman;
        public override string GetCaption() => "Identify the target with Monstrous Insight";
        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            if (caster == null || Target.Unit == null) return;
            var skills = new[] { StatType.SkillKnowledgeArcana, StatType.SkillKnowledgeWorld, StatType.SkillLoreNature, StatType.SkillLoreReligion };
            var skill = skills.OrderByDescending(s => caster.Stats.GetStat(s)?.ModifiedValue ?? int.MinValue).First();
            int level = Shaman?.Get() == null ? 0 : caster.Progression.GetClassLevel(Shaman.Get());
            var check = new RuleSkillCheck(caster, skill, Math.Max(0, 10 - level)) { Reason = Context, ShowAnyway = true };
            Context.TriggerRule(check);
        }
    }

    [TypeId("0118a16f067f4da0a110000000000001")]
    public sealed class LorePerfectKnowledgeChecks0118 : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleRollD20>, IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleRollD20 evt)
        {
            var check = Rulebook.CurrentContext.PreviousEvent as RuleSkillCheck;
            if (check != null && check.Initiator == Owner && new[] { StatType.SkillKnowledgeArcana, StatType.SkillKnowledgeWorld, StatType.SkillLoreNature, StatType.SkillLoreReligion }.Contains(check.StatType))
            {
                var result = AccessTools.Field(typeof(RuleRollD20), "m_Result") ?? AccessTools.Field(typeof(RuleRollD20), "<Result>k__BackingField");
                if (result == null) throw new MissingFieldException(typeof(RuleRollD20).FullName, "m_Result");
                result.SetValue(evt, 20);
            }
        }
        public void OnEventDidTrigger(RuleRollD20 evt) { }
    }

}
