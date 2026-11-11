# COTW-0068 B 阶段精简来源摘要

合同 `C-0068-20261111-r1`。本文件只记录来源与实现边界，不含原生反编译、游戏文件、提供方原件或私人日志。完整命令、秒数、SHA256、失败原件与未测边界见本机 `B-progress.md` / `B-evidence.json`。

## 来源复用（不重抄、不重写）

| 复用对象 | 来源 | 本项用法 |
| --- | --- | --- |
| 180 行原作法表 | `MindbladeSpellAnchors01454.Rows` | `BuildList` 逐行复用；抽出 `RowLevel` / `ResolveRow` 给 Mindblade 与侦探共用 |
| 单行候选解析 | `Mindblade01454` | 保留候选优先顺序、去重和歧义冲突；Mindblade 的排除与 Find Traps 2 环语义不变 |
| 九项补充映射 | `A-evidence.json` 的 `providerCorrections` | 八项走 `PsychicSharedSpellCatalog.Entries` + `PsychicSpellIntegration.Available`；Greater Stunning Barrier 用核实的 TTT 身份与真实发布法表检查 |
| 法表规范 | `SpellListCompatibility.Build/Canonical` | 0–10 索引，7–10 环为空，显式侦探环位优先，重复身份折叠 |
| 施法语义 | `PsychicCasting` / `PsychicSpellbookMarker` | 思考 -10、Center Self 抵消、情绪阻止、Logical 绕过；新书 `MaximumSpellLevel=6` |
| 降环与临时知识 | `PsychicUndercasting` / `TemporaryKnowledge01464` | 不新增第二套账本 |
| 11 种增幅 | 既有 `PsychicAmplifications` 等 | 复用现有效果、层级、cast receipt 与失败返还；只绑定必要 mutable 授权子图 |
| 注册与发布 | `PsychicAssets` / `SpellBatch01447` / `ExtraInvestigatorTalent` | 全自有图先暂存再原子发布、校验、回滚；重入不重复 |
| 提供方 | HA 真实 Investigator 职业／进展／天赋／毒抗／毒免／旧书身份 | 只做只读核对与追加，不克隆、不改写提供方根 |

## 新增生产文件

- `src/PsychicDetective0068Ids.cs` — 私有 token、GUID、身份、11 种模式与 9 项禁用发现集中定义。
- `src/PsychicDetective0068Binding.cs` — S-PSYCHIC-0068 显式 binding、Q05 Meddler、Q07 天赋来源 Harmony 前缀。
- `src/PsychicDetective0068.cs` — F-0068 主体：原子门禁、六环换书、完整进展、法表、11 种绑定增幅、两项天赋、重入与刷新。

## 修改的既有文件

`src/PsychicAmplificationComponent.cs`（BoundBook / BoundPool、同书清理、按绑定等级门槛、按绑定池支付）、
`src/PsychicAdvancedRuntime01458.cs`（HasDual / Real / Candidate / Plan / Claim / Coordinate / Surge / Mimic 的书本作用域）、
`src/PsychicAmplificationMenu.cs`（私有 root / feedback 头与各菜单按真实法书解析）、
`src/Mindblade01454.cs`（抽出共享单行解析）、
`src/BlueprintRegistration.cs`（RegisterAll 与 RegisterOptional 接入）、
`src/ModuleCatalog.cs`（settings 状态行）。

旧组件缺少 `BoundBook` 时一律回落到原 Psychic 书／池／等级／Dual 语义，未改动既有 GUID 与 TypeId；新增TypeId 仅 `PsychicDetectiveMeddler0068` = `878620c7e3b466888f969af1289ca39d`。

## 定向检查入口

`tests/PsychicDetective0068Tests.cs` + `tests/PsychicTests.cs` 的 `COTW_0068_CASE=psychicdetective0068` 单一路由 + `tests/Run-PsychicDetective0068.py`。共享 fixture 之后立即 return，不运行任何历史行为套件。

结果：主体编译 0 警告 / 0 错误；150 条定向断言 PASS，14.59 秒，0 条 HOST LOG、0 条失败。受控反证：临时移除 `Owns` 的书本作用域后，用例在 105 条断言处按预期失败，恢复后重新绿跑。

## D 复查 NEEDS_FIX 四项阻断的修复（B-fix）

独立 D 复查（`D-review.md`）给出 NEEDS_FIX 四项阻断，已在同一隔离分支修复，未重做已通过部分：

- **D-F01**：一级 add 同时授予原生 Detect Magic 特性根（`cache.Load`，不克隆）与私有施法特性；`VerifyShared` 增加一级双特性门禁。
- **D-F02**：Defensive Prognostication 第 i 档费用 i、强度 2*i，两档分别绑定两个不同的真实效果 buff。
- **D-F03**：Overpowering Mind 只立即授 1 档；2/3 档经 HA Investigator 的真实 `AddFeatureOnClassLevel`（8/15 级）延迟授予，低等级不存在可关闭可用档的多余 toggle。
- **D-F04**：Meddler 只按 `RuleSavingThrow.GetStatFromSavingThrow(evt.Type)` 调整本次豁免 stat，经 `evt.AddTemporaryModifier` 登记（与 Steadfast Personality 同边界）。真实 fact + 真实 `RulebookEventBus` 事件覆盖 2/5/8/11/14/17/20 级 → +1…+6/+6、1 级 0、无关描述符与非 Spell/SLA 反例。加成观测用 stat `ModifiedValue` 增量：原生 `AddTemporaryModifier` 只登记清理列表，加成不进 `evt.AllBonuses`。

修复后 clean rebuild：主体 0 警告 / 0 错误；**305 条定向断言 PASS，15.55 秒，0 HOST LOG、0 失败**。四项各做一次受控反证（临时改坏 → 预期失败 → 逐字节还原），失败点分别命中 D-F01 门禁、D-F02 费用断言、D-F03 立即授予断言、D-F04 单 stat 边界断言。

## 显式未运行

游戏 / UMM 启动、整套 Psychic 与 wotr-testing、全套历史专项、真实 HA / TTT 插件加载、SaveManager 与重训控制器、全库矩阵、打包、版本、正式推送、GitHub 附件下载核对与 #12 归档。这些属于独立 D 复查与后续统一 TEST 交付。中文 TEST README 的单行“猫猫堡是猪猪”也留待交付阶段保留。
