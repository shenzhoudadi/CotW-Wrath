using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicMawCleanupTests
{
    static int count;static EntityFactsManager manager;
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Check(bool ok,string why){if(!ok)throw new Exception("Maw cleanup: "+why);count++;}
    static T Raw<T>(BlueprintFact bp) where T:EntityFact {
        var f=Empty<T>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"<Manager>k__BackingField",manager);Set(f,"Components",new List<EntityFactComponent>());
        manager.List.Add(f);var c=Get(manager,"m_Cache");AccessTools.Method(c.GetType(),"Add").Invoke(c,new object[]{f});return f;
    }
    static Ability Button(BlueprintAbility bp,int slots){var a=Raw<Ability>(bp);var d=Empty<AbilityData>();Set(d,"Blueprint",bp);Set(d,"m_Fact",a);Set(d,"<HasActionBarSlot>k__BackingField",new CountableFlag(slots));Set(a,"Data",d);return a;}
    internal static int Run(BlueprintsCache cache) {
        var f=new Psychic070Tests.Caster(cache);manager=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",manager);Set(f.Descriptor,"m_IsPlayerFactionCached",(bool?)false);
        var state=Empty<Kingmaker.EntitySystem.PersistentState>();var player=Empty<Kingmaker.Player>();Set(player,"m_Party",new List<Kingmaker.EntitySystem.Entities.UnitEntityData>());Set(player,"m_CharacterListsValid",true);state.PlayerState=player;var oldState=Get(Kingmaker.Game.Instance,"State");Set(Kingmaker.Game.Instance,"State",state);
        Set(f.Descriptor,"UISettings",Empty<Kingmaker.UI.UnitSettings.UnitUISettings>());
        var collection=new AbilityCollection();AccessTools.Method(typeof(EntityFactsManager),"AddDelegate").MakeGenericMethod(typeof(AbilityCollection)).Invoke(manager,new object[]{collection});
        var assembly=typeof(PsychicBloodArmorDamage).Assembly;var factory=assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets");
        BlueprintGuid Id(string n)=>(BlueprintGuid)AccessTools.Method(factory,"Id").Invoke(null,new object[]{n});
        var roar=(BlueprintAbility)cache.Load(Id("SavageMawRoar"));var maw=(BlueprintBuff)cache.Load(Id("SavageMawBuff"));
        var cleanup=assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicMawCleanup");var repair=AccessTools.Method(cleanup,"Repair");
        var stale=Button(roar,1);f.Unit.RemoveFact(stale);
        Check(manager.GetAll<Ability>().Contains(stale)&&stale.Data.TemporarilyDisabled&&!stale.IsDisposed,"Reproduce original slotted disabled Roar in real native collection");
        var h=new Harmony("CotW.Maw.NativeDetachRegression");
        try {
            h.CreateClassProcessor(cleanup.GetNestedType("Detach",BindingFlags.NonPublic)).Patch();
            repair.Invoke(null,new object[]{f.Unit});Check(stale.IsDisposed&&!manager.GetAll<Ability>().Contains(stale)&&!manager.List.Contains(stale),"Old orphan removed from real manager and ability cache");
            repair.Invoke(null,new object[]{f.Unit});Check(manager.GetAll<Ability>().Count==0,"Orphan repair idempotent");
            foreach(var pair in new[]{new[]{"ContingencyStoreNext","ContingencyBuff"},new[]{"ContingencyRelease","ContingencyBuff"},new[]{"ContingencyDismiss","ContingencyBuff"},new[]{"SpiteStoreNext","SpiteBuff"},new[]{"SpiteDismiss","SpiteBuff"},new[]{"DelayedConsumptionStoreNext","DelayedConsumptionBuff"},new[]{"DelayedConsumptionRelease","DelayedConsumptionBuff"},new[]{"DelayedConsumptionDismiss","DelayedConsumptionBuff"},new[]{"EmblemOfGreedStoreNext","EmblemOfGreedBuff"},new[]{"EmblemOfGreedDismiss","EmblemOfGreedBuff"},new[]{"SpiritualWeaponDirect","SpiritualWeaponController"},new[]{"SpiritualWeaponDismiss","SpiritualWeaponController"},new[]{"SpiritualAllyDirect","SpiritualAllyController"},new[]{"SpiritualAllyDismiss","SpiritualAllyController"},new[]{"MagesSwordDirect","MagesSwordController"},new[]{"MagesSwordDismiss","MagesSwordController"},new[]{"TwilightKnifeDirect","TwilightKnifeController"},new[]{"TwilightKnifeDismiss","TwilightKnifeController"},new[]{"ArcaneEyeDismiss","ArcaneEyeController"},new[]{"SavageMawRoar","SavageMawBuff"},new[]{"FireSeedsThrow","FireSeedsBuff"},new[]{"FieryShurikenLaunch","FieryShurikenBuff"},new[]{"OrbOfTheVoidMove","OrbOfTheVoidController"},new[]{"DazzlingBladeRelease","DazzlingBladeBuff"},new[]{"ThreefoldAspectChange","ThreefoldAspectBuff"},new[]{"BowSpiritShot","BowSpiritBuff"},new[]{"AlliedCloakAid","AlliedCloakBuff"},new[]{"ScouringWindsMove","ScouringWindsController"},new[]{"FlamingSphereMove","FlamingSphereController"},new[]{"FlamingSphereGreaterMove","FlamingSphereGreaterController"},new[]{"AggressiveThundercloudMove","AggressiveThundercloudController"},new[]{"AggressiveThundercloudGreaterMove","AggressiveThundercloudGreaterController"},new[]{"BallLightningMove","BallLightningController"},new[]{"UndeadAnatomy1Return","UndeadAnatomy1Buff"},new[]{"UndeadAnatomy2Return","UndeadAnatomy2Buff"},new[]{"UndeadAnatomy3Return","UndeadAnatomy3Buff"},new[]{"GiantForm1TrollReturn","GiantForm1TrollBuff"},new[]{"GiantForm1FireReturn","GiantForm1FireBuff"},new[]{"GiantForm1FrostReturn","GiantForm1FrostBuff"},new[]{"GiantForm2StormReturn","GiantForm2StormBuff"},new[]{"GiantForm2AthachReturn","GiantForm2AthachBuff"}})
            foreach(int slots in new[]{0,1,2})for(int repetition=0;repetition<3;repetition++) {
                var button=Button((BlueprintAbility)cache.Load(Id(pair[0])),slots);var source=(BlueprintBuff)cache.Load(Id(pair[1]));var parent=Raw<Buff>(source);var grant=source.GetComponents<AddFacts>().Single(g=>g.Facts.Any(x=>x==button.Blueprint));
                var runtime=(EntityFactComponent)AccessTools.Method(grant.GetType(),"CreateRuntimeFactComponent").Invoke(grant,null);runtime.Setup(grant);runtime.RestoreOwnerLink(parent);
                var data=runtime.GetData<AddFactsData>();data.AppliedFacts.Add(button);
                repair.Invoke(null,new object[]{f.Unit});Check(!button.IsDisposed&&manager.GetAll<Ability>().Contains(button),"Present source buff protects live or still-loading Roar");
                runtime.Deactivate();Check(button.IsDisposed&&!manager.List.Contains(button)&&!manager.GetAll<Ability>().Contains(button)&&data.AppliedFacts.Count==0,"Real AddFacts Clear -> native removal disposes slotted Roar: "+slots);
                runtime.Deactivate();Check(manager.GetAll<Ability>().Count==0,"Repeated grant cleanup does not resurrect the button");
                manager.List.Remove(parent);var cacheState=Get(manager,"m_Cache");AccessTools.Method(cacheState.GetType(),"Clear").Invoke(cacheState,null);
            }
            var normal=Button(new BlueprintAbility {AssetGuid=BlueprintGuid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaabb1")},1);f.Unit.RemoveFact(normal);
            Check(manager.GetAll<Ability>().Contains(normal)&&normal.Data.TemporarilyDisabled&&!normal.IsDisposed,"Other abilities retain native quickbar behavior");
            var center=Button((BlueprintAbility)cache.Load(BlueprintGuid.Parse("e66116541ac4546c9084a7bcfa47e5a6")),1);
            repair.Invoke(null,new object[]{f.Unit});Check(manager.GetAll<Ability>().Contains(center)&&!center.IsDisposed&&manager.GetAll<Ability>().Contains(normal),"Permanent Center Self and unrelated abilities untouched");
            var post=cleanup.GetNestedType("Restore",BindingFlags.NonPublic);var orphan=Button(roar,2);AccessTools.Method(post,"Postfix").Invoke(null,new object[]{f.Descriptor});
            Check(orphan.IsDisposed&&!manager.GetAll<Ability>().Contains(orphan),"Actual load-repair callback uses same scoped removal");
        }finally{h.UnpatchAll(h.Id);Set(Kingmaker.Game.Instance,"State",oldState);}
        PsychicExpansionTests.Inventory["mawCleanupAssertions"]=count;
        Console.WriteLine("PASS: "+count+" Maw cleanup checks; native disabled-slot failure reproduced, AddFacts removal, disposal and cache eviction executed; load callback invoked on constructed facts, not a real save.");return count;
    }
}
