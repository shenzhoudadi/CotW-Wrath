using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.MVVM._VM.ActionBar;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.FactLogic;
using UniRx;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicMenuTests
{
    private static int count;
    private static Type menu;
    private static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    private static void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;}
    private static T Asset<T>(BlueprintsCache c,string token) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicAmplificationComponent).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
    private static bool NoTick(ref IObservable<UniRx.Unit> __result){__result=Observable.Never<UniRx.Unit>();return false;}
    private static bool Label(EntityFact __instance,ref string __result){__result=__instance.Blueprint.name;return false;}
    internal static int Run(BlueprintsCache cache)
    {
        var f=new Psychic070Tests.Caster(cache);var manager=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",manager);
        var abilities=new ActivatableAbilityCollection();abilities.Initialize(manager);Set(f.Unit,"<ActivatableAbilities>k__BackingField",abilities);
        var ordinary=new Kingmaker.UnitLogic.Abilities.AbilityCollection();ordinary.Initialize(manager);Set(f.Unit,"<Abilities>k__BackingField",ordinary);
        ActivatableAbility Add(string token) {
            var a=Empty<ActivatableAbility>();Set(a,"<Blueprint>k__BackingField",Asset<BlueprintActivatableAbility>(cache,token));Set(a,"Components",new List<EntityFactComponent>());
            a.RestoreOwnerLink(manager);Set(a,"<IsActive>k__BackingField",true);manager.List.Add(a);if(!abilities.Enumerable.Contains(a))abilities.Enumerable.Add(a);return a;
        }
        menu=typeof(PsychicAmplificationComponent).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAmplificationMenu");
        var children=AccessTools.Method(menu,"Children");
        var root=Add("PhrenicPoolMenu");
        List<MechanicActionBarSlot> Children(ActivatableAbility a)=>(List<MechanicActionBarSlot>)children.Invoke(null,new object[]{a});
        Check(Children(root).Count==0,"Unknown amplifications are not offered");
        var feedback=Add("PsychofeedbackMenu");
        Check(Children(root).Count==0,"Psychofeedback is absent from the root even when its legacy head is owned");
        var defense=Add("DefensivePrognosticationMenu");var d1=Add("DefensivePrognostication1Toggle");var d2=Add("DefensivePrognostication2Toggle");
        var mind=Add("OverpoweringMindMenu");var m1=Add("OverpoweringMind1Toggle");
        Check(Children(root).Count==2 && Children(defense).Count==2 && Children(mind).Count==1,"Root groups learned choices; tier menu only includes granted tier");
        var m2=Add("OverpoweringMind2Toggle");Check(Children(mind).Count==2,"Level-eight grant becomes available without re-registering menu");
        var m3=Add("OverpoweringMind3Toggle");Check(Children(mind).Count==3,"Level-fifteen grant becomes available");
        m3.TemporarilyDisabled=true;Check(Children(mind).Count==2,"Temporarily disabled tier is not exposed");m3.TemporarilyDisabled=false;
        foreach(var featureToken in new[]{"DefensivePrognosticationFeature","OverpoweringMindFeature","PsychofeedbackFeature"}) {
            var facts=Asset<BlueprintFeature>(cache,featureToken).GetComponent<AddFacts>();
            Check(!facts.DoNotRestoreMissingFacts && facts.Facts.Any(x=>x is BlueprintActivatableAbility b && (bool)AccessTools.Method(menu,"IsHead").Invoke(null,new object[]{b})),"Old learned feature uses native post-load missing-fact restoration: "+featureToken);
        }
        var casting=(BlueprintFeature)cache.Load(BlueprintGuid.Parse((string)AccessTools.Field(menu.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicClass"),"CastingId").GetValue(null)));
        Check(casting.GetComponent<AddFacts>().Facts.Any(x=>x==root.Blueprint) && !casting.GetComponent<AddFacts>().DoNotRestoreMissingFacts,"Existing casting feature restores the root via its unchanged AddFacts component");
        var h=new Harmony("CotWPsychic0110.MenuFixture");
        try {
            h.Patch(AccessTools.Method(typeof(MainThreadDispatcher),nameof(MainThreadDispatcher.LateUpdateAsObservable)),prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicMenuTests),nameof(NoTick))));
            foreach(string property in new[]{"Name","Description"})h.Patch(AccessTools.PropertyGetter(typeof(EntityFact),property),prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicMenuTests),nameof(Label))));
            foreach(var t in menu.GetNestedTypes(BindingFlags.NonPublic|BindingFlags.Public).Where(t=>t.GetCustomAttributes(typeof(HarmonyPatch),false).Any()))h.CreateClassProcessor(t).Patch();
            var slot=new MechanicActionBarSlotActivableAbility {ActivatableAbility=root,Unit=f.Unit};
            foreach(int amount in new[]{12,11,0,19}){f.Amount=amount;Check(slot.GetResource()==amount,"Native slot counter follows pool changes including zero");}
            var vm=new ActionBarSlotVM(slot);vm.OnMainClick();
            Check(vm.ConvertedVm.Value.Slots.Count==2 && !root.IsOn && f.Amount==19,"Root opens without activating a buff or spending points");
            var old=vm.ConvertedVm.Value;var branch=old.Slots.First(s=>((MechanicActionBarSlotActivableAbility)s.MechanicActionBarSlot).ActivatableAbility==mind);
            branch.OnMainClick();
            Check(old.IsDisposed && vm.ConvertedVm.Value.Slots.Count==4 && branch.ConvertedVm.Value==null &&
                (vm.ConvertedVm.Value.Slots[0].Tooltip.Value as Kingmaker.UI.MVVM._VM.Tooltip.Templates.TooltipTemplateSimple)?.Header=="Back to Phrenic Pool",
                "Submenu replaces the outer popup; visible tooltip identifies Back; no native nested-view closure");
            Check(vm.ConvertedVm.Value.Slots.Skip(1).Select(s=>((MechanicActionBarSlotActivableAbility)s.MechanicActionBarSlot).ActivatableAbility).SequenceEqual(new[]{m1,m2,m3}),"Leaf slots operate the original native toggle instances");
            vm.ConvertedVm.Value.Slots[0].OnMainClick();Check(vm.ConvertedVm.Value.Slots.Count==2,"Back returns to the root categories");
            vm.OnShowConvertRequest();Check(vm.ConvertedVm.Value==null,"Clicking the open root arrow closes the popup");
            f.Amount=0;vm.OnShowConvertRequest();Check(vm.ConvertedVm.Value!=null && !root.IsOn && f.Amount==0,"Zero-point root can still open to switch modes off");vm.CloseConvert();
            var bar=Empty<ActionBarVM>();Set(bar,"GroupAbilities",new List<ActionBarSlotVM>());
            AccessTools.Method(typeof(ActionBarVM),"CollectAbilities").Invoke(bar,new object[]{f.Unit});
            Check(bar.GroupAbilities.Count==1 && ((MechanicActionBarSlotActivableAbility)bar.GroupAbilities[0].MechanicActionBarSlot).ActivatableAbility==root,"Expanded abilities contain just the root, with leaves retained as character facts: "+string.Join(",",bar.GroupAbilities.Select(x=>x.Name.Value))+"");
            var pinned=new MechanicActionBarSlotActivableAbility {ActivatableAbility=d1,Unit=f.Unit};Check(!pinned.IsBad() && manager.List.Contains(d1),"Old pinned native leaf remains valid after list grouping");
            foreach(var s in bar.GroupAbilities)s.Dispose();vm.Dispose();
        } finally{h.UnpatchAll(h.Id);}
        PsychicExpansionTests.Inventory["psychicMenuAssertions"]=count;
        Console.WriteLine("PASS: "+count+" phrenic menu checks; native managed slots/view models, Unity frame stream replaced; not rendered game UI.");return count;
    }
}
