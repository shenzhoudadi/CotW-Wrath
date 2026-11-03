> 当前正式交付仍为0.14.65；0.14.66是正在最终验证的开发候选。候选源码：[agent-handoff-0.14.66-20261103](https://github.com/shenzhoudadi/CotW-Wrath/tree/agent-handoff-0.14.66-20261103)。本次只同步接手文档，不创建正式版本。

# 0.14.66 接手说明

本源码快照在 0.14.65 基础上修复 Amnesiac Spell Recollection，新增迁移项为零。实际发布与附件以 [v0.14.66](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.66) 为准；仓库文档不构成玩家游戏验收。

五项行为：日志显示真实 d100、战斗 d10 及合计；临时法术加入和移除后调用原生 UISettings.SetDirty；选项按实际 Psychic 法术表生成并共享对应法术本地化名称/描述；战斗外一小时冷却，战斗中不受其限制且不刷新它，成功回想的法术须在本人下一回合前成功施放一次（含超魔）才保留至休息，否则移除；新开放法术环级时同步获得对应回想资源，旧状态只补容量差额，不恢复已消耗次数。

主要实现位于 [Amnesiac01464.cs](https://github.com/shenzhoudadi/CotW-Wrath/blob/agent-handoff-0.14.66-20261103/src/Amnesiac01464.cs)、[AmnesiacRuntime01464.cs](https://github.com/shenzhoudadi/CotW-Wrath/blob/agent-handoff-0.14.66-20261103/src/AmnesiacRuntime01464.cs)。共享机制是 [TemporaryKnowledge01464.cs](https://github.com/shenzhoudadi/CotW-Wrath/blob/agent-handoff-0.14.66-20261103/src/TemporaryKnowledge01464.cs)，本轮没有重复实现它。原四个 Amnesiac TypeId 不变，CombatPending 新字段对旧数据默认 false；整个主程序集保留 306 个已有 TypeId。内部合并编译器属性的随机 GUID 按原有 ILRepack/门禁合同归一，不要求与旧 DLL 原始字节相同。

专项入口见 [AmnesiacFeedback066Tests.cs](https://github.com/shenzhoudadi/CotW-Wrath/blob/agent-handoff-0.14.66-20261103/tests/AmnesiacFeedback066Tests.cs)、[Amnesiac01464Tests.cs](https://github.com/shenzhoudadi/CotW-Wrath/blob/agent-handoff-0.14.66-20261103/tests/Amnesiac01464Tests.cs) 和 [Test-Amnesiac01464.py](https://github.com/shenzhoudadi/CotW-Wrath/blob/agent-handoff-0.14.66-20261103/tests/Test-Amnesiac01464.py)。历史 DLL 对照采用明确的实际 SHA 区分旧行为；不要为了新行为修改旧对照的预期。

玩家观察步骤见 [本版测试说明](https://github.com/shenzhoudadi/CotW-Wrath/blob/agent-handoff-0.14.66-20261103/0.14.66Amnesiac-feedback-test-guide.txt)。用户仅反馈 Urban Bloodrager 正常；没有启动游戏或 UMM，也不推定完整真实回合、界面、存读档、洗点或第三方模组组合已经由玩家通过。

Starseeker 审计仍为 HOLD：90 项目录不能拆成部分变体发布；五个提供方候选为 ExpandedContent 的 Hypnotic Pattern/Particulate Form、TomeOfTheFirebird 的 Mass Fly、CharacterOptionsPlus 的 Invisibility Purge、PrestigePlus 的 Akashic Form。Mass Fly 和 Akashic Form 已发现与原 CotW 行为差异；名称和 GUID 不是等价实现证明。完整目录元数据见 [审计索引](audits/starseeker-catalog.json)。HA Investigator 真实接口仍未建立；MCE Antipaladin 有来源，但完整接入合同未建立。

0.14.65 的一份历史原生索引原字节已缺失。旧证书、旧期望指纹和严格失败记录永久保留；用户确认了单独的历史证据接续方案，要求同等覆盖重测、逐项影响核对、独立复查及在新证书分列旧缺口和新结果。新验证不得称旧原件恢复。该例外只覆盖已声明的历史缺口，不能推广为其他输入缺失自动通过。

下一步只按新的用户任务选包。先读 [TODO](TODO.md)、[排除/覆盖条目](inventory.json) 及 [构建依赖](DEPENDENCIES.md)，不要由历史待办自动恢复用户排除项。
