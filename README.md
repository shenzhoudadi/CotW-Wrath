# Call of the Wild — Wrath Prototype

Pathfinder: Wrath of the Righteous 的 Call of the Wild 内容移植项目。当前源码快照对应已交付的 **0.14.65**：包含 0.14.64 的 Amnesiac 以及本版 Urban Bloodrager 修复。

项目中的游戏玩法文本保持原语言；`README.zh-CN.md` 记录概要，历史版本测试说明在 `docs/history/test-guides/`。0.14.65 的回归步骤见 [测试说明](0.14.65UrbanBloodrager-feedback-test-guide.txt)。完整托管门禁、最终安装包 DLL 双运行时与 Mono 检查和独立只读复查通过；游戏表现由测试者观察。

## 下载

[最新版本与全部附件](https://github.com/shenzhoudadi/CotW-Wrath/releases/latest) · [0.14.65 安装包](https://github.com/shenzhoudadi/CotW-Wrath/releases/download/v0.14.65/CallOfTheWildWrathPrototype-0.14.65-experimental.zip)

安装包、源码包、测试说明和公开自检摘要均在 Releases 的 Assets 中。安装请使用 experimental.zip 安装包；GitHub 自动生成的 Source code 归档是仓库源码。

## 源码与构建

此仓库包含项目源码、测试代码、资源、语言文件，以及 `Modules` 内的集成模块源码。它不包含游戏程序集、Harmony DLL、其他模组的预编译 DLL、游戏蓝图归档、个人日志或本地 `work` 验证夹具。

构建需要 Windows、本地正义之怒游戏文件以及项目使用的其他依赖。使用 `WOTR_GAME_PATH` 或 MSBuild 的 `WrathPath` 指定游戏目录。编译引用必须是游戏 `Wrath_Data/Managed/0Harmony.dll` 的 **2.0.4.0**，并通过仓库中的哈希校验。运行时兼容检查沿用项目现有策略。

`build.ps1` 和部分历史回归脚本还依赖原开发工作区的 `work` 目录、游戏蓝图数据与外部模组夹具；这些不在本仓库中。此快照不声称克隆后可直接运行完整发布门禁。

## 来源

项目包含对 [Call of the Wild for Kingmaker](https://github.com/Holic75/KingmakerRebalance) 的移植；其许可见 [LICENSE-CotW.txt](LICENSE-CotW.txt)。其他集成模块和资源的来源与许可见相应目录及源码声明。
