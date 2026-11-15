# COTW-0099 Occultist · B 首切片 D 复查（维护者指定自查）

日期：2026-11-15。合同：`C-0099-20261115-r1`（未改）。候选：`b6be98b`，分支 `feat/cotw-0099-occultist`，A/C `eb20191`。认领 [issue #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15) 保持开放。分工与规格：[OCCULTIST.md](../OCCULT-BASE-CLASSES/OCCULTIST.md) · [C 用例](C-occultist-cases.json) · [B 交接](B-abjuration-slice.md)。

**结论：NEEDS_FIX。** 不得据 `b6be98b` 判定 B 首切片「验收通过」，不得发布 TEST 包，不得关闭 #15，不得把 279 条隔离断言当作玩家验收。开发态草稿可保留，但合同明列的原生入口与下列阻断项闭环前，B 首切片不算完成。

## 0 · 角色独立性声明（必读）

`OCCULTIST.md:25-27,33` 规定 D「更换一名旗舰档」「每项保持单一实现负责人，D 换人」。用户 2026-11-15 23:59 明确指令「你自行复查D」，指定由**本会话（同时是 `b6be98b` 的 B 实现者）**执行。按 `AGENTS.md:3` 用户最新要求优先，本次复查照办，但：

- 本轮是**维护者指定的自查（self-review）**，不满足角色独立性，**不是独立 D 签字**；
- 下列技术结论、命令、指纹与失败断言可复核，但正式 D 验收仍需维护者另指派执行者，重点复验 D-0099-01、D-0099-02；
- 生产源码只读（`git diff HEAD -- src/ tests/ docs/.../B-slice-report.json` 为空），本轮新增的仅是 `tests/Occultist0099DReview*` 与 `docs/work-packages/COTW-0099/D-review-*`。

## 1 · 审查对象与证据

| 项目 | 结果 | 耗时 |
| --- | --- | --- |
| 主工程编译 `dotnet build CallOfTheWild.Wrath.Prototype.csproj -c Release -p:WrathPath=<本地游戏> --nologo -v:q` | exit 0，**0 警告 / 0 错误** | 1.545 秒 |
| 重复编译（验证 DLL 确定性） | exit 0，DLL SHA256 不变 | 0.903 秒 |
| B 隔离定向检查复现 `dotnet run --project tests/Occultist0099SliceTests.csproj -c Release -p:WrathPath=<本地游戏> -- docs/work-packages/COTW-0099/D-review-evidence.json` | exit 0，**279 条 PASS / 0 失败**，与 `B-slice-report.json` 的断言数、用例状态、`nativeWrathEntriesExercised=false` 完全一致 | 2.259 秒 |
| D 合同反例复现 `dotnet run --project tests/Occultist0099DReviewTests.csproj -c Release -p:WrathPath=<本地游戏> -- docs/work-packages/COTW-0099/D-review-assertions.json` | exit 0，**5 FAIL / 5 检查，NEEDS_FIX** | 1.249 秒（首跑 1.487 秒，因过宽的 D-F02 判据修正后重跑） |
| DLL 类型与 SHA256 核对 | 8/8 个 0099 类型存在 | 0.213 秒 |
| 全仓 TypeId 审计 | 365 个全为 32 位 hex、无重复；0099 两条与 `Ids.TypeId(1)/(2)` 计算值一致 | 0.127 秒 |
| 打包、安装包上传、下载核对 | **未运行**：本机仅 Windows PowerShell 5.1，`build.ps1` 要求 7.4；本轮无新包 | 未运行 |
| 游戏／UMM／原生 Wrath 入口、存读档、重训、提供方启用、实机、HA/TTT | **未运行** | 未运行 |

命令、逐步耗时、源码／DLL／文档 SHA256、TypeId 与资产核对明细：[D-review-timings.json](D-review-timings.json)；失败断言原件：[D-review-assertions.json](D-review-assertions.json)；B 断言复现：[D-review-evidence.json](D-review-evidence.json)。关键指纹：DLL `E7F5E557CF73C82F50077C699D333C83C3D48947F8D6FEEB77303A7ADC797AB7`；`Rules.cs` `AE39E296…6F0`；`Spells.cs` `306ACB52…4AA`；`Native.cs` `F32784BC…E8A`；`Ids.cs` `AE0FAB70…BD5`（全值见 timings）。

静态核对（不依赖生产常量）：`A-occultist-assets.json` 共 143 行、Abjuration 18 行、0 环 6 行；生产 `Roots` 展开常量后 18 条 `(GUID, 环位)` 与 A **全量一致**，`Knacks` 6 个 GUID 与 A 的 6 条 0 环**全量一致**。`BlueprintRegistration.cs` / `ModuleCatalog.cs` 无 0099 注册（`ModuleCatalog.cs:39` 仅「Not registered」文案），`SlicePublished=false` 无生产消费者，**玩家可见行为不变**属实。PR #14 仍 OPEN 且改动共享注册／Harmony／构建文件，接线前冲突复核未开始。

## 2 · 必须修复（阻断验收）

### D-0099-01 / P1：原生入口零接线，C 用例入口要求未满足

`C-occultist-cases.json:4` 写明「Run through the specified native Wrath paths in B/D. Sample numeric state models **supplement** the native tests.」；`OCCULTIST.md:22` 要求首切片「验收须经过原生资源支付或法术学习入口」。实际：`Occultist0099Native.cs:27,147` 两个组件全仓只有定义、无蓝图挂载；`src/Occultist0099*.cs` 无 `AbilityResourceLogic`、激活能力、`RuleCastSpell`、升级 UI、`BlueprintSpellbook`、`HarmonyPatch`、`RegisterAll`（仅 `Ids.cs:66-75` 的字符串清单）；`tests/Occultist0099SliceTests.csproj:10-14` 只链接 Rules/Spells/Ids，**不编译 Native.cs**。`B-slice-report.json:6` 自记 `nativeWrathEntriesExercised=false`。

因此 17 条记 `model_assertions` 的用例按合同只能算「模型补充／入口未跑」，不能算验收通过；B 首切片的完成条件（真实资源支付或法术学习入口）尚未达成。最小闭环：至少串起一条 `AbilityResourceLogic` 真实支付与一条原生学习选择，跑 MF01/SP01 的原生正反例，其余按实际状态记 `not_run`。

### D-0099-02 / P1：RemoveClass 丢失去重身份，同一事件可二次扣费

`Rules.cs:646` 在 `RemoveClass` 里 `ChargedExecutions.Clear()`，`Rules.cs:650-657` 的 `ReacquireClass` 不恢复；支付去重集合见 `Rules.cs:113,427`。D 反例**已实际执行**（`D-review-assertions.json` D-F01）：`Cycle(8,2)` → 投资 4 → Lock → `PaySchool(...,"same-execution")` = Ok → RemoveClass → ReacquireClass → 同一 id 再付 = **Ok，Spent=2**。守恒式仍成立，故 B 的 279 条断言测不出来。违反 `C:23`「存档只保存…必要的去重身份／每个事件最多一次」、`C:22`「防止同日重新获得时串来源」与 MF11「Same execution identity is charged at most once」。当前组件未挂蓝图故未触发，接线后必然暴露。最小闭环：删除职业时保留必要去重身份（或按周期键归档），重取后同一 id 第二次支付为拒绝、余额与 U 不变；另补一条不同事件 id 仍可支付的正例。

### D-0099-03 / P1：Int ≥ 10 + 环位 门槛完全缺失

`C:31` 冻结「学和施法门槛 Int≥10+环位」，SP02 的 setup 明确 `Int≥16`（`C-occultist-cases.json:215`）。`Spells.cs:133-147` 无 Int 字段；`Create:197-209` 只接 `classLevel`；`MaxCircleForLevel:89-98`、`OpenCircles:218-227`、`Candidates:318-332`、`Learn:334-356` 全部只看等级，无任何 Int 校验。D-F02 已反例：`ClassLevel=4` 时 2 环照样开、照样可学，与智力无关。`B-slice-report.json:21` 却把 SP02 记 `model_assertions`。最小闭环：低 Int 角色 2–6 环不开、不可学、不可施；达标 Int 恢复；临时 Int 波动不改变已建立配额。

## 3 · 证据口径问题（P2，须修正后重发报告）

### D-0099-04：`result: "PASS"` 无限定 + 恒真／占位断言拉高证据密度

`B-slice-report.json:8,9,33`（279 / failures[] / `result=PASS`）没有任何字段写明「原生入口零执行、不可作为验收或发布依据」；限定语只在 `B-abjuration-slice.md:24,49`。`Program.cs:781-782` 把 `Id("OccultistClass")` 与自身比较、`786-789` 用 const 对字面量、`724` 用同义谓词自证、`661/663` 用 `Spells.cs:111` 的硬编码 `143` 自证、`:535` 只验 Knacks 长度。279 计数准确，但含恒真／占位；有效证据密度低于该数字。要求把机器可读结果改为带作用域的 `MODEL_ONLY_PASS`，补 `acceptanceScope`、`nativeEntryPointsExercised=false`、`releaseEligible=false`，并给恒真断言补独立正反例或删除。

### D-0099-05：`SpellState.Fingerprint` 漏 7 个可变字段，逐字段恢复断言不可证

`Spells.cs:149-162` 只覆盖 `ClassLevel/NextSequence/Slots/Quotas/Sources/ForeignBookRoots`，漏 `Providers/PublishedLists/DlcAllowed/CotWRootsPublished/SameBookGrantSources/CotWRegistrationComplete/KnownRootsOverride`（`:140-147`）。`CancelPreview:439-459` 的生产恢复逻辑本身逐字段正确，但 `Program.cs:431`（MF09 byte-for-byte）与 `:606`（SP03 restores quota and data）只比指纹；SP03 预览恰调 `RemoveSameBookGrant` 改 `SameBookGrantSources`，即使该字段泄漏断言也通过。D-F03 已反例：`SetProvider` 改启用状态后指纹完全相同。最小闭环：指纹覆盖全部可变字段，或预览断言逐字段比对，并加一条「故意改未覆盖字段后取消预览必须被检出」的负例。

### D-0099-06：canonical／variant 与注册门禁是门面，SP08 证据对象不符

`Spells.cs:74-85` 令每条根 `CanonicalGuid=Guid`、`IsVariant=false`，使 `Candidates:328` 的 `r.Guid==r.CanonicalGuid` 恒真；`:146` `CotWRegistrationComplete=true` 无写入点；`:147` `KnownRootsOverride` 是死字段。D-F04 已反例：18 条根无任何变体数据，`B-abjuration-slice.md:14`「候选过滤 canonical」无负例。`Spells.cs:506-538` 的 `Occultist0099Registration` 是接受 `"core-a"/"core-b"` 字符串的玩具对象，`ForeignDataTouched` 所有路径恒 false（D-F05 反例），与 `C-occultist-cases.json` SP08 要求的真实缓存缺供体／签名 GUID 冲突夹具无关，却被 `B-slice-report.json:27` 记 `model_assertions`、`B-abjuration-slice.md:36` 写成「注册原子门禁」。最小闭环：接线后以真实 Blueprint GUID 与缓存夹具跑 SP08，缺供体／冲突时外部数据不变、注入供体后恰好注册一次；当前应把该条状态降为 `not_run`。

### D-0099-07：B 文档记录的 DLL SHA256 不可复现

`B-abjuration-slice.md:45` 记 `AA9C1F8D…D5EB`。D 对同一提交源码（`git diff HEAD` 为空）编译得 `E7F5E557…AB7`；`csproj` 有 `<Deterministic>true</Deterministic>`，重复编译 hash 稳定，全仓 DLL 副本也未找到 `AA9C1F8D…`。请 B 说明该指纹来源（是否修 TypeId 前的中间产物／其他路径），或改用源码 SHA256 作为权威指纹；本轮以 `D-review-timings.json` 的源码指纹为准。

### D-0099-08：接线前必修（当前未触发）

- `Native.cs:57-67` 用 `GameTime.Ticks` 拼 `RestEventId`，异常时 ticks=0；`HandleUnitRest:94-100` 每次回调都调 `BeginCycle`，而 `Rules.cs:239-243` 只按该字符串判幂等。同一休息多次回调跨过时间推进会二次重建周期，ticks 恒 0 时不同休息反而被吞。违反 `C:21`、MF04。应改用真正的休息事件身份。
- `Native.cs:30-32,46,73` 的 `CharacterClass` 未赋值时 `ClassLevel()=0`，`OnActivate` 会以 `B=0` 建立周期（`Rules.cs:250,258`），此后全部投资被拒。接线必须强制校验该引用，违反 `C:9`。
- `SlicePublished`（`Ids.cs:16`）不是有效开关：全仓无生产消费者，未发布靠的是「根本没有注册代码」；`Native.cs:26,146` 硬编码 TypeId，而 `Ids.TypeId()` 从未被生产代码调用（仅 `Program.cs:790`），建议接线时改为常量引用，避免后续学派包漂移。

### D-0099-09 / P3：文档口径不准

`B-abjuration-slice.md:14` 与 issue #15 评论写「18 条 Abjuration 法术根（含 6 Knacks）」。A 资产的 6 条 0 环跨 5 个学派（仅 Resistance 属 Abjuration），生产 `Knacks`（`Spells.cs:40-48`）是独立数组，`Roots` 只有 1 条 `knack=true`（`:52`）。**数据本身正确**（18/18、6/6 全量一致），只是「含 6 Knacks」表述错误，应改为「18 条 Abjuration 根 + 独立 6 Knacks」。

## 4 · 已核对通过的部分

- `G+ΣR+U+Q=B` 在 Invest / Reset / Lock / PaySchool / PayPanoply / Replenish / Repower / BeginCycle / RespecBudgetDown / RespecRemoveSchool 全路径守恒（`Rules.cs:300-337,358-374,434-489,528-529,594-639`）；预算下调按 `max(0,新上限−U−Q)` 先 G 后各校 R 转 Q，顺序与 `C:22` 一致。
- **无双扣**：Panoply 用 working 副本逐点取当前 R 最大者、平局按成员顺序，算完总向量一次性写回（`Rules.cs:473-489`），无第二次扣款；学校支付 H 优先再补 R、整笔不足原子拒绝（`Rules.cs:434-448`）。
- 重训预览深拷贝与逐字段恢复的生产逻辑正确（`Spells.cs:164-191,439-459`）；失去学校使依赖 Panoply 失效并 Q+3（`Rules.cs:594-610`）；同日重取不充值（`Rules.cs:239-249,650-657`，去重身份除外，见 D-0099-02）。
- 读档／重附只读恢复（`Native.cs:80-90`）；TypeId 约定、私有 GUID 空间、注册链隔离、Abjuration 18 根与 6 Knacks 数据、编译零警告零错误、DLL 8/8 类型，均通过。

## 5 · 未测边界与交接

原生资源支付、升级 UI、真实法术学习与来源撤销、存读档、重训、提供方启用／晚到、Fire Shield 冷热双变体、MF07／MF11、SP05／SP06 全表、实机／HA／TTT、PR #14 共享注册／Harmony／构建接线冲突复核，本轮**全部未运行**，不写成 PASS。D 只跑了 0099 相关的新增编译与两次隔离定向检查，未扫全历史、未启动游戏或 UMM、未改生产源码、未生成或上传任何安装包。

修复由 B 单一负责人（仍待维护者指定）执行：先闭环 D-0099-01～03，再按 D-0099-04～08 更正证据与接线前必修项；只针对实际更改跑一次最小必要定向检查，再编译、打包与包内 DLL 核对。齐套后复核 PR #14、原子注册，然后由**另一名执行者**做正式独立 D 并决定是否 TEST 交付。`WORK_STATUS.md`、`docs/agent-handoff/HANDOFF.md` 与 issue #15 已如实记录本轮自查结论，认领保持开放。根目录 `D:\CodexProjects\CotW-Wrath\` 不是 Git 仓库，其 `WORK_STATUS.md`／`HANDOFF.md` 为本地镜像副本，同步更新但不构成分支交付。
