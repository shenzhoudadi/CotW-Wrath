using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static int storedCasts;
    static RuleCastSpell lastStored;
    static bool StoredTerminal(RuleCastSpell __instance){storedCasts++;lastStored=__instance;return false;}
    static void AttackNotification(UnitEntityData source,UnitEntityData target,ItemEntityWeapon weapon,bool hit=true,bool fake=false)
    {
        var roll=new RuleAttackRoll(source,target,weapon,0);Set(roll,"Result",hit?AttackResult.Hit:AttackResult.Miss);Set(roll,"IsFake",fake);
        var attack=new RuleAttackWithWeapon(source,target,weapon,0);Set(attack,"AttackRoll",roll);roll.RuleAttackWithWeapon=attack;
        var field=AccessTools.Field(typeof(BardMasterpieces01435Tests),"current");var previous=field.GetValue(null);var events=new RulebookEventContext();events.PushEvent(attack);field.SetValue(null,events);
        try{RulebookEventBus.OnEventAboutToTrigger(attack);RulebookEventBus.OnEventDidTrigger(attack);}finally{events.PopEvent(attack);field.SetValue(null,previous);}
    }
    static void SkirnirCombat(BlueprintsCache c,UnitEntityData u,Spellbook book,ItemEntityShield shield,ItemEntityWeapon dagger,SpellSlot slot,SkirnirStorageData01454 state,Harmony h)
    {
        Console.WriteLine("054 Skirnir: native event bus, class-specific ASF, AC and stored release");
        var enemy=EquipmentUnit(c,19);var hostile=AccessTools.Field(typeof(Archetypes01445Tests),"hostile");var oldEnemy=hostile.GetValue(null);hostile.SetValue(null,enemy);
        var current=AccessTools.Field(typeof(BardMasterpieces01435Tests),"current");var old=current.GetValue(null);
        h.Patch(AccessTools.Method(typeof(RuleCastSpell),nameof(RuleCastSpell.OnTrigger)),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(StoredTerminal)));
        try{
            var spell=state.Spell.Blueprint;storedCasts=0;var release=Toggle(u,c,"SkirnirReleaseToggle01454");ToggleOn(release);
            AttackNotification(u,enemy,shield.WeaponComponent,false);Check(storedCasts==0&&state.Spell!=null,"actual miss notification retains stored spell");
            AttackNotification(u,enemy,shield.WeaponComponent,true,true);Check(storedCasts==0&&state.Spell!=null,"native fake attack notification cannot discharge storage");
            AttackNotification(u,enemy,dagger);Check(storedCasts==0&&state.Spell!=null,"ordinary main-hand hit does not release shield storage");
            AttackNotification(u,u,shield.WeaponComponent);Check(storedCasts==0&&state.Spell!=null,"friendly shield-hit notification does not consume storage");
            AttackNotification(u,enemy,shield.WeaponComponent);Check(storedCasts==1&&state.Spell==null&&lastStored.Spell.Blueprint==spell&&lastStored.Spell.Spellbook==book&&lastStored.Spell.SaveSpellbookSlot&&lastStored.Context.Params.CasterLevel==19,"actual native event dispatch reaches one native stored cast with exact book, saved parameters and no second slot request");
            AttackNotification(u,enemy,shield.WeaponComponent);Check(storedCasts==1,"same/duplicate successful notification cannot release empty storage twice");
            void StoreAgain(){slot.Available=true;var conversion=slot.SpellShell.GetConversions().Single(a=>a.Blueprint.GetComponent<SkirnirStoreSpell01454>()!=null);AbilityCastRateUtils.SpendResources(conversion);conversion.Blueprint.GetComponent<SkirnirStoreSpell01454>().Apply(new AbilityExecutionContext(conversion,new AbilityParams{CasterLevel=19},new TargetWrapper(u),new RulebookEventContext(),u),new TargetWrapper(u));Check(state.Spell!=null&&!slot.Available,"new controlled available preparation is consumed through the actual storage entry");}
            StoreAgain();Grant(u,c,"SkirnirGreaterSpellshield01454");var greater=Toggle(u,c,"SkirnirGreaterReleaseToggle01454");ToggleOn(greater);u.CombatState.Cooldown.SwiftAction=6f;
            AttackNotification(enemy,u,dagger);Check(storedCasts==1&&state.Spell!=null,"occupied native swift cooldown prevents retaliation");u.CombatState.Cooldown.SwiftAction=0;
            AttackNotification(enemy,u,dagger);Check(storedCasts==2&&state.Spell==null&&u.CombatState.Cooldown.SwiftAction>0&&lastStored.SpellTarget.Unit==enemy,"native enemy melee-hit event spends swift action once and releases at the attacker");
            StoreAgain();ToggleOff(greater);ToggleOff(release);
            foreach(bool own in new[]{true,false}){
                var selected=own?book:new Spellbook(u.Descriptor,N<BlueprintCharacterClass>(c,"45a4607686d96a1498891b3286121780").Spellbook);Set(selected,"m_BaseLevelInternal",19);
                var cast=(RuleCastSpell)H("Cast",u,enemy,selected,spell,19);var events=new RulebookEventContext();events.PushEvent(cast);current.SetValue(null,events);var failure=new RuleCalculateArcaneSpellFailureChance(u);events.PushEvent(failure);
                try{RulebookEventBus.OnEventAboutToTrigger(failure);failure.OnTrigger(events);RulebookEventBus.OnEventDidTrigger(failure);Check(failure.IgnoreShield==own&&!failure.IgnoreArmor,"actual native ASF event ignores shield only for private Skirnir book, preserving armor/other book rules");}finally{events.PopEvent(failure);events.PopEvent(cast);current.SetValue(null,old);}
            }
            var preview=new RuleCalculateArcaneSpellFailureChance(u);RulebookEventBus.OnEventAboutToTrigger(preview);Check(!preview.IgnoreShield,"global equipment-only preview does not grant shield immunity to unrelated spellbooks");
            var penalty=Add(u,N<BlueprintBuff>(c,"7b4cf64d3a49e3d45b1dbd2385f4eb6d"));var ours=u.Buffs.Enumerable.SingleOrDefault(b=>b.Blueprint.AssetGuid==Id("SkirnirCombatPenalty01454"));Check((ours==null)==ShieldRules056,"native combat penalty creation follows explicitly versioned user rules");if(ours==null)ours=Add(u,A<BlueprintBuff>(c,"SkirnirCombatPenalty01454"));int shieldBonus=u.Stats.AC.Modifiers.Where(m=>m.ModDescriptor==ModifierDescriptor.Shield||m.ModDescriptor==ModifierDescriptor.ShieldEnhancement||m.ModDescriptor==ModifierDescriptor.ShieldFocus).Sum(m=>m.ModValue);
            foreach(var type in new[]{AttackType.Melee,AttackType.Ranged,AttackType.Touch,AttackType.RangedTouch}){
                var ac=new RuleCalculateAC(enemy,u,type);RulebookEventBus.OnEventAboutToTrigger(ac);Check(ac.AllBonuses.Where(m=>m.Fact==ours).Sum(m=>m.Value)==(ShieldRules056||type.IsTouch()?0:-shieldBonus),"actual native target event applies original shield penalty only where shield AC participates: "+type);
            }
            var brilliant=dagger.AddEnchantment(N<Kingmaker.Blueprints.Items.Ecnchantments.BlueprintWeaponEnchantment>(c,"66e9e299c9002ea4bb65b6f300e43770"),new MechanicsContext(u,u.Descriptor,spell));var ignoresShield=new RuleCalculateAC(enemy,u,AttackType.Melee){BrilliantEnergy=brilliant};RulebookEventBus.OnEventAboutToTrigger(ignoresShield);Check(!ignoresShield.AllBonuses.Any(m=>m.Fact==ours),"native brilliant-energy AC flag prevents subtracting shield a second time");dagger.RemoveEnchantment(brilliant);
            Grant(u,c,"SkirnirGreaterShieldCombat01454");var retained=new RuleCalculateAC(enemy,u,AttackType.Melee);RulebookEventBus.OnEventAboutToTrigger(retained);Check(!retained.AllBonuses.Any(m=>m.Fact==ours),"level-19 feature retains shield AC even while prior penalty buff exists");penalty.Remove();ours.Remove();
        }finally{current.SetValue(null,old);hostile.SetValue(null,oldEnemy);h.Unpatch(AccessTools.Method(typeof(RuleCastSpell),nameof(RuleCastSpell.OnTrigger)),AccessTools.Method(typeof(MagusBatch01454Tests),nameof(StoredTerminal)));}
    }
}
