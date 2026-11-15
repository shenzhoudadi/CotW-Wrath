# 工作状态：先认领，避免重复

更新：2026-11-15。以本页和关联issue协调实际施工，不把旧inventory／HOLD描述当自动开工队列。

## 🚧 施工中／已认领，请勿重复开发

- **COTW-0099 Occultist 基础职业** — [issue #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15)。**A/C 完成；B 首切片（Abjuration）已实现，开发态未发布**，后续按完整学派分包。范围为八学派、四 Panoply、Mental Focus、独立六环法书和职业成长。按用户 2026-11-15 指令本会话执行 B 首切片：O-S01 权威状态机、O-S02 法器/配额/来源账本、Abjuration 18 根法术目录及原生适配器；`SlicePublished=false`，未注册职业/法书/成长，共享注册链未改。B 单一整合负责人仍待维护者指定。按用户 2026-11-15 23:59 指令由本会话自行复查 D：**NEEDS_FIX（自查，不满足分工文档「D 换人」，不是独立签字）**，不得据首切片验收或发布；正式独立 D 待维护者另指派执行者。基线主线 `939e04d` / 0.14.74 TEST，工作分支 `feat/cotw-0099-occultist`。主工程编译 0 警告/0 错误（首次 3.863 秒）；一次隔离状态模型定向检查 279 条断言 PASS / 2.837 秒，19 条用例按实际状态记录（MF07、MF11 未运行；SP05、SP06 部分覆盖），**原生 Wrath 入口未测**；包内 DLL 6/6 新类型核对通过。打包与安装包上传未运行（本机仅 PowerShell 5.1，build.ps1 要求 7.4），无新发布。 [A 盘点](docs/work-packages/COTW-0099/A-occultist-map.md) · [C 合同](docs/work-packages/COTW-0099/C-occultist-contract.md) · [B 首切片交接与证据](docs/work-packages/COTW-0099/B-abjuration-slice.md) · [断言报告](docs/work-packages/COTW-0099/B-slice-report.json) · [D 自查 NEEDS_FIX](docs/work-packages/COTW-0099/D-occultist-review.md) · [失败断言](docs/work-packages/COTW-0099/D-review-assertions.json) · [分工](docs/work-packages/OCCULT-BASE-CLASSES/OCCULTIST.md)。
- **COTW-0085 Spiritualist 基础职业** — [issue #16](https://github.com/shenzhoudadi/CotW-Wrath/issues/16)。A/C 准备，生产尚未开始；协调方为维护者／本会话，B 单一整合负责人待合同冻结后指定。基线 main `939e04d` / 0.14.74 TEST。[独立分工与接手](docs/work-packages/OCCULT-BASE-CLASSES/SPIRITUALIST.md)。

## 本次0.14.69 TEST交付

- **COTW-0117 Spirit Whisperer** — [issue #7](https://github.com/shenzhoudadi/CotW-Wrath/issues/7)。A/C/B/D和最终整合复查完成，十魂域Wizard变体及唯一隔离桥已合入源码；174条隔离检查37.59秒，合并后编译8.138秒、0警告／0错误。保留D报告的选标／控制器等边界，不宣称玩家游戏通过。[交付范围与未测](docs/spirit-whisperer/README.md)。已纳入[0.14.69 Combined TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.69-Combined-TEST)，GitHub上传下载核对后关闭issue，不重复开发。

## 已完成开发／TEST交付

- **COTW-0046 Speaker for the Past** — [0.14.74 Speaker for the Past TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.74-Speaker-for-the-Past-TEST) 已交付。211 条定向及包内 DLL 复跑通过，4/4 附件下载核对一致。[验证与未测范围](docs/work-packages/COTW-0046/RELEASE-VERIFY.md)。

- **COTW-0070 Cryptid Scholar** — [issue #13](https://github.com/shenzhoudadi/CotW-Wrath/issues/13)。D3复查通过，[0.14.73 Cryptid Scholar TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.73-Cryptid-Scholar-TEST)已交付，4/4附件下载一致。244条本项隔离定向及D3复跑5/5。真实HA、实机、存读档和重训未测。[交接与验证](docs/work-packages/COTW-0070/README.md)。

- **COTW-0068 Psychic Detective** — [issue #12](https://github.com/shenzhoudadi/CotW-Wrath/issues/12)。A/C/B 与同会话 D2 已完成，[0.14.72 Psychic Detective TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.72-Psychic-Detective-TEST)已交付。发布核查修正 HA 晚加载。306 条定向／14.778 秒，4/4 附件下载一致。真实 HA／TTT、实机、存读档和重训仍未测。[交接与证据](docs/work-packages/COTW-0068/README.md)。

- **COTW-0109 Iroran Paladin** — [issue #11](https://github.com/shenzhoudadi/CotW-Wrath/issues/11)。A/C/B/D已完成，[0.14.71 Iroran Paladin TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.71-Iroran-Paladin-TEST)已交付。105条隔离定向／1.23秒，4/4附件下载核对一致。实机、存档和升级界面等未测。[交接与证据](docs/work-packages/COTW-0109/README.md)。不重复开发。

- **COTW-0060 Spirit Guide** — [issue #10](https://github.com/shenzhoudadi/CotW-Wrath/issues/10)。已完成开发及D3复查；320条定向/15.00秒，[0.14.70 Spirit Guide TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.70-Spirit-Guide-TEST)已交付，4/4附件下载核对一致。四外部根原子门禁，缺失不激活；实机和控制器等未测。[交接与证据](docs/work-packages/COTW-0060/README.md)。不重复开发。

- **COTW-0081 Esoteric Starseeker** — [0.14.68 TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.68-Starseeker-TEST)。完整10星座×9环、双星座、15级Major及来源/资源生命周期；D-F01闭环，隔离托管定向536条断言通过。5项外部能力需合法提供方，任一缺失不半注册。[内容与未测边界](docs/starseeker/README.md)。实机UI、完整存档/重训等未验收。
- **COTW-0020 Havocker／S-KINETIC与Mimic展示更新** — [0.14.67 TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.67-Havocker-Mimic-TEST)。已有TEST实现作为基线，不重复开发；仍有明确场景/提供方等HOLD，见docs/havocker/README.md。不能把TEST交付写成全部历史边界已通过。

## 外部贡献已纳入0.14.69 TEST，勿重复开发

- **COTW-0111 Vindictive Bastard、COTW-0473 Spirit-Bound Blade的Menacing部分** — sxl1216的[PR #5](https://github.com/shenzhoudadi/CotW-Wrath/pull/5)，原提交 `71a9c2d6755dc6dd09f5cca5ade9058e5d5f4ab2`。2026-11-09已整合，保留当前Starseeker／认领文档并解决旧分支冲突。对方实机测试由用户接受，本机未重复实机／专项测试。PR #3／#4为同批旧入口，不算其他施工包。
- **COTW-0110 Sacred Servant及Live On原生mercy复用** — sxl1216的[PR #8](https://github.com/shenzhoudadi/CotW-Wrath/pull/8)，原提交 `bcd27389616f3abb5539dd00c65fc5c0c7cda694`。2026-11-09已整合，与#5同时保留注册／状态入口；接受用户认可的贡献者测试，只做静态与合并编译检查。没有恢复Sacred Servant重复开发队列，也没有合入fork上无关的构建／测试环境改动。
- 两份合并后主工程编译0警告／0错误，5.138秒；DLL新类型与Menacing嵌入图标已核对。[本次整合记录](docs/contributions/20261109-integration.md)。这些内容现均纳入0.14.69 Combined TEST；旧0.14.68附件未改变。

## 正式稳定基线

- 0.14.66 Amnesiac反馈修复及更早已交付功能。旧正式附件保留。

## 后续约定

确定新项先查本页、提交、所有PR、分支及必要fork，确认他人是否已做完或在施工，再创建或复用认领。**别人正在做的范围不接手、不修改、不重复实现，只有用户明确要求才介入**；不因估计对方能力不足或进度慢而自动接管。别人已完成的优先复用。实现/验证/交付完成后归档并关闭对应issue；未测边界另记，不冒充亲测。新想法没有认领前不算已开工。
