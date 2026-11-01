using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class NatureDiagnostics01449
    {
        readonly struct Key : IEquatable<Key>
        {
            internal readonly BlueprintGuid Spell,Book,Target;internal readonly int Flags;
            internal Key(BlueprintGuid spell,BlueprintGuid book,BlueprintGuid target,int flags){Spell=spell;Book=book;Target=target;Flags=flags;}
            public bool Equals(Key other)=>Spell==other.Spell&&Book==other.Book&&Target==other.Target&&Flags==other.Flags;
            public override bool Equals(object other)=>other is Key key&&Equals(key);
            public override int GetHashCode(){unchecked{return ((Spell.GetHashCode()*397^Book.GetHashCode())*397^Target.GetHashCode())*397^Flags;}}
        }
        sealed class Trace {internal readonly HashSet<Key> Keys=new HashSet<Key>();}
        static readonly ConditionalWeakTable<UnitEntityData,Trace> Traces=new ConditionalWeakTable<UnitEntityData,Trace>();
        // Read-only, at most64 distinct rejected spell/book/target combinations
        // per caster lifetime. Never re-run a target checker or log every frame.
        internal static string Rejection(AbilityData spell,UnitEntityData target)
        {
            var caster=spell?.Caster?.Unit;if(caster==null||target==null)return null;
            var marker=ResourcesLibrary.TryGetBlueprint<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(PsychicAssets.Id("NaturesCommandBuff01447"));
            if(marker==null||!caster.HasFact(marker))return null;
            var mode=caster.Buffs.Enumerable.FirstOrDefault(b=>b.Blueprint.GetComponent<NaturesCommand01447>()!=null);
            if(mode==null)return null;
            var trace=Traces.GetValue(caster,_=>new Trace());
            if(trace.Keys.Count>=64)return null;
            bool canPay=NatureTargeting01448.CanPay(spell,caster),language=MagaambyanTelepath01447.LanguageAllowed(caster,spell.Blueprint);
            int flags=(canPay?1:0)|(language?2:0)|(mode.IsActive?4:0)|(mode.IsTurnedOn?8:0)|(mode.IsSuppressed?16:0)|(spell.SourceItem!=null?32:0)|(spell.StickyTouch!=null?64:0);
            var key=new Key(spell.Blueprint.AssetGuid,spell.Spellbook?.Blueprint.AssetGuid??BlueprintGuid.Empty,target.Blueprint.AssetGuid,flags);
            if(!trace.Keys.Add(key))return null;
            string Amount(string id){var p=ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(BlueprintGuid.Parse(id));return p!=null&&caster.Resources.ContainsResource(p)?caster.Resources.GetResourceAmount(p).ToString():"absent";}
            string Facts(IEnumerable<Kingmaker.Blueprints.Facts.BlueprintUnitFact> facts)=>string.Join(",",facts.Where(f=>f!=null).Select(f=>f.name+"="+f.AssetGuid+":"+target.HasFact(f)));
            var restrictions=string.Join(";",spell.Blueprint.TargetRestrictions.Select(r=>r is AbilityTargetHasFact f?r.GetType().FullName+" inverted="+f.Inverted+" fromCaster="+f.FromCaster+" facts=["+Facts(f.CheckedFacts)+"]":r.GetType().FullName));
            return "[Nature target rejection 01449] spell="+spell.Blueprint.name+"/"+spell.Blueprint.AssetGuid+"; book="+spell.Spellbook?.Blueprint.name+"/"+spell.Spellbook?.Blueprint.AssetGuid+"; item="+(spell.SourceItem!=null)+"; sticky="+(spell.StickyTouch!=null)+"; descriptors="+spell.SpellDescriptor+"; target="+target.Blueprint.name+"/"+target.Blueprint.AssetGuid+"; plant="+target.HasFact(ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(BlueprintGuid.Parse(MagaambyanTelepath01447.PlantId)))+"; mode active="+mode.IsActive+" on="+mode.IsTurnedOn+" suppressed="+mode.IsSuppressed+"; points wis="+Amount(PsychicClass.WisdomPoolId)+" cha="+Amount(PsychicClass.CharismaPoolId)+"; canPay="+canPay+"; languageAllowed="+language+"; restrictions="+restrictions;
        }
    }
    [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.CanTarget),new[]{typeof(TargetWrapper)})]
    internal static class NatureTargetDiagnostic01449Patch
    {
        static void Postfix(AbilityData __instance,TargetWrapper __0,bool __result)
        {
            if(__result)return;
            try{var line=NatureDiagnostics01449.Rejection(__instance,__0?.Unit);if(line!=null)ModEntry.TraceNatureTarget(line);}catch{ /* Optional diagnostic must not change targeting. */ }
        }
    }
}
