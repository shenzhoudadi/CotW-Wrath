using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Way_of_the_shield.NewComponents;

namespace Way_of_the_shield.Tweaks_and_Changes
{
    [HarmonyPatch]
    public class ShieldBashAbilityTweak
    {
        private const string AbilityGuid = "3bb6b76ed5b38ab4f957c7f923c23b68";
        private const string ImprovedGuid = "121811173a614534e8720d7550aae253";
        private const string WrapperGuid = "f42adaab0f24462c87a7875c259ffccb";
        private static bool initialized;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
        public static void BlueprintsCache_Init_Patch_MoveShieldBashAbilityToProficiencyBP()
        {
            if (initialized) return;
            var wrapper = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(WrapperGuid);
            if (wrapper == null)
            {
                wrapper = new BlueprintFeature { IsClassFeature = true, HideInUI = true };
                wrapper.AddComponent(new AddFacts
                {
                    m_Facts = new[] { new BlueprintUnitFactReference { deserializedGuid = BlueprintGuid.Parse(AbilityGuid) } }
                });
                wrapper.AddToCache(WrapperGuid, "NewShieldBashFeature");
            }
            if (!AllowShieldBashToAllWhoProficient.GetValue()) { initialized = true; return; }
            const string circ = "allowing shield bash without Improved Shield Bash";
            if (!RetrieveBlueprint(AbilityGuid, out BlueprintActivatableAbility ability, "ShieldBashAbility", circ)
                || !RetrieveBlueprint(ImprovedGuid, out BlueprintFeature improved, "ShieldBashFeature", circ)) return;
            if (!ability.GetComponents<DirectlyControlledUnlessFactRestriction>().Any(c => c.m_Fact?.deserializedGuid == improved.AssetGuid))
                ability.AddComponent(new DirectlyControlledUnlessFactRestriction { m_Fact = improved.ToReference<BlueprintUnitFactReference>() });
            var wrapperReference = wrapper.ToReference<BlueprintUnitFactReference>();
            foreach (var facts in improved.GetComponents<AddFacts>())
                facts.m_Facts = (facts.m_Facts ?? Array.Empty<BlueprintUnitFactReference>())
                    .Where(f => f != null && f.deserializedGuid != BlueprintGuid.Parse(AbilityGuid)).ToArray();
            AddFactOnce(improved, wrapperReference);
            improved.m_DisplayName = new LocalizedString { m_Key = "OldShieldBashFeature_DisplayName" };
            improved.m_Description = new LocalizedString { m_Key = "OldShieldBashFeature_Description" };
            foreach (var guid in new[] { "cb8686e7357a68c42bdd9d4e65334633", "7c28228ce4eed1543a6b670fd2a88e72" })
                if (RetrieveBlueprint(guid, out BlueprintFeature proficiency, "Shield proficiency", circ)) AddFactOnce(proficiency, wrapperReference);
            if (RetrieveBlueprint("5566971fdebf7fe468a497bbee0d3ed5", out BlueprintBuff bashBuff, "ShieldBashBuff", circ)
                && !bashBuff.GetComponents<AddInitiatorShieldBashTrigger>().Any(c => c.name == "WayOfTheShield_ShieldBashLoseAC"))
            {
                bashBuff.AddComponent(new AddInitiatorShieldBashTrigger
                {
                    name = "WayOfTheShield_ShieldBashLoseAC",
                    OnlyOnFirstBashAttack = true,
                    ActionsOnInitiator = true,
                    Actions = new ActionList
                    {
                        Actions = new GameAction[]
                        {
                            new Conditional
                            {
                                ConditionsChecker = new ConditionsChecker
                                {
                                    Conditions = new Condition[]
                                    {
                                        new ContextConditionCasterHasFact { m_Fact = improved.ToReference<BlueprintUnitFactReference>() }
                                    }
                                },
                                IfTrue = new ActionList { Actions = Array.Empty<GameAction>() },
                                IfFalse = new ActionList
                                {
                                    Actions = new GameAction[]
                                    {
                                        new ContextActionApplyBuff
                                        {
                                            m_Buff = new BlueprintBuffReference { deserializedGuid = BlueprintGuid.Parse("414f40680af64050a2a9dde3dede32ac") },
                                            UseDurationSeconds = true,
                                            DurationSeconds = 6,
                                            ToCaster = true
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
            }
            initialized = true;
        }

        public static void AddFactOnce(BlueprintFeature feature, BlueprintUnitFactReference reference)
        {
            if (feature.GetComponents<AddFacts>().Any(c => (c.m_Facts ?? Array.Empty<BlueprintUnitFactReference>()).Any(f => f?.deserializedGuid == reference.deserializedGuid))) return;
            var facts = feature.GetComponent<AddFacts>();
            if (facts == null) { facts = new AddFacts { m_Facts = Array.Empty<BlueprintUnitFactReference>() }; feature.AddComponent(facts); }
            facts.m_Facts = (facts.m_Facts ?? Array.Empty<BlueprintUnitFactReference>()).Append(reference).ToArray();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ActivatableAbilityCollection), nameof(ActivatableAbilityCollection.OnFactDidAttach), new[] { typeof(ActivatableAbility) })]
        public static IEnumerable<CodeInstruction> ActivatableAbilityCollection_OnFactDidAttach_PatchToNotActivateOnAttach(IEnumerable<CodeInstruction> instructions)
        {
            var result = instructions.Select(i => new CodeInstruction(i)).ToList();
            var field = AccessTools.Field(typeof(BlueprintActivatableAbility), nameof(BlueprintActivatableAbility.IsOnByDefault));
            int index = result.FindIndex(i => i.opcode == OpCodes.Ldfld && Equals(i.operand, field));
            if (index < 0) throw new InvalidOperationException("Native activatable default activation check was not found.");
            result.InsertRange(index + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ShieldBashAbilityTweak), nameof(FilterDefaultActivation)))
            });
            return result;
        }
        public static bool FilterDefaultActivation(bool onByDefault, ActivatableAbility fact)
            => onByDefault && CheckShieldBashForDefaultOn(fact);
        public static bool CheckShieldBashForDefaultOn(ActivatableAbility fact)
            => fact?.Blueprint.AssetGuidThreadSafe != AbilityGuid || !fact.Owner.IsPlayerFaction;
    }
}

