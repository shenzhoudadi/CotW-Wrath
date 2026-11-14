# 当前交接：从简继续

- **COTW-0046 Speaker for the Past** — [0.14.74 Speaker for the Past TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.74-Speaker-for-the-Past-TEST) 已交付，211 条隔离定向与包内 DLL 复跑通过，4/4 附件下载一致。[验证说明](../work-packages/COTW-0046/RELEASE-VERIFY.md)。

- **COTW-0070 Cryptid Scholar** — [0.14.73 Cryptid Scholar TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.73-Cryptid-Scholar-TEST)已交付，4/4附件下载一致。D3复查通过，244条本项定向及D3复跑5/5。真实HA、实机、存读档和重训未测。[交接](../work-packages/COTW-0070/README.md) · [验证](../work-packages/COTW-0070/RELEASE-VERIFY.md)。

- **COTW-0068 Psychic Detective** — [issue #12](https://github.com/shenzhoudadi/CotW-Wrath/issues/12)。A/C/B 与同会话 D2 已完成，[0.14.72 Psychic Detective TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.72-Psychic-Detective-TEST)已交付。发布核查修正 HA 晚加载。306 条定向／14.778 秒，4/4 附件下载一致。真实 HA／TTT、实机、存读档和重训仍未测。[交接与证据](../work-packages/COTW-0068/README.md)。

- **COTW-0109 Iroran Paladin** — [issue #11](https://github.com/shenzhoudadi/CotW-Wrath/issues/11)。A/C/B/D已完成，[0.14.71 Iroran Paladin TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.71-Iroran-Paladin-TEST)已交付。105条隔离定向／1.23秒，4/4附件下载核对一致。实机、存档和升级界面等未测。[交接与证据](../work-packages/COTW-0109/README.md)。不重复开发。

- **COTW-0060 Spirit Guide** — [issue #10](https://github.com/shenzhoudadi/CotW-Wrath/issues/10)。已完成开发及D3复查；320条定向/15.00秒，0.14.70 TEST已上传GitHub并完成4/4下载核对。四外部根原子门禁，缺失不激活；实机和控制器等未测。[交接与证据](../work-packages/COTW-0060/README.md)。不重复开发。

- 历史预发布：[0.14.69 Combined TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.69-Combined-TEST)，含Spirit Whisperer和PR #5／#8外部贡献，保留此前功能。主DLL SHA256：`A638DA2FEC71B7E8915B600E60572A8CE280C987694DA1CB7CF31DD571D3E491`。旧附件与0.14.66正式版保留。
- COTW-0081已完成开发、D-F01修复与独立复查，本项隔离定向536条断言通过。角色A旧星座命名与C文档部分名称错误，以生产目录和docs/starseeker/README.md勘误为准。
- 保留0.14.67的Havocker与Mimic内容；正式稳定版0.14.66不变。TEST交付不等于完整玩家验收。
- COTW-0117 Spirit Whisperer：[issue #7](https://github.com/shenzhoudadi/CotW-Wrath/issues/7)，A/C/B/D及最终整合复查完成，已纳入0.14.69 TEST，不重复开发。十魂域Wizard变体与唯一最小S-SPIRIT桥；沿用174条／37.59秒隔离记录，合并最新main后编译0警告／0错误、8.138秒。见docs/spirit-whisperer/README.md中的真实范围、已知选标边界及未测项；没有玩家游戏PASS，不开Spirit Guide，不覆盖Speaker修复。
- 2026-11-09：sxl1216的Vindictive Bastard／Menacing [PR #5](https://github.com/shenzhoudadi/CotW-Wrath/pull/5)及Sacred Servant／Live On [PR #8](https://github.com/shenzhoudadi/CotW-Wrath/pull/8)已合入源码。用户认可贡献者测试，不重复实机／专项；仅解决冲突、合并编译与DLL／图标核对。此前源码阶段未出包；现已统一纳入0.14.69 TEST，旧0.14.68附件不变。见docs/contributions/20261109-integration.md。不要重新开发这些内容。
- 接手先读AGENTS、CURRENT_RELEASE、WORK_STATUS；状态板优先于历史库存的旧进度描述，不重复已交付功能，不自动恢复排除项。
- 出包包含commit/push、GitHub附件和一次下载核对；新内容先登记施工，完成必要验证与交付后归档并关闭认领issue。只做本轮必要检查，不启动游戏/UMM，不跑全量。
