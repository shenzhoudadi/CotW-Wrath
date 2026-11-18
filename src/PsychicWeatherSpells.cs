using System;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicWeatherSpells
    {
        internal static readonly string[] Tokens={"ObscuringMist","FogCloud","SolidFog","SleetStorm","RiverOfWind","FickleWinds"};
        static ContextValue Rank()=>new ContextValue {ValueType=ContextValueType.Rank};
        static ContextRankConfig Scaling(){var r=new ContextRankConfig {name="WeatherDuration"};SetEnum(r,"m_BaseValueType","CasterLevel");return r;}
        internal static AbilityAreaEffectBuff Owned(BlueprintBuff buff,params Condition[] conditions){
            var c=new AbilityAreaEffectBuff {name=buff.name+"AreaOwner",Condition=new ConditionsChecker {Conditions=conditions},CheckConditionEveryRound=true};Set(c,"m_Buff",Ref<BlueprintBuffReference>(buff));return c;
        }
        internal static BlueprintBuff OwnedBuff(PsychicAssets f,string token,string title,string text,UnityEngine.Sprite icon,params BlueprintComponent[] components){var b=f.Buff(token,title,text,components);b.Stacking=StackingType.Stack;Set(b,"m_Icon",icon);return b;}
        // Every fog concealment buff uses the original CotW Fog Cloud icon so players recognize one effect.
        internal static UnityEngine.Sprite FogIcon(PsychicAssets f)=>PsychicOriginalIcons.Load("FogCloud");
        internal const string FogConcealmentText="Attacks against or by creatures inside the cloud have a 20% miss chance within 5 feet and 50% beyond 5 feet. Blindsight and abilities that ignore fog concealment work normally.";
        internal static BlueprintBuff ConcealmentBuff(PsychicAssets f,string token,string title,string text,params BlueprintComponent[] components)=>OwnedBuff(f,token,title,text,FogIcon(f),components);
        internal static BlueprintComponent[] Fog(bool total)=>total?new BlueprintComponent[]{
            new AddConcealment {name="WeatherIncomingTotal",Descriptor=ConcealmentDescriptor.Fog,Concealment=Concealment.Total},new PsychicOutgoingFog {name="WeatherOutgoingTotal",Total=true}
        }:new BlueprintComponent[]{
            new AddConcealment {name="WeatherIncomingPartial",Descriptor=ConcealmentDescriptor.Fog,Concealment=Concealment.Partial},
            new AddConcealment {name="WeatherIncomingDistant",Descriptor=ConcealmentDescriptor.Fog,Concealment=Concealment.Total,CheckDistance=true,DistanceGreater=new Feet(5)},new PsychicOutgoingFog {name="WeatherOutgoingFog"}
        };
        internal static BlueprintAbility AreaSpell(PsychicAssets f,string token,string title,string text,BlueprintAbilityAreaEffect area,UnityEngine.Sprite icon,SpellSchool school,AbilityRange range,DurationRate rate,SpellDescriptor descriptor=SpellDescriptor.None){
            var spawn=new ContextActionSpawnAreaEffect {DurationValue=new ContextDurationValue {Rate=rate,DiceCountValue=0,BonusValue=Rank()}};Set(spawn,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));
            var a=f.Ability(token,title,text,AbilityType.Spell,UnitCommand.CommandType.Standard,range,new SpellComponent {name=token+"School",School=school},Scaling(),Run(spawn),new SpellDescriptorComponent {name=token+"Descriptor",Descriptor=descriptor});
            if(area.Shape==AreaEffectShape.Cylinder){var radius=new AbilityAoERadius {name=token+"Radius"};Set(radius,"m_Radius",area.Size);SetEnum(radius,"m_TargetType","Any");a.ComponentsArray=a.ComponentsArray.Concat(new BlueprintComponent[]{radius}).ToArray();}
            a.CanTargetPoint=true;a.CanTargetSelf=true;a.CanTargetFriends=true;a.CanTargetEnemies=true;a.ShouldTurnToTarget=true;a.SpellResistance=area.SpellResistance;a.EffectOnAlly=a.EffectOnEnemy=AbilityEffectOnUnit.Harmful;SetEnum(a,"Animation","Point");Set(a,"m_Icon",icon);
            a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken;if(range!=AbilityRange.Long)a.AvailableMetamagic|=Metamagic.Reach;
            a.LocalizedDuration=Text(token,"duration",rate==DurationRate.TenMinutes?"10 minutes per caster level":rate==DurationRate.Minutes?"1 minute per caster level":"1 round per caster level");a.LocalizedSavingThrow=Text(token,"savingThrow","");return a;
        }
        internal static BlueprintAbilityAreaEffect Area(PsychicAssets f,string token,string title,string text,PrefabLink fx,AreaEffectShape shape,int size,bool sr,params BlueprintComponent[] components){
            var a=f.Add(new BlueprintAbilityAreaEffect {Shape=shape,Size=new Feet(size),AffectEnemies=true,AggroEnemies=true,SpellResistance=sr,Fx=fx,ComponentsArray=components},token+"Area",title,text);SetEnum(a,"m_TargetType","Any");Set(a,"m_TickRoundAfterSpawn",false);return a;
        }
        internal const string StinkingCloudAreaId="aa2e0a0fe89693f4e9205fd52c5ba3e5";
        internal static readonly string[] NativeFogAreaIds={
            StinkingCloudAreaId,
            "053c8fd966bbcb94bb8db6582d990650",
            "b342e42d2ed58484c8dff9150d18f4e4",
            "ba09d51375db5f34790184443416d84b",
            "6cae3c64989f3684bb078efcfa9021a1",
            "6fa0adacca8d00f4aaba1e8df77a318f",
            "706df636208b2864aa80032b72e0aabd",
            "6df1ac314d4e6e9418e34470b79f90d8",
            "1a62a0463e7d4da0814167e8308f1ebd",
            "b6017edf09fe0374fa9a9bf06135f84b"
        };
        internal const string ShadowyMistBuffId="8de8e078992d7a4479f3d76e21aa1195";
        internal static void Build(PsychicAssets f){Mist(f,0);Mist(f,1);Mist(f,2);NativeFogEffects(f);Sleet(f);River(f);Fickle(f);}
        static void NativeFogEffects(PsychicAssets f){
            const string text="Creatures inside have concealment (20% miss chance) against attacks within 5 feet and total concealment (50%) beyond 5 feet. Their own attacks suffer the same concealment. Blindsight and abilities that ignore fog concealment work normally. The cloud affects allies as well as enemies.";
            var fog=ConcealmentBuff(f,"NativeFogCloudConcealmentBuff","Fog Cloud Concealment",text,Fog(false));
            var shadowArea=Area(f,"ShadowyMistFog","Shadowy Mist Form",text,new PrefabLink(),AreaEffectShape.Cylinder,20,false,Owned(fog));
            shadowArea.AggroEnemies=false;
        }
        static void Mist(PsychicAssets f,int kind){
            bool obscuring=kind==0,solid=kind==2;
            string token=obscuring?"ObscuringMist":solid?"SolidFog":"FogCloud",title=obscuring?"Obscuring Mist":solid?"Solid Fog":"Fog Cloud";
            string placement=obscuring?"Create a stationary 20-foot-radius cloud centered on your current position for 1 minute per caster level.":solid?"Create a 20-foot-radius cloud within medium range for 1 minute per caster level.":"Create a 20-foot-radius cloud within medium range for 10 minutes per caster level.";
            string text=placement+" Creatures inside have concealment (20% miss chance) against attacks within 5 feet and total concealment (50%) beyond 5 feet. Their own attacks suffer the same concealment. Blindsight and abilities that ignore fog concealment work normally. "+(solid?"Creatures move at half speed and take -2 on melee attack and damage rolls. Ordinary ranged weapon attacks into or out of the fog automatically miss; rays are unaffected by this blocking effect. Overlapping solid fogs do not stack their penalties.":"The cloud affects allies as well as enemies.");
            var icon=kind==1?PsychicOriginalIcons.Load("FogCloud"):f.Native<BlueprintFeature>("3c53ee4965a13d74e81b37ae34f0861b").Icon;
            // Obscuring Mist and Fog Cloud carry nothing but concealment, so it lives on
            // the spell's own buff instead of a second listed buff. Solid Fog keeps a
            // separate fog buff because it adds speed/attack/ranged components.
            var fog=solid?ConcealmentBuff(f,token+"Fog",title+": Concealment",FogConcealmentText,Fog(false)):null;
            var components=solid?new BlueprintComponent[]{new AddCondition {name="SolidFogSlow",Condition=UnitCondition.Slowed},new PsychicSolidFogPenalty {name="SolidFogMeleePenalty"},new PsychicWeatherMiss {name="SolidFogRangedBlock",Incoming=true,Outgoing=true}}:Fog(false);
            var b=OwnedBuff(f,token+"Buff",title,text,icon,components);
            // Original mist prefab is absent from Wrath; use the available native Mind Fog visual.
            var area=Area(f,token,title,text,new PrefabLink {AssetId="597682efc0419a142a3174fd6bb408f7"},AreaEffectShape.Cylinder,20,false,solid?new[]{Owned(fog),Owned(b)}:new[]{Owned(b)});
            var spell=AreaSpell(f,token,title,text,area,icon,SpellSchool.Conjuration,obscuring?AbilityRange.Personal:AbilityRange.Medium,kind==1?DurationRate.TenMinutes:DurationRate.Minutes);
            if(obscuring){spell.CanTargetPoint=false;spell.CanTargetFriends=false;spell.CanTargetEnemies=false;spell.CanTargetSelf=true;spell.ShouldTurnToTarget=false;spell.AvailableMetamagic&=~Metamagic.Reach;SetEnum(spell,"Animation","Self");}
        }
        static ContextConditionHasFact Air(PsychicAssets f,bool not=false){var c=new ContextConditionHasFact {Not=not};Set(c,"m_Fact",Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("dd3d0c7f4f57f304cbdbb68170b1b775")));return c;}
        static void Sleet(PsychicAssets f){
            const string text="Create a 20-foot-radius sleet storm within long range for 1 round per caster level. Creatures inside suffer total fog concealment on incoming and outgoing attacks, move at half speed, and are protected from sight-based effects. On entering and each round, a DC 10 Mobility check is required to move; failure prevents movement until the next successful check or leaving the storm, and failure by 5 or more also knocks the creature prone. Air-subtype creatures ignore these effects. Ordinary ranged weapon attacks into or out of the storm miss (including air creatures); rays are not blocked. Allies are also affected.";
            var icon=PsychicOriginalIcons.Load("SleetStorm");
            var fog=ConcealmentBuff(f,"SleetStormFog","Sleet Storm: Concealment",FogConcealmentText,Fog(true));
            var b=OwnedBuff(f,"SleetStormBuff","Sleet Storm",text,icon,new AddCondition {name="SleetSlow",Condition=UnitCondition.Slowed},new SpellImmunityToSpellDescriptor {name="SleetSightSpellImmunity",Descriptor=SpellDescriptor.Blindness|SpellDescriptor.SightBased},new BuffDescriptorImmunity {name="SleetSightBuffImmunity",Descriptor=SpellDescriptor.SightBased});
            var block=OwnedBuff(f,"SleetStormRangedBuff","Sleet Storm: Winds",text,icon,new PsychicWeatherMiss {name="SleetRangedBlock",Incoming=true,Outgoing=true});
            var stuck=OwnedBuff(f,"SleetStormMovementBuff","Sleet Storm: Cannot Move",text,icon,new AddCondition {name="SleetFailedMovement",Condition=UnitCondition.CantMove});
            var check=new PsychicSleetMovementFailure();Set(check,"AirSubtype",Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("dd3d0c7f4f57f304cbdbb68170b1b775")));
            var area=Area(f,"SleetStorm","Sleet Storm",text,f.Native<BlueprintAbilityAreaEffect>("6ea87a0ff5df41c459d641326f9973d5").Fx,AreaEffectShape.Cylinder,20,false,Owned(fog,Air(f,true)),Owned(b,Air(f,true)),Owned(block),Owned(stuck,check));
            AreaSpell(f,"SleetStorm","Sleet Storm",text,area,icon,SpellSchool.Conjuration,AbilityRange.Long,DurationRate.Rounds);
        }
        static void River(PsychicAssets f){
            const string text="Create a 60-foot-long, 5-foot-wide river of wind within medium range for 1 round per caster level. Creatures in the line when it appears take 4d6 bludgeoning damage and fall prone; Fortitude halves damage and prevents falling. On subsequent rounds, occupants take 2d6 bludgeoning damage with the same save. Air-subtype creatures and creatures protected by Freedom of Movement are unaffected. Spell resistance applies. The wind can harm allies.";
            var immunity=new PsychicWindImmunity {AirSubtype=Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("dd3d0c7f4f57f304cbdbb68170b1b775")),Freedom=new[]{
                Ref<BlueprintBuffReference>(f.Native<BlueprintBuff>("1533e782fca42b84ea370fc1dcbf4fc1")),Ref<BlueprintBuffReference>(f.Native<BlueprintBuff>("235533b62159790499ced35860636bb2")),Ref<BlueprintBuffReference>(f.Native<BlueprintBuff>("60906dd9e4ddec14c8ac9a0f4e47f54c")),Ref<BlueprintBuffReference>(f.Native<BlueprintBuff>("67519ff6ba615c045afca2347608bfe3")),Ref<BlueprintBuffReference>(f.Native<BlueprintBuff>("d6fb42ec153f4d699e57891522d7f4c9"))}};
            GameAction Pulse(int dice)=>new Conditional {ConditionsChecker=new ConditionsChecker {Conditions=new Condition[]{immunity}},IfTrue=Actions(),IfFalse=Actions(new ContextActionSavingThrow {Type=SavingThrowType.Fortitude,Actions=Actions(new ContextActionDealDamage {DamageType=new DamageTypeDescription {Type=DamageType.Physical,Physical=new DamageTypeDescription.PhysicalData {Form=PhysicalDamageForm.Bludgeoning}},Value=new ContextDiceValue {DiceType=DiceType.D6,DiceCountValue=dice,BonusValue=0},HalfIfSaved=true,IsAoE=true,IgnoreCritical=true,DisableSneakDamage=true},Saved(Array.Empty<GameAction>(),new GameAction[]{new ContextActionKnockdownTarget()}))})};
            var run=new AbilityAreaEffectRunAction {name="RiverPulse",UnitEnter=Actions(),UnitExit=Actions(),UnitMove=Actions(),Round=Actions(new PsychicRiverPulse {First=Actions(Pulse(4)),Later=Actions(Pulse(2))})};
            var area=Area(f,"RiverOfWind","River of Wind",text,f.Native<BlueprintAbilityAreaEffect>("2eef9ca9e79968547a01d06d3828e17f").Fx,AreaEffectShape.Wall,60,true,run,new SpellDescriptorComponent {name="RiverMovement",Descriptor=SpellDescriptor.MovementImpairing});Set(area,"m_TickRoundAfterSpawn",true);
            var a=AreaSpell(f,"RiverOfWind","River of Wind",text,area,f.Native<BlueprintFeature>("2aad85320d0751340a0786de073ee3d5").Icon,SpellSchool.Evocation,AbilityRange.Medium,DurationRate.Rounds,SpellDescriptor.MovementImpairing);a.LocalizedSavingThrow=Text("RiverOfWind","savingThrow","Fortitude partial");a.AvailableMetamagic|=Metamagic.Empower|Metamagic.Maximize|Metamagic.Persistent|Metamagic.Piercing;
        }
        static void Fickle(PsychicAssets f){
            const string text="Protect allies in a 15-foot burst within medium range with mobile winds for 1 minute per caster level. Incoming ordinary ranged weapon attacks automatically miss. The protected creatures can fire their own ranged weapons normally. Melee attacks and rays are unaffected. As in Call of the Wild, thrown weapons count as ordinary ranged weapons.";
            var b=f.Buff("FickleWindsBuff","Fickle Winds",text,new PsychicWeatherMiss {name="FickleIncomingRanged",Incoming=true});Set(b,"m_Icon",f.Native<BlueprintFeature>("f2fa7541f18b8af4896fbaf9f2a21dfe").Icon);b.FxOnStart=new PrefabLink {AssetId="6dc97e33e73b5ec49bd03b90c2345d7f"};
            var targets=new AbilityTargetsAround {name="FickleAllyBurst"};Set(targets,"m_Radius",new Feet(15));SetEnum(targets,"m_TargetType","Ally");Set(targets,"m_Condition",new ConditionsChecker {Conditions=Array.Empty<Condition>()});
            var apply=Apply(b,0,DurationRate.Minutes);apply.DurationValue.BonusValue=Rank();
            var a=f.Ability("FickleWinds","Fickle Winds",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Medium,new SpellComponent {name="FickleSchool",School=SpellSchool.Transmutation},Scaling(),targets,Run(apply),new AbilitySpawnFx {name="FickleCastVisual",PrefabLink=b.FxOnStart,Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
            Set(a,"m_Icon",b.Icon);a.CanTargetPoint=true;a.CanTargetSelf=true;a.CanTargetFriends=true;a.CanTargetEnemies=false;a.NotOffensive=true;a.SpellResistance=false;a.ShouldTurnToTarget=true;SetEnum(a,"Animation","Point");a.EffectOnAlly=AbilityEffectOnUnit.Helpful;a.EffectOnEnemy=AbilityEffectOnUnit.None;a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken;a.LocalizedDuration=Text("FickleWinds","duration","1 minute per caster level");a.LocalizedSavingThrow=Text("FickleWinds","savingThrow","");
        }
    }
    [TypeId("809c43a0a18c45ffbbfdf89cb3453e1b")]
    public sealed class PsychicWeatherMiss : UnitBuffComponentDelegate,IInitiatorRulebookHandler<RuleAttackRoll>,ITargetRulebookHandler<RuleAttackRoll>,ISubscriber
    {
        public bool Incoming,Outgoing;
        public void OnEventAboutToTrigger(RuleAttackRoll e){if(e.Weapon!=null&&e.AttackType==AttackType.Ranged&&((Incoming&&e.Target==Owner)||(Outgoing&&e.Initiator==Owner)))e.AutoMiss=true;}
        public void OnEventDidTrigger(RuleAttackRoll e){}
    }
    [TypeId("e256460c8e684a408f8ed775351c092c")]
    public sealed class PsychicSolidFogPenalty : UnitBuffComponentDelegate,IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,IInitiatorRulebookHandler<RuleCalculateWeaponStats>,ISubscriber
    {
        bool First()=>Owner.Buffs.Enumerable.FirstOrDefault(b=>b.IsTurnedOn&&!b.IsSuppressed&&b.Blueprint.GetComponent<PsychicSolidFogPenalty>()!=null)==Buff;
        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget e){if(e.Weapon?.Blueprint.IsMelee==true&&First())e.AddModifier(-2,Fact,ModifierDescriptor.Penalty);}
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats e){if(e.Weapon?.Blueprint.IsMelee==true&&First())e.AddDamageModifier(-2,Fact,ModifierDescriptor.Penalty);}
        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget e){}
        public void OnEventDidTrigger(RuleCalculateWeaponStats e){}
    }
    [TypeId("f817b30437664058bdf9d0c4b2b20643")]
    public sealed class PsychicOutgoingFog : BlueprintComponent
    {
        public bool Total;
        static readonly System.Reflection.FieldInfo Concealments=HarmonyLib.AccessTools.Field(typeof(UnitPartConcealment),"m_Concealments");
        // Temporary native entries let Calculate preserve all native sensory exceptions.
        // The existing concealment patch removes them in its finalizer, including on exceptions.
        internal sealed class Scope : IDisposable
        {
            internal IDisposable Extra;
            internal UnitPartConcealment Part;internal UnitPartConcealment.ConcealmentEntry[] Entries=Array.Empty<UnitPartConcealment.ConcealmentEntry>();internal readonly System.Collections.Generic.List<UnitPartConcealment.ConcealmentEntry> Removed=new System.Collections.Generic.List<UnitPartConcealment.ConcealmentEntry>();
            public void Dispose(){
                Extra?.Dispose();Extra=null;
                if(Part==null)return;
                // Native RemoveConcealement compares values and can remove someone else's
                // identical entry. Remove only the exact temporary references we appended.
                var list=(System.Collections.Generic.List<UnitPartConcealment.ConcealmentEntry>)Concealments.GetValue(Part);
                if(list!=null){list.RemoveAll(e=>Entries.Any(owned=>ReferenceEquals(e,owned)));list.AddRange(Removed);}Part=null;
            }
        }
        internal static Scope Begin(UnitEntityData initiator,UnitEntityData target){
            if(initiator==null||target==null)return null;
            var own=target.Buffs.Enumerable.Where(b=>b.IsTurnedOn&&!b.IsSuppressed&&b.Context?.MaybeCaster==initiator).SelectMany(b=>b.Blueprint.GetComponents<PsychicHazeConcealment>()).ToArray();
            var fog=initiator.Buffs.Enumerable.Where(b=>b.IsTurnedOn&&!b.IsSuppressed).Select(b=>b.Blueprint.GetComponent<PsychicOutgoingFog>()).Where(c=>c!=null).ToArray();
            var haze=initiator.Buffs.Enumerable.Any(b=>b.IsTurnedOn&&!b.IsSuppressed&&b.Blueprint.GetComponent<PsychicHazeConcealment>()!=null&&b.Context?.MaybeCaster!=initiator);
            if(fog.Length==0&&!haze&&own.Length==0&&!KineticWindsight01433.Active(initiator))return null;
            var part=target.Ensure<UnitPartConcealment>();var scope=new Scope {Part=part};
            var list=(System.Collections.Generic.List<UnitPartConcealment.ConcealmentEntry>)Concealments.GetValue(part);
            foreach(var c in own){var entry=list?.FirstOrDefault(e=>e.Descriptor==ConcealmentDescriptor.Fog&&e.Concealment==c.Concealment&&e.DistanceGreater.Value==(c.CheckDistance?c.DistanceGreater.Value:0)&&e.RangeType==null&&!e.OnlyForAttacks);if(entry!=null){list.Remove(entry);scope.Removed.Add(entry);}}
            if(fog.Length>0||haze){scope.Entries=fog.Any(c=>c.Total)?new[]{new UnitPartConcealment.ConcealmentEntry {Descriptor=ConcealmentDescriptor.Fog,Concealment=Concealment.Total}}:new[]{new UnitPartConcealment.ConcealmentEntry {Descriptor=ConcealmentDescriptor.Fog,Concealment=Concealment.Partial},new UnitPartConcealment.ConcealmentEntry {Descriptor=ConcealmentDescriptor.Fog,Concealment=Concealment.Total,DistanceGreater=new Feet(5)}};foreach(var e in scope.Entries)part.AddConcealment(e);}
            scope.Extra=KineticWindsight01433.Filter(initiator,target);
            return scope;
        }
    }
    [TypeId("129b293e32924cefa3fcd4d18258b035")]
    public sealed class PsychicSleetMovementFailure : ContextCondition
    {
        public BlueprintUnitFactReference AirSubtype;
        protected override string GetConditionCaption()=>"Sleet Mobility check failed";
        protected override bool CheckCondition(){
            var u=Target.Unit;if(u==null||u.Descriptor.HasFact(AirSubtype.Get()))return false;
            var roll=Context.TriggerRule(new RuleSkillCheck(u,StatType.SkillMobility,10));if(roll.Success)return false;
            if(roll.DC-roll.RollResult>=5)new ContextActionKnockdownTarget().RunAction();return true;
        }
    }
    [TypeId("14950f21d6e74ffca5c267bf170a8468")]
    public sealed class PsychicWindImmunity : ContextCondition
    {
        public BlueprintUnitFactReference AirSubtype;public BlueprintBuffReference[] Freedom;
        protected override string GetConditionCaption()=>"Air subtype or Freedom of Movement";
        protected override bool CheckCondition(){var u=Target.Unit;return u!=null&&(u.Descriptor.HasFact(AirSubtype.Get())||u.Buffs.Enumerable.Any(b=>b.IsTurnedOn&&!b.IsSuppressed&&Freedom.Any(r=>r.Get()==b.Blueprint)));}
    }
    [TypeId("9880ae4850824890b4823b3543c410cb")]
    public sealed class PsychicRiverPulse : ContextAction
    {
        public ActionList First,Later;
        public override string GetCaption()=>"River of Wind initial or ongoing pulse";
        public override void RunAction(){var area=Kingmaker.ElementsSystem.ContextData<AreaEffectContextData>.Current?.Entity;if(area==null)return;(PsychicUnoccupiedWall.Initial(area)?First:Later).Run();}
    }
}
