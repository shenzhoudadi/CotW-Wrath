using System;
using System.Linq;
using Kingmaker.ElementsSystem;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicBloodyClaws
    {
        internal static void Build(PsychicAssets f)
        {
            const string text="For 1 minute per caster level, the touched living creature's natural attacks that deal piercing or slashing damage inflict bleed equal to half your caster level, capped by that attack's maximum normal physical damage. This spell does not grant natural attacks and does not affect manufactured or unarmed attacks. Repeated applications of this spell's bleed do not stack; only the strongest is kept. Healing ends the bleeding, which otherwise follows the game's normal bleed cleanup. Undead, constructs and elementals cannot receive this spell.";
            var icon=f.Native<BlueprintBuff>("4a51dca9d9456214e9a382b9e47385b3").Icon;
            var effect=f.Clone(f.Native<BlueprintBuff>("75039846c3d85d940aa96c249b97e562"),"BloodyClawsEffectBuff","Bloody Claws: Bleed",text);
            var round=effect.GetComponent<AddFactContextActions>();
            var damage=round?.NewRound?.Actions?.OfType<ContextActionDealDamage>().FirstOrDefault()
                ??throw new InvalidOperationException("Bloody Claws requires the native bleed round damage on 75039846c3d85d940aa96c249b97e562; registration cancelled before publication.");
            round.NewRound.Actions=round.NewRound.Actions.Where(a=>!(a is ContextActionDealDamage)||ReferenceEquals(a,damage)).ToArray();
            damage.Value=new ContextDiceValue {DiceType=DiceType.Zero,DiceCountValue=0,BonusValue=new ContextValue {ValueType=ContextValueType.Rank}};
            damage.IgnoreCritical=true;damage.DisableSneakDamage=true;
            var potency=new ContextRankConfig {name="BloodyClawsBleedPotency"};SetEnum(potency,"m_BaseValueType","CasterLevel");
            effect.ComponentsArray=effect.ComponentsArray.Concat(new BlueprintComponent[]{potency}).ToArray();effect.Stacking=StackingType.Replace;Set(effect,"m_Icon",icon);
            var trigger=new PsychicBloodyClawsHit {name="BloodyClawsNaturalHit",Effect=Ref<BlueprintBuffReference>(effect)};
            var buff=f.Buff("BloodyClawsBuff","Bloody Claws",text,trigger);Set(buff,"m_Icon",icon);Set(buff,"FxOnStart",new PrefabLink {AssetId="53c86872d2be80b48afc218af1b204d7"});
            var apply=Apply(buff,0,DurationRate.Minutes);apply.AsChild=false;apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
            var rank=new ContextRankConfig {name="BloodyClawsDuration"};SetEnum(rank,"m_BaseValueType","CasterLevel");
            var exclude=new AbilityTargetHasFact {name="BloodyClawsLivingOnly",Inverted=true};
            Set(exclude,"m_CheckedFacts",new[]{Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("734a29b693e9ec346ba2951b27987e33")),Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("fd389783027d63343b4a5634bd81645f")),Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("198fd8924dabcb5478d0f78bd453c586"))});
            var spell=f.Ability("BloodyClaws","Bloody Claws",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Touch,
                new SpellComponent {name="BloodyClawsSchool",School=SpellSchool.Necromancy},rank,exclude,Run(apply),
                new AbilitySpawnFx {name="BloodyClawsVisual",PrefabLink=new PrefabLink {AssetId="53c86872d2be80b48afc218af1b204d7"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
            spell.CanTargetSelf=true;spell.CanTargetFriends=true;spell.CanTargetEnemies=false;spell.SpellResistance=false;Set(spell,"m_Icon",icon);
            spell.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach;
            spell.LocalizedDuration=Text("BloodyClaws","duration","1 minute per caster level");spell.LocalizedSavingThrow=Text("BloodyClaws","savingThrow","");
        }
    }
    [AllowedOn(typeof(BlueprintBuff),false)]
    [TypeId("f1375aba77a94d1899ac8967fcb1baf0")]
    public sealed class PsychicBloodyClawsHit : UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleAttackWithWeapon>
    {
        public BlueprintBuffReference Effect;
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if(evt.Initiator!=Owner||evt.Target==null||evt.Weapon?.Blueprint?.Type?.IsNatural!=true||evt.Weapon.Blueprint.Type.IsUnarmed||evt.AttackRoll?.IsHit!=true)return;
            var hit=evt.MeleeDamage;
            var physical=hit?.DamageBundle?.WeaponDamage as Kingmaker.RuleSystem.Rules.Damage.PhysicalDamage;
            if(physical==null||(physical.Form&(PhysicalDamageForm.Slashing|PhysicalDamageForm.Piercing))==0||hit.IsFake||hit.ResultList==null||!hit.ResultList.Any(d=>ReferenceEquals(d.Source,physical)&&d.FinalValue>0))return;
            // Use the pre-critical weapon statistics, including size and damage
            // bonuses. Extra energy/precision packets cannot inflate the cap.
            var stats=evt.WeaponStats;if(stats?.WeaponDamageDice==null)return;
            int cap=Math.Max(0,stats.WeaponDamageDice.ModifiedValue.MaxValue(stats.DamageModifiers.TotalValue));
            int amount=Math.Min(Math.Max(0,(Context?.Params?.CasterLevel??0)/2),cap);if(amount<=0)return;
            var effect=Effect.Get();
            if(evt.Target.Facts.List.OfType<Buff>().Any(b=>b.IsActive&&!b.IsDisposed&&!b.IsSuppressed&&b.Blueprint==effect&&(b.Context?.Params?.CasterLevel??0)>=amount))return;
            // A dedicated ordinary MechanicsContext snapshots potency using the
            // engine's serialized params; never overwrite the caster's context.
            var context=new MechanicsContext(Context.MaybeCaster,evt.Target.Descriptor,effect,null,new TargetWrapper(evt.Target));
            context.SetParams(new AbilityParams {CasterLevel=amount});
            var applied=UnitHelper.AddBuff(evt.Target.Descriptor,effect,context,null);
            if(applied!=null){applied.IsFromSpell=true;applied.IsNotDispelable=true;}
        }
    }
}
