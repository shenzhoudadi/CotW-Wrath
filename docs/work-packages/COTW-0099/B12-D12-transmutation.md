# COTW-0099 B12 / D12 变化系

变化系接入隔离职业。Physical Enhancement 在锁定时冻结所选属性和投资，Legacy Weapon 在确认装备效果生效后扣 Focus。五个可选能力是 Sudden Speed、Size Alteration、Philosopher’s Touch、Quickness 和 Mind Over Gravity。

- Sudden Speed、Size Alteration 和 Quickness 沿用 Wrath 的移动、体型和加速机制。Mind Over Gravity 用 CotW 的 Overland Flight 机制。Quickness 额外强化攻击、AC 和反射，额外攻击次数沿用原生 Haste。
- Legacy Weapon 借用 Aegis 的暂存、支付和精确附魔转移。Legacy Weapon 的特殊附魔以及 Philosopher’s Touch 的三种材质来自 Wrath。Cruel、Vorpal 等属性执行 Wrath 原生版本的触发条件。
- 法表共 27 条，14 条 Wrath，9 条由 CotW 已有构建器生成，4 条由可选外部提供方发布。Keen Edge 保留原生 DLC 门槛。定向宿主验证了门槛打开和关闭。

D12 查到 Quickness 缺少被原生 Slow 抵消的入口，以及 Legacy Weapon 漏听原生空手武器换实体。已分别接入原生施加状态规则和空手装备变更事件。复验也覆盖同 GUID 外来数值附魔共存、排除自身旧附魔作为基线、变化系移除时的精确清理，以及共用入口下 Aegis 的回归检查。

最终编译 0 警告、0 错误，1.239 秒。隔离原生 474/474，4.97 秒，组件跳过和宿主异常均为 0。状态模型 344/344，2.137 秒。D12 独立复查通过本轮定向范围，查阅源码和运行结果。 [机器可读证据](B12-D12-evidence.json)

Size Alteration 检查了原生效果图。Philosopher’s Touch 检查了提供的目标和爆发范围定义。实际场景内的目标选择、游戏和 UMM、升级及重训界面、存读档、真实外部提供方、全历史检查、打包和上传均未运行。当前仍是隔离开发登记，`SlicePublished=false`。 [issue #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15)
