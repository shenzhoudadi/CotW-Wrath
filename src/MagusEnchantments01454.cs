using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Items;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.UnitLogic.Mechanics;
namespace CallOfTheWild.Wrath.Prototype
{
    // Observe only items currently owned by one of these two abilities. There
    // is no world/unit scan or per-frame enchantment reconciliation.
    internal static class MagusEnchantments01454
    {
        static readonly ConditionalWeakTable<ItemEntity,UnitEntityData> Watched=new ConditionalWeakTable<ItemEntity,UnitEntityData>();
        internal static void Watch(ItemEntity item,UnitEntityData unit){if(Watched.TryGetValue(item,out var old)){if(!ReferenceEquals(old,unit))throw new InvalidOperationException("01454 enchantment owner changed.");return;}Watched.Add(item,unit);}
        internal static void Unwatch(ItemEntity item){if(item!=null)Watched.Remove(item);}
        internal static bool Current(ItemEntity[] items,BlueprintItemEnchantment[] desired,List<ItemEnchantment> owned)
        {
            foreach(var item in items)foreach(var bp in desired){
                var actual=item.Enchantments.Where(e=>e.Blueprint.AssetGuid==bp.AssetGuid).ToArray();
                bool foreign=actual.Any(e=>!owned.Any(o=>ReferenceEquals(e,o)));
                int ours=actual.Count(e=>owned.Any(o=>ReferenceEquals(e,o)));
                if(ours!=(foreign?0:1))return false;
            }
            return owned.All(e=>e?.Owner!=null&&e.Owner.Enchantments.Any(a=>ReferenceEquals(a,e)));
        }
        internal static void AddMissing(ItemEntity[] items,BlueprintItemEnchantment[] desired,List<ItemEnchantment> owned,MechanicsContext context)
        {
            foreach(var item in items)foreach(var bp in desired){
                if(item.Enchantments.Any(e=>e.Blueprint.AssetGuid==bp.AssetGuid))continue;
                var added=item.AddEnchantment(bp,context);if(added==null)throw new InvalidOperationException("01454 native enchantment creation failed: "+bp.AssetGuid);owned.Add(added);
            }
        }
        internal static void Changed(ItemEntity item)
        {
            if(item==null||!Watched.TryGetValue(item,out var unit))return;
            Mindblade01454.WithManifest(unit,c=>{c.Refresh();return false;});Skirnir01454.WithEnhancement(unit,c=>{c.Refresh();return false;});
        }
    }
    [HarmonyPatch(typeof(ItemEntity),"AddEnchantmentInternal",new[]{typeof(BlueprintItemEnchantment),typeof(MechanicsContext),typeof(Kingmaker.Utility.Rounds?)})]
    internal static class MagusEnchantAdded01454
    {static void Postfix(ItemEntity __instance)=>MagusEnchantments01454.Changed(__instance);}
    [HarmonyPatch(typeof(ItemEntity),nameof(ItemEntity.RemoveEnchantment),new[]{typeof(ItemEnchantment)})]
    internal static class MagusEnchantRemoved01454
    {static void Postfix(ItemEntity __instance)=>MagusEnchantments01454.Changed(__instance);}
}
