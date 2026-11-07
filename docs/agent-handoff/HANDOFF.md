# 当前交接：从简继续

- 当前预发布：**0.14.67 TEST**，含Havocker／S-KINETIC、Spellburn图标接线及Mimic展示移植。稳定正式版0.14.66保留。
- 2026-11-07：Harmony 编译基线改为 UMM 0.33.2 的 `Wrath_Data/Managed/UnityModManager/0Harmony.dll` 2.3.6.0（SHA `DF043F05C6E43602FA8E8F38F52A3A2CF962AAB7A65A9D85573EA25F01A63E80`）；运行时仍由 `Shared/HarmonyRuntimeCompatibility.cs` 接受已加载的兼容 2.x。旧 2.0.4.0／Managed 根路径及历史 .txt 描述为旧基线，不再适用。本轮未编译／未出包。
- 2026-11-07 协作约定：本仓库只写代码；测试包、上传代码与向上游提合并请求由用户完成，不迁移历史 Release 附件。
- 2026-11-08：`COTW-0473 Spirit-Bound Blade` 的 Menacing 已补完并经玩家确认正常；实现使用独立 Menacing 武器附魔、装备 feature 与夹击攻击加值规则，原 CotW 图标已嵌入。
- 2026-11-08：`COTW-0111 Vindictive Bastard` 已从 HOLD 清单移出并实现为独立职业。施法、Solo Tactics、Vindictive Smite 次数与目标限制、Gang Up、Swift Justice、Aura of Self-Righteousness、Ultimate Vindication 均已接入；玩家已确认存档、Smite 次数与目标限制、Gang Up 及 Ultimate Vindication 正常，本项完整实测通过。
- 2026-11-08：`COTW-0140 Relic Hunter` 按用户决定改为“暂且搁置”。该变体依赖 Occultist 的 implement/focus 体系，而 Occultist 基础职业及相关资源本身目前同样处于搁置状态，因此不列入当前 HOLD 开工清单。
- [测试包及源码](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.67-Havocker-Mimic-TEST)。本包主DLL SHA256：`3aad748c77197d5b1f1de5a253b1b8349f43509d7ef8bd4f36cac69edc03ad61`。
- 用户试玩反馈Havocker总体正常、仅缺Spellburn Buff图标；图标已接线，实际画面待再次确认，不据此宣称全部旧HOLD通过。
- 先读根AGENTS.md、CURRENT_RELEASE.md和[当前功能交接](../havocker/README.md)。不重做0.14.66，不恢复排除项，不启动其他HOLD。
- “出包”默认包括相关源码commit／push、GitHub附件上传及一次下载核对；TEST继续标测试版，不能只交本地ZIP。
- 新版本只改版本戳，编译15.514秒、包检查2.066秒；已有定向证据按功能代码复用，具体运行边界见当前交接。
- 当前测试源码及离线宿主边界补丁在tests/offline-havocker；合法游戏和第三方本地夹具不在公开仓库，不安装／启动UMM或游戏。

## 上游精选提交分支

- `main`：日常开发分支，跟踪 `origin/main`，仓库为 `https://github.com/sxl1216/CotW-Wrath.git`。
- `upstream-submissions`：长期保留的上游提交专用分支。原名 `feature/vindictive-menacing-handoff`，已于 2026-11-08 重命名；旧远程分支已删除。
- `upstream`：`https://github.com/shenzhoudadi/CotW-Wrath.git`。当前 `sxl1216` 对该仓库无直接写权限，直接 push 返回 HTTP 403，因此只能把精选分支推到 `origin/upstream-submissions` 后发起跨仓库 Pull Request。
- 当前专用分支提交 `71a9c2d` 以当时的 `upstream/main`（`15f3606`）为基线，只包含 Vindictive Bastard、Spirit-Bound Blade Menacing、Relic Hunter 搁置和 HOLD 清单调整；不含 Harmony 2.3.6 基线、构建脚本或无关测试改动。

后续 Agent 准备向上游提交时：先在 `main` 完成开发并推送，获取最新 `upstream/main`，然后从该基线重建 `upstream-submissions`，只摘取用户批准的文件与共享文件中的相关代码块。不得直接合并整个 `origin/main`，不得整体复制共享 JSON、注册文件或项目文件。提交前检查文件列表、`git diff --check`、JSON 解析、注册项、资源声明、本地化 key、572 项 inventory 和当前 HOLD 数量；确认无无关文件与格式噪声后，推送 `origin/upstream-submissions` 并创建指向 `shenzhoudadi/CotW-Wrath:main` 的 Pull Request。
