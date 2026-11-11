# 工作状态：先认领，避免重复

更新：2026-11-11。以本页和关联issue协调实际施工，不把旧inventory／HOLD描述当自动开工队列。

## 🚧 施工中／已认领，请勿重复开发

- **COTW-0109 Iroran Paladin** — [issue #11](https://github.com/shenzhoudadi/CotW-Wrath/issues/11)。A/C/B完成，D复查通过，可 TEST 交付。隔离分支 `feat/cotw-0109-iroran-paladin-b`。D-F01 标记等级与 D-F02 空手附魔已修，定向检查 105 条（31 重掷 + 74 注册），1.23 秒，exit 0。版本抬升、合并和 TEST 上传仍待出包。实机未测。[ABCD交接](docs/work-packages/COTW-0109/README.md)。

- **COTW-0046 Speaker for the Past** — [认领issue #6](https://github.com/shenzhoudadi/CotW-Wrath/issues/6)。负责人：仓库维护者／本会话协调，B为单一生产整合负责人。阶段：A/C完成，B v2已实现；**D复查 NEEDS_FIX，尚未出包**。[复查及修复要求](docs/work-packages/COTW-0046/D-review.md)：Rewind Time扣费／一次性销毁、奖励法术来源清理、Blood of Heroes豁免范围、Sacred Council检定范围。已有175条蓝图／组件断言不能覆盖这些实际分支，未声称玩家验收。[A/C/B/D分工](docs/work-packages/COTW-0046/README.md)。保留施工占用，不另起重复实现；0.14.68 Starseeker原交付不受影响。

## 本次0.14.69 TEST交付

- **COTW-0117 Spirit Whisperer** — [issue #7](https://github.com/shenzhoudadi/CotW-Wrath/issues/7)。A/C/B/D和最终整合复查完成，十魂域Wizard变体及唯一隔离桥已合入源码；174条隔离检查37.59秒，合并后编译8.138秒、0警告／0错误。保留D报告的选标／控制器等边界，不宣称玩家游戏通过。[交付范围与未测](docs/spirit-whisperer/README.md)。已纳入[0.14.69 Combined TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.69-Combined-TEST)，GitHub上传下载核对后关闭issue，不重复开发。

## 已完成开发／TEST交付

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
