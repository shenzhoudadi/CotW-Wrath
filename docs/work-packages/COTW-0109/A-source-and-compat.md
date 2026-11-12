# A 原作与 Wrath 依赖核对

COTW-0109 Iroran Paladin 属于现有允许范围。Wrath 提供原生圣武士、Irori、徒手武器、气池及神圣联结原语。以 `a3f534625643b94cfa644e19c8a7fbcb7d6e528a` / 0.14.70 TEST 为生产基线。[施工 issue #11](https://github.com/shenzhoudadi/CotW-Wrath/issues/11)

## 查重与固定来源

- 查阅上游 main、当时四个公开分支、全部七个公开 PR（含关闭／合并）、公开认领，以及 fork 的三个分支。上游生产源码和提交标题未发现 Iroran 或 Personal Trial。fork `sxl1216/CotW-Wrath` 的 main 为 `04159f63b46d9ba67b65947cfdc84466bf1456b1`，相对基线独有变更包括 Lore Spirit `4138e6f`、Sacred Servant 和雾效。两条贡献分支对应既有 PR #5／#8。Speaker 由 issue #6 保留认领。[上游 PR](https://github.com/shenzhoudadi/CotW-Wrath/pulls?q=is%3Apr) · [fork 对比](https://github.com/sxl1216/CotW-Wrath/compare/a3f5346...04159f6)
- 对已留存的 ExpandedContent、PrestigePlus、CharacterOptionsPlus、TabletopTweaks 等供体源码查 `IroranPaladin`、`Iroran Paladin` 和 `PersonalTrial`。相关代码命中为 Kingmaker 原作和另一 Kingmaker 项目的兼容。这个查重覆盖可见分支和已核对供体，玩家可报告未索引的实现。
- 原作固定为 `Holic75/KingmakerRebalance@1332fb0db844b7863f484ca978bea2349fe49769`。本机六份相关文件的 Git blob 已与 GitHub 核对，值在 `A-native-anchors.json`。[原作变体](https://github.com/Holic75/KingmakerRebalance/blob/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/Archetypes/IroranPaladin.cs) · [原作重掷](https://github.com/Holic75/KingmakerRebalance/blob/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/ShowRerolls.cs)

## 原作范围与 Wrath 接线差异

- `IroranPaladin.cs:79–151` 是完整加减表。Wrath 的 5 级 `PaladinDivineBondSelection` 为 `ad7dc4eba7bf92f4aba23f716d7a9ba6`，含武器 `WeaponBondProgression` 与坐骑进展。必须替换 Wrath 实际入口。`PaladinDeitySelection` 虽然存在，但当前归档的 `PaladinProgression` 一级条目并未直接列它。真实 deity 选择及已有信仰需走本作等级流程验证。
- `:248–311` 的 Personal Trial 以 swift 施放，资源加次数实际是 4、8、12、16、20。`OnePlusDivStep(4)` 令数值在 4、8、12、16、20 成长，20 级 +6。说明写的“每三级加一次”与代码冲突。原作施加者身份用于攻击、AC、伤害及保存检定。`NewMechanics.cs:3234–3262` 查保存检定来源，`:5937–5947` 查气能力的减伤绕过。
- `:186–230` 的气能力包裹 Scaled Fist 的气池 feature。Wrath 归档同一 feature `ae98ab7bda409ef4bb39149a212d6732` 自带气池和 Extra Attack。原作通过 `ClassToProgression.cs:370–397` 扩共享气池的等级贡献。Wrath `UnitAbilityResourceCollection.Add/Remove` 已对同一资源持有做 retain/release，`BlueprintAbilityResource.GetMaxAmount(UnitDescriptor)` 提供角色作用域。
- `:315–368` 对应徒手与 Confident Defense。原作 `settings.json` 的 `balance_fixes_monk_ac` 默认为 true。Wrath `EmptyHandWeaponOverride` 已支持按事实持有原生拳头实体，`UnitBody.SetEmptyHandWeapon/RemoveEmptyHandWeapon` 处理强度、覆盖和恢复。六个等级的原生拳头身份已核对。Wrath Scaled Fist 目前采用自己的 Buff 和 Property。C 采用独立的角色修正，保留原生贡献与叠加边界。
- `:234–244` 的 Divine Body 继承武器联结。Wrath 原生 `AbilityCasterMainWeaponCheck` 在 `PrimaryHand.HasWeapon == false` 时拒绝，而 `HandSlot.HasWeapon` 使用 `MaybeWeapon`，普通空手会被拒绝。`ContextActionWeaponEnchantPool` 本身会回退到 `Body.EmptyHandWeapon`。B 需使用同一真实拳头解析作为施放资格和执行目标。
- `:155–182` 两道光环使用 13 英尺蓝图半径，描述为 10 英尺。`ShowRerolls.cs:93–134` 定义额外取高、取消自己的取低、盟友增加一骰后取第二小。Wrath `RuleRollD20` 继承 `RuleRollDice`。`AddReroll` 记录带 `EntityFact Source` 的有符号量，`Roll/Reroll` 只按净量执行 Min/Max。`ModifyD20` 的 `Override` 分支为独立固定骰值。实际晚到重掷调用方有 Azata Favorable Magic、Demon Socothbenoth 和 RerollConcealment。主项目 Psychic Relentless/Pageantry 会在 `OnTrigger` 入口附加重掷。C 保留这些来源并覆盖实际两种调用入口。
- 原生 `UniqueBuff.OnActivate/OnDeactivate` 通过施法者的 `UnitPartUniqueBuffs` 登记。不同施法者在同一目标的共存仍取决于 Buff stacking，B 需以实际两施法者检查证明。`AuraFeatureComponent` 会在生命状态变化时移除／恢复载体。掷骰时仍以实际活动光环和施法者清醒状态为准。

## 已核实的输入与证据边界

本地检查使用当前安装归档和托管元数据。`Assembly-CSharp.dll` SHA256 为 `2CB7160B7154D4FFACC77B9C51B1EB26199E1294300F04FDFC073367B2EF8953`，Harmony 2.0.4.0 SHA256 为 `DE50CD610EA22854C8930EC5433276713AEE1BB0ECD785D6B15A0661C2FA9FD5`。发布的映射只含身份、签名和选定引用。

现有入口参考 `SacredServant0110.Register` 的原生圣武士联结替换与原子发布，复用 `ArchetypeBatch01445`／`MagusBatch01454`、`PsychicAssets` 的已有原语。旧 `HOLD_SHARED_REROLL_AC_KI` 反映适配尚待 B 实现。本次已定位原生输入和可用调用点，C 明确约束。编译、定向运行及玩家实机均未运行。
