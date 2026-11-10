using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.SpiritGuideBridge0060;

namespace CallOfTheWild.Wrath.Prototype
{
    // COTW-0060 / F-0060-SPIRITS. The full ten-spirit catalogue for the Oracle
    // Spirit Guide archetype: ten spirit progressions (hex at 3, bonded spells at
    // 4, base power at 7, greater power at 15), the 39-entry hex pool, the
    // 90-spell temporary-knowledge table, the Lore Arcane Enlightenment picks at
    // 15/17/19 and the private Life Extra Channel. Eight spirits clone their
    // native Wrath shaman donors and rebind them onto the real Oracle class and
    // Charisma; Heavens and Lore have no native donor and are built privately
    // from the original CotW methods. Every mutable subgraph is private: no
    // native blueprint component, spell list, buff or resource is edited.
    internal static class SpiritGuideSpirits0060
    {
        internal const string ParametrizedFeatureId = "bcd757ac2aeef3c49b77e5af4e510956";
        internal const string ExtraChannelId = "cd9f19775bd9d3343a31a065e93f0c47";
        internal const string FaerieFireBuffId = "cc383a9eaae4d2b45a925d442b367b54";
        internal const string GlitterdustId = "ce7dad2b25acf85429b6c9550787b2d9";
        private const string BlindnessBuffId = "187f88d96a0ef464280706b63635f2af";
        private const string ConfusedBuffId = "886c7407dc629dc499b9f1465ff382df";
        private const string EntangleBuffId = "f7f6330726121cf4b90a6086b05d2e38";
        private const string BlurBuffId = "dd3ad347240624d46a11a092b4dd4674";
        private const string SlowAbilityId = "f492622e473d34747806bdb39356eb89";
        private const string ConstructFeatureId = "fd389783027d63343b4a5634bd81645f";
        private const string DetectMagicIconId = "ee0b69e90bac14446a4cf9a050f87f2e";
        private const string RestorationIconId = "f2115ac1148256b4ba20788f7e966830";
        private const string BlurIconId = "14ec7a4e52e90fa47a4c8d63c69fd5c1";
        private const string AnimalFactId = "a95311b3dc996964cbaa30ff9965aaf6";
        private const string FriendCarrierBuffId = "05cdce24bc6c1b547a566faad6853579";
        private const string FriendDonorId = "0d92799cecf414249a23ad82f7e9901a";
        private const string FluidMagicIconId = "e3f41966c2d662a4e9582a0497621c46";
        private const string ChargedBlastIconId = "40681ea748d98f54ba7f5dc704507f39";

        // The native feat pools that already offer the native Extra Channel feat;
        // the private clone may only enter these (same gate as 0117).
        internal static readonly string[] ExtraChannelPoolIds =
        {
            "247a4068296e8be42890143f451b4b45",
            "d332c1748445e8f4f9e92763123e31bd",
            "a21acdafc0169f5488a9bd3256e2e65b"
        };

        // A hex either clones a real Wrath donor feature (Donor != null) or is
        // built privately from native components (Donor == null). Resource is the
        // contract shape: 0 passive, 2 = R2 (1 + Wisdom), 5 = R5 (one per Oracle level).
        internal sealed class HexSpec
        {
            internal string Name;
            internal string Donor;
            internal int Resource;
            internal HexSpec(string name, string donor, int resource) { Name = name; Donor = donor; Resource = resource; }
        }

        internal sealed class Row
        {
            internal string Name;
            internal string[] Tiers;      // base (7) and greater (15) donor GUIDs; null = private build
            internal HexSpec[] Hexes;     // the frozen contract hex pool for this spirit
            internal int[] Resource;      // 0 none, 1 R1 (3+Cha), 2 R2 (1+Cha), 3 R3 (fixed 3 + cooldown)
            internal string[] TierNames;
            internal string[] Spells;     // the nine bonded-spell donors, circles 1..9
            internal bool ExtraChannel;   // Life publishes a private Extra Channel feat
        }

        internal static readonly Row[] Rows =
        {
            new Row { Name = "Battle",
                Tiers = new[] { "5e1161a3a7e5a83458de8a5f412c8d0c", "cc6b293550ac66c48a0c787b5e986a34" },
                Hexes = new[] {
                    new HexSpec("Battle Master", null, 0),
                    new HexSpec("Battle Ward", "6f60723288b405c48af821cf995e4389", 0),
                    new HexSpec("Curse of Suffering", null, 0),
                    new HexSpec("Hampering Hex", "36f07f59f983cd042ae0c8c91e4dcea6", 0) },
                Resource = new[] { 1, 1 }, TierNames = new[] { "Battle Spirit", "Enemies' Bane" },
                Spells = new[] { "c60969e7f264e6d4b84a1499fdcf9039", "c28de1f98a3f432448e52e5d47c73208", "2d4263d80f5136b4296d6eb43a221d7d",
                    "80189142f7c640f39195defdc9777b27", "90810e5cf53bf854293cbd5ea1066252", "6a234c6dcde7ae94e94e9c36fd1163a7",
                    "da1b292d91ba37948893cdbe9ea89e28", "7cfbefe0931257344b2cb7ddc4cdff6f", "01300baad090d634cb1a1b2defe068d6"} },
            new Row { Name = "Bones",
                Tiers = new[] { "f556c44606e5eaa47bac31aae9ebc96d", "3f8db74aafbcaff40ae3f2cf5192ca4f" },
                Hexes = new[] {
                    new HexSpec("Bone Lock", "e506667113c93f94a962a90adedcdb03", 0),
                    new HexSpec("Bone Ward", "96a99ea4866da4b4cbd51b81f1a61a72", 0),
                    new HexSpec("Fearful Gaze", "89603c7a9ba690d46b95148b46a34f7d", 0),
                    new HexSpec("Deathly Being", null, 0) },
                Resource = new[] { 1, 3 }, TierNames = new[] { "Touch of the Grave", "Shard Soul" },
                Spells = new[] { "bd81a3931aa285a4f9844585b5d97e51", "7a5b5bf845779a941a67251539545762", "4b76d32feb089ad4499c3a1ce8e1ac27",
                    "d2aeac47450c76347aebbc02e4f463e0", "4fbd47525382517419c66fb548fe9a67", "a89dcbbab8f40e44e920cc60636097cf",
                    "ce00f6497d01ee5b42f2f328774cee96", "08323922485f7e246acb3d2276515526", "b24583190f36a8442b212e45226c54fc"} },
            new Row { Name = "Flame",
                Tiers = new[] { "ee5503b51c0c9684cb7300d28e3fc4e1", "c2a4ce87f8600db41a9a74d33af87c2c" },
                Hexes = new[] {
                    new HexSpec("Fire Nimbus", "24074b42608fff54f8e1a5b7fa4a7d29", 0),
                    new HexSpec("Flame Curse", "dec5607ab6ea68d4aac0772049e80be7", 0),
                    new HexSpec("Ward of Flames", "c95e1ccf872844b44a47378f761689be", 0),
                    new HexSpec("Cinder Dance", "6e67eae3081853544b191943f5ed4534", 0) },
                Resource = new[] { 1, 3 }, TierNames = new[] { "Touch of Flame", "Fiery Soul" },
                Spells = new[] { "4783c3709a74a794dbe7c8e7e0b1b038", "21ffef7791ce73f468b6fca4d9371e8b", "2d81362af43aeac4387a3d4fced489c3",
                    "80189142f7c640f39195defdc9777b27", "b3a203742191449458d2544b3f442194", "95300c35b46e1bb6a86d6fb3d7c1208e",
                    "e3d0dfe1c8527934294f241e0ae96a8d", "99bad8fca7534acdb86866940eef62da", "08ccad78cac525040919d51963f9ac39"} },
            new Row { Name = "Heavens",
                Tiers = new string[] { null, null },
                Hexes = new[] {
                    new HexSpec("Enveloping Void", null, 0),
                    new HexSpec("Lure of Heavens", null, 0),
                    new HexSpec("Heaven's Leap", null, 0),
                    new HexSpec("Starburn", null, 2) },
                Resource = new[] { 1, 0 }, TierNames = new[] { "Stardust", "Void Adaptation" },
                Spells = new[] { "91da41b9793a4624797921f221db653c", "56b8f0304a704a67b3c35cbe8c774854", "ce7dad2b25acf85429b6c9550787b2d9",
                    "4b8265132f9c8174f87ce7fa6d0fe47b", "7253f42036967e31c7d1da32b5b85e22", "645558d63604747428d55f0dd3a4cb58",
                    "b22fd434bdb60fb4ba1068206402c4cf", "e96424f70ff884947b06f41a765b7658", "5e36df08c71748f7936bce310181fb71"} },
            new Row { Name = "Life",
                Tiers = new[] { "9ae3ba76663a19141a3996c1e3ce95e3", "8cae5b62ad604de41bf3e9f4489c6b8f" },
                Hexes = new[] {
                    new HexSpec("Curse of Suffering", null, 0),
                    new HexSpec("Enhanced Cures", "973a22b02c793ca49b48652e3d70ae80", 0),
                    new HexSpec("Life Link", "ef97c9bcc1c54ea7993ef8b2489c908a", 5),
                    new HexSpec("Life Sight", null, 5) },
                Resource = new[] { 2, 0 }, TierNames = new[] { "Channel Positive Energy", "Healer's Touch" },
                Spells = new[] { "f6f95242abdfac346befd6f4f6222140", "e84fc922ccf952943b5240293669b171", "e7240516af4241b42b2cd819929ea9da",
                    "f2115ac1148256b4ba20788f7e966830", "d5847cad0b0e54c4d82d6c59a3cda6b0", "5da172c4c89f9eb4cbb614f3a67357d3",
                    "fafd77c6bfa85c04ba31fdc1c962c914", "867524328b54f25488d371214eea0d90", "80a1a388ee938aa4e90d427ce9a7a3e9"},
                ExtraChannel = true },
            new Row { Name = "Lore",
                Tiers = new string[] { null, null },
                Hexes = new[] {
                    new HexSpec("Benefit of Wisdom", null, 0),
                    new HexSpec("Brain Drain", null, 0),
                    new HexSpec("Confusion Curse", null, 0),
                    new HexSpec("Mental Acuity", "736d3d5d940736b4e87792e94eaff96b", 0) },
                Resource = new[] { 1, 0 }, TierNames = new[] { "Monstrous Insight", "Arcane Enlightenment" },
                Spells = new[] { "2c38da66e5a599347ac95b3294acbe00", "f0455c9295b53904f9e02fc571dd2ce1", "1a045f845778dc54db1c2be33a8c3c0a",
                    "e9cc9378fd6841f48ad59384e79e9953", "4cf3d0fae3239ec478f51e86f49161cb", "9f5ada581af3db4419b54db77f44e430",
                    "fafd77c6bfa85c04ba31fdc1c962c914", "0e67fa8f011662c43934d486acc50253", "585f7776285d17b60b9e8e2fe42a69be"} },
            new Row { Name = "Nature",
                Tiers = new[] { "0a7166e9c6ea5874e9fc4984f30f2d8d", "f54191d614633da4baac7a47a659746b" },
                Hexes = new[] {
                    new HexSpec("Entangling Curse", "176ee8bedc899964a96ef1b7478dcce7", 0),
                    new HexSpec("Erosion Curse", null, 0),
                    new HexSpec("Friend to Animals", null, 0),
                    new HexSpec("Stormwalker", null, 0) },
                Resource = new[] { 1, 0 }, TierNames = new[] { "Storm Burst", "Spirit of Nature" },
                Spells = new[] { "403cf599412299a4f9d5d925c7b9fb33", "5b77d7cc65b8ab74688e74a37fc2f553", "754c478a2aa9bb54d809e648c3f7ac0e",
                    "e418c20c8ce362943a8025d82c865c1c", "6d1d48a939ce475409f06e1b376bc386", "7c5d556b9a5883048bf030e20daebe31",
                    "b974af13e45639a41a04843ce1c9aa12", "7cfbefe0931257344b2cb7ddc4cdff6f", "d8144161e352ca846a73cf90e85bf9ac"} },
            new Row { Name = "Stone",
                Tiers = new[] { "5c3ccab7cb27f4a408531197eb2abd3f", "ef692472df1b10f4caec90b4984965ef" },
                Hexes = new[] {
                    new HexSpec("Stone Stability", "05b6b99b0f394284899eccd4728031b7", 0),
                    new HexSpec("Loadstone", null, 0),
                    new HexSpec("Metal Curse", "ea8e9fef24350684d82b38fec2cc3139", 0),
                    new HexSpec("Ward of Stone", null, 0) },
                Resource = new[] { 1, 3 }, TierNames = new[] { "Touch of Acid", "Body of Earth" },
                Spells = new[] { "85067a04a97416949b5d1dbf986d93f3", "5181c2ed0190fc34b8a1162783af5bf4", "1a36c8b9ed655c249a9f9e8d4731f001",
                    "d1afa8bc28c99104da7d784115552de5", "c66e86905f7606c4eaa5c774f0357b2b", "989d3ed13d27d054ea2d26ab4956d075",
                    "facdc8851a0b3f44a8bed50f0199b83c", "65254c7a2cf18944287207e1de3e44e8", "01300baad090d634cb1a1b2defe068d6"} },
            new Row { Name = "Waves",
                Tiers = new[] { "62f408b7561adb34b899363c905fa13a", "b2387cc5ccce4f9428ffd069aa03d029" },
                Hexes = new[] {
                    new HexSpec("Crashing Waves", null, 0),
                    new HexSpec("Mist's Shroud", null, 0),
                    new HexSpec("Fluid Magic", null, 0) },
                Resource = new[] { 1, 3 }, TierNames = new[] { "Ice Splinter", "Frigid Blast" },
                Spells = new[] { "67b29b99f13e40a48a1a95fbd909a298", "b6010dda6333bcf4093ce20f0063cd41", "72160cb6f0161bc713fef2941207ef38",
                    "fcb028205a71ee64d98175ff39a0abf9", "65e8d23aef5e7784dbeb27b1fca40931", "cee712a5129a14e1973f89e542671288",
                    "89778dc261fe6094bb2445cb389842d2", "7ef49f184922063499b8f1346fb7f521", "d8144161e352ca846a73cf90e85bf9ac"} },
            new Row { Name = "Wind",
                Tiers = new[] { "5d810adea03fb644582eb74de32c75ec", "59550028efc886a4e8ca2c80b680642f" },
                Hexes = new[] {
                    new HexSpec("Air Barrier", "4b5b0f429730a984fb56fc3e34e5231e", 0),
                    new HexSpec("Sparkling Aura", null, 0),
                    new HexSpec("Vortex Spells", "d54e52f0e3d086f468ede43222e053e9", 0),
                    new HexSpec("Wind Ward", "2a7f7b35e1a0fb44ba7f01e28fa7a535", 0) },
                Resource = new[] { 1, 3 }, TierNames = new[] { "Shocking Touch", "Spark Soul" },
                Spells = new[] { "ab395d2335d3f384e99dddee8562978f", "c28de1f98a3f432448e52e5d47c73208", "d2cff9243a7ee804cb6d5be47af30c73",
                    "abc2221c9f5d53015e7d4c31e2e752f1", "16fff43f034133a4a86e914a523e021f", "093ed1d67a539ad4c939d9d05cfe192c",
                    "616bf6d76f44461301f1dbe39bc7d5a8", "333efbf776ab61c4da53e9622751d95f", "5d8f1da2fdc0b9242af9f326f9e507be"} },
        };

        // Every identity this package reads must exist before anything is staged:
        // all 90 bonded spells (including the four external roots and this mod's
        // own NewSpells), every tier and hex donor, the Extra Channel feat, the
        // parametrized spell-learning feature and the private builders' native
        // effect donors and icons. One missing identity rejects the whole package.
        internal static IEnumerable<string> RequiredProviders()
        {
            yield return ExtraChannelId; yield return ParametrizedFeatureId;
            yield return FaerieFireBuffId; yield return GlitterdustId;
            yield return BlindnessBuffId; yield return ConfusedBuffId; yield return EntangleBuffId;
            yield return BlurBuffId; yield return SlowAbilityId; yield return ConstructFeatureId;
            yield return DetectMagicIconId; yield return RestorationIconId;
            yield return AnimalFactId; yield return FriendCarrierBuffId; yield return FriendDonorId;
            yield return BlurIconId; yield return FluidMagicIconId; yield return ChargedBlastIconId;
            foreach (var pool in ExtraChannelPoolIds) yield return pool;
            foreach (var row in Rows)
            {
                foreach (var tier in row.Tiers) if (!string.IsNullOrEmpty(tier)) yield return tier;
                foreach (var hex in row.Hexes) if (!string.IsNullOrEmpty(hex.Donor)) yield return hex.Donor;
            }
        }

        // D-F03. The four genuinely external bonded-spell roots and their frozen
        // identities. Everything else in the 90-spell table is produced by this
        // project's own registered modules (PsychicExpansion / TimeStop01438), so
        // those eight roots are validated as published spells rather than staged.
        internal sealed class ExternalSpell
        {
            internal readonly string Guid, Name, Provider;
            internal readonly SpellSchool School;
            internal ExternalSpell(string guid, string name, string provider, SpellSchool school)
            { Guid = guid; Name = name; Provider = provider; School = school; }
        }

        internal static readonly ExternalSpell[] ExternalSpells =
        {
            new ExternalSpell("80189142f7c640f39195defdc9777b27", "WallOfFireAbility", "ExpandedContent", SpellSchool.Evocation),
            new ExternalSpell("56b8f0304a704a67b3c35cbe8c774854", "HypnoticPatternAbility", "ExpandedContent", SpellSchool.Illusion),
            new ExternalSpell("99bad8fca7534acdb86866940eef62da", "IncendiaryCloudAbility", "ExpandedContent", SpellSchool.Conjuration),
            new ExternalSpell("67b29b99f13e40a48a1a95fbd909a298", "Frostbite", "CharacterOptionsPlus", SpellSchool.Transmutation)
        };

        internal static readonly string[] InternalSpellGuids = Rows.SelectMany(r => r.Spells).Distinct()
            .Where(g => !ExternalSpells.Any(e => e.Guid == g)).ToArray();

        // Returns the first precise blocker, or null when the whole package may be
        // staged. A loadable identity is not enough: external roots must satisfy the
        // existing PsychicSpellIntegration.Available contract (real name, spell
        // root, school, executable effect and a reciprocal provider spell-list
        // publication); internal roots must be published executable spells.
        internal static string MissingProvider(BlueprintsCache cache)
        {
            foreach (var guid in RequiredProviders())
                if (cache.Load(BlueprintGuid.Parse(guid)) == null) return "provider " + guid;
            foreach (var external in ExternalSpells)
            {
                var link = new PsychicSpellLink(external.Guid, external.Name, external.Provider, external.School, true, -1, -1, -1);
                if (!PsychicSpellIntegration.Available(cache, link, out var ability))
                    return "external " + external.Provider + " " + external.Name + " (" + external.Guid +
                           "): identity, school, effect or provider spell-list publication unavailable";
                // D2-F03: Available is a whole-library availability policy, so it
                // deliberately stops at "some effect component exists". This
                // package additionally vets the bounded shape of the exact roots
                // it consumes; a correct identity with a reciprocal publication
                // but an empty ActionList must not clear the atomic gate.
                if (!ExecutableRoot(ability))
                    return "external " + external.Provider + " " + external.Name + " (" + external.Guid +
                           "): published root has no executable effect path (empty actions, variants or touch target)";
            }
            foreach (var guid in InternalSpellGuids)
            {
                var spell = cache.Load(BlueprintGuid.Parse(guid)) as BlueprintAbility;
                if (!PublishedSpell(spell))
                    return "internal spell " + guid + " is not a published executable spell";
            }
            if (cache.Load(BlueprintGuid.Parse(RevelationSelectionId)) == null)
                return "provider " + RevelationSelectionId;
            return null;
        }

        private static bool PublishedSpell(BlueprintAbility ability) =>
            ability != null && ability.Type == AbilityType.Spell && ExecutableRoot(ability);

        // Bounded structural check for the five root shapes this package uses.
        // It never walks arbitrary component graphs and never changes the shared
        // PsychicSpellIntegration.Available policy; it only requires the one
        // effect path a published root actually executes to be non-empty.
        private static bool ExecutableRoot(BlueprintAbility ability)
        {
            if (ability == null) return false;
            foreach (var run in ability.GetComponents<AbilityEffectRunAction>())
                if (run.Actions?.Actions != null && run.Actions.Actions.Any(a => a != null)) return true;
            foreach (var variants in ability.GetComponents<AbilityVariants>())
            {
                var array = Get(variants, "m_Variants") as BlueprintAbilityReference[];
                if (array != null && array.Any(r => r != null)) return true;
            }
            foreach (var touch in ability.GetComponents<AbilityEffectStickyTouch>())
                if (Get(touch, "m_TouchDeliveryAbility") != null) return true;
            foreach (var shadow in ability.GetComponents<AbilityShadowSpell>())
                if (Get(shadow, "SpellList") != null) return true;
            foreach (var extra in ability.GetComponents<AdditionalAbilityEffectRunActionOnClickedTarget>())
                if (extra.Action?.Actions != null && extra.Action.Actions.Any(a => a != null)) return true;
            return false;
        }

        internal static readonly List<string> Diagnostics = new List<string>();

        internal static BlueprintFeatureSelection Build(Factory f, BlueprintCharacterClass oracle, BlueprintsCache cache)
        {
            var progressions = new List<BlueprintProgression>();
            var spiritText = new Dictionary<string, string>
            {
                { "Battle", "A spirit guide who selects the battle spirit gains scars from every wound she takes, and the grit of battle always seems to cling on her body." },
                { "Bones", "A spirit guide who selects the bones spirit is skin stretched over nothing but bone." },
                { "Flame", "A spirit guide who selects the flame spirit has a radiant heat about her, her hair flickers with traces of fire, and her smoldering eyes settle on whomever she talks to." },
                { "Heavens", "A spirit guide who selects the heavens spirit has eyes that sparkle like starlight, exuding an aura of otherworldliness to those around her." },
                { "Life", "A spirit guide who selects the life spirit appears more vibrant and full of life than others. Her skin has a warm glow, and a faint radiance seems to shine from within her." },
                { "Lore", "A spirit guide who selects the lore spirit speaks in a measured tone, always considering multiple options and outcomes before acting. Her eyes constantly dart around, absorbing every detail." },
                { "Nature", "A spirit guide who selects the nature spirit typically aligns herself with the natural order of the world, and rejects the laws of civilization." },
                { "Stone", "A spirit guide who selects the stone spirit typically adopts a blocky, crude appearance." },
                { "Waves", "A spirit guide who selects the waves spirit has a fluid grace that exhibits itself whenever she moves." },
                { "Wind", "A spirit guide who selects the wind spirit speaks with a breathy voice, and her hair moves as if blown by a gentle breeze." }
            };

            foreach (var row in Rows)
            {
                var hexes = row.Hexes.Select((hex, index) => Hex(f, oracle, row, hex, index)).ToArray();
                var hexSelection = f.Selection(row.Name + "HexSelection0060", "Hex",
                    "At 3rd level, a spirit guide gains one hex of her choice from the list of hexes available from that spirit. She uses her oracle level as her shaman level, and the hexes' save DCs use Charisma while the branches that read the shaman's Wisdom use her Wisdom.");
                Set(hexSelection, "m_AllFeatures", hexes.Select(Ref<BlueprintFeatureReference>).ToArray());
                Set(hexSelection, "m_Features", hexes.Select(Ref<BlueprintFeatureReference>).ToArray());

                var spells = row.Spells.Select(g => f.Native<BlueprintAbility>(g)).ToArray();
                if (spells.Any(s => s == null)) throw new InvalidOperationException("0060 bonded spell missing: " + row.Name);
                var spellNames = string.Join(", ", spells.Select(s => s.name));
                var learnSpells = f.Feature(row.Name + "Spells0060", "Bonded Spirit Spells",
                    "The " + row.Name + " bonded spirit grants the spirit guide the following spells, added to her oracle spellbook as temporary known spells while the bond is active: " + spellNames + ".",
                    spells.Select((s, i) => (BlueprintComponent)SpiritGuide0060.TemporarySpell(oracle, s, i + 1)).ToArray());

                var basePower = Tier(f, oracle, row, 0);
                var greaterPower = Tier(f, oracle, row, 1);

                var entries = new[] { Entry(3, hexSelection), Entry(4, learnSpells), Entry(7, basePower), Entry(15, greaterPower) };
                var progression = f.Progression(row.Name + "SpiritProgression0060", row.Name + " Spirit",
                    spiritText[row.Name], entries, new[] { Group(hexSelection, learnSpells, basePower, greaterPower) });
                Set(progression, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(oracle) } });
                Set(progression, "m_Icon", basePower.Icon);
                progressions.Add(progression);
                Diagnostics.Add(row.Name + ": " + hexes.Length + " hexes, 9 spells, base=" + basePower.name + ", greater=" + greaterPower.name);

                if (row.ExtraChannel) ExtraChannel(f, oracle, basePower);
            }

            var selection = f.Selection("BondedSpirit0060", "Bonded Spirit",
                "At 3rd level, a spirit guide can form a bond with a spirit, as the shaman's wandering spirit class feature. At 3rd level she gains one hex of her choice from that spirit's hexes. At 4th level she adds the bonded spirit's spells to her oracle spells known. At 7th level she gains the spirit ability of her bonded spirit, and at 15th level its greater spirit ability. The choice is permanent for this character.");
            Set(selection, "m_AllFeatures", progressions.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(selection, "m_Features", progressions.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(selection, "m_Icon", f.Native<BlueprintUnitFact>(MysterySelectionId).Icon);
            Diagnostics.Add("published " + progressions.Count + " spirits, "
                + Rows.Sum(r => r.Hexes.Length) + " hex pool entries, " + Rows.Length * 9 + " bonded spells");
            return selection;
        }

        // ================= tiers (base at 7, greater at 15) =================

        private static BlueprintFeature Tier(Factory f, BlueprintCharacterClass oracle, Row row, int tier)
        {
            string token = row.Name + "Tier" + tier + "Feature0060";
            string display = row.TierNames[tier];

            if (row.Name == "Heavens") return tier == 0 ? HeavensStardust(f, oracle, token, display) : HeavensVoid(f, token, display);
            if (row.Name == "Lore") return tier == 0 ? MonstrousInsight(f, oracle, token, display) : ArcaneEnlightenment(f, oracle, token, display);
            if (row.Name == "Life" && tier == 1) return HealersTouch(f, token, display);

            var donor = f.Native<BlueprintFeature>(row.Tiers[tier]);
            if (donor == null) throw new InvalidOperationException("0060 tier donor missing: " + row.Name + " " + display);
            var feature = (BlueprintFeature)f.Clone(donor, token, display + " (Spirit Guide)",
                "Spirit Guide " + display + ". The effective level is the real Oracle level; daily uses and save DCs follow Charisma where the original ability names a spellcasting stat.");
            StripSpellLists(feature);
            Icon(feature, donor);
            feature.ComponentsArray = feature.ComponentsArray
                .Where(c => c != null && c.GetType().Name != "AddAbilityResources").ToArray();

            var resource = ResourceCode(f, row, tier);
            Rebind(feature, oracle, StatType.Charisma, _ => resource);
            // D-F01: close the whole mutable subgraph (Buff -> Area -> EffectBuff
            // and AbilityVariants), not just the first AddFacts hop.
            SpiritGuideGraph0060.Privatize(f, oracle, feature, token, resource, false);
            if (resource != null)
                feature.ComponentsArray = feature.ComponentsArray.Concat(new BlueprintComponent[] { Grant(resource) }).ToArray();
            if (row.Resource[tier] == 3) AddCooldown(f, feature, token);
            return feature;
        }

        private static BlueprintAbilityResource ResourceCode(Factory f, Row row, int tier)
        {
            switch (row.Resource[tier])
            {
                case 1: return Resource(f, row.Name + "Tier" + tier + "R10060", 3, StatType.Charisma);
                case 2: return Resource(f, row.Name + "Tier" + tier + "R20060", 1, StatType.Charisma);
                case 3: return Resource(f, row.Name + "Tier" + tier + "R30060", 3, null);
                default: return null;
            }
        }

        // ================= hex pool =================

        private static BlueprintFeature Hex(Factory f, BlueprintCharacterClass oracle, Row row, HexSpec hex, int index)
        {
            string token = row.Name + "Hex" + index + "Feature0060";
            if (string.IsNullOrEmpty(hex.Donor)) return PrivateHex(f, oracle, row, hex, token);

            var donor = f.Native<BlueprintFeature>(hex.Donor);
            if (donor == null) throw new InvalidOperationException("0060 hex donor missing: " + hex.Name);
            var feature = (BlueprintFeature)f.Clone(donor, token, hex.Name + " (Spirit Guide)",
                "Spirit Guide " + hex.Name + " hex. Save DCs follow Charisma; the effect branches that read a secondary stat use Wisdom; level scaling uses the real Oracle level. Wrath effect donor: " + donor.name + ".");
            StripSpellLists(feature);
            Icon(feature, donor);
            // Drop the donor's resource grant and every shaman-level prerequisite;
            // keep PrerequisiteNoFeature so a hex cannot be taken twice.
            feature.ComponentsArray = feature.ComponentsArray
                .Where(c => c != null && c.GetType().Name != "AddAbilityResources" &&
                            (!(c is Prerequisite) || c is PrerequisiteNoFeature)).ToArray();

            var resource = hex.Resource == 5 ? Resource(f, token + "R5", 0, null, true)
                : hex.Resource == 2 ? Resource(f, token + "R2", 1, StatType.Wisdom)
                : HasConsumption(feature) ? Resource(f, token + "R1", 3, StatType.Charisma)
                : null;

            Rebind(feature, oracle, StatType.Charisma, _ => resource);
            SpiritGuideGraph0060.Privatize(f, oracle, feature, token, resource, true);
            if (resource != null)
                feature.ComponentsArray = feature.ComponentsArray.Concat(new BlueprintComponent[] { Grant(resource) }).ToArray();

            if (hex.Name == "Life Link" && resource != null) AddLifeLinkRefund(f, feature, resource);
            if (hex.Name == "Mental Acuity") FixMentalAcuity(feature);
            if (hex.Name == "Entangling Curse") FixStatMinimum(feature, "Wisdom", 1);
            if (hex.Name == "Friend to Animals") FixStatMinimum(feature, "Wisdom", 0);
            if (hex.Name == "Friend to Animals") RetargetSummon(f, oracle, feature, token);
            return feature;
        }

        private static BlueprintFeature PrivateHex(Factory f, BlueprintCharacterClass oracle, Row row, HexSpec hex, string token)
        {
            switch (hex.Name)
            {
                case "Curse of Suffering": return CurseOfSuffering(f, oracle, token);
                case "Battle Master": return BattleMaster(f, oracle, token);
                case "Deathly Being": return DeathlyBeing(f, token);
                case "Enveloping Void": return EnvelopingVoid(f, oracle, token);
                case "Lure of Heavens": return LureOfHeavens(f, oracle, token);
                case "Heaven's Leap": return HeavensLeap(f, token);
                case "Starburn": return Starburn(f, oracle, token);
                case "Benefit of Wisdom": return BenefitOfWisdom(f, token);
                case "Brain Drain": return BrainDrain(f, oracle, token);
                case "Confusion Curse": return ConfusionCurse(f, oracle, token);
                case "Erosion Curse": return ErosionCurse(f, oracle, token);
                case "Stormwalker": return Stormwalker(f, oracle, token);
                case "Loadstone": return Loadstone(f, token);
                case "Ward of Stone": return WardOfStone(f, token);
                case "Crashing Waves": return CrashingWaves(f, token);
                case "Mist's Shroud": return MistsShroud(f, token);
                case "Fluid Magic": return FluidMagic(f, token);
                case "Sparkling Aura": return SparklingAura(f, oracle, token);
                case "Friend to Animals": return FriendToAnimals(f, oracle, token);
                case "Life Sight": return LifeSight(f, oracle, token);
            }
            throw new NotSupportedException("0060 private hex '" + hex.Name + "' has no builder.");
        }

        // ================= private hexes (original CotW methods, 0060 stat rules) =================

        // D-F04: the native Wrath Battle Master donor only grants one extra attack
        // of opportunity; its 8/16 weapon growth lives in a separate Shaman
        // progression. The original CotW hex is a weapon choice whose entry grants
        // +1 AoO, Weapon Specialization at 8 and Greater Weapon Focus at 16 for the
        // same weapon, without the usual feat prerequisites. This builds that
        // choice privately from the native parametrized feats; it never grants the
        // native Shaman progression and never touches the native class.
        private static BlueprintFeature BattleMaster(Factory f, BlueprintCharacterClass oracle, string token)
        {
            const string desc = "The spirit guide makes an extra attack of opportunity each round. This stacks with the attacks of opportunity granted by Combat Reflexes. At 8th level she gains Weapon Specialization in a weapon of her choice as a bonus feat; at 16th level she gains Greater Weapon Focus for the same weapon. She does not need to meet the prerequisites of these feats.";
            var specialization = f.Native<BlueprintParametrizedFeature>("31470b17e8446ae4ea0dacd6c5817d86");
            var greaterFocus = f.Native<BlueprintParametrizedFeature>("09c9e82965fb4334b984a1e9df3bd088");
            if (specialization == null || greaterFocus == null)
                throw new InvalidOperationException("0060 Battle Master parametrized feat missing.");

            var selection = f.Selection(token, "Battle Master", desc);
            var entries = new List<BlueprintFeature>();
            foreach (WeaponCategory category in Enum.GetValues(typeof(WeaponCategory)))
            {
                var weaponSpecialization = f.Feature(token + "WeaponSpecialization" + category, "", "",
                    ParametrizedGrant(specialization, category));
                weaponSpecialization.HideInUI = true;
                weaponSpecialization.HideInCharacterSheetAndLevelUp = true;
                var greaterWeaponFocus = f.Feature(token + "GreaterWeaponFocus" + category, "", "",
                    ParametrizedGrant(greaterFocus, category));
                greaterWeaponFocus.HideInUI = true;
                greaterWeaponFocus.HideInCharacterSheetAndLevelUp = true;

                var entry = f.Feature(token + "Feature" + category, "Battle Master (" + category + ")",
                    desc + " Selected weapon: " + category + ".",
                    new AddStatBonus { Descriptor = ModifierDescriptor.UntypedStackable,
                        Stat = StatType.AttackOfOpportunityCount, Value = 1 },
                    FeatureAtLevel(weaponSpecialization, oracle, 8),
                    FeatureAtLevel(greaterWeaponFocus, oracle, 16),
                    new PrerequisiteProficiency
                    {
                        WeaponProficiencies = new[] { category },
                        ArmorProficiencies = Array.Empty<ArmorProficiencyGroup>()
                    });
                entries.Add(entry);
            }
            Set(selection, "m_AllFeatures", entries.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(selection, "m_Features", entries.Select(Ref<BlueprintFeatureReference>).ToArray());
            Diagnostics.Add("Battle Master: " + entries.Count + " weapon choices, +1 AoO, 8 Weapon Specialization, 16 Greater Weapon Focus");
            return selection;
        }

        // Grants a parametrized feat with a fixed weapon parameter and without
        // the feat's own prerequisites (the original hex explicitly waives them).
        private static AddParametrizedFeatures ParametrizedGrant(BlueprintParametrizedFeature feature, WeaponCategory category)
        {
            var component = new AddParametrizedFeatures();
            var dataType = typeof(AddParametrizedFeatures).GetNestedType("FeatureData", BindingFlags.NonPublic);
            if (dataType == null) throw new InvalidOperationException("0060 AddParametrizedFeatures.FeatureData missing.");
            var data = Activator.CreateInstance(dataType, true);
            AccessTools.Field(dataType, "m_Feature").SetValue(data, Ref<BlueprintParametrizedFeatureReference>(feature));
            AccessTools.Field(dataType, "ParamWeaponCategory").SetValue(data, category);
            var array = Array.CreateInstance(dataType, 1);
            array.SetValue(data, 0);
            AccessTools.Field(typeof(AddParametrizedFeatures), "m_Features").SetValue(component, array);
            return component;
        }

        private static AddFeatureOnClassLevel FeatureAtLevel(BlueprintFeature feature, BlueprintCharacterClass oracle, int level)
        {
            var component = new AddFeatureOnClassLevel { Level = level };
            Set(component, "m_Feature", Ref<BlueprintFeatureReference>(feature));
            Set(component, "m_Class", Ref<BlueprintCharacterClassReference>(oracle));
            Set(component, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            return component;
        }

        private static BlueprintFeature CurseOfSuffering(Factory f, BlueprintCharacterClass oracle, string token)
        {
            const string desc = "The target takes 1 extra point of damage whenever it takes bleed damage, and healing restores only half. The curse lasts one round per Oracle level.";
            var heal = Component("IncomingDamageAndHealingModifier", F("Type", "OnlyHeal"), F("ModifierPercents", new ContextValue { Value = 50 }));
            if (heal == null) throw new InvalidOperationException("IncomingDamageAndHealingModifier missing.");
            var buff = f.Buff(token + "Buff", "Curse of Suffering", desc, heal, new BleedIncrease0117());
            var ability = f.Ability(token + "Ability", "Curse of Suffering", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Run(RankRounds(buff)), OracleParams(oracle), OracleRank(oracle));
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Curse of Suffering", desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature DeathlyBeing(Factory f, string token)
        {
            const string desc = "Positive energy that would heal the spirit guide harms her, and negative energy heals her. She has a +4 bonus on saves against death effects and immunity to negative energy.";
            var immune = new AddEnergyImmunity { Type = DamageEnergyType.NegativeEnergy };
            var death = new SavingThrowBonusAgainstDescriptor { SpellDescriptor = SpellDescriptor.Death, ModifierDescriptor = ModifierDescriptor.UntypedStackable, Value = 4 };
            return f.Feature(token, "Deathly Being", desc, immune, death, new DeathlyHealing0117());
        }

        private static BlueprintFeature EnvelopingVoid(Factory f, BlueprintCharacterClass oracle, string token)
        {
            const string desc = "One enemy within close range is blinded for a number of rounds equal to the Oracle level. A Will save negates. Whether or not the save is successful, the creature cannot be the target of this hex again for 24 hours.";
            var donor = f.Native<BlueprintBuff>(BlindnessBuffId);
            if (donor == null) throw new InvalidOperationException("blindness buff missing.");
            var buff = (BlueprintBuff)f.Clone(donor, token + "Buff", "Enveloping Void", desc);
            var ability = f.Ability(token + "Ability", "Enveloping Void", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Save(SavingThrowType.Will, PsychicAssets.Saved(Array.Empty<GameAction>(), new GameAction[] { RankRounds(buff) })),
                OracleParams(oracle), OracleRank(oracle));
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Enveloping Void", desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature LureOfHeavens(Factory f, BlueprintCharacterClass oracle, string token)
        {
            const string desc = "At 10th Oracle level the spirit guide can fly, as the fly spell.";
            var flight = Component("AddMechanicsFeature", F("m_Feature", "Flying"));
            if (flight == null) throw new InvalidOperationException("Flying mechanics feature missing.");
            var gate = Component("PrerequisiteClassLevel", F("m_CharacterClass", Ref<BlueprintCharacterClassReference>(oracle)), F("Level", 10));
            var parts = new List<BlueprintComponent> { flight };
            if (gate != null) parts.Add(gate);
            return f.Feature(token, "Lure of Heavens", desc, parts.ToArray());
        }

        private static BlueprintFeature HeavensLeap(Factory f, string token)
        {
            const string desc = "Choose one ally, or yourself, within close range. Then choose a point within close range; that creature moves there. The same creature cannot be chosen again for 24 hours. Choosing a new target clears the previous mark.";
            var mark = f.Buff(token + "Mark", "Heaven's Leap Target", desc, new HeavensLeapMark0060());
            var day = f.Buff(token + "Day", "Heaven's Leap (already targeted)",
                "This creature cannot be the target of Heaven's Leap again for 24 hours.");
            var choose = f.Ability(token + "Select", "Heaven's Leap: Select", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Free, AbilityRange.Close, PsychicAssets.Run(PsychicAssets.Apply(mark, 1)));
            choose.CanTargetEnemies = false;
            choose.CanTargetFriends = true;
            choose.CanTargetSelf = true;
            var gate = new AbilityTargetHasFact { Inverted = true };
            Set(gate, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(day) });
            choose.ComponentsArray = choose.ComponentsArray.Concat(new BlueprintComponent[] { gate }).ToArray();
            var leap = new HeavensLeap0060 { Mark = mark, DayLock = day };
            var move = f.Ability(token + "Ability", "Heaven's Leap", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close, leap);
            move.CanTargetPoint = true;
            move.CanTargetFriends = false;
            move.CanTargetEnemies = false;
            move.CanTargetSelf = false;
            return f.Feature(token, "Heaven's Leap", desc, PsychicAssets.Facts(choose, move));
        }

        private static BlueprintFeature Starburn(Factory f, BlueprintCharacterClass oracle, string token)
        {
            const string desc = "One creature within close range takes 1d6 fire damage per 2 Oracle levels and shines for 1 round. A Fortitude save halves the damage. Uses are 1 + Wisdom, and each use imposes a 1d4-round cooldown. Whether or not the save is successful, the target cannot be chosen again for 24 hours.";
            var resource = Resource(f, "StarburnResource0060", 1, StatType.Wisdom);
            var light = (BlueprintBuff)f.Clone(f.Native<BlueprintBuff>(FaerieFireBuffId), token + "Light", "Starburn", desc);
            var damage = new ContextActionDealDamage
            {
                DamageType = new DamageTypeDescription { Type = DamageType.Energy, Energy = DamageEnergyType.Fire },
                Value = new ContextDiceValue { DiceType = DiceType.D6, DiceCountValue = RankValue(), BonusValue = 0 },
                HalfIfSaved = true, IgnoreCritical = true, DisableSneakDamage = true
            };
            var cooldown = f.Buff(token + "Cooldown", "Starburn cooldown", "This hex cannot be used again while this cooldown lasts.");
            var restriction = new AbilityCasterHasNoFacts();
            Set(restriction, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(cooldown) });
            var cooldownApply = new ContextActionApplyBuff { ToCaster = true, IsFromSpell = false,
                DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.D4, DiceCountValue = 1, BonusValue = 0 } };
            Set(cooldownApply, "m_Buff", Ref<BlueprintBuffReference>(cooldown));
            var ability = f.Ability(token + "Ability", "Starburn", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Save(SavingThrowType.Fortitude, damage, PsychicAssets.Saved(Array.Empty<GameAction>(), new GameAction[] { PsychicAssets.Apply(light, 1) })),
                PsychicAssets.Run(cooldownApply), restriction,
                Cost(resource), OracleParams(oracle), OracleRank(oracle, "Div2"));
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Starburn", desc, PsychicAssets.Facts(ability), Grant(resource));
        }

        private static BlueprintFeature BenefitOfWisdom(Factory f, string token)
        {
            const string desc = "Knowledge (Arcana), Knowledge (World), Lore (Nature) and Lore (Religion) checks use Charisma instead of Wisdom.";
            var components = new[] { "SkillKnowledgeArcana", "SkillKnowledgeWorld", "SkillLoreNature", "SkillLoreReligion" }
                .Select(stat => Component("ReplaceStatBaseAttribute", F("TargetStat", stat), F("BaseAttributeReplacement", "Charisma")))
                .ToArray();
            if (components.Any(c => c == null)) throw new InvalidOperationException("ReplaceStatBaseAttribute missing.");
            return f.Feature(token, "Benefit of Wisdom", desc, components);
        }

        private static BlueprintFeature BrainDrain(Factory f, BlueprintCharacterClass oracle, string token)
        {
            const string desc = "One intelligent enemy within 30 feet takes 1d4 damage per 2 Oracle levels. A Will save negates. This is a mind-affecting effect. A creature that fails its save cannot be the target of this hex again for 24 hours.";
            var damage = new ContextActionDealDamage
            {
                DamageType = new DamageTypeDescription { Type = DamageType.Direct },
                Value = new ContextDiceValue { DiceType = DiceType.D4, DiceCountValue = RankValue(), BonusValue = 0 },
                IgnoreCritical = true, DisableSneakDamage = true
            };
            var ability = f.Ability(token + "Ability", "Brain Drain", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Save(SavingThrowType.Will, PsychicAssets.Saved(Array.Empty<GameAction>(), new GameAction[] { damage })),
                OracleParams(oracle), OracleRank(oracle, "Div2"));
            AddDayLock(f, ability, token, true);
            return f.Feature(token, "Brain Drain", desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature ConfusionCurse(Factory f, BlueprintCharacterClass oracle, string token)
        {
            const string desc = "One intelligent enemy within 30 feet is confused for a number of rounds equal to the Wisdom modifier, minimum 1. A Will save negates. A creature that fails its save cannot be the target of this hex again for 24 hours.";
            var donor = f.Native<BlueprintBuff>(ConfusedBuffId);
            if (donor == null) throw new InvalidOperationException("confused buff missing.");
            var buff = (BlueprintBuff)f.Clone(donor, "ConfusionCurseAbility0060Buff", "Confusion Curse", desc);
            var ability = f.Ability("ConfusionCurseAbility0060", "Confusion Curse", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Save(SavingThrowType.Will, PsychicAssets.Saved(Array.Empty<GameAction>(), new GameAction[] { RankRounds(buff) })),
                OracleParams(oracle), SpiritGuide0060.WisdomRounds(1));
            AddDayLock(f, ability, "ConfusionCurseAbility0060", true);
            return f.Feature(token, "Confusion Curse", desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature ErosionCurse(Factory f, BlueprintCharacterClass oracle, string token)
        {
            const string desc = "A construct within close range takes 1d6 damage per 2 Oracle levels. A Reflex save halves the damage. Whether or not the save is successful, the construct cannot be the target of this hex again for 24 hours.";
            var construct = f.Native<BlueprintFeature>(ConstructFeatureId);
            var damage = new ContextActionDealDamage
            {
                DamageType = new DamageTypeDescription { Type = DamageType.Direct },
                Value = new ContextDiceValue { DiceType = DiceType.D6, DiceCountValue = RankValue(), BonusValue = 0 },
                HalfIfSaved = true, IgnoreCritical = true, DisableSneakDamage = true
            };
            var ability = f.Ability(token + "Ability", "Erosion Curse", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Save(SavingThrowType.Reflex, damage),
                OracleParams(oracle), OracleRank(oracle, "Div2"));
            if (construct != null)
            {
                var gate = new AbilityTargetHasFact { Inverted = false };
                Set(gate, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(construct) });
                ability.ComponentsArray = ability.ComponentsArray.Concat(new BlueprintComponent[] { gate }).ToArray();
            }
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Erosion Curse", desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature Stormwalker(Factory f, BlueprintCharacterClass oracle, string token)
        {
            const string desc = "While the weather is bad, the spirit guide's speed increases by 10 feet. At 10th Oracle level she ignores fog concealment.";
            var gate = new AddFeatureOnClassLevel { Level = 10 };
            Set(gate, "m_Class", Ref<BlueprintCharacterClassReference>(oracle));
            var fog = f.Feature(token + "Fog", "Stormwalker Fog", desc, new FogConcealment0117());
            Set(gate, "m_Feature", Ref<BlueprintFeatureReference>(fog));
            return f.Feature(token, "Stormwalker", desc, new WeatherSpeed0060(), gate);
        }

        private static BlueprintFeature Loadstone(Factory f, string token)
        {
            const string desc = "One creature within close range is slowed, as the Slow spell. A Will save negates. Whether or not the save is successful, the creature cannot be the target of this hex again for 24 hours.";
            var slow = f.Native<BlueprintAbility>(SlowAbilityId);
            if (slow == null) throw new InvalidOperationException("Slow spell missing for Loadstone.");
            var ability = (BlueprintAbility)f.Clone(slow, token + "Ability", "Loadstone", desc);
            ability.Type = AbilityType.Supernatural;
            ability.Range = AbilityRange.Close;
            ability.SpellResistance = false;
            ability.ActionType = UnitCommand.CommandType.Standard;
            StripSpellLists(ability);
            ability.ComponentsArray = ability.ComponentsArray.Where(c => c != null && c.GetType().Name != "SpellComponent" && c.GetType().Name != "AbilityTargetsAround").ToArray();
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Loadstone", desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature WardOfStone(Factory f, string token)
        {
            const string desc = "A touched ally gains DR 5/adamantine for 1 minute. The ward is discharged after one hit, after two hits at 8th Oracle level, or after three hits at 16th. Whether or not the target was hit, it cannot be the target of this hex again for 24 hours.";
            var dr = new AddDamageResistancePhysical { Value = 5, BypassedByMaterial = true, Material = PhysicalDamageMaterial.Adamantite };
            var buff = f.Buff(token + "Buff", "Ward of Stone", desc, dr, new WardCharges0060());
            var ability = f.Ability(token + "Ability", "Ward of Stone", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Touch,
                PsychicAssets.Run(PsychicAssets.Apply(buff, 1, DurationRate.Minutes)));
            ability.CanTargetEnemies = false;
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Ward of Stone", desc, PsychicAssets.Facts(ability));
        }

        private static BlueprintFeature CrashingWaves(Factory f, string token)
        {
            const string desc = "Water spells the spirit guide casts are cast at a higher caster level and can knock a damaged target prone unless it succeeds at a Fortitude save. From 8th Oracle level cold spells are also cast higher. From 16th Oracle level any damaging spell can knock the target prone on a failed Fortitude save.";
            return f.Feature(token, "Crashing Waves", desc, new CrashingWaves0060());
        }

        private static BlueprintFeature MistsShroud(Factory f, string token)
        {
            const string desc = "A willing creature gains blur concealment until an attack misses it because of that concealment, or for 1 minute. At 8th Oracle level the mist lasts through one extra such miss, and at 16th through two. Whether or not the ward was spent, the creature cannot be the target of this hex again for 24 hours.";
            var donor = f.Native<BlueprintBuff>(BlurBuffId);
            if (donor == null) throw new InvalidOperationException("blur buff missing.");
            var buff = (BlueprintBuff)f.Clone(donor, token + "Buff", "Mist's Shroud", desc);
            buff.ComponentsArray = buff.ComponentsArray.Concat(new BlueprintComponent[] { new MistCharges0060() }).ToArray();
            var ability = f.Ability(token + "Ability", "Mist's Shroud", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Touch, PsychicAssets.Run(PsychicAssets.Apply(buff, 1, DurationRate.Minutes)));
            ability.CanTargetEnemies = false;
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Mist's Shroud", desc, PsychicAssets.Facts(ability));
        }

        // Q08: Fluid Magic is a semantic marker. The bonded spells already ride
        // the native temporary-known ledger and are cast from normal Oracle
        // slots; the hex occupies one hex choice and carries the original text.
        private static BlueprintFeature FluidMagic(Factory f, string token)
        {
            const string desc = "The spirit guide's magic is not constrained by the reservoirs of magic that hold others back. Her bonded spirit's spells are cast from her normal Oracle spell slots.";
            var feature = f.Feature(token, "Fluid Magic", desc);
            Set(feature, "m_Icon", f.Native<BlueprintUnitFact>(FluidMagicIconId).Icon);
            return feature;
        }

        private static BlueprintFeature SparklingAura(Factory f, BlueprintCharacterClass oracle, string token)
        {
            const string desc = "A creature within close range glows for 1 round per 2 Oracle levels. A metal weapon that hits it deals extra electricity damage to that creature equal to the spirit guide's Wisdom modifier. Whether or not it was hit, the creature cannot be the target of this hex again for 24 hours.";
            var buff = f.Buff("SparklingAuraBuff0060", "Sparkling Aura", desc, new SparklingAura0060());
            var ability = f.Ability(token + "Ability", "Sparkling Aura", desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Run(RankRounds(buff)), OracleRank(oracle, "Div2"));
            AddDayLock(f, ability, token, false);
            return f.Feature(token, "Sparkling Aura", desc, PsychicAssets.Facts(ability));
        }

        // Nature.cs createFriendToAnimals, built privately: the donor's native
        // effect buff reads Charisma and its aura chain is never dereferenced by
        // the isolated host's cache, so the effect buff, the 30-foot area and the
        // animal gate are ours from scratch (Wisdom secondary, minimum 0) while
        // the carrier buff and the spontaneous summon conversion are cloned from
        // the loadable native donor.
        private static BlueprintFeature FriendToAnimals(Factory f, BlueprintCharacterClass oracle, string token)
        {
            const string desc = "The spirit guide can spontaneously cast summon nature's ally spells as a druid. In addition, all animals within 30 feet of the spirit guide receive a sacred bonus on all saving throws equal to her Wisdom modifier (minimum 0).";
            var animal = f.Native<BlueprintFeature>(AnimalFactId);
            var donor = f.Native<BlueprintFeature>(FriendDonorId);
            var carrierDonor = f.Native<BlueprintBuff>(FriendCarrierBuffId);
            if (animal == null || donor == null || carrierDonor == null)
                throw new InvalidOperationException("0060 Friend to Animals native donors missing.");

            var effect = f.Buff(token + "Buff", "Friend to Animals", desc,
                Component("AddContextStatBonus", F("Stat", "SaveFortitude"), F("Descriptor", "Sacred"), F("Value", RankValue())),
                Component("AddContextStatBonus", F("Stat", "SaveReflex"), F("Descriptor", "Sacred"), F("Value", RankValue())),
                Component("AddContextStatBonus", F("Stat", "SaveWill"), F("Descriptor", "Sacred"), F("Value", RankValue())),
                SpiritGuide0060.WisdomRounds(0));

            var area = f.Add(new BlueprintAbilityAreaEffect { Size = 30.Feet() }, token + "Area", "Friend to Animals", desc);
            var areaBuff = new Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectBuff();
            Set(areaBuff, "m_Buff", Ref<BlueprintBuffReference>(effect));
            var animalCondition = new Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasFact();
            Set(animalCondition, "m_Fact", Ref<BlueprintUnitFactReference>(animal));
            areaBuff.Condition = new Kingmaker.ElementsSystem.ConditionsChecker
            {
                Operation = Kingmaker.ElementsSystem.Operation.Or,
                Conditions = new Kingmaker.ElementsSystem.Condition[] { animalCondition }
            };
            area.ComponentsArray = new BlueprintComponent[] { areaBuff };

            var carrier = (BlueprintBuff)f.Clone(carrierDonor, token + "Aura", "Friend to Animals", desc);
            foreach (var addArea in carrier.ComponentsArray.OfType<Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect>())
                Set(addArea, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(area));

            var facts = new List<BlueprintUnitFact> { carrier };
            foreach (var component in donor.ComponentsArray)
            {
                if (component == null || component.GetType().Name != "AddFacts") continue;
                var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
                foreach (var reference in refs)
                {
                    var factDonor = reference?.Get() as BlueprintFeature;
                    if (factDonor == null || !factDonor.ComponentsArray.Any(c => c != null && c.GetType().Name == "SpontaneousSpellConversion")) continue;
                    var clone = (BlueprintFeature)f.Clone(factDonor, token + "Summon", factDonor.name + " (Spirit Guide)",
                        "Spirit Guide spontaneous summon conversion uses Oracle spell slots.");
                    foreach (var conversion in clone.ComponentsArray.Where(c => c != null && c.GetType().Name == "SpontaneousSpellConversion"))
                        Set(conversion, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(oracle));
                    facts.Add(clone);
                }
            }
            var feature = f.Feature(token, "Friend to Animals", desc, PsychicAssets.Facts(facts.ToArray()));
            Icon(feature, donor);
            return feature;
        }

        // Life Sight: one point per Oracle level, a free-action toggle that spends
        // one point per new round, gated at Oracle 12.
        private static BlueprintFeature LifeSight(Factory f, BlueprintCharacterClass oracle, string token)
        {
            const string desc = "The spirit guide sees the life force of nearby creatures, gaining blindsight 30 feet and immunity to gaze attacks. This toggle spends one use per round while active.";
            var resource = Resource(f, token + "R5", 0, null, true);
            var gazeSpell = new SpellImmunityToSpellDescriptor { Descriptor = SpellDescriptor.GazeAttack };
            var gazeBuff = new BuffDescriptorImmunity { Descriptor = SpellDescriptor.GazeAttack };
            Set(gazeSpell, "m_CasterIgnoreImmunityFact", new BlueprintUnitFactReference());
            Set(gazeBuff, "m_IgnoreFeature", new BlueprintUnitFactReference());
            var buff = f.Buff(token + "Buff", "Life Sight", desc,
                new Blindsense { Range = new Feet(30), Blindsight = true }, gazeSpell, gazeBuff);
            var spend = new ActivatableAbilityResourceLogic { SpendType = ActivatableAbilityResourceLogic.ResourceSpendType.NewRound };
            Set(spend, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
            var toggle = f.Add(new BlueprintActivatableAbility
            {
                ActivationType = AbilityActivationType.WithUnitCommand,
                Group = ActivatableAbilityGroup.None,
                DeactivateIfCombatEnded = true,
                ComponentsArray = new BlueprintComponent[] { spend }
            }, token + "Toggle", "Life Sight", desc);
            Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(buff));
            Icon(toggle, buff);
            var gate = Component("PrerequisiteClassLevel", F("m_CharacterClass", Ref<BlueprintCharacterClassReference>(oracle)), F("Level", 12));
            var components = new List<BlueprintComponent> { PsychicAssets.Facts(toggle), Grant(resource) };
            if (gate != null) components.Add(gate);
            return f.Feature(token, "Life Sight", desc, components.ToArray());
        }

        // ================= private tiers =================

        // Heavens.cs createSpiritAbility: Stardust. A private faerie-fire buff
        // imposes a stacking penalty to attack rolls and sight-based Perception
        // that grows with the Oracle level; the ability is a close-range
        // supernatural standard action spending one R1 use.
        private static BlueprintFeature HeavensStardust(Factory f, BlueprintCharacterClass oracle, string token, string display)
        {
            const string desc = "Stardust materializes around one creature within 30 feet. It sheds light as a candle, cannot benefit from concealment or invisibility, and takes a penalty on attack rolls and sight-based Perception checks that increases by 1 at 4th level and every 4 Oracle levels thereafter. The effect lasts 1 round per 2 Oracle levels. Sightless creatures are unaffected. The spirit guide can use this ability 3 + Charisma modifier times per day.";
            var resource = Resource(f, token + "R1", 3, StatType.Charisma);
            var buff = (BlueprintBuff)f.Clone(f.Native<BlueprintBuff>(FaerieFireBuffId), token + "Buff", display, desc);
            buff.ComponentsArray = buff.ComponentsArray.Concat(new[]
            {
                Component("AddContextStatBonus", F("Stat", "SkillPerception"), F("Descriptor", "UntypedStackable"), F("Value", RankValue(AbilityRankType.StatBonus)), F("Multiplier", -1)),
                Component("AddContextStatBonus", F("Stat", "AdditionalAttackBonus"), F("Descriptor", "UntypedStackable"), F("Value", RankValue(AbilityRankType.StatBonus)), F("Multiplier", -1)),
                Component("ContextRankConfig", F("m_BaseValueType", "ClassLevel"), F("m_Class", new[] { Ref<BlueprintCharacterClassReference>(oracle) }), F("m_Progression", "OnePlusDivStep"), F("m_StepLevel", 4), F("m_Type", "StatBonus")),
            }).Where(c => c != null).ToArray();
            Set(buff, "m_Icon", f.Native<BlueprintUnitFact>(GlitterdustId).Icon);

            var ability = f.Ability(token + "Ability", display, desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Close,
                PsychicAssets.Run(RankRounds(buff)),
                Component("ContextRankConfig", F("m_BaseValueType", "ClassLevel"), F("m_Class", new[] { Ref<BlueprintCharacterClassReference>(oracle) }), F("m_Progression", "Div2"), F("m_UseMin", true), F("m_Min", 1)),
                Cost(resource));
            return f.Feature(token, display, desc, PsychicAssets.Facts(ability), Grant(resource));
        }

        // Heavens.cs createGreaterSpiritAbility: Void Adaptation. Wrath has no
        // suffocation buff or condition, so the immunity that can be expressed
        // is poison plus blindsense (0117 precedent, D-approved).
        private static BlueprintFeature HeavensVoid(Factory f, string token, string display)
        {
            const string desc = "The spirit guide gains blindsense 30 feet and immunity to poison and suffocation.";
            var poison = new BuffDescriptorImmunity { Descriptor = SpellDescriptor.Poison };
            var sense = new Blindsense { Range = new Feet(30), Blindsight = false };
            var feature = f.Feature(token, display, desc, poison, sense);
            Set(feature, "m_Icon", f.Native<BlueprintUnitFact>(BlurIconId).Icon);
            return feature;
        }

        // Lore.cs createSpiritAbility: Monstrous Insight. A real Knowledge check
        // with an insight bonus equal to the Oracle level, plus a 1-minute +2
        // insight attack/AC buff against the inspected creature.
        private static BlueprintFeature MonstrousInsight(Factory f, BlueprintCharacterClass oracle, string token, string display)
        {
            const string desc = "As a standard action, attempt the Knowledge or Lore check appropriate to the target's type, with an insight bonus equal to the Oracle level. For 1 minute the spirit guide also gains a +2 insight bonus on attack rolls against that creature and to AC against its attacks. She can use this ability 3 + Charisma modifier times per day.";
            var resource = Resource(f, token + "R1", 3, StatType.Charisma);
            var attack = new AttackBonusAgainstTarget { Value = new ContextValue { Value = 2 }, Descriptor = ModifierDescriptor.Insight, CheckCaster = true };
            var ac = new ACBonusAgainstTarget { Value = new ContextValue { Value = 2 }, Descriptor = ModifierDescriptor.Insight, CheckCaster = true };
            var buff = f.Buff(token + "Buff", display, desc, attack, ac);
            var ability = f.Ability(token + "Ability", display, desc, AbilityType.Supernatural,
                UnitCommand.CommandType.Standard, AbilityRange.Touch,
                PsychicAssets.Run(new MonsterLore0060(), PsychicAssets.Apply(buff, 1, DurationRate.Minutes)),
                Cost(resource));
            return f.Feature(token, display, desc, PsychicAssets.Facts(ability), Grant(resource));
        }

        // Lore.cs createGreaterSpiritAbility: Arcane Enlightenment, three picks at
        // 15/17/19. The native parametrized spell-picking UI offers the wizard
        // list minus the Oracle base list one circle at a time, gated by
        // Intelligence, Charisma and the Oracle spell level. The result rides the
        // native temporary-known ledger through a private grant feature, never a
        // permanent LearnSpellParametrized.
        private static BlueprintFeature ArcaneEnlightenment(Factory f, BlueprintCharacterClass oracle, string token, string display)
        {
            const string desc = "The spirit guide's native intelligence grants her the ability to tap into arcane lore. She can add a spell from the sorcerer/wizard spell list to the list of Oracle spells she knows. To select a spell she must have both Intelligence and Charisma scores equal to 10 + the spell's level, and be able to cast Oracle spells of that level. She can add an additional spell every two levels thereafter. These spells are temporary known spells and are removed if the greater spirit power is removed.";
            var wizardList = f.Native<BlueprintSpellList>(WizardSpellListId);
            var oracleList = f.Native<BlueprintSpellList>(OracleSpellListId);
            if (wizardList == null || oracleList == null) throw new InvalidOperationException("0060 Arcane Enlightenment spell lists missing.");

            var excluded = oracleList.SpellsByLevel
                .SelectMany(e => (List<BlueprintAbilityReference>)Get(e, "m_Spells"))
                .Where(r => r != null).Select(r => r.Guid).ToHashSet();
            var rows = SpellListCompatibility.Build(wizardList.SpellsByLevel).Select(e =>
            {
                var copy = new SpellLevelList(e.SpellLevel);
                Set(copy, "m_Spells", ((List<BlueprintAbilityReference>)Get(e, "m_Spells"))
                    .Where(r => r != null && !excluded.Contains(r.Guid)).Select(PsychicAssets.CopyData).ToList());
                return copy;
            }).ToArray();
            var learnList = f.Add(new BlueprintSpellList { SpellsByLevel = rows }, token + "List0060", "", "");

            var selection = f.Selection(token + "Selection0060", display, desc);
            var children = new List<BlueprintFeatureReference>();
            int grants = 0;
            for (int circle = 1; circle <= 9; circle++)
            {
                var learn = (BlueprintParametrizedFeature)f.Clone(f.Native<BlueprintParametrizedFeature>(ParametrizedFeatureId),
                    token + circle + "0060", display + " (Level " + circle + ")", desc);
                learn.SpecificSpellLevel = true;
                learn.SpellLevel = circle;
                learn.SpellLevelPenalty = 0;
                Set(learn, "m_SpellList", Ref<BlueprintSpellListReference>(learnList));
                Set(learn, "m_SpellcasterClass", Ref<BlueprintCharacterClassReference>(oracle));
                Set(learn, "m_CachedItems", null);
                Set(learn, "m_Icon", f.Native<BlueprintUnitFact>(RestorationIconId).Icon);
                var temp = new LearnTemporarySpellParametrized0060 { Circle = circle };
                learn.ComponentsArray = new BlueprintComponent[]
                {
                    temp,
                    new PrerequisiteStatValue { name = token + "Int" + circle, Stat = StatType.Intelligence, Value = 10 + circle },
                    new PrerequisiteStatValue { name = token + "Cha" + circle, Stat = StatType.Charisma, Value = 10 + circle },
                    PrerequisiteSpellLevel(oracle, circle)
                };

                var level = rows.FirstOrDefault(r => r.SpellLevel == circle);
                var levelSpells = (List<BlueprintAbilityReference>)(level == null ? null : Get(level, "m_Spells")) ?? new List<BlueprintAbilityReference>();
                foreach (var reference in levelSpells)
                {
                    var spell = reference.Get() as BlueprintAbility;
                    if (spell == null) continue;
                    var grant = f.Feature(token + "Grant0060" + spell.AssetGuid, display + ": " + spell.name,
                        "Adds " + spell.name + " to the Oracle spellbook as a temporary known spell while this Spirit Guide feature is active.",
                        SpiritGuide0060.TemporarySpell(oracle, spell, circle));
                    grant.HideInUI = true;
                    temp.Grants[spell.AssetGuid.ToString()] = grant;
                    grants++;
                }
                children.Add(Ref<BlueprintFeatureReference>(learn));
            }
            selection.Ranks = 3;
            Set(selection, "m_AllFeatures", children.ToArray());
            Set(selection, "m_Features", children.ToArray());

            var entries = new[] { Entry(15, selection), Entry(17, selection), Entry(19, selection) };
            var progression = f.Progression(token, display, desc, entries, new[] { Group(selection) });
            Set(progression, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(oracle) } });
            Diagnostics.Add("Arcane Enlightenment: " + grants + " grant features over 9 circles");
            return progression;
        }

        private static PrerequisiteClassSpellLevel PrerequisiteSpellLevel(BlueprintCharacterClass oracle, int circle)
        {
            var p = new PrerequisiteClassSpellLevel { name = "0060ArcaneEnlightenmentSpellLevel" + circle, RequiredSpellLevel = circle };
            Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(oracle));
            return p;
        }

        // Life.cs createGreaterSpiritAbility: Healer's Touch, Lore (Religion) +3
        // untyped stacking (not a competence bonus).
        private static BlueprintFeature HealersTouch(Factory f, string token, string display)
        {
            const string desc = "The spirit guide gains a +3 bonus on Lore (Religion) checks.";
            var bonus = new AddStatBonus { Stat = StatType.SkillLoreReligion, Value = 3, Descriptor = ModifierDescriptor.UntypedStackable };
            var feature = f.Feature(token, display, desc, bonus);
            Set(feature, "m_Icon", f.Native<BlueprintUnitFact>(OracleLifeIconId).Icon);
            return feature;
        }
        private const string OracleLifeIconId = "f6f95242abdfac346befd6f4f6222140";

        // The private Extra Channel clone: +2 uses of this package's Life channel
        // pool only, requiring this package's channel feature.
        private static void ExtraChannel(Factory f, BlueprintCharacterClass oracle, BlueprintFeature lifeChannel)
        {
            var nativeExtra = f.Native<BlueprintFeature>(ExtraChannelId);
            var extra = (BlueprintFeature)f.Clone(nativeExtra, "LifeExtraChannel0060",
                "Extra Channel (Spirit Guide Life Spirit)",
                "You can channel the Life spirit's positive energy two additional times per day. Requires Channel Positive Energy from this archetype.");
            var increase = extra.ComponentsArray.FirstOrDefault(c => c != null && c.GetType().Name == "IncreaseResourceAmount");
            if (increase == null) throw new InvalidOperationException("native Extra Channel resource component changed.");
            var lifeResource = f.Find<BlueprintAbilityResource>("LifeTier0R20060");
            Set(increase, "m_Resource", Ref<BlueprintAbilityResourceReference>(lifeResource));
            Set(increase, "Value", 2);
            extra.ComponentsArray = extra.ComponentsArray.Where(c => !(c is Prerequisite))
                .Concat(new BlueprintComponent[] { Migration01426.Require(lifeChannel) }).ToArray();
        }

        // Publish the private Extra Channel into every native feat pool that
        // already offers the native Extra Channel feat (0117 gate).
        internal static void ConnectExtraChannel(BlueprintsCache cache, Factory f)
        {
            var extra = f.Find<BlueprintFeature>("LifeExtraChannel0060");
            var nativeExtraGuid = BlueprintGuid.Parse(ExtraChannelId);
            foreach (var guid in ExtraChannelPoolIds)
            {
                var pool = cache.Load(BlueprintGuid.Parse(guid)) as BlueprintFeatureSelection;
                if (pool == null) continue;
                var all = (BlueprintFeatureReference[])Get(pool, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
                if (!all.Any(r => r != null && r.Guid == nativeExtraGuid)) continue;
                foreach (string field in new[] { "m_Features", "m_AllFeatures" })
                {
                    var old = (BlueprintFeatureReference[])Get(pool, field) ?? Array.Empty<BlueprintFeatureReference>();
                    if (!old.Any(r => r != null && r.Guid == extra.AssetGuid))
                        Set(pool, field, old.Concat(new[] { Ref<BlueprintFeatureReference>(extra) }).ToArray());
                }
            }
        }

        internal static object[][] SnapshotPools(BlueprintsCache cache) =>
            ExtraChannelPoolIds.Select(g => cache.Load(BlueprintGuid.Parse(g)))
                .Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();

        internal static void RestorePools(BlueprintsCache cache, object[][] snapshot)
        {
            for (int i = 0; i < ExtraChannelPoolIds.Length; i++)
            {
                var pool = cache.Load(BlueprintGuid.Parse(ExtraChannelPoolIds[i]));
                Set(pool, "m_Features", snapshot[i][0]);
                Set(pool, "m_AllFeatures", snapshot[i][1]);
            }
        }

        // ================= clone rebinding =================
        // The private graph is closed by SpiritGuideGraph0060 (D-F01); these two
        // helpers hold the stat swap used both there and on the cloned feature.

        // The original spirit guide "switches Wisdom for Charisma and vice versa":
        // donor primary Wisdom becomes Charisma on every StatBonus rank in the
        // private graph, and for hexes donor secondary Charisma becomes Wisdom
        // (tiers keep Charisma on both channels per the frozen contract).
        internal static void SwapStats(BlueprintScriptableObject bp, bool hexStats)
        {
            foreach (var rank in (bp.ComponentsArray ?? Array.Empty<BlueprintComponent>()).OfType<ContextRankConfig>())
                SwapRankStat(rank, hexStats);
        }

        private static void SwapRankStat(ContextRankConfig rank, bool hexStats)
        {
            if ((ContextRankBaseValueType)Get(rank, "m_BaseValueType") != ContextRankBaseValueType.StatBonus) return;
            var name = Get(rank, "m_Stat")?.ToString();
            if (name == "Wisdom") Set(rank, "m_Stat", StatType.Charisma);
            else if (hexStats && name == "Charisma") Set(rank, "m_Stat", StatType.Wisdom);
        }

        // Rebind a private clone from Shaman/Wisdom donors onto the real Oracle
        // class and Charisma (primary). Returns a human-readable audit list.
        internal static List<string> Rebind(object root, BlueprintCharacterClass oracle, StatType dcStat,
            Func<BlueprintReferenceBase, BlueprintAbilityResource> mapResource)
        {
            var audit = new List<string>();
            var seen = new HashSet<object>(PsychicAssets.IdentityComparer);
            var start = root is BlueprintScriptableObject bp ? (object)bp.ComponentsArray : root;
            Walk(start, seen, o =>
            {
                var type = o.GetType();
                var name = type.Name;
                if (name == "ContextRankConfig" && Has(o, "m_Class"))
                {
                    Set(o, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(oracle) });
                    audit.Add("rank->Oracle");
                }
                if (name == "ContextCalculateAbilityParamsBasedOnClass")
                {
                    if (Has(o, "m_CharacterClass")) Set(o, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(oracle));
                    if (Has(o, "StatType")) Set(o, "StatType", dcStat);
                    audit.Add("params->Oracle/" + dcStat);
                }
                if (name == "AddFeatureOnClassLevel" && Has(o, "m_Class"))
                {
                    var field = AccessTools.Field(type, "m_Class");
                    object value = field.FieldType.IsArray
                        ? (object)new[] { Ref<BlueprintCharacterClassReference>(oracle) }
                        : Ref<BlueprintCharacterClassReference>(oracle);
                    field.SetValue(o, value);
                    audit.Add("classLevel->Oracle");
                }
                if (name == "SpontaneousSpellConversion" && Has(o, "m_CharacterClass"))
                {
                    Set(o, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(oracle));
                    audit.Add("conversion->Oracle");
                }
                if ((name.Contains("ResourceLogic") && Has(o, "m_RequiredResource")) ||
                    (name == "AddAbilityResources" && Has(o, "m_Resource")))
                {
                    string field = name == "AddAbilityResources" ? "m_Resource" : "m_RequiredResource";
                    var donor = Get(o, field) as BlueprintReferenceBase;
                    if (donor != null)
                    {
                        var replacement = mapResource(donor);
                        if (replacement != null)
                        {
                            Set(o, field, Ref<BlueprintAbilityResourceReference>(replacement));
                            audit.Add("resource->" + replacement.name);
                        }
                    }
                }
            });
            return audit;
        }

        internal static void Walk(object value, HashSet<object> seen, Action<object> visit)
        {
            if (value == null || value is string || value is Type || value is Delegate ||
                value.GetType().IsValueType || value is SimpleBlueprint ||
                value is BlueprintReferenceBase || value is UnityEngine.Object) return;
            if (!seen.Add(value)) return;
            visit(value);
            if (value is System.Collections.IEnumerable items)
            {
                foreach (var item in items) Walk(item, seen, visit);
                return;
            }
            for (var t = value.GetType(); t != null && t != typeof(object) &&
                 t != typeof(BlueprintComponent) && t != typeof(Element); t = t.BaseType)
                foreach (var fieldInfo in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!fieldInfo.IsStatic) Walk(fieldInfo.GetValue(value), seen, visit);
        }

        internal static int StripSpellLists(BlueprintScriptableObject bp)
        {
            var kept = (bp.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Where(c => !(c != null && c.GetType().Name.Contains("AddSpecialSpellList"))).ToArray();
            int removed = (bp.ComponentsArray?.Length ?? 0) - kept.Length;
            bp.ComponentsArray = kept;
            return removed;
        }

        private static bool HasConsumption(BlueprintScriptableObject bp)
        {
            bool found = false;
            Walk(bp.ComponentsArray, new HashSet<object>(PsychicAssets.IdentityComparer), o =>
            {
                var name = o.GetType().Name;
                if (name.Contains("ResourceLogic") && Has(o, "m_RequiredResource")) found = true;
            });
            return found;
        }

        private static BlueprintAbilityResource Resource(Factory f, string token, int baseValue, StatType? bonusStat, bool perLevel = false)
        {
            var r = f.Add(new BlueprintAbilityResource(), token, "", "");
            var amount = Get(r, "m_MaxAmount");
            Set(amount, "BaseValue", baseValue);
            Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            if (bonusStat.HasValue)
            {
                Set(amount, "IncreasedByStat", true);
                Set(amount, "ResourceBonusStat", bonusStat.Value);
            }
            if (perLevel)
            {
                Set(amount, "IncreasedByLevel", true);
                Set(amount, "LevelIncrease", 1);
            }
            Set(r, "m_MaxAmount", amount);
            if (Has(r, "m_Min")) Set(r, "m_Min", 0);
            return r;
        }

        // R3: fixed three uses plus a private 1d4-round cooldown. The cooldown
        // fact is checked by a native caster restriction and applied by a real
        // effect action on every successfully executed leaf ability.
        private static void AddCooldown(Factory f, BlueprintFeature feature, string token)
        {
            var buff = f.Buff(token + "Cooldown", "Spirit Guide cooldown",
                "This spirit power cannot be used again while this cooldown lasts.");
            var restriction = new AbilityCasterHasNoFacts();
            Set(restriction, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(buff) });
            var apply = new ContextActionApplyBuff { ToCaster = true, IsFromSpell = false,
                DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = DiceType.D4, DiceCountValue = 1, BonusValue = 0 } };
            Set(apply, "m_Buff", Ref<BlueprintBuffReference>(buff));
            foreach (var clone in f.Assets.Values.OfType<BlueprintAbility>().Where(a => a.name.Contains(token)))
            {
                var actions = clone.ComponentsArray.OfType<AbilityEffectRunAction>().ToArray();
                if (actions.Length == 0) continue;
                clone.ComponentsArray = clone.ComponentsArray.Concat(new BlueprintComponent[] { restriction }).ToArray();
                foreach (var run in actions)
                {
                    var list = Get(run, "Actions");
                    var old = (GameAction[])Get(list, "Actions") ?? Array.Empty<GameAction>();
                    Set(list, "Actions", old.Concat(new GameAction[] { apply }).ToArray());
                }
            }
        }

        // A 24-hour per-target lock: a native inverted AbilityTargetHasFact gate
        // plus a non-dispellable 1-day mark, appended to the failure branch when
        // the original hex only locks after a failed save.
        private static void AddDayLock(Factory f, BlueprintAbility ability, string token, bool onlyOnSuccess)
        {
            var mark = f.Buff(token + "Day", "Already targeted",
                "This creature cannot be the target of this hex again for 24 hours.");
            var gate = new AbilityTargetHasFact { Inverted = true };
            Set(gate, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(mark) });
            var apply = PsychicAssets.Apply(mark, 1, DurationRate.Days, false, false);
            apply.IsNotDispelable = true;
            if (onlyOnSuccess)
            {
                var saved = ability.ComponentsArray.OfType<AbilityEffectRunAction>()
                    .SelectMany(run => run.Actions.Actions ?? Array.Empty<GameAction>())
                    .OfType<ContextActionConditionalSaved>().FirstOrDefault();
                if (saved != null && saved.Failed != null)
                    saved.Failed.Actions = (saved.Failed.Actions ?? Array.Empty<GameAction>()).Concat(new GameAction[] { apply }).ToArray();
            }
            else ability.ComponentsArray = ability.ComponentsArray.Concat(new BlueprintComponent[] { PsychicAssets.Run(apply) }).ToArray();
            ability.ComponentsArray = ability.ComponentsArray.Concat(new BlueprintComponent[] { gate }).ToArray();
        }

        private static void FixMentalAcuity(BlueprintFeature feature)
        {
            foreach (var rank in feature.GetComponents<ContextRankConfig>())
            {
                PsychicAssets.SetEnum(rank, "m_Progression", "OnePlusDivStep");
                Set(rank, "m_StepLevel", 5);
                Set(rank, "m_StartLevel", 0);
                var field = AccessTools.Field(rank.GetType(), "m_CustomProgression");
                if (field != null && field.FieldType.IsArray)
                    field.SetValue(rank, Array.CreateInstance(field.FieldType.GetElementType(), 0));
            }
        }

        private static void FixStatMinimum(BlueprintFeature feature, string stat, int minimum)
        {
            foreach (var ability in GrantedAbilities(feature))
                foreach (var rank in ability.GetComponents<ContextRankConfig>())
                {
                    if (Get(rank, "m_Stat")?.ToString() != stat) continue;
                    Set(rank, "m_UseMin", true);
                    Set(rank, "m_Min", minimum);
                }
            foreach (var granted in GrantedFacts(feature))
                foreach (var rank in granted.GetComponents<ContextRankConfig>())
                {
                    if (Get(rank, "m_Stat")?.ToString() != stat) continue;
                    Set(rank, "m_UseMin", true);
                    Set(rank, "m_Min", minimum);
                }
            foreach (var rank in feature.GetComponents<ContextRankConfig>())
            {
                if (Get(rank, "m_Stat")?.ToString() != stat) continue;
                Set(rank, "m_UseMin", true);
                Set(rank, "m_Min", minimum);
            }
        }

        private static IEnumerable<BlueprintUnitFact> GrantedFacts(BlueprintFeature feature)
        {
            foreach (var component in feature.ComponentsArray)
            {
                if (component == null || component.GetType().Name != "AddFacts" || !Has(component, "m_Facts")) continue;
                var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
                foreach (var reference in refs)
                    if (reference?.Get() is BlueprintUnitFact fact) yield return fact;
            }
        }

        // Friend to Animals: the donor's spontaneous summon conversion must use
        // the Oracle class before the Oracle spellbook can drive it.
        private static void RetargetSummon(Factory f, BlueprintCharacterClass oracle, BlueprintFeature feature, string token)
        {
            foreach (var component in feature.ComponentsArray)
            {
                if (component == null || component.GetType().Name != "AddFacts" || !Has(component, "m_Facts")) continue;
                var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
                var rewritten = new List<BlueprintUnitFactReference>();
                int index = 0;
                foreach (var reference in refs)
                {
                    var donor = reference?.Get() as BlueprintFeature;
                    bool summon = donor != null && donor.ComponentsArray.Any(c => c != null && c.GetType().Name == "SpontaneousSpellConversion");
                    if (!summon) { rewritten.Add(reference); index++; continue; }
                    var clone = (BlueprintFeature)f.Clone(donor, token + "Summon" + index, donor.name + " (Spirit Guide)",
                        "Spirit Guide spontaneous summon conversion uses Oracle spell slots.");
                    foreach (var conversion in clone.ComponentsArray.Where(c => c != null && c.GetType().Name == "SpontaneousSpellConversion"))
                        Set(conversion, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(oracle));
                    rewritten.Add(Ref<BlueprintUnitFactReference>(clone));
                    index++;
                }
                Set(component, "m_Facts", rewritten.ToArray());
            }
        }

        // Ending a Life Link refunds exactly one R5 point to the caster. The
        // donor's life-link buff is matched by name: the Kingmaker-era fixed GUID
        // does not exist in the Wrath library, so identity alone would miss it.
        private static void AddLifeLinkRefund(Factory f, BlueprintFeature feature, BlueprintAbilityResource resource)
        {
            BlueprintBuff refund = null;
            var applies = new List<ContextActionApplyBuff>();
            Walk(feature.ComponentsArray, new HashSet<object>(PsychicAssets.IdentityComparer), o =>
            {
                if (o is ContextActionApplyBuff apply) applies.Add(apply);
            });
            foreach (var apply in applies)
            {
                var buff = (Get(apply, "m_Buff") as BlueprintBuffReference)?.Get();
                if (buff == null) continue;
                var buffName = buff.name ?? "";
                if (buffName.IndexOf("LifeLink", StringComparison.OrdinalIgnoreCase) < 0 &&
                    buffName.IndexOf("Life Link", StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (refund == null)
                {
                    refund = (BlueprintBuff)f.Clone(buff, "LifeLinkRefundBuff0060", "Life Link",
                        "When this link ends, one use of the spirit guide's Life Link pool is restored.");
                    var restore = new ContextRestoreResource();
                    Set(restore, "Resource", Ref<BlueprintAbilityResourceReference>(resource));
                    Set(restore, "Value", new ContextValue { Value = 1 });
                    var onCaster = WrapOnContextCaster(restore);
                    var ctx = new AddFactContextActions { Activated = PsychicAssets.Actions(), Deactivated = PsychicAssets.Actions(onCaster), NewRound = PsychicAssets.Actions() };
                    refund.ComponentsArray = refund.ComponentsArray.Concat(new BlueprintComponent[] { ctx }).ToArray();
                }
                Set(apply, "m_Buff", Ref<BlueprintBuffReference>(refund));
            }
        }

        private static GameAction WrapOnContextCaster(GameAction action)
        {
            var type = FindType("ContextActionOnContextCaster", typeof(GameAction));
            if (type == null) return action;
            var wrapper = (GameAction)Activator.CreateInstance(type);
            var field = AccessTools.Field(type, "Action") ?? AccessTools.Field(type, "m_Action");
            if (field != null) field.SetValue(wrapper, action);
            return wrapper;
        }

        private static Type FindType(string typeName, Type baseType)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException error) { types = error.Types; }
                foreach (var t in types)
                    if (t != null && t.Name == typeName && (baseType == null || baseType.IsAssignableFrom(t)))
                        return t;
            }
            return null;
        }

        private static IEnumerable<BlueprintAbility> GrantedAbilities(BlueprintFeature feature)
        {
            foreach (var component in feature.ComponentsArray)
            {
                if (component == null || component.GetType().Name != "AddFacts" || !Has(component, "m_Facts")) continue;
                var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
                foreach (var reference in refs)
                    if (reference?.Get() is BlueprintAbility ability) yield return ability;
            }
        }

        // Rank that reads the real Oracle class level with the given progression.
        private static ContextRankConfig OracleRank(BlueprintCharacterClass oracle, string progression = "AsIs", int minimum = 0)
        {
            var rank = new ContextRankConfig();
            PsychicAssets.SetEnum(rank, "m_BaseValueType", "ClassLevel");
            PsychicAssets.SetEnum(rank, "m_Progression", progression);
            Set(rank, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(oracle) });
            if (minimum > 0) { Set(rank, "m_UseMin", true); Set(rank, "m_Min", minimum); }
            if (progression == "Div2") { Set(rank, "m_UseMin", true); Set(rank, "m_Min", 1); }
            return rank;
        }

        private static ContextCalculateAbilityParamsBasedOnClass OracleParams(BlueprintCharacterClass oracle)
        {
            var p = new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Charisma };
            Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(oracle));
            return p;
        }

        private static ContextValue RankValue(AbilityRankType type = AbilityRankType.Default)
            => new ContextValue { ValueType = ContextValueType.Rank, ValueRank = type };

        private static ContextActionApplyBuff RankRounds(BlueprintBuff buff)
        {
            var apply = PsychicAssets.Apply(buff, 0);
            apply.DurationValue.BonusValue = RankValue();
            return apply;
        }

        private static KeyValuePair<string, object> F(string key, object value) => new KeyValuePair<string, object>(key, value);

        private static BlueprintComponent Component(string typeName, params KeyValuePair<string, object>[] fields)
        {
            var type = FindType(typeName, typeof(BlueprintComponent));
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
                else if (value is int number && target != typeof(int) && target.Name == "ContextValue")
                    value = new ContextValue { Value = number };
                if (field != null) field.SetValue(component, value);
                else if (property != null && property.GetSetMethod(true) != null) property.SetValue(component, value, null);
            }
            return component;
        }
    }

    // Q10: a new Heaven's Leap selection clears only this caster's previous mark,
    // so another spirit guide's marked ally is never moved or cleared.
    [TypeId("0060b1a2c3d4e5f60718293a4b5c0d01")]
    internal sealed class HeavensLeapMark0060 : UnitFactComponentDelegate
    {
        protected override void OnActivate()
        {
            var caster = Fact?.MaybeContext?.MaybeCaster;
            if (caster == null) return;
            foreach (var unit in HeavensLeap0060.Candidates(caster))
            {
                if (unit?.Buffs == null || ReferenceEquals(unit, Owner)) continue;
                foreach (var buff in unit.Buffs.Enumerable.Where(b => b.Blueprint == Fact.Blueprint).ToArray())
                    if (ReferenceEquals(buff.Context?.MaybeCaster, caster)) buff.Remove();
            }
        }
    }

    [TypeId("0060b1a2c3d4e5f60718293a4b5c0d02")]
    internal sealed class HeavensLeap0060 : AbilityCustomDimensionDoor
    {
        internal BlueprintBuff Mark;
        internal BlueprintBuff DayLock;
        public override System.Collections.Generic.IEnumerator<Kingmaker.UnitLogic.Abilities.Components.Base.AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target)
        {
            var caster = context.MaybeCaster;
            if (caster != null && Mark != null)
            {
                // Q10: move only the ally this caster explicitly selected. No
                // selection means no move; never silently fall back to the caster.
                var ally = Marked(caster);
                if (ally == null) yield break;
                bool locked = DayLock != null && ally.Buffs.Enumerable.Any(b => b.Blueprint == DayLock);
                if (!locked)
                {
                    ally.Position = target.Point;
                    foreach (var buff in ally.Buffs.Enumerable.Where(b => b.Blueprint == Mark).ToArray()) buff.Remove();
                    if (DayLock != null)
                    {
                        var added = ally.Buffs.AddBuff(DayLock, context, TimeSpan.FromDays(1));
                        if (added != null) added.IsNotDispelable = true;
                    }
                }
            }
            yield break;
        }
        UnitEntityData Marked(UnitEntityData caster)
        {
            foreach (var unit in Candidates(caster))
            {
                if (unit?.Buffs == null) continue;
                foreach (var buff in unit.Buffs.Enumerable)
                {
                    if (buff.Blueprint != Mark) continue;
                    var from = buff.Context?.MaybeCaster;
                    if (ReferenceEquals(from, caster)) return unit;
                }
            }
            return null;
        }
        internal static IEnumerable<UnitEntityData> Candidates(UnitEntityData caster)
        {
            yield return caster;
            var player = Game.Instance?.Player;
            if (player == null) yield break;
            object party = null;
            foreach (var name in new[] { "Party", "PartyCharacters", "Characters", "Group" })
            {
                party = AccessTools.Property(player.GetType(), name)?.GetValue(player, null);
                if (party != null) break;
            }
            if (party is System.Collections.IEnumerable items)
                foreach (var item in items)
                {
                    if (item is UnitEntityData unit) yield return unit;
                    else if (item != null)
                    {
                        var value = AccessTools.Property(item.GetType(), "Value")?.GetValue(item, null) as UnitEntityData;
                        if (value != null) yield return value;
                    }
                }
        }
    }

    // 0060 Crashing Waves: Oracle level and Charisma DC instead of the 0117
    // wizard hardcode; the water/cold detection follows the same name heuristics.
    [TypeId("0060b1a2c3d4e5f60718293a4b5c0d03")]
    internal sealed class CrashingWaves0060 : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IInitiatorRulebookHandler<RuleDealDamage>
    {
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            var spell = evt.Spell;
            if (spell == null) return;
            int level = OracleLevel();
            if (Water(spell)) evt.AddBonusCasterLevel(level >= 8 ? 2 : 1);
            else if (Cold(spell) && level >= 8) evt.AddBonusCasterLevel(2);
        }
        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (evt.Initiator != Owner || evt.Target == null) return;
            var spell = evt.SourceAbility;
            if (spell == null) return;
            int level = OracleLevel();
            if (level < 16 && !Water(spell)) return;
            int dc = 10 + level / 2 + Math.Max(0, Owner.Stats.Charisma.Bonus);
            var save = Rulebook.Trigger(new RuleSavingThrow(evt.Target, SavingThrowType.Fortitude, dc));
            if (save.IsPassed) return;
            evt.Target.Descriptor.State.AddCondition(UnitCondition.Prone);
        }
        public void OnEventDidTrigger(RuleDealDamage evt) { }
        int OracleLevel()
        {
            var oracle = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>("20ce9bf8af32bee4c8557a045ab499b1");
            return oracle == null ? 0 : Owner.Progression.GetClassLevel(oracle);
        }
        static bool Cold(BlueprintAbility spell)
        {
            var desc = spell.GetComponent<SpellDescriptorComponent>()?.Descriptor ?? SpellDescriptor.None;
            return (desc & SpellDescriptor.Cold) != 0;
        }
        static bool Water(BlueprintAbility spell)
        {
            var name = spell.name ?? "";
            return name.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Wave", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Hydraulic", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Tsunami", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    // 0060 weather speed: an own per-unit table so a unit that also carries the
    // 0117 wizard component never shares modifier state with this one.
    [TypeId("0060b1a2c3d4e5f60718293a4b5c0d04")]
    internal sealed class WeatherSpeed0060 : UnitFactComponentDelegate, Kingmaker.Controllers.Units.ITickEachRound, IGlobalSubscriber
    {
        sealed class State { internal Kingmaker.EntitySystem.Stats.ModifiableValue.Modifier Speed; }
        static readonly ConditionalWeakTable<UnitEntityData, State> Speeds = new ConditionalWeakTable<UnitEntityData, State>();
        public void OnNewRound() => Refresh();
        protected override void OnTurnOn() => Refresh();
        protected override void OnTurnOff() => Clear();
        void Refresh()
        {
            if (Owner == null) return;
            var state = Speeds.GetOrCreateValue(Owner);
            if (BadWeather())
            {
                if (state.Speed == null) state.Speed = Owner.Stats.Speed.AddModifier(10, ModifierDescriptor.UntypedStackable);
            }
            else if (state.Speed != null)
            {
                Owner.Stats.Speed.RemoveModifier(state.Speed);
                state.Speed = null;
            }
        }
        void Clear()
        {
            if (Owner != null && Speeds.TryGetValue(Owner, out var state) && state.Speed != null)
                Owner.Stats.Speed.RemoveModifier(state.Speed);
            if (Owner != null) Speeds.Remove(Owner);
        }
        static bool BadWeather()
        {
            var weather = Game.Instance?.Player?.Weather;
            var actual = weather == null ? null : AccessTools.Property(weather.GetType(), "ActualWeather")?.GetValue(weather);
            return actual != null && Convert.ToInt32(actual) >= 2;
        }
    }

    // D-F06: the remaining ward/mist uses live in the fact's serialized component
    // data (EntityFactComponentDelegate<TData> stores it as [JsonProperty] m_Data),
    // not in a ConditionalWeakTable. A restored buff therefore keeps the used
    // charges instead of refilling to the full amount on the first new hit.
    [Serializable]
    public sealed class ChargeState0060
    {
        [JsonProperty] public int Left;
        [JsonProperty] public bool Ready;
    }

    static class ChargeRules0060
    {
        internal static void Spend(ChargeState0060 data, EntityFact fact)
        {
            if (!data.Ready) { data.Left = For(fact); data.Ready = true; }
            if (data.Left <= 0) return;
            data.Left--;
            if (data.Left == 0 && fact is Kingmaker.UnitLogic.Buffs.Buff buff) buff.Remove();
        }

        internal static int For(EntityFact fact)
        {
            var caster = fact?.MaybeContext?.MaybeCaster;
            var oracle = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>("20ce9bf8af32bee4c8557a045ab499b1");
            int level = caster == null || oracle == null ? 0 : caster.Progression.GetClassLevel(oracle);
            if (level >= 16) return 3;
            if (level >= 8) return 2;
            return 1;
        }
    }

    [TypeId("0060b1a2c3d4e5f60718293a4b5c0d05")]
    internal sealed class WardCharges0060 : UnitFactComponentDelegate<ChargeState0060>, ITargetRulebookHandler<RuleAttackWithWeapon>
    {
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (evt.Target != Owner || evt.AttackRoll == null || !evt.AttackRoll.IsHit || evt.AttackRoll.IsFake) return;
            ChargeRules0060.Spend(Data, Fact);
        }
    }

    [TypeId("0060b1a2c3d4e5f60718293a4b5c0d06")]
    internal sealed class MistCharges0060 : UnitFactComponentDelegate<ChargeState0060>, ITargetRulebookHandler<RuleAttackWithWeapon>
    {
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (evt.Target != Owner || evt.AttackRoll == null || evt.AttackRoll.IsHit || evt.AttackRoll.IsFake) return;
            if (evt.AttackRoll.Result.ToString().IndexOf("Conceal", StringComparison.Ordinal) < 0) return;
            ChargeRules0060.Spend(Data, Fact);
        }
    }

    // 0060 Monstrous Insight: the insight bonus is the real Oracle level; the
    // identification DC keeps the native baseline.
    [TypeId("0060b1a2c3d4e5f60718293a4b5c0d07")]
    internal sealed class MonsterLore0060 : ContextAction
    {
        public override string GetCaption() => "Monstrous Insight";
        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            var target = Target?.Unit;
            if (caster == null || target == null) return;
            var skill = SkillFor(target);
            int level = 0;
            var oracle = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>("20ce9bf8af32bee4c8557a045ab499b1");
            if (oracle != null) level = caster.Progression.GetClassLevel(oracle);
            var check = new RuleSkillCheck(caster, skill, InsightDC(target.Progression.CharacterLevel));
            if (level > 0) check.AddTemporaryModifier(caster.Stats.GetStat(skill).AddModifier(level, ModifierDescriptor.Insight));
            var result = Context.TriggerRule(check);
            Reveal(caster, target, result.RollResult);
        }
        internal static int InsightDC(int targetCharacterLevel) => 10 + targetCharacterLevel;
        static void Reveal(UnitEntityData caster, UnitEntityData target, int roll)
        {
            var manager = Game.Instance?.Player?.InspectUnitsManager;
            if (manager == null) return;
            var blueprint = target.BlueprintForInspection;
            if (blueprint == null) return;
            var info = manager.GetInfo(blueprint);
            if (info == null || info.KnownPartsCount >= 4) return;
            info.SetCheck(roll);
            EventBus.RaiseEvent<IKnowledgeHandler>(h => h.HandleKnowledgeUpdated(info));
        }
        static StatType SkillFor(UnitEntityData unit)
        {
            var name = unit.Blueprint?.GetType().GetProperty("Type")?.GetValue(unit.Blueprint)?.ToString()
                ?? unit.Blueprint?.name ?? "";
            if (name.IndexOf("Undead", StringComparison.OrdinalIgnoreCase) >= 0) return StatType.SkillLoreReligion;
            if (name.IndexOf("Animal", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Vermin", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Plant", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Fey", StringComparison.OrdinalIgnoreCase) >= 0)
                return StatType.SkillLoreNature;
            if (name.IndexOf("Humanoid", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Construct", StringComparison.OrdinalIgnoreCase) >= 0)
                return StatType.SkillKnowledgeWorld;
            return StatType.SkillKnowledgeArcana;
        }
    }

    // C18: the parametrized selection result is a private grant feature carrying
    // a native AddSpellKnownTemporary, so exactly one ledger entry exists per
    // selection and removing the selection removes the temporary spell. The
    // grant map is a per component instance field, so isolated hosts with their
    // own caches never share grant blueprints across cache instances.
    [TypeId("0060b1a2c3d4e5f60718293a4b5c0d08")]
    internal sealed class LearnTemporarySpellParametrized0060 : UnitFactComponentDelegate
    {
        public int Circle = 1;

        internal readonly Dictionary<string, BlueprintFeature> Grants = new Dictionary<string, BlueprintFeature>();

        private BlueprintFeature Lookup(string spellGuid) =>
            Grants.TryGetValue(spellGuid, out var grant) ? grant : null;

        protected override void OnActivate()
        {
            var spell = (Param?.Value?.Blueprint) as BlueprintAbility;
            if (spell == null) return;
            var grant = Lookup(spell.AssetGuid.ToString());
            if (grant != null && !Owner.HasFact(grant)) Owner.AddFact(grant);
        }

        protected override void OnDeactivate()
        {
            var spell = (Param?.Value?.Blueprint) as BlueprintAbility;
            if (spell == null) return;
            var grant = Lookup(spell.AssetGuid.ToString());
            if (grant == null) return;
            foreach (var stale in Owner.Facts.List.Where(f => f.Blueprint == grant).ToArray())
                Owner.RemoveFact(stale);
        }
    }
}
