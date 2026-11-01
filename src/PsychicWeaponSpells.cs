using System;
using Kingmaker.Controllers.Units;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicWeaponSpells
    {
        internal static readonly string[] Tokens={"DazzlingBlade","VineStrike","FlamesOfTheFaithful","WrathfulWeapon","WeaponSpellAnchor"};
        static ContextValue Rank(AbilityRankType type=AbilityRankType.Default)=>new ContextValue {ValueType=ContextValueType.Rank,ValueRank=type};
        static ContextRankConfig Scaling(string name,AbilityRankType type=AbilityRankType.Default){var r=new ContextRankConfig {name=name};SetEnum(r,"m_BaseValueType","CasterLevel");Set(r,"m_Type",type);return r;}
        static AbilitySpawnFx Fx(string id)=>new AbilitySpawnFx {name="WeaponSpellVisual",PrefabLink=new PrefabLink {AssetId=id},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None};
        static void Display(BlueprintAbility a,string duration,string save=""){a.LocalizedDuration=Text(a.name,"duration",duration);a.LocalizedSavingThrow=Text(a.name,"savingThrow",save);}
        static ContextActionRemoveBuff Remove(BlueprintBuff b,bool caster=false){var a=new ContextActionRemoveBuff {ToCaster=caster};Set(a,"m_Buff",Ref<BlueprintBuffReference>(b));return a;}
        static BlueprintAbility Spell(PsychicAssets f,string token,string name,string text,BlueprintBuff buff,SpellSchool school,bool rounds=false,bool personal=false,UnitCommand.CommandType action=UnitCommand.CommandType.Standard,string fx="352469f228a3b1f4cb269c7ab0409b8e"){
            var apply=Apply(buff,0,rounds?DurationRate.Rounds:DurationRate.Minutes);apply.AsChild=false;apply.DurationValue.BonusValue=Rank();
            var a=f.Ability(token,name,text,AbilityType.Spell,action,personal?AbilityRange.Personal:AbilityRange.Touch,new SpellComponent {name="WeaponSpellSchool",School=school},Scaling("WeaponSpellDuration"),Run(apply),Fx(fx));
            a.CanTargetSelf=true;a.CanTargetFriends=!personal;a.CanTargetEnemies=false;a.SpellResistance=false;a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|(personal?0:Metamagic.Reach);Set(a,"m_Icon",buff.Icon);Display(a,rounds?"1 round per caster level":"1 minute per caster level");return a;
        }
        static BlueprintAbility Root(PsychicAssets f,string token,string name,string text,BlueprintAbility[] variants){
            var root=f.Clone(variants[0],token,name,text);var v=new AbilityVariants {name=token+"Choices"};Set(v,"m_Variants",variants.Select(Ref<BlueprintAbilityReference>).ToArray());
            root.ComponentsArray=new BlueprintComponent[]{new SpellComponent {name=token+"School",School=variants[0].School},v};
            foreach(var a in variants)Set(a,"m_Parent",Ref<BlueprintAbilityReference>(root));return root;
        }
        internal static void Build(PsychicAssets f){
            // An effect-free anchor records the exact weapon through native serialized
            // BuffEnchantWornItemData, even when a requested property already exists.
            var anchor=f.Clone(f.Native<BlueprintWeaponEnchantment>("6cbb732b9d638724a960d784634dcdcf"),"WeaponSpellAnchorEnchantment","","Temporary spell anchor.");
            anchor.ComponentsArray=Array.Empty<BlueprintComponent>();Set(anchor,"m_EnchantmentCost",0);Set(anchor,"m_EnchantName",new Kingmaker.Localization.LocalizedString());Set(anchor,"m_Description",new Kingmaker.Localization.LocalizedString());anchor.WeaponFxPrefab=new PrefabLink();Set(anchor,"m_HiddenInUI",true);Set(anchor,"m_Prefix",new Kingmaker.Localization.LocalizedString());Set(anchor,"m_Suffix",new Kingmaker.Localization.LocalizedString());
            Wrathful(f,anchor);Flames(f,anchor);Vines(f);Dazzling(f,anchor);
        }
        static PsychicFixedWeaponEnchantment Enchant(BlueprintWeaponEnchantment anchor,BlueprintWeaponEnchantment effect,bool off=false)=>new PsychicFixedWeaponEnchantment {name="FixedWeaponSpell",Anchor=Ref<BlueprintItemEnchantmentReference>(anchor),Effect=effect==null?null:Ref<BlueprintItemEnchantmentReference>(effect),OffHand=off};
        static void Wrathful(PsychicAssets f,BlueprintWeaponEnchantment anchor){
            const string text="For 1 minute per caster level, grant a manufactured weapon in the selected hand anarchic, axiomatic, holy or unholy. Choose the property and main or off hand. A weapon that already has the selected property cannot be targeted. Different properties can coexist; identical properties do not stack. Removing or switching away from the weapon ends its spell. No enhancement bonus is added.";
            var enchants=new[]{f.Native<BlueprintWeaponEnchantment>("d05753b8df780fc4bb55b318f06af453"),f.Native<BlueprintWeaponEnchantment>("28a9964d81fedae44bae3ca45710c140"),f.Native<BlueprintWeaponEnchantment>("57315bc1e1f62a741be0efde688087e9"),f.Native<BlueprintWeaponEnchantment>("0ca43051edefcad4b9b2240aa36dc8d4")};
            var icons=new[]{PsychicOriginalIcons.Load("HWUnholy"),f.Native<Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility>("ce0ece459ebed9941bb096f559f36fa8").Icon,PsychicOriginalIcons.Load("HWAnarchic"),f.Native<Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility>("d76e8a80ab14ac942b6a9b8aaa5860b1").Icon};
            var names=new[]{"Unholy","Holy","Anarchic","Axiomatic"};var descriptors=new[]{SpellDescriptor.Evil,SpellDescriptor.Good,SpellDescriptor.Chaos,SpellDescriptor.Law};var variants=new BlueprintAbility[8];
            for(int i=0;i<4;i++)for(int hand=0;hand<2;hand++){
                string token="WrathfulWeapon"+names[i]+(hand==1?"OffHand":"MainHand"),name="Wrathful Weapon: "+names[i]+(hand==1?" (Off-Hand)":" (Main Hand)");
                var b=f.Buff(token+"Buff",name,text,Enchant(anchor,enchants[i],hand==1));Set(b,"m_Icon",icons[i]);
                var a=Spell(f,token,name,text,b,SpellSchool.Transmutation);
                a.ComponentsArray=a.ComponentsArray.Concat(new BlueprintComponent[]{new SpellDescriptorComponent {name="WrathfulAlignment",Descriptor=descriptors[i]},new PsychicWeaponTarget {OffHand=hand==1,ForbiddenEnchantment=Ref<BlueprintItemEnchantmentReference>(enchants[i])}}).ToArray();variants[i*2+hand]=a;
            }
            var root=Root(f,"WrathfulWeapon","Wrathful Weapon",text,variants);Set(root,"m_Icon",icons[1]);
        }
        static void Flames(PsychicAssets f,BlueprintWeaponEnchantment anchor){
            const string text="Your main-hand manufactured weapon gains flaming, dealing an additional 1d6 fire damage on a hit, for 1 round per caster level. While your judgment is active, it also gains flaming burst's extra fire damage on critical hits. These properties do not stack with the same existing properties. Removing or switching away from the weapon ends this spell.";
            var c=Enchant(anchor,f.Native<BlueprintWeaponEnchantment>("30f90becaaac51f41bf56641966c4121"));var nativeBurst=f.Native<BlueprintWeaponEnchantment>("3f032a3cd54e57649a0cdad0434bf221");
            var critical=f.Clone(nativeBurst,"FlamesOfTheFaithfulBurstEnchantment","Flames of the Faithful: Burst",text);critical.ComponentsArray=critical.ComponentsArray.Where(x=>x is WeaponEnergyBurst).ToArray();Set(critical,"m_EnchantmentCost",0);
            c.Burst=Ref<BlueprintItemEnchantmentReference>(critical);c.NativeBurst=Ref<BlueprintItemEnchantmentReference>(nativeBurst);c.Judgment=Ref<BlueprintBuffReference>(f.Native<BlueprintBuff>("9b8bb2ce8f67e5b4fa634ed6a6671f7a"));
            var b=f.Buff("FlamesOfTheFaithfulBuff","Flames of the Faithful",text,c);Set(b,"m_Icon",f.Native<BlueprintBuff>("32e17840df49fbd48b835d080f5673a4").Icon);
            var a=Spell(f,"FlamesOfTheFaithful","Flames of the Faithful",text,b,SpellSchool.Transmutation,true,true);a.ComponentsArray=a.ComponentsArray.Concat(new BlueprintComponent[]{new SpellDescriptorComponent {name="FaithfulFire",Descriptor=SpellDescriptor.Fire},new PsychicWeaponTarget {CheckCaster=true}}).ToArray();
        }
        static void Vines(PsychicAssets f){
            const string text="Choose unarmed strikes, claws, bites, gore or other natural weapons. For 1 minute per caster level, hits with that category deal an extra 1d6 piercing damage, not multiplied on critical hits. Once per target per round, a hit also allows a Reflex save; failure entangles that target for 1 round. This uses Call of the Wild's one-round entangle implementation. The same caster cannot force another vine save on that target during that round, even after a successful save. Different chosen categories can coexist; repeated castings for the same category do not stack.";
            var native=f.Native<BlueprintBuff>("f7f6330726121cf4b90a6086b05d2e38");var cooldown=f.Buff("VineStrikeCooldownBuff","Vine Strike: Save Cooldown",text);SetEnum(cooldown,"m_Flags","HiddenInUi");cooldown.Stacking=StackingType.Stack;Set(cooldown,"m_Icon",native.Icon);
            var cats=new[]{WeaponCategory.UnarmedStrike,WeaponCategory.Claw,WeaponCategory.Bite,WeaponCategory.Gore,WeaponCategory.OtherNaturalWeapons};var names=new[]{"Unarmed Strike","Claw","Bite","Gore","Other Natural Weapons"};var variants=new BlueprintAbility[5];
            for(int i=0;i<5;i++){
                var entangle=Apply(native,1);entangle.AsChild=false;var lockout=Apply(cooldown,1);lockout.AsChild=false;lockout.IsNotDispelable=true;Set(lockout.DurationValue,"m_IsExtendable",false);Set(entangle.DurationValue,"m_IsExtendable",false);
                var ready=new ContextConditionHasBuffFromCaster {Not=true};Set(ready,"m_Buff",Ref<BlueprintBuffReference>(cooldown));
                var conditional=new Conditional {ConditionsChecker=new ConditionsChecker {Conditions=new Condition[]{ready}},IfTrue=Actions(lockout,new ContextActionSavingThrow {Type=SavingThrowType.Reflex,Actions=Actions(Saved(Array.Empty<GameAction>(),new GameAction[]{entangle}))}),IfFalse=Actions()};
                var damage=new ContextActionDealDamage {DamageType=new DamageTypeDescription {Type=DamageType.Physical,Physical=new DamageTypeDescription.PhysicalData {Form=PhysicalDamageForm.Piercing}},Value=new ContextDiceValue {DiceType=DiceType.D6,DiceCountValue=1,BonusValue=0},IgnoreCritical=true,DisableSneakDamage=true};
                var b=f.Buff("VineStrike"+cats[i]+"Buff","Vine Strike: "+names[i],text,new AddInitiatorAttackWithWeaponTrigger {name="VinesOnHit",OnlyHit=true,CheckWeaponCategory=true,Category=cats[i],Action=Actions(damage,conditional)});Set(b,"m_Icon",native.Icon);
                var a=Spell(f,"VineStrike"+cats[i],"Vine Strike: "+names[i],text,b,SpellSchool.Conjuration,personal:true);a.AvailableMetamagic|=Metamagic.Empower|Metamagic.Maximize|Metamagic.Persistent;variants[i]=a;
            }
            Root(f,"VineStrike","Vine Strike",text,variants);
        }
        static void Dazzling(PsychicAssets f,BlueprintWeaponEnchantment anchor){
            const string text="A manufactured melee weapon in the target's main hand shines for 1 minute per caster level. Its wielder gains a +1 competence bonus on disarm checks, CMD against disarm, and Bluff checks (including every Feint check), increasing by +1 per 3 caster levels (maximum +5). As a free action, Dazzling Blade: Release ends the spell and flashes at an adjacent enemy: Will failure blinds for 1 round, success dazzles for 1 round. The save uses the original caster's spell DC. The weapon does not become silver. Removing or switching away from the weapon ends the spell.";
            var glow=f.Clone(f.Native<BlueprintWeaponEnchantment>("6cbb732b9d638724a960d784634dcdcf"),"DazzlingBladeGlowEnchantment","Dazzling Blade","");glow.ComponentsArray=Array.Empty<BlueprintComponent>();Set(glow,"m_Prefix",new Kingmaker.Localization.LocalizedString());Set(glow,"m_Suffix",new Kingmaker.Localization.LocalizedString());Set(glow,"m_EnchantmentCost",0);Set(glow,"m_EnchantName",Text("DazzlingBladeGlow","name","Dazzling Blade"));Set(glow,"m_Description",Text("DazzlingBladeGlow","description",text));
            var rank=Scaling("DazzlingBladeBonus");SetEnum(rank,"m_Progression","OnePlusDivStep");Set(rank,"m_StepLevel",3);Set(rank,"m_UseMax",true);Set(rank,"m_Max",5);
            var b=f.Buff("DazzlingBladeBuff","Dazzling Blade",text,rank,new CMBBonusForManeuver {name="DazzlingDisarm",Descriptor=ModifierDescriptor.Competence,Value=Rank(),Maneuvers=new[]{CombatManeuver.Disarm}},new PsychicDazzlingBonuses(),new AddContextStatBonus {name="DazzlingBluff",Stat=StatType.CheckBluff,Descriptor=ModifierDescriptor.Competence,Value=Rank(),Multiplier=1},Enchant(anchor,glow));Set(b,"m_Icon",f.Native<BlueprintBuff>("50d0501ad05f15d498c3aa8d602af273").Icon);
            var blind=Apply(f.Native<BlueprintBuff>("187f88d96a0ef464280706b63635f2af"),1);blind.AsChild=false;var dazzle=Apply(f.Native<BlueprintBuff>("df6d1025da07524429afbae248845ecc"),1);dazzle.AsChild=false;
            var required=new AbilityCasterHasFacts {name="DazzlingRequiresSpell"};Set(required,"m_Facts",new[]{Ref<BlueprintUnitFactReference>(b)});
            var release=f.Ability("DazzlingBladeRelease","Dazzling Blade: Release",text,AbilityType.SpellLike,UnitCommand.CommandType.Free,AbilityRange.Touch,required,new SpellComponent {name="DazzlingReleaseSchool",School=SpellSchool.Illusion},new SpellDescriptorComponent {name="DazzlingReleaseBlindness",Descriptor=SpellDescriptor.Blindness},Save(SavingThrowType.Will,Saved(new GameAction[]{dazzle},new GameAction[]{blind})),new AbilityExecuteActionOnCast {name="DazzlingDischarge",Conditions=new ConditionsChecker {Conditions=Array.Empty<Condition>()},Actions=Actions(Remove(b,true))},Fx("790eb82d267bf0749943fba92b7953c2"));
            release.CanTargetEnemies=true;release.CanTargetFriends=false;release.CanTargetSelf=false;release.SpellResistance=false;release.AvailableMetamagic=Metamagic.Persistent;Set(release,"m_Icon",b.Icon);Display(release,"1 round","Will partial");
            var inherited=new ReplaceAbilityParamsWithContext {name="DazzlingOriginalCasterDC"};Set(inherited,"m_Ability",Ref<BlueprintAbilityReference>(release));b.ComponentsArray=b.ComponentsArray.Concat(new BlueprintComponent[]{Facts(release),inherited}).ToArray();
            var single=Spell(f,"DazzlingBlade","Dazzling Blade",text,b,SpellSchool.Illusion,action:UnitCommand.CommandType.Swift,fx:"790eb82d267bf0749943fba92b7953c2");single.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Reach|Metamagic.Persistent;single.ComponentsArray=single.ComponentsArray.Concat(new BlueprintComponent[]{new PsychicWeaponTarget {MeleeOnly=true}}).ToArray();
            var mass=f.Clone(single,"DazzlingBladeMass","Dazzling Blade, Mass","Affects allies wielding a main-hand manufactured melee weapon in a 15-foot burst within close range. Each wielder can discharge the spell independently.\n"+text);mass.ComponentsArray=mass.ComponentsArray.Where(c=>!(c is PsychicWeaponTarget)).ToArray();
            var area=new AbilityTargetsAround {name="DazzlingAllies"};Set(area,"m_Radius",new Feet(15));Set(area,"m_TargetType",TargetType.Ally);Set(area,"m_Condition",new ConditionsChecker {Conditions=new Condition[]{new PsychicHasMeleeWeapon()}});mass.ComponentsArray=mass.ComponentsArray.Concat(new BlueprintComponent[]{area}).ToArray();mass.Range=AbilityRange.Close;mass.CanTargetPoint=true;
        }
    }
    [AllowedOn(typeof(BlueprintAbility),false)]
    [TypeId("755374856cbf4ffaaa2653ba95a57dd4")]
    public sealed class PsychicWeaponTarget : BlueprintComponent,IAbilityTargetRestriction,IAbilityCasterRestriction
    {
        public bool OffHand,MeleeOnly,CheckCaster;
        public BlueprintItemEnchantmentReference ForbiddenEnchantment;
        internal static ItemEntityWeapon Weapon(UnitEntityData unit,bool off)=>off?unit?.Body?.SecondaryHand?.MaybeWeapon:unit?.Body?.PrimaryHand?.MaybeWeapon;
        internal static bool Valid(ItemEntityWeapon weapon,bool melee)=>weapon!=null&&!weapon.Blueprint.IsNatural&&!weapon.Blueprint.IsUnarmed&&(!melee||weapon.Blueprint.IsMelee);
        bool Check(UnitEntityData u){var w=Weapon(u,OffHand);return Valid(w,MeleeOnly)&&(ForbiddenEnchantment==null||!w.Enchantments.Any(e=>e.Blueprint==ForbiddenEnchantment.Get()));}
        public bool IsTargetRestrictionPassed(UnitEntityData caster,TargetWrapper target)=>Check(CheckCaster?caster:target?.Unit);
        public bool IsCasterRestrictionPassed(UnitEntityData caster)=>!CheckCaster||Check(caster);
        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster,TargetWrapper target)=>"Requires an eligible manufactured weapon in the selected hand without the selected property.";
        public string GetAbilityCasterRestrictionUIText()=>"Requires a manufactured main-hand weapon.";
    }
    [TypeId("f1dc5f9721b14a12ad0154d46b6a92e4")]
    public sealed class PsychicHasMeleeWeapon : ContextCondition
    {
        protected override string GetConditionCaption()=>"Target wields a manufactured melee weapon";
        protected override bool CheckCondition()=>PsychicWeaponTarget.Valid(PsychicWeaponTarget.Weapon(Target.Unit,false),true);
    }
    [AllowedOn(typeof(BlueprintBuff),false)]
    [TypeId("91e4bb3a9f4148cbbbf35b1eead6974c")]
    public sealed class PsychicFixedWeaponEnchantment : UnitBuffComponentDelegate<BuffEnchantWornItemData>,IUnitActiveEquipmentSetHandler,IUnitEquipmentHandler,IUnitGainFactHandler,IUnitLostFactHandler,IUnitSubscriber,ITickEachRound
    {
        public BlueprintItemEnchantmentReference Anchor,Effect,Burst,NativeBurst;
        public BlueprintBuffReference Judgment;
        public bool OffHand;
        ItemEntityWeapon Bound=>Data.Enchantments.FirstOrDefault(e=>e!=null&&e.Blueprint==Anchor.Get())?.Owner as ItemEntityWeapon;
        // Polymorph replaces PrimaryHand/SecondaryHand with claws, but the
        // original equipment set still owns the melded spell-bound item.
        // Keep the same binding; a real equipment swap still ends the spell.
        internal static ItemEntityWeapon BoundHand(UnitEntityData unit,bool off){
            if(unit?.Body?.IsPolymorphed==true){var set=unit.Body.HandsEquipmentSets[unit.Body.CurrentHandEquipmentSetIndex];return (off?set?.SecondaryHand:set?.PrimaryHand)?.MaybeItem as ItemEntityWeapon;}
            return PsychicWeaponTarget.Weapon(unit,off);
        }
        protected override void OnTurnOn(){
            var weapon=Bound??BoundHand(Owner,OffHand);if(weapon==null)return;
            if(Bound==null)Data.Enchantments.Add(weapon.AddEnchantment(Anchor.Get(),Context));
            if(NativeBurst==null||!weapon.Enchantments.Any(e=>e.Blueprint==NativeBurst.Get()))Add(weapon,Effect);Refresh();
        }
        void Add(ItemEntityWeapon weapon,BlueprintItemEnchantmentReference reference){if(reference!=null&&!weapon.Enchantments.Any(e=>e.Blueprint==reference.Get()))Data.Enchantments.Add(weapon.AddEnchantment(reference.Get(),Context));}
        protected override void OnTurnOff(){foreach(var e in Data.Enchantments.ToArray())e?.Owner?.RemoveEnchantment(e);Data.Enchantments.Clear();}
        void Refresh(EntityFact leaving=null,bool entering=false){
            var weapon=Bound;if(weapon==null)return;
            if(weapon!=BoundHand(Owner,OffHand)){Buff.Remove();return;}
            if(Burst==null)return;
            bool nativeBurst=NativeBurst!=null&&weapon.Enchantments.Any(e=>e.Blueprint==NativeBurst.Get());
            if(!nativeBurst)Add(weapon,Effect);
            else foreach(var e in Data.Enchantments.Where(e=>Effect!=null&&e.Blueprint==Effect.Get()).ToArray()){e.Owner?.RemoveEnchantment(e);Data.Enchantments.Remove(e);}
            bool active=entering||Owner.Facts.List.Any(f=>f!=leaving&&!f.IsDisposed&&f.Blueprint==Judgment.Get());
            if(active&&(NativeBurst==null||!weapon.Enchantments.Any(e=>e.Blueprint==NativeBurst.Get())))Add(weapon,Burst);else foreach(var e in Data.Enchantments.Where(e=>e.Blueprint==Burst.Get()).ToArray()){e.Owner?.RemoveEnchantment(e);Data.Enchantments.Remove(e);}
        }
        public void HandleEquipmentSlotUpdated(ItemSlot slot,ItemEntity previousItem)=>Refresh();
        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit)=>Refresh();
        public void HandleUnitGainFact(EntityFact fact){if(Judgment!=null&&fact?.Blueprint==Judgment.Get())Refresh(entering:true);}
        public void HandleUnitLostFact(EntityFact fact){if(Judgment!=null&&fact?.Blueprint==Judgment.Get())Refresh(leaving:fact);}
        public void OnNewRound()=>Refresh();
    }
    [AllowedOn(typeof(BlueprintBuff),false)]
    [TypeId("5ed4937687b04b37906b4cc46cd54a51")]
    public sealed class PsychicDazzlingBonuses : UnitFactComponentDelegate,ITargetRulebookHandler<RuleCalculateCMD>
    {
        public void OnEventAboutToTrigger(RuleCalculateCMD evt){if(evt.Type==CombatManeuver.Disarm)evt.AddModifier(new ContextValue {ValueType=ContextValueType.Rank}.Calculate(Context),Fact,ModifierDescriptor.Competence);}
        public void OnEventDidTrigger(RuleCalculateCMD evt){}
    }
}
