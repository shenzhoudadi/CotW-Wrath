# 英灵共鸣与阿德里尔传奇英灵：运行时规则与安装接线

```text
dotnet run --project tests/SpiritInfluenceScenarios/SpiritInfluenceScenarios.csproj -- artifacts/Release/MediumClass/MediumClass.dll Strings/Feats.json
```

不带参数时只运行业为部分；带发布 DLL 时追加安装产物审计。本套件真正链接五个生产运行时组件（此前其它套件中它们只是桩对象）：

- `MediumContextSpiritInfluenceComponent`（神话英灵共鸣：隐藏标记 buff）
- `MediumSpiritInfluenceAllyComponent`（盟友身上的镜像加值）
- `MediumSpiritInfluenceFeatureComponent`（神话能力本身，两种获得顺序的收敛）
- `MediumDeathCurseComponent`（阿德里尔的恶咒）
- `AbilityRequirementSpecificSpirit`（仅限特定英灵降灵时可用）

外加生产 `Guids.cs` 与 `LocalizationText.cs`。游戏对象是显式 API 模型；不启动 Unity、不执行游戏。

## 行为覆盖（0.3.1 为 62 项；0.3.2 新增恶咒开关 3 项，总数以本地运行输出为准）

**盟友镜像**：降灵开始授予盟友镜像特性；通灵者自身不获得；镜像值 = 英灵同调等级 + 英灵专攻；等级提升即时更新；重复刷新不叠加；多英灵同调项目取最高值而非相加；第二个英灵独有的属性仍会镜像；标记修饰符由盟友自己的 Runtime 持有、描述符与通灵者自身加值一致。

**归属转移**：两名通灵者共享一名盟友时，结束先手降灵会把授予移交给剩下的通灵者（特性保留、镜像值切换、先手的授予清单清空、后手收养；新提供者没有的属性被清除）；最后一名提供者结束降灵才移除特性与修饰符。

**边界**：无英灵状态 / 无主英灵的通灵者不授予任何东西；拆解时容忍 null 与已释放的授予；授予的修饰符通过**降灵蓝图上的属性清单**清除，因此即使提供者的运行时英灵表已丢失也照样清干净（特性本体则保留到降灵结束）；重新降灵后按新运行时实例重新授予。

**神话特性收敛**：先获得能力后降灵、先降灵后获得能力两条路径都只产生一个标记 buff，不重复；失去能力时移除标记。

**恶咒**：对**自己挑战目标**的**武器**攻击，每种已开启的能量各追加 1d6；五个能量开关各带一份组件，三个开关同时开启即三颗骰；同一组件内重复的能量只算一次；未开启任何能量时不动；无武器不动；无挑战 buff 的目标不动；**别人**的挑战目标不动；旧档丢失施法者链接（Context 为空）时视为自己的目标；换英灵后残留的旧 buff 保持惰性。

**能力限制**：降灵指定英灵时通过；降灵其它英灵不通过；`Not` 反转语义；无施法者、英灵引用缺失时返回 `Not`；UI 文本随中文/英文区域切换。

## 安装产物审计（0.3.1 为 167 项；0.3.2 项数以本地运行输出为准，需传发布 DLL）

- 20 个新 GUID 常量（英灵共鸣 2 个 + 阿德里尔 15 个 + 缚灵者 3 个）全部在 IL 中被引用——防止"常量定义了但蓝图没接线"。0.3.2 起恶咒是 5 个能量开关（能力 + buff 各 5），十个旧组合 GUID 只保留常量，另有断言确认 `DeathCurse` 不再用它们构建蓝图。
- 阿德里尔升级表恰为 1/6/11/17 四条：1 级挑战、6 级瞬袭、11 级疾袭、17 级恶咒。等级常量从 `AddEntry` 调用点回溯 `newarr` 提取，断言的是真实 IL 而非调用次数。
- 降灵选择（`ChannelSpirit`，含编译器生成的 lambda 方法）接线了阿德里尔的降灵能力与四级英灵之力；挑战与恶咒两个类型各自引用其全部 GUID。
- 33 个本地化 key（0.3.2：恶咒改为 5 个能量名，另加缚灵者"永不安宁"名称与描述）全部存在于 `Strings/Feats.json` 且中英文均非空。名称类 key 在配置器中是运行时拼接的，故只校验交付包中存在，不断言 IL 字面量；蓝图接线由上面的 GUID 检查覆盖。

## 模型依据（已核对的原生行为）

- `UnitEntityData.RemoveFact` 在移除前关闭 fact，从而触发组件的 `OnTurnOff`——本套件修正了模型后由"最后一名提供者退出时修饰符清除"这条断言反过来验证。
- `AddFact` 为每个 fact 构建全新运行时组件实例（`CreateRuntimeFactComponent`）；重新授予后必须重新取得组件实例，否则修饰符归属会错位。
- 蓝图引用按目标缓存：同一 GUID 的 `GetRef` 返回同一实例，与游戏的引用缓存一致。
- 传奇英灵与基础职阶共享英灵专攻的规则（`EffectiveSpiritFocus`）由 `tests/MythicRegistration` 链接真实 `MythicSpirits.cs` 覆盖；本套件用逐项 focus 的简化桩，避免拖入整套 BlueprintCore 配置器模型。

缚灵者变体的行为规则在 `tests/InfluenceScenarios`（695 项），其安装结构审计在 `tests/SpiritBinderArchetype`（40 项）。

## 0.3.4：恶咒两种上限（DeathCurseLimitChecks.cs）

本套件另链接真实的 `MediumDeathCurseRules`、`ContextActionToggleDeathCurse`、`AbilityRequirementDeathCurseSlot` 与 `MediumDeathCurseMigration`：五选二的十种组合、第三种被拒绝（界面限制与执行入口）、关闭后换类型、0/1/2 种、重复点击与排队命令、警告失败隔离、五个开关并存时伤害只有两份、旧档 3–5 个开关按最早开启的两个规范化且幂等、非挑战目标/无降灵/降灵结束。成品 DLL 审计另检查五个变体使用新动作与限制、两个新 TypeId 唯一、恢复入口顺序。负对照：把 0.3.3 成品 DLL 作为第一个参数运行，新增的 8 项成品检查失败。
