# 工作状态：先认领，避免重复

更新：2026-11-08。以本页和关联issue协调实际施工，不把旧inventory／HOLD描述当自动开工队列。

## 🚧 施工中／已认领，请勿重复开发

- **COTW-0046 Speaker for the Past** — [认领issue #6](https://github.com/shenzhoudadi/CotW-Wrath/issues/6)。负责人：仓库维护者／本会话协调。阶段：A资料搜集、C机制合同准备；B尚未实现，不表示功能已经可用。[A/C/B/D分工与完整prompt](docs/work-packages/COTW-0046/README.md)。范围仅16项启示、Shaman/Wisdom隔离、18项奖励法表及完整变体；请勿另起重复实现或整套Oracle引擎。

## 已完成开发／TEST交付

- **COTW-0081 Esoteric Starseeker** — [0.14.68 TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.68-Starseeker-TEST)。完整10星座×9环、双星座、15级Major及来源/资源生命周期；D-F01闭环，隔离托管定向536条断言通过。5项外部能力需合法提供方，任一缺失不半注册。[内容与未测边界](docs/starseeker/README.md)。实机UI、完整存档/重训等未验收。
- **COTW-0020 Havocker／S-KINETIC与Mimic展示更新** — [0.14.67 TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.67-Havocker-Mimic-TEST)。已有TEST实现作为基线，不重复开发；仍有明确场景/提供方等HOLD，见docs/havocker/README.md。不能把TEST交付写成全部历史边界已通过。

## 正式稳定基线

- 0.14.66 Amnesiac反馈修复及更早已交付功能。旧正式附件保留。

## 后续约定

确定新项先查本页/开放issue；立即创建或复用施工认领，注明范围、负责人、阶段、基线和prompt链接。实现/验证/交付完成后移入已完成开发或正式交付，并关闭对应issue；未测边界另记，不能假装玩家验收。新想法没有认领前不算已开工。
