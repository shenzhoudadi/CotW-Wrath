// COTW-0068 F-0068. Homebrew-Archetypes Investigator variant with a private
// six-circle psychic spellbook. The provider's own class, progression, talent
// selection, poison features and native roots are never cloned or rewritten.
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicDetective0068
    {
        internal const string ModuleName = PsychicDetective0068Ids.ModuleName;
        internal const string Description = "Some investigators turn their study of the arcane inward, unlocking psychic magic through deduction, discipline and a relentless interrogation of the mind.";
        internal static string LastSummary = "Not registered";
        internal static string LastSpellSummary = "Not built";
        internal static string[] LastMissing = Array.Empty<string>();
        internal static string[] LastOptional = Array.Empty<string>();

        private static T Provider<T>(BlueprintsCache cache,string id,string name,List<string> missing) where T : SimpleBlueprint
        {
            var bp=cache.Load(BlueprintGuid.Parse(id));
            if(bp==null){missing.Add(name+" missing: "+id);return null;}
            if(bp.GetType()!=typeof(T)||bp.name!=name||bp.AssetGuid!=BlueprintGuid.Parse(id))
                throw new InvalidOperationException("Unexpected HomebrewArchetypes identity: "+name+" / "+id);
            return (T)bp;
        }

        private static void RequireOwn(BlueprintsCache cache,string token,string label,List<string> missing)
        {
            if(cache.Load(Id(token))==null)missing.Add("Psychic shared root missing: "+label);
        }

        private static void RequireNative(BlueprintsCache cache,string id,string label,List<string> missing)
        {
            if(cache.Load(BlueprintGuid.Parse(id))==null)missing.Add("Native dependency missing: "+label);
        }

        // Q01: the provider graph and every shared Psychic root must be real
        // before a single private blueprint is published.
        internal static string[] MissingProvider(BlueprintsCache cache)
        {
            var missing=new List<string>();
            if(cache==null){missing.Add("BlueprintsCache is null");return missing.ToArray();}
            var cls=Provider<BlueprintCharacterClass>(cache,PsychicDetective0068Ids.ClassId,"InvestigatorClass",missing);
            var progression=Provider<BlueprintProgression>(cache,PsychicDetective0068Ids.ProgressionId,"InvestigatorProgression",missing);
            var talents=Provider<BlueprintFeatureSelection>(cache,PsychicDetective0068Ids.TalentSelectionId,"InvestigatorTalentSelection",missing);
            var poison=Provider<BlueprintFeature>(cache,PsychicDetective0068Ids.PoisonResistanceId,"InvestigatorPoisonResistance",missing);
            var immunity=Provider<BlueprintFeature>(cache,PsychicDetective0068Ids.PoisonImmunityId,"InvestigatorPoisonImmunity",missing);
            var discovery=Provider<BlueprintFeatureSelection>(cache,PsychicDetective0068Ids.AlchemistDiscoveryId,"AlchemistDiscovery",missing);
            RequireNative(cache,PsychicDetective0068Ids.DetectMagicId,"DetectMagic",missing);
            RequireNative(cache,PsychicDetective0068Ids.BardBookId,"BardSpellbook",missing);
            RequireNative(cache,PsychicDetective0068Ids.BardSpellsPerDayId,"BardSpellsPerDay",missing);
            RequireNative(cache,PsychicDetective0068Ids.BardSpellsKnownId,"BardSpellsKnown",missing);
            RequireNative(cache,PsychicClass.CenterAbilityId,"CenterSelf",missing);
            RequireNative(cache,PsychicClass.ListId,"PsychicSpellList",missing);
            foreach(var token in PsychicDetective0068Ids.BasicFeatureTokens)RequireOwn(cache,token,token,missing);
            foreach(var token in new[]{"DefensivePrognostication1Effect","DefensivePrognostication2Effect","ConjuredArmorEffect"})
                RequireOwn(cache,token,token,missing);
            if(cls!=null&&progression!=null)
            {
                var link=(BlueprintProgressionReference)Get(cls,"m_Progression");
                if(link==null||link.Guid!=progression.AssetGuid)missing.Add("Investigator class does not use its expected progression");
                if(cls.Spellbook==null||cls.Spellbook.AssetGuid!=BlueprintGuid.Parse("961e6b4edd3c80e4a9fe4435deff6fe7"))
                    missing.Add("Investigator class lost its provider spellbook identity");
            }
            if(progression!=null&&poison!=null&&immunity!=null&&talents!=null)
            {
                foreach(int level in new[]{2,5,8})if(!HasFeature(progression,level,poison))missing.Add("Progression level "+level+" lacks poison resistance");
                if(!HasFeature(progression,11,immunity))missing.Add("Progression level 11 lacks poison immunity");
                foreach(int level in new[]{3,5,7,9,11,13,15,17,19})if(!HasFeature(progression,level,talents))missing.Add("Progression level "+level+" lacks investigator talent");
                var headers=(BlueprintFeatureBaseReference[])Get(progression,"m_UIDeterminatorsGroup")??Array.Empty<BlueprintFeatureBaseReference>();
                if(!headers.Any(r=>r?.Guid==BlueprintGuid.Parse(PsychicDetective0068Ids.DetectMagicId)))
                    missing.Add("Provider no longer exposes native Detect Magic");
            }
            if(discovery!=null)
            {
                var ids=new HashSet<BlueprintGuid>(discovery.AllFeatures.Select(f=>f.AssetGuid));
                foreach(var id in PsychicDetective0068Ids.ForbiddenDiscoveryIds)
                    if(!ids.Contains(BlueprintGuid.Parse(id)))missing.Add("AlchemistDiscovery lost native option "+id);
            }
            return missing.ToArray();
        }

        private static bool HasFeature(BlueprintProgression progression,int level,BlueprintFeatureBase feature)
        {
            var entry=progression.LevelEntries.FirstOrDefault(e=>e.Level==level);
            return entry!=null&&entry.Features.Any(f=>f.AssetGuid==feature.AssetGuid);
        }

        // The detective's pool is the psychic's phrenic pool in everything but
        // its size, so it carries the very same icon: the psychic resource's
        // own, or the native Mind Fog ability CotW used when that is missing.
        private static BlueprintAbilityResource Pool(PsychicAssets f,BlueprintCharacterClass cls,BlueprintAbilityResource donor)
        {
            var resource=f.Add(new BlueprintAbilityResource(),PsychicDetective0068Ids.PoolToken,"","");
            var icon=donor?.Icon??f.Native<BlueprintAbility>("eabf94e4edc6e714cabd96aa69f8b207").Icon;
            if(icon!=null)Set(resource,"m_Icon",icon);
            var amount=Get(resource,"m_MaxAmount");
            Set(amount,"BaseValue",0);
            Set(amount,"m_Class",Array.Empty<BlueprintCharacterClassReference>());
            Set(amount,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());
            Set(amount,"IncreasedByStat",false);
            Set(amount,"IncreasedByLevelStartPlusDivStep",true);
            Set(amount,"StartingLevel",2);Set(amount,"StartingIncrease",1);
            Set(amount,"LevelStep",2);Set(amount,"PerStepIncrease",1);
            Set(amount,"m_ClassDiv",new[]{Ref<BlueprintCharacterClassReference>(cls)});
            Set(amount,"m_ArchetypesDiv",Array.Empty<BlueprintArchetypeReference>());
            Set(resource,"m_MaxAmount",amount);return resource;
        }

        private static BlueprintSpellbook Book(PsychicAssets f,BlueprintCharacterClass cls,BlueprintSpellList list)
        {
            var bard=f.Native<BlueprintSpellbook>(PsychicDetective0068Ids.BardBookId);
            if(Get(bard,"m_SpellsPerDay")==null||Get(bard,"m_SpellsKnown")==null)
                throw new InvalidOperationException("Bard donor lost its native progression tables.");
            var book=f.Clone(bard,PsychicDetective0068Ids.BookToken,"Psychic Detective Spellbook",
                "A private six-circle Intelligence spontaneous psychic spellbook.");
            book.CastingAttribute=StatType.Intelligence;book.Spontaneous=true;book.SpellsPerLevel=0;
            book.AllSpellsKnown=false;book.CanCopyScrolls=false;book.CantripsType=CantripsType.Cantrips;
            book.CasterLevelModifier=0;book.IsArcane=false;book.IsArcanist=false;book.IsMythic=false;
            book.ComponentsArray=new BlueprintComponent[]{new PsychicSpellbookMarker{name="DetectiveSixCircleMarker",MaximumSpellLevel=6}};
            foreach(var field in new[]{"m_SpellsKnown","m_SpellsPerDay"})Set(book,field,Get(bard,field));
            Set(book,"m_SpellSlots",null);Set(book,"m_MythicSpellList",null);Set(book,"m_SpellList",Ref<BlueprintSpellListReference>(list));
            // The provider's alchemist book points at the native Alchemist class.
            // This replacement is explicitly owned by the real HA Investigator.
            Set(book,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));
            return book;
        }

        // The detective's list is the Psychic list itself, circles 0-6. Ranked
        // families (Mind Thrust I-VI and the like) are separate spells at their
        // own circle, so copying the list keeps every rank the psychic has.
        private static BlueprintSpellList BuildList(PsychicAssets f,BlueprintsCache cache)
        {
            var source=cache.Load(BlueprintGuid.Parse(PsychicClass.ListId)) as BlueprintSpellList
                ??throw new InvalidOperationException("0068 Psychic spell list is missing.");
            var levels=Enumerable.Range(0,11).Select(_=>new List<BlueprintAbility>()).ToArray();
            foreach(var row in source.SpellsByLevel??Array.Empty<SpellLevelList>())
            {
                if(row==null||row.SpellLevel<0||row.SpellLevel>6)continue;
                foreach(var spell in row.Spells??Array.Empty<BlueprintAbility>())
                    if(spell!=null&&!levels[row.SpellLevel].Any(a=>a.AssetGuid==spell.AssetGuid))
                        levels[row.SpellLevel].Add(spell);
            }
            int resolved=levels.Sum(g=>g.Count);
            if(resolved==0)throw new InvalidOperationException("0068 Psychic spell list exposes no spell in circles 0-6.");
            LastOptional=Array.Empty<string>();
            LastSpellSummary="Psychic list circles 0-6: "+resolved+" spells copied from "+source.name+".";
            var list=SpellBatch01447.List(f,PsychicDetective0068Ids.ListToken,index=>index<levels.Length?levels[index].ToArray():Array.Empty<BlueprintAbility>());
            list.SpellsByLevel=SpellListCompatibility.Build(list.SpellsByLevel);
            if(!SpellListCompatibility.Canonical(list.SpellsByLevel))throw new InvalidOperationException("0068 detective spell list is not canonical.");
            return list;
        }

        // Q01: a save reload republishes this package before every later
        // provider has finished; re-copy the psychic circles so nothing that
        // arrived late stays missing. No foreign list is touched.
        internal static void RefreshOptional(BlueprintsCache cache)
        {
            var list=cache.Load(Id(PsychicDetective0068Ids.ListToken)) as BlueprintSpellList;
            var source=cache.Load(BlueprintGuid.Parse(PsychicClass.ListId)) as BlueprintSpellList;
            if(list?.SpellsByLevel==null||source?.SpellsByLevel==null)return;
            foreach(var row in list.SpellsByLevel)
            {
                if(row==null||row.SpellLevel<0||row.SpellLevel>6)continue;
                var refs=new List<BlueprintAbilityReference>();
                var donor=source.SpellsByLevel.FirstOrDefault(r=>r!=null&&r.SpellLevel==row.SpellLevel);
                if(donor!=null)
                    foreach(var spell in donor.Spells??Array.Empty<BlueprintAbility>())
                        if(spell!=null&&!refs.Any(r=>r.Guid==spell.AssetGuid))refs.Add(Ref<BlueprintAbilityReference>(spell));
                Set(row,"m_Spells",refs);Set(row,"m_SpellsProxy",null);Set(row,"m_SpellsFiltered",null);
            }
        }

        private static BlueprintFeature[] Amplifications(PsychicAssets f,BlueprintsCache cache,
            BlueprintCharacterClass cls,BlueprintSpellbook book,BlueprintAbilityResource pool,
            List<BlueprintActivatableAbility> modes)
        {
            var wisdom=cache.Load(BlueprintGuid.Parse(PsychicClass.WisdomPoolId)) as BlueprintAbilityResource;
            var charisma=cache.Load(BlueprintGuid.Parse(PsychicClass.CharismaPoolId)) as BlueprintAbilityResource;
            var defense1=cache.Load(Id("DefensivePrognostication1Effect")) as BlueprintBuff;
            var defense2=cache.Load(Id("DefensivePrognostication2Effect")) as BlueprintBuff;
            var armor=cache.Load(Id("ConjuredArmorEffect")) as BlueprintBuff;
            var features=new List<BlueprintFeature>();
            var toggles=new List<BlueprintActivatableAbility>();

            BlueprintActivatableAbility Mode(string name,PsychicAmplificationKind kind,int cost,int strength,int minimum,
                BlueprintActivatableAbility source,BlueprintBuff effect=null)
            {
                var component=new PsychicAmplificationComponent{name="DetectiveLinkedPhrenicCast",Kind=kind,Cost=cost,
                    Strength=strength,MinimumLevel=minimum,CharacterClass=Ref<BlueprintCharacterClassReference>(cls),
                    BoundBook=Ref<BlueprintSpellbookReference>(book),BoundPool=Ref<BlueprintAbilityResourceReference>(pool),
                    WisdomPool=wisdom==null?null:Ref<BlueprintAbilityResourceReference>(wisdom),
                    CharismaPool=charisma==null?null:Ref<BlueprintAbilityResourceReference>(charisma),
                    Defense=effect==null?null:Ref<BlueprintBuffReference>(effect)};
                var buff=f.Buff("PsychicDetective0068"+name+"Mode",name,name,component);
                var toggle=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.Immediately,
                    DeactivateImmediately=true,DeactivateIfOwnerDisabled=true,DeactivateIfOwnerUnconscious=true,
                    ComponentsArray=Array.Empty<BlueprintComponent>()},"PsychicDetective0068"+name+"Toggle",name,name);
                Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(buff));
                Set(toggle,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free);
                // The psychic toggle carries the published name, description and
                // icon; the detective copy must show exactly those in the bar.
                if(source!=null){Adopt(toggle,source);Adopt(buff,source);}
                toggles.Add(toggle);modes.Add(toggle);return toggle;
            }

            BlueprintFeature Feature(string token,string title,string text,params BlueprintComponent[] components)
                =>f.Feature("PsychicDetective0068"+token,title,text,components);

            void Adopt(BlueprintUnitFact fact,BlueprintUnitFact source)
            {
                Set(fact,"m_DisplayName",Get(source,"m_DisplayName"));
                Set(fact,"m_Description",Get(source,"m_Description"));
                if(source.Icon!=null)Set(fact,"m_Icon",source.Icon);
            }

            BlueprintFeature AddTiered(string token,BlueprintFeature source,PsychicAmplificationKind kind,
                ModeInfo[] found,int[] gates,bool linked,BlueprintBuff[] effects)
            {
                var granted=new List<BlueprintUnitFact>();
                var components=new List<BlueprintComponent>();
                for(int i=1;i<=found.Length;i++)
                {
                    var effect=effects!=null&&effects.Length>0?effects[Math.Min(i-1,effects.Length-1)]:null;
                    var toggle=Mode(token+i,kind,found[i-1].Mode.Cost,found[i-1].Mode.Strength,
                        found[i-1].Mode.MinimumLevel,found[i-1].Source,effect);
                    // Tiers the Psychic feature gates behind AddFeatureOnClassLevel
                    // arrive the same way, so a low-level toggle never exists to
                    // switch modes off.
                    if(gates[i-1]>0)
                    {
                        var tier=Feature(token+"Tier"+i,"","",Facts(toggle));
                        Adopt(tier,source);
                        var add=new AddFeatureOnClassLevel{name="DetectiveTier"+token+i,Level=gates[i-1]};
                        Set(add,"m_Class",Ref<BlueprintCharacterClassReference>(cls));
                        Set(add,"m_Feature",Ref<BlueprintFeatureReference>(tier));
                        Set(add,"m_AdditionalClasses",Array.Empty<BlueprintCharacterClassReference>());
                        Set(add,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());
                        components.Add(add);
                    }
                    else granted.Add(toggle);
                }
                if(granted.Count>0)components.Insert(0,Facts(granted.ToArray()));
                if(linked)components.Add(new PsychicLinkedSpellEffects{name="DetectivePaid"+token,Kind=kind});
                var feature=Feature(token,"","",components.ToArray());
                Adopt(feature,source);
                return feature;
            }

            // Read the list from Psychic's own selection instead of maintaining a
            // parallel hand-written catalogue. Amplifications gated on Major
            // Amplifications (or Bodily Mutations once it is published) stay with
            // the full psychic class and never appear here.
            var source=cache.Load(Id("PhrenicAmplificationSelection")) as BlueprintFeatureSelection
                ??throw new InvalidOperationException("0068 Psychic phrenic amplification selection is missing.");
            foreach(var offered in source.AllFeatures)
            {
                if(offered==null||NeedsAdvancedPsychic(offered))continue;
                var found=AmplificationModes(offered);
                if(found.Count==0)continue; // Psychofeedback is rebuilt separately below
                found=found.OrderBy(m=>m.Mode.MinimumLevel).ThenBy(m=>m.Mode.Cost).ToList();
                var kind=found[0].Mode.Kind;
                var token=kind.ToString();
                var gated=offered.GetComponents<AddFeatureOnClassLevel>().Any();
                var gates=Enumerable.Range(0,found.Count).Select(i=>i>0&&gated?found[i].Mode.MinimumLevel:0).ToArray();
                var effects=kind==PsychicAmplificationKind.DefensivePrognostication?new[]{defense1,defense2}
                    :kind==PsychicAmplificationKind.ConjuredArmor?new[]{armor}:new BlueprintBuff[0];
                features.Add(AddTiered(token,offered,kind,found.ToArray(),gates,
                    offered.GetComponent<PsychicLinkedSpellEffects>()!=null,effects));
            }
            features.Add(Psychofeedback(f,cache,cls,pool));
            return features.ToArray();
        }

        // Major Amplifications (and, once it exists, Bodily Mutations) are the
        // full psychic class's advanced gates; the archetype only borrows the
        // basic amplifications.
        private static bool NeedsAdvancedPsychic(BlueprintFeature feature)
        {
            var major=BlueprintGuid.Parse(PsychicClass.MajorId);
            foreach(var gate in feature.GetComponents<PrerequisiteFeature>())
            {
                var reference=(BlueprintFeatureReference)Get(gate,"m_Feature");
                if(reference==null)continue;
                if(reference.Guid==major)return true;
                var blueprint=reference.Get();
                if(blueprint?.name!=null&&blueprint.name.IndexOf("BodilyMutation",StringComparison.OrdinalIgnoreCase)>=0)return true;
            }
            return false;
        }

        private readonly struct ModeInfo
        {
            internal readonly PsychicAmplificationComponent Mode;
            internal readonly BlueprintActivatableAbility Source;
            internal ModeInfo(PsychicAmplificationComponent mode,BlueprintActivatableAbility source){Mode=mode;Source=source;}
        }

        // A Psychic amplification feature grants its mode toggles directly or
        // through tier features that AddFeatureOnClassLevel hands out later.
        // The owning toggle is kept so the detective copy can adopt its name,
        // description and icon instead of inventing its own.
        private static List<ModeInfo> AmplificationModes(BlueprintFeature feature)
        {
            var result=new List<ModeInfo>();
            void Walk(BlueprintFeature current,int depth)
            {
                if(depth>2)return;
                foreach(var grant in current.GetComponents<AddFacts>())
                    foreach(var reference in (BlueprintUnitFactReference[])Get(grant,"m_Facts")??Array.Empty<BlueprintUnitFactReference>())
                    {
                        var fact=reference?.Get();
                        if(fact is BlueprintActivatableAbility toggle)
                        {
                            var mode=toggle.Buff?.GetComponent<PsychicAmplificationComponent>();
                            if(mode!=null)result.Add(new ModeInfo(mode,toggle));
                        }
                        else if(fact is BlueprintFeature tier)Walk(tier,depth+1);
                    }
                foreach(var gate in current.GetComponents<AddFeatureOnClassLevel>())
                    if(((BlueprintFeatureReference)Get(gate,"m_Feature"))?.Get() is BlueprintFeature tier)Walk(tier,depth+1);
            }
            Walk(feature,0);
            return result;
        }

        private static BlueprintFeature Psychofeedback(PsychicAssets f,BlueprintsCache cache,
            BlueprintCharacterClass cls,BlueprintAbilityResource pool)
        {
            const string text="As a standard action, spend 2 phrenic points and sacrifice a 2nd- through 6th-level Psychic Detective spell slot. Gain an enhancement bonus to Strength, Dexterity or Constitution equal to the spent slot's level for one minute per investigator level.";
            var stats=new[]{StatType.Strength,StatType.Dexterity,StatType.Constitution};
            var conversions=new List<BlueprintComponent>();
            foreach(var stat in stats)
            {
                var spells=Enumerable.Range(0,10).Select(_=>new BlueprintAbilityReference()).ToArray();
                for(int level=2;level<=6;level++)
                {
                    var effect=cache.Load(Id("Psychofeedback"+stat+level+"Buff")) as BlueprintBuff;
                    if(effect==null)continue;
                    var rank=new ContextRankConfig{name="DetectivePsychofeedbackDuration"};
                    SetEnum(rank,"m_BaseValueType","ClassLevel");Set(rank,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(cls)});
                    var action=Apply(effect,0,DurationRate.Minutes,toCaster:true,fromSpell:false);
                    action.IsNotDispelable=true;
                    action.DurationValue.BonusValue=new ContextValue{ValueType=ContextValueType.Rank,ValueRank=AbilityRankType.Default};
                    var cost=new AbilityResourceLogic{name="DetectivePsychofeedbackCost",Amount=2};
                    Set(cost,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(pool));Set(cost,"m_IsSpendResource",true);
                    var ability=f.Ability("PsychofeedbackDetective0068"+stat+level,"Psychofeedback: +"+level+" "+stat,text,
                        AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Personal,rank,cost,Run(action));
                    spells[level]=Ref<BlueprintAbilityReference>(ability);
                }
                var conversion=new SpontaneousSpellConversion{name="DetectivePsychofeedback"+stat};
                Set(conversion,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));
                Set(conversion,"m_SpellsByLevel",spells);conversions.Add(conversion);
            }
            return f.Feature("PsychicDetective0068PsychofeedbackFeature","Psychofeedback",text,conversions.ToArray());
        }
        private static PrerequisiteArchetypeLevel Need(BlueprintCharacterClass cls,BlueprintArchetype root,int level)
        {
            var p=new PrerequisiteArchetypeLevel{name="DetectiveRequiresArchetype",Level=level};
            Set(p,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));
            Set(p,"m_Archetype",Ref<BlueprintArchetypeReference>(root));return p;
        }

        internal static bool Register(BlueprintsCache cache)
        {
            var missing=MissingProvider(cache);
            LastMissing=missing;
            if(missing.Length!=0){LastSummary="Deferred: "+string.Join("; ",missing);return false;}
            var cls=cache.Load(BlueprintGuid.Parse(PsychicDetective0068Ids.ClassId)) as BlueprintCharacterClass;
            var progression=cache.Load(BlueprintGuid.Parse(PsychicDetective0068Ids.ProgressionId)) as BlueprintProgression;
            var talents=cache.Load(BlueprintGuid.Parse(PsychicDetective0068Ids.TalentSelectionId)) as BlueprintFeatureSelection;
            var poison=cache.Load(BlueprintGuid.Parse(PsychicDetective0068Ids.PoisonResistanceId)) as BlueprintFeature;
            var immunity=cache.Load(BlueprintGuid.Parse(PsychicDetective0068Ids.PoisonImmunityId)) as BlueprintFeature;

            // Re-entry: every private identity is already published. Verify the
            // shared links and refresh only our own optional spell references.
            var existingRoot=cache.Load(Id(PsychicDetective0068Ids.ArchetypeToken)) as BlueprintArchetype;
            if(existingRoot!=null)
            {
                VerifyShared(cache,cls,progression,talents,existingRoot);
                RefreshOptional(cache);
                ExtraInvestigatorTalent.Register(cache);
                LastSummary="Already registered; shared links verified and optional spell list refreshed.";
                return true;
            }

            var f=new PsychicAssets(cache);
            var list=BuildList(f,cache);
            var book=Book(f,cls,list);
            var psychicPool=cache.Load(BlueprintGuid.Parse(PsychicClass.WisdomPoolId)) as BlueprintAbilityResource;
            var pool=Pool(f,cls,psychicPool);
            var center=cache.Load(BlueprintGuid.Parse(PsychicClass.CenterAbilityId)) as BlueprintAbility;
            var casting=f.Feature(PsychicDetective0068Ids.CastingToken,"Psychic Spellcasting",
                "A psychic detective casts spells spontaneously from a private six-circle Intelligence spellbook.",Facts(center));
            var meddler=f.Feature(PsychicDetective0068Ids.MeddlerToken,"Psychic Meddler",
                "Add a bonus on saving throws against mind-affecting spells and spell-like abilities. The bonus rises at 2nd, 5th, 8th, 11th, 14th and 17th investigator level, to a maximum of +6.",
                new PsychicDetectiveMeddler0068{CharacterClass=Ref<BlueprintCharacterClassReference>(cls),MaximumBonus=6});
            var dabbler=f.Feature(PsychicDetective0068Ids.DabblerToken,"Phrenic Dabbler",
                "Gain a phrenic pool equal to half your investigator level and one basic phrenic amplification.",
                new AddAbilityResources{name="DetectivePhrenicPool",RestoreAmount=true,RestoreOnLevelUp=true});
            if(pool.Icon!=null)Set(dabbler,"m_Icon",pool.Icon);
            Set(dabbler.GetComponent<AddAbilityResources>(),"m_Resource",Ref<BlueprintAbilityResourceReference>(pool));

            var modes=new List<BlueprintActivatableAbility>();
            var amplificationFeatures=Amplifications(f,cache,cls,book,pool,modes);
            var selection=f.Add(new BlueprintFeatureSelection{Ranks=1,IsClassFeature=true},
                PsychicDetective0068Ids.AmplificationSelectionToken,"Basic Phrenic Amplification",
                "Choose one of the eleven basic phrenic amplifications.");
            Set(selection,"m_Features",amplificationFeatures.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(selection,"m_AllFeatures",amplificationFeatures.Select(Ref<BlueprintFeatureReference>).ToArray());
            PsychicAmplificationMenu.BuildDetective(f,modes,casting);

            // D-F01: level 1 grants the native Detect Magic feature itself, not
            // just the provider progression's UI header. Never cloned.
            var detect=cache.Load(BlueprintGuid.Parse(PsychicDetective0068Ids.DetectMagicId)) as BlueprintFeature;
            var add=new[]{Entry(1,detect,casting),Entry(2,meddler),Entry(3,dabbler,selection)};
            var remove=new[]{Entry(2,poison),Entry(3,talents),Entry(5,poison),Entry(8,poison),Entry(11,immunity)};
            var root=SpellBatch01447.Archetype(f,cls,PsychicDetective0068Ids.ArchetypeToken,"Psychic Detective",Description,add,remove);
            root.ChangeCasterType=true;root.IsArcaneCaster=false;root.IsDivineCaster=false;
            root.ReplaceClassSkills=true;
            root.ClassSkills=new[]{StatType.SkillStealth,StatType.SkillThievery,StatType.SkillKnowledgeArcana,
                StatType.SkillKnowledgeWorld,StatType.SkillLoreNature,StatType.SkillLoreReligion,
                StatType.SkillPerception,StatType.SkillPersuasion,StatType.SkillUseMagicDevice};
            Set(root,"m_ReplaceSpellbook",Ref<BlueprintSpellbookReference>(book));

            // The talent is listed in the HomebrewArchetypes Investigator talent
            // selection and raises whichever pool the character actually uses:
            // the shared psychic pool when multiclassed, the private half-level
            // pool otherwise.
            var expanded=f.Feature(PsychicDetective0068Ids.ExpandedPoolTalentToken,"Expanded Phrenic Pool",
                "Your phrenic pool increases by 2 points.",Need(cls,root,3),
                new IncreaseResourceAmount{name="DetectiveExpandedPool",Value=2},
                new IncreaseResourceAmount{name="DetectiveExpandedPoolWisdom",Value=2},
                new IncreaseResourceAmount{name="DetectiveExpandedPoolCharisma",Value=2});
            Set(expanded.GetComponent<IncreaseResourceAmount>(),"m_Resource",Ref<BlueprintAbilityResourceReference>(pool));
            // Option B: detective levels count toward the shared psychic pool, so
            // a multiclass cap is floor((psychic + detective) / 2) plus the
            // discipline's stat modifier. A pure detective never receives that
            // resource and keeps the private half-level pool instead.
            var sharedPools=new[]{PsychicClass.WisdomPoolId,PsychicClass.CharismaPoolId}
                .Select(id=>cache.Load(BlueprintGuid.Parse(id)) as BlueprintAbilityResource).Where(r=>r!=null).ToArray();
            var sharedDivOld=sharedPools.Select(r=>(BlueprintArchetypeReference[])Get(Get(r,"m_MaxAmount"),"m_ArchetypesDiv")).ToArray();
            void LinkSharedPools()
            {
                for(int i=0;i<sharedPools.Length;i++)
                {
                    var current=sharedDivOld[i]??Array.Empty<BlueprintArchetypeReference>();
                    if(current.Any(r=>r?.Guid==root.AssetGuid))continue;
                    Set(Get(sharedPools[i],"m_MaxAmount"),"m_ArchetypesDiv",
                        current.Concat(new[]{Ref<BlueprintArchetypeReference>(root)}).ToArray());
                }
            }
            void UnlinkSharedPools()
            {
                for(int i=0;i<sharedPools.Length;i++)Set(Get(sharedPools[i],"m_MaxAmount"),"m_ArchetypesDiv",sharedDivOld[i]);
            }

            var sharedAmounts=new[]{PsychicClass.WisdomPoolId,PsychicClass.CharismaPoolId}
                .Select(id=>cache.Load(BlueprintGuid.Parse(id)) as BlueprintAbilityResource).Where(r=>r!=null).ToArray();
            var sharedComponents=expanded.GetComponents<IncreaseResourceAmount>().Skip(1).ToArray();
            for(int i=0;i<sharedComponents.Length&&i<sharedAmounts.Length;i++)
                Set(sharedComponents[i],"m_Resource",Ref<BlueprintAbilityResourceReference>(sharedAmounts[i]));
            var extra=f.Add(new BlueprintFeatureSelection{Ranks=1,IsClassFeature=true,
                ComponentsArray=new BlueprintComponent[]{Need(cls,root,3)}},
                PsychicDetective0068Ids.ExtraAmplificationTalentToken,"Extra Phrenic Amplification",
                "Learn one additional basic phrenic amplification you do not already have.");
            Set(extra,"m_Features",amplificationFeatures.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(extra,"m_AllFeatures",amplificationFeatures.Select(Ref<BlueprintFeatureReference>).ToArray());

            var assets=f.Assets.Values.ToArray();
            var occupied=assets.Select(a=>cache.Load(a.AssetGuid)).ToArray();
            if(occupied.Any(a=>a!=null))throw new InvalidOperationException("0068 private identity collision: partial publication.");

            var oldArchetypes=(BlueprintArchetypeReference[])Get(cls,"m_Archetypes");
            var oldGroups=cls.Progression.UIGroups;
            var oldTalentFeatures=(BlueprintFeatureReference[])Get(talents,"m_Features");
            var oldTalentAll=(BlueprintFeatureReference[])Get(talents,"m_AllFeatures");
            var added=new List<SimpleBlueprint>();
            var ui=MagusBatch01454.Group(casting,meddler,dabbler,selection);
            try
            {
                f.Enable();
                LinkSharedPools();
                foreach(var asset in assets){cache.AddCachedBlueprint(asset.AssetGuid,asset);added.Add(asset);}
                Set(cls,"m_Archetypes",(oldArchetypes??Array.Empty<BlueprintArchetypeReference>())
                    .Concat(new[]{Ref<BlueprintArchetypeReference>(root)}).ToArray());
                cls.Progression.UIGroups=(oldGroups??Array.Empty<UIGroup>()).Concat(new[]{ui}).ToArray();
                Append(talents,"m_Features",oldTalentFeatures,expanded,extra);
                Append(talents,"m_AllFeatures",oldTalentAll,expanded,extra);
                VerifyShared(cache,cls,progression,talents,root);
                ExtraInvestigatorTalent.Register(cache);
            }
            catch
            {
                Set(cls,"m_Archetypes",oldArchetypes);cls.Progression.UIGroups=oldGroups;
                Set(talents,"m_Features",oldTalentFeatures);Set(talents,"m_AllFeatures",oldTalentAll);
                UnlinkSharedPools();
                foreach(var asset in added)if(ReferenceEquals(cache.Load(asset.AssetGuid),asset))cache.RemoveCachedBlueprint(asset.AssetGuid);
                throw;
            }
            LastSummary="Registered private six-circle spellbook, full progression, eleven bound amplifications and two talents; "+LastSpellSummary;
            return true;
        }

        private static void Append(BlueprintFeatureSelection selection,string field,BlueprintFeatureReference[] old,
            params BlueprintFeature[] values)
        {
            var current=(old??Array.Empty<BlueprintFeatureReference>()).ToList();
            foreach(var value in values)if(!current.Any(r=>r?.Guid==value.AssetGuid))current.Add(Ref<BlueprintFeatureReference>(value));
            Set(selection,field,current.ToArray());
        }

        private static void VerifyShared(BlueprintsCache cache,BlueprintCharacterClass cls,BlueprintProgression progression,
            BlueprintFeatureSelection talents,BlueprintArchetype root)
        {
            var archetypes=(BlueprintArchetypeReference[])Get(cls,"m_Archetypes")??Array.Empty<BlueprintArchetypeReference>();
            if(archetypes.Count(r=>r?.Guid==root.AssetGuid)!=1)throw new InvalidOperationException("0068 archetype link missing or duplicated.");
            if(root.GetParentClass()!=cls)throw new InvalidOperationException("0068 archetype parent class mismatch.");
            if(root.ReplaceSpellbook==null||root.ReplaceSpellbook.AssetGuid!=Id(PsychicDetective0068Ids.BookToken)||
                root.ReplaceSpellbook.CharacterClass!=cls)throw new InvalidOperationException("0068 ReplaceSpellbook identity mismatch.");
            if(root.RemoveFeatures.Length!=5||root.AddFeatures.Length!=3)
                throw new InvalidOperationException("0068 archetype progression entries incomplete.");
            var level1=root.AddFeatures.FirstOrDefault(e=>e.Level==1);
            if(level1==null||!level1.Features.Any(f=>f.AssetGuid==BlueprintGuid.Parse(PsychicDetective0068Ids.DetectMagicId))||
                !level1.Features.Any(f=>f.AssetGuid==Id(PsychicDetective0068Ids.CastingToken)))
                throw new InvalidOperationException("0068 level 1 must grant native Detect Magic and the private spellcasting feature.");
            var all=(BlueprintFeatureReference[])Get(talents,"m_AllFeatures")??Array.Empty<BlueprintFeatureReference>();
            foreach(var token in new[]{PsychicDetective0068Ids.ExpandedPoolTalentToken,PsychicDetective0068Ids.ExtraAmplificationTalentToken})
                if(all.Count(r=>r?.Guid==Id(token))!=1)throw new InvalidOperationException("0068 talent link missing or duplicated: "+token);
            var list=cache.Load(Id(PsychicDetective0068Ids.ListToken)) as BlueprintSpellList;
            if(list==null||!SpellListCompatibility.Canonical(list.SpellsByLevel))
                throw new InvalidOperationException("0068 canonical spell list missing.");
        }
    }
}
