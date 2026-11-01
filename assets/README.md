# Original CotW icons

No generated artwork. HaltUndead, ForceSword, TemporalStasis and Silence are original PNG bytes from Holic75/KingmakerRebalance commit 1332fb0db844b7863f484ca978bea2349fe49769, CallOfTheWild/Icons/AbilityIcons.

DivideMind, Invigorate, HowlingAgony, LocateWeakness and ControlUndead are decoded sprites from the locally installed Kingmaker assets, selected using the exact native blueprint GUIDs referenced by the same CotW source. See 0.13.0-icon-provenance.json for source asset hash, blueprint names and sprite identities. They are embedded in the assembly; no runtime Kingmaker dependency. Existing spells with native Wrath donor icons retain their original CotW donor references.

0.13.1: WardingWeapon.png is copied byte-for-byte from the same CotW commit, Icons/AbilityIcons/WardingWeapon.png. Daze, Mass uses CotW original Daze blueprint GUID 55f14bc84d7c85446b07a1b5dd6b2b4c, available in Wrath; no replacement/generated icon. See 0.13.1-icon-provenance.json.

0.13.2: SavageMaw.png and BloodArmor.png are original bytes from the same CotW commit, Icons/AbilityIcons. Embedded directly, no runtime dependency on Kingmaker. See 0.13.2-icon-provenance.json.

0.13.3: Metamixing.png is the original CotW Globe icon, unchanged bytes from commit1332fb0db844b7863f484ca978bea2349fe49769, Icons/AbilityIcons. Embedded with no Kingmaker runtime dependency. See 0.13.3-icon-provenance.json.

0.13.4: IrresistibleDance.png is original CotW PNG, unchanged bytes, embedded as CotW.IrresistibleDance.png. Debilitating Portent uses the same native Fatigued icon identity as original CotW. See 0.13.4-icon-provenance.json.

GhostbaneDirge.png: original CotW Icons/AbilityIcons/GhostbaneDirge.png, pinned commit 1332fb0db844b7863f484ca978bea2349fe49769; embedded unchanged in 0.13.9.

0.14.14: BondedMind.png, ShareSpells.png, BrokenWingGambit.png, WoundedPawGambit.png and StunningAssault.png are copied byte-for-byte from the same CotW commit's Icons/FeatIcons directory (original names Icon_Bonded_Mind.png, Icon_Share_Spells.png, BrokenWingGambit.png, WoundedPawGambit.png and StunningAssault.png). Each feat and its associated abilities/buffs use its original icon, including hidden legacy Share Spells references. The PNGs are embedded as CotW.<name>.png and use the existing cached loader; no external image files or Kingmaker installation are needed at runtime. Companion Share Spells follows CotW's native Magic Fang icon donor, blueprint 403cf599412299a4f9d5d925c7b9fb33.
