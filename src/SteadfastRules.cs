using System;

namespace CallOfTheWild.Wrath.Prototype
{
    // CotW's rule: replace only a positive Wisdom bonus, retaining Wisdom penalties.
    // Negative Charisma is intentional, not clamped to zero.
    public static class SteadfastRules
    {
        public static int Adjustment(int wisdomBonus, int charismaBonus)
            => charismaBonus - Math.Max(wisdomBonus, 0);

        public static bool Applies(bool hasContext, bool isMindAffecting,
            bool isWillSave, bool usesWisdom)
            => hasContext && isMindAffecting && isWillSave && usesWisdom;
    }
}
