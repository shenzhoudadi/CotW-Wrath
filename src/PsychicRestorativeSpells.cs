using System;
using System.IO;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityEngine;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicRestorativeSpells
    {
        private static Sprite forceIcon;
        internal static Sprite ForceIcon()
        {
            if (!ReferenceEquals(forceIcon,null)) return forceIcon;
            using(var stream=typeof(PsychicRestorativeSpells).Assembly.GetManifestResourceStream("CotW.ForceSword.png")) {
                if(stream==null) throw new InvalidOperationException("Missing embedded CotW Force Sword icon.");
                using(var bytes=new MemoryStream()) {
                    stream.CopyTo(bytes);
                    var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    if(!ImageConversion.LoadImage(texture,bytes.ToArray())) throw new InvalidOperationException("Invalid Force Sword icon.");
                    forceIcon=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(0.5f,0.5f));
                }
            }
            return forceIcon;
        }
        internal static PsychicSpells.Entry ForceSword(PsychicAssets f)
        {
            const string text="Create a longsword of pure force in the target's empty primary hand for 1 minute per caster level. It deals normal longsword damage as force damage, using normal weapon attacks and proficiency. Its enhancement bonus is +1, increasing to +2 at caster level 8 and +3 at caster level 13. The hand is reserved while the sword exists; the sword vanishes when the effect ends. It cannot be sold or kept as a permanent item.";
            var icon=ForceIcon();
            var fx=f.Clone(f.Native<BlueprintWeaponEnchantment>("1d64abd0002b98043b199c0e3109d3ee"),"ForceSwordVisualEnchantment","Force Sword",text);
            fx.ComponentsArray=Array.Empty<BlueprintComponent>();
            var enhancement=new[]{"d42fc23b92c640846ac137dc26e000d4","eb2faccc4c9487d43b3575d7e77ff3f5","80bb8a737579e35498177e1e3c75899b"};
            var weapons=new BlueprintItemWeaponReference[3];
            for(int i=0;i<3;i++) {
                var weapon=f.Clone(f.Native<BlueprintItemWeapon>("6fd0a849531617844b195f452661b2cd"),"ForceSwordWeapon"+(i+1),"Force Sword +"+(i+1),text);
                Set(weapon,"m_DisplayNameText",Text("ForceSwordWeapon"+(i+1),"name","Force Sword +"+(i+1)));
                Set(weapon,"m_DescriptionText",Text("ForceSwordWeapon"+(i+1),"description",text));
                Set(weapon,"m_Icon",icon);Set(weapon,"m_Cost",0);Set(weapon,"m_Weight",0f);
                Set(weapon,"m_OverrideDamageType",true);Set(weapon,"m_DamageType",new DamageTypeDescription {Type=DamageType.Force});
                Set(weapon,"m_OverrideDamageDice",true);Set(weapon,"m_DamageDice",new DiceFormula(1,DiceType.D8));
                Set(weapon,"m_Enchantments",new[]{Ref<BlueprintWeaponEnchantmentReference>(f.Native<BlueprintWeaponEnchantment>(enhancement[i])),Ref<BlueprintWeaponEnchantmentReference>(fx)});
                // The donor can already have resolved its enchantments during another mod's load.
                Set(weapon,"m_CachedEnchantments",null);
                weapons[i]=Ref<BlueprintItemWeaponReference>(weapon);
            }
            var buff=f.Buff("ForceSwordBuff","Force Sword",text,new PsychicForceSword {name="CotWTemporaryForceSword",Weapons=weapons});
            Set(buff,"m_Icon",icon);buff.Stacking=StackingType.Replace;
            var rank=new ContextRankConfig {name="ForceSwordDuration"};SetEnum(rank,"m_BaseValueType","CasterLevel");
            var apply=Apply(buff,0,DurationRate.Minutes);apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
            var spell=f.Clone(f.Native<BlueprintAbility>("03a9630394d10164a9410882d31572f0"),"ForceSword","Force Sword",text);
            spell.ComponentsArray=spell.ComponentsArray.Where(c=>c is AbilitySpawnFx).Concat(new BlueprintComponent[]{
                new SpellComponent {name="ForceSwordEvocation",School=SpellSchool.Evocation},
                new SpellDescriptorComponent {name="ForceSwordDescriptor",Descriptor=SpellDescriptor.Force},rank,
                new PsychicForceSwordTarget {name="ForceSwordEmptyPrimaryHand"},Run(apply)}).ToArray();
            Set(spell,"m_Icon",icon);spell.Range=AbilityRange.Touch;spell.ActionType=UnitCommand.CommandType.Standard;
            spell.CanTargetSelf=true;spell.CanTargetFriends=true;spell.CanTargetEnemies=false;spell.SpellResistance=false;spell.NeedEquipWeapons=false;
            spell.AvailableMetamagic=Metamagic.Heighten|Metamagic.Reach|Metamagic.Extend|Metamagic.Quicken;
            spell.LocalizedDuration=Text("ForceSword","duration","1 minute per caster level");
            return new PsychicSpells.Entry {Token="ForceSword",Level=2,Ability=spell};
        }
        internal static PsychicSpells.Entry Surgery(PsychicAssets f)
        {
            const string text="Restore all Intelligence, Wisdom and Charisma damage and drain. Remove ongoing fear and confusion effects. Also remove harmful mind-affecting or enchantment effects: dispellable spell effects of any spell level, or other effects of spell level 6 or lower. Beneficial mental effects, physical ability damage, and effects without a removable ongoing buff are unaffected. Requires the same diamond dust component as Stoneskin.";
            ContextActionHealStatDamage Heal(bool drain) {
                var a=new ContextActionHealStatDamage {name=drain?"RestoreMentalDrain":"RestoreMentalDamage",HealDrain=drain};
                SetEnum(a,"m_HealType","HealAllDamage");SetEnum(a,"m_StatClass","Mental");return a;
            }
            var spell=f.Clone(f.Native<BlueprintAbility>("03a9630394d10164a9410882d31572f0"),"PsychicSurgery","Psychic Surgery",text);
            spell.ComponentsArray=spell.ComponentsArray.Where(c=>c is AbilitySpawnFx).Concat(new BlueprintComponent[]{
                new SpellComponent {name="PsychicSurgeryEnchantment",School=SpellSchool.Enchantment},
                new SpellDescriptorComponent {name="PsychicSurgeryMindAffecting",Descriptor=SpellDescriptor.MindAffecting},
                Run(Heal(false),Heal(true),new PsychicSurgeryCleanse {name="RemoveMentalAfflictions"})}).ToArray();
            spell.MaterialComponent=f.Native<BlueprintAbility>("c66e86905f7606c4eaa5c774f0357b2b").MaterialComponent;
            spell.Range=AbilityRange.Touch;spell.ActionType=UnitCommand.CommandType.Standard;spell.CanTargetSelf=true;spell.CanTargetFriends=true;spell.CanTargetEnemies=false;spell.SpellResistance=false;
            spell.AvailableMetamagic=Metamagic.Heighten|Metamagic.Reach|Metamagic.Quicken;
            return new PsychicSpells.Entry {Token="PsychicSurgery",Level=6,Ability=spell};
        }
    }
}
