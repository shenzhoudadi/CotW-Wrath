# 当前交接：从简继续

- 新认领COTW-0060 Spirit Guide：[issue #10](https://github.com/shenzhoudadi/CotW-Wrath/issues/10)，Sol已亲自完成A和C，合同C-0060-20261110-r1放行B；尚未实现／检查／出包。见docs/work-packages/COTW-0060/README.md及同目录A/C产物，不重做A/C。十魂域Oracle版与0117不同，尤其Lore、Waves、Cha/Wis、Mystery技能和来源清理；不抢Speaker或fork其他修复。

- 当前预发布：[0.14.69 Combined TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.69-Combined-TEST)，含Spirit Whisperer和PR #5／#8外部贡献，保留此前功能。主DLL SHA256：`A638DA2FEC71B7E8915B600E60572A8CE280C987694DA1CB7CF31DD571D3E491`。旧附件与0.14.66正式版保留。
- COTW-0081已完成开发、D-F01修复与独立复查，本项隔离定向536条断言通过。角色A旧星座命名与C文档部分名称错误，以生产目录和docs/starseeker/README.md勘误为准。
- 保留0.14.67的Havocker与Mimic内容；正式稳定版0.14.66不变。TEST交付不等于完整玩家验收。
- COTW-0046 Speaker for the Past已完成A/C及B v2实现，D复查发现具体行为问题，**NEEDS_FIX，尚未新出包**：[issue #6](https://github.com/shenzhoudadi/CotW-Wrath/issues/6)。修复要求见docs/work-packages/COTW-0046/D-review.md，交既有B生产负责人修复后复查，不重复开发。0.14.69 TEST仍不包含Speaker。
- COTW-0117 Spirit Whisperer：[issue #7](https://github.com/shenzhoudadi/CotW-Wrath/issues/7)，A/C/B/D及最终整合复查完成，已纳入0.14.69 TEST，不重复开发。十魂域Wizard变体与唯一最小S-SPIRIT桥；沿用174条／37.59秒隔离记录，合并最新main后编译0警告／0错误、8.138秒。见docs/spirit-whisperer/README.md中的真实范围、已知选标边界及未测项；没有玩家游戏PASS，不开Spirit Guide，不覆盖Speaker修复。
- 2026-11-09：sxl1216的Vindictive Bastard／Menacing [PR #5](https://github.com/shenzhoudadi/CotW-Wrath/pull/5)及Sacred Servant／Live On [PR #8](https://github.com/shenzhoudadi/CotW-Wrath/pull/8)已合入源码。用户认可贡献者测试，不重复实机／专项；仅解决冲突、合并编译与DLL／图标核对。此前源码阶段未出包；现已统一纳入0.14.69 TEST，旧0.14.68附件不变。见docs/contributions/20261109-integration.md。不要重新开发这些内容。
- 接手先读AGENTS、CURRENT_RELEASE、WORK_STATUS；状态板优先于历史库存的旧进度描述，不重复已交付功能，不自动恢复排除项。
- 出包包含commit/push、GitHub附件和一次下载核对；新内容先登记施工，完成必要验证与交付后归档并关闭认领issue。只做本轮必要检查，不启动游戏/UMM，不跑全量。
