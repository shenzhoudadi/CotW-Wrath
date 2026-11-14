# 当前交接：从简继续

- **COTW-0070 Cryptid Scholar** — [0.14.73 Cryptid Scholar TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.73-Cryptid-Scholar-TEST)已交付，4/4附件下载一致。D3复查通过，244条本项定向及D3复跑5/5。真实HA、实机、存读档和重训未测。[交接](../work-packages/COTW-0070/README.md) · [验证](../work-packages/COTW-0070/RELEASE-VERIFY.md)。

- **COTW-0068 Psychic Detective** — [issue #12](https://github.com/shenzhoudadi/CotW-Wrath/issues/12)。A/C/B 与同会话 D2 已完成，[0.14.72 Psychic Detective TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.72-Psychic-Detective-TEST)已交付。发布核查修正 HA 晚加载。306 条定向／14.778 秒，4/4 附件下载一致。真实 HA／TTT、实机、存读档和重训仍未测。[交接与证据](../work-packages/COTW-0068/README.md)。

- **COTW-0109 Iroran Paladin** — [issue #11](https://github.com/shenzhoudadi/CotW-Wrath/issues/11)。A/C/B/D已完成，[0.14.71 Iroran Paladin TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.71-Iroran-Paladin-TEST)已交付。105条隔离定向／1.23秒，4/4附件下载核对一致。实机、存档和升级界面等未测。[交接与证据](../work-packages/COTW-0109/README.md)。不重复开发。

- **COTW-0060 Spirit Guide** — [issue #10](https://github.com/shenzhoudadi/CotW-Wrath/issues/10)。已完成开发及D3复查；320条定向/15.00秒，0.14.70 TEST已上传GitHub并完成4/4下载核对。四外部根原子门禁，缺失不激活；实机和控制器等未测。[交接与证据](../work-packages/COTW-0060/README.md)。不重复开发。

- 历史预发布：[0.14.69 Combined TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.69-Combined-TEST)，含Spirit Whisperer和PR #5／#8外部贡献，保留此前功能。主DLL SHA256：`A638DA2FEC71B7E8915B600E60572A8CE280C987694DA1CB7CF31DD571D3E491`。旧附件与0.14.66正式版保留。
- COTW-0081已完成开发、D-F01修复与独立复查，本项隔离定向536条断言通过。角色A旧星座命名与C文档部分名称错误，以生产目录和docs/starseeker/README.md勘误为准。
- 保留0.14.67的Havocker与Mimic内容；正式稳定版0.14.66不变。TEST交付不等于完整玩家验收。
- COTW-0046 Speaker for the Past：A/C/B/D 各做过一轮，D 复查判定 **NEEDS_FIX**：[issue #6](https://github.com/shenzhoudadi/CotW-Wrath/issues/6)（仍 open 占用）。**2026-11-14 核实：B 的候选源码从未进入任何提交，已随 b55fa17 退役 tests/ 与 work-package 文档一起丢失，本机无副本；重做需从 B 开始。** 遗留文档只在 git 历史：`git show 9256b49:docs/work-packages/COTW-0046/`（D-review.md 含 4 项必修）。0.14.69 TEST 及后续包均不含 Speaker。
- 2026-11-14 inventory 分组收口：`docs/agent-handoff/inventory.json` 中原 60 条「暂且正常做的东西」，除 **COTW-0046**（仍要从 B 重做）外 **59 条改为「暂且搁置的东西」**；改后为 84 条搁置、仅 COTW-0046 仍是正常选题。**搁置不等于排除**：条目与规格仍留在 inventory，用户点名即可重启。同批校正 18 条过期状态字段（只改 `status`／`currentProfile`／`ecosystemProfile`，未动 `note` 与历史记录）：0020 Havocker、0095 Insinuator、0293 区域预览、0296 武器特性、0576 Allied Spellcaster、0582 Kinetic Knight Blade Rush、0598 Animate Dead、0600 Versatile Performance、0610 徒手副手记为已交付并注明版本；**0288 Wild Armor 记为部分覆盖**（0.14.74 只交付 COTW-01465 附魔本体，未给特定独特护甲附加、也没有全物品开关）；0094 Dread Vanguard、0295 双手军用武器、0571 Fiery Body 飞行、0573 法术射程、0614–0617 Beast Shape I–IV 记为未实现。顶层 `snapshotVersion` 由 0.14.66 改为 **0.14.104**（与 csproj／manifest 一致），`previousDeliveredVersion` 未动；`allowedRemainingIds` 由 8 项收窄为 **仅 COTW-0046**（0020／0060／0068／0070／0095／0109 已交付、0098 已排除），`authority` 文案同步改为「以 allowedRemainingIds 与显式排除为准」，不再写死项数。当前代码 0.14.104，未打包、未 commit。
- COTW-0117 Spirit Whisperer：[issue #7](https://github.com/shenzhoudadi/CotW-Wrath/issues/7)，A/C/B/D及最终整合复查完成，已纳入0.14.69 TEST，不重复开发。十魂域Wizard变体与唯一最小S-SPIRIT桥；沿用174条／37.59秒隔离记录，合并最新main后编译0警告／0错误、8.138秒。见docs/spirit-whisperer/README.md中的真实范围、已知选标边界及未测项；没有玩家游戏PASS，不开Spirit Guide，不覆盖Speaker修复。
- 2026-11-09：sxl1216的Vindictive Bastard／Menacing [PR #5](https://github.com/shenzhoudadi/CotW-Wrath/pull/5)及Sacred Servant／Live On [PR #8](https://github.com/shenzhoudadi/CotW-Wrath/pull/8)已合入源码。用户认可贡献者测试，不重复实机／专项；仅解决冲突、合并编译与DLL／图标核对。此前源码阶段未出包；现已统一纳入0.14.69 TEST，旧0.14.68附件不变。见docs/contributions/20261109-integration.md。不要重新开发这些内容。
- 接手先读AGENTS、CURRENT_RELEASE、本页与 `docs/agent-handoff/inventory.json`（`WORK_STATUS.md` 已于 `b55fa17` 退役，仅存 git 历史 `git show b55fa17^:WORK_STATUS.md`）；状态板优先于历史库存的旧进度描述，不重复已交付功能，不自动恢复排除项。
- 出包包含commit/push、GitHub附件和一次下载核对；新内容先登记施工，完成必要验证与交付后归档并关闭认领issue。只做本轮必要检查，不启动游戏/UMM，不跑全量。

## sxl1216 分支协作与精选提交边界


- `main`：日常开发分支，跟踪 `origin/main`，仓库为 `https://github.com/sxl1216/CotW-Wrath.git`。
- `upstream-submissions`：长期保留的上游提交专用分支。原名 `feature/vindictive-menacing-handoff`，已于 2026-11-08 重命名；旧远程分支已删除。
- `upstream`：`https://github.com/shenzhoudadi/CotW-Wrath.git`。当前 `sxl1216` 对该仓库无直接写权限，直接 push 返回 HTTP 403，因此只能把精选分支推到 `origin/upstream-submissions` 后发起跨仓库 Pull Request。
- 当前专用分支提交 `71a9c2d` 以当时的 `upstream/main`（`15f3606`）为基线，只包含 Vindictive Bastard、Spirit-Bound Blade Menacing、Relic Hunter 搁置和 HOLD 清单调整；不含 Harmony 2.3.6 基线、构建脚本或无关测试改动。

后续 Agent 准备向上游提交时：先在 `main` 完成开发并推送，获取最新 `upstream/main`，然后从该基线重建 `upstream-submissions`，只摘取用户批准的文件与共享文件中的相关代码块。不得直接合并整个 `origin/main`，不得整体复制共享 JSON、注册文件或项目文件。提交前检查文件列表、`git diff --check`、JSON 解析、注册项、资源声明、本地化 key、572 项 inventory 和当前 HOLD 数量；确认无无关文件与格式噪声后，推送 `origin/upstream-submissions` 并创建指向 `shenzhoudadi/CotW-Wrath:main` 的 Pull Request。

### Git 协作安全边界

- 用户只是询问同步方式、分支影响或协作规划时，只提供答复，禁止直接运行 Git 命令。必须等用户明确要求执行具体操作。
- `shenzhoudadi/CotW-Wrath` 也在持续移植新内容。同步对方更新时，在 `main` 上获取并正常合并 `upstream/main`，解决冲突、验证后再推送 `origin/main`；这与上游精选 PR 分支是两套独立流程。
- `upstream-submissions` 不参与日常同步。未经用户明确授权，不得对其执行 hard reset、force push、删除、重建或历史改写；旧 PR 未结束时必须保留对应提交。
- 重建精选分支前必须同时获取 `origin` 与 `upstream`，核对 `origin/upstream-submissions`、本地分支、旧 PR 状态和最新 `upstream/main`。远程分支含有本地缺失提交时，先保护并恢复远程内容，不能盲目覆盖。
- 如确需改写精选分支，必须由用户明确授权，并只使用 `--force-with-lease`。遇到 stale info、HTTP 403、权限或安全检查失败时立即停止并如实报告。
- 2026-11-08 曾发生一次误操作：用户只是询问同步影响时，命令被错误执行，本地 `upstream-submissions` 被重置到 `15f3606`；向远程的改写推送因 stale info 被拒绝，因此远程精选提交 `71a9c2d` 未被覆盖。当前工作区已切回 `main`。后续不得重复这种“询问即执行”的行为。
