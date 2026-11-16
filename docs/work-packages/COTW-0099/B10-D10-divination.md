# COTW-0099 B10 / D10 预言系

预言系新增八个分环候选根。一级法器授予 Sudden Insight 和 Third Eye。可选能力为 Diviner’s Fortune、Danger Sight、Mind Eye 和 In Accordance with the Prophecy，分别在实际职业 1、3、5、9 级开放。

- 沿用 Wrath 的 Diviner’s Fortune 触摸及加值组件、Guidance 掷骰触发、原生持握触摸、感知和凝视免疫。Diviner’s Fortune 在开始施法时支付焦点，后续交付沿用同一施法参数。离开战斗清理该职业持握动作时无需退还法术位。
- Mind Eye 直接沿用 CotW 的 Arcane Eye 生成动作、实体和控制器。Locate Weakness 和 Battlemind Link 同样使用原有 CotW 根。
- In Accordance with the Prophecy 沿用 CotW 巫师预言分支的合格法术判断、失败率和 Empower 保留机制。Occultist 在原生施法确认后用私有账本扣 1 点，且只对本职业法书生效。法书沿用现有 Psychic 标记并显式关闭原版默认的奥术属性。

D 发现撤销精通时 Third Eye 快照未更新、Diviner’s Fortune 缺少持握入口以及 Third Eye 主察觉加值未跨死亡保留。修复沿用已有重训入口、原生持握和 `StayOnDeath`。再次复查发现退战仍需执行原版目标清理，最终补丁只跳过私有交付的法术位退款。D10 定向复查通过。 [生产代码](../../../src/Occultist0099Divination.cs) · [机器可读证据](B10-D10-evidence.json)

最终编译 0 警告、0 错误，1.948 秒。隔离原生定向 393 条、0 失败、0 宿主异常，4.411 秒。验证覆盖本校候选与学习、锁定和重训快照、给定事件下的掷骰触发、持握、退战清除目标、混职法书隔离，以及预言成功和失败分支。Mind Eye 实体生成与场景操控、实际目标选择界面、完整触摸命令与掷骰、存读档、游戏/UMM 和出包未运行。 [认领 #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15)
