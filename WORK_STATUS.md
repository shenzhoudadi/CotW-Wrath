# 工作状态：先认领，避免重复

更新：2026-11-08。以本页和关联issue协调实际施工，不把旧inventory／HOLD描述当自动开工队列。

## 🚧 施工中／已认领，请勿重复开发

- **COTW-0046 Speaker for the Past** — [认领issue #6](https://github.com/shenzhoudadi/CotW-Wrath/issues/6)。负责人：仓库维护者／本会话协调，B为单一生产整合负责人。阶段：A/C完成，B v2已实现；**D复查 NEEDS_FIX，尚未出包**。[复查及修复要求](docs/work-packages/COTW-0046/D-review.md)：Rewind Time扣费／一次性销毁、奖励法术来源清理、Blood of Heroes豁免范围、Sacred Council检定范围。已有175条蓝图／组件断言不能覆盖这些实际分支，未声称玩家验收。[A/C/B/D分工](docs/work-packages/COTW-0046/README.md)。保留施工占用，不另起重复实现；0.14.68 Starseeker原交付不受影响。

## 已完成开发／TEST交付

- **COTW-0081 Esoteric Starseeker** — [0.14.68 TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.68-Starseeker-TEST)。完整10星座×9环、双星座、15级Major及来源/资源生命周期；D-F01闭环，隔离托管定向536条断言通过。5项外部能力需合法提供方，任一缺失不半注册。[内容与未测边界](docs/starseeker/README.md)。实机UI、完整存档/重训等未验收。
- **COTW-0020 Havocker／S-KINETIC与Mimic展示更新** — [0.14.67 TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.67-Havocker-Mimic-TEST)。已有TEST实现作为基线，不重复开发；仍有明确场景/提供方等HOLD，见docs/havocker/README.md。不能把TEST交付写成全部历史边界已通过。

## 外部已完成实现／待合入，勿重复开发

- **COTW-0111 Vindictive Bastard、COTW-0473 Spirit-Bound Blade的Menacing部分** — sxl1216的[PR #5](https://github.com/shenzhoudadi/CotW-Wrath/pull/5)，提交 `71a9c2d6755dc6dd09f5cca5ade9058e5d5f4ab2`。对方报告已完成并经实机确认，用户已明确接受该测试结果，不追加重复验证；本机没有冒充亲测。PR当前尚未合入且有冲突，后续整合只做必要冲突／编译／包内DLL核对，不能将“待合入”误作待从零实现。PR #3／#4是同批旧入口，不能算成额外施工包。

## 正式稳定基线

- 0.14.66 Amnesiac反馈修复及更早已交付功能。旧正式附件保留。

## 后续约定

确定新项先查本页、提交、所有PR、分支及必要fork，确认他人是否已做完或在施工，再创建或复用认领。**别人正在做的范围不接手、不修改、不重复实现，只有用户明确要求才介入**；不因估计对方能力不足或进度慢而自动接管。别人已完成的优先复用。实现/验证/交付完成后归档并关闭对应issue；未测边界另记，不冒充亲测。新想法没有认领前不算已开工。
