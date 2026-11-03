# Agent entry point

先读 [当前接手说明](docs/agent-handoff/HANDOFF.md)、[TODO](docs/agent-handoff/TODO.md)、[最新完整交付清单](docs/agent-handoff/DELIVERED.txt)、[开发与交付约定](docs/agent-handoff/POLICY.md)，再读 [完整条目与排除状态](docs/agent-handoff/inventory.json) 和 [十三项剩余清单](docs/agent-handoff/REMAINING.txt)。版本状态以对应 GitHub Release 的实际附件为准。

0.14.65 已交付，0.14.66 是 Amnesiac 反馈修复；不要重复迁移它们。用户排除项、原生和其他模组覆盖项不可重新加入。十三项 HOLD 不是可直接实现的工作队列。已有 TemporaryKnowledge01464.cs 机制应复用；共享合同先完成，才放行依赖功能。

采用小工作包、定向正反例、实际原生调用路径检查和最后统一整合。一个变体的必要子能力放在同一包；独立包可在隔离目录或分支并行，共享生产文件由一个负责人整合。保存实际源码、DLL、依赖指纹、退出状态和日志，保留所有失败原件；不得删断言、降低阈值或缩小覆盖换取通过。

编译引用沿用游戏 0Harmony 2.0.4.0 及仓库哈希约束；运行时沿用既有 Harmony/提供方兼容合同。保留已有 TypeId、原生来源所有权、洗点/升级/休息及存读档边界。没有真实证据不得称提供方已注册或全模组兼容。

不要启动游戏或 UMM。托管和隔离原生检查不等于玩家通过；目前用户仅对 Urban Bloodrager 给出本轮“正常”反馈，不扩大到 Amnesiac。游戏内自有文本保持英文，兼容既有语言文件约定。

必要出包程序自行完成，不逐次询问或汇报；改变门禁接受方式须先给具体方案确认。0.14.65 历史输入缺口仅按用户已确认的专项接续方案处理，不授权忽略其他证据问题。完整验证、独立只读复查、统一版本与出包、GitHub/原 Drive 上传和下载回读均属正式交付要求；所有旧文件保留。

本公开仓库包含源码和接手资料；完整本地发布夹具不随仓库分发。先读 [依赖和验证边界](docs/agent-handoff/DEPENDENCIES.md)，不要把缺少本地夹具的克隆当作完整发布环境，也不要把历史 PASS 当本次执行。
