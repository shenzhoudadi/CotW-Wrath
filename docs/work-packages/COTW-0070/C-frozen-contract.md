# C-0070-20261112-r1 Cryptid Scholar 行为合同

基线 0.14.72 TEST / `1c0f3b0`。`L` 是真实持有本变体的 HA Investigator 等级。行为以原作与下列 Wrath 适配裁决为准，公开身份和原生入口见 A 证据。

## Q01 提供方及发布

依赖已就绪的真实 HA Investigator、进展、旧书、天赋、毒抗／免、Studied Combat／Strike、InspirationFeature 及资源。核对身份、类型、实际引用、授予等级及资源来源，采用 `A-evidence.json` 中固定蓝图。必需根未到位时报告缺项并延后，`RegisterOptional` 可在本变体根尚不存在时完成首次发布；已有根走幂等核对。使用现有原子发布工具，冲突或异常回滚本次自有资产与追加引用。旧书、正常职业和其他变体的原生事实各保留所属来源。

新根 token 用 `CryptidScholarArchetype0070`，独立 GUID/TypeId 与真实来源。仅给 HA Investigator 接变体和其私有 UI 分组。生产只追加本项必要引用。当前 HA 未加载时，其他模块仍可工作。

## Q02 等级和 Intuitive Lore

1 级授 Intuitive Lore。4 级授 Opportune Advice 与 Knowledgeable Strike。移除 2/5/8 的毒抗、11 的毒免、4 级的 Studied Strike 和 4 到 20 的每个偶数级 Studied Combat 授予。保留 HA 法书、InspirationFeature、全部原本的奇数级天赋和未替换进展。

Intuitive Lore 对 Lore Nature、Lore Religion 加 `max(0, IntMod)`，对 Knowledge Arcana、Knowledge World 加 `max(0, WisMod)`，使用原作的 UntypedStackable 与属性变动重算。原生基础技能与其他来源正常结算，撤销本事实只移除此贡献。

## Q03 识怪与精确分类

Advice 在一个 Move 内选原生可识别的敌对活单位。盟友 30 ft 是广播范围。用 native `InspectUnitsManager.TryMakeKnowledgeCheck(target, caster)` 取得学者本人的结果，技能、训练、CR DC 和图鉴表现由原生决定。成功前只有 Move 的原生耗时，无 Advice、冷却或灵感扣费。已经有本人该分类的成功凭据时可复用，已揭示的图鉴、队友成功、预览及目标名字均不能制造凭据。失败可在新的有效 Move 重试。

分类键由实际生效的广义类型及该单位具备的全部已验证亚型／种族事实组成，按 GUID 排序去重。支持原作 14 组。广义类型用 A 的 `broadTypeFacts`。巨人、哥布林、爬行类是本包的三个类人入口，其他已核对的种族事实可区分有资格的目标，普通人类、精灵等不因此新增为可选组。外界生物使用真实 `OutsiderType` 并附 A 所列当前生效的原生分组事实。使用本机可验证的类型数据，不从名称、阵营或图鉴物种名猜亚型。只有 `InstantEnemyBuff`、`PlantTypeFake` 或其它未验证事实时不得充当分类凭据。无法解析类型时给出明确缺项，不使用匹配所有单位的默认值。运行时命中条件为广义类型和已证实的亚型集合均相同。

同一学者的识怪凭据按分类持久化，只有真成功才记录。队友或第二名学者拥有独立凭据。对已变化的实际类型重新评估，旧凭据只能匹配原类型键。支持范围是 A 已核实的 Wrath 类型数据，实际未表示的桌游亚型记录为适配边界。

## Q04 Opportune Advice 发出与资源

4 级开始，Move、Extraordinary。成功发给以施放者为中心 30 ft 内的活、清醒、可听盟友，包含满足条件的自己。沿用现有 `AudibleAbilityRestriction` 的静默识别和原生单位状态；禁声的学者不能发出，听不到的单位不接收。距离用原生距离口径；30 ft 边界包含。检查失败或没有可接收目标时，不改已有 Advice、资源或冷却。

本次加值为 `min(5, floor(L/4))`，4/8/12/16/20 级为 +1/+2/+3/+4/+5。持续 `max(1, IntMod)` 轮，发出时确定数值与持续。一个施放者的一次 Advice 是可追溯的单个批次。成功发出新批次时清退自己的上次 Advice。每个接收者同时只持有一个本机制 Advice，收到有效新 Advice 后替换旧批次。第二名学者的失败、离开或清退不能删除他人仍生效的来源。

同一施放者、同一精确分类从每次成功发出开始计 24 小时游戏时间。到期时免费；未到期的付费分支仍是同次 Move，恰扣 1 点真实 HA Inspiration。付费成功重新计该分类 24 小时，其他类别不变。取消、失效、资源不足、预览或重复事件不扣；失败回滚仅归还本次已实际扣除的那一点。用 native Resources／AbilityResourceLogic 的费用与收据，勿创建新的灵感池或免费重置按钮。换图、战斗开始／结束不直接重置计时，休息只按实际经过时间与原生资源恢复结算。

## Q05 Advice 的实际防御加值

在 `RuleCalculateAC` 中，仅对匹配分类的攻击者给予本次 Insight AC。豁免仅在实际来源为该类敌人使用的 Extraordinary、Supernatural 或 SpellLike 能力时给予同值 Insight；普通 Spell、物品、未知来源和其它分类无加值。匹配实际事件来源，避免通过召唤者、目标或 UI 描述伪造来源。豁免替换仍由原生比较，适用加值在本次真实候选豁免上参与结算并随事件清理。保留原生同类型加值规则，确认其他 AC 或豁免不会被永久改写。

## Q06 Knowledgeable Strike

4 级开始，Extraordinary Move。把自己当前 Advice 批次转换成攻击机会。先快照本次同源 Advice 的持有者，至少有一位仍有效且在 30 ft、可听才接受。接受后结束该批次的自身 Advice，仍在范围且可听的同源持有者得到 1 轮 Strike。其它学者的 Advice 保持所属来源；未持有本次 Advice 的单位不会获得 Strike。每个受益者同一时间最多一个本机制 Strike，新有效授予替换旧机会。

受益者的下一次合格武器、徒手或天生武器命中匹配的类别时，增加 `min(5, floor(L/4))d6` 精准伤害，使用原 Advice 源的计级。同一真实攻击及多个伤害包只消费一次。远程要求命中结算时目标距攻击者 ≤30 ft，近战用原生可及性。采用实际 AttackRoll，未命中、其它分类、纯法术射线、非武器伤害、隐蔽或无法看见要害、精准免疫，以及 `DisablePrecisionDamage` 都不触发或消耗。已通过触发门槛的那次精准伤害保留原生 Fortification 与伤害免疫处理。独立精准伤害包标记 Precision，临界倍率 1，不伪造 SneakAttack 事件或全局清除其它伤害。不能通过副弹、持续伤害、假命中或重复回调多领。

## Q07 天赋来源

按 `A-evidence.json` 的身份，仅在本变体的实际 Investigator Talent 及其嵌套／Extra Investigator Talent 入口排除原作九类研究天赋。Ranged Study 的选择与派生引用按真实入口覆盖。合法的独立职业来源照原生前置结算；普通 HA、Psychic Detective 和独立专长无需全局禁止。复用 0068 的 `FromInvestigatorTalents` 或抽取小型同义方法，沿实际 `CanSelect` 与提交路径。若 HA 的 Slowing Strike、Repositioning Strike 等附加选项有真实专用 Studied Strike 执行链，只在证明其从已替换来源授权时扩展排除；身份与证据随 B 交 D。没有证明时维持原作名单。已有独立事实不被清空。

## Q08 生命周期、验收与交付

源、分类、24 小时截止、批次和一次性机会使用可恢复的本项数据与原生 Buff/context，按实际 owner/fact 生命周期清退。休息遵循原生资源和时间；失去变体、重训或失去来源清掉本包自身授权及其批次，不清别的学者或普通 HA 的事实。读档恢复剩余时间，重复加载和重复事件不补资源、不重授机会。队友退队、失效或已到期的数据用本批次来源安全收束。

实施后只做本项编译与必要定向检查。保留原生动作入口、资源账目、真实规则事件和负例；结构断言单独描述。真实 HA 初始化、完整存档／重训、战斗场景或玩家实机如果未运行，明确记录。本轮只冻结预期，实际结果交 B 与独立 D。按用户授权统一 GitHub TEST 交付，包内中文 README 保留“猫猫堡是猪猪”。
