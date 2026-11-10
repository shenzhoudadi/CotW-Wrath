# 当前交接：从简继续

- 当前预发布：[0.14.69 Combined TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.69-Combined-TEST)，含Spirit Whisperer和PR #5／#8外部贡献，保留此前功能。主DLL SHA256：`A638DA2FEC71B7E8915B600E60572A8CE280C987694DA1CB7CF31DD571D3E491`。旧附件与0.14.66正式版保留。
- COTW-0081已完成开发、D-F01修复与独立复查，本项隔离定向536条断言通过。角色A旧星座命名与C文档部分名称错误，以生产目录和docs/starseeker/README.md勘误为准。
- 保留0.14.67的Havocker与Mimic内容；正式稳定版0.14.66不变。TEST交付不等于完整玩家验收。
- COTW-0046 Speaker for the Past已完成A/C及B v2实现，D复查发现具体行为问题，**NEEDS_FIX，尚未新出包**：[issue #6](https://github.com/shenzhoudadi/CotW-Wrath/issues/6)。修复要求见docs/work-packages/COTW-0046/D-review.md，交既有B生产负责人修复后复查，不重复开发。0.14.69 TEST仍不包含Speaker。
- COTW-0117 Spirit Whisperer：[issue #7](https://github.com/shenzhoudadi/CotW-Wrath/issues/7)，A/C/B/D及最终整合复查完成，已纳入0.14.69 TEST，不重复开发。十魂域Wizard变体与唯一最小S-SPIRIT桥；沿用174条／37.59秒隔离记录，合并最新main后编译0警告／0错误、8.138秒。见docs/spirit-whisperer/README.md中的真实范围、已知选标边界及未测项；没有玩家游戏PASS，不开Spirit Guide，不覆盖Speaker修复。
- 2026-11-10：**0117 后续修复已获玩家实机确认**（hex 专属列表、双 Touch of Flames、L20 Manifestation 三个不同超魔、开启超魔后法术全暗均正常）。两处根因是 Owlcat 原版数据缺陷，按用户决定连原生一起修；另一处是内置 MediumClass 的 AutoMetamagic 补丁异常。细节见 docs/spirit-whisperer/README.md「原生游戏缺陷修复」节与 CURRENT_RELEASE.md。**注意本页第3行主DLL SHA 仍是旧包 `A638DA2F…`**，最新本地包为 `d978d92e…`／SHA256 `0C1EDFCA…`（主DLL `0087FB88…`、MediumClass.dll `673C8045…`），尚未替换／上传 GitHub 附件；交付收尾与其余待办见 REMAINING.txt「下一步待做」。
- 2026-11-09：sxl1216的Vindictive Bastard／Menacing [PR #5](https://github.com/shenzhoudadi/CotW-Wrath/pull/5)及Sacred Servant／Live On [PR #8](https://github.com/shenzhoudadi/CotW-Wrath/pull/8)已合入源码。用户认可贡献者测试，不重复实机／专项；仅解决冲突、合并编译与DLL／图标核对。此前源码阶段未出包；现已统一纳入0.14.69 TEST，旧0.14.68附件不变。见docs/contributions/20261109-integration.md。不要重新开发这些内容。
- 接手先读AGENTS、CURRENT_RELEASE、WORK_STATUS；状态板优先于历史库存的旧进度描述，不重复已交付功能，不自动恢复排除项。
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
