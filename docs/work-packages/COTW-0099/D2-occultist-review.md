# COTW-0099 · D 独立复查（2026-11-16）

**结论：NEEDS_FIX。** 候选为 `77ea03b1e74190a98fab802e31dfd22515b7678a`，合同为 `C-0099-20261115-r1`。本轮由未参与 A/C/B 的独立 D（Codex 审阅代理 `occultist_d`）审阅执行链，协调者复跑两个现有宿主并构造定向反例。以下结论只覆盖 Abjuration 开发切片。切片待 B 修复后复验，保持 `SlicePublished=false`、`releaseEligible=false`，认领继续开放。[原 D 自查](D-occultist-review.md) · [B 修复记录](B-fix-round.md) · [合同](C-occultist-contract.md) · [issue #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15)

## 原生宿主已复现

### D2-01 / P1 · 不同施放共用去重键

[Occultist0099NativeWiring.cs:192](../../../src/Occultist0099NativeWiring.cs#L192) 把 `AbilityData.UniqueId` 当作每次施放的身份。Wrath 在构造 `AbilityData` 时分配该 ID。原生授予的 `Ability.Data` 是字段，两条不同 `UnitUseAbility` 命令可持有同一对象。测试构造两条原生命令，通过真实资源入口支付，首条支付成功，第二条预检失败，剩余总点数应为 5，实测 6，学派池仍有 3 点。既有宿主每次显式 new 一个 AbilityData，漏掉了复用。B 应由执行事件提供稳定身份，覆盖同一命令重复回调只扣一次、两条命令各扣一次、只读查询不消耗。

### D2-02 / P1 · 模型保住账本，原生事实替换丢账本

[Occultist0099Native.cs:35](../../../src/Occultist0099Native.cs#L35) 把状态保存于事实组件实例，[OnActivate](../../../src/Occultist0099Native.cs#L99) 对空状态建立新预算。[Rules.cs:706](../../../src/Occultist0099Rules.cs#L706) 的 RemoveClass 能保留去重集合，当前原生生命周期没有调用或转移该状态。复跑模型 D-F01 在同一对象上正确拒绝重复支付。改走原生事实管理器先移除、同一 camp 创建新事实后，历史支出和原支付身份都丢失，原能力重新通过同一 ID 的支付预检。B 应验证角色级周期记录跨原生事实销毁/重附，并接入重训的来源清退和剩余余额规则。

### D2-03 / P1 · 共享根阻断外职业的学习预检

[Occultist0099NativeWiring.cs:393](../../../src/Occultist0099NativeWiring.cs#L393) 的 SelectSpell.Check 后缀先找共享根上的 gate，随后覆盖原结果。gate 在 [:272](../../../src/Occultist0099NativeWiring.cs#L272) 对非 0099 书返回 false。原生离线测试中，外职业书有原生可选槽，MageShield 的 Check 仍被拒；B 的外书用例只调用 Apply，未经过 Check。B 应先按私有书识别入口，保留外职业原生结果；私有根元数据可保持在私有目录中，避免向共享根追加影响外部调用的组件。

### D2-04 / P1 · 表外学习与槽扣减绕过

[Occultist0099NativeWiring.cs:393](../../../src/Occultist0099NativeWiring.cs#L393) 与 [AddKnown 前缀](../../../src/Occultist0099NativeWiring.cs#L410) 对没有 gate 的根直接透传。测试向私有书提供不在目录中的 Evocation 根，原生 Check 接受，AddKnown 实际学会，绕开来源账本。另用已挂 gate 的 MageShield 提交错误环位，原生 SelectSpell.Apply 先 SpendSlot、后 AddKnown，后者拒绝时选择槽仍被占用。不能把 AddKnown 的空返回当作选择事务回滚。私有选择应在槽扣减前验明正确书、来源、环位和根，确保选择、账本与 Native Known 一致提交。固定 Knacks 或单独授予来源须有自己的明确入口。当前学习预检读取事实里上次同步的 Int，检查时应使用实时值；实际选择槽也应映射到对应法器来源，避免由 RecordLearn 选中首个合格槽。后两项是静态观察。

## 独立源码核实

- **D2-05 / P1 · 拒付结果来得太迟。** Wrath 的 UnitUseAbility.OnAction 先预检，再触发 RuleCastSpell；RuleCastSpell.OnTrigger 调用 Spell.Cast，返回后 OnAction 才 Spend。[Spend](../../../src/Occultist0099NativeWiring.cs#L218) 的拒绝仅从支付回调返回，无法撤销已送入 AbilityExecutor 的执行。若施法前置监听中耗尽余额或移走来源，当前接线没有取消执行的机制。B 应从真实命令或规则事件验证「预检通过后状态失效，效果零触发」，并保留合法启动后豁免或法抗失败的正常付费。此分支为静态确认，离线宿主未执行完整 RuleCastSpell。
- **D2-06 / P1 · 即时脚本休息复用了上一次 camp 的身份。** [RestEventId](../../../src/Occultist0099Native.cs#L82) 只读 OverallCampsCount。Wrath 的 StartScripted(immediate:true) 直接走 ApplyRestInterval → ApplyRest → HealAndApplyRest；只在 CampCoroutine 自增 camp 计数。连续两次成功的即时脚本休息可以走同一 ID，因此第二次可能未刷新焦点。官方离线 RESTN 手动增加计数，确认的是相同 camp 去重。B 应为真实成功休息区分不同事件，并保留同一事件的重复通知幂等。即时脚本休息为静态确认。

## 证据与边界

- 单次重编译主工程及原生宿主，0 警告、0 错误，**3.239 秒**。候选五个生产源文件 SHA256 均匹配 B 记录。DLL SHA256 `4180DA3380C637FD72F49C460E4CE178B1110A52C987903840F65F03D8FC61A0`，仅指本轮编译产物。
- 原 B 原生宿主 **80/80**，**3.336 秒**，异常与跳过组件均为 0；原 D 模型反例 **5/5**，**1.933 秒**。MF07 关乎 Spell Power；旧报告归入 MF07 的 RESTN 是休息身份证据，Spell Power 原生链未跑。SP02 的降低智力断言调用模型 CanCast；Wrath Spellbook.SpendInternal 源码有实时属性判定，本轮未跑已知法术的完整施放。
- 独立补充宿主 **32 条断言，8 失败**，**3.185 秒**，其中 12 条核对上述四组反例，其余是供体和夹具检查。首次与第二次调用构造函数分别遇到 RuntimeActionType 与 HasMetamagic 内的 Unity profiling ECall，中止于反例前（**3.247 / 3.209 秒**）。第三次仅在构造无超魔的两条命令时临时跳过 HasMetamagic，支付和学习均执行原生入口与候选生产代码。三次补充宿主编译 **1.289 / 1.100 / 1.047 秒**，均 0 警告、0 错误。
- [编译、指纹与复跑命令](D2-occultist-evidence.json)。本机交接目录 `C:/Users/xuenc/agent-tools/occultist-next/D-77ea03b/` 保留日志、反例断言原件与宿主差异。13 个原生根取自审阅过的档案，4 个 CotW 根走已有构建器，Cloak of Winds 为隔离环境供体。
- SP08 仍为 `not_run`。真实原子注册、游戏／UMM、实机、升级 UI、重训控制器、存读档、外部提供方启用、安装包打包和上传均未运行。优先修复 D2-01～04 的实际复现，再补原生执行与休息边界；验收只重跑受影响路径。

本轮补充宿主的两处临时目录原拟清理。执行工具在运行前以 blocked by policy 拒绝递归删除，两处目录保留本机，详见证据 JSON。
