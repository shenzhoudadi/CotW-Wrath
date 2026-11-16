using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Localization;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.ContextData;
using Kingmaker.Utility;
using Newtonsoft.Json;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    public sealed class Occultist0099AegisStoredSpell
    {
        [JsonProperty] public ItemEntityArmor Armor;
        [JsonProperty] public AbilityData Spell;
        [JsonProperty] public AbilityParams Parameters;
    }

    [TypeId(Occultist0099Ids.AegisStorageTypeId)]
    public sealed class Occultist0099AegisStorage : UnitFactComponentDelegate<Occultist0099AegisStoredSpell>
    {
        private const string FactToken = "AegisStorageFact";
        private static readonly string FactId = Occultist0099Ids.Id(FactToken);
        private static readonly BlueprintGuid ReleaseId = BlueprintGuid.Parse(Occultist0099Ids.Id("AegisReleaseToggle"));
        internal static readonly string[] Tokens = { FactToken, "AegisStorageEnchantment", "AegisStorageCommands",
            "AegisStoreSpellAbility", "AegisReleaseBuff", "AegisReleaseToggle" };

        internal static BlueprintScriptableObject[] Build(BlueprintAbility icon, Func<string, string, string, LocalizedString> text)
        {
            const string desc = "Store a hostile touch spell of 1st through 3rd level in this armor as a standard action. Enable Release to discharge it on the next foe that hits you in melee. A stored spell stays with the same armor when Aegis expires.";
            var holder = Identify(new BlueprintFeature { Ranks = 1, IsClassFeature = true, HideInUI = true,
                HideInCharacterSheetAndLevelUp = true, ComponentsArray = new BlueprintComponent[]
                { new Occultist0099AegisStorage { name = "OccultistAegisStorage" } } }, FactToken, "Aegis Spell Storage", "", null, text);
            var store = Identify(new BlueprintAbility { Type = AbilityType.Supernatural, Range = AbilityRange.Personal,
                CanTargetSelf = true, NotOffensive = true, ActionType = UnitCommand.CommandType.Standard,
                ComponentsArray = new BlueprintComponent[] { new Occultist0099AegisStoreSpell { name = "OccultistStoreSpell" } } },
                "AegisStoreSpellAbility", "Aegis - Store Spell", desc, icon, text);
            var armed = Identify(new BlueprintBuff { ComponentsArray = new BlueprintComponent[]
            {
                new AddTargetAttackWithWeaponTrigger { name = "OccultistAegisMeleeHit", OnlyHit = true, OnlyMelee = true,
                    ActionsOnAttacker = Actions(new Occultist0099AegisReleaseAction()), ActionOnSelf = Actions() }
            } },
                "AegisReleaseBuff", "Aegis - Release Ready", desc, icon, text);
            var release = Identify(new BlueprintActivatableAbility { ActivationType = AbilityActivationType.Immediately, DeactivateImmediately = true,
                ComponentsArray = new BlueprintComponent[] { new Occultist0099AegisRelease { name = "OccultistHasStoredSpell" } } },
                "AegisReleaseToggle", "Aegis - Release Stored Spell", desc, icon, text);
            Set(release, "m_Buff", Ref<BlueprintBuffReference>(armed));
            var commands = Identify(new BlueprintFeature { Ranks = 1, HideInUI = true, HideInCharacterSheetAndLevelUp = true,
                ComponentsArray = new BlueprintComponent[] { Facts(store, release) } }, "AegisStorageCommands", "Spell Storing", "", null, text);
            var grant = new AddUnitFeatureEquipment { name = "OccultistArmorSpellStoring" };
            Set(grant, "m_Feature", Ref<BlueprintFeatureReference>(commands));
            var enchantment = Identify(new BlueprintArmorEnchantment { ComponentsArray = new BlueprintComponent[] { grant } },
                "AegisStorageEnchantment", "", "", null, text);
            Set(enchantment, "m_EnchantmentCost", 1);
            Set(enchantment, "m_EnchantName", text(enchantment.name, "name", "Spell Storing"));
            Set(enchantment, "m_Description", text(enchantment.name, "description", desc));
            return new BlueprintScriptableObject[] { holder, commands, enchantment, store, armed, release };
        }

        internal static ItemEntityArmor ArmedArmor(UnitEntityData unit)
        {
            var armor = Occultist0099Aegis.Armor(unit);
            if (armor == null || !unit.Buffs.Enumerable.Any(buff => buff.Blueprint.AssetGuid == Occultist0099Aegis.WardId &&
                    buff.IsTurnedOn && !buff.IsSuppressed)) return null;
            return armor.Enchantments.Any(enchantment => enchantment.Blueprint.AssetGuid == Occultist0099Aegis.StorageEnchantmentId)
                ? armor : null;
        }

        private static EntityFactComponent Holder(UnitEntityData unit)
            => Occultist0099NativeScope.RuntimeOf(unit, FactId, typeof(Occultist0099AegisStorage));

        internal static bool HasHolder(UnitEntityData unit) => Holder(unit) != null;

        internal static bool Ready(UnitEntityData unit)
            => Occultist0099NativeScope.InRuntimeContext(Holder(unit), component => ((Occultist0099AegisStorage)component).HasStoredSpell(), false);

        private bool HasStoredSpell() => Data.Spell != null && Data.Parameters != null && Data.Armor != null && Data.Armor == ArmedArmor(Owner);

        internal static void Store(UnitEntityData unit, ItemEntityArmor armor, AbilityData spell, AbilityParams parameters)
            => Occultist0099NativeScope.InRuntimeContext(Holder(unit), component =>
            {
                var storage = (Occultist0099AegisStorage)component;
                storage.Data.Armor = armor;
                storage.Data.Spell = new AbilityData(spell, spell.Blueprint);
                storage.Data.Parameters = parameters.Clone();
            });

        internal static void Release(UnitEntityData unit, UnitEntityData target)
            => Occultist0099NativeScope.InRuntimeContext(Holder(unit), component => ((Occultist0099AegisStorage)component).Release(target));

        private void Release(UnitEntityData target)
        {
            if (target == null || target == Owner || !HasStoredSpell()) return;
            var marker = Owner.ActivatableAbilities.Enumerable.FirstOrDefault(ability => ability.Blueprint.AssetGuid == ReleaseId &&
                ability.Active && ability.IsStarted && ability.IsOn && ability.AppliedBuff?.IsSuppressed == false);
            if (marker == null) return;
            var cast = PsychicStoredSpell.BuildDelivery(Data.Spell, Data.Parameters, target, touch: true, directTouch: true);
            if (cast == null) return;
            Data.Spell = null;
            Data.Parameters = null;
            Data.Armor = null;
            marker.IsOn = false;
            // Native spell actions can create child buffs. Their cast must
            // not inherit the attacking unit's release trigger's buff scope.
            using (ContextData<Buff.Data>.Request()) PsychicStoredSpell.Launch(cast);
        }
    }

    [TypeId(Occultist0099Ids.AegisReleaseActionTypeId)]
    public sealed class Occultist0099AegisReleaseAction : ContextAction
    {
        public override string GetCaption() => "Release the spell stored in Aegis";

        public override void RunAction()
        {
            if (ContextData<ContextAttackData>.Current?.AttackRoll?.IsFake == true) return;
            Occultist0099AegisStorage.Release(Context.MaybeCaster, Target.Unit);
        }
    }

    [TypeId(Occultist0099Ids.AegisReleaseTypeId)]
    public sealed class Occultist0099AegisRelease : ActivatableAbilityRestriction
    {
        protected override bool IsAvailable() => Occultist0099AegisStorage.Ready(Owner);
    }

    [TypeId(Occultist0099Ids.AegisStoreSpellTypeId)]
    public sealed class Occultist0099AegisStoreSpell : AbilityConvertSpellLevelToResource, IAbilityRestriction, IAbilityVisibilityProvider
    {
        private static readonly ConditionalWeakTable<AbilityExecutionContext, object> Applied =
            new ConditionalWeakTable<AbilityExecutionContext, object>();

        internal static bool Eligible(AbilityData ability)
        {
            var bp = ability?.Blueprint;
            var sticky = bp?.GetComponent<AbilityEffectStickyTouch>();
            return bp?.Type == AbilityType.Spell && bp.Range == AbilityRange.Touch && bp.CanTargetEnemies &&
                bp.EffectOnEnemy == AbilityEffectOnUnit.Harmful && !bp.NotOffensive && !bp.IsFullRoundAction && !bp.HasVariants &&
                SkirnirStoreSpell01454.IsSingleTargetShape(bp) &&
                (sticky == null || SkirnirStoreSpell01454.IsSingleTargetShape(sticky.TouchDeliveryAbility)) &&
                (!ability.RequireMaterialComponent || ability.HasEnoughMaterialComponent);
        }

        private static bool Validate(AbilityData conversion, out AbilityData selected, out AbilityData source)
        {
            selected = conversion?.ConvertedFrom;
            source = null;
            var unit = conversion?.Caster?.Unit;
            var book = selected?.Spellbook;
            int circle = conversion?.ParamSpellLevel ?? 0;
            if (unit == null || book == null || !conversion.SaveSpellbookSlot || selected.Caster.Unit != unit ||
                selected.SourceItem != null || circle < 1 || circle > 3 ||
                selected.Caster.GetSpellbook(book.Blueprint) != book || !Eligible(selected) ||
                Occultist0099AegisStorage.ArmedArmor(unit) == null || !Occultist0099AegisStorage.HasHolder(unit)) return false;
            var grant = SkirnirStorageSource01455.BoundFact(conversion);
            if (grant?.Active != true || grant.IsDisposed || grant.Blueprint != conversion.Blueprint ||
                !unit.Abilities.Enumerable.Any(fact => ReferenceEquals(fact, grant))) return false;
            var leaf = selected;
            var examined = new HashSet<object>(IdentityComparer);
            for (var candidate = selected; candidate?.Spellbook == book && examined.Add(candidate); candidate = candidate.ConvertedFrom)
            {
                if (candidate.Caster.Unit != unit || candidate.SourceItem != null || candidate.ResourceLogic != null ||
                    candidate.SpellLevel != circle || !book.CanSpend(candidate)) continue;
                bool real = book.Blueprint.Spontaneous ? book.GetKnownSpells(circle).Concat(book.GetCustomSpells(circle))
                    .Concat(book.GetSpecialSpells(circle)).Contains(candidate) : candidate.SpellSlot?.Available == true;
                if (!real || (candidate.Blueprint != leaf.Blueprint &&
                    !SkirnirStoreSpell01454.Leaves(candidate).Any(option => option.Blueprint == leaf.Blueprint))) continue;
                source = candidate;
                return true;
            }
            return false;
        }

        public new bool IsAbilityVisible(AbilityData ability) => Validate(ability, out _, out _);
        public bool IsAbilityRestrictionPassed(AbilityData ability) => IsAbilityVisible(ability);
        public string GetAbilityRestrictionUIText() => "Choose an available hostile touch spell of 1st to 3rd level while wearing spell-storing armor.";

        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var conversion = context?.Ability;
            var unit = context?.MaybeCaster;
            if (unit == null || target?.Unit != unit || Applied.TryGetValue(context, out _) ||
                !Validate(conversion, out var selected, out var source)) return;
            var armor = Occultist0099AegisStorage.ArmedArmor(unit);
            var parameters = selected.CalculateParams();
            if (parameters == null || armor != Occultist0099AegisStorage.ArmedArmor(unit) ||
                !Validate(conversion, out _, out var liveSource) || !ReferenceEquals(source, liveSource)) return;
            Applied.Add(context, context);
            if (source.Spellbook.Blueprint.Spontaneous)
            {
                if (!source.Spellbook.Spend(source)) { Applied.Remove(context); return; }
            }
            else
            {
                source.SpellSlot.Spend();
            }
            selected.SpendMaterialComponent();
            Occultist0099AegisStorage.Store(unit, armor, selected, parameters);
        }
    }

    [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.GetConversions))]
    internal static class Occultist0099NativeAegisStoreConversions
    {
        private static readonly System.Reflection.PropertyInfo SpellLevelParameter = AccessTools.Property(typeof(AbilityData), nameof(AbilityData.ParamSpellLevel));

        private static void Postfix(AbilityData __instance, ref IEnumerable<AbilityData> __result)
        {
            if (__result == null) return;
            var original = __result.ToArray();
            if (!original.Any(candidate => candidate.Blueprint.GetComponent<Occultist0099AegisStoreSpell>() != null)) return;
            var next = new List<AbilityData>();
            foreach (var candidate in original)
            {
                var component = candidate.Blueprint.GetComponent<Occultist0099AegisStoreSpell>();
                if (component == null) { next.Add(candidate); continue; }
                var fact = __instance.Caster.Abilities.Enumerable.FirstOrDefault(ability => ability.Active && ability.Blueprint == candidate.Blueprint);
                if (fact == null) continue;
                foreach (var leaf in SkirnirStoreSpell01454.Leaves(__instance).Where(Occultist0099AegisStoreSpell.Eligible))
                {
                    var conversion = new AbilityData(fact) { ConvertedFrom = leaf, SaveSpellbookSlot = true };
                    SpellLevelParameter.SetValue(conversion, candidate.ParamSpellLevel, null);
                    SkirnirStorageSource01455.Bind(conversion, fact);
                    if (component.IsAbilityVisible(conversion)) next.Add(conversion);
                }
            }
            __result = next;
        }
    }

    [HarmonyPatch(typeof(AbilityData), "get_Name")]
    internal static class Occultist0099NativeAegisStoreCaption
    {
        private static void Postfix(AbilityData __instance, ref string __result)
        {
            if (__instance.Blueprint.GetComponent<Occultist0099AegisStoreSpell>() != null && __instance.ConvertedFrom != null)
                __result += " (" + __instance.ConvertedFrom.Name + ")";
        }
    }
}
