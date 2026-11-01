// Original CotW Archetypes/Skirnir.cs (MIT), native Wrath shield/slot APIs.
using System;
using System.Linq;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ElementsSystem;
using Kingmaker.Items;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Skirnir01454
    {
        internal const string ModuleName="Magus archetype: Skirnir",RootToken="SkirnirArchetype01454",BookToken="SkirnirSpellbook01454",ShieldToken="SkirnirArcaneShield101454",CombatToken="SkirnirShieldSpellCombat01454",GreaterCombatToken="SkirnirGreaterShieldCombat01454",StorageToken="SkirnirSpellshield01454",GreaterToken="SkirnirGreaterSpellshield01454";
        internal const ActivatableAbilityGroup ShieldGroup=ActivatableAbilityGroup.ArcaneArmorProperty;
        internal const string Description="Sometimes called a shield-vassal or shieldmaiden, the skirnir has learned to infuse his power into his shield.";
        internal const string CombatDescription="At 8th level, a skirnir gains the spell combat ability, but only when wielding his bonded shield. He retains his shield’s bonus to AC when using spell combat. At 14th level, he gains the benefits of improved spell combat.\nAt 19th level, he gains Greater Spell Combat.";
        internal static string T(string name)=>"Skirnir"+name+"01454";
        internal static bool Has(UnitEntityData unit,string token)=>Mindblade01454.Has(unit,token);
        internal static ItemEntityShield Shield(UnitEntityData unit)=>unit?.Body?.SecondaryHand?.MaybeShield;
        internal static int Capacity(UnitEntityData unit)=>Has(unit,ShieldToken)?unit.Ensure<Kingmaker.UnitLogic.Parts.UnitPartActivatableAbility>().GetGroupSize(ShieldGroup):0;
        // Retain the gate for already serialized penalty components/buffs.
        // The user's01456 rules retain shield AC at every Skirnir level.
        internal static bool LosesShieldAC(UnitEntityData unit)=>false;
        internal static bool WithStorage(UnitEntityData unit,Func<SkirnirStorage01454,bool> action)
        {
            if(unit==null)return false;foreach(var fact in unit.Facts.List.Where(f=>f.IsActive&&f.IsTurnedOn&&!f.IsDisposed&&f.Blueprint.GetComponent<SkirnirStorage01454>()!=null).ToArray())
                foreach(var c in fact.Components.Where(c=>c.SourceBlueprintComponent is SkirnirStorage01454).ToArray())using(c.RequestEventContext())if(action((SkirnirStorage01454)c.SourceBlueprintComponent))return true;return false;
        }
        internal static bool WithEnhancement(UnitEntityData unit,Func<SkirnirShieldEnhancement01454,bool> action)
        {
            if(unit==null)return false;foreach(var buff in unit.Buffs.Enumerable.Where(b=>b.IsTurnedOn&&!b.IsSuppressed&&!b.IsDisposed&&b.Blueprint.GetComponent<SkirnirShieldEnhancement01454>()!=null).ToArray())
                foreach(var c in buff.Components.Where(c=>c.SourceBlueprintComponent is SkirnirShieldEnhancement01454).ToArray())using(c.RequestEventContext())if(action((SkirnirShieldEnhancement01454)c.SourceBlueprintComponent))return true;return false;
        }
        static BlueprintFeature Original(PsychicAssets f,string token,string key,params BlueprintComponent[] components)=>f.Feature(token,SkirnirTexts01454.Name(key),SkirnirTexts01454.Text(key),components);
        static BlueprintActivatableAbility Toggle(PsychicAssets f,string token,string name,string text,BlueprintBuff buff,BlueprintUnitFact icon,params BlueprintComponent[] components)
        {var a=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.Immediately,DeactivateImmediately=true,Group=ActivatableAbilityGroup.None,ComponentsArray=components},token,name,text);Set(a,"m_Buff",Ref<BlueprintBuffReference>(buff));Set(a,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free);SpellBatch01447.Icon(a,icon);SpellBatch01447.Icon(buff,icon);return a;}
        internal static void Register(BlueprintsCache cache)
        {
            if(MagusBatch01454.Existing(cache,ModuleName))return;var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(MagusBatch01454.MagusId);var pool=f.Native<BlueprintAbilityResource>(MagusBatch01454.PoolId);
            var root=SpellBatch01447.Archetype(f,cls,RootToken,"Skirnir",Description,Array.Empty<LevelEntry>(),Array.Empty<LevelEntry>());
            // Original CotW uses Sword Saint as its prepared book donor. The
            // latest game's table has two extra low-circle reductions at5/6;
            // author Skirnir's stated one-fewer rule from the base Magus table
            // in a private copy, rather than inheriting those differences.
            var donor=f.Native<BlueprintSpellbook>("682545e11e5306c45b14ca78bcbe3e62");var normal=cls.Spellbook.SpellsPerDay;var diminished=donor.SpellsPerDay;
            var table=f.Clone(normal,T("SpellsPerDay"),"","");table.Levels=CopyData(normal.Levels);
            foreach(var row in table.Levels)row.Count=row.Count.Select((count,level)=>level==0?count:Math.Max(0,count-1)).ToArray();
            var book=f.Clone(donor,BookToken,"Skirnir",Description);Set(book,"m_SpellsPerDay",Ref<BlueprintSpellsTableReference>(table));Set(root,"m_ReplaceSpellbook",Ref<BlueprintSpellbookReference>(book));
            var diminishedFeature=Original(f,T("Diminished"),"DiminishedSpellcastingSkirnirFeature");SpellBatch01447.Icon(diminishedFeature,f.Native<BlueprintFeature>("61fc0521e9992624e9c518060bf89c0f"));
            var sorcerous=Original(f,T("SorcerousShield"),"SorcerousShieldFeature",Facts(f.Native<BlueprintFeature>("cb8686e7357a68c42bdd9d4e65334633"),f.Native<BlueprintFeature>("6105f450bb2acbd458d277e71e19d835")),new SkirnirSorcerousShield01454());SpellBatch01447.Icon(sorcerous,f.Native<BlueprintFeature>("cb8686e7357a68c42bdd9d4e65334633"));
            var bond=f.Clone(f.Native<BlueprintAbility>("e5dcf71e02e08fc448d9745653845df1"),T("BondedItemAbility"),"Bonded Item","At 1st level, a skirnir gains a shield (except for a tower shield) as an arcane bond item. This is identical to the wizard class ability, but the skirnir may only bond with a shield, not a familiar or other item.");
            var restores=bond.GetComponents<AbilityRestoreSpellSlot>().ToArray();if(restores.Length!=1)throw new InvalidOperationException("01454 native bonded item restoration changed.");restores[0].CheckSpellBook=true;restores[0].SpellbooksReference=new[]{Ref<BlueprintSpellbookReference>(book)};bond.Hidden=false;bond.ComponentsArray=bond.ComponentsArray.Concat(new[]{new SkirnirShieldRequired01454{NoTower=true}}).ToArray();
            var bonded=f.Feature(T("BondedItem"),bond.Name,bond.Description,Facts(bond),Grant(bond.GetComponent<AbilityResourceLogic>().RequiredResource));SpellBatch01447.Icon(bonded,bond);
            var strikeToggle=f.Clone(f.Native<BlueprintActivatableAbility>("e958891ef90f7e142a941c06c811181e"),T("SpellstrikeToggle"),"Shielded Spellstrike","At 1st level, a skirnir may use this ability with a weapon or shield bash attack.");
            var strike=f.Clone(f.Native<BlueprintFeature>("be50f4e97fff8a24ba92561f1694a945"),T("ShieldedSpellstrike"),strikeToggle.Name,strikeToggle.Description);strike.ComponentsArray=strike.ComponentsArray.Where(c=>!(c is AddFacts)).Concat(new[]{Facts(strikeToggle)}).ToArray();
            var combatToggle=f.Clone(f.Native<BlueprintActivatableAbility>("8898a573e8a8a184b8186dbc3a26da74"),T("SpellCombatToggle"),"Shield Spell Combat",CombatDescription);combatToggle.ComponentsArray=combatToggle.ComponentsArray.Concat(new[]{new SkirnirToggleRequired01454()}).ToArray();
            var acPenalty=f.Buff(T("CombatPenalty"),"Shielded Spell Combat Penalty",CombatDescription,new SkirnirShieldACPenalty01454());SpellBatch01447.Icon(acPenalty,combatToggle);
            var combat=f.Clone(f.Native<BlueprintFeature>("2464ba53317c7fc4d88f383fac2b45f9"),CombatToken,"Shield Spell Combat",CombatDescription);combat.ComponentsArray=combat.ComponentsArray.Where(c=>!(c is AddFacts)).Concat(new BlueprintComponent[]{Facts(combatToggle),new SkirnirCombatPenalty01454{Penalty=Ref<BlueprintBuffReference>(acPenalty)}}).ToArray();
            var nativeGreaterCombat=f.Native<BlueprintFeature>("379887a82a7248946bbf6d0158663b5e");
            var grantGreaterCombat=new AddFeatureIfHasFact{Not=true};Set(grantGreaterCombat,"m_CheckedFact",Ref<BlueprintUnitFactReference>(nativeGreaterCombat));Set(grantGreaterCombat,"m_Feature",Ref<BlueprintUnitFactReference>(nativeGreaterCombat));
            // Keep the old hidden node for existing saves; fresh progression
            // grants the native feature directly, without a duplicate UI row.
            var greaterCombat=f.Feature(GreaterCombatToken,"","",grantGreaterCombat);greaterCombat.HideInUI=greaterCombat.HideInCharacterSheetAndLevelUp=true;
            var shieldEffects=CreateShieldEnchantments(f);var shieldTiers=CreateShieldPool(f,cls,pool,shieldEffects);
            var release=f.Buff(T("ReleaseBuff"),"Spellshield: Release",SkirnirTexts01454.Text("SpellShieldFeature"));var greaterRelease=f.Buff(T("GreaterReleaseBuff"),"Greater Spellshield: Release",SkirnirTexts01454.Text("GreaterSpellshieldFeature"));
            var storage=Original(f,StorageToken,"SpellShieldFeature",new SkirnirStorage01454{Release=Ref<BlueprintBuffReference>(release),Greater=Ref<BlueprintBuffReference>(greaterRelease)});SpellBatch01447.Icon(storage,f.Native<BlueprintAbility>("ef768022b0785eb43a18969903c537c4"));
            var releaseToggle=Toggle(f,T("ReleaseToggle"),release.Name,release.Description,release,storage,new SkirnirToggleRequired01454{Stored=true});
            var stores=new List<BlueprintAbility>();for(int i=0;i<1;i++){
                var cost=Cost(pool);cost.CostIsCustom=true;
                var ability=f.Ability(T("Store"+i),storage.Name,storage.Description,AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Personal,new SkirnirStoreSpell01454(),cost);ability.ActionBarAutoFillIgnored=true;SpellBatch01447.Icon(ability,storage);stores.Add(ability);
            }
            storage.ComponentsArray=storage.ComponentsArray.Concat(new[]{Facts(stores.Cast<BlueprintUnitFact>().Concat(new[]{releaseToggle}).ToArray())}).ToArray();
            var greaterToggle=Toggle(f,T("GreaterReleaseToggle"),greaterRelease.Name,greaterRelease.Description,greaterRelease,f.Native<BlueprintAbility>("183d5bb91dea3a1489a6db6c9cb64445"),new SkirnirToggleRequired01454{Stored=true,LightArmor=true});
            var greater=Original(f,GreaterToken,"GreaterSpellshieldFeature",Facts(greaterToggle));SpellBatch01447.Icon(greater,greaterToggle);
            string[] removed={"2464ba53317c7fc4d88f383fac2b45f9","be50f4e97fff8a24ba92561f1694a945","61fc0521e9992624e9c518060bf89c0f","836879fcd5b29754eb664a090bd6c22f","0ef6ec1c2fdfc204fbd3bff9f1609490","379887a82a7248946bbf6d0158663b5e","cd96b7275c206da4899c69ae127ffda6","de18c849c41dbfa44801d812376c707d"};int[] levels={1,2,4,8,11,14,16,19};root.RemoveFeatures=removed.Select((id,i)=>Entry(levels[i],f.Native<BlueprintFeature>(id))).ToArray();
            root.AddFeatures=new[]{Entry(1,strike,shieldTiers[0],sorcerous,bonded,diminishedFeature),Entry(5,shieldTiers[1]),Entry(7,storage),Entry(8,combat),Entry(9,shieldTiers[2]),Entry(13,shieldTiers[3]),Entry(14,f.Native<BlueprintFeature>("836879fcd5b29754eb664a090bd6c22f")),Entry(16,greater),Entry(17,shieldTiers[4]),Entry(19,nativeGreaterCombat)};root.ReplaceStartingEquipment=true;Set(root,"m_StartingItems",cls.StartingItems.Select(Ref<BlueprintItemReference>).Concat(new[]{Ref<BlueprintItemReference>(f.Native<BlueprintItemShield>("f4cef3ba1a15b0f4fa7fd66b602ff32b"))}).ToArray());
            var undoClass=MagusBatch01454.CaptureClass(cls);var ui=MagusBatch01454.Group(strike,combat,greaterCombat,storage,greater,bonded,diminishedFeature,sorcerous,shieldTiers[0],shieldTiers[1],shieldTiers[2],shieldTiers[3],shieldTiers[4]);
            // Native prestige selection leaves bind an exact book. Preserve the
            // existing Magus leaves, exclude Skirnir from those leaves only,
            // and offer its private prepared book under the same native parents.
            var prestigeRows=new[]{new[]{"EldritchKnight","dc3ab8d0484467a4787979d93114ebc3","9a399ad2c35d4ff449789fcd7f02943b"},new[]{"ArcaneTrickster","ae04b7cdeb88b024b9fd3882cc7d3d76","f10f1ddf993f6224b8b744ac8c89b82d"},new[]{"MysticTheurge","97f510c6483523c49bc779e93e4c4568","2803ed6f49730cf4a9616880e0979269"}};
            var parents=new List<BlueprintFeatureSelection>();var sources=new List<BlueprintFeatureReplaceSpellbook>();var leaves=new List<BlueprintFeatureReplaceSpellbook>();var exclusions=new List<PrerequisiteNoArchetype>();
            foreach(var row in prestigeRows){
                var parent=f.Native<BlueprintFeatureSelection>(row[1]);var source=f.Native<BlueprintFeatureReplaceSpellbook>(row[2]);
                var own=f.Clone(source,T(row[0]+"Spellbook"),"Skirnir",Description);Set(own,"m_Spellbook",Ref<BlueprintSpellbookReference>(book));
                var need=new PrerequisiteArchetypeLevel{Level=1};Set(need,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));Set(need,"m_Archetype",Ref<BlueprintArchetypeReference>(root));own.ComponentsArray=own.ComponentsArray.Concat(new[]{need}).ToArray();
                var exclude=new PrerequisiteNoArchetype{name="CotW01454SkirnirPrestigePrerequisite",OwnerBlueprint=source};Set(exclude,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));Set(exclude,"m_Archetype",Ref<BlueprintArchetypeReference>(root));parents.Add(parent);sources.Add(source);leaves.Add(own);exclusions.Add(exclude);
            }
            var oldReferences=parents.Select(p=>new[]{Get(p,"m_Features"),Get(p,"m_AllFeatures")}).ToArray();var oldSourceComponents=sources.Select(s=>s.ComponentsArray).ToArray();
            Action connect=()=>{
                MagusBatch01454.ConnectArchetype(cls,root,ui);MagusBatch01454.Determinators(cls,bonded,diminishedFeature,sorcerous);
                for(int i=0;i<parents.Count;i++){
                    Set(parents[i],"m_Features",MagusBatch01454.Append((BlueprintFeatureReference[])Get(parents[i],"m_Features"),leaves[i]));Set(parents[i],"m_AllFeatures",MagusBatch01454.Append((BlueprintFeatureReference[])Get(parents[i],"m_AllFeatures"),leaves[i]));sources[i].ComponentsArray=sources[i].ComponentsArray.Concat(new[]{exclusions[i]}).ToArray();
                }
            };
            Action undo=()=>{undoClass();for(int i=0;i<parents.Count;i++){Set(parents[i],"m_Features",oldReferences[i][0]);Set(parents[i],"m_AllFeatures",oldReferences[i][1]);sources[i].ComponentsArray=oldSourceComponents[i];}};
            Action verify=()=>{MagusBatch01454.VerifyArchetype(cls,root,ui);MagusBatch01454.VerifyDeterminators(cls,bonded,diminishedFeature,sorcerous);for(int i=0;i<parents.Count;i++)if(parents[i].AllFeatures.Count(v=>ReferenceEquals(v,leaves[i]))!=1||sources[i].ComponentsArray.Count(v=>ReferenceEquals(v,exclusions[i]))!=1||leaves[i].Spellbook!=book)throw new InvalidOperationException("01454 Skirnir prestige book link changed.");};
            MagusBatch01454.Publish(ModuleName,f,connect,undo,verify);
        }
        static BlueprintArmorEnchantment Enchantment(PsychicAssets f,string token,string name,string description,BlueprintComponent component,int cost)
        {
            var e=f.Clone(f.Native<BlueprintArmorEnchantment>("a9ea95c5e02f9b7468447bc1010fe152"),token,name,description);e.ComponentsArray=new[]{component};Set(e,"m_EnchantmentCost",cost);Set(e,"m_EnchantName",Text(token,"name",name));Set(e,"m_Description",Text(token,"description",description));Set(e,"m_Prefix",Text(token,"prefix",""));Set(e,"m_Suffix",Text(token,"suffix",""));return e;
        }
        static BlueprintArmorEnchantment[] CreateShieldEnchantments(PsychicAssets f)
        {
            var results=new List<BlueprintArmorEnchantment>();
            for(int i=0;i<3;i++){
                int value=new[]{25,50,75}[i],cost=new[]{1,3,5}[i];string name=new[]{"Light","Moderate","Heavy"}[i]+" Fortification";string text="This suit of armor or shield produces a magical force that protects vital areas of the wearer more effectively. When a critical hit or sneak attack is scored on the wearer, there is a "+value+"% chance that the critical hit or sneak attack is negated and damage is instead rolled normally.";
                var feature=f.Feature(T("Fortification"+value+"Feature"),"","",new AddFortification{Bonus=value});feature.HideInUI=feature.HideInCharacterSheetAndLevelUp=true;var add=new AddUnitFeatureEquipment();Set(add,"m_Feature",Ref<BlueprintFeatureReference>(feature));results.Add(Enchantment(f,T("Fortification"+value),name,text,add,cost));
            }
            foreach(int value in new[]{13,17,21,25}){
                int cost=value==13?2:value==17?3:value==21?4:5;string name="Spell Resistance "+value,text="This special ability grants the armor or shield’s wearer spell resistance "+value+" while the armor or shield is worn.";
                var feature=f.Feature(T("SpellResistance"+value+"Feature"),"","",new AddSpellResistance{Value=value});feature.HideInUI=feature.HideInCharacterSheetAndLevelUp=true;var add=new AddUnitFeatureEquipment();Set(add,"m_Feature",Ref<BlueprintFeatureReference>(feature));results.Add(Enchantment(f,T("SpellResistance"+value),name,text,add,cost));
            }
            return results.ToArray();
        }
        static BlueprintFeature[] CreateShieldPool(PsychicAssets f,BlueprintCharacterClass cls,BlueprintAbilityResource pool,BlueprintArmorEnchantment[] properties)
        {
            string text=SkirnirTexts01454.Text("SkirnirArcaneShieldEnhancementSwitchBuff");var icon=f.Native<BlueprintUnitFact>("68666566c506d344bad1e30bc3194fed");
            var enhancements=new[]{"a9ea95c5e02f9b7468447bc1010fe152","758b77a97640fd747abf149f5bf538d0","9448d3026111d6d49b31fc85e7f3745a","eaeb89df5be2b784c96181552414ae5a","6628f9d77fd07b54c911cd8930c0d531"}.Select((id,i)=>f.Clone(f.Native<BlueprintArmorEnchantment>(id),T("TemporaryArmor"+(i+1)),"Temporary Enhancement + "+(i+1),"")).ToArray();
            for(int i=0;i<enhancements.Length;i++)Set(enhancements[i],"m_EnchantName",Text(T("TemporaryArmor"+(i+1)),"name","Temporary Enhancement + "+(i+1)));
            var host=f.Buff(T("ShieldEnhancementBuff"),"Arcane Shield Enchantment",text,new SkirnirShieldEnhancement01454{Enhancements=enhancements.Select(Ref<BlueprintArmorEnchantmentReference>).ToArray()});SpellBatch01447.Icon(host,icon);
            var apply=Apply(host,1,DurationRate.Minutes,true,false);apply.IsNotDispelable=true;var lasting=Apply(host,0,DurationRate.Minutes,true,false);lasting.IsNotDispelable=true;lasting.DurationValue.BonusValue=new ContextValue{ValueType=ContextValueType.Rank};
            var hasEnduring=new Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionCasterHasFact();Set(hasEnduring,"m_Fact",Ref<BlueprintUnitFactReference>(f.Native<BlueprintBuff>("3c2fe8e0374d28d4185355121f4c4544")));
            var conditional=new Conditional{ConditionsChecker=new ConditionsChecker{Conditions=new Kingmaker.ElementsSystem.Condition[]{hasEnduring}},IfTrue=Actions(lasting),IfFalse=Actions(apply)};
            var cost=Cost(pool);cost.CostIsCustom=true;var ability=f.Ability(T("ShieldEnhancementAbility"),host.Name,text,AbilityType.Supernatural,UnitCommand.CommandType.Swift,AbilityRange.Personal,Run(conditional),cost,new SkirnirShieldCost01454(),Rank(cls,"AsIs"),new SkirnirShieldRequired01454());SpellBatch01447.Icon(ability,icon);
            var fx=f.Native<BlueprintAbility>("ef768022b0785eb43a18969903c537c4").GetComponent<AbilitySpawnFx>()??throw new InvalidOperationException("01454 original shield casting FX missing.");ability.ComponentsArray=ability.ComponentsArray.Concat(new[]{CopyData(fx)}).ToArray();
            var byCost=Enumerable.Range(0,6).Select(_=>new List<BlueprintActivatableAbility>()).ToArray();
            foreach(var e in properties){string name="Arcane Shield Enchantment - "+e.Name,description=e.Description+"\nThis consumes "+e.EnchantmentCost+" point(s) of shield enhancement bonus.";var buff=f.Buff(T(e.name+"ChoiceBuff"),name,description,new SkirnirShieldProperty01454{Enchantment=Ref<BlueprintArmorEnchantmentReference>(e)});var toggle=Toggle(f,T(e.name+"ChoiceToggle"),name,description,buff,e.name.Contains("Fortification")?f.Native<BlueprintAbility>("c66e86905f7606c4eaa5c774f0357b2b"):f.Native<BlueprintAbility>("0a5ddfbcfb3989543ac7c936fc256889"));toggle.Group=ShieldGroup;toggle.WeightInGroup=e.EnchantmentCost;byCost[e.EnchantmentCost].Add(toggle);}
            var tiers=new BlueprintFeature[5];for(int i=0;i<5;i++){
                var choices=new List<BlueprintComponent>();if(i>0)choices.Add(new IncreaseActivatableAbilityGroupSize{Group=ShieldGroup});if(i==0)choices.Add(Facts(ability));else {var options=byCost[i+1].Concat(i==1?byCost[1]:Enumerable.Empty<BlueprintActivatableAbility>()).ToArray();if(options.Length>0)choices.Add(Facts(options));}
                tiers[i]=f.Feature(i==0?ShieldToken:T("ArcaneShield"+(i+1)),i==0?"Arcane Shield":"Arcane Shield (+"+(i+1)+")",text,choices.ToArray());SpellBatch01447.Icon(tiers[i],ability);
            }
            return tiers;
        }
    }
}
