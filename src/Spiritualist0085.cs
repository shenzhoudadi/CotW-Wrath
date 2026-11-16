// COTW-0085 Spiritualist base class. Contract C-0085-20261115-r1.
// Wisdom spontaneous psychic casting (circles 0-6), one phantom per master
// through the native AddPet chain, six original emotional focuses and the
// full master progression. Private identities only; native shared roots are
// read, never overwritten. Variant archetypes (COTW-0086..0091) stay out.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Spiritualist0085Ids
    {
        internal const string Salt = "CotW.Wrath.Spiritualist0085.v1/";
        internal const string AssetPrefix = "CotWWrathSpiritualist0085";

        internal static BlueprintGuid Id(string token)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
                return BlueprintGuid.Parse(string.Concat(md5.ComputeHash(
                    System.Text.Encoding.UTF8.GetBytes(Salt + token)).Select(b => b.ToString("x2"))));
        }

        // Native donors audited in A-spiritualist-assets.json (Wrath 2.7.0x).
        internal const string InquisitorClassId = "f1a70d9e1b0b41e49874e1fa9052a1ce";
        internal const string InquisitorBookId = "57fab75111f377248810ece84193a5a5";
        internal const string FighterClassId = "48ac8db94d5de7645906c7d0ad3bcfbd";
        internal const string AnimalClassId = "4cd1757a0eea7694ba5c933729a53920";
        internal const string SavesHighId = "ff4662bde9e75f145853417313842751";
        internal const string SpectreUnitId = "2f91d7337b60e3b4b9b137198a8c8745";
        internal const string SmilodonUnitId = "8a6986e17799d7d4b90f0c158b31c5b9";
        internal const string GhostFxId = "20f79fea035330b479fc899fa201d232";
        internal const string NaturalArmor2Id = "45a52ce762f637f4c80cc741c91f58b7";
        internal const string SlamWeaponId = "767e6932882a99c4b8ca95c88d823137";
        internal const string SlamTypeId = "f18cbcb39a1b35643a8d129b1ec4e716";
        internal const string CharacterBrainId = "cf986dd7ba9d4ec46ad8a3a0406d02ae";
        internal const string NeutralsFactionId = "d8de50cc80eb4dc409a983991e0b77ad";
        internal const string RayCalculateId = "d3e6275cfa6e7a04b9213b7b292a011c";
        internal const string TouchCalculateId = "62ef1cdb90f1d654d996556669caf7fa";
        internal const string DevotionId = "226f939b7dfd47b4697ec52f79799012";
        internal const string NaturalArmorGrowthId = "0d20d88abb7c33a47902bd99019f2ed1";
        internal const string AttributeGrowthId = "0c80276018694f24fbaf59ec7b841f2b";
        internal const string SkillFocusSelectionId = "c9629ef9eebb88b479b2fbc5e836656a";
        internal const string PowerAttackId = "9972f33f977fc724c838e59641b2fca5";
        internal const string WeaponFinesseId = "90e54424d682d104ab36436bd527af09";
        internal const string StealthyId = "c7e1d5ef809325943af97f093e149c4f";
        internal const string SneakAttackId = "9b9eac6709e1c084cb18c3a366e0ec87";
        internal const string ImprovedUnarmedId = "7812ad3672a4b9a4fb894ea402095167";
        internal const string DetectMagicId = "ee0b69e90bac14446a4cf9a050f87f2e";
        internal const string SorcererProficienciesId = "25c97697236ccf2479d0c6a4185eae7f";
        internal const string LightArmorProficiencyId = "6d3728d4e9c9898458fe5e9532951132";
        internal const string ScytheProficiencyId = "96c174b0ebca7b246b82d4bc4aac4574";
        internal const string KukriProficiencyId = "a7e822a8507e44b0a981ca55586dfad9";
        internal const string ShakenBuffId = "25ec6cb6ab1845c48a95f9c20b034220";
        internal const string FrightenedBuffId = "f08a7239aa961f34c8301518e71d4cdf";
        internal const string CrushingDespairBuffId = "36897b36a4bd63146b1d8509a17fc5ad";
        internal const string RemoveFearBuffId = "c5c86809a1c834e42a2eb33133e90a28";
        internal const string EnlargePersonBuffId = "4f139d125bb602f48bfaec3d3e1937cb";
        internal const string RageBuffId = "a1ffec0ce7c167a40aaea13dc49b757b";
        internal const string LuckDomainBuffId = "3bc40c9cbf9a0db4b8b43d8eedf2e6ec";
        internal const string WailOfBansheeId = "b24583190f36a8442b212e45226c54fc";
        internal const string CrushingDespairSpellId = "4baf4109145de4345861fe0f2209d903";
        internal const string HasteSpellId = "486eaff58293f6441a5c2759c4872f98";
        internal const string LayOnHandsSelfId = "8d6073201e5395d458b8251386d72df1";
        internal const string LayOnHandsOthersId = "caae1dc6fcf7b37408686971ee27db13";
        internal const string MercySelectionId = "02b187038a8dce545bb34bbfb346428d";
        internal const string DimensionDoorId = "5bdc37e4acfa209408334326076a43bc";
        internal const string OutsiderTypeId = "9054d3988d491d944ac144e27b6bc318";
        internal const string BasicFeatsId = "247a4068296e8be42890143f451b4b45";
        internal const string RootId = "2d77316c72b9ed44f888ceefc2a131f6";

        // Reserved by the frozen contract; native enum is 0..4 and the project's
        // Making Friends eidolon uses 604767. This value enters saves.
        internal const PetType PhantomPetType = (PetType)850085;

        internal sealed class EmotionRow
        {
            internal string Name;
            internal StatType[] Skills;
            internal bool Fort, Ref, Will;
            internal int Str, Dex;
            internal bool LargeSlam;          // Anger replaces the slam dice and STR/CHA growth
            internal string BaseTrait;        // level-1 archetype grant token
            internal string[] Features;       // phantom feature tokens at 1/7/12/17
            internal string[] SlaGuids;       // master SLA roots at 5/7/9/16 (native or provider)
            internal string[] SlaProviders;   // non-null entries resolve through the shared catalog
            internal string Description;
        }

        // A-spiritualist-map.md six emotions; Whimsy stays unwired as original.
        internal static readonly EmotionRow[] Emotions =
        {
            new EmotionRow { Name = "Anger", Skills = new[] { StatType.SkillPersuasion, StatType.SkillLoreNature },
                Fort = true, Ref = false, Will = true, Str = 14, Dex = 12, LargeSlam = true, BaseTrait = "PowerAttack",
                Features = new[] { "PowerfulStrikes", "AuraOfFury", "FerociousMien", "FuriousWail" },
                SlaGuids = new[] { "c60969e7f264e6d4b84a1499fdcf9039", "97b991256e43bb140b263c326f690ce2", "e80a4d6c0efa5774cbd515e3e37095b0", "f09453607e683784c8fca646eec49162" },
                SlaProviders = new string[] { null, null, null, null },
                Description = "Phantoms with this emotional focus are filled with seething anger from events in their past lives. Skills: Persuasion and Lore (Nature). Good saves: Fortitude and Will. Strength Focus: the phantom gains +2 Strength and -2 Dexterity, and gains Strength instead of Dexterity as the spiritualist gains levels." },
            new EmotionRow { Name = "Despair", Skills = new[] { StatType.SkillPersuasion, StatType.SkillStealth },
                Fort = true, Ref = false, Will = true, Str = 12, Dex = 14, LargeSlam = false, BaseTrait = "PowerFromDespair",
                Features = new[] { "MiserableStrike", "AuraOfDespair", "DespairingShout", "InescapableDespair" },
                SlaGuids = new[] { "ca577309cedc4f1daf6fe5795fb2619b", "8e4e94698fa142a98cfc44ad05ff8c91", "f492622e473d34747806bdb39356eb89", "4baf4109145de4345861fe0f2209d903" },
                SlaProviders = new string[] { null, "StrickenHeart", null, null },
                Description = "Despair phantoms use misery as a weapon. Skills: Persuasion and Stealth. Good saves: Fortitude and Will. Power from Despair: +2 attack and damage against shaken, frightened or despairing creatures." },
            new EmotionRow { Name = "Hatred", Skills = new[] { StatType.SkillMobility, StatType.SkillPerception },
                Fort = true, Ref = true, Will = false, Str = 12, Dex = 14, LargeSlam = false, BaseTrait = "WeaponFinesse",
                Features = new[] { "HatedTarget", "HatefulAura", "SneakAttack", "SharedHatred" },
                SlaGuids = new[] { "fbdd8c455ac4cde4a9a3e18c84af9485", "e023af1af9c147549a8e7bd246967861", "989ab5c44240907489aba0a8568d0603", "f34fb78eaaec141469079af124bcfa0f" },
                SlaProviders = new string[] { null, "InflictPainAbility", null, null },
                Description = "Hatred phantoms are blinded by emotion and blind others with it. Skills: Mobility and Perception. Good saves: Fortitude and Reflex. Weapon Finesse as a bonus feat." },
            new EmotionRow { Name = "Fear", Skills = new[] { StatType.SkillPersuasion, StatType.SkillStealth },
                Fort = false, Ref = true, Will = true, Str = 12, Dex = 14, LargeSlam = false, BaseTrait = "Stealthy",
                Features = new[] { "HorrifyingStrike", "IncreaseFear", "FrightfulAttack", "ShelterAllies" },
                SlaGuids = new[] { "8bc64d869456b004b9db255cdd1ea734", "d81f20775f4ef87375ec494a98955282", "8a28a811ca5d20d49a863e832c31cce1", "6717dbaef00c0eb4897a1c908a75dfe5" },
                SlaProviders = new string[] { null, "CotW:SavageMaw", null, null },
                Description = "Fear phantoms exist to inflict terror. Skills: Persuasion and Stealth. Good saves: Reflex and Will. Stealthy as a bonus feat." },
            new EmotionRow { Name = "Zeal", Skills = new[] { StatType.SkillMobility, StatType.SkillLoreNature },
                Fort = true, Ref = true, Will = false, Str = 12, Dex = 14, LargeSlam = false, BaseTrait = "Tracking",
                Features = new[] { "RuthlessCombatant", "DeterminationAura", "Haste", "ZealsResolve" },
                SlaGuids = new[] { "90e59f4a4ada87243b7b3535a06d0638", "f9ce1e9088de0d502a98bcce636b0615", "9d5d2d3ffdd73c648af3eb3e585b1113", "0087fc2d64b6095478bc7b8d7d512caf" },
                SlaProviders = new string[] { null, "CotW:ForceSword", null, null },
                Description = "Zeal phantoms burn with purpose. Skills: Mobility and Lore (Nature). Good saves: Fortitude and Reflex (code; original prose says Fortitude/Will). Tracking as a bonus feat." },
            new EmotionRow { Name = "Kindness", Skills = new[] { StatType.SkillPersuasion, StatType.SkillLoreReligion },
                Fort = true, Ref = false, Will = true, Str = 12, Dex = 14, LargeSlam = false, BaseTrait = "Benevolent",
                Features = new[] { "OpeningStrike", "EthericHealing", "ExpandedAid", "ExceptionalAid" },
                SlaGuids = new[] { "90e59f4a4ada87243b7b3535a06d0638", "03a9630394d10164a9410882d31572f0", "5ab0d42fb68c9e34abae4921822b9d63", "a5e23522eda32dc45801e32c05dc9f96" },
                SlaProviders = new string[] { null, null, null, null },
                Description = "Kindness phantoms protect and heal. Skills: Persuasion and Lore (Religion). Good saves: Fortitude and Will. Benevolent: aid another grants +2 more." },
        };

        internal sealed class SpellRow
        {
            internal int Circle;
            internal string Guid;          // native Wrath root
            internal string CotwToken;     // this repository's published root
            internal string Provider;      // shared catalog provider entry name
            internal string Label;
            internal SpellRow(int circle, string label, string guid = null, string cotwToken = null, string provider = null)
            { Circle = circle; Label = label; Guid = guid; CotwToken = cotwToken; Provider = provider; }
        }

        // 137 source rows, circles 0-6 = 6/17/24/28/22/24/16. Source circles are
        // preserved; Ghoul Touch has no confirmed equivalent and stays reported.
        internal static readonly SpellRow[] SpellList =
        {
            new SpellRow(0, "Daze", "55f14bc84d7c85446b07a1b5dd6b2b4c"),
            new SpellRow(0, "Guidance", "c3a8f31778c3980498d8f00c980be5f5"),
            new SpellRow(0, "Light", "95f206566c5261c42aa5b3e7e0d1e36c"),
            new SpellRow(0, "Resistance", "7bc8e27cba24f0e43ae64ed201ad5785"),
            new SpellRow(0, "Touch of Fatigue", "5bf3315ce1ed4d94e8805706820ef64d"),
            new SpellRow(0, "Virtue", "d3a852385ba4cd740992d1970170301a"),
            new SpellRow(1, "Burst of Adrenaline", cotwToken: "BurstOfAdrenaline"),
            new SpellRow(1, "Burst of Insight", cotwToken: "BurstOfInsight"),
            new SpellRow(1, "Cause Fear", "bd81a3931aa285a4f9844585b5d97e51"),
            new SpellRow(1, "Chill Touch", provider: "ChillTouch"),
            new SpellRow(1, "Cure Light Wounds", "5590652e1c2225c4ca30c4a699ab3649"),
            new SpellRow(1, "Doom", "fbdd8c455ac4cde4a9a3e18c84af9485"),
            new SpellRow(1, "Expeditious Retreat", "4f8181e7a7f1d904fbaea64220e83379"),
            new SpellRow(1, "Inflict Light Wounds", "e5af3674bb241f14b9a9f6b0c7dc3d27"),
            new SpellRow(1, "Mage Armor", "9e1ad5d6f87d19e4d8883d63a6e35568"),
            new SpellRow(1, "Magic Fang", "403cf599412299a4f9d5d925c7b9fb33"),
            new SpellRow(1, "Obscuring Mist", cotwToken: "ObscuringMist"),
            new SpellRow(1, "Protection from Alignment", "433b1faf4d02cc34abb0ade5ceda47c4"),
            new SpellRow(1, "Remove Fear", "55a037e514c0ee14a8e3ed14b47061de"),
            new SpellRow(1, "Sanctuary", cotwToken: "Sanctuary"),
            new SpellRow(1, "Shield", "ef768022b0785eb43a18969903c537c4"),
            new SpellRow(1, "Summon Monster I", "8fd74eddd9b6c224693d9ab241f25e84"),
            new SpellRow(1, "Touch of Gracelessness", "ad10bfec6d7ae8b47870e3a545cc8900"),
            new SpellRow(2, "Aid", "03a9630394d10164a9410882d31572f0"),
            new SpellRow(2, "Animate Dead, Lesser", "57fcf8016cf04da4a8b33d2add14de7e"),
            new SpellRow(2, "Blade Tutor", cotwToken: "BladeTutor"),
            new SpellRow(2, "Blur", "14ec7a4e52e90fa47a4c8d63c69fd5c1"),
            new SpellRow(2, "Boneshaker", "b7731c2b4fa1c9844a092329177be4c3"),
            new SpellRow(2, "Cure Moderate Wounds", "6b90c773a6543dc49b2505858ce33db5"),
            new SpellRow(2, "Eagle's Splendor", "446f7bf201dc1934f96ac0a26e324803"),
            new SpellRow(2, "Effortless Armor", "e1291272c8f48c14ab212a599ad17aac"),
            new SpellRow(2, "False Life", "7a5b5bf845779a941a67251539545762"),
            new SpellRow(2, "Force Sword", cotwToken: "ForceSword"),
            new SpellRow(2, "Ghoul Touch"),
            new SpellRow(2, "Inflict Moderate Wounds", "65f0b63c45ea82a4f8b8325768a3832d"),
            new SpellRow(2, "Inflict Pain", provider: "InflictPainAbility"),
            new SpellRow(2, "Invisibility", "89940cde01689fb46946b2f8cd7b66b7"),
            new SpellRow(2, "Protection from Arrows", "c28de1f98a3f432448e52e5d47c73208"),
            new SpellRow(2, "Restoration, Lesser", "e84fc922ccf952943b5240293669b171"),
            new SpellRow(2, "Resist Energy", "21ffef7791ce73f468b6fca4d9371e8b"),
            new SpellRow(2, "Scare", "08cb5f4c3b2695e44971bf5c45205df0"),
            new SpellRow(2, "See Invisibility", "30e5dc243f937fc4b95d2f8f4e1b7ff3"),
            new SpellRow(2, "Shadow Claws", provider: "ShadowClawsAbility"),
            new SpellRow(2, "Spiritual Weapon", cotwToken: "SpiritualWeapon"),
            new SpellRow(2, "Stricken Heart", provider: "StrickenHeart"),
            new SpellRow(2, "Summon Small Elemental", "970c6db48ff0c6f43afc9dbb48780d03"),
            new SpellRow(2, "Summon Monster II", "1724061e89c667045a6891179ee2e8e7"),
            new SpellRow(3, "Animate Dead", "4b76d32feb089ad4499c3a1ce8e1ac27"),
            new SpellRow(3, "Bestow Curse", "989ab5c44240907489aba0a8568d0603"),
            new SpellRow(3, "Blindness", "46fd02ad56c35224c9c91c88cd457791"),
            new SpellRow(3, "Cloak of Winds", provider: "CloakOfWindsAbility"),
            new SpellRow(3, "Condensed Ether", cotwToken: "CondensedEther"),
            new SpellRow(3, "Cure Serious Wounds", "3361c5df793b4c8448756146a88026ad"),
            new SpellRow(3, "Dispel Magic", "92681f181b507b34ea87018e8f7a528a"),
            new SpellRow(3, "Displacement", "903092f6488f9ce45a80943923576ab3"),
            new SpellRow(3, "Fly", provider: "FlySpell"),
            new SpellRow(3, "Haste", "486eaff58293f6441a5c2759c4872f98"),
            new SpellRow(3, "Heroism", "5ab0d42fb68c9e34abae4921822b9d63"),
            new SpellRow(3, "Howling Agony", cotwToken: "HowlingAgony"),
            new SpellRow(3, "Inflict Serious Wounds", "bd5da98859cf2b3418f6d68ea66cabbe"),
            new SpellRow(3, "Invisibility Purge", provider: "InvisibilityPurge"),
            new SpellRow(3, "Magic Fang, Greater", "f1100650705a69c4384d3edd88ba0f52"),
            new SpellRow(3, "Pain Strike", cotwToken: "PainStrike"),
            new SpellRow(3, "Protection from Energy", "d2f116cfe05fcdd4a94e80143b67046f"),
            new SpellRow(3, "Remove Blindness", "c927a8b0cd3f5174f8c0b67cdbfde539"),
            new SpellRow(3, "Remove Disease", "4093d5a0eb5cae94e909eb1e0e1a6b36"),
            new SpellRow(3, "Ray of Exhaustion", "8eead52509987034ea9025d60cc05985"),
            new SpellRow(3, "Rigor Mortis", provider: "RigorMortisAbility"),
            new SpellRow(3, "Sands of Time", provider: "SandsOfTimeCast"),
            new SpellRow(3, "Slow", "f492622e473d34747806bdb39356eb89"),
            new SpellRow(3, "Spirit Bound Blade", cotwToken: "SpiritBoundBlade"),
            new SpellRow(3, "Twilight Knife", cotwToken: "TwilightKnife"),
            new SpellRow(3, "Undead Anatomy I", cotwToken: "UndeadAnatomy1"),
            new SpellRow(3, "Vampiric Touch", "8a28a811ca5d20d49a863e832c31cce1"),
            new SpellRow(3, "Summon Monster III", "5d61dde0020bbf54ba1521f7ca0229dc"),
            new SpellRow(4, "Aura of Doom", cotwToken: "AuraOfDoom"),
            new SpellRow(4, "Confusion", "cf6c901fb7acc904e85c63b342e9c949"),
            new SpellRow(4, "Crushing Despair", "4baf4109145de4345861fe0f2209d903"),
            new SpellRow(4, "Cure Critical Wounds", "41c9016596fe1de4faf67425ed691203"),
            new SpellRow(4, "Death Ward", "e9cc9378fd6841f48ad59384e79e9953"),
            new SpellRow(4, "Debilitating Portent", cotwToken: "DebilitatingPortent"),
            new SpellRow(4, "Dimension Door", "4a648b57935a59547b7a2ee86fb4f26a"),
            new SpellRow(4, "Enervation", "f34fb78eaaec141469079af124bcfa0f"),
            new SpellRow(4, "False Life, Greater", "dc6af3b4fd149f841912d8a3ce0983de"),
            new SpellRow(4, "Fear", "d2aeac47450c76347aebbc02e4f463e0"),
            new SpellRow(4, "Freedom of Movement", "0087fc2d64b6095478bc7b8d7d512caf"),
            new SpellRow(4, "Inflict Critical Wounds", "651110ed4f117a948b41c05c5c7624c0"),
            new SpellRow(4, "Invisibility, Greater", "ecaa0def35b38f949bd1976a6c9539e0"),
            new SpellRow(4, "Neutralize Poison", "e7240516af4241b42b2cd819929ea9da"),
            new SpellRow(4, "Phantasmal Killer", "6717dbaef00c0eb4897a1c908a75dfe5"),
            new SpellRow(4, "Remove Curse", "b48674cef2bff5e478a007cf57d8345b"),
            new SpellRow(4, "Restoration", "f2115ac1148256b4ba20788f7e966830"),
            new SpellRow(4, "Shadow Conjuration", "caac251ca7601324bbe000372a0a1005"),
            new SpellRow(4, "Solid Fog", cotwToken: "SolidFog"),
            new SpellRow(4, "Spiritual Ally", cotwToken: "SpiritualAlly"),
            new SpellRow(4, "Summon Medium Elemental", "e42b1dbff4262c6469a9ff0a6ce730e3"),
            new SpellRow(4, "Summon Monster IV", "7ed74a3ec8c458d4fb50b192fd7be6ef"),
            new SpellRow(5, "Break Enchantment", "7792da00c85b9e042a0fdfc2b66ec9a8"),
            new SpellRow(5, "Breath of Life", "d5847cad0b0e54c4d82d6c59a3cda6b0"),
            new SpellRow(5, "Cloudkill", "548d339ba87ee56459c98e80167bdf10"),
            new SpellRow(5, "Curse, Major", cotwToken: "CurseMajor"),
            new SpellRow(5, "Dismissal", "95f7cdcec94e293489a85afdf5af1fd7"),
            new SpellRow(5, "Dominate Person", "d7cbd2004ce66a042aeab2e95a3c5c61"),
            new SpellRow(5, "Feeblemind", "444eed6e26f773a40ab6e4d160c67faa"),
            new SpellRow(5, "Fickle Winds", cotwToken: "FickleWinds"),
            new SpellRow(5, "Inflict Pain, Mass", provider: "InflictPainMassAbility"),
            new SpellRow(5, "Mind Fog", "eabf94e4edc6e714cabd96aa69f8b207"),
            new SpellRow(5, "Overland Flight", cotwToken: "OverlandFlight"),
            new SpellRow(5, "Pain Strike, Mass", cotwToken: "PainStrikeMass"),
            new SpellRow(5, "Phantasmal Web", "12fb4a4c22549c74d949e2916a2f0b6a"),
            new SpellRow(5, "Phantom Limbs", provider: "NewSpell.UsePhantomLimb"),
            new SpellRow(5, "Raise Dead", "a0fc99f0933d01643b2b8fe570caa4c5"),
            new SpellRow(5, "Shadow Evocation", "237427308e48c3341b3d532b9d3a001f"),
            new SpellRow(5, "Slay Living", "4fbd47525382517419c66fb548fe9a67"),
            new SpellRow(5, "Spell Resistance", "0a5ddfbcfb3989543ac7c936fc256889"),
            new SpellRow(5, "Suffocation", cotwToken: "Suffocation"),
            new SpellRow(5, "Summon Large Elemental", "89404dd71edc1aa42962824b44156fe5"),
            new SpellRow(5, "Summon Monster V", "630c8b85d9f07a64f917d79cb5905741"),
            new SpellRow(5, "Undead Anatomy II", cotwToken: "UndeadAnatomy2"),
            new SpellRow(5, "Vampiric Shadow Shield", "a34921035f2a6714e9be5ca76c5e34b5"),
            new SpellRow(5, "Waves of Fatigue", "8878d0c46dfbd564e9d5756349d5e439"),
            new SpellRow(6, "Banishment", "d361391f645db984bbf58907711a146a"),
            new SpellRow(6, "Banshee Blast", "d42c6d3f29e07b6409d670792d72bc82"),
            new SpellRow(6, "Circle of Death", "a89dcbbab8f40e44e920cc60636097cf"),
            new SpellRow(6, "Create Undead", "76a11b460be25e44ca85904d6806e5a3"),
            new SpellRow(6, "Dispel Magic, Greater", "f0f761b808dc4b149b08eaf44b99f633"),
            new SpellRow(6, "Disintegrate", "4aa7942c3e62a164387a73184bca3fc1"),
            new SpellRow(6, "Eyebite", "3167d30dd3c622c46b0c0cb242061642"),
            new SpellRow(6, "Harm", "cc09224ecc9af79449816c45bc5be218"),
            new SpellRow(6, "Heal", "5da172c4c89f9eb4cbb614f3a67357d3"),
            new SpellRow(6, "Heroism, Greater", "e15e5e7045fda2244b98c8f010adfe31"),
            new SpellRow(6, "Summon Huge Elemental", "766ec978fa993034f86a372c8eb1fc10"),
            new SpellRow(6, "Summon Monster VI", "e740afbab0147944dab35d83faa0ae1c"),
            new SpellRow(6, "Transformation", "27203d62eb3d4184c9aced94f22e1806"),
            new SpellRow(6, "True Seeing", "4cf3d0fae3239ec478f51e86f49161cb"),
            new SpellRow(6, "Umbral Strike", "474ed0aa656cc38499cc9a073d113716"),
            new SpellRow(6, "Undead Anatomy III", cotwToken: "UndeadAnatomy3"),
        };
    }

    internal static class Spiritualist0085
    {
        internal const string ModuleName = "COTW-0085 Spiritualist";
        internal const string RootToken = "Spiritualist0085Class";
        internal static string LastSummary = "Not registered";
        internal static string[] LastMissing = Array.Empty<string>();
        internal static string[] LastUnavailable = Array.Empty<string>();

        private sealed class State
        {
            internal BlueprintGuid Root;
            internal BlueprintScriptableObject[] Assets;
            internal string[] Definitions;
            internal BlueprintCharacterClass Class;
        }

        private static readonly ConditionalWeakTable<BlueprintsCache, State> Published =
            new ConditionalWeakTable<BlueprintsCache, State>();

        internal static BlueprintGuid Id(string token) => Spiritualist0085Ids.Id(token);

        private static LocalizedString Text(string token, string suffix, string value) =>
            PsychicAssets.Text("Spiritualist0085" + token, suffix, value);

        private static void Log(string line)
        {
            try { ModEntry.TraceNatureTarget(line); } catch { }
            Console.WriteLine(line);
        }

        private static LevelEntry Entry(int level, params BlueprintFeatureBase[] facts)
        {
            var e = new LevelEntry { Level = level };
            Set(e, "m_Features", facts.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            return e;
        }

        private static UIGroup Group(params BlueprintFeatureBase[] facts)
        {
            var g = new UIGroup();
            Set(g, "m_Features", facts.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            return g;
        }

        private static ContextRankConfig Rank(BlueprintCharacterClass cls, string progression, params int[] thresholds)
            => ArchetypeBatch01445.Rank(cls, progression, thresholds);

        private static BlueprintAbilityResource Resource(Phantom0085.FactoryShim f, string token, BlueprintCharacterClass cls,
            int baseValue, int startLevel, int step, StatType? bonusStat = null, bool fixedValue = false)
        {
            var r = f.Add(new BlueprintAbilityResource(), token, "", "");
            var amount = Get(r, "m_MaxAmount");
            Set(amount, "BaseValue", baseValue);
            Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>());
            Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            if (fixedValue)
            {
                Set(amount, "IncreasedByLevelStartPlusDivStep", false);
            }
            else
            {
                Set(amount, "IncreasedByLevelStartPlusDivStep", true);
                Set(amount, "StartingLevel", startLevel);
                Set(amount, "StartingIncrease", 1);
                Set(amount, "LevelStep", step);
                Set(amount, "PerStepIncrease", 1);
                Set(amount, "m_ClassDiv", new[] { Ref<BlueprintCharacterClassReference>(cls) });
            }
            if (bonusStat.HasValue)
            {
                Set(amount, "IncreasedByStat", true);
                Set(amount, "ResourceBonusStat", bonusStat.Value);
            }
            Set(r, "m_MaxAmount", amount);
            if (AccessTools.Field(r.GetType(), "m_Min") != null) Set(r, "m_Min", 0);
            return r;
        }

        private static AddAbilityResources Grant(BlueprintAbilityResource resource)
        {
            var c = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true };
            Set(c, "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
            return c;
        }

        private static AbilityResourceLogic Cost(BlueprintAbilityResource resource, int amount = 1)
        {
            var c = new AbilityResourceLogic { Amount = amount };
            Set(c, "m_IsSpendResource", true);
            Set(c, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
            return c;
        }

        private static AddFacts Facts(params BlueprintUnitFact[] facts)
        {
            var result = new AddFacts { name = "Spiritualist0085GrantedFacts" };
            Set(result, "m_Facts", facts.Select(Ref<BlueprintUnitFactReference>).ToArray());
            return result;
        }

        // Resolve one catalog row. Native roots load from the cache; repository
        // roots resolve through the Psychic identity salt; provider roots use
        // the audited shared catalog. Unavailable rows are reported, never faked.
        private static BlueprintAbility ResolveSpell(BlueprintsCache cache, Spiritualist0085Ids.SpellRow row, List<string> unavailable)
        {
            if (row.Guid != null)
            {
                var native = cache.Load(BlueprintGuid.Parse(row.Guid)) as BlueprintAbility;
                if (native == null) unavailable.Add(row.Label + " (native " + row.Guid + ")");
                return native;
            }
            if (row.CotwToken != null)
            {
                var own = cache.Load(PsychicAssets.Id(row.CotwToken)) as BlueprintAbility;
                if (own == null) unavailable.Add(row.Label + " (CotW " + row.CotwToken + ")");
                return own;
            }
            if (row.Provider != null)
            {
                var link = PsychicSharedSpellCatalog.Entries.FirstOrDefault(e => e.Name == row.Provider);
                if (link == null) { unavailable.Add(row.Label + " (provider entry " + row.Provider + " not audited)"); return null; }
                if (!PsychicSpellIntegration.Available(cache, link, out var ability))
                { unavailable.Add(row.Label + " (provider " + link.Provider + " not installed)"); return null; }
                return ability;
            }
            unavailable.Add(row.Label + " (no confirmed equivalent root)");
            return null;
        }

        private static BlueprintAbility ResolveSla(BlueprintsCache cache, string guid, string provider, List<string> unavailable, string emotion)
        {
            if (provider == null)
            {
                var native = cache.Load(BlueprintGuid.Parse(guid)) as BlueprintAbility;
                if (native == null) unavailable.Add(emotion + " SLA native " + guid);
                return native;
            }
            if (provider.StartsWith("CotW:", StringComparison.Ordinal))
            {
                var own = cache.Load(PsychicAssets.Id(provider.Substring(5))) as BlueprintAbility;
                if (own == null) unavailable.Add(emotion + " SLA CotW " + provider);
                return own;
            }
            var link = PsychicSharedSpellCatalog.Entries.FirstOrDefault(e => e.Name == provider);
            if (link == null || !PsychicSpellIntegration.Available(cache, link, out var ability))
            { unavailable.Add(emotion + " SLA provider " + provider); return null; }
            return ability;
        }

        internal static bool Register(BlueprintsCache cache)
        {
            if (Published.TryGetValue(cache, out var state))
            {
                if (!ReferenceEquals(cache.Load(state.Root), state.Assets[0]))
                    throw new InvalidOperationException("0085 published class identity changed.");
                for (int i = 0; i < state.Assets.Length; i++)
                    if (!ReferenceEquals(cache.Load(state.Assets[i].AssetGuid), state.Assets[i]) ||
                        Fingerprint(state.Assets[i]) != state.Definitions[i])
                        throw new InvalidOperationException("0085 published private blueprint changed: " + state.Assets[i].name);
                return true;
            }

            var missing = new List<string>();
            foreach (var guid in RequiredNative())
                if (cache.Load(BlueprintGuid.Parse(guid)) == null) missing.Add(guid);
            var inquisitor = cache.Load(BlueprintGuid.Parse(Spiritualist0085Ids.InquisitorClassId)) as BlueprintCharacterClass;
            var inquisitorBook = cache.Load(BlueprintGuid.Parse(Spiritualist0085Ids.InquisitorBookId)) as BlueprintSpellbook;
            if (inquisitor == null || inquisitorBook == null) missing.Add("Inquisitor class/book");
            if (missing.Count > 0)
            {
                LastMissing = missing.ToArray();
                LastSummary = "reject: missing native donors " + string.Join(", ", missing);
                Log(LastSummary);
                return false;
            }

            // Atomic SLA gate: every emotional power root must resolve before
            // any private blueprint is staged. Optional spell-list roots are
            // reported and skipped instead (the catalog stays complete).
            var missingSla = new List<string>();
            foreach (var row in Spiritualist0085Ids.Emotions)
                for (int i = 0; i < 4; i++)
                    ResolveSla(cache, row.SlaGuids[i], row.SlaProviders[i], missingSla, row.Name);
            if (missingSla.Count > 0)
            {
                LastMissing = missingSla.ToArray();
                LastSummary = "reject: missing emotional power roots " + string.Join("; ", missingSla);
                Log(LastSummary);
                return false;
            }

            var f = new Phantom0085.FactoryShim(cache);
            var unavailable = new List<string>();
            BlueprintCharacterClass root = null;
            Kingmaker.Blueprints.Root.ProgressionRoot rootProgression = null;
            BlueprintCharacterClassReference[] previousClasses = null;
            BlueprintCharacterClassReference[] publishedClasses = null;
            var inserted = new List<BlueprintScriptableObject>();
            try
            {
                root = Build(f, cache, inquisitor, inquisitorBook, unavailable);
                var assets = new[] { (BlueprintScriptableObject)root }.Concat(
                    f.Assets.Values.Where(a => !ReferenceEquals(a, root))).ToArray();
                foreach (var a in assets)
                    if (cache.Load(a.AssetGuid) != null)
                        throw new InvalidOperationException("0085 private GUID occupied: " + a.name);

                var bpRoot = cache.Load(BlueprintGuid.Parse(Spiritualist0085Ids.RootId)) as Kingmaker.Blueprints.Root.BlueprintRoot;
                var progression = bpRoot.Progression;
                rootProgression = progression;
                previousClasses = (BlueprintCharacterClassReference[])Get(progression, "m_CharacterClasses");
                var previous = previousClasses ?? Array.Empty<BlueprintCharacterClassReference>();
                if (previous.Any(r => r?.Guid == root.AssetGuid))
                    throw new InvalidOperationException("0085 class already listed in root progression.");

                foreach (var a in assets) Prepare(a);
                f.Enable();
                foreach (var a in assets)
                {
                    inserted.Add(a);
                    cache.AddCachedBlueprint(a.AssetGuid, a);
                }
                foreach (var a in assets)
                    if (!ReferenceEquals(cache.Load(a.AssetGuid), a))
                        throw new InvalidOperationException("0085 private blueprint not published: " + a.name);

                publishedClasses = previous.Concat(new[] { Ref<BlueprintCharacterClassReference>(root) }).ToArray();
                Set(progression, "m_CharacterClasses", publishedClasses);

                Published.Add(cache, new State
                {
                    Root = root.AssetGuid,
                    Assets = assets,
                    Definitions = assets.Select(Fingerprint).ToArray(),
                    Class = root
                });
                LastMissing = Array.Empty<string>();
                LastUnavailable = unavailable.ToArray();
                LastSummary = "published " + root.AssetGuid + "; unavailable roots: " +
                    (unavailable.Count == 0 ? "none" : string.Join("; ", unavailable));
                Log("0085 " + LastSummary);
                return true;
            }
            catch (Exception error)
            {
                if (publishedClasses != null && ReferenceEquals(Get(rootProgression, "m_CharacterClasses"), publishedClasses))
                    Set(rootProgression, "m_CharacterClasses", previousClasses);
                for (int i = inserted.Count - 1; i >= 0; i--)
                    if (ReferenceEquals(cache.Load(inserted[i].AssetGuid), inserted[i]))
                        cache.RemoveCachedBlueprint(inserted[i].AssetGuid);
                Published.Remove(cache);
                LastSummary = "reject: 0085 " + error.GetType().Name + ": " + error.Message;
                Log(LastSummary + "\n" + error.StackTrace);
                return false;
            }
        }

        private static IEnumerable<string> RequiredNative()
        {
            yield return Spiritualist0085Ids.InquisitorClassId;
            yield return Spiritualist0085Ids.InquisitorBookId;
            yield return Spiritualist0085Ids.FighterClassId;
            yield return Spiritualist0085Ids.AnimalClassId;
            yield return Spiritualist0085Ids.SavesHighId;
            yield return Spiritualist0085Ids.SpectreUnitId;
            yield return Spiritualist0085Ids.SmilodonUnitId;
            yield return Spiritualist0085Ids.GhostFxId;
            yield return Spiritualist0085Ids.NaturalArmor2Id;
            yield return Spiritualist0085Ids.SlamWeaponId;
            yield return Spiritualist0085Ids.SlamTypeId;
            yield return Spiritualist0085Ids.CharacterBrainId;
            yield return Spiritualist0085Ids.NeutralsFactionId;
            yield return Spiritualist0085Ids.RayCalculateId;
            yield return Spiritualist0085Ids.TouchCalculateId;
            yield return Spiritualist0085Ids.DevotionId;
            yield return "a634f97d23662b347872ffcf580495b3";
            yield return "c4a7f98d743bc784c9d4cf2105852c39";
            yield return Spiritualist0085Ids.NaturalArmorGrowthId;
            yield return Spiritualist0085Ids.AttributeGrowthId;
            yield return Spiritualist0085Ids.SkillFocusSelectionId;
            yield return Spiritualist0085Ids.PowerAttackId;
            yield return Spiritualist0085Ids.WeaponFinesseId;
            yield return Spiritualist0085Ids.StealthyId;
            yield return Spiritualist0085Ids.SneakAttackId;
            yield return Spiritualist0085Ids.DetectMagicId;
            yield return Spiritualist0085Ids.SorcererProficienciesId;
            yield return "e70ecf1ed95ca2f40b754f1adb22bbdd";
            yield return Spiritualist0085Ids.LightArmorProficiencyId;
            yield return Spiritualist0085Ids.ScytheProficiencyId;
            yield return Spiritualist0085Ids.KukriProficiencyId;
            yield return Spiritualist0085Ids.ShakenBuffId;
            yield return Spiritualist0085Ids.FrightenedBuffId;
            yield return Spiritualist0085Ids.CrushingDespairBuffId;
            yield return Spiritualist0085Ids.RemoveFearBuffId;
            yield return Spiritualist0085Ids.EnlargePersonBuffId;
            yield return Spiritualist0085Ids.RageBuffId;
            yield return Spiritualist0085Ids.LuckDomainBuffId;
            yield return Spiritualist0085Ids.WailOfBansheeId;
            yield return Spiritualist0085Ids.CrushingDespairSpellId;
            yield return Spiritualist0085Ids.HasteSpellId;
            yield return Spiritualist0085Ids.LayOnHandsSelfId;
            yield return Spiritualist0085Ids.LayOnHandsOthersId;
            yield return Spiritualist0085Ids.MercySelectionId;
            yield return Spiritualist0085Ids.DimensionDoorId;
            yield return Spiritualist0085Ids.OutsiderTypeId;
            yield return Spiritualist0085Ids.BasicFeatsId;
            yield return Spiritualist0085Ids.RootId;
        }

        // Structural fingerprint for idempotency. The shared batch definition
        // covers the common fact shapes; class/book/unit/resource assets fall
        // back to a type-and-component shape keyed by identity.
        private static string Fingerprint(BlueprintScriptableObject bp)
        {
            try { return ArchetypeBatch01445.Definition(bp); }
            catch (InvalidOperationException)
            {
                var components = string.Join(";", (bp.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                    .Select(c => c.GetType().FullName + ":" + c.name));
                return bp.GetType().FullName + "|" + bp.AssetGuid + "|" + bp.name + "|" + components;
            }
        }

        private static void Prepare(BlueprintScriptableObject bp)
        {
            var elements = SpellBatch01447.Elements(bp.ComponentsArray, new HashSet<object>(PsychicAssets.IdentityComparer)).ToList();
            if (AccessTools.Field(bp.GetType(), "m_AllElements") != null) Set(bp, "m_AllElements", elements);
            if (bp is BlueprintAbility ability) SpellBatch01447.ResetAbilityCaches(ability);
            PsychicRepairComponents.UniqueNames(bp);
        }

        private static BlueprintCharacterClass Build(Phantom0085.FactoryShim f, BlueprintsCache cache,
            BlueprintCharacterClass inquisitor, BlueprintSpellbook inquisitorBook, List<string> unavailable)
        {
            var fighter = f.Native<BlueprintCharacterClass>(Spiritualist0085Ids.FighterClassId);
            var animal = f.Native<BlueprintCharacterClass>(Spiritualist0085Ids.AnimalClassId);
            var savesHigh = f.Native<BlueprintStatProgression>(Spiritualist0085Ids.SavesHighId);

            // ---- Master class (Inquisitor data only; no deity dependency) ----
            var character = f.Add(new BlueprintCharacterClass
            {
                SkillPoints = inquisitor.SkillPoints - 1,
                IsArcaneCaster = false, IsDivineCaster = false, PrestigeClass = false, HideInUI = false,
                ClassSkills = new[] { StatType.SkillPersuasion, StatType.SkillKnowledgeArcana, StatType.SkillKnowledgeWorld,
                    StatType.SkillLoreNature, StatType.SkillLoreReligion, StatType.SkillPerception, StatType.SkillUseMagicDevice },
                RecommendedAttributes = new[] { StatType.Wisdom },
                NotRecommendedAttributes = Array.Empty<StatType>(),
                PrimaryColor = inquisitor.PrimaryColor, SecondaryColor = inquisitor.SecondaryColor,
                StartingGold = inquisitor.StartingGold,
                MaleEquipmentEntities = inquisitor.MaleEquipmentEntities,
                FemaleEquipmentEntities = inquisitor.FemaleEquipmentEntities,
                ComponentsArray = new BlueprintComponent[] { new PrerequisiteIsPet { name = "Spiritualist0085NotPet", Not = true } }
            }, RootToken, "Spiritualist",
                "Becoming a spiritualist is not a calling - it's a phenomenon. The spiritualist is the master of a single powerful phantom whom she can manifest to do her bidding, harnessing the powers of life and death, thought and nightmare, shadow and revelation.");
            foreach (var field in new[] { "HitDie", "m_BaseAttackBonus", "m_FortitudeSave", "m_ReflexSave", "m_WillSave", "m_StartingItems", "m_EquipmentEntities", "m_Difficulty" })
                Set(character, field, Get(inquisitor, field));
            Set(character, "HitDie", DiceType.D8);
            Set(character, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(character, "m_Icon", inquisitor.Icon);

            // ---- Private spellbook: Wisdom spontaneous, Inquisitor tables ----
            var resolved = new List<BlueprintAbility>[7];
            for (int circle = 0; circle <= 6; circle++)
            {
                resolved[circle] = new List<BlueprintAbility>();
                foreach (var row in Spiritualist0085Ids.SpellList.Where(r => r.Circle == circle))
                {
                    var spell = ResolveSpell(cache, row, unavailable);
                    if (spell != null && spell.Type == AbilityType.Spell) resolved[circle].Add(spell);
                }
            }
            var list = f.Add(new BlueprintSpellList
            {
                SpellsByLevel = SpellListCompatibility.Build(Enumerable.Range(0, 10).Select(level =>
                {
                    var entry = new SpellLevelList(level);
                    Set(entry, "m_Spells", (level <= 6 ? resolved[level] : new List<BlueprintAbility>())
                        .Select(Ref<BlueprintAbilityReference>).ToList());
                    return entry;
                }).ToArray())
            }, "SpellList", "Spiritualist Spell List", "");
            var book = f.Add(new BlueprintSpellbook
            {
                Name = Text("Spellbook", "name", "Spiritualist"),
                CastingAttribute = StatType.Wisdom, Spontaneous = true,
                SpellsPerLevel = 0, AllSpellsKnown = false, CanCopyScrolls = false,
                CantripsType = CantripsType.Cantrips, IsArcane = false, CasterLevelModifier = 0,
                ComponentsArray = new BlueprintComponent[] { new PsychicSpellbookMarker { name = "Spiritualist0085PsychicMarker", MaximumSpellLevel = 6 } }
            }, "Spellbook", "Spiritualist Spellbook", "");
            foreach (var field in new[] { "m_SpellsKnown", "m_SpellsPerDay" }) Set(book, field, Get(inquisitorBook, field));
            Set(book, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(character));
            Set(book, "m_SpellList", Ref<BlueprintSpellListReference>(list));
            Set(character, "m_Spellbook", Ref<BlueprintSpellbookReference>(book));

            // ---- Phantom class and shared progression ----
            var phantomClass = Phantom0085.BuildPhantomClass(f, fighter, animal, savesHigh);
            var phantomProgression = Phantom0085.BuildPhantomProgression(f, phantomClass);
            Set(phantomClass, "m_Progression", Ref<BlueprintProgressionReference>(phantomProgression));

            // ---- Emotion catalog: six progressions, each one native AddPet ----
            var emotionProgressions = new List<BlueprintProgression>();
            var emotionOptions = new List<BlueprintFeature>();
            foreach (var row in Spiritualist0085Ids.Emotions)
            {
                var progression = BuildEmotion(f, cache, character, phantomClass, row, unavailable);
                emotionProgressions.Add(progression);
            }

            var selection = f.Add(new BlueprintFeatureSelection { Ranks = 1, Groups = new[] { FeatureGroup.AnimalCompanion } },
                "EmotionalFocus", "Phantom",
                "A spiritualist begins play with the aid of a powerful spirit entity called a phantom. The phantom forms a link with the spiritualist, who can harbor it within her consciousness or manifest it as an ectoplasmic entity. The emotional focus determines the phantom's skills, good saves and special abilities.");
            Set(selection, "m_Features", emotionProgressions.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(selection, "m_AllFeatures", emotionProgressions.Select(Ref<BlueprintFeatureReference>).ToArray());

            // ---- Master abilities ----
            var abilities = BuildMasterAbilities(f, cache, character, phantomClass, selection, emotionProgressions, unavailable);

            // ---- Proficiencies, knacks, spellcasting marker ----
            var proficiencies = BuildProficiencies(f);
            var knacks = f.Feature("Knacks", "Knacks",
                "A spiritualist learns a number of knacks, or 0-level psychic spells. These spells are cast like any other spell, but they are not expended when cast and may be used again.");
            var spellcasting = f.Feature("Spellcasting", "Psychic Magic",
                "A spiritualist casts psychic spells drawn from the spiritualist spell list. She can cast any spell she knows without preparing it ahead of time. To learn or cast a spell, a spiritualist must have a Wisdom score equal to at least 10 + the spell level. The DC for a saving throw against a spiritualist's spell equals 10 + the spell level + the spiritualist's Wisdom modifier.",
                Facts(knacks));

            var detectMagic = f.Native<BlueprintFeature>(Spiritualist0085Ids.DetectMagicId);
            var ray = f.Native<BlueprintFeature>(Spiritualist0085Ids.RayCalculateId);
            var touch = f.Native<BlueprintFeature>(Spiritualist0085Ids.TouchCalculateId);

            var progression20 = f.Progression("Progression", "Spiritualist", "Spiritualist class progression.",
                new[]
                {
                    Entry(1, proficiencies, spellcasting, detectMagic, knacks, selection, abilities.EthericTether, abilities.SharedConsciousness, abilities.MastersAlignment, ray, touch),
                    Entry(2), Entry(3, abilities.BondedManifestation), Entry(4, abilities.SpiritualInference),
                    Entry(5), Entry(6, abilities.PhantomRecall), Entry(7), Entry(8), Entry(9),
                    Entry(10, abilities.FusedConsciousness), Entry(11), Entry(12, abilities.GreaterSpiritualInference),
                    Entry(13), Entry(14, abilities.SpiritualBond), Entry(15), Entry(16), Entry(17, abilities.DualBond),
                    Entry(18), Entry(19), Entry(20, abilities.PotentPhantom)
                },
                new[]
                {
                    Group(abilities.EthericTether, abilities.SpiritualInference, abilities.PhantomRecall, abilities.SpiritualBond, abilities.GreaterSpiritualInference, abilities.PotentPhantom),
                    Group(abilities.SharedConsciousness, abilities.BondedManifestation, abilities.FusedConsciousness, abilities.DualBond)
                });
            Set(progression20, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(character) } });
            Set(progression20, "m_UIDeterminatorsGroup", new BlueprintFeatureBaseReference[]
            {
                Ref<BlueprintFeatureBaseReference>(proficiencies), Ref<BlueprintFeatureBaseReference>(spellcasting),
                Ref<BlueprintFeatureBaseReference>(detectMagic), Ref<BlueprintFeatureBaseReference>(knacks),
                Ref<BlueprintFeatureBaseReference>(selection)
            });
            Set(character, "m_Progression", Ref<BlueprintProgressionReference>(progression20));
            return character;
        }

        private static BlueprintFeature BuildProficiencies(Phantom0085.FactoryShim f)
        {
            var simple = f.Native<BlueprintFeature>("e70ecf1ed95ca2f40b754f1adb22bbdd");
            var light = f.Native<BlueprintFeature>(Spiritualist0085Ids.LightArmorProficiencyId);
            var scythe = f.Native<BlueprintFeature>(Spiritualist0085Ids.ScytheProficiencyId);
            var kukri = f.Native<BlueprintFeature>(Spiritualist0085Ids.KukriProficiencyId);
            return f.Feature("Proficiencies", "Spiritualist Proficiencies",
                "A spiritualist is proficient with all simple weapons, kukris and scythes, as well as with light armor.",
                Facts(simple, light, scythe, kukri));
        }

        private sealed class MasterAbilities
        {
            internal BlueprintFeature EthericTether, SpiritualBond, SharedConsciousness, FusedConsciousness,
                BondedManifestation, DualBond, SpiritualInference, GreaterSpiritualInference,
                PhantomRecall, PotentPhantom, MastersAlignment;
        }

        private static MasterAbilities BuildMasterAbilities(Phantom0085.FactoryShim f, BlueprintsCache cache,
            BlueprintCharacterClass character, BlueprintCharacterClass phantomClass,
            BlueprintFeatureSelection selection, List<BlueprintProgression> emotions, List<string> unavailable)
        {
            var result = new MasterAbilities();
            var phantomArray = new[] { phantomClass };
            var spiritualistArray = new[] { character };
            var bothArray = new[] { phantomClass, character };

            // Masters alignment: phantom mirrors the master's alignment.
            result.MastersAlignment = f.Feature("MastersAlignment", "", "",
                new Phantom0085AlignmentSync { name = "Spiritualist0085AlignmentSync" });
            result.MastersAlignment.HideInUI = true;
            result.MastersAlignment.HideInCharacterSheetAndLevelUp = true;

            // Shared Consciousness: confined-only Skill Focus + mind-affecting saves.
            result.SharedConsciousness = f.Feature("SharedConsciousness", "Shared Consciousness",
                "While a phantom is confined in a spiritualist's consciousness (but not while it's fully manifested or banished), it grants the spiritualist Skill Focus in two skills determined by the phantom's emotional focus, unless she already has Skill Focus in those skills. It also grants a +4 bonus on saving throws against all mind-affecting effects; at 12th level, this bonus increases to +8.");
            // Fused Consciousness: persistent grant from level 10.
            result.FusedConsciousness = f.Feature("FusedConsciousness", "Fused Consciousness",
                "At 10th level, a spiritualist always gains the benefits of Shared Consciousness, even when her phantom is manifested.");
            Phantom0085.BuildConsciousnessGrants(f, character, phantomClass, selection, emotions, result.SharedConsciousness, result.FusedConsciousness);

            // Etheric Tether / Spiritual Bond: mutually exclusive free toggles.
            result.EthericTether = Phantom0085.BuildTetherToggle(f, character, "EthericTether", "Etheric Tether",
                "Whenever her manifested phantom takes enough damage to send it back to the Ethereal Plane, as a reaction to the damage, the spiritualist can sacrifice any number of her hit points without using an action. Each hit point sacrificed in this way prevents 1 point of damage dealt to the phantom. This can prevent the phantom from being sent back to the Ethereal Plane.",
                spiritualBond: false);
            result.SpiritualBond = Phantom0085.BuildTetherToggle(f, character, "SpiritualBond", "Spiritual Bond",
                "At 14th level, a spiritualist's life force becomes intrinsically linked with the phantom's spiritual essence. As long as the phantom has 1 or more hit points, when the spiritualist takes damage that would reduce her to fewer than 0 hit points, those points of damage are transferred to the phantom instead. This transfer stops after the phantom takes all the points of damage or the phantom is reduced to a negative amount of hit points equal to its Constitution score. This ability affects only effects that deal hit point damage.",
                spiritualBond: true);

            // Bonded Manifestation / Dual Bond.
            result.BondedManifestation = Phantom0085.BuildBondedManifestation(f, character, phantomClass, selection, out var dualBond);
            result.DualBond = dualBond;

            // Spiritual Inference / Greater.
            result.SpiritualInference = Phantom0085.BuildInference(f, character, phantomClass, greater: false);
            result.GreaterSpiritualInference = Phantom0085.BuildInference(f, character, phantomClass, greater: true);

            // Phantom Recall.
            result.PhantomRecall = Phantom0085.BuildRecall(f, character);

            // Potent Phantom: second emotion's four capabilities on the same phantom.
            result.PotentPhantom = Phantom0085.BuildPotentPhantom(f, character, phantomClass, emotions);

            // Confine / Manifest ride on the emotional focus selection.
            var confine = Phantom0085.BuildConfine(f, character);
            var manifest = Phantom0085.BuildManifest(f, character);
            var selectionAdd = selection.GetComponent<AddFacts>() ?? new AddFacts { name = "Spiritualist0085SelectionFacts" };
            if (selection.GetComponent<AddFacts>() == null)
                selection.ComponentsArray = selection.ComponentsArray.Concat(new BlueprintComponent[] { selectionAdd }).ToArray();
            Set(selectionAdd, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(confine), Ref<BlueprintUnitFactReference>(manifest) });
            return result;
        }

        private static BlueprintProgression BuildEmotion(Phantom0085.FactoryShim f, BlueprintsCache cache,
            BlueprintCharacterClass character, BlueprintCharacterClass phantomClass,
            Spiritualist0085Ids.EmotionRow row, List<string> unavailable)
        {
            var phantomArray = new[] { phantomClass };
            var bothArray = new[] { phantomClass, character };
            var unit = Phantom0085.BuildUnit(f, cache, phantomClass, row);
            var archetype = Phantom0085.BuildArchetype(f, phantomClass, row);
            Phantom0085.AttachArchetype(f, unit, archetype);

            // Master-side SLA features at 5/7/9/16 with per-emotion resources.
            var slaFeatures = new BlueprintFeature[4];
            for (int i = 0; i < 4; i++)
            {
                var root = ResolveSla(cache, row.SlaGuids[i], row.SlaProviders[i], unavailable, row.Name);
                int start = i == 3 ? 16 : 5 + i * 2;
                var resource = Resource(f, row.Name + "Sla" + i + "Resource", character, 0, start, 4);
                BlueprintAbility sla = null;
                if (root != null)
                    sla = Phantom0085.ConvertToSpellLike(f, cache, root, row.Name + "Sla" + i, character, character, StatType.Wisdom, resource);
                var feature = f.Feature(row.Name + "Sla" + i + "Feature", "Emotional Power: " + (root != null ? Phantom0085.NameOf(root) : row.Name),
                    "A spiritualist gains spell-like abilities tied to her phantom's emotional focus: one at 5th level, a second at 7th, a third at 9th and a fourth at 16th. Each can be used once per day, plus one additional time per day for every 4 spiritualist levels beyond the level at which it was gained. The save DC equals 10 + 1/2 spiritualist class level + Wisdom modifier.",
                    sla != null ? Facts(sla) : new AddFacts { name = "Spiritualist0085EmptySla" },
                    Grant(resource));
                slaFeatures[i] = feature;
            }

            // The private grant follows the companion owned by native AddPet.
            var petFeatures = Phantom0085.BuildEmotionFeatures(f, cache, character, phantomClass, row);
            var grants = petFeatures.Select(p => Phantom0085.GrantToPet(f, row.Name + "Grant" + p.name, p)).ToArray();

            var progression = f.Progression(row.Name + "Emotion", "Emotion Focus: " + row.Name, row.Description,
                new[]
                {
                    Entry(1, grants[0]), Entry(5, slaFeatures[0]), Entry(7, grants[1], slaFeatures[1]),
                    Entry(9, slaFeatures[2]), Entry(12, grants[2]), Entry(16, slaFeatures[3]), Entry(17, grants[3])
                },
                new[] { Group(grants), Group(slaFeatures) });
            Set(progression, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(character) } });
            progression.IsClassFeature = true;
            progression.ReapplyOnLevelUp = true;

            // One native AddPet per emotion; context level branch reads the
            // committed Spiritualist level, force auto-levelup, destroy on remove.
            var addPet = new AddPet { name = "Spiritualist0085AddPet" + row.Name, Type = Spiritualist0085Ids.PhantomPetType };
            Set(addPet, "m_Pet", Ref<BlueprintUnitReference>(unit));
            Set(addPet, "m_UseContextValueLevel", true);
            Set(addPet, "m_LevelContextValue", new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default });
            Set(addPet, "m_ForceAutoLevelup", true);
            Set(addPet, "m_DestroyPetOnDeactivate", true);
            Set(addPet, "m_LevelRank", null);
            Set(addPet, "m_UpgradeFeature", null);
            addPet.UpgradeLevel = 0;
            var link = new Phantom0085Link { name = "Spiritualist0085Link" + row.Name, Emotion = row.Name };
            var levelRank = new ContextRankConfig { name = "Spiritualist0085PetLevel" + row.Name };
            SetEnum(levelRank, "m_BaseValueType", "ClassLevel");
            SetEnum(levelRank, "m_Progression", "AsIs");
            Set(levelRank, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(character) });
            progression.ComponentsArray = progression.ComponentsArray.Concat(new BlueprintComponent[]
            {
                addPet, link, levelRank
            }).ToArray();
            return progression;
        }
    }
}
