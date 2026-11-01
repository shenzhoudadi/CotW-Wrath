using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicFaith
    {
        private const string DivineText="Convert a Psychic spell slot into a cure or inflict spell of the same level, using the conversion arrow under a known spell. Choose healing or wounding as permitted by your alignment and deity. Each conversion spends one daily use and its original spell slot. A successful conversion restores 1 phrenic point, up to your pool's maximum. Uses per day equal your Wisdom modifier (minimum 0). Converted spells are psychic spells, but are not added to your spells known or class spell list.";
        private const string AuraText="As a free action, project a 30-foot prayer aura for a total number of rounds per day equal to your psychic level. Allies (including you) gain a +1 luck bonus on attack rolls, weapon damage, saving throws and skill checks. Enemies take a -1 penalty to those rolls, or -2 if they oppose you on either the good/evil or lawful/chaotic axis. Opposing both axes still gives only -2. Turning the aura off removes its effects immediately; combat ending, unconsciousness or disability also ends it.";

        internal static BlueprintProgression Build(PsychicAssets f, BlueprintCharacterClass character)
        {
            var prayer=f.Native<BlueprintBuff>("789bae3802e7b6b4c8097aaf566a1cf5");
            var uses=Resource(f,character,"FaithDivineEnergyUses","Divine Energy",true);
            var options=new[]{Conversion(f,character,uses,true),Conversion(f,character,uses,false)};
            var divine=f.Add(new BlueprintFeatureSelection {Ranks=1,IsClassFeature=true},"FaithDivineEnergySelection","Divine Energy",DivineText);
            Set(divine,"m_Icon",prayer.Icon);Set(divine,"m_AllFeatures",options.Select(Ref<BlueprintFeatureReference>).ToArray());Set(divine,"m_Features",options.Select(Ref<BlueprintFeatureReference>).ToArray());

            var rank=new ContextRankConfig {name="FaithResistanceByPsychicLevel"};
            SetEnum(rank,"m_BaseValueType","ClassLevel");SetEnum(rank,"m_Progression","OnePlusDivStep");Set(rank,"m_StepLevel",5);Set(rank,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(character)});
            var saves=new[]{StatType.SaveFortitude,StatType.SaveReflex,StatType.SaveWill}.Select(stat=>(BlueprintComponent)new AddContextStatBonus {name="FaithResistance"+stat,Stat=stat,Descriptor=ModifierDescriptor.Resistance,Value=new ContextValue {ValueType=ContextValueType.Rank,ValueRank=AbilityRankType.Default}}).Concat(new[]{rank}).ToArray();
            var resilience=f.Feature("FaithResilience","Resilience of the Faithful","At psychic level 5, gain a +2 resistance bonus on all saving throws. This bonus increases to +3 at level 10, +4 at level 15 and +5 at level 20. It does not stack with other resistance bonuses.",saves);
            resilience.ReapplyOnLevelUp=true;Set(resilience,"m_Icon",f.Native<BlueprintFeature>("a05a8959c594daa40a1c5add79566566").Icon);

            var spells=new[]{f.Native<BlueprintAbility>("90e59f4a4ada87243b7b3535a06d0638"),f.Find<BlueprintAbility>("ForceSword"),f.Native<BlueprintAbility>("2d4263d80f5136b4296d6eb43a221d7d"),f.Native<BlueprintAbility>("f2115ac1148256b4ba20788f7e966830"),f.Native<BlueprintAbility>("1bc83efec9f8c4b42a46162d72cbf494"),f.Find<BlueprintAbility>("PsychicSurgery"),f.Native<BlueprintAbility>("fafd77c6bfa85c04ba31fdc1c962c914"),f.Native<BlueprintAbility>("d3ac756a229830243a72e84f3ab050d0"),f.Native<BlueprintAbility>("867524328b54f25488d371214eea0d90")};
            var names=new[]{"Bless","Force Sword","Magic Vestment","Restoration","Burst of Glory","Psychic Surgery","Greater Restoration","Summon Monster VIII","Mass Heal"};
            var levels=new[]{1,4,6,8,10,12,14,16,18};
            var bonuses=spells.Select((spell,i)=>{
                var add=new AddKnownSpell {name="FaithBonusSpell"+(i+1),SpellLevel=i+1};Set(add,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(character));Set(add,"m_Spell",Ref<BlueprintAbilityReference>(spell));
                var feature=f.Feature("FaithBonusSpell"+(i+1),"Discipline Spell: "+names[i],"Learn "+names[i]+" in addition to the normal Psychic spells known.",add);Set(feature,"m_Icon",spell.Icon);return feature;
            }).ToArray();
            var noAtheism=new PrerequisiteNoFeature {name="FaithRequiresBelief"};Set(noAtheism,"m_Feature",Ref<BlueprintFeatureReference>(f.Native<BlueprintFeature>("92c0d2da0a836ce418a267093c09ca54")));
            var progression=f.Add(new BlueprintProgression {Ranks=1,IsClassFeature=true,ComponentsArray=new BlueprintComponent[]{noAtheism},UIGroups=Array.Empty<UIGroup>(),LevelEntries=levels.Select((level,i)=>Entry(level,i==0?new BlueprintFeatureBase[]{bonuses[i],divine,f.Native<BlueprintFeature>(PsychicClass.WisdomFeatureId)}:new BlueprintFeatureBase[]{bonuses[i]})).Concat(new[]{Entry(5,resilience),Entry(13,Aura(f,character,prayer))}).OrderBy(e=>e.Level).ToArray()},"FaithDiscipline","Faith","Your belief in a higher power fuels your psychic abilities. Gain Divine Energy, Resilience of the Faithful, Prayer Aura and nine bonus spells. Your phrenic pool is half your psychic level plus your Wisdom modifier. This discipline cannot be chosen with Atheism.");
            Set(progression,"m_Classes",new[]{new BlueprintProgression.ClassWithLevel {m_Class=Ref<BlueprintCharacterClassReference>(character)}});Set(progression,"m_Icon",prayer.Icon);return progression;
        }

        private static BlueprintFeature Conversion(PsychicAssets f,BlueprintCharacterClass character,BlueprintAbilityResource uses,bool healing)
        {
            string token=healing?"FaithCure":"FaithInflict";
            var donor=f.Native<BlueprintFeature>(healing?"5e4620cea099c9345a9207c11d7bc916":"5ba6b9cc18acafd45b6293d1e03221ac");
            var originals=donor.GetComponent<SpontaneousSpellConversion>().SpellsByLevel;
            var refs=Enumerable.Range(0,10).Select(_=>new BlueprintAbilityReference()).ToArray();
            var spellNames=new[]{"", "Light Wounds","Moderate Wounds","Serious Wounds","Critical Wounds","Light Wounds, Mass","Moderate Wounds, Mass","Serious Wounds, Mass","Critical Wounds, Mass","Critical Wounds, Mass"};
            for(int level=1;level<10;level++) {
                var source=originals[level];
                string spellName=(healing?"Cure ":"Inflict ")+spellNames[level];
                var spell=f.Clone(source,token+level,"Divine Energy: "+spellName,DivineText+"\n\nFunctions as "+spellName+". "+(healing?"Positive energy heals living creatures and harms undead. Undead can make a Will save for half damage.":"Negative energy harms living creatures and heals undead. Living creatures can make a Will save for half damage."));
                // Native touch delivery stays native: only the conversion cast
                // pays/restores resources, never a later held-charge delivery.
                spell.ComponentsArray=spell.ComponentsArray.Where(c=>!(c is SpellListComponent)).ToArray();
                var cost=new AbilityResourceLogic {name="FaithConversionDailyCost",Amount=1};Set(cost,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(uses));Set(cost,"m_IsSpendResource",true);
                spell.ComponentsArray=spell.ComponentsArray.Concat(new BlueprintComponent[]{cost}).ToArray();
                spell.LocalizedDuration=Text(token+level,"duration","Instantaneous");spell.LocalizedSavingThrow=Text(token+level,"savingThrow",healing?"Will half (undead)":"Will half (living creatures)");
                Set(spell,"m_Parent",new BlueprintAbilityReference());refs[level]=Ref<BlueprintAbilityReference>(spell);
            }
            var conversion=new SpontaneousSpellConversion {name=token+"Conversion"};Set(conversion,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(character));Set(conversion,"m_SpellsByLevel",refs);
            var allowed=new PrerequisiteFeature {name=token+"DeityPermission"};Set(allowed,"m_Feature",Ref<BlueprintFeatureReference>(f.Native<BlueprintFeature>(healing?"8c769102f3996684fb6e09a2c4e7e5b9":"dab5255d809f77c4395afc2b713e9cd6")));
            var alignment=new PrerequisiteAlignment {name=token+"Alignment",Alignment=AlignmentMaskType.Any & ~(healing?AlignmentMaskType.Evil:AlignmentMaskType.Good)};
            var feature=f.Feature(token+"Feature",healing?"Spontaneous Healing":"Spontaneous Wounding",DivineText,conversion,Grant(uses),allowed,alignment,new PsychicFaithConversionRestore {name="FaithSuccessfulConversionRestore",Conversions=refs.Skip(1).ToArray()});
            Set(feature,"m_Icon",donor.Icon);return feature;
        }

        private static BlueprintFeature Aura(PsychicAssets f,BlueprintCharacterClass character,BlueprintBuff prayer)
        {
            var ally=f.Clone(prayer,"FaithPrayerAlly","Prayer Aura: Ally",AuraText);
            var enemy=f.Clone(f.Native<BlueprintBuff>("890182fa30a5f724c86ce41f237cf95f"),"FaithPrayerEnemy","Prayer Aura: Enemy",AuraText);
            // The native Prayer debuff uses untyped stacking penalties. Give
            // this aura's mutually exclusive -1/-2 tiers a common descriptor
            // so different Faith casters cannot stack both tiers to -3.
            foreach(var component in enemy.ComponentsArray) {
                if(component is AddStatBonus stat) stat.Descriptor=ModifierDescriptor.Luck;
                else if(component is BuffAllSavesBonus saves) saves.Descriptor=ModifierDescriptor.Luck;
                else if(component is BuffAllSkillsBonus skills) skills.Descriptor=ModifierDescriptor.Luck;
            }
            var opposed=f.Clone(enemy,"FaithPrayerOpposed","Prayer Aura: Opposing Alignment",AuraText);
            foreach(var component in opposed.ComponentsArray) {
                if(component is AddStatBonus stat) stat.Value=-2;
                else if(component is BuffAllSavesBonus saves) saves.Value=-2;
                else if(component is BuffAllSkillsBonus skills) skills.Value=-2;
            }
            AbilityAreaEffectBuff ApplyTo(BlueprintBuff buff,params Condition[] conditions) {
                var c=new AbilityAreaEffectBuff {name="FaithAura"+buff.name,CheckConditionEveryRound=true,Condition=new ConditionsChecker {Operation=Operation.And,Conditions=conditions}};Set(c,"m_Buff",Ref<BlueprintBuffReference>(buff));return c;
            }
            var area=f.Add(new BlueprintAbilityAreaEffect {Size=new Feet(30),Shape=AreaEffectShape.Cylinder,AffectEnemies=true,AggroEnemies=false,SpellResistance=false,Fx=new PrefabLink {AssetId="6b75812d8c3b0d34f9bc204d6babc2a1"},ComponentsArray=new BlueprintComponent[]{
                ApplyTo(ally,new ContextConditionIsAlly()),
                ApplyTo(enemy,new ContextConditionIsEnemy(),new PsychicFaithOpposedAlignment {Not=true}),
                ApplyTo(opposed,new ContextConditionIsEnemy(),new PsychicFaithOpposedAlignment())
            }},"FaithPrayerArea","Prayer Aura",AuraText);SetEnum(area,"m_TargetType","Any");
            var add=new AddAreaEffect {name="FaithPrayerAreaHost"};Set(add,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));
            var host=f.Buff("FaithPrayerHost","Prayer Aura",AuraText,add);Set(host,"m_Icon",prayer.Icon);
            var rounds=Resource(f,character,"FaithPrayerRounds","Prayer Aura Rounds",false);
            var cost=new ActivatableAbilityResourceLogic {name="FaithPrayerRoundCost",SpendType=ActivatableAbilityResourceLogic.ResourceSpendType.NewRound};Set(cost,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(rounds));
            var toggle=f.Add(new BlueprintActivatableAbility {ActivationType=AbilityActivationType.Immediately,DeactivateImmediately=true,DeactivateIfCombatEnded=true,DeactivateIfOwnerDisabled=true,DeactivateIfOwnerUnconscious=true,ComponentsArray=new BlueprintComponent[]{cost}},"FaithPrayerToggle","Prayer Aura",AuraText);
            Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(host));Set(toggle,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free);Set(toggle,"m_Icon",prayer.Icon);
            var feature=f.Feature("FaithPrayerFeature","Prayer Aura",AuraText,Facts(toggle),Grant(rounds));Set(feature,"m_Icon",prayer.Icon);return feature;
        }
        private static BlueprintAbilityResource Resource(PsychicAssets f,BlueprintCharacterClass character,string token,string name,bool wisdom)
        {
            var resource=f.Add(new BlueprintAbilityResource(),token,name,wisdom?DivineText:AuraText);var amount=Get(resource,"m_MaxAmount");
            Set(amount,"IncreasedByStat",wisdom);Set(amount,"ResourceBonusStat",StatType.Wisdom);Set(amount,"IncreasedByLevel",!wisdom);Set(amount,"LevelIncrease",1);
            Set(amount,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(character)});Set(amount,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());Set(amount,"m_ClassDiv",Array.Empty<BlueprintCharacterClassReference>());Set(amount,"m_ArchetypesDiv",Array.Empty<BlueprintArchetypeReference>());Set(resource,"m_MaxAmount",amount);return resource;
        }
        private static AddAbilityResources Grant(BlueprintAbilityResource resource) {var c=new AddAbilityResources {name="FaithGrant"+resource.name,RestoreAmount=true,RestoreOnLevelUp=true};Set(c,"m_Resource",Ref<BlueprintAbilityResourceReference>(resource));return c;}
        private static LevelEntry Entry(int level,params BlueprintFeatureBase[] features) {var e=new LevelEntry {Level=level};e.SetFeatures(features);return e;}
    }
}
