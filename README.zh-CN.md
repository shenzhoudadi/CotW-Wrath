# 0.14.76 Wild Armor / 武器特性 / Versatile Performance / Blade Rush TEST

本包在 0.14.75 基础上修两处。都只经过本机编译与包内核对，**没有做玩家实机验收**。

- **Kinetic Knight Blade Rush 修复**：DarkCodex 是 UMM 模组，它的蓝图在原生模组入口之后才发布，上一版首次尝试时必然取不到，于是整项被跳过。现在首次取不到会挂一个蓝图缓存监视：Blade Rush 一进入缓存，就把 `KineticBladeRush` 追加进 Kinetic Knight 的 3 级 `AddFeatures` 并报告注册成功。缺少 DarkCodex 时仍然完全不改动变体。**已有角色不会回溯补发**，请新建角色或重训到 3 级确认。
- **Weapon Shift 适用范围**：从"仅德鲁伊／变形者的 Wild Shape"改为**任何变形**（Wild Shape、Beast Shape、变形类法术等）。判定改成 `UnitBody.IsPolymorphed`，前置专长链**未改动**。描述同步改为 "During any polymorph or wild shape"。
- 其余内容与 0.14.75 相同：Wild armor（+3 护甲附魔）、武器类型 disarm/trip/sunder 特性、Versatile Performance 纯半级（2 级 +1、4 级 +2、12 级 +6）、Expanded Versatility、Martial Performance（武器名读游戏本地化）。

重点可测：Wild 变形期间 AC 不掉且结束后护甲恢复显示；连枷／镰刀类战技 +2 且白板武器仍可批量出售；Weapon Shift 在 Beast Shape 等非 Wild Shape 变形下也生效且 +2 只出现一次；Kinetic Knight 新建角色 3 级直接拿到 Blade Rush。

## 上一版：0.14.73 Cryptid Scholar TEST

猫猫堡是猪猪

猫猫堡别玩哈基米了快测试

本包新增 Cryptid Scholar（COTW-0070），是 Homebrew-Archetypes 的 Investigator 变体，保留该职业的法术书和灵感池。1 级获得 Intuitive Lore，4 级获得 Opportune Advice 和 Knowledgeable Strike，替换毒抗、毒免和 Studied Combat／Strike。

- **Opportune Advice**：学者本人通过识怪检定后，给 30 英尺内能听见的盟友对该怪物类别的洞察防御加值。同类在 24 小时内再次使用需消耗 1 点灵感。
- **Knowledgeable Strike**：结束自己的 Advice 批次，让仍在范围内的同源盟友在下一次合格武器命中时造成一次精准伤害。重点可测两名学者交错施放、授予失败后的旧效果和灵感余额，以及法术伤害与主武器命中先后结算。

此变体需要真实可用的 Homebrew-Archetypes Investigator。缺少提供方时，本变体会延后注册。[HA 参考版本](https://github.com/FrozenDervish/Homebrew-Archetypes/tree/211e9b82e252705dfaa7e4aa7507d9819737e232)

退出游戏，备份存档和旧模组目录。用 ZIP 内的完整 `CallOfTheWildWrathPrototype` 文件夹替换 Owlcat 模组目录中的同名文件夹。默认位置为 `%USERPROFILE%/AppData/LocalLow/Owlcat Games/Pathfinder Wrath Of The Righteous/Modifications/`。备份放在该目录外。

本项隔离定向 244 条通过，D3 独立复跑 5/5。真实 HA 插件启动、玩家实机、完整存读档和重训仍待验证。反馈请附职业等级、启用的提供方、操作步骤与日志。

[GitHub 测试包](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.73-Cryptid-Scholar-TEST) · [变体与验证说明](https://github.com/shenzhoudadi/CotW-Wrath/tree/main/docs/work-packages/COTW-0070)

[0.14.72 Psychic Detective 测试说明](docs/releases/0.14.72-TEST.md)

[0.14.71 Iroran Paladin 测试说明](docs/releases/0.14.71-TEST.md)

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
