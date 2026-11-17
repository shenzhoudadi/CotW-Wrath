# COTW-0099 Occultist · A 盘点

生产基线为 0.14.74 TEST（939e04d），认领说明基线为 ca35d78。上游为 Holic75/KingmakerRebalance 1332fb0，20 个本地审读文件的 Git blob 与上游树一致。能力表以实际建图调用为准。相应原作方法、引用 GUID、Wrath 静态元数据路径和每条法术的本职业环位见 [A-occultist-assets.json](A-occultist-assets.json)。[上游职业源码](https://github.com/Holic75/KingmakerRebalance/blob/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/Classes/Occultist.cs) · [上游许可](https://github.com/Holic75/KingmakerRebalance/blob/1332fb0db844b7863f484ca978bea2349fe49769/LICENSE)

## 职业等级入口

- 1 级。d8、3/4 BAB、强 Fort/Will、弱 Reflex。简单与军用武器、轻中甲、盾牌（塔盾除外）。原作以 Alchemist 为 BAB、法术位和装备外观供体，以 Fighter 为初始装备配置来源。技能对应 Stealth、Trickery、Knowledge (Arcana/World)、Lore (Religion)、Perception、Persuasion、Use Magic Device。
- 1 级。独立 Intelligence 自发念力法书，施法者等级为职业等级，1–6 环；固定六种无限次 Knacks。Detect Magic、射线和接触计算 fact。两次法器选择、Mental Focus、一次额外 Focus Power；首选各学校随附基础力。
- 2/6/10/14/18 级各加一个法器槽。3–19 奇数级各加一个额外 Focus Power。Magic Item Skill 从 2 级开始给 UMD 无类型可叠加的半职业等级加值。
- 8 级 Repower Construct 使用 1 通用点，原作 Swift、Medium 射程、已死 Construct 校验、独立召唤池，8/12/16 级分别用 1 轮、职业等级轮数、职业等级分钟数。复活实体和已使用尸体应各自留生命周期记录；说明写 30 英尺，实际蓝图为 Medium。Control Construct 是独立的法术根。原作基础职业没有 Shift Focus、Object Reading、Aura Sight 或圆阵成长入口。Prestige 接线属于 Silksworn/Reliquarian。
- 20 级 Implement Mastery 选一个已学基础学校。原作给该校 4 投资点和虚拟职业等级；工厂的等级、DC 与部分共鸣属性引用它。Wrath 私有计算范围见 C。

## 八学派

数字标示实际选项的最低等级和成本。未标等级即没有额外等级前置。每个选项首先要求所属法器，子选项再按原作等级接线。可复用供体只提供已核对的动作、效果或图标，0099 自己拥有 Focus Power 入口与支付。

- **Abjuration。** Warding Talisman 每 2 投资给全部豁免 +1 resistance，实际属性先把投资截到 2 + L/2；说明另写 1 + L/4 的奖励上限。Mind Barrier 消耗 1，Swift 给 2L 的本地总吸收池。Aegis 用 1/2 点 Standard/Swift 启动护甲增强，逐档配置 Fortification、元素抗力和 spell storing，占增强预算；原作具体挂护甲组件。Energy Shield（3，1）用跨酸、冷、电、火的 5L 合计吸收池，跟踪耗尽与结束。Unraveling（5，1）复用定向 Dispel Magic。Globe of Negation（11，3）复用 Globe 系列的入射检查，实现按累计抵消环位扣 L 容量的移动球。首切片验证 Mind Barrier、Warding Talisman 与本校法表。Shield、Resist Energy 等用原生根，Warding Weapon、Allied Cloak 和两种 Globe 用 CotW 根。
- **Conjuration。** Casting Focus 每 2 投资给召唤额外轮数和 Conjuration buff 持续时间，属性封顶 L；需要核对已有施法时长钩子语义。Servitor（1 点）召单只怪，1 分钟；1/4/7/10/13/16/19 级升级至 Summon Monster I–VII。Flesh Mend（3，1）触摸治疗活物，Lv3 起每隔四级一档 d8，再加 L。Psychic Fog 用 CotW Obscuring Mist（1）和 7 级 Solid Fog（2）。Purge Corruption（5，1）用 Neutralize Poison/Remove Disease 两支。Side Step（7，1）用 Dimension Door 移动分支，按 L 设射程，核对站位与合法落点。
- **Divination。** Third Eye 每 2 投资给 +1 Perception insight，上限 L；7/13/19 级与 9/12/15 点联合门槛，分别选择 See Invisibility、60 英尺 Blindsense、30 英尺 Blindsight（后者含 gaze immunity）。Sudden Insight（1）Swift，加 max(1,L/2) insight，攻击/技能等一次触发后移除。Diviner's Fortune（1）用原生同名触摸能力与 buff，1 轮攻击、检定和豁免加值。Danger Sight（3，1）Swift，下一次受击或豁免的 insight 加值，随后移除。Mind Eye（5，1）复用现有 Arcane Eye 受控探测设备。In Accordance with the Prophecy（9，按一次合格施法付 1）尝试免升环 Empower，20/15/10% 失败档从 9/12/16 级开始；无足额资源时原作两段独立处理的漏扣风险由 C 统一规约。
- **Enchantment。** Glorious Presence 用与 Warding Talisman 相同的投资截断，给 Persuasion、UMD 与 Charisma 能力检定 competence。Cloud Mind（1）Will，HD ≤ L 则 Daze，否则 Stagger，均 1 轮。Binding Pattern（7，1）Will 失败并行施加原生 Paralysis/Stagger、时长半级；王国原作此方法本身没有另加逐轮重豁免动作，应核对原生 buff 可否复用。Inspired Assault（1）接触，分钟/级，攻击伤害和三豁免 morale 为 1+floor(L/6)。Obey（1）复用 Command Halt/Fall/Run，另接同种族 DC +2；Wrath 的完整 Command 根有其他分支，Obey 只取这三支。
- **Evocation。** Intense Focus 每 2 投资给合格的 Evocation 伤害 +1，上限 1+floor(L/2)，按攻击/施法事件核对目标和伤害包的去重。Energy Ray（1）酸/冷/电/火，远程接触，1+floor((L−1)/2)d6。Energy Blast（5，2）同四分支，20 英尺爆发，Ld4、Reflex 半伤。Radiance（1）照亮武器，暴击触发短期显形与易中，结束自身的启用效果。Light Matrix（5，1）初次建立唯一光球，后续 Move 远程接触致盲，不重新支付建球成本。Wall of Power（9，1）四元素墙，2d6+L、L 轮，复用墙形/回合接触入口，区别于普通 Wall of Fire。
- **Illusion。** Distortion 给 min(投资,L)% 独立 miss chance，识破幻象时跳过。Color Beam（1）复用 Blinding Ray 骨架，远程接触，以 L 分流 Blind/Dazzle。Terror（1）触摸，低 HD 敌人被迫引发友军 AoO。Shadow Beasts（9，1）只克隆召怪族，5→9 阶，60% 真实度和不信检定。Unseen（7，1/2）用 Greater Invisibility 自己/他人两支。Bedeveling Aura（9，1）使 30 英尺内敌人减速、无法 AoO 并视为被夹击，持续半级轮数。
- **Necromancy。** Necromantic Focus 以投资每 4 点加对 Undead 的法术 DC、每点增加 2 HD 控制额度，原作依赖自有尸体动画机制。Mind Fear（1）Will，符合 HD 阈值 Frightened、高 HD Shaken，均 1d4 轮，排除 undead 和 construct。Flesh Rot（3，1）粘手触摸命中后造成 Unholy 伤害，Ldmg 加上 3/7/11/15/19 级递增的 d8，排除 undead、elemental、construct。Pain Wave（7，1）20 英尺爆发内施加 Sickened，持续 L 轮，Will 成功缩为 1 轮。Soulbound Puppet（1）用小动物侦察单位及各自 buffs。Spirit Shroud（3，1）1d6+L 临时 HP，附对指定负面描述符的 resistance。Necromantic Servant（3，1）使用尸体与私有召唤池，9/13/17 级分支为 bloody、范围动画、共享团队专长，受半级数量和有效等级限制。Ghostly Horde/Life Drain 属 Necroccultist，基础选择表未挂。
- **Transmutation。** Physical Enhancement 在锁定前从 Strength/Dexterity/Constitution 择一，至少 3 点，每 3 投资给 +2 enhancement；实际先截断到 3+L/2。Legacy Weapon（1/2）Standard/Swift 主手续附魔，1 分钟，1+floor(L/6) 增强预算与总增强上限 +5；武器换手、组内属性资源与失效要一起验。Mind Over Gravity（7，1）自施飞行 +10 ft，以现有 CotW 飞行代码提供私有 power 效果，Fly 法术候选另依 Tome。Philosopher’s Touch（1）主手近战武器材质处理，原作 cold iron、mithral、adamantine；有单体/群体分支，以代码门槛为准。Sudden Speed（1）Swift 自身加速。Size Alteration（1）Enlarge/Reduce 源效果并放宽生物类型。Quickness（5，1）接触版 Haste 相关增益，含每 6 级一次额外 AoO 的分支。

## 四个 Panoply

每个只占一个法器槽；已学成员是 AND，生效时每个成员至少 1 锁定投资。共鸣在成员投资总和上计算，法术从成员并集挑一个/有可学候选的环，支付从成员当前可支出普通余额取。每个依赖、基础力、共鸣、全部实际 Focus Power 按以下边界交付。

- **Trappings of the Warrior**（Abjuration、Transmutation）。Martial Skill 每 4 点给 +1 BAB；原作无装备法器动作校验，且用加值组件。Counterstrike 自动反击触发，Swift 消耗与 AoO 容量同时验证，1 点，伤害 rank 实际最低 3 而说明为 1。Combat Trick 是原生战斗专长列表的一次选择。需关注被 Necromantic Servant 借用的原作 Martial Skill buff。
- **Mage’s Paraphernalia**（Divination、Evocation、Necromancy）。Scholarly Knowledge 每 4 点给四类知识/学识加值，上限半级。Spell Power 只对本法书施法，+2/+3/+4 CL 从 1/12/18 级开放，分别付 2/3/4。Metamagic Knowledge 是一次超魔专长选择。Metamagic Master 在合法已知超魔且合格施法时按原始加环差付点，免费加进该次施法。原作向所有超魔专长追加组件，Wrath 改为私有查询。
- **Saint’s Holy Regalia**（Abjuration、Conjuration）。Font of Healing 对明确的 Cure 根恢复活物 HP，附加 min(半级, floor(总投资/2))。Restoring Touch（2）治疗属性伤害；10 级起 3 点治吸取，按每三级一档。Guardian Aura（3）以 Full Round 启动，20 英尺友军 sacred AC、移动减速、1 分钟。
- **Performer’s Accoutrements**（Enchantment、Illusion）。Trickster’s Skill 给 Stealth/Trickery，每 3 投资 +1，上限半级。Trickster’s Edge（2）复用使本人攻击绕过目标 Dex AC 的 Swift 效果。Puppet Master（9，3）用 Dominate Monster 效果，半级轮数、Occultist 特技 DC。

## 法表与提供方

原作 143 条，包括 6 Knacks，按实际环位归档。104 行可用 Wrath 根，27 行用现有 CotW 根，12 行是外部供体。原生验证为静态根 GUID、类型、父关系、学派、可执行组件和本机元数据中的环位检查。私有 Occultist 环位采用原作表，例如 Hideous Laughter 的 GUID 对应此根而非原注释的 Hypnotism，进 Occultist 1 环；Mass Daze 进 4 环，Lesser Animate Dead 进 2 环，Control Construct 进 6 环。Spirit-Bound Blade 的旧根为 Evocation，现有 Wrath CotW 根是 Transmutation；采用现有学派与根。原作以 Hungry Pit 顶替 Roaming Pit 的 5 环行保留。原作注释掉的 Frost Fall、Tactical Acumen、Etheric Shards 无实际接线。

- TabletopTweaks-Base。Cloak of Winds 3、Sands of Time 3。
- ExpandedContent。Inflict Pain 2、Rigor Mortis 4、Wall of Fire 4、Mass Inflict Pain 5、Particulate Form 5。Particulate Form 的提供方发布在 7 环，0099 用 5 环。
- CharacterOptionsPlus。Ice Slick 2、Weapon of Awe 2。以源文件的开关和发布结果为准。
- TomeOfTheFirebird。Fly 3、Fire Shield 4、Freezing Sphere 6。Fire Shield 的共享目录暂无行；当前供体的 Warm/Cold 变体共用一个法术根 017970c386634351bfdea34d6a4d4c74。复用路径附在 JSON。[Fire Shield 发布与分支](https://github.com/pheonix99/TomeOfTheFirebird/blob/8018d5d388ef4b746e74b196d7f55e7d64877861/TomeOfTheFirebird/New%20Content/Spells/ElementalShieldSpells.cs)

上面 12 条位于选装法表。B 运行时确认设置、根及其发布；受 DLC 条件的原生根继续服从 DLC 授权。Focus Power 核心需要的效果单独核验，缺失核心效果、错误根或者注册失败时保持全职业未发布。GitHub 检查覆盖主库提交、分支、全部 PR（含关闭）、#15 与 fork 的三条分支及 main 树；PR #14 涉及共享注册/Harmony/构建，未发现 0099 实现。[认领 #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15) · [PR #14](https://github.com/shenzhoudadi/CotW-Wrath/pull/14)

本阶段只做源码和元数据盘点。编译、游戏/离线运行时定向检查、打包、安装包上传均未运行。
