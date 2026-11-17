using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Newtonsoft.Json;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099ShadowBeasts
    {
        internal const int Reality = 60;
        private const string WizardListId = "ba0401fdeb4062f40a7aa95b6f07fe89";
        private static readonly (int Circle, string Root, string Leaves)[] Families =
        {
            (1, "8fd74eddd9b6c224693d9ab241f25e84", "8fd74eddd9b6c224693d9ab241f25e84"),
            (2, "1724061e89c667045a6891179ee2e8e7", "7ab27a0d547742741beb5d089f1c3852 6223958f3615eeb4d91e52ba57820878"),
            (2, "970c6db48ff0c6f43afc9dbb48780d03", "9cc6b61eba880b944a8f489c44640b5c 69b36426bb910e341a943f101daed594 d8f88028204bc2041be9d9d51f58e6a5 107788f47c4481f4db6da06498b28270"),
            (3, "5d61dde0020bbf54ba1521f7ca0229dc", "15b5efe371d47c944b58444e7b734ffb 28f49845fad6a534b95a65b6cea8f8d6 7b531828b885e8b47a7b06a6f1a34805"),
            (4, "7ed74a3ec8c458d4fb50b192fd7be6ef", "efa433a38e9c7c14bb4e780f8a3fe559 e73c4562e99c7764a9207710facc61d2 24ed31f7b42eb5f4ab461be90d479066"),
            (4, "e42b1dbff4262c6469a9ff0a6ce730e3", "cce8eca4409e1be4794ce94597034f38 4ed414d36753459499578c9c3eb38b17 7b30211b83d55194db872b6c9c0d9cc1 2c86e646a17f6064994ba7e8da4dd77c"),
            (5, "630c8b85d9f07a64f917d79cb5905741", "0964bf88b582bed41b74e79596c4f6d9 715f208d545be2f4aa2d3693e6347a5a e331ef5f6b235f54aaab5f2bf7a64eb6"),
            (5, "89404dd71edc1aa42962824b44156fe5", "16fff43f034133a4a86e914a523e021f 202a024bc7c224d429a9cfb14fa9c3d8 b3a203742191449458d2544b3f442194 9374c81817be79f4e92b48a4aa5ded6e"),
            (6, "e740afbab0147944dab35d83faa0ae1c", "02de4dd8add69aa42a3d1330b573e2ab 237d76aebbb28334e95d83475611cb47 59d28e07b948d4e45a7477ec0065ccb3"),
            (6, "766ec978fa993034f86a372c8eb1fc10", "2c26e78ca33e83843b1aa3f6783877bd 989d3ed13d27d054ea2d26ab4956d075 4814f8645d1d77447a70479c0be51c72 4695c4674e62eb3479fa03f0b47cc6a8"),
            (7, "ab167fd8203c1314bac6568932f1752f", "2920d48574933c24391fbb9e18f87bf5 43f763d347eb2744caed9c656ba89531 6e805a9e3ff445146a6386f5a704d4bc"),
            (7, "8eb769e3b583f594faabe1cfdb0bb696", "a4849d6bd536ade48a3839a4ad960a8b 3ecd589cf1a55df42a3b66940ee93ea4 35c6b4497a5bfd24aabe0cf28507abef cfa3ff71a42ab8a4c8a8a4b5db3f04d4"),
            (8, "d3ac756a229830243a72e84f3ab050d0", "eb6df7ddfc0669d4fb3fc9af4bd34bca ddc1d195a4434374e860b1568cfc7d11 12815be343acc4c45b24554089c8c4bc"),
            (8, "8a7f8c1223bda1541b42fd0320cdbe2b", "333efbf776ab61c4da53e9622751d95f 65254c7a2cf18944287207e1de3e44e8 e4926aa766a1cc048835237b3a97597d 1e54cc4ef5b261248adb30916ba49e72"),
            (9, "52b5df2a97df18242aec67610616ded0", "e96593e67d206ab49ad1b567327d1e75 4988b2e622c6f2d4b897894e3be13f09 22b9580c961c5e64f95f10ca9a82c564")
        };
        internal static readonly string[] DonorIds = Families.SelectMany(family => family.Leaves.Split(' ').Append(family.Root))
            .Append(WizardListId).Distinct().ToArray();
        internal static readonly BlueprintGuid RootId = BlueprintGuid.Parse(Occultist0099Ids.Id("ShadowBeastsAbility"));
        internal static IEnumerable<string> Tokens() => new[] { "ShadowBeastsAbility", "ShadowBeastsPower", "ShadowBeastsReality",
            "ShadowBeastsEmptyList", "ShadowBeastsHealth" }.Concat(Families.SelectMany(family => family.Leaves.Split(' ')).Select(Token));
        private static string Token(string donor) => "ShadowBeasts-" + donor;
        private static readonly string[] RomanCircles = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };

        private static string SummonLabel(string name, int circle)
        {
            if (!name.StartsWith("SummonMonster", StringComparison.Ordinal))
                return Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
            string count = name.EndsWith("d4plus1", StringComparison.Ordinal) ? "1d4+1" :
                name.EndsWith("d3", StringComparison.Ordinal) ? "1d3" : "1";
            return "Summon Monster " + RomanCircles[circle - 1] + " (" + count + ")";
        }

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache, BlueprintUnitProperty level, BlueprintUnitProperty dc,
            Func<string, string, string, LocalizedString> text)
        {
            var wizard = Occultist0099Illusion.Load<BlueprintSpellList>(cache, WizardListId);
            if (wizard?.name != "WizardSpellList") return null;
            const string description = "Spend 1 point of Illusion focus to mimic a summon monster or summon elemental spell with 60% real creatures. The maximum spell level is 5, plus 1 for every 2 occultist levels beyond 9th, up to 9th. Disbelieving targets take 60% damage and have a 60% chance to be affected by nondamaging effects.";
            var icon = Occultist0099Illusion.Load<BlueprintAbility>(cache, Families[0].Root);
            var health = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                ComponentsArray = new BlueprintComponent[] { new Occultist0099ShadowHealth { name = "ShadowBeastHitPoints" } } },
                "ShadowBeastsHealth", "Shadow Beasts", description, icon, text);
            SetEnum(health, "m_Flags", "HiddenInUi");
            var variants = new List<BlueprintAbility>();
            foreach (var family in Families)
            {
                var parent = Occultist0099Illusion.Load<BlueprintAbility>(cache, family.Root);
                var donors = family.Leaves.Split(' ');
                if (parent == null || parent.Type != AbilityType.Spell || parent.School != SpellSchool.Conjuration ||
                    PsychicSpellIntegration.ParentGuid(parent) != BlueprintGuid.Empty ||
                    !wizard.SpellsByLevel.Single(row => row.SpellLevel == family.Circle).SpellsRefs.Any(reference => reference.Guid == parent.AssetGuid) ||
                    !parent.GetComponents<SpellListComponent>().Any(link => link.SpellLevel == family.Circle &&
                        ((BlueprintReferenceBase)Get(link, "m_SpellList")).Guid == wizard.AssetGuid)) return null;
                var branches = parent.GetComponent<AbilityVariants>();
                if (branches == null ? donors.Length != 1 || donors[0] != family.Root :
                    !((BlueprintAbilityReference[])Get(branches, "m_Variants")).Select(reference => reference.Guid.ToString()).SequenceEqual(donors)) return null;
                foreach (string id in donors)
                {
                    var source = Occultist0099Illusion.Load<BlueprintAbility>(cache, id);
                    var actions = source?.GetComponent<AbilityEffectRunAction>();
                    if (source == null || source.Type != AbilityType.Spell || source.School != SpellSchool.Conjuration ||
                        !source.CanTargetPoint || actions == null || (source != parent && PsychicSpellIntegration.ParentGuid(source) != parent.AssetGuid)) return null;
                    var cloned = CopyData(actions);
                    var spawns = SpellBatch01447.Elements(new[] { cloned }, new HashSet<object>(IdentityComparer)).OfType<ContextActionSpawnMonster>().ToArray();
                    if (spawns.Length == 0) return null;
                    foreach (var spawn in spawns)
                    {
                        spawn.AfterSpawn = Actions((spawn.AfterSpawn?.Actions ?? Array.Empty<Kingmaker.ElementsSystem.GameAction>())
                            .Append(new Occultist0099ShadowHealthAction { Effect = Ref<BlueprintBuffReference>(health) }).ToArray());
                    }
                    var components = source.GetComponents<ContextRankConfig>().Select(component => (BlueprintComponent)CopyData(component))
                        .Concat(new BlueprintComponent[] { cloned, Occultist0099Illusion.Descriptor(SpellDescriptor.Summoning),
                            new Occultist0099ShadowTier { name = "OccultistShadowTier", Level = Ref<BlueprintUnitPropertyReference>(level),
                                Minimum = Math.Max(9, family.Circle * 2 - 1) } });
                    var ability = Occultist0099Illusion.NewPower(AbilityType.SpellLike, source.Range, 1, level, dc, components.ToArray());
                    ability.GetComponent<Occultist0099FocusResourceLogic>().CheckCasterRestrictions = true;
                    ability.CanTargetPoint = true;
                    ability.CanTargetSelf = false;
                    ability.CustomRange = source.CustomRange;
                    variants.Add(Identify(ability, Token(id), "Shadow Beasts: " + SummonLabel(source.name, family.Circle), description, source, text));
                }
            }
            var empty = Identify(new BlueprintSpellList { SpellsByLevel = Enumerable.Range(0, 11)
                .Select(circle => new SpellLevelList(circle)).ToArray(), ComponentsArray = Array.Empty<BlueprintComponent>() },
                "ShadowBeastsEmptyList", "", "", null);
            var factor = Identify(new BlueprintUnitProperty { BaseValue = Reality, ComponentsArray = Array.Empty<BlueprintComponent>() },
                "ShadowBeastsReality", "", "", null);
            var root = Occultist0099Illusion.Variants("ShadowBeastsAbility", "Shadow Beasts", description, icon, text, variants.ToArray());
            root.CanTargetPoint = true;
            // Native variants acquire shadow settings from their immediate ConvertedFrom.
            // The empty list suppresses the ordinary unrestricted shadow spell menu.
            var shadow = new AbilityShadowSpell { name = "ShadowBeastsNativeReality", School = SpellSchool.Conjuration,
                MaxSpellLevel = -1, SpellList = Ref<BlueprintSpellListReference>(empty) };
            Set(shadow, "m_Factor", Ref<BlueprintUnitPropertyReference>(factor));
            root.ComponentsArray = root.ComponentsArray.Concat(new BlueprintComponent[] { shadow }).ToArray();
            return new BlueprintScriptableObject[] { root, empty, factor, health }.Concat(variants).ToArray();
        }
    }

    [TypeId(Occultist0099Ids.ShadowTierTypeId)]
    public sealed class Occultist0099ShadowTier : BlueprintComponent, IAbilityVisibilityProvider, IAbilityCasterRestriction
    {
        public BlueprintUnitPropertyReference Level;
        public int Minimum;
        public bool IsAbilityVisible(AbilityData ability) => IsCasterRestrictionPassed(ability?.Caster?.Unit);
        public bool IsCasterRestrictionPassed(UnitEntityData caster)
            => caster != null && (Level?.Get()?.GetInt(caster) ?? 0) >= Minimum;
        public string GetAbilityCasterRestrictionUIText() => "Your occultist level is too low for this shadow summon.";
    }

    public sealed class Occultist0099ShadowHealthData
    {
        [JsonProperty] public int? Penalty;
    }

    [TypeId(Occultist0099Ids.ShadowHealthActionTypeId)]
    public sealed class Occultist0099ShadowHealthAction : ContextAction
    {
        public BlueprintBuffReference Effect;
        public override string GetCaption() => "Set the summoned shadow's maximum hit points";

        public override void RunAction()
        {
            var unit = Target.Unit;
            var effect = Effect?.Get();
            if (unit == null || effect == null) return;
            // The creature's inherent HP is not an effect it can disbelieve.
            var health = new MechanicsContext(Context.MaybeCaster, unit, Context.AssociatedBlueprint);
            using (health.GetDataScope(Target))
            {
                var buff = unit.Descriptor.AddBuff(effect, health, null);
                if (buff != null) buff.IsNotDispelable = true;
            }
        }
    }

    [TypeId(Occultist0099Ids.ShadowHealthTypeId)]
    public sealed class Occultist0099ShadowHealth : UnitBuffComponentDelegate<Occultist0099ShadowHealthData>
    {
        protected override void OnTurnOn()
        {
            var hp = Owner.Stats.HitPoints;
            if (!Data.Penalty.HasValue)
                Data.Penalty = hp.ModifiedValue * (100 - Occultist0099ShadowBeasts.Reality) / 100;
            hp.AddModifierUnique(-Data.Penalty.Value, Runtime, ModifierDescriptor.UntypedStackable);
        }

        protected override void OnTurnOff() => Owner.Stats.HitPoints.RemoveModifiersFrom(Runtime);
    }
}
