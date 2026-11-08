# COTW-0046 Speaker for the Past：施工认领与A／C／B／D交接

日期：2026-11-08。**已认领施工，禁止重复实现同一包。**
占用记录：https://github.com/shenzhoudadi/CotW-Wrath/issues/6
阶段：A资料／C机制合同准备中，B尚未实现，不代表变体已经可用。只启动这一项。

## 基线与目标

基于0.14.68 Starseeker TEST及其GitHub提交继续，开始前读取最新main和WORK_STATUS.md核对是否有人已接手。旧0.14.66是正式稳定版本，不能用它覆盖后续Havocker／Starseeker源码。测试包与正式包继续区分。

目标：**完整Shaman变体Speaker for the Past**，包括16项Ancestor／Time启示、等级／Wisdom适配、先决与资源／重掷来源隔离、私有奖励法表、技能与替换项、正常注册及图标。必要子能力属于同一个功能包，不按启示数量拆成16次发布。

优先理由：原生Shaman／Oracle均存在，不依赖尚未确认的HA Investigator或Antipaladin主体。新共享机制仅覆盖本项实际需要，不重建整个MysteryEngine，也不顺手实现Iroran三骰取次低等其他包规则。

## 公共路径和规则

- 本机：`D:\CodexProjects\CotW-Wrath`；实际生产工程 `outputs\CallOfTheWild.Wrath.Prototype`，其下 `src`／`tests`。独立GitHub克隆是仓库根src／tests，先核对布局。
- 先读根AGENTS.md、CURRENT_RELEASE.md、docs/agent-handoff/HANDOFF.md及WORK_STATUS.md。只查REMAINING／inventory的COTW-0046条目，不遍历全历史或恢复排除项。
- 固定原CotW：Holic75/KingmakerRebalance@1332fb0db844b7863f484ca978bea2349fe49769。
- 真实原文：`D:\CodexProjects\CotW-Wrath\work\port01441\sources\Holic75__KingmakerRebalance\tree\CallOfTheWild\Classes\Shaman.cs`，`createSpeakerForThePast()`约1094—1215行；SHA256 `bccafcb329d1cd9860700fc8e113af280f4c1f4c23c697b3b4d988bdb31c4dd5`。继续按实际调用定位MysteryEngine／HexEngine方法，禁止引用不存在的“镜像源码”或猜测GUID。
- 输出：`C:\Users\xuenc\agent-tools\speaker-next\`。只有B修改生产文件，A／C／D交文档／有界建议，不互相覆盖。
- 定向宿主优先复用现有可用路径，按新增内容选择最小必要检查；不强制新搭另一套宿主，不重建缺失旧环境。
- 禁止全量CotW／全wotr-testing／全历史检测；不启动游戏／UMM，不改玩家存档，不把隔离检查写成玩家通过。
- Clash保持rule、TUN关闭、7897；网络代理HTTP(S)／ALL_PROXY=`http://127.0.0.1:7897`，不改配置。
- 继续遵守Harmony、GUID／TypeId、提供方合同和许可；游戏内自有文字用英文。

## 必须一起覆盖的边界

原作启示池恰好16项：Aging Touch、Rewind Time、Blood of Heroes、Phantom Touch、Sacred Council、Spirit of the Warrior、Speed or Slow Time、Erase from Time、Spirit Shield、Storm of Souls、Temporal Celerity、Time Flicker、Time Hop、Time Sight、Spirit Walk、Ancestral Weapon。**Spirit Shield调用HexEngine.createAirBarrier，其余15项为MysteryEngine入口；不要因为只有15次MysteryEngine调用漏掉它。**

已读原作进展：1级移除shaman_familiar；4级移除wandering_shaman_spirits；6／14级移除wandering_hex_selection。1级加入Mysteries of the Past，4／6／12／14／20级各给一次启示选择。私有奖励法表为1—9环每环两项，共18项，并增加Knowledge World、Perception、Use Magic Device为职业技能；实际Wrath技能与原生Shaman结构由A/C核对，不凭同名硬接。原作还限制Beast of Ill Omen等魔宠依赖项，C需局部决定合法资格，禁止全局封禁Shaman巫术。

### Q-MAP-0046：原文与映射（A整理，C决定复用）

搜集16项启示、18项法术、进展替换与相关原生组件；每项注明真实GUID／类型、来源、等级／属性、资源／动作／DC、目标及效果调用链。只有名字不算语义等价；没有提供方不能编造。对应文件为A目录与后续私有目录文件，不默认新迁移法术。

### S-REVELATION-0046：必要的隔离桥（C冻结，B维护一份）

只适配本变体：Shaman有效等级、Wisdom、等级／其他先决、资源归属、启示授予和移除。保留原生Oracle的等级／Charisma与资源，混职不能互相扣费或叠错等级。复用只读效果时只克隆必须可变的子图，不全库扫描改写。

重掷／先攻按具体启示的原文和原生事件链裁定，只有语义确实一致才复用原生组件；不把AddReroll有一个累加器当作任何重掷规则都支持。不要为将来其他包建大而全的骰子框架。

### F-0046：完整变体（B统一实现）

包含16项池、等级进展、18项私有奖励法表、技能、魔宠／wandering来源替换、图标与英文说明、注册和生命周期。私有法表只作用于目标变体，不把奖励法术直接塞进全局Shaman／Cleric表，也不伪装Oracle等级或HasFact。

典型风险：普通Shaman和Oracle被污染；主属性未替换的子能力；一个启示多段子动作重复扣费；混职资源共享；低等级提前选取；重选／重训留下旧事实；奖励法表错误加入spirit magic书；法术／提供方缺失却半注册。

## 分工与顺序

**A → C → B → D**。A可与B只读熟悉注册模式并行，C放行前不实现共享机制。一个主开发即可，不强制全部模型同时跑。

- 辅助档：Gemini 3.8 Flash，适合A搜集和按固定命令执行，不定机制。
- 日常执行档：Composer 2.5／Kimi K2.8，适合B合同明确的实现／打包；复杂隔离可直接用攻坚档担任B。
- 攻坚／复查档：Grok 4.7、Gemini 4 Argon、Gemini 3.1 Pro、GLM 5.3、Kimi K3.1 Pro任选，负责C和独立D；D换一个agent／会话，不由B自己代替。这是任务分组，不是实测模型排名。

## 角色 A — 可直接执行

先读本文件，**只执行A**。允许只读上述工作区、固定原作、合法本地游戏／提供方源码或元数据；写 `A-source-scope.md`、`A-map16-and-spells18.json`、`A-gaps.md` 到输出目录。

从真实selection数组和法术表逐项提取，不照抄旧报告；分别记录16项与18项的数量、原表达式和候选当前根。找不到的条目精确报缺口，原生缺失不等于应新增迁移。静态匹配和运行验证分开标记，不写PASS。额外追踪MysteryEngine／HexEngine对等级／属性／重掷／资源的原作设置，列真实冲突供C决定。

不改生产、不全量检测、不启游戏／UMM、不push、不将问题扩到其他HOLD。原件路径失效时按固定commit读具体文件，不下载整个旧work。完成交C，无需再问用户是否开启攻坚模型。

## 角色 C — 可直接执行

读取本文件和A三份交付，**只执行C**。写 `C-frozen-contract.md`、`C-cases.json`，明确B可执行yes/no及条件。

逐项裁定：16项复用／私有适配边界；Shaman等级与Wisdom具体输入；技能与1／4／6／12／14／20进展；所有先决；资源／DC／动作／取消失败／重复事件；重掷与先攻取值和日志；Oracle／混职隔离；18项法术进哪本书及合法准备环级；魔宠依赖资格；休息／重训／移除；缺项／迟加载的原子注册。给实际expected，不以“待确认”关键空位放行。

真缺依赖就命名blocker，限制对应分支，不扩大成全项目环境重建；不得悄悄删难启示或替代奖励法术凑齐。只冻结本项，勿重开Havocker／Starseeker合同，不实现功能、不push、不要求额外全量门禁。

## 角色 B — 可直接执行

读取本文件、A与C交付，**只执行B**。C明确放行后实现完整F-0046及最小隔离桥；你是共享生产文件唯一整合负责人。建议文件SpeakerForThePast0046.cs与局部RevelationBridge，名字按实际工程决定，避免第二份基础机制。

16项均有身份／入口／前置／成长映射；对实际改变的机制选择一次必要定向正反例和真实原生路径：至少覆盖正确Shaman/Wisdom、Oracle不变、一个资源／一次性支付路径、重掷／先攻代表路径、先决／混职负例、法表和移除。共享代码若实际分支不同，要覆盖其差异；不为“效果全证明”重建整套场景或逐项开无限轮专项。

已有原生效果只读复用且未改时明确验证边界，不编造实机PASS。失败按log修，保留原件，不删断言、降阈值或悄悄缩覆盖换通过。输出B-progress.md／B-evidence.json／时间戳日志，含真正改动、运行命令／秒数、DLL／必要依赖指纹、未测项。

D复查后按本次实际交付统一出一个TEST版本，commit／push相关源码／说明，GitHub上传安装包与源码并下载回读，保留旧文件，不提交游戏文件／夹具／私人日志。完成后将WORK_STATUS.md移入已完成开发／TEST交付并关闭issue #6，注明未测实机边界；若仍blocked则保留施工中，不假装完成。

## 角色 D — 可直接执行

读取本文件、A/C和B交付及实际diff，**只执行独立D**，生产只读，写D-review.md。

核对完整16项和18项有没有漏换／冒名；C合同是否落实到真实子路径；Shaman/Wisdom、Oracle、混职、先决、资源与重掷是否局部隔离；图标是否接到实际Buff／能力；定向用例是否真的调用生产链且对应DLL；包／GitHub记录是否诚实。只列可定位复现的问题，不临时加全历史／全游戏门槛，不为了缺旧日志否决正常出包。

同内容证据不重跑，真实新修改只指出受影响最小复跑。最终说明是开发／TEST交付完成还是仍有具体blocker，未测控制器／存档UI交人用TEST检查。确认issue与WORK_STATUS.md一致，避免下一位重复开发。
