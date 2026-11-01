using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.Utility;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UI.UnitSettings;
using Kingmaker.PubSubSystem;
using Kingmaker;
using Kingmaker.Controllers;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static UnitCommand feedbackCommand;
    static void FeedbackRun(UnitCommand __0){feedbackCommand=__0;}
    static bool FeedbackUiOnly()=>false;
    static void FeedbackLegacySourceKeys055(BlueprintsCache c,JObject observations)
    {
        var blueprint=A<BlueprintFeature>(c,"MindbladeDualWeapons01454");var published=blueprint.ComponentsArray;var granters=published.OfType<Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel>().OrderBy(x=>!x.BeforeThisLevel).ToArray();var legacyKeys=new[]{"CotW025AddFeatureOnClassLevel","CotW025AddFeatureOnClassLevel1"};observations["legacySourceKeys"]=new JObject{["expected"]=new JArray(legacyKeys),["actual"]=new JArray(granters.Select(x=>x.name))};
        var output=Environment.GetEnvironmentVariable("COTW_01455_OBSERVATION");if(!string.IsNullOrEmpty(output))File.WriteAllText(output,observations.ToString());Check(granters.Length==2&&granters.Select(x=>x.name).SequenceEqual(legacyKeys),"native serialized Dual Weapons source names match actual054 runtime keys");
        var rebind=new JArray();observations["nativeSavedGrantDataRebind"]=rebind;
        foreach(int level in new[]{7,12}){
            var u=EquipmentUnit(c,level);u.Progression.Classes[0].AddArchetype(A<BlueprintArchetype>(c,"MindbladeArchetype01454"));Grant(u,c,"MindbladePsychicPool01454");for(int i=1;i<=4;i++)Grant(u,c,"MindbladeEnhancement"+i+"01454");Feature fact=null;
            try{
                blueprint.ComponentsArray=published.Select(source=>{
                    if(!(source is Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel original))return source;
                    var native=new Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel{name=original.BeforeThisLevel?legacyKeys[0]:legacyKeys[1],Level=original.Level,BeforeThisLevel=original.BeforeThisLevel,OwnerBlueprint=blueprint};
                    foreach(string field in new[]{"m_Class","m_Feature","m_AdditionalClasses","m_Archetypes"})AccessTools.Field(typeof(Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel),field).SetValue(native,AccessTools.Field(typeof(Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel),field).GetValue(original));return (BlueprintComponent)native;
                }).ToArray();fact=Grant(u,blueprint);
            }finally{blueprint.ComponentsArray=published;}
            var runtimes=fact.Components.Where(r=>r.SourceBlueprintComponent is Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel).ToArray();var data=runtimes.Select(r=>r.GetData<Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevelData>()).ToArray();var applied=data.Select(d=>d.AppliedFact).ToArray();var mode=u.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.Buff.GetComponent<MindbladeDualMode01454>()!=null);ToggleOn(mode);var ids=u.Facts.List.Select(f=>f.UniqueId).OrderBy(x=>x).ToArray();
            // Serialized runtimes retain their old source-name key and native
            // AddFeatureOnClassLevelData, not a blueprint-component pointer.
            foreach(var runtime in runtimes)Set(runtime,"SourceBlueprintComponent",null);Set(fact,"m_BlueprintComponents",null);Set(fact,"IsPostLoadExecuted",false);fact.PostLoad();fact.ApplyPostLoadFixes();
            bool same=runtimes.Select((runtime,i)=>fact.Components.Contains(runtime)&&ReferenceEquals(runtime.GetData<Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevelData>(),data[i])&&ReferenceEquals(data[i].AppliedFact,applied[i])&&runtime.SourceBlueprintComponent!=null&&runtime.SourceBlueprintComponent.name==legacyKeys[i]).All(x=>x);
            Check(same,"actual native EntityFact.PostLoad rebinds old source keys and preserves both native data records/owned-child references at"+level);Check(ids.SequenceEqual(u.Facts.List.Select(f=>f.UniqueId).OrderBy(x=>x))&&mode.IsOn&&ReferenceEquals(mode,u.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.Buff.GetComponent<MindbladeDualMode01454>()!=null)),"native saved healthy grant rebind keeps exact facts and active toggle without duplication at"+level);
            rebind.Add(new JObject{["level"]=level,["nativePostLoadDataIdentityPreserved"]=same,["ownedChildReferencesPreserved"]=true,["activeModeAndExactFactsPreserved"]=true});fact.Manager.Remove(fact);Check(!u.ActivatableAbilities.Enumerable.Any(a=>a.Blueprint.Buff.GetComponent<MindbladeDualMode01454>()!=null),"native parent removal still owns/removes rebound child and toggle at"+level);
        }
    }
    static void FeedbackManifestMenus055(BlueprintsCache c)
    {
        var u=EquipmentUnit(c,12);u.Progression.Classes[0].AddArchetype(A<BlueprintArchetype>(c,"MindbladeArchetype01454"));Grant(u,c,"MindbladePsychicPool01454");Grant(u,c,"MindbladeDualWeapons01454");for(int i=1;i<=4;i++)Grant(u,c,"MindbladeEnhancement"+i+"01454");u.Descriptor.Proficiencies.Add(WeaponCategory.Dagger);u.Descriptor.Proficiencies.Add(WeaponCategory.Longsword);
        MechanicActionBarSlotSpontaneusConvertedSpell[] Menu(int group){var source=u.Abilities.Enumerable.Single(a=>a.Blueprint.AssetGuid==Id("MindbladeManifestGroup"+group+"01454"));return new MechanicActionBarSlotAbility{Ability=source.Data,Unit=u}.GetConvertedAbilityData().GetMechanicSlots(u).OfType<MechanicActionBarSlotSpontaneusConvertedSpell>().ToArray();}
        Check(Menu(5).Length==0,"native parent popup correctly hides every double weapon without its actual exotic proficiency");u.Descriptor.Proficiencies.Add(WeaponCategory.DoubleSword);var doubleMenu=Menu(5);Check(doubleMenu.Length==1&&doubleMenu[0].Spell.Blueprint.GetComponent<MindbladeManifestCost01454>().Weapon.Get().Category==WeaponCategory.DoubleSword,"native parent popup shows exactly the proficient double sword, no unrelated exotic weapons");
        foreach(var pair in new[]{Tuple.Create(3,WeaponCategory.Dagger),Tuple.Create(4,WeaponCategory.Longsword),Tuple.Create(5,WeaponCategory.DoubleSword)}){var ui=Menu(pair.Item1).Single(s=>s.Spell.Blueprint.GetComponent<MindbladeManifestCost01454>().Weapon.Get().Category==pair.Item2);Check(!ui.IsPossibleActive(null)&&!ui.Spell.Blueprint.GetComponent<MindbladeManifestCost01454>().IsCasterRestrictionPassed(u),"native dual-form popup remains unavailable while Dual Weapons is off: "+pair.Item2);}
        var mode=u.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.Buff.GetComponent<MindbladeDualMode01454>()!=null);Check(mode.Blueprint.WeightInGroup==2&&!mode.Hidden,"native level12 parent grants one visible two-point group mode");ToggleOn(mode);
        foreach(var pair in new[]{Tuple.Create(3,WeaponCategory.Dagger),Tuple.Create(4,WeaponCategory.Longsword),Tuple.Create(5,WeaponCategory.DoubleSword)}){var ui=Menu(pair.Item1).Single(s=>s.Spell.Blueprint.GetComponent<MindbladeManifestCost01454>().Weapon.Get().Category==pair.Item2);Check(ui.IsPossibleActive(null)&&ui.Spell.Fact?.Active==true,"actual native popup enables owned dual-form leaf with correct mode/proficiency: "+pair.Item2);int before=u.Resources.GetResourceAmount(N<BlueprintAbilityResource>(c,"effc3e386331f864e9e06d19dc218b37"));var buff=Manifest(u,ui.Spell);var data=ManifestData(buff);Check(data.Created&&data.Weapons.Length==(pair.Item1==5?1:2)&&data.Weapons[0].Blueprint.Category==pair.Item2&&(pair.Item1!=5||data.Weapons[0].Second!=null),"native level12 manifestation creates actual paired/double equipment: "+pair.Item2);buff.Remove();Check(u.Body.PrimaryHand.MaybeItem==null&&u.Body.SecondaryHand.MaybeItem==null&&u.Resources.GetResourceAmount(N<BlueprintAbilityResource>(c,"effc3e386331f864e9e06d19dc218b37"))==before,"native dismissal restores empty hands and reserved points exactly: "+pair.Item2);}
        ToggleOff(mode);Check(Form(u,WeaponCategory.Dagger).Blueprint.GetComponent<MindbladeManifestCost01454>().IsCasterRestrictionPassed(u),"returning to single mode retains already-accepted one-weapon manifestation");
    }
    static void FeedbackIcons055(BlueprintsCache parent,Harmony h)
    {
        EquipmentPatches(h);var c=Fresh(parent);Register(c,"Mindblade01454");var u=EquipmentUnit(c,12);var results=new JArray();bool repaired=Main.GetType("CallOfTheWild.Wrath.Prototype.MindbladeVariantIcons01455")!=null;
        foreach(var category in new[]{WeaponCategory.Dagger,WeaponCategory.Longsword,WeaponCategory.DoubleSword}){
            var leaf=Form(u,category,category!=WeaponCategory.DoubleSword);var root=leaf.Blueprint.Parent;var iconField=AccessTools.Field(typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact),"m_Icon");var priorParentIcon=iconField.GetValue(root);var priorLeafIcon=iconField.GetValue(leaf.Blueprint);var parentIcon=(UnityEngine.Sprite)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnityEngine.Sprite));var leafIcon=(UnityEngine.Sprite)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnityEngine.Sprite));Set(root,"m_Icon",parentIcon);Set(leaf.Blueprint,"m_Icon",leafIcon);
            try{var converted=new AbilityData(new AbilityData(root,u.Descriptor),leaf.Blueprint);var ui=new SlotConversion(new List<AbilityData>{converted}).GetMechanicSlots(u).OfType<MechanicActionBarSlotSpontaneusConvertedSpell>().Single();var icon=ui.GetIcon();Check(ReferenceEquals(icon,repaired?leafIcon:parentIcon),"actual native converted popup "+(repaired?"uses selected weapon icon ":"old control reproduces parent icon ")+category);results.Add(new JObject{["category"]=category.ToString(),["selectedLeafIcon"]=ReferenceEquals(icon,leafIcon),["parentIcon"]=ReferenceEquals(icon,parentIcon)});}finally{iconField.SetValue(root,priorParentIcon);iconField.SetValue(leaf.Blueprint,priorLeafIcon);}
        }
        var source=N<BlueprintAbility>(c,"4ac47ddb9fa1eaf43a1b6809980cfbd2");var other=N<BlueprintAbility>(c,"ab395d2335d3f384e99dddee8562978f");var originalIcon=(UnityEngine.Sprite)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnityEngine.Sprite));Set(source,"m_Icon",originalIcon);Set(other,"m_Icon",(UnityEngine.Sprite)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnityEngine.Sprite)));var ordinary=new AbilityData(new AbilityData(source,u.Descriptor),other);var normalUi=new SlotConversion(new List<AbilityData>{ordinary}).GetMechanicSlots(u).OfType<MechanicActionBarSlotSpontaneusConvertedSpell>().Single();Check(ReferenceEquals(normalUi.GetIcon(),originalIcon),"foreign ordinary conversion retains exact native parent-icon behavior");
        File.WriteAllText(Environment.GetEnvironmentVariable("COTW_01455_OBSERVATION"),new JObject{["nativePopupIcons"]=results,["foreignConversionUnchanged"]=true,["renderingBoundary"]="Distinct controlled Sprite objects in exact blueprint icon fields; native popup branching executes, no pixel-rendering claim."}.ToString());
    }
    static AbilityExecutionContext pendingStore;
    static bool storeInitRejected;
    static bool FeedbackInterrupt(UnitCommand __instance){if(!(__instance is UnitUseAbility cast)||cast.Ability.Blueprint.GetComponent<SkirnirStoreSpell01454>()==null)return true;storeInitRejected=true;return false;}
    static bool FeedbackSkipAnimation(UnitCommand __instance,ref bool __result){if(!(__instance is UnitUseAbility cast)||cast.Ability.Blueprint.GetComponent<SkirnirStoreSpell01454>()==null)return true;__result=true;return false;}
    static bool FeedbackCastScene(AbilityExecutionContext context,ref AbilityExecutionProcess __result){pendingStore=context;__result=new AbilityExecutionProcess(context);return false;}
    static bool FeedbackInGame(ref bool __result){__result=true;return false;}
    static bool FeedbackSelfApproach(AbilityData __instance,ref float __result){if(__instance.Blueprint.GetComponent<SkirnirStoreSpell01454>()==null)return true;__result=0;return false;}
    static void FeedbackStorageSpend(AbilityData ability)
    {
        // CLR cannot JIT native SpendFromSpellbook's unused Unity path. Its
        // first branch returns on a null book. Refuse every other input, keep
        // native material and resource spending, and never stand in for the
        // selected preparation's real SpellSlot.Spend inside the effect.
        if(ability.Blueprint.GetComponent<SkirnirStoreSpell01454>()==null||ability.Spellbook!=null||ability.SourceItem!=null)throw new Exception("055 no-book payment boundary received a book/item/foreign ability.");
        ability.SpendMaterialComponent();AbilityCastRateUtils.SpendResources(ability);
    }
    static IEnumerable<CodeInstruction> FeedbackCommandPayment(IEnumerable<CodeInstruction> input)
    {
        int count=0;foreach(var code in input){if(code.operand is MethodInfo m&&m.DeclaringType==typeof(AbilityData)&&m.Name=="Spend"){code.opcode=OpCodes.Call;code.operand=AccessTools.Method(typeof(MagusBatch01454Tests),nameof(FeedbackStorageSpend));count++;}yield return code;}
        if(count!=1)throw new Exception("055 native OnAction resource payment call changed; expected exactly one.");
    }
    static void FeedbackShield055(BlueprintsCache parent,Harmony h)
    {
        EquipmentPatches(h);var c=Fresh(parent);Register(c,"Skirnir01454");var u=EquipmentUnit(c,8);var enemy=EquipmentUnit(c,8);var root=A<BlueprintArchetype>(c,"SkirnirArchetype01454");u.Progression.Classes[0].AddArchetype(root);Grant(u,c,"SkirnirShieldSpellCombat01454");
        var shield=root.StartingItems.OfType<Kingmaker.Blueprints.Items.Shields.BlueprintItemShield>().Single().CreateEntity<ItemEntityShield>();using(ContextData<ItemEntity.CanBeEquippedForce>.Request())Check(u.Body.SecondaryHand.InsertItem(shield),"native plain heavy shield equipped at8 for actual AC probe");
        var rows=new JArray();var current=AccessTools.Field(typeof(BardMasterpieces01435Tests),"current");var old=current.GetValue(null);
        foreach(bool improved in new[]{false,true}){
            if(improved)H("Fact",u,N<BlueprintFeature>(c,"121811173a614534e8720d7550aae253"));
            var native=Add(u,N<BlueprintBuff>(c,"7b4cf64d3a49e3d45b1dbd2385f4eb6d"));var ours=u.Buffs.Enumerable.SingleOrDefault(b=>b.Blueprint.AssetGuid==Id("SkirnirCombatPenalty01454"));
            var ac=new RuleCalculateAC(enemy,u,AttackType.Melee){ForceFlatFooted=true};var events=new RulebookEventContext();events.PushEvent(ac);current.SetValue(null,events);
            var row=new JObject{["improvedShieldBash"]=improved,["sheetAC"]=u.Stats.AC.ModifiedValue,["shieldModifiers"]=new JArray(u.Stats.AC.Modifiers.Where(m=>m.ModDescriptor==ModifierDescriptor.Shield||m.ModDescriptor==ModifierDescriptor.ShieldEnhancement||m.ModDescriptor==ModifierDescriptor.ShieldFocus).Select(m=>new JObject{["descriptor"]=m.ModDescriptor.ToString(),["value"]=m.ModValue})),["penaltyPresent"]=ours!=null};rows.Add(row);
            try{RulebookEventBus.OnEventAboutToTrigger(ac);row["penalty"]=ac.AllBonuses.Where(m=>m.Fact==ours).Sum(m=>m.Value);ac.OnTrigger(events);RulebookEventBus.OnEventDidTrigger(ac);row["actualAC"]=ac.Result;}catch(Exception e){row["error"]=e.ToString();}finally{events.PopEvent(ac);current.SetValue(null,old);native.Remove();ours?.Remove();}
        }
        File.WriteAllText(Environment.GetEnvironmentVariable("COTW_01455_OBSERVATION"),new JObject{["shield"]=rows}.ToString());Console.WriteLine("055_SHIELD="+rows.ToString(Newtonsoft.Json.Formatting.None));
        foreach(JObject row in rows){Check(row["error"]==null,"native equipped-shield AC calculation completes, Improved Shield Bash="+row["improvedShieldBash"]);Check((bool)row["penaltyPresent"]==!ShieldRules056&&(int)row["sheetAC"]==12&&(int)row["penalty"]==(ShieldRules056?0:-2)&&(int)row["actualAC"]==(ShieldRules056?12:10),"native level8 plain-heavy-shield Spell Combat removes two AC in actual attack rule while sheet stays12, Improved Shield Bash="+row["improvedShieldBash"]);}
    }
    static void FeedbackStore055(BlueprintsCache parent,Harmony h)
    {
        EquipmentPatches(h);var c=Fresh(parent);Register(c,"Skirnir01454");var u=EquipmentUnit(c,8);var root=A<BlueprintArchetype>(c,"SkirnirArchetype01454");u.Progression.Classes[0].AddArchetype(root);
        var pool=N<BlueprintAbilityResource>(c,"effc3e386331f864e9e06d19dc218b37");u.Resources.Add(pool,true);
        var shield=root.StartingItems.OfType<Kingmaker.Blueprints.Items.Shields.BlueprintItemShield>().Single().CreateEntity<ItemEntityShield>();using(ContextData<ItemEntity.CanBeEquippedForce>.Request())Check(u.Body.SecondaryHand.InsertItem(shield),"native heavy shield is equipped for level8 feedback reproduction");
        var book=new Spellbook(u.Descriptor,root.ReplaceSpellbook);Set(book,"m_BaseLevelInternal",8);Set(u.Descriptor,"m_Spellbooks",new Dictionary<BlueprintSpellbook,Spellbook>{{root.ReplaceSpellbook,book}});Grant(u,c,"SkirnirSpellshield01454");
        h.Patch(AccessTools.Method(typeof(UnitCommands),"Run",new[]{typeof(UnitCommand)}),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(FeedbackRun)){priority=Priority.First});
        h.Patch(AccessTools.Method(typeof(MechanicActionBarSlot),"PlaySound"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(FeedbackUiOnly)));
        h.Patch(AccessTools.Method(typeof(MechanicActionBarSlot),"TryShowWarning"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(FeedbackUiOnly)));
        h.Patch(AccessTools.Method(typeof(UnitUseAbility),"OnAction"),transpiler:new HarmonyMethod(typeof(BardMasterpieces01435Tests),"RuleBoundary"));
        h.Patch(AccessTools.Method(typeof(UnitUseAbility),"OnAction"),transpiler:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(FeedbackCommandPayment)));
        h.Patch(AccessTools.Method(typeof(RuleCastSpell),"OnTrigger"),transpiler:new HarmonyMethod(typeof(BardMasterpieces01435Tests),"RuleBoundary"));
        h.Patch(AccessTools.Method(typeof(AbilityExecutionProcess),"DoApplyEffect"),transpiler:new HarmonyMethod(typeof(BardMasterpieces01435Tests),"RuleBoundary"));
        h.Patch(AccessTools.Method(typeof(AbilityData),"Cast",new[]{typeof(AbilityExecutionContext)}),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(FeedbackCastScene)));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.EntitySystem.EntityDataBase),"IsInGame"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(FeedbackInGame)));
        h.Patch(AccessTools.Method(typeof(AbilityData),"GetApproachDistance"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(FeedbackSelfApproach)));
        h.Patch(AccessTools.Method(typeof(UnitCommand),"Interrupt",new[]{typeof(bool)}),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(FeedbackInterrupt)));
        h.Patch(AccessTools.PropertyGetter(typeof(UnitCommand),"ShouldSkipAnimation"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(FeedbackSkipAnimation)));
        foreach(var log in typeof(Owlcat.Runtime.Core.Logging.LogChannel).GetMethods().Where(m=>m.Name=="Error"))h.Patch(log,prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(FeedbackUiOnly)));
        foreach(string name in new[]{"SpawnInterruptFx","SpawnCastFx"})h.Patch(AccessTools.Method(typeof(UnitUseAbility),name),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(FeedbackUiOnly)));
        var rows=new JArray();
        foreach(var entry in new[]{new[]{"4ac47ddb9fa1eaf43a1b6809980cfbd2","1"},new[]{"ab395d2335d3f384e99dddee8562978f","1"},new[]{"b6010dda6333bcf4093ce20f0063cd41","2"}}){
            int level=int.Parse(entry[1]);var spell=N<BlueprintAbility>(c,entry[0]);var known=book.AddKnown(level,spell);var slot=new SpellSlot(level,SpellSlotType.Common,0,book){SpellShell=known,Available=true};((List<SpellSlot>[])AccessTools.Field(typeof(Spellbook),"m_MemorizedSpells").GetValue(book))[level]=new List<SpellSlot>{slot};
            var variants=slot.SpellShell.GetConversions().Where(a=>a.Blueprint.GetComponent<SkirnirStoreSpell01454>()!=null).ToArray();var row=new JObject{["spell"]=spell.name,["circle"]=level,["conversions"]=variants.Length};rows.Add(row);
            if(variants.Length==0)continue;var a=variants[0];row["hasParams"]=a.HasRequiredParams;row["effectRestriction"]=a.Blueprint.GetComponent<SkirnirStoreSpell01454>().IsAbilityRestrictionPassed(a);row["nativeAvailable"]=a.IsAvailable;row["targetAnchor"]=a.TargetAnchor.ToString();row["slotAvailable"]=slot.Available;
            var ui=new SlotConversion(new List<AbilityData>{a}).GetMechanicSlots(u).OfType<MechanicActionBarSlotSpontaneusConvertedSpell>().Single();feedbackCommand=null;row["nativeConversionPreservesAbility"]=ReferenceEquals(ui.Spell,a);
            try{row["nativeUiCanActivate"]=ui.IsPossibleActive(null);ui.OnClick();row["nativeUiCommand"]=feedbackCommand?.GetType().FullName;if(feedbackCommand is UnitUseAbility cast){row["commandAbilitySame"]=ReferenceEquals(cast.Ability,a);row["commandParamsSame"]=ReferenceEquals(cast.Ability.ParamSpellSlot,slot);row["commandCanTargetOwner"]=cast.Ability.CanTarget(new TargetWrapper(u));
                row["ownedAbilityFact"]=cast.Ability.Fact?.Active==true;storeInitRejected=false;int initPool=u.Resources.GetResourceAmount(pool);cast.Init(u);row["nativeInitRejected"]=storeInitRejected;if(storeInitRejected){row["rejectionPreservesSlotAndPool"]=slot.Available&&u.Resources.GetResourceAmount(pool)==initPool;continue;}pendingStore=null;int before=u.Resources.GetResourceAmount(pool);row["nativeOnActionResult"]=AccessTools.Method(typeof(UnitUseAbility),"OnAction").Invoke(cast,null).ToString();row["paid"]=before-u.Resources.GetResourceAmount(pool);row["scheduled"]=pendingStore!=null;
                if(pendingStore!=null){row["nativeContextParamsSame"]=ReferenceEquals(pendingStore.Ability.ParamSpellSlot,slot);pendingStore.DisableFx=true;Set(pendingStore,"FxSpawners",new List<Kingmaker.UnitLogic.Abilities.Components.Base.AbilitySpawnFx>());AccessTools.Method(typeof(AbilityExecutionProcess),"DoApplyEffect").Invoke(null,new object[]{pendingStore,new TargetWrapper(u),a.Blueprint.GetComponent<SkirnirStoreSpell01454>()});}var fact=u.Facts.List.Single(f=>f.Blueprint.GetComponent<SkirnirStorage01454>()!=null);var state=fact.Components.Single(r=>r.SourceBlueprintComponent is SkirnirStorage01454).GetData<SkirnirStorageData01454>();row["stored"]=state.Spell?.Blueprint==spell;row["slotConsumed"]=!slot.Available;row["storedBookSame"]=ReferenceEquals(state.Spell?.Spellbook,book);row["storedShieldSame"]=ReferenceEquals(state.Shield,shield);row["storedCasterLevel"]=state.Params?.CasterLevel;
                var stored=state.Spell;int paidPool=u.Resources.GetResourceAmount(pool);AccessTools.Method(typeof(AbilityExecutionProcess),"DoApplyEffect").Invoke(null,new object[]{pendingStore,new TargetWrapper(u),a.Blueprint.GetComponent<SkirnirStoreSpell01454>()});Check(ReferenceEquals(stored,state.Spell)&&!slot.Available&&u.Resources.GetResourceAmount(pool)==paidPool,"native effect replay on consumed preparation cannot replace storage or spend/mint points: "+spell.name);feedbackCommand=null;ui.OnClick();Check(feedbackCommand==null&&!ui.IsPossibleActive(null),"native converted UI rejects already consumed preparation: "+spell.name);
            }}catch(Exception e){row["uiBoundaryError"]=e.ToString();}
        }
        var lastSlot=book.GetMemorizedSpellSlots(2).Single();lastSlot.Available=true;var lastAbility=lastSlot.SpellShell.GetConversions().Single(a=>a.Blueprint.GetComponent<SkirnirStoreSpell01454>()!=null);var lastUi=new SlotConversion(new List<AbilityData>{lastAbility}).GetMechanicSlots(u).OfType<MechanicActionBarSlotSpontaneusConvertedSpell>().Single();int resources=u.Resources.GetResourceAmount(pool);
        using(ContextData<ItemSlot.IgnoreLock>.Request())u.Body.SecondaryHand.RemoveItem();feedbackCommand=null;lastUi.OnClick();bool noShield=!lastUi.IsPossibleActive(null)&&feedbackCommand==null&&lastSlot.Available&&u.Resources.GetResourceAmount(pool)==resources;Check(noShield,"native cached storage popup rejects missing shield without spending preparation or points");
        using(ContextData<ItemEntity.CanBeEquippedForce>.Request())Check(u.Body.SecondaryHand.InsertItem(shield),"native feedback shield restored for empty-pool negative control");u.Resources.Spend(pool,resources);feedbackCommand=null;lastUi.OnClick();bool emptyPool=!lastUi.IsPossibleActive(null)&&feedbackCommand==null&&lastSlot.Available&&u.Resources.GetResourceAmount(pool)==0;Check(emptyPool,"native storage popup rejects insufficient daily pool without spending preparation");
        var output=Environment.GetEnvironmentVariable("COTW_01455_OBSERVATION");if(!string.IsNullOrEmpty(output))File.WriteAllText(output,new JObject{["storageUiProbe"]=rows,["noShieldGuard"]=noShield,["emptyPoolGuard"]=emptyPool}.ToString());Console.WriteLine("055_STORAGE_UI_OBSERVATION="+rows.ToString(Newtonsoft.Json.Formatting.None));
        Check(rows.Count==3,"exact native Magic Missile control and reported Shocking Grasp/Frigid Touch slot conversions observed");
        bool repaired=Main.GetType("CallOfTheWild.Wrath.Prototype.MindbladeModeGrant01455")!=null;
        foreach(JObject row in rows){Check(row["uiBoundaryError"]==null,"native storage UI/init/action/effect path has no fixture exception: "+row["spell"]);Check((bool)row["nativeInitRejected"]==!repaired&&((bool)row["ownedAbilityFact"])==repaired,(repaired?"native Init accepts actual owned storage source: ":"old054 native Init rejects conversion lacking owned source: ")+row["spell"]);if(repaired)Check((string)row["nativeOnActionResult"]=="Success"&&(bool)row["scheduled"]&&(bool)row["stored"]&&(bool)row["slotConsumed"]&&(bool)row["storedBookSame"]&&(bool)row["storedShieldSame"]&&(int)row["paid"]==(int)row["circle"]&&(int)row["storedCasterLevel"]==8,"actual native action and DoApplyEffect consume exact preparation/circle cost and bind spell params/book/shield: "+row["spell"]);else Check((bool)row["rejectionPreservesSlotAndPool"],"old054 native Init rejection leaves exact preparation and daily points untouched: "+row["spell"]);}
    }
    static void Feedback055(BlueprintsCache parent,Harmony h)
    {
        EquipmentPatches(h);
        bool repaired=Main.GetType("CallOfTheWild.Wrath.Prototype.MindbladeModeGrant01455")!=null;
        var observations=new JObject();var problems=new List<string>();
        var c=Fresh(parent);Register(c,"Mindblade01454");
        FeedbackLegacySourceKeys055(c,observations);
        var list=A<BlueprintSpellList>(c,"MindbladePsychicSpellList01454");int before=list.SpellsByLevel.Length;
        Call(T("SpellListCompatibility"),"Reconcile",c);
        string error=null;try{Register(c,"Mindblade01454");}catch(Exception e){error=e.ToString();}
        observations["mindbladeAfterNativeListReconciliation"]=new JObject{["beforeRows"]=before,["afterRows"]=list.SpellsByLevel.Length,["error"]=error};
        Check(repaired?error==null&&before==11&&list.SpellsByLevel.Length==11:error!=null&&error.Contains("MindbladePsychicSpellList01454")&&before==10&&list.SpellsByLevel.Length==11,repaired?"canonical native eleven-row access list survives actual reconciliation and repeat publication":"old054 installation reproduces ten-to-eleven-row receipt failure through actual reconciliation");
        c=Fresh(parent);Register(c,"Skirnir01454");var root=A<BlueprintArchetype>(c,"SkirnirArchetype01454");
        int adds=root.AddFeatures.Length,removes=root.RemoveFeatures.Length;root.GetAddEntry(2);root.GetRemoveEntry(20);
        error=null;try{Register(c,"Skirnir01454");}catch(Exception e){error=e.ToString();}
        observations["skirnirNativeArchetypeEntryAccess"]=new JObject{["beforeAddRows"]=adds,["afterAddRows"]=root.AddFeatures.Length,["beforeRemoveRows"]=removes,["afterRemoveRows"]=root.RemoveFeatures.Length,["error"]=error};
        if(error!=null)problems.Add("Skirnir publication must survive native empty level-entry access");
        c=Fresh(parent);Register(c,"Mindblade01454");var grants=new JArray();
        foreach(int level in new[]{7,11,12,13}){
            var u=EquipmentUnit(c,level);u.Progression.Classes[0].AddArchetype(A<BlueprintArchetype>(c,"MindbladeArchetype01454"));
            Grant(u,c,"MindbladePsychicPool01454");Grant(u,c,"MindbladeDualWeapons01454");
            var toggles=u.ActivatableAbilities.Enumerable.Where(a=>a.Blueprint.name.Contains("MindbladeDualMode")).ToArray();
            grants.Add(new JObject{["level"]=level,["toggles"]=new JArray(toggles.Select(a=>new JObject{["name"]=a.Blueprint.name,["hidden"]=a.Hidden,["weight"]=a.Blueprint.WeightInGroup,["restrictionAvailable"]=a.IsAvailableByRestrictions})),["facts"]=new JArray(u.Facts.List.Where(f=>f.Blueprint.name.Contains("MindbladeDual")).Select(f=>f.Blueprint.name))});
            if(toggles.Count(a=>!a.Hidden)!=1)problems.Add("Exactly one visible native-granted Dual Weapons toggle at level "+level);
            if(level==7){Set(u.Progression.Classes[0],"Level",12);EventBus.RaiseEvent<IOwnerGainLevelHandler>(u,handler=>handler.HandleUnitGainLevel());var after=u.ActivatableAbilities.Enumerable.Where(a=>a.Blueprint.name.Contains("MindbladeDualMode")).ToArray();observations["level7To12NativeEvent"]=new JArray(after.Select(a=>new JObject{["name"]=a.Blueprint.name,["hidden"]=a.Hidden,["weight"]=a.Blueprint.WeightInGroup}));if(after.Count(a=>!a.Hidden)!=1||after.Single(a=>!a.Hidden).Blueprint.WeightInGroup!=2)problems.Add("Native level-gain dispatch must replace weight1 mode with exactly one visible weight2 mode at12");}
        }
        observations["nativeDualModeGrants"]=grants;
        var missing=new JArray();c=Fresh(parent);Register(c,"Mindblade01454");
        foreach(int level in new[]{7,12}){
            var u=EquipmentUnit(c,level);u.Progression.Classes[0].AddArchetype(A<BlueprintArchetype>(c,"MindbladeArchetype01454"));Grant(u,c,"MindbladePsychicPool01454");var fact=Grant(u,c,"MindbladeDualWeapons01454");
            var children=fact.Components.Where(r=>r.SourceBlueprintComponent is Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel).ToArray();
            foreach(var runtime in children){var data=runtime.GetData<Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevelData>();if(data.AppliedFact!=null){data.AppliedFact.Manager.Remove(data.AppliedFact);data.AppliedFact=null;}}
            foreach(var runtime in children)runtime.PostLoad();foreach(var runtime in children)runtime.ApplyPostLoadFixes();
            var toggles=u.ActivatableAbilities.Enumerable.Where(a=>a.Blueprint.Buff.GetComponent<MindbladeDualMode01454>()!=null).ToArray();missing.Add(new JObject{["level"]=level,["restoredToggles"]=toggles.Length});
            Check(repaired?toggles.Length==1&&toggles[0].Blueprint.WeightInGroup==(level<12?1:2)&&!toggles[0].Hidden&&!toggles[0].IsOn:toggles.Length==0,(repaired?"native post-load restoration recovers exactly one visible inactive correct-weight toggle at":"old054 native post-load leaves missing saved child absent at")+level);
            foreach(var runtime in children)runtime.ApplyPostLoadFixes();Check(u.ActivatableAbilities.Enumerable.Count(a=>a.Blueprint.Buff.GetComponent<MindbladeDualMode01454>()!=null)==(repaired?1:0),"repeated native fix hook is idempotent at"+level);
        }
        observations["nativeMissingGrantPostLoad"]=missing;
        FeedbackManifestMenus055(c);
        Serialization(c);
        var output=Environment.GetEnvironmentVariable("COTW_01455_OBSERVATION");
        observations["problems"]=new JArray(problems);if(!string.IsNullOrEmpty(output))File.WriteAllText(output,observations.ToString());
        Console.WriteLine("055_NATIVE_OBSERVATION="+observations.ToString(Newtonsoft.Json.Formatting.None));
        foreach(var problem in problems)Check(false,problem);
        Check(true,"actual native registration and class-level grant paths retain their resources and exactly one visible mode");
    }
}
