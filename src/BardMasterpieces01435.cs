using System;
using System.Collections;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Spells;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Original CotW VersatilePerformance.cs masterpieces. The two private
    // performance leaves preserve Wrath's distinct Bard and Skald resources.
    internal static class BardMasterpieces01435
    {
        internal const string ClamorModule = "Bardic masterpiece: Clamor of Heavens";
        internal const string DanceModule = "Bardic masterpiece: Dance of 23 Steps";
        internal const string SymphonyModule = "Bardic masterpiece: Symphony of the Elysian Heart";
        internal const string ClamorToken = "ClamorOfHeavensMasterpiece", DanceToken = "DanceOf23StepsMasterpiece", SymphonyToken = "SymphonyOfElysianHeartMasterpiece";
        internal const string SelectionToken = "BardicMasterpieces01435Selection";
        internal const string BardClass = "772c83a25e2268e448e841dcd548235f", SkaldClass = "6afa347d804838b48bda16acb0573dc0";
        internal const string BardResource = "e190ba276831b5c4fa28737e5e49e6a6", SkaldResource = "4a2302c4ec2cfb042bba67d825babfec";
        internal const string BardResourceFact = "b92bfc201c6a79e49afd0b5cfbfc269f", SkaldResourceFact = "334cb75aa673d1d4bb279761c2ef5cf1";
        internal static readonly string[] RootTokens = { ClamorToken, DanceToken, SymphonyToken };
        internal const string PrestigeSelection="22df5ebca04e4c3bb72bd6a032df26bb", PrestigeCore="0901ea8f1cae465da272bab8fee863e4", BardSpellList="25a5013493bdcf74bb2424532214d0c8";
        internal const string KnownSourceToken="BardMasterpieceKnownSource01436";
        internal static readonly string[] MarkerTokens={ClamorToken+"KnownSpellMarker",DanceToken+"KnownSpellMarker",SymphonyToken+"KnownSpellMarker"};
        internal static readonly int[] MarkerLevels={5,2,3};
        internal static readonly BlueprintGuid[] RootIds=RootTokens.Select(Id).ToArray(), MarkerIds=MarkerTokens.Select(Id).ToArray();
        internal static readonly string[] PrestigeFeatures={"ea78acb4d56044c3a6da2e3ddd20006b","c73eb87ced4e42e882c9c73c793b8797","21606a268ea7453892e23ac549548efa","9185044273244e208f00845cf44b81c4","2ebc866b610a465583032c5006f55bc3"};
        internal static readonly string[] PrestigeAbilities={"ad42caf043154c088ebb1fb888f32f84","b2c149bdce2347539d759a74c642c9e3","df05fc9b9f394d26ad3a51e21ec6ac06","c897a7577c0a4e68b733b3c655c9eb79","292c89759fa14af383d5b7dbccf72b92"};
        internal static string LastOptionalDiagnostic;
        internal static readonly string[] OwnTokens = {
            SelectionToken, ClamorToken, ClamorToken+"Host", ClamorToken+"Area", ClamorToken+"Effect",
            ClamorToken+"Stunned", ClamorToken+"Staggered", ClamorToken+"Blind", ClamorToken+"Shaken",
            ClamorToken+"LevelProperty", ClamorToken+"HalfLevelProperty", ClamorToken+"BardToggle", ClamorToken+"SkaldToggle",
            DanceToken, DanceToken+"Effect", DanceToken+"BardToggle", DanceToken+"SkaldToggle",
            SymphonyToken, SymphonyToken+"Host", SymphonyToken+"Area", SymphonyToken+"Effect", SymphonyToken+"BardToggle", SymphonyToken+"SkaldToggle",
            KnownSourceToken,ClamorToken+"KnownSpellMarker",DanceToken+"KnownSpellMarker",SymphonyToken+"KnownSpellMarker"
        };
        const string ContainerText = "Learn one original CotW bardic masterpiece. Each masterpiece has its own Bard or Skald class-level requirement and uses your existing bardic performance or raging song rounds. It grants no performance resource. A Bard/Skald multiclass character can choose which existing pool to spend. The masterpieces share the native Bardic Performance group, including its normal exclusivity and Lingering Performance rules.";
        const string ClamorText = "As a standard action, perform a 30-foot audible masterpiece affecting evil creatures, including evil allies. Each affected creature makes a Will save (DC 10 + half your combined Bard and Skald levels + your Charisma modifier). Evil undead and outsiders are stunned on failure or staggered on success; other evil creatures are blinded on failure or shaken on success. Effects last while the creature hears this performance. This follows original CotW's implemented effects: Wrath has no deafened condition. Normal condition immunities apply. Staggered, blinded and shaken also respect their usual specific-effect immunities. Stun follows original CotW's special rules for this performance. Starting costs 3 performance rounds; each additional round costs 1.";
        const string DanceText = "As a free action, gain a +2 dodge bonus to AC while performing. The bonus and penalties increase by 1 at combined Bard/Skald levels 8, 12, 16 and 20. Take an equal penalty on melee attack rolls and combat maneuver checks; ranged attacks retain their normal bonus. Casting a spell requires the native difficult-casting concentration check (DC 15 + spell level). Other sources of difficult casting retain their normal rules. Costs 1 existing performance round per round. Native performance exclusivity and Lingering Performance apply.";
        const string SymphonyText = "As a standard action, perform a 30-foot audible masterpiece. You and allies who can hear it gain the native Freedom of Movement effects while the performance affects them. Leaving its area or becoming unable to hear ends that source's effect. Costs 1 existing bardic performance or raging song round per round. Native performance exclusivity and Lingering Performance apply.";
        sealed class Publication
        {
            internal BlueprintScriptableObject[] Assets;
            internal string[] Definitions;
            internal BlueprintFeature Root;
            internal BlueprintFeatureSelection Container;
            internal string ContainerDefinition;
        }
        static readonly ConditionalWeakTable<BlueprintsCache, Dictionary<string, Publication>> Published = new ConditionalWeakTable<BlueprintsCache, Dictionary<string, Publication>>();
        internal static void Register(BlueprintsCache cache) { RegisterClamor(cache); RegisterDance(cache); RegisterSymphony(cache); }
        internal static void Optional(BlueprintsCache cache) => Register(cache);
        internal static void RegisterClamor(BlueprintsCache cache) => RegisterOne(cache, 0);
        internal static void RegisterDance(BlueprintsCache cache) => RegisterOne(cache, 1);
        internal static void RegisterSymphony(BlueprintsCache cache) => RegisterOne(cache, 2);

        static PrerequisiteClassLevel Level(PsychicAssets f, string classId, int level) {
            var p = new PrerequisiteClassLevel { Level = level, Group = Prerequisite.GroupType.Any };
            Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(f.Native<BlueprintCharacterClass>(classId))); return p;
        }
        static BlueprintFeatureSelection Container(PsychicAssets f) {
            var old = f.Cache.Load(Id(SelectionToken));
            if (old != null) {
                if (!Published.TryGetValue(f.Cache, out var owners) || !owners.Values.Any(p => ReferenceEquals(p.Container,old) && Definition(p.Container)==p.ContainerDefinition))
                    throw new InvalidOperationException("01435 private masterpiece container has no valid publication receipt.");
                if (!(old is BlueprintFeatureSelection selection) || selection.name != "CotWWrathPsychic"+SelectionToken || selection.IgnorePrerequisites ||
                    selection.Groups == null || !selection.Groups.SequenceEqual(new[] { FeatureGroup.Feat }) || selection.ComponentsArray == null || selection.ComponentsArray.Any(c => c == null))
                    throw new InvalidOperationException("01435 masterpiece container identity changed.");
                return selection;
            }
            return f.Add(new BlueprintFeatureSelection { Ranks = 10, Groups = new[] { FeatureGroup.Feat }, IsClassFeature = false,
                HideInUI=true, HideInCharacterSheetAndLevelUp=true, ComponentsArray = new BlueprintComponent[] { Level(f, BardClass, 3), Level(f, SkaldClass, 3) } }, SelectionToken, "Bardic Masterpieces (CotW)", ContainerText);
        }
        internal static BlueprintFeatureSelection[] Pools(BlueprintsCache cache) => SelectionSnapshot(cache).Where(p=>new[]{"m_Features","m_AllFeatures"}.Any(n=>(Get(p,n) as BlueprintFeatureReference[]??Array.Empty<BlueprintFeatureReference>()).Any(r=>r!=null&&(r.Guid==Id(SelectionToken)||RootTokens.Any(t=>r.Guid==Id(t)))))).ToArray();
        internal static void AppendToPools(BlueprintsCache cache, BlueprintFeatureSelection container, BlueprintFeature leaf) {
            // Keep the old hidden identity for existing saves, never publish it
            // as another ordinary/Versatile Performance selection container.
            Set(container,"m_Features",Array.Empty<BlueprintFeatureReference>());Set(container,"m_AllFeatures",Array.Empty<BlueprintFeatureReference>());
            RetireLegacySelection(cache,container);
        }
        static BlueprintFeatureSelection[] SelectionSnapshot(BlueprintsCache cache){
            var gate=Get(cache,"m_Lock");var loaded=(IDictionary)Get(cache,"m_LoadedBlueprints");var field=AccessTools.Field(loaded.GetType().GetGenericArguments()[1],"Blueprint");object[] entries=null;
            for(int attempt=0;;attempt++)try{lock(gate){entries=new object[loaded.Count];loaded.Values.CopyTo(entries,0);}break;}catch(Exception e)when(attempt<2&&(e is InvalidOperationException||e is ArgumentException||e is IndexOutOfRangeException)){}
            return entries.Where(e=>e!=null).Select(e=>field.GetValue(e)).OfType<BlueprintFeatureSelection>().ToArray();
        }
        static void RetireLegacySelection(BlueprintsCache cache,BlueprintFeatureSelection container) {
            var snapshot=SelectionSnapshot(cache).Where(p=>!ReferenceEquals(p,container)).ToArray();
            bool ready;try{ready=PrestigeReady(cache);}catch{ready=false;}
            bool Keep(BlueprintFeatureSelection pool,BlueprintFeatureReference reference)=>reference==null||reference.Guid!=Id(SelectionToken)&&(ready&&pool.AssetGuid==BlueprintGuid.Parse(PrestigeSelection)||!RootTokens.Any(t=>reference.Guid==Id(t)));
            var changed=snapshot.Where(p=>new[]{"m_Features","m_AllFeatures"}.Any(n=>(Get(p,n) as BlueprintFeatureReference[]??Array.Empty<BlueprintFeatureReference>()).Any(r=>!Keep(p,r)))).ToArray();
            var before=changed.Select(p=>new[]{Get(p,"m_Features"),Get(p,"m_AllFeatures")}).ToArray();
            try{foreach(var pool in changed)foreach(string field in new[]{"m_Features","m_AllFeatures"}){var refs=Get(pool,field) as BlueprintFeatureReference[];if(refs!=null)Set(pool,field,refs.Where(r=>Keep(pool,r)).ToArray());}}
            catch{for(int i=0;i<changed.Length;i++){Set(changed[i],"m_Features",before[i][0]);Set(changed[i],"m_AllFeatures",before[i][1]);}throw;}
        }
        static void OptionalVersatile(BlueprintsCache cache, BlueprintFeatureSelection container) {
            try{ConnectPrestige(cache);LastOptionalDiagnostic=null;}
            catch(Exception e){LastOptionalDiagnostic="01436 optional PrestigePlus masterpiece integration isolated; cached performances preserved. "+e;ModEntry.LogSpellDiagnostic(LastOptionalDiagnostic);}
        }
        internal static bool PrestigeReady(BlueprintsCache cache) {
            var rawCore=cache.Load(BlueprintGuid.Parse(PrestigeCore));var rawPool=cache.Load(BlueprintGuid.Parse(PrestigeSelection));
            if(rawCore==null||rawPool==null)return false;
            if(!(rawCore is BlueprintFeature core)||!(rawPool is BlueprintFeatureSelection pool))throw new InvalidOperationException("01436 PrestigePlus masterpiece core/selection type changed.");
            if(core.GetType()!=typeof(BlueprintFeature)||core.name!="BardMasterpiece.BardMasterpieceCore"||pool.name!="BardMasterpiece.BardMasterpiece"||pool.IgnorePrerequisites||pool.ComponentsArray==null||
                !pool.GetComponents<PrerequisiteFeature>().Any(p=>p.Feature?.AssetGuid==core.AssetGuid))throw new InvalidOperationException("01436 PrestigePlus masterpiece core/selection identity changed.");
            string[] names={"TripleTime","TwistingSteel","StoneFace","RatQuadrille","PriestKing"};int[] circles={1,2,3,2,3};
            for(int i=0;i<names.Length;i++){
                var feature=cache.Load(BlueprintGuid.Parse(PrestigeFeatures[i])) as BlueprintFeature;var ability=cache.Load(BlueprintGuid.Parse(PrestigeAbilities[i])) as BlueprintAbility;
                if(feature==null||ability==null)return false;
                if(feature.GetType()!=typeof(BlueprintFeature)||feature.name!="Masterpiece."+names[i]||ability.name!="Masterpiece.Use"+names[i]||ability.Type!=AbilityType.Supernatural||
                    !pool.AllFeatures.Any(f=>f?.AssetGuid==feature.AssetGuid)||!feature.GetComponents<AddFacts>().Any(a=>a.Facts.Any(f=>f?.AssetGuid==ability.AssetGuid))||
                    !ability.GetComponents<SpellListComponent>().Any(c=>c.SpellList?.AssetGuid==BlueprintGuid.Parse(BardSpellList)&&c.SpellLevel==circles[i]))throw new InvalidOperationException("01436 PrestigePlus original masterpiece child graph changed: "+names[i]);
            }
            return true;
        }
        internal static void ConnectPrestige(BlueprintsCache cache) {
            if(!PrestigeReady(cache))return;
            var pool=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(PrestigeSelection));var list=cache.Load(BlueprintGuid.Parse(BardSpellList)) as BlueprintSpellList??throw new InvalidOperationException("01436 native Bard spell list missing.");
            var roots=RootTokens.Select(t=>cache.Load(Id(t)) as BlueprintFeature).ToArray();var markers=MarkerTokens.Select(t=>cache.Load(Id(t)) as BlueprintAbility).ToArray();
            var before=new[]{Get(pool,"m_Features"),Get(pool,"m_AllFeatures")};var rows=list.SpellsByLevel;
            if(rows==null||rows.Select((r,i)=>r!=null&&r.SpellLevel==i).Any(ok=>!ok)||MarkerLevels.Any(i=>i>=rows.Length))throw new InvalidOperationException("01436 Bard list lacks native indexed masterpiece rows.");
            var spellRefs=rows.Select(r=>Get(r,"m_Spells")).ToArray();var proxies=rows.Select(r=>Get(r,"m_SpellsProxy")).ToArray();var filtered=rows.Select(r=>Get(r,"m_SpellsFiltered")).ToArray();
            try{
                AppendPrestige(pool,roots.Where(r=>r!=null).ToArray());
                for(int i=0;i<markers.Length;i++)if(markers[i]!=null){var row=rows[MarkerLevels[i]];var refs=Get(row,"m_Spells") as List<BlueprintAbilityReference>??throw new InvalidOperationException("01436 Bard spell row references missing.");if(!refs.Any(r=>r?.Guid==markers[i].AssetGuid)){Set(row,"m_Spells",refs.Concat(new[]{Ref<BlueprintAbilityReference>(markers[i])}).ToList());Set(row,"m_SpellsProxy",null);Set(row,"m_SpellsFiltered",null);}}
            }catch{Set(pool,"m_Features",before[0]);Set(pool,"m_AllFeatures",before[1]);for(int i=0;i<rows.Length;i++){Set(rows[i],"m_Spells",spellRefs[i]);Set(rows[i],"m_SpellsProxy",proxies[i]);Set(rows[i],"m_SpellsFiltered",filtered[i]);}throw;}
        }
        internal static void AppendPrestige(BlueprintFeatureSelection pool,BlueprintFeature[] roots)=>T09CombatFeats.Append(new[]{pool},roots);
        static T ById<T>(string token)where T:BlueprintReferenceBase,new(){var r=new T();Set(r,"deserializedGuid",Id(token));return r;}
        static BlueprintFeature KnownSource(PsychicAssets f) {
            var old=f.Cache.Load(Id(KnownSourceToken));
            if(old!=null){if(!Published.TryGetValue(f.Cache,out var entries)||!entries.Values.Any(p=>p.Assets.Any(a=>ReferenceEquals(a,old))))throw new InvalidOperationException("01436 known-source bridge has no own receipt.");return old as BlueprintFeature??throw new InvalidOperationException("01436 known-source bridge identity changed.");}
            return f.Add(new BlueprintFeature{HideInUI=true,HideInCharacterSheetAndLevelUp=true,IsClassFeature=true,ComponentsArray=new BlueprintComponent[]{new MasterpieceKnownSource01436{Roots=RootTokens.Select(ById<BlueprintFeatureReference>).ToArray(),Markers=MarkerTokens.Select(ById<BlueprintAbilityReference>).ToArray()}}},KnownSourceToken,"Masterpiece known-spell sources","Internal source ownership for learned bardic masterpieces.");
        }
        static void Marker(PsychicAssets f,int index,BlueprintFeature root,BlueprintFeature ledger) {
            string title=index==0?"Clamor of the Heavens":index==1?"The Dance of 23 Steps":"Symphony of the Elysian Heart";string rules=index==0?ClamorText:index==1?DanceText:SymphonyText;
            var marker=f.Ability(MarkerTokens[index],title+" (Masterpiece)","Spend one Bard spell known of level "+MarkerLevels[index]+" to learn this masterpiece instead of a spell. You must meet its normal class-level requirements and have PrestigePlus's Bardic Masterpieces. Use the performance in the abilities tab; this choice cannot be cast or converted into a spell. Removing this known choice revokes only its own grant. Choosing the feat does not grant a free known choice.\n\n"+rules,AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Personal,
                new MasterpieceKnownMarker01436{Root=Ref<BlueprintFeatureReference>(root),Ledger=Ref<BlueprintFeatureReference>(ledger),Circle=MarkerLevels[index],MinimumClassLevel=new[]{10,4,7}[index]});
            Set(marker,"m_Icon",Get(root,"m_Icon"));marker.NotOffensive=true;
        }
        static string Definition(BlueprintScriptableObject bp) {
            string result = bp.GetType().FullName+"/"+bp.name+"/"+string.Join("|", (bp.ComponentsArray ?? Array.Empty<BlueprintComponent>()).Select(Migration01426.Definition));
            if(bp is BlueprintAbility marker)result+="/"+marker.Type+"/"+marker.Range+"/"+marker.ActionType+"/"+marker.NotOffensive;
            if (bp is BlueprintActivatableAbility a) result += "/"+a.Group+"/"+a.ActivationType+"/"+Get(a,"m_ActivateWithUnitCommand")+"/"+(Get(a,"m_Buff") as BlueprintBuffReference)?.Guid;
            if (bp is BlueprintAbilityAreaEffect area) result += "/"+area.Size.Value+"/"+area.Shape+"/"+area.AffectEnemies+"/"+area.AggroEnemies+"/"+area.SpellResistance;
            if (bp is BlueprintBuff b) result += "/"+b.Stacking+"/"+b.FxOnStart?.AssetId+"/"+b.FxOnRemove?.AssetId;
            if (bp is BlueprintFeature feature) result += "/"+feature.Ranks+"/"+feature.IsClassFeature+"/"+string.Join(",",feature.Groups??Array.Empty<FeatureGroup>());
            if (bp is BlueprintFeatureSelection selection) result += "/"+selection.IgnorePrerequisites+"/"+selection.Group+"/"+selection.Group2;
            return result;
        }
        static void RegisterOne(BlueprintsCache cache, int index) {
            string token = RootTokens[index]; var records = Published.GetOrCreateValue(cache);
            if (records.TryGetValue(token, out var publication)) {
                for (int i=0; i<publication.Assets.Length; i++) {
                    var asset=publication.Assets[i];
                    if (!ReferenceEquals(cache.Load(asset.AssetGuid), asset))
                        throw new InvalidOperationException("01435 published masterpiece graph identity changed: "+token+"; asset="+asset.name+" ["+asset.AssetGuid+"]");
                    var current=Definition(asset);
                    if(current!=publication.Definitions[i])
                        throw new InvalidOperationException("01435 published masterpiece graph changed: "+token+"; asset="+asset.name+" ["+asset.AssetGuid+"]; "+Migration01426.Difference(publication.Definitions[i],current));
                    if(asset.ComponentsArray.Any(c=>c==null||!ReferenceEquals(c.OwnerBlueprint,asset)))
                        throw new InvalidOperationException("01435 published masterpiece component ownership changed: "+token+"; asset="+asset.name+" ["+asset.AssetGuid+"]");
                }
                var containerDefinition=Definition(publication.Container);
                if(containerDefinition!=publication.ContainerDefinition)throw new InvalidOperationException("01435 published masterpiece container changed; "+Migration01426.Difference(publication.ContainerDefinition,containerDefinition));
                AppendTransactional(cache, publication.Container, publication.Root); OptionalVersatile(cache,publication.Container); return;
            }
            var f = new PsychicAssets(cache); var container = Container(f);var ledger=KnownSource(f);
            var classes = new[] { f.Native<BlueprintCharacterClass>(BardClass), f.Native<BlueprintCharacterClass>(SkaldClass) };
            // Read and validate the real native resource/grant graph. No new
            // resource or alteration of a native song is part of this module.
            foreach (var pair in new[] { new[]{BardResource, BardResourceFact}, new[]{SkaldResource, SkaldResourceFact} }) {
                var resource = f.Native<BlueprintAbilityResource>(pair[0]); var grant = f.Native<BlueprintFeature>(pair[1]);
                if (!grant.GetComponents<AddAbilityResources>().Any(c => c.Resource?.AssetGuid == resource.AssetGuid))
                    throw new InvalidOperationException("01435 native performance resource grant changed.");
            }
            string name = index == 0 ? "Clamor of the Heavens" : index == 1 ? "The Dance of 23 Steps" : "Symphony of the Elysian Heart";
            string text = index == 0 ? ClamorText : index == 1 ? DanceText : SymphonyText;
            var effect = index == 0 ? Clamor(f, classes) : index == 1 ? Dance(f, classes) : Symphony(f);
            var toggles = new[] { Toggle(f, token, name, text, effect, false, index), Toggle(f, token, name, text, effect, true, index) };
            var root = f.Feature(token, name, text, Level(f, BardClass, index == 0 ? 10 : index == 1 ? 4 : 7), Level(f, SkaldClass, index == 0 ? 10 : index == 1 ? 4 : 7));
            root.Groups = Array.Empty<FeatureGroup>(); root.ReapplyOnLevelUp = true;
            var no = new PrerequisiteNoFeature(); Set(no,"m_Feature",Ref<BlueprintFeatureReference>(root));
            root.ComponentsArray = root.ComponentsArray.Concat(new BlueprintComponent[] { no, Grant(f, BardResourceFact, toggles[0]), Grant(f, SkaldResourceFact, toggles[1]) }).ToArray();
            Set(root,"m_Icon",Get(effect,"m_Icon")); Set(container,"m_Icon",Get(effect,"m_Icon"));Marker(f,index,root,ledger);
            Commit(f, container, root, records);
            OptionalVersatile(cache,container);
        }
        static AddFeatureIfHasFact Grant(PsychicAssets f, string checkedFact, BlueprintActivatableAbility ability) {
            var grant = new AddFeatureIfHasFact(); Set(grant,"m_CheckedFact",Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>(checkedFact)));
            Set(grant,"m_Feature",Ref<BlueprintUnitFactReference>(ability)); return grant;
        }
        static void AppendTransactional(BlueprintsCache cache, BlueprintFeatureSelection container, BlueprintFeature root) {
            var pools = Pools(cache).Concat(new[]{container}).Distinct().ToArray();
            var before = pools.Select(p=>new[]{Get(p,"m_Features"),Get(p,"m_AllFeatures")}).ToArray();
            try { AppendToPools(cache,container,root); }
            catch { for(int i=0;i<pools.Length;i++){Set(pools[i],"m_Features",before[i][0]);Set(pools[i],"m_AllFeatures",before[i][1]);} throw; }
        }
        static void Commit(PsychicAssets f, BlueprintFeatureSelection container, BlueprintFeature root, Dictionary<string, Publication> records) {
            var staged = f.Assets.Values.ToArray(); foreach(var bp in staged) { PsychicRepairComponents.UniqueNames(bp); if(f.Cache.Load(bp.AssetGuid)!=null)throw new InvalidOperationException("01435 masterpiece identity collision: "+bp.name); }
            var pools = Pools(f.Cache).Concat(new[]{container}).Distinct().ToArray(); var before=pools.Select(p=>new[]{Get(p,"m_Features"),Get(p,"m_AllFeatures")}).ToArray();
            try {
                f.Enable(); foreach(var bp in staged)f.Cache.AddCachedBlueprint(bp.AssetGuid,bp);
                AppendToPools(f.Cache,container,root);
                records.Add(RootTokens.First(t=>root.AssetGuid==Id(t)),new Publication{Assets=staged,Definitions=staged.Select(Definition).ToArray(),Root=root,Container=container,ContainerDefinition=Definition(container)});
            } catch {
                for(int i=0;i<pools.Length;i++){Set(pools[i],"m_Features",before[i][0]);Set(pools[i],"m_AllFeatures",before[i][1]);}
                foreach(var bp in staged)if(ReferenceEquals(f.Cache.Load(bp.AssetGuid),bp))f.Cache.RemoveCachedBlueprint(bp.AssetGuid);
                throw;
            }
        }
        static BlueprintActivatableAbility Toggle(PsychicAssets f,string token,string name,string text,BlueprintBuff host,bool skald,int kind) {
            var resource=f.Native<BlueprintAbilityResource>(skald?SkaldResource:BardResource);
            ActivatableAbilityResourceLogic Cost(ActivatableAbilityResourceLogic.ResourceSpendType type) {var c=new ActivatableAbilityResourceLogic{SpendType=type};Set(c,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(resource));return c;}
            var components=new List<BlueprintComponent>{Cost(ActivatableAbilityResourceLogic.ResourceSpendType.NewRound),new MasterpiecePerformanceRestriction01435{Resource=Ref<BlueprintAbilityResourceReference>(resource),Audible=kind!=1,InitialCost=kind==0?3:1}};
            if(kind==0){components.Add(Cost(ActivatableAbilityResourceLogic.ResourceSpendType.Start));components.Add(Cost(ActivatableAbilityResourceLogic.ResourceSpendType.Start));}
            var toggle=f.Add(new BlueprintActivatableAbility { Group=ActivatableAbilityGroup.BardicPerformance,ActivationType=kind==1?AbilityActivationType.Immediately:AbilityActivationType.WithUnitCommand,
                DeactivateIfCombatEnded=true,DeactivateIfOwnerDisabled=true,DeactivateIfOwnerUnconscious=true,ComponentsArray=components.ToArray()},token+(skald?"SkaldToggle":"BardToggle"),name+(skald?" (Raging Song)":" (Bardic Performance)"),text);
            Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(host));Set(toggle,"m_ActivateWithUnitCommand",kind==1?UnitCommand.CommandType.Free:UnitCommand.CommandType.Standard);Set(toggle,"m_Icon",Get(host,"m_Icon"));return toggle;
        }
        static BlueprintBuff AreaHost(PsychicAssets f,string token,string name,string text,BlueprintBuff effect,bool ally,object icon) {
            var conditions=new List<Condition>{new AudibleAbilityTargetCondition()};
            if(ally)conditions.Add(new ContextConditionIsAlly());else conditions.Add(new ContextConditionAlignment{Alignment=AlignmentComponent.Evil});
            var owned=new AbilityAreaEffectBuff{CheckConditionEveryRound=true,Condition=new ConditionsChecker{Operation=Operation.And,Conditions=conditions.ToArray()}};Set(owned,"m_Buff",Ref<BlueprintBuffReference>(effect));
            var area=f.Add(new BlueprintAbilityAreaEffect{Size=new Feet(30),Shape=AreaEffectShape.Cylinder,AffectEnemies=!ally,AggroEnemies=!ally,SpellResistance=false,
                Fx=new PrefabLink{AssetId=ally?"79665f3d500fdf44083feccf4cbfc00a":"dfc59904273f7ee49ab00e5278d86e16"},ComponentsArray=new BlueprintComponent[]{owned}},token+"Area",name,text);SetEnum(area,"m_TargetType","Any");
            var add=new AddAreaEffect();Set(add,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));
            var host=f.Buff(token+"Host",name,text,add);host.Stacking=StackingType.Stack;host.FxOnStart=new PrefabLink{AssetId="9353083f430e5a44a8e9d6e26faec248"};Set(host,"m_Icon",icon);return host;
        }
        static BlueprintBuff Clamor(PsychicAssets f,BlueprintCharacterClass[] classes) {
            var statusIds=new[]{"09d39b38bb7c6014394b6daced9bacd3","df3950af5a783bd4d91ab73eb8fa0fd3","0ec36e7596a4928489d2049e1e1c76a7","25ec6cb6ab1845c48a95f9c20b034220"};
            var labels=new[]{"Stunned","Staggered","Blind","Shaken"};var statuses=new List<BlueprintBuff>();
            for(int i=0;i<4;i++) {
                var b=f.Clone(f.Native<BlueprintBuff>(statusIds[i]),ClamorToken+labels[i],"Clamor of the Heavens: "+labels[i],ClamorText);
                // This private exception is authored by original CotW for the
                // undead-target branch; native condition immunity still decides
                // whether Stunned can actually take effect. No global immunity
                // or native buff is changed.
                if(i==0)b.ComponentsArray=b.ComponentsArray.Where(c=>!(c is SpellDescriptorComponent)).ToArray();
                b.Stacking=StackingType.Stack;statuses.Add(b);
            }
            GameAction Child(int status){
                var a=Apply(statuses[status],0,fromSpell:false);a.Permanent=true;a.AsChild=true;a.IsNotDispelable=true;
                if(status==0)return a; // Original CotW's private stun exception.
                // A private stacked child retains source ownership, but its new
                // identity must not evade an immunity to the original status.
                return new Conditional{ConditionsChecker=new ConditionsChecker{Conditions=new Condition[]{new MasterpieceNativeStatusAllowed01435{NativeBuff=Ref<BlueprintBuffReference>(f.Native<BlueprintBuff>(statusIds[status]))}}},IfTrue=Actions(a),IfFalse=Actions()};
            }
            var special=new Conditional{ConditionsChecker=new ConditionsChecker{Operation=Operation.Or,Conditions=new Condition[]{Has(f.Native<BlueprintFeature>("734a29b693e9ec346ba2951b27987e33")),Has(f.Native<BlueprintFeature>("9054d3988d491d944ac144e27b6bc318"))}},IfTrue=Actions(Saved(new GameAction[]{Child(1)},new GameAction[]{Child(0)})),IfFalse=Actions(Saved(new GameAction[]{Child(3)},new GameAction[]{Child(2)}))};
            var save=new ContextActionSavingThrow{Type=SavingThrowType.Will,Actions=Actions(special)};
            var effect=f.Buff(ClamorToken+"Effect","Clamor of the Heavens",ClamorText,Inquisitions01426.Parameters(f,ClamorToken,classes,StatType.Charisma).Concat(new BlueprintComponent[]{new AddFactContextActions{Activated=Actions(save),Deactivated=Actions(),NewRound=Actions()}}).ToArray());effect.Stacking=StackingType.Stack;
            var icon=Get(f.Native<BlueprintAbility>("574cf074e8b65e84d9b69a8c6f1af27b"),"m_Icon");Set(effect,"m_Icon",icon);
            return AreaHost(f,ClamorToken,"Clamor of the Heavens",ClamorText,effect,false,icon);
        }
        static ContextConditionHasFact Has(BlueprintFeature feature){var c=new ContextConditionHasFact();Set(c,"m_Fact",Ref<BlueprintUnitFactReference>(feature));return c;}
        static BlueprintBuff Dance(PsychicAssets f,BlueprintCharacterClass[] classes) {
            var rank=Inquisitions01426.Scaling(classes,AbilityRankType.Default,false);Set(rank,"m_Progression",ContextRankProgression.OnePlusDivStep);Set(rank,"m_StepLevel",4);Set(rank,"m_UseMin",true);Set(rank,"m_Min",2);
            var buff=f.Buff(DanceToken+"Effect","The Dance of 23 Steps",DanceText,rank,
                new AddContextStatBonus{Stat=StatType.AC,Descriptor=ModifierDescriptor.Dodge,Multiplier=1,Value=Inquisitions01426.Rank()},
                new AttackTypeAttackBonus{Type=WeaponRangeType.Melee,AttackBonus=-1,Descriptor=ModifierDescriptor.Penalty,Value=Inquisitions01426.Rank()},
                new AddContextStatBonus{Stat=StatType.AdditionalCMB,Descriptor=ModifierDescriptor.Penalty,Multiplier=-1,Value=Inquisitions01426.Rank()},
                new AddCondition{Condition=UnitCondition.SpellCastingIsDifficult});
            buff.Stacking=StackingType.Replace;buff.FxOnStart=new PrefabLink{AssetId="91ef30ab58fa0d3449d4d2ccc20cb0f8"};Set(buff,"m_Icon",PsychicOriginalIcons.Load("IrresistibleDance"));return buff;
        }
        static BlueprintBuff Symphony(PsychicAssets f) {
            var buff=f.Clone(f.Native<BlueprintBuff>("1533e782fca42b84ea370fc1dcbf4fc1"),SymphonyToken+"Effect","Symphony of the Elysian Heart",SymphonyText);buff.Stacking=StackingType.Stack;
            return AreaHost(f,SymphonyToken,"Symphony of the Elysian Heart",SymphonyText,buff,true,Get(buff,"m_Icon"));
        }
    }
    [TypeId("af25108ff27c473e9fca036000000001")]
    public sealed class MasterpieceKnownMarker01436 : BlueprintComponent,IAbilityRestriction,IAbilityVisibilityProvider
    {
        public BlueprintFeatureReference Root,Ledger;
        public int Circle,MinimumClassLevel;
        public bool IsAbilityRestrictionPassed(AbilityData ability)=>false;
        public string GetAbilityRestrictionUIText()=>"This is a masterpiece learning choice. Use its performance from the abilities tab.";
        public bool IsAbilityVisible(AbilityData ability)=>false;
        internal static MasterpieceKnownMarker01436 For(BlueprintAbility ability){
            if(ability==null)return null;int i=Array.IndexOf(BardMasterpieces01435.MarkerIds,ability.AssetGuid);if(i<0)return null;
            var c=ability.GetComponent<MasterpieceKnownMarker01436>();return c?.Root?.Guid==BardMasterpieces01435.RootIds[i]&&c.Circle==BardMasterpieces01435.MarkerLevels[i]&&c.Ledger?.Guid==Id(BardMasterpieces01435.KnownSourceToken)?c:null;
        }
        internal static bool BardBook(Spellbook book)=>book?.Owner!=null&&book.Blueprint?.CharacterClass?.AssetGuid.ToString()==BardMasterpieces01435.BardClass&&book.Blueprint.Spontaneous;
        internal bool Classes(UnitEntityData owner)=>owner!=null&&owner.Progression.Classes.Any(c=>c.Level>0&&c.CharacterClass.AssetGuid.ToString()==BardMasterpieces01435.BardClass)&&
            owner.Progression.Classes.Any(c=>c.Level>=MinimumClassLevel&&(c.CharacterClass.AssetGuid.ToString()==BardMasterpieces01435.BardClass||c.CharacterClass.AssetGuid.ToString()==BardMasterpieces01435.SkaldClass));
        bool LearningContext(Spellbook book,UnitDescriptor owner,int level){
            if(!BardBook(book)||book.Owner!=owner||level!=Circle||!Classes(owner.Unit)||Root?.Get()==null)return false;
            var core=ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(BlueprintGuid.Parse(BardMasterpieces01435.PrestigeCore));
            return core!=null&&owner.HasFact(core);
        }
        internal bool CanLearn(Spellbook book,UnitDescriptor owner,int level)=>LearningContext(book,owner,level)&&Root.Get().MeetsPrerequisites(null,owner,null,false,false);
        internal bool CanKeepCurrentSelection(Spellbook book,UnitDescriptor owner,int level){
            if(!LearningContext(book,owner,level)||!book.GetKnownSpells(Circle).Any(a=>ReferenceEquals(For(a.Blueprint),this)&&!a.IsTemporary&&a.MetamagicData==null&&a.ConvertedFrom==null)||!MasterpieceKnownSource01436.Owns(owner,Root.Get()))return false;
            // A selected native preview has already received this rank-one
            // feature. Preserve that exact current choice while retaining every
            // other native prerequisite; never relax a fresh known-spell slot.
            var prerequisites=Root.Get().GetComponents<Prerequisite>().Where(p=>!(p is PrerequisiteNoFeature no&&no.Feature?.AssetGuid==Root.Guid)).ToArray();
            return prerequisites.Where(p=>p.Group==Prerequisite.GroupType.All).All(p=>p.Check(null,owner,null))&&
                (!prerequisites.Any(p=>p.Group!=Prerequisite.GroupType.All)||prerequisites.Where(p=>p.Group!=Prerequisite.GroupType.All).Any(p=>p.Check(null,owner,null)));
        }
    }
    public sealed class MasterpieceKnownState01436
    {
        [JsonProperty]public EntityFactRef<Feature>[] Owned=new EntityFactRef<Feature>[3];
    }
    [TypeId("af25108ff27c473e9fca036000000002")]
    public sealed class MasterpieceKnownSource01436 : UnitFactComponentDelegate<MasterpieceKnownState01436>,IUnitLostFactHandler,IOwnerGainLevelHandler,ILevelUpCompleteUIHandler
    {
        public BlueprintFeatureReference[] Roots=Array.Empty<BlueprintFeatureReference>();
        public BlueprintAbilityReference[] Markers=Array.Empty<BlueprintAbilityReference>();
        sealed class Guard{internal bool Changing;internal Feature Creating;}
        static readonly ConditionalWeakTable<UnitEntityData,Guard> Guards=new ConditionalWeakTable<UnitEntityData,Guard>();
        protected override void OnActivate()=>Refresh();
        // Manager TurnOff/TurnOn suspends existing facts. Do not mutate its
        // enumerated collection, refill resources or create duplicate grants.
        protected override void OnApplyPostLoadFixes()=>Refresh();
        protected override void OnDeactivate(){if(!Fact.IsReapplying)Refresh(null,true);}
        public void HandleUnitLostFact(EntityFact fact){if(fact?.Blueprint!=null&&Roots.Any(r=>r?.Guid==fact.Blueprint.AssetGuid))Refresh();}
        public void HandleUnitGainLevel()=>Refresh();
        public void HandleLevelUpComplete(UnitEntityData unit,bool isChargen){if(unit==Owner)Refresh();}
        Feature Resolve(int i)=>Owner.Progression.Features.Enumerable.FirstOrDefault(f=>!f.IsDisposed&&f.UniqueId==Data.Owned[i].FactId&&f.Blueprint.AssetGuid==Roots[i].Guid);
        internal void Refresh(Spellbook candidate=null,bool removing=false){
            if(Owner==null)return;var guard=Guards.GetOrCreateValue(Owner);if(guard.Changing)return;guard.Changing=true;
            try{
                if(Data.Owned==null||Data.Owned.Length!=3)Data.Owned=new EntityFactRef<Feature>[3];
                var books=Owner.Spellbooks.Where(MasterpieceKnownMarker01436.BardBook).ToList();if(candidate!=null&&candidate.Owner==Owner.Descriptor&&MasterpieceKnownMarker01436.BardBook(candidate)&&!books.Contains(candidate))books.Add(candidate);
                for(int i=0;i<Math.Min(3,Math.Min(Roots.Length,Markers.Length));i++){
                    var root=Roots[i]?.Get();var marker=Markers[i]?.Get();var descriptor=marker?.GetComponent<MasterpieceKnownMarker01436>();if(root==null||descriptor==null)continue;
                    bool known=!removing&&descriptor.Classes(Owner)&&books.Any(b=>b.GetKnownSpells(descriptor.Circle).Any(a=>a.Blueprint==marker&&!a.IsTemporary&&a.MetamagicData==null&&a.ConvertedFrom==null));
                    var owned=Resolve(i);if(!known){Data.Owned[i]=default;if(owned?.Manager!=null)Owner.Facts.Remove(owned);continue;}
                    var current=Owner.Progression.Features.Enumerable.FirstOrDefault(f=>!f.IsDisposed&&f.Blueprint==root);
                    if(current==null){var incoming=new Feature(root,Owner.Descriptor);guard.Creating=incoming;try{current=Owner.Facts.Add(incoming);}finally{guard.Creating=null;}Data.Owned[i]=current;}
                    else if(!ReferenceEquals(current,owned))Data.Owned[i]=default;
                }
            }finally{guard.Changing=false;}
        }
        internal static void Update(UnitDescriptor owner,Spellbook candidate=null,BlueprintFeature ledger=null){
            if(owner?.Unit==null)return;
            var fact=owner.Progression.Features.Enumerable.FirstOrDefault(f=>!f.IsDisposed&&f.Blueprint.AssetGuid==Id(BardMasterpieces01435.KnownSourceToken));
            if(fact==null&&ledger!=null&&candidate!=null&&MasterpieceKnownMarker01436.BardBook(candidate)&&candidate.GetAllKnownSpells().Any(a=>!a.IsTemporary&&a.MetamagicData==null&&a.ConvertedFrom==null&&MasterpieceKnownMarker01436.For(a.Blueprint)!=null))fact=owner.Unit.Facts.Add(new Feature(ledger,owner));
            if(fact==null)return;foreach(var runtime in fact.Components.Where(r=>r.SourceBlueprintComponent is MasterpieceKnownSource01436))using(runtime.RequestEventContext())((MasterpieceKnownSource01436)runtime.SourceBlueprintComponent).Refresh(candidate);
        }
        internal static bool Owns(UnitDescriptor owner,BlueprintFeature root){
            var fact=owner?.Progression.Features.Enumerable.FirstOrDefault(f=>!f.IsDisposed&&f.Blueprint.AssetGuid==Id(BardMasterpieces01435.KnownSourceToken));if(fact==null||root==null)return false;
            foreach(var runtime in fact.Components.Where(r=>r.SourceBlueprintComponent is MasterpieceKnownSource01436))using(runtime.RequestEventContext()){
                var source=(MasterpieceKnownSource01436)runtime.SourceBlueprintComponent;int index=Array.FindIndex(source.Roots,r=>r?.Guid==root.AssetGuid);
                if(index>=0&&source.Data.Owned!=null&&source.Data.Owned.Length>index&&source.Resolve(index)!=null)return true;
            }
            return false;
        }
        internal static void HandOff(Feature incoming,Feature existing){
            if(incoming==null||existing==null||ReferenceEquals(incoming,existing)||Array.IndexOf(BardMasterpieces01435.RootIds,existing.Blueprint.AssetGuid)<0||incoming.Blueprint.AssetGuid!=existing.Blueprint.AssetGuid)return;
            var owner=existing.Owner?.Unit;if(owner==null||!Guards.TryGetValue(owner,out var guard)||ReferenceEquals(incoming,guard.Creating))return;
            var ledger=owner.Progression.Features.Enumerable.FirstOrDefault(f=>!f.IsDisposed&&f.Blueprint.AssetGuid==Id(BardMasterpieces01435.KnownSourceToken));if(ledger==null)return;
            foreach(var runtime in ledger.Components.Where(r=>r.SourceBlueprintComponent is MasterpieceKnownSource01436))using(runtime.RequestEventContext()){
                var c=(MasterpieceKnownSource01436)runtime.SourceBlueprintComponent;int index=Array.FindIndex(c.Roots,r=>r.Guid==existing.Blueprint.AssetGuid);if(index<0||c.Data.Owned==null||c.Data.Owned.Length<=index||!ReferenceEquals(c.Resolve(index),existing))continue;
                // Native rank-one merging has accepted a genuine other source.
                // Transfer ownership instead of deleting it with our known spell.
                existing.SetSameSource(incoming);existing.IsTemporary=incoming.IsTemporary;existing.IgnorePrerequisites=incoming.IgnorePrerequisites;
                foreach(string field in new[]{"<SourceFact>k__BackingField","<SourceItem>k__BackingField","<SourceAbility>k__BackingField","<SourceCutscene>k__BackingField"})AccessTools.Field(typeof(EntityFact),field).SetValue(existing,AccessTools.Field(typeof(EntityFact),field).GetValue(incoming));
                AccessTools.Field(typeof(Feature),"m_Context").SetValue(existing,incoming.Context);c.Data.Owned[index]=default;
            }
        }
    }
    [HarmonyPatch(typeof(Spellbook),nameof(Spellbook.AddKnown))]
    internal static class MasterpieceLearn01436 {
        static bool Prefix(Spellbook __instance,int __0,BlueprintAbility __1,ref AbilityData __result){
            var marker=MasterpieceKnownMarker01436.For(__1);if(marker==null||!MasterpieceKnownMarker01436.BardBook(__instance)||__0!=marker.Circle)return true;
            var known=__instance.GetKnownSpells(__0).FirstOrDefault(a=>a.Blueprint==__1&&!a.IsTemporary&&a.MetamagicData==null&&a.ConvertedFrom==null);if(known==null)return true;
            __result=known;return false;
        }
        static void Postfix(Spellbook __instance,BlueprintAbility __1){var marker=MasterpieceKnownMarker01436.For(__1);if(marker!=null&&MasterpieceKnownMarker01436.BardBook(__instance))MasterpieceKnownSource01436.Update(__instance.Owner,__instance,marker.Ledger?.Get());}
    }
    [HarmonyPatch(typeof(Spellbook),nameof(Spellbook.RemoveSpell))]
    internal static class MasterpieceForget01436 {
        static void Postfix(Spellbook __instance,BlueprintAbility __0){if(MasterpieceKnownMarker01436.For(__0)!=null&&MasterpieceKnownMarker01436.BardBook(__instance))MasterpieceKnownSource01436.Update(__instance.Owner,__instance);}
    }
    [HarmonyPatch(typeof(Spellbook),nameof(Spellbook.PostLoad))]
    internal static class MasterpieceBookLoad01436 {
        static void Postfix(Spellbook __instance){if(!MasterpieceKnownMarker01436.BardBook(__instance))return;var marker=__instance.GetAllKnownSpells().Select(a=>MasterpieceKnownMarker01436.For(a.Blueprint)).FirstOrDefault(m=>m!=null);MasterpieceKnownSource01436.Update(__instance.Owner,__instance,marker?.Ledger?.Get());}
    }
    [HarmonyPatch(typeof(UnitDescriptor),nameof(UnitDescriptor.DeleteSpellbook),new[]{typeof(BlueprintSpellbook)})]
    internal static class MasterpieceBookRemoved01436 {
        static void Postfix(UnitDescriptor __instance,BlueprintSpellbook __0){if(__0?.CharacterClass?.AssetGuid.ToString()==BardMasterpieces01435.BardClass)MasterpieceKnownSource01436.Update(__instance);}
    }
    [HarmonyPatch(typeof(FeatureCollection),"PrepareFactForAttach",new[]{typeof(Feature)})]
    internal static class MasterpieceOtherSource01436 {
        static void Postfix(Feature __0,Feature __result)=>MasterpieceKnownSource01436.HandOff(__0,__result);
    }
    [HarmonyPatch(typeof(SelectSpell),nameof(SelectSpell.Check))]
    internal static class MasterpieceSelect01436 {
        static void Postfix(SelectSpell __instance,UnitDescriptor __1,ref bool __result){var marker=MasterpieceKnownMarker01436.For(__instance.Spell);if(__result&&marker!=null)__result=marker.CanLearn(__1.GetSpellbook(__instance.Spellbook),__1,__instance.SpellLevel);}
    }
    [HarmonyPatch(typeof(CharGenSpellsPhaseVM),"CreateSpellVMsCollection")]
    internal static class MasterpieceChoiceList01436 {
        static void Postfix(Spellbook ___PreviewSpellbook,BlueprintAbility[] ___MechanicSelectedSpells,ref List<CharGenSpellSelectorItemVM> __result){
            if(__result==null)return;
            __result.RemoveAll(v=>{
                var marker=MasterpieceKnownMarker01436.For(v.Spell);if(marker==null)return false;
                bool keep=MasterpieceKnownMarker01436.BardBook(___PreviewSpellbook)&&(marker.CanLearn(___PreviewSpellbook,___PreviewSpellbook.Owner,v.Level)||
                    (___MechanicSelectedSpells?.Contains(v.Spell)??false)&&marker.CanKeepCurrentSelection(___PreviewSpellbook,___PreviewSpellbook.Owner,v.Level));
                if(!keep)v.Dispose();return !keep;
            });
        }
    }
    [TypeId("5d09caef7f1d4aacaac6c03514003502")]
    public sealed class MasterpieceNativeStatusAllowed01435 : ContextCondition
    {
        public BlueprintBuffReference NativeBuff;
        protected override string GetConditionCaption() => "Preserve native status-specific immunity";
        protected override bool CheckCondition() {
            var target=Target.Unit;var original=NativeBuff?.Get();
            if(target==null||original==null)return false;
            // Only an attempted Clamor status visits this one target. Reuse the
            // actual attached native handler so its caster-fact and alignment
            // checks stay native. Do not trigger the probe rule: its OnTrigger
            // can change story-mode immunity state even though no buff is added.
            var probe=new RuleCanApplyBuff(target,original,Context,null){Reason=Context};
            foreach(var fact in target.Facts.List.Where(f=>f.IsActive&&f.IsTurnedOn&&(!(f is Kingmaker.UnitLogic.Buffs.Buff b)||!b.IsSuppressed)))
                foreach(var runtime in fact.Components.Where(r=>r.IsActive&&r.IsTurnedOn&&!r.IsDisposed&&ReferenceEquals(r.Owner,target)&&r.SourceBlueprintComponent is SpecificBuffImmunity))
                    using(runtime.RequestEventContext())((SpecificBuffImmunity)runtime.SourceBlueprintComponent).OnEventAboutToTrigger(probe);
            return !probe.Immunity;
        }
    }
    [TypeId("20a045e20d104bcd916a9345d1c03501")]
    public sealed class MasterpiecePerformanceRestriction01435 : ActivatableAbilityRestriction
    {
        public BlueprintAbilityResourceReference Resource;
        public int InitialCost=1;
        public bool Audible;
        protected override bool IsAvailable() => Owner?.Resources != null && Resource?.Get()!=null &&
            Owner.Resources.HasEnoughResource(Resource.Get(),Fact.IsStarted?1:InitialCost) && (!Audible || AudibleAbilityRestriction.Audible(Owner));
    }
}
