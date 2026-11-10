0.14.70 Spirit Guide TEST（测试包，非正式版）

猫猫堡是猪猪

本包新增 Spirit Guide（Oracle变体）：固定选择十魂域之一，3级巫术、4级起奖励法术、7级基础能力、15级高阶能力；含Oracle/Charisma与指定Wisdom分支、Mystery技能来源适配、Lore参数化选法及来源安全清理。
保留0.14.69已交付的Spirit Whisperer、Havocker、Mimic、Starseeker、外部贡献以及Medium/Way of the Shield内置模块。Speaker for the Past仍未纳入。

重要：Spirit Guide需ExpandedContent的Wall of Fire、Hypnotic Pattern、Incendiary Cloud及CharacterOptionsPlus的Frostbite真实可用。任一缺失、停用或为空壳时，整个Spirit Guide不激活；其他已交付模块按各自条件工作。本包不包含或安装这些提供方，也不重新移植其法术。八个CotW本项目法术继续复用已有实现。

安装：退出游戏并备份模组目录与存档，将ZIP中的完整CallOfTheWildWrathPrototype文件夹放到现有Owlcat模组目录。默认路径为 %USERPROFILE%/AppData/LocalLow/Owlcat Games/Pathfinder Wrath Of The Righteous/Modifications/。升级时替换同名旧目录，备份移到Modifications之外；保留Modules、Localization及空Blueprints/Bundles目录。不要只替换主DLL，不要同时启用旧独立Medium安装。沿用已有合法依赖；发布者未启动游戏或UMM。

验证：本项320条隔离定向断言、15.00秒、exit0；D3独立复查通过。Life缺行受控反证在同一二进制下明确失败并保留原件。整合仅重编译、打包及包内DLL核对，不重跑同内容专项、全历史或全量测试。

未测：真实外部提供方执行、玩家实机、SaveManager/升级UI、完整休息重训、Life引导治疗/伤害结算等。Battle支付证据限于私有资源指向及Resources.Spend，不是variant完整CanUse/Spend链；内部18根归档重著、8根host暂存不算完整生产注册通过。旧模块已披露边界继续有效；0117旧Heaven's Leap选标边界不代表新0060使用同一实现。

反馈请附日志、职业/等级、所选魂域/巫术、提供方启用状态及操作步骤。命名中TEST/测试包表示预发布；正式版不带TEST。旧正式0.14.66与旧TEST附件保留。

下载：https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.70-Spirit-Guide-TEST
交接：docs/work-packages/COTW-0060/README.md、D-review.md、VALIDATION.json。


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
