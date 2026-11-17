// COTW-0099 Occultist, B stage, first vertical slice: Abjuration.
// Private identity namespace. The A-occultist-assets.json GUIDs are the
// Kingmaker originals and are never reused as Wrath blueprint ids.
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace CallOfTheWild.Wrath.Prototype
{
    public static class Occultist0099Ids
    {
        public const string Contract = "C-0099-20261115-r1";
        public const string Salt = "CotW.Wrath.Occultist0099.v1/";
        public const string SliceSchool = "Abjuration";
        public const bool SlicePublished = true;

        public static string Id(string token)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(Salt + token));
                return string.Concat(hash.Select(b => b.ToString("x2")));
            }
        }

        public static readonly string ClassGuid = Id("OccultistClass");
        public static readonly string BookGuid = Id("OccultistSpellbook");
        public static readonly string SpellListGuid = Id("OccultistSpellList");
        public static readonly string FocusFactGuid = Id("MentalFocusFact");
        public static readonly string FocusResourceGuid = Id("MentalFocusResource");
        public static readonly string LockFocusAbilityGuid = Id("LockFocusAbility");
        public static readonly string ResetFocusAbilityGuid = Id("ResetFocusAbility");
        public static readonly string InvestAbilityGuid = Id("InvestFocusAbility");
        public static readonly string ReplenishAbilityGuid = Id("ReplenishFocusAbility");
        public static readonly string WardingTalismanBuffGuid = Id("WardingTalismanBuff");
        public static readonly string MindBarrierAbilityGuid = Id("MindBarrierAbility");
        public static readonly string MindBarrierBuffGuid = Id("MindBarrierBuff");
        public static readonly string ImplementSelectionGuid = Id("ImplementSelection");
        public static readonly string ProgressionGuid = Id("OccultistProgression");

        // Private TypeId block for the 0099 runtime components. Kept in one
        // place so later school packages cannot collide with other packages.
        // D-0099-08 fix: the native components reference these CONSTANTS in
        // their [TypeId] attributes instead of hardcoding the hex strings.
        public const string FocusFactTypeId = "0099c0e1a1b24c0d8e5f600000000001";
        public const string SpellbookMarkerTypeId = "0099c0e1a1b24c0d8e5f600000000002";
        // D-0099-01: the three native entry-point components wired in
        // Occultist0099NativeWiring.cs. Same private block, same numbering.
        public const string FocusResourceLogicTypeId = "0099c0e1a1b24c0d8e5f600000000003";
        // Reserved for the retired, development-only donor-root LearnGate.
        public const string LearnGateTypeId = "0099c0e1a1b24c0d8e5f600000000004";
        public const string SpellbookFactTypeId = "0099c0e1a1b24c0d8e5f600000000005";
        public const string FocusPartTypeId = "0099c0e1a1b24c0d8e5f600000000006";
        public const string FocusPropertyTypeId = "0099c0e1a1b24c0d8e5f600000000007";
        public const string FocusOperationTypeId = "0099c0e1a1b24c0d8e5f600000000008";
        public const string ImplementSchoolTypeId = "0099c0e1a1b24c0d8e5f600000000009";
        public const string ImplementSlotTypeId = "0099c0e1a1b24c0d8e5f60000000000a";
        public const string SchoolValueTypeId = "0099c0e1a1b24c0d8e5f60000000000b";
        public const string SameRaceTypeId = "0099c0e1a1b24c0d8e5f60000000000c";
        public const string SchoolPowerGrantTypeId = "0099c0e1a1b24c0d8e5f60000000000d";
        public const string EnergyShieldTypeId = "0099c0e1a1b24c0d8e5f60000000000e";
        public const string GlobeOfNegationTypeId = "0099c0e1a1b24c0d8e5f60000000000f";
        public const string AegisOptionTypeId = "0099c0e1a1b24c0d8e5f600000000010";
        public const string AegisTargetTypeId = "0099c0e1a1b24c0d8e5f600000000011";
        public const string AegisArmorTypeId = "0099c0e1a1b24c0d8e5f600000000012";
        public const string AegisStorageTypeId = "0099c0e1a1b24c0d8e5f600000000013";
        public const string AegisStoreSpellTypeId = "0099c0e1a1b24c0d8e5f600000000014";
        public const string AegisReleaseTypeId = "0099c0e1a1b24c0d8e5f600000000015";
        public const string AegisReleaseActionTypeId = "0099c0e1a1b24c0d8e5f600000000016";
        public const string AegisApplyTypeId = "0099c0e1a1b24c0d8e5f600000000017";
        public const string AegisGrantsTypeId = "0099c0e1a1b24c0d8e5f600000000018";
        public const string ProphecyReadyTypeId = "0099c0e1a1b24c0d8e5f600000000019";
        public const string ShadowTierTypeId = "0099c0e1a1b24c0d8e5f60000000001a";
        public const string ShadowHealthTypeId = "0099c0e1a1b24c0d8e5f60000000001b";
        public const string ShadowHealthActionTypeId = "0099c0e1a1b24c0d8e5f60000000001c";
        public const string PowerLevelTypeId = "0099c0e1a1b24c0d8e5f60000000001d";
        public const string LegacyWeaponOptionTypeId = "0099c0e1a1b24c0d8e5f60000000001e";
        public const string LegacyWeaponTargetTypeId = "0099c0e1a1b24c0d8e5f60000000001f";
        public const string LegacyWeaponBuffTypeId = "0099c0e1a1b24c0d8e5f600000000020";
        public const string PhilosophersTouchBuffTypeId = "0099c0e1a1b24c0d8e5f600000000021";
        public const string QuicknessCounterTypeId = "0099c0e1a1b24c0d8e5f600000000022";
        public const string NecromanticFocusTypeId = "0099c0e1a1b24c0d8e5f600000000023";
        public const string SoulboundPuppetTypeId = "0099c0e1a1b24c0d8e5f600000000024";
        public const string ServantTargetTypeId = "0099c0e1a1b24c0d8e5f600000000026";
        public const string ServantConditionTypeId = "0099c0e1a1b24c0d8e5f600000000027";
        public const string AnimateServantTypeId = "0099c0e1a1b24c0d8e5f600000000028";
        public const string CastingFocusTypeId = "0099c0e1a1b24c0d8e5f600000000029";
        public const string RadianceBuffTypeId = "0099c0e1a1b24c0d8e5f60000000002b";
        public const string IntenseFocusTypeId = "0099c0e1a1b24c0d8e5f60000000002c";
        public const string PanoplyCarrierTypeId = "0099c0e1a1b24c0d8e5f60000000002d";
        public const string PanoplyValueTypeId = "0099c0e1a1b24c0d8e5f60000000002e";
        public const string PanoplyVisibilityTypeId = "0099c0e1a1b24c0d8e5f60000000002f";
        public const string FontOfHealingTypeId = "0099c0e1a1b24c0d8e5f600000000030";
        public const string CounterstrikeTypeId = "0099c0e1a1b24c0d8e5f600000000031";
        public const string SpellPowerTypeId = "0099c0e1a1b24c0d8e5f600000000032";
        public const string MetamagicMasterTypeId = "0099c0e1a1b24c0d8e5f600000000033";
        public const string RepowerConstructTypeId = "0099c0e1a1b24c0d8e5f600000000034";
        public const string ImplementMasteryTypeId = "0099c0e1a1b24c0d8e5f600000000035";
        public const string SpellPowerAvailabilityTypeId = "0099c0e1a1b24c0d8e5f600000000036";
        public const string MetamagicMasterToggleTypeId = "0099c0e1a1b24c0d8e5f600000000037";
        public const string RepowerTargetTypeId = "0099c0e1a1b24c0d8e5f600000000038";
        public const string RepowerLifecycleTypeId = "0099c0e1a1b24c0d8e5f600000000039";
        public const string CounterstrikeAvailabilityTypeId = "0099c0e1a1b24c0d8e5f60000000003a";

        // The per-unit spell-state holder fact (O-S02). Distinct from the
        // Mental Focus fact so the two state machines stay separable.
        public static readonly string SpellbookFactGuid = Id("OccultistSpellbookFact");

        public static string TypeId(int index)
            => "0099c0e1a1b24c0d8e5f6000" + index.ToString("x8");

        public static readonly string[] SliceTokens =
        {
            "OccultistClass", "OccultistSpellbook", "OccultistSpellList",
            "MentalFocusFact", "MentalFocusResource", "LockFocusAbility",
            "ResetFocusAbility", "InvestFocusAbility", "ReplenishFocusAbility",
            "WardingTalismanBuff", "MindBarrierAbility", "MindBarrierBuff", "ImplementSelection",
            "OccultistProgression", "OccultistSpellbookFact", "MindBarrierAbsorption",
            "WardingTalismanResistance", "AbjurationImplementFocus", "AbjurationExtraSpells",
            "slot-1-Abjuration-LearnList", "slot-2-Abjuration-ExtraLearnList", "ZeroSpellsKnown",
            "ExtraImplementSelection", "OccultistKnacksFeature"
        };
    }

    // Startup publishes the complete eight-school, four-panoply career through
    // the shared registration chain. Directed fixtures can also use a detached cache.
    public static class Occultist0099Slice
    {
        public static string Contract => Occultist0099Ids.Contract;
        public static string School => Occultist0099Ids.SliceSchool;
        public static bool Published => Occultist0099Ids.SlicePublished;
        public static bool IsRuntimePatch(Type type)
            => type.Name.StartsWith("Occultist0099Native", StringComparison.Ordinal);

        // A partial implement map is never eligible for publication.
        public static bool CanRegister(bool fullMapComplete)
            => Occultist0099Ids.SlicePublished && fullMapComplete;

        // Read through a method so the startup module keeps a runtime gate
        // instead of a compile-time constant branch.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool StartupEnabled() => Occultist0099Ids.SlicePublished;

        public static string[] NativeEntryPoints =
        {
            "Mental Focus fact: authoritative per-unit state (O-S01)",
            "Invest / Reset / Full Round Lock abilities (O-S01)",
            "Replenish Focus ability, distinct from pre-lock Invest (O-S01)",
            "AbilityResourceLogic / activation precheck then atomic payment",
            "IUnitRestHandler successful-rest cycle, idempotent by event id",
            "ImplementSelection fact and private spellbook quota sources (O-S02)",
            "Mind Barrier (Swift, 2L absorption) and Warding Talisman resonance"
        };

        public static string[] DeferredToLaterSchoolPackages =
        {
            "Seven remaining schools and their focus powers",
            "Four Panoplies and derived spell sources",
            "full progression",
            "Complete 143-row private spell table and 12 optional donors",
            "Shared BlueprintRegistration / ModuleCatalog wiring and PR #14 review",
            "Real in-game level-up UI, save/load, respec and donor enable checks"
        };
    }
}
