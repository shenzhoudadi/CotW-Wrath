# 当前交接：从简继续

- 正式版本：0.14.66；[下载](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.66)。Amnesiac五项反馈修复已交付，新增迁移项0；GitHub与原Drive均已下载回读。用户仅明确反馈Urban Bloodrager正常，Amnesiac不登记玩家验收。
- 主DLL SHA256：`242DC7220302179D64E6A4034C89F7BF51D5528902AC33FD589B7D5C9EBB5B59`。已有同DLL定向结果保留在发布记录中；不要为了接手重新跑一遍。
- 首先遵循根 `AGENTS.md` 和 `CURRENT_RELEASE.md`。只围绕本轮问题改代码、做一次必要定向检查、编译出包；真实问题按log修，不恢复全历史闭包。
- 当前没有自动开工的新迁移项。十三项HOLD见 `REMAINING.txt`；仅选新项时才按ID查 `inventory.json` 的范围/排除状态。
- 当前源码、必要构建工具和有用的当前测试保留；旧测试入口、全量发布器、旧版指南和重复流程文档已从main清理。旧内容仍在Git历史与正式版本源码包，按需找某个文件，不整树恢复。
- 默认构建只需合法游戏与Medium当前引用；可选诊断可能需要未公开的本地夹具。旧夹具缺失不阻断普通出包，不下载整个旧work。
- 每轮记录编译、定向检查、打包和上传实际用时；本轮轻量流程基准13.612秒，未执行定向测试，详见 `CURRENT_RELEASE.md`。
- 不启动游戏/UMM，不把未测写PASS。完成后清理无用产物并简短交接，不自动扩大范围或抬版本。
