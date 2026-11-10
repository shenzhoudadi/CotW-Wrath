using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Class.Kineticist.Properties;
using Kingmaker.UnitLogic.Mechanics.Properties;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class BlueprintRegistration
    {
        // This prototype has its own identity: Kingmaker saves are not portable.
        internal const string FeatureId = "e7f2ef3960ee4a7e88d6fb773b0a6dcc";
        internal const string BasicFeatsId = "247a4068296e8be42890143f451b4b45";
        internal const string IronWillId = "175d1577bb6c9a04baf88eec99c66334";
        internal const string FeatureName = "CotWWrathPrototypeSteadfastPersonality";
        internal const string DisplayNameKey = "650aef18-9d51-4af7-9609-23e0e53c5dd9";
        internal const string DescriptionKey = "3dfbe98a-aa2f-49d0-a116-b12b2ad44e14";
        internal const string ExtendedBaneId = "f362a8fdff7146d5b2e614c39349718d";
        internal const string BaneResourceId = "a708945b17c56fa4196e8d20f8af1b0d";
        internal const string BaneFeatureId = "7ddf7fbeecbe78342b83171d888028cf";
        internal const string ExtraBaneId = "756dc2f7340b0b34285a1dc367ff7359";
        internal const string ExtendedBaneNameKey = "d8b9a823-2bb4-41e0-a203-133c733aaef7";
        internal const string ExtendedBaneDescriptionKey = "af58b74c-64c9-47be-b69e-319ed85bff9b";
        internal const string ImprovedCombatExpertiseId = "c01055f33991416bb1ecc332a578e7cd";
        internal const string ImprovedCombatExpertiseNameKey = "f07eef7e-4545-4ac8-996c-8d3d9d7b2321";
        internal const string ImprovedCombatExpertiseDescriptionKey = "f21c1e3e-56df-4d8c-845e-658bec53fecc";
        internal const string CombatExpertiseId = "4c44724ffa8844f4d9bedb5bb27d144a";
        internal const string CombatExpertisePenaltyPropertyId = "8a63b06d20838954e97eb444f805ec89";
        // RealityMachina/BrawlerClassWrath uses a separate combat feat selection.
        // These identifiers are read from its MIT-licensed, pinned source snapshot.
        internal const string BrawlerClassId = "1c44d137122540d5aed818fd7902625f";
        internal const string BrawlerCombatFeatSelectionId = "93acb287d012446f9bc4f6203d3e57d6";
        internal static readonly string[] NativeCombatFeatSelectionIds = {
            "66befe7b24c42dd458952e3c47c93563", // Magus
            "41c8486641f7d6d4283ca9dae4147a9f", // Fighter
            "da03141df23f3fe45b0c7c323a8e5a0e", // Eldritch Knight
            "79c6421dbdb028c4fa0c31b8eea95f16", // War domain
            "c5158a6622d0b694a99efb1d0025d2c1"  // Rogue combat trick
        };

        // Each module is its own transaction: a new module failure must not remove
        // the older blueprint needed by existing experimental saves.
        [ThreadStatic] private static RegistrationRetry01444 initialPublication;
        internal static void RegisterInitial(BlueprintsCache cache, RegistrationRetry01444 state, Action<string, bool, Exception> report)
        {
            var previous = initialPublication; initialPublication = state;
            try { RegisterAll(cache, report); } finally { initialPublication = previous; }
        }
        internal static void RetryInitial(RegistrationRetry01444 state, Action<string, bool, Exception> report)
        {
            var previous = initialPublication; initialPublication = state;
            try { state.Retry(report); } finally { initialPublication = previous; }
        }
        internal static void RegisterAll(BlueprintsCache cache, Action<string, bool, Exception> report)
        {
            RegisterModule("Steadfast Personality", () => Register(cache), report);
            RegisterModule("Extended Bane", () => RegisterExtendedBane(cache), report);
            RegisterModule("Improved Combat Expertise", () => RegisterImprovedCombatExpertise(cache), report);
            RegisterModule(ShieldedMage.ModuleName, () => ShieldedMage.Register(cache), report);
            RegisterModule(T09CombatFeats.ModuleName, () => T09CombatFeats.Register(cache), report);
            RegisterModule(DevastatingStrikeFeat.ModuleName, () => DevastatingStrikeFeat.Register(cache), report);
            RegisterModule(FeralGraceFeat.ModuleName, () => FeralGraceFeat.Register(cache), report);
            RegisterModule(T11TrainingOptions.ModuleName, () => T11TrainingOptions.Register(cache), report);
            RegisterModule(SpellResourceFeats.ModuleName, () => SpellResourceFeats.Register(cache), report);
            RegisterModule(ChosenWeaponFeats.ModuleName, () => ChosenWeaponFeats.Register(cache), report);
            RegisterModule(WildShapeFeats.ModuleName, () => WildShapeFeats.Register(cache), report);
            RegisterModule(TeamworkFeats.ModuleName, () => TeamworkFeats.Register(cache), report);
            RegisterModule(CooperationFeats.ModuleName, () => CooperationFeats.Register(cache), report);
            RegisterModule(LinnormFeats.ModuleName, () => LinnormFeats.Register(cache), report);
            RegisterModule(SpellAndSwordFeats.ModuleName, () => SpellAndSwordFeats.Register(cache), report);
            RegisterModule(ImprovedTwoWeaponFeint.ModuleName, () => ImprovedTwoWeaponFeint.Register(cache), report);
            RegisterModule(ReapingStalker.ModuleName, () => ReapingStalker.Register(cache), report);
            RegisterModule(RogueFeatTalent.ModuleName, () => RogueFeatTalent.Register(cache), report);
            RegisterModule(TerrifyingHowl.ModuleName, () => TerrifyingHowl.Register(cache), report);
            RegisterModule(Inquisitions01426.ModuleName, () => Inquisitions01426.Register(cache), report);
            RegisterModule(Inquisitions01428.ModuleName, () => Inquisitions01428.Register(cache), report);
            RegisterModule(PurifyingFlames01428.ModuleName, () => PurifyingFlames01428.Register(cache), report);
            RegisterModule(WizardSubschools01428.ModuleName, () => WizardSubschools01428.Register(cache), report);
            RegisterModule(EnhancementSchool01429.ModuleName, () => EnhancementSchool01429.Register(cache), report);
            RegisterModule(RagePowers.ModuleName, () => RagePowers.Register(cache), report);
            RegisterModule(SuperstitionRage01434.SuperstitionModule, () => SuperstitionRage01434.RegisterSuperstition(cache), report);
            RegisterModule(SuperstitionRage01434.WitchHunterModule, () => SuperstitionRage01434.RegisterWitchHunter(cache), report);
            RegisterModule(AntimagicRage01434.GhostModule, () => AntimagicRage01434.RegisterGhostRager(cache), report);
            RegisterModule(AntimagicRage01434.DisruptiveModule, () => AntimagicRage01434.RegisterDisruptive(cache), report);
            RegisterModule(AntimagicRage01434.SpellbreakerModule, () => AntimagicRage01434.RegisterSpellbreaker(cache), report);
            RegisterModule(FerociousBeast01431.ModuleName, () => FerociousBeast01431.Register(cache), report);
            RegisterModule(AngerInquisition01431.ModuleName, () => AngerInquisition01431.Register(cache), report);
            RegisterModule(PsychicClass.ModuleName, () => {
                if(!PsychicCasting.RuntimePatchesAvailable) throw new InvalidOperationException("Psychic runtime patches are unavailable; registration skipped.");
                PsychicClass.Register(cache);
            }, report);
            RegisterModule(VindictiveBastard0111.ModuleName, () => VindictiveBastard0111.Register(cache), report);
            RegisterModule(SacredServant0110.ModuleName, () => SacredServant0110.Register(cache), report);
            RegisterModule(PsychicExpansion.ModuleName, () => {
                if(!PsychicCasting.RuntimePatchesAvailable) throw new InvalidOperationException("Psychic runtime patches are unavailable; registration skipped.");
                PsychicExpansion.Register(cache);
            }, report);
            // Two ki donors are owned by PsychicExpansion; register only after
            // that module has had its initial opportunity to publish them.
            RegisterModule(MonkKiPowers01427.ModuleName, () => MonkKiPowers01427.Register(cache), report);
            RegisterModule(FlameJet01429.ModuleName, () => FlameJet01429.Register(cache), report);
            RegisterModule(WizardSubschools01431.ModuleName, () => WizardSubschools01431.Register(cache), report);
            RegisterModule(SpiritualGuardian01433.ModuleName, () => SpiritualGuardian01433.Register(cache), report);
            RegisterModule(KineticInvocation01433.ModuleName, () => KineticInvocation01433.Register(cache), report);
            RegisterModule(KineticUtility01433.WindsightModule, () => KineticUtility01433.RegisterWindsight(cache), report);
            RegisterModule(KineticUtility01433.ColdSnapModule, () => KineticUtility01433.RegisterColdSnap(cache), report);
            RegisterModule(KineticUtility01433.SmokeStormModule, () => KineticUtility01433.RegisterSmokeStorm(cache), report);
            RegisterModule(WizardDiscoveries01435.ForestModule, () => WizardDiscoveries01435.RegisterForest(cache), report);
            RegisterModule(WizardDiscoveries01435.ResilientModule, () => WizardDiscoveries01435.RegisterResilient(cache), report);
            RegisterModule(TimeStutter01437.ModuleName, () => TimeStutter01437.Register(cache), report);
            RegisterModule(Invoker01437.ModuleName, () => Invoker01437.Register(cache), report);
            RegisterModule(TimeStop01438.ModuleName, () => TimeStop01438.Register(cache), report);
            RegisterModule(IncreasedDamageReduction01439.ModuleName, () => IncreasedDamageReduction01439.Register(cache), report);
            RegisterModule(WarDrummer01441.ModuleName, () => WarDrummer01441.Register(cache), report);
            RegisterModule(Sunsinger01443.ModuleName, () => Sunsinger01443.Register(cache), report);
            RegisterModule(UrbanSkald01443.ModuleName, () => UrbanSkald01443.Register(cache), report);
            RegisterModule(RogueSwashbuckler01443.ModuleName, () => RogueSwashbuckler01443.Register(cache), report);
            RegisterModule(HedgeWitch01443.ModuleName, () => HedgeWitch01443.Register(cache), report);
            RegisterModule(Havocker0020.ModuleName, () => Havocker0020.Register(cache), report);
            RegisterModule(PrimalSorcerer01444.ModuleName, () => PrimalSorcerer01444.Register(cache), report);
            RegisterModule(Rake01444.ModuleName, () => Rake01444.Register(cache), report);

            RegisterModule(BardMasterpieces01435.ClamorModule, () => BardMasterpieces01435.RegisterClamor(cache), report);
            RegisterModule(BardMasterpieces01435.DanceModule, () => BardMasterpieces01435.RegisterDance(cache), report);
            RegisterModule(BardMasterpieces01435.SymphonyModule, () => BardMasterpieces01435.RegisterSymphony(cache), report);
            RegisterModule(PsychicAmplifications.ModuleName, () => {
                if(!PsychicCasting.RuntimePatchesAvailable) throw new InvalidOperationException("Psychic runtime patches are unavailable; registration skipped.");
                PsychicAmplifications.Register(cache);
            }, report);
            // Terror Weaver removes amplification choices at11/19 and reuses
            // Expansion's Aura of Doom. Both producers must publish first.
            RegisterModule(TerrorWeaver01444.ModuleName, () => TerrorWeaver01444.Register(cache), report);
            RegisterModule(CourtBard01445.ModuleName, () => CourtBard01445.Register(cache), report);
            RegisterModule(NatureBondedMagus01445.ModuleName, () => NatureBondedMagus01445.Register(cache), report);
            RegisterModule(SageCounselor01445.ModuleName, () => SageCounselor01445.Register(cache), report);
            RegisterModule(GraveWarden01445.ModuleName, () => GraveWarden01445.Register(cache), report);
            RegisterModule(DrillSergeant01445.ModuleName, () => DrillSergeant01445.Register(cache), report);
            RegisterModule(BloodlineMutations01446.ModuleName, () => BloodlineMutations01446.Register(cache), report);
            RegisterModule(Admixture01446.ModuleName, () => Admixture01446.Register(cache), report);
            RegisterModule(Overseer01447.ModuleName, () => Overseer01447.Register(cache), report);
            RegisterModule(SpellSpecialist01447.ModuleName, () => SpellSpecialist01447.Register(cache), report);
            RegisterModule(MagaambyanTelepath01447.ModuleName, () => MagaambyanTelepath01447.Register(cache), report);
            RegisterModule(Phantasm01447.ModuleName, () => Phantasm01447.Register(cache), report);
            RegisterModule(InfectiousCharms01447.ModuleName, () => InfectiousCharms01447.Register(cache), report);
            RegisterModule(Mindblade01454.ModuleName, () => {
                if(!PsychicCasting.RuntimePatchesAvailable)throw new InvalidOperationException("Psychic runtime patches are unavailable; Mindblade registration skipped.");
                Mindblade01454.Register(cache);
            }, report);
            RegisterModule(Skirnir01454.ModuleName, () => Skirnir01454.Register(cache), report);
            RegisterAvailableModule(WhipHurricane01454.ModuleName, () => {
                if(!WhipHurricane01454.HasDependency(cache))return false;
                WhipHurricane01454.Register(cache);return true;
            }, report);
            RegisterOptional(cache, report);
            RegisterModule(MutationMind01457.ModuleName, () => MutationMind01457.Register(cache), report);
            RegisterModule(PsychicMarauder01457.ModuleName, () => PsychicMarauder01457.Register(cache), report);
            RegisterModule(PsychicClassCompletion01458.ModuleName, () => PsychicClassCompletion01458.Register(cache), report);
            RegisterModule(FeralHunter01461.ModuleName, () => FeralHunter01461.Register(cache), report);
            RegisterModule(TotemBonded01461.ModuleName, () => TotemBonded01461.Register(cache), report);
            RegisterAvailableModule(UrbanBloodrager01463.ModuleName, () => UrbanBloodrager01463.Register(cache), report);
            RegisterAvailableModule(PsychicDisciplineBridge01458.ModuleName, () => PsychicDisciplineBridge01458.Register(cache), report);
            RegisterAvailableModule(Amnesiac01464.ModuleName, () => Amnesiac01464.Register(cache), report);
            RegisterAvailableModule(EsotericStarseeker0081.ModuleName, () => EsotericStarseeker0081.Register(cache), report);
            RegisterAvailableModule(SpiritWhisperer0117.ModuleName, () => SpiritWhisperer0117.Register(cache), report);
            RegisterAvailableModule(LoreSpirit0118.ModuleName, () => LoreSpirit0118.Register(cache), report);
            RegisterAvailableModule(SpiritGuide0060.ModuleName, () => SpiritGuide0060.Register(cache), report);
        }

        internal static void RegisterOptional(BlueprintsCache cache, Action<string, bool, Exception> report)
        {
            RegisterAvailableModule(Mindblade01454.BlendingModule, () => Mindblade01454.OptionalBlending(cache), report);
            RegisterAvailableModule(WhipHurricane01454.ModuleName, () => {
                if(!WhipHurricane01454.HasDependency(cache))return false;
                WhipHurricane01454.Register(cache);return true;
            }, report);
            if(cache.Load(PsychicAssets.Id(Mindblade01454.RootToken))!=null)RegisterModule(Mindblade01454.ModuleName, () => Mindblade01454.Register(cache), report);
            if(cache.Load(PsychicAssets.Id(Skirnir01454.RootToken))!=null)RegisterModule(Skirnir01454.ModuleName, () => Skirnir01454.Register(cache), report);
            if(cache.Load(PsychicAssets.Id(InfectiousCharms01447.RootToken))!=null) RegisterModule(InfectiousCharms01447.ModuleName, () => InfectiousCharms01447.Reconcile(cache), report);
            if(cache.Load(PsychicAssets.Id(BloodlineMutations01446.RootToken))!=null) RegisterModule(BloodlineMutations01446.ModuleName, () => BloodlineMutations01446.Register(cache), report);
            if (cache.Load(PsychicAssets.Id(Sunsinger01443.RootToken)) != null) RegisterModule(Sunsinger01443.ModuleName, () => Sunsinger01443.Register(cache), report);
            if (cache.Load(PsychicAssets.Id(UrbanSkald01443.RootToken)) != null) RegisterModule(UrbanSkald01443.ModuleName, () => UrbanSkald01443.Register(cache), report);
            if (cache.Load(PsychicAssets.Id(RogueSwashbuckler01443.RootToken)) != null) RegisterModule(RogueSwashbuckler01443.ModuleName, () => RogueSwashbuckler01443.Register(cache), report);
            if (cache.Load(PsychicAssets.Id(TimeStop01438.RootToken)) != null)
                RegisterModule(TimeStop01438.ModuleName, () => TimeStop01438.RegisterOptional(cache), report);
            if (cache.Load(PsychicAssets.Id(TimeStutter01437.RootToken)) != null)
                RegisterModule(TimeStutter01437.ModuleName, () => TimeStutter01437.RegisterOptional(cache), report);
            if (cache.Load(PsychicAssets.Id(Invoker01437.RootToken)) != null)
                RegisterModule(Invoker01437.ModuleName, () => Invoker01437.RegisterOptional(cache), report);
            if (cache.Load(PsychicAssets.Id(WizardDiscoveries01435.ForestToken)) != null)
                RegisterModule(WizardDiscoveries01435.ForestModule, () => WizardDiscoveries01435.RegisterForest(cache), report);
            if (cache.Load(PsychicAssets.Id(WizardDiscoveries01435.ResilientToken)) != null)
                RegisterModule(WizardDiscoveries01435.ResilientModule, () => WizardDiscoveries01435.RegisterResilient(cache), report);
            if (cache.Load(PsychicAssets.Id(BardMasterpieces01435.ClamorToken)) != null)
                RegisterModule(BardMasterpieces01435.ClamorModule, () => BardMasterpieces01435.RegisterClamor(cache), report);
            if (cache.Load(PsychicAssets.Id(BardMasterpieces01435.DanceToken)) != null)
                RegisterModule(BardMasterpieces01435.DanceModule, () => BardMasterpieces01435.RegisterDance(cache), report);
            if (cache.Load(PsychicAssets.Id(BardMasterpieces01435.SymphonyToken)) != null)
                RegisterModule(BardMasterpieces01435.SymphonyModule, () => BardMasterpieces01435.RegisterSymphony(cache), report);
            if (cache.Load(PsychicAssets.Id("SuperstitionRagePower")) != null)
                RegisterModule(SuperstitionRage01434.SuperstitionModule, () => SuperstitionRage01434.RegisterSuperstition(cache), report);
            if (cache.Load(PsychicAssets.Id("WitchHunterRagePower")) != null)
                RegisterModule(SuperstitionRage01434.WitchHunterModule, () => SuperstitionRage01434.RegisterWitchHunter(cache), report);
            if (cache.Load(PsychicAssets.Id("GhostRagerRagePower")) != null)
                RegisterModule(AntimagicRage01434.GhostModule, () => AntimagicRage01434.RegisterGhostRager(cache), report);
            if (cache.Load(PsychicAssets.Id("DisruptiveRagePower")) != null)
                RegisterModule(AntimagicRage01434.DisruptiveModule, () => AntimagicRage01434.RegisterDisruptive(cache), report);
            if (cache.Load(PsychicAssets.Id("SpellbreakerRagePower")) != null)
                RegisterModule(AntimagicRage01434.SpellbreakerModule, () => AntimagicRage01434.RegisterSpellbreaker(cache), report);
            if (cache.Load(PsychicAssets.Id(SpiritualGuardian01433.Token)) != null)
                RegisterModule(SpiritualGuardian01433.ModuleName, () => SpiritualGuardian01433.Register(cache), report);
            if (cache.Load(PsychicAssets.Id(KineticInvocation01433.Token)) != null)
                RegisterModule(KineticInvocation01433.ModuleName, () => KineticInvocation01433.Register(cache), report);
            if (cache.Load(PsychicAssets.Id(KineticUtility01433.WindsightToken)) != null)
                RegisterModule(KineticUtility01433.WindsightModule, () => KineticUtility01433.RegisterWindsight(cache), report);
            if (cache.Load(PsychicAssets.Id(KineticUtility01433.ColdSnapToken)) != null)
                RegisterModule(KineticUtility01433.ColdSnapModule, () => KineticUtility01433.RegisterColdSnap(cache), report);
            if (cache.Load(PsychicAssets.Id(KineticUtility01433.SmokeStormToken)) != null)
                RegisterModule(KineticUtility01433.SmokeStormModule, () => KineticUtility01433.RegisterSmokeStorm(cache), report);
            if (cache.Load(PsychicAssets.Id(FerociousBeast01431.BaseToken)) != null)
                RegisterModule(FerociousBeast01431.ModuleName, () => FerociousBeast01431.Optional(cache), report);
            if (cache.Load(PsychicAssets.Id(WizardSubschools01431.UndeadToken)) != null)
                RegisterModule(WizardSubschools01431.ModuleName, () => WizardSubschools01431.Optional(cache), report);
            if (cache.Load(PsychicAssets.Id(WizardSubschools01428.CrafterToken)) != null)
                RegisterModule(WizardSubschools01428.ModuleName, () => WizardSubschools01428.Optional(cache), report);
            if (cache.Load(PsychicAssets.Id(ReapingStalker.Token)) != null)
                RegisterModule(ReapingStalker.ModuleName, () => ReapingStalker.Optional(cache), report);
            if (cache.Load(PsychicAssets.Id(RogueFeatTalent.Token)) != null)
                RegisterModule(RogueFeatTalent.ModuleName, () => RogueFeatTalent.Optional(cache), report);
            if (cache.Load(PsychicAssets.Id(TerrifyingHowl.Token)) != null)
                RegisterModule(TerrifyingHowl.ModuleName, () => TerrifyingHowl.Optional(cache), report);
            RegisterModule(MonkKiPowers01427.ModuleName, () => MonkKiPowers01427.Register(cache), report);
            RegisterAvailableModule(ExtraInvestigatorTalent.ModuleName, () => ExtraInvestigatorTalent.Register(cache), report);
            if (ProviderResourceFeats.Available(cache))
                RegisterModule(ProviderResourceFeats.ModuleName, () => ProviderResourceFeats.Register(cache), report);
            if (cache.Load(PsychicAssets.Id(ChosenWeaponFeats.Tokens[0])) is BlueprintFeature chosen &&
                chosen.GetComponent<PrerequisiteChosenWeapon>() != null)
                RegisterModule("Combat feat pool repair", () => CombatFeatPoolRepair.Reconcile(cache), report);
            // Training providers can load late even when no Brawler mod is installed.
            if (cache.Load(PsychicAssets.Id(T11TrainingOptions.FeatTokens[0])) != null)
                RegisterModule("Advanced training integration", () => T11TrainingOptions.Optional(cache), report);
            if (cache.Load(BlueprintGuid.Parse(PsychicClass.ListId)) != null)
                RegisterModule(PsychicSpellIntegration.ModuleName, () => PsychicSpellIntegration.Register(cache), report);
            if (cache.Load(BlueprintGuid.Parse(BrawlerClassId)) != null ||
                cache.Load(BlueprintGuid.Parse(BrawlerCombatFeatSelectionId)) != null)
                RegisterModule("Brawler combat feat integration", () => { RegisterBrawlerCombatFeat(cache); T09CombatFeats.Optional(cache); DevastatingStrikeFeat.Optional(cache); FeralGraceFeat.Optional(cache); ChosenWeaponFeats.Optional(cache); }, report);
            if (cache.Load(BlueprintGuid.Parse(ExemplarInspireCourage.ArchetypeId)) != null)
            {
                RegisterModule(ExemplarInspireCourage.ModuleName, () => ExemplarInspireCourage.Register(cache), report);
                RegisterModule(ExemplarPerformanceResource.ModuleName, () => ExemplarPerformanceResource.Register(cache), report);
            }
            if (cache.Load(PsychicAssets.Id(DrillSergeant01445.RootToken)) != null)
                RegisterModule(DrillSergeant01445.ModuleName, () => DrillSergeant01445.Register(cache), report);
            if(cache.Load(PsychicAssets.Id("PainDiscipline01458"))!=null)RegisterAvailableModule(PsychicDisciplineBridge01458.ModuleName, () => PsychicDisciplineBridge01458.Register(cache), report);
            if(cache.Load(PsychicAssets.Id(Amnesiac01464.RootToken))!=null)RegisterAvailableModule(Amnesiac01464.ModuleName, () => Amnesiac01464.Register(cache), report);
            RegisterAvailableModule(EsotericStarseeker0081.ModuleName, () => EsotericStarseeker0081.Register(cache), report);
            RegisterAvailableModule(SpiritWhisperer0117.ModuleName, () => SpiritWhisperer0117.Register(cache), report);
            RegisterAvailableModule(LoreSpirit0118.ModuleName, () => LoreSpirit0118.Register(cache), report);
            RegisterAvailableModule(SpiritGuide0060.ModuleName, () => SpiritGuide0060.Register(cache), report);
            if(cache.Load(PsychicAssets.Id(SacredServant0110.RootToken))!=null)RegisterAvailableModule(SacredServant0110.ReconcileModuleName, () => SacredServant0110.Reconcile(cache), report);
        }

        private static void RegisterAvailableModule(string name, Func<bool> register, Action<string, bool, Exception> report)
        {
            if (initialPublication != null) { initialPublication.RunAvailable(name, register, report); return; }
            try { if (register()) report(name, true, null); }
            catch (Exception error) { report(name, false, error); }
        }

        private static void RegisterModule(string name, Action register, Action<string, bool, Exception> report)
        {
            if (initialPublication != null) { initialPublication.Run(name, register, report); return; }
            Exception failure = null;
            try { register(); }
            catch (Exception exception) { failure = exception; }
            report(name, failure == null, failure);
        }

        private static FieldInfo RequiredField(Type type, string name)
            => AccessTools.Field(type, name)
               ?? throw new MissingFieldException(type.FullName, name);

        internal static bool CacheIsReady(BlueprintsCache cache)
            => RequiredField(typeof(BlueprintsCache), "m_PackSerializer").GetValue(cache) != null;

        internal static void Register(BlueprintsCache cache)
            => RegisterFeature(cache, FeatureId, FeatureName, DisplayNameKey, DescriptionKey, IronWillId,
                () => new BlueprintComponent[] {
                    new SteadfastPersonalityComponent { name = "SteadfastPersonalitySaveReplacement" }
                });

        internal static void RegisterExtendedBane(BlueprintsCache cache)
        {
            var resource = cache.Load(BlueprintGuid.Parse(BaneResourceId)) as BlueprintAbilityResource
                ?? throw new InvalidOperationException("Inquisitor Bane resource was not found.");
            var prerequisite = cache.Load(BlueprintGuid.Parse(BaneFeatureId)) as BlueprintFeature
                ?? throw new InvalidOperationException("Inquisitor Bane prerequisite was not found.");
            var prerequisiteField = AccessTools.Field(typeof(PrerequisiteFeature), "m_Feature")
                ?? throw new MissingFieldException(typeof(PrerequisiteFeature).FullName, "m_Feature");
            RegisterFeature(cache, ExtendedBaneId, "CotWWrathPrototypeExtendedBane",
                ExtendedBaneNameKey, ExtendedBaneDescriptionKey, ExtraBaneId, () => {
                    var requirement = new PrerequisiteFeature { name = "ExtendedBaneRequiresBane" };
                    prerequisiteField.SetValue(requirement, BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(prerequisite));
                    return new BlueprintComponent[] {
                        requirement,
                        new ExtendedBaneComponent {
                            name = "ExtendedBaneWisdomResourceBonus",
                            Resource = BlueprintReferenceBase.CreateTyped<BlueprintAbilityResourceReference>(resource)
                        }
                    };
                });
        }

        internal static void RegisterImprovedCombatExpertise(BlueprintsCache cache)
        {
            var combatExpertise = cache.Load(BlueprintGuid.Parse(CombatExpertiseId)) as BlueprintFeature
                ?? throw new InvalidOperationException("Combat Expertise prerequisite was not found.");
            var property = cache.Load(BlueprintGuid.Parse(CombatExpertisePenaltyPropertyId)) as BlueprintUnitProperty
                ?? throw new InvalidOperationException("Combat Expertise attack penalty property was not found.");
            var component = property.ComponentsArray.OfType<BaseAttackPropertyWithFeatureList>().SingleOrDefault()
                ?? throw new InvalidOperationException("Combat Expertise penalty component was not found or is ambiguous.");
            if (component.FeatureBonus != -1 || component.BaseValue != 1 ||
                component.BaseAttackDiv != 4 || component.BaseAttackZero != 0)
                throw new InvalidOperationException("Combat Expertise penalty calculation differs from the verified game version.");

            // The native property counts each matching reference separately. Two
            // references to our rank-one feat therefore reduce its penalty by two.
            var featuresField = RequiredField(typeof(BaseAttackPropertyWithFeatureList), "m_Features");
            var previous = (BlueprintFeatureReference[])featuresField.GetValue(component)
                ?? Array.Empty<BlueprintFeatureReference>();
            int present = previous.Count(item => item != null && item.Guid == BlueprintGuid.Parse(ImprovedCombatExpertiseId));
            if (present > 2)
                throw new InvalidOperationException("Improved Combat Expertise has too many entries in the native penalty property.");
            var reference = new BlueprintFeatureReference();
            reference.ReadGuidFromJson(ImprovedCombatExpertiseId);
            var updated = previous.Concat(Enumerable.Repeat(reference, 2 - present)).ToArray();
            var prerequisiteField = RequiredField(typeof(PrerequisiteFeature), "m_Feature");
            var requirement = new PrerequisiteFeature { name = "ImprovedCombatExpertiseRequiresCombatExpertise" };
            prerequisiteField.SetValue(requirement, BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(combatExpertise));

            featuresField.SetValue(component, updated);
            try
            {
                RegisterFeature(cache, ImprovedCombatExpertiseId, "CotWWrathPrototypeImprovedCombatExpertise",
                    ImprovedCombatExpertiseNameKey, ImprovedCombatExpertiseDescriptionKey, CombatExpertiseId,
                    () => new BlueprintComponent[] {
                        new PrerequisiteStatValue { name = "ImprovedCombatExpertiseIntelligence13", Stat = StatType.Intelligence, Value = 13 },
                        new PrerequisiteStatValue { name = "ImprovedCombatExpertiseBaseAttack8", Stat = StatType.BaseAttackBonus, Value = 8 },
                        requirement
                    }, NativeCombatFeatSelectionIds,
                    new[] { FeatureGroup.Feat, FeatureGroup.CombatFeat });
            }
            catch
            {
                featuresField.SetValue(component, previous);
                throw;
            }
        }

        internal static void RegisterBrawlerCombatFeat(BlueprintsCache cache)
        {
            var brawlerClass = cache.Load(BlueprintGuid.Parse(BrawlerClassId)) as BlueprintCharacterClass
                ?? throw new InvalidOperationException("Brawler class blueprint was not found.");
            if (brawlerClass.name != "RMBrawlerHybridClass")
                throw new InvalidOperationException("Brawler class GUID belongs to an unexpected blueprint.");
            var selection = cache.Load(BlueprintGuid.Parse(BrawlerCombatFeatSelectionId)) as BlueprintFeatureSelection
                ?? throw new InvalidOperationException("Brawler combat feat selection was not found.");
            if (selection.name != "RMBrawlerCombatFeat")
                throw new InvalidOperationException("Brawler combat feat GUID belongs to an unexpected blueprint.");
            var progressionReference = (BlueprintProgressionReference)RequiredField(typeof(BlueprintCharacterClass),
                "m_Progression").GetValue(brawlerClass);
            var progression = progressionReference == null ? null
                : cache.Load(progressionReference.Guid) as BlueprintProgression;
            var levelFeaturesField = RequiredField(typeof(LevelEntry), "m_Features");
            if (progression == null || progression.name != "RMBrawlerProgression" ||
                !progression.LevelEntries.Any(level =>
                    ((System.Collections.Generic.List<BlueprintFeatureBaseReference>)levelFeaturesField.GetValue(level))
                    .Any(feature => feature != null && feature.Guid == selection.AssetGuid)))
                throw new InvalidOperationException("Brawler class has not completed its combat feat progression.");
            var feats = new System.Collections.Generic.List<BlueprintFeature>();
            foreach (var entry in new[] {
                new[] { ImprovedCombatExpertiseId, "CotWWrathPrototypeImprovedCombatExpertise" },
                new[] { ShieldedMage.Id, ShieldedMage.Name }
            }) {
                var asset = cache.Load(BlueprintGuid.Parse(entry[0]));
                if (asset == null) continue;
                var feat = asset as BlueprintFeature;
                if (feat == null || feat.name != entry[1])
                    throw new InvalidOperationException("Combat feat GUID belongs to an unexpected blueprint: " + entry[0]);
                ContentIdentity.Validate(feat, entry[0] == ShieldedMage.Id
                    ? new BlueprintComponent[] { new Kingmaker.Designers.Mechanics.Facts.ArcaneSpellFailureIncrease { name = "ShieldedMageShieldFailureReduction" } }
                    : new BlueprintComponent[] { new PrerequisiteFeature { name = "ImprovedCombatExpertiseRequiresCombatExpertise" } });
                feats.Add(feat);
            }
            if (feats.Count == 0) throw new InvalidOperationException("No prototype combat feats are available for Brawler integration.");
            var featuresField = RequiredField(typeof(BlueprintFeatureSelection), "m_Features");
            var allFeaturesField = RequiredField(typeof(BlueprintFeatureSelection), "m_AllFeatures");
            var oldFeatures = (BlueprintFeatureReference[])featuresField.GetValue(selection);
            var oldAllFeatures = (BlueprintFeatureReference[])allFeaturesField.GetValue(selection);
            try
            {
                var updatedFeatures = oldFeatures;
                var updatedAllFeatures = oldAllFeatures;
                foreach (var feat in feats) {
                    var reference = BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(feat);
                    updatedFeatures = AppendIfMissing(updatedFeatures, reference);
                    updatedAllFeatures = AppendIfMissing(updatedAllFeatures, reference);
                }
                featuresField.SetValue(selection, updatedFeatures);
                allFeaturesField.SetValue(selection, updatedAllFeatures);
            }
            catch
            {
                featuresField.SetValue(selection, oldFeatures);
                allFeaturesField.SetValue(selection, oldAllFeatures);
                throw;
            }
        }

        internal static void RegisterFeature(BlueprintsCache cache, string featureId, string featureName,
            string displayKey, string descriptionKey, string iconId, Func<BlueprintComponent[]> components,
            string[] additionalSelectionIds = null, FeatureGroup[] groups = null)
        {
            // Resolve every dependency and field before modifying the shared selection.
            var selections = new[] { BasicFeatsId }.Concat(additionalSelectionIds ?? Array.Empty<string>())
                .Distinct().Select(id => cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection
                    ?? throw new InvalidOperationException("Feat selection was not found: " + id)).ToArray();
            var iconSource = cache.Load(BlueprintGuid.Parse(iconId)) as BlueprintFeature
                ?? throw new InvalidOperationException("Feat icon dependency was not found: " + iconId);
            var displayField = RequiredField(typeof(BlueprintUnitFact), "m_DisplayName");
            var descriptionField = RequiredField(typeof(BlueprintUnitFact), "m_Description");
            var iconField = RequiredField(typeof(BlueprintUnitFact), "m_Icon");
            var featuresField = RequiredField(typeof(BlueprintFeatureSelection), "m_Features");
            var allFeaturesField = RequiredField(typeof(BlueprintFeatureSelection), "m_AllFeatures");
            var oldFeatures = selections.Select(item => (BlueprintFeatureReference[])featuresField.GetValue(item)).ToArray();
            var oldAllFeatures = selections.Select(item => (BlueprintFeatureReference[])allFeaturesField.GetValue(item)).ToArray();
            var guid = BlueprintGuid.Parse(featureId);
            var existing = cache.Load(guid);
            if (existing != null && (existing.name != featureName || !(existing is BlueprintFeature)))
                throw new InvalidOperationException("Prototype blueprint GUID is already used by another asset: " + featureId);

            var feature = existing as BlueprintFeature;
            if (feature != null) ContentIdentity.Validate(feature, components());
            bool created = feature == null;
            if (created)
            {
                feature = new BlueprintFeature
                {
                    name = featureName,
                    AssetGuid = guid,
                    Groups = groups ?? new[] { FeatureGroup.Feat },
                    Ranks = 1,
                    IsClassFeature = false,
                    ComponentsArray = components()
                };
                displayField.SetValue(feature, new LocalizedString { Key = displayKey });
                descriptionField.SetValue(feature, new LocalizedString { Key = descriptionKey });
                iconField.SetValue(feature, iconSource.Icon);
                feature.OnEnable();
            }

            // Both arrays are updated independently: preserve all existing mods' entries.
            var reference = BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(feature);
            try
            {
                if (created) cache.AddCachedBlueprint(guid, feature);
                for (int i = 0; i < selections.Length; i++)
                {
                    featuresField.SetValue(selections[i], AppendIfMissing(oldFeatures[i], reference));
                    allFeaturesField.SetValue(selections[i], AppendIfMissing(oldAllFeatures[i], reference));
                }
            }
            catch
            {
                for (int i = 0; i < selections.Length; i++)
                {
                    featuresField.SetValue(selections[i], oldFeatures[i]);
                    allFeaturesField.SetValue(selections[i], oldAllFeatures[i]);
                }
                if (created) cache.RemoveCachedBlueprint(guid);
                throw;
            }
        }

        private static BlueprintFeatureReference[] AppendIfMissing(
            BlueprintFeatureReference[] source, BlueprintFeatureReference value)
            => source != null && source.Any(item => item != null && item.Guid == value.Guid)
                ? source
                : (source ?? Array.Empty<BlueprintFeatureReference>()).Concat(new[] { value }).ToArray();
    }
}
