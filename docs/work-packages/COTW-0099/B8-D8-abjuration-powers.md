# COTW-0099 B8 / D8 三项防护系能力

Energy Shield、Unraveling 和 Globe of Negation 已接入私有职业的原生 Focus Power 选择。分别在实际职业 3、5、11 级开放，支付 1、1、3 点 Abjuration focus。

- Energy Shield 复用 Wrath 的抗力组件、伤害规则和原生耗尽移除。只建一个伤害池，因此同一伤害包内的酸和冷共享 `5 × 有效等级` 的上限。原生抗力优先级与压制仍然适用。元素范围按已冻结的 A 清单取酸、冷、电、火。王国原作的执行图还列了 Sonic，本轮定向验证了该类伤害照常通过。
- Unraveling 复制 Wrath 的 `DispelMagicTarget` 动作图，沿用原生一条效果、施法者等级检定和驱散顺序。独立触摸能力保留对自身与友方的目标许可。D 给出的自我驱散反例已通过实际原生选择与施法入口。
- Globe of Negation 沿用 CotW 静态 Globe 的入射和区域登记补丁。静态保护先判定，已有保护挡下时不消耗新池。移动球按原作执行图拦截正环位的法术，从池中按环位扣减。D 复现了同一施法从直接和区域两处重复扣减的问题。修复后两处共享同一条施法记录，最后一发的效果链完整拦截。终结发生时的区域登记同样拦截，之后的区域检测恢复原生处理。

最终编译 0 警告、0 错误，用时 1.943 秒。托管原生定向 319 条断言、0 失败、0 宿主异常，用时 3.978 秒，隔离登记 110 个私有身份。检查覆盖实际伤害管线、给定掷骰下的原生驱散、原有入射和区域抗力入口。实际场景区域与游戏表现未运行。独立 D8 复查通过。 [机器可读证据](B8-D8-evidence.json)

Aegis 仍有单独实现工作，D 已核对其预算与装备语义。Fortification 和四系抗力已有原生护甲附魔。可复用 CotW 的原生物品附魔增删和储法投递；Aegis 需要保留启动时的配置快照、实际护甲身份、同一件护甲的储法载荷，以及两个动作费用。 [CotW 附魔辅助](../../../src/MagusEnchantments01454.cs) · [原作 Abjuration](https://github.com/Holic75/KingmakerRebalance/blob/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/Classes/Implements/Abjuration.cs)

`SlicePublished=false`。Aegis、另外六学派、四个 Panoply、剩余职业特性及普通启动登记仍待实现。实际提供方、完整升级/重训、磁盘存读档、游戏/UMM、模型补充、历史测试、打包和上传未运行。 [认领 #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15)
