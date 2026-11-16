# COTW-0099 B9 / D9 Aegis

Aegis 作为 Abjuration 的一级 Focus Power 进入原生选择。标准动作消耗 1 点，迅捷动作消耗 2 点。只处理穿在护甲槽里的护甲，持续 1 分钟。增强预算在职业 1、6、12、18 级分别为 1、2、3、4，20 级 Abjuration Implement Mastery 提升到 5。施放时锁定所选属性，失败时保留原有护甲、锁定计数和专注余额。

- **Wrath 复用。** 直接引用 5 个数值增强、3 个 Fortification 和 12 个元素抗力附魔。沿用原生护甲槽锁、附魔授予事实、法术转换入口、准备位与自发位消耗，以及近战命中触发。原生 `BuffEnchantArmor` 会把效果迁移到替换护甲，Aegis 需要按原护甲实体保留记录并一起提交多项附魔。Magic Deceiver 已有的 Fake/True Enhancement 参与数值计算。没有能满足预算和 +5 上限的原生档位时，保留当前数值。 [Aegis 实现](../../../src/Occultist0099Aegis.cs)
- **CotW 复用。** 沿用 Magus 的原生附魔挂载、Skirnir 的法术变体筛选和储法事实绑定，以及 Psychic 的参数快照投递。Aegis 负责本职专注支付、配置快照和护甲实体身份。 [储法实现](../../../src/Occultist0099AegisStorage.cs)
- **跨项目复查。** 王国版 CotW 已有护甲 Spell Storing，其代码使用 Kingmaker 的事实与法术槽接口。Wrath 档案和本地已审阅的其他 mod 源码快照里，未找到可直接引用的 Aegis 或护甲 Spell Storing 供体。 [原作护甲附魔](https://github.com/Holic75/KingmakerRebalance/blob/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/Enchantments/ArmorEnchantments.cs)

隔离检查覆盖缓冲效果被原生规则拒绝、附魔写入过程中出错、原有属性与锁的隔离、实际职业等级和 Mastery、原生 Magic Deceiver 状态、准备与自发来源分别花费、相同执行重复进入、到期后过期指令、同蓝图不同护甲、同一护甲恢复释放，以及给定近战命中后的单次触摸投递。 [机器可读证据](B9-D9-evidence.json)

D9 再次复查通过。最终编译 0 警告、0 错误，2.086 秒。托管原生定向 359 条断言、0 失败、0 意外宿主报错，4.170 秒。隔离登记 154 个私有身份。储法使用原生法术转换提供的 1 至 3 环法术位。

隔离宿主使用原生护甲实体、法术与 Buff 规则，同时为无 Unity 环境提供装备摆放、效果派发与显示边界。真实装备操作、AC 更新、动作栏 UI、命中掷骰、命令清理、磁盘存读档、提供方装载、游戏/UMM 和出包未运行。切片仍未发布。 [认领 #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15)
