using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using CallOfTheWild.Wrath.Prototype;

internal static class AuraOfDoomCasting01444Tests
{
    static int checks, availabilityCalls, contexts;
    static readonly Type Casting = typeof(PsychicSpellbookMarker).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicCasting", true);
    static readonly Type Assets = typeof(PsychicSpellbookMarker).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets", true);
    static readonly SpellDescriptor Fear = SpellDescriptor.Fear | SpellDescriptor.Emotion | SpellDescriptor.MindAffecting | SpellDescriptor.Shaken;
    static object Get(object o,string n) => (AccessTools.Field(o.GetType(),n) ?? AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).GetValue(o);
    static void Set(object o,string n,object v) => (AccessTools.Field(o.GetType(),n) ?? AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).SetValue(o,v);
    static T Empty<T>() where T:class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static T Asset<T>(BlueprintsCache c,string token) where T:SimpleBlueprint => (T)c.Load((BlueprintGuid)AccessTools.Method(Assets,"Id").Invoke(null,new object[]{token}));
    static void Check(bool ok,string reason) { if(!ok)throw new Exception("AuraOfDoomCasting01444: "+reason);checks++; }
    static bool Slots(ref int __result) { __result=1;return false; }
    static void ObserveAvailability() { availabilityCalls++; }
    static bool Blocked(AbilityData data) => (bool)AccessTools.Method(Casting,"IsEmotionBlocked").Invoke(null,new object[]{data});
    static MechanicsContext Context(UnitEntityData owner,UnitEntityData caster,BlueprintScriptableObject bp,MechanicsContext parent=null) {
        contexts++;var c=new MechanicsContext(caster,owner.Descriptor,bp,parent);c.SetParams(new AbilityParams{CasterLevel=12,SpellLevel=4,DC=23});return c;
    }
    static Buff Receipt(UnitEntityData owner,BlueprintBuff bp,MechanicsContext context) {
        var b=Empty<Buff>();Set(b,"Blueprint",bp);Set(b,"m_Context",context);Set(b,"IsTurnedOn",true);Set(b,"IsActive",true);
        owner.Buffs.Enumerable.Add(b);return b;
    }
    static AbilityData Spell(UnitEntityData unit,BlueprintSpellbook blueprint,BlueprintAbility probe) {
        var book=Empty<Spellbook>();Set(book,"Blueprint",blueprint);Set(book,"Owner",unit.Descriptor);
        var data=new AbilityData(probe,unit.Descriptor);Set(data,"m_CachedSpellbook",book);return data;
    }
    static int ThoughtPenalty(UnitEntityData unit,AbilityData data) {
        var r=Empty<RuleCalculateAbilityParams>();Set(r,"Initiator",unit);Set(r,"Spellbook",data.Spellbook);Set(r,"AbilityData",data);
        AccessTools.Method(Casting,"ApplyThoughtPenalty").Invoke(null,new object[]{r});return (int)Get(r,"m_BonusConcentration");
    }
    internal static int Run(BlueprintsCache cache) {
        checks=availabilityCalls=contexts=0;
        var doom=Asset<BlueprintAbility>(cache,"AuraOfDoom");var host=Asset<BlueprintBuff>(cache,"AuraOfDoomBuff");var area=Asset<BlueprintAbilityAreaEffect>(cache,"AuraOfDoomArea");var shaken=Asset<BlueprintBuff>(cache,"AuraOfDoomShakenBuff");
        var book=(BlueprintSpellbook)cache.Load(BlueprintGuid.Parse("3ce26d41e96b5ca289f0c6ce9db28d3c"));
        var originalComponents=new BlueprintScriptableObject[]{doom,host,area,shaken}.Select(b=>b.ComponentsArray).ToArray();
        var h=new Harmony("CotW.AuraOfDoomCasting01444.Tests");
        var units=new System.Collections.Generic.List<UnitEntityData>();
        try {
            h.CreateClassProcessor(Casting.GetNestedType("EmotionPatch",BindingFlags.NonPublic)).Patch();
            // AbilityData's tiny forwarding method can already be inlined by
            // earlier suites. Seed the actual slot-count service instead.
            h.Patch(AccessTools.Method(typeof(AbilityData).Assembly.GetType("Kingmaker.UnitLogic.Abilities.AbilityCastRateUtils",true),"GetAvailableForCastCount",new[]{typeof(AbilityData)}),prefix:new HarmonyMethod(typeof(AuraOfDoomCasting01444Tests),nameof(Slots)));
            h.Patch(AccessTools.PropertyGetter(typeof(AbilityData),"IsAvailableForCast"),prefix:new HarmonyMethod(typeof(AuraOfDoomCasting01444Tests),nameof(ObserveAvailability)));
            UnitEntityData Unit(string name) {
                var u=(UnitEntityData)AccessTools.Method(typeof(FlameJetLifecycle01430Tests),"Unit").Invoke(null,new object[]{cache,"doom044-"+name});
                units.Add(u);return u;
            }
            var caster=Unit("caster");var target=Unit("target");var other=Unit("other");
            var probe=new BlueprintAbility{Type=AbilityType.Spell,ActionType=UnitCommand.CommandType.Standard,ComponentsArray=Array.Empty<BlueprintComponent>()};probe.OnEnable();
            var data=Spell(caster,book,probe);var targetData=Spell(target,book,probe);
            bool healthy=data.IsAvailableForCast&&!Blocked(data);
            if(!healthy) {
                Console.WriteLine("DOOM fixture baseline: blocked="+Blocked(data)+", stateCanCast="+caster.State.CanCast+", count="+data.GetAvailableForCastCount()+", buffs="+string.Join(";",caster.Buffs.Enumerable.Select(b=>b.Blueprint.name+":"+b.Context?.SpellDescriptor)));
                foreach(string name in new[]{"CanBeCastByCaster","IsCastByServiceCaster","HasRequiredParams","TemporarilyDisabled","ShouldDelegateToMount","UseMagicDeviceDC"})try{Console.WriteLine("DOOM native "+name+"="+AccessTools.PropertyGetter(typeof(AbilityData),name).Invoke(data,null));}catch(Exception e){Console.WriteLine(name+" diagnostic="+e.Message);}
                Console.WriteLine("DOOM conditions="+string.Join(",",((sbyte[])Get(caster.State,"m_Conditions")).Select((x,i)=>i+":"+x).Where(x=>!x.EndsWith(":0"))));
            }
            Check(healthy,"healthy psychic passes actual native availability plus production emotion postfix");
            Check(doom.ActionType==UnitCommand.CommandType.Standard&&host.ComponentsArray.OfType<AddAreaEffect>().Single().AreaEffect==area,"one standard-action spell leaves an attached aura, not a sustained command");
            Check((doom.GetComponent<SpellDescriptorComponent>().Descriptor&Fear)==Fear&&(area.GetComponent<SpellDescriptorComponent>().Descriptor&Fear)==Fear&&(shaken.GetComponent<SpellDescriptorComponent>().Descriptor&Fear)==Fear,"spell, area and enemy debuff retain their fear/emotion/mind-affecting metadata");
            Check(host.GetComponent<SpellDescriptorComponent>()==null,"emitter blueprint is not itself a fear debuff");
            var root=Context(caster,caster,doom);var hostContext=Context(caster,caster,host,root);var areaContext=Context(caster,caster,area,hostContext);
            Check((hostContext.SpellDescriptor&Fear)==Fear,"native constructor reproduces inherited fear on caster's emitter context");
            var active=Receipt(caster,host,hostContext);
            Check(data.IsAvailableForCast&&!Blocked(data),"actual native availability remains true with own Aura of Doom active");
            Check(hostContext.SpellDescriptor==Fear&&ReferenceEquals(active.Context,hostContext),"fix reads the emitter role without mutating shared or saved contexts");
            Check(!caster.State.HasCondition(UnitCondition.SpellcastingForbidden),"aura has no global spellcasting condition");
            Check(ThoughtPenalty(caster,data)==-10,"aura does not change psychic thought concentration penalty");
            var center=Receipt(caster,(BlueprintBuff)cache.Load(BlueprintGuid.Parse((string)AccessTools.Field(typeof(PsychicSpellbookMarker).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicClass"),"CenterBuffId").GetValue(null))),Context(caster,caster,host));
            Check(ThoughtPenalty(caster,data)==0&&data.IsAvailableForCast,"Center Self still removes only the thought penalty while aura is active");caster.Buffs.Enumerable.Remove(center);
            // The native property is serialized. Seed its historical numeric
            // value exactly; a whole game save/scene is outside this fixture.
            Check(typeof(MechanicsContext).GetProperty("SpellDescriptor").GetCustomAttributes(typeof(JsonPropertyAttribute),true).Any(),"actual native context descriptor is a persisted property");
            var saved=JObject.Parse("{\"SpellDescriptor\":"+(long)Fear+"}");
            Set(hostContext,"SpellDescriptor",(SpellDescriptor)(long)saved["SpellDescriptor"]);
            Check(data.IsAvailableForCast&&hostContext.SpellDescriptor==Fear,"persisted old descriptor value is harmless on the exact emitter without migration or recasting");
            foreach(var flags in new[]{SpellDescriptor.Fear,SpellDescriptor.Shaken,SpellDescriptor.Frightened,SpellDescriptor.NegativeEmotion}) {
                var negative=new BlueprintBuff{AssetGuid=BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),ComponentsArray=Array.Empty<BlueprintComponent>()};negative.OnEnable();
                var context=Context(caster,other,negative);context.AddSpellDescriptor(flags);var receipt=Receipt(caster,negative,context);
                Check(!data.IsAvailableForCast&&Blocked(data),"independent hostile context still blocks "+flags);
                Set(receipt,"IsSuppressed",true);Check(data.IsAvailableForCast,"suppression of only hostile receipt restores casting "+flags);Set(receipt,"IsSuppressed",false);
                data.MetamagicData=new MetamagicData();data.MetamagicData.Add((Metamagic)(1<<29));Check(data.IsAvailableForCast&&!Blocked(data),"Logical Spell still bypasses real emotion restriction "+flags);data.MetamagicData=null;
                caster.Buffs.Enumerable.Remove(active);Check(!data.IsAvailableForCast,"ending only aura never clears independent fear "+flags);caster.Buffs.Enumerable.Add(active);
                caster.Buffs.Enumerable.Remove(receipt);Check(data.IsAvailableForCast,"removing only hostile receipt restores casting with aura retained "+flags);
            }
            var targetContext=Context(target,caster,shaken,areaContext);var enemyDebuff=Receipt(target,shaken,targetContext);
            Check(!targetData.IsAvailableForCast&&Blocked(targetData)&&data.IsAvailableForCast,"enemy aura victim is blocked while emitter can cast");
            target.Buffs.Enumerable.Remove(enemyDebuff);Check(targetData.IsAvailableForCast,"enemy effect removal restores only victim casting");
            var explicitHost=new BlueprintBuff{AssetGuid=host.AssetGuid,ComponentsArray=new BlueprintComponent[]{new SpellDescriptorComponent{Descriptor=SpellDescriptor.Fear}}};explicitHost.OnEnable();
            caster.Buffs.Enumerable.Remove(active);var explicitDebuff=Receipt(caster,explicitHost,Context(caster,caster,explicitHost,root));Check(!data.IsAvailableForCast,"exact emitter ID never hides an explicit harmful blueprint descriptor");caster.Buffs.Enumerable.Remove(explicitDebuff);caster.Buffs.Enumerable.Add(active);
            var conditions=(sbyte[])Get(caster.State,"m_Conditions");conditions[(int)UnitCondition.SpellcastingForbidden]=1;
            Check(!data.IsAvailableForCast,"unrelated global anti-casting condition remains enforced with aura");conditions[(int)UnitCondition.SpellcastingForbidden]=0;Check(data.IsAvailableForCast,"release of unrelated condition restores actual availability");
            var ordinary=new BlueprintSpellbook{ComponentsArray=Array.Empty<BlueprintComponent>()};var ordinaryData=Spell(caster,ordinary,probe);Check(ordinaryData.IsAvailableForCast&&!Blocked(ordinaryData),"nonpsychic book stays unaffected");
            caster.Buffs.Enumerable.Remove(active);Check(data.IsAvailableForCast&&caster.Buffs.Enumerable.Count==0,"aura removal leaves no orphan restriction or receipt");
            Check(new BlueprintScriptableObject[]{doom,host,area,shaken}.Select((b,i)=>ReferenceEquals(b.ComponentsArray,originalComponents[i])).All(x=>x),"all published aura graph component arrays retain exact identity");
            var contract=new JObject{["passed"]=true,["assertions"]=checks,["nativeAvailabilityCalls"]=availabilityCalls,["nativeContexts"]=contexts,["legacyContextDescriptorInput"]=true,["hostOnlyContextExemption"]=true,["hostExplicitDescriptorPreserved"]=true,["enemyAndForeignFearPreserved"]=true,["logicalAndThoughtRulesPreserved"]=true,["globalCastingRestrictionsPreserved"]=true,["blueprintGraphsUnchanged"]=true,["boundary"]="Actual native MechanicsContext constructors and AbilityData.IsAvailableForCast plus production EmotionPatch execute. Native entity fixture and buff receipts are constructed; spell-slot count is seeded to one. No availability getter or emotion helper is replaced. Legacy serialized descriptor is seeded through its actual native property; full save/scene serialization and real cast animation are outside this host. Existing tactical suite independently verifies native area entry/exit, enemy/save gates, overlap ownership and duration."};
            PsychicExpansionTests.Inventory["auraOfDoomCasting01444Contracts"]=contract;
            Console.WriteLine("PASS: "+checks+" AuraOfDoomCasting01444 contracts; native availability "+availabilityCalls+", contexts "+contexts);return checks;
        } finally {foreach(var u in units)u.Buffs.Enumerable.Clear();h.UnpatchAll(h.Id);}
    }
}
