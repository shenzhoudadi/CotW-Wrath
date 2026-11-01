// Original CotW Archetypes/MindBlade.cs and Features/MagusArcana.cs (MIT).
using System;
using System.Linq;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Mindblade01454
    {
        internal const string ModuleName="Magus archetype: Mindblade",RootToken="MindbladeArchetype01454",PoolToken="MindbladePsychicPool01454",DualToken="MindbladeDualWeapons01454",RapidToken="MindbladeRapidManifest01454",DualManifestToken="MindbladeDualManifest01454";
        internal const string Description="A mindblade blends psychic talent and martial skill to lethal effect. By forming weapons with her mind, she always has the right tool for any situation.";
        internal const string BlendingModule="Mindblade: optional Spell Blending integration";
        internal const string BlendingDescription="When a magus selects this arcana, he must select one spell from the wizard spell list that is of a magus spell level he can cast. He adds this spell to his spellbook and list of magus spells known as a magus spell of its wizard spell level.\nSpecial: A magus can select this magus arcana more than once.\nWhen a mindblade takes the spell blending arcana, she gains spells from the psychic class spell list instead of the wizard spell list.";
        internal static string T(string value)=>"Mindblade"+value+"01454";
        internal static bool Has(UnitEntityData unit,string token){if(unit==null)return false;var id=MagusBatch01454.Identity(token);return unit.Facts.List.Any(f=>f.Blueprint.AssetGuid==id&&f.IsActive&&f.IsTurnedOn);}
        internal static bool WithManifest(UnitEntityData unit,Func<MindbladeManifest01454,bool> action)
        {
            if(unit==null)return false;
            foreach(var buff in unit.Buffs.Enumerable.ToArray()){
                if(!buff.IsActive||!buff.IsTurnedOn||buff.IsSuppressed||buff.IsDisposed||buff.Blueprint.GetComponent<MindbladeManifest01454>()==null)continue;
                foreach(var component in buff.Components.Where(c=>c.SourceBlueprintComponent is MindbladeManifest01454).ToArray())
                    using(component.RequestEventContext())if(action((MindbladeManifest01454)component.SourceBlueprintComponent))return true;
            }
            return false;
        }
        static BlueprintFeature OriginalFeature(PsychicAssets f,string token,string original,params BlueprintComponent[] components)
            =>f.Feature(token,MindbladeTexts01454.Name(original),MindbladeTexts01454.Text(original),components);
        static PrerequisiteArchetypeLevel Need(BlueprintCharacterClass cls,BlueprintArchetype root,int level)
        {var c=new PrerequisiteArchetypeLevel{Level=level};Set(c,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));Set(c,"m_Archetype",Ref<BlueprintArchetypeReference>(root));return c;}
        static PrerequisiteNoArchetype Exclude(BlueprintCharacterClass cls,BlueprintArchetype root)
        {var c=new PrerequisiteNoArchetype{name="CotW01454MindbladePrerequisite"};Set(c,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));Set(c,"m_Archetype",Ref<BlueprintArchetypeReference>(root));return c;}
        internal static BlueprintSpellList AccessList(PsychicAssets f,BlueprintSpellList magus)
        {
            var excluded=new HashSet<BlueprintGuid>(magus.SpellsByLevel.SelectMany(r=>r.Spells).Select(s=>s.AssetGuid)){BlueprintGuid.Parse("d361391f645db984bbf58907711a146a"),BlueprintGuid.Parse("4709274b2080b6444a3c11c6ebbe2404")};
            var spells=Enumerable.Range(0,10).Select(_=>new List<BlueprintAbility>()).ToArray();
            foreach(var row in MindbladeSpellAnchors01454.Rows){
                int level=int.Parse(row[0]);if(level<0||level>6)continue;
                BlueprintAbility spell=null;
                foreach(var candidates in new[]{row[2],row[3],row[4]}){
                    var matches=candidates.Split(',').Where(s=>s.Length==32).Select(s=>f.Cache.Load(BlueprintGuid.Parse(s))).OfType<BlueprintAbility>().Where(a=>a.Type==AbilityType.Spell&&a.Parent==null).GroupBy(a=>a.AssetGuid).Select(g=>g.First()).ToArray();
                    if(matches.Length>1)throw new InvalidOperationException("01454 ambiguous original Psychic Access spell: "+row[1]);
                    if(matches.Length==1){spell=matches[0];break;}
                }
                // Unported spell roots are never synthesized by this archetype.
                if(spell==null||excluded.Contains(spell.AssetGuid)||spells[level].Any(a=>a.AssetGuid==spell.AssetGuid))continue;
                spells[level].Add(spell);
            }
            var traps=f.Native<BlueprintAbility>("4709274b2080b6444a3c11c6ebbe2404");if(!magus.SpellsByLevel.SelectMany(r=>r.Spells).Any(a=>a.AssetGuid==traps.AssetGuid))spells[2].Add(traps);
            for(int i=1;i<=6;i++)if(spells[i].Count==0)throw new InvalidOperationException("01454 Psychic Access has no available spells at level "+i);
            var list=SpellBatch01447.List(f,T("PsychicSpellList"),level=>spells[level].ToArray());
            // The retained post-load reconciler reserves native indices0..10.
            // Publish the canonical shape immediately, before recording it.
            list.SpellsByLevel=SpellListCompatibility.Build(list.SpellsByLevel);
            if(!SpellListCompatibility.Canonical(list.SpellsByLevel))throw new InvalidOperationException("01455 Psychic Access list is not canonical.");
            return list;
        }
        static BlueprintFeatureSelection Learn(PsychicAssets f,BlueprintCharacterClass cls,BlueprintSpellList list,string token,string name,string text)
        {
            var selection=f.Add(new BlueprintFeatureSelection{Ranks=5,IsClassFeature=true},token,name,text);var children=new List<BlueprintFeatureReference>();
            for(int level=1;level<=6;level++){
                var p=f.Clone(f.Native<BlueprintParametrizedFeature>("bcd757ac2aeef3c49b77e5af4e510956"),token+level,name+" (Level "+level+")",text);
                p.SpecificSpellLevel=true;p.SpellLevel=level;p.SpellLevelPenalty=0;p.Ranks=20;p.HideInUI=false;
                Set(p,"m_SpellcasterClass",Ref<BlueprintCharacterClassReference>(cls));Set(p,"m_SpellList",Ref<BlueprintSpellListReference>(list));
                var learn=new LearnSpellParametrized{SpecificSpellLevel=true,SpellLevel=level,SpellLevelPenalty=0};Set(learn,"m_SpellcasterClass",Ref<BlueprintCharacterClassReference>(cls));Set(learn,"m_SpellList",Ref<BlueprintSpellListReference>(list));
                var need=new PrerequisiteClassSpellLevel{RequiredSpellLevel=level};Set(need,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));p.ComponentsArray=new BlueprintComponent[]{learn,need};children.Add(Ref<BlueprintFeatureReference>(p));
            }
            Set(selection,"m_Features",children.ToArray());Set(selection,"m_AllFeatures",children.ToArray());return selection;
        }
        static BlueprintActivatableAbility Toggle(PsychicAssets f,string token,string name,string text,BlueprintBuff buff,int weight,BlueprintUnitFact icon)
        {
            var a=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.Immediately,DeactivateImmediately=true,Group=ActivatableAbilityGroup.ArcaneWeaponProperty,WeightInGroup=weight},token,name,text);
            Set(a,"m_Buff",Ref<BlueprintBuffReference>(buff));Set(a,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free);SpellBatch01447.Icon(a,icon);SpellBatch01447.Icon(buff,icon);return a;
        }
        internal static void Register(BlueprintsCache cache)
        {
            if(MagusBatch01454.Existing(cache,ModuleName))return;
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(MagusBatch01454.MagusId);var pool=f.Native<BlueprintAbilityResource>(MagusBatch01454.PoolId);
            var root=SpellBatch01447.Archetype(f,cls,RootToken,"Mindblade",Description,Array.Empty<LevelEntry>(),Array.Empty<LevelEntry>());root.ChangeCasterType=true;root.IsArcaneCaster=false;
            var book=f.Clone(f.Native<BlueprintSpellbook>("e2763fbfdb91920458c4686c3e7ed085"),T("Spellbook"),"Mindblade",Description);book.IsArcane=false;book.CastingAttribute=StatType.Intelligence;book.ComponentsArray=new BlueprintComponent[]{new PsychicSpellbookMarker{MaximumSpellLevel=6}};Set(root,"m_ReplaceSpellbook",Ref<BlueprintSpellbookReference>(book));
            var casting=OriginalFeature(f,T("PsychicCasting"),"MindBladePsychicSpellCasting",Facts(f.Native<BlueprintAbility>(PsychicClass.CenterAbilityId)));SpellBatch01447.Icon(casting,f.Native<BlueprintAbility>("eabf94e4edc6e714cabd96aa69f8b207"));
            var list=AccessList(f,cls.Spellbook.SpellList);var access=Learn(f,cls,list,T("PsychicAccess"),MindbladeTexts01454.Name("PsychicAccessFeatureSelection"),MindbladeTexts01454.Text("PsychicAccessFeatureSelection"));SpellBatch01447.Icon(access,casting);
            var charge=f.Ability(T("BaneCharge"),"Psychic Weapon - Bane",MindbladeTexts01454.Text("MindBladeWeaponEnchancementBane"),AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Personal,new MindbladeBaneCost01454());charge.Hidden=true;var pay=Cost(pool);pay.CostIsCustom=true;charge.ComponentsArray=charge.ComponentsArray.Concat(new BlueprintComponent[]{pay}).ToArray();
            var props=new Dictionary<string,BlueprintActivatableAbility>();var baneBuff=(BlueprintBuff)null;
            string[][] properties={
                new[]{"BrilliantEnergy","66e9e299c9002ea4bb65b6f300e43770","f1eec5cc68099384cbfc6964049b24fa","4"},
                new[]{"Flaming","30f90becaaac51f41bf56641966c4121","05b7cbe45b1444a4f8bf4570fb2c0208","1"},
                new[]{"Frost","421e54078b7719d40915ce0672511d0b","b338e43a8f81a2f43a73a4ae676353a5","1"},
                new[]{"Shock","7bda5277d36ad114f9f9fd21d0dab658","a3a9e9a2f909cd74e9aee7788a7ec0c6","1"},
                new[]{"Keen","102a9c8c9b7a75e4fb5844e79deaf4c0","27d76f1afda08a64d897cc81201b5218","1"},
                new[]{"Speed","f1c0c50108025d546b2554674ea1c006","85742dd6788c6914f96ddc4628b23932","3"},
                new[]{"GhostTouch","47857e1a5a3ec1a46adf6491b1423b4f","688d42200cbb2334c8e27191c123d18f","1"},
                new[]{"Holy","28a9964d81fedae44bae3ca45710c140","ce0ece459ebed9941bb096f559f36fa8","2"},
                new[]{"Unholy","d05753b8df780fc4bb55b318f06af453","561803a819460f34ea1fe079edabecce","2"},
                new[]{"Axiomatic","0ca43051edefcad4b9b2240aa36dc8d4","d76e8a80ab14ac942b6a9b8aaa5860b1","2"},
                new[]{"Anarchic","57315bc1e1f62a741be0efde688087e9","8ed07b0cc56223c46953348f849f3309","2"},
                new[]{"Bane","1a93ab9c46e48f3488178733be29342a","3a909d1effa3bbc4084f2b5ac95f5306","0"}};
            foreach(var row in properties){
                string key="MindBladeWeaponEnchancement"+row[0];var donor=f.Native<BlueprintActivatableAbility>(row[2]);
                var enchant=f.Native<BlueprintWeaponEnchantment>(row[1]);
                var buff=f.Buff(T(row[0]+"Buff"),MindbladeTexts01454.Name(key),MindbladeTexts01454.Text(key),new MindbladeProperty01454{Enchantment=Ref<BlueprintWeaponEnchantmentReference>(enchant),EnhancementCost=int.Parse(row[3]),Bane=row[0]=="Bane",BaneCharge=Ref<BlueprintAbilityReference>(charge)});
                var toggle=Toggle(f,T(row[0]+"Toggle"),MindbladeTexts01454.Name(key),MindbladeTexts01454.Text(key),buff,int.Parse(row[3]),donor);
                if(row[0]=="Bane"){baneBuff=buff;toggle.Group=ActivatableAbilityGroup.None;toggle.ComponentsArray=new BlueprintComponent[]{new MindbladeBaneAvailable01454{Charge=Ref<BlueprintAbilityReference>(charge)}};}
                if(new[]{"Holy","Unholy","Axiomatic","Anarchic"}.Contains(row[0]))toggle.ComponentsArray=new BlueprintComponent[]{new MindbladeAlignment01454{Component=(AlignmentComponent)Enum.Parse(typeof(AlignmentComponent),row[0]=="Holy"?"Good":row[0]=="Unholy"?"Evil":row[0]=="Axiomatic"?"Lawful":"Chaotic")}};
                props.Add(row[0],toggle);
            }
            var poolFeature=OriginalFeature(f,PoolToken,"PsychicPoolFeature",Grant(pool));SpellBatch01447.Icon(poolFeature,f.Native<BlueprintFeature>("3ce9bb90749c21249adc639031d5eed1"));
            var dual=OriginalFeature(f,DualToken,"DualWeaponsFeature");var dualBuff=f.Buff(T("DualModeBuff"),dual.Name,dual.Description,new MindbladeDualMode01454());var dualToggle=Toggle(f,T("DualModeToggle"),dual.Name,dual.Description,dualBuff,1,f.Native<Kingmaker.Blueprints.Facts.BlueprintUnitFact>("464a7193519429f48b4d190acb753cf0"));
            var dualToggle2=Toggle(f,T("DualMode2Toggle"),dual.Name,dual.Description,dualBuff,2,dualToggle);
            var dualGrants=new[]{f.Feature(T("DualModeGrant"),"","",Facts(dualToggle)),f.Feature(T("DualMode2Grant"),"","",Facts(dualToggle2))};foreach(var grant in dualGrants)grant.HideInUI=grant.HideInCharacterSheetAndLevelUp=true;
            var rapid=OriginalFeature(f,RapidToken,"MindBladeRapidManifestFeature");SpellBatch01447.Icon(rapid,f.Native<Kingmaker.Blueprints.Facts.BlueprintUnitFact>("f9e6281bffd7030499e2ab469e15f1a7"));var dualManifest=OriginalFeature(f,DualManifestToken,"MindBladeDualManifestFeature");SpellBatch01447.Icon(dualManifest,props["Keen"]);
            var enhancements=WeaponEnhancements(f);var tag=f.Clone(f.Native<BlueprintWeaponEnchantment>("66e9e299c9002ea4bb65b6f300e43770"),T("ManifestedEnchantment"),"Manifested","This is a weapon manifested by the power of your will.");tag.ComponentsArray=Array.Empty<BlueprintComponent>();Set(tag,"m_EnchantmentCost",0);
            Set(tag,"m_EnchantName",Text(T("ManifestedEnchantment"),"name","Manifested"));Set(tag,"m_Description",Text(T("ManifestedEnchantment"),"description","This is a weapon manifested by the power of your will."));Set(tag,"m_Prefix",Text(T("ManifestedEnchantment"),"prefix","Manifested"));Set(tag,"m_Suffix",Text(T("ManifestedEnchantment"),"suffix",""));
            var forms=Enumerable.Range(0,6).Select(_=>new List<BlueprintAbility>()).ToArray();
            var defaults=f.Native<BlueprintCategoryDefaults>("567dc59213fd9664c8cb291643439714");
            foreach(var group in defaults.Entries.Where(e=>e.DefaultWeapon!=null).GroupBy(e=>e.Key)){
                var donors=group.Select(e=>e.DefaultWeapon).GroupBy(w=>w.AssetGuid).Select(g=>g.First()).ToArray();if(donors.Length!=1)throw new InvalidOperationException("01454 ambiguous weapon category donor: "+group.Key);var donor=donors[0];
                if(!donor.IsMelee||donor.IsNatural||donor.IsUnarmed)continue;
                var weapon=f.Clone(donor,T("Weapon"+group.Key),donor.Name,donor.Description);Set(weapon,"m_Enchantments",new[]{Ref<BlueprintWeaponEnchantmentReference>(tag)});Set(weapon,"m_Cost",0);
                if(weapon.Double){var second=f.Clone(donor.SecondWeapon,T("Weapon"+group.Key+"Second"),donor.SecondWeapon.Name,donor.SecondWeapon.Description);second.Double=false;Set(second,"m_SecondWeapon",null);Set(second,"m_Enchantments",new[]{Ref<BlueprintWeaponEnchantmentReference>(tag)});Set(second,"m_Cost",0);Set(weapon,"m_SecondWeapon",Ref<BlueprintItemWeaponReference>(second));}
                bool two=weapon.Double||weapon.IsTwoHanded&&weapon.Category!=WeaponCategory.BastardSword&&weapon.Category!=WeaponCategory.DwarvenWaraxe;
                int cost=two?3:weapon.IsLight?1:2;int form=weapon.Double?5:two?2:weapon.IsLight?0:1;
                forms[form].Add(Manifest(f,cls,pool,weapon,enhancements,baneBuff,cost,false,two));
                if(!two)forms[form==0?3:4].Add(Manifest(f,cls,pool,weapon,enhancements,baneBuff,2*cost,true,true));
            }
            var names=new[]{"Manifest Light Weapon","Manifest One-Handed Weapon","Manifest Two-Handed Weapon","Manifest Two Light Weapons","Manifest Two One-Handed Weapons","Manifest Double Weapon"};var wrappers=new List<BlueprintAbility>();
            for(int i=0;i<6;i++){
                if(forms[i].Count==0)throw new InvalidOperationException("01454 missing manufactured melee form "+names[i]);
                var variants=new AbilityVariants();Set(variants,"m_Variants",forms[i].Select(Ref<BlueprintAbilityReference>).ToArray());var wrapper=f.Ability(T("ManifestGroup"+i),names[i],poolFeature.Description,AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Personal,variants,new MindbladeManifestMenu01454{Dual=i>=3});SpellBatch01447.Icon(wrapper,forms[i][0]);
                foreach(var leaf in forms[i])Set(leaf,"m_Parent",Ref<BlueprintAbilityReference>(wrapper));wrappers.Add(wrapper);
            }
            var dismiss=f.Ability(T("Dismiss"),MindbladeTexts01454.Name("ManifestWeaponDeactivateAbility"),MindbladeTexts01454.Text("ManifestWeaponDeactivateAbility"),AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Personal,Run(new MindbladeDismiss01454()));SpellBatch01447.Icon(dismiss,f.Native<BlueprintAbility>("1d6364123e1f6a04c88313d83d3b70ee"));
            poolFeature.ComponentsArray=poolFeature.ComponentsArray.Concat(new[]{Facts(wrappers.Take(3).Cast<Kingmaker.Blueprints.Facts.BlueprintUnitFact>().Concat(new[]{dismiss}).ToArray())}).ToArray();dual.ComponentsArray=new BlueprintComponent[]{Facts(wrappers.Skip(3).ToArray()),GrantAt(cls,dualGrants[0],true),GrantAt(cls,dualGrants[1],false)};SpellBatch01447.Icon(dual,dualToggle);
            var upgrades=Enumerable.Range(1,6).Select(i=>f.Feature(T("Enhancement"+i),poolFeature.Name,poolFeature.Description,new IncreaseActivatableAbilityGroupSize{Group=ActivatableAbilityGroup.ArcaneWeaponProperty})).ToArray();foreach(var u in upgrades)SpellBatch01447.Icon(u,poolFeature);
            upgrades[2].ComponentsArray=upgrades[2].ComponentsArray.Concat(new[]{Facts(props["Speed"])}).ToArray();
            var basic=f.Feature(T("Properties"),poolFeature.Name,poolFeature.Description,Facts(new[]{"Flaming","Frost","Shock","Keen"}.Select(n=>props[n]).ToArray()));basic.HideInUI=basic.HideInCharacterSheetAndLevelUp=true;
            var arcana=f.Native<BlueprintFeatureSelection>("e9dc4dfc73eaaf94aae27e0ed6cc9ada");var extra=new List<BlueprintFeature>();
            foreach(var row in new[]{new[]{"Devoted","4be0bb10e110a35419e406da767bd1e3","12","Holy","Unholy","Axiomatic","Anarchic"},new[]{"Ghost","8896f327c59569c4eaf129bf35b96c1f","9","GhostTouch","BrilliantEnergy"},new[]{"Bane","a2e0691dcfda2374e84d8bbf480e06a0","15","Bane"}}){var native=f.Native<BlueprintFeature>(row[1]);var own=f.Clone(native,T(row[0]+"Arcana"),native.Name,row[0]=="Bane"?props["Bane"].Description:native.Description);own.ComponentsArray=new BlueprintComponent[]{Need(cls,root,int.Parse(row[2])),Facts(row.Skip(3).Select(n=>props[n]).ToArray())};extra.Add(own);}
            var blending=Learn(f,cls,list,T("SpellBlending"),"Spell Blending",BlendingDescription);blending.Ranks=20;blending.ComponentsArray=new BlueprintComponent[]{Need(cls,root,1)};SpellBatch01447.Icon(blending,access);extra.Add(blending);
            string[] removed={"3ce9bb90749c21249adc639031d5eed1","61fc0521e9992624e9c518060bf89c0f","36b609a6946733c42930c55ac540416b","b24897e082896654c8dd64c8fb677363","836879fcd5b29754eb664a090bd6c22f","70be888059f99a245a79d6d61b90edc5","0ef6ec1c2fdfc204fbd3bff9f1609490","1804187264121cd439d70a96234d4ddb","447ca91389e5c9246acb2c640d63f4da","379887a82a7248946bbf6d0158663b5e","3cbe3e308342b3247ba2f4fbaf5e6307","de18c849c41dbfa44801d812376c707d"};int[] levels={1,4,5,7,8,9,11,13,13,14,17,19};
            root.RemoveFeatures=removed.Select((id,i)=>Entry(levels[i],f.Native<BlueprintFeature>(id))).ToArray();root.AddFeatures=new[]{Entry(1,casting,poolFeature),Entry(3,upgrades[0]),Entry(4,access),Entry(5,basic),Entry(6,upgrades[1]),Entry(7,access,dual),Entry(8,rapid),Entry(9,upgrades[2]),Entry(11,access),Entry(12,upgrades[3]),Entry(13,dualManifest),Entry(14,access),Entry(15,upgrades[4]),Entry(18,upgrades[5]),Entry(19,access)};
            var restricted=new[]{"a2e0691dcfda2374e84d8bbf480e06a0","4be0bb10e110a35419e406da767bd1e3","8896f327c59569c4eaf129bf35b96c1f","7a73bf165e8eda6478b4419f857d1ab5","9a399ad2c35d4ff449789fcd7f02943b","f10f1ddf993f6224b8b744ac8c89b82d","2803ed6f49730cf4a9616880e0979269"}.Select(f.Native<BlueprintFeature>).ToArray();
            var undoClass=MagusBatch01454.CaptureClass(cls);var oldAll=Get(arcana,"m_AllFeatures");var oldChoices=Get(arcana,"m_Features");var oldComponents=restricted.Select(b=>b.ComponentsArray).ToArray();var ui=MagusBatch01454.Group(casting,poolFeature,access,dual,rapid,dualManifest);var exclusions=restricted.Select(_=>Exclude(cls,root)).ToArray();
            Action connect=()=>{MagusBatch01454.ConnectArchetype(cls,root,ui);MagusBatch01454.Determinators(cls,casting);foreach(var e in extra){Set(arcana,"m_AllFeatures",MagusBatch01454.Append((BlueprintFeatureReference[])Get(arcana,"m_AllFeatures"),e));Set(arcana,"m_Features",MagusBatch01454.Append((BlueprintFeatureReference[])Get(arcana,"m_Features"),e));}for(int i=0;i<restricted.Length;i++){exclusions[i].OwnerBlueprint=restricted[i];restricted[i].ComponentsArray=restricted[i].ComponentsArray.Concat(new[]{exclusions[i]}).ToArray();}};
            Action undo=()=>{undoClass();Set(arcana,"m_AllFeatures",oldAll);Set(arcana,"m_Features",oldChoices);for(int i=0;i<restricted.Length;i++)restricted[i].ComponentsArray=oldComponents[i];};
            Action verify=()=>{MagusBatch01454.VerifyArchetype(cls,root,ui);MagusBatch01454.VerifyDeterminators(cls,casting);foreach(var e in extra)if(arcana.AllFeatures.Count(a=>ReferenceEquals(a,e))!=1)throw new InvalidOperationException("01454 Mindblade arcana attachment incomplete.");for(int i=0;i<restricted.Length;i++)if(restricted[i].ComponentsArray.Count(c=>ReferenceEquals(c,exclusions[i]))!=1)throw new InvalidOperationException("01454 Mindblade exclusion missing.");};
            MagusBatch01454.Publish(ModuleName,f,connect,undo,verify);
        }
        internal static bool OptionalBlending(BlueprintsCache cache)
        {
            var root=cache.Load(MagusBatch01454.Identity(RootToken)) as BlueprintArchetype;if(root==null)return false;
            var source=cache.Load(BlueprintGuid.Parse("67d5b18ec9344c6da4a6446df6855ad6")) as BlueprintFeatureSelection;if(source==null)return false;
            var cls=(BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse(MagusBatch01454.MagusId));
            var actual=source.AllFeatures.Select(f=>f.AssetGuid.ToString()).ToArray();
            if(actual.Count(s=>s=="c07bac92bcc842948baa0c1aa5d51406")!=1||actual.Count(s=>s=="c26af0444449473d80a02681ab34a387")!=1)throw new InvalidOperationException("01454 TabletopTweaks Spell Blending parent changed.");
            var exclusions=source.GetComponents<PrerequisiteNoArchetype>().Where(p=>p.Archetype==root).ToArray();
            if(exclusions.Length==1){if(exclusions[0].CharacterClass!=cls||exclusions[0].Group!=Prerequisite.GroupType.All)throw new InvalidOperationException("01454 external Spell Blending exclusion changed.");return true;}
            if(exclusions.Length!=0)throw new InvalidOperationException("01454 duplicate external Spell Blending exclusion.");
            var component=Exclude(cls,root);component.OwnerBlueprint=source;source.ComponentsArray=source.ComponentsArray.Concat(new[]{component}).ToArray();return true;
        }
        static AddFeatureOnClassLevel GrantAt(BlueprintCharacterClass cls,BlueprintFeature feature,bool before)
        {var c=new MindbladeModeGrant01455{name=before?"CotW025AddFeatureOnClassLevel":"CotW025AddFeatureOnClassLevel1",Level=12,BeforeThisLevel=before};Set(c,"m_Class",Ref<BlueprintCharacterClassReference>(cls));Set(c,"m_Feature",Ref<BlueprintFeatureReference>(feature));Set(c,"m_AdditionalClasses",Array.Empty<BlueprintCharacterClassReference>());Set(c,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());return c;}
        internal static BlueprintWeaponEnchantmentReference[] WeaponEnhancements(PsychicAssets f)
        {
            // Exact temporary enchantments from original CotW, also used by
            // native enhancement-pool actions. Never stack permanent +N donors.
            return new[]{"d704f90f54f813043a525f304f6c0050","9e9bab3020ec5f64499e007880b37e52","d072b841ba0668846adeb007f623bd6c","6a6a0901d799ceb49b33d4851ff72132","746ee366e50611146821d61e391edf16"}.Select(s=>Ref<BlueprintWeaponEnchantmentReference>(f.Native<BlueprintWeaponEnchantment>(s))).ToArray();
        }
        static BlueprintAbility Manifest(PsychicAssets f,BlueprintCharacterClass cls,BlueprintAbilityResource pool,BlueprintItemWeapon weapon,BlueprintWeaponEnchantmentReference[] enhancements,BlueprintBuff bane,int cost,bool pair,bool two)
        {
            string token=T("Manifest"+weapon.Category+(pair?"BothHands":""));var buff=f.Buff(token+"Buff","", "",new MindbladeManifest01454{Weapon=Ref<BlueprintItemWeaponReference>(weapon),Pool=Ref<BlueprintAbilityResourceReference>(pool),Class=Ref<BlueprintCharacterClassReference>(cls),Enhancements=enhancements,Pair=pair,RequiresTwoHands=two});SetEnum(buff,"m_Flags","RemoveOnRest");
            var apply=Apply(buff,0,toCaster:true,fromSpell:false);apply.Permanent=true;apply.IsNotDispelable=true;var pay=Cost(pool,cost);pay.CostIsCustom=true;
            var a=f.Ability(token,"Manifest "+weapon.Name+(pair?" (Both Hands)":""),MindbladeTexts01454.Text("PsychicPoolFeature"),AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Personal,Run(apply),pay,new MindbladeManifestCost01454{Weapon=Ref<BlueprintItemWeaponReference>(weapon),Bane=Ref<BlueprintBuffReference>(bane),ReservedCost=cost,Pair=pair,Double=weapon.Double,RequiresTwoHands=two});Set(a,"m_Icon",weapon.Icon);return a;
        }
    }
    [TypeId("9306b81fc5794dd08aa94951e73e5454")]
    public sealed class MindbladeDismiss01454:Kingmaker.UnitLogic.Mechanics.Actions.ContextAction
    {public override string GetCaption()=>"Dismiss Manifested Weapon(s)";public override void RunAction()=>Mindblade01454.WithManifest(Context.MaybeCaster,c=>{c.End();return false;});}
}
