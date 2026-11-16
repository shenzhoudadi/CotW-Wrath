# COTW-0099 · B 首切片（Abjuration）实现与证据

合同：`C-0099-20261115-r1`（已冻结，本轮未改）。基线：`939e04d` / 0.14.74 TEST，分支 `feat/cotw-0099-occultist`，A/C 提交 `eb20191`，B 首切片 `b6be98b`，D 自查 `8896a7f`。认领 [issue #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15) 保持开放。单一生产整合负责人仍待维护者指定；**正式独立 D 仍未开始**（`8896a7f` 是维护者指定的自查，不满足分工文档「D 换人」）。

本文件已按 D 自查的 9 项发现更新到**修复轮之后的状态**。逐条闭环记录见 [B-fix-round.md](B-fix-round.md)，三层机器可读证据见 [B-fix-evidence.json](B-fix-evidence.json)。

## 本轮范围

按合同「B 先以 Abjuration 的 Mind Barrier、Warding Talisman、资源操作和私有法术学习串起原生入口，首切片只供开发」执行**首切片的共享机制与 Abjuration 数据**，并在修复轮把三条真实原生入口接通。首切片仍为**开发态**：`Occultist0099Ids.SlicePublished = false`，没有注册职业／法书／成长蓝图，没有挂进 `BlueprintRegistration`／`ModuleCatalog`，玩家可见行为不变。合同要求八学派、四组合、成长和私有法表齐套后由同一负责人统一注册；PR #14 的共享注册／Harmony／构建变更仍在接线前复核范围，未动。

生产文件（均在同一分支）：

| 文件 | 内容 |
| --- | --- |
| `src/Occultist0099Rules.cs` | O-S01 权威状态机：每角色一周期，`G+ΣR+U+Q=B` 守恒；投资／Reset／Full Round Lock 快照／1:1 补充／学校与 Panoply 原子支付／执行身份去重／Repower Construct 通用支出／休息幂等／重训预算与来源清退／混职与多单位隔离；Warding Talisman 与 Mind Barrier 参数；Implement Mastery 的 L+4、DC+2 与书 CL 仍为 L。**周期作用域去重身份只在 `BeginCycle` 清空，`RemoveClass` 保留**（D-0099-02）。无游戏程序集依赖。 |
| `src/Occultist0099Spells.cs` | O-S02 首切片：**18 条 Abjuration 法术根（13 Wrath + 4 CotW + 1 External）＋ 独立的 6 条 0 环 Knacks**（仅 Resistance 属 Abjuration，其余 5 条属他校，不计入这 18 条；D-0099-09 措辞更正）、私有环位 `1/4/7/10/13/16`、**Int ≥ 10 + 环位 的学习与施法门槛**（D-0099-03）、法器槽与 Extra Spells 顺序校验、(槽键, 环位) 配额与来源账本、候选过滤（本学派／本环／canonical／提供方与已发布列表／DLC／同书重复根）、**可写入的 canonical/variant 身份**（D-0099-06）、候选耗尽留空位、**覆盖全部可变字段的预览指纹**（D-0099-05）、预览取消与提交、同书多来源与外书隔离、Panoply 前置与成员失效、跨书 Psychic 边界、**基于真实 GUID 与 owner-token 缓存差分的注册原子门禁**（D-0099-06）。 |
| `src/Occultist0099Ids.cs` | 私有 salt GUID 空间（MD5 token→32 hex），不复用 A 中 Kingmaker 原作 GUID；32 位 TypeId 段与 5 个 TypeId 常量（组件 `[TypeId]` 引用常量，不再硬编码 hex）；`SlicePublished` 作为**被强制执行的注册门禁**（`CanRegister` 需同时满足发布开关与完整映射，D-0099-08）。 |
| `src/Occultist0099Native.cs` | 原生 Wrath 适配器：`UnitFactComponentDelegate<Occultist0099NativeData>` 事实组件，`IUnitRestHandler`／`IRestFinishedHandler` 成功休息周期、读档／重附只读恢复、UI 预检与实际执行共用同一权威状态；私有法书标记组件。**休息身份改用 `CampingState.OverallCampsCount`**（不再用 `GameTime.Ticks`）；`CharacterClass` 未赋值或等级 0 时 `OnActivate` 拒绝建立 `B=0` 周期（D-0099-08）。 |
| `src/Occultist0099NativeWiring.cs` | **D-0099-01 闭环，本轮新增**：三条真实原生入口。① `Occultist0099FocusResourceLogic : AbilityResourceLogic`，原生施法管线经 `BlueprintAbility.GetComponent<AbilityResourceLogic>()` → `AbilityCastRateUtils.SpendResources` → `Spend` 抵达，UI 预检（`IsAbilityRestrictionPassed`）与实际扣款走同一权威逻辑、共享同一 `AbilityData.UniqueId`；② `Occultist0099LearnGate` 经两条 Harmony 桥接到 `SelectSpell.Check` 与 `Spellbook.AddKnown`，拒绝的选择不会到达 `SpellSelectionData.SpendSlot`／`AddKnown`；③ `Occultist0099SpellbookFact0099` 在休息与升级时从真实 `ClassData.Level` 与真实 Intelligence 重同步。原生资源只作**显示镜像**（`AddAbilityResources(RestoreAmount=false)` + 成功交易后 `SyncResourceMirror()`），镜像从不被本组件扣款。 |

测试与证据文件：

| 文件 | 内容 |
| --- | --- |
| `tests/Occultist0099SliceTests.csproj`、`tests/Occultist0099SliceProgram.cs` | 链接生产源码的 net9.0 隔离规则宿主，不引用游戏程序集、不启动游戏或 UMM。**模型补充层**。 |
| `tests/Occultist0099NativeTests.csproj`、`tests/Occultist0099NativeProgram.cs`、`tests/Run-Occultist0099Native.py` | **本轮新增**：net472 离线原生宿主 + 运行器。引用真实 Wrath `Managed` 程序集，经 `EngineLauncher.exe` 与 publicized engine 运行；不启动游戏、不启动 UMM、不进共享注册链。**原生入口层**。 |
| `tests/Occultist0099DReviewTests.csproj`、`tests/Occultist0099DReviewProgram.cs` | D 合同反例宿主（5 条 D-F01～F05）。本轮把 D-F02 从「字段存在」升级为 Int 门槛行为正反例，D-F03 从只探 2 个字段升级为探全部 10 个可变字段。 |
| `docs/work-packages/COTW-0099/B-slice-report.json` | 模型层机器可读报告（19 条用例状态、364 条断言、失败列表、耗时、作用域限定）。 |
| `tests/occultist0099-native-report.json`、`tests/occultist0099-native-result.json` | 原生层机器可读报告与运行记录（80 条断言、19 条用例原生状态、夹具边界、源码／环境 SHA256、真实耗时）。 |
| `docs/work-packages/COTW-0099/B-fix-evidence.json` | 三层证据汇总、权威源码指纹、逐步耗时与未运行清单。 |

关键语义按冻结合同取值：共鸣按锁定快照计算，学校点花完仍生效；通用点采用原作可执行的 **1:1 补充已花学派点**，并保留显式 1 点通用支出（Repower Construct）；Panoply 为纯投影，逐点取当前余额最高成员、平局按 A JSON 成员顺序，算完总向量再原子扣减，无第二次扣款；精通 H 优先付本校 Focus Power，Panoply 与书 CL 不受精通影响。

## 验收用例对照（C-occultist-cases.json，共 19 条）

状态说明：`native_*_pass` = 经真实 Wrath 原生入口跑通的正反例；`model_assertions` = 用生产源码的隔离数值状态模型断言（**合同定义的补充，不是入口验收**）；`partial_model` = 仅切片能覆盖的部分；`not_run` = 未运行，**不写成 PASS**。本轮没有启动游戏或 UMM，没有跑升级 UI、存读档、重训控制器或整套历史测试。

| 用例 | 原生层状态 | 模型层状态 | 说明 |
| --- | --- | --- | --- |
| MF01 | `native_ability_resource_logic_pass` | `model_assertions` | 真实 `AbilityResourceLogic` 支付链：锁前拒绝、锁定后扣 1、同一 `UniqueId` 重复执行零扣费、UI 预检与扣款一致。 |
| MF02–MF04 | `not_run_native` | `model_assertions` | 1:1 补充、满池与锁定后 Invest/Reset/属性变更；R=0 无通用回退；读档与临时 Int 不充值、同一休息事件幂等。 |
| MF05、MF06 | `not_run_native` | `model_assertions` | Repower Construct 1 点通用支出＋尸体生命周期、两次 Reset 守恒；Trappings 投影支付、平局顺序、cost-2 计划、重复与不可支付事务。 |
| MF07 | `native_rest_camp_identity_pass`（仅休息身份部分） | `not_run` | 真实 `RestController.ApplyRest` → `IUnitRestHandler` → `CampingState.OverallCampsCount`：同一 camp 重复回调幂等、不同 camp 不合并、新周期重新锁定后仍能支付。**Spell Power 随施法付费、armed 重入、失效来源、StickyTouch 投递仍未运行**。 |
| MF08 | `not_run_native` | `model_assertions` | L=20／Int+5：Mind Barrier 有效等级 24、吸收 48、DC 27，他校 DC 25，书 CL 仍 20；H 优先、Panoply 不动 H、重训携带 T=1 并撤销旧精通贡献。 |
| MF09、MF10 | `not_run_native` | `model_assertions` | 预览丢弃逐字段恢复（指纹已覆盖全部 10 个可变字段）、Extra Spells 移除不动学派状态、锁定状态移除学校使 Q+3 并失效依赖组合、同日重训进出不充值；两单位与外书资源隔离、预算下限 0。 |
| MF11 | `not_run_native` | `partial_model_dedup_only_reentry_stickytouch_not_run` | **部分覆盖**：「同一执行身份最多扣费一次」已有正反例（`RemoveClass` → `ReacquireClass` 后同 id 被拒 `RejectedDuplicate`、Spent=1，不同 id 仍可支付，新周期才清空），D-F01 反例已闭环。**armed 重入、失效 Panoply 成员、StickyTouch 投递仍未运行**，不算 MF11 全通过。 |
| SP01 | `native_select_spell_pass` | `model_assertions` | 真实 `SelectSpell.Check` / `Spellbook.AddKnown`：达标选择通过并落账，超额／异学派／重复根被拒且不消耗槽位。 |
| SP02 | `native_int_gate_pass` | `model_assertions` | 真实 Intelligence 统计驱动的 `Int ≥ 10 + 环位` 门槛：低 Int 不开环不可学不可施，达标恢复，临时 Int 波动不删除已建立配额。 |
| SP03、SP04 | `not_run_native` | `model_assertions` | 预览取消、同书另一来源与 Wizard 来源互不影响；Panoply 前置、重复、成员失效失效派生配额。 |
| SP05 | `not_run_native` | `partial_model` | 从 A JSON **独立**核对 143 行总量、18 行切片、6 条 0 环 Knacks、Abjuration 私有环位与开环等级；Hideous Laughter／Mass Daze／Lesser Animate Dead／Control Construct／Spirit-Bound Blade 的全表检查未运行。 |
| SP06 | `not_run_native` | `partial_model` | 提供方缺失／禁用／启用／发布列表／损坏子项与已保存根恢复，用 Cloak of Winds 验证；Fire Shield 冷热双变体共根未运行（variant 机制本身已有正反例）。 |
| SP07 | `not_run_native` | `model_assertions` | 私有书 thought/emotion 与 Psychic 专属机制边界、外书环位不变。 |
| SP08 | `native_gate_only_registration_not_run` | `model_gate_only_native_cache_not_run` | **按 D-0099-06 降级**：门禁已改为真实 GUID + owner-token 缓存差分（篡改外部条目可被检出，冲突／成功路径外部数据不变），但**没有跑真实 `BlueprintsCache` 缺供体／签名 GUID 冲突夹具**，也没有真实注册。不写成 PASS。 |

## 验证与耗时（本轮实测）

命令统一带 `-p:WrathPath="D:/steam/steamapps/common/Pathfinder Second Adventure"`。

| 项目 | 结果 | 耗时 |
| --- | --- | --- |
| 主工程增量编译 | 0 警告 / 0 错误 | 1.007 秒 |
| 主工程重复编译（无源码变更） | 0 警告 / 0 错误 | 0.973 秒 |
| 主工程全量重编译 `-t:Rebuild`（首次，含全部 0099 新文件） | 0 警告 / 0 错误 | 4.508 秒 |
| 原生宿主编译 `tests/Occultist0099NativeTests.csproj` | 0 警告 / 0 错误 | 1.116 秒 |
| 主工程全量重编译 `-t:Rebuild`（措辞更正后，**指纹权威构建**） | 0 警告 / 0 错误 | 2.899 秒 |
| 模型层定向检查 `dotnet run --project tests/Occultist0099SliceTests.csproj`（含宿主编译） | **MODEL_ONLY_PASS，364 条断言，0 失败** | 2.819 秒（宿主内断言 0.042 秒） |
| D 合同反例复跑 `dotnet run --project tests/Occultist0099DReviewTests.csproj`（含宿主编译） | **REVIEW_PASS，5 检查 / 0 FAIL**（D-F01～F05 全部不再复现） | 2.715 秒 |
| **原生入口定向检查** `python tests/Run-Occultist0099Native.py` | **NATIVE_DIRECTED_PASS，80 条断言，0 失败**；`hostLoggedExceptions=0`、`skippedArchiveComponents=0`、`productionBridgesInstalled=2` | 3.976 秒（宿主进程本身 3.51 秒） |
| 包内类型与指纹核对 | 28/28 个 `Occultist0099*` 类型存在 | 0.035 秒 |
| 打包、版本号、安装包上传、下载核对 | **未运行**：本机只有 Windows PowerShell 5.1，`build.ps1` 的 `#Requires -Version 7.4` 不满足；本轮为开发态切片，没有新安装包或发布 | 未运行 |
| 游戏／UMM 启动、实机、升级 UI、存读档、重训控制器、真实第三方提供方程序集加载、整套历史测试 | **未运行**，不写成 PASS | 未运行 |

### 权威指纹口径（D-0099-07 闭环）

**以源码 SHA256 为权威身份**，DLL 哈希只作构建产物记录。原因：DLL 哈希随工具链与中间产物变化，本轮同一份逻辑仅因注释更正就从 `EAC87ED1…4A86` 变为 `3F17C68F…1FF9`，用它作验收身份不可靠。原 `B-abjuration-slice.md` 记录的 `AA9C1F8D…D5EB` 是 **TypeId 位宽修正前的中间产物**，与 `b6be98b` 源码不对应，**予以撤回**。

| 源码 | SHA256 |
| --- | --- |
| `src/Occultist0099Ids.cs` | `476E41F20404C1147BCCE5CC41B13AF6375CF8F8081A2AD333820A7480FE0716` |
| `src/Occultist0099Native.cs` | `5EBDF9813C2501E47A366830927AE0DD8B239840343DC262B950D10077F269F3` |
| `src/Occultist0099NativeWiring.cs` | `40644DB4C38EC055EFA8AF6B15383DCB71A83446FB7A1DCCCFDDCFEB31EE6E32` |
| `src/Occultist0099Rules.cs` | `872B7F6DCD859B6FE3F73834F30652A4F8F9D3AD73C44D5FBAFB26FB9A528F02` |
| `src/Occultist0099Spells.cs` | `DC644CA5A7A127F4BC96F9A1A0E4159CC9F5C5DA91D565C5CDECF45CE165EED8` |

构建产物 DLL：`3F17C68F09E97B3EF91BE2CD52DBE3BD2131E2A8F871AA493E9DFF9EC2D81FF9`（3 276 800 字节），与原生运行器独立记录的 `candidateSHA256` 一致。环境指纹（Harmony `DE50CD61…9FD5`、游戏程序集、engine、A 证据、原生夹具）与三个宿主源码指纹见 [B-fix-evidence.json](B-fix-evidence.json)。

### 原生宿主夹具边界（如实披露）

18 条 Abjuration 根中：

- **13 条 Wrath 源根**来自已审阅的原生档案夹具 `spirit-guide-0060-native-ext.json`，经**真实 `BlueprintsCache` 与原生多态组件反序列化器**加载；
- **4 条 CotW 根**由本仓库**真实生产 builder**（`PsychicWardingSpells` / `PsychicGlobeSpells` / `PsychicSupportSpells.Cloak`）针对真实档案供体构建，仅 `PsychicOriginalIcons.Load` 被替换（`ImageConversion` 是 Unity 原生调用）；
- **1 条 External 根**（`CloakOfWindsAbility`，TabletopTweaks-Base）按冻结 A 身份 host-stage，带真实 Abjuration `SpellComponent` 与真实可执行效果路径；**真实第三方提供方程序集未加载**，其提供方门禁只经权威状态验证，未经真实第三方蓝图验证。

原生真实覆盖：`BlueprintsCache` 与多态反序列化、`EntityFactsManager`/`Feature`/`UnitFactComponentDelegate` 激活与事件上下文、`AbilityData` 构造与 `UniqueId`、`AbilityCastRateUtils.SpendResources`、`AbilityResourceLogic` 子类分派、`UnitAbilityResourceCollection`、`SelectSpell.Check/Apply`、`SpellSelectionData` 槽位记账、`Spellbook.AddKnown/IsKnown`、`RestController.ApplyRest` 的 `RemoveOnRest` 循环与 `IUnitRestHandler` 分派、`CampingState.OverallCampsCount`。

边界替代：构造的 `Game`/`Player`/unit 代理、无 View 的场景服务、固定 `TimeController` 时钟、`LogChannel` 捕获，以及 `ApplyRest` 内跳过 `UnitDescriptor.Replenish`（它会恢复所有无关 descriptor 资源）。

## 未测边界与接手说明

- 七学派、四 Panoply 的实际 Focus Power、Implement Mastery、Repower Construct 蓝图、完整成长与 143 行私有法表、12 个可选供体的运行时启用检查均未完成；`SP05/SP06` 为部分模型覆盖，`MF07` 仅休息身份部分跑通，`MF11` 原生事件链未跑。
- **SP08 仍未达合同要求**：需要真实 `BlueprintsCache` 缺供体／签名 GUID 冲突夹具与真实原子注册，当前只有模型层门禁正反例。
- 升级 UI／VM 层、完整休息协程、存读档传输、重训控制器、DLC 授权与真实第三方提供方启用未验收；**没有玩家验收结论**。
- 组件仍未挂到任何蓝图，共享注册链未改，`SlicePublished=false`。下一步按完整学派分包，八校、四组合、成长和私有法表齐套后再由同一负责人复核 PR #14、接线共享注册并做原子发布；随后由**另一名执行者**做正式独立 D。不要在本次开发态切片上冒充已发布或重复开发他人范围。
