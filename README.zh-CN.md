# Call of the Wild — Wrath 原型（中文说明）

当前源码快照对应已交付 **0.14.65**（含 0.14.64 Amnesiac 与本版 Urban Bloodrager 反馈修复）。游戏内玩法文本保持英文；中文说明仅用于仓库与测试文档。

**测试入口：** [0.14.65UrbanBloodrager-feedback-test-guide.txt](0.14.65UrbanBloodrager-feedback-test-guide.txt)。近几版专项指南另见 `0.14.62`–`0.14.64` 同名 `*-test-guide.txt`。更早版本的逐步说明已归档至 [docs/history/test-guides/](docs/history/test-guides/)；0.13.x 法术清单见 [docs/history/spells/](docs/history/spells/)。

Harmony 编译与独立自检固定游戏 `Wrath_Data/Managed/0Harmony.dll` **2.0.4.0** 及仓库审核 SHA；流程见 [交付自检流程.md](交付自检流程.md)。内置 Medium **0.4.3**、Way of the Shield **4.0.13**。

英文概览与 Releases 下载见 [README.md](README.md)。

以下为仍适用的产品说明（安装、验证边界、ModMenu/Harmony、构建来源等），未改写机制描述。

## 验证边界

独立自检使用真实游戏托管类型和构造的角色、蓝图、规则状态，包含注册冲突/重复、资源公式、原生法术学习/遗忘/PostLoad、超魔及新伤害动作参数。仅测试替身避开Unity计时/阵营服务，并截获新伤害动作，不执行真实战斗。没有运行Unity、升级界面、休息、光环、洗点或游戏存档序列化。完整结果和摘要在工作区outputs/验证记录.json。

## ModMenu 与 Harmony

原生Owlcat入口，无UMM/TTT硬依赖。可选ModMenu 2.0.14显示版本行+10个模块状态（共11行），Registered仅表示注册。Harmony按2.0.4.0编译，检查2.0.4.0/2.2.2.0/2.3.6.0；不支持Harmony1。异能补丁布局不符时独立卸载并跳过新模块，保留旧功能。没有打包或替换游戏、Harmony、ModMenu DLL。游戏内英文在中文设置下也保持英文。

## 安装或从旧版升级

1. 完全退出游戏。备份可丢弃的测试存档、旧模组文件夹和下面的启用配置，保留当前旧包；不要覆盖正式进度。
2. 把 ZIP 内的 `CallOfTheWildWrathPrototype` 文件夹放到 `%USERPROFILE%/AppData/LocalLow/Owlcat Games/Pathfinder Wrath Of The Righteous/Modifications/`。升级时替换同名目录的内容，不要同时保留两个启用副本。旧目录备份应放在 Modifications 目录之外。
3. 保留 `Assemblies/`、`Blueprints/`、`Bundles/`、`Localization/` 四个目录，其中空目录也必须存在。Assemblies 中只放模组 DLL，不放说明、PDB 或游戏 DLL。
4. 启用配置是 `%USERPROFILE%/AppData/LocalLow/Owlcat Games/Pathfinder Wrath Of The Righteous/OwlcatModificationManagerSettings.json`。首次安装时把 `CallOfTheWildWrathPrototype` 加入 `EnabledModifications` 数组，并保留其他条目；文件不存在时才创建为 `{"EnabledModifications":["CallOfTheWildWrathPrototype"]}`。从 0.1.1 升级不需要改模组标识。
5. 启动后在上述游戏数据目录的 `GameLogFull.txt` 中搜索 `Experimental CotW port 0.6.5` 及四个专长和 `Rage powers` 各自的 `Registered:` 结果；若检测到拳斗士模组，还会记录独立的 `Brawler combat feat integration` 结果。如已安装并启用 ModMenu，可到设置 → Mods → Call of the Wild - Wrath Prototype 查看状态。未安装 ModMenu 时没有页面，专长仍可加载。启用配置变化需要重启；日志取法详见《测试者必读.txt》。

本次构建没有替测试者安装或修改游戏/存档。保留原身份不等于已证明跨版本存档安全。不要用 0.1.1 读取已经引用延长灾厄的较新存档，也不要用 0.2.x 读取已经引用 Improved Combat Expertise 的 0.3.x 存档；要回退应同时恢复升级前的存档和旧包。卸载前优先回到未引用这些专长的原始存档。


## 构建与来源

从本工作区运行build.ps1，传入本机WrathPath。编译与回归只使用游戏Wrath_Data/Managed/0Harmony.dll 2.0.4.0及固定SHA，不接受AdditionalHarmonyPaths选择其他运行时。需要PowerShell7.4+、.NET9 SDK、net472参考程序集、Python3及游戏/测试引用；直接使用ZIP的测试者不需要这些工具。构建仅输出包和验证记录，不安装或修改存档。

规则源为MIT许可CotW固定提交1332fb0db844b7863f484ca978bea2349fe49769，Psychic.cs、NewSpells.cs和SpellFailureMechanics.cs；原许可随包保留。新实现参考本机Wrath2.7.0x组件，不分发游戏资源或反编译文件。0.5.1源码及记录已保存在work/psychic060，T15剩余原因见迁移规划/T15本批实现与未完成清单.md。