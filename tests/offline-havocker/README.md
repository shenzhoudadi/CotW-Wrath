# 当前Havocker定向消费者源码

这里保存本轮仍有用的消费者测试、adapter和最小宿主边界patch，不包含编译DLL、游戏数据或日志。

宿主为 [Acture/wotr-testing](https://github.com/Acture/wotr-testing) @ `487ecd0e043b75ee985b665c224716976a498cf9`，遵守其AGPL-3.0许可。保留测试源码中的来源注释；宿主许可随本目录保存，生产模组原许可不变。

将两个项目目录放到合法本地wotr-testing的tests/下，按需审阅并应用`wotr-testing-boundaries.patch`；相对Import是为那个宿主布局设计的，不能在本目录直接当独立工程运行。补丁仅为离线Unity环境边界，不替代费用／命中／时长逻辑。

指定WrathInstallDir、CotwAssembly及WotrHarmony=game；定向filter保持`FullyQualifiedName~CotW.Havocker.Tests.BridgeTests`，需要可进一步缩小到实际变化用例。可选TTT／DarkCodex探测的绝对夹具路径需接手者核对，仓库不分发这些DLL。未经需要不要运行整套宿主自测或其他包专项。

141/141是K12先前运行的基线；另有图标引用1/1、Mimic展示1/1的分次定向结果，不能相加冒充0.14.67整套新结果。0.14.67版本更新没有重跑测试。
