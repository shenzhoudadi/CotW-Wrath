using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MediumClass.Medium;
using MediumClass.Medium.NewActions;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Medium.NewUnitParts;
using MediumClass.NewComponents;
using MediumClass.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Mono.Cecil;

int checks = 0;
var failures = new List<string>();
BlueprintTool.Assets[Guids.RobinHood] = new BlueprintCharacterClass { Name = "RobinHood" };
var all = new[] { Guids.Archmage, Guids.Hierophant, Guids.Champion, Guids.Guardian, Guids.Marshal, Guids.Trickster };
foreach (var id in all) BlueprintTool.Assets[id] = new BlueprintCharacterClass { Name = id };
foreach (var id in new[] { Guids.MediumChannelSpirit, Guids.ProwlerSpirit, Guids.SpiritPower, Guids.MediumSpiritBonus,
    Guids.MediumSpiritMastery, Guids.AstralBeacon, Guids.MediumSpellcasterFeatProhibitArchmage,
    Guids.MediumSpellcasterFeatProhibitHierophant, MythicSpirits.MultipleSpiritsGuid })
    BlueprintTool.Assets[id] = new BlueprintFeature { Name = id, Ranks = 10 };
foreach (var id in new[] { Guids.MediumChannelSpiritPrimarySpiritBuff, Guids.MediumSharedSeanceBuff,
    Guids.MediumSpiritBonusBuff, Guids.MediumInfluenceDebuff, Guids.WeakerSpiritChannelBuff,
    Guids.MediumSpiritInfluenceBuff })
    BlueprintTool.Assets[id] = new BlueprintBuff { Name = id };
foreach (var id in new[] { Guids.WeakerSpiritChannelOneAbility, Guids.WeakerSpiritChannelTwoAbility,
    Guids.WeakerSpiritChannelThreeAbility, Guids.WeakerSpiritChannelFourAbility })
    BlueprintTool.Assets[id] = new BlueprintAbility();
var definitions = all.Select(Definition).ToArray();
BlueprintTool.Get<BlueprintFeature>(Guids.MediumChannelSpirit).Components.AddRange(definitions);
BlueprintTool.Get<BlueprintFeature>(Guids.ProwlerSpirit).Components.AddRange(definitions.Skip(2));
var json = new JsonSerializerSettings { ContractResolver = new GameOptInResolver() };
foreach (var id in MediumDeathCurseMigration.LegacyBuffGuids)
    BlueprintTool.Assets[id] = new BlueprintBuff { Name = id };
var migrationOwner = new UnitEntityData();
foreach (var id in MediumDeathCurseMigration.LegacyBuffGuids)
    migrationOwner.Buffs.Enumerable.Add(new Buff { Blueprint = BlueprintTool.Get<BlueprintBuff>(id) });
var retained = new Buff { Blueprint = BlueprintTool.Get<BlueprintBuff>(Guids.MediumSpiritBonusBuff) };
migrationOwner.Buffs.Enumerable.Add(retained);
MediumChannelRestore.Restore(migrationOwner.Descriptor);
Check(migrationOwner.Buffs.Enumerable.Count == 1 && migrationOwner.Buffs.Enumerable.Contains(retained),
    "restore clears every legacy pair even without a class fact, preserving unrelated buffs");
MediumChannelRestore.Restore(migrationOwner.Descriptor);
Check(migrationOwner.Buffs.Enumerable.Count == 1, "legacy cleanup is idempotent");

// 0.3.4: the restore entry first runs the Medium progression recovery, and it trims 0.3.3's 3-5 Death Curse toggles to two.
MediumProgressionMigration.Calls.Clear();
MediumChannelRestore.Restore(migrationOwner.Descriptor);
Check(MediumProgressionMigration.Calls.Count == 1, "the restore entry runs the Medium progression recovery once per restore");
var cursed = new UnitEntityData();
var toggleBlueprints = new[] { Kingmaker.Enums.Damage.DamageEnergyType.Cold, Kingmaker.Enums.Damage.DamageEnergyType.NegativeEnergy, Kingmaker.Enums.Damage.DamageEnergyType.Acid,
    Kingmaker.Enums.Damage.DamageEnergyType.Fire, Kingmaker.Enums.Damage.DamageEnergyType.Electricity }
    .Select(e => new BlueprintBuff { Name = "Curse" + e, Components = new() { new FakeToggle(e) } }).ToArray();
var bystander = new Buff { Blueprint = BlueprintTool.Get<BlueprintBuff>(MediumDeathCurseMigration.LegacyBuffGuids[0]) };
cursed.Buffs.Enumerable.Add(new Buff { Blueprint = new BlueprintBuff { Name = "Unrelated" } });
foreach (var blueprint in toggleBlueprints) cursed.Buffs.Enumerable.Add(new Buff { Blueprint = blueprint });
MediumChannelRestore.Restore(cursed.Descriptor);
Check(cursed.Buffs.Enumerable.Count(b => MediumDeathCurseRules.IsToggle(b.Blueprint)) == 2
    && cursed.Buffs.Enumerable.Where(b => MediumDeathCurseRules.IsToggle(b.Blueprint)).Select(b => b.Blueprint).SequenceEqual(toggleBlueprints.Take(2)),
    "restore keeps the first two Death Curse toggles (switch-on order) of a five-toggle 0.3.3 save");
Check(cursed.Buffs.Enumerable.Any(b => b.Blueprint.Name == "Unrelated"), "and no unrelated buff");
MediumChannelRestore.Restore(cursed.Descriptor);
Check(cursed.Buffs.Enumerable.Count(b => MediumDeathCurseRules.IsToggle(b.Blueprint)) == 2, "the trimming is idempotent");

// Old saves without a channel marker repair orphaned seance buttons, but a
// still-owned/shared boon remains authoritative even on a non-channeling unit.
foreach(bool retainedBoon in new[]{false,true})
{
    var owner=NewOwner(false);
    var boon=owner.Get<UnitPartMedium>().Spirits[Ref(Guids.Marshal)].SpiritSeanceBoon.Get();
    var ability=new BlueprintAbility {Name=Guids.MarshalSeanceBoonAbility};
    var grant=new Kingmaker.UnitLogic.FactLogic.AddFacts {Facts=new Kingmaker.Blueprints.Facts.BlueprintUnitFact[]{ability}};
    boon.Components.Add(grant);
    if(retainedBoon) owner.AddFact(boon);
    var button=new Kingmaker.EntitySystem.EntityFact {Blueprint=ability,Owner=owner};owner.Facts.Facts.Add(ability,button);
    MediumChannelRestore.Restore(owner.Descriptor);
    Check(button.IsDisposed==!retainedBoon,"no-channel restore removes only orphan seance buttons");
    Check(owner.HasFact(boon)==retainedBoon,"restore never revokes an independently held/shared boon");
    MediumChannelRestore.Restore(owner.Descriptor);
    Check((owner.Facts.Get(ability)!=null)==retainedBoon,"seance orphan restore remains idempotent");
    boon.Components.Remove(grant);
}

// Old saves without a channel marker must lose orphaned power/ability facts.
{
    var owner = NewOwner(false);
    var state = owner.Get<UnitPartMedium>();
    state.PrimarySpirit = Ref(Guids.Champion);
    var power = state.Spirits[state.PrimarySpirit].SpiritGreaterPower.Get();
    var ability = new BlueprintAbility {Name=Guids.ChampionFleetChargeAbility};
    power.Components.Add(new Kingmaker.UnitLogic.FactLogic.AddFacts {Facts=new Kingmaker.Blueprints.Facts.BlueprintUnitFact[]{ability}});
    owner.AddFact(power);
    var orphan = new Kingmaker.EntitySystem.EntityFact {Blueprint=ability,Owner=owner};owner.Facts.Facts.Add(ability,orphan);
    MediumChannelRestore.Restore(owner.Descriptor);
    Check(owner.Progression.Features.GetRank(power)==0,"no-channel load removes old power ranks");
    Check(orphan.IsDisposed && owner.Facts.Get(ability)==null,"no-channel load repairs Fleet Charge orphan");
    Check(!state.ActiveSpiritClasses.Any(),"no-channel load clears old channel choice");
    MediumChannelRestore.Restore(owner.Descriptor);
    Check(owner.Facts.Get(ability)==null,"no-channel repair cannot regrant orphan after second restore");
}
{
    var owner = NewOwner(false);
    var state = owner.Get<UnitPartMedium>();
    state.PrimarySpirit = Ref(Guids.Champion);
    var channel=AddChannel(owner,state.PrimarySpirit);
    channel.IsActive=false;
    var before=Powers(owner);
    MediumChannelRestore.Restore(owner.Descriptor);
    Check(state.PrimarySpirit.Get()==Ref(Guids.Champion).Get(),"present inactive marker retains saved primary during restoration");
    Check(before.SequenceEqual(Powers(owner)),"present inactive marker retains powers pending native activation");
}
foreach (bool prowler in new[] { false, true })
foreach (int count in new[] { 1, 2, 4 })
foreach (int forgone in new[] { 0, 1 })
foreach (int remaining in new[] { 0, 1 })
{
    string label = $"prowler={prowler}, spirits={count}, weaker={forgone}, freeRemaining={remaining}";
    var owner = NewOwner(prowler);
    var state = owner.Get<UnitPartMedium>();
    var selected = (prowler ? all.Skip(2) : all).Take(count).Select(Ref).ToArray();
    state.PrimarySpirit = selected[0];
    state.AdditionalSpirits.AddRange(selected.Skip(1));
    state.SecondarySpirit = Ref(Guids.Trickster);
    state.ForgonePowers = forgone;
    state.FreeSurgeAmount = remaining;
    var channel = AddChannel(owner, selected[0]);
    MediumChannelRestore.Restore(owner.Descriptor);
    var expectedPowers = Powers(owner);
    Check(expectedPowers.Length > 0, label + ": initial powers granted");

    // Saving and zone unloading turn effects off; neither operation ends a channel.
    var catalogue = owner.Facts.Get(BlueprintTool.Get<BlueprintFeature>(prowler ? Guids.ProwlerSpirit : Guids.MediumChannelSpirit));
    catalogue.CallComponents<MediumSpiritComponent>(c => c.OnTurnOff());
    new MediumSpiritMasteryComponent { Owner = owner }.OnTurnOff();
    new MediumWeakerSpiritComponent { Owner = owner }.OnTurnOff();
    Check(ReferenceEquals(state, owner.Get<UnitPartMedium>()), label + ": temporary turn-off retains part");
    Check(state.ActiveSpiritClasses.Count() == count, label + ": temporary turn-off retains catalogue and selection");
    Check(state.FreeSurgeAmount == remaining && state.ForgonePowers == forgone, label + ": turn-off retains daily counters");
    state.OnPreSave();
    string payload = JsonConvert.SerializeObject(state, json);
    var saved = JObject.Parse(payload);
    foreach (var field in new[] { "ChannelSaveVersion", "PrimarySpirit", "SecondarySpirit", "AdditionalSpirits", "ForgonePowers", "FreeSurgeAmount" })
        Check(saved.ContainsKey(field), label + ": real opt-in JSON includes " + field);
    Check(!saved.ContainsKey("Spirits") && !saved.ContainsKey("Owner"), label + ": derived catalogue and owner excluded");

    // Deserialize a fresh object, with no runtime catalogue and no legacy ability
    // context. New saves must succeed solely from their explicit saved choice.
    var loaded = NewOwner(prowler);
    var restored = JsonConvert.DeserializeObject<UnitPartMedium>(payload, json);
    loaded.LoadPart(restored);
    Check(restored.Spirits.Count == 0, label + ": fresh JSON object starts with empty derived catalogue");
    var savedChannel = AddChannel(loaded, null);
    loaded.Influence = 4;
    restored.OnPostLoad();
    InvokeRestorePrefix(loaded.Descriptor);
    Check(restored.ActiveSpiritClasses.Count() == count, label + ": restored formal spirits");
    Check(restored.PrimarySpirit.Equals(selected[0]), label + ": restored primary");
    Check(restored.SecondarySpirit.Equals(Ref(Guids.Trickster)), label + ": secondary persisted");
    Check(restored.ForgonePowers == forgone && restored.FreeSurgeAmount == remaining, label + ": spent free surges and forgone powers persisted");
    Check(Powers(loaded) == expectedPowers, label + ": powers reconstructed before native spellbook fixes");
    Check(loaded.Influence == 4, label + ": load never resets accumulated influence");
    Check((loaded.Get<UnitPartMediumPreparedSpells>()?.Clears ?? 0) == 0, label + ": no destructive channel reapply");
    restored.OnTurnOn();
    InvokeRestorePrefix(loaded.Descriptor);
    Check(Powers(loaded) == expectedPowers, label + ": repeat restoration never stacks feature ranks");
    Check(restored.FreeSurgeAmount == remaining && loaded.Influence == 4, label + ": repeat/zone turn-on preserves counters");
    catalogue.CallComponents<MediumSpiritComponent>(c => c.OnTurnOn());
    new MediumSpiritMasteryComponent { Owner = owner }.OnTurnOn();
    new MediumWeakerSpiritComponent { Owner = owner }.OnTurnOn();
    Check(state.FreeSurgeAmount == remaining, label + ": re-enable does not replenish free surges");
}

// 0.2.2 saved only AdditionalSpirits. Recover a legacy primary from the buff's
// real source action, even when the old part vanished completely before saving.
foreach (bool prowler in new[] { false, true })
foreach (bool missingPart in new[] { false, true })
{
    var owner = NewOwner(prowler);
    var primary = Ref(prowler ? Guids.Champion : Guids.Archmage);
    var legacy = JsonConvert.DeserializeObject<UnitPartMedium>("{\"AdditionalSpirits\":[]}", json);
    owner.LoadPart(legacy);
    if (missingPart) owner.RemovePart(typeof(UnitPartMedium));
    AddChannel(owner, primary);
    owner.Influence = 3;
    InvokeRestorePrefix(owner.Descriptor);
    Check(owner.Get<UnitPartMedium>().IsActiveSpirit(primary), "legacy primary repaired for both classes, missingPart=" + missingPart);
    Check(owner.Influence == 3, "legacy repair retains influence");
    Check(owner.Get<UnitPartMedium>().FreeSurgeAmount == 0, "legacy cannot invent unspent free surges");
}

// An early focus callback is safe; the restoration pass reconciles it after
// rebuilding the catalogue. Recovering an absent channel must not resurrect it.
{
    var owner = NewOwner(false);
    var state = owner.Get<UnitPartMedium>();
    state.Spirits.Clear();
    var focusFeature = new BlueprintFeature { Name = "SaveRestoreFocus" };
    var focusFact = owner.AddFact(focusFeature);
    focusFact.Components.Add(new MediumSpiritFocusComponent { Spirit = Ref(Guids.Archmage) });
    focusFact.CallComponents<MediumSpiritFocusComponent>(c => c.OnTurnOn());
    state.PrimarySpirit = Ref(Guids.Archmage);
    AddChannel(owner, state.PrimarySpirit);
    InvokeRestorePrefix(owner.Descriptor);
    Check(state.Spirits[Ref(Guids.Archmage)].SpiritFocus == 1, "focus rebuilt after catalogue");
    owner.Buffs.Enumerable.Clear();
    InvokeRestorePrefix(owner.Descriptor);
    Check(!state.ActiveSpiritClasses.Any(), "no channel buff cannot resurrect saved choices");
    var fact = owner.Facts.Get(BlueprintTool.Get<BlueprintFeature>(Guids.MediumChannelSpirit));
    fact.CallComponents<MediumSpiritComponent>(c => c.OnDeactivate());
    Check(state.Spirits.Count == 0 && owner.Get<UnitPartMedium>() == state, "actual feature removal detaches catalogue without deleting saved part");
    fact.IsActive = false;
    fact.CallComponents<MediumSpiritComponent>(c => c.OnPostLoad());
    Check(state.Spirits.Count == 0, "inactive class fact cannot rebuild a catalogue during loading");
}

// A seventh channel ability (the legendary spirit) and the mythic influence
// marker buff must ride the same restoration path as the six base spirits.
{
    BlueprintTool.Assets[Guids.Adriel] = new BlueprintCharacterClass { Name = Guids.Adriel };
    BlueprintTool.Get<BlueprintFeature>(Guids.MediumChannelSpirit).Components.Add(Definition(Guids.Adriel));
    var owner = NewOwner(false);
    var state = owner.Get<UnitPartMedium>();
    state.PrimarySpirit = Ref(Guids.Adriel);
    AddChannel(owner, state.PrimarySpirit);
    MediumChannelRestore.Restore(owner.Descriptor);
    var expectedPowers = Powers(owner);
    Check(expectedPowers.Contains("power-" + Guids.Adriel), "legendary spirit powers granted before saving");
    state.OnPreSave();
    string payload = JsonConvert.SerializeObject(state, json);
    var loaded = NewOwner(false);
    var restored = JsonConvert.DeserializeObject<UnitPartMedium>(payload, json);
    loaded.LoadPart(restored);
    AddChannel(loaded, null);
    restored.OnPostLoad();
    var influence = new Buff { Owner = loaded, Blueprint = BlueprintTool.Get<BlueprintBuff>(Guids.MediumSpiritInfluenceBuff) };
    var allies = new MediumContextSpiritInfluenceComponent();
    influence.Components.Add(allies);
    loaded.Buffs.Enumerable.Add(influence);
    InvokeRestorePrefix(loaded.Descriptor);
    Check(restored.IsActiveSpirit(Ref(Guids.Adriel)), "legendary spirit restored from its saved channel ability");
    Check(Powers(loaded) == expectedPowers, "legendary spirit powers reconstructed after load");
    // One pass comes from the spirit modifier refresh, one from the explicit
    // influence-buff pass; RefreshAllies is idempotent, so both are expected.
    Check(allies.AllyRefreshes == 2, "mythic influence buff re-grants ally bonuses during load");
    InvokeRestorePrefix(loaded.Descriptor);
    Check(allies.AllyRefreshes == 4 && Powers(loaded) == expectedPowers, "repeat restoration refreshes allies without stacking powers");
}

if (args.Length > 0)
{
    using var game = AssemblyDefinition.ReadAssembly(args[0]);
    var blueprintReader = game.MainModule.GetType("Kingmaker.EntitySystem.Persistence.JsonUtility.BlueprintConverter")
        .Methods.Single(m => m.Name == "ReadJson");
    Check(Calls(blueprintReader).Any(m => m.Name == "TryGetBlueprint")
        && blueprintReader.Body.Instructions.Any(i => i.OpCode.Code == Mono.Cecil.Cil.Code.Throw),
        "native blueprint reader rejects a missing saved GUID; legacy registrations are required");
    var resolver = game.MainModule.GetType("Kingmaker.EntitySystem.Persistence.JsonUtility.OptInContractResolver")
        .Methods.Single(m => m.Name == "CreateProperties");
    Check(resolver.Body.Instructions.Any(i => i.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_I4_1), "native contract resolver explicitly selects MemberSerialization.OptIn");
    var preSave = game.MainModule.GetType("Kingmaker.EntitySystem.EntityDataBase").Methods.Single(m => m.Name == "PreSave");
    Check(Calls(preSave).Any(m => m.Name == "TurnOff"), "native pre-save may temporarily turn effects off");
    var apply = game.MainModule.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData").Methods.Single(m => m.Name == "OnApplyPostLoadFixes");
    Check(Calls(apply).Any(m => m.DeclaringType.FullName == "Kingmaker.UnitLogic.UnitDescriptor" && m.Name == "ApplyPostLoadFixes"), "native load reaches our restoration boundary");
    var remove=game.MainModule.GetType("Kingmaker.EntitySystem.EntityFactsManager").Methods.Single(m=>m.Name=="Remove");
    var instructions=remove.Body.Instructions;
    int cacheRemoval=instructions.Select((i,n)=>(i,n)).First(t=>t.i.Operand is MethodReference call
        && call.DeclaringType.Name=="EntityFactsCache" && call.Name=="Remove").n;
    int detach=instructions.Select((i,n)=>(i,n)).First(t=>t.i.Operand is MethodReference call && call.Name=="Detach").n;
    Check(cacheRemoval<detach,"native marker leaves fact cache before its component deactivation callbacks");
}
if (args.Length > 1)
{
    using var mod = AssemblyDefinition.ReadAssembly(args[1]);
    var part = mod.MainModule.GetType("MediumClass.Medium.NewUnitParts.UnitPartMedium");
    foreach (var name in new[] { "ChannelSaveVersion", "PrimarySpirit", "SecondarySpirit", "AdditionalSpirits", "ForgonePowers", "FreeSurgeAmount" })
        Check(part.Fields.Single(f => f.Name == name).CustomAttributes.Any(a => a.AttributeType.FullName == "Newtonsoft.Json.JsonPropertyAttribute"), "installer DLL persists " + name);
    Check(part.Fields.Single(f => f.Name == "Spirits").CustomAttributes.Any(a => a.AttributeType.Name == "JsonIgnoreAttribute"), "installer excludes runtime catalogue");
    Check(!Calls(part.Methods.Single(m => m.Name == "RemoveSpiritEntry")).Any(m => m.Name == "RemoveSelf"), "installer cannot delete saved state when catalogue empties");
    var prefix = mod.MainModule.GetType("MediumClass.Medium.MediumChannelRestorePatch");
    var patchAttribute = prefix.CustomAttributes.Single(a => a.AttributeType.Name == "HarmonyPatch");
    Check(((TypeReference)patchAttribute.ConstructorArguments[0].Value).FullName == "Kingmaker.UnitLogic.UnitDescriptor"
        && (string)patchAttribute.ConstructorArguments[1].Value == "ApplyPostLoadFixes", "installer patch targets the native load boundary");
    Check(Calls(prefix.Methods.Single(m => m.Name == "Prefix")).Any(m => m.Name == "Restore"), "installer repairs before original load fixes");
    foreach (var component in new[] { "MediumSpiritComponent", "MediumSpiritMasteryComponent", "MediumWeakerSpiritComponent" })
    {
        var type = mod.MainModule.GetType("MediumClass.Medium.NewComponents.AbilitySpecific." + component);
        Check(type.Methods.All(m => m.Name != "OnTurnOff"), "installer temporary disable preserves " + component);
        Check(type.Methods.Any(m => m.Name == "OnDeactivate"), "installer actual removal still cleans " + component);
    }
    var repair = mod.MainModule.GetType("MediumClass.Medium.MediumChannelRestore");
    Check(Calls(repair.Methods.Single(m=>m.Name=="Restore")).Any(m=>m.Name=="RevokeAll"),"installer no-channel load repairs orphan powers");
    Check(Calls(repair.Methods.Single(m=>m.Name=="Restore")).Any(m=>m.Name=="RevokeSeanceOrphans"),"installer no-channel load repairs seance orphans");
    var teardown=mod.MainModule.GetType("MediumClass.NewComponents.ApplySpirits").Methods.Single(m=>m.Name=="OnDeactivate");
    var teardownCalls=Calls(teardown).Select(m=>m.Name).ToList();
    Check(teardownCalls.IndexOf("ClearChannelSelection")>=0 && teardownCalls.IndexOf("ClearChannelSelection")<teardownCalls.IndexOf("Clear"),"installer retires saved choice before clear callback");
    Check(!teardownCalls.Contains("HasPrimarySpirit"),"installer teardown no longer abandons missing-primary cleanup");
    var repairMethods = repair.Methods.Concat(repair.NestedTypes.SelectMany(t => t.Methods)).Where(m => m.HasBody);
    Check(!repairMethods.SelectMany(Calls).Any(m => m.Name is "Rest" or "Reapply" or "Reset" or "OnDeactivate"), "installer restoration never rests, reapplies channel, or resets influence");
}
Console.WriteLine($"Save/restore: {checks - failures.Count}/{checks} checks passed. Real Newtonsoft opt-in JSON roundtrip and linked production lifecycle on explicit game API doubles; no Unity gameplay.");
foreach (var failure in failures) Console.Error.WriteLine(failure);
return failures.Count == 0 ? 0 : 1;

void Check(bool condition, string name) { checks++; if (!condition) failures.Add(name); }
BlueprintCharacterClassReference Ref(string id) => BlueprintTool.GetRef<BlueprintCharacterClassReference>(id);
IEnumerable<MethodReference> Calls(MethodDefinition method) => method.Body.Instructions.Select(i => i.Operand).OfType<MethodReference>();
void InvokeRestorePrefix(UnitDescriptor owner) => typeof(MediumChannelRestorePatch).GetMethod("Prefix", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.Invoke(null, new object[] { owner });
string Powers(UnitEntityData owner) => string.Join(";", owner.Progression.Features.Counts.Where(p => p.Key.Name.StartsWith("power-"))
    .OrderBy(p => p.Key.Name).Select(p => p.Key.Name + "=" + p.Value));
MediumSpiritComponent Definition(string id)
{
    BlueprintFeatureReference Power(string suffix) => new() { Blueprint = new BlueprintFeature { Name = "power-" + id + suffix, Ranks = 10 } };
    return new MediumSpiritComponent {
        SpiritClass = Ref(id), SpiritBonusFeature = BlueprintTool.GetRef<BlueprintFeatureReference>(Guids.MediumSpiritBonus),
        SpiritSeanceBoon = Power("boon"), SpiritLesserPower = Power("lesser"), SpiritIntermediatePower = Power("intermediate"),
        SpiritGreaterPower = Power("greater"), SpiritSupremePower = Power("supreme"),
        Stats = new[] { StatType.AdditionalAttackBonus }, Penalties = new[] { StatType.SaveFortitude }
    };
}
UnitEntityData NewOwner(bool prowler)
{
    var owner = new UnitEntityData();
    owner.Ensure<UnitPartMedium>();
    var blueprint = BlueprintTool.Get<BlueprintFeature>(prowler ? Guids.ProwlerSpirit : Guids.MediumChannelSpirit);
    var fact = owner.AddFact(blueprint);
    fact.Components.AddRange(blueprint.Components);
    fact.CallComponents<MediumSpiritComponent>(c => c.OnActivate());
    owner.Progression.Features.Counts[BlueprintTool.Get<BlueprintFeature>(Guids.SpiritPower)] = prowler ? 3 : 4;
    return owner;
}
Buff AddChannel(UnitEntityData owner, BlueprintCharacterClassReference primary)
{
    var ability = new BlueprintAbility();
    ability.Components.Add(new AbilityEffectRunAction { Actions = new() { Actions = new object[] { new ContextActionApplySpirit { Spirit = primary } } } });
    var buff = new Buff { Owner = owner, Blueprint = BlueprintTool.Get<BlueprintBuff>(Guids.MediumChannelSpiritPrimarySpiritBuff), Context = new() { SourceAbility = primary == null ? null : ability } };
    buff.Components.Add(new ApplySpirits());
    owner.Buffs.Enumerable.Add(buff);
    return buff;
}

class GameOptInResolver : DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        => base.CreateProperties(type, MemberSerialization.OptIn);
}

sealed class FakeToggle : MediumClass.Medium.IDeathCurseToggle
{
    private readonly Kingmaker.Enums.Damage.DamageEnergyType energy;
    public FakeToggle(Kingmaker.Enums.Damage.DamageEnergyType energy) { this.energy = energy; }
    public Kingmaker.Enums.Damage.DamageEnergyType[] ToggleEnergies => new[] { energy };
}
