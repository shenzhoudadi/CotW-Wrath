> 当前正式交付仍为0.14.65；0.14.66是正在最终验证的开发候选。候选源码：[agent-handoff-0.14.66-20261103](https://github.com/shenzhoudadi/CotW-Wrath/tree/agent-handoff-0.14.66-20261103)。本次只同步接手文档，不创建正式版本。

# 当前 TODO

0.14.65 已交付，不重做；0.14.66 只修复 Amnesiac 五项反馈，新增迁移项0。COTW-0080 Amnesiac 已于0.14.64完整迁移，本轮不是另一项迁移。下一批范围须按用户新任务确认，不由此文档自动开工。

唯一允许的剩余选题为 [REMAINING.txt](REMAINING.txt) 的十三项，当前均 HOLD：Havocker、Speaker for the Past、Spirit Guide、Psychic Detective、Cryptid Scholar、Esoteric Starseeker、Insinuator、Unholy Minion、Iroran Paladin、Sacred Servant、Vindictive Bastard、Spirit Whisperer、Relic Hunter。

Starseeker 的临时知识基础机制已有实现，后续复用，不重新造轮子；尚缺完整90法术可用性/等价行为与星座规则合同。五个提供方候选及 Mass Fly/Akashic Form 差异见 HANDOFF 与审计数据。不得以相似法术替换缺项或发部分星座。

Psychic Detective/Cryptid Scholar 保持 HA 真实接口依赖；Antipaladin 相关变体不能因找到 MCE 源码就直接放行。其他项按 REMAINING 中各自的共享规则/资源/来源所有权合同处理。

开始新包前，查 [inventory.json](inventory.json) 的 id、status、excludeNewImplementation、userSelectionAllowed、dedupDisposition、来源及历史注记。572条是完整条目索引，不是572项待实现任务；历史 tasks 和 ecosystemProfile 不能覆盖当前排除与已交付状态。除这十三项外不自行恢复候选。

玩家反馈仍按具体范围记录。Urban Bloodrager 本轮收到“正常”；Amnesiac 的实际画面、完整回合、真实存读档/洗点和模组组合由测试者按说明观察，不从自动验证推定通过。
