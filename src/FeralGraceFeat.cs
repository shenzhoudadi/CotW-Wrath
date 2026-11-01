using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Parts;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class FeralGraceFeat
    {
        internal const string ModuleName = "Feral Grace";
        internal const string Token = "FeralGraceFeat";
        // Wrath's player companions level in AnimalCompanionClass. AnimalClass
        // belongs to monster animals and is retained only for existing custom pets.
        internal const string AnimalClassId = "26b10d4340839004f960f9816f6109fe";
        internal const string LegacyAnimalClassId = "4cd1757a0eea7694ba5c933729a53920";
        internal const string WeaponFinesseId = "90e54424d682d104ab36436bd527af09";
        internal const string WeaponFocusId = "1e1f627d26ad36f43bbd26cc2bf8ac7e";
        internal const int MakingFriendsEidolonPetType = 604767;
        // Exact optional provider identities, from Making Friends' GUIDs.json.
        internal static readonly string[][] EidolonClasses = {
            new[]{"075743fe93d54a58bef4fae948513a6f", "EidolonBaseClass"},
            new[]{"2c8e7db74d8541ccb60c720b037b5c96", "EidolonAbberantClass"},
            new[]{"6f4b088a6bee44ef9623821f97b610e4", "EidolonBipedClass"},
            new[]{"0c910ba2dc154810b185f17ad03cb690", "EidolonQuadrupedClass"},
            new[]{"e4396e852244492d8e7970468d10e1b2", "EidolonSerpentineClass"}
        };
        internal const string Description = "Choose one natural attack category. When this animal companion or supported eidolon uses Dexterity for a melee attack with that category and Strength for its damage, it adds one quarter of its own Hit Dice (rounded down) as a bonus to damage. This flat bonus does not change for primary or secondary natural attacks. Requires Weapon Finesse and base attack bonus +3. You may take this feat again for a different natural attack category; the same category does not stack. Dexterity-to-damage attacks do not gain this bonus.";

        internal static void Register(BlueprintsCache cache)
        {
            var selections = new[]{BlueprintRegistration.BasicFeatsId}.Concat(BlueprintRegistration.NativeCombatFeatSelectionIds)
                .Select(id=>cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection ?? throw new InvalidOperationException("Feral Grace selection missing: " + id)).ToArray();
            var f = new PsychicAssets(cache);
            f.Native<BlueprintCharacterClass>(AnimalClassId);
            var finesse = f.Native<BlueprintFeature>(WeaponFinesseId);
            var template = f.Native<BlueprintParametrizedFeature>(WeaponFocusId);
            if (template.ParameterType != FeatureParameterType.WeaponCategory) throw new InvalidOperationException("Feral Grace weapon parameter template differs");
            var need = new PrerequisiteFeature(); Set(need, "m_Feature", Ref<BlueprintFeatureReference>(finesse));
            var feature = f.Add(new BlueprintParametrizedFeature {
                ParameterType = FeatureParameterType.WeaponCategory, WeaponSubCategory = WeaponSubCategory.Natural,
                RequireProficiency = template.RequireProficiency, Ranks = 1, Groups = new[]{FeatureGroup.Feat,FeatureGroup.CombatFeat},
                ComponentsArray = new BlueprintComponent[]{need, new PrerequisiteStatValue{Stat=StatType.BaseAttackBonus,Value=3},
                    // Native IsPet uses the misleading "No pet" UI string. The
                    // visible companion prerequisite below states the same identity clearly.
                    new PrerequisiteIsPet { HideInUI = true }, new FeralGraceCompanionPrerequisite(), new FeralGraceDamage()}
            }, Token, "Feral Grace", Description);
            Set(feature, "m_Icon", PsychicOriginalIcons.Load("FeralGrace"));
            PsychicRepairComponents.UniqueNames(feature);
            var previous = cache.Load(feature.AssetGuid);
            if (previous != null && (!(previous is BlueprintParametrizedFeature owned) || owned.name != feature.name ||
                owned.ParameterType != feature.ParameterType || owned.WeaponSubCategory != feature.WeaponSubCategory ||
                owned.ComponentsArray == null || feature.ComponentsArray.Any(expected=>!owned.ComponentsArray.Any(c=>c!=null && c.GetType()==expected.GetType() && c.name==expected.name))))
                throw new InvalidOperationException("Feral Grace GUID/component collision; existing asset preserved: " + feature.AssetGuid);
            var actual = (BlueprintParametrizedFeature)(previous ?? feature);
            f.Enable();
            var arrays = selections.Select(s=>new[]{Get(s,"m_Features"),Get(s,"m_AllFeatures")}).ToArray();
            try {
                if (previous == null) cache.AddCachedBlueprint(feature.AssetGuid, feature);
                T09CombatFeats.Append(selections, new BlueprintFeature[]{actual});
            } catch {
                for(int i=0;i<selections.Length;i++){Set(selections[i],"m_Features",arrays[i][0]);Set(selections[i],"m_AllFeatures",arrays[i][1]);}
                if(previous==null)cache.RemoveCachedBlueprint(feature.AssetGuid);
                throw;
            }
        }

        internal static void Optional(BlueprintsCache cache)
        {
            var selection = cache.Load(BlueprintGuid.Parse(BlueprintRegistration.BrawlerCombatFeatSelectionId)) as BlueprintFeatureSelection;
            var feature = cache.Load(Id(Token)) as BlueprintParametrizedFeature;
            if (selection != null && feature != null) T09CombatFeats.Append(new[]{selection},new BlueprintFeature[]{feature});
        }

        internal static bool SupportedCompanion(UnitEntityData unit)
        {
            var pet = unit?.Get<UnitPartPet>() ?? unit?.PreviewOf.Entity?.Get<UnitPartPet>();
            if(pet==null || unit.Progression==null)return false;
            var animal = ResourcesLibrary.BlueprintsCache.Load(BlueprintGuid.Parse(AnimalClassId)) as BlueprintCharacterClass;
            if(pet.Type == PetType.AnimalCompanion) {
                if(animal!=null && unit.Progression.GetClassLevel(animal)>0)return true;
                var legacy = ResourcesLibrary.BlueprintsCache.Load(BlueprintGuid.Parse(LegacyAnimalClassId)) as BlueprintCharacterClass;
                return legacy!=null && unit.Progression.GetClassLevel(legacy)>0;
            }
            if((int)pet.Type != MakingFriendsEidolonPetType)return false;
            foreach(var entry in EidolonClasses) {
                var character = ResourcesLibrary.BlueprintsCache.Load(BlueprintGuid.Parse(entry[0])) as BlueprintCharacterClass;
                if(character?.name==entry[1] && unit.Progression.GetClassLevel(character)>0)return true;
            }
            return false;
        }
    }

    [TypeId("f6c7dde4307943f48fd875e9157dfd97")]
    public sealed class FeralGraceCompanionPrerequisite : Prerequisite
    {
        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
            => FeralGraceFeat.SupportedCompanion(unit?.Unit);
        protected override string GetUITextInternal(UnitDescriptor unit) => "Animal companion (learned by the companion) or supported Making Friends eidolon";
    }

    [TypeId("e234feb35bde40c189a7d85994dcd4c0")]
    public sealed class FeralGraceDamage : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>, ISubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }
        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            // The completed native attack rule supplies the actual attack stat;
            // its already-completed weapon stats supply the actual damage stat.
            // This avoids guessing from Dexterity/Strength or adding the bonus
            // before Agile/Mythic Weapon Finesse replaces the damage stat.
            var weapon = evt.Weapon;
            var damage = evt.WeaponStats;
            if(!Fact.IsActive || !Fact.IsTurnedOn || Fact.IsDisposed || !Owner.Facts.List.Contains(Fact) || evt.Initiator!=Owner || weapon==null || !weapon.Blueprint.IsMelee || !weapon.Blueprint.IsNatural || weapon.Blueprint.IsUnarmed ||
                weapon.Blueprint.Category!=Param || evt.AttackBonusStat!=StatType.Dexterity || damage.DamageBonusStat!=StatType.Strength ||
                !FeralGraceFeat.SupportedCompanion(Owner))return;
            var finesse = ResourcesLibrary.BlueprintsCache.Load(BlueprintGuid.Parse(FeralGraceFeat.WeaponFinesseId)) as BlueprintFeature;
            if(finesse==null || !Owner.HasFact(finesse))return;
            int bonus = Owner.Progression.CharacterLevel / 4;
            if(bonus<=0 || damage.DamageDescription.Count==0)return;
            var description = damage.DamageDescription[0];
            if(description.Modifiers.Any(m=>m.Fact?.Blueprint?.AssetGuid==Fact.Blueprint.AssetGuid))return;
            // A damage modifier is independent of Strength's 0.5/1/1.5 multiplier.
            description.AddModifier(new Modifier(bonus,Fact,ModifierDescriptor.UntypedStackable));
        }
    }
}
