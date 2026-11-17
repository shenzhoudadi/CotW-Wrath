# A · Spiritualist 原作与 Wrath 入口

2026-11-15。COTW-0085 基础职业，认领 [#16](https://github.com/shenzhoudadi/CotW-Wrath/issues/16)。生产基线 `939e04d543875ff28314b808dcc996d16227acc3` / 0.14.74 TEST，认领文档基线 `ca35d784`。本机原作九个文件的 Git blob 均与固定提交一致。蓝图路径、GUID、类型、记录指纹和分环逐条来源见 [A-spiritualist-assets.json](A-spiritualist-assets.json)。[原作固定提交](https://github.com/Holic75/KingmakerRebalance/tree/1332fb0db844b7863f484ca978bea2349fe49769)。

已查主库最新提交、本地已取分支、GitHub 全部开放和关闭 PR、认领及 sxl1216 fork 分支和主线历史。#16 是对应认领。PR #14 的 Phantom diff 只显式将 Medium 字段设为 null。主库 `Modules/MediumClass/Spiritualist/Phantom.cs` 只有未接线的单位构建样稿，`ConfigureEnabled` 内容已注释，`Main.cs` 无调用。可参考 `AddClassLevels`、空手武器和基础属性字段写法。保留 Medium 自己的 GUID 和入口。PR #14 的 Lesser Animate Dead 与共享注册归原贡献者范围。[PR #14](https://github.com/shenzhoudadi/CotW-Wrath/pull/14)。

## 六个情绪

原作 `Classes/Phantoms/Phantom.cs:102–110` 按 Anger、Despair、Hatred、Fear、Zeal、Kindness 接通，Whimsy 只留注释。下列豁免取各情绪实际传入 `createPhantomArchetype` 的 Fortitude、Reflex、Will 参数。两个情绪技能的伙伴等级机制见原作 `SetSkillRankToValue`，主人 Skill Focus 按同一对技能。

- **Anger** — Persuasion、Lore (Nature)，Fortitude/Will，STR 14、DEX 12。Phantom 1/7/12/17 级依次 Powerful Strikes、Aura of Fury、Ferocious Mien、Furious Wail。主人 5/7/9/16 级依次 Enlarge Person、Rage、Greater Longstrider、Shout。一级另有 Power Attack，STR/CHA 代替 DEX/CHA 成长，使用大号 slam 骰。
- **Despair** — Persuasion、Stealth，Fortitude/Will，STR 12、DEX 14。Miserable Strike、Aura of Despair、Despairing Shout、Inescapable Despair，一级另有 Power from Despair。主人 Ill Omen、Stricken Heart、Slow、Crushing Despair。
- **Hatred** — Mobility、Perception，Fortitude/Reflex，STR 12、DEX 14。Hated Target、Hateful Aura、Sneak Attack、Shared Hatred，一级另有 Weapon Finesse。主人 Doom、Inflict Pain、Bestow Curse、Enervation。
- **Fear** — Persuasion、Stealth，Reflex/Will，STR 12、DEX 14。Horrifying Strike、Increase Fear、Frightful Attack、Shelter Allies，一级另有 Stealthy。主人 Bane、Savage Maw、Vampiric Touch、Phantasmal Killer。
- **Zeal** — Mobility、Lore (Nature)，Fortitude/Reflex，STR 12、DEX 14。Ruthless Combatant、Determination Aura、Haste、Zeal's Resolve，一级另有 Tracking。主人 Bless、Force Sword、Divine Favor、Freedom of Movement。原作描述写 Fortitude/Will，与代码不符。
- **Kindness** — Persuasion、Lore (Religion)，Fortitude/Will，STR 12、DEX 14。Opening Strike、Etheric Healing、Expanded Aid、Exceptional Aid，一级另有 Benevolent。主人 Bless、Aid、Heroism、Good Hope。

共用 `CON 13/INT 7/WIS 10/CHA 13`、30 ft、d10、全 BAB、对齐主人阵营、两个 slam。砸击以 Phantom 职业等级 1/4/7/10/13 为五段，标准 1d6/1d8/1d10/2d6/2d8，Anger 1d8/2d6/2d8/3d6/3d8。一级基础 natural armor +2 和 DR 5/slashing，3 级魔法武器，4/8/12 级 DR 5/10/15/magic，5 级 Devotion，7 级 Multiattack，2/4/6/8/9/12/13/15 级自然护甲和对应属性成长。原作 15 级 DR 文案是 15/–，组件实设 5/slashing。主人每种 Emotional Power 自获时起每天 1 次，每再升四级加 1 次，DC = 10 + ⌊Spiritualist 等级/2⌋ + WIS 修正。原作二次情绪的 capstone 只授同四个能力 wrapper，一级情绪 archetype 独有能力、技能和豁免由一级选择继续提供。

## 主人进度与私人法表

- Spiritualist d8、Inquisitor 同 BAB/豁免和职业技能点减一。职业技能 Persuasion、Knowledge (Arcana/World)、Lore (Nature/Religion)、Perception、Use Magic Device。简易武器、kukri、scythe、轻甲。Inquisitor 仅供数据，避免继承 DeityDependencyClass 等不适用组件。
- 1 级法术、Knacks、Detect Magic、Phantom 情绪、Etheric Tether、Shared Consciousness、主人阵营同步及 touch/ray 辅助。3 Bonded Manifestation，4 Spiritual Inference，6 Phantom Recall，10 Fused Consciousness，12 Greater Spiritual Inference，14 Spiritual Bond，17 Dual Bond，20 Potent Phantom。其余等级照表推进法术及情绪进度。
- 原作 `createSpiritualistSpellbook` 有 137 行，0–6 环分别 6/17/24/28/22/24/16。取 Wisdom 自发、施法等级全进、非奥术/神术、Inquisitor 已知/日用表。原生 Inquisitor Spellbook `57fab75111f377248810ece84193a5a5` 引用已知表 `c133d22305bab964c88a767cc69b1f9b`、日用表 `83d3e15962e5d6949b90b5c226a2b487`。本职保留源环位，使用 `PsychicSpellbookMarker` 及 `SpellListCompatibility` 的 0–10 数组契约，7–10 环为空。读取共同法术根，私人加入列表，不覆写共同根、既有 Psychic/Magus/Medium 法表或把别职环位搬来。
- 137 行中 100 行原生已定位，24 行指向本仓库已有根，11 行是现有提供方候选，Lesser Animate Dead 有原生候选但需与 PR #14 的变动核对，Ghoul Touch 待找等价根。24 个情绪 SLA 根中 20 原生、2 本仓库已有、2 外部候选（Despair 的 Stricken Heart，Hatred 的 Inflict Pain）。Ill Omen 可用原生 `ca577309cedc4f1daf6fe5795fb2619b`。已定位表示此来源可查，具体运行、DLC 与提供方可用性留 B 验证。

## Wrath 原生入口

以本机 2.7.0x 的 `Assembly-CSharp.dll` 与 `blueprints.zip` 为证据。原作复制的 Smilodon `126712ef923ab204983d6f107629c895` 在 Wrath 为带 `AddPet`、`PrerequisitePet` 的动物选项。独立对照 Dog `f894e003d31461f48a02f5caec4e3359` → `AddPet` → Dog 单位 `918939943bf32ba4a95470ea696c2ba5`。动物类型、原生 AnimalCompanionRank、7 级体型升级均属于供体。原作 Animal Domain 进度 `125af359f8bc9a145968b5d8fd8159b8` 在 Wrath 绑定三条带 −3 偏移的现有职业线。Phantom 的伙伴授予与等级计算据 C 合同单独建立。

- 建立与升级。 `AddPet.OnActivate/OnAreaDidLoad/IUpdatePet.TryUpdatePet → TryUpdatePet → EntityCreator.SpawnUnit → SetMaster → UnitPartPetMaster.AddPet → UnitPartPet.Initialize`。`AddPetData.SpawnedPetRef`、主人 Pet 列表和子单位 Master/Type 都由原生维护。`AddPet.GetPetLevel` 的 context 值分支直取自定义源；常规分支走宠物表。`TryLevelUpPet` 只补正的等级差，调用单位 `AddClassLevels`，故降级要走重训提交。个别主人蓝图存在 `AddClassLevelsToPets` 覆盖点，B 切片需验证不会把普通宠物进度套到 Phantom。
- 出入场。 `UnitPartPet.SetHidden` 同步 `ShouldBeHidden` 与在场状态。`m_ShouldBeHidden` 无 `JsonProperty`，需要从持久收回状态复投影；原生挂接也有写 `IsInGame` 的路径。 `TeleportToMaster` 使用 `FreePlaceSelector` 和地面投影。Dimension Door `5bdc37e4acfa209408334326076a43bc` 可参考视觉资源，默认行为含施法者/其他单位，按所有权显式调用单只 Phantom 移动。
- 死亡与恢复。 `AddResurrectOnRest` 挂在原生 Smilodon 单位上，`RestController.ResurrectDeadUnits` 对休息组中具该特性的最终死亡单位走 `Resurrect`、恢复生命状态并处理地图定位。原生普通死亡/最终死亡和 Death's Door 受难度影响。休息组来自 `CampingState.GetPartyUnitsForRest(removePets: false, includeDeadAndParalyzed: true)`。自定义 PetType 是否贯通队列及不同区域仍需验证。原作收回利用临时死亡与 HP 快照，Wrath 已有隐藏入口。
- 移除与重训。 `AddPet.OnDeactivate` 在重应用期间跳过清退。真实撤掉时归还可移除装备、`RemoveMaster`、按 `m_DestroyPetOnDeactivate` 销毁。`RemoveMaster` 调 `UnitPartPetMaster.RemovePet`，清双向关系及角色列表缓存。重训入口 `UnitHelper.RespecOnCommit` 存在 `IUpdatePet.TryUpdatePet` 更新调用；新自定义类型在取消、改情绪、同情绪重训中的实际事件顺序由 B 切片／玩家验收覆盖。
- 视觉。原作以 `CR7_SpectreStandard`（`2f91d7337b60e3b4b9b137198a8c8745`）为单位底子，并复制 Smilodon 的 Visual 元数据、改 ghost 脚步/血效；GhostFX `20f79fea035330b479fc899fa201d232`、Slam1d6 `767e6932882a99c4b8ca95c88d823137` 均存在。Spectre 自带敌方类型、经验、8 级 NPC 职业和本体能力，拆除玩法成分后方可作为私有外观供体。Prefab、动画、手部/身体装备槽、碰撞、动作条和跨区载入只能通过场景确认。

`src/CompanionSpellSharing.cs` 只认 AnimalCompanion、NightHag、MythicSkeletalChampion、AzataHavocDragon，并核对 `UnitPartPet.Master`。沿用这种所有权核对思路。其白名单、Making Friends Eidolon 及 Medium 不需要因本项改动。
