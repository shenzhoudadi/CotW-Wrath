using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicCurseSpells
    {
        internal static readonly string[] Tokens={"Screech","CurseMajor"};
        internal static void Build(PsychicAssets f)
        {
            const string screechText="Enemies within 30 feet must succeed at a Fortitude save or immediately provoke attacks of opportunity from creatures that threaten them. Allies are unaffected. The attacks use the threatening creatures' normal remaining attacks of opportunity and restrictions. Spell resistance applies.";
            var around=new AbilityTargetsAround {name="ScreechEnemies"};Set(around,"m_Radius",new Feet(30));Set(around,"m_TargetType",TargetType.Enemy);Set(around,"m_Condition",new ConditionsChecker {Conditions=Array.Empty<Condition>()});
            var screech=f.Ability("Screech","Screech",screechText,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,
                new SpellComponent {name="ScreechSchool",School=SpellSchool.Evocation},new SpellDescriptorComponent {name="ScreechSonic",Descriptor=SpellDescriptor.Sonic},around,
                Run(new ContextActionSavingThrow {Type=SavingThrowType.Fortitude,Actions=Actions(Saved(Array.Empty<GameAction>(),new GameAction[]{new ContextActionProvokeAttackOfOpportunity()}))}),
                new AbilitySpawnFx {name="ScreechFx",PrefabLink=new PrefabLink {AssetId="9a1f0fd6379b7d645a2d7742a947d7b5"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
            Set(screech,"m_Icon",f.Native<BlueprintAbility>("8e7cfa5f213a90549aadd18f8f6f4664").Icon);
            screech.SpellResistance=true;screech.AvailableMetamagic=Metamagic.Heighten|Metamagic.Quicken|Metamagic.Persistent|Metamagic.Piercing;
            screech.LocalizedDuration=Text("Screech","duration","Instantaneous");screech.LocalizedSavingThrow=Text("Screech","savingThrow","Fortitude negates");

            const string curseText="This spell functions as bestow curse, but works at close range without a touch attack. Choose one of the game's four bestow curse effects. The curse is permanent, and the DC to remove it is the spell's saving throw DC + 5. Will negates; spell resistance applies. The initial saving throw DC is not increased by this removal modifier.";
            var donors=new[]{f.Native<BlueprintAbility>("69851cc3b821c2d479ac1f2d86e8ffa5"),f.Native<BlueprintAbility>("0c853a9f35a7bf749821ebe5d06fade7"),f.Native<BlueprintAbility>("a9c4c2552b9a9564e8ee2eb3b20cc66d"),f.Native<BlueprintAbility>("f428939d629fa9942a55e1a144edd294")};
            var buffs=new[]{f.Native<BlueprintBuff>("caae9592917719a41b601b678a8e6ddf"),f.Native<BlueprintBuff>("c092750ba895e014cb24a25e2e8274a7"),f.Native<BlueprintBuff>("7fbb7799e8684434e80487cef9cc7f09"),f.Native<BlueprintBuff>("de92c96c86cb2cd4c8eb8e2881b84d99")};
            var effects=new[]{"-4 penalty on attack rolls, saving throws, ability checks and skill checks.","-6 penalty to Constitution.","-6 penalty to Intelligence, Wisdom and Charisma.","-6 penalty to Strength and Dexterity."};
            var names=new[]{"Deterioration","Feeble Body","Idiocy","Weakness"};var variants=new BlueprintAbility[4];
            for(int i=0;i<4;i++){
                var a=f.Clone(donors[i],"CurseMajor"+names[i],"Curse, Major: "+names[i],curseText+"\n"+effects[i]);
                var apply=Apply(buffs[i],0);apply.Permanent=true;apply.AsChild=false;
                a.ComponentsArray=a.ComponentsArray.Where(c=>c is SpellComponent||c is SpellDescriptorComponent||c is AbilitySpawnFx).Concat(new BlueprintComponent[]{Run(new ContextActionSavingThrow {Type=SavingThrowType.Will,Actions=Actions(Saved(Array.Empty<GameAction>(),new GameAction[]{new PsychicApplyMajorCurse {Apply=apply}}))})}).ToArray();
                a.Range=AbilityRange.Close;a.CanTargetEnemies=true;a.CanTargetFriends=true;a.CanTargetSelf=true;a.CanTargetPoint=false;a.SpellResistance=true;Set(a,"m_Parent",null);
                a.AvailableMetamagic=Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach|Metamagic.Persistent|Metamagic.Piercing;
                a.LocalizedDuration=Text(a.name,"duration","Permanent");a.LocalizedSavingThrow=Text(a.name,"savingThrow","Will negates");variants[i]=a;
            }
            var root=f.Clone(variants[0],"CurseMajor","Curse, Major",curseText);var choices=new AbilityVariants {name="MajorCurseChoices"};Set(choices,"m_Variants",variants.Select(Ref<BlueprintAbilityReference>).ToArray());
            root.ComponentsArray=new BlueprintComponent[]{new SpellComponent {name="MajorCurseSchool",School=SpellSchool.Necromancy},new SpellDescriptorComponent {name="MajorCurseDescriptor",Descriptor=SpellDescriptor.Curse},choices};
            root.LocalizedDuration=Text("CurseMajor","duration","Permanent");root.LocalizedSavingThrow=Text("CurseMajor","savingThrow","Will negates");
            foreach(var a in variants)Set(a,"m_Parent",Ref<BlueprintAbilityReference>(root));
        }
    }
    [TypeId("b364fe8e7e804514895983463b4883e8")]
    public sealed class PsychicApplyMajorCurse:ContextAction
    {
        public ContextActionApplyBuff Apply;
        public override string GetCaption()=>"Apply a curse with removal DC +5";
        public override void RunAction(){
            var p=Context.Params;
            var nested=new MechanicsContext(Context.MaybeCaster,Target.Unit?.Descriptor,Context.AssociatedBlueprint,Context,Target);
            nested.SetParams(new AbilityParams {CasterLevel=p.CasterLevel,SpellLevel=p.SpellLevel,DC=p.DC+5,Metamagic=p.Metamagic,Concentration=p.Concentration});
            using(nested.GetDataScope(Target))Apply.RunAction();
        }
    }
    // Native Blindness is Replace. Only reject a temporary replacement when the
    // existing native permanent status came from this wall; preserve its context/DC.
    [HarmonyPatch(typeof(BuffCollection),"PrepareFactForAttach",new[]{typeof(Buff)})]
    internal static class PsychicWallBlindnessDurationPatch
    {
        static bool Prefix(BuffCollection __instance,Buff __0,ref Buff __result){var fact=__0;
            if(fact?.Blueprint?.AssetGuid.ToString()!="187f88d96a0ef464280706b63635f2af"||!fact.PlannedDuration.HasValue||fact.PlannedDuration.Value==TimeSpan.MaxValue)return true;
            var old=__instance.Enumerable.FirstOrDefault(b=>b.Blueprint==fact.Blueprint&&b.IsPermanent&&b.Context?.SourceAbility?.AssetGuid==Id("WallOfBlindness"));
            if(old==null)return true;
            __result=null;return false;
        }
    }
}
