// COTW-0111 Vindictive Bastard: separate class port (2026-11-08 rules pass).
// Covers: paladin spellbook/progression, retaliation-driven Vindictive Smite,
// native Inquisitor Solo Tactics, Faded Grace, Spiteful Tenacity, Stalwart,
// teamwork feats, Gang Up (move action, half smite vs one target, Cha rounds),
// Swift Justice, Aura of Self-Righteousness and Ultimate Vindication ledger.
using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype {
 internal static class VindictiveBastard0111 {
  internal const string ModuleName = "Vindictive Bastard (COTW-0111)";
  const string Prefix = "VindictiveBastard0111";
  const string PaladinClassId = "bfa11238e7ae3544bbeb4d0b92e897ec";
  const string PaladinProficienciesId = "b10ff88c03308b649b50c31611c2fefb";
  const string PaladinSpellbookId = "bce4989b070ce924b986bf346f59e885";
  const string SmiteEvilAbilityId = "7bb9eb2042e67bf489ccd1374423cdec";
  const string SmiteEvilBuffId = "b6570b8cbb32eaf4ca8255d0ec3310b0";
  const string SmiteEvilResourceId = "b4274c5bb0bf2ad4190eb7c44859048b";
  const string SmiteAdditionalUseId = "0f5c99ffb9c084545bbbe960b825d137";
  const string InquisitorSoloTacticsId = "5602845cd22683840a6f28ec46331051";
  const string DisintegrateId = "4aa7942c3e62a164387a73184bca3fc1";
  const string GreatFortitudeId = "175d1577bb6c9a04baf88eec99c66334";
  const string IronWillId = "79042cb55f030614ea29956177977c52";
  const string LightningReflexesId = "15e7da6645a7f3d41bdad7c8c4b9de1e";
  const string DiehardId = "86669ce8759f9d7478565db69b8c19ad";
  const string StalwartId = "ec9dbc9a5fa26e446a54fe5df6779088";
  const string TeamworkSelectionId = "d87e2f6a9278ac04caeb0f93eff95fcb";
  const string RootId = "2d77316c72b9ed44f888ceefc2a131f6";
  const string AuraOfRighteousnessFeatureId = "6bd4a71232014254e80726f3a3756962";
  const string AuraOfJusticeAbilityId = "7a4f0c48829952e47bb1fd1e4e9da83a";
  const string AuraOfJusticeBuffId = "ac3c66782859eb84692a8782320ffd2c";
  const string FreebooterBaneBuffId = "76dabd40a1c1c644c86ce30e41ad5cab";
  // Authoritative English text from Holic75/KingmakerRebalance Classes/VindicativeBastard.cs.
  const string ClassText = "While paladins often collaborate with less righteous adventurers in order to further their causes, those who spend too much time around companions with particularly loose morals run the risk of adopting those same unscrupulous ideologies and methods. Such a vindictive bastard, as these fallen paladins are known, strikes out for retribution and revenge, far more interested in tearing down those who have harmed her or her companions than furthering a distant deity's cause.";
  const string ProfText = "Vindictive Bastards are proficient with all simple and martial weapons, with all types of armor (heavy, medium, and light), and with shields (except tower shields).";
  const string SmiteText = "A vindictive bastard is particularly ruthless against those who have harmed her or her allies. Once per day as a swift action, she can smite one target within sight who has dealt hit point damage to her or an ally. She adds her Charisma modifier to her attack rolls and adds her paladin level to damage rolls against the target of her smite. In addition, while vindictive smite is in effect, the vindictive bastard gains a deflection bonus equal to her Charisma bonus (if any) to her AC against attacks by the target of the smite. The vindictive smite effect remains until the target of the smite is dead or the next time the vindictive bastard rests and regains her uses of this ability. At 4th level and every 3 levels thereafter, the vindictive bastard can invoke her vindictive smite one additional time per day, to a maximum of seven times per day at 19th level.";
  const string FadedText = "At 2nd level, a vindictive bastard gains one of the following as a bonus feat: Great Fortitude, Iron Will, or Lightning Reflexes.";
  const string TeamworkText = "At 3rd level and every 3 levels thereafter, the vindictive bastard gains a bonus feat in addition to those gained from normal advancement. These bonus feats must be selected from those listed as teamwork feats. The vindictive bastard must meet the prerequisites of the selected bonus feat.";
  const string SpitefulText = "At 3rd level vindictive bastard receives diehard feat for free.";
  const string GangUpText = "At 5th level, a vindictive bastard forms a close bond with her companions. This allows her to spend a move action to grant half her vindictive smite bonus against a single target to all allies who can see and hear her. This bonus lasts for a number of rounds equal to the vindictive bastard's Charisma modifier (minimum 1).";
  const string SwiftText = "At 11th level, a vindictive bastard can activate her gang up ability as a swift action.";
  const string AuraText = "At 17th level, a vindictive bastard gains DR 5/lawful or good and immunity to compulsion spells and spell-like abilities. Each ally within 10 feet of her gains a +4 morale bonus on saving throws against compulsion effects. Aura of self-righteousness functions only while the vindictive bastard is conscious, not if she is unconscious or dead.";
  const string UltimateText = "At 20th level, if a foe knocks vindictive bastard or one of a vindictive bastard's allies unconscious, the vindictive bastard musters a vindictive fury. The next time she hits that foe within 1 minute, the vindictive bastard can channel the effects of a disintegrate spell through her weapon, using her paladin level as her effective caster level. Whether or not the target succeeds at its save against the disintegrate effect, it is immune to this ability for the next 24 hours.";

  static T N<T>(BlueprintsCache cache, string id) where T : SimpleBlueprint
   => cache.Load(BlueprintGuid.Parse(id)) as T ?? throw new InvalidOperationException("COTW-0111 native anchor missing: " + id);
  static BlueprintGuid Gid(string token) => PsychicAssets.Id(Prefix + "/" + token);
  static LocalizedString Tx(string token, string part, string value) => PsychicAssets.Text(Prefix + token, part, value);
  static void Set(object o, string f, object v) => PsychicAssets.Set(o, f, v);
  static object Get(object o, string f) => PsychicAssets.Get(o, f);
  static void SetEnum(object o, string f, string name) => PsychicAssets.SetEnum(o, f, name);
  static T Rf<T>(SimpleBlueprint o) where T : BlueprintReferenceBase, new() => PsychicAssets.Ref<T>(o);
  static readonly System.Reflection.MethodInfo Memberwise = AccessTools.Method(typeof(object), "MemberwiseClone");
  static T Clone<T>(T o) where T : class => (T)Memberwise.Invoke(o, null);
  static T A<T>(T o, string token, string name, string desc) where T : BlueprintScriptableObject {
   o.AssetGuid = Gid(token); o.name = "CotWWrath" + Prefix + token;
   if (o is BlueprintUnitFact fact) { Set(fact, "m_DisplayName", Tx(token, "name", name)); Set(fact, "m_Description", Tx(token, "description", desc)); }
   return o;
  }
  static AddFacts Facts(params BlueprintUnitFact[] facts) {
   var c = new AddFacts(); Set(c, "m_Facts", facts.Select(f => Rf<BlueprintUnitFactReference>(f)).ToArray()); return c;
  }
  static LevelEntry Entry(int level, params BlueprintFeatureBase[] features) {
   var e = new LevelEntry { Level = level }; Set(e, "m_Features", features.Select(f => Rf<BlueprintFeatureBaseReference>(f)).ToList()); return e;
  }
  static AddAbilityResources Grant(BlueprintAbilityResource res) {
   var c = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true }; Set(c, "m_Resource", Rf<BlueprintAbilityResourceReference>(res)); return c;
  }
  static BlueprintFeature Feature(string token, string name, string desc, params BlueprintComponent[] components)
   => A(new BlueprintFeature { Ranks = 1, IsClassFeature = true, ComponentsArray = components }, token, name, desc);
  static BlueprintBuff Buff(string token, string name, string desc, params BlueprintComponent[] components)
   => A(new BlueprintBuff { Stacking = StackingType.Replace, ComponentsArray = components }, token, name, desc);
  static void Hide(BlueprintBuff b) => SetEnum(b, "m_Flags", "HiddenInUi");
  static BlueprintComponent Copy(BlueprintComponent c) => Clone(c);
  static void TrySet(object o, string field, object value) { if (o != null && AccessTools.Field(o.GetType(), field) != null) Set(o, field, value); }

  internal static void Register(BlueprintsCache cache) {
   var paladin = N<BlueprintCharacterClass>(cache, PaladinClassId);
   var root = N<BlueprintRoot>(cache, RootId);
   var prof = N<BlueprintFeature>(cache, PaladinProficienciesId);
   var paladinBook = N<BlueprintSpellbook>(cache, PaladinSpellbookId);
   var smiteNative = N<BlueprintAbility>(cache, SmiteEvilAbilityId);
   var smiteBuffNative = N<BlueprintBuff>(cache, SmiteEvilBuffId);
   var smiteResource = N<BlueprintAbilityResource>(cache, SmiteEvilResourceId);
   var extraUse = N<BlueprintFeature>(cache, SmiteAdditionalUseId);
   var soloNative = N<BlueprintFeature>(cache, InquisitorSoloTacticsId);
   var disintegrate = N<BlueprintAbility>(cache, DisintegrateId);
   var fort = N<BlueprintFeature>(cache, GreatFortitudeId);
   var will = N<BlueprintFeature>(cache, IronWillId);
   var reflex = N<BlueprintFeature>(cache, LightningReflexesId);
   var diehard = N<BlueprintFeature>(cache, DiehardId);
   var stalwart = N<BlueprintFeature>(cache, StalwartId);
   var teamwork = N<BlueprintFeatureSelection>(cache, TeamworkSelectionId);
   var auraJustice = N<BlueprintAbility>(cache, AuraOfJusticeAbilityId);
   var auraJusticeBuff = N<BlueprintBuff>(cache, AuraOfJusticeBuffId);
   var gangIcon = N<BlueprintBuff>(cache, FreebooterBaneBuffId).Icon;

   const string classDesc = ClassText;
   var character = new BlueprintCharacterClass {
    LocalizedName = Tx("Class", "name", "Vindictive Bastard"),
    LocalizedDescription = Tx("Class", "description", classDesc),
    LocalizedDescriptionShort = Tx("Class", "description", classDesc),
    SkillPoints = 3, IsArcaneCaster = false, IsDivineCaster = true, PrestigeClass = false, HideInUI = false,
    ClassSkills = new[] { StatType.SkillKnowledgeWorld, StatType.SkillLoreReligion, StatType.SkillPerception, StatType.SkillPersuasion },
    RecommendedAttributes = new[] { StatType.Strength, StatType.Charisma },
    NotRecommendedAttributes = Array.Empty<StatType>(),
    PrimaryColor = paladin.PrimaryColor, SecondaryColor = paladin.SecondaryColor, StartingGold = paladin.StartingGold,
    MaleEquipmentEntities = paladin.MaleEquipmentEntities, FemaleEquipmentEntities = paladin.FemaleEquipmentEntities,
    ComponentsArray = new BlueprintComponent[] { new PrerequisiteIsPet { Not = true } }
   };
   A(character, "Class", "Vindictive Bastard", classDesc);
   foreach (var field in new[] { "HitDie", "m_BaseAttackBonus", "m_FortitudeSave", "m_ReflexSave", "m_WillSave", "m_StartingItems", "m_EquipmentEntities", "m_Difficulty" })
    Set(character, field, Get(paladin, field));
   Set(character, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());

   // Paladin spellbook, spell list and spells-per-day, with the class rebound
   // and the original -3 caster level modifier preserved.
   var book = Clone(paladinBook); book.ComponentsArray = paladinBook.ComponentsArray.ToArray();
   A(book, "Spellbook", "Vindictive Bastard", "Divine spellcasting as a paladin of three levels lower.");
   Set(book, "m_CharacterClass", Rf<BlueprintCharacterClassReference>(character));
   Set(character, "m_Spellbook", Rf<BlueprintSpellbookReference>(book));

   var proficiencies = Feature("Proficiencies", "Vindictive Bastard Proficiencies",
    ProfText, Facts(prof));

   // CotW uses the native Smite Evil resource plus its Additional Use feature.

   // CotW marks a foe with a rest-cleared buff; only marked foes can be smited.
   var allowed = Buff("SmiteAllowed", "Vindictive Smite Target", "This creature has harmed you or one of your allies.");
   var flagField = AccessTools.Field(allowed.GetType(), "m_Flags");
   Set(allowed, "m_Flags", Enum.ToObject(flagField.FieldType,
    Convert.ToInt64(Enum.Parse(flagField.FieldType, "RemoveOnRest")) | Convert.ToInt64(Enum.Parse(flagField.FieldType, "HiddenInUi"))));
   // CotW removes IgnoreTargetDR from the copied native Smite Evil buff.
   // Port of CotW's AddIncomingDamageTriggerOnAttacker: a 100-foot aura puts
   // the trigger buff on the bastard and every ally; any damage they take marks
   // the attacker. This is the CotW retaliation ledger, not a weapon-hit proxy.
   var smiteTriggerBuff = Buff("SmiteTriggerBuff", "Vindictive Smite Retaliation",
    "Marks a foe that damages the bastard or an ally.",
    new CotWIncomingDamageTriggerOnAttacker0111 { Mark = Rf<BlueprintBuffReference>(allowed) }); Hide(smiteTriggerBuff);
   var smiteTriggerApply = new AbilityAreaEffectBuff { CheckConditionEveryRound = true,
    Condition = new ConditionsChecker { Conditions = new Condition[] { new ContextConditionIsAlly() } } };
   Set(smiteTriggerApply, "m_Buff", Rf<BlueprintBuffReference>(smiteTriggerBuff));
   var smiteTriggerArea = A(new BlueprintAbilityAreaEffect { Size = new Feet(100), Shape = AreaEffectShape.Cylinder,
    AffectEnemies = false, AggroEnemies = false, SpellResistance = false,
    ComponentsArray = new BlueprintComponent[] { smiteTriggerApply } }, "SmiteTriggerArea", "Vindictive Smite Retaliation", "Watches allies within 100 feet.");
   SetEnum(smiteTriggerArea, "m_TargetType", "Any");
   var smiteAreaAdd = new AddAreaEffect(); Set(smiteAreaAdd, "m_AreaEffect", Rf<BlueprintAbilityAreaEffectReference>(smiteTriggerArea));
   var smiteTriggerHost = Buff("SmiteTriggerHost", "Vindictive Smite Retaliation", "Watches allies within 100 feet.", smiteAreaAdd); Hide(smiteTriggerHost);

   var smiteBuff = Clone(smiteBuffNative);
   smiteBuff.ComponentsArray = smiteBuffNative.ComponentsArray.Select(Copy)
    .Where(c => c.GetType().Name != "IgnoreTargetDR").ToArray();
   A(smiteBuff, "SmiteBuff", "Vindictive Smite",
    "Add your Charisma modifier to attack rolls, your class level to damage, and your Charisma bonus as a deflection bonus to AC against this target.");
   foreach (var c in smiteBuff.ComponentsArray)
    if (c.GetType().Name == "ContextRankConfig" && AccessTools.Field(c.GetType(), "m_Class") != null)
     Set(c, "m_Class", new[] { Rf<BlueprintCharacterClassReference>(character) });

   var apply = new ContextActionApplyBuff { name = "CotW0111SmiteApply", IsFromSpell = false, ToCaster = false, IsNotDispelable = true,
    DurationValue = new ContextDurationValue { Rate = DurationRate.Minutes, DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = 1440 } };
   TrySet(apply, "Permanent", true);
   Set(apply, "m_Buff", Rf<BlueprintBuffReference>(smiteBuff));
   var cost = new AbilityResourceLogic { Amount = 1 }; Set(cost, "m_IsSpendResource", true); Set(cost, "m_RequiredResource", Rf<BlueprintAbilityResourceReference>(smiteResource));
   // Clone the native Smite Evil ability so its ContextCalculateSharedValue /
   // ContextRankConfig still feed the buff's StatBonus / DamageBonus values.
   var smite = Clone(smiteNative); smite.ComponentsArray = smiteNative.ComponentsArray.Select(Copy).ToArray();
   A(smite, "Smite", "Vindictive Smite", SmiteText);
   smite.ComponentsArray = smite.ComponentsArray
    .Where(c => c.GetType().Name != "AbilityCasterAlignment" && c.GetType().Name != "AbilityResourceLogic").ToArray();
   smite.ComponentsArray = smite.ComponentsArray.Concat(new BlueprintComponent[] {
    cost, new VindictiveSmiteRestriction {
     Allowed = Rf<BlueprintBuffReference>(allowed), Blocked = Rf<BlueprintBuffReference>(smiteBuff) }
   }).ToArray();
   foreach (var run in smite.ComponentsArray.Where(c => c.GetType().Name == "AbilityEffectRunAction"))
    Set(Get(run, "Actions"), "Actions", new GameAction[] { apply });
   foreach (var c in smite.ComponentsArray.Where(c => c.GetType().Name == "ContextRankConfig"))
    if (AccessTools.Field(c.GetType(), "m_Class") != null)
     TrySet(c, "m_Class", new[] { Rf<BlueprintCharacterClassReference>(character) });

   var smiteFeature = Feature("SmiteFeature", "Vindictive Smite",
    SmiteText,
    Facts(smite, smiteTriggerHost), Grant(smiteResource));

   var spiteful = Feature("SpitefulTenacity", "Spiteful Tenacity", SpitefulText, Facts(diehard));
   var teamworkFeat = Clone(teamwork); teamworkFeat.ComponentsArray = teamwork.ComponentsArray.Select(Copy).ToArray();
   A(teamworkFeat, "TeamworkFeat", "Bonus Teamwork Feat", TeamworkText);
   var faded = A(new BlueprintFeatureSelection { Ranks = 1 }, "FadedGrace", "Faded Grace",
    FadedText);
   var saves = new[] { fort, will, reflex }.Select(f => Rf<BlueprintFeatureReference>(f)).ToArray();
   Set(faded, "m_AllFeatures", saves); Set(faded, "m_Features", saves);

   // Gang Up clones the WORKING Vindictive Smite ability chain so the native
   // ContextCalculateSharedValue / ContextRankConfig still feed the buff.
   // Deep-copy the ActionList: a shallow clone would share the same Conditional
   // objects with the native Aura of Justice and with Ultimate Vindication.
   var gangBuff = Clone(auraJusticeBuff); gangBuff.ComponentsArray = auraJusticeBuff.ComponentsArray.Select(Copy).ToArray();
   A(gangBuff, "GangUpBuff", "Gang Up", GangUpText); Set(gangBuff, "m_Icon", gangIcon);
   var gangUpAbility = Clone(smite); gangUpAbility.ComponentsArray = smite.ComponentsArray.Select(Copy).ToArray();
   A(gangUpAbility, "GangUpAbility", "Gang Up", GangUpText);
   gangUpAbility.ActionType = UnitCommand.CommandType.Move;
   Set(gangUpAbility, "m_Icon", gangIcon);
   gangUpAbility.ComponentsArray = gangUpAbility.ComponentsArray
    .Where(c => c.GetType().Name != "AbilityResourceLogic" && !(c is VindictiveSmiteRestriction)).ToArray();
   foreach (var run in gangUpAbility.ComponentsArray.Where(c => c.GetType().Name == "AbilityEffectRunAction")) {
    var list = (ActionList)PsychicAssets.CopyData(Get(run, "Actions"));
    foreach (var action in (GameAction[])Get(list, "Actions")) {
     if (action.GetType().Name != "ContextActionApplyBuff") continue;
     Set(action, "m_Buff", Rf<BlueprintBuffReference>(gangBuff));
     var duration = Get(action, "DurationValue");
     Set(duration, "Rate", DurationRate.Rounds);
     Set(duration, "BonusValue", new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.SpeedBonus });
     Set(action, "DurationValue", duration);
    }
    Set(run, "Actions", list);
   }
   foreach (var c in gangUpAbility.ComponentsArray.Where(c => c.GetType().Name == "ContextRankConfig")) {
    TrySet(c, "m_UseMin", true); TrySet(c, "m_Min", 1);
    if (AccessTools.Field(c.GetType(), "m_Progression") != null) SetEnum(c, "m_Progression", "Div2");
   }
   // Independent Charisma rank (minimum 1) controls the duration in rounds.
   var speedRank = new ContextRankConfig(); Set(speedRank, "m_Type", AbilityRankType.SpeedBonus);
   SetEnum(speedRank, "m_BaseValueType", "StatBonus"); Set(speedRank, "m_Stat", StatType.Charisma);
   Set(speedRank, "m_UseMin", true); Set(speedRank, "m_Min", 1);
   gangUpAbility.ComponentsArray = gangUpAbility.ComponentsArray.Concat(new BlueprintComponent[] { speedRank }).ToArray();
   var gangUp = Feature("GangUp", "Gang Up", GangUpText, Facts(gangUpAbility));
   var swiftAbility = Clone(gangUpAbility); swiftAbility.ComponentsArray = gangUpAbility.ComponentsArray.Select(Copy).ToArray();
   A(swiftAbility, "SwiftJusticeAbility", "Swift Justice", SwiftText);
   swiftAbility.ActionType = UnitCommand.CommandType.Swift;
   Set(swiftAbility, "m_Icon", gangIcon);
   var swiftJustice = Feature("SwiftJustice", "Swift Justice", SwiftText, Facts(swiftAbility));

   // Direct clone of the native Paladin Aura of Righteousness feature. CotW did
   // exactly this in Kingmaker; the same four blueprints and GUIDs exist in Wrath.
   var auraNative = N<BlueprintFeature>(cache, AuraOfRighteousnessFeatureId);
   var aura = Clone(auraNative); aura.ComponentsArray = auraNative.ComponentsArray.Select(Copy).ToArray();
   A(aura, "Aura", "Aura of Self-Righteousness", AuraText);
   foreach (var c in aura.ComponentsArray) {
    if (c.GetType().Name != "AddDamageResistancePhysical") continue;
    TrySet(c, "BypassedByAlignment", true);
    var field = AccessTools.Field(c.GetType(), "Alignment");
    if (field != null) {
     long good = Convert.ToInt64(Enum.Parse(field.FieldType, "Good"));
     long lawful = Convert.ToInt64(Enum.Parse(field.FieldType, "Lawful"));
     Set(c, "Alignment", Enum.ToObject(field.FieldType, good | lawful));
    }
   }

   // Ultimate Vindication, following CotW :424-492 as closely as Wrath allows.
   // The hit trigger sits directly on the feature (not behind AddFacts), and the
   // 24h immunity is appended to Disintegrate's own effect actions.
   var ultimateMark = Buff("UltimateMark", "Ultimate Vindication Target", "This foe has downed you or an ally."); Hide(ultimateMark);
   var ultimateCooldown = Buff("UltimateCooldown", "Ultimate Vindication Immunity", "Immune to Ultimate Vindication for 24 hours."); Hide(ultimateCooldown);
   var ultimateSpell = Clone(disintegrate);
   ultimateSpell.ComponentsArray = disintegrate.ComponentsArray.Select(Copy)
    .Where(c => c.GetType().Name != "SpellListComponent" && c.GetType().Name != "AbilityDeliverProjectile").ToArray();
   A(ultimateSpell, "UltimateSpell", "Ultimate Vindication", UltimateText);
   ultimateSpell.Type = AbilityType.Supernatural;
   Set(ultimateSpell, "m_Icon", disintegrate.Icon);
   var ultimateParams = new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Charisma };
   Set(ultimateParams, "m_CharacterClass", Rf<BlueprintCharacterClassReference>(character));
   ultimateSpell.ComponentsArray = ultimateSpell.ComponentsArray.Concat(new BlueprintComponent[] { ultimateParams }).ToArray();
   var cooldownApply = new ContextActionApplyBuff { name = "CotW0111UltimateCooldown", IsFromSpell = false,
    ToCaster = false, IsNotDispelable = true,
    DurationValue = new ContextDurationValue { Rate = DurationRate.Days, DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = 1 } };
   Set(cooldownApply, "m_Buff", Rf<BlueprintBuffReference>(ultimateCooldown));
   foreach (var run in ultimateSpell.ComponentsArray.Where(c => c.GetType().Name == "AbilityEffectRunAction")) {
    var list = (ActionList)PsychicAssets.CopyData(Get(run, "Actions"));
    var acts = (GameAction[])Get(list, "Actions");
    Set(list, "Actions", acts.Concat(new GameAction[] { cooldownApply }).ToArray());
    Set(run, "Actions", list);
   }

   var cast = new ContextActionCastSpell { name = "CotW0111UltimateCast" }; Set(cast, "m_Spell", Rf<BlueprintAbilityReference>(ultimateSpell));
   var hasMark = new ContextConditionHasBuff { name = "CotW0111HasMark" }; Set(hasMark, "m_Buff", Rf<BlueprintBuffReference>(ultimateMark));
   var noCooldown = new ContextConditionHasBuffFromCaster { name = "CotW0111NoCooldown", Not = true };
   Set(noCooldown, "m_Buff", Rf<BlueprintBuffReference>(ultimateCooldown));
   var hitConditional = new Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional {
    name = "CotW0111UltimateConditional",
    ConditionsChecker = new ConditionsChecker { Conditions = new Condition[] { hasMark, noCooldown } },
    IfTrue = new ActionList { Actions = new GameAction[] { cast } },
    IfFalse = new ActionList { Actions = Array.Empty<GameAction>() } };
   var hitTrigger = new AddInitiatorAttackWithWeaponTrigger { name = "CotW0111UltimateHit", OnlyHit = true,
    Action = new ActionList { Actions = new GameAction[] { hitConditional } } };

   // CotW's AddIncomingDamageTriggerOnAttacker with reduce_below0=true: the
   // 100-foot aura puts the trigger on the bastard and all allies.
   var ultimateTriggerBuff = Buff("UltimateTriggerBuff", "Ultimate Vindication Trigger",
    "Marks a foe that knocks the bastard or an ally unconscious or dead.",
    new CotWIncomingDamageTriggerOnAttacker0111 { Mark = Rf<BlueprintBuffReference>(ultimateMark), OnlyLethal = true, Minutes = 1f }); Hide(ultimateTriggerBuff);
   var ultimateTriggerApply = new AbilityAreaEffectBuff { CheckConditionEveryRound = true,
    Condition = new ConditionsChecker { Conditions = new Condition[] { new ContextConditionIsAlly() } } };
   Set(ultimateTriggerApply, "m_Buff", Rf<BlueprintBuffReference>(ultimateTriggerBuff));
   var ultimateTriggerArea = A(new BlueprintAbilityAreaEffect { Size = new Feet(100), Shape = AreaEffectShape.Cylinder,
    AffectEnemies = false, AggroEnemies = false, SpellResistance = false,
    ComponentsArray = new BlueprintComponent[] { ultimateTriggerApply } }, "UltimateTriggerArea", "Ultimate Vindication Trigger", "Watches allies within 100 feet.");
   SetEnum(ultimateTriggerArea, "m_TargetType", "Any");
   var ultimateAreaAdd = new AddAreaEffect(); Set(ultimateAreaAdd, "m_AreaEffect", Rf<BlueprintAbilityAreaEffectReference>(ultimateTriggerArea));
   var ultimateTriggerHost = Buff("UltimateTriggerHost", "Ultimate Vindication Trigger", "Watches allies within 100 feet.", ultimateAreaAdd); Hide(ultimateTriggerHost);

   var ultimate = Feature("Ultimate", "Ultimate Vindication", UltimateText,
    hitTrigger, Facts(ultimateTriggerHost));
   Set(ultimate, "m_Icon", disintegrate.Icon);

   var entries = new List<LevelEntry>();
   for (int level = 1; level <= 20; level++) {
    var at = new List<BlueprintFeatureBase>();
    if (level == 1) { at.Add(proficiencies); at.Add(smiteFeature); }
    if (level == 2) { at.Add(faded); at.Add(soloNative); }
    if (level == 3) { at.Add(spiteful); at.Add(teamworkFeat); }
    if (level == 4 || level == 7 || level == 10 || level == 13 || level == 16 || level == 19) at.Add(extraUse);
    if (level == 5) at.Add(gangUp);
    if (level == 6 || level == 9 || level == 12 || level == 15 || level == 18) at.Add(teamworkFeat);
    if (level == 11) at.Add(swiftJustice);
    if (level == 14) at.Add(stalwart);
    if (level == 17) at.Add(aura);
    if (level == 20) at.Add(ultimate);
    entries.Add(Entry(level, at.ToArray()));
   }
   var progression = A(new BlueprintProgression { Ranks = 1, LevelEntries = entries.ToArray(), UIGroups = Array.Empty<UIGroup>() },
    "Progression", "Vindictive Bastard", classDesc);
   Set(progression, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Rf<BlueprintCharacterClassReference>(character) } });
   Set(progression, "m_UIDeterminatorsGroup", new[] { Rf<BlueprintFeatureBaseReference>(proficiencies) });
   Set(character, "m_Progression", Rf<BlueprintProgressionReference>(progression));

   var assets = new BlueprintScriptableObject[] { character, book, progression, proficiencies, allowed,
    smiteTriggerBuff, smiteTriggerArea, smiteTriggerHost,
    smiteBuff, smite, smiteFeature, faded, gangBuff, gangUpAbility, gangUp, swiftAbility, swiftJustice,
    spiteful, teamworkFeat, aura, ultimateMark, ultimateCooldown, ultimateSpell,
    ultimateTriggerBuff, ultimateTriggerArea, ultimateTriggerHost, ultimate };
   // Owlcat's EntityFact.ComponentsDictionary is keyed by component name;
   // a null name makes saving throw "Value cannot be null. Parameter name: key".
   int componentIndex = 0;
   foreach (var bp in assets) {
    var componentsProperty = bp.GetType().GetProperty("ComponentsArray");
    if (componentsProperty == null || !componentsProperty.CanRead) continue;
    var components = componentsProperty.GetValue(bp) as BlueprintComponent[];
    if (components == null) continue;
    foreach (var component in components)
     if (component != null && string.IsNullOrEmpty(component.name))
      component.name = "CotW0111Component" + (componentIndex++).ToString();
   }
   foreach (var bp in assets) bp.OnEnable();
   var previous = (BlueprintCharacterClassReference[])Get(root.Progression, "m_CharacterClasses") ?? Array.Empty<BlueprintCharacterClassReference>();
   var added = new List<BlueprintGuid>();
   try {
    foreach (var bp in assets) if (cache.Load(bp.AssetGuid) == null) { cache.AddCachedBlueprint(bp.AssetGuid, bp); added.Add(bp.AssetGuid); }
    if (!previous.Any(r => r?.Guid == character.AssetGuid))
     Set(root.Progression, "m_CharacterClasses", previous.Concat(new[] { Rf<BlueprintCharacterClassReference>(character) }).ToArray());
   } catch {
    Set(root.Progression, "m_CharacterClasses", previous);
    foreach (var id in added) cache.RemoveCachedBlueprint(id);
    throw;
   }
  }
 }
 [TypeId("8ba9b8aa60074a7baa5be097a0c7fb03")]
 public sealed class VindictiveSmiteRestriction : BlueprintComponent, IAbilityTargetRestriction {
  public BlueprintBuffReference Allowed, Blocked;
  public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target) {
   var unit = target?.Unit;
   if (unit == null || Allowed == null) return false;
   if (Blocked != null && unit.Buffs.Enumerable.Any(b => b.IsActive && !b.IsSuppressed && b.Blueprint.AssetGuid == Blocked.Guid)) return false;
   return unit.Buffs.Enumerable.Any(b => b.IsActive && !b.IsSuppressed && b.Blueprint.AssetGuid == Allowed.Guid);
  }
  public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target)
   => "The target must have harmed you or one of your allies.";
 }
 [TypeId("907fd20803514b1a98606e39aa7821d8")]
 public sealed class CotWIncomingDamageTriggerOnAttacker0111 : UnitFactComponentDelegate,
  ITargetRulebookHandler<RuleDealDamage>, ITargetRulebookSubscriber {
  public BlueprintBuffReference Mark;
  public bool OnlyLethal;
  public float Minutes = 1440f;
  public void OnEventAboutToTrigger(RuleDealDamage e) { }
  public void OnEventDidTrigger(RuleDealDamage e) {
   if (Owner == null || Mark == null) return;
   var attacker = e.Initiator; var victim = e.Target;
   if (attacker == null || victim == null || attacker.IsAlly(Owner)) return;
   if (victim != Owner && !victim.IsAlly(Owner)) return;
   if (e.Result <= 0) return;
   if (OnlyLethal && !(victim.HPLeft <= 0 || victim.State.IsDead || victim.State.IsUnconscious)) return;
   var bp = Mark.Get(); if (bp == null) return;
   if (attacker.Buffs.Enumerable.Any(b => b.IsActive && b.Blueprint.AssetGuid == Mark.Guid)) return;
   var context = e.Reason?.Context ?? Context;
   if (context == null) return;
   var added = attacker.Buffs.AddBuff(bp, context, TimeSpan.FromMinutes(Minutes));
   if (added != null) added.IsNotDispelable = true;
  }
 }
}
