# 当前依赖

目标为Windows / .NET Framework4.7.2。仓库根就是源码根；原本地目录为 `outputs/CallOfTheWild.Wrath.Prototype`。

默认编译需要合法本地游戏的 `Wrath_Data/Managed`、.NET SDK与模块所需引用。2026-11-07 重新审计后，Harmony 编译基线切到 UMM 0.33.2 自带的 `Wrath_Data/Managed/UnityModManager/0Harmony.dll`（2.3.6.0，SHA256 `DF043F05C6E43602FA8E8F38F52A3A2CF962AAB7A65A9D85573EA25F01A63E80`）；仍禁止 NuGet 或替代副本。运行时由 `Shared/HarmonyRuntimeCompatibility.cs` 接受已加载的兼容 2.x。历史 .txt 与旧测试说明中的 2.0.4.0／Managed 根路径是旧基线，不再适用。Medium的ModMenu、UMM、TTTCore路径在 `Modules/MediumClass/MediumClass.local.props` 配置（参考example）；文件不提交。项目和模块csproj/props中的实际路径为准。

当前构建仍按源码上两级定位 `work`。独立clone请先核对输出布局，或沿用 `outputs/CallOfTheWild.Wrath.Prototype` 布局；只准备当前必要引用，无须完整旧work。已记录的参考指纹在 `BUILD-REFERENCES.json`，按所需引用查，不默认遍历审计所有历史依赖。

`-CheckStartup`、`-CheckRules` 和 `-Focus amnesiac066` 为显式诊断；Amnesiac旧宿主/蓝图/外部模组夹具不随仓库分发，旧环境不承诺完整。当前有用的测试源码保留，缺失时按具体问题补最小夹具，不恢复全历史环境或把其缺失当作普通出包失败。

游戏程序集、其他模组预编译DLL、蓝图包和私人日志不提交公开仓库或进入安装包。旧指南与索引中的历史路径仅作来源记录，可能已删除，不表示必须重新取得或逐一复验。
