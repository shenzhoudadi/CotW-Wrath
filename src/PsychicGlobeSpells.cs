using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Controllers;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicGlobeSpells
    {
        static readonly BlueprintGuid LesserBuff=Id("GlobeOfInvulnerabilityLesserBuff"),GreaterBuff=Id("GlobeOfInvulnerabilityBuff");
        static readonly FieldInfo CreationTime=AccessTools.Field(typeof(AreaEffectEntityData),"m_CreationTime");
        internal static IEnumerable<PsychicSpells.Entry> Build(PsychicAssets f)
        {
            for(int i=0;i<2;i++) {
                string token=i==0?"GlobeOfInvulnerabilityLesser":"GlobeOfInvulnerability",title=i==0?"Globe of Invulnerability, Lesser":"Globe of Invulnerability";
                string text="Create a stationary 13-foot-radius globe centered on your current position for 1 round per caster level. Creatures inside are protected from newly applied spells and spell-like abilities of level "+(i+3)+" or lower, including cantrips and spells from items. This blocks helpful as well as harmful effects, regardless of spell resistance. Spells can be cast out of or through the globe and affect creatures outside normally. Higher-level spells and supernatural abilities are unaffected. Heighten Spell changes the spell level used for this protection; other metamagic does not. Existing buffs and areas created before the globe are not removed or suppressed. Area dispelling can remove the globe. The globe stays behind if you move; leaving ends its protection, and returning restores it. Uses Call of the Wild's 13-foot area adaptation.";
                var buff=f.Buff(token+"Buff",title,text);buff.Stacking=StackingType.Stack;Set(buff,"m_Icon",PsychicOriginalIcons.Load("Metamixing"));
                var protection=new AbilityAreaEffectBuff {name=token+"AreaOwnedProtection",Condition=new ConditionsChecker {Conditions=Array.Empty<Condition>()}};Set(protection,"m_Buff",Ref<BlueprintBuffReference>(buff));
                var area=f.Add(new BlueprintAbilityAreaEffect {Shape=AreaEffectShape.Cylinder,Size=new Feet(13),AffectEnemies=true,AggroEnemies=false,SpellResistance=false,Fx=new PrefabLink {AssetId="cda35ba5c34a61b499f5858eabcedec7"},ComponentsArray=new BlueprintComponent[]{protection}},token+"Area",title,text);SetEnum(area,"m_TargetType","Any");
                var spawn=new ContextActionSpawnAreaEffect {name=token+"StationaryArea",OnUnit=false,DurationValue=new ContextDurationValue {Rate=DurationRate.Rounds,DiceType=Kingmaker.RuleSystem.DiceType.Zero,DiceCountValue=0,BonusValue=new ContextValue {ValueType=ContextValueType.Rank}}};Set(spawn,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));
                var rank=new ContextRankConfig {name=token+"Duration"};SetEnum(rank,"m_BaseValueType","CasterLevel");
                var radius=new AbilityAoERadius {name=token+"Radius"};Set(radius,"m_Radius",new Feet(13));Set(radius,"m_TargetType",TargetType.Any);
                var spell=f.Ability(token,title,text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,new SpellComponent {name=token+"School",School=SpellSchool.Abjuration},rank,Run(spawn),radius,new AbilitySpawnFx {name=token+"CastVisual",PrefabLink=new PrefabLink {AssetId="c4d861e816edd6f4eab73c55a18fdadd"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.Caster,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
                spell.SpellResistance=false;spell.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken;Set(spell,"m_Icon",buff.Icon);spell.LocalizedDuration=Text(token,"duration","1 round per caster level");
                yield return new PsychicSpells.Entry {Ability=spell,Level=i==0?4:6};
            }
        }
        // Read native area-owned facts rather than keeping a second, serialized
        // registry. Native area exit/expiration and suppression own their lifetime.
        internal static bool Blocks(UnitEntityData target,MechanicsContext context,AreaEffectEntityData incomingArea=null)
        {
            if(target?.Buffs==null || context?.Params==null)return false;
            var source=context.SourceAbility;
            if(source==null || (source.Type!=AbilityType.Spell && source.Type!=AbilityType.SpellLike))return false;
            int level=context.Params.SpellLevel;if(level<0 || level>4)return false;
            foreach(var buff in target.Buffs.Enumerable) {
                if(!buff.IsTurnedOn || buff.IsSuppressed)continue;
                int limit=buff.Blueprint.AssetGuid==GreaterBuff?4:buff.Blueprint.AssetGuid==LesserBuff?3:-1;
                if(level>limit)continue;
                if(incomingArea!=null && !string.IsNullOrEmpty(buff.SourceAreaEffectId)) {
                    var globe=Game.Instance?.State?.AreaEffects.FirstOrDefault(a=>a.UniqueId==buff.SourceAreaEffectId);
                    if(globe!=null && (TimeSpan)CreationTime.GetValue(incomingArea)<(TimeSpan)CreationTime.GetValue(globe))continue;
                }
                return true;
            }
            return false;
        }
        // Touch preparation and creation of a persistent area must still occur;
        // protection is checked against the eventual target/area occupant.
        internal static bool IsPreparation(AbilityExecutionContext context,AbilityApplyEffect effect)
            =>context?.AbilityBlueprint?.StickyTouch!=null ||
              (effect is AbilityEffectRunAction run && run.Actions?.Actions?.Length>0 && run.Actions.Actions.All(a=>a is ContextActionSpawnAreaEffect));
        [HarmonyPatch(typeof(AbilityExecutionProcess),"DoApplyEffect")]
        internal static class DeliveryPatch
        {
            static bool Prefix(AbilityExecutionContext __0,TargetWrapper __1,AbilityApplyEffect __2)
                =>IsPreparation(__0,__2) || (!PsychicFlightGroundPatch.BlocksTar(__1?.Unit,__0) && !Blocks(__1?.Unit,__0));
        }
        [HarmonyPatch(typeof(AreaEffectEntityData),"TryOvercomeSpellResistance")]
        internal static class AreaPatch
        {
            static bool Prefix(AreaEffectEntityData __instance,UnitEntityData unit,ref bool __result)
            {
                if(!Blocks(unit,__instance.Context,__instance))return true;
                __result=false;return false;
            }
        }
    }
}
