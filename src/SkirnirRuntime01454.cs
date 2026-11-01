// Original CotW Skirnir and SpellManipulationMechanics adapted to Wrath's
// native parameter/slot API. Ownership follows actual shield entities.
using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
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
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    [TypeId("9852c843c3a94e849d31455f2dc15454")]
    public sealed class SkirnirShieldRequired01454:BlueprintComponent,IAbilityCasterRestriction
    {
        public bool NoTower;
        public bool IsCasterRestrictionPassed(UnitEntityData caster)=>Skirnir01454.Shield(caster) is ItemEntityShield shield&&(!NoTower||shield.ArmorComponent.Blueprint.ProficiencyGroup!=ArmorProficiencyGroup.TowerShield);
        public string GetAbilityCasterRestrictionUIText()=>NoTower?"A bonded shield other than a tower shield is required.":"A shield is required.";
    }
    [TypeId("bce371b125c64782be4400837c4f5454")]
    public sealed class SkirnirToggleRequired01454:ActivatableAbilityRestriction
    {
        public bool Stored,LightArmor;
        protected override bool IsAvailable()=>Skirnir01454.Shield(Owner)!=null&&(!Stored||Skirnir01454.WithStorage(Owner,s=>s.Ready))&&(!LightArmor||Owner.Body.Armor.MaybeArmor==null||Owner.Body.Armor.MaybeArmor.Blueprint.ProficiencyGroup==ArmorProficiencyGroup.Light);
    }
    [TypeId("3c3efac64e454ad0b4b44c6af4455454")]
    public sealed class SkirnirSorcerousShield01454:UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCalculateArcaneSpellFailureChance>,IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateArcaneSpellFailureChance evt)
        {
            var cast=Rulebook.CurrentContext?.EventStack.OfType<RuleCastSpell>().LastOrDefault();
            if(cast?.Initiator==Owner&&cast.Spell?.SourceItem==null&&cast.Spell.Spellbook?.Blueprint.AssetGuid==MagusBatch01454.Identity(Skirnir01454.BookToken))evt.IgnoreShield=true;
        }
        public void OnEventDidTrigger(RuleCalculateArcaneSpellFailureChance evt){}
    }
    [TypeId("ea5b0970dd5f4ad487742886c1d25454")]
    public sealed class SkirnirCombatPenalty01454:UnitFactComponentDelegate,IUnitBuffHandler,IUnitSubscriber
    {
        public BlueprintBuffReference Penalty;
        public void HandleBuffDidAdded(Buff buff)
        {
            if(buff.Owner!=Owner||buff.Blueprint.AssetGuid!=BlueprintGuid.Parse("7b4cf64d3a49e3d45b1dbd2385f4eb6d")||!Skirnir01454.LosesShieldAC(Owner))return;
            Owner.AddBuff(Penalty.Get(),buff.Context,TimeSpan.FromSeconds(6));
        }
        public void HandleBuffDidRemoved(Buff buff){}
        protected override void OnTurnOff(){foreach(var buff in Owner.Buffs.Enumerable.Where(b=>b.Blueprint==Penalty.Get()).ToArray())buff.Remove();}
    }
    [TypeId("8e6dd85c8c7d4bdfbb80b3bbfbfe5454")]
    public sealed class SkirnirShieldACPenalty01454:UnitBuffComponentDelegate,ITargetRulebookHandler<RuleCalculateAC>,ITargetRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateAC evt)
        {
            // Like the original CotW rule, remove the actual shield descriptors
            // only for attacks to which they apply. Brilliant energy and touch
            // already exclude them in native code; never subtract them twice.
            if(evt.Target!=Owner||!Skirnir01454.LosesShieldAC(Owner)||evt.AttackType.IsTouch()||evt.BrilliantEnergy!=null)return;
            int amount=Owner.Stats.AC.Modifiers.Where(m=>m.ModDescriptor==ModifierDescriptor.Shield||m.ModDescriptor==ModifierDescriptor.ShieldEnhancement||m.ModDescriptor==ModifierDescriptor.ShieldFocus).Sum(m=>m.ModValue);
            if(amount!=0)evt.AddModifier(-amount,Fact,ModifierDescriptor.UntypedStackable);
        }
        public void OnEventDidTrigger(RuleCalculateAC evt){}
    }
    [TypeId("be4d687e45914a90abed46a64bb65454")]
    public sealed class SkirnirShieldProperty01454:UnitBuffComponentDelegate
    {
        public BlueprintArmorEnchantmentReference Enchantment;
        protected override void OnTurnOn()=>Skirnir01454.WithEnhancement(Owner,c=>{c.Refresh();return false;});
        protected override void OnTurnOff()=>Skirnir01454.WithEnhancement(Owner,c=>{c.Refresh(Buff);return false;});
    }
    public sealed class SkirnirShieldEnhancementData01454
    {
        [JsonProperty] public ItemEntityShield Shield;
        [JsonProperty] public List<ItemEnchantment> Enchantments=new List<ItemEnchantment>();
        [JsonIgnore] public string Shape;
        [JsonIgnore] public bool Busy;
    }
    [TypeId("e19e1b8821b34e7b870f95be301e5454")]
    public sealed class SkirnirShieldEnhancement01454:UnitBuffComponentDelegate<SkirnirShieldEnhancementData01454>,IUnitEquipmentHandler,IUnitActiveEquipmentSetHandler,IUnitBuffHandler,IUnitGainFactHandler,IUnitLostFactHandler,IUnitSubscriber
    {
        public BlueprintArmorEnchantmentReference[] Enhancements;
        void Clear(){foreach(var e in Data.Enchantments.ToArray())e?.Owner?.RemoveEnchantment(e);Data.Enchantments.Clear();Data.Shape=null;}
        protected override void OnTurnOn(){if(Data.Shield==null)Data.Shield=Skirnir01454.Shield(Owner);Refresh();}
        protected override void OnTurnOff(){MagusEnchantments01454.Unwatch(Data.Shield?.ArmorComponent);Clear();Data.Shield=null;}
        protected override void OnApplyPostLoadFixes(){Data.Shape=null;Refresh();}
        internal void Refresh(Buff leaving=null)
        {
            if(Data.Busy)return;if(Data.Shield==null||!ReferenceEquals(Data.Shield,Skirnir01454.Shield(Owner))){Buff.Remove();return;}Data.Busy=true;
            try{
                var armor=Data.Shield.ArmorComponent;MagusEnchantments01454.Watch(armor,Owner);
                var selected=Owner.Buffs.Enumerable.Where(b=>b!=leaving&&b.IsTurnedOn&&!b.IsSuppressed&&!b.IsDisposed).Select(b=>b.Blueprint.GetComponent<SkirnirShieldProperty01454>()).Where(p=>p!=null).ToArray();
                int used=Owner.ActivatableAbilities.Enumerable.Where(a=>a.IsOn&&a.Blueprint.Group==Skirnir01454.ShieldGroup&&(leaving==null||!ReferenceEquals(a.AppliedBuff,leaving))).Sum(a=>a.Blueprint.WeightInGroup);
                int numeric=Math.Max(0,Math.Min(5,Skirnir01454.Capacity(Owner)-used));var properties=selected.Select(p=>p.Enchantment).GroupBy(p=>p.Guid).Select(g=>g.First()).ToArray();
                var desired=(numeric>0?new BlueprintItemEnchantment[]{Enhancements[numeric-1].Get()}:Array.Empty<BlueprintItemEnchantment>()).Concat(properties.Select(p=>p.Get())).ToArray();
                string shape=numeric+"/"+string.Join("/",properties.Select(p=>p.Guid.ToString()).OrderBy(p=>p));if(shape==Data.Shape&&MagusEnchantments01454.Current(new ItemEntity[]{armor},desired,Data.Enchantments))return;Clear();
                MagusEnchantments01454.AddMissing(new ItemEntity[]{armor},desired,Data.Enchantments,Context);Data.Shape=shape;
            }finally{Data.Busy=false;}
        }
        public void HandleEquipmentSlotUpdated(ItemSlot slot,ItemEntity oldItem){if(!Data.Busy)Refresh();}
        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit){if(unit==Owner.Descriptor)Refresh();}
        public void HandleBuffDidAdded(Buff buff){if(buff.Owner==Owner)Refresh();}
        public void HandleBuffDidRemoved(Buff buff){if(buff.Owner==Owner)Refresh(buff);}
        public void HandleUnitGainFact(EntityFact fact){if(fact.Owner==Owner)Refresh();}
        public void HandleUnitLostFact(EntityFact fact){if(fact.Owner==Owner)Refresh();}
    }
    [TypeId("d63e4af700d642d3be1698033dc85454")]
    public sealed class SkirnirShieldCost01454:BlueprintComponent,IAbilityResourceCostCalculator
    {public int Calculate(AbilityData ability)=>ability.Caster.Buffs.Enumerable.Any(b=>b.IsTurnedOn&&!b.IsSuppressed&&b.Blueprint.AssetGuid==BlueprintGuid.Parse("3c2fe8e0374d28d4185355121f4c4544"))?2:1;}
    public sealed class SkirnirStorageData01454
    {
        [JsonProperty] public AbilityData Spell;
        [JsonProperty] public AbilityParams Params;
        [JsonProperty] public ItemEntityShield Shield;
        [JsonIgnore] public bool Releasing;
    }
    [TypeId("f537d2a4c8d3434f924508d848665454")]
    public sealed class SkirnirStorage01454:UnitFactComponentDelegate<SkirnirStorageData01454>,IInitiatorRulebookHandler<RuleAttackWithWeapon>,ITargetRulebookHandler<RuleAttackWithWeapon>,IUnitEquipmentHandler,IUnitActiveEquipmentSetHandler,IUnitSubscriber
    {
        public BlueprintBuffReference Release,Greater;
        internal bool Ready=>!Data.Releasing&&Data.Spell!=null&&Data.Params!=null&&Data.Shield!=null&&ReferenceEquals(Data.Shield,Skirnir01454.Shield(Owner));
        internal void Clear(){Data.Spell=null;Data.Params=null;Data.Shield=null;}
        internal void Store(AbilityData spell,AbilityParams parameters){Data.Spell=new AbilityData(spell,spell.Blueprint);Data.Params=parameters;Data.Shield=Skirnir01454.Shield(Owner);}
        internal void Discharge(UnitEntityData target)
        {
            if(!Ready||target==null||!Owner.IsEnemy(target))return;
            var spell=Data.Spell;var parameters=Data.Params;Clear();Data.Releasing=true;
            try{
                bool touch=spell.Blueprint.GetComponent<AbilityEffectStickyTouch>()!=null;var blueprint=touch?spell.Blueprint.GetComponent<AbilityEffectStickyTouch>().TouchDeliveryAbility:spell.Blueprint;
                if(blueprint==null)return;
                var delivery=new AbilityData(spell,blueprint){SaveSpellbookSlot=true,MetamagicData=PsychicStoredSpell.SnapshotMetamagic(spell,parameters),OverrideCasterLevel=parameters.CasterLevel,OverrideDC=parameters.DC,OverrideSpellLevel=parameters.SpellLevel};
                if(!delivery.CanTarget(new TargetWrapper(target)))return;var cast=new RuleCastSpell(delivery,new TargetWrapper(target)){IsCutscene=true};cast.Context.SetParams(parameters.Clone());cast.Context.ForceAlwaysHit=touch;cast.Context.IsForced=true;PsychicStoragePatch.MarkRelease(cast.Context);PsychicStoredSpell.Launch(cast);
            }finally{Data.Releasing=false;}
        }
        bool Armed(BlueprintBuffReference reference)=>Owner.Buffs.Enumerable.Any(b=>b.Blueprint==reference.Get()&&b.IsTurnedOn&&!b.IsSuppressed&&!b.IsDisposed);
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt){}
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if(!Ready||evt.AttackRoll?.IsHit!=true||evt.AttackRoll.IsFake||evt.Weapon?.Blueprint.IsMelee!=true)return;
            if(evt.Initiator==Owner&&evt.Target!=Owner&&ReferenceEquals(evt.Weapon,Data.Shield.WeaponComponent)&&Armed(Release))Discharge(evt.Target);
            else if(evt.Target==Owner&&evt.Initiator!=Owner&&Owner.IsEnemy(evt.Initiator)&&Armed(Greater)&&Skirnir01454.Has(Owner,Skirnir01454.GreaterToken)&&Owner.State.CanAct&&Owner.Commands.Swift==null&&!Owner.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Swift)&&
                (Owner.Body.Armor.MaybeArmor==null||Owner.Body.Armor.MaybeArmor.Blueprint.ProficiencyGroup==ArmorProficiencyGroup.Light)){
                FellingSmashTrigger.SpendSwift(Owner,Greater.Get());Discharge(evt.Initiator);
            }
        }
        protected override void OnTurnOff()=>Clear();
        protected override void OnApplyPostLoadFixes(){if(!Ready)Clear();}
        public void HandleEquipmentSlotUpdated(ItemSlot slot,ItemEntity oldItem){if(Data.Shield!=null&&!ReferenceEquals(Data.Shield,Skirnir01454.Shield(Owner)))Clear();}
        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit){if(unit==Owner.Descriptor&&Data.Shield!=null&&!ReferenceEquals(Data.Shield,Skirnir01454.Shield(Owner)))Clear();}
    }
    [TypeId("b21e652f901f4c62ad4683d0462e5454")]
    public sealed class SkirnirStoreSpell01454:AbilityApplyEffect,IAbilityRestriction,IAbilityRequiredParameters,IAbilityResourceCostCalculator,IAbilityVisibilityProvider
    {
        public AbilityParameter RequiredParameters=>AbilityParameter.SpellSlot;
        public bool IsAbilityVisible(AbilityData ability)=>ability?.ParamSpellSlot!=null;
        internal static IEnumerable<AbilityData> Leaves(AbilityData source)
        {
            var seen=new HashSet<BlueprintGuid>();var queue=new Queue<AbilityData>();if(source!=null)queue.Enqueue(source);
            while(queue.Count>0){var spell=queue.Dequeue();if(!seen.Add(spell.Blueprint.AssetGuid))continue;if(!spell.Blueprint.HasVariants){yield return spell;continue;}
                foreach(var variant in spell.AbilityVariants.Variants)if(variant!=null)queue.Enqueue(new AbilityData(spell,variant));}
        }
        internal AbilityData Selected(AbilityData ability)
        {
            var source=ability?.ParamSpellSlot?.SpellShell;var selected=ability?.ConvertedFrom;
            if(source==null||selected==null||!ReferenceEquals(source.Caster,selected.Caster)||!ReferenceEquals(source.Spellbook,selected.Spellbook))return null;
            if(!Leaves(source).Any(a=>ReferenceEquals(a.Blueprint,selected.Blueprint)))return null;
            return new AbilityData(source,selected.Blueprint);
        }
        internal static bool Eligible(AbilityData spell)
        {
            if(spell?.Spellbook?.Blueprint.AssetGuid!=MagusBatch01454.Identity(Skirnir01454.BookToken)||spell.SourceItem!=null||spell.SpellLevel<1||spell.SpellLevel>6||spell.Blueprint.Type!=AbilityType.Spell||spell.Blueprint.HasVariants)return false;
            var bp=spell.Blueprint;return bp.EffectOnEnemy==AbilityEffectOnUnit.Harmful&&bp.CanTargetEnemies&&!bp.CanTargetPoint&&bp.Range!=AbilityRange.Personal&&!bp.IsFullRoundAction&&
                bp.GetComponent<AbilityTargetsAround>()==null&&!bp.ComponentsArray.Any(c=>c.GetType().Name=="TargetsAroundEnemy"||c is AbilityDeliverProjectile p&&p.Type!=AbilityProjectileType.Simple)&&!SpellBatch01447.Elements(bp.ComponentsArray,new HashSet<object>()).Any(e=>e is Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnAreaEffect)&&
                (!spell.RequireMaterialComponent||spell.HasEnoughMaterialComponent);
        }
        public bool IsAbilityRestrictionPassed(AbilityData ability)
        {
            var spell=Selected(ability);var slot=ability?.ParamSpellSlot;return Skirnir01454.Has(ability?.Caster?.Unit,Skirnir01454.StorageToken)&&Skirnir01454.Shield(ability.Caster.Unit)!=null&&Eligible(spell)&&slot.Available&&
                spell.Spellbook.GetMemorizedSpellSlots(slot.SpellLevel).Any(s=>ReferenceEquals(s,slot))&&ReferenceEquals(slot.SpellShell.Spellbook,spell.Spellbook);
        }
        public string GetAbilityRestrictionUIText()=>"An available hostile targeted magus spell and a shield are required.";
        public int Calculate(AbilityData ability)=>ability?.ParamSpellSlot?.SpellLevel??0;
        public override void Apply(AbilityExecutionContext context,TargetWrapper target)
        {
            var ability=context?.Ability;if(!IsAbilityRestrictionPassed(ability))return;var spell=Selected(ability);var slot=ability.ParamSpellSlot;
            // Native SpellSlot.Spend handles linked/opposition slots. Only the
            // selected preparation is consumed; release never spends it again.
            Skirnir01454.WithStorage(context.MaybeCaster,state=>{var parameters=spell.CalculateParams().Clone();slot.Spend();spell.SpendMaterialComponent();state.Store(spell,parameters);return true;});
        }
    }
    [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.GetConversions))]
    internal static class SkirnirStoreConversions01454
    {
        static void Postfix(AbilityData __instance,ref IEnumerable<AbilityData> __result)
        {
            if(__instance?.Blueprint.Type!=AbilityType.Spell||__instance.Spellbook?.Blueprint.AssetGuid!=MagusBatch01454.Identity(Skirnir01454.BookToken)||!Skirnir01454.Has(__instance.Caster.Unit,Skirnir01454.StorageToken))return;
            var slot=__instance.SpellSlot;if(slot==null||!slot.Available)return;var extra=new List<AbilityData>();
            foreach(var fact in __instance.Caster.Abilities.Enumerable.Where(a=>a.Blueprint.GetComponent<SkirnirStoreSpell01454>()!=null))
                foreach(var selected in SkirnirStoreSpell01454.Leaves(__instance)){
                    // A no-book supernatural conversion still needs its real
                    // owned Ability fact. Without it UnitUseAbility.Init
                    // rejects the command before any effect/payment executes.
                    var converted=new AbilityData(fact){ConvertedFrom=selected};
                    // ConvertedFrom's native setter replaces m_Fact with the
                    // selected prepared spell's null fact. Restore the owned
                    // storage source after that setter, retaining all of its
                    // other native conversion/parameter semantics.
                    SkirnirStorageSource01455.Bind(converted,fact);AccessTools.Property(typeof(AbilityData),nameof(AbilityData.ParamSpellSlot)).SetValue(converted,slot,null);
                    if(fact.Blueprint.GetComponent<SkirnirStoreSpell01454>().IsAbilityRestrictionPassed(converted))extra.Add(converted);
                }
            if(extra.Count>0)__result=(__result??Enumerable.Empty<AbilityData>()).Concat(extra).ToArray();
        }
    }
    [HarmonyPatch(typeof(AbilityData),"get_Spellbook")]
    internal static class SkirnirStoreOwnBook01454
    {static bool Prefix(AbilityData __instance,ref Spellbook __result){if(__instance.Blueprint.GetComponent<SkirnirStoreSpell01454>()==null)return true;__result=null;return false;}}
    [HarmonyPatch(typeof(AbilityData),"get_Name")]
    internal static class SkirnirStoreCaption01454
    {static void Postfix(AbilityData __instance,ref string __result){var c=__instance.Blueprint.GetComponent<SkirnirStoreSpell01454>();if(c?.Selected(__instance) is AbilityData spell)__result+=" ("+spell.Name+")";}}
}
