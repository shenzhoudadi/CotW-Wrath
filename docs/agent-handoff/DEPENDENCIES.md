# 依赖与验证边界

项目目标框架为 .NET Framework 4.7.2，主要开发环境为 Windows。源码根是公开仓库根；原开发工作区将它放在 outputs/CallOfTheWild.Wrath.Prototype，部分脚本按这个历史布局定位外部 work 目录。

已有引用指纹见 [BUILD-REFERENCES.json](BUILD-REFERENCES.json)，用于核对本地依赖；它不包含这些程序集的字节。

项目通过 WOTR_GAME_PATH 或 MSBuild 的 WrathPath 找本地游戏目录，引用 Wrath_Data/Managed 的 Assembly-CSharp、UnityEngine、Owlcat 和相关程序集。编译 Harmony 必须是游戏提供的 0Harmony 2.0.4.0，并符合仓库哈希检查；不要以 NuGet 新版替代。Medium 和 WayOfTheShield 使用既有集成构建合同。其他外部依赖的实际位置以 csproj、Directory.Build.props、模块项目和 build.ps1 为准。

当前Medium编译仍需ModMenu、UMM和TTTCore引用；Brawler、外部适配与原生蓝图等夹具仅在对应可选诊断需要时取得。仓库不分发游戏程序集、游戏蓝图包、其他模组预编译 DLL、本地工作目录或私人日志。默认编译/出包无需整套历史work目录；先取得合法本地游戏与当前模块所需引用，并核验项目实际路径。可选旧诊断不保证环境完整。

普通编译入口为 CallOfTheWild.Wrath.Prototype.csproj；默认出包入口为 build.ps1，仅编译、打包并核对包内必要文件/DLL。启动、规则及Amnesiac专项仅在明确选择后运行。已交付内容作为可用基线，不再默认逐版本历史或递归认证链；旧指纹/旧夹具缺失不阻断默认构建。实际测试范围及未测项如实记录，分发包DLL仍对应当前构建对象。原完整脚本build.full-legacy.ps1仅供回看，不保证旧环境仍可重跑。

本地工作证据的不可移植路径在公开 inventory.json 中以 $WORKSPACE、$WOTR_GAME_PATH、$USER_PROFILE 占位；这些是来源标识，不表示相关文件已上传。内部 GUID 和公共来源链接保留，用于避免重复迁移和找回提供方证据。

提供方源码存在、旧 DLL 能反编译或找到 GUID，不等于当前已安装、启用、成功注册或规则等价。可选提供方缺失、晚加载、冲突和失败回滚应保持既有合同；Starseeker、HA Investigator 和 Antipaladin 的具体 HOLD 见 TODO/REMAINING。
