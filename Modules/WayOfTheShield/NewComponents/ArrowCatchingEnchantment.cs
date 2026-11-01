using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Items;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.ElementsSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.TextTools;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static Way_of_the_shield.Main;
using System.Reflection.Emit;
using Kingmaker.Designers;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;

namespace Way_of_the_shield.NewComponents
{
    [HarmonyPatch]
    public class ArrowCatching
    {
        public static BlueprintBuff buff;
        public static BlueprintBuff AoEBuff;
        public static BlueprintArmorEnchantment Enchantment;
        public static RulebookEvent.CustomDataKey ArrowCatchingKey = new("ArrowCatching");

        public static void CreateArrowCatchingEnchantment()
        {
            #region create Arrow Catching effect buff
            BlueprintBuff ArrowCatchingEffectBuff = new()
            {
                AssetGuid = new BlueprintGuid(new Guid("472986c4378d417e8066608452f6560b")),
                name = modName + "_ArrowCatchingEffectBuff",
                FxOnRemove = new(),
                FxOnStart = new(),
                m_DisplayName = new LocalizedString() { Key = "ArrowCatchingEffectBuff_DisplayName" },
                m_Description = new LocalizedString() { Key = "ArrowCatchingEffectBuff_Description" },
                m_DescriptionShort = new LocalizedString() { Key = "ArrowCatchingEffectBuff_ShortDescription" },
                Stacking = StackingType.Stack,
                m_Flags = BlueprintBuff.Flags.StayOnDeath,
            };
            ArrowCatchingEffectBuff.AddToCache();
            buff = ArrowCatchingEffectBuff;
            #endregion
            #region Create ArrowCatchingAreaEffect
            BlueprintAbilityAreaEffect ArrowCatchingAreaEffect = new()
            {
                AssetGuid = new BlueprintGuid(new Guid("9c14a794f68947b0b0f5d5158e421b10")),
                name = modName + "_ArrowCatchingAreaEffect",
                Shape = AreaEffectShape.Cylinder,
                Size = new(5),
                Fx = new(),
                m_TargetType = BlueprintAbilityAreaEffect.TargetType.Ally,
                AffectDead = true,
            };
            ArrowCatchingAreaEffect.AddComponent(
                new AbilityAreaEffectBuff()
                {
                    m_Buff = ArrowCatchingEffectBuff.ToReference<BlueprintBuffReference>(),
                    Condition = new()
                    {
                        Conditions = new Condition[1]
                        {
                            new ContextConditionIsCaster() {Not = true}
                        }
                    }
                });
            ArrowCatchingAreaEffect.AddToCache();
            #endregion
            #region Create ArrowCatchingAoEBuff
            BlueprintBuff ArrowCatchingAoEBuff = new()
            {
                AssetGuid = new BlueprintGuid(new Guid("194bf0bcb7474983b395eea1844a200c")),
                name = modName + "_ArrowCatchingAoEBuff",
                FxOnRemove = new(),
                FxOnStart = new(),
                m_DisplayName = new LocalizedString() { Key = "ArrowCatchingAoEBuff_DisplayName" },
                m_Description = new LocalizedString() { Key = "ArrowCatchingAoEBuff_Description" },
                //m_DescriptionShort = new LocalizedString() { Key = "ArrowCatchingAoEBuff_ShortDescription" },
                Stacking = StackingType.Replace,
                m_Flags = BlueprintBuff.Flags.StayOnDeath
                | BlueprintBuff.Flags.HiddenInUi,
            };
            ArrowCatchingAoEBuff.AddComponent(
                new AddAreaEffect()
                {
                    m_AreaEffect = ArrowCatchingAreaEffect.ToReference<BlueprintAbilityAreaEffectReference>(),
                });
            ArrowCatchingAoEBuff.AddToCache();
            AoEBuff = ArrowCatchingAoEBuff;
            #endregion
            #region Create ArrowCatchingFact

            BlueprintUnitFact ArrowCatchingFact = new()
            {
                AssetGuid = new BlueprintGuid(new Guid("63726ab5d359497291fa2280603ca9d8")),
                name = modName + "_ArrowCatchingFact"
            };
            ArrowCatchingFact.AddComponent(
                new AddFactContextActions()
                {
                    Deactivated = new(),
                    NewRound = new(),
                    Activated = new()
                    {
                        Actions = new GameAction[1]
                        {
                            new ContextActionApplyBuff()
                            {
                                DurationValue = new(),
                                Permanent = true,
                                m_Buff = ArrowCatchingAoEBuff.ToReference<BlueprintBuffReference>(),
                                IsNotDispelable = true,

                            }
                        }
                    }
                });
            ArrowCatchingFact.AddToCache();
            #endregion
            #region Create ArrowCatchingEnchantment
            BlueprintArmorEnchantment ArrowCatchingEnchantment = new()
            {
                AssetGuid = new BlueprintGuid(new Guid("e778c16a7f404d59bca25d42e380c9d0")),
                name = modName + "_ArrowCatchingEnchantment",
                m_EnchantmentCost = 1,
                m_EnchantName = new() { Key = "ArrowCatchingEnchantment_EnchantName" },
                m_Description = new LocalizedString() { Key = "ArrowCatchingEnchantment_Description" }
            };
            ArrowCatchingEnchantment.AddComponent(
                new AddUnitFactEquipment()
                {
                    m_Blueprint = AoEBuff.ToReference<BlueprintUnitFactReference>()
                });
            ArrowCatchingEnchantment.AddToCache();
            Enchantment = ArrowCatchingEnchantment;
            #endregion
        }

        [HarmonyPatch(typeof(RuleAttackWithWeapon), MethodType.Constructor, new Type[] { typeof(UnitEntityData), typeof(UnitEntityData), typeof(ItemEntityWeapon), typeof(int) })]
        [HarmonyPrefix]
        public static void InsertArrowCatchCheckIntoRuleAttackWithWeapon(UnitEntityData attacker, ref UnitEntityData target, ItemEntityWeapon weapon, out UnitEntityData __state)
        {
            __state = null;
            if (target?.Buffs == null || attacker == null || weapon?.Blueprint?.VisualParameters?.Projectiles == null || weapon.Blueprint.VisualParameters.Projectiles.Length == 0) return;
            if (buff == null || AoEBuff == null || target.Buffs.HasFact(AoEBuff)) return;
            foreach (Buff candidate in target.Buffs.Enumerable.Where(value => value.Blueprint == buff))
            {
                UnitEntityData catcher = candidate.MaybeContext?.MaybeCaster;
                ItemEntity sourceItem = candidate.MaybeContext?.SourceItem;
                if (catcher == null) catcher = sourceItem?.Owner;
                ItemEntityArmor shieldArmor = catcher?.Body?.SecondaryHand?.MaybeShield?.ArmorComponent;
                if (catcher == null || shieldArmor == null || catcher == target || !shieldArmor.HasEnchantment(Enchantment)) continue;
                if (sourceItem != null && sourceItem != shieldArmor) continue;
                if (GameHelper.GetItemEnhancementBonus(shieldArmor) < GameHelper.GetItemEnhancementBonus(weapon)) continue;
                if (catcher.State.LifeState != Kingmaker.UnitLogic.UnitLifeState.Conscious || !attacker.HasLOS(catcher)) continue;
                __state = target;
                target = catcher;
                return;
            }
        }

        [HarmonyPatch(typeof(RuleAttackWithWeapon), MethodType.Constructor, new Type[] { typeof(UnitEntityData), typeof(UnitEntityData), typeof(ItemEntityWeapon), typeof(int) })]
        [HarmonyPostfix]
        public static void RememberArrowCatchingTarget(RuleAttackWithWeapon __instance, UnitEntityData __state)
        {
            if (__state != null) __instance.SetCustomData(ArrowCatchingKey, __state);
        }
        [HarmonyPatch(typeof(AttackLogMessage), nameof(AttackLogMessage.GetData))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> AddArrowCatchingLineIntoAttackLog(IEnumerable<CodeInstruction> instructions)
        {
#if DEBUG
            if (Debug.GetValue())
                Comment.Log("Entered AddArrowCatchingLineIntoAttackLog transpiler"); 
#endif

            List<CodeInstruction> _instructions = instructions.ToList();

            CodeInstruction[] toSearch = new CodeInstruction[]
            {
                new (OpCodes.Ldarg_0),
                new (OpCodes.Ldfld, typeof(AttackLogMessage).GetField(nameof(AttackLogMessage.Message)))
            };

            int index = IndexFinder(_instructions, toSearch);
            if (index == -1)
            {
                Comment.Error("Failed to find this.Message when transpiling AttackLogMessage.GetData) to add Arrow Catching string");
                return instructions;
            };

            
            _instructions.RemoveRange(index - 1, 2);

            CodeInstruction[] toInsert = new CodeInstruction[]
            {
                new (OpCodes.Ldarg_1),
               new CodeInstruction(OpCodes.Call, typeof(ArrowCatching).GetMethod(nameof(CatchOrNoCatch)))
            };

            _instructions.InsertRange(index - 1, toInsert);

            return _instructions;
        }

        public static string CatchOrNoCatch(AttackLogMessage msg, RuleAttackRoll rule)
        {
#if DEBUG
            if (Debug.GetValue())
            {
                Comment.Log("ArrowCatching - rule.TryGetCustomData(ArrowCatchingKey, out UnitEntityData original1) is " + (rule.TryGetCustomData(ArrowCatchingKey, out UnitEntityData original1)));
                Comment.Log("ArrowCatching - original1 is null? " + (original1 is null));
            }  
#endif
            if (rule.RuleAttackWithWeapon is null || !rule.RuleAttackWithWeapon.TryGetCustomData(ArrowCatchingKey, out UnitEntityData original) || original is null) return msg.Message;
            else return (Catch1 + LogHelper.GetUnitName(original) + Catch2);
            //else return string.Format("{0} attacks {1} with {2}, but the projectile veers towards the enchanted shield of {3}",
                   //rule.Initiator.CharacterName, original.CharacterName, rule.Weapon?.Name, rule.Target.CharacterName);
        }

        public static LocalizedString Catch1
        {
            get
            {
                m_Catch1 ??= new() { m_Key = "ArrowCatching_SourceAttacks", m_ShouldProcess = true, Shared = null };
                return m_Catch1;
            }
             
        }

        public static LocalizedString Catch2
        {
            get
            {
                m_Catch2 ??= new() { m_Key = "ArrowCatching_ShieldOfTarget", m_ShouldProcess = true, Shared = null };
                return m_Catch2;
            }

        }

        private static LocalizedString m_Catch1;
        private static LocalizedString m_Catch2;
    }

}
