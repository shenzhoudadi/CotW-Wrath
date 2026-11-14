# CotW-Wrath：一切从简

适用于整个项目。用户最新要求覆盖旧文档中的全历史闭包与过度验证流程。

1. 先读 `docs/agent-handoff/HANDOFF.md` 和 `CURRENT_RELEASE.md`。只有选择新迁移项时才查 `REMAINING.txt` 和 `inventory.json` 对应条目；不要默认遍历全部572条或旧证书。
2. 0.14.66已交付；已交付功能作为可用基线。围绕本轮明确问题修改，不恢复排除项，不自行开做HOLD队列。
3. 小工作包直接开发，共享机制只维护一份；避免重复实现、反复制订计划或无关重构。
4. 默认只编译、打包、核对必要文件和包内DLL。真正改变行为时，选择一次最小必要定向检查；说明、上传或同内容再次打包不触发重测。
5. 不自动跑全历史、旧证书链、多场景、双运行时或整套专项。不因旧日志、夹具、指纹缺失阻断新包，不为已有功能重建旧环境。
6. 遇到真实报错先看log，再针对复现修复。仅在实际变化或失败影响已检查内容时重跑受影响步骤；未测、失败、中止如实记录，不能写成PASS。
7. 每次记录编译、定向检查、打包和上传实际耗时。未运行的检查写“未运行”，不把零秒当通过。成本明显扩大时先收窄到具体问题。
8. 改完直接清理缓存、过期展开副本、废弃尝试、重复依赖与整树冻结备份；依赖按指纹单份保存，不先造另一份大归档，不维护全部历史重跑环境。
9. 保留当前源码、必要依赖、有用的当前测试与正式交付原件。旧文件可从Git历史/正式源码包找回；不得把整个旧源码树复制回main或重新上传历史垃圾。
10. 遵守Harmony、GUID/TypeId与提供方兼容合同，保留版权许可。游戏内自有文字用英文。不启动游戏或UMM，不把托管/隔离原生检查写成玩家游戏通过。
11. 正常出包和已授权清理直接执行、简短汇报，不重复请求授权；若执行工具拒绝，记录真实拒绝，不换执行器绕过或假称完成。
12. 用户说“出包”即要求将自上次交付以来的相关源码／说明提交并推送到GitHub，同时上传本次安装包并下载核对；不能只生成本地ZIP就结束。TEST包继续明确标TEST并使用预发布，不能自动改称正式版。除非用户明确只要本地包，否则无需再问是否上传。

## 向 shenzhoudadi/CotW-Wrath 提交精选改动

`main` 是日常开发分支，推送到 `origin`（`https://github.com/sxl1216/CotW-Wrath.git`）。`upstream-submissions` 是长期保留的上游提交专用分支，用于向 `upstream`（`https://github.com/shenzhoudadi/CotW-Wrath.git`）准备精选 Pull Request；不要在该分支做日常开发。

1. 开始准备上游提交前，先确保 `main` 已提交并推送，再执行 `git fetch upstream --prune`。
2. 每一轮上游提交都从最新 `upstream/main` 重建 `upstream-submissions`。不要把旧 PR 的提交长期叠加，也不要直接 merge `origin/main` 或整体 cherry-pick 含无关内容的日常提交。
3. 只摘取用户明确批准提交上游的功能文件、资源和必要说明。共享文件必须按代码块或数据项筛选：
   - `src/BlueprintRegistration.cs`、`src/ModuleCatalog.cs`：只加入相关注册或目录项。
   - `CallOfTheWild.Wrath.Prototype.csproj`：只加入相关资源声明。
   - `package/Localization-QL/CallOfTheWildWrathPrototypeLocalized.json`：只加入相关 key；不修改 enGB/zhCN 文件。
   - `docs/agent-handoff/inventory.json`：保留全部 572 项，只更新相关条目、`allowedRemainingIds` 和对应 authority 文案。
   - `HANDOFF.md`、`REMAINING.txt`：只写本轮批准的状态变化。
4. 默认排除未获批准的 Harmony 基线、构建脚本、测试修改、其他功能、Release 附件、历史产物和换行/行尾格式噪声。
5. 提交前至少检查：`git diff --name-status upstream/main..HEAD`、`git diff --check`、JSON 可解析、相关注册存在、资源已嵌入、本地化 key 存在、清单项数与 HOLD 数量正确。输出若出现大规模无意义增删，先处理编码、BOM、CRLF/LF 或整文件覆盖问题。
6. 精选提交推送到 `origin/upstream-submissions`。当前账号 `sxl1216` 对 `shenzhoudadi/CotW-Wrath` 没有直接写权限，直接 push 会返回 HTTP 403；因此从 `sxl1216:upstream-submissions` 向 `shenzhoudadi:main` 发起跨仓库 Pull Request。
7. 不强推 `upstream/main`，不把上游 Release 附件迁移到 fork。若一个 PR 已合并或关闭，下一轮先重新以最新 `upstream/main` 重建专用分支，再加入新的精选改动。
8. 用户只是在询问 Git 同步、分支或协作流程时，只回答和分析，**不得执行任何 Git 命令**。只有用户明确要求“同步、拉取、合并、重建、恢复、推送、删除或重命名分支”时才能实际操作。
9. 对方持续开发的新提交应正常同步到 `main`：先获取 `upstream/main`，再在 `main` 上进行普通 merge（或用户明确指定的 rebase），解决冲突并验证后推送到 `origin/main`。该同步用于保留双方协作成果，不应改写或清除本地功能提交。
10. `upstream-submissions` 不参与日常上游同步，也不作为 `main` 的合并来源。它只用于准备回馈给上游的精选 Pull Request。
11. 未经用户明确授权，禁止对 `upstream-submissions` 执行 `reset --hard`、force push、删除、重建或改写历史。旧 PR 未合并或未关闭时，必须保留对应远程分支和提交。
12. 需要重建 `upstream-submissions` 时，先 `git fetch origin upstream --prune`，核对本地分支、`origin/upstream-submissions`、旧 PR 状态和目标基线；确认远程精选提交已保存且用户明确要求后，才以最新 `upstream/main` 重建。若本地与远程不一致，优先保护远程提交，不得盲目覆盖。
13. 任何 force push 都必须由用户明确授权，并使用 `--force-with-lease`；若出现 stale info、权限拒绝或安全检查阻止，立即停止并报告，不能换方式绕过。
14. 确定新工作包后立即在GitHub创建或复用“施工中／请勿重复开发”issue，并更新根WORK_STATUS.md，注明范围、负责人／分工、基线和接手说明；实现尚未开始要明确写阶段。完成必要验证与GitHub交付后移入“已完成开发／TEST交付”并关闭对应issue，保留未测边界，不冒充玩家验收。选题先查现有认领，避免重复。
15. 开新功能前先查GitHub已有提交、PR（含已关闭入口）、分支、必要的fork和施工认领，确认别人是否已完成或正在做；不能只看main或搜索索引。别人已完成的优先复用，不另起重复实现；别人正在做的范围不得接手、修改或重复开发，除非用户明确要求介入。不因估计对方能力不足或进度慢而自动接管，遇到重叠改选其他内容。
16. 用户明确认可他人的实机测试结果时，接受该结果，不追加重复验证；保留“对方实机确认、尚未合入”与“本机已亲测”的区别。后续实际整合只做必要冲突处理、编译和包内DLL核对；若另改行为，只检查真正受影响的范围。

