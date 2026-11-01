// Mindblade manifestation/equipment lifetime adapted from original CotW
// MindBlade and CreateWeapon; Wrath owns actual items, enchantments and slots.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    [TypeId("e1c69dd9304e4ed49e30271c6fdf5454")]
    public sealed class MindbladeManifestCost01454:BlueprintComponent,IAbilityCasterRestriction,IAbilityResourceCostCalculator,IAbilityVisibilityProvider
    {
        public BlueprintItemWeaponReference Weapon;
        public BlueprintBuffReference Bane;
        public int ReservedCost;
        public bool Pair,Double,RequiresTwoHands;
        public bool IsAbilityVisible(AbilityData ability)=>ability?.Caster?.Proficiencies.WeaponProficiencies.Contains(Weapon.Get().Category)==true;
        internal int Ends=>Pair||Double?2:1;
        public int Calculate(AbilityData ability)=>ReservedCost+(Bane!=null&&ability?.Caster?.Buffs.HasFact(Bane.Get())==true?Ends:0);
        public bool IsCasterRestrictionPassed(UnitEntityData caster)
        {
            var bp=Weapon?.Get();if(caster==null||bp==null||!Mindblade01454.Has(caster,Mindblade01454.PoolToken))return false;
            if(!caster.Descriptor.Proficiencies.WeaponProficiencies.Contains(bp.Category))return false;
            if((Pair||Double)&&!Mindblade01454.Has(caster,Mindblade01454.DualToken))return false;
            bool dualMode=caster.Buffs.Enumerable.Any(b=>b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed&&!b.IsDisposed&&b.Blueprint.GetComponent<MindbladeDualMode01454>()!=null);
            if((Pair||Double)!=dualMode)return false;
            return PsychicGreedWeapon.HandFree(caster.Body.PrimaryHand)&&(!RequiresTwoHands||PsychicGreedWeapon.HandFree(caster.Body.SecondaryHand));
        }
        public string GetAbilityCasterRestrictionUIText()=>"Empty, unlocked hands, full weapon proficiency and the correct psychic weapon mode are required.";
    }
    [TypeId("8c740f0ba59f4c7e92ba9651f0e65454")]
    public sealed class MindbladeDualMode01454:UnitBuffComponentDelegate
    {
        protected override void OnTurnOff()=>Mindblade01454.WithManifest(Owner,c=>{c.End();return false;});
    }
    [TypeId("209e2d22342042b89b1933189cb35454")]
    public sealed class MindbladeProperty01454:UnitBuffComponentDelegate
    {
        public BlueprintWeaponEnchantmentReference Enchantment;
        public BlueprintAbilityReference BaneCharge;
        public int EnhancementCost;
        public bool Bane;
        protected override void OnTurnOn()=>Mindblade01454.WithManifest(Owner,c=>{if(Bane&&!c.PayBane(BaneCharge.Get())){Buff.Remove();return true;}c.Refresh();return false;});
        protected override void OnTurnOff()=>Mindblade01454.WithManifest(Owner,c=>{c.Refresh(Buff);return false;});
    }
    [TypeId("45eadfb9cb0d48bd8f0eb10ca8505454")]
    public sealed class MindbladeAlignment01454:ActivatableAbilityRestriction
    {
        public AlignmentComponent Component;
        protected override bool IsAvailable()=>Owner.Alignment.ValueRaw.HasComponent(Component);
    }
    [TypeId("f9b0acd5ee2142819b65f7159c7b5454")]
    public sealed class MindbladeBaneAvailable01454:ActivatableAbilityRestriction
    {
        public BlueprintAbilityReference Charge;
        protected override bool IsAvailable()
        {
            bool available=true;Mindblade01454.WithManifest(Owner,c=>{available=c.BanePaid||Charge.Get().GetComponent<AbilityResourceLogic>().IsAbilityRestrictionPassed(new AbilityData(Charge.Get(),Owner.Descriptor));return true;});return available;
        }
    }
    [TypeId("401bd0052e4b4213b70a0297e7ee5454")]
    public sealed class MindbladeBaneCost01454:BlueprintComponent,IAbilityResourceCostCalculator
    {
        public int Calculate(AbilityData ability){int count=0;Mindblade01454.WithManifest(ability?.Caster?.Unit,c=>{count=c.EndCount;return true;});return count;}
    }
    internal static class MindbladePayment01454
    {
        internal sealed class Paid {internal int Actual;internal int Reserved;internal BlueprintScriptableObject Resource;}
        static readonly ConditionalWeakTable<AbilityData,Paid> Payments=new ConditionalWeakTable<AbilityData,Paid>();
        internal static void Record(AbilityData ability,BlueprintScriptableObject resource,int actual,int reserved){Payments.Remove(ability);Payments.Add(ability,new Paid{Resource=resource,Actual=actual,Reserved=Math.Min(actual,reserved)});}
        internal static Paid Claim(AbilityData ability){if(ability==null||!Payments.TryGetValue(ability,out var value))return null;Payments.Remove(ability);return value;}
    }
    [HarmonyPatch(typeof(AbilityResourceLogic),nameof(AbilityResourceLogic.Spend))]
    internal static class MindbladeNativePayment01454
    {
        internal sealed class Check {internal int Before;internal bool Allowed;internal BlueprintScriptableObject Resource;}
        static void Prefix(AbilityResourceLogic __instance,AbilityData __0,out Check __state)
        {
            __state=null;if(__0?.Blueprint.GetComponent<MindbladeManifestCost01454>()==null||__instance.RequiredResource==null)return;
            var resource=__0.OverrideRequiredResource??__instance.RequiredResource;__state=new Check{Resource=resource,Before=__0.Caster.Resources.GetResourceAmount(resource),Allowed=__instance.IsAbilityRestrictionPassed(__0)};
        }
        static void Postfix(AbilityResourceLogic __instance,AbilityData __0,Check __state)
        {
            if(__state?.Allowed!=true)return;var marker=__0.Blueprint.GetComponent<MindbladeManifestCost01454>();if(marker==null)return;
            int actual=__state.Before-__0.Caster.Resources.GetResourceAmount(__state.Resource);if(actual<0)return;
            int bane=marker.Calculate(__0)-marker.ReservedCost;
            MindbladePayment01454.Record(__0,__state.Resource,actual,Math.Min(marker.ReservedCost,Math.Max(0,actual-bane)));
        }
    }
    public sealed class MindbladeManifestData01454
    {
        [JsonProperty] public ItemEntityWeapon[] Weapons;
        [JsonProperty] public ItemSlot[] LockedSlots;
        [JsonProperty] public List<ItemEnchantment> Enchantments=new List<ItemEnchantment>();
        [JsonProperty] public int Paid,Reserved;
        [JsonProperty] public BlueprintScriptableObject PaidResource;
        [JsonProperty] public bool Created,Refunded,BanePaid;
        [JsonIgnore] public bool Busy;
        [JsonIgnore] public string EnchantmentShape;
    }
    [TypeId("c0abbe3cb8ad4e6d9c9ac16667305454")]
    public sealed class MindbladeManifest01454:UnitBuffComponentDelegate<MindbladeManifestData01454>,IUnitEquipmentHandler,IUnitActiveEquipmentSetHandler,IUnitGainFactHandler,IUnitLostFactHandler,IUnitBuffHandler,IUnitSubscriber
    {
        public BlueprintItemWeaponReference Weapon;
        public BlueprintAbilityResourceReference Pool;
        public BlueprintCharacterClassReference Class;
        public BlueprintWeaponEnchantmentReference[] Enhancements;
        public bool Pair,RequiresTwoHands;
        internal int EndCount=>Pair||Weapon.Get().Double?2:1;
        internal bool BanePaid=>Data.BanePaid;
        internal bool Ready=>Buff.IsActive&&Buff.IsTurnedOn&&!Buff.IsSuppressed&&!Buff.IsDisposed&&!Data.Refunded&&Data.Created&&Data.Weapons?.Length==(Pair?2:1)&&ReferenceEquals(Owner.Body.PrimaryHand.MaybeWeapon,Data.Weapons[0])&&(!Pair||ReferenceEquals(Owner.Body.SecondaryHand.MaybeWeapon,Data.Weapons[1]));
        protected override void OnTurnOn()
        {
            if(Data.Created){if(Ready)Refresh();else End();return;}
            if(Data.Refunded){Buff.Remove();return;}
            var paid=MindbladePayment01454.Claim(Context?.SourceAbilityContext?.Ability);if(paid==null){Buff.Remove();return;}
            Data.PaidResource=paid.Resource;Data.Paid=paid.Actual;Data.Reserved=paid.Reserved;Data.Busy=true;
            try{
                var bp=Weapon.Get();bool both=RequiresTwoHands;
                if(!PsychicGreedWeapon.HandFree(Owner.Body.PrimaryHand)||both&&!PsychicGreedWeapon.HandFree(Owner.Body.SecondaryHand)){Buff.Remove();return;}
                var weapons=new List<ItemEntityWeapon>();var slots=new List<ItemSlot>();Data.Weapons=Array.Empty<ItemEntityWeapon>();Data.LockedSlots=Array.Empty<ItemSlot>();
                for(int i=0;i<(Pair?2:1);i++){
                    var item=bp.CreateEntity<ItemEntityWeapon>();item.MakeNotLootable();item.Identify();weapons.Add(item);Data.Weapons=weapons.ToArray();
                    var slot=i==0?Owner.Body.PrimaryHand:Owner.Body.SecondaryHand;
                    bool inserted;using(ContextData<ItemEntity.CanBeEquippedForce>.Request())inserted=slot.CanInsertItem(item)&&slot.InsertItem(item);
                    if(!inserted){Buff.Remove();return;}
                    slots.Add(slot);
                }
                if(!Pair&&both)slots.Add(Owner.Body.SecondaryHand);
                // Native off-hand insertion may adjust the primary hand. Lock
                // only after both insertions have succeeded, then own exactly
                // one retain per participating slot.
                foreach(var slot in slots)slot.Lock.Retain();Data.LockedSlots=slots.ToArray();
                Data.Created=true;Data.BanePaid=Owner.Buffs.Enumerable.Any(b=>b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed&&b.Blueprint.GetComponent<MindbladeProperty01454>()?.Bane==true);
            }catch{Buff.Remove();throw;}finally{Data.Busy=false;}
            Refresh();
        }
        internal bool PayBane(BlueprintAbility charge)
        {
            if(Data.BanePaid)return true;if(!Ready||charge==null)return false;
            var ability=new AbilityData(charge,Owner.Descriptor);var logic=charge.GetComponent<AbilityResourceLogic>();if(logic==null||!logic.IsAbilityRestrictionPassed(ability))return false;
            logic.Spend(ability);Data.BanePaid=true;return true;
        }
        internal ItemEntityWeapon[] Ends()=>Data.Weapons==null?Array.Empty<ItemEntityWeapon>():Data.Weapons.SelectMany(w=>w.Second==null?new[]{w}:new[]{w,w.Second}).ToArray();
        void RemoveEnchantments(){foreach(var item in Data.Enchantments.ToArray())item?.Owner?.RemoveEnchantment(item);Data.Enchantments.Clear();Data.EnchantmentShape=null;}
        internal void Refresh(Buff leaving=null)
        {
            if(Data.Busy||!Ready)return;Data.Busy=true;
            try{
                var ends=Ends();foreach(var item in ends)MagusEnchantments01454.Watch(item,Owner);
                var properties=Owner.Buffs.Enumerable.Where(b=>b!=leaving&&b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed&&!b.IsDisposed).Select(b=>b.Blueprint.GetComponent<MindbladeProperty01454>()).Where(p=>p!=null&&(!p.Bane||Data.BanePaid)).ToArray();
                int available=Owner.Ensure<UnitPartActivatableAbility>().GetGroupSize(ActivatableAbilityGroup.ArcaneWeaponProperty);
                available-=Owner.ActivatableAbilities.Enumerable.Where(a=>a.IsOn&&a.Blueprint.Group==ActivatableAbilityGroup.ArcaneWeaponProperty&&(leaving==null||!ReferenceEquals(a.AppliedBuff,leaving))).Sum(a=>a.Blueprint.WeightInGroup);
                int numeric=Math.Max(0,Math.Min(5,available));var refs=properties.Select(p=>p.Enchantment).Where(e=>e!=null).GroupBy(e=>e.Guid).Select(g=>g.First()).ToArray();
                var desired=(numeric>0?new BlueprintItemEnchantment[]{Enhancements[numeric-1].Get()}:Array.Empty<BlueprintItemEnchantment>()).Concat(refs.Select(e=>e.Get())).ToArray();
                string shape=numeric+"/"+string.Join("/",refs.Select(e=>e.Guid.ToString()).OrderBy(s=>s));if(shape==Data.EnchantmentShape&&MagusEnchantments01454.Current(ends,desired,Data.Enchantments))return;
                RemoveEnchantments();MagusEnchantments01454.AddMissing(ends,desired,Data.Enchantments,Context);
                Data.EnchantmentShape=shape;
            }finally{Data.Busy=false;}
        }
        internal bool Owns(ItemEntityWeapon item)=>Ready&&Ends().Any(w=>ReferenceEquals(w,item));
        internal bool AllowsSpellCombatGrip(bool inserting)
        {
            if(!(EndCount==2&&Mindblade01454.Has(Owner,Mindblade01454.DualToken)||EndCount==1&&RequiresTwoHands&&Mindblade01454.Has(Owner,Mindblade01454.DualManifestToken)))return false;
            if(Ready)return true;
            // Native equipment events run between the two insertions. Keep an
            // already selected Spell Combat mode during this owned transaction;
            // the actual attack gate still requires the complete Ready state.
            return inserting&&Data.Busy&&!Data.Refunded&&!Data.Created&&Data.Weapons?.Length>0&&ReferenceEquals(Owner.Body.PrimaryHand.MaybeItem,Data.Weapons[0]);
        }
        internal void End(){if(!Buff.IsDisposed)Buff.Remove();}
        protected override void OnTurnOff()
        {
            if(Data.Refunded)return;Data.Refunded=true;Data.Busy=true;
            try{
                foreach(var item in Ends())MagusEnchantments01454.Unwatch(item);RemoveEnchantments();var slots=Data.LockedSlots;Data.LockedSlots=null;if(slots!=null)foreach(var slot in slots)slot?.Lock.Release();
                var weapons=Data.Weapons;Data.Weapons=null;if(weapons!=null)foreach(var item in weapons){
                    var slot=item.HoldingSlot;if(slot!=null&&ReferenceEquals(slot.MaybeItem,item))using(ContextData<ItemSlot.IgnoreLock>.Request())slot.RemoveItem();item.Collection?.Remove(item);
                }
                int refund=Data.Created?Data.Reserved:Data.Paid;Data.Reserved=Data.Paid=0;var resource=Data.PaidResource;Data.PaidResource=null;if(refund>0&&resource!=null&&Owner.Resources.ContainsResource(resource))Owner.Resources.Restore(resource,refund);
            }finally{Data.Busy=false;}
        }
        protected override void OnApplyPostLoadFixes(){if(Ready){Data.EnchantmentShape=null;Refresh();}else End();}
        public void HandleEquipmentSlotUpdated(ItemSlot slot,ItemEntity previousItem){if(Data.Busy)return;if(!Ready)End();}
        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit){if(unit==Owner.Descriptor&&!Data.Busy&&!Ready)End();}
        public void HandleUnitGainFact(EntityFact fact){if(fact.Owner==Owner)Refresh();}
        public void HandleUnitLostFact(EntityFact fact){if(fact.Owner==Owner){if(!Mindblade01454.Has(Owner,Mindblade01454.PoolToken))End();else Refresh();}}
        public void HandleBuffDidAdded(Buff buff){if(buff.Owner==Owner)Refresh();}
        public void HandleBuffDidRemoved(Buff buff){if(buff.Owner==Owner)Refresh(buff);}
    }
    [TypeId("a7fdc5d726054527b856390190105454")]
    public sealed class MindbladeManifestMenu01454:BlueprintComponent {public bool Dual;}
    [HarmonyPatch(typeof(AbilityData),"get_ActionType")]
    internal static class MindbladeManifestAction01454
    {
        static void Postfix(AbilityData __instance,ref UnitCommand.CommandType __result)
        {
            var marker=__instance.Blueprint.GetComponent<MindbladeManifestCost01454>();var menu=__instance.Blueprint.GetComponent<MindbladeManifestMenu01454>();if(marker==null&&menu==null)return;var unit=__instance.Caster?.Unit;
            if(Mindblade01454.Has(unit,(marker!=null?(marker.Pair||marker.Double):menu.Dual)?Mindblade01454.DualManifestToken:Mindblade01454.RapidToken))__result=UnitCommand.CommandType.Swift;
        }
    }
}
