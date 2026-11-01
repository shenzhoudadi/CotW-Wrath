// Adapted from Holic75/KingmakerRebalance Hunter.createFeralHunter (MIT).
using System;
using System.Linq;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Commands.Base;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class FeralHunter01461
    {
        internal const string ModuleName="Hunter archetype: Feral Hunter",ClassId="34ecd1b5e1b90b9498795791b0855239",RootToken="FeralHunterArchetype01461",WildToken="FeralHunterWildShapeWolfFeature01461";
        internal static readonly string[] Summons={"c6147854641924442a3bb736080cfeb6","298148133cdc3fd42889b99c82711986","fdcf7e57ec44f704591f11b45f4acf61","c83db50513abdf74ca103651931fac4b","8f98a22f35ca6684a983363d32e51bfe","55bbce9b3e76d4a4a8c8e0698d29002c","051b979e7d7f8ec41b9fa35d04746b33","ea78c04f0bd13d049a1cce5daf8d83e0","a7469ef84ba50ac4cbf3d145e3173f8e"};
        static readonly HashSet<BlueprintGuid> BaseAspects=new HashSet<BlueprintGuid>(new[]{"a5dba776428e8c441a8528edc444f3a8","3a3cc64f9d92b9148bc1f1f3fac19256","32d8e51cf253ff3498a4fc999d6936fa","bc5b2b6acc8842f498a0b77329de8b26","b6fae1b94fe19fb40a96134a918bfdb4","e2ffd47fe9a846748bf639b9f47d169a","e3324e6489ad69840badb2838525ca59","422a086ad2c2fe0439958b7178131f7b"}.Select(BlueprintGuid.Parse));
        const string FocusText="At 1st level, a feral hunter gains a limited ability to change her shape into hybrid animal forms. This functions as the animal focus class feature, except that the hunter always applies the animal aspect to herself, and there is no limit to this ability’s duration.";
        const string SummonText="Starting at 1st level, a feral hunter can cast summon nature's ally I as a spell-like ability a number of times per day equal to 3 + her Wisdom modifier. She can cast this spell as a standard action, and the creatures remain for 1 minute per level (instead of 1 round per level). At 3rd level and every 2 hunter levels thereafter, the power of this ability increases by 1 spell level, allowing her to summon more powerful creatures (to a maximum of summon nature's ally IX at 17th level). A feral hunter cannot have more than one summon nature's ally spell active in this way at a time; if she uses another, any existing summon nature's ally immediately ends.";
        const string WildText="At 4th level, a feral hunter gains the ability to change shape. This ability functions like the druid wild shape ability, except the hunter can take only animal forms (not elemental or plant forms). The hunter’s effective druid level is equal to her class level. A feral hunter can use wild shape once per day at 4th level and one additional time per day every 2 levels thereafter.";
        const string ArchetypeText="A feral hunter has forged a bond with nature that’s so strong that she doesn’t merely channel the aspects of animals—she actually becomes an animal herself. Though she lacks an animal companion, a feral hunter is in tune with the beast lurking within her flesh and spirit, and lives in a near-wild state of being. A feral hunter often resembles a lycanthrope, but her power comes from her own nature and is not influenced by moonlight or silver.";
        static void Icon(BlueprintUnitFact target,BlueprintUnitFact donor)=>PsychicRemainingDisciplines01458.Icon(target,donor);
        internal static void Register(BlueprintsCache cache)
        {
            if(MagusBatch01454.Existing(cache,ModuleName))return;var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(ClassId);
            BlueprintFeature Native(string id)=>f.Native<BlueprintFeature>(id);
            var nativeFocus=Native("443365823b7d6d14b8d12f4e7bce1077");var focus=f.Clone(nativeFocus,"FeralHunterAnimalFocusFeature01461","Feral Hunter Animal Focus",FocusText);
            var facts=focus.GetComponent<AddFacts>()??throw new InvalidOperationException("Feral Hunter requires native animal focus facts.");
            // Only adapt the eight actual base-game aspects. Additional provider
            // choices retain their original parent and mechanics; an appended
            // foreign selector must neither break this module nor grant its
            // companion-only behavior to a hunter without a companion.
            focus.ComponentsArray=new BlueprintComponent[]{facts};
            var free=new List<BlueprintUnitFact>();
            foreach(var source in nativeFocus.GetComponents<AddFacts>().SelectMany(c=>c.Facts).OfType<BlueprintActivatableAbility>().Where(a=>BaseAspects.Contains(a.AssetGuid)).GroupBy(a=>a.AssetGuid).Select(g=>g.First())){
                var buff=f.Clone(source.Buff,"FeralHunterFocus"+source.AssetGuid+"Buff01461",source.Name,FocusText);
                var actions=buff.GetComponent<AddFactContextActions>()??throw new InvalidOperationException("Feral aspect requires native focus actions.");
                var conditional=actions.Activated.Actions.SingleOrDefault() as Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional;
                if(conditional==null)throw new InvalidOperationException("Feral aspect requires exactly one native self/pet branch.");
                // Retain the native self effect, rank and child-buff lifetime.
                // A Feral aspect has no companion branch or timed self host.
                actions.Activated=conditional.IfTrue;actions.Deactivated=Actions();
                var mode=f.Clone(source,"FeralHunterFocus"+source.AssetGuid+"Toggle01461",source.Name,FocusText);Set(mode,"m_Buff",Ref<BlueprintBuffReference>(buff));mode.HiddenInUI=false;mode.DeactivateImmediately=false;free.Add(mode);
            }
            if(free.Count!=8)throw new InvalidOperationException("Feral Hunter requires exactly eight native aspects.");Set(facts,"m_Facts",free.Select(Ref<BlueprintUnitFactReference>).ToArray());
            var sourceGroup=Native("cbc90fcdb60809744989d386a98cd21c");var third=f.Clone(sourceGroup,"FeralHunterThirdAnimalFocus01461","Third Animal Focus","At 20th level, the feral hunter can apply three animal aspects to herself at the same time. Her animal focus remains unlimited.");
            var summoning=BuildSummons(f,cls,out var origins);var forms=BuildShapes(f,cls);
            var precise=f.Clone(f.Native<BlueprintFeatureSelection>("75e9a5179aeff4140a8235ebdeaf8df7"),"FeralHunterPreciseNatureAlly01461","Precise Nature's Ally","At 2nd level, a hunter chooses either Precise Shot or Outflank as a bonus feat. She does not need to meet the prerequisites for this feat. In addition the feral hunter grants all her teamwork feats to all creatures she summons with summon nature’s ally.");precise.IgnorePrerequisites=true;
            var shared=new AddFactsFromCaster{FeatureFromSelection=true};Set(shared,"m_Selection",Ref<BlueprintFeatureSelectionReference>(f.Native<BlueprintFeatureSelection>("d87e2f6a9278ac04caeb0f93eff95fcb")));
            var share=f.Buff("FeralHunterSummonTacticsBuff01461","Precise Nature's Ally",precise.Description,shared);Icon(share,precise);
            precise.ComponentsArray=precise.ComponentsArray.Concat(new[]{new FeralSummonTactics01461{Origins=origins,Effect=Ref<BlueprintBuffReference>(share)}}).ToArray();
            var add=new List<LevelEntry>{Entry(1,focus,summoning[0]),Entry(2,precise),Entry(4,forms[0],forms[1]),Entry(6,forms[2],forms[3]),Entry(8,forms[4],forms[5]),Entry(20,third)};
            for(int i=1;i<9;i++)add.Add(Entry(1+2*i,summoning[i]));for(int level=6;level<=20;level+=2)add.Add(Entry(level,Native("f78260b9a089ccc44b55f0fed08b1752")));
            var remove=new List<LevelEntry>{Entry(1,Native("715ac15eb8bd5e342bc8a0a3c9e3e38f"),nativeFocus),Entry(2,Native("75e9a5179aeff4140a8235ebdeaf8df7")),Entry(3,Native("1b9916f7675d6ef4fb427081250d49de")),Entry(10,Native("27ad1316abbbbb34b8ffb9a87f38c10e")),Entry(20,Native("d8a126a3ed3b62943a597c937a4bf840"))};
            foreach(int level in new[]{6,9,12,15,18})remove.Add(Entry(level,Native("01046afc774beee48abde8e35da0f4ba")));
            LevelEntry[] Rows(IEnumerable<LevelEntry> entries)=>entries.GroupBy(e=>e.Level).OrderBy(g=>g.Key).Select(g=>Entry(g.Key,g.SelectMany(e=>e.Features).ToArray())).ToArray();
            var root=f.Add(new BlueprintArchetype{LocalizedName=Text(RootToken,"name","Feral Hunter"),LocalizedDescription=Text(RootToken,"description",ArchetypeText),ComponentsArray=Array.Empty<BlueprintComponent>(),AddFeatures=Rows(add),RemoveFeatures=Rows(remove)},RootToken,"Feral Hunter",ArchetypeText);
            Set(root,"m_ParentClass",cls);var group=MagusBatch01454.Group(new BlueprintFeatureBase[]{focus,precise}.Concat(forms).Concat(summoning).Concat(new[]{third}).ToArray());var undo=MagusBatch01454.CaptureClass(cls);
            foreach(var feature in f.Assets.Values.OfType<BlueprintFeature>())feature.IsClassFeature=true;
            void Connect(){MagusBatch01454.ConnectArchetype(cls,root,group);MagusBatch01454.Determinators(cls,focus,summoning[0]);}
            void Verify(){MagusBatch01454.VerifyArchetype(cls,root,group);MagusBatch01454.VerifyDeterminators(cls,focus,summoning[0]);foreach(var entry in root.AddFeatures.Concat(root.RemoveFeatures)){var resolved=entry.Features;}}
            MagusBatch01454.Publish(ModuleName,f,Connect,undo,Verify);
        }
        static BlueprintFeature[] BuildSummons(PsychicAssets f,BlueprintCharacterClass cls,out BlueprintAbilityReference[] origins)
        {
            var resource=PsychicRemainingDisciplines01458.Daily(f,"FeralHunterSummonResource01461",3,StatType.Wisdom);var pool=f.Clone(f.Native<Kingmaker.Blueprints.BlueprintSummonPool>("490248a826bbf904e852f5e3afa6d138"),"FeralHunterSummonPool01461","","");
            var getter=new Kingmaker.UnitLogic.Mechanics.Properties.ClassLevelGetter{Settings=new Kingmaker.UnitLogic.Mechanics.Properties.PropertySettings()};Set(getter,"m_Class",Ref<BlueprintCharacterClassReference>(cls));Set(getter,"m_Archetype",new BlueprintArchetypeReference());
            var casterProperty=f.Add(new Kingmaker.UnitLogic.Mechanics.Properties.BlueprintUnitProperty{ComponentsArray=new BlueprintComponent[]{getter}},"FeralHunterCasterLevelProperty01461","","");
            ContextValue CasterLevel(){var value=new ContextValue{ValueType=ContextValueType.CasterCustomProperty};Set(value,"m_CustomProperty",Ref<BlueprintUnitPropertyReference>(casterProperty));return value;}
            var allowed=new Dictionary<BlueprintGuid,BlueprintAbility>();var result=new List<BlueprintFeature>();
            for(int i=0;i<9;i++){
                var spell=f.Native<BlueprintAbility>(Summons[i]);var cloned=new Dictionary<BlueprintGuid,BlueprintAbility>();
                BlueprintAbility Clone(BlueprintAbility source){
                    allowed[source.AssetGuid]=source;if(cloned.TryGetValue(source.AssetGuid,out var old))return old;
                    var a=f.Clone(source,"FeralHunterSummon"+(i+1)+source.AssetGuid+"Ability01461",source.Name,SummonText);cloned.Add(source.AssetGuid,a);allowed[a.AssetGuid]=a;a.Type=AbilityType.SpellLike;a.ActionType=UnitCommand.CommandType.Standard;Set(a,"m_IsFullRoundAction",false);a.AvailableMetamagic=0;Set(a,"m_Parent",new BlueprintAbilityReference());
                    a.ComponentsArray=a.ComponentsArray.Where(c=>!(c is SpellListComponent)&&!(c is AbilityResourceLogic)&&!(c is ContextAbilityParamsCalculator)).Concat(new BlueprintComponent[]{Cost(resource),new ContextCalculateAbilityParams{StatType=StatType.Wisdom,ReplaceCasterLevel=true,CasterLevel=CasterLevel(),ReplaceSpellLevel=true,SpellLevel=i+1}}).ToArray();
                    if(!a.GetComponents<ContextRankConfig>().Any(r=>(AbilityRankType)Get(r,"m_Type")==AbilityRankType.Default))a.ComponentsArray=a.ComponentsArray.Concat(new[]{Rank(cls,"AsIs")}).ToArray();
                    foreach(var rank in a.GetComponents<ContextRankConfig>().Where(r=>(ContextRankBaseValueType)Get(r,"m_BaseValueType")!=ContextRankBaseValueType.FeatureList)){SetEnum(rank,"m_BaseValueType","ClassLevel");Set(rank,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(cls)});Set(rank,"Archetype",new BlueprintArchetypeReference());}
                    var variants=a.GetComponent<AbilityVariants>();if(variants!=null){var children=source.GetComponent<AbilityVariants>().Variants.Select(Clone).ToArray();Set(variants,"m_Variants",children.Select(Ref<BlueprintAbilityReference>).ToArray());foreach(var child in children)Set(child,"m_Parent",Ref<BlueprintAbilityReference>(a));}
                    else{
                        var graph=PsychicRemainingDisciplines01458.Graph(a.ComponentsArray).ToArray();var spawns=graph.OfType<ContextActionSpawnMonster>().ToArray();if(spawns.Length==0)throw new InvalidOperationException("Feral summon leaf has no native spawn action: "+source.AssetGuid);
                        foreach(var spawn in spawns){Set(spawn,"m_SummonPool",Ref<BlueprintSummonPoolReference>(pool));spawn.DurationValue.Rate=DurationRate.Minutes;}
                        var run=a.GetComponent<AbilityEffectRunAction>()??throw new InvalidOperationException("Feral summon leaf lacks its native action list.");var clear=new FeralClearOwnSummons01461{Pool=Ref<BlueprintSummonPoolReference>(pool)};run.Actions.Actions=new Kingmaker.ElementsSystem.GameAction[]{clear}.Concat(run.Actions.Actions).ToArray();
                    }
                    a.LocalizedDuration=Text(a.name,"duration","1 minute per hunter level");return a;
                }
                var ability=Clone(spell);var feature=f.Feature("FeralHunterSummon"+(i+1)+"Feature01461",ability.Name,SummonText,Facts(ability));Icon(feature,ability);if(i==0)feature.ComponentsArray=feature.ComponentsArray.Concat(new[]{Grant(resource)}).ToArray();result.Add(feature);
            }
            origins=allowed.Values.Select(Ref<BlueprintAbilityReference>).ToArray();return result.ToArray();
        }
        static BlueprintFeature[] BuildShapes(PsychicAssets f,BlueprintCharacterClass cls)
        {
            var shapes=new[]{"Wolf","Leopard","Bear","DireWolf","Smilodon","Mastodon"};var sourceBuffs=new[]{"470fb1a22e7eb5849999f1101eacc5dc","8abf1c437ebee8048a4a3335efc27eb3","d5640d96888dd20489922045fde35059","470fb1a22e7eb5849999f1101eacc5dc","49a77c5c5266c42429f7afbb038ada60","470fb1a22e7eb5849999f1101eacc5dc"};var icons=new[]{"19bb148cb92db224abb431642d10efeb","c4d651bc0d4eabd41b08ee81bfe701d8","1368c7ce69702444893af5ffd3226e19","67a9dc42b15d0954ca4689b13e8dedea","253c0c0d00e50a24797445f20af52dc8","6adc3aab7cde56b40aa189a797254271"};var result=new List<BlueprintFeature>();
            for(int i=0;i<shapes.Length;i++){
                string shape=shapes[i];var buff=f.Clone(f.Native<BlueprintBuff>(sourceBuffs[i]),"FeralHunterWildShape"+shape+"Buff01461","Wild Shape ("+shape.Replace("DireWolf","Dire Wolf")+")",WildText);Icon(buff,f.Native<BlueprintFeature>(icons[i]));var poly=buff.GetComponent<Polymorph>()??throw new InvalidOperationException("Feral Hunter requires native animal polymorph.");
                if(shape=="DireWolf"||shape=="Mastodon"){
                    bool mammoth=shape=="Mastodon";var unit=f.Native<BlueprintUnit>(mammoth?"24dceb9ad134ee344a142c2a68f0f695":"87b83e0e06432a44eb50fb03c71bc8f5");Set(poly,"m_Prefab",unit.Prefab);Set(poly,"m_PrefabFemale",new Kingmaker.ResourceLinks.UnitViewLink{AssetId=""});poly.Size=mammoth?Size.Huge:Size.Large;poly.StrengthBonus=mammoth?6:4;poly.DexterityBonus=mammoth?-4:-2;poly.ConstitutionBonus=0;poly.NaturalArmor=mammoth?6:4;
                    Set(poly,"m_MainHand",Ref<BlueprintItemWeaponReference>(f.Native<BlueprintItemWeapon>(mammoth?"de42c58801037b84c9d992634ddd7220":"a000716f88c969c499a535dadcf09286")));Set(poly,"m_OffHand",mammoth?Ref<BlueprintItemWeaponReference>(f.Native<BlueprintItemWeapon>("c2ce7bc3559b2024ea91ddf5bb321f0a")):new BlueprintItemWeaponReference());Set(poly,"m_AdditionalLimbs",Array.Empty<BlueprintItemWeaponReference>());Set(poly,"m_SecondaryAdditionalLimbs",Array.Empty<BlueprintItemWeaponReference>());
                    var grants=new[]{"a2cb181ee69860b46b82844a3a8569b8","13c87ac5985cc85498ef9d1ac8b78923","d808863c4bd44fd8bd9cf5892460705d"}.Concat(mammoth?new[]{"9292099e5fd70f84fb07fbb9b8b6a5a5","1a3b471ecea51f7439a946b23577fd70"}:new[]{"f957b4444b6fb404e84ae2a5765797bb"});Set(poly,"m_Facts",grants.Select(g=>Ref<BlueprintUnitFactReference>(f.Native<BlueprintUnitFact>(g))).ToArray());
                    buff.GetComponent<BuffMovementSpeed>().Value=10;var barks=buff.GetComponent<ReplaceAsksList>();Set(barks,"m_Asks",Ref<BlueprintUnitAsksListReference>(unit.Visual.Barks));
                    Icon(buff,f.Native<BlueprintFeature>(mammoth?"6adc3aab7cde56b40aa189a797254271":"67a9dc42b15d0954ca4689b13e8dedea"));
                }
                else{
                    var fourLegs=f.Native<BlueprintFeature>("13c87ac5985cc85498ef9d1ac8b78923");var grants=(BlueprintUnitFactReference[])Get(poly,"m_Facts")??Array.Empty<BlueprintUnitFactReference>();
                    bool alreadyGranted=false;foreach(var grant in grants)if(grant!=null&&grant.Guid==fourLegs.AssetGuid){alreadyGranted=true;break;}
                    if(!alreadyGranted)Set(poly,"m_Facts",grants.Concat(new[]{Ref<BlueprintUnitFactReference>(fourLegs)}).ToArray());
                }
                var ability=f.Clone(f.Native<BlueprintAbility>("ac8811714a45a5948b27208538ce4f03"),"FeralHunterWildShape"+shape+"Ability01461",buff.Name,WildText);Icon(ability,buff);Set(ability,"m_Parent",new BlueprintAbilityReference());
                var graph=PsychicRemainingDisciplines01458.Graph(ability.ComponentsArray).ToArray();var applications=graph.OfType<ContextActionApplyBuff>().Where(a=>a.Buff.GetComponent<Polymorph>()!=null).ToArray();if(applications.Length!=1)throw new InvalidOperationException("Feral wild shape requires exactly one native animal application.");var source=applications[0].Buff;Set(applications[0],"m_Buff",Ref<BlueprintBuffReference>(buff));
                foreach(var restriction in graph.OfType<AbilityTargetHasFact>()){var refs=(BlueprintUnitFactReference[])Get(restriction,"m_CheckedFacts");Set(restriction,"m_CheckedFacts",refs.Select(r=>r.Guid==source.AssetGuid?Ref<BlueprintUnitFactReference>(buff):r).ToArray());}
                foreach(var rank in graph.OfType<ContextRankConfig>()){SetEnum(rank,"m_BaseValueType","ClassLevel");SetEnum(rank,"m_Progression","AsIs");Set(rank,"m_StartLevel",0);Set(rank,"m_StepLevel",0);Set(rank,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(cls)});Set(rank,"Archetype",new BlueprintArchetypeReference());Set(rank,"m_AdditionalArchetypes",Array.Empty<BlueprintArchetypeReference>());}
                var feature=f.Feature("FeralHunterWildShape"+shape+"Feature01461",ability.Name,WildText,Facts(ability));Icon(feature,ability);if(i==0)feature.ComponentsArray=feature.ComponentsArray.Concat(new[]{Grant(f.Native<BlueprintAbilityResource>("ae6af4d58b70a754d868324d1a05eda4"))}).ToArray();result.Add(feature);
            }
            return result.ToArray();
        }
    }
}
