# C-0068-20261111-r1 行为合同

适用于 COTW-0068 Psychic Detective 和 S-PSYCHIC-0068。A 已完成来源核对，B 可以按本合同实施。`L` 为真实持有本变体的 HA Investigator 等级，其他职业等级不累加。纯侦探与 Psychic 混职都使用真实职业、书本和 fact，保留 Harmony 2.0.4 及既有 GUID/TypeId 约定。

## Q01 提供方、注册与发布

采用 A 中固定的 HA 真实职业／进展／书本／天赋／毒抗／毒免身份。职业必须实际引用预期进展，进展必须包含相应授予，天赋入口及必要子项须可解析。旧书按 HA 真实身份核对，其 `m_CharacterClass` 的已知原生 Alchemist 引用不作为新书构建依据。不创建替代 Investigator，也不把空壳蓝图当成提供方已就绪。

提供方或本项目必要 Psychic 施法／11 种增幅根尚未就绪时延后本变体注册，给出具体缺项。接入现有 `RegisterOptional` 重试。全部自有图先暂存，再统一接到 HA 职业、UI 和两项新天赋。冲突或异常回滚本次自有发布与追加引用，重入不重复。后续已有可选法术提供方就绪时，只刷新本包法表的可用引用。

公开数据确认了适配依据。真实 HA 加载与注册成功须由 B 实测后记录，不能把本合同当作运行 PASS。

## Q02 完整成长和职业技能

沿用 HA 的 BAB、生命骰、豁免、技能点、灵感和研究能力。一级新增 Detect Magic 原生特性、私有 Psychic Spellcasting。二级新增 Psychic Meddler。三级用 Phrenic Dabbler 替换当级的一次 Investigator Talent。

移除 HA 在 2、5、8 级的三次毒抗授予及 11 级毒免，其他 5、7……19 级天赋授予继续存在。只在本 archetype 上写加减进展。技能集合采用原作九项 Stealth、Thievery、Knowledge Arcana、Knowledge World、Lore Nature、Lore Religion、Perception、Persuasion、Use Magic Device。换书、阵营和兼容变体判断使用原生 archetype 机制。

Detect Magic 复用已核实的原生特性。该 Wrath 特性组件为空，本包不扩展场景侦测系统。禁止授予整套 Psychic 职业、纪律、Major Amplifications 或 Phrenic Mastery。

## Q03 私有六环法术书

通过真实 `BlueprintArchetype.ReplaceSpellbook` 接入。新书 `m_CharacterClass` 明确为 HA Investigator，Intelligence、自发、零 caster-level modifier、不能抄卷、`SpellsPerLevel=0`，使用当前原生 Bard 的每日和已知法术表及其六环解锁。`ChangeCasterType=true`，本变体的 Arcane／Divine 标记均为 false，marker 的 `MaximumSpellLevel=6`。原生 ClassData 与 DemandSpellbook 必须实际取得新书。

不继承旧书的 `IsAlchemistSpellbook` 组件。五个原作零环 knacks 走同一法书和原生零环规则。授予已有 Center Self，复用 thought 的 -10 concentration、Center Self 抵消、emotion 阻止与 Logical Spell 绕过行为。只对这本标记书生效，物品施法与其他职业法书继续由各自原生规则处理。

## Q04 法表、可选提供方与降环

采用原作代码的 180 行目录，复用 `MindbladeSpellAnchors01454.Rows`。它比“所有 Psychic 低环法术”的概括少 Condensed Ether 和 Phantom Limbs，保留这两处原作目录边界。Find Traps 固定 1 环，Banishment 固定 6 环。相同实际法术身份只留一个侦探环位，显式侦探环位优先。表形使用既有 `SpellListCompatibility` 的 0–10 索引规范，7–10 环为空。

抽取现有单行候选解析给两个调用方共用。Mindblade 保留其排除和 Find Traps 2 环语义。对九个空行按 `A-evidence.json` 的 providerCorrections 补充当前可用映射。八项使用既有共享链接与 Available，Greater Stunning Barrier 使用核实的 TTT 身份、Abjuration 和真实发布法表检查。保持候选优先顺序，歧义给出明确冲突。

原生和本项目已实现的法术直接复用实际根。可选插件缺失、停用、身份错误、执行根不可用或应有发布链缺失时，跳过对应可选法术并记录名称／原因。沿用现有可选目录政策，运行可用数与原作总行数分别汇报。禁止为这几项重写外部法术或注册占位。刷新自有列表不能改写其他职业列表、提供方根或永久已知法术。

降环复用 `PsychicUndercasting` 和现有临时知识账本。下位法术属于同一本侦探书，不占正常选法配额。失去高位来源只清退本包自有的临时下位条目，保留永久已学和其他真实来源。

## Q05 Psychic Meddler

2 级获得，对 mind-affecting spells 或 spell-like abilities 的真实豁免事件给予加值。2／5／8／11／14／17 级分别为 +1／+2／+3／+4／+5／+6，17–20 级保持 +6。采用原作说明中明示的 +6 上限，显式配置 rank 最大值。无关描述符、没有法术或类法术来源的普通效果不触发。

等级配置放在实际承载豁免组件的 fact 上，读取真实 Investigator 等级。优先使用原生 descriptor 豁免组件并补足来源类型范围；原生实现会读取 Reason.Context，因此定向检查必须传真实 reason/context。只给该事件临时修正。

## Q06 Phrenic Dabbler 和共用增幅绑定

3 级获得私有池与一次基础增幅选择。池上限为 `floor(L/2)`，没有属性加成，3／4／5／20 级为 1／2／2／10 点。Expanded Phrenic Pool 天赋另加 2。沿用原生资源的休息、正常升级和首次授予语义，重复注册、加载或协调不凭空恢复已用点数。

11 种选择为 Biokinetic Healing、Conjured Armor、Defensive Prognostication、Focused Force、Mind's Eye、Overpowering Mind、Will of the Dead、Ongoing Defense、Relentless Casting、Undercast Surge、Psychofeedback。复用现有效果、层级、cast receipt 与失败处理。私有授权图只改书本、职业、池、等级和必要菜单／转换引用。

S-PSYCHIC-0068 在现有运行时中建立一个显式 binding，至少可判定目标法书、计级职业、资源与模式归属。旧蓝图和旧存档没有新 binding 时按原 Psychic 规则解析。修改范围覆盖 Eligible／Real／Candidate／Plan／Claim、池解析、按等级解锁、模式协调及菜单。实际调用点以 A 的 sourceReuse 指纹为依据，禁止只改 Eligible 一处后把克隆图判为已接通。

同一本书最多一个侦探模式。切换只关闭同一书本绑定的模式。正常 Psychic 的 Dual／Major 和侦探模式各按所属法书处理，双职业的模式不互相清理或借池。只有实际侦探书本上的有效非零环施法能用其资源，书外施法、物品、预览和重复通知不扣点。欠费保持原本施法效果，不加增幅。不做资源混合支付。

成功施法的同一次事件只支付一次，子上下文、重复回调不再扣；模式切换不改变已支付执行的效果来源。取消／失败按现有共享运行时的真实完成状态向原池返还一次，UI 预览无状态变化。不得额外制造第二份费用系统或跨书的静态模式计数。

Overpowering Mind 的更高选项按侦探 L 的 8／15 级门槛。Undercast Surge 沿用同一家族、同一本书、同次转换规则，最高可施放环不超过 6。Psychofeedback 通过真实侦探书的原生转换消耗 2–6 环法术位及 2 点侦探池，给所选体能属性等于实际消耗环位的 enhancement，持续每 L 级一分钟；保留原有动作类型和不叠加规则。菜单和转换都通过所属实际法书解析。

## Q07 天赋限制与独立职业来源

保留 HA 正常天赋目录及各项既有前置。只给 Investigator 天赋体系追加两项有真实侦探三级前置的私有选项。Expanded Phrenic Pool 加 2 点，按原作单一特性限制选一次；Extra Phrenic Amplification 从相同 11 项中再选一次，也只能选一次且不能重复已有增幅。

侦探不能经 Investigator Talent、其嵌套 AlchemistDiscovery 或 Extra Investigator Talent 路径获得 Infusion、Mutagen、Enhance Potion、Extend Potion。原生 `BlueprintFeatureSelection.CanSelect` 能取得 `FeatureSelectionState.Parent/Source/Selection`，限制使用实际入口及祖先来源。保持原有前置检查和提交校验。嵌套选择若没有合法结果，不应作为空选择交给玩家。

这些四项原生特性不能被添加全局 NoDetective 前置。同一角色从独立 Alchemist 或其他合法来源取得其中一项仍遵守该来源原有规则。已选特性不因开本变体而被全局删除。正常 Investigator 的同一路径保持原样。允许使用有来源范围的私有选择克隆或一个受限的原生选项检查接线，源根和效果只维护一份。

追加两项天赋后调用已有 `ExtraInvestigatorTalent.Register` 刷新，核对其 m_Features 与 m_AllFeatures。原有等价外部普通专长的兼容策略继续有效，任何路径的放行都要能追溯到实际天赋来源。

纯侦探的 Phrenic Dabbler 不赋予 Extra Phrenic Amplification（Psychic 普通专长）资格。真实另有 Psychic 等级且满足原专长前置时，可以按原规则选取，其效果仅作用原 Psychic 书和池。禁止 HasFact 或职业等级伪装。

## Q08 生命周期与已有对象

私有 GUID 和 TypeId 固定，授权跟随原生 fact/source，书本跟随真实 ClassData。休息按原生资源刷新；升级调整本书和池的有效等级；降环、模式、转化和天赋奖励按实际来源对账。失去本变体时移除自有来源，保留其他职业书本、气池、已学知识与正常提供方数据。移除模式不撤销已经支付且仍有效的独立效果。

存读档按现有原生组件和 PostLoad 恢复绑定及临时知识。完整 SaveManager／重训控制器是否运行如实记录。旧 Investigator 转为侦探由原生重训／选择流程操作，不能把旧 extracts 全部迁成免费自发已知法术，也不能重写原提供方或旧存档的全局书本身份。

## Q09 验证与交付

实施后只编译及运行本项最小必要定向检查。共享绑定实际改变的正常 Psychic、双职业模式、支付、选项和 Mindblade 候选解析应放在本项局部正反例中。禁止全历史、全量库、整套专项和游戏／UMM 启动。结构检查、原生事件、提供方初始化和玩家实机分别记录实际证据。

保留真实命令、秒数、源码／DLL／依赖 SHA256 与失败原件，未运行写未运行。本轮 A/C 没有编译或测试 PASS。B 交候选后由独立 D 复查。正常 TEST 交付按已有流程统一版本、提交／推送、上传和下载核对。中文包内 README 保留单独一行“猫猫堡是猪猪”。
