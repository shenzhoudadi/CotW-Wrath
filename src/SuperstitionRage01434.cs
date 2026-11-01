using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class SuperstitionRage01434
    {
        internal const string ModuleName = "Superstition and Witch Hunter rage powers";
        internal const string SuperstitionModule = "Rage power: Superstition", WitchHunterModule = "Rage power: Witch Hunter";
        internal const string SuperstitionToken = "SuperstitionRagePower", WitchHunterToken = "WitchHunterRagePower";
        internal const string SuperstitionEffectToken = "SuperstitionRageEffectBuff", WitchHunterEffectToken = "WitchHunterRageEffectBuff";
        const string SuperstitionText = "While raging, gain a +2 competence bonus on saving throws against spells and spell-like abilities, increasing by 1 every 4 barbarian, skald or primalist levels. You cannot willingly accept the harmless spells identified by the original CotW rules: when another ally casts one on you, make its Will or Fortitude save to resist it. A successful save negates a harmless effect, or halves healing from the listed healing spells. Your own spells, spells without the harmless rule, and non-spell abilities retain their normal rules. Shared rage uses the rage source's class levels; overlapping copies do not stack.";
        const string WitchHunterText = "Requires Superstition. While raging, gain +1 damage against creatures possessing spellcasting or spells or spell-like abilities. This bonus increases by 1 every 4 barbarian, skald or primalist levels. Apply it once to the first damage component of each damage calculation. Alchemist extracts alone do not qualify a target. Shared rage uses the rage source's class levels; overlapping copies use the strongest bonus.";
        internal static void Register(BlueprintsCache cache) { RegisterSuperstition(cache); RegisterWitchHunter(cache); }
        internal static void Optional(BlueprintsCache cache) { RagePowerMigration01434.Optional(cache, SuperstitionToken); RagePowerMigration01434.Optional(cache, WitchHunterToken); }
        internal static void RegisterSuperstition(BlueprintsCache cache) {
            if (RagePowerMigration01434.Restore(cache, SuperstitionToken)) return;
            var f = new PsychicAssets(cache);
            var root = RagePowerMigration01434.Root(f, SuperstitionToken, "Superstition", SuperstitionText, 1);
            var buff = f.Buff(SuperstitionEffectToken, "Superstition", SuperstitionText,
                RagePowerMigration01434.Rank(f), new SuperstitionDefense01434(),
                new SavingThrowBonusAgainstAbilityType { AbilityType = AbilityType.Spell, ModifierDescriptor = ModifierDescriptor.Competence, Bonus = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default } },
                new SavingThrowBonusAgainstAbilityType { AbilityType = AbilityType.SpellLike, ModifierDescriptor = ModifierDescriptor.Competence, Bonus = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default } });
            buff.Stacking = StackingType.Stack; SetEnum(buff, "m_Flags", "HiddenInUi");
            var icon = Get(f.Native<BlueprintAbility>("0a5ddfbcfb3989543ac7c936fc256889"), "m_Icon"); Set(root, "m_Icon", icon); Set(buff, "m_Icon", icon);
            RagePowerMigration01434.Commit(f, root, buff);
        }
        internal static void RegisterWitchHunter(BlueprintsCache cache) {
            if (RagePowerMigration01434.Restore(cache, WitchHunterToken)) return;
            var f = new PsychicAssets(cache);
            var prerequisite = f.Native<BlueprintFeature>(Id(SuperstitionToken).ToString());
            if (prerequisite.name != "CotWWrathPsychic" + SuperstitionToken) throw new InvalidOperationException("01434 Superstition prerequisite identity changed.");
            var root = RagePowerMigration01434.Root(f, WitchHunterToken, "Witch Hunter", WitchHunterText, 1, Migration01426.Require(prerequisite));
            var buff = f.Buff(WitchHunterEffectToken, "Witch Hunter", WitchHunterText,
                RagePowerMigration01434.Rank(f, 1), new WitchHunterDamage01434 { Value = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default } });
            buff.Stacking = StackingType.Stack; SetEnum(buff, "m_Flags", "HiddenInUi");
            var icon = Get(f.Native<Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility>("ce0ece459ebed9941bb096f559f36fa8"), "m_Icon"); Set(root, "m_Icon", icon); Set(buff, "m_Icon", icon);
            RagePowerMigration01434.Commit(f, root, buff);
        }
        // Original CotW HarmlessSpells lists, including the four duplicate Will
        // rows. All 154 rows still identify native blueprints in current Wrath.
        // They are queried by identity; no shared spell blueprint is rewritten.
        internal static readonly string[] WillNegates = {
            "0082c3ed87626204f9b46d84cec1518d", // GoodHopeTrailblazer
            "8b9916170be016a49a94cc41634b3a24", // GoodHopeTrollLair
            "7bc8e27cba24f0e43ae64ed201ad5785", // Resistance
            "c3a8f31778c3980498d8f00c980be5f5", // Guidance
            "9e1ad5d6f87d19e4d8883d63a6e35568", // MageArmor
            "403cf599412299a4f9d5d925c7b9fb33", // MagicFang
            "1eaf1020e82028d4db55e6e464269e00", // ProtectionFromChaos
            "b70104f09b3da794da923fbf248befc5", // ProtectionFromChaosChaosEvil
            "c28f7234f5fb8c943a77621ad96ad8f9", // ProtectionFromChaosEvil
            "eee384c813b6d74498d1b9cc720d61f4", // ProtectionFromEvil
            "07dccc8e4c4489c4d9de721dddaf12cc", // ProtectionFromEvilChaosEvil
            "2ac7637daeb2aa143a3bae860095b63e", // ProtectionFromGood
            "c3aafbbb6e8fc754fb8c82ede3280051", // ProtectionFromLaw
            "55a037e514c0ee14a8e3ed14b47061de", // RemoveFear
            "183d5bb91dea3a1489a6db6c9cb64445", // ShieldOfFaith
            "f001c73999fb5a543a199f890108d936", // Vanish
            "a900628aea19aa74aad0ece0e65d091a", // BearsEndurance
            "042aaa117e89c4d4b8cb41478dd3fca3", // BestowGrace
            "c36c1d11771b0584f8e100b92ee5475b", // BlessingOfCourageAndLife
            "061041f379a5ced4090e49ac0e69cc4e", // BlessingOfLuckAndResolve
            "14ec7a4e52e90fa47a4c8d63c69fd5c1", // Blur
            "4c3d08935262b6544ae97599b3a9556d", // BullsStrength
            "446f7bf201dc1934f96ac0a26e324803", // EaglesSplendor
            "ae4d3ad6a8fda1542acf2e9bbc13d113", // FoxsCunning
            "89940cde01689fb46946b2f8cd7b66b7", // Invisibility
            "dde924776bd7ad247928b5efbfacfbdd", // NaturalRythm
            "f0455c9295b53904f9e02fc571dd2ce1", // OwlsWisdom
            "c28de1f98a3f432448e52e5d47c73208", // ProtectionFromArrows
            "f8bce986adfc88544a42bf4ab7ae75b2", // RemoveParalysis
            "e84fc922ccf952943b5240293669b171", // RestorationLesser
            "903092f6488f9ce45a80943923576ab3", // Displacement
            "a5e23522eda32dc45801e32c05dc9f96", // GoodHope
            "5ab0d42fb68c9e34abae4921822b9d63", // Heroism
            "956309af83352714aa7ee89fb4ecf201", // MagicalVestmentArmor
            "adcda176d1756eb45bd5ec9592073b09", // MagicalVestmentShield
            "f1100650705a69c4384d3edd88ba0f52", // MagicFangGreater
            "e7240516af4241b42b2cd819929ea9da", // NeutralizePoison
            "b48674cef2bff5e478a007cf57d8345b", // RemoveCurse
            "3cf5b2bd093a36a468b4ece38ad4d5fa", // BestowGraceOfTheChampion
            "462c21cebf7820c40a87f5e4d03e17cf", // BlessingOfLuckAndResolveMass
            "a26c23a887a6f154491dc2cefdad2c35", // CrusadersEdge
            "0413915f355a38146bc6ad40cdf27b3f", // DeathWard
            "4c349361d720e844e846ad8c19959b1e", // FreedomOfMovement
            "f2115ac1148256b4ba20788f7e966830", // Restoration
            "c66e86905f7606c4eaa5c774f0357b2b", // Stoneskin
            "56923211d2ac95e43b8ac5031bab74d8", // AnimalGrowth
            "6ab3056345b78164fa34b2c947b52083", // DisruptingWeapon
            "963be80e4c1b3734ab6b276659d834c4", // PolymorphAnimal
            "c5e2e73f089c76f42a34ae829aa2d4ce", // PolymorphElementalAir
            "0ecb7ea0c8b4ef54c879ad5737acb080", // PolymorphElementalEarth
            "b7acdfe077f32004c9c5938b079d5e5e", // PolymorphElementalFire
            "256a21630b956cf48917a8413c745a41", // PolymorphElementalWater
            "0a5ddfbcfb3989543ac7c936fc256889", // SpellResistance
            "7c5d556b9a5883048bf030e20daebe31", // StoneskinCommunal
            "f6bcea6db14f0814d99b54856e918b92", // BearsEnduranceMass
            "a5c56f0f699daec44b7aedd8b273b08a", // BrilliantInspiration
            "6a234c6dcde7ae94e94e9c36fd1163a7", // BullsStrengthMass
            "1f6c94d56f178b84ead4c02f1b1e1c48", // CatsGraceMass
            "2caa607eadda4ab44934c5c9875e01bc", // EaglesSplendorMass
            "2b24159ad9907a8499c2313ba9c0f615", // FoxsCunningMass
            "e15e5e7045fda2244b98c8f010adfe31", // HeroismGreater
            "9f5ada581af3db4419b54db77f44e430", // OwlsWisdomMass
            "b3da3fbee6a751d4197e446c7e852bcb", // TrueSeeing
            "98734a2665c18cd4db71878b0532024a", // Firebrand
            "1879c6d863758c54085d3e5bcafa26f4", // PolymorphGreaterDragonBlack
            "07e1a623d980cc8458f7f637b21c1b68", // PolymorphGreaterDragonBlue
            "e135ae7588972d144bec027902d1bc22", // PolymorphGreaterDragonBrass
            "f7de833b55f92e243ad045b319b7ba95", // PolymorphGreaterDragonGold
            "9cb55798ee8999542ac49ce0899380cf", // PolymorphGreaterDragonGreen
            "a62c6b235d13b684086d00ee94a5f233", // PolymorphGreaterDragonSilver
            "da424ee2441464d4c87ff91de9cd600b", // PolymorphGreaterElementalAir
            "e9d281e342bbd78479e2cfc9aa48499e", // PolymorphGreaterElementalEarth
            "f4027096eba0bdf4b92c2e712a2e1889", // PolymorphGreaterElementalFire
            "7887dd5d176e3504cae7b1e6d23eff57", // PolymorphGreaterElementalWater
            "223b4f19be8458d44b0110c91250048c", // PolymorphGreaterShamblingMound
            "763430b60405dc645a048b4be22d3f63", // PolymorphGreaterSmilodon
            "39d4eeb58ddb6cc4dacf2e11d345bc5f", // PolymorphGreaterWyvern
            "fafd77c6bfa85c04ba31fdc1c962c914", // RestorationGreater
            "42aa71adc7343714fa92e471baa98d42", // ProtectionFromSpells
            "1f01a098d737ec6419aedc4e7ad61fdd", // Foresight
            "43740dab07286fe4aa00a6ee104ce7c1", // HeroicInvocation
            "f4d181f91680c8e42a7db8b60082bb0a", // InvisibilityAasimar
            "69b7e466a6186d44a88a73f63ffb8dc3", // RestorationLesserAasimar
            "120ffcf4fc599a34e96e307091491764", // BlurTiefling
            "81de55902e7caaf4398a5fbc9fe70123", // InvisibilityTiefling
            "e6d3d5b1ffb576e4d9a4226d36fb3744", // ResistAcidTiefling
            "2317930d7ccac3c469ab6ef130b2cfcc", // ResistColdTiefling
            "2be099877cf9c564788ac3896d272dd7", // ResistElectricityTiefling
            "11ea631b47b65aa4ba0e60c088602cf3", // ResistFireTiefling
            "bbaaf67996fae1c44b7d37073d74c016", // ResistSonicTiefling
            "eee384c813b6d74498d1b9cc720d61f4", // ProtectionFromEvil
            "2ac7637daeb2aa143a3bae860095b63e", // ProtectionFromGood
            "c3aafbbb6e8fc754fb8c82ede3280051", // ProtectionFromLaw
            "1eaf1020e82028d4db55e6e464269e00", // ProtectionFromChaos
            "93f391b0c5a99e04e83bbfbe3bb6db64", // ProtectionFromEvilCommunal
            "5bfd4cce1557d5744914f8f6d85959a4", // ProtectionFromGoodCommunal
            "8b8ccc9763e3cc74bbf5acc9c98557b9", // ProtectionFromLawCommunal
            "0ec75ec95d9e39d47a23610123ba1bad", // ProtectionFromChaosCommunal
        };
        internal static readonly string[] FortNegates = {
            "80e7e30cdf96be0418a615ebb38ea4b9", // Celerity
            "b09bba47d93632b478cace0f57cec527", // CelerityShort
            "c60969e7f264e6d4b84a1499fdcf9039", // EnlargePerson
            "f3c0b267dd17a2a45a40805e31fe3cd1", // FeatherStep
            "4e0e9aba6447d514f88eff1464cc4763", // ReducePerson
            "f6f95242abdfac346befd6f4f6222140", // RemoveSickness
            "b48b4c5ffb4eab0469feba27fc86a023", // DelayPoison
            "04e820e1ce3a66f47a50ad5074d3ae40", // DelayPoisonCommunal
            "d219494150ac1f24f9ce14a3d4f66d26", // FeatherStepMass
            "486eaff58293f6441a5c2759c4872f98", // Haste
            "c927a8b0cd3f5174f8c0b67cdbfde539", // RemoveBlindness
            "4093d5a0eb5cae94e909eb1e0e1a6b36", // RemoveDisease
            "66dc49bf154863148bd217287079245e", // EnlargePersonMass
            "2427f2e3ca22ae54ea7337bbab555b16", // ReducePersonMass
            "9256a86aec14ad14e9497f6b60e26f3f", // BlessingOfTheSalamander
            "fedc77de9b7aad54ebcc43b4daf8decd", // ResistAcid
            "5368cecec375e1845ae07f48cdc09dd1", // ResistCold
            "90987584f54ab7a459c56c2d2f22cee2", // ResistElectricity
            "ddfb4ac970225f34dbff98a10a4a8844", // ResistFire
            "8d3b10f92387c84429ced317b06ad001", // ResistSonic
            "3d77ee3fc4913c44b9df7c5bbcdc4906", // ProtectionFromAcid
            "96c9d98b6a9a7c249b6c4572e4977157", // ProtectionFromArrowsCommunal
            "021d39c8e0eec384ba69140f4875e166", // ProtectionFromCold
            "e24ce0c3e8eaaaf498d3656b534093df", // ProtectionFromElectricity
            "3f9605134d34e1243b096e1f6cb4c148", // ProtectionFromFire
            "0cee375b4e5265a46a13fc269beb8763", // ProtectionFromSonic
            "feb51767db2ae54409e41ec224071819", // ResistAcidCommunal
            "b229c12f1c2db3c48a50beda6c33bde0", // ResistColdCommunal
            "96ec0a066f55ef5458040be1f0929216", // ResistElectricityCommunal
            "832bf98966e72cd478eecc9f8ba829f5", // ResistFireCommunal
            "c9b73a202d818c240b1f46e8e16395f2", // ResistSonicCommunal
            "de4e28c053d936d45b6c9e361f90acc2", // ProtectionFromAcidCommunal
            "3bd50e8acc303d244a1cec9df04ad050", // ProtectionFromColdCommunal
            "f10cd112b876a6f449d52dee0a57e602", // ProtectionFromElectricityCommunal
            "2903d31d6c8356547aa4aae5a3e7a655", // ProtectionFromFireCommunal
            "b065b2f0589225f4897fc8b345acbfb6", // ProtectionFromSonicCommunal
        };
        internal static readonly string[] HealingHalved = {
            "47808d23c67033d4bbab86a1070fd62f", // CureLightWounds
            "e5cb4c4459e437e49a4cd73fde6b9063", // InflictLightWounds
            "1c1ebf5370939a9418da93176cc44cd9", // CureModerateWounds
            "14d749ecacca90a42b6bf1c3f580bb0c", // InflictModerateWounds
            "b0b8a04a3d74e03489862b03f4e467a6", // InflictSeriousWounds
            "6e81a6679a0889a429dec9cedcf3729c", // CureSeriousWounds
            "0d657aa811b310e4bbd8586e60156a2d", // CureCriticalWounds
            "5d3d689392e4ff740a761ef346815074", // CureLightWoundsMass
            "9da37873d79ef0a468f969e4e5116ad2", // InflictLightWoundsMass
            "571221cc141bc21449ae96b3944652aa", // CureModerateWoundsMass
            "03944622fbe04824684ec29ff2cec6a7", // InflictModerateWoundsMass
            "0cea35de4d553cc439ae80b3a8724397", // CureSeriousWoundsMass
            "820170444d4d2a14abc480fcbdb49535", // InflictSeriousWoundsMass
            "1f173a16120359e41a20fc75bb53d449", // CureCriticalWoundsMass
            "5ee395a2423808c4baf342a4f8395b19", // InflictCriticalWoundsMass
            "cbd03c874e39e6c4795fe0093544f2a2", // BreathOfLifeTouch
            "788d72e7713cf90418ee1f38449416dc", // InspiringRecovery
            "ff8f1534f66559c478448723e16b6624", // Heal
            "137af566f68fd9b428e2e12da43c1482", // Harm
            "867524328b54f25488d371214eea0d90", // HealMass
        };
        static readonly HashSet<string> Will = new HashSet<string>(WillNegates, StringComparer.Ordinal);
        static readonly HashSet<string> Fort = new HashSet<string>(FortNegates, StringComparer.Ordinal);
        static readonly HashSet<string> Heals = new HashSet<string>(HealingHalved, StringComparer.Ordinal);
        sealed class SaveState { internal readonly Dictionary<UnitEntityData, bool> Negates = new Dictionary<UnitEntityData, bool>(), Halves = new Dictionary<UnitEntityData, bool>(); }
        sealed class HealState { }
        static readonly ConditionalWeakTable<MechanicsContext, SaveState> Saved = new ConditionalWeakTable<MechanicsContext, SaveState>();
        static readonly ConditionalWeakTable<RuleHealDamage, HealState> Halved = new ConditionalWeakTable<RuleHealDamage, HealState>();
        static readonly Type SaveData = typeof(ContextActionSavingThrow).Assembly.GetType("Kingmaker.UnitLogic.Mechanics.Actions.SavingThrowData", true);
        static readonly PropertyInfo CurrentSave = AccessTools.Property(typeof(ContextData<>).MakeGenericType(SaveData), "Current");
        static readonly PropertyInfo SaveRule = AccessTools.Property(SaveData, "Rule");
        static Buff Active(UnitEntityData target) => target?.Facts?.List.OfType<Buff>().FirstOrDefault(b => b.IsActive && b.IsTurnedOn && !b.IsDisposed && !b.IsSuppressed && b.Blueprint.GetComponent<SuperstitionDefense01434>() != null);
        static bool Eligible(MechanicsContext context, UnitEntityData target, out Buff source) {
            source = null;
            if (context?.Params == null || context.MaybeCaster == null || target == null || target == context.MaybeCaster ||
                context.SourceAbility?.Type != AbilityType.Spell || !target.IsAlly(context.MaybeCaster)) return false;
            source = Active(target); return source != null;
        }
        static bool NativeSaveAlreadyExists(MechanicsContext context, UnitEntityData target) {
            var data = CurrentSave.GetValue(null, null); if (data == null) return false;
            var rule = SaveRule.GetValue(data, null) as RuleSavingThrow;
            return rule != null && rule.Initiator == target && ReferenceEquals(rule.Reason?.Context, context);
        }
        static bool Passed(MechanicsContext context, UnitEntityData target, SavingThrowType type, bool healing) {
            var state = Saved.GetOrCreateValue(context); var values = healing ? state.Halves : state.Negates;
            if (values.TryGetValue(target, out var previous)) return previous;
            var save = context.TriggerRule(new RuleSavingThrow(target, type, context.Params.DC) { Reason = context });
            values.Add(target, save.IsPassed); return save.IsPassed;
        }
        [HarmonyPatch(typeof(AbilityEffectRunAction), nameof(AbilityEffectRunAction.Apply))]
        internal static class HarmlessApplyPatch {
            [HarmonyPrefix]
            internal static bool Prefix(AbilityEffectRunAction __instance, AbilityExecutionContext context, TargetWrapper target) {
                if (__instance == null || __instance.SavingThrowType != SavingThrowType.Unknown || target?.Unit == null || !Eligible(context, target.Unit, out _)) return true;
                string id = context.SourceAbility.AssetGuid.ToString();
                SavingThrowType type = Will.Contains(id) ? SavingThrowType.Will : Fort.Contains(id) ? SavingThrowType.Fortitude : SavingThrowType.Unknown;
                if (type == SavingThrowType.Unknown || NativeSaveAlreadyExists(context, target.Unit)) return true;
                return !Passed(context, target.Unit, type, false);
            }
        }
        [HarmonyPatch(typeof(RuleHealDamage), nameof(RuleHealDamage.OnTrigger))]
        internal static class HarmlessHealPatch {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.Last)]
            internal static void Prefix(RuleHealDamage __instance) {
                var context = __instance?.Reason?.Context;
                if (__instance == null || __instance.IsFake || __instance.Interrupted || Halved.TryGetValue(__instance, out _) ||
                    !Eligible(context, __instance.Target, out var source) || !Heals.Contains(context.SourceAbility.AssetGuid.ToString()) || NativeSaveAlreadyExists(context, __instance.Target)) return;
                if (!Passed(context, __instance.Target, SavingThrowType.Will, true)) return;
                // Native OnTrigger multiplies all final healing by (1-r). Apply
                // half after other handlers, preserving their bonus, Empower and
                // existing healing resistance instead of replacing the action.
                float resistance = __instance.HealResistance;
                __instance.HealResistance.Set(1f - (1f - resistance) * 0.5f, source);
                Halved.Add(__instance, new HealState());
            }
        }
    }
    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("c8f02c2da8104ba2be89bb45aac03401")]
    public sealed class SuperstitionDefense01434 : BlueprintComponent { }

    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("c8f02c2da8104ba2be89bb45aac03402")]
    public sealed class WitchHunterDamage01434 : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateDamage>, IInitiatorRulebookSubscriber
    {
        public ContextValue Value;
        sealed class Applied { }
        static readonly ConditionalWeakTable<RuleCalculateDamage, Applied> Applications = new ConditionalWeakTable<RuleCalculateDamage, Applied>();
        internal static bool SpellUser(UnitEntityData target) {
            if (target?.Descriptor == null) return false;
            foreach (var c in target.Progression.Classes) {
                var book = c.Spellbook;
                if (book != null && !book.IsAlchemist) return true;
            }
            return target.Abilities.Enumerable.Any(a => a.Blueprint.Type == AbilityType.Spell || a.Blueprint.Type == AbilityType.SpellLike);
        }
        public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
            if (evt?.Initiator != Owner || evt.Target == null || evt.DamageBundle.First == null || Applications.TryGetValue(evt, out _) || !SpellUser(evt.Target)) return;
            Buff strongest = null; int amount = 0;
            foreach (var b in Owner.Facts.List.OfType<Buff>()) {
                if (!b.IsActive || !b.IsTurnedOn || b.IsDisposed || b.IsSuppressed) continue;
                var component = b.Blueprint.GetComponent<WitchHunterDamage01434>(); if (component == null) continue;
                int value = component.Value?.Calculate(b.Context) ?? 0;
                if (value > amount) { strongest = b; amount = value; }
            }
            if (!ReferenceEquals(strongest, Fact) || amount <= 0) return;
            evt.DamageBundle.First.AddModifier(amount, Fact); Applications.Add(evt, new Applied());
        }
        public void OnEventDidTrigger(RuleCalculateDamage evt) { }
    }
}
