// Verbatim assembled original CotW UI strings, source manifest in work/batch01454.
using System;
using System.Collections.Generic;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class SkirnirTexts01454 {
  static readonly Dictionary<string,string[]> Rows=new Dictionary<string,string[]> {
   {"SpellShieldFeature",new[]{"Spellshield","At 7th level, as a standard action, a skirnir may store a magus spell in his shield by spending 1 point from his arcane pool per level of the spell. This functions as the spell storing weapon special ability, but activates only on a successful shield bash by the skirnir and is not limited to spells of 3rd level or less. "}},
   {"GreaterSpellshieldFeature",new[]{"Greater Spellshield","At 16th level, a skirnir may activate a stored spell as a swift action after being struck in combat."}},
   {"GreaterSpellCombatFeature",new[]{"",""}},
   {"SorcerousShieldFeature",new[]{"Sorcerous Shield","At 1st level, skirnirs are proficient with all types of shields, including tower shields, and do not suffer an arcane spell failure chance when casting magus spells while using a shield."}},
   {"SkirnirArcaneShieldEnchancementBaseBuff",new[]{"",""}},
   {"SkirnirArcaneShieldEnhancementSwitchBuff",new[]{"Arcane Shield Enchantment","At 1st level, a skirnir can use his arcane pool to grant an enhancement bonus to a weapon as normal, as well as to his shield, paying the arcane pool cost separately for each. At 5th level and above, he can also add the following shield special abilities: fortification (any), spell resistance (any)."}},
   {"DiminishedSpellcastingSkirnirFeature",new[]{"Diminished Spellcasting","A skirnir casts one fewer spell of each level than normal. If this reduces the number to 0, he may cast spells of that level only if his Intelligence allows bonus spells of that level."}},
  };
  internal static string Name(string key)=>Rows.TryGetValue(key,out var row)?row[0]:throw new InvalidOperationException("Original CotW name missing: "+key);
  internal static string Text(string key)=>Rows.TryGetValue(key,out var row)?row[1]:throw new InvalidOperationException("Original CotW description missing: "+key);
 }
}
