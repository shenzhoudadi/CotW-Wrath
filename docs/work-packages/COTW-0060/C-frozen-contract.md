# C — Spirit Guide 机制合同冻结

合同 **C-0060-20261110-r1**；基线main `91825bcd021a0e7439a5d8541c3a631f0c9a6d5d`／0.14.69 TEST。本会话Sol亲自完成A后冻结，**B可执行：yes**。前提是使用本文及A-spirit-map.json的完整范围；有实际缺依赖时拒绝本包发布并记录具体原因，不准缩范围或空壳上线。这里的yes是实现放行，**不是代码、检查或玩家PASS**。

本轮生产只读，仅交付文档／证据／prompt；B/D尚未执行。以下裁决取代旧HOLD对本项的推测，不改变其他工作包合同。

## 1. Q01–Q10冻结裁决

- **Q01采用固定源码的持久选择**。3级选一族、一项该族巫术，保存到升级选择；只有原生重训更换。无需每日休息换魂UI，不添加24小时自动失效。原作描述中的wandering不是额外实现授权。理由：Oracle.cs:149–198,693–744；十族均返回常驻progression。
- **Q02采用仅本角色、仅真实Mystery来源的技能替换**。保留原Mystery事实和后续法术／资格，删除它额外贡献的AddClassSkill作用；保留Oracle基础5技能以及背景／其他职业／其他事实贡献。禁止删Mystery根、私有改名伪装HasFact或全局修改Oracle蓝图。具体合法窄钩子与成对计数见第3节。
- **Q03采用双通道属性**。本包有效等级L=真实Oracle等级；魂域基础／高阶能力primary与secondary都为Charisma；HexEngine.primary为Charisma、secondary为Wisdom。不能整池都用Wisdom或沿用0117 Int/Cha。固定StatType常量不因说明文字而自动替换。
- **Q04采用Oracle专属目录**。十族、39池条目、90法术条目必须完整；Lore的15级是Arcane Enlightenment，Waves三选项为crashing_waves、mists_shroud、fluid_magic，排除beckoning_chill。没有true层、manifestation、Wizard bonus-feat回退或额外魂域。
- **Q05采用原生来源账本**。九环奖励法术和Lore额外选择均绑定实际授予Fact／稳定组件名，活跃期间持续，撤销只移除自己的UnitPartTemporarySpellsKnown条目。复用TemporaryKnowledge01464已有保护，不写第二本账、不直接RemoveSpell、不把源自本包的知识变永久。
- **Q06采用四外部根严格门禁**。A-map列出的ExpandedContent三根＋CharacterOptionsPlus Frostbite根缺失、关闭、空壳或结构不符时，不发布任何0060根／子图／职业连接；晚加载按现有RegisterOptional重试。缺提供方不触发全项目环境重建、不另移植被用户排除重复的法术。
- **Q07采用Lore原作15/17/19三次选择**。每次一个当前Wizard表中不在Oracle基础表的合法法术，Int、Cha均至少10+环级，Oracle当前主书可施放该环。不能把Perfect Knowledge、Wish或辅助wandering的19级progression放进来，也不能每次把整张Wizard表加入Oracle。
- **Q08保留Fluid Magic的Oracle语义等价marker**。魂域已知法术从正常Oracle自发槽施放，故它不额外提供槽、法表或资源；其英文说明明确这一点。它仍占原作的一次巫术选择，不能悄悄删去或添加随机加值；这个特例不允许其他能力做空占位。
- **Q09采用有界私有化和现有公共原语**。新0060身份与旧0117不同，需改职业／属性／费用／冷却的可变图必须私有；效果确为只读且输入完全相同才共享。复用PsychicAssets.CopyData、缓存修复、原生账本和已有合法效果；不复制整份0117或再造通用SpiritsEngine。
- **Q10不得重带已知选标／来源错误**。Heaven's Leap新选择必须清掉该施法者以前的目标标记；没有“永久自带自身标记”抢占队友。Friend to Animals按Wisdom且最少0，不能共享仍读Charisma的原生Effect Buff。不要因此顺手改0117、Speaker、Sacred Servant或fork雾效工作。

## 2. 完整变体进展、身份和资格

唯一根token `SpiritGuideArchetype0060`；GUID统一 `MD5(UTF8("CotW.Wrath.SpiritGuide0060.v1/" + token))`，名字前缀 `CotWWrathSpiritGuide0060`。不更换已交付GUID／TypeId。新的序列化组件TypeId由B登记32位十六进制唯一表，在新增范围内查冲突；同一新类型发布后不得重排。主根、每族Progression、每族HexSelection、KnownSpellsFeature、BaseFeature、GreaterFeature分别使用稳定token `SpiritGuide...0060` 或 `{Spirit}{Purpose}0060`，token清单随B交付。

宿主：OracleClass `20ce9bf8af32bee4c8557a045ab499b1`；进展 `5dd9b4982c9ed0b4090d5a35c459b729`；原生主书 `6c03364712b415941a98f74522a81273`；基础表 `8443ce803d2d31347897a3d85cc32f53`；revelation selection `60008a10ad7ad6543b1f63016741a5d2`；Mystery selection `5531b975dcdf0e24c98f1ff7e017e741`。

1级加Class Skills适配，移除仅Mystery来源的额外技能；3级以Bonded Spirit选择替代revelation；7/15级移除该次revelation，不重复替换其他等级。所选魂域Progression包含3级巫术选择、4级法术feature、7级Base、15级Greater。普通Oracle的Mystery、curse、mystery spells、1/11/19级revelation和20级Mystery终阶全部保留。不得在RemoveFeatures中填一个不存在的通用mystery_skills GUID。

每次选择必须满足真实Oracle＋本archetype、当前所属魂域、巫术等级与前置；只允许一族和该族的一项巫术。普通Oracle、Shaman、Spirit Whisperer、Mystery/Extra Revelation不能凭Spirit Guide等级获得额外资格。独立事实合法授予相同能力时不互相删除。原生的单archetype约束沿用；其他提供方若开放叠加，先检查相同等级revelation／Mystery技能替换交叉，冲突组合拒绝，不全局禁用合法Oracle。

## 3. Mystery技能替换的本机裁决

当前Oracle基础已有四Knowledge/Lore和Persuasion。Mystery根上的AddClassSkill是额外来源；只改archetype.ClassSkills不会阻止它。**保留Mystery身份，用窄作用域、成对的AddClassSkill.OnTurnOn／OnTurnOff适配**，允许作为本包唯一必要的环境无关Harmony钩子；已从当前Assembly-CSharp元数据确认这两个托管方法有合法IL，不需升级Harmony。

作用域必须同时满足：角色确为Oracle/SpiritGuide；组件的真实OwnerBlueprint／来源Fact属于OracleMysterySelection实际成员（及明确追踪到其技能子事实）的白名单；组件是AddClassSkill。背景、第二职业、任意其他Fact、普通Oracle均走原方法。不能按所有Oracle类／类型名称模糊匹配，不改共享Mystery.ComponentsArray。

原生OnTurnOn执行ClassSkillRetain，OnTurnOff执行ClassSkillRelease。必须有按角色＋Fact身份＋稳定组件名记录的本包抑制状态：已执行的Retain只能Release一次；跳过过Retain的TurnOff不得再Release；移除本archetype时对仍存活Mystery来源恢复一次Retain。激活时晚于Mystery已生效，则只抵消本来源一次；早于Mystery则成对跳过；两种顺序结果一致。重复回调、卸载、重训和读档不产生计数漂移。白名单只读重建，来源状态随原生生命周期可恢复，不能仅靠不可恢复的static bool。若源Fact已消失，不恢复虚构技能来源。

这是行为及钩子范围冻结，具体存储结构由B实现；B必须跑第一个计数正反例后再接完整图，不能只检查补丁存在。任何确实不兼容的当前API变更必须附元数据命名blocker，不能改成删除整个Mystery或全局技能重写。

## 4. 十族效果、公式与属性

A-spirit-map.json中每族createSpiritAbility／createGreaterSpiritAbility／createHexes的源码行及公式是执行输入，**不是仅凭原生同名Feature可判完成**。保留原动作、距离、骰子、伤害类型、抗性、持续时间、按轮／按次消耗和描述符。等级公式中shaman level换L，不换成总角色等级；七级才解锁的基础能力也按L缩放，不按L−6。15级Greater仍按原本公式的L，不普遍减7。

- Battle：Battle Spirit、Enemies' Bane；两个池各max(0,3+ChaMod)，不是同一个共享全职业池。
- Bones：Touch of the Grave、Shard Soul；基础池max(0,3+ChaMod)，高阶原作次数与冷却按该方法保持。
- Flame：Touch of Flame、Fiery Soul；基础池max(0,3+ChaMod)，高阶保留原生火抗／武器效果层级，不授予20级manifestation。
- Heavens：Stardust、Void Adaptation；基础池max(0,3+ChaMod)，保留原作渐进能力而不是替换成错误的普通隐形／通用豁免。
- Life：Channel Positive Energy、Healer's Touch；引导池max(0,1+ChaMod)，伤害／治疗骰为1d6+每超过1级两Oracle等级额外1d6，伤害豁免DC按10+floor(L/2)+ChaMod。治活物／伤亡灵共享本包池；私有Extra Channel只给该池＋2且要求真实本包Life授予，不影响Oracle其他引导或Paladin／Cleric次数。
- Lore：Monstrous Insight基础池max(0,3+ChaMod)；实际识别技能按目标类型，Insight只加L一次，不改识别DC来同时变难；对该目标攻击／AC的+2 Insight持续1分钟，成功失败都按原作授予。Greater见第5节，绝非0117 Perfect Knowledge。
- Nature：Storm Burst、Spirit of Nature；基础池max(0,3+ChaMod)，低血量快速医疗的原作触发／等级升级保留，不授予动物伙伴或变形manifestation。
- Stone：Touch of Acid、Body of Earth；基础池max(0,3+ChaMod)，高阶按原资源／逐轮规则，不能把计费改为持续免费开关。
- Waves：Ice Splinter、Frigid Blast；基础池max(0,3+ChaMod)，保留冰伤害／豁免／冷却；三项巫术按Q04。
- Wind：Shocking Touch、Spark Soul；基础池max(0,3+ChaMod)，保留原闪电效果／抗性，不能授予0117 Manifestation。

巫术池字段精确取A-map的hexFields。所有DC中hex_stat使用Cha；读取hex_secondary_stat的原作分支用Wis，至少包括Confusion Curse和Entangling Curse持续max(1,WisMod)、Friend to Animals动物三豁免加max(0,WisMod)、Sparkling Aura金属命中回调的Wis数值、Starburn每日max(0,1+WisMod)。其余level型持续／资源保留原公式，不能把它们全改成Wis。Benefit of Wisdom采用原作hex_stat==Charisma分支：四Knowledge/Lore基准属性换Cha，保留各类来源和重算；不是0117的只改两Lore为Int。原作固定的Intelligence inherent bonus（Mental Acuity）仍加Int，不改为Cha。

对需要实际触发的支付、目标冷却、命中后移除、反应伤害，必须绑定真实Spend／命中后事件：预览／CanUse不扣；一次动作只扣一次；合法外层施法已付费的持电荷接触在未命中后保留原生充能，不再扣；无资源不可施放；失败的保存按该能力原本是否消耗处理，不凭是否命中自行退款。UI取消在未进入原生支付点前不扣。一次事件多个子context不可重复支付／加冷却。private buff的保留／解除与资源恰好配对。

## 5. 奖励法表与Lore额外法术

90行精确环位和身份以A-map为准，同族9项按数组顺序1–9环，不按原生该法术常见环级重排。Life的Mass Heal／Resurrection与Flame的Fire Storm、Heavens的Glitterdust原作重复壳可复用已证明的原法术根，原环位不变；不因包名含Oracle就换成任意近似治疗／攻击法术。

4级起仅在Oracle实际主书已可施放的环级加入；低于4级不授予。使用普通Oracle自发槽，不增KnownSpells选择点，不建Spirit Magic特殊槽或每日免费槽。升环／降环／读档／事实移除同步；未解锁环不得提前出现在可施放入口。找书按真实Oracle职业归属，兼容原生合法替换／合并书；换书时只转移自己的来源，其他书不被清理。

沿用AddSpellKnownTemporary＋UnitPartTemporarySpellsKnown；本包每个授予源用稳定组件名，按source fact＋component匹配（序列化重建时不能只比内存引用）。不直接写Spellbook.RemoveSpell、不可把其他永久或临时知识删掉，也不能用“只加不删”漏掉重训清理。刷施法菜单沿用当前Dirty／native临时知识同步，不恢复Amnesiac bug。

Lore15／17／19每次选择一个Wizard法表法术；基础Oracle法表排除，Int和Cha均>=10+circle，circle<=当前Oracle主书可施放上限。选择的法术在Oracle同环使用，无额外费用／加环。私有参数选择器可沿用原生parametrized spell UI与资格检查，但结果由本包来源组件接原生临时账本，**不要让LearnSpellParametrized永久写入后再全局RemoveSpell清理**。每次selection一个独立来源；重训／移除Lore进展只去除其自有条目，任何独立永久或其他临时来源保留。当前Wizard表新增可用法术可列入，但不得修改共享Wizard／Oracle表，不为缺提供方占一个空洞选项。

Fluid Magic不添加额外法术或资源；Q08的说明要在游戏内英文显示，避免玩家误认为有新特殊槽。它是唯一经冻结的语义marker特例。

## 6. 原子发布、提供方与共享机制边界

90项和39池条目及全部必要可变子图先在私有staging构建、验证后一次发布。只接受正确BlueprintAbility／Spell根、真实school／effect／spell-list及提供方启用状态；不能把Frostbite Effect、同名武器或一个空Ability视为合法法术。四外部根使用A-map固定身份；沿用现有PsychicSpellIntegration.Available等六要素可用性合同，不另起检测框架。无法确认必要提供方时记录精确repo／GUID／理由，并整包返回false；不能只发布九族或删掉某个奖励法术。

BlueprintRegistration主入口和RegisterOptional均接同一幂等Register；ModuleCatalog单条正式状态；支持晚加载重试。注册失败回滚private cache、职业archetype列表、UI组以及本包所有白名单／辅助注册状态，不修改外国已存在GUID。重复调用返回同一个逻辑形状，不复加资源、魂域或技能计数。

可安全共享只读原生效果；改输入就连同可变Buff／Area／action／资源有界克隆，保留真实child/context／OwnerBlueprint。0117的private GUID、资源Fact和选标状态不得直接发给Oracle。不得全文复制SpiritClassBridge0117、为Oracle再造另一套费用／临时知识引擎，或未经必要理由重构0117。若提取少量真公共helper，只验证受影响的那个共享分支，不跑0117全部效果／全历史。

## 7. 生命周期与局部边界

休息仅恢复各自每日池／结束原生rest-cleared效果，**不换魂域或再发一个巫术**；来源法术仍跟随其持久feature。升级单向解锁3/4/7/15，降级／预览副本不得泄漏到正式角色或重复购买法术。重训／移除archetype撤销自有选择、资源、冷却／标记、状态、附魔和临时知识，恢复仅Mystery来源技能的正常行为。不要删除物品永久附魔或其他来源事实；重加后次数不是双倍，不能无限刷新资源。

存读档按稳定事实／组件键恢复；不要求本轮重建完整SaveManager。先有Mystery再获archetype、先archetype再选Mystery、已加载或已耗费资源的重入都必须幂等。原生伴随Buff／武器／免疫等独立来源保持。Heaven's Leap只移动本次明确选中的单位；新选标清旧标，仅清自己的来源，首次选择自己再选队友不能仍优先自己；每目标／施法者冷却按原作24h，预览不生效。

## 8. 定向验收、模型分工与发布

先做C-cases中的技能Retain/Release、同名法术来源撤销、Cha/Wis差异这三条危险路径，再接完整图。每种实际改变的分支选一次最小正反例和生产／原生调用；字段／类型存在与真实结算分别记层级。不得要求十族全部原生效果全场景证明；也不能只用“图存在”宣告支付、技能计数、清理通过。失败原件保留，不删断言／降阈值／缩覆盖换绿。C-cases是必要差异的行为expected，不是要求先建几十个新场景工程。

Gate仅新增内容编译＋本项可运行性及实际受影响的最小定向检测。**禁止全量检测／整套历史专项／全游戏验收作为门禁**。不启动游戏／UMM，不安装提供方，不把隔离检查写成玩家通过。未测完整UI／坐标移动／天气／存档控制器如实留给TEST玩家，不单独恢复无限HOLD队列。

A/C本轮Sol完成；B为单一生产负责人，可由攻坚档担任，冻结后的机械接线可用执行档；D须换独立agent／会话。辅助档不得决定来源／支付／技能计数终审或PASS。B从届时最新main的隔离分支开始，绝不把带未通过Speaker的主工程整树覆盖回main。主注册／目录文件串行整合，其他贡献修复不接管。实现／独立D后统一正常TEST出包，源码commit/push＋GitHub安装包／源码上传并下载核对，保留旧包；现在不抬版本、不实现、不关闭issue #10。

**冻结blocker：无。** 四提供方根是已定位、可执行的运行发布前提，当前并未声称已在本机注册／测试通过。只有真实发现API／提供方输入与此证据不符时才命名B-blocker附新证据，不用旧HOLD或缺历史日志阻断本包，也不能偷偷改变本合同来求绿。
