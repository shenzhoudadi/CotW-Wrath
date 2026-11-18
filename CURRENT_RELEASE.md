# 当前开发与出包流程

2026-11-15（通灵者大改 阶段 4：英灵专长 Spirit Feat）：4 级获得一个满足先决条件的额外专长，8 级及之后每 4 级再各一个 → **4/8/12/16/20 共 5 次**（按用户 2026-11-15 拍板的正文口径，不是标题写的 5 级起）。

**实现**：`Medium/SpiritFeat.cs` 建一个 `BlueprintFeatureSelection`（`Guids.MediumSpiritFeat`，`Ranks = 5`），`MediumProgression` 在 4/8/12/16/20 五个条目各授予一次。规则原文是「一个他满足先决条件的额外专长」，即游戏自己的专长池，所以**池子直接拷贝原生 `BasicFeatSelection`（`247a4068296e8be42890143f451b4b45`）的 `m_AllFeatures`**（276 项 = 241 特性 + 21 选择器 + 14 参数化专长），不手写清单；能不能选仍由原生 `BlueprintFeatureSelection.CanSelect` 判先决条件，同一专长不能选两次也由它按 `feature.Ranks` 挡掉。`SetGroup(FeatureGroup.Feat)` 只为让升级界面把它归到专长那一类。

**为什么要延迟再同步一次**：其他 mod（TTT 等）是在缓存 patch 阶段往 `BasicFeatSelection` 里加专长的，可能晚于我们建蓝图。所以 `Main.cs` 的 `AfterBlueprintCachePatches` 增加 `delayed Spirit Feat pool` 阶段调 `SpiritFeat.ConfigureDelayed()`，按 `deserializedGuid` 去重后把新增项追加进池（幂等、只增不减），异常只记日志不阻断建角色。**注意**：BlueprintCore 的 `PopulateSelections` 只会把新特性加到它「按引用认识」的原生选择器里，我们这种运行时新建的选择器收不到自动追加，所以这一步必须自己写。

**测试**：新增 `tests/SpiritFeat` 静态审计 **35** 项（GUID 唯一性、`NativeBasicFeatSelection` 锚点、`Medium.json` 中英文本齐全且无孤儿键、`BlueprintFeatureSelection.CanSelect` 仍按 `Ranks`+`MeetsPrerequisites` 判、`Items` 仍读 `m_AllFeatures`、`blueprints.zip` 里 `BasicFeatSelection` 的 `Group == Feat` 且 276 项、产物里 `ConfigureEnabled`/`ConfigureDelayed` 与进度表/延迟阶段的接线）；`MediumProgressionLevels` 增加 2g（4/8/12/16/20 五次，纯 10 级骑士不触发），当前 **12015**。

**测试**：`SpiritFeat` 35、`MediumProgressionLevels` 12015、`SpiritAffinity` 186、`SpiritSurgeScenarios` 1099、`InfluenceScenarios` 703、`SpiritChannelLifecycle` 8103、`SpiritInfluenceScenarios` 378、`MythicRegistration` 55、`CombatLog` 16、`HardenedSoul` 32 全 PASS；模块编译 **0 警告 0 错误**。

**未做 / 已知偏差**：没有任何「按英灵区分的专长清单」——原文没有按英灵限定的专长，所以这一阶段与英灵无关；`SpiritSaveRestore` 仍未运行（既有的 `extern alias NativeCecil` 与 csproj 引用不匹配问题，本轮没动它）。

未打包、未 commit、未 push、未启动游戏/UMM。

2026-11-15（通灵者大改 阶段 3：英灵亲和 Spirit Affinity）：3 级起每 4 级（3/7/11/15/19）各选一项，共 5 次。13 项里落地 **12 项**；`Divine Spirit`（牧师领域）按用户 2026-11-15 决定**搁置**。

**框架**：`Medium/SpiritAffinity.cs` 建一个 `BlueprintFeatureSelection`（`Guids.MediumSpiritAffinity`，`Ranks = 5`），`MediumProgression` 在 3/7/11/15/19 五个条目各授予一次（同一个选择器授予多次 = 多次选择，与原生 `ArmorTrainingSelection` 同形）。**默认不可重复选**——游戏只在 `Ranks > 1` 时允许重复选同一项，所以所有条目 `Ranks = 1`，只有明确「可第二次选」的做成两个独立特性（`SpiritWarding`/`SpiritWardingGreater`、`Spiritsense`/`SpiritsenseGreater`），用 `AddPrerequisiteFeature` + `AddPrerequisiteClassLevel` 做等级门。

**优先复用原生蓝图，不重造**：
- **阵营武器** = 原生 `AddOutgoingPhysicalDamageProperty(addAlignment: true, myAlignment: true)`，`MyAlignment` 直接读 `Owner.Alignment.ValueRaw`。
- **灵体化身** = 同一个组件的 `addReality: true, reality: DamageRealityType.Ghost`。原生**没有** GhostTouch 组件，武器的幽冥附魔 `Weapons/Enchantments/GhostTouch` 本身就是 `WeaponReality{Reality: Ghost}`；**盔甲/盾牌的幽冥附魔游戏里没有**（`GhostLeather` 是另一回事，与幽冥无关），所以只有单档。
- **寻找陷阱** = 原生 `Trapfinding` 的 `ContextRankConfig`（ClassLevel `Div2`、`m_UseMin` 1、`m_Max` 20）改 `m_Class` 为 `Guids.Medium`，再 `AddContextStatBonus` 到 Perception 与 Trickery。原生只加 Perception，按用户口径补上巧手。
- **直觉闪避** = 原生 `UncannyDodge` 的两个组件（`FlatFootedIgnore(UncannyDodge)` + `AddCondition(AttackOfOpportunityBeforeInitiative)`）**加上原生 `ImprovedUncannyDodge` 的实现**：`AddMechanicsFeature(MechanicsFeatureType.CannotBeFlanked)`。「盗贼高 4 级才能夹击」是 PF 的 Improved Uncanny Dodge 规则，Wrath 把它简化成绝对的 `CannotBeFlanked` 标记，没有等级差判定，所以照原生写法即可。
- **精魂感知** = `AddBlindsense(30ft)`（`Blindsight` 默认 false = 盲感）；第二次选 `AddRemoveFeatureOnApply(30ft 版)` + `AddBlindsense(60ft, blindsight: true)`。
- **英灵防护** = 与 `DeathWard` 同一组组件，**判据用 `SpellDescriptor`，不写能力清单**：`Death | ChannelNegativeHarm`（`ChannelNegativeHarm = 0x100000000000000`，`ChannelNegativeEnergy` 技能上挂的就是这个）。第二次选 `AddEnergyImmunity(NegativeEnergy)` + `AddImmunityToEnergyDrain` + `SpellImmunityToSpellDescriptor` + `BuffDescriptorImmunity`，同一组描述符。
- **精魂魔宠** = 运行时拷贝原生盗贼天赋 `Familiar`（`363cab72…`）的 `m_AllFeatures`（13 种魔宠），不手写清单；魔宠的主人加值本来就是定值，所以不需要职业等级替换。
- **有效奔涌** = 新 `SpiritSurgeMath.Draw(sides, random, assuredSurge)`，并在 `SurgeRoll` / `MediumSpiritSurgeComponent.SurgeData` 中保留 `FirstDraws`/`SecondDraws`（向后兼容旧存档）。`CombatLog.SpiritSurge` 在触发有效奔涌时展示每次重骰结果（如 `5(重骰2,5)` / `5(reroll 2,5)`）。`HardenedSoul` 的 2d8 同路径。
- **灵御** = 新组件 `MediumSpiritualDefenseComponent` 改为在 `Owner.Stats.AC` 上挂持久化 `ModifierDescriptor.Shield` 加值（`AddModifierUnique`/`RemoveModifiersFrom`），并监听装备变更、切换武器栏、Buff 增减及 `ApplySpirits`/`MediumSpiritFocusComponent` 刷新，使人物面板、工具提示与战斗都可见。另通过 `ModifiableValueArmorClass.OnUpdate` 与 `StatModifiersBreakdown.GetDisplayArmorModifiers` 的 Postfix 让灵御的盾牌加值同样计入接触 AC 与面板明细（普通盾牌仍不计入接触 AC）。英灵同调值统一由 `SpiritBonusValue.Of(owner)` 现算。

**TabletopTweaks-Base 与进阶训练（奥术发现 / 护甲训练 / 武器训练）**：`Medium/SpiritAffinityOptional.cs` 在**延迟阶段**（`Main.cs` 的 `optional TTT Spirit Affinity`）跑，并在 **`GameStarter.FixTMPAssets` Postfix（`Priority.Last`）** 调 `SpiritAffinityOptional.SyncPools()` 与 `SpiritFeat.ConfigureDelayed()` 再同步一次，以吸收 EbonsContentMod、MysticalMayhem 等在 `LoadAllJson` / `FixTMPAssets` 阶段才注入的奥术发现与专长。
- **奥术发现**：拷贝 `ArcaneDiscoverySelection`（`45b72087…`）的 `m_AllFeatures`，并在 `FixTMPAssets` 后幂等补齐迟到的第三方条目。
- **护甲训练**：提供 TTT `AdvancedArmorTrainingSelection`（`5bf17c2a…`，`Ranks = 5`），向 `FighterArmorTrainingProperty` 追加 `MediumTrainingLevelGetter`（以通灵者等级补足，不重复计兼职战士等级），并由 `PrerequisiteFeature` / `PrerequisiteFeaturesFromList` 补丁使 `ArmorTraining` 前提交由 `MediumSpiritAffinityArmorTraining` 满足（未装 TTT 时不提供）。
- **武器训练**：优先拷贝 TTT `AdvancedWeaponTrainingSelection`（`538bc739…`，`Ranks = 5`），未装 TTT 时降级拷贝原生 `AdvancedWeaponTraining1`（`3aa4cbdd…`）；向 `FighterWeaponTrainingProperty` 追加 `MediumTrainingLevelGetter`；挂 `MediumAdvancedWeaponTrainingComponent` 确保拥有 `UnitPartWeaponTraining`，并由 `UnitPartWeaponTraining.GetWeaponRank`/`GetMaxWeaponRank` Postfix 使其不低于 `SpiritBonusValue.Of(unit)`；`PrerequisiteFeature` / `PrerequisiteFeaturesFromList` 补丁使 `WeaponTrainingSelection` 与选择窗内的 `Bravery`（仅 `ArmedBravery`）前提交由 `MediumSpiritAffinityWeaponTraining` 满足；`ContextRankConfig.GetBaseValue` Postfix 为无战士 `Bravery` 层的通灵者提供 `SpiritBonusValue`（仅 `ArmedBravery`）。
- **作用域**：`ClassLevelsForPrerequisites` 的 `m_ForSelection` 必须指向**我们拷贝出来的选择器树**（`selection`，而非 `source`），否则在拷贝选择器里不生效且会漏到原版选择器。

**测试**：`SpiritAffinity` **197**、`SpiritFeat` 35、`MediumProgressionLevels` 12015、`SpiritSurgeScenarios` **1105**、`InfluenceScenarios` 703、`SpiritChannelLifecycle` 8103、`SpiritInfluenceScenarios` 378、`MythicRegistration` 55、`CombatLog` **18**、`HardenedSoul` 32 全 PASS；模块编译 **0 警告 0 错误**。

**未做 / 已知偏差**（不要当成已完成）：
1. **Divine Spirit（神圣英灵）整项未做**，用户 2026-11-15 拍板搁置。
2. **灵体化身的第二档**（盔甲/盾牌幽冥）没做：原生游戏没有盔甲侧的幽冥实现，虚体接触攻击无视护甲是 `RuleCalculateAC` 内部行为，没有可复用组件。

未打包、未 commit、未 push、未启动游戏/UMM。**阶段 4**（英灵专长 4/8/12/16/20）已在后续一轮完成，见文首条目。

2026-11-15（通灵者大改 阶段 2：神话多重降灵改造）：按用户方案重做神话能力，三项改动 + 一项核实。

**① 数量上限**：新增 `MultiSpiritRules.MaxSpirits(owner)` = **2 位（神话 1-6）/ 3 位（神话 7 起）**，`CanChannel` 在「已有降灵且拥有神话能力」之后加 `ActiveSpiritClasses.Count() < MaxSpirits(owner)`。`CanChannel` 同时是技能灰显（`AbilityRequirementHasSpirit.Not`）与实际施放（`ContextActionApplySpirit`）的唯一判据，所以一处改动两处生效。灰显文案同步成「追加降灵需要神话多重降灵，且数量受其限制」。

**② 共鸣代价不再递增**：`MultiSpiritRules.NextChannelCost` 由 `Math.Max(1, 已降灵数)` 改为**恒 1**——旧实现第 3 位会收 2 点、第 4 位 3 点；现在每位都只加 1 点（与阶段 0 拍板口径「仍 1 点、只是不再递增」一致）。

**③ 同类型英灵同调只应用一次**：**代码本来就是这么写的，本轮只补测试说明**——`MediumContextSpiritBonusComponent.ApplyModifiers` 对 `ActiveSpiritClasses` 的加值做 `GroupBy(Stat)` + `Max`，盟友镜像 `MediumSpiritInfluenceAllyComponent.CollectBonuses` 同样是「每 stat 取最高」。不要改成求和。

**④ 与 10 级多重降灵可叠加**：已核实——借力（星界灯塔、多重降灵）不进 `UnitPartMedium.ActiveSpiritClasses`，所以既不占数量上限、也不计入惩罚除数 N。

**惩罚减幅保留**：`InfluencePenaltyMath.Magnitude` 仍按 `ActiveSpiritClasses.Count()` 除（两位减半、三位三分之一），本轮未改。

**测试**：`MythicRegistration` **55**（+7：第二位允许、神话 6 时第三位被拒、神话 7 允许第三位、神话 7 仍限三位、`MaxSpirits` 2/3、`NextChannelCost` 恒 1）、`InfluenceScenarios` **703**（+2：代价恒 1 连降 5 次到上限、第 6 次被拒；原「1+1+2 递增」断言已改为恒 1）、`SpiritChannelLifecycle` **8103**（不变）全 PASS；模块编译 **0 警告 0 错误**。`Feats.json` 的 `MythicMultipleSpirits.Description` 中英文同步改写。

未打包、未 commit、未 push、未启动游戏/UMM。**阶段 3**（英灵亲和 13 项）、**阶段 4**（英灵专长 4/8/12/16/20）未做。

2026-11-15（通灵者大改 阶段 1b：多重降灵 10/14/18 + 英灵变体自动发现）：本轮完成阶段 1 的后半，并回答用户「以后做很多传奇英灵会自动加进去吗」。

**英灵自动发现**：星界灯塔与多重降灵不再手写英灵清单。
- `ChannelSpirit.Feature` 保存降灵特性；`LegendVariants.All()` 读该特性的 `AddFacts` 表，用每个降灵技能里的 `ContextActionApplySpirit.Spirit` 反查它对应哪个英灵。这个动作本来就是「降灵」的唯一入口，新英灵不可能绕过它——所以新增传奇英灵只要建好降灵技能并加进 `AddFacts`，两个能力就自动多出对应子选项，`AstralBeacon.cs` / `TranceOfMany.cs` 一行都不用改。
- 变体 GUID 由 `VariantId.Of(purpose, spiritGuid)` 用 MD5 派生（`Utilities/VariantId.cs`），跨读档稳定；`Guids.cs` 里 32 个手写变体常量已删除。变体资产名 = `AstralBeaconAbility`/`TranceOfManyAbility` + 降灵技能名去掉 `MediumChannelSpiritAbility` 前缀（`Archmage` 等），拿不到就退回英灵 GUID。
- 变体名称与图标直接取降灵技能的：名字复用 `<Legend>.Name` 键（先 `LocalizationManager.CurrentPack.GetText(key, reportUnknown:false)` 探测，缺失才回退父能力名），图标取 `channel.Icon`。`Main.cs` 的 `localization` 阶段在 `classes` 之前，所以配置时键已可查。

**多重降灵（10 级）**：`TranceOfMany.cs` = 特性（10 级授予）+ 父技能 + 每英灵一个变体技能 + 一个 buff。
- 变体：自由动作，`AbilityRequirementHasSpirit`（须已降灵）+ `AbilityRequirementSpiritAvailable`（该英灵未降灵、且不与当前英灵同档冲突），用 `ApplyBuffPermanentFixed` 挂永久 buff——`ContextDuration` 没有 Permanent 档，只有这个重载会置 `ContextActionApplyBuff.Permanent = true`。
- `MediumTranceOfManyComponent`（buff 组件）：`Tiers(owner)` = 10/14/18 → 1/2/3 层；`OnActivate`/`OnPostLoad`/`OnTurnOn` 都走 `GrantSecondary`，并实现 `IUnitReapplyFeaturesOnLevelUpHandler` 在升级时补层——10 级选了之后 14/18 级自动升级，不用重新点。
- `GrantSecondary` 改成可续借：多一个 `SecondaryGrant grant` 参数和 `Tiers` 字段，只补「还没给过」的层；诡术师/罗宾汉的中等之力 rank（等级/3）也会随等级补足。`SecondaryGrant` 记录实际加了几级，`RevokeSecondary` 只收回这些。
- 只借力量：不给英灵同调、降灵奖励、共鸣惩罚或法术，也不占神话多重降灵的名额。

**一次只能借一位**：新增 `SecondaryLend`（静态登记所有借力 buff）。任何新借力（灯塔或降灵）会先撤掉上一个；`ApplySpirits.OnDeactivate` 在结束降灵时 `RetireAll`，所以多重降灵不会跨降灵残留，也不会和灯塔叠两份。

**同档冲突**：`AbilityRequirementSpiritAvailable` 现在也拒绝与当前英灵同档的英灵（Adriel↔Champion、RobinHood↔Trickster），否则会把同一份蓝图再叠一级（`MultiSpiritRules.SameTierConflict` 由 private 改 internal 复用）。

**测试**：`SpiritChannelLifecycle` **8103/8103**（新增多重降灵 10/14/18 分层、升级补层、结束只收回自己借的层、主英灵同档不叠加；灯塔到期断言改为「只收回它补的那 1 级」）、`MediumProgressionLevels` **11999**（比上轮 +1，即 10 级那条授予）、`InfluenceScenarios` **701**、`SpiritInfluenceScenarios` **378**、`SpiritSurgeScenarios` **1090**、`MythicRegistration` **48** 全 PASS；模块编译 **0 警告 0 错误**。`SpiritSaveRestore` 仍未运行（既有 Mono.Cecil alias 问题）。
运行方式备忘：`InfluenceScenarios` 是 net472，要 `dotnet build -t:Rebuild` 后直接跑 exe 并传 `Assembly-CSharp.dll`；`MythicRegistration` 要传 `BlueprintCore.dll`；`MediumProgressionLevels` 的检查数随进度表条目变化（每次授予多一条断言），不要拿旧数字当基线。

未打包、未 commit、未 push、未启动游戏/UMM。**阶段 2**（神话多重降灵：数量上限 1、神话 7 级 2，额外降灵 0 共鸣，同类型英灵同调只应用一次）、**阶段 3**（英灵亲和 13 项）、**阶段 4**（英灵专长 4/8/12/16/20）未做。

2026-11-15（通灵者大改 阶段 0 修订 + 阶段 1a：共鸣基础值 3、星界灯塔改造）：用户追加「基础值加个 3」，共鸣上限最终为 **3 + 通灵者等级/2 + 魅力调整值**；随后开始阶段 1。

**共鸣上限**：`MediumInfluenceRules.BaseCap` 改成 `Math.Max(0, 3 + level/2 + charisma)`；`Influence.cs` 蓝图 `ResourceAmountBuilder.New(3)`；描述文本同步。测试 `Fresh()` 默认改为 4 级/魅力 0（3+2+0=5），既保住下面所有「上限 5」的容量断言，又新增 6 条公式断言（5/13/17/6/0/null）。

**星界灯塔改造（阶段 1a）**：原来的实现是「20 级特性一旦拥有 → `ApplySpirits.RefreshActiveSpirits` 每轮把**其余全部**职阶的中等+高等+至高之力塞给玩家，全天有效」。现改为：**自由动作 + 2 点共鸣 + 持续 2 轮 + 只借一个当前未降灵职阶**。
- `ApplySpirits` 把 `AddPower`/`RemovePower` 拆成「实例包装 + 静态实现（参数为 `UnitEntityData`）」，并新增 `internal static GrantSecondary(UnitEntityData, ref)` / `RevokeSecondary(...)` / `Entry(...)`；`RefreshActiveSpirits` 里那段「有 AstralBeacon 特性就借全部职阶」的循环已删除。**`Owner` 是 `UnitEntityData` 不是 `UnitDescriptor`**（`UnitDescriptor.Get<T>` 只接受 `OldStyleUnitPart`，`UnitPartMedium` 不是）。
- `SpiritEntry` 查找用**蓝图引用相等**（`pair.Key?.Get() == blueprint`），与 `IsActiveSpirit` 同一判据；不要改成比较 `AssetGuid`，离线测试桩的 `BlueprintCharacterClass` 没有该字段。
- 新增 `MediumAstralBeaconComponent`（buff 组件，字段 `Spirit`，`OnActivate`/`OnDeactivate` 调 Grant/Revoke）。`RevokeSecondary` 在目标职阶**当前已被正式降灵**时直接返回，避免把主英灵的能力一起拆掉。
- `AstralBeacon.cs`：变体技能 + buff + 父技能挂 `AddAbilityVariants`；特性加 `.AddFacts(ability)`。每个变体：`AbilityRequirementHasSpirit` + 新增的 `AbilityRequirementSpiritAvailable`（要求该职阶**未降灵**且已注册）+ `AddAbilityResourceLogic(amount: 2)` + `ApplyBuff(2 轮)`。（阶段 1b 已把变体改为按降灵表自动生成，手写 GUID 与图标常量全部删除，见下节。）
- `MediumTranceOfThreeComponent.MinimumChannelRanks()` 里「拥有 AstralBeacon 特性就保留 6 个 rank」的豁免已删除——灯塔不再授予任何永久 rank。

**测试**：`InfluenceScenarios` 701、`MediumProgressionLevels` 12010、`SpiritChannelLifecycle` **8088/8088**（灯塔场景改写为「只借一个职阶 + 到期收回」）、`SpiritInfluenceScenarios` 576、`SpiritSurgeScenarios` 1090、`MythicRegistration` 48 全 PASS；模块编译 **0 警告 0 错误**。`SpiritSaveRestore` **未运行**：该工程 `Program.cs` 用 `extern alias NativeCecil` 但 csproj 里没有带别名的 Mono.Cecil 引用（`git status` 确认本轮未改动该目录），属既有状态问题。

未打包、未 commit、未 push、未启动游戏/UMM。**阶段 1b 见本节上方（多重降灵 10/14/18 已实现）**。

2026-11-15（通灵者大改 阶段 0：共鸣上限 / 灵力涌动 / 移除四项）：用户提出通灵者整体重做方案，明确「一步一步来」。本轮只做数值与进度表，能力框架留到后续阶段。

**用户拍板的 6 个口径**：① 英灵专长按 **4/8/12/16/20** 共 5 次（正文而非标题的 5 级起）；② 共鸣上限**去掉硬顶**；③ 神话多重降灵「降灵不再额外多承受共鸣」= 第二位英灵**仍 1 点、只是不再递增**；④ 星界灯塔改造后**仍留 20 级**，保留与「坚韧灵魂」的巅峰二选一；⑤ 奥术发现/护甲训练/武器训练三项**未装 TTT-Base 时隐藏**；⑥ 英灵亲和可重复项用 **ranks + 等级前置**控制。

**① 共鸣上限 = 通灵者等级的一半 + 魅力调整值**。关键发现：`MediumInfluenceRules` 里的 `InfluenceCapPatch` 已经 Prefix 掉 `BlueprintAbilityResource.GetMaxAmount` 并对所有共鸣资源直接返回 `MediumInfluenceRules.Cap(owner)`，**蓝图的 `m_MaxAmount` 根本不参与实际上限**。因此真源改为新增的 `MediumInfluenceRules.BaseCap(owner)`：遍历 `Progression.Classes` 累加 `Guids.Medium` 的等级（不走 `BlueprintTool.Get`，避免 UI 路径上抛异常），加 `Stats.GetStat(StatType.Charisma).BonusWithoutTemp`，`Math.Max(0, ...)` 保证负魅力不倒扣。`Cap()` 仍在其上叠加神话共鸣扩容／坚韧灵魂 +3／缚灵者 +1。`Influence.cs` 的蓝图 Amount 同步改成 `ResourceAmountBuilder.New(0).IncreaseByLevelStartPlusDivStep(Medium, levelsPerStep:2, bonusPerStep:1).IncreaseByStat(Charisma)` 并保留 `SetUseMax(false)`，只作为绕过补丁时的兜底。`InfluenceMath.BaseCap = 5` **保留不动**——它只服务于 `Migrate()` 对老存档「剩余点数 → 累计共鸣」的换算，那必须用旧上限 5。

**② 灵力涌动**：`SpiritSurge.cs` 的 `AddAbilityResourceLogic(amount: 1 → 2)`、`ContextDuration.Fixed(5 → 1)` 轮。注意共鸣是**累加**不是消耗：`AddAbilityResourceLogic(isSpendResource: true)` 被 `InfluenceAbilitySpendPatch` 拦下后走 `MediumInfluenceRules.TryAccept`，所以 `amount` 就是「承受几点共鸣」；`CanAccept` 同步要求上限还容得下 2 点。

**③ 移除四项**：`MediumProgression` 删掉 1 级 `WeakerSpiritChannel`、9 级 `MediumPropitation`、15 级 `MediumTranceOfThree`、19 级 `MediumSpiritMastery` 的授予，但**四个 `ConfigureEnabled()` 调用全部保留**——老存档里已拥有这些 fact 的角色需要蓝图继续存在，神话「多重降灵」也仍引用三重降灵的 6 个变体技能。已核实没有第二条授予路径（`UnitPartMedium` 读 `HasFact(SpiritMastery)` 判断每日免费奔涌、`MediumInfluenceRules` 读 `HasFact(Propitation)` 做迁移，两者现在恒为 false，语义与移除一致）。

**测试**（全部离线，不启动游戏）：`InfluenceScenarios` **701 checks PASS**（新增 6 条 BaseCap 公式断言，`Fresh()` 默认 10 级/魅力 0 以保持历史 5 点边界，并补 20 级、20 级+4 魅力、1 级取整、负魅力下限、null owner）；`MediumProgressionLevels` **12010 checks PASS**（对真实 `Assembly-CSharp` 的升级模型验证进度表与迁移恢复）；`SpiritSurgeScenarios` **1090 checks PASS**；`SpiritChannelLifecycle` **7919/7919 PASS**。模块 `dotnet build MediumClass.csproj -c Release` **0 警告 0 错误**；产物反查确认 `BaseCap`、`AddAbilityResourceLogic(2)`、`Fixed(1)` 均已生效。测试桩同步扩了 `BlueprintCharacterClass`/`ClassData`/`Progression.Classes`/`Stats`/`StatType`/`ModifiableValueAttributeStat`，并在 `GuidsModel` 补了 `Guids.Medium`。

未打包、未 commit、未 push、未启动游戏/UMM。**阶段 1-4 未做**：星界灯塔改造、多重降灵（10/14/18）、神话多重降灵改造、英灵亲和 13 项、英灵专长。

2026-11-15（续 6，无图标能力的字母占位缩写全是 "CW"）：玩家问「超凡之弦在职业界面的图标是不是因为没给图标所以都长一样，能不能也用其他无图标能力那种花体英文字母」。核实结论：**回退机制本来就在走，坏的是缩写内容**。

**机制**：`BlueprintUnitFact.Icon => m_Icon`，为 null 时 UI 走 `UIUtility.GetIconByText` + `GetAbilityAcronym`，两者**都只看资产内部名 `name`**（`BlueprintUnitFact.NameForAcronym => name`，非虚属性，无 blueprint 级覆写；`EntityFact` 那套 `UIData` 覆写是运行时的，管不到蓝图）。缩写算法：抽 `name` 里所有大写字母 → 取前 2~3 位；占位底图是 `UIRoot.UIIcons.AbilityPlaceholderIcon` 的 **10 张**按 `name.Length % 10` 取，颜色同理。

**根因**：`PsychicAssets.Add` 统一把资产名写成 `"CotWWrathPsychic" + token`，抽大写字母永远是 `C W W P S C C …`，长度 > 3 被截成前 2 位 → **所有无图标事实的缩写都是 `CW`**。这不只影响超凡之弦，全仓 95 个 `new PsychicAssets(` 的构建器都受影响；超凡之弦只有 3 个能力设了真图标（奥术共鸣、火墙、`TimeStop` 的永恒之歌），其余（施法、选书、逸闻见识、表演、奥术增强之歌、永恒之歌、无限火焰、progression 等）全是 null。

**改动**：① 新增 `src/AcronymPresentation.cs`，对 `UIUtility.GetAbilityAcronym(string)` 加 Postfix，**只认 `CotWWrathPsychic` 前缀**（其余字符串原样返回，原生事实与本地化显示名不受影响）：剥前缀 → 剥已知职业前缀（`SublimeChord`）→ 按驼峰/数字边界分词 → 单字词取前 3 字母、2~3 词取全部首字母、4 词以上取**末 3 个**首字母（`Ability`/`Feature` 这类后缀是兄弟节点共有的，区分度在尾部）；token 的 `/` 之后是身份后缀（`SublimeChordBook/<guid>`），先截断再分词，否则 GUID 会被切成垃圾词。结果缓存进 `Dictionary`（锁保护，键只有本 mod 资产名，规模有界）。资产 `name` 一个字都没动，所以所有 `name == "CotWWrathPsychic"+token` 的身份校验、`ContentIdentity` 比对、`HighlightKey`（读的是未打补丁的 `NameForAcronym`）全部不变。② `SublimeChord.BookFeature` 给每个选书条目设 `m_Icon = book.CharacterClass.m_Icon`：这些条目共用 `SublimeChordBook/<guid>` 形态，缩写只能算出同一个词，用原生职业图标既区分又有意义。

**为什么选 Postfix 而不是改 `name`**：`name` 是承重字段（多个模块用它做幂等/身份判定），改名风险远大于改显示；`NameForAcronym` 是 `=> name` 的非虚 getter，patch 它只能覆盖走属性的路径，而工具提示等路径直接读 `feature.name`，只有 `GetAbilityAcronym(string)` 是所有路径的公共出口。

预期：Casting→CAS、Knowledge→KNO、Performance→PER、ArcaneSong→AS、ArcaneSongAbility→ASA、EternalSong→ES、EternalSongStopAbility→SSA、InfiniteFire→IF、BookSelection→BS、Progression→PRO。占位底图仍按名字长度在 10 张里轮换，字母与底纹都会区分开；3 个已有真图标的能力不变。

编译 `dotnet build -c Release -p:WrathPath="D:\steam\steamapps\common\Pathfinder Second Adventure"` **0 警告 0 错误／5.94 秒**。未打包、未 commit、未 push、未启动游戏/UMM、未做玩家验收；需用户出包看职业界面与能力栏的字母是否已经分开。

2026-11-15（续 5，通用法术书识别扩展到全部进阶职业 + 菜单清理为单一开关）：用户要求把超凡之弦 pilot 的「自动发现所有法术书（含第三方 mod）」扩展到所有进阶职业，并把菜单里「注册是否失败」的显示全部清除，只留一个通用法术书识别开关。

**扩展实现**（新增 `src/SpellbookRecognition.cs`）：**按结构自动发现**所有「选择基础施法职业」的选择器，不写 GUID 清单——判据是 `m_AllFeatures` 非空、不超过 200 项、**全部**候选都是法术书变体（`BlueprintFeatureReplaceSpellbook` 或 `BlueprintProgression` 且其 level-up feature 含 `AddSpellbookLevel`），且**至少一个候选是该职业的默认法术书**（`CharacterClass.Spellbook == book`）。最后一条用于排除 `ThassilonianSchoolSelection f431178e…` 这类「同一职业的学派变体选择」——实测原生按此判据正好识别 7 个：Arcane Trickster `ae04b7cd…`、Dragon Disciple `8c1ba14c…`、Eldritch Knight `dc3ab8d0…`、Loremaster `7a28ab4d…`、Mystic Theurge 奥术 `97f510c6…` 与神术 `7cd05794…`、Winter Witch `ea20b26d…`；第三方同样覆盖，已核实 **Prestige-Plus** 自建的两个通用选择器 `Spellupgrade E995A687…`（5 个 progression）与 `Spellupgrade2 05DC9561…`（50+ 个 feature，已含 ExpandedContent／CharacterOptionsPlus／MCE／TTT 的法术书）都会命中。**两种模板形态**：① 奥术侧候选是 `BlueprintFeatureReplaceSpellbook`（字段 `m_Spellbook`）；② 神术侧候选是 `BlueprintProgression`，其 `LevelEntries[].Features` 里的 level-up feature 挂 `AddSpellbookLevel.m_Spellbook`。两者都逐法术书 clone：奥术 clone 后替换 `m_Spellbook`、用通用 `Retarget` 重定向组件里的法术书引用／职业引用／**字符串 GUID**（Prestige-Plus 的 `PrerequisiteSpellBook.Book` 是 `string` 而非引用对象）、再追加自定义 `SpellbookOwnedPrerequisite`（精确拥有该书才显示，避免同职业变体刷屏）；神术 clone 后用 `CopyData` 深拷贝 `LevelEntries`（`MemberwiseClone` 只深拷贝 `ComponentsArray`，条目对象仍是共享的）、改写 `m_Classes[0].m_Class` 并保留 `AdditionalLevel`、按源 feature GUID 只 clone 一次 level-up feature、替换其 `AddSpellbookLevel.m_Spellbook`，且**每个 clone 的 level-up feature 都必须 `AddCachedBlueprint` 提交**，否则 progression 引用会解析为 null。注入判据：非神话、`SpellsPerDay != null`、`CharacterClass != null`、非进阶职业、且是该职业默认书；按 `m_Spellbook` GUID 去重，任何 picker 已列出的书都不重复注入；奥术/神术按模板自身法术书的 `IsArcane` 分组注入（混合 picker 两半都处理，不再只取第一个模板）。late provider 仍走 `AddCachedBlueprint` 标记 + `get_AllFeatures` 惰性重扫，另有 `KnownPickers` 缓存与 10 次扫描上限，无定时器/轮询。

**开关**：新增 `SpellbookRecognitionSettings`，持久化到 mod 程序集同目录的 `spellbook-recognition.json`（`{"enabled":true}`），**默认开启**；注册发生在 `BlueprintsCache.Init` Postfix，早于设置页构建，因此不能用 ModMenu 的 `SettingsEntity` 作为真值来源，Toggle 只作 UI 代理，切换后写回文件、**重启生效**。`ModMenuIntegration` 用反射解析 `ModMenu.Settings.Toggle.New(string,bool,LocalizedString)`、`Toggle.OnValueChanged(Action<bool>)`、`SettingsBuilder.AddToggle(Toggle)`。

**菜单清理**：`ModMenuIntegration` 删除全部模块状态行、版本行与「Write log」按钮，以及对应的 `Button` 反射签名；`TryRegister` 去掉 `ModuleStatus[]` 参数，`ModEntry.RegisterMenu` 同步简化。`ModuleCatalog` 保留（内部仍用于 `ReportModule` 的状态去重），只是不再出现在菜单里。

编译 `dotnet build -c Release -p:WrathPath="D:\steam\steamapps\common\Pathfinder Second Adventure"` **0 警告 0 错误**；TypeId 全仓 419 个，0 非法、0 重复（新增 `d1c…0e SpellbookOwnedPrerequisite`）。未打包、未 commit、未 push、未启动游戏/UMM、未做玩家验收；需用户出包验证：① 各进阶职业选择列表出现第三方法术书；② 关闭开关并重启后不再出现；③ 升级界面不因新候选刷屏。

2026-11-15（续 4，修复 9 级诗人能力栏失效/卡顿与设置菜单卡顿，均据玩家日志定位）：玩家报两个新症状，日志路径 `C:\Users\Administrator\AppData\LocalLow\Owlcat Games\Pathfinder Wrath Of The Righteous`。

**① 9 级诗人时能力栏全部变灰、法术栏消失、游戏极其卡顿**。证据在 `GameLogFull.txt` 时间 512.4364：紧接 `Action bar slot added automatically: InspireGreatnessToggleAbility`（Bard 9 级解锁）与 `ArcaneConcordanceAbility` 之后抛出 `NullReferenceException`，堆栈为 `ActivatableAbilityResourceLogic.get_IsFreeBecauseOfFact_Patch1` → `ActivatableAbility.get_ResourceCount_Patch2` → `MechanicActionBarSlotActivableAbility.GetResource_Patch1` → `ActionBarSlotVM.SetMechanicSlot` → `ActionBarVM.SetMechanicSlots_Patch1` → `OnUnitChanged_Patch2` → `UpdateSelection` → UniRx `MainThreadDispatcher.Update`。根因是 `src/CourtBard01445.cs` 的 `CourtBardFreePerformance01445Patch.Prefix`（全仓唯一 patch 该 getter 的代码）直接解引用 `__instance.Fact.Owner` 并调用 `owner.Progression.Classes.Any(c => c.Archetypes.Any(...))`：在 UI 预览/蓝图级上下文或 `Archetypes` 为 null 时抛 NRE，异常打断 `SetMechanicSlots`，能力栏无法构建（全部变灰）、法术栏随之不生成，UniRx 每次行动栏更新重抛。修复：整个 Prefix 包 `try/catch` + 完整 null 守卫（`Fact?.Owner`、`owner?.Unit`、`Progression`、`cls?.Archetypes`），接管后任何异常一律 `__result=false; return false`（安全「不免费」），仅在探测阶段异常时 `return true` 交回原生。附带修正：原生 `IsFreeBecauseOfFact` 在 `m_UseListForFreeBlueprints=true` 且 `m_FreeBlueprints=null`（DarkCodex `SetResourceDecreasing` 造成）时会在 `freeBlueprints.Length` 抛 NRE，原实现对**非 Court Bard** 角色 `return true` 仍会崩，现在统一安全返回。

**② 打开游戏选项菜单卡一会**。证据在 `Modifications/CallOfTheWildWrathPrototype_Log.txt` 尾部堆栈：`ModEntry.ReconcileOptional()` ← `ModEntry+SettingsOpeningPatch.Prefix()` ← `SettingsVM..ctor_Patch1` ← `CommonVM.HandleOpenSettings` ← `EscMenuVM.OnSettings`。根因是 `SettingsOpeningPatch` 作为 `SettingsVM` 构造函数 Prefix，每次打开设置菜单都调用 `ReconcileOptional()` → `BlueprintRegistration.RegisterOptional()`，后者无条件执行约 40 次 `cache.Load(...)` 强制蓝图加载并重跑失败 provider（日志中 Mindblade 的 `01454 published resource changed` 每次重试）；`RegistrationRetry01444` 只对已成功模块幂等，挡不住这些探针。修复：`ReconcileOptional()` 增加 `optionalPasses` 节流——首次必跑，之后仅当 `RegistrationRetry01444.PendingCount != 0` 才重试，最多 3 次；已无 pending 时打开设置菜单直接跳过。`TryMenu()` 保留（ModMenu 下拉框需要）。

**附注**：日志中的 `01454 published resource changed: CotWWrathPsychicMindbladeWeaponHeavyMace01454` 是既有误报——`MagusBatch01454.Existing` 的序列化比对把 Unity 运行时填充的 `m_CachedBeltModel`／`m_CachedSheathModel`／`m_CachedEquipLinksUpToDate` 也算进去，导致 Mindblade 可选模块每次注册失败。本轮**未改**该比对逻辑（超出本次两个症状范围），只是不再随设置菜单反复重试；如需修可选模块注册可另起一轮。

编译 `dotnet build -c Release -p:WrathPath="D:\steam\steamapps\common\Pathfinder Second Adventure"` **0 警告 0 错误／6.12 秒**。未打包、未 commit、未 push、未启动游戏/UMM、未做玩家验收；两项均需用户再次出包复测（重点：Bard 9 级能力栏/法术栏恢复、连续多次打开设置菜单不再卡）。

2026-11-14（续 3，超凡之弦变体演艺叠加与表演 DC）：玩家确认「诗人/Skald 超凡之弦叠加等级计算加值现在正常了」，但反馈两项残留 + 一项新要求：① Skald 变体 Court Poet 的 Insightful Contemplation、② 本仓库 Urban Skald 的 Controlled Inspired Rage 仍没按叠加等级计算；③ 这类表演能力的 **DC** 也应把超凡之弦算作诗人/Skald 等级。本轮只读核实后落地，**不动已确认正常的普通演艺加值路径**。

**根因核实**：Court Poet `InsightfulContemplationSongEffectBuff` 的两个 `ContextRankConfig` 都是 `ClassLevel` + `m_Class=[Skald]` + `m_Progression=Custom`（3/7/11/15/18/20 → 1..6 与 7/15/20 → 1..3），位于深层效果 buff；旧 `RankPatch` 只对 `MaxClassLevelWithArchetype` 做直接兜底，未被 `ReachableRanks` 发现的 rank 完全拿不到 SC 等级。Urban Skald 的 `Controlled Inspired Rage` 从原生 `InspiredRageEffectBuff`／Mythic `f.Clone` 深拷贝，其 `ContextRankConfig` 实例与原生蓝图不同，`Reconcile` 往原生 `m_Class` 追加 SC 根本到不了克隆；且 `UrbanSkaldAllocationRestriction01442.IsAvailable()` 直接读 `GetClassLevel(Skald)` 决定 8／16 级 allocation 预算，完全绕过 rank。DC 侧：`ContextCalculateAbilityParamsBasedOnClass.Calculate()` 在 `Rulebook.Trigger` **之前**就写入 `ReplaceCasterLevel = GetClassLevel(cls)`／`ReplaceSpellLevel = GetClassLevel(cls)/2`，`RuleCalculateAbilityParams.OnTrigger()` 再用 `10 + spell level + stat bonus` 算 DC，rank patch 触及不到（全库含 Bard/Skald 的该组件原生命中 15 个：Fascinate、Frightening Tune、Thundercaller 各区域、Battle Singer 三诗节、Herald of the Horn Crumbling Blast，加上 Urban Skald 的 Infuriating Mockery 与第三方等价物）。

**改动**（`src/SublimeChordPerformances.cs`、`src/UrbanSkald01443.cs`）：① `RankPatch.ShouldScale()` 统一处理 4 种 class-level rank（`ClassLevel`／`MaxClassLevelWithArchetype`／`SummClassLevelWithArchetype`／`OwnerSummClassLevelWithArchetype`），并新增 fallback——owner 为 `BlueprintBuff`／`BlueprintAbilityAreaEffect` 且 `m_Class` 含 Bard/Skald GUID 时也补 SC 等级，覆盖漏发现、late provider 与深拷贝克隆；若 `m_Class` 已被 `Reconcile` 追加 SC GUID 则跳过，避免双重计数。因 patch 打在 `GetBaseValue`（`GetValue` 里 `ApplyProgression` 之前），Court Poet 的 `Custom` 进度表能看到合并等级。`Reconcile` 对求和型 rank 仍保留幂等 `AppendClass`，玩家已确认正常的普通加值路径不变。② 新增 `[TypeId d1c…0d] SublimeChordPerformanceDC : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAbilityParams>`，挂「吟游演艺」feature，在 `OnEventAboutToTrigger` 只对 blueprint 带 Bard/Skald `ContextCalculateAbilityParamsBasedOnClass` 的事件把 `ReplaceCasterLevel` 补 SC 等级、`ReplaceSpellLevel` 设为 `(Bard/Skald + SC)/2`。③ 新增共享 `SublimeChordPerformances.BonusLevels(UnitDescriptor)`（SC 未注册返回 0），`UrbanSkaldAllocationRestriction01442.IsAvailable()` 预算等级改为 `GetClassLevel(Skald) + BonusLevels()`，8／16 阈值按合并等级判定。④ 清理 `PerformanceRanks` 过时注释。

全库只读扫描：含 Bard/Skald 的 `ContextRankConfig` 70 个（ClassLevel 61／MaxClassLevelWithArchetype 7／Summ 1／CharacterLevel 1，`m_ExceptClasses` 全 false）；`ContextRankBaseValueType.ClassLevel` 的 `GetBaseValue` 已确认遍历 `Progression.Classes` 对 `m_Class` 匹配项**求和**，故 `AppendClass` 语义正确。编译 `dotnet build -c Release -p:WrathPath="D:\steam\steamapps\common\Pathfinder Second Adventure"` **0 警告 0 错误／1.55 秒**（增量）；TypeId 扫描 0 非法、0 重复（src 373 + Modules 45 = 418）。未打包、未 commit、未 push、未启动游戏/UMM、未做玩家验收；3 项均需用户再次出包复测。

2026-11-14（续 2，超凡之弦实机反馈五连修复 + 60 尺墙形 + Skald 13 级迅捷动作）：玩家出包实机确认注册已通过（`Registered: Sublime Chord`），随后反馈 5 个问题；本轮按「先把引擎契约核实清楚」再落地，只读核对了 `BlueprintAbilityAreaEffect`／`AreaEffectView.SpawnFxs`／`ContextActionSpawnAreaEffect`→`MechanicsContext.CloneFor` 共享值复制／`IsSuitableTargetType` 友军过滤／`AddFeatureOnClassLevel.CalculateClassLevel` 求和语义／原生 `TimeStopEffectBuff` 组件结构／`EntityFact.Setup` 组件名去重。改动集中在 `src/SublimeChord.cs` 与 `src/SublimeChordPerformances.cs`：

1. **变体法术书刷屏**：`Eligible()` 原先放出 33 个非神话奥术法术书（Wizard 9 个变体、Sorcerer 4 个、Arcanist 4 个等），且 `SublimeChordBookPrerequisite` 只比 `book.CharacterClass`，同职业所有变体一起可选。改为：前置先按 `unit.Spellbooks` 精确匹配实际拥有的法术书实例（仅当职业默认法术书 + 职业存在时才回退）；新增 `NarrowBooks()` 在 `get_AllFeatures` Postfix 里把 `m_AllFeatures`/`m_Features` 与 `__result` 一并收窄到**该升级角色实际拥有的法术书**，用 `Kingmaker.Game.Instance.LevelUpController.Preview/Unit`。候选 feature 名称改用 `BlueprintCharacterClass.Name` 而非内部资产名。`Eligible()` 去掉 `CharacterClass != null` 限制以保留第三方 mod 兼容性（收窄改为按角色，不按蓝图）。`SelectionOpenedPatch` 修掉原短路，前 6 次打开选择界面各做一次幂等 `Reconcile`，之后由 `AddCachedBlueprint`（含 `BlueprintBuff`/`BlueprintFeature`）的 `Pending` 驱动重扫；`Reconcile` 改为只取一次快照。
2. **永恒之歌无效**：手写 6 个冻结组件 `name == ""`，`EntityFact.Setup` 按 `SourceBlueprintComponentName` 去重后只创建第一个运行时组件。改为 `f.Clone` 原生 `TimeStopEffectBuff 8fff364a…`（6 个组件与手写集合一致但各有唯一 name，且带 `StayOnDeath`），并给 `SublimeChordCasterLevelLink`、`SublimeChordPerformanceRounds` 及新增组件补唯一 name。
3. **无限火焰之歌改 60 尺墙形**：移除 20 尺 `AbilityTargetsAround`，新建 `BlueprintAbilityAreaEffect`（`Shape=AreaEffectShape.Wall`、`Size=60ft`、`Fx=PrefabLink{AssetId=098a29fefbbc4564281afa5a6887cd2c}` 即 ExpandedContent Wall of Fire 特效、`m_TargetType=Enemy` 排除友军），`AbilityAreaEffectRunAction.UnitEnter` 用 `ContextActionSavingThrow`+`ContextActionDealDamage(HalfIfSaved)` 结算；技能本体改为点选（`CanTargetPoint=true`，其余 CanTarget* 关掉，`EffectOnAlly=None`），用 `ContextActionSpawnAreaEffect` 生成**1 轮即消失**的火墙。沟通检定 ×2 与 DC 在技能动作列表里算好写入共享值，`CloneFor` 会把 `m_SharedValues` 复制进区域上下文，故 `UnitEnter` 能直接读 `Damage`/`Duration`。
4. **演艺加值只有 1**：`ReachableRanks` BFS 深度 3→8（Inspire Courage 的 rank 在第 3 层 `InspireCourageEffectBuff`，刚好被切掉），并加 `result<4096 / seen<8192` 硬上限；`BookPublishedPatch` 补 `BlueprintBuff`/`BlueprintFeature`；`RankPatch` 增加不依赖发现集合的直接兜底——`MaxClassLevelWithArchetype` 且 `m_Class` 含 Bard／Skald GUID 时补超凡之弦等级（已核实原生只有 7 个此类 rank，全为演艺效果）。全库扫描确认 Bard/Skald 演艺相关 rank 为 22 个 `ClassLevel` + 5 个 `MaxClassLevelWithArchetype`。
5. **7/13 动作能力**：新增 `SublimeChordActionFeatureGrant`（`IOwnerGainLevelHandler`），挂在「吟游演艺」feature 上，按 `基础职业等级 + 超凡之弦等级` 求和，7 级给移动动作、13 级给迅捷动作，诗人与 Skald 各一套（Skald 13 级迅捷按用户本轮拍板一并给）。用自定义幂等组件而非原生 `AddFeatureOnClassLevel`：原生动作能力是 `Ranks=1` 的 LevelEntry 授予，直接套原生组件会在高基础职业等级时重复加 rank，`OnDeactivate` 还可能移除原生副本。

编译 `dotnet build -c Release -p:WrathPath="D:\steam\steamapps\common\Pathfinder Second Adventure"` **0 警告 0 错误／4.97 秒**；TypeId 全仓 368 个，32 位合法、无重复。未打包、未 commit、未 push、未启动游戏/UMM、未做玩家验收；定向检查与游戏内验收均未运行，需用户再次出包复测这 5 项。上一条记录中的「注册是否真正通过仍需用户出包确认」已由玩家实机确认解除。

2026-11-14（续，超凡之弦注册失败修复 + 纯 Skald 双池 + 演艺互斥）：按玩家日志 `C:\Users\Administrator\AppData\LocalLow\Owlcat Games\Pathfinder Wrath Of The Righteous\Modifications\CallOfTheWildWrathPrototype_Log.txt` 定位注册失败根因：`[188.6198] Registration failed: Sublime Chord` → `System.ArgumentException: Object of type 'Kingmaker.Blueprints.BlueprintAbilityReference[]' cannot be converted to type 'System.Collections.Generic.List\`1[Kingmaker.Blueprints.BlueprintAbilityReference]'`，堆栈 `PsychicAssets.Set` ← `SublimeChord.MergedList` ← `SublimeChord.Register`。`SpellLevelList.m_Spells` 声明为 `List<BlueprintAbilityReference>`，上一轮写成 `.ToArray()`，运行时 `SetValue` 类型不符直接抛。已改为 `.ToList()`。**额外静态复核**（避免修一处又炸下一处）：全量反编译 `Assembly-CSharp.dll`，确认 `HarmonyLib.AccessTools.Field` 用 `FindIncludingBaseTypes` 遍历基类、`BlueprintScriptableObject.GetComponent<T>/GetComponents<T>` 用 `is T` 匹配（自定义子类组件可被原生识别）；逐一核对 `MergedList` 之后所有反射字段的声明与精确类型（`BlueprintSpellbook.m_SpellsPerDay/m_SpellsKnown/m_SpellList/m_CharacterClass`、`BlueprintCharacterClass.HitDie/m_BaseAttackBonus/m_*Save/m_StartingItems/m_EquipmentEntities/m_Difficulty/m_Archetypes/m_Spellbook/m_Progression`、`BlueprintFeatureSelection.m_AllFeatures/m_Features`、`BlueprintProgression.m_Classes/m_UIDeterminatorsGroup`、`ProgressionRoot.m_CharacterClasses`、`BlueprintUnitFact.m_Icon/m_DisplayName/m_Description`、`AbilityTargetsAround.m_Radius/m_TargetType/m_Condition/m_SpreadSpeed`、`ContextRankConfig.m_BaseValueType/m_Class/m_Progression/m_ExceptClasses`、`BlueprintAbilityResource.m_MaxAmount`、`BlueprintsCache.m_LoadedBlueprints/m_Lock` 等）均存在且类型匹配。

纯 Skald 适配（`src/SublimeChordPerformances.cs`）：新增 `SublimeChordPerformanceCost : AbilityResourceLogic`（`BardPool`/`SkaldPool` 两个引用），按 `UnitAbilityResourceCollection.ContainsResource` / `HasEnoughResource` 在运行期选择角色实际拥有且付得起的池，override `IsAbilityRestrictionPassed` 与 `Spend` 从该池扣除；同时用两个按组件类型严格守卫的 Harmony Postfix 重定向 `AbilityData.get_RequiredResource` / `get_Resource`——因为 `AbilityCastRateUtils.GetSpellResourceAndCost` 读 `RequiredResource` 计算可用施法次数，不改的话纯 Skald 永远算出 0 次、技能栏直接变灰。双池职业（Bard/Skald 兼职）优先用付得起的那一池。永恒之歌与无限火焰之歌都换用该组件（1 轮 / 2 轮）。

演艺互斥：永恒之歌施放时由 `SublimeChordStopOtherPerformances` 关闭角色身上所有已开启的演艺开关；`[HarmonyPatch(ActivatableAbility, "OnDidTurnOn")]` Postfix 在任意演艺开关开启时调用 `StopEternalSong` 解除冻结。识别判据为 `BlueprintActivatableAbility.Group == BardicPerformance` **或**组件 `ActivatableAbilityResourceLogic.m_RequiredResource` 命中两套资源池之一——后者覆盖 `Group=None` 的 Deadly Vibrato 及第三方 mod 新增的演艺，不依赖启动期的发现结果。Patch 只对拥有超凡之弦等级的角色生效，不改变其他角色的原生行为。手动「停止永恒之歌」按钮保留，并与自动解除共用同一条移除冻结路径。

编译 `dotnet build -c Release -p:WrathPath="D:\steam\steamapps\common\Pathfinder Second Adventure"` **0 警告 0 错误／5.96 秒**。未打包、未 commit、未 push、未启动游戏/UMM、未做玩家验收；注册是否真正通过仍需用户出包进游戏确认。**剩余限制**：通用法术书识别仍是超凡之弦 pilot；永恒之歌解除冻结靠施法者周围 200 尺扫描，目标被拖出该范围时无法解除（既有近似）；未做玩家验收。上一条源码记录末尾列出的「纯 Skald 未适配」「演艺互斥未模拟」两项本轮已解除。

2026-11-14（**超凡之弦 Sublime Chord，全新原创内容、不是 CotW 移植**）：用户明确开启「此前没有 mod 做过的完全新内容」阶段，本轮不再查看 Kingmaker 原版 CotW 源码，游戏内文本用中文。新增 `src/SublimeChord.cs`（10 级进阶职业：d6／4+智力技能点／中 BAB／低强韧与反射／高意志，前置为知识神秘 10、沟通 10、察觉 6、能施 3 环奥术、拥有任一 Bardic Performance 类能力；独立魅力自发法术书，4–9 环每日法术与可知法术按用户 10 行数值表；合并 Bard + Wizard 法术表，只在 ≥4 环取最低环，实测 185 个法术）与 `src/SublimeChordPerformances.cs`（逸闻见识、演艺叠加方案 B、奥术增强之歌／永恒之歌／无限火焰之歌）。**自动法术书目录只做超凡之弦 pilot**：从缓存快照发现非神话奥术法术书，用 `AddCachedBlueprint` 增量登记 + `get_AllFeatures` 惰性重扫，候选 feature 只挂施法者等级联动与职业前置，**不挂 `AddSpellbookLevel`**（否则会给选中职业额外法术位，违反「只叠加 CL、不给更多可知法术」）。演艺按用户选定方案 B：求和型 `ContextRankConfig.m_Class` 幂等追加超凡之弦，7 个 `MaxClassLevelWithArchetype` 型用 `Priority.Last` 的 `GetBaseValue` Postfix 补等级；两套资源池（BardicPerformanceResource／RagingSongResource）均以 `m_RequiredResource` 为判据自动发现，覆盖 Bard 演艺与 Skald 怒歌，不手写 GUID 清单，不改动 Exemplar 的 rank 结构。三个歌曲用 `AbilityResourceLogic` 消耗吟游演艺轮数（永恒之歌 1 轮、无限火焰之歌 2 轮），无限火焰之歌用 `Once` 守卫保证一次沟通检定贯穿整次 AoE，伤害为检定结果 ×2、`HalfIfSaved` 反射减半、`TargetType.Enemy` 不打友军；永恒之歌用原生 Time Stop 效果组件集合冻结、自定义 DC = 10 + 职业等级 + 魅力调整，并以「停止永恒之歌」能力近似「持续演奏维持」。图标：时间停止取原生 `Spells/DLC5NewSpells/TimeStop.jbp`，Wall of Fire／Arcane Concordance 按蓝图名从 ExpandedContent 运行时取，缺失时留空。编译 `dotnet build -c Release -p:WrathPath=...` **0 警告 0 错误／5.71 秒**（末次）；未打包、未 commit、未 push、未启动游戏/UMM、未做玩家验收。版本仍为 0.14.104，仅源码阶段。**当时的已知限制**（前两项已于本轮解除）：三个新歌曲只消耗 BardicPerformanceResource，纯 Skald 怒歌出身角色需下一轮适配；「不能同时维持两种表演」的互斥未模拟；通用法术书识别按用户要求先只做本职业 pilot，验证可行后再扩展到全部施法进阶职业。

0.14.76 修 Kinetic Knight Blade Rush 与 Weapon Shift 范围，本地包已生成（未上传、未 commit）。**Blade Rush 未生效根因（日志证据）**：`Player.log` 里我们的注册在 985 行附近（`Modifications/CallOfTheWildWrathPrototype_Log.txt` 记 `Initial registration complete.`、无 Blade Rush 行，说明 `RegisterAvailableModule` 返回 false 被静默跳过），而 DarkCodex 在第 15550 行才 `[DarkCodex] Loading Kineticist.CreateBladeRushInfusion`；`RunAvailable` 对「provider 未就绪」不登记 pending，故永不重试。修复：`KineticKnightBladeRush` 改为首次取不到时设 `Waiting`，由 `[HarmonyPatch(BlueprintsCache, "AddCachedBlueprint")]` 监视，Blade Rush GUID 一进入缓存即挂载并 `report(ModuleName,true)`；缺 provider 时仍完全不改 archetype。Weapon Shift 的 `Source()` 判定由 `WildShapeFeats.InShape` 改为 `UnitBody.IsPolymorphed`（前置链未动），描述改为 "During any polymorph or wild shape"。编译 0 警告 0 错误；打包总 27.265 秒。包 `D://clew//work//release-staging//0.14.76//6d455affa8474aaf8529a5e947d61603//CallOfTheWildWrathPrototype-0.14.76-experimental.zip`，SHA256 `00BE9132A5878A34452D9C2736023C4585F96954A119D6534626547F6E97E0CF`；包内 23 条，主 DLL 含 `KineticKnightBladeRushWatcher`。另诊断：接触攻击对友军自动命中／伤害性 touch 不能选友军均属原版（`AbilityDeliverTouch` 的 ally 分支与 `CanTargetFriends=false`），已扫已装 mod 源码无对应改动；能力栏残留最可疑的是 `PrestigePlus.FixNoToybox2` 在 `ClassProgressionVM.Dispose` 的 `Sequence contains no elements` 与 `CharacterOptionsPlus.UnitPartExpandedSpellList` 的 NRE（日志重复刷屏）。未上传、未 commit。

0.14.75 Wild Armor / 武器特性 / Versatile Performance / Blade Rush TEST 本地包已生成（尚未上传、未 commit）。按用户 0.14.74 实测反馈：① VP 加值改为桌面规则纯半级（原为上游 `OnePlusDiv2`，2 级 2 点／12 级 7 点）；② Martial Performance 的武器名改用 `LocalizedTexts.Instance.WeaponCategories.GetText`，取不到才回退枚举名；③ 新增 `src/KineticKnightBladeRush.cs`：原版 Kinetic Knight 变体 3 级追加 DarkCodex `KineticBladeRush`（`350b164b…`，能力 `e6b56916…`），提供方缺失时 `RegisterAvailableModule` 静默跳过、archetype 保持原样，幂等与回滚按 LevelEntry 副本处理。编译 25.281 秒、包检查 0.579 秒、总 26.070 秒，0 警告 0 错误。包 `D://clew//work//release-staging//0.14.75//0c190dfc7e824a20b4416e9f8e3082e6//CallOfTheWildWrathPrototype-0.14.75-experimental.zip`，SHA256 `BBB003AD4CE56E83558C0497D503800EBCDC4E1BB47F0333210724004EFB1E9B`；主 DLL `41F9F8B3…`、MediumClass.dll `7E2E0D6E…`、Way of the shield.dll `F2AB0136…`；包内 23 条、4 个 loader 空目录齐全，主 DLL 含 KineticKnightBladeRush／VersatilePerformance0600／WeaponPropertyEnchants0296／WildArmorPolymorphApplied01465，README 顶部为 0.14.75。未运行定向检查、未启动游戏/UMM、未做玩家验收、未上传、未 commit。

0.14.74 Wild Armor / 武器特性 / Versatile Performance TEST 本地包已生成（尚未上传、未 commit）。本包新增 COTW-01465 Wild armor 附魔、COTW-0296 武器类型 disarm/trip/sunder 特性（含 Weapon Shift 适配）、COTW-0600 Versatile Performance／Expanded Versatility／Martial Performance。打包前修复了 `b55fa17` 删除 `README.zh-CN.md` 但 `build.ps1` 仍引用该文件导致的打包中断（从 `5f1729b` 恢复文件，并按本轮更新顶部说明）。编译 21.495 秒、包检查 0.547 秒、总 22.249 秒，0 警告 0 错误。包 `D://clew//work//release-staging//0.14.74//713d4b4e709849d0a2746ae51726e3a5//CallOfTheWildWrathPrototype-0.14.74-experimental.zip`，SHA256 `E107209DF24DE846F0B485CC03C54B543157FA4A4A3E1C7D4C4A48D2EBFF38DF`；主 DLL `92025AF2…`、MediumClass.dll `CC9FB2DE…`、Way of the shield.dll `F2AB0136…`，与构建产物一致；包内 23 条、4 个 loader 空目录齐全，主 DLL 含三个新类型，README 顶部为 0.14.74。未运行定向检查、未启动游戏/UMM、未做玩家验收、未上传、未 commit。

[0.14.73 Cryptid Scholar TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.73-Cryptid-Scholar-TEST)已在 GitHub 预发布，4/4 附件下载一致。版本提交 `5f1729b`，编译 26.062 秒、打包与 DLL 核对 1.816 秒、GitHub 上传 4.902 秒、分段下载回读 27.125 秒。复用 244 条定向和 D3 的 5/5，未重复跑同内容。[测试说明](docs/releases/0.14.73-TEST.md)

0.14.72 Psychic Detective TEST 已在 GitHub 预发布，四个附件下载一致。306 条本项隔离定向／14.778 秒。[测试说明](docs/releases/0.14.72-TEST.md)

0.14.71 Iroran Paladin TEST 已在 GitHub 预发布，4/4附件下载核对一致。D复查通过，105条隔离定向／1.23秒。见[测试说明](docs/releases/0.14.71-TEST.md)。

2026-11-10（COTW-0117 后续修复：原版 Manifestation 缺陷 + 内置模块补丁，玩家实机确认）：三个症状的根因与前两轮的猜测不同。① Owlcat 原版数据缺陷：Flame/Frost/Stone/Waves/Wind 五个魂域 L20 `Manifestation` 的 `ActivatableAbilityVariants.m_Variants` 均为 `[Extend, Bolster, Bolster]`（Bolster 重复、Reach 缺失），而同层 `AddFacts` 实际授予 Extend/Bolster/Reach；普通萨满同样受影响，玩家实测确认。按用户决定**连原生一起修**：注册时从 `AddFacts` 重建父能力变体列表，幂等且带失败回滚，共享蓝图的全局改动不会在注册失败时残留。② 双 Touch of Flames 是原生 Flame L1 有两个同名 `AddFacts` 组件，游戏靠同名折叠，而 `UniqueNames()` 改名后两个都生效；改为在改名之前按「组件名 + fact GUID」去重组件。③ 开启超魔后所有法术变暗与 Spirit Whisperer 无关：内置模块 `MediumClass.Prowler.ShapeQuickenPatch` 无条件读 `AutoMetamagic.Owner`，在 UI／可用性计算的蓝图级上下文抛 `ComponentRuntime is unavailable`，经 `UnitPartSpecialMetamagic.GetMetamagic` → `AbilityData.CanBeCastByCaster` 污染全部法术判定；补丁整段加 try/catch 放行原生逻辑，Prowler 原语义不变。证据（两份独立原生导出、Player.log 堆栈、Kingmaker 原版 CotW `Flame.cs:375-389`）与影响面见 [Spirit Whisperer 交付说明](docs/spirit-whisperer/README.md) 的「原生游戏缺陷修复」节。编译 0 警告 0 错误 9 秒；打包编译 23.667 秒／包检查 0.602 秒／总 24.483 秒，0 警告 0 错误。本地包 `D:\clew\work\release-staging\0.14.69\d978d92e6608468dbe38460e65152a27\CallOfTheWildWrathPrototype-0.14.69-experimental.zip`，SHA256 `0C1EDFCA75E438661CC3BF1BC0570DA01EA799E9FC423BBE56EF73C46EB5D350`；主 DLL `0087FB88…`，MediumClass.dll `673C8045…`（与合并产物一致）。用户 2026-11-10 实机确认三处症状全部恢复正常；本机未启动游戏/UMM、未跑离线测试宿主、未 commit/push/上传。此项是定向玩家确认，不等于全魂域／全职业／完整存档验收；当前预发布仍为 0.14.69 Combined TEST，正式稳定版 0.14.66 不变。

0.14.70 Spirit Guide TEST：D3复查通过，320条本项定向/15.00秒，GitHub交付已完成，4/4附件下载核对一致。正式0.14.66和旧TEST保留。见[本次说明](docs/releases/0.14.70-TEST.md)。只做常规编译/打包/包内DLL核对，复用同内容结果，不跑全量或历史专项。

0.14.66正式稳定版保留。历史预发布 **[0.14.69 Combined TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.69-Combined-TEST)**（程序集0.14.69.0）：Spirit Whisperer、Vindictive Bastard／Menacing、Sacred Servant／Live On统一测试包，保留已有Havocker／Mimic／Starseeker和内置模块；不含待修复的Speaker。编译29.737秒、常规包检查2.157秒，已有同内容专项未重跑。未测与已知边界见docs/releases/0.14.69-TEST.md。正常入口仍是build.ps1。

历史源码阶段（现已纳入0.14.69）：Spirit Whisperer COTW-0117已完成A/C/B/D和最终源码整合，尚未发布新安装包。沿用174条／37.59秒隔离检查，合并编译8.138秒、0警告／0错误。未测及已知边界见docs/spirit-whisperer/README.md；当前GitHub安装包仍为原0.14.68。

历史源码阶段（现已纳入0.14.69）：main另已合入sxl1216的PR #5（Vindictive Bastard／Menacing）与PR #8（Sacred Servant／Live On），尚未新出安装包。沿用用户认可的贡献者测试，本机仅完成必要静态整合、编译和DLL内容核对，不宣称亲测游戏；旧0.14.68附件及版本未变。记录见docs/contributions/20261109-integration.md。

2026-11-08：当前开发预发布为 **0.14.68 Starseeker TEST**（程序集0.14.68.0），[GitHub下载](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.68-Starseeker-TEST)。完整变体已完成开发和D复查；先前本项隔离定向536条断言通过、48.36秒；版本更新没有重跑同内容专项。实机／完整存档等边界仍未验收。施工占用与已完成条目统一看 [WORK_STATUS.md](WORK_STATUS.md)。正式稳定版仍为0.14.66，旧附件保留。


## sxl1216 分支已完成功能与协作边界

2026-11-07：当前预发布为 **0.14.67 TEST**（程序集0.14.67.0），包含Havocker、Spellburn图标接线及Mimic展示更新。[下载测试包](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.67-Havocker-Mimic-TEST)。正式稳定版仍为0.14.66；说明见 [Havocker交接](docs/havocker/README.md)。版本更新编译15.514秒、包检查2.066秒，未重复运行同逻辑定向检查。

2026-11-07（基线切换）：按用户决定，把 Harmony 编译基线由已不存在的 `Wrath_Data/Managed/0Harmony.dll` 2.0.4.0（SHA `DE50CD61…`）切到 UMM 0.33.2 自带的 `Wrath_Data/Managed/UnityModManager/0Harmony.dll` 2.3.6.0（SHA `DF043F05…`）。同步更新构建守卫、引用路径、参考指纹、测试断言与依赖说明；历史 .txt 记录原样保留，其中 2.0.4.0 描述均为旧基线。本轮只改代码：未编译、未打包、未启动游戏或 UMM，验证与上传由用户在出包时进行。

2026-11-07（COTW-0473 Spirit-Bound Blade / Menacing）：按用户点名补完第五种附魔。Wrath 原生无 Menacing 武器附魔，Kingmaker 原版改的是 RuleCalculateAttackBonus.FlankingBonus（Wrath 无此字段），因此新建 BlueprintWeaponEnchantment + AddUnitFeatureEquipment + PsychicMenacing（IGlobalRulebookHandler<RuleCalculateAttackBonus>）：持有者相邻目标、目标正被夹击且攻击者是夹击者时，给该盟友 +2 攻击加值。Menacing 追加在变体末尾，既有 8 个变体 GUID 不变。图标 assets/HWMenacing.png 取自 Kingmaker CotW 原素材（沿用 LICENSE-CotW.txt）。enGB/zhCN/QL 描述同步去掉「Menacing is not included」。编译 0 警告 0 错误，产物内新类型/文案/资源已核对；未启动游戏或 UMM、未做玩家验收、未打包、未 commit、未 push。本项在 inventory 中曾被标为排除，按用户本轮点名覆盖。

2026-11-07（COTW-0111 Vindictive Bastard）：按用户点名实现为独立职业（非 Paladin 变体），注册到 BlueprintRegistration/ModuleCatalog。已做：独立 BlueprintCharacterClass + 20 级 Progression + 熟练项；报复账本（盟友或自身被敌人命中后标记攻击者，Vindictive Smite 仅可对该目标使用）、复用原生 Smite Evil buff/resource、每日次数按 4/7/10/13/16/19 级增长；Solo Tactics、Faded Grace 三选一、Spiteful Tenacity(Diehard)、Stalwart、团队专长选择、Gang Up 30 尺光环。**未移植**：4 级法术书、Swift Justice（11）、Aura of Self-Righteousness（17）、Ultimate Vindication（20）及原版报复/来源归属的更细合同，不能写成完整迁移通过。仅更新 Localization-QL；enGB/zhCN 按用户要求未动。未启动游戏/UMM，未做玩家验收。

2026-11-07（编译打包）：按用户要求执行 `build.ps1 -WrathPath ...`。首次失败于 `MediumClass.csproj` 把 UnityModManager 引用钉死在 0.32.4.0（本机 UMM 0.33.2.0），改为按 HintPath 解析（SpecificVersion=false）后重跑成功：编译 29.111 秒、包检查 0.606 秒、总 29.946 秒，0 警告 0 错误。本地包 `D://clew//work//release-staging//0.14.67//8830709fdf41461ca4a0bf01c6122b03//CallOfTheWildWrathPrototype-0.14.67-experimental.zip`，SHA256 `D9BED0EFCAF6A2497325BDABF2D472504EB8D993C8782911E3D06CD4975A2B98`；3 个 DLL 指纹与构建一致，包内主 DLL 含 Vindictive Bastard / Menacing 新类型与文案。验证记录：deliveryReady=false、gameLaunched=false、ummLaunched=false、未运行定向检查。未启动游戏/UMM、未做玩家验收、未上传、未 commit/push；版本号仍为 0.14.67（未升版本）。注意：按用户要求 enGB/zhCN 未改，未启用 QL 的玩家仍会看到 Spirit-Bound Blade 旧描述中的「Menacing is not included」。

2026-11-07（加载失败修复）：用户实测首个包后反馈"新内容和老内容都不生效"。日志证据：`CallOfTheWildWrathPrototype_Log.txt` 的 `Exception occured while applying modification` 与 `Guid should contain 32 digits with 4 dashes`，堆栈指向 WrathPatches 的 `OwlcatModification_LoadAssemblies_Patch` 枚举程序集类型读取自定义属性；TypeId binder 缓存扫描在 `PsychicGreedHands` 后中断，正是本轮新增 `PsychicMenacing` 的位置。根因：`src/PsychicBoundWeapons.cs` 的 `[TypeId]` 误写为 33 位 `dfad8430c0924a0749b3f8cac50afc0bb`，导致整个模组程序集加载失败，所有注册（含历史内容）都不执行。已改为合法 32 位 `dfad8430c0924a079b3f8cac50afc0bb`；全仓 314 个 TypeId 重扫 0 个非法；新 DLL 内旧串 0 处、新串 1 处。重新打包成功：编译 21.985 秒、包检查 0.564 秒、总 22.779 秒，0 警告 0 错误。修复包 `D://clew//work//release-staging//0.14.67//3d5e84cc5cde401d88b51c784650a7aa//CallOfTheWildWrathPrototype-0.14.67-experimental.zip`，SHA256 `8C4E2DF5186C9957AC3AEBD86313A33787EA386E25FA8BBB352467716DAA8FA1`（主 DLL `F68E861D3EA059D574B89478F87DA80F7F4417225901A3D780F6BA8D41E5D90A`）。仍为本地包：未启动游戏/UMM、未做玩家验收、未上传、未 commit/push；需用户替换安装目录后复测，若 Apply 阶段另有异常需再看新日志。

2026-11-08（COTW-0111 规则重写）：按用户实测反馈修正。上一版明确没做：Paladin 施法进度/法术列表、Swift Justice(11)、Aura of Self-Righteousness(17)、Ultimate Vindication(20)；做错或没生效：Solo Tactics（改为直接引用原生 Inquisitor Solo Tactics `5602845c…`）、Vindictive Smite 资源（改为自建按本职业等级 1 次起、4 级起每 3 级 +1 的资源，不再复用 Paladin 资源）、Gang Up（改为 move action、对单个目标把半数 Vindictive Smite 加成分享给盟友、持续 Cha 调整值轮且最低 1 轮；Swift Justice 为该能力的 swift 版本）。新增：从 `bce4989b…` 克隆 Paladin 法术书并把 m_CharacterClass 重绑到新职业（保留 -3 施法者等级修正）、IsDivineCaster=true；17 级 Aura（DR 5、compulsion 免疫、10 尺盟友 +4 士气豁免）；20 级 Ultimate（击晕触发标记 → 下次命中授予可施放的 Disintegrate + 24 小时免疫账本）。**边界**：Ultimate 的 Disintegrate 为手动施放，不是原版「通过武器自动触发」；Aura 的 DR 对齐击穿与 compulsion 描述为近似实现；以上均需游戏内验收。仅更新 Localization-QL，enGB/zhCN 未动。

2026-11-08（按用户新约定改用原生锚点）：用户指出 Wrath 底层与 Kingmaker 变化不大，且原 CotW 的 Gang Up / Aura of Self-Righteousness / Ultimate Vindication 可参考原版圣武士能力。已核实：原 CotW `createAuraOfSelfRighteousness()` 引用的四个 GUID 在 Wrath 就是 `Classes/Paladin/AuraOfRighteousness/*`；`AuraOfJusticeAbility 7a4f0c48…` 的组件正是「把 smite 加成分享给盟友」；`HolyChampion eff3b63f…` 为 20 级 capstone 框架。本轮把 17 级 Aura 改为**直接 clone 原生 AuraOfRighteousnessFeature**（保留原生组件/图标/光环，仅改名、改描述、把 DR 对齐改为 Good|Lawful）；Gang Up 改用原 CotW 的 freebooter bane 图标 `76dabd40…` 并从原生 Aura of Justice 拷贝 AbilitySpawnFx 特效；Ultimate 图标用原生 Disintegrate；所有能力描述统一替换为 Kingmaker CotW `Classes/VindicativeBastard.cs` 的权威英文文本，Spiteful Tenacity / 团队专长也改为带原版文本的包装 feature。QL 同步为同一批文本；enGB/zhCN 仍未动。边界：Gang Up 的规则实现仍为本项目的自定义 action（非直接 clone Aura of Justice 组件），Ultimate 的 Disintegrate 仍为手动施放。

2026-11-08（按原生组件彻底重做两项，未打包）：Gang Up 改为直接 clone 原生 `AuraOfJusticeAbility 7a4f0c48…` + `AuraOfJusticeSmiteEvilBuff ac3c6678…`，保留原生 `ACBonusAgainstTarget/AttackBonusAgainstTarget/DamageBonusAgainstTarget/IgnoreTargetDR`（本来就是 CheckCasterFriend）与 `AbilitySpawnFx/FxOnStart`，仅做原版差异：ActionType=Move、移除 AbilityResourceLogic 与对齐限制、两个 ContextRankConfig 设 Div2+min1、独立 SpeedBonus(Cha,min1) rank 控制持续轮数、apply buff 重绑到克隆 buff、图标用原 CotW `76dabd40…`。Swift Justice 为该 ability 的 swift 克隆。Ultimate Vindication 改为原生 `AddInitiatorAttackWithWeaponTrigger(OnlyHit)` + clone 原生 `Conditional` + `ContextActionCastSpell`(Supernatural Disintegrate，带 `ContextCalculateAbilityParamsBasedOnClass` 按本职业/Cha) + 24 小时冷却 buff；击晕标记账本仍由自定义 `VindictiveUltimate` 承担（CotW 原版用的是自写 `AddIncomingDamageTriggerOnAttacker`，Wrath 无同名原生组件）。删除自定义 `VindictiveGangUpApply`。编译 0 警告 0 错误，TypeId 全仓 0 非法；按用户新约定**本轮不打包、不做包后核对**，等用户明确要求再出包。

2026-11-08（Vindictive Smite 按原 CotW 对齐）：改为 **clone 原生 Smite Evil ability `7bb9eb20…`**，保留 `ContextCalculateSharedValue` + `ContextRankConfig`（之前新建 ability 导致 buff 的 StatBonus/DamageBonus 共享值无人填充、加值算不出来），把 `ContextRankConfig.m_Class` 重绑到本职业；移除 `AbilityCasterAlignment`（LG 施法者限制）并换成「目标必须有 SmiteAllowed 标记、且不能已有 smite buff」的 `VindictiveSmiteRestriction{Allowed,Blocked}`；`AbilityEffectRunAction` 整段替换为永久/不可驱散的 apply buff；`SmiteEvilBuff` 克隆后按原版移除 `IgnoreTargetDR`；标记 buff 改为 `RemoveOnRest|HiddenInUi`、永久施加；报复账本加 100 尺距离限制（对齐原版光环范围）。法术书阵营限制按用户要求**不补**（当前 VB 无 LG 建角/禁施法限制）。编译 0 警告 0 错误。边界：触发仍以 `RuleAttackWithWeapon` 命中近似「造成 HP 伤害」，Wrath 无 CotW 的 `AddIncomingDamageTriggerOnAttacker` 同名组件；需游戏内验收。

2026-11-08（移植 CotW 伤害触发组件，修复实测反馈）：用户实测 Vindictive Smite 次数为 0、敌人杀死两名盟友后既不能 smite 也不触发 Ultimate。排查结论：① 自建资源不必要——原 CotW 直接用原生 `SmiteEvilResource b4274c5b…`（BaseValue=1、不按职业等级）+ `SmiteEvilAdditionalUse 0f5c99ff…`；已改回并在 4/7/10/13/16/19 级恢复额外次数 feature。② `VindictiveUltimate` 只认 `State.IsUnconscious`，盟友**死亡**是 `IsDead` → 被挡掉；`VindictiveRetaliation` 只监听 `RuleAttackWithWeapon` 命中，法术/其它伤害不触发。③ 按用户要求移植 CotW 的 `AddIncomingDamageTriggerOnAttacker`（`NewMechanics/NewMechanics.cs:5731`），实现为 `CotWIncomingDamageTriggerOnAttacker0111 : UnitFactComponentDelegate, ITargetRulebookHandler<RuleDealDamage>`（`RuleDealDamage` 在 `Kingmaker.RuleSystem.Rules.Damage`），按原版用 100 尺 `AddAreaEffect` 光环把 trigger buff 挂到自身及所有盟友，伤害事件的 `Initiator` 即攻击者；`OnlyLethal` 分支同时认 `IsDead|IsUnconscious`，供 20 级 Ultimate 用。删除旧的 `VindictiveRetaliation` / `VindictiveUltimate` 近似组件。Wrath 的 `RuleDealDamage` 无 `Damage` 成员，min_dmg 检查省略（事件本身即伤害结算）。编译 0 警告 0 错误、TypeId 0 非法；QL 补 12 键。**按新约定本轮未打包**，等用户明确要求。未启动游戏/UMM、未 commit/push。

2026-11-08（日志诊断：修复标记失效与 SavingError）：日志显示 `Registered: Vindictive Bastard (COTW-0111)` 成功，但 ① `GameLogFull.txt` 反复出现 `CotWIncomingDamageTriggerOnAttacker0111.OnEventDidTrigger` → `ToyBox.BagOfPatches.Multipliers+BuffCollection_AddBuff2_patch.Prefix` 的 `NullReferenceException`：组件用 `AddBuff(bp, null, null)` 传了空 parentContext，ToyBox 补丁解引用即崩，标记打不上。改为 `e.Reason?.Context ?? Context`（项目 `PsychicAdvancedRuntime01458` 的既有用法）+ 具体时长，并用 `e.Result > 0` 判定伤害。② 保存失败 `Error getting value from ComponentsDictionary on Kingmaker.UnitLogic.Feature` → `Value cannot be null. Parameter name: key`：自建 `AddFacts`/`AddAbilityResources`/`AbilityResourceLogic`/`AddAreaEffect` 等 `BlueprintComponent` 没设 `name`；注册前统一给所有空 name 组件赋唯一名，并给几个 GameAction 补 name。次数为 0 与成长未获得最可能同源于组件 name 缺失（组件索引/存档），需进游戏复测确认。编译 0 警告 0 错误、TypeId 0 非法。**本轮未打包**（用户未要求）；旧存档可能已损坏，建议用新角色/新存档验证。

2026-11-08（Gang Up / Ultimate 共享对象修复）：用户确认存档、次数、Smite 限制均已修好，但 Gang Up 使用后无 buff 无加成、Ultimate 不生效。根因：Gang Up 与 Ultimate 都从原生 `AuraOfJusticeAbility` 的 `AbilityEffectRunAction` 取 `Conditional`，而 `Copy()` 只做一层 MemberwiseClone，`ConditionsChecker` / `IfTrue.Actions` 仍指向原生对象；Ultimate 后写入的 `[hasMark,noCooldown]` 条件与 `[cast,cooldownApply]` 动作把 Gang Up 的"施加 gangBuff"覆盖掉了，同时也污染了原生 Paladin 的 Aura of Justice。修复：Gang Up 改为 **clone 已验证生效的 Vindictive Smite ability**（保留 `ContextCalculateSharedValue`/`ContextRankConfig` 共享值计算），移除资源逻辑与 smite restriction，深拷贝 `ActionList` 后把 apply buff 换成 gangBuff、时长改 Cha 轮、RankConfig 设 Div2+min1；Ultimate 的原生 Conditional 用 `PsychicAssets.CopyData` 深拷贝，两边彻底独立、不再改原生蓝图。说明：Gang Up 的 buff 按原生 `CheckCasterFriend` 设计是加在**被选中的敌人**身上，盟友身上本来不会出现 buff；加成在盟友攻击该敌人时结算。Ultimate 是 **20 级 feature**（`level == 20`），低等级角色测不到。编译 0 警告 0 错误、TypeId 0 非法。**本轮未打包**（用户未要求）。

2026-11-08（Ultimate Vindication 按 CotW `:424-492` 重写并验证正常）：Gang Up 已验证正常；Ultimate 先前仍不生效。对照原版发现 4 处结构偏差并修正：① 原版把 `AddInitiatorAttackWithWeaponTrigger` **直接挂在 feature 上**，移除之前多套的 `AddFacts(ultimateHitBuff)`，feature 直接带 `hitTrigger`。② 原版 `reduce_below0=true` 按“伤害后 HP ≤ 0”判定，`CotWIncomingDamageTriggerOnAttacker0111.OnlyLethal` 改为 `victim.HPLeft <= 0 || IsDead || IsUnconscious`。③ 原版把 24 小时免疫 buff **追加进 Disintegrate 自己的 `AbilityEffectRunAction`**，改为深拷贝其 ActionList 后追加 `cooldownApply`，并给 `ultimateSpell` 加 `ContextCalculateAbilityParamsBasedOnClass(Charisma, VB 职业)`。④ 命中条件改为原版的 `ContextConditionHasBuff(allowed)` + `ContextConditionHasBuffFromCaster(cooldown, Not=true)`，使用全新 `Conditional`，命中时自动执行 `ContextActionCastSpell`。100 尺标记光环保留。编译 0 警告 0 错误、TypeId 0 非法；随后已打入 `0.14.67-experimental` 本地测试包。用户已用 20 级 Vindictive Bastard 实测确认 Ultimate Vindication 正常。至此 Vindictive Bastard 的存档、施法、Smite 次数与目标限制、Gang Up 和 Ultimate Vindication 均已取得玩家确认。

2026-11-08（待做清单与交接更新）：`COTW-0111 Vindictive Bastard` 已实现并获玩家完整确认，从 HOLD 清单移除；`COTW-0473 Spirit-Bound Blade / Menacing` 已实现并获玩家确认；`COTW-0140 Relic Hunter` 按用户决定改为暂且搁置，因为其依赖的 Occultist 基础职业、ImplementEngine 与 implement/focus 相关资源目前同样处于搁置状态。当前可开工 HOLD 清单由十三项缩减为十一项。

## 协作约定（2026-11-07，覆盖旧文档中"出包即上传"的默认）
本仓库只负责写代码；测试包、上传代码和向上游提交合并请求由用户完成。不需要把历史 Release 附件迁到 sxl1216/CotW-Wrath。

1. 只处理本轮明确改动。实际改行为时，选择一次必要定向检查；已有同内容结果不因文档、打包、上传重复执行。
   用户说“出包”默认包含本轮相关源码commit／push、GitHub安装包附件上传和下载核对；仅生成本地包不算完成。测试包保持TEST标记及预发布属性。
2. 默认执行 `./build.ps1 -WrathPath '<本地游戏目录>'`：编译主体和两个内置模块，打包，核对必要文件、三个DLL与资源。默认不运行测试。
3. 从本次staging的 `验证记录.json` 读取实际结果和 `timings`。失败按log修复；未执行检查写未运行，不追补全历史或旧证书。
4. 用户要求发布时，生成当前源码包与简短说明，提交并上传GitHub/原Drive；仅对本次文件下载核对一次。保留旧正式附件，更新HANDOFF，清理本轮无用产物。

`-CheckStartup`、`-CheckRules`、`-Focus amnesiac066` 都是按具体问题显式选择的诊断，不是默认门禁。启动检查需要本地游戏和模块引用；Amnesiac旧诊断另依赖未公开的原工作夹具，独立clone不保证可直接运行，缺失时不恢复整套历史环境来阻塞普通出包。

当前脚本沿用源码上两级的 `work` 布局；原开发目录按 `outputs/CallOfTheWild.Wrath.Prototype` 放置。独立clone先核对实际输出目录和模块本地引用，只补当前需要的依赖，不下载整个旧work。

## 耗时

2026-11-03同机热构建实测：编译11.953秒，打包核对1.611秒，总计13.612秒。本次无功能增量，定向检查未运行；不能写成“零秒通过”。未重复运行已发布Amnesiac专项。

每次记录 `timings.compileSeconds`、`packageChecksSeconds`、`incrementalChecksSeconds`、`checks` 和 `totalSeconds`；没有定向检查时 `incrementalChecksSeconds=null`。发布另记实际上传用时。旧专项未记录进程总时长，不反推或补造旧秒数。

规划估计：普通小修的必要定向检查加出包约1–5分钟；无行为变化时同机编译打包约15–30秒。相较旧全历史流程，纯验证/出包时间可能减少80%–95%；这是估计，不含编码、排错和上传，用后续实测校正。
