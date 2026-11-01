// Adapted from Holic75/KingmakerRebalance (MIT), Witch.createHedgeWitch.
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class HedgeWitch01443
    {
        internal const string ModuleName = "Witch archetype: Hedge Witch";
        internal const string RootToken = "HedgeWitchArchetype01443", HealingToken = "HedgeWitchSpontaneousHealing01443", EmpathyToken = "HedgeWitchEmpathicHealing01443";
        internal static readonly string[] OwnTokens = { RootToken, HealingToken, EmpathyToken };
        internal const string CureFeatureId = "5e4620cea099c9345a9207c11d7bc916", EmpathyIconId = "f6f95242abdfac346befd6f4f6222140";
        // Preserve the actual CotW Witch-specific table, including Moderate at
        // both levels 2 and 3. Do not substitute the different cleric table.
        internal static readonly string[] CureIds = { null, "5590652e1c2225c4ca30c4a699ab3649", "6b90c773a6543dc49b2505858ce33db5", "6b90c773a6543dc49b2505858ce33db5", "3361c5df793b4c8448756146a88026ad", "41c9016596fe1de4faf67425ed691203", "5d3d689392e4ff740a761ef346815074", "571221cc141bc21449ae96b3944652aa", "0cea35de4d553cc439ae80b3a8724397", "1f173a16120359e41a20fc75bb53d449" };
        const string Description = "A hedge witch devotes herself to healing. Spontaneous Healing at level 1 converts prepared witch spells into cure spells without learning or preparing them. Empathic Healing at level 8 grants +3 to Lore (Religion), following the original CotW implementation. Replaces only the hex choices at levels 4 and 8; retains the familiar, patron, prepared spellbook, spell progression and other hexes.";
        sealed class Receipt { internal BlueprintScriptableObject[] Assets; internal string[] Definitions; internal BlueprintCharacterClass Witch; internal BlueprintProgression Progression; internal BlueprintArchetype Root; internal UIGroup Group; }
        static readonly ConditionalWeakTable<BlueprintsCache, Receipt> Published = new ConditionalWeakTable<BlueprintsCache, Receipt>();
        static LevelEntry Entry(int n, BlueprintFeature feature) { var e = new LevelEntry { Level = n }; Set(e, "m_Features", new[] { Ref<BlueprintFeatureBaseReference>(feature) }.ToList()); return e; }
        static string Definition(BlueprintScriptableObject bp)
        {
            object data;
            if (bp is BlueprintArchetype a) data = new object[] { a.GetParentClass(), a.AddFeatures.Select(e => new object[] { e.Level, Get(e, "m_Features") }).ToArray(), a.RemoveFeatures.Select(e => new object[] { e.Level, Get(e, "m_Features") }).ToArray(), a.RemoveSpellbook, a.ReplaceClassSkills, a.ChangeCasterType, a.AddSkillPoints };
            else if (bp is BlueprintFeature f) data = new object[] { f.Ranks, f.Groups, f.IsClassFeature, f.HideInCharacterSheetAndLevelUp };
            else throw new InvalidOperationException("Unexpected Hedge Witch resource type.");
            return Migration01426.Definition(new object[] { bp.name, bp.ComponentsArray, data });
        }
        static void Connect(Receipt r)
        {
            var before = Get(r.Witch, "m_Archetypes"); var oldGroups = r.Progression.UIGroups;
            try {
                var refs = (BlueprintArchetypeReference[])before ?? Array.Empty<BlueprintArchetypeReference>();
                var own = refs.Where(x => x?.Guid == r.Root.AssetGuid).ToArray();
                if (own.Length > 1 || own.Length == 1 && !ReferenceEquals(own[0].Get(), r.Root)) throw new InvalidOperationException("Hedge Witch parent reference changed.");
                if (own.Length == 0) Set(r.Witch, "m_Archetypes", refs.Concat(new[] { Ref<BlueprintArchetypeReference>(r.Root) }).ToArray());
                var groups = oldGroups ?? Array.Empty<UIGroup>();
                var matching = groups.Where(g => g?.Features.Any(f => f?.AssetGuid == Id(HealingToken) || f?.AssetGuid == Id(EmpathyToken)) == true).ToArray();
                if (matching.Length == 0) r.Progression.UIGroups = groups.Concat(new[] { r.Group }).ToArray();
                else if (matching.Length != 1 || !ReferenceEquals(matching[0], r.Group)) throw new InvalidOperationException("Hedge Witch UI group changed.");
            } catch { Set(r.Witch, "m_Archetypes", before); r.Progression.UIGroups = oldGroups; throw; }
        }
        internal static void Register(BlueprintsCache cache)
        {
            var f = new PsychicAssets(cache);
            var witch = f.Native<BlueprintCharacterClass>(Invoker01437.WitchClassId); var progression = f.Native<BlueprintProgression>(Invoker01437.WitchProgressionId);
            var hex = f.Native<BlueprintFeatureSelection>(Invoker01437.HexPoolId); var donor = f.Native<BlueprintFeature>(CureFeatureId);
            if (witch.name != "WitchClass" || witch.Progression != progression || hex.name != "WitchHexSelection" || donor.name != "ClericSpontaneousCure" || donor.GetComponents<SpontaneousSpellConversion>().Count() != 1 || new[] { 4, 8 }.Any(n => progression.LevelEntries.Count(e => e.Level == n && e.Features.Any(x => x?.AssetGuid == hex.AssetGuid)) != 1))
                throw new InvalidOperationException("Hedge Witch native Witch/hex/conversion schema changed; parents preserved.");
            if (Published.TryGetValue(cache, out var prior)) {
                if (!ReferenceEquals(witch, prior.Witch) || !ReferenceEquals(progression, prior.Progression)) throw new InvalidOperationException("Hedge Witch native parent identity changed.");
                for (int i = 0; i < prior.Assets.Length; i++) if (!ReferenceEquals(cache.Load(prior.Assets[i].AssetGuid), prior.Assets[i]) || Definition(prior.Assets[i]) != prior.Definitions[i]) throw new InvalidOperationException("Hedge Witch private resource changed: " + prior.Assets[i].name);
                Connect(prior); return;
            }
            foreach (string token in OwnTokens) if (cache.Load(Id(token)) != null) throw new InvalidOperationException("Hedge Witch private identity occupied: " + token);
            var conversion = new SpontaneousSpellConversion(); Set(conversion, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(witch));
            Set(conversion, "m_SpellsByLevel", CureIds.Select(id => id == null ? new BlueprintAbilityReference() : Ref<BlueprintAbilityReference>(f.Native<BlueprintAbility>(id))).ToArray());
            var healing = f.Clone(donor, HealingToken, "Spontaneous Healing", "Convert a prepared witch spell of level 1 or higher into a cure spell without knowing or preparing that cure. Uses the original CotW witch conversion table: Light 1, Moderate 2/3, Serious 4, Critical 5, Mass Light 6, Mass Moderate 7, Mass Serious 8, Mass Critical 9. Cantrips cannot be converted. The converted spell consumes its original prepared slot; no separate daily resource.");
            healing.ComponentsArray = new BlueprintComponent[] { conversion }; healing.Groups = Array.Empty<FeatureGroup>(); healing.IsClassFeature = true;
            var empathy = f.Feature(EmpathyToken, "Empathic Healing", "At witch level 8, gain a +3 bonus on Lore (Religion) checks. This is the original CotW skill-bonus implementation.", new AddStatBonus { Stat = StatType.SkillLoreReligion, Value = 3, Descriptor = ModifierDescriptor.UntypedStackable });
            empathy.IsClassFeature = true; Set(empathy, "m_Icon", Get(f.Native<BlueprintAbility>(EmpathyIconId), "m_Icon"));
            var root = f.Add(new BlueprintArchetype { LocalizedName = Text(RootToken, "name", "Hedge Witch"), LocalizedDescription = Text(RootToken, "description", Description), AddFeatures = new[] { Entry(1, healing), Entry(8, empathy) }, RemoveFeatures = new[] { Entry(4, hex), Entry(8, hex) }, ComponentsArray = Array.Empty<BlueprintComponent>() }, RootToken, "Hedge Witch", Description);
            Set(root, "m_ParentClass", witch);
            var group = new UIGroup(); Set(group, "m_Features", new[] { healing, empathy }.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            var staged = OwnTokens.Select(t => f.Assets[t]).ToArray(); foreach (var bp in staged) PsychicRepairComponents.UniqueNames(bp);
            var next = new Receipt { Assets = staged, Witch = witch, Progression = progression, Root = root, Group = group };
            var oldArchetypes = Get(witch, "m_Archetypes"); var oldGroups = progression.UIGroups;
            try { f.Enable(); foreach (var bp in staged) cache.AddCachedBlueprint(bp.AssetGuid, bp); Connect(next); next.Definitions = staged.Select(Definition).ToArray(); Published.Add(cache, next); }
            catch { Set(witch, "m_Archetypes", oldArchetypes); progression.UIGroups = oldGroups; foreach (var bp in staged) if (ReferenceEquals(cache.Load(bp.AssetGuid), bp)) cache.RemoveCachedBlueprint(bp.AssetGuid); throw; }
        }
    }
}
