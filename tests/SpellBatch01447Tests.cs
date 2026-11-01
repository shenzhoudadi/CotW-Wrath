using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers;
using Kingmaker.Designers;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class SpellBatch01447Tests
{
    static readonly Assembly Main=typeof(ArcaneCastMode01446).Assembly;
    static Type T(string n)=>Main.GetType("CallOfTheWild.Wrath.Prototype."+n,true);
    static object Call(Type t,string m,params object[] a)=>AccessTools.Method(t,m).Invoke(null,a);
    static object H(string m,params object[] a)=>Call(typeof(Arcane01446Tests),m,a);
    static object Get(object o,string n)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).GetValue(o);
    static void Set(object o,string n,object v)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).SetValue(o,v);
    static BlueprintGuid Id(string s)=>(BlueprintGuid)Call(T("PsychicAssets"),"Id",s);
    static TAsset A<TAsset>(BlueprintsCache c,string s)where TAsset:SimpleBlueprint=>(TAsset)c.Load(Id(s));
    static TAsset N<TAsset>(BlueprintsCache c,string s)where TAsset:SimpleBlueprint=>(TAsset)c.Load(BlueprintGuid.Parse(s));
    static UnitEntityData Unit(BlueprintsCache c,string cls,int level=19){var u=(UnitEntityData)H("Unit",c,cls,level);Set(u.CombatState,"m_EngagedBy",new Dictionary<UnitEntityData,TimeSpan>());return u;}
    static Spellbook Book(UnitEntityData u,BlueprintCharacterClass cls)=>(Spellbook)H("Book",u,cls);
    static Feature Fact(UnitEntityData u,BlueprintFeature f)=>(Feature)H("Fact",u,f);
    static Buff Buff(UnitEntityData u,BlueprintBuff bp,UnitEntityData caster=null)=>(Buff)H("Buff",u,bp,caster);
    static IDisposable Scope(UnitFact f,BlueprintComponent c)=>(IDisposable)H("Scope",f,c);
    static BlueprintsCache Fresh(BlueprintsCache p){var c=(BlueprintsCache)H("Fresh",p);foreach(var f in Loaded(c).OfType<BlueprintUnitFact>())if(ReferenceEquals(f.Icon,null))Set(f,"m_Icon",FormatterServices.GetUninitializedObject(typeof(UnityEngine.Sprite)));
        // Legacy fixtures restore component arrays after their former minimal
        // donors have warmed BlueprintAbility Cacheable<T> fields. Reset those
        // derived caches only on this isolated native-data copy.
        foreach(var spell in Loaded(c).OfType<BlueprintAbility>())foreach(var field in typeof(BlueprintAbility).GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly))if(field.FieldType.IsGenericType&&field.FieldType.GetGenericTypeDefinition()==typeof(Kingmaker.Utility.Cacheable<>))field.SetValue(spell,Activator.CreateInstance(field.FieldType));
        foreach(var spell in Loaded(c).OfType<BlueprintAbility>())foreach(string name in new[]{"m_CachedRestrictions","m_CachedTargetRestrictions","m_CachedCasterRestrictions","m_HasVariants","m_IsCantrip","m_SpellDescriptor"})Set(spell,name,null);
        return c;}
    static SimpleBlueprint[] Loaded(BlueprintsCache c)=>(SimpleBlueprint[])H("Loaded",c);
    static readonly string[] Modules={"Overseer01447","SpellSpecialist01447","MagaambyanTelepath01447","Phantasm01447","InfectiousCharms01447"};
    static int checks,rollbackCases,privateCount;
    static void Register(BlueprintsCache c,string module)=>Call(T(module),"Register",c);
    static void Check(bool ok,string why){if(!ok)throw new Exception("SpellBatch01447: "+why);checks++;}
    static bool Reject(Action a){try{a();return false;}catch(Exception e){while(e is TargetInvocationException)e=e.InnerException;if(e is InvalidOperationException)return true;throw;}}
    static void Structural(BlueprintsCache parent) {
        foreach(string module in Modules) {
            var c=Fresh(parent);
            if(module=="MagaambyanTelepath01447"&&c.Load((BlueprintGuid)Call(T("PsychicAssets"),"Id","PhrenicAmplificationSelection"))==null)Call(T("PsychicAmplifications"),"Register",c);
            foreach(var spell in Loaded(c).OfType<BlueprintAbility>()){var school=spell.School;var sticky=spell.StickyTouch;var target=spell.TargetRestrictions;var caster=spell.CasterRestrictions;var restrictions=spell.Restrictions;}
            var before=Loaded(c);var native=before.OfType<BlueprintScriptableObject>().Select(bp=>new{Bp=bp,Cache=bp.ElementsArray,Nodes=bp.ElementsArray.ToArray(),Owners=bp.ElementsArray.Select(e=>e.Owner).ToArray()}).ToArray();
            Register(c,module);var assets=Loaded(c).Except(before).OfType<BlueprintScriptableObject>().ToArray();privateCount+=assets.Length;
            var rootToken=(string)AccessTools.Field(T(module),"RootToken").GetRawConstantValue();var rootId=(BlueprintGuid)Call(T("PsychicAssets"),"Id",rootToken);var root=c.Load(rootId);
            Check(root!=null&&assets.Length>=4,"complete independent root: "+module);Register(c,module);Register(c,module);Check(assets.All(bp=>ReferenceEquals(c.Load(bp.AssetGuid),bp)),"idempotent successful reentry: "+module);
            Check(native.All(x=>ReferenceEquals(x.Cache,x.Bp.ElementsArray)&&x.Nodes.SequenceEqual(x.Bp.ElementsArray)&&x.Owners.SequenceEqual(x.Bp.ElementsArray.Select(e=>e.Owner))),"all preexisting action/condition caches and owner metadata preserved: "+module);
            foreach(var bp in assets) {
                var nodes=((IEnumerable<Element>)Call(T("SpellBatch01447"),"Elements",bp.ComponentsArray,new HashSet<object>())).ToArray();
                if(bp is BlueprintAbility ability)Check(ability.TargetRestrictions.Cast<BlueprintComponent>().SequenceEqual(ability.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.Base.IAbilityTargetRestriction>().Cast<BlueprintComponent>())&&ability.CasterRestrictions.Cast<BlueprintComponent>().SequenceEqual(ability.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.Base.IAbilityCasterRestriction>().Cast<BlueprintComponent>())&&ability.Restrictions.Cast<BlueprintComponent>().SequenceEqual(ability.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.Base.IAbilityRestriction>().Cast<BlueprintComponent>()),"warmed source restrictions cannot leak into private native caches: "+bp.name);
                Check(bp.ElementsArray.SequenceEqual(nodes)&&bp.ElementsArray.All(e=>ReferenceEquals(e.Owner,bp)),"private element graph owns its independent native cache: "+bp.name);
                if(bp is BlueprintUnitFact fact)Check(!ReferenceEquals(fact.Icon,null),"every new ability/option has source icon: "+bp.name);
                foreach(var value in bp.ComponentsArray.Cast<object>().Concat(nodes).Where(x=>x.GetType().Assembly==Main&&x.GetType().GetCustomAttributes(typeof(TypeIdAttribute),false).Any()).GroupBy(x=>x.GetType()).Select(g=>g.First())) {
                    var prior=Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead;var original=bp.ElementsArray;var beforeNodes=original.ToArray();
                    try{Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=new BlueprintJsonWrapper(bp);var declared=value is BlueprintComponent?typeof(BlueprintComponent):typeof(Element);var writer=new StringWriter();Kingmaker.Blueprints.JsonSystem.Json.Serializer.Serialize(new JsonTextWriter(writer),value,declared);var copy=Kingmaker.Blueprints.JsonSystem.Json.Serializer.Deserialize(new JsonTextReader(new StringReader(writer.ToString())),declared);Check(copy.GetType()==value.GetType(),"native polymorphic roundtrip "+value.GetType().Name);}
                    finally{original.Clear();original.AddRange(beforeNodes);Set(bp,"m_AllElements",original);Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=prior;}
                }
            }
            Register(c,module);Check(assets.All(bp=>ReferenceEquals(c.Load(bp.AssetGuid),bp)),"reentry after private native cache warm-up preserves committed graph: "+module);
            if(root is BlueprintArchetype archetype) {Check(archetype.GetParentClass().Archetypes.Count(a=>a==archetype)==1,"archetype present once in real class list");for(int level=1;level<=20;level++){archetype.GetAddEntry(level);archetype.GetRemoveEntry(level);Check(true,"native archetype milestone readable "+module+level);}}
            foreach(int fault in new[]{1,2,3}) {
                var bad=Fresh(parent);
                SetStatic(typeof(Arcane01446Tests),"faultId",assets.Last().AssetGuid);SetStatic(typeof(Arcane01446Tests),"fault",fault);
                var classes=Loaded(bad).OfType<BlueprintCharacterClass>().Select(cl=>new{C=cl,A=Get(cl,"m_Archetypes"),G=cl.Progression?.UIGroups}).ToArray();
                var pools=Loaded(bad).OfType<BlueprintFeatureSelection>().Select(p=>new{P=p,F=Get(p,"m_Features"),A=Get(p,"m_AllFeatures")}).ToArray();
                Check(Reject(()=>Register(bad,module))&&(int)AccessTools.Field(typeof(Arcane01446Tests),"fault").GetValue(null)==0,"real publication failure is rejected "+module+"/"+fault);
                Check(assets.All(bp=>bad.Load(bp.AssetGuid)==null),"failed transaction leaves no partial GUIDs "+module+"/"+fault);
                Check(classes.All(x=>ReferenceEquals(x.A,Get(x.C,"m_Archetypes"))&&ReferenceEquals(x.G,x.C.Progression?.UIGroups))&&pools.All(x=>ReferenceEquals(x.F,Get(x.P,"m_Features"))&&ReferenceEquals(x.A,Get(x.P,"m_AllFeatures"))),"native lists and UI groups rolled back "+module+"/"+fault);
                Register(bad,module);Check(assets.All(bp=>bad.Load(bp.AssetGuid)!=null),"retry publishes every previously failed output "+module+"/"+fault);rollbackCases++;
            }
            var collision=Fresh(parent);var foreign=new BlueprintFeature{AssetGuid=rootId,name="Foreign01447"};collision.AddCachedBlueprint(rootId,foreign);Check(Reject(()=>Register(collision,module))&&ReferenceEquals(collision.Load(rootId),foreign),"foreign GUID preserved "+module);
            PsychicExpansionTests.Inventory[module=="InfectiousCharms01447"?"infectiousBlueprints01447Contracts":module+"Contracts"]=new JObject{["passed"]=true,["root"]=rootId.ToString(),["privateResourceCount"]=assets.Length,["ids"]=new JArray(assets.Select(a=>a.AssetGuid.ToString())),["sourceElementsUnchanged"]=true,["privateElementsIndependent"]=true,["threeRollbackPaths"]=true};
        }
    }
    static void SetStatic(Type t,string n,object v)=>AccessTools.Field(t,n).SetValue(null,v);
    static void Overseer(BlueprintsCache parent,Harmony h) {
        var c=Fresh(parent);Register(c,Modules[0]);foreach(string name in new[]{"OverseerSpiritActivation01447Patch","OverseerSpiritDeactivation01447Patch"})h.CreateClassProcessor(T(name)).Patch();
        var cls=N<BlueprintCharacterClass>(c,"145f1d3d360a7ad48bd95d392c81b38e");var root=A<BlueprintArchetype>(c,"OverseerArchetype01447");var u=Unit(c,cls.AssetGuid.ToString());var book=Book(u,cls);u.Progression.Classes[0].Archetypes.Add(root);
        var spirit=N<BlueprintProgression>(c,"50efeb23b314869409365c8061a4082d");var native=spirit.LevelEntries.SelectMany(e=>e.Features).OfType<BlueprintFeature>().First(f=>f.GetComponents<AddSpecialSpellList>().Any());var list=native.GetComponent<AddSpecialSpellList>().SpellList;
        Fact(u,native);var field=AccessTools.Field(typeof(Spellbook),"m_SpecialLists");Check(field!=null,"real native special-slot list service exists");var old=(System.Collections.IEnumerable)field.GetValue(book);Check(!old.Cast<object>().Contains(list),"Overseer suppresses original spirit magic at native feature activation");
        var feature=A<BlueprintFeature>(c,"OverseerControllingMagic01447");var ff=Fact(u,feature);var controlling=A<BlueprintSpellList>(c,"OverseerSpellList01447");
        Check(((System.Collections.IEnumerable)field.GetValue(book)).Cast<object>().Contains(controlling),"Controlling Magic registers the actual native special-slot list");
        Check(controlling.SpellsByLevel.Single(e=>e.SpellLevel==1).Spells.Single().AssetGuid.ToString()=="88367310478c10b47903463c5d0152b0"&&controlling.SpellsByLevel.Single(e=>e.SpellLevel==3).Spells.Single()==A<BlueprintAbility>(c,"OverseerHoldPerson01447"),"CotW first-level Hypnotism and independent third-level Hold Person");
        Check(Enumerable.Range(1,9).All(l=>controlling.SpellsByLevel.Single(e=>e.SpellLevel==l).Spells.Count==1),"one exact controlling spell at each of nine circles");
        u.Progression.Classes[0].Archetypes.Remove(root);u.Facts.Remove(ff);Check(!((System.Collections.IEnumerable)field.GetValue(book)).Cast<object>().Contains(controlling)&&((System.Collections.IEnumerable)field.GetValue(book)).Cast<object>().Contains(list),"native original spirit list restored when archetype is removed");
    }
    static string UIName(BlueprintUnitFact f)=>f?.name??"fixture";
    static bool UIParam(FeatureParam __0,ref string __result){if(__0?.Blueprint==null)return true;__result=__0.Blueprint.name;return false;}
    static bool ObjectEqual(UnityEngine.Object a,UnityEngine.Object b)=>ReferenceEquals(a,b);
    static IEnumerable<CodeInstruction> UILabelBoundary(IEnumerable<CodeInstruction> codes){foreach(var c in codes){if(c.operand is MethodInfo m){if((m.Name=="get_Name"||m.Name=="get_Description")&&typeof(BlueprintUnitFact).IsAssignableFrom(m.DeclaringType)){c.opcode=System.Reflection.Emit.OpCodes.Call;c.operand=AccessTools.Method(typeof(SpellBatch01447Tests),nameof(UIName));}if(m.DeclaringType==typeof(UnityEngine.Object)&&m.Name=="op_Equality")c.operand=AccessTools.Method(typeof(SpellBatch01447Tests),nameof(ObjectEqual));}yield return c;}}
    static void Specialist(BlueprintsCache parent,Harmony h) {
        var c=Fresh(parent);Register(c,Modules[1]);
        foreach(var m in new MethodBase[]{AccessTools.Constructor(typeof(FeatureUIData),new[]{typeof(BlueprintFeature),typeof(FeatureParam)}),AccessTools.Method(typeof(FeatureUIData),"GetParamName"),AccessTools.Method(typeof(FeatureUIData),"GetParamDescription"),AccessTools.Method(typeof(FeatureUIData),"GetParamAcronym")})h.Patch(m,transpiler:new HarmonyMethod(typeof(SpellBatch01447Tests),nameof(UILabelBoundary)));
        foreach(string m in new[]{"GetParamName","GetParamDescription"})h.Patch(AccessTools.Method(typeof(FeatureUIData),m),prefix:new HarmonyMethod(typeof(SpellBatch01447Tests),nameof(UIParam)));
        h.CreateClassProcessor(T("SignatureSelection01447Patch")).Patch();
        var cls=N<BlueprintCharacterClass>(c,"52dbfd8505e22f84fad8d702611f60b7");var root=A<BlueprintArchetype>(c,"SpellSpecialistArchetype01447");
        Check(root.RemoveFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,7,13,19})&&root.AddFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,4,6,8,10,12,14,16,18}),"exact CotW exploit and signature-spell milestones");
        foreach(int level in new[]{1,9,10,19}) {
            var u=Unit(c,cls.AssetGuid.ToString(),level);var book=Book(u,cls);var spell=N<BlueprintAbility>(c,"2d81362af43aeac4387a3d4fced489c3");var data=new AbilityData(spell,u.Descriptor);Set(data,"m_CachedSpellbook",book);book.AddKnown(3,spell);var before=data.CalculateParams();
            var param=A<BlueprintParametrizedFeature>(c,"SignatureSpell301447Parameter");
            var fact=new Feature(param,u.Descriptor){Param=new FeatureParam(spell)};u.Facts.Add(fact);Check(book.GetKnownSpell(spell)!=null,"native parametrized acquisition learns selected signature");var after=data.CalculateParams();
            Check(after.DC==before.DC+1&&after.Concentration==before.Concentration+(level>=10?4:2),"native signature DC/concentration at class level "+level);
            var offered=param.ExtractSelectionItems(u.Descriptor,u.Descriptor).ToArray();Check(offered.Any(x=>x.Param.Blueprint==spell)&&offered.All(x=>!ReferenceEquals(x.Icon,null)),"real native parameter list includes known spells and source icons");
            var pool=N<BlueprintAbilityResource>(c,"cac948cbbe79b55459459dd6a8fe44ce");u.Resources.Add(pool,true);var mode=Buff(u,A<BlueprintBuff>(c,"SpellBenderBuff01447"));var component=mode.Blueprint.GetComponent<SpellBender01447>();int amount=u.Resources.GetResourceAmount(pool);var cast=(RuleCastSpell)H("Cast",u,u,book,spell,19);
            using(Scope(mode,component))component.OnEventAboutToTrigger(cast);Check(cast.Context.HasMetamagic(Metamagic.Selective)&&u.Resources.GetResourceAmount(pool)==amount-1,"native resource spends once and adds execution-only Selective");using(Scope(mode,component)){component.OnEventAboutToTrigger(cast);component.OnEventDidTrigger(cast);}Check(u.Resources.GetResourceAmount(pool)==amount&&!cast.Context.HasMetamagic(Metamagic.Selective),"duplicate notification does not double-charge; failed cast refunds");mode.Remove();
        }
    }
    static int opportunities;static UnitEntityData forbidden;
    static bool Engagement(ref UnitCombatEngagementController __result){__result=new UnitCombatEngagementController();return false;}
    static bool Opportunity(UnitEntityData __0,UnitEntityData __1){Check(__0!=forbidden,"CotW actual Terror excludes caster from AoO list");opportunities++;return false;}
    static void Phantasm(BlueprintsCache parent,Harmony h) {
        var c=Fresh(parent);Register(c,Modules[3]);h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper),"IsActive"),prefix:new HarmonyMethod(typeof(SpellBatch01447Tests),nameof(False)));h.CreateClassProcessor(T("PhantasmFlanking01447Patch")).Patch();
        var cls=N<BlueprintCharacterClass>(c,"ba34257984f4c41408ce1dc2004e342e");var u=Unit(c,cls.AssetGuid.ToString());u.Stats.Intelligence.BaseValue=24;
        Check(A<BlueprintAbilityResource>(c,"PhantasmTerrorResource01447").GetMaxAmount(u.Descriptor)==10&&A<BlueprintAbilityResource>(c,"PhantasmAuraResource01447").GetMaxAmount(u.Descriptor)==19,"native 3+Int uses and wizard-level aura rounds");
        var target=Unit(c,cls.AssetGuid.ToString());var b=Buff(target,A<BlueprintBuff>(c,"PhantasmAuraEffect01447"),u);Check(target.State.HasCondition(UnitCondition.Slowed)&&target.State.HasCondition(UnitCondition.DisableAttacksOfOpportunity)&&target.CombatState.IsFlanked,"native aura conditions and real installed flanking getter");target.State.Features.CannotBeFlanked.Retain();Check(!target.CombatState.IsFlanked,"native CannotBeFlanked remains authoritative");target.State.Features.CannotBeFlanked.Release();b.Remove();Check(!target.State.HasCondition(UnitCondition.Slowed)&&!target.State.HasCondition(UnitCondition.DisableAttacksOfOpportunity),"aura conditions remove cleanly");
        var area=A<BlueprintAbilityAreaEffect>(c,"PhantasmAuraArea01447");Check(area.Fx!=null&&area.Fx.AssetId=="dfadb7fa26de0384d9d9a6dabb0bea72"&&area.Size.Value==30,"original CotW visual FX link and original thirty-foot aura");
        var book=Book(u,cls);var delivery=A<BlueprintAbility>(c,"PhantasmTerrorTouch01447");var wrapper=A<BlueprintAbility>(c,"PhantasmTerrorAbility01447");Check(wrapper.GetComponent<AbilityEffectStickyTouch>().TouchDeliveryAbility==delivery&&delivery.GetComponent<AbilityDeliverTouch>()!=null,"native held-touch wrapper and real melee-touch delivery retained");
        var controllerGetter=AccessTools.PropertyGetter(typeof(Game),"CombatEngagementController");h.Patch(controllerGetter,prefix:new HarmonyMethod(typeof(SpellBatch01447Tests),nameof(Engagement)));var ao=AccessTools.Method(typeof(UnitCombatEngagementController),"ForceAttackOfOpportunity");h.Patch(ao,prefix:new HarmonyMethod(typeof(SpellBatch01447Tests),nameof(Opportunity)));
        try{var ally=Unit(c,cls.AssetGuid.ToString());var engaged=(Dictionary<UnitEntityData,TimeSpan>)Get(target.CombatState,"m_EngagedBy");engaged.Add(u,TimeSpan.Zero);engaged.Add(ally,TimeSpan.Zero);for(int hd=18;hd<=20;hd++){target.Progression.Classes[0].Level=hd;Set(target.Progression,"m_CharacterLevel",null);var evt=Cast(u,target,book,delivery);opportunities=0;forbidden=u;using(evt.Context.GetDataScope(new TargetWrapper(target)))delivery.GetComponent<AbilityEffectRunAction>().Actions.Run();Check(opportunities==(hd<=19?1:0),"actual Terror action wizard/target HD boundary "+hd);}}finally{h.Unpatch(ao,AccessTools.Method(typeof(SpellBatch01447Tests),nameof(Opportunity)));h.Unpatch(controllerGetter,AccessTools.Method(typeof(SpellBatch01447Tests),nameof(Engagement)));forbidden=null;}
        Fact(u,A<BlueprintFeature>(c,"PhantasmAuraFeature01447"));var toggle=u.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.AssetGuid==Id("PhantasmAuraToggle01447"));var pool=A<BlueprintAbilityResource>(c,"PhantasmAuraResource01447");int rounds=u.Resources.GetResourceAmount(pool);toggle.IsOn=true;toggle.TryStart();Check(toggle.IsStarted,"actual native Bedeveling Aura starts");toggle.OnNewRound();Check(u.Resources.GetResourceAmount(pool)==rounds-1,"actual native Bedeveling Aura spends exactly one round");var host=toggle.AppliedBuff;toggle.TurnOffImmediately();Check(!toggle.IsStarted&&toggle.AppliedBuff==null,"native aura stops its active resource consumer");AccessTools.Method(typeof(BuffCollection),"TickBuff").Invoke(u.Buffs,new object[]{host,TimeSpan.FromHours(1)});Check(!u.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==Id("PhantasmAuraHost01447")),"native aura stops with no caster host left");
        var carrier=A<BlueprintBuff>(c,"PhantasmAuraHost01447");Check(!carrier.GetComponents<AddCondition>().Any()&&carrier.GetComponent<SpellDescriptorComponent>()==null,"aura carrier cannot impose its enemy conditions or mind flags on the caster");
    }

    static RuleCastSpell Cast(UnitEntityData u,UnitEntityData t,Spellbook b,BlueprintAbility s)=>(RuleCastSpell)H("Cast",u,t,b,s,19);
    static void Nature(BlueprintsCache parent,Harmony h) {
        var c=Fresh(parent);if(c.Load(Id("PhrenicAmplificationSelection"))==null)Call(T("PsychicAmplifications"),"Register",c);Register(c,Modules[2]);
        foreach(string p in new[]{"NatureSpellImmunity01447Patch","NatureBuffImmunity01447Patch","NatureAmplificationSwitch01447Patch"})h.CreateClassProcessor(T(p)).Patch();
        var cls=N<BlueprintCharacterClass>(c,"bd97b2e1454a44ba9b0a294edf50f79e");if(cls==null)cls=(BlueprintCharacterClass)c.Load(BlueprintGuid.Parse((string)AccessTools.Field(T("PsychicClass"),"ClassId").GetRawConstantValue()));
        var root=A<BlueprintArchetype>(c,"MagaambyanTelepathArchetype01447");Check(root.RemoveFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,11}),"Magaambyan replaces initial discipline/spells and exactly two amplification choices");
        var selector=A<BlueprintFeatureSelection>(c,"MagaambyanDisciplineSelection01447");Check(selector.AllFeatures.Count()==3&&selector.AllFeatures.OfType<BlueprintProgression>().All(p=>!p.LevelEntries.SelectMany(e=>e.Features).Any(f=>f.GetComponents<AddKnownSpell>().Any())),"three original disciplines retain powers without bonus-spell grants");
        for(int circle=1;circle<=9;circle++){var choice=A<BlueprintFeatureSelection>(c,"MagaambyanPrimalSpell"+circle+"01447");var param=(BlueprintParametrizedFeature)choice.AllFeatures.Single();Check(param.SpellLevel==circle&&param.GetComponent<LearnSpellParametrized>().SpellLevel==circle,"native Druid spell choice circle "+circle);}
        // Scene-only bone/view callbacks do not participate in plant immunity.
        foreach(var type in typeof(BlueprintsCache).Assembly.GetTypes().Where(t=>t.Name=="ReplaceSourceBone"||t.Name=="ReplaceCastSource"))foreach(var m in type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(m=>m.Name=="OnTurnOn"||m.Name=="OnTurnOff"))h.Patch(m,prefix:new HarmonyMethod(typeof(SpellBatch01447Tests),nameof(Skip)));
        var plantId=(string)AccessTools.Field(T("MagaambyanTelepath01447"),"PlantId").GetRawConstantValue();var plant=Unit(c,cls.AssetGuid.ToString());Fact(plant,N<BlueprintFeature>(c,plantId));
        var caster=Unit(c,cls.AssetGuid.ToString(),10);Set(plant,"m_Position",new UnityEngine.Vector3(3,0,0));var book=Book(caster,cls);var mind=N<BlueprintAbility>(c,"3c17035ec4717674cae2e841a190e757");var cast=Cast(caster,plant,book,mind);var part=plant.Get<UnitPartSpellResistance>();Check(part!=null&&part.IsImmune(cast.Context,false),"actual native Plant feature grants mind-affecting spell immunity");
        var mode=Buff(caster,A<BlueprintBuff>(c,"NaturesCommandBuff01447"));var comp=mode.Blueprint.GetComponent<NaturesCommand01447>();var wis=(string)AccessTools.Field(T("PsychicClass"),"WisdomPoolId").GetRawConstantValue();var pool=N<BlueprintAbilityResource>(c,wis);caster.Resources.Add(pool,true);caster.Stats.Wisdom.BaseValue=24;caster.Resources.Restore(pool,99);int count=caster.Resources.GetResourceAmount(pool);
        using(Scope(mode,comp))comp.OnEventAboutToTrigger(cast);Check(caster.Resources.GetResourceAmount(pool)==count-2&&!part.IsImmune(cast.Context,false),"paid native Psychic spell overcomes only plant mind immunity, spending two points");
        var immune=plant.Facts.List.SelectMany(f=>f.Components).Where(r=>r.SourceBlueprintComponent is BuffDescriptorImmunity bi&&(bi.Descriptor.Value&SpellDescriptor.MindAffecting)!=0).ToArray();Check(immune.Length>0,"actual plant buff-immunity graph exists");foreach(var r in immune)using(r.RequestEventContext())Check(!(bool)AccessTools.Method(typeof(BuffDescriptorImmunity),"IsImmune").Invoke(r.SourceBlueprintComponent,new object[]{cast.Context}),"native BuffDescriptorImmunity accepts paid Nature's Command");
        int extra=part.AddImmunity(SpellImmunityType.SpellDescriptor,0,null,SpellDescriptor.MindAffecting,null);Check(part.IsImmune(cast.Context,false),"independent additional mind immunity remains effective");part.Remove(extra);
        using(Scope(mode,comp)){comp.OnEventAboutToTrigger(cast);comp.OnEventDidTrigger(cast);}Check(caster.Resources.GetResourceAmount(pool)==count&&part.IsImmune(cast.Context,false),"failed spell refunds exactly once and restores original params/immunity");
        var commandRoot=N<BlueprintAbility>(c,"feb70aab86cc17f4bb64432c83737ac2");var command=commandRoot.GetComponent<AbilityVariants>().Variants.First();if(command.Parent==null)Set(command,"m_Parent",BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(commandRoot));foreach(int level in new[]{10,11}){var u=Unit(c,cls.AssetGuid.ToString(),level);var b=Book(u,cls);var p=N<BlueprintAbilityResource>(c,wis);u.Stats.Wisdom.BaseValue=24;u.Resources.Add(p,true);u.Resources.Restore(p,99);var m=Buff(u,A<BlueprintBuff>(c,"NaturesCommandBuff01447"));var e=Cast(u,plant,b,command);using(Scope(m,m.Blueprint.GetComponent<NaturesCommand01447>()))m.Blueprint.GetComponent<NaturesCommand01447>().OnEventAboutToTrigger(e);Check(part.IsImmune(e.Context,false)==(level<11),"actual Command language gate at Psychic "+level);m.Remove();}
        mode.Remove();PsychicExpansionTests.Inventory["naturesCommand01447Contracts"]=new JObject{["passed"]=true,["nativePlantImmunity"]=true,["nativeBuffImmunity"]=true,["separateImmunityRetained"]=true,["languageLevelBoundary"]=true,["refund"]=true};
    }
    static bool Skip()=>false;
    static bool False(ref bool __result){__result=false;return false;}
    static UnitEntityData[] world;
    static bool Targets(UnityEngine.Vector3 __0,float __1,ref IEnumerable<UnitEntityData> __result){__result=world.Where(u=>(u.Position-__0).sqrMagnitude<=__1*__1);return false;}
    static bool InGame(ref bool __result){__result=true;return false;}
    static void Infectious(BlueprintsCache parent,Harmony h) {
        var c=Fresh(parent);Register(c,Modules[4]);foreach(string p in new[]{"InfectiousSuccess01447Patch","InfectiousDeathSuccess01447Patch","InfectiousParameters01447Patch","InfectiousConsume01447Patch"})h.CreateClassProcessor(T(p)).Patch();
        var method=AccessTools.Method(typeof(GameHelper),"GetTargetsAround",new[]{typeof(UnityEngine.Vector3),typeof(float),typeof(bool),typeof(bool)});h.Patch(method,prefix:new HarmonyMethod(typeof(SpellBatch01447Tests),nameof(Targets)));var inGame=AccessTools.PropertyGetter(typeof(EntityDataBase),"IsInGame");h.Patch(inGame,prefix:new HarmonyMethod(typeof(SpellBatch01447Tests),nameof(InGame)));
        var hostileField=AccessTools.Field(typeof(BardMasterpieces01435Tests),"hostile");var savedHostile=hostileField.GetValue(null);
        try {
            var cls=N<BlueprintCharacterClass>(c,"ba34257984f4c41408ce1dc2004e342e");var caster=Unit(c,cls.AssetGuid.ToString());var book=Book(caster,cls);hostileField.SetValue(null,caster);Fact(caster,A<BlueprintFeature>(c,"InfectiousCharmsFeature01447"));
            var target=Unit(c,cls.AssetGuid.ToString());var near=Unit(c,cls.AssetGuid.ToString());var far=Unit(c,cls.AssetGuid.ToString());Set(caster,"m_Position",new UnityEngine.Vector3(-2,0,0));Set(target,"m_Position",UnityEngine.Vector3.zero);Set(near,"m_Position",new UnityEngine.Vector3(2,0,0));Set(far,"m_Position",new UnityEngine.Vector3(6,0,0));world=new[]{far,target,near};
            var spell=N<BlueprintAbility>(c,"c7104f7526c4c524f91474614054547e");var swift=A<BlueprintAbility>(c,"InfectiousCharms"+spell.AssetGuid+"Swift01447");Check(swift!=null,"actual native single-target Hold Person has a CotW swift option");var restriction=swift.GetComponent<InfectiousSwift01447>();
            Check(!restriction.IsCasterRestrictionPassed(caster),"swift button unavailable without a successful original spell");Call(T("InfectiousCharms01447"),"Applied",target,null);Check(!restriction.IsCasterRestrictionPassed(caster),"failed/no-effect spell grants no window");
            var cast=Cast(caster,target,book,spell);cast.Context.Params.DC=37;cast.Context.Params.CasterLevel=17;var fixture=new BlueprintBuff{AssetGuid=Id("SuccessfulCompulsionFixture01447"),name="SuccessfulCompulsionFixture01447",FxOnStart=new Kingmaker.ResourceLinks.PrefabLink(),FxOnRemove=new Kingmaker.ResourceLinks.PrefabLink(),ComponentsArray=Array.Empty<BlueprintComponent>()};fixture.OnEnable();
            var applied=target.Buffs.AddBuff(fixture,cast.Context,TimeSpan.FromSeconds(60));Check(applied!=null&&restriction.IsCasterRestrictionPassed(caster),"real native AddBuff success opens the three-second swift window");Check(restriction.IsTargetRestrictionPassed(caster,new TargetWrapper(near))&&!restriction.IsTargetRestrictionPassed(caster,new TargetWrapper(far))&&!restriction.IsTargetRestrictionPassed(caster,new TargetWrapper(target)),"nearest legal target only; original and farther target rejected");
            var data=new AbilityData(swift,caster.Descriptor);var p=data.CalculateParams();Check(p.DC==37&&p.CasterLevel==17,"actual AbilityData preview inherits original paid spell parameters");
            var repeat=Cast(caster,near,book,swift);var scope=(IDisposable)Call(T("InfectiousCharms01447"),"Begin",repeat);Check(scope!=null&&restriction.IsTargetRestrictionPassed(caster,new TargetWrapper(near)),"reservation keeps native CanTarget valid during its own cast");var nested=Cast(caster,near,book,swift);Check(Call(T("InfectiousCharms01447"),"Begin",nested)==null&&nested.ForceFail,"same window cannot be reused by a nested second cast");scope.Dispose();Check(!restriction.IsCasterRestrictionPassed(caster)&&!near.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==Id("InfectiousCharms"+spell.AssetGuid+"Target01447")),"completion cleans both caster window and exactly its target marker");
            var noChain=near.Buffs.AddBuff(fixture,repeat.Context,TimeSpan.FromSeconds(60));Check(noChain!=null&&!restriction.IsCasterRestrictionPassed(caster),"repeated swift effect cannot create an infinite chain");
            near.Stats.HitPoints.BaseValue=50;far.Stats.HitPoints.BaseValue=50;var deathSpell=N<BlueprintAbility>(c,"2f8a67c483dfa0f439b293e094ca9e3c");var deathSwift=A<BlueprintAbility>(c,"InfectiousCharms"+deathSpell.AssetGuid+"Swift01447");Check(deathSwift!=null,"native Power Word Kill Compulsion has an infectious variant; school="+deathSpell.School+" descriptor="+deathSpell.SpellDescriptor);var deathBinding=deathSwift.GetComponent<InfectiousSwift01447>();var kill=deathSpell.ElementsArray.OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionKill>().First();
            var green=AccessTools.Method(typeof(GameHelper),"IsAttackingGreenNPC");h.Patch(green,prefix:new HarmonyMethod(typeof(SpellBatch01447Tests),nameof(False)));try{var e=Cast(caster,target,book,deathSpell);target.State.MarkedForDeath=false;using(e.Context.GetDataScope(new TargetWrapper(target)))kill.RunAction();Check(target.State.MarkedForDeath&&deathBinding.IsCasterRestrictionPassed(caster),"actual native kill action/death-mark transition opens no-buff infectious window");var db=caster.Buffs.Enumerable.Single(b=>b.Blueprint==deathBinding.Window.Get());db.Remove();Check(!deathBinding.IsCasterRestrictionPassed(caster),"native no-buff window removes its paired marker");}finally{h.Unpatch(green,AccessTools.Method(typeof(SpellBatch01447Tests),nameof(False)));target.State.MarkedForDeath=false;}
            PsychicExpansionTests.Inventory["infectiousCharms01447Contracts"]=new JObject{["passed"]=true,["nativeBuffSuccessHook"]=true,["nativeNoBuffKillPath"]=true,["nearestLegalTarget"]=true,["originalParams"]=true,["noNestedReuse"]=true,["noChain"]=true,["boundary"]="Native BuffCollection.AddBuff, feature/buff activation, AbilityData parameter query, native restrictions and production reservation/finalizer body execute; the scene neighborhood is bounded by a distance-filtered fixture. Combat saving throws, live scene geometry and rendered swift actionbar are outside this test."};
        }finally{h.Unpatch(method,AccessTools.Method(typeof(SpellBatch01447Tests),nameof(Targets)));h.Unpatch(inGame,AccessTools.Method(typeof(SpellBatch01447Tests),nameof(InGame)));world=null;hostileField.SetValue(null,savedHostile);}
    }
    internal static int Run(BlueprintsCache parent,Harmony h) {
        checks=rollbackCases=privateCount=0;
        var binder=(GuidClassBinder)Kingmaker.Blueprints.JsonSystem.Json.Serializer.Binder;foreach(var type in Main.GetTypes())foreach(var a in type.GetCustomAttributes(typeof(TypeIdAttribute),false).Cast<TypeIdAttribute>())binder.AddToCache(type,a.GuidString);
        var counters=new[]{"cacheFailures","missingFailures","connectFailures"}.ToDictionary(n=>n,n=>AccessTools.Field(typeof(Arcane01446Tests),n).GetValue(null));
        try{if(Environment.GetEnvironmentVariable("COTW_01447_BEHAVIOR_ONLY")!="1")Structural(parent);}finally{foreach(var pair in counters)SetStatic(typeof(Arcane01446Tests),pair.Key,pair.Value);}
        Overseer(parent,h);Specialist(parent,h);Phantasm(parent,h);Nature(parent,h);Infectious(parent,h);
        PsychicExpansionTests.Inventory["spellBatch01447Contracts"]=new JObject{["passed"]=rollbackCases==15,["assertions"]=checks,["newItemCount"]=5,["privateResourceCount"]=privateCount,["rollbackCases"]=rollbackCases};return checks;
    }
}
