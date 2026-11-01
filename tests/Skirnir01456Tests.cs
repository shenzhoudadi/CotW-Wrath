using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static bool ShieldRules056=>Main.GetName().Version>=new Version(0,14,56,0);
    static void Skirnir056(BlueprintsCache parent,Harmony h)
    {
        EquipmentPatches(h);var c=Fresh(parent);Register(c,"Skirnir01454");var root=A<BlueprintArchetype>(c,"SkirnirArchetype01454");var upgrade=A<BlueprintFeature>(c,"SkirnirGreaterShieldCombat01454");var greater=N<BlueprintFeature>(c,"379887a82a7248946bbf6d0158663b5e");var improved=N<BlueprintFeature>(c,"836879fcd5b29754eb664a090bd6c22f");
        var cls=N<BlueprintCharacterClass>(c,"45a4607686d96a1498891b3286121780");Check(cls.Progression.UIGroups.Sum(g=>g.Features.Count(f=>f==greater))==1,"native Greater Spell Combat occurs in exactly one class UI group");
        var effective=new ProgressionData(cls.Progression);effective.AddArchetype(root);Check(effective.GetLevelEntry(14).Features.Contains(improved)&&!effective.GetLevelEntry(14).Features.Contains(greater),"actual native ProgressionData removes base Greater at14 and retains delayed Improved");Check(effective.GetLevelEntry(19).Features.Contains(greater)==ShieldRules056,"actual native ProgressionData merges Greater into19 only under056 rules");
        var nativeBonus=greater.GetComponent<ConcentrationBonus>();Check(nativeBonus!=null&&nativeBonus.CheckFact&&nativeBonus.Value.Value==2,"actual native Greater Spell Combat has conditional native concentration component");
        Check(root.AddFeatures.Single(e=>e.Level==19).Features.Single()==(ShieldRules056?greater:upgrade),"nineteenth-level progression grants native Greater Spell Combat under056 rules, old node remains loadable");Check(root.AddFeatures.Single(e=>e.Level==14).Features.Contains(improved),"native Improved Spell Combat stays at fourteen");
        Check(root.RemoveFeatures.Single(e=>e.Level==14).Features.Contains(greater),"base Magus Greater Spell Combat remains removed at fourteen");
        Check(upgrade.GetComponents<AddFeatureIfHasFact>().Any(a=>a.Not&&a.CheckedFact==greater&&a.Feature==greater)==ShieldRules056,"new policy grants actual native Greater Spell Combat only when absent; old055 control lacks it");
        if(ShieldRules056){Check(!greater.HideInUI&&!greater.HideInCharacterSheetAndLevelUp&&upgrade.HideInUI&&upgrade.HideInCharacterSheetAndLevelUp,"native nineteenth-level upgrade visible while legacy node stays hidden");Check(UiDescription(A<BlueprintFeature>(c,"SkirnirShieldSpellCombat01454")).Contains("he gains Greater Spell Combat")&&!UiDescription(A<BlueprintFeature>(c,"SkirnirShieldSpellCombat01454")).Contains("forfeiting"),"new combat tooltip describes retained AC and native nineteenth-level upgrade");}
        var levels=new JArray();
        foreach(int level in new[]{7,8,14,18,19,20}){
            var u=EquipmentUnit(c,level);u.Progression.Classes[0].AddArchetype(root);
            foreach(var entry in root.AddFeatures.Where(e=>e.Level<=level))foreach(var feature in entry.Features.Where(f=>f==upgrade||f==greater||f==improved||f.AssetGuid==Id("SkirnirShieldSpellCombat01454")))Grant(u,(BlueprintFeature)feature);
            bool expected=ShieldRules056&&level>=19;Check(u.HasFact(greater)==expected,"native fact membership at actual Magus level "+level);Check(u.Facts.List.Count(f=>f.Blueprint==greater)==(expected?1:0),"single native Greater Spell Combat fact at "+level);
            var fact=u.GetFact(greater);if(fact!=null){Check(((Feature)fact).Rank==1,"native Greater Spell Combat rank is one");var bonus=fact.Components.Single(x=>x.SourceBlueprintComponent is ConcentrationBonus);int Amount(){var rule=new RuleCalculateAbilityParams(u,new Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility(),null);using(bonus.RequestEventContext())((ConcentrationBonus)bonus.SourceBlueprintComponent).OnEventAboutToTrigger(rule);return (int)AccessTools.Field(typeof(RuleCalculateAbilityParams),"m_BonusConcentration").GetValue(rule);}
                Check(Amount()==0,"native Greater Spell Combat requires defensive-casting fact");var defensive=Add(u,(BlueprintBuff)nativeBonus.RequiredFact);Check(Amount()==2,"actual native concentration callback adds two exactly once");defensive.Remove();}
            if(level>=14){var defensive=Add(u,(BlueprintBuff)nativeBonus.RequiredFact);var rule=new RuleCalculateAbilityParams(u,new Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility(),null);RulebookEventBus.OnEventAboutToTrigger(rule);Check((int)AccessTools.Field(typeof(RuleCalculateAbilityParams),"m_BonusConcentration").GetValue(rule)==(expected?4:2),"actual native event bus combines Improved and Greater concentration without duplication");defensive.Remove();}
            levels.Add(new JObject{["level"]=level,["nativeGreaterSpellCombat"]=u.HasFact(greater),["count"]=u.Facts.List.Count(f=>f.Blueprint==greater)});
        }
        var saved=EquipmentUnit(c,19);saved.Progression.Classes[0].AddArchetype(root);var published=upgrade.ComponentsArray;Feature legacy;
        try{upgrade.ComponentsArray=Array.Empty<BlueprintComponent>();legacy=Grant(saved,upgrade);}finally{upgrade.ComponentsArray=published;}
        Set(legacy,"m_BlueprintComponents",null);Set(legacy,"IsPostLoadExecuted",false);legacy.PostLoad();legacy.ApplyPostLoadFixes();Check(saved.HasFact(greater)==ShieldRules056,"actual native post-load of old empty nineteenth-level node grants new native feature");legacy.ApplyPostLoadFixes();Check(saved.Facts.List.Count(f=>f.Blueprint==greater)==(ShieldRules056?1:0),"native repeated post-load does not duplicate grant");saved.Facts.Remove(legacy);Check(!saved.HasFact(greater),"removing own upgrade cleans only its native child");
        var ordinary=EquipmentUnit(c,19);var external=Grant(ordinary,greater);var own=Grant(ordinary,upgrade);Check(ordinary.Facts.List.Count(f=>f.Blueprint==greater)==1&&external.Rank==1,"preexisting ordinary Magus native Greater Spell Combat never stacks");ordinary.Facts.Remove(own);Check(ordinary.HasFact(greater)&&!external.IsDisposed,"removing Skirnir wrapper preserves externally owned native feature");
        var acRows=new JArray();foreach(bool improvedBash in new[]{false,true})foreach(bool legacyPenalty in new[]{false,true}){
            var u=EquipmentUnit(c,8);Grant(u,c,"SkirnirShieldSpellCombat01454");if(improvedBash)H("Fact",u,N<BlueprintFeature>(c,"121811173a614534e8720d7550aae253"));var shield=root.StartingItems.OfType<Kingmaker.Blueprints.Items.Shields.BlueprintItemShield>().Single().CreateEntity<ItemEntityShield>();using(ContextData<ItemEntity.CanBeEquippedForce>.Request())Check(u.Body.SecondaryHand.InsertItem(shield),"native plain heavy shield equips for policy test");var enemy=EquipmentUnit(c,8);var native=Add(u,N<BlueprintBuff>(c,"7b4cf64d3a49e3d45b1dbd2385f4eb6d"));var automatic=u.Buffs.Enumerable.SingleOrDefault(b=>b.Blueprint.AssetGuid==Id("SkirnirCombatPenalty01454"));Check((automatic==null)==ShieldRules056,"actual native combat buff does not create own penalty under056 policy");var oldBuff=legacyPenalty?Add(u,A<BlueprintBuff>(c,"SkirnirCombatPenalty01454")):automatic;
            var ac=new RuleCalculateAC(enemy,u,AttackType.Melee){ForceFlatFooted=true};var events=new RulebookEventContext();var field=AccessTools.Field(typeof(BardMasterpieces01435Tests),"current");var previous=field.GetValue(null);events.PushEvent(ac);field.SetValue(null,events);
            try{RulebookEventBus.OnEventAboutToTrigger(ac);ac.OnTrigger(events);RulebookEventBus.OnEventDidTrigger(ac);Check(u.Stats.AC.ModifiedValue==12&&ac.Result==(ShieldRules056?12:10),"actual native attack AC retains heavy shield, including old penalty buff and Improved Shield Bash controls");acRows.Add(new JObject{["improvedShieldBash"]=improvedBash,["legacyPenaltyBuff"]=legacyPenalty,["sheetAC"]=u.Stats.AC.ModifiedValue,["actualAC"]=ac.Result,["automaticPenalty"]=automatic!=null});}finally{events.PopEvent(ac);field.SetValue(null,previous);native.Remove();oldBuff?.Remove();}
        }
        File.WriteAllText(Environment.GetEnvironmentVariable("COTW_01455_OBSERVATION"),new JObject{["rules056"]=ShieldRules056,["nativeGreaterId"]=greater.AssetGuid.ToString(),["levels"]=levels,["shield"]=acRows,["nativeOldNodePostLoadChecked"]=true,["nativeConcentrationCallbackChecked"]=true,["externalOwnershipPreserved"]=true,["boundary"]="Actual native equipment, attack AC, AddFacts, EntityFact.PostLoad/ApplyPostLoadFixes and native ConcentrationBonus execute. Level entries are applied from the actual Skirnir progression at actual class levels; full Unity level-up screens, save-file transport, turns and rendering are tester observations."}.ToString());
    }
}
