using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.Utility;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class FlameJet01429
    {
        internal const string ModuleName = "Wild talents: Flame Jet and Greater Flame Jet";
        internal const string Token = "FlameJetWildTalent";
        internal const string GreaterToken = "FlameJetGreaterWildTalent";
        internal static void Register(BlueprintsCache cache)
        {
            var f = new PsychicAssets(cache);
            var pool = f.Native<BlueprintFeatureSelection>(PurifyingFlames01428.PoolId);
            var kineticist = f.Native<BlueprintCharacterClass>(PurifyingFlames01428.ClassId);
            var fire = f.Native<BlueprintFeature>(PurifyingFlames01428.FoxfireId).GetComponents<PrerequisiteFeaturesFromList>().ToArray();
            var donor = f.Native<BlueprintAbility>("a9b8be9b87865744382f7c64e599aeb2");
            var burn = f.Native<BlueprintAbility>(PurifyingFlames01428.HealerId).GetComponents<AbilityKineticist>().ToArray();
            var sourceFlight = cache.Load(Id("OverlandFlightBuff")) as BlueprintBuff ?? throw new InvalidOperationException("Greater Flame Jet requires the registered flight effect.");
            var flight=f.Clone(sourceFlight,"FlameJetHoverBuff","Flame Jet: Hover","Permanent airborne benefits granted by Greater Flame Jet; independent of temporary flight spells.");
            flight.ComponentsArray=flight.ComponentsArray.Where(c=>!(c is AddStatBonus)&&!(c is PsychicFlightDefense)&&!(c is FlameJetFlightLifecycle01430)).Concat(new BlueprintComponent[]{new FlameJetHover01429()}).ToArray();
            flight.Stacking=StackingType.Stack;
            var flags=Get(flight,"m_Flags");
            Set(flight,"m_Flags",Enum.ToObject(flags.GetType(),Convert.ToInt32(flags)|8)); // native StayOnDeath
            var projectile = f.Native<BlueprintProjectile>("ecf79fc871f15074e95698a3fef47aee");
            if (fire.Length != 1 || fire[0].Features.Length == 0 || burn.Length != 1 ||
                donor.GetComponents<AbilityCustomDimensionDoor>().Count() != 1 || donor.GetComponent<AbilityCustomDimensionDoor>().Radius.Meters != 0)
                throw new InvalidOperationException("Flame Jet fire, kineticist or caster-only movement contract changed.");
            const string text = "Element: fire. Utility wild talent, level 3; requires kineticist level 6. Burn: 0. As a spell-like standard action, propel yourself up to 30 feet to a visible valid destination. Nearby allies do not travel with you; native mount/rider pairing remains. Greater Flame Jet changes this talent to a move action and grants the native airborne benefits. The game uses its native flat-map destination/navigation checks.";
            const string greaterText = "Element: fire. Utility wild talent, level 5; requires kineticist level 10 and Flame Jet. Burn: 0. Flame Jet uses a move action instead of a standard action. Hover without an activation cost, using the existing flight effect: +10 feet speed, ground and difficult-terrain protection, and +3 dodge AC against grounded melee attackers while mobile and retaining Dexterity to AC. No nearby allies accompany the jet; the flying AC bonus does not stack with native wings.";
            var feature = f.Feature(Token, "Flame Jet", text);
            var greater = f.Feature(GreaterToken, "Flame Jet, Greater", greaterText, Facts(flight));
            var standard = Leaf(f, kineticist, donor, burn[0], projectile, feature, greater, false, text);
            var move = Leaf(f, kineticist, donor, burn[0], projectile, feature, greater, true, greaterText);
            var variants = new AbilityVariants(); Set(variants, "m_Variants", new[] { standard, move }.Select(Ref<BlueprintAbilityReference>).ToArray());
            var wrapper = f.Ability("FlameJetAbility", "Flame Jet", text, AbilityType.SpellLike, UnitCommand.CommandType.Standard, AbilityRange.Custom, variants);
            wrapper.CustomRange = new Feet(30); wrapper.CanTargetPoint = true; wrapper.IgnoreMinimalRangeLimit = true; wrapper.SpellResistance = false;
            foreach (var leaf in new[] { standard, move }) Set(leaf, "m_Parent", Ref<BlueprintAbilityReference>(wrapper));
            foreach (var item in new[] { feature, greater }) { item.IsClassFeature = true; item.Groups = new[] { FeatureGroup.KineticWildTalent }; Set(item, "m_Icon", Get(donor, "m_Icon")); }
            feature.ComponentsArray = new BlueprintComponent[] { CopyData(fire[0]), Level(kineticist, 6), NoFeature(feature), Facts(wrapper) };
            greater.ComponentsArray = greater.ComponentsArray.Concat(new BlueprintComponent[] { CopyData(fire[0]), Level(kineticist, 10), Migration01426.Require(feature), NoFeature(greater) }).ToArray();
            Set(wrapper, "m_Icon", Get(donor, "m_Icon"));
            // Temporary flight keeps its native stat component. Its own lifecycle
            // must notify hover even when a save restart raises no added event.
            var flightComponents=sourceFlight.ComponentsArray;
            try {
                if(!flightComponents.Any(c=>c is FlameJetFlightLifecycle01430))
                    sourceFlight.ComponentsArray=flightComponents.Concat(new BlueprintComponent[]{new FlameJetFlightLifecycle01430 {name="FlameJetFlightLifecycle01430"}}).ToArray();
                Migration01426.Commit(f, new[] { pool }, feature, greater);
            } catch { sourceFlight.ComponentsArray=flightComponents; throw; }
        }
        static PrerequisiteClassLevel Level(BlueprintCharacterClass cls, int level) { var p = new PrerequisiteClassLevel { Level = level }; Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(cls)); return p; }
        static PrerequisiteNoFeature NoFeature(BlueprintFeature feature) { var p = new PrerequisiteNoFeature(); Set(p, "m_Feature", Ref<BlueprintFeatureReference>(feature)); return p; }
        static BlueprintAbility Leaf(PsychicAssets f, BlueprintCharacterClass kineticist, BlueprintAbility donor, AbilityKineticist burn,
            BlueprintProjectile projectile, BlueprintFeature basic, BlueprintFeature greater, bool upgraded, string text)
        {
            string token = upgraded ? "FlameJetMove" : "FlameJetStandard";
            var ability = f.Clone(donor, token, upgraded ? "Flame Jet (Move Action)" : "Flame Jet", text);
            ability.Type = AbilityType.SpellLike; ability.ActionType = upgraded ? UnitCommand.CommandType.Move : UnitCommand.CommandType.Standard;
            ability.Range = AbilityRange.Custom; ability.CustomRange = new Feet(30); ability.IgnoreMinimalRangeLimit = true;
            ability.AvailableMetamagic = 0; ability.SpellResistance = false; Set(ability, "m_IsFullRoundAction", false);
            ability.MaterialComponent = new BlueprintAbility.MaterialComponentData(); Set(ability, "m_Parent", new BlueprintAbilityReference());
            ability.ComponentsArray = ability.ComponentsArray.Where(c => c is AbilityCustomDimensionDoor || c is AbilitySpawnFx || c is SpellComponent ||
                c.GetType().Namespace == "Kingmaker.UnitLogic.Abilities.Components.TargetCheckers").Where(c => c.GetType().Name != "LineOfSightIgnorance" && c.GetType().Name != "DimensionDoorRestrictionIgnorance").ToArray();
            var movement = ability.GetComponent<AbilityCustomDimensionDoor>();
            Set(movement, "m_CasterDisappearProjectile", Ref<BlueprintProjectileReference>(projectile)); Set(movement, "m_CasterAppearProjectile", Ref<BlueprintProjectileReference>(projectile));
            var cost = CopyData(burn); cost.Amount = cost.WildTalentBurnCost = cost.InfusionBurnCost = cost.BlastBurnCost = 0;
            var visible = new AbilityShowIfCasterHasFact { Not = !upgraded }; Set(visible, "m_UnitFact", Ref<BlueprintUnitFactReference>(greater));
            ability.ComponentsArray = ability.ComponentsArray.Concat(Inquisitions01426.Parameters(f, token, new[] { kineticist }, StatType.Constitution))
                .Concat(new BlueprintComponent[] { cost, visible, new FlameJetUpgradeRestriction { Basic = Ref<BlueprintFeatureReference>(basic), Greater = Ref<BlueprintFeatureReference>(greater), Upgraded = upgraded } }).ToArray();
            ability.GetComponent<Kingmaker.UnitLogic.Mechanics.Components.ContextCalculateAbilityParams>().UseKineticistMainStat = true;
            return ability;
        }
    }

    [TypeId("8cb8f830c4214fae93e9c9e809ea243e")]
    public sealed class FlameJetFlightLifecycle01430:UnitBuffComponentDelegate
    {
        // Appended after the existing AddStatBonus. Native EntityFact updates
        // IsTurnedOn before dispatch, so stopped/suppressed flight cannot hide
        // hover's contribution. Only hover-owned modifiers are ever changed.
        void RefreshHover(){
            foreach(var hover in Owner.Buffs.Enumerable.Where(b=>b.Blueprint.GetComponent<FlameJetHover01429>()!=null).ToArray())
                foreach(var runtime in hover.Components.Where(r=>r.SourceBlueprintComponent is FlameJetHover01429))
                    using(runtime.RequestEventContext())((FlameJetHover01429)runtime.SourceBlueprintComponent).Refresh();
        }
        protected override void OnTurnOn()=>RefreshHover();
        protected override void OnTurnOff()=>RefreshHover();
        protected override void OnApplyPostLoadFixes()=>RefreshHover();
    }

    [TypeId("e1156a76cd264cdb92d8a411a1da9b50")]
    public sealed class FlameJetHover01429:UnitBuffComponentDelegate,IUnitBuffHandler,ITargetRulebookHandler<RuleCalculateAC>
    {
        bool SpellFlight=>Owner.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==Id("OverlandFlightBuff")&&b.IsTurnedOn&&!b.IsSuppressed&&!b.IsDisposed);
        void Clear(){foreach(var m in Owner.Stats.Speed.Modifiers.Where(m=>ReferenceEquals(m.Source,Fact)).ToArray())Owner.Stats.Speed.RemoveModifier(m);}
        internal void Refresh(){Clear();if(Fact.IsTurnedOn&&!Fact.IsDisposed&&!Buff.IsSuppressed&&!SpellFlight)Owner.Stats.Speed.AddModifier(10,Fact,ModifierDescriptor.UntypedStackable);}
        protected override void OnTurnOn()=>Refresh();
        protected override void OnTurnOff()=>Clear();
        protected override void OnApplyPostLoadFixes()=>Refresh();
        public void HandleBuffDidAdded(Buff b){if(b.Owner==Owner&&b.Blueprint.AssetGuid==Id("OverlandFlightBuff"))Refresh();}
        public void HandleBuffDidRemoved(Buff b){if(b.Owner==Owner&&b.Blueprint.AssetGuid==Id("OverlandFlightBuff"))Refresh();}
        public void OnEventAboutToTrigger(RuleCalculateAC e){
            if(SpellFlight||(e.AttackType!=AttackType.Melee&&e.AttackType!=AttackType.Touch)||e.Initiator.State.Features.Flying||!Owner.State.CanMove||e.IsTargetFlatFooted||Context.TriggerRule(new RuleCheckTargetFlatFooted(e.Initiator,Owner)).IsFlatFooted)return;
            if(Owner.Buffs.Enumerable.Any(b=>b.IsTurnedOn&&!b.IsSuppressed&&b.Blueprint.GetComponents<Kingmaker.Designers.Mechanics.Facts.ACBonusAgainstAttacks>().Any(c=>c.AgainstMeleeOnly&&c.Descriptor==ModifierDescriptor.Dodge&&c.ArmorClassBonus==3)))return;
            e.AddModifier(3,Fact,ModifierDescriptor.Dodge);
        }
        public void OnEventDidTrigger(RuleCalculateAC e){}
    }

    // Both modes belong to one native variants wrapper, so old hotbar roots
    // retain their identity. A queued obsolete standard leaf is also rejected.
    [TypeId("a0fdd988ef714f4aa4455331399bcd27")]
    public sealed class FlameJetUpgradeRestriction : BlueprintComponent, IAbilityCasterRestriction
    {
        public BlueprintFeatureReference Basic, Greater;
        public bool Upgraded;
        public bool IsCasterRestrictionPassed(UnitEntityData caster) => caster != null && Basic?.Get() != null && Greater?.Get() != null &&
            caster.Descriptor.HasFact(Basic.Get()) && caster.Descriptor.HasFact(Greater.Get()) == Upgraded;
        public string GetAbilityCasterRestrictionUIText() => "Use the Flame Jet action appropriate to your selected wild talents.";
    }
}
