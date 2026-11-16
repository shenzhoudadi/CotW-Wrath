# COTW-0085 · D 第二次独立复查

复查对象为 `acca8897db2122e948b7806df67ef691e0f31169`，按 `C-0085-20261115-r1` 核对。状态为 **NEEDS_FIX**。[认领 #16](https://github.com/shenzhoudadi/CotW-Wrath/issues/16)

## 可离线定位的问题

### D2-F01 · 原生等级公式除零（C16）

六个情绪的技能特性和 Anger 的 Ferocious Mien 使用 `OnePlusDivStep`，共享构建器留下 `m_StepLevel=0`。用候选 DLL 构建同一配置并调用未经修改的原生 `ContextRankConfig.ApplyProgression`，输入 1 和 12 都抛出 `DivideByZeroException`，对照 `AsIs(12)=12`。技能组件已直接读取 Phantom 职业等级，可以删除多余的 rank。Ferocious Mien 由 Phantom 施放，原作持续时间为每主人 Spiritualist 等级 1 轮，当前 rank 却在 Phantom 身上查 Spiritualist 职业。定向复验应覆盖原生 rank 和实际持续时间。[情绪技能](../../../src/Phantom0085.cs#L242) · [Ferocious Mien](../../../src/Phantom0085.cs#L533) · [原作 Anger](https://github.com/Holic75/KingmakerRebalance/blob/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/Classes/Phantoms/Anger.cs#L72)

### D2-F02 · 伤害互保组越界（C13/C14）

两个互保开关把私有 `PetType` 数值 850085 写入 `ActivatableAbilityGroup`。原生 `UnitPartActivatableAbility` 的组数组长 59，激活流程调用的 `GetGroupSize(850085)` 实测抛出 `IndexOutOfRangeException`，同一对象 `GetGroupSize(0)=1`。现有测试把 850085 当成正确组号。复验应调用原生组查询，并覆盖两个 0085 开关互斥以及同角色 Ghost Rider 的独立开关。[互保构建](../../../src/Phantom0085.cs#L1027) · [现有断言](../../../tests/Spiritualist0085Tests.cs#L320)

### D2-F03 · 三项豁免全高，Anger 叠用动物成长（C16）

基础 Phantom 三项都用高豁免，各情绪又只把指定的两项覆盖为高。原作基础用 Druid 的低豁免，Wrath 对应低进度 `dc0c7c1aba755c54f96c089cdf7d14a3`，等级 1 的基础值为 0，高进度为 2。当前共享成长引用 `AnimalCompanionStrengthDexterityConstitution`（`0c80276018694f24fbaf59ec7b841f2b`），原生每档 STR、DEX、CON 各 +2，原作共享档是 DEX、CHA 各 +1。Anger 的移除表引用了它新增的 `PhantomStrChaGrowth`，原生动物成长留在共享进度上。复验需要算每种情绪的最终基础豁免和成长，并确认 Anger 的重复等级档实际生效。[基础与情绪进度](../../../src/Phantom0085.cs#L76) · [Anger 替换表](../../../src/Phantom0085.cs#L250) · [原作 Phantom](https://github.com/Holic75/KingmakerRebalance/blob/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/Classes/Phantoms/Phantom.cs)

### D2-F04 · 重训还原可带回旧豁免，C09 的 Dog 实例要求需对齐原生（C09/C11）

Shared 和 Fused 豁免由 `BuffCollection.AddBuff` 直接添加，得到无父上下文的永久 buff。蓝图只有 `HiddenInUi`，`IsClassFeature` 为 false。原生 `UnitHelper.RespecOnCommit` 会收集并还原这类 buff，旧加值可绕过新的职业与情绪授权。原生重训还会遍历提交前的所有主人伙伴并销毁旧实体，包含同主人 Dog。当前 `BoundPet` 能约束 0085 自己的操作，C09 的同主人其他伙伴验收要按原生重训结果核对，另一主人和他的伙伴仍应保持原实例。两条均由原生源码调用链确认，重训界面未运行。[Shared/Fused 蓝图](../../../src/Phantom0085.cs#L995) · [添加豁免 buff](../../../src/PhantomRuntime0085.cs#L553)

### D2-F05 · 付款方的临时 HP 可以吸收转移（C13/C14）

`ExactDamageTransfer` 在受保护方用普通 HP 将受损的数值算转移，再通过 `GameHelper.DealDirectDamage` 向付款方扣款。后者重新进入原生 `RuleDealDamage.OnTrigger`，在修改 `Damage` 前调用付款方的 `TemporaryHitPoints.HandleDamage`。按此路径推算，若主人有 10 真实 HP、4 临时 HP，C13 为 Phantom 转移 3 点时，真实 HP 会留在 10，合同的真实 HP 付款应得到 7。C14 的逆向付款同样经过临时 HP。宿主实测 Harmony 能安装到候选与原生方法，临时 HP 实例受 Unity ECall 限制，战斗支付结果未运行。[伤害计算](../../../src/PhantomRuntime0085.cs#L574) · [付款调用](../../../src/PhantomRuntime0085.cs#L1021)

### D2-F06 · 骑乘过滤遗漏原生排除类型（C17）

原生 `UpdateMountAbility` 排除 `MythicSkeletalChampion` 与 `Clone`。0085 的补丁只看总伙伴数是否为 1。只有 Phantom 加骨骸冠军或分身时，原生会因 Phantom 添加骑乘能力，补丁又因总数超过 1 留下它。复验应使用原生候选条件跳过 Phantom，并覆盖只有 Phantom、Phantom 加骨骸冠军以及 Phantom 加可骑乘伙伴。原生骑乘按钮未运行。[骑乘补丁](../../../src/PhantomRuntime0085.cs#L1093)

## 游戏和现有模组的实现核对

- **休息复活（C07）**。私有单位已加原生 `AddResurrectOnRest`。原生与后置补丁重复执行 `Resurrect`、设为 Conscious、设 1 HP 和地图放置。原生从 `CampingState.GetPartyUnitsForRest(false, true)` 取单位，普通休息的 `SelectionCharacterController.GetGroup` 确实跳过 `ShouldBeHidden` 的伙伴。已入队的死亡 Phantom 走原生即可，后置的最终死亡判断也使同次重复复活未被证实。C 合同只允许对确实被遗漏的本项死亡 Phantom 补进成功休息的原生恢复。应保留这层队列例外，复用同一原生复活路径，并检查原生恢复后保存的收回意图会被清理。[原生标志](../../../src/Phantom0085.cs#L195) · [重复复活体](../../../src/PhantomRuntime0085.cs#L1116) · [C 合同](C-spiritualist-contract.md)
- **Ghoul Touch**。原生 DLC5 的 `GhoulTouchCast`（`a2b05555c704458aaadc34be52a63633`）已进入原生二环法表，由 sticky touch 调用 `GhoulTouchAbility`（`cea2f6ead41c449191e1acc4ca488bd0`）。它已有强韧抗性、1d6+2 轮麻痹及 10 尺 Sickened 区域，原作还用 Hold Person 的目标条件。0085 仍把这一行记为无来源。接私表前核对目标差异和 DLC 许可，复用原生施法根。[0085 法表](../../../src/Spiritualist0085.cs#L207) · [原作 Ghoul Touch](https://github.com/Holic75/KingmakerRebalance/blob/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/Features/NewSpells.cs#L7000)
- **Kindness 的 Aid Another（C16）**。ExpandedContent 已实际注册通用攻击／防御 Aid Another（`1eea0d80c6b7407aa8a902f6e750ea95`）和迅捷版（`840e53983d9843489554f76cc764b776`）。0085 只有 `AidAnotherOverride` 和 `HasBenevolent` 的状态记录，仓库内没有消费者；当前没有生效的重复按钮，Kindness 的移动／迅捷与额外 +2 仍缺接线。接提供方时检查已有授权和动作消耗，只扩展 Kindness 差量。[0085 运行时](../../../src/PhantomRuntime0085.cs#L1030) · [ExpandedContent 实现](https://github.com/ka-dyn/ExpandedContent/blob/master/ExpandedContent/Tweaks/Miscellaneous/AidAnother.cs)
- **互保的现成逻辑**。Ghost Rider 已有同名 Etheric Tether（原生开关 `89d4b5016d35429eac524712cc87b6e8`），以 `ContextActionsOnPet` 向 `AnimalCompanion` 挂 buff，用受伤触发后扣主人直接伤害、治疗伙伴与再生组合实现。Oracle Life Link（`ef97c9bcc1c54ea7993ef8b2489c908a`）按回合处理固定 5 点。0085 可复用原生组件和开关用法，C13/C14 要求的双向同一打击精确余额及 0085 授权仍需要专门处理。直接接 Ghost Rider 原件会绑定动物伙伴，使用同组还会串两个职业的开关。[0085 互保](../../../src/Phantom0085.cs#L1018)
- **职业和伙伴入口**。原生蓝图、仓库主线／候选分支／PR（含关闭项）、可见 fork 和本地已审计提供方中，未找到已发布的完整 Wrath Spiritualist 或六情绪 Phantom。Medium 附带的 `Phantom.cs` 没有从模块入口发布，PR #14 仍把创建标为禁用。Making Friends 的 Summoner／Eidolon 是独立入口。0085 已复用原生 `AddPet`、`AddClassLevels`、`UnitPartPet.SetHidden` 与 `FreePlaceSelector`，并读现有 Ill Omen、Lesser Animate Dead、CharacterOptionsPlus 的 Stricken Heart、ExpandedContent 的 Inflict Pain 和 CotW 已有法术根。法表注册与伙伴作用域仍保留自己的入口。[MediumClass](https://github.com/Telyl/MediumClass) · [PR #14](https://github.com/shenzhoudadi/CotW-Wrath/pull/14) · [Making Friends](https://github.com/Balkoth-dev/WOTR_MAKING_FRIENDS) · [CharacterOptionsPlus](https://github.com/WittleWolfie/CharacterOptionsPlus) · [共享法术目录](../../../src/PsychicSharedSpellCatalog.cs)

## 验证记录

- 核对候选 DLL 和 B 原本地实验包，三 DLL 回读一致。主 DLL SHA256 `E499EC7478E40C45571235807D8A00B50113E34FD7EFEDDC67181405C6262795`，ZIP `4C631D6A8954EDB6A1D658460B753D02D687052D944F468B5942307102CCCB88`。前轮 227/227（13.913 秒）按原结果保留。[原验证](B-D-fixes.md)
- Wrath 2.7.0x `Assembly-CSharp.dll` SHA256 `2CB7160B7154D4FFACC77B9C51B1EB26199E1294300F04FDFC073367B2EF8953`。原生 Harmony 2.0.4.0 SHA256 `DE50CD610EA22854C8930EC5433276713AEE1BB0ECD785D6B15A0661C2FA9FD5`。等级与 Harmony 探针编译 0.138 秒、运行 0.443 秒，安装到 1 个原生 `RuleDealDamage` 方法。组号与临时 HP 探针运行 0.829 秒，组号异常成功复现；临时 HP 初次探针失败退出（3.665 秒），收窄后仍在实例初始化受 Unity ECall 限制。
- 本轮生产编译、227 条整组重跑、打包及安装包上传均未运行。完整 AddPet、存读档、重训、休息界面、骑乘按钮和真实战斗未运行。复查提交只记录发现与认领状态。
