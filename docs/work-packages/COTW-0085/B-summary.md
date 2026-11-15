# B · Spiritualist 实现交付（COTW-0085）

2026-11-15。合同 `C-0085-20261115-r1`，基线 main `bdc4e39`（生产基线 `939e04d` / 0.14.74 TEST）。工作分支 `feat/cotw-0085-spiritualist`。S-B01（Anger 原生伙伴链状态桥）与 S-B02（基础职业、私有法表、六情绪、完整主人进度）均已实现并通过离线定向检查；D 独立复查与玩家 TEST 尚未开始。

## 实现范围

- `src/Spiritualist0085.cs` — 私有身份盐 `CotW.Wrath.Spiritualist0085.v1/`、137 行法表目录（0–6 环 6/17/24/28/22/24/16）、六情绪数据、原子注册（供体门禁 + SLA 根门禁 + 指纹幂等）、主人职业/法表/进度/能力图。
- `src/Phantom0085.cs` — 私有 Phantom 职业（d10、全 BAB、三项高豁免、仅宠物）、20 级共享进度、六情绪 archetype（豁免/技能/成长替换）、六个标记单位（Spectre 底子 + Smilodon 视觉 + ghost 脚步/黑血 + AllowDyingCondition + AddResurrectOnRest）、24 个情绪特性、主人能力构建（Confine/Manifest、Recall、Shared/Fused、Bonded/Dual、Inference、Tether/Bond、Potent）。
- `src/PhantomRuntime0085.cs` — 可序列化主人状态 `UnitPartPhantom0085`（Confined 意图、主情绪、实体身份、待显现、已授 Skill Focus 台账）；身份校验的生命周期（显现/收回/召回/休息/死亡/跨区复投影）；Etheric Tether/Spiritual Bond 伤害转移（原生 RedirectedDamage 通道）；意识加值随状态刷新；骑乘按钮抑制补丁（仅 850085）；休息复活兜底补丁（仅本主人最终死亡且原生队列遗漏时）；情绪技能等级同步补丁（仅标记单位，AddClassLevels.LevelUp 后置）。
- 注册入口：`BlueprintRegistration.RegisterAll/RegisterOptional` 末尾 `RegisterAvailableModule`；`ModuleCatalog` 登记。
- 每个情绪进度恰好一个原生 `AddPet`：`Type=(PetType)850085`、`m_UseContextValueLevel=true`（ContextRankConfig 读已提交 Spiritualist 等级）、`m_ForceAutoLevelup=true`、`m_DestroyPetOnDeactivate=true`、无 LevelRank/UpgradeFeature（不借 Smilodon 体型升级）。

## 验证（本机，未启动游戏/UMM）

- 隔离定向：**PASS，210 条断言，0 失败**，19.38 秒（含两轮复查新增 17 条，C13/C14 精确算术离线验证）（PsychicTests 宿主，`COTW_0085_CASE=spiritualist0085`，`COTW_ARCANE_01446_ONLY=1`）。覆盖 C01 注册/幂等、主人职业与私法表、137 行目录与源环位、六情绪各一 AddPet 与标记单位、Phantom 职业/进度、主人里程碑、Recall 1/2/3/4 与 Bonded 3+L/3+2L 资源形状、意识加值、Potent 互斥、共享根不变。运行器 `tests/Run-Spiritualist0085.py`，提取器 `tests/Extract-Spiritualist0085.py`（172 行原生供体，A 记录指纹全部核验），定向 `tests/Spiritualist0085Tests.cs`。生成件（B-report/B-result/B-directed 日志/计时）含绝对本机路径，按交接约定只留 `C:/Users/xuenc/agent-tools/spiritualist-next/`。
- 编译：主体重编 **0 警告／0 错误**（首轮全量 4.189 秒）（游戏 Harmony 2.0.4.0 固定引用）。内置 Medium/WayOfTheShield 源码相对 origin/main 无改动，复用 0.14.74 已暂存产物（本机缺 UMM 0.32.4.0 引用，未重编）。
- 打包与包内核对：**0.605 秒**，23 条目，三个 DLL 从 ZIP 回读 SHA256 一致。主 DLL `B5151123BE75C02D31F6B762A4479BD2605CE24E15ABD80CB394780CA1665BB3`。本地实验包见 `C:/Users/xuenc/agent-tools/spiritualist-next/B-package.json`（未上传 GitHub，未称 TEST 交付）。
- `build.ps1` 未运行：本机无 PowerShell 7.4；以等价 dotnet 编译 + python 打包/回读步骤代替，如实记录。

## 合同修订与差异记录

- **DR 15/–**：原作 15 级组件实设 5/slashing，文案 15/–；按 C 合同实现明示 DR 15/–（`PhantomDR15`），差异留此记录。
- **Zeal 豁免**：按原作代码 Fortitude/Reflex（文案 Fortitude/Will 不采）。
- **Lesser Animate Dead**：原生根 `57fcf8016cf04da4a8b33d2add14de7e`（Wrath 库存 3 环）按 A 接入私有法表 2 环；PR #14 未合并、未采用其 corpse-based 实现；行为对比留 D。
- **Ghoul Touch**：无确认等价根，目录保留、注册时报告缺失，不以假根填空。
- **可选提供方 SLA**（Stricken Heart / Inflict Pain / Savage Maw / Force Sword）：原子门禁，根缺失则整包不发布；离线测试按 A 审计身份暂存提供方根（含互惠列表发布），真实提供方行为留 D/玩家。
- **抓取（grappled）门控**：Wrath 无 `UnitCondition.Grappled`；Inference 暂以 Helpless/Unconscious 门控，grappled 判定留 D 确认原生等价状态。

## 自行复查（2026-11-16，B 同会话）

按 C 合同逐条审代码，发现并修复：

1. **PetId 永不初始化（致命）**：原 `BoundPet` 要求 PetId 非空才匹配，而 PetId 只在匹配后赋值——生命周期桥找不到伙伴。改为按 Type+私有标记+双向 Master 解析并收养 PetId；新增主情绪 fact 授权校验与保留类型碰撞保护（碰撞时保留外部单位、停用本次操作）。
2. **Recall 失败也扣资源**：原用 AbilityResourceLogic 预扣。改为动作内仅在成功移动时扣 1；已在落点（≤5 ft）、收回、阵亡、出区、引用不匹配、次数不足均不扣。
3. **伤害转移时序与语义**：原在 RuleDealDamage.OnEventDidTrigger 设 RedirectedDamage（只读计算属性，且伤害已结算）。改为 RuleCalculateDamage 全局后置（Result 求和前），按 C13/C14 算精确转移量，用原生 `RedirectedPercent`/`RedirectionTarget` 通道（向上取整百分比，<100 伤害时精确）；新增显现、同区、存活门控；派生伤害不回流（目标互换后各自短路）。难度缩放与 MinHP 钳制下的百分比舍入留 D 核。
4. **Tether/Bond 未互斥**：两开关改同组 `(ActivatableAbilityGroup)850085`、WeightInGroup=1。
5. **意识台账串号**：Shared/Fused 共用台账会互删 Skill Focus。改为单一协调器按主情绪 fact/收回/Fused 状态汇总期望集合，再增删 fact 与模式豁免 buff；情绪撤销时确定性回收全部已授 focus。
6. **Potent 接线错误**：原把次情绪整条进度（含主人 SLA）挂到 Phantom。改为只取 1/7/12/17 四个 `AddFeatureToPet` 内层特性授予同一 Phantom。
7. **Bonded 未清场**：死亡/显现/休息/跨区复投影时清除 Bonded buff；休息后复投影复活伙伴。
8. **触手未实现**：补 8 级 Swift / 18 级 Full-round 触手攻击（主情绪 slam 骰、18 级两掷取高 ModifyD20），经 AddFeatureOnClassLevel 授予。
9. **Inference 叠加**：owner/ally buff 改 StackingType.Stack，两主人独立。
10. **Multiattack 供体 GUID 错误**：原猜测 GUID 不存在，改原生 `a634f97d23662b347872ffcf580495b3` 并入门禁与提取器。
11. **骑乘排除**：新增 `AbilityTargetIsSuitableMount.IsTargetRestrictionPassed` 窄前缀，仅拒绝标记的 850085 Phantom；仅有 Phantom 时移除骑乘按钮（原有补丁）。
12. **视觉 Barks**：补回 Spectre 供体 Barks 复制。

CotW 根 GUID（BurstOfAdrenaline/SavageMaw/ForceSword）经 MD5 盐值核验一致。新增 9 条定向断言覆盖 Recall 扣减、互斥组、Stack、触手授予、Multiattack、DR 15/–。

## 第二轮修复（2026-11-16，继续修复）

1. **转移算术 HP 语义错误**：原用 `Damage`（已受伤害，setter 钳制 ≥0）当当前 HP。改 `HPLeft`（= HitPoints − Damage，可为负）。抽出纯函数 `TransferAmount`，C13/C14 六组数值离线断言精确通过（3／1／0／13／0／0）。
2. **grappled 门控落地**：Wrath 无 Grappled 条件，改用 `UnitPartGrappleInitiator/Target` 判定，Inference 门控补齐合同要求的抓取状态。
3. **Inference 幻影自身排除**：enter/round 动作显式 `unit == phantom → return`，幻影自身不获加值（C15）。
4. **Bonded 护盾对虚体接触**：新增 `Phantom0085ShieldToIncorporealTouch`（ITargetRulebookHandler<RuleCalculateAC>），仅当攻击者具 Incorporeal 特性（c4a7f98d，已入门禁）且为接触攻击时，把护盾描述符加值转入 Touch AC；按原作约定限定虚体。
5. **阵营同步**：Reproject 时同步幻影阵营到主人（覆盖跨区/休息/复活时机）。

## 未测边界（D / 玩家 TEST）

- 原生链真实调用：`AddPet → SetMaster → UnitPartPetMaster → AddClassLevels`，及同实例 `SetHidden → TeleportToMaster → RemoveMaster`（C02–C04、C10、C17）。
- 休息 UI、存读档、重训界面、跨区、世界地图、骑乘按钮、场景放置与动画（C07–C09、C18）。
- 伤害转移在难度缩放／MinHP 钳制／临时 HP 下的百分比舍入（≥100 伤害可能 ±1）、意识加值刷新、技能等级同步补丁的运行时行为。
- 完整六情绪实机成长与主人进度（C16）。

## 接手

D 换一名旗舰档，按 C 合同走新增编译、0085 离线注册与显现/收回/移除方案，检查两主人同场及动物伙伴负例；原生链证据用 Wrath 原生程序集与最小宿主，记录确实调用的方法与无法进入的方法。B 的离线宿主不进入场景/存档/重训控制器，这些不得写成 PASS。