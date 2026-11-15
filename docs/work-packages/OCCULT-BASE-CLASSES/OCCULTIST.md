# COTW-0099 Occultist · 基础职业

状态为 **A/C 已完成，生产实现待 B 开工**。基线 `939e04d543875ff28314b808dcc996d16227acc3` / 0.14.74 TEST。工作分支 `feat/cotw-0099-occultist`。范围是 Intelligence 六环自发念力施法、法器学派与 Panoply、Mental Focus 与职业本体进度。COTW-0056 同名 Arcanist 变体、COTW-0100–0105 与 Relic Hunter（0140）沿用原状态。 [施工认领 #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15)

[A 盘点](../COTW-0099/A-occultist-map.md) · [A 根和供体](../COTW-0099/A-occultist-assets.json) · [C 冻结合同](../COTW-0099/C-occultist-contract.md) · [C 待执行用例](../COTW-0099/C-occultist-cases.json)。B 首切片为 Abjuration，单一整合负责人由维护者指定。

每个角色开始先读本页与 `AGENTS.md`。阶段原件写入本机 `C:/Users/xuenc/agent-tools/occultist-next/`，保留来源路径、GUID 与失败原件。A 和 C 可读原作、当前仓库及本机合法游戏元数据，不提交第三方程序集或原生蓝图。B 不启动游戏或 UMM，不跑整套测试。D 以新增编译、离线注册和本职业定向检查为准，实机存读档／重训界面等未测边界如实列明。

## A · 依赖、法器与能力图

**Gemini 3.8 Flash。** 读原作 `Classes/Occultist.cs`、`Classes/Implements/ImplementsEngine.cs`、`NewMechanics/ImplementMechanics.cs`，以及主线 `src/PsychicClass.cs`、`PsychicCasting.cs`、`PsychicSpellIntegration.cs`、`PsychicSharedSpellCatalog.cs`。输出 `A-occultist-map.md` 与 `A-occultist-assets.json`。逐项列出等级入口、学派、法术根及实际执行效果，标记原生可复用、现有 CotW 可复用、外部供体或缺口。法术目录中的 Occultist 列已经存在，仍需核对学派、实际环位及提供方启用。盘点八学派与原作接线的四个 Panoply，列出每项成员学派和能力完整边界。 [原作职业](https://github.com/Holic75/KingmakerRebalance/tree/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/Classes/Occultist.cs)

## C · 冻结两个共享机制

**旗舰档。** 使用 A 的证据，输出 `C-occultist-contract.md` 与 `C-occultist-cases.json`。

- **O-S01 Mental Focus**。定义通用点与各学派已投资、已锁定和剩余可花费数，写明投资、锁定、支付、再分配、休息和重训时的状态变化。原作共鸣按投资时的数值计算，花掉该学派资源后仍应保持先前的共鸣值。核对通用点高成本及补充已花学派点的真实调用。验收包括预算守恒、支付失败无副作用、反复操作不充值、两角色及混职隔离。
- **O-S02 法器选择与法术学习**。冻结首次选择、重复选学派、每环学法术的来源与额度、Panoply 成员先决、升级撤销／重训和 Implement Mastery。把原作虚拟职业等级的实际语义映射到 Occultist 自身的私有参数。法术根优先复用。验收包括正确学派与实际环位、选项互斥／重复、旧来源清退、保留其他来源以及普通 Psychic 法表和其他职业不串入。两条机制接口共同冻结后再放行 B。

## B · 分包实现

- **O-B01 运行时底座**。旗舰档实现 O-S01/O-S02，预计落在独立的 `src/Occultist*0099.cs`。先用一个真实学派完成投资、锁定、共鸣、支付和学法术的纵向证明，该切片仅用于开发。验收须经过原生资源支付或法术学习入口。
- **O-B02 基础职业与目录**。一名日常执行档负责接线与每学派的一整套法术、基础力、共鸣能力、备选 Focus Powers 和前置。可以按完整学派在隔离分支并行。原作 Panoply 与等级能力（含 Magic Item Skill、Repower Construct、Implement Mastery）各按完整入口和效果提交。难移植效果回旗舰档，先登记针对性的阻塞点。完成条件为全部约定学派／Panoply、等级成长和私有法表原子发布。

## D · 独立验收

**更换一名旗舰档。** 输出 `D-occultist-review.md`。核对原作是否漏项、是否重写已存在效果、学派与通用池是否双扣、重训及冷却退出是否串来源。只跑 0099 相关新增编译和定向检查。把失败断言、DLL/依赖 SHA256 和命令留在交接目录。

### 可用模型分档

- **A 资料档**：Gemini 3.8 Flash，负责原文、供体、映射、清单，也可按冻结合同整理机械数据。
- **B 执行档**：Composer 2.5、Kimi K2.8，负责独立文件内的明确接线、必要用例与打包步骤。
- **C／难点 B／D 攻坚档**：Grok 4.7、Gemini 4 Argon、Gemini 3.1 Pro、GLM 5.3、Kimi K3.1 Pro 或 Sol。可任选，每项保持单一实现负责人，D 换人。共享机制、Harmony、生命周期和最终冲突处理留给这一档。
