> Current preview: [0.14.67 Havocker / Mimic TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.67-Havocker-Mimic-TEST). Stable release: 0.14.66. See [scope and handoff](docs/havocker/README.md).

# CotW Wrath 0.14.66

当前版本修复Amnesiac的Spell Recollection：真实检定日志、施法菜单即时刷新、动态读取Psychic法术和名称描述、战斗外冷却与战斗中额外1d10/当回合施放保留，以及对应法术环首次开放时的回想资源。此前已交付功能作为可用基线，新增迁移项0。

## 安装与升级

1. 下载Releases的 `experimental.zip`，完全退出游戏，保留旧包和测试存档备份。
2. 将ZIP中的 `CallOfTheWildWrathPrototype` 放到 `%USERPROFILE%/AppData/LocalLow/Owlcat Games/Pathfinder Wrath Of The Righteous/Modifications/`，升级时替换同名目录，备份放在Modifications之外。
3. 保留包内 `Assemblies/`、`Blueprints/`、`Bundles/`、`Localization/`，空目录也保留。不要复制游戏或其他模组DLL进Assemblies。
4. 在游戏数据目录的 `OwlcatModificationManagerSettings.json` 的 `EnabledModifications` 数组中加入 `CallOfTheWildWrathPrototype`，保留其他启用项；旧版升级沿用相同标识。

包内集成Medium与WayOfTheShield模块，原生Owlcat入口。可选ModMenu显示模块状态；注册状态不代表游戏效果验收。沿用现有Harmony和兼容约定，不替换游戏Harmony。

## 测试与反馈

本轮步骤见 `0.14.66Amnesiac-feedback-test-guide.txt`；日志获取见 `测试者必读.txt`，必要的快速定位见 `百宝袋快速测试.txt`，QL汉化见 `Localization-QL使用说明.txt`。

遇到问题提供具体操作、版本与对应GameLogFull.txt片段；根据真实log修复。发布记录说明实际执行范围，未做玩家游戏验收，不把托管或隔离原生检查当作游戏通过。

## 开发与构建

先读 `AGENTS.md` 和 `CURRENT_RELEASE.md`。默认 `build.ps1 -WrathPath '<本地游戏目录>'` 只编译、打包并核对包内必要文件/DLL；当前变化按需做一次最小定向检查，不跑全历史，不因说明和上传重复跑同一内容。必要依赖和输出布局见 `docs/agent-handoff/DEPENDENCIES.md`。

每次记录实际耗时；未运行的检查写未运行。改完清理缓存、重复依赖和过期尝试，不创建多份冻结备份。保留当前源码、必要输入和正式交付原件。历史内容按需从Git记录或旧正式源码包找回。

基于Holic75的Call of the Wild，许可见 `LICENSE-CotW.txt`；集成模块与资源许可见其目录。
