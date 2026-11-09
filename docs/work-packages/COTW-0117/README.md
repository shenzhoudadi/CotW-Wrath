# COTW-0117 Spirit Whisperer：A／C／B／D 分工与执行 prompt

更新：2026-11-09。认领：[issue #7](https://github.com/shenzhoudadi/CotW-Wrath/issues/7)。**A/C/B/D及最终整合复查已完成，源码已合入；待后续TEST安装包交付，不重复开发。**174条隔离检查37.59秒，合并编译8.138秒、0警告／0错误。实际范围、已知边界与未测项见[源码交付说明](../../spirit-whisperer/README.md)。下文保留原分工作为历史交接，不能把旧“B未实现”描述当当前状态。

这是完整 Wizard 变体包 F-0117，含它必需的最小 S-SPIRIT 隔离桥。一个变体统一交付，不拆成十次魂域发布。将本文件完整交给 agent，再指定“只执行 A／C／B／D”即可。

## 选题依据与避让

- 最新 GitHub main：`9256b4944b480c2cc87d256ac509ea65ab09e9fd`；已交付 TEST 为 0.14.68 Starseeker。这里的基线是选题时的快照，真正实施前重新读取最新 main 和认领。
- 已读本库最近提交、全部 open/closed PR #1–#5、三个分支；必要 fork `sxl1216/CotW-Wrath` 的 main／upstream-submissions、独有提交和 issue／PR 入口。未见本项实现或占用。两个历史分支分别是旧清理／0.14.66交接，不是新魂域实现。查重结论限于这些已检查入口，不声称其他公开模组均无实现。
- Speaker #6 为既有 B 的修复工作，**不接手、不改 Speaker 源码，也不借用其未通过 D 的支付／清理机制**。Vindictive Bastard／Menacing 为 sxl1216 已完成、用户接受对方实机结果的待合入工作，不重复开发。
- 当前允许剩余项中，Spirit Whisperer 有原生 Wizard／Shaman 作为身份基础；相比 Spirit Guide，它不先引入每日游荡魂域切换；相比 Investigator／Antipaladin 路线，它不先依赖未核实的职业主体。它仍是复杂移植，不宣称“原生引用接上就完成”。
- 未启动 Spirit Guide、Spiritualist、Relic Hunter、全 Oracle 引擎或其他 HOLD；用户排除项、已交付和原生／外部模组已覆盖项不恢复。

## 已从固定原作确认的范围

原作：`Holic75/KingmakerRebalance@1332fb0db844b7863f484ca978bea2349fe49769`。

- 主入口：`CallOfTheWild/Archetypes/SpiritWhisperer.cs`，已与 GitHub 固定文件核对 blob：`f211a6a6a8bbdbd7f4ee18c944df56ef306f406f`。
- 十族：Battle、Bones、Flame、Heavens、Life、Lore、Nature、Stone、Waves、Wind。A 从各族 `createSpiritWhispererSpirit` 的真实入口提取，不拿 Shaman 或 Spirit Guide 的总目录代替。
- 原作桥用 Wizard 等级、Intelligence 主属性、Charisma 次属性。**不是把所有 Wisdom／Charisma 字段一律改成 Intelligence**；哪些资源用次属性必须逐项追踪。
- 原作各族进展为 1 级 spirit ability、8 级 greater、20 级 manifestation；**Lore 在 8 级使用 true spirit ability 是明确例外**。不普遍额外授予 Shaman 的 true spirit 层，不发 spirit magic spells。
- L1 移除 school selection／arcane bond，加入 spirit selection／仅魔宠的 bond；L5/10/15/20 移除原 Wizard bonus feat，L5/10/15 加 spirit hex selection。**原作 hex 总入口还保留 Wizard bonus feat 作为可选项**，不能凭概述删掉。L20 没有再加一个 hex selection，不擅自补偿。当前 Wrath 的精确 GUID／冲突由 A 核对、C 冻结。
- 保留正常 Wizard 法术书和准备法术路径；不要因为原作描述“与魔宠沟通”而另造施法书／UI。必要巫术、能力、资源、manifestation 与魔宠资格属于同一个功能包。

## 路径与共有边界

- 本地项目：`D:\CodexProjects\CotW-Wrath`；生产工程：`outputs\CallOfTheWild.Wrath.Prototype`。根目录不是完整 git checkout。
- 发布 checkout：`D:\CodexProjects\CotW-Wrath\outputs\release066-current\github-checkout`。它用于最终串行合并／交付，不让多个 agent 同时改这个 checkout。
- 当前主工程里可能有 Speaker 尚未提交的实现。**不得把该整树或其中的候选 DLL 当作本项干净基线**。B 在 C 放行后从届时最新 GitHub main 建隔离分支／worktree；不复制旧整树，不覆盖另一位 B 的工作。
- 本地原作（仍存在时只读）：`D:\CodexProjects\CotW-Wrath\work\port01441\sources\Holic75__KingmakerRebalance\tree\CallOfTheWild\`。失效时只取固定 commit 的具体源文件，不下载完整旧 work。相关文件：`Archetypes/SpiritWhisperer.cs`、`Classes/ShamanSpirits/{SpiritsEngine,Battle,Bones,Flame,Heavens,Life,Lore,Nature,Stone,Waves,Wind}.cs` 和这些入口实际用到的 HexEngine／Hexes 源码。
- 先读 `AGENTS.md`、`docs/agent-handoff/HANDOFF.md`、`CURRENT_RELEASE.md`、`WORK_STATUS.md`。仅查 inventory 的 COTW-0117 对应项及本包依赖，不遍历全部旧库存／证书。
- 本机交付目录：`C:\Users\xuenc\agent-tools\spirit-whisperer-next\`。建议生产文件名为 `SpiritWhisperer0117.cs`、`SpiritClassBridge0117.cs`，名称可由 C/B 结合真实工程确定；**仅一份隔离桥**。不为将来可能使用它的 Spirit Guide 预做第二套机制。
- A/C 可只读并行；B 必须等 A 和 C 明确放行。`BlueprintRegistration.cs`、`ModuleCatalog.cs`、项目／本地化／包清单由 B 单一整合负责人串行修改，与 Speaker 的正式合并协调，不能互相复制覆盖。
- 禁止游戏／UMM启动或安装，禁止全量、全历史、全游戏门槛，禁止扩大依赖环境。合法本地程序集／模组仅作测试引用，不进公开仓库和安装包。Harmony 2.0.4、GUID／TypeId、提供方合同及许可沿用现状；游戏内自有文字英文。

## 推荐顺序与工作包

1. **Q-0117：十魂域／巫术及 Wrath 对应映射（A，辅助档）**。交付真实入口、分阶、资源与依赖，不实现。主要风险是把 Shaman 总图当作 Wizard 私有图，或漏掉 Lore／Life／bonus feat 例外。完成条件：十族逐项有可追溯映射和明确缺口，没有靠猜测填 GUID。
2. **S-SPIRIT-0117：隔离行为合同（C，攻坚档）**。先定 Wizard／Int／次要 Cha、巫术资格、资源和可变图所有权，生命周期与缺项策略。只冻结本项用到的共享合同，不实现通用全职业 SpiritsEngine。完成条件：A 的关键冲突被裁决，B可照做，expected具体，不能用“待确认”放行关键支付／清理分支。
3. **F-0117：完整变体实现（B，执行档接线，复杂机制用攻坚档）**。依赖 Q 和 S 冻结，完整十族、1/8/20及5/10/15入口、仅魔宠、原作保留选项、注册／图标／诊断。必要子能力一起做，不先上线少数魂域凑版本。优先私有有界克隆，可安全共享只读效果；不全局修改 Shaman 或伪装职业等级／HasFact。
4. **V-0117：本项最小定向闭环（B，C定边界）**。只检查改动的实际分支：代表性施放／扣费、Wizard和非Wizard混职、Int与Cha独立改变、先决负例、1/8/20切换、魂域巫术归属、移除和同名独立来源。不同机制需要不同代表例，**不要求十族所有原生效果重建全场景**。图字段检查与真实原生规则链分别记录。失败先看log，保留失败原件，修后只重跑受影响检查。
5. **D-0117／交付（D独立复查，B最终串行整合）**。D不改生产，复查候选与证据；具体 bug 回原 B 修复。新内容编译／可运行性及上述必要定向检查通过即可；完整 UI／存档控制器和玩家游戏验证未测就写未测，不能变成额外全量门槛。最后一次性 TEST 出包、相关源码／说明 commit／push、GitHub安装包与源码上传、下载核对，保留旧文件。统一版本届时确定，**现在不抢占0.14.69、不抬版本**。交付后更新状态并关闭 #7。

并行只用于 A 的十族资料分组、C只读审查既有模式、冻结后的纯目录草稿；不并行写同一桥或注册文件，不为并行重复实现机制。当前只登记和写 prompt，不派发实现。

## 模型按档位使用

以下是方便分工的大致分组，不是精确模型能力排名：

- **辅助／资料档**：Gemini 3.8 Flash；也可用 Composer 2.5、Kimi K2.8 跑固定搜集／清单。优先 A。
- **执行档**：Composer 2.5、Kimi K2.8，按冻结合同接线、文档、固定检查与打包；机制出现真实复杂冲突时由攻坚档承担 B 主责，避免凭猜测改合同。
- **攻坚／独立复查档**：Grok 4.7、Gemini 4 Argon、Gemini 3.1 Pro、GLM 5.3、Kimi K3.1 Pro，以及本会话 Sol。C和关键桥／支付／清理适合这一档；D换独立 agent／会话，默认可留给 Sol。
- 不要交给辅助档：机制定案、白名单／断言预期裁决、全局 Harmony 补丁、混职资格伪装、资源和来源清理终审、正式整合以及 PASS 宣告。A输出“找到对应”不能替代 C 合同或 B 行为验证。

## A prompt — 资料与真实映射

你只执行角色 A。先完整读取本文件、根规则与 issue #7，再只读固定原作和当前干净 main 的对应模块。本轮允许写交付目录，禁止生产／测试源码修改、push、版本／包操作及游戏／UMM启动。

目标：为 F-0117／S-SPIRIT-0117 生成可冻结的十族映射。逐个调用入口静态追踪 spirit ability／greater（Lore例外true）／manifestation／各自hex pool，记录原公式、等级／Int／次要Cha、动作／消耗、重复与撤销、提供方／资源／必要子图、对应 Wrath 身份。核对L1替换、L5/10/15/20、仅魔宠与保留bonus feat选项。不要把普通Shaman或Medium灵体混入，不新增spirit magic spells，不为找不到项造GUID。

交付到 `C:\Users\xuenc\agent-tools\spirit-whisperer-next\`：`A-source-scope.md`、`A-spirit-map.json`、`A-gaps.md`。map每族至少有原作file:line、实际可达能力／hex列表、层级、资源公式、候选Wrath根／组件、可变／只读边界、缺失原因。找到合法已存在实现就说明可复用版本和身份，不重新做。不要复制程序集、游戏蓝图包和整个旧环境进交付。

完成定义：十族、变体骨架和三次巫术入口均有实际证据，未知明确命名交C；不写运行PASS。A结束后直接交C，不再问用户是否开启攻坚。

## C prompt — 冻结最小 S-SPIRIT／F-0117 合同

你只执行角色 C。完整读取本文件、A三份产物、固定原作实际入口和当前生产模式；生产只读，交付目录可写，不实现、不push、不跑全量、不启游戏／UMM。

写 `C-frozen-contract.md` 和 `C-cases.json` 到 `C:\Users\xuenc\agent-tools\spirit-whisperer-next\`。逐项冻结：十族保留范围／Lore与Life例外、每个不同资源／DC分支的等级与Int／Cha输入、进展／魔宠／巫术与bonus feat资格、可变子图私有边界、支付／取消失败／重复事件、防双付、同名多来源所有权、升级／休息／撤销／重训恢复、缺提供方与晚加载原子注册、图标／诊断、GUID／TypeId约定。优先现有局部机制，禁止假装全局Shaman等级／HasFact，不借未通过复查的Speaker机制当正确基线。

冻结expected要有语义正反例：资源花一次、零资源不触发、非本魂域巫术不可选、非Wizard等级不增效、改变Int和Cha分别影响正确项、原生Shaman／其他职业不变、移除只清本来源。代表每种真正改变的分支，不设计全库或十族全场景矩阵。未知API先查本机真实定义，不允许因不存在某枚举改成作用范围不同的属性。

真缺证据用命名blocker限制该分支，明确缺什么；只有关键行为合同无空白才写“B可执行 yes + 条件”，不以“先做占位以后再补”放行。C只冻结，B才实现；A→C无需再问用户。

## B prompt — 唯一生产整合负责人

你只执行角色 B。完整读本文件和A/C产物。C未放行则只补读缺口、不实现；放行后，从届时最新GitHub main的隔离分支／worktree开始，记录基线和依赖来源，不从当前夹有未提交Speaker的工程整树复制。避免覆盖 #6、PR #5 或其他新认领。

完整实现F-0117及唯一S-SPIRIT局部桥。十族必须真实可用的入口／效果适配，不能拿统一加值占位凑数；保留原作特殊路径和bonus feat回退。只读原生效果可共享，需要改职业、属性、资源、前置、Buff或动作的图有界私有化；不复制整套引擎，不额外发魂域法术。你是注册、目录、项目和本地化文件唯一修改整合人，合入时与另一工作包串行。

实际行为变化只运行一次最小必要定向检查，调用真实生产链；至少覆盖支付／不支付、来源清理保留别家、属性和混职隔离、前置负例和代表等级切换。不要把“组件存在”或“方法存在”记录为实际行为PASS。**禁止全量测试、全历史／全游戏验收，不启动游戏／UMM，不改全局Harmony合同。**若现有宿主无条件先跑其他整套检查，选择本项独立入口，不把旧整套启动流程绑成本包门禁，也不重建旧工作环境。

输出目录同上：`B-progress.md`、`B-evidence.json`、时间戳日志。记录真正改变的文件、命令／实际耗时、当前源码／DLL／必要依赖指纹、检查层级和未测边界。日志失败原件保留，修真实问题，不降低阈值／删断言换绿。若需纠正与合同冲突的旧expected，写原因并补语义正反例。

交独立D；D发现具体问题由你修，D未通过不宣告完成。D通过后才统一TEST交付，版本在与当前main确认后确定；正常“出包”包括源码提交推送、GitHub包和源码资产上传并下载核对。不提交第三方DLL／游戏文件／私人日志。完成后更新WORK_STATUS／HANDOFF并关闭#7，保留实际未测边界；不因文档或同内容包装重跑测试。

## D prompt — 独立复查

你只执行角色 D，B之外的独立agent／会话。完整读本文件、A/C和B产物，针对实际候选diff与DLL指纹生产只读复查，写 `C:\Users\xuenc\agent-tools\spirit-whisperer-next\D-review.md`。

查十族／原作特殊层级与巫术有无漏换或占位；Shaman／Oracle原生图是否被改；Wizard／Int／次要Cha是否逐分支正确；魔宠和先决、费用、重复事件、Buff移除、多来源法术／巫术／附魔卸载是否真正安全；实际图标／注册诊断；正反例是否走真实生产分支、证据对应哪个DLL。特别防止只看组件字段就把双扣、错误CL加成、越权清理判PASS。

只列能定位到代码与触发条件的具体问题，交回原B修，自己不同时修改共享生产。已有同内容绿记录不重跑；改动或失败影响哪条才最小重查。不要临时要求全历史、十族全部原生效果／全存档UI证明，不把没跑游戏作为独立发布阻断。最终明确“可TEST交付”或命名具体blocker，区分离线规则链与玩家实机结果，确认issue／状态与真实交付一致。

## 可交接文件

完整本文本机副本：`C:\Users\xuenc\agent-tools\spirit-whisperer-next\HANDOFF-0117-ABCD-20261108.md`。

可直接粘贴的四份短入口：`A-prompt.txt`、`C-prompt.txt`、`B-prompt.txt`、`D-prompt.txt`，都先要求读取本文再执行一个角色。后续成果写同目录，代码与正式发布资料按正常repo结构整合，不把整个 agent-tools 上传。
