using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.ElementsSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class Feedback01421Tests
{
    static int n,casts,checks,pendingCasts;static BlueprintsCache cache;static bool success=true,throwOnCast;static TimeSpan now;
    static readonly List<Action> callbacks=new List<Action>();
    static void Set(object o,string f,object v)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).SetValue(o,v);
    static object Get(object o,string f)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).GetValue(o);
    static void Check(bool b,string m){if(!b)throw new Exception("Feedback01421: "+m);n++;}
    static Type Module(string s)=>typeof(CrueltyFocusDC).Assembly.GetType("CallOfTheWild.Wrath.Prototype."+s,true);
    static object Helper(string s,params object[] args)=>AccessTools.Method(typeof(SpellAndSword01417Tests),s).Invoke(null,args);
    static UnitEntityData Unit(string id)=>(UnitEntityData)Helper("Unit",cache,id);
    static Feature Fact(UnitEntityData u,BlueprintFeature f)=>(Feature)Helper("Fact",u,f);
    static T Ref<T>(SimpleBlueprint b)where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(b);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,b,null);return r;}
    static bool Skip()=>false;
    static bool Time(ref TimeSpan __result){__result=now;return false;}
    static bool Skill(RuleStatCheck __0,ref RuleStatCheck __result){checks++;Set(__0,"m_Success",success);__result=__0;return false;}
    static bool Difficulty(int __0,ref int __1,ref int __result){__1=0;__result=__0;return false;}
    static bool Params(ref AbilityParams __result){__result=new AbilityParams{CasterLevel=12,DC=20};return false;}
    static bool AddBuff(BuffCollection __instance,BlueprintBuff __0,MechanicsContext __1,TimeSpan? __2,ref Buff __result)
    {__result=Psychic034Tests.Fact(__instance.Owner.Unit,__0,__1.MaybeCaster,12);Set(__result,"UniqueId",Guid.NewGuid().ToString("N"));return false;}
    static bool AddUnitBuff(UnitDescriptor __0,BlueprintBuff __1,MechanicsContext __2,TimeSpan? __3,ref Buff __result)
    {return AddBuff(__0.Buffs,__1,__2,__3,ref __result);}
    static bool Remove(Buff __instance){Set(__instance,"IsDisposed",true);Set(__instance,"IsActive",false);__instance.Owner.Unit.Facts.List.Remove(__instance);AccessTools.Method(Get(__instance.Owner.Unit.Facts,"m_Cache").GetType(),"Clear").Invoke(Get(__instance.Owner.Unit.Facts,"m_Cache"),null);return false;}
    static bool Cast(ContextActionCastSpell __instance){if(throwOnCast)throw new InvalidOperationException("Feint cast boundary failure");casts++;var scope=ContextData<MechanicsContext.Data>.Current;var target=scope.CurrentTarget.Unit;
        var ctx=new AbilityExecutionContext(new AbilityData(__instance.Spell,scope.Context.MaybeCaster.Descriptor),new AbilityParams{CasterLevel=12},target);
        var rule=new RuleCastSpell(ctx.Ability,new TargetWrapper(target));Set(rule,"Context",ctx);rule.OnTrigger(null);
        var pending=AccessTools.Field(Module("ImprovedFeintCastPatch"),"Pending").GetValue(null);
        if((bool)AccessTools.Method(pending.GetType(),"TryGetValue").Invoke(pending,new object[]{ctx,null}))pendingCasts++;
        callbacks.Add(()=>{using(ctx.GetDataScope(target))__instance.Spell.GetComponent<AbilityEffectRunAction>().Actions.Run();});return false;}
    internal static int Pain(BlueprintsCache c,Assembly a)
    {
        n=0;var entries=((Array)AccessTools.Field(a.GetType("CallOfTheWild.Wrath.Prototype.PsychicSharedSpellCatalog"),"Entries").GetValue(null)).Cast<object>().ToArray();
        string[] ids={"edd43108e61a438694624b6f6e6f4bce","776eefcb0ba841bfae8ceba58a19ff29"};int[] levels={3,5};
        var links=ids.Select(id=>entries.FirstOrDefault(e=>(string)Get(e,"Guid")==id)).ToArray();
        Check(links.All(x=>x!=null),"Both PrestigePlus Debilitating Pain links are registered");
        var integrate=AccessTools.Method(a.GetType("CallOfTheWild.Wrath.Prototype.PsychicSpellIntegration"),"Register");
        var list=(BlueprintSpellList)c.Load(BlueprintGuid.Parse("6a4b9d6651b155058004354a803da3bf"));
        var original=list.SpellsByLevel.Select(l=>(List<BlueprintAbilityReference>)Get(l,"m_Spells")).ToArray();
        var old=ids.Select(id=>c.Load(BlueprintGuid.Parse(id))).ToArray();var spells=new List<BlueprintAbility>();
        try {
            for(int i=0;i<2;i++) {
                Check((int)Get(links[i],"Psychic")==levels[i] && (int)Get(links[i],"Occultist")==-1 && (int)Get(links[i],"Spiritualist")==-1,"Psychic-only correct circle "+levels[i]);
                c.RemoveCachedBlueprint(BlueprintGuid.Parse(ids[i]));
            }
            integrate.Invoke(null,new object[]{c});Check(!list.SpellsByLevel.Any(l=>l.Spells.Any(s=>ids.Contains(s.AssetGuid.ToString()))),"absent PP creates no replacement spells");
            for(int i=0;i<2;i++) {
                var bp=new BlueprintAbility {AssetGuid=BlueprintGuid.Parse(ids[i]),name=(string)Get(links[i],"Name"),Type=AbilityType.Spell,ComponentsArray=new BlueprintComponent[]{new SpellComponent{School=SpellSchool.Enchantment},new AbilityEffectRunAction{Actions=new ActionList{Actions=Array.Empty<GameAction>()}}}};
                bp.OnEnable();c.AddCachedBlueprint(bp.AssetGuid,bp);spells.Add(bp);
            }
            integrate.Invoke(null,new object[]{c});Check(!list.SpellsByLevel.Any(l=>l.Spells.Any(s=>ids.Contains(s.AssetGuid.ToString()))),"disabled/unpublished PP spells not reenabled");
            var donor=new BlueprintSpellList {AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),SpellsByLevel=Enumerable.Range(0,10).Select(i=>new SpellLevelList(i)).ToArray()};
            for(int i=0;i<2;i++) {Set(donor.SpellsByLevel[levels[i]],"m_Spells",new List<BlueprintAbilityReference>{Ref<BlueprintAbilityReference>(spells[i])});var sc=new SpellListComponent{SpellLevel=levels[i]};Set(sc,"m_SpellList",Ref<BlueprintSpellListReference>(donor));spells[i].ComponentsArray=spells[i].ComponentsArray.Concat(new[]{sc}).ToArray();spells[i].OnEnable();}
            var components=spells.Select(s=>s.ComponentsArray).ToArray();integrate.Invoke(null,new object[]{c});integrate.Invoke(null,new object[]{c});
            for(int i=0;i<2;i++) {Check(list.SpellsByLevel[levels[i]].Spells.Count(s=>s.AssetGuid.ToString()==ids[i])==1,"enabled late PP linked once at "+levels[i]);Check(list.SpellsByLevel.Where(l=>l.SpellLevel!=levels[i]).All(l=>l.Spells.All(s=>s.AssetGuid.ToString()!=ids[i])),"no other circle registration");Check(ReferenceEquals(components[i],spells[i].ComponentsArray)&&spells[i].GetComponents<SpellListComponent>().Count()==1,"foreign spell graph unchanged");Check(donor.SpellsByLevel[levels[i]].Spells.Count()==1,"Witch/provider list unchanged");}
            for(int i=0;i<original.Length;i++)Check(original[i].All(r=>list.SpellsByLevel[i].Spells.Any(s=>s.AssetGuid==r.Guid)),"existing Psychic list preserved at "+i);
        }finally{for(int i=0;i<2;i++){c.RemoveCachedBlueprint(BlueprintGuid.Parse(ids[i]));if(old[i]!=null)c.AddCachedBlueprint(BlueprintGuid.Parse(ids[i]),old[i]);}for(int i=0;i<original.Length;i++){Set(list.SpellsByLevel[i],"m_Spells",original[i]);Set(list.SpellsByLevel[i],"m_SpellsProxy",null);Set(list.SpellsByLevel[i],"m_SpellsFiltered",null);}}
        PsychicExpansionTests.Inventory["pain01421Assertions"]=n;Console.WriteLine("PASS: "+n+" exact Pain list/provider contracts.");return n;
    }
    internal static int Feint(BlueprintsCache c)
    {
        n=casts=checks=pendingCasts=0;throwOnCast=false;cache=c;now=TimeSpan.Zero;var h=new Harmony("CotW01421.FeintContracts");
        var oldTime=Game.Instance.TimeController;Set(Game.Instance,"TimeController",System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Kingmaker.Controllers.TimeController)));
        void Patch(MethodBase m,string name)=>h.Patch(m,prefix:new HarmonyMethod(typeof(Feedback01421Tests),name));
        Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),nameof(Skip));Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));
        Patch(AccessTools.Method(typeof(Kingmaker.Designers.GameHelper),"TriggerStatCheck"),nameof(Skill));
        Patch(AccessTools.Method(typeof(RuleSkillCheck),"AdjustDCByDifficulty"),nameof(Difficulty));
        Patch(AccessTools.Method(typeof(BuffCollection),"AddBuff",new[]{typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),nameof(AddBuff));
        h.Patch(AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),prefix:new HarmonyMethod(typeof(Feedback01421Tests),nameof(AddUnitBuff)){priority=Priority.First});
        Patch(AccessTools.Method(typeof(Buff),"Remove"),nameof(Remove));Patch(AccessTools.Method(typeof(ContextActionCastSpell),"RunAction"),nameof(Cast));
        Patch(AccessTools.Method(typeof(AbilityData),"CalculateParams"),nameof(Params));
        Patch(AccessTools.Method(typeof(RuleCastSpell),"OnTrigger"),nameof(Skip));
        foreach(var name in new[]{"ImprovedFeintNativeCastPatch","ImprovedFeintCastPatch","ImprovedFeintAppliedPatch","ImprovedFeintConsumePatch","ImprovedFeintEndPatch"})h.CreateClassProcessor(Module(name)).Patch();
        try {
            var mod=Module("ImprovedTwoWeaponFeint");var mode=(BlueprintBuff)c.Load(BlueprintGuid.Parse("621523572cfe4dc48ee1bdddba2f4725"));
            var first=mode.ComponentsArray;var originalTrigger=mode.GetComponent<AddInitiatorAttackWithWeaponTrigger>();var originalActions=originalTrigger.Action;
            var originalGraph=((IEnumerable<GameAction>)AccessTools.Method(mod,"Walk").Invoke(null,new object[]{originalActions})).ToArray();
            AccessTools.Method(mod,"Register").Invoke(null,new object[]{c});
            var id=(BlueprintGuid)AccessTools.Property(mod,"FeatId").GetValue(null,null);var feat=(BlueprintFeature)c.Load(id);
            AccessTools.Method(mod,"Register").Invoke(null,new object[]{c});Check(ReferenceEquals(feat,c.Load(id)),"root identity stable");
            Check(ReferenceEquals(first,mode.ComponentsArray)&&ReferenceEquals(originalTrigger,mode.GetComponent<AddInitiatorAttackWithWeaponTrigger>()),"registration preserves shared native component identities");
            Check(ReferenceEquals(originalActions,originalTrigger.Action)&&originalGraph.SequenceEqual((IEnumerable<GameAction>)AccessTools.Method(mod,"Walk").Invoke(null,new object[]{originalTrigger.Action})),"registration preserves every shared native action identity");
            Check(originalGraph.All(a=>!(a is ImprovedFeintNativeCast)),"shared native graph contains no CotW cast wrapper for other mod scanners");
            Check(feat.HasGroup(FeatureGroup.Feat)&&feat.HasGroup(FeatureGroup.CombatFeat)&&feat.Ranks==1,"single general/combat feat");Check(feat.GetComponents<PrerequisiteFeature>().Count()==4,"all four feat prerequisites");
            Check(feat.GetComponents<PrerequisiteStatValue>().Any(p=>p.Stat==StatType.Dexterity&&p.Value==17)&&feat.GetComponents<PrerequisiteStatValue>().Any(p=>p.Stat==StatType.BaseAttackBonus&&p.Value==6),"Dex17 and BAB6 requirements");
            var trigger=mode.GetComponent<AddInitiatorAttackWithWeaponTrigger>();var nativeCast=((IEnumerable<GameAction>)AccessTools.Method(mod,"Walk").Invoke(null,new object[]{trigger.Action})).OfType<ContextActionCastSpell>().Single();
            Check(nativeCast.Spell.AssetGuid.ToString()=="1bb6f0b196aa457ba80bdb312dc64952","exact native cast retained");Check(mode.GetComponent<ReduceAttacksCount>().ReduceCount==1,"native attack sacrifice retained");
            foreach(string pool in (string[])AccessTools.Field(Module("CombatFeatPoolRepair"),"NativeIds").GetValue(null)) {var p=c.Load(BlueprintGuid.Parse(pool))as BlueprintFeatureSelection;if(p==null)continue;foreach(string f in new[]{"m_Features","m_AllFeatures"})Check(((BlueprintFeatureReference[])Get(p,f)).Count(r=>r.Guid==id)==1,"eligible pool unique "+pool);}
            var u=Unit("01421feinter");var other=Unit("01421other");var target=Unit("01421target");Fact(u,(BlueprintFeature)c.Load(BlueprintGuid.Parse("c610310d31414edabcedf0c8a6fe32c4")));var fact=Fact(u,feat);var modeFact=Psychic034Tests.Fact(u,mode,u,12);
            var weapon=(Kingmaker.Items.ItemEntityWeapon)Helper("Weapon",u,WeaponCategory.Shortsword,"01421blade");Helper("Equip",u,weapon);var attack=(RuleAttackWithWeapon)Helper("Attack",u,target,weapon,false);attack.IsFullAttack=true;attack.IsFirstAttack=true;
            void Queue(MechanicsContext context,RuleAttackWithWeapon currentAttack){Rulebook.CurrentContext.PushEvent(currentAttack);try{using(context.GetDataScope(target))nativeCast.RunAction();}finally{Rulebook.CurrentContext.PopEvent(currentAttack);}}
            void Deliver(){foreach(var a in callbacks.ToArray())a();callbacks.Clear();}
            bool ScopeClosed()=>AccessTools.Field(Module("ImprovedFeintNativeCastPatch"),"CurrentCaster").GetValue(null)==null&&AccessTools.Field(Module("ImprovedFeintNativeCastPatch"),"CurrentTarget").GetValue(null)==null;
            void Attempt(bool ok){success=ok;Queue(modeFact.MaybeContext,attack);Check(ScopeClosed(),"scope closed before delayed delivery");Deliver();}
            void Consume(UnitEntityData actor){var nativeFact=actor.Facts.List.FirstOrDefault(f=>f.Blueprint.AssetGuid.ToString()=="c610310d31414edabcedf0c8a6fe32c4");var native=((BlueprintFeature)nativeFact.Blueprint).GetComponents<AddInitiatorAttackWithWeaponTrigger>().First(t=>!t.TriggerBeforeAttack);using(nativeFact.MaybeContext.GetDataScope(target))native.Action.Run();}
            void Before(UnitEntityData actor){var nativeFact=actor.Facts.List.First(f=>f.Blueprint.AssetGuid.ToString()=="c610310d31414edabcedf0c8a6fe32c4");var native=((BlueprintFeature)nativeFact.Blueprint).GetComponents<AddInitiatorAttackWithWeaponTrigger>().First(t=>t.TriggerBeforeAttack);using(nativeFact.MaybeContext.GetDataScope(target))native.Action.Run();}
            int ArmorFlags()=>target.Buffs.Enumerable.Count(b=>b.Blueprint.AssetGuid.ToString()=="4cf41187a0764c51b86ce1f28df7de97"&&!b.IsDisposed);
            int Flags()=>target.Buffs.Enumerable.Count(b=>b.Blueprint.AssetGuid.ToString()=="e14f5e333e0e42fbbb7d7791c6072e7b"&&!b.IsDisposed);
            Attempt(true);Console.WriteLine("Feint diagnostic: casts="+casts+", checks="+checks+", flags="+Flags()+", buffs="+string.Join(",",target.Buffs.Enumerable.Select(b=>b.Blueprint.AssetGuid)));Check(casts==1&&checks==1&&Flags()==1,"successful native skill branch captured once");
            Check(pendingCasts==1,"active feat owner creates one exact asynchronous context marker");
            var rt=fact.Components.Single(r=>r.SourceBlueprintComponent is ImprovedFeintWindow);var data=rt.GetData<ImprovedFeintData>();Check(data.Marks.Count==1,"success creates one owner mark");
            Before(u);Check(ArmorFlags()==1,"native first-attack flat-footed buff applied");Consume(u);Check(Flags()==1&&ArmorFlags()==0,"first attack keeps flag but removes temporary AC buff");
            Before(u);Check(ArmorFlags()==1,"native later attack gains same flat-footed effect");Consume(u);Check(Flags()==1&&ArmorFlags()==0,"later cleanup retains flag, no permanent flat-footed condition");
            Fact(other,(BlueprintFeature)c.Load(BlueprintGuid.Parse("c610310d31414edabcedf0c8a6fe32c4")));Consume(other);Check(Flags()==1,"other actor cannot consume or extend owner's feint");
            Before(other);Check(ArmorFlags()==0,"ally receives no flat-footed benefit");
            AccessTools.Method(Module("ImprovedFeintEndPatch"),"End").Invoke(null,new object[]{other});Check(Flags()==1,"other turn end isolated");
            AccessTools.Method(Module("ImprovedFeintEndPatch"),"End").Invoke(null,new object[]{u});Check(Flags()==0&&data.Marks.Count==0,"own turn end clears only marked buff");
            Attempt(false);Check(data.Marks.Count==0&&Flags()==0,"failed feint never opens window");
            Attempt(true);var saved=JsonConvert.DeserializeObject<ImprovedFeintData>(JsonConvert.SerializeObject(data));Check(saved.Marks.Count==1&&saved.Marks[0].BuffId==data.Marks[0].BuffId,"save state preserves exact buff identity");
            now=TimeSpan.FromSeconds(6);using(rt.RequestEventContext())((ImprovedFeintWindow)rt.SourceBlueprintComponent).OnNewRound();Check(Flags()==0&&data.Marks.Count==0,"real-time six-second expiry");
            now=TimeSpan.FromSeconds(10);attack.IsFullAttack=false;Attempt(true);Check(Flags()==1&&data.Marks.Count==0,"non-full attack does not extend native result");Consume(u);Check(Flags()==0,"ordinary native result consumed normally");
            attack.IsFullAttack=true;Set(fact,"IsTurnedOn",false);var pendingBefore=pendingCasts;Attempt(true);Check(data.Marks.Count==0&&pendingCasts==pendingBefore,"inactive feat bypasses asynchronous tracking and keeps native duration");Consume(u);Check(Flags()==0,"inactive feat result consumed normally");Set(fact,"IsTurnedOn",true);
            var otherMode=Psychic034Tests.Fact(other,mode,other,12);var otherWeapon=(Kingmaker.Items.ItemEntityWeapon)Helper("Weapon",other,WeaponCategory.Shortsword,"01422otherBlade");Helper("Equip",other,otherWeapon);
            var otherAttack=(RuleAttackWithWeapon)Helper("Attack",other,target,otherWeapon,false);otherAttack.IsFullAttack=true;otherAttack.IsFirstAttack=true;
            pendingBefore=pendingCasts;var castsBefore=casts;Queue(otherMode.MaybeContext,otherAttack);Check(ScopeClosed()&&pendingCasts==pendingBefore&&casts==castsBefore+1,"nonowner executes original native cast without a CotW pending marker");Deliver();Consume(other);Check(Flags()==0,"nonowner successful feint retains original single-use duration");
            var ordinary=u.Facts.List.First(f=>f.Blueprint.AssetGuid.ToString()=="c610310d31414edabcedf0c8a6fe32c4");pendingBefore=pendingCasts;
            Queue(ordinary.MaybeContext,attack);Deliver();Check(pendingCasts==pendingBefore&&data.Marks.Count==0,"same Feint cast outside native Two-Weapon mode does not upgrade");Consume(u);Check(Flags()==0,"ordinary action result consumes normally");
            throwOnCast=true;bool threw=false;try{Queue(modeFact.MaybeContext,attack);}catch(InvalidOperationException ex){threw=ex.Message=="Feint cast boundary failure";}finally{throwOnCast=false;}
            Check(threw&&ScopeClosed(),"native cast exception propagates with both scope fields restored");
            Queue(modeFact.MaybeContext,attack);
            AccessTools.Method(Module("ImprovedFeintEndPatch"),"End").Invoke(null,new object[]{u});foreach(var a in callbacks.ToArray())a();callbacks.Clear();Check(data.Marks.Count==0,"delayed delivery after own turn end cannot extend effect");Consume(u);Check(Flags()==0,"late native result remains normally consumable");
            Check(ScopeClosed(),"cast scope restored");
        }finally{h.UnpatchAll(h.Id);Set(Game.Instance,"TimeController",oldTime);callbacks.Clear();throwOnCast=false;}
        PsychicExpansionTests.Inventory["feint01421Assertions"]=n;Console.WriteLine("PASS: "+n+" native improved Two-Weapon Feint contracts.");return n;
    }
}
