using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.RuleSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static bool WhipIcon(ref UnityEngine.Sprite __result){__result=(UnityEngine.Sprite)FormatterServices.GetUninitializedObject(typeof(UnityEngine.Sprite));return false;}
    static void WhipProvider(BlueprintsCache parent,Harmony h)
    {
        Console.WriteLine("054 Hurricane: actual latest DarkCodex CreateWhipInfusion provider");EquipmentPatches(h);var c=Fresh(parent);
        Check(!(bool)Call(T("WhipHurricane01454"),"HasDependency",c)&&A<BlueprintFeature>(c,"WhipHurricaneFeature01454")==null,"absent actual whip provider leaves Hurricane unpublished");
        string directory=Environment.GetEnvironmentVariable("COTW_01454_WHIP_ADAPTER");if(string.IsNullOrEmpty(directory))throw new Exception("Actual latest DarkCodex access adapter must be explicitly supplied.");
        var umm=Assembly.LoadFrom(Path.Combine(directory,"UnityModManager.dll"));var lib=Assembly.LoadFrom(Path.Combine(directory,"CodexLib.dll"));var dc=Assembly.LoadFrom(Path.Combine(directory,"DarkCodex.dll"));
        Check(dc.GetName().Version==new Version(1,9,6,0)&&lib.GetName().Version==new Version(1,8,3,1),"actual official latest release assembly versions");
        h.Patch(AccessTools.Method(dc.GetType("DarkCodex.Kineticist",true),"CreateWhipInfusion"),transpiler:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Labels)));
        var helper=lib.GetType("CodexLib.Helper",true);h.Patch(AccessTools.Method(helper,"StealIcon",new[]{typeof(string)}),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(WhipIcon)));
        var oldPack=Kingmaker.Localization.LocalizationManager.CurrentPack;Kingmaker.Localization.LocalizationManager.CurrentPack=new Kingmaker.Localization.LocalizationPack();
        try{
            var logType=umm.GetType("UnityModManagerNet.UnityModManager+ModEntry+ModLogger",true);var logger=Activator.CreateInstance(logType,new object[]{"054Provider"});var scope=lib.GetType("CodexLib.Scope",true);
            using((IDisposable)Activator.CreateInstance(scope,new object[]{Path.Combine(Root(),"work/batch01454/dependencies/DarkCodex/release/DarkCodex"),logger,h,false}))
                AccessTools.Method(dc.GetType("DarkCodex.Kineticist",true),"CreateWhipInfusion").Invoke(null,null);
        }finally{Kingmaker.Localization.LocalizationManager.CurrentPack=oldPack;}
        var whip=N<BlueprintFeature>(c,"00df2fabfc3f40a4aaf4a066dcd78b80");var buff=N<BlueprintBuff>(c,"26983c7ac7f1465fab8ec646f366b9f7");Check(whip!=null&&buff.GetComponents<AddKineticistBurnModifier>().Count()==1,"real provider creates the configured feat and cost component");
        var original=Definition(buff);var selection=N<BlueprintFeatureSelection>(c,"58d6f8e9eea63f6418b107ce64f315ea");var old=selection.AllFeatures.ToArray();
        var knight=N<BlueprintArchetype>(c,"7d61d9b2250260a45b18c5634524a8fb");var cls=N<BlueprintCharacterClass>(c,"42a455d9ec1ad924d889272429eb8391");var entries=knight.AddFeatures;var groups=cls.Progression.UIGroups;
        // Resolve the six actual native rows from the production enrolment;
        // their IDs are also checked against the original source in the audit.
        var source=File.ReadAllText(TestPaths.Outputs("src/WhipHurricane01454.cs"));
        var metaRows=System.Text.RegularExpressions.Regex.Matches(source,"\"([0-9a-f]{32})\"").Cast<System.Text.RegularExpressions.Match>().Select(m=>m.Groups[1].Value).Distinct().Select(id=>N<BlueprintScriptableObject>(c,id)).OfType<BlueprintBuff>().Where(b=>b.GetComponent<Kingmaker.Designers.Mechanics.Facts.AutoMetamagic>()!=null&&b.GetComponent<AddKineticistBurnModifier>()?.BurnType==KineticistBurnType.Metakinesis).ToArray();Check(metaRows.Length==6,"all six real native metakinesis input rows found");
        var metaBefore=metaRows.Select(Definition).ToArray();h.Patch(AccessTools.Method(T("MagusBatch01454"),"Publish"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(PublicationConnect)));hurricaneConnectFault=true;
        Check(Refused(()=>Register(c,"WhipHurricane01454"))&&!hurricaneConnectFault,"real Hurricane post-connect failure is rejected");Check(A<BlueprintFeature>(c,"WhipHurricaneFeature01454")==null&&old.SequenceEqual(selection.AllFeatures)&&ReferenceEquals(entries,knight.AddFeatures)&&ReferenceEquals(groups,cls.Progression.UIGroups)&&metaBefore.SequenceEqual(metaRows.Select(Definition)),"Hurricane rollback restores actual infusion, knight, UI and all six native metakinesis lists");
        var reports=new List<Tuple<string,bool,Exception>>();Call(T("BlueprintRegistration"),"RegisterOptional",c,new Action<string,bool,Exception>((n,ok,e)=>reports.Add(Tuple.Create(n,ok,e))));Check(reports.Any(r=>r.Item1=="Infusion: Whip Hurricane"&&r.Item2),"real optional dispatcher retries Hurricane after actual provider publication and rollback");var root=A<BlueprintFeature>(c,"WhipHurricaneFeature01454");Check(root!=null&&old.All(f=>selection.AllFeatures.Contains(f))&&selection.AllFeatures.Count(f=>f==root)==1,"late actual provider publication appends Hurricane once and preserves every existing infusion");
        Check(Definition(buff)==original,"Hurricane never mutates the external whip buff");Register(c,"WhipHurricane01454");Check(ReferenceEquals(root,A<BlueprintFeature>(c,"WhipHurricaneFeature01454")),"repeat real-provider registration retains exact feature identity");
        var controller=new KineticistController();EventBus.Subscribe(controller);try{WhipNative(c,h,dc,buff);}finally{EventBus.Unsubscribe(controller);}
    }
    static void WhipNative(BlueprintsCache c,Harmony h,Assembly dc,BlueprintBuff external)
    {
        Console.WriteLine("054 Hurricane: native blade, real provider AoO flag, burn pipeline and actual PrepareCast");
        h.CreateClassProcessor(dc.GetType("DarkCodex.Patch_KineticistAllowOpportunityAttack",true)).Patch();
        var u=EquipmentUnit(c,13,"42a455d9ec1ad924d889272429eb8391");u.Stats.Constitution.BaseValue=20;u.Stats.HitPoints.BaseValue=1000;
        var ability=A<BlueprintAbility>(c,"WhipHurricaneAbility01454");var gate=ability.GetComponent<WhipHurricaneCaster01454>();Check(!gate.IsCasterRestrictionPassed(u),"no kinetic part/blade cannot cast Hurricane");
        Grant(u,N<BlueprintFeature>(c,"57e3577a0eb53294e9d7cc649d5239a3"));Grant(u,N<BlueprintFeature>(c,"ccc7583a7cb345a41a33a8e11ddd91b5"));
        Grant(u,N<BlueprintFeature>(c,"31ad04e4c767f5d4b96c13a71fd7ff15"));u.Resources.Restore(N<BlueprintAbilityResource>(c,"066ac4b762e32be4b953703174ed925c"));u.Resources.Restore(N<BlueprintAbilityResource>(c,"90eec36e1bca15a4dafc7b17916debf4"));Check(u.Get<UnitPartKineticist>().LeftBurnThisRound>=4,"native fresh/rested resource input permits four burn this round");
        var bladeToggle=u.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.AssetGuid==BlueprintGuid.Parse("89acea313b9a9cb4d86bbbca01b90346"));ToggleOn(bladeToggle);
        var blade=u.Body.PrimaryHand.MaybeWeapon;Check(blade?.Blueprint.GetComponent<WeaponKineticBlade>()!=null&&gate.IsCasterRestrictionPassed(u),"actual native blade activation creates its weapon and satisfies the caster gate");
        var threat=AccessTools.Method(dc.GetType("DarkCodex.Patch_KineticistAllowOpportunityAttack",true),"GetThreatHand_AttackOfOpportunity");Check(threat.Invoke(null,new object[]{u})==null,"actual provider rejects ordinary kinetic blade AoO");
        var kinetic=ability.GetComponent<AbilityKineticist>();var ordinary=N<BlueprintAbility>(c,"77cb8c607b263194894a929c8ac59708");int ordinaryBefore=ordinary.AbilityKineticist.CalculateBurnCost(u.Descriptor,ordinary).Total;
        var cost=kinetic.CalculateBurnCost(u.Descriptor,ability);Check(cost.BlastBase==0&&cost.InfusionBase==4&&cost.Total==4,"native basic blade uses Hurricane four-point form, not old whirlwind's fixed 3+2");
        int reach=u.Stats.Reach.ModifiedValue;var part=u.Get<UnitPartKineticist>();var data=new AbilityData(ability,u.Descriptor);var context=new AbilityExecutionContext(data,new AbilityParams{CasterLevel=13,DC=15},new TargetWrapper(u),new RulebookEventContext(),u);Kingmaker.Controllers.AbilityExecutionProcess.PrepareCast(context);
        var lease=u.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("WhipHurricaneLease01454"));var state=lease.Components.Single(r=>r.SourceBlueprintComponent is WhipHurricaneLease01454).GetData<WhipHurricaneState01454>();
        Check(u.Stats.Reach.ModifiedValue==reach+5&&ReferenceEquals(threat.Invoke(null,new object[]{u}),u.Body.PrimaryHand)&&state.OwnsMarker,"actual native on-cast action creates lease, +5 reach, provider AoO flag and owned native marker");
        var savedBlade=state.Blade;var savedMarker=state.ActivationMarker;var savedWhip=state.Whip;var savedRuntime=lease.Components.Single(r=>r.SourceBlueprintComponent is WhipHurricaneLease01454);using(savedRuntime.RequestEventContext())AccessTools.Method(typeof(WhipHurricaneLease01454),"OnTurnOn").Invoke(savedRuntime.SourceBlueprintComponent,null);Check(ReferenceEquals(state.Blade,savedBlade)&&ReferenceEquals(state.ActivationMarker,savedMarker)&&ReferenceEquals(state.Whip,savedWhip)&&state.OwnsMarker,"reactivation preserves exact persisted blade, marker and own-child references");
        PostLoad(lease,typeof(WhipHurricaneLease01454));Check(!lease.IsDisposed&&ReferenceEquals(state.ActivationMarker,savedMarker)&&ReferenceEquals(state.Whip,savedWhip),"post-load validation retains the exact attached activation marker without duplicate child effects");
        Check(kinetic.CalculateBurnCost(u.Descriptor,ability).Total==4&&ordinary.AbilityKineticist.CalculateBurnCost(u.Descriptor,ordinary).Total==ordinaryBefore+1,"Hurricane includes whip form once while ordinary blade attacks retain provider surcharge");
        part.RemoveBladeActivatedBuff();Check(!lease.IsDisposed&&state.ActivationMarker?.IsDisposed==false&&state.DeferredRemoval,"native ordinary marker cleanup is deferred while Hurricane owns persistence");
        var foreign=Add(u,external);Check(u.Stats.Reach.ModifiedValue==reach+5&&state.Whip==null&&foreign.IsActive,"external whip replaces only our redundant copy with no doubled reach");foreign.Remove();Check(state.Whip?.IsActive==true&&u.Stats.Reach.ModifiedValue==reach+5,"actual external buff removal restores only our lease copy");
        var component=lease.Blueprint.GetComponent<WhipHurricaneLease01454>();var runtime=lease.Components.Single(r=>r.SourceBlueprintComponent==component);using(runtime.RequestEventContext())component.HandleNewCombatRound(u);
        Check(lease.IsDisposed&&u.Stats.Reach.ModifiedValue==reach&&threat.Invoke(null,new object[]{u})==null&&state.ActivationMarker==null,"next actual round handler removes only owned reach/AoO/marker state");
        Kingmaker.Controllers.AbilityExecutionProcess.PrepareCast(new AbilityExecutionContext(data,new AbilityParams{CasterLevel=13},new TargetWrapper(u),new RulebookEventContext(),u));lease=u.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("WhipHurricaneLease01454"));state=lease.Components.Single(r=>r.SourceBlueprintComponent is WhipHurricaneLease01454).GetData<WhipHurricaneState01454>();state.Blade=blade.Blueprint.CreateEntity<ItemEntityWeapon>();savedRuntime=lease.Components.Single(r=>r.SourceBlueprintComponent is WhipHurricaneLease01454);using(savedRuntime.RequestEventContext())AccessTools.Method(typeof(WhipHurricaneLease01454),"OnTurnOn").Invoke(savedRuntime.SourceBlueprintComponent,null);Check(lease.IsDisposed&&u.Stats.Reach.ModifiedValue==reach,"reactivation refuses a mismatched persisted blade instead of adopting the current equipped entity");
        Kingmaker.Controllers.AbilityExecutionProcess.PrepareCast(new AbilityExecutionContext(data,new AbilityParams{CasterLevel=13},new TargetWrapper(u),new RulebookEventContext(),u));lease=u.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("WhipHurricaneLease01454"));state=lease.Components.Single(r=>r.SourceBlueprintComponent is WhipHurricaneLease01454).GetData<WhipHurricaneState01454>();state.ActivationMarker=null;PostLoad(lease,typeof(WhipHurricaneLease01454));Check(lease.IsDisposed&&u.Stats.Reach.ModifiedValue==reach,"missing persisted activation-marker link ends the lease instead of acquiring another native marker");
        Kingmaker.Controllers.AbilityExecutionProcess.PrepareCast(new AbilityExecutionContext(data,new AbilityParams{CasterLevel=13,DC=15},new TargetWrapper(u),new RulebookEventContext(),u));lease=u.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("WhipHurricaneLease01454"));
        var whirlwind=new AbilityData(N<BlueprintAbility>(c,"80f10dc9181a0f64f97a9f7ac9f47d65"),u.Descriptor);Kingmaker.Controllers.AbilityExecutionProcess.PrepareCast(new AbilityExecutionContext(whirlwind,new AbilityParams{CasterLevel=13},new TargetWrapper(u),new RulebookEventContext(),u));Check(lease.IsDisposed&&u.Stats.Reach.ModifiedValue==reach,"real PrepareCast of another blade form ends Hurricane even when the equipped entity/marker would otherwise be reused");
        ToggleOff(bladeToggle);
        Grant(u,N<BlueprintFeature>(c,"360a3adea35bfca4c9bd6dac151705fc"));var metal=u.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.AssetGuid==BlueprintGuid.Parse("ea2b3e7e3b8726d4c94ba58118749742"));ToggleOn(metal);
        var composite=kinetic.CalculateBurnCost(u.Descriptor,ability);Check(composite.BlastBase==2&&composite.InfusionBase==4&&composite.Total==6,"actual native composite blade retains its two-point blast plus Hurricane form");
        var specialization=Grant(u,N<BlueprintFeature>(c,"5f64f446aef387d499a396b626d5fc51"));specialization.SetRank(3);Check(kinetic.CalculateBurnCost(u.Descriptor,ability).Total==3,"actual native ranked infusion specialization applies to Hurricane through the native cost event pipeline");
        var empower=Add(u,N<BlueprintBuff>(c,"f5f3aa17dd579ff49879923fb7bc2adb"));Check(kinetic.CalculateBurnCost(u.Descriptor,ability).Total==4,"actual native empower adds its one-point metakinesis cost once");var powered=new AbilityData(ability,u.Descriptor).CalculateParams();Check((powered.Metamagic&Metamagic.Empower)!=0,"native parameter event applies actual metakinesis effect, not only its burn surcharge");empower.Remove();
        Grant(u,N<BlueprintFeature>(c,"0601925a028b788469365d5f8f39e14a"));u.AddFact(N<BlueprintAbility>(c,"6dcbffb8012ba2a4cb4ac374a33e2d9a"));part.SetGatherPowerMode(KineticistGatherPowerMode.Low);
        var gather=Add(u,N<BlueprintBuff>(c,"e6b8b31e1f8c524458dc62e8a763cfb1"));Check(kinetic.CalculateBurnCost(u.Descriptor,ability).Total==2,"actual native controller, mode and Gather Power buff reduce remaining total burn");gather.Remove();part.SetGatherPowerMode(KineticistGatherPowerMode.Off);
        int accepted=part.AcceptedBurn;kinetic.Spend(new AbilityData(ability,u.Descriptor));Check(part.AcceptedBurn==accepted+3&&part.AcceptedBurnThisRound==3,"actual native AbilityKineticist.Spend accepts the computed composite/specialized burn once");
        Kingmaker.Controllers.AbilityExecutionProcess.PrepareCast(new AbilityExecutionContext(data,new AbilityParams{CasterLevel=13},new TargetWrapper(u),new RulebookEventContext(),u));lease=u.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("WhipHurricaneLease01454"));Check(u.Stats.Reach.ModifiedValue==reach+5,"repeated actual Hurricane creates one lease without doubled reach");
        ToggleOff(metal);Check(lease.IsDisposed&&u.Stats.Reach.ModifiedValue==reach,"actual blade equipment removal ends owned lease immediately");new KineticistController().HandleNewCombatRound(u);u.Resources.Restore(N<BlueprintAbilityResource>(c,"90eec36e1bca15a4dafc7b17916debf4"));ToggleOn(metal);
        var marker=(BlueprintBuff)Call(T("WhipHurricane01454"),"Marker",part);var prior=Add(u,marker);Kingmaker.Controllers.AbilityExecutionProcess.PrepareCast(new AbilityExecutionContext(data,new AbilityParams{CasterLevel=13},new TargetWrapper(u),new RulebookEventContext(),u));lease=u.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("WhipHurricaneLease01454"));state=lease.Components.Single(r=>r.SourceBlueprintComponent is WhipHurricaneLease01454).GetData<WhipHurricaneState01454>();Check(!state.OwnsMarker&&ReferenceEquals(prior,state.ActivationMarker),"preexisting native activation marker is borrowed by exact reference");lease.Remove();Check(!prior.IsDisposed&&u.Stats.Reach.ModifiedValue==reach,"ordinary lease expiry retains external activation marker when no native cleanup was deferred");prior.Remove();
        Serialization(c);
        ToggleOff(metal);
    }
}
