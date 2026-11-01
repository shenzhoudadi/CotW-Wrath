using System;
using System.Linq;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Properties;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class AngerInquisition01431
    {
        internal const string ModuleName = "Inquisition: Anger";
        internal const string Token = "AngerInquisition";
        internal const string RageToken = "AngerDivineRageBuff";
        internal const string GreaterId = "ce49c579fe0bcc647a32c96929fae982";
        internal const string MightyId = "06a7e5b60020ad947aed107d82d1f897";
        internal const string TirelessId = "ca9343d75a83a2745a22fa11c383153a";

        static void Icon(Kingmaker.Blueprints.Facts.BlueprintUnitFact fact, Kingmaker.Blueprints.Facts.BlueprintUnitFact source) => Set(fact,"m_Icon",Get(source,"m_Icon"));
        static AddAbilityResources Grant(BlueprintAbilityResource resource) {
            var c=new AddAbilityResources { RestoreAmount=true, RestoreOnLevelUp=true }; Set(c,"m_Resource",Ref<BlueprintAbilityResourceReference>(resource)); return c;
        }
        static BlueprintAbilityResource Resource(PsychicAssets a,string token,string label,BlueprintCharacterClass[] classes,bool rage) {
            var r=a.Add(new BlueprintAbilityResource(),token,label,label); var v=Get(r,"m_MaxAmount");
            Set(v,"BaseValue",rage?-4:1); Set(v,"IncreasedByLevel",rage); Set(v,"LevelIncrease",1);
            Set(v,"IncreasedByStat",rage); Set(v,"ResourceBonusStat",StatType.Wisdom);
            Set(v,"m_Class",rage?classes.Select(Ref<BlueprintCharacterClassReference>).ToArray():Array.Empty<BlueprintCharacterClassReference>());
            Set(v,"m_ClassDiv",Array.Empty<BlueprintCharacterClassReference>());
            Set(v,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>()); Set(v,"m_ArchetypesDiv",Array.Empty<BlueprintArchetypeReference>());
            Set(r,"m_MaxAmount",v); Set(r,"m_Min",0); r.IsDomainAbility=true;
            r.LocalizedName=Text(token,"name",label); r.LocalizedDescription=Text(token,"description",rage?"Divine Anger rounds: cleric/inquisitor levels minus 4 plus Wisdom modifier (minimum 0).":"Hateful Retort: one daily use."); return r;
        }
        static LevelEntry Entry(int level,params BlueprintFeatureBase[] facts) {
            var e=new LevelEntry { Level=level }; Set(e,"m_Features",facts.Select(Ref<BlueprintFeatureBaseReference>).ToList()); return e;
        }
        internal static void Register(BlueprintsCache cache) {
            var a=new PsychicAssets(cache);
            var classes=Inquisitions01426.ClassIds.Select(a.Native<BlueprintCharacterClass>).ToArray();
            var rageSource=a.Native<BlueprintBuff>("da8ce41ac3cd74742b80984ccc3c9613");
            var toggleSource=a.Native<BlueprintActivatableAbility>("df6a2cce8e3a9bd4592fb1968b83f730");
            if(rageSource.GetComponents<TemporaryHitPointsPerLevel>().Count()!=1 || toggleSource.GetComponents<ActivatableAbilityResourceLogic>().Count()!=1)
                throw new InvalidOperationException("Anger requires the current native rage HP/resource layout; original rage preserved.");
            var greater=a.Native<BlueprintFeature>(GreaterId); var tireless=a.Native<BlueprintFeature>(TirelessId);
            var retortUses=Resource(a,"AngerHatefulRetortUses","Hateful Retort",classes,false);
            const string retortText="Once per day, enable this option to consent to a free melee counterattack after an enemy hits you with a real melee weapon attack. The enemy must still be in reach and you must be able to attack. This is one attack at your highest attack bonus; it spends no standard, swift or opportunity-attack allowance. A legal queued counterattack spends the daily use; a miss, ranged hit, preview or illegal target does not.";
            var retort=a.Buff("AngerHatefulRetortBuff","Hateful Retort",retortText,new AngerHatefulRetort01431 { Resource=Ref<BlueprintAbilityResourceReference>(retortUses) });
            Icon(retort,rageSource);
            var gate=new ActivatableAbilityResourceLogic { SpendType=ActivatableAbilityResourceLogic.ResourceSpendType.Never }; Set(gate,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(retortUses));
            var option=a.Add(new BlueprintActivatableAbility { ActivationType=AbilityActivationType.Immediately, Group=ActivatableAbilityGroup.None, DeactivateImmediately=true, DoNotTurnOffOnRest=true, ComponentsArray=new BlueprintComponent[] { gate } },"AngerHatefulRetortToggle","Hateful Retort: Allow Counterattack",retortText);
            Set(option,"m_Buff",Ref<BlueprintBuffReference>(retort)); Set(option,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free); Icon(option,rageSource);
            var retortFeature=a.Feature("AngerHatefulRetortFeature","Hateful Retort",retortText,Facts(option),Grant(retortUses)); Icon(retortFeature,rageSource);

            const string rageText="At level 6, rage using the current game's native rage effects, with effective barbarian level equal to cleric/inquisitor level minus 3. Rounds per day are Wisdom modifier plus one per cleric/inquisitor level above 4 (minimum 0). This grants no rage powers, but retains powers you already possess. Greater rage begins at level 14 and tireless rage at level 20. Divine Anger has its own resource and does not refill ordinary barbarian rage.";
            var rageUses=Resource(a,"AngerDivineRageUses","Divine Anger",classes,true);
            var hp=a.Add(new BlueprintUnitProperty { ComponentsArray=new BlueprintComponent[] { new AngerRageHitPoints01431 { Settings=new PropertySettings(), Classes=classes.Select(Ref<BlueprintCharacterClassReference>).ToArray(), Greater=Ref<BlueprintFeatureReference>(greater), Mighty=Ref<BlueprintFeatureReference>(a.Native<BlueprintFeature>(MightyId)), Limitless=Ref<BlueprintUnitFactReference>(a.Native<Kingmaker.Blueprints.Facts.BlueprintUnitFact>("5cb58e6e406525342842a073fb70d068")), Resource=Ref<BlueprintAbilityResourceReference>(rageUses) } } },"AngerRageHitPointsProperty","","");
            var rage=a.Clone(rageSource,RageToken,"Divine Anger",rageText);
            var hpValue=new ContextValue { ValueType=ContextValueType.CasterCustomProperty }; Set(hpValue,"m_CustomProperty",Ref<BlueprintUnitPropertyReference>(hp));
            rage.ComponentsArray=rage.ComponentsArray.Select(c=>c is TemporaryHitPointsPerLevel ? (BlueprintComponent)new TemporaryHitPointsFromAbilityValue { Descriptor=ModifierDescriptor.Rage, Value=hpValue, RemoveWhenHitPointsEnd=false } : c).ToArray();
            var toggle=a.Clone(toggleSource,"AngerDivineRageToggle","Divine Anger",rageText); Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(rage));
            foreach(var c in toggle.GetComponents<ActivatableAbilityResourceLogic>()) Set(c,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(rageUses));
            toggle.DeactivateIfCombatEnded=true; toggle.DeactivateImmediately=false;
            FerociousBeast01431.ConfigureDivine(a,rage,rageUses);
            var rageFeature=a.Feature("AngerDivineRageFeature","Divine Anger",rageText,Facts(toggle),Grant(rageUses)); Icon(rageFeature,rageSource);
            var root=a.Add(new BlueprintProgression { Ranks=1, IsClassFeature=true, Groups=new[] { FeatureGroup.Domain }, GiveFeaturesForPreviousLevels=true,
                LevelEntries=new[] { Entry(1,retortFeature),Entry(6,rageFeature),Entry(14,greater),Entry(20,tireless) },
                ComponentsArray=new BlueprintComponent[] { Migration01426.Require(a.Native<BlueprintFeature>("8f49a5d8528a82c44b8c117a89f6b68c"),true), Migration01426.Require(a.Native<BlueprintFeature>("04bc2b62273ab744092d992ed72bff41"),true) }
            },Token,"Anger Inquisition","Gorum or Rovagug. Hateful Retort at level 1; Divine Anger at level 6, Greater Rage at level 14, Tireless Rage at level 20. Uses cleric/inquisitor levels; replaces a domain without domain spells or slots.");
            Set(root,"m_Classes",classes.Select(c=>new BlueprintProgression.ClassWithLevel { m_Class=Ref<BlueprintCharacterClassReference>(c) }).ToArray());
            var once=new PrerequisiteNoFeature(); Set(once,"m_Feature",Ref<BlueprintFeatureReference>(root)); root.ComponentsArray=root.ComponentsArray.Concat(new[] { once }).ToArray();
            var ui=new UIGroup(); Set(ui,"m_Features",new[] { retortFeature,rageFeature,greater,tireless }.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            root.UIGroups=new[] { ui }; Icon(root,rageSource);
            Migration01426.Commit(a,Inquisitions01426.PoolIds.Select(a.Native<BlueprintFeatureSelection>).ToArray(),root);
        }
    }

    [TypeId("aa3efeb6bb4144d5a9d989a456b6c31b")]
    public sealed class AngerRageHitPoints01431 : PropertyValueGetter
    {
        public BlueprintCharacterClassReference[] Classes;
        public BlueprintFeatureReference Greater,Mighty;
        public BlueprintUnitFactReference Limitless;
        public BlueprintAbilityResourceReference Resource;
        protected override int GetBaseValue(UnitEntityData unit) => Math.Max(1,(Classes??Array.Empty<BlueprintCharacterClassReference>()).Sum(c=>unit.Progression.GetClassLevel(c.Get()))-3)*(2+(unit.HasFact(Greater?.Get())?1:0)+(unit.HasFact(Mighty?.Get())?1:0))
            +(Limitless?.Get()!=null&&unit.HasFact(Limitless.Get())&&Resource?.Get()!=null?unit.Resources.GetResourceAmount(Resource.Get())/2:0);
    }
    [TypeId("3613ee8f981c4b9e8982b20cc64717ad")]
    public sealed class AngerHatefulRetort01431 : UnitBuffComponentDelegate, ITargetRulebookHandler<RuleAttackWithWeapon>
    {
        public BlueprintAbilityResourceReference Resource;
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt) {
            if(!Buff.IsActive||!Buff.IsTurnedOn||Buff.IsSuppressed||evt.Target!=Owner||evt.AttackRoll?.IsHit!=true||evt.AttackRoll.IsFake||evt.Weapon?.Blueprint.IsMelee!=true||evt.Initiator?.Commands?.Free is AngerRetortCommand01431||!AngerRetortCommand01431.Legal(Owner,evt.Initiator)) return;
            var resource=Resource?.Get(); if(resource==null||Owner.Resources.GetResourceAmount(resource)<1) return;
            var command=new AngerRetortCommand01431(evt.Initiator,Context,Owner.GetThreatHand().Weapon.UniqueId,Ref<BlueprintBuffReference>(Buff.Blueprint));
            command.Init(Owner); Owner.Resources.Spend(resource,1); Owner.Commands.Queue.AddFirst(command);
        }
    }
    // The existing paid-reaction scheduler recognizes this exact command too.
    // Animation waits for the weapon act frame; a zero attack penalty is the
    // highest BAB attack, independent of the ordinary full-attack iterator.
    [TypeId("db5c92d2891044b48fa386a5c66a39c7")]
    public sealed class AngerRetortCommand01431 : UnitCommand
    {
        [JsonProperty] MechanicsContext m_Context;
        [JsonProperty] string m_WeaponId;
        [JsonProperty] BlueprintBuffReference m_Consent;
        [JsonProperty] bool m_Resolved;
        [JsonIgnore] AttackHandInfo m_Attack;
        public AngerRetortCommand01431(JsonConstructorMark mark):base(mark) { }
        internal AngerRetortCommand01431(UnitEntityData target,MechanicsContext context,string weapon,BlueprintBuffReference consent):base(CommandType.Free,target) {
            m_Context=context; m_WeaponId=weapon; m_Consent=consent; ApproachRadius=1000000f; ReactionAction=true; IgnoreCooldown(); DoNotUpdateManualTarget();
        }
        internal static bool Legal(UnitEntityData unit,UnitEntityData enemy) {
            if(unit==null||enemy==null||unit==enemy||unit.IsDisposed||enemy.IsDisposed||unit.State.IsDead||enemy.State.IsDead||!unit.State.CanAct||enemy.IsAlly(unit)||!unit.CanAttack(enemy)) return false;
            var hand=unit.GetThreatHand(); var range=hand==null?null:unit.GetThreatRange(hand);
            return hand?.Weapon?.Blueprint.IsMelee==true&&range.HasValue&&UnitHelper.IsAttackOfOpportunityReach(unit,enemy,range.Value);
        }
        bool Ready => Legal(Executor,Target?.Unit)&&Executor.GetThreatHand().Weapon.UniqueId==m_WeaponId&&Executor.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==m_Consent?.Guid&&b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed);
        public override bool IsOneFrameCommand=>false;
        public override bool IsInterruptible=>false;
        public override bool ShouldTurnToTarget=>true;
        public override bool MarkManualTarget=>false;
        public override bool NeedEquipWeapons=>true;
        public override bool CanStart { get { if(!base.CanStart||Executor==null)return false; return !Executor.Commands.Raw.Any(c=>c!=this&&c?.IsRunning==true&&c.IsAnimating); } }
        public override bool ShouldBeInterrupted=>!Ready;
        protected override void OnStart() {
            base.OnStart(); if(!Ready){Interrupt();return;}
            m_Attack=new AttackHandInfo(Executor.GetThreatHand(),0) { Target=Target.Unit };
            if(Executor.View?.AnimationManager!=null&&!ShouldSkipAnimation) {
                m_Attack.CreateAnimationHandleForAttack(new[] { m_Attack },Executor.GetSaddledUnit()!=null);
                if(m_Attack.AnimationHandle!=null) { m_Attack.AnimationHandle.AttackTargetDistance=(Target.Unit.Position-Executor.Position).magnitude; StartAnimation(m_Attack.AnimationHandle); return; }
            }
            ScheduleAct();
        }
        protected override ResultType OnAction() {
            if(m_Resolved||m_Context==null||!Ready) return ResultType.Fail; m_Resolved=true;
            var hand=Executor.GetThreatHand();
            var rule=new RuleAttackWithWeapon(Executor,Target.Unit,hand.Weapon,0) { ExtraAttack=true,IsFirstAttack=true,AttacksCount=1,SkipAnimation=false,WeaponSlot=hand,Reason=new RuleReason(m_Context) };
            TeamworkRangedReaction.Mark(rule); if(m_Attack!=null)m_Attack.Rule=rule; m_Context.TriggerRule(rule); if(m_Attack!=null)m_Attack.IsHit=rule.AttackRoll?.IsHit==true;
            return ResultType.Success;
        }
    }
}
