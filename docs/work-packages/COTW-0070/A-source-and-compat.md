# A 原作与 Wrath 接口核对

已查看上游 main、公开分支、开关 PR、施工状态及公开 fork 差异。未见 Cryptid Scholar 重复实现或认领。HA 公开完整树也无 Cryptid 或 Schoolar 条目。#6 Speaker 及 fork 的 Lore Spirit 等范围继续由原负责人处理。[上游施工状态](https://github.com/shenzhoudadi/CotW-Wrath/blob/main/WORK_STATUS.md)

## 原作的明确范围和差异

固定 `Holic75/KingmakerRebalance@1332fb0db844b7863f484ca978bea2349fe49769` 的 `Classes/Investigator.cs`，本机 blob 与 GitHub 相符。原作方法 `createCryptidSchoolar`、`createIntuitiveMonsterLore`、`createOpportuneAdviceAndKnowledgeableStrike` 分别定义职业进展、四技能和两项团队能力。1 级 Intuitive Lore，4 级 Opportune Advice 和 Knowledgeable Strike。原作移除毒抗、毒免、Studied Combat／Strike，并给九类研究系天赋加排除。[固定原作](https://github.com/Holic75/KingmakerRebalance/blob/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/Classes/Investigator.cs#L333)

Intuitive Lore 对 Lore Nature／Religion 补非负 Int 修正，对 Knowledge Arcana／World 补非负 Wis 修正。原作两个团队能力都是 Move，授予范围 30 ft。Advice 按每 4 级 +1 洞察、每 Int 修正 1 轮，Strike 按每 4 级 1d6 精准，持续 1 轮。原作实现用 14 个可直接选的类别按钮，只取天敌首个 fact；没有学者自身识怪成功检查。其 24 小时 Buff 实际还会在战斗开始、结束和休息时清退，付费按钮消耗 1 灵感并免费重置全部类别。合同在这三点采用原作能力说明的行为，由 Wrath 原生检定、精确分类和同次 Move 扣费实现。

## HA 真实来源

`FrozenDervish/Homebrew-Archetypes@211e9b82e252705dfaa7e4aa7507d9819737e232` 的 31 个定向 `.jbp` blob 已核对。核心职业、进展、法书、天赋及毒抗毒免可沿用 0068 已验证的身份。HA 的 `InvestigatorStudiedCombatFeature` 为 `5a10de57f3d1a584c917f830c32742c9`，4、6、8、10、12、14、16、18、20 级各出现一次。`StudiedStrikeFeature` 为 `0d545d60ed6aa1f48acd73c78676b94d`，4 级出现。毒抗在 2、5、8 级，毒免在 11 级。原作只写 4 级 Studied Combat 移除，直接移植会在 HA 6 级重新授予。普通天赋是 3 到 19 级每个奇数级，全部保留。[HA 进展蓝图](https://github.com/FrozenDervish/Homebrew-Archetypes/blob/211e9b82e252705dfaa7e4aa7507d9819737e232/Blueprints/Investigator/InvestigatorProgression.jbp)

`InspirationFeature` 的 `AddAbilityResources` 授真实 HA `InspirationResource`，GUID `60b4629bd1e92194382ae6a754f738ac`。`InspirationStudiedCombat` 是原生 Move 能力，`AbilityResourceLogic` 的 `m_RequiredResource` 指向该池，`m_IsSpendResource=true`、固定 Amount=1。本包沿用 HA 池及其原生恢复。普通 Studied Combat 走独立 variant、mark 与 cooldown，真实引用见证据；新能力的标记和计时由本变体自己持有。[HA 付费研究蓝图](https://github.com/FrozenDervish/Homebrew-Archetypes/blob/211e9b82e252705dfaa7e4aa7507d9819737e232/Blueprints/Investigator/InspirationStudiedCombat.jbp)

原作九类天赋在 HA 的身份列于 `A-evidence.json`，Ranged Study 有选择根和派生标记两个入口。HA 还存在 Slowing Strike 与 Repositioning Strike 根；现有定向静态证据证明授予了 toggle，尚未证明其最终执行是否依赖 Studied Strike。合同保留按真实可执行来源核对的边界。禁止只凭显示名扩展排除项。

## Wrath 可直接用的接口

- `InspectUnitsManager.TryMakeKnowledgeCheck(unit, inspector, overrideStat)` 返回指定 inspector 自己的真实 `RuleSkillCheck.Success`，即使图鉴已经完整也可检定。用 `BlueprintForInspection.Type.KnowledgeStat`（缺 type 走原生 Nature）、原生训练要求及原生 `10+floor(1.25×CR)`。单参数版本在玩家队伍中循环，既有图鉴/集体成功都不足以证明学者本人成功。复用原生接口，不照搬 0060/0117 的猜名选技能或单独 DC。
- 14 个原作天敌供体均在 Wrath 归档找到。`FavoredEnemy.CheckedFacts` 的实际命中是任一事实，且大部分列表含 `InstantEnemyBuff`，故它可作为图标和身份证据。实际分类用已核对的类型、亚型和种族事实的组合。排除 `InstantEnemyBuff` 与 `PlantTypeFake`。Wrath 的 `BlueprintUnit.Type` 有 Wolf、Dretch 等图鉴类名，不能作为等同桌游广义类别的唯一键。
- `AbilityResourceLogic` 走单位的 native Resources。`RuleCalculateAC` 有实际攻击者、受益目标和本次 AC 修正。`RuleSavingThrow.OnTrigger` 根据实际事件决定使用的豁免 stat，并可能应用替换。`RulePrepareDamage.ParentRule.AttackRoll` 和 `.Add` 可把精准伤害接到真实命中，`RuleCalculateDamage` 会处理 `DisablePrecisionDamage`。事件上查原生隐蔽等级、精准免疫与武器证据，不能因命中骰通过就跳过隐蔽门槛。
- 复用 `PsychicDetectiveTalent0068Patch.FromInvestigatorTalents` 的实际来源祖先链、`ExtraInvestigatorTalent` 的兼容身份、`MagusBatch01454` 的发布事务以及 `AudibleAbilityRestriction` 的现有静默识别。采用项目现有 `TimeController.GameTime` 和原生 Buff/context 来源约定。Wrath `UnitCondition` 没有通用 Deafened，听觉用已有可核验的禁声与意识状态。

原件在本机 `C:/Users/xuenc/agent-tools/cryptid-scholar-next`，包括固定 provider 文件与原生反编译。公开交接仅提交身份、摘要和指纹。真实 HA 初始化、技能和资源实付、战斗事件、存读档和玩家实机均未运行。
