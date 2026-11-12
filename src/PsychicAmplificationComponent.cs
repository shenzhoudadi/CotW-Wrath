using System;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections.Generic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;

namespace CallOfTheWild.Wrath.Prototype
{
    public enum PsychicAmplificationKind { BiokineticHealing, DefensivePrognostication, OverpoweringMind, RelentlessCasting, OngoingDefense, FocusedForce, MindsEye, ConjuredArmor, WillOfTheDead, UndercastSurge, SynapticShock, SpaceRendingSpell }

    [AllowedOn(typeof(BlueprintUnitFact), false)]
    [TypeId("089fcba29be4517f8bd68d68795b12e8")]
    public sealed class PsychicAmplificationComponent : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCastSpell>, IInitiatorRulebookSubscriber
    {
        public PsychicAmplificationKind Kind;
        public int Cost, Strength, MinimumLevel;
        public BlueprintAbilityResourceReference WisdomPool, CharismaPool;
        public BlueprintCharacterClassReference CharacterClass;
        public BlueprintBuffReference Defense;
        // S-PSYCHIC-0068: explicit book/class/pool binding. All three legacy
        // fields above keep their original meaning when BoundBook is absent.
        public BlueprintSpellbookReference BoundBook;
        public BlueprintAbilityResourceReference BoundPool;
        protected override void OnTurnOn()
        {
            var scope = PsychicAmplificationBinding.BookId(this);
            if(PsychicAdvancedRuntime01458.CoordinateIfNeeded(Owner,Fact,scope))return;
            foreach(var mode in Owner.Facts.List.OfType<ActivatableAbility>().ToArray())
                if(mode.Blueprint.Buff?.GetComponent<PsychicAmplificationComponent>() is PsychicAmplificationComponent otherMode &&
                   PsychicAmplificationBinding.BookId(otherMode)==scope && mode.Blueprint.Buff!=Fact.Blueprint)
                    mode.IsOn=false;
            // Also retire orphaned pre-update/restored mode buffs with no native
            // toggle owner. Only modes bound to the same book are retired.
            foreach(var buff in Owner.Buffs.Enumerable.ToArray())
                if(buff!=Fact && buff.Blueprint!=Fact.Blueprint &&
                   buff.Blueprint.GetComponent<PsychicAmplificationComponent>() is PsychicAmplificationComponent otherBuff &&
                   PsychicAmplificationBinding.BookId(otherBuff)==scope)
                    buff.Remove();
        }
        private sealed class Reservation {
            internal PsychicAmplificationComponent Component;
            internal BlueprintAbilityResource Resource;
            internal AbilityParams OriginalParams;
            internal int SpellLevel;
            internal bool Finished;
            internal int Spent;
        }
        // Transient rule identity, not a save component. One charge per cast even
        // with duplicated notifications, nested casts or corrupt multiple modes.
        private static readonly ConditionalWeakTable<RuleCastSpell, Dictionary<PsychicAmplificationKind,Reservation>> Casts = new ConditionalWeakTable<RuleCastSpell, Dictionary<PsychicAmplificationKind,Reservation>>();
        internal bool Eligible(RuleCastSpell evt)
        {
            var spell=evt.Spell;
            if(evt.ForceFail || evt.IsDuplicateSpellApplied || evt.Context==null || evt.Context.IsDuplicateSpellApplied ||
                spell==null || !PsychicAmplificationBinding.Owns(this,spell) ||
                spell.StickyTouch!=null || evt.Context.Params.SpellLevel<=0) return false;
            // Legacy components keep the original Psychic Major/level gate. A
            // bound component reads the real level of its own class instead.
            if(PsychicAmplificationBinding.IsBound(this)
                ? PsychicAmplificationBinding.Level(this,evt.Initiator)<MinimumLevel
                : !PsychicMajor01460.Allows(evt.Initiator,this)) return false;
            if(Kind>=PsychicAmplificationKind.WillOfTheDead)return PsychicAdvancedRuntime01458.Eligible(this,evt);
            if(Kind==PsychicAmplificationKind.RelentlessCasting) return spell.Blueprint.SpellResistance;
            if(Kind==PsychicAmplificationKind.OngoingDefense) return PsychicAmplificationRuntime.IsDefense(spell.Blueprint);
            if(Kind==PsychicAmplificationKind.FocusedForce) return PsychicLinkedSpellEffects.HasForceDice(spell.Blueprint);
            if(Kind==PsychicAmplificationKind.MindsEye) return spell.Blueprint.GetComponents<AbilityDeliverProjectile>()
                .Any(p=>p.NeedAttackRoll && (p.Weapon==null || p.Weapon.IsRanged));
            if(Kind==PsychicAmplificationKind.ConjuredArmor) return spell.Blueprint.School==SpellSchool.Conjuration &&
                (evt.Context.SpellDescriptor & SpellDescriptor.Summoning)!=0;
            var school=spell.Blueprint.School;
            return Kind==PsychicAmplificationKind.BiokineticHealing ? school==SpellSchool.Transmutation :
                Kind==PsychicAmplificationKind.DefensivePrognostication ? school==SpellSchool.Divination :
                (evt.Context.SpellDescriptor & SpellDescriptor.MindAffecting)!=0;
        }
        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
            if((Casts.TryGetValue(evt,out var recorded)&&recorded.ContainsKey(Kind)) || !Eligible(evt)) return;
            var resources=evt.Initiator.Descriptor.Resources;
            // One pool per bound book; an unbound legacy component still uses
            // the original Wisdom/Charisma resolution. No mixed payment.
            var pool=PsychicAmplificationBinding.IsBound(this)
                ? PsychicAmplificationBinding.Pool(this,evt.Initiator)
                : (resources.ContainsResource(WisdomPool.Get()) ? WisdomPool.Get() : CharismaPool.Get());
            if(pool==null || !resources.HasEnoughResource(pool,Cost)) return;
            if(!PsychicAdvancedRuntime01458.Claim(evt,Kind.ToString()))return;
            int spent=Cost;
            var reservation=new Reservation { Component=this, Resource=pool, OriginalParams=evt.Context.Params, SpellLevel=evt.Context.Params.SpellLevel, Spent=spent };
            Casts.GetOrCreateValue(evt).Add(Kind,reservation);
            resources.Spend(pool,spent);
            if((int)Kind>=3) PsychicAmplificationRuntime.Reserve(evt.Context,Kind,Fact,Defense?.Get());
            if(Kind==PsychicAmplificationKind.OverpoweringMind) {
                // Clone: a preview/cached AbilityData parameter object can be shared
                // with later casts. Only this execution context gets the paid bonus.
                var parameters=reservation.OriginalParams.Clone(); parameters.DC+=Strength; evt.Context.SetParams(parameters);
            }
        }
        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            if(!Casts.TryGetValue(evt,out var records) || !records.TryGetValue(Kind,out var reservation) || reservation.Component!=this || reservation.Finished) return;
            reservation.Finished=true;
            if(!evt.Success) {
                evt.Initiator.Descriptor.Resources.Restore(reservation.Resource,reservation.Spent);
                PsychicAmplificationRuntime.Cancel(evt.Context);
                if(Kind==PsychicAmplificationKind.OverpoweringMind) evt.Context.SetParams(reservation.OriginalParams);
                return;
            }
            if(Kind==PsychicAmplificationKind.BiokineticHealing)
                Rulebook.Trigger(new RuleHealDamage(evt.Initiator,evt.Initiator,2*reservation.SpellLevel) { Reason=evt.Context });
            else if(Kind==PsychicAmplificationKind.DefensivePrognostication) {
                var buff=evt.Initiator.Descriptor.AddBuff(Defense.Get(),evt.Context,TimeSpan.FromSeconds(6*reservation.SpellLevel));
                if(buff!=null) buff.IsNotDispelable=true;
            }
            else if(Kind==PsychicAmplificationKind.SpaceRendingSpell)PsychicAdvancedRuntime01458.PermitSpace(evt);
        }
    }
}
