using System;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Newtonsoft.Json;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class AmnesiaRuntime01464
    {
        internal static Spellbook Book(UnitEntityData owner)
        {
            var cls=ResourcesLibrary.TryGetBlueprint<Kingmaker.Blueprints.Classes.BlueprintCharacterClass>(BlueprintGuid.Parse(PsychicClass.ClassId));
            var data=owner?.Progression.GetClassData(cls);
            if(data?.Archetypes.Any(a=>a.AssetGuid==PsychicAssets.Id(Amnesiac01464.RootToken))!=true)return null;
            var book=owner.Descriptor.GetSpellbook(data.Spellbook);
            return book?.Blueprint.AssetGuid==PsychicAssets.Id(Amnesiac01464.BookToken)?book:null;
        }
        internal static bool Eligible(UnitEntityData owner,int circle,BlueprintAbility spell=null)
        {
            var book=Book(owner);if(book==null||book.MaxSpellLevel<circle)return false;
            int level=owner.Progression.GetClassLevel(book.Blueprint.CharacterClass);
            if(Amnesiac01464.Capacity(level,circle)<=0)return false;
            return spell==null||!book.IsKnown(spell)&&book.Blueprint.SpellList.SpellsByLevel.Any(e=>e.SpellLevel==circle&&e.Spells.Any(s=>ReferenceEquals(s,spell)));
        }
        internal static Buff Add(UnitEntityData owner,BlueprintBuff bp,Kingmaker.UnitLogic.Mechanics.MechanicsContext context,TimeSpan? duration)
        {var buff=owner.Buffs.AddBuff(bp,context,duration);if(buff!=null)buff.IsNotDispelable=true;return buff;}
    }
    public sealed class AmnesiaCapacityData01464
    {
        [JsonProperty]public int[] Previous;
    }
    [TypeId("45ec5164b9324c20ac57a16f64000201")]
    public sealed class AmnesiaCapacity01464:UnitFactComponentDelegate<AmnesiaCapacityData01464>,IResourceAmountBonusHandler,IOwnerGainLevelHandler
    {
        public BlueprintAbilityResourceReference[] Resources;
        int[] Current()
        {
            var cls=ResourcesLibrary.TryGetBlueprint<Kingmaker.Blueprints.Classes.BlueprintCharacterClass>(BlueprintGuid.Parse(PsychicClass.ClassId));
            int level=Owner.Progression.GetClassLevel(cls);
            return Enumerable.Range(1,9).Select(i=>Amnesiac01464.Capacity(level,i)).ToArray();
        }
        // The native grants initialize each pool once. Persist the last maximum
        // so a later class level grants only its new capacity, without restoring
        // slots spent on failed attempts or on duplicate level notifications.
        protected override void OnTurnOn()
        {
            if(Data.Previous!=null)return;
            var current=Current();Data.Previous=current;
            // AddAbilityResources.OnActivate precedes fact subscription, so
            // its first fill cannot see our maximum bonus. Finish that one
            // initial fill after subscription; saved/reapplied state skips it.
            for(int i=0;i<9;i++)Owner.Resources.Restore(Resources[i].Get(),current[i]);
        }
        public void HandleUnitGainLevel()
        {
            var current=Current();var previous=Data.Previous;
            Data.Previous=current;
            if(previous==null||previous.Length!=9)return;
            for(int i=0;i<9;i++){
                var resource=Resources[i].Get();int amount=Owner.Resources.GetResourceAmount(resource);
                if(current[i]>previous[i])Owner.Resources.Restore(resource,current[i]-previous[i]);
                else if(amount>current[i])Owner.Resources.Spend(resource,amount-current[i]);
            }
        }
        public void CalculateMaxResourceAmount(BlueprintAbilityResource resource,ref int bonus)
        {
            if(!Fact.Active)return;int index=Array.FindIndex(Resources,r=>r.Get()==resource);if(index<0)return;
            var cls=Kingmaker.Blueprints.ResourcesLibrary.TryGetBlueprint<Kingmaker.Blueprints.Classes.BlueprintCharacterClass>(BlueprintGuid.Parse(PsychicClass.ClassId));
            bonus+=Amnesiac01464.Capacity(Owner.Progression.GetClassLevel(cls),index+1);
        }
    }
    [TypeId("45ec5164b9324c20ac57a16f64000202")]
    public sealed class AmnesiaRecallGate01464:BlueprintComponent,IAbilityCasterRestriction,IAbilityVisibilityProvider
    {
        public int Circle;public BlueprintAbilityReference Spell;public BlueprintBuffReference Cooldown;
        public bool IsAbilityVisible(AbilityData ability)=>AmnesiaRuntime01464.Eligible(ability?.Caster?.Unit,Circle,Spell?.Get());
        public bool IsCasterRestrictionPassed(UnitEntityData caster)=>AmnesiaRuntime01464.Eligible(caster,Circle,Spell?.Get())&&!caster.Buffs.HasFact(Cooldown.Get());
        public string GetAbilityCasterRestrictionUIText()=>"Requires an unknown Psychic spell, an eligible amnesia circle, and no Spell Recollection cooldown.";
    }
    public sealed class AmnesiaSourceData01464{[JsonProperty]public bool RefundOnLevelUp=true;}
    [TypeId("45ec5164b9324c20ac57a16f64000203")]
    public sealed class AmnesiaSource01464:UnitFactComponentDelegate<AmnesiaSourceData01464>,ILevelUpInitiateUIHandler
    {
        public BlueprintAbilityResourceReference Resource;
        // As in CotW, clear borrowed knowledge before native spell selection.
        // Failed attempts keep their spent slot. A free-success source must
        // never refund an unrelated later attempt when the level-up begins.
        public void HandleLevelUpStart(LevelUpConfig config)
        {
            if(config?.Unit!=Owner)return;
            if(Data.RefundOnLevelUp){Data.RefundOnLevelUp=false;Owner.Resources.Restore(Resource.Get(),1);}
            ((Buff)Fact).Remove();
        }
    }
    // A UI listener can synchronously create the level-up preview before the
    // global start event reaches unit facts. Clear before that native boundary,
    // so subscriber ordering cannot copy borrowed knowledge into selection.
    [HarmonyPatch(typeof(LevelUpConfig),nameof(LevelUpConfig.StartController))]
    internal static class AmnesiaLevelUp01464Patch
    {
        static void Prefix(LevelUpConfig __instance)
        {
            var owner=__instance?.Unit;if(owner==null)return;
            foreach(var buff in owner.Buffs.Enumerable.Where(b=>b.Blueprint.GetComponent<AmnesiaSource01464>()!=null).ToArray())
                foreach(var runtime in buff.Components.Where(r=>r.SourceBlueprintComponent is AmnesiaSource01464).ToArray())
                    using(runtime.RequestEventContext())((AmnesiaSource01464)runtime.SourceBlueprintComponent).HandleLevelUpStart(__instance);
        }
    }
    [TypeId("45ec5164b9324c20ac57a16f64000204")]
    public sealed class RecallAmnesiaSpell01464:ContextAction
    {
        public int Circle;public BlueprintAbilityReference Spell;
        public BlueprintBuffReference Known,Cooldown,Forbidden;
        public BlueprintAbilityResourceReference Resource;
        public override string GetCaption()=>"Attempt Spell Recollection using native d100, plus d10 in combat";
        public override void RunAction()
        {
            var owner=Context.MaybeCaster;
            if(!AmnesiaRuntime01464.Eligible(owner,Circle,Spell.Get())||owner.Buffs.HasFact(Cooldown.Get()))return;
            int result=Context.TriggerRule(new RuleRollDice(owner,new DiceFormula(1,DiceType.D100))).Result;
            if(owner.IsInCombat)result+=Context.TriggerRule(new RuleRollDice(owner,new DiceFormula(1,DiceType.D10))).Result;
            if(result<=10)AmnesiaRuntime01464.Add(owner,Forbidden.Get(),Context,TimeSpan.FromSeconds(6));
            else if(result>=36){
                var buff=AmnesiaRuntime01464.Add(owner,Known.Get(),Context,null);
                if(result>=96&&buff!=null){
                    var runtime=buff.Components.Single(c=>c.SourceBlueprintComponent is AmnesiaSource01464);
                    runtime.GetData<AmnesiaSourceData01464>().RefundOnLevelUp=false;
                    owner.Resources.Restore(Resource.Get(),1);
                }
            }
            AmnesiaRuntime01464.Add(owner,Cooldown.Get(),Context,TimeSpan.FromHours(1));
        }
    }
}
