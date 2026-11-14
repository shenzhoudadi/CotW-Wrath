// COTW-0046 Oracle Time Mystery & Ancestors Mystery Completion.
// Conforms to native Wrath Oracle Mystery conventions (Groups, AddSpellsToDescription,
// AddFeaturesFromSelectionToDescription, OracleRevelationSelection, SecondMystery,
// Enlightened Philosopher / Divine Herbalist / Magic Deceiver / Hermit support).
//
// Time Mystery shares the exact combat-effect builders in SpeakerRevelationEffects0046
// (Rewind Time active/reaction D-0046-01 fixes and Time Flicker Standard Displacement)
// and completes Ancestors Mystery with Ancestral Weapon + Spirit Walk prerequisites so
// Speaker for the Past automatically discovers and clones all 18 spells and 16 revelations.
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using Rev = CallOfTheWild.Wrath.Prototype.SpeakerForThePast0046.Rev;
using ResCfg = CallOfTheWild.Wrath.Prototype.SpeakerForThePast0046.ResCfg;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class TimeMystery0046
    {
        internal const string ModuleName = "Oracle mystery: Time";
        internal const string RootToken = "TimeOracleMysteryFeature0046";
        internal const string SpellsToken = "TimeOracleMysterySpells0046";
        internal const string FinalToken = "TimeOracleFinalRevelation0046";
        internal const string FinalStopAbilityToken = "TimeOracleFinalTimeStopAbility0046";
        internal const string FinalStopResourceToken = "TimeOracleFinalTimeStopResource0046";
        internal const string EpMysteryToken = "EnlightenedPhilosopherTimeMysteryFeature0046";
        internal const string EpSpellsToken = "EnlightenedPhilosopherTimeSpells0046";
        internal const string DhMysteryToken = "DivineHerbalistTimeMysteryFeature0046";
        internal const string AncestralWeaponToken = "AncestralWeaponOracleRevelation0046";

        internal const string OracleClassId = "20ce9bf8af32bee4c8557a045ab499b1";
        internal const string ArcanistClassId = "52dbfd8505e22f84fad8d702611f60b7";
        internal const string MagicDeceiverArchetypeId = "5c77110cd0414e7eb4c2e485659c9a46";
        internal const string OracleMysterySelectionId = "5531b975dcdf0e24c98f1ff7e017e741";
        internal const string OracleRevelationSelectionId = "60008a10ad7ad6543b1f63016741a5d2";
        internal const string SecondMysteryId = "277b0164740b97945a3f8022bd572f48";
        internal const string MysteryGiftSelectionId = "e9629fc4de4c4b3ea1c26e9c13c2402d";
        internal const string EpMysterySelectionId = "9d5fdd3b4a6cd4f40beddbc72b2c07a0";
        internal const string SecondMysteryEpId = "4ff6f2905e1f4d1b92930b87f85bf86c";
        internal const string DhMysterySelectionId = "c11ff5dbd8518c941849b3112d4d6b68";
        internal const string SecondMysteryDhId = "cff5c53fe99c48bf863a0005d768f75a";
        internal const string HermitMysterySelectionId = "bc86ea452bef22a45989c6fa644bd2eb";
        internal const string SecondMysteryHermitId = "324476155d5c4968babfecda37bcc87c";
        internal const string OracleAncestorsMysteryId = "9936554e9372dab4a874c1dd165fb6f8";
        internal const string EpAncestorsMysteryId = "36e136a4cc77a434083a12a72678a9ba";
        internal const string DhAncestorsMysteryId = "2af1112188727bf469ec50c5575b6c52";
        internal const string OracleRevelationSpiritWalkId = "87bdea524911cfa4dacfe7243abe8b51";
        internal const string SandsOfTimeId = "da49f07e431b47abaad41ef28df487f7";

        internal static readonly string[] TimeSpellIds = {
            "14c90900b690cac429b229efdf416127", // 1: Longstrider
            "464a7193519429f48b4d190acb753cf0", // 2: Grace
            SandsOfTimeId,                      // 3: Sands of Time (TTT-Base)
            "503c3613f77e27d0a2650a09001b511f", // 4: Threefold Aspect
            "41e8a952da7a5c247b3ec1c2dbb73018", // 5: Hold Monster
            "b39dd2d96af348236b0117f57e5e445d", // 6: Contingency
            "4aa7942c3e62a164387a73184bca3fc1", // 7: Disintegrate
            "8b0761f3873aefb976c0f47fec3c600c", // 8: Temporal Stasis
            "585f7776285d17b60b9e8e2fe42a69be", // 9: TimeStopSpell01438
        };

        // Frozen C01 2.8 Ancestors spell table (circles 1..9); prevents Wrath's
        // OracleAncestorsSpells.jbp from drifting the 7 non-matching circles.
        internal static readonly string[] AncestorsFrozenSpellIds = {
            "2c38da66e5a599347ac95b3294acbe00", // 1: True Strike
            "b924f40705afa57283f65679a2468775", // 2: Spiritual Weapon
            "5ab0d42fb68c9e34abae4921822b9d63", // 3: Heroism
            "b406c2835f416afb0824cec610cba20a", // 4: Spiritual Ally
            "90810e5cf53bf854293cbd5ea1066252", // 5: Righteous Might
            "e15e5e7045fda2244b98c8f010adfe31", // 6: Heroism, Greater
            "98310a099009bbd4dbdf66bcef58b4cd", // 7: Invisibility, Mass
            "0e67fa8f011662c43934d486acc50253", // 8: Prediction of Failure
            "43740dab07286fe4aa00a6ee104ce7c1", // 9: Heroic Invocation
        };

        // Vanilla Enlightened Philosopher circles 2..9 fixed spell table.
        static readonly string[] EpFixedSpellIds = {
            "f0455c9295b53904f9e02fc571dd2ce1", // 2
            "c927a8b0cd3f5174f8c0b67cdbfde539", // 3
            "cf6c901fb7acc904e85c63b342e9c949", // 4
            "4cf3d0fae3239ec478f51e86f49161cb", // 5
            "9f5ada581af3db4419b54db77f44e430", // 6
            "fa08cb49ade3eee42b5fd42bd33cb407", // 7
            "df2a0ba6b6dcecf429cbb80a56fee5cf", // 8
            "87a29febd010993419f2a4a9bee11cfc", // 9
        };

        internal static string GateReport = "not probed";
        internal static string ProviderReport = "none";
        static bool Reject(string reason) { GateReport = reason; return false; }

        internal const string TimeMysteryText =
            "You understand the inexorable tick of the cosmic clock and bend its flow to your will. " +
            "An oracle with the time mystery adds Mobility, Perception and Use Magic Device to her list of class skills, " +
            "and gains bonus spells and revelations drawn from the Time mystery.";
        const string BonusSpellsIntroText =
            "You receive bonus spells at 2nd, 4th, 6th, 8th, 10th, 12th, 14th, 16th and 18th oracle levels.";
        const string FinalRevelationText =
            "Upon reaching 20th level, you become a true master of time and stop aging. " +
            "You gain a +2 competence bonus to Intelligence, Wisdom and Charisma, and may cast Time Stop once per day as a spell-like ability.";

        internal static Rev[] TimeRevelations() => new[] {
            new Rev { Token="AgingTouchOracleRevelation0046", Name="Aging Touch", Min=0,
                Text="As a standard action, touch a creature to wither it with the weight of years. Undead are unaffected; constructs take direct damage instead of ability damage.",
                ResToken="AgingTouchOracleRevelationResource0046", Res=ResCfg.Div(5,5,1,1),
                Action=UnitCommand.CommandType.Standard, Range=AbilityRange.Touch, Duration=1 },
            new Rev { Token="RewindTimeOracleRevelation0046", Name="Rewind Time", Min=7,
                Text="As a swift action, gain the ability to reroll one failed d20 check within the next round, taking the better result. Limited uses per day.",
                ResToken="RewindTimeOracleRevelationResource0046", Res=ResCfg.Gated(7,4),
                Action=UnitCommand.CommandType.Swift, Range=AbilityRange.Personal, Personal=true, Duration=1 },
            new Rev { Token="SpeedOrSlowTimeOracleRevelation0046", Name="Speed or Slow Time", Min=7,
                Text="As a standard action, either hasten yourself and your allies or slow your enemies. This is a supernatural ability.",
                ResToken="SpeedOrSlowTimeOracleRevelationResource0046", Res=ResCfg.Gated(7,5),
                Action=UnitCommand.CommandType.Standard, Range=AbilityRange.Close, Duration=1 },
            new Rev { Token="TemporalCelerityOracleRevelation0046", Name="Temporal Celerity", Min=0,
                Text="Roll initiative twice and take the better result. At 7th level you may act in a surprise round, and at 11th level you roll initiative three times.",
                Passive=true, Action=UnitCommand.CommandType.Free, Range=AbilityRange.Personal, Personal=true },
            new Rev { Token="TimeFlickerOracleRevelation0046", Name="Time Flicker", Min=3,
                Text="As a standard action, blur your outline for one minute. At 7th oracle level you may instead maintain a displacement effect, spending one minute of the same daily pool per round as a standard action.",
                ResToken="TimeFlickerOracleRevelationResource0046", Res=ResCfg.Level(1),
                Action=UnitCommand.CommandType.Standard, Range=AbilityRange.Personal, Personal=true, Duration=1, Rate=DurationRate.Minutes,
                ApplyIds=new[]{ SpeakerForThePast0046.BlurBuffId },
                GrantLevel=7, GrantToken="TimeFlickerOracleDisplacementFeature0046", GrantName="Time Flicker: Displacement", GrantBuffId=SpeakerForThePast0046.DisplacementBuffId },
            new Rev { Token="TimeHopOracleRevelation0046", Name="Time Hop", Min=7,
                Text="As a move action, step through a moment of time to teleport yourself, or spend two uses to bring allies with you.",
                ResToken="TimeHopOracleRevelationResource0046", Res=ResCfg.Div(3,3),
                Action=UnitCommand.CommandType.Move, Range=AbilityRange.Personal, Personal=true, Duration=1 },
            new Rev { Token="TimeSightOracleRevelation0046", Name="Time Sight", Min=11,
                Text="As a standard action, see things as they truly are for one minute. At 18th oracle level the effect also grants foresight.",
                ResToken="TimeSightOracleRevelationResource0046", Res=ResCfg.Level(1),
                Action=UnitCommand.CommandType.Standard, Range=AbilityRange.Personal, Personal=true, Duration=1, Rate=DurationRate.Minutes,
                ApplyIds=new[]{ SpeakerForThePast0046.TrueSeeingBuffId },
                GrantLevel=18, GrantToken="TimeSightOracleForesightFeature0046", GrantName="Time Sight: Foresight", GrantBuffId=SpeakerForThePast0046.ForesightBuffId },
            new Rev { Token="EraseFromTimeOracleRevelation0046", Name="Erase from Time", Min=0,
                Text="As a standard action, touch a creature and erase it from time. A successful Fortitude save negates the effect; the target is suspended and its other effects are paused.",
                ResToken="EraseFromTimeOracleRevelationResource0046", Res=ResCfg.Gated(11,100,1,1),
                Action=UnitCommand.CommandType.Standard, Range=AbilityRange.Touch, Duration=1 },
        };

        internal static Rev AncestralWeaponSpec() => new Rev {
            Token = AncestralWeaponToken,
            Name = "Ancestral Weapon",
            Min = 0,
            Text = "As a standard action, grant your primary weapon the ghost touch property for one minute.",
            ResToken = "AncestralWeaponOracleRevelationResource0046",
            Res = ResCfg.Level(1),
            Action = UnitCommand.CommandType.Standard,
            Range = AbilityRange.Personal,
            Personal = true,
            Duration = 1,
            Rate = DurationRate.Minutes
        };

        // C01 2.7 strictly isolates native Oracle mysteries, selections and prerequisites.
        static readonly bool OracleExpansionEnabled = false;

        internal static BlueprintFeature BuildSpeakerRevelation(PsychicAssets f, RevelationBinding0046 bind,
            Rev spec, Dictionary<string, BlueprintUnitFact> leaf)
        {
            var donor = leaf[SpeakerForThePast0046.IconDonorId];
            string stem = spec.Token.EndsWith("OracleRevelation0046", StringComparison.Ordinal)
                ? spec.Token.Substring(0, spec.Token.Length - "OracleRevelation0046".Length)
                : spec.Token;
            var mapped = new Rev {
                Token = stem + "ShamanRevelationFeature0046",
                Name = spec.Name,
                Text = spec.Text,
                Min = spec.Min,
                ResToken = stem + "ShamanRevelationResource0046",
                Res = spec.Res,
                Action = spec.Action,
                Range = spec.Range,
                Duration = spec.Duration,
                Rate = spec.Rate,
                Personal = spec.Personal,
                ApplyIds = spec.ApplyIds,
                OwnBuffTokens = spec.OwnBuffTokens,
                Passive = spec.Passive,
                GrantLevel = spec.GrantLevel,
                GrantToken = spec.GrantToken,
                GrantName = spec.GrantName,
                GrantBuffId = spec.GrantBuffId
            };
            var comps = new List<BlueprintComponent>();
            if (mapped.Min > 0)
                comps.Add(RevelationBridge0046.Level(bind, mapped.Min));
            BlueprintAbilityResource res = null;
            if (mapped.Res != null)
            {
                res = SpeakerForThePast0046.BuildResource(f, bind, mapped.ResToken, mapped.Res);
                comps.Add(ArchetypeBatch01445.Grant(res));
            }
            if (mapped.Passive)
                comps.AddRange(SpeakerForThePast0046.BuildTemporalCelerityEffects(f, bind, donor, "SpeakerTemporalCelerity"));
            else
                comps.AddRange(SpeakerForThePast0046.BuildEffects(f, bind, mapped, res, leaf));

            var feat = f.Feature(mapped.Token, mapped.Name, mapped.Text, comps.ToArray());
            feat.Groups = Array.Empty<FeatureGroup>();
            feat.Ranks = 1;
            feat.IsClassFeature = true;
            feat.HideInUI = false;
            feat.HideInCharacterSheetAndLevelUp = false;
            RevelationBridge0046.Icon(feat, donor);
            return feat;
        }

        internal static bool Register(BlueprintsCache cache)
        {
            if (!OracleExpansionEnabled)
                return Reject("disabled: COTW-0046 keeps native Oracle mysteries, selections and Spirit Walk untouched per C01 2.7");

            GateReport = "preflight";
            var oracle = cache.Load(BlueprintGuid.Parse(OracleClassId)) as BlueprintCharacterClass;
            if (oracle == null) return Reject("reject: OracleClass " + OracleClassId + " missing");
            var mysterySelection = cache.Load(BlueprintGuid.Parse(OracleMysterySelectionId)) as BlueprintFeatureSelection;
            var revelationSelection = cache.Load(BlueprintGuid.Parse(OracleRevelationSelectionId)) as BlueprintFeatureSelection;
            var secondMystery = cache.Load(BlueprintGuid.Parse(SecondMysteryId)) as BlueprintFeatureSelection;
            var ancestors = cache.Load(BlueprintGuid.Parse(OracleAncestorsMysteryId)) as BlueprintFeature;
            if (mysterySelection == null || revelationSelection == null || secondMystery == null || ancestors == null)
                return Reject("reject: Oracle mystery/revelation selections or Ancestors mystery missing");

            if (cache.Load(Id(RootToken)) != null)
            {
                GateReport = "already published (idempotent)";
                return true;
            }

            // Pre-validate all 9 Time Mystery spells (including external TTT-Base Sands of Time).
            var spells = new BlueprintAbility[9];
            for (int i = 0; i < 9; i++)
            {
                var guid = TimeSpellIds[i];
                if (guid == SandsOfTimeId)
                {
                    var link = PsychicSharedSpellCatalog.Entries.FirstOrDefault(l => l.Guid == guid);
                    if (link == null) return Reject("reject: Sands of Time link missing from PsychicSharedSpellCatalog");
                    if (!PsychicSpellIntegration.Available(cache, link, out spells[i]))
                    {
                        ProviderReport = "missing: " + link.Provider + " / " + link.Name + " / " + guid;
                        return Reject("reject: external provider " + link.Provider +
                            " has not published " + link.Name + " (" + guid + "); TimeMystery Register returns false by contract");
                    }
                    ProviderReport = "available: " + link.Provider + " / " + link.Name + " / " + guid;
                }
                else
                {
                    spells[i] = cache.Load(BlueprintGuid.Parse(guid)) as BlueprintAbility;
                    if (!SpeakerForThePast0046.ValidSpell(spells[i]))
                        return Reject("reject: Time spell " + guid + " missing or not executable");
                }
            }

            // Pre-validate shared leaf and non-fact native donors used by revelation builders.
            var leaf = SpeakerForThePast0046.LoadLeafAssets(cache, out var missingLeaf);
            if (leaf == null) return Reject("reject: native leaf " + missingLeaf + " missing");
            foreach (var donor in SpeakerForThePast0046.ExtraNativeIds)
            {
                var asset = cache.Load(BlueprintGuid.Parse(donor.Guid));
                if (asset == null || !donor.Type.IsInstanceOfType(asset))
                    return Reject("reject: native donor " + donor.Guid + " missing or not a " + donor.Type.Name);
            }

            // Optional Oracle sub-archetypes and secondary selections.
            var arcanist = cache.Load(BlueprintGuid.Parse(ArcanistClassId)) as BlueprintCharacterClass;
            var magicDeceiver = cache.Load(BlueprintGuid.Parse(MagicDeceiverArchetypeId)) as BlueprintArchetype;
            var mysteryGift = cache.Load(BlueprintGuid.Parse(MysteryGiftSelectionId)) as BlueprintFeatureSelection;
            var epSelection = cache.Load(BlueprintGuid.Parse(EpMysterySelectionId)) as BlueprintFeatureSelection;
            var epSecond = cache.Load(BlueprintGuid.Parse(SecondMysteryEpId)) as BlueprintFeatureSelection;
            var epAncestors = cache.Load(BlueprintGuid.Parse(EpAncestorsMysteryId)) as BlueprintFeature;
            var dhSelection = cache.Load(BlueprintGuid.Parse(DhMysterySelectionId)) as BlueprintFeatureSelection;
            var dhSecond = cache.Load(BlueprintGuid.Parse(SecondMysteryDhId)) as BlueprintFeatureSelection;
            var dhAncestors = cache.Load(BlueprintGuid.Parse(DhAncestorsMysteryId)) as BlueprintFeature;
            var hermitSelection = cache.Load(BlueprintGuid.Parse(HermitMysterySelectionId)) as BlueprintFeatureSelection;
            var hermitSecond = cache.Load(BlueprintGuid.Parse(SecondMysteryHermitId)) as BlueprintFeatureSelection;
            var spiritWalk = cache.Load(BlueprintGuid.Parse(OracleRevelationSpiritWalkId)) as BlueprintFeature;

            BlueprintAbility[] epSpells = null;
            if (epSelection != null)
            {
                epSpells = EpFixedSpellIds.Select(id => cache.Load(BlueprintGuid.Parse(id)) as BlueprintAbility).ToArray();
                if (epSpells.Any(s => !SpeakerForThePast0046.ValidSpell(s))) epSpells = null;
            }

            var f = new PsychicAssets(cache);
            var iconDonor = leaf[SpeakerForThePast0046.IconDonorId];

            // 1. Time Mystery bonus spells feature (level 2..18 spells).
            var spellsFeature = BuildSpellsFeature(f, oracle, SpellsToken, "Time Mystery Spells", TimeMysteryText, spells);

            // 2. Time Mystery Final Revelation (+2 competence Int/Wis/Cha + Time Stop 1/day SLA copy).
            var finalStopRes = f.Add(new BlueprintAbilityResource(), FinalStopResourceToken, "", "");
            var finalStopMax = Get(finalStopRes, "m_MaxAmount");
            Set(finalStopMax, "BaseValue", 1);
            Set(finalStopMax, "m_Class", Array.Empty<BlueprintCharacterClassReference>());
            Set(finalStopMax, "m_ClassDiv", Array.Empty<BlueprintCharacterClassReference>());
            Set(finalStopMax, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(finalStopMax, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>());
            Set(finalStopRes, "m_MaxAmount", finalStopMax);

            // Clone Time Stop into a private SLA ability without editing TimeStop01438.Root.
            var finalStopSla = f.Clone(spells[8], FinalStopAbilityToken, "Time Stop (Final Revelation)", FinalRevelationText);
            finalStopSla.ComponentsArray = finalStopSla.ComponentsArray
                .Where(c => c != null && !(c is SpellListComponent) && !(c is AbilityResourceLogic))
                .Concat(new BlueprintComponent[] {
                    ArchetypeBatch01445.Cost(finalStopRes),
                    RevelationBridge0046.Params(oracle, StatType.Charisma)
                }).ToArray();
            finalStopSla.Type = AbilityType.SpellLike;
            finalStopSla.AvailableMetamagic = 0;
            finalStopSla.MaterialComponent = new BlueprintAbility.MaterialComponentData();

            var finalRevComps = new List<BlueprintComponent> {
                new AddStatBonus { name = "TimeFinalInt", Stat = StatType.Intelligence, Value = 2, Descriptor = ModifierDescriptor.Competence },
                new AddStatBonus { name = "TimeFinalWis", Stat = StatType.Wisdom, Value = 2, Descriptor = ModifierDescriptor.Competence },
                new AddStatBonus { name = "TimeFinalCha", Stat = StatType.Charisma, Value = 2, Descriptor = ModifierDescriptor.Competence },
                ArchetypeBatch01445.Grant(finalStopRes),
                Facts(finalStopSla)
            };
            var finalRevelation = f.Feature(FinalToken, "Time Final Revelation", FinalRevelationText, finalRevComps.ToArray());
            finalRevelation.IsClassFeature = true;
            finalRevelation.HideInCharacterSheetAndLevelUp = false;
            RevelationBridge0046.Icon(finalRevelation, iconDonor);

            // 3. Main Time Mystery feature (matching OracleAncestorsMysteryFeature schema).
            var level20Grant = ClassLevelGrant(oracle, 20, finalRevelation, arcanist, magicDeceiver);
            var level2Grant = ClassLevelGrant(oracle, 2, spellsFeature, null, null);
            var revealDesc = new AddFeaturesFromSelectionToDescription {
                name = "TimeOracleRevelationsDesc",
                OnlyIfRequiresThisFeature = true
            };
            Set(revealDesc, "m_FeatureSelection", Ref<BlueprintFeatureSelectionReference>(revelationSelection));

            var mysteryFeature = f.Feature(RootToken, "Time", TimeMysteryText,
                level20Grant, level2Grant,
                SkillComp(StatType.SkillMobility),
                SkillComp(StatType.SkillPerception),
                SkillComp(StatType.SkillUseMagicDevice),
                revealDesc);
            mysteryFeature.Groups = new[] { FeatureGroup.OracleMystery };
            mysteryFeature.IsClassFeature = true;
            mysteryFeature.HideInCharacterSheetAndLevelUp = false;
            RevelationBridge0046.Icon(mysteryFeature, iconDonor);

            // Optional Enlightened Philosopher / Divine Herbalist Time mystery variants.
            var timeMysteries = new List<BlueprintFeature> { mysteryFeature };
            BlueprintFeature epMystery = null;
            if (epSelection != null && epSpells != null)
            {
                var epTable = new[] { spells[0] }.Concat(epSpells).ToArray();
                var epSpellsFeat = BuildSpellsFeature(f, oracle, EpSpellsToken, "Enlightened Philosopher Time Spells", TimeMysteryText, epTable);
                epMystery = f.Feature(EpMysteryToken, "Time", TimeMysteryText,
                    ClassLevelGrant(oracle, 2, epSpellsFeat, null, null),
                    SkillComp(StatType.SkillMobility),
                    SkillComp(StatType.SkillPerception),
                    SkillComp(StatType.SkillUseMagicDevice));
                epMystery.Groups = new[] { FeatureGroup.EnlightenedPhilosopherMystery };
                epMystery.IsClassFeature = true;
                epMystery.HideInCharacterSheetAndLevelUp = false;
                RevelationBridge0046.Icon(epMystery, iconDonor);
                timeMysteries.Add(epMystery);
            }

            BlueprintFeature dhMystery = null;
            if (dhSelection != null)
            {
                dhMystery = f.Feature(DhMysteryToken, "Time", TimeMysteryText,
                    ClassLevelGrant(oracle, 20, finalRevelation, null, null),
                    ClassLevelGrant(oracle, 2, spellsFeature, null, null));
                dhMystery.Groups = new[] { FeatureGroup.DivineHerbalistMystery };
                dhMystery.IsClassFeature = true;
                dhMystery.HideInCharacterSheetAndLevelUp = false;
                RevelationBridge0046.Icon(dhMystery, iconDonor);
                timeMysteries.Add(dhMystery);
            }

            // 4. All 8 Time revelations + missing Ancestral Weapon revelation for Ancestors.
            var bind = RevelationBridge0046.ForOracle(oracle, arcanist, magicDeceiver);
            var timeSpecs = TimeRevelations();
            var timeRevFeatures = new BlueprintFeature[timeSpecs.Length];
            for (int i = 0; i < timeSpecs.Length; i++)
                timeRevFeatures[i] = BuildOracleRevelation(f, bind, timeSpecs[i], timeMysteries.ToArray(), leaf);

            var ancestorsList = new[] { ancestors, epAncestors, dhAncestors }.Where(x => x != null).ToArray();
            var ancestralWeaponOracle = BuildOracleRevelation(f, bind, AncestralWeaponSpec(), ancestorsList, leaf);

            // 5. Commit atomically with full rollback on external selection / Spirit Walk edits.
            var allPublishedRevelations = timeRevFeatures.Concat(new[] { ancestralWeaponOracle }).ToArray();
            var undoStack = new List<Action>();

            void Connect()
            {
                AppendSelection(mysterySelection, new[] { mysteryFeature }, undoStack);
                AppendSelection(secondMystery, new[] { mysteryFeature }, undoStack);
                if (mysteryGift != null) AppendSelection(mysteryGift, new[] { mysteryFeature }, undoStack);
                if (hermitSelection != null) AppendSelection(hermitSelection, new[] { mysteryFeature }, undoStack);
                if (hermitSecond != null) AppendSelection(hermitSecond, new[] { mysteryFeature }, undoStack);
                if (epMystery != null)
                {
                    AppendSelection(epSelection, new[] { epMystery }, undoStack);
                    if (epSecond != null) AppendSelection(epSecond, new[] { epMystery }, undoStack);
                }
                if (dhMystery != null)
                {
                    AppendSelection(dhSelection, new[] { dhMystery }, undoStack);
                    if (dhSecond != null) AppendSelection(dhSecond, new[] { dhMystery }, undoStack);
                }

                AppendSelection(revelationSelection, allPublishedRevelations, undoStack);
                if (spiritWalk != null)
                {
                    AddMysteryPrereqs(spiritWalk, ancestorsList, undoStack);
                    AppendSelection(revelationSelection, new[] { spiritWalk }, undoStack);
                }
            }

            void Undo()
            {
                for (int i = undoStack.Count - 1; i >= 0; i--)
                    undoStack[i]();
            }

            SpellBatch01447.Publish(ModuleName, f, Connect, Undo, () => { });
            GateReport = "published: Oracle Time Mystery (9 spells, 8 revelations) + Ancestors (Spirit Walk + Ancestral Weapon) / " + ProviderReport;
            return true;
        }

        static BlueprintFeature BuildSpellsFeature(PsychicAssets f, BlueprintCharacterClass oracle,
            string token, string name, string text, BlueprintAbility[] spells)
        {
            var comps = new List<BlueprintComponent>();
            for (int circle = 1; circle <= 9; circle++)
            {
                var ak = new AddKnownSpell { name = token + "Known" + circle, SpellLevel = circle };
                Set(ak, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(oracle));
                Set(ak, "m_Spell", Ref<BlueprintAbilityReference>(spells[circle - 1]));
                Set(ak, "m_Archetype", new BlueprintArchetypeReference());
                Set(ak, "m_ExcludeArchetype", new BlueprintArchetypeReference());
                comps.Add(ak);
            }
            var desc = new AddSpellsToDescription { name = token + "Desc" };
            Set(desc, "Introduction", Text(token, "spellsIntro", BonusSpellsIntroText));
            Set(desc, "m_SpellLists", Array.Empty<BlueprintSpellListReference>());
            Set(desc, "m_Spells", spells.Select(Ref<BlueprintAbilityReference>).ToArray());
            comps.Add(desc);

            var feature = f.Feature(token, name, text, comps.ToArray());
            feature.IsClassFeature = true;
            feature.HideInCharacterSheetAndLevelUp = true;
            return feature;
        }

        static BlueprintFeature BuildOracleRevelation(PsychicAssets f, RevelationBinding0046 bind,
            Rev spec, BlueprintFeature[] mysteryPrereqs, Dictionary<string, BlueprintUnitFact> leaf)
        {
            var donor = leaf[SpeakerForThePast0046.IconDonorId];
            var components = new List<BlueprintComponent>();

            var prereqList = new PrerequisiteFeaturesFromList {
                name = spec.Token + "MysteryPrereq",
                Amount = 1,
                Group = Prerequisite.GroupType.All
            };
            Set(prereqList, "m_Features", mysteryPrereqs.Select(Ref<BlueprintFeatureReference>).ToArray());
            components.Add(prereqList);

            if (spec.Min > 0)
                components.Add(RevelationBridge0046.Level(bind, spec.Min));

            BlueprintAbilityResource resource = null;
            if (spec.ResToken != null)
            {
                resource = SpeakerForThePast0046.BuildResource(f, bind, spec.ResToken, spec.Res);
                components.Add(ArchetypeBatch01445.Grant(resource));
            }

            if (spec.Passive)
                components.AddRange(SpeakerForThePast0046.BuildTemporalCelerityEffects(f, bind, donor, "TimeOracleTemporalCelerity"));
            else
                components.AddRange(SpeakerForThePast0046.BuildEffects(f, bind, spec, resource, leaf));

            var feature = f.Feature(spec.Token, spec.Name, spec.Text, components.ToArray());
            feature.Groups = new[] { FeatureGroup.OracleRevelation };
            feature.Ranks = 1;
            feature.IsClassFeature = true;
            feature.HideInCharacterSheetAndLevelUp = true;
            RevelationBridge0046.Icon(feature, donor);
            return feature;
        }

        static AddFeatureOnClassLevel ClassLevelGrant(BlueprintCharacterClass oracle, int level,
            BlueprintFeature feat, BlueprintCharacterClass extraClass, BlueprintArchetype extraArchetype)
        {
            var c = new AddFeatureOnClassLevel { name = feat.name + "L" + level, Level = level, BeforeThisLevel = false };
            Set(c, "m_Class", Ref<BlueprintCharacterClassReference>(oracle));
            Set(c, "m_Feature", Ref<BlueprintFeatureReference>(feat));
            Set(c, "m_AdditionalClasses", extraClass == null
                ? Array.Empty<BlueprintCharacterClassReference>()
                : new[] { Ref<BlueprintCharacterClassReference>(extraClass) });
            Set(c, "m_Archetypes", extraArchetype == null
                ? Array.Empty<BlueprintArchetypeReference>()
                : new[] { Ref<BlueprintArchetypeReference>(extraArchetype) });
            return c;
        }

        static AddClassSkill SkillComp(StatType skill) => new AddClassSkill { name = "TimeSkill" + skill, Skill = skill };

        static void AppendSelection(BlueprintFeatureSelection selection, BlueprintFeature[] additions, List<Action> undo)
        {
            if (selection == null || additions == null || additions.Length == 0) return;
            var allBefore = (BlueprintFeatureReference[])Get(selection, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
            var featBefore = (BlueprintFeatureReference[])Get(selection, "m_Features") ?? Array.Empty<BlueprintFeatureReference>();
            var missing = additions.Where(a => a != null && !allBefore.Any(r => r != null && r.Guid == a.AssetGuid)).ToArray();
            if (missing.Length == 0 && !(selection.Group == FeatureGroup.None && additions.Any(a => a != null && !featBefore.Any(r => r != null && r.Guid == a.AssetGuid))))
                return;

            var snapAll = allBefore.ToArray();
            var snapFeat = featBefore.ToArray();
            undo.Add(() => {
                Set(selection, "m_AllFeatures", snapAll);
                Set(selection, "m_Features", snapFeat);
            });

            var nextAll = allBefore.Concat(missing.Select(Ref<BlueprintFeatureReference>)).ToArray();
            Set(selection, "m_AllFeatures", nextAll);

            if (selection.Group == FeatureGroup.None || featBefore.Length > 0)
            {
                var missingFeat = additions.Where(a => a != null && !featBefore.Any(r => r != null && r.Guid == a.AssetGuid)).ToArray();
                if (missingFeat.Length > 0)
                    Set(selection, "m_Features", featBefore.Concat(missingFeat.Select(Ref<BlueprintFeatureReference>)).ToArray());
            }
        }

        static void AddMysteryPrereqs(BlueprintFeature target, BlueprintFeature[] mysteries, List<Action> undo)
        {
            var prereq = target.ComponentsArray?.OfType<PrerequisiteFeaturesFromList>().FirstOrDefault();
            if (prereq == null) return;
            var existing = (BlueprintFeatureReference[])Get(prereq, "m_Features") ?? Array.Empty<BlueprintFeatureReference>();
            var missing = mysteries.Where(m => m != null && !existing.Any(r => r != null && r.Guid == m.AssetGuid)).ToArray();
            if (missing.Length == 0) return;

            var snap = existing.ToArray();
            undo.Add(() => Set(prereq, "m_Features", snap));
            Set(prereq, "m_Features", existing.Concat(missing.Select(Ref<BlueprintFeatureReference>)).ToArray());
        }
    }
}
