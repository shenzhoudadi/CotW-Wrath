// Test-only: precise optional-entry boundary for existing scoped fixtures.
// Real Urban registration/native tests and the complete order model do not bind it.
using System;using System.Reflection;using HarmonyLib;
internal static class ScopedAvailable01463Tests{
 internal static void Bind(Harmony owner,Type registrar,string prefix){owner.Patch(AccessTools.Method(registrar,"RegisterAvailableModule"),prefix:new HarmonyMethod(typeof(ScopedAvailable01463Tests),prefix));}
 static bool Scope(string name,Type fixture,string method){
  if(name!="Bloodrager archetype: Urban Bloodrager"&&name!="Psychic archetype: Amnesiac")return true;
  var scope=AccessTools.Method(fixture,method);object[] args;
  if(scope.GetParameters().Length==1)args=new object[]{name};
  else args=new object[]{name,(Action)(()=>{throw new InvalidOperationException("A scoped non-target Urban action must never execute.");})};
  bool run=(bool)scope.Invoke(null,args);
  if(run){if(args.Length!=2)throw new InvalidOperationException("Urban unexpectedly belongs to a legacy scoped target set.");((Action)args[1])();}
  // No new action and no fabricated registration report. Owner.UnpatchAll
  // removes this exact hook with the fixture's already existing lifecycle.
  return false;
 }
 static bool ScopeArchetypes01444Tests(string name)=>Scope(name,typeof(Archetypes01444Tests),"Scope");
 static bool ScopeHedgeWitch01443Tests(string name)=>Scope(name,typeof(HedgeWitch01443Tests),"Scope");
 static bool ScopeIncreasedDamageReduction01439Tests(string name)=>Scope(name,typeof(IncreasedDamageReduction01439Tests),"Scope");
 static bool ScopeMigration01434CallerTests(string name)=>Scope(name,typeof(Migration01434CallerTests),"CallerScope");
 static bool ScopeMigration01435CallerTests(string name)=>Scope(name,typeof(Migration01435CallerTests),"CallerScope");
 static bool ScopeMigration01437CallerTests(string name)=>Scope(name,typeof(Migration01437CallerTests),"Scope");
 static bool ScopeMigration01443CallerTests(string name)=>Scope(name,typeof(Migration01443CallerTests),"Scope");
 static bool ScopeRegistrationFeatKi01432Tests(string name)=>Scope(name,typeof(RegistrationFeatKi01432Tests),"CallerScope");
 static bool ScopeRegistrationWarmup01443Tests(string name)=>Scope(name,typeof(RegistrationWarmup01443Tests),"Scope");
 static bool ScopeRepair01436CallerTests(string name)=>Scope(name,typeof(Repair01436CallerTests),"CallerScope");
 static bool ScopeRepair01438CallerTests(string name)=>Scope(name,typeof(Repair01438CallerTests),"Scope");
 static bool ScopeStartupArcane01446Tests(string name)=>Scope(name,typeof(StartupArcane01446Tests),"ModuleBoundary");
 static bool ScopeStartupArchetypes01445Tests(string name)=>Scope(name,typeof(StartupArchetypes01445Tests),"ModuleBoundary");
 static bool ScopeStartupRetry01444Tests(string name)=>Scope(name,typeof(StartupRetry01444Tests),"ModuleBoundary");
 static bool ScopeStartupSpellBatch01447Tests(string name)=>Scope(name,typeof(StartupSpellBatch01447Tests),"ModuleBoundary");
}
