> 🚧 **[施工占用 / WORK STATUS](WORK_STATUS.md)**：COTW-0046 Speaker for the Past已认领，请勿重复开发。
> Current preview: [0.14.68 Starseeker TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.68-Starseeker-TEST). Stable: 0.14.66.

# Call of the Wild — Wrath

Pathfinder: Wrath of the Righteous 的 Call of the Wild 内容移植项目。当前正式版本 **0.14.66**，修复Amnesiac的Spell Recollection；本轮新增迁移项0。

[下载安装包](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.66) · [安装说明](README.zh-CN.md) · [本轮测试说明](0.14.66Amnesiac-feedback-test-guide.txt)

后续agent：先读 [AGENTS.md](AGENTS.md)、[当前交接](docs/agent-handoff/HANDOFF.md) 和 [精简流程/耗时](CURRENT_RELEASE.md)。只改本轮内容，做一次必要定向检查，直接编译出包；遇到真实问题按log修。默认不跑测试套件、全历史或旧证书链。

构建入口 `build.ps1 -WrathPath '<本地游戏目录>'`，编译主体与两个内置模块并核对安装包。游戏及模块引用配置见 [依赖说明](docs/agent-handoff/DEPENDENCIES.md)；可选诊断可能另需本地夹具，仓库不分发整个work。

十三项HOLD见 [剩余清单](docs/agent-handoff/REMAINING.txt)，具体范围/排除在 [inventory.json](docs/agent-handoff/inventory.json) 中按需查。旧版指南与重复验证工具已从main移除，Git历史和正式附件保留。未启动游戏/UMM；托管检查不等于玩家验收。

项目基于 [Call of the Wild](https://github.com/Holic75/KingmakerRebalance)，许可见 [LICENSE-CotW.txt](LICENSE-CotW.txt)；集成模块、资源的来源与许可保留在各自目录。
