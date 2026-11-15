// COTW-0099 Occultist, B stage, first vertical slice: Abjuration.
// Private identity namespace. The A-occultist-assets.json GUIDs are the
// Kingmaker originals and are never reused as Wrath blueprint ids.
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CallOfTheWild.Wrath.Prototype
{
    public static class Occultist0099Ids
    {
        public const string Contract = "C-0099-20261115-r1";
        public const string Salt = "CotW.Wrath.Occultist0099.v1/";
        public const string SliceSchool = "Abjuration";
        public const bool SlicePublished = false;

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
        public static readonly string ImplementSelectionGuid = Id("ImplementSelection");
        public static readonly string ProgressionGuid = Id("OccultistProgression");

        // Private TypeId block for the 0099 runtime components. Kept in one
        // place so later school packages cannot collide with other packages.
        public static string TypeId(int index)
            => "0099c0e1a1b24c0d8e5f6000" + index.ToString("x8");

        public static readonly string[] SliceTokens =
        {
            "OccultistClass", "OccultistSpellbook", "OccultistSpellList",
            "MentalFocusFact", "MentalFocusResource", "LockFocusAbility",
            "ResetFocusAbility", "InvestFocusAbility", "ReplenishFocusAbility",
            "WardingTalismanBuff", "MindBarrierAbility", "ImplementSelection",
            "OccultistProgression"
        };
    }

    // B first slice is development-only by contract: eight schools, four
    // Panoplies, progression and the full private spell table must be complete
    // before the same owner registers the class in the shared registration
    // chain. Nothing here calls BlueprintRegistration.RegisterAll.
    public static class Occultist0099Slice
    {
        public static string Contract => Occultist0099Ids.Contract;
        public static string School => Occultist0099Ids.SliceSchool;
        public static bool Published => Occultist0099Ids.SlicePublished;

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
            "Implement Mastery, Repower Construct ability, full progression",
            "Complete 143-row private spell table and 12 optional donors",
            "Shared BlueprintRegistration / ModuleCatalog wiring and PR #14 review",
            "Real in-game level-up UI, save/load, respec and donor enable checks"
        };
    }
}
