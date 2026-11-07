# 当前交接：从简继续

- 当前预发布：**0.14.67 TEST**，含Havocker／S-KINETIC、Spellburn图标接线及Mimic展示移植。稳定正式版0.14.66保留。
- 2026-11-08：`COTW-0473 Spirit-Bound Blade` 的 Menacing 已补完并经玩家确认正常；实现使用独立 Menacing 武器附魔、装备 feature 与夹击攻击加值规则，原 CotW 图标已嵌入。
- 2026-11-08：`COTW-0111 Vindictive Bastard` 已从 HOLD 清单移出并实现为独立职业。施法、Solo Tactics、Vindictive Smite 次数与目标限制、Gang Up、Swift Justice、Aura of Self-Righteousness、Ultimate Vindication 均已接入；玩家已确认存档、Smite 次数与目标限制、Gang Up 及 Ultimate Vindication 正常，本项完整实测通过。
- 2026-11-08：`COTW-0140 Relic Hunter` 按用户决定改为“暂且搁置”。该变体依赖 Occultist 的 implement/focus 体系，而 Occultist 基础职业及相关资源本身目前同样处于搁置状态，因此不列入当前 HOLD 开工清单。
- [测试包及源码](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.67-Havocker-Mimic-TEST)。本包主DLL SHA256：`3aad748c77197d5b1f1de5a253b1b8349f43509d7ef8bd4f36cac69edc03ad61`。
- 用户试玩反馈Havocker总体正常、仅缺Spellburn Buff图标；图标已接线，实际画面待再次确认，不据此宣称全部旧HOLD通过。
- 先读根AGENTS.md、CURRENT_RELEASE.md和[当前功能交接](../havocker/README.md)。不重做0.14.66，不恢复排除项，不启动其他HOLD。
- “出包”默认包括相关源码commit／push、GitHub附件上传及一次下载核对；TEST继续标测试版，不能只交本地ZIP。
- 新版本只改版本戳，编译15.514秒、包检查2.066秒；已有定向证据按功能代码复用，具体运行边界见当前交接。
- 当前测试源码及离线宿主边界补丁在tests/offline-havocker；合法游戏和第三方本地夹具不在公开仓库，不安装／启动UMM或游戏。
