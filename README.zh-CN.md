0.14.69 Combined TEST（测试包，非正式版）

0.14.69 Combined TEST / 测试包（非正式版）

本次将当前GitHub main的新增内容统一放进测试安装包：
1. Spirit Whisperer：十魂域Wizard变体、魂域能力和巫术、仅魔宠Arcane Bond及独立属性／资源适配。
2. sxl1216贡献的Vindictive Bastard独立职业和Spirit-Bound Blade的Menacing选项。
3. sxl1216贡献的Sacred Servant及Live On复用原生Paladin mercies更新。
保留已有Havocker、Mimic、Starseeker和内置Medium／Way of the Shield模块。
尚未通过独立复查的Speaker for the Past不在本包内。

安装：备份现有模组目录和存档，退出游戏；将ZIP内完整的CallOfTheWildWrathPrototype文件夹替换到当前已经正常工作的Owlcat模组安装目录（沿用原安装位置）。不要只替换主DLL，保留Modules、资源和Localization目录。若已装同名旧版本，先移出旧文件夹再放入新文件夹。不要同时启用旧的独立Medium Class安装；本包没有替你安装UMM或第三方依赖，沿用现有合法依赖配置。

验证范围：Spirit Whisperer沿用174条隔离结构／组件及有限真实回调记录（37.59秒）与独立D结论；外部贡献沿用用户认可的贡献者测试。本次仅版本／说明变更后重新编译、打包和核对包内DLL，没有重复跑全量或同内容专项，没有启动游戏／UMM。

已知边界：Heaven's Leap若同时残留自己和队友标记，可能优先选中自己；完整位移／天气／支付／休息／重训／存档UI和完整战斗链不是本机玩家验收结果。原有Havocker、Starseeker的提供方和已知边界仍适用。具体见GitHub docs/spirit-whisperer/README.md、docs/havocker/README.md和docs/starseeker/README.md。可用模块仍受其提供方／依赖是否就绪影响。

反馈：附Player.log或模组日志、职业／等级、具体能力及操作步骤；按实际问题定向修复。不要把离线检查记作玩家游戏全部PASS。

命名：本包与预发布标签均明确含TEST。正式版不带TEST；0.14.66正式版和旧TEST附件保留，不覆盖旧文件。


以下为保留的旧版说明，仅供历史参考，不是本次全量验收要求：

> 🚧 **[施工占用 / WORK STATUS](WORK_STATUS.md)**：COTW-0046 Speaker for the Past已认领，请勿重复开发。
> Current preview: [0.14.68 Starseeker TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.68-Starseeker-TEST). Stable: 0.14.66.

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
