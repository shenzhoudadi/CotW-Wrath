# C-0099-20261115-r1 · Occultist 共享机制合同

适用基线 0.14.74 TEST（939e04d）。用 [A 盘点](A-occultist-map.md) 与 [根和原件](A-occultist-assets.json) 的学派、GUID 与实际环位。B 先以 Abjuration 的 Mind Barrier、Warding Talisman、资源操作和私有法术学习串起原生入口，首切片只供开发。验收预期见 [C 用例](C-occultist-cases.json)。

## O-S01 · Mental Focus

### 状态

角色实体及 0099 职业是所有权边界。L 为实际 Occultist 等级，M 为有效 Intelligence 修正值；每个休息周期保存普通预算 B=max(0,L+M)、通用余额 G、各校普通投资 A 与剩余 R、普通已耗 U 与失效 Q。守恒式为 G+ΣR+U+Q=B，且每校 0≤R≤A。A 是投资记录，重复法器共用同一学派 A/R。精通另有 4 点，记已花 T 与余额 H，T+H≤4。每个周期只发一次；同日更换精通学校可转移尚未使用的精通额度。

锁定保存已学学校及组合的有效投资、选定的身体属性及由此计算的共鸣快照。基础校门槛读 A 加本校精通投资量。组合要求每个成员达到最低 1，按总投资计算。实际支付和 Replenish 都只改余额，学校耗尽时仍维持已有共鸣，直到休息或来源撤销。这采用 Kingmaker 可执行语义。读档、buff 重建和重复回调直接恢复已有快照。[原作 ImplementMechanics](https://github.com/Holic75/KingmakerRebalance/blob/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/NewMechanics/ImplementMechanics.cs) · [桌面规则对照](https://aonprd.com/ClassDisplay.aspx?ItemName=Occultist)

Warding Talisman、Distortion 等直接赋给角色的共鸣照常参与通用豁免和受击规则。按施法触发的效果，如 Spell Power、Casting Focus、Intense Focus 和 Font of Healing，在 0099 以本职业法书和明确适用的私有 Focus Power 为边界。被动属性与施法事件是两条入口，物品和混职法书不得触发 0099 的附带施法加成或消耗。

### 状态变化

- **投资与锁定。** 未锁且拥有该基础校，Free 投资每次 G−1、A+1、R+1。Panoply 没有投资入口。Free Reset 归还各校未花的普通 R，清除普通投资；当天普通支出、精通支出保持。Physical Enhancement 在锁定前从三种物理属性择一。Full Round Lock 一次固化快照，锁后禁止重新投资或 Reset。未知学校、错误角色、非法数量或重复执行事件均拒绝。
- **学校支付。** 确认能力所有权、已锁、投资门槛、合法目标或触发事件及全部价格后一次提交。优先本校精通 H，再扣本校 R；整笔不足则拒绝。触摸在所选施法动作付费，粘手投递不再计费。合法施法付费后未命中、正常豁免或法抗消耗照常。维持费用、AoO、随施法模式在实际触发时走同一路径，每个事件最多一次。
- **Panoply 支付。** 组合有效且每个成员都有锁定投资时，从成员的普通 R 中取点。逐点选当前余额最高的成员，平局按 A JSON 的成员顺序；算完总向量且余额充足再一起扣减。原作 ConnectResource 令组合资源显示为自身容量与成员余额总和的较小者，并在 Spend 后回扣成员。Wrath 组合采用纯投影与原子支付，禁用第二次扣款，精通专用点只付本校 Focus Power。[原作资源桥](https://github.com/Holic75/KingmakerRebalance/blob/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/NewMechanics/ResourceMechanics.cs#L328)
- **通用与补充。** Repower Construct 是显式通用支出，1 点，允许在锁前用。原作把投资动作的 RequiredResource 换成 Universalist。ContextActionInvestFocus 先检查锁定，只有投资记录的写入受限，Restore(school,1) 仍执行，锁后能 1:1 补回已花的学校点。原作说明提及高成本，基础职业与四组合的实际执行图没有通用直付。桌面规则是直付 2 倍。0099 保留 Kingmaker 的 1:1 补充和显式通用支出，学校或组合余额不足时直接拒绝。锁后 Replenish 仅在已学、A>0、R<A 且 G≥1 时提交 G−1、R+1，快照、精通和 U 保持，满池拒绝。以独立动作说明区分锁前的 Invest。[原作投资与补充](https://github.com/Holic75/KingmakerRebalance/blob/1332fb0db844b7863f484ca978bea2349fe49769/CallOfTheWild/Classes/Occultist.cs#L1613)
- **休息。** 成功休息或首次真正取得本职业时创建周期。成功休息重新采样 L/M，余额重置为 B，清空旧普通投资、锁定、共鸣、自有待触发模式及消耗，精通重新授予。日内等级、Int 增益到下次休息兑现。临时属性波动、死亡、读档、切场景或重附事实均保持已有周期。相同休息事件幂等。
- **升级与重训。** 预览只改自己的状态，取消恢复到预览前。提交重训时保留现周期普通与精通支出。未锁时失去学校退回该校未花 R；已锁时其未花 R 转入 Q，撤销该校及失效组合的快照和能力。锁定后新学学校等下个周期投资。提交重训若预算下调，B 仍是本周期建立时的上限，按新可用余额的下限 max(0,新上限−U−Q) 从 G、再从各校稳定顺序的普通 R 转入 Q，使新可用额受限。全职业删除时撤掉私有效果和资源投影，留失效/消耗及周期最小记录，防止同日重新获得时充值。读到未知或不一致状态时关闭支付和共鸣直到合法修复或成功休息。
- **原生入口。** UI 余额、预检与实际执行读同一权威逻辑。适配 Wrath AbilityResourceLogic、激活能力或随施法事件，执行前重新校验，失败无资源、效果或模式变动。用发起者、私有能力和事件身份去重。现有 Harmony 版本、前后缀链、GUID/TypeId 注册约定均适用。补丁限 0099 书、能力或资源身份，第三方普通资源透传。存档只保存权威来源、快照和必要的去重身份，UI 与镜像随时重建。

例。B=7，锁定前投入 Abjuration 4 与 Transmutation 2，G=1。Mind Barrier 后 G=1、R=3/2、U=1，Warding Talisman 仍用 4。再补 1 后 G=0、R=4/2、U=1。

## O-S02 · 法器与法术学习

- **法器身份。** 所持法器是角色的私有授予事实，沿用王国原作的抽象法器。装备切换、商店、借出物品与施法无关。法器槽在 1 级依次两次，2/6/10/14/18 级各一次，共七槽，每槽有稳定键。首次选择学校才发该校基础力、共鸣及资源入口。同校 Extra Spells 需首次事实且只可再选一次，只加法术配额；同一次升级可先获首次事实再选 Extra Spells。顺序校验失败时保留可修正的预览。
- **Panoply。** 每个占一个槽、每个只选一次。需全部成员的基础学校事实，候选法术为成员并集。失去成员后组合及派生来源即时失效；修正预览可重新计算。成员双校最快 2 级、三校最快 6 级，组合本身不会授予成员学校。
- **私有法书。** Intelligence 自发念力，独立书、法表、Known 选择及同校过滤列表；复用 Wrath Alchemist 六环位表，1/4/7/10/13/16 开环。Wrath 的 Alchemist 没有 SpellsKnown 表，0099 仅由法器给学习配额，原生自发法书若要求表则配私有零配额表。学和施法门槛 Int≥10+环位，普通术 DC=10+环位+Int，六个 Knacks 固定在 1 级授予，无限次。沿用现有 PsychicCasting 的 thought/emotion、Center Self、Logical 通用机制并审计所有 IsPsychic 调用点。Psychic 职业专属扩展仍核对原书身份，尤其 Magaambyan、Discipline、Amplification、临时可知和 Undercasting。普通 Psychic、Psychic Detective、物品与同名 Arcanist 独立。
- **配额与来源。** 以（槽键，实际环位）记录一个 root 或空位。每个已学法器对每个已开的 1–6 环给一选。新法器补当前全部开环，新开环给所有已选槽各一选。升级 UI 走原生选择入口与按校过滤的私有列表，预览重进和撤回使用待提交来源差分。选择 canonical root，变体/粘手投递只占一个配额，同书已有根从可选候选排除。候选耗尽时该配额保持空位，下一次合法升级或重训才可能补选；禁止挪到不同学派、环位或背后赠送。放弃来源走原生已知法术更新；根仍由本书另一个合法来源持有或由其他书持有时，该来源继续有效。
- **身份与环位。** 按 A 的 Occultist 实际环位与目标根实际学派。私有表只挂 canonical GUID；现有根、原生和外部列表保持各自环位。若 WotR 从 backlink 求环位，仅让本书使用私有表查询。外部 12 根检查精确 GUID/name、Spell/父关系、学派、执行形态及启用的外部列表发布。发布证明用原生或供体列表与 backlink 双向一致，排除 CotW 自己生产的列表，Fire Shield 验证 Warm/Cold 两支。未启用仅不进入候选，继续遵守原生 DLC 可用性。曾选根因提供方缺失而不可用时，保存 GUID 和来源；供体恢复时恢复同一根。共享根迟加载需用现有注册重试，最终原子发布职业。
- **Implement Mastery。** 20 级从已学基础校中选一。四点精通投资既进入该校共鸣快照，也作为独立的本校支付额度。该校普通 Focus Power 的功能等级取 L+4，DC 取 10+floor(L/2)+Int+2，避免通过虚拟等级再加一次；继续遵守原作能力明定的封顶。共鸣按锁定快照计算，本校原作工厂 rank 用有效等级；学习前置、职业等级、法书 CL、法术位、Magic Item Skill、Repower Construct、Panoply 一律用实际 L。精通的预览提交不将普通学派重新解锁，改变精通来源时撤销因旧精通增加的共鸣和额度，并在合法提交时重建受影响的快照。无虚拟职业或全局等级补丁。

## 移交 B/D

Abjuration 的 2 环同时有原生和 CotW 候选，6 环只有一个候选，能验证独立配额、重复学校耗尽和来源撤销。B 完成首切片的原生支付与升级学习用例后按完整学派分包；八校、四组合、成长和私有法表齐套后由同一负责人注册。PR #14 的共享注册、构建和 Harmony 变更在接线前复核。核心效果缺失时发布原子失败，选装法术缺失按候选规则处理。B 负责人待维护者指定，#15 继续开放。

合同已冻结，C 用例是后续验收规格。本轮的编译、游戏/隔离运行时定向检查、打包、安装包上传均未运行。实机升级、存读档、重训及外部供体启用待实际验证。
