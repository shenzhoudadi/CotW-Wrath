# COTW-0046 Speaker for the Past — D 独立复查

日期：2026-11-08。合同：`C01-SPEAKER-0046-20261108-r1`。结论：**NEEDS_FIX；不得标为已完成／不得据此发布新 TEST 包。**

本轮是生产只读复查，没有修改生产源码、编译、重跑检查、抬版本、启动游戏或 UMM。问题由当前源码的可定位执行分支直接判定；下列复现步骤是修复者应执行的最小正反例，**不是声称 D 已运行了这些规则链**。不追加全量检测、旧证书或玩家验收门槛。

## 审查对象与已有证据

- 已交付基线：main `8c5cf5ed1c236ca62e5c779d8d04682a846c0722`；[0.14.68 Starseeker TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.68-Starseeker-TEST) 已发布。
- 本次候选是基线后的 **Speaker for the Past 新增实现**，不是已经交付的 Starseeker。候选程序集仍为 `0.14.68.0`，尚未成为新的 GitHub 交付。
- 候选 DLL SHA256：`841701372EA145469A8DF7F7317462E1FE8615D5CFC7233B16822928EEACD899`（本轮重新读取确认，与 B 记录一致）。
- `src/SpeakerForThePast0046.cs`：`09E3EFEF46909E0640F600905BA24A12E6EBEFE2C0D792E2B00B8EB287DA1507`。
- `src/SpeakerRevelationEffects0046.cs`：`FFBA0CC09BB9575C4330C98D686B6C59A3EFDA3A4ACD1710BDB93029B90334A9`。
- `src/RevelationBridge0046.cs`：`16223C51AA3A41D801EBFE5B5BA4656190878A45B8D8D4632611E0FD0690BA1C`。
- B 最后编译：零警告／零错误；编译 14.952 秒、打包检查 1.585 秒，总计 16.65 秒（引用 B 记录，D 未重跑）。
- B 的隔离宿主结果：exit 0，175 条 Speaker 断言，40.05 秒，候选指纹一致。阅读测试确认主要验证蓝图、资源字段、组件和方法是否存在；不证明下面的支付、销毁和法术来源运行行为。原日志还包含宿主共同前置的其他历史检查输出，不将它们当作本项新增门禁，也不重跑。
- 本地生产路径为 `D:\CodexProjects\CotW-Wrath\outputs\CallOfTheWild.Wrath.Prototype\`。以下行号均指本次未提交的候选源码；不能误认为这些行已经在 GitHub main 中发布。

## 必须修复的问题

### D-0046-01 / P1：Rewind Time 主动版重复扣费、最后一次不可用、重掷后不销毁

定位：`src/SpeakerRevelationEffects0046.cs:170–176,331–338`；`src/RevelationBridge0046.cs:184–197,216–220`。

`Ab(...res...)` 经 `Core` 给主动技能添加一次 `AbilityResourceLogic` 消耗。主动 Buff 的 `SpeakerFailedReroll0046` 随后再次要求资源大于零，并在触发时再扣一次；`OnEventDidTrigger` 是空方法，没有任何移除 Buff 的分支。

因此 L7 仅剩一次资源时，施放主动版后资源变为零，之后即使符合失败检定也会被第 189 行拒绝。剩余两次以上则存在施放扣一次、触发再扣一次的路径，且同一轮还可再次触发。`Rerolls` 防重只检查同一个 Fact，不能阻止主动 Buff 和另一个反应开关对同一事件重复排入重掷。规则类型识别失败时 `Matches` 返回 true，使六类开关的隔离也不可靠。

按冻结合同修复：主动版只在施放时预付一次，触发时允许剩余零资源，首次合格失败重掷后移除；反应版只在实际失败重掷时付一次；同一个 d20 事件最多使用一个 Speaker Rewind 来源，不影响 Oracle 或其他独立来源。规则分类应走实际原生事件类型，不能未知即全匹配。失败判定必须含真实检定加值，不可用裸 d20 与 DC 比较替代。

最小定向闭环：最后一点资源主动施放后仍能重掷且 Buff 销毁；初次成功不触发／不销毁；开关在零资源不触发、失败只扣一次；主动与开关同开不双扣／不双重掷；不匹配类型不触发。保留真实 `RuleRollD20` 来源记录。

### D-0046-02 / P1：奖励法术清理可能误删其他仍有效的来源

定位：`src/RevelationBridge0046.cs:121–162`，特别是第 134 行降环分支和 `Cleanup` 第 149–161 行。

`Cleanup` 的 keep 集合只包含原生 Shaman 法表，完全没有兑现注释里的“another still-active feature”。只要另一个有效额外法表在同一本 Shaman 主书里授予了同一个非原生法术，移除 Speaker 特性仍会调用 `RemoveSpell(spell)`。`Sync` 对高于当前可施放环级的条目也直接执行删除，没有检查来源归属。

合同 2.10 明确要求保留原生法表和角色其他仍激活的额外加表来源。修复必须局限这 18 项、这本主书与当前来源，不重建全局法术框架，也不能以“遇到重叠永不清理”留下 Speaker 独占法术。

最小定向闭环：同书叠加一个仍有效的独立来源，移除 Speaker 后重叠法术仍保留；Speaker 独占法术被移除；原生 Shaman 法术保留；重复清理幂等；降环同步不能删除非 Speaker 所有的条目。无需完整 SaveManager 或玩家存档。

### D-0046-03 / P2：Blood of Heroes 错把加值施加到所有意志豁免

定位：`src/SpeakerRevelationEffects0046.cs:359–365`。

第 364 行的 `AddContextStatBonus(StatType.SaveWill)` 无条件增加意志豁免。第 365 行另加固定的恐惧描述符组件，并不能将上一项限制为“对抗恐惧”。冻结合同要求仅对抗 `SpellDescriptor.Fear` 的意志豁免获得随等级增长的士气加值；攻击和武器伤害的加值保留。

最小定向闭环：L1／L7／L14 的恐惧意志分别 +1／+2／+3；普通意志不增加；非意志的恐惧豁免不误加；攻击／伤害仍按同档增长，同源刷新不叠加。只检查这条实际更改的分支。

### D-0046-04 / P2：Sacred Council 的“Caster Level Checks”被实现成通用施法等级 +2

定位：`src/SpeakerRevelationEffects0046.cs:398–408`；`tests/Speaker0046Tests.cs:451–453`。

该变体现在增加 `StatType.BonusCasterLevel`，影响通用施法等级，而冻结的是施法者等级／穿透**检定** +2。没有 `StatType.SpellPenetration` 这个枚举，并不允许用扩大作用范围的属性代替。现有断言恰好检查了这个错误替代，175 条通过不能替代合同语义。

应绑定当前游戏实际检定规则。项目已有 `PsychicSpiritSpells.cs` 中 `RuleSpellResistanceCheck.AddSpellPenetration` 的局部实现示例可参考，但不要加全局 Harmony 补丁或凭空发明 API。修改该条错误断言需在修复证据中说明“纠正与合同相反的 expected”，同时增加正反例，不能仅删除断言换绿。

最小定向闭环：相应实际穿透检定增加 2；通用 CL、法术伤害骰和持续时间不因该变体增加；关闭／结束 Buff 恢复；其他四个变体不改动。

## 其余范围的审阅边界

- 16 项均有独立 Feature／入口与前置映射，没有继续使用 B v1 全体 +1 Will 的通用占位。
- Aging Touch：已接持电荷／接触交付和真实属性伤害 action；未在 D 中执行战斗路径。
- Phantom Touch：有构装／亡灵排除与 shaken→frightened 分支；未冒充实际目标控制器通过。
- Spirit of the Warrior：有私有 Transformation 克隆、Keen 与逐轮资源。B 还应核实原生克隆残留的强韧／BAB及与 Transformation 的互斥是否符合合同，不能仅以 `PsychicFixedWeaponEnchantment` 组件存在当作结算证明；本报告未将未执行的部分判为 PASS。
- Speed or Slow Time：保留原生 Haste／Slow 交付，私有外层与资源；未要求重新验证完整原生法术库。
- Spirit Shield：护甲分档与 L13 子特性／远程失手接线存在。B 进度表曾把 +8 的等级写作 L13，实际合同／源码分档为 L11，L13 是远程失手的解锁；后续说明需区分。
- Storm of Souls：活物／亡灵分支与强韧减半 action 存在；真实区域控制器未测。
- Spirit Walk：逐轮资源、虚体／隐形／CantAct 接线存在，实际移动与关闭控制器留给 TEST 实机。
- Temporal Celerity：双骰／三骰隐藏子特性和 L7 接口存在；不将接口存在等同真实突袭轮通过。
- Time Flicker：Blur 与 Displacement 共用资源的接线存在；合同写 Standard，而当前 Toggle 默认 Free，修复者应一并对齐明确动作规格，勿扩大到其他 toggle。
- Time Hop：私有两变体与 1／2 点消耗接线；Time Sight：L18 子特性；Erase from Time：接触／强韧分支和已有 PsychicStasisPause；Ancestral Weapon：GhostTouch 与现有附魔卸载机制。以上控制器、计时和装备交互未由 D 实机验证，不标 PASS。
- 18 项奖励法表为 1–9 环各两项，目标主书／原生 LearnSpellList 三引用已接，未改为 Spirit Magic 专用槽；来源清理问题见 D-0046-02。
- `BlueprintRegistration` 两入口保留 Starseeker。`ModuleCatalog.Definitions` 尚无 Speaker，因此基于该固定列表生成的模块状态页不会列出 Speaker；建议 B 按现有惯例补一条诊断展示，不能据此宣称运行成功。
- 真正的 TTT-Base `SandsOfTimeCast` 未在 B 宿主中初始化，正例使用符合合同的模拟提供方，缺项负例已记原因。缺合法提供方时不发布变体是预期行为，**不作为额外全项目门禁**；也不能将模拟提供方正例写成真实 TTT 兼容通过。
- native Oracle 签名与独立资源接线的已有检查可以沿用；没有需求重新跑 Havocker、Starseeker、全历史或其他包。

## 交接与发布状态

生产源码仍由 B 单一负责人修复。先闭环四项具体问题，并核实上面明确指出的 Warrior／Time Flicker 合同差异；D 复查后，只对实际更改执行一次最小必要定向检查，再正常编译、打包和包内 DLL 核对。不追加全量检测或“全部16效果全场景证明”。失败原件保留，未测项继续如实交给 TEST 玩家。

本轮编译：**未运行**；本轮定向检查：**未运行**；本轮新安装包：**未生成**；本轮新版本发布：**未执行**。只上传 D 复查与施工状态，不覆盖、删除或重发 0.14.68 原交付，不关闭 issue #6。
