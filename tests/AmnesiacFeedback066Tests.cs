using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UI.MVVM._VM.ActionBar;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    sealed class FeedbackMessageProbe066 : ILogMessageUIHandler
    {
        internal readonly List<string> Messages=new List<string>();
        public void HandleLogMessage(string text) { Messages.Add(text); }
    }
    sealed class FeedbackActionBarProbe066 : IUnitActionBarUpdateHandler
    {
        internal int Count;
        public void HandleActionBarUpdated() { Count++; }
    }
    sealed class FeedbackSnapshotProbe066 : IUnitActionBarUpdateHandler
    {
        internal UnitEntityData Owner;internal BlueprintAbility Donor;
        internal readonly List<bool> Contains=new List<bool>();
        public void HandleActionBarUpdated()
        { Contains.Add(ActionBarSpellbookHelper.Fetch(Owner).Any(s=>s.Spell.Blueprint.AssetGuid==Donor.AssetGuid)); }
    }
    sealed class FeedbackThrowingLog066 : ILogMessageUIHandler
    {
        internal int Count;
        public void HandleLogMessage(string text) { Count++;throw new InvalidOperationException("injected log subscriber failure"); }
    }
    sealed class FeedbackThrowingRefresh066 : IUnitActionBarUpdateHandler
    {
        internal int Count;
        public void HandleActionBarUpdated() { Count++;throw new InvalidOperationException("injected action-bar subscriber failure"); }
    }
    static bool FeedbackInjectLogRaise066;
    static void FeedbackLogRaiseFault066(){if(FeedbackInjectLogRaise066)throw new InvalidOperationException("injected EventBus.RaiseEvent failure");}
    static bool FeedbackInjectDirty066;
    static void FeedbackDirtyFault066(){if(FeedbackInjectDirty066)throw new InvalidOperationException("injected UISettings.SetDirty failure");}
    static void Feedback066(BlueprintsCache parent,Harmony h,string chosen)
    {
        EquipmentPatches(h);
        foreach(var type in T("PsychicUndercasting").GetNestedTypes(System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public)
            .Concat(T("TemporaryKnowledge01464").GetNestedTypes(System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public))
            .Where(t=>t.GetCustomAttributes(typeof(HarmonyPatch),false).Any()))h.CreateClassProcessor(type).Patch();
        h.Patch(AccessTools.Method(typeof(RuleRollDice),"OnTrigger"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(RecallDiceInput064)));
        var c=AmnesiacCache064(parent);Register(c,"Amnesiac01464");
        var menu=A<BlueprintAbility>(c,"AmnesiacRecallMenu101464");
        var donor=A<BlueprintAbility>(c,"MindThrust1");
        var leaf=menu.GetComponent<AbilityVariants>().Variants.Single(a=>a.GetComponent<AmnesiaRecallGate01464>().Spell.Guid==donor.AssetGuid);
        if(chosen=="feedback066labels"){
            var display=AccessTools.Field(typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact),"m_DisplayName");
            var desc=AccessTools.Field(typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact),"m_Description");
            var donorDisplay=(Kingmaker.Localization.LocalizedString)display.GetValue(donor);
            var donorDesc=(Kingmaker.Localization.LocalizedString)desc.GetValue(donor);
            var recallDisplay=(Kingmaker.Localization.LocalizedString)display.GetValue(leaf);
            var recallDesc=(Kingmaker.Localization.LocalizedString)desc.GetValue(leaf);
            Check(donorDisplay!=null&&recallDisplay!=null&&donorDisplay.Key==recallDisplay.Key,"066 actual recall label uses donor localized name key");
            Check(donorDesc!=null&&recallDesc!=null&&donorDesc.Key==recallDesc.Key,"066 actual recall description uses donor localized description key");
            return;
        }
        if(chosen=="feedback066capacity"){
            for(int circle=2;circle<=9;circle++){
                int level=circle*2;var owner=AmnesiacOwner064(c,level,out var book);
                var resource=A<BlueprintAbilityResource>(c,"AmnesiacSlot"+circle+"01464");
                Check(book.MaxSpellLevel>=circle,"066 actual spellbook unlocks its circle at class level "+level);
                Check(resource.GetMaxAmount(owner.Descriptor)>0&&owner.Resources.GetResourceAmount(resource)>0,"066 actual native resource exists at first unlocked circle="+circle+" level="+level);
                Check(A<BlueprintAbility>(c,"AmnesiacRecallMenu"+circle+"01464").GetComponent<AmnesiaRecallGate01464>().IsAbilityVisible(new AbilityData(A<BlueprintAbility>(c,"AmnesiacRecallMenu"+circle+"01464"),owner.Descriptor)),"066 native recollection menu visible at first unlocked circle="+circle);
            }
            return;
        }
        if(chosen=="feedback066growth"){
            FeedbackGrowth066(c);
            return;
        }
        if(chosen=="feedback066migration"){
            FeedbackMigration066(c);
            return;
        }
        if(chosen=="feedback066manager"){
            FeedbackManager066(c);
            return;
        }
        if(chosen=="feedback066snapshot"){
            FeedbackSnapshot066(c,leaf,donor);
            return;
        }
        var u=AmnesiacOwner064(c,5,out var spellbook);
        var ability=new AbilityData(leaf,u.Descriptor);var gate=leaf.GetComponent<AmnesiaRecallGate01464>();
        Check(gate.IsCasterRestrictionPassed(u),"066 native recollection ability accepts its unknown spell");
        D100Input064=51;D10Input064=7;
        var message=new FeedbackMessageProbe066();var refresh=new FeedbackActionBarProbe066();
        EventBus.Subscribe(message);EventBus.Subscribe(refresh);
        try{
            if(chosen=="feedback066combatlog")Set(u.CombatState,"m_InCombat",true);
            if(chosen=="feedback066logfault"||chosen=="feedback066uifault"){
                var logFault=new FeedbackThrowingLog066();var uiFault=new FeedbackThrowingRefresh066();
                if(chosen=="feedback066logfault")EventBus.Subscribe(logFault);else EventBus.Subscribe(uiFault);
                try{
                    int before=u.Resources.GetResourceAmount(A<BlueprintAbilityResource>(c,"AmnesiacSlot101464"));
                    ApplySelf(u,ability);
                    Check(spellbook.IsKnown(donor)&&u.Resources.GetResourceAmount(A<BlueprintAbilityResource>(c,"AmnesiacSlot101464"))==before-1,"066 throwing UI subscriber cannot prevent actual spell grant or native payment");
                    if(chosen=="feedback066logfault")Check(logFault.Count>0,"066 throwing game-log subscriber reached through actual event bus");
                    else Check(uiFault.Count>0,"066 throwing action-bar subscriber reached through actual event bus");
                    if(chosen=="feedback066uifault"){
                        var source=u.Buffs.Enumerable.Single(b=>b.Blueprint.GetComponent<AmnesiaSource01464>()!=null);
                        source.Remove();
                        Check(!spellbook.IsKnown(donor)&&uiFault.Count>=2,"066 throwing action-bar subscriber cannot prevent native source removal");
                    }
                }finally{EventBus.Unsubscribe(logFault);EventBus.Unsubscribe(uiFault);}
                return;
            }
            if(chosen=="feedback066logendpoint"||chosen=="feedback066uiendpoint"){
                var endpoint=chosen=="feedback066logendpoint"?
                    typeof(EventBus).GetMethods().Single(m=>m.Name=="RaiseEvent"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==2&&m.GetParameters()[0].ParameterType.IsGenericType).MakeGenericMethod(typeof(ILogMessageUIHandler)):
                    AccessTools.Method(typeof(Kingmaker.UI.UnitSettings.UnitUISettings),"SetDirty");
                var fault=AccessTools.Method(typeof(MagusBatch01454Tests),chosen=="feedback066logendpoint"?nameof(FeedbackLogRaiseFault066):nameof(FeedbackDirtyFault066));
                h.Patch(endpoint,prefix:new HarmonyMethod(fault));
                try{
                    FeedbackInjectLogRaise066=chosen=="feedback066logendpoint";FeedbackInjectDirty066=chosen=="feedback066uiendpoint";
                    int before=u.Resources.GetResourceAmount(A<BlueprintAbilityResource>(c,"AmnesiacSlot101464"));
                    ApplySelf(u,ability);
                    Check(spellbook.IsKnown(donor)&&u.Resources.GetResourceAmount(A<BlueprintAbilityResource>(c,"AmnesiacSlot101464"))==before-1,"066 injected UI endpoint exception does not interrupt actual spell grant or payment");
                    if(chosen=="feedback066uiendpoint"){
                        var source=u.Buffs.Enumerable.Single(b=>b.Blueprint.GetComponent<AmnesiaSource01464>()!=null);source.Remove();
                        Check(!spellbook.IsKnown(donor),"066 injected action-bar endpoint exception does not interrupt actual source removal");
                    }
                }finally{FeedbackInjectLogRaise066=false;FeedbackInjectDirty066=false;h.Unpatch(endpoint,fault);}
                return;
            }
            ApplySelf(u,ability);
            Check(spellbook.IsKnown(donor),"066 actual recall grants the spell in native spellbook");
            if(chosen=="feedback066ui"){
                Check(u.UISettings.Dirty&&refresh.Count>0,"066 recalled spell triggers actual native action-bar dirty/update event");
                Check(ActionBarSpellbookHelper.Fetch(u).Any(s=>s.Spell.Blueprint.AssetGuid==donor.AssetGuid),"066 native action-bar spell fetch includes newly recalled spell immediately");
            }
            if(chosen=="feedback066log")
                Check(message.Messages.Any(m=>m.Contains("51")),"066 game log receives real d100 recollection result");
            if(chosen=="feedback066combatlog")
                Check(message.Messages.Any(m=>m.Contains("51")&&m.Contains("7")&&m.Contains("58")),"066 game log receives d100, combat d10 and actual combined result");
        }finally{EventBus.Unsubscribe(message);EventBus.Unsubscribe(refresh);}
    }
    static int FeedbackCapacity066(int level,int circle,Newtonsoft.Json.Linq.JObject oracle)
    {
        int old=(int)oracle["capacityByClassLevel"][level-1]["byCircle"][circle-1];
        return old+(circle>=2&&level==circle*2?1:0);
    }
    static void FeedbackGrowth066(BlueprintsCache c)
    {
        var oracle=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(Root(),"work/batch01464/amnesiac-table-oracle064.json")));
        var owner=AmnesiacOwner064(c,1,out var book);var cls=N<BlueprintCharacterClass>(c,Psychic058);
        Set(owner.Progression,"Owner",owner.Descriptor);
        if(owner.State.Features==null)Set(owner.State,"Features",System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnitMechanicFeatures)));
        if(owner.State.Features.LegendaryHero==null)Set(owner.State.Features,"LegendaryHero",new Kingmaker.Utility.CountableFlag());
        var progression=Kingmaker.Game.Instance.BlueprintRoot.Progression;var xp=AccessTools.Field(progression.GetType(),"m_XPTable").GetValue(progression);
        Set(progression,"m_XPTable",Ref063<BlueprintStatProgressionReference>(N<BlueprintStatProgression>(c,"87c24ce6bcf1a994296f3c582c1a632b")));
        if(AccessTools.Field(typeof(UnitProgressionData),"m_ClassesOrder").GetValue(owner.Progression)==null)Set(owner.Progression,"m_ClassesOrder",new List<BlueprintCharacterClass>{cls});
        var fact=owner.Facts.List.Single(f=>f.Blueprint==A<BlueprintFeature>(c,"AmnesiacRecollectionFeature01464"));
        var runtime=fact.Components.Single(r=>r.SourceBlueprintComponent is AmnesiaCapacity01464);
        var first=A<BlueprintAbilityResource>(c,"AmnesiacSlot101464");owner.Resources.Spend(first,1);
        try{
            for(int level=2;level<=20;level++){
                int[] before=Enumerable.Range(1,9).Select(i=>owner.Resources.GetResourceAmount(A<BlueprintAbilityResource>(c,"AmnesiacSlot"+i+"01464"))).ToArray();
                int[] previous=runtime.GetData<AmnesiaCapacityData01464>().Previous.ToArray();
                owner.Progression.AddClassLevel(cls);book.AddLevelFromClass(cls);
                EventBus.RaiseEvent<IOwnerGainLevelHandler>(owner,h=>h.HandleUnitGainLevel());
                for(int circle=1;circle<=9;circle++){
                    var resource=A<BlueprintAbilityResource>(c,"AmnesiacSlot"+circle+"01464");
                    int max=FeedbackCapacity066(level,circle,oracle);
                    int expected=Math.Min(max,before[circle-1]+Math.Max(0,max-previous[circle-1]));
                    Check(resource.GetMaxAmount(owner.Descriptor)==max&&owner.Resources.GetResourceAmount(resource)==expected,
                        "066 continuous actual class level up retains old capacity oracle except exactly eight first-unlock cells level="+level+" circle="+circle);
                    if(circle>=2&&level==2*circle)Check(book.MaxSpellLevel>=circle&&expected>0,"066 same actor gains unlocked recollection circle="+circle);
                }
                var balances=Enumerable.Range(1,9).Select(i=>owner.Resources.GetResourceAmount(A<BlueprintAbilityResource>(c,"AmnesiacSlot"+i+"01464"))).ToArray();
                var saved=Newtonsoft.Json.JsonConvert.SerializeObject(runtime.GetData<AmnesiaCapacityData01464>());
                runtime.GetData<AmnesiaCapacityData01464>().Previous=Newtonsoft.Json.JsonConvert.DeserializeObject<AmnesiaCapacityData01464>(saved).Previous;
                EventBus.RaiseEvent<IOwnerGainLevelHandler>(owner,h=>h.HandleUnitGainLevel());
                fact.Reapply();fact.TurnOff();fact.TurnOn();
                Check(balances.SequenceEqual(Enumerable.Range(1,9).Select(i=>owner.Resources.GetResourceAmount(A<BlueprintAbilityResource>(c,"AmnesiacSlot"+i+"01464")))),"066 duplicate notification, serialized previous, Reapply and toggle cannot refill resources level="+level);
            }
        }finally{Set(progression,"m_XPTable",xp);}
    }
    static void FeedbackMigration066(BlueprintsCache c)
    {
        var oracle=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(Root(),"work/batch01464/amnesiac-table-oracle064.json")));
        for(int circle=2;circle<=9;circle++){
            int level=circle*2;var owner=AmnesiacOwner064(c,level,out var book);
            var fact=owner.Facts.List.Single(f=>f.Blueprint==A<BlueprintFeature>(c,"AmnesiacRecollectionFeature01464"));
            var runtime=fact.Components.Single(r=>r.SourceBlueprintComponent is AmnesiaCapacity01464);
            var data=runtime.GetData<AmnesiaCapacityData01464>();
            var current=A<BlueprintAbilityResource>(c,"AmnesiacSlot"+circle+"01464");owner.Resources.Spend(current,owner.Resources.GetResourceAmount(current));
            var previous=Enumerable.Range(1,9).Select(i=>(int)oracle["capacityByClassLevel"][level-1]["byCircle"][i-1]).ToArray();
            int otherIndex=Array.FindIndex(previous,x=>x>0);
            var other=otherIndex<0?null:A<BlueprintAbilityResource>(c,"AmnesiacSlot"+(otherIndex+1)+"01464");
            if(other!=null)owner.Resources.Spend(other,owner.Resources.GetResourceAmount(other));
            var raw=Newtonsoft.Json.JsonConvert.SerializeObject(new AmnesiaCapacityData01464{Previous=previous});
            data.Previous=Newtonsoft.Json.JsonConvert.DeserializeObject<AmnesiaCapacityData01464>(raw).Previous;
            fact.TurnOff();fact.TurnOn();
            Check(owner.Resources.GetResourceAmount(current)==1,"066 serialized 065 Previous load and native turn-on adds missing new circle exactly once="+circle);
            if(other!=null)Check(owner.Resources.GetResourceAmount(other)==0,"066 serialized previous retains previously spent other circle="+(otherIndex+1));
            owner.Resources.Spend(current,1);
            EventBus.RaiseEvent<IOwnerGainLevelHandler>(owner,h=>h.HandleUnitGainLevel());fact.Reapply();fact.TurnOff();fact.TurnOn();
            Check(owner.Resources.GetResourceAmount(current)==0&&(other==null||owner.Resources.GetResourceAmount(other)==0),"066 old-save reconcile and duplicate notifications never refill consumed resources circle="+circle);
        }
    }
    static void FeedbackManager066(BlueprintsCache c)
    {
        var oracle=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(Root(),"work/batch01464/amnesiac-table-oracle064.json")));
        for(int circle=2;circle<=9;circle++){
            int level=circle*2;var owner=AmnesiacOwner064(c,level,out var book);
            var fact=owner.Facts.List.Single(f=>f.Blueprint==A<BlueprintFeature>(c,"AmnesiacRecollectionFeature01464"));
            var runtime=fact.Components.Single(r=>r.SourceBlueprintComponent is AmnesiaCapacity01464);
            var data=runtime.GetData<AmnesiaCapacityData01464>();
            var resource=A<BlueprintAbilityResource>(c,"AmnesiacSlot"+circle+"01464");
            owner.Resources.Spend(resource,owner.Resources.GetResourceAmount(resource));
            int[] old=Enumerable.Range(1,9).Select(i=>(int)oracle["capacityByClassLevel"][level-1]["byCircle"][i-1]).ToArray();
            int otherIndex=Array.FindIndex(old,x=>x>0);
            var other=otherIndex<0?null:A<BlueprintAbilityResource>(c,"AmnesiacSlot"+(otherIndex+1)+"01464");
            if(other!=null)owner.Resources.Spend(other,owner.Resources.GetResourceAmount(other));
            string json=Newtonsoft.Json.JsonConvert.SerializeObject(new AmnesiaCapacityData01464{Previous=old});
            data.Previous=Newtonsoft.Json.JsonConvert.DeserializeObject<AmnesiaCapacityData01464>(json).Previous;
            owner.Facts.TurnOff();Set(fact,"IsPostLoadExecuted",false);
            owner.Resources.PostLoad(owner.Descriptor);
            owner.Facts.PostLoad();
            Check(fact.IsPostLoadExecuted,"066 real EntityFactsManager.PostLoad invokes restored fact PostLoad circle="+circle);
            owner.Facts.TurnOn();
            Check(owner.Resources.GetResourceAmount(resource)==1&&(other==null||owner.Resources.GetResourceAmount(other)==0),"066 native resources PostLoad then facts manager PostLoad/TurnOn reconciles old Previous without refilling spent slots circle="+circle);
            owner.Resources.Spend(resource,1);owner.Facts.TurnOff();owner.Facts.TurnOn();
            Check(owner.Resources.GetResourceAmount(resource)==0&&(other==null||owner.Resources.GetResourceAmount(other)==0),"066 second facts manager TurnOn cannot duplicate the migration circle="+circle);
        }
    }
    static void FeedbackSnapshot066(BlueprintsCache c,BlueprintAbility leaf,BlueprintAbility donor)
    {
        var cls=N<BlueprintCharacterClass>(c,Psychic058);
        foreach(string kind in new[]{"single","overlap","permanent"}){
            var owner=AmnesiacOwner064(c,5,out var book);var ability=new AbilityData(leaf,owner.Descriptor);
            var probe=new FeedbackSnapshotProbe066{Owner=owner,Donor=donor};EventBus.Subscribe(probe);
            try{
                D100Input064=51;D10Input064=7;ApplySelf(owner,ability);
                Check(book.IsKnown(donor)&&probe.Contains.Count>0&&probe.Contains.Last(),"066 synchronous native Fetch observes recalled spell during add event, kind="+kind);
                var source=owner.Buffs.Enumerable.Single(b=>b.Blueprint.GetComponent<AmnesiaSource01464>()!=null);
                Kingmaker.UnitLogic.Buffs.Buff foreign=null;
                if(kind=="overlap")foreign=owner.Buffs.AddBuff(TemporaryBuff064(c,"FeedbackOverlap066",cls,donor,1),owner,null);
                if(kind=="permanent")book.AddKnown(1,donor,false);
                probe.Contains.Clear();source.Remove();
                bool keep=kind!="single";
                Check(book.IsKnown(donor)==keep&&probe.Contains.Count>0&&probe.Contains.Last()==keep,"066 synchronous native Fetch sees exact final knowledge at source removal, kind="+kind);
                if(foreign!=null)foreign.Remove();
            }finally{EventBus.Unsubscribe(probe);}
        }
    }
}
