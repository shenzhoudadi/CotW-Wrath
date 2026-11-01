// Original CotW MechanicsFixes/KineticistFix.createWhipHurricane (MIT).
// Reuses the installed DarkCodex whip mechanics; does not create another
// Kinetic Whip feat or modify its globally shared buff.
using System;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ElementsSystem;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class WhipHurricane01454
    {
        internal const string ModuleName="Infusion: Whip Hurricane",RootToken="WhipHurricaneFeature01454",AbilityToken="WhipHurricaneAbility01454",LeaseToken="WhipHurricaneLease01454",WhipToken="WhipHurricaneWhip01454";
        internal const string WhipId="00df2fabfc3f40a4aaf4a066dcd78b80",WhipBuffId="26983c7ac7f1465fab8ec646f366b9f7";
        internal const string Description="Element: universal\nType: form infusion\nLevel: 6\nBurn: 4\nPrerequisites: kinetic blade, kinetic whip, blade whirlwind\nAssociated Blasts: any\nSaving Throw: none\nAs blade whirlwind, except it manifests a kinetic whip, and the whip lasts until the beginning of your next turn or until you use any form infusion that creates a blade or whip again, whichever comes first.";
        internal static WeaponKineticBlade Blade(UnitEntityData unit)=>unit?.Body?.PrimaryHand?.MaybeWeapon?.Blueprint.GetComponent<WeaponKineticBlade>();
        internal static BlueprintBuff Marker(UnitPartKineticist part)=>(Get(part,"m_Settings") as AddKineticistPart)?.BladeActivatedBuff;
        internal static bool HasDependency(BlueprintsCache cache)=>cache.Load(BlueprintGuid.Parse(WhipId)) is BlueprintFeature&&cache.Load(BlueprintGuid.Parse(WhipBuffId)) is BlueprintBuff;
        internal static void Register(BlueprintsCache cache)
        {
            if(MagusBatch01454.Existing(cache,ModuleName))return;
            var f=new PsychicAssets(cache);var whip=f.Native<BlueprintFeature>(WhipId);var originalWhip=f.Native<BlueprintBuff>(WhipBuffId);
            var selection=f.Native<BlueprintFeatureSelection>("58d6f8e9eea63f6418b107ce64f315ea");var cls=f.Native<BlueprintCharacterClass>("42a455d9ec1ad924d889272429eb8391");var knight=f.Native<BlueprintArchetype>("7d61d9b2250260a45b18c5634524a8fb");
            var whirlwind=f.Native<BlueprintAbility>("80f10dc9181a0f64f97a9f7ac9f47d65");
            var originalModifier=originalWhip.GetComponents<AddKineticistBurnModifier>().ToArray();if(originalModifier.Length!=1||originalModifier[0].BurnType!=KineticistBurnType.Infusion||originalModifier[0].Value!=1)throw new InvalidOperationException("01454 configured Kinetic Whip cost shape changed.");
            var effect=f.Clone(originalWhip,WhipToken,"Kinetic Whip",Description);Set(effect,"m_Icon",PsychicOriginalIcons.Load("WhipHurricane"));
            var lease=f.Buff(LeaseToken,"Whip Hurricane",Description,new WhipHurricaneLease01454{Whip=Ref<BlueprintBuffReference>(effect),ExternalWhip=Ref<BlueprintBuffReference>(originalWhip)});Set(lease,"m_Icon",effect.Icon);lease.Stacking=StackingType.Replace;SetEnum(lease,"m_Flags","RemoveOnRest");
            var ability=f.Clone(whirlwind,AbilityToken,"Whip Hurricane",Description);Set(ability,"m_Icon",effect.Icon);
            // Only the obsolete fixed blade-buff whitelist is replaced. The
            // actual attack/delivery, animation and target collection remain
            // native (including any supported provider's reach controller).
            var gates=ability.GetComponents<AbilityCasterHasFacts>().ToArray();if(gates.Length!=1)throw new InvalidOperationException("01454 native whirlwind caster gate changed.");
            ability.ComponentsArray=ability.ComponentsArray.Where(c=>c!=gates[0]&&c.GetType().FullName!="CodexLib.AddKineticBladeBurn").Concat(new BlueprintComponent[]{new WhipHurricaneCaster01454()}).ToArray();
            var cost=ability.GetComponent<AbilityKineticist>()??throw new InvalidOperationException("01454 native whirlwind burn component missing.");cost.BlastBurnCost=0;cost.InfusionBurnCost=4;
            var apply=Apply(lease,1,Kingmaker.UnitLogic.Mechanics.DurationRate.Rounds,toCaster:true,fromSpell:false);apply.IsNotDispelable=true;
            var onCast=new AbilityExecuteActionOnCast{Conditions=new ConditionsChecker{Conditions=Array.Empty<Condition>()},Actions=new ActionList{Actions=new GameAction[]{apply}}};ability.ComponentsArray=ability.ComponentsArray.Concat(new BlueprintComponent[]{onCast}).ToArray();
            var blade=f.Native<BlueprintFeature>("9ff81732daddb174aa8138ad1297c787");var whirlwindFeat=f.Native<BlueprintFeature>("80fdf049d396c33408a805d9e21a42e1");
            var needBlade=new PrerequisiteFeature();Set(needBlade,"m_Feature",Ref<BlueprintFeatureReference>(blade));var needWhip=new PrerequisiteFeature();Set(needWhip,"m_Feature",Ref<BlueprintFeatureReference>(whip));var needWhirlwind=new PrerequisiteFeature();Set(needWhirlwind,"m_Feature",Ref<BlueprintFeatureReference>(whirlwindFeat));
            var level=new PrerequisiteClassLevel{Level=12};Set(level,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));
            var feature=f.Feature(RootToken,"Whip Hurricane",Description,needBlade,needWhip,needWhirlwind,level,Facts(ability),new SpellFeatCleanup{Buffs=new[]{Ref<BlueprintBuffReference>(lease)}});feature.Groups=new[]{FeatureGroup.KineticBlastInfusion};Set(feature,"m_Icon",effect.Icon);
            var oldAll=(BlueprintFeatureReference[])Get(selection,"m_AllFeatures");var oldFeatures=(BlueprintFeatureReference[])Get(selection,"m_Features");var oldEntries=knight.AddFeatures;var oldGroups=cls.Progression.UIGroups;var group=MagusBatch01454.Group(feature);
            // Original CotW explicitly enrolls Hurricane in metakinesis. Keep
            // each current provider list, including foreign entries, and append
            // only this exact ability to both its effect and burn filters.
            var metas=new[]{"f5f3aa17dd579ff49879923fb7bc2adb","f8d0f7099e73c95499830ec0a93e2eeb","870d7e67e97a68f439155bdf465ea191","b8f43f0040155c74abd1bc794dbec320","f690edc756b748e43bba232e0eabd004","c4b74e4448b81d04f9df89ed14c38a95"}.Select(f.Native<BlueprintBuff>).ToArray();
            var auto=metas.Select(b=>{var a=b.GetComponents<AutoMetamagic>().ToArray();if(a.Length!=1)throw new InvalidOperationException("01454 native metakinesis effect shape changed.");return a[0];}).ToArray();
            var burn=metas.Select(b=>{var a=b.GetComponents<AddKineticistBurnModifier>().ToArray();if(a.Length!=1||a[0].BurnType!=KineticistBurnType.Metakinesis)throw new InvalidOperationException("01454 native metakinesis cost shape changed.");return a[0];}).ToArray();
            var oldAuto=auto.Select(a=>a.Abilities).ToArray();var oldBurn=burn.Select(b=>Get(b,"m_AppliableTo")).ToArray();
            Action connect=()=>{
                Set(selection,"m_AllFeatures",MagusBatch01454.Append((BlueprintFeatureReference[])Get(selection,"m_AllFeatures"),feature));Set(selection,"m_Features",MagusBatch01454.Append((BlueprintFeatureReference[])Get(selection,"m_Features"),feature));
                if(!knight.AddFeatures.Any(e=>e.Level==11&&e.Features.Any(x=>ReferenceEquals(x,feature))))knight.AddFeatures=knight.AddFeatures.Concat(new[]{ArchetypeBatch01445.Entry(11,feature)}).ToArray();
                if(!cls.Progression.UIGroups.Contains(group))cls.Progression.UIGroups=cls.Progression.UIGroups.Concat(new[]{group}).ToArray();
                for(int i=0;i<metas.Length;i++){auto[i].Abilities=AppendAbility(auto[i].Abilities?.ToArray(),ability).ToList();Set(burn[i],"m_AppliableTo",AppendAbility((BlueprintAbilityReference[])Get(burn[i],"m_AppliableTo"),ability));}
            };
            Action undo=()=>{Set(selection,"m_AllFeatures",oldAll);Set(selection,"m_Features",oldFeatures);knight.AddFeatures=oldEntries;cls.Progression.UIGroups=oldGroups;for(int i=0;i<metas.Length;i++){auto[i].Abilities=oldAuto[i];Set(burn[i],"m_AppliableTo",oldBurn[i]);}};
            Action verify=()=>{if(selection.AllFeatures.Count(x=>ReferenceEquals(x,feature))!=1||knight.AddFeatures.Where(e=>e.Level==11).SelectMany(e=>e.Features).Count(x=>ReferenceEquals(x,feature))!=1||cls.Progression.UIGroups.Count(g=>ReferenceEquals(g,group))!=1)throw new InvalidOperationException("01454 Hurricane attachment incomplete.");for(int i=0;i<metas.Length;i++)if(auto[i].Abilities.Count(a=>a?.Guid==ability.AssetGuid&&ReferenceEquals(a.Get(),ability))!=1||burn[i].AppliableTo.Count(a=>ReferenceEquals(a,ability))!=1)throw new InvalidOperationException("01454 Hurricane metakinesis attachment changed.");};
            MagusBatch01454.Publish(ModuleName,f,connect,undo,verify);
        }
        static BlueprintAbilityReference[] AppendAbility(BlueprintAbilityReference[] before,BlueprintAbility ability)
        {
            before=before??Array.Empty<BlueprintAbilityReference>();var same=before.Where(r=>r?.Guid==ability.AssetGuid).ToArray();if(same.Length==0)return before.Concat(new[]{Ref<BlueprintAbilityReference>(ability)}).ToArray();
            if(same.Length!=1||!ReferenceEquals(same[0].Get(),ability))throw new InvalidOperationException("01454 metakinesis ability identity collision.");return before;
        }
        internal static bool WithLease(UnitEntityData unit,Func<WhipHurricaneLease01454,bool> action)
        {
            if(unit==null)return false;
            foreach(var buff in unit.Buffs.Enumerable.ToArray()){
                if(buff.Blueprint.AssetGuid!=MagusBatch01454.Identity(LeaseToken)||!buff.IsActive||!buff.IsTurnedOn||buff.IsSuppressed||buff.IsDisposed)continue;
                var runtime=buff.Components.FirstOrDefault(c=>c.SourceBlueprintComponent is WhipHurricaneLease01454);if(runtime==null)continue;
                using(runtime.RequestEventContext())if(action((WhipHurricaneLease01454)runtime.SourceBlueprintComponent))return true;
            }
            return false;
        }
    }
    [TypeId("6b68e8a4f99548de919d13cfad4e9454")]
    public sealed class WhipHurricaneCaster01454:BlueprintComponent,IAbilityCasterRestriction
    {
        public bool IsCasterRestrictionPassed(UnitEntityData caster)=>caster?.Get<UnitPartKineticist>() is UnitPartKineticist part&&WhipHurricane01454.Marker(part)!=null&&WhipHurricane01454.Blade(caster)?.ActivationAbility!=null;
        public string GetAbilityCasterRestrictionUIText()=>"An active kinetic blade is required.";
    }
    public sealed class WhipHurricaneState01454
    {
        [JsonProperty] public ItemEntityWeapon Blade;
        [JsonProperty] public Buff Whip;
        [JsonProperty] public Buff ActivationMarker;
        [JsonProperty] public bool OwnsMarker,DeferredRemoval;
        [JsonIgnore] public bool Busy,Releasing;
    }
    [TypeId("9de14a1e2c3d463b84b5ca67bdbbe454")]
    public sealed class WhipHurricaneLease01454:UnitBuffComponentDelegate<WhipHurricaneState01454>,IUnitBuffHandler,IUnitEquipmentHandler,IUnitActiveEquipmentSetHandler,IUnitNewCombatRoundHandler,IUnitSubscriber
    {
        public BlueprintBuffReference Whip,ExternalWhip;
        internal bool Ready=>!Data.Releasing&&Buff.IsActive&&Buff.IsTurnedOn&&!Buff.IsSuppressed&&!Buff.IsDisposed&&Data.Blade!=null&&ReferenceEquals(Owner.Body.PrimaryHand.MaybeWeapon,Data.Blade);
        void RemoveWhip(){var child=Data.Whip;Data.Whip=null;if(child!=null&&!child.IsDisposed&&child.Owner==Owner&&child.Blueprint==Whip.Get()&&child.Context?.MaybeCaster==Owner)child.Remove();}
        void Ensure(Buff leaving=null)
        {
            if(Data.Busy||!Ready)return;Data.Busy=true;
            try{
                bool external=Owner.Buffs.Enumerable.Any(b=>b!=leaving&&b.Blueprint==ExternalWhip.Get()&&b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed&&!b.IsDisposed);
                if(external)RemoveWhip();else if(Data.Whip==null||Data.Whip.IsDisposed){Data.Whip=Owner.Buffs.AddBuff(Whip.Get(),Context,(TimeSpan?)null);if(Data.Whip!=null)Data.Whip.IsNotDispelable=true;}
            }finally{Data.Busy=false;}
        }
        protected override void OnTurnOn()
        {
            Data.Releasing=false;
            // A restored component already owns exact persisted references.
            // Never turn a stale record into a lease on a different weapon or
            // borrow a newly created activation marker while restoring it.
            // Post-load validation runs after all buff references are attached.
            if(Data.Blade!=null){if(!ReferenceEquals(Data.Blade,Owner.Body.PrimaryHand.MaybeWeapon))Buff.Remove();return;}
            Data.Blade=Owner.Body.PrimaryHand.MaybeWeapon;
            if(WhipHurricane01454.Blade(Owner)==null){Buff.Remove();return;}
            var part=Owner.Get<UnitPartKineticist>();if(part==null){Buff.Remove();return;}
            var marker=WhipHurricane01454.Marker(part);var before=Owner.Buffs.Enumerable.FirstOrDefault(b=>b.Blueprint==marker&&!b.IsDisposed);
            Data.Busy=true;
            try{
                if(before==null){part.AddBladeActivatedBuff();Data.ActivationMarker=Owner.Buffs.Enumerable.FirstOrDefault(b=>b.Blueprint==marker&&!b.IsDisposed);Data.OwnsMarker=Data.ActivationMarker!=null;}
                else Data.ActivationMarker=before;
            }finally{Data.Busy=false;}
            Ensure();
        }
        protected override void OnTurnOff()
        {
            if(Data.Releasing)return;Data.Releasing=true;Data.Busy=true;
            try{
                RemoveWhip();var marker=Data.ActivationMarker;Data.ActivationMarker=null;
                if(marker!=null&&!marker.IsDisposed&&marker.Owner==Owner){
                    if(Data.OwnsMarker)marker.Remove();
                    else if(Data.DeferredRemoval&&Owner.Buffs.Enumerable.Any(b=>ReferenceEquals(b,marker)))Owner.Get<UnitPartKineticist>()?.RemoveBladeActivatedBuff();
                }
                Data.OwnsMarker=false;Data.DeferredRemoval=false;Data.Blade=null;
            }finally{Data.Busy=false;}
        }
        protected override void OnApplyPostLoadFixes(){var part=Owner.Get<UnitPartKineticist>();if(!Ready||part==null||Data.ActivationMarker==null||Data.ActivationMarker.IsDisposed||Data.ActivationMarker.Owner!=Owner||Data.ActivationMarker.Blueprint!=WhipHurricane01454.Marker(part)||!Owner.Buffs.Enumerable.Any(b=>ReferenceEquals(b,Data.ActivationMarker))){Buff.Remove();return;}Ensure();}
        internal bool HoldRemoval(){if(!Ready)return false;Data.DeferredRemoval=true;return true;}
        internal void ObserveForm(AbilityExecutionContext context){if(!ReferenceEquals(Context?.SourceAbilityContext,context))Buff.Remove();}
        public void HandleBuffDidAdded(Buff buff){
            if(buff.Owner!=Owner||Data.Busy)return;
            if(buff.Blueprint==ExternalWhip.Get()){Ensure();return;}
            var part=Owner.Get<UnitPartKineticist>();
            if(part!=null&&buff.Blueprint==WhipHurricane01454.Marker(part)&&!ReferenceEquals(buff,Data.ActivationMarker))Buff.Remove();
        }
        public void HandleBuffDidRemoved(Buff buff){if(buff.Owner==Owner&&buff.Blueprint==ExternalWhip.Get())Ensure(buff);}
        public void HandleEquipmentSlotUpdated(ItemSlot slot,ItemEntity previousItem){if(slot.Owner==Owner&&!Data.Busy&&!Ready)Buff.Remove();}
        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit){if(unit==Owner.Descriptor&&!Ready)Buff.Remove();}
        public void HandleNewCombatRound(UnitEntityData unit){if(unit==Owner)Buff.Remove();}
    }
    [HarmonyPatch(typeof(UnitPartKineticist),nameof(UnitPartKineticist.RemoveBladeActivatedBuff))]
    internal static class WhipHurricaneBladeLifetime01454
    {
        static bool Prefix(UnitPartKineticist __instance)=>!WhipHurricane01454.WithLease(__instance.Owner.Unit,c=>c.HoldRemoval());
    }
    [HarmonyPatch(typeof(Kingmaker.Controllers.AbilityExecutionProcess),nameof(Kingmaker.Controllers.AbilityExecutionProcess.PrepareCast))]
    internal static class WhipHurricaneNewForm01454
    {
        static void Postfix(AbilityExecutionContext __0)
        {
            var bp=__0?.AbilityBlueprint;if(bp?.GetComponent<AbilityKineticist>()==null)return;
            bool form=bp.GetComponent<AbilityKineticBlade>()!=null||bp.AssetGuid==BlueprintGuid.Parse("80f10dc9181a0f64f97a9f7ac9f47d65")||bp.AssetGuid==MagusBatch01454.Identity(WhipHurricane01454.AbilityToken)||
                bp.AssetGuid==BlueprintGuid.Parse("e6b5691631c7417696af64bdef75f706")&&bp.ComponentsArray.Any(c=>c.GetType().Name=="AbilityCustomFlashStep");
            if(form)WhipHurricane01454.WithLease(__0.MaybeCaster,c=>{c.ObserveForm(__0);return false;});
        }
    }
    [HarmonyPatch(typeof(AbilityKineticist),nameof(AbilityKineticist.CalculateBurnCost),new[]{typeof(UnitDescriptor),typeof(BlueprintAbility)})]
    internal static class WhipHurricaneBurn01454
    {
        static bool Prefix(UnitDescriptor __0,BlueprintAbility __1,ref KineticistAbilityBurnCost __result)
        {
            if(__1?.GetComponent<WhipHurricaneCaster01454>()==null)return true;
            var blade=WhipHurricane01454.Blade(__0?.Unit);var activation=blade?.ActivationAbility;var blast=blade?.Blast;
            if(activation?.AbilityKineticist==null||blast?.AbilityKineticist==null)return true;
            // A form infusion replaces the blade's form cost. Run the actual
            // native handler pipeline with the associated blade identity so
            // compatible substances, metakinesis, specialization and gather
            // power retain their real filtering and arithmetic.
            var baseCost=new AbilityKineticist{BlastBurnCost=blast.AbilityKineticist.BlastBurnCost,InfusionBurnCost=4};
            __result=baseCost.CalculateBurnCost(__0,activation);
            // Existing/copy whip mechanics add one to ordinary blade attacks.
            // Hurricane's four-point form already includes its whip; remove
            // only that exact configured modifier, never an arbitrary bonus.
            foreach(var buff in __0.Buffs.Enumerable.Where(b=>b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed&&!b.IsDisposed&&(b.Blueprint.AssetGuid.ToString()==WhipHurricane01454.WhipBuffId||b.Blueprint.AssetGuid==MagusBatch01454.Identity(WhipHurricane01454.WhipToken))))
                foreach(var modifier in buff.Blueprint.GetComponents<AddKineticistBurnModifier>().Where(m=>m.BurnType==KineticistBurnType.Infusion&&m.Value==1&&m.AppliableTo.Contains(activation)))__result.Decrease(1,KineticistBurnType.Infusion);
            return false;
        }
    }
}
