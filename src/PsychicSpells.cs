using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    [AllowedOn(typeof(BlueprintAbility), false)]
    [TypeId("65458c88a8dd51c79e6ba0d83eae8d39")]
    public sealed class PsychicUndercastFamily : BlueprintComponent
    {
        public string Family;
        public int Circle;
        public BlueprintAbilityReference[] LowerSpells;
    }

    [TypeId("896fb8c015155d59b26d93d3824384dd")]
    public sealed class PsychicDirectDamage : ContextAction
    {
        public DiceType Die = DiceType.D8;
        public int Dice;
        public int Cap;
        public bool PerCasterLevel;
        public bool AddCasterLevel;
        public bool HalfOnSave;
        public bool LimitHP;
        public bool Intensifiable;
        public override string GetCaption() => "Psychic direct damage";
        public int DiceAtLevel(int casterLevel, bool intensified) => PerCasterLevel ? Math.Min(Math.Max(0, casterLevel), Cap + (Intensifiable && intensified ? 5 : 0)) : Dice;
        public override void RunAction()
        {
            var damage = new ContextActionDealDamage {
                DamageType = new DamageTypeDescription { Type = DamageType.Direct },
                Value = new ContextDiceValue { DiceType = Die,
                    DiceCountValue = DiceAtLevel(Context.Params.CasterLevel, (Context.Params.Metamagic & Kingmaker.UnitLogic.Abilities.Metamagic.Intensified) != 0),
                    BonusValue = AddCasterLevel ? Context.Params.CasterLevel : 0 },
                HalfIfSaved = HalfOnSave, UseMinHPAfterDamage = LimitHP, MinHPAfterDamage = -1
            };
            damage.RunAction();
        }
    }

    internal static class PsychicSpells
    {
        internal sealed class Entry { internal int Level; internal BlueprintAbility Ability; internal string Token; }
        internal static readonly string[] Roman = { "", "I", "II", "III", "IV", "V", "VI" };
        internal static List<Entry> Build(PsychicAssets f)
        {
            var result = new List<Entry>();
            var fatigue = f.Native<BlueprintBuff>("e6f2fc5d73d88064583cb828801212f4");
            var exhaustion = f.Native<BlueprintBuff>("46d1b9cc3d0fd36469a471b047d773a2");
            var stun = f.Native<BlueprintBuff>("09d39b38bb7c6014394b6daced9bacd3");
            var sickened = f.Native<BlueprintBuff>("4e42460798665fd4cb9173ffa7ada323");
            var intelligent = new AbilityTargetHasFact { name = "RequiresIntelligentTarget", Inverted = true };
            Set(intelligent, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("fd389783027d63343b4a5634bd81645f")), Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("09478937695300944a179530664e42ec")) });
            var offensiveMeta = Kingmaker.UnitLogic.Abilities.Metamagic.Empower | Kingmaker.UnitLogic.Abilities.Metamagic.Maximize |
                Kingmaker.UnitLogic.Abilities.Metamagic.Quicken | Kingmaker.UnitLogic.Abilities.Metamagic.Reach | Kingmaker.UnitLogic.Abilities.Metamagic.Heighten |
                Kingmaker.UnitLogic.Abilities.Metamagic.Persistent | Kingmaker.UnitLogic.Abilities.Metamagic.Piercing;
            var thrust = new List<BlueprintAbility>();
            for (int i = 0; i < 6; i++) {
                int circle = i+1, cap = new[] { 5, 5, 10, 15, 15, 20 }[i]; var die = i == 0 ? DiceType.D6 : DiceType.D8;
                var effects = new List<GameAction> { new PsychicDirectDamage { Die = die, Cap = cap, PerCasterLevel = true, HalfOnSave = true, Intensifiable = true } };
                string extra = "";
                if (i == 3) { effects.Add(Saved(Array.Empty<GameAction>(), new GameAction[] { Apply(fatigue, 1) })); extra = " A failed save also causes fatigue for 1 round."; }
                if (i == 4) { effects.Add(Saved(Array.Empty<GameAction>(), new GameAction[] { Apply(exhaustion, 1) })); extra = " A failed save also causes exhaustion for 1 round."; }
                if (i == 5) { effects.Add(Saved(new GameAction[] { Apply(fatigue, 1) }, new GameAction[] { Apply(exhaustion, 1), Apply(stun, 1) })); extra = " A failed save causes exhaustion and stun for 1 round; a successful save causes fatigue for 1 round."; }
                var spell = Spell(f, "MindThrust" + circle, "Mind Thrust " + Roman[circle],
                    "Overload an intelligent creature's mind, dealing 1d" + (int)die + " direct damage per caster level (maximum " + cap + " dice). Will halves the damage." + extra + (i > 0 ? " This spell can be undercast." : ""),
                    SpellSchool.Divination, SpellDescriptor.MindAffecting, AbilityRange.Close, UnitCommand.CommandType.Standard,
                    Save(SavingThrowType.Will, effects.ToArray()), new AbilityTargetHasFact { Inverted = intelligent.Inverted });
                Set(spell.ComponentsArray.OfType<AbilityTargetHasFact>().Single(), "m_CheckedFacts", Get(intelligent, "m_CheckedFacts"));
                spell.SpellResistance = true; spell.AvailableMetamagic = offensiveMeta | Kingmaker.UnitLogic.Abilities.Metamagic.Intensified;
                AddFamily(spell, "MindThrust", circle, thrust); thrust.Add(spell); result.Add(new Entry { Level = circle, Ability = spell, Token = "MindThrust" + circle });
            }
            var barriers = new List<BlueprintAbility>();
            for (int i = 0; i < 5; i++) {
                int bonus = Math.Min(8, 4+2*i); string token = "MentalBarrier"+(i+1);
                string description = "A mental barrier grants a +" + bonus + " shield bonus to AC for 2 rounds.";
                var components = new List<BlueprintComponent> { new AddStatBonus { name = "MentalBarrierShieldAC", Stat = StatType.AC, Value = bonus, Descriptor = ModifierDescriptor.Shield } };
                if (i >= 3) { components.Add(new AddFortification { name = "MentalBarrierFortification", Bonus = (i-2)*25 }); description += " Critical hits and sneak attacks have a " + (i-2)*25 + "% chance to lose their additional damage."; }
                var buff = f.Buff(token+"Buff", "Mental Barrier " + Roman[i+1], description, components.ToArray());
                var spell = Spell(f, token, "Mental Barrier " + Roman[i+1], description + (i>0 ? " This spell can be undercast." : ""), SpellSchool.Abjuration, SpellDescriptor.None,
                    AbilityRange.Personal, UnitCommand.CommandType.Swift, Run(Apply(buff, 2, toCaster:true)));
                spell.AvailableMetamagic = Kingmaker.UnitLogic.Abilities.Metamagic.Heighten | Kingmaker.UnitLogic.Abilities.Metamagic.Extend;
                AddFamily(spell, "MentalBarrier", i+2, barriers); barriers.Add(spell); result.Add(new Entry { Level=i+2, Ability=spell, Token=token });
            }
            var shields = new List<BlueprintAbility>();
            for (int i = 0; i < 3; i++) {
                string token = "ThoughtShield"+(i+1), name = "Thought Shield " + Roman[i+1];
                string description = "For 2 rounds, gain a +" + (4+2*i) + " circumstance bonus on saving throws against mind-affecting effects.";
                var buff = f.Buff(token+"Buff", name, description, new SavingThrowBonusAgainstDescriptor { name="ThoughtShieldMindSaves", Value=4+2*i, Bonus=0, ModifierDescriptor=ModifierDescriptor.Circumstance, SpellDescriptor=SpellDescriptor.MindAffecting });
                var spell = Spell(f, token, name, description + (i>0 ? " This spell can be undercast." : ""), SpellSchool.Abjuration, SpellDescriptor.None, AbilityRange.Personal,
                    UnitCommand.CommandType.Swift, Run(Apply(buff, 2, toCaster:true)));
                spell.AvailableMetamagic = Kingmaker.UnitLogic.Abilities.Metamagic.Heighten | Kingmaker.UnitLogic.Abilities.Metamagic.Extend;
                AddFamily(spell, "ThoughtShield", i+2, shields); shields.Add(spell); result.Add(new Entry { Level=i+2, Ability=spell, Token=token });
            }
            var suppressed = f.Buff("IntellectFortressEffect", "Intellect Fortress", "Emotion and fear effects are suppressed while inside Intellect Fortress.",
                new SuppressBuffs { name="SuppressEmotionsFear", Descriptor=SpellDescriptor.Fear | SpellDescriptor.Emotion | SpellDescriptor.NegativeEmotion, Schools=Array.Empty<SpellSchool>() });
            var host = Aura(f, "IntellectFortress", "Intellect Fortress", "Suppress all fear and emotion effects within 20 feet for 2 rounds.", suppressed, 20);
            var fortress = Spell(f, "IntellectFortress", "Intellect Fortress", "Use pure logic to suppress fear and emotion effects within 20 feet for 2 rounds. Effects resume on leaving the area or when the spell ends.",
                SpellSchool.Abjuration, SpellDescriptor.None, AbilityRange.Personal, UnitCommand.CommandType.Swift, Run(Apply(host, 2, toCaster:true)));
            fortress.AvailableMetamagic=Kingmaker.UnitLogic.Abilities.Metamagic.Heighten | Kingmaker.UnitLogic.Abilities.Metamagic.Extend;
            result.Add(new Entry { Level=4, Ability=fortress, Token="IntellectFortress" });
            for (int i = 0; i < 2; i++) {
                string token = i==0 ? "SynapticPulse" : "SynapticPulseGreater", name = i==0 ? "Synaptic Pulse" : "Synaptic Pulse, Greater";
                var applyStun=Apply(stun, 1); if(i==1) { applyStun.DurationValue.DiceType=DiceType.D4; applyStun.DurationValue.DiceCountValue=1; applyStun.DurationValue.BonusValue=0; }
                var area = new AbilityTargetsAround { name="PsychicPulseTargets" }; Set(area,"m_Radius",new Feet(30)); Set(area,"m_TargetType",TargetType.Any);
                Set(area,"m_Condition",new ConditionsChecker { Conditions=Array.Empty<Condition>() });
                var spell = Spell(f, token, name, i==0 ? "Stun creatures within 30 feet for 1 round. Will negates. You are excluded." : "Stun creatures within 30 feet for 1d4 rounds. A successful Will save instead causes sickness for 1 round. You are excluded.",
                    SpellSchool.Enchantment,SpellDescriptor.Stun|SpellDescriptor.Compulsion|SpellDescriptor.MindAffecting,AbilityRange.Personal,UnitCommand.CommandType.Standard,
                    Save(SavingThrowType.Will, Saved(i==0 ? Array.Empty<GameAction>() : new GameAction[] { Apply(sickened,1) }, new GameAction[] { applyStun })),area);
                spell.CanTargetFriends=true; spell.CanTargetEnemies=true;
                spell.GetComponent<AbilityEffectRunAction>().IgnoreCaster=true; spell.SpellResistance=true;
                spell.AvailableMetamagic=Kingmaker.UnitLogic.Abilities.Metamagic.Quicken|Kingmaker.UnitLogic.Abilities.Metamagic.Extend|Kingmaker.UnitLogic.Abilities.Metamagic.Heighten|Kingmaker.UnitLogic.Abilities.Metamagic.Persistent|Kingmaker.UnitLogic.Abilities.Metamagic.Piercing|Kingmaker.UnitLogic.Abilities.Metamagic.Selective;
                result.Add(new Entry { Level=i==0?3:5, Ability=spell, Token=token });
            }
            return result;
        }
        private static BlueprintAbility Spell(PsychicAssets f, string token, string name, string description, SpellSchool school, SpellDescriptor descriptor,
            AbilityRange range, UnitCommand.CommandType action, params BlueprintComponent[] components)
        {
            var spell=f.Ability(token, name, description, AbilityType.Spell, action, range,
                components.Concat(new BlueprintComponent[] { new SpellComponent { name="PsychicSpellSchool", School=school }, new SpellDescriptorComponent { name="PsychicSpellDescriptors", Descriptor=descriptor } }).ToArray());
            return spell;
        }
        private static void AddFamily(BlueprintAbility spell, string family, int circle, List<BlueprintAbility> lower)
            => spell.ComponentsArray=spell.ComponentsArray.Concat(new BlueprintComponent[] { new PsychicUndercastFamily { name="PsychicUndercastFamily", Family=family, Circle=circle, LowerSpells=lower.Select(Ref<BlueprintAbilityReference>).ToArray() } }).ToArray();
        internal static BlueprintBuff Aura(PsychicAssets f, string token, string name, string description, BlueprintBuff effect, int feet)
        {
            var areaBuff=new AbilityAreaEffectBuff { name="PsychicAreaBuff", Condition=new ConditionsChecker { Conditions=Array.Empty<Condition>() } };
            Set(areaBuff,"m_Buff",Ref<BlueprintBuffReference>(effect));
            var area=f.Add(new BlueprintAbilityAreaEffect { Shape=AreaEffectShape.Cylinder, Size=new Feet(feet), AffectEnemies=true,
                ComponentsArray=new BlueprintComponent[] { areaBuff } },token+"Area",name,description);
            SetEnum(area,"m_TargetType","Any");
            var add=new AddAreaEffect { name="PsychicAttachedArea" }; Set(add,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));
            return f.Buff(token+"Host",name,description,add);
        }
    }
}
