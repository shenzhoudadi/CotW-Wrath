using System;
using System.Collections.Generic;
using System.Linq;

namespace CallOfTheWild.Wrath.Prototype
{
    // One initial publication ledger per native cache. Optional provider
    // reconciliation remains separate: it must still discover late providers.
    internal sealed class RegistrationRetry01444
    {
        private readonly HashSet<string> completed = new HashSet<string>();
        private readonly Dictionary<string, Func<bool>> pending = new Dictionary<string, Func<bool>>();
        private readonly List<string> order = new List<string>();
        internal bool InitialDispatchCompleted;
        internal int PendingCount => pending.Count;
        internal bool Succeeded(string name) => completed.Contains(name);
        internal void Complete(string name) { completed.Add(name); pending.Remove(name); }

        internal void Run(string name, Action action, Action<string, bool, Exception> report)
            => RunAvailable(name, () => { action(); return true; }, report);

        internal void RunAvailable(string name, Func<bool> action, Action<string, bool, Exception> report)
        {
            if (completed.Contains(name)) return;
            Exception error = null; bool available = false;
            try { available = action(); } catch (Exception failure) { error = failure; }
            // An absent optional provider is neither a success nor a new error.
            if (error == null && !available) return;
            if (error == null) {
                // Commit before reporting: a logger/menu failure cannot cause a
                // successful publication to be invoked a second time.
                Complete(name);
            } else {
                if (!pending.ContainsKey(name)) order.Add(name);
                pending[name] = action;
            }
            report(name, error == null, error);
        }

        internal void Retry(Action<string, bool, Exception> report)
        {
            // Preserve producer-before-consumer order from the initial pass.
            // Snapshot only pending actions; successes are never traversed again.
            foreach (string name in order.ToArray())
                if (pending.TryGetValue(name, out var action)) RunAvailable(name, action, report);
        }
    }
}
