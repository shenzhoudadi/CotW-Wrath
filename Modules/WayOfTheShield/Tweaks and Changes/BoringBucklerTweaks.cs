using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.ElementsSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityEngine;
using Way_of_the_shield.NewComponents;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Way_of_the_shield
{

    public static class BoringBucklerTweaks
    {

        public class Buckler1h_Component : CanUse2hWeaponAs1hBase, IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>, IInitiatorRulebookHandler<RuleAttackWithWeapon>
        {
            public static BlueprintBuff ShieldForbiddance;
            //static Buckler1h_Component()
            //{
            //    ShieldForbiddance = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("414f40680af64050a2a9dde3dede32ac");
            //   if (ShieldForbiddance is not null) return;
            //    ShieldForbiddance = new()
            //    {
            //        AssetGuid = new BlueprintGuid(new Guid("414f40680af64050a2a9dde3dede32ac")),
            //        name = "WayOfTheShield_ForbidShieldACforOneTurn",
            //        FxOnRemove = new(),
            //        m_Flags = BlueprintBuff.Flags.HiddenInUi
            //   };
            //    ShieldForbiddance.AddComponent(new AddMechanicsFeature() {m_Feature = MechanicsFeatureExtension.ShieldDenied });

            //}

            public override bool CanHoldWeaponWithGrip(ItemEntityWeapon weapon, GripType gripType)
                => (gripType) switch
                {
                    GripType.Auto => true,
                    GripType.OneHanded => !weapon.Blueprint.Type.IsTwoHanded,
                    GripType.TwoHanded => weapon?.CanTakeTwoHands() ?? false,
                    _ => false

                };
            public override bool IsApplicableToOffHand
                => false;

            public override bool CanBeUsedOn(ItemEntityWeapon weapon, HandSlot slotToInsert = null, ItemEntity itemBeingInserted = null)
            {
                if (weapon == null || weapon.Blueprint.Double) return false;
                ItemEntityShield shield;
                bool WeaponIsTHeOneThatIsInserted = weapon == itemBeingInserted;
                if (WeaponIsTHeOneThatIsInserted)
                {
                    if (!slotToInsert.IsPrimaryHand)
                    {
                        return false;

                    }
                    else
                    {
                        shield = slotToInsert.PairSlot.MaybeShield;
                    }
                }
                else if (slotToInsert is not null && (weapon.HoldingSlot as HandSlot)?.PairSlot == slotToInsert)
                {
                    shield = itemBeingInserted as ItemEntityShield;
                }
                else
                {
                    shield = (weapon.HoldingSlot as HandSlot)?.PairSlot.MaybeShield;
                }
                if (shield is null)
                    return false;

                bool result = shield.ArmorComponent.Blueprint.ProficiencyGroup == ArmorProficiencyGroup.Buckler;

                return result;
            }

            public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
            {
                if (evt.Initiator != Owner) return;
                ItemEntityWeapon weapon = evt.Weapon;
                if (!CanBeUsedOn(weapon)) return;
                if (!weapon.HoldInTwoHands) return;
                BlueprintWeaponType type = weapon.Blueprint.Type;

                if (weapon.Blueprint.IsMelee && !Owner.Ensure<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>().UnhinderingShield)
                    evt.AddModifier(-1, Fact, ModifierDescriptor.Penalty);
            }

            public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }
            public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
            {
                if (evt.Weapon != null && CanBeUsedOn(evt.Weapon) && evt.Weapon.HoldInTwoHands && !(Owner.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>()?.UnhinderingShield ?? false) && ShieldForbiddance != null) {
                    Owner.AddBuff(ShieldForbiddance, Fact.MaybeContext, new TimeSpan?(new Rounds(1).Seconds));
                    ShieldAcState.Refresh(Owner);
                }
            }
            public void OnEventDidTrigger(RuleAttackWithWeapon evt) { }

            public static class CachePatch
            {

                //[HarmonyPrepare]
                //public static bool Prepare()
                //{
                //    if (AllowTwoHanded_as_OneHandedWhenBuckler.GetValue()) return true;
                //    else { Comment.Log("AllowTwoHanded_as_OneHandedWhenBuckler setting is disabled, patch AddBuckler1hToProfficiencyBlueprint won't be applied."); return false; };
                //}

                public static void AddBuckler1hToBlueprint()
                {
#if DEBUG
                    if (Settings.Debug.GetValue())
                        Comment.Log("Entered cache postfix to add Buckler1h component"); 
#endif
                    #region create 1h feature
                    BlueprintFeature Buckler1h_blueprint = new()
                    {
                        AssetGuid = new(new Guid("eb322fe21ec64a928ace4eaedb6d4339")),
                        name = "Buckler1h_Feature",
                        m_DisplayName = new LocalizedString() { Key = "Buckler1h_Name" },
                        HideInCharacterSheetAndLevelUp = true
                    };
                    Buckler1h_blueprint.AddComponent(new Buckler1h_Component());
                    Buckler1h_blueprint.AddToCache();
                    #endregion

                    if (!AllowTwoHanded_as_OneHandedWhenBuckler.GetValue())
                        { Comment.Log("AllowTwoHanded_as_OneHandedWhenBuckler setting is disabled, patch AddBuckler1hToProfficiencyBlueprint won't be applied."); return ; };

                    if (!RetrieveBlueprint("ca22afeb94442b64fb8536e7a9f7dc11", out BlueprintFeature FightDefensively, "FightDefensively", "when adding Buckler_1h to it")) return;
                    if (FightDefensively.Components.FirstOrDefault(component => component is AddFacts) is not AddFacts AF)
                    {
                        Comment.Warning("Failed to find AddFacts component on the FightDefensively feature blueprint. Can not add Buckler1h component.");
                        return;
                    };
                    AF.m_Facts = AF.m_Facts.Append(Buckler1h_blueprint.ToReference<BlueprintUnitFactReference>()).ToArray();

                    if (RetrieveBlueprint("26fcc43f7d20374498d2e1643381d345", out BlueprintShieldType BucklerType, "BucklerType"))
                        BucklerType.m_DescriptionText = new LocalizedString() { Key = "Buckler_Description" };
                }
            }
        }
        public static BlueprintActivatableAbility BlueprintBucklerParryActivatableAbility 
        {
            get
            {
                if (!Created) AddBucklerParryToBucklerProfficiencyBlueprint();
                return m_BlueprintBucklerParryActivatableAbility;
            }
        }

        internal static bool Created;
        static BlueprintActivatableAbility m_BlueprintBucklerParryActivatableAbility;

        public static void AddBucklerParryToBucklerProfficiencyBlueprint()
        {
            if (Created) return;
            // Native toggle artwork has a dark, colored background; the upstream
            // buckler image is opaque white and hides the action-bar highlight.
            Sprite BucklerParryIcon = ResourcesLibrary.TryGetBlueprint<BlueprintActivatableAbility>("3bb6b76ed5b38ab4f957c7f923c23b68")?.Icon ?? LoadIcon("Heavy");
            #region Create Shield Forbiddance blueprint
            BlueprintBuff ShieldForbiddance = new()
            {
                FxOnRemove = new(),
                m_Flags = BlueprintBuff.Flags.HiddenInUi,
            };
            ShieldForbiddance.AddComponent(new AddMechanicsFeature() { m_Feature = MechanicsFeatureExtension.ShieldDenied });
            ShieldForbiddance.AddToCache("414f40680af64050a2a9dde3dede32ac", "ForbidShieldACforOneTurn");
            Buckler1h_Component.ShieldForbiddance = ShieldForbiddance;
            #endregion
            #region Create BucklerParryBuff
            BlueprintBuff BucklerParryBuff = new()
            {
                AssetGuid = new BlueprintGuid(new Guid("a8f2b254bc2e4f8b95e12aa444287c58")),
                name = "BucklerParryBuff",
                m_DisplayName = new LocalizedString() { Key = "BucklerParry_DisplayName" },
                m_Description = new LocalizedString() { Key = "BucklerParry_Description" },
                m_DescriptionShort = new LocalizedString() { Key = "BucklerParry_ShortDescription" },
                Stacking = StackingType.Replace,
                m_Icon = BucklerParryIcon,
                FxOnRemove = new(),
                FxOnStart = new()
            };
            BucklerParryBuff.AddToCache();
            BucklerParryBuff.AddComponent(new OffHandParry.OffHandParryComponent() { category = WeaponCategory.WeaponLightShield });
            //BucklerParryBuff.AddComponent(new AddMechanicsFeature() { m_Feature = MechanicsFeatureExtension.ShieldDenied });
            BucklerParryBuff.AddComponent(new BuffDynamicDescriptionComponent_Parries(new() { m_Key = "BucklerParry_DynamicDescription" }));
            #endregion
            #region BucklerParryMainBuff
            BlueprintBuff BucklerParryMainBuff = new()
            {
                m_Flags = BlueprintBuff.Flags.HiddenInUi | BlueprintBuff.Flags.StayOnDeath,
                FxOnRemove = new(),
                FxOnStart = new()
            };
            BucklerParryMainBuff.AddToCache("dc65131392614eecba8b399e279c3915", "BucklerParryMainBuff");
            BucklerParryMainBuff.AddComponent(new AddInitiatorAttackWithWeaponTrigger()
            {
                ActionsOnInitiator = true,
                CheckWeaponRangeType = true,
                RangeType = WeaponRangeType.Melee,
                TriggerBeforeAttack = true,
                OnlyOnFirstAttack = true,
                Action = new()
                {
                    Actions = new GameAction[]
                    {
                        new ContextActionApplyBuff()
                        {
                            m_Buff = BucklerParryBuff.ToReference<BlueprintBuffReference>(),
                            UseDurationSeconds = true,
                            DurationSeconds = 7,
                            IsNotDispelable = true,
                            ToCaster = true,
                        }
                    }
                }
            });
            #endregion
            #region Create BucklerParryActivatableAbility
            BlueprintActivatableAbility BucklerParryActivatableAbility = new()
            {
                AssetGuid = new BlueprintGuid(new Guid("a86db1a253d0446b857938314af1ae51")),
                name = "BucklerParryActivatableAbility",
                m_Buff = BucklerParryMainBuff.ToReference<BlueprintBuffReference>(),
                ActivationType = AbilityActivationType.Immediately,
                //DeactivateAfterFirstRound = true,
                DeactivateIfCombatEnded= true,
                m_DisplayName = new LocalizedString() { Key = "BucklerParry_DisplayName" },
                m_Description = new LocalizedString() { Key = "BucklerParry_Description" },
                m_DescriptionShort = new LocalizedString() { Key = "BucklerParry_ShortDescription" },
                DoNotTurnOffOnRest = true,
                m_Icon = BucklerParryIcon,
            };
            BucklerParryActivatableAbility.AddComponent(new ShieldEquippedRestriction() { RequireWeaponComponent = true, RequireLightParryFeat = true, categories = new ArmorProficiencyGroup[] { ArmorProficiencyGroup.Buckler, ArmorProficiencyGroup.LightShield } });
            BucklerParryActivatableAbility.AddComponent(new RestrictionNonRangedWeapon());
            BucklerParryActivatableAbility.AddComponent(new RestrictionOtherActivatables() {
                m_ActivatableAbilities = new[] {
                    new BlueprintActivatableAbilityReference { deserializedGuid = BlueprintGuid.Parse("3bb6b76ed5b38ab4f957c7f923c23b68") },
                    new BlueprintActivatableAbilityReference { deserializedGuid = BlueprintGuid.Parse("7ca07f3bdbf145c2a975b11d9690c0b3") }
                }
            });
            BucklerParryActivatableAbility.AddComponent(new DirectlyControlledUnlessFactRestriction() { m_Fact = new() { deserializedGuid = BlueprintGuid.Parse("ac8aaf29054f5b74eb18f2af950e752d") } }); //TwoWeaponFighting
            BucklerParryActivatableAbility.AddToCache();
            #endregion
            #region BucklerParryFeature
            BlueprintFeature BucklerParryFeature = new()
            {
                HideInUI = true,
            };
            BucklerParryFeature.AddToCache("e547039b227a42d7a8d98fad72f8717c", "BucklerParryFeature");
            BucklerParryFeature.AddComponent(new AddFacts() { m_Facts = new BlueprintUnitFactReference[] { BucklerParryActivatableAbility.ToReference<BlueprintUnitFactReference>() } });
            BlueprintUnitFactReference BucklerParryFeatureReference = BucklerParryFeature.ToReference<BlueprintUnitFactReference>();
            #endregion
            string circ = "when adding BucklerParry";
            #region Add buckler parry to ShieldBash restrictions
            if (!RetrieveBlueprint("3bb6b76ed5b38ab4f957c7f923c23b68", out BlueprintActivatableAbility ShieldBashAbility, "ShieldBashAbility", circ)) goto skipShieldBashAbility;
            IEnumerable<RestrictionOtherActivatables> ROA = ShieldBashAbility.GetComponents<RestrictionOtherActivatables>();
            if (ROA.Count() == 0)
            {
                var newRestriction = new RestrictionOtherActivatables()
                {
                    m_ActivatableAbilities = new BlueprintActivatableAbilityReference[] { BucklerParryActivatableAbility.ToReference<BlueprintActivatableAbilityReference>() }
                };
                ShieldBashAbility.AddComponent(newRestriction);
            }
            else 
            {
                if (ROA.Any(restriction => restriction.m_ActivatableAbilities.Contains(BucklerParryActivatableAbility))) { Comment.Log("Skipping ShieldBash"); goto skipShieldBashAbility; }
                    else { var firstRestriction = ROA.First(); firstRestriction.m_ActivatableAbilities = firstRestriction.m_ActivatableAbilities.AddToArray(BucklerParryActivatableAbility.ToReference<BlueprintActivatableAbilityReference>()); }
            }
#if DEBUG
            Comment.Log("Added ShieldedDefenseActivatableAbility to the ShieldBashAbility restrictions."); 
#endif
            ; skipShieldBashAbility:
            #endregion
            RetrieveBlueprint("cb8686e7357a68c42bdd9d4e65334633", out BlueprintFeature ShieldsProficiency, "ShieldProficiency", circ);
            if (!AddBucklerParry.GetValue()) goto skipParry;
            #region modify Buckler Proficiency blueprint
            if (!RetrieveBlueprint("7c28228ce4eed1543a6b670fd2a88e72", out BlueprintFeature BucklerProf, "Buckler Proficiency", circ)) goto SkipBucklerProf;
            AddFacts af  = BucklerProf.GetComponent<AddFacts>();
            if (af is null)
            {
                af = new() { m_Facts = Array.Empty<BlueprintUnitFactReference>() };
                BucklerProf.AddComponent(af);
            }
            if (!(af.m_Facts ?? Array.Empty<BlueprintUnitFactReference>()).Contains(BucklerParryFeatureReference)) af.m_Facts = (af.m_Facts ?? Array.Empty<BlueprintUnitFactReference>()).AddToArray(BucklerParryFeatureReference);
            Comment.Log("Added BucklerParryActivatableFeature to the BucklerProficiency blueprint.");
            BucklerProf.AddComponent(new AddProficiencies() { ArmorProficiencies = new ArmorProficiencyGroup[] { }, WeaponProficiencies = new WeaponCategory[] { WeaponCategory.WeaponLightShield } });
            BucklerProf.m_Description = new() { m_Key = "BucklerProficiencyWithParry_Description" };
            if (!RetrieveBlueprint("121811173a614534e8720d7550aae253", out BlueprintFeature ShieldBashFeature, "ShieldBashFeature", circ)) goto SkipBucklerProf;
            ShieldBashFeature.Components = ShieldBashFeature.Components.Where(component => component is not PrerequisiteNotProficient noProf || !noProf.WeaponProficiencies.Contains(WeaponCategory.WeaponLightShield)).ToArray();
            Comment.Log("Emptied weapon proficiencies on the old Shield Bash blueprint");
            ; SkipBucklerProf:;
            #endregion
            ;skipParry:;
            NewFeatsAndAbilities.LightShieldParry.CreateBlueprint(BucklerParryFeature);
            m_BlueprintBucklerParryActivatableAbility = BucklerParryActivatableAbility;
            Created = true;
        }

        public class ShieldEquippedRestriction : ActivatableAbilityRestriction
        {
            public ArmorProficiencyGroup[] categories = new ArmorProficiencyGroup[] { };
            public bool RequireWeaponComponent;
            public bool RequireLightParryFeat;
            protected override bool IsAvailable()
            {
               
                UnitBody body = Owner?.Body;
                if (body is null) return false;
                ItemEntityWeapon mainhand = body.PrimaryHand.MaybeWeapon;
                if (mainhand is not null && mainhand.HoldInTwoHands) return false;
                ItemEntityShield shield = body.SecondaryHand?.MaybeShield;
                if (shield is null || shield.ArmorComponent is null || (RequireWeaponComponent && shield.WeaponComponent is null)) return false;
                if (RequireLightParryFeat && shield.Blueprint.Type.ProficiencyGroup == ArmorProficiencyGroup.LightShield && !Owner.HasFact(NewFeatsAndAbilities.LightShieldParry.Feature)) return false;
                foreach (var category in categories)
                {
                    if (shield.ArmorComponent.Blueprint.ProficiencyGroup == category
                        && Owner.Proficiencies.Contains(category))
                    {
                        return true;
                    }
                };
                return false;
            }
        }

        //[HarmonyPatch]
        //public static class BucklerModelReAttachment
        //{
        //    [HarmonyPatch(typeof(UnitViewHandSlotData), nameof(UnitViewHandSlotData.OffHandTransform), MethodType.Getter)]
        //    public static bool Prefix(UnitViewHandSlotData __instance, ref Transform __result)
        //    {
        //        Comment.Log($"BucklerModelReAttachment - item {__instance?.VisibleItem?.Blueprint.name} (object name '{__instance?.VisualModel?.name}') on unit { __instance?.Owner?.CharacterName}. " +
        //            $"Buckler? {(__instance?.VisibleItem?.Blueprint as BlueprintItemShield)?.Type.ProficiencyGroup == ArmorProficiencyGroup.Buckler}. " +
        //            $"Owner has forearm? {__instance.Character.transform.FindChildRecursive("L_ForeArm") is not null}.");
        //        if ((__instance?.VisibleItem?.Blueprint as BlueprintItemShield)?.Type.ProficiencyGroup == ArmorProficiencyGroup.Buckler)
        //        {
        //            __result = __instance.Character.transform.FindChildRecursive("L_ForeArm");
        //        }
        //        return __result is null;
        //    }

        //}
    }
}
