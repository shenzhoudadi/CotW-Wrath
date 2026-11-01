using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicGhostbaneDirge
    {
        internal static void Build(PsychicAssets f)
        {
            const string text="An incorporeal creature coalesces into a semi-physical form for 1 round per caster level. It takes half damage from nonmagical physical attacks and full damage from magic weapons, spells, spell-like abilities and supernatural effects. This changes only its incorporeal damage reduction; other defenses and incorporeal traits remain. Will negates; spell resistance applies.";
            var buff=f.Buff("GhostbaneDirgeBuff","Ghostbane Dirge",text);
            Set(buff,"m_Icon",PsychicOriginalIcons.Load("GhostbaneDirge"));SetEnum(buff,"m_Flags","Harmful");
            Set(buff,"FxOnStart",new PrefabLink {AssetId="3cf209e5299921349a1c159f35cfa369"});
            var incorporeal=f.Native<BlueprintFeature>("c4a7f98d743bc784c9d4cf2105852c39");
            foreach(bool mass in new[]{false,true}) {
                string token=mass?"GhostbaneDirgeMass":"GhostbaneDirge";
                var apply=Apply(buff,0,DurationRate.Rounds);apply.AsChild=false;apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
                var rank=new ContextRankConfig {name=token+"Duration"};SetEnum(rank,"m_BaseValueType","CasterLevel");
                BlueprintComponent target;
                if(mass) {
                    var condition=new ContextConditionHasFact();Set(condition,"m_Fact",Ref<BlueprintUnitFactReference>(incorporeal));
                    var around=new AbilityTargetsAround {name=token+"Burst"};Set(around,"m_Radius",new Feet(15));Set(around,"m_TargetType",TargetType.Enemy);
                    Set(around,"m_Condition",new ConditionsChecker {Conditions=new Condition[]{condition}});target=around;
                } else {
                    var single=new AbilityTargetHasFact {name=token+"OnlyIncorporeal"};Set(single,"m_CheckedFacts",new[]{Ref<BlueprintUnitFactReference>(incorporeal)});target=single;
                }
                var spell=f.Ability(token,mass?"Ghostbane Dirge, Mass":"Ghostbane Dirge",mass?"Affects incorporeal enemies in a 15-foot burst within close range. "+text:text,
                    AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Close,
                    new SpellComponent {name=token+"School",School=SpellSchool.Transmutation},rank,target,
                    Save(SavingThrowType.Will,Saved(Array.Empty<GameAction>(),new GameAction[]{apply})),
                    new AbilitySpawnFx {name=token+"Visual",PrefabLink=new PrefabLink {AssetId="3cf209e5299921349a1c159f35cfa369"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
                spell.CanTargetSelf=false;spell.CanTargetFriends=false;spell.CanTargetEnemies=true;spell.CanTargetPoint=mass;spell.SpellResistance=true;
                spell.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach|Metamagic.Persistent|Metamagic.Piercing;
                Set(spell,"m_Icon",buff.Icon);spell.LocalizedDuration=Text(token,"duration","1 round per caster level");spell.LocalizedSavingThrow=Text(token,"savingThrow","Will negates");
            }
        }
        static readonly BlueprintGuid BuffId=Id("GhostbaneDirgeBuff");
        static Buff ActiveBuff(RuleCalculateDamage evt)=>evt?.Target?.Facts?.List.OfType<Buff>().FirstOrDefault(b=>b.IsActive&&!b.IsDisposed&&!b.IsSuppressed&&b.Blueprint.AssetGuid==BuffId);
        static bool MagicalEffect(RuleCalculateDamage evt)
        {
            var ability=evt.ParentRule?.Reason?.Ability?.Blueprint??evt.ParentRule?.Reason?.Context?.SourceAbility
                ??evt.Reason?.Ability?.Blueprint??evt.Reason?.Context?.SourceAbility;
            return evt.DamageBundle.Weapon==null&&ability!=null&&(ability.Type==AbilityType.Spell||ability.Type==AbilityType.SpellLike||ability.Type==AbilityType.Supernatural);
        }
        // Scope the change to the native incorporeal handler and an active spell
        // fact. No global immunity removal or saved unit part is necessary.
        [HarmonyPatch(typeof(AddIncorporealDamageDivisor),nameof(AddIncorporealDamageDivisor.OnEventAboutToTrigger))]
        internal static class DamagePatch
        {
            [HarmonyPrefix]
            internal static bool Prefix(RuleCalculateDamage evt)
            {
                var buff=ActiveBuff(evt);
                if(buff==null)return true;
                // Spell-triggered weapon attacks still use their actual weapon
                // enchantment. Direct magical physical damage is a spell effect.
                bool magicalEffect=MagicalEffect(evt);
                foreach(var damage in evt.DamageBundle)
                    if(damage.Reality!=DamageRealityType.Ghost&&damage is PhysicalDamage physical&&physical.EnchantmentTotal<=0&&!magicalEffect)
                        damage.AddDecline(new DamageDecline(DamageDeclineType.ByHalf,buff) {IncorporealDamageDivisor=true});
                return false;
            }
        }
        // Wrath's second component also adds Total/ByHalf to ordinary damage,
        // despite its name. Let its critical/precision logic execute, then edit
        // ONLY the ordinary declines this invocation appended. Earlier foreign
        // declines and actual precision immunity must survive, in either order.
        [HarmonyPatch(typeof(GhostCriticalAndPrecisionImmunity),nameof(GhostCriticalAndPrecisionImmunity.OnEventAboutToTrigger))]
        internal static class CriticalPatch
        {
            static readonly System.Reflection.FieldInfo CachedDecline=AccessTools.Field(typeof(BaseDamage),"m_Decline");
            [HarmonyPrefix]
            internal static void Prefix(RuleCalculateDamage evt,out Dictionary<BaseDamage,int> __state)
            {
                __state=ActiveBuff(evt)==null?null:evt.DamageBundle.ToDictionary(d=>d,d=>d.Declines.Count);
            }
            [HarmonyPostfix]
            internal static void Postfix(GhostCriticalAndPrecisionImmunity __instance,RuleCalculateDamage evt,Dictionary<BaseDamage,int> __state)
            {
                if(__state==null)return;
                bool magicalEffect=MagicalEffect(evt);
                foreach(var pair in __state) {
                    var damage=pair.Key;
                    if(damage.Precision)continue;
                    var declines=(List<DamageDecline>)damage.Declines;
                    bool changed=false;
                    for(int i=declines.Count-1;i>=pair.Value;i--) {
                        var decline=declines[i];
                        if(decline.SourceFact?.Blueprint is BlueprintScriptableObject source && source.GetComponents<GhostCriticalAndPrecisionImmunity>().Any(c=>ReferenceEquals(c,__instance))) {
                            if(damage.Reality!=DamageRealityType.Ghost&&damage is PhysicalDamage physical&&physical.EnchantmentTotal<=0&&!magicalEffect) {
                                decline.Type=DamageDeclineType.ByHalf;decline.IncorporealDamageDivisor=true;
                            } else declines.RemoveAt(i);
                            changed=true;
                        }
                    }
                    if(changed)CachedDecline.SetValue(damage,null);
                }
            }
        }
    }
}
