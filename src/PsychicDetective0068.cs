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

        private sealed class Correction
        {
            internal readonly string OriginalRef, Guid;
            internal readonly int Level;
            internal readonly PsychicSpellLink Fallback;
            internal Correction(string originalRef,string guid,int level,PsychicSpellLink fallback=null)
            { OriginalRef=originalRef; Guid=guid; Level=level; Fallback=fallback; }
        }

        // A-evidence.json providerCorrections. Empty rows in the original 180-line
        // catalogue are resolved from existing shared links or the audited TTT
        // identity; no external spell is re-authored or faked as published.
        private static readonly Correction[] Corrections = {
            new Correction("Witch.hermean_potential.AssetGuid","37853550902a4cadae16833c5f4c6b17",1),
            new Correction("NewSpells.long_arm.AssetGuid","fde4d2da304345bca11a73eb8f06b755",1),
            new Correction("NewSpells.hypnotic_pattern.AssetGuid","56b8f0304a704a67b3c35cbe8c774854",2),
            new Correction("NewSpells.inflict_pain.AssetGuid","e023af1af9c147549a8e7bd246967861",2),
            new Correction("NewSpells.telekinetic_strikes.AssetGuid","9c2f011a4c664dccb73a1426fb55050e",2),
            new Correction("NewSpells.resinous_skin.AssetGuid","4b8f3792a86e45f4b40a2c01805f1e5e",3),
            new Correction("NewSpells.sands_of_time.AssetGuid","da49f07e431b47abaad41ef28df487f7",3),
            new Correction("NewSpells.stunning_barrier_greater.AssetGuid","9e33ed477ddd4fad9d90fb64b1a6f732",3,
                new PsychicSpellLink("9e33ed477ddd4fad9d90fb64b1a6f732","StunningBarrierGreaterAbility",
                    "TabletopTweaks-Base",SpellSchool.Abjuration,true,3,-1,-1)),
            new Correction("NewSpells.inflict_pain_mass.AssetGuid","ff31ae1abe3c418db7842dcc76eca7ee",6)
        };

        private static Correction FindCorrection(string originalRef) => Corrections.FirstOrDefault(c => c.OriginalRef == originalRef);

        private static PsychicSpellLink LinkFor(Correction correction) => correction.Fallback ??
            PsychicSharedSpellCatalog.Entries.FirstOrDefault(e => e.Guid == correction.Guid);

        private static BlueprintAbility ResolveCorrection(BlueprintsCache cache,Correction correction,out string reason)
        {
            reason=null;
            var link=LinkFor(correction);
            if(link==null){reason="no shared provider link";return null;}
            var spell=cache.Load(BlueprintGuid.Parse(correction.Guid)) as BlueprintAbility;
            if(spell==null){reason="provider root missing";return null;}
            if(!PsychicSpellIntegration.Available(cache,link,out var available)){reason="provider unavailable, disabled, wrong identity or unpublished";return null;}
            return available;
        }

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

        private static BlueprintAbilityResource Pool(PsychicAssets f,BlueprintCharacterClass cls)
        {
            var resource=f.Add(new BlueprintAbilityResource(),PsychicDetective0068Ids.PoolToken,"","");
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

        private static BlueprintSpellList BuildList(PsychicAssets f,BlueprintsCache cache)
        {
            var spells=Enumerable.Range(0,11).Select(_=>new List<BlueprintAbility>()).ToArray();
            var optional=new List<string>();int resolved=0,skipped=0;
            var traps=BlueprintGuid.Parse(PsychicDetective0068Ids.FindTrapsId);
            var banishment=BlueprintGuid.Parse(PsychicDetective0068Ids.BanishmentId);
            foreach(var row in MindbladeSpellAnchors01454.Rows)
            {
                int level=Mindblade01454.RowLevel(row);
                if(level<0||level>6)continue;
                var spell=Mindblade01454.ResolveRow(f,row);
                if(spell==null)
                {
                    var correction=FindCorrection(row[1]);
                    if(correction!=null)
                    {
                        spell=ResolveCorrection(cache,correction,out var reason);
                        optional.Add(correction.OriginalRef+": "+(spell==null?"skipped ("+reason+")":"available"));
                        if(spell!=null)level=correction.Level;
                    }
                }
                if(spell==null){skipped++;continue;}
                bool explicitRing=spell.AssetGuid==traps||spell.AssetGuid==banishment;
                int target=spell.AssetGuid==traps?1:spell.AssetGuid==banishment?6:level;
                if(explicitRing)foreach(var group in spells)group.RemoveAll(a=>a.AssetGuid==spell.AssetGuid);
                else if(spells.Any(g=>g.Any(a=>a.AssetGuid==spell.AssetGuid)))continue;
                if(target<0||target>6||spells[target].Any(a=>a.AssetGuid==spell.AssetGuid))continue;
                spells[target].Add(spell);resolved++;
            }
            LastOptional=optional.ToArray();
            LastSpellSummary=$"Original 180 rows: {resolved} available, {skipped} skipped, {optional.Count} provider corrections examined, levels 7-10 empty.";
            var list=SpellBatch01447.List(f,PsychicDetective0068Ids.ListToken,index=>index<spells.Length?spells[index].ToArray():Array.Empty<BlueprintAbility>());
            list.SpellsByLevel=SpellListCompatibility.Build(list.SpellsByLevel);
            if(!SpellListCompatibility.Canonical(list.SpellsByLevel))throw new InvalidOperationException("0068 detective spell list is not canonical.");
            return list;
        }

        // Q01: late optional providers only refresh this package's own list.
        // No foreign spell list, provider root or permanent known spell is touched.
        internal static void RefreshOptional(BlueprintsCache cache)
        {
            var list=cache.Load(Id(PsychicDetective0068Ids.ListToken)) as BlueprintSpellList;
            if(list==null||list.SpellsByLevel==null)return;
            foreach(var correction in Corrections)
            {
                if(correction.Level>=list.SpellsByLevel.Length)continue;
                var row=list.SpellsByLevel[correction.Level];
                var current=(List<BlueprintAbilityReference>)Get(row,"m_Spells")??new List<BlueprintAbilityReference>();
                var spell=ResolveCorrection(cache,correction,out _);
                var id=BlueprintGuid.Parse(correction.Guid);
                if(spell!=null)
                {
                    if(!current.Any(r=>r.Guid==id))current.Add(Ref<BlueprintAbilityReference>(spell));
                }
                else current.RemoveAll(r=>r.Guid==id);
                Set(row,"m_Spells",current);Set(row,"m_SpellsProxy",null);Set(row,"m_SpellsFiltered",null);
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

            BlueprintActivatableAbility Mode(string name,PsychicAmplificationKind kind,int cost,int strength,int minimum,BlueprintBuff effect=null)
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
                toggles.Add(toggle);modes.Add(toggle);return toggle;
            }

            BlueprintFeature Feature(string token,string title,string text,params BlueprintComponent[] components)
                =>f.Feature("PsychicDetective0068"+token,title,text,components);

            void Add(string token,string title,string text,PsychicAmplificationKind kind,int cost,int strength,
                int minimum,BlueprintBuff effect=null,bool linked=false,int tiers=1)
            {
                var granted=new List<BlueprintUnitFact>();
                for(int i=1;i<=tiers;i++)
                {
                    int tierCost=kind==PsychicAmplificationKind.OverpoweringMind?2*i:
                        kind==PsychicAmplificationKind.UndercastSurge?2*i:cost;
                    int tierStrength=kind==PsychicAmplificationKind.OverpoweringMind?i:
                        kind==PsychicAmplificationKind.UndercastSurge?i:strength;
                    int tierMinimum=kind==PsychicAmplificationKind.OverpoweringMind?(i==1?1:i==2?8:15):minimum;
                    var toggle=Mode(token+i,kind,tierCost,tierStrength,tierMinimum,effect);
                    granted.Add(toggle);
                }
                var components=new List<BlueprintComponent>{Facts(granted.ToArray())};
                if(linked)components.Add(new PsychicLinkedSpellEffects{name="DetectivePaid"+token,Kind=kind});
                var feature=Feature(token,title,text,components.ToArray());
                features.Add(feature);
            }

            Add("BiokineticHealing","Biokinetic Healing","Spend one phrenic point to heal 2 hit points per spell level when casting a transmutation spell.",
                PsychicAmplificationKind.BiokineticHealing,1,2,1);
            Add("ConjuredArmor","Conjured Armor","Spend one phrenic point to protect summoned creatures.",
                PsychicAmplificationKind.ConjuredArmor,1,0,1,armor,true);
            Add("DefensivePrognostication","Defensive Prognostication","Spend one or two phrenic points for an insight bonus to AC.",
                PsychicAmplificationKind.DefensivePrognostication,1,2,1,defense1,tiers:2);
            Add("FocusedForce","Focused Force","Spend one phrenic point to increase force damage dice by one step.",
                PsychicAmplificationKind.FocusedForce,1,0,1,null,true);
            Add("MindsEye","Mind's Eye","Spend two phrenic points for an insight bonus on ranged attack rolls.",
                PsychicAmplificationKind.MindsEye,2,0,1,null,true);
            Add("OverpoweringMind","Overpowering Mind","Spend phrenic points to increase the saving throw DC of mind-affecting spells.",
                PsychicAmplificationKind.OverpoweringMind,2,1,1,null,false,3);
            Add("WillOfTheDead","Will of the Dead","Spend two phrenic points to affect mindless undead with a mind-affecting spell.",
                PsychicAmplificationKind.WillOfTheDead,2,0,1);
            Add("OngoingDefense","Ongoing Defense","Spend one phrenic point to extend a mental barrier by one round.",
                PsychicAmplificationKind.OngoingDefense,1,0,1);
            Add("RelentlessCasting","Relentless Casting","Spend one phrenic point to roll spell resistance twice.",
                PsychicAmplificationKind.RelentlessCasting,1,0,1);
            Add("UndercastSurge","Undercast Surge","Spend phrenic points to cast a higher-circle family spell with a lower slot.",
                PsychicAmplificationKind.UndercastSurge,2,1,1,null,false,5);
            features.Add(Psychofeedback(f,cache,cls,pool));
            return features.ToArray();
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
            var pool=Pool(f,cls);
            var center=cache.Load(BlueprintGuid.Parse(PsychicClass.CenterAbilityId)) as BlueprintAbility;
            var casting=f.Feature(PsychicDetective0068Ids.CastingToken,"Psychic Spellcasting",
                "A psychic detective casts spells spontaneously from a private six-circle Intelligence spellbook.",Facts(center));
            var meddler=f.Feature(PsychicDetective0068Ids.MeddlerToken,"Psychic Meddler",
                "Add a bonus on saving throws against mind-affecting spells and spell-like abilities. The bonus rises at 2nd, 5th, 8th, 11th, 14th and 17th investigator level, to a maximum of +6.",
                new PsychicDetectiveMeddler0068{CharacterClass=Ref<BlueprintCharacterClassReference>(cls),MaximumBonus=6});
            var dabbler=f.Feature(PsychicDetective0068Ids.DabblerToken,"Phrenic Dabbler",
                "Gain a phrenic pool equal to half your investigator level and one basic phrenic amplification.",
                new AddAbilityResources{name="DetectivePhrenicPool",RestoreAmount=true,RestoreOnLevelUp=true});
            Set(dabbler.GetComponent<AddAbilityResources>(),"m_Resource",Ref<BlueprintAbilityResourceReference>(pool));

            var modes=new List<BlueprintActivatableAbility>();
            var amplificationFeatures=Amplifications(f,cache,cls,book,pool,modes);
            var selection=f.Add(new BlueprintFeatureSelection{Ranks=1,IsClassFeature=true},
                PsychicDetective0068Ids.AmplificationSelectionToken,"Basic Phrenic Amplification",
                "Choose one of the eleven basic phrenic amplifications.");
            Set(selection,"m_Features",amplificationFeatures.Select(Ref<BlueprintFeatureReference>).ToArray());
            Set(selection,"m_AllFeatures",amplificationFeatures.Select(Ref<BlueprintFeatureReference>).ToArray());
            PsychicAmplificationMenu.BuildDetective(f,modes,casting);

            var add=new[]{Entry(1,casting),Entry(2,meddler),Entry(3,dabbler,selection)};
            var remove=new[]{Entry(2,poison),Entry(3,talents),Entry(5,poison),Entry(8,poison),Entry(11,immunity)};
            var root=SpellBatch01447.Archetype(f,cls,PsychicDetective0068Ids.ArchetypeToken,"Psychic Detective",Description,add,remove);
            root.ChangeCasterType=true;root.IsArcaneCaster=false;root.IsDivineCaster=false;
            root.ReplaceClassSkills=true;
            root.ClassSkills=new[]{StatType.SkillStealth,StatType.SkillThievery,StatType.SkillKnowledgeArcana,
                StatType.SkillKnowledgeWorld,StatType.SkillLoreNature,StatType.SkillLoreReligion,
                StatType.SkillPerception,StatType.SkillPersuasion,StatType.SkillUseMagicDevice};
            Set(root,"m_ReplaceSpellbook",Ref<BlueprintSpellbookReference>(book));

            var expanded=f.Feature(PsychicDetective0068Ids.ExpandedPoolTalentToken,"Expanded Phrenic Pool",
                "Your phrenic pool increases by 2 points.",Need(cls,root,3),
                new IncreaseResourceAmount{name="DetectiveExpandedPool",Value=2});
            Set(expanded.GetComponent<IncreaseResourceAmount>(),"m_Resource",Ref<BlueprintAbilityResourceReference>(pool));
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
            var all=(BlueprintFeatureReference[])Get(talents,"m_AllFeatures")??Array.Empty<BlueprintFeatureReference>();
            foreach(var token in new[]{PsychicDetective0068Ids.ExpandedPoolTalentToken,PsychicDetective0068Ids.ExtraAmplificationTalentToken})
                if(all.Count(r=>r?.Guid==Id(token))!=1)throw new InvalidOperationException("0068 talent link missing or duplicated: "+token);
            var list=cache.Load(Id(PsychicDetective0068Ids.ListToken)) as BlueprintSpellList;
            if(list==null||!SpellListCompatibility.Canonical(list.SpellsByLevel))
                throw new InvalidOperationException("0068 canonical spell list missing.");
        }
    }
}
