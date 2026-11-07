# K01 — S-KINETIC / F-0020 Havocker 冻结契约

- 契约编号：K01-HAVOCKER-20261106-r1。
- 状态：**FROZEN（机制及 expected 已冻结；实现、编译、行为验证均未运行）**。
- 决策责任：gpt-6.1-sol；日期：2026-11-06。
- 基线：CotW-Wrath f532b11d12d8b75862ca83e390d0ec3fff120f25 / v0.14.66。
- 原 CotW：Holic75/KingmakerRebalance@1332fb0db844b7863f484ca978bea2349fe49769。
- 当前 blocker：**无**。前两轮 BLOCKED_INPUT / B-K01-LOCAL-EXEC-START 已由本轮真实读取解除，旧占位不再有效。
- 本轮只写冻结文档及其副本，不改生产源码、不执行测试、不 push、不安装 UMM、不启动游戏。冻结不等于功能 PASS，也不表示 Havocker 已交付。

## 0. 用户硬性规则与输入收据

Flash 01—03 结束后直接由 Sol 冻结，不再询问用户是否开启 Sol。以下是 Sol 对原文、原实现和 Flash 问题的真实裁决，Flash 无权改判。

**出包前检查 / V01 / Z01 / 预发布门禁：禁止全量检测，禁止整套全量测试。充分条件仅 A：新增内容成功编译并通过可运行性检查；B：D:\CodexProjects\wotr-testing 针对新增内容的定向检测跑绿。不得追加全历史、全库或全游戏验收门槛。** 详见 K01-release-gate.md。

已实际读取工作区 _havocker_flash_inputs 中的 Q-01—Q-12、SC-01—SC-29、交接、01 来源索引和 02 映射/调用链。三份 03 输入与 agent-tools 原件逐字节一致：
- 03-open-questions.md SHA256：25E3A6B890B0804B7E8D03C42CE237AE93ADE4140DD99CA6A1E4797FFE9218DF。
- 03-case-candidates.tsv SHA256：C2B53EB6018D453930E6FB609584439A06C43602AE25094ACB19110542B1CFBC。
- 03-handoff.md SHA256：059AA8E5D4B7CA69C26BDC7C7C873E20281643496D86FE0EB906BBF915ACFDBD。

必要源码证据：
1. 原实现：work\port01441\sources\Holic75__KingmakerRebalance\tree\CallOfTheWild\Classes\Witch.cs:606-824、199-228，以及 NewMechanics\KineticistMechanics.cs:141-177。Witch.cs 实测 SHA256 为 7C15E1FB47D15EEA12FCA246C56F4CB04B8E46CDE4FF63262039CDD920B9F2EF。
2. 实际当前生产工程：outputs\CallOfTheWild.Wrath.Prototype；已核对其中 PsychicAssets.cs、Invoker01437.cs、HedgeWitch01443.cs、KineticInvocation01433.cs、KineticUtility01433.cs、PurifyingFlames01428.cs 及相关测试源码。根目录没有 src/tests 不表示源文件缺失。
3. 原生源码导出：work\port01428\howl-audit\AbilityKineticist.cs、AddKineticistPart.cs；work\port01433\kinetic-utility\UnitPartKineticist.cs。仅阅读，没有加载/执行游戏程序集。
4. work\prototype-build\psychic-expansion-native.json 中的相关蓝图及组件，仅作历史身份/结构证据；不当作本轮运行结果。
5. 已读 wotr-testing 的 AGENTS.md 指向的 CLAUDE.md、README 和 docs\offline.md；本轮不修改该工具，也不运行其整套自测。

## 1. 必须纠正的 Flash 输入

- Q-04 混淆了两种事件。ClearAcceptedBurn 在 0→0 时不派发燃素数值变化，不能推出零费用 Spend 不派发接受事件。已读 AbilityKineticist.Spend：调用 caster.AcceptBurn 后，无条件 Raise IKineticistAcceptBurnHandler，接受值为 0 也派发。不能以 Flash 的相反推断设计补丁。
- Q-07 的“物理念袭加一半体质、能量念袭无属性”不采纳。原生 Earth 的主属性 DamageBonus 为 AsIs，Cold 为 Div2；保留原数学式，只改 Havocker 来源的主属性。
- SC-11 六级 Witch 正常具有四环准备槽不成立，冻结输入改为七级；保留原输入并标注纠正。
- bb24cc01319528849b09a3ae8eec0b31 是同名元素法术专长池（Feat/WizardFeat），不是 Kineticist 元素池。模板固定为 1f3a15a3ae8a5524ab8b97f469bf4e3d。
- 阅读测试源码不等于本轮已执行原生路径。本轮所有场景执行状态均为“未运行”。

## 2. Q-01—Q-12 逐项冻结裁决

### Q-01：采用说明的四阶表，修正代码错位
采用 10/12/16/20 级各增加一阶专精，累计减免 1/2/3/4。14、18 级不额外增加专精；12、16、20 级同时授予原有注能选择，不二选一。
理由：说明给出完整递增和上限，按明确规则承诺修正原 AddFeatures 错位；英文说明与进展必须一致。
影响：原 InfusionSpecializationHavockerFeature 670798393e7e43a29c03a768b2125046 的 Wrath 私有对应及 Havocker 进展；原生专精 5f64f446aef387d499a396b626d5fc51 不改。等级 10/12/14/16/18/20 的固定结果为 1/2/2/3/3/4。

### Q-02：补齐九环，九环额度为整数 4
Witch 6 级取得 Spellburn。转换数组长度 10，索引 0/1=null，2—9 都有入口；实际已准备槽环级 L 给 floor(L/2)：2/3→1，4/5→2，6/7→3，8/9→4，绝无 4.5。必须正常取得并准备该环，不能提前得到法术位。
理由：九环无文字排除，原循环上界遗漏应补齐，燃素为整数。
影响：SpellBurn2..9Ability0020、SpellBurn2..9Buff0020、SpellburnHavockerFeature0020；原九环 GUID 不存在，不编造旧 GUID。

### Q-03：严格限定为注能燃素，不采用原作越界分支
Spellburn 只减少自有念袭的 Infusion 费用，不减少 Blast、WildTalent、Metakinesis 费用，不向 GatherPower 注入额度。原作 WildTalent 固定减 1 和 IncreaseGatherPower(value) 均不沿用。聚能按自己的原生适用范围正常处理，不因 Spellburn 扩权。
理由：准备法术用于念袭施加注能是明确边界；原作分支让非注能费用受益且收益不对称。
影响：原 DecreaseWildTalentCostWithActionOnBurn 的私有替代、Spellburn Buff、私有念袭/注能；其他职业与普通狂野天赋不变。只有实际得到正数注能折扣的执行才消费令牌。

### Q-04：消费绑定真实资源提交，零费用同样清退
主钩子固定为原生 AbilityKineticist.Spend → IKineticistAcceptBurnHandler.HandleKineticistAcceptBurn；burn 参数为 0 也处理。只匹配本 owner、Havocker 私有念袭来源、当前支付收据和实际使用的 charge。
不以 ClearAcceptedBurn、燃素数值变化、命中、伤害或 Buff 超时作为支付证据。费用预览、CanUse、CalculateBurnCost 均无消费副作用。
提交后先把 charge 标为已消费并撤销后续抵扣资格，再移除展示 Buff；移除异常保留已消费状态并重试清退，不能让残留 Buff 再折扣。刀刃/子投射物路径必须统一到同一资源提交协议，不能每个攻击子事件再扣一次。
理由：实际源码证明 Spend 即使 0 也派发接受事件；Flash 的“零费用必不派发”不成立。K02 必须通过真实原生 Spend 的定向用例证明接线，不直接调用自定义监听函数冒充原生。
影响：所有 SpellBurn Buff、私有念袭/刀刃激活和执行收据；不改变原生 AcceptBurn/ClearAcceptedBurn 行为。

### Q-05：专精→Spellburn→原生聚能，溢出不结转
按第 4 节费用式每项计算一次。一次真正提交后，原生聚能按原生 Spend 归零，已使用的 Spellburn charge 整体消费；双方剩余额度不带入下一动作。预览/不可用判定不得清空聚能。
Havocker 的两项燃素上限以及正常 AcceptedBurn/AcceptedBurnThisRound 均为 0；最终费用>0 必须在实际支付前拒绝，不能靠 AcceptBurn 的截断把非法费用“夹成0”免费施放。
影响：私有聚能组、两项零资源、念袭/注能费用。Spellburn 本身不增加原生聚能存量。

### Q-06：继承互斥，在职业入口双向阻断
Havocker 与原生 KineticistClass 42a455d9ec1ad924d889272429eb8391 的任意变体不得共存。不实现双主属性/双燃素仲裁。已有 Kineticist 等级≥1 不可选 Havocker；已有 Havocker 不可选择 Kineticist 职业。
Havocker 根加对应职业先决，Kineticist 选择入口增加精确 Havocker 排除；升级提交前二次校验。不能等到元素子选择才报错。拒绝不得增加等级、消费资源或覆盖单例。
普通 Witch+Kineticist 的既有合法混职不变；Havocker+Wizard/Cleric/Sorcerer 等不冲突职业可存在，但其他职业等级不提高念袭。
被外部强制注入的非法双职业状态不支持：禁用本包 Havocker 支付/施放并记录诊断，不删除外国事实、不覆盖外国 UnitPart、不破坏存档。
理由：原作确有互斥意图；原生单例不支持并存设置，入口提前阻断比子选择死锁明确。
影响：Havocker 根、KineticistClass 选择入口、自有 AddKineticistPart 与来源收据；不全局伪造职业等级/HasFact。

### Q-07：原生主属性＋私有图，保留数学，废除全库修改
自有 AddKineticistPart 设置 Class=Witch、MainStat=Intelligence，相关参数用 UseKineticistMainStat。有效等级仅 Witch 等级，念袭成长 N=ceil(WitchLevel/2)，其他职业不相加。
基础 Earth 物理念袭为 N d6 + N + IntMod；基础 Cold 能量念袭为 N d6 + nativeDiv2(IntMod)。保留各自原生倍率、骰数、舍入与下限。DC/专注/原先基于体质的持续时间用同级原生该能力公式，把 Con 主属性槽换为 Int；基础念袭没有豁免的不新造豁免。命中属性、BAB、普通/接触AC不统改智力。
只克隆四元素、所需念袭/注能/聚能的有界可变引用子图；纯只读效果可共享。私有池中的职业/元素先决按 Witch+Havocker 来源改写，其他先决保留。
禁止全库遍历替换组件、共享进展就地改写、FactCollection.HasFact 全局补丁，以及全局 ClassLevelsForPrerequisites 让单位冒充 Kineticist 的资格。
影响：私有元素/成长/能力/武器/投射物引用。原生 KineticistMainStatProperty f897845bbbc008d4f9c1c4a03e22357a 只读使用，其他职业不变。

### Q-08：仅当前角色真实准备女巫槽，可并存列表
同时要求：Havocker、Witch≥6、当前角色 Witch 职业绑定的非自发准备书、ConvertedFrom 链最终对应该书真实可用准备槽、L=2—9 且匹配转换环级、没有待用 charge。ConvertedFrom 链须防环路；不能只比 GUID 接受伪造/过期/外来对象。
当前支持 WitchSpellbook dd04f9239f655ea438976742728e4909。Wizard/Cleric/术士槽、卷轴/物品、非准备槽、已耗尽槽均拒绝，不回退其他书或任意同环槽。超魔准备法术按实际占用槽环级 L，不按原法术基础环级。
使用 SpontaneousSpellConversion，只添加/撤销自己列表来源，不覆盖同书其他转换。Ley Line Guardian 等自发 Witch 组合在兼容选择与实际执行处拒绝，不能消费自发槽。
Hedge Witch 与 Havocker 正常因重叠替换项不可叠；“多转换源共存”用独立来源夹具验证，不宣称该变体组合合法。
影响：Spellburn 入口、WitchSpellbook、自有转换收据；现有 Cure 等列表不改。

### Q-09：已有待用令牌则拒绝新激活，事件幂等
同一 owner 最多一个 Armed/Reserved charge；同环重按、换高环都在 UI 和执行前拒绝，不扣新槽、不覆盖额度、不重设期限。撤去原作“扣新槽再移除旧 Buff”的覆盖语义。
一次逻辑转换只扣一个准备槽并授予一个 charge；一次实际念袭提交只消费一次。标识必须绑定 owner、实际执行上下文/原生命令实例及递增支付序号，不能只按 AbilityData、环级或回合号去重。重复旧回调不得消费后来新建的 charge；后续合法执行仍可复用同一个 AbilityData。
影响：SpellBurn2—9、激活限制器与执行收据。

### Q-10：提交前保留至原到期，提交后消费且不因命中退款
Spellburn 激活本身提交前取消/校验失败：不扣槽、不建 charge。激活成功后槽已牺牲，本能力不退款；其他合法原生回收法术机制不由本合同禁用。
激活后、念袭支付前：费用预览、选目标、转向、射程不足、排队取消、提交前打断、最终费用>0，不消费 charge，保留其原始截止时间，不刷新也不退槽。
合法自有念袭真实提交且 D>0 时立即消费，不依赖命中；之后打断、未命中、免疫、伤害0、效果失败均不恢复 charge/槽。无注能或专精已将注能费用归零（D=0）的动作不消费 charge。
影响：念袭提交边界和 Spellburn 生命周期；SC-21 明确拆提交前/后两分支，不用“失败”混淆时刻。

### Q-11：保留原替换范围，专长按真实能力来源，不全局禁 Havocker
1级替换魔宠选择 29a333b7ccad3214ea3a51943fa0d8e9、普通庇护主选择 381cf4c890815d049a4420c6f31d063f，以及 1/2/4/6/8/10/12/14/16/18/20 共11个 WitchHexSelection 9846043cf51251a4897728ed6e24e76f 来源。保留正常准备 Witch 施法/学习及所有未声明替换能力，不能因移除魔宠选择破坏法术书。
不复制原作8处全局 NoHavocker 先决，也不伪造 HasFact。纯 Havocker 等级不构成巫术/庇护主资格；混职有独立合法巫术来源时仍按专长原有职业、等级、具体巫术要求判定，满足才允许，不一刀切禁止。Witch Knife/Patron CL FCB 分别要求其实际能力来源，其他职业有巫术不自动构成庇护主资格。
优先原生真实先决；若某项仅看 Witch 等级造成纯 Havocker 漏洞，只允许在资格查询入口对“有Havocker＋精确核验的对应专长＋缺实际所需来源”作局部过滤，不改共享先决数组，不扫描全库。范围仅实际存在的 Split Hex、Split Major Hex、Amplified Hex、Accursed Hex、Hex Strike、Extra Hex、Witch Knife、Patron CL FCB 对应项；未装的可选项不创建、不增加依赖。当前专长GUID由 Sol 根据实际提供方类型/来源绑定，Flash 不可按同名扩展。
影响：Havocker RemoveFeatures 和这8项的有限资格查询；不删除其他职业来源的魔宠/巫术/庇护主。

### Q-12：来源独立清退，保存恢复不刷新额度或期限
休息完成按原生恢复准备槽，同时清除自有 Spellburn/预留/待清退 Buff/聚能，燃素仍为0；不清外国状态。普通升级重算新等级资格/专精/成长，不恢复已花槽或重发 charge。
真正移除 Havocker/洗点时撤销自有转换列表、资源、图授予来源、令牌和聚能；仅当 UnitPart 当前 settings/owner 收据属于本包才移除。禁止按类型盲删外国后来接管的 part。改为普通 Witch 后按新进展恢复魔宠/庇护主/巫术；之后合法 Kineticist 由其原生创建自己的主属性和燃素。
临时 Deactivate/Reapply/保存/载入不等于洗点。保存 chargeId、来源、额度、绝对到期时间、提交/消费状态；只恢复未过期且未消费的 Armed，不重复扣槽、不刷新一轮期限、不复活 Consumed。提交中或归属无效状态保守清退，不重新给折扣。Activate/PostLoad 重复调用幂等。
影响：私有进展/转换来源账本/资源、UnitPart、聚能、Spellburn 状态；非法双职业仍按 Q-06 处理。

## 3. F-0020 完整授予与边界

- 1级：Patron Element 四家族（风/土/火/水）、合法基础念袭、按 Witch 等级念袭成长、基础聚能、自有智力/零燃素设置、念袭武器熟练。
- 4/8/12/16/20级：各一次注能选择，保持元素/等级/其他原生先决。
- 6级：Spellburn；2—9入口是否可用取决于真实准备槽。
- 10/12/16/20级：各加一阶专精；14/18不填旧专精空缺。说明文本用英文并与冻结等级表一致。
- 念袭每两级增阶；聚能仅基础能力，不凭空授予高阶聚能增幅、额外元素、utility天赋、Elemental Overflow 或 Metakinesis。
- 注能“额外主动承受燃素换增强”的可选加码禁止；不能借抵扣让该禁止项合法。
- 复合念袭仅在其原生资格确实满足时可用；SC-16/17 是费用边界夹具，不表示正常 Havocker 自动获得复合念袭或 utility talent。
- 不迁移其他变体、不恢复排除项、不改 Amnesiac、不增加通用法术或强制外部模组。

## 4. 费用式、生命周期与来源合同

令 B=本体、I=正常注能合计、M=合法其他念袭附加、W=独立WildTalent费用，均为非负整数；其他合法来源按原生类型规则纳入，不能按总量猜类别。
1. S 按 Q-01；I1=max(0,I-S)。
2. 有效待用令牌 T=floor(L/2)，无令牌T=0；D=min(I1,T)；I2=I1-D。
3. Spellburn 不修改 B/M/W 或原生聚能存量。
4. 对原生允许聚能的念袭费用使用实际 G；普通本包念袭 Final=max(0,B+M+I2-G)+W。纯 utility 不受本包扩权，仍按其原生费用。
5. Final>0 拒绝；Final=0才可提交。提交若D>0整枚charge消费，T-D作废；D=0令牌保留至原始期限。原生聚能在其资源提交处正常消费，不能清掉令牌后再算一次不同费用。

固定例：
- Witch6，B0/I1/M0/W0，S0/T1/G0：Final0；提交消费charge。
- B2/I0，T2/G0：Final2，不能靠Spellburn支付复合本体。
- 纯W=2、T3：Final2，不能按原作减1或减3。
- Witch10，B2/I3，S1/T1/G3：I1=2,D=1,I2=1,Final0；提交消费charge及聚能，无余额结转。

状态：Absent → Armed → Reserved（仅真实执行）→ Consumed → Removed。预览不预留；成功激活只扣一枚源槽并建一个charge。先撤销Consumed的抵扣资格再清展示Buff，清退失败不能重新获益。
Armed 截止时间为激活实际提交时刻＋原生一轮（6个游戏时间秒）。回合制同用原生duration/游戏时钟，不简化为任意下一角色回合；暂停不推进游戏时间，战斗外/即时制相同。保存/重应用不重设截止时间。

S-KINETIC 由 Sol 维护一份；F-0020不得另写第二套费用/来源机制。注册、项目文件、共享父选择池由同一 Sol 整合负责人改；暂存私有图后原子发布。重复注册不重复追加，身份碰撞/形状不符不覆盖外国资源、不留下半注册。
已有 KineticUtility/KineticInvocation 不因 Havocker 存在而自动开放、重构或修改；其正常来源行为保持。

## 5. GUID/token 与克隆范围

旧 Kingmaker GUID 只作来源索引，不直接注册为 Wrath 新资源。用当前 PsychicAssets.Id：MD5(UTF8("CotW.Wrath.Psychic.v1/"+token))，32位小写十六进制。
根 token 固定 HavockerWitchArchetype0020；其余命名资源为“原资源名＋0020”，包括 SpellBurn9Ability/Buff。有界可变克隆 token 为 Havocker0020.Clone/<原Wrath资源GUID小写>；同一原资源只克隆一次，以映射保持共享/回环，不按遍历顺序生成ID。具体计算表见末尾附录。
需要新的可序列化组件时由 Sol 按现有 TypeId 合同分配并记录，不复用外国 TypeId，不授权 Flash 编写组件；这是实现细节，不是未决行为。

固定原生身份：
- WitchClass 1b9873f1e7bfe5449bc84d03e9c8e3cc；WitchProgression c5e9e2b086a9814409fb6a29a5acdd0f；WitchSpellbook dd04f9239f655ea438976742728e4909。
- KineticistClass 42a455d9ec1ad924d889272429eb8391；元素模板 1f3a15a3ae8a5524ab8b97f469bf4e3d；排除同名法术专长池 bb24cc01319528849b09a3ae8eec0b31。
- 四元素根 Air 2bd0d44953a536f489082534c48f8e31；Earth c6816ad80a3df9c4ea7d3b012b06bacd；Fire 3d8d3d6678b901444a07984294a1bc24；Water 7ab8947ce2e19c44a9edcf5fd1466686。
- 念袭成长模板 30a5b8cf728bd4a4d8d90fc4953e322e；注能池 58d6f8e9eea63f6418b107ce64f315ea。
- 聚能复制根 GatherPowerAbilitiesFeature 71f526b1d4b50b94582b0b9cbe12b0e0；0601925a028b788469365d5f8f39e14a 是其中的阶数特性，不是完整聚能能力组。可变能力/状态由私有来源绑定。

## 6. 用例、运行状态及门禁边界

K01-frozen-cases.tsv 保留 SC-01—SC-29 编号、原输入和证据；“冻结输入修正”和 expected 单列。新场景标 Sol补充，不改 Flash 原件。表内行为可由少量参数化定向测试覆盖，不要求一行一个进程，不扩张为其他功能套件。
真正的 B 必须走原生转换/费用/Spend入口，不能直接呼叫自定义回调或把原生规则替换为expected。Unity原生不支持的路径如实记录，不能伪称玩家游戏通过。

本轮：编译未运行、可运行性检查未运行、wotr-testing定向检测未运行、打包未运行、上传未运行。仅核对文档、场景编号、来源指纹与副本一致性。

## 7. Flash 04 明确放行

**Flash 04 可运行：yes。** 仅放行非代码机械接线草稿；本轮不调用 agy/Flash。

条件：
1. 读取本契约 K01-HAVOCKER-20261106-r1、同目录冻结用例和发布门禁，基线相符。原 prompt 中 havocker-sol\S-KINETIC-F0020-FROZEN.md 路径由本轮明确交付的 havocker-flash\K01-frozen-contract.md 取代，不再等待另一批准文件。
2. 三份03输入匹配第0节指纹。输入若变化，交 Sol 对齐，不让 Flash 改机制。
3. 仅写 <agent-tools/havocker-flash>\04-wiring-draft.tsv 与 04-contract-trace.md，列合同条款、冻结token/GUID、来源、字段/等级及拟接线位置。
4. 不写C#、可执行伪实现、可应用diff、测试代码；不改生产src/项目/注册/构建/门禁，不改白名单/expected，不运行宿主/测试/游戏/UMM，不整合、不发布、不宣称PASS。
5. 无法机械对应的实现点写“交Sol”，不得缩范围、近似代替或复制原作全库修改。合同已冻结，不再问用户是否开Sol。

## 8. K02+ 留给 Sol

依次实现来源隔离、双向选择入口、私有图、准备槽支付与零费用提交收据，再接完整 Havocker；Sol 设计实际断言并在 wotr-testing 执行本功能定向检测。独立只读复查限本次差异及证据，不扩大为新增全量门禁。技术发布充分条件始终仅 A+B。

<!-- K01-GENERATED-APPENDIX -->

## 附录A：确定生成的新资源身份

以下旧GUID仅为Kingmaker来源索引，新GUID由冻结token按现有PsychicAssets.Id计算；本轮未注册任何资源。

- HavockerArchetype：原GUID 6b818f4ae81d41d590ca5106324e6f79；token HavockerWitchArchetype0020；新GUID c5c71fdb65d94504b2214b1ee5e5d032。
- PatronElementFeatureSelection：原GUID 814378c6228a4067b58cac9e579efb8e；token PatronElementFeatureSelection0020；新GUID 45c746aef0ef78c14914a054cbc68aa2。
- HavockerElementalFocusAir：原GUID 72a4135184074b92b343acd6316d7f9c；token HavockerElementalFocusAir0020；新GUID ae3e2487cc167ee640b7c14111b2a0d8。
- HavockerElementalFocusEarth：原GUID b0f927cf6672492ba7806a16d2b26774；token HavockerElementalFocusEarth0020；新GUID dfa0e660c5f3c693f780a3922b40b2b4。
- HavockerElementalFocusFire：原GUID 9367b80bf17a4ff6bac3faa32c65a18c；token HavockerElementalFocusFire0020；新GUID 649ae63d093b90e91e794044f1ed7cd4。
- HavockerElementalFocusWater：原GUID 9f58cc4afe074882b5eca2c2279119d0；token HavockerElementalFocusWater0020；新GUID fae3435c36cc5eba5240ddff9f7b5cfd。
- HavockerBurnResource：原GUID bcde4d59ff44400cb05238190a370306；token HavockerBurnResource0020；新GUID b0183e62d74b2b405edbb7c9fef01a09。
- HavockerBurnPerRoundResource：原GUID b58156fb1b8c4b3cafb1b8c5c3fd06f6；token HavockerBurnPerRoundResource0020；新GUID 7c0f92145f6ec27b8f95fc0409b40965。
- HavockerKineticBlastProgression：原GUID c32f99fcb740447f948b70de216c751b；token HavockerKineticBlastProgression0020；新GUID b31be531deacddeb28b070744fc74250。
- HavockerGatherPowerFeature：原GUID 67909e9e85c346f9842e2f771cffdca8；token HavockerGatherPowerFeature0020；新GUID c2055897ed5df1a6a7cdf45af1d07934。
- HavockerInfusionSelectionFeature：原GUID e2b221d848c44a3691f24085151e01f8；token HavockerInfusionSelectionFeature0020；新GUID dd6a4b9e6484a10ed60648126fff2825。
- InfusionSpecializationHavockerFeature：原GUID 670798393e7e43a29c03a768b2125046；token InfusionSpecializationHavockerFeature0020；新GUID 1d713e7a46789c37f9ef3f6daee32111。
- SpellburnHavockerFeature：原GUID 8fdae44f04294581bec3e941c76097c5；token SpellburnHavockerFeature0020；新GUID b269a4bd4b4ed424d7d50459b13e8fcd。
- SpellBurn2Buff：原GUID e0f556ed91324b7d8758711136f627ff；token SpellBurn2Buff0020；新GUID 4595cd773821ca7813e12820280c9e3e。
- SpellBurn2Ability：原GUID 6567125dbb1541438e9a0253aa455cd6；token SpellBurn2Ability0020；新GUID e401b2761a32a35224b73088db8da5b2。
- SpellBurn3Buff：原GUID 69e70939d65b48e0862330b4a5c7c68f；token SpellBurn3Buff0020；新GUID 4ada8a82b705769a536a5cf283d32bd8。
- SpellBurn3Ability：原GUID 0fead3f5db544b73abb301e46942e392；token SpellBurn3Ability0020；新GUID 5a77da1936fef096fafab86ff24f65e2。
- SpellBurn4Buff：原GUID 26765e5d9b14444fb87bd79f8b9281d3；token SpellBurn4Buff0020；新GUID 2161a4297cc39b9284568db494357d5e。
- SpellBurn4Ability：原GUID e83d9518ec814b35a4891b0f8ea1bb85；token SpellBurn4Ability0020；新GUID c054087f586eda15b7bfdbdbfd980811。
- SpellBurn5Buff：原GUID da5fba3d323f494084de6e8cf2fcf3df；token SpellBurn5Buff0020；新GUID 239b51dcba44838e0c0256263b98e49b。
- SpellBurn5Ability：原GUID 7cf8f38219e649c8879ab1aa1c5bdd3d；token SpellBurn5Ability0020；新GUID b7b5396a8d8876b6aa657cde27288cb1。
- SpellBurn6Buff：原GUID eb1bb8595100437a831e777a6c5961cb；token SpellBurn6Buff0020；新GUID 3572532065c157e37d93043ea5c27703。
- SpellBurn6Ability：原GUID ef99b59ab6284e25a48bb16d0d18177e；token SpellBurn6Ability0020；新GUID 368d3d4f7f1adb5cfa2db48cc1dccd8a。
- SpellBurn7Buff：原GUID e6de2c6d0d2e4bfdb3e8e9ea93a282bc；token SpellBurn7Buff0020；新GUID 05d7c17e920baed1e39fd05913d522b0。
- SpellBurn7Ability：原GUID ad32835b4be8499a8b997cb109b38d1d；token SpellBurn7Ability0020；新GUID 5fa79e79f97422aa2416b45afecea1b2。
- SpellBurn8Buff：原GUID 97e4d933932b4b758e4e1311b894f375；token SpellBurn8Buff0020；新GUID b7bd000e923ef6f4ec1bca2544e3c973。
- SpellBurn8Ability：原GUID 8334424e2d0d4c13bac248c570d08ae0；token SpellBurn8Ability0020；新GUID b40d28b9605b60b118c72b3d6ab2060f。
- SpellBurn9Buff：原GUID MISSING；token SpellBurn9Buff0020；新GUID 052a985fd27ef6cf2e9b2348e703b532。
- SpellBurn9Ability：原GUID MISSING；token SpellBurn9Ability0020；新GUID ba84c5768f30ce02151117fd1837c7d0。

## 附录B：本轮只读核对的当前生产源码指纹

- src/PsychicAssets.cs：SHA256 8F5BB7C698EB9C845E9EE9386D38DDBA3240719209338C38A13E0C752AB1295E（前后相同）。
- src/Invoker01437.cs：SHA256 B2F1D7588BEFD7DF877A0F93C80BA0629C8124333CC4B6D5520FDCF0084C3226（前后相同）。
- src/HedgeWitch01443.cs：SHA256 1E53E6519298735C228222376F39403C85EFF7661DD11FB0892499A3A26EF695（前后相同）。
- src/KineticInvocation01433.cs：SHA256 7A28E30509B22409426519D813302A7D9E2DE7D9E0DB9A5733BA5A5E042F1ADF（前后相同）。
- src/KineticUtility01433.cs：SHA256 8FE9456FBF3AABFC56F238031CB19B662FCC14836C50BE227D94BF36F0ED3B66（前后相同）。
- src/PurifyingFlames01428.cs：SHA256 305E08C5C1731C4C11DCF80C640DAEFB631E5AE0AACD2994D82CD1E0A6C7C1B4（前后相同）。

说明：以上为本次实际读取的六份生产源码，不声称扫描了整个项目或执行了功能验证。构建引用DLL指纹本轮未重新采集；实际A/B运行时记录其输入，不能补造历史数据。
