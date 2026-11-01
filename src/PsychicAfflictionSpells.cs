using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using UnityEngine;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicAfflictionSpells
    {
        private const Metamagic ControlMeta = Metamagic.Extend | Metamagic.Quicken | Metamagic.Reach |
            Metamagic.Heighten | Metamagic.Persistent | Metamagic.Piercing;
        internal static IEnumerable<PsychicSpells.Entry> Build(PsychicAssets f)
        {
            var sickened=f.Native<BlueprintBuff>("4e42460798665fd4cb9173ffa7ada323");
            const string painText="The target suffers 1d6 direct damage each round and is sickened for 1 round per caster level (maximum 10 rounds). Fortitude negates.";
            var child=Apply(sickened,0);child.Permanent=true;child.AsChild=true;child.IsNotDispelable=true;
            var pain=f.Buff("PainStrikeBuff","Pain Strike",painText,new AddFactContextActions {
                name="PainStrikeEachRound",Activated=Actions(child),Deactivated=Actions(),Dispose=Actions(),
                NewRound=Actions(new ContextActionDealDamage {name="PainStrikeDamage",IgnoreCritical=true,
                    DamageType=new DamageTypeDescription {Type=DamageType.Direct},
                    Value=new ContextDiceValue {DiceType=DiceType.D6,DiceCountValue=1,BonusValue=0}})});
            Set(pain,"m_Icon",sickened.Icon);SetEnum(pain,"m_Flags","Harmful");
            // Wrath has no Pain descriptor; do not reuse CotW's numeric bit,
            // which belongs to unrelated Wrath descriptors. Sickened keeps its
            // own native immunities; direct damage and Evil remain independent.
            yield return Debuff(f,"PainStrike","Pain Strike",painText,pain,2,SpellSchool.Evocation,
                SpellDescriptor.Evil,SavingThrowType.Fortitude,10,false,ControlMeta|Metamagic.Empower|Metamagic.Maximize);
            yield return Debuff(f,"PainStrikeMass","Pain Strike, Mass",painText,pain,4,SpellSchool.Evocation,
                SpellDescriptor.Evil,SavingThrowType.Fortitude,10,true,ControlMeta|Metamagic.Empower|Metamagic.Maximize);

            const string sensesText="Overstimulate the target's senses for 1 round per caster level. It moves at half speed, suffers a 20% miss chance on attacks, and takes a -4 penalty to AC, skill checks and Reflex saves. Casting a spell requires a concentration check (DC 15 + spell level). Will negates.";
            var senses=f.Buff("SynesthesiaBuff","Synesthesia",sensesText,
                new AddStatBonus {name="SynesthesiaAC",Stat=StatType.AC,Value=-4,Descriptor=ModifierDescriptor.UntypedStackable},
                new AddStatBonus {name="SynesthesiaReflex",Stat=StatType.SaveReflex,Value=-4,Descriptor=ModifierDescriptor.UntypedStackable},
                new BuffAllSkillsBonus {name="SynesthesiaSkills",Value=-4,Multiplier=1,Descriptor=ModifierDescriptor.UntypedStackable},
                new AddCondition {name="SynesthesiaHalfSpeed",Condition=UnitCondition.Slowed},
                new AddCondition {name="SynesthesiaConcentration",Condition=UnitCondition.SpellCastingIsDifficult},
                new SpellDescriptorComponent {name="SynesthesiaMind",Descriptor=SpellDescriptor.MindAffecting});
            Set(senses,"m_Icon",f.Native<BlueprintAbility>("55f14bc84d7c85446b07a1b5dd6b2b4c").Icon);
            SetEnum(senses,"m_Flags","Harmful");
            senses.FxOnStart=new PrefabLink {AssetId="396af91a93f6e2b468f5fa1a944fae8a"};
            yield return Debuff(f,"Synesthesia","Synesthesia",sensesText,senses,3,SpellSchool.Illusion,
                SpellDescriptor.MindAffecting,SavingThrowType.Will,0,false,ControlMeta);
            yield return Debuff(f,"SynesthesiaMass","Synesthesia, Mass",sensesText,senses,7,SpellSchool.Illusion,
                SpellDescriptor.MindAffecting,SavingThrowType.Will,0,true,ControlMeta);
            yield return Synapse(f);
        }
        private static ContextRankConfig CasterRank(string name,int cap,bool intensified=false)
        {
            var rank=new ContextRankConfig {name=name};SetEnum(rank,"m_BaseValueType","CasterLevel");
            Set(rank,"m_UseMax",cap>0);Set(rank,"m_Max",cap);Set(rank,"m_AffectedByIntensifiedMetamagic",intensified);return rank;
        }
        private static PsychicSpells.Entry Debuff(PsychicAssets f,string token,string name,string text,BlueprintBuff buff,
            int circle,SpellSchool school,SpellDescriptor descriptor,SavingThrowType save,int cap,bool mass,Metamagic meta)
        {
            if(mass) text="Affects enemies in a 15-foot burst within medium range. "+text;
            var apply=Apply(buff,0);apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
            var spell=f.Ability(token,name,text,AbilityType.Spell,UnitCommand.CommandType.Standard,mass?AbilityRange.Medium:AbilityRange.Close,
                new SpellComponent {name=token+"School",School=school},new SpellDescriptorComponent {name=token+"Descriptor",Descriptor=descriptor},
                CasterRank(token+"Duration",cap),Save(save,Saved(Array.Empty<GameAction>(),new GameAction[]{apply})));
            spell.CanTargetSelf=false;spell.CanTargetFriends=false;spell.CanTargetEnemies=true;spell.CanTargetPoint=mass;
            spell.EffectOnAlly=AbilityEffectOnUnit.Harmful;spell.SpellResistance=true;spell.AvailableMetamagic=meta;
            Set(spell,"m_Icon",buff.Icon);
            spell.LocalizedDuration=Text(token,"duration","1 round per caster level"+(cap>0?" (maximum 10 rounds)":""));
            spell.LocalizedSavingThrow=Text(token,"savingThrow",save==SavingThrowType.Will?"Will negates":"Fortitude negates");
            spell.ComponentsArray=spell.ComponentsArray.Concat(new BlueprintComponent[]{new AbilitySpawnFx {
                name=token+"CastVisual",PrefabLink=new PrefabLink {AssetId="c388856d0e8855f429a83ccba67944ba"},
                Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,
                PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None}}).ToArray();
            if(mass) {
                var targets=new AbilityTargetsAround {name=token+"Targets"};Set(targets,"m_Radius",new Feet(15));Set(targets,"m_TargetType",TargetType.Enemy);
                Set(targets,"m_Condition",new ConditionsChecker {Conditions=Array.Empty<Condition>()});
                spell.ComponentsArray=spell.ComponentsArray.Concat(new BlueprintComponent[]{targets}).ToArray();
            }
            return new PsychicSpells.Entry {Token=token,Level=circle,Ability=spell};
        }
        private static Sprite synapseIcon;
        internal static Sprite SynapseIcon()
        {
            if(!ReferenceEquals(synapseIcon,null))return synapseIcon;
            using(var stream=typeof(PsychicAfflictionSpells).Assembly.GetManifestResourceStream("CotW.TemporalStasis.png")) {
                if(stream==null)throw new InvalidOperationException("Missing CotW Synapse Overload icon.");
                using(var bytes=new MemoryStream()) {
                    stream.CopyTo(bytes);var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    if(!ImageConversion.LoadImage(texture,bytes.ToArray()))throw new InvalidOperationException("Invalid Synapse Overload icon.");
                    synapseIcon=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(0.5f,0.5f));
                }
            }
            return synapseIcon;
        }
        private static PsychicSpells.Entry Synapse(PsychicAssets f)
        {
            const string text="A melee touch attack overloads the target's synapses, dealing 1d6 electricity damage per caster level (maximum 15d6) and staggering it for 1 minute. Fortitude negates only the staggered effect, not the damage. A missed touch retains the charge.";
            var effect=f.Clone(f.Native<BlueprintAbility>("17451c1327c571641a1345bd31155209"),"SynapseOverloadTouch","Synapse Overload",text);
            effect.ComponentsArray=effect.ComponentsArray.Where(c=>c is AbilitySpawnFx || c.GetType().Name=="AbilityDeliverTouch").Concat(new BlueprintComponent[]{
                new SpellComponent {name="SynapseDivination",School=SpellSchool.Divination},
                new SpellDescriptorComponent {name="SynapseMind",Descriptor=SpellDescriptor.MindAffecting},
                CasterRank("SynapseDamageDice",15,true),
                Save(SavingThrowType.Fortitude,new ContextActionDealDamage {name="SynapseElectricDamage",
                    DamageType=new DamageTypeDescription {Type=DamageType.Energy,Energy=DamageEnergyType.Electricity},
                    Value=new ContextDiceValue {DiceType=DiceType.D6,DiceCountValue=new ContextValue {ValueType=ContextValueType.Rank},BonusValue=0},HalfIfSaved=false},
                    Saved(Array.Empty<GameAction>(),new GameAction[]{Apply(f.Native<BlueprintBuff>("df3950af5a783bd4d91ab73eb8fa0fd3"),1,DurationRate.Minutes)}))}).ToArray();
            var cast=f.Clone(f.Native<BlueprintAbility>("ab395d2335d3f384e99dddee8562978f"),"SynapseOverload","Synapse Overload",text);
            var sticky=new AbilityEffectStickyTouch {name="SynapseHeldCharge"};Set(sticky,"m_TouchDeliveryAbility",Ref<BlueprintAbilityReference>(effect));
            cast.ComponentsArray=new BlueprintComponent[]{sticky,new SpellComponent {name="SynapseDivination",School=SpellSchool.Divination},
                new SpellDescriptorComponent {name="SynapseMind",Descriptor=SpellDescriptor.MindAffecting}};
            foreach(var spell in new[]{cast,effect}) {
                Set(spell,"m_Parent",new BlueprintAbilityReference());Set(spell,"m_Icon",SynapseIcon());
                spell.SpellResistance=true;spell.CanTargetFriends=false;spell.CanTargetSelf=false;spell.CanTargetEnemies=true;
                spell.AvailableMetamagic=(ControlMeta & ~Metamagic.Extend)|Metamagic.Empower|Metamagic.Maximize|Metamagic.Intensified;
                spell.LocalizedDuration=Text("SynapseOverload","duration","Instantaneous; staggered for 1 minute");
                spell.LocalizedSavingThrow=Text("SynapseOverload","savingThrow","Fortitude partial");
            }
            return new PsychicSpells.Entry {Token="SynapseOverload",Level=5,Ability=cast};
        }
    }

    // The native PartialConcealmentOnAttacks condition replaces total concealment
    // with partial. Keep stronger concealment and restrict this correction to
    // the caster's active Synesthesia buff; other abilities use native behavior.
    [HarmonyPatch(typeof(UnitPartConcealment),nameof(UnitPartConcealment.Calculate))]
    internal static class PsychicSynesthesiaConcealmentPatch
    {
        private static readonly BlueprintGuid BuffId=Id("SynesthesiaBuff");
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyBefore("TabletopTweaks-Base")]
        private static void Prefix(UnitEntityData initiator,UnitEntityData target,out PsychicOutgoingFog.Scope __state){__state=PsychicOutgoingFog.Begin(initiator,target);}
        [HarmonyFinalizer]
        private static void Finalizer(PsychicOutgoingFog.Scope __state){__state?.Dispose();}
        [HarmonyPostfix]
        private static void Postfix(UnitEntityData initiator,bool attack,ref Concealment __result)
        {
            if(!attack || __result!=Concealment.None || initiator==null)return;
            if(!initiator.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==BuffId && b.IsTurnedOn && !b.IsSuppressed))return;
            var senses=initiator.Get<UnitPartConcealment>();
            if(senses!=null && (senses.IgnoreAll || senses.IgnorePartial))return;
            __result=Concealment.Partial;
        }
    }
}

