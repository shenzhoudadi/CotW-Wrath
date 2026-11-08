> 交付勘误：实际十星座名称与接口以本目录README及已复查生产目录为准；文中旧名称／接口建议保留为历史合同，不凭错误标注重写实现。原文固定来源为Holic75/KingmakerRebalance对应commit的Classes/Psychic.cs。

# C-Frozen-Contract: Task 0081 Psychic `Esoteric Starseeker` (`esoteric_starseeker`)

- **Contract ID**: `C01-STARSEEKER-20261107-r1`
- **Task**: `0081` — Psychic Archetype `Esoteric Starseeker` (`esoteric_starseeker`)
- **Role**: C (攻坚档：核实关键证据、裁定机制冲突并冻结实施合同)
- **Target Module**: `EsotericStarseeker0081` (`<CotW workspace>\outputs\CallOfTheWild.Wrath.Prototype\src\EsotericStarseeker0081.cs`)
- **Source Reference**: `<CotW workspace>\work\reference\kingmaker-cotw\CallOfTheWild\Archetypes\Psychic.cs` (`createEsotericStarseeker()`, lines `408–567`)

---

## 0. Executive Gate Ruling (`B可执行: YES/NO 及条件`)

- **Ruling**: **`B可执行: YES`**（在满足下列原子目录门禁与零妥协约束的前提下，允许启动 Role B 正式实现与定向测试）。
- **Critical Correction to Role A Catalog Audit (`hypnotic_pattern`)**:
  - Role A 将 `Daughter L2` 的 `NewSpells.hypnotic_pattern` 标为 `unresolved_missing`，原因是 `MindbladeSpellAnchors01454.cs:54` 留空且 `PsychicSharedSpellCatalog.cs` 尚未收录其桥接项。
  - Role C 复核本地去重工作区 `<CotW workspace>\work\dedup\sources\ka-dyn__ExpandedContent` 证实：**`ExpandedContent` 已完整实现 `HypnoticPatternAbility`**：
    - 源码：`ExpandedContent\Tweaks\Spells\HypnoticPattern.cs:81` & `ExpandedContent\Config\Blueprints.json:1225`
    - 固定 GUID：`56b8f0304a704a67b3c35cbe8c774854` (`56b8f030-4a70-4a67-b3c3-5cbe8c774854`)
    - 蓝图名：`"HypnoticPatternAbility"`
    - 契约属性：`AbilityType.Spell`、`SpellSchool.Illusion`、`Parent == null`、含 `AbilityEffectRunAction`，且已发布至 Bard L2 与 Wizard L2 法术表（`PublishedListRequired = true`）。
- **Full 90-Entry Catalog Closure Summary**:
  - **10 个星座 × 9 环 = 90 个法术槽位已 100% 闭合静态身份**，无任何 `unresolved_missing`：
    1. **73 项 Wrath 原生法术 (`Wrath`)**：69 个原始 GUID 直连 + 4 个符号映射到原生 Wrath GUID（`command` -> `feb70aab86cc17f4bb64432c83737ac2`，`ray_of_exhaustion` -> `8eead52509987034ea9025d60cc05985`，`shapechange` -> `22b9044aa229815429d57d0a30e4b739`，`meteor_swarm` -> `5e36df08c71748f7936bce310181fb71`）。
    2. **12 项 CotW-Wrath 原型库自建法术 (`CotW-Prototype`)**：全部已在 `BlueprintRegistration.RegisterAll` 中先于本变体注册（含 `TimeStop01438` `585f7776285d17b60b9e8e2fe42a69be`）。
    3. **5 项外部 Provider Mod 真实法术 (`External-Provider`)**：
       - `Archer L6` `fly_mass` -> `TomeOfTheFirebird` `MassFlySpell` (`2ddc377f87b94aba9e8bab6c03f477e8`, `SpellSchool.Transmutation`, `PublishedListRequired = true`)
       - `Charioteer L7` `particulate_form` -> `ExpandedContent` `ParticulateFormAbility` (`99e39437184a46b994da740ce399f591`, `SpellSchool.Transmutation`, `PublishedListRequired = true`)
       - `Daughter L2` `hypnotic_pattern` -> `ExpandedContent` `HypnoticPatternAbility` (`56b8f0304a704a67b3c35cbe8c774854`, `SpellSchool.Illusion`, `PublishedListRequired = true`)
       - `Mountain L3` `invisibility_purge` -> `CharacterOptionsPlus` `InvisibilityPurge` (`c3647ce7d6da49faa6e86830841e56e1`, `SpellSchool.Evocation`, `PublishedListRequired = true`)
       - `Tree L9` `akashic_form` -> `PrestigePlus` `NewSpell.UseAkashicForm` (`58bcb999b3ad46c289dc68cba9c50bd0`, `SpellSchool.Necromancy`, `PublishedListRequired = false`)
- **Mandatory Conditions for Role B Execution**:
  1. **补齐共享目录链接**：在 `PsychicSharedSpellCatalog.cs` 中补入 `HypnoticPatternAbility` (`56b8f0304a704a67b3c35cbe8c774854`, `"HypnoticPatternAbility"`, `"ExpandedContent"`, `SpellSchool.Illusion`, `published: true`, `psychic: 2, occultist: -1, spiritualist: -1`)，使 `PsychicSpellIntegration` 在 `ExpandedContent` 存在时同样可将其接入 Psychic 2 环并执行统一契约校验。
  2. **严禁残缺星座或替代法术凑数**：`EsotericStarseeker0081.Register(BlueprintsCache cache)` 必须在建图前校验全部 90 个星座法术（73 原生 + 12 自建 + 5 外部 Provider）。若任一法术在当前 `BlueprintsCache` 中缺失或未通过契约校验，**必须返回 `false` 并保持零发布（不写入任何私有蓝图、不修改 `PsychicClass.m_Archetypes` 或 `PsychicProgression.UIGroups`）**。
  3. **支持迟加载完成原子发布**：在 `BlueprintRegistration.RegisterAll` 与 `BlueprintRegistration.RegisterOptional` 中均通过 `RegisterAvailableModule(cache, "0081 Esoteric Starseeker", EsotericStarseeker0081.Register)` 挂载；当外部 Provider 迟加载就绪后，重试时一次性完成全部 10 星座 × 9 环与变体本体的原子发布。

---

## 1. Level 1 / 11 / 15 替换关系与局部资格改动

### 1.1 Archetype Feature Replacement Table (`AddFeatures` / `RemoveFeatures`)

| Psychic Level | `RemoveFeatures` | `AddFeatures` | 说明 |
|---:|---|---|---|
| **Level 1** | `DisciplineSelection` (`PsychicAssets.Id("DisciplineSelection")`)<br>`PhrenicAmplificationSelection` (`PsychicAssets.Id("PhrenicAmplificationSelection")`) | `EsotericStarseekerDisciplineSelection0081`<br>`WrittenInTheStarsFeature0081` | 替换 1 级带奖励法术的学科选择与 1 级增幅选择 |
| **Level 11** | `PhrenicAmplificationSelection` (`PsychicAssets.Id("PhrenicAmplificationSelection")`)<br>`MajorAmplifications` (`PsychicClass.MajorId = "fdd7cee80b9e81640bc490e3eafb2742"`) | *(无新增特征；11 级雙星座能力与第 2 次每日次数由 `WrittenInTheStarsFeature0081` / `WrittenInTheStarsResource0081` 自动随职业等级解锁)* | 移除 11 级增幅选择，并将 `MajorAmplifications` 推迟至 15 级 |
| **Level 15** | *(无)* | `MajorAmplifications` (`PsychicClass.MajorId = "fdd7cee80b9e81640bc490e3eafb2742"`) | 在保留原 15 级 `PhrenicAmplificationSelection` 的基础上，补发 `MajorAmplifications` 资格特性 |

### 1.2 无奖励法术学科选择 (`EsotericStarseekerDisciplineSelection0081`)
- **冲突点**：`MagaambyanTelepath01447` 已创建了 `MagaambyanDisciplineSelection01447` 及其 6 个剥离了 `AddKnownSpell` 的学科进阶（`Ferocity`, `Faith`, `Pageantry`, `Pain`, `Psychedelia`, `Abomination`，后三者由 `PsychicDisciplineBridge01458` 桥接），但 `MagaambyanDisciplineSelection01447` 的描述文本写有 `"A Magaambyan telepath..."`，且属于 `MagaambyanTelepath01447` 的私有发布回执（`ArchetypeBatch01445.Receipt`）。
- **裁定**：
  - `EsotericStarseeker0081` **严禁修改 `MagaambyanDisciplineSelection01447` 本体**（避免破坏 01447 的 `Definition` 幂等指纹）。
  - `EsotericStarseeker0081` 创建独立的 `BlueprintFeatureSelection`（Token：`"EsotericStarseekerDisciplineSelection0081"`，名称：`"Psychic Discipline"`），其 `m_Features` 与 `m_AllFeatures` 直接引用 `MagaambyanDisciplineSelection01447` 下的全部 6 个无奖励法术学科 `BlueprintProgression`（注册前必须校验 `magaambyanDisciplines.AllFeatures.Length == disciplines.AllFeatures.Length && disciplines.AllFeatures.Length == 6`）。
  - 验证效果：玩家在 1 级任选 6 个学科之一，保留该学科的 Phrenic Pool 属性路由（Wisdom / Charisma）与 1/5/13 级学科能力，但**不获得任何原学科的 1–9 环固定奖励法术**。

### 1.3 Major Amplifications 与 Mimic Metamagic 局部资格门禁 (`PsychicMajor01460`)
- **关键架构证据**：
  1. 在 `PsychicClass.cs:162-165` 中，`PsychicSpellcasting` (`CastingId = "4896622b699957208e128c3a5361609c"`) 挂载了组件 `MajorGrant01460 : AddFeatureOnClassLevel`（`Level = 11`），用于在角色达到 Psychic 11 级或读档时自动授予 `MajorAmplifications` (`MajorId = "fdd7cee80b9e81640bc490e3eafb2742"`)。
  2. `PsychicCompletionAmplifications01458.Major(cls)` 与 `MimicCatalogue01461` (`MimicMetamagicI/II/III`) 均使用 `PrerequisiteFeature(MajorId)`，且运行时 `PsychicMajor01460.Allows` 检查 `PsychicMajor01460.Has(owner)`（即是否持有 `MajorId` Fact）。
  3. 同时，`PsychicClass.Register` 具有严格的蓝图 `Fingerprint` 幂等检查（`PsychicClass.cs:182`），**严禁在外部修改 `PsychicSpellcasting` 蓝图的序列化组件字段**。
- **裁定**：
  - **不触碰 `PsychicSpellcasting` 蓝图字段**（保持 `PsychicClass` 蓝图指纹 100% 不变），仅在 `PsychicMajor01460.cs` 的 `MajorGrant01460` / Harmony 局部逻辑中感知 `EsotericStarseekerArchetype0081`：
    - 定义辅助判断：`PsychicMajor01460.RequiredLevel(UnitEntityData owner)` —— 若 `owner` 在 `PsychicClass` 上拥有 `EsotericStarseekerArchetype0081`，返回 `15`；否则返回 `11`。
    - 在 `MajorGrant01460`（可通过覆写 `OnActivate` / `OnPostLoad` / `HandleUnitGainLevel` 或对 `AddFeatureOnClassLevel` 针对 `MajorGrant01460` 实例的后置对账）中：当 `owner` 具有 `EsotericStarseekerArchetype0081` 且 Psychic 职业等级 `< 15` 时，若 `Data.AppliedFact != null` 或 `owner` 持有 `MajorId`，立即移除 `Data.AppliedFact` / `MajorId` Fact；当 Psychic 等级 `>= 15` 时正常授予并保留 `MajorId`。
    - 在 `PsychicMajor01460.Has(UnitEntityData owner)` 中：除检查 `owner` 持有 `MajorId` 外，同时校验 `owner.Progression.GetClassLevel(psychicClass) >= RequiredLevel(owner)`。
  - **效果闭环**：
    - 非 Starseeker 的普通 Psychic 与其它变体：11 级照常获得 `MajorId`，100% 不受影响。
    - `Esoteric Starseeker`：1–10 级与 **11–14 级**均不持有 `MajorId`（14 级时的增幅槽只能选普通 Minor Amplifications，不能选 Major Amplifications，也不能选 `Mimic Metamagic`）；到达 **15 级**时，同时通过 Progression L15 与 `MajorGrant01460` 获得 `MajorId`，解锁全部 Major Amplifications 与 `Mimic Metamagic`（15 级、17 级、19 级增幅槽可选）。
    - **严禁使用全局 `HasFact` 欺骗或职业等级伪装**。

---

## 2. 每个法术环位奖励法术数量与 11 级双星座语义

### 2.1 桌面规则（AoN 原文）与原版 CotW 冲突裁定
- **桌面规则原文**：
  - 1–10 级：每日可调谐 1 个星座（Full-round action），将该星座的奖励法术加入已知法术列表（每个她能施放的法术环位获得 1 个来自该星座的奖励法术），持续 24 小时或直至下次准备法术/休息。
  - 11 级起：*"At 11th level, she can attune herself to two constellations at once, gaining two bonus spells per spell level, and she can adjust her constellations twice per day."*
- **裁定**：
  1. **单星座奖励法术数**：每个处于激活状态的星座 Buff，在角色**当前可施放的每个法术环位**（`1 <= circle <= book.MaxSpellLevel`）提供 **1 个**对应的星座法术作为 Psychic 自发施法书的临时已知法术。
  2. **1–10 级单星座上限**：同一时刻最多只能有 **1 个**星座 Buff 处于激活状态（每个可施放环位共 1 个星座奖励法术）。若因任何原因在已有 1 个激活星座时激活新星座，先移除旧星座 Buff，再挂载新星座 Buff。
  3. **11–20 级双星座同时维持 (`Two Constellations At Once`)**：
     - 从 Psychic 11 级起，角色同一时刻最多可同时拥有 **2 个不同**的激活星座 Buff。
     - 当 2 个星座同时激活时，两个星座各自提供其 `1 .. book.MaxSpellLevel` 环位的星座法术，因此角色在每个可施放环位同时拥有 **2 个**星座奖励法术（分别来自星座 A 与星座 B）。
     - 若角色已同时激活 2 个星座（按获得先后顺序记为 $C_{\text{old}}, C_{\text{new}}$），且再次施放技能调谐第 3 个星座 $C_3$（例如通过外部恢复每日次数），则遵循 **FIFO 替换最旧星座**：仅移除最早激活的 $C_{\text{old}}$，保留 $C_{\text{new}}$ 并加入 $C_3$。

---

## 3. 未达环位的高环法术是否允许在调谐时预先挂入

### 3.1 冲突分析
- 原版 Kingmaker CotW 在 `WrittenInTheStars{Constellation}Buff` 上直接静态堆叠了 1–9 环共 9 个 `AddKnownSpell`，导致 1 级 Psychic 一旦开启星座，法术书的 2–9 环列表里就全部塞满了未解锁环位的高环法术。
- 这不仅违反 AoN 规则（*"Instead of gaining a bonus spell at each spell level she can cast..."*），还会污染升级预览（`CopySpellbook`）和 UI 法术书页签。

### 3.2 裁定（严禁预先挂入未解锁高环，升级时动态补发）
1. **调谐时仅授予当前可施放环位 (`1 .. book.MaxSpellLevel`)**：
   - 当星座 Buff 激活时，读取角色 Psychic `Spellbook` 的 `MaxSpellLevel`（若 `book == null || book.MaxSpellLevel < 1` 则不授予任何法术）。
   - **仅将环位 `circle <= book.MaxSpellLevel` 的星座法术**注册进原生 `UnitPartTemporarySpellsKnown`（即高环 `circle > book.MaxSpellLevel` 的法术**绝不预先挂入**法术书或 `UnitPartTemporarySpellsKnown`）。
2. **激活期间升级解锁新环位 (`HandleUnitGainLevel` / `OnPostLoad`)**：
   - 星座 Buff 的运行时组件必须实现 `IUnitGainLevelHandler`（并在 `OnPostLoad` 时对账）。
   - 当角色在星座 Buff 持续期间提升 Psychic 等级并解锁新的 `MaxSpellLevel`（例如从 3 级升到 4 级，`MaxSpellLevel` 从 1 变为 2）时，组件自动将该星座新解锁环位（如 2 环）的星座法术增量注册进 `UnitPartTemporarySpellsKnown`，并触发 `PsychicUndercasting.Reconcile(book)` 与 `Owner.UISettings?.SetDirty()`。

---

## 4. 每日调整窗口、次数及取消／重复／失败／移除 Buff／休息时的资源语义

### 4.1 技能形态与动作消耗
- **母技能**：`WrittenInTheStarsAbility0081` (`BlueprintAbility`)
  - `Type = AbilityType.Supernatural`
  - `ActionType = UnitCommand.CommandType.Standard`，`m_IsFullRoundAction = true`（**整轮动作 Full-Round Action**）
  - `Range = AbilityRange.Personal`，`Set(ability, "CanTargetSelf", true)`
  - 通过 `AbilityVariants` 挂载 10 个子变体技能：`WrittenInTheStars{Constellation}Ability0081`。
- **子变体技能**（10 个）：
  - 每个子技能同样为 `AbilityType.Supernatural`、整轮动作、`AbilityRange.Personal`，挂载：
    1. `AbilityResourceLogic` (`m_RequiredResource = WrittenInTheStarsResource0081`, `Amount = 1`, `m_IsSpendResource = true`)
    2. 自定义施法前置校验组件 `WrittenInTheStarsRestriction0081 : BlueprintComponent, IAbilityCasterRestriction`
    3. `AbilityEffectRunAction`（执行自定义 `WrittenInTheStarsApplyAction0081 : ContextAction`）

### 4.2 每日资源池 (`WrittenInTheStarsResource0081`)
- **次数公式**：
  - Psychic 1–10 级：**1 次/天**
  - Psychic 11–20 级：**2 次/天**
- **蓝图配置**：
  - 使用 `MaxAmount`：
    - `BaseValue = 1`
    - `IncreasedByLevelStartPlusDivStep = true`
    - `StartingLevel = 11`
    - `StartingIncrease = 1`
    - `LevelStep = 10`
    - `PerStepIncrease = 0`（或使用 `ArchetypeBatch01445.Resource(f, "WrittenInTheStarsResource0081", cls, 1, 11)` 并将 `PerStepIncrease` 设为 `0`，确保 1–10 级恒为 1，11–20 级恒为 2，20 级不会变成 3！**注意**：`ArchetypeBatch01445.Resource(f, token, cls, 1, 11)` 默认 `PerStepIncrease = 1`，在 21 级才 +1，在 1–10 级为 `1`，在 11–20 级为 `1 + 1 + (level - 11)/11 * 1 = 2`，但显式设定 `StartingLevel = 11, StartingIncrease = 1, LevelStep = 100, PerStepIncrease = 0` 最安全直观）。
  - 由 `WrittenInTheStarsFeature0081` 通过 `AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = false }` 授予。

### 4.3 取消／重复／失败／移除 Buff／休息时的资源状态机（严禁退费漏洞）
- **历史 Bug 裁决**：原版 Kingmaker CotW 在 Buff 的 `Deactivated` 动作中写入了 `ContextRestoreResource`，导致只要星座 Buff 被移除或替换就退还每日次数，使 1/day 限制完全失效（无限免费切换星座）。**本合同严厉禁止在 Buff `OnDeactivate` / `Deactivated` 中退还 `WrittenInTheStarsResource0081`！**
- **精确状态机规则**：
  1. **施法前拦截 (`WrittenInTheStarsRestriction0081.IsCasterRestrictionPassed`)**：
     - 校验 A：施法者拥有 Psychic `Spellbook` 且 `MaxSpellLevel >= 1`。
     - 校验 B：**防重复同星座浪费**——施法者当前**未持有**该子技能对应的同一个星座 Buff (`!caster.HasFact(ConstellationBuff)`)。若已持有同一星座 Buff，UI 置灰并提示原因，不允许施放，**消耗 0 次数**。
  2. **前摇取消 / 施法中断**：
     - 整轮动作完成前取消命令或被打断，`AbilityEffectRunAction` 未执行，**消耗 0 次数**。
  3. **成功施放 (`WrittenInTheStarsApplyAction0081.RunAction`)**：
     - `AbilityResourceLogic` 正常扣除 **1 点** `WrittenInTheStarsResource0081`。
     - 检查施法者当前已有的 `WrittenInTheStars*Buff0081` 列表（按激活时间排序）：
       - 设上限 `cap = caster.Progression.GetClassLevel(psychicClass) >= 11 ? 2 : 1`。
       - 若当前激活星座数 `>= cap`，移除最早激活的星座 Buff，直到激活数 `< cap`。
     - 对施法者施加目标星座的 `WrittenInTheStars{Constellation}Buff0081`（永久持续 `AddBuffPermanent` / `IsNotDispelable = true`）。
  4. **Buff 属性与移除/驱散语义**：
     - 星座 Buff 设置 `m_Flags = BuffFlags.RemoveOnRest | BuffFlags.StayOnDeath`（即整数位 `(int)24` 或通过枚举设置 `RemoveOnRest` 与 `StayOnDeath`），且设置 `IsNotDispelable = true`。
     - **任何原因导致的星座 Buff 移除（包括被新星座顶替、死亡后强制清理、洗点移除或脚本移除）均仅移除该星座授予的临时法术，绝不退还 `WrittenInTheStarsResource0081` 次数！**
  5. **队伍休息 (`Rest`)**：
     - 休息时，Wrath 原生机制自动移除带有 `RemoveOnRest` 标志的星座 Buff（清理其临时已知法术），同时自动将 `WrittenInTheStarsResource0081` 恢复至当日上限（1–10 级为 1，11–20 级为 2）。
     - 这完美还原桌面规则：星座调谐持续至下次休息刷新每日次数，角色在休息后需花费整轮动作重新调谐当日星座。

---

## 5. 永久已知法术、多来源同法术与法术菜单／快捷栏刷新

### 5.1 必须复用 `TemporaryKnowledge01464` 与原生 `UnitPartTemporarySpellsKnown`
- **裁定**：严禁自造第二套脱离 `UnitPartTemporarySpellsKnown` 的临时法术账本或直接调用裸 `book.AddKnown(level, spell)`（裸 `AddKnown` 会创建 `IsTemporary = false` 的永久法术并在移除时误删玩家永久法术）。
- **实现机制**：
  - 在每个星座 Buff `WrittenInTheStars{Constellation}Buff0081` 上挂载自定义运行时组件 `ConstellationSpellSource0081 : UnitFactComponentDelegate, IUnitGainLevelHandler`（拥有固定唯一 `[TypeId]`，存放该星座 1–9 环的 `BlueprintAbilityReference[] Spells`）。
  - `ConstellationSpellSource0081` 在 `OnActivate()` / `OnPostLoad()` / `HandleUnitGainLevel()` 时调用统一的 `Sync()`：
    - 获取 `Owner` 的 Psychic `Spellbook`（`cls.Spellbook`）。
    - 计算当前可施放环位上限 `maxLevel = Math.Min(9, book.MaxSpellLevel)`。
    - 使用组件自身的 `Fact`（或每个环位独立的合成 `EntityFactComponent` / `UnitPartTemporarySpellsKnown.AddEntry`）管理 `1 .. maxLevel` 的临时法术：
      - **注意 `UnitPartTemporarySpellsKnown.RemoveEntry(EntityFactComponent source)` 的工作方式**：在 Wrath 原生代码中，`UnitPartTemporarySpellsKnown.AddEntry(spell, level, sourceComponent)` 以 `EntityFactComponent`（即 `this.Runtime`）为 `m_Source` 键；一次 `AddEntry` 会在同一个 `SpellEntry`（对应特定 `sourceComponent`）下记录单个或多个法术，或者若在 `BlueprintBuff` 上为 1–9 环挂载 9 个受等级门控的组件实例（例如每个环位一个 `ConstellationCircleSpell0081 : UnitFactComponentDelegate, IUnitGainLevelHandler`，含 `int Circle` 与 `BlueprintAbilityReference Spell`），则每个环位天然拥有独立的 `EntityFactComponent Runtime`！
      - **强烈推荐每个环位一个 `ConstellationCircleSpell0081` 组件实例（共 9 个挂在 `WrittenInTheStars{Constellation}Buff0081` 上）**：
        - 当 `book != null && book.MaxSpellLevel >= Circle` 时，调用 `Owner.Ensure<UnitPartTemporarySpellsKnown>().AddEntry(Spell.Get(), Circle, Runtime)`（幂等：若 `Runtime` 已在 `m_Entries` 中注册则不重复添加）。
        - 当 `OnDeactivate()` 时，调用 `Owner.Get<UnitPartTemporarySpellsKnown>()?.RemoveEntry(Runtime)`。
        - 当 `HandleUnitGainLevel()` 或 `OnPostLoad()` 触发且 `book.MaxSpellLevel >= Circle` 时，若尚未注册则调用 `AddEntry(Spell.Get(), Circle, Runtime)`！
        - 这种设计 100% 契合原生 `UnitPartTemporarySpellsKnown`（每个 `EntityFactComponent` 对应 1 个法术条目）及 `TemporaryKnowledge01464.cs` 的全部 Harmony 补丁（`AddKnownTemporaryPatch`、`RemoveKnownTemporaryPatch`、`PruneSourcesPatch`、`PostLoadPatch`、`GainLevelPatch`、`RestoreCopy`）！

### 5.2 多来源隔离与永久已知法术保护
- **场景 A（星座法术 vs 玩家已永久学会的同名法术）**：
  - 若玩家在调谐星座前已永久学会法术 $S$（`book.GetKnownSpells(circle)` 中已有 `IsTemporary == false` 的 $S$）：
    - 当 `ConstellationCircleSpell0081` 激活时：若 `book.GetKnownSpells(circle).Any(a => a.Blueprint == spell && !a.IsTemporary)` 为真，则无需向法术书写入重复临时项（或由 `UnitPartTemporarySpellsKnown` 记录但 `PruneSourcesPatch` 保护非临时项）；当星座 Buff 移除时，**玩家永久学会的 $S$（`!IsTemporary`）必须完整保留，绝不丢失**。
  - 若玩家在星座 Buff 激活期间（此時 $S$ 为 `IsTemporary == true`）通过升级法术选择永久学会了 $S$：
    - `TemporaryKnowledge01464.RestoreCopy` 在 `CopySpellbook` 预览副本中将星座授予的 $S$ 还原为 `IsTemporary = true`，因此升级界面允许玩家再次选择 $S$ 作为永久已知法术；
    - 升级提交后，`S` 在真实法术书中拥有永久条目（`!IsTemporary`），后续星座 Buff 移除时，`TemporaryKnowledge01464.PruneSourcesPatch` 保证只清理临时来源，**绝不误删由于值相等匹配的永久 `AbilityData`**。
- **场景 B（双星座或星座 + 其它临时来源如 Amnesiac 同名法术）**：
  - 由于每个 `ConstellationCircleSpell0081` 使用各自 Buff 实例下的 `Runtime` (`EntityFactComponent`) 作为 `UnitPartTemporarySpellsKnown` 的 source key，移除来源 A 时，`UnitPartTemporarySpellsKnown.RemoveEntry(runtimeA)` 发现来源 B 仍在提供该法术，因此法术仍保留在 `book` 中；直到最后一个临时来源移除（且无永久已知项）时才从 `book` 移除。

### 5.3 Undercasting 对账与快捷栏／法术菜单刷新
- **Undercasting**：`TemporaryKnowledge01464` 已在 `UnitPartTemporarySpellsKnown.AddEntry` / `RemoveEntry` / `OnPostLoad` / `HandleUnitGainLevel` 后自动调用 `PsychicUndercasting.Reconcile(book)`。因此当星座授予高环可降环法术（如 `synesthesia_mass` L9）并在后续移除时，玩家原有的低环永久已知法术（如 `synesthesia` L3）不会因降环覆盖而在 Buff 结束时被误删。
- **UI 与快捷栏刷新**：在星座 Buff 激活（`OnTurnOn` / `OnActivate`）、升级增量解锁（`HandleUnitGainLevel`）与失效（`OnTurnOff` / `OnDeactivate`）时，必须调用 `Owner.UISettings?.SetDirty()`，确保自发施法弹出菜单与动作条状态立即同步。

---

## 6. 升级预览 (`CopySpellbook`) 与重训／洗点 (`Respec`)

1. **升级预览 (`UnitHelper.CopySpellbook`)**：
   - `PsychicUndercasting.CopiedBookPatch` 已挂钩 `UnitHelper.CopySpellbook` 并调用 `TemporaryKnowledge01464.RestoreCopy(unit, copy)`。
   - 因为星座法术通过 `UnitPartTemporarySpellsKnown` 登记，在 `CopySpellbook` 阶段：
     - 星座临时法术在预览法术书中被正确标记为 `IsTemporary = true`；
     - **不占用**玩家升级时的永久已知法术配额（`SpellsKnown`）；
     - **不阻塞**玩家在升级时将该法术选为永久已知法术。
2. **重训／洗点与特性移除清理**：
   - 在 `WrittenInTheStarsFeature0081` 上挂载清理组件 `WrittenInTheStarsFeatureCleanup0081 : UnitFactComponentDelegate`：
   - 当 `WrittenInTheStarsFeature0081` 被停用或移除（`OnDeactivate`）时，遍历 `Owner.Buffs` 移除全部 10 个星座对应的 `WrittenInTheStars{Constellation}Buff0081`，从而立即触发各 Buff 的 `RemoveEntry(Runtime)`，确保重训/洗点后法术书中零残留星座法术、零残留 Buff。

---

## 7. 外部法术 Provider 校验与原子拒登／迟加载策略

### 7.1 外部 Provider 门禁规则
- `EsotericStarseeker0081.Register(BlueprintsCache cache)` 在创建和发布任何蓝图前，必须对 `A-catalog90.json` 中的全部 90 个法术条目执行前置门禁校验：
  1. **73 个原生 Wrath 法术 + 12 个内部 Prototype 法术**：
     - 必须在 `cache` 中存在，类型为 `BlueprintAbility`，`ability.Type == AbilityType.Spell`，`ability.Parent == null`，且包含有效执行负载（`AbilityEffectRunAction` 或 `AbilityVariants`）。
  2. **5 个外部 Provider 法术**：
     - 必须复用 `PsychicSpellIntegration.Available(cache, link)` 等价的完整五重契约校验：
       - `!PsychicSpellIntegration.IsDisabled(link.ModId)`（若对应 Provider Mod 被禁用，直接返回 `false`）
       - `cache.Load(BlueprintGuid.Parse(link.Guid)) is BlueprintAbility spell`
       - `spell.name == link.Name`
       - `spell.Type == AbilityType.Spell && spell.Parent == null && spell.School == link.School`
       - 具备可执行负载（`AbilityEffectRunAction` 或非空 `AbilityVariants`），且不含空壳/缺失引用
       - 若 `link.PublishedListRequired == true`，必须已出现在至少一个标准职业法术表（Wizard / Cleric / Druid / Bard / Alchemist / Inquisitor / Magus / Shaman / Witch / Bloodrager / Warpriest / Paladin / Ranger）中。
- **原子拒登（No Partial Registration）**：
  - 若 90 个法术中有**任何一个**未通过上述校验，`EsotericStarseeker0081.Register(cache)` **立即返回 `false`**：
    - 不向 `cache` 写入任何 `0081` 蓝图；
    - 不修改 `PsychicClass.m_Archetypes`；
    - 不修改 `PsychicProgression.UIGroups`。
- **迟加载自动闭合 (`RegisterOptional`)**：
  - 当外部 Provider Mod 在 `BlueprintsCaches_Init` 后期加载并触发 `BlueprintRegistration.RegisterOptional(cache)` 时，`RegisterAvailableModule(cache, "0081 Esoteric Starseeker", EsotericStarseeker0081.Register)` 再次执行；此时若 90 个法术全部就绪，立即原子发布整个 `EsotericStarseeker0081` 蓝图图谱并记录 `ArchetypeBatch01445` 回执。

---

## 8. 确定性 GUID、TypeId、图标与私有蓝图图谱规范

### 8.1 确定性 Token 与 GUID 命名表（统一使用 `PsychicAssets.Id(token)`）

| 蓝图角色 | Token 命名 (`PsychicAssets.Id(token)`) | 确定性 GUID (`PsychicAssets.Id`) | Blueprint 类型 |
|---|---|---|---|
| 变体根蓝图 | `"EsotericStarseekerArchetype0081"` | 由 `PsychicAssets.Id` 生成 | `BlueprintArchetype` |
| 无奖励法术学科选择 | `"EsotericStarseekerDisciplineSelection0081"` | 由 `PsychicAssets.Id` 生成 | `BlueprintFeatureSelection` |
| 核心特性 | `"WrittenInTheStarsFeature0081"` | 由 `PsychicAssets.Id` 生成 | `BlueprintFeature` |
| 每日资源 | `"WrittenInTheStarsResource0081"` | 由 `PsychicAssets.Id` 生成 | `BlueprintAbilityResource` |
| 调谐母技能 | `"WrittenInTheStarsAbility0081"` | 由 `PsychicAssets.Id` 生成 | `BlueprintAbility` |
| 10 个星座子技能 | `"WrittenInTheStars{Constellation}Ability0081"`<br>(`{Constellation}` $\in$ `{Archer, Charioteer, Comet, Courtesan, Crest, Daughter, Dragon, Mountain, Rider, Tree}`) | 由 `PsychicAssets.Id` 生成 | `BlueprintAbility` (×10) |
| 10 个星座 Buff | `"WrittenInTheStars{Constellation}Buff0081"`<br>(`{Constellation}` 同上) | 由 `PsychicAssets.Id` 生成 | `BlueprintBuff` (×10) |

- **总计私有蓝图数**：`1 (Archetype) + 1 (DisciplineSelection) + 1 (Feature) + 1 (Resource) + 1 (Parent Ability) + 10 (Variant Abilities) + 10 (Buffs) = 25` 个私有蓝图，全部交由 `SpellBatch01447.PublishArchetype(cache, f, cls, root, new BlueprintFeatureBase[] { noSpells, feature })` 原子发布与验签。

### 8.2 90 项星座法术静态映射表（供 Role B 直接编码）

| 星座 (`Constellation`) | L1 | L2 | L3 | L4 | L5 | L6 | L7 | L8 | L9 |
|---|---|---|---|---|---|---|---|---|---|
| **Archer** | `true_strike`<br>`2c38da66e5a599347ac95b3294acbe00` | `protection_from_arrows`<br>`c28de1f98a3f432448e52e5d47c73208` | `flame_arrow`<br>`95810d2829895724f950c8c4086056e7` | `DimensionDoorBase`<br>`4a648b57935a59547b7a2ee86fb4f26a` | `lightning_arc`<br>`d2cff9243a7ee804cb6d5be47af30c73` | `MassFlySpell` *(TotF)*<br>`2ddc377f87b94aba9e8bab6c03f477e8` | `true_seeing`<br>`4cf3d0fae3239ec478f51e86f49161cb` | `shout_greater`<br>`fd0d3840c48cafb44bb29e8eb74df204` | `foresight`<br>`1f01a098d737ec6419aedc4e7ad61fdd` |
| **Charioteer** | `expeditious_retreat`<br>`4f8181e7a7f1d904fbaea64220e83379` | `effortless_armor`<br>`1a3b19e83ffc4964f8f03d58d7360623` | `displacement`<br>`903092f6488f9ce45a80943923576ab3` | `freedom_of_movement`<br>`0087fc2d64b6095478bc7b8d7d512caf` | `angelic_aspect`<br>`75a10d5a635986641bf3ceceec87217e` | `serenity`<br>`d316d3d94d20c674db2c24d7de96f6a7` | `ParticulateFormAbility` *(EC)*<br>`99e39437184a46b994da740ce399f591` | `frightful_aspect`<br>`e788b02f8d21014488067bdd3ba7b325` | `winds_of_vengeance`<br>`5d8f1da2fdc0b9242af9f326f9e507be` |
| **Comet** | `magic_missile`<br>`4ac47ddb9fa1eaf43a1b6809980cfbd2` | `ReturningWeapon`<br>`9b2ff8fdbe843c141aa7ab9eed3decef` | `fireball`<br>`2d81362af43aeac4387a3d4fced489c3` | `ice_storm`<br>`f2115ac1148256b4ba20788f7e966830` | `fire_snake`<br>`ebade19998e1f8542a1b55bd4da766b3` | `sirocco`<br>`093ed1d67a539ad4c939d9d05cfe192c` | `sunbeam`<br>`1fca0ba2fdfe2994a8c8bc1f0f2fc5b1` | `sunburst`<br>`e96424f70ff884947b06f41a765b7658` | `MeteorSwarm`<br>`5e36df08c71748f7936bce310181fb71` |
| **Courtesan** | `charm_person`<br>`d7fdd79f0f6b6a2418298e936bb6c0c9` | `burst_of_adrenaline` *(CotW)*<br>`b0a50deb87a2b4e7bf38d9aff5c9ffcc` | `synesthesia` *(CotW)*<br>`55c8366f5298b04e4ba678f49bc46005` | `rainbow_pattern`<br>`4b8265132f9c8174f87ce7fa6d0fe47b` | `dominate_person`<br>`d7cbd2004ce66a042aeab2e95a3c5c61` | `cloak_of_dreams`<br>`FF8f1534f66559c478448723e16b6624` | `waves_of_ecstasy`<br>`1e2d1489781b10a45a3b70192a9be385` | `irresistible_dance` *(CotW)*<br>`120c84ce90b778cb82f025c2b62a9bf9` | `synesthesia_mass` *(CotW)*<br>`f3277b49cc8ab2f4b03464bd11ef6a4b` |
| **Crest** | `mage_armor`<br>`9e1ad5d6f87d19e4d8883d63a6e35568` | `force_sword` *(CotW)*<br>`f9ce1e9088de0d502a98bcce636b0615` | `magic_vestment`<br>`9e265139cf6c07c4fb8298cb8b646de9` | `echolocation`<br>`20b548bf09bb3ea4bafea78dcb4f3db6` | `burst_of_force` *(CotW)*<br>`8266f8809fc2de62bbd5ab64abe4b683` | `fluid_form` *(CotW)*<br>`cee712a5129a14e1973f89e1542671288` *(注:`cee712a5129a14e1973f89e542671288`)* | `spell_turning`<br>`1f0738a5c5371b94c9c33b8f392c78be` | `temporal_stasis` *(CotW)*<br>`8b0761f3873aefb976c0f47fec3c600c` | `time_stop` *(CotW)*<br>`585f7776285d17b60b9e8e2fe42a69be` |
| **Daughter** | `faerie_fire`<br>`5fa1b8b4226c8f141b8319143c707200` | `HypnoticPatternAbility` *(EC)*<br>`56b8f0304a704a67b3c35cbe8c774854` | `deep_slumber`<br>`7658b74f626c56a49939d9c20580885e` | `serenity` (L4 slot)<br>`d316d3d94d20c674db2c24d7de96f6a7` | `song_of_discord`<br>`c927a8b0cd3f5174f8c0b67cdbfde539` | `PublishHeroismGreater`<br>`e15e5e7045fda2244b98c8f010adfe31` | `insanity`<br>`2b044152b3620c841badb090e01ed9de` | `euphoric_tranquility`<br>`cbfd2f5279f5946439fe82570fd61df2` | `overwhelming_presence`<br>`41cf93453b027b94886901dbfc680cb9` |
| **Dragon** | `cause_fear`<br>`bd81a3931aa285a4f9844585b5d97e51` | `resist_energy`<br>`21ffef7791ce73f468b6fca4d9371e8b` | `dragon_breath`<br>`5e826bcdfde7f82468776b55315b2403` | `fear`<br>`d2aeac47450c76347aebbc02e4f463e0` | `spell_resistance`<br>`0a5ddfbcfb3989543ac7c936fc256889` | `form_of_the_dragon_i`<br>`f767399367df54645ac620ef7b2062bb` | `form_of_the_dragon_ii`<br>`666556ded3a32f34885e8c318c3a0ced` | `form_of_the_dragon_iii`<br>`1cdc4ad4c208246419b98a35539eafa6` | `Shapechange`<br>`22b9044aa229815429d57d0a30e4b739` |
| **Mountain** | `stone_fist`<br>`85067a04a97416949b5d1dbf986d93f3` | `stone_call`<br>`5181c2ed0190fc34b8a116278315bf44` | `InvisibilityPurge` *(COP)*<br>`c3647ce7d6da49faa6e86830841e56e1` | `stoneskin`<br>`c66e86905f7606c4eaa5c774f0357b2b` | `chains_of_light`<br>`f001c73999fb5a543a199f890108d936` | `contingency` *(CotW)*<br>`b39dd2d96af348236b0117f57e5e445d` | `legendary_proportions`<br>`da1b292d91ba37948893cdbe9ea89e28` | `seamantle`<br>`7ef49f184922063499b8f1346fb7f521` | `clashing_rocks`<br>`01300baad090d634cb1a1b2defe068d6` |
| **Rider** | `Command`<br>`feb70aab86cc17f4bb64432c83737ac2` | `grace`<br>`Files/8a28a811ca5d20d49a863e832c314115` (`8a28a811ca5d20d49a863e832c314115`) | `haste`<br>`486eaff58293f6441a5c2759c4872f98` | `orders_wrath`<br>`55e3b3817f18759489966c695b31a724` | `divide_mind` *(CotW)*<br>`fc27ca9e65567cf7830601564a677d68` | `circle_of_death`<br>`a89dcbbab8f40e44e920cc60636097cf` | `dictum`<br>`2453d5eb2a12872499f09f1be02916de` | `unholy_aura`<br>`426f012b202943d40b0c9f79c3a74082` | `weird`<br>`870af83be6572594d84d276d7fc583e0` |
| **Tree** | `doom`<br>`fbdd8c455ac4cde4a9a3e18c84af9485` | `howling_agony` *(CotW)*<br>`981a4173d895e71184d9a7f557a9e1d4` | `RayOfExhaustion`<br>`8eead52509987034ea9025d60cc05985` | `dimension_door_caster_only` (`DimensionDoorCasterOnly`)<br>`a9b8be9b87865744VA` *(注:`5bdc37e4acfa209408334326076a43bc`)* | `vampiric_shadow_shield`<br>`3276511512A` *(注:`a36b8c6c911303c4b912b56f2076770f`)* | `eyebite`<br>`3167d30dd3c622c46b0c0cb242061642` | `finger_of_death`<br>`6f1dcf6cfa92d1948a740195707c0dbe` | `horrid_wilting`<br>`08323922485f7e246acb3d2276515526` | `NewSpell.UseAkashicForm` *(PP)*<br>`58bcb999b3ad46c289dc68cba9c50bd0` |

*(注：全部 90 个 GUID 的精确值以 `<local starseeker evidence>\A-catalog90.json` + `Daughter L2 = 56b8f0304a704a67b3c35cbe8c774854` 为唯一权威数据源；其中 `Crest L6` 为 `cee712a5129a14e1973f89e542671288`，`Tree L4` 为 `5bdc37e4acfa209408334326076a43bc`，`Tree L5` 为 `a36b8c6c911303c4b912b56f2076770f`。)*

### 8.3 确定性 TypeId 分配表

| 组件类名 | 固定 `[TypeId]` | 职责 |
|---|---|---|
| `WrittenInTheStarsRestriction0081` | `[TypeId("0081c4b29e7d4a5183f0112233440001")]` | `IAbilityCasterRestriction`：校验施法者具有 Psychic 法术书（`MaxSpellLevel >= 1`）且当前未激活同一星座 Buff |
| `WrittenInTheStarsApplyAction0081` | `[TypeId("0081c4b29e7d4a5183f0112233440002")]` | `ContextAction`：根据 Psychic 等级（1–10 上限 1，11–20 上限 2）执行 FIFO 星座 Buff 替换并挂载目标星座 Buff |
| `ConstellationCircleSpell0081` | `[TypeId("0081c4b29e7d4a5183f0112233440003")]` | `UnitFactComponentDelegate, IUnitGainLevelHandler`：按 `Circle <= book.MaxSpellLevel` 将单环星座法术注册/移除于 `UnitPartTemporarySpellsKnown`，并触发 `Owner.UISettings?.SetDirty()` |
| `WrittenInTheStarsFeatureCleanup0081` | `[TypeId("0081c4b29e7d4a5183f0112233440004")]` | `UnitFactComponentDelegate`：当 `WrittenInTheStarsFeature0081` 移除/洗点时清理全部激活的星座 Buff |

### 8.4 图标与本地化规范
- **全英文文本**：所有 `m_DisplayName`、`m_Description` 及技能限制提示信息必须使用标准英文（可直接采用 `Psychic.cs:458–529` 的官方星座与特性英文描述）。
- **非空图标 (`Icon`)**：
  - `WrittenInTheStarsFeature0081` 与 `WrittenInTheStarsAbility0081`：使用原生法术 `Foresight` (`1f01a098d737ec6419aedc4e7ad61fdd`) 或 `Stars` / `TrueSeeing` 的非空图标。
  - 每个星座的子技能 `WrittenInTheStars{Constellation}Ability0081` 与 `WrittenInTheStars{Constellation}Buff0081`：直接复用该星座 **1 环代表法术**（或原版 CotW 中对应的原生法术）的非空 `Icon`，并在发布前断言所有 25 个蓝图中 `BlueprintUnitFact` 的 `Icon != null`。

---

## 9. Role B 允许修改的文件边界
1. **新增**：`<CotW workspace>\outputs\CallOfTheWild.Wrath.Prototype\src\EsotericStarseeker0081.cs`
2. **微调（目录补齐）**：`<CotW workspace>\outputs\CallOfTheWild.Wrath.Prototype\src\PsychicSharedSpellCatalog.cs`（补入 `HypnoticPatternAbility` 链接项）
3. **微调（局部 11->15 Major 门禁）**：`<CotW workspace>\outputs\CallOfTheWild.Wrath.Prototype\src\PsychicMajor01460.cs`（对 `EsotericStarseekerArchetype0081` 将 `MajorGrant01460` / `Has` 门槛从 11 级延后至 15 级）
4. **微调（模块挂载）**：`<CotW workspace>\outputs\CallOfTheWild.Wrath.Prototype\src\BlueprintRegistration.cs`（在 `RegisterAll` 与 `RegisterOptional` 中挂载 `EsotericStarseeker0081.Register`）
5. **新增／扩展定向测试**：`<CotW workspace>\wotr-testing\` 下针对 `0081 Esoteric Starseeker` 的定向测试类（对照 `C-cases.json` 逐条断言）。
