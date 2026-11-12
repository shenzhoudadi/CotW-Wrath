using System.Collections.Generic;
using System.Linq;

namespace CallOfTheWild.Wrath.Prototype
{
    internal sealed class ModuleDefinition
    {
        internal readonly string Name, Detail, InitialState;
        internal ModuleDefinition(string name, string detail, string initialState)
        { Name = name; Detail = detail; InitialState = initialState; }
    }
    internal sealed class ModuleStatus
    {
        internal readonly ModuleDefinition Definition;
        internal readonly string State;
        internal ModuleStatus(ModuleDefinition definition, string state)
        { Definition = definition; State = state; }
    }
    internal static class ModuleCatalog
    {
        // Registration states and menu rows share these definitions. The
        // explicitly retired Atavism module is removed rather than displayed.
        // Registration states and the menu both originate from this catalogue.
        internal static readonly IReadOnlyList<ModuleDefinition> Definitions = new[] {
            new ModuleDefinition("Steadfast Personality", "Replaces positive Wisdom with Charisma on qualifying mind-affecting Will saves. Registration does not mean the current character has selected the feat.", "Not registered"),
            new ModuleDefinition("Extended Bane", "Adds positive Wisdom to the Inquisitor bane resource maximum. Rest to check the refreshed capacity; registration does not grant the feat.", "Not registered"),
            new ModuleDefinition("Improved Combat Expertise", "Reduces Combat Expertise penalties by 2. Requires Intelligence 13, base attack bonus +8 and Combat Expertise. Registration does not grant the feat.", "Not registered"),
            new ModuleDefinition("Brawler combat feat integration", "Adds this mod's available combat feats to RealityMachina Brawler's bonus feat list. Rechecks when settings opens, including providers initialized later. This package does not include the Brawler class.", "Not detected"),
            new ModuleDefinition(ExemplarInspireCourage.ModuleName, "Uses Exemplar levels minus 2 for Inspire Courage while retaining native contributions from other eligible classes. Registered means the expected rank structure was found; it does not verify every song or archetype.", "Not detected"),
            new ModuleDefinition(ShieldedMage.ModuleName, "Reduces shield arcane spell failure by 15 percentage points, to a minimum of zero. Does not reduce body armor spell failure. Requires Shield Focus or armor training, plus BAB 3 or Fighter 1.", "Not registered"),
            new ModuleDefinition(ExemplarPerformanceResource.ModuleName, "Removes duplicate Brawler level counting from the shared performance resource. Pure Exemplar maximum is Brawler level + Charisma modifier from level 3. Preserves other resource bonuses; rest after updating to verify the refreshed maximum.", "Not detected"),
            new ModuleDefinition(RagePowers.ModuleName, "Adds Quick Reflexes, Unrestrained Rage, five Energy Resistance choices and Sharpened Accuracy for Barbarian, Skald and Primalist. Registration does not grant these powers.", "Not registered"),
            new ModuleDefinition(PsychicClass.ModuleName, "Registers Psychic (Casting Prototype): Intelligence spontaneous spells, thought/emotion components, Center Self, Logical Spell and the phrenic pool foundation. Additional amplifications, the other five disciplines and mythic merging remain pending. Registration is not game validation.", "Not registered"),
            new ModuleDefinition(VindictiveBastard0111.ModuleName, "Separate class with paladin spellbook/progression, retaliation-driven Vindictive Smite with its own daily resource, native Inquisitor Solo Tactics, Faded Grace, Spiteful Tenacity, Stalwart, teamwork feats, Gang Up (move action, half smite vs one target), Swift Justice, Aura of Self-Righteousness and the Ultimate Vindication ledger. Registration is not game validation.", "Not registered"),
            new ModuleDefinition(SacredServant0110.ModuleName, "Paladin archetype choosing one deity domain, using Paladin level minus 3 for the shared domain progression and adding the domain's special spell list to the Paladin spellbook. Replaces weapon bond, its additional uses, Aura of Resolve and three extra Smite Evil uses with the six-tier celestial-spirit Divine Bond and level-scaling Call Celestial Ally. Registration is not game validation.", "Not registered"),
            new ModuleDefinition(PsychicExpansion.ModuleName, "Adds 19 migrated Psychic spells, including Burst of Adrenaline and Burst of Insight, three undercast families and the complete Ferocity discipline with level 1/5/13 powers, nine bonus spells and Wisdom phrenic resources.", "Not registered"),
            new ModuleDefinition(PsychicAmplifications.ModuleName, "Eight basic amplifications: Biokinetic Healing, Defensive Prognostication, Overpowering Mind, Relentless Casting, Ongoing Defense, Focused Force, Mind's Eye and Conjured Armor. Choose at levels 1, 3, 7, 11, 15 and 19; Phrenic Mastery grants two more choices and +6 pool capacity at level 20. Other basic and major amplifications remain pending. Respec existing characters to choose missed amplifications.", "Not registered"),
            new ModuleDefinition(PsychicSpellIntegration.ModuleName, "Adds audited native spells and optional provider spells to the private Psychic list. Missing or disabled providers are skipped. Open this settings page before character creation/leveling to retry late providers. Restart after changing provider settings. Occultist and Spiritualist classes remain unimplemented.", "Not registered"),
            new ModuleDefinition(MediumIntegration.ModuleName, "Bundled Medium class, spirits and archetypes with shared Psychic casting and Logical Spell. Disable the old standalone Medium UMM entry before using this package. Existing Medium dependencies remain required.", "Not registered"),
            new ModuleDefinition(WayOfTheShieldIntegration.ModuleName, "Bundled Way of the Shield combat features; registration status for this session.", "Not registered"),
            new ModuleDefinition(T09CombatFeats.ModuleName, "Artful Dodge, Osyluth Guile and Felling Smash.", "Not registered"),
            new ModuleDefinition(DevastatingStrikeFeat.ModuleName, "Devastating Strike combat feat.", "Not registered"),
            new ModuleDefinition(FeralGraceFeat.ModuleName, "Feral Grace companion feat.", "Not registered"),
            new ModuleDefinition(T11TrainingOptions.ModuleName, "Advanced training options and magus arcana.", "Not registered"),
            new ModuleDefinition(SpellResourceFeats.ModuleName, "Amplified Hex and Rage Casting.", "Not registered"),
            new ModuleDefinition(ChosenWeaponFeats.ModuleName, "Weapon of the Chosen feat chain.", "Not registered"),
            new ModuleDefinition(WildShapeFeats.ModuleName, "Natural Spell, Wild Speech, Mutated Shape and Weapon Shift feats.", "Not registered"),
            new ModuleDefinition(TeamworkFeats.ModuleName, "Teamwork combat feats and their reaction settings.", "Not registered"),
            new ModuleDefinition(CooperationFeats.ModuleName, "Bonded spell sharing, gambits and Stunning Assault.", "Not registered"),
            new ModuleDefinition(LinnormFeats.ModuleName, "Linnorm Style, Vengeance, Wrath, Scales and Skin and Vicious Stomp.", "Not registered"),
            new ModuleDefinition(SpellAndSwordFeats.ModuleName, "Toppling Spell, Spontaneous Metafocus, Swordplay Style/Upset and Fey Foundling.", "Not registered"),
            new ModuleDefinition("Combat feat pool repair", "Preserves existing entries while adding eligible migrated combat feats.", "Not detected"),
            new ModuleDefinition("Advanced training integration", "Optional provider training menu integration.", "Not detected"),
            new ModuleDefinition(ProviderResourceFeats.ModuleName, "Extra Spell Synthesis for Mystical Mayhem; Ability Focus: Cruelty and Channel Corruption for MCE antipaladin. Only available providers are registered; rechecks on settings open.", "Not detected"),
            new ModuleDefinition(ImprovedTwoWeaponFeint.ModuleName, "Extends successful native Two-Weapon Feint for the feinter's remaining attacks until their turn ends.", "Not registered"),
            new ModuleDefinition(ExtraInvestigatorTalent.ModuleName, "One additional eligible Investigator talent per selection. Requires a complete HomebrewArchetypes talent provider; rechecks on settings open and preserves equivalent external feats.", "Not detected"),
            new ModuleDefinition(ReapingStalker.ModuleName, "Advanced slayer talent for sickles and scythes, using native damage-size and critical-range components. Available through slayer talent selections, including native and detected TTT Extra Slayer Talent; requires Advanced Talents.", "Not registered"),
            new ModuleDefinition(RogueFeatTalent.ModuleName, "Choose an eligible ordinary feat once as an advanced rogue/slayer talent.", "Not registered"),
            new ModuleDefinition(TerrifyingHowl.ModuleName, "Level 8 self-rage power; shaken enemies save against fear with 24-hour caster-specific immunity.", "Not registered"),
            new ModuleDefinition(Inquisitions01426.ModuleName, "Conversion, Valor and Persistence replace a cleric/inquisitor domain; no domain spell grants.", "Not registered"),
            new ModuleDefinition(MonkKiPowers01427.ModuleName, "Monk/Scaled Fist ki powers and Sensei advice. Cloak of Winds requires TTT; Akashic Form requires PrestigePlus.", "Not registered"),
            new ModuleDefinition(Inquisitions01428.ModuleName, "Heresy and Imprisonment replace a cleric/inquisitor domain; no domain spell grants.", "Not registered"),
            new ModuleDefinition(PurifyingFlames01428.ModuleName, "Fire utility wild talent; kineticist level 8, touch poison removal for 1 burn.", "Not registered"),
            new ModuleDefinition(WizardSubschools01428.ModuleName, "Arcane Crafter universalist and Teleportation conjuration options; native school progressions preserved.", "Not registered"),
            new ModuleDefinition(EnhancementSchool01429.ModuleName, "Transmutation option with Augment and Perfection of Self; native physical enhancement and capstone retained.", "Not registered"),
            new ModuleDefinition(FlameJet01429.ModuleName, "Fire utility talent: standard jet, optional move-action upgrade and flight; zero burn.", "Not registered"),
            new ModuleDefinition(FerociousBeast01431.ModuleName, "Animal companion shares personal rage and constant rage powers; each round costs one additional personal rage use. Barbarian and Primalist options.", "Not registered"),
            new ModuleDefinition(AngerInquisition01431.ModuleName, "Gorum/Rovagug inquisition: one daily melee retort and Divine Anger with a separate Wisdom-based resource.", "Not registered"),
            new ModuleDefinition(WizardSubschools01431.ModuleName, "Undead and Prophecy subschools; native school growth retained. COP Consecrate integration is optional.", "Not registered"),
            new ModuleDefinition(KineticInvocation01433.ModuleName, "Ordinary kineticist feat unlocking separately selected elemental spell utility talents. Optional Cloak of Winds follows enabled TTT publication.", "Not registered"),
            new ModuleDefinition(SpiritualGuardian01433.ModuleName, "Spiritual Weapon and Spiritual Ally use caster level for base attack bonus and gain +2 damage and spell penetration when their caster has this feat.", "Not registered"),
            new ModuleDefinition(KineticUtility01433.WindsightModule, "Air utility talent, kineticist level 6: ignore fog concealment while preserving other concealment.", "Not registered"),
            new ModuleDefinition(KineticUtility01433.ColdSnapModule, "Water utility talent, kineticist level 6: spend 1 burn for a swift cold aura, with cold immunity and resistance exceptions.", "Not registered"),
            new ModuleDefinition(KineticUtility01433.SmokeStormModule, "Fire utility talent, kineticist level 6: create a choking fog cloud for 0 burn.", "Not registered"),
            new ModuleDefinition(SuperstitionRage01434.SuperstitionModule, "Competence saves against spells and spell-like abilities during rage; original CotW harmless spell lists require allied saves.", "Not registered"),
            new ModuleDefinition(SuperstitionRage01434.WitchHunterModule, "Rage damage bonus against creatures with spells or spell-like abilities; requires Superstition.", "Not registered"),
            new ModuleDefinition(AntimagicRage01434.GhostModule, "Rage weapons affect incorporeal enemies normally; morale touch AC is capped by full AC. Requires Superstition and rage level 6.", "Not registered"),
            new ModuleDefinition(AntimagicRage01434.DisruptiveModule, "Grants native Disruptive while raging. Requires Superstition and rage level 8.", "Not registered"),
            new ModuleDefinition(AntimagicRage01434.SpellbreakerModule, "Uses the native counterstrike path when a threatened enemy successfully casts defensively. Requires the Disruptive rage power and rage level 12.", "Not registered"),
            new ModuleDefinition(WizardDiscoveries01435.ForestModule, "Arcane Discovery submenu only. Wizard level 5 or Arcanist level 10: +1 caster level and DC for spells on both the wizard and druid lists.", "Not registered"),
            new ModuleDefinition(WizardDiscoveries01435.ResilientModule, "Arcane Discovery submenu only. Wizard level 8 or Arcanist level 16: improve actual disbelief saves; ordinary illusion saves remain unchanged.", "Not registered"),
            new ModuleDefinition(TimeStutter01437.ModuleName, "Arcane Discovery submenu: Wizard 10 or Arcanist 20. Uses the native Time Stop effect for one round and an independent daily resource.", "Not registered"),
            new ModuleDefinition(Invoker01437.ModuleName, "Witch archetype replacing four hex choices with nine patron facets, swift one-minute invocations and a second simultaneous facet at level 10.", "Not registered"),
            new ModuleDefinition(TimeStop01438.ModuleName, "Personal 9th-level wizard/sorcerer/arcanist and Psychic spell: 1d4+1 rounds. Shares the repaired native freeze lifecycle, with no discovery or daily-resource requirement.", "Not registered"),
            new ModuleDefinition(IncreasedDamageReduction01439.ModuleName, "Stalwart defender defensive power: each selection adds DR 2/-, up to DR 4/- at two selections. Existing class, archetype and mod prerequisites stay. No stance, barbarian or class damage-reduction changes.", "Not registered"),
            new ModuleDefinition(WarDrummer01441.ModuleName, "Skald archetype: Fearsome Mien, personal club/greatclub Deadly Rhythm, and Improved Critical at level 6. Replaces martial weapon proficiency, Bardic Knowledge and the level-7 Versatile Performance choice.", "Not registered"),
            new ModuleDefinition(Sunsinger01443.ModuleName, "Skald archetype: Sarenrae, Channel Solar Energy and Pillar of Light. Replaces native Spell Kenning when present, preserving unrelated class features.", "Not registered"),
            new ModuleDefinition(UrbanSkald01443.ModuleName, "Skald archetype: controlled ability-score rage, Back of the Crowd dodge AC and Infuriating Mockery; native accept/refuse, resource and rage power paths.", "Not registered"),
            new ModuleDefinition(RogueSwashbuckler01443.ModuleName, "Rogue archetype: Martial Training and Daring. A qualified second Combat Trick is provided only when the upstream talent is once-only.", "Not registered"),
            new ModuleDefinition(HedgeWitch01443.ModuleName, "Witch archetype: native prepared cure conversion and +3 Lore (Religion) at level 8; replaces only hex choices at levels 4 and 8.", "Not registered"),
            new ModuleDefinition(Havocker0020.ModuleName, "Witch archetype: four patron elements, private Intelligence kinetic blasts and prepared-slot Spellburn. Development HOLD pending remaining execution-path proof.", "Not registered"),
            new ModuleDefinition(TerrorWeaver01444.ModuleName, "Psychic archetype: Command, Aura of Doom and caster-owned intimidation; replaces amplification choices at 11 and 19.", "Not registered"),
            new ModuleDefinition(PrimalSorcerer01444.ModuleName, "Four primal elemental bloodlines with per-die spell damage and Elementalist Summoning at level 9; excludes Dragon Disciple.", "Not registered"),
            new ModuleDefinition(Rake01444.ModuleName, "Rogue archetype: five private Bravado's Blade stances and Rake's Smile; replaces Trapfinding and Danger Sense.", "Not registered"),

            new ModuleDefinition(BardMasterpieces01435.ClamorModule, "Bard or Skald level 10 performance. PrestigePlus Bardic Masterpieces submenu and a level-5 Bard spell-known acquisition option; existing native performance resources.", "Not registered"),
            new ModuleDefinition(BardMasterpieces01435.DanceModule, "Bard or Skald level 4 performance. PrestigePlus Bardic Masterpieces submenu and a level-2 Bard spell-known acquisition option; native difficult spellcasting while performing.", "Not registered"),
            new ModuleDefinition(BardMasterpieces01435.SymphonyModule, "Bard or Skald level 7 performance. PrestigePlus Bardic Masterpieces submenu and a level-3 Bard spell-known acquisition option; audible ally freedom of movement.", "Not registered"),
            new ModuleDefinition(CourtBard01445.ModuleName, "Original CotW CourtBard archetype.", "Not registered"),
            new ModuleDefinition(NatureBondedMagus01445.ModuleName, "Original CotW NatureBondedMagus archetype.", "Not registered"),
            new ModuleDefinition(SageCounselor01445.ModuleName, "Original CotW SageCounselor archetype.", "Not registered"),
            new ModuleDefinition(GraveWarden01445.ModuleName, "Original CotW GraveWarden archetype.", "Not registered"),
            new ModuleDefinition(DrillSergeant01445.ModuleName, "Original CotW DrillSergeant archetype.", "Not registered"),
            new ModuleDefinition(BloodlineMutations01446.ModuleName, "Bloodline Familiar, Blood Intensity and Blood Piercing; keeps ECM's Blood Havoc and connects native, Seeker, Primal and verified external bloodlines.", "Not registered"),
            new ModuleDefinition(Admixture01446.ModuleName, "Original CotW Admixture focused school.", "Not registered"),
            new ModuleDefinition(Overseer01447.ModuleName, "Original CotW Overseer archetype.", "Not registered"),
            new ModuleDefinition(SpellSpecialist01447.ModuleName, "Original CotW Spell Specialist archetype.", "Not registered"),
            new ModuleDefinition(MagaambyanTelepath01447.ModuleName, "Original CotW Magaambyan Telepath archetype.", "Not registered"),
            new ModuleDefinition(Phantasm01447.ModuleName, "Original CotW Phantasm focused school.", "Not registered"),
            new ModuleDefinition(InfectiousCharms01447.ModuleName, "Original CotW Infectious Charms discovery.", "Not registered"),
            new ModuleDefinition(WhipHurricane01454.ModuleName, "Original CotW Whip Hurricane. Requires DarkCodex Kinetic Whip to be enabled and registered; rechecks when settings opens.", "Requires configured Kinetic Whip"),
            new ModuleDefinition(Mindblade01454.ModuleName, "Original CotW psychic Magus archetype, manifested weapons and Psychic Access.", "Not registered"),
            new ModuleDefinition(Skirnir01454.ModuleName, "Original CotW shield Magus archetype, arcane shield and Spellshield.", "Not registered"),
            new ModuleDefinition(Mindblade01454.BlendingModule, "When TabletopTweaks Spell Blending is enabled, its wizard-list choice is restricted to non-Mindblades; Mindblade keeps its own original psychic-list choice.", "Not detected"),
            new ModuleDefinition(MutationMind01457.ModuleName, "Original CotW Mutation Mind, twelve bodily mutations including Elongated Fingers, and a maintained Physical Mutation toggle.", "Not registered"),
            new ModuleDefinition(PsychicMarauder01457.ModuleName, "Original CotW Psychic Marauder, insanity aura and mental defenses.", "Not registered"),
            new ModuleDefinition(PsychicClassCompletion01458.ModuleName, "Original CotW Pain, Psychedelia and Abomination disciplines and remaining six phrenic amplification groups.", "Not registered"),
            new ModuleDefinition(FeralHunter01461.ModuleName, "Original CotW Feral Hunter: unlimited self animal focus, nature's ally, teamwork and animal wild shape.", "Not registered"),
            new ModuleDefinition(TotemBonded01461.ModuleName, "Original CotW Totem-Bonded: primeval companion and shared strength aspects.", "Not registered"),
            new ModuleDefinition(UrbanBloodrager01463.ModuleName, "Original CotW Urban Bloodrager: controlled bloodrage, restrained magic and adopted magic.", "Not registered"),
            new ModuleDefinition(Amnesiac01464.ModuleName, "Original CotW Amnesiac: retained spell knowledge and hourly Spell Recollection.", "Not registered"),
            new ModuleDefinition(PsychicDisciplineBridge01458.ModuleName, "Add the three completed disciplines to Magaambyan Telepath while preserving its replacement of discipline bonus spells.", "Not registered"),
            new ModuleDefinition(EsotericStarseeker0081.ModuleName, "Original CotW Esoteric Starseeker: psychic archetype replacing discipline bonus spells with Written in the Stars, attuning to a Cosmic Caravan constellation for one bonus spell per castable level (two constellations and two daily uses from 11th, Major Amplifications delayed to 15th). Registers only when all 90 constellation spells, including five from external providers, are available.", "Not registered"),
            new ModuleDefinition(SpiritWhisperer0117.ModuleName, "Original CotW Spirit Whisperer: Wizard archetype rebuilt from the live Shaman spirit pool. Discovers every spirit published in the current game (Owlcat and mod-added), reuses each spirit's own progression text, icon, powers and owned hexes, then rebinds them to the Wizard 1st/8th/20th progression; hex selections draw only from the chosen spirit's own hexes. Wizard level and Intelligence replace shaman level and Wisdom, while Charisma remains on the named daily-use branches. Familiar-only Arcane Bond and a private Life Extra Channel remain; registers atomically only when every native donor is available.", "Not registered"),
            new ModuleDefinition(LoreSpirit0118.ModuleName, "Standard Shaman Lore spirit with spirit magic, Arcane Enlightenment, Perfect Knowledge, Manifestation and four Lore hexes. Includes private audited Wish variants and atomic native/SpiritTools registration.", "Not registered"),
            new ModuleDefinition(SpiritSpellListOverrides0119.ModuleName, "Replaces Battle and Flame spirit magic at circle 4 with Wall of Fire, Flame circle 8 with Incendiary Cloud, and Waves circle 1 with Frostbite when their providers are available. Shared native spell lists are changed atomically and restored on failure.", "Not registered"),
            new ModuleDefinition(ShamanSpiritHexes0119.ModuleName, "Adds eight original CotW spirit hexes to the native Shaman hex pool and the SpiritTools Wandering Hex pools, gated on the owning spirit (including its wandering form), using Shaman level, Wisdom and Owlcat's native 24-hour target-lock pattern. Lure of the Heavens is left to ExpandedContent, which already publishes that hex. Spirits not installed in the current game are skipped.", "Not registered"),
            new ModuleDefinition(BattleMysteryBonusSpell0119.ModuleName, "Replaces the Oracle Battle mystery's bonus spell gained at 4th level (the 2nd-circle grant) with the migrated Fog Cloud, discovered from the live OracleMysterySelection by level and circle rather than a fixed GUID list. The shared native AddKnownSpell is snapshotted and restored on failure, and registration is idempotent.", "Not registered"),
            new ModuleDefinition(SpiritGuide0060.ModuleName, "Original CotW Spirit Guide rebuilt from the live Shaman spirit pool: discovers Owlcat and mod-added spirits, their level-1/8 powers, owned hexes and spirit spell lists at registration time, then rebinds them to the Oracle 3/4/7/15 progression with Charisma/Wisdom rules and temporary known spells. The private Life Extra Channel and atomic publication safeguards remain.", "Not registered"),
        };
        internal static Dictionary<string, string> InitialStates() =>
            Definitions.ToDictionary(d => d.Name, d => d.InitialState);
        internal static ModuleStatus[] Snapshot(IReadOnlyDictionary<string, string> states) =>
            Definitions.Select(d => new ModuleStatus(d, states.TryGetValue(d.Name, out var state) ? state : d.InitialState)).ToArray();
    }
}
