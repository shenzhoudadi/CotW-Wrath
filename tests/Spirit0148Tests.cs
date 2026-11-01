using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UI.UnitSettings;
using CallOfTheWild.Wrath.Prototype;

internal static class Spirit0148Tests {
 static int n;static Type forms=typeof(PsychicSpiritBind).Assembly.GetType("CallOfTheWild.Wrath.Prototype.SpiritWeaponForms");
 static object Get(object o,string f)=>AccessTools.Field(o.GetType(),f).GetValue(o);
 static void Set(object o,string f,object v)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")??throw new Exception("Missing fixture field "+f)).SetValue(o,v);
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool b,string m){if(!b)throw new Exception("0148: "+m);n++;}
 static object Call(string name,params object[] args)=>AccessTools.Method(forms,name).Invoke(null,args);
 static T Asset<T>(BlueprintsCache c,string token)where T:SimpleBlueprint=>Psychic034Tests.Asset<T>(c,token);
 static BlueprintFeature Native(BlueprintsCache c,string name)=>(BlueprintFeature)c.Load(BlueprintGuid.Parse((string)PsychicExpansionTests.NativeRows.Single(r=>(string)r["name"]==name)["blueprint"]["AssetId"]));
 static void Feature(UnitEntityData u,BlueprintFeature bp){var f=Empty<Feature>();Set(f,"Components",new List<EntityFactComponent>());f.RestoreOwnerLink(u.Facts);Set(f,"Blueprint",bp);Set(f,"IsActive",true);Set(f,"IsTurnedOn",true);u.Facts.List.Add(f);AccessTools.Method(Get(u.Facts,"m_Cache").GetType(),"Clear").Invoke(Get(u.Facts,"m_Cache"),null);}
 internal static int Run(BlueprintsCache cache){
  n=0;var map=(IDictionary)GetStatic(forms,"NativeItems");var inventory=new JArray();
  var nativeObjects=new Dictionary<object,string>();
  foreach(DictionaryEntry pair in map){var native=(BlueprintItemWeapon)cache.Load(BlueprintGuid.Parse((string)pair.Value));Check(native!=null,"actual native standard weapon exists "+pair.Key);nativeObjects[native]=Snapshot(native);nativeObjects[native.Type]=Snapshot(native.Type);}
  // Observe actual native OnEnableWithLibrary on every new private item. Its
  // visual Prototype write must remain inside the private object graph.
  for(int kind=0;kind<4;kind++)foreach(WeaponCategory category in map.Keys){
   if(kind>=2&&category!=(kind==3?WeaponCategory.Dagger:WeaponCategory.Longsword))continue;
   string token=(string)Call("ItemToken",kind,category);var item=Asset<BlueprintItemWeapon>(cache,token);var native=(BlueprintItemWeapon)cache.Load(BlueprintGuid.Parse((string)map[category]));
   Check(item!=null&&item!=native&&item.Type!=native.Type,"item and weapon type are private "+token);
   Check(item.AssetGuid!=native.AssetGuid&&item.Type.AssetGuid!=native.Type.AssetGuid,"private stable GUIDs "+token);
   Check(item.Category==category&&item.CriticalRollEdge==native.CriticalRollEdge&&item.CriticalModifier==native.CriticalModifier,"native favored category/critical range/multiplier "+token);
   Check(item.DamageType.Type==DamageType.Force&&item.Type.DamageType.Type==DamageType.Force,"force exists only in private item/type "+token);
   Check(!ReferenceEquals(item.DamageType,native.DamageType)&&!ReferenceEquals(item.Type.DamageType,native.Type.DamageType)&&!ReferenceEquals(item.DamageType,item.Type.DamageType),"no shared damage graph "+token);
   Check(!ReferenceEquals(Get(item,"m_Type"),Get(native,"m_Type")),"independent type reference "+token);
   var v=Get(item,"m_VisualParameters");var nv=Get(native,"m_VisualParameters");Check(v==null||!ReferenceEquals(v,nv),"independent item visual graph "+token);
   var tv=Get(item.Type,"m_VisualParameters");var ntv=Get(native.Type,"m_VisualParameters");Check(tv==null||!ReferenceEquals(tv,ntv),"independent type visual graph "+token);
   item.OnEnableWithLibrary();Check(Get(item,"m_VisualParameters")!=null&&!ReferenceEquals(Get(item,"m_VisualParameters"),Get(native,"m_VisualParameters")),"native enabling stays private "+token);
   var descriptor=item.DamageType.GetDamageDescriptor(item.Damage);descriptor.TypeDescription.Type=DamageType.Physical;Check(item.DamageType.Type==DamageType.Force,"native rule descriptor copy isolates mutations "+token);
   var old=item.DamageType.Physical.Form;item.DamageType.Physical.Form=PhysicalDamageForm.Bludgeoning;Check(Snapshot(native)==nativeObjects[native]&&Snapshot(native.Type)==nativeObjects[native.Type],"spell damage edits cannot reach native weapon "+token);item.DamageType.Physical.Form=old;
   Check((int)Call("Kind",item)==kind,"attack log identifies every private weapon form "+token);
   if(native.SecondWeapon!=null){Check(item.SecondWeapon!=native.SecondWeapon&&item.SecondWeapon.Type!=native.SecondWeapon.Type&&item.SecondWeapon.Type!=item.Type,"second end and type are independently private "+token);Check(item.SecondWeapon.DamageType.Type==DamageType.Force&&item.SecondWeapon.CriticalRollEdge==native.SecondWeapon.CriticalRollEdge&&item.SecondWeapon.CriticalModifier==native.SecondWeapon.CriticalModifier&&!item.SecondWeapon.Double&&item.SecondWeapon.SecondWeapon==null,"second end keeps native criticals without recursive equipment graph "+token);}
   inventory.Add(new JObject{["token"]=token,["guid"]=item.AssetGuid.ToString(),["typeGuid"]=item.Type.AssetGuid.ToString(),["category"]=category.ToString(),["criticalEdge"]=item.CriticalRollEdge,["criticalMultiplier"]=item.CriticalModifier.ToString()});
  }
  foreach(var pair in nativeObjects)Check(Snapshot(pair.Key)==pair.Value,"all native item/type signatures remain unchanged after private initialization");
  var sword=(BlueprintItemWeapon)cache.Load(BlueprintGuid.Parse("6fd0a849531617844b195f452661b2cd"));Check(sword.DamageType.Type==DamageType.Physical&&sword.DamageType.Physical.Form==PhysicalDamageForm.Slashing,"ordinary longsword remains physical/slashing without Chosen protection");
  for(int kind=0;kind<2;kind++){
   string token=kind==0?"SpiritualWeapon":"SpiritualAlly";var root=Asset<BlueprintAbility>(cache,token);var children=root.GetComponent<AbilityVariants>().Variants;
   Check(children.Length==5&&root.GetComponent<AbilityEffectRunAction>()==null,"one existing spell root has five native forms "+token);
   for(int i=0;i<5;i++){var child=children[i];Check(child.Parent==root&&child.Type==root.Type&&child.Range==root.Range&&child.AvailableMetamagic==root.AvailableMetamagic&&child.SpellResistance==root.SpellResistance,"variant preserves spell metadata "+token+i);var spawn=child.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionSpawnMonster>().Single();var bind=spawn.AfterSpawn.Actions.OfType<PsychicSpiritBind>().Single();Check(spawn.Blueprint==Asset<BlueprintUnit>(cache,token+"Unit")&&bind.Kind==kind&&bind.Choice==i,"actual spawn/bind routes correct form "+token+i);Check(child.GetComponent<SpiritFavoredWeaponRestriction>()!=null==(i==4),"only favored form needs a deity weapon "+token+i);}
  }
  var u=new Psychic070Tests.Caster(cache).Unit;var facts=new EntityFactsManager(u);Set(u,"Facts",facts);
  foreach(string deity in new[]{"IomedaeFeature","PharasmaFeature","GorumFeature","ErastilFeature","SarenraeFeature","DeskariFeature","AtheismFeature","IroriFeature"}){
   facts.List.Clear();Feature(u,Native(cache,deity));var category=(WeaponCategory?)Call("Category",u,4);
   var expected=deity=="IomedaeFeature"?WeaponCategory.Longsword:deity=="PharasmaFeature"?WeaponCategory.Dagger:deity=="GorumFeature"?WeaponCategory.Greatsword:deity=="ErastilFeature"?WeaponCategory.Longbow:deity=="SarenraeFeature"?WeaponCategory.Scimitar:deity=="DeskariFeature"?WeaponCategory.Scythe:WeaponCategory.UnarmedStrike;
   Check(deity=="AtheismFeature"?!category.HasValue:category==expected,"actual native deity resolves favored weapon "+deity);
   Check(new SpiritFavoredWeaponRestriction().IsCasterRestrictionPassed(u)==category.HasValue,"favored option qualification "+deity);
   foreach(int choice in Enumerable.Range(0,4))Check((WeaponCategory?)Call("Category",u,choice)==new[]{WeaponCategory.Battleaxe,WeaponCategory.Flail,WeaponCategory.Warhammer,WeaponCategory.Longsword}[choice],"fixed forms available independently of faith/alignment "+deity+choice);
  }
  facts.List.Clear();AccessTools.Method(Get(facts,"m_Cache").GetType(),"Clear").Invoke(Get(facts,"m_Cache"),null);Check(Call("Category",u,4)==null,"no faith safely rejects favored form");
  for(int k=0;k<4;k++){string token=new[]{"SpiritualWeapon","SpiritualAlly","MagesSword","TwilightKnife"}[k];var unit=Asset<BlueprintUnit>(cache,token+"Unit");Check(unit.Prefab.AssetId=="b88c013664f578846b4e31f2dc1cff44","actual human native animation carrier "+token);var actor=Asset<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(cache,token+"ActorBuff");Check(actor.GetComponent<SpiritWeaponAnimatedVisual>().Kind==k&&actor.GetComponent<SpiritWeaponBlueVisual>()!=null,"scoped lifecycle/blue animation components "+token);}
  Check(Asset<BlueprintItemWeapon>(cache,"TwilightKnifeItem").Category==WeaponCategory.Dagger&&Asset<BlueprintItemWeapon>(cache,"MagesSwordItem").Category==WeaponCategory.Longsword,"knife dagger and Mage sword longsword");
  var selector=Asset<BlueprintActivatableAbility>(cache,"ChosenPreferenceToggle");Check(selector.GetComponent<ActivatableAbilityVariants>()!=null&&selector.GetComponent<ActivationDisable>()!=null&&!selector.HiddenInUI,"native Sacred Weapon single menu entry");
  var refs=(BlueprintActivatableAbilityReference[])Get(selector.GetComponent<ActivatableAbilityVariants>(),"m_Variants");Check(refs.Length==5&&refs.All(r=>r.Get().HiddenInUI&&r.Get().ActionBarAutoFillIgnored),"five persistent conversions hidden from separate entries/autofill");
  var collection=new ActivatableAbilityCollection();collection.Initialize(facts);Set(u,"ActivatableAbilities",collection);
  foreach(var reference in refs){var a=Empty<ActivatableAbility>();Set(a,"Components",new List<EntityFactComponent>());a.RestoreOwnerLink(facts);Set(a,"Blueprint",reference.Get());Set(a,"IsActive",true);facts.List.Add(a);}AccessTools.Method(Get(facts,"m_Cache").GetType(),"Clear").Invoke(Get(facts,"m_Cache"),null);
  var converted=selector.GetComponent<ActivatableAbilityVariants>().GetConversions(u.Descriptor).ToList();Check(converted.Count==5&&converted.All(a=>a.Blueprint.HiddenInUI),"actual native provider returns owner's five hidden persistent options");var slots=new Kingmaker.SlotConversion(converted).GetMechanicSlots(u).Cast<MechanicActionBarSlotActivableAbility>().ToArray();Check(slots.Length==5&&slots.All(s=>s.IsHiddenOverride==false),"actual native conversion menu explicitly reveals hidden children");
  PsychicExpansionTests.Inventory["spirit0148Assets"]=inventory;PsychicExpansionTests.Inventory["spirit0148Assertions"]=n;Console.WriteLine("PASS: "+n+" 0148 native private weapons/types, initialization, criticals, deity forms and Sacred Weapon conversion menu checks; Unity view lifecycle wired to native animation clips.");return n;
 }
 static object GetStatic(Type t,string f)=>AccessTools.Field(t,f).GetValue(null);
 static string Data(object value)=>(string)AccessTools.Method(typeof(PsychicSpiritBind).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicExpansion"),"Signature").Invoke(null,new[]{value});
 static string Snapshot(object o){if(o is BlueprintItemWeapon w)return Data(new object[]{w.AssetGuid,w.Category,w.Damage,w.CriticalModifier,w.CriticalRollEdge,w.DamageType,w.Type.AssetGuid,Get(w,"m_VisualParameters"),Get(w,"m_Enchantments")});var t=(BlueprintWeaponType)o;return Data(new object[]{t.AssetGuid,t.Category,t.BaseDamage,t.CriticalModifier,t.CriticalRollEdge,t.DamageType,Get(t,"m_VisualParameters"),Get(t,"m_Enchantments")});}
}
