# 外部贡献整合记录 — 2026-11-09

用户授权：查看其他人的更新，检查能否合并；没有明显代码问题即可合并，接受贡献者对自己内容的测试，不重复玩家测试或追加全量门槛。

- 整合基线：`5275b864c52414044c96e8b9dd8e6da58bbb42e6`，已交付安装包仍为0.14.68 Starseeker TEST。
- [PR #5](https://github.com/shenzhoudadi/CotW-Wrath/pull/5)：sxl1216，`71a9c2d6755dc6dd09f5cca5ade9058e5d5f4ab2`。Vindictive Bastard独立职业、Spirit-Bound Blade Menacing、图标和本地化。保留当前Starseeker注册、Speaker／Spirit Whisperer认领与0.14.68交接；只解决旧分支的注册／文档冲突，未改其玩法代码。原PR关于Relic Hunter依赖Occultist而搁置的记录保留，不重新开工。
- [PR #8](https://github.com/shenzhoudadi/CotW-Wrath/pull/8)：sxl1216，`bcd27389616f3abb5539dd00c65fc5c0c7cda694`。Sacred Servant、原生领域目录适配与Live On复用原生Paladin mercies。仅合入该PR五个生产源码文件，不合入fork上其他构建／测试环境变化。两个注册入口／ModuleCatalog发生同位置冲突，保留双方新增行。
- 检查了提交差异、注册／资源／回滚代码、引用与资源身份，未发现需要阻止本次整合的明显语法／引用／漏注册问题。依照用户要求接受贡献者测试，这不是独立完整机制审计或本机游戏验收。
- PR #5的新职业／Menacing叶文件、图片／本地化以及PR #8三个具体实现文件，合并后与原贡献提交一致；没有借整合改写其功能。

## 本机必要验证

在干净发布checkout的隔离整合分支进行，不覆盖带未提交Speaker实现的主开发工程，不覆盖其DLL。

命令：`dotnet build CallOfTheWild.Wrath.Prototype.csproj -c Release -p:WrathPath=<合法本地游戏目录> -p:BaseIntermediateOutputPath=<独立整合obj目录> -p:BaseOutputPath=<独立整合bin目录> --nologo -v:q`。

- 主工程编译：成功，0警告／0错误，进程总耗时 **5.138秒**。
- 程序集版本：仍为 `0.14.68.0`，未抬版本。
- 主DLL SHA256：`546FC1B03DC7A8BE65566FD696D489C7C1F28E7C5429D6A8B853113516D20C68`。
- 只读元数据核对：包含Havocker0020、EsotericStarseeker0081、VindictiveBastard0111、PsychicMenacing、SacredServant0110、LiveOnMercyLevel01458；包含嵌入资源 `CotW.HWMenacing.png`。未启动游戏或Unity场景。
- 定向／玩家测试：**本机未运行**，沿用用户认可的贡献者测试；未跑全量、全历史或其他包测试。
- 新安装包／包内DLL检查／新Release：**未运行／未生成**。本轮请求是合并源码，旧安装包不因此自动包含新功能。没有安装或启动UMM。
- 首次Git空白检查将Windows CRLF识别为尾随空白；随后按Git的`cr-at-eol`选项识别合法CRLF后检查通过，未因该问题重排整树或改玩法。

当前Speaker修复与Spirit Whisperer认领保持原状态；后续开发请先拉取main，不能把旧的整树工作副本覆盖回来。GitHub安装包发布仍需另一次正常TEST交付，不将本记录当作已经发布新版本。
