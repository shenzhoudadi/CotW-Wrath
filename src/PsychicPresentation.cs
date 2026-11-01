using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicPresentation
    {
        internal static void Initialize(BlueprintScriptableObject asset, string token)
        {
            if (asset is BlueprintBuff buff) {
                // Native Buff.OnRemove calls FxOnRemove.Load before its removal
                // notification. A missing link object aborts that notification.
                buff.FxOnStart = buff.FxOnStart ?? new PrefabLink();
                buff.FxOnRemove = buff.FxOnRemove ?? new PrefabLink();
            }
            if (asset is BlueprintAbility ability) Labels(ability, token, "Instantaneous", "");
        }
        private static void Labels(BlueprintAbility ability, string token, string duration, string save)
        {
            ability.LocalizedDuration = Text(token, "duration", duration);
            // A valid empty string suppresses the whole native saving-throw
            // block. "None" creates a row, while a null label can show <null>.
            ability.LocalizedSavingThrow = Text(token, "savingThrow", save);
        }
        private static void Icon(BlueprintUnitFact target, BlueprintUnitFact source) => Set(target, "m_Icon", source.Icon);
        private static void Fx(BlueprintAbility ability, string id, bool caster = false, bool destroy = false)
        {
            ability.ComponentsArray = ability.ComponentsArray.Concat(new BlueprintComponent[] {
                new AbilitySpawnFx { name = "PsychicCastVisual", PrefabLink = new PrefabLink { AssetId = id },
                    Time = AbilitySpawnFxTime.OnApplyEffect,
                    Anchor = caster ? AbilitySpawnFxAnchor.Caster : AbilitySpawnFxAnchor.SelectedTarget,
                    PositionAnchor = AbilitySpawnFxAnchor.None, OrientationAnchor = AbilitySpawnFxAnchor.None,
                    DestroyOnCast = destroy }
            }).ToArray();
        }
        internal static void Center(BlueprintsCache cache, BlueprintAbility ability, BlueprintBuff buff)
        {
            var f = new PsychicAssets(cache);
            var icon = f.Native<BlueprintBuff>("00369aa0a76141a479382e360b1f3dd7");
            Initialize(buff, "CenterSelfBuff"); Icon(ability, icon); Icon(buff, icon);
            Labels(ability, "CenterSelf", "1 round", "");
            // CotW Center Self has an icon but deliberately has no cast particle.
        }
        internal static void Expansion(PsychicAssets f)
        {
            // CotW uses these native icons, not bespoke image files. All are also
            // present in Wrath. Prefab IDs are checked against Wrath locationlist.
            var thrust = f.Native<BlueprintAbility>("dd2a5a6e76611c04e9eac6254fcf8c6b");
            var barrier = f.Native<BlueprintAbility>("62888999171921e4dafb46de83f4d67d");
            var shield = f.Native<BlueprintAbility>("eabf94e4edc6e714cabd96aa69f8b207");
            var fortress = f.Native<BlueprintAbility>("183d5bb91dea3a1489a6db6c9cb64445");
            var pulse = f.Native<BlueprintBuff>("df3950af5a783bd4d91ab73eb8fa0fd3");
            // CotW Countless Eyes inherits this native icon (Remove Blindness).
            var senses = f.Native<BlueprintAbility>("c927a8b0cd3f5174f8c0b67cdbfde539");
            var rage = f.Native<BlueprintAbility>("97b991256e43bb140b263c326f690ce2");
            var transformation = f.Native<BlueprintBuff>("287682389d2011b41b5a65195d9cbc84");
            foreach (var pair in f.Assets) {
                string token = pair.Key;
                if(token.StartsWith("ChannelVigor")||token.StartsWith("BladeTutor"))continue;
                if(PsychicStorageSpells.Tokens.Concat(PsychicBoundWeapons.Tokens).Any(t=>token.StartsWith(t))||PsychicSpiritWeapons.Tokens.Any(t=>token.StartsWith(t))||PsychicLinkedWard.Tokens.Any(t=>token.StartsWith(t))||token.StartsWith("FieryShuriken")||token.StartsWith("OrbOfTheVoid")||token.StartsWith("HexVulnerability")||token.StartsWith("FireSeeds")||token.StartsWith("TemporalStasis")||PsychicSpiritSpells.Tokens.Any(t=>token.StartsWith(t))||token.StartsWith("MagicalBeastShape")||PsychicConduitSpells.Tokens.Any(t=>token.StartsWith(t))||token.StartsWith("BloodyClaws")||PsychicShapeSpells.Tokens.Concat(PsychicCataclysmSpells.Tokens).Concat(PsychicOrbSpells.Tokens).Concat(PsychicTravelSpells.Tokens).Concat(PsychicSupportSpells.Tokens).Concat(PsychicCurseSpells.Tokens).Concat(PsychicWeatherSpells.Tokens).Concat(PsychicAspectSpells.Tokens).Concat(PsychicAreaSpells.Tokens).Concat(PsychicTacticalSpells.Tokens).Concat(PsychicWeaponSpells.Tokens).Any(t=>token.StartsWith(t))||PsychicBattleSpells.Tokens.Any(t=>token.StartsWith(t)))continue;
                if(token.StartsWith("GhostbaneDirge"))continue;
                if(token.StartsWith("FluidForm")||token.StartsWith("SecondWind"))continue;
                if(token.StartsWith("DebilitatingPortent") || token.StartsWith("IrresistibleDance"))continue;
                if(token.StartsWith("GlobeOfInvulnerability"))continue;
                if(token.StartsWith("SavageMaw") || token.StartsWith("BloodArmor"))continue;
                if(token.StartsWith("WardingWeapon") || token.StartsWith("DazeMass"))continue;
                if(token.StartsWith("HaltUndead") || token.StartsWith("ControlUndead"))continue;
                if(token.StartsWith("Invigorate") || token.StartsWith("HowlingAgony"))continue;
                if(token.StartsWith("DivideMind") || token.StartsWith("LocateWeakness"))continue;
                if(token.StartsWith("PainStrike") || token.StartsWith("Synesthesia") || token.StartsWith("SynapseOverload") || token.StartsWith("TelekineticStorm") || token.StartsWith("PsychicCrush"))continue;
                if(token.StartsWith("Pageantry") || token.StartsWith("Silence") || token.StartsWith("Faith") || token.StartsWith("BurstOf") || token.StartsWith("CountlessEyes") || token.StartsWith("ForceSword") || token.StartsWith("PsychicSurgery")) continue; // Builders supply their icons, labels and native visual components.
                if (!(pair.Value is BlueprintUnitFact fact)) continue;
                BlueprintUnitFact donor = token.StartsWith("MindThrust") ? thrust :
                    token.StartsWith("MentalBarrier") ? barrier : token.StartsWith("ThoughtShield") ? shield :
                    token.StartsWith("IntellectFortress") ? fortress : token.StartsWith("SynapticPulse") ? pulse :
                    token.StartsWith("FerocitySenses") || token.StartsWith("FerocityBlind") ? senses :
                    token.StartsWith("FerocityPrimalFury") ? transformation : rage;
                if (token.StartsWith("FerocityBonusSpell")) {
                    int n = int.Parse(token.Substring("FerocityBonusSpell".Length));
                    donor = f.Native<BlueprintAbility>(PsychicDisciplines.FerocitySpells[n - 1]);
                }
                Icon(fact, donor);
                if (!(fact is BlueprintAbility ability)) continue;
                if (token.StartsWith("MindThrust")) {
                    Labels(ability, token, int.Parse(token.Substring(10)) < 4 ? "Instantaneous" : "Instantaneous; conditions last 1 round", "Will half; see text");
                    Fx(ability, "c388856d0e8855f429a83ccba67944ba");
                } else if (token.StartsWith("MentalBarrier")) {
                    Labels(ability, token, "2 rounds", ""); Fx(ability, "6bebb42fd1a2ab9499d19f7a1dce3359");
                } else if (token.StartsWith("ThoughtShield") || token == "IntellectFortress") {
                    Labels(ability, token, "2 rounds", ""); Fx(ability, "c4d861e816edd6f4eab73c55a18fdadd");
                } else if (token.StartsWith("SynapticPulse")) {
                    bool greater = token.EndsWith("Greater");
                    Labels(ability, token, greater ? "1d4 rounds; sickness 1 round" : "1 round", greater ? "Will partial" : "Will negates");
                    Fx(ability, "9012980c54f45ee428aa717c69446820", true, true);
                } else if (token.StartsWith("FerocitySenses")) {
                    Labels(ability, token, "1 minute", ""); Fx(ability, "8de64fbe047abc243a9b4715f643739f");
                }
            }
            f.Find<BlueprintBuff>("IntellectFortressEffect").FxOnStart = new PrefabLink { AssetId = "d541609d507424640a84153b89abf210" };
            f.Find<BlueprintAbilityAreaEffect>("IntellectFortressArea").Fx = new PrefabLink { AssetId = "63f322580ec0e7c4c96fc62ecabad40f" };
        }
    }
}
