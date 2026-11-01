using System;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    const string Psychic058="83fa3b50138c566698801ba5967bbde0";
    static void WarmMercies058(BlueprintsCache c){foreach(var mercy in N<BlueprintFeatureSelection>(c,"02b187038a8dce545bb34bbfb346428d").AllFeatures)foreach(var prerequisite in mercy.GetComponents<PrerequisiteFeature>())Check(prerequisite.Feature!=null,"actual native mercy prerequisite resolves before cloning: "+mercy.name);}
    static AbilityExecutionContext Super058(UnitEntityData owner,BlueprintAbility bp,UnitEntityData target=null)
    {
        var a=new AbilityData(bp,owner.Descriptor);var p=a.CalculateParams();var recipient=new TargetWrapper(target??owner);AbilityCastRateUtils.SpendResources(a);
        var context=new AbilityExecutionContext(a,p,recipient,new RulebookEventContext(),owner);AbilityExecutionProcess.PrepareCast(context);bp.GetComponent<AbilityEffectRunAction>().Apply(context,recipient);return context;
    }
    static void Disciplines058(BlueprintsCache c,Harmony h)
    {
        var cls=N<BlueprintCharacterClass>(c,Psychic058);var pool=A<BlueprintFeatureSelection>(c,"DisciplineSelection");
        foreach(string key in new[]{"Pain","Psychedelia","Abomination"}){
            var p=A<BlueprintProgression>(c,key+"Discipline01458");Check(pool.AllFeatures.Count(v=>v==p)==1,"actual discipline parent publishes one "+key);
            Check(p.LevelEntries.SelectMany(e=>e.Features.SelectMany(f=>f.GetComponents<AddKnownSpell>()).Select(s=>new{Level=e.Level,Spell=s.SpellLevel})).OrderBy(v=>v.Spell).Select(v=>v.Level).SequenceEqual(new[]{1,4,6,8,10,12,14,16,18}),"original nine bonus-spell level entries retained: "+key);
            Check(p.LevelEntries.Any(e=>e.Level==5)&&p.LevelEntries.Any(e=>e.Level==13),"original fifth/thirteenth level powers retained: "+key);
        }
        var heal=AccessTools.Method(typeof(RuleHealDamage),nameof(RuleHealDamage.OnTrigger));var oldHeal=(Harmony.GetPatchInfo(heal)?.Prefixes.AsEnumerable()??Enumerable.Empty<Patch>()).Where(p=>p.PatchMethod.DeclaringType==typeof(Psychic070Tests)&&p.PatchMethod.Name=="CaptureHealing").ToArray();
        // StatusBuffs is a Unity ScriptableObject asset (the HUD attribute-
        // damage indicators), absent from this standalone scene. Supply an
        // empty presentation table; native stat-damage and healing bodies run.
        Kingmaker.Blueprints.Root.BlueprintRoot root;active=null;try{root=Kingmaker.Blueprints.Root.BlueprintRoot.Instance;}finally{active=c;}
        // Retain the existing scene-root fixture; the lazy native blueprint
        // reader cannot hydrate Unity's unrelated AnimationCurve settings.
        native[root.AssetGuid]=root;var oldStatus=root.StatusBuffs;var status=(Kingmaker.Blueprints.Root.StatusBuffsRoot)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Kingmaker.Blueprints.Root.StatusBuffsRoot));status.Entries=Array.Empty<Kingmaker.Blueprints.Root.StatDamageEntry>();root.StatusBuffs=status;
        foreach(var p in oldHeal)h.Unpatch(heal,p.PatchMethod);
        try{Pain058(c,h);OptionalSpell058(c,h);Cognatogens058(c,h);Abomination058(c,h);}
        finally{root.StatusBuffs=oldStatus;foreach(var p in oldHeal)new Harmony(p.owner).Patch(heal,prefix:new HarmonyMethod(p.PatchMethod){priority=p.priority,before=p.before,after=p.after});}
        PsychicExpansionTests.Inventory["psychicDisciplines01458Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["coverage"]=new JArray(Coverage),["gameLaunchTested"]=false,["boundary"]="Actual native ability parameters, resources, action lists, feature/buff lifecycle and discovery prerequisites. Scene geometry, rendered visuals and complete level-up/save transport remain tester observations."};
    }
    static void Pain058(BlueprintsCache c,Harmony h)
    {
        var cls=N<BlueprintCharacterClass>(c,Psychic058);var live=A<BlueprintProgression>(c,"LiveOnProgression01458");var bp=A<BlueprintAbility>(c,"LiveOnAbility01458");var resource=A<BlueprintAbilityResource>(c,"LiveOnResource01458");
        Check(bp.Range==AbilityRange.Personal&&bp.CanTargetSelf&&!bp.CanTargetFriends&&!bp.CanTargetEnemies,"Live On remains genuinely self-only, including native targeting flags");
        Check(bp.ComponentsArray.All(v=>v.GetType().Name!="AbilityCasterAlignment"),"Live On drops the paladin alignment restriction as original CotW requires");
        var mercy=A<BlueprintFeatureSelection>(c,"LiveOnMercySelection01458");var native=N<BlueprintFeatureSelection>(c,"02b187038a8dce545bb34bbfb346428d");
        Check(mercy.AllFeatures.Length==native.AllFeatures.Length&&live.LevelEntries.Select(e=>e.Level).SequenceEqual(new[]{6,9,12,15,18}),"private complete native mercy graph and delayed mercy levels");
        foreach(int level in new[]{5,6,7,8,9,12,15,18,20}){
            var u=EquipmentUnit(c,level,Psychic058);u.Stats.Charisma.BaseValue=16;u.Stats.HitPoints.BaseValue=200;u.Damage=100;Grant(u,live);
            Check(resource.GetMaxAmount(u.Descriptor)==(level-3)/2+3,"native Live On daily uses use effective paladin level and Charisma: "+level);
            var a=new AbilityData(bp,u.Descriptor);var p=a.CalculateParams();Check(p.CasterLevel==level-3,"actual native parameter event limits Live On caster level: "+level);
            var context=Super058(u,bp);int dice=context[AbilityRankType.Default];Check(dice==(level-3)/2,"native Live On healing dice use effective paladin level: "+level);Console.WriteLine("058 Live On diagnostics level="+level+" damage="+u.Damage+" rank="+dice+" shared="+context[AbilitySharedValue.Damage]+" values="+string.Join(";",bp.GetComponents<Kingmaker.UnitLogic.Mechanics.Components.ContextCalculateSharedValue>().Select(v=>v.ValueType+"/"+v.Modifier+"/"+v.Value.DiceType+"/"+v.Value.DiceCountValue.Calculate(context)+"/"+v.Calculate(context)))+" sources="+((System.Collections.ICollection)AccessTools.Field(typeof(MechanicsContext),"m_ValueSources").GetValue(context))?.Count);Check(u.Damage<100&&u.Damage>=100-6*dice,"actual native self-heal action changes real HP within correct dice bounds: "+level);
            Check(u.Resources.GetResourceAmount(resource)==resource.GetMaxAmount(u.Descriptor)-1,"actual native Live On ability pays one daily use: "+level);
            foreach(var original in native.AllFeatures){var privateMercy=A<BlueprintFeature>(c,"LiveOnMercy"+original.AssetGuid+"01458");var nativeLevel=original.GetComponents<PrerequisiteClassLevel>().FirstOrDefault();if(nativeLevel!=null)Check(privateMercy.GetComponents<PrerequisiteClassLevel>().Any(req=>req.CharacterClass==cls&&req.Level==nativeLevel.Level+3),"private mercy level offset leaves original feature untouched: "+level+"/"+original.name);}
        }
        var healer=EquipmentUnit(c,6,Psychic058);Grant(healer,live);Grant(healer,c,"LiveOnMercye5aa306af9b91974a9b2f2cbe702f56201458");var fatigue=Add(healer,N<BlueprintBuff>(c,"e6f2fc5d73d88064583cb828801212f4"));
        Super058(healer,bp);Check(!fatigue.IsAttached&&!healer.State.HasCondition(UnitCondition.Fatigued),"actual native Live On action runs its private selected fatigue mercy");
        foreach(int level in new[]{11,12}){
            var mixed=EquipmentUnit(c,level,Psychic058);var nativeFatigue=N<BlueprintFeature>(c,"e5aa306af9b91974a9b2f2cbe702f562");var privateFatigue=A<BlueprintFeature>(c,"LiveOnMercy"+nativeFatigue.AssetGuid+"01458");var privateExhausted=A<BlueprintFeature>(c,"LiveOnMercy25641bda25467224e930e8c70eaf9a8301458");
            Check(!Pick058(mixed,mercy,privateExhausted),"native private higher mercy requires its matching private lower mercy: "+level);Grant(mixed,nativeFatigue);Check(!Pick058(mixed,mercy,privateExhausted),"warmed native paladin prerequisite cannot satisfy private Psychic mercy through cached reference: "+level);Grant(mixed,privateFatigue);Check(Pick058(mixed,mercy,privateExhausted)==(level>=12),"native private mercy qualification preserves both class offset and actual private prerequisite: "+level);
            var paladin=N<BlueprintCharacterClass>(c,"bfa11238e7ae3544bbeb4d0b92e897ec");mixed.Progression.Classes.Add(new ClassData(paladin){Level=9});Check(Pick058(mixed,native,N<BlueprintFeature>(c,"25641bda25467224e930e8c70eaf9a83")),"actual native Paladin mercy remains available through its own class and unchanged lower mercy: "+level);
        }
        var reminder=A<BlueprintAbility>(c,"PainfulReminderAbility01458");foreach(int level in new[]{1,7,8,14,15,20}){var u=EquipmentUnit(c,level,Psychic058);var a=new AbilityData(reminder,u.Descriptor);var context=new AbilityExecutionContext(a,a.CalculateParams(),new TargetWrapper(u),new RulebookEventContext(),u);AbilityExecutionProcess.PrepareCast(context);Check(context[AbilityRankType.Default]==(level<8?1:level<15?2:3),"native Painful Reminder rank boundaries "+level);}
    }
    static void Cognatogens058(BlueprintsCache c,Harmony h)
    {
        var native=N<BlueprintFeature>(c,"e3f460ea61fcc504183c7d6818bbbf7a");var own=A<BlueprintFeature>(c,"PsychedeliaCognatogenFeature01458");var resource=N<BlueprintAbilityResource>(c,"3b163587f010382408142fc8a97852b6");var greater=N<BlueprintFeature>(c,"18eb29676492e844eb5a55d1c855ce69");var grand=N<BlueprintFeature>(c,"af4a320648eb5724889d6ff6255090b2");
        Check(own.GetComponent<AddFacts>().Facts.Single()==native,"Psychedelia grants original native Cognatogen rather than parallel duplicate abilities");
        foreach(var ability in native.GetComponent<AddFacts>().Facts.OfType<BlueprintAbility>()){
            var u=EquipmentUnit(c,7,Psychic058);Grant(u,own);Check(u.HasFact(native)&&u.Abilities.Enumerable.Count(v=>v.Blueprint==ability)==1,"actual native discovery ownership and single ability: "+ability.name);
            var context=Super058(u,ability);Check(context.Params.CasterLevel==7&&u.Resources.GetResourceAmount(resource)==0,"actual native cognatogen cast has Psychic level and shared one-use payment: "+ability.name);
            var effect=u.Buffs.Enumerable.Single(v=>v.Blueprint.name.StartsWith("Cognatogen")&&v.IsActive);Check(effect.TimeLeft==TimeSpan.FromMinutes(7),"actual native cognatogen buff lasts one minute per Psychic level: "+ability.name);
            Check(effect.Blueprint.GetComponents<AddStatBonus>().Any(v=>v.Value==4)&&effect.Blueprint.GetComponents<AddStatBonus>().Any(v=>v.Value==-2),"native mental bonus and physical penalty retained: "+ability.name);
            var penalty=effect.Blueprint.GetComponents<AddStatBonus>().Single(v=>v.Value==-2).Stat;effect.Remove();Check(((Kingmaker.EntitySystem.Stats.ModifiableValueAttributeStat)u.Stats.GetStat(penalty)).Damage==2,"actual native cognatogen expiry deals two physical ability damage: "+ability.name);
        }
        var mixed=EquipmentUnit(c,7,Psychic058);mixed.Progression.Classes.Add(new ClassData(N<BlueprintCharacterClass>(c,"0937bec61c0dabc468428f496580c721")){Level=16});Grant(mixed,own);
        Check(greater.GetComponents<Prerequisite>().All(p=>p.Check(null,mixed.Descriptor,null)),"actual native Greater Cognatogen prerequisites accept real Psychedelia Cognatogen and sufficient Alchemist level");Grant(mixed,greater);
        Check(grand.GetComponents<Prerequisite>().All(p=>p.Check(null,mixed.Descriptor,null)),"actual native Grand Cognatogen prerequisites preserve Greater and Alchemist16 requirements");Grant(mixed,grand);
        var abilityGrand=grand.GetComponent<AddFacts>().Facts.OfType<BlueprintAbility>().First();mixed.Resources.Restore(resource,1);var ctx=Super058(mixed,abilityGrand);
        Check(ctx.Params.CasterLevel==23&&mixed.Buffs.Enumerable.Any(v=>v.Blueprint.name.StartsWith("GrandCognatogen")&&v.TimeLeft==TimeSpan.FromMinutes(23)),"native Grand discovery uses stacked classes while retaining minute duration");
        foreach(var b in mixed.Buffs.Enumerable.Where(v=>v.Blueprint.name.StartsWith("GrandCognatogen")).ToArray())b.Remove();
        var no=EquipmentUnit(c,7,Psychic058);Grant(no,own);Check(!greater.GetComponents<Prerequisite>().All(p=>p.Check(null,no.Descriptor,null)),"Cognatogen qualification never forges required Alchemist discovery class levels");
        var alch=EquipmentUnit(c,7,"0937bec61c0dabc468428f496580c721");Grant(alch,native);alch.Resources.Add(resource,true);var nativeCtx=Super058(alch,native.GetComponent<AddFacts>().Facts.OfType<BlueprintAbility>().First());Check(nativeCtx.Params.CasterLevel==7&&alch.Buffs.Enumerable.Any(v=>v.Blueprint.name.StartsWith("Cognatogen")&&v.TimeLeft==TimeSpan.FromMinutes(70)),"unrelated native Alchemist retains its original ten-minute-per-level cognatogen");
        foreach(var b in alch.Buffs.Enumerable.Where(v=>v.Blueprint.name.StartsWith("Cognatogen")).ToArray())b.Remove();
        AreaDispatch058(A<BlueprintAbilityAreaEffect>(c,"HallucinogenicAuraArea01458"));
    }
    static void OptionalSpell058(BlueprintsCache c,Harmony h)
    {
        var id=BlueprintGuid.Parse("ff31ae1abe3c418db7842dcc76eca7ee");Check(c.Load(id)==null,"optional EC bonus-spell absence is a real native cache input");var owner=EquipmentUnit(c,12,Psychic058);var book=Book058(c,owner);var feature=A<BlueprintFeature>(c,"PainBonusSpell601458");var fact=Grant(owner,feature);Check(!book.GetKnownSpells(6).Any(v=>v.Blueprint.AssetGuid==id),"actual optional AddKnownSpell activation safely skips missing external spell");
        var runtime=fact.Components.Single(v=>v.SourceBlueprintComponent is OptionalDisciplineSpell01458);
        // Deliberately explicit provider-publication inputs: this tests the
        // CotW availability guard and native known-spell grant, not the EC
        // spell's combat implementation or its actual module initializer.
        var provider=new BlueprintAbility{AssetGuid=id,name="InflictPainMassAbility",Type=AbilityType.Spell,ComponentsArray=new BlueprintComponent[]{new SpellComponent{School=SpellSchool.Enchantment},new AbilityEffectRunAction{Actions=new Kingmaker.ElementsSystem.ActionList{Actions=new Kingmaker.ElementsSystem.GameAction[]{new Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage()}}}}};provider.OnEnable();c.AddCachedBlueprint(id,provider);
        using(runtime.RequestEventContext())((Kingmaker.PubSubSystem.ILevelUpCompleteUIHandler)runtime.SourceBlueprintComponent).HandleLevelUpComplete(owner,false);Check(!book.GetKnownSpells(6).Any(v=>v.Blueprint.AssetGuid==id),"actual native level-up notification skips a full but unpublished disabled-provider spell");
        var list=new BlueprintSpellList{AssetGuid=BlueprintGuid.Parse("dff516cdd538443d89b9161be580a058"),name="058TestProviderPublication",SpellsByLevel=Enumerable.Range(0,10).Select(i=>new SpellLevelList(i)).ToArray()};Set(list.SpellsByLevel[7],"m_Spells",new System.Collections.Generic.List<BlueprintAbilityReference>{BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(provider)});list.OnEnable();c.AddCachedBlueprint(list.AssetGuid,list);var link=new SpellListComponent{SpellLevel=7};Set(link,"m_SpellList",BlueprintReferenceBase.CreateTyped<BlueprintSpellListReference>(list));provider.ComponentsArray=provider.ComponentsArray.Concat(new[]{link}).ToArray();provider.OnEnable();
        try{
            using(runtime.RequestEventContext())((Kingmaker.PubSubSystem.ILevelUpCompleteUIHandler)runtime.SourceBlueprintComponent).HandleLevelUpComplete(owner,false);Check(book.GetKnownSpells(6).Count(v=>v.Blueprint==provider)==1,"actual native level-up grant adds available EC-source spell at Psychic sixth circle despite provider wizard seventh circle");
            using(runtime.RequestEventContext())((Kingmaker.PubSubSystem.ILevelUpCompleteUIHandler)runtime.SourceBlueprintComponent).HandleLevelUpComplete(owner,false);Check(book.GetKnownSpells(6).Count(v=>v.Blueprint==provider)==1,"repeated actual native level-up grant never duplicates optional known spell");
            var fresh=EquipmentUnit(c,12,Psychic058);var freshBook=Book058(c,fresh);var added=Grant(fresh,feature);Check(freshBook.GetKnownSpells(6).Count(v=>v.Blueprint==provider)==1,"actual native initial activation grants already-published optional sixth-circle bonus");fresh.Facts.Remove(added);
        }finally{owner.Facts.Remove(fact);c.RemoveCachedBlueprint(id);c.RemoveCachedBlueprint(list.AssetGuid);}
        PsychicExpansionTests.Inventory["optionalDisciplineSpell01458Contract"]=new JObject{["passed"]=true,["nativeActivationAndLevelUpNotification"]=true,["missingAndDisabledSkipped"]=true,["sourcePublishedCircle"]=7,["psychicBonusCircle"]=6,["providerInputBoundary"]="Explicit native blueprint/reciprocal-list publication fixture. Real EC spell initialization/combat effects are not executed by this contract; current official source and GUID separately audited."};
    }
    static void Abomination058(BlueprintsCache c,Harmony h)
    {
        foreach(int level in new[]{1,4,5,12,13,20}){
            var u=EquipmentUnit(c,level,Psychic058);u.Stats.Charisma.BaseValue=16;Grant(u,c,"DarkHalfFeature01458");if(level>=5)Grant(u,c,"MorphicFormFeature01458");if(level>=13)Grant(u,c,"PsychicSafeguardFeature01458");
            var resource=A<BlueprintAbilityResource>(c,"DarkHalfResource01458");Check(resource.GetMaxAmount(u.Descriptor)==3+3+level/2,"native Dark Half rounds scale with half Psychic level and Charisma: "+level);
            var ctx=Super058(u,A<BlueprintAbility>(c,"DarkHalfAbility01458"));var buff=u.Buffs.Enumerable.Single(v=>v.Blueprint.AssetGuid==Id("DarkHalfBuff01458"));Check(u.Resources.GetResourceAmount(resource)==resource.GetMaxAmount(u.Descriptor)-1&&buff.IsPermanent,"native Dark Half swift activation pays first round and creates maintained state: "+level);
            Check(u.Stats.CheckDiplomacy.Modifiers.Any(m=>m.ModValue==-20)&&!u.State.HasCondition(UnitCondition.Frightened),"actual native Dark Half skill penalty and no self fear condition: "+level);
            Check(u.Buffs.Enumerable.Count(v=>v.Blueprint.name.Contains("MorphicForm")&&v.IsActive)==(level>=5?1:0),"native activation chooses exactly one real DR form only after milestone: "+level);
            if(level>=13)Check(u.Get<UnitPartSpellResistance>().SRs.Any(v=>v.Value==16+level),"actual native active safeguard SR is16+Psychic level: "+level);
            var runtime=buff.Components.Single(v=>v.SourceBlueprintComponent is DarkHalf01458);using(runtime.RequestEventContext())((DarkHalf01458)runtime.SourceBlueprintComponent).OnNewRound();Check(u.Resources.GetResourceAmount(resource)==resource.GetMaxAmount(u.Descriptor)-2,"native Dark Half round callback spends once: "+level);
            buff.Remove();Check(!u.Buffs.Enumerable.Any(v=>v.Blueprint.name.Contains("MorphicForm")||v.Blueprint.AssetGuid==Id("PsychicSafeguardBuff01458")),"native parent removal cleans only its stored protection/form children: "+level);
            if(level>=13)Check(u.Get<UnitPartSpellResistance>().SRs.Any(v=>v.Value==8+level)&&!u.Get<UnitPartSpellResistance>().SRs.Any(v=>v.Value==16+level),"native base safeguard remains after active stronger child is removed: "+level);
        }
    }
}
