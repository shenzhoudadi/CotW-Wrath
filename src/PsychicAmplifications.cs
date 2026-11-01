using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicAmplifications
    {
        internal const string ModuleName="Psychic phrenic amplifications";
        internal static readonly int[] ChoiceLevels={1,3,7,11,15,19};
        internal static void Register(BlueprintsCache cache)
        {
            var f=new PsychicAssets(cache);
            var character=f.Native<BlueprintCharacterClass>(PsychicClass.ClassId);
            var progression=f.Native<BlueprintProgression>(PsychicClass.ProgressionId);
            var wisdom=f.Native<BlueprintAbilityResource>(PsychicClass.WisdomPoolId);
            var charisma=f.Native<BlueprintAbilityResource>(PsychicClass.CharismaPoolId);
            var modes=new List<BlueprintActivatableAbility>();
            const string usage=" Only one amplification mode can be active. Clicking another mode immediately switches to it. Points are spent on each eligible leveled Psychic spell cast; other spells and casts without enough points are unmodified. Failed casts refund the points.";
            const string heal="Spend 1 phrenic point when casting a transmutation spell to heal yourself for 2 hit points per level of that spell.";
            const string defense="Spend 1 or 2 phrenic points when casting a divination spell to gain a +2 or +4 insight bonus to AC for a number of rounds equal to that spell's level.";
            const string mind="Spend 2 phrenic points when casting a mind-affecting spell to increase its saving throw DC by 1. At psychic level 8 you can instead spend 4 points for +2 DC, and at level 15 spend 6 points for +3 DC. The bonus applies to the cast, not the idle spell tooltip.";
            BlueprintActivatableAbility Mode(string token,string title,string description,PsychicAmplificationKind kind,int cost,int strength,int level,BlueprintBuff effect=null) {
                var component=new PsychicAmplificationComponent { name="LinkedPhrenicCast", Kind=kind, Cost=cost, Strength=strength, MinimumLevel=level,
                    CharacterClass=Ref<BlueprintCharacterClassReference>(character), WisdomPool=Ref<BlueprintAbilityResourceReference>(wisdom), CharismaPool=Ref<BlueprintAbilityResourceReference>(charisma),
                    Defense=effect==null?null:Ref<BlueprintBuffReference>(effect) };
                var buff=f.Buff(token+"Mode",title,description+usage,component);
                // Like CotW's free-action buffToToggle: clear the mode when switched off,
                // otherwise native OnDidTurnOff leaves its restriction fact until the next round.
                var toggle=f.Add(new BlueprintActivatableAbility { ActivationType=AbilityActivationType.Immediately, DeactivateImmediately=true, DeactivateIfOwnerDisabled=true,
                    DeactivateIfOwnerUnconscious=true, ComponentsArray=Array.Empty<BlueprintComponent>() },token+"Toggle",title,description+usage);
                Set(toggle,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free); Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(buff));
                modes.Add(toggle); return toggle;
            }
            var healing=Mode("BiokineticHealing","Biokinetic Healing (1 point)",heal,PsychicAmplificationKind.BiokineticHealing,1,2,1);
            var healingFeature=f.Feature("BiokineticHealingFeature","Biokinetic Healing",heal+usage,Facts(healing));
            var defenses=new List<BlueprintActivatableAbility>();
            for(int i=1;i<=2;i++) {
                var effect=f.Buff("DefensivePrognostication"+i+"Effect","Defensive Prognostication +"+(2*i),defense,
                    new AddStatBonus { name="PhrenicInsightAC",Stat=StatType.AC,Value=2*i,Descriptor=ModifierDescriptor.Insight });
                defenses.Add(Mode("DefensivePrognostication"+i,"Defensive Prognostication ("+i+" point"+(i==1?"":"s")+")",defense,PsychicAmplificationKind.DefensivePrognostication,i,2*i,1,effect));
            }
            var defenseFeature=f.Feature("DefensivePrognosticationFeature","Defensive Prognostication",defense+usage,Facts(defenses.ToArray()));
            var mindComponents=new List<BlueprintComponent>();
            for(int i=1;i<=3;i++) {
                int level=i==1?1:i==2?8:15;
                var toggle=Mode("OverpoweringMind"+i,"Overpowering Mind ("+(i*2)+" points)",mind,PsychicAmplificationKind.OverpoweringMind,2*i,i,level);
                if(i==1) mindComponents.Add(Facts(toggle));
                else {
                    var tier=f.Feature("OverpoweringMindTier"+i,"Overpowering Mind +"+i,mind+usage,Facts(toggle));
                    var add=new AddFeatureOnClassLevel { name="PhrenicMindTier"+i,Level=level };
                    Set(add,"m_Class",Ref<BlueprintCharacterClassReference>(character)); Set(add,"m_Feature",Ref<BlueprintFeatureReference>(tier));
                    Set(add,"m_AdditionalClasses",Array.Empty<BlueprintCharacterClassReference>());Set(add,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());
                    mindComponents.Add(add);
                }
            }
            var mindFeature=f.Feature("OverpoweringMindFeature","Overpowering Mind",mind+usage,mindComponents.ToArray());
            const string relentlessText="Spend 1 phrenic point when casting a spell subject to spell resistance to roll its caster level checks to overcome spell resistance twice and take the higher result. The point is spent even if the target has no spell resistance.";
            var relentless=Mode("RelentlessCasting","Relentless Casting (1 point)",relentlessText,
                PsychicAmplificationKind.RelentlessCasting,1,0,1);
            var relentlessFeature=f.Feature("RelentlessCastingFeature","Relentless Casting",relentlessText+usage,Facts(relentless));
            const string ongoingText="Spend 1 phrenic point when casting Mental Barrier, Thought Shield or Intellect Fortress to extend its duration by 1 round. This extra round is added after Extend Spell.";
            var ongoing=Mode("OngoingDefense","Ongoing Defense (1 point)",ongoingText,
                PsychicAmplificationKind.OngoingDefense,1,0,1);
            var ongoingFeature=f.Feature("OngoingDefenseFeature","Ongoing Defense",ongoingText+usage,Facts(ongoing));
            BlueprintFeature Linked(string token,string title,string description,PsychicAmplificationKind kind,int cost,BlueprintBuff effect=null) {
                var mode=Mode(token,title+" ("+cost+" point"+(cost==1?"":"s")+")",description,kind,cost,0,1,effect);
                return f.Feature(token+"Feature",title,description+usage,Facts(mode),new PsychicLinkedSpellEffects { name="Paid"+token,Kind=kind });
            }
            var forceFeature=Linked("FocusedForce","Focused Force",
                "Spend 1 phrenic point when casting a spell that deals force damage with dice. Increase each force damage die by one step: d4 to d6, d6 to d8, d8 to d10, or d10 to d12. Flat damage and d12s are unchanged. This affects all projectiles of the linked spell.",PsychicAmplificationKind.FocusedForce,1);
            var eyeFeature=Linked("MindsEye","Mind's Eye",
                "Spend 2 phrenic points when casting a spell that requires a ranged attack roll to gain a +4 insight bonus on its ranged attack rolls. This bonus does not stack with other insight bonuses to the same roll.",PsychicAmplificationKind.MindsEye,2);
            const string armorText="Spend 1 phrenic point when casting a conjuration summoning spell to grant each summoned creature a +2 deflection bonus to AC for 1 round per caster level or until it disappears. The bonus increases to +3 at psychic level 8 and +4 at level 15.";
            var armorRank=new ContextRankConfig { name="ConjuredArmorClassScaling" };
            SetEnum(armorRank,"m_BaseValueType","ClassLevel");SetEnum(armorRank,"m_Progression","Custom");
            Set(armorRank,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(character)});
            var rowType=typeof(ContextRankConfig).GetNestedType("CustomProgressionItem",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
            var table=Array.CreateInstance(rowType,3);
            for(int i=0;i<3;i++) {var row=Activator.CreateInstance(rowType);Set(row,"BaseValue",i==0?7:i==1?14:int.MaxValue);Set(row,"ProgressionValue",i+2);table.SetValue(row,i);}
            Set(armorRank,"m_CustomProgression",table);
            var armor=f.Buff("ConjuredArmorEffect","Conjured Armor",armorText,armorRank,
                new AddContextStatBonus {name="ConjuredDeflectionAC",Stat=StatType.AC,Descriptor=ModifierDescriptor.Deflection,Value=new ContextValue {ValueType=ContextValueType.Rank,ValueRank=AbilityRankType.Default}});
            var armorFeature=Linked("ConjuredArmor","Conjured Armor",armorText,PsychicAmplificationKind.ConjuredArmor,1,armor);
            // Buff activation switches the previous mode off. Sibling buttons stay clickable;
            // no global enum group is reserved and paid spell effects are independent.
            var selection=f.Add(new BlueprintFeatureSelection { Ranks=8,IsClassFeature=true },"PhrenicAmplificationSelection","Phrenic Amplification (Prototype)",
                "Choose one phrenic amplification at psychic levels 1, 3, 7, 11, 15 and 19, and two more with Phrenic Mastery at level 20. Nine basic amplifications are available, including Psychofeedback spell-slot conversions. Other basic and major amplifications are still being ported.");
            var feedback=PsychicFurtherOptions.Psychofeedback(f,character);
            PsychicAmplificationMenu.Build(f,modes,defenseFeature,mindFeature,feedback);
            var choices=new[] {healingFeature,defenseFeature,mindFeature,relentlessFeature,ongoingFeature,forceFeature,eyeFeature,armorFeature,feedback};
            var feats=PsychicFurtherOptions.Feats(f,character,choices);
            var basic=f.Native<BlueprintFeatureSelection>(BlueprintRegistration.BasicFeatsId);
            var oldBasic=(BlueprintFeatureReference[])Get(basic,"m_Features");var oldAll=(BlueprintFeatureReference[])Get(basic,"m_AllFeatures");
            foreach(var feature in choices) feature.IsClassFeature=true;
            Set(selection,"m_Features",choices.Select(Ref<BlueprintFeatureReference>).ToArray()); Set(selection,"m_AllFeatures",choices.Select(Ref<BlueprintFeatureReference>).ToArray());
            IncreaseResourceAmount Increase(BlueprintAbilityResource pool,string name) { var c=new IncreaseResourceAmount {name=name,Value=6};Set(c,"m_Resource",Ref<BlueprintAbilityResourceReference>(pool));return c; }
            var mastery=f.Feature("PhrenicMasteryFeature","Phrenic Mastery","At psychic level 20, your phrenic pool increases by 6 and you learn two additional phrenic amplifications.",Increase(wisdom,"MasteryWisdomPool"),Increase(charisma,"MasteryCharismaPool"));
            mastery.IsClassFeature=true;
            foreach(var pair in f.Assets) if(pair.Value is BlueprintUnitFact fact) {
                if(pair.Key.StartsWith("Psychofeedback") && !pair.Key.StartsWith("PsychofeedbackMenu")) continue;
                string donor=pair.Key.StartsWith("FocusedForce")?"0a2f7c6aa81bc6548ac7780d8b70bcbc":pair.Key.StartsWith("MindsEye")?"2c38da66e5a599347ac95b3294acbe00":pair.Key.StartsWith("ConjuredArmor")?"9e1ad5d6f87d19e4d8883d63a6e35568":pair.Key.StartsWith("Biokinetic")?"c927a8b0cd3f5174f8c0b67cdbfde539":pair.Key.StartsWith("Defensive")?"62888999171921e4dafb46de83f4d67d":"eabf94e4edc6e714cabd96aa69f8b207";
                Set(fact,"m_Icon",f.Native<BlueprintAbility>(donor).Icon);
            }
            f.Enable();
            var expected=f.Assets.Values.ToArray(); var existing=expected.Select(a=>cache.Load(a.AssetGuid)).ToArray();
            for(int i=0;i<expected.Length;i++) if(existing[i]!=null && (existing[i].name!=expected[i].name || existing[i].GetType()!=expected[i].GetType() ||
                Fingerprint(PsychicClassCompletion01458.RegistrationShape((BlueprintScriptableObject)existing[i]))!=Fingerprint(expected[i])))
                    throw new InvalidOperationException("Phrenic amplification identity/structure conflict: "+expected[i].AssetGuid);
            var entries=ChoiceLevels.Concat(new[]{20}).Select(l=>progression.LevelEntries.Single(e=>e.Level==l)).ToArray();
            var old=entries.Select(e=>e.Features.ToArray()).ToArray();
            if(existing.All(a=>a!=null)) {
                if(entries.Any(e=>e.Features.Count(a=>a.AssetGuid==selection.AssetGuid)!=(e.Level==20?2:1)) || entries.Last().Features.Count(a=>a.AssetGuid==mastery.AssetGuid)!=1) throw new InvalidOperationException("Phrenic progression link missing or duplicated.");
                if(feats.Any(a=>oldBasic.Count(r=>r.Guid==a.AssetGuid)!=1 || oldAll.Count(r=>r.Guid==a.AssetGuid)!=1)) throw new InvalidOperationException("Phrenic feat link missing or duplicated.");
                return;
            }
            if(existing.Any(a=>a!=null) || old.Any(fs=>fs.Any(a=>a.AssetGuid==selection.AssetGuid || a.AssetGuid==mastery.AssetGuid)) ||
                (oldBasic??Array.Empty<BlueprintFeatureReference>()).Concat(oldAll??Array.Empty<BlueprintFeatureReference>()).Any(r=>r!=null && feats.Any(a=>r.Guid==a.AssetGuid)))
                throw new InvalidOperationException("Partial phrenic registration.");
            var added=new List<BlueprintScriptableObject>();
            try {
                foreach(var asset in expected) {cache.AddCachedBlueprint(asset.AssetGuid,asset);added.Add(asset);}
                for(int i=0;i<entries.Length;i++) {entries[i].SetFeatures(old[i].Concat(entries[i].Level==20?new BlueprintFeatureBase[]{mastery,selection,selection}:new BlueprintFeatureBase[]{selection}).ToArray());Set(entries[i],"m_FeaturesList",null);}
                Set(basic,"m_Features",(oldBasic??Array.Empty<BlueprintFeatureReference>()).Concat(feats.Select(Ref<BlueprintFeatureReference>)).ToArray());
                Set(basic,"m_AllFeatures",(oldAll??Array.Empty<BlueprintFeatureReference>()).Concat(feats.Select(Ref<BlueprintFeatureReference>)).ToArray());
            } catch {
                Set(basic,"m_Features",oldBasic);Set(basic,"m_AllFeatures",oldAll);
                for(int i=0;i<entries.Length;i++) {entries[i].SetFeatures(old[i]);Set(entries[i],"m_FeaturesList",null);}
                foreach(var asset in added) cache.RemoveCachedBlueprint(asset.AssetGuid); throw;
            }
        }
        private static string Fingerprint(BlueprintScriptableObject asset) {
            string value=PsychicExpansion.Signature(asset.ComponentsArray);
            if(asset is BlueprintFeature feature) value+="|"+feature.Ranks+"|"+feature.IsClassFeature;
            if(asset is BlueprintFeatureSelection choice) value+="|"+PsychicExpansion.Signature(Get(choice,"m_AllFeatures"))+"|"+PsychicExpansion.Signature(Get(choice,"m_Features"));
            if(asset is BlueprintActivatableAbility mode) value+="|"+mode.Buff.AssetGuid+"|"+mode.ActivationType+"|"+Get(mode,"m_ActivateWithUnitCommand")+"|"+mode.Group+"|"+mode.IsOnByDefault+"|"+mode.DeactivateImmediately;
            if(asset is BlueprintBuff buff) value+="|"+buff.Stacking;
            return value;
        }
    }
}
