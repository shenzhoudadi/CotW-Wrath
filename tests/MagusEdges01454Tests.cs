using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static bool countIdentityHashes;static int identityHashes;
    static void CountIdentityHash(string token){if(countIdentityHashes&&token=="054HotIdentityNotOwnedProbe")identityHashes++;}
    static void IdentityHotPath(BlueprintsCache c)
    {
        var unit=EquipmentUnit(c,19);for(int i=0;i<32;i++){var feature=new BlueprintFeature{AssetGuid=Id("054HotFactInput"+i),name="054HotFactInput"+i,ComponentsArray=Array.Empty<BlueprintComponent>()};feature.OnEnable();Grant(unit,feature);}
        var instrumentation=new Harmony("CotW.054.IdentityCostCounter");instrumentation.Patch(AccessTools.Method(T("PsychicAssets"),"Id"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(CountIdentityHash)));identityHashes=0;countIdentityHashes=true;
        try{bool a=(bool)Call(T("Mindblade01454"),"Has",unit,"054HotIdentityNotOwnedProbe"),b=(bool)Call(T("Mindblade01454"),"Has",unit,"054HotIdentityNotOwnedProbe");Check(!a&&!b,"native fact-list qualification keeps both missing-feature results with many actual facts");Check(identityHashes==1,"fixed identity is hashed once across repeated fact-list qualifications, never once per fact");}
        finally{countIdentityHashes=false;instrumentation.UnpatchAll(instrumentation.Id);}
    }
    static void Param(AbilityData ability,string name,object value)=>AccessTools.Property(typeof(AbilityData),name).SetValue(ability,value,null);
    static void PostLoad(Kingmaker.EntitySystem.EntityFact fact,Type component)
    {var runtime=fact.Components.Single(r=>r.SourceBlueprintComponent.GetType()==component);using(runtime.RequestEventContext())AccessTools.Method(component,"OnApplyPostLoadFixes").Invoke(runtime.SourceBlueprintComponent,null);}
    static void Serialization(BlueprintsCache c)
    {
        foreach(var group in Loaded(c).OfType<BlueprintScriptableObject>().SelectMany(b=>b.ComponentsArray).Where(x=>x.GetType().Assembly==Main&&(x.GetType().Name.EndsWith("01454")||x.GetType().Name.EndsWith("01455"))).GroupBy(x=>x.GetType())){
            var component=group.First();var owner=component.OwnerBlueprint;var nodes=owner.ElementsArray.ToArray();var previous=Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead;
            try{Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=new BlueprintJsonWrapper(owner);var sw=new StringWriter();var writer=new JsonTextWriter(sw);Kingmaker.Blueprints.JsonSystem.Json.Serializer.Serialize(writer,component,typeof(BlueprintComponent));writer.Flush();var copy=Kingmaker.Blueprints.JsonSystem.Json.Serializer.Deserialize(new JsonTextReader(new StringReader(sw.ToString())),typeof(BlueprintComponent));Check(copy.GetType()==group.Key,"actual native polymorphic component roundtrip: "+group.Key.Name);}
            finally{owner.ElementsArray.Clear();owner.ElementsArray.AddRange(nodes);Set(owner,"m_AllElements",owner.ElementsArray);Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=previous;}
        }
    }
    static void MindbladeEdges(BlueprintsCache c)
    {
        IdentityHotPath(c);
        Console.WriteLine("054 Mindblade: native parametrized menu, level gates, saved-state repair and resource override");
        var root=A<BlueprintArchetype>(c,"MindbladeArchetype01454");var pool=N<BlueprintAbilityResource>(c,"effc3e386331f864e9e06d19dc218b37");
        var u=EquipmentUnit(c,19);u.Progression.Classes[0].AddArchetype(root);Grant(u,c,"MindbladePsychicPool01454");u.Descriptor.Proficiencies.Add(WeaponCategory.Dagger);
        var book=new Spellbook(u.Descriptor,root.ReplaceSpellbook);Set(book,"m_BaseLevelInternal",19);Set(u.Descriptor,"m_Spellbooks",new Dictionary<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook,Spellbook>{{root.ReplaceSpellbook,book}});
        var choice=A<BlueprintParametrizedFeature>(c,"MindbladePsychicAccess014541");var items=choice.ExtractSelectionItems(u.Descriptor,u.Descriptor).ToArray();Check(items.Length>0,"native parametrized selection produces actual psychic spell choices");
        var ordinary=EquipmentUnit(c,19);Check(!A<BlueprintFeature>(c,"MindbladeBaneArcana01454").GetComponent<PrerequisiteArchetypeLevel>().Check(null,ordinary.Descriptor,null)&&A<BlueprintFeature>(c,"MindbladeBaneArcana01454").GetComponent<PrerequisiteArchetypeLevel>().Check(null,u.Descriptor,null),"actual archetype prerequisite distinguishes ordinary Magus from qualified Mindblade");
        foreach(int level in new[]{11,12}){var x=EquipmentUnit(c,level);x.Progression.Classes[0].AddArchetype(root);Grant(x,c,"MindbladeDualWeapons01454");var dual=x.ActivatableAbilities.Enumerable.Where(a=>a.Blueprint.Buff.GetComponent<MindbladeDualMode01454>()!=null).ToArray();Check(dual.Length==1&&dual[0].Blueprint.WeightInGroup==(level<12?1:2),"native AddFeatureOnClassLevel grants only correct dual-mode weight at "+level);}
        var dagger=Form(u,WeaponCategory.Dagger);int before=u.Resources.GetResourceAmount(pool);var manifest=Manifest(u,dagger);var data=ManifestData(manifest);var exact=data.Weapons[0];var ench=data.Enchantments.ToArray();int locks=u.Body.PrimaryHand.Lock.Count;
        PostLoad(manifest,typeof(MindbladeManifest01454));Check(!manifest.IsDisposed&&ReferenceEquals(u.Body.PrimaryHand.MaybeWeapon,exact)&&locks==u.Body.PrimaryHand.Lock.Count&&ench.Select(e=>e.Blueprint.AssetGuid).SequenceEqual(data.Enchantments.Select(e=>e.Blueprint.AssetGuid))&&data.Enchantments.All(e=>ReferenceEquals(e.Owner,exact)&&exact.Enchantments.Count(x=>x.Blueprint==e.Blueprint)==1)&&u.Resources.GetResourceAmount(pool)==before-1,"actual component post-load hook preserves owned entities, locks and payment and rebuilds exactly one copy of each owned effect");manifest.Remove();
        var other=N<BlueprintAbility>(c,"e5dcf71e02e08fc448d9745653845df1").GetComponent<AbilityResourceLogic>().RequiredResource;u.Resources.Add(other,true);int otherBefore=u.Resources.GetResourceAmount(other);Check(otherBefore>0,"native bonded resource is available as override control");Set(dagger,"OverrideRequiredResource",other);manifest=Manifest(u,dagger);Check(ManifestData(manifest).PaidResource==other&&u.Resources.GetResourceAmount(pool)==before&&u.Resources.GetResourceAmount(other)==otherBefore-1,"native resource override records the actual paying pool");manifest.Remove();Check(u.Resources.GetResourceAmount(other)==otherBefore&&u.Resources.GetResourceAmount(pool)==before,"override dismissal refunds only actual paying resource");Set(dagger,"OverrideRequiredResource",null);
        for(int i=1;i<=6;i++)Grant(u,c,"MindbladeEnhancement"+i+"01454");Grant(u,c,"MindbladeDualWeapons01454");Grant(u,c,"MindbladeBaneArcana01454");var dualMode=u.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.Buff.GetComponent<MindbladeDualMode01454>()!=null);ToggleOn(dualMode);ToggleOn(Toggle(u,c,"MindbladeBaneToggle01454"));manifest=Manifest(u,Form(u,WeaponCategory.Dagger,true));Check(ManifestData(manifest).Paid==4&&ManifestData(manifest).Reserved==2&&ManifestData(manifest).Enchantments.Count(e=>e.Blueprint.AssetGuid==BlueprintGuid.Parse("1a93ab9c46e48f3488178733be29342a"))==2,"paired Bane pays one additional nonrefundable point and enchants each actual end");manifest.Remove();Check(u.Resources.GetResourceAmount(pool)==before-2,"paired dismissal refunds two reserved points, not the Bane surcharge");ToggleOff(dualMode);
        Serialization(c);
    }
    static void SkirnirEdges(BlueprintsCache c,UnitEntityData u,Spellbook book,ItemEntityShield shield,SpellSlot slot,SkirnirStorageData01454 state,Harmony h)
    {
        Console.WriteLine("054 Skirnir: conversion negatives, native bonded restoration and shield Spellstrike controller");
        var effect=A<BlueprintAbility>(c,"SkirnirStore001454").GetComponent<SkirnirStoreSpell01454>();var raw=new AbilityData(effect.OwnerBlueprint as BlueprintAbility,u.Descriptor);Check(!effect.IsAbilityVisible(raw)&&!effect.IsAbilityRestrictionPassed(raw),"unparameterized actionbar storage is hidden and unavailable");
        slot.Available=true;var conversion=slot.SpellShell.GetConversions().Single(a=>a.Blueprint.GetComponent<SkirnirStoreSpell01454>()!=null);Check(effect.IsAbilityVisible(conversion)&&effect.IsAbilityRestrictionPassed(conversion),"actual slot conversion remains visible and legal");
        var selected=conversion.ConvertedFrom;conversion.ConvertedFrom=new AbilityData(N<BlueprintAbility>(c,"ab395d2335d3f384e99dddee8562978f"),u.Descriptor);Check(!effect.IsAbilityRestrictionPassed(conversion),"forged different spell cannot borrow a legal preparation");conversion.ConvertedFrom=selected;
        var bogus=new SpellSlot(1,SpellSlotType.Common,88,book){SpellShell=slot.SpellShell,Available=true};Param(conversion,nameof(AbilityData.ParamSpellSlot),bogus);Check(!effect.IsAbilityRestrictionPassed(conversion),"foreign slot with identical spell data is rejected by native book membership");Param(conversion,nameof(AbilityData.ParamSpellSlot),slot);
        foreach(var id in new[]{"ab395d2335d3f384e99dddee8562978f","2d81362af43aeac4387a3d4fced489c3"}){var a=new AbilityData(N<BlueprintAbility>(c,id),u.Descriptor);Set(a,"m_CachedSpellbook",book);if(a.Blueprint.CanTargetPoint)Check(!(bool)Call(typeof(SkirnirStoreSpell01454),"Eligible",a),"native point-area spell is not eligible for shield storage");}
        var storage=u.Facts.List.Single(f=>f.Blueprint.GetComponent<SkirnirStorage01454>()!=null);PostLoad(storage,typeof(SkirnirStorage01454));Check(state.Spell!=null&&ReferenceEquals(state.Shield,shield),"actual storage post-load hook preserves valid exact shield linkage");
        var bond=new AbilityData(A<BlueprintAbility>(c,"SkirnirBondedItemAbility01454"),u.Descriptor);var restore=bond.Blueprint.GetComponent<AbilityRestoreSpellSlot>();Check(restore.CanCreateRestoreSpell(book)&&!restore.CanCreateRestoreSpell(new Spellbook(u.Descriptor,N<BlueprintCharacterClass>(c,"45a4607686d96a1498891b3286121780").Spellbook)),"native bonded item offers only private Skirnir book");
        slot.Available=false;Param(bond,nameof(AbilityData.ParamSpellbook),book);Param(bond,nameof(AbilityData.ParamSpellLevel),(int?)1);Param(bond,nameof(AbilityData.ParamSpellSlot),slot);Check(restore.IsAbilityRestrictionPassed(bond),"actual bonded restoration accepts the expended selected preparation");restore.Apply(new AbilityExecutionContext(bond,new AbilityParams{CasterLevel=19},new TargetWrapper(u),new RulebookEventContext(),u),new TargetWrapper(u));Check(slot.Available&&!restore.IsAbilityRestrictionPassed(bond),"native bonded restoration restores selected slot and no longer offers full slot");
        Grant(u,c,"SkirnirShieldedSpellstrike01454");ToggleOn(Toggle(u,c,"SkirnirSpellstrikeToggle01454"));var touch=N<BlueprintAbility>(c,"ab395d2335d3f384e99dddee8562978f");var original=(Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell)H("Cast",u,u,book,touch,19);var part=u.Ensure<UnitPartTouch>();part.Init(touch.GetComponent<AbilityEffectStickyTouch>().TouchDeliveryAbility,original.Spell,original.Context);
        var target=EquipmentUnit(c,19);var roll=new RuleAttackRoll(u,target,shield.WeaponComponent,0);Set(roll,"Result",AttackResult.Hit);var attack=new RuleAttackWithWeapon(u,target,shield.WeaponComponent,0);Set(attack,"AttackRoll",roll);roll.RuleAttackWithWeapon=attack;
        storedCasts=0;h.Patch(AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell),"OnTrigger"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(StoredTerminal)));
        try{new Kingmaker.Controllers.MagusController().OnEventDidTrigger(attack);Check(storedCasts==1&&lastStored.Spell.StickyTouch==original.Spell&&lastStored.SpellTarget.Unit==target,"actual native MagusController accepts shield bash and dispatches original held-touch spell");Set(roll,"Result",AttackResult.Miss);new Kingmaker.Controllers.MagusController().OnEventDidTrigger(attack);Check(storedCasts==1,"actual native MagusController rejects missed shield bash");}
        finally{h.Unpatch(AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell),"OnTrigger"),AccessTools.Method(typeof(MagusBatch01454Tests),nameof(StoredTerminal)));Set(part,"Ability",null);}
        var pool=N<BlueprintAbilityResource>(c,"effc3e386331f864e9e06d19dc218b37");var enduring=Add(u,N<BlueprintBuff>(c,"3c2fe8e0374d28d4185355121f4c4544"));var enhancement=new AbilityData(A<BlueprintAbility>(c,"SkirnirShieldEnhancementAbility01454"),u.Descriptor);int amount=u.Resources.GetResourceAmount(pool);ApplySelf(u,enhancement);var lasting=u.Buffs.Enumerable.Single(b=>b.Blueprint.GetComponent<SkirnirShieldEnhancement01454>()!=null);Check(u.Resources.GetResourceAmount(pool)==amount-2&&lasting.TimeLeft==TimeSpan.FromMinutes(19),"actual native Enduring Blade condition selects two-point, one-minute-per-Magus-level shield enchantment");PostLoad(lasting,typeof(SkirnirShieldEnhancement01454));Check(!lasting.IsDisposed&&ReferenceEquals(u.Body.SecondaryHand.MaybeShield,shield),"actual shield enhancement post-load rebinds only the equipped shield");lasting.Remove();enduring.Remove();
        var bucklerUnit=EquipmentUnit(c,8);Grant(bucklerUnit,c,"SkirnirShieldSpellCombat01454");N<Kingmaker.Blueprints.Items.Armors.BlueprintItemArmor>(c,"cc094148611ee2040952b007940d7d18");var buckler=N<Kingmaker.Blueprints.Items.Shields.BlueprintItemShield>(c,"78aef04821150bd479314bc974ea73e2").CreateEntity<ItemEntityShield>();using(ContextData<ItemEntity.CanBeEquippedForce>.Request())Check(bucklerUnit.Body.SecondaryHand.InsertItem(buckler),"native buckler control equips normally");Add(bucklerUnit,N<BlueprintBuff>(c,"7b4cf64d3a49e3d45b1dbd2385f4eb6d"));Check(!bucklerUnit.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==Id("SkirnirCombatPenalty01454")),"actual combat penalty event retains buckler AC at level eight");
        Serialization(c);
    }
}
