// Original CotW Classes/Psychic.cs createMagaambyanTelepath (MIT).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class MagaambyanTelepath01447
    {
        internal const string ModuleName="Psychic archetype: Magaambyan Telepath",RootToken="MagaambyanTelepathArchetype01447",PlantId="706e61781d692a042b35941f14bc41c5";
        internal const string NatureText="The Magaambyan telepath’s mind-affecting spells can affect even plant creatures. When she casts a mind-affecting spell, the Magaambyan telepath can spend 2 points from her phrenic pool to overcome a plant creature’s immunity to mind-affecting effects for the purposes of that spell. This ability functions even on mindless plant creatures.\nAt 11th-level she can treat plant creatures as if they understood her language for purpose of language-dependent spells or abilities.";
        internal const string DisciplineText="Each psychic accesses and improves her mental powers through a particular method, such as rigorous study or attaining a particular mental state.\nThis is called her psychic discipline. She gains additional spells known based on her selected discipline. The choice of discipline must be made at 1st level; once made, it can’t be changed. Each psychic discipline gives the psychic a number of discipline powers (at 1st, 5th, and 13th levels), and grants her additional spells known. In addition, the discipline determines which ability score the psychic uses for her phrenic pool and phrenic amplifications abilities. The DC of a saving throw against a psychic discipline ability equals 10 + 1/2 the psychic’s level + the psychic’s Intelligence modifier.\nAt 1st level, a psychic learns an additional spell determined by her discipline. She learns another additional spell at 4th level and every 2 levels thereafter, until learning the final one at 18th level. These spells are in addition to the number of spells given. Spells learned from a discipline can’t be exchanged for different spells at higher levels.";
        internal const string PrimalText="A Magaambyan telepath adds one 1st-level spell from the druid spell list to the psychic spell list and her spells known. Each time a Magaambyan telepath gains the ability to cast a new level of spell, she can add one spell of that level from the druid spell list to both the psychic spell list and her spells known. She casts these spells as psychic spells, and once the spells are selected these choices cannot be changed.\nA Magaambyan telepath does not gain discipline spells.";
        static readonly Dictionary<BlueprintGuid,string> DisciplineDescriptions=new Dictionary<BlueprintGuid,string>{
            {Id("FerocityDiscipline"),"You are in touch with the most primal part of your subconscious. Your training allows you to unlock animalistic senses and powers.\nDiscipline Powers: Through your psychic training, you can unleash the most animalistic portions of your subconscious, enhancing your senses and physical capabilities.\nPhrenic Pool Ability: Wisdom"},
            {Id("FaithDiscipline"),"Your belief in a higher power fuels your psychic abilities. Whether your mental abilities truly come to you as a divine gift or are simply enhanced by the power of your belief, none can say. In many ways, you resemble a divine caster, and prayers often factor into your casting of psychic spells.\nDiscipline Powers: Your powers serve to protect or cure you and your allies.\nPhrenic Pool Ability: Wisdom"},
            {Id("PageantryDiscipline"),"The act of ritual, no matter how ostentatious, can be a route to mental strength. By engaging in esoteric and intricate routines, you have unlocked potent psychic power.\nPhrenic Pool Ability: Charisma"},
            {Id("PainDiscipline01458"),PsychicTexts01458.Description("Pain")+"\nPhrenic Pool Ability: Charisma"},
            {Id("PsychedeliaDiscipline01458"),PsychicTexts01458.Description("Psychedelia")+"\nPhrenic Pool Ability: Wisdom"},
            {Id("AbominationDiscipline01458"),PsychicTexts01458.Description("Abomination")+"\nPhrenic Pool Ability: Charisma"}
        };
        internal static readonly HashSet<BlueprintGuid> PlantImmunities=new HashSet<BlueprintGuid>();
        // Execution-only; never selectable. Bit29 is our Logical Spell and
        // bit30 is Arcane conversion, so neither can mean Nature was paid.
        // The signed high bit is separately audited against native/current mod
        // masks, and round-tripped by the native AbilityParams serializer.
        internal const Metamagic Paid=(Metamagic)int.MinValue;
        // Wrath's one inherent mind-immunity component also declares Charm and
        // Compulsion explicitly. These belong to the same bypass, while Sleep,
        // Paralysis and additional immunity facts remain independent.
        internal const SpellDescriptor NativeMindImmunity=SpellDescriptor.MindAffecting|SpellDescriptor.Charm|SpellDescriptor.Compulsion;
        internal static UnitEntityData Owner(object component) {var v=AccessTools.Method(component.GetType(),"get_Owner").Invoke(component,null);return v as UnitEntityData??(v as UnitDescriptor)?.Unit;}
        internal static bool Bypass(UnitEntityData target,MechanicsContext context) {
            var caster=context?.MaybeCaster;
            if(caster==null||target==null||!context.HasMetamagic(Paid)||context.SourceAbility?.IsSpell!=true||!target.HasFact(ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(BlueprintGuid.Parse(PlantId))))return false;
            return LanguageAllowed(caster,context.SourceAbility);
        }
        internal static bool LanguageAllowed(UnitEntityData caster,BlueprintAbility spell) {
            // Wrath has no LanguageDependent descriptor: the CotW bit is now
            // native Stratagem. Never reinterpret that bit. Command's actual
            // native family is language dependent; preserve CotW's level11 gate.
            var command=ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(BlueprintGuid.Parse(TerrorWeaver01444.CommandId));
            var greater=ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(BlueprintGuid.Parse("cb15cc8d7a5480648855a23b3ba3f93d"));
            bool language=spell==command||spell.Parent==command||spell==greater||spell.Parent==greater||new[]{"ce4c4e52c53473549ae033e2bb44b51a","41236cf0e476d7043bc16a33a9f449bd"}.Any(id=>spell.AssetGuid.ToString()==id||spell.Parent?.AssetGuid.ToString()==id);
            return !language||caster.Progression.GetClassLevel(ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(BlueprintGuid.Parse(PsychicClass.ClassId)))>=11;
        }
        internal static bool Inherent(UnitEntityData target,int id)=>target.Facts.List.Where(f=>PlantImmunities.Contains(f.Blueprint.AssetGuid)).SelectMany(f=>f.Components).Any(r=>r.SourceBlueprintComponent is SpellImmunityToSpellDescriptor im&&(im.Descriptor.Value&SpellDescriptor.MindAffecting)!=0&&r.GetData<SpellImmunityToSpellDescriptor.ComponentData>().AppliedId==id);
        internal static void Register(BlueprintsCache cache) {
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(PsychicClass.ClassId);if(Existing(cache,RootToken,cls))return;
            var disciplines=cache.Load(Id("DisciplineSelection")) as BlueprintFeatureSelection;var amp=cache.Load(Id("PhrenicAmplificationSelection")) as BlueprintFeatureSelection;
            if(disciplines==null||amp==null||new[]{1,11}.Any(l=>!cls.Progression.LevelEntries.Any(e=>e.Level==l&&e.Features.Contains(amp))))throw new InvalidOperationException("01447 Magaambyan Telepath requires published Psychic disciplines and amplifications.");
            var icon=f.Native<BlueprintAbility>("0fd00984a2c0e0a429cf1a911b4ec5ca");var plant=f.Native<BlueprintFeature>(PlantId);
            var inherited=plant.GetComponents<AddFacts>().SelectMany(c=>c.Facts).OfType<BlueprintFeature>().Where(p=>p.GetComponents<SpellImmunityToSpellDescriptor>().Any(c=>(c.Descriptor.Value&SpellDescriptor.MindAffecting)!=0)||p.GetComponents<BuffDescriptorImmunity>().Any(c=>(c.Descriptor.Value&SpellDescriptor.MindAffecting)!=0)).ToArray();
            if(inherited.Length==0)throw new InvalidOperationException("01447 native Plant mind-immunity anchors changed.");
            var options=new List<BlueprintFeature>();
            foreach(var original in disciplines.AllFeatures.OfType<BlueprintProgression>()) {
                var p=SpellBatch01447.Progression(f,original,"Magaambyan"+original.AssetGuid+"01447","","");
                p.LevelEntries=original.LevelEntries.Select(e=>Entry(e.Level,e.Features.Where(x=>!x.GetComponents<AddKnownSpell>().Any()).ToArray())).ToArray();
                if(original.LevelEntries.SelectMany(e=>e.Features).Any(x=>x.GetComponents<AddKnownSpell>().Any()&&x.ComponentsArray.Any(c=>!(c is AddKnownSpell))))throw new InvalidOperationException("01447 mixed discipline spell/ability grant requires a separate adapter.");
                p.UIGroups=Array.Empty<UIGroup>();Set(p,"m_UIDeterminatorsGroup",Array.Empty<BlueprintFeatureBaseReference>());Set(p,"m_DisplayName",Get(original,"m_DisplayName"));Set(p,"m_Description",Text("Magaambyan"+original.AssetGuid+"01447","description",DisciplineDescriptions.TryGetValue(original.AssetGuid,out var disciplineDescription)?disciplineDescription:throw new InvalidOperationException("01447 original CotW discipline description is unavailable.")));SpellBatch01447.Icon(p,original);if(ReferenceEquals(p.Icon,null))SpellBatch01447.Icon(p,icon);options.Add(p);
            }
            if(options.Count!=disciplines.AllFeatures.Count())throw new InvalidOperationException("01447 unsupported Psychic discipline selection shape.");
            var noSpells=f.Clone(disciplines,"MagaambyanDisciplineSelection01447","Psychic Discipline",DisciplineText);foreach(string field in new[]{"m_Features","m_AllFeatures"})Set(noSpells,field,options.Select(Ref<BlueprintFeatureReference>).ToArray());SpellBatch01447.Icon(noSpells,icon);
            var druid=f.Native<BlueprintSpellList>("bad8638d40639d04fa2f80a1cac67d6b");var psychic=cls.Spellbook.SpellList;
            var list=SpellBatch01447.List(f,"MagaambyanPrimalSpellList01447",level=>druid.SpellsByLevel.Where(e=>e.SpellLevel==level).SelectMany(e=>e.Spells).Where(s=>!psychic.SpellsByLevel.Any(e=>e.SpellLevel!=level&&e.Spells.Contains(s))).Distinct().ToArray());
            var choices=new BlueprintFeatureSelection[9];
            for(int i=0;i<9;i++) {
                string token="MagaambyanPrimalSpell"+(i+1)+"01447";var learn=new LearnSpellParametrized{SpecificSpellLevel=true,SpellLevel=i+1};Set(learn,"m_SpellcasterClass",Ref<BlueprintCharacterClassReference>(cls));Set(learn,"m_SpellList",Ref<BlueprintSpellListReference>(list));
                var param=f.Clone(f.Native<BlueprintParametrizedFeature>("bcd757ac2aeef3c49b77e5af4e510956"),token+"Parameter","Primal Spells (level "+(i+1)+")",PrimalText);param.ParameterType=FeatureParameterType.LearnSpell;param.SpecificSpellLevel=true;param.SpellLevel=i+1;param.SpellLevelPenalty=0;Set(param,"m_SpellcasterClass",Ref<BlueprintCharacterClassReference>(cls));Set(param,"m_SpellList",Ref<BlueprintSpellListReference>(list));param.ComponentsArray=new BlueprintComponent[]{learn};SpellBatch01447.Icon(param,icon);
                var choice=f.Add(new BlueprintFeatureSelection{Ranks=1,IsClassFeature=true},token,"Primal Spells (Level "+(i+1)+")",PrimalText);foreach(string field in new[]{"m_Features","m_AllFeatures"})Set(choice,field,new[]{Ref<BlueprintFeatureReference>(param)});SpellBatch01447.Icon(choice,icon);choices[i]=choice;
            }
            var mode=f.Buff("NaturesCommandBuff01447","Nature’s Command",NatureText,new NaturesCommand01447());SpellBatch01447.Icon(mode,icon);
            var toggle=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.Immediately,DeactivateImmediately=true,DeactivateIfOwnerDisabled=true,DeactivateIfOwnerUnconscious=true,ComponentsArray=Array.Empty<BlueprintComponent>()},"NaturesCommandToggle01447","Nature’s Command",NatureText);Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(mode));Set(toggle,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free);SpellBatch01447.Icon(toggle,icon);
            var nature=f.Feature("NaturesCommandFeature01447","Nature’s Command",NatureText,Facts(toggle));SpellBatch01447.Icon(nature,icon);
            var levels=new[]{1,4,6,8,10,12,14,16,18};var add=levels.Select((l,i)=>i==0?Entry(l,noSpells,nature,choices[i]):Entry(l,choices[i])).ToArray();
            const string description="While most students of the Magaambya in the Mwangi Expanse focus on the blending of arcane and divine magic, followers of Old-Mage Jatembe’s more esoteric wisdom instead focus on the occult connection between all living creatures and the resonance between the natural and supernatural world. These Magaambyan telepaths hone their mental abilities to detect such spiritual and eldritch bonds, and attempt to commune with the mind of the jungle or the hearts of its wild inhabitants. Whether seeking information that only nature knows or attempting to impose their will upon the natural world, these psychics employ techniques that few outside of the Magaambya understand.";
            var root=SpellBatch01447.Archetype(f,cls,RootToken,"Magaambyan Telepath",description,add,new[]{Entry(1,disciplines,amp),Entry(11,amp)});
            SpellBatch01447.PublishArchetype(cache,f,cls,root,new BlueprintFeatureBase[]{noSpells,nature}.Concat(choices).ToArray());foreach(var p in inherited)PlantImmunities.Add(p.AssetGuid);
        }
    }
    [TypeId("2d7f217773984cc99f4a84755fab0147")]
    public sealed class NaturesCommand01447 : UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCastSpell>,IInitiatorRulebookSubscriber
    {
        sealed class Charge {internal BlueprintAbilityResource Pool;internal AbilityParams Before;internal bool Done;}
        static readonly ConditionalWeakTable<RuleCastSpell,Charge> Charges=new ConditionalWeakTable<RuleCastSpell,Charge>();
        internal static void RemoveModes(UnitEntityData owner,bool nature) {
            bool Other(Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff bp)=>nature?bp.GetComponent<NaturesCommand01447>()!=null:bp.GetComponent<PsychicAmplificationComponent>()!=null;
            foreach(var t in owner.Facts.List.OfType<ActivatableAbility>().Where(t=>t.Blueprint.Buff!=null&&Other(t.Blueprint.Buff)).ToArray())t.TurnOffImmediately();
            foreach(var b in owner.Buffs.Enumerable.Where(b=>Other(b.Blueprint)).ToArray())b.Remove();
        }
        protected override void OnTurnOn(){if(!PsychicAdvancedRuntime01458.CoordinateIfNeeded(Owner,Fact))RemoveModes(Owner,false);}
        public void OnEventAboutToTrigger(RuleCastSpell e) {
            if(Charges.TryGetValue(e,out _)||e.ForceFail||e.IsDuplicateSpellApplied||e.Context==null||e.Context.IsDuplicateSpellApplied||e.Spell?.SourceItem!=null||e.Spell?.StickyTouch!=null||!PsychicCasting.IsPsychic(e.Spell)||(e.Context.SpellDescriptor&SpellDescriptor.MindAffecting)==0||e.Context.Params.SpellLevel<=0)return;
            var wis=ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(BlueprintGuid.Parse(PsychicClass.WisdomPoolId));var cha=ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(BlueprintGuid.Parse(PsychicClass.CharismaPoolId));var pool=Owner.Resources.ContainsResource(wis)?wis:cha;if(pool==null||!Owner.Resources.HasEnoughResource(pool,2))return;
            if(!PsychicAdvancedRuntime01458.Claim(e,"Nature"))return;
            var charge=new Charge{Pool=pool,Before=e.Context.Params};Charges.Add(e,charge);Owner.Resources.Spend(pool,2);var copy=e.Context.Params.Clone();copy.Metamagic|=MagaambyanTelepath01447.Paid;e.Context.SetParams(copy);
        }
        public void OnEventDidTrigger(RuleCastSpell e){if(!Charges.TryGetValue(e,out var c)||c.Done)return;c.Done=true;if(!e.Success){Owner.Resources.Restore(c.Pool,2);e.Context.SetParams(c.Before);}}
    }
    [HarmonyPatch(typeof(PsychicAmplificationComponent),"OnTurnOn")]
    internal static class NatureAmplificationSwitch01447Patch {static void Postfix(PsychicAmplificationComponent __instance){var owner=MagaambyanTelepath01447.Owner(__instance);if(!PsychicAdvancedRuntime01458.HasDual(owner))NaturesCommand01447.RemoveModes(owner,true);}}
    [HarmonyPatch]
    internal static class NatureSpellImmunity01447Patch
    {
        static MethodBase TargetMethod()=>AccessTools.Method(typeof(UnitPartSpellResistance),"CanApply",new[]{typeof(UnitPartSpellResistance.SpellImmunity),typeof(MechanicsContext)});
        static bool Prefix(UnitPartSpellResistance __instance,UnitPartSpellResistance.SpellImmunity __0,MechanicsContext __1,ref bool __result) {
            if(__1==null||__0.Type!=SpellImmunityType.SpellDescriptor)return true;
            var target=MagaambyanTelepath01447.Owner(__instance);
            if((MagaambyanTelepath01447.Bypass(target,__1)||NatureTargeting01448.ImmunityPreview(target,__1))&&MagaambyanTelepath01447.Inherent(target,__0.Id)&&(__0.SpellDescriptor&__1.SpellDescriptor&~MagaambyanTelepath01447.NativeMindImmunity)==0){__result=false;return false;}return true;
        }
    }
    [HarmonyPatch(typeof(BuffDescriptorImmunity),"IsImmune")]
    internal static class NatureBuffImmunity01447Patch
    {
        static void Postfix(BuffDescriptorImmunity __instance,MechanicsContext __0,ref bool __result){if(__result&&(__instance.Descriptor.Value&SpellDescriptor.MindAffecting)!=0&&__0?.HasMetamagic(MagaambyanTelepath01447.Paid)==true&&MagaambyanTelepath01447.PlantImmunities.Contains(__instance.OwnerBlueprint.AssetGuid)&&MagaambyanTelepath01447.Bypass(MagaambyanTelepath01447.Owner(__instance),__0)&&(__instance.Descriptor.Value&__0.SpellDescriptor&~MagaambyanTelepath01447.NativeMindImmunity)==0)__result=false;}
    }
}
