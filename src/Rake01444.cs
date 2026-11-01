// Adapted from Holic75/KingmakerRebalance (MIT), Archetypes/Rake.cs.
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01444;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Rake01444
    {
        internal const string ModuleName="Rogue archetype: Rake",RootToken="RakeArchetype01444",SmileToken="RakesSmile01444";
        internal const string DemoralizeId="5f3126d4120b2b244a95cb2ec23d69fb",BladeIconId="bd81a3931aa285a4f9844585b5d97e51",SmileIconId="fd4d9fd7f87575d47aafe2a64a6e2d8d";
        internal static string Token(int dice,string kind)=>"BravadosBlade"+dice+kind+"01444";
        internal static readonly string[] OwnTokens=new[]{RootToken,SmileToken}.Concat(Enumerable.Range(1,5).SelectMany(n=>new[]{"Feature","Toggle","Buff","Reduction"}.Select(k=>Token(n,k)))).ToArray();
        const string BladeText="When a rake hits an opponent and deals sneak attack damage, she can forgo 1d6 points of that damage and make a free Intimidate check to demoralize the foe. Starting from level 5 she can decide to forgo 1 additional sneak attack die + 1 more die per 4 levels thereafter. For every additional 1d6 points of sneak attack damage she forgoes, she receives a +5 circumstance bonus on this check.";
        internal static void Register(BlueprintsCache cache)
        {
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(RogueSwashbuckler01443.RogueId);
            if(Existing(cache,RootToken,cls))return;Vacant(cache,OwnTokens);
            var trap=f.Native<BlueprintFeature>(RogueSwashbuckler01443.TrapfindingId);var danger=f.Native<BlueprintFeature>(RogueSwashbuckler01443.DangerId);
            if(new[]{3,6,9,12,15,18}.Any(n=>cls.Progression.LevelEntries.Count(e=>e.Level==n&&e.Features.Any(x=>x?.AssetGuid==danger.AssetGuid))!=1))throw new InvalidOperationException("Rake native Rogue danger-sense progression changed.");
            var icon=f.Native<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(BladeIconId);var source=f.Native<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(DemoralizeId).GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Actions.Actions.OfType<Demoralize>().Single();
            var smile=f.Feature(SmileToken,"Rake’s Smile","At 3rd level, a rake gains a +1 morale bonus on Bluff and Diplomacy checks. This bonus increases by +1 for every 3 levels beyond 3rd.");smile.Ranks=6;smile.ReapplyOnLevelUp=true;
            var rank=new ContextRankConfig();Set(rank,"m_BaseValueType",ContextRankBaseValueType.FeatureRank);Set(rank,"m_Feature",Ref<BlueprintFeatureReference>(smile));
            smile.ComponentsArray=new BlueprintComponent[]{rank,new AddContextStatBonus{Stat=StatType.CheckBluff,Descriptor=ModifierDescriptor.Morale,Value=new ContextValue{ValueType=ContextValueType.Rank},Multiplier=1},new AddContextStatBonus{Stat=StatType.CheckDiplomacy,Descriptor=ModifierDescriptor.Morale,Value=new ContextValue{ValueType=ContextValueType.Rank},Multiplier=1}};Set(smile,"m_Icon",Get(f.Native<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(SmileIconId),"m_Icon"));
            var feats=new BlueprintFeature[5];
            for(int n=1;n<=5;n++) {
                // Retain old save GUIDs as inert hidden assets. No new attack
                // grants a temporary buff or changes CharacterStats.SneakAttack.
                var reduction=f.Buff(Token(n,"Reduction"),"","");SetEnum(reduction,"m_Flags","HiddenInUi");reduction.Stacking=StackingType.Replace;
                var demoralize=(Demoralize)CopyData(source);demoralize.Bonus=(n-1)*5;
                var buff=f.Buff(Token(n,"Buff"),"Bravado's Blade ("+new[]{"","I","II","III","IV","V"}[n]+")",BladeText,
                    new RakeStance01444{DiceToForgo=n,DemoralizeAction=Actions(demoralize)});
                buff.Stacking=StackingType.Replace;Set(buff,"m_Icon",Get(icon,"m_Icon"));
                var toggle=f.Add(new BlueprintActivatableAbility{Group=ActivatableAbilityGroup.None,ActivationType=AbilityActivationType.Immediately,DoNotTurnOffOnRest=true,DeactivateIfCombatEnded=false},Token(n,"Toggle"),"Bravado's Blade ("+new[]{"","I","II","III","IV","V"}[n]+")",BladeText);Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(buff));Set(toggle,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free);Set(toggle,"m_Icon",Get(icon,"m_Icon"));
                feats[n-1]=f.Feature(Token(n,"Feature"),"Bravado's Blade ("+new[]{"","I","II","III","IV","V"}[n]+")",BladeText,Facts(toggle));Set(feats[n-1],"m_Icon",Get(icon,"m_Icon"));
            }
            var additions=Enumerable.Range(0,5).Select(i=>Entry(1+4*i,feats[i])).Concat(new[]{3,6,9,12,15,18}.Select(n=>Entry(n,smile))).GroupBy(e=>e.Level).OrderBy(g=>g.Key).Select(g=>Entry(g.Key,g.SelectMany(e=>e.Features).ToArray())).ToArray();
            var root=f.Add(new BlueprintArchetype{LocalizedName=Text(RootToken,"name","Rake"),LocalizedDescription=Text(RootToken,"description","The rake is a rogue who is open about her skills and talents, often to the point of being boastful. Usually she has the protection of an important figure who finds her services useful, but sometimes her bravado is enough to keep enemies away. She is often used as a face for the group for diplomacy, gathering information, negotiations, or to gain the most lucrative contracts and quests from local authorities."),AddFeatures=additions,RemoveFeatures=new[]{Entry(1,trap)}.Concat(new[]{3,6,9,12,15,18}.Select(n=>Entry(n,danger))).ToArray(),ComponentsArray=Array.Empty<BlueprintComponent>()},RootToken,"Rake","The rake is a rogue who is open about her skills and talents, often to the point of being boastful. Usually she has the protection of an important figure who finds her services useful, but sometimes her bravado is enough to keep enemies away. She is often used as a face for the group for diplomacy, gathering information, negotiations, or to gain the most lucrative contracts and quests from local authorities.");Set(root,"m_ParentClass",cls);
            Publish(cache,f,OwnTokens,cls,root,feats.Concat(new[]{smile}).Cast<BlueprintFeatureBase>().ToArray());
        }
    }
    [TypeId("4eb457a72e154765897c2deed7561b85")]
    public sealed class RakeStance01444 : Kingmaker.UnitLogic.Buffs.Components.UnitBuffComponentDelegate,
        IInitiatorRulebookHandler<RulePrepareDamage>,IRulebookHandler<RulePrepareDamage>,ISubscriber,IInitiatorRulebookSubscriber
    {
        public int DiceToForgo;
        public ActionList DemoralizeAction;
        private static readonly ConditionalWeakTable<RuleAttackRoll,object> Processed=new ConditionalWeakTable<RuleAttackRoll,object>();
        protected override void OnTurnOn() {
            var legacy=Enumerable.Range(1,5).Select(n=>PsychicAssets.Id(Rake01444.Token(n,"Reduction"))).ToArray();
            foreach(var old in Owner.Buffs.Enumerable.Where(b=>legacy.Contains(b.Blueprint.AssetGuid)).ToArray())old.Remove();
            var ids=Enumerable.Range(1,5).Select(n=>PsychicAssets.Id(Rake01444.Token(n,"Toggle"))).ToArray();
            foreach(var ability in Owner.ActivatableAbilities.Enumerable.ToArray())
                if(ids.Contains(ability.Blueprint.AssetGuid)&&ability.Blueprint.Buff!=Buff.Blueprint){ability.IsOn=false;ability.Stop();}
        }
        public void OnEventAboutToTrigger(RulePrepareDamage evt) { }
        public void OnEventDidTrigger(RulePrepareDamage evt) {
            var roll=evt.ParentRule?.AttackRoll;
            if(DiceToForgo<=0 || roll==null || evt.Initiator!=Owner || evt.Target!=roll.Target ||
                !roll.IsHit || !roll.IsSneakAttack || roll.IsFake || roll.FortificationNegatesSneakAttack || Processed.TryGetValue(roll,out _))return;
            var sneak=evt.DamageBundle.Where(d=>d.Sneak&&d.Dice.ModifiedValue.Rolls>0).ToArray();
            if(sneak.Sum(d=>d.Dice.ModifiedValue.Rolls)<DiceToForgo)return;
            Processed.Add(roll,new object());int remaining=DiceToForgo;
            foreach(var damage in sneak) {
                var dice=damage.Dice.ModifiedValue;int remove=Math.Min(remaining,dice.Rolls);
                if(remove>0)damage.Dice.Modify(new DiceFormula(dice.Rolls-remove,dice.Dice),Fact);
                remaining-=remove;if(remaining==0)break;
            }
            Buff.RunActionInContext(DemoralizeAction,evt.Target);
        }
    }
}
