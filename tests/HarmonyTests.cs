using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UI.MVVM._VM.Settings;

// Run in a fresh process per Harmony DLL; reuse the identical compiled mod DLL.
// Patch real target methods without invoking them, then execute a harmless test
// method to verify interception and owner-scoped cleanup on the chosen runtime.
internal static class HarmonyTests
{
    private static string managedPath, modPath, harmonyPath;
    private static string sceneFixture058;
    private static int postfixCalls;
    private static int Main(string[] args)
    {
        if (args.Length != 3 && args.Length != 5) { Console.Error.WriteLine("Expected game Managed directory, mod DLL, and Harmony DLL paths, optionally audited Unity fixture and SHA."); return 2; }
        managedPath = Path.GetFullPath(args[0]); modPath = Path.GetFullPath(args[1]); harmonyPath = Path.GetFullPath(args[2]);
        if(args.Length==5){sceneFixture058=Path.GetFullPath(args[3]);using(var algorithm=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(sceneFixture058)){var hash=BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-","");if(hash!=args[4]||sceneFixture058==Path.Combine(managedPath,"UnityEngine.CoreModule.dll")||Path.GetFileName(sceneFixture058)!="UnityEngine.CoreModule.dll")throw new Exception("058 standalone scene dependency does not match its audited bytes.");}}
        AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
        {
            var name = new AssemblyName(eventArgs.Name).Name;
            if (name == "0Harmony") return Assembly.LoadFrom(harmonyPath);
            if (name == "UnityEngine.CoreModule" && sceneFixture058 != null) return Assembly.LoadFrom(sceneFixture058);
            foreach (var directory in new[] { Path.GetDirectoryName(harmonyPath), managedPath, Path.GetDirectoryName(modPath) })
            {
                var path = Path.Combine(directory, name + ".dll");
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        try { if(sceneFixture058!=null)Assembly.LoadFrom(sceneFixture058);Assembly.LoadFrom(harmonyPath); Run(); return 0; }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        if(sceneFixture058!=null){if(Path.GetFullPath(typeof(UnityEngine.Component).Assembly.Location)!=sceneFixture058)throw new Exception("058 Unity fixture was not the actual loaded dependency.");Console.WriteLine("SCENE_058_FIXTURE="+typeof(UnityEngine.Component).Assembly.Location);}
        var runtime = typeof(Harmony).Assembly;
        if(runtime.GetName().Version==new Version(2,0,4,0))RequiredHarmonyRuntime.Verify(harmonyPath);
        else if(runtime.GetName().Version==new Version(2,3,6,0)&&Path.GetFullPath(runtime.Location)==harmonyPath) {
            var verify=Assembly.LoadFrom(modPath).GetType("CotW.Runtime.HarmonyRuntimeCompatibility").GetMethod("Verify",BindingFlags.Static|BindingFlags.NonPublic,null,Type.EmptyTypes,null);verify.Invoke(null,null);
        } else throw new Exception("Unexpected isolated runtime: "+runtime.FullName);
        // A test-local copy is permitted only when byte-identical to the game
        // runtime; RequiredHarmonyRuntime verifies the bytes and logs its path.
        var mod = Assembly.LoadFrom(modPath);
        var referenced = mod.GetReferencedAssemblies().Single(a => a.Name == "0Harmony");
        if (referenced.Version != new Version(2,0,4,0))
            throw new Exception("Compile API baseline changed: " + referenced);
        const string owner = "CotWWrathPrototype.IsolatedHarmonyTest";
        var harmony = new Harmony(owner);
        var targets = new MethodBase[] {
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Commands.Base.UnitCommand),"CommandTargetUntargetable"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Buffs.BuffCollection),"PrepareFactForAttach",new[]{typeof(Kingmaker.UnitLogic.Buffs.Buff)}),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.FactLogic.GhostCriticalAndPrecisionImmunity), "OnEventAboutToTrigger"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.FactLogic.AddIncorporealDamageDivisor), "OnEventAboutToTrigger"),
            AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.RuleCalculateAttackBonusWithoutTarget), "OnTrigger"),
            typeof(BlueprintsCache).GetMethod(nameof(BlueprintsCache.Init)),
            typeof(LocalizationManager).GetMethod(nameof(LocalizationManager.WaitForInit)),
            typeof(LocalizationManager).GetMethod("OnLocaleChanged", BindingFlags.Static | BindingFlags.NonPublic),
            typeof(SettingsVM).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single(),
            typeof(ContextRankConfig).GetMethod("GetBaseValue", BindingFlags.Instance | BindingFlags.NonPublic),
            AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Abilities.AbilityData), "IsAvailableForCast"),
            AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Abilities.AbilityData), "CanBeCastByCaster"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.AbilityData), "CalcSpellSource"),
            AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.Abilities.RuleCalculateAbilityParams), "OnTrigger"),
            AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.RuleCollectMetamagic), "AddMetamagic"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.MetamagicHelper), "DefaultCost"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.MetamagicHelper), "SpellIcon"),
            AccessTools.Method(typeof(Kingmaker.UI.Common.UIUtilityTexts), "GetMetamagicName"),
            AccessTools.Method(typeof(Kingmaker.UI.Common.UIUtilityTexts), "GetMetamagicList"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Spellbook), "AddKnown"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Spellbook), "RemoveSpell"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Spellbook), "PostLoad"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.UnitHelper), "CopySpellbook"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Class.LevelUp.Actions.SelectSpell), "Apply"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Class.LevelUp.Actions.ApplySpellbook), "Apply"),
            AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.RuleRollDice), "OnTrigger"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.ContextDurationValue), "Calculate"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Parts.UnitPartConcealment), "Calculate"),
            AccessTools.Method(typeof(Kingmaker.Designers.Mechanics.Facts.AutoMetamagic), "ShouldApplyTo"),
            AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Abilities.AbilityData), "ActionType"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.Components.CasterCheckers.AbilityCasterHasNoFacts), "IsCasterRestrictionPassed"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.AbilityCollection), "PrepareFactForDetach", new[]{typeof(Kingmaker.UnitLogic.Abilities.Ability)}),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.UnitDescriptor), "ApplyPostLoadFixes"),
            AccessTools.Method(typeof(Kingmaker.Controllers.AbilityExecutionProcess), "DoApplyEffect"),
            AccessTools.Method(typeof(Kingmaker.EntitySystem.Entities.AreaEffectEntityData), "TryOvercomeSpellResistance"),
            AccessTools.Method(typeof(Kingmaker.UI.MVVM._VM.ActionBar.ActionBarSlotVM), "OnMainClick"),
            AccessTools.Method(typeof(Kingmaker.UI.MVVM._VM.ActionBar.ActionBarSlotVM), "OnShowConvertRequest"),
            AccessTools.Method(typeof(Kingmaker.UI.MVVM._VM.ActionBar.ActionBarSlotVM), "UpdateResource"),
            AccessTools.Method(typeof(Kingmaker.UI.MVVM._VM.ActionBar.ActionBarVM), "CollectAbilities"),
            AccessTools.Method(typeof(Kingmaker.UI.UnitSettings.MechanicActionBarSlotActivableAbility), "GetResource")
        };
        targets = targets.Concat(new[]{AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetHasFact),"IsTargetRestrictionPassed"),AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.RuleAttackRoll),"TryOvercomeTargetConcealmentAndMissChance"),AccessTools.Method(typeof(Kingmaker.EntitySystem.Entities.AreaEffectEntityData),"IsSuitableTargetType"),AccessTools.Method(typeof(Kingmaker.Assets.Controllers.GlobalMap.GlobalMapMovementController), "CalcPlayerSpeedModifiers")}).ToArray();
        targets=targets.Concat(new MethodBase[]{
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"CanTarget",new[]{typeof(Kingmaker.Utility.TargetWrapper)}),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"CantTargetReason",new[]{typeof(Kingmaker.Utility.TargetWrapper)}),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"ValidateUnit",new[]{typeof(Kingmaker.EntitySystem.Entities.UnitEntityData)}),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction),"Apply"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.Components.AbilityEffectStickyTouch),"Apply"),
            AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"Spellbook"),
            AccessTools.PropertyGetter(typeof(Kingmaker.EntitySystem.Entities.UnitEntityData),"IsDirectlyControllable")
        }).ToArray();
        targets=targets.Concat(new MethodBase[]{
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Parts.UnitPartPet),"Initialize"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Parts.UnitPartPet),"OnPostLoad"),
            AccessTools.Method(typeof(TurnBased.Controllers.TurnController),"ForceToEnd")
        }).ToArray();
        targets=targets.Concat(new[]{AccessTools.Method(typeof(Kingmaker.Controllers.Units.UnitCommandController),"TickCommandTurnBased")}).ToArray();
        targets=targets.Concat(new[]{AccessTools.Method(typeof(Kingmaker.Blueprints.Root.Strings.GameLog.AttackLogMessage),"AppendAttackBonusBreakdown")}).ToArray();
        targets=targets.Concat(new[]{AccessTools.Method(typeof(Kingmaker.Controllers.Clicks.Handlers.ClickUnitHandler),"GetPriority"),AccessTools.Method(typeof(Kingmaker.View.UnitEntityView),"HandleHoverChange")}).ToArray();
        targets=targets.Concat(new[]{AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.Combat.UnitCombatState),"IsFlanked")}).ToArray();
        targets=targets.Concat(new[]{AccessTools.Method(typeof(Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeature),"CheckInternal"),AccessTools.Method(typeof(Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeaturesFromList),"CheckInternal")}).ToArray();
        targets=targets.Concat(new[]{AccessTools.Method(typeof(Kingmaker.EntitySystem.Stats.ModifiableValueSkill),"CalculateBaseValue",new[]{typeof(int)}),AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"RequireFullRoundAction")}).ToArray();
        targets=targets.Concat(new MethodBase[]{
            AccessTools.Constructor(typeof(Kingmaker.UI.Tooltip.StatTooltipData),new[]{typeof(Kingmaker.EntitySystem.Stats.ModifiableValueSkill)}),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Class.LevelUp.Actions.SelectFeature),"Apply"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Class.LevelUp.Actions.ApplySkillPoints),"Apply"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Class.LevelUp.Actions.SpendSkillPoint),"Check"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"CalculateParams"),
            AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell),"OnTrigger"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.MechanicsContext),"Recalculate"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"GetApproachDistance"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff),"CalculateDuration")
        }).ToArray();
        targets=targets.Concat(new MethodBase[]{
            AccessTools.Method(typeof(Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection),"ExtractSelectionItems"),
            AccessTools.PropertyGetter(typeof(Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection),"Items"),
            AccessTools.Method(typeof(Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection),"CanSelect")
        }).ToArray();
        targets=targets.Concat(new MethodBase[]{
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic),"CalculateCost"),
            AccessTools.Method(typeof(Kingmaker.Items.ItemEntity),"AddEnchantment"),
            AccessTools.Method(typeof(Kingmaker.Items.ItemEntity),"RemoveEnchantment"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionCombatManeuverCustom),"RunAction"),
            AccessTools.Method(typeof(Kingmaker.Designers.Mechanics.Facts.ManeuverTrigger),"OnEventDidTrigger"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage),"RunAction"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff),"RunAction")
        }).ToArray();
        targets=targets.Concat(new MethodBase[]{
            AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"Range"),
            AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"TargetAnchor"),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff),"GetBuffTarget"),
            AccessTools.Method(typeof(TurnBased.Controllers.TurnController),"End")
        }).ToArray();
        targets=targets.Concat(new MethodBase[]{AccessTools.Method(typeof(Kingmaker.UnitLogic.Parts.UnitPartBuffSuppress),"ApplyChanges")}).ToArray();
        targets=targets.Concat(new MethodBase[]{AccessTools.Method(typeof(Kingmaker.Blueprints.Classes.Selection.BlueprintParametrizedFeature),"ExtractSpellsFromSpellbooks"),AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"RequireFullRoundAction"),AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.Damage.RuleHealDamage),"OnTrigger")}).ToArray();
        targets=targets.Concat(new[]{AccessTools.Method(typeof(Kingmaker.UnitLogic.FeatureCollection),"PrepareFactForAttach",new[]{typeof(Kingmaker.UnitLogic.Feature)})}).Distinct().ToArray();
        targets=targets.Concat(new[]{AccessTools.Method(typeof(Kingmaker.UnitLogic.FactLogic.AddMagusMechanicPart),"OnDeactivate")}).Distinct().ToArray();
        targets=targets.Concat(new[]{AccessTools.Method(typeof(StartGameLoader),nameof(StartGameLoader.LoadPackTOC))}).Distinct().ToArray();
        targets=targets.Concat(new[]{AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.MechanicsContext),nameof(Kingmaker.UnitLogic.Mechanics.MechanicsContext.TryAffectByShadow))}).Distinct().ToArray();
        targets=targets.Concat(new[]{AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff),nameof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff.RunAction))}).Distinct().ToArray();
        targets=targets.Concat(new[]{AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionCastSpell),nameof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionCastSpell.RunAction))}).Distinct().ToArray();
        targets=targets.Concat(new MethodBase[]{
            AccessTools.Method(typeof(Kingmaker.Blueprints.Classes.Selection.BlueprintParametrizedFeature),"CanSelect"),
            AccessTools.Method(typeof(Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteParametrizedFeature),"CheckInternal")
        }).Distinct().ToArray();
        // 036 adds three unique native targets; AddKnown/Remove/PostLoad,
        // FeatureCollection handoff and discovery CanSelect already exist above.
        targets=targets.Concat(new MethodBase[]{
            AccessTools.Method(typeof(Kingmaker.UnitLogic.UnitDescriptor),"DeleteSpellbook",new[]{typeof(Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook)}),
            AccessTools.Method(typeof(Kingmaker.UnitLogic.Class.LevelUp.Actions.SelectSpell),"Check",new[]{typeof(Kingmaker.UnitLogic.Class.LevelUp.LevelUpState),typeof(Kingmaker.UnitLogic.UnitDescriptor)}),
            AccessTools.Method(typeof(Kingmaker.UI.MVVM._VM.CharGen.Phases.Spells.CharGenSpellsPhaseVM),"CreateSpellVMsCollection",new[]{typeof(Kingmaker.UnitLogic.Class.LevelUp.LevelUpController),typeof(Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList)})
        }).Distinct().ToArray();
        // 038 has six engine patch methods, but only three new native targets.
        // CommandTargetUntargetable, PrepareFactForAttach and CalculateDuration
        // already belong to the preserved target inventory above.
        var timeStopPatches = new[] {
            new { Target = (MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Commands.Base.UnitCommand), "CommandTargetUntargetable", new[]{typeof(Kingmaker.EntitySystem.EntityDataBase),typeof(Kingmaker.EntitySystem.Entities.UnitEntityData),typeof(Kingmaker.RuleSystem.RulebookEvent)}), Type = "TimeStopAreaUntargetable01438Patch", Method = "Transpiler", Kind = HarmonyPatchType.Transpiler },
            new { Target = (MethodBase)AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Buffs.Buff), "NextEventTime"), Type = "TimeStopPausedQueue01438Patch", Method = "Prefix", Kind = HarmonyPatchType.Prefix },
            new { Target = (MethodBase)AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Buffs.Buff), "TimeLeft"), Type = "TimeStopPausedTimeLeft01438Patch", Method = "Prefix", Kind = HarmonyPatchType.Prefix },
            new { Target = (MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Buffs.BuffCollection), "TickBuff", new[]{typeof(Kingmaker.UnitLogic.Buffs.Buff),typeof(TimeSpan)}), Type = "TimeStopPausedTick01438Patch", Method = "Prefix", Kind = HarmonyPatchType.Prefix },
            new { Target = (MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Buffs.BuffCollection), "PrepareFactForAttach", new[]{typeof(Kingmaker.UnitLogic.Buffs.Buff)}), Type = "TimeStopRefresh01438Patch", Method = "Prefix", Kind = HarmonyPatchType.Prefix },
            new { Target = (MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff), "CalculateDuration", new[]{typeof(Kingmaker.UnitLogic.Mechanics.MechanicsContext)}), Type = "TimeStopTurnWindow01438Patch", Method = "Postfix", Kind = HarmonyPatchType.Postfix }
        };
        if (timeStopPatches.Any(p => p.Target == null)) throw new Exception("A native Time Stop engine target is missing.");
        targets = targets.Concat(timeStopPatches.Select(p => p.Target)).Distinct().ToArray();
        targets=targets.Concat(new[]{AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityResourceLogic),"IsFreeBecauseOfFact")}).Distinct().ToArray();
        var arcanePatches=new[]{
            new {Target=(MethodBase)AccessTools.Method(typeof(ContextRankConfig),"ApplyMinMax"),Type="BloodIntensityRank01446Patch",Method="Prefix",Kind=HarmonyPatchType.Prefix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.Damage.RulePrepareDamage),"OnTrigger"),Type="ArcanePrepare01446Patch",Method="Prefix",Kind=HarmonyPatchType.Prefix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.Damage.RuleCalculateDamage),"OnTrigger"),Type="ArcaneDamage01446Patch",Method="Prefix",Kind=HarmonyPatchType.Prefix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.RuleSpellResistanceCheck),"OnTrigger"),Type="BloodPiercingResistance01446Patch",Method="Prefix",Kind=HarmonyPatchType.Prefix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage),"GetDamageInfo"),Type="BloodIntensityDamageInfo01446Patch",Method="Postfix",Kind=HarmonyPatchType.Postfix}
        };
        bool retiredMutations=mod.GetType("CallOfTheWild.Wrath.Prototype.TomeClaws01449")!=null&&mod.GetType("CallOfTheWild.Wrath.Prototype.BloodlineExternal01450")==null;
        if(retiredMutations) {
            var removed=arcanePatches.Where(p=>p.Type!="ArcanePrepare01446Patch").ToArray();
            if(removed.Length!=4||removed.Any(p=>mod.GetType("CallOfTheWild.Wrath.Prototype."+p.Type)!=null))throw new Exception("01449 retired mutation patch types must be absent.");
            arcanePatches=arcanePatches.Where(p=>p.Type=="ArcanePrepare01446Patch").ToArray();
            arcanePatches=arcanePatches.Concat(new[]{new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"CanTarget",new[]{typeof(Kingmaker.Utility.TargetWrapper)}),Type="NatureTargetDiagnostic01449Patch",Method="Postfix",Kind=HarmonyPatchType.Postfix}}).ToArray();
        }
        if(mod.GetType("CallOfTheWild.Wrath.Prototype.BloodlineMenu01452")!=null)arcanePatches=arcanePatches.Concat(new[]{new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Class.LevelUp.Actions.LevelUpHelper),"UpdateProgression"),Type="BloodlineMenu01452",Method="Prefix",Kind=HarmonyPatchType.Prefix}}).ToArray();
        if(arcanePatches.Any(p=>p.Target==null))throw new Exception("A native01446 patch target is missing.");
        targets=targets.Concat(arcanePatches.Select(p=>p.Target)).Distinct().ToArray();
        var spellBatchPatches=new[]{
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"CalculateParams"),Type="AdmixtureParams01447Patch",Method="Prefix",Kind=HarmonyPatchType.Prefix},
            new {Target=(MethodBase)AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"SpellDescriptor"),Type="AdmixtureAbilityDescriptor01447Patch",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility),"SpellDescriptor"),Type="AdmixtureBlueprintDescriptor01447Patch",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.FactLogic.AddSpecialSpellList),"OnActivate"),Type="OverseerSpiritActivation01447Patch",Method="Prefix",Kind=HarmonyPatchType.Prefix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.FactLogic.AddSpecialSpellList),"OnDeactivate"),Type="OverseerSpiritDeactivation01447Patch",Method="Prefix",Kind=HarmonyPatchType.Prefix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.Blueprints.Classes.Selection.BlueprintParametrizedFeature),"ExtractSelectionItems"),Type="SignatureSelection01447Patch",Method="Prefix",Kind=HarmonyPatchType.Prefix},
            new {Target=(MethodBase)AccessTools.Method(mod.GetType("CallOfTheWild.Wrath.Prototype.PsychicAmplificationComponent",true),"OnTurnOn"),Type="NatureAmplificationSwitch01447Patch",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Parts.UnitPartSpellResistance),"CanApply",new[]{typeof(Kingmaker.UnitLogic.Parts.UnitPartSpellResistance.SpellImmunity),typeof(Kingmaker.UnitLogic.Mechanics.MechanicsContext)}),Type="NatureSpellImmunity01447Patch",Method="Prefix",Kind=HarmonyPatchType.Prefix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.FactLogic.BuffDescriptorImmunity),"IsImmune"),Type="NatureBuffImmunity01447Patch",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.Combat.UnitCombatState),"IsFlanked"),Type="PhantasmFlanking01447Patch",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Buffs.BuffCollection),"AddBuff",new[]{typeof(Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff),typeof(Kingmaker.UnitLogic.Mechanics.MechanicsContext),typeof(TimeSpan?)}),Type="InfectiousSuccess01447Patch",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionKill),"RunAction"),Type="InfectiousDeathSuccess01447Patch",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"CalculateParams"),Type="InfectiousParameters01447Patch",Method="Prefix",Kind=HarmonyPatchType.Prefix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell),"OnTrigger"),Type="InfectiousConsume01447Patch",Method="Prefix",Kind=HarmonyPatchType.Prefix}
        };
        if(spellBatchPatches.Any(p=>p.Target==null))throw new Exception("A01447 exact patch target is missing.");
        targets=targets.Concat(spellBatchPatches.Select(p=>p.Target)).Distinct().ToArray();
        var magusPatches=spellBatchPatches.Take(0).ToArray();
        if(mod.GetType("CallOfTheWild.Wrath.Prototype.MagusBatch01454")!=null)magusPatches=new[]{
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.Items.ItemEntity),"AddEnchantmentInternal",new[]{typeof(Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment),typeof(Kingmaker.UnitLogic.Mechanics.MechanicsContext),typeof(Kingmaker.Utility.Rounds?)}),Type="MagusEnchantAdded01454",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.Items.ItemEntity),"RemoveEnchantment",new[]{typeof(Kingmaker.Blueprints.Items.Ecnchantments.ItemEnchantment)}),Type="MagusEnchantRemoved01454",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Parts.UnitPartMagus),"HasOneHandedMeleeWeaponAndFreehand"),Type="MagusHands01454",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.ActivatableAbilities.Restrictions.ActivatableAbilityRestrictionByEquipmentSet),"IsAvailable"),Type="MagusCombatGripAvailable01454",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.ActivatableAbilities.DeactivateOnGripChanged),"TryDeactivate"),Type="MagusCombatGripLifetime01454",Method="Prefix",Kind=HarmonyPatchType.Prefix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic),"Spend"),Type="MindbladeNativePayment01454",Method="Prefix",Kind=HarmonyPatchType.Prefix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic),"Spend"),Type="MindbladeNativePayment01454",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"ActionType"),Type="MindbladeManifestAction01454",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"GetConversions"),Type="SkirnirStoreConversions01454",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"Spellbook"),Type="SkirnirStoreOwnBook01454",Method="Prefix",Kind=HarmonyPatchType.Prefix},
            new {Target=(MethodBase)AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Abilities.AbilityData),"Name"),Type="SkirnirStoreCaption01454",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Class.Kineticist.UnitPartKineticist),"RemoveBladeActivatedBuff"),Type="WhipHurricaneBladeLifetime01454",Method="Prefix",Kind=HarmonyPatchType.Prefix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.Controllers.AbilityExecutionProcess),"PrepareCast"),Type="WhipHurricaneNewForm01454",Method="Postfix",Kind=HarmonyPatchType.Postfix},
            new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UnitLogic.Class.Kineticist.AbilityKineticist),"CalculateBurnCost",new[]{typeof(Kingmaker.UnitLogic.UnitDescriptor),typeof(Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility)}),Type="WhipHurricaneBurn01454",Method="Prefix",Kind=HarmonyPatchType.Prefix}
        };
        if(mod.GetType("CallOfTheWild.Wrath.Prototype.MindbladeVariantIcons01455")!=null)magusPatches=magusPatches.Concat(new[]{new {Target=(MethodBase)AccessTools.Method(typeof(Kingmaker.UI.UnitSettings.MechanicActionBarSlotSpontaneusConvertedSpell),"GetIcon"),Type="MindbladeVariantIcons01455",Method="Postfix",Kind=HarmonyPatchType.Postfix}}).ToArray();
        if(magusPatches.Any(p=>p.Target==null))throw new Exception("054/055 exact native patch target missing.");
        targets=targets.Concat(magusPatches.Select(p=>p.Target)).Distinct().ToArray();
        var psychic058Patches=PsychicPatchCoverage01458Tests.Rows(mod).Select(r=>new{Target=r.Target,Type=r.Type,Method=r.Method,Kind=r.Kind}).ToArray();
        targets=targets.Concat(psychic058Patches.Select(p=>p.Target)).Distinct().ToArray();
        var probe = typeof(HarmonyTests).GetMethod(nameof(Original), BindingFlags.NonPublic | BindingFlags.Static);
        try
        {
            harmony.PatchAll(mod);
            foreach (var target in targets)
            {
                var patches = Harmony.GetPatchInfo(target);
                if (patches == null || !patches.Prefixes.Concat(patches.Postfixes).Concat(patches.Transpilers).Any(p => p.owner == owner))
                    throw new Exception("Production patch was not installed: " + target);
            }
            foreach (var expected in timeStopPatches.Concat(arcanePatches).Concat(spellBatchPatches).Concat(magusPatches).Concat(psychic058Patches))
            {
                var method = AccessTools.DeclaredMethod(mod.GetType("CallOfTheWild.Wrath.Prototype." + expected.Type, true), expected.Method);
                if (method == null) throw new Exception("A Time Stop engine patch method is missing: " + expected.Type + "." + expected.Method);
                var installed = Harmony.GetPatchInfo(expected.Target);
                var correctKind = expected.Kind == HarmonyPatchType.Prefix ? installed.Prefixes.ToArray()
                    : expected.Kind == HarmonyPatchType.Postfix ? installed.Postfixes.ToArray() : installed.Transpilers.ToArray();
                var matches = harmony.GetPatchedMethods().SelectMany(target => {
                    var info = Harmony.GetPatchInfo(target);
                    return info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Where(p => p.owner == owner && p.PatchMethod == method);
                }).ToArray();
                if (matches.Length != 1 || correctKind.Count(p => p.owner == owner && p.PatchMethod == method) != 1)
                    throw new Exception("Time Stop engine patch must be installed exactly once, with its exact owner, native target and patch kind: " + method + " -> " + expected.Target + " (" + expected.Kind + ")");
            }
            if (harmony.GetPatchedMethods().Count() != targets.Length)
                throw new Exception("Unexpected production patch target count: actual "+harmony.GetPatchedMethods().Count()+", expected "+targets.Length+"; extra: "+string.Join("; ",harmony.GetPatchedMethods().Except(targets)));
            if (Original(10) != 11) throw new Exception("Unexpected original test result.");
            harmony.Patch(probe, postfix: new HarmonyMethod(typeof(HarmonyTests).GetMethod(nameof(Postfix), BindingFlags.NonPublic | BindingFlags.Static)));
            if (Original(10) != 18 || postfixCalls != 1) throw new Exception("Postfix execution failed.");
        }
        finally { harmony.UnpatchAll(owner); }
        if (harmony.GetPatchedMethods().Any() || Original(10) != 11 || postfixCalls != 1)
            throw new Exception("Owner-scoped unpatch failed.");
        Console.WriteLine("PASS: Harmony " + runtime.GetName().Version +
            "; " + targets.Length + " production patches installed; harmless postfix executed; owner patches removed; identical mod DLL references " + referenced.Version + ".");
        Console.WriteLine("PASS: all six exact Time Stop engine patch methods have one production-owner installation on the correct native target and prefix/postfix/transpiler kind.");
        Console.WriteLine("Game/Unity startup and production target methods were not executed.");
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static int Original(int value) => value + 1;
    private static void Postfix(ref int __result) { postfixCalls++; __result += 7; }
}
