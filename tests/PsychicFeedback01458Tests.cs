using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    internal static void Prepare058Presentation()
    {
        var h=new Harmony("CotW.Psychic058Presentation");
        foreach(var type in Main.GetTypes().Where(PresentationType058))foreach(var method in type.GetMethods(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.DeclaredOnly).Where(m=>m.GetMethodBody()!=null&&!m.ContainsGenericParameters))h.Patch(method,transpiler:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Labels)));
    }
    static bool PresentationType058(Type t)=>t.Name=="PsychicRemainingDisciplines01458"||t.Name=="PsychicCompletionAmplifications01458"||t.Name=="PsychicClassCompletion01458"||t.Name=="PsychicDisciplineBridge01458"||t.Name=="FeralHunter01461"||t.Name=="TotemBonded01461"||(t.DeclaringType!=null&&PresentationType058(t.DeclaringType));
    static bool Pick058(UnitEntityData unit,BlueprintFeatureSelection pool,BlueprintFeature feature)
    {
        var state=(LevelUpState)FormatterServices.GetUninitializedObject(typeof(LevelUpState));Set(state,"m_Unit",unit);Set(state,"Selections",new List<FeatureSelectionState>());Set(state,"NextCharacterLevel",unit.Progression.Classes[0].Level);state.NextClassLevel=unit.Progression.Classes[0].Level;state.SelectedClass=unit.Progression.Classes[0].CharacterClass;
        var pick=state.AddSelection(null,default(FeatureSource),pool,1);return pool.CanSelect(unit.Descriptor,state,pick,feature);
    }
    static bool NoDebug058(ref bool __result){__result=false;return false;}
    static bool PoolIgnore058(BlueprintFeatureSelection __instance,ref bool __result){__result=__instance.IgnorePrerequisites;return false;}
    static int D20Input058=10;
    static bool DiceInput058(Kingmaker.RuleSystem.DiceFormula __0,ref int __result){__result=__0.Rolls*(__0.Dice==Kingmaker.RuleSystem.DiceType.D20?D20Input058:((int)__0.Dice+1)/2);return false;}
    static Kingmaker.EntitySystem.EntityService EntityRegistry058;
    static readonly Kingmaker.View.UnitEntityView ViewInput058=(Kingmaker.View.UnitEntityView)FormatterServices.GetUninitializedObject(typeof(Kingmaker.View.UnitEntityView));
    static bool View058(UnitEntityData __instance,ref Kingmaker.View.UnitEntityView __result){if(!Actors.Contains(__instance))return true;__result=ViewInput058;return false;}
    static bool Corpulence058(ref float __result){__result=.5f;return false;}
    static float Approach058(AbilityData ability,UnitEntityData target,Harmony h)
    {
        var viewField=AccessTools.Field(typeof(Kingmaker.EntitySystem.EntityDataBase),"<View>k__BackingField");var caster=ability.Caster.Unit;var beforeCaster=viewField.GetValue(caster);var beforeTarget=viewField.GetValue(target);viewField.SetValue(caster,ViewInput058);viewField.SetValue(target,ViewInput058);var corp=AccessTools.PropertyGetter(typeof(Kingmaker.View.UnitEntityView),"Corpulence");h.Patch(corp,prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Corpulence058)));
        try{return ability.GetApproachDistance(target);}finally{viewField.SetValue(caster,beforeCaster);viewField.SetValue(target,beforeTarget);h.Unpatch(corp,AccessTools.Method(typeof(MagusBatch01454Tests),nameof(Corpulence058)));}
    }
    static bool Registry058(ref Kingmaker.EntitySystem.EntityService __result){__result=EntityRegistry058;return false;}
    static void SeedEntity058(UnitEntityData unit)
    {
        var field=AccessTools.Field(typeof(Kingmaker.EntitySystem.EntityService),"m_Entities");var map=(System.Collections.IDictionary)field.GetValue(EntityRegistry058);map[unit.UniqueId]=unit.Proxy;
    }
    static readonly List<string> AreaLoads058=new List<string>();
    static UnityEngine.GameObject LoadArea058(Kingmaker.ResourceLinks.WeakResourceLink<UnityEngine.GameObject> link,bool unused,bool async){AreaLoads058.Add(link.AssetId);return null;}
    static IEnumerable<CodeInstruction> AreaEndpoints058(IEnumerable<CodeInstruction> input)
    {
        var bounded=(IEnumerable<CodeInstruction>)AccessTools.Method(typeof(Arcane01446Tests),"FxBoundary").Invoke(null,new object[]{input});int loads=0;
        foreach(var code in bounded){var m=code.operand as System.Reflection.MethodInfo;if(m?.DeclaringType==typeof(Kingmaker.ResourceLinks.WeakResourceLink<UnityEngine.GameObject>)&&m.Name=="Load"){code.opcode=System.Reflection.Emit.OpCodes.Call;code.operand=AccessTools.Method(typeof(MagusBatch01454Tests),nameof(LoadArea058));loads++;}yield return code;}
        if(loads!=1)throw new Exception("058 actual native area SpawnFxs must retain exactly one bounded prefab Load endpoint, got "+loads);
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void AreaNative058(Kingmaker.View.MapObjects.AreaEffectView view)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)=>AreaEndpoints058(code);
        _=(Func<IEnumerable<CodeInstruction>,IEnumerable<CodeInstruction>>)Transpiler;
        throw new NotImplementedException("058 exact native SpawnFxs reverse copy must replace this stub");
    }
    static void Feedback058(BlueprintsCache parent,Harmony h)
    {
        var clock=AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime");var prior=(Harmony.GetPatchInfo(clock)?.Prefixes.AsEnumerable()??Enumerable.Empty<Patch>()).ToArray();
        foreach(var patch in prior){Check(patch.PatchMethod.DeclaringType.Name.EndsWith("Tests")&&(patch.PatchMethod.Name=="Time"||patch.PatchMethod.Name=="Clock"),"058 clock replaces only declared standalone scene clock endpoint");h.Unpatch(clock,patch.PatchMethod);}
        var dice=AccessTools.Method(typeof(Kingmaker.RuleSystem.RulebookEvent.Dice),"D",new[]{typeof(Kingmaker.RuleSystem.DiceFormula)});var oldDice=(Harmony.GetPatchInfo(dice)?.Prefixes.AsEnumerable()??Enumerable.Empty<Patch>()).ToArray();
        foreach(var p in oldDice){Check(p.PatchMethod.DeclaringType==typeof(BardMasterpieces01435Tests)&&p.PatchMethod.Name=="Dice","058 replaces only inherited declared scene RNG endpoint, preserving native rule/stat arithmetic");h.Unpatch(dice,p.PatchMethod);}
        h.Patch(dice,prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(DiceInput058)));
        try{Check(Game.Instance.TimeController.GameTime==Game.Instance.Player.GameTime,"058 timing uses the actual native Player clock getter");Check(Kingmaker.RuleSystem.RulebookEvent.Dice.D(new Kingmaker.RuleSystem.DiceFormula(1,Kingmaker.RuleSystem.DiceType.One))==1,"058 RNG endpoint returns legal d1 result rather than inherited zero-only non-d20 fixture");FeedbackBody058(parent,h);}
        catch(Exception error){for(var e=error;e!=null;e=e.InnerException)Console.Error.WriteLine("058_SAFE_EXCEPTION "+e.GetType().FullName+" "+(AccessTools.Field(typeof(Exception),"_message")?.GetValue(e) as string)+"\n"+e.StackTrace);throw new InvalidOperationException("058 native test failed; original exception types and stacks recorded above.");}
        finally{h.Unpatch(dice,AccessTools.Method(typeof(MagusBatch01454Tests),nameof(DiceInput058)));foreach(var p in oldDice)new Harmony(p.owner).Patch(dice,prefix:new HarmonyMethod(p.PatchMethod){priority=p.priority,before=p.before,after=p.after});foreach(var p in prior)new Harmony(p.owner).Patch(clock,prefix:new HarmonyMethod(p.PatchMethod){priority=p.priority,before=p.before,after=p.after});}
    }
    static void FeedbackBody058(BlueprintsCache parent,Harmony h)
    {
        EquipmentPatches(h);h.Patch(AccessTools.Method(T("PsychicOriginalIcons"),"Load"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Icon057)));
        EntityRegistry058=(Kingmaker.EntitySystem.EntityService)FormatterServices.GetUninitializedObject(typeof(Kingmaker.EntitySystem.EntityService));Set(EntityRegistry058,"m_EntitiesLock",new object());var registryField=AccessTools.Field(typeof(Kingmaker.EntitySystem.EntityService),"m_Entities");registryField.SetValue(EntityRegistry058,Activator.CreateInstance(registryField.FieldType));
        h.Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites),"Ignore"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(NoDebug058)));
        h.Patch(AccessTools.Method(typeof(BlueprintFeatureSelection),"IsIgnorePrerequisites"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(PoolIgnore058)));
        // The standalone CLR cannot JIT Unity internal calls. Preserve the
        // actual SpawnFxs reverse body, then invoke the actual production
        // postfix. Native-target detour pinning itself requires Unity Mono;
        // it is explicitly not counted as installed in this CLR fixture.
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.View.MapObjects.AreaEffectView),"TacticalCombat"),prefix:new HarmonyMethod(typeof(SpellBatch01447Tests),"False"));
        var areaNative=AccessTools.Method(typeof(MagusBatch01454Tests),nameof(AreaNative058));
        h.CreateReversePatcher(AccessTools.Method(typeof(Kingmaker.View.MapObjects.AreaEffectView),"SpawnFxs"),new HarmonyMethod(areaNative)).Patch();
        var patches=Main.GetTypes().Where(t=>t.Name.EndsWith("01458Patch")&&t.GetCustomAttributes(typeof(HarmonyPatch),false).Any()).ToArray();bool current061=Main.GetType("CallOfTheWild.Wrath.Prototype.PsychicWoundMenu01461")!=null;Check(patches.Length==(current061?14:15)&&patches.Any(t=>t.Name=="UndercastSurgeConversions01458Patch")==!current061,"exact058 production patch inventory retains all targets except061 explicitly retired Surge conversion UI; area rendering uses audited native reverse-body boundary");
        foreach(var type in patches){Console.WriteLine("058 production patch: "+type.Name);if(type.Name!="AuraRadiusSpawn01458Patch")h.CreateClassProcessor(type).Patch();}
        if((Environment.GetEnvironmentVariable("COTW_01454_CASE")??"").EndsWith("062",StringComparison.Ordinal)){Feedback062(parent,h);return;}
        if((Environment.GetEnvironmentVariable("COTW_01454_CASE")??"").EndsWith("059",StringComparison.Ordinal)){Feedback059(parent,h);return;}
        if((Environment.GetEnvironmentVariable("COTW_01454_CASE")??"").EndsWith("060",StringComparison.Ordinal)){Feedback060(parent,h);return;}
        if((Environment.GetEnvironmentVariable("COTW_01454_CASE")??"").EndsWith("061",StringComparison.Ordinal)){Feedback061(parent,h);return;}
        if(Environment.GetEnvironmentVariable("COTW_01454_CASE")=="registration058"){Registration058(parent,h);return;}
        var c=PsychicCache057(parent);WarmMercies058(c);Register(c,"MutationMind01457");Register(c,"PsychicMarauder01457");Register(c,"PsychicClassCompletion01458");var root=A<BlueprintArchetype>(c,"MutationMindArchetype01457");
        if(Environment.GetEnvironmentVariable("COTW_01454_CASE")=="disciplines058"){Disciplines058(c,h);return;}
        if(Environment.GetEnvironmentVariable("COTW_01454_CASE")=="discipline-effects058"){DisciplineEffects058(c,h);return;}
        if(Environment.GetEnvironmentVariable("COTW_01454_CASE")=="amplifications058"){Amplifications058(c,h);return;}
        var selection=A<BlueprintFeatureSelection>(c,"PhrenicAmplificationSelection");var extra=A<BlueprintFeatureSelection>(c,"ExtraPhrenicAmplificationFeat");var finger=A<BlueprintFeature>(c,"ElongatedFingersBodilyMutationFeature01457");
        string[] mutations=(string[])AccessTools.Field(T("MutationMind01457"),"Mutations").GetValue(null);
        foreach(var pool in new[]{selection,extra})foreach(string name in mutations)Check(pool.AllFeatures.Count(v=>v.AssetGuid==Id(name+"BodilyMutationFeature01457"))==1,"both actual selection pools contain exactly one mutation: "+pool.name+"/"+name);
        foreach(int level in new[]{1,2,3,7,10,11,20}){
            var unit=EquipmentUnit(c,level,"83fa3b50138c566698801ba5967bbde0");unit.Progression.Classes[0].AddArchetype(root);Grant(unit,N<BlueprintFeature>(c,"4e8266b477c65aaf836869377711184a"));
            if(level>=3)Grant(unit,c,"BodilyMutationsFeature01457");if(level>=11)Grant(unit,c,"ImprovedBodilyMutationsFeature01457");
            Check(Pick058(unit,extra,finger)==(level>=3),"native Extra Amplification selector applies basic Elongated Fingers requirement at level "+level);
            Check(Pick058(unit,extra,A<BlueprintFeature>(c,"WingsBodilyMutationFeature01457"))==(level>=11),"native Extra Amplification selector applies improved mutation requirement at level "+level);
        }
        var other=EquipmentUnit(c,20);Check(!Pick058(other,extra,finger),"native extra selector rejects unrelated class without bodily mutation qualification");
        var owner=EquipmentUnit(c,12,"83fa3b50138c566698801ba5967bbde0");owner.Progression.Classes[0].AddArchetype(root);Grant(owner,c,"PhysicalMutationFeature01457");Grant(owner,finger);
        var poolResource=A<BlueprintAbilityResource>(c,"PhysicalMutationResource01457");var toggle=Toggle(owner,c,"PhysicalMutationToggle01458");
        var touch=N<BlueprintAbility>(c,"17451c1327c571641a1345bd31155209");var weapon=touch.GetComponent<AbilityDeliverTouch>().TouchWeapon;
        var touchData=new AbilityData(touch,owner.Descriptor);var victim=EquipmentUnit(c,12);int ordinary=owner.Descriptor.GetWeaponRange(weapon).Value;float approach=Approach058(touchData,victim,h);int normalWeapon=owner.Body.EmptyHandWeapon.AttackRange.Value;Check(ordinary>0,"actual native melee touch weapon has baseline reach");
        var time=Game.Instance.Player.GameTime;int before=owner.Resources.GetResourceAmount(poolResource);ToggleOn(toggle);var effect=owner.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("PhysicalMutationBuff01457"));var paidUntil=effect.EndTime;
        Check(owner.Resources.GetResourceAmount(poolResource)==before-1&&paidUntil-time==TimeSpan.FromMinutes(1),"native persistent mutation activation pays exactly one minute");
        Console.WriteLine("058 reach diagnostics type="+weapon.AttackType+" before="+ordinary+" after="+owner.Descriptor.GetWeaponRange(weapon).Value+" buffs="+string.Join(";",owner.Buffs.Enumerable.Select(b=>b.Blueprint.name+":"+b.IsActive+":"+b.IsTurnedOn+":"+b.IsSuppressed)));
        Check(owner.Descriptor.GetWeaponRange(weapon).Value==ordinary+5,"actual native melee touch weapon gains exactly five feet while fingers are active");
        Check(Math.Abs(Approach058(touchData,victim,h)-approach-1.524f)<.0001f,"actual native melee-touch approach gains exactly five feet with fixed scene corpulence input");
        Check(owner.Body.EmptyHandWeapon.AttackRange.Value==normalWeapon&&victim.Descriptor.GetWeaponRange(weapon).Value==ordinary,"actual native ordinary equipped weapon and another caster receive no fingers reach");
        var ray=new AbilityData(N<BlueprintAbility>(c,"fa3078b9976a5b24caf92e20ee9c0f54"),owner.Descriptor);Check(!(bool)Call(T("MutationReach01458"),"MeleeTouch",ray),"native projectile ray is excluded from melee-touch mutation");
        var healing=new AbilityData(N<BlueprintAbility>(c,"5590652e1c2225c4ca30c4a699ab3649"),owner.Descriptor);Check(!(bool)Call(T("MutationReach01458"),"MeleeTouch",healing),"native friendly healing touch without a melee attack is excluded");
        var reached=new AbilityData(touch,owner.Descriptor){MetamagicData=new MetamagicData()};reached.MetamagicData.Add(Metamagic.Reach);Check(!(bool)Call(T("MutationReach01458"),"MeleeTouch",reached),"Reach-metamagic delivery is excluded from melee-touch approach mutation");
        Game.Instance.Player.GameTime=time+TimeSpan.FromSeconds(30);ToggleOff(toggle);
        Check(effect.IsAttached&&effect.EndTime==paidUntil&&owner.Resources.GetResourceAmount(poolResource)==before-1,"native toggle-off preserves exact remaining paid buff and stops spending");
        ToggleOn(toggle);Check(ReferenceEquals(effect,owner.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("PhysicalMutationBuff01457")))&&effect.EndTime==paidUntil&&owner.Resources.GetResourceAmount(poolResource)==before-1,"native re-enable reuses remaining paid time without cost or refresh");
        Game.Instance.Player.GameTime=paidUntil;owner.Buffs.TickBuff(effect,paidUntil);
        Check(effect.IsAttached&&effect.EndTime==paidUntil+TimeSpan.FromMinutes(1)&&owner.Resources.GetResourceAmount(poolResource)==before-2,"actual native scheduled expiry renews one minute without removing mutation");
        Check(!owner.State.HasCondition(UnitCondition.Fatigued),"maintained native minute boundary does not transiently apply fatigue");
        ToggleOff(toggle);Game.Instance.Player.GameTime=effect.EndTime;owner.Buffs.TickBuff(effect,effect.EndTime);
        Console.WriteLine("058 expiry diagnostics attached="+effect.IsAttached+" disposed="+effect.IsDisposed+" fatigue="+owner.State.HasCondition(UnitCondition.Fatigued)+" reach="+owner.Descriptor.GetWeaponRange(weapon).Value+" end="+effect.EndTime+" now="+Game.Instance.TimeController.GameTime+" buffs="+string.Join(";",owner.Buffs.Enumerable.Select(b=>b.Blueprint.name+":"+b.IsActive+":"+b.IsTurnedOn)));
        Check(!effect.IsAttached&&owner.State.HasCondition(UnitCondition.Fatigued)&&owner.Descriptor.GetWeaponRange(weapon).Value==ordinary,"native paid expiry removes fingers and applies fatigue only at actual end");
        PhysicalEdges058(c,root,h,touch);
        Game.Instance.Player.GameTime=time;
        foreach(string mode in new[]{"I","II","IIIStill","IIIHarm","IIIAttack"})for(int i=0;i<6;i++){
            var area=A<BlueprintAbilityAreaEffect>(c,"Marauder"+mode+i+"Area01457");Check(area.Fx?.AssetId=="20caf000cd4c3434da00a74f4a49dccc"&&area.Size.Value==10+5*i,"native aura ring resource and actual radius are bound for "+mode+"/"+i);AreaDispatch058(area);
        }
        AuraVisual058(c,h);Register(c,"PsychicAmplifications");Register(c,"MutationMind01457");Check(extra.AllFeatures.Count(v=>v.AssetGuid==finger.AssetGuid)==1,"actual producers repeat with both declared mutation selection extensions");
        PsychicExpansionTests.Inventory["psychicFeedback01458Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["coverage"]=new JArray(Coverage),["gameLaunchTested"]=false,["boundary"]="Actual native selection, persistent toggle, resource/stat and scheduled BuffCollection expiry paths. Scene/rendered FX and final game save transport remain explicit boundaries."};
    }
    static void AreaDispatch058(BlueprintAbilityAreaEffect area)
    {
        var view=(Kingmaker.View.MapObjects.AreaEffectView)FormatterServices.GetUninitializedObject(typeof(Kingmaker.View.MapObjects.AreaEffectView));Set(view,"m_Blueprint",BlueprintReferenceBase.CreateTyped<BlueprintAbilityAreaEffectReference>(area));AreaLoads058.Clear();
        AreaNative058(view);Call(T("AuraRadiusSpawn01458Patch"),"Postfix",view);
        Check(AreaLoads058.SequenceEqual(new[]{area.Fx.AssetId}),"actual native area spawn reads declared ring and actual production postfix tolerates pending/no spawned handle: "+area.name);
        var saved=area.Fx;area.Fx=null;bool rejected=false;try{AreaNative058(view);}catch(NullReferenceException){rejected=true;}finally{area.Fx=saved;}Check(rejected,"native area prefab null control rejects instead of silently passing: "+area.name);
    }
    static void PhysicalEdges058(BlueprintsCache c,BlueprintArchetype root,Harmony h,BlueprintAbility touch)
    {
        var resource=A<BlueprintAbilityResource>(c,"PhysicalMutationResource01457");var start=Game.Instance.Player.GameTime;
        foreach(int level in new[]{1,3,12,20}){
            var u=EquipmentUnit(c,level,"83fa3b50138c566698801ba5967bbde0");u.Progression.Classes[0].AddArchetype(root);Grant(u,c,"PhysicalMutationFeature01457");Grant(u,c,"ElongatedFingersBodilyMutationFeature01457");var t=Toggle(u,c,"PhysicalMutationToggle01458");
            int initial=u.Resources.GetResourceAmount(resource);ToggleOn(t);var b=u.Buffs.Enumerable.Single(v=>v.Blueprint.AssetGuid==Id("PhysicalMutationBuff01457"));var end=b.EndTime;
            for(int i=0;i<8;i++){ToggleOff(t);Check(!t.IsOn&&b.IsAttached&&b.EndTime==end,"native swift toggle IsOn clears while paid buff remains at "+level+" / "+i);ToggleOn(t);}
            Check(u.Resources.GetResourceAmount(resource)==initial-1&&b.EndTime==end&&u.Buffs.Enumerable.Count(v=>v.Blueprint.AssetGuid==b.Blueprint.AssetGuid)==1,"repeated re-enable cannot mint duration, double-charge or multiply physical buffs at "+level);
            SeedEntity058(u);var state=b.Components.Single(v=>v.SourceBlueprintComponent is PhysicalMutationDuration01458).GetData<PhysicalMutationDurationState01458>();var saveSerializer=Newtonsoft.Json.JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings{ContractResolver=new Kingmaker.EntitySystem.Persistence.JsonUtility.OptInContractResolver(),Converters=Kingmaker.EntitySystem.Persistence.JsonUtility.DefaultJsonSettings.CommonConverters.ToList(),TypeNameHandling=Newtonsoft.Json.TypeNameHandling.None});var payload=JObject.FromObject(state,saveSerializer);Check(payload["Controller"]!=null,"native save serializer actually emits the paid controller reference");var roundtrip=payload.ToObject<PhysicalMutationDurationState01458>(saveSerializer);
            var registry=AccessTools.PropertyGetter(typeof(Kingmaker.EntitySystem.EntityService),"Instance");h.Patch(registry,prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Registry058)));
            try{Check(roundtrip.Controller.FactId==t.AppliedBuff.UniqueId&&ReferenceEquals(roundtrip.Controller.Fact,t.AppliedBuff),"native saved fact-reference serializer and actual EntityService.GetProxy rebind exact controller at "+level);state.Controller=roundtrip.Controller;}
            finally{h.Unpatch(registry,AccessTools.Method(typeof(MagusBatch01454Tests),nameof(Registry058)));}
            Game.Instance.Player.GameTime=end-TimeSpan.FromTicks(1);u.Buffs.TickBuff(b,Game.Instance.Player.GameTime);Check(b.EndTime==end&&u.Resources.GetResourceAmount(resource)==initial-1,"scheduled tick before minute expiry spends nothing at "+level);
            Game.Instance.Player.GameTime=end+TimeSpan.FromMinutes(100);u.Buffs.TickBuff(b,Game.Instance.Player.GameTime);
            Check(!b.IsAttached&&!t.IsOn&&u.Resources.GetResourceAmount(resource)==0&&u.State.HasCondition(UnitCondition.Fatigued),"large actual native clock advance spends bounded remaining minutes, expires once and disables exhausted toggle at "+level);
            Check(u.Buffs.Enumerable.Count(v=>v.Blueprint.AssetGuid==BlueprintGuid.Parse("e6f2fc5d73d88064583cb828801212f4"))==1,"exhaustion creates one fatigue buff at "+level);
            Game.Instance.Player.GameTime=start;
        }
        var other=EquipmentUnit(c,20);var bare=new AbilityData(touch,other.Descriptor);Check(!(bool)Call(T("MutationReach01458"),"Enabled",other.Descriptor),"unrelated owner never has fingers reach");
        var owner=EquipmentUnit(c,12,"83fa3b50138c566698801ba5967bbde0");Grant(owner,c,"PhysicalMutationFeature01457");var toggle=Toggle(owner,c,"PhysicalMutationToggle01458");
        var tired=Add(owner,N<BlueprintBuff>(c,"e6f2fc5d73d88064583cb828801212f4"));Check(!toggle.IsAvailableByRestrictions,"native persistent toggle restriction rejects fatigue");tired.Remove();
        var exhausted=Add(owner,N<BlueprintBuff>(c,"46d1b9cc3d0fd36469a471b047d773a2"));Check(!toggle.IsAvailableByRestrictions,"native persistent toggle restriction rejects exhaustion");exhausted.Remove();
        ToggleOn(toggle);var main=owner.Buffs.Enumerable.Single(v=>v.Blueprint.AssetGuid==Id("PhysicalMutationBuff01457"));int paid=owner.Resources.GetResourceAmount(resource);main.Remove();Check(!toggle.IsOn&&owner.Resources.GetResourceAmount(resource)==paid,"external native physical-buff removal stops controller without another charge");
    }
}
