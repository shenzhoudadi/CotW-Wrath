using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static void MutationBodies057(BlueprintsCache c,BlueprintArchetype root)
    {
        var physical=A<BlueprintFeature>(c,"PhysicalMutationFeature01457");var ability=A<BlueprintAbility>(c,"PhysicalMutationAbility01457");var resource=A<BlueprintAbilityResource>(c,"PhysicalMutationResource01457");
        string[] choices=(string[])AccessTools.Field(T("MutationMind01457"),"Mutations").GetValue(null);
        var rootBlueprint=BlueprintRoot.Instance;var oldLimits=AccessTools.Field(typeof(BlueprintRoot),"m_PersonageLimits").GetValue(rootBlueprint);
        Set(rootBlueprint,"m_PersonageLimits",BlueprintReferenceBase.CreateTyped<BlueprintPersonageLimitsReference>(N<Kingmaker.Dungeon.Blueprints.BlueprintPersonageLimits>(c,"2a609e8ce9844f98ae11097bef7be8bb")));
        try{
        foreach(string key in choices){
            var u=EquipmentUnit(c,16,"83fa3b50138c566698801ba5967bbde0");u.Progression.Classes[0].AddArchetype(root);u.Stats.Strength.BaseValue=12;u.Stats.Dexterity.BaseValue=14;u.Body.EmptyHandWeapon.OnDidEquipped(u);
            Grant(u,physical);Grant(u,c,"BodilyMutationsFeature01457");Grant(u,c,"ImprovedBodilyMutationsFeature01457");Grant(u,c,key+"BodilyMutationFeature01457");
            int speed=u.Stats.Speed.ModifiedValue,ac=u.Stats.AC.ModifiedValue,cmd=u.Stats.AdditionalCMD.ModifiedValue,perception=u.Stats.SkillPerception.ModifiedValue;
            var empty=u.Body.EmptyHandWeapon;int limbs=u.Body.AdditionalLimbs.Count(v=>v.HasItem);
            StartMutation057Or058(u,new AbilityData(ability,u.Descriptor));var main=u.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("PhysicalMutationBuff01457"));var body=u.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id(key+"BodilyMutationBuff01457"));
            Check(body.IsActive&&body.IsTurnedOn&&body.IsPermanent&&u.Buffs.Enumerable.Count(b=>b.Blueprint.name.Contains("BodilyMutationBuff01457"))==1,"actual selected mutation activates exactly one native permanent child: "+key);
            if(key=="BiteAttack")Check(u.Body.AdditionalLimbs.Count(v=>v.HasItem)==limbs+1&&u.Body.AdditionalLimbs.Any(v=>v.MaybeWeapon?.Blueprint.Category==WeaponCategory.Bite),"native bite limb exists only during physical mutation");
            if(key=="Claws"){Console.WriteLine("057 claws actual "+u.Body.EmptyHandWeapon.Blueprint.name+" category="+u.Body.EmptyHandWeapon.Blueprint.Category+" form="+u.Body.EmptyHandWeapon.Blueprint.DamageType.Physical.Form+" stack="+string.Join(",",u.Body.EmptyHandWeaponsStack.Select(v=>v.Blueprint.name)));Check(u.Body.EmptyHandWeapon.Blueprint.Category==WeaponCategory.Claw&&u.Body.EmptyHandWeapon.Blueprint.DamageType.Physical.Form==(PhysicalDamageForm.Bludgeoning|PhysicalDamageForm.Slashing),"native empty-hand override preserves the original combined bludgeoning/slashing claw");}
            if(key=="ElongatedLegs")Check(u.Stats.Speed.ModifiedValue==speed+10,"native elongated legs increases actual base speed by ten");
            if(key=="ElongatedTorso")Check(u.Stats.AC.ModifiedValue==ac+2,"native elongated torso grants two natural AC");
            if(key=="Slimy")Check(u.Stats.AdditionalCMD.ModifiedValue==cmd+4,"native slimy mutation adds four CMD");
            if(key=="EnlargedBody")Check(u.Stats.Strength.ModifiedValue==24&&u.Stats.Dexterity.ModifiedValue==12,"native enlarged body adds four size Strength and subtracts two Dexterity alongside alchemical mutation");
            if(key=="MultipleEyes")Check(u.Stats.SkillPerception.ModifiedValue==perception+4&&u.Descriptor.State.Features.CannotBeFlanked,"native multiple eyes grants perception and actual cannot-be-flanked flag");
            if(key=="Rubbery")Check(body.Components.Any(v=>v.SourceBlueprintComponent is Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical),"native physical DR component actually activates for rubbery mutation");
            if(key=="Recuperation")Check(body.Components.Any(v=>v.SourceBlueprintComponent is Kingmaker.UnitLogic.Buffs.Components.AddEffectFastHealing),"native fast-healing component actually activates for recuperation");
            if(key=="Wings"){var visual=body.Blueprint.ComponentsArray.OfType<Kingmaker.UnitLogic.FactLogic.AddEquipmentEntity>().Single();Check(body.Components.Count>0&&visual.EquipmentEntity.AssetId=="23b3eed8f78e69c40a2c6d416cac2f9e"&&body.Blueprint.FxOnStart!=null&&body.Blueprint.FxOnRemove!=null,"native wing equipment visual component activates with safe optional particle links");}
            main.Remove();Check(body.IsDisposed&&!u.Buffs.Enumerable.Any(b=>b.Blueprint.name.Contains("BodilyMutationBuff01457")),"actual parent expiry removes selected mutation and every owned child: "+key);
            foreach(var b in u.Buffs.Enumerable.Where(b=>b.Blueprint.AssetGuid==BlueprintGuid.Parse("e6f2fc5d73d88064583cb828801212f4")).ToArray())b.Remove();
            Check(u.Stats.Speed.ModifiedValue==speed&&u.Stats.AdditionalCMD.ModifiedValue==cmd&&u.Stats.SkillPerception.ModifiedValue==perception,"native mutation cleanup restores speed/CMD/perception: "+key);
            if(key=="BiteAttack")Check(u.Body.AdditionalLimbs.Count(v=>v.HasItem)==limbs,"native bite removal balances the extra limb");
            if(key=="Claws")Check(u.Body.EmptyHandWeapon.Blueprint==empty.Blueprint,"native claw removal restores original empty-hand weapon");
        }
        var mixed=EquipmentUnit(c,1,"83fa3b50138c566698801ba5967bbde0");mixed.Progression.Classes.Add(new ClassData(N<BlueprintCharacterClass>(c,"45a4607686d96a1498891b3286121780")){Level=20});Grant(mixed,physical);
        Check(resource.GetMaxAmount(mixed.Descriptor)==1,"twenty unrelated class levels do not increase mutation minutes");
        var data=new AbilityData(ability,mixed.Descriptor);StartMutation057Or058(mixed,data);if(MutationPolicy058)EmptyMutationPool058(mixed,data);else Check(!ability.GetComponent<AbilityResourceLogic>().IsAbilityRestrictionPassed(data),"actual native resource restriction rejects exhausted mutation pool");
        foreach(var b in mixed.Buffs.Enumerable.ToArray())b.Remove();
        var exhausted=Add(mixed,N<BlueprintBuff>(c,"46d1b9cc3d0fd36469a471b047d773a2"));Check(!ability.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.Base.IAbilityCasterRestriction>().All(v=>v.IsCasterRestrictionPassed(mixed)),"native caster checks reject exhaustion");exhausted.Remove();
        }finally{Set(rootBlueprint,"m_PersonageLimits",oldLimits);}
    }
}
