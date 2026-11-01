# Call of the Wild — Wrath Prototype

Pathfinder: Wrath of the Righteous 的 Call of the Wild 内容移植项目。当前源码快照对应已交付的 **0.14.62**；下一批 Urban Bloodrager、Cryptid Scholar 与 Iroran Paladin 处于依赖核查阶段，尚未计入实现。

项目中的游戏玩法文本保持原语言；`README.zh-CN.md` 与各版本测试说明记录历史工作。0.14.62 的回归步骤见 [测试说明](0.14.62PsychicFeedback-test-guide.txt)。

## 源码与构建

此仓库包含项目源码、测试代码、资源、语言文件，以及 `Modules` 内的集成模块源码。它不包含游戏程序集、Harmony DLL、其他模组的预编译 DLL、游戏蓝图归档、个人日志或本地 `work` 验证夹具。

构建需要 Windows、本地正义之怒游戏文件以及项目使用的其他依赖。使用 `WOTR_GAME_PATH` 或 MSBuild 的 `WrathPath` 指定游戏目录。编译引用必须是游戏 `Wrath_Data/Managed/0Harmony.dll` 的 **2.0.4.0**，并通过仓库中的哈希校验。运行时兼容检查沿用项目现有策略。

`build.ps1` 和部分历史回归脚本还依赖原开发工作区的 `work` 目录、游戏蓝图数据与外部模组夹具；这些不在本仓库中。此快照不声称克隆后可直接运行完整发布门禁。

## 来源

项目包含对 [Call of the Wild for Kingmaker](https://github.com/Holic75/KingmakerRebalance) 的移植；其许可见 [LICENSE-CotW.txt](LICENSE-CotW.txt)。其他集成模块和资源的来源与许可见相应目录及源码声明。
