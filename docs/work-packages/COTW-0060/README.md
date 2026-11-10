# 当前状态：D3通过，0.14.70 TEST待GitHub交付

完整实现与D/D2修复均完成；最终候选`0ba589a`，320条隔离定向断言/15.00秒。见[D复查](D-review.md)、[证据摘要](VALIDATION.json)和[本包说明](../../releases/0.14.70-TEST.md)。真实提供方缺失时原子拒绝整包，不新移植替代法术。保留未测边界，不冒充玩家通过。原始A/C资料以下留作实现依据，不代表B仍未开始。

---
# Spirit Guide — 分工、A/C完成交接与B执行入口

2026-11-10；COTW-0060；[施工issue #10](https://github.com/shenzhoudadi/CotW-Wrath/issues/10)。**本轮Sol已亲自完成A和C；B未实现，D未复查，不是TEST或功能PASS。** 冻结合同`C-0060-20261110-r1`，B可执行yes，无冻结blocker。

## 基线和范围

main `91825bcd021a0e7439a5d8541c3a631f0c9a6d5d`／0.14.69 Combined TEST。已查本库最新提交、全部PR（含关闭）、4分支、唯一fork的3分支／独有提交／认领入口，未见0060实现／认领。保留Speaker #6原负责人修复；fork的雾效展示和Sacred Servant新修复不接管；已交付0117／0110／0111不重做。

只做完整Oracle Spirit Guide及其最小适配：十魂域、3级一族一巫术、4级起9项奖励法术、7级Base、15级Greater、Mystery技能替换、来源与资源生命周期。不拆成十次功能交付，不开其他HOLD、不恢复排除法术。

关键冻结：原作使用固定3级选择，**不加每日切换**；能力用Cha，HexEngine.secondary分支用Wis；Lore是Arcane Enlightenment而不是0117的Perfect Knowledge；Waves保留Fluid Magic、排除Beckoning Chill。90条法术已有77原生／8本包／5外部条目身份，外部4个唯一根不可缺项半注册。39是巫术池条目数，不是互不重复效果数。

## A/C真实完成内容

- **A，Sol完成**：`A-source-scope.md`、`A-source-index.json`、`A-spirit-map.json`、`A-api-and-provider-anchors.json`、`A-gaps.md`、`A-collection-evidence.json`。Oracle／Common／Hexes／十族共13份本地原作文件与固定GitHub blob逐一相等；提供方ID源另核对固定blob。提取90行环级／表达式／当前身份、39池字段及每族公式／方法位置。
- **C，Sol完成**：`C-frozen-contract.md`、`C-cases.json`。Q01–Q10已裁决，24个具体expected场景（均NOT RUN）。技能Retain/Release、临时知识原生Add/RemoveEntry签名已读实际程序集元数据；没有编译、执行运行检查、启动游戏／UMM或改生产源码。
- Native归档／源码匹配只证明身份及合同输入；不声称运行兼容、支付、休息、重训或实机通过。四个外部根是明确运行前提，并非本机已验证已安装。

## 推荐工作包和顺序

1. **S-0060-OWNERSHIP／高风险桥**：B先完成成对Mystery技能来源抑制、Oracle临时已知来源及Cha/Wis分路，先跑C03/C06/C10/C13最小正反例，再放行完整接线。依赖A/C，无需再问用户是否开攻坚。风险是技能计数下溢、清错其他法术来源、属性全替换；完成条件是实际生产调用有证据，不是字段存在。
2. **F-0060-SPIRITS／完整变体**：同一B接十族/39池/90表、进展、Lore参数选择、原子提供方门禁和晚加载、图标／诊断。依赖第1包合同及接口，不另写第二费用／知识系统。风险是漏改Buff/Area/child边、误授manifestation、外部空壳。完成条件按A-map身份逐一对应，特殊路径实际接线，没有随机属性占位。
3. **V-0060／必要定向闭环**：B只查实际改变的机制分支，结构与真实原生行为分开；同内容绿记录复用，真实失败修后只重跑受影响部分。C24不是允许全历史宿主跑一遍的借口。保存失败原件、命令／耗时、源／DLL／依赖指纹和未测项。
4. **D-0060／独立复查及TEST交付**：独立D生产只读，具体缺陷交原B；B修复后D再看受影响部分。通过后统一正常TEST出包、相关源码commit/push、GitHub包和源码上传及一次下载核对，保留旧包。只在真实交付后关闭#10；现在不抬版本。

可并行：B尚未开始时，资料／目录校对和只读API核对；冻结后的机械行表可分开起草。**不并行写共享生产文件或重复基础机制**。`BlueprintRegistration.cs`／`ModuleCatalog.cs`／项目／本地化由单一B整合人串行修改。构建和门禁脚本未经新授权不改。

## 模型分档

- A的后续机械搜集／固定检查可用Flash、Composer 2.5、Kimi K2.8；这次A已由Sol做完，不需要重做。
- C、S-0060-OWNERSHIP和复杂B交互适合Sol／Grok 4.7／Gemini4 Argon／Gemini3.1 Pro／GLM5.3／Kimi K3.1 Pro等攻坚档；C已经冻结，不交辅助档重定机制。
- 合同明确后的B目录接线可用Composer／Kimi K2.8，由同一个B负责人最终整合；D换独立攻坚agent／会话，可留给Sol。此分组非精确模型排名。
- 不交给辅助档：技能计数成对／读档终审、临时知识所有权、Harmony窄钩子范围、断言expected裁决、混职／提供方兼容终审及PASS宣告。

## 本机路径

交付目录：`C:\Users\xuenc\agent-tools\spirit-guide-next\`。完整本文为`HANDOFF-0060-ABCD-20261110.md`。

发布checkout：`D:\CodexProjects\CotW-Wrath\outputs\release066-current\github-checkout`；主工程原位置`D:\CodexProjects\CotW-Wrath\outputs\CallOfTheWild.Wrath.Prototype`可能含未提交Speaker，**不能当作干净基线整树复制**。B从届时最新main建隔离分支，依赖按已合法本地引用单份使用，不复制旧work。

固定原作本地副本：`work\port01441\sources\Holic75__KingmakerRebalance\tree\CallOfTheWild`；失效时只取固定commit的对应文件，不恢复整棵旧环境。当前源码／版权许可／Harmony2.0.4／GUID／TypeId约定保持。游戏内自有文字英文。

## B prompt（可直接粘贴）

只执行角色B。先读本文件、AGENTS／HANDOFF／CURRENT_RELEASE／WORK_STATUS／issue #10，以及A全部证据、C-frozen-contract和C-cases。合同C-0060-20261110-r1已放行，无需重做A/C或问用户；正式开始前复查最新main与认领，避免别人新提交重叠。

从干净main隔离实现完整F-0060，只用一份必要的共享适配。**先完成技能Retain/Release成对、同名法术来源安全和Cha/Wis分路代表用例**；这些危险路径未打通不要堆十族外层图。没有原生mystery_skills公共进展项，不能猜GUID；窄AddClassSkill钩子仅按C白名单和本角色配对，不伪装HasFact／职业等级。Oracle奖励与Lore选择均沿用原生临时知识账本，不能粗暴RemoveSpell；参考现有保护，不复制未通过D的Speaker方案。

十族/39池/90项身份按A-map，不照抄0117的Lore/Waves/属性；不可另造Frostbite／WallOfFire或删族求绿。缺任一必要提供方整包拒绝、可晚加载重试。保留一族一巫术、3/4/7/15进展、普通Oracle其他能力；Fluid Magic特例按C解释。依赖现有效果但改输入的子图要有界私有，图标接真实对象，不以统一buff占位。

只做新增内容编译／可运行性和实际改动的最小定向正反例；不跑全库／全历史／全游戏、不启动游戏或UMM、不安装第三方依赖、不改构建门禁。只读／字段检查不写成支付／清理行为PASS。C-cases按差异选实际必要验证，不机械扩成全场景矩阵。

输出同目录`B-progress.md`、`B-evidence.json`和时间戳日志，记录命令／实际秒数、源码／DLL／依赖指纹、检查层级、未测边界，保留所有失败原件。共享生产仅你一人改；不碰Speaker／fork修复／其他HOLD，不整树覆盖main。完成交独立D，不自己代替D。D放行后按用户正常TEST发布约定统一版本／GitHub交付，当前不先发包或关闭#10。

## D prompt（可直接粘贴）

只执行独立D，生产只读。读本文、A/C、B交付与实际diff／当前DLL。优先核对：技能计数两种激活顺序与成对卸载、同名永久／临时法术保留、能力Cha和巫术Wis分支、Lore参数选择及来源、Waves三项、原子提供方门禁；其次完整目录、图标、身份和既有模块不变。不能仅看组件存在判行为PASS，也不为“全效果证明”重建场景。

写同目录`D-review.md`：只列能定位的具体问题与最小复跑范围；没有新阻断时明确可TEST交付，保留未测UI／控制器／完整存档，不临时追加全量门槛。具体问题退原B，不同时改共享生产；同内容证据不重复跑。确认issue #10和WORK_STATUS与真正实现／交付一致。
