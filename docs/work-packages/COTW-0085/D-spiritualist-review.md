# D · Spiritualist 独立复查

2026-11-16，**NEEDS_FIX，退回 B**。复查候选 `a293f31835ee899069c6a3161e5fb4ea3dbd1f40`，生产变更截至 `39fe319`，合同 `C-0085-20261115-r1`。主工程重编和 210 条离线注册／定向断言通过。以下问题已能用当前源码、原生代码或定向执行定位，处理后复查受影响用例。认领 [#16](https://github.com/shenzhoudadi/CotW-Wrath/issues/16) 保持开放。

## 退回 B 的问题

### D-F01 · 伙伴等级多算一级，Bonded 轮数接线错误（C02/C03/C12）

一级选择 Anger 即可触发。情绪的原生 `AddPet` 读取 class rank，配置 `OnePlusDivStep` 且步长 1。D 直接对本机原版 `ContextRankConfig.ApplyProgression` 给出 1/2/20，返回 **2/3/21**。原生 `AddPet.GetPetLevel` 在 context 分支直接返回该值，常规动物等级表在后面的分支。改为原样使用已提交本职等级，测 1、2、20 及兼职负例。[伙伴等级配置](../../../src/Spiritualist0085.cs#L878)

Bonded 的线性资源把职业写入 `m_ClassDiv` 并清空 `m_Class`，原生 `GetMaxAmount` 的 `IncreasedByLevel` 只从后者读取，3 级基础预算因此只有 3。Dual Bond 还使用 `OnePlusDivStep` 而未设置非零除数，原生计算可除零。验证 3/16/17/20 级最大值应为 6/19/37/43。现有断言只查资源标志和 Dual Bond 组件存在。[资源配置](../../../src/Phantom0085.cs#L1034)

### D-F02 · 伤害转移可以接管无关目标，最终扣血也会偏移（C13/C14/C17）

`39fe319` 抽出 `TransferAmount` 时移除了原来两个 `deal.Target` 条件。现为全局 `RuleCalculateDamage` 回调，验证了主宠关系后对任意受击事件算数并写入 `RedirectionTarget`。例：A 的 Tether 已开，A 的 Phantom 1 HP、CON 13、A 有 10 HP，场上敌人、B 或 Dog 吃到 16 伤害，A 的回调仍算 3 并把接收者设为 A。另一组主人也能覆盖同一事件，转移产生的伤害可重入。先恢复精确受击者条件、活跃双方和同区门控、既有重定向／派生包保护。测错主人、动物伙伴、敌人和转移派生包；B 的六个纯算术样例保留。

原生 `RuleDealDamage.OnTrigger` 在本回调之后处理 MinHP、难度、整数百分比，再扣临时 HP 并派生直接伤害。D 用候选函数计算 200 点伤害、Phantom 185 HP、CON 13、主人 10 HP，应转 3；现有百分比编码按原生公式变成 4。初始伤害和最终实际扣血可因保护与临时 HP 分离。实现须按实际 HP 包保护两端阈值，单独覆盖零有效伤害和 200 点样例。原生完整战斗包序仍待验证。[转移回调](../../../src/PhantomRuntime0085.cs#L537)

### D-F03 · 收回、显现、召回仍缺执行门控（C04/C05/C12）

- 满血 Phantom 的 `Damage=0` 被 Confine 拒绝，已受伤且濒死的活单位反而越过这道检查。以 `HPLeft >= 1` 判定，并要求可安全隐藏的当前本地区域。
- Manifest 主动删除正在生效的 Bonded buff 后显现，合同要求先手动关闭；不可放置时也写入待显现，下一次加载会自动执行失败的手动施法。
- Recall 在调用原生 `TeleportToMaster` 之前扣次，且持有区相等可同时为空。先确定有效区、可落点及合法移动，再扣一次。重放、无落点、正在 Bonded、未加载、满血及濒死分别加入负例。[收回与显现](../../../src/PhantomRuntime0085.cs#L778) · [召回](../../../src/PhantomRuntime0085.cs#L647)

### D-F04 · Inference 的撤销及来源隔离失效（C15/C17）

两种原生 grapple 部件确实存在，`unit == phantom` 也已挡住自身授予。已有 buff 是永久的，进入抓取、无助、昏迷或死亡时 `RunAction` 提前返回，未撤销先前授予。同一个盟友被两个 Greater aura 覆盖时，`BuffCollection.GetBuff(blueprint)` 只按蓝图返回第一条，第二只 Phantom 的进入会被第一条挡住，第二只离开又能删第一只的事实。12 级仍挂有基础 aura。按实际发射者登记并清理，12 级只保留 Greater 本来源。复查两个主人同场时同一个盟友进入、失能、恢复、离开及收回，保留其他来源。[授予与撤销](../../../src/PhantomRuntime0085.cs#L606) · [两级进度](../../../src/Spiritualist0085.cs#L732)

### D-F05 · 状态桥没有验证原生授权实体，休息与骑乘存在越界（C07/C09/C17）

`BoundPet` 只核对主情绪 fact 存在、保留类型、任一 Phantom marker、Master 与缓存 ID；接受的未绑定候选无需同情绪，也未核对 `AddPetData.SpawnedPetRef`，且排除未标记单位后才判断碰撞。构造主 Anger、私有类型被同主人另一情绪或外来授予占用的负例，操作应停用，不能认领。骑乘按钮补丁只比较 Type，会对没有私有 marker 的外来单位生效。休息兜底遍历 `player.AllCharacters`，比原生实际休息组宽，队外在册主人的伙伴也可能被满休复活。复活范围要由本次成功休息的主人集合限定。换情绪／撤 Potent 另需验证旧次情绪清退，`Phantom0085PotentGrant` 目前只有直接添加，缺撤销及绑定更新入口。[身份校验](../../../src/PhantomRuntime0085.cs#L60) · [次情绪](../../../src/PhantomRuntime0085.cs#L670) · [骑乘与休息](../../../src/PhantomRuntime0085.cs#L886)

### D-F06 · Bonded 护盾和阵营同步仍有漏项（C12/C16）

`RuleCalculateAC` 确实会将规则临时修正纳入触碰 AC；虚体与接触判定方向正确。处理器取的是整项 `Shield` 描述符，而非 Bonded 本身的 +4／+6。开启 Bonded 且同时存在 +8 的其他护盾加值时，虚体触碰预计会拿到 +8。应只读 Bonded 自己的数值，测试普通／虚体接触与外部更高护盾。阵营现在在生成／复投影时更新，原生会发布 `IAlignmentChangeHandler`；主人同一区域改变阵营且无生成、休息和切区事件时，Phantom 仍保留旧阵营。按当前主人事件更新私有伙伴，两角色互不影响。[护盾规则](../../../src/PhantomRuntime0085.cs#L763) · [阵营同步](../../../src/PhantomRuntime0085.cs#L241)

### D-F07 · Fused Consciousness 对同一豁免重复加值（C11）

Fused 职业 fact 自带一份 `UntypedStackable` mind-affecting 加值，协调器还发同描述符、同档位的 `FusedConsciousnessSave` buff。生效时会有 +8／+16 两份合计，合同要求 +4／+8 一份。保留一个来源，验证 9→10→12 级的实际规则修正、已有 Skill Focus 和撤主情绪。[职业 fact](../../../src/Spiritualist0085.cs#L790) · [协调器 buff](../../../src/Phantom0085.cs#L985)

### D-F08 · 主人 Emotional Power 读取 Phantom 职业等级（C16）

`BuildEmotion` 把 `phantomClass` 当作主人 SLA 的 DC 职业传入。原生 `ContextCalculateAbilityParamsBasedOnClass` 用施法者指定职业的等级替换 CL、该等级的一半替换法术等效等级。主人没有 Phantom 职业，现配置将两值算成 0。按主人 Spiritualist 职业取 CL 与半等级，覆盖高低等级及至少一个带变体的根，核对各子选项继承正确的资源与 DC。[SLA 接线](../../../src/Spiritualist0085.cs#L848) · [参数构建](../../../src/Phantom0085.cs#L579)

### D-F09 · 注册失败缺少回滚（C01）

预检和成功重复注册通过。写缓存后仍有类型／根进度检查及其他可失败步骤，catch 只写日志并返回 false。晚失败可留下部分私有蓝图，后续重试在 GUID 检查处失败。给注册尾部注入一次失败，验证本项缓存和职业根数组恢复，下一次同条件注册可成功，保护原有外部条目。[发布事务](../../../src/Spiritualist0085.cs#L509)

## 实际验证

- 主体 `dotnet build CallOfTheWild.Wrath.Prototype.csproj -c Release -p:WrathPath=<安装目录>`，2.570 秒，0 警告／0 错误。独立宿主 `dotnet build tests/PsychicTests.csproj -c Release`，同游戏路径，3.490 秒，0 错误，两条既有测试警告（Psychic027、Migration01429）。
- `COTW_ARCANE_01446_ONLY=1 COTW_0085_CASE=spiritualist0085`，`EngineLauncher.exe <engine02> <PsychicTests.exe> <Managed> <D主DLL> <D-registration-report.json> <native-test.json> <spiritualist-0085-native-ext.json>`。结果 **210/210，20.673 秒**。覆盖门禁、成功重复注册、私有蓝图图谱、资源组件和六个纯算术值。宿主只在蓝图阶段暂存必需提供方根；无原生伙伴实体、实际属性轨迹或玩家场景。engine02 仅为独立蓝图加载调整了物品缓存的 Unity 检查。
- 原版 `Assembly-CSharp.dll` 定向探针 0.188 秒。调用原生等级公式并核对 200 点转移算术，随后尝试 `UnitEntityData` 原生构造，在 `BuildModeUtility.GetStartUpData` 的 Unity 内部调用得到 `SecurityException`，退出码 4。原始输出在本机 `D-native-probe.log`。**AddPet、SetHidden、TeleportToMaster、RemoveMaster 的实链未进入**。存读档、跨区、休息 UI、重训 UI、骑乘按钮与摆放未运行。没有启动游戏或 UMM。
- B 原 ZIP 三个 DLL 已独立回读一致，核对耗时 0.105 秒。D 未重打包，安装包上传未运行。B 源码指纹与提交一致（五个活动文件只存在 CRLF 转换）。

### 证据指纹

- D 主 DLL `0DE9E8255B8F639DC1B51BE5AFB8525FD47FA199A696F92997E1CF44DC5991BB`，D 运行宿主 `2CF6F96D97D1EFE0F394761E6CE16E091D609F1292AD6027ABD2EB717B721501`。
- 游戏 `Assembly-CSharp.dll` `2CB7160B7154D4FFACC77B9C51B1EB26199E1294300F04FDFC073367B2EF8953`，Harmony 2.0.4.0 `DE50CD610EA22854C8930EC5433276713AEE1BB0ECD785D6B15A0661C2FA9FD5`。
- 定向宿主引导 `764AF33A42AE8E2DDD37E07B090F6DF5414863AF977CBB9AC2525C27854C34B2`，engine02 `3A85EDDC5D21CE98779C54F549BDD471D3DEF061948AEE2E85988E9782B82E80`，合并蓝图夹具 `F37256BF468420E30A2B5C825D79C495A97ADEE53794550B66FCE2549A38049B`，证据夹具 `F8CBC0B7F3740FEAB3FF1E66FF1B98D3D9F3C49170AA7B2C1B515AA6003153A1`。
- B 本地 ZIP `36DE29BE732EF6E66B04D450AC58C575D4CBDD240FB2AC0AD3D2006B936C258A`，内部主 DLL `B5151123BE75C02D31F6B762A4479BD2605CE24E15ABD80CB394780CA1665BB3`。ZIP 内其余两个 DLL 指纹与 B 记录吻合。该 ZIP 尚未发布。
