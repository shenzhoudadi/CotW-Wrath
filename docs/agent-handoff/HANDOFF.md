# 当前交接：从简继续
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
