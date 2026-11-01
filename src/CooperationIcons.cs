using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Original CotW artwork, embedded unchanged. A feat's associated ability
    // and buff share its icon; no blueprint or save identity is changed.
    internal static class CooperationIcons
    {
        internal static void Apply(PsychicAssets assets)
        {
            Apply(assets, "BondedMind", "BondedMindFeat");
            // Retain icons on the legacy toggle and buff for saved references.
            Apply(assets, "ShareSpells", "ShareSpellsFeat", "BondedShareToggle", "BondedShareEnabled");
            Apply(assets, "BrokenWingGambit", "BrokenWingGambitFeat", "BrokenWingToggle", "BrokenWingEnabled");
            Apply(assets, "WoundedPawGambit", "WoundedPawGambitFeat", "WoundedPawToggle", "WoundedPawEnabled");
            Apply(assets, "StunningAssault", "StunningAssaultFeat", "StunningAssaultAbility", "StunningAssaultArmed");
        }

        private static void Apply(PsychicAssets assets, string iconToken, params string[] blueprintTokens)
        {
            var icon = PsychicOriginalIcons.Load(iconToken);
            foreach (var token in blueprintTokens) Set(assets.Assets[token], "m_Icon", icon);
        }
    }
}
