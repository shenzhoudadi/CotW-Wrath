using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class Psychic093Tests
{
    static int count,created,removed,removedBuffs;static bool insertAllowed=true,disabled;
    static HandSlot hand;static UnitDescriptor equipmentOwner;static bool nativeEquipCheck;
    static readonly Dictionary<ItemSlot,ItemEntity> slots=new Dictionary<ItemSlot,ItemEntity>();
    static readonly Dictionary<StatType,int> damage=new Dictionary<StatType,int>(),drain=new Dictionary<StatType,int>();
    static readonly List<RuleHealStatDamage> heals=new List<RuleHealStatDamage>();
    static readonly List<Buff> cleansed=new List<Buff>();
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);count++;}
    static T Asset<T>(BlueprintsCache cache,string token) where T:SimpleBlueprint=>(T)cache.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicForceSword).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
    static void Patch(Harmony h,MethodBase method,string prefix)=>h.Patch(method,prefix:new HarmonyMethod(AccessTools.Method(typeof(Psychic093Tests),prefix)));
    static bool Primary(ref HandSlot __result){__result=hand;return false;}
    static bool Maybe(ItemSlot __instance,ref ItemEntity __result){slots.TryGetValue(__instance,out __result);return false;}
    static bool Disabled(ref bool __result){__result=disabled;return false;}
    static bool CanInsert(ItemEntity __0,ref bool __result){__result=insertAllowed && (!nativeEquipCheck || __0.CanBeEquippedBy(equipmentOwner));return false;}
    static bool Insert(ItemSlot __instance,ItemEntity __0,ref bool __result){__result=insertAllowed;if(__result){slots[__instance]=__0;Set(__0,"<HoldingSlot>k__BackingField",__instance);}return false;}
    static bool RemoveSlot(ItemSlot __instance,ref bool __result){var item=slots[__instance];if(item!=null)Set(item,"<HoldingSlot>k__BackingField",null);slots[__instance]=null;__result=true;return false;}
    static bool Create(BlueprintItem __0,ref ItemEntity __result){var item=Empty<ItemEntityWeapon>();Set(item,"m_Blueprint",__0);Set(item,"<Collection>k__BackingField",Empty<ItemsCollection>());__result=item;created++;return false;}
    static bool NoItemServices()=>false;
    static bool RemoveCollection(ItemEntity __0,ref ItemEntity __result){removed++;Set(__0,"<Collection>k__BackingField",null);return false;}
    static bool RemoveBuff(Buff __instance){removedBuffs++;cleansed.Add(__instance);return false;}
    static bool Damage(ModifiableValueAttributeStat __instance,ref int __result){damage.TryGetValue(__instance.Type,out __result);return false;}
    static bool Drain(ModifiableValueAttributeStat __instance,ref int __result){drain.TryGetValue(__instance.Type,out __result);return false;}
    static bool Heal(RuleHealStatDamage __instance){var __0=__instance;heals.Add(__0);var values=__0.ShouldHealDrain?drain:damage;values[__0.StatType]=Math.Max(0,values[__0.StatType]-__0.Amount);return false;}
    internal static int Run(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        Console.WriteLine("Psychic 0.9.3: summoned weapon lifecycle, mental healing and shared class lists");
        var h=new Harmony("CotW.Psychic093.Boundaries");
        try {
            TestBlueprints(cache,f);
            Patch(h,AccessTools.PropertyGetter(typeof(UnitBody),"PrimaryHand"),nameof(Primary));
            Patch(h,AccessTools.PropertyGetter(typeof(ItemSlot),"MaybeItem"),nameof(Maybe));
            Patch(h,AccessTools.PropertyGetter(typeof(ItemSlot),"Disabled"),nameof(Disabled));
            Patch(h,AccessTools.Method(typeof(ItemSlot),"CanInsertItem"),nameof(CanInsert));
            Patch(h,AccessTools.Method(typeof(ItemSlot),"InsertItem"),nameof(Insert));
            Patch(h,AccessTools.Method(typeof(HandSlot),"RemoveItem",new[]{typeof(bool)}),nameof(RemoveSlot));
            Patch(h,typeof(ItemsEntityFactory).GetMethods().Single(m=>m.Name=="CreateEntity" && !m.IsGenericMethod),nameof(Create));
            Patch(h,AccessTools.Method(typeof(ItemEntity),"MakeNotLootable"),nameof(NoItemServices));
            Patch(h,AccessTools.Method(typeof(ItemEntity),"Identify"),nameof(NoItemServices));
            Patch(h,AccessTools.Method(typeof(ItemsCollection),"Remove",new[]{typeof(ItemEntity)}),nameof(RemoveCollection));
            Patch(h,AccessTools.Method(typeof(Buff),"Remove"),nameof(RemoveBuff));
            TestWeapon(cache,f);int greedBefore=count;TestGreedWeapon(cache,f);PsychicExpansionTests.Inventory["greed031EquipmentAssertions"]=count-greedBefore;
            Patch(h,AccessTools.PropertyGetter(typeof(ModifiableValueAttributeStat),"Damage"),nameof(Damage));
            Patch(h,AccessTools.PropertyGetter(typeof(ModifiableValueAttributeStat),"Drain"),nameof(Drain));
            Patch(h,AccessTools.Method(typeof(RuleHealStatDamage),"OnTrigger"),nameof(Heal));
            TestSurgery(cache,f);
        } finally {h.UnpatchAll(h.Id);}
        PsychicExpansionTests.Inventory["psychic093Assertions"]=count;
        Console.WriteLine("PASS: "+count+" 0.9.3 checks; native managed objects with equipment/healing delivery boundaries, not gameplay.");return count;
    }
    static void TestBlueprints(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var spell=Asset<BlueprintAbility>(cache,"ForceSword");
        var buff=Asset<BlueprintBuff>(cache,"ForceSwordBuff");
        Check(spell.School==SpellSchool.Evocation && spell.Range==AbilityRange.Touch && !spell.SpellResistance,"Force Sword is a friendly evocation touch spell");
        Check(spell.GetComponents<SpellListComponent>().Select(c=>c.SpellLevel).SequenceEqual(new[]{2,2,2}),"Force Sword in Psychic, Wizard and Magus second circle");
        var eyes=Asset<BlueprintAbility>(cache,"CountlessEyes");
        Check(eyes.GetComponents<SpellListComponent>().Count()==4,"Countless Eyes in Psychic, Wizard, Alchemist and Inquisitor");
        foreach(var ability in new[]{spell,eyes}) foreach(var link in ability.GetComponents<SpellListComponent>())
            Check(link.SpellList.SpellsByLevel.Single(l=>l.SpellLevel==link.SpellLevel).Spells.Count(s=>s==ability)==1,"Reciprocal list link, exactly once");
        for(int i=1;i<=3;i++) {
            var weapon=Asset<BlueprintItemWeapon>(cache,"ForceSwordWeapon"+i);
            Check(weapon.Category==Kingmaker.Enums.WeaponCategory.Longsword && weapon.Damage.Dice==DiceType.D8,"A real longsword, not a touch attack or kinetic blade");
            Check(weapon.DamageType.Type==DamageType.Force && weapon.DamageType.CreateDamage(new DiceFormula(1,DiceType.D8),i) is ForceDamage,"Native force damage constructor, not physical damage relabeled");
            Check(weapon.Cost==0 && ((BlueprintWeaponEnchantmentReference[])Get(weapon,"m_Enchantments")).Length==2,"Only native enhancement plus cosmetic force enchantment; zero resale value");
        }
        var ctx=Empty<MechanicsContext>();Set(ctx,"m_MainTarget",new TargetWrapper(f.Unit));ctx.SetParams(new AbilityParams {CasterLevel=13});Set(ctx,"m_Ranks",new int[30]);ctx[Kingmaker.Enums.AbilityRankType.Default]=13;
        var duration=spell.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single().DurationValue;
        Check(duration.Calculate(ctx).Value==130,"Force Sword lasts 13 minutes at CL13");ctx.Params.Metamagic=Metamagic.Extend;Check(duration.Calculate(ctx).Value==260,"Extend doubles Force Sword duration");
        Check(buff.FxOnRemove!=null && buff.GetComponent<PsychicForceSword>().Weapons.Length==3,"Removal metadata and all enhancement tiers exist");
        var surgery=Asset<BlueprintAbility>(cache,"PsychicSurgery");Check(surgery.GetComponents<SpellListComponent>().Single().SpellLevel==6 && surgery.MaterialComponent.Count>0,"Psychic Surgery sixth circle with original Stoneskin material cost");
        Check(typeof(PsychicForceSwordData).GetFields().All(a=>a.GetCustomAttributes(typeof(Newtonsoft.Json.JsonPropertyAttribute),false).Length==1),"Weapon and acquired slot lock both persist through native component data");
    }
    static void TestWeapon(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        Set(f.Descriptor,"<Body>k__BackingField",Empty<UnitBody>());
        var manager=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",manager);
        var bp=Asset<BlueprintBuff>(cache,"ForceSwordBuff");var c=bp.GetComponent<PsychicForceSword>();
        var fact=Empty<Buff>();Set(fact,"<Blueprint>k__BackingField",bp);Set(fact,"Components",new List<EntityFactComponent>());fact.RestoreOwnerLink(manager);
        var ctx=Empty<MechanicsContext>();ctx.SetParams(new AbilityParams());Set(fact,"m_Context",ctx);
        var runtime=c.CreateRuntimeFactComponent();runtime.Setup(c);runtime.RestoreOwnerLink(fact);
        var data=runtime.GetData<PsychicForceSwordData>();
        void Activate(){using(runtime.RequestEventContext())AccessTools.Method(typeof(PsychicForceSword),"OnActivate").Invoke(c,null);}
        void End(){using(runtime.RequestEventContext())AccessTools.Method(typeof(PsychicForceSword),"OnDeactivate").Invoke(c,null);}
        hand=Empty<HandSlot>();Set(hand,"Lock",new CountableFlag());slots[hand]=null;
        equipmentOwner=f.Descriptor;Set(equipmentOwner,"Proficiencies",new UnitProficiency());
        var untrained=Empty<ItemEntityWeapon>();Set(untrained,"m_Blueprint",Asset<BlueprintItemWeapon>(cache,"ForceSwordWeapon1"));
        Check(!untrained.CanBeEquippedBy(equipmentOwner),"Native Wrath equipment rejects the 0.9.3 sword for an unproficient player");
        nativeEquipCheck=true;Activate();
        Check(data.Weapon!=null && slots[hand]==data.Weapon,"Production creation now passes the native proficiency gate in its insertion scope");
        Check(!untrained.CanBeEquippedBy(equipmentOwner) && !equipmentOwner.Proficiencies.Contains(Kingmaker.Enums.WeaponCategory.Longsword),"Scope ends immediately and grants no persistent proficiency");
        End();nativeEquipCheck=false;
        foreach(var sample in new[]{new[]{1,1},new[]{7,1},new[]{8,2},new[]{12,2},new[]{13,3},new[]{25,3}}) {
            ctx.Params.CasterLevel=sample[0];int n=created;Activate();var item=data.Weapon;
            Check(created==n+1 && item.Blueprint==Asset<BlueprintItemWeapon>(cache,"ForceSwordWeapon"+sample[1]) && hand.Lock.Count==1,"Correct item tier and exactly one acquired slot lock at CL"+sample[0]);
            Activate();Check(created==n+1 && ReferenceEquals(data.Weapon,item) && hand.Lock.Count==1,"Repeated/restored activation does not duplicate sword or lock");
            hand.Lock.Retain();int rem=removed;End();Check(removed==rem+1 && slots[hand]==null && hand.Lock.Count==1 && data.Weapon==null && data.LockedSlot==null,"Expiry deletes only own item and preserves another lock");
            End();Check(removed==rem+1 && hand.Lock.Count==1,"Repeated cleanup is harmless");hand.Lock.Release();
        }
        var existing=Empty<ItemEntityWeapon>();slots[hand]=existing;int before=created;Activate();Check(created==before && slots[hand]==existing,"Occupied hand is rejected without replacing its weapon");slots[hand]=null;
        hand.Lock.Retain();Activate();Check(created==before && hand.Lock.Count==1,"Another effect's lock is not released to equip a sword");hand.Lock.Release();
        disabled=true;Activate();Check(created==before,"Disabled hand blocks creation");disabled=false;
        insertAllowed=false;int oldRemoved=removed;Activate();Check(created==before+1 && removed==oldRemoved+1 && data.Weapon==null && !hand.Lock.Value,"Rejected equipment insertion cleans the created item");insertAllowed=true;
        Activate();var owned=data.Weapon;slots[hand]=existing;End();Check(slots[hand]==existing && data.Weapon==null,"Forced replacement survives old sword cleanup");slots[hand]=null;
        Check(removedBuffs>=4,"Blocked creation removes the unusable buff");
    }
    static void TestGreedWeapon(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        Set(f.Descriptor,"<Body>k__BackingField",Empty<UnitBody>());
        var manager=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",manager);
        var bp=Asset<BlueprintBuff>(cache,"EmblemOfGreedBuff");var c=bp.GetComponent<PsychicGreedWeapon>();
        var fact=Empty<Buff>();Set(fact,"<Blueprint>k__BackingField",bp);Set(fact,"Components",new List<EntityFactComponent>());fact.RestoreOwnerLink(manager);
        var ctx=Empty<MechanicsContext>();ctx.SetParams(new AbilityParams());Set(fact,"m_Context",ctx);
        var runtime=c.CreateRuntimeFactComponent();runtime.Setup(c);runtime.RestoreOwnerLink(fact);
        var data=runtime.GetData<PsychicGreedWeaponData>();
        void Activate(){using(runtime.RequestEventContext())AccessTools.Method(typeof(PsychicGreedWeapon),"OnActivate").Invoke(c,null);}
        void End(){using(runtime.RequestEventContext())AccessTools.Method(typeof(PsychicGreedWeapon),"OnDeactivate").Invoke(c,null);}
        hand=Empty<HandSlot>();Set(hand,"Lock",new CountableFlag());slots[hand]=null;
        equipmentOwner=f.Descriptor;Set(equipmentOwner,"Proficiencies",new UnitProficiency());
        var untrained=Empty<ItemEntityWeapon>();Set(untrained,"m_Blueprint",Asset<BlueprintItemWeapon>(cache,"EmblemOfGreedItem0"));
        Check(!untrained.CanBeEquippedBy(equipmentOwner),"Native Wrath equipment rejects the 0.9.3 sword for an unproficient player");
        nativeEquipCheck=true;Activate();
        Check(data.Weapon!=null && slots[hand]==data.Weapon,"Production creation now passes the native proficiency gate in its insertion scope");
        Check(!untrained.CanBeEquippedBy(equipmentOwner) && !equipmentOwner.Proficiencies.Contains(Kingmaker.Enums.WeaponCategory.Glaive),"Scope ends immediately and grants no persistent proficiency");
        End();nativeEquipCheck=false;
        foreach(var sample in new[]{new[]{11,0},new[]{14,0},new[]{15,1},new[]{16,1},new[]{17,2},new[]{18,2},new[]{19,3},new[]{25,3}}) {
            ctx.Params.CasterLevel=sample[0];int n=created;Activate();var item=data.Weapon;
            Check(created==n+1 && item.Blueprint==Asset<BlueprintItemWeapon>(cache,"EmblemOfGreedItem"+sample[1]) && hand.Lock.Count==1,"Correct item tier and exactly one acquired slot lock at CL"+sample[0]);
            Activate();Check(created==n+1 && ReferenceEquals(data.Weapon,item) && hand.Lock.Count==1,"Repeated/restored activation does not duplicate sword or lock");
            hand.Lock.Retain();int rem=removed;End();Check(removed==rem+1 && slots[hand]==null && hand.Lock.Count==1 && data.Weapon==null && data.LockedSlot==null,"Expiry deletes only own item and preserves another lock");
            End();Check(removed==rem+1 && hand.Lock.Count==1,"Repeated cleanup is harmless");hand.Lock.Release();
        }
        var existing=Empty<ItemEntityWeapon>();slots[hand]=existing;int before=created;Activate();Check(created==before && slots[hand]==existing,"Occupied hand is rejected without replacing its weapon");slots[hand]=null;
        hand.Lock.Retain();Activate();Check(created==before && hand.Lock.Count==1,"Another effect's lock is not released to equip a sword");hand.Lock.Release();
        disabled=true;Activate();Check(created==before,"Disabled hand blocks creation");disabled=false;
        insertAllowed=false;int oldRemoved=removed;Activate();Check(created==before+1 && removed==oldRemoved+1 && data.Weapon==null && !hand.Lock.Value,"Rejected equipment insertion cleans the created item");insertAllowed=true;
        Activate();var owned=data.Weapon;slots[hand]=existing;End();Check(slots[hand]==existing && data.Weapon==null,"Forced replacement survives old sword cleanup");slots[hand]=null;
        Check(removedBuffs>=4,"Blocked creation removes the unusable buff");
    }
    static void TestSurgery(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var stats=f.Descriptor.Stats;var all=new List<ModifiableValue>();
        foreach(var stat in new[]{StatType.Strength,StatType.Dexterity,StatType.Constitution,StatType.Intelligence,StatType.Wisdom,StatType.Charisma}) {
            var value=new ModifiableValueAttributeStat(stats,stat);Set(value,"Type",stat);Set(stats,stat.ToString(),value);all.Add(value);damage[stat]=5;drain[stat]=3;
        }
        Set(stats,"<AllStats>k__BackingField",all);
        var spell=Asset<BlueprintAbility>(cache,"PsychicSurgery");var actions=spell.GetComponent<AbilityEffectRunAction>().Actions.Actions;
        var ctx=Empty<MechanicsContext>();Set(ctx,"m_MainTarget",new TargetWrapper(f.Unit));ctx.SetParams(new AbilityParams {SpellLevel=6});
        foreach(var action in actions.OfType<ContextActionHealStatDamage>()) using(ctx.GetDataScope(f.Unit))action.RunAction();
        Check(heals.Count==6 && heals.Count(x=>x.ShouldHealDrain)==3,"Native heal actions dispatch both damage and drain healing for three mental attributes");
        foreach(var stat in damage.Keys.ToArray()) {
            bool mental=stat==StatType.Intelligence||stat==StatType.Wisdom||stat==StatType.Charisma;
            Check(damage[stat]==(mental?0:5) && drain[stat]==(mental?0:3),"Physical damage/drain left alone: "+stat);
        }
        var check=AccessTools.Method(typeof(PsychicSurgeryCleanse),"CanRemove");
        Buff Case(SpellDescriptor descriptor,SpellSchool school,int level,bool harmful,bool fromSpell,bool notDispel) {
            var bp=new BlueprintBuff {ComponentsArray=Array.Empty<BlueprintComponent>()};Set(bp,"m_Flags",Enum.ToObject(AccessTools.Field(typeof(BlueprintBuff),"m_Flags").FieldType,harmful?64:0));bp.OnEnable();
            var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);var context=Empty<MechanicsContext>();Set(context,"<SpellDescriptor>k__BackingField",descriptor);Set(context,"<SpellSchool>k__BackingField",school);context.SetParams(new AbilityParams {SpellLevel=level});Set(b,"m_Context",context);b.IsFromSpell=fromSpell;b.IsNotDispelable=notDispel;return b;
        }
        foreach(var descriptor in new[]{SpellDescriptor.MindAffecting,SpellDescriptor.None}) foreach(int level in new[]{0,1,6,7,9}) foreach(bool harmful in new[]{false,true}) foreach(bool fromSpell in new[]{false,true}) foreach(bool fixedEffect in new[]{false,true}) {
            var b=Case(descriptor,descriptor==SpellDescriptor.None?SpellSchool.Enchantment:SpellSchool.Illusion,level,harmful,fromSpell,fixedEffect);
            bool expected=harmful&&((fromSpell&&!fixedEffect)||(level>=1&&level<=6));
            Check((bool)check.Invoke(null,new object[]{b})==expected,"Mental affliction dispel/level/beneficial matrix");
        }
        var physical=Case(SpellDescriptor.Poison,SpellSchool.Transmutation,2,true,true,false);Check(!(bool)check.Invoke(null,new object[]{physical}),"Unrelated poison not removed");
        var fear=Case(SpellDescriptor.Fear,SpellSchool.Necromancy,9,true,false,true);Check((bool)check.Invoke(null,new object[]{fear}),"Fear removed even above sixth level");
        var confusion=Case(SpellDescriptor.None,SpellSchool.None,0,false,false,true);confusion.Blueprint.ComponentsArray=new BlueprintComponent[]{new AddCondition {Condition=UnitCondition.Confusion}};confusion.Blueprint.OnEnable();Check((bool)check.Invoke(null,new object[]{confusion}),"Untagged native confusion condition removed");
        Check(!(bool)check.Invoke(null,new object[]{null}),"Null buff ignored");
        var manager=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",manager);
        var buffs=new BuffCollection();buffs.Initialize(manager);Set(f.Unit,"<Buffs>k__BackingField",buffs);
        var beneficial=Case(SpellDescriptor.MindAffecting,SpellSchool.Enchantment,6,false,true,false);
        var beneficialFear=Case(SpellDescriptor.Fear|SpellDescriptor.MindAffecting,SpellSchool.Abjuration,1,false,true,false);
        beneficial.Blueprint.ComponentsArray=new BlueprintComponent[]{new AddStatBonus {Stat=StatType.Strength,Value=-2}};beneficial.Blueprint.OnEnable();
        Check(!(bool)check.Invoke(null,new object[]{beneficial}),"A mixed-benefit mental buff is not marked harmful merely for a negative stat modifier");
        var nativeCases=new List<Buff>();
        foreach(var sample in new[]{new[]{"886c7407dc629dc499b9f1465ff382df","","4"},new[]{"8b3b4c225fe0fb046bfa8881c3ddad0d","444eed6e26f773a40ab6e4d160c67faa","5"},new[]{"2cfcce5b62d3e6d4082ec31b58468cc8","41e8a952da7a5c247b3ec1c2dbb73018","5"},new[]{"11cb2fe4fe9c44b448cfe1788ae1ab59","c7104f7526c4c524f91474614054547e","2"},new[]{"36897b36a4bd63146b1d8509a17fc5ad","4baf4109145de4345861fe0f2209d903","4"},new[]{"c5c86809a1c834e42a2eb33133e90a28","55a037e514c0ee14a8e3ed14b47061de","1"}}) {
            var bp=PsychicExpansionTests.NativeCase<BlueprintBuff>(sample[0]);
            var source=sample[1]==""?null:PsychicExpansionTests.NativeCase<BlueprintAbility>(sample[1]);
            var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);b.IsFromSpell=true;
            var parent=Empty<MechanicsContext>();Set(parent,"AssociatedBlueprint",source);parent.SetParams(new AbilityParams {SpellLevel=int.Parse(sample[2])});
            var context=Empty<MechanicsContext>();Set(context,"AssociatedBlueprint",bp);Set(context,"<ParentContext>k__BackingField",parent);Set(b,"m_Context",context);
            Check(!bp.Harmful,bp.name+" actual blueprint omits the harmful flag used by the old filter");
            Check((bool)check.Invoke(null,new object[]{b})==(nativeCases.Count<5),bp.name+" real component/source metadata is classified correctly");
            nativeCases.Add(b);
        }
        // Confusion uses BuffStatusCondition and remains recognizable with no source context.
        Set(nativeCases[0],"m_Context",null);Check((bool)check.Invoke(null,new object[]{nativeCases[0]}),"Native confusion condition works without a source context");
        foreach(var b in new[]{fear,confusion,physical,beneficial,beneficialFear}.Concat(nativeCases)) {Set(b,"Components",new List<EntityFactComponent>());b.RestoreOwnerLink(manager);manager.List.Add(b);}
        cleansed.Clear();
        using(ctx.GetDataScope(f.Unit)) actions.OfType<PsychicSurgeryCleanse>().Single().RunAction();
        Check(cleansed.Count==7 && cleansed.Contains(fear) && cleansed.Contains(confusion) && nativeCases.Take(5).All(cleansed.Contains) && !cleansed.Contains(nativeCases[5]),"Production cleanse removes native afflictions and preserves Remove Fear and beneficial buffs");
    }
}

