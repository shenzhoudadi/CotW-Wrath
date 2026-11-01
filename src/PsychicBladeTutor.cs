using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicBladeTutor
    {
        internal static void Build(PsychicAssets f)
        {
            const string text="An illusory tutor guides your melee attacks for 1 minute per caster level. Reduce the combined melee attack penalties from Power Attack, Piranha Strike, Combat Expertise, fighting defensively and two-weapon fighting by 1, plus 1 per 5 caster levels. This cannot grant a bonus beyond cancelling those penalties. It does not reduce penalties from hostile effects, secondary natural attacks or iterative attacks, and does not affect ranged attacks.";
            var icon=f.Native<BlueprintAbility>("bea9deffd3ab6734c9534153ddc70bde").Icon;
            var buff=f.Buff("BladeTutorBuff","Blade Tutor",text);Set(buff,"m_Icon",icon);
            var apply=Apply(buff,0,DurationRate.Minutes);apply.AsChild=false;apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
            var rank=new ContextRankConfig {name="BladeTutorDuration"};SetEnum(rank,"m_BaseValueType","CasterLevel");
            var spell=f.Ability("BladeTutor","Blade Tutor",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,
                new SpellComponent {name="BladeTutorSchool",School=SpellSchool.Conjuration},rank,Run(apply),
                new AbilitySpawnFx {name="BladeTutorVisual",PrefabLink=new PrefabLink {AssetId="930c1a4aa129b8344a40c8c401d99a04"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
            Set(spell,"m_Icon",icon);spell.SpellResistance=false;spell.NeedEquipWeapons=true;
            spell.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken;
            spell.LocalizedDuration=Text("BladeTutor","duration","1 minute per caster level");spell.LocalizedSavingThrow=Text("BladeTutor","savingThrow","");
        }
        // Exact native sources, including Wrath's attack-triggered child effects.
        // Never infer a penalty from the toggle being on: feats/mods may remove it.
        static readonly HashSet<BlueprintGuid> Sources=new HashSet<BlueprintGuid>(new[]{
            "5898bcf75a0942449a5dc16adc97b279","bfe8300529214c7ca4ae0024f834f085",
            "8af258b1dd322874ba6047b0c24660c7","b93ed538543945378613b228dfdd4578",
            "e81cd772a7311554090e413ea28ceea1","6ffd93355fb3bcf4592a5d976b1d32a9",
            "6948b379c0562714d9f6d58ccbfa8faa"}.Select(BlueprintGuid.Parse));
        static readonly BlueprintGuid FocusedStrikeId=BlueprintGuid.Parse("c632e516f383442489abb8ca810d639a");
        static bool PowerAttack(EntityFact f)=>f?.Blueprint!=null&&(f.Blueprint.AssetGuid==BlueprintGuid.Parse("5898bcf75a0942449a5dc16adc97b279")||f.Blueprint.AssetGuid==BlueprintGuid.Parse("bfe8300529214c7ca4ae0024f834f085"));
        static bool FocusedStrike(EntityFact f)=>f?.Blueprint?.AssetGuid==FocusedStrikeId;
        static readonly BlueprintGuid TutorId=Id("BladeTutorBuff");
        static bool Eligible(EntityFact fact)=>fact?.Blueprint!=null&&Sources.Contains(fact.Blueprint.AssetGuid);
        [HarmonyPatch(typeof(RuleCalculateAttackBonusWithoutTarget),nameof(RuleCalculateAttackBonusWithoutTarget.OnTrigger))]
        internal static class AttackPatch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.Last)]
            internal static void Prefix(RuleCalculateAttackBonusWithoutTarget __instance)
            {
                var evt=__instance;
                if(evt?.Weapon?.Blueprint==null||!evt.Weapon.Blueprint.IsMelee)return;
                var buff=evt.Initiator.Facts.List.OfType<Buff>().Where(b=>b.IsActive&&!b.IsDisposed&&!b.IsSuppressed&&b.Blueprint.AssetGuid==TutorId)
                    .OrderByDescending(b=>b.Context?.Params?.CasterLevel??0).FirstOrDefault();
                if(buff?.Context?.Params==null||evt.AllBonuses.Any(m=>m.Fact==buff))return;
                // Sum only the known voluntary sources. Positive offsets from the same
                // source also count, so a reduced/removed penalty is not paid twice.
                int penalty=-evt.AllBonuses.Where(m=>Eligible(m.Fact)).Sum(m=>m.Value);
                penalty-=evt.Initiator.Stats.AdditionalAttackBonus.Modifiers.Where(m=>Eligible(m.Source)).Sum(m=>m.ModValue);
                // Native Focused Strike cancels Power Attack via a separate positive
                // stat source. Consume that refund only against Power Attack, never
                // against an unrelated defensive/two-weapon penalty.
                int powerPenalty=-evt.AllBonuses.Where(m=>PowerAttack(m.Fact)).Sum(m=>m.Value)
                    -evt.Initiator.Stats.AdditionalAttackBonus.Modifiers.Where(m=>PowerAttack(m.Source)).Sum(m=>m.ModValue);
                int refunded=evt.AllBonuses.Where(m=>FocusedStrike(m.Fact)&&m.Value>0).Sum(m=>m.Value)
                    +evt.Initiator.Stats.AdditionalAttackBonus.Modifiers.Where(m=>FocusedStrike(m.Source)&&m.ModValue>0).Sum(m=>m.ModValue);
                penalty-=Math.Min(Math.Max(0,powerPenalty),refunded);
                int bonus=Math.Min(Math.Max(0,penalty),1+Math.Max(0,buff.Context.Params.CasterLevel)/5);
                if(bonus>0)evt.AddModifier(bonus,buff,ModifierDescriptor.UntypedStackable);
            }
        }
    }
}
