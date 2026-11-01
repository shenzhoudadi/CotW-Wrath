using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.ContextData;
using Kingmaker.UnitLogic.Parts;
using CallOfTheWild.Wrath.Prototype;

internal static class ArchetypeRepair01444Tests
{
    static readonly Type E=typeof(Archetypes01444Tests),Factory=typeof(RakeStance01444).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets");
    static int checks,serial,rayRules,weaponRules,multiRaySalvos,replays,uiCases,resistanceCases,localeReads,damageReductions;
    static bool uiFault;
    static object Call(string m,params object[] args)=>AccessTools.Method(E,m).Invoke(null,args);
    static object Get(object o,string n)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).GetValue(o);
    static void Set(object o,string n,object v)=>(AccessTools.Field(o.GetType(),n)??AccessTools.Field(o.GetType(),"<"+n+">k__BackingField")).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception("ArchetypeRepair01444: "+why);checks++;}
    static BlueprintGuid Id(string token)=>(BlueprintGuid)AccessTools.Method(Factory,"Id").Invoke(null,new object[]{token});
    static T Asset<T>(BlueprintsCache c,string token)where T:SimpleBlueprint=>(T)c.Load(Id(token));
    static T Native<T>(BlueprintsCache c,string id)where T:SimpleBlueprint=>(T)c.Load(BlueprintGuid.Parse(id));
    static T Ref<T>(SimpleBlueprint b)where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(b);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,b,null);return r;}
    static UnitEntityData Unit(BlueprintsCache c)=>(UnitEntityData)Call("Unit",c,"299aa766dee3cbf4790da4efb8c72484",19);
    static Feature Fact(UnitEntityData u,BlueprintFeature bp)=>(Feature)Call("Fact",u,bp);
    static Buff Buff(UnitEntityData u,BlueprintBuff bp)=>(Buff)Call("Buff",u,bp,null);
    static Feature Custom(UnitEntityData u,params BlueprintComponent[] components){for(int i=0;i<components.Length;i++)components[i].name="Repair044Component"+i;var bp=new BlueprintFeature{name="Repair044Feature"+(++serial),AssetGuid=Id("Repair044Feature"+serial),ComponentsArray=components};bp.OnEnable();return Fact(u,bp);}
    internal static void Reset(){checks=serial=rayRules=weaponRules=multiRaySalvos=replays=uiCases=resistanceCases=localeReads=damageReductions=0;uiFault=false;}
    static void BeforePublish(){if(uiFault){uiFault=false;throw new InvalidOperationException("044 injected private UI staging fault");}}
    internal static int Primal(BlueprintsCache parent,Harmony h)
    {
        int start=checks;
        var module=typeof(PrimalSummonResistance01444).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PrimalSorcerer01444");
        var common=module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.ArchetypeBatch01444");
        var tokens=(string[])AccessTools.Field(module,"OwnTokens").GetValue(null);var sourceIds=(string[])AccessTools.Field(module,"Progressions").GetValue(null);
        h.Patch(AccessTools.Method(common,"Publish"),prefix:new HarmonyMethod(typeof(ArchetypeRepair01444Tests),nameof(BeforePublish)));
        try {
            foreach(int fault in new[]{0,1,2,3}) {
                var c=(BlueprintsCache)Call("Fresh",parent);var source=sourceIds.Select(id=>Native<BlueprintProgression>(c,id)).ToArray();
                var groups=new UIGroup[4][];var lists=new object[4][];var proxies=new object[4][];var refs=new BlueprintFeatureBaseReference[4][][];var determinators=new BlueprintFeatureBaseReference[4][];
                var parents=source.Select(p=>p.LevelEntries).ToArray();
                for(int i=0;i<4;i++) {
                    var arc=source[i].LevelEntries.SelectMany(e=>e.Features).Single(f=>f.name.Contains("Arcana"));var blast=source[i].LevelEntries.Where(e=>e.Level==9).SelectMany(e=>e.Features).Single(f=>f.name.Contains("ElementalBlastFeature"));var other=source[i].LevelEntries.SelectMany(e=>e.Features).First(f=>f!=arc&&f!=blast);
                    groups[i]=new[]{new UIGroup(),new UIGroup()};
                    Set(groups[i][0],"m_Features",new List<BlueprintFeatureBaseReference>{Ref<BlueprintFeatureBaseReference>(arc),Ref<BlueprintFeatureBaseReference>(blast),Ref<BlueprintFeatureBaseReference>(other)});
                    Set(groups[i][1],"m_Features",new List<BlueprintFeatureBaseReference>{Ref<BlueprintFeatureBaseReference>(other)});source[i].UIGroups=groups[i];
                    determinators[i]=new[]{Ref<BlueprintFeatureBaseReference>(arc),Ref<BlueprintFeatureBaseReference>(blast),Ref<BlueprintFeatureBaseReference>(other)};Set(source[i],"m_UIDeterminatorsGroup",determinators[i]);
                    lists[i]=groups[i].Select(g=>Get(g,"m_Features")).ToArray();refs[i]=groups[i].Select(g=>((List<BlueprintFeatureBaseReference>)Get(g,"m_Features")).ToArray()).ToArray();
                    foreach(var group in groups[i]){var warm=group.Features.ToArray();}proxies[i]=groups[i].Select(g=>Get(g,"m_FeaturesList")).ToArray();
                }
                var cls=Native<BlueprintCharacterClass>(c,"b3a505fb61437dc4097f43c3f8f9a4cf");var arch=Get(cls,"m_Archetypes");var ui=cls.Progression.UIGroups;
                uiFault=fault==1;SetStatic(E,"cacheFault",fault==2);SetStatic(E,"definitionFault",fault==3);SetStatic(E,"faultId",Id(tokens.Last()));
                bool rejected=(bool)AccessTools.Method(E,"Reject").Invoke(null,new object[]{(Action)(()=>Call("Register",c,1))});
                Check(rejected==(fault!=0),"private UI publication result "+fault);uiCases++;
                for(int i=0;i<4;i++) {
                    Check(ReferenceEquals(source[i].UIGroups,groups[i])&&ReferenceEquals(source[i].LevelEntries,parents[i])&&ReferenceEquals(Get(source[i],"m_UIDeterminatorsGroup"),determinators[i]),"native UI/level/determinator arrays retain exact identities "+i+"/"+fault);
                    for(int g=0;g<2;g++)Check(ReferenceEquals(Get(groups[i][g],"m_Features"),lists[i][g])&&ReferenceEquals(Get(groups[i][g],"m_FeaturesList"),proxies[i][g])&&((List<BlueprintFeatureBaseReference>)Get(groups[i][g],"m_Features")).SequenceEqual(refs[i][g]),"native warmed group list, proxy and reference identities unchanged "+i+"/"+g+"/"+fault);
                }
                if(fault!=0){Check(tokens.All(t=>c.Load(Id(t))==null)&&ReferenceEquals(Get(cls,"m_Archetypes"),arch)&&ReferenceEquals(cls.Progression.UIGroups,ui),"failed private UI staging/cache/connect leaves no native or cache changes "+fault);Call("Register",c,1);}
                for(int i=0;i<4;i++) {
                    string element=new[]{"Air","Earth","Fire","Water"}[i];var copy=Asset<BlueprintProgression>(c,"Primal"+element+"Progression01444");
                    Check(!ReferenceEquals(copy.UIGroups,groups[i])&&copy.UIGroups.Length==2,"private UI group array independently copied");
                    for(int g=0;g<2;g++) {
                        var own=copy.UIGroups[g];var raw=(List<BlueprintFeatureBaseReference>)Get(own,"m_Features");var warm=own.Features.ToArray();
                        Check(!ReferenceEquals(own,groups[i][g])&&!ReferenceEquals(raw,lists[i][g])&&!ReferenceEquals(Get(own,"m_FeaturesList"),proxies[i][g])&&raw.Select((r,n)=>!ReferenceEquals(r,refs[i][g][n])).All(x=>x),"private groups/references/native lazy proxy isolated after warming");
                    }
                    Check(copy.UIGroups[0].Features.Take(2).Select(f=>f.AssetGuid).SequenceEqual(new[]{Id("Primal"+element+"Arcana01444"),Id("Primal"+element+"SummoningFeature01444")}),"private UI arcana/blast mapped to own new facts");
                }
                Call("Register",c,1);Check(true,"warmed copied UI groups accepted by author-only receipt");
            }
        } finally {uiFault=false;SetStatic(E,"cacheFault",false);SetStatic(E,"definitionFault",false);h.Unpatch(AccessTools.Method(common,"Publish"),AccessTools.Method(typeof(ArchetypeRepair01444Tests),nameof(BeforePublish)));}
        Resistance(parent);return checks-start;
    }
    static void SetStatic(Type t,string n,object v)=>AccessTools.Field(t,n).SetValue(null,v);
    internal sealed class StackableEnergy : AddDamageResistanceEnergy { public override bool IsStackable=>true; }
    static int Reduce(UnitEntityData attacker,UnitEntityData target,DamageEnergyType type)
    {
        var energy=new EnergyDamage(new DiceFormula(0,DiceType.Zero),0,type){PreRolledValue=100};var calc=new RuleCalculateDamage(new RuleDealDamage(attacker,target,new DamageBundle(energy)){IsFake=true});
        calc.CalculatedDamage.Add(new DamageValue(energy,100,100,100,1));target.Get<UnitPartDamageReduction>()?.ApplyDamageReduction(calc);damageReductions++;
        return calc.CalculatedDamage[0].Reduction;
    }
    static void Resistance(BlueprintsCache parent)
    {
        var c=(BlueprintsCache)Call("Fresh",parent);Call("Register",c,1);var owner=Unit(c);
        foreach(int i in Enumerable.Range(0,4)) {
            string element=new[]{"Air","Earth","Fire","Water"}[i];var type=new[]{DamageEnergyType.Electricity,DamageEnergyType.Acid,DamageEnergyType.Fire,DamageEnergyType.Cold}[i];var bp=Asset<BlueprintBuff>(c,"Primal"+element+"SummoningBuff01444");
            foreach(int existing in new[]{0,1,5,10,20,30}) {
                var target=Unit(c);Feature original=null;if(existing>0)original=Custom(target,new AddDamageResistanceEnergy{Type=type,Value=existing});
                var foreign=Custom(target,new AddDamageResistanceEnergy{Type=DamageEnergyType.Sonic,Value=17});int before=Reduce(owner,target,type);var fact=Buff(target,bp);var runtime=(AddDamageResistanceBase.ComponentRuntime)fact.Components.Single(r=>r.SourceBlueprintComponent is PrimalSummonResistance01444);
                int expected=existing==0?10:existing+5;Check(before==existing&&runtime.GetCurrentValue()==expected&&Reduce(owner,target,type)==expected,"real native resistance aggregation none=>10/existing+5 "+element+"/"+existing);resistanceCases++;
                Check(Reduce(owner,target,DamageEnergyType.Sonic)==17,"unrelated energy resistance unchanged");fact.Remove();Check(Reduce(owner,target,type)==existing&&target.Descriptor.HasFact(foreign.Blueprint),"removing own summon buff restores existing resistance/source");
            }
            var stacked=Unit(c);Custom(stacked,new AddDamageResistanceEnergy{Type=type,Value=20},new StackableEnergy{Type=type,Value=3});var added=Buff(stacked,bp);int actual=Reduce(owner,stacked,type);Check(actual==28,"native stackable sources counted once, total existing23+5 actual="+actual+" sources="+string.Join(",",stacked.Get<UnitPartDamageReduction>().AllSources.Select(r=>r.Settings.GetType().Name+":"+r.GetCurrentValue()+":"+r.Settings.IsStackable)));added.Remove();Check(Reduce(owner,stacked,type)==23,"native stackable sources preserved after removal");resistanceCases++;
            var ownType=typeof(PrimalSummonResistance01444).GetCustomAttributes(typeof(TypeIdAttribute),false).Cast<TypeIdAttribute>().Single().Guid;
            Check(typeof(BlueprintsCache).Assembly.GetTypes().SelectMany(t=>t.GetCustomAttributes(typeof(TypeIdAttribute),false).Cast<TypeIdAttribute>()).All(a=>a.Guid!=ownType),"new resistance native TypeId collision check");
        }
    }
    internal static int Rake(BlueprintsCache c,UnitEntityData owner,UnitEntityData target,Buff stance,ItemEntityWeapon weapon,int n)
    {
        int start=checks;var component=stance.Blueprint.GetComponent<RakeStance01444>();var runtime=stance.Components.Single(x=>ReferenceEquals(x.SourceBlueprintComponent,component));int stat=owner.Stats.SneakAttack.ModifiedValue;
        Check(component.DiceToForgo==n&&stance.Blueprint.GetComponents<Kingmaker.UnitLogic.Mechanics.Components.AddInitiatorAttackRollTrigger>().Count()==0&&stance.Blueprint.GetComponents<Kingmaker.UnitLogic.Mechanics.Components.AddInitiatorAttackWithWeaponTrigger>().Count()==0,"only damage-event handler; no global modifier/roll and weapon cleanup pair");
        var demoralize=component.DemoralizeAction.Actions.OfType<Demoralize>().Single();Check(demoralize.Bonus==(n-1)*5,"native per-check bonus preserved");
        foreach(bool ray in new[]{false,true})foreach(int mode in new[]{0,1,2,3,4}) {
            var roll=new RuleAttackRoll(owner,target,weapon,0);Set(roll,"Result",mode==0?AttackResult.Miss:AttackResult.Hit);Set(roll,"IsSneakAttack",mode!=1);Set(roll,"IsFake",mode==2);Set(roll,"FortificationNegatesSneakAttack",mode==3);Set(roll,"AttackType",ray?AttackType.RangedTouch:AttackType.Melee);
            if(!ray){var attack=new RuleAttackWithWeapon(owner,target,weapon,0);Set(attack,"AttackRoll",roll);roll.RuleAttackWithWeapon=attack;}
            var main=new EnergyDamage(new DiceFormula(4,DiceType.D6),DamageEnergyType.Fire);var sneak=new EnergyDamage(new DiceFormula(stat,DiceType.D6),DamageEnergyType.Fire){Sneak=true,Precision=true};var otherPrecision=new EnergyDamage(new DiceFormula(2,DiceType.D6),DamageEnergyType.Fire){Precision=true};
            var deal=new RuleDealDamage(owner,target,new DamageBundle(main,sneak,otherPrecision)){AttackRoll=roll};var prepare=new RulePrepareDamage(deal);int count=(int)AccessTools.Field(E,"demoralizes").GetValue(null);
            using(runtime.RequestEventContext())component.OnEventDidTrigger(prepare);
            Check(sneak.Dice.ModifiedValue.Rolls==stat-(mode==4?n:0)&&main.Dice.ModifiedValue.Rolls==4&&otherPrecision.Dice.ModifiedValue.Rolls==2,"only matching attack sneak dice modified "+ray+"/"+mode+"/"+n);
            Check(owner.Stats.SneakAttack.ModifiedValue==stat&&(int)AccessTools.Field(E,"demoralizes").GetValue(null)==count+(mode==4?1:0),"global stat unchanged and demoralize once only for actual eligible damage");
            using(runtime.RequestEventContext())component.OnEventDidTrigger(prepare);Check(sneak.Dice.ModifiedValue.Rolls==stat-(mode==4?n:0),"duplicate prepare delivery never subtracts again");replays++;
            if(ray)rayRules++;else weaponRules++;
        }
        // Native damage-action ray linkage, then native preparation of three
        // independent projectile rolls. There is deliberately no weapon resolve.
        var action=new ContextActionDealDamage{DamageType=new DamageTypeDescription{Type=DamageType.Energy,Energy=DamageEnergyType.Fire},Value=new ContextDiceValue{DiceType=DiceType.D6,DiceCountValue=4,BonusValue=0}};
        var ability=new BlueprintAbility{Type=AbilityType.Spell,ComponentsArray=Array.Empty<BlueprintComponent>()};ability.OnEnable();var context=new MechanicsContext(owner,target.Descriptor,ability);
        for(int ray=0;ray<3;ray++) {
            var roll=new RuleAttackRoll(owner,target,weapon,0);Set(roll,"Result",AttackResult.Hit);Set(roll,"IsSneakAttack",true);Set(roll,"AttackType",AttackType.RangedTouch);
            RuleDealDamage deal;using(context.GetDataScope(target))using(ContextAttackData.Request().Setup(roll,null,0,ray))deal=action.GetDamageRule(owner,target);
            Check(ReferenceEquals(deal.AttackRoll,roll)&&roll.RuleAttackWithWeapon==null,"actual native damage action retains independent ray attack roll");
            deal.Reason=context;var prepare=new RulePrepareDamage(deal);prepare.OnTrigger(null);var sneak=prepare.DamageBundle.Single(d=>d.Sneak);Check(sneak.Dice.ModifiedValue.Rolls==stat,"native preparation uses full unchanged character sneak dice for every ray");
            RulebookEventBus.OnEventDidTrigger(prepare);Check(sneak.Dice.ModifiedValue.Rolls==stat-n&&owner.Stats.SneakAttack.ModifiedValue==stat,"native event subscription: each ray independently sacrifices exactly selected dice");rayRules++;
        }
        multiRaySalvos++;
        var nextRoll=new RuleAttackRoll(owner,target,weapon,0);Set(nextRoll,"Result",AttackResult.Hit);Set(nextRoll,"IsSneakAttack",true);var nextAttack=new RuleAttackWithWeapon(owner,target,weapon,0);Set(nextAttack,"AttackRoll",nextRoll);nextRoll.RuleAttackWithWeapon=nextAttack;
        var nextDeal=new RuleDealDamage(owner,target,new DamageBundle(new EnergyDamage(new DiceFormula(1,DiceType.D6),DamageEnergyType.Fire))){AttackRoll=nextRoll,Reason=context};var nextPrepare=new RulePrepareDamage(nextDeal);nextPrepare.OnTrigger(null);var nextSneak=nextPrepare.DamageBundle.Single(d=>d.Sneak);
        Check(nextSneak.Dice.ModifiedValue.Rolls==stat,"weapon attack immediately after multiray starts with full native sneak dice");RulebookEventBus.OnEventDidTrigger(nextPrepare);Check(nextSneak.Dice.ModifiedValue.Rolls==stat-n&&owner.Stats.SneakAttack.ModifiedValue==stat,"native event subscription: subsequent weapon attack has only its own subtraction");weaponRules++;
        var insufficient=new EnergyDamage(new DiceFormula(n-1,DiceType.D6),DamageEnergyType.Fire){Sneak=true,Precision=true};var lowRoll=new RuleAttackRoll(owner,target,weapon,0);Set(lowRoll,"Result",AttackResult.Hit);Set(lowRoll,"IsSneakAttack",true);var low=new RulePrepareDamage(new RuleDealDamage(owner,target,new DamageBundle(insufficient)){AttackRoll=lowRoll});int before=(int)AccessTools.Field(E,"demoralizes").GetValue(null);
        using(runtime.RequestEventContext())component.OnEventDidTrigger(low);Check(insufficient.Dice.ModifiedValue.Rolls==n-1&&(int)AccessTools.Field(E,"demoralizes").GetValue(null)==before,"insufficient actual sneak dice give no free higher-tier bonus");
        Check(owner.Stats.SneakAttack.ModifiedValue==stat&&!owner.Buffs.Enumerable.Any(b=>b.Blueprint.name.Contains("Reduction01444")),"weapon/ray paths finish with no temporary reduction state");return checks-start;
    }
    internal static int Localization()
    {
        int start=checks;JObject data;using(var stream=typeof(ArchetypeRepair01444Tests).Assembly.GetManifestResourceStream("CotW.Tests.Localization01444.json"))using(var reader=new StreamReader(stream))data=JObject.Parse(reader.ReadToEnd());
        var keys=data["keys"].Values<string>().ToArray();var cn=(JObject)data["strings"];Check(keys.Length==106&&keys.Distinct().Count()==106&&cn.Properties().Count()==106,"exact106 original English key manifest");
        var defaults=(IDictionary<string,string>)AccessTools.Field(Factory,"Strings").GetValue(null);var original=LocalizationManager.CurrentPack;
        try {
            // Original Helpers.savingThrowNone must also win during the first
            // construction into a pack which has no mod text keys yet.
            var savedCache=ResourcesLibrary.BlueprintsCache;
            try {
                var firstPack=new LocalizationPack{Locale=Locale.enGB};LocalizationManager.CurrentPack=firstPack;
                var first=(BlueprintsCache)Call("Fresh",savedCache);Call("Register",first,0);
                foreach(string token in new[]{"TerrorMoveWisdomAbility01444","TerrorMoveCharismaAbility01444","TerrorSwiftWisdomAbility01444","TerrorSwiftCharismaAbility01444"}) {
                    var ability=Asset<BlueprintAbility>(first,token);
                    Check(firstPack.GetText(ability.LocalizedSavingThrow.Key,false)=="None","first registration into a missing-key pack retains original CotW None label "+token);
                }
            } finally {AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,savedCache);}
            foreach(var locale in new[]{Locale.zhCN,Locale.enGB}) {
                var pack=new LocalizationPack{Locale=locale};LocalizationManager.CurrentPack=pack;
                foreach(string key in keys)Check(defaults[key]==(string)cn[key],"runtime default exactly matches original CotW source "+key);
                foreach(string key in keys)pack.PutString(key,locale==Locale.zhCN?(string)cn[key]:defaults[key]);
                foreach(string key in keys) {
                    string expected=locale==Locale.zhCN?(string)cn[key]:defaults[key];Check(!System.Text.RegularExpressions.Regex.IsMatch(expected,"[\\p{IsCJKUnifiedIdeographs}]"),"every restored key contains English text");
                    var name=key.Substring("cotw-wrath.psychic.full.".Length);int split=name.LastIndexOf('.');AccessTools.Method(Factory,"Text").Invoke(null,new object[]{name.Substring(0,split),name.Substring(split+1),defaults[key]});
                    Check(pack.GetText(key,false)==expected,"runtime Text preserves current language value "+key);localeReads++;
                }
                AccessTools.Method(Factory,"ApplyLocalization").Invoke(null,null);Check(keys.All(key=>pack.GetText(key,false)==(locale==Locale.zhCN?(string)cn[key]:defaults[key])),"ApplyLocalization preserves current language pack");
                pack.PutString(keys[0],"");AccessTools.Method(Factory,"ApplyLocalization").Invoke(null,null);Check(pack.GetText(keys[0],true)=="","intentionally blank existing translations are never replaced by English");
            }
            var missing=new LocalizationPack{Locale=Locale.zhCN};LocalizationManager.CurrentPack=missing;AccessTools.Method(Factory,"ApplyLocalization").Invoke(null,null);Check(keys.All(k=>missing.GetText(k,false)==defaults[k]),"only absent keys receive exact English fallback");
            var loaded=new LocalizationPack{Locale=Locale.zhCN};foreach(string k in keys)loaded.PutString(k,(string)cn[k]);missing.AddStrings(loaded);AccessTools.Method(Factory,"ApplyLocalization").Invoke(null,null);Check(keys.All(k=>missing.GetText(k,false)==(string)cn[k]),"late native language-pack overlay replaces fallback and remains unchanged");
        } finally {LocalizationManager.CurrentPack=original;}
        return checks-start;
    }
    internal static void Report()
    {
        Check(rayRules==40&&weaponRules==30&&multiRaySalvos==5&&replays==50&&uiCases==4&&resistanceCases==28&&localeReads==212,"all four requested regression families executed");
        PsychicExpansionTests.Inventory["repair01444Contracts"]=new JObject{["passed"]=true,["additionalMigrationItemCount"]=0,["rayDamageRules"]=rayRules,["weaponDamageRules"]=weaponRules,["multiRaySalvos"]=multiRaySalvos,["repeatDeliveriesChecked"]=replays,["nativeUICloneAndRollbackCases"]=uiCases,["resistanceCases"]=resistanceCases,["nativeDRApplications"]=damageReductions,["englishRestoredKeys"]=106,["nativeLocaleReads"]=localeReads,["noGlobalSneakMutation"]=true,["allNativeGroupIdentitiesPreserved"]=true,["activeLocaleRespected"]=true,["existingResistancePlusFive"]=true,["boundary"]="Actual native ray damage-action linkage and RulePrepareDamage.OnTrigger execute; prepared weapon/ray damage fields and same-roll repeats run through the attached component with native Demoralize arithmetic and bounded final skill result. Native damage reduction selects resistance on controlled DamageValue inputs. Scene command/HP/visual/save scheduling and full external mod initialization are not inferred."};
    }
}
