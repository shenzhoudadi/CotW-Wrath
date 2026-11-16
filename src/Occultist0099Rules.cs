// COTW-0099 Occultist, B stage, first vertical slice: Abjuration.
// Frozen contract C-0099-20261115-r1, sections O-S01 and O-S02.
//
// This file is the authoritative state model. It deliberately has NO Unity,
// game-assembly or Harmony dependency: the native Wrath entry points in
// Occultist0099Native.cs must read and write this same state, and the isolated
// rules host links this file directly so the numbers in C-occultist-cases.json
// can be checked without launching the game or UMM.
//
// Ownership boundary: one Occultist0099FocusState per character entity. The
// class, its spellbook and its focus powers are the only identities allowed to
// use it. Foreign books, foreign resources, items and other units never enter.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace CallOfTheWild.Wrath.Prototype
{
    public enum Occultist0099Outcome
    {
        Ok,
        Noop,
        RejectedNotOwned,
        RejectedUnknownSchool,
        RejectedNotLocked,
        RejectedAlreadyLocked,
        RejectedInsufficient,
        RejectedIntTooLow,
        RejectedFull,
        RejectedDuplicate,
        RejectedInvalidArgument,
        RejectedInvalidState,
        RejectedClassRemoved,
        RejectedTargetInvalid
    }

    public static class Occultist0099Schools
    {
        // Order is the A JSON member order. Panoply tie-breaking uses it.
        public static readonly string[] Order =
        {
            "Abjuration", "Conjuration", "Divination", "Enchantment",
            "Evocation", "Illusion", "Necromancy", "Transmutation"
        };

        public static readonly string[] PanoplyOrder =
        {
            "Trappings of the Warrior", "Mage's Paraphernalia",
            "Saint's Holy Regalia", "Performer's Accoutrements"
        };

        public static readonly string[][] PanoplyMembers =
        {
            new[] { "Abjuration", "Transmutation" },
            new[] { "Divination", "Evocation", "Necromancy" },
            new[] { "Abjuration", "Conjuration" },
            new[] { "Enchantment", "Illusion" }
        };

        public static bool IsSchool(string school) => Array.IndexOf(Order, school) >= 0;

        public static string[] MembersOf(string panoply)
        {
            int i = Array.IndexOf(PanoplyOrder, panoply);
            return i < 0 ? null : PanoplyMembers[i];
        }

        public static string[] PanopliesUsing(string school)
            => PanoplyOrder.Where((p, i) => PanoplyMembers[i].Contains(school)).ToArray();
    }

    [DataContract]
    public sealed class Occultist0099SchoolState
    {
        [DataMember] public bool Present;            // the implement (school fact) is owned
        [DataMember] public int Invested;            // A, ordinary investment for this cycle
        [DataMember] public int Remaining;           // R, unspent ordinary points
        [DataMember] public int LockedInvestment;    // A frozen at Full Round Lock
        [DataMember] public int LockedLevel;
        [DataMember] public int Resonance;           // computed from the locked snapshot
        [DataMember] public int ResonanceTier;
        [DataMember] public bool ResonanceValid;

        public Occultist0099SchoolState Clone()
        {
            return new Occultist0099SchoolState
            {
                Present = Present, Invested = Invested, Remaining = Remaining,
                LockedInvestment = LockedInvestment, LockedLevel = LockedLevel,
                Resonance = Resonance, ResonanceTier = ResonanceTier,
                ResonanceValid = ResonanceValid
            };
        }
    }

    // One rest cycle per character. G + sum(R) + U + Q = B is invariant.
    // Mastery points (T/H) are a separate 4-point pool outside B.
    [DataContract]
    public sealed class Occultist0099FocusState
    {
        [DataMember] public bool ClassPresent;
        [DataMember] public bool Enabled;                 // false closes payment and resonance
        [DataMember] public int Level;                    // actual Occultist level L
        [DataMember] public int IntModifier;              // effective INT modifier M at cycle start
        // Effective Intelligence score at cycle start (score = 10 + 2*M for
        // the model fixtures; the native adapter passes the real stat). The
        // O-S02 learning/casting gate Int >= 10 + circle reads this.
        [DataMember] public int Intelligence;
        [DataMember] public int Budget;                   // B = max(0, L + M)
        [DataMember] public int? RespecBudgetCap;
        [DataMember] public int Generic;                  // G
        [DataMember] public int Spent;                    // U, ordinary points actually paid
        [DataMember] public int Invalidated;              // Q, ordinary points retired by respec
        [DataMember] public int MasterySpent;             // T
        [DataMember] public int MasteryRemaining;         // H, T + H <= 4
        [DataMember] public string MasterySchool;
        [DataMember] public string PhysicalAttribute;     // Physical Enhancement, chosen before lock
        [DataMember] public bool Locked;
        [DataMember] public string LastRestEventId;
        [DataMember] public Dictionary<string, Occultist0099SchoolState> Schools =
            new Dictionary<string, Occultist0099SchoolState>();
        [DataMember] public Dictionary<string, int> PanoplyResonance = new Dictionary<string, int>();
        [DataMember] public Dictionary<string, bool> PanoplyValid = new Dictionary<string, bool>();
        [DataMember] public HashSet<string> ChargedExecutions = new HashSet<string>();
        [DataMember] public HashSet<string> ConsumedCorpses = new HashSet<string>();

        public Occultist0099FocusState()
        {
            foreach (string school in Occultist0099Schools.Order)
                Schools[school] = new Occultist0099SchoolState();
            foreach (string panoply in Occultist0099Schools.PanoplyOrder)
            {
                PanoplyResonance[panoply] = 0;
                PanoplyValid[panoply] = false;
            }
            MasteryRemaining = 4;
        }

        public int Available => Generic + Schools.Values.Sum(s => s.Remaining);

        public string Fingerprint()
        {
            // Read-only availability checks must not change a single field.
            // Tests compare this string before and after those calls.
            var parts = new List<string>
            {
                ClassPresent ? "1" : "0", Enabled ? "1" : "0", Level.ToString(),
                IntModifier.ToString(), Intelligence.ToString(),
                Budget.ToString(), Generic.ToString(),
                RespecBudgetCap?.ToString() ?? "-",
                Spent.ToString(), Invalidated.ToString(), MasterySpent.ToString(),
                MasteryRemaining.ToString(), MasterySchool ?? "-",
                PhysicalAttribute ?? "-", Locked ? "1" : "0",
                LastRestEventId ?? "-"
            };
            foreach (string school in Occultist0099Schools.Order)
            {
                var s = Schools[school];
                parts.Add(school + ":" + (s.Present ? 1 : 0) + ":" + s.Invested + ":" +
                          s.Remaining + ":" + s.LockedInvestment + ":" + s.LockedLevel + ":" + s.Resonance + ":" + s.ResonanceTier +
                          ":" + (s.ResonanceValid ? 1 : 0));
            }
            foreach (string panoply in Occultist0099Schools.PanoplyOrder)
                parts.Add(panoply + ":" + PanoplyResonance[panoply] + ":" +
                          (PanoplyValid[panoply] ? 1 : 0));
            parts.Add("charged=" + string.Join(",", ChargedExecutions.OrderBy(x => x)));
            parts.Add("corpses=" + string.Join(",", ConsumedCorpses.OrderBy(x => x)));
            return string.Join("|", parts);
        }

        public Occultist0099FocusState Clone()
        {
            var copy = new Occultist0099FocusState
            {
                ClassPresent = ClassPresent, Enabled = Enabled, Level = Level,
                IntModifier = IntModifier, Intelligence = Intelligence,
                Budget = Budget, RespecBudgetCap = RespecBudgetCap, Generic = Generic,
                Spent = Spent, Invalidated = Invalidated, MasterySpent = MasterySpent,
                MasteryRemaining = MasteryRemaining, MasterySchool = MasterySchool,
                PhysicalAttribute = PhysicalAttribute, Locked = Locked,
                LastRestEventId = LastRestEventId
            };
            foreach (var kv in Schools) copy.Schools[kv.Key] = kv.Value.Clone();
            foreach (var kv in PanoplyResonance) copy.PanoplyResonance[kv.Key] = kv.Value;
            foreach (var kv in PanoplyValid) copy.PanoplyValid[kv.Key] = kv.Value;
            foreach (string id in ChargedExecutions) copy.ChargedExecutions.Add(id);
            foreach (string id in ConsumedCorpses) copy.ConsumedCorpses.Add(id);
            return copy;
        }
    }

    public sealed class Occultist0099Payment
    {
        public Occultist0099Outcome Outcome;
        public int FromMastery;
        public int FromOrdinary;
        public Dictionary<string, int> FromMembers = new Dictionary<string, int>();
        public bool Applied => Outcome == Occultist0099Outcome.Ok;
    }

    public sealed class Occultist0099FocusPower
    {
        public string School;
        public int EffectiveLevel;
        public int SaveDC;
        public int Absorption;
        public int Resonance;
    }

    public static class Occultist0099Focus
    {
        public static Occultist0099FocusState Create() => new Occultist0099FocusState();

        public static bool IsConsistent(Occultist0099FocusState s)
        {
            if (s == null || s.Budget < 0 || s.Generic < 0 || s.Spent < 0 ||
                s.Invalidated < 0 || s.MasterySpent < 0 || s.MasteryRemaining < 0 ||
                s.MasterySpent + s.MasteryRemaining > 4 || s.RespecBudgetCap < 0 || s.RespecBudgetCap > s.Budget)
                return false;
            if (s.Available + s.Spent + s.Invalidated != s.Budget) return false;
            if (s.RespecBudgetCap.HasValue && s.Available > Math.Max(0, s.RespecBudgetCap.Value - s.Spent)) return false;
            foreach (string school in Occultist0099Schools.Order)
            {
                var st = s.Schools[school];
                if (st.Invested < 0 || st.Remaining < 0 || st.Remaining > st.Invested ||
                    st.LockedInvestment < 0 || st.LockedLevel < 0 || st.ResonanceTier < 0 || st.ResonanceTier > 3) return false;
            }
            if (s.MasterySchool != null && !Occultist0099Schools.IsSchool(s.MasterySchool))
                return false;
            return true;
        }

        private static Occultist0099Outcome Guard(Occultist0099FocusState s)
        {
            if (s == null) return Occultist0099Outcome.RejectedInvalidState;
            if (!IsConsistent(s))
            {
                s.Enabled = false;
                return Occultist0099Outcome.RejectedInvalidState;
            }
            if (!s.ClassPresent) return Occultist0099Outcome.RejectedClassRemoved;
            if (!s.Enabled) return Occultist0099Outcome.RejectedInvalidState;
            return Occultist0099Outcome.Ok;
        }

        // O-S01: a successful rest (or the first real acquisition of the class)
        // creates the cycle. Same rest event id is idempotent; temporary INT
        // changes, death, reload and fact reattachment never call this.
        public static Occultist0099Outcome BeginCycle(Occultist0099FocusState s, int level,
            int intModifier, string restEventId, bool firstAcquisition)
        {
            if (s == null || level < 0 || string.IsNullOrEmpty(restEventId))
                return Occultist0099Outcome.RejectedInvalidArgument;
            if (s.LastRestEventId == restEventId)
            {
                if (!s.ClassPresent) s.ClassPresent = true; // MF09: no free refill
                return Occultist0099Outcome.Noop;
            }
            // A class removed and regained on the same day must not re-arm.
            if (firstAcquisition && s.LastRestEventId != null)
            {
                s.ClassPresent = true;
                return Occultist0099Outcome.Noop;
            }
            int budget = Math.Max(0, level + intModifier);
            var present = Occultist0099Schools.Order
                .Where(x => s.Schools[x].Present).ToArray();
            s.ClassPresent = true;
            s.Enabled = true;
            s.Level = level;
            s.IntModifier = intModifier;
            s.Intelligence = 10 + 2 * intModifier;   // model convention
            s.Budget = budget;
            s.RespecBudgetCap = null;
            s.Generic = budget;
            s.Spent = 0;
            s.Invalidated = 0;
            s.MasterySpent = 0;
            s.MasteryRemaining = 4;
            s.MasterySchool = null;
            s.PhysicalAttribute = null;
            s.Locked = false;
            s.LastRestEventId = restEventId;
            foreach (string school in Occultist0099Schools.Order)
            {
                var st = s.Schools[school];
                st.Present = present.Contains(school);
                st.Invested = 0;
                st.Remaining = 0;
                st.LockedInvestment = 0;
                st.LockedLevel = 0;
                st.Resonance = 0;
                st.ResonanceTier = 0;
                st.ResonanceValid = false;
            }
            foreach (string panoply in Occultist0099Schools.PanoplyOrder)
            {
                s.PanoplyResonance[panoply] = 0;
                s.PanoplyValid[panoply] = false;
            }
            s.ChargedExecutions.Clear();
            s.ConsumedCorpses.Clear();
            return Occultist0099Outcome.Ok;
        }

        // Implement facts are private grants. Panoply facts never grant schools.
        public static Occultist0099Outcome SetSchoolPresent(Occultist0099FocusState s,
            string school, bool present)
        {
            var g = Guard(s);
            if (g != Occultist0099Outcome.Ok) return g;
            if (!Occultist0099Schools.IsSchool(school))
                return Occultist0099Outcome.RejectedUnknownSchool;
            s.Schools[school].Present = present;
            if (!present) InvalidateSchool(s, school);
            return Occultist0099Outcome.Ok;
        }

        public static Occultist0099Outcome Invest(Occultist0099FocusState s, string school,
            int points)
        {
            var g = Guard(s);
            if (g != Occultist0099Outcome.Ok) return g;
            if (s.Locked) return Occultist0099Outcome.RejectedAlreadyLocked;
            if (!Occultist0099Schools.IsSchool(school))
                return Occultist0099Outcome.RejectedUnknownSchool;
            if (points <= 0 || points > s.Generic)
                return Occultist0099Outcome.RejectedInsufficient;
            var st = s.Schools[school];
            if (!st.Present) return Occultist0099Outcome.RejectedNotOwned;
            // Free invest: G-1, A+1, R+1 per point. Repeated implements of the
            // same school share one A/R record.
            s.Generic -= points;
            st.Invested += points;
            st.Remaining += points;
            return Occultist0099Outcome.Ok;
        }

        // Free Reset returns each school's unspent R only. U, Q, mastery and
        // the same-day general expenditure all stay.
        public static Occultist0099Outcome Reset(Occultist0099FocusState s)
        {
            var g = Guard(s);
            if (g != Occultist0099Outcome.Ok) return g;
            if (s.Locked) return Occultist0099Outcome.RejectedAlreadyLocked;
            bool changed = false;
            foreach (string school in Occultist0099Schools.Order)
            {
                var st = s.Schools[school];
                if (st.Remaining == 0 && st.Invested == 0) continue;
                s.Generic += st.Remaining;
                st.Invested = 0;
                st.Remaining = 0;
                changed = true;
            }
            return changed ? Occultist0099Outcome.Ok : Occultist0099Outcome.Noop;
        }

        public static Occultist0099Outcome ChoosePhysicalAttribute(
            Occultist0099FocusState s, string attribute)
        {
            var g = Guard(s);
            if (g != Occultist0099Outcome.Ok) return g;
            if (s.Locked) return Occultist0099Outcome.RejectedAlreadyLocked;
            if (attribute != "Strength" && attribute != "Dexterity" &&
                attribute != "Constitution")
                return Occultist0099Outcome.RejectedInvalidArgument;
            if (!s.Schools["Transmutation"].Present) return Occultist0099Outcome.RejectedNotOwned;
            s.PhysicalAttribute = attribute;
            return Occultist0099Outcome.Ok;
        }

        public static Occultist0099Outcome Lock(Occultist0099FocusState s, int? classLevel = null)
        {
            var g = Guard(s);
            if (g != Occultist0099Outcome.Ok) return g;
            if (s.Locked) return Occultist0099Outcome.RejectedAlreadyLocked;
            foreach (string school in Occultist0099Schools.Order)
            {
                var st = s.Schools[school];
                st.LockedInvestment = st.Present ? st.Invested : 0;
                st.LockedLevel = st.Present ? classLevel ?? s.Level : 0;
                st.Resonance = WardingTalismanResonance(classLevel ?? s.Level, st.LockedInvestment);
                st.ResonanceValid = st.Present && st.LockedInvestment > 0;
            }
            CaptureSchoolResonance(s);
            foreach (string panoply in Occultist0099Schools.PanoplyOrder)
            {
                string[] members = Occultist0099Schools.MembersOf(panoply);
                bool valid = members.All(m => s.Schools[m].LockedInvestment >= 1);
                s.PanoplyValid[panoply] = valid;
                s.PanoplyResonance[panoply] = valid
                    ? members.Sum(m => s.Schools[m].LockedInvestment) : 0;
            }
            s.Locked = true;
            return Occultist0099Outcome.Ok;
        }

        // Warding Talisman: every 2 invested points gives +1 resistance to all
        // saves; the original clips the investment to 2 + L/2 first. Resonance
        // is a locked snapshot: spending the school to R=0 never removes it.
        public static int WardingTalismanResonance(int level, int invested)
        {
            int cap = 2 + Math.Max(0, level) / 2;
            return Math.Max(0, Math.Min(Math.Max(0, invested), cap) / 2);
        }

        public static int Resonance(Occultist0099FocusState s, string school)
        {
            if (s == null || !s.Enabled || !s.Locked ||
                !Occultist0099Schools.IsSchool(school)) return 0;
            var st = s.Schools[school];
            return st.ResonanceValid ? st.Resonance : 0;
        }

        internal static int ResonanceTier(Occultist0099FocusState s, string school)
            => Resonance(s, school) > 0 ? s.Schools[school].ResonanceTier : 0;

        private static void CaptureSchoolResonance(Occultist0099FocusState state)
        {
            CaptureThirdEye(state);
            CapturePhysicalEnhancement(state);
            const string school = "Illusion";
            var source = state.Schools[school];
            if (!source.Present || source.LockedLevel <= 0) return;
            int mastery = state.MasterySchool == school ? 4 : 0;
            int invested = source.LockedInvestment + mastery;
            source.ResonanceValid = invested > 0;
            source.Resonance = Math.Min(source.LockedLevel + mastery, invested);
        }

        private static void CapturePhysicalEnhancement(Occultist0099FocusState state)
        {
            const string school = "Transmutation";
            var source = state.Schools[school];
            if (!source.Present || source.LockedLevel <= 0) return;
            int mastery = state.MasterySchool == school ? 4 : 0;
            int invested = source.LockedInvestment + mastery;
            source.ResonanceValid = state.PhysicalAttribute != null && invested >= 3;
            source.Resonance = source.ResonanceValid ? 2 * (Math.Min(3 + (source.LockedLevel + mastery) / 2, invested) / 3) : 0;
        }

        private static void CaptureThirdEye(Occultist0099FocusState state)
        {
            const string school = "Divination";
            var source = state.Schools[school];
            if (!source.Present || source.LockedLevel <= 0) return;
            int mastery = state.MasterySchool == school ? 4 : 0;
            int level = source.LockedLevel + mastery;
            int invested = source.LockedInvestment + mastery;
            source.ResonanceValid = invested > 0;
            source.Resonance = Math.Min(level, invested / 2);
            source.ResonanceTier = level >= 19 && invested >= 15 ? 3 :
                level >= 13 && invested >= 12 ? 2 : level >= 7 && invested >= 9 ? 1 : 0;
        }

        public static int PanoplyResonance(Occultist0099FocusState s, string panoply)
        {
            if (s == null || !s.Enabled || !s.Locked ||
                Occultist0099Schools.MembersOf(panoply) == null) return 0;
            return s.PanoplyValid[panoply] ? s.PanoplyResonance[panoply] : 0;
        }

        // Read-only availability. Deliberately side-effect free.
        public static bool CanPaySchool(Occultist0099FocusState s, string school, int cost,
            string executionId)
        {
            if (s == null || !s.ClassPresent || !s.Enabled || !s.Locked ||
                !Occultist0099Schools.IsSchool(school) || cost <= 0 ||
                (executionId != null && s.ChargedExecutions.Contains(executionId)))
                return false;
            var st = s.Schools[school];
            if (!st.Present) return false;
            int fromMastery = school == s.MasterySchool
                ? Math.Min(s.MasteryRemaining, cost) : 0;
            return st.Remaining >= cost - fromMastery;
        }

        public static Occultist0099Payment PaySchool(Occultist0099FocusState s,
            string school, int cost, string executionId)
        {
            var charge = new Occultist0099Payment();
            var g = Guard(s);
            if (g != Occultist0099Outcome.Ok) { charge.Outcome = g; return charge; }
            if (!s.Locked) { charge.Outcome = Occultist0099Outcome.RejectedNotLocked; return charge; }
            if (!Occultist0099Schools.IsSchool(school))
            { charge.Outcome = Occultist0099Outcome.RejectedUnknownSchool; return charge; }
            if (cost <= 0 || string.IsNullOrEmpty(executionId))
            { charge.Outcome = Occultist0099Outcome.RejectedInvalidArgument; return charge; }
            if (s.ChargedExecutions.Contains(executionId))
            { charge.Outcome = Occultist0099Outcome.RejectedDuplicate; return charge; }
            var st = s.Schools[school];
            if (!st.Present) { charge.Outcome = Occultist0099Outcome.RejectedNotOwned; return charge; }

            // Mastery H is a private school-only allowance; ordinary R pays the
            // rest. The whole transaction is rejected if the sum is short.
            int fromMastery = school == s.MasterySchool
                ? Math.Min(s.MasteryRemaining, cost) : 0;
            int fromOrdinary = cost - fromMastery;
            if (st.Remaining < fromOrdinary)
            { charge.Outcome = Occultist0099Outcome.RejectedInsufficient; return charge; }

            s.MasteryRemaining -= fromMastery;
            s.MasterySpent += fromMastery;
            st.Remaining -= fromOrdinary;
            s.Spent += fromOrdinary;          // U counts ordinary points only
            s.ChargedExecutions.Add(executionId);
            charge.Outcome = Occultist0099Outcome.Ok;
            charge.FromMastery = fromMastery;
            charge.FromOrdinary = fromOrdinary;
            return charge;
        }

        // Panoply payment is a pure projection over member pools: each point
        // takes the member with the highest current R, ties by A JSON order.
        // The full vector is computed before any balance is touched, and no
        // second debit (the Kingmaker ConnectResource path) is allowed.
        // D-0099-01: read-only Panoply availability. It runs the SAME highest-R
        // projection as PayPanoply over a working copy, so the UI precheck and
        // the atomic payment can never disagree, and it mutates nothing.
        public static bool CanPayPanoply(Occultist0099FocusState s, string panoply,
            int cost, string executionId)
        {
            if (s == null || !s.ClassPresent || !s.Enabled || !s.Locked ||
                cost <= 0 || (executionId != null && s.ChargedExecutions.Contains(executionId)))
                return false;
            string[] members = Occultist0099Schools.MembersOf(panoply);
            if (members == null || !s.PanoplyValid[panoply]) return false;
            var working = members.ToDictionary(m => m, m => s.Schools[m].Remaining);
            for (int i = 0; i < cost; i++)
            {
                string chosen = null;
                foreach (string m in members)
                    if (chosen == null || working[m] > working[chosen]) chosen = m;
                if (working[chosen] <= 0) return false;
                working[chosen]--;
            }
            return true;
        }

        // D-0099-01: explicit general expenditure from G (the Repower Construct
        // class of powers). Unlike a school payment it is allowed before lock,
        // and it shares the cycle-scoped execution dedup identity.
        public static bool CanPayGeneric(Occultist0099FocusState s, int cost,
            string executionId)
        {
            if (s == null || !s.ClassPresent || !s.Enabled || cost <= 0 ||
                (executionId != null && s.ChargedExecutions.Contains(executionId)))
                return false;
            return s.Generic >= cost;
        }

        public static Occultist0099Payment PayGeneric(Occultist0099FocusState s,
            int cost, string executionId)
        {
            var charge = new Occultist0099Payment();
            var g = Guard(s);
            if (g != Occultist0099Outcome.Ok) { charge.Outcome = g; return charge; }
            if (cost <= 0 || string.IsNullOrEmpty(executionId))
            { charge.Outcome = Occultist0099Outcome.RejectedInvalidArgument; return charge; }
            if (s.ChargedExecutions.Contains(executionId))
            { charge.Outcome = Occultist0099Outcome.RejectedDuplicate; return charge; }
            if (s.Generic < cost)
            { charge.Outcome = Occultist0099Outcome.RejectedInsufficient; return charge; }
            s.Generic -= cost;
            s.Spent += cost;
            s.ChargedExecutions.Add(executionId);
            charge.Outcome = Occultist0099Outcome.Ok;
            charge.FromOrdinary = cost;
            return charge;
        }

        public static Occultist0099Payment PayPanoply(Occultist0099FocusState s,
            string panoply, int cost, string executionId)
        {
            var charge = new Occultist0099Payment();
            var g = Guard(s);
            if (g != Occultist0099Outcome.Ok) { charge.Outcome = g; return charge; }
            string[] members = Occultist0099Schools.MembersOf(panoply);
            if (members == null)
            { charge.Outcome = Occultist0099Outcome.RejectedUnknownSchool; return charge; }
            if (!s.Locked || !s.PanoplyValid[panoply])
            { charge.Outcome = Occultist0099Outcome.RejectedNotLocked; return charge; }
            if (cost <= 0 || string.IsNullOrEmpty(executionId))
            { charge.Outcome = Occultist0099Outcome.RejectedInvalidArgument; return charge; }
            if (s.ChargedExecutions.Contains(executionId))
            { charge.Outcome = Occultist0099Outcome.RejectedDuplicate; return charge; }

            var planned = new Dictionary<string, int>();
            foreach (string m in members) planned[m] = 0;
            var working = members.ToDictionary(m => m, m => s.Schools[m].Remaining);
            for (int i = 0; i < cost; i++)
            {
                string chosen = null;
                foreach (string m in members)
                    if (chosen == null || working[m] > working[chosen]) chosen = m;
                if (working[chosen] <= 0)
                { charge.Outcome = Occultist0099Outcome.RejectedInsufficient; return charge; }
                working[chosen]--;
                planned[chosen]++;
            }
            foreach (string m in members)
            {
                s.Schools[m].Remaining = working[m];
                charge.FromMembers[m] = planned[m];
            }
            s.Spent += cost;
            s.ChargedExecutions.Add(executionId);
            charge.Outcome = Occultist0099Outcome.Ok;
            charge.FromOrdinary = cost;
            return charge;
        }

        public static Occultist0099Outcome RepowerConstruct(Occultist0099FocusState s,
            string corpseId)
        {
            var g = Guard(s);
            if (g != Occultist0099Outcome.Ok) return g;
            if (string.IsNullOrEmpty(corpseId))
                return Occultist0099Outcome.RejectedTargetInvalid;
            if (s.ConsumedCorpses.Contains(corpseId))
                return Occultist0099Outcome.RejectedTargetInvalid;
            if (s.Generic < 1) return Occultist0099Outcome.RejectedInsufficient;
            // Explicit general expenditure; allowed before lock, 1 point flat.
            s.Generic -= 1;
            s.Spent += 1;
            s.ConsumedCorpses.Add(corpseId);
            return Occultist0099Outcome.Ok;
        }

        // Locked Replenish: 1:1 replacement of spent school points from G.
        // Kingmaker's executable ContextActionInvestFocus semantics, NOT the
        // tabletop 2-for-1 rule. Distinct from pre-lock Invest.
        public static Occultist0099Outcome Replenish(Occultist0099FocusState s, string school)
        {
            var g = Guard(s);
            if (g != Occultist0099Outcome.Ok) return g;
            if (!s.Locked) return Occultist0099Outcome.RejectedNotLocked;
            if (!Occultist0099Schools.IsSchool(school))
                return Occultist0099Outcome.RejectedUnknownSchool;
            var st = s.Schools[school];
            if (!st.Present) return Occultist0099Outcome.RejectedNotOwned;
            if (st.Invested <= 0 || st.Remaining >= st.Invested)
                return Occultist0099Outcome.RejectedFull;
            if (s.Generic < 1) return Occultist0099Outcome.RejectedInsufficient;
            s.Generic -= 1;
            st.Remaining += 1;
            return Occultist0099Outcome.Ok;
        }

        public static Occultist0099FocusPower MindBarrier(Occultist0099FocusState s,
            string school, int? classLevel = null, int? intModifier = null)
        {
            bool mastery = s != null && s.MasterySchool == school;
            int level = classLevel ?? s?.Level ?? 0;
            int effective = level + (mastery ? 4 : 0);
            int dc = 10 + level / 2 + (intModifier ?? s?.IntModifier ?? 0) + (mastery ? 2 : 0);
            return new Occultist0099FocusPower
            {
                School = school, EffectiveLevel = effective, SaveDC = dc,
                Absorption = 2 * effective,
                Resonance = Resonance(s, school)
            };
        }

        // Implement Mastery: 4 points, own-school powers use L+4, own-school DC
        // uses +2. Class level, book CL, slots, UMD and Panoply still use L.
        public static Occultist0099Outcome SetMastery(Occultist0099FocusState s, string school)
        {
            var g = Guard(s);
            if (g != Occultist0099Outcome.Ok) return g;
            if (!Occultist0099Schools.IsSchool(school))
                return Occultist0099Outcome.RejectedUnknownSchool;
            if (!s.Schools[school].Present)
                return Occultist0099Outcome.RejectedNotOwned;
            s.MasterySchool = school;
            if (s.Locked) CaptureSchoolResonance(s);
            return Occultist0099Outcome.Ok;
        }

        public static Occultist0099Outcome RespecMastery(Occultist0099FocusState s,
            string newSchool)
        {
            var g = Guard(s);
            if (g != Occultist0099Outcome.Ok) return g;
            if (newSchool != null)
            {
                if (!Occultist0099Schools.IsSchool(newSchool)) return Occultist0099Outcome.RejectedUnknownSchool;
                if (!s.Schools[newSchool].Present) return Occultist0099Outcome.RejectedNotOwned;
            }
            // T is carried into the new school; H never exceeds 4-T. Old
            // mastery-only rank/DC contributions are not virtual level patches,
            // so dropping the identity removes them.
            s.MasterySchool = newSchool;
            s.MasteryRemaining = Math.Min(s.MasteryRemaining, 4 - s.MasterySpent);
            if (s.Locked) CaptureSchoolResonance(s);
            return Occultist0099Outcome.Ok;
        }

        private static void InvalidateSchool(Occultist0099FocusState s, string school)
        {
            var st = s.Schools[school];
            st.LockedInvestment = 0;
            st.LockedLevel = 0;
            st.Resonance = 0;
            st.ResonanceTier = 0;
            st.ResonanceValid = false;
            foreach (string panoply in Occultist0099Schools.PanopliesUsing(school))
            {
                s.PanoplyValid[panoply] = false;
                s.PanoplyResonance[panoply] = 0;
            }
        }

        // Respec source removal. Unlocked school returns its R to G; locked
        // school retires R into Q and invalidates its resonance and Panoplies.
        public static Occultist0099Outcome RespecRemoveSchool(Occultist0099FocusState s,
            string school)
        {
            var g = Guard(s);
            if (g != Occultist0099Outcome.Ok) return g;
            if (!Occultist0099Schools.IsSchool(school))
                return Occultist0099Outcome.RejectedUnknownSchool;
            var st = s.Schools[school];
            if (!st.Present) return Occultist0099Outcome.RejectedNotOwned;
            int returned = st.Remaining;
            st.Present = false;
            if (s.Locked) s.Invalidated += returned;
            else s.Generic += returned;
            st.Invested = 0;
            st.Remaining = 0;
            InvalidateSchool(s, school);
            if (s.MasterySchool == school) s.MasterySchool = null;
            if (school == "Transmutation") s.PhysicalAttribute = null;
            return Occultist0099Outcome.Ok;
        }

        // Budget downgrade on respec commit: B stays the cycle cap; the usable
        // balance is clamped to max(0, newCap - U - Q), draining G first and
        // then each school's R in stable order into Q.
        public static Occultist0099Outcome RespecBudgetDown(Occultist0099FocusState s,
            int newLevel, int newIntModifier)
        {
            var g = Guard(s);
            if (g != Occultist0099Outcome.Ok) return g;
            int newCap = Math.Max(0, newLevel + newIntModifier);
            if (newCap >= (s.RespecBudgetCap ?? s.Budget)) return Occultist0099Outcome.Noop;
            s.RespecBudgetCap = newCap;
            int targetAvailable = Math.Max(0, newCap - s.Spent - s.Invalidated);
            int excess = s.Available - targetAvailable;
            if (excess <= 0) return Occultist0099Outcome.Ok;
            int fromG = Math.Min(s.Generic, excess);
            s.Generic -= fromG;
            int left = excess - fromG;
            foreach (string school in Occultist0099Schools.Order)
            {
                if (left == 0) break;
                var st = s.Schools[school];
                int taken = Math.Min(st.Remaining, left);
                st.Remaining -= taken;
                left -= taken;
            }
            s.Invalidated += excess - left;
            return Occultist0099Outcome.Ok;
        }

        public static Occultist0099Outcome RemoveClass(Occultist0099FocusState s)
        {
            if (s == null) return Occultist0099Outcome.RejectedInvalidState;
            s.ClassPresent = false;
            s.Enabled = false;                    // payments and resonance closed
            // D-0099-02 fix (C:22-23, MF11): cycle-scoped dedup identities
            // (charged executions, consumed corpses) are part of the minimal
            // cycle record and MUST survive class removal. Clearing them here
            // let the same execution id pay twice after same-day reacquire.
            // Only BeginCycle (a genuinely new rest cycle) clears them.
            return Occultist0099Outcome.Ok;
        }

        public static void RetireClass(Occultist0099FocusState s)
        {
            if (IsConsistent(s))
            {
                ReacquireClass(s);
                foreach (string school in Occultist0099Schools.Order.Where(school => s.Schools[school].Present))
                    RespecRemoveSchool(s, school);
                RespecBudgetDown(s, 0, 0);
                s.MasterySchool = null;
            }
            RemoveClass(s);
        }

        public static Occultist0099Outcome ReacquireClass(Occultist0099FocusState s)
        {
            if (s == null) return Occultist0099Outcome.RejectedInvalidState;
            // No new daily budget and no free snapshot on the same day.
            s.ClassPresent = true;
            s.Enabled = IsConsistent(s);
            return Occultist0099Outcome.Ok;
        }

        public static Occultist0099Outcome Validate(Occultist0099FocusState s)
        {
            if (s == null) return Occultist0099Outcome.RejectedInvalidState;
            bool ok = IsConsistent(s);
            s.Enabled = ok && s.ClassPresent;
            return ok ? Occultist0099Outcome.Ok :
                Occultist0099Outcome.RejectedInvalidState;
        }
    }

    // Per-character ownership boundary. A foreign unit or a foreign resource
    // simply has no entry here; no call can move its numbers.
    public sealed class Occultist0099Ledger
    {
        private readonly Dictionary<string, Occultist0099FocusState> units =
            new Dictionary<string, Occultist0099FocusState>();

        public Occultist0099FocusState For(string unitId)
        {
            if (string.IsNullOrEmpty(unitId)) return null;
            Occultist0099FocusState state;
            if (!units.TryGetValue(unitId, out state))
            {
                state = Occultist0099Focus.Create();
                units[unitId] = state;
            }
            return state;
        }

        public bool Has(string unitId) => units.ContainsKey(unitId);
        public int Count => units.Count;
    }
}
