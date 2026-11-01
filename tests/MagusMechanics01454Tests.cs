using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.RuleSystem;
using Kingmaker.Utility;
using Kingmaker.ElementsSystem;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static Feature Grant(UnitEntityData u,BlueprintFeature bp)=>(Feature)H("Fact",u,bp);
    static Feature Grant(UnitEntityData u,BlueprintsCache c,string token)=>Grant(u,A<BlueprintFeature>(c,token));
    static Buff Add(UnitEntityData u,BlueprintBuff bp)=>(Buff)H("Buff",u,bp,null);
    static AbilityData Form(UnitEntityData u,WeaponCategory category,bool pair=false)=>Loaded(active).OfType<BlueprintAbility>().Where(b=>b.GetComponent<MindbladeManifestCost01454>()!=null).Select(b=>new AbilityData(b,u.Descriptor)).Single(a=>a.Blueprint.GetComponent<MindbladeManifestCost01454>() is MindbladeManifestCost01454 m&&m.Weapon.Get().Category==category&&m.Pair==pair);
    static Buff Manifest(UnitEntityData u,AbilityData a)
    {
        var gate=a.Blueprint.GetComponent<MindbladeManifestCost01454>();Check(gate.IsCasterRestrictionPassed(u),"actual native caster checker accepts "+gate.Weapon.Get().Category+" pair="+gate.Pair);
        AbilityCastRateUtils.SpendResources(a);var ctx=new AbilityExecutionContext(a,new AbilityParams{CasterLevel=u.Progression.Classes[0].Level,DC=15},new TargetWrapper(u),new RulebookEventContext(),u);a.Blueprint.GetComponent<AbilityEffectRunAction>().Apply(ctx,new TargetWrapper(u));
        Console.WriteLine("manifest probe "+gate.Weapon.Get().Category+" cost="+a.Blueprint.GetComponent<AbilityResourceLogic>().CalculateCost(a)+" pool="+u.Resources.GetResourceAmount(N<BlueprintAbilityResource>(active,"effc3e386331f864e9e06d19dc218b37"))+" hands="+u.Body.PrimaryHand.MaybeWeapon?.Blueprint.AssetGuid+"/"+u.Body.SecondaryHand.MaybeItem?.Blueprint.AssetGuid+" locks="+u.Body.PrimaryHand.Lock.Count+"/"+u.Body.SecondaryHand.Lock.Count+" buffs="+string.Join(",",u.Buffs.Enumerable.Select(b=>b.Blueprint.name)));
        return u.Buffs.Enumerable.Single(b=>b.Blueprint.GetComponent<MindbladeManifest01454>()!=null);
    }
    static MindbladeManifestData01454 ManifestData(Buff buff)=>buff.Components.Single(c=>c.SourceBlueprintComponent is MindbladeManifest01454).GetData<MindbladeManifestData01454>();
    static ActivatableAbility Toggle(UnitEntityData u,BlueprintsCache c,string token)=>u.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint==A<BlueprintActivatableAbility>(c,token));
    static void ToggleOn(ActivatableAbility ability)
    {ability.IsOn=true;ability.TryStart();Check(ability.IsOn&&ability.AppliedBuff?.IsTurnedOn==true,"actual native activatable selection creates its buff: "+ability.Blueprint.name);}
    static void ToggleOff(ActivatableAbility ability){ability.IsOn=false;ability.TurnOffImmediately();}
    static void MindbladeMore(BlueprintsCache c,UnitEntityData u,BlueprintAbilityResource pool)
    {
        Console.WriteLine("054 Mindblade: forms, native toggles, budget, actions and psychic learning");
        var dagger=Form(u,WeaponCategory.Dagger);var pair=Form(u,WeaponCategory.Dagger,true);var great=Form(u,WeaponCategory.Greatsword);var bastard=Form(u,WeaponCategory.BastardSword);var doubled=Form(u,WeaponCategory.DoubleSword);
        foreach(var cat in new[]{WeaponCategory.Greatsword,WeaponCategory.BastardSword,WeaponCategory.DoubleSword})u.Descriptor.Proficiencies.Add(cat);
        Check(!pair.Blueprint.GetComponent<MindbladeManifestCost01454>().IsCasterRestrictionPassed(u),"pair is forbidden without Dual Weapons, even with empty hands and proficiency");
        Check(!bastard.Blueprint.GetComponent<MindbladeManifestCost01454>().RequiresTwoHands&&bastard.Blueprint.GetComponent<MindbladeManifestCost01454>().ReservedCost==2,"fully proficient bastard sword uses original one-handed cost");
        Check(dagger.ActionType==UnitCommand.CommandType.Standard&&pair.ActionType==UnitCommand.CommandType.Standard,"original standard manifestation actions before milestones");
        var rapid=Grant(u,c,"MindbladeRapidManifest01454");Check(dagger.ActionType==UnitCommand.CommandType.Swift&&pair.ActionType==UnitCommand.CommandType.Standard,"Rapid Manifest changes only single forms");
        Grant(u,N<BlueprintFeature>(c,"2464ba53317c7fc4d88f383fac2b45f9"));var spellCombat=u.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.AssetGuid==BlueprintGuid.Parse("8898a573e8a8a184b8186dbc3a26da74"));ToggleOn(spellCombat);
        int amount=u.Resources.GetResourceAmount(pool);var single=Manifest(u,great);Check(u.Body.PrimaryHand.MaybeWeapon.Blueprint.IsTwoHanded&&u.Body.SecondaryHand.Lock.Count==1&&!UnitPartMagus.HasOneHandedMeleeWeaponAndFreehand(u.Descriptor),"two-handed manifestation reserves both slots and cannot spell-combat before Dual Manifest");
        var dualManifest=Grant(u,c,"MindbladeDualManifest01454");Check(UnitPartMagus.HasOneHandedMeleeWeaponAndFreehand(u.Descriptor)&&pair.ActionType==UnitCommand.CommandType.Swift,"Dual Manifest enables only the owned two-handed form and paired swift action");ToggleOn(spellCombat);single.Remove();Check(u.Resources.GetResourceAmount(pool)==amount&&u.Body.SecondaryHand.Lock.Count==0,"two-handed reservation refunded with both own locks released");
        Grant(u,c,"MindbladeDualWeapons01454");var dual=Toggle(u,c,"MindbladeDualModeToggle01454");ToggleOn(dual);Check(!dagger.Blueprint.GetComponent<MindbladeManifestCost01454>().IsCasterRestrictionPassed(u),"dual mode rejects single manifestation");
        var paired=Manifest(u,pair);var pd=ManifestData(paired);Check(pd.Weapons.Length==2&&ReferenceEquals(u.Body.SecondaryHand.MaybeWeapon,pd.Weapons[1])&&u.Resources.GetResourceAmount(pool)==amount-2&&UnitPartMagus.HasOneHandedMeleeWeaponAndFreehand(u.Descriptor),"native paired insertion, two-point payment and owned spell-combat gate");Check(spellCombat.IsOn&&spellCombat.IsAvailableByRestrictions,"actual native Spell Combat mode survives paired insertion and its UI grip restriction accepts the complete owned form");
        ToggleOff(dual);Check(u.Body.PrimaryHand.MaybeItem==null&&u.Body.SecondaryHand.MaybeItem==null&&u.Resources.GetResourceAmount(pool)==amount,"actual native dual-mode removal dismisses both owned items and refunds once");
        ToggleOn(dual);var doubleBuff=Manifest(u,doubled);var dd=ManifestData(doubleBuff);Check(dd.Weapons.Length==1&&dd.Weapons[0].Second!=null&&!dd.Weapons[0].Second.Blueprint.Double&&dd.Weapons[0].Second.Second==null&&u.Resources.GetResourceAmount(pool)==amount-3,"native double weapon factory has two finite ends and one three-point reservation");doubleBuff.Remove();ToggleOff(dual);
        for(int i=1;i<=6;i++)Grant(u,c,"MindbladeEnhancement"+i+"01454");Grant(u,c,"MindbladeProperties01454");
        var enhanced=Manifest(u,dagger);var ed=ManifestData(enhanced);Check(ed.Enchantments.Count==1&&ed.Enchantments[0].Blueprint.AssetGuid==BlueprintGuid.Parse("746ee366e50611146821d61e391edf16"),"numeric enhancement is capped at five even with all six upgrades");
        var flame=Toggle(u,c,"MindbladeFlamingToggle01454");ToggleOn(flame);Check(ed.Enchantments.Any(e=>e.Blueprint.AssetGuid==BlueprintGuid.Parse("30f90becaaac51f41bf56641966c4121")),"actual property toggle applies native flaming enchantment");ToggleOff(flame);Check(!ed.Enchantments.Any(e=>e.Blueprint.AssetGuid==BlueprintGuid.Parse("30f90becaaac51f41bf56641966c4121")),"native toggle off removes only our property");enhanced.Remove();
        bool cheat=u.Blueprint.IsCheater;u.Blueprint.IsCheater=true;try{var free=Manifest(u,dagger);Check(ManifestData(free).Paid==0&&ManifestData(free).Reserved==0&&u.Resources.GetResourceAmount(pool)==amount,"native free spend is recorded as zero");free.Remove();Check(u.Resources.GetResourceAmount(pool)==amount,"free manifestation dismissal cannot create resource points");}finally{u.Blueprint.IsCheater=cheat;}
        var root=A<BlueprintArchetype>(c,"MindbladeArchetype01454");u.Progression.Classes[0].AddArchetype(root);var book=new Spellbook(u.Descriptor,root.ReplaceSpellbook);Set(book,"m_BaseLevelInternal",7);Set(u.Descriptor,"m_Spellbooks",new Dictionary<BlueprintSpellbook,Spellbook>{{root.ReplaceSpellbook,book}});
        var list=A<BlueprintSpellList>(c,"MindbladePsychicSpellList01454");var spell=list.SpellsByLevel[1].Spells.First();var choice=A<BlueprintParametrizedFeature>(c,"MindbladePsychicAccess014541");
        Check(choice.GetComponent<PrerequisiteClassSpellLevel>().Check(null,u.Descriptor,null),"native spell-level prerequisite accepts a real first-circle private book");
        var feat=new Feature(choice,u.Descriptor){Param=new FeatureParam(spell)};u.Facts.Add(feat);Check(book.IsKnown(spell)&&book.GetKnownSpell(spell).SpellLevel==1,"actual native LearnSpellParametrized adds the chosen psychic spell to the private Magus book");
        Check(!N<BlueprintCharacterClass>(c,"45a4607686d96a1498891b3286121780").Spellbook.SpellList.SpellsByLevel.SelectMany(r=>r.Spells).Contains(spell),"learning does not insert psychic spell into the global Magus spell list");
        Check(!A<BlueprintParametrizedFeature>(c,"MindbladePsychicAccess014546").GetComponent<PrerequisiteClassSpellLevel>().Check(null,u.Descriptor,null),"native sixth-circle prerequisite rejects a seventh-level Mindblade");
        var spellsBefore=book.GetAllKnownSpells().Count();u.Facts.Remove(feat);Check(book.IsKnown(spell)&&book.GetAllKnownSpells().Count()==spellsBefore,"native LearnSpellParametrized permanently learns in the book; removing its granting fact alone does not rewrite that native behavior");Check(!new Spellbook(u.Descriptor,root.ReplaceSpellbook).IsKnown(spell),"a newly constructed private book starts without the previous book knowledge");
        var veteran=EquipmentUnit(c,19);veteran.Progression.Classes[0].AddArchetype(root);Grant(veteran,c,"MindbladePsychicPool01454");Grant(veteran,c,"MindbladeBaneArcana01454");veteran.Descriptor.Proficiencies.Add(WeaponCategory.Dagger);int total=veteran.Resources.GetResourceAmount(pool);var bane=Toggle(veteran,c,"MindbladeBaneToggle01454");
        ToggleOn(bane);var vdagger=Form(veteran,WeaponCategory.Dagger);var vb=Manifest(veteran,vdagger);var vd=ManifestData(vb);Check(vd.Paid==2&&vd.Reserved==1&&vd.BanePaid&&vd.Enchantments.Any(e=>e.Blueprint.AssetGuid==BlueprintGuid.Parse("1a93ab9c46e48f3488178733be29342a")),"preselected Bane charges one nonrefundable point per manifested end");vb.Remove();ToggleOff(bane);Check(veteran.Resources.GetResourceAmount(pool)==total-1,"Bane extra point is not refunded on dismissal");
        vb=Manifest(veteran,vdagger);int paid=veteran.Resources.GetResourceAmount(pool);ToggleOn(bane);Check(veteran.Resources.GetResourceAmount(pool)==paid-1,"enabling Bane after manifestation uses actual native extra payment");ToggleOff(bane);ToggleOn(bane);Check(veteran.Resources.GetResourceAmount(pool)==paid-1,"Bane toggling on the same weapon does not charge twice");ToggleOff(bane);vb.Remove();
        vb=Manifest(veteran,vdagger);veteran.Resources.Spend(pool,veteran.Resources.GetResourceAmount(pool));Check(!bane.IsAvailableByRestrictions,"native toggle restriction prevents unaffordable Bane on an existing manifestation");vb.Remove();Check(veteran.Resources.GetResourceAmount(pool)==1,"only the actual reserved point returns after spending remaining pool elsewhere");
        var reclaimed=Manifest(veteran,vdagger);var lost=ManifestData(reclaimed).Weapons[0];using(ContextData<ItemSlot.IgnoreLock>.Request())veteran.Body.PrimaryHand.RemoveItem();Check(reclaimed.IsDisposed&&veteran.Resources.GetResourceAmount(pool)==1&&lost.Collection==null,"native equipment removal ends the exact manifestation, removes its inventory entity and refunds once");
        MindbladeEdges(c);
    }
    static void SeedFist(BlueprintsCache c,UnitEntityData u)
    {
        var bp=N<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>(c,"e5ea5042194712040a3b2944e6944d10");var fist=bp.CreateEntity<ItemEntityWeapon>();Set(u.Body,"m_EmptyHandWeapon",fist);Check(fist.Blueprint.IsUnarmed&&fist.CanTakeOneHand(u),"native unarmed fallback is present in constructed body input");
    }
    static UnitEntityData EquipmentUnit(BlueprintsCache c,int level,string classId="45a4607686d96a1498891b3286121780")
    {
        UnitEntityData u;active=null;try{u=(UnitEntityData)H("Unit",c,classId,level);}finally{active=c;}Actors.Add(u);u.Stats.Intelligence.BaseValue=20;u.Descriptor.m_Inventory=new ItemsCollection(u);
        Set(u.Descriptor,"Proficiencies",new UnitProficiency());Set(u.Descriptor,"Body",new UnitBody(u.Descriptor));Set(u.Body,"m_HandsAreEnabled",true);SeedFist(c,u);
        if(classId=="83fa3b50138c566698801ba5967bbde0"&&AccessTools.Field(T("PsychicClass"),"MajorId")!=null)Grant(u,N<BlueprintFeature>(c,"4896622b699957208e128c3a5361609c"));
        return u;
    }
    static void ApplySelf(UnitEntityData u,AbilityData ability)
    {AbilityCastRateUtils.SpendResources(ability);var context=new AbilityExecutionContext(ability,new AbilityParams{CasterLevel=u.Progression.Classes[0].Level,DC=15},new TargetWrapper(u),new RulebookEventContext(),u);ability.Blueprint.GetComponent<AbilityEffectRunAction>().Apply(context,new TargetWrapper(u));}
    static void SkirnirMechanics(BlueprintsCache parent,Harmony h)
    {
        Console.WriteLine("054 Skirnir: native equipment, armor property effects and private spell slots");EquipmentPatches(h);var c=Fresh(parent);Register(c,"Skirnir01454");var u=EquipmentUnit(c,19);var root=A<BlueprintArchetype>(c,"SkirnirArchetype01454");u.Progression.Classes[0].AddArchetype(root);
        var pool=N<BlueprintAbilityResource>(c,"effc3e386331f864e9e06d19dc218b37");u.Resources.Add(pool,true);var first=Grant(u,c,"SkirnirArcaneShield101454");Grant(u,c,"SkirnirSorcerousShield01454");for(int i=2;i<=5;i++)Grant(u,c,"SkirnirArcaneShield"+i+"01454");
        var ability=new AbilityData(A<BlueprintAbility>(c,"SkirnirShieldEnhancementAbility01454"),u.Descriptor);var gate=ability.Blueprint.GetComponent<SkirnirShieldRequired01454>();Check(!gate.IsCasterRestrictionPassed(u),"Arcane Shield requires an actually equipped shield");
        var bp=root.StartingItems.OfType<Kingmaker.Blueprints.Items.Shields.BlueprintItemShield>().Single();var shield=bp.CreateEntity<ItemEntityShield>();using(ContextData<ItemEntity.CanBeEquippedForce>.Request())Check(u.Body.SecondaryHand.InsertItem(shield),"native shield factory and actual secondary slot insertion");
        Check(gate.IsCasterRestrictionPassed(u)&&u.Ensure<UnitPartActivatableAbility>().GetGroupSize(ActivatableAbilityGroup.ArcaneArmorProperty)==5,"native shield gate and dedicated group capacity at level 19");int amount=u.Resources.GetResourceAmount(pool);ApplySelf(u,ability);
        var buff=u.Buffs.Enumerable.Single(b=>b.Blueprint.GetComponent<SkirnirShieldEnhancement01454>()!=null);var data=buff.Components.Single(r=>r.SourceBlueprintComponent is SkirnirShieldEnhancement01454).GetData<SkirnirShieldEnhancementData01454>();
        Check(ReferenceEquals(data.Shield,shield)&&u.Resources.GetResourceAmount(pool)==amount-1&&data.Enchantments.Count==1&&ReferenceEquals(data.Enchantments[0].Owner,shield.ArmorComponent),"native Arcane Shield pays once and enhances armor component only");Check(shield.WeaponComponent==null||!shield.WeaponComponent.Enchantments.Any(e=>data.Enchantments.Contains(e)),"shield bash component receives no armor enchantments");
        var fort=u.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.Buff.GetComponent<SkirnirShieldProperty01454>()?.Enchantment.Get().AssetGuid==Id("SkirnirFortification2501454"));ToggleOn(fort);
        Check(u.Get<UnitPartFortification>()?.Value==25&&data.Enchantments.Any(e=>e.Blueprint.AssetGuid==Id("SkirnirFortification2501454")),"native equipped armor enchantment grants actual 25-percent fortification");ToggleOff(fort);Check(u.Get<UnitPartFortification>()?.Value!=25,"native property removal removes actual fortification effect");
        var sr=u.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.Buff.GetComponent<SkirnirShieldProperty01454>()?.Enchantment.Get().AssetGuid==Id("SkirnirSpellResistance1301454"));ToggleOn(sr);Check(u.Get<UnitPartSpellResistance>()?.SRs.Any(r=>r.Value==13)==true,"actual equipped native spell resistance is 13");ToggleOff(sr);
        var foreign=shield.ArmorComponent.AddEnchantment(data.Enchantments.Single().Blueprint,new MechanicsContext(u,u.Descriptor,buff.Blueprint));Check(data.Enchantments.Count==0&&shield.ArmorComponent.Enchantments.Any(e=>ReferenceEquals(e,foreign)),"external identical armor enhancement is preserved without duplicate own effect");shield.ArmorComponent.RemoveEnchantment(foreign);Check(data.Enchantments.Count==1,"native removal restores shield enhancement");buff.Remove();Check(data.Enchantments.Count==0&&ReferenceEquals(u.Body.SecondaryHand.MaybeShield,shield)&&u.Resources.GetResourceAmount(pool)==amount-1,"shield expiration removes own effects, retains the actual shield and never refunds spent daily pool");
        Grant(u,c,"SkirnirShieldSpellCombat01454");var dagger=N<Kingmaker.Blueprints.Items.Weapons.BlueprintCategoryDefaults>(c,"567dc59213fd9664c8cb291643439714").Entries.Single(e=>e.Key==WeaponCategory.Dagger).DefaultWeapon.CreateEntity<ItemEntityWeapon>();using(ContextData<ItemEntity.CanBeEquippedForce>.Request())Check(u.Body.PrimaryHand.InsertItem(dagger),"native one-handed shield-combat equipment");Check(UnitPartMagus.HasOneHandedMeleeWeaponAndFreehand(u.Descriptor),"native spell combat gate accepts shield plus one-handed melee at milestone");var combat=Toggle(u,c,"SkirnirSpellCombatToggle01454");Check(combat.IsAvailableByRestrictions,"actual native equipment restriction accepts the Skirnir shield exception");ToggleOn(combat);
        var book=new Spellbook(u.Descriptor,root.ReplaceSpellbook);Set(book,"m_BaseLevelInternal",19);Set(u.Descriptor,"m_Spellbooks",new Dictionary<BlueprintSpellbook,Spellbook>{{root.ReplaceSpellbook,book}});Check(book.Blueprint.CastingAttribute==Kingmaker.EntitySystem.Stats.StatType.Intelligence&&book.MaxSpellLevel==6,"actual private prepared Magus book has six circles and Int");
        Grant(u,c,"SkirnirSpellshield01454");var spell=N<BlueprintAbility>(c,"4ac47ddb9fa1eaf43a1b6809980cfbd2"); // native Magic Missile
        var known=book.AddKnown(1,spell);var slot=new SpellSlot(1,SpellSlotType.Common,0,book){SpellShell=known,Available=true};var rows=(List<SpellSlot>[])AccessTools.Field(typeof(Spellbook),"m_MemorizedSpells").GetValue(book);rows[1]=new List<SpellSlot>{slot};
        var conversions=slot.SpellShell.GetConversions().Where(a=>a.Blueprint.GetComponent<SkirnirStoreSpell01454>()!=null).ToArray();Check(conversions.Length==1&&ReferenceEquals(conversions[0].ParamSpellSlot,slot)&&conversions[0].Spellbook==null,"actual native GetConversions appends exactly one eligible storage choice tied to the selected preparation");
        int before=u.Resources.GetResourceAmount(pool);AbilityCastRateUtils.SpendResources(conversions[0]);var effect=conversions[0].Blueprint.GetComponent<SkirnirStoreSpell01454>();effect.Apply(new AbilityExecutionContext(conversions[0],new AbilityParams{CasterLevel=19,DC=15},new TargetWrapper(u),new RulebookEventContext(),u),new TargetWrapper(u));
        var storage=u.Facts.List.Single(f=>f.Blueprint.GetComponent<SkirnirStorage01454>()!=null);var stored=storage.Components.Single(r=>r.SourceBlueprintComponent is SkirnirStorage01454).GetData<SkirnirStorageData01454>();Check(!slot.Available&&stored.Spell?.Blueprint==spell&&stored.Spell.Spellbook==book&&stored.Params.CasterLevel==19&&ReferenceEquals(stored.Shield,shield)&&u.Resources.GetResourceAmount(pool)==before-1,"actual native slot Spend, spell params and one-circle resource payment bind the exact shield and book");
        Check(!effect.IsAbilityRestrictionPassed(conversions[0])&&!slot.SpellShell.GetConversions().Any(a=>a.Blueprint.GetComponent<SkirnirStoreSpell01454>()!=null),"spent preparation cannot be stored again or offered in native conversion menu");
        SkirnirCombat(c,u,book,shield,dagger,slot,stored,h);
        SkirnirEdges(c,u,book,shield,slot,stored,h);
        using(ContextData<ItemSlot.IgnoreLock>.Request())u.Body.SecondaryHand.RemoveItem();Check(stored.Spell==null&&!UnitPartMagus.HasOneHandedMeleeWeaponAndFreehand(u.Descriptor),"actual equipment event clears stored spell and rejects shield combat after shield removal");
    }
}
