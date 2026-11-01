using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicBurstSpells
    {
        internal static bool IsBurst(BlueprintAbility spell) => spell != null &&
            (spell.AssetGuid == Id("BurstOfAdrenaline") || spell.AssetGuid == Id("BurstOfInsight") ||
             spell.Parent != null && (spell.Parent.AssetGuid == Id("BurstOfAdrenaline") || spell.Parent.AssetGuid == Id("BurstOfInsight")));

        internal static string KnownState(Spellbook book) => string.Join("; ", new[] { "BurstOfAdrenaline", "BurstOfInsight" }.Select(token => {
            var known = book.GetAllKnownSpells().Where(a => a.Blueprint.AssetGuid == Id(token)).ToArray();
            return token + "=" + (known.Length == 0 ? "absent" : string.Join(",",known.Select(a =>
                "circle:"+a.SpellLevel+"/temporary:"+a.IsTemporary+"/variants:"+(a.Blueprint.AbilityVariants?.Variants.Length ?? 0))));
        }));

        internal static void Trace(Spellbook book, string operation, BlueprintAbility spell = null, Exception error = null)
        {
            PsychicLearningDiagnostics.Trace(book,operation,spell,error);
            if (book == null || (spell != null ? !IsBurst(spell) : !PsychicCasting.IsPsychic(book.Blueprint))) return;
            try { ModEntry.LogSpellDiagnostic("[Burst learning] " + operation + "; book="+book.Blueprint.AssetGuid+
                "; spell="+spell?.AssetGuid+"; "+KnownState(book)+(error==null?"":"; error="+error)); }
            catch (Exception e) { ModEntry.LogSpellDiagnostic("[Burst learning] diagnostic unavailable: "+e); }
        }
        // CotW adapts the tabletop immediate/check-only spells into swift,
        // one-round enhancement bonuses. Preserve that documented game design.
        internal static IEnumerable<PsychicSpells.Entry> Build(PsychicAssets f)
        {
            var fatigue=f.Native<BlueprintBuff>("e6f2fc5d73d88064583cb828801212f4");
            var dazed=f.Native<BlueprintBuff>("9934fedff1b14994ea90205d189c8759");
            var visual=f.Native<BlueprintBuff>("a1ffec0ce7c167a40aaea13dc49b757b");
            var physical=new[]{StatType.Strength,StatType.Dexterity,StatType.Constitution};
            var mental=new[]{StatType.Intelligence,StatType.Wisdom,StatType.Charisma};
            // Existing native spell icons, matching the selected attribute.
            var icons=new[] {
                f.Native<BlueprintAbility>("4c3d08935262b6544ae97599b3a9556d"),f.Native<BlueprintAbility>("de7a025d48ad5da4991e7d3c682cf69d"),f.Native<BlueprintAbility>("a900628aea19aa74aad0ece0e65d091a"),
                f.Native<BlueprintAbility>("ae4d3ad6a8fda1542acf2e9bbc13d113"),f.Native<BlueprintAbility>("f0455c9295b53904f9e02fc571dd2ce1"),f.Native<BlueprintAbility>("446f7bf201dc1934f96ac0a26e324803") };
            foreach(bool insight in new[]{false,true}) {
                string token=insight?"BurstOfInsight":"BurstOfAdrenaline",title=insight?"Burst of Insight":"Burst of Adrenaline";
                string text=insight?"For 1 round, gain a +8 enhancement bonus to Intelligence, Wisdom, or Charisma (choose when casting). You are dazed for 1 round afterward.":
                    "For 1 round, gain a +8 enhancement bonus to Strength, Dexterity, or Constitution (choose when casting). You are fatigued for 1 round afterward.";
                BlueprintAbility Spell(string key,string name,params BlueprintComponent[] components) {
                    var spell=f.Ability(key,name,text,AbilityType.Spell,UnitCommand.CommandType.Swift,AbilityRange.Personal,
                        components.Concat(new[]{new SpellComponent {name="BurstTransmutation",School=SpellSchool.Transmutation}}).ToArray());
                    spell.AvailableMetamagic=Metamagic.Heighten;spell.SpellResistance=false;
                    spell.LocalizedDuration=Text(key,"duration","1 round; aftereffect 1 round");
                    spell.LocalizedSavingThrow=Text(key,"savingThrow","");return spell;
                }
                var variants=new List<BlueprintAbility>();int i=insight?3:0;
                foreach(var stat in insight?mental:physical) {
                    var after=Apply(insight?dazed:fatigue,1,toCaster:true,fromSpell:false);after.IsNotDispelable=true;
                    var buff=f.Buff(token+stat+"Buff",title+": "+stat,text,
                        new AddStatBonus {name="BurstAttributeBonus",Stat=stat,Value=8,Descriptor=ModifierDescriptor.Enhancement},
                        new AddFactContextActions {name="BurstAftereffect",Deactivated=Actions(after)});
                    buff.FxOnStart=visual.FxOnStart ?? new Kingmaker.ResourceLinks.PrefabLink();
                    var spell=Spell(token+stat,title+": "+stat,Run(Apply(buff,1,toCaster:true)));
                    Set(spell,"m_Icon",icons[i].Icon);Set(buff,"m_Icon",icons[i++].Icon);variants.Add(spell);
                }
                var choices=new AbilityVariants {name="BurstAttributeChoice"};Set(choices,"m_Variants",variants.Select(Ref<BlueprintAbilityReference>).ToArray());
                var parent=Spell(token,title,choices);Set(parent,"m_Icon",icons[insight?3:0].Icon);
                foreach(var spell in variants) Set(spell,"m_Parent",Ref<BlueprintAbilityReference>(parent));
                yield return new PsychicSpells.Entry {Token=token,Level=1,Ability=parent};
            }
        }
    }
}
