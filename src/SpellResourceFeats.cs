using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class SpellResourceFeats {
  internal const string ModuleName="Spell resource feats";
  internal const string Bloodrager="d77e67a814d686842802c9cfd8ef8499";
  internal static readonly string[] Tokens={"AmplifiedHexFeat","RageCastingFeat"};
  internal static readonly HashSet<string> HexIds=new HashSet<string>{"1af015a346ef453d8f0151343c27efd9","2b35e887d852cb949a1d8e308efe8065","ff1cf8952b8551f4c9fbe32966f9a945","8bc94d17348204041ab8863d4f841a99","5e02868509087b248a2c6daad16e9aef","137c8f8182413dc4e80abe2a9d0313b5","6cd07c80aabf2b248a11921090de9c17","9802c5950219c6c41a9293076e221d12","66a62322b44f25c418472df9a77f5b3a","e66d3b4d6669f9f4ea79056d4a525098","3e427e1fb78631c4b9f17e39bc19c96f","c2fa8c5d307efbb41a15d55373f414e2","c91f9e996c0af364dae41569cb9586e7","313c37a6410ac514b835490cf768794f","3d4c6361e60fa664db01d5709baaa812","8b9ffc4cd53b82f44ab7c0ddca85877d","e3eddc31b2430b7408c62f313efea4bc","d7767bd68ca218c4cb99e35b1cc50d8c","07e1c038726e0824eabaa39e6038a854","29b3a96956eaf8d4cb42205ee233a158","8e7f48a657b6380429dc1fc685096288","98293e0bfb86f4f418dcfbe7bf7f8fc2","1d33fcbc7f5175446bb9577e0f7811b3","a824b1806059bd54db0b5b0093472f21","487981678b77e0b4aa5ff0328f50ecb2","ba28f4a77f3741c4187b283d42e22554","e80c565458d9c0c4ea1323dc8cf122d9","4de66387a2c36624385930b0f5e596af","dcabd85e723bac941bc5807a6a953e89","ff2f3177d34778b41b5bfe0b140cba42","5617c38bde7d56f4094b2fc714399566","3e14f46a35228b946af0c1fe79870e54","1b1e0ae5916e6564ba3f0e2ab4fc403a","48f55c7834a2fe845928337f091a0932","c9097a1d0685f2f468ab63c1138815ad","b1505c6512a0f2c4581dfa6af14e9d5c","43738e1ea3c328549bcdd81883841e05","e98cf086294d6134bb76d35e090cb059","4bd01292a9bc4304f861a6a07f03b855","d25c72a92dd8d38449a6a371ef36413e","899b08dfc31868e4cb2c6287df9d355c","954650e8a7542e642819716bb78bee86","ba52aed3017521a4abafcbae4ee06d10","eaf7077a8ff35644883df6d4f7b2084c","ed4fbfcdb0f5dcb41b76d27ed00701af","46cf5c995494e784c8d9a1696f9c61a7","e7ecd11651b4df34897f33271a8d1cfc","630ea63a63457ff4f9de059c578c930a","8f0eb58c2d6aeab4e8523ec85b4b2bc7","5adb5a0650f5b2049bab1afe822bd3bd","4a511e1dfeec46c4b867bf61b15eae2b","d560ab2a1b0613649833a0d92d6cfc6b","1bb5466b9bfcb5e47b9f667dad5784f9","cedc4959ab311d548881844eecddf57a","0d38e470e350ce34f869c20002d45763","3cae80cecdc2fb84b86587032132d48f","b5816eb8a1c295b42b1edb0b9f48dfd8","fcaaf1f25440a8f40ae424c7708e9a0f","3108e18055b46b84f8a57898ad5ad075","e7489733ac7ccca40917d9364b406adb","7244a24f0c186ce4b8a89fd26feded50","5fa471f18a914f743b1200b7520c215d","e946e3fdc42ea65408daee0f029b4100","0e8f3d24a2cea644e91273f6c551152d","05bba992bea833349a246fd47042748c","1411baa7c0e58f547b76b6b13e5443f3","3408c351753aa9049af25af31ebef624","40d201c6fbbb46e46a63dec8508de65a","b5c4c51cc9993b14b9dc0f0c54199d09","0a6effb356101cc46aa0bed8c3ab6fd4","a69fb167bb41c6f45a19c81ed4e3c0d9"};
  internal static bool Hex(BlueprintAbility bp)=>bp!=null&&(HexIds.Contains(bp.AssetGuid.ToString())||bp.Parent!=null&&HexIds.Contains(bp.Parent.AssetGuid.ToString())||PublishedHexes.Contains(bp.AssetGuid));
  // Hexes published by this project are recognised by their ShamanHex group and
  // by the fact that they carry a cooldown lock, not by a frozen GUID list. The
  // list above only covers Owlcat's own table, which is why a hex we author could
  // never be amplified.
  internal static readonly HashSet<BlueprintGuid> PublishedHexes=new HashSet<BlueprintGuid>();
  internal static void PublishHex(BlueprintAbility ability) {
   if (ability != null) PublishedHexes.Add(ability.AssetGuid);
  }
  internal static AmplifiedHexDailyState Daily(UnitEntityData owner) {
   var fact=owner?.Facts.List.FirstOrDefault(f=>T11TrainingOptions.Active(owner,f)&&f.Blueprint.AssetGuid==Id("AmplifiedHexFeat"));
   return fact?.Components.FirstOrDefault(c=>c.SourceBlueprintComponent is AmplifiedHexDaily)?.GetData<AmplifiedHexDailyState>();
  }
  internal static bool HexArmed(UnitEntityData owner)=>owner?.Buffs?.Enumerable.Any(b=>b.Blueprint.AssetGuid==Id("AmplifiedHexBuff")&&b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed)==true;
  internal static void Clear(UnitEntityData owner,IEnumerable<BlueprintGuid> ids) {
   var set=new HashSet<BlueprintGuid>(ids);if(owner?.Buffs!=null)foreach(var b in owner.Buffs.Enumerable.Where(b=>set.Contains(b.Blueprint.AssetGuid)).ToArray())b.Remove();
  }
  internal static void Commit(BlueprintsCache cache,PsychicAssets f,BlueprintFeature[] roots) {
   var pools=new[]{BlueprintRegistration.BasicFeatsId}.Concat(BlueprintRegistration.NativeCombatFeatSelectionIds).Select(id=>f.Native<BlueprintFeatureSelection>(id)).ToArray();
   var before=pools.Select(s=>new[]{Get(s,"m_Features"),Get(s,"m_AllFeatures")}).ToArray();var added=new List<BlueprintGuid>();
   foreach(var bp in f.Assets.Values){PsychicRepairComponents.UniqueNames(bp);var old=cache.Load(bp.AssetGuid);if(old!=null&&(old.GetType()!=bp.GetType()||old.name!=bp.name||bp.ComponentsArray.Any(c=>!((BlueprintScriptableObject)old).ComponentsArray.Any(x=>x!=null&&x.GetType()==c.GetType()&&x.name==c.name))))throw new InvalidOperationException("Feat identity collision: "+bp.AssetGuid);}
   f.Enable();try{foreach(var bp in f.Assets.Values)if(cache.Load(bp.AssetGuid)==null){cache.AddCachedBlueprint(bp.AssetGuid,bp);added.Add(bp.AssetGuid);}var actual=roots.Select(x=>(BlueprintFeature)cache.Load(x.AssetGuid)).ToArray();T09CombatFeats.Append(pools.Take(1).ToArray(),actual);T09CombatFeats.Append(pools.Skip(1).ToArray(),actual.Where(x=>x.Groups.Contains(FeatureGroup.CombatFeat)).ToArray());}
   catch{for(int i=0;i<pools.Length;i++){Set(pools[i],"m_Features",before[i][0]);Set(pools[i],"m_AllFeatures",before[i][1]);}foreach(var id in added)cache.RemoveCachedBlueprint(id);throw;}
  }
  internal static void Register(BlueprintsCache cache) {
   var f=new PsychicAssets(cache);var blood=f.Native<BlueprintCharacterClass>(Bloodrager);var icon=f.Native<BlueprintAbility>(T11TrainingOptions.Demoralize).Icon;
   const string ampText="Sacrifice any prepared spell or spontaneous spell slot of level 1 to 9 to amplify your next native witch or shaman hex. All three benefits apply together: saving throw DC +slot level, finite effect duration +slot level rounds (if originally at least 1 round), and range +10 feet for levels 1-3, +20 feet for 4-6, or +30 feet for 7-9 (if originally at least 30 feet). Personal/touch range, instantaneous/permanent effects and target cooldowns are unchanged. This is an empowered variant of the tabletop feat. There is no increasing slot-level requirement or daily use cap. Use the spell conversion menu; the sacrificed spell is not cast. Only one hex can be armed. Other spells and abilities do not consume it. Requires a learned supported hex.";
   var ampBuff=f.Buff("AmplifiedHexBuff","Amplified Hex: Next Hex",ampText,new AmplifiedHexBoost());ampBuff.Stacking=StackingType.Replace;SetEnum(ampBuff,"m_Flags","RemoveOnRest");
   var convert=f.Ability("AmplifiedHexConversion","Amplified Hex",ampText,AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Personal,new AmplifiedHexConversion());convert.NotOffensive=true;
   var cancel=f.Ability("AmplifiedHexCancel","Amplified Hex: Dismiss", "Dismiss the armed hex without refunding its spell slot.",AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Personal,new AmplifiedHexDismiss());cancel.NotOffensive=true;
   var amp=f.Feature("AmplifiedHexFeat","Amplified Hex",ampText,new PrerequisiteSupportedHex(),new AmplifiedHexDaily(),Facts(convert,cancel));amp.Groups=new[]{FeatureGroup.Feat};
   const string rageText="As a swift action, arm your next bloodrager spell for 1 round. Choose 1 to 4 damage dice; the actual number is capped by that spell's slot level. Gain that many points of saving throw DC and lose that many d6 hit points when the spell is attempted. This loss bypasses temporary hit points, resistances and damage reduction and cannot be reduced by damage modifiers. Items, cantrips and spells from other classes do not trigger it. Only one choice can be armed. Requires bloodrager level 4. A native failed spell that still spends its slot also pays this cost; canceled commands and forced failures do not.";
   var buffs=new List<BlueprintBuff>();var abilities=new List<BlueprintAbility>();
   for(int dice=1;dice<=4;dice++)buffs.Add(f.Buff("RageCasting"+dice+"Buff","Rage Casting: "+dice+"d6",rageText,new RageCastingBoost{Dice=dice}));
   foreach(var b in buffs){b.Stacking=StackingType.Replace;SetEnum(b,"m_Flags","RemoveOnRest");}
   for(int dice=1;dice<=4;dice++) {
    var a=f.Ability("RageCasting"+dice+"Ability","Rage Casting: "+dice+"d6",rageText,AbilityType.Supernatural,UnitCommand.CommandType.Swift,AbilityRange.Personal,new RageCastingArm{Dice=dice});a.NotOffensive=true;abilities.Add(a);
   }
   var variants=new AbilityVariants();Set(variants,"m_Variants",abilities.Select(Ref<BlueprintAbilityReference>).ToArray());var parent=f.Ability("RageCastingAbility","Rage Casting",rageText,AbilityType.Supernatural,UnitCommand.CommandType.Swift,AbilityRange.Personal,variants);parent.NotOffensive=true;foreach(var a in abilities)Set(a,"m_Parent",Ref<BlueprintAbilityReference>(parent));
   var cleanup=new SpellFeatCleanup{Buffs=buffs.Select(Ref<BlueprintBuffReference>).ToArray()};var rage=f.Feature("RageCastingFeat","Rage Casting",rageText,T11TrainingOptions.Level(blood,4),cleanup,Facts(parent));rage.Groups=new[]{FeatureGroup.Feat};
   foreach(var bp in f.Assets.Values)Set(bp,"m_Icon",icon);Commit(cache,f,new[]{amp,rage});
  }
  internal static void Optional(BlueprintsCache cache) {} // These are general feats, not Brawler combat feats.
 }
 public sealed class AmplifiedHexDailyState {[JsonProperty]public int Uses;}
 [TypeId("9b40c8c5d51d45a783674b6b0d8349a1")]
 public sealed class PrerequisiteSupportedHex:Prerequisite {
  protected override bool CheckInternal(Kingmaker.UnitLogic.Class.LevelUp.FeatureSelectionState selectionState,UnitDescriptor unit,Kingmaker.UnitLogic.Class.LevelUp.LevelUpState state)=>unit?.Unit?.Abilities?.Enumerable.Any(a=>a.IsActive&&a.IsTurnedOn&&SpellResourceFeats.Hex(a.Blueprint))==true;
  protected override string GetUITextInternal(UnitDescriptor unit)=>"A learned native witch or shaman hex";
 }
 [TypeId("c6e65d1cf4d14a15a958a3422d57039c")]
 public sealed class AmplifiedHexDaily:UnitFactComponentDelegate<AmplifiedHexDailyState>,IUnitRestHandler,IGlobalSubscriber,ISubscriber {
  public void HandleUnitRest(UnitEntityData unit){if(unit==Owner){Data.Uses=0;SpellResourceFeats.Clear(Owner,new[]{Id("AmplifiedHexBuff")});}}
  protected override void OnTurnOff(){SpellResourceFeats.Clear(Owner,new[]{Id("AmplifiedHexBuff")});}
 }
 [TypeId("e3d1b79cfe6945bb889b52754f2023ab")]
 public sealed class AmplifiedHexConversion:AbilityConvertSpellLevelToResource,IAbilityRestriction,IAbilityVisibilityProvider {
  public new bool IsAbilityVisible(AbilityData ability){var d=SpellResourceFeats.Daily(ability?.Caster?.Unit);return d!=null&&!SpellResourceFeats.HexArmed(ability.Caster.Unit)&&ability.ConvertedFrom?.SpellLevel>=1&&ability.ConvertedFrom.SpellLevel<=9;}
  public bool IsAbilityRestrictionPassed(AbilityData ability)=>IsAbilityVisible(ability)&&ability.ConvertedFrom?.Spellbook!=null&&ability.ConvertedFrom.SourceItem==null&&ability.ConvertedFrom.Spellbook.CanSpend(ability.ConvertedFrom);
  public string GetAbilityRestrictionUIText()=>"Sacrifice a level 1-9 spell slot; only one amplified hex can be armed.";
  public override void Apply(AbilityExecutionContext context,TargetWrapper target) {
   var owner=context.MaybeCaster;var d=SpellResourceFeats.Daily(owner);var source=context.Ability?.ConvertedFrom;
   if(d==null||source?.Spellbook==null||source.SourceItem!=null||source.SpellLevel<1||source.SpellLevel>9||SpellResourceFeats.HexArmed(owner))return;
   var bp=Kingmaker.Blueprints.ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(Id("AmplifiedHexBuff"));var b=owner.Buffs.AddBuff(bp,context,null);if(b!=null){b.IsNotDispelable=true;b.Components.Single(c=>c.SourceBlueprintComponent is AmplifiedHexBoost).GetData<SpellBoostState>().Level=source.SpellLevel;if(d.Uses<int.MaxValue)d.Uses++;}
  }
 }
 [TypeId("84d0518946f6467e83bf74cac2cedd35")]
 public sealed class AmplifiedHexDismiss:AbilityApplyEffect {
  public override void Apply(AbilityExecutionContext context,TargetWrapper target)=>SpellResourceFeats.Clear(context.MaybeCaster,new[]{Id("AmplifiedHexBuff")});
 }
 public sealed class SpellBoostState {[JsonProperty]public bool Consumed;[JsonProperty]public int Level=1;}
 [TypeId("0ff016b2d744421aaa2d5a745f26540b")]
 public sealed class AmplifiedHexBoost:UnitBuffComponentDelegate<SpellBoostState> {}
 [TypeId("03bac87b2e784be8929b5c57415ca4dd")]
 public sealed class SpellFeatCleanup:UnitFactComponentDelegate {
  public BlueprintBuffReference[] Buffs=Array.Empty<BlueprintBuffReference>();
  protected override void OnTurnOff()=>SpellResourceFeats.Clear(Owner,Buffs.Select(b=>b.Guid));
 }
 [TypeId("38e7d1a5c4274cd0a314f0c6272d1b7a")]
 public sealed class RageCastingBoost:UnitBuffComponentDelegate<SpellBoostState> {
  public int Dice;
 }
 [TypeId("c3e72b69d27b4b7394a34190483f4ac2")]
 public sealed class RageCastingArm:AbilityApplyEffect {
  public int Dice;
  public override void Apply(AbilityExecutionContext context,TargetWrapper target) {
   var u=context.MaybeCaster;if(u==null)return;SpellResourceFeats.Clear(u,Enumerable.Range(1,4).Select(i=>Id("RageCasting"+i+"Buff")));
   var b=u.Buffs.AddBuff(Kingmaker.Blueprints.ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(Id("RageCasting"+Dice+"Buff")),context,TimeSpan.FromSeconds(6));if(b!=null)b.IsNotDispelable=true;
  }
 }
}
