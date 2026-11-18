using System;
using System.Security.Cryptography;
using System.Text;

namespace MediumClass.Utilities
{
    /// <summary>
    /// Blueprint ids for the per-legend variants of Astral Beacon and Trance of
    /// Many. Those variants are generated from the legends the medium actually
    /// registers, so they cannot have hand-written guid constants: the id is
    /// derived from the legend's own class guid and a purpose token, which
    /// makes it identical on every load (and therefore save-safe) while still
    /// letting a new legend join without touching this file.
    /// </summary>
    internal static class VariantId
    {
        internal static string Of(string purpose, string legend)
        {
            using (var md5 = MD5.Create())
            {
                var seed = Encoding.UTF8.GetBytes("CotW.Wrath.Medium.v1/" + purpose + "/" + legend);
                return new Guid(md5.ComputeHash(seed)).ToString("D");
            }
        }
    }
}
