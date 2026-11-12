# A 原作、提供方与复用核对

本项基线为 main `56ff438` / 0.14.71 TEST。2026-11-11 已检查上游近期提交、7 个开放及关闭 PR 入口、全部公开分支、全部公开 fork 及 sxl1216 的分支与 main 差异。未发现 Psychic Detective 的实现或认领。Speaker #6 继续由原负责人处理，fork 的 Lore Spirit 及既有修复范围继续由其负责人处理。新认领为 #12。

## 固定输入

- 原作 `Holic75/KingmakerRebalance@1332fb0db844b7863f484ca978bea2349fe49769`。核对 `Classes/Investigator.cs`、`Classes/Psychic.cs`、`Classes/PhrenicAmplifications.cs`、`Helpers.cs`、`NewMechanics/SpellbookMechanics.cs` 的五个远端 blob 与本机原件。
- HA 为 `FrozenDervish/Homebrew-Archetypes@211e9b82e252705dfaa7e4aa7507d9819737e232`，MIT。核对七份 Investigator 蓝图与 LICENSE 的八个 blob。公开树未截断，未发现 Psychic Detective 或 Cryptid Scholar。该仓库是数据型提供方，身份必须来自实际加载的蓝图，不能要求不存在的专用程序集。
- Greater Stunning Barrier 的身份另由 `Vek17/TabletopTweaks-Base@221decc02265c780685650ea987bca0a9eeaf49a` 的法术构建器和 GUID 表交叉核对。可选插件版本与开关由真实加载状态决定。
- 本机现有游戏程序集和 Harmony 指纹见 `A-evidence.json`。本轮只做定向元数据和反编译阅读。

## HA 的实际接口

`InvestigatorClass` 为 `adb9e138bee9ecc4db246b64d563f900`，进展 `5eaea532547c7ce40a84563e15fd3440`，炼金术书 `961e6b4edd3c80e4a9fe4435deff6fe7`，天赋选择 `da89239cc5859a1478007cd5e6d44f67`。前三者中的职业、进展和天赋身份已由现有 `ExtraInvestigatorTalent` 采用，沿用其真实注册检查。

HA 毒抗 `152ea6d8f279a73489c9dc0815e9e9b1` 在 2、5、8 级分别授予，11 级毒免 `03d1877a5ffcbd34d9501b005434cb4f`。原作变体只写移除 2 级毒抗，是因为原作使用单一成长特性。Wrath 适配必须移除 HA 的全部三次毒抗授予。天赋在 3、5、7……19 级，侦探只替换 3 级那一次。

HA 的嵌套 `AlchemistDiscovery` 选择 `f68b959943348034d8469c165442e019` 含四项原生选项，恰好是原作禁用的 Mutagen、Enhance Potion、Extend Potion、Infusion。其 GUID 已在游戏归档逐一核实。原作通过修改这些共享特性禁用，本合同把限制收窄到 Investigator 天赋选择路径。

HA 的原始 spellbook 数据中 `m_CharacterClass` 指向原生 Alchemist `0937bec61c0dabc468428f496580c721`。这是读到的提供方数据，不作为新书的父职业。新书从 Bard 的表构建，显式绑定真实 HA Investigator；原提供方资产保持来源所有权。是否安装了可实际授予职业的 HA，仍须 B 在宿主或真实环境核对。

## 直接复用的已有实现

- **180 行原作法表已经存在**。`src/MindbladeSpellAnchors01454.cs` 的等级、原作引用和顺序与原作侦探法表逐行一致。各环为 5 / 26 / 36 / 29 / 31 / 24 / 29 行。本轮已核对全部 109 个直接原生法术 GUID，均在当前归档存在。保留一份目录，B 复用该表与 `Mindblade01454.AccessList` 的有界候选解析，保持旧调用方的过滤语义。
- 旧表九行候选为空。其中八行已有 `PsychicSharedSpellCatalog` 链接，可直接调用 `PsychicSpellIntegration.Available` 核验。另一行 Greater Stunning Barrier 的真实 TTT GUID 为 `9e33ed477ddd4fad9d90fb64b1a6f732`。九项是提供方映射补充，不重新制作法术。当前可用集合与原作 180 行总目录分别记录。
- 原作 Find Traps 为 1 环，Banishment 为 6 环。现有 `Mindblade01454.AccessList` 刻意排除这两项并把 Find Traps 放回 2 环，因而不能直接把该函数输出作为侦探书。抽取共同的单行解析即可，保留 Mindblade 的原有调用结果。
- `PsychicSpellbookMarker`、`PsychicCasting`、`PsychicUndercasting`、已有 Center Self、Logical Spell 与增幅效果均已存在。新书 marker 上限为 6。
- 11 种原作基础增幅均有现有实现。九种在 `PsychicAmplifications`，Will of the Dead 和 Undercast Surge 在 `PsychicCompletionAmplifications01458`。当前 `PsychicAmplificationComponent.Eligible`、`PsychicAdvancedRuntime01458.Real/Plan`、资源挑选和菜单写死原 Psychic 身份，克隆蓝图并不能接通运行时。由 S-PSYCHIC-0068 扩展同一份机制。
- 现有 `Psychofeedback` 已使用原生 `SpontaneousSpellConversion` 和资源支付，新分支只需绑定侦探职业、侦探池和 2–6 环。保留现有效果和失败处理。
- `ExtraInvestigatorTalent.Register` 会从提供方天赋选择同步自己的选项。新增两个侦探天赋后复用它刷新，检查实际普通专长入口。原子发布复用 `PsychicAssets` 与 `MagusBatch01454` 等现有工具。

## 已读原生路径

本机反编译确认 `ClassData.AddArchetype` 通过 `ReplaceSpellbook` 修改实际 `ClassData.Spellbook`。`SpontaneousSpellConversion.OnTurnOn/OnTurnOff` 通过该职业的 `DemandSpellbook` 挂接和清退转换。`BlueprintFeatureSelection.CanSelect` 接收 `FeatureSelectionState`，其中 `Parent`、`Source` 和 `Selection` 可界定真实天赋来源；`SelectFeature.Check` 调用同一入口。`SavingThrowBonusAgainstDescriptor` 从实际 fact 上下文计算加值并给触发事件加临时修正。

这些证据支持合同和 B 的接线设计。提供方初始化、单位实付、升级界面、存档与玩家游戏均未运行。原生反编译文件和提供方原件保留在本机 `C:\Users\xuenc\agent-tools\psychic-detective-next\`，公开仓库仅保存必要索引、身份和指纹。
