# COTW-0099 · B 首切片（Abjuration）实现与证据

合同：`C-0099-20261115-r1`（已冻结，本轮未改）。基线：`939e04d` / 0.14.74 TEST，分支 `feat/cotw-0099-occultist`，A/C 提交 `eb20191`。认领 [issue #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15) 保持开放；按用户 2026-11-15 指令继续执行 B。单一生产整合负责人仍待维护者指定，D 独立复查未开始。

## 本轮范围

按合同「B 先以 Abjuration 的 Mind Barrier、Warding Talisman、资源操作和私有法术学习串起原生入口，首切片只供开发」执行**首切片的共享机制与 Abjuration 数据**。首切片为**开发态**：`Occultist0099Ids.SlicePublished = false`，没有注册职业／法书／成长蓝图，没有挂进 `BlueprintRegistration`／`ModuleCatalog`，玩家可见行为不变。合同要求八学派、四组合、成长和私有法表齐套后由同一负责人统一注册；PR #14 的共享注册／Harmony／构建变更仍在接线前复核范围，未动。

新增文件（均在同一分支）：

| 文件 | 内容 |
| --- | --- |
| `src/Occultist0099Rules.cs` | O-S01 权威状态机：每角色一周期，`G+ΣR+U+Q=B` 守恒；投资／Reset／Full Round Lock 快照／1:1 补充／学校与 Panoply 原子支付／执行身份去重／Repower Construct 通用支出／休息幂等／重训预算与来源清退／混职与多单位隔离；Warding Talisman 与 Mind Barrier 参数；Implement Mastery 的 L+4、DC+2 与书 CL 仍为 L。无游戏程序集依赖。 |
| `src/Occultist0099Spells.cs` | O-S02 首切片：18 条 Abjuration 法术根（含 6 Knacks）、私有环位 `1/4/7/10/13/16`、法器槽与 Extra Spells 顺序校验、(槽键, 环位) 配额与来源账本、候选过滤（本学派／本环／canonical／提供方与已发布列表／DLC／同书重复根）、候选耗尽留空位、预览取消与提交、同书多来源与外书隔离、Panoply 前置与成员失效、跨书 Psychic 边界、注册原子门禁。 |
| `src/Occultist0099Ids.cs` | 私有 salt GUID 空间（MD5 token→32 hex），不复用 A 中 Kingmaker 原作 GUID；32 位 TypeId 段；开发态切片标记与移交清单。 |
| `src/Occultist0099Native.cs` | 原生 Wrath 适配器：`UnitFactComponentDelegate<Occultist0099NativeData>` 事实组件，`IUnitRestHandler` 成功休息周期、读档／重附只读恢复、UI 预检与实际执行共用同一权威状态；私有法书标记组件。TypeId 均为 32 位 hex，未与现有包冲突。组件尚未挂到任何蓝图。 |
| `tests/Occultist0099SliceTests.csproj`、`tests/Occultist0099SliceProgram.cs` | 链接生产源码的 net9.0 隔离规则宿主，不引用游戏程序集、不启动游戏或 UMM。 |
| `docs/work-packages/COTW-0099/B-slice-report.json` | 本次机器可读断言报告（19 条用例状态、279 条断言、失败列表、耗时说明）。 |

关键语义按冻结合同取值：共鸣按锁定快照计算，学校点花完仍生效；通用点采用原作可执行的 **1:1 补充已花学派点**，并保留显式 1 点通用支出（Repower Construct）；Panoply 为纯投影，逐点取当前余额最高成员、平局按 A JSON 成员顺序，算完总向量再原子扣减，无第二次扣款；精通 H 优先付本校 Focus Power，Panoply 与书 CL 不受精通影响。

## 验收用例对照（C-occultist-cases.json，共 19 条）

状态说明：`model_assertions` = 用生产源码的隔离数值状态模型断言；`partial_model` = 仅切片能覆盖的部分；`not_run` = 原生入口／能力尚未实现或未运行，**不写成 PASS**。本次没有运行任何真实 Wrath 蓝图、资源逻辑、升级 UI、存档或实机路径。

| 用例 | 状态 | 说明 |
| --- | --- | --- |
| MF01–MF04 | `model_assertions` | 投资、锁前拒绝、锁定快照、Mind Barrier 扣 1 且共鸣仍在；1:1 补充、满池与锁定后 Invest/Reset/属性变更；R=0 无通用回退、重复事件零扣费；读档与临时 Int 不充值、同一休息事件幂等。 |
| MF05、MF06 | `model_assertions` | Repower Construct 1 点通用支出＋尸体生命周期、两次 Reset 守恒；Trappings 投影支付、平局顺序、cost-2 计划、重复与不可支付事务。 |
| MF08 | `model_assertions` | L=20／Int+5：Mind Barrier 有效等级 24、吸收 48、DC 27，他校 DC 25，书 CL 仍 20；H 优先、Panoply 不动 H、重训携带 T=1 并撤销旧精通贡献。 |
| MF09、MF10 | `model_assertions` | 预览丢弃逐字段恢复、Extra Spells 移除不动学派状态、锁定状态移除学校使 Q+3 并失效依赖组合、同日重训进出不充值；两单位与外书资源隔离、预算下限 0。 |
| MF07、MF11 | `not_run` | Spell Power 随施法付费、armed 重入、失效来源、StickyTouch 投递需要 Panoply 能力与原生施法事件链，留给后续学派包。 |
| SP01–SP04 | `model_assertions` | 法器顺序与第三同学派拒绝、1 环两根＋6 Knacks；4 级开 2 环、16 级 6 环只有一个候选且第二槽留空；预览取消、同书另一来源与 Wizard 来源互不影响；Panoply 前置、重复、成员失效失效派生配额。 |
| SP05 | `partial_model` | 核对 143 行总量、18 行切片、Abjuration 私有环位与开环等级；Hideous Laughter／Mass Daze／Lesser Animate Dead／Control Construct／Spirit-Bound Blade 的全表检查未运行。 |
| SP06 | `partial_model` | 提供方缺失／禁用／启用／发布列表／损坏子项与已保存根恢复，用 Cloak of Winds 验证；Fire Shield 冷热双变体共根未运行。 |
| SP07、SP08 | `model_assertions` | 私有书 thought/emotion 与 Psychic 专属机制边界、外书环位不变；缺供体／GUID 冲突不发布且外书数据完好，完整映射恰好一个职业与一个法书、重复完成不重复注册。 |

## 验证与耗时（实测）

| 项目 | 结果 | 耗时 |
| --- | --- | --- |
| 主工程编译（`dotnet build ... -c Release -p:WrathPath=<本地游戏>`） | 0 警告 / 0 错误；首次全量编译含新文件 | 3.863 秒 |
| 主工程复编译（无源码变更） | 0 警告 / 0 错误 | 1.600 秒 |
| 一次最小必要定向检查（隔离状态模型，`dotnet run` 含测试宿主编译） | **PASS，279 条断言，0 失败**，19 条用例均有明确状态 | 2.837 秒（宿主内断言 0.030 秒） |
| 包内 DLL 新类型核对 | 6/6 个新类型存在；候选 DLL SHA256 `AA9C1F8D6517452DDC59235A8465499A5E3610FBC5B18D7BABD7F94BE134D5EB` | 0.213 秒 |
| 打包、安装包上传、下载核对 | **未运行**：本机只有 Windows PowerShell 5.1，`build.ps1` 的 `#Requires -Version 7.4` 不满足；本轮为开发态首切片，没有新安装包或发布 | 未运行 |
| 游戏／隔离运行时定向、实机、存档、重训、提供方启用、HA/TTT | **未运行**，不写成 PASS | 未运行 |

隔离检查命令：`dotnet run --project tests/Occultist0099SliceTests.csproj -c Release -p:WrathPath='<本地游戏>' -- docs/work-packages/COTW-0099/B-slice-report.json`。报告明确写 `nativeWrathEntriesExercised=false`、`gameOrUmmLaunched=false`。本轮定向检查是合同允许的「数值状态模型补充」，**不是原生入口 PASS**。

## 未测边界与接手说明

- 原生 `AbilityResourceLogic`／激活能力／升级选择 UI／`UnitPartTemporarySpellsKnown` 账本尚未接线；适配器只提供权威状态与只读恢复入口，组件没有挂到蓝图，共享注册链未改。
- 七学派、四 Panoply 的实际 Focus Power、Implement Mastery、Repower Construct 蓝图、完整成长与 143 行私有法表、12 个可选供体的运行时启用检查均未完成；`SP05/SP06` 为部分模型覆盖，`MF07/MF11` 未运行。
- 真实实机升级、存读档、重训、控制器、DLC 授权与第三方提供方启用未验收；没有玩家验收结论。
- 下一步按完整学派分包，八校、四组合、成长和私有法表齐套后再由同一负责人复核 PR #14、接线共享注册并做原子发布；随后 D 独立复查。不要在本次开发态切片上冒充已发布或重复开发他人范围。
