# Medium集成模块接手

此模块随CotW Wrath交付；先读仓库根AGENTS.md和CURRENT_RELEASE.md，沿用精简出包流程。不要按旧版本交接要求恢复全部历史检查或夹具。

当前编译/StageMod合同、GUID/TypeId、现有Harmony与共享施法兼容保持。复制MediumClass.local.props.example配置合法本地游戏及UMM/ModMenu/TTTCore引用，不提交本地路径或预编译依赖。

只对本轮实际修改做一次必要定向检查；未改模块不跑全套模块测试。实际失败按log修复，未测不写PASS，不启动游戏/UMM。完成后清理无用生成物和重复依赖。
