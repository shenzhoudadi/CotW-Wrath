using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;

internal static class Launcher
{
    public static int Main(string[] args)
    {
        if (args.Length != 5) return 2;
        AppDomain.CurrentDomain.AssemblyResolve += (s,e) => {
            var name=new AssemblyName(e.Name).Name;
            if (name=="0Harmony") return Assembly.LoadFrom(args[2]);
            foreach(var dir in new[] { args[0],Path.GetDirectoryName(args[1]),Path.GetDirectoryName(args[3]) }) {
                var path=Path.Combine(dir,name+".dll");
                if(File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        try {Assembly.LoadFrom(args[2]);typeof(Launcher).Assembly.GetType("Checks").GetMethod("Run").Invoke(null,new object[]{args});return 0; }
        catch(Exception e) { while(e!=null) {Console.Error.WriteLine(e.Message+"\n"+e.StackTrace);e=e.InnerException;}return 1; }
    }
}

internal static class Checks
{
    private static Type medium,cotw;
    private static int assertions;
    private static readonly Metamagic Logical=(Metamagic)(1<<29);
    private static T Empty<T>() where T:class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Set(object target,string field,object value) => AccessTools.Field(target.GetType(),field).SetValue(target,value);
    private static object Get(object target,string field) => AccessTools.Field(target.GetType(),field).GetValue(target);
    private static object Call(Type type,string name,params object[] args) => AccessTools.Method(type,name).Invoke(null,args);
    private static void Check(bool value,string label) { if(!value)throw new Exception(label); assertions++; }
    private static bool SkipProfiling() => false;
    private static void CheckNativeOrphanCleanup(Assembly assembly, UnitEntityData unit, UnitDescriptor descriptor, EntityFactsManager manager)
    {
        // Native fact/cache removal on synthetic entities. Reproduce native slot
        // retention, then install only the production spirit detach prefix.
        Set(unit,"Facts",manager);
        var progression=Empty<UnitProgressionData>();
        var features=new FeatureCollection();features.Initialize(manager);
        Set(progression,"<Features>k__BackingField",features);
        Set(progression,"<Owner>k__BackingField",descriptor);Set(descriptor,"Progression",progression);
        Set(descriptor,"m_IsPlayerFactionCached",(bool?)false);
        var abilityCollection=new AbilityCollection();
        AccessTools.Method(typeof(EntityFactsManager),"AddDelegate")
            .MakeGenericMethod(typeof(AbilityCollection)).Invoke(manager,new object[]{abilityCollection});
        var guids=assembly.GetType("MediumClass.Utilities.Guids",true);
        BlueprintGuid Id(string name)=>BlueprintGuid.Parse((string)AccessTools.Field(guids,name).GetRawConstantValue());
        var own1=new BlueprintAbility {AssetGuid=Id("ChampionFleetChargeAbility")};
        var own2=new BlueprintAbility {AssetGuid=Id("MarshalMarshalsOrdersAbility")};
        var native=new BlueprintAbility {AssetGuid=BlueprintGuid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaabb")};
        var center=new BlueprintAbility {AssetGuid=Id("MediumPsychicCenterSelf")};
        var parent=new BlueprintFeature {AssetGuid=BlueprintGuid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaabc")};
        var add=new AddFacts();
        Set(add,"m_Facts",new[]{own1,own2,native}.Select(bp=>{
            var reference=BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(bp);
            Set(reference,"<Cached>k__BackingField",bp);return reference;
        }).ToArray());
        parent.ComponentsArray=new BlueprintComponent[]{add};
        T Raw<T>(Kingmaker.Blueprints.Facts.BlueprintFact blueprint) where T:EntityFact {
            var fact=Empty<T>();Set(fact,"<Blueprint>k__BackingField",blueprint);
            Set(fact,"<Manager>k__BackingField",manager);Set(fact,"Components",new List<EntityFactComponent>());
            manager.List.Add(fact);var cache=Get(manager,"m_Cache");
            AccessTools.Method(cache.GetType(),"Add").Invoke(cache,new object[]{fact});return fact;
        }
        var a=Raw<Ability>(own1);var b=Raw<Ability>(own2);var n=Raw<Ability>(native);var c=Raw<Ability>(center);
        foreach(var fact in new[]{a,b,n,c}) {
            var data=Empty<AbilityData>();Set(data,"Blueprint",fact.Blueprint);Set(data,"m_Fact",fact);
            Set(data,"<HasActionBarSlot>k__BackingField",new Kingmaker.Utility.CountableFlag(1));Set(fact,"Data",data);
        }
        // Native collection retention branch reproduced before installing our narrow patch.
        unit.RemoveFact(a);
        Check(manager.List.Contains(a)&&a.Data.TemporarilyDisabled,"Native slotted ability remains as a disabled fact");
        var removal=new Harmony("MediumPsychic.SpiritDetachFixture");
        removal.CreateClassProcessor(assembly.GetType("MediumClass.Medium.MediumSpiritAbilityRemovalPatch",true)).Patch();
        var counter=Raw<Feature>(new BlueprintFeature {AssetGuid=Id("SpiritPower")});Set(counter,"<Rank>k__BackingField",4);
        var cleanup=assembly.GetType("MediumClass.Medium.MediumSpiritPowerCleanup",true);
        Check(manager.GetAll<Ability>().Count==4,"native fixture initially exposes four abilities");
        Call(cleanup,"RevokeAll",unit,new[]{parent});
        Check(a.IsDisposed && b.IsDisposed,"native disposal removes both owned spirit ability facts");
        Check(!manager.GetAll<Ability>().Contains(a) && !manager.GetAll<Ability>().Contains(b),"native ability cache loses spirit buttons");
        Check(manager.GetAll<Ability>().Contains(n) && !n.IsDisposed,"native shared ability preserved despite power reference");
        Check(manager.GetAll<Ability>().Contains(c) && !c.IsDisposed,"permanent Center Self remains outside spirit roots");
        Check(features.GetRank(counter.Blueprint)==4,"permanent SpiritPower rank unchanged");
        Call(cleanup,"RevokeAll",unit,new[]{parent});
        Check(manager.GetAll<Ability>().Count==2,"native orphan cleanup is idempotent");
        Check(!(bool)Call(cleanup,"IsRemoving",a),"Narrow removal scope cleared after cleanup");
        unit.RemoveFact(n);
        Check(manager.List.Contains(n)&&n.Data.TemporarilyDisabled,"Ordinary slotted ability still uses native disabled retention");
        foreach(var stem in new[]{"Marshal","Trickster"}) {
            var bp=new BlueprintAbility {AssetGuid=Id(stem+"SeanceBoonAbility")};
            var boon=new BlueprintFeature {AssetGuid=Id(stem+"SeanceBoon")};
            var refAbility=BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(bp);Set(refAbility,"<Cached>k__BackingField",bp);
            var grants=new AddFacts();Set(grants,"m_Facts",new[]{refAbility});boon.ComponentsArray=new BlueprintComponent[]{grants};
            var button=Raw<Ability>(bp);var data=Empty<AbilityData>();Set(data,"Blueprint",bp);Set(data,"m_Fact",button);
            Set(data,"<HasActionBarSlot>k__BackingField",new Kingmaker.Utility.CountableFlag(1));Set(button,"Data",data);
            var liveBoon=Raw<Feature>(boon);Set(liveBoon,"<Rank>k__BackingField",1);
            Call(cleanup,"RevokeOrphanAbilities",unit,new[]{boon});
            Check(manager.List.Contains(button)&&!button.IsDisposed,stem+" live/shared boon preserves button");
            unit.RemoveFact(liveBoon);unit.RemoveFact(button);
            Check(manager.List.Contains(button)&&button.Data.TemporarilyDisabled,stem+" native boon removal leaves disabled slotted button");
            Call(cleanup,"RevokeOrphanAbilities",unit,new[]{boon});
            Check(button.IsDisposed&&!manager.GetAll<Ability>().Contains(button),stem+" orphan boon button removed from native cache");
        }
        CheckNativeToggleCleanup(assembly,unit,descriptor,manager,removal);
        removal.UnpatchAll(removal.Id);
    }

    private static void CheckNativeToggleCleanup(Assembly assembly,UnitEntityData unit,UnitDescriptor descriptor,EntityFactsManager manager,Harmony removal)
    {
        // Native manager, cache, PrepareFactForDetach and UI dirty notification.
        // Synthetic inactive facts reproduce the orphan left after native AddFacts
        // removal; no active buff installation or rendered UI is simulated here.
        var collection=new ActivatableAbilityCollection();
        AccessTools.Method(typeof(EntityFactsManager),"AddDelegate").MakeGenericMethod(typeof(ActivatableAbilityCollection))
            .Invoke(manager,new object[]{collection});
        var settings=Empty<Kingmaker.UI.UnitSettings.UnitUISettings>();Set(descriptor,"UISettings",settings);
        // Native detach broadcasts a party event before marking the UI dirty.
        // Supply only this missing global game/party service, not a detach substitute.
        var gameField=AccessTools.Field(typeof(Kingmaker.Game),"s_Instance");var previousGame=gameField.GetValue(null);
        var game=Empty<Kingmaker.Game>();var state=Empty<PersistentState>();var player=Empty<Kingmaker.Player>();
        Set(player,"m_Party",new List<UnitEntityData>());Set(player,"m_CharacterListsValid",true);
        state.PlayerState=player;Set(game,"State",state);gameField.SetValue(null,game);
        var guids=assembly.GetType("MediumClass.Utilities.Guids",true);
        BlueprintGuid Id(string name)=>BlueprintGuid.Parse((string)AccessTools.Field(guids,name).GetRawConstantValue());
        var cleanup=assembly.GetType("MediumClass.Medium.MediumSpiritPowerCleanup",true);
        var energies=new[]{"Negative","Fire","Cold","Electricity","Acid"};
        var modes=energies.Select(e=>new BlueprintActivatableAbility{AssetGuid=Id("MediumAdrielDeathCurseToggle"+e),DeactivateImmediately=true}).ToArray();
        var refs=modes.Select(bp=>{var reference=BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(bp);Set(reference,"<Cached>k__BackingField",bp);return reference;}).ToArray();
        var parent=new BlueprintFeature{AssetGuid=Id("MediumAdrielDeathCurse")};
        var grants=new AddFacts();Set(grants,"m_Facts",refs);parent.ComponentsArray=new BlueprintComponent[]{grants};
        T Raw<T>(Kingmaker.Blueprints.Facts.BlueprintFact bp) where T:EntityFact {
            var fact=Empty<T>();Set(fact,"<Blueprint>k__BackingField",bp);Set(fact,"<Manager>k__BackingField",manager);
            Set(fact,"Components",new List<EntityFactComponent>());manager.List.Add(fact);
            var cache=Get(manager,"m_Cache");AccessTools.Method(cache.GetType(),"Add").Invoke(cache,new object[]{fact});return fact;
        }
        ActivatableAbility Toggle(BlueprintActivatableAbility bp,int slots) {
            var fact=Raw<ActivatableAbility>(bp);Set(fact,"<HasActionBarSlot>k__BackingField",new Kingmaker.Utility.CountableFlag(slots));return fact;
        }
        // 0.4.0 has the normal-ability prefix only: the actual production sweep
        // still leaves every slotted Death Curse toggle in the native cache.
        foreach(var mode in modes) {
            var fact=Toggle(mode,1);unit.RemoveFact(fact);
            Check(fact.TemporarilyDisabled&&!fact.IsDisposed&&manager.GetAll<ActivatableAbility>().Contains(fact),"Reproduced native disabled toggle retention: "+mode.AssetGuid);
            Call(cleanup,"RevokeAll",unit,new[]{parent});
            Check(!fact.IsDisposed&&manager.GetAll<ActivatableAbility>().Contains(fact),"Old normal-ability patch cannot clear toggle orphan");
        }
        removal.CreateClassProcessor(assembly.GetType("MediumClass.Medium.MediumSpiritToggleRemovalPatch",true)).Patch();
        Check(removal.GetPatchedMethods().Count()==2,"Two distinct native collection detach targets installed");
        Call(cleanup,"RevokeAll",unit,new[]{parent});
        Check(manager.GetAll<ActivatableAbility>().Count==0,"Existing five disabled mode orphans removed from native cache");
        Check(settings.Dirty,"Native toggle detach marks action bar dirty");
        foreach(int slots in new[]{0,1,2}) foreach(var mode in modes) {
            var fact=Toggle(mode,slots);var live=Raw<Feature>(parent);Set(live,"<Rank>k__BackingField",1);
            Call(cleanup,"RevokeOrphanAbilities",unit,new[]{parent});
            Check(!fact.IsDisposed&&manager.GetAll<ActivatableAbility>().Contains(fact),"Owned/shared power preserves toggle "+slots);
            unit.RemoveFact(live);unit.RemoveFact(fact);
            Check(slots==0 ? fact.IsDisposed : fact.TemporarilyDisabled&&!fact.IsDisposed,"Native detach slot predicate "+slots);
            Call(cleanup,"RevokeAll",unit,new[]{parent});
            Check(fact.IsDisposed&&!manager.GetAll<ActivatableAbility>().Contains(fact),"Channel-end catalogue cleanup removes toggle "+slots);
            Check(!(bool)Call(cleanup,"IsRemoving",fact),"Removal scope restored");
            // Post-load repair uses this same ownership-scoped entry for stale modes.
            var restored=Toggle(mode,1);Set(restored,"<TemporarilyDisabled>k__BackingField",true);
            Call(cleanup,"RemoveOwnedFact",unit,restored);
            Check(restored.IsDisposed&&!manager.GetAll<ActivatableAbility>().Contains(restored),"Old-save scoped removal repairs disabled toggle");
        }
        var ordinary=Toggle(new BlueprintActivatableAbility{AssetGuid=BlueprintGuid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaabd")},1);
        var outside=Toggle(modes[1],1);
        Call(cleanup,"RevokeAll",unit,Array.Empty<BlueprintFeature>());
        Check(!ordinary.IsDisposed&&!outside.IsDisposed,"No global sweep of native or unrelated owned modes");
        unit.RemoveFact(ordinary);unit.RemoveFact(outside);
        Check(ordinary.TemporarilyDisabled&&!ordinary.IsDisposed&&outside.TemporarilyDisabled&&!outside.IsDisposed,"Ordinary removals retain native behavior even for a mod blueprint");
        Call(cleanup,"RevokeAll",unit,new[]{parent});Call(cleanup,"RevokeAll",unit,new[]{parent});
        Check(outside.IsDisposed&&manager.GetAll<ActivatableAbility>().Count==1&&manager.GetAll<ActivatableAbility>().Contains(ordinary),"Repeated cleanup removes only catalogue-owned toggle");
        gameField.SetValue(null,previousGame);
    }
    public static void Run(string[] args)
    {
        RequiredHarmonyRuntime.Verify(args[2]);
        Check(Path.GetFullPath(typeof(Harmony).Assembly.Location)==Path.GetFullPath(args[2]),"Selected Harmony runtime loaded");
        // Unity's profiling initializer cannot run in a standalone CLR process.
        // Only that timer is bypassed; casting logic is unchanged.
        var profiling=new Harmony("MediumPsychic.ProfilingFixture");
        profiling.Patch(AccessTools.Method(typeof(Kingmaker.Utility.ProfileScope),"New",new[]{typeof(string),typeof(UnityEngine.Object)}),
            prefix:new HarmonyMethod(AccessTools.Method(typeof(Checks),nameof(SkipProfiling))));
        // Seeded inactive ability facts and caster-free duration contexts emit
        // diagnostics whose logger initialization needs Unity's native callback.
        profiling.Patch(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel),"Error",new[]{typeof(string),typeof(object[])}),
            prefix:new HarmonyMethod(AccessTools.Method(typeof(Checks),nameof(SkipProfiling))));
        var assembly=Assembly.LoadFrom(args[1]);
        Check(assembly.GetName().Version==new Version(0,4,3,0),"Merged mod version 0.4.3.0");
        Check(assembly.GetReferencedAssemblies().Single(a=>a.Name=="0Harmony").Version==new Version(2,3,6,0),"Pinned Harmony compile reference preserved");
        // Actual merged production helper, with the game's real StatType catalogue.
        var marshalRules=assembly.GetType("MediumClass.Medium.MarshalSpiritRules",true);
        var passive=(Kingmaker.EntitySystem.Stats.StatType[])Call(marshalRules,"get_PassiveBonusStats");
        var surge=(Kingmaker.EntitySystem.Stats.StatType[])Call(marshalRules,"get_SurgeStats");
        var stat=typeof(Kingmaker.EntitySystem.Stats.StatType);
        Check(passive.SequenceEqual(new[]{Kingmaker.EntitySystem.Stats.StatType.SkillPersuasion,Kingmaker.EntitySystem.Stats.StatType.SkillUseMagicDevice}),"Marshal passive bonus only native Charisma skills");
        Check(!passive.Contains(Kingmaker.EntitySystem.Stats.StatType.AdditionalAttackBonus),"Marshal passive attack bonus absent");
        foreach(var value in Kingmaker.EntitySystem.Stats.StatTypeHelper.Saves)
            Check(!passive.Contains(value) && surge.Contains(value),"Marshal passive/active save coverage separated: "+value);
        Check(surge.Contains(Kingmaker.EntitySystem.Stats.StatType.AdditionalAttackBonus),"Marshal Order still covers attacks");
        Check(Kingmaker.EntitySystem.Stats.StatTypeHelper.Skills.All(surge.Contains),"Marshal Order still covers every native skill");
        Check(!passive.Contains(Kingmaker.EntitySystem.Stats.StatType.Charisma),"No Charisma score/DC/slot increase");
        medium=assembly.GetType("MediumClass.Medium.MediumPsychicCasting",true);
        cotw=Assembly.LoadFrom(args[3]).GetType("CallOfTheWild.Wrath.Prototype.PsychicCasting",true);
        var guids=assembly.GetType("MediumClass.Utilities.Guids",true);
        string Id(string name)=>(string)AccessTools.Field(guids,name).GetRawConstantValue();
        var centerDuration=(ContextDurationValue)Call(assembly.GetType("MediumClass.Medium.MediumPsychicBlueprints",true),"CenterSelfDuration");
        var duration=Empty<MechanicsContext>();duration.SetParams(new AbilityParams());
        Check(centerDuration.DiceCountValue!=null && centerDuration.Calculate(duration).Value==1,
            "Native Medium Center Self duration calculation succeeds for one round");
        var main=new BlueprintSpellbook { AssetGuid=BlueprintGuid.Parse(Id("MediumSpellbook")) };
        var ordinary=new BlueprintSpellbook { AssetGuid=BlueprintGuid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),IsArcane=true };
        var archmage=new BlueprintSpellbook {AssetGuid=BlueprintGuid.Parse(Id("ArchmageSpellbook")),IsArcane=true};
        var hierophant=new BlueprintSpellbook {AssetGuid=BlueprintGuid.Parse(Id("HierophantSpellbook"))};
        bool IsBook(BlueprintSpellbook b)=>(bool)AccessTools.Method(medium,"IsPsychic",new[]{typeof(BlueprintSpellbook)}).Invoke(null,new object[]{b});
        Check(IsBook(main),"Only stable main Medium book classified psychic");
        foreach(var b in new[]{ordinary,archmage,hierophant}) Check(!IsBook(b),"Other and preparation books remain outside casting conversion");
        var unit=Empty<UnitEntityData>();var descriptor=Empty<UnitDescriptor>();
        Set(unit,"<Descriptor>k__BackingField",descriptor);Set(descriptor,"<Unit>k__BackingField",unit);
        Set(unit,"Parts",new EntityPartsManager(unit));
        var manager=new EntityFactsManager(unit);var buffs=new BuffCollection();buffs.Initialize(manager);Set(unit,"<Buffs>k__BackingField",buffs);
        CheckNativeOrphanCleanup(assembly,unit,descriptor,manager);
        var book=Empty<Spellbook>();Set(book,"Blueprint",main);Set(book,"<Owner>k__BackingField",descriptor);
        var spell=Empty<AbilityData>();Set(spell,"Blueprint",new BlueprintAbility {Type=AbilityType.Spell});Set(spell,"m_CachedSpellbook",book);
        var proxy=FormatterServices.GetUninitializedObject(typeof(EntityService).GetNestedType("Proxy",BindingFlags.Public|BindingFlags.NonPublic));
        Set(proxy,"<Entity>k__BackingField",unit);object casterRef=default(EntityRef<UnitEntityData>);Set(casterRef,"m_Proxy",proxy);Set(spell,"m_CasterRef",casterRef);
        Buff Buff(BlueprintBuff bp,SpellDescriptor flags=SpellDescriptor.None) {
            var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);Set(b,"<IsTurnedOn>k__BackingField",true);
            var context=Empty<MechanicsContext>();Set(context,"<SpellDescriptor>k__BackingField",flags);Set(b,"m_Context",context);return b;
        }
        RuleCalculateAbilityParams Params() { var r=Empty<RuleCalculateAbilityParams>();Set(r,"Spellbook",book);Set(r,"AbilityData",spell);Set(r,"Initiator",unit);return r; }
        Check(!(bool)Call(medium,"IsEmotionBlocked",spell),"Healthy Medium spells available");
        foreach(var flag in new[]{SpellDescriptor.Fear,SpellDescriptor.Shaken,SpellDescriptor.Frightened,SpellDescriptor.NegativeEmotion}) {
            var b=Buff(new BlueprintBuff(),flag);buffs.Enumerable.Add(b);
            Check((bool)Call(medium,"IsEmotionBlocked",spell),"Harmful descriptor blocks "+flag);
            var emotion=medium.GetNestedType("EmotionPatch",BindingFlags.NonPublic);
            object[] available={spell,true};Call(emotion,"Postfix",available);Check(!(bool)available[1],"Availability changes true to false");
            available=new object[]{spell,false};Call(emotion,"Postfix",available);Check(!(bool)available[1],"Availability never overrides previous restriction");
            Set(b,"<IsSuppressed>k__BackingField",true);Check(!(bool)Call(medium,"IsEmotionBlocked",spell),"Suppressed harmful effect ignored");
            Set(b,"<IsSuppressed>k__BackingField",false);Set(b,"<IsTurnedOn>k__BackingField",false);Check(!(bool)Call(medium,"IsEmotionBlocked",spell),"Inactive harmful effect ignored");
            Set(b,"<IsTurnedOn>k__BackingField",true);spell.MetamagicData=new MetamagicData();spell.MetamagicData.Add(Logical);
            Check(!(bool)Call(medium,"IsEmotionBlocked",spell),"Logical removes emotion only");spell.MetamagicData=null;
            Set(book,"Blueprint",ordinary);Check(!(bool)Call(medium,"IsEmotionBlocked",spell),"Multiclass ordinary book unaffected");Set(book,"Blueprint",main);buffs.Enumerable.Clear();
        }
        buffs.Enumerable.Add(Buff(new BlueprintBuff(),SpellDescriptor.Emotion));Check(!(bool)Call(medium,"IsEmotionBlocked",spell),"Positive generic emotion allowed");buffs.Enumerable.Clear();
        var harmful=new BlueprintBuff {ComponentsArray=new BlueprintComponent[]{new SpellDescriptorComponent{Descriptor=SpellDescriptor.Shaken}}};harmful.OnEnable();
        buffs.Enumerable.Add(Buff(harmful));Check((bool)Call(medium,"IsEmotionBlocked",spell),"Blueprint descriptor without context descriptor blocks");buffs.Enumerable.Clear();
        var evt=Params();evt.AddBonusConcentration(7);Call(medium,"ApplyThoughtPenalty",evt);Check((int)Get(evt,"m_BonusConcentration")==-3,"Thought penalty preserves other concentration bonuses");
        var center=Buff(new BlueprintBuff{AssetGuid=BlueprintGuid.Parse(Id("MediumPsychicCenterSelfBuff"))});buffs.Enumerable.Add(center);
        evt=Params();evt.AddBonusConcentration(7);Call(medium,"ApplyThoughtPenalty",evt);Check((int)Get(evt,"m_BonusConcentration")==7,"Center Self restores normal concentration");
        Set(center,"<IsSuppressed>k__BackingField",true);evt=Params();Call(medium,"ApplyThoughtPenalty",evt);Check((int)Get(evt,"m_BonusConcentration")==-10,"Suppressed Center does not help");
        Set(center,"<IsSuppressed>k__BackingField",false);Set(center,"<IsTurnedOn>k__BackingField",false);evt=Params();Call(medium,"ApplyThoughtPenalty",evt);Check((int)Get(evt,"m_BonusConcentration")==-10,"Inactive Center does not help");buffs.Enumerable.Clear();
        spell.MetamagicData=new MetamagicData();spell.MetamagicData.Add(Logical);evt=Params();Call(medium,"ApplyThoughtPenalty",evt);Check((int)Get(evt,"m_BonusConcentration")==-10,"Logical retains thought component");spell.MetamagicData=null;
        foreach(var b in new[]{ordinary,archmage,hierophant}) {Set(book,"Blueprint",b);evt=Params();Call(medium,"ApplyThoughtPenalty",evt);Check((int)Get(evt,"m_BonusConcentration")==0,"Unrelated/preparation book thought modifier unchanged");}Set(book,"Blueprint",main);
        var source=medium.GetNestedType("SourcePatch",BindingFlags.NonPublic);object[] sourceArgs={spell,SpellSource.Arcane};Call(source,"Postfix",sourceArgs);Check((SpellSource)sourceArgs[1]==SpellSource.None,"Main book has no arcane armor failure source");
        Set(spell,"Blueprint",new BlueprintAbility{Type=AbilityType.SpellLike});Check(!(bool)Call(medium,"IsEmotionBlocked",spell),"Spell-like power has no psychic components");Set(spell,"Blueprint",new BlueprintAbility{Type=AbilityType.Spell});
        var item=Empty<ItemEntityUsable>();var itemFact=Empty<Ability>();Set(itemFact,"<SourceItem>k__BackingField",item);Set(spell,"m_Fact",itemFact);
        evt=Params();Call(medium,"ApplyThoughtPenalty",evt);Check((int)Get(evt,"m_BonusConcentration")==0,"Scroll/item casting remains native");Set(spell,"m_Fact",null);
        var state=Empty<UnitState>();Set(state,"m_Conditions",new sbyte[100]);Set(state,"m_ConditionsImmunity",new sbyte[100]);
        var body=Empty<UnitBody>();Set(body,"<IsPolymorphed>k__BackingField",true);
        void CastingChecks(bool withCotW) {
            foreach(var silence in new[]{"e4b8a158e8aabfe41b5e0bd334d8c386","73e09d4c3e3a4d918988a0aae069f372"}) {
                var bp=new BlueprintBuff{AssetGuid=BlueprintGuid.Parse(silence),ComponentsArray=new BlueprintComponent[]{new AddCondition{Condition=UnitCondition.SpellcastingForbidden}}};bp.OnEnable();buffs.Enumerable.Add(Buff(bp));
                ((sbyte[])Get(state,"m_Conditions"))[(int)UnitCondition.SpellcastingForbidden]=1;
                Check(!(bool)Call(medium,"CheckCastingCondition",state,UnitCondition.SpellcastingForbidden,spell),"Known silence alone exempt");
                ((sbyte[])Get(state,"m_Conditions"))[(int)UnitCondition.SpellcastingForbidden]=2;
                Check((bool)Call(medium,"CheckCastingCondition",state,UnitCondition.SpellcastingForbidden,spell),"Silence plus rage/Transformation source remains forbidden");
                buffs.Enumerable.Clear();
            }
            Check((bool)Call(medium,"CheckCastingCondition",state,UnitCondition.SpellcastingForbidden,spell),"Unknown prohibition retained");
            Check(!(bool)Call(medium,"CheckPolymorph",body,spell),"Medium can cast while polymorphed");
            Set(book,"Blueprint",ordinary);Check((bool)Call(medium,"CheckPolymorph",body,spell),"Ordinary book retains polymorph restriction");
            Set(book,"Blueprint",main);
            if(withCotW) {
                var psychic=new BlueprintSpellbook{ComponentsArray=new[]{(BlueprintComponent)Activator.CreateInstance(cotw.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicSpellbookMarker"))}};
                Set(book,"Blueprint",psychic);Check(!(bool)Call(medium,"CheckPolymorph",body,spell),"CotW exemption retained by composed wrapper");
                evt=Params();Call(medium,"ApplyThoughtPenalty",evt);Call(cotw,"ApplyThoughtPenalty",evt);Check((int)Get(evt,"m_BonusConcentration")==-10,"CotW concentration penalty applied once");
                Set(book,"Blueprint",main);evt=Params();Call(cotw,"ApplyThoughtPenalty",evt);Call(medium,"ApplyThoughtPenalty",evt);Check((int)Get(evt,"m_BonusConcentration")==-10,"Medium concentration penalty applied once");
            }
        }
        void Patch(Type type,Harmony harmony) {
            var nested=type.GetNestedTypes(BindingFlags.NonPublic).Where(t=>t.GetCustomAttributes(typeof(HarmonyPatch),false).Length>0).ToArray();
            Check(nested.Length==9,"Nine production casting patch classes");
            foreach(var t in nested)harmony.CreateClassProcessor(t).Patch();
            Check(harmony.GetPatchedMethods().Count()==9,"Nine real game targets patched");
        }
        var own=new Harmony("MediumPsychic.Tests");var other=new Harmony("CotW.Psychic.CoexistenceTests");
        void MetamagicChecks() {
            Check(Logical.DefaultCost()==1&&Metamagic.Piercing.DefaultCost()==1&&Metamagic.Quicken.DefaultCost()==4,"Logical cost +1 leaves native costs");
            Check(UIUtilityTexts.GetMetamagicName(Logical)=="Logical","Logical English UI name");
            Check(UIUtilityTexts.GetMetamagicList(Logical)=="Logical","Exactly one shared Logical UI label");
            var bp=new BlueprintFeature{ComponentsArray=new BlueprintComponent[]{new AddMetamagicFeat{Metamagic=Logical}}};bp.OnEnable();
            var feat=Empty<Feature>();Set(feat,"<Blueprint>k__BackingField",bp);
            var component=bp.GetComponent<AddMetamagicFeat>();var runtime=component.CreateRuntimeFactComponent();runtime.Setup(component);runtime.RestoreOwnerLink(feat);Set(feat,"Components",new List<EntityFactComponent>{runtime});
            var collect=Empty<RuleCollectMetamagic>();Set(collect,"Spellbook",book);Set(collect,"Spell",spell.Blueprint);Set(collect,"m_SpellLevel",1);Set(collect,"KnownMetamagics",new List<Feature>());Set(collect,"SpellMetamagics",new List<Feature>());
            collect.AddMetamagic(feat);collect.AddMetamagic(feat);Check(collect.SpellMetamagics.Count(f=>f==feat)==1,"Logical offered once for Medium spell");
            Set(collect,"m_SpellLevel",6);Set(collect,"SpellMetamagics",new List<Feature>());collect.AddMetamagic(feat);Check(!collect.SpellMetamagics.Contains(feat),"Medium cannot create seventh-circle recipe");
            Set(collect,"m_SpellLevel",1);Set(book,"Blueprint",ordinary);collect.AddMetamagic(feat);Check(!collect.SpellMetamagics.Contains(feat),"Logical not offered on ordinary book");Set(book,"Blueprint",main);
        }
        try {
            Patch(medium,own);CastingChecks(false);MetamagicChecks();own.UnpatchAll(own.Id);
            foreach(bool mediumFirst in new[]{true,false}) {
                if(mediumFirst){Patch(medium,own);Patch(cotw,other);}else{Patch(cotw,other);Patch(medium,own);}
                CastingChecks(true);MetamagicChecks();
                other.UnpatchAll(other.Id);Check(own.GetPatchedMethods().Count()==9,"Unpatch CotW retains Medium targets");CastingChecks(false);MetamagicChecks();own.UnpatchAll(own.Id);
            }
            Patch(cotw,other);Patch(medium,own);own.UnpatchAll(own.Id);Check(other.GetPatchedMethods().Count()==9,"Unpatch Medium retains CotW targets");other.UnpatchAll(other.Id);
            Check(!own.GetPatchedMethods().Any()&&!other.GetPatchedMethods().Any(),"Owner patches fully removed");
        } finally {own.UnpatchAll(own.Id);other.UnpatchAll(other.Id);}
        // The integrated module uses only CotW's runtime casting owner.
        var integration=cotw.Assembly.GetType("CallOfTheWild.Wrath.Prototype.MediumIntegration",true);
        var mediumClass=new BlueprintCharacterClass { AssetGuid=BlueprintGuid.Parse(Id("Medium")) };
        var mediumClassRef=BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(mediumClass);
        Set(mediumClassRef,"<Cached>k__BackingField",mediumClass);Set(main,"m_CharacterClass",mediumClassRef);
        var psychicRequirement=new Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteClassLevel { Level=1 };
        var shared=new BlueprintFeature { ComponentsArray=new BlueprintComponent[] { psychicRequirement } };
        Call(integration,"BindSharedBlueprints",main,shared);Call(integration,"BindSharedBlueprints",main,shared);
        Check(main.ComponentsArray.Count(c=>c.GetType().Name=="PsychicSpellbookMarker")==1,"shared Medium marker registered once");
        var requirements=shared.GetComponents<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteClassLevel>().ToArray();
        Check(requirements.Length==2&&requirements.All(c=>c.Group==Kingmaker.Blueprints.Classes.Prerequisites.Prerequisite.GroupType.Any),"Logical accepts Psychic OR Medium");
        Check(requirements.Single(c=>((BlueprintCharacterClassReference)Get(c,"m_CharacterClass"))?.Guid==mediumClass.AssetGuid).Level==1,"shared Logical available at Medium 1");
        // Reuse the exhaustive casting matrix against the shared production helpers.
        var legacyHelpers=medium;medium=cotw;
        Patch(cotw,other);CastingChecks(false);MetamagicChecks();
        evt=Params();Call(cotw,"ApplyThoughtPenalty",evt);Check((int)Get(evt,"m_BonusConcentration")==-10,"integrated Medium receives exactly one thought penalty");
        Set(center,"<IsTurnedOn>k__BackingField",true);buffs.Enumerable.Add(center);
        evt=Params();Call(cotw,"ApplyThoughtPenalty",evt);Check((int)Get(evt,"m_BonusConcentration")==0,"old Medium Center Self works with shared casting");
        buffs.Enumerable.Clear();other.UnpatchAll(other.Id);medium=legacyHelpers;
        profiling.UnpatchAll(profiling.Id);
        File.WriteAllText(args[4],"{\"assertionsPassed\":"+assertions+",\"harmony\":\""+typeof(Harmony).Assembly.GetName().Version+"\",\"newProductionTargets\":11,\"castingTargets\":9,\"spiritDetachTargets\":2,\"gameTested\":false,\"scope\":\"Actual managed game types and production helpers/patch installation; native buff duration calculation, normal ability and five Death Curse toggle retention/removal with both production detach prefixes and native UI dirty flag, both CotW casting load orders and owner removal. Synthetic inactive facts/non-player unit and seeded empty Game/Player/party services; Unity profiling and diagnostic Error logging bypassed. Actual post-load helper removal entry is exercised, not save deserialization. No actual buff application, Unity, combat, rest, leveling UI or game save pipeline.\"}");
        Console.WriteLine("PASS: "+assertions+" Medium psychic managed assertions; Harmony "+typeof(Harmony).Assembly.GetName().Version+". No game test.");
    }
}
