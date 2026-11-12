# COTW-0070 B 证据摘要

合同 C-0070-20261112-r1。分支 `feat/cotw-0070-cryptid-scholar-b`（自 main `f674d49`，基线 `1c0f3b0` / 0.14.72 TEST）。含 D NEEDS_FIX 退回后的修复轮。

## 结果

- 隔离定向：**PASS，236 条断言（原 214 + D 验收 22）**，两次复跑 14.39 秒 / 22.15 秒（PsychicTests 宿主，`COTW_0070_CASE=cryptidscholar0070`，`COTW_ARCANE_01446_ONLY=1`）。C0070-01…20 与 D-F01…F04 验收全覆盖。
- 主工程编译 0 警告 0 错误；最终增量 0.92 秒。
- 候选 DLL SHA256：`1CCD92DF65B7D37A581F48F61177DDB7DE0B0EA93C606A7325C3B0BEBC0E0B0F`。
- 原生分类/图标行 45 条自本机游戏归档提取，逐行 SHA256 与 A-evidence 一致；与 0060 扩展夹具合并 +15 行。
- 运行器 `tests/Run-CryptidScholar0070.py`、提取器 `tests/Extract-CryptidScholar0070.py`、定向 `tests/CryptidScholar0070Tests.cs`。生成件（报告/结果/时间戳日志）含绝对本机路径，按 B/D 交接约定只留本机交接目录 `C:/Users/xuenc/agent-tools/cryptid-scholar-next`（B-progress.md、B-evidence.json、B-report.json、B-result.json、B-directed-*.log），不随公开仓库分发。

## 实现

- `src/CryptidScholar0070.cs`：A-evidence 固定身份门禁（类型/名称/GUID/进展引用/授予等级/资源来源），MagusBatch01454 原子发布与回滚，幂等重入；AddFeatures 1/4，RemoveFeatures 12 条（毒抗 2/5/8、毒免 11、Studied Strike 4、Studied Combat 4–20 全偶数级）；保留 HA 旧书、灵感池、奇数级天赋。
- `src/CryptidScholarRuntime0070.cs`：精确分类键（11 广义类型 + 巨人/哥布林/爬行三类人入口 + 16 已验证亚型/种族/外界分组事实，GUID 排序去重；InstantEnemyBuff/PlantTypeFake 排除）；学者本人原生 TryMakeKnowledgeCheck 凭据；30 ft 可听盟友 Advice（含边界、静默识别）；24h 学者×分类收据，冷却内重用经原生 Resources 恰扣 1 点真实 HA Inspiration；批次账本 + 薄令牌 + 真实授予持有者集合；失源/重训清退本人批次，PostLoad 按游戏时间收束，重复加载不补不重授。
- 天赋排除复用 0068 `FromInvestigatorTalents` ancestry，仅本变体生效，原作九类 + Ranged Study 双入口共 10 身份；普通 HA、0068、独立来源不受影响。
- `BlueprintRegistration` RegisterAll/RegisterOptional 各接一条晚加载入口；`ModuleCatalog` 条目。

## D-F01…F04 修复

- **F01 事务性授予**：先应用后提交。原生 AddBuff 返回 null（RuleCanApplyBuff 拒绝）跳过该接收者；完整拒绝仅退本次实付 1 点，原批次/收据/令牌保留；中途失败移除已应用新令牌并仅退本次；提交后按批次持有者退旧令牌、每接收者替换任意旧 Advice。反例含完整拒绝、中途异常、双学者交错、拒绝后付费复用正例。
- **F02 豁免来源与替换**：要求敌对关系、排除物品来源（SourceItem）；临时 Insight 同时参与原生替换涉及的两个真实候选，由原生 OnTrigger 比较实际 ModifiedValue 选择，事件结束双清理。反例含盟友/物品/普通法术=0、敌对 Ex/Su/Sp=+2、已有更高 Insight 改变候选（12/13）。
- **F03 持有者集合**：批次记录真实授予持有者（含 PartyAndPets 外，UnitReference 序列化）；Strike 转换不要求施放者自身令牌，按批次持有者完成转换与清退。反例含自身被覆盖、远处持有者、非队伍盟友授予/转换/失源清退。
- **F04 射线/接触武器**：排除 WeaponCategory.Ray/Touch（与项目既有精准组件同口径）；带真实武器对象的射线与接触占位不追加不消费，真实武器命中恰结算一次，次生法术包不追加。

## 边界与未测

- Slowing Strike/Repositioning Strike 未证明专用 Studied Strike 执行依赖（执行 buff 不在固定供体快照），按 Q07 维持原作名单，交 D 复核。
- 宿主声明边界：HA 供体按固定身份 staging；识怪可见性/区域扫描/阵营/游戏时间/骰/中性难度/AC 场景与战斗日志为受控输入；分类事实以身份标记附着（原生机械组件剥离）；staging 灵感资源设固定上限 10 以匹配原生 Restore 钳制。**轻量宿主不执行 PrepareFactForAttach，拒绝反例在 AddBuff 返回边界注入 null；授予边界补丁须在动作首次 JIT 前安装（JIT 内联短包装）。** D 首轮 RuleCanApplyBuff 前缀在该宿主不触发，复查时需注意。详见 B-progress.md。
- 未运行：游戏/UMM、真实 HA 插件加载、完整历史套件、SaveManager/重训控制器、全库矩阵、打包与 TEST 发布。编译与隔离定向不等于玩家验收。