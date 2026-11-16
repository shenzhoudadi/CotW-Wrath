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
using System.Runtime.Serialization;

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
        // D-0099-06 fix: variant identity is a real, writable concept. The
        // Abjuration catalog legitimately carries no variants (Fire Shield
        // warm/cold belongs to the later Evocation package), so variant rows
        // are registered explicitly via Occultist0099Spells.AddVariantRoot
        // when a school package brings them; Candidates/Learn resolve them
        // to the canonical root and never give them a second quota.
        public string[] VariantGuids = new string[0];
    }

    // A-occultist-assets.json, spellRoots rows with school == Abjuration:
    // 18 roots (13 Wrath + 4 CotW + 1 External), PLUS a separate fixed grant of
    // six level-0 Knacks. Only Resistance is Abjuration; the other five Knacks
    // belong to other schools, so they are NOT part of the 18 Abjuration roots
    // (D-0099-09 wording fix; A JSON spellRoots.level == 0 has exactly 6 rows).
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

    [DataContract]
    public sealed class Occultist0099Slot
    {
        [DataMember] public string Key;
        [DataMember] public string School;             // null for a Panoply
        [DataMember] public string Panoply;            // null for a school
        [DataMember] public bool ExtraSpells;
        [DataMember] public int Sequence;
    }

    [DataContract]
    public sealed class Occultist0099Source
    {
        [DataMember] public string Id;
        [DataMember] public string SlotKey;
        [DataMember] public int Circle;
        [DataMember] public string RootGuid;
        [DataMember] public string Origin;             // "implement" | "same-book-grant"
    }

    [DataContract]
    public sealed class Occultist0099SpellState
    {
        [DataMember] public int ClassLevel;
        // D-0099-03 fix (C:31): learning AND casting gate Int >= 10 + circle.
        // Temporary Int changes gate new learning/casting but never delete an
        // established quota or a saved source.
        [DataMember] public int Intelligence = 10;
        [DataMember] public int NextSequence = 1;
        [DataMember] public List<Occultist0099Slot> Slots = new List<Occultist0099Slot>();
        [DataMember] public Dictionary<string, string> Quotas = new Dictionary<string, string>();
        [DataMember] public List<Occultist0099Source> Sources = new List<Occultist0099Source>();
        [DataMember] public Dictionary<string, bool> Providers = new Dictionary<string, bool>();
        [DataMember] public Dictionary<string, bool> PublishedLists = new Dictionary<string, bool>();
        [DataMember] public HashSet<string> DlcAllowed = new HashSet<string>();
        [DataMember] public HashSet<string> CotWRootsPublished = new HashSet<string>();
        [DataMember] public HashSet<string> ForeignBookRoots = new HashSet<string>();
        [DataMember] public HashSet<string> SameBookGrantSources = new HashSet<string>();
        // D-0099-06 fix: no longer a hardcoded facade. Create() sets this true
        // only because the development fixture assumes the slice's own CotW
        // roots published; the registration package will drive it from the
        // real atomic publish result, and SetCotWRegistrationComplete is the
        // single write point. While false, no CotW root enters candidates.
        [DataMember] public bool CotWRegistrationComplete;
        // D-0099-06 fix: variant rows registered by later school packages
        // (Fire Shield warm/cold, sticky touch delivery). Each maps a variant
        // GUID to its canonical root GUID; a variant never owns a quota.
        [DataMember] public Dictionary<string, string> VariantRoots = new Dictionary<string, string>();

        // D-0099-05 fix: the fingerprint covers EVERY mutable field so that
        // preview-cancel byte-for-byte assertions cannot silently miss a leak.
        public string Fingerprint()
        {
            var parts = new List<string>
            {
                ClassLevel.ToString(), Intelligence.ToString(),
                NextSequence.ToString(),
                CotWRegistrationComplete ? "1" : "0"
            };
            foreach (var s in Slots)
                parts.Add("slot:" + s.Key + ":" + (s.School ?? s.Panoply) + ":" +
                          (s.ExtraSpells ? 1 : 0) + ":" + s.Sequence);
            foreach (var q in Quotas.OrderBy(k => k.Key))
                parts.Add("quota:" + q.Key + ":" + (q.Value ?? "-"));
            foreach (var s in Sources.OrderBy(k => k.Id))
                parts.Add("src:" + s.Id + ":" + s.SlotKey + ":" + s.Circle + ":" +
                          s.RootGuid + ":" + s.Origin);
            foreach (var p in Providers.OrderBy(k => k.Key))
                parts.Add("provider:" + p.Key + ":" + (p.Value ? 1 : 0));
            foreach (var p in PublishedLists.OrderBy(k => k.Key))
                parts.Add("published:" + p.Key + ":" + (p.Value ? 1 : 0));
            parts.Add("dlc=" + string.Join(",", DlcAllowed.OrderBy(x => x)));
            parts.Add("cotwpub=" + string.Join(",", CotWRootsPublished.OrderBy(x => x)));
            parts.Add("foreign=" + string.Join(",", ForeignBookRoots.OrderBy(x => x)));
            parts.Add("grants=" + string.Join(",", SameBookGrantSources.OrderBy(x => x)));
            foreach (var v in VariantRoots.OrderBy(k => k.Key))
                parts.Add("variant:" + v.Key + ":" + v.Value);
            return string.Join("|", parts);
        }

        public Occultist0099SpellState Clone()
        {
            var copy = new Occultist0099SpellState
            {
                ClassLevel = ClassLevel, Intelligence = Intelligence,
                NextSequence = NextSequence,
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
            foreach (var v in VariantRoots) copy.VariantRoots[v.Key] = v.Value;
            return copy;
        }
    }

    public static class Occultist0099Spells
    {
        // Development fixture: Int defaults to 16 so every circle 1..6 of the
        // six-circle table is reachable; the native adapter passes the real
        // Owner.Stats.Intelligence value instead.
        public static Occultist0099SpellState Create(int classLevel = 1,
            int intelligence = 16)
        {
            var s = new Occultist0099SpellState
            {
                ClassLevel = classLevel, Intelligence = intelligence
            };
            foreach (string root in Occultist0099AbjurationCatalog.Roots
                         .Where(r => r.Source == "CotW").Select(r => r.Guid))
                s.CotWRootsPublished.Add(root);
            foreach (string root in Occultist0099AbjurationCatalog.Roots
                         .Select(r => r.Guid))
                s.DlcAllowed.Add(root);
            s.Providers["TabletopTweaks-Base"] = false;
            s.PublishedLists["TabletopTweaks-Base"] = false;
            // The slice's own CotW roots are published in the dev fixture;
            // the registration package replaces this with the atomic result.
            s.CotWRegistrationComplete = true;
            OpenCircles(s);
            return s;
        }

        public static string QuotaKey(string slotKey, int circle)
            => slotKey + ":" + circle;

        // D-0099-03: the effective open circle is min(level table, Int gate).
        // Int >= 10 + circle is required to learn or cast that circle.
        public static int MaxCircle(Occultist0099SpellState s)
        {
            int byLevel = Occultist0099AbjurationCatalog.MaxCircleForLevel(s.ClassLevel);
            int byInt = Math.Max(0, s.Intelligence - 10);
            return Math.Min(byLevel, byInt);
        }

        public static bool IntAllowsCircle(Occultist0099SpellState s, int circle)
            => s != null && circle >= 0 && s.Intelligence >= 10 + circle;

        // Casting gate: the root must be known in THIS book and the current
        // Int must still satisfy 10 + the root's private circle. A temporary
        // Int drop blocks casting but never deletes the quota or the source.
        public static bool CanCast(Occultist0099SpellState s, string rootGuid)
        {
            if (s == null || !IsKnown(s, rootGuid)) return false;
            string canonical = CanonicalOf(s, rootGuid);
            var root = Occultist0099AbjurationCatalog.Find(canonical);
            return root != null && IntAllowsCircle(s, root.Circle);
        }

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

        // Temporary or permanent Int change. Raising Int opens newly eligible
        // circles; lowering Int never removes an established quota or source
        // (C:31-32), it only gates further learning and casting.
        public static Occultist0099Outcome SetIntelligence(Occultist0099SpellState s,
            int intelligence)
        {
            if (s == null || intelligence < 0)
                return Occultist0099Outcome.RejectedInvalidArgument;
            s.Intelligence = intelligence;
            OpenCircles(s);
            return Occultist0099Outcome.Ok;
        }

        // Single write point for the CotW atomic registration result.
        public static void SetCotWRegistrationComplete(Occultist0099SpellState s,
            bool complete)
        {
            if (s != null) s.CotWRegistrationComplete = complete;
        }

        // D-0099-06: later school packages register variant rows here. A
        // variant resolves to its canonical root for candidates, learning and
        // known-spell identity; it never consumes a second quota.
        public static Occultist0099Outcome AddVariantRoot(Occultist0099SpellState s,
            string variantGuid, string canonicalGuid)
        {
            if (s == null || !IsHex32(variantGuid) || !IsHex32(canonicalGuid) ||
                variantGuid == canonicalGuid)
                return Occultist0099Outcome.RejectedInvalidArgument;
            if (Occultist0099AbjurationCatalog.Find(canonicalGuid) == null)
                return Occultist0099Outcome.RejectedInvalidArgument;
            // A catalog root can never be demoted to a variant of another root.
            if (Occultist0099AbjurationCatalog.Find(variantGuid) != null)
                return Occultist0099Outcome.RejectedInvalidArgument;
            if (s.VariantRoots.ContainsKey(variantGuid) &&
                s.VariantRoots[variantGuid] != canonicalGuid)
                return Occultist0099Outcome.RejectedDuplicate;
            s.VariantRoots[variantGuid] = canonicalGuid;
            return Occultist0099Outcome.Ok;
        }

        public static bool IsHex32(string g)
            => g != null && g.Length == 32 &&
               g.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

        public static string CanonicalOf(Occultist0099SpellState s, string rootGuid)
        {
            if (s == null || rootGuid == null) return rootGuid;
            string mapped;
            return s.VariantRoots.TryGetValue(rootGuid, out mapped) ? mapped : rootGuid;
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
            // D-0099-03: Int >= 10 + circle gates learning as well as casting.
            if (!IntAllowsCircle(s, circle)) return new Occultist0099SpellRoot[0];
            string[] schools = SchoolsOf(s, slotKey);
            var known = new HashSet<string>(s.Sources.Select(x => x.RootGuid));
            return Occultist0099AbjurationCatalog.Roots
                .Where(r => r.Circle == circle && !r.IsKnack && !r.IsVariant &&
                            r.Guid == r.CanonicalGuid &&
                            !s.VariantRoots.ContainsKey(r.Guid) &&
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
            if (!IntAllowsCircle(s, circle))
                return Occultist0099Outcome.RejectedIntTooLow;   // D-0099-03
            if (s.Quotas[key] != null)
                return Occultist0099Outcome.RejectedDuplicate;
            // A variant GUID resolves to its canonical root before candidacy;
            // the quota and the source always record the canonical identity.
            string canonicalGuid = CanonicalOf(s, rootGuid);
            var root = Occultist0099AbjurationCatalog.Find(canonicalGuid);
            if (root == null || root.IsVariant ||
                !Candidates(s, slotKey, circle).Any(r => r.Guid == canonicalGuid))
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
                RootGuid = CanonicalOf(s, rootGuid), Origin = "same-book-grant"
            });
            return Occultist0099Outcome.Ok;
        }

        // Known identity is canonical: a variant GUID counts as known exactly
        // when its canonical root is held by a source in THIS book.
        public static bool IsKnown(Occultist0099SpellState s, string rootGuid)
        {
            if (s == null) return false;
            string canonical = CanonicalOf(s, rootGuid);
            return s.Sources.Any(x => x.RootGuid == canonical);
        }

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
            // Every mutable field is assigned; Fingerprint() covers them all.
            var restore = snapshot.Clone();
            s.ClassLevel = restore.ClassLevel;
            s.Intelligence = restore.Intelligence;
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
            s.VariantRoots = restore.VariantRoots;
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

    // O-S02 registration atomicity (SP08). D-0099-06 fix: the gate now works
    // over real 32-hex blueprint GUIDs and an explicit cache dictionary with
    // owner tokens, and ForeignDataTouched is a genuine before/after diff of
    // every non-0099 entry instead of a hardcoded false. The native wiring
    // (task #11) feeds it the real BlueprintsCache snapshot; until that runs,
    // SP08 stays not_run for acceptance purposes.
    public sealed class Occultist0099Registration
    {
        public const string OwnerToken = "0099";

        public int ClassEntries;
        public int SpellbookEntries;
        public int ChoiceEntries;
        public bool PreviewLedgerPresent;
        public bool ForeignDataTouched;

        // The only identities this gate may write. Everything else in the
        // cache is foreign data and must remain byte-identical.
        public static readonly string[] PrivateGuids = Occultist0099Ids.SliceTokens.Select(Occultist0099Ids.Id).ToArray();

        public bool Published => ClassEntries == 1 && SpellbookEntries == 1 &&
                                 ChoiceEntries > 0 && !PreviewLedgerPresent;

        private static bool IsPrivate(string guid)
            => Array.IndexOf(PrivateGuids, guid) >= 0;

        private static Dictionary<string, string> ForeignSubset(
            Dictionary<string, string> cache)
            => cache.Where(kv => !IsPrivate(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

        // cache: guid -> owner token, shared with foreign packages. A foreign
        // owner already holding one of our private GUIDs is a collision; the
        // attempt must fail without writing anything, foreign or private.
        // afterForeignWrite (optional) is invoked between the foreign snapshot
        // and the private write, so a concurrent foreign mutation during
        // publication is caught by the genuine before/after diff.
        public bool TryPublish(string[] mandatoryRoots, string[] availableRoots,
            Dictionary<string, string> cache, int choiceCount,
            Action afterForeignWrite = null)
        {
            PreviewLedgerPresent = true;
            ForeignDataTouched = false;
            var foreignBefore = ForeignSubset(cache);

            bool validGuids = mandatoryRoots != null && availableRoots != null &&
                mandatoryRoots.All(Occultist0099Spells.IsHex32) &&
                availableRoots.All(Occultist0099Spells.IsHex32);
            bool collision = PrivateGuids.Any(g =>
                cache.ContainsKey(g) && cache[g] != OwnerToken);
            if (!validGuids || collision || choiceCount <= 0 ||
                mandatoryRoots.Any(m => !availableRoots.Contains(m)))
            {
                ClassEntries = 0; SpellbookEntries = 0; ChoiceEntries = 0;
                ForeignDataTouched = !SameSubset(foreignBefore, ForeignSubset(cache));
                PreviewLedgerPresent = false;
                return false;
            }
            if (afterForeignWrite != null) afterForeignWrite();
            // Write only private identities; repeated completion is a no-op.
            foreach (string g in PrivateGuids) cache[g] = OwnerToken;
            ClassEntries = 1;
            SpellbookEntries = 1;
            ChoiceEntries = choiceCount;
            ForeignDataTouched = !SameSubset(foreignBefore, ForeignSubset(cache));
            PreviewLedgerPresent = false;
            return true;
        }

        private static bool SameSubset(Dictionary<string, string> a,
            Dictionary<string, string> b)
            => a.Count == b.Count && a.All(kv => b.ContainsKey(kv.Key) &&
                                               b[kv.Key] == kv.Value);
    }
}
