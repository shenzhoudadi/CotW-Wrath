# COTW-0099 · B 修复轮记录（对应 D 自查 9 项发现）

复查对象：`b6be98b`（B 首切片 Abjuration）。D 自查报告：[D-occultist-review.md](D-occultist-review.md)，结论 **NEEDS_FIX**，9 项发现 D-0099-01～09。合同 `C-0099-20261115-r1` **本轮未改**。分支 `feat/cotw-0099-occultist`，认领 [issue #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15) 保持开放。

**角色声明**：本轮修复与复跑均由**本会话（同时是 B 实现者与 D 自查者）**执行。D 自查不满足分工文档「D 换人」，**本文件不构成独立 D 签字**。正式独立 D 仍需维护者另指派执行者，重点复验 D-0099-01 与 D-0099-02。

**总体状态**：9 项发现全部有对应改动与可复核证据；其中 **D-0099-06 的 SP08 原生夹具部分仍未达成合同要求**，已按 D 的要求降级为 `not_run`，不写成 PASS。切片仍 `SlicePublished=false`、未注册、`releaseEligible=false`、无玩家验收。

## 逐条闭环

| 发现 | 优先级 | 状态 | 落点 |
| --- | --- | --- | --- |
| D-0099-01 原生入口零接线 | P1 | **已闭环（定向范围）** | `src/Occultist0099NativeWiring.cs`（新增）+ `tests/Occultist0099NativeTests.csproj` / `Occultist0099NativeProgram.cs` / `Run-Occultist0099Native.py`（新增） |
| D-0099-02 RemoveClass 丢去重身份 | P1 | **已闭环** | `src/Occultist0099Rules.cs` `RemoveClass`；`tests/Occultist0099SliceProgram.cs` `DFix02`；D-F01 转 PASS |
| D-0099-03 Int 门槛缺失 | P1 | **已闭环** | `src/Occultist0099Spells.cs` `Intelligence` / `MaxCircle` / `IntAllowsCircle` / `CanCast` / `SetIntelligence` / `Learn`；`DFix03`；原生 SP02；D-F02 转 PASS |
| D-0099-04 结果无限定 + 恒真断言 | P2 | **已闭环** | 三份机器可读报告全部带作用域；恒真断言改为独立来源或行为正反例 |
| D-0099-05 指纹漏 7 字段 | P2 | **已闭环** | `src/Occultist0099Spells.cs` `Fingerprint` / `Clone` / `CancelPreview`；`DFix05`；D-F03 升级为 10 字段全探 |
| D-0099-06 canonical/注册门禁是门面 | P2 | **部分闭环，SP08 仍 not_run** | `AddVariantRoot` / `CanonicalOf` / `SetCotWRegistrationComplete` / `Occultist0099Registration`；`DFix06`；D-F04 / D-F05 转 PASS |
| D-0099-07 DLL 指纹不可复现 | P2 | **已闭环** | 权威口径改源码 SHA256，撤回 `AA9C1F8D…D5EB` 并说明来源 |
| D-0099-08 接线前必修 | P1（接线前） | **已闭环** | `src/Occultist0099Native.cs` 休息身份 / `OnActivate` 守卫；`src/Occultist0099Ids.cs` TypeId 常量与 `SlicePublished` 强制门禁 |
| D-0099-09 「含 6 Knacks」措辞 | P3 | **已闭环** | 生产注释、B 文档、状态与交接文档；issue #15 评论同步更正 |

## D-0099-01 · 原生入口接线（本轮主体）

新增 `src/Occultist0099NativeWiring.cs`，只接三条链，不动共享注册：

1. **Mental Focus 支付**：`Occultist0099FocusResourceLogic : AbilityResourceLogic`。原生管线经 `BlueprintAbility.GetComponent<AbilityResourceLogic>()` → `AbilityCastRateUtils.SpendResources` → `AbilityData.ResourceLogic.Spend` 抵达。UI 预检 `IsAbilityRestrictionPassed` 与实际 `Spend` 调同一权威谓词／交易，并共享同一 `AbilityData.UniqueId`。原生 `BlueprintAbilityResource` 只作**显示镜像**（`GetMaxAmount` 非 virtual，故用 `AddAbilityResources(RestoreAmount=false)` + 成功交易后 `SyncResourceMirror()` 写 `UnitAbilityResource.Amount`），镜像从不被本组件扣款，不存在双扣。
2. **法术学习**：`Occultist0099LearnGate` 经两条 Harmony 桥接到 `SelectSpell.Check`（postfix）与 `Spellbook.AddKnown`（prefix，拒绝时 `__result=null; return false;`）。被拒的选择不会到达 `SpellSelectionData.SpendSlot` 或 `AddKnown`。
3. **休息／等级身份**：`Occultist0099SpellbookFact0099` 在休息与升级时从真实 `ClassData.Level` 与真实 Intelligence 统计重同步。

### 接线暴露的两个真实生产缺陷（模型层永远看不到）

这两个缺陷是 D-0099-01 要求「真实原生入口」才暴露出来的，**不是测试代码问题**：

- **支付链上下文缺陷**：`UnitFactComponentDelegate<TData>` 的 `Owner` / `Data` 只在 `RequestEventContext()` 内有效，而原生 `AbilityResourceLogic.IsAbilityRestrictionPassed` / `Spend` 都从**事实上下文之外**调用。首跑抛 `ComponentRuntime is unavailable in current context`。修复：新增 `Occultist0099NativeScope.RuntimeOf` / `OwnerOf` / `InRuntimeContext`，改为「先解析活 runtime → 用**上下文无关**的 `EntityFactComponent.Owner` 验权 → 再打开原生上下文读状态」。
- **学习链同类缺陷**：`Occultist0099LearnGate.CanLearn` 经 `SelectSpell.Check` postfix 进入时同样在上下文之外。同法修复。

### 夹具缺陷（测试侧，已如实分类）

- **210 个原生组件被静默跳过**：`BlueprintComponent.OnDeserialized` 内部读 `Json.BlueprintBeingRead.Data`，不设该上下文则每个组件抛 NRE。原夹具用 `catch {}` 吞掉，导致 MageShield 的 `SpellComponent` 丢失、`SP01N … really is Abjuration` 失败。修复：按仓库既有 `Arcane01446Tests` 范式设 `Json.BlueprintBeingRead = new BlueprintJsonWrapper(bp)` 并 `finally` 还原，同时把吞异常改为打印 inner exception。现 `skippedArchiveComponents=0`，学派由**真实原生反序列化器**证明。
- **18 根夹具边界**：已审阅档案只含 13 条 Wrath 源根。改为如实分类：13 档案根 + 4 条 CotW 根走**真实生产 builder**（仅 `PsychicOriginalIcons.Load` 因 Unity `ImageConversion` 边界替换）+ 1 条 External 根 host-stage（真实第三方程序集未加载，已披露）。报告写入 `rootFixtureBoundary`。
- **RESTN 用例方向错**：新 camp 重建周期后 `Locked=false`，按 MF01 正确拒绝支付，原用例却断言能付。修复：补 `!IsLocked()` 断言 + 重新 Invest/Lock 后再验支付。
- **D-F02 探针夹具错**（本轮加强 D 反例时发现）：把 1 环 MageShield 学进 2 环槽，返回 `RejectedInvalidArgument`。改用 2 环根 `ResistEnergy`，使两角色唯一差异是 Int。

### 原生定向检查结果

`python tests/Run-Occultist0099Native.py` → **NATIVE_DIRECTED_PASS，80 条断言 / 0 失败**，exit 0，宿主进程 3.51 秒（运行器 3.976 秒）。`hostLoggedExceptions=0`、`skippedArchiveComponents=0`、`productionBridgesInstalled=2`、`slicePublished=false`、`releaseEligible=false`、`playerGameAcceptance=false`。

用例原生状态：MF01 `native_ability_resource_logic_pass`、SP01 `native_select_spell_pass`、SP02 `native_int_gate_pass`、MF07 `native_rest_camp_identity_pass`（仅休息身份）、SP08 `native_gate_only_registration_not_run`，其余 14 条 `not_run_native`。满足 D 要求的「至少一条真实支付 + 一条原生学习选择，跑 MF01/SP01 正反例，其余按实际状态记 not_run」。

## D-0099-02 · 去重身份周期作用域

`ChargedExecutions` / `ConsumedCorpses` 是**周期记录的一部分**，只在 `BeginCycle`（真正的新休息周期）清空；`RemoveClass` 不再清空，`ReacquireClass` 无需恢复。`DFix02` 覆盖：首次付 Ok（U=1）→ RemoveClass 后 id 仍在 → Reacquire 后同 id `RejectedDuplicate` 且指纹逐字节不变、U/R 不变、`CanPaySchool=false` → **不同 id 仍可付**（U=2）→ 已消耗尸体仍 `RejectedTargetInvalid` → 新周期清空后同 id 才能再付。D-F01 由 FAIL 转 **PASS**（`Spent=1`）。

## D-0099-03 · Int ≥ 10 + 环位

`SpellState.Intelligence` 新字段；`MaxCircle = min(等级表, Int-10)`；`IntAllowsCircle` / `CanCast` / `Learn` 全部校验。临时 Int 下降**只挡后续学习与施法，不删除已建立配额或来源**；Int 回升重新开放。原生适配器传真实 `Owner.Stats.Intelligence`。D-F02 本轮从「字段存在」升级为行为正反例：L4/Int10 → maxCircle=0、2 环无配额、learn `RejectedInsufficient`；L4/Int16 → maxCircle=2、learn Ok、可施；降到 11 → 挡施法但保留配额；回到 16 → 恢复可施。

## D-0099-04 · 证据作用域与断言密度

- 模型报告 `result` 由裸 `PASS` 改 **`MODEL_ONLY_PASS`**，新增 `acceptanceScope`、`nativeEntryPointsExercised=false`、`releaseEligible=false`、`resultMeaning`。
- 原生报告 `acceptanceScope=native_entry_points_directed`、`playerGameAcceptance=false`、`releaseEligible=false`、`boundary`、`notRun`。
- D 反例报告 `acceptanceScope=d_counterexample_reproduction_only`、`independentReviewer=false`、`reviewedCandidate` 与 `recheckTarget` 分列，并说明 PASS 只表示反例不再复现。
- 恒真／占位断言处理：`Id("OccultistClass")` 自比改为**独立 salted MD5 重算**；143 行与 6 Knacks 改为从 **A JSON 独立读取**比对；TypeId 常量与 `Ids.TypeId(n)` 交叉核对；canonical 过滤补真实 variant 正反例；`CotWRegistrationComplete` 补真实写入点与关闭负例。
- 断言数由 279 增至 **364**（模型层），另有原生层 80 条与 D 反例 5 条，三层分列不混算。

## D-0099-05 · 指纹覆盖全部可变字段

`Fingerprint()` 现覆盖 `ClassLevel / Intelligence / NextSequence / Slots / Quotas / Sources / Providers / PublishedLists / DlcAllowed / CotWRootsPublished / ForeignBookRoots / SameBookGrantSources / CotWRegistrationComplete / VariantRoots`，`Clone` / `CancelPreview` 同步。D-F03 本轮升级为**逐字段探针**：10 个可变字段各改一次，要求指纹全部变化，`uncovered=none`。另有「故意改未覆盖字段后取消预览必须被检出」的负例。

## D-0099-06 · canonical/variant 与注册门禁（部分闭环）

- `VariantRoots` 成为**可写入**映射，`AddVariantRoot` / `CanonicalOf` 真实解析；variant 不占第二份配额，`IsKnown` 按 canonical 判定。`KnownRootsOverride` 死字段已删除。D-F04 转 PASS（variant 注册 Ok、被候选排除、canonical 在列、经 variant 学习 Ok、配额记 canonical、单一来源、variant 视为已知）。Abjuration 目录本身无 variant 是正确的（Fire Shield 属 Evocation，后续包）。
- `CotWRegistrationComplete` 有唯一写入点 `SetCotWRegistrationComplete`，关闭时 CotW 根不进候选（负例已跑）。
- `Occultist0099Registration` 改为真实 32-hex GUID + owner-token 缓存，`ForeignDataTouched` 是**所有非 0099 条目的前后差分**。D-F05 转 PASS（注入篡改被检出；冲突路径与成功路径外部数据均不变）。
- **仍未达成**：SP08 合同要求真实 `BlueprintsCache` 缺供体／签名 GUID 冲突夹具与真实原子注册。当前只有模型层门禁正反例，故模型层记 `model_gate_only_native_cache_not_run`、原生层记 `native_gate_only_registration_not_run`，**不写成 PASS**。

## D-0099-07 · 指纹权威口径

改用**源码 SHA256** 为权威身份，DLL 哈希降级为构建产物记录。原 `AA9C1F8D…D5EB` 是 **TypeId 位宽修正前的中间产物**，与 `b6be98b` 源码不对应，予以撤回。本轮实证：仅一处注释更正就让 DLL 从 `EAC87ED1…4A86` 变为 `3F17C68F…1FF9`，而逻辑与断言结果不变——这正是 DLL 哈希不适合作验收身份的原因。运行器 `Run-Occultist0099Native.py` 已按新口径产出 `sourceSHA256` / `hostSourceSHA256`，DLL 哈希仅作 `candidateSHA256` 交叉核对。全值见 [B-fix-evidence.json](B-fix-evidence.json)。

## D-0099-08 · 接线前必修

- **休息身份**改用 `Kingmaker.Controllers.Rest.State.CampingState.OverallCampsCount`（`RestController.CampCoroutine` 在 `ApplyRest` 触发 `HandleUnitRest` **之前**自增一次），不再用 `GameTime.Ticks`。同一 camp 重复回调幂等，不同 camp 永不合并。原生 RESTN 用例已实测。
- **`OnActivate` 守卫**：`CharacterClass` 未赋值或等级 0 时拒绝建立 `B=0` 周期，等真实休息／获得职业后再建。
- **TypeId 常量**：5 个组件的 `[TypeId]` 引用 `Occultist0099Ids.*TypeId` 常量，不再硬编码 hex；模型宿主交叉核对常量与 `Ids.TypeId(n)`。
- **`SlicePublished` 成为被强制执行的门禁**：`CanRegister` 需同时满足发布开关与完整映射，不再是无人消费的装饰常量。

## D-0099-09 · 措辞更正

「18 条 Abjuration 法术根（含 6 Knacks）」→ **「18 条 Abjuration 根（13 Wrath + 4 CotW + 1 External）＋ 独立的 6 条 0 环 Knacks」**。A JSON `spellRoots.level == 0` 恰好 6 行，跨 5 个学派，仅 Resistance 属 Abjuration；生产 `Roots` 只有 1 条 `IsKnack=true`。数据本身一直正确（18/18、6/6 与 A 全量一致），只是表述错误。已更正生产注释与 B 文档；`WORK_STATUS.md`、`HANDOFF.md` 与 issue #15 评论同步。

## 本轮复跑与真实耗时

| 项目 | 结果 | 耗时 |
| --- | --- | --- |
| 主工程增量编译 / 重复编译 | 0 警告 / 0 错误 | 1.007 秒 / 0.973 秒 |
| 主工程全量重编译（首次 / 措辞更正后） | 0 警告 / 0 错误 | 4.508 秒 / 2.899 秒 |
| 原生宿主编译 | 0 警告 / 0 错误 | 1.116 秒 |
| 模型层定向检查 | MODEL_ONLY_PASS，364 / 0 失败 | 2.819 秒（断言 0.042 秒） |
| D 合同反例复跑 | REVIEW_PASS，5 检查 / 0 FAIL | 2.715 秒 |
| 原生入口定向检查 | NATIVE_DIRECTED_PASS，80 / 0 失败 | 3.976 秒（宿主 3.51 秒） |
| 包内类型与指纹核对 | 28/28 类型存在 | 0.035 秒 |

D 反例本轮共跑三次，全部如实记录：首次 1.249 秒（旧弱断言）→ 加强后 2.677 秒（**D-F02 FAIL**，探针夹具缺陷）→ 修正后 2.715 秒（全 PASS）。

**未运行**：游戏／UMM 启动、玩家实机验收、整套历史测试、共享职业/法书/成长注册、升级 UI 与重训控制器、存读档传输、真实 TabletopTweaks 等第三方程序集加载、打包与版本号（`build.ps1` 要求 PowerShell 7.4，本机 5.1）、安装包上传与下载核对、issue #15 关闭。

## 接手要点

1. **SP08 是唯一未达合同的门禁项**，需要真实 `BlueprintsCache` 夹具与真实原子注册，属接线阶段工作。
2. MF07 只跑通休息身份，Spell Power / armed 重入 / StickyTouch 仍未运行；MF11 只覆盖去重身份，其余仍未运行。
3. 组件仍未挂蓝图，`SlicePublished=false`。八校、四 Panoply、成长与 143 行私有法表齐套后，由同一负责人复核 PR #14、接线共享注册并原子发布。
4. 随后由**另一名执行者**做正式独立 D，重点复验 D-0099-01（原生入口）与 D-0099-02（去重身份）。不得据本轮结果发布 TEST 包或关闭 #15。
