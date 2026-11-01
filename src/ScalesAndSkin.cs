using System;
using System.Linq;
using Newtonsoft.Json;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Parts;

namespace CallOfTheWild.Wrath.Prototype
{
    public sealed class ScalesAndSkinData { [JsonIgnore] public ModifiableValue.Modifier Modifier; }
    [TypeId("bed611413d304b83bf2210a4e7e0e041")]
    public sealed class ScalesAndSkinEffect : UnitFactComponentDelegate<ScalesAndSkinData>, IUnitBuffHandler
    {
        internal static int Value(Buff buff)
        {
            if (buff == null || !buff.IsActive || !buff.IsTurnedOn || buff.IsSuppressed || buff.IsDisposed) return 0;
            var context = buff.Context; var source = context?.SourceAbilityContext?.Ability?.Blueprint ?? context?.SourceAbility;
            if (source == null || (source.Type != AbilityType.Spell && source.Type != AbilityType.SpellLike) || source.School != SpellSchool.Transmutation) return 0;
            return context.Params.CasterLevel >= 10 ? 2 : 1;
        }
        void Clear() { foreach (var m in Owner.Stats.AC.Modifiers.Where(m => ReferenceEquals(m.Source, Fact)).ToArray()) Owner.Stats.AC.RemoveModifier(m); Data.Modifier = null; }
        internal void Refresh()
        {
            int bonus = Fact.IsActive && Fact.IsTurnedOn && !Fact.IsDisposed ? Owner.Buffs.Enumerable.Select(Value).DefaultIfEmpty(0).Max() : 0;
            if (Data.Modifier?.ModValue == bonus || bonus == 0 && Data.Modifier == null) return;
            Clear();
            if (bonus > 0) Data.Modifier = Owner.Stats.AC.AddModifier(new ModifiableValue.Modifier { ModValue = bonus, Source = Fact, ModDescriptor = ModifierDescriptor.NaturalArmor, StackMode = ModifiableValue.StackMode.ForceStack });
        }
        public void HandleBuffDidAdded(Buff buff) { if (buff.Owner == Owner) Refresh(); }
        public void HandleBuffDidRemoved(Buff buff) { if (buff.Owner == Owner) Refresh(); }
        protected override void OnTurnOn() => Refresh();
        protected override void OnTurnOff() => Clear();
        protected override void OnApplyPostLoadFixes() { Clear(); Refresh(); }
    }

    [HarmonyPatch(typeof(UnitPartBuffSuppress), nameof(UnitPartBuffSuppress.ApplyChanges))]
    internal static class ScalesAndSkinSuppressionPatch
    {
        [HarmonyPostfix] static void Postfix(UnitPartBuffSuppress __instance)
        {
            var owner = __instance.Owner?.Unit;
            if (owner == null || owner.IsDisposed) return;
            foreach (var fact in owner.Facts.List.Where(f => f.Blueprint.AssetGuid == LinnormFeats.Ids[3] && T11TrainingOptions.Active(owner, f)))
                foreach (var runtime in fact.Components.Where(c => c.SourceBlueprintComponent is ScalesAndSkinEffect))
                    using (runtime.RequestEventContext()) ((ScalesAndSkinEffect)runtime.SourceBlueprintComponent).Refresh();
        }
    }
}
