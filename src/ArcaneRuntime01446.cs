using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    public enum ArcaneMode01446 { Intensity, Piercing, Versatile }
    internal static class ArcaneCast01446
    {
        // Execution-only metadata. Never added to AbilityData, spellbook slots or
        // AvailableMetamagic. Native context serialization retains these flags.
        // Toppling exclusively owns bit28; neither paid mutation sets it.
        internal const Metamagic Conversion=(Metamagic)(1<<30),EnergyMask=(Metamagic)(3<<14);
        // Versioned execution payload in bits already owned by this module.
        // Bit28 remains exclusively Toppling. Without MutationStamp,14/15
        // retain their old conversion-energy meaning for existing contexts.
        internal const Metamagic MutationStamp=(Metamagic)(1<<27),Intensity=(Metamagic)(1<<14),Piercing=(Metamagic)(1<<15);
        internal static bool Marked(MechanicsContext context,Metamagic mode)=>context?.Params!=null&&(context.Params.Metamagic&(MutationStamp|mode))==(MutationStamp|mode);

        internal static readonly DamageEnergyType[] Energies={DamageEnergyType.Acid,DamageEnergyType.Electricity,DamageEnergyType.Fire,DamageEnergyType.Cold};
        internal static readonly SpellDescriptor[] Descriptors={SpellDescriptor.Acid,SpellDescriptor.Electricity,SpellDescriptor.Fire,SpellDescriptor.Cold};
        internal const SpellDescriptor Elements=SpellDescriptor.Acid|SpellDescriptor.Cold|SpellDescriptor.Electricity|SpellDescriptor.Fire;
        sealed class Charge {internal BlueprintAbilityResource Pool;internal int Amount;}
        sealed class Cast {internal AbilityParams Original;internal SpellDescriptor Descriptor;internal readonly HashSet<ArcaneMode01446> Modes=new HashSet<ArcaneMode01446>();internal readonly List<Charge> Charges=new List<Charge>();internal bool Finished;}
        static readonly ConditionalWeakTable<RuleCastSpell,Cast> Casts=new ConditionalWeakTable<RuleCastSpell,Cast>();
        internal static int Strength(UnitEntityData caster)=>Math.Max(0,Math.Max(caster.Stats.Strength.Bonus,caster.Stats.Charisma.Bonus));
        internal static bool Element(DamageEnergyType e)=>Energies.Contains(e);
        internal static bool SpellBook(AbilityData spell) {
            var cls=spell?.Spellbook?.Blueprint.CharacterClass;
            return cls!=null&&(cls.AssetGuid.ToString()==BloodlineMutations01446.SorcererId||cls.AssetGuid.ToString()==BloodlineMutations01446.BloodragerId||cls.AssetGuid.ToString()==BloodlineMutations01446.MagusId&&spell.Caster.Progression.GetClassData(cls)?.Archetypes.Any(a=>a.AssetGuid.ToString()==BloodlineMutations01446.ScionId)==true);
        }
        internal static bool Damage(BlueprintAbility ability)=>DamageObject(ability,new HashSet<object>(),0);
        static bool DamageObject(object value,HashSet<object> seen,int depth) {
            if(value==null||depth>30||value is string||value.GetType().IsValueType||!seen.Add(value))return false;
            if(value is Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage damage)return !damage.IsAoE||damage.Value!=null;
            if(value is BlueprintReferenceBase reference)return (reference is BlueprintAbilityReference||reference is BlueprintBuffReference||reference is BlueprintAbilityAreaEffectReference)&&DamageObject(reference.GetBlueprint(),seen,depth+1);
            if(value is BlueprintAbility||value is BlueprintBuff||value is BlueprintAbilityAreaEffect)return ((BlueprintScriptableObject)value).ComponentsArray.Any(c=>DamageObject(c,seen,depth+1));
            if(value is SimpleBlueprint||value is UnityEngine.Object)return false;
            if(value is IEnumerable items)return items.Cast<object>().Any(x=>DamageObject(x,seen,depth+1));
            if(!(value is BlueprintComponent)&&!(value is Kingmaker.ElementsSystem.Element)&&!value.GetType().Namespace.StartsWith("Kingmaker"))return false;
            for(var t=value.GetType();t!=null&&t!=typeof(object)&&t!=typeof(BlueprintComponent)&&t!=typeof(Kingmaker.ElementsSystem.Element);t=t.BaseType)
                foreach(var field in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(p=>!p.IsStatic))if(DamageObject(field.GetValue(value),seen,depth+1))return true;
            return false;
        }
        internal static bool Eligible(RuleCastSpell evt,ArcaneMode01446 mode) {
            var a=evt.Spell;
            if(evt.ForceFail||evt.IsDuplicateSpellApplied||evt.Context==null||evt.Context.IsDuplicateSpellApplied||a==null||a.SourceItem!=null||!a.Blueprint.IsSpell||a.StickyTouch!=null||!Damage(a.Blueprint))return false;
            if(mode==ArcaneMode01446.Versatile)return a.Blueprint.School==SpellSchool.Evocation&&(evt.Context.SpellDescriptor&Elements)!=0;
            return SpellBook(a)&&(mode!=ArcaneMode01446.Intensity||(a.Blueprint.AvailableMetamagic&Metamagic.Intensified)!=0);
        }
        internal static void Begin(ArcaneCastMode01446 mode,RuleCastSpell evt) {
            if(!Eligible(evt,mode.Kind))return;
            var pool=mode.Resource?.Get();if(pool==null||!evt.Initiator.Descriptor.Resources.HasEnoughResource(pool,1))return;
            var state=Casts.GetValue(evt,_=>new Cast{Original=evt.Context.Params,Descriptor=evt.Context.SpellDescriptor});
            if(state.Finished||!state.Modes.Add(mode.Kind))return;
            evt.Initiator.Descriptor.Resources.Spend(pool,1);state.Charges.Add(new Charge{Pool=pool,Amount=1});
            var parameters=evt.Context.Params.Clone();
            if(mode.Kind==ArcaneMode01446.Versatile) {
                if((parameters.Metamagic&MutationStamp)==0)parameters.Metamagic=(parameters.Metamagic&~EnergyMask)|(Metamagic)(mode.EnergyIndex<<14);
                parameters.Metamagic|=Conversion;
                evt.Context.RemoveSpellDescriptor(Elements);evt.Context.AddSpellDescriptor(Descriptors[mode.EnergyIndex]);
            } else {
                if((parameters.Metamagic&MutationStamp)==0)parameters.Metamagic&=~EnergyMask;
                parameters.Metamagic|=MutationStamp|(mode.Kind==ArcaneMode01446.Intensity?Intensity:Piercing);
            }
            evt.Context.SetParams(parameters);evt.Context.RecalculateRanks();
        }
        internal static void End(RuleCastSpell evt) {
            if(!Casts.TryGetValue(evt,out var state)||state.Finished)return;state.Finished=true;
            if(evt.Success)return;
            foreach(var charge in state.Charges)evt.Initiator.Descriptor.Resources.Restore(charge.Pool,charge.Amount);
            evt.Context.SetParams(state.Original);evt.Context.RemoveSpellDescriptor(Elements);evt.Context.AddSpellDescriptor(state.Descriptor&Elements);evt.Context.RecalculateRanks();
        }
        internal static bool Intensify(ContextRankConfig rank,int value,bool force,MechanicsContext context,ref int result) {
            if(force||context?.SourceAbility?.IsSpell!=true||!Marked(context,Intensity)||context.MaybeCaster==null||!(bool)Get(rank,"m_AffectedByIntensifiedMetamagic")||!(bool)Get(rank,"m_UseMax"))return true;
            int bonus=Scale(rank,Strength(context.MaybeCaster));if(context.HasMetamagic(Metamagic.Intensified))bonus=Math.Max(5,bonus);
            if(context.HasMetamagic((Metamagic)(1<<12)))bonus=Math.Max(Scale(rank,5),bonus);
            result=(bool)Get(rank,"m_UseMin")?Math.Max(value,(int)Get(rank,"m_Min")):value;result=Math.Min(result,(int)Get(rank,"m_Max")+bonus);return false;
        }
        internal static int Scale(ContextRankConfig rank,int value) {
            switch((ContextRankProgression)Get(rank,"m_Progression")) {
                case ContextRankProgression.AsIs:case ContextRankProgression.BonusValue:return value;
                case ContextRankProgression.Div2:case ContextRankProgression.OnePlusDiv2:case ContextRankProgression.Div2PlusStep:return value/2;
                case ContextRankProgression.DivStep:case ContextRankProgression.OnePlusDivStep:case ContextRankProgression.StartPlusDivStep:case ContextRankProgression.DelayedStartPlusDivStep:return value/Math.Max(1,(int)Get(rank,"m_StepLevel"));
                case ContextRankProgression.StartPlusDoubleDivStep:return 2*(value/Math.Max(1,(int)Get(rank,"m_StepLevel")));
                case ContextRankProgression.HalfMore:return value+value/2;
                case ContextRankProgression.MultiplyByModifier:return value*(int)Get(rank,"m_StepLevel");
                default:return 0;
            }
        }
        internal static void Prepare(RuleCalculateDamage rule) {
            var context=rule.Reason.Context;if(context?.SourceAbility?.IsSpell!=true||context.Params==null)return;
            foreach(var damage in rule.DamageBundle.OfType<EnergyDamage>()) {
                if(Marked(context,Piercing)&&context.MaybeCaster!=null)damage.ReductionPenalty.Add(ModifierDescriptor.UntypedStackable,Strength(context.MaybeCaster));
            }
        }
        internal static void Convert(RulePrepareDamage rule) {
            var context=rule.ParentRule?.Reason?.Context??rule.Reason?.Context;
            if(context?.SourceAbility?.IsSpell!=true||!context.HasMetamagic(Conversion))return;
            int energyIndex;
            if((context.Params.Metamagic&MutationStamp)!=0) {
                var source=context.SourceAbilityContext??context;var descriptor=source.SpellDescriptor&Elements;
                energyIndex=Array.FindIndex(Descriptors,d=>d==descriptor);
                if(energyIndex<0)return;
            } else energyIndex=((int)context.Params.Metamagic>>14)&3;
            foreach(var damage in rule.DamageBundle.OfType<EnergyDamage>().Where(d=>Element(d.EnergyType)))damage.ReplaceEnergy(Energies[energyIndex]);
        }
    }
    [AllowedOn(typeof(BlueprintBuff),false),TypeId("d720b4453f874e12b11f7a100581df46")]
    public sealed class ArcaneCastMode01446 : UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCastSpell>,IInitiatorRulebookSubscriber
    {
        public ArcaneMode01446 Kind;
        public int EnergyIndex;
        public BlueprintAbilityResourceReference Resource;
        protected override void OnTurnOn() {
            // Only the four Versatile modes are mutually exclusive. Intensity
            // and Piercing can be used together, as in the original CotW.
            if(Kind!=ArcaneMode01446.Versatile)return;
            foreach(var toggle in Owner.Facts.List.OfType<ActivatableAbility>().ToArray())if(toggle.Blueprint.Buff!=Fact.Blueprint&&toggle.Blueprint.Buff?.GetComponent<ArcaneCastMode01446>()?.Kind==Kind)toggle.IsOn=false;
            foreach(var buff in Owner.Buffs.Enumerable.ToArray())if(buff!=Fact&&buff.Blueprint.GetComponent<ArcaneCastMode01446>()?.Kind==Kind)buff.Remove();
        }
        public void OnEventAboutToTrigger(RuleCastSpell evt)=>ArcaneCast01446.Begin(this,evt);
        public void OnEventDidTrigger(RuleCastSpell evt)=>ArcaneCast01446.End(evt);
    }
    [AllowedOn(typeof(BlueprintFeature),false),TypeId("711a815799d948719b6be774c508a046")]
    public sealed class BloodHavoc01446 : UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCalculateDamage>,IInitiatorRulebookSubscriber
    {
        public BlueprintAbilityReference[] Spells;
        public BlueprintFeatureBaseReference[] Grants;
        // Retain the serialized component identity only. No blueprint publishes
        // this component, and even an old serialized fact grants no damage.
        public void OnEventAboutToTrigger(RuleCalculateDamage evt) {}
        public void OnEventDidTrigger(RuleCalculateDamage evt){}
    }
    [AllowedOn(typeof(BlueprintBuff),false),TypeId("ab52779d012c457ba51adc2b1c42d046")]
    public sealed class ElementalManipulation01446 : UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCalculateDamage>,IInitiatorRulebookSubscriber
    {
        public int EnergyIndex;
        public BlueprintCharacterClassReference Wizard;
        public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
            var context=evt.Reason.Context;var ability=context?.SourceAbility;if(ability==null)return;
            bool supernatural=ability.Type==AbilityType.Supernatural;if(!ability.IsSpell&&!supernatural)return;
            int level=supernatural?evt.Initiator.Progression.CharacterLevel:context.Params?.CasterLevel??int.MaxValue;
            var caster=Context?.MaybeCaster;if(caster==null||level>caster.Progression.GetClassLevel(Wizard.Get()))return;
            foreach(var damage in evt.DamageBundle.OfType<EnergyDamage>().Where(d=>ArcaneCast01446.Element(d.EnergyType)))damage.ReplaceEnergy(ArcaneCast01446.Energies[EnergyIndex]);
        }
        public void OnEventDidTrigger(RuleCalculateDamage evt){}
        protected override void OnTurnOn() {
            // Overlapping auras follow native Replace stacking; switching one's
            // carrier is handled separately by ElementalAuraMode01446.
        }
    }
    [AllowedOn(typeof(BlueprintBuff),false),TypeId("74daac43445244bfaf7c9aa02b830046")]
    public sealed class ElementalAuraMode01446 : UnitFactComponentDelegate
    {
        protected override void OnTurnOn() {
            foreach(var toggle in Owner.Facts.List.OfType<ActivatableAbility>().ToArray())if(toggle.Blueprint.Buff!=Fact.Blueprint&&toggle.Blueprint.Buff?.GetComponent<ElementalAuraMode01446>()!=null)toggle.IsOn=false;
            foreach(var buff in Owner.Buffs.Enumerable.ToArray())if(buff!=Fact&&buff.Blueprint.GetComponent<ElementalAuraMode01446>()!=null)buff.Remove();
        }
    }
    [HarmonyPatch(typeof(ContextRankConfig),"ApplyMinMax")]
    internal static class BloodIntensityRank01446Patch
    {
        static bool Prefix(ContextRankConfig __instance,int value,bool __1,MechanicsContext context,ref int __result)=>ArcaneCast01446.Intensify(__instance,value,__1,context,ref __result);
    }
    [HarmonyPatch(typeof(RuleCalculateDamage),nameof(RuleCalculateDamage.OnTrigger))]
    internal static class ArcaneDamage01446Patch { static void Prefix(RuleCalculateDamage __instance)=>ArcaneCast01446.Prepare(__instance); }
    // CotW converts the paid spell during preparation. The later calculate
    // event then lets Elemental Manipulation override it inside the aura.
    [HarmonyPatch(typeof(RulePrepareDamage),nameof(RulePrepareDamage.OnTrigger))]
    internal static class ArcanePrepare01446Patch { static void Prefix(RulePrepareDamage __instance)=>ArcaneCast01446.Convert(__instance); }
    // TTT's Intensified postprocessor computes its own five-level cap from the
    // unmodified rank definition. Retain the higher paid Blood Intensity result;
    // do not add the two caps and do not rewrite the shared rank definition.
    [HarmonyPatch(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage),"GetDamageInfo")]
    internal static class BloodIntensityDamageInfo01446Patch
    {
        [HarmonyPostfix,HarmonyPriority(Priority.Last),HarmonyAfter("TabletopTweaks-Base")]
        static void Postfix(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage __instance,ref object __result) {
            var context=(MechanicsContext)AccessTools.Property(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextAction),"Context").GetValue(__instance,null);if(context?.SourceAbility?.IsSpell!=true||!ArcaneCast01446.Marked(context,ArcaneCast01446.Intensity)||!context.HasMetamagic((Metamagic)(1<<12))||__instance.Value.DiceCountValue.ValueType!=ContextValueType.Rank)return;
            var ranks=context.AssociatedBlueprint?.GetComponents<ContextRankConfig>()??Enumerable.Empty<ContextRankConfig>();
            var rank=ranks.FirstOrDefault(r=>r.Type==__instance.Value.DiceCountValue.ValueRank&&(bool)Get(r,"m_AffectedByIntensifiedMetamagic"));
            if(rank!=null){var dice=(DiceFormula)Get(__result,"Dices");Set(__result,"Dices",new DiceFormula(Math.Max(dice.Rolls,rank.GetValue(context)),dice.Dice));}
        }
    }
    [HarmonyPatch(typeof(RuleSpellResistanceCheck),nameof(RuleSpellResistanceCheck.OnTrigger))]
    internal static class BloodPiercingResistance01446Patch
    {
        static void Prefix(RuleSpellResistanceCheck __instance) {
            var context=__instance.Context;if(context?.SourceAbility?.IsSpell==true&&ArcaneCast01446.Marked(context,ArcaneCast01446.Piercing)&&context.MaybeCaster!=null)__instance.AddSpellPenetration(ArcaneCast01446.Strength(context.MaybeCaster),ModifierDescriptor.UntypedStackable);
        }
    }
}
