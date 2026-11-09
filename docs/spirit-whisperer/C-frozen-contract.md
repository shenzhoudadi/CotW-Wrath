# C 冻结合同：COTW-0117 Spirit Whisperer / S-SPIRIT-0117

- **合同号**：`C-0117-20261108`
- **角色**：C。只冻结，不实现、不改生产、不 push、不启动游戏或 UMM、不跑全量。
- **原作**：`Holic75/KingmakerRebalance@1332fb0db844b7863f484ca978bea2349fe49769`
- **本地只读原作**：`work/port01441/sources/Holic75__KingmakerRebalance/tree/CallOfTheWild/`
- **权威入口**：`Archetypes/SpiritWhisperer.cs` 的 `create()` / `createSpiritSelection()` / `createHexSelection()`，以及各族 `createSpiritWhispererSpirit` 实际调用的 `createSpiritAbility` / `createGreaterSpiritAbility` 或 Lore 的 `createTrueSpiritAbility` / `createManifestation` / `createHexes`。
- **效果正文以这些方法为准。** `A-source-scope.md`、`A-spirit-map.json`、`A-gaps.md` 里的能力名称和数值概述不是合同。它们只证明十族入口、变体骨架和三次巫术选择已被找到。

## 0. A 核对与放行

A 三份产物都在 `C:\Users\xuenc\agent-tools\spirit-whisperer-next\`。十族、L1 移除、L5/10/15/20 的 Wizard bonus feat、仅魔宠、Lore 把 true ability 放进 8 级槽、Waves 调用 `Skip(1)`，都能在原作对上。A 没有自造 GUID，也没有写运行 PASS。

下列 A 概述与原作方法冲突，本文件已经改判，B 不得再按 A 的句子实现：

| A 的说法 | 原作实际授予 |
|---|---|
| Lore 8 级是 Automatic Writing / Divination Insight | `Lore.cs` `createTrueSpiritAbility` 是 **Perfect Knowledge**：四项知识/Lore 技能 +10 competence。Whisperer **不**调用 `createGreaterSpiritAbility`，因此没有 Arcane Enlightenment |
| Lore 20 级是豁免 +4 并自动确认重击 | `createManifestation`：那些知识检定取 20，另加 1 次/日 Wish（固定资源 1，施法参数用 Intelligence） |
| Heavens 1 级是 Starlight 冷冻射线 | `createSpiritAbility` 是 **Stardust**，3+魅力次/日 |
| Heavens 8 级是寒冷抗性、黑暗视觉、盲视 | **Void Adaptation**：30 尺 blindsense、毒素免疫、窒息免疫 |
| Heavens 20 级是寒冷免疫加免费超魔 | 豁免加值等于 **Intelligence** 调整值、恐惧免疫、自动确认自己威胁的重击。没有超魔 |
| Life 20 级是每日自我复活 | `createManifestation` 的免疫与 Diehard。`createTrueSpiritAbility`（Quick Healing）虽被调用，但 **没有**放进 `Spirit` 构造；B 禁止移植它及其 `createSwiftPositiveChannel` |
| Battle 20 级是 DR 5/— 和重击倍率 | Pounce、缺少 Diehard 时补 Diehard、重击无视伤害减免、别人确认打她的重击时 AC +4 insight |
| Bones 20 级是亡灵免疫 | 随意 Animate Dead（不耗材料）加 1 次/日 Power Word Kill，目标当前 HP ≤ 150 |
| Nature 20 级是植物免疫 | 1 次/日把自身类型改为 animal、plant 或 humanoid，且必须与当前类型不同 |
| Waves `Skip(1)` 删掉 Fluid Magic | 数组第 0 项是 Beckoning Chill，最后一项才是空的 Fluid Magic。见第 3 节裁定 |
| 多数 8 级爆发是“只打伤害” | Bones / Flame / Stone / Waves / Wind 的 8 级同时带固定 3 次和 1d4 轮冷却；Stone 的 Body of Earth 还有 DR |

**B 可执行：YES。** 条件：

1. 只做本文件列出的 F-0117 与一份 `SpiritClassBridge0117`。不实现 Spirit Guide，不发 spirit magic spells，不发各族 true spirit（Lore 的 Perfect Knowledge 除外，它占的是 8 级槽）。
2. 从执行当时的最新 GitHub main 做隔离分支。不复制含未提交 Speaker 的整树，不改 `Speaker*.cs`，不把 Speaker 的扣费或清理当基线。
3. 共享写入只限新文件、`BlueprintRegistration.cs`、`ModuleCatalog.cs`，以及把新文件编进工程所必需的项目条目。与其他包串行，不覆盖别人的追加。
4. 缺任一第 9 节必需提供方时整包不发布。不允许先上九个魂域。
5. 定向检查只跑 `C-cases.json` 的代表分支，走真实 `AbilityResourceLogic` / `ActivatableAbilityResourceLogic` / `AbilityCasterHasNoFacts` / `ContextCalculateAbilityParamsBasedOnClass`。组件存在不算行为通过。

## 1. 职业骨架

父职业只使用 Wizard `ba34257984f4c41408ce1dc2004e342e`。法术书保持 Wizard 准备制，施法属性仍是 Intelligence。不替换法术书，不加魂域法术，不改环位。

| 等级 | 移除 | 添加 |
|---:|---|---|
| 1 | Specialist School `5f838049069f1ac4d804ce0862ab5110`；Arcane Bond `03a1781486ba98043afddaabf6b7d8ff` | Spirit Link 选择；仅魔宠的 Arcane Bond |
| 5、10、15 | Wizard Feat `8c3102c2ff3b69444b139a98521a4899` | 同一个 Spirit Hex 选择 |
| 20 | Wizard Feat | 无。20 级 Manifestation 只来自所选魂域 progression |

Spirit Link 是 `Ranks = 1` 的选择，十个 progression 各出现一次。没有第二个魂域，没有每日游荡魂域。

Spirit Hex 的 `AllFeatures` 是十个“该魂域巫术选择”再追加原生 `WizardFeatSelection`。`IgnorePrerequisites = false`。每个魂域巫术选择带 `PrerequisiteFeature(本魂域 progression)`。因此：

- 选了 Battle 的人可以选 Battle 的四个巫术，或退回一次普通 Wizard bonus feat。
- 不能选 Flame 或其他魂域的巫术。
- 三个等级各选一次。选过的巫术用 `PrerequisiteNoFeature` 或选择项自身不可重复，不能同一巫术占两级。
- 20 级不再给第四次巫术，也不把 bonus feat 加回来。

UI 只追加，不重排别人的组：`UIDeterminatorsGroup` 加上 Spirit Link；新建一个 UIGroup，顺序为魔宠、5 级巫术、10 级巫术、15 级巫术。失败回滚时拆掉的是本次追加的引用。

## 2. 等级、智力、魅力：只有这些分支

所有等级、DC、解锁都只读 **Wizard 等级**。`ContextRankConfig.m_Class` 与 `ContextCalculateAbilityParamsBasedOnClass.m_CharacterClass` 只有 Wizard。不写 Shaman，不用角色总等级，不用 `HasFact` 假装萨满等级。Wizard 1 / Fighter 19 按 1 级计算。纯萨满不受这些图影响。

默认 DC 组件是 `ContextCalculateAbilityParamsBasedOnClass`：`10 + Wizard等级/2 + 属性`。

| 分支 | 上限 | 属性 | 代表 |
|---|---|---|---|
| R1 | `BaseValue = 3`，`IncreasedByStat`，`ResourceBonusStat = Charisma`，`m_Min = 0` | 次数看魅力；DC 若存在则看智力 | Battle Spirit 轮数、Enemies' Bane、Touch of the Grave、Touch of Flame、Touch of Acid、Ice Splinter、Shocking Touch、Stardust、Storm Burst、Monstrous Insight |
| R2 | `BaseValue = 1`，同样只加魅力，`m_Min = 0` | 次数看魅力 | Life Channel；Starburn |
| R2-DC | 同上资源 | **DC 看魅力，不看智力** | 只有 Life Channel 的 Will |
| R3 | `BaseValue = 3`，不随属性、不随等级 | DC 看智力；另有 1d4 轮冷却 | Shard Soul、Fiery Soul、Body of Earth 爆发、Frigid Blast、Spark Soul |
| R4 | `BaseValue = 1`，固定 | Wish / Power Word Kill 的施法参数看智力 | Lore Wish、Bones 的 Power Word Kill、Nature 变形 |
| R5 | `IncreasedByLevel`，每 Wizard 等级 1 点，不加魅力 | 不看智力/魅力次数 | Life Sight 轮数；Life Link 同时存在的链接数 |
| 被动 | 无资源 | 智力或魅力只影响标明的那一项 | 见下表 |

数值例（合同期望，不是近似）：

- 魅力 10（+0）时 R1=3，R2=1。魅力 14（+2）时 R1=5，R2=3。魅力 7（−2）时 R1=1，R2=0。魅力再低也不得小于 0，不得把资源花成负数。
- Wizard 8、智力 18（+4）：智力 DC = 18。把智力改到 10 后同一能力 DC = 14。这两次改变魅力，DC 不变。
- Wizard 8、魅力 18：Life Channel DC = 18。改变智力不改变这个 DC，但会改变 Shard Soul 一类的智力 DC。
- Wizard 1 / Fighter 19：上述 DC 用 1，不用 20；8 级能力和 20 级 Manifestation 都不出现。

其他已分清的属性，禁止互相替换：

- Heavens Manifestation 的豁免加值读 **Intelligence**。原作描述句写 Wisdom，组件是 `primary_stat`。英文描述改成 Intelligence，不要留下 Wisdom。
- Benefit of Wisdom 在 `hex_stat == Intelligence` 的分支：Lore (Nature) 与 Lore (Religion) 的基准属性改为 Intelligence。Knowledge (Arcana/World) 保持原本的智力，不要再改一次。不要实现 Wisdom 或 Charisma 那两个分支。
- Confusion Curse 与 Entangling Curse 的持续轮数 = 魅力调整值，最少 1。DC 仍是智力。
- Friend to Animals 给范围内动物的豁免加值 = 魅力调整值，最少 0。
- Mental Acuity：固有智力 +1，之后每 5 个 Wizard 等级再 +1。
- Sparkling Aura 的伤害加值读魅力。
- Life Link 结束时按第 6 节退回 1 点 R5，不按魅力计算。

## 3. 十族保留范围

Progression 固定三级：1 级 spirit ability，8 级 greater（Lore 除外），20 级 manifestation。`progression.Classes = { Wizard }`。8 级 Lore 槽放 Perfect Knowledge，不另造一个 greater，也不再加 true spirit 层。

| 魂域 | 1 级 | 8 级 | 20 级 | 巫术 |
|---|---|---|---|---|
| Battle | Battle Spirit，R1，标准动作开启，之后按轮扣 | Enemies' Bane，R1，迅捷，需持武器，1 分钟 | Pounce；无 Diehard 则给 Diehard；重击无视 DR；确认打她的重击时 AC +4 insight | Battle Master、Battle Ward、Curse of Suffering、Hampering Hex |
| Bones | Touch of the Grave，R1；11 级起武器附加负能量 | Shard Soul，R3+冷却，反射减半，智力 DC | 随意 Animate Dead，不耗材料；Power Word Kill R4，HP 上限 150 | Bone Lock、Bone Ward、Fearful Gaze、Deathly Being |
| Flame | Touch of Flame，R1；11 级火焰武器 | Fiery Soul，R3+冷却，反射减半，智力 DC；另有火焰抗性 10 | 火焰抗性 30；火系免费 Extend/Reach | Fire Nimbus、Flame Curse、Ward of Flames、Cinder Dance |
| Heavens | Stardust，R1 | Void Adaptation，无资源 | 豁免加智力调整值；恐惧免疫；自动确认自己的重击 | Enveloping Void、Lure of Heavens、Heaven's Leap、Starburn |
| Life | Channel Positive Energy，R2，治疗/伤害共用一池，DC 看魅力 | Healer's Touch：Lore (Religion) +3，无资源 | 出血、死亡、负能量、力竭、疲乏、恶心、反胃免疫；属性伤害/吸取不能降到 1 以下；无 Diehard 则给 Diehard | Curse of Suffering、Enhanced Cures、Life Link、Life Sight |
| Lore | Monstrous Insight，R1 | **Perfect Knowledge**，四项知识/Lore +10 competence | 这些检定取 20；Wish R4，参数看智力，不能用来加属性或复制昂贵材料法术 | Benefit of Wisdom、Brain Drain、Confusion Curse、Mental Acuity |
| Nature | Storm Burst，R1 | Spirit of Nature：HP 低于 25% 时快速医疗 1，持续 1d4 轮；Wizard 15 级起为 3 | R4，变为 animal、plant 或 humanoid，且不同于当前类型 | Entangling Curse、Erosion Curse、Friend to Animals、Stormwalker |
| Stone | Touch of Acid，R1；11 级酸蚀武器 | Body of Earth：精金 DR，并从 8 级起每 4 级 +1；爆发 R3+冷却 | 强酸抗性 30；酸系免费 Extend/Reach | Stone Stability、Loadstone、Metal Curse、Ward of Stone |
| Waves | Ice Splinter，R1；11 级霜冻武器 | Frigid Blast，R3+冷却，反射减半，智力 DC | 寒冷抗性 30；寒冷系免费 Extend/Reach | **Beckoning Chill、Crashing Waves、Mist's Shroud** |
| Wind | Shocking Touch，R1；11 级电击武器 | Spark Soul，R3+冷却，反射减半，智力 DC | 电击抗性 30；电击系免费 Extend/Reach | Air Barrier、Sparkling Aura、Vortex Spells、Wind Ward |

伤害骰、范围和豁免类型沿对应 `create*` 方法，不沿 A 的缩写。11 级武器附魔用 `AddFeatureOnClassLevel(..., 11, {Wizard})`。Battle 的士气加值用 Wizard 等级、`OnePlusDivStep`、`stepLevel = 8`：1 级 +1，8 级 +2，16 级 +3。

**Waves 裁定。** `hexes` 的顺序是 Beckoning Chill、Crashing Waves、Mist's Shroud、Fluid Magic。`Skip(1)` 会删掉第一个真实巫术，并留下一个没有组件的 Fluid Magic。该方法注释写明 Fluid Magic 仍是空的，作用是把 spirit magic 放进普通法术位；本变体明确没有 spirit magic。因此巫术池是三个有实际效果的巫术，**不包含 Fluid Magic**。不要按字面 `Skip(1)` 做，也不要按 A 所说“Skip 掉的就是 Fluid Magic”来解释数组。

**Life 的 true spirit 裁定。** 原作方法调用了 `createTrueSpiritAbility`，但构造 progression 时传入的是 `greater_spirit_ability`。B 不调用该方法，不创建 Quick Healing，不调用全局迅捷通道，不改戒指或 Xavorn's Cross。

**Extra Channel。** 克隆原生 Extra Channel `cd9f19775bd9d3343a31a065e93f0c47`（Sunsinger 已用同一 GUID）。把唯一的 `IncreaseResourceAmount` 指到本包 Life 资源，`Value = 2`。先决改成只要求本包的 Channel 特性。追加到已经包含该原生专长的专长池，每次注册只追加一次。不修改原生 Extra Channel 本体，不把加值写进物品附魔。没有这个专长池则整包不发布。

**Lure of Heavens** 要求 Wizard 10 级。**Life Sight** 要求 Wizard 12 级，资源是 R5，自由动作开关，按 `ResourceSpendType.NewRound` 扣轮。

**Heavens 与 Lore** 在 Wrath 萨满里没有现成魂域。它们的图全部私有新建。另外八个魂域即使原生有同名能力，只要职业、属性、资源、先决、Buff 或动作不同，就做有界私有副本。只读的图标、法术描述材料和原生附魔可以引用；禁止 `ReplaceComponent` 写回原生蓝图。

## 4. 图所有权

- 新蓝图 GUID 用 MD5(`CotW.Wrath.SpiritWhisperer0117.v1/` + 稳定英文 token)，与 `PsychicAssets.Id` 同一算法、不同盐。token 不含显示名。资产名前缀 `CotWWrathSpiritWhisperer0117`。
- 自定义 `BlueprintComponent` 各自带未占用的 `[TypeId("0117" + 28 位十六进制)]`。不复用 0046 或 0081 的 TypeId。
- 不新增 Harmony 补丁，不挂钩 `AutoMetamagic.ShouldApplyTo`。
- 魔宠选择克隆 `363cab72f77c47745bf3a8807074d183`，清空选择自身组件以去掉盗贼先决，复制原生特性引用。这与已交付的 `BloodlineMutations01446` 相同，但是另一份选择，名叫 Arcane Bond。图标用 `1194129055923ae46a6b876ff6a30358`；该蓝图缺失时用克隆体自己的图标。禁止物品契约（戒指、项链、武器、魔杖、法杖）。
- 不修改原生魔宠特性、`BloodlineFamiliar01446` 或盗贼选择。若某个魔宠 progression 的等级数组明确写成非 Wizard 职业，该项改为私有副本并把职业改成 Wizard；改不动就整包不发布。禁止让法师魔宠去读盗贼等级。纯盗贼的原选择保持原样。
- 巫术方法里的 `addToAmplifyHex`、`addToSplitHex`、`addToRodOfAbruptHexes`、`addToRodOfInterminableHexes`、`addToHexStrike` 全部不移植。它们会改全局女巫物品。
- 免费超魔只存在于 Flame、Stone、Waves、Wind 的 20 级特性。Heavens 没有。用私有 Buff 上的原生 `Kingmaker.Designers.Mechanics.Facts.AutoMetamagic`：`Metamagic` 为 Extend 或 Reach，`Once = false`，`m_AllowedAbilities = SpellOnly`。同一 Manifestation 的 Extend 与 Reach 互斥；开启一个就关掉另一个。不要占用 Bard、萨满或其他原生 `ActivatableAbilityGroup`，原作那个扩展枚举在 Wrath 不存在。
- Wrath 的 `SpellDescriptor` 已核对，有 Fire、Acid、Cold、Electricity、Sonic，没有 Air、Water、Earth。过滤器只用对应能量：火、酸、冷、电。不要把 Air 收成 Sonic，也不要放到全部法术。只带原作额外描述符、不带能量描述符的法术不吃这个免费超魔，记为 `LIMIT-0117-DESCRIPTOR-ENERGY-ONLY`，不是发布阻断。若现场 `AutoMetamagic` 没有 `Descriptor` 字段，改用挂在该私有 Buff 上的局部组件，只在法术描述符含该能量时加上对应超魔；仍然禁止全局补丁。超魔不提高法术环位、不延长施法时间、不消耗资源。
- 英文界面文字。描述与组件一致，尤其是智力/魅力那几句。

## 5. 注册、缺提供方、图标

先检查第 9 节全部必需 GUID 都能 `cache.Load`。任一缺失：返回 false，不 `Add` 私有蓝图，不改 Wizard 的 archetype、UI 或专长池，日志一行 `reject: 0117 <缺失 GUID 或原因>`。

提供方齐全后才把私有图放入 cache，再追加 Wizard。追加若失败，恢复追加前的数组，并从 cache 去掉本次私有 GUID。第二次调用发现自己的 archetype GUID 已在且定义未变时直接返回，不再追加。

挂到现有 `RegisterAvailableModule` 的 `RegisterAll` 与 `RegisterOptional`。Wizard 或专长池晚到时，下次调用可以一次发布；第一次的拒绝不得留下半套魂域。

图标只取原作方法里已经写出的蓝图 `.Icon`。不新增 png。Starburn 与 Lore Wish 原作用文件精灵：Starburn 改用同一方法里的 faerie fire Buff `cc383a9eaae4d2b45a925d442b367b54` 的图标；Wish 用一个已经能加载的预言/法术图标，并在诊断里写明替代了哪个 GUID。两个都加载不到则整包拒绝。

成功日志一行 `0117 published <archetype guid>`。

## 6. 支付、取消、重复、冷却

每条可花费能力在 `AbilityCastRateUtils.SpendResources` 会走到的那一个叶子上，只有一个 `AbilityResourceLogic`：`Amount = 1`，`m_IsSpendResource = true`。变体菜单父级没有资源组件。Life 的治疗与伤害是两个叶子、同一个资源。触碰投递的子能力不再带第二个 `AbilityResourceLogic`。Monstrous Insight 只在真正交出的那次触摸上扣 1 点。

- 资源为 0：`IsAbilityRestrictionPassed == false`。不调用效果，不挂冷却，资源保持 0。
- 通过限制并执行一次：正好减 1。同一次执行里的伤害加 Buff 仍只扣 1。豁免成功或减半都不退款。
- 施放前取消，或限制未过：扣 0。
- 连续两次合法施放：扣 2。
- R3：资源与冷却同时存在。冷却 Buff 在效果动作里挂给施法者，持续 1d4 轮。`AbilityCasterHasNoFacts` 指向这个私有冷却 Buff。冷却中即使还剩次数也不能施放，且这次尝试不扣次数、不刷新冷却。冷却不是资源，休息时随 Buff 清除。
- Battle Spirit 只有 `ActivatableAbilityResourceLogic`，`ResourceSpendType.NewRound`，没有 `AbilityResourceLogic`。开启命令是标准动作。每个新回合扣 1 轮，包括开启的那个回合若原生 `OnNewRound` 会扣则只扣那 1 点。关闭不退当前轮。战斗结束按 `DeactivateIfCombatEnded` 关闭，不多扣一轮。次数为 0 时不能开启，也不能停在开启状态把资源扣负。
- Life Sight 用同一套按轮扣除，资源是 R5。
- Life Link：建立一条链接扣 1 点 R5；该 Buff 结束或被驱散时用一次恢复把这 1 点还回。驱散动作本身不再扣点。同时存在的链接数不超过 Wizard 等级。结束一条只还 1 点，不能还出超过上限的点数。
- 超魔开关不扣资源。

## 7. 同名来源、升级、休息、重训

- 私有事实的 source 必须是本包蓝图。移除 Spirit Link、巫术或变体时，只移除 source 属于本包 GUID 集合的事实和资源。
- 同显示名的外来 Buff、原生萨满事实、别人的 Channel 资源保留。
- `AddAbilityResources` 使用 `RestoreAmount = true`、`RestoreOnLevelUp = true`。魅力或 Wizard 等级令上限上升时，新增的次数可用；上限下降时当前值夹到新上限，不得超过。
- 休息只依赖原生资源回满。本包不补全局休息补丁，也不回满外来资源。
- 重训或移除特性后，没有特性就不再持有可用次数；能力限制失败。不得留下可以继续花的孤儿池。
- 升级只按 Wizard 等级插入 8 级与 20 级条目。中途换智力或魅力立即改变第 2 节对应分支，不改变另一分支。

## 8. 明确不做

不改 `ShamanClass`、萨满 progression、原生巫术、Speaker、Havocker、Starseeker。不发 Paragon of Battle、元素形态、Phantasmagoric Display、Quick Healing、Arcane Enlightenment、动物伙伴或 Fluid Magic。不把 Wizard 等级写进萨满，也不把萨满等级写进本包。

## 9. 必需提供方

这些 GUID 在发布前必须都能加载，否则整包拒绝：

- Wizard `ba34257984f4c41408ce1dc2004e342e`
- Specialist School `5f838049069f1ac4d804ce0862ab5110`
- Arcane Bond `03a1781486ba98043afddaabf6b7d8ff`
- Wizard Feat `8c3102c2ff3b69444b139a98521a4899`
- 高级魔宠选择 `363cab72f77c47745bf3a8807074d183`
- Extra Channel `cd9f19775bd9d3343a31a065e93f0c47`，以及 Sunsinger 所用、已经包含它的专长池
- 第 3 节各能力在原作方法里 `library.Get` / `CopyAndAdd` 的每一个蓝图

`1194129055923ae46a6b876ff6a30358` 只影响魔宠图标，缺失时用克隆体图标，不单独阻断。

## 10. B 的检查边界

实现后按 `C-cases.json` 做一次最小定向检查。十族的全部原生伤害场景不要求重建。未进游戏、未做完整存档 UI，就写成未测。不要把本项绑进全量或旧证书。
