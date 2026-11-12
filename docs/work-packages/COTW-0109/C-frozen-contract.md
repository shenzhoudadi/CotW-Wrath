# C-0109-20261111-r1 行为合同

A 已核对固定原作和 Wrath 输入。B 可开工。`L` 是有真实 Iroran archetype 的 Paladin 等级。`F` 是真实 Scaled Fist Monk 等级，只有相应原生气／AC 来源符合各节时才参与。所有变体身份取自真实 class/archetype/fact，保留现有 Harmony 2.0.4 兼容。

## Q01 原生职业入口与完整进展

注册在原生 Paladin `bfa11238e7ae3544bbeb4d0b92e897ec`，沿用 LG、圣武士施法、BAB、生命骰、豁免和原有进展。职业技能加 Athletics、Mobility，去重保留已有技能。一级必须为真实 Irori `23a77a5985de08349820429ce1b5a234`。未选神时通过正常选神或单一自有授予来源得到真实 Irori。已有 Irori 原样复用。已有其他神祇时拒绝 Iroran 入职，不替换其神祇或允许双信仰。保留合法原生神祇前置、阵营及同职业变体限制。只在当前 archetype 的原生预览与提交入口收窄适用范围。

按 `A-native-anchors.json` 选择实际原生 LevelEntries。1 移除原职业熟练和 Smite Evil。3 移除 Aura of Courage。4 移除 Channel Positive Energy。4、7、10、13、16、19 移除 Smite Extra Use。5 移除整个原生 Divine Bond selection。11 移除 Aura of Justice。原有 Lay on Hands、Mercy、Divine Grace、Divine Health、其余光环及 20 级 Holy Champion 保留。若基线 Holy Champion 自带削弱关联能力的部分仍保持原生机制，不能借适配发回 Smite 或 Channel。当前一级实际原生表中没有直接的 `PaladinDeitySelection` 可删除，必须在真实选神／升级路径检查上面的单神规则。

- 1 级授 Irori 约束、轻甲及简易／军用武器熟练、Improved Unarmed Strike、徒手成长、Confident Defense、Personal Trial。
- 3 级授 Aura of Excellence。
- 4 级授气池及其能力、Ki Strike Magic、Personal Trial 额外次数。
- 5 级授 Divine Body 私有进展。
- 7 级授 Ki Strike Cold Iron/Silver。8 级授 Personal Trial 额外次数。
- 10 级授 Ki Strike Lawful。11 级授 Aura of Perfection。12 级授 Personal Trial 额外次数。
- 16 级授 Ki Strike Adamantine 及 Personal Trial 额外次数。20 级授 Personal Trial 额外次数。

正常圣武士和其他变体不受本模块修改。使用 `PsychicAssets.Id` 的已有命名空间，根 token 固定 `IroranPaladinArchetype0109`。新增子图使用明确 token 和稳定组件 TypeId，B 交接列出新增身份。重入幂等，原子发布和回滚使用已有注册工具。必要根缺失或 GUID 冲突应可定位并保留原资产。可选图标／场景 FX 缺失时使用可用的原生表现供体。

## Q02 Personal Trial

Swift、一次标准资源支付、一个可合法瞄准的活敌对单位、沿用原生视线和本地 Smite donor 的距离类别。目标阵营无额外门槛。使用本包独立池。每日次数 `1 + floor(L/4)`，到 20 为 6。数值 `1 + floor(L/4)`。选择实际等级表，修正原作描述中相冲突的每三级文字。预览、取消以及被原生施放资格拒绝时无消费。成功施放只扣一次。

同一 caster 至多一个本包标记，同一目标可以保留两个 Iroran caster 的独立标记。优先复用原生 `UniqueBuff`／`UnitPartUniqueBuffs`，配套确认实际 stacking。切换目标、目标死亡、休息、失去变体或来源时，只撤自己的标记。没有临时性的全局目标名单。以 native fact/context 识别来源和序列化恢复。

只在自己标记的目标方向上生效。攻击检定对它加 insight，造成的伤害含原作允许的法术伤害加对应伤害值，抵御该单位攻击时 AC 加 insight，豁免只针对该单位发起的法术或特殊能力加 insight。各事件一次，正确维护临时 modifier 结算。与已有同类 insight 遵循原生叠加。保存检定来源优先使用 `evt.Reason.Caster`。无 caster 的危险和被其他人标记的目标无此加值。无 Smite Evil 阵营、DR、首击倍增或队友共享副作用。

## Q03 徒手成长、熟练与 Confident Defense

用已核对的 Monk 原生拳头和 `EmptyHandWeaponOverride` 持有。纯 Iroran 以 `L` 对齐 1/4/8/12/16/20 级拳头。真实武僧徒手成长存在时按原作的实际跨职业计级方式合计符合条件的 Monk 等级和 `L`，同一个有效等级只选一个拳头档。其他职业、徒手专长和动物爪不增加等级。保留原生身体覆盖、体型和装备规则，形态临时覆盖结束后还原自有正确档次。只撤自己持有的武器／事实，独立徒手来源保持。可复用原生不可变拳头及 IUS 事实，授权不能扩大到整套 Monk 进展。

Confident Defense 在无甲或轻甲、没有盾、且实际未失去 Dex AC 时使用。新增 Dodge 贡献上限为 `min(max(0, ChaMod), L + F)`，只计真实 Scaled Fist 等级，普通武僧、神话和其他等级不提高上限。沿用原作默认的等级上限。无额外按 Iroran 等级计算的无甲成长 AC。Wrath 中同单位已有实际生效的原生 Scaled Fist Charisma AC 时只补足到上述上限，保留该原生 AC 的较高值及原生等级／其他独立贡献。既有不同属性或外部加值按其现有规则结算。上限与同源去重是 C 的 Wrath 兼容裁决。不能用共享 Scaled Fist buff 的修改、全局抑制或职业伪装实现。

通过原生 Dodge modifier 进入普通、接触及失去 Dex 的 AC 计算。装备、武器组、Cha 变化、角色等级变动和事实移除后即同步，原生 CMD 派生按引擎。测轻甲切中甲、加盾（包括 buckler）、失去 Dex，以及无叠加的真实 Scaled Fist 混职。

## Q04 气池、额外攻击和 Ki Strike

4 级激活。使用原生 Charisma 气池 `7d002c1025fbfe2458f1509bf7a89ce1` 每单位同一余额与 retain/release；仅 `L>=4` 且持有本包有效气来源的角色获得额外计级。正常上限 `max(0, floor((L+F)/2) + ChaMod)`，同一 Charisma 只算一次。仅持有真实 Scaled Fist 气来源时计 `F`，普通 Monk 的 Wisdom 池或同名资源不混入。上限补丁如需采用，应限定资源身份、当前角色和本包来源，并保留原生附加资源修正。相同原生池已经持有时附加来源不能另开余额或无条件回满。掉来源只 release 自己的占用，剩余 Scaled Fist 来源仍可用并限于其现有上限。休息按原生回满。

保留原作继承的 Scaled Fist Extra Attack 及其原生条件。另加一项 Ki Power: Ignore DR，沿用 donor 动作类型和 1 点花费，持续一轮。只对施法者自己的 Personal Trial 目标绕过 DR，保留其他伤害抗性／减免及其他单位的攻击。两能力走同一原生池并按真实 Spend 结算，不能借移植为其他职业授气或改变其蓝图。

4/7/10/16 Ki Strike 按原生四个事实的徒手类别规则提供魔法、寒铁＋银、守序、精金属性。新来源仅在实际变体等级拥有。测试使用真实 `RulePrepareDamage`／`RuleDealDamage`，包括普通持武器、独立 Monk／Scaled Fist 和他人的 Trial 目标。

## Q05 Divine Body

5 级取代整个联结选择，保留原生武器联结选项集及花费预算、加值上限、持续和成长。5/8/11/14/17/20 级预算 1/2/3/4/5/6，9/13/17 级各加每日一次，持续 `L` 分钟。使用本包资源身份，禁止与普通 Paladin/其他职业花费交叉。复用原生联结动作、属性及不可变附魔供体。只接线本包进展，先核对 native group 与 per-unit enchant pool 的实际归属，再选择复用的边。持有同类独立来源时只能撤本包附魔，不能清其他来源的池或物品事实。普通角色原生选择表保持。

施放资格与应用使用同一个真实主手解析 `PrimaryHand.MaybeWeapon ?? Body.EmptyHandWeapon`，要求 `UnarmedStrike`。真空手可用。普通武器、自然武器及无有效拳头不可用。只增强解析到的本人拳头，预览／失败不扣费，使用成功一次扣费。重新施放与持续结束按原生联结生命周期，换装、拳头变更、撤来源时清退本包增强，不把其附到换上的剑、不删独立永久附魔。B 若直接使用原生 `ContextActionWeaponEnchantPool`，需验证其 `ClearEnchantPool` 对现有真实合法来源无误删，并保存实际附魔句柄的隔离证据。

## Q06 两道光环和原生重掷

保持原作 13 ft 蓝图半径，英文说明与实际范围一致。只在 emitter 清醒、存活、活动且有效时生效，目标为本人及有效盟友。多施法者同种光环只触发一次。敌人和范围外仍按原生，来源失效与离开范围撤销。Aura of Excellence 的本人免疫优先于盟友增强。两者皆为原作自动效果，无新增切换界面。

- 本人有 Excellence 时，过滤实际取低来源，保留正向来源，即使原生有符号和原本被负向抵消。只影响当前掷骰事件，记录真实获准来源。没有正向来源时只掷基础一骰。保留原生确定结果、替换骰值、预掷及伪骰保护。
- 盟友受 Excellence，已有取低分支时多掷一骰，取有效样本第二小，重复值按样本计入。基础＋一负向 `[19,2]` 加 `13` 得 `13`。多负向同理取全部样本的第二小，按原作代码。无取低时不白送骰。
- 本人或盟友受 Perfection，已有取高分支时多掷一骰并取最大。基础＋一正向 `[2,19]` 加 `13` 得 `19`。零重掷保持一骰。盟友有正负混合来源时保持原生有符号合并，以实际执行的取高／取低分支判定光环，净零无增强。本人免疫先去负向，再判断有效的正向分支。
- 支持原生 `AddReroll -> RuleRollDice.Roll` 聚合入口，兼容 Relentless/Pageantry 的 `OnTrigger` 附加来源。支持已核对的 `Reroll(EntityFact, bool)` 晚到实际掷骰。晚到针对这一批 `[调用前有效结果, 本次新骰, 光环额外骰]` 判定，不重新处理之前的历史。以实际执行的 Min/Max 判定盟友增强。本人应拒绝负向晚到入口自身的额外抽骰，不能只屏蔽 AddReroll 后仍让私有 Reroll 执行。两个入口按一次真实批次处理，防止重复增强。
- 仅处理真实 `RuleRollD20`。保持 `PreRollDice` 已取得的基础骰、`ResultOverride` 以及原生 `ReplaceOneWithMax` 的有效值语义。真实掷骰顺序进入 `RollHistory`，保留来源和实际骰子消耗。允许在受控宿主固定 RNG 边界。不能伪造结果、重写所有骰类、用单次 `AddReroll(true)` 代替次低，或推断所有 `Override` 都是强制重掷。

## Q07 验收与公开交付

以 `C-cases.json` 的实际路径和预期值为准。先证明重掷、标记来源、气池、空手联结与 AC 的最小正反例，再完成整包级表。无需每个等级重复运行同一机制。单一 B 负责人整合生产注册及目录，D 独立核对变更和原始证据。修改生产行为才跑相应的最小定向检查。保留失败记录及实际耗时，区分托管路径、结构、实际场景证据。常规编译和打包按项目现行规则执行，禁止全历史、全量、游戏和 UMM 验收。存档／升级 UI 等完整场景未运行时如实记录。
