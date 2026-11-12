# COTW-0070 B 证据摘要

合同 C-0070-20261112-r1。分支 `feat/cotw-0070-cryptid-scholar-b`（自 main `f674d49`，基线 `1c0f3b0` / 0.14.72 TEST）。

## 结果

- 隔离定向：**PASS，214 条断言 / 14.69 秒**（PsychicTests 宿主，`COTW_0070_CASE=cryptidscholar0070`，`COTW_ARCANE_01446_ONLY=1`）。C0070-01…20 全覆盖。
- 主工程编译 0 警告 0 错误；最终增量 0.909 秒。
- 候选 DLL SHA256：`CCFC6DE33DACA31B94C30C69BE5D2B5666967F125AA1AC8066B9489C16CEAFE5`。
- 原生分类/图标行 45 条自本机游戏归档提取，逐行 SHA256 与 A-evidence 一致；与 0060 扩展夹具合并 +15 行。
- 运行器 `tests/Run-CryptidScholar0070.py`、提取器 `tests/Extract-CryptidScholar0070.py`、定向 `tests/CryptidScholar0070Tests.cs`；机器指纹与命令见 `tests/cryptidscholar0070-result.json`。完整进度与本机日志在交接目录 `C:/Users/xuenc/agent-tools/cryptid-scholar-next`（B-progress.md、B-evidence.json、B-directed-*.log），不随公开仓库分发。

## 实现

- `src/CryptidScholar0070.cs`：A-evidence 固定身份门禁（类型/名称/GUID/进展引用/授予等级/资源来源），MagusBatch01454 原子发布与回滚，幂等重入；AddFeatures 1/4，RemoveFeatures 12 条（毒抗 2/5/8、毒免 11、Studied Strike 4、Studied Combat 4–20 全偶数级）；保留 HA 旧书、灵感池、奇数级天赋。
- `src/CryptidScholarRuntime0070.cs`：精确分类键（11 广义类型 + 巨人/哥布林/爬行三类人入口 + 16 已验证亚型/种族/外界分组事实，GUID 排序去重；InstantEnemyBuff/PlantTypeFake 排除）；学者本人原生 TryMakeKnowledgeCheck 凭据；30 ft 可听盟友 Advice（含边界、静默识别）；24h 学者×分类收据，冷却内重用经原生 Resources 恰扣 1 点真实 HA Inspiration，失败仅返本次；批次账本 + 薄令牌，每接收者显式替换旧 Advice/Strike；Strike 接 RulePrepareDamage 真实命中/武器/隐蔽/精准免疫/远程 30 ft/DisablePrecisionDamage，同一 AttackRoll 只消费一次，独立 Precision 包临界倍率 1，保留原生 Fortification；失源/重训清退本人批次，PostLoad 按游戏时间收束，重复加载不补不重授。
- 天赋排除复用 0068 `FromInvestigatorTalents` ancestry，仅本变体生效，原作九类 + Ranged Study 双入口共 10 身份；普通 HA、0068、独立来源不受影响。
- `BlueprintRegistration` RegisterAll/RegisterOptional 各接一条晚加载入口；`ModuleCatalog` 条目。

## 边界与未测

- Slowing Strike/Repositioning Strike 未证明专用 Studied Strike 执行依赖（执行 buff 不在固定供体快照），按 Q07 维持原作名单，交 D 复核。
- 宿主声明边界：HA 供体按固定身份 staging；识怪可见性/区域扫描/阵营/游戏时间/骰/中性难度/AC 场景与战斗日志为受控输入；分类事实以身份标记附着（原生机械组件剥离）。详见 B-progress.md。
- 未运行：游戏/UMM、真实 HA 插件加载、完整历史套件、SaveManager/重训控制器、全库矩阵、打包与 TEST 发布。编译与隔离定向不等于玩家验收。