using System.Text.Json;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Mechanics;
using MediumClass.Medium.NewComponents;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;

int checks = 0;
var failures = new List<string>();
void Check(bool condition, string label) { checks++; if (!condition) failures.Add(label); }
void Equal<T>(T expected, T actual, string label) =>
    Check(EqualityComparer<T>.Default.Equals(expected, actual), $"{label}: expected {expected}, actual {actual}");
(int Amount, object Source, Kingmaker.Enums.ModifierDescriptor Descriptor) SingleModifier(UnitEntityData unit, StatType stat, string label)
{
    var modifiers = unit.Stats.GetStat(stat).Modifiers;
    Equal(1, modifiers.Count, label + " applies exactly one modifier");
    return modifiers.Count == 1 ? modifiers[0] : default;
}
void Register(string id, object blueprint) => BlueprintTool.Assets[id] = blueprint;

// ---------------------------------------------------------------------------
// Blueprints the linked production code resolves through BlueprintTool.
// ---------------------------------------------------------------------------
var channelBlueprint = new BlueprintFeature { Name = "ChannelSpirit" };
Register(Guids.MediumChannelSpirit, channelBlueprint);
channelBlueprint.Components.Add(new MediumSpiritComponent
{
    // RemoveOwnModifiers clears by the channel blueprint's stat list, which
    // survives even after the provider's runtime spirit table is rebuilt.
    Stats = new[] { StatType.AdditionalAttackBonus, StatType.SaveFortitude, StatType.SaveWill }
});
var allyBlueprint = new BlueprintFeature { Name = "SpiritInfluenceAlly" };
Register(Guids.MediumSpiritInfluenceAlly, allyBlueprint);
allyBlueprint.RuntimeFactories.Add((unit, fact) => new MediumSpiritInfluenceAllyComponent { Owner = unit, Fact = (UnitFact)fact });
var markerBlueprint = new Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff { Name = "SpiritInfluenceMarker" };
Register(Guids.MediumSpiritInfluenceBuff, markerBlueprint);
var channelBuffBlueprint = new Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff { Name = "ChannelBuff" };
Register(Guids.MediumChannelSpiritPrimarySpiritBuff, channelBuffBlueprint);
var ownBonusBlueprint = new Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff { Name = "OwnBonus" };
Register(Guids.MediumSpiritBonusBuff, ownBonusBlueprint);
var challengeTargetBlueprint = new Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff { Name = "Challenged" };
Register(Guids.MediumAdrielChallengeBuffTarget, challengeTargetBlueprint);

var bonusFeature = new BlueprintFeature { Name = "SpiritBonus" };
Register(Guids.MediumSpiritBonus, bonusFeature);
var bonusRef = BlueprintTool.GetRef<BlueprintFeatureReference>(Guids.MediumSpiritBonus);
var championClass = new BlueprintCharacterClass { Name = "Champion" };
Register(Guids.Champion, championClass);
var guardianClass = new BlueprintCharacterClass { Name = "Guardian" };
Register(Guids.Guardian, guardianClass);
var adrielClass = new BlueprintCharacterClass { Name = "Adriel" };
Register(Guids.Adriel, adrielClass);
var championRef = BlueprintTool.GetRef<BlueprintCharacterClassReference>(Guids.Champion);
var guardianRef = BlueprintTool.GetRef<BlueprintCharacterClassReference>(Guids.Guardian);
var adrielRef = BlueprintTool.GetRef<BlueprintCharacterClassReference>(Guids.Adriel);

UnitEntityData Medium(out UnitPartMedium state, BlueprintCharacterClassReference primary, int focus, int rank,
    params StatType[] stats)
{
    var unit = new UnitEntityData();
    state = unit.Ensure<UnitPartMedium>();
    state.PrimarySpirit = primary;
    state.Spirits[primary] = new SpiritEntry
    {
        SpiritFocus = focus,
        SpiritBonus = new SpiritStatEntry { Stats = stats, SpiritBonusFeature = bonusRef }
    };
    unit.Progression.Features.Counts[bonusFeature] = rank;
    Game.Instance.Player.ActiveCompanions.Add(unit);
    return unit;
}

// ---------------------------------------------------------------------------
// A. Mythic Spirit Influence: the marker buff mirrors the channel onto allies.
// ---------------------------------------------------------------------------
var provider = Medium(out var providerState, championRef, 1, 2,
    StatType.AdditionalAttackBonus, StatType.SaveFortitude);
var ally = new UnitEntityData();
Game.Instance.Player.ActiveCompanions.Add(ally);
var providerMarker = new Buff { Blueprint = markerBlueprint };
provider.Buffs.Enumerable.Add(providerMarker);
var influence = new MediumContextSpiritInfluenceComponent { Owner = provider, Fact = providerMarker };
providerMarker.Components.Add(influence);

influence.OnTurnOn();
var allyFact = ally.Facts.Get(allyBlueprint);
Check(allyFact != null && allyFact.IsActive, "seance start grants the mirrored bonus to an ally");
Check(provider.Facts.Get(allyBlueprint) == null, "the medium itself never receives the mirrored fact");
var allyComponent = allyFact?.Components.OfType<MediumSpiritInfluenceAllyComponent>().SingleOrDefault();
Check(allyComponent != null, "the granted fact carries the ally component");
Equal(provider, allyComponent?.Data.Provider, "the grant records its provider");
Equal(1, influence.Data.Grants.Count, "one grant is tracked");
Equal(ally, influence.Data.Grants[0].Target, "the tracked target is the ally");
Equal(allyFact, influence.Data.Grants[0].Fact, "the tracked fact is the granted one");
Equal(3, SingleModifier(ally, StatType.AdditionalAttackBonus, "rank 2 + focus 1 mirrors as +3").Amount,
    "mirror includes rank and spirit focus");
Equal(3, SingleModifier(ally, StatType.SaveFortitude, "second stat mirrors too").Amount, "every spirit stat is mirrored");
Equal(Kingmaker.Enums.ModifierDescriptor.UntypedStackable, ally.Stats.GetStat(StatType.AdditionalAttackBonus)
    .Modifiers.Single().Descriptor, "mirror uses the same descriptor as the medium's own bonus");
Equal(allyComponent!.Runtime, ally.Stats.GetStat(StatType.AdditionalAttackBonus).Modifiers.Single().Source,
    "mirror modifiers are owned by the ally's own runtime");

provider.Progression.Features.Counts[bonusFeature] = 3;
influence.RefreshAllies();
Equal(4, SingleModifier(ally, StatType.AdditionalAttackBonus, "level-up refresh").Amount,
    "level-up raises the mirrored value");
influence.RefreshAllies();
Equal(1, ally.Stats.GetStat(StatType.AdditionalAttackBonus).Modifiers.Count, "repeated refresh does not stack");

providerState.AdditionalSpirits.Add(guardianRef);
providerState.Spirits[guardianRef] = new SpiritEntry
{
    SpiritFocus = 0,
    SpiritBonus = new SpiritStatEntry { Stats = new[] { StatType.AdditionalAttackBonus, StatType.SaveWill },
        SpiritBonusFeature = bonusRef }
};
influence.RefreshAllies();
Equal(4, SingleModifier(ally, StatType.AdditionalAttackBonus, "overlapping spirits use the highest value").Amount,
    "a weaker second spirit does not add to the mirror");
Equal(3, SingleModifier(ally, StatType.SaveWill, "second spirit's own stat").Amount, "stats only the second spirit has still mirror");
Equal(4, SingleModifier(ally, StatType.SaveFortitude, "primary-only stat after overlap").Amount, "the primary spirit keeps its own stat");

// Two mediums share one ally: ending the first seance hands the grant over
// instead of erasing the ally's mirrored bonus.
var provider2 = Medium(out var provider2State, guardianRef, 2, 2, StatType.AdditionalAttackBonus);
var provider2Marker = new Buff { Blueprint = markerBlueprint };
provider2.Buffs.Enumerable.Add(provider2Marker);
var influence2 = new MediumContextSpiritInfluenceComponent { Owner = provider2, Fact = provider2Marker };
provider2Marker.Components.Add(influence2);

// Turning the marker buff off is what invokes OnTurnOff, so the retiring
// medium's own buff is already gone when the grants are handed over.
provider.Buffs.Enumerable.Remove(providerMarker);
influence.OnTurnOff();
Equal(0, influence.Data.Grants.Count, "the retiring medium forgets the transferred grant");
Equal(1, influence2.Data.Grants.Count, "the remaining medium adopts the grant");
Check(ally.Facts.Get(allyBlueprint)?.IsActive == true, "the ally fact survives the provider change");
Equal(provider2, allyComponent.Data.Provider, "the grant follows the remaining provider");
Equal(4, SingleModifier(ally, StatType.AdditionalAttackBonus, "transfer re-mirrors the new provider").Amount,
    "after the handover the ally mirrors provider 2 (rank 2 + focus 2)");
Equal(0, ally.Stats.GetStat(StatType.SaveFortitude).Modifiers.Count, "stats the new provider lacks are cleared");
provider2.Buffs.Enumerable.Remove(provider2Marker);
influence2.OnTurnOff();
Check(ally.Facts.Get(allyBlueprint) == null, "the last provider's exit removes the ally fact");
Equal(0, ally.Stats.GetStat(StatType.AdditionalAttackBonus).Modifiers.Count, "the mirrored modifier ends with the seance");

// A medium without a current seance grants nothing.
var idle = new UnitEntityData();
var idleMarker = new Buff { Blueprint = markerBlueprint };
idle.Buffs.Enumerable.Add(idleMarker);
var idleInfluence = new MediumContextSpiritInfluenceComponent { Owner = idle, Fact = idleMarker };
idleMarker.Components.Add(idleInfluence);
idleInfluence.OnTurnOn();
Equal(0, idleInfluence.Data.Grants.Count, "a medium without spirit state grants nothing");
idle.Ensure<UnitPartMedium>();
idleInfluence.OnTurnOn();
Equal(0, idleInfluence.Data.Grants.Count, "a medium without a primary spirit grants nothing");

// Stale or disposed grants are skipped during teardown instead of throwing.
var orphan = new MediumContextSpiritInfluenceComponent { Owner = provider, Fact = providerMarker };
orphan.Data.Grants.Add(new MediumContextSpiritInfluenceComponent.Grant { Target = null, Fact = null });
orphan.Data.Grants.Add(new MediumContextSpiritInfluenceComponent.Grant { Target = ally, Fact = new UnitFact { IsDisposed = true } });
orphan.OnTurnOff();
Check(true, "teardown tolerates null and disposed grants");

// The removal path uses the channel blueprint's stat list, so it still clears
// modifiers after the provider's runtime spirit table has been lost.
providerState.PrimarySpirit = championRef;
influence.OnTurnOn();
Check(ally.Facts.Get(allyBlueprint)?.IsActive == true, "the same medium can grant again after a rechannel");
// A fresh grant rebuilds the runtime delegate, so read the new instance.
allyComponent = ally.Facts.Get(allyBlueprint).Components.OfType<MediumSpiritInfluenceAllyComponent>().Single();
providerState.Spirits.Clear();
providerState.PrimarySpirit = null;
providerState.AdditionalSpirits.Clear();
allyComponent.RefreshModifiers();
Equal(0, ally.Stats.GetStat(StatType.AdditionalAttackBonus).Modifiers.Count,
    "modifiers clear even when the provider's spirit table is gone");
Check(ally.Facts.Get(allyBlueprint)?.IsActive == true, "the granted fact itself waits for the seance to end");

// ---------------------------------------------------------------------------
// B. The feature component converges both gain orders onto one marker buff.
// ---------------------------------------------------------------------------
var featureComponent = new MediumSpiritInfluenceFeatureComponent { Owner = provider2 };
featureComponent.OnTurnOn();
Check(provider2.Buffs.GetBuff(markerBlueprint) == null, "no channel, no marker");
provider2.Buffs.Enumerable.Add(new Buff { Blueprint = channelBuffBlueprint });
featureComponent.OnTurnOn();
Check(provider2.Buffs.GetBuff(markerBlueprint) != null, "gaining the feature after the channel applies the marker");
featureComponent.OnTurnOn();
Equal(1, provider2.Buffs.Enumerable.Count(b => b.Blueprint == markerBlueprint), "the marker is never duplicated");
featureComponent.OnTurnOff();
Check(provider2.Buffs.GetBuff(markerBlueprint) == null, "losing the feature removes the marker");

// ---------------------------------------------------------------------------
// C. Adriel's Death Curse: 1d6 of each chosen energy against the own target.
// ---------------------------------------------------------------------------
var attacker = new UnitEntityData();
var attackerState = attacker.Ensure<UnitPartMedium>();
attackerState.PrimarySpirit = adrielRef;
attackerState.Spirits[adrielRef] = new SpiritEntry();
var target = new UnitEntityData();
var challengeBuff = new Buff { Blueprint = challengeTargetBlueprint, Context = new MechanicsContext { MaybeCaster = attacker } };
target.Buffs.Enumerable.Add(challengeBuff);
var curse = new MediumDeathCurseComponent { Owner = attacker, Energies = new[] { DamageEnergyType.Fire, DamageEnergyType.Cold } };
// The damage path only honours toggles that are active buffs of the attacker (limit of two, see MediumDeathCurseRules).
Kingmaker.UnitLogic.Buffs.Buff ToggleBuff(MediumDeathCurseComponent component) => new()
{
    Blueprint = new Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff { Components = new() { component } }
};
attacker.Buffs.Enumerable.Add(ToggleBuff(curse));

var hit = new RulePrepareDamage { DamageBundle = new DamageBundle { Weapon = new object() }, Target = target };
curse.OnEventAboutToTrigger(hit);
Equal(2, hit.Added.Count, "a weapon hit on the challenged target adds both energies");
Equal(DamageEnergyType.Fire, hit.Added[0].Energy, "first energy follows the enabled set");
Equal(DamageEnergyType.Cold, hit.Added[1].Energy, "second energy follows the enabled set");
Equal(1, hit.Added[0].Formula.RollsCount, "each energy rolls one die");
Equal(DiceType.D6, hit.Added[0].Formula.Dice, "the curse die is a d6");
Equal(DiceType.D6, hit.Added[1].Formula.Dice, "the second die is a d6 too");

// In game each energy toggle buff carries its own single-energy component. Since 0.3.4 at most two
// toggles are effective (the first two in buff order); a third one, which only old saves or foreign
// code can still create, adds nothing. The full matrix is in tests/DeathCurseLimit.
attacker.Buffs.Enumerable.Clear();
var toggles = new[] { DamageEnergyType.NegativeEnergy, DamageEnergyType.Electricity, DamageEnergyType.Acid }
    .Select(e => new MediumDeathCurseComponent { Owner = attacker, Energies = new[] { e } }).ToArray();
foreach (var toggle in toggles) attacker.Buffs.Enumerable.Add(ToggleBuff(toggle));
var toggledHit = new RulePrepareDamage { DamageBundle = new DamageBundle { Weapon = new object() }, Target = target };
foreach (var toggle in toggles) toggle.OnEventAboutToTrigger(toggledHit);
Equal(2, toggledHit.Added.Count, "three toggles still add only two dice");
Equal(DamageEnergyType.NegativeEnergy, toggledHit.Added[0].Energy, "the first switched-on toggle stays effective");
Equal(DamageEnergyType.Electricity, toggledHit.Added[1].Energy, "the second switched-on toggle stays effective");
attacker.Buffs.Enumerable.Clear();
attacker.Buffs.Enumerable.Add(ToggleBuff(curse));
var doubled = new RulePrepareDamage { DamageBundle = new DamageBundle { Weapon = new object() }, Target = target };
new MediumDeathCurseComponent { Owner = attacker, Energies = new[] { DamageEnergyType.Fire, DamageEnergyType.Fire } }.OnEventAboutToTrigger(doubled);
Equal(1, doubled.Added.Count, "a repeated energy on one component adds one die");
var none = new RulePrepareDamage { DamageBundle = new DamageBundle { Weapon = new object() }, Target = target };
new MediumDeathCurseComponent { Owner = attacker }.OnEventAboutToTrigger(none);
Equal(0, none.Added.Count, "no energy toggled leaves the curse inert");

checks += DeathCurseLimitChecks.Run((ok, label) => Check(ok, label), adrielRef, challengeTargetBlueprint);
var bare = new RulePrepareDamage { DamageBundle = new DamageBundle(), Target = target };
curse.OnEventAboutToTrigger(bare);
Equal(0, bare.Added.Count, "attacks without a weapon leave the curse inert");

var stranger = new RulePrepareDamage { DamageBundle = new DamageBundle { Weapon = new object() }, Target = new UnitEntityData() };
curse.OnEventAboutToTrigger(stranger);
Equal(0, stranger.Added.Count, "a target without the challenge buff is unaffected");

challengeBuff.Context = new MechanicsContext { MaybeCaster = new UnitEntityData() };
var foreign = new RulePrepareDamage { DamageBundle = new DamageBundle { Weapon = new object() }, Target = target };
curse.OnEventAboutToTrigger(foreign);
Equal(0, foreign.Added.Count, "another medium's challenge target is unaffected");

challengeBuff.Context = null;
var orphanTarget = new RulePrepareDamage { DamageBundle = new DamageBundle { Weapon = new object() }, Target = target };
curse.OnEventAboutToTrigger(orphanTarget);
Equal(2, orphanTarget.Added.Count, "a restored save without a caster link still counts as the own target");

attackerState.PrimarySpirit = championRef;
attackerState.Spirits[championRef] = new SpiritEntry();
var stale = new RulePrepareDamage { DamageBundle = new DamageBundle { Weapon = new object() }, Target = target };
curse.OnEventAboutToTrigger(stale);
Equal(0, stale.Added.Count, "a stale buff from a previous seance stays inert");

// ---------------------------------------------------------------------------
// D. The ability restriction: only while the required spirit is channeled.
// ---------------------------------------------------------------------------
attackerState.PrimarySpirit = adrielRef;
var requirement = new AbilityRequirementSpecificSpirit { Spirit = adrielRef };
var ability = new AbilityData { Caster = attacker.Descriptor };
Check(requirement.IsAbilityRestrictionPassed(ability), "channeled spirit passes the restriction");
attackerState.PrimarySpirit = championRef;
Check(!requirement.IsAbilityRestrictionPassed(ability), "another spirit fails the restriction");
var inverted = new AbilityRequirementSpecificSpirit { Spirit = adrielRef, Not = true };
Check(inverted.IsAbilityRestrictionPassed(ability), "the inverted restriction passes while unchanneled");
attackerState.PrimarySpirit = adrielRef;
Check(!inverted.IsAbilityRestrictionPassed(ability), "the inverted restriction fails while channeled");
var casterless = new AbilityData();
Check(!requirement.IsAbilityRestrictionPassed(casterless), "no caster fails the positive restriction");
Check(inverted.IsAbilityRestrictionPassed(casterless), "no caster passes the inverted restriction");
Check(!new AbilityRequirementSpecificSpirit().IsAbilityRestrictionPassed(ability), "a missing spirit fails the positive restriction");
Check(new AbilityRequirementSpecificSpirit { Not = true }.IsAbilityRestrictionPassed(ability), "a missing spirit passes the inverted restriction");
Equal("Requires this spirit to be channeled.", requirement.GetAbilityRestrictionUIText(), "restriction text is english by default");
LocalizationManager.CurrentLocale = Locale.zhCN;
Equal("需要先降灵该英灵。", requirement.GetAbilityRestrictionUIText(), "restriction text follows the chinese locale");
LocalizationManager.CurrentLocale = Locale.enGB;

// ---------------------------------------------------------------------------
// E. Two channeling mediums on one ally: each stat mirrors the highest offer.
// ---------------------------------------------------------------------------
var low = Medium(out var lowState, championRef, 0, 2,
    StatType.AdditionalAttackBonus, StatType.SaveFortitude);
var high = Medium(out var highState, guardianRef, 1, 3, StatType.AdditionalAttackBonus);
var sharedAlly = new UnitEntityData();
Game.Instance.Player.ActiveCompanions.Add(sharedAlly);

var lowMarker = new Buff { Blueprint = markerBlueprint };
low.Buffs.Enumerable.Add(lowMarker);
var lowInfluence = new MediumContextSpiritInfluenceComponent { Owner = low, Fact = lowMarker };
lowMarker.Components.Add(lowInfluence);
lowInfluence.OnTurnOn();

var highMarker = new Buff { Blueprint = markerBlueprint };
high.Buffs.Enumerable.Add(highMarker);
var highInfluence = new MediumContextSpiritInfluenceComponent { Owner = high, Fact = highMarker };
highMarker.Components.Add(highInfluence);
highInfluence.OnTurnOn();

var sharedComponent = sharedAlly.Facts.Get(allyBlueprint)
    .Components.OfType<MediumSpiritInfluenceAllyComponent>().Single();
Equal(low, sharedComponent.Data.Provider, "the medium that granted first keeps the grant");
Equal(4, SingleModifier(sharedAlly, StatType.AdditionalAttackBonus, "two mediums overlap").Amount,
    "a stat two mediums cover takes the higher value, never the sum");
Equal(2, SingleModifier(sharedAlly, StatType.SaveFortitude, "only the weaker medium has this stat").Amount,
    "a stat only one medium covers still mirrors that medium");
Check(lowInfluence.Data.Grants.Count >= 1, "the first medium still owns its grant");

high.Buffs.Enumerable.Remove(highMarker);
highInfluence.OnTurnOff();
Equal(2, SingleModifier(sharedAlly, StatType.AdditionalAttackBonus, "the stronger medium stopped").Amount,
    "the mirror falls back to the runner-up without restarting the seance");
Check(low.Facts.Get(allyBlueprint) == null,
    "a mirror one medium granted to another ends with its grantor instead of self-hosting");

low.Buffs.Enumerable.Remove(lowMarker);
lowInfluence.OnTurnOff();
Check(sharedAlly.Facts.Get(allyBlueprint) == null, "the ally fact ends with the last channeling medium");
Equal(0, sharedAlly.Stats.GetStat(StatType.AdditionalAttackBonus).Modifiers.Count,
    "both mediums gone leaves no mirrored modifier");

// Equal offers stay one modifier, and a holder can hand its grant over.
var holder = Medium(out var holderState, championRef, 1, 3, StatType.SaveWill);
var other = Medium(out var otherState, guardianRef, 0, 4, StatType.SaveWill);
var tieAlly = new UnitEntityData();
Game.Instance.Player.ActiveCompanions.Add(tieAlly);

var holderMarker = new Buff { Blueprint = markerBlueprint };
holder.Buffs.Enumerable.Add(holderMarker);
var holderInfluence = new MediumContextSpiritInfluenceComponent { Owner = holder, Fact = holderMarker };
holderMarker.Components.Add(holderInfluence);
holderInfluence.OnTurnOn();

var otherMarker = new Buff { Blueprint = markerBlueprint };
other.Buffs.Enumerable.Add(otherMarker);
var otherInfluence = new MediumContextSpiritInfluenceComponent { Owner = other, Fact = otherMarker };
otherMarker.Components.Add(otherInfluence);
otherInfluence.OnTurnOn();

Equal(4, SingleModifier(tieAlly, StatType.SaveWill, "two mediums tie").Amount,
    "a tie yields one modifier at that value, not two");

holder.Buffs.Enumerable.Remove(holderMarker);
holderInfluence.OnTurnOff();
var tieComponent = tieAlly.Facts.Get(allyBlueprint)
    ?.Components.OfType<MediumSpiritInfluenceAllyComponent>().Single();
Check(tieComponent != null && tieComponent.Fact.IsActive, "the grant holder leaving keeps the mirror alive");
Equal(other, tieComponent?.Data.Provider, "the grant moves to the medium that is still channeling");
Equal(4, SingleModifier(tieAlly, StatType.SaveWill, "the holder stopped").Amount,
    "the remaining medium keeps the same value");

other.Buffs.Enumerable.Remove(otherMarker);
otherInfluence.OnTurnOff();
Check(tieAlly.Facts.Get(allyBlueprint) == null, "the last channeler leaving removes the mirror");
Equal(0, tieAlly.Stats.GetStat(StatType.SaveWill).Modifiers.Count, "no modifier survives the final seance");

// ---------------------------------------------------------------------------
// F. The own bonus and mirror execute together, including intermediate turn-off.
Game.Instance.Player.ActiveCompanions.Clear();
var external = Medium(out var externalState, championRef, 0, 4, StatType.AdditionalAttackBonus);
var receiver = Medium(out var receiverState, championRef, 0, 2, StatType.AdditionalAttackBonus);
var externalMarker = new Buff { Blueprint = markerBlueprint };
external.Buffs.Enumerable.Add(externalMarker);
var receiverFact = receiver.AddFact(allyBlueprint);
var receiverMirror = receiverFact.Components.OfType<MediumSpiritInfluenceAllyComponent>().Single();
receiverMirror.SetProvider(external);
int Total(UnitEntityData unit) => unit.Stats.GetStat(StatType.AdditionalAttackBonus).Modifiers.Sum(m => m.Amount);
Equal(4, Total(receiver), "a saved spirit selection without an enabled own bonus cannot reduce the mirror");
var receiverBuff = new Buff { Blueprint = ownBonusBlueprint, IsTurnedOn = false };
receiver.Buffs.Enumerable.Add(receiverBuff);
receiverMirror.RefreshModifiers();
Equal(4, Total(receiver), "an unturned-on own buff cannot reduce the mirror");
var ownBonus = new MediumContextSpiritBonusComponent { Owner = receiver, Fact = receiverBuff };
receiverBuff.Components.Add(ownBonus);
receiverBuff.IsTurnedOn = true;
ownBonus.OnTurnOn();
Equal(4, Total(receiver), "own +2 and external +4 total +4");
receiver.Progression.Features.Counts[bonusFeature] = 6;
ownBonus.RefreshModifiers();
Equal(6, Total(receiver), "own +6 wins over external +4");
receiverBuff.IsTurnedOn = false;
ownBonus.OnTurnOff();
Equal(4, Total(receiver), "own turn-off retains full external +4");
receiverMirror.RefreshModifiers();
Equal(4, Total(receiver), "a second refresh during own turn-off cannot subtract the stale spirit selection");
external.Progression.Features.Counts[bonusFeature] = 2;
receiverMirror.RefreshModifiers();
Equal(2, Total(receiver), "provider change while own buff is off tracks the provider");
receiverBuff.IsTurnedOn = true;
ownBonus.OnTurnOn();
Equal(6, Total(receiver), "own turn-on restores max without stacking");
external.Buffs.Enumerable.Remove(externalMarker);
receiverMirror.RefreshModifiers();
Equal(6, Total(receiver), "provider exit preserves active own bonus");
receiverBuff.IsTurnedOn = false;
ownBonus.OnTurnOff();
Equal(0, Total(receiver), "both effects ended leaves no modifier");

// G. Installer audit: every new GUID and localization key is really wired.
// ---------------------------------------------------------------------------
// Marshal's passive scope must also propagate correctly through the mythic mirror.
{
    Game.Instance.Player.ActiveCompanions.Clear();
    Register(Guids.Marshal, new BlueprintCharacterClass {Name = "Marshal"});
    var marshalRef = BlueprintTool.GetRef<BlueprintCharacterClassReference>(Guids.Marshal);
    var marshalProvider = Medium(out var marshalState, marshalRef, 1, 3,
        MediumClass.Medium.MarshalSpiritRules.PassiveBonusStats);
    var recipient = new UnitEntityData();
    Game.Instance.Player.ActiveCompanions.Add(recipient);
    var marker = new Buff {Blueprint = markerBlueprint};
    marshalProvider.Buffs.Enumerable.Add(marker);
    var grant = new MediumContextSpiritInfluenceComponent {Owner = marshalProvider, Fact = marker};
    marker.Components.Add(grant);
    grant.OnTurnOn();
    var mirror = recipient.Facts.Get(allyBlueprint).Components.OfType<MediumSpiritInfluenceAllyComponent>().Single();
    foreach(var skill in MediumClass.Medium.MarshalSpiritRules.PassiveBonusStats)
        Equal(4, recipient.Stats.GetStat(skill).Modifiers.Sum(m => m.Amount), "Marshal mirror grants Charisma skill plus focus: "+skill);
    foreach(var value in new[]{StatType.AdditionalAttackBonus, StatType.SaveFortitude, StatType.SkillKnowledgeArcana})
        Equal(0, recipient.Stats.GetStat(value).Modifiers.Sum(m => m.Amount), "Marshal mirror does not widen passive scope: "+value);
    recipient.Stats.GetStat(StatType.SkillKnowledgeWorld).AddModifier(4, mirror.Runtime, Kingmaker.Enums.ModifierDescriptor.UntypedStackable);
    grant.RefreshAllies();
    Equal(0, recipient.Stats.GetStat(StatType.SkillKnowledgeWorld).Modifiers.Sum(m => m.Amount), "Marshal mirror refresh clears legacy non-Charisma modifier");
    grant.OnTurnOff();
    foreach(var skill in MediumClass.Medium.MarshalSpiritRules.PassiveBonusStats)
        Equal(0, recipient.Stats.GetStat(skill).Modifiers.Sum(m => m.Amount), "Marshal mirror ends with provider: "+skill);
}

if (args.Length >= 1 && File.Exists(args[0]))
{
    using var mod = AssemblyDefinition.ReadAssembly(args[0]);
    IEnumerable<MethodDefinition> AllMethods(TypeDefinition type)
    {
        foreach (var method in type.Methods) yield return method;
        foreach (var nested in type.NestedTypes)
            foreach (var method in AllMethods(nested)) yield return method;
    }
    var bodies = mod.MainModule.Types.SelectMany(AllMethods)
        .Select(m => m.Body).Where(b => b != null).ToArray();
    var literals = bodies.SelectMany(b => b.Instructions).Where(i => i.OpCode == OpCodes.Ldstr)
        .Select(i => (string)i.Operand).ToHashSet();
    var methodLiterals = bodies.ToDictionary(
        b => b.Method,
        b => b.Instructions.Where(i => i.OpCode == OpCodes.Ldstr).Select(i => (string)i.Operand).ToHashSet());

    string[] newGuids =
    {
        Guids.MediumSpiritInfluenceBuff, Guids.MediumSpiritInfluenceAlly,
        Guids.Adriel, Guids.AdrielProgression, Guids.MediumChannelSpiritAbilityAdriel,
        Guids.MediumAdrielChallenge, Guids.MediumAdrielChallengeAbility,
        Guids.MediumAdrielChallengeBuffTarget, Guids.MediumAdrielChallengeBuffSelf,
        Guids.MediumAdrielChallengeResource, Guids.MediumAdrielDeathCurse, Guids.MediumAdrielDeathCurseAbility,
        Guids.MediumAdrielDeathCurseAbilityNegative, Guids.MediumAdrielDeathCurseAbilityFire,
        Guids.MediumAdrielDeathCurseAbilityCold, Guids.MediumAdrielDeathCurseAbilityElectricity,
        Guids.MediumAdrielDeathCurseAbilityAcid,
        Guids.SpiritBinderArchetype, Guids.SpiritBinderFeature, Guids.SpiritBinderBonusFeat,
    };
    foreach (var guid in newGuids)
        Check(literals.Contains(guid), "the installer references new GUID " + guid);

    // Each energy is its own toggle buff; switching one off, and the seance
    // ending, both go through ContextActionRemoveBuff.
    string[] curseBuffs =
    {
        Guids.MediumAdrielDeathCurseBuffNegative, Guids.MediumAdrielDeathCurseBuffFire,
        Guids.MediumAdrielDeathCurseBuffCold, Guids.MediumAdrielDeathCurseBuffElectricity,
        Guids.MediumAdrielDeathCurseBuffAcid,
    };
    foreach (var guid in curseBuffs)
        Check(literals.Contains(guid), "the installer references death-curse buff " + guid);

    var curseType = mod.MainModule.GetType("MediumClass.Medium.Spirits.Adriel.DeathCurse");
    Check(curseType != null, "the installer ships Adriel's death curse");
    bool RemovesABuff(MethodDefinition method) => method.Body != null
        && method.Body.Instructions.Any(i => i.OpCode == OpCodes.Stfld
            && i.Operand is FieldReference field
            && field.Name == "m_Buff"
            && field.DeclaringType.FullName == "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff");
    Check(curseType != null && AllMethods(curseType).Any(RemovesABuff),
        "the curse toggles and the seance end remove curse buffs");

    // 0.3.4: the two-energy limit is decided in the toggle action the five variants run, not in a plain apply/remove
    // conditional; the UI restriction is attached, and the restore entry trims old saves.
    IEnumerable<string> TypeArguments(MethodDefinition method) => method.Body == null ? Enumerable.Empty<string>()
        : method.Body.Instructions.Select(i => i.Operand).OfType<GenericInstanceMethod>().SelectMany(g => g.GenericArguments).Select(a => a.Name);
    var curseTypeArguments = curseType == null ? new string[0] : AllMethods(curseType).SelectMany(TypeArguments).ToArray();
    Check(curseTypeArguments.Contains("ContextActionToggleDeathCurse"), "the five variants run ContextActionToggleDeathCurse");
    Check(curseTypeArguments.Contains("AbilityRequirementDeathCurseSlot"), "the variants carry the two-energy UI restriction");
    Check(!curseTypeArguments.Contains("ContextActionApplyBuff"), "no variant applies its buff directly, bypassing the limit");
    foreach (var (typeName, id) in new[] { ("MediumClass.Medium.NewActions.ContextActionToggleDeathCurse", "8312c244-24dc-4983-aa1a-68f8677f61c5"),
        ("MediumClass.Medium.NewComponents.AbilityRequirementDeathCurseSlot", "19866215-af68-4eaa-9aac-0eb6e6454c4d") })
    {
        var type = mod.MainModule.GetType(typeName);
        var typeId = type?.CustomAttributes.SingleOrDefault(a => a.AttributeType.Name == "TypeIdAttribute")?.ConstructorArguments[0].Value as string;
        Check(typeId == id, typeName + " ships with its new unique TypeId");
        Check(mod.MainModule.GetTypes().Count(t => t.CustomAttributes.Any(a => a.AttributeType.Name == "TypeIdAttribute"
            && (a.ConstructorArguments[0].Value as string) == id)) == 1, "TypeId " + id + " is used by exactly one type in the installer");
    }
    var restoreCalls = mod.MainModule.GetType("MediumClass.Medium.MediumChannelRestore").Methods.Single(m => m.Name == "Restore")
        .Body.Instructions.Select(i => (i.Operand as MethodReference)?.Name).Where(n => n != null).ToList();
    Check(restoreCalls.Contains("RemoveLegacyBuffs") && restoreCalls.IndexOf("NormalizeActiveEnergies") > restoreCalls.IndexOf("RemoveLegacyBuffs"),
        "the restore entry removes legacy pairs, then trims the active energies");

    // Resonance: the penalty tracks the same shared spirit focus the bonus uses,
    // and the ally mirror surveys every channeling medium instead of one provider.
    var penalty = mod.MainModule.GetType(
        "MediumClass.Medium.NewComponents.AbilitySpecific.MediumInfluencePenaltyComponent");
    Check(penalty != null, "the installer ships the resonance penalty component");
    bool Calls(MethodDefinition method, string name) => method.Body != null
        && method.Body.Instructions.Any(i => i.Operand is MethodReference call && call.Name == name);
    Check(penalty != null && AllMethods(penalty).Any(m => Calls(m, "EffectiveSpiritFocus")),
        "the penalty resolves the effective spirit focus through MultiSpiritRules");
    Check(penalty != null && !AllMethods(penalty).Where(m => m.Body != null)
        .SelectMany(m => m.Body.Instructions)
        .Any(i => i.OpCode == OpCodes.Ldfld && i.Operand is FieldReference raw
            && raw.Name == "SpiritFocus"),
        "the penalty never reads the raw SpiritFocus field");

    var allyMirror = mod.MainModule.GetType(
        "MediumClass.Medium.NewComponents.AbilitySpecific.MediumSpiritInfluenceAllyComponent");
    Check(allyMirror != null, "the installer ships the ally mirror component");
    Check(allyMirror != null && AllMethods(allyMirror).Any(m => Calls(m, "get_ActiveCompanions")),
        "the ally mirror enumerates the whole party to find every channeling medium");
    Check(allyMirror != null && AllMethods(allyMirror).Any(m => Calls(m, "EffectiveSpiritFocus")),
        "the ally mirror resolves the effective spirit focus through MultiSpiritRules");

    // Production wiring: the Marshal registration uses the narrow passive scope,
    // while the surge component has its own broad active scope.
    var marshalRegistration = AllMethods(mod.MainModule.GetType("MediumClass.Medium.ChannelSpirit"))
        .SingleOrDefault(m => Calls(m, "get_PassiveBonusStats"));
    Check(marshalRegistration != null, "channel catalogue binds Marshal passive Charisma skills");
    Check(marshalRegistration != null && marshalRegistration.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldstr
        && (string)i.Operand == Guids.Marshal), "narrow passive scope belongs to Marshal registration");
    Check(marshalRegistration != null && marshalRegistration.Body.Instructions.Any(i => i.OpCode == OpCodes.Stfld
        && i.Operand is FieldReference field && field.Name == "Concentration" && i.Previous.OpCode == OpCodes.Ldc_I4_0),
        "Marshal catalogue no longer grants passive concentration");
    Check(AllMethods(mod.MainModule.GetType("MediumClass.Medium.NewComponents.AbilitySpecific.MediumSpiritSurgeComponent"))
        .Any(m => Calls(m, "get_SurgeStats")), "active Marshal surge uses separate broad roll coverage");

    // The Adriel progression publishes exactly the four spirit powers on time.
    var progression = mod.MainModule.GetType("MediumClass.Medium.Spirits.Adriel.Adriel")
        ?.Methods.SingleOrDefault(m => m.Name == "AdrielProgression");
    Check(progression != null, "the installer ships Adriel's progression");
    if (progression != null)
    {
        var il = progression.Body.Instructions;
        var entries = new List<(int Level, string Guid)>();
        for (int i = 0; i < il.Count; i++)
        {
            if (il[i].Operand is not MethodReference call || call.Name != "AddEntry") continue;
            int newArray = -1;
            for (int back = i - 1; back >= 0 && newArray < 0; back--)
                if (il[back].OpCode == OpCodes.Newarr) newArray = back;
            if (newArray < 2) continue;
            // il[newArray-1] is the array length; the level constant sits before it.
            int? level = null;
            for (int back = newArray - 2; back >= 0 && level == null; back--)
                level = il[back].OpCode.Code switch
                {
                    Code.Ldc_I4_0 => 0, Code.Ldc_I4_1 => 1, Code.Ldc_I4_2 => 2, Code.Ldc_I4_3 => 3,
                    Code.Ldc_I4_4 => 4, Code.Ldc_I4_5 => 5, Code.Ldc_I4_6 => 6, Code.Ldc_I4_7 => 7,
                    Code.Ldc_I4_8 => 8, Code.Ldc_I4_S => Convert.ToInt32(il[back].Operand),
                    Code.Ldc_I4 => Convert.ToInt32(il[back].Operand), _ => null
                };
            if (level == null) continue;
            for (int inside = newArray + 1; inside < i; inside++)
                if (il[inside].OpCode == OpCodes.Ldstr) entries.Add((level.Value, (string)il[inside].Operand));
        }
        Equal(4, entries.Count, "Adriel grants four powers across the tier");
        Check(new[] { 1, 6, 11, 17 }.SequenceEqual(entries.Select(e => e.Level)),
            "lesser at 1, intermediate at 6, greater at 11, supreme at 17");
        Equal(Guids.MediumAdrielChallenge, entries.ElementAtOrDefault(0).Guid, "level 1 grants Challenge");
        Equal(Guids.ChampionSuddenAttack, entries.ElementAtOrDefault(1).Guid, "level 6 keeps Sudden Attack");
        Equal(Guids.ChampionFleetCharge, entries.ElementAtOrDefault(2).Guid, "level 11 keeps Fleet Charge");
        Equal(Guids.MediumAdrielDeathCurse, entries.ElementAtOrDefault(3).Guid, "level 17 grants Death Curse");
    }

    var channelType = mod.MainModule.GetType("MediumClass.Medium.ChannelSpirit");
    Check(channelType != null, "the installer ships the channel spirit selection");
    if (channelType != null)
    {
        // The spirit component is configured inside lambdas, which the compiler
        // emits as separate methods, so collect the whole type's literals.
        var channelLiterals = AllMethods(channelType).Where(m => methodLiterals.ContainsKey(m))
            .SelectMany(m => methodLiterals[m]).ToHashSet();
        foreach (var (label, guid) in new[]
        {
            ("channel ability", Guids.MediumChannelSpiritAbilityAdriel),
            ("spirit class", Guids.Adriel),
            ("lesser power", Guids.MediumAdrielChallenge),
            ("intermediate power", Guids.ChampionSuddenAttack),
            ("greater power", Guids.ChampionFleetCharge),
            ("supreme power", Guids.MediumAdrielDeathCurse),
        })
            Check(channelLiterals.Contains(guid), "the channel selection wires Adriel's " + label);
    }

    foreach (var (typeName, label, guids) in new[]
    {
        ("MediumClass.Medium.Spirits.Adriel.Challenge", "Challenge", new[]
        {
            Guids.MediumAdrielChallenge, Guids.MediumAdrielChallengeAbility, Guids.MediumAdrielChallengeBuffTarget,
            Guids.MediumAdrielChallengeBuffSelf, Guids.MediumAdrielChallengeResource,
        }),
        ("MediumClass.Medium.Spirits.Adriel.DeathCurse", "Death Curse", new[]
        {
            Guids.MediumAdrielDeathCurse, Guids.MediumAdrielDeathCurseAbility,
            Guids.MediumAdrielDeathCurseAbilityNegative, Guids.MediumAdrielDeathCurseAbilityFire,
            Guids.MediumAdrielDeathCurseAbilityCold, Guids.MediumAdrielDeathCurseAbilityElectricity,
            Guids.MediumAdrielDeathCurseAbilityAcid,
            Guids.MediumAdrielDeathCurseBuffNegative, Guids.MediumAdrielDeathCurseBuffFire,
            Guids.MediumAdrielDeathCurseBuffCold, Guids.MediumAdrielDeathCurseBuffElectricity,
            Guids.MediumAdrielDeathCurseBuffAcid,
        }),
    })
    {
        var type = mod.MainModule.GetType(typeName);
        Check(type != null, "the installer ships Adriel's " + label);
        if (type != null)
        {
            var typeLiterals = AllMethods(type).Where(m => methodLiterals.ContainsKey(m))
                .SelectMany(m => methodLiterals[m]).ToHashSet();
            foreach (var guid in guids)
                Check(typeLiterals.Contains(guid), "Adriel's " + label + " references " + guid);
        }
    }

    // Legacy identities stay resolvable for saved buffs and ability contexts.
    var deathCurseType = mod.MainModule.GetType("MediumClass.Medium.Spirits.Adriel.DeathCurse");
    if (deathCurseType != null)
    {
        var curseLiterals = AllMethods(deathCurseType).Where(m => methodLiterals.ContainsKey(m))
            .SelectMany(m => methodLiterals[m]).ToHashSet();
        foreach (var legacy in new[]
        {
            Guids.MediumAdrielDeathCurseAbilityNegativeFire, Guids.MediumAdrielDeathCurseBuffNegativeFire,
            Guids.MediumAdrielDeathCurseAbilityNegativeCold, Guids.MediumAdrielDeathCurseBuffNegativeCold,
            Guids.MediumAdrielDeathCurseAbilityNegativeElectricity, Guids.MediumAdrielDeathCurseBuffNegativeElectricity,
            Guids.MediumAdrielDeathCurseAbilityNegativeAcid, Guids.MediumAdrielDeathCurseBuffNegativeAcid,
            Guids.MediumAdrielDeathCurseAbilityFireCold, Guids.MediumAdrielDeathCurseBuffFireCold,
            Guids.MediumAdrielDeathCurseAbilityFireElectricity, Guids.MediumAdrielDeathCurseBuffFireElectricity,
            Guids.MediumAdrielDeathCurseAbilityFireAcid, Guids.MediumAdrielDeathCurseBuffFireAcid,
            Guids.MediumAdrielDeathCurseAbilityColdElectricity, Guids.MediumAdrielDeathCurseBuffColdElectricity,
            Guids.MediumAdrielDeathCurseAbilityColdAcid, Guids.MediumAdrielDeathCurseBuffColdAcid,
            Guids.MediumAdrielDeathCurseAbilityElectricityAcid, Guids.MediumAdrielDeathCurseBuffElectricityAcid,
        })
            Check(curseLiterals.Contains(legacy), "Death Curse keeps legacy blueprint resolvable " + legacy);
    }

    // Localization: every 0.3.2 key exists in the shipped pack with both
    // languages. (The configurators build most name keys by string
    // concatenation, so only presence in the pack is asserted, not an IL
    // literal; the GUID checks above cover the blueprint wiring.)
    string[] keys =
    {
        "SpiritFocus.Name", "SpiritFocus.Description", "SpiritFocusArchmage.Name", "SpiritFocusChampion.Name",
        "SpiritFocusGuardian.Name", "SpiritFocusHierophant.Name", "SpiritFocusMarshal.Name", "SpiritFocusTrickster.Name",
        "MediumSpiritInfluence.Name", "MediumSpiritInfluence.Description", "Adriel.Name", "Adriel.Description",
        "MediumAdrielChallenge.Name", "MediumAdrielChallenge.Description", "MediumAdrielChallengeBuffTarget.Name",
        "MediumAdrielChallengeBuffTarget.Description", "MediumAdrielChallengeBuffSelf.Name",
        "MediumAdrielChallengeBuffSelf.Description", "MediumAdrielChallengeAbility.Name",
        "MediumAdrielChallengeAbility.Description", "MediumAdrielDeathCurse.Name", "MediumAdrielDeathCurse.Description",
        "SpiritBinderArchetype.Name", "SpiritBinderArchetype.Description", "SpiritBinderBonusFeat.Name",
        "SpiritBinderBonusFeat.Description", "SpiritBinderRestlessSpirit.Name", "SpiritBinderRestlessSpirit.Description",
    };
    keys = keys.Concat(new[] { "Negative", "Fire", "Cold", "Electricity", "Acid" }
        .Select(suffix => "MediumAdrielDeathCurse" + suffix + ".Name")).ToArray();

    var stringsPath = args.Length > 1 ? args[1] : "Strings/Feats.json";
    var shipped = new Dictionary<string, (string English, string Chinese)>();
    try
    {
        using var pack = JsonDocument.Parse(File.ReadAllText(stringsPath));
        foreach (var entry in pack.RootElement.EnumerateArray())
            shipped[entry.GetProperty("Key").GetString()] = (
                entry.GetProperty("enGB").GetString(), entry.GetProperty("zhCN").GetString());
    }
    catch (Exception e) { failures.Add("shipped localization could not be read: " + e.Message); }
    foreach (var key in keys)
    {
        Check(shipped.ContainsKey(key), "shipped localization defines " + key);
        if (shipped.TryGetValue(key, out var texts))
        {
            Check(!string.IsNullOrWhiteSpace(texts.English), key + " has english text");
            Check(!string.IsNullOrWhiteSpace(texts.Chinese), key + " has chinese text");
        }
    }
    Equal(33, keys.Length, "the 0.3.2 localization set is complete");
}
else
{
    Console.WriteLine("Installer wiring audit skipped: pass the staged MediumClass.dll [and Strings/Feats.json].");
}

Report();
return failures.Count == 0 ? 0 : 1;

void Report()
{
    Console.WriteLine($"Spirit influence: {checks - failures.Count}/{checks} checks passed; linked production components on explicit API doubles"
        + (args.Length >= 1 ? "; installer GUID, progression, wiring and localization audited" : "") + "; no Unity gameplay.");
    foreach (var failure in failures) Console.Error.WriteLine(failure);
}
