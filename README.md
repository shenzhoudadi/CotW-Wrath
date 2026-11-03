开发候选分支：接手资料和 0.14.66 候选源码已上传；正式交付基线仍为 0.14.65。0.14.66 最终认证与正式包发布仍在进行，本次不创建正式 tag 或 Release。

# Call of the Wild — Wrath Prototype

Pathfinder: Wrath of the Righteous 的 Call of the Wild 内容移植项目。当前源码快照对应 **0.14.66**，修复 Amnesiac 的 Spell Recollection。保留此前已交付内容；本轮没有新增内容移植。

本版修复回想检定日志、施法菜单即时刷新、法术选项的名称和描述同步，以及对应法术环级的回想资源取得时间。战斗外沿用一小时冷却；战斗中回想不受该冷却限制，检定额外获得 1d10。战斗中新回想的法术须在本人下回合前成功施放一次，包含超魔版本，否则在本人下回合开始移除；使用后保留至休息。详见 [测试说明](0.14.66Amnesiac-feedback-test-guide.txt)。

发布须通过完整构建、最终安装包 DLL 的历史回归、双 Harmony 与 Mono 检查，以及独立只读复查。未启动游戏或 UMM；玩家反馈仅确认 Urban Bloodrager 正常，Amnesiac 的实际游戏表现仍须按说明观察。

供其他 agent 接手：先读 [AGENTS.md](AGENTS.md)，再读 [当前接手说明](docs/agent-handoff/HANDOFF.md)、[TODO](docs/agent-handoff/TODO.md) 和 [排除/覆盖索引](docs/agent-handoff/inventory.json)。

## 下载

[版本与附件](https://github.com/shenzhoudadi/CotW-Wrath/releases) · [0.14.66 安装包](https://github.com/shenzhoudadi/CotW-Wrath/releases/download/v0.14.66/CallOfTheWildWrathPrototype-0.14.66-experimental.zip)

安装包、源码包、测试说明和公开自检摘要均在 Releases 的 Assets 中。安装请使用 experimental.zip 安装包；GitHub 自动生成的 Source code 归档是仓库源码。历史附件保留。

## 源码与构建

此仓库包含项目源码、测试代码、资源、语言文件，以及 `Modules` 内的集成模块源码。它不包含游戏程序集、Harmony DLL、其他模组的预编译 DLL、游戏蓝图归档、个人日志或本地 `work` 验证夹具。

构建需要 Windows、本地正义之怒游戏文件以及项目使用的其他依赖。使用 `WOTR_GAME_PATH` 或 MSBuild 的 `WrathPath` 指定游戏目录。编译引用必须是游戏 `Wrath_Data/Managed/0Harmony.dll` 的 **2.0.4.0**，并通过仓库中的哈希校验。运行时兼容检查沿用项目现有策略。

`build.ps1` 和部分历史回归脚本还依赖原开发工作区的 `work` 目录、游戏蓝图数据与外部模组夹具；这些不在本仓库中。此快照不声称克隆后可直接运行完整发布门禁。

## 来源

项目包含对 [Call of the Wild for Kingmaker](https://github.com/Holic75/KingmakerRebalance) 的移植；其许可见 [LICENSE-CotW.txt](LICENSE-CotW.txt)。其他集成模块和资源的来源与许可见相应目录及源码声明。
