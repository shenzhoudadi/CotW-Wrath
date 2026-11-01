using Mono.Cecil;
using Mono.Cecil.Cil;

// Links the action-economy fixture to real blueprint/native command contracts.
internal static class FleetContracts
{
    static IEnumerable<TypeDefinition> All(TypeDefinition t) => new[] {t}.Concat(t.NestedTypes.SelectMany(All));
    static IEnumerable<MethodReference> Calls(MethodDefinition m) => m.Body.Instructions.Select(i=>i.Operand).OfType<MethodReference>();
    static int? Number(Instruction i) => i.OpCode.Code switch {
        Code.Ldc_I4_0=>0,Code.Ldc_I4_1=>1,Code.Ldc_I4_2=>2,Code.Ldc_I4_3=>3,
        Code.Ldc_I4_S=>Convert.ToInt32(i.Operand),Code.Ldc_I4=>(int)i.Operand,_=>null};
    public static void Run(AssemblyDefinition game,AssemblyDefinition mod,Action<bool,string> check)
    {
        var blueprint=mod.MainModule.Types.Single(t=>t.FullName=="MediumClass.Medium.Spirits.Champion.FleetCharge");
        var build=blueprint.Methods.Single(m=>m.Name=="ConfigureEnabled");
        void Option(string method,int value) {
            var call=build.Body.Instructions.Last(i=>i.Operand is MethodReference r&&r.Name==method);
            check(Number(call.Previous)==value,"Fleet blueprint "+method+" = "+value);
        }
        Option("SetActionType",1);Option("SetIsFullRoundAction",0);Option("SetRange",9);
        Option("SetType",3); // Supernatural; no psychic spell components or spell failure.
        Option("SetCanTargetEnemies",1);Option("SetCanTargetSelf",0);Option("SetCanTargetFriends",0);Option("SetCanTargetPoint",0);
        const string name="MediumClass.Medium.NewComponents.AbilitySpecific.ChampionFleetChargeLogic";
        check(Calls(build).OfType<GenericInstanceMethod>().Any(m=>m.Name=="AddComponent"&&m.GenericArguments.Any(t=>t.FullName==name)),"Fleet blueprint wires actual delivery");
        check(!Calls(build).Any(m=>m.Name=="MeleeAttack"),"old melee-only duplicate action removed");
        var swift=build.Body.Instructions.First(i=>i.Operand is MethodReference r&&r.Name=="SetActionType");
        check(Number(swift.Previous)==2,"separate preparation uses native swift command");
        check(Calls(build).Count(m=>m.Name=="AddAbilityEffectRunAction")==1,"only preparation uses a buff action; attack uses native weapon command");
        check(Calls(build).Any(m=>m.Name=="AddFactContextActions"),"preparation cleaned when the granting spirit power ends");
        var logic=mod.MainModule.Types.Single(t=>t.FullName==name);
        var methods=All(logic).SelectMany(t=>t.Methods).Where(m=>m.HasBody).ToArray();
        var calls=methods.SelectMany(Calls).ToArray();
        check(!calls.Any(m=>m.Name=="set_SwiftAction"||m.Name=="SpendAction") && !logic.Interfaces.Any(i=>i.InterfaceType.Name=="IAbilityOnCastLogic"),"attack cannot manually overwrite native swift cost");
        check(calls.Any(m=>m.Name=="GetBuff")&&calls.Any(m=>m.Name=="Remove"),"attack requires and consumes preparation");
        check(!calls.Any(m=>m.Name=="set_IsCharging"),"Fleet never changes the charging state used by Sudden Attack");
        check(!calls.Any(m=>m.Name=="Run"&&m.DeclaringType.Name=="UnitCommands"),"child attack is never queued for another standard/full-round cost");
        check(calls.Any(m=>m.Name=="Tick"&&m.DeclaringType.Name=="UnitCommand"),"native attack tick owns weapon animations and projectiles");
        var enumerator=methods.Single(m=>m.Name=="MoveNext");
        foreach(var pair in new[]{("set_ForceFullAttack",1),("set_IsSingleAttack",0),("set_IsCharge",0)}){
            var i=enumerator.Body.Instructions.Single(x=>x.Operand is MethodReference r&&r.Name==pair.Item1);
            check(Number(i.Previous)==pair.Item2,"Fleet command "+pair.Item1+" = "+pair.Item2);
        }
        var attack=game.MainModule.Types.Single(t=>t.FullName=="Kingmaker.UnitLogic.Commands.UnitAttack");
        var init=attack.Methods.Single(m=>m.Name=="InitAttacks");
        var ordered=Calls(init).Select(m=>m.Name).ToList();
        check(ordered.IndexOf("get_ForceFullAttack")<ordered.IndexOf("get_IsSingleAttack")&&ordered.Contains("CreateFullAttack"),"native forced full attack precedes ordinary single/move restrictions");
        check(Calls(attack.Methods.Single(m=>m.Name=="CreateFullAttack")).Any(m=>m.Name=="EnumerateFullAttack"),"native command uses full attack enumerator");
        var full=All(attack).Single(t=>t.Name.StartsWith("<EnumerateFullAttack>"));
        check(Calls(full.Methods.Single(m=>m.Name=="MoveNext")).Any(m=>m.DeclaringType.Name=="RuleCalculateAttacksCount"),"full attack dispatches the rule handled by Sudden Attack");
        var baseCommand=game.MainModule.Types.Single(t=>t.FullName=="Kingmaker.UnitLogic.Commands.Base.UnitCommand");
        check(!Calls(baseCommand.Methods.Single(m=>m.Name=="Tick")).Any(m=>m.Name=="SpendAction"),"direct native tick does not charge an action");
        var controller=game.MainModule.Types.Single(t=>t.FullName=="Kingmaker.Controllers.Units.UnitCommandController");
        var spend=controller.Methods.Single(m=>m.Name=="TickCommand");
        check(Calls(spend).Any(m=>m.Name=="SpendAction")&&Calls(spend).Any(m=>m.Name=="IsFullRoundAction")&&Calls(spend).Any(m=>m.Name=="get_IsIgnoreCooldown"),"outer ability uses native action/full-round cost controller");
        var surge=mod.MainModule.Types.Single(t=>t.FullName=="MediumClass.Medium.SpiritSurge").Methods.Single(m=>m.Name=="ConfigureEnabled");
        check(!Calls(surge).OfType<GenericInstanceMethod>().Any(m=>m.GenericArguments.Any(a=>a.Name=="AbilityRequirementNotMarshalsOrder")),"Marshal no longer removes access to self Spirit Surge");
        check(Calls(surge).Any(m=>m.Name=="AddAbilityResourceLogic"),"self Spirit Surge retains influence resource payment");
        check(Calls(surge).OfType<GenericInstanceMethod>().Any(m=>m.GenericArguments.Any(a=>a.Name=="AbilityRequirementHasSpirit")),"self surge still requires a channeled spirit");
        var legacy=mod.MainModule.Types.Single(t=>t.FullName=="MediumClass.Medium.MediumPsychicBlueprints");
        check(legacy.Methods.Where(m=>m.HasBody).SelectMany(Calls).Any(m=>m.Name=="get_Integrated"),"legacy Logical registration follows integrated mode");
        var facts=game.MainModule.Types.Single(t=>t.FullName=="Kingmaker.UnitLogic.FactLogic.AddFacts");
        check(Calls(facts.Methods.Single(m=>m.Name=="OnApplyPostLoadFixes")).Any(m=>m.Name=="UpdateFacts"),"native AddFacts repairs missing grant on old-save post-load");
        var data=game.MainModule.Types.Single(t=>t.FullName=="Kingmaker.UnitLogic.Abilities.AbilityData");
        check(data.Methods.Where(m=>m.HasBody).Any(m=>Calls(m).Any(r=>r.Name=="get_Range")&&Calls(m).Any(r=>r.Name=="GetFirstWeapon")&&Calls(m).Any(r=>r.Name=="get_AttackRange")),"native weapon range follows equipped weapon attack range");
    }
}
