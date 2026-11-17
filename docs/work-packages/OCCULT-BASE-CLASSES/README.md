# Occultist 与 Spiritualist 移植交接

两个基础职业已合入 main。联合 TEST 的包内 DLL 定向结果为 Occultist 994/994、Spiritualist 391/391。[0.14.75 联合 TEST](https://github.com/shenzhoudadi/CotW-Wrath/releases/tag/v0.14.75-Occultist-Spiritualist-TEST)。以下保留两项原始分工。

- [Occultist · COTW-0099 独立分工与执行指令](OCCULTIST.md) · [issue #15](https://github.com/shenzhoudadi/CotW-Wrath/issues/15)
- [Spiritualist · COTW-0085 独立分工与执行指令](SPIRITUALIST.md) · [issue #16](https://github.com/shenzhoudadi/CotW-Wrath/issues/16)

两套 A/C 可以并行。C 冻结合约后各自进入 B。执行者只需读自己职业的文件并执行指定角色。角色 D 指派给未参与对应实现的人。两个职业之间没有实现依赖。实际接入 `BlueprintRegistration.cs`、`ModuleCatalog.cs`、`ContentIdentity.cs` 或共享法表加载时由同一个整合负责人串行处理，并与 PR #14 核对冲突。准备阶段未改这些生产文件。每个职业齐套后再安排 TEST 验收，内部工作包无需单独出包。

GitHub 主库及 fork 的提交、分支、开放和关闭的 PR、issue 均已核对。PR #14 的 `Modules/MediumClass/Spiritualist/Phantom.cs` 改动只把原字段明确初始化为 null，现有 Medium 入口仍无 Spiritualist 注册。PR 另有共享注册、构建和 Harmony 改动，属于当前贡献者范围。 [PR #14](https://github.com/shenzhoudadi/CotW-Wrath/pull/14) · [原作固定快照](https://github.com/Holic75/KingmakerRebalance/tree/1332fb0db844b7863f484ca978bea2349fe49769)
