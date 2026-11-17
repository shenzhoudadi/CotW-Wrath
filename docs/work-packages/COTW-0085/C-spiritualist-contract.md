# C · Spiritualist 生命周期合同

合同 `C-0085-20261115-r1`。适用 0.14.74 TEST 基线 `939e04d` 上的 Spiritualist 基础职业。范围、情绪、蓝图与法表以 [A 盘点](A-spiritualist-map.md) 为准。验收实例见 [C 用例](C-spiritualist-cases.json)。A/C 为文档与静态证据阶段，B 从本合约的 Anger 切片接手。D 独立复查。

## 唯一主人、伙伴与等级

1. 一级提交后，主人的有效 Spiritualist 职业、一个有效主情绪 fact、对应的私有 Phantom 单位蓝图共同授权一个原生 `AddPet`。运行时只有该主人的原生 Pet 列表及该单位 `UnitPartPet.Master` 双向匹配、私有单位标记、有效主情绪与 `AddPet` 持有的实体引用一致时才允许读写伙伴。调用时校验实际单位，暂未解析的引用只等待原生重连。Preview、别人的同情绪单位、擅自加入的同类型单位都不得接管。
2. 为本项目保留 `(PetType)850085`，静态检查当前原生枚举和项目已知的 Making Friends `604767` 无重叠。此数值进入存档，B 须在其目标分支、已知兼容提供方及蓝图缓存核对占用，稳定后保持编号。Wrath 原生按 Type 判单例，`Clone` 等分支有专用行为。相同数值碰撞或主情绪异常时保留外部单位并停用该次 0085 操作，记录具体主人与来源。Phantom 自身不可骑乘。Wrath 的 `UpdateMountAbility` 会对大多数非 Clone／骨架类型挂原生骑乘按钮，切片需核对 Phantom 不可被选作坐骑及仅有 Phantom 时的按钮表现。若须处理 UI，限定本项和此唯一来源，主人有合法坐骑时保留原生能力。
3. 使用私有、可序列化的主人状态保存 Confined 意图和对应主情绪／实体身份。生命周期根据授权 fact、原生引用、当前生命状态及此意图推导，等级、HP、库存、伴随实例与原生存档字段仍由原生维护。跨事件重放必须幂等，旧实体的延迟回调必须在身份校验时失效。
4. 使用原生 `AddPet` 的 context 等级分支和单位 `AddClassLevels`，强制自动升级，私有 Phantom 职业等级等于已提交 Spiritualist 等级（1–20）。普通兼职、神话等级、原生 AnimalCompanionRank 和 Boon Companion 均不推进 Phantom；Phantom 也不推进其他伙伴。一级之后的正常升级沿用同一实体。若主人蓝图的 `AddClassLevelsToPets` 覆盖导致选错职业，只在当前授予的私有 Phantom 原生升级点收窄适配。蓝图、序列化类型沿用项目 ContentIdentity 约定并登记唯一 GUID/TypeId。

## 显现、收回、召回与恢复

- **初次建立**。有效提交后默认 Manifested。主人尚未进入可放置的本地区域时保留原生绑定，等原生持有场景就绪，不在预览、世界地图或加载中创建额外单位。
- **Confine Phantom**。Standard 自身动作。仅在自己的 Phantom 已显现、存活、HP ≥ 1、与主人处于同一有效本地区域且原生可安全隐藏时执行。用 `UnitPartPet.SetHidden(true)` 投影到场景，无 HP、死亡、位置记录之外的装备、进度或资源重置。原状态已收回时无操作。收回后不能选中、攻击、施法、获得行动、发光环或提供战场反应。
- **Manifest Phantom**。Full-round 自身动作。需要有效、存活、已收回的同一只 Phantom，主人可行动、区内可放置，并先解除 Bonded Manifestation。验证后在主人身边放置并解除隐藏，近点按原生地面选择。发生失败保持收回意图和原有实体。现身、区内召回和休息均复用原生 `UnitPartPet.TeleportToMaster` 的安全落点思路，遇无合法落点不得传到世界原点或另一持有区。
- **Phantom Recall**。6 级起 Swift 自身动作，每日 1 次，10/14/18 级各加 1 次。只移动同一场景内存活、已经显现且引用匹配的 Phantom，主人和周围其他单位不移动。成功一次只扣一次资源；已在召回落点、收回、阵亡、无有效位置、出区或目标在结算前失效时不扣。资源可按通常休息恢复，Manifest 和跨区自动定位不消耗召回。
- **阵亡／回归**。原生 `State.IsDead`、最终死亡和自动恢复决定 Dead/Banished。保留原生伙伴关系、实体、死亡状态及允许的复生目标性。阵亡时解除正在运行的 0085 战场效果与互保，禁止显现、收回、召回和惟有活动伙伴才开的能力。存活但濒死并不等于阵亡，Confine 仍要求 HP ≥ 1。接回由有效的原生复生、难度规则造成的起身或完整休息驱动。沿用私有单位上的 `AllowDyingCondition` 与 `AddResurrectOnRest`。以原作可执行的休息恢复为基线，不另引入文案所述的 24 小时锁，也不模拟死亡来收回。若实际发现自定义类型未进原生休息组，只允许让本主人的本项最终死亡单位在成功休息时进入原生恢复，失败／取消休息不调用恢复。
- **正常休息**。完成可恢复资源的休息后，活着且已收回的 Phantom 如原作 RemoveOnRest 结束收回，结束 Bonded Manifestation，资源按等级和原生休息恢复。当主人处于世界地图或区域不可放置时，仅记待显现意图，进入可放置本地区域后显示。原生死者恢复也以同实体回到 Manifested 为目标。仅加载、走地图或过过场不消去收回、复活或补满次数。
- **场景切换与读档**。原生持有状态和区域加载先完成，等主人与绑定实体可解析再复投影可见性。活着收回保持收回，原生死者保留死亡，已显现者跟随主人在合法区域现身。只在状态需要时调用原生放置／隐藏，重放不重复挂事件、授予 fact 或补资源。主人死亡或暂离队时不强行召回或生成第二只，重新可用时再按保存意图恢复。

## 重训和撤掉授权

- 重训预览、打开、返回与取消不更动真正世界中伙伴的实体、buff、资源、装备及位置。只在原生提交后接收新职业和主情绪事实。以 `UnitHelper.RespecOnCommit` 的原生事实移除和 `IUpdatePet` 提交链为入口，保护原生的 Reapply，避免把重应用误当正式移除。确定提交时验证旧实体与事实来源，再由原生 `AddPet.OnDeactivate` 解除关系并清理合法装备。
- 失去职业／情绪后释放该实例的私有状态、子事实、技能与互保效果，附件按原生规则归还一次。改情绪先清旧 Pet 关系再让新 `AddPet` 接管，旧伙伴与 ex-pet 引用不得留下可回响的权利，旧有资源、HP 及状态不转给新伙伴。同情绪且原生保留的实体、身份与等级均有效则沿用。若提交确需降级／替换，走同一原生清退与重建一次的路径，禁止在正常加载和等级重算时销毁重建。正常重训由新职业表重算已提交能力，老实例的临时效果不能挂到新实例。

## 与伙伴关联的主人能力

- **Shared/Fused Consciousness**。1–9 级仅在活着收回时给两个主情绪 Skill Focus 和原作 mind-affecting 豁免 +4。10 级切到 Fused 的一份授予，12 级为 +8，主人拥有有效主情绪时持续生效，包括显现与暂时阵亡，沿用原作 Fused 常驻 fact。已有 Skill Focus 去重并保留原有来源。Shared、Fused 与其他角色的来源不得叠成两份，撤主情绪／重训须重新绑定。
- **Bonded Manifestation / Dual Bond**。3 级起 Swift 开启，Free 关闭。只在活着且已收回时可开启；最少支出 1 轮，每日 `3+L` 轮，17 级起 `3+2L`。按持续 round 扣减，结束、撤授权、实际失去活着收回条件时清效果。3–12 级 +4 shield AC，13 级起 +6，仅按原作约定作用于相应 incorporeal touch。8 级 Swift 触手，18 级 Full-round 范围触手两掷取高。用主情绪 slam 骰。Dual Bond 只扩预算，仍要求 Phantom 已收回。
- **Spiritual Inference / Greater Spiritual Inference**。4 级起，自己的活着且显现的 Phantom 在 10 ft 内，只给主人 +2 shield AC 和 +2 circumstance saves。12 级以主人 +4 及其他合法盟友 +2 替换，Phantom 自身不获；格斗压制、helpless、unconscious、收回、阵亡或范围外均无此来源。两个拥有者的所属关系独立，范围效果按原作修正类型正常叠加规则处理。
- **Etheric Tether / Spiritual Bond**。可选 Free 开关且同组互斥。仅在已绑定的活着、显现、同区双方参与的实际 HP damage packet 中生效，转移造成的派生伤害不得回流。Etheric Tether 从主人 HP 支付，最多降其至 1，将 Phantom 的致命溢出尽可能拉回到 `1−CON`，不足照常死亡。14 级 Spiritual Bond 要求 Phantom 当下 HP ≥ 1，按原作代码阈值将主人尽可能保在 1 HP，最多把 Phantom 降到 `1−CON`。只转本次有效 HP 伤害，非 HP 效果以及直接 Kill 不进入此路径。玩法文案用实际阈值。
- **Potent Phantom**。20 级选择另一主表情绪；原作接线只把次情绪 1/7/12/17 四个能力应用于同一 Phantom。保留主情绪的单位、技能、一级专有 archetype 条目、豁免、主人 Skill Focus 和 SLA。第二选择无 `AddPet`，同情绪禁止。为处理原作冲突，本项选择 Zeal 实际的 Fortitude/Reflex，并在 15 级实现明示的 DR 15/–，用文案修订记录说明差异。SLA 独立于战场投影，按有效的主情绪和主人等级授予。

## B 的切片及验收边界

先用 Anger 和原生 Dog 对照走一次真实 `AddPet → SetMaster → UnitPartPetMaster → AddClassLevels`，接着以同一实例跑 `SetHidden → TeleportToMaster → RemoveMaster`。使用 Wrath 原生程序集和最小可用宿主；记录确实调用的方法、日志、DLL/依赖指纹及无法进入的方法。建议从 `m_UseContextValueLevel=true`、`m_ForceAutoLevelup=true`、`m_DestroyPetOnDeactivate=true`、无 Smilodon 升级特征的配置开始。单元替身可证纯判定或幂等，原生链要求另有真实调用证据。视图、寻路、场景切换、休息 UI、存读档与重训界面的最终观察留玩家 TEST。

次序为单情绪生命周期，随后六情绪与完整主人进度。用一个私有法表，同表根可按已审计原生／本仓库／兼容提供方读取，保留原环位。不发布空情绪、空 SLA、假根或未完整可选的职业。存在合法但未安装的可选外围法术提供方时，报告缺失的具体法术并保留完整目录，禁止用其他职业表或异名效果填空。Ghoul Touch 尚无确认等价来源，Lesser Animate Dead 在 B 核对后按 A 中的 2 环接入；主线、提供方或协作范围调整需把对应差异留在交接记录中。任何需要变更合约的失败先给出最小证据并回到 C 处理。

本轮只读取原作、Wrath 蓝图和托管代码／IL。C# 反编译 `UnitEntityData` 时工具栈溢出，针对相关方法改查 IL，核到了 `SetMaster`、`RemoveMaster` 和重训提交中的 `IUpdatePet` 调用。没有运行实际伙伴链。编译、运行时定向检查、打包、安装包上传均未运行。
