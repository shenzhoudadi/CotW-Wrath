# COTW-0099 B11 / D11 幻术系

幻术法器授予 Color Beam 和 Distortion，并开放八个原版法术根。Terror 可从一级选择，Unseen 从七级选择，Bedeveling Aura 和 Shadow Beasts 从九级选择。

- Color Beam 沿用原版 Blinding Ray 及其目盲、目眩分支。Unseen 两支沿用 Greater Invisibility，自己或盟友分别消耗 1 或 2 点。Distortion 与 CotW 的 Ether 共用独立未命中判定和结果记录，原版识破条件照常生效。
- Terror 沿用 CotW 的 Phantasm 借机攻击动作及既有持握触摸接线。Bedeveling Aura 沿用 Phantasm 的敌方区域、迟缓、禁用借机和夹击机制。
- Shadow Beasts 使用原作限定的 15 个原版召唤根及 48 个变体。Wrath 原生处理怀疑检定、伤害比例和非伤害效果。施法透传 60% 真实度，额外只挂载原作的 60% 最大生命值调整。

D11 已复查影兽的生命值调整不会被自身怀疑检定拦截，重叠光环可以独立退出，未命中记录优先保留实际导致失败的骰。最终编译 0 警告、0 错误，1.250 秒。原生隔离 425/425、零宿主异常，4.68 秒。状态模型 344/344，2.542 秒。 [机器可读证据](B11-D11-evidence.json)

原生隔离验证了真实法表、选择、支付、命中后分支、未命中入口、光环 Buff 来源及召唤上下文。真实投射物和触摸命中、借机执行、场景光环与怪物生成、实机、磁盘存读档和出包未运行。 [认领 #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15)
