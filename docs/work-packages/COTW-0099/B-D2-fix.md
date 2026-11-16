# COTW-0099 · D2 修复与复用复查

2026-11-16，Abjuration 开发切片，合同 `C-0099-20261115-r1`。本轮处理候选 `77ea03b` 的 D2-01～06。`occultist_d` 独立复核修复源码和 119 条宿主断言，指出普通启动仍会发现未发布补丁。补上启动门禁后，B 定向复跑 120 条断言，全部通过。 [原独立 D 报告](D2-occultist-review.md) · [复跑证据](B-D2-fix-evidence.json)

## 修复与复验

- **D2-01 / D2-05。** 通过原生 `UnitUseAbility` 与 `RuleCastSpell` 区分施放身份，在 `RuleApplySpell.Before`、原生目标检查和 UMD 通过后、失败掷骰及效果派发前提交 Mental Focus。两条命令共用授予能力对象时各付一次，同一命令重入无法重复派发。预查后移去学派时派发计数不增长，合法施放掷骰失败照常付款。原生末尾资源扣减不再重复扣这笔焦点。
- **D2-02。** 当天账本与已付身份存于角色 `UnitPart`，原生事实拆下期间支付拒绝，重附后继续使用同一账本。旧开发事实数据在需要时迁入该部件。重训控制器与存读档未运行。
- **D2-03 / D2-04。** 外书继续执行原生 `SelectSpell.Check` 和 `Apply`。私有书额外检查目录、实际环位、实时智力和该选择槽对应的法器配额，通过后使用原生 `AddKnown` 和 `SpendSlot`。表外根直写、错误环位 Apply、借用其他法器配额均被拒，错误选择不扣槽。原生 `UnitHelper.CopySpellbook` 复制原书已知法术，私有账本独立复制其来源。宿主构造带明确索引的选择槽，真实升级产生的每环增量要由后续职业接线映射到稳定法器槽键，升级重进与撤回尚未验证。
- **D2-06。** 以原生 `RestController.ApplyRest` 批次为身份，每个成功完成的角色休息至多刷新一次。两次营地计数相同的成功批次仍分别刷新，事实暂离期间成功休息后重附也能刷新。宿主以替代的 HealAndApplyRest 边界调真实角色 ApplyRest，完整即时脚本场景和原生 Replenish 未运行。

## 复用核对

- **Wrath 原生。** 13 个防护系根保留原 GUID 和效果。沿用原生施放规则、选择检查和原生书复制及休息入口。0099 提供私有资源支付、周期、配额来源与隔离校验。私有书直接引用供体根，已移除追加到供体蓝图上的学习门禁。通用点数镜像仅展示总余额，学派能力可用性走私有余额校验。
- **CotW。** Warding Weapon、Allied Cloak、Lesser Globe of Invulnerability、Globe of Invulnerability 通过已有 `PsychicWardingSpells`、`PsychicSupportSpells`、`PsychicGlobeSpells` 构建器提供。宿主使用这些真实构建器，没有新增法术效果或法术根副本。另有固定 6 条 0 环 Knacks，独立于 18 条防护系根。
- **TabletopTweaks-Base。** Cloak of Winds 沿用 `PsychicSharedSpellCatalog` 的已有供体 GUID，隔离宿主暂置该根。生产发布需复用 `PsychicSpellIntegration.Available`，核对供体的可执行效果，以及供体与启用法表的双向引用。实际提供方未加载，SP08 缺供体或冲突的原子注册未执行。
- **普通启动。** 沿用现有补丁发现逻辑，`SlicePublished=false` 时排除 0099 补丁。宿主验证普通发现路径仍提供其他补丁，再显式安装 0099 补丁进行定向检查。共享职业、成长和法书登记未接入。

## 验证范围

- 主工程与原生宿主编译 **1.80 秒**（MSBuild 计时），0 警告 / 0 错误。最终隔离原生复跑 **120/120，3.198 秒**，宿主异常和档案组件跳过均为 0。模型定向 **364/364，2.241 秒**，模型宿主不加载游戏程序集。受影响的普通启动检查在最终原生复跑中核验。
- 全命令 `OnAction` 场景、真实效果投递、StickyTouch、焦点预览与重训、升级 UI、游戏／UMM、存读档、真实外部提供方、真实原子注册、打包和安装包上传未运行。切片仍处于开发态，`SlicePublished=false`、`releaseEligible=false`。 [issue #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15)
