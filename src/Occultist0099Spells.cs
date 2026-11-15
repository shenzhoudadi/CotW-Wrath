// COTW-0099 Occultist, B stage, first vertical slice: Abjuration.
// Frozen contract C-0099-20261115-r1, section O-S02 (implements and spell
// learning) plus the cross-book psychic boundary.
//
// Pure state model, no game dependency. The native level-up selection, the
// private spellbook and the temporary-known-spell ledger are wired to this
// same state in the later school packages; the isolated rules host links this
// file directly for the numeric acceptance cases.
using System;
using System.Collections.Generic;
using System.Linq;

namespace CallOfTheWild.Wrath.Prototype
{
    public sealed class Occultist0099SpellRoot
    {
        public string Guid;
        public string Name;
        public int Circle;               // the 0099 private circle, not the native one
        public string School;
        public string Source;            // Wrath | CotW | External
        public string Provider;          // external donor id, null otherwise
        public bool RequiresPublishedList;
        public bool IsVariant;
        public string CanonicalGuid;     // variants point at the canonical root
        public bool IsKnack;
    }

    // A-occultist-assets.json, spellRoots rows with school == Abjuration.
    // 18 rows: 6 knacks-wide catalog is separate; circles 0..6 here.
    public static class Occultist0099AbjurationCatalog
    {
        public const string WardingWeapon = "1ce11d27e8d07955d1012a6abaabfbe0";
        public const string MageShield = "ef768022b0785eb43a18969903c537c4";
        public const string AlliedCloak = "57252e407614fb087709d40d6c86353b";
        public const string ResistEnergy = "21ffef7791ce73f468b6fca4d9371e8b";
        public const string GlobeOfInvulnerability = "8e2d102dd34eb1418ba38753ff3771d4";
        public const string CloakOfWinds = "6fb10fff27b84bff81746194bf8e38a8";

        public static readonly string[] Knacks =
        {
            "55f14bc84d7c85446b07a1b5dd6b2b4c", // Daze
            "f0f8e5b9808f44e4eadd22b138131d52", // Flare
            "c3a8f31778c3980498d8f00c980be5f5", // Guidance
            "95f206566c5261c42aa5b3e7e0d1e36c", // MageLight
            "7bc8e27cba24f0e43ae64ed201ad5785", // Resistance
            "5bf3315ce1ed4d94e8805706820ef64d"  // TouchOfFatigueCast
        };

        public static readonly Occultist0099SpellRoot[] Roots =
        {
            R("7bc8e27cba24f0e43ae64ed201ad5785", "Resistance", 0, "Wrath", true),
            R("ef768022b0785eb43a18969903c537c4", "MageShield", 1, "Wrath"),
            R(WardingWeapon, "CotWWrathPsychicWardingWeapon", 1, "CotW"),
            R(AlliedCloak, "CotWWrathPsychicAlliedCloak", 2, "CotW"),
            R(ResistEnergy, "ResistEnergy", 2, "Wrath"),
            R(CloakOfWinds, "CloakOfWindsAbility", 3, "External", false,
              "TabletopTweaks-Base", true),
            R("92681f181b507b34ea87018e8f7a528a", "DispelMagic", 3, "Wrath"),
            R("d2f116cfe05fcdd4a94e80143b67046f", "ProtectionFromEnergy", 3, "Wrath"),
            R("7bb0c402f7f789d4d9fae8ca87b4c7e2", "ResistEnergyCommunal", 3, "Wrath"),
            R("7792da00c85b9e042a0fdfc2b66ec9a8", "BreakEnchantment", 4, "Wrath"),
            R("95f7cdcec94e293489a85afdf5af1fd7", "Dismissal", 4, "Wrath"),
            R("0087fc2d64b6095478bc7b8d7d512caf", "FreedomOfMovementCast", 4, "Wrath"),
            R("7243c2cbfe8b027021a9f7b0270d7171",
              "CotWWrathPsychicGlobeOfInvulnerabilityLesser", 4, "CotW"),
            R("c66e86905f7606c4eaa5c774f0357b2b", "Stoneskin", 4, "Wrath"),
            R("f0f761b808dc4b149b08eaf44b99f633", "DispelMagicGreater", 5, "Wrath"),
            R("0a5ddfbcfb3989543ac7c936fc256889", "SpellResistance", 5, "Wrath"),
            R("7c5d556b9a5883048bf030e20daebe31", "StoneskinCommunal", 5, "Wrath"),
            R(GlobeOfInvulnerability, "CotWWrathPsychicGlobeOfInvulnerability", 6, "CotW")
        };

        private static Occultist0099SpellRoot R(string guid, string name, int circle,
            string source, bool knack = false, string provider = null,
            bool publishedList = false)
        {
            return new Occultist0099SpellRoot
            {
                Guid = guid, Name = name, Circle = circle, School = "Abjuration",
                Source = source, Provider = provider,
                RequiresPublishedList = publishedList, IsKnack = knack,
                CanonicalGuid = guid
            };
        }

        // 1/4/7/10/13/16 open circles 1..6, taken from the Wrath Alchemist
        // six-circle table donor (A JSON dailySpellSlots).
        public static int MaxCircleForLevel(int classLevel)
        {
            if (classLevel >= 16) return 6;
            if (classLevel >= 13) return 5;
            if (classLevel >= 10) return 4;
            if (classLevel >= 7) return 3;
            if (classLevel >= 4) return 2;
            if (classLevel >= 1) return 1;
            return 0;
        }

        public static int ClassLevelForCircle(int circle)
        {
            int[] levels = { 0, 1, 4, 7, 10, 13, 16 };
            return circle >= 1 && circle <= 6 ? levels[circle] : 0;
        }

        public static Occultist0099SpellRoot Find(string guid)
            => Roots.FirstOrDefault(r => r.Guid == guid);

        // 143 rows live in A-occultist-assets.json. This slice registers only
        // the 18 Abjuration rows; the remaining 125 belong to later packages.
        public static int TotalCatalogRows => 143;
        public static int SliceRows => Roots.Length;
    }

    public sealed class Occultist0099Slot
    {
        public string Key;
        public string School;             // null for a Panoply
        public string Panoply;            // null for a school
        public bool ExtraSpells;
        public int Sequence;
    }

    public sealed class Occultist0099Source
    {
        public string Id;
        public string SlotKey;
        public int Circle;
        public string RootGuid;
        public string Origin;             // "implement" | "same-book-grant"
    }

    public sealed class Occultist0099SpellState
    {
        public int ClassLevel;
        public int NextSequence = 1;
        public List<Occultist0099Slot> Slots = new List<Occultist0099Slot>();
        public Dictionary<string, string> Quotas = new Dictionary<string, string>();
        public List<Occultist0099Source> Sources = new List<Occultist0099Source>();
        public Dictionary<string, bool> Providers = new Dictionary<string, bool>();
        public Dictionary<string, bool> PublishedLists = new Dictionary<string, bool>();
        public HashSet<string> DlcAllowed = new HashSet<string>();
        public HashSet<string> CotWRootsPublished = new HashSet<string>();
        public HashSet<string> ForeignBookRoots = new HashSet<string>();
        public HashSet<string> SameBookGrantSources = new HashSet<string>();
        public bool CotWRegistrationComplete = true;
        public HashSet<string> KnownRootsOverride = new HashSet<string>();

        public string Fingerprint()
        {
            var parts = new List<string> { ClassLevel.ToString(), NextSequence.ToString() };
            foreach (var s in Slots)
                parts.Add("slot:" + s.Key + ":" + (s.School ?? s.Panoply) + ":" +
                          (s.ExtraSpells ? 1 : 0) + ":" + s.Sequence);
            foreach (var q in Quotas.OrderBy(k => k.Key))
                parts.Add("quota:" + q.Key + ":" + (q.Value ?? "-"));
            foreach (var s in Sources.OrderBy(k => k.Id))
                parts.Add("src:" + s.Id + ":" + s.SlotKey + ":" + s.Circle + ":" +
                          s.RootGuid + ":" + s.Origin);
            parts.Add("foreign=" + string.Join(",", ForeignBookRoots.OrderBy(x => x)));
            return string.Join("|", parts);
        }

        public Occultist0099SpellState Clone()
        {
            var copy = new Occultist0099SpellState
            {
                ClassLevel = ClassLevel, NextSequence = NextSequence,
                CotWRegistrationComplete = CotWRegistrationComplete
            };
            foreach (var s in Slots)
                copy.Slots.Add(new Occultist0099Slot
                {
                    Key = s.Key, School = s.School, Panoply = s.Panoply,
                    ExtraSpells = s.ExtraSpells, Sequence = s.Sequence
                });
            foreach (var q in Quotas) copy.Quotas[q.Key] = q.Value;
            foreach (var s in Sources)
                copy.Sources.Add(new Occultist0099Source
                {
                    Id = s.Id, SlotKey = s.SlotKey, Circle = s.Circle,
                    RootGuid = s.RootGuid, Origin = s.Origin
                });
            foreach (var p in Providers) copy.Providers[p.Key] = p.Value;
            foreach (var p in PublishedLists) copy.PublishedLists[p.Key] = p.Value;
            foreach (string x in DlcAllowed) copy.DlcAllowed.Add(x);
            foreach (string x in CotWRootsPublished) copy.CotWRootsPublished.Add(x);
            foreach (string x in ForeignBookRoots) copy.ForeignBookRoots.Add(x);
            foreach (string x in SameBookGrantSources) copy.SameBookGrantSources.Add(x);
            foreach (string x in KnownRootsOverride) copy.KnownRootsOverride.Add(x);
            return copy;
        }
    }

    public static class Occultist0099Spells
    {
        public static Occultist0099SpellState Create(int classLevel = 1)
        {
            var s = new Occultist0099SpellState { ClassLevel = classLevel };
            foreach (string root in Occultist0099AbjurationCatalog.Roots
                         .Where(r => r.Source == "CotW").Select(r => r.Guid))
                s.CotWRootsPublished.Add(root);
            foreach (string root in Occultist0099AbjurationCatalog.Roots
                         .Select(r => r.Guid))
                s.DlcAllowed.Add(root);
            s.Providers["TabletopTweaks-Base"] = false;
            s.PublishedLists["TabletopTweaks-Base"] = false;
            OpenCircles(s);
            return s;
        }

        public static string QuotaKey(string slotKey, int circle)
            => slotKey + ":" + circle;

        public static int MaxCircle(Occultist0099SpellState s)
            => Occultist0099AbjurationCatalog.MaxCircleForLevel(s.ClassLevel);

        private static void OpenCircles(Occultist0099SpellState s)
        {
            int max = MaxCircle(s);
            foreach (var slot in s.Slots)
                for (int circle = 1; circle <= max; circle++)
                {
                    string key = QuotaKey(slot.Key, circle);
                    if (!s.Quotas.ContainsKey(key)) s.Quotas[key] = null;
                }
        }

        public static Occultist0099Outcome SetLevel(Occultist0099SpellState s, int level)
        {
            if (s == null || level < 0 || level > 20)
                return Occultist0099Outcome.RejectedInvalidArgument;
            s.ClassLevel = level;
            OpenCircles(s);   // new circle gives every existing slot one choice
            return Occultist0099Outcome.Ok;
        }

        public static int QuotaCount(Occultist0099SpellState s, int circle)
            => s.Quotas.Count(q => q.Key.EndsWith(":" + circle));

        public static string[] SchoolsOf(Occultist0099SpellState s, string slotKey)
        {
            var slot = s.Slots.FirstOrDefault(x => x.Key == slotKey);
            if (slot == null) return new string[0];
            if (slot.School != null) return new[] { slot.School };
            return Occultist0099Schools.MembersOf(slot.Panoply) ?? new string[0];
        }

        // O-S02: one base implement per school, at most one Extra Spells for
        // the same school, and Extra Spells needs the first fact first. A
        // Panoply needs every member school present and can only be chosen once.
        public static Occultist0099Outcome AddSlot(Occultist0099SpellState s,
            string slotKey, string school, bool extraSpells = false,
            string panoply = null)
        {
            if (s == null || string.IsNullOrEmpty(slotKey))
                return Occultist0099Outcome.RejectedInvalidArgument;
            if (s.Slots.Any(x => x.Key == slotKey))
                return Occultist0099Outcome.RejectedInvalidArgument;
            bool isPanoply = panoply != null;
            if (isPanoply)
            {
                string[] members = Occultist0099Schools.MembersOf(panoply);
                if (members == null)
                    return Occultist0099Outcome.RejectedUnknownSchool;
                if (s.Slots.Any(x => x.Panoply == panoply))
                    return Occultist0099Outcome.RejectedDuplicate;
                if (members.Any(m => !s.Slots.Any(x => x.School == m)))
                    return Occultist0099Outcome.RejectedNotOwned;
            }
            else
            {
                if (!Occultist0099Schools.IsSchool(school))
                    return Occultist0099Outcome.RejectedUnknownSchool;
                var same = s.Slots.Where(x => x.School == school).ToArray();
                if (extraSpells && same.Length == 0)
                    return Occultist0099Outcome.RejectedNotOwned; // SP01 ordering
                if (same.Length >= 2 || (extraSpells && same.Any(x => x.ExtraSpells)))
                    return Occultist0099Outcome.RejectedDuplicate; // third selection
            }
            var slot = new Occultist0099Slot
            {
                Key = slotKey, School = isPanoply ? null : school,
                Panoply = panoply, ExtraSpells = extraSpells && !isPanoply,
                Sequence = s.NextSequence++
            };
            s.Slots.Add(slot);
            OpenCircles(s);    // a new implement fills every already open circle
            return Occultist0099Outcome.Ok;
        }

        public static bool IsRootAvailable(Occultist0099SpellState s,
            Occultist0099SpellRoot root)
        {
            if (root == null || root.IsVariant) return false;
            if (root.Source == "CotW")
                return s.CotWRegistrationComplete &&
                       s.CotWRootsPublished.Contains(root.Guid);
            if (root.Source == "External")
            {
                bool enabled;
                if (!s.Providers.TryGetValue(root.Provider, out enabled) || !enabled)
                    return false;
                if (root.RequiresPublishedList)
                {
                    bool published;
                    if (!s.PublishedLists.TryGetValue(root.Provider, out published) ||
                        !published) return false;
                }
                return s.DlcAllowed.Contains(root.Guid);
            }
            return s.DlcAllowed.Contains(root.Guid);
        }

        // Candidate roots are filtered by the slot's school(s), the private
        // circle, canonical identity, provider/DLC gates and roots already
        // known in THIS spellbook. Other books never enter the candidate list.
        public static Occultist0099SpellRoot[] Candidates(Occultist0099SpellState s,
            string slotKey, int circle)
        {
            string key = QuotaKey(slotKey, circle);
            if (!s.Quotas.ContainsKey(key) || s.Quotas[key] != null)
                return new Occultist0099SpellRoot[0];
            string[] schools = SchoolsOf(s, slotKey);
            var known = new HashSet<string>(s.Sources.Select(x => x.RootGuid));
            return Occultist0099AbjurationCatalog.Roots
                .Where(r => r.Circle == circle && !r.IsKnack &&
                            r.Guid == r.CanonicalGuid &&
                            schools.Contains(r.School) &&
                            !known.Contains(r.Guid) && IsRootAvailable(s, r))
                .ToArray();
        }

        public static Occultist0099Outcome Learn(Occultist0099SpellState s,
            string slotKey, int circle, string rootGuid)
        {
            if (s == null) return Occultist0099Outcome.RejectedInvalidState;
            string key = QuotaKey(slotKey, circle);
            if (!s.Quotas.ContainsKey(key))
                return Occultist0099Outcome.RejectedInsufficient; // circle not open
            if (s.Quotas[key] != null)
                return Occultist0099Outcome.RejectedDuplicate;
            var root = Occultist0099AbjurationCatalog.Find(rootGuid);
            if (!Candidates(s, slotKey, circle).Any(r => r.Guid == rootGuid))
                return Occultist0099Outcome.RejectedInvalidArgument;
            // A quota holds exactly one canonical root. Variants and sticky
            // touch delivery never consume another quota.
            s.Quotas[key] = root.CanonicalGuid;
            s.Sources.Add(new Occultist0099Source
            {
                Id = "src-" + s.NextSequence++, SlotKey = slotKey,
                Circle = circle, RootGuid = root.CanonicalGuid,
                Origin = "implement"
            });
            return Occultist0099Outcome.Ok;
        }

        // A second legal grant inside the SAME book keeps a root known even
        // when its implement source is removed. A Wizard/other book copy is
        // tracked separately and is never touched by Occultist cleanup.
        public static Occultist0099Outcome AddSameBookGrant(
            Occultist0099SpellState s, string sourceId, string rootGuid, int circle)
        {
            if (s == null || string.IsNullOrEmpty(sourceId))
                return Occultist0099Outcome.RejectedInvalidArgument;
            if (s.SameBookGrantSources.Contains(sourceId))
                return Occultist0099Outcome.RejectedDuplicate;
            s.SameBookGrantSources.Add(sourceId);
            s.Sources.Add(new Occultist0099Source
            {
                Id = sourceId, SlotKey = "same-book-grant", Circle = circle,
                RootGuid = rootGuid, Origin = "same-book-grant"
            });
            return Occultist0099Outcome.Ok;
        }

        public static bool IsKnown(Occultist0099SpellState s, string rootGuid)
            => s.Sources.Any(x => x.RootGuid == rootGuid);

        public static string[] KnownRoots(Occultist0099SpellState s)
            => s.Sources.Select(x => x.RootGuid).Distinct().OrderBy(x => x).ToArray();

        public static Occultist0099Outcome RemoveSameBookGrant(
            Occultist0099SpellState s, string sourceId)
        {
            if (s == null) return Occultist0099Outcome.RejectedInvalidState;
            if (!s.SameBookGrantSources.Remove(sourceId))
                return Occultist0099Outcome.RejectedNotOwned;
            s.Sources.RemoveAll(x => x.Id == sourceId);
            return Occultist0099Outcome.Ok;
        }

        public static void AddForeignBookRoot(Occultist0099SpellState s, string rootGuid)
            => s.ForeignBookRoots.Add(rootGuid);

        public static void SetProvider(Occultist0099SpellState s, string provider,
            bool enabled, bool publishedList)
        {
            s.Providers[provider] = enabled;
            s.PublishedLists[provider] = publishedList;
        }

        public static Occultist0099Outcome RemoveSlot(Occultist0099SpellState s,
            string slotKey)
        {
            var slot = s == null ? null : s.Slots.FirstOrDefault(x => x.Key == slotKey);
            if (slot == null) return Occultist0099Outcome.RejectedNotOwned;
            s.Slots.Remove(slot);
            foreach (string key in s.Quotas.Keys.Where(k => k.StartsWith(slotKey + ":"))
                         .ToArray()) s.Quotas.Remove(key);
            s.Sources.RemoveAll(x => x.SlotKey == slotKey);
            // Losing a member school invalidates dependent Panoply slots and
            // every derived source; the member schools themselves stay.
            if (slot.School != null)
            {
                foreach (string panoply in Occultist0099Schools.PanopliesUsing(slot.School))
                {
                    if (!Occultist0099Schools.MembersOf(panoply)
                            .All(m => s.Slots.Any(x => x.School == m)))
                    {
                        foreach (var derived in s.Slots.Where(x => x.Panoply == panoply)
                                     .ToArray())
                        {
                            s.Slots.Remove(derived);
                            foreach (string key in s.Quotas.Keys
                                         .Where(k => k.StartsWith(derived.Key + ":"))
                                         .ToArray()) s.Quotas.Remove(key);
                            s.Sources.RemoveAll(x => x.SlotKey == derived.Key);
                        }
                    }
                }
            }
            return Occultist0099Outcome.Ok;
        }

        public static Occultist0099SpellState BeginPreview(Occultist0099SpellState s)
            => s == null ? null : s.Clone();

        public static Occultist0099Outcome CancelPreview(Occultist0099SpellState s,
            Occultist0099SpellState snapshot)
        {
            if (s == null || snapshot == null)
                return Occultist0099Outcome.RejectedInvalidState;
            // Restore byte-for-byte equivalent live state (SP03 / MF09).
            var restore = snapshot.Clone();
            s.ClassLevel = restore.ClassLevel;
            s.NextSequence = restore.NextSequence;
            s.CotWRegistrationComplete = restore.CotWRegistrationComplete;
            s.Slots = restore.Slots;
            s.Quotas = restore.Quotas;
            s.Sources = restore.Sources;
            s.Providers = restore.Providers;
            s.PublishedLists = restore.PublishedLists;
            s.DlcAllowed = restore.DlcAllowed;
            s.CotWRootsPublished = restore.CotWRootsPublished;
            s.ForeignBookRoots = restore.ForeignBookRoots;
            s.SameBookGrantSources = restore.SameBookGrantSources;
            s.KnownRootsOverride = restore.KnownRootsOverride;
            return Occultist0099Outcome.Ok;
        }

        public static string[] Knacks(Occultist0099SpellState s)
            => s.ClassLevel >= 1 ? Occultist0099AbjurationCatalog.Knacks : new string[0];
    }

    // O-S02: the 0099 book is private. Shared psychic thought/emotion and
    // Center Self / Logical apply only to the book that owns the cast; the
    // Psychic-only amplifications, undercasting and disciplines still demand
    // the real Psychic book. Items keep their native behaviour.
    public static class Occultist0099BookScope
    {
        public static bool IsOccultistBook(string bookId)
            => bookId == Occultist0099Ids.BookGuid;

        public static bool IsSharedMechanic(string mechanic)
            => mechanic == "thought" || mechanic == "emotion" ||
               mechanic == "CenterSelf" || mechanic == "Logical";

        public static bool IsPsychicOnlyMechanic(string mechanic)
            => mechanic == "Amplification" || mechanic == "Undercasting" ||
               mechanic == "Discipline" || mechanic == "Magaambyan";

        public static bool Allows(string mechanic, string castBookId,
            string mechanicOwnerBookId, string psychicBookId)
        {
            if (string.IsNullOrEmpty(castBookId)) return false; // item / no book
            if (IsPsychicOnlyMechanic(mechanic))
                return castBookId == psychicBookId;
            if (IsSharedMechanic(mechanic))
                return castBookId == mechanicOwnerBookId;
            return false;
        }

        public static int CircleOf(string bookId, string rootGuid, int nativeCircle)
        {
            if (!IsOccultistBook(bookId)) return nativeCircle;
            var root = Occultist0099AbjurationCatalog.Find(rootGuid);
            return root == null ? nativeCircle : root.Circle;
        }
    }

    // O-S02 registration atomicity (SP08). Missing core donor or a foreign
    // GUID collision leaves 0099 unpublished and foreign data intact. A
    // complete map publishes exactly one class, one spellbook and the whole
    // choice set; repeated completion never duplicates anything.
    public sealed class Occultist0099Registration
    {
        public int ClassEntries;
        public int SpellbookEntries;
        public int ChoiceEntries;
        public bool PreviewLedgerPresent;
        public bool ForeignDataTouched;

        public bool Published => ClassEntries == 1 && SpellbookEntries == 1 &&
                                 ChoiceEntries > 0 && !PreviewLedgerPresent;

        public bool TryPublish(string[] mandatoryRoots, string[] availableRoots,
            bool foreignGuidCollision, int choiceCount)
        {
            PreviewLedgerPresent = true;
            if (foreignGuidCollision || choiceCount <= 0 ||
                mandatoryRoots.Any(m => !availableRoots.Contains(m)))
            {
                ClassEntries = 0; SpellbookEntries = 0; ChoiceEntries = 0;
                ForeignDataTouched = false;
                PreviewLedgerPresent = false;
                return false;
            }
            if (ClassEntries == 0)
            {
                ClassEntries = 1;
                SpellbookEntries = 1;
                ChoiceEntries = choiceCount;
            }
            PreviewLedgerPresent = false;
            return true;
        }
    }
}
