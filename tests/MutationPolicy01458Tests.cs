using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs;

internal static partial class MagusBatch01454Tests
{
    static bool MutationPolicy058=>Main.GetType("CallOfTheWild.Wrath.Prototype.PhysicalMutationToggle01458")!=null;
    static int MutationChoiceCount058=>MutationPolicy058?12:11;
    static void StartMutation057Or058(UnitEntityData owner,AbilityData ability)
    {
        ApplySelf(owner,ability);
        if(MutationPolicy058){var toggle=owner.ActivatableAbilities.Enumerable.Single(v=>v.Blueprint.AssetGuid==Id("PhysicalMutationToggle01458"));Check(toggle.IsOn,"actual legacy mutation shortcut switches on the058 persistent toggle");toggle.TryStart();Check(toggle.IsStarted,"actual native058 toggle starts through its command-ready lifecycle");}
    }
    static void RepeatMutation058(UnitEntityData owner,AbilityData ability,Buff effect)
    {
        var pool=A<Kingmaker.Blueprints.BlueprintAbilityResource>(active,"PhysicalMutationResource01457");int before=owner.Resources.GetResourceAmount(pool);var end=effect.EndTime;
        ApplySelf(owner,ability);Check(effect.IsAttached&&effect.EndTime==end&&owner.Resources.GetResourceAmount(pool)==before,"058 legacy shortcut toggles off without removing or changing already paid minute");
        StartMutation057Or058(owner,ability);Check(effect.IsAttached&&effect.EndTime==end&&owner.Resources.GetResourceAmount(pool)==before,"058 legacy shortcut reopens remaining paid minute without resetting or charging it");
    }
    static void EmptyMutationPool058(UnitEntityData owner,AbilityData ability)
    {
        var toggle=owner.ActivatableAbilities.Enumerable.Single(v=>v.Blueprint.AssetGuid==Id("PhysicalMutationToggle01458"));var paid=owner.Buffs.Enumerable.Single(v=>v.Blueprint.AssetGuid==Id("PhysicalMutationBuff01457"));
        Check(toggle.IsAvailableByRestrictions,"058 exhausted resource pool can still retain or resume its already paid minute");paid.Remove();foreach(var b in owner.Buffs.Enumerable.ToArray())b.Remove();
        Check(!toggle.IsAvailableByRestrictions,"058 zero resource without a paid minute cannot start a free mutation");
    }
}
